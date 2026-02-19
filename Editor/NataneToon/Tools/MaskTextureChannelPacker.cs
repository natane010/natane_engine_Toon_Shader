using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    // ================================================================
    // Channel Packing Configuration
    // ================================================================

    internal class ChannelPackingConfig
    {
        public Texture2D redSource;
        public Texture2D greenSource;
        public Texture2D blueSource;
        public Texture2D alphaSource;

        public enum SourceChannel { R, G, B, A, Luminance }
        public SourceChannel redFrom = SourceChannel.R;
        public SourceChannel greenFrom = SourceChannel.R;
        public SourceChannel blueFrom = SourceChannel.R;
        public SourceChannel alphaFrom = SourceChannel.R;
    }

    // ================================================================
    // MaskTextureChannelPacker
    // チャンネルパッキングツール - 最大4枚のグレースケールマスクをRGBAチャンネルに結合
    // ================================================================

    internal static class MaskTextureChannelPacker
    {
        private static ChannelPackingConfig config = new ChannelPackingConfig();
        private static int outputSize = 1024;
        private static bool foldoutPack = true;
        private static bool foldoutUnpack = true;
        private static Texture2D unpackSource;

        /// <summary>
        /// Pack up to 4 source textures into a single RGBA texture.
        /// 最大4枚のソーステクスチャを1枚のRGBAテクスチャにパック
        /// </summary>
        public static Texture2D Pack(ChannelPackingConfig cfg, int size)
        {
            Color[] redPixels = ReadChannel(cfg.redSource, cfg.redFrom, size);
            Color[] greenPixels = ReadChannel(cfg.greenSource, cfg.greenFrom, size);
            Color[] bluePixels = ReadChannel(cfg.blueSource, cfg.blueFrom, size);
            Color[] alphaPixels = ReadChannel(cfg.alphaSource, cfg.alphaFrom, size);

            int totalPixels = size * size;
            Color[] output = new Color[totalPixels];

            for (int i = 0; i < totalPixels; i++)
            {
                output[i] = new Color(
                    redPixels != null ? redPixels[i].r : 0f,
                    greenPixels != null ? greenPixels[i].r : 0f,
                    bluePixels != null ? bluePixels[i].r : 0f,
                    alphaPixels != null ? alphaPixels[i].r : 1f
                );
            }

            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            result.SetPixels(output);
            result.Apply();
            return result;
        }

        /// <summary>
        /// Unpack an RGBA texture into 4 separate grayscale textures.
        /// RGBAテクスチャを4枚のグレースケールテクスチャに分割
        /// </summary>
        public static Texture2D[] Unpack(Texture2D source, int size)
        {
            if (source == null) return null;

            Texture2D readable = MakeReadable(source, size);
            Color[] pixels = readable.GetPixels();

            int totalPixels = size * size;
            Texture2D[] results = new Texture2D[4];

            for (int ch = 0; ch < 4; ch++)
            {
                Color[] channelPixels = new Color[totalPixels];
                for (int i = 0; i < totalPixels; i++)
                {
                    float v;
                    switch (ch)
                    {
                        case 0: v = pixels[i].r; break;
                        case 1: v = pixels[i].g; break;
                        case 2: v = pixels[i].b; break;
                        case 3: v = pixels[i].a; break;
                        default: v = 0f; break;
                    }
                    channelPixels[i] = new Color(v, v, v, 1f);
                }

                results[ch] = new Texture2D(size, size, TextureFormat.RGB24, false);
                results[ch].SetPixels(channelPixels);
                results[ch].Apply();
            }

            if (readable != source)
                Object.DestroyImmediate(readable);

            return results;
        }

        /// <summary>
        /// Draw the channel packing UI.
        /// チャンネルパッキングUIを描画
        /// </summary>
        public static void DrawChannelPackUI()
        {
            // --- Pack Section ---
            foldoutPack = NataneToonShaderGUIUtility.DrawFoldoutHeader(
                "チャンネルパッキング Channel Packing", foldoutPack);

            if (foldoutPack)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("出力サイズ Output Size", EditorStyles.boldLabel);
                outputSize = EditorGUILayout.IntPopup("サイズ", outputSize,
                    new[] { "256", "512", "1024", "2048", "4096" },
                    new[] { 256, 512, 1024, 2048, 4096 });

                EditorGUILayout.Space(5);
                NataneToonShaderGUIUtility.DrawSeparator();

                // Red channel
                DrawChannelRow("R (赤)", ref config.redSource, ref config.redFrom,
                    new Color(1f, 0.3f, 0.3f, 0.15f));

                // Green channel
                DrawChannelRow("G (緑)", ref config.greenSource, ref config.greenFrom,
                    new Color(0.3f, 1f, 0.3f, 0.15f));

                // Blue channel
                DrawChannelRow("B (青)", ref config.blueSource, ref config.blueFrom,
                    new Color(0.3f, 0.3f, 1f, 0.15f));

                // Alpha channel
                DrawChannelRow("A (アルファ)", ref config.alphaSource, ref config.alphaFrom,
                    new Color(1f, 1f, 1f, 0.1f));

                EditorGUILayout.Space(10);

                bool hasAnySource = config.redSource != null || config.greenSource != null ||
                                    config.blueSource != null || config.alphaSource != null;

                EditorGUI.BeginDisabledGroup(!hasAnySource);
                if (GUILayout.Button("パック Pack Channels", GUILayout.Height(28)))
                {
                    Texture2D packed = Pack(config, outputSize);
                    SavePackedTexture(packed, "ChannelPacked");
                    Object.DestroyImmediate(packed);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // --- Unpack Section ---
            foldoutUnpack = NataneToonShaderGUIUtility.DrawFoldoutHeader(
                "チャンネルアンパック Channel Unpack", foldoutUnpack);

            if (foldoutUnpack)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                unpackSource = (Texture2D)EditorGUILayout.ObjectField(
                    "ソーステクスチャ Source", unpackSource, typeof(Texture2D), false);

                EditorGUILayout.Space(5);

                EditorGUI.BeginDisabledGroup(unpackSource == null);
                if (GUILayout.Button("アンパック Unpack to R/G/B/A", GUILayout.Height(28)))
                {
                    int size = Mathf.Max(unpackSource.width, unpackSource.height);
                    Texture2D[] channels = Unpack(unpackSource, size);
                    if (channels != null)
                    {
                        string[] suffixes = { "_R", "_G", "_B", "_A" };
                        string baseName = unpackSource.name;

                        for (int i = 0; i < 4; i++)
                        {
                            SavePackedTexture(channels[i], baseName + suffixes[i]);
                            Object.DestroyImmediate(channels[i]);
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();
            }
        }

        // ================================================================
        // Private Helpers
        // ================================================================

        private static void DrawChannelRow(string label, ref Texture2D source,
            ref ChannelPackingConfig.SourceChannel channel, Color bgColor)
        {
            Rect rowRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(rowRect, bgColor);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(100));
            source = (Texture2D)EditorGUILayout.ObjectField(source, typeof(Texture2D), false);
            channel = (ChannelPackingConfig.SourceChannel)EditorGUILayout.EnumPopup(channel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        /// <summary>
        /// Read a specific channel from a texture, resizing to target size.
        /// テクスチャから指定チャンネルを読み取り、ターゲットサイズにリサイズ
        /// </summary>
        private static Color[] ReadChannel(Texture2D source, ChannelPackingConfig.SourceChannel channel, int targetSize)
        {
            if (source == null) return null;

            Texture2D readable = MakeReadable(source, targetSize);
            Color[] sourcePixels = readable.GetPixels();
            int totalPixels = targetSize * targetSize;
            Color[] result = new Color[totalPixels];

            for (int i = 0; i < totalPixels; i++)
            {
                float v;
                switch (channel)
                {
                    case ChannelPackingConfig.SourceChannel.R:
                        v = sourcePixels[i].r;
                        break;
                    case ChannelPackingConfig.SourceChannel.G:
                        v = sourcePixels[i].g;
                        break;
                    case ChannelPackingConfig.SourceChannel.B:
                        v = sourcePixels[i].b;
                        break;
                    case ChannelPackingConfig.SourceChannel.A:
                        v = sourcePixels[i].a;
                        break;
                    case ChannelPackingConfig.SourceChannel.Luminance:
                        v = sourcePixels[i].r * 0.299f + sourcePixels[i].g * 0.587f + sourcePixels[i].b * 0.114f;
                        break;
                    default:
                        v = sourcePixels[i].r;
                        break;
                }
                result[i] = new Color(v, v, v, 1f);
            }

            if (readable != source)
                Object.DestroyImmediate(readable);

            return result;
        }

        /// <summary>
        /// Copy non-readable texture to a readable one via RenderTexture GPU readback.
        /// 読み取り不可テクスチャをRenderTexture経由で読み取り可能にコピー
        /// </summary>
        private static Texture2D MakeReadable(Texture2D source, int targetSize)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetSize, targetSize, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D readable = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return readable;
        }

        /// <summary>
        /// Save texture to file with a save dialog.
        /// 保存ダイアログ付きでテクスチャをファイルに保存
        /// </summary>
        private static void SavePackedTexture(Texture2D texture, string defaultName)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "テクスチャを保存 Save Texture",
                defaultName,
                "png",
                "保存場所を選択してください Choose save location");

            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "保存完了 Save Complete",
                $"テクスチャを保存しました: {path}\nTexture saved: {path}",
                "OK");
        }
    }
}
