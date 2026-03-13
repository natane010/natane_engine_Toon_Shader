using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;

    /// <summary>
    /// VRChat統合ウィンドウの列挙型
    /// </summary>
    public enum VRChatIntegrationTab
    {
        LightVolumes,
        DependencySetup,
        AutoDetect
    }

    /// <summary>
    /// VRChat Integration Window
    /// VRChat統合
    /// Consolidates 3 VRChat-related tools into a single tabbed window:
    ///   VRC Light Volumes Helper / Dependency Setup / Auto Detectors
    /// </summary>
    public class VRChatIntegrationWindow : NataneTabbedToolWindow<VRChatIntegrationTab>
    {
        // ===== Window metadata =====
        protected override string WindowTitleJP => "VRChat統合";
        protected override string WindowTitleEN => "VRChat Integration";
        protected override Vector2 DefaultMinSize => new Vector2(600, 500);

        // ===== Tab factory =====
        protected override INataneToolTab CreateTab(VRChatIntegrationTab tab)
        {
            switch (tab)
            {
                case VRChatIntegrationTab.LightVolumes:   return new LightVolumesStubTab();
                case VRChatIntegrationTab.DependencySetup: return new DependencySetupStubTab();
                case VRChatIntegrationTab.AutoDetect:      return new AutoDetectStubTab();
                default:                                   return new LightVolumesStubTab();
            }
        }

        // ===== Menu items =====
        [MenuItem("Tools/Natane/VRChat/VRChat統合 VRChat Integration", false, 10)]
        public static void ShowWindow()
        {
            var w = GetWindow<VRChatIntegrationWindow>();
            w.OpenToTab(VRChatIntegrationTab.LightVolumes);
        }

        /// <summary>
        /// 指定タブを開いた状態でウィンドウを表示する。
        /// オプションでマテリアルコンテキストを渡せる。
        /// </summary>
        public static void ShowTab(VRChatIntegrationTab tab, Material material = null)
        {
            var w = GetWindow<VRChatIntegrationWindow>();
            w.OpenToTab(tab, material);
        }

        // =================================================================
        //  Stub tab adapters (placeholder until full migration)
        // =================================================================

        /// <summary>
        /// VRC Light Volumes ヘルパー スタブタブ
        /// </summary>
        private sealed class LightVolumesStubTab : INataneToolTab
        {
            public string TabLabel   => L("Light Volumes", "Light Volumes");
            public string TabTooltip => L("VRC Light Volumes のセットアップと管理", "Setup and manage VRC Light Volumes");
            public string HelpToolKey => "VRCLightVolumes";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("VRC Light Volumes ヘルパー — このタブは統合中です",
                      "VRC Light Volumes Helper — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧VRC Light Volumesヘルパーを開く", "Open Legacy VRC Light Volumes Helper"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("VRCLightVolumes");
                }
            }
        }

        /// <summary>
        /// パッケージ依存関係設定 スタブタブ
        /// </summary>
        private sealed class DependencySetupStubTab : INataneToolTab
        {
            public string TabLabel   => L("パッケージ設定", "Package Setup");
            public string TabTooltip => L("VRChat 関連パッケージの依存関係を設定", "Configure VRChat-related package dependencies");
            public string HelpToolKey => "DependencySetup";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("パッケージ依存関係設定 — このタブは統合中です",
                      "Dependency Setup — This tab is being integrated"),
                    MessageType.Info);
            }
        }

        /// <summary>
        /// 自動検出ツール スタブタブ
        /// </summary>
        private sealed class AutoDetectStubTab : INataneToolTab
        {
            public string TabLabel   => L("自動検出", "Auto Detect");
            public string TabTooltip => L("VRChat 環境を自動検出して最適化を提案", "Auto-detect VRChat environment and suggest optimizations");
            public string HelpToolKey => "AutoDetectors";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("自動検出ツール — このタブは統合中です",
                      "Auto Detect — This tab is being integrated"),
                    MessageType.Info);
            }
        }
    }
}
