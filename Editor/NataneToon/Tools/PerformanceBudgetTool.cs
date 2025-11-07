using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// Performance Budget Tool
    /// パフォーマンスバジェットツール
    /// </summary>
    public class PerformanceBudgetTool : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;

        private enum TargetPlatform { PC, Quest, Mobile }
        private TargetPlatform targetPlatform = TargetPlatform.PC;

        private Dictionary<string, int> featureCosts = new Dictionary<string, int>
        {
            { "_SPECULAR", 5 }, { "_RIM_LIGHT", 5 }, { "_SSS", 10 },
            { "_MATCAP", 8 }, { "_EMISSION", 3 }, { "_NORMALMAP", 7 },
            { "_REFLECTION", 12 }, { "_PARALLAX", 15 }, { "_REFRACTION", 20 },
            { "_DISSOLVE", 6 }, { "_HUE_SHIFT", 4 }
        };

        private Dictionary<TargetPlatform, int> platformBudgets = new Dictionary<TargetPlatform, int>
        {
            { TargetPlatform.PC, 100 }, { TargetPlatform.Quest, 40 }, { TargetPlatform.Mobile, 25 }
        };

        [MenuItem("Tools/Natane/Performance Budget Tool", false, 133)]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceBudgetTool>("パフォーマンスバジェット Performance");
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("パフォーマンスバジェットツール Performance Budget Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            targetMaterial = (Material)EditorGUILayout.ObjectField("ターゲット Target", targetMaterial, typeof(Material), false);
            targetPlatform = (TargetPlatform)EditorGUILayout.EnumPopup("プラットフォーム Platform", targetPlatform);

            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox("マテリアルを選択してください\nSelect a material", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawBudgetMeter();
            EditorGUILayout.Space(10);
            DrawFeatureList();
            EditorGUILayout.Space(10);
            DrawRecommendations();

            EditorGUILayout.EndScrollView();
        }

        private void DrawBudgetMeter()
        {
            int currentCost = CalculateCurrentCost();
            int budget = platformBudgets[targetPlatform];
            float percentage = (float)currentCost / budget;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("パフォーマンスコスト Performance Cost", EditorStyles.boldLabel);

            Color meterColor = percentage < 0.7f ? Color.green : percentage < 1f ? Color.yellow : Color.red;
            Rect rect = GUILayoutUtility.GetRect(18, 18f);
            EditorGUI.ProgressBar(rect, percentage, $"{currentCost} / {budget}");

            EditorGUILayout.LabelField($"使用率 Usage: {percentage * 100:F1}%");
            EditorGUILayout.LabelField($"評価 Rating: {GetPerformanceRating(percentage)}");

            EditorGUILayout.EndVertical();
        }

        private void DrawFeatureList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("有効な機能 Active Features", EditorStyles.boldLabel);

            foreach (var feature in featureCosts)
            {
                bool isEnabled = targetMaterial.IsKeywordEnabled(feature.Key);
                if (isEnabled)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(feature.Key.Replace("_", ""), GUILayout.Width(150));
                    EditorGUILayout.LabelField($"コスト Cost: {feature.Value}", GUILayout.Width(100));

                    if (GUILayout.Button("無効化 Disable", GUILayout.Width(100)))
                    {
                        Undo.RecordObject(targetMaterial, "Disable Feature");
                        targetMaterial.DisableKeyword(feature.Key);
                        EditorUtility.SetDirty(targetMaterial);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRecommendations()
        {
            int currentCost = CalculateCurrentCost();
            int budget = platformBudgets[targetPlatform];

            if (currentCost > budget)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("推奨事項 Recommendations", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox(
                    $"バジェット超過: {currentCost - budget}ポイント削減が必要\n" +
                    $"Over budget: Need to reduce by {currentCost - budget} points",
                    MessageType.Warning);

                EditorGUILayout.LabelField("削減候補 Reduction Candidates:");

                var sortedFeatures = new List<KeyValuePair<string, int>>(featureCosts);
                sortedFeatures.Sort((a, b) => b.Value.CompareTo(a.Value));

                foreach (var feature in sortedFeatures)
                {
                    if (targetMaterial.IsKeywordEnabled(feature.Key))
                    {
                        EditorGUILayout.LabelField($"• {feature.Key.Replace("_", "")} (-{feature.Value})");
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        private int CalculateCurrentCost()
        {
            int cost = 10; // Base cost
            foreach (var feature in featureCosts)
            {
                if (targetMaterial.IsKeywordEnabled(feature.Key))
                {
                    cost += feature.Value;
                }
            }
            return cost;
        }

        private string GetPerformanceRating(float percentage)
        {
            if (percentage < 0.5f) return "A (優秀 Excellent)";
            if (percentage < 0.7f) return "B (良好 Good)";
            if (percentage < 1.0f) return "C (許容 Acceptable)";
            if (percentage < 1.5f) return "D (重い Heavy)";
            return "F (過負荷 Overload)";
        }
    }
}
