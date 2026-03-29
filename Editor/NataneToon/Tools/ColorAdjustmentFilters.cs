using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// Color adjustment filters for mask texture layers.
    /// マスクテクスチャレイヤー用カラー調整フィルター
    /// </summary>
    internal static class ColorAdjustmentFilters
    {
        /// <summary>
        /// Adjust hue, saturation, and lightness.
        /// 色相・彩度・明度を調整
        /// </summary>
        public static void HSLAdjust(Color[] pixels, int width, int height,
            float hueShift, float saturationMul, float lightnessMul)
        {
            if (pixels == null || pixels.Length != width * height) return;
            for (int i = 0; i < pixels.Length; i++)
            {
                float h, s, v;
                Color.RGBToHSV(pixels[i], out h, out s, out v);
                h = (h + hueShift) % 1f;
                if (h < 0f) h += 1f;
                s = Mathf.Clamp01(s * saturationMul);
                v = Mathf.Clamp01(v * lightnessMul);
                Color c = Color.HSVToRGB(h, s, v);
                c.a = pixels[i].a;
                pixels[i] = c;
            }
        }

        /// <summary>
        /// Color balance adjustment for shadows, midtones, highlights.
        /// シャドウ・ミッドトーン・ハイライトのカラーバランス調整
        /// </summary>
        public static void ColorBalance(Color[] pixels, int width, int height,
            Vector3 shadowsBalance, Vector3 midtonesBalance, Vector3 highlightsBalance)
        {
            if (pixels == null || pixels.Length != width * height) return;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

                // Shadow weight: highest when lum is low
                float shadowW = Mathf.Clamp01(1f - lum * 2f);
                // Highlight weight: highest when lum is high
                float highlightW = Mathf.Clamp01(lum * 2f - 1f);
                // Midtone weight: highest at 0.5
                float midtoneW = 1f - shadowW - highlightW;

                Vector3 shift = shadowsBalance * shadowW +
                               midtonesBalance * midtoneW +
                               highlightsBalance * highlightW;

                c.r = Mathf.Clamp01(c.r + shift.x);
                c.g = Mathf.Clamp01(c.g + shift.y);
                c.b = Mathf.Clamp01(c.b + shift.z);
                pixels[i] = c;
            }
        }

        /// <summary>
        /// Apply tone curve using AnimationCurves for each channel.
        /// AnimationCurveを使用したチャンネルごとのトーンカーブ適用
        /// </summary>
        public static void ToneCurve(Color[] pixels, int width, int height,
            AnimationCurve masterCurve,
            AnimationCurve redCurve = null,
            AnimationCurve greenCurve = null,
            AnimationCurve blueCurve = null)
        {
            if (pixels == null || pixels.Length != width * height) return;
            if (masterCurve == null) return;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                c.r = Mathf.Clamp01(masterCurve.Evaluate(c.r));
                c.g = Mathf.Clamp01(masterCurve.Evaluate(c.g));
                c.b = Mathf.Clamp01(masterCurve.Evaluate(c.b));

                if (redCurve != null)   c.r = Mathf.Clamp01(redCurve.Evaluate(c.r));
                if (greenCurve != null) c.g = Mathf.Clamp01(greenCurve.Evaluate(c.g));
                if (blueCurve != null)  c.b = Mathf.Clamp01(blueCurve.Evaluate(c.b));

                pixels[i] = c;
            }
        }

        /// <summary>
        /// Posterize: reduce color depth to specified number of levels.
        /// ポスタリゼーション: 指定レベル数に色深度を削減
        /// </summary>
        public static void Posterize(Color[] pixels, int width, int height, int levels)
        {
            if (pixels == null || pixels.Length != width * height) return;
            levels = Mathf.Clamp(levels, 2, 256);
            float factor = levels - 1f;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                c.r = Mathf.Round(c.r * factor) / factor;
                c.g = Mathf.Round(c.g * factor) / factor;
                c.b = Mathf.Round(c.b * factor) / factor;
                pixels[i] = c;
            }
        }

        /// <summary>
        /// Desaturate: convert to grayscale.
        /// 彩度除去: グレースケールに変換
        /// </summary>
        public static void Desaturate(Color[] pixels, int width, int height)
        {
            if (pixels == null || pixels.Length != width * height) return;
            for (int i = 0; i < pixels.Length; i++)
            {
                float lum = 0.2126f * pixels[i].r + 0.7152f * pixels[i].g + 0.0722f * pixels[i].b;
                pixels[i] = new Color(lum, lum, lum, pixels[i].a);
            }
        }

        /// <summary>
        /// Vibrance: selectively increase saturation of less-saturated colors.
        /// 自然な彩度: 彩度の低い色を優先的に彩度アップ
        /// </summary>
        public static void Vibrance(Color[] pixels, int width, int height, float amount)
        {
            if (pixels == null || pixels.Length != width * height) return;
            amount = Mathf.Clamp(amount, -1f, 1f);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                float sat = max > 0.0001f ? (max - min) / max : 0f;

                // Less saturated colors get more adjustment
                float boost = amount * (1f - sat);
                float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

                c.r = Mathf.Clamp01(Mathf.Lerp(lum, c.r, 1f + boost));
                c.g = Mathf.Clamp01(Mathf.Lerp(lum, c.g, 1f + boost));
                c.b = Mathf.Clamp01(Mathf.Lerp(lum, c.b, 1f + boost));
                pixels[i] = c;
            }
        }

        /// <summary>
        /// Color temperature adjustment.
        /// 色温度調整
        /// </summary>
        public static void Temperature(Color[] pixels, int width, int height, float temperature)
        {
            if (pixels == null || pixels.Length != width * height) return;
            temperature = Mathf.Clamp(temperature, -1f, 1f);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                c.r = Mathf.Clamp01(c.r + temperature * 0.1f);
                c.b = Mathf.Clamp01(c.b - temperature * 0.1f);
                pixels[i] = c;
            }
        }
    }
}
