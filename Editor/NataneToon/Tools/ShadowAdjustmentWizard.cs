using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    /// <summary>
    /// Shadow Adjustment Wizard
    /// シャドウ調整ウィザード
    /// Step-by-step configurator for multiple shadow parameters
    /// 複数のシャドウパラメータをステップバイステップで設定
    /// </summary>
    public class ShadowAdjustmentWizard : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;

        private enum WizardStep
        {
            SelectMaterial,
            BasicShadow,
            MultiToneShadow,
            ShadowMaps,
            Preview
        }

        private WizardStep currentStep = WizardStep.SelectMaterial;

        // Shadow presets
        private enum ShadowPreset
        {
            Custom,
            SharpAnime,
            SoftToon,
            Realistic,
            CellShaded,
            Gradient
        }

        private ShadowPreset selectedPreset = ShadowPreset.Custom;

        // Preview settings (reserved for future use)
        #pragma warning disable CS0414
        private float previewLightAngle = 45f;
        private bool autoRefreshPreview = true;
        #pragma warning restore CS0414

        [MenuItem("Tools/Natane/エフェクト Effects/シャドウ調整ウィザード Shadow Adjustment Wizard", false, 41)]
        public static void ShowWindow()
        {
            var window = GetWindow<ShadowAdjustmentWizard>(L("シャドウ調整ウィザード", "Shadow Adjustment Wizard"));
            window.minSize = new Vector2(550, 650);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawStepIndicator();
            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentStep)
            {
                case WizardStep.SelectMaterial:
                    DrawSelectMaterialStep();
                    break;
                case WizardStep.BasicShadow:
                    DrawBasicShadowStep();
                    break;
                case WizardStep.MultiToneShadow:
                    DrawMultiToneShadowStep();
                    break;
                case WizardStep.ShadowMaps:
                    DrawShadowMapsStep();
                    break;
                case WizardStep.Preview:
                    DrawPreviewStep();
                    break;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            DrawNavigationButtons();
        }

        private void DrawHeader()
        {
            NataneToonShaderGUIUtility.DrawToolHeader("シャドウ調整ウィザード", "Shadow Adjustment Wizard", "ShadowAdjustmentWizard");
            EditorGUILayout.HelpBox(
                L("複数のシャドウパラメータを簡単に設定", "Easy setup for multiple shadow parameters"),
                MessageType.Info);
        }

        private void DrawStepIndicator()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            string[] stepNames = new string[]
            {
                L("1. マテリアル", "1. Material"),
                L("2. 基本シャドウ", "2. Basic Shadow"),
                L("3. 多階調", "3. Multi-Tone"),
                L("4. マップ", "4. Maps"),
                L("5. プレビュー", "5. Preview")
            };

            for (int i = 0; i < stepNames.Length; i++)
            {
                WizardStep step = (WizardStep)i;
                GUI.backgroundColor = (currentStep == step) ? new Color(0.5f, 0.8f, 1f) : Color.white;

                if (GUILayout.Button(stepNames[i], GUILayout.Height(45)))
                {
                    if (targetMaterial != null || step == WizardStep.SelectMaterial)
                    {
                        currentStep = step;
                    }
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectMaterialStep()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ステップ1: マテリアル選択", "Step 1: Select Material"), EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            targetMaterial = (Material)EditorGUILayout.ObjectField(
                L("ターゲットマテリアル", "Target Material"),
                targetMaterial,
                typeof(Material),
                false);

            if (EditorGUI.EndChangeCheck() && targetMaterial != null)
            {
                // Auto-advance to next step
                currentStep = WizardStep.BasicShadow;
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button(L("選択中のマテリアルを使用", "Use Selected Material"), GUILayout.Height(30)))
            {
                if (Selection.activeObject is Material mat)
                {
                    targetMaterial = mat;
                    currentStep = WizardStep.BasicShadow;
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        L("エラー", "Error"),
                        L("マテリアルを選択してください", "Please select a material"),
                        "OK");
                }
            }

            if (targetMaterial != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"{L("選択中", "Selected")}: {targetMaterial.name}", MessageType.Info);

                if (!IsNataneToonShader(targetMaterial))
                {
                    EditorGUILayout.HelpBox(
                        L("警告: このマテリアルはNatane Toon Shaderを使用していません。一部のパラメータが利用できない可能性があります。",
                          "Warning: This material is not using Natane Toon Shader. Some parameters may not be available."),
                        MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBasicShadowStep()
        {
            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox(L("マテリアルを選択してください", "Please select a material first"), MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ステップ2: 基本シャドウ設定", "Step 2: Basic Shadow Settings"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Preset selector
            EditorGUILayout.LabelField(L("プリセット", "Presets"), EditorStyles.boldLabel);
            selectedPreset = (ShadowPreset)EditorGUILayout.EnumPopup(L("スタイル", "Style"), selectedPreset);

            if (selectedPreset != ShadowPreset.Custom)
            {
                if (GUILayout.Button(L("このプリセットを適用", "Apply This Preset"), GUILayout.Height(25)))
                {
                    ApplyShadowPreset(selectedPreset);
                }

                EditorGUILayout.Space(10);
                DrawSeparator();
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.LabelField(L("手動調整", "Manual Adjustment"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Toon Steps
            if (targetMaterial.HasProperty("_ToonSteps"))
            {
                EditorGUI.BeginChangeCheck();
                int toonSteps = EditorGUILayout.IntSlider(
                    L("トゥーン段階", "Toon Steps"),
                    (int)targetMaterial.GetFloat("_ToonSteps"),
                    1,
                    10);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Toon Steps");
                    targetMaterial.SetFloat("_ToonSteps", toonSteps);
                    EditorUtility.SetDirty(targetMaterial);
                    selectedPreset = ShadowPreset.Custom;
                }
            }

            // Toon Sharpness
            if (targetMaterial.HasProperty("_ToonSharpness"))
            {
                EditorGUI.BeginChangeCheck();
                float sharpness = EditorGUILayout.Slider(
                    L("境界シャープネス", "Sharpness"),
                    targetMaterial.GetFloat("_ToonSharpness"),
                    0f,
                    1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Sharpness");
                    targetMaterial.SetFloat("_ToonSharpness", sharpness);
                    EditorUtility.SetDirty(targetMaterial);
                    selectedPreset = ShadowPreset.Custom;
                }
            }

            // Shadow Color
            if (targetMaterial.HasProperty("_ShadowColor"))
            {
                EditorGUI.BeginChangeCheck();
                Color shadowColor = EditorGUILayout.ColorField(
                    L("影の色", "Shadow Color"),
                    targetMaterial.GetColor("_ShadowColor"));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Shadow Color");
                    targetMaterial.SetColor("_ShadowColor", shadowColor);
                    EditorUtility.SetDirty(targetMaterial);
                    selectedPreset = ShadowPreset.Custom;
                }
            }

            // Shadow Receive
            if (targetMaterial.HasProperty("_ShadowReceive"))
            {
                EditorGUI.BeginChangeCheck();
                float shadowReceive = EditorGUILayout.Slider(
                    L("影の受け取り", "Shadow Receive"),
                    targetMaterial.GetFloat("_ShadowReceive"),
                    0f,
                    1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Shadow Receive");
                    targetMaterial.SetFloat("_ShadowReceive", shadowReceive);
                    EditorUtility.SetDirty(targetMaterial);
                    selectedPreset = ShadowPreset.Custom;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMultiToneShadowStep()
        {
            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox(L("マテリアルを選択してください", "Please select a material first"), MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ステップ3: 多階調シャドウ", "Step 3: Multi-Tone Shadow"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                L("複数段階の影を設定します（1次影、2次影、3次影）",
                  "Configure multiple shadow levels (1st, 2nd, 3rd shadow)"),
                MessageType.Info);
            EditorGUILayout.Space(5);

            // Enable multi-tone shadows
            bool useMultiTone = targetMaterial.IsKeywordEnabled("_USE_MULTI_TONE_SHADOW");
            EditorGUI.BeginChangeCheck();
            useMultiTone = EditorGUILayout.Toggle(L("多階調シャドウを使用", "Use Multi-Tone"), useMultiTone);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetMaterial, "Toggle Multi-Tone Shadow");
                if (useMultiTone)
                    targetMaterial.EnableKeyword("_USE_MULTI_TONE_SHADOW");
                else
                    targetMaterial.DisableKeyword("_USE_MULTI_TONE_SHADOW");
                EditorUtility.SetDirty(targetMaterial);
            }

            if (useMultiTone)
            {
                EditorGUILayout.Space(10);

                // 1st Shadow
                EditorGUILayout.LabelField(L("1次影", "1st Shadow"), EditorStyles.boldLabel);
                if (targetMaterial.HasProperty("_1stShadowColor"))
                {
                    EditorGUI.BeginChangeCheck();
                    Color color1 = EditorGUILayout.ColorField(L("色", "Color"), targetMaterial.GetColor("_1stShadowColor"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetMaterial, "Change 1st Shadow Color");
                        targetMaterial.SetColor("_1stShadowColor", color1);
                        EditorUtility.SetDirty(targetMaterial);
                    }
                }

                if (targetMaterial.HasProperty("_1stShadowBorder"))
                {
                    EditorGUI.BeginChangeCheck();
                    float border1 = EditorGUILayout.Slider(L("境界", "Border"), targetMaterial.GetFloat("_1stShadowBorder"), 0f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetMaterial, "Change 1st Shadow Border");
                        targetMaterial.SetFloat("_1stShadowBorder", border1);
                        EditorUtility.SetDirty(targetMaterial);
                    }
                }

                EditorGUILayout.Space(5);

                // 2nd Shadow
                EditorGUILayout.LabelField(L("2次影", "2nd Shadow"), EditorStyles.boldLabel);
                if (targetMaterial.HasProperty("_2ndShadowColor"))
                {
                    EditorGUI.BeginChangeCheck();
                    Color color2 = EditorGUILayout.ColorField(L("色", "Color"), targetMaterial.GetColor("_2ndShadowColor"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetMaterial, "Change 2nd Shadow Color");
                        targetMaterial.SetColor("_2ndShadowColor", color2);
                        EditorUtility.SetDirty(targetMaterial);
                    }
                }

                if (targetMaterial.HasProperty("_2ndShadowBorder"))
                {
                    EditorGUI.BeginChangeCheck();
                    float border2 = EditorGUILayout.Slider(L("境界", "Border"), targetMaterial.GetFloat("_2ndShadowBorder"), 0f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetMaterial, "Change 2nd Shadow Border");
                        targetMaterial.SetFloat("_2ndShadowBorder", border2);
                        EditorUtility.SetDirty(targetMaterial);
                    }
                }

                EditorGUILayout.Space(5);

                // 3rd Shadow
                EditorGUILayout.LabelField(L("3次影", "3rd Shadow"), EditorStyles.boldLabel);
                if (targetMaterial.HasProperty("_3rdShadowColor"))
                {
                    EditorGUI.BeginChangeCheck();
                    Color color3 = EditorGUILayout.ColorField(L("色", "Color"), targetMaterial.GetColor("_3rdShadowColor"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetMaterial, "Change 3rd Shadow Color");
                        targetMaterial.SetColor("_3rdShadowColor", color3);
                        EditorUtility.SetDirty(targetMaterial);
                    }
                }

                if (targetMaterial.HasProperty("_3rdShadowBorder"))
                {
                    EditorGUI.BeginChangeCheck();
                    float border3 = EditorGUILayout.Slider(L("境界", "Border"), targetMaterial.GetFloat("_3rdShadowBorder"), 0f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetMaterial, "Change 3rd Shadow Border");
                        targetMaterial.SetFloat("_3rdShadowBorder", border3);
                        EditorUtility.SetDirty(targetMaterial);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawShadowMapsStep()
        {
            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox(L("マテリアルを選択してください", "Please select a material first"), MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ステップ4: シャドウマップ", "Step 4: Shadow Maps"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                L("テクスチャを使用して影を詳細に制御",
                  "Control shadows in detail using textures"),
                MessageType.Info);
            EditorGUILayout.Space(5);

            // Ramp Texture
            EditorGUILayout.LabelField(L("ランプテクスチャ", "Ramp Texture"), EditorStyles.boldLabel);
            if (targetMaterial.HasProperty("_RampTex"))
            {
                EditorGUI.BeginChangeCheck();
                Texture2D rampTex = (Texture2D)EditorGUILayout.ObjectField(
                    L("ランプ", "Ramp"),
                    targetMaterial.GetTexture("_RampTex"),
                    typeof(Texture2D),
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Ramp Texture");
                    targetMaterial.SetTexture("_RampTex", rampTex);
                    if (rampTex != null)
                        targetMaterial.EnableKeyword("_USE_RAMP");
                    else
                        targetMaterial.DisableKeyword("_USE_RAMP");
                    EditorUtility.SetDirty(targetMaterial);
                }

                if (GUILayout.Button(L("ランプテクスチャを自動生成", "Auto-Generate Ramp"), GUILayout.Height(25)))
                {
                    GenerateRampTexture();
                }
            }

            EditorGUILayout.Space(10);

            // Shading Grade Map
            EditorGUILayout.LabelField(L("シェーディンググレードマップ", "Shading Grade Map"), EditorStyles.boldLabel);
            if (targetMaterial.HasProperty("_ShadingGradeMap"))
            {
                EditorGUI.BeginChangeCheck();
                Texture2D gradeMap = (Texture2D)EditorGUILayout.ObjectField(
                    L("グレードマップ", "Grade Map"),
                    targetMaterial.GetTexture("_ShadingGradeMap"),
                    typeof(Texture2D),
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Grade Map");
                    targetMaterial.SetTexture("_ShadingGradeMap", gradeMap);
                    EditorUtility.SetDirty(targetMaterial);
                }

                EditorGUILayout.HelpBox(
                    L("白=明るく、黒=暗く影を調整",
                      "White=brighter, Black=darker shadows"),
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // AO Map
            EditorGUILayout.LabelField(L("アンビエントオクルージョン", "Ambient Occlusion"), EditorStyles.boldLabel);
            if (targetMaterial.HasProperty("_OcclusionMap"))
            {
                EditorGUI.BeginChangeCheck();
                Texture2D aoMap = (Texture2D)EditorGUILayout.ObjectField(
                    L("AOマップ", "AO Map"),
                    targetMaterial.GetTexture("_OcclusionMap"),
                    typeof(Texture2D),
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change AO Map");
                    targetMaterial.SetTexture("_OcclusionMap", aoMap);
                    EditorUtility.SetDirty(targetMaterial);
                }

                if (targetMaterial.HasProperty("_OcclusionStrength"))
                {
                    EditorGUI.BeginChangeCheck();
                    float aoStrength = EditorGUILayout.Slider(
                        L("強度", "Strength"),
                        targetMaterial.GetFloat("_OcclusionStrength"),
                        0f,
                        1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetMaterial, "Change AO Strength");
                        targetMaterial.SetFloat("_OcclusionStrength", aoStrength);
                        EditorUtility.SetDirty(targetMaterial);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewStep()
        {
            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox(L("マテリアルを選択してください", "Please select a material first"), MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ステップ5: プレビュー", "Step 5: Preview"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                L("シャドウ設定のプレビュー",
                  "Preview shadow settings"),
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Summary
            EditorGUILayout.LabelField(L("設定サマリー", "Settings Summary"), EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_ToonSteps"))
            {
                EditorGUILayout.LabelField($"{L("トゥーン段階", "Toon Steps")}: {targetMaterial.GetFloat("_ToonSteps")}");
            }

            if (targetMaterial.HasProperty("_ToonSharpness"))
            {
                EditorGUILayout.LabelField($"{L("シャープネス", "Sharpness")}: {targetMaterial.GetFloat("_ToonSharpness"):F2}");
            }

            if (targetMaterial.HasProperty("_ShadowColor"))
            {
                Color shadowColor = targetMaterial.GetColor("_ShadowColor");
                EditorGUILayout.LabelField($"{L("影の色", "Shadow Color")}: RGB({shadowColor.r:F2}, {shadowColor.g:F2}, {shadowColor.b:F2})");
            }

            EditorGUILayout.Space(10);

            // Quick actions
            if (GUILayout.Button(L("設定をリセット", "Reset Settings"), GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    L("設定をリセット", "Reset Settings"),
                    L("シャドウ設定をデフォルトに戻しますか？", "Reset shadow settings to default?"),
                    L("はい", "Yes"),
                    L("いいえ", "No")))
                {
                    ResetShadowSettings();
                }
            }

            if (GUILayout.Button(L("マテリアルをpingして選択", "Ping Material"), GUILayout.Height(25)))
            {
                EditorGUIUtility.PingObject(targetMaterial);
                Selection.activeObject = targetMaterial;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawNavigationButtons()
        {
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(currentStep == WizardStep.SelectMaterial))
            {
                if (GUILayout.Button(L("← 前へ", "← Previous"), GUILayout.Height(30)))
                {
                    currentStep = (WizardStep)((int)currentStep - 1);
                }
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(currentStep == WizardStep.Preview || targetMaterial == null))
            {
                if (GUILayout.Button(L("次へ →", "Next →"), GUILayout.Height(30)))
                {
                    currentStep = (WizardStep)((int)currentStep + 1);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ApplyShadowPreset(ShadowPreset preset)
        {
            Undo.RecordObject(targetMaterial, "Apply Shadow Preset");

            switch (preset)
            {
                case ShadowPreset.SharpAnime:
                    if (targetMaterial.HasProperty("_ToonSteps"))
                        targetMaterial.SetFloat("_ToonSteps", 2);
                    if (targetMaterial.HasProperty("_ToonSharpness"))
                        targetMaterial.SetFloat("_ToonSharpness", 1f);
                    if (targetMaterial.HasProperty("_ShadowColor"))
                        targetMaterial.SetColor("_ShadowColor", new Color(0.5f, 0.5f, 0.6f));
                    if (targetMaterial.HasProperty("_ShadowReceive"))
                        targetMaterial.SetFloat("_ShadowReceive", 1f);
                    break;

                case ShadowPreset.SoftToon:
                    if (targetMaterial.HasProperty("_ToonSteps"))
                        targetMaterial.SetFloat("_ToonSteps", 3);
                    if (targetMaterial.HasProperty("_ToonSharpness"))
                        targetMaterial.SetFloat("_ToonSharpness", 0.3f);
                    if (targetMaterial.HasProperty("_ShadowColor"))
                        targetMaterial.SetColor("_ShadowColor", new Color(0.6f, 0.6f, 0.65f));
                    if (targetMaterial.HasProperty("_ShadowReceive"))
                        targetMaterial.SetFloat("_ShadowReceive", 0.8f);
                    break;

                case ShadowPreset.Realistic:
                    if (targetMaterial.HasProperty("_ToonSteps"))
                        targetMaterial.SetFloat("_ToonSteps", 5);
                    if (targetMaterial.HasProperty("_ToonSharpness"))
                        targetMaterial.SetFloat("_ToonSharpness", 0.1f);
                    if (targetMaterial.HasProperty("_ShadowColor"))
                        targetMaterial.SetColor("_ShadowColor", new Color(0.7f, 0.7f, 0.75f));
                    if (targetMaterial.HasProperty("_ShadowReceive"))
                        targetMaterial.SetFloat("_ShadowReceive", 1f);
                    break;

                case ShadowPreset.CellShaded:
                    if (targetMaterial.HasProperty("_ToonSteps"))
                        targetMaterial.SetFloat("_ToonSteps", 1);
                    if (targetMaterial.HasProperty("_ToonSharpness"))
                        targetMaterial.SetFloat("_ToonSharpness", 1f);
                    if (targetMaterial.HasProperty("_ShadowColor"))
                        targetMaterial.SetColor("_ShadowColor", new Color(0.4f, 0.4f, 0.5f));
                    if (targetMaterial.HasProperty("_ShadowReceive"))
                        targetMaterial.SetFloat("_ShadowReceive", 1f);
                    break;

                case ShadowPreset.Gradient:
                    if (targetMaterial.HasProperty("_ToonSteps"))
                        targetMaterial.SetFloat("_ToonSteps", 10);
                    if (targetMaterial.HasProperty("_ToonSharpness"))
                        targetMaterial.SetFloat("_ToonSharpness", 0f);
                    if (targetMaterial.HasProperty("_ShadowColor"))
                        targetMaterial.SetColor("_ShadowColor", new Color(0.65f, 0.65f, 0.7f));
                    if (targetMaterial.HasProperty("_ShadowReceive"))
                        targetMaterial.SetFloat("_ShadowReceive", 0.9f);
                    break;
            }

            EditorUtility.SetDirty(targetMaterial);

            EditorUtility.DisplayDialog(
                L("プリセット適用", "Preset Applied"),
                L($"{preset}プリセットを適用しました", $"Applied {preset} preset"),
                "OK");
        }

        private void GenerateRampTexture()
        {
            // Create a simple gradient ramp texture
            string path = EditorUtility.SaveFilePanelInProject(
                L("ランプテクスチャを保存", "Save Ramp Texture"),
                "RampTexture",
                "png",
                L("保存場所を選択", "Choose save location"));

            if (string.IsNullOrEmpty(path)) return;

            int toonSteps = 2;
            if (targetMaterial.HasProperty("_ToonSteps"))
                toonSteps = (int)targetMaterial.GetFloat("_ToonSteps");

            Texture2D ramp = new Texture2D(256, 16, TextureFormat.RGB24, false);
            ramp.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float t = x / 255f;
                    float stepped = Mathf.Floor(t * toonSteps) / toonSteps;
                    Color color = Color.Lerp(Color.black, Color.white, stepped);
                    ramp.SetPixel(x, y, color);
                }
            }

            ramp.Apply();

            byte[] bytes = ramp.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            Texture2D savedRamp = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            targetMaterial.SetTexture("_RampTex", savedRamp);
            targetMaterial.EnableKeyword("_USE_RAMP");
            EditorUtility.SetDirty(targetMaterial);

            EditorUtility.DisplayDialog(
                L("成功", "Success"),
                L($"ランプテクスチャを生成しました: {path}", $"Generated ramp texture: {path}"),
                "OK");
        }

        private void ResetShadowSettings()
        {
            Undo.RecordObject(targetMaterial, "Reset Shadow Settings");

            if (targetMaterial.HasProperty("_ToonSteps"))
                targetMaterial.SetFloat("_ToonSteps", 2);
            if (targetMaterial.HasProperty("_ToonSharpness"))
                targetMaterial.SetFloat("_ToonSharpness", 0.5f);
            if (targetMaterial.HasProperty("_ShadowColor"))
                targetMaterial.SetColor("_ShadowColor", new Color(0.5f, 0.5f, 0.5f));
            if (targetMaterial.HasProperty("_ShadowReceive"))
                targetMaterial.SetFloat("_ShadowReceive", 1f);

            EditorUtility.SetDirty(targetMaterial);
        }

        private bool IsNataneToonShader(Material mat)
        {
            if (mat == null || mat.shader == null) return false;
            return mat.shader.name.Contains("Natane") && mat.shader.name.Contains("Toon");
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space(5);
        }
    }
}
