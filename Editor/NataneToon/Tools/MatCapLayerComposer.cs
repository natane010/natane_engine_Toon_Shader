using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// MatCap Layer Composer
    /// MatCapレイヤーコンポーザー
    /// </summary>
    public class MatCapLayerComposer : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;
        private bool[] layerFoldouts = new bool[] { true, true, true };

        private readonly string[] matcapProps = { "_MatCap", "_MatCap2", "_MatCap3" };
        private readonly string[] matcapIntensityProps = { "_MatCapIntensity", "_MatCap2Intensity", "_MatCap3Intensity" };
        private readonly string[] matcapBlendProps = { "_MatCapBlend", "_MatCap2Blend", "_MatCap3Blend" };
        private readonly string[] blendModeNames = { "加算 Add", "乗算 Multiply", "オーバーレイ Overlay" };

        [MenuItem("Tools/Natane/MatCap Layer Composer", false, 132)]
        public static void ShowWindow()
        {
            var window = GetWindow<MatCapLayerComposer>("MatCapコンポーザー MatCap");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("MatCapレイヤーコンポーザー MatCap Layer Composer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("3つのMatCapレイヤーをリアルタイムプレビュー\nReal-time preview of 3 MatCap layers", MessageType.Info);
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField("ターゲットマテリアル Target", targetMaterial, typeof(Material), false);

            if (targetMaterial == null) return;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < 3; i++)
            {
                DrawMatCapLayer(i);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.Space(10);
            DrawBatchOperations();

            EditorGUILayout.EndScrollView();
        }

        private void DrawMatCapLayer(int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            layerFoldouts[index] = EditorGUILayout.Foldout(layerFoldouts[index], $"MatCap Layer {index + 1}", true);

            if (layerFoldouts[index] && targetMaterial.HasProperty(matcapProps[index]))
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                Texture2D matcap = (Texture2D)EditorGUILayout.ObjectField("MatCap", targetMaterial.GetTexture(matcapProps[index]), typeof(Texture2D), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change MatCap");
                    targetMaterial.SetTexture(matcapProps[index], matcap);
                    EditorUtility.SetDirty(targetMaterial);
                }

                if (matcap != null)
                {
                    Rect previewRect = GUILayoutUtility.GetRect(100, 100);
                    EditorGUI.DrawPreviewTexture(previewRect, matcap, null, ScaleMode.ScaleToFit);

                    if (targetMaterial.HasProperty(matcapIntensityProps[index]))
                    {
                        EditorGUI.BeginChangeCheck();
                        float intensity = EditorGUILayout.Slider("強度 Intensity", targetMaterial.GetFloat(matcapIntensityProps[index]), 0f, 2f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change MatCap Intensity");
                            targetMaterial.SetFloat(matcapIntensityProps[index], intensity);
                            EditorUtility.SetDirty(targetMaterial);
                        }
                    }

                    if (targetMaterial.HasProperty(matcapBlendProps[index]))
                    {
                        EditorGUI.BeginChangeCheck();
                        int blend = (int)targetMaterial.GetFloat(matcapBlendProps[index]);
                        blend = EditorGUILayout.Popup("ブレンド Blend", blend, blendModeNames);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(targetMaterial, "Change Blend Mode");
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
            EditorGUILayout.LabelField("一括操作 Batch Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("すべてクリア Clear All", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("確認 Confirm", "すべてのMatCapをクリアしますか？\nClear all MatCaps?", "はい Yes", "いいえ No"))
                {
                    Undo.RecordObject(targetMaterial, "Clear All MatCaps");
                    foreach (var prop in matcapProps)
                    {
                        if (targetMaterial.HasProperty(prop))
                            targetMaterial.SetTexture(prop, null);
                    }
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
