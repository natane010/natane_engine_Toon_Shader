using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;
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

        private readonly List<DifferenceInfo> differences = new List<DifferenceInfo>();
        private static readonly string[][] FloatPropertyAliases =
        {
            new[] { "_ShadowSteps", "_ToonSteps" },
            new[] { "_ShadowSharpness", "_ToonSharpness" },
            new[] { "_ShadowReceive" },
            new[] { "_OutlineWidth" },
            new[] { "_SpecularBlend", "_SpecularIntensity" },
            new[] { "_RimIntensity" },
            new[] { "_EmissionGlow", "_EmissionIntensity" }
        };
        private static readonly string[] ColorProperties =
        {
            "_Color", "_BaseColor", "_ShadowColor", "_Shadow1Color", "_Shadow2Color",
            "_RimColor", "_RimColor2", "_EmissionColor", "_SpecularColor"
        };

        private class DifferenceInfo
        {
            public string Label;
            public string ValueA;
            public string ValueB;
            public bool HasColorPreview;
            public Color ColorA;
            public Color ColorB;
        }

        [MenuItem("Tools/Natane/マテリアル Material/マテリアル比較 Material Comparison Tool", false, 14)]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialComparisonTool>(L("マテリアル比較", "Material Comparison Tool"));
            window.minSize = new Vector2(WINDOW_WIDTH_STANDARD, 600);
            window.Show();
        }

        private void OnGUI()
        {
            NataneToonShaderGUIUtility.DrawToolHeader("マテリアル比較ツール", "Material Comparison Tool", nameof(MaterialComparisonTool));
            EditorGUILayout.HelpBox(L("2つのマテリアルを比較して、差分やコピー対象を確認します。", "Compare two materials and inspect the differences before copying."), MessageType.Info);
            EditorGUILayout.Space(SPACE_STANDARD);

            DrawMaterialSelection();
            EditorGUILayout.Space(SPACE_STANDARD);

            if (materialA != null && materialB != null)
            {
                DrawViewModeSelector();
                EditorGUILayout.Space(SPACE_STANDARD);

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

            EditorGUILayout.Space(SPACE_SMALL);

            if (GUILayout.Button(L("比較を実行", "Run Comparison"), GUILayout.Height(BUTTON_HEIGHT_STANDARD)))
            {
                CompareMaterials();
            }

            if (materialA != null && materialB != null)
            {
                EditorGUILayout.Space(SPACE_SMALL);
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
            bool stackedLayout = position.width < 780f;
            float columnWidth = Mathf.Max(250f, (position.width - 32f) * 0.5f);

            if (stackedLayout)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawMaterialInfoCard(L("マテリアルA", "Material A"), materialA);
                    EditorGUILayout.Space(6f);
                    DrawMaterialInfoCard(L("マテリアルB", "Material B"), materialB);
                }
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawMaterialInfoCard(L("マテリアルA", "Material A"), materialA, columnWidth);
            GUILayout.Space(8f);
            DrawMaterialInfoCard(L("マテリアルB", "Material B"), materialB, columnWidth);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiffView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("差分", "Differences"), EditorStyles.boldLabel);

            if (differences.Count == 0)
            {
                EditorGUILayout.HelpBox(L("差分がありません - マテリアルは同じ設定です", "No differences - Materials have the same settings"), MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(L($"{differences.Count}個の差分が見つかりました", $"{differences.Count} differences found"), MessageType.Warning);

                foreach (var difference in differences)
                {
                    DrawDifferenceEntry(difference);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawParametersView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("パラメータ一覧", "Parameter List"), EditorStyles.boldLabel);

            // Compare float properties
            foreach (var aliases in FloatPropertyAliases)
            {
                string propA = GetFirstExistingProperty(materialA, aliases);
                string propB = GetFirstExistingProperty(materialB, aliases);
                if (!string.IsNullOrEmpty(propA) && !string.IsNullOrEmpty(propB))
                {
                    float valueA = materialA.GetFloat(propA);
                    float valueB = materialB.GetFloat(propB);
                    bool isDifferent = !Mathf.Approximately(valueA, valueB);
                    string displayName = aliases[0];

                    GUI.backgroundColor = isDifferent ? new Color(1f, 0.8f, 0.8f) : Color.white;
                    DrawValueComparisonRow(displayName, valueA.ToString("F3"), valueB.ToString("F3"), isDifferent);
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

            EditorGUILayout.Space(SPACE_SMALL);

            int featureCount = CountActiveFeatures(mat);
            EditorGUILayout.LabelField($"{L("有効な機能", "Active Features")}: {featureCount}");

            EditorGUILayout.Space(SPACE_SMALL);

            if (mat.HasProperty("_Color"))
            {
                EditorGUILayout.ColorField(L("メインカラー", "Main Color"), mat.GetColor("_Color"));
            }
        }

        private void DrawMaterialInfoCard(string title, Material mat, float? width = null)
        {
            GUILayoutOption[] options = width.HasValue
                ? new[] { GUILayout.Width(width.Value), GUILayout.MinHeight(160f) }
                : new[] { GUILayout.MinHeight(160f) };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, options);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            DrawMaterialInfo(mat);
            EditorGUILayout.EndVertical();
        }

        private void CompareMaterials()
        {
            differences.Clear();

            if (materialA == null || materialB == null) return;

            // Compare shader
            if (materialA.shader != materialB.shader)
            {
                AddDifference(L("シェーダー", "Shader"), materialA.shader.name, materialB.shader.name);
            }

            // Compare float properties
            foreach (var aliases in FloatPropertyAliases)
            {
                string propA = GetFirstExistingProperty(materialA, aliases);
                string propB = GetFirstExistingProperty(materialB, aliases);
                if (!string.IsNullOrEmpty(propA) && !string.IsNullOrEmpty(propB))
                {
                    float valueA = materialA.GetFloat(propA);
                    float valueB = materialB.GetFloat(propB);
                    if (!Mathf.Approximately(valueA, valueB))
                    {
                        AddDifference(aliases[0], valueA.ToString("F3"), valueB.ToString("F3"));
                    }
                }
            }

            // Compare color properties
            foreach (var prop in ColorProperties)
            {
                if (materialA.HasProperty(prop) && materialB.HasProperty(prop))
                {
                    Color colorA = materialA.GetColor(prop);
                    Color colorB = materialB.GetColor(prop);
                    if (colorA != colorB)
                    {
                        AddColorDifference(prop, colorA, colorB);
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

        private void DrawDifferenceEntry(DifferenceInfo difference)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(difference.Label, EditorStyles.boldLabel);
                DrawValueComparisonRow(L("比較値", "Compared Values"), difference.ValueA, difference.ValueB, true, difference.HasColorPreview, difference.ColorA, difference.ColorB);
            }
        }

        private void DrawValueComparisonRow(string label, string valueA, string valueB, bool isDifferent, bool hasColorPreview = false, Color colorA = default, Color colorB = default)
        {
            bool stackedLayout = position.width < 720f;

            if (stackedLayout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
                DrawValueCell(L("A", "A"), valueA, hasColorPreview, colorA);
                DrawValueCell(L("B", "B"), valueB, hasColorPreview, colorB);
                if (isDifferent)
                {
                    EditorGUILayout.LabelField(L("差分あり", "Different"), EditorStyles.miniBoldLabel);
                }
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, GUILayout.MinWidth(120f), GUILayout.MaxWidth(180f));
            DrawValueCell(L("A", "A"), valueA, hasColorPreview, colorA, true);
            DrawValueCell(L("B", "B"), valueB, hasColorPreview, colorB, true);
            if (isDifferent)
            {
                EditorGUILayout.LabelField("✗", EditorStyles.boldLabel, GUILayout.Width(20f));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawValueCell(string prefix, string value, bool hasColorPreview, Color color, bool inline = false)
        {
            if (inline)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.MinWidth(120f));
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
            }

            EditorGUILayout.LabelField($"{prefix}: {value}", EditorStyles.wordWrappedMiniLabel);
            if (hasColorPreview)
            {
                Color previewColor = GUI.color;
                GUI.color = color;
                GUILayout.Box(GUIContent.none, GUILayout.Width(18f), GUILayout.Height(18f));
                GUI.color = previewColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void AddDifference(string label, string valueA, string valueB)
        {
            differences.Add(new DifferenceInfo
            {
                Label = label,
                ValueA = valueA,
                ValueB = valueB
            });
        }

        private void AddColorDifference(string label, Color colorA, Color colorB)
        {
            differences.Add(new DifferenceInfo
            {
                Label = label,
                ValueA = FormatColor(colorA),
                ValueB = FormatColor(colorB),
                HasColorPreview = true,
                ColorA = colorA,
                ColorB = colorB
            });
        }

        private static string FormatColor(Color color)
        {
            return $"RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})";
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

        private static string GetFirstExistingProperty(Material material, params string[] propertyNames)
        {
            if (material == null || propertyNames == null)
            {
                return null;
            }

            foreach (var propertyName in propertyNames)
            {
                if (!string.IsNullOrEmpty(propertyName) && material.HasProperty(propertyName))
                {
                    return propertyName;
                }
            }

            return null;
        }
    }
}
