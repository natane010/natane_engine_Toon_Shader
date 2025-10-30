using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using NataneToon.MaterialSystem;

namespace NataneToon.Editor
{
    /// <summary>
    /// Color palette management window
    /// Manage project-wide color schemes and apply to materials
    /// </summary>
    public class ColorPaletteManager : EditorWindow
    {
        private ColorPalette currentPalette;
        private Vector2 scrollPosition;
        private List<Material> linkedMaterials = new List<Material>();

        [MenuItem("Tools/Natane/Color Palette Manager", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ColorPaletteManager>("カラーパレット");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("カラーパレットマネージャー", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("プロジェクトの色を管理し、マテリアル全体に同期します", MessageType.Info);

            EditorGUILayout.Space(10);

            // Palette selection
            currentPalette = (ColorPalette)EditorGUILayout.ObjectField(
                "現在のパレット", currentPalette, typeof(ColorPalette), false);

            if (currentPalette == null)
            {
                if (GUILayout.Button("新しいパレットを作成", GUILayout.Height(25)))
                {
                    CreateNewPalette();
                }
                return;
            }

            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Draw colors
            SerializedObject so = new SerializedObject(currentPalette);
            SerializedProperty colorsProp = so.FindProperty("colors");

            for (int i = 0; i < colorsProp.arraySize; i++)
            {
                SerializedProperty colorEntry = colorsProp.GetArrayElementAtIndex(i);
                DrawColorEntry(colorEntry, i);
            }

            EditorGUILayout.EndScrollView();

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            // Actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("色を追加")) AddColor();
            if (GUILayout.Button("選択されたマテリアルに適用")) ApplyToSelected();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColorEntry(SerializedProperty entry, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            SerializedProperty nameProp = entry.FindPropertyRelative("name");
            SerializedProperty colorProp = entry.FindPropertyRelative("color");

            nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.Width(150));
            colorProp.colorValue = EditorGUILayout.ColorField(colorProp.colorValue);

            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                RemoveColor(index);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void AddColor()
        {
            Undo.RecordObject(currentPalette, "色を追加");
            currentPalette.colors.Add(new ColorPalette.ColorEntry { name = $"色 {currentPalette.colors.Count + 1}" });
            EditorUtility.SetDirty(currentPalette);
        }

        private void RemoveColor(int index)
        {
            Undo.RecordObject(currentPalette, "色を削除");
            currentPalette.colors.RemoveAt(index);
            EditorUtility.SetDirty(currentPalette);
        }

        private void ApplyToSelected()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Material mat)
                {
                    if (currentPalette.colors.Count > 0)
                    {
                        Undo.RecordObject(mat, "パレットを適用");
                        mat.SetColor("_Color", currentPalette.colors[0].color);
                        EditorUtility.SetDirty(mat);
                    }
                }
            }
        }

        private void CreateNewPalette()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "カラーパレットを作成", "NewPalette", "asset", "パレットを作成");
            if (!string.IsNullOrEmpty(path))
            {
                var palette = CreateInstance<ColorPalette>();
                AssetDatabase.CreateAsset(palette, path);
                currentPalette = palette;
            }
        }
    }
}
