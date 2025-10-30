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
            var window = GetWindow<MaterialPresetBrowser>("Material Presets");
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

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshPresetList();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create Preset", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ShowCreatePresetDialog();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Import from File", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                ImportMaterialFromFile();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Help", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ShowHelp();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Target Material", EditorStyles.boldLabel);

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
                        "Warning",
                        $"Selected material '{selectedMaterial.name}' is not using Natane Toon Shader.\n\n" +
                        "Presets may not apply correctly.",
                        "OK");
                }
            }

            if (selectedMaterial == null)
            {
                EditorGUILayout.HelpBox("Select a material to apply presets", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Create Preset from This Material", GUILayout.Height(25)))
                {
                    CreatePresetFromMaterial(selectedMaterial);
                }

                if (GUILayout.Button("Export to File", GUILayout.Height(25)))
                {
                    ExportMaterialToFile(selectedMaterial);
                }

                if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(25)))
                {
                    CopyMaterialToClipboard(selectedMaterial);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Paste from Clipboard", GUILayout.Height(25)))
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
            EditorGUILayout.LabelField("Category:", GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            selectedCategory = (PresetCategory)EditorGUILayout.EnumPopup(selectedCategory, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                FilterPresets();
            }

            GUILayout.Space(10);

            // Search bar
            EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
            EditorGUI.BeginChangeCheck();
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                FilterPresets();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchQuery = "";
                selectedCategory = PresetCategory.Custom;
                FilterPresets();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Found {filteredPresets.Count} presets", EditorStyles.miniLabel);
        }

        private void DrawPresetGrid()
        {
            if (filteredPresets.Count == 0)
            {
                EditorGUILayout.HelpBox("No presets found. Create one or adjust filters.", MessageType.Info);
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
                GUI.Label(thumbnailRect, "No Preview", EditorStyles.centeredGreyMiniLabel);
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
                if (GUILayout.Button("Apply", GUILayout.Height(20)))
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
                "Preset Applied",
                $"Successfully applied preset '{preset.presetName}' to material '{selectedMaterial.name}'",
                "OK");
        }

        private void ShowCreatePresetDialog()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Material Preset",
                "New Material Preset",
                "asset",
                "Choose where to save the preset");

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
                "Create Preset from Material",
                defaultName,
                "asset",
                "Choose where to save the preset");

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
                    "Preset Created",
                    $"Created preset '{preset.presetName}' from material '{material.name}'",
                    "OK");
            }
        }

        private void ExportMaterialToFile(Material material)
        {
            string defaultFolder = MaterialParameterShareSystem.GetDefaultExportFolder();
            string defaultName = $"{material.name}_Parameters";
            string extension = MaterialParameterShareSystem.GetFileExtension();

            string path = EditorUtility.SaveFilePanel(
                "Export Material Parameters",
                defaultFolder,
                defaultName,
                extension.TrimStart('.'));

            if (!string.IsNullOrEmpty(path))
            {
                string result = MaterialParameterShareSystem.ExportToFile(material, path);
                if (!string.IsNullOrEmpty(result))
                {
                    EditorUtility.DisplayDialog(
                        "Export Successful",
                        $"Material parameters exported to:\n{result}",
                        "OK");
                }
            }
        }

        private void ImportMaterialFromFile()
        {
            if (selectedMaterial == null)
            {
                EditorUtility.DisplayDialog(
                    "No Material Selected",
                    "Please select a target material first",
                    "OK");
                return;
            }

            string defaultFolder = MaterialParameterShareSystem.GetDefaultExportFolder();
            string extension = MaterialParameterShareSystem.GetFileExtension();

            string path = EditorUtility.OpenFilePanel(
                "Import Material Parameters",
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
                        "Import Successful",
                        $"Material parameters imported to '{selectedMaterial.name}'",
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
                    "Copied to Clipboard",
                    $"Material '{material.name}' parameters copied to clipboard.\n\n" +
                    "You can now paste these parameters to another material or share with others.",
                    "OK");
            }
        }

        private void PasteMaterialFromClipboard(Material material)
        {
            if (!MaterialParameterShareSystem.IsClipboardValid())
            {
                EditorUtility.DisplayDialog(
                    "Invalid Clipboard",
                    "Clipboard does not contain valid material parameter data.",
                    "OK");
                return;
            }

            var info = MaterialParameterShareSystem.GetClipboardInfo();
            bool proceed = EditorUtility.DisplayDialog(
                "Paste Material Parameters",
                $"Paste parameters from:\n\n" +
                $"Material: {info.materialName}\n" +
                $"Exported by: {info.exportedBy}\n" +
                $"Export date: {info.exportDate}\n" +
                $"Notes: {info.notes}\n\n" +
                $"This will overwrite current settings of '{material.name}'",
                "Paste",
                "Cancel");

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
                "Material Preset Browser - Help",
                "Material Preset Browser\n\n" +
                "Features:\n" +
                "• Browse and apply material presets visually\n" +
                "• Filter by category and search by name\n" +
                "• Create presets from existing materials\n" +
                "• Share parameters via file or clipboard\n\n" +
                "Usage:\n" +
                "1. Select a target material\n" +
                "2. Browse presets and click 'Apply'\n" +
                "3. Create your own presets from materials\n" +
                "4. Share with others using Export/Import\n\n" +
                "Tip: Use clipboard copy/paste for quick sharing!",
                "OK");
        }
    }
}
