using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                    // Retry connection with delay (exe may still be starting)
                    const int maxRetries = 6;
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
                            await Task.Delay(1000, token); // Wait 1s before retry
                        }
                    }

                    if (_pipe == null || !_pipe.IsConnected)
                    {
                        Debug.LogWarning("[TextureStudioBridge] Could not connect after retries.");
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
            if (!_connected || _writer == null)
            {
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
        }

        private static void HandleEvent(string json)
        {
            try
            {
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
                else if (json.Contains("\"event\":\"preview\""))
                {
                    // Live preview update from studio
                    string tempPath = ExtractJsonString(json, "tempPath");
                    if (!string.IsNullOrEmpty(tempPath))
                    {
                        _livePreview?.OnPreviewUpdate(tempPath);
                    }
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
    }
}
