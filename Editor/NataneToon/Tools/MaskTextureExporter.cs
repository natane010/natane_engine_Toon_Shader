using UnityEngine;
using UnityEditor;
using System.IO;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Export format options for mask textures.
    /// マスクテクスチャのエクスポート形式
    /// </summary>
    internal enum ExportFormat { PNG, TGA, EXR }

    /// <summary>
    /// Compression presets for texture import settings.
    /// テクスチャインポート設定用圧縮プリセット
    /// </summary>
    internal enum CompressionPreset { Default, VRChatPC, VRChatQuest, HighQuality }

    /// <summary>
    /// Multi-format mask texture exporter with VRChat-optimized compression presets.
    /// VRChat最適化圧縮プリセット付きマルチフォーマットマスクテクスチャエクスポーター
    /// </summary>
    internal static class MaskTextureExporter
    {
        /// <summary>
        /// Export a Texture2D to the specified path with format and compression settings.
        /// Texture2Dを指定パスに形式と圧縮設定でエクスポートする
        /// </summary>
        /// <param name="texture">Source texture to export.</param>
        /// <param name="path">Absolute or project-relative path (without extension).</param>
        /// <param name="format">Output file format.</param>
        /// <param name="preset">Compression preset to apply after import.</param>
        /// <returns>The asset path of the exported texture, or null on failure.</returns>
        public static string Export(Texture2D texture, string path, ExportFormat format, CompressionPreset preset)
        {
            if (texture == null)
            {
                Debug.LogError("[NataneToon] Export failed: texture is null.");
                return null;
            }

            string extension = GetFormatExtension(format);
            string fullPath = path;
            if (!fullPath.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase))
                fullPath += extension;

            // Ensure directory exists
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Encode texture data
            byte[] data;
            switch (format)
            {
                case ExportFormat.PNG:
                    data = texture.EncodeToPNG();
                    break;
                case ExportFormat.TGA:
                    data = texture.EncodeToTGA();
                    break;
                case ExportFormat.EXR:
                    data = texture.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
                    break;
                default:
                    data = texture.EncodeToPNG();
                    break;
            }

            if (data == null || data.Length == 0)
            {
                Debug.LogError("[NataneToon] Export failed: encoding returned no data.");
                return null;
            }

            try
            {
                File.WriteAllBytes(fullPath, data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Natane Toon] Failed to write file: {fullPath}\n{e.Message}");
                return null;
            }
            AssetDatabase.Refresh();

            // Convert to asset-relative path for TextureImporter
            string assetPath = fullPath;
            string dataPath = Application.dataPath;
            if (assetPath.StartsWith(dataPath))
            {
                assetPath = "Assets" + assetPath.Substring(dataPath.Length);
            }
            // Normalize path separators
            assetPath = assetPath.Replace('\\', '/');

            ApplyImportSettings(assetPath, preset);

            Debug.Log($"[NataneToon] Exported mask texture: {assetPath} ({preset})");
            return assetPath;
        }

        /// <summary>
        /// Apply compression preset import settings to an existing texture asset.
        /// 既存テクスチャアセットに圧縮プリセットインポート設定を適用する
        /// </summary>
        public static void ApplyImportSettings(string assetPath, CompressionPreset preset)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[NataneToon] Could not find TextureImporter for: {assetPath}");
                return;
            }

            // Common settings for mask textures
            importer.sRGBTexture = false;           // Masks are linear data
            importer.alphaIsTransparency = false;   // Not transparency data
            importer.mipmapEnabled = false;          // Masks don't need mipmaps
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;

            switch (preset)
            {
                case CompressionPreset.Default:
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    break;

                case CompressionPreset.VRChatPC:
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ; // BC7
                    break;

                case CompressionPreset.VRChatQuest:
                    importer.maxTextureSize = 1024;
                    importer.textureCompression = TextureImporterCompression.Compressed;

                    // Android platform override for ASTC compression
                    var androidSettings = importer.GetPlatformTextureSettings("Android");
                    androidSettings.overridden = true;
                    androidSettings.maxTextureSize = 1024;
                    androidSettings.format = TextureImporterFormat.ASTC_6x6;
                    androidSettings.compressionQuality = (int)TextureCompressionQuality.Normal;
                    importer.SetPlatformTextureSettings(androidSettings);
                    break;

                case CompressionPreset.HighQuality:
                    importer.maxTextureSize = 4096;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    break;
            }

            importer.SaveAndReimport();
        }

        /// <summary>
        /// Get file extension string for the given export format.
        /// エクスポート形式に対応するファイル拡張子を取得する
        /// </summary>
        public static string GetFormatExtension(ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.PNG: return ".png";
                case ExportFormat.TGA: return ".tga";
                case ExportFormat.EXR: return ".exr";
                default: return ".png";
            }
        }

        /// <summary>
        /// Get a description of what the compression preset does.
        /// 圧縮プリセットの説明を取得する
        /// </summary>
        public static string GetPresetDescription(CompressionPreset preset)
        {
            switch (preset)
            {
                case CompressionPreset.Default:
                    return L("標準圧縮 (2048px, Compressed)", "Standard compression (2048px, Compressed)");
                case CompressionPreset.VRChatPC:
                    return L("VRChat PC最適化 (2048px, BC7 高品質)", "VRChat PC optimized (2048px, BC7 HQ)");
                case CompressionPreset.VRChatQuest:
                    return L("VRChat Quest最適化 (1024px, ASTC 6x6)", "VRChat Quest optimized (1024px, ASTC 6x6)");
                case CompressionPreset.HighQuality:
                    return L("非圧縮 高品質 (4096px)", "Uncompressed high quality (4096px)");
                default:
                    return "";
            }
        }

        // UI state
        private static ExportFormat selectedFormat = ExportFormat.PNG;
        private static CompressionPreset selectedPreset = CompressionPreset.VRChatPC;
        private static string lastExportPath = "";

        /// <summary>
        /// Draw the export settings UI panel.
        /// エクスポート設定UIパネルを描画する
        /// </summary>
        /// <param name="texture">Texture to export. If null, the export button is disabled.</param>
        /// <returns>True if the texture was saved during this call.</returns>
        public static bool DrawExportUI(Texture2D texture)
        {
            bool saved = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("エクスポート", "Export"), EditorStyles.boldLabel);

            selectedFormat = (ExportFormat)EditorGUILayout.EnumPopup(
                new GUIContent(L("形式", "Format"), L("出力ファイル形式", "Output file format")),
                selectedFormat);

            selectedPreset = (CompressionPreset)EditorGUILayout.EnumPopup(
                new GUIContent(L("プリセット", "Preset"), L("圧縮設定プリセット", "Compression preset")),
                selectedPreset);

            // Show preset description
            EditorGUILayout.HelpBox(GetPresetDescription(selectedPreset), MessageType.Info);

            EditorGUILayout.Space(5);

            GUI.enabled = texture != null;
            if (GUILayout.Button(L("保存", "Save"), GUILayout.Height(28)))
            {
                string extension = GetFormatExtension(selectedFormat);
                string defaultName = "MaskTexture" + extension;
                string defaultDir = string.IsNullOrEmpty(lastExportPath)
                    ? Application.dataPath
                    : Path.GetDirectoryName(lastExportPath);

                string filePath = EditorUtility.SaveFilePanel(
                    L("マスクテクスチャを保存", "Save Mask Texture"),
                    defaultDir,
                    defaultName,
                    extension.TrimStart('.'));

                if (!string.IsNullOrEmpty(filePath))
                {
                    lastExportPath = filePath;
                    string result = Export(texture, filePath, selectedFormat, selectedPreset);
                    if (result != null)
                    {
                        saved = true;
                        EditorUtility.DisplayDialog(
                            L("エクスポート完了", "Export Complete"),
                            L($"保存先: {result}\nプリセット: {selectedPreset}", $"Saved to: {result}\nPreset: {selectedPreset}"),
                            "OK");
                    }
                }
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
            return saved;
        }
    }
}
