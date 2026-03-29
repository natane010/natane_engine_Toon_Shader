using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Normal map painting utilities for the texture studio.
    /// テクスチャスタジオ用法線マップペイントユーティリティ
    /// </summary>
    internal static class NormalMapPainter
    {
        /// <summary>Normal direction for painting.</summary>
        internal enum NormalDirection { Raise, Lower, Left, Right, Smooth, Flatten }

        private static NormalDirection currentDirection = NormalDirection.Raise;
        private static float normalStrength = 0.5f;

        /// <summary>
        /// Paint normal map direction at the given pixel position.
        /// 指定ピクセル位置に法線マップの方向を描画
        /// </summary>
        public static void PaintNormal(
            Color[] normalPixels, int width, int height,
            Vector2 center, float radius, float hardness, float opacity,
            NormalDirection direction, float strength)
        {
            if (normalPixels == null || width <= 0 || height <= 0) return;

            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + radius));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float falloff = MaskTextureBrush.BrushFalloff(dist, radius, hardness);
                    if (falloff <= 0f) continue;

                    float alpha = falloff * opacity * strength;
                    int idx = y * width + x;
                    Color current = normalPixels[idx];

                    // Decode current normal (Unity tangent space: R=X, G=Y, B=Z)
                    float nx = current.r * 2f - 1f;
                    float ny = current.g * 2f - 1f;
                    float nz = current.b * 2f - 1f;

                    // Apply direction
                    switch (direction)
                    {
                        case NormalDirection.Raise:
                            // Push normal toward camera (increase Z, reduce XY)
                            nz = Mathf.Lerp(nz, 1f, alpha * 0.5f);
                            break;
                        case NormalDirection.Lower:
                            // Indent (decrease Z slightly)
                            nz = Mathf.Lerp(nz, 0.5f, alpha * 0.3f);
                            break;
                        case NormalDirection.Left:
                            nx = Mathf.Lerp(nx, -1f, alpha * 0.3f);
                            break;
                        case NormalDirection.Right:
                            nx = Mathf.Lerp(nx, 1f, alpha * 0.3f);
                            break;
                        case NormalDirection.Smooth:
                            // Average with neighbors
                            Color avg = GetAverageNormal(normalPixels, x, y, width, height);
                            nx = Mathf.Lerp(nx, avg.r * 2f - 1f, alpha);
                            ny = Mathf.Lerp(ny, avg.g * 2f - 1f, alpha);
                            nz = Mathf.Lerp(nz, avg.b * 2f - 1f, alpha);
                            break;
                        case NormalDirection.Flatten:
                            nx = Mathf.Lerp(nx, 0f, alpha);
                            ny = Mathf.Lerp(ny, 0f, alpha);
                            nz = Mathf.Lerp(nz, 1f, alpha);
                            break;
                    }

                    // Normalize
                    float len = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                    if (len > 0.001f) { nx /= len; ny /= len; nz /= len; }
                    else { nx = 0; ny = 0; nz = 1; }

                    // Encode back
                    normalPixels[idx] = new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f);
                }
            }
        }

        private static Color GetAverageNormal(Color[] pixels, int cx, int cy, int w, int h)
        {
            float r = 0, g = 0, b = 0;
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = Mathf.Clamp(cx + dx, 0, w - 1);
                    int ny = Mathf.Clamp(cy + dy, 0, h - 1);
                    Color c = pixels[ny * w + nx];
                    r += c.r; g += c.g; b += c.b; count++;
                }
            }
            return new Color(r / count, g / count, b / count, 1f);
        }

        /// <summary>
        /// Create a flat (default) normal map.
        /// フラット（デフォルト）法線マップを作成
        /// </summary>
        public static Color[] CreateFlatNormalMap(int width, int height)
        {
            Color[] pixels = new Color[width * height];
            Color flat = new Color(0.5f, 0.5f, 1f, 1f); // (0, 0, 1) in tangent space
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = flat;
            return pixels;
        }

        /// <summary>
        /// Generate normal map from height map (grayscale pixels).
        /// ハイトマップ（グレースケールピクセル）から法線マップを生成
        /// </summary>
        public static Color[] HeightToNormal(Color[] heightPixels, int width, int height, float strength)
        {
            Color[] normals = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float left = GetHeight(heightPixels, Mathf.Max(0, x - 1), y, width);
                    float right = GetHeight(heightPixels, Mathf.Min(width - 1, x + 1), y, width);
                    float down = GetHeight(heightPixels, x, Mathf.Max(0, y - 1), width);
                    float up = GetHeight(heightPixels, x, Mathf.Min(height - 1, y + 1), width);

                    float dx = (left - right) * strength;
                    float dy = (down - up) * strength;
                    float dz = 1f;

                    float len = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                    dx /= len; dy /= len; dz /= len;

                    normals[y * width + x] = new Color(dx * 0.5f + 0.5f, dy * 0.5f + 0.5f, dz * 0.5f + 0.5f, 1f);
                }
            }
            return normals;
        }

        private static float GetHeight(Color[] pixels, int x, int y, int width)
        {
            Color c = pixels[y * width + x];
            return (c.r + c.g + c.b) / 3f;
        }

        /// <summary>
        /// Draw normal map painter settings UI.
        /// 法線マップペインター設定UIを描画
        /// </summary>
        public static void DrawSettingsUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(L("法線ペイント", "Normal Paint"), EditorStyles.boldLabel);
                currentDirection = (NormalDirection)EditorGUILayout.EnumPopup(
                    L("方向", "Direction"), currentDirection);
                normalStrength = EditorGUILayout.Slider(
                    L("強度", "Strength"), normalStrength, 0.1f, 1f);

                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(L("ハイトマップから変換", "From Height Map"), EditorStyles.miniButton, GUILayout.Width(140)))
                {
                    // Placeholder - will be called by integration code
                }
                if (GUILayout.Button(L("フラットにリセット", "Reset to Flat"), EditorStyles.miniButton, GUILayout.Width(130)))
                {
                    // Placeholder
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        public static NormalDirection CurrentDirection => currentDirection;
        public static float CurrentStrength => normalStrength;
    }
}
