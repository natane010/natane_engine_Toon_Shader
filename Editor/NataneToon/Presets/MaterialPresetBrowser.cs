using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NataneToon.MaterialSystem;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

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
        private bool showAllCategories = true;

        private const float THUMBNAIL_SIZE = 100f;
        private const float PRESET_CARD_HEIGHT = 140f;
        private const int PRESETS_PER_ROW = 4;

        [MenuItem("Tools/Natane/プリセット Presets/Material Preset Browser _p", false, 21)]
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
            NataneToonShaderGUIUtility.DrawToolHeader("マテリアルプリセットブラウザ", "Material Preset Browser", "MaterialPresetBrowser");
            EditorGUILayout.Space(5);
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

            // VTuber Presets Generation Button
            var buttonStyle = new GUIStyle(EditorStyles.toolbarButton);
            buttonStyle.normal.textColor = new Color(0.3f, 0.8f, 0.3f); // Green color
            buttonStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button(new GUIContent("Generate VTuber Presets", "Generate 5 VTuber-optimized material presets"),
                buttonStyle, GUILayout.Width(160)))
            {
                GenerateVTuberPresets();
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

            // "All" toggle
            EditorGUI.BeginChangeCheck();
            showAllCategories = GUILayout.Toggle(showAllCategories, L("全て", "All"), EditorStyles.toolbarButton, GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
            {
                FilterPresets();
            }

            // Category filter (disabled when "All" is selected)
            using (new EditorGUI.DisabledScope(showAllCategories))
            {
                EditorGUILayout.LabelField("Category:", GUILayout.Width(70));
                EditorGUI.BeginChangeCheck();
                selectedCategory = (PresetCategory)EditorGUILayout.EnumPopup(selectedCategory, GUILayout.Width(200));
                if (EditorGUI.EndChangeCheck())
                {
                    showAllCategories = false;
                    FilterPresets();
                }
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
                showAllCategories = true;
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
                bool categoryMatch = showAllCategories || p.category == selectedCategory;

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
            NataneToonMaterialPresetEditor.ApplyPresetWithUIUpdate(preset, selectedMaterial);

            EditorUtility.DisplayDialog(
                L("プリセット適用完了", "Preset Applied"),
                L($"プリセット '{preset.presetName}' をマテリアル '{selectedMaterial.name}' に適用しました。\n" +
                $"インスペクターUIは有効な機能を表示するように更新されました。",
                $"Successfully applied preset '{preset.presetName}' to material '{selectedMaterial.name}'\n" +
                $"Inspector UI has been updated to show active features."),
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
                "• Share parameters via file or clipboard\n" +
                "• Generate VTuber-optimized presets\n\n" +
                "Usage:\n" +
                "1. Select a target material\n" +
                "2. Browse presets and click 'Apply'\n" +
                "3. Create your own presets from materials\n" +
                "4. Share with others using Export/Import\n" +
                "5. Click 'Generate VTuber Presets' for optimized character presets\n\n" +
                "Tip: Use clipboard copy/paste for quick sharing!",
                "OK");
        }

        /// <summary>
        /// Generate VTuber-optimized material presets
        /// Creates 5 professional presets for VTuber character rendering
        /// </summary>
        private void GenerateVTuberPresets()
        {
            // Show confirmation dialog
            bool proceed = EditorUtility.DisplayDialog(
                L("VTuberプリセット生成", "Generate VTuber Presets"),
                L("VTuber向けの高品質マテリアルプリセットを生成します。\n\n" +
                "以下の5種類のプリセットが作成されます：\n" +
                "1. キャラクター肌 - 柔らかいセルシェーディング、SSS\n" +
                "2. キャラクター髪 - ツヤのあるアニメ調ヘア\n" +
                "3. キャラクター服 - クリーンなアニメ調\n" +
                "4. キャラクター目 - キラキラした瞳\n" +
                "5. ライブパフォーマンス - 軽量・高パフォーマンス\n\n" +
                "保存先: Assets/NataneToon/Runtime/Presets/VTuber/\n\n" +
                "生成しますか？",
                "Generate high-quality VTuber material presets.\n\n" +
                "The following 5 presets will be created:\n" +
                "1. Character Skin - Soft cell shading with SSS\n" +
                "2. Character Hair - Glossy anime-style hair\n" +
                "3. Character Clothing - Clean anime style\n" +
                "4. Character Eyes - Sparkling eyes\n" +
                "5. Live Performance - Lightweight & high performance\n\n" +
                "Location: Assets/NataneToon/Runtime/Presets/VTuber/\n\n" +
                "Generate?"),
                L("生成する", "Generate"),
                L("キャンセル", "Cancel"));

            if (!proceed) return;

            // Ensure directory exists with robust creation logic
            string presetPath = "Assets/NataneToon/Runtime/Presets/VTuber";

            // Create directory using System.IO for more reliable handling
            string fullPath = System.IO.Path.GetFullPath(presetPath);
            if (!System.IO.Directory.Exists(fullPath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(fullPath);
                    Debug.Log($"[MaterialPresetBrowser] Created directory: {fullPath}");

                    // Refresh AssetDatabase to recognize new directory
                    AssetDatabase.Refresh();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[MaterialPresetBrowser] Failed to create directory: {e.Message}");
                    EditorUtility.DisplayDialog(
                        L("エラー", "Error"),
                        L($"ディレクトリの作成に失敗しました:\n{e.Message}\n\n" +
                        $"手動で以下のディレクトリを作成してください:\n{presetPath}",
                        $"Failed to create directory:\n{e.Message}\n\n" +
                        $"Please manually create the directory:\n{presetPath}"),
                        "OK");
                    return;
                }
            }

            int presetsCreated = 0;

            // Generate presets
            presetsCreated += GenerateCharacterSkinPreset(presetPath) ? 1 : 0;
            presetsCreated += GenerateCharacterHairPreset(presetPath) ? 1 : 0;
            presetsCreated += GenerateCharacterClothingPreset(presetPath) ? 1 : 0;
            presetsCreated += GenerateCharacterEyesPreset(presetPath) ? 1 : 0;
            presetsCreated += GenerateLivePerformancePreset(presetPath) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Refresh preset list to show new presets
            RefreshPresetList();

            // Auto-filter to show VTuber presets
            showAllCategories = false;
            selectedCategory = PresetCategory.Character_Skin;
            FilterPresets();

            // Show completion dialog
            EditorUtility.DisplayDialog(
                L("完了", "Complete"),
                L($"VTuber向けプリセットの生成が完了しました！\n\n" +
                $"生成されたプリセット: {presetsCreated}個\n" +
                $"保存場所: {presetPath}\n\n" +
                $"プリセットはこのブラウザに表示されています。\n" +
                $"マテリアルを選択して「Apply」ボタンで適用できます。",
                $"VTuber preset generation complete!\n\n" +
                $"Presets created: {presetsCreated}\n" +
                $"Location: {presetPath}\n\n" +
                $"Presets are now displayed in this browser.\n" +
                $"Select a material and click 'Apply' to use them."),
                "OK");

            Debug.Log($"[MaterialPresetBrowser] VTuber向けプリセットを{presetsCreated}個生成しました");
        }

        // VTuber Preset Generation Methods
        private bool GenerateCharacterSkinPreset(string basePath)
        {
            try
            {
                // Verify directory exists
                string fullBasePath = System.IO.Path.GetFullPath(basePath);
                if (!System.IO.Directory.Exists(fullBasePath))
                {
                    Debug.LogError($"[GenerateCharacterSkinPreset] Directory does not exist: {fullBasePath}");
                    return false;
                }

                var preset = CreateInstance<NataneToonMaterialPreset>();
                preset.presetName = "VTuber - キャラクター肌";
                preset.description = "VTuberキャラクターの肌に最適化されたプリセット。\n" +
                                   "・柔らかいセルシェーディング\n" +
                                   "・SSSで透明感と血色感\n" +
                                   "・リムライトで立体感\n" +
                                   "・高品質なアニメ調セルシェーディング";
                preset.category = PresetCategory.Character_Skin;
                preset.author = "Natane Toon Shader";
                preset.version = "1.0";
                preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");

                var p = preset.parameters;
                // mainColorを白にしてテクスチャの色を完全に反映
                p.mainColor = new Color(1.0f, 1.0f, 1.0f, 1f);
                p.alpha = 1f;
                // 温かみのあるピーチ系の影色
                p.shadowColor = new Color(0.95f, 0.75f, 0.68f, 1f);
                p.toonSteps = 2;
                p.toonSharpness = 0.08f;
                p.shadowReceive = 0.8f;
                p.shadowIntensityMax = 0.3f;
                p.lightInfluence = 1.2f;
                p.lightColorInfluence = 1.0f;
                p.backlight = 0.2f;
                p.useSSS = true;
                // より温かみのあるSSS色（ピンクがかったオレンジ）
                p.sssColor = new Color(1f, 0.65f, 0.55f, 1f);
                p.sssIntensity = 0.45f;
                p.sssDistortion = 0.3f;
                p.sssPower = 2.5f;
                p.sssScale = 0.8f;
                p.useRimLight = true;
                // 温かみのあるリムライト色
                p.rimColor = new Color(1f, 0.9f, 0.85f, 1f);
                p.rimIntensity = 0.6f;
                p.rimPower = 4f;
                p.useSpecular = true;
                p.specularColor = new Color(1f, 1f, 1f, 1f);
                p.specularIntensity = 0.3f;
                p.specularSize = 0.2f;
                p.specularSharpness = 0.4f;
                p.useOutline = true;
                p.outlineColor = new Color(0.3f, 0.2f, 0.15f, 1f);
                p.outlineWidth = 0.003f;
                p.renderQueue = 2000;
                p.cullMode = 2;

                string assetPath = $"{basePath}/VTuber_CharacterSkin.asset";
                AssetDatabase.CreateAsset(preset, assetPath);
                Debug.Log($"[GenerateCharacterSkinPreset] Created preset at: {assetPath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GenerateCharacterSkinPreset] Failed to generate: {e.Message}\nStack: {e.StackTrace}");
                return false;
            }
        }

        private bool GenerateCharacterHairPreset(string basePath)
        {
            try
            {
                // Verify directory exists
                string fullBasePath = System.IO.Path.GetFullPath(basePath);
                if (!System.IO.Directory.Exists(fullBasePath))
                {
                    Debug.LogError($"[GenerateCharacterHairPreset] Directory does not exist: {fullBasePath}");
                    return false;
                }

                var preset = CreateInstance<NataneToonMaterialPreset>();
                preset.presetName = "VTuber - キャラクター髪";
                preset.description = "VTuberキャラクターの髪に最適化されたプリセット。\n" +
                                   "・シャープなハイライト\n" +
                                   "・MatCapで光沢感\n" +
                                   "・アニメ調のツヤ表現";
                preset.category = PresetCategory.Character_Hair;
                preset.author = "Natane Toon Shader";
                preset.version = "1.0";
                preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");

                var p = preset.parameters;
                // mainColorを白にしてテクスチャの色を完全に反映
                p.mainColor = new Color(1.0f, 1.0f, 1.0f, 1f);
                p.alpha = 1f;
                // 温かみのあるブラウン系の影色
                p.shadowColor = new Color(0.45f, 0.35f, 0.28f, 1f);
                p.toonSteps = 2;
                p.toonSharpness = 0.05f;
                p.shadowReceive = 0.9f;
                p.shadowIntensityMax = 0.2f;
                p.lightInfluence = 1.3f;
                p.lightColorInfluence = 1.0f;
                p.backlight = 0.3f;
                p.useSpecular = true;
                p.specularColor = new Color(1f, 1f, 1f, 1f);
                p.specularIntensity = 1.2f;
                p.specularSize = 0.15f;
                p.specularSharpness = 0.9f;
                p.useMatCap = true;
                p.matCapIntensity = 0.4f;
                p.matCapBlendMode = 0;
                p.useRimLight = true;
                p.rimColor = new Color(0.8f, 0.7f, 0.6f, 1f);
                p.rimIntensity = 0.8f;
                p.rimPower = 3f;
                p.useOutline = true;
                p.outlineColor = new Color(0.1f, 0.05f, 0.05f, 1f);
                p.outlineWidth = 0.004f;
                p.renderQueue = 2000;
                p.cullMode = 0;

                string assetPath = $"{basePath}/VTuber_CharacterHair.asset";
                AssetDatabase.CreateAsset(preset, assetPath);
                Debug.Log($"[GenerateCharacterHairPreset] Created preset at: {assetPath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GenerateCharacterHairPreset] Failed to generate: {e.Message}\nStack: {e.StackTrace}");
                return false;
            }
        }

        private bool GenerateCharacterClothingPreset(string basePath)
        {
            try
            {
                // Verify directory exists
                string fullBasePath = System.IO.Path.GetFullPath(basePath);
                if (!System.IO.Directory.Exists(fullBasePath))
                {
                    Debug.LogError($"[GenerateCharacterClothingPreset] Directory does not exist: {fullBasePath}");
                    return false;
                }

                var preset = CreateInstance<NataneToonMaterialPreset>();
                preset.presetName = "VTuber - キャラクター服";
                preset.description = "VTuberキャラクターの服に最適化されたプリセット。\n" +
                                   "・クリーンなセルシェーディング\n" +
                                   "・明瞭なアウトライン\n" +
                                   "・シンプルで美しい表現";
                preset.category = PresetCategory.Character_Clothing;
                preset.author = "Natane Toon Shader";
                preset.version = "1.0";
                preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");

                var p = preset.parameters;
                // mainColorを白にしてテクスチャの色を完全に反映
                p.mainColor = new Color(1.0f, 1.0f, 1.0f, 1f);
                p.alpha = 1f;
                // やや温かみのあるグレー系の影色
                p.shadowColor = new Color(0.75f, 0.73f, 0.72f, 1f);
                p.toonSteps = 2;
                p.toonSharpness = 0.1f;
                p.shadowReceive = 1f;
                p.shadowIntensityMax = 0.25f;
                p.lightInfluence = 1f;
                p.lightColorInfluence = 1.0f;
                p.backlight = 0.1f;
                p.useSpecular = false;
                p.useRimLight = true;
                p.rimColor = new Color(1f, 1f, 1f, 1f);
                p.rimIntensity = 0.4f;
                p.rimPower = 5f;
                p.useOutline = true;
                p.outlineColor = new Color(0f, 0f, 0f, 1f);
                p.outlineWidth = 0.005f;
                p.renderQueue = 2000;
                p.cullMode = 2;

                string assetPath = $"{basePath}/VTuber_CharacterClothing.asset";
                AssetDatabase.CreateAsset(preset, assetPath);
                Debug.Log($"[GenerateCharacterClothingPreset] Created preset at: {assetPath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GenerateCharacterClothingPreset] Failed to generate: {e.Message}\nStack: {e.StackTrace}");
                return false;
            }
        }

        private bool GenerateCharacterEyesPreset(string basePath)
        {
            try
            {
                // Verify directory exists
                string fullBasePath = System.IO.Path.GetFullPath(basePath);
                if (!System.IO.Directory.Exists(fullBasePath))
                {
                    Debug.LogError($"[GenerateCharacterEyesPreset] Directory does not exist: {fullBasePath}");
                    return false;
                }

                var preset = CreateInstance<NataneToonMaterialPreset>();
                preset.presetName = "VTuber - キャラクター目";
                preset.description = "VTuberキャラクターの目に最適化されたプリセット。\n" +
                                   "・明るくキラキラした表現\n" +
                                   "・エミッションで輝き\n" +
                                   "・スペキュラーでハイライト";
                preset.category = PresetCategory.Character_Eyes;
                preset.author = "Natane Toon Shader";
                preset.version = "1.0";
                preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");

                var p = preset.parameters;
                // mainColorを白にしてテクスチャの色を完全に反映
                p.mainColor = new Color(1.0f, 1.0f, 1.0f, 1f);
                p.alpha = 1f;
                // やや温かみのある青系の影色
                p.shadowColor = new Color(0.65f, 0.7f, 0.85f, 1f);
                p.toonSteps = 3;
                p.toonSharpness = 0.15f;
                p.shadowReceive = 0.5f;
                p.shadowIntensityMax = 0.4f;
                p.lightInfluence = 1.5f;
                p.lightColorInfluence = 1.0f;
                p.backlight = 0.4f;
                p.useSpecular = true;
                p.specularColor = new Color(1f, 1f, 1f, 1f);
                p.specularIntensity = 1.5f;
                p.specularSize = 0.3f;
                p.specularSharpness = 0.95f;
                p.useEmission = true;
                p.emissionColor = new Color(0.5f, 0.7f, 1f, 1f);
                p.emissionIntensity = 0.3f;
                p.useRimLight = true;
                p.rimColor = new Color(1f, 1f, 1f, 1f);
                p.rimIntensity = 1f;
                p.rimPower = 3f;
                p.useOutline = true;
                p.outlineColor = new Color(0.1f, 0.1f, 0.2f, 1f);
                p.outlineWidth = 0.002f;
                p.renderQueue = 2000;
                p.cullMode = 2;

                string assetPath = $"{basePath}/VTuber_CharacterEyes.asset";
                AssetDatabase.CreateAsset(preset, assetPath);
                Debug.Log($"[GenerateCharacterEyesPreset] Created preset at: {assetPath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GenerateCharacterEyesPreset] Failed to generate: {e.Message}\nStack: {e.StackTrace}");
                return false;
            }
        }

        private bool GenerateLivePerformancePreset(string basePath)
        {
            try
            {
                // Verify directory exists
                string fullBasePath = System.IO.Path.GetFullPath(basePath);
                if (!System.IO.Directory.Exists(fullBasePath))
                {
                    Debug.LogError($"[GenerateLivePerformancePreset] Directory does not exist: {fullBasePath}");
                    return false;
                }

                var preset = CreateInstance<NataneToonMaterialPreset>();
                preset.presetName = "VTuber - ライブパフォーマンス";
                preset.description = "VTuberライブ配信・パフォーマンスに最適化されたプリセット。\n" +
                                   "・高パフォーマンス設定\n" +
                                   "・必要最小限の機能\n" +
                                   "・クリーンで安定したレンダリング";
                preset.category = PresetCategory.Style_Toon;
                preset.author = "Natane Toon Shader";
                preset.version = "1.0";
                preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");

                var p = preset.parameters;
                // mainColorは白のままでテクスチャの色を完全に反映
                p.mainColor = Color.white;
                p.alpha = 1f;
                // やや温かみのあるグレー系の影色
                p.shadowColor = new Color(0.75f, 0.72f, 0.7f, 1f);
                p.toonSteps = 2;
                p.toonSharpness = 0.1f;
                p.shadowReceive = 1f;
                p.shadowIntensityMax = 0.2f;
                p.lightInfluence = 1f;
                p.lightColorInfluence = 1.0f;
                p.backlight = 0f;
                p.useSpecular = false;
                p.useRimLight = false;
                p.useSSS = false;
                p.useMatCap = false;
                p.useEmission = false;
                p.useReflection = false;
                p.useEnvRim = false;
                p.useParallax = false;
                p.useRefraction = false;
                p.useOutline = true;
                p.outlineColor = Color.black;
                p.outlineWidth = 0.004f;
                p.renderQueue = 2000;
                p.cullMode = 2;

                string assetPath = $"{basePath}/VTuber_LivePerformance.asset";
                AssetDatabase.CreateAsset(preset, assetPath);
                Debug.Log($"[GenerateLivePerformancePreset] Created preset at: {assetPath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GenerateLivePerformancePreset] Failed to generate: {e.Message}\nStack: {e.StackTrace}");
                return false;
            }
        }
    }
}
