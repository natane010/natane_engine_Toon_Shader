using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Cross-assembly tool health validator and diagnostics window.
    /// クロスアセンブリ ツール健全性バリデータと診断ウィンドウ。
    ///
    /// Validates that all registered tools are reachable across assembly boundaries
    /// before launching, and provides a diagnostics UI for troubleshooting.
    /// </summary>
    public class NataneToolHealthValidator : EditorWindow
    {
        // =====================================================================
        // Data Types
        // =====================================================================

        public enum HealthStatus
        {
            Healthy,
            Warning,
            Error
        }

        public enum DiagnosticSeverity
        {
            OK,
            Warning,
            Error
        }

        public class DiagnosticResult
        {
            public string toolKey;
            public string displayName;
            public string menuPath;
            public DiagnosticSeverity severity;
            public string message;
            public string suggestion;
        }

        /// <summary>
        /// Single Source of Truth for tool registration.
        /// ツール登録の唯一の真実の情報源。
        /// </summary>
        private class ToolRegistryEntry
        {
            public string toolKey;
            public string displayName;
            public string menuPath;
            public string typeName;
            public string assemblyName;

            public ToolRegistryEntry(string toolKey, string displayName, string menuPath,
                string typeName, string assemblyName)
            {
                this.toolKey = toolKey;
                this.displayName = displayName;
                this.menuPath = menuPath;
                this.typeName = typeName;
                this.assemblyName = assemblyName;
            }
        }

        // =====================================================================
        // Tool Registry (Single Source of Truth)
        // =====================================================================

        private const string ASSEMBLY_EDITOR = "NataneToon.Editor";
        private const string ASSEMBLY_TOOLS = "NataneToon.Editor.Tools";
        private const string ASSEMBLY_MIGRATION = "NataneToon.Editor.Migration";

        private static readonly List<ToolRegistryEntry> ToolRegistry = new List<ToolRegistryEntry>
        {
            // --- Material Tools (Tools assembly) ---
            new ToolRegistryEntry("MaterialValidator", "マテリアル検証 Material Validator",
                NataneToolMenuPaths.MaterialValidator, "NataneToon.Editor.MaterialValidator", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("MaterialEditor", "マテリアルエディタ Material Editor",
                NataneToolMenuPaths.MaterialEditor, "NataneToon.Editor.UnifiedMaterialEditor", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("MaterialPreview", "マテリアルプレビュー Material Preview",
                NataneToolMenuPaths.MaterialPreview, "NataneToon.Editor.MaterialPreviewWindow", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("MaterialComparison", "マテリアル比較 Material Comparison",
                NataneToolMenuPaths.MaterialComparison, "NataneToon.Editor.MaterialComparisonTool", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("MakeupLayerManager", "メイクアップレイヤー管理 Makeup Layer Manager",
                NataneToolMenuPaths.MakeupLayerManager, "NataneToon.Editor.MakeupLayerManager", ASSEMBLY_TOOLS),

            // --- Preset Tools (Editor assembly) ---
            new ToolRegistryEntry("MaterialPresetBrowser", "Material Preset Browser",
                NataneToolMenuPaths.MaterialPresetBrowser, "NataneToon.Editor.MaterialPresetBrowser", ASSEMBLY_EDITOR),
            new ToolRegistryEntry("ColorPaletteManager", "カラーパレット管理 Color Palette Manager",
                NataneToolMenuPaths.ColorPaletteManager, "NataneToon.Editor.NataneToonColorPaletteManager", ASSEMBLY_EDITOR),

            // --- Effects Tools (Tools assembly) ---
            new ToolRegistryEntry("ShadowAdjustmentWizard", "シャドウ調整ウィザード Shadow Wizard",
                NataneToolMenuPaths.ShadowAdjustmentWizard, "NataneToon.Editor.ShadowAdjustmentWizard", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("MatCapLayerComposer", "MatCapレイヤーコンポーザー MatCap Composer",
                NataneToolMenuPaths.MatCapLayerComposer, "NataneToon.Editor.MatCapLayerComposer", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("DissolvePatternGenerator", "ディゾルブパターン生成 Dissolve Generator",
                NataneToolMenuPaths.DissolvePatternGenerator, "NataneToon.Editor.DissolvePatternGenerator", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("RimLightDirectionVisualizer", "リムライト方向ビジュアライザー Rim Visualizer",
                NataneToolMenuPaths.RimLightDirectionVisualizer, "NataneToon.Editor.RimLightDirectionVisualizer", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("ScreenFXSetup", "スクリーンエフェクト設定 Screen FX Setup",
                NataneToolMenuPaths.ScreenFXSetup, "NataneToon.Editor.ScreenFXSetupTool", ASSEMBLY_TOOLS),

            // --- Optimization Tools (Tools assembly) ---
            new ToolRegistryEntry("PerformanceBudget", "パフォーマンスバジェット Performance Budget",
                NataneToolMenuPaths.PerformanceBudgetTool, "NataneToon.Editor.PerformanceBudgetTool", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("TextureOptimizer", "テクスチャ最適化 Texture Optimizer",
                NataneToolMenuPaths.TextureOptimizer, "NataneToon.Editor.TextureOptimizer", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("OutlineOptimizer", "アウトライン最適化 Outline Optimizer",
                NataneToolMenuPaths.OutlineOptimizer, "NataneToon.Editor.OutlineOptimizer", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("RefractionQualityBalancer", "屈折品質バランサー Refraction Balancer",
                NataneToolMenuPaths.RefractionQualityBalancer, "NataneToon.Editor.RefractionQualityBalancer", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("ShaderVariantCollector", "シェーダーバリアント収集 Shader Variant Collector",
                NataneToolMenuPaths.ShaderVariantCollector, "ShaderVariantCollector", ASSEMBLY_TOOLS),
            new ToolRegistryEntry("VRCLightVolumesHelper", "VRCライトボリュームヘルパー VRC Light Volumes Helper",
                NataneToolMenuPaths.VRCLightVolumesHelper, "NataneToon.Editor.VRCLightVolumesHelper", ASSEMBLY_TOOLS),

            // --- Migration Tools (Migration assembly) ---
            new ToolRegistryEntry("LilToonMigration", "lilToon移行ツール lilToon Migration",
                NataneToolMenuPaths.LilToonMigration, "NataneToon.Editor.LilToonMigrationTool", ASSEMBLY_MIGRATION),
            new ToolRegistryEntry("BatchMaterialConverter", "一括マテリアル変換 Batch Converter",
                NataneToolMenuPaths.BatchMaterialConverter, "NataneToon.Editor.BatchMaterialConverter", ASSEMBLY_MIGRATION),
            new ToolRegistryEntry("PrefabVariantConverter", "プレハブバリアント変換 Prefab Converter",
                NataneToolMenuPaths.PrefabVariantConverter, "NataneToon.Editor.PrefabVariantConverter", ASSEMBLY_TOOLS),
        };

        // =====================================================================
        // Static Validation API
        // =====================================================================

        /// <summary>
        /// Validate a single tool by menu path. Returns null if healthy, DiagnosticResult otherwise.
        /// 単一ツールのメニューパスで事前チェック。正常ならnull、問題があればDiagnosticResultを返す。
        /// </summary>
        public static DiagnosticResult ValidateTool(string menuPath)
        {
            var entry = ToolRegistry.FirstOrDefault(e => e.menuPath == menuPath);
            if (entry == null)
            {
                return new DiagnosticResult
                {
                    menuPath = menuPath,
                    severity = DiagnosticSeverity.Warning,
                    message = "ツールレジストリに登録されていないメニューパスです。\nMenu path is not registered in the tool registry.",
                    suggestion = "NataneToolHealthValidator.ToolRegistry にエントリを追加してください。"
                };
            }

            return ValidateEntry(entry);
        }

        /// <summary>
        /// Validate and launch a tool. Shows a dialog on failure with cause and remedy.
        /// チェック後に起動。失敗時は原因と対処法をダイアログ表示。
        /// </summary>
        public static bool ValidateAndLaunch(string menuPath, string displayName)
        {
            var result = ValidateTool(menuPath);

            if (result != null && result.severity == DiagnosticSeverity.Error)
            {
                bool openDiagnostics = EditorUtility.DisplayDialog(
                    "ツール起動エラー Tool Launch Error",
                    $"ツールを起動できません:\n{displayName}\n\n" +
                    $"原因 Cause:\n{result.message}\n\n" +
                    $"対処法 Remedy:\n{result.suggestion}",
                    "診断ツールを開く Open Diagnostics",
                    "閉じる Close");

                if (openDiagnostics)
                {
                    ShowWindow();
                }

                return false;
            }

            // Attempt to launch via NataneToolMenuPaths
            if (!NataneToolMenuPaths.TryExecute(menuPath))
            {
                EditorUtility.DisplayDialog(
                    "ツール起動エラー Tool Launch Error",
                    $"メニュー項目を実行できませんでした:\n{displayName}\n\n" +
                    $"Menu path: {menuPath}\n\n" +
                    "メニューパスが正しいか、または対象アセンブリがロードされているか確認してください。\n" +
                    "Verify that the menu path is correct and the target assembly is loaded.",
                    "OK");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Lightweight health check for Dashboard toolbar indicator.
        /// Dashboard ツールバーインジケータ用の軽量ヘルスチェック。
        /// </summary>
        public static HealthStatus GetOverallHealth()
        {
            bool hasError = false;
            bool hasWarning = false;

            // Check critical assemblies are loaded
            var loadedAssemblyNames = new HashSet<string>(
                AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetName().Name));

            if (!loadedAssemblyNames.Contains(ASSEMBLY_TOOLS))
            {
                return HealthStatus.Error;
            }

            if (!loadedAssemblyNames.Contains(ASSEMBLY_MIGRATION))
            {
                return HealthStatus.Error;
            }

            // Spot-check a few critical tools
            var spotCheckKeys = new[] { "MaterialValidator", "LilToonMigration", "ShaderVariantCollector" };
            foreach (var key in spotCheckKeys)
            {
                var entry = ToolRegistry.FirstOrDefault(e => e.toolKey == key);
                if (entry == null) continue;

                var result = ValidateEntry(entry);
                if (result != null)
                {
                    if (result.severity == DiagnosticSeverity.Error) hasError = true;
                    else if (result.severity == DiagnosticSeverity.Warning) hasWarning = true;
                }
            }

            if (hasError) return HealthStatus.Error;
            if (hasWarning) return HealthStatus.Warning;
            return HealthStatus.Healthy;
        }

        /// <summary>
        /// Run full diagnostics on all registered tools.
        /// 全登録ツールの包括的チェックを実行。
        /// </summary>
        public static List<DiagnosticResult> RunFullDiagnostics()
        {
            var results = new List<DiagnosticResult>();

            // 1. Assembly load checks
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var loadedAssemblyNames = new HashSet<string>(loadedAssemblies.Select(a => a.GetName().Name));

            var requiredAssemblies = new[] { ASSEMBLY_EDITOR, ASSEMBLY_TOOLS, ASSEMBLY_MIGRATION };
            foreach (var asmName in requiredAssemblies)
            {
                if (!loadedAssemblyNames.Contains(asmName))
                {
                    results.Add(new DiagnosticResult
                    {
                        toolKey = "__assembly__",
                        displayName = $"アセンブリ Assembly: {asmName}",
                        menuPath = "",
                        severity = DiagnosticSeverity.Error,
                        message = $"アセンブリ '{asmName}' がロードされていません。\nAssembly '{asmName}' is not loaded.",
                        suggestion = $"対応する .asmdef ファイルが存在し、参照が正しく設定されているか確認してください。\n" +
                                     $"Verify the .asmdef file exists and references are correctly configured."
                    });
                }
            }

            // 2. Per-tool checks
            foreach (var entry in ToolRegistry)
            {
                var result = ValidateEntry(entry);
                if (result != null)
                {
                    results.Add(result);
                }
                else
                {
                    // Add OK result for display purposes
                    results.Add(new DiagnosticResult
                    {
                        toolKey = entry.toolKey,
                        displayName = entry.displayName,
                        menuPath = entry.menuPath,
                        severity = DiagnosticSeverity.OK,
                        message = "正常 Healthy",
                        suggestion = ""
                    });
                }
            }

            return results;
        }

        // =====================================================================
        // Internal Validation Logic
        // =====================================================================

        private static DiagnosticResult ValidateEntry(ToolRegistryEntry entry)
        {
            // Skip non-menu-path entries (e.g. HelpToolTab sentinel)
            if (string.IsNullOrEmpty(entry.menuPath) || entry.menuPath.StartsWith("__"))
                return null;

            // 1. Check assembly is loaded
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var targetAssembly = loadedAssemblies.FirstOrDefault(a => a.GetName().Name == entry.assemblyName);

            if (targetAssembly == null)
            {
                return new DiagnosticResult
                {
                    toolKey = entry.toolKey,
                    displayName = entry.displayName,
                    menuPath = entry.menuPath,
                    severity = DiagnosticSeverity.Error,
                    message = $"アセンブリ '{entry.assemblyName}' が見つかりません。\n" +
                              $"Assembly '{entry.assemblyName}' not found.",
                    suggestion = $".asmdef ファイルが正しく配置されているか確認してください。\n" +
                                 $"Verify the .asmdef file is correctly placed."
                };
            }

            // 2. Check type exists in assembly
            Type toolType = null;
            try
            {
                toolType = targetAssembly.GetType(entry.typeName);

                // If not found by full name, search all types (for global namespace types)
                if (toolType == null)
                {
                    try
                    {
                        toolType = targetAssembly.GetTypes()
                            .FirstOrDefault(t => t.FullName == entry.typeName || t.Name == entry.typeName);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Some types may fail to load; check loaded types
                        toolType = ex.Types
                            .Where(t => t != null)
                            .FirstOrDefault(t => t.FullName == entry.typeName || t.Name == entry.typeName);
                    }
                }
            }
            catch (Exception)
            {
                // Assembly.GetType can throw in rare cases
            }

            if (toolType == null)
            {
                return new DiagnosticResult
                {
                    toolKey = entry.toolKey,
                    displayName = entry.displayName,
                    menuPath = entry.menuPath,
                    severity = DiagnosticSeverity.Error,
                    message = $"型 '{entry.typeName}' がアセンブリ '{entry.assemblyName}' 内に見つかりません。\n" +
                              $"Type '{entry.typeName}' not found in assembly '{entry.assemblyName}'.",
                    suggestion = "クラス名または名前空間が変更されていないか確認してください。\n" +
                                 "Check if the class name or namespace has been changed."
                };
            }

            // 3. Check MenuItem attribute exists (for non-static-class tools)
            if (!toolType.IsAbstract || !toolType.IsSealed) // not a static class
            {
                bool hasMenuItem = toolType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(m => m.GetCustomAttributes(typeof(MenuItem), false).Length > 0);

                if (!hasMenuItem)
                {
                    return new DiagnosticResult
                    {
                        toolKey = entry.toolKey,
                        displayName = entry.displayName,
                        menuPath = entry.menuPath,
                        severity = DiagnosticSeverity.Warning,
                        message = $"型 '{entry.typeName}' に [MenuItem] 属性が見つかりません。\n" +
                                  $"No [MenuItem] attribute found on type '{entry.typeName}'.",
                        suggestion = "メニューパスからの起動ができない可能性があります。\n" +
                                     "Launching from menu path may not work."
                    };
                }
            }

            return null; // Healthy
        }

        // =====================================================================
        // EditorWindow UI
        // =====================================================================

        private List<DiagnosticResult> diagnosticResults = new List<DiagnosticResult>();
        private Vector2 scrollPosition;
        private bool showOnlyProblems = false;

        [MenuItem("Tools/Natane/診断 Diagnostics", false, 2)]
        public static void ShowWindow()
        {
            var window = GetWindow<NataneToolHealthValidator>("診断 Diagnostics");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RunDiagnostics();
        }

        private void RunDiagnostics()
        {
            diagnosticResults = RunFullDiagnostics();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(5);
            DrawSummary();
            EditorGUILayout.Space(5);
            DrawToolbar();
            EditorGUILayout.Space(5);
            DrawResultsList();
        }

        private void DrawHeader()
        {
            NataneToonShaderGUIUtility.DrawToolHeader(
                "ツール診断",
                "Tool Health Diagnostics",
                "Diagnostics");
        }

        private void DrawSummary()
        {
            int errorCount = diagnosticResults.Count(r => r.severity == DiagnosticSeverity.Error);
            int warningCount = diagnosticResults.Count(r => r.severity == DiagnosticSeverity.Warning);
            int okCount = diagnosticResults.Count(r => r.severity == DiagnosticSeverity.OK);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            Color oldColor = GUI.color;

            GUI.color = errorCount > 0 ? NataneToonColorPalette.Error : Color.white;
            EditorGUILayout.LabelField($"エラー Errors: {errorCount}", EditorStyles.boldLabel, GUILayout.Width(150));

            GUI.color = warningCount > 0 ? NataneToonColorPalette.Warning : Color.white;
            EditorGUILayout.LabelField($"警告 Warnings: {warningCount}", EditorStyles.boldLabel, GUILayout.Width(160));

            GUI.color = NataneToonColorPalette.Success;
            EditorGUILayout.LabelField($"正常 OK: {okCount}", EditorStyles.boldLabel, GUILayout.Width(120));

            GUI.color = oldColor;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("再チェック Re-check", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                RunDiagnostics();
            }

            showOnlyProblems = GUILayout.Toggle(showOnlyProblems, "問題のみ表示 Problems Only",
                EditorStyles.toolbarButton, GUILayout.Width(160));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawResultsList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var result in diagnosticResults)
            {
                if (showOnlyProblems && result.severity == DiagnosticSeverity.OK)
                    continue;

                DrawResultItem(result);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawResultItem(DiagnosticResult result)
        {
            Color bgColor;
            string icon;

            switch (result.severity)
            {
                case DiagnosticSeverity.Error:
                    bgColor = new Color(1f, 0.5f, 0.5f, 0.3f);
                    icon = "X";
                    break;
                case DiagnosticSeverity.Warning:
                    bgColor = new Color(1f, 1f, 0.5f, 0.3f);
                    icon = "!";
                    break;
                default:
                    bgColor = new Color(0.5f, 1f, 0.5f, 0.2f);
                    icon = "OK";
                    break;
            }

            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();

            // Severity indicator
            var oldColor = GUI.color;
            GUI.color = result.severity == DiagnosticSeverity.Error ? NataneToonColorPalette.Error :
                        result.severity == DiagnosticSeverity.Warning ? NataneToonColorPalette.Warning :
                        NataneToonColorPalette.Success;
            EditorGUILayout.LabelField($"[{icon}]", EditorStyles.boldLabel, GUILayout.Width(30));
            GUI.color = oldColor;

            // Display name
            EditorGUILayout.LabelField(result.displayName, EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();

            // Message
            if (result.severity != DiagnosticSeverity.OK)
            {
                EditorGUILayout.LabelField(result.message, EditorStyles.wordWrappedLabel);

                if (!string.IsNullOrEmpty(result.suggestion))
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("対処法 Remedy:", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField(result.suggestion, EditorStyles.wordWrappedLabel);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }
}
