using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Launches Texture Studio from material inspector with pre-configured settings.
    /// マテリアルインスペクタからテクスチャスタジオを起動するランチャー
    /// </summary>
    public static class TextureStudioLauncher
    {
        /// <summary>
        /// Open Texture Studio for editing a specific texture property.
        /// 特定のテクスチャプロパティの編集用にテクスチャスタジオを開く
        /// </summary>
        public static void OpenForProperty(Material material, string propertyName, Texture2D currentTexture)
        {
            if (material == null) return;

            // [TEMPORARY] Password gate - remove at official release
            // [一時的] パスワードゲート - 正式リリース時に削除
            if (!TextureStudioPasswordGate.Verify()) return;

            // Get or open the texture studio window
            var window = EditorWindow.GetWindow<UVTextureGenerator>(false,
                L("テクスチャスタジオ", "Texture Studio"));

            // Configure for the target material/property
            window.CurrentTargetMaterial = material;
            window.CurrentPropertyName = propertyName;

            // Find property index
            string[] props = UVTextureGenerator.MaskPropertyNames;
            int propIdx = -1;
            for (int i = 0; i < props.Length; i++)
            {
                if (props[i] == propertyName) { propIdx = i; break; }
            }

            if (propIdx >= 0)
                window.CurrentPropertyIndex = propIdx;

            // Detect texture type and set appropriate mode
            var texType = TextureTypeDetector.Detect(propertyName);

            // Import existing texture if available
            if (currentTexture != null)
            {
                // Send message to import texture (the window will handle it)
                window.ImportTextureForEditing(currentTexture, propertyName);
            }

            window.Show();
            window.Focus();
        }

        /// <summary>
        /// Draw an "Edit" button next to a texture property in ShaderGUI.
        /// ShaderGUIでテクスチャプロパティの横に「Edit」ボタンを描画
        /// </summary>
        /// <returns>True if the button was clicked.</returns>
        public static bool DrawEditButton(MaterialEditor editor, MaterialProperty texProperty)
        {
            if (editor == null || texProperty == null) return false;
            if (texProperty.type != MaterialProperty.PropType.Texture) return false;

            Material mat = editor.target as Material;
            if (mat == null) return false;

            // Detect type for icon/tooltip
            var texType = TextureTypeDetector.Detect(texProperty.name);
            string typeName = TextureTypeDetector.GetDisplayName(texType);

            string tooltip = L(
                $"テクスチャスタジオで編集 ({typeName})",
                $"Edit in Texture Studio ({typeName})");

            // Small edit button
            if (GUILayout.Button(new GUIContent("Edit", tooltip),
                EditorStyles.miniButton, GUILayout.Width(36), GUILayout.Height(18)))
            {
                Texture2D currentTex = texProperty.textureValue as Texture2D;
                OpenForProperty(mat, texProperty.name, currentTex);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Draw a compact edit button (icon only) for inline use.
        /// インライン使用のためのコンパクトなEditボタンを描画
        /// </summary>
        public static bool DrawCompactEditButton(Material mat, string propertyName)
        {
            if (mat == null || !mat.HasProperty(propertyName)) return false;

            if (GUILayout.Button(new GUIContent("E",
                L("テクスチャスタジオで編集", "Edit in Texture Studio")),
                EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(16)))
            {
                Texture2D tex = mat.GetTexture(propertyName) as Texture2D;
                OpenForProperty(mat, propertyName, tex);
                return true;
            }
            return false;
        }
    }
}
