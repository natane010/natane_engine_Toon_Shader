using UnityEngine;
using UnityEditor;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    /// <summary>
    /// Custom ShaderGUI for AutoMat/PBRLit_Builtin.
    /// All PBR map generation controls live directly in the material inspector.
    /// </summary>
    public class AutoMatShaderGUI : ShaderGUI
    {
        // ===== State =====
        private AutoMatGenerationParams genParams = new AutoMatGenerationParams();
        private AutoMatGenerationResult lastResult;
        private int resolution = 1024;
        private int presetIndex = 0;
        private string saveFolderOverride = "";
        private bool showNormalParams = false;
        private bool showRoughnessParams = false;
        private bool showMetallicParams = false;
        private bool showHeightParams = false;
        private bool showAOParams = false;
        private bool showCurvatureParams = false;
        private bool showDebug = false;

        private static readonly string[] PresetNames = new[]
        {
            "Custom", "Stone", "Metal", "Wood", "Fabric", "Plastic"
        };

        private static readonly int[] ResolutionOptions = { 256, 512, 1024, 2048 };
        private static readonly string[] ResolutionLabels = { "256", "512", "1024", "2048" };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material mat = materialEditor.target as Material;
            if (mat == null) return;

            // ===== Source Texture =====
            EditorGUILayout.LabelField(L("ソーステクスチャ", "Source Texture"), EditorStyles.boldLabel);
            MaterialProperty baseMap = FindProperty("_BaseMap", properties, false);
            MaterialProperty baseColor = FindProperty("_BaseColor", properties, false);
            if (baseMap != null) materialEditor.TexturePropertySingleLine(
                new GUIContent(L("ベースマップ (Albedo)", "Base Map (Albedo)")), baseMap, baseColor);

            EditorGUILayout.Space(8);

            // ===== Auto Generation Section =====
            EditorGUILayout.LabelField(L("▶ PBRマップ自動生成", "▶ Auto PBR Map Generation"), EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Preset
                int newPreset = EditorGUILayout.Popup(L("プリセット", "Preset"), presetIndex, PresetNames);
                if (newPreset != presetIndex)
                {
                    presetIndex = newPreset;
                    ApplyPreset(presetIndex);
                }

                // Resolution
                int resIdx = System.Array.IndexOf(ResolutionOptions, resolution);
                if (resIdx < 0) resIdx = 2;
                resIdx = EditorGUILayout.Popup(L("解像度", "Resolution"), resIdx, ResolutionLabels);
                resolution = ResolutionOptions[resIdx];

                EditorGUILayout.Space(4);

                // === Normal Parameters ===
                showNormalParams = EditorGUILayout.Foldout(showNormalParams, L("Normal 設定", "Normal Settings"), true);
                if (showNormalParams)
                {
                    EditorGUI.indentLevel++;
                    genParams.normalStrength = EditorGUILayout.Slider(L("強度", "Strength"), genParams.normalStrength, 0f, 5f);
                    genParams.normalBlurRadius = EditorGUILayout.IntSlider(L("ブラー半径", "Blur Radius"), genParams.normalBlurRadius, 0, 10);
                    EditorGUI.indentLevel--;
                }

                // === Roughness Parameters ===
                showRoughnessParams = EditorGUILayout.Foldout(showRoughnessParams, L("Roughness 設定", "Roughness Settings"), true);
                if (showRoughnessParams)
                {
                    EditorGUI.indentLevel++;
                    genParams.roughnessBlockSize = EditorGUILayout.IntSlider(L("ブロックサイズ", "Block Size"), genParams.roughnessBlockSize, 2, 32);
                    genParams.roughnessRemapMin = EditorGUILayout.Slider(L("リマップ 最小", "Remap Min"), genParams.roughnessRemapMin, 0f, 0.1f);
                    genParams.roughnessRemapMax = EditorGUILayout.Slider(L("リマップ 最大", "Remap Max"), genParams.roughnessRemapMax, 0.01f, 0.5f);
                    genParams.roughnessSaturationWeight = EditorGUILayout.Slider(L("彩度補正", "Saturation Correction"), genParams.roughnessSaturationWeight, 0f, 1f);
                    EditorGUI.indentLevel--;
                }

                // === Metallic Parameters ===
                showMetallicParams = EditorGUILayout.Foldout(showMetallicParams, L("Metallic 設定", "Metallic Settings"), true);
                if (showMetallicParams)
                {
                    EditorGUI.indentLevel++;
                    genParams.metallicLuminanceThreshold = EditorGUILayout.Slider(L("輝度閾値", "Luminance Threshold"), genParams.metallicLuminanceThreshold, 0f, 1f);
                    genParams.metallicSaturationThreshold = EditorGUILayout.Slider(L("彩度閾値", "Saturation Threshold"), genParams.metallicSaturationThreshold, 0f, 1f);
                    genParams.metallicBias = EditorGUILayout.Slider(L("バイアス", "Bias"), genParams.metallicBias, -0.5f, 0.5f);
                    EditorGUI.indentLevel--;
                }

                // === Height Parameters ===
                showHeightParams = EditorGUILayout.Foldout(showHeightParams, L("Height 設定", "Height Settings"), true);
                if (showHeightParams)
                {
                    EditorGUI.indentLevel++;
                    genParams.heightContrast = EditorGUILayout.Slider(L("コントラスト", "Contrast"), genParams.heightContrast, 0.1f, 3f);
                    genParams.heightInvert = EditorGUILayout.Toggle(L("反転", "Invert"), genParams.heightInvert);
                    genParams.heightBlurRadius = EditorGUILayout.IntSlider(L("ブラー半径", "Blur Radius"), genParams.heightBlurRadius, 0, 10);
                    EditorGUI.indentLevel--;
                }

                // === AO Parameters ===
                showAOParams = EditorGUILayout.Foldout(showAOParams, L("AO 設定", "AO Settings"), true);
                if (showAOParams)
                {
                    EditorGUI.indentLevel++;
                    genParams.aoRadius = EditorGUILayout.IntSlider(L("半径", "Radius"), genParams.aoRadius, 1, 32);
                    genParams.aoStrength = EditorGUILayout.Slider(L("強度", "Strength"), genParams.aoStrength, 0f, 2f);
                    genParams.aoBias = EditorGUILayout.Slider(L("バイアス", "Bias"), genParams.aoBias, 0f, 0.1f);
                    EditorGUI.indentLevel--;
                }

                // === Curvature Parameters ===
                showCurvatureParams = EditorGUILayout.Foldout(showCurvatureParams, L("Curvature 設定 (オプション)", "Curvature Settings (Optional)"), true);
                if (showCurvatureParams)
                {
                    EditorGUI.indentLevel++;
                    genParams.enableCurvature = EditorGUILayout.Toggle(L("有効", "Enable"), genParams.enableCurvature);
                    if (genParams.enableCurvature)
                    {
                        genParams.curvatureScale = EditorGUILayout.Slider(L("スケール", "Scale"), genParams.curvatureScale, 0.1f, 5f);
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(8);

                // === Generate Button ===
                Texture2D sourceTex = baseMap?.textureValue as Texture2D;
                using (new EditorGUI.DisabledScope(sourceTex == null))
                {
                    var oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                    if (GUILayout.Button(L("▶ 全マップ生成", "▶ Generate All Maps"), GUILayout.Height(30)))
                    {
                        lastResult = AutoMatMapGenerator.GenerateAll(sourceTex, genParams, resolution);
                        if (lastResult != null)
                        {
                            Debug.Log($"[AutoMat] {L("生成完了", "Generation complete")}: {lastResult.processingTimeMs:F0}ms");
                        }
                    }
                    GUI.backgroundColor = oldBg;
                }

                if (sourceTex == null)
                {
                    EditorGUILayout.HelpBox(
                        L("Base Map にテクスチャを設定してください。", "Please assign a texture to Base Map."),
                        MessageType.Info);
                }

                // === Save Button ===
                if (lastResult != null)
                {
                    EditorGUILayout.Space(4);

                    // Determine save folder
                    string matPath = AssetDatabase.GetAssetPath(mat);
                    string defaultFolder = string.IsNullOrEmpty(matPath)
                        ? "Assets/AutoMatGenerated"
                        : System.IO.Path.GetDirectoryName(matPath).Replace("\\", "/");
                    if (string.IsNullOrEmpty(saveFolderOverride))
                        saveFolderOverride = defaultFolder;

                    saveFolderOverride = EditorGUILayout.TextField(L("保存先", "Save Folder"), saveFolderOverride);

                    EditorGUILayout.BeginHorizontal();
                    var oldBg2 = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
                    if (GUILayout.Button(L("💾 保存してマテリアルに適用", "💾 Save & Apply to Material"), GUILayout.Height(25)))
                    {
                        string baseName = mat.name;
                        var saved = AutoMatTextureExporter.SaveAll(lastResult, sourceTex, saveFolderOverride, baseName);
                        if (saved != null)
                        {
                            AutoMatTextureExporter.ApplyToMaterial(mat, saved);
                            Debug.Log($"[AutoMat] {L("保存完了", "Saved")} → {saveFolderOverride}");
                        }
                    }
                    GUI.backgroundColor = oldBg2;
                    EditorGUILayout.EndHorizontal();

                    // Show processing time
                    EditorGUILayout.LabelField(
                        $"{L("処理時間", "Processing Time")}: {lastResult.processingTimeMs:F0}ms",
                        EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(12);

            // ===== Standard PBR Properties =====
            EditorGUILayout.LabelField(L("PBR マップ", "PBR Maps"), EditorStyles.boldLabel);

            DrawStandardPBRProperties(materialEditor, properties);

            EditorGUILayout.Space(8);

            // ===== Debug Section =====
            showDebug = EditorGUILayout.Foldout(showDebug, L("デバッグ表示", "Debug Display"), true);
            if (showDebug)
            {
                DrawToggleKeyword(materialEditor, properties, "_DebugAlbedo", "_DEBUG_ALBEDO", "Albedo");
                DrawToggleKeyword(materialEditor, properties, "_DebugNormal", "_DEBUG_NORMAL", "Normal");
                DrawToggleKeyword(materialEditor, properties, "_DebugRoughness", "_DEBUG_ROUGHNESS", "Roughness");
                DrawToggleKeyword(materialEditor, properties, "_DebugMetallic", "_DEBUG_METALLIC", "Metallic");
            }

            EditorGUILayout.Space(4);

            // Render queue
            materialEditor.RenderQueueField();
        }

        // ===== Standard PBR property drawing =====

        private void DrawStandardPBRProperties(MaterialEditor editor, MaterialProperty[] props)
        {
            // Normal
            DrawMapProperty(editor, props, "_BumpMap", "_UseNormalMap", "_NORMALMAP",
                L("ノーマルマップ", "Normal Map"), "_BumpScale", L("強度", "Strength"));

            // Roughness
            DrawMapProperty(editor, props, "_RoughnessMap", "_UseRoughnessMap", "_USE_ROUGHNESS_MAP",
                L("ラフネスマップ", "Roughness Map"));

            // Metallic
            DrawMapProperty(editor, props, "_MetallicMap", "_UseMetallicMap", "_METALLIC_MAP",
                L("メタリックマップ", "Metallic Map"), "_Metallic", L("メタリック", "Metallic"));

            // Smoothness (when no roughness map)
            MaterialProperty smoothness = FindProperty("_Smoothness", props, false);
            if (smoothness != null)
                editor.ShaderProperty(smoothness, L("スムーズネス", "Smoothness"));

            // Height
            DrawMapProperty(editor, props, "_HeightMap", "_UseHeightMap", "_HEIGHTMAP",
                L("ハイトマップ", "Height Map"), "_HeightScale", L("スケール", "Scale"));
            MaterialProperty heightSteps = FindProperty("_HeightSteps", props, false);
            if (heightSteps != null)
                editor.ShaderProperty(heightSteps, L("POM ステップ数", "POM Steps"));

            // Occlusion
            DrawMapProperty(editor, props, "_OcclusionMap", "_UseOcclusionMap", "_OCCLUSION_MAP",
                L("オクルージョンマップ", "Occlusion Map"), "_OcclusionStrength", L("強度", "Strength"));

            // Emission
            MaterialProperty emissionMap = FindProperty("_EmissionMap", props, false);
            MaterialProperty emissionColor = FindProperty("_EmissionColor", props, false);
            MaterialProperty useEmission = FindProperty("_UseEmission", props, false);
            if (emissionMap != null && emissionColor != null)
            {
                editor.TexturePropertySingleLine(
                    new GUIContent(L("エミッション", "Emission")), emissionMap, emissionColor);
            }
        }

        private void DrawMapProperty(MaterialEditor editor, MaterialProperty[] props,
            string texProp, string toggleProp, string keyword, string label,
            string scaleProp = null, string scaleLabel = null)
        {
            MaterialProperty tex = FindProperty(texProp, props, false);
            if (tex == null) return;

            MaterialProperty scale = scaleProp != null ? FindProperty(scaleProp, props, false) : null;

            if (scale != null)
                editor.TexturePropertySingleLine(new GUIContent(label), tex, scale);
            else
                editor.TexturePropertySingleLine(new GUIContent(label), tex);

            // Auto-sync keyword
            Material mat = editor.target as Material;
            if (mat != null)
            {
                bool hasTex = tex.textureValue != null;
                MaterialProperty toggle = FindProperty(toggleProp, props, false);
                if (toggle != null)
                    toggle.floatValue = hasTex ? 1f : 0f;
                if (hasTex)
                    mat.EnableKeyword(keyword);
                else
                    mat.DisableKeyword(keyword);
            }
        }

        private void DrawToggleKeyword(MaterialEditor editor, MaterialProperty[] props,
            string propName, string keyword, string label)
        {
            MaterialProperty prop = FindProperty(propName, props, false);
            if (prop == null) return;

            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle(label, prop.floatValue > 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = enabled ? 1f : 0f;
                foreach (Material mat in editor.targets)
                {
                    if (enabled) mat.EnableKeyword(keyword);
                    else mat.DisableKeyword(keyword);
                }
            }
        }

        // ===== Presets =====

        private void ApplyPreset(int index)
        {
            switch (index)
            {
                case 1: // Stone
                    genParams.normalStrength = 1.0f;
                    genParams.roughnessRemapMin = 0.01f;
                    genParams.roughnessRemapMax = 0.04f;
                    genParams.roughnessSaturationWeight = 0.1f;
                    genParams.metallicBias = -0.3f;
                    genParams.aoStrength = 0.8f;
                    break;
                case 2: // Metal
                    genParams.normalStrength = 0.5f;
                    genParams.roughnessRemapMin = 0.005f;
                    genParams.roughnessRemapMax = 0.03f;
                    genParams.roughnessSaturationWeight = 0.5f;
                    genParams.metallicLuminanceThreshold = 0.5f;
                    genParams.metallicSaturationThreshold = 0.4f;
                    genParams.metallicBias = 0.2f;
                    genParams.aoStrength = 0.5f;
                    break;
                case 3: // Wood
                    genParams.normalStrength = 0.8f;
                    genParams.roughnessRemapMin = 0.01f;
                    genParams.roughnessRemapMax = 0.04f;
                    genParams.roughnessSaturationWeight = 0.2f;
                    genParams.metallicBias = -0.5f;
                    genParams.aoStrength = 0.6f;
                    break;
                case 4: // Fabric
                    genParams.normalStrength = 0.3f;
                    genParams.roughnessRemapMin = 0.02f;
                    genParams.roughnessRemapMax = 0.06f;
                    genParams.roughnessSaturationWeight = 0.1f;
                    genParams.metallicBias = -0.5f;
                    genParams.aoStrength = 0.3f;
                    break;
                case 5: // Plastic
                    genParams.normalStrength = 0.7f;
                    genParams.roughnessRemapMin = 0.005f;
                    genParams.roughnessRemapMax = 0.03f;
                    genParams.roughnessSaturationWeight = 0.3f;
                    genParams.metallicBias = -0.4f;
                    genParams.aoStrength = 0.5f;
                    break;
            }
        }
    }
}
