using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;

    /// <summary>
    /// エフェクトスタジオ統合ウィンドウの列挙型
    /// </summary>
    public enum EffectStudioTab
    {
        Shadow,
        MatCap,
        RimLight,
        Dissolve,
        Layers
    }

    /// <summary>
    /// Effect Studio Window
    /// エフェクトスタジオ
    /// Consolidates 5 effect-editing tools into a single tabbed window:
    ///   Shadow Adjustment Wizard / MatCap Layer Composer /
    ///   Rim Light Direction Visualizer / Dissolve Pattern Generator /
    ///   Makeup Layer Manager
    /// </summary>
    public class EffectStudioWindow : NataneTabbedToolWindow<EffectStudioTab>
    {
        // ===== Window metadata =====
        protected override string WindowTitleJP => "エフェクトスタジオ";
        protected override string WindowTitleEN => "Effect Studio";
        protected override Vector2 DefaultMinSize => new Vector2(650, 550);

        // ===== Tab factory =====
        protected override INataneToolTab CreateTab(EffectStudioTab tab)
        {
            switch (tab)
            {
                case EffectStudioTab.Shadow:   return new ShadowStubTab();
                case EffectStudioTab.MatCap:   return new MatCapStubTab();
                case EffectStudioTab.RimLight: return new RimLightStubTab();
                case EffectStudioTab.Dissolve: return new DissolveStubTab();
                case EffectStudioTab.Layers:   return new LayersStubTab();
                default:                       return new ShadowStubTab();
            }
        }

        // ===== Menu items =====
        [MenuItem("Tools/Natane/エフェクト Effects/エフェクトスタジオ Effect Studio", false, 20)]
        public static void ShowWindow()
        {
            var w = GetWindow<EffectStudioWindow>();
            w.OpenToTab(EffectStudioTab.Shadow);
        }

        /// <summary>
        /// 指定タブを開いた状態でウィンドウを表示する。
        /// オプションでマテリアルコンテキストを渡せる。
        /// </summary>
        public static void ShowTab(EffectStudioTab tab, Material material = null)
        {
            var w = GetWindow<EffectStudioWindow>();
            w.OpenToTab(tab, material);
        }

        // =================================================================
        //  Stub tab adapters (placeholder until full migration)
        // =================================================================

        /// <summary>
        /// シャドウ調整ウィザード スタブタブ
        /// </summary>
        private sealed class ShadowStubTab : INataneToolTab
        {
            public string TabLabel   => L("シャドウ調整", "Shadow");
            public string TabTooltip => L("シャドウパラメータをステップバイステップで調整", "Step-by-step shadow parameter adjustment");
            public string HelpToolKey => "ShadowAdjustmentWizard";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("シャドウ調整ウィザード — このタブは統合中です",
                      "Shadow Adjustment Wizard — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧シャドウ調整ウィザードを開く", "Open Legacy Shadow Adjustment Wizard"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("ShadowAdjustmentWizard");
                }
            }
        }

        /// <summary>
        /// MatCapレイヤーコンポーザー スタブタブ
        /// </summary>
        private sealed class MatCapStubTab : INataneToolTab
        {
            public string TabLabel   => L("MatCap合成", "MatCap");
            public string TabTooltip => L("MatCapレイヤー1-3を同時に編集", "Edit MatCap layers 1-3 simultaneously");
            public string HelpToolKey => "MatCapLayerComposer";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("MatCapレイヤーコンポーザー — このタブは統合中です",
                      "MatCap Layer Composer — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧MatCapレイヤーコンポーザーを開く", "Open Legacy MatCap Layer Composer"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("MatCapLayerComposer");
                }
            }
        }

        /// <summary>
        /// リムライト方向ビジュアライザー スタブタブ
        /// </summary>
        private sealed class RimLightStubTab : INataneToolTab
        {
            public string TabLabel   => L("リムライト", "RimLight");
            public string TabTooltip => L("リムライトの方向を視覚的に調整", "Visually adjust rim light direction");
            public string HelpToolKey => "RimLightDirectionVisualizer";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("リムライト方向ビジュアライザー — このタブは統合中です",
                      "Rim Light Direction Visualizer — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧リムライト方向ビジュアライザーを開く", "Open Legacy Rim Light Direction Visualizer"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("RimLightDirectionVisualizer");
                }
            }
        }

        /// <summary>
        /// ディゾルブパターンジェネレーター スタブタブ
        /// </summary>
        private sealed class DissolveStubTab : INataneToolTab
        {
            public string TabLabel   => L("ディゾルブ", "Dissolve");
            public string TabTooltip => L("プロシージャルにディゾルブテクスチャを生成", "Generate dissolve textures procedurally");
            public string HelpToolKey => "DissolvePatternGenerator";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("ディゾルブパターンジェネレーター — このタブは統合中です",
                      "Dissolve Pattern Generator — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧ディゾルブパターンジェネレーターを開く", "Open Legacy Dissolve Pattern Generator"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("DissolvePatternGenerator");
                }
            }
        }

        /// <summary>
        /// メイクアップレイヤーマネージャー スタブタブ
        /// </summary>
        private sealed class LayersStubTab : INataneToolTab
        {
            public string TabLabel   => L("レイヤー管理", "Layers");
            public string TabTooltip => L("メイクアップテクスチャレイヤーを管理", "Manage makeup texture layers");
            public string HelpToolKey => "MakeupLayerManager";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("メイクアップレイヤーマネージャー — このタブは統合中です",
                      "Makeup Layer Manager — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧メイクアップレイヤーマネージャーを開く", "Open Legacy Makeup Layer Manager"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("MakeupLayerManager");
                }
            }
        }
    }
}
