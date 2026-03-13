using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;

    /// <summary>
    /// プリセット管理ウィンドウの列挙型
    /// </summary>
    public enum PresetManagerTab
    {
        PresetBrowser,
        ColorPalette,
        PresetGenerator
    }

    /// <summary>
    /// Preset Manager Window
    /// プリセット管理
    /// Consolidates 3 preset-related tools into a single tabbed window:
    ///   Material Preset Browser / Color Palette Manager / Default Preset Generator
    /// </summary>
    public class PresetManagerWindow : NataneTabbedToolWindow<PresetManagerTab>
    {
        // ===== Window metadata =====
        protected override string WindowTitleJP => "プリセット管理";
        protected override string WindowTitleEN => "Preset Manager";
        protected override Vector2 DefaultMinSize => new Vector2(650, 550);

        // ===== Tab factory =====
        protected override INataneToolTab CreateTab(PresetManagerTab tab)
        {
            switch (tab)
            {
                case PresetManagerTab.PresetBrowser:   return new PresetBrowserStubTab();
                case PresetManagerTab.ColorPalette:    return new ColorPaletteStubTab();
                case PresetManagerTab.PresetGenerator: return new PresetGeneratorStubTab();
                default:                               return new PresetBrowserStubTab();
            }
        }

        // ===== Menu items =====
        [MenuItem("Tools/Natane/プリセット Presets/プリセット管理 Preset Manager", false, 10)]
        public static void ShowWindow()
        {
            var w = GetWindow<PresetManagerWindow>();
            w.OpenToTab(PresetManagerTab.PresetBrowser);
        }

        /// <summary>
        /// 指定タブを開いた状態でウィンドウを表示する。
        /// オプションでマテリアルコンテキストを渡せる。
        /// </summary>
        public static void ShowTab(PresetManagerTab tab, Material material = null)
        {
            var w = GetWindow<PresetManagerWindow>();
            w.OpenToTab(tab, material);
        }

        // =================================================================
        //  Stub tab adapters (placeholder until full migration)
        // =================================================================

        /// <summary>
        /// マテリアルプリセットブラウザ スタブタブ
        /// </summary>
        private sealed class PresetBrowserStubTab : INataneToolTab
        {
            public string TabLabel   => L("ブラウザ", "Browse");
            public string TabTooltip => L("マテリアルプリセットを一覧・適用", "Browse and apply material presets");
            public string HelpToolKey => "MaterialPresetBrowser";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("マテリアルプリセットブラウザ — このタブは統合中です",
                      "Material Preset Browser — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧マテリアルプリセットブラウザを開く", "Open Legacy Material Preset Browser"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("MaterialPresetBrowser");
                }
            }
        }

        /// <summary>
        /// カラーパレットマネージャー スタブタブ
        /// </summary>
        private sealed class ColorPaletteStubTab : INataneToolTab
        {
            public string TabLabel   => L("カラーパレット", "Colors");
            public string TabTooltip => L("カラーパレットを管理・適用", "Manage and apply color palettes");
            public string HelpToolKey => "ColorPaletteManager";
            public bool RequiresMaterial => true;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("カラーパレットマネージャー — このタブは統合中です",
                      "Color Palette Manager — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧カラーパレットマネージャーを開く", "Open Legacy Color Palette Manager"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("ColorPaletteManager");
                }
            }
        }

        /// <summary>
        /// デフォルトプリセットジェネレーター スタブタブ
        /// </summary>
        private sealed class PresetGeneratorStubTab : INataneToolTab
        {
            public string TabLabel   => L("プリセット生成", "Generate");
            public string TabTooltip => L("デフォルトプリセットを自動生成", "Auto-generate default presets");
            public string HelpToolKey => "DefaultPresetGenerator";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("デフォルトプリセットジェネレーター — このタブは統合中です",
                      "Default Preset Generator — This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧デフォルトプリセット生成を開く", "Open Legacy Generate Default Presets"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("GenerateDefaultPresets");
                }
            }
        }
    }
}
