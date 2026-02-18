using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace NataneToon.Editor
{
    /// <summary>
    /// Prefab Variant Converter with Material Migration
    /// プレハブバリアント生成＆マテリアル移行ツール
    /// Creates prefab variants and converts lilToon materials to NataneToon
    /// プレハブバリアントを生成し、lilToonマテリアルをNataneToonに変換
    /// </summary>
    public class PrefabVariantConverter : EditorWindow
    {
        private GameObject selectedPrefab;
        private Vector2 scrollPosition;

        // Settings
        private string materialPrefix = "NT_";
        private string materialSuffix = "";
        private bool createMaterialFolder = true;
        private string variantFolderPath = "Assets/Prefabs/Variants";
        private string materialFolderPath = "Assets/Materials/NataneToon";

        // Material detection
        private List<MaterialInfo> detectedMaterials = new List<MaterialInfo>();
        private bool showMaterialPreview = true;

        private class MaterialInfo
        {
            public Material original;
            public Renderer renderer;
            public int materialIndex;
            public bool isLilToon;
            public string newName;
            public bool willConvert;
        }

        [MenuItem("Tools/Natane/移行 Migration/プレハブバリアント変換 Prefab Variant Converter", false, 54)]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabVariantConverter>("プレハブバリアント変換 Prefab Variant Converter");
            window.minSize = new Vector2(600, 700);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshSelection();
        }

        private void OnSelectionChange()
        {
            RefreshSelection();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            NataneToonShaderGUIUtility.DrawHeaderWithHelp(
                "プレハブバリアント変換ツール",
                "Prefab Variant Converter",
                "PrefabVariantConverter");
            EditorGUILayout.LabelField(
                "プレハブバリアントを生成し、lilToonマテリアルをNataneToonに一括変換\n" +
                "Create prefab variant and batch convert lilToon materials to NataneToon",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawPrefabSelection();
            EditorGUILayout.Space(10);

            if (selectedPrefab != null)
            {
                DrawSettings();
                EditorGUILayout.Space(10);
                DrawMaterialPreview();
                EditorGUILayout.Space(10);
                DrawActions();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPrefabSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. プレハブ選択 Prefab Selection", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "シーンまたはプロジェクトビューでプレハブを選択してください\n" +
                "Select a prefab in Scene or Project view",
                MessageType.Info);

            GUI.enabled = false;
            EditorGUILayout.ObjectField("選択中のプレハブ Selected Prefab", selectedPrefab, typeof(GameObject), false);
            GUI.enabled = true;

            if (selectedPrefab != null)
            {
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedPrefab);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
                }

                EditorGUILayout.LabelField("パス Path:", EditorStyles.miniLabel);
                EditorGUILayout.SelectableLabel(prefabPath, EditorStyles.textField, GUILayout.Height(18));

                // Material count
                int materialCount = detectedMaterials.Count;
                int lilToonCount = detectedMaterials.Count(m => m.isLilToon);
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"マテリアル数 Materials: {materialCount}");
                EditorGUILayout.LabelField($"lilToonマテリアル lilToon Materials: {lilToonCount}",
                    lilToonCount > 0 ? EditorStyles.boldLabel : EditorStyles.label);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "プレハブが選択されていません\nNo prefab selected",
                    MessageType.Warning);
            }

            if (GUILayout.Button("選択を更新 Refresh Selection", GUILayout.Height(25)))
            {
                RefreshSelection();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2. 設定 Settings", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("バリアント保存先 Variant Save Location", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            variantFolderPath = EditorGUILayout.TextField("フォルダパス Folder Path", variantFolderPath);
            if (GUILayout.Button("選択 Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("バリアント保存先を選択", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        variantFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("マテリアル設定 Material Settings", EditorStyles.boldLabel);

            materialPrefix = EditorGUILayout.TextField("接頭辞 Prefix", materialPrefix);
            materialSuffix = EditorGUILayout.TextField("接尾辞 Suffix", materialSuffix);

            EditorGUILayout.Space(5);
            createMaterialFolder = EditorGUILayout.Toggle("専用フォルダに保存 Save to Folder", createMaterialFolder);

            if (createMaterialFolder)
            {
                EditorGUILayout.BeginHorizontal();
                materialFolderPath = EditorGUILayout.TextField("保存先 Save Path", materialFolderPath);
                if (GUILayout.Button("選択 Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("マテリアル保存先を選択", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.StartsWith(Application.dataPath))
                        {
                            materialFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            // Preview naming
            if (detectedMaterials.Count > 0 && detectedMaterials.Any(m => m.isLilToon))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("命名プレビュー Naming Preview:", EditorStyles.miniLabel);
                var firstLilToon = detectedMaterials.First(m => m.isLilToon);
                string exampleName = GetNewMaterialName(firstLilToon.original);
                EditorGUILayout.SelectableLabel($"例 Example: {firstLilToon.original.name} → {exampleName}",
                    EditorStyles.textField, GUILayout.Height(18));
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialPreview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            showMaterialPreview = EditorGUILayout.Foldout(showMaterialPreview,
                $"3. マテリアルプレビュー Material Preview ({detectedMaterials.Count})", true);
            EditorGUILayout.EndHorizontal();

            if (showMaterialPreview && detectedMaterials.Count > 0)
            {
                EditorGUILayout.Space(5);

                // Statistics
                int lilToonCount = detectedMaterials.Count(m => m.isLilToon);
                int willConvertCount = detectedMaterials.Count(m => m.willConvert);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"lilToonマテリアル: {lilToonCount}", GUILayout.Width(150));
                EditorGUILayout.LabelField($"変換予定: {willConvertCount}", GUILayout.Width(150));
                if (GUILayout.Button(willConvertCount == lilToonCount ? "すべて解除 Deselect All" : "すべて選択 Select All"))
                {
                    bool selectAll = willConvertCount != lilToonCount;
                    foreach (var mat in detectedMaterials.Where(m => m.isLilToon))
                    {
                        mat.willConvert = selectAll;
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Material list
                foreach (var matInfo in detectedMaterials)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();

                    if (matInfo.isLilToon)
                    {
                        matInfo.willConvert = EditorGUILayout.Toggle(matInfo.willConvert, GUILayout.Width(20));
                    }
                    else
                    {
                        GUI.enabled = false;
                        EditorGUILayout.Toggle(false, GUILayout.Width(20));
                        GUI.enabled = true;
                    }

                    EditorGUILayout.ObjectField(matInfo.original, typeof(Material), false, GUILayout.Width(200));

                    if (matInfo.isLilToon)
                    {
                        EditorGUILayout.LabelField("→", GUILayout.Width(20));
                        EditorGUILayout.LabelField(GetNewMaterialName(matInfo.original), EditorStyles.boldLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("(変換不要 No conversion needed)", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField(
                        $"使用箇所 Used in: {matInfo.renderer.name} [Slot {matInfo.materialIndex}]",
                        EditorStyles.miniLabel);

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("4. 実行 Execute", EditorStyles.boldLabel);

            int willConvertCount = detectedMaterials.Count(m => m.willConvert);

            if (willConvertCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "変換するマテリアルがありません\n" +
                    "No materials selected for conversion",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"以下の処理を実行します：\n" +
                    $"• プレハブバリアントを作成\n" +
                    $"• {willConvertCount}個のlilToonマテリアルを複製\n" +
                    $"• NataneToonシェーダーに変換\n" +
                    $"• バリアントに新しいマテリアルを適用\n\n" +
                    $"The following will be executed:\n" +
                    $"• Create prefab variant\n" +
                    $"• Duplicate {willConvertCount} lilToon materials\n" +
                    $"• Convert to NataneToon shader\n" +
                    $"• Apply new materials to variant",
                    MessageType.Info);
            }

            EditorGUILayout.Space(5);

            GUI.enabled = willConvertCount > 0;
            if (GUILayout.Button("バリアント生成＆変換実行 Create Variant & Convert", GUILayout.Height(40)))
            {
                ExecuteConversion();
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void RefreshSelection()
        {
            detectedMaterials.Clear();
            selectedPrefab = null;

            GameObject selected = Selection.activeGameObject;
            if (selected == null) return;

            // Check if it's a prefab
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(selected);
            if (prefabType == PrefabAssetType.NotAPrefab)
            {
                return;
            }

            selectedPrefab = selected;
            DetectMaterials();
        }

        private void DetectMaterials()
        {
            if (selectedPrefab == null) return;

            detectedMaterials.Clear();

            // Get all renderers in prefab
            Renderer[] renderers = selectedPrefab.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;

                    bool isLilToon = materials[i].shader != null &&
                                     materials[i].shader.name.Contains("lilToon");

                    detectedMaterials.Add(new MaterialInfo
                    {
                        original = materials[i],
                        renderer = renderer,
                        materialIndex = i,
                        isLilToon = isLilToon,
                        willConvert = isLilToon,
                        newName = GetNewMaterialName(materials[i])
                    });
                }
            }
        }

        private string GetNewMaterialName(Material mat)
        {
            if (mat == null) return "";

            string baseName = mat.name;

            // Remove existing prefixes if any
            if (baseName.StartsWith("lil_"))
                baseName = baseName.Substring(4);

            return materialPrefix + baseName + materialSuffix;
        }

        private void ExecuteConversion()
        {
            if (selectedPrefab == null) return;

            try
            {
                // Step 1: Create folders if needed
                if (createMaterialFolder && !AssetDatabase.IsValidFolder(materialFolderPath))
                {
                    CreateFolderRecursively(materialFolderPath);
                }

                if (!AssetDatabase.IsValidFolder(variantFolderPath))
                {
                    CreateFolderRecursively(variantFolderPath);
                }

                // Step 2: Duplicate and convert materials
                Dictionary<Material, Material> materialMapping = new Dictionary<Material, Material>();
                int convertedCount = 0;

                var materialsToConvert = detectedMaterials.Where(m => m.willConvert).ToList();

                EditorUtility.DisplayProgressBar("変換中 Converting", "マテリアルを変換中...", 0f);

                for (int i = 0; i < materialsToConvert.Count; i++)
                {
                    var matInfo = materialsToConvert[i];
                    EditorUtility.DisplayProgressBar("変換中 Converting",
                        $"マテリアルを変換中... {matInfo.original.name}",
                        (float)i / materialsToConvert.Count);

                    Material newMaterial = ConvertMaterial(matInfo.original);
                    if (newMaterial != null)
                    {
                        materialMapping[matInfo.original] = newMaterial;
                        convertedCount++;
                    }
                }

                EditorUtility.DisplayProgressBar("変換中 Converting", "バリアントを作成中...", 0.8f);

                // Step 3: Create prefab variant
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedPrefab);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
                }

                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                string variantName = materialPrefix + prefabName + "_Variant.prefab";
                string variantPath = Path.Combine(variantFolderPath, variantName);

                // Create variant
                GameObject variantPrefab = PrefabUtility.LoadPrefabContents(prefabPath);

                // Apply material mapping to variant
                Renderer[] renderers = variantPrefab.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    Material[] materials = renderer.sharedMaterials;
                    bool changed = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null && materialMapping.ContainsKey(materials[i]))
                        {
                            materials[i] = materialMapping[materials[i]];
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        renderer.sharedMaterials = materials;
                    }
                }

                // Save variant
                PrefabUtility.SaveAsPrefabAsset(variantPrefab, variantPath);
                PrefabUtility.UnloadPrefabContents(variantPrefab);

                EditorUtility.ClearProgressBar();

                AssetDatabase.Refresh();

                // Select the created variant
                GameObject createdVariant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                EditorGUIUtility.PingObject(createdVariant);
                Selection.activeObject = createdVariant;

                EditorUtility.DisplayDialog(
                    "変換完了 Conversion Complete",
                    $"プレハブバリアントを作成しました\nCreated prefab variant\n\n" +
                    $"バリアント Variant: {variantPath}\n" +
                    $"変換したマテリアル Converted Materials: {convertedCount}個\n\n" +
                    $"詳細はコンソールを確認してください\nCheck console for details",
                    "OK");

                Debug.Log($"[PrefabVariantConverter] バリアント作成完了: {variantPath}");
                Debug.Log($"[PrefabVariantConverter] 変換したマテリアル数: {convertedCount}");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "エラー Error",
                    $"変換中にエラーが発生しました\nError during conversion:\n\n{e.Message}",
                    "OK");
                Debug.LogError($"[PrefabVariantConverter] エラー: {e}");
            }
        }

        private Material ConvertMaterial(Material sourceMaterial)
        {
            if (sourceMaterial == null) return null;

            // Find NataneToon shader first
            Shader nataneToonShader = Shader.Find("Natane/Toon Shader");
            if (nataneToonShader == null)
            {
                Debug.LogError($"[PrefabVariantConverter] Natane/Toon Shader not found! Material conversion failed for {sourceMaterial.name}");
                return null;
            }

            // Store original properties before creating new material
            var originalProperties = CaptureProperties(sourceMaterial);

            // Create new material
            Material newMaterial = new Material(nataneToonShader);

            string newName = GetNewMaterialName(sourceMaterial);
            newMaterial.name = newName;

            // Map properties from lilToon to NataneToon
            MapProperties(originalProperties, newMaterial);

            // Save material
            string savePath;
            if (createMaterialFolder)
            {
                savePath = Path.Combine(materialFolderPath, newName + ".mat");
            }
            else
            {
                // Save next to original material
                string originalPath = AssetDatabase.GetAssetPath(sourceMaterial);
                string originalDir = Path.GetDirectoryName(originalPath);
                savePath = Path.Combine(originalDir, newName + ".mat");
            }

            // Check if file already exists
            if (AssetDatabase.LoadAssetAtPath<Material>(savePath) != null)
            {
                savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);
            }

            AssetDatabase.CreateAsset(newMaterial, savePath);

            Debug.Log($"[PrefabVariantConverter] マテリアル作成: {sourceMaterial.name} → {savePath}");

            return newMaterial;
        }

        private Dictionary<string, object> CaptureProperties(Material material)
        {
            var properties = new Dictionary<string, object>();

            // Common textures
            CaptureTexture(material, "_MainTex", properties);
            CaptureTexture(material, "_BumpMap", properties);
            CaptureTexture(material, "_EmissionMap", properties);
            CaptureTexture(material, "_MatCapTex", properties);
            CaptureTexture(material, "_OutlineWidthMask", properties);

            // lilToon specific textures (with different names)
            CaptureTexture(material, "_lilMainTex", properties);
            CaptureTexture(material, "_lilBumpMap", properties);
            CaptureTexture(material, "_lilEmissionMap", properties);

            // Common colors
            CaptureColor(material, "_Color", properties);
            CaptureColor(material, "_ShadowColor", properties);
            CaptureColor(material, "_EmissionColor", properties);
            CaptureColor(material, "_OutlineColor", properties);

            // lilToon specific colors
            CaptureColor(material, "_lilColor", properties);
            CaptureColor(material, "_lilShadowColor", properties);

            // Common floats
            CaptureFloat(material, "_Cutoff", properties);
            CaptureFloat(material, "_BumpScale", properties);
            CaptureFloat(material, "_OutlineWidth", properties);
            CaptureFloat(material, "_Glossiness", properties);
            CaptureFloat(material, "_Metallic", properties);

            // lilToon specific floats
            CaptureFloat(material, "_lilShadowBorder", properties);
            CaptureFloat(material, "_lilShadowBlur", properties);

            return properties;
        }

        private void CaptureTexture(Material material, string propertyName, Dictionary<string, object> properties)
        {
            if (material.HasProperty(propertyName))
            {
                var texture = material.GetTexture(propertyName);
                if (texture != null)
                {
                    properties[propertyName] = texture;
                }
            }
        }

        private void CaptureColor(Material material, string propertyName, Dictionary<string, object> properties)
        {
            if (material.HasProperty(propertyName))
            {
                properties[propertyName] = material.GetColor(propertyName);
            }
        }

        private void CaptureFloat(Material material, string propertyName, Dictionary<string, object> properties)
        {
            if (material.HasProperty(propertyName))
            {
                properties[propertyName] = material.GetFloat(propertyName);
            }
        }

        private void MapProperties(Dictionary<string, object> originalProperties, Material targetMaterial)
        {
            // Map textures
            MapTexture(originalProperties, targetMaterial, "_MainTex", "_MainTex");
            MapTexture(originalProperties, targetMaterial, "_lilMainTex", "_MainTex");
            MapTexture(originalProperties, targetMaterial, "_BumpMap", "_BumpMap");
            MapTexture(originalProperties, targetMaterial, "_lilBumpMap", "_BumpMap");
            MapTexture(originalProperties, targetMaterial, "_EmissionMap", "_EmissionMap");
            MapTexture(originalProperties, targetMaterial, "_lilEmissionMap", "_EmissionMap");
            MapTexture(originalProperties, targetMaterial, "_MatCapTex", "_MatCapTex");  // Fixed: _MatCap is Float, _MatCapTex is Texture
            MapTexture(originalProperties, targetMaterial, "_OutlineWidthMask", "_OutlineWidthMask");

            // Map colors
            MapColor(originalProperties, targetMaterial, "_Color", "_Color");
            MapColor(originalProperties, targetMaterial, "_lilColor", "_Color");
            MapColor(originalProperties, targetMaterial, "_ShadowColor", "_ShadowColor");
            MapColor(originalProperties, targetMaterial, "_lilShadowColor", "_ShadowColor");
            MapColor(originalProperties, targetMaterial, "_EmissionColor", "_EmissionColor");
            MapColor(originalProperties, targetMaterial, "_OutlineColor", "_OutlineColor");

            // Map floats
            MapFloat(originalProperties, targetMaterial, "_Cutoff", "_Cutoff");
            MapFloat(originalProperties, targetMaterial, "_BumpScale", "_BumpScale");
            MapFloat(originalProperties, targetMaterial, "_OutlineWidth", "_OutlineWidth");
            MapFloat(originalProperties, targetMaterial, "_Glossiness", "_Glossiness");
            MapFloat(originalProperties, targetMaterial, "_Metallic", "_Metallic");

            // Map lilToon specific properties to NataneToon equivalents
            MapFloat(originalProperties, targetMaterial, "_lilShadowBorder", "_ShadowReceive");

            // Enable MatCap if texture exists
            if (originalProperties.ContainsKey("_MatCapTex") && originalProperties["_MatCapTex"] != null)
            {
                if (targetMaterial.HasProperty("_MatCap"))
                {
                    targetMaterial.SetFloat("_MatCap", 1f);
                }
            }
        }

        private void MapTexture(Dictionary<string, object> properties, Material target, string sourceKey, string targetKey)
        {
            if (properties.ContainsKey(sourceKey) && target.HasProperty(targetKey))
            {
                target.SetTexture(targetKey, properties[sourceKey] as Texture);
            }
        }

        private void MapColor(Dictionary<string, object> properties, Material target, string sourceKey, string targetKey)
        {
            if (properties.ContainsKey(sourceKey) && target.HasProperty(targetKey))
            {
                target.SetColor(targetKey, (Color)properties[sourceKey]);
            }
        }

        private void MapFloat(Dictionary<string, object> properties, Material target, string sourceKey, string targetKey)
        {
            if (properties.ContainsKey(sourceKey) && target.HasProperty(targetKey))
            {
                target.SetFloat(targetKey, (float)properties[sourceKey]);
            }
        }

        private void CreateFolderRecursively(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parentPath = Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = Path.GetFileName(path);

            if (!AssetDatabase.IsValidFolder(parentPath))
            {
                CreateFolderRecursively(parentPath);
            }

            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
