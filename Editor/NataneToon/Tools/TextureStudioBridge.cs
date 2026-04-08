using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace NataneToon.Editor
{
    /// <summary>
    /// Named Pipe client bridge for communicating with NataneTextureStudio.exe.
    /// NataneTextureStudio.exe と通信するための名前付きパイプクライアントブリッジ
    /// </summary>
    internal static class TextureStudioBridge
    {
        private static NamedPipeClientStream _pipe;
        private static StreamReader _reader;
        private static StreamWriter _writer;
        private static CancellationTokenSource _cts;
        private static bool _connected;

        // Live preview system
        private static TextureStudioLivePreview _livePreview;

        // Cached mesh for UV requests (when Selection changes while studio is active)
        private static Mesh _lastKnownMesh;
        private static int _lastKnownMeshSlot = -1;

        /// <summary>Cache a mesh for later UV requests.</summary>
        public static void CacheMesh(Mesh mesh, int slot = -1)
        {
            _lastKnownMesh = mesh;
            _lastKnownMeshSlot = slot;
        }

        public static bool IsConnected => _connected;
        public static bool IsLivePreviewEnabled => _livePreview != null && _livePreview.IsEnabled;

        /// <summary>Connect to the studio's named pipe server.</summary>
        public static void Connect(string pipeName)
        {
            Disconnect();
            _cts = new CancellationTokenSource();

            var token = _cts.Token;
            Task.Run(async () =>
            {
                try
                {
                    // Retry connection with exponential backoff (exe may still be starting)
                    const int maxRetries = 8;
                    int delayMs = 500;
                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        try
                        {
                            _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                            await _pipe.ConnectAsync(3000, token);
                            break; // Connected
                        }
                        catch (TimeoutException) when (attempt < maxRetries - 1)
                        {
                            _pipe?.Dispose();
                            _pipe = null;
                            Debug.Log($"[TextureStudioBridge] Attempt {attempt + 1}/{maxRetries} timed out, retrying in {delayMs}ms...");
                            await Task.Delay(delayMs, token);
                            delayMs = Math.Min(delayMs * 2, 8000); // Exponential backoff, cap at 8s
                        }
                    }

                    if (_pipe == null || !_pipe.IsConnected)
                    {
                        Debug.LogWarning($"[TextureStudioBridge] Could not connect after {maxRetries} attempts.");
                        return;
                    }

                    _reader = new StreamReader(_pipe, Encoding.UTF8);
                    _writer = new StreamWriter(_pipe, Encoding.UTF8) { AutoFlush = true };
                    _connected = true;

                    Debug.Log("[TextureStudioBridge] Connected to pipe: " + pipeName);

                    // Listen for events from studio
                    await ListenLoop(token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TextureStudioBridge] Connection failed: " + ex.Message);
                }
            });
        }

        public static void Disconnect()
        {
            _connected = false;
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            if (_writer != null) { _writer.Dispose(); _writer = null; }
            if (_reader != null) { _reader.Dispose(); _reader = null; }
            if (_pipe != null) { _pipe.Dispose(); _pipe = null; }
        }

        /// <summary>Send an openTexture command to the studio.</summary>
        public static void SendOpenTexture(string path, string propertyName)
        {
            SendMessage("{\"method\":\"openTexture\",\"params\":{\"path\":\"" + EscapeJson(path) + "\",\"propertyName\":\"" + EscapeJson(propertyName) + "\"}}");
        }

        /// <summary>Send an importLayer command to the studio.</summary>
        public static void SendImportLayer(string path, string name, float opacity = 1f)
        {
            SendMessage("{\"method\":\"importLayer\",\"params\":{\"path\":\"" + EscapeJson(path) + "\",\"name\":\"" + EscapeJson(name) + "\",\"opacity\":" + opacity.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}}");
        }

        /// <summary>Send a raw JSON string to the studio (public wrapper for SendMessage).</summary>
        public static void SendRaw(string json)
        {
            SendMessage(json);
        }

        private static void SendMessage(string json)
        {
            // Check actual pipe state, not just the _connected flag
            if (_writer == null || _pipe == null || !_pipe.IsConnected)
            {
                // Try to reconnect if studio is running
                if (TextureStudioLauncher.IsRunning && !string.IsNullOrEmpty(TextureStudioLauncher.PipeName))
                {
                    if (!_connected)
                    {
                        Debug.Log("[TextureStudioBridge] Reconnecting...");
                        Connect(TextureStudioLauncher.PipeName);
                    }
                    // Queue the message for retry after connection with 3 attempts
                    string capturedJson = json;
                    int retryCount = 0;
                    void RetryQueuedSend()
                    {
                        if (_connected && _writer != null && _pipe != null && _pipe.IsConnected)
                        {
                            try
                            {
                                _writer.WriteLine(capturedJson);
                                Debug.Log("[TextureStudioBridge] Queued message sent after reconnect.");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning("[TextureStudioBridge] Queued send failed: " + ex.Message);
                                _connected = false;
                            }
                        }
                        else if (retryCount < 3)
                        {
                            retryCount++;
                            EditorApplication.delayCall += RetryQueuedSend;
                        }
                        else
                        {
                            Debug.LogWarning("[TextureStudioBridge] Queued message dropped after 3 retries.");
                        }
                    }
                    EditorApplication.delayCall += RetryQueuedSend;
                    return;
                }
                Debug.LogWarning("[TextureStudioBridge] Not connected. Launch Texture Studio first.");
                return;
            }

            try
            {
                _writer.WriteLine(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TextureStudioBridge] Send failed: " + ex.Message);
                _connected = false;
            }
        }

        private static async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _reader != null)
            {
                try
                {
                    string line = await _reader.ReadLineAsync();
                    if (line == null) break; // Pipe closed

                    // Dispatch to main thread
                    string captured = line;
                    EditorApplication.delayCall += () => HandleEvent(captured);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TextureStudioBridge] Read error: " + ex.Message);
                    break;
                }
            }
            _connected = false;
            // Clean up pipe resources so next Connect starts fresh
            try { _writer?.Dispose(); } catch { } _writer = null;
            try { _reader?.Dispose(); } catch { } _reader = null;
            try { _pipe?.Dispose(); } catch { } _pipe = null;

            // Studio closed — restore original texture if live preview was active
            EditorApplication.delayCall += () =>
            {
                if (_livePreview != null && _livePreview.IsEnabled)
                {
                    _livePreview.Disable();
                    Debug.Log("[TextureStudioBridge] Studio disconnected — live preview disabled, original texture restored.");
                }
                // Auto-reconnect if studio was relaunched
                if (TextureStudioLauncher.IsRunning && !string.IsNullOrEmpty(TextureStudioLauncher.PipeName))
                {
                    Debug.Log("[TextureStudioBridge] Auto-reconnecting to new studio instance...");
                    Connect(TextureStudioLauncher.PipeName);
                }
            };
        }

        private static void HandleEvent(string json)
        {
            try
            {
                Debug.Log("[TextureStudioBridge] RX: " + (json.Length > 100 ? json.Substring(0, 100) + "..." : json));

                // Simple JSON parsing for event type
                if (json.Contains("\"event\":\"saved\""))
                {
                    // Extract path from JSON
                    string savedPath = ExtractJsonString(json, "path");
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        // Reimport the asset in Unity
                        string assetPath = FileToAssetPath(savedPath);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                            Debug.Log("[TextureStudioBridge] Reimported: " + assetPath);
                        }
                    }
                }
                else if (json.Contains("\"event\":\"enableLivePreviewRequest\""))
                {
                    // Studio requests Unity to enable live preview
                    string propName = ExtractJsonString(json, "propertyName");
                    if (string.IsNullOrEmpty(propName)) propName = "_MainTex";

                    // Try selected object first, then search all scene renderers
                    Material targetMat = FindTargetMaterial();
                    if (targetMat == null)
                    {
                        // Search all renderers for a material with this property
                        var renderers = Object.FindObjectsOfType<Renderer>();
                        foreach (var r in renderers)
                        {
                            if (r.sharedMaterials == null) continue;
                            foreach (var m in r.sharedMaterials)
                            {
                                if (m != null && m.HasProperty(propName) && m.GetTexture(propName) != null)
                                {
                                    targetMat = m;
                                    Selection.activeGameObject = r.gameObject;
                                    break;
                                }
                            }
                            if (targetMat != null) break;
                        }
                    }

                    if (targetMat != null)
                    {
                        EnableLivePreview(targetMat, propName);
                        Debug.Log("[TextureStudioBridge] Live preview enabled: " + targetMat.name + "." + propName);
                        // Notify studio that live preview is active
                        SendMessage("{\"event\":\"livePreviewActive\",\"params\":{\"material\":\"" + EscapeJson(targetMat.name) + "\"}}");
                    }
                    else
                    {
                        Debug.LogWarning("[TextureStudioBridge] Live preview: プロパティ '" + propName + "' を持つマテリアルが見つかりません。");
                        SendMessage("{\"event\":\"livePreviewFailed\",\"params\":{\"reason\":\"マテリアルが見つかりません\"}}");
                    }
                }
                else if (json.Contains("\"event\":\"disableLivePreviewRequest\""))
                {
                    DisableLivePreview();
                    Debug.Log("[TextureStudioBridge] Live preview disabled by studio.");
                }
                else if (json.Contains("\"event\":\"preview\""))
                {
                    // Live preview update from studio
                    string tempPath = ExtractJsonString(json, "tempPath");
                    if (!string.IsNullOrEmpty(tempPath))
                    {
                        _livePreview?.OnPreviewUpdate(tempPath);
                    }
                }
                else if (json.Contains("\"event\":\"requestSceneData\""))
                {
                    // Studio requests all scene renderers/materials/textures
                    SendSceneData();
                }
                else if (json.Contains("\"event\":\"requestUVData\""))
                {
                    // Studio requests UV wireframe — try Selection first, then cached mesh
                    var mesh = TextureStudioUVExporter.GetMeshFromSelection();
                    if (mesh == null) mesh = _lastKnownMesh;

                    // Also search all scene renderers as last resort
                    if (mesh == null)
                    {
                        var renderers = Object.FindObjectsOfType<Renderer>();
                        foreach (var r in renderers)
                        {
                            var mf = r.GetComponent<MeshFilter>();
                            if (mf != null && mf.sharedMesh != null) { mesh = mf.sharedMesh; break; }
                            var smr = r as SkinnedMeshRenderer;
                            if (smr != null && smr.sharedMesh != null) { mesh = smr.sharedMesh; break; }
                        }
                    }

                    if (mesh != null)
                    {
                        TextureStudioUVExporter.SendUVWireframe(mesh, _lastKnownMeshSlot);
                        _lastKnownMesh = mesh;
                        Debug.Log("[TextureStudioBridge] Sent UV wireframe: " + mesh.name);
                    }
                    else
                    {
                        Debug.LogWarning("[TextureStudioBridge] UV要求: シーンにメッシュが見つかりません");
                    }
                }
                else if (json.Contains("\"event\":\"selectMaterialTexture\""))
                {
                    // Studio selected a specific material texture - open it
                    string materialName = ExtractJsonString(json, "materialName");
                    string propertyName = ExtractJsonString(json, "propertyName");
                    HandleSelectMaterialTexture(materialName, propertyName);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TextureStudioBridge] Event handling error: " + ex.Message);
            }
        }

        /// <summary>
        /// Enable live preview for a material property.
        /// マテリアルプロパティのライブプレビューを有効にする
        /// </summary>
        public static void EnableLivePreview(Material material, string propertyName)
        {
            if (_livePreview == null)
                _livePreview = new TextureStudioLivePreview();
            _livePreview.Enable(material, propertyName);

            // Notify the studio to start sending preview updates
            SendMessage("{\"method\":\"enableLivePreview\",\"params\":{}}");
        }

        /// <summary>
        /// Disable live preview and restore the original texture.
        /// ライブプレビューを無効にし、元のテクスチャを復元する
        /// </summary>
        public static void DisableLivePreview()
        {
            _livePreview?.Disable();

            // Notify the studio to stop sending preview updates
            SendMessage("{\"method\":\"disableLivePreview\",\"params\":{}}");
        }

        /// <summary>
        /// Collect all renderers, materials, and textures in the scene and send to studio.
        /// シーン内の全レンダラー・マテリアル・テクスチャを収集してスタジオに送信する
        /// </summary>
        private static void SendSceneData()
        {
            var renderers = Object.FindObjectsOfType<Renderer>();
            var sb = new StringBuilder();
            sb.Append("{\"event\":\"sceneData\",\"params\":{\"objects\":[");

            bool first = true;
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null) continue;
                var go = renderer.gameObject;

                if (!first) sb.Append(",");
                first = false;

                sb.Append("{\"name\":\"").Append(EscapeJson(go.name));
                sb.Append("\",\"rendererType\":\"").Append(EscapeJson(renderer.GetType().Name));
                sb.Append("\",\"materials\":[");

                bool firstMat = true;
                for (int m = 0; m < renderer.sharedMaterials.Length; m++)
                {
                    var mat = renderer.sharedMaterials[m];
                    if (mat == null) continue;

                    if (!firstMat) sb.Append(",");
                    firstMat = false;

                    sb.Append("{\"name\":\"").Append(EscapeJson(mat.name));
                    sb.Append("\",\"slot\":").Append(m);
                    sb.Append(",\"shader\":\"").Append(EscapeJson(mat.shader != null ? mat.shader.name : ""));
                    sb.Append("\",\"textures\":[");

                    var shader = mat.shader;
                    int propCount = ShaderUtil.GetPropertyCount(shader);
                    bool firstTex = true;
                    for (int p = 0; p < propCount; p++)
                    {
                        if (ShaderUtil.GetPropertyType(shader, p) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                        string propName = ShaderUtil.GetPropertyName(shader, p);
                        Texture tex = mat.GetTexture(propName);
                        if (tex == null) continue;

                        if (!firstTex) sb.Append(",");
                        firstTex = false;

                        string desc = ShaderUtil.GetPropertyDescription(shader, p);
                        string path = AssetDatabase.GetAssetPath(tex);
                        sb.Append("{\"property\":\"").Append(EscapeJson(propName));
                        sb.Append("\",\"label\":\"").Append(EscapeJson(desc));
                        sb.Append("\",\"path\":\"").Append(EscapeJson(!string.IsNullOrEmpty(path) ? Path.GetFullPath(path) : ""));
                        sb.Append("\",\"width\":").Append(tex.width);
                        sb.Append(",\"height\":").Append(tex.height).Append("}");
                    }
                    sb.Append("]}");
                }
                sb.Append("]}");
            }
            sb.Append("]}}");
            SendMessage(sb.ToString());
            Debug.Log("[TextureStudioBridge] Sent scene data (" + renderers.Length + " renderers)");
        }

        /// <summary>
        /// Handle studio selecting a specific material texture - send it for editing.
        /// スタジオが選択したマテリアルテクスチャを処理する
        /// </summary>
        private static void HandleSelectMaterialTexture(string materialName, string propertyName)
        {
            if (string.IsNullOrEmpty(materialName) || string.IsNullOrEmpty(propertyName))
            {
                Debug.LogWarning("[TextureStudioBridge] selectMaterialTexture: missing materialName or propertyName");
                return;
            }

            var renderers = Object.FindObjectsOfType<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null) continue;
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null || mat.name != materialName) continue;

                    Texture tex = mat.GetTexture(propertyName);
                    if (tex == null) continue;

                    string path = AssetDatabase.GetAssetPath(tex);
                    if (string.IsNullOrEmpty(path)) continue;

                    // Send the texture to studio for editing
                    SendOpenTexture(Path.GetFullPath(path), propertyName);

                    // Send UV wireframe from the renderer's mesh
                    Mesh mesh = null;
                    var mf = renderer.GetComponent<MeshFilter>();
                    if (mf != null) mesh = mf.sharedMesh;
                    else
                    {
                        var smr = renderer as SkinnedMeshRenderer;
                        if (smr != null) mesh = smr.sharedMesh;
                    }
                    if (mesh != null)
                    {
                        int slotIdx = System.Array.IndexOf(renderer.sharedMaterials, mat);
                        TextureStudioUVExporter.SendUVWireframe(mesh, slotIdx >= 0 ? slotIdx : -1);
                    }

                    // Auto-enable live preview for the selected texture
                    EnableLivePreview(mat, propertyName);

                    // Select the GameObject in Unity editor
                    Selection.activeGameObject = renderer.gameObject;

                    Debug.Log($"[TextureStudioBridge] Selected: {mat.name}.{propertyName}");
                    return;
                }
            }
            Debug.LogWarning($"[TextureStudioBridge] Material not found: {materialName}");
        }

        private static string FileToAssetPath(string fullPath)
        {
            fullPath = fullPath.Replace("\\", "/");
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (fullPath.StartsWith(dataPath))
                return "Assets" + fullPath.Substring(dataPath.Length);
            return "";
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// Extract a string value from a simple JSON object by key name.
        /// シンプルな JSON オブジェクトからキー名で文字列値を抽出する
        /// </summary>
        private static string ExtractJsonString(string json, string key)
        {
            string search = "\"" + key + "\":\"";
            int start = json.IndexOf(search);
            if (start < 0) return null;
            start += search.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return null;
            return json.Substring(start, end - start).Replace("\\\\", "\\").Replace("\\\"", "\"");
        }

        /// <summary>
        /// Find a material from the currently selected GameObject in the scene.
        /// 選択中のGameObjectからマテリアルを取得する
        /// </summary>
        private static Material FindTargetMaterial()
        {
            var go = Selection.activeGameObject;
            if (go == null) return null;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
                return renderer.sharedMaterial;

            return null;
        }

    }
}
