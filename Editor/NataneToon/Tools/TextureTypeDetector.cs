using UnityEngine;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    public enum TextureEditType
    {
        GrayscaleMask,  // グレースケールマスク (影、SSS、リム等)
        ColorTexture,   // カラーテクスチャ (メイン、デカール等)
        NormalMap,       // 法線マップ
        RampTexture,     // ランプ/グラデーション
        MatCapTexture,   // MatCapテクスチャ
        EyeTexture,      // 瞳テクスチャ
        EmissionMap,     // エミッションマップ
        Unknown
    }

    /// <summary>
    /// Detects texture type from shader property name.
    /// シェーダープロパティ名からテクスチャタイプを自動判定
    /// </summary>
    public static class TextureTypeDetector
    {
        private static readonly Dictionary<string, TextureEditType> exactMatches = new Dictionary<string, TextureEditType>
        {
            { "_MainTex", TextureEditType.ColorTexture },
            { "_BumpMap", TextureEditType.NormalMap },
            { "_DetailNormalMap", TextureEditType.NormalMap },
            { "_ShadowRamp", TextureEditType.RampTexture },
            { "_ShadowRamp2", TextureEditType.RampTexture },
            { "_MatCapTex", TextureEditType.MatCapTexture },
            { "_MatCapTex2", TextureEditType.MatCapTexture },
            { "_MatCapTex3", TextureEditType.MatCapTexture },
            { "_EyeHighlightTex", TextureEditType.EyeTexture },
            { "_EyeIrisTex", TextureEditType.EyeTexture },
            { "_EmissionMap", TextureEditType.EmissionMap },
        };

        private static readonly (string suffix, TextureEditType type)[] suffixRules = new[]
        {
            ("Mask", TextureEditType.GrayscaleMask),
            ("Map", TextureEditType.GrayscaleMask),
            ("Ramp", TextureEditType.RampTexture),
            ("Normal", TextureEditType.NormalMap),
            ("MatCap", TextureEditType.MatCapTexture),
            ("Emission", TextureEditType.EmissionMap),
            ("Decal", TextureEditType.ColorTexture),
            ("Eye", TextureEditType.EyeTexture),
            ("Tex", TextureEditType.ColorTexture),
        };

        /// <summary>
        /// Detect texture type from property name.
        /// プロパティ名からテクスチャタイプを判定
        /// </summary>
        public static TextureEditType Detect(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return TextureEditType.Unknown;

            // Exact match first
            if (exactMatches.TryGetValue(propertyName, out var exactType))
                return exactType;

            // Suffix matching
            foreach (var (suffix, type) in suffixRules)
            {
                if (propertyName.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                    return type;
            }

            return TextureEditType.Unknown;
        }

        /// <summary>
        /// Get the recommended canvas color mode for the texture type.
        /// テクスチャタイプに推奨されるキャンバスカラーモードを取得
        /// </summary>
        public static bool IsGrayscale(TextureEditType type)
        {
            switch (type)
            {
                case TextureEditType.GrayscaleMask:
                case TextureEditType.RampTexture:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get display name for the texture type.
        /// テクスチャタイプの表示名を取得
        /// </summary>
        public static string GetDisplayName(TextureEditType type)
        {
            switch (type)
            {
                case TextureEditType.GrayscaleMask: return "Mask";
                case TextureEditType.ColorTexture: return "Color";
                case TextureEditType.NormalMap: return "Normal";
                case TextureEditType.RampTexture: return "Ramp";
                case TextureEditType.MatCapTexture: return "MatCap";
                case TextureEditType.EyeTexture: return "Eye";
                case TextureEditType.EmissionMap: return "Emission";
                default: return "Unknown";
            }
        }
    }
}
