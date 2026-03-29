using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// Wet brush color mixing system - simulates paint mixing on canvas.
    /// ウェットブラシ色混合システム - キャンバス上の絵の具混合をシミュレーション
    /// </summary>
    internal static class WetBrushMixer
    {
        /// <summary>
        /// Mix two colors as if wet paint.
        /// ウェットペイントとして2色を混合
        /// Uses a subtractive-like model based on Kubelka-Munk theory (simplified).
        /// </summary>
        /// <param name="canvasColor">Existing color on canvas.</param>
        /// <param name="brushColor">Brush color being applied.</param>
        /// <param name="mixRatio">How much to mix (0=all canvas, 1=all brush).</param>
        /// <param name="wetness">How wet the brush is (0=dry/no mixing, 1=very wet).</param>
        /// <returns>Mixed color result.</returns>
        public static Color MixWet(Color canvasColor, Color brushColor, float mixRatio, float wetness)
        {
            if (wetness <= 0.001f)
                return Color.Lerp(canvasColor, brushColor, mixRatio);

            // Convert to "pigment space" (approximate subtractive mixing)
            // We use a simplified Kubelka-Munk approach:
            // Instead of linear interpolation (additive), we multiply in complementary space

            float wetMix = mixRatio * wetness;
            float dryMix = mixRatio * (1f - wetness);

            // Subtractive component (wet mixing - like real paint)
            Color subtractiveResult = SubtractiveMix(canvasColor, brushColor, wetMix);

            // Additive/linear component (dry mixing - standard digital)
            Color additiveResult = Color.Lerp(canvasColor, brushColor, dryMix);

            // Blend the two mixing modes
            Color result = Color.Lerp(additiveResult, subtractiveResult, wetness);
            result.a = Mathf.Lerp(canvasColor.a, brushColor.a, mixRatio);

            return result;
        }

        /// <summary>
        /// Simplified subtractive color mixing.
        /// 簡易減法混色
        /// </summary>
        private static Color SubtractiveMix(Color a, Color b, float t)
        {
            // Convert to approximate CMY
            float cA = 1f - a.r, mA = 1f - a.g, yA = 1f - a.b;
            float cB = 1f - b.r, mB = 1f - b.g, yB = 1f - b.b;

            // Mix in CMY space
            float cMix = Mathf.Lerp(cA, cB, t);
            float mMix = Mathf.Lerp(mA, mB, t);
            float yMix = Mathf.Lerp(yA, yB, t);

            // Add a slight nonlinearity to simulate pigment saturation
            cMix = Mathf.Pow(cMix, 0.85f);
            mMix = Mathf.Pow(mMix, 0.85f);
            yMix = Mathf.Pow(yMix, 0.85f);

            // Back to RGB
            return new Color(
                Mathf.Clamp01(1f - cMix),
                Mathf.Clamp01(1f - mMix),
                Mathf.Clamp01(1f - yMix),
                1f);
        }

        /// <summary>
        /// Apply wet brush to a pixel with the given parameters.
        /// 指定パラメータでピクセルにウェットブラシを適用
        /// </summary>
        public static Color ApplyWetBrush(Color current, float falloff, float opacity, Color brushColor, float wetness)
        {
            float alpha = falloff * opacity;
            if (alpha <= 0.001f) return current;

            Color mixed = MixWet(current, brushColor, alpha, wetness);
            return mixed;
        }
    }
}
