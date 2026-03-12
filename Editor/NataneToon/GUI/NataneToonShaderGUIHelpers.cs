using UnityEditor;
using UnityEngine;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    /// <summary>
    /// Helper methods for NataneToonShaderGUI.
    /// </summary>
    public static class NataneToonShaderGUIHelpers
    {
        private const float GRADIENT_MODE_THRESHOLD = 0.5f;
        private const float STANDARD_TOON_MODE_THRESHOLD = 1.5f;
        private const float PBR_LIKE_MODE_THRESHOLD = 2.5f;

        public delegate bool DrawToggleDelegate(string keyword, string propertyName, string label);
        public delegate void DrawPropertyDelegate(string propertyName, string label);
        public delegate bool DrawHelpToggleDelegate(string sectionKey, string helpText, MessageType messageType = MessageType.Info);
        public delegate void SaveFoldoutStatesDelegate();
        public delegate MaterialProperty FindPropertyDelegate(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory);

        public static void DrawShadingSection(
            ref bool showShading,
            MaterialProperty[] properties,
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            SaveFoldoutStatesDelegate saveFoldoutStates,
            FindPropertyDelegate findProperty)
        {
            _ = showShading;
            _ = saveFoldoutStates;
            DrawShadingSectionContent(properties, drawToggle, drawProperty, drawHelpToggle, findProperty);
        }

        public static void DrawShadingSectionContent(
            MaterialProperty[] properties,
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            FindPropertyDelegate findProperty,
            System.Func<string, bool> isSectionAvailable = null)
        {
            drawHelpToggle(
                "ShadingSection",
                L("Anime-style cel shading with clean and controllable shadow transitions.",
                  "Anime-style cel shading with clean and controllable shadow transitions."),
                MessageType.None);

            EditorGUILayout.Space(5);
            DrawShadingModeControls(properties, drawToggle, drawProperty, drawHelpToggle, findProperty);

            EditorGUILayout.Space(10);
            DrawShadowReceiveMaskControls(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space(10);
            drawHelpToggle(
                "LightShadowTabMigration",
                L("AO and dithering settings have been moved to the Light & Shadow tab.",
                  "AO and dithering settings have been moved to the Light & Shadow tab."),
                MessageType.None);

            if (isSectionAvailable == null || isSectionAvailable("SDFMap"))
            {
                EditorGUILayout.Space(10);
                DrawSDFShadowMapControls(drawToggle, drawProperty, drawHelpToggle);
            }

            EditorGUILayout.Space(10);
            DrawShadingGradeMapControls(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space(10);
            DrawShadowColorTextureControls(drawToggle, drawProperty, drawHelpToggle);
        }

        public static void DrawShadingModeControls(
            MaterialProperty[] properties,
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            FindPropertyDelegate findProperty)
        {
            drawHelpToggle(
                "ShadowSoftnessGuide",
                L("To soften shadows, start with Shadow Blend around 0.3 to 0.5, then adjust Lit Softness if needed.",
                  "To soften shadows, start with Shadow Blend around 0.3 to 0.5, then adjust Lit Softness if needed."),
                MessageType.None);

            bool useRamp = drawToggle("_USE_RAMP", "_UseRamp", L("Use Ramp Texture", "Use Ramp Texture"));
            if (useRamp)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("Ramp Texture Settings", "Ramp Texture Settings"), EditorStyles.boldLabel);
                drawProperty("_RampTex", L("Ramp Texture", "Ramp Texture"));
                drawHelpToggle(
                    "RampTexture",
                    L("The ramp texture should run from dark on the left to bright on the right.",
                      "The ramp texture should run from dark on the left to bright on the right."),
                    MessageType.Info);
            }
            else
            {
                MaterialProperty shadingModeProp = findProperty("_ShadingMode", properties, false);
                MaterialProperty lilToonCompatibilityProp = findProperty("_LilToonExactCompatibility", properties, false);
                float shadingModeValue = shadingModeProp != null ? shadingModeProp.floatValue : 0f;
                bool usesLilToonCompatibilityBase = lilToonCompatibilityProp != null &&
                                                    !lilToonCompatibilityProp.hasMixedValue &&
                                                    lilToonCompatibilityProp.floatValue > 0.5f;
                bool isStandardToon = usesLilToonCompatibilityBase && IsStandardToonMode(shadingModeValue);
                bool isGradientMode = IsGradientMode(shadingModeValue);
                bool isPbrLikeMode = IsPbrLikeMode(shadingModeValue);

                EditorGUILayout.Space(5);

                if (!isStandardToon)
                {
                    EditorGUILayout.LabelField(L("見た目の方向性", "Shading Style"), EditorStyles.boldLabel);
                    if (shadingModeProp != null)
                    {
                        DrawUserFacingShadingModePopup(shadingModeProp);
                    }
                    else
                    {
                        drawProperty("_ShadingMode", L("モード", "Mode"));
                    }

                    drawHelpToggle(
                        "ShadingMode",
                        L("Toon はくっきりしたアニメ調、Gradient はやわらかい陰影、PBR-Like は立体感を少し強めた見た目です。lilToon 近似や移行マテリアルでは、必要に応じて内部の互換ベースが自動で使われます。",
                          "Toon gives stepped cel shading. Gradient gives softer transitions. PBR-Like adds a bit more volume. lilToon Match and migrated materials automatically use the internal compatibility base when needed."),
                        MessageType.None);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        L("lilToon互換ベース", "lilToon Compatibility Base"),
                        EditorStyles.boldLabel);
                    drawHelpToggle(
                        "StandardToonModeNotice",
                        L("このマテリアルは lilToon 近似または移行用の互換ベースを使用しています。通常はこのまま編集して問題ありません。",
                          "This material is using the lilToon compatibility base. In most cases you should keep this base active while editing."),
                        MessageType.Warning);
                }

                EditorGUILayout.Space(5);
                if (isStandardToon)
                {
                    DrawStandardToonSettings(drawProperty, drawHelpToggle, drawToggle);
                }
                else if (isGradientMode)
                {
                    DrawGradientModeSettings(drawProperty, drawHelpToggle);
                }
                else if (isPbrLikeMode)
                {
                    drawHelpToggle(
                        "PbrLikeMode",
                        L("PBR-Like はトゥーン感を残しつつ立体感を強める方向です。影の調整は通常のトゥーン設定で行えます。",
                          "PBR-Like keeps the toon workflow but pushes the lighting toward stronger volume. Use the regular toon controls below to tune shadows."),
                        MessageType.None);
                    DrawToonModeSettings(drawProperty, drawHelpToggle, drawToggle);
                }
                else
                {
                    DrawToonModeSettings(drawProperty, drawHelpToggle, drawToggle);
                }
            }

            EditorGUILayout.Space(5);
            drawProperty("_ShadowHueShift", L("Shadow Hue Shift", "Shadow Hue Shift"));
            drawProperty("_ShadowSaturation", L("Shadow Saturation", "Shadow Saturation"));
            drawHelpToggle(
                "ShadowHSVShift",
                L("Adjust shadow hue and saturation in HSV space.",
                  "Adjust shadow hue and saturation in HSV space."),
                MessageType.Info);

            EditorGUILayout.Space(5);
            drawProperty("_ShadowOffset", L("Shadow Offset", "Shadow Offset"));
            drawHelpToggle(
                "ShadowOffset",
                L("Positive values brighten the shadow boundary. Negative values deepen it.",
                  "Positive values brighten the shadow boundary. Negative values deepen it."),
                MessageType.Info);

            drawProperty("_WrapAmount", L("Wrap Amount", "Wrap Amount"));
            drawHelpToggle(
                "WrapAmount",
                L("Controls how much light wraps around the form.",
                  "Controls how much light wraps around the form."),
                MessageType.Info);

            EditorGUILayout.Space(5);
            drawProperty("_LitSoftness", L("Lit Softness", "Lit Softness"));
            drawHelpToggle(
                "LitSoftness",
                L("Softens transitions on lit areas.",
                  "Softens transitions on lit areas."),
                MessageType.Info);

            EditorGUILayout.Space(5);
            DrawBlendParameter(
                "_ShadowBlend",
                "ShadowBlend",
                L("Shadow Blend", "Shadow Blend"),
                L("Higher values soften and widen the shadow transition.",
                  "Higher values soften and widen the shadow transition."),
                drawProperty,
                drawHelpToggle);

            MaterialProperty shadowBlendProp = findProperty("_ShadowBlend", properties, false);
            if (shadowBlendProp != null && shadowBlendProp.floatValue > 0.7f)
            {
                drawHelpToggle(
                    "ShadowBlendHigh",
                    L("Shadow Blend is set high. Check other softness values if the result feels too soft.",
                      "Shadow Blend is set high. Check other softness values if the result feels too soft."),
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);
            bool vertexColorShadow = drawToggle(
                "_VERTEX_COLOR_SHADOW",
                "_VertexColorShadow",
                L("Vertex Color Shadow Threshold", "Vertex Color Shadow Threshold"));
            if (vertexColorShadow)
            {
                EditorGUI.indentLevel++;
                drawProperty("_VCShadowThreshold", L("Shadow Threshold", "Shadow Threshold"));
                drawProperty("_VCShadowPush", L("Shadow Push", "Shadow Push"));
                drawHelpToggle(
                    "VertexColorShadow",
                    L("Uses vertex color R to offset the shadow threshold per area.",
                      "Uses vertex color R to offset the shadow threshold per area."),
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }

        public static void DrawBlendParameter(
            string propertyName,
            string sectionKey,
            string label,
            string helpText,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            drawProperty(propertyName, label);
            drawHelpToggle(sectionKey, helpText, MessageType.Info);
        }

        public static void DrawGradientModeSettings(
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            EditorGUILayout.LabelField(L("Gradient Settings", "Gradient Settings"), EditorStyles.boldLabel);
            drawProperty("_ShadowColor", L("Shadow Color", "Shadow Color"));
            drawProperty("_ShadingGradientWidth", L("Gradient Width", "Gradient Width"));
            drawHelpToggle(
                "ShadingGradientWidth",
                L("Controls how soft the light-to-shadow transition is.",
                  "Controls how soft the light-to-shadow transition is."),
                MessageType.Info);
        }

        public static void DrawStandardToonSettings(
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            DrawToggleDelegate drawToggle)
        {
            EditorGUILayout.LabelField(L("lilToon互換ベース設定", "lilToon Compatibility Settings"), EditorStyles.boldLabel);
            drawProperty("_STShadowBorder", L("Shadow Border", "Shadow Border"));
            drawProperty("_STShadowBlur", L("Shadow Blur", "Shadow Blur"));
            drawProperty("_STShadowStrength", L("Shadow Strength", "Shadow Strength"));
            drawProperty("_ShadowColor", L("Shadow Color (1st)", "Shadow Color (1st)"));

            EditorGUILayout.Space();
            DrawMultiToneShadowSettings(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space();
            drawProperty("_STAsUnlit", L("As Unlit", "As Unlit"));
            drawProperty("_STShadowEnvStrength", L("Shadow Env Strength", "Shadow Env Strength"));
            drawHelpToggle(
                "StandardToon",
                L("この設定群は lilToon 近似や移行マテリアル用の内部互換ベースです。通常の Natane 素材では無理に使う必要はありません。",
                  "These controls belong to the internal lilToon compatibility base used by migrated and lilToon Match materials."),
                MessageType.None);
        }

        private static bool IsGradientMode(float shadingModeValue)
        {
            return shadingModeValue >= GRADIENT_MODE_THRESHOLD &&
                   shadingModeValue < STANDARD_TOON_MODE_THRESHOLD;
        }

        private static bool IsStandardToonMode(float shadingModeValue)
        {
            return shadingModeValue >= STANDARD_TOON_MODE_THRESHOLD &&
                   shadingModeValue < PBR_LIKE_MODE_THRESHOLD;
        }

        private static bool IsPbrLikeMode(float shadingModeValue)
        {
            return shadingModeValue >= PBR_LIKE_MODE_THRESHOLD;
        }

        private static void DrawUserFacingShadingModePopup(MaterialProperty shadingModeProp)
        {
            bool previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = shadingModeProp.hasMixedValue;
            EditorGUI.BeginChangeCheck();

            int currentIndex = GetUserFacingShadingModeIndex(shadingModeProp.floatValue);
            int nextIndex = EditorGUILayout.Popup(
                L("モード", "Mode"),
                currentIndex,
                GetUserFacingShadingModeLabels());

            EditorGUI.showMixedValue = previousMixedValue;

            if (EditorGUI.EndChangeCheck())
            {
                shadingModeProp.floatValue = GetShadingModeValueFromUserFacingIndex(nextIndex);
            }
        }

        private static int GetUserFacingShadingModeIndex(float shadingModeValue)
        {
            if (IsPbrLikeMode(shadingModeValue))
            {
                return 2;
            }

            if (IsGradientMode(shadingModeValue))
            {
                return 1;
            }

            return 0;
        }

        private static float GetShadingModeValueFromUserFacingIndex(int index)
        {
            switch (index)
            {
                case 1:
                    return 1f;
                case 2:
                    return 3f;
                default:
                    return 0f;
            }
        }

        private static string[] GetUserFacingShadingModeLabels()
        {
            return new[]
            {
                L("トゥーン", "Toon"),
                L("グラデーション", "Gradient"),
                L("PBRライク", "PBR-Like")
            };
        }

        public static void DrawMultiToneShadowSettings(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useMultiShadow = drawToggle(
                "_USE_MULTI_SHADOW",
                "_UseMultiShadow",
                L("Use Multi-Tone Shadow", "Use Multi-Tone Shadow"));
            if (!useMultiShadow)
            {
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("Multi-Tone Shadow Settings", "Multi-Tone Shadow Settings"), EditorStyles.boldLabel);

            drawProperty("_Shadow2ndColor", L("Shadow Color (2nd)", "Shadow Color (2nd)"));
            drawProperty("_Shadow2ndBorder", L("2nd Border", "2nd Border"));
            drawHelpToggle(
                "Shadow2ndBorder",
                L("Sets where the second shadow tone begins.",
                  "Sets where the second shadow tone begins."),
                MessageType.None);

            EditorGUILayout.Space();
            drawProperty("_Shadow3rdColor", L("Shadow Color (3rd)", "Shadow Color (3rd)"));
            drawProperty("_Shadow3rdBorder", L("3rd Border", "3rd Border"));
            drawHelpToggle(
                "Shadow3rdBorder",
                L("Sets where the deepest shadow tone begins.",
                  "Sets where the deepest shadow tone begins."),
                MessageType.None);

            EditorGUILayout.Space();
            drawHelpToggle(
                "MultiShadowUsage",
                L("Use descending border values: 1st > 2nd > 3rd.",
                  "Use descending border values: 1st > 2nd > 3rd."),
                MessageType.Info);
        }

        public static void DrawToonModeSettings(
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle,
            DrawToggleDelegate drawToggle)
        {
            EditorGUILayout.LabelField(L("Cel Shading Settings", "Cel Shading Settings"), EditorStyles.boldLabel);
            drawProperty("_ShadowColor", L("Shadow Color (1st)", "Shadow Color (1st)"));

            EditorGUILayout.Space();
            DrawMultiToneShadowSettings(drawToggle, drawProperty, drawHelpToggle);

            EditorGUILayout.Space();
            drawProperty("_ShadowSteps", L("Shadow Steps", "Shadow Steps"));
            drawHelpToggle(
                "ShadowSteps",
                L("Recommended values are usually 2 to 3 steps.",
                  "Recommended values are usually 2 to 3 steps."),
                MessageType.Info);

            drawProperty("_ShadowSharpness", L("Shadow Sharpness", "Shadow Sharpness"));
            drawHelpToggle(
                "ShadowSharpness",
                L("Lower values sharpen the border. Higher values soften it.",
                  "Lower values sharpen the border. Higher values soften it."),
                MessageType.Info);

            drawProperty("_StepBorderSmooth", L("Step Border Smoothing", "Step Border Smoothing"));
            drawHelpToggle(
                "StepBorderSmooth",
                L("Smooths the borders between multi-tone shadow steps.",
                  "Smooths the borders between multi-tone shadow steps."),
                MessageType.Info);
        }

        public static void DrawShadowReceiveMaskControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useShadowReceiveMask = drawToggle(
                "_SHADOW_RECEIVE_MASK",
                "_UseShadowReceiveMask",
                L("Use Shadow Receive Mask", "Use Shadow Receive Mask"));
            if (!useShadowReceiveMask)
            {
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("Shadow Receive Mask Settings", "Shadow Receive Mask Settings"), EditorStyles.boldLabel);
            drawProperty("_ShadowReceiveMask", L("Shadow Receive Mask", "Shadow Receive Mask"));
            drawHelpToggle(
                "ShadowReceiveMask",
                L("White areas receive less shadow. Black areas receive full shadow.",
                  "White areas receive less shadow. Black areas receive full shadow."),
                MessageType.Info);
        }

        public static void DrawAmbientOcclusionControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useAO = drawToggle("_USE_AO", "_UseAO", L("Use Ambient Occlusion (AO)", "Use Ambient Occlusion (AO)"));
            if (!useAO)
            {
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("AO Settings", "AO Settings"), EditorStyles.boldLabel);
            drawProperty("_AOMap", L("AO Map", "AO Map"));
            drawProperty("_AOIntensity", L("AO Intensity", "AO Intensity"));
            drawHelpToggle(
                "AmbientOcclusion",
                L("Darkens creases and recessed areas using an AO map.",
                  "Darkens creases and recessed areas using an AO map."),
                MessageType.Info);
        }

        public static void DrawDitheringControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useDithering = drawToggle("_USE_DITHERING", "_UseDithering", L("Use Dithering (Halftone)", "Use Dithering (Halftone)"));
            if (!useDithering)
            {
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("Dithering Settings", "Dithering Settings"), EditorStyles.boldLabel);
            drawProperty("_DitheringScale", L("Dithering Scale", "Dithering Scale"));
            drawProperty("_DitheringStrength", L("Dithering Strength", "Dithering Strength"));
            drawHelpToggle(
                "Dithering",
                L("Adds a halftone-like pattern to shadow transitions.",
                  "Adds a halftone-like pattern to shadow transitions."),
                MessageType.Info);
        }

        public static void DrawSDFShadowMapControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useSDFMap = drawToggle("_SDF_MAP", "_UseSDFMap", L("Use SDF Shadow Map", "Use SDF Shadow Map"));
            if (useSDFMap)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("SDF Shadow Map Settings", "SDF Shadow Map Settings"), EditorStyles.boldLabel);
                drawProperty("_SDFMap", L("SDF Map", "SDF Map"));
                drawProperty("_SDFIntensity", L("SDF Intensity", "SDF Intensity"));
                drawProperty("_SDFSoftness", L("SDF Softness", "SDF Softness"));
                drawProperty("_SDFOffset", L("SDF Offset", "SDF Offset"));
                drawHelpToggle(
                    "SDFShadowMap",
                    L("Controls shadow placement using an SDF texture.",
                      "Controls shadow placement using an SDF texture."),
                    MessageType.Info);
            }

            bool faceSDFRotation = drawToggle(
                "_FACE_SDF_ROTATION",
                "_FaceSDFRotation",
                L("Enable Face SDF Rotation Tracking", "Enable Face SDF Rotation Tracking"));
            if (!faceSDFRotation)
            {
                return;
            }

            EditorGUI.indentLevel++;
            drawProperty("_FaceForwardDirection", L("Face Forward Direction", "Face Forward Direction"));
            drawProperty("_FaceRightDirection", L("Face Right Direction", "Face Right Direction"));
            drawHelpToggle(
                "FaceSDFRotation",
                L("Lets face SDF shadows rotate with light direction.",
                  "Lets face SDF shadows rotate with light direction."),
                MessageType.Info);
            EditorGUI.indentLevel--;
        }

        public static void DrawShadingGradeMapControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useShadingGradeMap = drawToggle(
                "_SHADING_GRADE_MAP",
                "_UseGradeMap",
                L("Use Shading Grade Map", "Use Shading Grade Map"));
            if (!useShadingGradeMap)
            {
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("Shading Grade Map Settings", "Shading Grade Map Settings"), EditorStyles.boldLabel);
            drawProperty("_ShadingGradeMap", "Shading Grade Map");
            drawProperty("_ShadingGradeScale", L("Grade Scale", "Grade Scale"));
            drawHelpToggle(
                "ShadingGradeMap",
                L("Adjusts shadow strength locally using a texture map.",
                  "Adjusts shadow strength locally using a texture map."),
                MessageType.Info);
        }

        public static void DrawPCSSControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool usePCSS = drawToggle("_PCSS", "_UsePCSS", L("Use PCSS Soft Shadows", "Use PCSS Soft Shadows"));
            if (!usePCSS)
            {
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("PCSS Settings", "PCSS Settings"), EditorStyles.boldLabel);
            drawProperty("_PCSSLightSize", L("Light Size", "Light Size"));
            drawProperty("_PCSSSoftness", L("Softness", "Softness"));
            drawProperty("_PCSSBlockerSearchRadius", L("Blocker Search Radius", "Blocker Search Radius"));
            drawProperty("_PCSSMinFilterRadius", L("Min Filter Radius", "Min Filter Radius"));
            drawProperty("_PCSSMaxFilterRadius", L("Max Filter Radius", "Max Filter Radius"));
            drawProperty("_PCSSSampleCount", L("Sample Quality", "Sample Quality"));

            EditorGUILayout.Space(5);
            drawProperty("_PCSSBlendMode", L("Blend Mode", "Blend Mode"));
            drawProperty("_PCSSBlend", L("Blend", "Blend"));
            drawProperty("_PCSSBlur", L("Blur", "Blur"));
            drawHelpToggle(
                "PCSS",
                L("PCSS adds softer, distance-aware shadows for directional lights.",
                  "PCSS adds softer, distance-aware shadows for directional lights."),
                MessageType.Info);
        }

        public static void DrawShadowColorTextureControls(
            DrawToggleDelegate drawToggle,
            DrawPropertyDelegate drawProperty,
            DrawHelpToggleDelegate drawHelpToggle)
        {
            bool useShadowColorTex = drawToggle(
                "_SHADOW_COLOR_TEX",
                "_UseShadowColorTex",
                L("Use Shadow Color Texture", "Use Shadow Color Texture"));
            if (!useShadowColorTex)
            {
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("Shadow Color Texture Settings", "Shadow Color Texture Settings"), EditorStyles.boldLabel);
            drawProperty("_ShadowColorTex", "Shadow Color Texture");
            drawProperty("_ShadowColorTexStrength", L("Apply Strength", "Apply Strength"));
            drawHelpToggle(
                "ShadowColorTexture",
                L("Uses a texture to control shadow colors. Apply Strength controls how strongly the texture affects the result.",
                  "Uses a texture to control shadow colors. Apply Strength controls how strongly the texture affects the result."),
                MessageType.Info);
        }
    }
}
