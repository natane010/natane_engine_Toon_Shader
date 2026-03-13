using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;

    /// <summary>
    /// マテリアル分析統合ウィンドウ
    /// Material Validator / Performance Budget / Comparison / Asset Reference の4ツールをタブ統合
    /// </summary>
    public enum MaterialAnalysisTab
    {
        Validator,
        PerformanceBudget,
        Comparison,
        AssetReference
    }

    public class MaterialAnalysisWindow : NataneTabbedToolWindow<MaterialAnalysisTab>
    {
        protected override string WindowTitleJP => "マテリアル分析";
        protected override string WindowTitleEN => "Material Analysis";
        protected override Vector2 DefaultMinSize => new Vector2(650, 550);

        protected override INataneToolTab CreateTab(MaterialAnalysisTab tabEnum)
        {
            switch (tabEnum)
            {
                case MaterialAnalysisTab.Validator:
                    return new ValidatorStubTab();
                case MaterialAnalysisTab.PerformanceBudget:
                    return new PerformanceBudgetStubTab();
                case MaterialAnalysisTab.Comparison:
                    return new ComparisonStubTab();
                case MaterialAnalysisTab.AssetReference:
                    return new AssetReferenceStubTab();
                default:
                    return null;
            }
        }

        // ===== Static API =====

        [MenuItem("Tools/Natane/マテリアル Material/マテリアル分析 Material Analysis", false, 10)]
        public static void ShowWindow()
        {
            var w = GetWindow<MaterialAnalysisWindow>();
            w.OpenToTab(MaterialAnalysisTab.Validator);
        }

        public static void ShowTab(MaterialAnalysisTab tab, Material material = null)
        {
            var w = GetWindow<MaterialAnalysisWindow>();
            w.OpenToTab(tab, material);
        }

        // ===== Stub Tab Adapters =====

        private class ValidatorStubTab : INataneToolTab
        {
            public string TabLabel => L("検証", "Validate");
            public string TabTooltip => L("マテリアル検証ツール", "Material Validator");
            public string HelpToolKey => "MaterialValidator";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                DrawStubMessage(
                    L("マテリアル検証", "Material Validator"),
                    L("MaterialValidator を統合中です。", "MaterialValidator is being integrated."));

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧マテリアル検証ツールを開く", "Open Legacy Material Validator"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("MaterialValidator");
                }
            }
        }

        private class PerformanceBudgetStubTab : INataneToolTab
        {
            public string TabLabel => L("パフォーマンス", "Performance");
            public string TabTooltip => L("パフォーマンスバジェットツール", "Performance Budget Tool");
            public string HelpToolKey => "PerformanceBudget";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                DrawStubMessage(
                    L("パフォーマンスバジェット", "Performance Budget"),
                    L("PerformanceBudgetTool を統合中です。", "PerformanceBudgetTool is being integrated."));

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧パフォーマンスバジェットツールを開く", "Open Legacy Performance Budget Tool"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("PerformanceBudget");
                }
            }
        }

        private class ComparisonStubTab : INataneToolTab
        {
            public string TabLabel => L("比較", "Compare");
            public string TabTooltip => L("マテリアル比較ツール", "Material Comparison Tool");
            public string HelpToolKey => "MaterialComparison";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                DrawStubMessage(
                    L("マテリアル比較", "Material Comparison"),
                    L("MaterialComparisonTool を統合中です。", "MaterialComparisonTool is being integrated."));

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧マテリアル比較ツールを開く", "Open Legacy Material Comparison Tool"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryExecute(NataneToolMenuPaths.MaterialComparison);
                }
            }
        }

        private class AssetReferenceStubTab : INataneToolTab
        {
            public string TabLabel => L("参照チェック", "References");
            public string TabTooltip => L("アセット参照チェッカー", "Asset Reference Checker");
            public string HelpToolKey => "AssetReferenceChecker";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                DrawStubMessage(
                    L("アセット参照チェック", "Asset Reference Check"),
                    L("AssetReferenceChecker を統合中です。", "AssetReferenceChecker is being integrated."));

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧アセット参照チェッカーを開く", "Open Legacy Asset Reference Checker"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("AssetReferenceChecker");
                }
            }
        }

        // ===== Shared Stub Drawing =====

        private static void DrawStubMessage(string title, string message)
        {
            EditorGUILayout.Space(SPACE_LARGE);

            var centeredStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
            EditorGUILayout.LabelField(title, centeredStyle);

            EditorGUILayout.Space(SPACE_SMALL);

            var messageStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            EditorGUILayout.LabelField(message, messageStyle);

            EditorGUILayout.Space(SPACE_LARGE);

            var hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField(
                L("このタブは現在プレースホルダーです。完全な統合は近日実装予定です。",
                  "This tab is currently a placeholder. Full integration coming soon."),
                hintStyle);
        }
    }
}
