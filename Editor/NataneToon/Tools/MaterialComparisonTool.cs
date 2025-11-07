using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// Material Comparison Tool
    /// マテリアル比較ツール
    /// </summary>
    public class MaterialComparisonTool : EditorWindow
    {
        private Material materialA;
        private Material materialB;
        private Vector2 scrollPosition;

        private enum ViewMode { SideBySide, Diff, Parameters }
        private ViewMode viewMode = ViewMode.SideBySide;

        private List<string> differentProperties = new List<string>();

        [MenuItem("Tools/Natane/Material Comparison Tool", false, 139)]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialComparisonTool>("マテリアル比較 Comparison");
            window.minSize = new Vector2(600, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("マテリアル比較ツール Material Comparison Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("2つのマテリアルを比較\nCompare two materials", MessageType.Info);
            EditorGUILayout.Space(10);

            DrawMaterialSelection();
            EditorGUILayout.Space(10);

            if (materialA != null && materialB != null)
            {
                DrawViewModeSelector();
                EditorGUILayout.Space(10);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                switch (viewMode)
                {
                    case ViewMode.SideBySide:
                        DrawSideBySideView();
                        break;
                    case ViewMode.Diff:
                        DrawDiffView();
                        break;
                    case ViewMode.Parameters:
                        DrawParametersView();
                        break;
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("マテリアル選択 Material Selection", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("マテリアルA Material A", EditorStyles.boldLabel);
            materialA = (Material)EditorGUILayout.ObjectField(materialA, typeof(Material), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("マテリアルB Material B", EditorStyles.boldLabel);
            materialB = (Material)EditorGUILayout.ObjectField(materialB, typeof(Material), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("比較を実行 Compare", GUILayout.Height(25)))
            {
                CompareMaterials();
            }

            if (materialA != null && materialB != null)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("AからBにコピー Copy A to B"))
                {
                    if (EditorUtility.DisplayDialog("確認 Confirm", "マテリアルAの設定をBにコピーしますか？\nCopy settings from A to B?", "はい Yes", "いいえ No"))
                    {
                        CopyMaterialSettings(materialA, materialB);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawViewModeSelector()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(viewMode == ViewMode.SideBySide, "並べて表示 Side by Side", EditorStyles.toolbarButton))
                viewMode = ViewMode.SideBySide;
            if (GUILayout.Toggle(viewMode == ViewMode.Diff, "差分 Diff", EditorStyles.toolbarButton))
                viewMode = ViewMode.Diff;
            if (GUILayout.Toggle(viewMode == ViewMode.Parameters, "パラメータ Parameters", EditorStyles.toolbarButton))
                viewMode = ViewMode.Parameters;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSideBySideView()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 2 - 10));
            EditorGUILayout.LabelField("マテリアルA Material A", EditorStyles.boldLabel);
            DrawMaterialInfo(materialA);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 2 - 10));
            EditorGUILayout.LabelField("マテリアルB Material B", EditorStyles.boldLabel);
            DrawMaterialInfo(materialB);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiffView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("差分 Differences", EditorStyles.boldLabel);

            if (differentProperties.Count == 0)
            {
                EditorGUILayout.HelpBox("差分がありません - マテリアルは同じ設定です\nNo differences - materials have same settings", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"{differentProperties.Count}個の差分が見つかりました\nFound {differentProperties.Count} differences", MessageType.Warning);

                foreach (var prop in differentProperties)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(prop, EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawParametersView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("パラメータ一覧 Parameter List", EditorStyles.boldLabel);

            // Compare float properties
            string[] floatProps = { "_ToonSteps", "_ToonSharpness", "_ShadowReceive", "_OutlineWidth",
                                   "_SpecularIntensity", "_RimIntensity", "_EmissionIntensity" };

            foreach (var prop in floatProps)
            {
                if (materialA.HasProperty(prop) && materialB.HasProperty(prop))
                {
                    float valueA = materialA.GetFloat(prop);
                    float valueB = materialB.GetFloat(prop);
                    bool isDifferent = !Mathf.Approximately(valueA, valueB);

                    GUI.backgroundColor = isDifferent ? new Color(1f, 0.8f, 0.8f) : Color.white;
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(prop, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"A: {valueA:F3}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"B: {valueB:F3}", GUILayout.Width(100));
                    if (isDifferent)
                        EditorGUILayout.LabelField("✗", EditorStyles.boldLabel, GUILayout.Width(20));
                    EditorGUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialInfo(Material mat)
        {
            if (mat == null) return;

            EditorGUILayout.LabelField($"名前 Name: {mat.name}");
            EditorGUILayout.LabelField($"シェーダー Shader: {mat.shader.name}");

            EditorGUILayout.Space(5);

            int featureCount = CountActiveFeatures(mat);
            EditorGUILayout.LabelField($"有効な機能 Active Features: {featureCount}");

            EditorGUILayout.Space(5);

            if (mat.HasProperty("_Color"))
            {
                EditorGUILayout.ColorField("メインカラー Main Color", mat.GetColor("_Color"));
            }
        }

        private void CompareMaterials()
        {
            differentProperties.Clear();

            if (materialA == null || materialB == null) return;

            // Compare shader
            if (materialA.shader != materialB.shader)
            {
                differentProperties.Add($"シェーダー Shader: {materialA.shader.name} != {materialB.shader.name}");
            }

            // Compare float properties
            string[] floatProps = { "_ToonSteps", "_ToonSharpness", "_ShadowReceive", "_OutlineWidth",
                                   "_SpecularIntensity", "_RimIntensity", "_EmissionIntensity" };

            foreach (var prop in floatProps)
            {
                if (materialA.HasProperty(prop) && materialB.HasProperty(prop))
                {
                    float valueA = materialA.GetFloat(prop);
                    float valueB = materialB.GetFloat(prop);
                    if (!Mathf.Approximately(valueA, valueB))
                    {
                        differentProperties.Add($"{prop}: {valueA:F3} != {valueB:F3}");
                    }
                }
            }

            // Compare color properties
            string[] colorProps = { "_Color", "_ShadowColor", "_RimColor", "_EmissionColor" };

            foreach (var prop in colorProps)
            {
                if (materialA.HasProperty(prop) && materialB.HasProperty(prop))
                {
                    Color colorA = materialA.GetColor(prop);
                    Color colorB = materialB.GetColor(prop);
                    if (colorA != colorB)
                    {
                        differentProperties.Add($"{prop}: Different colors");
                    }
                }
            }

            viewMode = ViewMode.Diff;
            Repaint();
        }

        private void CopyMaterialSettings(Material source, Material target)
        {
            Undo.RecordObject(target, "Copy Material Settings");

            // Copy all properties
            var shader = source.shader;
            int propCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propCount; i++)
            {
                string propName = ShaderUtil.GetPropertyName(shader, i);
                var propType = ShaderUtil.GetPropertyType(shader, i);

                if (!target.HasProperty(propName)) continue;

                try
                {
                    switch (propType)
                    {
                        case ShaderUtil.ShaderPropertyType.Color:
                            target.SetColor(propName, source.GetColor(propName));
                            break;
                        case ShaderUtil.ShaderPropertyType.Vector:
                            target.SetVector(propName, source.GetVector(propName));
                            break;
                        case ShaderUtil.ShaderPropertyType.Float:
                        case ShaderUtil.ShaderPropertyType.Range:
                            target.SetFloat(propName, source.GetFloat(propName));
                            break;
                        case ShaderUtil.ShaderPropertyType.TexEnv:
                            target.SetTexture(propName, source.GetTexture(propName));
                            break;
                    }
                }
                catch { }
            }

            // Copy keywords
            target.shaderKeywords = source.shaderKeywords;

            EditorUtility.SetDirty(target);

            EditorUtility.DisplayDialog(
                "コピー完了 Copied",
                "マテリアル設定をコピーしました\nCopied material settings",
                "OK");
        }

        private int CountActiveFeatures(Material mat)
        {
            int count = 0;
            string[] keywords = { "_SPECULAR", "_RIM_LIGHT", "_SSS", "_MATCAP", "_EMISSION", "_NORMALMAP" };

            foreach (var keyword in keywords)
            {
                if (mat.IsKeywordEnabled(keyword))
                    count++;
            }

            return count;
        }
    }
}
