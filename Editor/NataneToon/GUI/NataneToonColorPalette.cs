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

        // Section Category Colors (Inspector UI)
        public static readonly Color SectionBasic = new Color(0.35f, 0.70f, 0.95f);
        public static readonly Color SectionShading = new Color(0.55f, 0.45f, 0.85f);
        public static readonly Color SectionLighting = new Color(0.95f, 0.75f, 0.30f);
        public static readonly Color SectionEffects = new Color(0.40f, 0.85f, 0.55f);
        public static readonly Color SectionEnvironment = new Color(0.45f, 0.80f, 0.90f);
        public static readonly Color SectionAdvanced = new Color(0.75f, 0.55f, 0.55f);

        // Toggle Badge Colors
        public static readonly Color ToggleOn = new Color(0.3f, 0.8f, 0.3f);
        public static readonly Color ToggleOff = new Color(0.5f, 0.5f, 0.5f);

        public static Color PanelBackground
        {
            get
            {
                return UnityEditor.EditorGUIUtility.isProSkin
                    ? new Color(0.16f, 0.17f, 0.19f, 1f)
                    : new Color(0.92f, 0.93f, 0.95f, 1f);
            }
        }

        public static Color PanelBackgroundElevated
        {
            get
            {
                return UnityEditor.EditorGUIUtility.isProSkin
                    ? new Color(0.205f, 0.215f, 0.235f, 1f)
                    : new Color(0.975f, 0.978f, 0.985f, 1f);
            }
        }

        public static Color Divider
        {
            get
            {
                return UnityEditor.EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.10f)
                    : new Color(0f, 0f, 0f, 0.12f);
            }
        }

        public static Color PrimaryText
        {
            get
            {
                return UnityEditor.EditorGUIUtility.isProSkin
                    ? new Color(0.91f, 0.92f, 0.94f, 1f)
                    : new Color(0.16f, 0.17f, 0.19f, 1f);
            }
        }

        public static Color SecondaryText
        {
            get
            {
                return UnityEditor.EditorGUIUtility.isProSkin
                    ? new Color(0.68f, 0.70f, 0.74f, 1f)
                    : new Color(0.35f, 0.37f, 0.42f, 1f);
            }
        }

        public static Color DisabledText
        {
            get
            {
                return UnityEditor.EditorGUIUtility.isProSkin
                    ? new Color(0.50f, 0.52f, 0.56f, 1f)
                    : new Color(0.52f, 0.54f, 0.58f, 1f);
            }
        }

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
