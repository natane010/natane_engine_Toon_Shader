using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;

    /// <summary>
    /// シェーダービルド管理ウィンドウのタブ列挙型
    /// </summary>
    public enum ShaderBuildTab
    {
        VariantCollector,
        Prewarming,
        VariantStripper
    }

    /// <summary>
    /// Shader Build Manager Window
    /// シェーダービルド管理
    /// Consolidates 3 shader build tools into a single tabbed window:
    ///   Shader Variant Collector / Shader Prewarming / Shader Variant Stripper
    /// </summary>
    public class ShaderBuildManagerWindow : NataneTabbedToolWindow<ShaderBuildTab>
    {
        // ===== Window metadata =====
        protected override string WindowTitleJP => "シェーダービルド管理";
        protected override string WindowTitleEN => "Shader Build Manager";
        protected override Vector2 DefaultMinSize => new Vector2(650, 550);

        // ===== Tab factory =====
        protected override INataneToolTab CreateTab(ShaderBuildTab tab)
        {
            switch (tab)
            {
                case ShaderBuildTab.VariantCollector: return new VariantCollectorStubTab();
                case ShaderBuildTab.Prewarming:       return new PrewarmingStubTab();
                case ShaderBuildTab.VariantStripper:  return new VariantStripperStubTab();
                default:                              return new VariantCollectorStubTab();
            }
        }

        // ===== Menu items =====
        [MenuItem("Tools/Natane/シェーダー Shader/ビルド管理 Build Manager", false, 10)]
        public static void ShowWindow()
        {
            var w = GetWindow<ShaderBuildManagerWindow>();
            w.OpenToTab(ShaderBuildTab.VariantCollector);
        }

        /// <summary>
        /// 指定タブを開いた状態でウィンドウを表示する。
        /// </summary>
        public static void ShowTab(ShaderBuildTab tab, Material material = null)
        {
            var w = GetWindow<ShaderBuildManagerWindow>();
            w.OpenToTab(tab, material);
        }

        // =================================================================
        //  Stub tab adapters (placeholder until full migration)
        // =================================================================

        /// <summary>
        /// シェーダーバリアントコレクター スタブタブ
        /// </summary>
        private sealed class VariantCollectorStubTab : INataneToolTab
        {
            public string TabLabel   => L("バリアント収集", "Collect");
            public string TabTooltip => L("プロジェクト内マテリアルからシェーダーバリアントを収集", "Collect shader variants from project materials");
            public string HelpToolKey => "ShaderVariantCollector";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("シェーダーバリアントコレクター — このタブは統合中です",
                      "Shader Variant Collector — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧シェーダーバリアントコレクターを開く", "Open Legacy Shader Variant Collector"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("ShaderVariantCollector");
                }
            }
        }

        /// <summary>
        /// シェーダープリウォーミング スタブタブ
        /// </summary>
        private sealed class PrewarmingStubTab : INataneToolTab
        {
            public string TabLabel   => L("プリウォーム", "Prewarm");
            public string TabTooltip => L("シェーダーバリアントのプリウォーミング設定", "Shader variant prewarming configuration");
            public string HelpToolKey => "ShaderPrewarming";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("シェーダープリウォーミング — このタブは統合中です",
                      "Shader Prewarming — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧シェーダープリウォーミングを開く", "Open Legacy Shader Prewarming"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("ShaderPrewarming");
                }
            }
        }

        /// <summary>
        /// シェーダーバリアントストリッパー スタブタブ
        /// </summary>
        private sealed class VariantStripperStubTab : INataneToolTab
        {
            public string TabLabel   => L("ストリッピング", "Strip");
            public string TabTooltip => L("未使用シェーダーバリアントのストリッピング設定", "Configure stripping of unused shader variants");
            public string HelpToolKey => "ShaderVariantStripper";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("シェーダーバリアントストリッパー — このタブは統合中です",
                      "Shader Variant Stripper — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧シェーダーバリアントストリッパーを開く", "Open Legacy Shader Variant Stripper"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("ShaderVariantStripper");
                }
            }
        }
    }
}
