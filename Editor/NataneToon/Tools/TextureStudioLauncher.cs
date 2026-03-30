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
    internal static class TextureStudioLauncher
    {
        private static Process _studioProcess;
        private static string _currentPipeName;

        /// <summary>Whether the studio process is currently running.</summary>
        public static bool IsRunning => _studioProcess != null && !_studioProcess.HasExited;

        /// <summary>Current pipe name for IPC.</summary>
        public static string PipeName => _currentPipeName;

        [MenuItem("Tools/Natane/テクスチャスタジオ Texture Studio")]
        public static void Launch()
        {
            LaunchWithTexture(null, null);
        }

        /// <summary>
        /// Launch the studio, optionally opening a texture for editing.
        /// スタジオを起動し、オプションでテクスチャを開いて編集する
        /// </summary>
        public static void LaunchWithTexture(string texturePath, string propertyName)
        {
            if (IsRunning)
            {
                // Already running -- send open command via bridge instead
                if (texturePath != null)
                    TextureStudioBridge.SendOpenTexture(texturePath, propertyName ?? "");
                return;
            }

            string exePath = FindExePath();
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
