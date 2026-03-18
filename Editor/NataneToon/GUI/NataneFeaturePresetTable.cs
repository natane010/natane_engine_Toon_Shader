using System.Collections.Generic;
using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// Feature × Role optimal parameter table for Stage 2 Role-Aware Toggle.
    /// When a feature is toggled ON, this table provides the best parameters for the current Role × Look.
    /// </summary>
    public static class NataneFeaturePresetTable
    {
        /// <summary>
        /// A set of optimal parameters for a specific feature × role × look combination.
        /// </summary>
        public struct FeaturePreset
        {
            public (string name, float value)[] floatProperties;
            public (string name, Color value)[] colorProperties;
            public bool requiresSDF;
            public bool requiresSmoothNormal;

            /// <summary>Whether this feature is not recommended for this role.</summary>
            public bool notRecommended;

            /// <summary>Reason shown when attempting to enable a non-recommended feature.</summary>
            public string notRecommendedReason;
        }

        // Key: (keyword, role, look)
        private static readonly Dictionary<(string, AutoSetupRole, AutoSetupLook), FeaturePreset> Table
            = new Dictionary<(string, AutoSetupRole, AutoSetupLook), FeaturePreset>();

        // Fallback: (keyword, role) — used when no look-specific entry exists
        private static readonly Dictionary<(string, AutoSetupRole), FeaturePreset> FallbackTable
            = new Dictionary<(string, AutoSetupRole), FeaturePreset>();

        static NataneFeaturePresetTable()
        {
            BuildTable();
        }

        /// <summary>
        /// Get optimal parameters for a feature on a given role and look.
        /// Falls back to role-only, then to generic defaults.
        /// </summary>
        public static FeaturePreset Get(AutoSetupRole role, AutoSetupLook look, string keyword)
        {
            if (Table.TryGetValue((keyword, role, look), out var preset))
                return preset;
            if (FallbackTable.TryGetValue((keyword, role), out preset))
                return preset;
            return default;
        }

        /// <summary>
        /// Check if a feature is recommended for a given role.
        /// </summary>
        public static bool IsRecommended(AutoSetupRole role, AutoSetupLook look, string keyword)
        {
            var preset = Get(role, look, keyword);
            return !preset.notRecommended;
        }

        private static void BuildTable()
        {
            // ===== SSS =====
            SetFallback("_SSS", AutoSetupRole.Face, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_SSSIntensity", 0.6f), ("_SSSPower", 2.0f), ("_SSSDistortion", 0.3f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_SSS", AutoSetupRole.Clothing, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_SSSIntensity", 0.2f), ("_SSSPower", 3.0f), ("_SSSDistortion", 0.1f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetNotRecommended("_SSS", AutoSetupRole.Hair, "髪には SSS は通常使用しません。", "SSS is not typically used for hair.");
            SetNotRecommended("_SSS", AutoSetupRole.Eye, "目には SSS は通常使用しません。", "SSS is not typically used for eyes.");
            SetNotRecommended("_SSS", AutoSetupRole.Metal, "金属には SSS は通常使用しません。", "SSS is not typically used for metal.");

            // ===== Specular =====
            SetFallback("_SPECULAR", AutoSetupRole.Face, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_SpecularSize", 0.08f), ("_SpecularSoftness", 0.15f), ("_SpecularIntensity", 0.5f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_SPECULAR", AutoSetupRole.Clothing, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_SpecularSize", 0.06f), ("_SpecularSoftness", 0.1f), ("_SpecularIntensity", 0.8f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_SPECULAR", AutoSetupRole.Eye, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_SpecularSize", 0.15f), ("_SpecularSoftness", 0.05f), ("_SpecularIntensity", 2.0f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_SPECULAR", AutoSetupRole.Metal, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_SpecularSize", 0.12f), ("_SpecularSoftness", 0.03f), ("_SpecularIntensity", 2.5f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });

            // ===== Hair Specular =====
            SetFallback("_HAIR_SPECULAR", AutoSetupRole.Hair, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_HairSpecShift1", -0.1f), ("_HairSpecShift2", 0.1f),
                    ("_HairSpecWidth1", 0.15f), ("_HairSpecIntensity", 1.5f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetNotRecommended("_HAIR_SPECULAR", AutoSetupRole.Face, "顔には Hair Specular は通常使いません。", "Hair Specular is not typically used for faces.");
            SetNotRecommended("_HAIR_SPECULAR", AutoSetupRole.Clothing, "服には Hair Specular は通常使いません。", "Hair Specular is not typically used for clothing.");
            SetNotRecommended("_HAIR_SPECULAR", AutoSetupRole.Eye, "目には Hair Specular は通常使いません。", "Hair Specular is not typically used for eyes.");
            SetNotRecommended("_HAIR_SPECULAR", AutoSetupRole.Metal, "金属には Hair Specular は通常使いません。", "Hair Specular is not typically used for metal.");

            // ===== Rim Light =====
            SetFallback("_RIM_LIGHT", AutoSetupRole.Face, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_RimPower", 3.0f), ("_RimIntensity", 0.8f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_RIM_LIGHT", AutoSetupRole.Hair, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_RimPower", 2.0f), ("_RimIntensity", 2.0f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_RIM_LIGHT", AutoSetupRole.Clothing, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_RimPower", 2.5f), ("_RimIntensity", 1.0f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_RIM_LIGHT", AutoSetupRole.Metal, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_RimPower", 2.0f), ("_RimIntensity", 1.5f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetNotRecommended("_RIM_LIGHT", AutoSetupRole.Eye, "目にはリムライトは通常使用しません。", "Rim Light is not typically used for eyes.");

            // ===== Outline =====
            SetFallback("_OUTLINE", AutoSetupRole.Face, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_OutlineWidth", 0.03f), ("_OutlineTexColorBlend", 0.8f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
                requiresSmoothNormal = true,
            });
            SetFallback("_OUTLINE", AutoSetupRole.Hair, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_OutlineWidth", 0.06f), ("_OutlineTexColorBlend", 0.9f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
                requiresSmoothNormal = true,
            });
            SetFallback("_OUTLINE", AutoSetupRole.Clothing, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_OutlineWidth", 0.08f), ("_OutlineTexColorBlend", 0.8f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
                requiresSmoothNormal = true,
            });
            SetFallback("_OUTLINE", AutoSetupRole.Metal, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_OutlineWidth", 0.04f), ("_OutlineTexColorBlend", 0.8f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
                requiresSmoothNormal = true,
            });
            SetNotRecommended("_OUTLINE", AutoSetupRole.Eye, "目にはアウトラインは通常使用しません。", "Outline is not typically used for eyes.");

            // ===== MatCap =====
            SetFallback("_MAT_CAP", AutoSetupRole.Face, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_MatCapIntensity", 0.3f), ("_MatCapBlendMode", 0f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_MAT_CAP", AutoSetupRole.Hair, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_MatCapIntensity", 0.4f), ("_MatCapBlendMode", 1f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_MAT_CAP", AutoSetupRole.Clothing, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_MatCapIntensity", 0.2f), ("_MatCapBlendMode", 2f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_MAT_CAP", AutoSetupRole.Eye, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_MatCapIntensity", 0.4f), ("_MatCapBlendMode", 1f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_MAT_CAP", AutoSetupRole.Metal, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_MatCapIntensity", 0.7f), ("_MatCapBlendMode", 1f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });

            // ===== Emission =====
            SetFallback("_EMISSION", AutoSetupRole.Face, new FeaturePreset
            {
                floatProperties = new[] { ("_EmissionGlow", 0.1f) },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_EMISSION", AutoSetupRole.Hair, new FeaturePreset
            {
                floatProperties = new[] { ("_EmissionGlow", 0.05f) },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_EMISSION", AutoSetupRole.Clothing, new FeaturePreset
            {
                floatProperties = new[] { ("_EmissionGlow", 0.1f) },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_EMISSION", AutoSetupRole.Eye, new FeaturePreset
            {
                floatProperties = new[] { ("_EmissionGlow", 0.3f) },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_EMISSION", AutoSetupRole.Metal, new FeaturePreset
            {
                floatProperties = new[] { ("_EmissionGlow", 0.2f) },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });

            // ===== Reflection =====
            SetFallback("_REFLECTION", AutoSetupRole.Metal, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_ReflectionIntensity", 0.6f), ("_ReflectionSmoothness", 0.8f), ("_Metallic", 0.9f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetNotRecommended("_REFLECTION", AutoSetupRole.Face, "顔には Reflection は通常使用しません。", "Reflection is not typically used for faces.");
            SetNotRecommended("_REFLECTION", AutoSetupRole.Hair, "髪には Reflection は通常使用しません。", "Reflection is not typically used for hair.");
            SetNotRecommended("_REFLECTION", AutoSetupRole.Clothing, "服には Reflection は通常使用しません。", "Reflection is not typically used for clothing.");
            SetNotRecommended("_REFLECTION", AutoSetupRole.Eye, "目には Reflection は通常使用しません。", "Reflection is not typically used for eyes.");

            // ===== Parallax =====
            SetFallback("_PARALLAX", AutoSetupRole.Eye, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_ParallaxScale", 0.02f), ("_EyeDepth", 0.15f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetNotRecommended("_PARALLAX", AutoSetupRole.Face, "顔には Parallax は通常使用しません。", "Parallax is not typically used for faces.");
            SetNotRecommended("_PARALLAX", AutoSetupRole.Hair, "髪には Parallax は通常使用しません。", "Parallax is not typically used for hair.");
            SetNotRecommended("_PARALLAX", AutoSetupRole.Clothing, "服には Parallax は通常使用しません。", "Parallax is not typically used for clothing.");
            SetNotRecommended("_PARALLAX", AutoSetupRole.Metal, "金属には Parallax は通常使用しません。", "Parallax is not typically used for metal.");

            // ===== EnvRim =====
            SetFallback("_ENV_RIM", AutoSetupRole.Face, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_EnvRimPower", 3.0f), ("_EnvRimIntensity", 0.3f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_ENV_RIM", AutoSetupRole.Hair, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_EnvRimPower", 2.5f), ("_EnvRimIntensity", 0.5f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_ENV_RIM", AutoSetupRole.Clothing, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_EnvRimPower", 3.0f), ("_EnvRimIntensity", 0.3f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetFallback("_ENV_RIM", AutoSetupRole.Metal, new FeaturePreset
            {
                floatProperties = new[]
                {
                    ("_EnvRimPower", 2.0f), ("_EnvRimIntensity", 0.6f),
                },
                colorProperties = System.Array.Empty<(string, Color)>(),
            });
            SetNotRecommended("_ENV_RIM", AutoSetupRole.Eye, "目には環境リムは通常使用しません。", "Environment Rim is not typically used for eyes.");
        }

        // ===== Helpers =====

        private static void SetFallback(string keyword, AutoSetupRole role, FeaturePreset preset)
        {
            FallbackTable[(keyword, role)] = preset;
        }

        private static void SetNotRecommended(string keyword, AutoSetupRole role, string reasonJa, string reasonEn)
        {
            FallbackTable[(keyword, role)] = new FeaturePreset
            {
                notRecommended = true,
                notRecommendedReason = NataneToonLocalization.L(reasonJa, reasonEn),
                floatProperties = System.Array.Empty<(string, float)>(),
                colorProperties = System.Array.Empty<(string, Color)>(),
            };
        }
    }
}
