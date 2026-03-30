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

        /// <summary>
        /// Draw a button to send all material textures as layers to the studio.
        /// マテリアルの全テクスチャをレイヤーとしてスタジオに送信するボタンを描画する
        /// </summary>
        public static void DrawMaterialLayersButton(Material material)
        {
            if (material == null) return;
            if (GUILayout.Button("\ud83d\udce6 \u5168\u30c6\u30af\u30b9\u30c1\u30e3\u3092\u30ec\u30a4\u30e4\u30fc\u5316", GUILayout.Height(22)))
            {
                TextureStudioMaterialExporter.SendMaterialTextures(material);
            }
        }

        /// <summary>
        /// Draw a toggle button for live preview mode.
        /// ライブプレビューモードのトグルボタンを描画する
        /// </summary>
        public static void DrawLivePreviewToggle(Material material, string propertyName)
        {
            if (material == null) return;

            bool isActive = TextureStudioBridge.IsLivePreviewEnabled;
            string label = isActive ? "\ud83d\udd34 \u30e9\u30a4\u30d6\u30d7\u30ec\u30d3\u30e5\u30fc OFF" : "\ud83d\udfe2 \u30e9\u30a4\u30d6\u30d7\u30ec\u30d3\u30e5\u30fc ON";

            if (GUILayout.Button(label, GUILayout.Height(22)))
            {
                if (isActive)
                {
                    TextureStudioBridge.DisableLivePreview();
                }
                else
                {
                    TextureStudioBridge.EnableLivePreview(material, propertyName);
                }
            }
        }
    }
}
