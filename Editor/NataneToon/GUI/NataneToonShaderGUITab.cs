using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Base class for NataneToonShaderGUI tab implementations
    /// NataneToonShaderGUIタブ実装の基底クラス
    /// Provides common drawing methods and property access
    /// </summary>
    public abstract class NataneToonShaderGUITab
    {
        // ===== CONSTANTS =====
        /// <summary>Threshold for float comparisons (e.g., checking if a toggle is enabled)</summary>
        protected const float FLOAT_COMPARISON_THRESHOLD = 0.5f;

        /// <summary>Minimum value to consider a parameter active (avoid floating point issues)</summary>
        protected const float MIN_PARAMETER_VALUE = 0.001f;

        // ===== REFERENCES =====
        /// <summary>All shader properties for the current material</summary>
        protected MaterialProperty[] properties;

        /// <summary>Unity's material editor instance</summary>
        protected MaterialEditor materialEditor;

        /// <summary>Target material being edited</summary>
        protected Material targetMaterial;

        /// <summary>
        /// Initialize the tab with editor context
        /// </summary>
        public virtual void Initialize(MaterialEditor materialEditor, MaterialProperty[] properties, Material targetMaterial)
        {
            this.materialEditor = materialEditor;
            this.properties = properties;
            this.targetMaterial = targetMaterial;
        }

        /// <summary>
        /// Draw the tab content
        /// Must be implemented by derived classes
        /// </summary>
        public abstract void Draw();

        /// <summary>
        /// Load foldout states from EditorPrefs
        /// Override to load tab-specific foldout states
        /// </summary>
        public virtual void LoadFoldoutStates() { }

        /// <summary>
        /// Save foldout states to EditorPrefs
        /// Override to save tab-specific foldout states
        /// </summary>
        public virtual void SaveFoldoutStates() { }

        // ===== COMMON DRAWING METHODS =====

        /// <summary>
        /// Find a material property by name
        /// マテリアルプロパティを名前で検索
        /// </summary>
        protected MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory = true)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i] != null && properties[i].name == propertyName)
                {
                    return properties[i];
                }
            }

            if (propertyIsMandatory)
            {
                throw new System.ArgumentException($"Could not find MaterialProperty: '{propertyName}'");
            }

            return null;
        }

        /// <summary>
        /// Draw a simple property field
        /// </summary>
        protected void DrawProperty(string propertyName, string label)
        {
            MaterialProperty property = FindProperty(propertyName, properties, false);
            if (property != null)
            {
                materialEditor.ShaderProperty(property, label);
            }
        }

        /// <summary>
        /// Draw a toggle with keyword support and visual indicator
        /// </summary>
        protected bool DrawToggle(string keyword, string propertyName, string label)
        {
            MaterialProperty property = FindProperty(propertyName, properties, false);
            if (property == null)
            {
                return false;
            }

            EditorGUI.BeginChangeCheck();
            bool enabled = property.floatValue > FLOAT_COMPARISON_THRESHOLD;

            // Create a horizontal layout for toggle with visual indicator
            EditorGUILayout.BeginHorizontal();

            // Draw toggle
            enabled = EditorGUILayout.Toggle(label, enabled);

            // Visual indicator
            string statusIcon = enabled ? "✓" : "✗";
            Color statusColor = enabled ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.6f, 0.6f, 0.6f);

            var oldColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(statusIcon, GUILayout.Width(20));
            GUI.color = oldColor;

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = enabled ? 1.0f : 0.0f;

                // Set shader keyword
                if (enabled)
                    targetMaterial.EnableKeyword(keyword);
                else
                    targetMaterial.DisableKeyword(keyword);
            }

            return enabled;
        }

        /// <summary>
        /// Draw help toggle button and help box
        /// Returns true if help is shown
        /// </summary>
        protected bool DrawHelpToggle(string sectionKey, string helpText, MessageType messageType = MessageType.Info)
        {
            // Get help state for this section
            string prefsKey = NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "Help_" + sectionKey);
            bool showHelp = EditorPrefs.GetBool(prefsKey, false);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string buttonLabel = showHelp ? "ヘルプを非表示 Hide Help" : "ヘルプを表示 Show Help";
            if (GUILayout.Button(buttonLabel, EditorStyles.miniButton, GUILayout.Width(140)))
            {
                showHelp = !showHelp;
                EditorPrefs.SetBool(prefsKey, showHelp);
            }

            EditorGUILayout.EndHorizontal();

            if (showHelp)
            {
                EditorGUILayout.HelpBox(helpText, messageType);
            }

            return showHelp;
        }

        /// <summary>
        /// Draw a category header with title and description
        /// </summary>
        protected void DrawCategoryHeader(string title, string description)
        {
            EditorGUILayout.Space(5);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.7f, 0.9f, 1.0f) }
            };

            EditorGUILayout.LabelField(title, titleStyle);

            if (!string.IsNullOrEmpty(description))
            {
                GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true
                };
                EditorGUILayout.LabelField(description, descStyle);
            }

            EditorGUILayout.Space(3);
        }

        /// <summary>
        /// Safe section drawing with exception handling
        /// </summary>
        protected void SafeDrawSection(System.Action drawAction, string sectionName)
        {
            try
            {
                drawAction?.Invoke();
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"{sectionName}セクションの描画中にエラーが発生しました: {e.Message}", MessageType.Warning);
                UnityEngine.Debug.LogWarning($"[NataneToonShaderGUI] Error drawing {sectionName} section: {e.Message}");
            }
        }

        /// <summary>
        /// Draw a blend parameter with linear/exponential curve options
        /// </summary>
        protected void DrawBlendParameter(string propertyName, string label, string helpText)
        {
            DrawProperty(propertyName, label);
            if (!string.IsNullOrEmpty(helpText))
            {
                EditorGUILayout.HelpBox(helpText, MessageType.Info);
            }
        }
    }
}
