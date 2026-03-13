using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;

    /// <summary>
    /// 最適化ハブウィンドウのタブ列挙型
    /// </summary>
    public enum OptimizationHubTab
    {
        TextureOptimizer,
        OutlineOptimizer,
        RefractionBalancer
    }

    /// <summary>
    /// Optimization Hub Window
    /// 最適化ハブ
    /// Consolidates 3 optimization tools into a single tabbed window:
    ///   Texture Optimizer / Outline Optimizer / Refraction Quality Balancer
    /// </summary>
    public class OptimizationHubWindow : NataneTabbedToolWindow<OptimizationHubTab>
    {
        // ===== Window metadata =====
        protected override string WindowTitleJP => "最適化ハブ";
        protected override string WindowTitleEN => "Optimization Hub";
        protected override Vector2 DefaultMinSize => new Vector2(600, 500);

        // ===== Tab factory =====
        protected override INataneToolTab CreateTab(OptimizationHubTab tab)
        {
            switch (tab)
            {
                case OptimizationHubTab.TextureOptimizer:    return new TextureOptimizerStubTab();
                case OptimizationHubTab.OutlineOptimizer:    return new OutlineOptimizerStubTab();
                case OptimizationHubTab.RefractionBalancer:  return new RefractionBalancerStubTab();
                default:                                     return new TextureOptimizerStubTab();
            }
        }

        // ===== Menu items =====
        [MenuItem("Tools/Natane/最適化 Optimization/最適化ハブ Optimization Hub", false, 10)]
        public static void ShowWindow()
        {
            var w = GetWindow<OptimizationHubWindow>();
            w.OpenToTab(OptimizationHubTab.TextureOptimizer);
        }

        /// <summary>
        /// 指定タブを開いた状態でウィンドウを表示する。
        /// オプションでマテリアルコンテキストを渡せる。
        /// </summary>
        public static void ShowTab(OptimizationHubTab tab, Material material = null)
        {
            var w = GetWindow<OptimizationHubWindow>();
            w.OpenToTab(tab, material);
        }

        // =================================================================
        //  Stub tab adapters (placeholder until full migration)
        // =================================================================

        /// <summary>
        /// テクスチャオプティマイザー スタブタブ
        /// </summary>
        private sealed class TextureOptimizerStubTab : INataneToolTab
        {
            public string TabLabel   => L("テクスチャ", "Texture");
            public string TabTooltip => L("テクスチャの最適化と圧縮設定", "Texture optimization and compression settings");
            public string HelpToolKey => "TextureOptimizer";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("テクスチャオプティマイザー — このタブは統合中です",
                      "Texture Optimizer — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧テクスチャオプティマイザーを開く", "Open Legacy Texture Optimizer"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("TextureOptimizer");
                }
            }
        }

        /// <summary>
        /// アウトラインオプティマイザー スタブタブ
        /// </summary>
        private sealed class OutlineOptimizerStubTab : INataneToolTab
        {
            public string TabLabel   => L("アウトライン", "Outline");
            public string TabTooltip => L("アウトラインパラメータの最適化", "Outline parameter optimization");
            public string HelpToolKey => "OutlineOptimizer";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("アウトラインオプティマイザー — このタブは統合中です",
                      "Outline Optimizer — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧アウトラインオプティマイザーを開く", "Open Legacy Outline Optimizer"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("OutlineOptimizer");
                }
            }
        }

        /// <summary>
        /// 屈折クオリティバランサー スタブタブ
        /// </summary>
        private sealed class RefractionBalancerStubTab : INataneToolTab
        {
            public string TabLabel   => L("屈折", "Refraction");
            public string TabTooltip => L("屈折品質とパフォーマンスのバランス調整", "Balance refraction quality and performance");
            public string HelpToolKey => "RefractionQualityBalancer";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("屈折クオリティバランサー — このタブは統合中です",
                      "Refraction Quality Balancer — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧屈折クオリティバランサーを開く", "Open Legacy Refraction Quality Balancer"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("RefractionQualityBalancer");
                }
            }
        }
    }
}
