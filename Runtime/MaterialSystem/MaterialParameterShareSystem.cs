using UnityEngine;
using System;
using System.IO;

namespace NataneToon.MaterialSystem
{
    /// <summary>
    /// System for sharing material parameters via JSON export/import
    /// Supports file-based sharing and clipboard sharing
    /// </summary>
    public static class MaterialParameterShareSystem
    {
        private const string FILE_EXTENSION = ".ntmaterial";
        private const string DEFAULT_FOLDER = "MaterialParameters";

        /// <summary>
        /// Shareable material data wrapper with metadata
        /// </summary>
        [Serializable]
        public class ShareableData
        {
            public string shaderName = "Natane/Toon Shader";
            public string materialName = "";
            public string exportDate = "";
            public string exportedBy = "";
            public string notes = "";
            public MaterialParameterData parameters;

            public ShareableData()
            {
                exportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                exportedBy = Environment.UserName;
                parameters = new MaterialParameterData();
            }
        }

        /// <summary>
        /// Export material parameters to JSON string
        /// </summary>
        public static string ExportToJSON(Material material, string notes = "")
        {
            if (material == null)
            {
                Debug.LogError("[MaterialParameterShareSystem] Material is null");
                return null;
            }

            ShareableData data = new ShareableData
            {
                shaderName = material.shader.name,
                materialName = material.name,
                notes = notes
            };

            // Create a temporary preset to extract parameters
            var tempPreset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
            tempPreset.CreateFromMaterial(material);
            data.parameters = tempPreset.parameters;
            ScriptableObject.DestroyImmediate(tempPreset);

            string json = JsonUtility.ToJson(data, true);
            return json;
        }

        /// <summary>
        /// Import material parameters from JSON string
        /// </summary>
        public static bool ImportFromJSON(string json, Material targetMaterial)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[MaterialParameterShareSystem] JSON is null or empty");
                return false;
            }

            if (targetMaterial == null)
            {
                Debug.LogError("[MaterialParameterShareSystem] Target material is null");
                return false;
            }

            try
            {
                ShareableData data = JsonUtility.FromJson<ShareableData>(json);

                // Create temporary preset and apply
                var tempPreset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
                tempPreset.parameters = data.parameters;
                tempPreset.ApplyToMaterial(targetMaterial);
                ScriptableObject.DestroyImmediate(tempPreset);

                Debug.Log($"[MaterialParameterShareSystem] Successfully imported parameters to '{targetMaterial.name}'\n" +
                         $"Original material: {data.materialName}\n" +
                         $"Exported by: {data.exportedBy}\n" +
                         $"Export date: {data.exportDate}\n" +
                         $"Notes: {data.notes}");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MaterialParameterShareSystem] Failed to import JSON: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export material parameters to file
        /// </summary>
        public static string ExportToFile(Material material, string filePath = null, string notes = "")
        {
            string json = ExportToJSON(material, notes);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            // Generate default file path if not provided
            if (string.IsNullOrEmpty(filePath))
            {
                string folder = Path.Combine(Application.dataPath, DEFAULT_FOLDER);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = $"{material.name}_{DateTime.Now:yyyyMMdd_HHmmss}{FILE_EXTENSION}";
                filePath = Path.Combine(folder, fileName);
            }

            try
            {
                File.WriteAllText(filePath, json);
                Debug.Log($"[MaterialParameterShareSystem] Exported to file: {filePath}");
                return filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MaterialParameterShareSystem] Failed to export file: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Import material parameters from file
        /// </summary>
        public static bool ImportFromFile(string filePath, Material targetMaterial)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[MaterialParameterShareSystem] File not found: {filePath}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                bool success = ImportFromJSON(json, targetMaterial);

                if (success)
                {
                    Debug.Log($"[MaterialParameterShareSystem] Imported from file: {filePath}");
                }

                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MaterialParameterShareSystem] Failed to import file: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Copy material parameters to clipboard
        /// </summary>
        public static bool CopyToClipboard(Material material, string notes = "")
        {
            string json = ExportToJSON(material, notes);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            GUIUtility.systemCopyBuffer = json;
            Debug.Log($"[MaterialParameterShareSystem] Copied '{material.name}' parameters to clipboard");
            return true;
        }

        /// <summary>
        /// Paste material parameters from clipboard
        /// </summary>
        public static bool PasteFromClipboard(Material targetMaterial)
        {
            string json = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[MaterialParameterShareSystem] Clipboard is empty");
                return false;
            }

            return ImportFromJSON(json, targetMaterial);
        }

        /// <summary>
        /// Validate if clipboard contains valid material data
        /// </summary>
        public static bool IsClipboardValid()
        {
            string json = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                ShareableData data = JsonUtility.FromJson<ShareableData>(json);
                return data != null && data.parameters != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get info from clipboard data without applying
        /// </summary>
        public static ShareableData GetClipboardInfo()
        {
            string json = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<ShareableData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get default export folder path
        /// </summary>
        public static string GetDefaultExportFolder()
        {
            string folder = Path.Combine(Application.dataPath, DEFAULT_FOLDER);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return folder;
        }

        /// <summary>
        /// Get file extension for material parameter files
        /// </summary>
        public static string GetFileExtension()
        {
            return FILE_EXTENSION;
        }
    }
}
