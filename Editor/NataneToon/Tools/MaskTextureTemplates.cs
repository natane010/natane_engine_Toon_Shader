using UnityEngine;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    // ================================================================
    // Template Types
    // ================================================================

    internal enum MaskTemplate
    {
        FaceSSS,
        HairRimLight,
        ClothAlpha,
        BodyOutline,
        EyeHighlight,
        SkinSubsurface,
        MetallicMask,
        EmissionGlow,
    }

    // ================================================================
    // Template Info
    // ================================================================

    internal struct MaskTemplateInfo
    {
        public string nameJP;
        public string nameEN;
        public string descriptionJP;
        public string descriptionEN;
        public bool requiresMesh;
    }

    // ================================================================
    // MaskTextureTemplates
    // テンプレートプリセット - レイヤー構成を自動生成
    // ================================================================

    internal static class MaskTextureTemplates
    {
        /// <summary>
        /// Get info for a template type.
        /// テンプレートタイプの情報を取得
        /// </summary>
        public static MaskTemplateInfo GetTemplateInfo(MaskTemplate template)
        {
            switch (template)
            {
                case MaskTemplate.FaceSSS:
                    return new MaskTemplateInfo
                    {
                        nameJP = "顔SSS",
                        nameEN = "Face SSS",
                        descriptionJP = "顔の中心から放射状グラデーション + ブラーで肌の透過光マスクを生成",
                        descriptionEN = "Radial gradient from face center + blur for skin subsurface scattering mask",
                        requiresMesh = false
                    };

                case MaskTemplate.HairRimLight:
                    return new MaskTemplateInfo
                    {
                        nameJP = "髪リムライト",
                        nameEN = "Hair Rim Light",
                        descriptionJP = "法線方向(カメラ向き)ベースで髪のリムライトマスクを生成",
                        descriptionEN = "Normal direction based rim light mask for hair",
                        requiresMesh = true
                    };

                case MaskTemplate.ClothAlpha:
                    return new MaskTemplateInfo
                    {
                        nameJP = "服アルファ",
                        nameEN = "Cloth Alpha",
                        descriptionJP = "リニアグラデーションで服のアルファマスクを生成",
                        descriptionEN = "Linear gradient for cloth alpha mask",
                        requiresMesh = false
                    };

                case MaskTemplate.BodyOutline:
                    return new MaskTemplateInfo
                    {
                        nameJP = "体アウトライン",
                        nameEN = "Body Outline",
                        descriptionJP = "放射状グラデーション(エッジ)でアウトライン太さマスクを生成",
                        descriptionEN = "Radial gradient for outline width mask",
                        requiresMesh = false
                    };

                case MaskTemplate.EyeHighlight:
                    return new MaskTemplateInfo
                    {
                        nameJP = "目ハイライト",
                        nameEN = "Eye Highlight",
                        descriptionJP = "小さな放射状グラデーションで目のスペキュラマスクを生成",
                        descriptionEN = "Small radial gradient for eye specular highlight mask",
                        requiresMesh = false
                    };

                case MaskTemplate.SkinSubsurface:
                    return new MaskTemplateInfo
                    {
                        nameJP = "肌サブサーフェス",
                        nameEN = "Skin Subsurface",
                        descriptionJP = "曲率ベースでサブサーフェススキャタリングマスクを生成",
                        descriptionEN = "Curvature-based subsurface scattering mask",
                        requiresMesh = true
                    };

                case MaskTemplate.MetallicMask:
                    return new MaskTemplateInfo
                    {
                        nameJP = "メタリックマスク",
                        nameEN = "Metallic Mask",
                        descriptionJP = "法線方向(上向き)ベースでメタリックマスクを生成",
                        descriptionEN = "Normal direction (upward-facing) based metallic mask",
                        requiresMesh = true
                    };

                case MaskTemplate.EmissionGlow:
                    return new MaskTemplateInfo
                    {
                        nameJP = "エミッション発光",
                        nameEN = "Emission Glow",
                        descriptionJP = "放射状 + 角度グラデーションの複合でエミッションマスクを生成",
                        descriptionEN = "Radial + angular gradient combo for emission glow mask",
                        requiresMesh = false
                    };

                default:
                    return new MaskTemplateInfo
                    {
                        nameJP = "不明",
                        nameEN = "Unknown",
                        descriptionJP = "",
                        descriptionEN = "",
                        requiresMesh = false
                    };
            }
        }

        /// <summary>
        /// Get all template infos for display.
        /// 表示用の全テンプレート情報を取得
        /// </summary>
        public static List<(MaskTemplate template, MaskTemplateInfo info)> GetAllTemplateInfos()
        {
            var result = new List<(MaskTemplate, MaskTemplateInfo)>();
            var values = System.Enum.GetValues(typeof(MaskTemplate));
            foreach (MaskTemplate t in values)
            {
                result.Add((t, GetTemplateInfo(t)));
            }
            return result;
        }

        /// <summary>
        /// Apply a template to a MaskLayerStack. Generates pixel data into new layers.
        /// テンプレートをMaskLayerStackに適用。新しいレイヤーにピクセルデータを生成
        /// </summary>
        public static void ApplyTemplate(MaskTemplate template, MaskLayerStack stack, int textureSize,
            Mesh mesh = null, List<Vector2> uvs = null)
        {
            switch (template)
            {
                case MaskTemplate.FaceSSS:
                    ApplyFaceSSS(stack, textureSize);
                    break;
                case MaskTemplate.HairRimLight:
                    ApplyHairRimLight(stack, textureSize, mesh, uvs);
                    break;
                case MaskTemplate.ClothAlpha:
                    ApplyClothAlpha(stack, textureSize);
                    break;
                case MaskTemplate.BodyOutline:
                    ApplyBodyOutline(stack, textureSize);
                    break;
                case MaskTemplate.EyeHighlight:
                    ApplyEyeHighlight(stack, textureSize);
                    break;
                case MaskTemplate.SkinSubsurface:
                    ApplySkinSubsurface(stack, textureSize, mesh, uvs);
                    break;
                case MaskTemplate.MetallicMask:
                    ApplyMetallicMask(stack, textureSize, mesh, uvs);
                    break;
                case MaskTemplate.EmissionGlow:
                    ApplyEmissionGlow(stack, textureSize);
                    break;
            }
        }

        // ================================================================
        // Template Implementations - generate actual pixel data
        // ================================================================

        private static void ApplyFaceSSS(MaskLayerStack stack, int size)
        {
            var layer = stack.AddLayer("Face SSS - Radial", size, size);
            layer.sourceType = MaskTextureLayer.SourceType.Gradient;

            GradientGenerator.Generate(layer.pixels, size, GradientType.Radial, new GradientParams
            {
                center = new Vector2(0.5f, 0.6f),
                radius = 0.4f,
                curve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                invert = false
            });

            MaskTextureFilters.GaussianBlur(layer.pixels, size, size, 3f);
        }

        private static void ApplyHairRimLight(MaskLayerStack stack, int size, Mesh mesh, List<Vector2> uvs)
        {
            if (mesh == null || uvs == null || uvs.Count == 0)
            {
                Debug.LogWarning("[NataneToon] Hair Rim Light テンプレートにはメッシュデータが必要です。");
                return;
            }

            var layer = stack.AddLayer("Hair Rim - Normal Direction", size, size);
            layer.sourceType = MaskTextureLayer.SourceType.MeshInfo;

            MeshInfoGenerator.GenerateNormalDirection(layer.pixels, size, mesh, uvs,
                new Vector3(0, 0, 1), 0.3f);

            MaskTextureFilters.GaussianBlur(layer.pixels, size, size, 1f);
        }

        private static void ApplyClothAlpha(MaskLayerStack stack, int size)
        {
            var layer = stack.AddLayer("Cloth Alpha - Linear Gradient", size, size);
            layer.sourceType = MaskTextureLayer.SourceType.Gradient;

            GradientGenerator.Generate(layer.pixels, size, GradientType.Linear, new GradientParams
            {
                angle = 90f,
                center = new Vector2(0.5f, 0.5f),
                curve = AnimationCurve.Linear(0, 1, 1, 0),
                invert = false
            });
        }

        private static void ApplyBodyOutline(MaskLayerStack stack, int size)
        {
            var layer = stack.AddLayer("Outline Base - Radial", size, size);
            layer.sourceType = MaskTextureLayer.SourceType.Gradient;

            GradientGenerator.Generate(layer.pixels, size, GradientType.Radial, new GradientParams
            {
                center = new Vector2(0.5f, 0.5f),
                radius = 0.7f,
                curve = AnimationCurve.Linear(0, 0.3f, 1, 1),
                invert = true
            });
        }

        private static void ApplyEyeHighlight(MaskLayerStack stack, int size)
        {
            var layer = stack.AddLayer("Eye Highlight - Radial", size, size);
            layer.sourceType = MaskTextureLayer.SourceType.Gradient;

            GradientGenerator.Generate(layer.pixels, size, GradientType.Radial, new GradientParams
            {
                center = new Vector2(0.5f, 0.55f),
                radius = 0.15f,
                curve = new AnimationCurve(
                    new Keyframe(0, 0),
                    new Keyframe(0.7f, 0),
                    new Keyframe(1, 1)
                ),
                invert = false
            });

            MaskTextureFilters.GaussianBlur(layer.pixels, size, size, 2f);
        }

        private static void ApplySkinSubsurface(MaskLayerStack stack, int size, Mesh mesh, List<Vector2> uvs)
        {
            if (mesh == null || uvs == null || uvs.Count == 0)
            {
                Debug.LogWarning("[NataneToon] Skin Subsurface テンプレートにはメッシュデータが必要です。");
                return;
            }

            var layer = stack.AddLayer("Skin SSS - Curvature", size, size);
            layer.sourceType = MaskTextureLayer.SourceType.MeshInfo;
            layer.opacity = 0.8f;

            MeshInfoGenerator.GenerateCurvature(layer.pixels, size, mesh, uvs, 1.5f);

            MaskTextureFilters.GaussianBlur(layer.pixels, size, size, 2f);
        }

        private static void ApplyMetallicMask(MaskLayerStack stack, int size, Mesh mesh, List<Vector2> uvs)
        {
            if (mesh == null || uvs == null || uvs.Count == 0)
            {
                Debug.LogWarning("[NataneToon] Metallic Mask テンプレートにはメッシュデータが必要です。");
                return;
            }

            var layer = stack.AddLayer("Metallic - Normal Up", size, size);
            layer.sourceType = MaskTextureLayer.SourceType.MeshInfo;

            MeshInfoGenerator.GenerateNormalDirection(layer.pixels, size, mesh, uvs,
                Vector3.up, 0.5f);

            MaskTextureFilters.GaussianBlur(layer.pixels, size, size, 1f);
        }

        private static void ApplyEmissionGlow(MaskLayerStack stack, int size)
        {
            // Layer 1: Radial base
            var radialLayer = stack.AddLayer("Emission - Radial Base", size, size);
            radialLayer.sourceType = MaskTextureLayer.SourceType.Gradient;

            GradientGenerator.Generate(radialLayer.pixels, size, GradientType.Radial, new GradientParams
            {
                center = new Vector2(0.5f, 0.5f),
                radius = 0.5f,
                curve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                invert = false
            });

            // Layer 2: Angular modulation
            var angularLayer = stack.AddLayer("Emission - Angular Modulation", size, size);
            angularLayer.sourceType = MaskTextureLayer.SourceType.Gradient;
            angularLayer.blendMode = MaskBlendMode.Multiply;
            angularLayer.opacity = 0.7f;

            GradientGenerator.Generate(angularLayer.pixels, size, GradientType.Angular, new GradientParams
            {
                center = new Vector2(0.5f, 0.5f),
                angle = 0f,
                curve = new AnimationCurve(
                    new Keyframe(0, 0.5f),
                    new Keyframe(0.25f, 1f),
                    new Keyframe(0.5f, 0.5f),
                    new Keyframe(0.75f, 1f),
                    new Keyframe(1, 0.5f)
                ),
                invert = false
            });
        }
    }
}
