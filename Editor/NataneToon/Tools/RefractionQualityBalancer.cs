using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Refraction Quality Balancer
    /// 屈折品質バランサー
    /// </summary>
    public class RefractionQualityBalancer : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;

        private enum QualityPreset { VeryLow, Low, Medium, High, VeryHigh }
        private QualityPreset quality = QualityPreset.Medium;

        [MenuItem("Tools/Natane/Refraction Quality Balancer", false, 137)]
        public static void ShowWindow()
        {
            var window = GetWindow<RefractionQualityBalancer>("屈折品質 Refraction");
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("屈折品質バランサー Refraction Quality Balancer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("パフォーマンスと品質のバランスを調整\nBalance performance vs quality", MessageType.Info);
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField("ターゲット Target", targetMaterial, typeof(Material), false);

            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox("マテリアルを選択してください\nSelect a material", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawQualitySelector();
            EditorGUILayout.Space(10);
            DrawRefractionSettings();
            EditorGUILayout.Space(10);
            DrawPerformanceInfo();

            EditorGUILayout.EndScrollView();
        }

        private void DrawQualitySelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("品質プリセット Quality Preset", EditorStyles.boldLabel);

            quality = (QualityPreset)EditorGUILayout.EnumPopup("プリセット Preset", quality);

            string description = GetQualityDescription(quality);
            EditorGUILayout.HelpBox(description, MessageType.Info);

            if (GUILayout.Button("このプリセットを適用 Apply Preset", GUILayout.Height(30)))
            {
                ApplyQualityPreset(quality);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRefractionSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("屈折設定 Refraction Settings", EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_UseRefraction"))
            {
                EditorGUI.BeginChangeCheck();
                bool useRefraction = targetMaterial.GetFloat("_UseRefraction") > 0.5f;
                useRefraction = EditorGUILayout.Toggle("屈折を使用 Use Refraction", useRefraction);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Toggle Refraction");
                    targetMaterial.SetFloat("_UseRefraction", useRefraction ? 1f : 0f);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_RefractionIntensity"))
            {
                EditorGUI.BeginChangeCheck();
                float intensity = EditorGUILayout.Slider("強度 Intensity", targetMaterial.GetFloat("_RefractionIntensity"), 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Refraction Intensity");
                    targetMaterial.SetFloat("_RefractionIntensity", intensity);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_IOR"))
            {
                EditorGUI.BeginChangeCheck();
                float ior = EditorGUILayout.Slider("屈折率 IOR", targetMaterial.GetFloat("_IOR"), 1f, 3f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change IOR");
                    targetMaterial.SetFloat("_IOR", ior);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_RefractionSamples"))
            {
                EditorGUI.BeginChangeCheck();
                int samples = EditorGUILayout.IntSlider("サンプル数 Samples", (int)targetMaterial.GetFloat("_RefractionSamples"), 1, 9);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Refraction Samples");
                    targetMaterial.SetFloat("_RefractionSamples", samples);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("パフォーマンス情報 Performance Info", EditorStyles.boldLabel);

            int samples = 5; // Default
            if (targetMaterial.HasProperty("_RefractionSamples"))
                samples = (int)targetMaterial.GetFloat("_RefractionSamples");

            string performanceRating = GetPerformanceRating(samples);
            int gpuCost = samples * 10; // Rough estimate

            EditorGUILayout.LabelField($"現在のサンプル数 Current Samples: {samples}");
            EditorGUILayout.LabelField($"推定GPUコスト Estimated GPU Cost: {gpuCost}%");
            EditorGUILayout.LabelField($"パフォーマンス評価 Performance Rating: {performanceRating}");

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "サンプル数が多いほど品質は向上しますが、パフォーマンスが低下します\n" +
                "Higher sample count improves quality but decreases performance",
                MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void ApplyQualityPreset(QualityPreset preset)
        {
            Undo.RecordObject(targetMaterial, "Apply Refraction Quality Preset");

            if (targetMaterial.HasProperty("_UseRefraction"))
                targetMaterial.SetFloat("_UseRefraction", 1f);

            switch (preset)
            {
                case QualityPreset.VeryLow:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 1);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 0.3f);
                    break;

                case QualityPreset.Low:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 3);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 0.5f);
                    break;

                case QualityPreset.Medium:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 5);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 0.7f);
                    break;

                case QualityPreset.High:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 7);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 0.9f);
                    break;

                case QualityPreset.VeryHigh:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 9);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 1f);
                    break;
            }

            EditorUtility.SetDirty(targetMaterial);
            EditorUtility.DisplayDialog("適用完了 Applied", $"{preset}を適用しました\nApplied {preset}", "OK");
        }

        private string GetQualityDescription(QualityPreset preset)
        {
            switch (preset)
            {
                case QualityPreset.VeryLow:
                    return "最低品質（サンプル数: 1）- モバイル向け\nLowest quality (Samples: 1) - For mobile";
                case QualityPreset.Low:
                    return "低品質（サンプル数: 3）- Quest向け\nLow quality (Samples: 3) - For Quest";
                case QualityPreset.Medium:
                    return "中品質（サンプル数: 5）- バランス型\nMedium quality (Samples: 5) - Balanced";
                case QualityPreset.High:
                    return "高品質（サンプル数: 7）- PC向け\nHigh quality (Samples: 7) - For PC";
                case QualityPreset.VeryHigh:
                    return "最高品質（サンプル数: 9）- ハイエンドPC向け\nVery high quality (Samples: 9) - For high-end PC";
                default:
                    return "";
            }
        }

        private string GetPerformanceRating(int samples)
        {
            if (samples <= 1) return "A (非常に軽い Very Light)";
            if (samples <= 3) return "B (軽い Light)";
            if (samples <= 5) return "C (普通 Normal)";
            if (samples <= 7) return "D (重い Heavy)";
            return "E (非常に重い Very Heavy)";
        }
    }
}
