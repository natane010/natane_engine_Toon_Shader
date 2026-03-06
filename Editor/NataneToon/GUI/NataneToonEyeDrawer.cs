using UnityEngine;
using UnityEditor;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    /// <summary>
    /// Eye シェーダー用のDrawer
    /// NataneToonShaderGUIから委譲される
    /// </summary>
    public class NataneToonEyeDrawer : NataneToonShaderGUITab
    {
        private static GUIStyle _headerStyle;
        private static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 16,
                        alignment = TextAnchor.MiddleCenter
                    };
                    _headerStyle.normal.textColor = new Color(0.8f, 0.6f, 1f);
                }

                return _headerStyle;
            }
        }

        private static GUIStyle _footerStyle;
        private static GUIStyle FooterStyle
        {
            get
            {
                if (_footerStyle == null)
                {
                    _footerStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };
                }

                return _footerStyle;
            }
        }

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
                L("右上/左上など部分UV: Eye Select Mode を 'Nearest Center' に設定。\n" +
                  "左右対称UV: Eye Center Mode を 'Symmetry From Center1' にし、Symmetry Pivot X を調整。",
                  "Partial UV (top-right/top-left etc.): Set Eye Select Mode to 'Nearest Center'.\n" +
                  "Symmetrical UV: Set Eye Center Mode to 'Symmetry From Center1' and adjust Symmetry Pivot X."),
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ナタネトゥーン 目シェーダー", "Natane Toon Eye Shader"), HeaderStyle);
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
            if (!DrawFoldout(ref showEyeState, L("瞳の状態 (Eye State)", "Eye State"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_EyeState", L("目の状態 (Normal/Star/Heart/Dead/Nervous)", "Eye State (Normal/Star/Heart/Dead/Nervous)"));
            EditorGUILayout.HelpBox(
                L("Normal=通常 / Star=星目 / Heart=ハート / Dead=死んだ目 / Nervous=緊張",
                  "Normal / Star / Heart / Dead / Nervous"),
                MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawMainSettingsSection()
        {
            if (!DrawFoldout(ref showMainSettings, L("メイン設定", "Main Settings"))) return;
            EditorGUI.indentLevel++;
            MaterialProperty mainTexProp = FindProperty("_MainTex", properties, false);
            if (mainTexProp != null) materialEditor.TextureProperty(mainTexProp, L("ベーステクスチャ", "Base Texture"));
            DrawToggle("_USE_TEXTURE", "_UseTexture", L("テクスチャを使用", "Use Texture"));
            DrawProperty("_TextureBlend", L("テクスチャブレンド", "Texture Blend"));
            DrawProperty("_BlendMode", L("ブレンドモード", "Blend Mode"));
            DrawProperty("_MainParallax", L("パララックス強度", "Parallax Intensity"));
            DrawProperty("_MainColor", L("グレアカラー (HDR)", "Glare Color (HDR)"));
            DrawProperty("_BackgroundColor", L("背景色 (HDR)", "Background Color (HDR)"));
            DrawProperty("_PupilSize", L("瞳孔サイズ", "Pupil Size"));
            DrawProperty("_PupilAspect", L("瞳孔アスペクト XY (1,1=円)", "Pupil Aspect XY (1,1=Circle)"));
            EditorGUI.indentLevel--;
        }

        private void DrawDualCenterSection()
        {
            if (!DrawFoldout(ref showDualCenter, L("デュアルアイセンター", "Dual Eye Center"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_EyeCenter1", L("アイセンター 1 (UV)", "Eye Center 1 (UV)"));
            DrawProperty("_EyeCenter2", L("アイセンター 2 (UV)", "Eye Center 2 (UV)"));
            DrawProperty("_EyeSeparationX", L("左右分離Xしきい値", "Left/Right Separation X Threshold"));
            DrawProperty("_EyeSelectMode", L("目の選択モード", "Eye Select Mode"));
            DrawProperty("_EyeCenterMode", L("アイセンターモード", "Eye Center Mode"));
            DrawProperty("_EyeSymmetryPivotX", L("対称ピボット X", "Symmetry Pivot X"));
            DrawProperty("_MirrorRightEyeUV", L("右目UVミラー", "Mirror Right Eye UV"));
            EditorGUILayout.HelpBox(
                L("Nearest Center: UV座標に最も近い中心を選択。\n" +
                  "Symmetry From Center1: Center1を基準に左右対称化。",
                  "Nearest Center: Selects the center closest to the UV coordinate.\n" +
                  "Symmetry From Center1: Symmetrizes based on Center1."),
                MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawRegionMaskSection()
        {
            if (!DrawFoldout(ref showRegionMask, L("瞳リージョンマスク", "Eye Region Mask"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseEyeRegionMask", L("リージョンマスクを使用", "Use Region Mask"));
            MaterialProperty maskProp = FindProperty("_EyeRegionMask", properties, false);
            if (maskProp != null) materialEditor.TextureProperty(maskProp, L("マスクテクスチャ", "Mask Texture"));
            DrawProperty("_EyeMaskChannel", L("マスクチャンネル", "Mask Channel"));
            DrawProperty("_EyeMaskThreshold", L("マスクしきい値", "Mask Threshold"));
            DrawProperty("_EyeMaskSoftness", L("マスク柔らかさ", "Mask Softness"));
            DrawProperty("_EyeMaskInvert", L("マスク反転", "Invert Mask"));
            EditorGUILayout.HelpBox(L("顔と目が同一マテリアルの場合に使用。目の領域をマスクで指定します。", "Use when the face and eyes share the same material. Specify the eye region with a mask."), MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawStateNormalSection()
        {
            if (!DrawFoldout(ref showStateNormal, L("通常状態 (Normal)", "Normal State"))) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_NormalStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, L("通常状態テクスチャ", "Normal State Texture"));
            DrawProperty("_NormalPupilColor", L("瞳孔カラー (HDR)", "Pupil Color (HDR)"));
            DrawProperty("_DetailBrightness", L("ディテール明るさ", "Detail Brightness"));
            EditorGUI.indentLevel--;
        }

        private void DrawStateStarSection()
        {
            if (!DrawFoldout(ref showStateStar, L("星目状態 (Star)", "Star State"))) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_StarStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, L("星目テクスチャ", "Star Texture"));
            DrawProperty("_StarColor", L("星カラー (HDR)", "Star Color (HDR)"));
            DrawProperty("_StarRockSharpness", L("揺れ鋭さ", "Rock Sharpness"));
            DrawProperty("_StarRockAngle", L("揺れ角度", "Rock Angle"));
            DrawProperty("_StarRockSpeed", L("揺れ速度", "Rock Speed"));
            EditorGUI.indentLevel--;
        }

        private void DrawStateHeartSection()
        {
            if (!DrawFoldout(ref showStateHeart, L("ハート状態 (Heart)", "Heart State"))) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_HeartStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, L("ハートテクスチャ", "Heart Texture"));
            DrawProperty("_HeartPupilColor", L("ハートカラー (HDR)", "Heart Color (HDR)"));
            DrawProperty("_HeartPulseSpeed", L("パルス速度", "Pulse Speed"));
            DrawProperty("_HeartPulsePower", L("パルス強度", "Pulse Power"));
            EditorGUI.indentLevel--;
        }

        private void DrawStateDeadSection()
        {
            if (!DrawFoldout(ref showStateDead, L("死んだ目状態 (Dead)", "Dead State"))) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_DeadStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, L("死んだ目テクスチャ", "Dead Eye Texture"));
            DrawProperty("_DeadGradientOffset", L("グラデーションオフセット", "Gradient Offset"));
            DrawProperty("_DeadTopColor", L("上部カラー", "Top Color"));
            DrawProperty("_DeadBottomColor", L("下部カラー", "Bottom Color"));
            EditorGUI.indentLevel--;
        }

        private void DrawStateNervousSection()
        {
            if (!DrawFoldout(ref showStateNervous, L("緊張状態 (Nervous)", "Nervous State"))) return;
            EditorGUI.indentLevel++;
            MaterialProperty texProp = FindProperty("_NervousStateTex", properties, false);
            if (texProp != null) materialEditor.TextureProperty(texProp, L("緊張テクスチャ", "Nervous Texture"));
            DrawProperty("_NervousBackgroundColor", L("背景色 (HDR)", "Background Color (HDR)"));
            DrawProperty("_NervousLinesColor", L("線カラー (HDR)", "Lines Color (HDR)"));
            DrawProperty("_NervousLinesRandSeed", L("ランダムシード", "Random Seed"));
            DrawProperty("_NervousLinesRandOffs", L("線のオフセット", "Lines Offset"));
            DrawProperty("_NervousLinesSize", L("線のサイズ", "Lines Size"));
            DrawProperty("_NervousLinesThickness", L("線の太さ", "Lines Thickness"));
            DrawProperty("_NervousCenterFill", L("中心塗りつぶし", "Center Fill"));
            EditorGUI.indentLevel--;
        }

        private void DrawHueColorSection()
        {
            if (!DrawFoldout(ref showHueColor, L("色調調整 (Hue & Color)", "Hue & Color"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_Contrast", L("コントラスト", "Contrast"));
            DrawProperty("_MainSaturation", L("彩度", "Saturation"));
            DrawProperty("_MainHueShift", L("色相オフセット", "Hue Offset"));
            DrawProperty("_MainHueSpeed", L("色相アニメ速度", "Hue Animation Speed"));
            EditorGUI.indentLevel--;
        }

        private void DrawBubbleSection()
        {
            if (!DrawFoldout(ref showBubble, L("バブルエフェクト", "Bubble Effect"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_BubbleSize", L("バブルサイズ", "Bubble Size"));
            DrawProperty("_BubbleBrightness", L("バブル明るさ", "Bubble Brightness"));
            DrawProperty("_BubbleWobbleSpeed", L("揺れ速度", "Wobble Speed"));
            DrawProperty("_BubbleWobbleStrength", L("揺れ強度", "Wobble Strength"));
            EditorGUILayout.HelpBox(L("瞳のハイライト粒子エフェクト。キラキラした輝きを追加します。", "Iris highlight particle effect. Adds sparkling shine."), MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawIrisCausticsSection()
        {
            if (!DrawFoldout(ref showIrisCaustics, L("虹彩コースティクス", "Iris Caustics"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseIrisCaustics", L("コースティクスを有効化", "Enable Caustics"));
            DrawProperty("_IrisCausticsColor", L("コースティクスカラー (HDR)", "Caustics Color (HDR)"));
            DrawProperty("_IrisCausticsIntensity", L("強度", "Intensity"));
            DrawProperty("_IrisCausticsScale", L("スケール", "Scale"));
            DrawProperty("_IrisCausticsSpeed", L("速度", "Speed"));
            DrawProperty("_IrisCausticsParallax", L("パララックス", "Parallax"));
            DrawProperty("_IrisCausticsTwist", L("ツイスト", "Twist"));
            DrawProperty("_IrisCausticsBlendMode", L("ブレンドモード", "Blend Mode"));
            EditorGUI.indentLevel--;
        }

        private void DrawIrisRingPulseSection()
        {
            if (!DrawFoldout(ref showIrisRingPulse, L("虹彩リングパルス", "Iris Ring Pulse"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseIrisRingPulse", L("リングパルスを有効化", "Enable Ring Pulse"));
            DrawProperty("_IrisRingColor", L("リングカラー (HDR)", "Ring Color (HDR)"));
            DrawProperty("_IrisRingRadius", L("リング半径", "Ring Radius"));
            DrawProperty("_IrisRingWidth", L("リング幅", "Ring Width"));
            DrawProperty("_IrisRingPulseSpeed", L("パルス速度", "Pulse Speed"));
            DrawProperty("_IrisRingPulseAmount", L("パルス量", "Pulse Amount"));
            DrawProperty("_IrisRingParallax", L("パララックス", "Parallax"));
            DrawProperty("_IrisRingIntensity", L("強度", "Intensity"));
            DrawProperty("_IrisRingBlendMode", L("ブレンドモード", "Blend Mode"));
            EditorGUI.indentLevel--;
        }

        private void DrawTexturePolishSection()
        {
            if (!DrawFoldout(ref showTexturePolish, L("テクスチャポリッシュ", "Texture Polish"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseTexturePolish", L("テクスチャポリッシュを有効化", "Enable Texture Polish"));
            DrawProperty("_TexturePolishBlendMode", L("ブレンドモード", "Blend Mode"));
            DrawProperty("_TexturePolishStrength", L("強度", "Strength"));
            DrawProperty("_TexturePolishContrast", L("コントラスト", "Contrast"));
            DrawProperty("_TexturePolishSaturation", L("彩度", "Saturation"));
            DrawProperty("_TexturePolishIrisFocus", L("虹彩フォーカス", "Iris Focus"));
            DrawProperty("_TexturePolishTint", L("ティントカラー (HDR)", "Tint Color (HDR)"));
            EditorGUI.indentLevel--;
        }

        private void DrawExpressionSection()
        {
            if (!DrawFoldout(ref showExpression, L("表情オーバーレイ", "Expression Overlay"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_ExpressionPreset", L("表情プリセット", "Expression Preset"));
            DrawProperty("_ExpressionMode", L("表情モード (Off/Spiral/Tearful/Shock)", "Expression Mode (Off/Spiral/Tearful/Shock)"));
            DrawProperty("_ExpressionColor", L("表情カラー (HDR)", "Expression Color (HDR)"));
            DrawProperty("_ExpressionIntensity", L("強度", "Intensity"));
            DrawProperty("_ExpressionScale", L("スケール", "Scale"));
            DrawProperty("_ExpressionSpeed", L("速度", "Speed"));
            DrawProperty("_ExpressionParallax", L("パララックス", "Parallax"));
            DrawProperty("_ExpressionDetail", L("ディテール", "Detail"));

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField(L("スパイラル設定", "Spiral Settings"), EditorStyles.miniBoldLabel);
            DrawProperty("_SpiralTightness", L("スパイラル巻き", "Spiral Tightness"));
            DrawProperty("_SpiralLineWidth", L("スパイラル線幅", "Spiral Line Width"));

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField(L("涙設定", "Tear Settings"), EditorStyles.miniBoldLabel);
            DrawProperty("_TearFlow", L("涙の流れ", "Tear Flow"));
            DrawProperty("_TearRim", L("涙のリムハイライト", "Tear Rim Highlight"));

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField(L("ショックリング設定", "Shock Ring Settings"), EditorStyles.miniBoldLabel);
            DrawProperty("_ShockRingCount", L("リング数", "Ring Count"));
            DrawProperty("_ShockRingWidth", L("リング幅", "Ring Width"));
            DrawProperty("_ExpressionBlendMode", L("ブレンドモード", "Blend Mode"));
            EditorGUI.indentLevel--;
        }

        private void DrawInnerMeshPrioritySection()
        {
            if (!DrawFoldout(ref showInnerMeshPriority, L("内部メッシュ優先度 (BlendShape)", "Inner Mesh Priority (BlendShape)"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_UseInnerMeshPriority", L("内部メッシュ優先を有効化", "Enable Inner Mesh Priority"));
            MaterialProperty maskProp = FindProperty("_InnerMeshPriorityMask", properties, false);
            if (maskProp != null) materialEditor.TextureProperty(maskProp, L("優先度マスク", "Priority Mask"));
            DrawProperty("_InnerMeshPriorityChannel", L("マスクチャンネル", "Mask Channel"));
            DrawProperty("_InnerMeshPriorityThreshold", L("しきい値", "Threshold"));
            DrawProperty("_InnerMeshPrioritySoftness", L("柔らかさ", "Softness"));
            DrawProperty("_InnerMeshPriorityInvert", L("反転", "Invert"));
            DrawProperty("_InnerMeshPriorityMode", L("優先度モード", "Priority Mode"));
            DrawProperty("_InnerMeshPriorityStrength", L("強度", "Strength"));
            EditorGUILayout.HelpBox(L("BlendShape使用時の内部メッシュ描画優先度を制御します。", "Controls inner mesh draw priority when using BlendShapes."), MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawPerformanceSection()
        {
            if (!DrawFoldout(ref showPerformance, L("パフォーマンス", "Performance"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_PerformanceTier", L("パフォーマンスティア (Quality/Balanced/Lite)", "Performance Tier (Quality/Balanced/Lite)"));
            EditorGUILayout.HelpBox(L("Lite: 軽量化のため一部エフェクトを無効化。Quest対応時はLite推奨。", "Lite: Disables some effects for optimization. Recommended for Quest."), MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawVignetteSection()
        {
            if (!DrawFoldout(ref showVignette, L("ビネットエフェクト", "Vignette Effect"))) return;
            EditorGUI.indentLevel++;
            DrawProperty("_VignetteTransparency", L("透過を有効化", "Enable Transparency"));
            DrawProperty("_VignetteDitherScale", L("ディザースケール", "Dither Scale"));
            DrawProperty("_VignetteThickness", L("太さ", "Thickness"));
            DrawProperty("_VignetteFalloff", L("フォールオフ", "Falloff"));
            EditorGUI.indentLevel--;
        }

        private void DrawAudioLinkSection()
        {
            if (!DrawFoldout(ref showAudioLink, "Audio Link")) return;
            EditorGUI.indentLevel++;
            DrawProperty("_BandSelection", L("周波数帯域", "Frequency Band"));
            DrawProperty("_Intensity", L("強度", "Intensity"));
            DrawProperty("_MinValue", L("最小明るさ", "Minimum Brightness"));
            EditorGUILayout.HelpBox(L("VRChatワールドにAudioLinkが設置されている場合に音楽連動します。", "Syncs with music when AudioLink is installed in the VRChat world."), MessageType.None);
            EditorGUI.indentLevel--;
        }

        private void DrawRenderingSection()
        {
            if (!DrawFoldout(ref showRendering, L("レンダリング設定", "Rendering Settings"))) return;
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(L("ステンシル", "Stencil"), EditorStyles.miniBoldLabel);
            DrawProperty("_StencilRef", L("参照値", "Reference Value"));
            DrawProperty("_StencilComp", L("比較関数", "Comparison Function"));
            DrawProperty("_StencilPass", L("パス操作", "Pass Operation"));
            DrawProperty("_StencilFail", L("失敗操作", "Fail Operation"));
            DrawProperty("_StencilZFail", L("Z失敗操作", "Z Fail Operation"));
            DrawProperty("_StencilReadMask", L("リードマスク", "Read Mask"));
            DrawProperty("_StencilWriteMask", L("ライトマスク", "Write Mask"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("ブレンド & 透明", "Blend & Transparency"), EditorStyles.miniBoldLabel);
            DrawProperty("_SrcBlend", L("ソースブレンド", "Source Blend"));
            DrawProperty("_DstBlend", L("宛先ブレンド", "Destination Blend"));
            DrawProperty("_BlendOp", L("ブレンド操作", "Blend Operation"));
            DrawProperty("_TransparencyResolveMode", L("透過解決モード", "Transparency Resolve Mode"));
            DrawProperty("_TransparencyDitherScale", L("透過ディザースケール", "Transparency Dither Scale"));
            DrawProperty("_GlobalOpacity", L("全体透過率", "Global Opacity"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("深度 & カリング", "Depth & Culling"), EditorStyles.miniBoldLabel);
            DrawProperty("_ZTest", L("Zテスト", "Z Test"));
            DrawProperty("_ZWrite", L("Z書き込み", "Z Write"));
            DrawProperty("_OffsetFactor", L("オフセットファクター", "Offset Factor"));
            DrawProperty("_OffsetUnits", L("オフセットユニット", "Offset Units"));
            DrawProperty("_AlphaClipThreshold", L("アルファクリップしきい値", "Alpha Clip Threshold"));
            DrawProperty("_Cull", L("カリングモード", "Culling Mode"));
            DrawProperty("_ColorMask", L("カラーマスク", "Color Mask"));
            EditorGUI.indentLevel--;
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ナタネ 目シェーダー v1.1", "Natane Eye Shader v1.1"), FooterStyle);
            EditorGUILayout.LabelField(L("5つの瞳状態 + 表情オーバーレイ対応", "5 Eye States + Expression Overlay Support"), FooterStyle);
            EditorGUILayout.EndVertical();
        }
    }
}
