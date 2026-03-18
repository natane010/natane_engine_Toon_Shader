using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Analyzes material textures (MainTex, BumpMap, masks) to derive optimal shader parameters.
    /// Uses K-means color clustering, histogram analysis, and percentile-based color extraction.
    /// </summary>
    public static class NataneTextureAnalyzer
    {
        private const int SampleSize = 64;
        private const int KMeansK = 6;
        private const int KMeansIterations = 3;

        /// <summary>
        /// Result of main texture color analysis.
        /// </summary>
        public struct ColorAnalysisResult
        {
            public Color dominantColor;
            public Color shadowColor;
            public Color rimColor;
            public Color outlineColor;
            public Color specularColor;
            public bool valid;
        }

        /// <summary>
        /// Analyze the main texture and compute shadow, rim, outline, and specular colors.
        /// </summary>
        public static ColorAnalysisResult AnalyzeMainTexture(Material mat, AutoSetupRole role)
        {
            var result = new ColorAnalysisResult { valid = false };
            if (mat == null) return result;

            var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") as Texture2D : null;
            if (mainTex == null) return result;

            Texture2D readable = CreateReadableCopy(mainTex, SampleSize, SampleSize);
            if (readable == null) return result;

            try
            {
                Color[] pixels = readable.GetPixels();
                result.dominantColor = CalculateDominantColorKMeans(pixels);
                result.shadowColor = DeriveShadowColor(result.dominantColor, role);
                result.rimColor = DeriveRimColor(pixels);
                result.outlineColor = DeriveOutlineColor(pixels);
                result.specularColor = DeriveSpecularColor(pixels);
                result.valid = true;
                return result;
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        /// <summary>
        /// Auto-calculate BumpScale from a normal map's deviation.
        /// </summary>
        public static float AutoBumpScale(Texture2D bumpMap)
        {
            if (bumpMap == null) return 1.0f;

            Texture2D readable = CreateReadableCopy(bumpMap, 32, 32);
            if (readable == null) return 1.0f;

            try
            {
                Color[] pixels = readable.GetPixels();
                float totalDeviation = 0f;
                for (int i = 0; i < pixels.Length; i++)
                {
                    float nx = pixels[i].r * 2f - 1f;
                    float ny = pixels[i].g * 2f - 1f;
                    totalDeviation += Mathf.Sqrt(nx * nx + ny * ny);
                }
                float avgDeviation = totalDeviation / pixels.Length;
                return Mathf.Clamp(avgDeviation * 2.5f, 0.2f, 2.0f);
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        /// <summary>
        /// Derive optimal intensity from a mask texture's coverage and brightness.
        /// </summary>
        public static float AutoIntensityFromMask(Texture2D mask, float baseIntensity = 1.0f)
        {
            if (mask == null) return baseIntensity;

            Texture2D readable = CreateReadableCopy(mask, 32, 32);
            if (readable == null) return baseIntensity;

            try
            {
                Color[] pixels = readable.GetPixels();
                float avgBrightness = 0f;
                int aboveThreshold = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    float g = pixels[i].grayscale;
                    avgBrightness += g;
                    if (g > 0.5f) aboveThreshold++;
                }
                avgBrightness /= pixels.Length;
                float coverage = aboveThreshold / (float)pixels.Length;

                float intensityScale = Mathf.Lerp(1.3f, 0.7f, coverage);
                return baseIntensity * intensityScale * (0.5f + avgBrightness * 0.8f);
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        /// <summary>
        /// Apply auto-derived colors to the material.
        /// </summary>
        public static ColorAnalysisResult AnalyzeAndApplyColors(Material mat, AutoSetupRole role, AutoSetupRecord record)
        {
            var result = AnalyzeMainTexture(mat, role);
            if (!result.valid) return result;

            mat.SetColor("_ShadowColor", result.shadowColor);
            record?.RecordAutoFloat("_ShadowColor_r", result.shadowColor.r);

            if (mat.HasProperty("_RimColor"))
                mat.SetColor("_RimColor", result.rimColor);
            if (mat.HasProperty("_OutlineColor"))
                mat.SetColor("_OutlineColor", result.outlineColor);
            if (mat.HasProperty("_SpecularColor"))
                mat.SetColor("_SpecularColor", result.specularColor);

            return result;
        }

        // ===== K-means color clustering =====

        private static Color CalculateDominantColorKMeans(Color[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                return Color.gray;

            // Initialize centroids by sampling evenly
            Color[] centroids = new Color[KMeansK];
            int step = Mathf.Max(1, pixels.Length / KMeansK);
            for (int i = 0; i < KMeansK; i++)
                centroids[i] = pixels[Mathf.Min(i * step, pixels.Length - 1)];

            int[] assignments = new int[pixels.Length];

            for (int iter = 0; iter < KMeansIterations; iter++)
            {
                // Assign pixels to nearest centroid
                for (int i = 0; i < pixels.Length; i++)
                {
                    float bestDist = float.MaxValue;
                    int bestCluster = 0;
                    for (int c = 0; c < KMeansK; c++)
                    {
                        float dist = ColorDistanceSq(pixels[i], centroids[c]);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestCluster = c;
                        }
                    }
                    assignments[i] = bestCluster;
                }

                // Recompute centroids
                Color[] sums = new Color[KMeansK];
                int[] counts = new int[KMeansK];
                for (int i = 0; i < pixels.Length; i++)
                {
                    int c = assignments[i];
                    sums[c] += pixels[i];
                    counts[c]++;
                }
                for (int c = 0; c < KMeansK; c++)
                {
                    if (counts[c] > 0)
                        centroids[c] = sums[c] / counts[c];
                }
            }

            // Find largest cluster
            int[] clusterSizes = new int[KMeansK];
            for (int i = 0; i < pixels.Length; i++)
                clusterSizes[assignments[i]]++;

            int largestCluster = 0;
            int largestSize = 0;
            for (int c = 0; c < KMeansK; c++)
            {
                if (clusterSizes[c] > largestSize)
                {
                    largestSize = clusterSizes[c];
                    largestCluster = c;
                }
            }

            return centroids[largestCluster];
        }

        private static float ColorDistanceSq(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }

        // ===== Color derivation =====

        private static Color DeriveShadowColor(Color dominant, AutoSetupRole role)
        {
            Color.RGBToHSV(dominant, out float h, out float s, out float v);

            switch (role)
            {
                case AutoSetupRole.Face:
                    h += 0.02f;     // Warm shift (reddish)
                    s *= 1.2f;      // Saturation up
                    v *= 0.65f;     // Darker
                    break;
                case AutoSetupRole.Hair:
                    s *= 1.3f;      // Saturation emphasis
                    v *= 0.55f;     // Much darker
                    break;
                case AutoSetupRole.Clothing:
                    h -= 0.03f;     // Cool shift (bluish)
                    s *= 1.1f;
                    v *= 0.60f;
                    break;
                case AutoSetupRole.Metal:
                    s *= 0.8f;      // Desaturate (metallic)
                    v *= 0.50f;
                    break;
                default:
                    v *= 0.6f;
                    break;
            }

            return Color.HSVToRGB(
                Mathf.Repeat(h, 1f),
                Mathf.Clamp01(s),
                Mathf.Clamp01(v));
        }

        private static Color DeriveRimColor(Color[] pixels)
        {
            // Use top 10% brightest pixels
            int count = Mathf.Max(1, pixels.Length / 10);
            Color sum = Color.black;
            // Simple partial sort: find brightness threshold
            float[] grays = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                grays[i] = pixels[i].grayscale;
            System.Array.Sort(grays);
            float threshold = grays[pixels.Length - count];

            int found = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].grayscale >= threshold)
                {
                    sum += pixels[i];
                    found++;
                }
            }
            if (found > 0) sum /= found;

            return Color.Lerp(Color.white, sum, 0.3f);
        }

        private static Color DeriveOutlineColor(Color[] pixels)
        {
            // Use bottom 10% darkest pixels
            int count = Mathf.Max(1, pixels.Length / 10);
            Color sum = Color.black;
            float[] grays = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                grays[i] = pixels[i].grayscale;
            System.Array.Sort(grays);
            float threshold = grays[Mathf.Min(count, pixels.Length - 1)];

            int found = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].grayscale <= threshold)
                {
                    sum += pixels[i];
                    found++;
                }
            }
            if (found > 0) sum /= found;

            return Color.Lerp(Color.black, sum, 0.5f);
        }

        private static Color DeriveSpecularColor(Color[] pixels)
        {
            // Bright part base + low saturation
            int count = Mathf.Max(1, pixels.Length / 10);
            Color sum = Color.black;
            float[] grays = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                grays[i] = pixels[i].grayscale;
            System.Array.Sort(grays);
            float threshold = grays[pixels.Length - count];

            int found = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].grayscale >= threshold)
                {
                    sum += pixels[i];
                    found++;
                }
            }
            if (found > 0) sum /= found;

            Color.RGBToHSV(sum, out float bh, out float bs, out float bv);
            return Color.HSVToRGB(bh, bs * 0.3f, Mathf.Clamp01(bv * 1.1f));
        }

        // ===== Texture utility =====

        internal static Texture2D CreateReadableCopy(Texture source, int width, int height)
        {
            if (source == null) return null;

            RenderTexture temporary = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;

                Texture2D readable = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readable.Apply(false, false);
                return readable;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }
    }
}
