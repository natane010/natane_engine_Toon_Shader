using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

namespace NataneToon.Editor
{
    /// <summary>
    /// Launches NataneTextureStudio.exe from the Unity package.
    /// Unity パッケージから NataneTextureStudio.exe を起動するランチャー
    /// </summary>
    [InitializeOnLoad]
    internal static class TextureStudioLauncher
    {
        private static Process _studioProcess;
        private static string _currentPipeName;

        static TextureStudioLauncher()
        {
            // Auto-send UV when Selection changes while studio is running
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            SendUVFromSelectionIfAvailable();
        }

        /// <summary>Whether the studio process is currently running.</summary>
        public static bool IsRunning => _studioProcess != null && !_studioProcess.HasExited;

        /// <summary>Current pipe name for IPC.</summary>
        public static string PipeName => _currentPipeName;

        [MenuItem("Tools/Natane/テクスチャスタジオ Texture Studio")]
        public static void Launch()
        {
            LaunchWithTexture(null, null);

            // Auto-send UV wireframe from selected mesh after connection is established
            EditorApplication.delayCall += () =>
                EditorApplication.delayCall += () =>
                    EditorApplication.delayCall += () => SendUVFromSelectionIfAvailable();
        }

        /// <summary>
        /// Send UV wireframe from the currently selected GameObject (if it has a mesh).
        /// 選択中のGameObjectからUVワイヤーフレームを送信（メッシュがある場合）
        /// </summary>
        public static void SendUVFromSelectionIfAvailable()
        {
            if (!IsRunning || !TextureStudioBridge.IsConnected) return;
            var mesh = TextureStudioUVExporter.GetMeshFromSelection();
            if (mesh != null)
            {
                TextureStudioUVExporter.SendUVWireframe(mesh);
                UnityEngine.Debug.Log("[NataneTextureStudio] Auto-sent UV wireframe: " + mesh.name);
            }
        }

        /// <summary>
        /// Launch the studio, optionally opening a texture for editing.
        /// スタジオを起動し、オプションでテクスチャを開いて編集する
        /// </summary>
        public static void LaunchWithTexture(string texturePath, string propertyName)
        {
            UnityEngine.Debug.Log("[NataneTextureStudio] LaunchWithTexture called. texturePath=" + (texturePath ?? "null") + " propertyName=" + (propertyName ?? "null"));

            if (IsRunning)
            {
                UnityEngine.Debug.Log("[NataneTextureStudio] Already running, sending open command.");
                if (texturePath != null)
                    TextureStudioBridge.SendOpenTexture(texturePath, propertyName ?? "");
                return;
            }

            string exePath = FindExePath();
            UnityEngine.Debug.Log("[NataneTextureStudio] FindExePath result: " + (string.IsNullOrEmpty(exePath) ? "(empty)" : exePath) + " exists=" + (!string.IsNullOrEmpty(exePath) && File.Exists(exePath)));

            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                // Fallback: try dotnet run from source
                string projectDir = FindProjectDir();
                if (!string.IsNullOrEmpty(projectDir))
                {
                    _currentPipeName = "NataneTSPipe_" + Process.GetCurrentProcess().Id;
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = string.Format("run --project \"{0}\" -- --pipe {1}", projectDir, _currentPipeName),
                        UseShellExecute = false,
                        CreateNoWindow = false,
                    };
                    // Add texture path if specified
                    if (!string.IsNullOrEmpty(texturePath))
                        startInfo.Arguments += string.Format(" --open \"{0}\"", Path.GetFullPath(texturePath));
                    if (!string.IsNullOrEmpty(propertyName))
                        startInfo.Arguments += " --property " + propertyName;

                    try
                    {
                        _studioProcess = Process.Start(startInfo);
                        string pipeName = _currentPipeName;
                        EditorApplication.delayCall += () => TextureStudioBridge.Connect(pipeName);
                        UnityEngine.Debug.Log("[NataneTextureStudio] Launched via dotnet run (pipe: " + _currentPipeName + ")");
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError("[NataneTextureStudio] Failed to launch: " + ex.Message);
                    }
                    return;
                }

                EditorUtility.DisplayDialog("Natane Texture Studio",
                    "NataneTextureStudio.exe \u304c\u898b\u3064\u304b\u308a\u307e\u305b\u3093\u3002\nPlugins/Tools/ \u30c7\u30a3\u30ec\u30af\u30c8\u30ea\u3092\u78ba\u8a8d\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                    "OK");
                return;
            }

            _currentPipeName = "NataneTSPipe_" + Process.GetCurrentProcess().Id;
            var args = "--pipe " + _currentPipeName;
            if (!string.IsNullOrEmpty(texturePath))
                args += string.Format(" --open \"{0}\"", Path.GetFullPath(texturePath));
            if (!string.IsNullOrEmpty(propertyName))
                args += " --property " + propertyName;

            try
            {
                _studioProcess = Process.Start(exePath, args);
                string pipeNameCapture = _currentPipeName;
                EditorApplication.delayCall += () => TextureStudioBridge.Connect(pipeNameCapture);
                UnityEngine.Debug.Log("[NataneTextureStudio] Launched (pipe: " + _currentPipeName + ")");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("[NataneTextureStudio] Failed to launch: " + ex.Message);
            }
        }

        /// <summary>
        /// Legacy compatibility: called via reflection from ShaderGUI.
        /// ShaderGUI からリフレクション経由で呼ばれる旧互換メソッド
        /// </summary>
        public static void OpenForProperty(Material material, string propertyName, Texture2D currentTexture)
        {
            if (material == null) return;
            string texturePath = null;
            if (currentTexture != null)
            {
                texturePath = AssetDatabase.GetAssetPath(currentTexture);
            }
            LaunchWithTexture(texturePath, propertyName);
        }

        private static string FindExePath()
        {
            // Search for exe in Plugins/Tools/
            string[] guids = AssetDatabase.FindAssets("NataneTextureStudio");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("NataneTextureStudio.exe"))
                    return Path.GetFullPath(path);
            }

            // Fallback: relative path from package root
            string packageRoot = GetPackageRoot();
            if (!string.IsNullOrEmpty(packageRoot))
            {
                string candidate = Path.Combine(packageRoot, "Plugins", "Tools", "NataneTextureStudio.exe");
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }

            return "";
        }

        private static string FindProjectDir()
        {
            // Find the .csproj source directory for dotnet run fallback
            string packageRoot = GetPackageRoot();
            if (string.IsNullOrEmpty(packageRoot)) return "";
            string projectDir = Path.Combine(packageRoot, "NativeSource~", "NataneTextureStudio");
            if (File.Exists(Path.Combine(projectDir, "NataneTextureStudio.csproj")))
                return projectDir;
            return "";
        }

        /// <summary>
        /// Launch Texture Studio from a selected mesh, showing material slot picker.
        /// メッシュ選択からテクスチャスタジオを起動（マテリアルスロット選択付き）
        /// </summary>
        public static void LaunchFromMesh(GameObject go)
        {
            UnityEngine.Debug.Log("[NataneTextureStudio] LaunchFromMesh called. go=" + (go != null ? go.name : "null"));
            if (go == null) return;

            // Get renderer
            Renderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = go.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null || renderer.sharedMaterials.Length == 0)
            {
                EditorUtility.DisplayDialog("Texture Studio", "選択したオブジェクトにマテリアルがありません。", "OK");
                return;
            }

            // Collect all available textures from all material slots
            var entries = new System.Collections.Generic.List<(string label, string path, string propName, int slot, Material mat)>();
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;
                var shader = mat.shader;
                int propCount = ShaderUtil.GetPropertyCount(shader);
                for (int p = 0; p < propCount; p++)
                {
                    if (ShaderUtil.GetPropertyType(shader, p) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                    string propName = ShaderUtil.GetPropertyName(shader, p);
                    Texture tex = mat.GetTexture(propName);
                    if (tex == null) continue;
                    string propDesc = ShaderUtil.GetPropertyDescription(shader, p);
                    string texPath = AssetDatabase.GetAssetPath(tex);
                    if (string.IsNullOrEmpty(texPath)) continue;
                    entries.Add((string.Format("{0} / {1} ({2})", mat.name, propDesc, propName), Path.GetFullPath(texPath), propName, i, mat));
                }
            }

            UnityEngine.Debug.Log("[NataneTextureStudio] Found " + entries.Count + " textures on " + go.name);

            if (entries.Count == 0)
            {
                // No textures — just launch the studio empty
                UnityEngine.Debug.Log("[NataneTextureStudio] No textures found, launching empty studio.");
                Launch();
                return;
            }

            if (entries.Count == 1)
            {
                // Only one texture — launch directly without menu
                var e = entries[0];
                UnityEngine.Debug.Log("[NataneTextureStudio] Single texture, launching directly: " + e.label);
                LaunchWithTextureAndUV(e.path, e.propName, e.slot, e.mat, go);
                return;
            }

            // Multiple textures — show picker via delayCall to avoid context menu timing issues
            var capturedEntries = entries;
            var capturedGo = go;
            EditorApplication.delayCall += () =>
            {
                GenericMenu menu = new GenericMenu();
                foreach (var entry in capturedEntries)
                {
                    var captured = entry;
                    menu.AddItem(new GUIContent(captured.label), false, () =>
                    {
                        LaunchWithTextureAndUV(captured.path, captured.propName, captured.slot, captured.mat, capturedGo);
                    });
                }
                menu.ShowAsContext();
            };
        }

        private static void LaunchWithTextureAndUV(string texPath, string propName, int slot, Material mat, GameObject go)
        {
            LaunchWithTexture(texPath, propName);

            // Send UV + enable live preview after connection
            int capturedSlot = slot;
            Material capturedMat = mat;
            string capturedProp = propName;
            EditorApplication.delayCall += () =>
                EditorApplication.delayCall += () =>
                    EditorApplication.delayCall += () =>
                    {
                        var mesh = TextureStudioUVExporter.GetMeshFromSelection();
                        if (mesh != null) TextureStudioUVExporter.SendUVWireframe(mesh, capturedSlot);
                        if (TextureStudioBridge.IsConnected)
                            TextureStudioBridge.EnableLivePreview(capturedMat, capturedProp);
                    };
        }

        [MenuItem("GameObject/Natane Texture Studio で編集", false, 49)]
        public static void LaunchFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go != null) LaunchFromMesh(go);
        }

        [MenuItem("GameObject/Natane Texture Studio で編集", true)]
        public static bool ValidateLaunchFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null) return false;
            return go.GetComponent<MeshRenderer>() != null || go.GetComponent<SkinnedMeshRenderer>() != null;
        }

        private static string GetPackageRoot()
        {
            string[] guids = AssetDatabase.FindAssets("t:TextAsset package");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("com.natane.toonshader") && path.EndsWith("package.json"))
                    return Path.GetDirectoryName(Path.GetFullPath(path)) ?? "";
            }
            return "";
        }
    }
}
