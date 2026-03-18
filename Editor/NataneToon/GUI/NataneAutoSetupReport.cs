using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    /// <summary>
    /// Setup result data returned by the auto-setup pipeline.
    /// </summary>
    public class AutoSetupResult
    {
        public bool success;
        public AutoSetupRole role;
        public AutoSetupLook look;
        public AutoSetupQuality quality;
        public List<string> enabledFeatures = new List<string>();
        public List<string> warnings = new List<string>();
        public bool sdfGenerated;
        public bool smoothNormalsBaked;
        public NataneTextureAnalyzer.ColorAnalysisResult colorResult;
        public NataneMeshAnalyzer.MeshAnalysisResult meshResult;
        public int samplerCount;
        public string performanceRating;
    }

    /// <summary>
    /// Draws a compact summary UI for the auto-setup result.
    /// </summary>
    public static class NataneAutoSetupReport
    {
        /// <summary>
        /// Draw the result summary in the inspector.
        /// </summary>
        public static void DrawResultSummary(AutoSetupResult result)
        {
            if (result == null || !result.success) return;

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Header
                EditorGUILayout.LabelField(
                    L("自動セットアップ結果", "Auto Setup Result"),
                    EditorStyles.boldLabel);

                // Role / Look / Quality
                EditorGUILayout.LabelField(
                    $"{GetRoleName(result.role)} × {GetLookName(result.look)}  |  {GetQualityName(result.quality)}",
                    EditorStyles.miniLabel);

                EditorGUILayout.Space(2);

                // Enabled features
                string features = string.Join(", ", result.enabledFeatures);
                EditorGUILayout.LabelField(L("有効機能: ", "Enabled: ") + features, EditorStyles.wordWrappedMiniLabel);

                // Generated data
                if (result.sdfGenerated || result.smoothNormalsBaked)
                {
                    string generated = "";
                    if (result.sdfGenerated)
                        generated += "SDF ";
                    if (result.smoothNormalsBaked)
                        generated += "SmoothNormals ";
                    EditorGUILayout.LabelField(L("自動生成: ", "Generated: ") + generated, EditorStyles.miniLabel);
                }

                // UV analysis info
                if (result.meshResult.valid && result.meshResult.uvResult.valid)
                {
                    var uv = result.meshResult.uvResult;
                    string uvInfo = $"{L("テクセル密度", "Texel Density")}: {uv.texelDensity:F0}  |  "
                        + $"{L("UVシーム", "UV Seams")}: {uv.seamCount} ({uv.seamRatio:P0})";
                    if (uv.hasUV1)
                        uvInfo += $"  |  UV1: {L("あり", "Yes")}";
                    if (uv.hasVertexColors)
                        uvInfo += $"  |  {L("頂点色", "VCol")}: {L("あり", "Yes")}";
                    EditorGUILayout.LabelField(uvInfo, EditorStyles.miniLabel);
                }

                // Performance
                if (!string.IsNullOrEmpty(result.performanceRating))
                {
                    Color ratingColor = GetRatingColor(result.performanceRating);
                    var oldColor = GUI.contentColor;
                    GUI.contentColor = ratingColor;
                    EditorGUILayout.LabelField(
                        $"{L("パフォーマンス", "Performance")}: {result.performanceRating}  |  {L("サンプラー", "Samplers")}: {result.samplerCount}",
                        EditorStyles.miniBoldLabel);
                    GUI.contentColor = oldColor;
                }

                // Warnings
                foreach (string warning in result.warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
        }

        /// <summary>
        /// Calculate a simple performance rating based on enabled feature count.
        /// </summary>
        public static string CalculatePerformanceRating(Material mat)
        {
            if (mat == null) return "?";
            int featureCount = CountEnabledFeatures(mat);
            if (featureCount <= 3) return "A";
            if (featureCount <= 6) return "B";
            if (featureCount <= 9) return "C";
            return "D";
        }

        /// <summary>
        /// Count texture sampler slots in use.
        /// </summary>
        public static int CountSamplers(Material mat)
        {
            if (mat == null) return 0;
            int count = 0;
            string[] samplerProps = new[]
            {
                "_MainTex", "_BumpMap", "_SDFMap", "_ShadowReceiveMask",
                "_SpecularMask", "_RimMask", "_MatCapTex", "_MatCapMask",
                "_EmissionMap", "_EmissionMask", "_ThickMap", "_SSSMask",
                "_GlitterMask", "_ReflectionCube", "_SheenMask",
                "_MakeupTex2nd", "_MakeupTex3rd", "_MakeupTex4th",
            };
            foreach (string prop in samplerProps)
            {
                if (mat.HasProperty(prop) && mat.GetTexture(prop) != null)
                    count++;
            }
            return count;
        }

        private static int CountEnabledFeatures(Material mat)
        {
            int count = 0;
            string[] featureProps = new[]
            {
                "_Specular", "_HairSpecular", "_RimLight", "_SSS", "_MatCap",
                "_Glitter", "_Reflection", "_Iridescence", "_EnvRim",
                "_Outline", "_Emission", "_UseNormalMap", "_Parallax",
            };
            foreach (string prop in featureProps)
            {
                if (mat.HasProperty(prop) && mat.GetFloat(prop) > 0.5f)
                    count++;
            }
            return count;
        }

        // ===== Display helpers =====

        public static string GetRoleName(AutoSetupRole role)
        {
            switch (role)
            {
                case AutoSetupRole.Face: return L("顔/肌", "Face/Skin");
                case AutoSetupRole.Hair: return L("髪", "Hair");
                case AutoSetupRole.Clothing: return L("服/布", "Clothing");
                case AutoSetupRole.Eye: return L("目", "Eye");
                case AutoSetupRole.Metal: return L("金属", "Metal");
                default: return role.ToString();
            }
        }

        public static string GetLookName(AutoSetupLook look)
        {
            switch (look)
            {
                case AutoSetupLook.Anime: return L("アニメ風", "Anime");
                case AutoSetupLook.GameCharacter: return L("ゲーム風", "Game Character");
                case AutoSetupLook.SemiRealistic: return L("セミリアル", "Semi-Realistic");
                default: return look.ToString();
            }
        }

        public static string GetQualityName(AutoSetupQuality quality)
        {
            switch (quality)
            {
                case AutoSetupQuality.High: return L("高品質", "High");
                case AutoSetupQuality.Standard: return L("標準", "Standard");
                case AutoSetupQuality.Mobile: return L("モバイル", "Mobile");
                default: return quality.ToString();
            }
        }

        private static Color GetRatingColor(string rating)
        {
            switch (rating)
            {
                case "A": return new Color(0.3f, 0.9f, 0.3f);
                case "B": return new Color(0.6f, 0.9f, 0.3f);
                case "C": return new Color(0.9f, 0.7f, 0.2f);
                case "D": return new Color(0.9f, 0.3f, 0.3f);
                default: return Color.white;
            }
        }
    }
}
