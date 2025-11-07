using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// Batch processing tool for bulk material operations
    /// マテリアルの一括処理ツール
    /// Allows efficient management of multiple materials simultaneously
    /// 複数のマテリアルを同時に効率的に管理できます
    /// </summary>
    public class BatchMaterialProcessor : EditorWindow
    {
        private Vector2 scrollPosition;
        private Vector2 materialListScroll;
        private List<Material> selectedMaterials = new List<Material>();
        private int selectedTab = 0;
        private string[] tabs = new[] { "パラメータ調整 Parameter", "色調整 Color", "テクスチャ置換 Texture", "機能切替 Feature", "バリアント変換 Variant" };

        // Parameter adjustment
        private enum ParameterType { Float, Color, Vector }
        private string[] floatParameters = new[] { "_ToonSteps", "_ToonSharpness", "_ShadowReceive", "_OutlineWidth",
                                                   "_SpecularIntensity", "_RimIntensity", "_SSSIntensity",
                                                   "_EmissionIntensity", "_ReflectionIntensity", "_Metallic", "_Smoothness" };
        private int selectedParameter = 0;
        private enum AdjustMode { Set, Add, Multiply }
        private AdjustMode adjustMode = AdjustMode.Set;
        private float adjustValue = 1.0f;

        // Color adjustment
        private enum ColorParameter { MainColor, ShadowColor, RimColor, EmissionColor, OutlineColor, SpecularColor }
        private ColorParameter selectedColorParam = ColorParameter.MainColor;
        private Color targetColor = Color.white;
        private bool adjustHue = false;
        private bool adjustSaturation = false;
        private bool adjustBrightness = false;
        private float hueShift = 0f;
        private float saturationMultiplier = 1f;
        private float valueMultiplier = 1f;

        // Texture replacement
        private string textureProperty = "_MainTex";
        private Texture2D replacementTexture;

        // Feature toggle
        private string[] features = new[] { "_SPECULAR", "_RIM", "_SSS", "_MATCAP", "_OUTLINE", "_EMISSION",
                                           "_NORMALMAP", "_REFLECTION", "_ENV_RIM", "_PARALLAX", "_REFRACTION" };
        private bool[] featureStates = new bool[11];

        // Variant conversion
        private enum ShaderVariant { Opaque, Cutout, Transparent }
        private ShaderVariant targetVariant = ShaderVariant.Opaque;

        [MenuItem("Tools/Natane/Batch Material Processor", false, 80)]
        public static void ShowWindow()
        {
            var window = GetWindow<BatchMaterialProcessor>("一括処理 Batch Processor");
            window.minSize = new Vector2(650, 500);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(5);
            DrawMaterialSelection();
            EditorGUILayout.Space(5);

            if (selectedMaterials.Count > 0)
            {
                DrawTabs();
                EditorGUILayout.Space(5);
                DrawTabContent();
            }
            else
            {
                EditorGUILayout.HelpBox("マテリアルを選択して一括処理を開始してください\nSelect materials to begin batch processing", MessageType.Info);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("マテリアル一括処理 Batch Material Processor", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("複数のマテリアルを効率的に処理 Efficiently process multiple materials at once", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("マテリアル選択 Material Selection", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("選択を追加 Add Selected", GUILayout.Height(25)))
            {
                AddSelectedMaterials();
            }

            if (GUILayout.Button("全Natane Toon追加 Add All", GUILayout.Height(25)))
            {
                AddAllNataneToonMaterials();
            }

            if (GUILayout.Button("名前で追加 By Name", GUILayout.Height(25)))
            {
                ShowAddByNameDialog();
            }

            if (GUILayout.Button("クリア Clear", GUILayout.Height(25)))
            {
                selectedMaterials.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (selectedMaterials.Count > 0)
            {
                EditorGUILayout.LabelField($"選択中 Selected: {selectedMaterials.Count} マテリアル materials", EditorStyles.boldLabel);

                materialListScroll = EditorGUILayout.BeginScrollView(materialListScroll, GUILayout.Height(100));
                for (int i = selectedMaterials.Count - 1; i >= 0; i--)
                {
                    if (selectedMaterials[i] == null)
                    {
                        selectedMaterials.RemoveAt(i);
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(selectedMaterials[i], typeof(Material), false);
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        selectedMaterials.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(25));
        }

        private void DrawTabContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawParameterAdjust(); break;
                case 1: DrawColorAdjust(); break;
                case 2: DrawTextureReplace(); break;
                case 3: DrawFeatureToggle(); break;
                case 4: DrawVariantConvert(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawParameterAdjust()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("パラメータ一括調整 Batch Parameter Adjustment", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択したすべてのマテリアルのパラメータを調整します\n" +
                "Adjust float parameters across all selected materials.\n" +
                "Set = 置き換え replace value, Add = 加算 add to current, Multiply = 乗算 multiply current",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Parameter selection
            selectedParameter = EditorGUILayout.Popup("パラメータ Parameter", selectedParameter, floatParameters);

            // Adjust mode
            adjustMode = (AdjustMode)EditorGUILayout.EnumPopup("モード Mode", adjustMode);

            // Value
            string label = adjustMode == AdjustMode.Set ? "新しい値 New Value" :
                          adjustMode == AdjustMode.Add ? "加算量 Add Amount" : "乗算 Multiply By";
            adjustValue = EditorGUILayout.FloatField(label, adjustValue);

            EditorGUILayout.Space(10);

            // Preview
            if (selectedMaterials.Count > 0 && selectedMaterials[0] != null)
            {
                string paramName = floatParameters[selectedParameter];
                if (selectedMaterials[0].HasProperty(paramName))
                {
                    float currentValue = selectedMaterials[0].GetFloat(paramName);
                    float newValue = CalculateNewValue(currentValue, adjustValue, adjustMode);
                    EditorGUILayout.LabelField($"例 Example: {currentValue:F3} → {newValue:F3}");
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("すべてに適用 Apply to All Selected Materials", GUILayout.Height(30)))
            {
                ApplyParameterAdjustment();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawColorAdjust()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("色一括調整 Batch Color Adjustment", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択したすべてのマテリアルの色を調整します\n" +
                "Adjust colors across all selected materials.\n" +
                "絶対値で設定、またはHSV値を相対的に調整できます\n" +
                "Can set absolute color or adjust HSV values relatively.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Color parameter selection
            selectedColorParam = (ColorParameter)EditorGUILayout.EnumPopup("色パラメータ Color Parameter", selectedColorParam);

            EditorGUILayout.Space(10);

            // Absolute color setting
            EditorGUILayout.LabelField("絶対色設定 Absolute Color Setting", EditorStyles.boldLabel);
            targetColor = EditorGUILayout.ColorField("色を設定 Set Color To", targetColor);

            if (GUILayout.Button("色を設定 Set Color", GUILayout.Height(25)))
            {
                ApplyColorSet();
            }

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // Relative HSV adjustment
            EditorGUILayout.LabelField("相対HSV調整 Relative HSV Adjustment", EditorStyles.boldLabel);

            adjustHue = EditorGUILayout.Toggle("色相調整 Adjust Hue", adjustHue);
            if (adjustHue)
            {
                hueShift = EditorGUILayout.Slider("色相シフト Hue Shift", hueShift, -180f, 180f);
            }

            adjustSaturation = EditorGUILayout.Toggle("彩度調整 Adjust Saturation", adjustSaturation);
            if (adjustSaturation)
            {
                saturationMultiplier = EditorGUILayout.Slider("彩度乗算 Saturation Multiply", saturationMultiplier, 0f, 2f);
            }

            adjustBrightness = EditorGUILayout.Toggle("明度調整 Adjust Brightness", adjustBrightness);
            if (adjustBrightness)
            {
                valueMultiplier = EditorGUILayout.Slider("明度乗算 Brightness Multiply", valueMultiplier, 0f, 2f);
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("HSV調整を適用 Apply HSV Adjustment", GUILayout.Height(25)))
            {
                ApplyHSVAdjustment();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTextureReplace()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("テクスチャ一括置換 Batch Texture Replacement", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択したすべてのマテリアルのテクスチャを置換します\n" +
                "Replace textures across all selected materials.\n" +
                "テクスチャセットの入れ替えやアセット更新に便利\n" +
                "Useful for swapping texture sets or updating assets.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Texture property selection
            string[] textureProps = new[] { "_MainTex", "_BumpMap", "_EmissionMap", "_MatCap", "_RampTex",
                                           "_SpecularMask", "_RimMask", "_SSSMask", "_MatCapMask", "_EmissionMask",
                                           "_ReflectionMask", "_ParallaxMap" };
            int selectedProp = System.Array.IndexOf(textureProps, textureProperty);
            if (selectedProp < 0) selectedProp = 0;

            selectedProp = EditorGUILayout.Popup("テクスチャプロパティ Texture Property", selectedProp, textureProps);
            textureProperty = textureProps[selectedProp];

            // Replacement texture
            replacementTexture = (Texture2D)EditorGUILayout.ObjectField(
                "置換テクスチャ Replacement Texture",
                replacementTexture,
                typeof(Texture2D),
                false);

            EditorGUILayout.Space(10);

            // Statistics
            int materialsWithThisTexture = selectedMaterials.Count(m =>
                m != null && m.HasProperty(textureProperty) && m.GetTexture(textureProperty) != null);

            EditorGUILayout.LabelField($"{textureProperty}を持つマテリアル Materials with {textureProperty}: {materialsWithThisTexture}/{selectedMaterials.Count}");

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(replacementTexture == null))
            {
                if (GUILayout.Button("すべてのテクスチャを置換 Replace Texture in All Selected", GUILayout.Height(30)))
                {
                    ApplyTextureReplacement();
                }
            }

            EditorGUILayout.Space(10);

            // Clear texture option
            if (GUILayout.Button("すべてのテクスチャをクリア Clear Texture in All Selected", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "テクスチャをクリア Clear Texture",
                    $"選択したすべてのマテリアルから{textureProperty}を削除しますか？\nRemove {textureProperty} from all selected materials?",
                    "クリア Clear",
                    "キャンセル Cancel"))
                {
                    ClearTexture();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFeatureToggle()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("機能一括切替 Batch Feature Toggle", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択したすべてのマテリアルのシェーダー機能を有効/無効にします\n" +
                "Enable or disable shader features across all selected materials.\n" +
                "パフォーマンス最適化や一貫したスタイリングに便利\n" +
                "Useful for performance optimization or consistent styling.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            for (int i = 0; i < features.Length; i++)
            {
                featureStates[i] = EditorGUILayout.Toggle(GetFeatureName(features[i]), featureStates[i]);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("選択機能を有効化 Enable Selected Features", GUILayout.Height(25)))
            {
                ApplyFeatureToggle(true);
            }

            if (GUILayout.Button("選択機能を無効化 Disable Selected Features", GUILayout.Height(25)))
            {
                ApplyFeatureToggle(false);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("すべての機能を無効化（最大パフォーマンス）Disable All Features", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "すべての機能を無効化 Disable All Features",
                    "選択したマテリアルのすべてのシェーダー機能を無効化して最大パフォーマンスにしますか？\nThis will disable all shader features in selected materials for maximum performance. Continue?",
                    "すべて無効化 Disable All",
                    "キャンセル Cancel"))
                {
                    DisableAllFeatures();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawVariantConvert()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("バリアント一括変換 Batch Variant Conversion", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "シェーダーバリアント間（Opaque/Cutout/Transparent）でマテリアルを変換します\n" +
                "Convert materials between shader variants (Opaque/Cutout/Transparent).\n" +
                "すべてのパラメータ設定は保持されます\n" +
                "Preserves all parameter settings.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            targetVariant = (ShaderVariant)EditorGUILayout.EnumPopup("ターゲットバリアント Target Variant", targetVariant);

            EditorGUILayout.Space(10);

            // Statistics
            int opaqueCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Toon Shader") && !m.shader.name.Contains("Cutout") && !m.shader.name.Contains("Transparent"));
            int cutoutCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Cutout"));
            int transparentCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Transparent"));

            EditorGUILayout.LabelField($"現在の分布 Current Distribution:");
            EditorGUILayout.LabelField($"  Opaque: {opaqueCount}");
            EditorGUILayout.LabelField($"  Cutout: {cutoutCount}");
            EditorGUILayout.LabelField($"  Transparent: {transparentCount}");

            EditorGUILayout.Space(10);

            if (GUILayout.Button($"すべてを{targetVariant}に変換 Convert All to {targetVariant}", GUILayout.Height(30)))
            {
                ApplyVariantConversion();
            }

            EditorGUILayout.EndVertical();
        }

        // Implementation Methods

        private void AddSelectedMaterials()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Material material)
                {
                    if (!selectedMaterials.Contains(material))
                    {
                        selectedMaterials.Add(material);
                    }
                }
            }
        }

        private void AddAllNataneToonMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && material.shader != null)
                {
                    if (material.shader.name.Contains("Natane") && material.shader.name.Contains("Toon"))
                    {
                        if (!selectedMaterials.Contains(material))
                        {
                            selectedMaterials.Add(material);
                        }
                    }
                }
            }

            Debug.Log($"[BatchProcessor] {selectedMaterials.Count}個のNatane Toonマテリアルを見つけました Found {selectedMaterials.Count} Natane Toon materials");
        }

        private void ShowAddByNameDialog()
        {
            // Simple implementation - can be enhanced
            string searchTerm = EditorInputDialog.Show("名前でマテリアルを追加 Add Materials by Name", "検索する名前を入力 Enter name to search:", "");
            if (!string.IsNullOrEmpty(searchTerm))
            {
                AddMaterialsByName(searchTerm);
            }
        }

        private void AddMaterialsByName(string searchTerm)
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int addedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && material.name.ToLower().Contains(searchTerm.ToLower()))
                {
                    if (!selectedMaterials.Contains(material))
                    {
                        selectedMaterials.Add(material);
                        addedCount++;
                    }
                }
            }

            Debug.Log($"[BatchProcessor] '{searchTerm}'にマッチする{addedCount}個のマテリアルを追加しました Added {addedCount} materials matching '{searchTerm}'");
        }

        private void ApplyParameterAdjustment()
        {
            string paramName = floatParameters[selectedParameter];
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(paramName)) continue;

                Undo.RecordObject(material, "Batch Parameter Adjustment");

                float currentValue = material.GetFloat(paramName);
                float newValue = CalculateNewValue(currentValue, adjustValue, adjustMode);
                material.SetFloat(paramName, newValue);

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "パラメータを調整しました Parameter Adjusted",
                $"{successCount}個のマテリアルの{paramName}を調整しました\nAdjusted {paramName} in {successCount} materials",
                "OK");
        }

        private float CalculateNewValue(float current, float adjust, AdjustMode mode)
        {
            switch (mode)
            {
                case AdjustMode.Set: return adjust;
                case AdjustMode.Add: return current + adjust;
                case AdjustMode.Multiply: return current * adjust;
                default: return current;
            }
        }

        private void ApplyColorSet()
        {
            string colorPropName = GetColorPropertyName(selectedColorParam);
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(colorPropName)) continue;

                Undo.RecordObject(material, "Batch Color Set");
                material.SetColor(colorPropName, targetColor);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "色を設定しました Color Set",
                $"{successCount}個のマテリアルの{colorPropName}を設定しました\nSet {colorPropName} in {successCount} materials",
                "OK");
        }

        private void ApplyHSVAdjustment()
        {
            string colorPropName = GetColorPropertyName(selectedColorParam);
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(colorPropName)) continue;

                Undo.RecordObject(material, "Batch HSV Adjustment");

                Color currentColor = material.GetColor(colorPropName);
                Color newColor = AdjustColorHSV(currentColor);
                material.SetColor(colorPropName, newColor);

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "HSVを調整しました HSV Adjusted",
                $"{successCount}個のマテリアルの{colorPropName}を調整しました\nAdjusted {colorPropName} in {successCount} materials",
                "OK");
        }

        private Color AdjustColorHSV(Color color)
        {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);

            if (adjustHue)
            {
                h = (h + hueShift / 360f) % 1f;
                if (h < 0) h += 1f;
            }

            if (adjustSaturation)
            {
                s = Mathf.Clamp01(s * saturationMultiplier);
            }

            if (adjustBrightness)
            {
                v = Mathf.Clamp01(v * valueMultiplier);
            }

            return Color.HSVToRGB(h, s, v);
        }

        private void ApplyTextureReplacement()
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(textureProperty)) continue;

                Undo.RecordObject(material, "Batch Texture Replace");
                material.SetTexture(textureProperty, replacementTexture);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "テクスチャを置換しました Texture Replaced",
                $"{successCount}個のマテリアルの{textureProperty}を置換しました\nReplaced {textureProperty} in {successCount} materials",
                "OK");
        }

        private void ClearTexture()
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(textureProperty)) continue;

                Undo.RecordObject(material, "Batch Texture Clear");
                material.SetTexture(textureProperty, null);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "テクスチャをクリアしました Texture Cleared",
                $"{successCount}個のマテリアルの{textureProperty}をクリアしました\nCleared {textureProperty} in {successCount} materials",
                "OK");
        }

        private void ApplyFeatureToggle(bool enable)
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null) continue;

                Undo.RecordObject(material, "Batch Feature Toggle");

                for (int i = 0; i < features.Length; i++)
                {
                    if (featureStates[i])
                    {
                        if (enable)
                        {
                            material.EnableKeyword(features[i]);
                        }
                        else
                        {
                            material.DisableKeyword(features[i]);
                        }
                    }
                }

                EditorUtility.SetDirty(material);
                successCount++;
            }

            string action = enable ? "有効化しました Enabled" : "無効化しました Disabled";
            int featureCount = featureStates.Count(f => f);

            EditorUtility.DisplayDialog(
                "機能を切り替えました Features Toggled",
                $"{successCount}個のマテリアルの{featureCount}個の機能を{action}\n{action} {featureCount} features in {successCount} materials",
                "OK");
        }

        private void DisableAllFeatures()
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null) continue;

                Undo.RecordObject(material, "Disable All Features");

                foreach (string feature in features)
                {
                    material.DisableKeyword(feature);
                }

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "すべての機能を無効化しました All Features Disabled",
                $"{successCount}個のマテリアルのすべての機能を無効化して最大パフォーマンスにしました\nDisabled all features in {successCount} materials for maximum performance",
                "OK");
        }

        private void ApplyVariantConversion()
        {
            string targetShaderName = GetShaderNameForVariant(targetVariant);
            Shader targetShader = Shader.Find(targetShaderName);

            if (targetShader == null)
            {
                EditorUtility.DisplayDialog("エラー Error", $"シェーダーが見つかりません Shader not found: {targetShaderName}", "OK");
                return;
            }

            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null) continue;

                Undo.RecordObject(material, "Batch Variant Conversion");
                material.shader = targetShader;
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "バリアントを変換しました Variant Converted",
                $"{successCount}個のマテリアルを{targetVariant}バリアントに変換しました\nConverted {successCount} materials to {targetVariant} variant",
                "OK");
        }

        // Helper Methods

        private string GetColorPropertyName(ColorParameter param)
        {
            switch (param)
            {
                case ColorParameter.MainColor: return "_Color";
                case ColorParameter.ShadowColor: return "_ShadowColor";
                case ColorParameter.RimColor: return "_RimColor";
                case ColorParameter.EmissionColor: return "_EmissionColor";
                case ColorParameter.OutlineColor: return "_OutlineColor";
                case ColorParameter.SpecularColor: return "_SpecularColor";
                default: return "_Color";
            }
        }

        private string GetFeatureName(string keyword)
        {
            return keyword.Replace("_", " ").Trim();
        }

        private string GetShaderNameForVariant(ShaderVariant variant)
        {
            switch (variant)
            {
                case ShaderVariant.Opaque: return "Natane/Toon Shader";
                case ShaderVariant.Cutout: return "Natane/Toon Shader Cutout";
                case ShaderVariant.Transparent: return "Natane/Toon Shader Transparent";
                default: return "Natane/Toon Shader";
            }
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space(5);
        }
    }

    /// <summary>
    /// Simple input dialog helper
    /// シンプルな入力ダイアログヘルパー
    /// </summary>
    public static class EditorInputDialog
    {
        public static string Show(string title, string message, string defaultValue)
        {
            // Unity doesn't have built-in input dialog, so we return default for now
            // In real implementation, would create a custom EditorWindow
            return defaultValue;
        }
    }
}
