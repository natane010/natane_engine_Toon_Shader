using UnityEngine;
using UnityEditor;
using System;
using NataneToon.Editor;

/// <summary>
/// Custom shader GUI for Natane Toon Shader
///
/// Provides a user-friendly, organized interface for the NataneToon shader system
/// with support for:
/// - Material presets and sharing
/// - Performance indicators
/// - Makeup texture layers (up to 4 layers with HSV adjustment)
/// - Advanced shading and lighting controls
/// - VRC Light Volumes integration
/// - Multiple surface effects (Specular, Rim Light, SSS, MatCap, etc.)
///
/// The GUI is organized into logical categories with foldout sections for better
/// navigation and reduced clutter.
/// </summary>
public class NataneToonShaderGUI : ShaderGUI
{
    // ===== CONSTANTS =====
    /// <summary>Threshold for float comparisons (e.g., checking if a toggle is enabled)</summary>
    private const float FLOAT_COMPARISON_THRESHOLD = 0.5f;

    /// <summary>Number of makeup texture layers supported (2nd, 3rd, 4th, 5th)</summary>
    private const int MAKEUP_TEXTURE_COUNT = 4;

    /// <summary>Number of multi-tone shadow levels supported</summary>
    private const int MULTI_SHADOW_LEVELS = 3; // 1st, 2nd, 3rd

    /// <summary>Minimum value to consider a parameter active (avoid floating point issues)</summary>
    private const float MIN_PARAMETER_VALUE = 0.001f;

    // ===== FIELD REFERENCES =====
    /// <summary>All shader properties for the current material</summary>
    private MaterialProperty[] properties;

    /// <summary>Unity's material editor instance</summary>
    private MaterialEditor materialEditor;

    /// <summary>Target material being edited</summary>
    private Material targetMaterial;

    // ===== RENDERING MODE =====
    public enum RenderingMode
    {
        Opaque = 0,
        Cutout = 1,
        Transparent = 2
    }

    // ===== UI STATE =====
    // Tab index for category navigation
    private int selectedTab = 0;
    private readonly string[] tabNames = new string[]
    {
        "基本", "ライティング", "エフェクト", "環境", "詳細"
    };

    // ===== FOLDOUT STATES =====
    // Foldout states are per-material and persisted using EditorPrefs
    private bool showPresets;
    private bool showPerformance;
    private bool showMainTexture;
    private bool showMakeupTextures;
    private bool showShading;
    private bool showAdvancedLighting;
    private bool showLightVolume;
    private bool showSpecular;
    private bool showRimLight;
    private bool showSSS;
    private bool showMatCap;
    private bool showOutline;
    private bool showEmission;
    private bool showVirtualExpression;
    private bool showNormalMap;
    private bool showReflection;
    private bool showEnvironmentalRim;
    private bool showParallax;
    private bool showRefraction;
    private bool showRendering;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        try
        {
            this.materialEditor = materialEditor;
            this.properties = properties;
            this.targetMaterial = materialEditor.target as Material;

            // Validate that we have valid references
            if (this.materialEditor == null || this.properties == null || this.targetMaterial == null)
            {
                EditorGUILayout.HelpBox("マテリアルエディタの初期化に失敗しました。", MessageType.Error);
                return;
            }

            // Load foldout states and UI state from EditorPrefs
            LoadFoldoutStates();
            LoadUIState();

            // Validate and fix shader keywords (ensures keywords match property values)
            ValidateAndFixKeywords();

            // ===== Compact Header =====
            DrawCompactHeader();

            // ===== Tab Navigation =====
            EditorGUI.BeginChangeCheck();
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
            if (EditorGUI.EndChangeCheck())
            {
                SaveUIState();
                GUI.FocusControl(null); // Clear focus to update UI

                // Force repaint and exit GUI to prevent layout conflicts
                if (materialEditor != null)
                {
                    materialEditor.Repaint();
                }
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space(10);

            // ===== Tab Content =====
            switch (selectedTab)
            {
                case 0: // 基本
                    DrawBasicTab();
                    break;
                case 1: // ライティング
                    DrawLightingTab();
                    break;
                case 2: // エフェクト
                    DrawEffectsTab();
                    break;
                case 3: // 環境
                    DrawEnvironmentTab();
                    break;
                case 4: // 詳細
                    DrawAdvancedTab();
                    break;
            }
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"インスペクターの描画中にエラーが発生しました: {e.Message}", MessageType.Error);
            UnityEngine.Debug.LogException(e);
        }
    }

    private void SafeDrawSection(System.Action drawAction, string sectionName)
    {
        try
        {
            drawAction?.Invoke();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"{sectionName}セクションの描画中にエラーが発生しました: {e.Message}", MessageType.Warning);
            UnityEngine.Debug.LogWarning($"[NataneToonShaderGUI] Error drawing {sectionName} section: {e.Message}");
        }
    }

    private void DrawMainTextureSection()
    {
        EditorGUI.BeginChangeCheck();
        showMainTexture = EditorGUILayout.Foldout(showMainTexture, "メインテクスチャ", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showMainTexture)
        {
            EditorGUI.indentLevel++;
            DrawProperty("_MainTex", "メインテクスチャ");
            DrawProperty("_Color", "カラー");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("カラー保持・強化設定", EditorStyles.boldLabel);

            DrawProperty("_AlbedoPreservation", "テクスチャカラー保持");
            DrawHelpToggle("AlbedoPreservation",
                "🎨 テクスチャカラー保持（改善版・色相維持）:\n" +
                "テクスチャの元の色を保持しながら、ライティングの明暗効果を適用します。\n" +
                "ライティングで「白くなる」のではなく「本来の色のまま明るくなる」表現を実現。\n\n" +
                "✨ 新機能:\n" +
                "　・黒などの暗い色が光で白くなる問題を解決\n" +
                "　・色相・彩度を保持したまま明るさだけを調整\n" +
                "　・スペキュラーやリムライトも色に応じて自然に\n\n" +
                "🎛️ 推奨設定:\n" +
                "• 0 = 従来のライティング（色が変わりやすい）\n" +
                "• 0.7-0.9 = 強い色保持（黒や暗い色も自然に）★推奨\n" +
                "• 1.0 = 完全な色保持（最も自然な色）\n\n" +
                "💡 効果:\n" +
                "　・黒い服が光で灰色/白っぽくならない\n" +
                "　・赤い服が光でピンク/白にならない\n" +
                "　・暗い色が本来の色相を保ったまま明るくなる\n" +
                "　・影やシェーディングの明暗は完全に維持\n" +
                "　・Light Volumeの白飛び問題も解決\n\n" +
                "⚠️ 注意:\n" +
                "値を上げすぎると、ライトカラーの影響が減ります。\n" +
                "青いライトで青く照らしたい場合は0.5程度に。",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_Saturation", "彩度");
            DrawHelpToggle("Saturation",
                "✨ 彩度調整:\n" +
                "最終カラーの彩度（色の鮮やかさ）を調整します。\n" +
                "• 0 = モノクロ（グレースケール）\n" +
                "• 1 = デフォルト（元の彩度）\n" +
                "• 1.5-2.0 = 鮮やかな色合い",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_Brightness", "全体明度");
            DrawHelpToggle("Brightness",
                "💡 全体明度調整:\n" +
                "最終的な明るさを微調整します。\n" +
                "• 0.5-0.9 = 暗めに\n" +
                "• 1.0 = デフォルト\n" +
                "• 1.1-1.5 = 明るめに",
                MessageType.Info);

            // Final Color Blending Section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("最終カラーブレンディング", EditorStyles.boldLabel);
            DrawHelpToggle("FinalColorBlending",
                "🎨 最終なじませ処理（白飛び・黒つぶれ防止）:\n" +
                "すべてのエフェクト適用後の最終段階で、明るすぎる部分と暗すぎる部分を\n" +
                "周囲となじませて、より自然で滑らかな見た目にします。",
                MessageType.None);

            EditorGUILayout.Space();
            DrawProperty("_FinalHighlightBlend", "ハイライトなじませ（白飛び防止）");
            DrawProperty("_HighlightThreshold", "ハイライト閾値");
            DrawHelpToggle("HighlightBlend",
                "✨ ハイライトなじませ（トーンマッピング搭載）:\n" +
                "明るすぎる部分（ハイライト）を自然になじませます。\n\n" +
                "📊 処理フロー（4段階）:\n" +
                "1️⃣ スムーズショルダートーンマッピング\n" +
                "   　・輝度60%以上の明るい部分を自然に圧縮\n" +
                "   　・色の鮮やかさを保ちながら白飛びを防止\n" +
                "2️⃣ ハイライトブレンド（閾値以上）\n" +
                "   　・30%のデサチュレーションで過度な彩度を抑制\n" +
                "   　・境界を柔らかくスムーズに\n" +
                "3️⃣ ジェントルクランプ（1.05まで許容）\n" +
                "   　・ハードカットオフを回避\n\n" +
                "🎛️ パラメータ:\n" +
                "• なじませ 0 = 効果なし、1 = 最大圧縮\n" +
                "• 閾値: この値より明るい部分に追加ブレンド（推奨: 0.75）\n\n" +
                "💡 おすすめ設定:\n" +
                "　・通常: なじませ0.3-0.5、閾値0.75\n" +
                "　・強め: なじませ0.7-0.9、閾値0.6\n" +
                "　・最大: なじませ1.0、閾値0.5",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_FinalShadowBlend", "シャドーなじませ（黒つぶれ防止）");
            DrawProperty("_ShadowThreshold", "シャドー閾値");
            DrawHelpToggle("ShadowBlend",
                "🌙 シャドーなじませ:\n" +
                "暗すぎる部分（シャドー）を周囲となじませます。\n" +
                "• なじませ 0 = 効果なし、1 = 最大\n" +
                "• 閾値: この値より暗い部分に適用（推奨: 0.25）\n\n" +
                "💡 効果:\n" +
                "　・黒つぶれを防止し、ディテールを保持\n" +
                "　・影の境界を柔らかく\n" +
                "　・わずかにシャドーを持ち上げて自然に",
                MessageType.Info);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawSurfaceFinishSection()
    {
        // Glossiness control
        DrawProperty("_Glossiness", "光沢度（グロッシネス）");
        DrawHelpToggle("Glossiness",
            "✨ 光沢度（Glossiness）:\n" +
            "スペキュラー、リフレクション、リムライトなどの\n" +
            "反射系エフェクトの強度を一括で調整します。\n\n" +
            "• 0 = 光沢なし（完全マット）\n" +
            "• 0.5 = 中程度の光沢\n" +
            "• 1.0 = 最大光沢（デフォルト）\n\n" +
            "💡 適用されるエフェクト:\n" +
            "　・スペキュラーハイライト\n" +
            "　・リムライト（1 & 2）\n" +
            "　・環境リム\n" +
            "　・MatCap\n" +
            "　・キューブマップリフレクション\n" +
            "　・Light Volume Specular\n\n" +
            "※ Emission（発光）には影響しません",
            MessageType.Info);

        EditorGUILayout.Space();

        // Matte Effect (additional reduction)
        DrawProperty("_MatteEffect", "マット効果（追加の光沢抑制）");
        DrawHelpToggle("MatteEffect",
            "🎨 マット効果:\n" +
            "光沢度に加えて、さらに光沢を減らすための\n" +
            "追加パラメータです。\n\n" +
            "• 0 = 光沢度のみで制御\n" +
            "• 0.5 = 光沢度の50%に減少\n" +
            "• 1.0 = 完全にマット（光沢ゼロ）\n\n" +
            "💡 使い方:\n" +
            "光沢度と組み合わせて、より細かい調整が可能です。\n" +
            "例: 光沢度0.8 × マット効果0.3 = 実質56%の光沢",
            MessageType.Info);
    }

    private void DrawMakeupTexturesSection()
    {
        EditorGUI.BeginChangeCheck();
        showMakeupTextures = EditorGUILayout.Foldout(showMakeupTextures, "メイクアップテクスチャ", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showMakeupTextures)
        {
            EditorGUI.indentLevel++;

            DrawHelpToggle("MakeupTextures",
                "💄 メイクアップテクスチャ（透過PNG対応・HSVカラー調整）\n" +
                "最大4つのメイクアップテクスチャでキャラクターを彩ることができます。\n" +
                "• テクスチャそのまま：デフォルト値(Hue=0, Sat=1, Value=1)で元の色を維持\n" +
                "• 透過PNG対応：アルファチャンネルで適用範囲を制御\n" +
                "• HSVカラー：色相・彩度・明度を直感的に調整可能\n" +
                "• 追加マスク：さらに詳細な制御が可能\n" +
                "• 複数のブレンドモードで自然なメイクアップ表現",
                MessageType.None);

            EditorGUILayout.Space(5);

            // Draw each makeup texture layer using helper method
            DrawMakeupTextureLayer("2nd", "2ND_TEXTURE", "2ND_TEX_MASK", "ハイライト、アイシャドウ");
            EditorGUILayout.Space(10);

            DrawMakeupTextureLayer("3rd", "3RD_TEXTURE", "3RD_TEX_MASK", "チーク");
            EditorGUILayout.Space(10);

            DrawMakeupTextureLayer("4th", "4TH_TEXTURE", "4TH_TEX_MASK", "グリッター、シャドウ");
            EditorGUILayout.Space(10);

            DrawMakeupTextureLayer("5th", "5TH_TEXTURE", "5TH_TEX_MASK", "細部のハイライト、細部のシャドウ");

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    /// <summary>
    /// Helper method to draw a single makeup texture layer with all its properties
    /// Reduces code duplication for 2nd, 3rd, 4th, 5th textures
    /// </summary>
    private void DrawMakeupTextureLayer(string layerName, string textureKeyword, string maskKeyword, string usageHint)
    {
        bool useTexture = DrawToggle($"_{textureKeyword}", $"_Use{layerName}Texture", $"{layerName} Textureを有効化");

        if (useTexture)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"{layerName} Texture設定", EditorStyles.boldLabel);

            // Texture and HSV controls
            DrawProperty($"_{layerName}Tex", $"{layerName} Texture");
            DrawProperty($"_{layerName}TexHueShift", "Hue Shift");
            DrawProperty($"_{layerName}TexSaturation", "Saturation");
            DrawProperty($"_{layerName}TexValue", "Brightness");

            DrawHelpToggle("MakeupTextureHSV",
                "💡 HSVカラー調整:\n" +
                "• Hue=0, Sat=1, Value=1 = テクスチャそのまま\n" +
                "• Hue Shift = 色相を変更（-0.5～0.5）\n" +
                "• Saturation = 彩度調整（0～2、1=元の彩度）\n" +
                "• Brightness = 明度調整（0～2、1=元の明度）",
                MessageType.None);

            // Intensity and blend mode
            DrawProperty($"_{layerName}TexIntensity", "強度");
            DrawProperty($"_{layerName}TexBlendMode", "ブレンドモード");

            DrawHelpToggle("MakeupTextureBlendMode",
                $"ブレンドモード:\n" +
                "• Add: 加算（ハイライトに最適）\n" +
                $"• Multiply: 乗算（{usageHint}に最適）\n" +
                "• Overlay: オーバーレイ（自然なメイク）\n" +
                "• Screen: スクリーン（柔らかいハイライト）\n\n" +
                "💡 透過PNG対応：\n" +
                "テクスチャのアルファチャンネルが適用強度として使用されます",
                MessageType.Info);

            // Optional mask
            EditorGUILayout.Space();
            bool useMask = DrawToggle($"_{maskKeyword}", $"_Use{layerName}TexMask", $"{layerName} Texマスクを使用");
            if (useMask)
            {
                DrawProperty($"_{layerName}TexMask", $"{layerName} Texマスク");
                DrawHelpToggle("MakeupTextureMask",
                    "白 = テクスチャ適用、黒 = 適用なし\n" +
                    "マスクはアルファチャンネルと乗算されます",
                    MessageType.Info);
            }
        }
    }

    private void DrawShadingSection()
    {
        EditorGUI.BeginChangeCheck();
        showShading = EditorGUILayout.Foldout(showShading, "シェーディング", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showShading)
        {
            EditorGUI.indentLevel++;

            DrawHelpToggle("ShadingSection",
                "🎨 NiloToonスタイルのセルシェーディング\n" +
                "クリーンで明瞭な陰影境界を実現し、高品質なアニメ調レンダリングを提供します。",
                MessageType.None);

            EditorGUILayout.Space(5);

            // Main shading controls (Ramp or Toon/Gradient mode)
            DrawShadingModeControls();

            EditorGUILayout.Space(10);

            // Shadow Receive Mask
            DrawShadowReceiveMaskControls();

            EditorGUILayout.Space(10);

            // Ambient Occlusion
            DrawAmbientOcclusionControls();

            EditorGUILayout.Space(10);

            // Dithering
            DrawDitheringControls();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    /// <summary>
    /// Draw shading mode controls (Ramp texture or Toon/Gradient mode)
    /// </summary>
    private void DrawShadingModeControls()
    {
        bool useRamp = DrawToggle("_USE_RAMP", "_UseRamp", "ランプテクスチャを使用");

        if (useRamp)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("ランプテクスチャ設定", EditorStyles.boldLabel);
            DrawProperty("_RampTex", "ランプテクスチャ");
            DrawHelpToggle("RampTexture",
                "ランプテクスチャは暗い色（左）から明るい色（右）へのグラデーションにしてください。\n" +
                "カスタムグラデーションで独自の影の色合いを作成できます。",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("シェーディングモード", EditorStyles.boldLabel);
            DrawProperty("_ShadingMode", "モード");
            DrawHelpToggle("ShadingMode",
                "🎨 シェーディングモード:\n" +
                "• Toon: 階段状のセルシェーディング（クラシックなアニメ調）\n" +
                "• Gradient: 滑らかなグラデーションシェーディング（柔らかい印象）",
                MessageType.None);

            EditorGUILayout.Space(5);

            // Get current shading mode value
            MaterialProperty shadingModeProp = FindProperty("_ShadingMode", properties, false);
            bool isGradientMode = shadingModeProp != null && shadingModeProp.floatValue >= FLOAT_COMPARISON_THRESHOLD;

            if (isGradientMode)
            {
                DrawGradientModeSettings();
            }
            else
            {
                DrawToonModeSettings();
            }
        }

        // Common controls for all shading modes
        EditorGUILayout.Space(5);
        DrawProperty("_ShadowOffset", "影のオフセット");
        DrawHelpToggle("ShadowOffset",
            "影の境界を調整します。正の値で影を明るく、負の値で影を暗くします。",
            MessageType.Info);

        EditorGUILayout.Space(5);
        DrawProperty("_LitSoftness", "ライト部分のソフトネス");
        DrawHelpToggle("LitSoftness",
            "✨ ライト部分のなじませ調整:\n" +
            "光の当たっている部分を周囲となじませます。\n" +
            "• 0 = シャープな境界（デフォルト）\n" +
            "• 0.3-0.5 = 適度な柔らかさ\n" +
            "• 1.0 = 最大のなじませ効果\n\n" +
            "💡 使い方: 光の当たり方が強すぎる場合や、\n" +
            "より滑らかなグラデーションが欲しい場合に調整してください。",
            MessageType.Info);

        EditorGUILayout.Space(5);
        DrawBlendParameter(
            "_ShadowBlend",
            "影のなじませ（柔らかさ）",
            "✨ 影のなじませ調整:\n" +
            "影の境界を周囲となじませて、より柔らかい印象にします。\n" +
            "• 0 = シャープな境界（デフォルト）\n" +
            "• 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
            "• 0.7-1.0 = 非常に柔らかい境界\n\n" +
            "💡 使い方: 影の境界が鋭すぎる場合や、\n" +
            "よりイラスト調の柔らかな影が欲しい場合に調整してください。");
    }

    /// <summary>
    /// Draw gradient mode specific settings
    /// </summary>
    private void DrawGradientModeSettings()
    {
        EditorGUILayout.LabelField("グラデーション設定", EditorStyles.boldLabel);
        DrawProperty("_ShadowColor", "影の色");
        DrawProperty("_ShadingGradientWidth", "グラデーション幅");
        DrawHelpToggle("ShadingGradientWidth",
            "✨ グラデーション幅:\n" +
            "影と光の境界の滑らかさを調整します。\n" +
            "• 0.1 = 狭いグラデーション（シャープな境界）\n" +
            "• 0.2-0.3 = 標準的なグラデーション（推奨）\n" +
            "• 0.5+ = 広いグラデーション（非常に柔らかい）\n\n" +
            "💡 柔らかい印象を与えるために、0.2以上の値がおすすめです。",
            MessageType.Info);
    }

    /// <summary>
    /// Draw toon mode specific settings
    /// </summary>
    private void DrawToonModeSettings()
    {
        EditorGUILayout.LabelField("セルシェーディング設定", EditorStyles.boldLabel);
        DrawProperty("_ShadowColor", "影の色 (1段目)");

        // Multi-tone shadow colors
        EditorGUILayout.Space();
        DrawMultiToneShadowSettings();

        EditorGUILayout.Space();
        DrawProperty("_ShadowSteps", "影のステップ数");
        DrawHelpToggle("ShadowSteps",
            "推奨値: 2-3（アニメ調）、より多いステップでグラデーション効果",
            MessageType.Info);

        DrawProperty("_ShadowSharpness", "影のシャープネス");
        DrawHelpToggle("ShadowSharpness",
            "低い値: シャープな境界（アニメ調）\n" +
            "高い値: 柔らかい境界（イラスト調）\n" +
            "NiloToonスタイル推奨: 0.05-0.15",
            MessageType.Info);
    }

    /// <summary>
    /// Draw multi-tone shadow settings
    /// </summary>
    private void DrawMultiToneShadowSettings()
    {
        bool useMultiShadow = DrawToggle("_USE_MULTI_SHADOW", "_UseMultiShadow", "多段階影を使用");
        if (useMultiShadow)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("多段階影設定", EditorStyles.boldLabel);

            // 2nd shadow level
            DrawProperty("_Shadow2ndColor", "影の色 (2段目)");
            DrawProperty("_Shadow2ndBorder", "2段目の境界");
            DrawHelpToggle("Shadow2ndBorder",
                "💡 2段目の境界:\n" +
                "この値より暗い部分に2段目の影色が適用されます。\n" +
                "• 0.5 = 半分より暗い部分\n" +
                "• 0.3 = やや暗い部分（推奨）\n" +
                "• 0.1 = 最も暗い部分のみ",
                MessageType.None);

            EditorGUILayout.Space();

            // 3rd shadow level
            DrawProperty("_Shadow3rdColor", "影の色 (3段目)");
            DrawProperty("_Shadow3rdBorder", "3段目の境界");
            DrawHelpToggle("Shadow3rdBorder",
                "💡 3段目の境界:\n" +
                "この値より暗い部分に3段目の影色（最も濃い影）が適用されます。\n" +
                "• 0.15-0.2 = 標準的な最暗部（推奨）\n" +
                "• 0.05-0.1 = 非常に暗い部分のみ",
                MessageType.None);

            EditorGUILayout.Space();
            DrawHelpToggle("MultiShadowUsage",
                "🎨 多段階影の使い方:\n" +
                "より細かな諧調表現が可能になります。\n" +
                "• 1段目: メインの影色（明るい影）\n" +
                "• 2段目: 中間の影色\n" +
                "• 3段目: 最も濃い影色（深い影）\n\n" +
                "境界値は 1段目 > 2段目 > 3段目 の順に設定してください。",
                MessageType.Info);
        }
    }

    /// <summary>
    /// Draw shadow receive mask controls
    /// </summary>
    private void DrawShadowReceiveMaskControls()
    {
        bool useShadowReceiveMask = DrawToggle("_SHADOW_RECEIVE_MASK", "_UseShadowReceiveMask", "シャドー受け取りマスクを使用");

        if (useShadowReceiveMask)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("シャドー受け取りマスク設定", EditorStyles.boldLabel);
            DrawProperty("_ShadowReceiveMask", "シャドー受け取りマスク");
            DrawHelpToggle("ShadowReceiveMask",
                "🎭 シャドー受け取りマスク（髪の影問題解決）:\n" +
                "• 白 = 影を受けない（明るく保つ、シェーディングも無効化）\n" +
                "• 黒 = 影を完全に受ける（通常の影とシェーディング）\n" +
                "• グレー = 影を部分的に受ける\n\n" +
                "💡 使い方：\n" +
                "顔が髪の影で暗くなる場合、顔部分を白く塗ったマスクを使用することで\n" +
                "顔に影がかからないようにできます。VRChatアバターでよく使われるテクニックです。\n\n" +
                "🌟 Light Volumeとの統合：\n" +
                "マスクはLight Volumeの間接光（リム効果）も制御します。\n" +
                "白い部分は暗いワールドでも明るく保たれます。",
                MessageType.Info);
        }
    }

    /// <summary>
    /// Draw ambient occlusion controls
    /// </summary>
    private void DrawAmbientOcclusionControls()
    {
        bool useAO = DrawToggle("_USE_AO", "_UseAO", "アンビエントオクルージョン（AO）を使用");

        if (useAO)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("AO設定", EditorStyles.boldLabel);
            DrawProperty("_AOMap", "AOマップ");
            DrawProperty("_AOIntensity", "AO強度");
            DrawHelpToggle("AmbientOcclusion",
                "🌑 アンビエントオクルージョン（AO）:\n" +
                "隙間や窪みなど、環境光が届きにくい部分を暗くして\n" +
                "より立体的で柔らかい印象を与えます。\n\n" +
                "• AOマップ: 白 = 明るい、黒 = 暗い\n" +
                "• AO強度: 0 = 効果なし、1 = 最大効果\n\n" +
                "💡 使い方:\n" +
                "衣服の折り目、髪の毛の重なり、耳の内側など\n" +
                "自然な陰影を加えたい部分にAOマップで指定します。\n" +
                "推奨強度: 0.5-0.8",
                MessageType.Info);
        }
    }

    /// <summary>
    /// Draw a blend/softness parameter with help text
    /// </summary>
    private void DrawBlendParameter(string propertyName, string label, string helpText)
    {
        DrawProperty(propertyName, label);
        EditorGUILayout.HelpBox(helpText, MessageType.Info);
    }

    /// <summary>
    /// Draw dithering controls
    /// </summary>
    private void DrawDitheringControls()
    {
        bool useDithering = DrawToggle("_USE_DITHERING", "_UseDithering", "ディザリング（ハーフトーン）を使用");

        if (useDithering)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("ディザリング設定", EditorStyles.boldLabel);
            DrawProperty("_DitheringScale", "ディザリングスケール");
            DrawProperty("_DitheringStrength", "ディザリング強度");
            DrawHelpToggle("Dithering",
                "🎨 ディザリング（ハーフトーン）:\n" +
                "影の境界にドットパターンを追加して、\n" +
                "より柔らかく芸術的な印象を与えます。\n\n" +
                "• スケール: パターンの細かさ（推奨: 5-20）\n" +
                "  　小さい値 = 細かいパターン\n" +
                "  　大きい値 = 粗いパターン\n" +
                "• 強度: 効果の強さ（推奨: 0.3-0.7）\n" +
                "  　0 = 効果なし、1 = 最大効果\n\n" +
                "💡 使い方:\n" +
                "印刷物やマンガ風の柔らかい影の表現に最適です。",
                MessageType.Info);
        }
    }

    private void DrawAdvancedLightingSection()
    {
        EditorGUI.BeginChangeCheck();
        showAdvancedLighting = EditorGUILayout.Foldout(showAdvancedLighting, "高度なライティング", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showAdvancedLighting)
        {
            EditorGUI.indentLevel++;

            // Soft Lighting Mode
            EditorGUILayout.LabelField("ソフトライティングモード", EditorStyles.boldLabel);
            bool softLightingMode = DrawToggle("_SOFT_LIGHTING_MODE", "_SoftLightingMode", "ソフトライティングモード（グローバル）");
            if (softLightingMode)
            {
                DrawProperty("_SoftLightingIntensity", "ソフトライティング強度");
                DrawHelpToggle("SoftLightingMode",
                    "🌟 ソフトライティングモード:\n" +
                    "すべてのライティングとシェーディングを柔らかくします。\n" +
                    "このモードを有効にすると、影の境界、ライトの強さ、\n" +
                    "ハイライトなどが全体的に柔らかく調整されます。\n\n" +
                    "• 強度 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
                    "• 強度 0.5-0.7 = かなり柔らかい\n" +
                    "• 強度 0.7-1.0 = 非常に柔らかい\n\n" +
                    "💡 イラスト調や水彩画風の柔らかな表現に最適です。",
                    MessageType.Info);
                EditorGUILayout.Space();
            }

            // Global Light Controls
            EditorGUILayout.LabelField("グローバルライト制御", EditorStyles.boldLabel);
            DrawProperty("_LightIntensity", "ライト強度（グローバル）");
            DrawHelpToggle("LightIntensity", "全体的なライティングの強さを制御します。0 = ライトなし、1 = 標準、2 = 明るい", MessageType.Info);

            DrawProperty("_IndirectLightIntensity", "間接光の強度");
            DrawHelpToggle("IndirectLightIntensity", "環境光やライトプローブからの間接照明の強さを制御します。", MessageType.Info);

            DrawProperty("_GIIntensity", "GI強度（環境反射）");
            DrawHelpToggle("GIIntensity", "環境反射（Light Probes/GI）の影響度を制御します。\n• 0 = 環境反射を完全に無効化（環境光の影響を受けない）\n• 0.5 = 環境反射を50%に軽減\n• 1 = 通常通り環境反射を適用\nVRChatで暗いワールドやライティングが強すぎるワールドで、見た目を安定させるために使用します。", MessageType.Info);

            DrawProperty("_LightColorInfluence", "ライトカラー影響度");
            DrawHelpToggle("LightColorInfluence", "ライトの色がマテリアルに与える影響を制御します。\n• 0 = ライトの色を無視（白色光として処理）\n• 1 = ライトの色を完全に反映\n• 0.5 = 中間（推奨）", MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("シャドウ設定", EditorStyles.boldLabel);
            DrawProperty("_ShadowReceive", "影の受け取り");
            DrawHelpToggle("ShadowReceive", "他のオブジェクトからの影がこのマテリアルに与える影響を制御します。1 = 完全な影、0 = 影なし。", MessageType.Info);

            DrawProperty("_ShadowMaxDarkness", "影の最大暗さ");
            DrawHelpToggle("ShadowMaxDarkness", "影の最小明るさです。0 = 完全に暗い、1 = 暗くならない。影が真っ黒になりすぎるのを防ぎます。", MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ライト影響範囲", EditorStyles.boldLabel);
            DrawProperty("_LightMinInfluence", "ライトの最小影響");
            DrawProperty("_LightMaxInfluence", "ライトの最大影響");
            DrawHelpToggle("LightInfluenceRange", "最小/最大で明るさの範囲を制御します。最小値は暗くなりすぎを防ぎ、最大値は露出オーバーを防ぎます。", MessageType.Info);

            EditorGUILayout.Space();
            DrawBlendParameter(
                "_LightBlend",
                "ライトのなじませ（柔らかさ）",
                "✨ ライトのなじませ調整:\n" +
                "ライティングの変化を周囲となじませて、より滑らかにします。\n" +
                "• 0 = シャープな変化（デフォルト）\n" +
                "• 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
                "• 0.7-1.0 = 非常に柔らかい変化\n\n" +
                "💡 ライトの強弱が急激すぎる場合に調整してください。");

            EditorGUILayout.Space();
            DrawBlendParameter(
                "_HighlightSoftness",
                "ハイライトの柔らかさ",
                "✨ ハイライトのなじませ調整:\n" +
                "明るい部分（ハイライト）を周囲となじませます。\n" +
                "• 0 = シャープなハイライト（デフォルト）\n" +
                "• 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
                "• 0.7-1.0 = 非常に柔らかいハイライト\n\n" +
                "💡 ハイライトが強すぎる場合や、\n" +
                "より柔らかい印象が欲しい場合に調整してください。");

            EditorGUILayout.Space();
            DrawProperty("_BacklightIntensity", "逆光の強さ");
            if (targetMaterial.GetFloat("_BacklightIntensity") > 0)
            {
                DrawProperty("_BacklightColor", "逆光の色");
                DrawHelpToggle("Backlight", "逆光はオブジェクトの背後に光がある時に照明を追加し、リムライトのような効果を作ります。", MessageType.Info);
            }

            EditorGUILayout.Space();
            DrawProperty("_AdditionalLightIntensity", "追加ライトの強さ");
            DrawHelpToggle("AdditionalLight", "追加ライト（ForwardAddパス）の強度を制御します。低い値は複数のライトを使用する際の明るくなりすぎを防ぎます。0 = 追加ライトなし、1 = 最大強度。", MessageType.Info);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawLightVolumeSection()
    {
        EditorGUI.BeginChangeCheck();
        showLightVolume = EditorGUILayout.Foldout(showLightVolume, "VRC Light Volumes", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showLightVolume)
        {
            EditorGUI.indentLevel++;

            bool enableLightVolume = DrawToggle("_USE_LIGHT_VOLUME", "_UseLightVolume", "Light Volumeを有効化");

            if (enableLightVolume)
            {
                DrawHelpToggle("LightVolumeIntro", "VRC Light Volumesはボクセルベースの次世代ライティングシステムです。対応ワールドでより正確な部分的照明が可能になります。", MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_LightVolumeIntensity", "Light Volumeの強さ");
                DrawHelpToggle("LightVolumeIntensity", "Light Volumeライティングの強度を制御します。1 = 完全強度、0 = 無効。", MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_LightVolumeBlendMode", "ブレンドモード");
                DrawHelpToggle("LightVolumeBlendMode",
                    "🎨 Light Volumeブレンドモード (lilToon互換実装):\n" +
                    "• Add: 直接光をアンビエントに、間接光をリムライトとして統合\n" +
                    "  　　　（デフォルト、lilToon推奨方式、最も自然）\n" +
                    "• Multiply: 既存のライティングと乗算（暗くなる効果）\n" +
                    "• Replace: Light Volumeで置き換え（完全に制御）\n\n" +
                    "💡 Addモード:\n" +
                    "　lilToonと同じ方式で、Light Volumeの直接光と間接光を分離。\n" +
                    "　間接光は自然なリム効果として統合され、より立体的な表現に。\n" +
                    "　Shadow Receive Maskで間接光の影響を制御できます。",
                    MessageType.Info);

                EditorGUILayout.Space();
                bool enableSpecular = DrawToggle("_LIGHT_VOLUME_SPECULAR", "_LightVolumeSpecular", "Light Volume スペキュラー");
                if (enableSpecular)
                {
                    DrawHelpToggle("LightVolumeSpecular", "Light Volumeからカラースペキュラーを生成します。アバターに推奨されます。", MessageType.Info);
                }

                EditorGUILayout.Space();
                DrawHelpToggle("LightVolumeNotes",
                    "注意：\n" +
                    "• ワールドとアバター両方が対応している必要があります\n" +
                    "• 非対応環境では自動的にUnityのライトプローブにフォールバックします\n" +
                    "• ハッシュタグ #VRCLightVolumesReady で対応ワールドを検索できます",
                    MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawSpecularSection()
    {
        EditorGUI.BeginChangeCheck();
        showSpecular = EditorGUILayout.Foldout(showSpecular, "スペキュラー", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showSpecular)
        {
            EditorGUI.indentLevel++;

            bool enableSpecular = DrawToggle("_SPECULAR", "_Specular", "スペキュラーを有効化");

            if (enableSpecular)
            {
                DrawProperty("_SpecularColor", "スペキュラーの色");
                DrawProperty("_SpecularSize", "スペキュラーのサイズ");
                DrawProperty("_SpecularSoftness", "スペキュラーの柔らかさ");

                EditorGUILayout.Space();
                bool useSpecularMask = DrawToggle("_SPECULAR_MASK", "_UseSpecularMask", "スペキュラーマスクを使用");
                if (useSpecularMask)
                {
                    DrawProperty("_SpecularMask", "スペキュラーマスク");
                    DrawHelpToggle("SpecularMask", "白 = スペキュラーあり、黒 = スペキュラーなし", MessageType.Info);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawRimLightSection()
    {
        EditorGUI.BeginChangeCheck();
        showRimLight = EditorGUILayout.Foldout(showRimLight, "リムライト", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showRimLight)
        {
            EditorGUI.indentLevel++;

            bool enableRimLight = DrawToggle("_RIM_LIGHT", "_RimLight", "リムライトを有効化");

            if (enableRimLight)
            {
                DrawProperty("_RimColor", "リムライトの色");
                DrawProperty("_RimPower", "リムライトのパワー");
                DrawProperty("_RimIntensity", "リムライトの強さ");

                EditorGUILayout.Space();
                DrawProperty("_RimSpread", "リムの広がり（グロー）");
                DrawHelpToggle("RimSpread",
                    "✨ リムの広がり（グロー効果）:\n" +
                    "リムライトを滲ませて広げ、柔らかく発光している\n" +
                    "ような効果を追加します。\n\n" +
                    "• 0 = 広がりなし（シャープなリム）\n" +
                    "• 0.3-0.5 = 適度な広がり（推奨）\n" +
                    "• 0.7-1.0 = 大きな広がり（強いグロー）",
                    MessageType.None);

                EditorGUILayout.Space();
                bool useRimMask = DrawToggle("_RIM_MASK", "_UseRimMask", "リムマスクを使用");
                if (useRimMask)
                {
                    DrawProperty("_RimMask", "リムマスク");
                    DrawHelpToggle("RimMask", "白 = リムライトあり、黒 = リムライトなし", MessageType.Info);
                }
            }

            EditorGUILayout.Space(10);

            // Rim Light 2
            bool enableRimLight2 = DrawToggle("_RIM_LIGHT_2", "_RimLight2", "リムライト2を有効化（多段階リム）");

            if (enableRimLight2)
            {
                DrawHelpToggle("RimLight2",
                    "🌟 多段階リムライト:\n" +
                    "2つのリムライトレイヤーを重ねることで、\n" +
                    "より複雑で立体的なリムライト表現が可能です。\n\n" +
                    "💡 おすすめ設定:\n" +
                    "リムライト1: 明るい色、低いパワー（内側の輪郭）\n" +
                    "リムライト2: 淡い色、高いパワー（外側の輪郭）",
                    MessageType.None);

                DrawProperty("_RimColor2", "リムライト2の色");
                DrawProperty("_RimPower2", "リムライト2のパワー");
                DrawProperty("_RimIntensity2", "リムライト2の強さ");

                EditorGUILayout.Space();
                DrawProperty("_RimSpread2", "リムの広がり2（グロー）");
                DrawHelpToggle("RimSpread2",
                    "✨ リムの広がり（グロー効果）:\n" +
                    "リムライト2を滲ませて広げます。",
                    MessageType.None);

                EditorGUILayout.Space();
                bool useRimMask2 = DrawToggle("_RIM_MASK_2", "_UseRimMask2", "リムマスク2を使用");
                if (useRimMask2)
                {
                    DrawProperty("_RimMask2", "リムマスク2");
                    DrawHelpToggle("RimMask2", "白 = リムライトあり、黒 = リムライトなし", MessageType.Info);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawSSSSection()
    {
        EditorGUI.BeginChangeCheck();
        showSSS = EditorGUILayout.Foldout(showSSS, "サブサーフェススキャッタリング (SSS)", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showSSS)
        {
            EditorGUI.indentLevel++;

            bool enableSSS = DrawToggle("_SSS", "_SSS", "SSSを有効化");

            if (enableSSS)
            {
                DrawProperty("_SSSColor", "SSSの色");
                DrawProperty("_SSSIntensity", "SSSの強さ");
                DrawProperty("_SSSPower", "SSSのパワー");
                DrawProperty("_SSSDistortion", "SSSの歪み");

                EditorGUILayout.Space();
                bool useThicknessMap = DrawToggle("_THICKNESS_MAP", "_UseThicknessMap", "厚さマップを使用");

                if (useThicknessMap)
                {
                    DrawProperty("_ThicknessMap", "厚さマップ");
                    DrawHelpToggle("ThicknessMap", "白 = 薄い（SSSが強い）、黒 = 厚い（SSSが弱い）", MessageType.Info);
                }

                DrawProperty("_ThicknessScale", "厚さのスケール");

                EditorGUILayout.Space();
                bool useSSSMask = DrawToggle("_SSS_MASK", "_UseSSS_Mask", "SSSマスクを使用");
                if (useSSSMask)
                {
                    DrawProperty("_SSSMask", "SSSマスク");
                    DrawHelpToggle("SSSMask", "白 = SSSあり、黒 = SSSなし", MessageType.Info);
                }

                DrawHelpToggle("SSSInfo", "SSSはオブジェクトを通過する光をシミュレートします。肌、葉、薄い素材に最適です。", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawMatCapSection()
    {
        EditorGUI.BeginChangeCheck();
        showMatCap = EditorGUILayout.Foldout(showMatCap, "MatCap", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showMatCap)
        {
            EditorGUI.indentLevel++;

            bool enableMatCap = DrawToggle("_MATCAP", "_MatCap", "MatCapを有効化");

            if (enableMatCap)
            {
                DrawProperty("_MatCapTex", "MatCapテクスチャ");
                DrawProperty("_MatCapIntensity", "MatCapの強さ");
                DrawProperty("_MatCapBlendMode", "MatCapのブレンドモード");

                EditorGUILayout.Space();
                bool useMatCapMask = DrawToggle("_MATCAP_MASK", "_UseMatCapMask", "MatCapマスクを使用");
                if (useMatCapMask)
                {
                    DrawProperty("_MatCapMask", "MatCapマスク");
                    DrawHelpToggle("MatCapMask", "白 = MatCapあり、黒 = MatCapなし", MessageType.Info);
                }

                DrawHelpToggle("MatCapInfo", "MatCapテクスチャは球面反射マップである必要があります。", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawOutlineSection()
    {
        // Check if outline is enabled for header indicator
        bool outlineEnabled = targetMaterial.IsKeywordEnabled("_OUTLINE");
        string headerLabel = outlineEnabled ? "アウトライン [ON]" : "アウトライン";

        EditorGUI.BeginChangeCheck();
        showOutline = EditorGUILayout.Foldout(showOutline, headerLabel, true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showOutline)
        {
            EditorGUI.indentLevel++;

            bool enableOutline = DrawToggle("_OUTLINE", "_Outline", "アウトラインを有効化");

            if (enableOutline)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("アウトライン設定", EditorStyles.boldLabel);

                DrawProperty("_OutlineMode", "描画方法");
                DrawProperty("_OutlineWidth", "アウトラインの幅");
                DrawProperty("_OutlineColor", "アウトラインの色");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("アウトラインマスク", EditorStyles.boldLabel);

                bool useOutlineMask = DrawToggle("_OUTLINE_MASK", "_UseOutlineMask", "アウトラインマスクを使用");
                if (useOutlineMask)
                {
                    DrawProperty("_OutlineMask", "アウトラインマスク (R)");
                    DrawHelpToggle("OutlineMask",
                        "アウトラインマスクのR(赤)チャンネルを使用してアウトラインの表示を制御します。\n" +
                        "• 白 (1.0): アウトラインを完全に表示\n" +
                        "• 黒 (0.0): アウトラインを非表示\n" +
                        "• グレー: 部分的に表示",
                        MessageType.Info);
                }

                EditorGUILayout.Space(5);

                float outlineMode = targetMaterial.GetFloat("_OutlineMode");
                if (outlineMode < FLOAT_COMPARISON_THRESHOLD)
                {
                    DrawHelpToggle("OutlineInvertedHull",
                        "【反転ハル方式】\n" +
                        "法線方向に頂点を押し出してアウトラインを描画します。\n" +
                        "• 利点: 一般的に安定した結果、距離補正により遠近で一貫した太さ\n" +
                        "• 欠点: ローポリモデルやハードエッジで乱れる場合があります\n" +
                        "• NiloToon互換: カメラ距離による自動調整機能を搭載",
                        MessageType.Info);
                }
                else
                {
                    DrawHelpToggle("OutlineBackface",
                        "【背面法】\n" +
                        "メッシュを拡大して背面を描画します。\n" +
                        "• 利点: スムーズなアウトライン、ハイポリモデルに適しています\n" +
                        "• 欠点: 内部構造が見える場合があります\n" +
                        "• NiloToon互換: 距離補正により遠くでも視認性を維持",
                        MessageType.Info);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawEmissionSection()
    {
        EditorGUI.BeginChangeCheck();
        showEmission = EditorGUILayout.Foldout(showEmission, "エミッション（発光）", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showEmission)
        {
            EditorGUI.indentLevel++;

            bool enableEmission = DrawToggle("_EMISSION", "_Emission", "エミッションを有効化");

            if (enableEmission)
            {
                DrawProperty("_EmissionColor", "エミッションの色");
                DrawProperty("_EmissionMap", "エミッションマップ");

                EditorGUILayout.Space();
                bool enableScroll = DrawToggle("_EMISSION_SCROLL", "_EmissionScroll", "エミッションのスクロール");
                if (enableScroll)
                {
                    DrawProperty("_EmissionScrollSpeed", "スクロール速度");
                }

                bool enablePulse = DrawToggle("_EMISSION_PULSE", "_EmissionPulse", "エミッションのパルス");
                if (enablePulse)
                {
                    DrawProperty("_EmissionPulseSpeed", "パルス速度");
                    DrawProperty("_EmissionPulseAmplitude", "パルスの振幅");
                }

                EditorGUILayout.Space();
                bool useEmissionMask = DrawToggle("_EMISSION_MASK", "_UseEmissionMask", "エミッションマスクを使用");
                if (useEmissionMask)
                {
                    DrawProperty("_EmissionMask", "エミッションマスク");
                    DrawHelpToggle("EmissionMask", "白 = エミッションあり、黒 = エミッションなし", MessageType.Info);
                }

                EditorGUILayout.Space();
                DrawProperty("_EmissionGlow", "エミッショングロー（ブルーム）");
                DrawHelpToggle("EmissionGlow",
                    "✨ エミッショングロー（ブルーム効果）:\n" +
                    "発光部分を滲ませて明るく広げ、柔らかく\n" +
                    "輝いているような効果を追加します。\n\n" +
                    "• 0 = グローなし（シャープな発光）\n" +
                    "• 0.3-0.5 = 適度なグロー（推奨）\n" +
                    "• 0.7-1.0 = 強いグロー（強烈な輝き）\n\n" +
                    "💡 ヒント:\n" +
                    "HDRカラーと組み合わせると、より鮮やかな\n" +
                    "発光効果が得られます。",
                    MessageType.None);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawVirtualExpressionSection()
    {
        EditorGUI.BeginChangeCheck();
        showVirtualExpression = EditorGUILayout.Foldout(showVirtualExpression, "バーチャル表現", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showVirtualExpression)
        {
            EditorGUI.indentLevel++;

            // Dissolve Effect
            bool enableDissolve = DrawToggle("_DISSOLVE", "_Dissolve", "ディゾルブを有効化");
            if (enableDissolve)
            {
                DrawProperty("_DissolveAmount", "ディゾルブ量");
                DrawHelpToggle("DissolveAmount", "0 = 完全に表示、1 = 完全に消滅", MessageType.Info);

                DrawProperty("_DissolveTex", "ディゾルブテクスチャ（ノイズ）");
                DrawProperty("_DissolveEdgeWidth", "エッジの幅");
                DrawProperty("_DissolveEdgeColor", "エッジの色");
                DrawProperty("_DissolveEdgeIntensity", "エッジの強さ");

                EditorGUILayout.Space();
                bool useDissolveMask = DrawToggle("_DISSOLVE_MASK", "_UseDissolveMask", "ディゾルブマスクを使用");
                if (useDissolveMask)
                {
                    DrawProperty("_DissolveMask", "ディゾルブマスク");
                    DrawHelpToggle("DissolveMask", "白 = ディゾルブあり、黒 = ディゾルブなし", MessageType.Info);
                }

                DrawHelpToggle("DissolveInfo", "ディゾルブはVRChatアバターの出現アニメーションに最適な消滅・分解エフェクトを作成します。ディゾルブ量パラメータをアニメーションさせることで、オブジェクトを出現または消滅させることができます。", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Hue Shift
            bool enableHueShift = DrawToggle("_HUE_SHIFT", "_HueShiftEnable", "色相シフトを有効化");
            if (enableHueShift)
            {
                DrawProperty("_HueShift", "色相シフト");
                DrawHelpToggle("HueShift", "マテリアル全体の色相を変更します。0 = 変更なし、0.5 = 補色、1 = 完全な回転。VRChatでの色変更エフェクトに最適です。", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Alpha Mask
            bool useAlphaMask = DrawToggle("_ALPHA_MASK", "_UseAlphaMask", "アルファマスクを使用");
            if (useAlphaMask)
            {
                DrawProperty("_AlphaMask", "アルファマスク");
                DrawHelpToggle("AlphaMask",
                    "🎭 アルファマスク（部分的な半透明）:\n\n" +
                    "テクスチャのRチャンネルを使用して、マテリアルの透明度を\n" +
                    "部分的に制御します。\n\n" +
                    "• 白（1.0）= 完全に不透明\n" +
                    "• グレー（0.5）= 半透明\n" +
                    "• 黒（0.0）= 完全に透明\n\n" +
                    "💡 用途:\n" +
                    "　・ガラスの一部だけを透明に\n" +
                    "　・服の一部を半透明に（シースルー効果）\n" +
                    "　・グラデーションで端を透明に（フェードアウト）\n" +
                    "　・複雑な形状の透明度制御\n\n" +
                    "⚠️ 注意:\n" +
                    "半透明効果を使用するには、レンダリングモードを\n" +
                    "「Transparent」に設定してください。",
                    MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawNormalMapSection()
    {
        EditorGUI.BeginChangeCheck();
        showNormalMap = EditorGUILayout.Foldout(showNormalMap, "ノーマルマップ", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showNormalMap)
        {
            EditorGUI.indentLevel++;

            bool useNormalMap = DrawToggle("_NORMALMAP", "_UseNormalMap", "ノーマルマップを使用");

            if (useNormalMap)
            {
                DrawProperty("_BumpMap", "ノーマルマップ");
                DrawProperty("_BumpScale", "ノーマルのスケール");
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawReflectionSection()
    {
        EditorGUI.BeginChangeCheck();
        showReflection = EditorGUILayout.Foldout(showReflection, "リフレクション（キューブマップ反射）", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showReflection)
        {
            EditorGUI.indentLevel++;

            bool enableReflection = DrawToggle("_REFLECTION", "_Reflection", "リフレクションを有効化");

            if (enableReflection)
            {
                DrawProperty("_ReflectionCube", "リフレクションキューブマップ");
                DrawProperty("_ReflectionColor", "リフレクションの色");
                DrawProperty("_ReflectionIntensity", "リフレクションの強さ");
                DrawProperty("_Smoothness", "滑らかさ（光沢）");
                DrawProperty("_Metallic", "メタリック");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("フレネル設定（柔らかい反射）", EditorStyles.boldLabel);
                DrawProperty("_FresnelPower", "フレネルパワー");
                DrawProperty("_FresnelSoftness", "フレネルソフトネス");
                DrawHelpToggle("FresnelSoftness",
                    "✨ フレネルソフトネス:\n" +
                    "反射の境界を柔らかくして、より自然で芸術的な反射を実現します。\n" +
                    "• 0 = シャープな境界（現実的）\n" +
                    "• 0.5-0.7 = 柔らかい境界（推奨）\n" +
                    "• 1.0 = 非常に柔らかい境界（芸術的）",
                    MessageType.None);

                DrawProperty("_ReflectionBlendMode", "ブレンドモード");
                DrawHelpToggle("ReflectionBlendMode",
                    "• 0 = 加算（明るく追加）\n" +
                    "• 1 = オーバーレイ（自然な統合）",
                    MessageType.Info);

                EditorGUILayout.Space();
                bool useReflectionMask = DrawToggle("_REFLECTION_MASK", "_UseReflectionMask", "リフレクションマスクを使用");
                if (useReflectionMask)
                {
                    DrawProperty("_ReflectionMask", "リフレクションマスク");
                    DrawHelpToggle("ReflectionMask", "白 = リフレクションあり、黒 = リフレクションなし", MessageType.Info);
                }

                DrawHelpToggle("ReflectionInfo", "キューブマップを使用して環境反射をシミュレートします。金属やガラスなどの反射素材に最適です。", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawEnvironmentalRimSection()
    {
        EditorGUI.BeginChangeCheck();
        showEnvironmentalRim = EditorGUILayout.Foldout(showEnvironmentalRim, "環境リム", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showEnvironmentalRim)
        {
            EditorGUI.indentLevel++;

            bool enableEnvRim = DrawToggle("_ENV_RIM", "_EnvRim", "環境リムを有効化");

            if (enableEnvRim)
            {
                DrawProperty("_EnvRimCube", "環境キューブマップ");
                DrawProperty("_EnvRimColor", "環境リムの色");
                DrawProperty("_EnvRimPower", "環境リムのパワー");
                DrawProperty("_EnvRimIntensity", "環境リムの強さ");

                EditorGUILayout.Space();
                bool useEnvRimMask = DrawToggle("_ENV_RIM_MASK", "_UseEnvRimMask", "環境リムマスクを使用");
                if (useEnvRimMask)
                {
                    DrawProperty("_EnvRimMask", "環境リムマスク");
                    DrawHelpToggle("EnvRimMask", "白 = 環境リムあり、黒 = 環境リムなし", MessageType.Info);
                }

                DrawHelpToggle("EnvRimInfo", "キューブマップを使用して環境に基づいたリムライト効果を作成します。", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawParallaxSection()
    {
        EditorGUI.BeginChangeCheck();
        showParallax = EditorGUILayout.Foldout(showParallax, "視差マッピング", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showParallax)
        {
            EditorGUI.indentLevel++;

            bool enableParallax = DrawToggle("_PARALLAX", "_Parallax", "視差マッピングを有効化");

            if (enableParallax)
            {
                DrawProperty("_ParallaxMap", "高さマップ");
                DrawProperty("_ParallaxScale", "視差のスケール");
                DrawProperty("_ParallaxMinSamples", "最小サンプル数");
                DrawProperty("_ParallaxMaxSamples", "最大サンプル数");

                DrawHelpToggle("ParallaxInfo", "視差マッピングは高さマップを使用してサーフェスに深度の錯覚を作成します。石や壁などの素材に最適です。", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawRefractionSection()
    {
        EditorGUI.BeginChangeCheck();
        showRefraction = EditorGUILayout.Foldout(showRefraction, "屈折", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showRefraction)
        {
            EditorGUI.indentLevel++;

            bool enableRefraction = DrawToggle("_REFRACTION", "_Refraction", "屈折を有効化");

            if (enableRefraction)
            {
                DrawProperty("_RefractionIndex", "屈折率（IOR）");
                DrawProperty("_RefractionIntensity", "屈折の強さ");
                DrawProperty("_RefractionBlur", "屈折のぼかし");

                EditorGUILayout.Space();
                bool useRefractionMask = DrawToggle("_REFRACTION_MASK", "_UseRefractionMask", "屈折マスクを使用");
                if (useRefractionMask)
                {
                    DrawProperty("_RefractionMask", "屈折マスク");
                    DrawHelpToggle("RefractionMask", "白 = 屈折あり、黒 = 屈折なし", MessageType.Info);
                }

                DrawHelpToggle("RefractionInfo", "屈折はガラスや水などの透明素材で光の曲がりをシミュレートします。透明マテリアルに最適です。", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawRenderingSection()
    {
        EditorGUI.BeginChangeCheck();
        showRendering = EditorGUILayout.Foldout(showRendering, "レンダリング", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showRendering)
        {
            EditorGUI.indentLevel++;

            // Rendering Mode Selection
            EditorGUILayout.LabelField("レンダリングモード", EditorStyles.boldLabel);
            RenderingMode currentMode = GetCurrentRenderingMode();

            EditorGUI.BeginChangeCheck();
            RenderingMode newMode = (RenderingMode)EditorGUILayout.EnumPopup("描画タイプ", currentMode);
            if (EditorGUI.EndChangeCheck())
            {
                SetRenderingMode(newMode);
            }

            DrawHelpToggle("RenderingMode",
                "🎨 レンダリングモード:\n\n" +
                "• Opaque（不透明）: 標準的な不透明オブジェクト\n" +
                "  - 肌、服、硬い物体など\n" +
                "  - 最も高速で推奨\n\n" +
                "• Cutout（切り抜き）: アルファ閾値による透過\n" +
                "  - 髪の毛、葉っぱ、フェンスなど\n" +
                "  - アルファ値が0.5以上で表示、未満で非表示\n" +
                "  - 半透明ではなく、完全に透明か不透明かの2択\n\n" +
                "• Transparent（半透明）: 滑らかな透過\n" +
                "  - ガラス、水、煙、エフェクトなど\n" +
                "  - アルファ値に応じて段階的に透過\n" +
                "  - 最も負荷が高い\n\n" +
                "💡 注意:\n" +
                "モードを変更すると、内部的に適切なシェーダーバリアントに\n" +
                "切り替わりますが、すべてのプロパティは保持されます。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Advanced Rendering Settings
            EditorGUILayout.LabelField("詳細設定", EditorStyles.boldLabel);
            DrawProperty("_Cull", "カリングモード");
            DrawProperty("_ZWrite", "Z書き込み");

            // Show blend mode properties for Transparent mode
            if (currentMode == RenderingMode.Transparent)
            {
                DrawProperty("_SrcBlend", "ソースブレンド");
                DrawProperty("_DstBlend", "宛先ブレンド");
                DrawHelpToggle("BlendMode",
                    "🎨 ブレンドモード:\n" +
                    "半透明の合成方法を制御します。\n\n" +
                    "標準設定:\n" +
                    "• ソースブレンド: SrcAlpha (5)\n" +
                    "• 宛先ブレンド: OneMinusSrcAlpha (10)\n\n" +
                    "通常は変更する必要はありません。",
                    MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    /// <summary>
    /// Get current rendering mode based on shader name
    /// </summary>
    private RenderingMode GetCurrentRenderingMode()
    {
        if (targetMaterial == null || targetMaterial.shader == null)
            return RenderingMode.Opaque;

        string shaderName = targetMaterial.shader.name;

        if (shaderName.Contains("Transparent"))
            return RenderingMode.Transparent;
        else if (shaderName.Contains("Cutout"))
            return RenderingMode.Cutout;
        else
            return RenderingMode.Opaque;
    }

    /// <summary>
    /// Set rendering mode by switching to appropriate shader variant
    /// </summary>
    private void SetRenderingMode(RenderingMode mode)
    {
        if (targetMaterial == null)
            return;

        // Get base shader name
        string baseShaderName = "Natane/Toon Shader";
        string newShaderName = baseShaderName;

        // Determine shader variant based on mode
        switch (mode)
        {
            case RenderingMode.Opaque:
                newShaderName = baseShaderName;
                break;
            case RenderingMode.Cutout:
                newShaderName = baseShaderName + " (Cutout)";
                break;
            case RenderingMode.Transparent:
                newShaderName = baseShaderName + " (Transparent)";
                break;
        }

        // Find the shader
        Shader newShader = Shader.Find(newShaderName);
        if (newShader == null)
        {
            Debug.LogError($"[NataneToonShaderGUI] Shader not found: {newShaderName}");
            return;
        }

        // Check if already using this shader
        if (targetMaterial.shader == newShader)
            return;

        // Record undo
        Undo.RecordObject(targetMaterial, "Change Rendering Mode");

        // Switch shader (properties with same names are preserved)
        targetMaterial.shader = newShader;

        // Set default properties based on mode
        switch (mode)
        {
            case RenderingMode.Opaque:
                targetMaterial.SetFloat("_ZWrite", 1);
                targetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                break;

            case RenderingMode.Cutout:
                targetMaterial.SetFloat("_ZWrite", 1);
                targetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                break;

            case RenderingMode.Transparent:
                targetMaterial.SetFloat("_ZWrite", 0);
                targetMaterial.SetFloat("_SrcBlend", 5); // SrcAlpha
                targetMaterial.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
                targetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
        }

        // Mark as dirty
        EditorUtility.SetDirty(targetMaterial);

        // Repaint inspector
        if (materialEditor != null)
        {
            materialEditor.Repaint();
        }
    }

    private void DrawProperty(string propertyName, string label)
    {
        MaterialProperty property = FindProperty(propertyName, properties, false);
        if (property != null)
        {
            materialEditor.ShaderProperty(property, label);
        }
    }

    private bool DrawToggle(string keyword, string propertyName, string label)
    {
        MaterialProperty property = FindProperty(propertyName, properties, false);
        if (property == null)
        {
            return false;
        }

        EditorGUI.BeginChangeCheck();
        bool enabled = property.floatValue > FLOAT_COMPARISON_THRESHOLD;

        // Create a horizontal layout for toggle with visual indicator
        EditorGUILayout.BeginHorizontal();

        // Draw toggle
        enabled = EditorGUILayout.Toggle(label, enabled);

        // Visual indicator
        string statusIcon = enabled ? "✓" : "✗";
        Color statusColor = enabled ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.6f, 0.6f, 0.6f);

        var oldColor = GUI.color;
        GUI.color = statusColor;
        GUILayout.Label(statusIcon, GUILayout.Width(20));
        GUI.color = oldColor;

        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            property.floatValue = enabled ? 1.0f : 0.0f;

            // Set shader keyword
            if (enabled)
                targetMaterial.EnableKeyword(keyword);
            else
                targetMaterial.DisableKeyword(keyword);
        }

        return enabled;
    }

    /// <summary>
    /// Draw presets and sharing section
    /// </summary>
    private void DrawPresetsSection()
    {
        EditorGUI.BeginChangeCheck();
        showPresets = NataneToonShaderGUIUtility.DrawFoldoutHeader("マテリアルプリセット＆共有", showPresets);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showPresets)
        {
            NataneToonShaderGUIUtility.DrawMaterialActionsToolbar(targetMaterial, materialEditor);
        }
    }

    /// <summary>
    /// Draw performance indicator section
    /// </summary>
    private void DrawPerformanceSection()
    {
        EditorGUI.BeginChangeCheck();
        showPerformance = NataneToonShaderGUIUtility.DrawFoldoutHeader("パフォーマンス", showPerformance);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showPerformance)
        {
            NataneToonShaderGUIUtility.DrawPerformanceIndicator(targetMaterial);

            DrawHelpToggle("PerformanceHint",
                "ヒント：使用していない機能を無効化するとパフォーマンスが向上します。\n" +
                "チェックボックスのある機能はオン/オフの切り替えが可能です。",
                MessageType.Info);
        }
    }

    /// <summary>
    /// Load UI state (selected tab) from EditorPrefs
    /// </summary>
    private void LoadUIState()
    {
        selectedTab = EditorPrefs.GetInt(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "SelectedTab"), 0);
    }

    /// <summary>
    /// Save UI state (selected tab) to EditorPrefs
    /// </summary>
    private void SaveUIState()
    {
        EditorPrefs.SetInt(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "SelectedTab"), selectedTab);
    }

    /// <summary>
    /// Load foldout states from EditorPrefs for this specific material
    /// </summary>
    private void LoadFoldoutStates()
    {
        showPresets = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPresets"), true);
        showPerformance = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPerformance"), true);
        showMainTexture = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowBasic"), true);
        showMakeupTextures = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowMakeupTextures"), false);
        showShading = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowShading"), true);
        showAdvancedLighting = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAdvancedLighting"), false);
        showLightVolume = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowLightVolume"), false);
        showSpecular = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowSpecular"), false);
        showRimLight = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowRimLight"), false);
        showSSS = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowSSS"), false);
        showMatCap = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowMatCap"), false);
        showOutline = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowOutline"), false);
        showEmission = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowEmission"), false);
        showVirtualExpression = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVirtualExpression"), false);
        showNormalMap = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowNormalMap"), false);
        showReflection = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowReflection"), false);
        showEnvironmentalRim = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowEnvironmentalRim"), false);
        showParallax = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowParallax"), false);
        showRefraction = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowRefraction"), false);
        showRendering = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAdvanced"), false);
    }

    /// <summary>
    /// Save foldout states to EditorPrefs for this specific material
    /// </summary>
    private void SaveFoldoutStates()
    {
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPresets"), showPresets);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPerformance"), showPerformance);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowBasic"), showMainTexture);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowMakeupTextures"), showMakeupTextures);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowShading"), showShading);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAdvancedLighting"), showAdvancedLighting);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowLightVolume"), showLightVolume);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowSpecular"), showSpecular);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowRimLight"), showRimLight);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowSSS"), showSSS);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowMatCap"), showMatCap);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowOutline"), showOutline);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowEmission"), showEmission);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVirtualExpression"), showVirtualExpression);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowNormalMap"), showNormalMap);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowReflection"), showReflection);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowEnvironmentalRim"), showEnvironmentalRim);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowParallax"), showParallax);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowRefraction"), showRefraction);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAdvanced"), showRendering);
    }

    // ===== UI HELPER METHODS =====

    /// <summary>
    /// Draw compact header with quick actions
    /// </summary>
    private void DrawCompactHeader()
    {
        EditorGUILayout.BeginHorizontal();

        // Title (compact)
        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 14;
        EditorGUILayout.LabelField("Natane Toon Shader", titleStyle, GUILayout.Width(200));

        GUILayout.FlexibleSpace();

        // Quick validation button
        if (GUILayout.Button(new GUIContent("更新", "キーワード検証"), GUILayout.Width(40), GUILayout.Height(20)))
        {
            ValidateAndFixKeywords();
        }

        // Expand/Collapse buttons
        if (GUILayout.Button("展開", GUILayout.Width(50), GUILayout.Height(20)))
        {
            ExpandAllSections(true);

            // Force repaint and exit GUI to prevent layout conflicts
            if (materialEditor != null)
            {
                materialEditor.Repaint();
            }
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("折畳", GUILayout.Width(50), GUILayout.Height(20)))
        {
            ExpandAllSections(false);

            // Force repaint and exit GUI to prevent layout conflicts
            if (materialEditor != null)
            {
                materialEditor.Repaint();
            }
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    // ===== TAB DRAWING METHODS =====

    /// <summary>
    /// Draw Basic tab content (Main Texture, Surface Finish, Makeup, Shading)
    /// </summary>
    private void DrawBasicTab()
    {
        SafeDrawSection(DrawPresetsSection, "プリセット");
        SafeDrawSection(DrawPerformanceSection, "パフォーマンス");
        EditorGUILayout.Space(5);

        SafeDrawSection(DrawMainTextureSection, "メインテクスチャ");
        DrawSurfaceFinishSection();
        EditorGUILayout.Space(5);
        SafeDrawSection(DrawMakeupTexturesSection, "メイクアップテクスチャ");
        SafeDrawSection(DrawShadingSection, "シェーディング");
    }

    /// <summary>
    /// Draw Lighting tab content
    /// </summary>
    private void DrawLightingTab()
    {
        SafeDrawSection(DrawAdvancedLightingSection, "高度なライティング");
        SafeDrawSection(DrawLightVolumeSection, "Light Volume");
    }

    /// <summary>
    /// Draw Effects tab content
    /// </summary>
    private void DrawEffectsTab()
    {
        SafeDrawSection(DrawSpecularSection, "スペキュラー");
        SafeDrawSection(DrawRimLightSection, "リムライト");
        SafeDrawSection(DrawSSSSection, "SSS");
        SafeDrawSection(DrawMatCapSection, "MatCap");
        EditorGUILayout.Space(5);
        SafeDrawSection(DrawOutlineSection, "アウトライン");
        SafeDrawSection(DrawEmissionSection, "エミッション");
        SafeDrawSection(DrawVirtualExpressionSection, "バーチャル表現");
    }

    /// <summary>
    /// Draw Environment tab content
    /// </summary>
    private void DrawEnvironmentTab()
    {
        SafeDrawSection(DrawReflectionSection, "リフレクション");
        SafeDrawSection(DrawEnvironmentalRimSection, "環境リム");
        SafeDrawSection(DrawRefractionSection, "屈折");
    }

    /// <summary>
    /// Draw Advanced tab content
    /// </summary>
    private void DrawAdvancedTab()
    {
        SafeDrawSection(DrawNormalMapSection, "ノーマルマップ");
        SafeDrawSection(DrawParallaxSection, "視差マッピング");
        SafeDrawSection(DrawRenderingSection, "レンダリング");
    }

    /// <summary>
    /// Draw help toggle button and help box
    /// Returns true if help is shown
    /// </summary>
    private bool DrawHelpToggle(string sectionKey, string helpText, MessageType messageType = MessageType.Info)
    {
        // Get help state for this section
        string prefsKey = NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "Help_" + sectionKey);
        bool showHelp = EditorPrefs.GetBool(prefsKey, false);

        EditorGUILayout.BeginHorizontal();

        // Help toggle button (small, on the left)
        GUIContent helpContent = new GUIContent(showHelp ? "▼ ヘルプ" : "▶ ヘルプ", "クリックでヘルプを表示/非表示");
        if (GUILayout.Button(helpContent, EditorStyles.miniButton, GUILayout.Width(70)))
        {
            showHelp = !showHelp;
            EditorPrefs.SetBool(prefsKey, showHelp);
        }

        EditorGUILayout.EndHorizontal();

        // Show help box if enabled
        if (showHelp && !string.IsNullOrEmpty(helpText))
        {
            EditorGUILayout.HelpBox(helpText, messageType);
            EditorGUILayout.Space(3);
        }

        return showHelp;
    }

    /// <summary>
    /// Draw category header with description
    /// </summary>
    private void DrawCategoryHeader(string title, string description)
    {
        EditorGUILayout.Space(5);

        // Background box
        var boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(10, 10, 5, 5);

        EditorGUILayout.BeginVertical(boxStyle);

        // Title with icon
        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 13;
        EditorGUILayout.LabelField("▣ " + title, titleStyle);

        // Description
        if (!string.IsNullOrEmpty(description))
        {
            var descStyle = new GUIStyle(EditorStyles.miniLabel);
            descStyle.wordWrap = true;
            EditorGUILayout.LabelField(description, descStyle);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(3);
    }

    /// <summary>
    /// Expand or collapse all sections
    /// </summary>
    private void ExpandAllSections(bool expand)
    {
        showPresets = expand;
        showPerformance = expand;
        showMainTexture = expand;
        showMakeupTextures = expand;
        showShading = expand;
        showAdvancedLighting = expand;
        showLightVolume = expand;
        showSpecular = expand;
        showRimLight = expand;
        showSSS = expand;
        showMatCap = expand;
        showOutline = expand;
        showEmission = expand;
        showVirtualExpression = expand;
        showNormalMap = expand;
        showReflection = expand;
        showEnvironmentalRim = expand;
        showParallax = expand;
        showRefraction = expand;
        showRendering = expand;

        SaveFoldoutStates();

        // Force repaint
        if (materialEditor != null)
        {
            materialEditor.Repaint();
        }
    }

    /// <summary>
    /// Called when a new shader is assigned to a material
    /// </summary>
    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        // Validate keywords after shader assignment
        targetMaterial = material;
        ValidateAndFixKeywords();
    }

    /// <summary>
    /// Validate and fix shader keywords based on property values
    /// This ensures keywords are in sync with material properties even if not toggled manually
    /// </summary>
    private void ValidateAndFixKeywords()
    {
        if (targetMaterial == null) return;

        // Define all toggle properties and their corresponding keywords
        var keywordMappings = new (string propertyName, string keyword)[]
        {
            // Makeup Textures
            ("_Use2ndTexture", "_2ND_TEXTURE"),
            ("_Use2ndTexMask", "_2ND_TEX_MASK"),
            ("_Use3rdTexture", "_3RD_TEXTURE"),
            ("_Use3rdTexMask", "_3RD_TEX_MASK"),
            ("_Use4thTexture", "_4TH_TEXTURE"),
            ("_Use4thTexMask", "_4TH_TEX_MASK"),
            ("_Use5thTexture", "_5TH_TEXTURE"),
            ("_Use5thTexMask", "_5TH_TEX_MASK"),

            // Shading
            ("_UseRamp", "_USE_RAMP"),
            ("_UseMultiShadow", "_USE_MULTI_SHADOW"),
            ("_UseShadowReceiveMask", "_SHADOW_RECEIVE_MASK"),
            ("_UseAO", "_USE_AO"),
            ("_UseDithering", "_USE_DITHERING"),

            // Lighting
            ("_SoftLightingMode", "_SOFT_LIGHTING_MODE"),
            ("_UseLightVolume", "_USE_LIGHT_VOLUME"),
            ("_UseLightVolumeSpecular", "_LIGHT_VOLUME_SPECULAR"),

            // Effects
            ("_UseSpecular", "_SPECULAR"),
            ("_UseSpecularMask", "_SPECULAR_MASK"),
            ("_UseSSS", "_SSS"),
            ("_UseSSSMask", "_SSS_MASK"),
            ("_UseThicknessMap", "_THICKNESS_MAP"),
            ("_UseRimLight", "_RIM_LIGHT"),
            ("_UseRimMask", "_RIM_MASK"),
            ("_UseRimLight2", "_RIM_LIGHT_2"),
            ("_UseRimMask2", "_RIM_MASK_2"),
            ("_UseMatCap", "_MATCAP"),
            ("_UseMatCapMask", "_MATCAP_MASK"),
            ("_UseReflection", "_REFLECTION"),
            ("_UseReflectionMask", "_REFLECTION_MASK"),
            ("_UseEnvRim", "_ENV_RIM"),
            ("_UseEnvRimMask", "_ENV_RIM_MASK"),
            ("_UseOutline", "_OUTLINE"),
            ("_UseOutlineMask", "_OUTLINE_MASK"),
            ("_UseEmission", "_EMISSION"),
            ("_UseEmissionMask", "_EMISSION_MASK"),
            ("_UseEmissionScroll", "_EMISSION_SCROLL"),
            ("_UseEmissionPulse", "_EMISSION_PULSE"),
            ("_UseNormalMap", "_NORMALMAP"),
            ("_UseParallax", "_PARALLAX"),
            ("_UseDissolve", "_DISSOLVE"),
            ("_UseDissolveMask", "_DISSOLVE_MASK"),
            ("_UseAlphaMask", "_ALPHA_MASK"),
            ("_UseHueShift", "_HUE_SHIFT"),
            ("_UseRefraction", "_REFRACTION"),
            ("_UseRefractionMask", "_REFRACTION_MASK")
        };

        bool anyChanges = false;

        foreach (var mapping in keywordMappings)
        {
            // Check if property exists
            if (!targetMaterial.HasProperty(mapping.propertyName))
                continue;

            // Get current property value (0 or 1)
            float propertyValue = targetMaterial.GetFloat(mapping.propertyName);
            bool shouldBeEnabled = propertyValue >= 0.5f;

            // Get current keyword state
            bool isEnabled = targetMaterial.IsKeywordEnabled(mapping.keyword);

            // Fix if mismatch
            if (shouldBeEnabled != isEnabled)
            {
                if (shouldBeEnabled)
                    targetMaterial.EnableKeyword(mapping.keyword);
                else
                    targetMaterial.DisableKeyword(mapping.keyword);

                anyChanges = true;
            }
        }

        // Mark material as dirty if changes were made
        if (anyChanges)
        {
            EditorUtility.SetDirty(targetMaterial);
        }
    }
}
