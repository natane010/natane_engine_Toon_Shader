using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// Image processing filters for mask textures.
    /// マスクテクスチャ用画像処理フィルター
    /// Provides Gaussian blur, levels adjustment, Sobel edge detection,
    /// sharpening (unsharp mask), and binary threshold operations.
    /// </summary>
    internal static class MaskTextureFilters
    {
        /// <summary>
        /// Separable two-pass Gaussian blur.
        /// 分離可能な2パスガウシアンブラー
        /// </summary>
        /// <param name="pixels">Source pixel array (modified in-place).</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="sigma">Standard deviation of the Gaussian kernel. Clamped to [0.1, 100].</param>
        public static void GaussianBlur(Color[] pixels, int width, int height, float sigma)
        {
            if (pixels == null || pixels.Length != width * height) return;
            sigma = Mathf.Clamp(sigma, 0.1f, 100f);

            // Try GPU path first / まずGPUパスを試行
            if (MaskTextureComputeDispatcher.TryGaussianBlurGPU(pixels, width, height, sigma))
                return;

            // Kernel radius: 3 sigma covers ~99.7% of the distribution
            int radius = Mathf.CeilToInt(sigma * 3f);
            if (radius < 1) radius = 1;

            float[] kernel = BuildGaussianKernel(radius, sigma);

            // Temporary buffer for intermediate pass
            Color[] temp = new Color[pixels.Length];

            // Horizontal pass: source -> temp
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sx = Mathf.Clamp(x + k, 0, width - 1);
                        Color c = pixels[y * width + sx];
                        float w = kernel[k + radius];
                        r += c.r * w;
                        g += c.g * w;
                        b += c.b * w;
                        a += c.a * w;
                    }
                    temp[y * width + x] = new Color(r, g, b, a);
                }
            }

            // Vertical pass: temp -> pixels
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sy = Mathf.Clamp(y + k, 0, height - 1);
                        Color c = temp[sy * width + x];
                        float w = kernel[k + radius];
                        r += c.r * w;
                        g += c.g * w;
                        b += c.b * w;
                        a += c.a * w;
                    }
                    pixels[y * width + x] = new Color(r, g, b, a);
                }
            }
        }

        private static float[] BuildGaussianKernel(int radius, float sigma)
        {
            int size = radius * 2 + 1;
            float[] kernel = new float[size];
            float sigma2 = 2f * sigma * sigma;
            float sum = 0f;

            for (int i = 0; i < size; i++)
            {
                float x = i - radius;
                kernel[i] = Mathf.Exp(-(x * x) / sigma2);
                sum += kernel[i];
            }

            // Normalize so kernel sums to 1
            if (sum > 0f)
            {
                float invSum = 1f / sum;
                for (int i = 0; i < size; i++)
                    kernel[i] *= invSum;
            }
            return kernel;
        }

        /// <summary>
        /// Levels adjustment: remaps pixel values through input range, gamma, and output range.
        /// レベル調整：入力範囲、ガンマ、出力範囲を通じてピクセル値を再マッピング
        /// </summary>
        /// <param name="pixels">Source pixel array (modified in-place).</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="inputBlack">Input black point [0..1]. Values at or below become output black.</param>
        /// <param name="inputWhite">Input white point [0..1]. Values at or above become output white.</param>
        /// <param name="gamma">Gamma correction (midtone adjustment). 1.0 = linear. Clamped to [0.01, 10].</param>
        /// <param name="outputBlack">Output black point [0..1].</param>
        /// <param name="outputWhite">Output white point [0..1].</param>
        public static void Levels(Color[] pixels, int width, int height,
            float inputBlack, float inputWhite, float gamma,
            float outputBlack, float outputWhite)
        {
            if (pixels == null || pixels.Length != width * height) return;

            inputBlack = Mathf.Clamp01(inputBlack);
            inputWhite = Mathf.Clamp01(inputWhite);
            gamma = Mathf.Clamp(gamma, 0.01f, 10f);
            outputBlack = Mathf.Clamp01(outputBlack);
            outputWhite = Mathf.Clamp01(outputWhite);

            // Prevent division by zero if input range is degenerate
            float inputRange = inputWhite - inputBlack;
            if (inputRange < 0.001f) inputRange = 0.001f;
            float invInputRange = 1f / inputRange;

            float invGamma = 1f / gamma;
            float outputRange = outputWhite - outputBlack;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                c.r = ApplyLevelsChannel(c.r, inputBlack, invInputRange, invGamma, outputBlack, outputRange);
                c.g = ApplyLevelsChannel(c.g, inputBlack, invInputRange, invGamma, outputBlack, outputRange);
                c.b = ApplyLevelsChannel(c.b, inputBlack, invInputRange, invGamma, outputBlack, outputRange);
                c.a = ApplyLevelsChannel(c.a, inputBlack, invInputRange, invGamma, outputBlack, outputRange);
                pixels[i] = c;
            }
        }

        private static float ApplyLevelsChannel(float value, float inputBlack, float invInputRange,
            float invGamma, float outputBlack, float outputRange)
        {
            // Map to input range [0..1]
            float normalized = Mathf.Clamp01((value - inputBlack) * invInputRange);
            // Apply gamma
            float gammaCorrected = Mathf.Pow(normalized, invGamma);
            // Map to output range
            return Mathf.Clamp01(outputBlack + gammaCorrected * outputRange);
        }

        /// <summary>
        /// Sobel edge detection filter.
        /// Sobelエッジ検出フィルター
        /// </summary>
        /// <param name="pixels">Source pixel array (modified in-place).</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="strength">Edge strength multiplier. Clamped to [0, 10].</param>
        public static void SobelEdge(Color[] pixels, int width, int height, float strength)
        {
            if (pixels == null || pixels.Length != width * height) return;
            strength = Mathf.Clamp(strength, 0f, 10f);

            // Try GPU path first / まずGPUパスを試行
            if (MaskTextureComputeDispatcher.TrySobelEdgeGPU(pixels, width, height, strength))
                return;

            // Work on a copy to avoid reading modified values
            Color[] source = new Color[pixels.Length];
            System.Array.Copy(pixels, source, pixels.Length);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Sample 3x3 neighborhood (clamped at borders)
                    float tl = Luminance(SampleClamped(source, width, height, x - 1, y - 1));
                    float tc = Luminance(SampleClamped(source, width, height, x,     y - 1));
                    float tr = Luminance(SampleClamped(source, width, height, x + 1, y - 1));
                    float ml = Luminance(SampleClamped(source, width, height, x - 1, y));
                    float mr = Luminance(SampleClamped(source, width, height, x + 1, y));
                    float bl = Luminance(SampleClamped(source, width, height, x - 1, y + 1));
                    float bc = Luminance(SampleClamped(source, width, height, x,     y + 1));
                    float br = Luminance(SampleClamped(source, width, height, x + 1, y + 1));

                    // Sobel kernels
                    float gx = (-tl - 2f * ml - bl) + (tr + 2f * mr + br);
                    float gy = (-tl - 2f * tc - tr) + (bl + 2f * bc + br);

                    float edge = Mathf.Clamp01(Mathf.Sqrt(gx * gx + gy * gy) * strength);
                    pixels[y * width + x] = new Color(edge, edge, edge, source[y * width + x].a);
                }
            }
        }

        private static Color SampleClamped(Color[] pixels, int width, int height, int x, int y)
        {
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            return pixels[y * width + x];
        }

        private static float Luminance(Color c)
        {
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        /// <summary>
        /// Sharpen using unsharp mask: result = original + amount * (original - blurred).
        /// アンシャープマスクによるシャープ化: 結果 = 元画像 + amount * (元画像 - ブラー画像)
        /// </summary>
        /// <param name="pixels">Source pixel array (modified in-place).</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="amount">Sharpening intensity. Clamped to [0, 10].</param>
        /// <param name="sigma">Blur sigma for the unsharp mask. Clamped to [0.1, 100].</param>
        public static void Sharpen(Color[] pixels, int width, int height, float amount, float sigma)
        {
            if (pixels == null || pixels.Length != width * height) return;
            amount = Mathf.Clamp(amount, 0f, 10f);

            // Create blurred copy
            Color[] blurred = new Color[pixels.Length];
            System.Array.Copy(pixels, blurred, pixels.Length);
            GaussianBlur(blurred, width, height, sigma);

            // Unsharp mask: original + amount * (original - blurred)
            for (int i = 0; i < pixels.Length; i++)
            {
                Color orig = pixels[i];
                Color blur = blurred[i];
                pixels[i] = new Color(
                    Mathf.Clamp01(orig.r + amount * (orig.r - blur.r)),
                    Mathf.Clamp01(orig.g + amount * (orig.g - blur.g)),
                    Mathf.Clamp01(orig.b + amount * (orig.b - blur.b)),
                    Mathf.Clamp01(orig.a + amount * (orig.a - blur.a)));
            }
        }

        /// <summary>
        /// Binary threshold: pixels below threshold become 0 (black), above become 1 (white).
        /// 二値化閾値：閾値未満のピクセルは0（黒）、以上は1（白）になる
        /// </summary>
        /// <param name="pixels">Source pixel array (modified in-place).</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="threshold">Threshold value [0..1].</param>
        public static void Threshold(Color[] pixels, int width, int height, float threshold)
        {
            if (pixels == null || pixels.Length != width * height) return;
            threshold = Mathf.Clamp01(threshold);

            for (int i = 0; i < pixels.Length; i++)
            {
                float a = pixels[i].a;
                float lum = Luminance(pixels[i]);
                float val = lum >= threshold ? 1f : 0f;
                pixels[i] = new Color(val, val, val, a);
            }
        }
    }
}
