using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// ツールドキュメントデータベース
    /// Documentation database for all Natane Toon tools
    /// </summary>
    public static class NataneToonToolsDocumentation
    {
        public class ToolDocumentation
        {
            public string toolName;
            public string toolNameJP;
            public string description;
            public string descriptionJP;
            public List<string> features;
            public List<string> featuresJP;
            public List<UsageStep> steps;
            public List<string> tips;
            public List<string> tipsJP;
            public string category;
        }

        public class UsageStep
        {
            public string title;
            public string titleJP;
            public string description;
            public string descriptionJP;
            public Texture2D screenshot; // オプション
        }

        private static Dictionary<string, ToolDocumentation> documentationDatabase;

        public static Dictionary<string, ToolDocumentation> GetAllDocumentation()
        {
            if (documentationDatabase == null)
            {
                InitializeDocumentation();
            }
            return documentationDatabase;
        }

        public static ToolDocumentation GetDocumentation(string toolKey)
        {
            if (documentationDatabase == null)
            {
                InitializeDocumentation();
            }
            return documentationDatabase.ContainsKey(toolKey) ? documentationDatabase[toolKey] : null;
        }

        private static void InitializeDocumentation()
        {
            documentationDatabase = new Dictionary<string, ToolDocumentation>();

            // ===== マテリアル検証ツール =====
            documentationDatabase["MaterialValidator"] = new ToolDocumentation
            {
                toolName = "Material Validator",
                toolNameJP = "マテリアル検証",
                description = "Validates materials for VRChat optimization and performance",
                descriptionJP = "マテリアルのVRChat最適化とパフォーマンスを検証します",
                category = "Quality",
                features = new List<string>
                {
                    "VRChat optimization checks",
                    "Texture size validation",
                    "Performance rating",
                    "Auto-fix suggestions",
                    "Batch validation"
                },
                featuresJP = new List<string>
                {
                    "VRChat最適化チェック",
                    "テクスチャサイズの検証",
                    "パフォーマンス評価",
                    "自動修正提案",
                    "一括検証"
                },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Add Materials",
                        titleJP = "マテリアルを追加",
                        description = "Drag and drop materials or click 'Add Selected Materials'",
                        descriptionJP = "マテリアルをドラッグ&ドロップするか「選択したマテリアルを追加」をクリック"
                    },
                    new UsageStep
                    {
                        title = "Configure Checks",
                        titleJP = "チェック項目を設定",
                        description = "Enable/disable validation checks based on your needs",
                        descriptionJP = "必要に応じて検証項目を有効/無効化"
                    },
                    new UsageStep
                    {
                        title = "Run Validation",
                        titleJP = "検証を実行",
                        description = "Click 'Validate Materials' to start checking",
                        descriptionJP = "「マテリアルを検証」をクリックして検証開始"
                    },
                    new UsageStep
                    {
                        title = "Review Results",
                        titleJP = "結果を確認",
                        description = "Check issues by severity (Error/Warning/Info)",
                        descriptionJP = "重要度別（エラー/警告/情報）に問題を確認"
                    },
                    new UsageStep
                    {
                        title = "Apply Fixes",
                        titleJP = "修正を適用",
                        description = "Use 'Auto Fix' buttons to apply suggested fixes",
                        descriptionJP = "「自動修正」ボタンで提案された修正を適用"
                    }
                },
                tips = new List<string>
                {
                    "Run validation before uploading to VRChat",
                    "Check 'VRChat Optimization' for avatar compatibility",
                    "Fix all errors before warnings for best results"
                },
                tipsJP = new List<string>
                {
                    "VRChatアップロード前に検証を実行しましょう",
                    "アバター互換性のため「VRChat最適化」をチェック",
                    "最良の結果のため、警告より先にエラーを修正"
                }
            };

            // ===== 一括マテリアル処理 =====
            documentationDatabase["BatchMaterialProcessor"] = new ToolDocumentation
            {
                toolName = "Batch Material Processor",
                toolNameJP = "一括マテリアル処理",
                description = "Process multiple materials simultaneously with various operations",
                descriptionJP = "複数のマテリアルを様々な操作で一括処理します",
                category = "Utility",
                features = new List<string>
                {
                    "Parameter adjustment",
                    "Color modification",
                    "Texture replacement",
                    "Feature toggling",
                    "Variant conversion"
                },
                featuresJP = new List<string>
                {
                    "パラメータ調整",
                    "色の変更",
                    "テクスチャ置換",
                    "機能の切り替え",
                    "バリアント変換"
                },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Select Materials",
                        titleJP = "マテリアルを選択",
                        description = "Add materials to process using the material list",
                        descriptionJP = "マテリアルリストから処理対象を追加"
                    },
                    new UsageStep
                    {
                        title = "Choose Operation",
                        titleJP = "操作を選択",
                        description = "Select tab: Parameter/Color/Texture/Feature/Variant",
                        descriptionJP = "タブを選択: パラメータ/色/テクスチャ/機能/バリアント"
                    },
                    new UsageStep
                    {
                        title = "Configure Settings",
                        titleJP = "設定を構成",
                        description = "Set values for the selected operation",
                        descriptionJP = "選択した操作の値を設定"
                    },
                    new UsageStep
                    {
                        title = "Preview Changes",
                        titleJP = "変更をプレビュー",
                        description = "Click 'Preview' to see changes without applying",
                        descriptionJP = "「プレビュー」で適用前に変更を確認"
                    },
                    new UsageStep
                    {
                        title = "Apply",
                        titleJP = "適用",
                        description = "Click 'Apply to All' to process all materials",
                        descriptionJP = "「すべてに適用」ですべてのマテリアルを処理"
                    }
                },
                tips = new List<string>
                {
                    "Use 'Set' mode to set exact values",
                    "Use 'Multiply' for relative adjustments",
                    "Always backup before batch operations"
                },
                tipsJP = new List<string>
                {
                    "「Set」モードで正確な値を設定",
                    "「Multiply」で相対的な調整が可能",
                    "一括操作前に必ずバックアップを"
                }
            };

            // ===== シャドウ調整ウィザード =====
            documentationDatabase["ShadowAdjustmentWizard"] = new ToolDocumentation
            {
                toolName = "Shadow Adjustment Wizard",
                toolNameJP = "シャドウ調整ウィザード",
                description = "Step-by-step wizard for configuring shadow parameters",
                descriptionJP = "シャドウパラメータを段階的に設定するウィザード",
                category = "Shading",
                features = new List<string>
                {
                    "Guided setup process",
                    "Shadow presets",
                    "Multi-tone shadow configuration",
                    "Real-time preview",
                    "Shadow map settings"
                },
                featuresJP = new List<string>
                {
                    "ガイド付きセットアップ",
                    "シャドウプリセット",
                    "多階調シャドウ設定",
                    "リアルタイムプレビュー",
                    "シャドウマップ設定"
                },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Select Material",
                        titleJP = "マテリアルを選択",
                        description = "Choose the material to configure",
                        descriptionJP = "設定するマテリアルを選択"
                    },
                    new UsageStep
                    {
                        title = "Choose Preset",
                        titleJP = "プリセットを選択",
                        description = "Select a shadow style: Sharp Anime, Soft Toon, etc.",
                        descriptionJP = "シャドウスタイルを選択: シャープアニメ、ソフトトゥーンなど"
                    },
                    new UsageStep
                    {
                        title = "Adjust Basic Shadow",
                        titleJP = "基本シャドウを調整",
                        description = "Configure shadow color, offset, and sharpness",
                        descriptionJP = "シャドウの色、オフセット、シャープネスを設定"
                    },
                    new UsageStep
                    {
                        title = "Configure Multi-tone",
                        titleJP = "多階調を設定",
                        description = "Add additional shadow levels if needed",
                        descriptionJP = "必要に応じて追加のシャドウレベルを設定"
                    },
                    new UsageStep
                    {
                        title = "Preview",
                        titleJP = "プレビュー",
                        description = "Check result with adjustable light angle",
                        descriptionJP = "調整可能なライト角度で結果を確認"
                    }
                },
                tips = new List<string>
                {
                    "Use 'Sharp Anime' for crisp cel-shading",
                    "Try 'Soft Toon' for character skin",
                    "Adjust preview light angle to test different lighting"
                },
                tipsJP = new List<string>
                {
                    "クリスプなセルシェーディングには「シャープアニメ」",
                    "キャラクターの肌には「ソフトトゥーン」を試してください",
                    "プレビューのライト角度を調整して様々な照明をテスト"
                }
            };

            // ===== マテリアルプレビュー =====
            documentationDatabase["MaterialPreview"] = new ToolDocumentation
            {
                toolName = "Material Preview",
                toolNameJP = "マテリアルプレビュー",
                description = "Enhanced 3D material preview with lighting control",
                descriptionJP = "ライティング制御付きの拡張3Dマテリアルプレビュー",
                category = "Preview",
                features = new List<string>
                {
                    "Real-time 3D preview",
                    "Multiple preview shapes",
                    "Adjustable lighting",
                    "Rotation and zoom",
                    "HDR support"
                },
                featuresJP = new List<string>
                {
                    "リアルタイム3Dプレビュー",
                    "複数のプレビュー形状",
                    "調整可能なライティング",
                    "回転とズーム",
                    "HDRサポート"
                },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Select Material",
                        titleJP = "マテリアルを選択",
                        description = "Drag material to preview field",
                        descriptionJP = "プレビューフィールドにマテリアルをドラッグ"
                    },
                    new UsageStep
                    {
                        title = "Choose Shape",
                        titleJP = "形状を選択",
                        description = "Select preview object: Sphere, Cube, etc.",
                        descriptionJP = "プレビューオブジェクトを選択: 球、立方体など"
                    },
                    new UsageStep
                    {
                        title = "Adjust Lighting",
                        titleJP = "ライティングを調整",
                        description = "Configure light color, intensity, and rotation",
                        descriptionJP = "ライトの色、強度、回転を設定"
                    },
                    new UsageStep
                    {
                        title = "Rotate View",
                        titleJP = "ビューを回転",
                        description = "Drag in preview window to rotate camera",
                        descriptionJP = "プレビューウィンドウでドラッグしてカメラを回転"
                    }
                },
                tips = new List<string>
                {
                    "Use sphere for general material preview",
                    "Try different lighting angles",
                    "Scroll to zoom in/out"
                },
                tipsJP = new List<string>
                {
                    "一般的なマテリアルプレビューには球を使用",
                    "様々なライト角度を試してください",
                    "スクロールでズームイン/アウト"
                }
            };

            // ===== テクスチャ最適化 =====
            documentationDatabase["TextureOptimizer"] = new ToolDocumentation
            {
                toolName = "Texture Optimizer",
                toolNameJP = "テクスチャ最適化",
                description = "Automatically optimize textures for performance",
                descriptionJP = "テクスチャをパフォーマンス用に自動最適化",
                category = "Optimization",
                features = new List<string>
                {
                    "Size optimization",
                    "Compression settings",
                    "Mipmap generation",
                    "Memory calculation",
                    "Batch processing"
                },
                featuresJP = new List<string>
                {
                    "サイズ最適化",
                    "圧縮設定",
                    "ミップマップ生成",
                    "メモリ計算",
                    "一括処理"
                },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Add Textures",
                        titleJP = "テクスチャを追加",
                        description = "Select textures to optimize",
                        descriptionJP = "最適化するテクスチャを選択"
                    },
                    new UsageStep
                    {
                        title = "Configure Settings",
                        titleJP = "設定を構成",
                        description = "Set max size, compression, and mipmap options",
                        descriptionJP = "最大サイズ、圧縮、ミップマップオプションを設定"
                    },
                    new UsageStep
                    {
                        title = "Analyze",
                        titleJP = "分析",
                        description = "Click 'Analyze' to check current texture status",
                        descriptionJP = "「分析」で現在のテクスチャ状態を確認"
                    },
                    new UsageStep
                    {
                        title = "Optimize",
                        titleJP = "最適化",
                        description = "Click 'Optimize All' to apply changes",
                        descriptionJP = "「すべて最適化」で変更を適用"
                    }
                },
                tips = new List<string>
                {
                    "2048x2048 is recommended max for VRChat",
                    "Enable compression for better performance",
                    "Always check visual quality after optimization"
                },
                tipsJP = new List<string>
                {
                    "VRChatには2048x2048が推奨最大サイズ",
                    "パフォーマンス向上のため圧縮を有効化",
                    "最適化後は必ず視覚品質を確認"
                }
            };

            // ===== アウトライン最適化 =====
            documentationDatabase["OutlineOptimizer"] = new ToolDocumentation
            {
                toolName = "Outline Optimizer",
                toolNameJP = "アウトライン最適化",
                description = "Optimize outline rendering for better performance",
                descriptionJP = "アウトラインレンダリングをパフォーマンス向上のため最適化",
                category = "Optimization",
                features = new List<string>
                {
                    "Width optimization",
                    "LOD-based adjustment",
                    "Distance fading",
                    "Performance presets",
                    "Visual quality balance"
                },
                featuresJP = new List<string>
                {
                    "幅の最適化",
                    "LODベースの調整",
                    "距離フェード",
                    "パフォーマンスプリセット",
                    "視覚品質バランス"
                },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Select Target",
                        titleJP = "ターゲットを選択",
                        description = "Choose material or object to optimize",
                        descriptionJP = "最適化するマテリアルまたはオブジェクトを選択"
                    },
                    new UsageStep
                    {
                        title = "Analyze",
                        titleJP = "分析",
                        description = "Check current outline settings",
                        descriptionJP = "現在のアウトライン設定を確認"
                    },
                    new UsageStep
                    {
                        title = "Choose Preset",
                        titleJP = "プリセットを選択",
                        description = "Select optimization level: Balanced, Performance, Quality",
                        descriptionJP = "最適化レベルを選択: バランス、パフォーマンス、品質"
                    },
                    new UsageStep
                    {
                        title = "Apply",
                        titleJP = "適用",
                        description = "Apply optimized settings",
                        descriptionJP = "最適化された設定を適用"
                    }
                },
                tips = new List<string>
                {
                    "Use 'Balanced' for general use",
                    "Enable distance fading for better performance",
                    "Thinner outlines = better performance"
                },
                tipsJP = new List<string>
                {
                    "一般使用には「バランス」を使用",
                    "パフォーマンス向上のため距離フェードを有効化",
                    "細いアウトライン = 高パフォーマンス"
                }
            };

            // ===== ディゾルブパターン生成 =====
            documentationDatabase["DissolvePatternGenerator"] = new ToolDocumentation
            {
                toolName = "Dissolve Pattern Generator",
                toolNameJP = "ディゾルブパターン生成",
                description = "Generate procedural dissolve textures",
                descriptionJP = "プロシージャルディゾルブテクスチャを生成",
                category = "Texture",
                features = new List<string>
                {
                    "Multiple noise types",
                    "Adjustable parameters",
                    "Real-time preview",
                    "Export to texture",
                    "Preset patterns"
                },
                featuresJP = new List<string>
                {
                    "複数のノイズタイプ",
                    "調整可能なパラメータ",
                    "リアルタイムプレビュー",
                    "テクスチャに出力",
                    "プリセットパターン"
                },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Choose Noise Type",
                        titleJP = "ノイズタイプを選択",
                        description = "Select: Perlin, Voronoi, Cellular, Random, or Gradient",
                        descriptionJP = "選択: パーリン、ボロノイ、セルラー、ランダム、グラデーション"
                    },
                    new UsageStep
                    {
                        title = "Adjust Parameters",
                        titleJP = "パラメータを調整",
                        description = "Configure scale, contrast, and other settings",
                        descriptionJP = "スケール、コントラスト、その他の設定を調整"
                    },
                    new UsageStep
                    {
                        title = "Preview",
                        titleJP = "プレビュー",
                        description = "Check the generated pattern in real-time",
                        descriptionJP = "生成されたパターンをリアルタイムで確認"
                    },
                    new UsageStep
                    {
                        title = "Generate",
                        titleJP = "生成",
                        description = "Click 'Generate Texture' to create the asset",
                        descriptionJP = "「テクスチャを生成」でアセットを作成"
                    }
                },
                tips = new List<string>
                {
                    "Voronoi creates organic dissolve patterns",
                    "Higher contrast = sharper edges",
                    "512x512 is usually sufficient for dissolve"
                },
                tipsJP = new List<string>
                {
                    "ボロノイは有機的なディゾルブパターンを作成",
                    "高コントラスト = シャープなエッジ",
                    "ディゾルブには通常512x512で十分"
                }
            };

            // 他のツールも同様に追加...
            // MatCapLayerComposer, MakeupLayerManager, MaterialComparison, etc.

            AddRemainingTools();
        }

        private static void AddRemainingTools()
        {
            // ===== MatCapレイヤーコンポーザー =====
            documentationDatabase["MatCapLayerComposer"] = new ToolDocumentation
            {
                toolName = "MatCap Layer Composer",
                toolNameJP = "MatCapレイヤーコンポーザー",
                description = "Compose multiple MatCap layers with blend modes",
                descriptionJP = "複数のMatCapレイヤーをブレンドモードで合成",
                category = "Effect",
                featuresJP = new List<string> { "最大3レイヤー", "ブレンドモード", "強度調整", "プレビュー" },
                tipsJP = new List<string> { "レイヤーを重ねて複雑な効果を", "加算モードで光沢感を追加" }
            };

            // ===== パフォーマンスバジェット =====
            documentationDatabase["PerformanceBudget"] = new ToolDocumentation
            {
                toolName = "Performance Budget Tool",
                toolNameJP = "パフォーマンスバジェット",
                description = "Track and manage material performance budget",
                descriptionJP = "マテリアルのパフォーマンスバジェットを追跡・管理",
                category = "Optimization",
                featuresJP = new List<string> { "プラットフォーム別予算", "機能コスト計算", "リアルタイム追跡" },
                tipsJP = new List<string> { "Quest向けは40ポイント以下を目標", "不要な機能は無効化" }
            };

            // ===== VRCライトボリューム =====
            documentationDatabase["VRCLightVolumes"] = new ToolDocumentation
            {
                toolName = "VRC Light Volumes Helper",
                toolNameJP = "VRCライトボリュームヘルパー",
                description = "Configure VRC Light Volumes integration",
                descriptionJP = "VRC Light Volumesの統合を設定",
                category = "VRChat",
                featuresJP = new List<string> { "品質プリセット", "自動設定", "パフォーマンス最適化" },
                tipsJP = new List<string> { "VRChatワールドで最高の照明を得るために使用", "Mediumで十分な品質" }
            };

            // ===== Screen FX セットアップ =====
            documentationDatabase["ScreenFXSetup"] = new ToolDocumentation
            {
                toolName = "Screen FX Setup",
                toolNameJP = "スクリーンエフェクト設定",
                description = "Create a camera-attached full-screen overlay for custom VRC screen effects",
                descriptionJP = "VRC向けカスタム画面効果用のフルスクリーンオーバーレイをカメラに自動作成",
                category = "Effect",
                featuresJP = new List<string> { "GrabPassベース", "ランタイムスクリプト不要", "カメラ自動配置", "推奨初期値" },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Select Camera",
                        titleJP = "カメラを選択",
                        description = "Select a Camera object (or rely on MainCamera fallback)",
                        descriptionJP = "カメラを選択（未選択時はMainCameraを自動使用）"
                    },
                    new UsageStep
                    {
                        title = "Run Setup",
                        titleJP = "セットアップを実行",
                        description = "Run Tools > Natane > Effects > Screen FX Setup",
                        descriptionJP = "Tools > Natane > Effects > Screen FX Setup を実行"
                    },
                    new UsageStep
                    {
                        title = "Tune Material",
                        titleJP = "マテリアルを調整",
                        description = "Adjust posterize/edge/vignette/chromatic aberration on the generated material",
                        descriptionJP = "生成マテリアルの posterize / edge / vignette / chromatic aberration を調整"
                    }
                },
                tipsJP = new List<string>
                {
                    "Quest向けはChromatic AberrationとScanlineを低めに設定",
                    "Overlayはカメラ直下に維持してズレを防止",
                    "Natane Toon本体の陰影を活かす場合はEdgeを0.2前後から調整"
                }
            };

            // ===== プレハブバリアント変換 =====
            documentationDatabase["PrefabVariantConverter"] = new ToolDocumentation
            {
                toolName = "Prefab Variant Converter",
                toolNameJP = "プレハブバリアント変換",
                description = "Create prefab variants and convert lilToon materials to NataneToon",
                descriptionJP = "プレハブバリアントを生成し、lilToonマテリアルをNataneToonに一括変換",
                category = "Utility",
                features = new List<string>
                {
                    "Prefab variant creation",
                    "Batch material conversion",
                    "lilToon to NataneToon migration",
                    "Material naming customization",
                    "Preview before execution"
                },
                featuresJP = new List<string>
                {
                    "プレハブバリアントの自動生成",
                    "マテリアルの一括変換",
                    "lilToonからNataneToonへの移行",
                    "マテリアル名のカスタマイズ",
                    "実行前のプレビュー"
                },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Select Prefab",
                        titleJP = "プレハブを選択",
                        description = "Select a prefab in the Scene or Project view",
                        descriptionJP = "シーンまたはプロジェクトビューでプレハブを選択します"
                    },
                    new UsageStep
                    {
                        title = "Configure Settings",
                        titleJP = "設定を構成",
                        description = "Set material prefix/suffix and save locations",
                        descriptionJP = "マテリアルの接頭辞/接尾辞と保存場所を設定します"
                    },
                    new UsageStep
                    {
                        title = "Preview Materials",
                        titleJP = "マテリアルをプレビュー",
                        description = "Review detected lilToon materials and select which to convert",
                        descriptionJP = "検出されたlilToonマテリアルを確認し、変換するものを選択します"
                    },
                    new UsageStep
                    {
                        title = "Execute Conversion",
                        titleJP = "変換を実行",
                        description = "Click 'Create Variant & Convert' to create the variant and convert materials",
                        descriptionJP = "「バリアント生成＆変換実行」をクリックしてバリアントを作成し、マテリアルを変換します"
                    }
                },
                tips = new List<string>
                {
                    "Original prefab and materials are not modified",
                    "Use consistent prefixes for easy organization",
                    "Preview shows the exact naming before conversion",
                    "Can batch convert multiple materials at once"
                },
                tipsJP = new List<string>
                {
                    "元のプレハブとマテリアルは変更されません",
                    "統一された接頭辞を使用すると整理が簡単",
                    "プレビューで変換前の正確な命名を確認できます",
                    "複数のマテリアルを一括変換可能"
                }
            };

            // ===== UVテクスチャ生成 =====
            documentationDatabase["UVTextureGenerator"] = new ToolDocumentation
            {
                toolName = "UV Texture Generator",
                toolNameJP = "UVテクスチャ生成",
                description = "Generate noise and mask textures from mesh UV islands",
                descriptionJP = "メッシュのUVアイランドからノイズ・マスクテクスチャを生成",
                category = "Texture",
                features = new List<string>
                {
                    "5 noise types (Perlin, Voronoi, Cellular, FBM, Value)",
                    "UV island detection with Union-Find",
                    "Island-based mask generation",
                    "Boundary gradient with erosion",
                    "Noise + Mask combined mode",
                    "Direct material property assignment"
                },
                featuresJP = new List<string>
                {
                    "5種のノイズ (Perlin, Voronoi, Cellular, FBM, Value)",
                    "Union-FindによるUVアイランド検出",
                    "アイランド単位のマスク生成",
                    "侵食法による境界グラデーション",
                    "ノイズ＋マスクの複合モード",
                    "マテリアルプロパティへの直接割当"
                },
                steps = new List<UsageStep>
                {
                    new UsageStep
                    {
                        title = "Choose Tab",
                        titleJP = "タブを選択",
                        description = "Select Noise, UV Mask, or Combined mode",
                        descriptionJP = "ノイズ、UVマスク、複合モードから選択"
                    },
                    new UsageStep
                    {
                        title = "Configure Parameters",
                        titleJP = "パラメータを設定",
                        description = "Set noise type/scale or mesh source/UV channel",
                        descriptionJP = "ノイズタイプ/スケール、またはメッシュソース/UVチャンネルを設定"
                    },
                    new UsageStep
                    {
                        title = "Generate Preview",
                        titleJP = "プレビューを生成",
                        description = "Click 'Generate Texture' to preview the result",
                        descriptionJP = "「テクスチャを生成」でプレビューを確認"
                    },
                    new UsageStep
                    {
                        title = "Save & Assign",
                        titleJP = "保存＆割当",
                        description = "Save as PNG and optionally assign to a material property",
                        descriptionJP = "PNGとして保存し、必要に応じてマテリアルプロパティに割当"
                    }
                },
                tips = new List<string>
                {
                    "Use FBM for natural-looking dissolve patterns",
                    "Voronoi noise works great for cellular masks",
                    "Combined mode lets you create island-specific noise",
                    "512x512 is sufficient for most mask textures"
                },
                tipsJP = new List<string>
                {
                    "自然なディゾルブパターンにはFBMが最適",
                    "ボロノイノイズはセルラーマスクに最適",
                    "複合モードでアイランド固有のノイズを作成可能",
                    "ほとんどのマスクテクスチャには512x512で十分"
                }
            };
        }
    }
}
