using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Helper methods for NataneToonShaderGUI
    /// NataneToonShaderGUIのヘルパーメソッド集
    /// Extracted from main ShaderGUI to reduce file size and improve maintainability
    /// </summary>
    public static class NataneToonShaderGUIHelpers
    {
        // ===== CONSTANTS =====
        private const float FLOAT_COMPARISON_THRESHOLD = 0.5f;

        // ===== COMMON DELEGATE TYPES =====
        public delegate bool DrawToggleDelegate(string keyword, string propertyName, string label);
        public delegate void DrawPropertyDelegate(string propertyName, string label);
        public delegate bool DrawHelpToggleDelegate(string sectionKey, string helpText, MessageType messageType = MessageType.Info);
        public delegate void SaveFoldoutStatesDelegate();
        public delegate MaterialProperty FindPropertyDelegate(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory);

        // ===== MAIN SECTION DRAWING =====

        /// <summary>
        /// Draw the complete Shading section
        /// シェーディングセクション全体を描画
        /// </summary>
        public static void DrawShadingSection(
            ref bool showShading,
            MaterialProperty[] properties,
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            SaveFoldoutStatesDelegate saveFoldoutStates,
            FindPropertyDelegate findProperty)
        {
            EditorGUI.BeginChangeCheck();
            showShading = EditorGUILayout.Foldout(showShading, "シェーディング", true, EditorStyles.foldoutHeader);
            if (EditorGUI.EndChangeCheck()) saveFoldoutStates();

            if (showShading)
            {
                EditorGUI.indentLevel++;

                drawHelpToggle("ShadingSection",
                    "🎨 アニメ調セルシェーディング\n" +
                    "クリーンで明瞭な陰影境界を実現し、高品質なアニメ調レンダリングを提供します。",
                    MessageType.None);

                EditorGUILayout.Space(5);

                // Main shading controls (Ramp or Toon/Gradient mode)
                DrawShadingModeControls(properties, drawToggle, drawProperty, drawHelpToggle, findProperty);

                EditorGUILayout.Space(10);

                // Shadow Receive Mask
                DrawShadowReceiveMaskControls(drawToggle, drawProperty, drawHelpToggle);

                EditorGUILayout.Space(10);

                // Ambient Occlusion
                DrawAmbientOcclusionControls(drawToggle, drawProperty, drawHelpToggle);

                EditorGUILayout.Space(10);

                // Dithering
                DrawDitheringControls(drawToggle, drawProperty, drawHelpToggle);

                EditorGUILayout.Space(10);

                // SDF Shadow Map
                DrawSDFShadowMapControls(drawToggle, drawProperty, drawHelpToggle);

                EditorGUILayout.Space(10);

                // Shading Grade Map
                DrawShadingGradeMapControls(drawToggle, drawProperty, drawHelpToggle);

                EditorGUILayout.Space(10);

                // Shadow Color Texture
                DrawShadowColorTextureControls(drawToggle, drawProperty, drawHelpToggle);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// Draw shading mode controls (Ramp texture or Toon/Gradient mode)
        /// シェーディングモード制御（ランプテクスチャまたはToon/Gradientモード）
        /// </summary>
        public static void DrawShadingModeControls(
            MaterialProperty[] properties,
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            FindPropertyDelegate findProperty)
        {
            // Shadow softness guide (shown when help mode is active)
            drawHelpToggle("ShadowSoftnessGuide",
                "🎨 影をソフトにしたい場合:\n" +
                "① まず「影のなじませ」を 0.3〜0.5 に設定\n" +
                "② 全体を柔らかくしたい場合は「ライト部分のソフトネス」を調整\n" +
                "③ 高度な調整は「高度なライティング」タブへ",
                MessageType.None);

            bool useRamp = drawToggle("_USE_RAMP", "_UseRamp", "ランプテクスチャを使用");

            if (useRamp)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("ランプテクスチャ設定", EditorStyles.boldLabel);
                drawProperty("_RampTex", "ランプテクスチャ");
                drawHelpToggle("RampTexture",
                    "ランプテクスチャは暗い色（左）から明るい色（右）へのグラデーションにしてください。\n" +
                    "カスタムグラデーションで独自の影の色合いを作成できます。",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("シェーディングモード", EditorStyles.boldLabel);
                drawProperty("_ShadingMode", "モード");
                drawHelpToggle("ShadingMode",
                    "🎨 シェーディングモード:\n" +
                    "• Toon: 階段状のセルシェーディング（クラシックなアニメ調）\n" +
                    "• Gradient: 滑らかなグラデーションシェーディング（柔らかい印象）",
                    MessageType.None);

                EditorGUILayout.Space(5);

                // Get current shading mode value
                MaterialProperty shadingModeProp = findProperty("_ShadingMode", properties, false);
                bool isGradientMode = shadingModeProp != null && shadingModeProp.floatValue >= FLOAT_COMPARISON_THRESHOLD;

                if (isGradientMode)
                {
                    DrawGradientModeSettings(drawProperty, drawHelpToggle);
                }
                else
                {
                    DrawToonModeSettings(drawProperty, drawHelpToggle, drawToggle);
                }
            }

            // Common controls for all shading modes
            EditorGUILayout.Space(5);
            drawProperty("_ShadowOffset", "影のオフセット");
            drawHelpToggle("ShadowOffset",
                "影の境界を調整します。正の値で影を明るく、負の値で影を暗くします。",
                MessageType.Info);

            EditorGUILayout.Space(5);
            drawProperty("_LitSoftness", "ライト部分のソフトネス");
            drawHelpToggle("LitSoftness",
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
                "よりイラスト調の柔らかな影が欲しい場合に調整してください。",
                drawProperty);

            // Show warning when shadow blend is very high
            MaterialProperty shadowBlendProp = findProperty("_ShadowBlend", properties, false);
            if (shadowBlendProp != null && shadowBlendProp.floatValue > 0.7f)
            {
                EditorGUILayout.HelpBox(
                    "影のなじませが高い値に設定されています。必要に応じて「高度なライティング」タブの他のソフトネスパラメーターも調整してください。",
                    MessageType.Info);
            }
        }

        /// <summary>
        /// Draw a blend/softness parameter with help text
        /// </summary>
        public static void DrawBlendParameter(
            string propertyName,
            string label,
            string helpText,
            DrawPropertyDelegate drawProperty)
        {
            drawProperty(propertyName, label);
            EditorGUILayout.HelpBox(helpText, MessageType.Info);
        }

        /// <summary>
        /// Draw gradient mode specific settings
        /// </summary>
        public static void DrawGradientModeSettings(
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            EditorGUILayout.LabelField("グラデーション設定", EditorStyles.boldLabel);
            drawProperty("_ShadowColor", "影の色");
            drawProperty("_ShadingGradientWidth", "グラデーション幅");
            drawHelpToggle("ShadingGradientWidth",
                "✨ グラデーション幅:\n" +
                "影と光の境界の滑らかさを調整します。\n" +
                "• 0.1 = 狭いグラデーション（シャープな境界）\n" +
                "• 0.2-0.3 = 標準的なグラデーション（推奨）\n" +
                "• 0.5+ = 広いグラデーション（非常に柔らかい）\n\n" +
                "💡 柔らかい印象を与えるために、0.2以上の値がおすすめです。",
                MessageType.Info);
        }

        /// <summary>
        /// Draw multi-tone shadow settings
        /// </summary>
        public static void DrawMultiToneShadowSettings(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useMultiShadow = drawToggle("_USE_MULTI_SHADOW", "_UseMultiShadow", "多段階影を使用");
            if (useMultiShadow)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("多段階影設定", EditorStyles.boldLabel);

                // 2nd shadow level
                drawProperty("_Shadow2ndColor", "影の色 (2段目)");
                drawProperty("_Shadow2ndBorder", "2段目の境界");
                drawHelpToggle("Shadow2ndBorder",
                    "💡 2段目の境界:\n" +
                    "この値より暗い部分に2段目の影色が適用されます。\n" +
                    "• 0.5 = 半分より暗い部分\n" +
                    "• 0.3 = やや暗い部分（推奨）\n" +
                    "• 0.1 = 最も暗い部分のみ",
                    MessageType.None);

                EditorGUILayout.Space();

                // 3rd shadow level
                drawProperty("_Shadow3rdColor", "影の色 (3段目)");
                drawProperty("_Shadow3rdBorder", "3段目の境界");
                drawHelpToggle("Shadow3rdBorder",
                    "💡 3段目の境界:\n" +
                    "この値より暗い部分に3段目の影色（最も濃い影）が適用されます。\n" +
                    "• 0.15-0.2 = 標準的な最暗部（推奨）\n" +
                    "• 0.05-0.1 = 非常に暗い部分のみ",
                    MessageType.None);

                EditorGUILayout.Space();
                drawHelpToggle("MultiShadowUsage",
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
        /// Draw toon mode specific settings
        /// </summary>
        public static void DrawToonModeSettings(
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            DrawToggleDelegate drawToggle)
        {
            EditorGUILayout.LabelField("セルシェーディング設定", EditorStyles.boldLabel);
            drawProperty("_ShadowColor", "影の色 (1段目)");

            // Multi-tone shadow colors
            EditorGUILayout.Space();
            DrawMultiToneShadowSettings(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space();
            drawProperty("_ShadowSteps", "影のステップ数");
            drawHelpToggle("ShadowSteps",
                "推奨値: 2-3（アニメ調）、より多いステップでグラデーション効果",
                MessageType.Info);

            drawProperty("_ShadowSharpness", "影のシャープネス");
            drawHelpToggle("ShadowSharpness",
                "低い値: シャープな境界（アニメ調）\n" +
                "高い値: 柔らかい境界（イラスト調）\n" +
                "アニメ調推奨: 0.05-0.15",
                MessageType.Info);
        }

        /// <summary>
        /// Draw shadow receive mask controls
        /// </summary>
        public static void DrawShadowReceiveMaskControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useShadowReceiveMask = drawToggle("_SHADOW_RECEIVE_MASK", "_UseShadowReceiveMask", "シャドー受け取りマスクを使用");

            if (useShadowReceiveMask)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("シャドー受け取りマスク設定", EditorStyles.boldLabel);
                drawProperty("_ShadowReceiveMask", "シャドー受け取りマスク");
                drawHelpToggle("ShadowReceiveMask",
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
        public static void DrawAmbientOcclusionControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useAO = drawToggle("_USE_AO", "_UseAO", "アンビエントオクルージョン（AO）を使用");

            if (useAO)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("AO設定", EditorStyles.boldLabel);
                drawProperty("_AOMap", "AOマップ");
                drawProperty("_AOIntensity", "AO強度");
                drawHelpToggle("AmbientOcclusion",
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
        /// Draw dithering controls
        /// </summary>
        public static void DrawDitheringControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useDithering = drawToggle("_USE_DITHERING", "_UseDithering", "ディザリング（ハーフトーン）を使用");

            if (useDithering)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("ディザリング設定", EditorStyles.boldLabel);
                drawProperty("_DitheringScale", "ディザリングスケール");
                drawProperty("_DitheringStrength", "ディザリング強度");
                drawHelpToggle("Dithering",
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

        /// <summary>
        /// Draw SDF Shadow Map controls
        /// </summary>
        public static void DrawSDFShadowMapControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useSDFMap = drawToggle("_SDF_MAP", "_UseSDFMap", "SDF Shadow Mapを使用");

            if (useSDFMap)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("SDF Shadow Map設定", EditorStyles.boldLabel);
                drawProperty("_SDFMap", "SDFマップ");
                drawProperty("_SDFIntensity", "SDF強度");
                drawProperty("_SDFSoftness", "SDFソフトネス");
                drawProperty("_SDFOffset", "SDFオフセット");
                drawHelpToggle("SDFShadowMap",
                    "📍 SDF Shadow Map（距離場シャドウマップ）:\n" +
                    "テクスチャベースで影の位置を正確にコントロールできる高度な機能です。\n\n" +
                    "• SDFマップ: 白 = 明るい、黒 = 影\n" +
                    "• 強度: 影の強さ（0-1）\n" +
                    "• ソフトネス: 影の境界の柔らかさ\n" +
                    "• オフセット: 影の位置調整\n\n" +
                    "💡 使い方:\n" +
                    "顔の影を細かく制御したい場合や、特定の場所に常に影を落としたい場合に使用します。",
                    MessageType.Info);
            }

            // Face SDF Rotation (Genshin/AK:EF style)
            bool faceSDFRotation = drawToggle("_FACE_SDF_ROTATION", "_FaceSDFRotation", "Face SDF回転追従を有効化");
            if (faceSDFRotation)
            {
                EditorGUI.indentLevel++;
                drawProperty("_FaceForwardDirection", "顔の正面方向");
                drawProperty("_FaceRightDirection", "顔の右方向");
                drawHelpToggle("FaceSDFRotation",
                    "🔄 Face SDF回転追従:\n" +
                    "SDF影がライトの方向に追従して回転します。\n" +
                    "Genshin Impact / アークナイツ：エンドフィールド スタイルの\n" +
                    "顔影表現を実現します。\n\n" +
                    "• 正面方向: キャラの顔が向いている方向（オブジェクト空間）\n" +
                    "• 右方向: キャラの顔の右側の方向（オブジェクト空間）\n\n" +
                    "💡 使い方:\n" +
                    "顔のSDF影がライトの方向に応じて自動的に回転し、\n" +
                    "どの角度からでも自然な影表現を維持します。",
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draw Shading Grade Map controls
        /// </summary>
        public static void DrawShadingGradeMapControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useShadingGradeMap = drawToggle("_SHADING_GRADE_MAP", "_UseGradeMap", "Shading Grade Mapを使用");

            if (useShadingGradeMap)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Shading Grade Map設定", EditorStyles.boldLabel);
                drawProperty("_ShadingGradeMap", "Shading Grade Map");
                drawProperty("_ShadingGradeScale", "グレードスケール");
                drawHelpToggle("ShadingGradeMap",
                    "🎭 Shading Grade Map:\n" +
                    "影の濃さを部分的に調整できる機能です。\n\n" +
                    "• 白 = 明るく（影が薄くなる）\n" +
                    "• 黒 = 暗く（影が濃くなる）\n" +
                    "• グレー = 中間\n" +
                    "• スケール: -1（暗く）～ 0（変化なし）～ 1（明るく）\n\n" +
                    "💡 使い方:\n" +
                    "顔は明るく、服は暗くなど、部位ごとに影の濃さを変えたい場合に使用します。",
                    MessageType.Info);
            }
        }

        /// <summary>
        /// Draw Shadow Color Texture controls
        /// </summary>
        public static void DrawShadowColorTextureControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Shadow Color Texture設定", EditorStyles.boldLabel);
            drawProperty("_ShadowColorTex", "Shadow Color Texture");
            drawProperty("_ShadowColorTexStrength", "適用強度");
            drawHelpToggle("ShadowColorTexture",
                "🌈 Shadow Color Texture:\n" +
                "影の色をテクスチャで指定できる高度な機能です。\n\n" +
                "• テクスチャの色が影色として使用されます\n" +
                "• 強度: テクスチャの影響度（0 = 使わない、1 = フル適用）\n\n" +
                "💡 使い方:\n" +
                "服の影を青っぽく、肌の影を赤っぽくなど、\n" +
                "部位ごとに異なる影色を設定したい場合に使用します。",
                MessageType.Info);
        }
    }
}
