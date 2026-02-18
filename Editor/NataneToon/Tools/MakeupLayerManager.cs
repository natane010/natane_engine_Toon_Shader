using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// Makeup Texture Layer Management Tool
    /// メイクアップテクスチャレイヤー管理ツール
    /// Visual layer stack editor for managing 5 makeup texture layers with HSV per layer
    /// 5つのメイクアップテクスチャレイヤーをHSV調整付きで視覚的に管理
    /// </summary>
    public class MakeupLayerManager : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;

        // Layer data
        private class LayerData
        {
            public string textureProp;
            public string colorProp;
            public string hueProp;
            public string saturationProp;
            public string valueProp;
            public string blendModeProp;
            public string displayName;
            public bool foldout = true;
        }

        private LayerData[] layers = new LayerData[]
        {
            new LayerData {
                textureProp = "_2ndTex",
                colorProp = "_2ndColor",
                hueProp = "_2ndHue",
                saturationProp = "_2ndSaturation",
                valueProp = "_2ndValue",
                blendModeProp = "_2ndBlendMode",
                displayName = "第2レイヤー"
            },
            new LayerData {
                textureProp = "_3rdTex",
                colorProp = "_3rdColor",
                hueProp = "_3rdHue",
                saturationProp = "_3rdSaturation",
                valueProp = "_3rdValue",
                blendModeProp = "_3rdBlendMode",
                displayName = "第3レイヤー"
            },
            new LayerData {
                textureProp = "_4thTex",
                colorProp = "_4thColor",
                hueProp = "_4thHue",
                saturationProp = "_4thSaturation",
                valueProp = "_4thValue",
                blendModeProp = "_4thBlendMode",
                displayName = "第4レイヤー"
            },
            new LayerData {
                textureProp = "_5thTex",
                colorProp = "_5thColor",
                hueProp = "_5thHue",
                saturationProp = "_5thSaturation",
                valueProp = "_5thValue",
                blendModeProp = "_5thBlendMode",
                displayName = "第5レイヤー"
            },
        };

        private string[] blendModeNames = new string[] { "加算", "乗算", "オーバーレイ", "スクリーン" };

        // Layer templates
        private enum LayerTemplate { Custom, Blush, EyeShadow, Lipstick, Highlight, Contour }
        private LayerTemplate selectedTemplate = LayerTemplate.Custom;

        [MenuItem("Tools/Natane/マテリアル Material/メイクアップレイヤー管理 Makeup Layer Manager", false, 15)]
        public static void ShowWindow()
        {
            var window = GetWindow<MakeupLayerManager>("メイクアップレイヤー管理 Makeup Layer Manager");
            window.minSize = new Vector2(500, 700);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawMaterialSelection();
            EditorGUILayout.Space(10);

            if (targetMaterial != null)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                DrawLayerStack();
                EditorGUILayout.Space(10);
                DrawTemplates();
                EditorGUILayout.Space(10);
                DrawBatchOperations();

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("メイクアップレイヤー管理", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("5つのテクスチャレイヤーをHSV調整付きで視覚的に管理", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();
            targetMaterial = (Material)EditorGUILayout.ObjectField(
                "ターゲットマテリアル",
                targetMaterial,
                typeof(Material),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                // Material changed
                Repaint();
            }

            if (GUILayout.Button("選択中のマテリアルを使用", GUILayout.Height(25)))
            {
                if (Selection.activeObject is Material mat)
                {
                    targetMaterial = mat;
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "エラー",
                        "マテリアルを選択してください",
                        "OK");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLayerStack()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("レイヤースタック", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Draw layers from top to bottom
            for (int i = layers.Length - 1; i >= 0; i--)
            {
                DrawLayer(layers[i], i);
                EditorGUILayout.Space(5);
            }

            // Base layer (read-only)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);
            EditorGUILayout.LabelField("ベースレイヤー (_MainTex)", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.white;

            if (targetMaterial.HasProperty("_MainTex"))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("テクスチャ", targetMaterial.GetTexture("_MainTex"), typeof(Texture2D), false);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void DrawLayer(LayerData layer, int index)
        {
            if (!targetMaterial.HasProperty(layer.textureProp)) return;

            Texture2D tex = targetMaterial.GetTexture(layer.textureProp) as Texture2D;
            bool hasTexture = tex != null;

            // Layer header with foldout
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Color bgColor = hasTexture ? new Color(0.6f, 0.8f, 1f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.2f);
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginHorizontal();
            layer.foldout = EditorGUILayout.Foldout(layer.foldout, layer.displayName, true, EditorStyles.foldoutHeader);

            // Quick enable/disable
            if (hasTexture)
            {
                if (GUILayout.Button("クリア", GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog(
                        "レイヤーをクリア",
                        $"{layer.displayName}のテクスチャをクリアしますか？",
                        "はい",
                        "いいえ"))
                    {
                        Undo.RecordObject(targetMaterial, "Clear Layer Texture");
                        targetMaterial.SetTexture(layer.textureProp, null);
                        EditorUtility.SetDirty(targetMaterial);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            if (layer.foldout)
            {
                EditorGUI.indentLevel++;

                // Texture
                EditorGUI.BeginChangeCheck();
                Texture2D newTex = (Texture2D)EditorGUILayout.ObjectField(
                    "テクスチャ",
                    tex,
                    typeof(Texture2D),
                    false);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Layer Texture");
                    targetMaterial.SetTexture(layer.textureProp, newTex);
                    EditorUtility.SetDirty(targetMaterial);
                }

                if (hasTexture)
                {
                    // Show texture preview
                    EditorGUILayout.Space(5);
                    Rect previewRect = GUILayoutUtility.GetRect(100, 100);
                    EditorGUI.DrawPreviewTexture(previewRect, tex, null, ScaleMode.ScaleToFit);
                    EditorGUILayout.Space(5);

                    // Color tint
                    if (targetMaterial.HasProperty(layer.colorProp))
                    {
                        EditorGUI.BeginChangeCheck();
                        Color color = EditorGUILayout.ColorField("色", targetMaterial.GetColor(layer.colorProp));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change Layer Color");
                            targetMaterial.SetColor(layer.colorProp, color);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }

                    // HSV adjustment
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("HSV調整", EditorStyles.boldLabel);

                    if (targetMaterial.HasProperty(layer.hueProp))
                    {
                        EditorGUI.BeginChangeCheck();
                        float hue = EditorGUILayout.Slider("色相", targetMaterial.GetFloat(layer.hueProp), -180f, 180f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change Layer Hue");
                            targetMaterial.SetFloat(layer.hueProp, hue);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }

                    if (targetMaterial.HasProperty(layer.saturationProp))
                    {
                        EditorGUI.BeginChangeCheck();
                        float saturation = EditorGUILayout.Slider("彩度", targetMaterial.GetFloat(layer.saturationProp), 0f, 2f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change Layer Saturation");
                            targetMaterial.SetFloat(layer.saturationProp, saturation);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }

                    if (targetMaterial.HasProperty(layer.valueProp))
                    {
                        EditorGUI.BeginChangeCheck();
                        float value = EditorGUILayout.Slider("明度", targetMaterial.GetFloat(layer.valueProp), 0f, 2f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change Layer Value");
                            targetMaterial.SetFloat(layer.valueProp, value);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }

                    // Blend mode
                    if (targetMaterial.HasProperty(layer.blendModeProp))
                    {
                        EditorGUILayout.Space(5);
                        EditorGUI.BeginChangeCheck();
                        int blendMode = (int)targetMaterial.GetFloat(layer.blendModeProp);
                        blendMode = EditorGUILayout.Popup("ブレンドモード", blendMode, blendModeNames);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change Blend Mode");
                            targetMaterial.SetFloat(layer.blendModeProp, blendMode);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }

                    // Reset button
                    EditorGUILayout.Space(5);
                    if (GUILayout.Button("このレイヤーをリセット"))
                    {
                        ResetLayer(layer);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("テクスチャが設定されていません", MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTemplates()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("レイヤーテンプレート", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "事前設定されたHSV値でレイヤーを素早く設定",
                MessageType.Info);

            selectedTemplate = (LayerTemplate)EditorGUILayout.EnumPopup("テンプレート", selectedTemplate);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("第2レイヤーに適用", GUILayout.Height(25)))
            {
                ApplyTemplate(0, selectedTemplate);
            }
            if (GUILayout.Button("第3レイヤーに適用", GUILayout.Height(25)))
            {
                ApplyTemplate(1, selectedTemplate);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("第4レイヤーに適用", GUILayout.Height(25)))
            {
                ApplyTemplate(2, selectedTemplate);
            }
            if (GUILayout.Button("第5レイヤーに適用", GUILayout.Height(25)))
            {
                ApplyTemplate(3, selectedTemplate);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawBatchOperations()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("一括操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("すべてリセット", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "すべてリセット",
                    "すべてのメイクアップレイヤーをリセットしますか？",
                    "はい",
                    "いいえ"))
                {
                    foreach (var layer in layers)
                    {
                        ResetLayer(layer);
                    }
                }
            }

            if (GUILayout.Button("すべてクリア", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "すべてクリア",
                    "すべてのメイクアップレイヤーのテクスチャをクリアしますか?",
                    "はい",
                    "いいえ"))
                {
                    Undo.RecordObject(targetMaterial, "Clear All Layers");
                    foreach (var layer in layers)
                    {
                        if (targetMaterial.HasProperty(layer.textureProp))
                        {
                            targetMaterial.SetTexture(layer.textureProp, null);
                        }
                    }
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ResetLayer(LayerData layer)
        {
            Undo.RecordObject(targetMaterial, "Reset Layer");

            if (targetMaterial.HasProperty(layer.hueProp))
                targetMaterial.SetFloat(layer.hueProp, 0f);
            if (targetMaterial.HasProperty(layer.saturationProp))
                targetMaterial.SetFloat(layer.saturationProp, 1f);
            if (targetMaterial.HasProperty(layer.valueProp))
                targetMaterial.SetFloat(layer.valueProp, 1f);
            if (targetMaterial.HasProperty(layer.blendModeProp))
                targetMaterial.SetFloat(layer.blendModeProp, 0f);
            if (targetMaterial.HasProperty(layer.colorProp))
                targetMaterial.SetColor(layer.colorProp, Color.white);

            EditorUtility.SetDirty(targetMaterial);
        }

        private void ApplyTemplate(int layerIndex, LayerTemplate template)
        {
            if (layerIndex < 0 || layerIndex >= layers.Length) return;

            LayerData layer = layers[layerIndex];
            Undo.RecordObject(targetMaterial, "Apply Template");

            switch (template)
            {
                case LayerTemplate.Blush:
                    if (targetMaterial.HasProperty(layer.hueProp))
                        targetMaterial.SetFloat(layer.hueProp, 10f); // Slightly red
                    if (targetMaterial.HasProperty(layer.saturationProp))
                        targetMaterial.SetFloat(layer.saturationProp, 1.2f);
                    if (targetMaterial.HasProperty(layer.valueProp))
                        targetMaterial.SetFloat(layer.valueProp, 1.1f);
                    if (targetMaterial.HasProperty(layer.blendModeProp))
                        targetMaterial.SetFloat(layer.blendModeProp, 2f); // Overlay
                    if (targetMaterial.HasProperty(layer.colorProp))
                        targetMaterial.SetColor(layer.colorProp, new Color(1f, 0.8f, 0.8f, 1f));
                    break;

                case LayerTemplate.EyeShadow:
                    if (targetMaterial.HasProperty(layer.hueProp))
                        targetMaterial.SetFloat(layer.hueProp, -30f); // Purple-ish
                    if (targetMaterial.HasProperty(layer.saturationProp))
                        targetMaterial.SetFloat(layer.saturationProp, 1.3f);
                    if (targetMaterial.HasProperty(layer.valueProp))
                        targetMaterial.SetFloat(layer.valueProp, 0.9f);
                    if (targetMaterial.HasProperty(layer.blendModeProp))
                        targetMaterial.SetFloat(layer.blendModeProp, 1f); // Multiply
                    break;

                case LayerTemplate.Lipstick:
                    if (targetMaterial.HasProperty(layer.hueProp))
                        targetMaterial.SetFloat(layer.hueProp, 0f);
                    if (targetMaterial.HasProperty(layer.saturationProp))
                        targetMaterial.SetFloat(layer.saturationProp, 1.5f);
                    if (targetMaterial.HasProperty(layer.valueProp))
                        targetMaterial.SetFloat(layer.valueProp, 1.2f);
                    if (targetMaterial.HasProperty(layer.blendModeProp))
                        targetMaterial.SetFloat(layer.blendModeProp, 0f); // Add
                    if (targetMaterial.HasProperty(layer.colorProp))
                        targetMaterial.SetColor(layer.colorProp, new Color(1f, 0.3f, 0.3f, 1f));
                    break;

                case LayerTemplate.Highlight:
                    if (targetMaterial.HasProperty(layer.hueProp))
                        targetMaterial.SetFloat(layer.hueProp, 40f); // Warm
                    if (targetMaterial.HasProperty(layer.saturationProp))
                        targetMaterial.SetFloat(layer.saturationProp, 0.8f);
                    if (targetMaterial.HasProperty(layer.valueProp))
                        targetMaterial.SetFloat(layer.valueProp, 1.5f);
                    if (targetMaterial.HasProperty(layer.blendModeProp))
                        targetMaterial.SetFloat(layer.blendModeProp, 3f); // Screen
                    if (targetMaterial.HasProperty(layer.colorProp))
                        targetMaterial.SetColor(layer.colorProp, new Color(1f, 1f, 0.9f, 1f));
                    break;

                case LayerTemplate.Contour:
                    if (targetMaterial.HasProperty(layer.hueProp))
                        targetMaterial.SetFloat(layer.hueProp, 20f); // Warm brown
                    if (targetMaterial.HasProperty(layer.saturationProp))
                        targetMaterial.SetFloat(layer.saturationProp, 0.7f);
                    if (targetMaterial.HasProperty(layer.valueProp))
                        targetMaterial.SetFloat(layer.valueProp, 0.6f);
                    if (targetMaterial.HasProperty(layer.blendModeProp))
                        targetMaterial.SetFloat(layer.blendModeProp, 1f); // Multiply
                    if (targetMaterial.HasProperty(layer.colorProp))
                        targetMaterial.SetColor(layer.colorProp, new Color(0.7f, 0.6f, 0.5f, 1f));
                    break;
            }

            EditorUtility.SetDirty(targetMaterial);

            EditorUtility.DisplayDialog(
                "テンプレート適用",
                $"{template}テンプレートを{layer.displayName}に適用しました",
                "OK");
        }
    }
}
