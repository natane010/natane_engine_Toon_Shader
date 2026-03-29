using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Utility window for export and material assignment.
    /// 書き出し・マテリアル割当用のユーティリティウィンドウ
    /// </summary>
    internal class MaskTextureOutputWindow : EditorWindow
    {
        private Vector2 scrollPosition;

        public static void Open()
        {
            var window = GetWindow<MaskTextureOutputWindow>(true,
                L("書き出し / マテリアル割当", "Export / Material Assignment"));
            window.minSize = new Vector2(340, 300);
            window.ShowUtility();
        }

        private UVTextureGenerator FindParent()
        {
            var parents = Resources.FindObjectsOfTypeAll<UVTextureGenerator>();
            return parents.FirstOrDefault();
        }

        private void OnGUI()
        {
            var parent = FindParent();
            if (parent == null)
            {
                EditorGUILayout.HelpBox(
                    L("テクスチャスタジオを開いてください。",
                      "Please open Texture Studio first."),
                    MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Export section
            EditorGUILayout.LabelField(L("書き出し", "Export"), EditorStyles.boldLabel);
            MaskTextureExporter.DrawExportUI(parent.CurrentPreviewTexture);

            EditorGUILayout.Space(10);

            // Material Assignment section
            DrawMaterialAssignment(parent);

            EditorGUILayout.EndScrollView();
        }

        private void DrawMaterialAssignment(UVTextureGenerator parent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("マテリアル割当", "Material Assignment"), EditorStyles.boldLabel);

            Material mat = parent.CurrentTargetMaterial;
            Material newMat = (Material)EditorGUILayout.ObjectField(
                L("ターゲットマテリアル", "Target Material"), mat, typeof(Material), false);
            if (newMat != mat)
                parent.CurrentTargetMaterial = newMat;

            if (newMat != null)
            {
                List<string> availableProps = new List<string>();
                List<string> availableLabels = new List<string>();
                UVTextureGenerator.GetMaterialTexturePropertyOptions(newMat, availableProps, availableLabels);

                if (availableProps.Count > 0)
                {
                    int idx = availableProps.IndexOf(parent.CurrentPropertyName);
                    if (idx < 0)
                        idx = 0;

                    int newIdx = EditorGUILayout.Popup(
                        L("割当先プロパティ", "Target Property"), idx, availableLabels.ToArray());
                    if (newIdx != idx)
                        parent.CurrentPropertyName = availableProps[newIdx];
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        L("対応するテクスチャプロパティが見つかりません。",
                          "No compatible texture properties found."),
                        MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();

            // Live Link status
            EditorGUILayout.Space(6);
            DrawLiveLinkStatus(parent);
        }

        private void DrawLiveLinkStatus(UVTextureGenerator parent)
        {
            var link = parent.LiveLink;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                L("ライブリンク", "Live Link"), EditorStyles.boldLabel);

            if (link != null && link.IsEnabled)
            {
                EditorGUILayout.HelpBox(
                    L("接続中: " + link.BoundPropertyName,
                      "Connected: " + link.BoundPropertyName),
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField(
                    L("未接続 — コマンドバーの [LIVE] で接続",
                      "Disconnected — Use [LIVE] in command bar"));
            }
            EditorGUILayout.EndVertical();
        }
    }
}
