using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// Natane Toon Shader統一カラーパレット
    /// Unified color palette for consistent UI design
    /// </summary>
    public static class NataneToonColorPalette
    {
        // Primary Colors
        public static readonly Color BrandPrimary = new Color(0.3f, 0.7f, 1.0f);
        public static readonly Color BrandSecondary = new Color(0.2f, 0.5f, 0.8f);

        // Status Colors
        public static readonly Color Success = new Color(0.3f, 0.8f, 0.3f);
        public static readonly Color Warning = new Color(1.0f, 0.7f, 0.2f);
        public static readonly Color Error = new Color(0.9f, 0.3f, 0.3f);
        public static readonly Color Info = new Color(0.4f, 0.6f, 1.0f);

        // Performance Rating Colors
        public static readonly Color PerformanceA = new Color(0.2f, 0.9f, 0.2f);
        public static readonly Color PerformanceB = new Color(0.6f, 0.9f, 0.3f);
        public static readonly Color PerformanceC = new Color(1.0f, 0.8f, 0.2f);
        public static readonly Color PerformanceD = new Color(1.0f, 0.4f, 0.2f);

        // Text Colors
        public static readonly Color TextPrimary = new Color(0.9f, 0.9f, 0.9f);
        public static readonly Color TextSecondary = new Color(0.7f, 0.7f, 0.7f);
        public static readonly Color TextDisabled = new Color(0.5f, 0.5f, 0.5f);

        /// <summary>
        /// Get performance rating color by rating string
        /// パフォーマンス評価文字列から色を取得
        /// </summary>
        public static Color GetPerformanceColor(string rating)
        {
            switch (rating?.ToUpper())
            {
                case "A": return PerformanceA;
                case "B": return PerformanceB;
                case "C": return PerformanceC;
                case "D": return PerformanceD;
                default: return TextDisabled;
            }
        }
    }
}
