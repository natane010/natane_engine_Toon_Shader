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

            // Header
            EditorGUILayout.LabelField("Natane Toon Shader", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Material Actions (Presets & Sharing)
            SafeDrawSection(DrawPresetsSection, "プリセット");

            // Performance Indicator
            SafeDrawSection(DrawPerformanceSection, "パフォーマンス");

            SafeDrawSection(DrawMainTextureSection, "メインテクスチャ");
            SafeDrawSection(DrawShadingSection, "シェーディング");
            SafeDrawSection(DrawAdvancedLightingSection, "高度なライティング");
            SafeDrawSection(DrawLightVolumeSection, "Light Volume");
            SafeDrawSection(DrawSpecularSection, "スペキュラ");
            SafeDrawSection(DrawRimLightSection, "リムライト");
            SafeDrawSection(DrawSSSSection, "SSS");
            SafeDrawSection(DrawMatCapSection, "MatCap");
            SafeDrawSection(DrawOutlineSection, "アウトライン");
            SafeDrawSection(DrawEmissionSection, "エミッション");
            SafeDrawSection(DrawVirtualExpressionSection, "バーチャル表現");
            SafeDrawSection(DrawNormalMapSection, "ノーマルマップ");
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

            bool useRamp = DrawToggle("_USE_RAMP", "_UseRamp", "ランプテクスチャを使用");

            if (useRamp)
            {
                DrawProperty("_RampTex", "ランプテクスチャ");
                EditorGUILayout.HelpBox("ランプテクスチャは暗い色（左）から明るい色（右）へのグラデーションにしてください。", MessageType.Info);
            }
            else
            {
                DrawProperty("_ShadowColor", "影の色");
                DrawProperty("_ShadowSteps", "影のステップ数");
                DrawProperty("_ShadowSharpness", "影のシャープネス");
            }

            DrawProperty("_ShadowOffset", "影のオフセット");

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

            DrawProperty("_ShadowReceive", "影の受け取り");
            EditorGUILayout.HelpBox("他のオブジェクトからの影がこのマテリアルに与える影響を制御します。1 = 完全な影、0 = 影なし。", MessageType.Info);

            DrawProperty("_ShadowMaxDarkness", "影の最大暗さ");
            EditorGUILayout.HelpBox("影の最小明るさです。0 = 完全に暗い、1 = 暗くならない。影が真っ黒になりすぎるのを防ぎます。", MessageType.Info);

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
                EditorGUILayout.HelpBox("MatCapテクスチャは球面反射マップである必要があります。", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawOutlineSection()
    {
        EditorGUI.BeginChangeCheck();
        showOutline = EditorGUILayout.Foldout(showOutline, "アウトライン", true, EditorStyles.foldoutHeader);
        if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

        if (showOutline)
        {
            EditorGUI.indentLevel++;

            bool enableOutline = DrawToggle("_OUTLINE", "_Outline", "アウトラインを有効化");

            if (enableOutline)
            {
                DrawProperty("_OutlineWidth", "アウトラインの幅");
                DrawProperty("_OutlineColor", "アウトラインの色");
                EditorGUILayout.HelpBox("アウトラインは反転ハル方式を使用します。ローポリモデルでは正しく動作しない場合があります。", MessageType.Info);
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
        bool enabled = EditorGUILayout.Toggle(label, property.floatValue > 0.5f);

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
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAdvanced"), showRendering);
    }
}
