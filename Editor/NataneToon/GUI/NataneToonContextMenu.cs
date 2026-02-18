using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// Context menu integration for Natane Toon Shader
    /// Natane Toon Shaderのコンテキストメニュー統合
    /// </summary>
    public static class NataneToonContextMenu
    {
        /// <summary>
        /// Open an EditorWindow by its full type name (cross-assembly)
        /// アセンブリを跨いでEditorWindowを型名で開く
        /// </summary>
        private static void OpenEditorWindow(string typeName, string title)
        {
            Type windowType = null;

            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        windowType = types.FirstOrDefault(t =>
                            (t.FullName == typeName || t.Name == typeName) &&
                            typeof(EditorWindow).IsAssignableFrom(t));

                        if (windowType != null) break;
                    }
                    catch (System.Reflection.ReflectionTypeLoadException ex)
                    {
                        // Some assemblies may have types that fail to load
                        windowType = ex.Types
                            .Where(t => t != null)
                            .FirstOrDefault(t =>
                                (t.FullName == typeName || t.Name == typeName) &&
                                typeof(EditorWindow).IsAssignableFrom(t));

                        if (windowType != null) break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NataneToon] Error searching for window type '{typeName}': {ex.Message}");
            }

            if (windowType != null)
            {
                var window = EditorWindow.GetWindow(windowType, false, title);
                window.Show();
            }
            else
            {
                Debug.LogWarning($"[NataneToon] Window type not found: {typeName}");

                bool openDiagnostics = EditorUtility.DisplayDialog(
                    "ウィンドウが見つかりません Window Not Found",
                    $"型 '{typeName}' が見つかりませんでした。\n" +
                    $"Type '{typeName}' was not found.\n\n" +
                    "アセンブリが正しくロードされているか確認してください。\n" +
                    "診断ツールで詳細を確認できます。\n\n" +
                    "Please verify that the assembly is correctly loaded.\n" +
                    "You can check details in the Diagnostics tool.",
                    "診断ツールを開く Open Diagnostics",
                    "閉じる Close");

                if (openDiagnostics)
                {
                    EditorApplication.ExecuteMenuItem("Tools/Natane/診断 Diagnostics");
                }
            }
        }

        // マテリアル右クリックメニュー Material Right-Click Menu
        [MenuItem("Assets/Natane/マテリアル検証 Validate Material", false, 100)]
        private static void ValidateMaterialContext()
        {
            Material material = Selection.activeObject as Material;
            if (material != null && IsNataneToonMaterial(material))
            {
                OpenEditorWindow("NataneToon.Editor.MaterialValidator", "マテリアル検証 Material Validator");
            }
        }

        [MenuItem("Assets/Natane/マテリアル検証 Validate Material", true)]
        private static bool ValidateMaterialContextValidation()
        {
            Material material = Selection.activeObject as Material;
            return material != null && IsNataneToonMaterial(material);
        }

        [MenuItem("Assets/Natane/プリセット適用 Apply Preset", false, 101)]
        private static void ApplyPresetContext()
        {
            Material material = Selection.activeObject as Material;
            if (material != null && IsNataneToonMaterial(material))
            {
                var window = EditorWindow.GetWindow<MaterialPresetBrowser>("Material Preset Browser");
                window.Show();
            }
        }

        [MenuItem("Assets/Natane/プリセット適用 Apply Preset", true)]
        private static bool ApplyPresetContextValidation()
        {
            Material material = Selection.activeObject as Material;
            return material != null && IsNataneToonMaterial(material);
        }

        // GameObject右クリックメニュー GameObject Right-Click Menu
        [MenuItem("GameObject/Natane/マテリアルを検証 Validate Materials", false, 100)]
        private static void ValidateGameObjectMaterialsContext()
        {
            GameObject obj = Selection.activeGameObject;
            if (obj != null)
            {
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    OpenEditorWindow("NataneToon.Editor.MaterialValidator", "マテリアル検証 Material Validator");
                }
            }
        }

        [MenuItem("GameObject/Natane/マテリアルを検証 Validate Materials", true)]
        private static bool ValidateGameObjectMaterialsContextValidation()
        {
            GameObject obj = Selection.activeGameObject;
            if (obj == null) return false;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return false;

            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && IsNataneToonMaterial(mat))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if material is using Natane Toon Shader
        /// マテリアルがNatane Toon Shaderを使用しているか確認
        /// </summary>
        private static bool IsNataneToonMaterial(Material mat)
        {
            if (mat == null || mat.shader == null) return false;
            return mat.shader.name.Contains("Natane") && mat.shader.name.Contains("Toon");
        }
    }
}
