using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    internal enum HalftoneType { CircleDot, SquareDot, Line, CrossHatch, Diamond }

    /// <summary>
    /// Halftone/screentone pattern generator for manga-style effects.
    /// 漫画風エフェクト用ハーフトーン/スクリーントーンパターンジェネレータ
    /// </summary>
    internal static class HalftoneGenerator
    {
        /// <summary>
        /// Generate halftone pattern from a source grayscale image.
        /// ソースグレースケール画像からハーフトーンパターンを生成
        /// </summary>
        public static Color[] Generate(Color[] source, int width, int height,
            HalftoneType type, float dotScale, float angle, Color inkColor, Color paperColor)
        {
            if (source == null) return null;
            Color[] result = new Color[width * height];
            float cosA = Mathf.Cos(angle * Mathf.Deg2Rad);
            float sinA = Mathf.Sin(angle * Mathf.Deg2Rad);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float v = (float)y / height;

                    // Rotated coordinates
                    float cu = u - 0.5f, cv = v - 0.5f;
                    float ru = cu * cosA - cv * sinA;
                    float rv = cu * sinA + cv * cosA;
                    ru *= dotScale; rv *= dotScale;

                    // Source luminance
                    int idx = y * width + x;
                    Color src = source[idx];
                    float lum = (src.r + src.g + src.b) / 3f;

                    // Halftone threshold
                    float pattern = EvaluateHalftone(ru, rv, type);
                    bool isDot = pattern < lum;

                    result[idx] = isDot ? paperColor : inkColor;
                }
            }
            return result;
        }

        /// <summary>
        /// Generate halftone from uniform value (no source image).
        /// 均一値からハーフトーンを生成（ソース画像なし）
        /// </summary>
        public static Color[] GenerateUniform(int width, int height,
            HalftoneType type, float dotScale, float angle, float density,
            Color inkColor, Color paperColor)
        {
            Color[] source = new Color[width * height];
            Color fill = new Color(density, density, density, 1f);
            for (int i = 0; i < source.Length; i++) source[i] = fill;
            return Generate(source, width, height, type, dotScale, angle, inkColor, paperColor);
        }

        private static float EvaluateHalftone(float u, float v, HalftoneType type)
        {
            float fu = u - Mathf.Floor(u), fv = v - Mathf.Floor(v);
            switch (type)
            {
                case HalftoneType.CircleDot:
                    float dx = fu - 0.5f, dy = fv - 0.5f;
                    return Mathf.Sqrt(dx * dx + dy * dy) * 2f;

                case HalftoneType.SquareDot:
                    return Mathf.Max(Mathf.Abs(fu - 0.5f), Mathf.Abs(fv - 0.5f)) * 2f;

                case HalftoneType.Line:
                    return Mathf.Abs(fu - 0.5f) * 2f;

                case HalftoneType.CrossHatch:
                    float h1 = Mathf.Abs(fu - 0.5f);
                    float h2 = Mathf.Abs(fv - 0.5f);
                    return Mathf.Min(h1, h2) * 2f;

                case HalftoneType.Diamond:
                    return (Mathf.Abs(fu - 0.5f) + Mathf.Abs(fv - 0.5f));

                default: return 0.5f;
            }
        }

        public static void DrawSettingsUI(ref HalftoneType type, ref float dotScale,
            ref float angle, ref float density, ref Color inkColor, ref Color paperColor)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(L("ハーフトーン", "Halftone"), EditorStyles.boldLabel);
                type = (HalftoneType)EditorGUILayout.EnumPopup(L("タイプ", "Type"), type);
                dotScale = EditorGUILayout.Slider(L("ドットスケール", "Dot Scale"), dotScale, 2f, 60f);
                angle = EditorGUILayout.Slider(L("角度", "Angle"), angle, 0f, 90f);
                density = EditorGUILayout.Slider(L("濃度", "Density"), density, 0f, 1f);
                inkColor = EditorGUILayout.ColorField(L("インク色", "Ink"), inkColor);
                paperColor = EditorGUILayout.ColorField(L("紙色", "Paper"), paperColor);
            }
        }
    }
}
