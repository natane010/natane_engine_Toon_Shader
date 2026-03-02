using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
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

        [MenuItem("Tools/Natane/最適化 Optimization/屈折品質バランサー Refraction Quality Balancer", false, 34)]
        public static void ShowWindow()
        {
            var window = GetWindow<RefractionQualityBalancer>(L("屈折品質バランサー", "Refraction Quality Balancer"));
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(L("屈折品質バランサー", "Refraction Quality Balancer"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L("パフォーマンスと品質のバランスを調整", "Adjust balance between performance and quality"), MessageType.Info);
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField(L("ターゲット", "Target"), targetMaterial, typeof(Material), false);

            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox(L("マテリアルを選択してください", "Please select a material"), MessageType.Info);
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
            EditorGUILayout.LabelField(L("品質プリセット", "Quality Preset"), EditorStyles.boldLabel);

            quality = (QualityPreset)EditorGUILayout.EnumPopup(L("プリセット", "Preset"), quality);

            string description = GetQualityDescription(quality);
            EditorGUILayout.HelpBox(description, MessageType.Info);

            if (GUILayout.Button(L("このプリセットを適用", "Apply Preset"), GUILayout.Height(30)))
            {
                ApplyQualityPreset(quality);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRefractionSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("屈折設定", "Refraction Settings"), EditorStyles.boldLabel);

            string refractionToggleProperty = GetRefractionToggleProperty(targetMaterial);
            if (!string.IsNullOrEmpty(refractionToggleProperty))
            {
                EditorGUI.BeginChangeCheck();
                bool useRefraction = targetMaterial.GetFloat(refractionToggleProperty) > 0.5f;
                useRefraction = EditorGUILayout.Toggle(L("屈折を使用", "Use Refraction"), useRefraction);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Toggle Refraction");
                    SetRefractionEnabled(targetMaterial, useRefraction);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_RefractionIntensity"))
            {
                EditorGUI.BeginChangeCheck();
                float intensity = EditorGUILayout.Slider(L("強度", "Intensity"), targetMaterial.GetFloat("_RefractionIntensity"), 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Refraction Intensity");
                    targetMaterial.SetFloat("_RefractionIntensity", intensity);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            string iorProperty = GetRefractionIorProperty(targetMaterial);
            if (!string.IsNullOrEmpty(iorProperty))
            {
                EditorGUI.BeginChangeCheck();
                float ior = EditorGUILayout.Slider(L("屈折率", "IOR"), targetMaterial.GetFloat(iorProperty), 1f, 3f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change IOR");
                    targetMaterial.SetFloat(iorProperty, ior);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_RefractionSamples"))
            {
                EditorGUI.BeginChangeCheck();
                int samples = EditorGUILayout.IntSlider(L("サンプル数", "Samples"), (int)targetMaterial.GetFloat("_RefractionSamples"), 1, 9);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Refraction Samples");
                    targetMaterial.SetFloat("_RefractionSamples", samples);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }
            else if (targetMaterial.HasProperty("_RefractionBlur"))
            {
                EditorGUI.BeginChangeCheck();
                float blur = EditorGUILayout.Slider(L("ぼかし", "Blur"), targetMaterial.GetFloat("_RefractionBlur"), 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Refraction Blur");
                    targetMaterial.SetFloat("_RefractionBlur", blur);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("パフォーマンス情報", "Performance Info"), EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_RefractionSamples"))
            {
                int samples = (int)targetMaterial.GetFloat("_RefractionSamples");
                string performanceRating = GetPerformanceRating(samples);
                int gpuCost = samples * 10; // Rough estimate

                EditorGUILayout.LabelField($"{L("現在のサンプル数", "Current Samples")}: {samples}");
                EditorGUILayout.LabelField($"{L("推定GPUコスト", "Estimated GPU Cost")}: {gpuCost}%");
                EditorGUILayout.LabelField($"{L("パフォーマンス評価", "Performance Rating")}: {performanceRating}");
            }
            else if (targetMaterial.HasProperty("_RefractionBlur"))
            {
                float blur = targetMaterial.GetFloat("_RefractionBlur");
                string performanceRating = GetBlurPerformanceRating(blur);
                int gpuCost = Mathf.RoundToInt(20f + blur * 20f); // Rough estimate for blur-based path

                EditorGUILayout.LabelField($"{L("現在のぼかし", "Current Blur")}: {blur:F2}");
                EditorGUILayout.LabelField($"{L("推定GPUコスト", "Estimated GPU Cost")}: {gpuCost}%");
                EditorGUILayout.LabelField($"{L("パフォーマンス評価", "Performance Rating")}: {performanceRating}");
            }
            else
            {
                EditorGUILayout.HelpBox(
                    L("このシェーダーには品質指標（Samples/Blur）が見つかりません。", "No quality control property (Samples/Blur) found on this shader."),
                    MessageType.Info);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                L("サンプル数が多いほど品質は向上しますが、パフォーマンスが低下します",
                  "Higher sample count improves quality but decreases performance"),
                MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void ApplyQualityPreset(QualityPreset preset)
        {
            Undo.RecordObject(targetMaterial, "Apply Refraction Quality Preset");

            SetRefractionEnabled(targetMaterial, true);

            switch (preset)
            {
                case QualityPreset.VeryLow:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 1);
                    else if (targetMaterial.HasProperty("_RefractionBlur"))
                        targetMaterial.SetFloat("_RefractionBlur", 0.05f);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 0.3f);
                    break;

                case QualityPreset.Low:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 3);
                    else if (targetMaterial.HasProperty("_RefractionBlur"))
                        targetMaterial.SetFloat("_RefractionBlur", 0.2f);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 0.5f);
                    break;

                case QualityPreset.Medium:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 5);
                    else if (targetMaterial.HasProperty("_RefractionBlur"))
                        targetMaterial.SetFloat("_RefractionBlur", 0.35f);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 0.7f);
                    break;

                case QualityPreset.High:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 7);
                    else if (targetMaterial.HasProperty("_RefractionBlur"))
                        targetMaterial.SetFloat("_RefractionBlur", 0.55f);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 0.9f);
                    break;

                case QualityPreset.VeryHigh:
                    if (targetMaterial.HasProperty("_RefractionSamples"))
                        targetMaterial.SetFloat("_RefractionSamples", 9);
                    else if (targetMaterial.HasProperty("_RefractionBlur"))
                        targetMaterial.SetFloat("_RefractionBlur", 0.75f);
                    if (targetMaterial.HasProperty("_RefractionIntensity"))
                        targetMaterial.SetFloat("_RefractionIntensity", 1f);
                    break;
            }

            EditorUtility.SetDirty(targetMaterial);
            EditorUtility.DisplayDialog(L("適用完了", "Applied"), L($"{preset}を適用しました", $"Applied {preset}"), "OK");
        }

        private string GetQualityDescription(QualityPreset preset)
        {
            switch (preset)
            {
                case QualityPreset.VeryLow:
                    return L("最低品質（サンプル数: 1）- モバイル向け", "Lowest quality (Samples: 1) - For mobile");
                case QualityPreset.Low:
                    return L("低品質（サンプル数: 3）- Quest向け", "Low quality (Samples: 3) - For Quest");
                case QualityPreset.Medium:
                    return L("中品質（サンプル数: 5）- バランス型", "Medium quality (Samples: 5) - Balanced");
                case QualityPreset.High:
                    return L("高品質（サンプル数: 7）- PC向け", "High quality (Samples: 7) - For PC");
                case QualityPreset.VeryHigh:
                    return L("最高品質（サンプル数: 9）- ハイエンドPC向け", "Very high quality (Samples: 9) - For high-end PC");
                default:
                    return "";
            }
        }

        private string GetPerformanceRating(int samples)
        {
            if (samples <= 1) return L("A (非常に軽い)", "A (Very Light)");
            if (samples <= 3) return L("B (軽い)", "B (Light)");
            if (samples <= 5) return L("C (普通)", "C (Normal)");
            if (samples <= 7) return L("D (重い)", "D (Heavy)");
            return L("E (非常に重い)", "E (Very Heavy)");
        }

        private string GetBlurPerformanceRating(float blur)
        {
            if (blur <= 0.1f) return L("A (非常に軽い)", "A (Very Light)");
            if (blur <= 0.3f) return L("B (軽い)", "B (Light)");
            if (blur <= 0.5f) return L("C (普通)", "C (Normal)");
            if (blur <= 0.7f) return L("D (重い)", "D (Heavy)");
            return L("E (非常に重い)", "E (Very Heavy)");
        }

        private static string GetRefractionToggleProperty(Material material)
        {
            if (material == null) return null;
            if (material.HasProperty("_Refraction")) return "_Refraction";
            if (material.HasProperty("_UseRefraction")) return "_UseRefraction";
            return null;
        }

        private static string GetRefractionIorProperty(Material material)
        {
            if (material == null) return null;
            if (material.HasProperty("_RefractionIndex")) return "_RefractionIndex";
            if (material.HasProperty("_IOR")) return "_IOR";
            return null;
        }

        private static void SetRefractionEnabled(Material material, bool enabled)
        {
            if (material == null) return;

            if (material.HasProperty("_Refraction"))
            {
                material.SetFloat("_Refraction", enabled ? 1f : 0f);
            }

            if (material.HasProperty("_UseRefraction"))
            {
                material.SetFloat("_UseRefraction", enabled ? 1f : 0f);
            }

            if (enabled)
            {
                material.EnableKeyword("_REFRACTION");
            }
            else
            {
                material.DisableKeyword("_REFRACTION");
            }
        }
    }
}
