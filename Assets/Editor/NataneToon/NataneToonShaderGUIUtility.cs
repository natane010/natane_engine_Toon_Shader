using UnityEngine;
using UnityEditor;
using NataneToon.MaterialSystem;

namespace NataneToon.Editor
{
    /// <summary>
    /// Utility class for Natane Toon Shader GUI operations
    /// Provides reusable UI components and helper functions
    /// </summary>
    public static class NataneToonShaderGUIUtility
    {
        private static GUIStyle headerStyle;
        private static GUIStyle boxStyle;

        /// <summary>
        /// Initialize styles
        /// </summary>
        public static void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    padding = new RectOffset(5, 5, 5, 5)
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        /// <summary>
        /// Draw a section header with foldout
        /// </summary>
        public static bool DrawFoldoutHeader(string title, bool foldout)
        {
            InitializeStyles();
            EditorGUILayout.Space(5);
            return EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
        }

        /// <summary>
        /// Draw a property with optional tooltip
        /// </summary>
        public static void DrawProperty(MaterialEditor editor, MaterialProperty prop, string label = null, string tooltip = null)
        {
            GUIContent content;
            if (string.IsNullOrEmpty(label))
            {
                content = new GUIContent(prop.displayName, tooltip ?? "");
            }
            else
            {
                content = new GUIContent(label, tooltip ?? "");
            }

            editor.ShaderProperty(prop, content);
        }

        /// <summary>
        /// Draw a color property with HDR support check
        /// </summary>
        public static void DrawColorProperty(MaterialEditor editor, MaterialProperty prop, string label = null, string tooltip = null, bool hdr = false)
        {
            GUIContent content;
            if (string.IsNullOrEmpty(label))
            {
                content = new GUIContent(prop.displayName, tooltip ?? "");
            }
            else
            {
                content = new GUIContent(label, tooltip ?? "");
            }

            if (hdr)
            {
                editor.ColorProperty(prop, content);
            }
            else
            {
                Color oldColor = prop.colorValue;
                Color newColor = EditorGUILayout.ColorField(content, oldColor);
                if (newColor != oldColor)
                {
                    prop.colorValue = newColor;
                }
            }
        }

        /// <summary>
        /// Draw a toggle with keyword enable/disable
        /// </summary>
        public static bool DrawToggleWithKeyword(Material material, MaterialProperty prop, string keyword, string label = null, string tooltip = null)
        {
            GUIContent content;
            if (string.IsNullOrEmpty(label))
            {
                content = new GUIContent(prop.displayName, tooltip ?? "");
            }
            else
            {
                content = new GUIContent(label, tooltip ?? "");
            }

            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle(content, material.IsKeywordEnabled(keyword));

            if (EditorGUI.EndChangeCheck())
            {
                if (enabled)
                {
                    material.EnableKeyword(keyword);
                    prop.floatValue = 1f;
                }
                else
                {
                    material.DisableKeyword(keyword);
                    prop.floatValue = 0f;
                }
            }

            return enabled;
        }

        /// <summary>
        /// Draw texture property with tiling/offset
        /// </summary>
        public static void DrawTextureProperty(MaterialEditor editor, MaterialProperty texProp, bool showScaleOffset = true)
        {
            editor.TexturePropertySingleLine(new GUIContent(texProp.displayName), texProp);

            if (showScaleOffset && texProp.textureValue != null)
            {
                EditorGUI.indentLevel++;
                editor.TextureScaleOffsetProperty(texProp);
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draw a separator line
        /// </summary>
        public static void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Draw a help box with icon
        /// </summary>
        public static void DrawHelpBox(string message, MessageType type = MessageType.Info)
        {
            EditorGUILayout.HelpBox(message, type);
        }

        /// <summary>
        /// Draw preset and sharing buttons
        /// </summary>
        public static void DrawMaterialActionsToolbar(Material material, MaterialEditor editor)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Material Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // Preset Browser
            if (GUILayout.Button(new GUIContent("Preset Browser", "Open Material Preset Browser"), GUILayout.Height(25)))
            {
                MaterialPresetBrowser.ShowWindow();
            }

            // Create Preset
            if (GUILayout.Button(new GUIContent("Save as Preset", "Create preset from this material"), GUILayout.Height(25)))
            {
                CreatePresetFromMaterial(material);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // Export to File
            if (GUILayout.Button(new GUIContent("Export to File", "Export parameters to file"), GUILayout.Height(25)))
            {
                ExportMaterialToFile(material);
            }

            // Copy to Clipboard
            if (GUILayout.Button(new GUIContent("Copy", "Copy parameters to clipboard"), GUILayout.Height(25)))
            {
                CopyToClipboard(material);
            }

            // Paste from Clipboard
            bool clipboardValid = MaterialParameterShareSystem.IsClipboardValid();
            using (new EditorGUI.DisabledScope(!clipboardValid))
            {
                if (GUILayout.Button(new GUIContent("Paste", "Paste parameters from clipboard"), GUILayout.Height(25)))
                {
                    PasteFromClipboard(material);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Create preset from material
        /// </summary>
        private static void CreatePresetFromMaterial(Material material)
        {
            string defaultName = $"{material.name}_Preset";
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Preset from Material",
                defaultName,
                "asset",
                "Choose where to save the preset");

            if (!string.IsNullOrEmpty(path))
            {
                var preset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
                preset.presetName = System.IO.Path.GetFileNameWithoutExtension(path);
                preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");
                preset.CreateFromMaterial(material);

                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorGUIUtility.PingObject(preset);

                EditorUtility.DisplayDialog(
                    "Preset Created",
                    $"Created preset '{preset.presetName}' from material '{material.name}'\n\n" +
                    "You can now share this preset file with others!",
                    "OK");
            }
        }

        /// <summary>
        /// Export material to file
        /// </summary>
        private static void ExportMaterialToFile(Material material)
        {
            string defaultFolder = MaterialParameterShareSystem.GetDefaultExportFolder();
            string defaultName = $"{material.name}_Parameters";
            string extension = MaterialParameterShareSystem.GetFileExtension();

            string path = EditorUtility.SaveFilePanel(
                "Export Material Parameters",
                defaultFolder,
                defaultName,
                extension.TrimStart('.'));

            if (!string.IsNullOrEmpty(path))
            {
                string result = MaterialParameterShareSystem.ExportToFile(material, path);
                if (!string.IsNullOrEmpty(result))
                {
                    EditorUtility.DisplayDialog(
                        "Export Successful",
                        $"Material parameters exported to:\n{result}\n\n" +
                        "Share this file with others to transfer settings!",
                        "OK");
                }
            }
        }

        /// <summary>
        /// Copy material to clipboard
        /// </summary>
        private static void CopyToClipboard(Material material)
        {
            bool success = MaterialParameterShareSystem.CopyToClipboard(material);
            if (success)
            {
                EditorUtility.DisplayDialog(
                    "Copied to Clipboard",
                    $"Material '{material.name}' parameters copied to clipboard.\n\n" +
                    "You can now paste these parameters to another material or share with others.",
                    "OK");
            }
        }

        /// <summary>
        /// Paste material from clipboard
        /// </summary>
        private static void PasteFromClipboard(Material material)
        {
            var info = MaterialParameterShareSystem.GetClipboardInfo();
            bool proceed = EditorUtility.DisplayDialog(
                "Paste Material Parameters",
                $"Paste parameters from:\n\n" +
                $"Material: {info.materialName}\n" +
                $"Exported by: {info.exportedBy}\n" +
                $"Export date: {info.exportDate}\n" +
                $"Notes: {info.notes}\n\n" +
                $"This will overwrite current settings of '{material.name}'",
                "Paste",
                "Cancel");

            if (proceed)
            {
                Undo.RecordObject(material, "Paste Material Parameters");
                bool success = MaterialParameterShareSystem.PasteFromClipboard(material);

                if (success)
                {
                    EditorUtility.SetDirty(material);
                    EditorUtility.DisplayDialog(
                        "Paste Successful",
                        $"Material parameters pasted to '{material.name}'",
                        "OK");
                }
            }
        }

        /// <summary>
        /// Draw a property with inline toggle
        /// </summary>
        public static void DrawPropertyWithToggle(MaterialEditor editor, MaterialProperty toggleProp, MaterialProperty valueProp, string label, string tooltip = null)
        {
            EditorGUILayout.BeginHorizontal();

            // Toggle
            bool enabled = toggleProp.floatValue > 0.5f;
            enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(15));
            toggleProp.floatValue = enabled ? 1f : 0f;

            // Property
            using (new EditorGUI.DisabledScope(!enabled))
            {
                DrawProperty(editor, valueProp, label, tooltip);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Get MaterialProperty by name safely
        /// </summary>
        public static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool optional = false)
        {
            if (optional)
            {
                try
                {
                    return ShaderGUI.FindProperty(propertyName, properties);
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return ShaderGUI.FindProperty(propertyName, properties);
            }
        }

        /// <summary>
        /// Validate material shader compatibility
        /// </summary>
        public static bool ValidateShaderCompatibility(Material material)
        {
            if (material == null || material.shader == null)
            {
                return false;
            }

            string shaderName = material.shader.name;
            return shaderName.Contains("Natane") && shaderName.Contains("Toon");
        }

        /// <summary>
        /// Draw performance indicator
        /// </summary>
        public static void DrawPerformanceIndicator(Material material)
        {
            int activeFeatures = CountActiveFeatures(material);
            string rating = GetPerformanceRating(activeFeatures);
            Color ratingColor = GetRatingColor(rating);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Performance Rating:", GUILayout.Width(130));

            Color oldColor = GUI.color;
            GUI.color = ratingColor;
            EditorGUILayout.LabelField($"{rating} ({activeFeatures} features)", EditorStyles.boldLabel);
            GUI.color = oldColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static int CountActiveFeatures(Material material)
        {
            int count = 0;

            string[] keywords = new[]
            {
                "_SPECULAR", "_RIM", "_SSS", "_MATCAP", "_OUTLINE", "_EMISSION",
                "_EMISSION_ANIMATION", "_DISSOLVE", "_HUE_SHIFT",
                "_REFLECTION", "_ENV_RIM", "_PARALLAX", "_REFRACTION"
            };

            foreach (string keyword in keywords)
            {
                if (material.IsKeywordEnabled(keyword))
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetPerformanceRating(int featureCount)
        {
            if (featureCount <= 3) return "Excellent (A)";
            if (featureCount <= 6) return "Good (B)";
            if (featureCount <= 9) return "Fair (C)";
            return "Heavy (D)";
        }

        private static Color GetRatingColor(string rating)
        {
            if (rating.StartsWith("Excellent")) return Color.green;
            if (rating.StartsWith("Good")) return Color.cyan;
            if (rating.StartsWith("Fair")) return Color.yellow;
            return new Color(1f, 0.5f, 0f); // Orange
        }
    }
}
