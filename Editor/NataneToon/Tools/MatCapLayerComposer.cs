using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// MatCap Layer Composer
    /// </summary>
    public class MatCapLayerComposer : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;
        private readonly bool[] layerFoldouts = { true, true, true };

        private readonly string[] matcapTextureProps = { "_MatCapTex", "_MatCapTex2", "_MatCapTex3" };
        private readonly string[] matcapEnableProps = { "_MatCap", "_MatCap2", "_MatCap3" };
        private readonly string[] matcapEnableKeywords = { "_MATCAP", "_MATCAP_2", "_MATCAP_3" };
        private readonly string[] matcapIntensityProps = { "_MatCapIntensity", "_MatCapIntensity2", "_MatCapIntensity3" };
        private readonly string[] matcapBlendModeProps = { "_MatCapBlendMode", "_MatCapBlendMode2", "_MatCapBlendMode3" };
        private readonly string[] matcapBlendProps = { "_MatCapBlend", "_MatCapBlend2", "_MatCapBlend3" };

        private string[] BlendModeNames => new[] { L("加算", "Add"), L("乗算", "Multiply"), L("置換", "Replace") };

        [MenuItem("Tools/Natane/エフェクト Effects/MatCapレイヤーコンポーザー MatCap Layer Composer", false, 42)]
        public static void ShowWindow()
        {
            var window = GetWindow<MatCapLayerComposer>(L("MatCapレイヤーコンポーザー", "MatCap Layer Composer"));
            window.minSize = new Vector2(500, 560);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            NataneToonShaderGUIUtility.DrawHeaderWithHelp("MatCapレイヤーコンポーザー", "MatCap Layer Composer", "MatCapLayerComposer");
            EditorGUILayout.LabelField(L("MatCap 1-3を同時に編集します。", "Edit MatCap layers 1-3 in one place."), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField(L("ターゲットマテリアル", "Target Material"), targetMaterial, typeof(Material), false);
            if (targetMaterial == null)
            {
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < 3; i++)
            {
                DrawMatCapLayer(i);
                EditorGUILayout.Space(6);
            }

            DrawBatchOperations();
            EditorGUILayout.EndScrollView();
        }

        private void DrawMatCapLayer(int index)
        {
            if (!targetMaterial.HasProperty(matcapTextureProps[index]))
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            layerFoldouts[index] = EditorGUILayout.Foldout(layerFoldouts[index], $"MatCap Layer {index + 1}", true);

            if (layerFoldouts[index])
            {
                EditorGUI.indentLevel++;

                Texture2D currentTex = targetMaterial.GetTexture(matcapTextureProps[index]) as Texture2D;
                bool enabled = targetMaterial.HasProperty(matcapEnableProps[index]) && targetMaterial.GetFloat(matcapEnableProps[index]) > 0.5f;

                EditorGUI.BeginChangeCheck();
                Texture2D newTex = (Texture2D)EditorGUILayout.ObjectField("MatCap", currentTex, typeof(Texture2D), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change MatCap Texture");
                    targetMaterial.SetTexture(matcapTextureProps[index], newTex);
                    SetLayerEnabled(index, newTex != null);
                    EditorUtility.SetDirty(targetMaterial);
                    currentTex = newTex;
                    enabled = newTex != null;
                }

                if (targetMaterial.HasProperty(matcapEnableProps[index]))
                {
                    EditorGUI.BeginChangeCheck();
                    bool nextEnabled = EditorGUILayout.Toggle(L("有効", "Enabled"), enabled);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetMaterial, "Toggle MatCap");
                        SetLayerEnabled(index, nextEnabled);
                        EditorUtility.SetDirty(targetMaterial);
                        enabled = nextEnabled;
                    }
                }

                if (currentTex != null)
                {
                    Rect previewRect = GUILayoutUtility.GetRect(100, 100);
                    EditorGUI.DrawPreviewTexture(previewRect, currentTex, null, ScaleMode.ScaleToFit);

                    if (targetMaterial.HasProperty(matcapIntensityProps[index]))
                    {
                        EditorGUI.BeginChangeCheck();
                        float intensity = EditorGUILayout.Slider(L("強度", "Intensity"), targetMaterial.GetFloat(matcapIntensityProps[index]), 0f, 2f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change MatCap Intensity");
                            targetMaterial.SetFloat(matcapIntensityProps[index], intensity);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }

                    if (targetMaterial.HasProperty(matcapBlendModeProps[index]))
                    {
                        EditorGUI.BeginChangeCheck();
                        int mode = (int)targetMaterial.GetFloat(matcapBlendModeProps[index]);
                        mode = EditorGUILayout.Popup(L("ブレンドモード", "Blend Mode"), mode, BlendModeNames);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change MatCap Blend Mode");
                            targetMaterial.SetFloat(matcapBlendModeProps[index], mode);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }

                    if (targetMaterial.HasProperty(matcapBlendProps[index]))
                    {
                        EditorGUI.BeginChangeCheck();
                        float blend = EditorGUILayout.Slider(L("ブレンド", "Blend"), targetMaterial.GetFloat(matcapBlendProps[index]), 0f, 1f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change MatCap Blend");
                            targetMaterial.SetFloat(matcapBlendProps[index], blend);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBatchOperations()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("一括操作", "Batch Operations"), EditorStyles.boldLabel);

            if (GUILayout.Button(L("すべてクリア", "Clear All"), GUILayout.Height(28)))
            {
                Undo.RecordObject(targetMaterial, "Clear All MatCaps");
                for (int i = 0; i < matcapTextureProps.Length; i++)
                {
                    if (targetMaterial.HasProperty(matcapTextureProps[i]))
                    {
                        targetMaterial.SetTexture(matcapTextureProps[i], null);
                    }
                    SetLayerEnabled(i, false);
                }
                EditorUtility.SetDirty(targetMaterial);
            }

            EditorGUILayout.EndVertical();
        }

        private void SetLayerEnabled(int index, bool enabled)
        {
            if (targetMaterial.HasProperty(matcapEnableProps[index]))
            {
                targetMaterial.SetFloat(matcapEnableProps[index], enabled ? 1f : 0f);
            }

            if (enabled)
            {
                targetMaterial.EnableKeyword(matcapEnableKeywords[index]);
            }
            else
            {
                targetMaterial.DisableKeyword(matcapEnableKeywords[index]);
            }
        }
    }
}
