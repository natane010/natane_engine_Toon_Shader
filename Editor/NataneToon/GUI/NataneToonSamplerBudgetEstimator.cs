using System.Collections.Generic;
using UnityEngine;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Estimates sampler usage so the inspector can warn and block risky feature combinations
    /// before they trigger shader compilation failures.
    /// </summary>
    public static class NataneToonSamplerBudgetEstimator
    {
        public const int BaseSamplerCount = 5;
        public const int SamplerLimit = 16;
        public const int WarningThreshold = 13;
        public const int NearLimitThreshold = 15;

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
            new FeatureCost("_2ND_TEXTURE", "2nd テクスチャ", "2nd Texture", 2),
            new FeatureCost("_3RD_TEXTURE", "3rd テクスチャ", "3rd Texture", 2),
            new FeatureCost("_4TH_TEXTURE", "4th テクスチャ", "4th Texture", 2),
            new FeatureCost("_5TH_TEXTURE", "5th テクスチャ", "5th Texture", 2),
            new FeatureCost("_SCREEN_TONE", "スクリーントーン", "Screen Tone", 1),
            new FeatureCost("_USE_AO", "AO", "Ambient Occlusion", 1),
            new FeatureCost("_SPECULAR", "スペキュラー", "Specular", 1),
            new FeatureCost("_HAIR_SPECULAR", "ヘアスペキュラー", "Hair Specular", 2),
            new FeatureCost("_RIM_LIGHT", "リムライト", "Rim Light", 1),
            new FeatureCost("_RIM_LIGHT_2", "リムライト 2", "Rim Light 2", 1),
            new FeatureCost("_OFFSET_RIM_LIGHT", "オフセットリムライト", "Offset Rim Light", 1),
            new FeatureCost("_SHEEN", "シーン", "Sheen", 1),
            new FeatureCost("_MATCAP", "MatCap", "MatCap", 2),
            new FeatureCost("_MATCAP_2", "MatCap 2", "MatCap 2", 2),
            new FeatureCost("_MATCAP_3", "MatCap 3", "MatCap 3", 2),
            new FeatureCost("_GLITTER", "グリッター", "Glitter", 1),
            new FeatureCost("_OUTLINE", "アウトライン", "Outline", 1),
            new FeatureCost("_EMISSION", "エミッション", "Emission", 2),
            new FeatureCost("_NORMALMAP", "ノーマルマップ", "Normal Map", 1),
            new FeatureCost("_SSS", "SSS", "SSS", 2),
            new FeatureCost("_SSS_LUT", "SSS LUT", "SSS LUT", 1),
            new FeatureCost("_DISSOLVE", "ディゾルブ", "Dissolve", 2),
            new FeatureCost("_ALPHA_MASK", "アルファマスク", "Alpha Mask", 1),
            new FeatureCost("_REFLECTION", "リフレクション", "Reflection", 2),
            new FeatureCost("_IRIDESCENCE", "イリデッセンス", "Iridescence", 1),
            new FeatureCost("_ENV_RIM", "環境リム", "Environmental Rim", 2),
            new FeatureCost("_PARALLAX", "パララックス", "Parallax", 1),
            new FeatureCost("_REFRACTION", "屈折", "Refraction", 2),
            new FeatureCost("_DECAL", "デカール", "Decal", 1),
            new FeatureCost("_BACKFACE_TEXTURE", "裏面テクスチャ", "Backface Texture", 1),
            new FeatureCost("_VIDEO_TEXTURE", "ビデオテクスチャ", "Video Texture", 1),
            new FeatureCost("_VERTEX_ANIMATION", "頂点アニメーション", "Vertex Animation", 1),
            new FeatureCost("_WATER_DRIP", "雫エフェクト", "Water Drip", 1),
            new FeatureCost("_SMEAR", "スミア", "Smear", 1),
            new FeatureCost("_FUR", "ファー", "Fur", 2),
            new FeatureCost("_PBR", "PBR", "PBR", 2),
            new FeatureCost("_SMOOTH_NORMAL", "スムース法線", "Smooth Normal", 1),
            new FeatureCost("_HOLOGRAM", "ホログラム", "Hologram", 1),
            new FeatureCost("_HOLOGRAM_NOISE", "ホログラムノイズ", "Hologram Noise", 1),
            new FeatureCost("_GLITCH", "グリッチ", "Glitch", 2),
            new FeatureCost("_GLITCH_STRETCH", "ストレッチグリッチ", "Stretch Glitch", 1),
            new FeatureCost("_COLOR_QUANTIZE", "色の量子化", "Color Quantization", 1),
            new FeatureCost("_LUT_3D", "LUT", "LUT", 1),
            new FeatureCost("_HATCHING", "ハッチング", "Hatching", 3),
            new FeatureCost("_WATERCOLOR", "水彩", "Watercolor", 3),
            new FeatureCost("_SCREEN_EDGE", "スクリーンエッジ", "Screen Edge", 1),
            new FeatureCost("_INTERSECTION_FADE", "交差フェード", "Intersection Fade", 1),
            new FeatureCost("_DETAIL_MAP", "ディテールマップ", "Detail Map", 2),
            new FeatureCost("_SURFACE_COVER", "サーフェスカバー", "Surface Cover", 2),
            new FeatureCost("_AUDIOLINK", "AudioLink", "AudioLink", 2),
            new FeatureCost("_VAT", "VAT", "VAT", 2),
            new FeatureCost("_TESS_DISPLACEMENT", "ディスプレイスメント", "Displacement", 1),
            new FeatureCost("_USE_LIGHT_VOLUME", "Light Volume", "Light Volume", 1),
            new FeatureCost("_LTCGI", "LTCGI", "LTCGI", 1)
        };

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

                if (!isEnabled)
                {
                    continue;
                }

                total += feature.SamplerCost;
                contributors.Add(feature);
            }

            contributors.Sort((left, right) =>
            {
                int byCost = right.SamplerCost.CompareTo(left.SamplerCost);
                return byCost != 0 ? byCost : string.CompareOrdinal(left.Keyword, right.Keyword);
            });

            bool hasLightVolume = IsKeywordEnabled(material, "_USE_LIGHT_VOLUME", overrideKeyword, overrideEnabled);
            bool hasLtcgi = IsKeywordEnabled(material, "_LTCGI", overrideKeyword, overrideEnabled);
            bool hasHatching = IsKeywordEnabled(material, "_HATCHING", overrideKeyword, overrideEnabled);

            return new SamplerBudgetEstimate(
                BaseSamplerCount,
                total,
                SamplerLimit,
                contributors.ToArray(),
                hasLightVolume && hasLtcgi,
                hasLightVolume && hasLtcgi && hasHatching);
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
