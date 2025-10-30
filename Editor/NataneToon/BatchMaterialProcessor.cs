using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// Batch processing tool for bulk material operations
    /// Allows efficient management of multiple materials simultaneously
    /// </summary>
    public class BatchMaterialProcessor : EditorWindow
    {
        private Vector2 scrollPosition;
        private Vector2 materialListScroll;
        private List<Material> selectedMaterials = new List<Material>();
        private int selectedTab = 0;
        private string[] tabs = new[] { "パラメータ調整", "色調整", "テクスチャ置換", "機能切り替え", "バリアント変換" };

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
            var window = GetWindow<BatchMaterialProcessor>("バッチプロセッサー");
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
                EditorGUILayout.HelpBox("バッチ処理を開始するためにマテリアルを選択してください", MessageType.Info);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("バッチマテリアルプロセッサー", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("複数のマテリアルを一度に効率的に処理", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("マテリアル選択", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("選択を追加", GUILayout.Height(25)))
            {
                AddSelectedMaterials();
            }

            if (GUILayout.Button("すべての Natane Toon を追加", GUILayout.Height(25)))
            {
                AddAllNataneToonMaterials();
            }

            if (GUILayout.Button("名前で追加", GUILayout.Height(25)))
            {
                ShowAddByNameDialog();
            }

            if (GUILayout.Button("クリア", GUILayout.Height(25)))
            {
                selectedMaterials.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (selectedMaterials.Count > 0)
            {
                EditorGUILayout.LabelField($"選択済み: {selectedMaterials.Count} マテリアル", EditorStyles.boldLabel);

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
            EditorGUILayout.LabelField("バッチパラメータ調整", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択されたすべてのマテリアル全体でフロートパラメータを調整します。\n" +
                "設定 = 値を置き換え、追加 = 現在の値に追加、乗算 = 現在の値に乗算",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Parameter selection
            selectedParameter = EditorGUILayout.Popup("パラメータ", selectedParameter, floatParameters);

            // Adjust mode
            adjustMode = (AdjustMode)EditorGUILayout.EnumPopup("モード", adjustMode);

            // Value
            string label = adjustMode == AdjustMode.Set ? "新規値" :
                          adjustMode == AdjustMode.Add ? "追加量" : "乗算値";
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
                    EditorGUILayout.LabelField($"Example: {currentValue:F3} → {newValue:F3}");
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("すべての選択マテリアルに適用", GUILayout.Height(30)))
            {
                ApplyParameterAdjustment();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawColorAdjust()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("バッチ色調整", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択されたすべてのマテリアル全体で色を調整します。\n" +
                "絶対色を設定するか、HSV値を相対的に調整できます。",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Color parameter selection
            selectedColorParam = (ColorParameter)EditorGUILayout.EnumPopup("色パラメータ", selectedColorParam);

            EditorGUILayout.Space(10);

            // Absolute color setting
            EditorGUILayout.LabelField("絶対色設定", EditorStyles.boldLabel);
            targetColor = EditorGUILayout.ColorField("色を設定", targetColor);

            if (GUILayout.Button("色を設定", GUILayout.Height(25)))
            {
                ApplyColorSet();
            }

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // Relative HSV adjustment
            EditorGUILayout.LabelField("相対 HSV 調整", EditorStyles.boldLabel);

            adjustHue = EditorGUILayout.Toggle("色相を調整", adjustHue);
            if (adjustHue)
            {
                hueShift = EditorGUILayout.Slider("色相シフト", hueShift, -180f, 180f);
            }

            adjustSaturation = EditorGUILayout.Toggle("彩度を調整", adjustSaturation);
            if (adjustSaturation)
            {
                saturationMultiplier = EditorGUILayout.Slider("彩度乗算", saturationMultiplier, 0f, 2f);
            }

            adjustBrightness = EditorGUILayout.Toggle("明度を調整", adjustBrightness);
            if (adjustBrightness)
            {
                valueMultiplier = EditorGUILayout.Slider("明度乗算", valueMultiplier, 0f, 2f);
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("HSV 調整を適用", GUILayout.Height(25)))
            {
                ApplyHSVAdjustment();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTextureReplace()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("バッチテクスチャ置換", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択されたすべてのマテリアル全体でテクスチャを置き換えます。\n" +
                "テクスチャセットの入れ替えやアセットの更新に便利です。",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Texture property selection
            string[] textureProps = new[] { "_MainTex", "_BumpMap", "_EmissionMap", "_MatCap", "_RampTex",
                                           "_SpecularMask", "_RimMask", "_SSSMask", "_MatCapMask", "_EmissionMask",
                                           "_ReflectionMask", "_ParallaxMap" };
            int selectedProp = System.Array.IndexOf(textureProps, textureProperty);
            if (selectedProp < 0) selectedProp = 0;

            selectedProp = EditorGUILayout.Popup("テクスチャプロパティ", selectedProp, textureProps);
            textureProperty = textureProps[selectedProp];

            // Replacement texture
            replacementTexture = (Texture2D)EditorGUILayout.ObjectField(
                "置換テクスチャ",
                replacementTexture,
                typeof(Texture2D),
                false);

            EditorGUILayout.Space(10);

            // Statistics
            int materialsWithThisTexture = selectedMaterials.Count(m =>
                m != null && m.HasProperty(textureProperty) && m.GetTexture(textureProperty) != null);

            EditorGUILayout.LabelField($"{textureProperty} を持つマテリアル: {materialsWithThisTexture}/{selectedMaterials.Count}");

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(replacementTexture == null))
            {
                if (GUILayout.Button("すべての選択対象のテクスチャを置換", GUILayout.Height(30)))
                {
                    ApplyTextureReplacement();
                }
            }

            EditorGUILayout.Space(10);

            // Clear texture option
            if (GUILayout.Button("すべての選択対象のテクスチャをクリア", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "テクスチャをクリア",
                    $"すべての選択マテリアルから {textureProperty} を削除しますか?",
                    "クリア",
                    "キャンセル"))
                {
                    ClearTexture();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFeatureToggle()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("バッチ機能切り替え", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択されたすべてのマテリアル全体でシェーダー機能を有効または無効にします。\n" +
                "パフォーマンスの最適化または一貫したスタイリングに便利です。",
                MessageType.Info);

            EditorGUILayout.Space(5);

            for (int i = 0; i < features.Length; i++)
            {
                featureStates[i] = EditorGUILayout.Toggle(GetFeatureName(features[i]), featureStates[i]);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("選択機能を有効にする", GUILayout.Height(25)))
            {
                ApplyFeatureToggle(true);
            }

            if (GUILayout.Button("選択機能を無効にする", GUILayout.Height(25)))
            {
                ApplyFeatureToggle(false);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("すべての機能を無効化（最大パフォーマンス）", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "すべての機能を無効化",
                    "選択されたマテリアルのすべてのシェーダー機能を無効にして、最大パフォーマンスを実現します。続行しますか?",
                    "すべて無効化",
                    "キャンセル"))
                {
                    DisableAllFeatures();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawVariantConvert()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("バッチバリアント変換", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "シェーダーバリアント（オペーク/カットアウト/透明）間でマテリアルを変換します。\n" +
                "すべてのパラメータ設定を保持します。",
                MessageType.Info);

            EditorGUILayout.Space(5);

            targetVariant = (ShaderVariant)EditorGUILayout.EnumPopup("目標バリアント", targetVariant);

            EditorGUILayout.Space(10);

            // Statistics
            int opaqueCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Toon Shader") && !m.shader.name.Contains("Cutout") && !m.shader.name.Contains("Transparent"));
            int cutoutCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Cutout"));
            int transparentCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Transparent"));

            EditorGUILayout.LabelField($"現在の分布:");
            EditorGUILayout.LabelField($"  オペーク: {opaqueCount}");
            EditorGUILayout.LabelField($"  カットアウト: {cutoutCount}");
            EditorGUILayout.LabelField($"  透明: {transparentCount}");

            EditorGUILayout.Space(10);

            if (GUILayout.Button($"すべてを {targetVariant} に変換", GUILayout.Height(30)))
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

            Debug.Log($"[BatchProcessor] {selectedMaterials.Count} 個の Natane Toon マテリアルが見つかりました");
        }

        private void ShowAddByNameDialog()
        {
            // Simple implementation - can be enhanced
            string searchTerm = EditorInputDialog.Show("名前でマテリアルを追加", "検索する名前を入力してください:", "");
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

            Debug.Log($"[BatchProcessor] '{searchTerm}' に一致する {addedCount} 個のマテリアルを追加しました");
        }

        private void ApplyParameterAdjustment()
        {
            string paramName = floatParameters[selectedParameter];
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(paramName)) continue;

                Undo.RecordObject(material, "バッチパラメータ調整");

                float currentValue = material.GetFloat(paramName);
                float newValue = CalculateNewValue(currentValue, adjustValue, adjustMode);
                material.SetFloat(paramName, newValue);

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "パラメータ調整完了",
                $"{successCount} 個のマテリアルで {paramName} を調整しました",
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

                Undo.RecordObject(material, "バッチ色設定");
                material.SetColor(colorPropName, targetColor);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "色設定完了",
                $"{successCount} 個のマテリアルで {colorPropName} を設定しました",
                "OK");
        }

        private void ApplyHSVAdjustment()
        {
            string colorPropName = GetColorPropertyName(selectedColorParam);
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(colorPropName)) continue;

                Undo.RecordObject(material, "バッチ HSV 調整");

                Color currentColor = material.GetColor(colorPropName);
                Color newColor = AdjustColorHSV(currentColor);
                material.SetColor(colorPropName, newColor);

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "HSV 調整完了",
                $"{successCount} 個のマテリアルで {colorPropName} を調整しました",
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

                Undo.RecordObject(material, "バッチテクスチャ置換");
                material.SetTexture(textureProperty, replacementTexture);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "テクスチャ置換完了",
                $"{successCount} 個のマテリアルで {textureProperty} を置き換えました",
                "OK");
        }

        private void ClearTexture()
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(textureProperty)) continue;

                Undo.RecordObject(material, "バッチテクスチャクリア");
                material.SetTexture(textureProperty, null);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "テクスチャクリア完了",
                $"{successCount} 個のマテリアルで {textureProperty} をクリアしました",
                "OK");
        }

        private void ApplyFeatureToggle(bool enable)
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null) continue;

                Undo.RecordObject(material, "バッチ機能切り替え");

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

            string action = enable ? "有効化" : "無効化";
            int featureCount = featureStates.Count(f => f);

            EditorUtility.DisplayDialog(
                "機能切り替え完了",
                $"{successCount} 個のマテリアルで {featureCount} 個の機能を {action} しました",
                "OK");
        }

        private void DisableAllFeatures()
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null) continue;

                Undo.RecordObject(material, "すべての機能を無効化");

                foreach (string feature in features)
                {
                    material.DisableKeyword(feature);
                }

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "すべての機能を無効化完了",
                $"{successCount} 個のマテリアルのすべての機能を無効化して、最大パフォーマンスを実現しました",
                "OK");
        }

        private void ApplyVariantConversion()
        {
            string targetShaderName = GetShaderNameForVariant(targetVariant);
            Shader targetShader = Shader.Find(targetShaderName);

            if (targetShader == null)
            {
                EditorUtility.DisplayDialog("エラー", $"シェーダーが見つかりません: {targetShaderName}", "OK");
                return;
            }

            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null) continue;

                Undo.RecordObject(material, "バッチバリアント変換");
                material.shader = targetShader;
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "バリアント変換完了",
                $"{successCount} 個のマテリアルを {targetVariant} バリアントに変換しました",
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
