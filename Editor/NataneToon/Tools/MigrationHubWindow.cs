using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;

    /// <summary>
    /// マイグレーションハブのタブ列挙型
    /// </summary>
    public enum MigrationHubTab
    {
        LilToonMigration,
        BatchConverter,
        PrefabVariant
    }

    /// <summary>
    /// Migration Hub Window
    /// マイグレーションハブ
    /// Consolidates migration tools into a single tabbed window:
    ///   lilToon Migration / Batch Material Converter / Prefab Variant Converter
    /// </summary>
    public class MigrationHubWindow : NataneTabbedToolWindow<MigrationHubTab>
    {
        // ===== Window metadata =====
        protected override string WindowTitleJP => "マイグレーションハブ";
        protected override string WindowTitleEN => "Migration Hub";
        protected override Vector2 DefaultMinSize => new Vector2(700, 600);

        // ===== Tab factory =====
        protected override INataneToolTab CreateTab(MigrationHubTab tab)
        {
            switch (tab)
            {
                case MigrationHubTab.LilToonMigration: return new LilToonMigrationTab();
                case MigrationHubTab.BatchConverter:   return new BatchConverterTab();
                case MigrationHubTab.PrefabVariant:    return new PrefabVariantTab();
                default:                               return new LilToonMigrationTab();
            }
        }

        // ===== Menu items =====
        [MenuItem("Tools/Natane/移行 Migration/マイグレーションハブ Migration Hub", false, 10)]
        public static void ShowWindow()
        {
            var w = GetWindow<MigrationHubWindow>();
            w.OpenToTab(MigrationHubTab.LilToonMigration);
        }

        /// <summary>
        /// 指定タブを開いた状態でウィンドウを表示する。
        /// オプションでマテリアルコンテキストを渡せる。
        /// </summary>
        public static void ShowTab(MigrationHubTab tab, Material material = null)
        {
            var w = GetWindow<MigrationHubWindow>();
            w.OpenToTab(tab, material);
        }

        // =================================================================
        //  Stub tab adapters (placeholder until full migration)
        // =================================================================

        /// <summary>
        /// lilToon移行 スタブタブ
        /// </summary>
        private sealed class LilToonMigrationTab : INataneToolTab
        {
            public string TabLabel   => L("lilToon移行", "lilToon");
            public string TabTooltip => L("lilToonマテリアルをNatane Toonに移行", "Migrate lilToon materials to Natane Toon");
            public string HelpToolKey => "LilToonMigration";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("lilToon移行 - このタブは統合中です",
                      "lilToon Migration - This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧lilToon移行ツールを開く", "Open Legacy lilToon Migration Tool"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("LilToonMigration");
                }
            }
        }

        /// <summary>
        /// 一括変換 スタブタブ
        /// </summary>
        private sealed class BatchConverterTab : INataneToolTab
        {
            public string TabLabel   => L("一括変換", "Batch");
            public string TabTooltip => L("複数マテリアルを一括でNatane Toonに変換", "Batch convert multiple materials to Natane Toon");
            public string HelpToolKey => "BatchMaterialConverter";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("一括変換 - このタブは統合中です",
                      "Batch Material Converter - This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧一括マテリアル変換ツールを開く", "Open Legacy Batch Material Converter"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("BatchMaterialConverter");
                }
            }
        }

        /// <summary>
        /// Prefab変換 スタブタブ
        /// </summary>
        private sealed class PrefabVariantTab : INataneToolTab
        {
            public string TabLabel   => L("Prefab変換", "Prefab");
            public string TabTooltip => L("Prefabバリアントのマテリアルを変換", "Convert materials in prefab variants");
            public string HelpToolKey => "PrefabVariantConverter";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("Prefab変換 - このタブは統合中です",
                      "Prefab Variant Converter - This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧Prefabバリアント変換ツールを開く", "Open Legacy Prefab Variant Converter"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("PrefabVariantConverter");
                }
            }
        }
    }
}
