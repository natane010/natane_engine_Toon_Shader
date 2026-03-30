using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Helper to add "Edit in Texture Studio" buttons next to texture properties in ShaderGUI.
    /// ShaderGUI でテクスチャプロパティの横に「Texture Studio で編集」ボタンを追加するヘルパー
    /// Call DrawEditButton() from NataneToonShaderGUI when drawing texture properties.
    /// </summary>
    internal static class TextureStudioInspectorButton
    {
        /// <summary>
        /// Draw an "Edit" button next to a texture property.
        /// Returns true if the button was clicked.
        /// </summary>
        public static bool DrawEditButton(MaterialProperty prop, string displayName = null)
        {
            if (prop == null || prop.type != MaterialProperty.PropType.Texture)
                return false;

            Texture2D tex = prop.textureValue as Texture2D;
            if (tex == null) return false;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("\u270f \u7de8\u96c6", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    string path = AssetDatabase.GetAssetPath(tex);
                    if (!string.IsNullOrEmpty(path))
                    {
                        TextureStudioLauncher.LaunchWithTexture(path, prop.name);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Draw a standalone "Open Texture Studio" button.
        /// 「Texture Studio を開く」スタンドアロンボタンを描画
        /// </summary>
        public static void DrawLaunchButton()
        {
            if (GUILayout.Button("\ud83c\udfa8 Texture Studio \u3092\u958b\u304f", GUILayout.Height(24)))
            {
                TextureStudioLauncher.Launch();
            }
        }
    }
}
