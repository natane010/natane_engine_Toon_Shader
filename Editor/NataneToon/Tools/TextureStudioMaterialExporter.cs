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
            }
            // Wait for connection with polling (pipe connection takes time)
            WaitForConnectionAndSend(material, 0);
        }

        private static void WaitForConnectionAndSend(Material material, int attempt)
        {
            if (TextureStudioBridge.IsConnected)
            {
                SendMaterialTexturesInternal(material);
                return;
            }
            if (attempt >= 30) // ~15 seconds max wait
            {
                Debug.LogWarning("[TextureStudioMaterialExporter] Texture Studio への接続がタイムアウトしました。");
                return;
            }
            // Retry in 0.5 seconds
            EditorApplication.delayCall += () =>
            {
                // delayCall fires once per frame (~16ms). Schedule next check.
                double waitUntil = EditorApplication.timeSinceStartup + 0.5;
                EditorApplication.update += CheckConnection;

                void CheckConnection()
                {
                    if (EditorApplication.timeSinceStartup < waitUntil) return;
                    EditorApplication.update -= CheckConnection;
                    WaitForConnectionAndSend(material, attempt + 1);
                }
            };
        }

        private static void SendMaterialTexturesInternal(Material material)
        {
            var shader = material.shader;
            int propCount = ShaderUtil.GetPropertyCount(shader);

            // Collect texture entries first
            var textures = new System.Collections.Generic.List<(string path, string propName, string displayName)>();
            for (int i = 0; i < propCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propName = ShaderUtil.GetPropertyName(shader, i);
                Texture tex = material.GetTexture(propName);
                if (tex == null || !(tex is Texture2D tex2d)) continue;

                string texPath = AssetDatabase.GetAssetPath(tex2d);
                if (string.IsNullOrEmpty(texPath)) continue;

                textures.Add((Path.GetFullPath(texPath), propName, ShaderUtil.GetPropertyDescription(shader, i)));
            }

            if (textures.Count == 0)
            {
                Debug.Log("[TextureStudioMaterialExporter] マテリアルにテクスチャがありません。");
                return;
            }

            // Send first texture as base
            TextureStudioBridge.SendOpenTexture(textures[0].path, textures[0].propName);
            Debug.Log($"[TextureStudioMaterialExporter] Base: {textures[0].propName} → {textures[0].path}");

            // Send remaining textures as layers with staggered delay
            for (int idx = 1; idx < textures.Count; idx++)
            {
                int capturedIdx = idx;
                double sendTime = EditorApplication.timeSinceStartup + idx * 0.5; // 500ms interval

                EditorApplication.update += SendDelayed;
                void SendDelayed()
                {
                    if (EditorApplication.timeSinceStartup < sendTime) return;
                    EditorApplication.update -= SendDelayed;

                    var t = textures[capturedIdx];
                    TextureStudioBridge.SendImportLayer(t.path, $"{t.displayName} ({t.propName})", 1f);
                    Debug.Log($"[TextureStudioMaterialExporter] Layer: {t.propName} → {t.path}");
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
