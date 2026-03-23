using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    /// <summary>
    /// Natane Toon Shader Dashboard - Central hub for all tools
    /// Natane Toon Shader ダッシュボード - 全ツールへの統合アクセス
    /// Provides categorized access to 25+ tools and features
    /// </summary>
    public class NataneDashboard : EditorWindow
    {
        private static string DashboardWindowTitle => L("Natane ダッシュボード", "Natane Dashboard");

        private Vector2 scrollPosition;
        private string searchQuery = "";
        private ToolCategory selectedCategory = ToolCategory.All;
        private const float MinToolCardWidth = 260f;
        private const float ToolCardSpacing = 8f;
        private const float ToolGridHorizontalPadding = 24f;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle selectedCategoryButtonStyle;
        private GUIStyle toolIconStyle;
        private GUIStyle toolNameStyle;
        private GUIStyle toolSecondaryNameStyle;
        private GUIStyle toolDescriptionStyle;
        private GUIStyle toolLaunchButtonStyle;
        private bool cachedProSkin;

        private enum ToolCategory
        {
            All,
            Material,
            Presets,
            Advanced,
            Migration,
            Performance,
            Help
        }

        private class ToolInfo
        {
            public readonly string nameJP;
            public readonly string nameEN;
            public readonly string descriptionJP;
            public readonly string descriptionEN;
            public readonly string menuPath;
            public readonly ToolCategory category;
            public readonly string icon;

            public string DisplayName => L(nameJP, nameEN);
            public string SecondaryName => L(nameEN, nameJP);
            public string DisplayDescription => L(descriptionJP, descriptionEN);

            public ToolInfo(
                string nameJP,
                string nameEN,
                string descriptionJP,
                string descriptionEN,
                string menuPath,
                ToolCategory category,
                string icon = "🔧")
            {
                this.nameJP = nameJP;
                this.nameEN = nameEN;
                this.descriptionJP = descriptionJP;
                this.descriptionEN = descriptionEN;
                this.menuPath = menuPath;
                this.category = category;
                this.icon = icon;
            }
        }

        private readonly List<ToolInfo> allTools = new List<ToolInfo>();

        [MenuItem(NataneToolMenuPaths.Dashboard, false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<NataneDashboard>(DashboardWindowTitle);
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshWindowTitle();
            EnsureStyles();
            InitializeToolsList();
        }

        private void RefreshWindowTitle()
        {
            titleContent = new GUIContent(DashboardWindowTitle);
        }

        private void InitializeToolsList()
        {
            allTools.Clear();

            // Material Tools
            AddTool("マテリアル検証", "Material Validator", "VRChat最適化チェック、パフォーマンス評価、自動修正提案", "Validate materials for VRChat readiness, performance, and suggested fixes.", NataneToolMenuPaths.MaterialValidator, ToolCategory.Material, "✅");
            AddTool("マテリアルエディタ", "Material Editor", "Batch ModeとScene Modeを含む統合マテリアル編集ツール。複数マテリアルの一括処理とシーン内リアルタイム編集に対応します。", "Edit multiple materials in batch or scene mode with live updates.", NataneToolMenuPaths.MaterialEditor, ToolCategory.Material, "✏️");
            AddTool("マテリアルプレビュー", "Material Preview", "リアルタイムマテリアルプレビューとライティングテスト。", "Preview materials in real time with lighting controls.", NataneToolMenuPaths.MaterialPreview, ToolCategory.Material, "👁");
            AddTool("マテリアル比較", "Material Comparison Tool", "2つのマテリアルのパラメータ差分を比較します。", "Compare parameter differences between two materials.", NataneToolMenuPaths.MaterialComparison, ToolCategory.Material, "⚖");

            // Presets & Assets
            AddTool("マテリアルプリセットブラウザ", "Material Preset Browser", "マテリアルプリセットを視覚的に閲覧して適用します。", "Browse and apply material presets with visual previews.", NataneToolMenuPaths.MaterialPresetBrowser, ToolCategory.Presets, "🎨");
            AddTool("カラーパレット管理", "Color Palette Manager", "カラーパレットの作成と管理を行います。", "Create and manage shared color palettes.", NataneToolMenuPaths.ColorPaletteManager, ToolCategory.Presets, "🌈");
            AddTool("デフォルトプリセット生成", "Generate Default Presets", "基本的なマテリアルプリセットを自動生成します。", "Generate the standard starter material presets.", NataneToolMenuPaths.GenerateDefaultPresets, ToolCategory.Presets, "⚡");
            AddTool("全プリセット再生成", "Regenerate All Presets", "全てのプリセットを再生成します。開発・メンテナンス向けです。", "Rebuild every preset asset for development or maintenance.", NataneToolMenuPaths.RegenerateAllPresets, ToolCategory.Presets, "🔄");
            AddTool("VTuberプリセット生成", "VTuber Preset Generator", "VTuber向けの肌・髪・衣装・瞳・ライブ用プリセットを Material Preset Browser 内で自動生成します。", "Generate VTuber-focused presets for skin, hair, outfits, eyes, and live scenes from the preset browser.", NataneToolMenuPaths.MaterialPresetBrowser, ToolCategory.Presets, "🎤");

            // Advanced Tools (Effects + Makeup)
            AddTool("シャドウ調整ウィザード", "Shadow Adjustment Wizard", "トゥーンシャドウを視覚的に段階調整します。", "Tune toon shadow parameters with guided visual controls.", NataneToolMenuPaths.ShadowAdjustmentWizard, ToolCategory.Advanced, "🌓");
            AddTool("メイクアップレイヤー管理", "Makeup Layer Manager", "キャラクターメイクアップを多層で管理します。", "Manage layered character makeup and masks.", NataneToolMenuPaths.MakeupLayerManager, ToolCategory.Advanced, "💄");
            AddTool("MatCapレイヤーコンポーザー", "MatCap Layer Composer", "複数の MatCap テクスチャを合成します。", "Blend and composite multiple MatCap textures.", NataneToolMenuPaths.MatCapLayerComposer, ToolCategory.Advanced, "🎭");
            AddTool("ディゾルブパターン生成", "Dissolve Pattern Generator", "ディゾルブエフェクト用のパターンテクスチャを生成します。", "Create pattern textures for dissolve effects.", NataneToolMenuPaths.DissolvePatternGenerator, ToolCategory.Advanced, "✨");
            AddTool("スクリーンエフェクト設定", "Screen FX Setup", "VRC向け画面効果オーバーレイをカメラへ自動セットアップします。", "Automatically set up a VRC-friendly full-screen overlay on a camera.", NataneToolMenuPaths.ScreenFXSetup, ToolCategory.Advanced, "🖥");
            AddTool("リムライト方向ビジュアライザー", "Rim Light Direction Visualizer", "リムライト方向を視覚的に確認します。", "Visualize rim light direction before committing settings.", NataneToolMenuPaths.RimLightDirectionVisualizer, ToolCategory.Advanced, "💡");
            AddTool("屈折品質バランサー", "Refraction Quality Balancer", "屈折エフェクトの品質とパフォーマンスをバランス調整します。", "Balance refraction quality against performance.", NataneToolMenuPaths.RefractionQualityBalancer, ToolCategory.Advanced, "🔮");

            AddTool("パーティクルエフェクトエディタ", "Particle Effect Editor", "パーティクルエフェクトの見た目とプリセットを編集します。", "Edit particle effect visuals and presets.", NataneToolMenuPaths.ParticleEffectEditor, ToolCategory.Advanced, "✨");

            // Migration Tools
            AddTool("lilToon移行ツール", "lilToon Migration Tool", "lilToon シェーダーからマテリアルを自動移行します。", "Migrate materials from lilToon automatically.", NataneToolMenuPaths.LilToonMigration, ToolCategory.Migration, "🔀");
            AddTool("一括マテリアル変換", "Batch Material Converter", "汎用マテリアルを一括変換します。", "Convert many generic materials in one pass.", NataneToolMenuPaths.BatchMaterialConverter, ToolCategory.Migration, "📤");
            AddTool("プレハブバリアント変換", "Prefab Variant Converter", "プレハブ内マテリアルを一括変換します。", "Convert materials inside prefab variants in bulk.", NataneToolMenuPaths.PrefabVariantConverter, ToolCategory.Migration, "📦");

            // Performance Tools
            AddTool("パフォーマンスバジェット", "Performance Budget Tool", "シーン全体のパフォーマンス予算を分析します。", "Analyze scene-wide material performance budgets.", NataneToolMenuPaths.PerformanceBudgetTool, ToolCategory.Performance, "⚡");
            AddTool("テクスチャ最適化", "Texture Optimizer", "テクスチャサイズと圧縮設定を自動最適化します。", "Optimize texture size and compression settings automatically.", NataneToolMenuPaths.TextureOptimizer, ToolCategory.Performance, "🖼");
            AddTool("アウトライン最適化", "Outline Optimizer", "アウトライン設定を最適化します。", "Optimize outline settings for better performance.", NataneToolMenuPaths.OutlineOptimizer, ToolCategory.Performance, "🎯");
            AddTool("シェーダーバリアント収集", "Shader Variant Collector", "使用中のシェーダーバリアントを収集してビルドサイズを削減します。", "Collect used shader variants to cut build size.", NataneToolMenuPaths.ShaderVariantCollector, ToolCategory.Performance, "📊");
            AddTool("シェーダープリウォーミング", "Shader Prewarming", "ビルド前のシェーダーウォーミングで VRChat 初回フリーズを防ぎます。", "Warm shaders before build to reduce first-load stalls in VRChat.", NataneToolMenuPaths.ShaderPrewarming, ToolCategory.Performance, "🔥");
            AddTool("VRCライトボリュームヘルパー", "VRC Light Volumes Helper", "VRChat Light Volumes のセットアップを支援します。", "Assist with VRC Light Volumes setup.", NataneToolMenuPaths.VRCLightVolumesHelper, ToolCategory.Performance, "💡");

            // Consolidated Windows (統合ウィンドウ)
            AddTool("マテリアル分析", "Material Analysis", "マテリアル検証・パフォーマンス・比較・参照チェックを統合したウィンドウです。", "Unified window for material validation, performance, comparison, and reference checks.", NataneToolMenuPaths.MaterialAnalysis, ToolCategory.Material, "📋");
            AddTool("エフェクトスタジオ", "Effect Studio", "シャドウ・MatCap・リムライト・ディゾルブ・レイヤー管理を統合したウィンドウです。", "Unified window for shadow, MatCap, rim light, dissolve, and layer management.", NataneToolMenuPaths.EffectStudio, ToolCategory.Advanced, "🎬");
            AddTool("最適化ハブ", "Optimization Hub", "テクスチャ・アウトライン・屈折の最適化を統合したウィンドウです。", "Unified window for texture, outline, and refraction optimization.", NataneToolMenuPaths.OptimizationHub, ToolCategory.Performance, "⚙");
            AddTool("シェーダービルド管理", "Shader Build Manager", "バリアント収集・プリウォーミング・ストリッピングを統合したウィンドウです。", "Unified window for variant collection, prewarming, and stripping.", NataneToolMenuPaths.ShaderBuildManager, ToolCategory.Performance, "🔨");
            AddTool("マイグレーションハブ", "Migration Hub", "lilToon移行・一括変換・Prefab変換を統合したウィンドウです。", "Unified window for lilToon migration, batch conversion, and prefab conversion.", NataneToolMenuPaths.MigrationHub, ToolCategory.Migration, "🔄");
            AddTool("プリセット管理", "Preset Manager", "プリセットブラウザ・カラーパレット・プリセット生成を統合したウィンドウです。", "Unified window for preset browsing, color palettes, and preset generation.", NataneToolMenuPaths.PresetManager, ToolCategory.Presets, "📦");
            AddTool("VRChat統合", "VRChat Integration", "Light Volumes・パッケージ設定・自動検出を統合したウィンドウです。", "Unified window for Light Volumes, package setup, and auto-detection.", NataneToolMenuPaths.VRChatIntegration, ToolCategory.Performance, "🌐");

            // Help & Documentation
            AddTool("ツールヘルプ", "Tool Help", "全ツールの使い方と説明を確認します。", "Browse usage guides and explanations for every tool.", NataneToolMenuPaths.HelpToolTab, ToolCategory.Help, "❓");
            AddTool("統合ヘルプ", "Interactive Help", "インタラクティブな統合ヘルプシステムを開きます。", "Open the unified interactive help system.", NataneToolMenuPaths.HelpWindow, ToolCategory.Help, "📚");
        }

        private void AddTool(
            string nameJP,
            string nameEN,
            string descriptionJP,
            string descriptionEN,
            string menuPath,
            ToolCategory category,
            string icon)
        {
            allTools.Add(new ToolInfo(nameJP, nameEN, descriptionJP, descriptionEN, menuPath, category, icon));
        }

        private void EnsureStyles()
        {
            if (titleStyle != null && cachedProSkin == EditorGUIUtility.isProSkin)
            {
                return;
            }

            cachedProSkin = EditorGUIUtility.isProSkin;

            var secondaryTextColor = EditorGUIUtility.isProSkin
                ? new Color(0.72f, 0.72f, 0.72f)
                : new Color(0.35f, 0.35f, 0.35f);
            var descriptionTextColor = EditorGUIUtility.isProSkin
                ? new Color(0.78f, 0.78f, 0.78f)
                : new Color(0.4f, 0.4f, 0.4f);

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.3f, 0.7f, 1.0f);

            subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            subtitleStyle.normal.textColor = Color.gray;

            selectedCategoryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };

            toolIconStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.UpperLeft
            };

            toolNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                wordWrap = true
            };

            toolSecondaryNameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
            toolSecondaryNameStyle.normal.textColor = secondaryTextColor;

            toolDescriptionStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                fontSize = 10,
                wordWrap = true
            };
            toolDescriptionStyle.normal.textColor = descriptionTextColor;

            toolLaunchButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void OnGUI()
        {
            EnsureStyles();
            RefreshWindowTitle();
            DrawHeader();
            DrawToolbar();
            EditorGUILayout.Space(5);
            DrawCategoryTabs();
            EditorGUILayout.Space(5);
            DrawSearchBar();
            EditorGUILayout.Space(10);
            DrawToolGrid();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("🎨 Natane Toon Shader ダッシュボード", "🎨 Natane Toon Shader Dashboard"), titleStyle, GUILayout.Height(30));
            EditorGUILayout.LabelField(L("統合ツールハブ - 全機能へのワンクリックアクセス", "Unified tool hub - one-click access to every feature"), subtitleStyle);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(L("更新", "Refresh"), EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                InitializeToolsList();
            }

            GUILayout.FlexibleSpace();

            // Health indicator
            var health = NataneToolHealthValidator.GetOverallHealth();
            Color indicatorColor;
            string indicatorLabel;
            switch (health)
            {
                case NataneToolHealthValidator.HealthStatus.Error:
                    indicatorColor = NataneToonColorPalette.Error;
                    indicatorLabel = L("異常", "Error");
                    break;
                case NataneToolHealthValidator.HealthStatus.Warning:
                    indicatorColor = NataneToonColorPalette.Warning;
                    indicatorLabel = L("警告", "Warning");
                    break;
                default:
                    indicatorColor = NataneToonColorPalette.Success;
                    indicatorLabel = L("正常", "Healthy");
                    break;
            }

            var oldColor = GUI.color;
            GUI.color = indicatorColor;
            EditorGUILayout.LabelField(indicatorLabel, EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.color = oldColor;

            if (GUILayout.Button(L("診断", "Diagnostics"), EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                NataneToolHealthValidator.ShowWindow();
            }

            int filteredToolCount = GetFilteredTools().Count;
            EditorGUILayout.LabelField(L($"{filteredToolCount} ツール", $"{filteredToolCount} tools"), EditorStyles.miniLabel, GUILayout.Width(90));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategoryTabs()
        {
            EditorGUILayout.BeginHorizontal();

            var categories = System.Enum.GetValues(typeof(ToolCategory));
            foreach (ToolCategory category in categories)
            {
                string categoryName = GetCategoryDisplayName(category);
                int count = GetToolCountForCategory(category);
                string label = $"{categoryName} ({count})";

                bool isSelected = selectedCategory == category;
                GUIStyle buttonStyle = isSelected ? selectedCategoryButtonStyle : GUI.skin.button;

                if (GUILayout.Button(label, buttonStyle, GUILayout.MinHeight(28f)))
                {
                    selectedCategory = category;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L("🔍 検索", "🔍 Search"), GUILayout.Width(72f));
            searchQuery = EditorGUILayout.TextField(searchQuery);

            if (GUILayout.Button(L("クリア", "Clear"), GUILayout.Width(60f)))
            {
                searchQuery = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolGrid()
        {
            var filteredTools = GetFilteredTools();

            if (filteredTools.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    L("該当するツールが見つかりません。検索条件かカテゴリを変更してください。", "No tools matched your current search. Change the search text or category."),
                    MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            float availableWidth = Mathf.Max(200f, position.width - ToolGridHorizontalPadding);
            int columns = GetResponsiveColumnCount(availableWidth);
            float cardWidth = Mathf.Max(180f, (availableWidth - ((columns - 1) * ToolCardSpacing)) / columns);
            int rows = Mathf.CeilToInt((float)filteredTools.Count / columns);

            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index >= filteredTools.Count) break;

                    DrawToolCard(filteredTools[index], cardWidth);

                    if (col < columns - 1 && index < filteredTools.Count - 1)
                    {
                        GUILayout.Space(ToolCardSpacing);
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(ToolCardSpacing);
            }

            EditorGUILayout.EndScrollView();
        }

        private int GetResponsiveColumnCount(float availableWidth)
        {
            return Mathf.Max(1, Mathf.FloorToInt((availableWidth + ToolCardSpacing) / (MinToolCardWidth + ToolCardSpacing)));
        }

        private void DrawToolCard(ToolInfo tool, float cardWidth)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(cardWidth), GUILayout.MinHeight(120f));

            // Tool name with icon
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(tool.icon, toolIconStyle, GUILayout.Width(32f));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(tool.DisplayName, toolNameStyle);
            string secondaryName = tool.SecondaryName;
            if (!string.IsNullOrEmpty(secondaryName) && secondaryName != tool.DisplayName)
            {
                EditorGUILayout.LabelField(secondaryName, toolSecondaryNameStyle);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            // Description
            EditorGUILayout.LabelField(tool.DisplayDescription, toolDescriptionStyle);
            EditorGUILayout.Space(8f);

            // Launch button
            if (GUILayout.Button(L("起動", "Launch"), toolLaunchButtonStyle, GUILayout.Height(28f)))
            {
                LaunchTool(tool);
            }

            EditorGUILayout.EndVertical();
        }

        private void LaunchTool(ToolInfo tool)
        {
            NataneToolHealthValidator.ValidateAndLaunch(tool.menuPath, tool.DisplayName);
        }

        private List<ToolInfo> GetFilteredTools()
        {
            var filtered = new List<ToolInfo>();

            foreach (var tool in allTools)
            {
                // Category filter
                if (selectedCategory != ToolCategory.All && tool.category != selectedCategory)
                    continue;

                // Search filter
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    string query = searchQuery.Trim();
                    bool matchesName = ContainsSearchText(tool.nameJP, query) || ContainsSearchText(tool.nameEN, query);
                    bool matchesDescription = ContainsSearchText(tool.descriptionJP, query) || ContainsSearchText(tool.descriptionEN, query);

                    if (!matchesName && !matchesDescription)
                        continue;
                }

                filtered.Add(tool);
            }

            return filtered;
        }

        private static bool ContainsSearchText(string source, string query)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GetCategoryDisplayName(ToolCategory category)
        {
            switch (category)
            {
                case ToolCategory.All: return L("すべて", "All");
                case ToolCategory.Material: return L("マテリアル", "Materials");
                case ToolCategory.Presets: return L("プリセット", "Presets");
                case ToolCategory.Advanced: return L("高度な機能", "Advanced");
                case ToolCategory.Migration: return L("移行", "Migration");
                case ToolCategory.Performance: return L("パフォーマンス", "Performance");
                case ToolCategory.Help: return L("ヘルプ", "Help");
                default: return category.ToString();
            }
        }

        private int GetToolCountForCategory(ToolCategory category)
        {
            if (category == ToolCategory.All)
                return allTools.Count;

            int count = 0;
            foreach (var tool in allTools)
            {
                if (tool.category == category)
                    count++;
            }
            return count;
        }
    }
}
