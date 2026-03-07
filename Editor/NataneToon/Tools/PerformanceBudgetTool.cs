using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    /// <summary>
    /// Performance Budget Tool
    /// パフォーマンスバジェットツール
    /// </summary>
    public class PerformanceBudgetTool : EditorWindow
    {
        private readonly struct FeatureBudgetEntry
        {
            public FeatureBudgetEntry(string keyword, string propertyName, string labelJa, string labelEn, int gpuCost)
            {
                Keyword = keyword;
                PropertyName = propertyName;
                LabelJa = labelJa;
                LabelEn = labelEn;
                GpuCost = gpuCost;
            }

            public string Keyword { get; }
            public string PropertyName { get; }
            public string LabelJa { get; }
            public string LabelEn { get; }
            public int GpuCost { get; }
        }

        private Material targetMaterial;
        private Vector2 scrollPosition;

        private enum TargetPlatform { PC, Quest, Mobile }
        private TargetPlatform targetPlatform = TargetPlatform.PC;

        private static readonly FeatureBudgetEntry[] FeatureEntries =
        {
            new FeatureBudgetEntry("_2ND_TEXTURE", "_Use2ndTexture", "2nd Texture", "2nd Texture", 4),
            new FeatureBudgetEntry("_3RD_TEXTURE", "_Use3rdTexture", "3rd Texture", "3rd Texture", 4),
            new FeatureBudgetEntry("_4TH_TEXTURE", "_Use4thTexture", "4th Texture", "4th Texture", 4),
            new FeatureBudgetEntry("_5TH_TEXTURE", "_Use5thTexture", "5th Texture", "5th Texture", 4),
            new FeatureBudgetEntry("_SPECULAR", "_Specular", "スペキュラ", "Specular", 5),
            new FeatureBudgetEntry("_HAIR_SPECULAR", "_HairSpecular", "ヘアスペキュラ", "Hair Specular", 6),
            new FeatureBudgetEntry("_RIM_LIGHT", "_RimLight", "リムライト", "Rim Light", 5),
            new FeatureBudgetEntry("_RIM_LIGHT_2", "_RimLight2", "リムライト2", "Rim Light 2", 4),
            new FeatureBudgetEntry("_OFFSET_RIM_LIGHT", "_OffsetRimLight", "オフセットリム", "Offset Rim Light", 4),
            new FeatureBudgetEntry("_SSS", "_SSS", "SSS", "SSS", 10),
            new FeatureBudgetEntry("_MATCAP", "_MatCap", "MatCap", "MatCap", 8),
            new FeatureBudgetEntry("_MATCAP_2", "_MatCap2", "MatCap 2", "MatCap 2", 8),
            new FeatureBudgetEntry("_MATCAP_3", "_MatCap3", "MatCap 3", "MatCap 3", 8),
            new FeatureBudgetEntry("_EMISSION", "_Emission", "エミッション", "Emission", 3),
            new FeatureBudgetEntry("_NORMALMAP", "_UseNormalMap", "法線マップ", "Normal Map", 7),
            new FeatureBudgetEntry("_REFLECTION", "_Reflection", "反射", "Reflection", 12),
            new FeatureBudgetEntry("_ENV_RIM", "_EnvRim", "環境リム", "Environmental Rim", 5),
            new FeatureBudgetEntry("_PARALLAX", "_Parallax", "パララックス", "Parallax", 15),
            new FeatureBudgetEntry("_REFRACTION", "_Refraction", "屈折", "Refraction", 20),
            new FeatureBudgetEntry("_DISSOLVE", "_Dissolve", "ディゾルブ", "Dissolve", 6),
            new FeatureBudgetEntry("_HUE_SHIFT", "_HueShiftEnable", "色相シフト", "Hue Shift", 4),
            new FeatureBudgetEntry("_OUTLINE", "_Outline", "アウトライン", "Outline", 3),
            new FeatureBudgetEntry("_IRIDESCENCE", "_Iridescence", "イリデッセンス", "Iridescence", 6),
            new FeatureBudgetEntry("_GLITTER", "_Glitter", "グリッター", "Glitter", 8),
            new FeatureBudgetEntry("_AUDIOLINK", "_AudioLink", "AudioLink", "AudioLink", 5),
            new FeatureBudgetEntry("_HOLOGRAM", "_Hologram", "ホログラム", "Hologram", 12),
            new FeatureBudgetEntry("_GLITCH", "_Glitch", "グリッチ", "Glitch", 6),
            new FeatureBudgetEntry("_HOLOGRAM_NOISE", "_UseHologramNoise", "ホログラムノイズ", "Hologram Noise", 2),
            new FeatureBudgetEntry("_DECAL", "_Decal", "デカール", "Decal", 3),
            new FeatureBudgetEntry("_VAT", "_VAT", "VAT", "VAT", 6),
            new FeatureBudgetEntry("_VERTEX_ANIMATION", "_VertexAnimation", "頂点アニメーション", "Vertex Animation", 4),
            new FeatureBudgetEntry("_PIXEL_VERTEX_LIGHTS", "_UsePixelVertexLights", "Pixel Vertex Lights", "Pixel Vertex Lights", 3),
            new FeatureBudgetEntry("_DETAIL_MAP", "_DetailMap", "ディテールマップ", "Detail Map", 5),
            new FeatureBudgetEntry("_TRIPLANAR", "_Triplanar", "トライプレーナー", "Triplanar", 10),
            new FeatureBudgetEntry("_HEIGHT_FOG", "_HeightFog", "ハイトフォグ", "Height Fog", 3),
            new FeatureBudgetEntry("_SURFACE_COVER", "_SurfaceCover", "サーフェスカバー", "Surface Cover", 6),
            new FeatureBudgetEntry("_MIRROR_CONTROL", "_MirrorControl", "ミラー制御", "Mirror Control", 1),
            new FeatureBudgetEntry("_WATER_DRIP", "_WaterDrip", "水滴", "Water Drip", 8),
            new FeatureBudgetEntry("_VIDEO_TEXTURE", "_VideoTexture", "ビデオテクスチャ", "Video Texture", 3),
            new FeatureBudgetEntry("_INTERSECTION_FADE", "_IntersectionFade", "交差フェード", "Intersection Fade", 4),
            new FeatureBudgetEntry("_SCREEN_TONE", "_ScreenTone", "スクリーントーン", "Screen Tone", 4),
            new FeatureBudgetEntry("_SCREEN_EDGE", "_UseScreenEdge", "スクリーンエッジ", "Screen Edge", 4),
            new FeatureBudgetEntry("_HATCHING", "_UseHatching", "ハッチング", "Hatching", 8),
            new FeatureBudgetEntry("_USE_LIGHT_VOLUME", "_UseLightVolume", "Light Volume", "Light Volume", 7),
            new FeatureBudgetEntry("_LTCGI", "_LTCGI", "LTCGI", "LTCGI", 7),
            new FeatureBudgetEntry("_WATERCOLOR", "_UseWatercolor", "水彩", "Watercolor", 10),
            new FeatureBudgetEntry("_BACKFACE_TEXTURE", "_BackfaceTexture", "裏面テクスチャ", "Backface Texture", 4),
            new FeatureBudgetEntry("_SMEAR", "_Smear", "スミア", "Smear", 5),
            new FeatureBudgetEntry("_FUR", "_Fur", "Fur", "Fur", 10)
        };

        private static readonly Dictionary<TargetPlatform, int> PlatformBudgets = new Dictionary<TargetPlatform, int>
        {
            { TargetPlatform.PC, 100 }, { TargetPlatform.Quest, 40 }, { TargetPlatform.Mobile, 25 }
        };

        [MenuItem("Tools/Natane/最適化 Optimization/パフォーマンスバジェット Performance Budget Tool", false, 31)]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceBudgetTool>(L("パフォーマンスバジェット", "Performance Budget Tool"));
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            NataneToonShaderGUIUtility.DrawHeaderWithHelp("パフォーマンスバジェットツール", "Performance Budget Tool", "PerformanceBudget");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            targetMaterial = (Material)EditorGUILayout.ObjectField(L("ターゲット", "Target"), targetMaterial, typeof(Material), false);
            targetPlatform = (TargetPlatform)EditorGUILayout.EnumPopup(L("プラットフォーム", "Platform"), targetPlatform);

            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox(L("マテリアルを選択してください", "Please select a material"), MessageType.Info);
                return;
            }

            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate samplerEstimate =
                NataneToonSamplerBudgetEstimator.Estimate(targetMaterial);

            DrawBudgetMeter();
            EditorGUILayout.Space(10);
            NataneToonShaderGUIUtility.DrawPerformanceIndicatorWithSamplerBudget(targetMaterial, samplerEstimate);
            EditorGUILayout.Space(10);
            DrawFeatureList();
            EditorGUILayout.Space(10);
            DrawRecommendations(samplerEstimate);

            EditorGUILayout.EndScrollView();
        }

        private void DrawBudgetMeter()
        {
            int currentCost = CalculateCurrentCost();
            int budget = PlatformBudgets[targetPlatform];
            float percentage = (float)currentCost / budget;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("GPUコスト", "GPU Cost"), EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(18, 18f);
            EditorGUI.ProgressBar(rect, Mathf.Clamp01(percentage), $"{currentCost} / {budget}");

            EditorGUILayout.LabelField($"{L("使用率", "Usage")}: {percentage * 100:F1}%");
            EditorGUILayout.LabelField($"{L("評価", "Rating")}: {GetPerformanceRating(percentage)}");
            EditorGUILayout.LabelField(
                L(
                    "この指標は演算・画面効果寄りの重さを表します。Sampler 制限は下のセクションで別管理です。",
                    "This meter focuses on shading/screen-effect cost. Sampler limits are tracked separately below."),
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawFeatureList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("有効な機能", "Active Features"), EditorStyles.boldLabel);

            List<FeatureBudgetEntry> activeFeatures = GetActiveFeatures();
            if (activeFeatures.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    L("対象の高コスト機能は有効になっていません。", "No tracked heavy features are enabled."),
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (FeatureBudgetEntry feature in activeFeatures)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(GetFeatureDisplayName(feature), GUILayout.Width(170));
                EditorGUILayout.LabelField($"{L("GPU", "GPU")}: {feature.GpuCost}", GUILayout.Width(70));
                EditorGUILayout.LabelField($"{L("Sampler", "Sampler")}: {GetSamplerCostLabel(feature.Keyword)}", GUILayout.Width(95));

                if (GUILayout.Button(L("無効化", "Disable"), GUILayout.Width(100)))
                {
                    DisableFeature(feature);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRecommendations(NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate samplerEstimate)
        {
            int currentCost = CalculateCurrentCost();
            int budget = PlatformBudgets[targetPlatform];
            bool showGpuAdvice = currentCost > budget;
            bool showSamplerAdvice =
                samplerEstimate.IsWarning ||
                samplerEstimate.HasLightVolumeLtcgiCombo ||
                samplerEstimate.HasCriticalLightingCombo ||
                samplerEstimate.HasScreenSpaceLightingCombo;

            if (!showGpuAdvice && !showSamplerAdvice)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("推奨事項", "Recommendations"), EditorStyles.boldLabel);

            if (showGpuAdvice)
            {
                EditorGUILayout.HelpBox(
                    L($"GPUバジェット超過: {currentCost - budget}ポイント削減が必要です",
                      $"GPU budget exceeded: reduce by {currentCost - budget} points"),
                    MessageType.Warning);
            }

            if (samplerEstimate.IsOverLimit)
            {
                EditorGUILayout.HelpBox(
                    L(
                        $"推定 Sampler 上限を超えています ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})",
                        $"Estimated sampler usage is over the limit ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})"),
                    MessageType.Error);
            }
            else if (samplerEstimate.HasCriticalLightingCombo)
            {
                EditorGUILayout.HelpBox(
                    L(
                        "Light Volume + LTCGI + Hatching は危険な組み合わせです。追加レイヤーや髪表現を盛る前に見直してください。",
                        "Light Volume + LTCGI + Hatching is a high-risk combination. Review it before adding more layered effects."),
                    MessageType.Warning);
            }
            else if (samplerEstimate.HasScreenSpaceLightingCombo)
            {
                EditorGUILayout.HelpBox(
                    L(
                        "Light Volume + LTCGI + Screen Edge は危険な組み合わせです。追加テクスチャや髪表現を盛る前に見直してください。",
                        "Light Volume + LTCGI + Screen Edge is a high-risk combination. Review it before adding more texture-heavy effects."),
                    MessageType.Warning);
            }
            else if (samplerEstimate.HasLightVolumeLtcgiCombo)
            {
                EditorGUILayout.HelpBox(
                    L(
                        "Light Volume と LTCGI の併用は Sampler 制限に近づきやすいです。髪や追加テクスチャの前に余裕を確認してください。",
                        "Using Light Volume with LTCGI can quickly approach the sampler limit. Check headroom before adding hair or layered textures."),
                    MessageType.Info);
            }
            else if (samplerEstimate.IsNearLimit)
            {
                EditorGUILayout.HelpBox(
                    L(
                        $"推定 Sampler 数が上限付近です ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})",
                        $"Estimated sampler usage is near the limit ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})"),
                    MessageType.Warning);
            }

            if (samplerEstimate.ExtraPassCount > 0)
            {
                EditorGUILayout.HelpBox(
                    L(
                        $"Screen Edge 分離バリアントにより追加パスが {samplerEstimate.ExtraPassCount} つあります。Sampler 負荷は下がりますが、ドローコールは増えます。",
                        $"The Screen Edge split variant adds {samplerEstimate.ExtraPassCount} extra pass. Sampler pressure is lower, but draw calls increase."),
                    MessageType.Info);
            }

            EditorGUILayout.LabelField(L("見直し候補:", "Review Candidates:"));
            List<FeatureBudgetEntry> activeFeatures = GetActiveFeatures();
            activeFeatures.Sort((a, b) => b.GpuCost.CompareTo(a.GpuCost));

            int candidateCount = Mathf.Min(6, activeFeatures.Count);
            for (int i = 0; i < candidateCount; i++)
            {
                FeatureBudgetEntry feature = activeFeatures[i];
                EditorGUILayout.LabelField(
                    $"• {GetFeatureDisplayName(feature)} ({L("GPU", "GPU")} -{feature.GpuCost}, {L("Sampler", "Sampler")} {GetSamplerCostLabel(feature.Keyword)})");
            }

            EditorGUILayout.EndVertical();
        }

        private int CalculateCurrentCost()
        {
            int cost = 10; // Base cost
            for (int i = 0; i < FeatureEntries.Length; i++)
            {
                if (targetMaterial.IsKeywordEnabled(FeatureEntries[i].Keyword))
                {
                    cost += FeatureEntries[i].GpuCost;
                }
            }
            return cost;
        }

        private string GetPerformanceRating(float percentage)
        {
            if (percentage < 0.5f) return L("A (優秀)", "A (Excellent)");
            if (percentage < 0.7f) return L("B (良好)", "B (Good)");
            if (percentage < 1.0f) return L("C (許容)", "C (Acceptable)");
            if (percentage < 1.5f) return L("D (重い)", "D (Heavy)");
            return L("F (過負荷)", "F (Overload)");
        }

        private List<FeatureBudgetEntry> GetActiveFeatures()
        {
            var activeFeatures = new List<FeatureBudgetEntry>();

            for (int i = 0; i < FeatureEntries.Length; i++)
            {
                if (targetMaterial.IsKeywordEnabled(FeatureEntries[i].Keyword))
                {
                    activeFeatures.Add(FeatureEntries[i]);
                }
            }

            return activeFeatures;
        }

        private void DisableFeature(FeatureBudgetEntry feature)
        {
            Undo.RecordObject(targetMaterial, L("機能を無効化", "Disable Feature"));

            if (!string.IsNullOrEmpty(feature.PropertyName) && targetMaterial.HasProperty(feature.PropertyName))
            {
                targetMaterial.SetFloat(feature.PropertyName, 0.0f);
            }

            targetMaterial.DisableKeyword(feature.Keyword);
            EditorUtility.SetDirty(targetMaterial);
        }

        private string GetFeatureDisplayName(FeatureBudgetEntry feature)
        {
            return L(feature.LabelJa, feature.LabelEn);
        }

        private string GetSamplerCostLabel(string keyword)
        {
            if (!NataneToonSamplerBudgetEstimator.TryGetFeatureCost(keyword, out NataneToonSamplerBudgetEstimator.FeatureCost featureCost))
            {
                return L("対象外", "N/A");
            }

            if (featureCost.SamplerCost <= 0)
            {
                return L("共有", "Shared");
            }

            return $"+{featureCost.SamplerCost}";
        }
    }
}
