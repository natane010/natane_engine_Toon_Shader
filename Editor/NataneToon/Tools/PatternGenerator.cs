using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    internal enum PatternType { Dots, Stripes, Checkerboard, Hexagon, Diamonds, Waves, Grid }

    /// <summary>
    /// Procedural pattern texture generator.
    /// プロシージャルパターンテクスチャジェネレータ
    /// </summary>
    internal static class PatternGenerator
    {
        public static Color[] Generate(int width, int height, PatternType type,
            float scale, float rotation, Color foreground, Color background, float threshold)
        {
            Color[] pixels = new Color[width * height];
            float cosR = Mathf.Cos(rotation * Mathf.Deg2Rad);
            float sinR = Mathf.Sin(rotation * Mathf.Deg2Rad);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float v = (float)y / height;

                    // Apply rotation
                    float cu = u - 0.5f, cv = v - 0.5f;
                    float ru = cu * cosR - cv * sinR + 0.5f;
                    float rv = cu * sinR + cv * cosR + 0.5f;

                    float value = EvaluatePattern(ru, rv, type, scale);
                    Color col = value > threshold ? foreground : background;
                    pixels[y * width + x] = col;
                }
            }
            return pixels;
        }

        private static float EvaluatePattern(float u, float v, PatternType type, float scale)
        {
            float su = u * scale, sv = v * scale;
            switch (type)
            {
                case PatternType.Dots:
                    float dx = Frac(su) - 0.5f, dy = Frac(sv) - 0.5f;
                    return 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 3f);

                case PatternType.Stripes:
                    return Mathf.Abs(Mathf.Sin(su * Mathf.PI)) > 0.5f ? 1f : 0f;

                case PatternType.Checkerboard:
                    return ((Mathf.FloorToInt(su) + Mathf.FloorToInt(sv)) % 2 == 0) ? 1f : 0f;

                case PatternType.Hexagon:
                    return HexPattern(su, sv);

                case PatternType.Diamonds:
                    float fdx = Mathf.Abs(Frac(su) - 0.5f);
                    float fdy = Mathf.Abs(Frac(sv) - 0.5f);
                    return (fdx + fdy) < 0.4f ? 1f : 0f;

                case PatternType.Waves:
                    float wave = Mathf.Sin(su * Mathf.PI * 2f + Mathf.Sin(sv * Mathf.PI) * 2f);
                    return wave > 0f ? 1f : 0f;

                case PatternType.Grid:
                    float gx = Mathf.Abs(Frac(su) - 0.5f);
                    float gy = Mathf.Abs(Frac(sv) - 0.5f);
                    return (gx > 0.4f || gy > 0.4f) ? 1f : 0f;

                default: return 0f;
            }
        }

        private static float HexPattern(float u, float v)
        {
            float sqrt3 = 1.732f;
            float r = 0.5f;
            v /= sqrt3;
            float hx = u - Mathf.Floor(u + 0.5f);
            float hy = v - Mathf.Floor(v + 0.5f);
            float dist = Mathf.Max(Mathf.Abs(hx), Mathf.Abs(hy * sqrt3 * 0.5f + hx * 0.5f));
            dist = Mathf.Max(dist, Mathf.Abs(hy * sqrt3 * 0.5f - hx * 0.5f));
            return dist < r * 0.8f ? 1f : 0f;
        }

        private static float Frac(float x) => x - Mathf.Floor(x);

        /// <summary>
        /// Draw pattern generator settings UI. Returns true if Generate should be called.
        /// パターンジェネレータ設定UIを描画。Generateを呼ぶべき場合trueを返す。
        /// </summary>
        public static bool DrawSettingsUI(ref PatternType type, ref float scale,
            ref float rotation, ref Color foreground, ref Color background, ref float threshold)
        {
            bool changed = false;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(L("パターン生成", "Pattern Generator"), EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                type = (PatternType)EditorGUILayout.EnumPopup(L("タイプ", "Type"), type);
                scale = EditorGUILayout.Slider(L("スケール", "Scale"), scale, 1f, 50f);
                rotation = EditorGUILayout.Slider(L("回転", "Rotation"), rotation, 0f, 360f);
                foreground = EditorGUILayout.ColorField(L("前景色", "Foreground"), foreground);
                background = EditorGUILayout.ColorField(L("背景色", "Background"), background);
                threshold = EditorGUILayout.Slider(L("閾値", "Threshold"), threshold, 0f, 1f);
                if (EditorGUI.EndChangeCheck()) changed = true;

                if (GUILayout.Button(L("レイヤーに生成", "Generate to Layer")))
                    changed = true;
            }
            return changed;
        }
    }
}
