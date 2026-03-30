using UnityEngine;
using UnityEditor;
using System.IO;

namespace NataneToon.Editor
{
    /// <summary>
    /// Export material textures as layers to the standalone Texture Studio.
    /// マテリアルのテクスチャをレイヤーとしてスタンドアロン Texture Studio にエクスポートする
    /// </summary>
    internal static class TextureStudioMaterialExporter
    {
        /// <summary>
        /// Send all texture properties of a material as layers to the studio.
        /// マテリアルの全テクスチャプロパティをレイヤーとしてスタジオに送信する
        /// </summary>
        public static void SendMaterialTextures(Material material)
        {
            if (material == null) return;
            if (!TextureStudioLauncher.IsRunning)
            {
                TextureStudioLauncher.Launch();
                // Wait for connection then send
                EditorApplication.delayCall += () => EditorApplication.delayCall += () => SendMaterialTexturesInternal(material);
                return;
            }
            SendMaterialTexturesInternal(material);
        }

        private static void SendMaterialTexturesInternal(Material material)
        {
            var shader = material.shader;
            int propCount = ShaderUtil.GetPropertyCount(shader);

            bool first = true;
            for (int i = 0; i < propCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propName = ShaderUtil.GetPropertyName(shader, i);
                Texture tex = material.GetTexture(propName);
                if (tex == null || !(tex is Texture2D tex2d)) continue;

                string texPath = AssetDatabase.GetAssetPath(tex2d);
                if (string.IsNullOrEmpty(texPath)) continue;

                string fullPath = Path.GetFullPath(texPath);
                string displayName = ShaderUtil.GetPropertyDescription(shader, i);

                if (first)
                {
                    // First texture opens as the base
                    TextureStudioBridge.SendOpenTexture(fullPath, propName);
                    first = false;
                }
                else
                {
                    // Subsequent textures imported as layers
                    TextureStudioBridge.SendImportLayer(fullPath, $"{displayName} ({propName})", 1f);
                }
            }
        }

        /// <summary>
        /// Send a single texture property as a new layer.
        /// 単一のテクスチャプロパティを新しいレイヤーとして送信する
        /// </summary>
        public static void SendSingleTexture(Material material, string propertyName)
        {
            if (material == null) return;
            Texture tex = material.GetTexture(propertyName);
            if (tex == null || !(tex is Texture2D tex2d)) return;

            string texPath = AssetDatabase.GetAssetPath(tex2d);
            if (string.IsNullOrEmpty(texPath)) return;

            string fullPath = Path.GetFullPath(texPath);

            if (!TextureStudioLauncher.IsRunning)
            {
                TextureStudioLauncher.LaunchWithTexture(fullPath, propertyName);
            }
            else
            {
                TextureStudioBridge.SendOpenTexture(fullPath, propertyName);
            }
        }
    }
}
