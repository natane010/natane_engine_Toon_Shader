using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NataneToon.MaterialSystem;

namespace NataneToon.Editor
{
    /// <summary>
    /// Material Preset Browser for easy material management
    /// Provides visual preset selection, filtering, and application
    /// </summary>
    public class MaterialPresetBrowser : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchQuery = "";
        private PresetCategory selectedCategory = PresetCategory.Custom;
        private List<NataneToonMaterialPreset> allPresets = new List<NataneToonMaterialPreset>();
        private List<NataneToonMaterialPreset> filteredPresets = new List<NataneToonMaterialPreset>();
        private Material selectedMaterial;
        private NataneToonMaterialPreset selectedPreset;

        private const float THUMBNAIL_SIZE = 100f;
        private const float PRESET_CARD_HEIGHT = 140f;
        private const int PRESETS_PER_ROW = 4;

        [MenuItem("Tools/Natane/Material Preset Browser", false, 50)]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialPresetBrowser>("マテリアルプリセット");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshPresetList();

            // Auto-select material if one is selected in the project
            if (Selection.activeObject is Material)
            {
                selectedMaterial = Selection.activeObject as Material;
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(5);
            DrawMaterialSelection();
            EditorGUILayout.Space(5);
            DrawFilterBar();
            EditorGUILayout.Space(10);
            DrawPresetGrid();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshPresetList();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("プリセット作成", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ShowCreatePresetDialog();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("ファイルからインポート", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                ImportMaterialFromFile();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("ヘルプ", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ShowHelp();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ターゲットマテリアル", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            selectedMaterial = (Material)EditorGUILayout.ObjectField(
                selectedMaterial,
                typeof(Material),
                false,
                GUILayout.Height(60));

            if (EditorGUI.EndChangeCheck() && selectedMaterial != null)
            {
                // Validate shader
                if (!selectedMaterial.shader.name.Contains("Natane") || !selectedMaterial.shader.name.Contains("Toon"))
                {
                    EditorUtility.DisplayDialog(
                        "警告",
                        $"選択したマテリアル '{selectedMaterial.name}' は Natane Toon Shader を使用していません。\n\n" +
                        "プリセットが正しく適用されない可能性があります。",
                        "OK");
                }
            }

            if (selectedMaterial == null)
            {
                EditorGUILayout.HelpBox("プリセットを適用するマテリアルを選択してください", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("このマテリアルからプリセットを作成", GUILayout.Height(25)))
                {
                    CreatePresetFromMaterial(selectedMaterial);
                }

                if (GUILayout.Button("ファイルにエクスポート", GUILayout.Height(25)))
                {
                    ExportMaterialToFile(selectedMaterial);
                }

                if (GUILayout.Button("クリップボードにコピー", GUILayout.Height(25)))
                {
                    CopyMaterialToClipboard(selectedMaterial);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("クリップボードから貼り付け", GUILayout.Height(25)))
                {
                    PasteMaterialFromClipboard(selectedMaterial);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal();

            // Category filter
            EditorGUILayout.LabelField("カテゴリ:", GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            selectedCategory = (PresetCategory)EditorGUILayout.EnumPopup(selectedCategory, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                FilterPresets();
            }

            GUILayout.Space(10);

            // Search bar
            EditorGUILayout.LabelField("検索:", GUILayout.Width(60));
            EditorGUI.BeginChangeCheck();
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                FilterPresets();
            }

            if (GUILayout.Button("クリア", GUILayout.Width(50)))
            {
                searchQuery = "";
                selectedCategory = PresetCategory.Custom;
                FilterPresets();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"{filteredPresets.Count} 件のプリセットが見つかりました", EditorStyles.miniLabel);
        }

        private void DrawPresetGrid()
        {
            if (filteredPresets.Count == 0)
            {
                EditorGUILayout.HelpBox("プリセットが見つかりません。新しく作成するか、フィルターを調整してください。", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            int columns = PRESETS_PER_ROW;
            int rows = Mathf.CeilToInt((float)filteredPresets.Count / columns);

            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index >= filteredPresets.Count) break;

                    DrawPresetCard(filteredPresets[index]);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPresetCard(NataneToonMaterialPreset preset)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(THUMBNAIL_SIZE + 20), GUILayout.Height(PRESET_CARD_HEIGHT));

            // Thumbnail
            Rect thumbnailRect = GUILayoutUtility.GetRect(THUMBNAIL_SIZE, THUMBNAIL_SIZE);
            if (preset.thumbnail != null)
            {
                GUI.DrawTexture(thumbnailRect, preset.thumbnail, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(thumbnailRect, new Color(0.2f, 0.2f, 0.2f));
                GUI.Label(thumbnailRect, "プレビュー無し", EditorStyles.centeredGreyMiniLabel);
            }

            // Preset name
            GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            EditorGUILayout.LabelField(preset.presetName, nameStyle, GUILayout.Height(30));

            // Apply button
            bool canApply = selectedMaterial != null;
            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("適用", GUILayout.Height(20)))
                {
                    ApplyPreset(preset);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void RefreshPresetList()
        {
            allPresets.Clear();

            string[] guids = AssetDatabase.FindAssets("t:NataneToonMaterialPreset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<NataneToonMaterialPreset>(path);
                if (preset != null)
                {
                    allPresets.Add(preset);
                }
            }

            allPresets = allPresets.OrderBy(p => p.category).ThenBy(p => p.presetName).ToList();
            FilterPresets();

            Debug.Log($"[MaterialPresetBrowser] Found {allPresets.Count} presets");
        }

        private void FilterPresets()
        {
            filteredPresets = allPresets.Where(p =>
            {
                // Category filter
                bool categoryMatch = selectedCategory == PresetCategory.Custom || p.category == selectedCategory;

                // Search filter
                bool searchMatch = string.IsNullOrEmpty(searchQuery) ||
                                   p.presetName.ToLower().Contains(searchQuery.ToLower()) ||
                                   p.description.ToLower().Contains(searchQuery.ToLower());

                return categoryMatch && searchMatch;
            }).ToList();
        }

        private void ApplyPreset(NataneToonMaterialPreset preset)
        {
            if (selectedMaterial == null || preset == null) return;

            Undo.RecordObject(selectedMaterial, "Apply Material Preset");
            preset.ApplyToMaterial(selectedMaterial);
            EditorUtility.SetDirty(selectedMaterial);

            EditorUtility.DisplayDialog(
                "プリセット適用完了",
                $"プリセット '{preset.presetName}' をマテリアル '{selectedMaterial.name}' に正常に適用しました。",
                "OK");
        }

        private void ShowCreatePresetDialog()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "マテリアルプリセットを作成",
                "新規マテリアルプリセット",
                "asset",
                "プリセットの保存先を選択してください");

            if (!string.IsNullOrEmpty(path))
            {
                var preset = CreateInstance<NataneToonMaterialPreset>();
                preset.presetName = Path.GetFileNameWithoutExtension(path);
                preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");

                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorGUIUtility.PingObject(preset);
                Selection.activeObject = preset;

                RefreshPresetList();
            }
        }

        private void CreatePresetFromMaterial(Material material)
        {
            string defaultName = $"{material.name}_Preset";
            string path = EditorUtility.SaveFilePanelInProject(
                "マテリアルからプリセットを作成",
                defaultName,
                "asset",
                "プリセットの保存先を選択してください");

            if (!string.IsNullOrEmpty(path))
            {
                var preset = CreateInstance<NataneToonMaterialPreset>();
                preset.presetName = Path.GetFileNameWithoutExtension(path);
                preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");
                preset.CreateFromMaterial(material);

                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorGUIUtility.PingObject(preset);
                RefreshPresetList();

                EditorUtility.DisplayDialog(
                    "プリセット作成完了",
                    $"マテリアル '{material.name}' からプリセット '{preset.presetName}' を作成しました。",
                    "OK");
            }
        }

        private void ExportMaterialToFile(Material material)
        {
            string defaultFolder = MaterialParameterShareSystem.GetDefaultExportFolder();
            string defaultName = $"{material.name}_Parameters";
            string extension = MaterialParameterShareSystem.GetFileExtension();

            string path = EditorUtility.SaveFilePanel(
                "マテリアルパラメータをエクスポート",
                defaultFolder,
                defaultName,
                extension.TrimStart('.'));

            if (!string.IsNullOrEmpty(path))
            {
                string result = MaterialParameterShareSystem.ExportToFile(material, path);
                if (!string.IsNullOrEmpty(result))
                {
                    EditorUtility.DisplayDialog(
                        "エクスポート成功",
                        $"マテリアルパラメータをエクスポートしました:\n{result}",
                        "OK");
                }
            }
        }

        private void ImportMaterialFromFile()
        {
            if (selectedMaterial == null)
            {
                EditorUtility.DisplayDialog(
                    "マテリアルが選択されていません",
                    "先にターゲットマテリアルを選択してください",
                    "OK");
                return;
            }

            string defaultFolder = MaterialParameterShareSystem.GetDefaultExportFolder();
            string extension = MaterialParameterShareSystem.GetFileExtension();

            string path = EditorUtility.OpenFilePanel(
                "マテリアルパラメータをインポート",
                defaultFolder,
                extension.TrimStart('.'));

            if (!string.IsNullOrEmpty(path))
            {
                Undo.RecordObject(selectedMaterial, "Import Material Parameters");
                bool success = MaterialParameterShareSystem.ImportFromFile(path, selectedMaterial);

                if (success)
                {
                    EditorUtility.SetDirty(selectedMaterial);
                    EditorUtility.DisplayDialog(
                        "インポート成功",
                        $"マテリアルパラメータを '{selectedMaterial.name}' にインポートしました。",
                        "OK");
                }
            }
        }

        private void CopyMaterialToClipboard(Material material)
        {
            bool success = MaterialParameterShareSystem.CopyToClipboard(material);
            if (success)
            {
                EditorUtility.DisplayDialog(
                    "クリップボードにコピーしました",
                    $"マテリアル '{material.name}' のパラメータをクリップボードにコピーしました。\n\n" +
                    "これで他のマテリアルに貼り付けるか、他の人と共有できます。",
                    "OK");
            }
        }

        private void PasteMaterialFromClipboard(Material material)
        {
            if (!MaterialParameterShareSystem.IsClipboardValid())
            {
                EditorUtility.DisplayDialog(
                    "無効なクリップボード",
                    "クリップボードに有効なマテリアルパラメータデータが含まれていません。",
                    "OK");
                return;
            }

            var info = MaterialParameterShareSystem.GetClipboardInfo();
            bool proceed = EditorUtility.DisplayDialog(
                "マテリアルパラメータを貼り付け",
                $"以下のパラメータから貼り付けます:\n\n" +
                $"マテリアル: {info.materialName}\n" +
                $"エクスポート者: {info.exportedBy}\n" +
                $"エクスポート日時: {info.exportDate}\n" +
                $"備考: {info.notes}\n\n" +
                $"これにより '{material.name}' の現在の設定が上書きされます。",
                "貼り付け",
                "キャンセル");

            if (proceed)
            {
                Undo.RecordObject(material, "Paste Material Parameters");
                bool success = MaterialParameterShareSystem.PasteFromClipboard(material);

                if (success)
                {
                    EditorUtility.SetDirty(material);
                }
            }
        }

        private void ShowHelp()
        {
            EditorUtility.DisplayDialog(
                "マテリアルプリセットブラウザ - ヘルプ",
                "マテリアルプリセットブラウザ\n\n" +
                "機能:\n" +
                "• マテリアルプリセットを視覚的に参照して適用\n" +
                "• カテゴリでフィルタリング、名前で検索\n" +
                "• 既存マテリアルからプリセットを作成\n" +
                "• ファイルまたはクリップボード経由でパラメータを共有\n\n" +
                "使い方:\n" +
                "1. ターゲットマテリアルを選択\n" +
                "2. プリセットを参照して 'Apply' をクリック\n" +
                "3. マテリアルから独自のプリセットを作成\n" +
                "4. エクスポート/インポートで他のユーザーと共有\n\n" +
                "ヒント: クリップボード機能でクイック共有できます!",
                "OK");
        }
    }
}
