using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// Natane Toon Shader Dashboard - Central hub for all tools
    /// Natane Toon Shader ダッシュボード - 全ツールへの統合アクセス
    /// Provides categorized access to 25+ tools and features
    /// </summary>
    public class NataneDashboard : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchQuery = "";
        private ToolCategory selectedCategory = ToolCategory.All;

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
            public string name;
            public string description;
            public string menuPath;
            public ToolCategory category;
            public string icon;

            public ToolInfo(string name, string description, string menuPath, ToolCategory category, string icon = "🔧")
            {
                this.name = name;
                this.description = description;
                this.menuPath = menuPath;
                this.category = category;
                this.icon = icon;
            }
        }

        private List<ToolInfo> allTools = new List<ToolInfo>();

        [MenuItem(NataneToolMenuPaths.Dashboard, false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<NataneDashboard>("Natane Dashboard");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeToolsList();
        }

        private void InitializeToolsList()
        {
            allTools.Clear();

            // Material Tools
            allTools.Add(new ToolInfo(
                "マテリアル検証 Material Validator",
                "VRChat最適化チェック、パフォーマンス評価、自動修正提案",
                NataneToolMenuPaths.MaterialValidator,
                ToolCategory.Material,
                "✅"
            ));

            allTools.Add(new ToolInfo(
                "マテリアルエディタ Material Editor",
                "Batch ModeとScene Modeを含む統合マテリアル編集ツール - 複数マテリアルの一括処理とシーン内リアルタイム編集",
                NataneToolMenuPaths.MaterialEditor,
                ToolCategory.Material,
                "✏️"
            ));

            allTools.Add(new ToolInfo(
                "マテリアルプレビュー Material Preview",
                "リアルタイムマテリアルプレビューとライティングテスト",
                NataneToolMenuPaths.MaterialPreview,
                ToolCategory.Material,
                "👁"
            ));

            allTools.Add(new ToolInfo(
                "マテリアル比較 Material Comparison Tool",
                "2つのマテリアルのパラメータを比較",
                NataneToolMenuPaths.MaterialComparison,
                ToolCategory.Material,
                "⚖"
            ));

            // Presets & Assets
            allTools.Add(new ToolInfo(
                "Material Preset Browser",
                "マテリアルプリセットの視覚的な閲覧と適用",
                NataneToolMenuPaths.MaterialPresetBrowser,
                ToolCategory.Presets,
                "🎨"
            ));

            allTools.Add(new ToolInfo(
                "カラーパレット管理 Color Palette Manager",
                "カラーパレットの作成・管理",
                NataneToolMenuPaths.ColorPaletteManager,
                ToolCategory.Presets,
                "🌈"
            ));

            allTools.Add(new ToolInfo(
                "デフォルトプリセットを生成",
                "基本的なマテリアルプリセットを自動生成",
                NataneToolMenuPaths.GenerateDefaultPresets,
                ToolCategory.Presets,
                "⚡"
            ));

            allTools.Add(new ToolInfo(
                "全プリセットを再生成",
                "全てのプリセットを再生成（開発用）",
                NataneToolMenuPaths.RegenerateAllPresets,
                ToolCategory.Presets,
                "🔄"
            ));

            allTools.Add(new ToolInfo(
                "VTuberプリセット生成 VTuber Preset Generator",
                "VTuber向けマテリアルプリセットの自動生成（肌/髪/衣装/瞳/ライブ）",
                NataneToolMenuPaths.VTuberPresetGenerator,
                ToolCategory.Presets,
                "🎤"
            ));

            // Advanced Tools (Effects + Makeup)
            allTools.Add(new ToolInfo(
                "シャドウ調整ウィザード Shadow Adjustment Wizard",
                "トゥーンシャドウの視覚的な調整",
                NataneToolMenuPaths.ShadowAdjustmentWizard,
                ToolCategory.Advanced,
                "🌓"
            ));

            allTools.Add(new ToolInfo(
                "メイクアップレイヤー管理 Makeup Layer Manager",
                "キャラクターメイクアップの多層管理",
                NataneToolMenuPaths.MakeupLayerManager,
                ToolCategory.Advanced,
                "💄"
            ));

            allTools.Add(new ToolInfo(
                "MatCapレイヤーコンポーザー MatCap Layer Composer",
                "複数MatCapテクスチャの合成",
                NataneToolMenuPaths.MatCapLayerComposer,
                ToolCategory.Advanced,
                "🎭"
            ));

            allTools.Add(new ToolInfo(
                "ディゾルブパターン生成 Dissolve Pattern Generator",
                "ディゾルブエフェクト用パターンテクスチャの生成",
                NataneToolMenuPaths.DissolvePatternGenerator,
                ToolCategory.Advanced,
                "✨"
            ));

            allTools.Add(new ToolInfo(
                "スクリーンエフェクト設定 Screen FX Setup",
                "VRC向け画面効果オーバーレイをカメラへ自動セットアップ",
                NataneToolMenuPaths.ScreenFXSetup,
                ToolCategory.Advanced,
                "🖥"
            ));

            allTools.Add(new ToolInfo(
                "リムライト方向ビジュアライザー Rim Light Direction Visualizer",
                "リムライトの方向を視覚的に確認",
                NataneToolMenuPaths.RimLightDirectionVisualizer,
                ToolCategory.Advanced,
                "💡"
            ));

            allTools.Add(new ToolInfo(
                "屈折品質バランサー Refraction Quality Balancer",
                "屈折エフェクトの品質とパフォーマンスのバランス調整",
                NataneToolMenuPaths.RefractionQualityBalancer,
                ToolCategory.Advanced,
                "🔮"
            ));

            allTools.Add(new ToolInfo(
                "マスクテクスチャスタジオ Mask Texture Studio",
                "ノイズ/グラデーション/メッシュ情報ベースのマスクテクスチャ生成、レイヤー合成、ブラシペイント、チャネルパッキング",
                NataneToolMenuPaths.UVTextureGenerator,
                ToolCategory.Advanced,
                "🎭"
            ));

            allTools.Add(new ToolInfo(
                "パーティクルエフェクトエディタ Particle Effect Editor",
                "パーティクルエフェクトのビジュアル編集、テンプレートプリセット",
                NataneToolMenuPaths.ParticleEffectEditor,
                ToolCategory.Advanced,
                "✨"
            ));

            // Migration Tools
            allTools.Add(new ToolInfo(
                "lilToon移行ツール lilToon Migration Tool",
                "lilToonシェーダーからの自動移行",
                NataneToolMenuPaths.LilToonMigration,
                ToolCategory.Migration,
                "🔀"
            ));

            allTools.Add(new ToolInfo(
                "一括マテリアル変換 Batch Material Converter",
                "汎用マテリアル一括変換ツール",
                NataneToolMenuPaths.BatchMaterialConverter,
                ToolCategory.Migration,
                "📤"
            ));

            allTools.Add(new ToolInfo(
                "プレハブバリアント変換 Prefab Variant Converter",
                "プレハブ内マテリアルの一括変換",
                NataneToolMenuPaths.PrefabVariantConverter,
                ToolCategory.Migration,
                "📦"
            ));

            // Performance Tools
            allTools.Add(new ToolInfo(
                "パフォーマンスバジェット Performance Budget Tool",
                "シーン全体のパフォーマンス分析",
                NataneToolMenuPaths.PerformanceBudgetTool,
                ToolCategory.Performance,
                "⚡"
            ));

            allTools.Add(new ToolInfo(
                "テクスチャ最適化 Texture Optimizer",
                "テクスチャサイズと圧縮の自動最適化",
                NataneToolMenuPaths.TextureOptimizer,
                ToolCategory.Performance,
                "🖼"
            ));

            allTools.Add(new ToolInfo(
                "アウトライン最適化 Outline Optimizer",
                "アウトライン設定の最適化",
                NataneToolMenuPaths.OutlineOptimizer,
                ToolCategory.Performance,
                "🎯"
            ));

            allTools.Add(new ToolInfo(
                "シェーダーバリアント収集 Shader Variant Collector",
                "使用中のシェーダーバリアントを収集してビルドサイズを削減",
                NataneToolMenuPaths.ShaderVariantCollector,
                ToolCategory.Performance,
                "📊"
            ));

            allTools.Add(new ToolInfo(
                "シェーダープリウォーミング Shader Prewarming",
                "ビルド前シェーダーウォーミングでVRChat初回フリーズ防止",
                NataneToolMenuPaths.ShaderPrewarming,
                ToolCategory.Performance,
                "🔥"
            ));

            allTools.Add(new ToolInfo(
                "VRCライトボリュームヘルパー VRC Light Volumes Helper",
                "VRChat Light Volumesのセットアップ支援",
                NataneToolMenuPaths.VRCLightVolumesHelper,
                ToolCategory.Performance,
                "💡"
            ));

            // Help & Documentation
            allTools.Add(new ToolInfo(
                "ツールヘルプ Tool Help",
                "全ツールの使い方と説明",
                NataneToolMenuPaths.HelpToolTab,
                ToolCategory.Help,
                "❓"
            ));

            allTools.Add(new ToolInfo(
                "ヘルプ Interactive Help",
                "インタラクティブなヘルプシステム",
                NataneToolMenuPaths.HelpWindow,
                ToolCategory.Help,
                "📚"
            ));
        }

        private void OnGUI()
        {
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

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 0.7f, 1.0f) }
            };

            EditorGUILayout.LabelField("🎨 Natane Toon Shader Dashboard", titleStyle, GUILayout.Height(30));

            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };

            EditorGUILayout.LabelField("統合ツールハブ - 全機能へのワンクリックアクセス", subtitleStyle);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
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
                    indicatorLabel = "異常 Error";
                    break;
                case NataneToolHealthValidator.HealthStatus.Warning:
                    indicatorColor = NataneToonColorPalette.Warning;
                    indicatorLabel = "警告 Warning";
                    break;
                default:
                    indicatorColor = NataneToonColorPalette.Success;
                    indicatorLabel = "正常 Healthy";
                    break;
            }

            var oldColor = GUI.color;
            GUI.color = indicatorColor;
            EditorGUILayout.LabelField(indicatorLabel, EditorStyles.miniLabel, GUILayout.Width(90));
            GUI.color = oldColor;

            if (GUILayout.Button("診断 Diagnostics", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                NataneToolHealthValidator.ShowWindow();
            }

            EditorGUILayout.LabelField($"{GetFilteredTools().Count} tools", EditorStyles.miniLabel, GUILayout.Width(80));

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
                string label = category == ToolCategory.All ? $"{categoryName} ({count})" : $"{categoryName} ({count})";

                bool isSelected = selectedCategory == category;
                GUIStyle buttonStyle = isSelected
                    ? new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold }
                    : GUI.skin.button;

                if (GUILayout.Button(label, buttonStyle))
                {
                    selectedCategory = category;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍 検索", GUILayout.Width(50));
            searchQuery = EditorGUILayout.TextField(searchQuery);

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
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
                EditorGUILayout.HelpBox("該当するツールが見つかりませんでした。検索条件やカテゴリを変更してください。", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            int columns = 2;
            int rows = Mathf.CeilToInt((float)filteredTools.Count / columns);

            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index >= filteredTools.Count) break;

                    DrawToolCard(filteredTools[index]);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolCard(ToolInfo tool)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(330), GUILayout.Height(100));

            // Tool name with icon
            EditorGUILayout.BeginHorizontal();

            GUIStyle iconStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField(tool.icon, iconStyle, GUILayout.Width(30));

            GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                wordWrap = true
            };
            EditorGUILayout.LabelField(tool.name, nameStyle);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // Description
            GUIStyle descStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = Color.gray }
            };
            EditorGUILayout.LabelField(tool.description, descStyle, GUILayout.Height(30));

            EditorGUILayout.Space(3);

            // Launch button
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };

            if (GUILayout.Button("起動 Launch", buttonStyle, GUILayout.Width(120), GUILayout.Height(25)))
            {
                LaunchTool(tool);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void LaunchTool(ToolInfo tool)
        {
            NataneToolHealthValidator.ValidateAndLaunch(tool.menuPath, tool.name);
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
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    string query = searchQuery.ToLower();
                    bool matchesName = tool.name.ToLower().Contains(query);
                    bool matchesDescription = tool.description.ToLower().Contains(query);

                    if (!matchesName && !matchesDescription)
                        continue;
                }

                filtered.Add(tool);
            }

            return filtered;
        }

        private string GetCategoryDisplayName(ToolCategory category)
        {
            switch (category)
            {
                case ToolCategory.All: return "すべて All";
                case ToolCategory.Material: return "マテリアル";
                case ToolCategory.Presets: return "プリセット";
                case ToolCategory.Advanced: return "高度な機能";
                case ToolCategory.Migration: return "移行";
                case ToolCategory.Performance: return "パフォーマンス";
                case ToolCategory.Help: return "ヘルプ";
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

