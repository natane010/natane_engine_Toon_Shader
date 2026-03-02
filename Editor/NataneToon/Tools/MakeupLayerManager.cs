using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Makeup Texture Layer Management Tool
    /// </summary>
    public class MakeupLayerManager : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;

        private class LayerData
        {
            public string textureProp;
            public string toggleProp;
            public string toggleKeyword;
            public string hueProp;
            public string saturationProp;
            public string valueProp;
            public string intensityProp;
            public string blendModeProp;
            public string displayName;
            public bool foldout = true;
        }

        private readonly LayerData[] layers =
        {
            new LayerData
            {
                textureProp = "_2ndTex",
                toggleProp = "_Use2ndTexture",
                toggleKeyword = "_2ND_TEXTURE",
                hueProp = "_2ndTexHueShift",
                saturationProp = "_2ndTexSaturation",
                valueProp = "_2ndTexValue",
                intensityProp = "_2ndTexIntensity",
                blendModeProp = "_2ndTexBlendMode",
                displayName = "Layer 2"
            },
            new LayerData
            {
                textureProp = "_3rdTex",
                toggleProp = "_Use3rdTexture",
                toggleKeyword = "_3RD_TEXTURE",
                hueProp = "_3rdTexHueShift",
                saturationProp = "_3rdTexSaturation",
                valueProp = "_3rdTexValue",
                intensityProp = "_3rdTexIntensity",
                blendModeProp = "_3rdTexBlendMode",
                displayName = "Layer 3"
            },
            new LayerData
            {
                textureProp = "_4thTex",
                toggleProp = "_Use4thTexture",
                toggleKeyword = "_4TH_TEXTURE",
                hueProp = "_4thTexHueShift",
                saturationProp = "_4thTexSaturation",
                valueProp = "_4thTexValue",
                intensityProp = "_4thTexIntensity",
                blendModeProp = "_4thTexBlendMode",
                displayName = "Layer 4"
            },
            new LayerData
            {
                textureProp = "_5thTex",
                toggleProp = "_Use5thTexture",
                toggleKeyword = "_5TH_TEXTURE",
                hueProp = "_5thTexHueShift",
                saturationProp = "_5thTexSaturation",
                valueProp = "_5thTexValue",
                intensityProp = "_5thTexIntensity",
                blendModeProp = "_5thTexBlendMode",
                displayName = "Layer 5"
            }
        };

        private string[] BlendModeNames => new[]
        {
            L("加算", "Add"),
            L("乗算", "Multiply"),
            L("オーバーレイ", "Overlay"),
            L("スクリーン", "Screen")
        };

        private enum LayerTemplate
        {
            Custom,
            Blush,
            EyeShadow,
            Lipstick,
            Highlight,
            Contour
        }

        private LayerTemplate selectedTemplate = LayerTemplate.Custom;

        [MenuItem("Tools/Natane/マテリアル Material/メイクアップレイヤー管理 Makeup Layer Manager", false, 15)]
        public static void ShowWindow()
        {
            var window = GetWindow<MakeupLayerManager>(L("メイクアップレイヤー管理", "Makeup Layer Manager"));
            window.minSize = new Vector2(500, 680);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            DrawHeader();
            EditorGUILayout.Space(10);
            DrawMaterialSelection();
            EditorGUILayout.Space(10);

            if (targetMaterial == null)
            {
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawLayerStack();
            EditorGUILayout.Space(10);
            DrawTemplates();
            EditorGUILayout.Space(10);
            DrawBatchOperations();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            NataneToonShaderGUIUtility.DrawToolHeader("メイクアップレイヤー管理", "Makeup Layer Manager", "MakeupLayerManager");
            EditorGUILayout.HelpBox(
                L("2nd-5thテクスチャのHSV・強度・ブレンドをレイヤーごとに編集します。", "Edit HSV, intensity, and blend mode for 2nd-5th texture layers."),
                MessageType.Info);
        }

        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            targetMaterial = (Material)EditorGUILayout.ObjectField(
                L("ターゲットマテリアル", "Target Material"),
                targetMaterial,
                typeof(Material),
                false);

            if (GUILayout.Button(L("選択中のマテリアルを使用", "Use Selected Material"), GUILayout.Height(24)))
            {
                if (Selection.activeObject is Material selectedMaterial)
                {
                    targetMaterial = selectedMaterial;
                }
                else
                {
                    EditorUtility.DisplayDialog(L("エラー", "Error"), L("マテリアルを選択してください", "Please select a material"), "OK");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLayerStack()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("レイヤースタック", "Layer Stack"), EditorStyles.boldLabel);

            for (int i = layers.Length - 1; i >= 0; i--)
            {
                DrawLayer(layers[i]);
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ベースレイヤー", "Base Layer") + " (_MainTex)", EditorStyles.boldLabel);
            if (targetMaterial.HasProperty("_MainTex"))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(L("テクスチャ", "Texture"), targetMaterial.GetTexture("_MainTex"), typeof(Texture2D), false);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void DrawLayer(LayerData layer)
        {
            if (!targetMaterial.HasProperty(layer.textureProp))
            {
                return;
            }

            Texture2D tex = targetMaterial.GetTexture(layer.textureProp) as Texture2D;
            bool hasTexture = tex != null;
            bool enabled = IsLayerEnabled(layer);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            layer.foldout = EditorGUILayout.Foldout(layer.foldout, layer.displayName, true, EditorStyles.foldoutHeader);

            if (layer.foldout)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                Texture2D newTex = (Texture2D)EditorGUILayout.ObjectField(L("テクスチャ", "Texture"), tex, typeof(Texture2D), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Layer Texture");
                    SetLayerTexture(layer, newTex);
                    EditorUtility.SetDirty(targetMaterial);
                    tex = newTex;
                    hasTexture = tex != null;
                    enabled = hasTexture;
                }

                if (targetMaterial.HasProperty(layer.toggleProp))
                {
                    EditorGUI.BeginChangeCheck();
                    bool nextEnabled = EditorGUILayout.Toggle(L("有効", "Enabled"), enabled);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetMaterial, "Toggle Layer");
                        SetLayerEnabled(layer, nextEnabled);
                        EditorUtility.SetDirty(targetMaterial);
                        enabled = nextEnabled;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!hasTexture))
                    {
                        if (GUILayout.Button(L("クリア", "Clear"), GUILayout.Width(80)))
                        {
                            Undo.RecordObject(targetMaterial, "Clear Layer Texture");
                            SetLayerTexture(layer, null);
                            EditorUtility.SetDirty(targetMaterial);
                            tex = null;
                            hasTexture = false;
                            enabled = false;
                        }
                    }

                    using (new EditorGUI.DisabledScope(!hasTexture))
                    {
                        if (GUILayout.Button(L("このレイヤーをリセット", "Reset This Layer")))
                        {
                            ResetLayer(layer);
                        }
                    }
                }

                if (hasTexture)
                {
                    Rect previewRect = GUILayoutUtility.GetRect(100, 100);
                    EditorGUI.DrawPreviewTexture(previewRect, tex, null, ScaleMode.ScaleToFit);

                    DrawFloatSlider(layer.intensityProp, L("強度", "Intensity"), 0f, 2f);

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(L("HSV調整", "HSV Adjustment"), EditorStyles.boldLabel);
                    DrawFloatSlider(layer.hueProp, L("色相", "Hue"), -0.5f, 0.5f);
                    DrawFloatSlider(layer.saturationProp, L("彩度", "Saturation"), 0f, 2f);
                    DrawFloatSlider(layer.valueProp, L("明度", "Value"), 0f, 2f);

                    if (targetMaterial.HasProperty(layer.blendModeProp))
                    {
                        EditorGUI.BeginChangeCheck();
                        int blendMode = (int)targetMaterial.GetFloat(layer.blendModeProp);
                        blendMode = EditorGUILayout.Popup(L("ブレンドモード", "Blend Mode"), blendMode, BlendModeNames);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change Blend Mode");
                            targetMaterial.SetFloat(layer.blendModeProp, blendMode);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(L("テクスチャが未設定です。", "No texture is set."), MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTemplates()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("レイヤーテンプレート", "Layer Templates"), EditorStyles.boldLabel);

            selectedTemplate = (LayerTemplate)EditorGUILayout.EnumPopup(L("テンプレート", "Template"), selectedTemplate);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(L("Layer 2に適用", "Apply to Layer 2"), GUILayout.Height(24))) ApplyTemplate(0, selectedTemplate);
                if (GUILayout.Button(L("Layer 3に適用", "Apply to Layer 3"), GUILayout.Height(24))) ApplyTemplate(1, selectedTemplate);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(L("Layer 4に適用", "Apply to Layer 4"), GUILayout.Height(24))) ApplyTemplate(2, selectedTemplate);
                if (GUILayout.Button(L("Layer 5に適用", "Apply to Layer 5"), GUILayout.Height(24))) ApplyTemplate(3, selectedTemplate);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBatchOperations()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("一括操作", "Batch Operations"), EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(L("すべてリセット", "Reset All"), GUILayout.Height(28)))
                {
                    foreach (var layer in layers)
                    {
                        ResetLayer(layer);
                    }
                }

                if (GUILayout.Button(L("すべてクリア", "Clear All"), GUILayout.Height(28)))
                {
                    Undo.RecordObject(targetMaterial, "Clear All Layers");
                    foreach (var layer in layers)
                    {
                        SetLayerTexture(layer, null);
                    }
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFloatSlider(string propName, string label, float min, float max)
        {
            if (!targetMaterial.HasProperty(propName))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider(label, targetMaterial.GetFloat(propName), min, max);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetMaterial, "Change Layer Parameter");
                targetMaterial.SetFloat(propName, value);
                EditorUtility.SetDirty(targetMaterial);
            }
        }

        private void ResetLayer(LayerData layer)
        {
            Undo.RecordObject(targetMaterial, "Reset Layer");

            if (targetMaterial.HasProperty(layer.hueProp)) targetMaterial.SetFloat(layer.hueProp, 0f);
            if (targetMaterial.HasProperty(layer.saturationProp)) targetMaterial.SetFloat(layer.saturationProp, 1f);
            if (targetMaterial.HasProperty(layer.valueProp)) targetMaterial.SetFloat(layer.valueProp, 1f);
            if (targetMaterial.HasProperty(layer.intensityProp)) targetMaterial.SetFloat(layer.intensityProp, 1f);
            if (targetMaterial.HasProperty(layer.blendModeProp)) targetMaterial.SetFloat(layer.blendModeProp, 0f);

            SetLayerEnabled(layer, targetMaterial.GetTexture(layer.textureProp) != null);
            EditorUtility.SetDirty(targetMaterial);
        }

        private void ApplyTemplate(int layerIndex, LayerTemplate template)
        {
            if (layerIndex < 0 || layerIndex >= layers.Length)
            {
                return;
            }

            LayerData layer = layers[layerIndex];
            Undo.RecordObject(targetMaterial, "Apply Template");

            switch (template)
            {
                case LayerTemplate.Blush:
                    SetTemplateValues(layer, 0.03f, 1.2f, 1.1f, 1.0f, 2f);
                    break;
                case LayerTemplate.EyeShadow:
                    SetTemplateValues(layer, -0.08f, 1.3f, 0.9f, 1.0f, 1f);
                    break;
                case LayerTemplate.Lipstick:
                    SetTemplateValues(layer, 0f, 1.5f, 1.2f, 1.2f, 0f);
                    break;
                case LayerTemplate.Highlight:
                    SetTemplateValues(layer, 0.1f, 0.8f, 1.5f, 1.1f, 3f);
                    break;
                case LayerTemplate.Contour:
                    SetTemplateValues(layer, 0.06f, 0.7f, 0.6f, 1.0f, 1f);
                    break;
            }

            SetLayerEnabled(layer, targetMaterial.GetTexture(layer.textureProp) != null);
            EditorUtility.SetDirty(targetMaterial);

            EditorUtility.DisplayDialog(
                L("テンプレート適用", "Template Applied"),
                L($"{template}を{layer.displayName}に適用しました", $"Applied {template} to {layer.displayName}"),
                "OK");
        }

        private void SetTemplateValues(LayerData layer, float hue, float saturation, float value, float intensity, float blendMode)
        {
            if (targetMaterial.HasProperty(layer.hueProp)) targetMaterial.SetFloat(layer.hueProp, hue);
            if (targetMaterial.HasProperty(layer.saturationProp)) targetMaterial.SetFloat(layer.saturationProp, saturation);
            if (targetMaterial.HasProperty(layer.valueProp)) targetMaterial.SetFloat(layer.valueProp, value);
            if (targetMaterial.HasProperty(layer.intensityProp)) targetMaterial.SetFloat(layer.intensityProp, intensity);
            if (targetMaterial.HasProperty(layer.blendModeProp)) targetMaterial.SetFloat(layer.blendModeProp, blendMode);
        }

        private bool IsLayerEnabled(LayerData layer)
        {
            if (!string.IsNullOrEmpty(layer.toggleProp) && targetMaterial.HasProperty(layer.toggleProp))
            {
                return targetMaterial.GetFloat(layer.toggleProp) > 0.5f;
            }

            if (!string.IsNullOrEmpty(layer.toggleKeyword))
            {
                return targetMaterial.IsKeywordEnabled(layer.toggleKeyword);
            }

            return false;
        }

        private void SetLayerEnabled(LayerData layer, bool enabled)
        {
            if (!string.IsNullOrEmpty(layer.toggleProp) && targetMaterial.HasProperty(layer.toggleProp))
            {
                targetMaterial.SetFloat(layer.toggleProp, enabled ? 1f : 0f);
            }

            if (!string.IsNullOrEmpty(layer.toggleKeyword))
            {
                if (enabled)
                {
                    targetMaterial.EnableKeyword(layer.toggleKeyword);
                }
                else
                {
                    targetMaterial.DisableKeyword(layer.toggleKeyword);
                }
            }
        }

        private void SetLayerTexture(LayerData layer, Texture2D texture)
        {
            if (!targetMaterial.HasProperty(layer.textureProp))
            {
                return;
            }

            targetMaterial.SetTexture(layer.textureProp, texture);
            SetLayerEnabled(layer, texture != null);
        }
    }
}
