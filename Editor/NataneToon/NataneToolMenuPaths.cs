using System;
using System.Collections.Generic;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Centralized menu path registry for Natane editor tools.
    /// Keeps Dashboard/Help launcher paths in sync with actual MenuItem definitions.
    /// </summary>
    public static class NataneToolMenuPaths
    {
        public const string Dashboard = "Tools/Natane/Dashboard _d";
        public const string HelpWindow = "Tools/Natane/ヘルプ Help _h";

        public const string MaterialValidator = "Tools/Natane/マテリアル Material/マテリアル検証 Material Validator _m";
        public const string MaterialEditor = "Tools/Natane/マテリアル Material/マテリアルエディタ Material Editor";
        public const string MaterialPreview = "Tools/Natane/マテリアル Material/マテリアルプレビュー Material Preview";
        public const string MaterialComparison = "Tools/Natane/マテリアル Material/マテリアル比較 Material Comparison Tool";
        public const string MakeupLayerManager = "Tools/Natane/マテリアル Material/メイクアップレイヤー管理 Makeup Layer Manager";

        public const string MaterialPresetBrowser = "Tools/Natane/プリセット Presets/Material Preset Browser _p";
        public const string ColorPaletteManager = "Tools/Natane/プリセット Presets/カラーパレット管理 Color Palette Manager";
        public const string GenerateDefaultPresets = "Tools/Natane/プリセット Presets/デフォルトプリセット生成 Generate Default Presets";
        public const string RegenerateAllPresets = "Tools/Natane/プリセット Presets/全プリセット再生成 Regenerate All Presets";

        public const string ShadowAdjustmentWizard = "Tools/Natane/エフェクト Effects/シャドウ調整ウィザード Shadow Adjustment Wizard";
        public const string MatCapLayerComposer = "Tools/Natane/エフェクト Effects/MatCapレイヤーコンポーザー MatCap Layer Composer";
        public const string DissolvePatternGenerator = "Tools/Natane/エフェクト Effects/ディゾルブパターン生成 Dissolve Pattern Generator";
        public const string RimLightDirectionVisualizer = "Tools/Natane/エフェクト Effects/リムライト方向ビジュアライザー Rim Light Direction Visualizer";
        public const string ScreenFXSetup = "Tools/Natane/エフェクト Effects/スクリーンエフェクト設定 Screen FX Setup";

        public const string PerformanceBudgetTool = "Tools/Natane/最適化 Optimization/パフォーマンスバジェット Performance Budget Tool";
        public const string TextureOptimizer = "Tools/Natane/最適化 Optimization/テクスチャ最適化 Texture Optimizer";
        public const string OutlineOptimizer = "Tools/Natane/最適化 Optimization/アウトライン最適化 Outline Optimizer";
        public const string RefractionQualityBalancer = "Tools/Natane/最適化 Optimization/屈折品質バランサー Refraction Quality Balancer";

        public const string LilToonMigration = "Tools/Natane/移行 Migration/lilToon移行ツール lilToon Migration Tool";
        public const string BatchMaterialConverter = "Tools/Natane/移行 Migration/一括マテリアル変換 Batch Material Converter";
        public const string PrefabVariantConverter = "Tools/Natane/移行 Migration/プレハブバリアント変換 Prefab Variant Converter";

        public const string ShaderVariantCollector = "Tools/Natane/シェーダー Shader/シェーダーバリアント収集 Shader Variant Collector";
        public const string VRCLightVolumesHelper = "Tools/Natane/VRChat/VRCライトボリュームヘルパー VRC Light Volumes Helper";

        // Not an actual Unity menu path. Used by launchers for direct tab open.
        public const string HelpToolTab = "__NATANE_HELP_TOOL_TAB__";

        private static readonly Dictionary<string, string> ToolKeyToMenuPath = new Dictionary<string, string>
        {
            { "MaterialValidator", MaterialValidator },
            { "BatchMaterialProcessor", MaterialEditor }, // Legacy key mapped to unified editor
            { "ShadowAdjustmentWizard", ShadowAdjustmentWizard },
            { "MaterialPreview", MaterialPreview },
            { "TextureOptimizer", TextureOptimizer },
            { "OutlineOptimizer", OutlineOptimizer },
            { "DissolvePatternGenerator", DissolvePatternGenerator },
            { "MatCapLayerComposer", MatCapLayerComposer },
            { "ScreenFXSetup", ScreenFXSetup },
            { "PerformanceBudget", PerformanceBudgetTool },
            { "VRCLightVolumes", VRCLightVolumesHelper },
            { "PrefabVariantConverter", PrefabVariantConverter }
        };

        private static readonly Dictionary<string, string> LegacyPathAliases = new Dictionary<string, string>
        {
            { "Tools/Natane/Dashboard", Dashboard },
            { "Tools/Natane/Help", HelpWindow },
            { "Tools/Natane/ヘルプ Help", HelpWindow },
            { "Tools/Natane/ツールヘルプ Tool Help", HelpToolTab },
            { "Tools/Natane/ヘルプ Interactive Help", HelpWindow },
            { "Tools/Natane/Material Validator", MaterialValidator },
            { "Tools/Natane/Batch Material Processor", MaterialEditor },
            { "Tools/Natane/Shadow Adjustment Wizard", ShadowAdjustmentWizard },
            { "Tools/Natane/Material Preview", MaterialPreview },
            { "Tools/Natane/Texture Optimizer", TextureOptimizer },
            { "Tools/Natane/Outline Optimizer", OutlineOptimizer },
            { "Tools/Natane/Dissolve Pattern Generator", DissolvePatternGenerator },
            { "Tools/Natane/MatCap Layer Composer", MatCapLayerComposer },
            { "Tools/Natane/Performance Budget Tool", PerformanceBudgetTool },
            { "Tools/Natane/VRC Light Volumes Helper", VRCLightVolumesHelper },
            { "Tools/Natane/Prefab Variant Converter", PrefabVariantConverter }
        };

        public static bool TryOpenByToolKey(string toolKey)
        {
            if (string.IsNullOrEmpty(toolKey))
            {
                return false;
            }

            if (!ToolKeyToMenuPath.TryGetValue(toolKey, out var menuPath))
            {
                return false;
            }

            return TryExecute(menuPath);
        }

        public static bool TryExecute(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
            {
                return false;
            }

            if (menuPath == HelpToolTab)
            {
                UnifiedHelpSystem.ShowTab(1);
                return true;
            }

            if (ExecuteMenuPath(menuPath))
            {
                return true;
            }

            if (LegacyPathAliases.TryGetValue(menuPath, out var aliasedPath))
            {
                if (aliasedPath == HelpToolTab)
                {
                    UnifiedHelpSystem.ShowTab(1);
                    return true;
                }

                return ExecuteMenuPath(aliasedPath);
            }

            return false;
        }

        private static bool ExecuteMenuPath(string menuPath)
        {
            if (EditorApplication.ExecuteMenuItem(menuPath))
            {
                return true;
            }

            // ExecuteMenuItem expects menu labels, not shortcut suffixes like " _h".
            // Retry with a sanitized path to keep callers robust against MenuItem hotkey changes.
            var normalizedPath = RemoveTrailingShortcut(menuPath);
            return !string.Equals(normalizedPath, menuPath, StringComparison.Ordinal) &&
                   EditorApplication.ExecuteMenuItem(normalizedPath);
        }

        /// <summary>
        /// Returns all registered menu paths for external validation.
        /// バリデータが外部から参照可能な全登録メニューパスのリストを返す。
        /// </summary>
        public static List<string> GetAllMenuPaths()
        {
            var paths = new List<string>
            {
                Dashboard, HelpWindow,
                MaterialValidator, MaterialEditor, MaterialPreview, MaterialComparison, MakeupLayerManager,
                MaterialPresetBrowser, ColorPaletteManager, GenerateDefaultPresets, RegenerateAllPresets,
                ShadowAdjustmentWizard, MatCapLayerComposer, DissolvePatternGenerator,
                RimLightDirectionVisualizer, ScreenFXSetup,
                PerformanceBudgetTool, TextureOptimizer, OutlineOptimizer, RefractionQualityBalancer,
                LilToonMigration, BatchMaterialConverter, PrefabVariantConverter,
                ShaderVariantCollector, VRCLightVolumesHelper,
            };
            return paths;
        }

        private static string RemoveTrailingShortcut(string menuPath)
        {
            const int shortcutTokenLength = 3; // " _x"
            int shortcutIndex = menuPath.LastIndexOf(" _", StringComparison.Ordinal);
            if (shortcutIndex < 0 || shortcutIndex != menuPath.Length - shortcutTokenLength)
            {
                return menuPath;
            }

            return menuPath.Substring(0, shortcutIndex);
        }
    }
}
