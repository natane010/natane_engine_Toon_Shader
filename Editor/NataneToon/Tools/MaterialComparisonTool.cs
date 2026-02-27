using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
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

        [MenuItem("Tools/Natane/マテリアル Material/マテリアル比較 Material Comparison Tool", false, 14)]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialComparisonTool>(L("マテリアル比較", "Material Comparison Tool"));
            window.minSize = new Vector2(600, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(L("マテリアル比較ツール", "Material Comparison Tool"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L("2つのマテリアルを比較", "Compare two materials"), MessageType.Info);
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
            EditorGUILayout.LabelField(L("マテリアル選択", "Material Selection"), EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(L("マテリアルA", "Material A"), EditorStyles.boldLabel);
            materialA = (Material)EditorGUILayout.ObjectField(materialA, typeof(Material), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(L("マテリアルB", "Material B"), EditorStyles.boldLabel);
            materialB = (Material)EditorGUILayout.ObjectField(materialB, typeof(Material), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button(L("比較を実行", "Run Comparison"), GUILayout.Height(25)))
            {
                CompareMaterials();
            }

            if (materialA != null && materialB != null)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button(L("AからBにコピー", "Copy A to B")))
                {
                    if (EditorUtility.DisplayDialog(L("確認", "Confirm"), L("マテリアルAの設定をBにコピーしますか？", "Copy settings from Material A to B?"), L("はい", "Yes"), L("いいえ", "No")))
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
            if (GUILayout.Toggle(viewMode == ViewMode.SideBySide, L("並べて表示", "Side by Side"), EditorStyles.toolbarButton))
                viewMode = ViewMode.SideBySide;
            if (GUILayout.Toggle(viewMode == ViewMode.Diff, L("差分", "Diff"), EditorStyles.toolbarButton))
                viewMode = ViewMode.Diff;
            if (GUILayout.Toggle(viewMode == ViewMode.Parameters, L("パラメータ", "Parameters"), EditorStyles.toolbarButton))
                viewMode = ViewMode.Parameters;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSideBySideView()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 2 - 10));
            EditorGUILayout.LabelField(L("マテリアルA", "Material A"), EditorStyles.boldLabel);
            DrawMaterialInfo(materialA);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 2 - 10));
            EditorGUILayout.LabelField(L("マテリアルB", "Material B"), EditorStyles.boldLabel);
            DrawMaterialInfo(materialB);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiffView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("差分", "Differences"), EditorStyles.boldLabel);

            if (differentProperties.Count == 0)
            {
                EditorGUILayout.HelpBox(L("差分がありません - マテリアルは同じ設定です", "No differences - Materials have the same settings"), MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(L($"{differentProperties.Count}個の差分が見つかりました", $"{differentProperties.Count} differences found"), MessageType.Warning);

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
            EditorGUILayout.LabelField(L("パラメータ一覧", "Parameter List"), EditorStyles.boldLabel);

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

            EditorGUILayout.LabelField($"{L("名前", "Name")}: {mat.name}");
            EditorGUILayout.LabelField($"{L("シェーダー", "Shader")}: {mat.shader.name}");

            EditorGUILayout.Space(5);

            int featureCount = CountActiveFeatures(mat);
            EditorGUILayout.LabelField($"{L("有効な機能", "Active Features")}: {featureCount}");

            EditorGUILayout.Space(5);

            if (mat.HasProperty("_Color"))
            {
                EditorGUILayout.ColorField(L("メインカラー", "Main Color"), mat.GetColor("_Color"));
            }
        }

        private void CompareMaterials()
        {
            differentProperties.Clear();

            if (materialA == null || materialB == null) return;

            // Compare shader
            if (materialA.shader != materialB.shader)
            {
                differentProperties.Add($"{L("シェーダー", "Shader")}: {materialA.shader.name} != {materialB.shader.name}");
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
                L("コピー完了", "Copy Complete"),
                L("マテリアル設定をコピーしました", "Material settings have been copied"),
                "OK");
        }

        private int CountActiveFeatures(Material mat)
        {
            int count = 0;
            string[] keywords = { "_SPECULAR", "_RIM_LIGHT", "_SSS", "_MATCAP", "_EMISSION", "_NORMALMAP",
                                  "_REFLECTION", "_ENV_RIM", "_PARALLAX", "_REFRACTION",
                                  "_DETAIL_MAP", "_TRIPLANAR", "_HEIGHT_FOG",
                                  "_SURFACE_COVER", "_MIRROR_CONTROL", "_QUEST_LITE",
                                  "_WATER_DRIP", "_VIDEO_TEXTURE", "_INTERSECTION_FADE" };

            foreach (var keyword in keywords)
            {
                if (mat.IsKeywordEnabled(keyword))
                    count++;
            }

            return count;
        }
    }
}
