using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Eye シェーダー用のDrawer
    /// NataneToonShaderGUIから委譲される
    /// </summary>
    public class NataneToonEyeDrawer : NataneToonShaderGUITab
    {
        // Foldout states
        private bool showEyeState = true;
        private bool showMainSettings = true;
        private bool showDualCenter = true;
        private bool showRegionMask = false;
        private bool showStateNormal = true;
        private bool showStateStar = false;
        private bool showStateHeart = false;
        private bool showStateDead = false;
        private bool showStateNervous = false;
        private bool showHueColor = true;
        private bool showBubble = false;
        private bool showIrisCaustics = false;
        private bool showIrisRingPulse = false;
        private bool showTexturePolish = false;
        private bool showExpression = false;
        private bool showInnerMeshPriority = false;
        private bool showPerformance = false;
        private bool showVignette = false;
        private bool showAudioLink = false;
        private bool showRendering = false;

        private string prefsPrefix;

        public override void Initialize(MaterialEditor materialEditor, MaterialProperty[] properties, Material targetMaterial)
        {
            base.Initialize(materialEditor, properties, targetMaterial);
            prefsPrefix = NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "Eye_");
            LoadFoldoutStates();
        }

        public override void LoadFoldoutStates()
        {
            if (targetMaterial == null) return;
            showEyeState = EditorPrefs.GetBool(prefsPrefix + "EyeState", true);
            showMainSettings = EditorPrefs.GetBool(prefsPrefix + "Main", true);
            showDualCenter = EditorPrefs.GetBool(prefsPrefix + "DualCenter", true);
            showRegionMask = EditorPrefs.GetBool(prefsPrefix + "RegionMask", false);
            showStateNormal = EditorPrefs.GetBool(prefsPrefix + "StateNormal", true);
            showStateStar = EditorPrefs.GetBool(prefsPrefix + "StateStar", false);
            showStateHeart = EditorPrefs.GetBool(prefsPrefix + "StateHeart", false);
            showStateDead = EditorPrefs.GetBool(prefsPrefix + "StateDead", false);
            showStateNervous = EditorPrefs.GetBool(prefsPrefix + "StateNervous", false);
            showHueColor = EditorPrefs.GetBool(prefsPrefix + "HueColor", true);
            showBubble = EditorPrefs.GetBool(prefsPrefix + "Bubble", false);
            showIrisCaustics = EditorPrefs.GetBool(prefsPrefix + "IrisCaustics", false);
            showIrisRingPulse = EditorPrefs.GetBool(prefsPrefix + "IrisRingPulse", false);
            showTexturePolish = EditorPrefs.GetBool(prefsPrefix + "TexturePolish", false);
            showExpression = EditorPrefs.GetBool(prefsPrefix + "Expression", false);
            showInnerMeshPriority = EditorPrefs.GetBool(prefsPrefix + "InnerMesh", false);
            showPerformance = EditorPrefs.GetBool(prefsPrefix + "Performance", false);
            showVignette = EditorPrefs.GetBool(prefsPrefix + "Vignette", false);
            showAudioLink = EditorPrefs.GetBool(prefsPrefix + "AudioLink", false);
            showRendering = EditorPrefs.GetBool(prefsPrefix + "Rendering", false);
        }

        public override void SaveFoldoutStates()
        {
            if (targetMaterial == null) return;
            EditorPrefs.SetBool(prefsPrefix + "EyeState", showEyeState);
            EditorPrefs.SetBool(prefsPrefix + "Main", showMainSettings);
            EditorPrefs.SetBool(prefsPrefix + "DualCenter", showDualCenter);
            EditorPrefs.SetBool(prefsPrefix + "RegionMask", showRegionMask);
            EditorPrefs.SetBool(prefsPrefix + "StateNormal", showStateNormal);
            EditorPrefs.SetBool(prefsPrefix + "StateStar", showStateStar);
            EditorPrefs.SetBool(prefsPrefix + "StateHeart", showStateHeart);
            EditorPrefs.SetBool(prefsPrefix + "StateDead", showStateDead);
            EditorPrefs.SetBool(prefsPrefix + "StateNervous", showStateNervous);
            EditorPrefs.SetBool(prefsPrefix + "HueColor", showHueColor);
            EditorPrefs.SetBool(prefsPrefix + "Bubble", showBubble);
            EditorPrefs.SetBool(prefsPrefix + "IrisCaustics", showIrisCaustics);
            EditorPrefs.SetBool(prefsPrefix + "IrisRingPulse", showIrisRingPulse);
            EditorPrefs.SetBool(prefsPrefix + "TexturePolish", showTexturePolish);
            EditorPrefs.SetBool(prefsPrefix + "Expression", showExpression);
            EditorPrefs.SetBool(prefsPrefix + "InnerMesh", showInnerMeshPriority);
            EditorPrefs.SetBool(prefsPrefix + "Performance", showPerformance);
            EditorPrefs.SetBool(prefsPrefix + "Vignette", showVignette);
            EditorPrefs.SetBool(prefsPrefix + "AudioLink", showAudioLink);
            EditorPrefs.SetBool(prefsPrefix + "Rendering", showRendering);
        }

        public override void Draw()
        {
            DrawHeader();
            EditorGUILayout.Space(5);

            // ヘルプボックス
            EditorGUILayout.HelpBox(
                "右上/左上など部分UV: Eye Select Mode を 'Nearest Center' に設定。\n" +
                "左右対称UV: Eye Center Mode を 'Symmetry From Center1' にし、Symmetry Pivot X を調整。",
                MessageType.Info);
            EditorGUILayout.Space(5);

            SafeDrawSection(() => DrawEyeStateSection(), "Eye State");
            SafeDrawSection(() => DrawMainSettingsSection(), "Main Settings");
            SafeDrawSection(() => DrawDualCenterSection(), "Dual Center");
            SafeDrawSection(() => DrawRegionMaskSection(), "Region Mask");

            // 状態別設定
            SafeDrawSection(() => DrawStateNormalSection(), "State Normal");
            SafeDrawSection(() => DrawStateStarSection(), "State Star");
            SafeDrawSection(() => DrawStateHeartSection(), "State Heart");
            SafeDrawSection(() => DrawStateDeadSection(), "State Dead");
            SafeDrawSection(() => DrawStateNervousSection(), "State Nervous");

            SafeDrawSection(() => DrawHueColorSection(), "Hue Color");
            SafeDrawSection(() => DrawBubbleSection(), "Bubble");
            SafeDrawSection(() => DrawIrisCausticsSection(), "Iris Caustics");
            SafeDrawSection(() => DrawIrisRingPulseSection(), "Iris Ring Pulse");
            SafeDrawSection(() => DrawTexturePolishSection(), "Texture Polish");
            SafeDrawSection(() => DrawExpressionSection(), "Expression");
            SafeDrawSection(() => DrawInnerMeshPrioritySection(), "Inner Mesh Priority");
            SafeDrawSection(() => DrawPerformanceSection(), "Performance");
            SafeDrawSection(() => DrawVignetteSection(), "Vignette");
            SafeDrawSection(() => DrawAudioLinkSection(), "Audio Link");
            SafeDrawSection(() => DrawRenderingSection(), "Rendering");

            EditorGUILayout.Space(10);
            DrawFooter();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.6f, 1f) }
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ナタネトゥーン 目シェーダー", headerStyle);
            EditorGUILayout.EndVertical();
        }

        // ===== Foldout Helper =====
        private bool DrawFoldout(ref bool state, string label)
        {
            EditorGUI.BeginChangeCheck();
            state = EditorGUILayout.Foldout(state, label, true, EditorStyles.foldoutHeader);
            if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();
            return state;
        }

        // ===== Section Implementations =====

        private void DrawEyeStateSection()
        {
            if (!DrawFoldout(ref showEyeState, "瞳の状態 (Eye State)")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_EyeState", "目の状態 (Normal/Star/Heart/Dead/Nervous)");
            EditorGUILayout.HelpBox(
                "Normal=通常 / Star=星目 / Heart=ハート / Dead=死んだ目 / Nervous=緊張",
                MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawMainSettingsSection()
        {
            if (!DrawFoldout(ref showMainSettings, "メイン設定")) return;
            EditorGUI.indentLevel++;
            MaterialProperty mainTexProp = FindProperty("_MainTex", properties, false);
            if (mainTexProp != null) materialEditor.TextureProperty(mainTexProp, "ベーステクスチャ");
            DrawToggle("_USE_TEXTURE", "_UseTexture", "テクスチャを使用");
            DrawProperty("_TextureBlend", "テクスチャブレンド");
            DrawProperty("_BlendMode", "ブレンドモード");
            DrawProperty("_MainParallax", "パララックス強度");
            DrawProperty("_MainColor", "グレアカラー (HDR)");
            DrawProperty("_BackgroundColor", "背景色 (HDR)");
            DrawProperty("_PupilSize", "瞳孔サイズ");
            DrawProperty("_PupilAspect", "瞳孔アスペクト XY (1,1=円)");
            EditorGUI.indentLevel--;
        }

        private void DrawDualCenterSection()
        {
            if (!DrawFoldout(ref showDualCenter, "デュアルアイセンター")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_EyeCenter1", "アイセンター 1 (UV)");
            DrawProperty("_EyeCenter2", "アイセンター 2 (UV)");
            DrawProperty("_EyeSeparationX", "左右分離Xしきい値");
            DrawProperty("_EyeSelectMode", "目の選択モード");
            DrawProperty("_EyeCenterMode", "アイセンターモード");
            DrawProperty("_EyeSymmetryPivotX", "対称ピボット X");
            DrawProperty("_MirrorRightEyeUV", "右目UVミラー");
            EditorGUILayout.HelpBox(
                "Nearest Center: UV座標に最も近い中心を選択。\n" +
                "Symmetry From Center1: Center1を基準に左右対称化。",
                MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawRegionMaskSection()
        {
            if (!DrawFoldout(ref showRegionMask, "瞳リージョンマスク")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseEyeRegionMask", "リージョンマスクを使用");
            MaterialProperty maskProp = FindProperty("_EyeRegionMask", properties, false);
            if (maskProp != null) materialEditor.TextureProperty(maskProp, "マスクテクスチャ");
            DrawProperty("_EyeMaskChannel", "マスクチャンネル");
            DrawProperty("_EyeMaskThreshold", "マスクしきい値");
            DrawProperty("_EyeMaskSoftness", "マスク柔らかさ");
            DrawProperty("_EyeMaskInvert", "マスク反転");
            EditorGUILayout.HelpBox("顔と目が同一マテリアルの場合に使用。目の領域をマスクで指定します。", MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawStateNormalSection()
        {
            if (!DrawFoldout(ref showStateNormal, "通常状態 (Normal)")) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_NormalStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, "通常状態テクスチャ");
            DrawProperty("_NormalPupilColor", "瞳孔カラー (HDR)");
            DrawProperty("_DetailBrightness", "ディテール明るさ");
            EditorGUI.indentLevel--;
        }

        private void DrawStateStarSection()
        {
            if (!DrawFoldout(ref showStateStar, "星目状態 (Star)")) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_StarStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, "星目テクスチャ");
            DrawProperty("_StarColor", "星カラー (HDR)");
            DrawProperty("_StarRockSharpness", "揺れ鋭さ");
            DrawProperty("_StarRockAngle", "揺れ角度");
            DrawProperty("_StarRockSpeed", "揺れ速度");
            EditorGUI.indentLevel--;
        }

        private void DrawStateHeartSection()
        {
            if (!DrawFoldout(ref showStateHeart, "ハート状態 (Heart)")) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_HeartStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, "ハートテクスチャ");
            DrawProperty("_HeartPupilColor", "ハートカラー (HDR)");
            DrawProperty("_HeartPulseSpeed", "パルス速度");
            DrawProperty("_HeartPulsePower", "パルス強度");
            EditorGUI.indentLevel--;
        }

        private void DrawStateDeadSection()
        {
            if (!DrawFoldout(ref showStateDead, "死んだ目状態 (Dead)")) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_DeadStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, "死んだ目テクスチャ");
            DrawProperty("_DeadGradientOffset", "グラデーションオフセット");
            DrawProperty("_DeadTopColor", "上部カラー");
            DrawProperty("_DeadBottomColor", "下部カラー");
            EditorGUI.indentLevel--;
        }

        private void DrawStateNervousSection()
        {
            if (!DrawFoldout(ref showStateNervous, "緊張状態 (Nervous)")) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_NervousStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, "緊張テクスチャ");
            DrawProperty("_NervousBackgroundColor", "背景色 (HDR)");
            DrawProperty("_NervousLinesColor", "線カラー (HDR)");
            DrawProperty("_NervousLinesRandSeed", "ランダムシード");
            DrawProperty("_NervousLinesRandOffs", "線のオフセット");
            DrawProperty("_NervousLinesSize", "線のサイズ");
            DrawProperty("_NervousLinesThickness", "線の太さ");
            DrawProperty("_NervousCenterFill", "中心塗りつぶし");
            EditorGUI.indentLevel--;
        }

        private void DrawHueColorSection()
        {
            if (!DrawFoldout(ref showHueColor, "色調調整 (Hue & Color)")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_Contrast", "コントラスト");
            DrawProperty("_MainSaturation", "彩度");
            DrawProperty("_MainHueShift", "色相オフセット");
            DrawProperty("_MainHueSpeed", "色相アニメ速度");
            EditorGUI.indentLevel--;
        }

        private void DrawBubbleSection()
        {
            if (!DrawFoldout(ref showBubble, "バブルエフェクト")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_BubbleSize", "バブルサイズ");
            DrawProperty("_BubbleBrightness", "バブル明るさ");
            DrawProperty("_BubbleWobbleSpeed", "揺れ速度");
            DrawProperty("_BubbleWobbleStrength", "揺れ強度");
            EditorGUILayout.HelpBox("瞳のハイライト粒子エフェクト。キラキラした輝きを追加します。", MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawIrisCausticsSection()
        {
            if (!DrawFoldout(ref showIrisCaustics, "虹彩コースティクス")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseIrisCaustics", "コースティクスを有効化");
            DrawProperty("_IrisCausticsColor", "コースティクスカラー (HDR)");
            DrawProperty("_IrisCausticsIntensity", "強度");
            DrawProperty("_IrisCausticsScale", "スケール");
            DrawProperty("_IrisCausticsSpeed", "速度");
            DrawProperty("_IrisCausticsParallax", "パララックス");
            DrawProperty("_IrisCausticsTwist", "ツイスト");
            DrawProperty("_IrisCausticsBlendMode", "ブレンドモード");
            EditorGUI.indentLevel--;
        }

        private void DrawIrisRingPulseSection()
        {
            if (!DrawFoldout(ref showIrisRingPulse, "虹彩リングパルス")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseIrisRingPulse", "リングパルスを有効化");
            DrawProperty("_IrisRingColor", "リングカラー (HDR)");
            DrawProperty("_IrisRingRadius", "リング半径");
            DrawProperty("_IrisRingWidth", "リング幅");
            DrawProperty("_IrisRingPulseSpeed", "パルス速度");
            DrawProperty("_IrisRingPulseAmount", "パルス量");
            DrawProperty("_IrisRingParallax", "パララックス");
            DrawProperty("_IrisRingIntensity", "強度");
            DrawProperty("_IrisRingBlendMode", "ブレンドモード");
            EditorGUI.indentLevel--;
        }

        private void DrawTexturePolishSection()
        {
            if (!DrawFoldout(ref showTexturePolish, "テクスチャポリッシュ")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseTexturePolish", "テクスチャポリッシュを有効化");
            DrawProperty("_TexturePolishBlendMode", "ブレンドモード");
            DrawProperty("_TexturePolishStrength", "強度");
            DrawProperty("_TexturePolishContrast", "コントラスト");
            DrawProperty("_TexturePolishSaturation", "彩度");
            DrawProperty("_TexturePolishIrisFocus", "虹彩フォーカス");
            DrawProperty("_TexturePolishTint", "ティントカラー (HDR)");
            EditorGUI.indentLevel--;
        }

        private void DrawExpressionSection()
        {
            if (!DrawFoldout(ref showExpression, "表情オーバーレイ")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_ExpressionPreset", "表情プリセット");
            DrawProperty("_ExpressionMode", "表情モード (Off/Spiral/Tearful/Shock)");
            DrawProperty("_ExpressionColor", "表情カラー (HDR)");
            DrawProperty("_ExpressionIntensity", "強度");
            DrawProperty("_ExpressionScale", "スケール");
            DrawProperty("_ExpressionSpeed", "速度");
            DrawProperty("_ExpressionParallax", "パララックス");
            DrawProperty("_ExpressionDetail", "ディテール");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("スパイラル設定", EditorStyles.miniBoldLabel);
            DrawProperty("_SpiralTightness", "スパイラル巻き");
            DrawProperty("_SpiralLineWidth", "スパイラル線幅");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("涙設定", EditorStyles.miniBoldLabel);
            DrawProperty("_TearFlow", "涙の流れ");
            DrawProperty("_TearRim", "涙のリムハイライト");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("ショックリング設定", EditorStyles.miniBoldLabel);
            DrawProperty("_ShockRingCount", "リング数");
            DrawProperty("_ShockRingWidth", "リング幅");
            DrawProperty("_ExpressionBlendMode", "ブレンドモード");
            EditorGUI.indentLevel--;
        }

        private void DrawInnerMeshPrioritySection()
        {
            if (!DrawFoldout(ref showInnerMeshPriority, "内部メッシュ優先度 (BlendShape)")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseInnerMeshPriority", "内部メッシュ優先を有効化");
            MaterialProperty maskProp = FindProperty("_InnerMeshPriorityMask", properties, false);
            if (maskProp != null) materialEditor.TextureProperty(maskProp, "優先度マスク");
            DrawProperty("_InnerMeshPriorityChannel", "マスクチャンネル");
            DrawProperty("_InnerMeshPriorityThreshold", "しきい値");
            DrawProperty("_InnerMeshPrioritySoftness", "柔らかさ");
            DrawProperty("_InnerMeshPriorityInvert", "反転");
            DrawProperty("_InnerMeshPriorityMode", "優先度モード");
            DrawProperty("_InnerMeshPriorityStrength", "強度");
            EditorGUILayout.HelpBox("BlendShape使用時の内部メッシュ描画優先度を制御します。", MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawPerformanceSection()
        {
            if (!DrawFoldout(ref showPerformance, "パフォーマンス")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_PerformanceTier", "パフォーマンスティア (Quality/Balanced/Lite)");
            EditorGUILayout.HelpBox("Lite: 軽量化のため一部エフェクトを無効化。Quest対応時はLite推奨。", MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawVignetteSection()
        {
            if (!DrawFoldout(ref showVignette, "ビネットエフェクト")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_VignetteTransparency", "透過を有効化");
            DrawProperty("_VignetteDitherScale", "ディザースケール");
            DrawProperty("_VignetteThickness", "太さ");
            DrawProperty("_VignetteFalloff", "フォールオフ");
            EditorGUI.indentLevel--;
        }

        private void DrawAudioLinkSection()
        {
            if (!DrawFoldout(ref showAudioLink, "Audio Link")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_BandSelection", "周波数帯域");
            DrawProperty("_Intensity", "強度");
            DrawProperty("_MinValue", "最小明るさ");
            EditorGUILayout.HelpBox("VRChatワールドにAudioLinkが設置されている場合に音楽連動します。", MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawRenderingSection()
        {
            if (!DrawFoldout(ref showRendering, "レンダリング設定")) return;
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("ステンシル", EditorStyles.miniBoldLabel);
            DrawProperty("_StencilRef", "参照値");
            DrawProperty("_StencilComp", "比較関数");
            DrawProperty("_StencilPass", "パス操作");
            DrawProperty("_StencilFail", "失敗操作");
            DrawProperty("_StencilZFail", "Z失敗操作");
            DrawProperty("_StencilReadMask", "リードマスク");
            DrawProperty("_StencilWriteMask", "ライトマスク");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("ブレンド & 透明", EditorStyles.miniBoldLabel);
            DrawProperty("_SrcBlend", "ソースブレンド");
            DrawProperty("_DstBlend", "宛先ブレンド");
            DrawProperty("_BlendOp", "ブレンド操作");
            DrawProperty("_TransparencyResolveMode", "透過解決モード");
            DrawProperty("_TransparencyDitherScale", "透過ディザースケール");
            DrawProperty("_GlobalOpacity", "全体透過率");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("深度 & カリング", EditorStyles.miniBoldLabel);
            DrawProperty("_ZTest", "Zテスト");
            DrawProperty("_ZWrite", "Z書き込み");
            DrawProperty("_OffsetFactor", "オフセットファクター");
            DrawProperty("_OffsetUnits", "オフセットユニット");
            DrawProperty("_AlphaClipThreshold", "アルファクリップしきい値");
            DrawProperty("_Cull", "カリングモード");
            DrawProperty("_ColorMask", "カラーマスク");
            EditorGUI.indentLevel--;
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle footerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            EditorGUILayout.LabelField("ナタネ 目シェーダー v1.1", footerStyle);
            EditorGUILayout.LabelField("5つの瞳状態 + 表情オーバーレイ対応", footerStyle);
            EditorGUILayout.EndVertical();
        }
    }
}
