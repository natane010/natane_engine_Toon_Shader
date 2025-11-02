using UnityEngine;
using UnityEditor;
using System;
using NataneToon.Editor;

/// <summary>
/// Custom shader GUI for Natane Toon Shader
/// Provides user-friendly interface with presets and sharing capabilities
/// </summary>
public class NataneToonShaderGUI : ShaderGUI
{
    private MaterialProperty[] properties;
    private MaterialEditor materialEditor;
    private Material targetMaterial;

    // Foldout states - now per-material using EditorPrefs
    private bool showPresets;
    private bool showPerformance;
    private bool showMainTexture;
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

            // Load foldout states from EditorPrefs for this specific material
            LoadFoldoutStates();

            // ===== Header with Logo Style =====
            DrawHeaderSection();

            // ===== Quick Access Toolbar =====
            DrawQuickAccessToolbar();

            EditorGUILayout.Space(5);

            // Material Actions (Presets & Sharing)
            SafeDrawSection(DrawPresetsSection, "プリセット");

            // Performance Indicator
            SafeDrawSection(DrawPerformanceSection, "パフォーマンス");

            // ===== CATEGORY: 基本設定 =====
            DrawCategoryHeader("基本設定", "メインテクスチャとシェーディングの基本設定");
            SafeDrawSection(DrawMainTextureSection, "メインテクスチャ");
            SafeDrawSection(DrawShadingSection, "シェーディング");
            EditorGUILayout.Space(10);

            // ===== CATEGORY: ライティング =====
            DrawCategoryHeader("ライティング", "光の当たり方と影の設定");
            SafeDrawSection(DrawAdvancedLightingSection, "高度なライティング");
            SafeDrawSection(DrawLightVolumeSection, "Light Volume");
            EditorGUILayout.Space(10);

            // ===== CATEGORY: 表面エフェクト =====
            DrawCategoryHeader("表面エフェクト", "材質感を表現するエフェクト");
            SafeDrawSection(DrawSpecularSection, "スペキュラー");
            SafeDrawSection(DrawRimLightSection, "リムライト");
            SafeDrawSection(DrawSSSSection, "SSS");
            SafeDrawSection(DrawMatCapSection, "MatCap");
            EditorGUILayout.Space(10);

            // ===== CATEGORY: アウトライン =====
            DrawCategoryHeader("アウトライン", "輪郭線の設定");
            SafeDrawSection(DrawOutlineSection, "アウトライン");
            EditorGUILayout.Space(10);

            // ===== CATEGORY: エミッションと表現 =====
            DrawCategoryHeader("エミッションと表現", "発光効果とアニメーション");
            SafeDrawSection(DrawEmissionSection, "エミッション");
            SafeDrawSection(DrawVirtualExpressionSection, "バーチャル表現");
            EditorGUILayout.Space(10);

            // ===== CATEGORY: テクスチャマッピング =====
            DrawCategoryHeader("テクスチャマッピング", "詳細なテクスチャ設定");
            SafeDrawSection(DrawNormalMapSection, "ノーマルマップ");
            SafeDrawSection(DrawParallaxSection, "視差マッピング");
            EditorGUILayout.Space(10);

            // ===== CATEGORY: 環境効果 =====
            DrawCategoryHeader("環境効果", "環境からの反射と屈折");
            SafeDrawSection(DrawReflectionSection, "リフレクション");
            SafeDrawSection(DrawEnvironmentalRimSection, "環境リム");
            SafeDrawSection(DrawRefractionSection, "屈折");
            EditorGUILayout.Space(10);

            // ===== CATEGORY: レンダリング設定 =====
            DrawCategoryHeader("レンダリング設定", "描画モードの設定");
            SafeDrawSection(DrawRenderingSection, "レンダリング");
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
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
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

            EditorGUILayout.HelpBox(
                "🎨 NiloToonスタイルのセルシェーディング\n" +
                "クリーンで明瞭な陰影境界を実現し、高品質なアニメ調レンダリングを提供します。",
                MessageType.None);

            EditorGUILayout.Space(5);

            bool useRamp = DrawToggle("_USE_RAMP", "_UseRamp", "ランプテクスチャを使用");

            if (useRamp)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("ランプテクスチャ設定", EditorStyles.boldLabel);
                DrawProperty("_RampTex", "ランプテクスチャ");
                EditorGUILayout.HelpBox(
                    "ランプテクスチャは暗い色（左）から明るい色（右）へのグラデーションにしてください。\n" +
                    "カスタムグラデーションで独自の影の色合いを作成できます。",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("セルシェーディング設定", EditorStyles.boldLabel);
                DrawProperty("_ShadowColor", "影の色");
                DrawProperty("_ShadowSteps", "影のステップ数");
                EditorGUILayout.HelpBox(
                    "推奨値: 2-3（アニメ調）、より多いステップでグラデーション効果",
                    MessageType.Info);

                DrawProperty("_ShadowSharpness", "影のシャープネス");
                EditorGUILayout.HelpBox(
                    "低い値: シャープな境界（アニメ調）\n" +
                    "高い値: 柔らかい境界（イラスト調）\n" +
                    "NiloToonスタイル推奨: 0.05-0.15",
                    MessageType.Info);
            }

            EditorGUILayout.Space(5);
            DrawProperty("_ShadowOffset", "影のオフセット");
            EditorGUILayout.HelpBox(
                "影の境界を調整します。正の値で影を明るく、負の値で影を暗くします。",
                MessageType.Info);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
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

            // Global Light Controls
            EditorGUILayout.LabelField("グローバルライト制御", EditorStyles.boldLabel);
            DrawProperty("_LightIntensity", "ライト強度（グローバル）");
            EditorGUILayout.HelpBox("全体的なライティングの強さを制御します。0 = ライトなし、1 = 標準、2 = 明るい", MessageType.Info);

            DrawProperty("_IndirectLightIntensity", "間接光の強度");
            EditorGUILayout.HelpBox("環境光やライトプローブからの間接照明の強さを制御します。", MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("シャドウ設定", EditorStyles.boldLabel);
            DrawProperty("_ShadowReceive", "影の受け取り");
            EditorGUILayout.HelpBox("他のオブジェクトからの影がこのマテリアルに与える影響を制御します。1 = 完全な影、0 = 影なし。", MessageType.Info);

            DrawProperty("_ShadowMaxDarkness", "影の最大暗さ");
            EditorGUILayout.HelpBox("影の最小明るさです。0 = 完全に暗い、1 = 暗くならない。影が真っ黒になりすぎるのを防ぎます。", MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ライト影響範囲", EditorStyles.boldLabel);
            DrawProperty("_LightMinInfluence", "ライトの最小影響");
            DrawProperty("_LightMaxInfluence", "ライトの最大影響");
            EditorGUILayout.HelpBox("最小/最大で明るさの範囲を制御します。最小値は暗くなりすぎを防ぎ、最大値は露出オーバーを防ぎます。", MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_BacklightIntensity", "逆光の強さ");
            if (targetMaterial.GetFloat("_BacklightIntensity") > 0)
            {
                DrawProperty("_BacklightColor", "逆光の色");
                EditorGUILayout.HelpBox("逆光はオブジェクトの背後に光がある時に照明を追加し、リムライトのような効果を作ります。", MessageType.Info);
            }

            EditorGUILayout.Space();
            DrawProperty("_AdditionalLightIntensity", "追加ライトの強さ");
            EditorGUILayout.HelpBox("追加ライト（ForwardAddパス）の強度を制御します。低い値は複数のライトを使用する際の明るくなりすぎを防ぎます。0 = 追加ライトなし、1 = 最大強度。", MessageType.Info);

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
                EditorGUILayout.HelpBox("VRC Light Volumesはボクセルベースの次世代ライティングシステムです。対応ワールドでより正確な部分的照明が可能になります。", MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_LightVolumeIntensity", "Light Volumeの強さ");
                EditorGUILayout.HelpBox("Light Volumeライティングの強度を制御します。1 = 完全強度、0 = 無効。", MessageType.Info);

                EditorGUILayout.Space();
                bool enableSpecular = DrawToggle("_LIGHT_VOLUME_SPECULAR", "_LightVolumeSpecular", "Light Volume スペキュラー");
                if (enableSpecular)
                {
                    EditorGUILayout.HelpBox("Light Volumeからカラースペキュラーを生成します。アバターに推奨されます。", MessageType.Info);
                }

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
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
                    EditorGUILayout.HelpBox("白 = スペキュラーあり、黒 = スペキュラーなし", MessageType.Info);
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
                bool useRimMask = DrawToggle("_RIM_MASK", "_UseRimMask", "リムマスクを使用");
                if (useRimMask)
                {
                    DrawProperty("_RimMask", "リムマスク");
                    EditorGUILayout.HelpBox("白 = リムライトあり、黒 = リムライトなし", MessageType.Info);
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
                    EditorGUILayout.HelpBox("白 = 薄い（SSSが強い）、黒 = 厚い（SSSが弱い）", MessageType.Info);
                }

                DrawProperty("_ThicknessScale", "厚さのスケール");

                EditorGUILayout.Space();
                bool useSSSMask = DrawToggle("_SSS_MASK", "_UseSSS_Mask", "SSSマスクを使用");
                if (useSSSMask)
                {
                    DrawProperty("_SSSMask", "SSSマスク");
                    EditorGUILayout.HelpBox("白 = SSSあり、黒 = SSSなし", MessageType.Info);
                }

                EditorGUILayout.HelpBox("SSSはオブジェクトを通過する光をシミュレートします。肌、葉、薄い素材に最適です。", MessageType.Info);
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
                    EditorGUILayout.HelpBox("白 = MatCapあり、黒 = MatCapなし", MessageType.Info);
                }

                EditorGUILayout.HelpBox("MatCapテクスチャは球面反射マップである必要があります。", MessageType.Info);
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

                float outlineMode = targetMaterial.GetFloat("_OutlineMode");
                if (outlineMode < 0.5f)
                {
                    EditorGUILayout.HelpBox(
                        "【反転ハル方式】\n" +
                        "法線方向に頂点を押し出してアウトラインを描画します。\n" +
                        "• 利点: 一般的に安定した結果、距離補正により遠近で一貫した太さ\n" +
                        "• 欠点: ローポリモデルやハードエッジで乱れる場合があります\n" +
                        "• NiloToon互換: カメラ距離による自動調整機能を搭載",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
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
                    EditorGUILayout.HelpBox("白 = エミッションあり、黒 = エミッションなし", MessageType.Info);
                }
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
                EditorGUILayout.HelpBox("0 = 完全に表示、1 = 完全に消滅", MessageType.Info);

                DrawProperty("_DissolveTex", "ディゾルブテクスチャ（ノイズ）");
                DrawProperty("_DissolveEdgeWidth", "エッジの幅");
                DrawProperty("_DissolveEdgeColor", "エッジの色");
                DrawProperty("_DissolveEdgeIntensity", "エッジの強さ");

                EditorGUILayout.Space();
                bool useDissolveMask = DrawToggle("_DISSOLVE_MASK", "_UseDissolveMask", "ディゾルブマスクを使用");
                if (useDissolveMask)
                {
                    DrawProperty("_DissolveMask", "ディゾルブマスク");
                    EditorGUILayout.HelpBox("白 = ディゾルブあり、黒 = ディゾルブなし", MessageType.Info);
                }

                EditorGUILayout.HelpBox("ディゾルブはVRChatアバターの出現アニメーションに最適な消滅・分解エフェクトを作成します。ディゾルブ量パラメータをアニメーションさせることで、オブジェクトを出現または消滅させることができます。", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Hue Shift
            bool enableHueShift = DrawToggle("_HUE_SHIFT", "_HueShiftEnable", "色相シフトを有効化");
            if (enableHueShift)
            {
                DrawProperty("_HueShift", "色相シフト");
                EditorGUILayout.HelpBox("マテリアル全体の色相を変更します。0 = 変更なし、0.5 = 補色、1 = 完全な回転。VRChatでの色変更エフェクトに最適です。", MessageType.Info);
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
                DrawProperty("_FresnelPower", "フレネルパワー");

                EditorGUILayout.Space();
                bool useReflectionMask = DrawToggle("_REFLECTION_MASK", "_UseReflectionMask", "リフレクションマスクを使用");
                if (useReflectionMask)
                {
                    DrawProperty("_ReflectionMask", "リフレクションマスク");
                    EditorGUILayout.HelpBox("白 = リフレクションあり、黒 = リフレクションなし", MessageType.Info);
                }

                EditorGUILayout.HelpBox("キューブマップを使用して環境反射をシミュレートします。金属やガラスなどの反射素材に最適です。", MessageType.Info);
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
                    EditorGUILayout.HelpBox("白 = 環境リムあり、黒 = 環境リムなし", MessageType.Info);
                }

                EditorGUILayout.HelpBox("キューブマップを使用して環境に基づいたリムライト効果を作成します。", MessageType.Info);
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

                EditorGUILayout.HelpBox("視差マッピングは高さマップを使用してサーフェスに深度の錯覚を作成します。石や壁などの素材に最適です。", MessageType.Info);
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
                    EditorGUILayout.HelpBox("白 = 屈折あり、黒 = 屈折なし", MessageType.Info);
                }

                EditorGUILayout.HelpBox("屈折はガラスや水などの透明素材で光の曲がりをシミュレートします。透明マテリアルに最適です。", MessageType.Info);
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
            DrawProperty("_Cull", "カリングモード");
            DrawProperty("_ZWrite", "Z書き込み");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
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
        bool enabled = property.floatValue > 0.5f;

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

            EditorGUILayout.HelpBox(
                "ヒント：使用していない機能を無効化するとパフォーマンスが向上します。\n" +
                "チェックボックスのある機能はオン/オフの切り替えが可能です。",
                MessageType.Info);
        }
    }

    /// <summary>
    /// Load foldout states from EditorPrefs for this specific material
    /// </summary>
    private void LoadFoldoutStates()
    {
        showPresets = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPresets"), true);
        showPerformance = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPerformance"), true);
        showMainTexture = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowBasic"), true);
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
    /// Draw stylized header with logo
    /// </summary>
    private void DrawHeaderSection()
    {
        // Create a box style for the header
        var headerStyle = new GUIStyle(EditorStyles.helpBox);
        headerStyle.padding = new RectOffset(10, 10, 10, 10);

        EditorGUILayout.BeginVertical(headerStyle);

        // Title
        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 16;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Natane Toon Shader", titleStyle);

        // Subtitle
        var subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("NiloToon-Style High-Quality Anime Rendering", subtitleStyle);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    /// <summary>
    /// Draw quick access toolbar with expand/collapse buttons
    /// </summary>
    private void DrawQuickAccessToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("すべて展開", GUILayout.Width(100)))
        {
            ExpandAllSections(true);
        }

        if (GUILayout.Button("すべて折りたたむ", GUILayout.Width(120)))
        {
            ExpandAllSections(false);
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();
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
}
