using System.Collections.Generic;
using UnityEngine;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Estimates D3D11 sampler pressure so the inspector can warn and block
    /// risky feature combinations before shader compilation fails.
    /// </summary>
    public static class NataneToonSamplerBudgetEstimator
    {
        public const int BaseSamplerCount = 5;
        public const int SamplerLimit = 16;
        public const int WarningThreshold = 13;
        public const int NearLimitThreshold = 15;

        private const int LightVolumeLtcgiReserve = 2;
        private const int CriticalLightingReserve = 1;

        public readonly struct FeatureCost
        {
            public readonly string Keyword;
            public readonly string LabelJa;
            public readonly string LabelEn;
            public readonly int SamplerCost;

            public FeatureCost(string keyword, string labelJa, string labelEn, int samplerCost)
            {
                Keyword = keyword;
                LabelJa = labelJa;
                LabelEn = labelEn;
                SamplerCost = samplerCost;
            }
        }

        public readonly struct SamplerBudgetEstimate
        {
            public readonly int BaseSamplers;
            public readonly int EstimatedSamplers;
            public readonly int Limit;
            public readonly FeatureCost[] Contributors;
            public readonly bool HasLightVolumeLtcgiCombo;
            public readonly bool HasCriticalLightingCombo;

            public SamplerBudgetEstimate(
                int baseSamplers,
                int estimatedSamplers,
                int limit,
                FeatureCost[] contributors,
                bool hasLightVolumeLtcgiCombo,
                bool hasCriticalLightingCombo)
            {
                BaseSamplers = baseSamplers;
                EstimatedSamplers = estimatedSamplers;
                Limit = limit;
                Contributors = contributors ?? new FeatureCost[0];
                HasLightVolumeLtcgiCombo = hasLightVolumeLtcgiCombo;
                HasCriticalLightingCombo = hasCriticalLightingCombo;
            }

            public int OptionalSamplers => Mathf.Max(0, EstimatedSamplers - BaseSamplers);
            public bool IsWarning => EstimatedSamplers >= WarningThreshold;
            public bool IsNearLimit => EstimatedSamplers >= NearLimitThreshold;
            public bool IsAtLimit => EstimatedSamplers == Limit;
            public bool IsOverLimit => EstimatedSamplers > Limit;
        }

        public readonly struct ToggleEvaluation
        {
            public readonly bool CanEnable;
            public readonly int AddedSamplers;
            public readonly SamplerBudgetEstimate Current;
            public readonly SamplerBudgetEstimate AfterEnable;

            public ToggleEvaluation(bool canEnable, int addedSamplers, SamplerBudgetEstimate current, SamplerBudgetEstimate afterEnable)
            {
                CanEnable = canEnable;
                AddedSamplers = addedSamplers;
                Current = current;
                AfterEnable = afterEnable;
            }
        }

        private static readonly FeatureCost[] FeatureCosts =
        {
            new FeatureCost("_2ND_TEXTURE", "2nd Texture", "2nd Texture", 1),
            new FeatureCost("_3RD_TEXTURE", "3rd Texture", "3rd Texture", 1),
            new FeatureCost("_4TH_TEXTURE", "4th Texture", "4th Texture", 1),
            new FeatureCost("_5TH_TEXTURE", "5th Texture", "5th Texture", 1),
            new FeatureCost("_SCREEN_TONE", "Screen Tone", "Screen Tone", 0),
            new FeatureCost("_USE_AO", "AO", "Ambient Occlusion", 0),
            new FeatureCost("_SPECULAR", "Specular", "Specular", 0),
            new FeatureCost("_HAIR_SPECULAR", "Hair Specular", "Hair Specular", 0),
            new FeatureCost("_RIM_LIGHT", "Rim Light", "Rim Light", 0),
            new FeatureCost("_RIM_LIGHT_2", "Rim Light 2", "Rim Light 2", 0),
            new FeatureCost("_OFFSET_RIM_LIGHT", "Offset Rim Light", "Offset Rim Light", 0),
            new FeatureCost("_SHEEN", "Sheen", "Sheen", 1),
            new FeatureCost("_MATCAP", "MatCap", "MatCap", 1),
            new FeatureCost("_MATCAP_2", "MatCap 2", "MatCap 2", 1),
            new FeatureCost("_MATCAP_3", "MatCap 3", "MatCap 3", 1),
            new FeatureCost("_GLITTER", "Glitter", "Glitter", 0),
            new FeatureCost("_OUTLINE", "Outline", "Outline", 1),
            new FeatureCost("_EMISSION", "Emission", "Emission", 1),
            new FeatureCost("_NORMALMAP", "Normal Map", "Normal Map", 1),
            new FeatureCost("_SSS", "SSS", "SSS", 2),
            new FeatureCost("_SSS_LUT", "SSS LUT", "SSS LUT", 1),
            new FeatureCost("_DISSOLVE", "Dissolve", "Dissolve", 2),
            new FeatureCost("_ALPHA_MASK", "Alpha Mask", "Alpha Mask", 0),
            new FeatureCost("_REFLECTION", "Reflection", "Reflection", 1),
            new FeatureCost("_IRIDESCENCE", "Iridescence", "Iridescence", 0),
            new FeatureCost("_ENV_RIM", "Environmental Rim", "Environmental Rim", 1),
            new FeatureCost("_PARALLAX", "Parallax", "Parallax", 1),
            new FeatureCost("_REFRACTION", "Refraction", "Refraction", 2),
            new FeatureCost("_DECAL", "Decal", "Decal", 1),
            new FeatureCost("_BACKFACE_TEXTURE", "Backface Texture", "Backface Texture", 1),
            new FeatureCost("_VIDEO_TEXTURE", "Video Texture", "Video Texture", 1),
            new FeatureCost("_VERTEX_ANIMATION", "Vertex Animation", "Vertex Animation", 1),
            new FeatureCost("_WATER_DRIP", "Water Drip", "Water Drip", 1),
            new FeatureCost("_SMEAR", "Smear", "Smear", 1),
            new FeatureCost("_FUR", "Fur", "Fur", 2),
            new FeatureCost("_PBR", "PBR", "PBR", 2),
            new FeatureCost("_SMOOTH_NORMAL", "Smooth Normal", "Smooth Normal", 1),
            new FeatureCost("_HOLOGRAM", "Hologram", "Hologram", 1),
            new FeatureCost("_HOLOGRAM_NOISE", "Hologram Noise", "Hologram Noise", 1),
            new FeatureCost("_GLITCH", "Glitch", "Glitch", 2),
            new FeatureCost("_GLITCH_STRETCH", "Stretch Glitch", "Stretch Glitch", 1),
            new FeatureCost("_COLOR_QUANTIZE", "Color Quantize", "Color Quantize", 1),
            new FeatureCost("_LUT_3D", "LUT", "LUT", 1),
            new FeatureCost("_HATCHING", "Hatching", "Hatching", 2),
            new FeatureCost("_WATERCOLOR", "Watercolor", "Watercolor", 3),
            new FeatureCost("_SCREEN_EDGE", "Screen Edge", "Screen Edge", 1),
            new FeatureCost("_INTERSECTION_FADE", "Intersection Fade", "Intersection Fade", 1),
            new FeatureCost("_DETAIL_MAP", "Detail Map", "Detail Map", 2),
            new FeatureCost("_SURFACE_COVER", "Surface Cover", "Surface Cover", 2),
            new FeatureCost("_AUDIOLINK", "AudioLink", "AudioLink", 2),
            new FeatureCost("_VAT", "VAT", "VAT", 2),
            new FeatureCost("_TESS_DISPLACEMENT", "Displacement", "Displacement", 1),
            new FeatureCost("_USE_LIGHT_VOLUME", "Light Volume", "Light Volume", 1),
            new FeatureCost("_LTCGI", "LTCGI", "LTCGI", 1)
        };

        private static readonly FeatureCost LightVolumeLtcgiReserveCost =
            new FeatureCost("__LIGHT_VOLUME_LTCGI_RESERVE", "Light Volume + LTCGI Reserve", "Light Volume + LTCGI Reserve", LightVolumeLtcgiReserve);

        private static readonly FeatureCost CriticalLightingReserveCost =
            new FeatureCost("__CRITICAL_LIGHTING_RESERVE", "Critical Lighting Reserve", "Critical Lighting Reserve", CriticalLightingReserve);

        public static SamplerBudgetEstimate Estimate(Material material)
        {
            return Estimate(material, null, false);
        }

        public static SamplerBudgetEstimate Estimate(Material material, string overrideKeyword, bool overrideEnabled)
        {
            if (material == null)
            {
                return new SamplerBudgetEstimate(0, 0, SamplerLimit, new FeatureCost[0], false, false);
            }

            int total = BaseSamplerCount;
            var contributors = new List<FeatureCost>();

            for (int i = 0; i < FeatureCosts.Length; i++)
            {
                FeatureCost feature = FeatureCosts[i];
                bool isEnabled = material.IsKeywordEnabled(feature.Keyword);
                if (!string.IsNullOrEmpty(overrideKeyword) && feature.Keyword == overrideKeyword)
                {
                    isEnabled = overrideEnabled;
                }

                if (!isEnabled || feature.SamplerCost <= 0)
                {
                    continue;
                }

                total += feature.SamplerCost;
                contributors.Add(feature);
            }

            bool hasLightVolume = IsKeywordEnabled(material, "_USE_LIGHT_VOLUME", overrideKeyword, overrideEnabled);
            bool hasLtcgi = IsKeywordEnabled(material, "_LTCGI", overrideKeyword, overrideEnabled);
            bool hasHatching = IsKeywordEnabled(material, "_HATCHING", overrideKeyword, overrideEnabled);

            bool hasLightVolumeLtcgiCombo = hasLightVolume && hasLtcgi;
            bool hasCriticalLightingCombo = hasLightVolumeLtcgiCombo && hasHatching;

            if (hasLightVolumeLtcgiCombo)
            {
                total += LightVolumeLtcgiReserve;
                contributors.Add(LightVolumeLtcgiReserveCost);
            }

            if (hasCriticalLightingCombo)
            {
                total += CriticalLightingReserve;
                contributors.Add(CriticalLightingReserveCost);
            }

            contributors.Sort((left, right) =>
            {
                int byCost = right.SamplerCost.CompareTo(left.SamplerCost);
                return byCost != 0 ? byCost : string.CompareOrdinal(left.Keyword, right.Keyword);
            });

            return new SamplerBudgetEstimate(
                BaseSamplerCount,
                total,
                SamplerLimit,
                contributors.ToArray(),
                hasLightVolumeLtcgiCombo,
                hasCriticalLightingCombo);
        }

        public static ToggleEvaluation EvaluateEnable(Material material, string keyword)
        {
            SamplerBudgetEstimate current = Estimate(material);

            if (!TryGetFeatureCost(keyword, out FeatureCost feature) || feature.SamplerCost <= 0)
            {
                return new ToggleEvaluation(true, 0, current, current);
            }

            if (material != null && material.IsKeywordEnabled(keyword))
            {
                return new ToggleEvaluation(true, 0, current, current);
            }

            SamplerBudgetEstimate afterEnable = Estimate(material, keyword, true);
            bool canEnable = afterEnable.EstimatedSamplers <= SamplerLimit;
            return new ToggleEvaluation(canEnable, feature.SamplerCost, current, afterEnable);
        }

        public static bool TryGetFeatureCost(string keyword, out FeatureCost feature)
        {
            for (int i = 0; i < FeatureCosts.Length; i++)
            {
                if (FeatureCosts[i].Keyword == keyword)
                {
                    feature = FeatureCosts[i];
                    return true;
                }
            }

            feature = default;
            return false;
        }

        public static string GetDisplayName(FeatureCost feature)
        {
            return L(feature.LabelJa, feature.LabelEn);
        }

        public static string FormatContributor(FeatureCost feature)
        {
            return $"{GetDisplayName(feature)} +{feature.SamplerCost}";
        }

        private static bool IsKeywordEnabled(Material material, string keyword, string overrideKeyword, bool overrideEnabled)
        {
            if (material == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(overrideKeyword) && keyword == overrideKeyword)
            {
                return overrideEnabled;
            }

            return material.IsKeywordEnabled(keyword);
        }
    }
}