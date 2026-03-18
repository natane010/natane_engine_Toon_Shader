using System.IO;
using UnityEngine;
using UnityEditor;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    /// <summary>
    /// Saves generated PBR map textures as PNG assets and configures TextureImporter.
    /// </summary>
    public static class AutoMatTextureExporter
    {
        /// <summary>
        /// Save a generated map as a PNG asset and return the imported Texture2D.
        /// </summary>
        public static Texture2D SaveMap(Texture2D tex, string folder, string baseName, string mapSuffix, bool isLinear, bool isNormalMap = false)
        {
            if (tex == null) return null;

            string dir = EnsureFolder(folder);
            string fileName = $"{baseName}_{mapSuffix}.png";
            string assetPath = $"{dir}/{fileName}";

            // Write PNG
            byte[] bytes = tex.EncodeToPNG();
            string absPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                assetPath.Replace('/', Path.DirectorySeparatorChar));

            string dirPath = Path.GetDirectoryName(absPath);
            if (!string.IsNullOrEmpty(dirPath))
                Directory.CreateDirectory(dirPath);

            File.WriteAllBytes(absPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Configure importer
            ConfigureImporter(assetPath, isLinear, isNormalMap);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        /// <summary>
        /// Save all maps from a generation result.
        /// </summary>
        public static AutoMatSavedMaps SaveAll(AutoMatGenerationResult result, Texture2D sourceAlbedo, string folder, string baseName)
        {
            if (result == null) return null;

            var saved = new AutoMatSavedMaps();
            saved.normal = SaveMap(result.normal, folder, baseName, "Normal", true, true);
            saved.roughness = SaveMap(result.roughness, folder, baseName, "Roughness", true);
            saved.metallic = SaveMap(result.metallic, folder, baseName, "Metallic", true);
            saved.height = SaveMap(result.height, folder, baseName, "Height", true);
            saved.ao = SaveMap(result.ao, folder, baseName, "AO", true);
            if (result.curvature != null)
                saved.curvature = SaveMap(result.curvature, folder, baseName, "Curvature", true);
            return saved;
        }

        /// <summary>
        /// Apply generated maps to a material's properties.
        /// </summary>
        public static void ApplyToMaterial(Material mat, AutoMatSavedMaps maps)
        {
            if (mat == null || maps == null) return;

            Undo.RecordObject(mat, L("PBRマップを適用", "Apply PBR Maps"));

            if (maps.normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", maps.normal);
                mat.SetFloat("_UseNormalMap", 1f);
                mat.EnableKeyword("_NORMALMAP");
                mat.EnableKeyword("_USE_NORMAL_MAP");
            }

            if (maps.roughness != null && mat.HasProperty("_RoughnessMap"))
            {
                mat.SetTexture("_RoughnessMap", maps.roughness);
                mat.EnableKeyword("_USE_ROUGHNESS_MAP");
            }

            if (maps.metallic != null && mat.HasProperty("_MetallicMap"))
                mat.SetTexture("_MetallicMap", maps.metallic);

            if (maps.height != null && mat.HasProperty("_HeightMap"))
                mat.SetTexture("_HeightMap", maps.height);

            if (maps.ao != null && mat.HasProperty("_OcclusionMap"))
                mat.SetTexture("_OcclusionMap", maps.ao);

            EditorUtility.SetDirty(mat);
        }

        // ===== Helpers =====

        private static void ConfigureImporter(string assetPath, bool isLinear, bool isNormalMap)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            if (isNormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = !isLinear;
            }

            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static string EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return assetPath;

            string[] segments = assetPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
            return current;
        }
    }

    /// <summary>
    /// Holds references to saved PBR map textures.
    /// </summary>
    public class AutoMatSavedMaps
    {
        public Texture2D normal;
        public Texture2D roughness;
        public Texture2D metallic;
        public Texture2D height;
        public Texture2D ao;
        public Texture2D curvature;
    }
}
