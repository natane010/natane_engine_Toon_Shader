using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Converts textures to seamless/tileable by blending edges.
    /// エッジブレンドによるシームレス/タイル可能テクスチャへの変換
    /// </summary>
    internal static class SeamlessConverter
    {
        /// <summary>
        /// Convert pixel array to seamless by cross-fading edges.
        /// エッジをクロスフェードしてシームレスに変換
        /// </summary>
        /// <param name="pixels">Source pixels (modified in-place).</param>
        /// <param name="width">Texture width.</param>
        /// <param name="height">Texture height.</param>
        /// <param name="blendWidth">Width of the blend region (0-0.5, fraction of texture size).</param>
        public static void MakeSeamless(Color[] pixels, int width, int height, float blendWidth)
        {
            if (pixels == null || width <= 0 || height <= 0) return;
            blendWidth = Mathf.Clamp(blendWidth, 0.01f, 0.5f);

            Color[] original = new Color[pixels.Length];
            System.Array.Copy(pixels, original, pixels.Length);

            int blendPixelsX = Mathf.Max(1, Mathf.RoundToInt(width * blendWidth));
            int blendPixelsY = Mathf.Max(1, Mathf.RoundToInt(height * blendWidth));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate blend weights for each edge
                    float weightX = 1f;
                    float weightY = 1f;

                    if (x < blendPixelsX)
                        weightX = (float)x / blendPixelsX;
                    else if (x >= width - blendPixelsX)
                        weightX = (float)(width - 1 - x) / blendPixelsX;

                    if (y < blendPixelsY)
                        weightY = (float)y / blendPixelsY;
                    else if (y >= height - blendPixelsY)
                        weightY = (float)(height - 1 - y) / blendPixelsY;

                    float weight = weightX * weightY;
                    weight = weight * weight * (3f - 2f * weight); // Smoothstep

                    if (weight < 0.999f)
                    {
                        // Sample from the opposite edge
                        int mirrorX = (x + width / 2) % width;
                        int mirrorY = (y + height / 2) % height;
                        Color mirrorColor = original[mirrorY * width + mirrorX];
                        Color currentColor = original[y * width + x];

                        pixels[y * width + x] = Color.Lerp(mirrorColor, currentColor, weight);
                    }
                }
            }
        }

        /// <summary>
        /// Generate a seamless-check preview (2x2 tiled view).
        /// シームレスチェックプレビュー（2x2タイル表示）を生成
        /// </summary>
        public static Texture2D CreateTiledPreview(Color[] pixels, int width, int height, int tileCount)
        {
            int previewW = width * tileCount;
            int previewH = height * tileCount;
            // Cap preview size
            if (previewW > 1024) { previewW = 1024; previewH = 1024; }

            var tex = new Texture2D(previewW, previewH, TextureFormat.RGBA32, false);
            for (int y = 0; y < previewH; y++)
            {
                for (int x = 0; x < previewW; x++)
                {
                    int srcX = x % width;
                    int srcY = y % height;
                    tex.SetPixel(x, y, pixels[srcY * width + srcX]);
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Draw seamless converter settings UI.
        /// シームレス変換設定UIを描画
        /// </summary>
        public static void DrawSettingsUI(ref float blendWidth)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(L("シームレス変換", "Seamless Converter"), EditorStyles.boldLabel);
                blendWidth = EditorGUILayout.Slider(
                    L("ブレンド幅", "Blend Width"), blendWidth, 0.05f, 0.5f);
                EditorGUILayout.HelpBox(
                    L("アクティブレイヤーのエッジをクロスフェードしてシームレスに変換します。",
                      "Cross-fades edges of the active layer to make it seamless/tileable."),
                    MessageType.Info);
            }
        }
    }
}
