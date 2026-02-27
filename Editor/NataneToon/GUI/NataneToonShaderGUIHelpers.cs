using UnityEngine;
using UnityEditor;
using static NataneToon.Editor.NataneToonLocalization;

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
            // Content only (foldout/boxed section handled by caller)
            DrawShadingSectionContent(properties, drawToggle, drawProperty, drawHelpToggle, findProperty);
        }

        /// <summary>
        /// Draw shading section content without foldout wrapper
        /// フォルダウトなしでシェーディングセクションの内容を描画（呼び出し元がBoxedSectionを管理）
        /// </summary>
        public static void DrawShadingSectionContent(
            MaterialProperty[] properties,
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            FindPropertyDelegate findProperty)
        {
            drawHelpToggle("ShadingSection",
                L("アニメ調セルシェーディング - クリーンで明瞭な陰影境界を実現します。",
                  "Anime-style cel shading - Achieves clean and clear shadow boundaries."),
                MessageType.None);

            EditorGUILayout.Space(5);

            // Main shading controls (Ramp or Toon/Gradient mode)
            DrawShadingModeControls(properties, drawToggle, drawProperty, drawHelpToggle, findProperty);

            EditorGUILayout.Space(10);

            // Shadow Receive Mask
            DrawShadowReceiveMaskControls(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space(10);

            // AO・ディザリング設定は「ライト&影」タブに移動
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(L("AO・ディザリング設定は「ライト&影」タブに移動しました。", "AO and dithering settings have been moved to the \"Light & Shadow\" tab."), MessageType.None);
            EditorGUILayout.Space(5);

            // SDF Shadow Map
            DrawSDFShadowMapControls(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space(10);

            // Shading Grade Map
            DrawShadingGradeMapControls(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space(10);

            // Shadow Color Texture
            DrawShadowColorTextureControls(drawToggle, drawProperty, drawHelpToggle);
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
                L("🎨 影をソフトにしたい場合:\n" +
                  "① まず「影のなじませ」を 0.3〜0.5 に設定\n" +
                  "② 全体を柔らかくしたい場合は「ライト部分のソフトネス」を調整\n" +
                  "③ 高度な調整は「高度なライティング」タブへ",
                  "🎨 To soften shadows:\n" +
                  "① First set \"Shadow Blend\" to 0.3-0.5\n" +
                  "② To soften overall, adjust \"Lit Softness\"\n" +
                  "③ For advanced adjustments, go to the \"Advanced Lighting\" tab"),
                MessageType.None);

            bool useRamp = drawToggle("_USE_RAMP", "_UseRamp", L("ランプテクスチャを使用", "Use Ramp Texture"));

            if (useRamp)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("ランプテクスチャ設定", "Ramp Texture Settings"), EditorStyles.boldLabel);
                drawProperty("_RampTex", L("ランプテクスチャ", "Ramp Texture"));
                drawHelpToggle("RampTexture",
                    L("ランプテクスチャは暗い色（左）から明るい色（右）へのグラデーションにしてください。\n" +
                      "カスタムグラデーションで独自の影の色合いを作成できます。",
                      "The ramp texture should be a gradient from dark (left) to bright (right).\n" +
                      "You can create custom shadow tones using a custom gradient."),
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("シェーディングモード", "Shading Mode"), EditorStyles.boldLabel);
                drawProperty("_ShadingMode", L("モード", "Mode"));
                drawHelpToggle("ShadingMode",
                    L("🎨 シェーディングモード:\n" +
                      "• Toon: 階段状のセルシェーディング（クラシックなアニメ調）\n" +
                      "• Gradient: 滑らかなグラデーションシェーディング（柔らかい印象）\n" +
                      "• StandardToon: lilToon互換のシェーディング（移行時に使用）",
                      "🎨 Shading Mode:\n" +
                      "• Toon: Stepped cel shading (classic anime style)\n" +
                      "• Gradient: Smooth gradient shading (soft impression)\n" +
                      "• StandardToon: lilToon-compatible shading (for migration)"),
                    MessageType.None);

                EditorGUILayout.Space(5);

                // Get current shading mode value
                MaterialProperty shadingModeProp = findProperty("_ShadingMode", properties, false);
                float shadingModeValue = shadingModeProp != null ? shadingModeProp.floatValue : 0f;
                bool isStandardToon = shadingModeValue >= 1.5f;
                bool isGradientMode = !isStandardToon && shadingModeValue >= FLOAT_COMPARISON_THRESHOLD;

                if (isStandardToon)
                {
                    DrawStandardToonSettings(drawProperty, drawHelpToggle, drawToggle);
                }
                else if (isGradientMode)
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
            drawProperty("_ShadowOffset", L("影のオフセット", "Shadow Offset"));
            drawHelpToggle("ShadowOffset",
                L("影の境界を調整します。正の値で影を明るく、負の値で影を暗くします。",
                  "Adjusts the shadow boundary. Positive values brighten shadows, negative values darken them."),
                MessageType.Info);

            EditorGUILayout.Space(5);
            drawProperty("_LitSoftness", L("ライト部分のソフトネス", "Lit Softness"));
            drawHelpToggle("LitSoftness",
                L("✨ ライト部分のなじませ調整:\n" +
                  "光の当たっている部分を周囲となじませます。\n" +
                  "• 0 = シャープな境界（デフォルト）\n" +
                  "• 0.3-0.5 = 適度な柔らかさ\n" +
                  "• 1.0 = 最大のなじませ効果\n\n" +
                  "💡 使い方: 光の当たり方が強すぎる場合や、\n" +
                  "より滑らかなグラデーションが欲しい場合に調整してください。",
                  "✨ Lit Softness Adjustment:\n" +
                  "Blends the lit areas with their surroundings.\n" +
                  "• 0 = Sharp boundary (default)\n" +
                  "• 0.3-0.5 = Moderate softness\n" +
                  "• 1.0 = Maximum blending\n\n" +
                  "💡 Usage: Adjust when lighting is too harsh or\n" +
                  "when you want a smoother gradient."),
                MessageType.Info);

            EditorGUILayout.Space(5);
            DrawBlendParameter(
                "_ShadowBlend",
                L("影のなじませ（柔らかさ）", "Shadow Blend (Softness)"),
                L("✨ 影のなじませ調整:\n" +
                  "影の境界（特に多段階影の境目）を周囲となじませて、\n" +
                  "より柔らかく美しい印象にします。\n\n" +
                  "• 0 = シャープな境界（デフォルト）\n" +
                  "• 0.2-0.4 = 適度な柔らかさ（推奨）\n" +
                  "• 0.5-0.7 = かなり柔らかい境界\n" +
                  "• 0.8-1.0 = 非常に広いフェード（水彩風）\n\n" +
                  "💡 多段階影の境目がパっきり出る場合:\n" +
                  "この値を0.3〜0.5に設定すると自然になじみます。",
                  "✨ Shadow Blend Adjustment:\n" +
                  "Blends shadow boundaries (especially multi-tone shadow edges)\n" +
                  "with their surroundings for a softer, more beautiful look.\n\n" +
                  "• 0 = Sharp boundary (default)\n" +
                  "• 0.2-0.4 = Moderate softness (recommended)\n" +
                  "• 0.5-0.7 = Fairly soft boundary\n" +
                  "• 0.8-1.0 = Very wide fade (watercolor style)\n\n" +
                  "💡 If multi-tone shadow edges appear too sharp:\n" +
                  "Set this value to 0.3-0.5 for a natural blend."),
                drawProperty);

            // Show warning when shadow blend is very high
            MaterialProperty shadowBlendProp = findProperty("_ShadowBlend", properties, false);
            if (shadowBlendProp != null && shadowBlendProp.floatValue > 0.7f)
            {
                EditorGUILayout.HelpBox(
                    L("影のなじませが高い値に設定されています。必要に応じて「高度なライティング」タブの他のソフトネスパラメーターも調整してください。",
                      "Shadow blend is set to a high value. Consider adjusting other softness parameters in the \"Advanced Lighting\" tab as needed."),
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
            EditorGUILayout.LabelField(L("グラデーション設定", "Gradient Settings"), EditorStyles.boldLabel);
            drawProperty("_ShadowColor", L("影の色", "Shadow Color"));
            drawProperty("_ShadingGradientWidth", L("グラデーション幅", "Gradient Width"));
            drawHelpToggle("ShadingGradientWidth",
                L("✨ グラデーション幅:\n" +
                  "影と光の境界の滑らかさを調整します。\n" +
                  "• 0.1 = 狭いグラデーション（シャープな境界）\n" +
                  "• 0.2-0.3 = 標準的なグラデーション（推奨）\n" +
                  "• 0.5+ = 広いグラデーション（非常に柔らかい）\n\n" +
                  "💡 柔らかい印象を与えるために、0.2以上の値がおすすめです。",
                  "✨ Gradient Width:\n" +
                  "Adjusts the smoothness of the shadow-light boundary.\n" +
                  "• 0.1 = Narrow gradient (sharp boundary)\n" +
                  "• 0.2-0.3 = Standard gradient (recommended)\n" +
                  "• 0.5+ = Wide gradient (very soft)\n\n" +
                  "💡 Values of 0.2 or higher are recommended for a soft impression."),
                MessageType.Info);
        }

        /// <summary>
        /// Draw StandardToon mode specific settings (lilToon-compatible)
        /// </summary>
        public static void DrawStandardToonSettings(
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            DrawToggleDelegate drawToggle)
        {
            EditorGUILayout.LabelField(L("StandardToon設定 (lilToon互換)", "StandardToon Settings (lilToon Compatible)"), EditorStyles.boldLabel);
            drawProperty("_STShadowBorder", L("影の境界", "Shadow Border"));
            drawProperty("_STShadowBlur", L("影のぼかし", "Shadow Blur"));
            drawProperty("_STShadowStrength", L("影の強さ", "Shadow Strength"));
            drawProperty("_ShadowColor", L("影の色 (1段目)", "Shadow Color (1st)"));

            // Multi-tone shadow colors (shared with Toon mode)
            EditorGUILayout.Space();
            DrawMultiToneShadowSettings(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space();
            drawProperty("_STAsUnlit", L("アンライト度", "As Unlit"));

            drawHelpToggle("StandardToon",
                L("🎨 StandardToonモード:\n" +
                  "lilToon と同じ計算式を使用し、シェーダー切り替え時の見た目の一致を実現します。\n\n" +
                  "• Half-Lambert NdotL（影の位置がlilToonと一致）\n" +
                  "• リニア補間（smoothstepではなく線形補間）\n" +
                  "• ShadowStrength 制御（影の強さを調整可能）\n" +
                  "• 簡易ライトカラー乗算（normalize+luminance分離なし）\n\n" +
                  "💡 lilToonからの移行時に自動で選択されます。",
                  "🎨 StandardToon Mode:\n" +
                  "Uses the same calculations as lilToon for visual parity when switching shaders.\n\n" +
                  "• Half-Lambert NdotL (shadow position matches lilToon)\n" +
                  "• Linear interpolation (not smoothstep)\n" +
                  "• ShadowStrength control\n" +
                  "• Simple light color multiply (no normalize+luminance)\n\n" +
                  "💡 Automatically selected when migrating from lilToon."),
                MessageType.None);
        }

        /// <summary>
        /// Draw multi-tone shadow settings
        /// </summary>
        public static void DrawMultiToneShadowSettings(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useMultiShadow = drawToggle("_USE_MULTI_SHADOW", "_UseMultiShadow", L("多段階影を使用", "Use Multi-Tone Shadow"));
            if (useMultiShadow)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("多段階影設定", "Multi-Tone Shadow Settings"), EditorStyles.boldLabel);

                // 2nd shadow level
                drawProperty("_Shadow2ndColor", L("影の色 (2段目)", "Shadow Color (2nd)"));
                drawProperty("_Shadow2ndBorder", L("2段目の境界", "2nd Border"));
                drawHelpToggle("Shadow2ndBorder",
                    L("💡 2段目の境界:\n" +
                      "この値より暗い部分に2段目の影色が適用されます。\n" +
                      "• 0.5 = 半分より暗い部分\n" +
                      "• 0.3 = やや暗い部分（推奨）\n" +
                      "• 0.1 = 最も暗い部分のみ",
                      "💡 2nd Border:\n" +
                      "The 2nd shadow color is applied to areas darker than this value.\n" +
                      "• 0.5 = Darker than half\n" +
                      "• 0.3 = Slightly dark areas (recommended)\n" +
                      "• 0.1 = Darkest areas only"),
                    MessageType.None);

                EditorGUILayout.Space();

                // 3rd shadow level
                drawProperty("_Shadow3rdColor", L("影の色 (3段目)", "Shadow Color (3rd)"));
                drawProperty("_Shadow3rdBorder", L("3段目の境界", "3rd Border"));
                drawHelpToggle("Shadow3rdBorder",
                    L("💡 3段目の境界:\n" +
                      "この値より暗い部分に3段目の影色（最も濃い影）が適用されます。\n" +
                      "• 0.15-0.2 = 標準的な最暗部（推奨）\n" +
                      "• 0.05-0.1 = 非常に暗い部分のみ",
                      "💡 3rd Border:\n" +
                      "The 3rd shadow color (deepest shadow) is applied to areas darker than this value.\n" +
                      "• 0.15-0.2 = Standard darkest areas (recommended)\n" +
                      "• 0.05-0.1 = Very dark areas only"),
                    MessageType.None);

                EditorGUILayout.Space();
                drawHelpToggle("MultiShadowUsage",
                    L("🎨 多段階影の使い方:\n" +
                      "より細かな諧調表現が可能になります。\n" +
                      "• 1段目: メインの影色（明るい影）\n" +
                      "• 2段目: 中間の影色\n" +
                      "• 3段目: 最も濃い影色（深い影）\n\n" +
                      "境界値は 1段目 > 2段目 > 3段目 の順に設定してください。",
                      "🎨 How to use multi-tone shadows:\n" +
                      "Enables finer tonal expression.\n" +
                      "• 1st: Main shadow color (light shadow)\n" +
                      "• 2nd: Mid-tone shadow color\n" +
                      "• 3rd: Deepest shadow color\n\n" +
                      "Set border values in order: 1st > 2nd > 3rd."),
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
            EditorGUILayout.LabelField(L("セルシェーディング設定", "Cel Shading Settings"), EditorStyles.boldLabel);
            drawProperty("_ShadowColor", L("影の色 (1段目)", "Shadow Color (1st)"));

            // Multi-tone shadow colors
            EditorGUILayout.Space();
            DrawMultiToneShadowSettings(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space();
            drawProperty("_ShadowSteps", L("影のステップ数", "Shadow Steps"));
            drawHelpToggle("ShadowSteps",
                L("推奨値: 2-3（アニメ調）、より多いステップでグラデーション効果",
                  "Recommended: 2-3 (anime style), more steps for gradient effect"),
                MessageType.Info);

            drawProperty("_ShadowSharpness", L("影のシャープネス", "Shadow Sharpness"));
            drawHelpToggle("ShadowSharpness",
                L("低い値: シャープな境界（アニメ調）\n" +
                  "高い値: 柔らかい境界（イラスト調）\n" +
                  "アニメ調推奨: 0.05-0.15",
                  "Low values: Sharp boundary (anime style)\n" +
                  "High values: Soft boundary (illustration style)\n" +
                  "Anime recommended: 0.05-0.15"),
                MessageType.Info);

            drawProperty("_StepBorderSmooth", L("段階境界のなじみ", "Step Border Smoothing"));
            drawHelpToggle("StepBorderSmooth",
                L("🎨 段階境界のなじみ:\n" +
                  "多段階影のステップ間の境界をなじませます。\n\n" +
                  "• 0 = シャープな境界（デフォルト）\n" +
                  "• 0.1-0.3 = 軽いなじみ（推奨）\n" +
                  "• 0.4-0.7 = 柔らかい境界\n" +
                  "• 0.8-1.0 = ほぼグラデーション\n\n" +
                  "💡 「影のシャープネス」とは独立して動作します。\n" +
                  "多段階影の色の遷移にも適用されます。",
                  "🎨 Step Border Smoothing:\n" +
                  "Smooths the boundaries between multi-tone shadow steps.\n\n" +
                  "• 0 = Sharp boundary (default)\n" +
                  "• 0.1-0.3 = Light smoothing (recommended)\n" +
                  "• 0.4-0.7 = Soft boundary\n" +
                  "• 0.8-1.0 = Nearly gradient\n\n" +
                  "💡 Works independently from \"Shadow Sharpness\".\n" +
                  "Also applies to multi-tone shadow color transitions."),
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
            bool useShadowReceiveMask = drawToggle("_SHADOW_RECEIVE_MASK", "_UseShadowReceiveMask", L("シャドー受け取りマスクを使用", "Use Shadow Receive Mask"));

            if (useShadowReceiveMask)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("シャドー受け取りマスク設定", "Shadow Receive Mask Settings"), EditorStyles.boldLabel);
                drawProperty("_ShadowReceiveMask", L("シャドー受け取りマスク", "Shadow Receive Mask"));
                drawHelpToggle("ShadowReceiveMask",
                    L("🎭 シャドー受け取りマスク（髪の影問題解決）:\n" +
                      "• 白 = 影を受けない（明るく保つ、シェーディングも無効化）\n" +
                      "• 黒 = 影を完全に受ける（通常の影とシェーディング）\n" +
                      "• グレー = 影を部分的に受ける\n\n" +
                      "💡 使い方：\n" +
                      "顔が髪の影で暗くなる場合、顔部分を白く塗ったマスクを使用することで\n" +
                      "顔に影がかからないようにできます。VRChatアバターでよく使われるテクニックです。\n\n" +
                      "🌟 Light Volumeとの統合：\n" +
                      "マスクはLight Volumeの間接光（リム効果）も制御します。\n" +
                      "白い部分は暗いワールドでも明るく保たれます。",
                      "🎭 Shadow Receive Mask (solves hair shadow issue):\n" +
                      "• White = Does not receive shadows (stays bright, disables shading)\n" +
                      "• Black = Fully receives shadows (normal shadows and shading)\n" +
                      "• Gray = Partially receives shadows\n\n" +
                      "💡 Usage:\n" +
                      "When the face gets darkened by hair shadows, use a mask with white painted on the face area\n" +
                      "to prevent shadows from falling on the face. A common technique for VRChat avatars.\n\n" +
                      "🌟 Light Volume Integration:\n" +
                      "The mask also controls Light Volume indirect light (rim effect).\n" +
                      "White areas stay bright even in dark worlds."),
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
            bool useAO = drawToggle("_USE_AO", "_UseAO", L("アンビエントオクルージョン（AO）を使用", "Use Ambient Occlusion (AO)"));

            if (useAO)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("AO設定", "AO Settings"), EditorStyles.boldLabel);
                drawProperty("_AOMap", L("AOマップ", "AO Map"));
                drawProperty("_AOIntensity", L("AO強度", "AO Intensity"));
                drawHelpToggle("AmbientOcclusion",
                    L("🌑 アンビエントオクルージョン（AO）:\n" +
                      "隙間や窪みなど、環境光が届きにくい部分を暗くして\n" +
                      "より立体的で柔らかい印象を与えます。\n\n" +
                      "• AOマップ: 白 = 明るい、黒 = 暗い\n" +
                      "• AO強度: 0 = 効果なし、1 = 最大効果\n\n" +
                      "💡 使い方:\n" +
                      "衣服の折り目、髪の毛の重なり、耳の内側など\n" +
                      "自然な陰影を加えたい部分にAOマップで指定します。\n" +
                      "推奨強度: 0.5-0.8",
                      "🌑 Ambient Occlusion (AO):\n" +
                      "Darkens areas where ambient light is hard to reach, such as gaps and crevices,\n" +
                      "giving a more three-dimensional and soft impression.\n\n" +
                      "• AO Map: White = bright, Black = dark\n" +
                      "• AO Intensity: 0 = no effect, 1 = maximum effect\n\n" +
                      "💡 Usage:\n" +
                      "Use the AO map to add natural shading to areas like\n" +
                      "clothing folds, hair overlaps, and inner ears.\n" +
                      "Recommended intensity: 0.5-0.8"),
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
            bool useDithering = drawToggle("_USE_DITHERING", "_UseDithering", L("ディザリング（ハーフトーン）を使用", "Use Dithering (Halftone)"));

            if (useDithering)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("ディザリング設定", "Dithering Settings"), EditorStyles.boldLabel);
                drawProperty("_DitheringScale", L("ディザリングスケール", "Dithering Scale"));
                drawProperty("_DitheringStrength", L("ディザリング強度", "Dithering Strength"));
                drawHelpToggle("Dithering",
                    L("🎨 ディザリング（ハーフトーン）:\n" +
                      "影の境界にドットパターンを追加して、\n" +
                      "より柔らかく芸術的な印象を与えます。\n\n" +
                      "• スケール: パターンの細かさ（推奨: 5-20）\n" +
                      "  　小さい値 = 細かいパターン\n" +
                      "  　大きい値 = 粗いパターン\n" +
                      "• 強度: 効果の強さ（推奨: 0.3-0.7）\n" +
                      "  　0 = 効果なし、1 = 最大効果\n\n" +
                      "💡 使い方:\n" +
                      "印刷物やマンガ風の柔らかい影の表現に最適です。",
                      "🎨 Dithering (Halftone):\n" +
                      "Adds a dot pattern to shadow boundaries\n" +
                      "for a softer, more artistic impression.\n\n" +
                      "• Scale: Pattern fineness (recommended: 5-20)\n" +
                      "   Small values = Fine pattern\n" +
                      "   Large values = Coarse pattern\n" +
                      "• Strength: Effect intensity (recommended: 0.3-0.7)\n" +
                      "   0 = No effect, 1 = Maximum effect\n\n" +
                      "💡 Usage:\n" +
                      "Ideal for print-style or manga-style soft shadow expressions."),
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
            bool useSDFMap = drawToggle("_SDF_MAP", "_UseSDFMap", L("SDF Shadow Mapを使用", "Use SDF Shadow Map"));

            if (useSDFMap)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("SDF Shadow Map設定", "SDF Shadow Map Settings"), EditorStyles.boldLabel);
                drawProperty("_SDFMap", L("SDFマップ", "SDF Map"));
                drawProperty("_SDFIntensity", L("SDF強度", "SDF Intensity"));
                drawProperty("_SDFSoftness", L("SDFソフトネス", "SDF Softness"));
                drawProperty("_SDFOffset", L("SDFオフセット", "SDF Offset"));
                drawHelpToggle("SDFShadowMap",
                    L("📍 SDF Shadow Map（距離場シャドウマップ）:\n" +
                      "テクスチャベースで影の位置を正確にコントロールできる高度な機能です。\n\n" +
                      "• SDFマップ: 白 = 明るい、黒 = 影\n" +
                      "• 強度: 影の強さ（0-1）\n" +
                      "• ソフトネス: 影の境界の柔らかさ\n" +
                      "• オフセット: 影の位置調整\n\n" +
                      "💡 使い方:\n" +
                      "顔の影を細かく制御したい場合や、特定の場所に常に影を落としたい場合に使用します。",
                      "📍 SDF Shadow Map (Signed Distance Field Shadow Map):\n" +
                      "An advanced feature for precise texture-based shadow position control.\n\n" +
                      "• SDF Map: White = bright, Black = shadow\n" +
                      "• Intensity: Shadow strength (0-1)\n" +
                      "• Softness: Shadow boundary softness\n" +
                      "• Offset: Shadow position adjustment\n\n" +
                      "💡 Usage:\n" +
                      "Use when you need fine control over face shadows or want shadows in specific locations."),
                    MessageType.Info);
            }

            // Face SDF Rotation (Genshin/AK:EF style)
            bool faceSDFRotation = drawToggle("_FACE_SDF_ROTATION", "_FaceSDFRotation", L("Face SDF回転追従を有効化", "Enable Face SDF Rotation Tracking"));
            if (faceSDFRotation)
            {
                EditorGUI.indentLevel++;
                drawProperty("_FaceForwardDirection", L("顔の正面方向", "Face Forward Direction"));
                drawProperty("_FaceRightDirection", L("顔の右方向", "Face Right Direction"));
                drawHelpToggle("FaceSDFRotation",
                    L("🔄 Face SDF回転追従:\n" +
                      "SDF影がライトの方向に追従して回転します。\n" +
                      "Genshin Impact / アークナイツ：エンドフィールド スタイルの\n" +
                      "顔影表現を実現します。\n\n" +
                      "• 正面方向: キャラの顔が向いている方向（オブジェクト空間）\n" +
                      "• 右方向: キャラの顔の右側の方向（オブジェクト空間）\n\n" +
                      "💡 使い方:\n" +
                      "顔のSDF影がライトの方向に応じて自動的に回転し、\n" +
                      "どの角度からでも自然な影表現を維持します。",
                      "🔄 Face SDF Rotation Tracking:\n" +
                      "SDF shadows rotate to follow the light direction.\n" +
                      "Achieves Genshin Impact / Arknights: Endfield style\n" +
                      "face shadow expressions.\n\n" +
                      "• Forward Direction: Direction the character's face is facing (object space)\n" +
                      "• Right Direction: Right side direction of the character's face (object space)\n\n" +
                      "💡 Usage:\n" +
                      "Face SDF shadows automatically rotate based on light direction,\n" +
                      "maintaining natural shadow expression from any angle."),
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
            bool useShadingGradeMap = drawToggle("_SHADING_GRADE_MAP", "_UseGradeMap", L("Shading Grade Mapを使用", "Use Shading Grade Map"));

            if (useShadingGradeMap)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("Shading Grade Map設定", "Shading Grade Map Settings"), EditorStyles.boldLabel);
                drawProperty("_ShadingGradeMap", "Shading Grade Map");
                drawProperty("_ShadingGradeScale", L("グレードスケール", "Grade Scale"));
                drawHelpToggle("ShadingGradeMap",
                    L("🎭 Shading Grade Map:\n" +
                      "影の濃さを部分的に調整できる機能です。\n\n" +
                      "• 白 = 明るく（影が薄くなる）\n" +
                      "• 黒 = 暗く（影が濃くなる）\n" +
                      "• グレー = 中間\n" +
                      "• スケール: -1（暗く）～ 0（変化なし）～ 1（明るく）\n\n" +
                      "💡 使い方:\n" +
                      "顔は明るく、服は暗くなど、部位ごとに影の濃さを変えたい場合に使用します。",
                      "🎭 Shading Grade Map:\n" +
                      "A feature to partially adjust shadow intensity.\n\n" +
                      "• White = Brighter (lighter shadows)\n" +
                      "• Black = Darker (deeper shadows)\n" +
                      "• Gray = Intermediate\n" +
                      "• Scale: -1 (darker) to 0 (no change) to 1 (brighter)\n\n" +
                      "💡 Usage:\n" +
                      "Use when you want different shadow intensities per area, e.g., bright face, dark clothing."),
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
            bool useShadowColorTex = drawToggle("_SHADOW_COLOR_TEX", "_UseShadowColorTex", L("影色テクスチャを使用", "Use Shadow Color Texture"));

            if (useShadowColorTex)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("Shadow Color Texture設定", "Shadow Color Texture Settings"), EditorStyles.boldLabel);
                drawProperty("_ShadowColorTex", "Shadow Color Texture");
                drawProperty("_ShadowColorTexStrength", L("適用強度", "Apply Strength"));
                drawHelpToggle("ShadowColorTexture",
                    L("🌈 Shadow Color Texture:\n" +
                      "影の色をテクスチャで指定できる高度な機能です。\n\n" +
                      "• テクスチャの色が影色として使用されます\n" +
                      "• 強度: テクスチャの影響度（0 = 使わない、1 = フル適用）\n\n" +
                      "💡 使い方:\n" +
                      "服の影を青っぽく、肌の影を赤っぽくなど、\n" +
                      "部位ごとに異なる影色を設定したい場合に使用します。",
                      "🌈 Shadow Color Texture:\n" +
                      "An advanced feature to specify shadow colors using a texture.\n\n" +
                      "• The texture colors are used as shadow colors\n" +
                      "• Strength: Texture influence (0 = not used, 1 = fully applied)\n\n" +
                      "💡 Usage:\n" +
                      "Use when you want different shadow colors per area,\n" +
                      "e.g., bluish shadows for clothing, reddish shadows for skin."),
                    MessageType.Info);
            }
        }
    }
}
