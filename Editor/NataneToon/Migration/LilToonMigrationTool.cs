using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// lilToon から Natane Toon Shader への自動移行ツール
    /// </summary>
    public class LilToonMigrationTool : EditorWindow
    {
        private List<Material> lilToonMaterials = new List<Material>();
        private Vector2 scrollPosition;
        private bool createBackup = true;
        private bool replaceOriginal = false;
        private bool showPreview = false;
        private Material previewMaterial = null;

        // 変換モード: VisualMatch（見た目一致）vs MinimalSafe（最小限・旧動作）
        private enum ConversionMode { VisualMatch, MinimalSafe }
        private ConversionMode conversionMode = ConversionMode.VisualMatch;
        private string[] conversionModeNames => new[] {
            L("見た目一致 (推奨)", "Visual Match (Recommended)"),
            L("最小限（旧動作）", "Minimal Safe (Legacy)")
        };

        // モード切替
        private enum MigrationMode { Project, Prefab }
        private MigrationMode currentMode = MigrationMode.Project;
        private string[] modeNames => new[] { L("全プロジェクト", "Project"), L("アバター/プレハブ", "Avatar/Prefab") };

        // プレハブモード用フィールド
        private GameObject targetPrefab = null;
        private List<PrefabMaterialInfo> prefabMaterials = new List<PrefabMaterialInfo>();
        private Vector2 prefabScrollPosition;
        private bool updatePrefabReferences = true;
        private bool duplicateInHierarchy = false;
        private GameObject lastDuplicatedObject = null;

        // プレハブ内マテリアル情報
        private class PrefabMaterialInfo
        {
            public Material original;
            public Material converted;
            public Renderer renderer;
            public int materialIndex;
            public bool willConvert = true;
            public string rendererPath;
        }

        // 変換レポート
        private class ConversionReport
        {
            public string materialName;
            public bool success;
            public List<string> warnings = new List<string>();
            public List<string> infos = new List<string>();
            public bool hasMultipleShadowLayers;
            public bool outlineWidthAdjusted;
            public float originalOutlineWidth;
            public float convertedOutlineWidth;
        }

        [MenuItem("Tools/Natane/移行 Migration/lilToon移行ツール lilToon Migration Tool", false, 51)]
        public static void ShowWindow()
        {
            var window = GetWindow<LilToonMigrationTool>(L("lilToon移行ツール", "lilToon Migration"));
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            ScanForLilToonMaterials();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(L("lilToon から Natane Toon Shader への移行ツール", "lilToon to Natane Toon Shader Migration Tool"), EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                L("このツールはlilToonマテリアルを自動的にNatane Toon Shaderに変換します。\n" +
                "「見た目一致」モード: lilToonで有効な機能（リムライト・アウトライン・エミッション等）をすべて変換・有効化します。\n" +
                "「最小限」モード: 基本設定のみ有効。プロパティ値は保持されるため、移行後に個別に有効化できます。",
                "This tool automatically converts lilToon materials to Natane Toon Shader.\n" +
                "Visual Match mode: Converts and enables all active lilToon features (rim light, outline, emission, etc.).\n" +
                "Minimal Safe mode: Only basic settings enabled. Property values are preserved for manual activation."),
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Options
            EditorGUILayout.LabelField(L("オプション", "Options"), EditorStyles.boldLabel);
            createBackup = EditorGUILayout.Toggle(L("バックアップを作成", "Create Backup"), createBackup);
            replaceOriginal = EditorGUILayout.Toggle(L("元を置換（破壊的）", "Replace Original (Destructive)"), replaceOriginal);
            showPreview = EditorGUILayout.Toggle(L("変換後プレビュー表示", "Show Preview After Conversion"), showPreview);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("変換モード", "Conversion Mode"), EditorStyles.boldLabel);
            conversionMode = (ConversionMode)GUILayout.Toolbar((int)conversionMode, conversionModeNames);
            if (conversionMode == ConversionMode.VisualMatch)
            {
                EditorGUILayout.HelpBox(
                    L("lilToonで有効な機能（リムライト・アウトライン・エミッション・MatCap・スペキュラ等）をすべて変換・有効化します。\n" +
                    "変換直後から見た目が近い状態になります。",
                    "All active lilToon features (rim light, outline, emission, MatCap, specular, etc.) will be converted and enabled.\n" +
                    "The result will visually match the original material immediately after conversion."),
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    L("基本設定（テクスチャ・カラー・シャドウ）のみ有効化します。他の機能はOFF状態で移行されます。\n" +
                    "プロパティ値は保持されるため、移行後にインスペクターから個別に有効化できます。",
                    "Only basic settings (texture, color, shadow) are enabled. Other features are migrated in OFF state.\n" +
                    "Property values are preserved, so you can enable features individually after migration."),
                    MessageType.Info
                );
            }

            EditorGUILayout.Space(5);

            if (replaceOriginal)
            {
                EditorGUILayout.HelpBox(
                    L("警告: この操作は元のマテリアルを永久に変更します！\n" +
                    "プロジェクトのバックアップがあることを確認してください。",
                    "WARNING: This will permanently modify your original materials!\n" +
                    "Make sure you have a backup of your project."),
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space();

            // モード切替タブ
            currentMode = (MigrationMode)GUILayout.Toolbar((int)currentMode, modeNames);
            EditorGUILayout.Space();

            switch (currentMode)
            {
                case MigrationMode.Project:
                    DrawProjectMode();
                    break;
                case MigrationMode.Prefab:
                    DrawPrefabMode();
                    break;
            }
        }

        /// <summary>
        /// 全プロジェクトモードのUI描画
        /// </summary>
        private void DrawProjectMode()
        {
            // Scan button
            if (GUILayout.Button(L("lilToonマテリアルをスキャン", "Scan for lilToon Materials"), GUILayout.Height(30)))
            {
                ScanForLilToonMaterials();
            }

            EditorGUILayout.Space();

            // Materials list
            EditorGUILayout.LabelField(L($"見つかったマテリアル: {lilToonMaterials.Count}個", $"Found {lilToonMaterials.Count} lilToon Materials"), EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var material in lilToonMaterials)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(material, typeof(Material), false);

                if (GUILayout.Button(L("変換", "Convert"), GUILayout.Width(80)))
                {
                    var report = ConvertMaterialWithReport(material);
                    if (report.success)
                    {
                        // 単一変換でも簡易レポートを表示
                        ShowConversionReport(new List<ConversionReport> { report }, 1);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Convert all button
            GUI.enabled = lilToonMaterials.Count > 0;
            if (GUILayout.Button(L("すべて変換", "Convert All Materials"), GUILayout.Height(40)))
            {
                ConvertAllMaterials();
            }
            GUI.enabled = true;
        }

        /// <summary>
        /// アバター/プレハブモードのUI描画
        /// </summary>
        private void DrawPrefabMode()
        {
            // セクション1: プレハブ選択
            EditorGUILayout.LabelField(L("プレハブ選択", "Prefab Selection"), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            targetPrefab = (GameObject)EditorGUILayout.ObjectField(
                L("対象プレハブ", "Target Prefab"),
                targetPrefab,
                typeof(GameObject),
                true // allowSceneObjects - シーン上のインスタンスもD&D可能
            );
            if (EditorGUI.EndChangeCheck() && targetPrefab != null)
            {
                ScanPrefabMaterials();
            }
            EditorGUILayout.EndHorizontal();

            if (targetPrefab == null)
            {
                EditorGUILayout.HelpBox(
                    L("変換するプレハブまたはシーン上のアバターをここにドラッグ＆ドロップしてください。",
                    "Drag & drop a prefab or scene avatar here to scan for lilToon materials."),
                    MessageType.Info
                );
                return;
            }

            EditorGUILayout.Space();

            // セクション2: プレハブ設定
            EditorGUILayout.LabelField(L("プレハブ設定", "Prefab Settings"), EditorStyles.boldLabel);

            // 複製モード（元のプレハブを維持）
            duplicateInHierarchy = EditorGUILayout.Toggle(
                L("ヒエラルキーに複製を作成", "Create Duplicate in Hierarchy"),
                duplicateInHierarchy
            );
            if (duplicateInHierarchy)
            {
                EditorGUILayout.HelpBox(
                    L("元のプレハブを維持したまま、変換済みマテリアルを適用した複製をヒエラルキーに作成します。\n" +
                    "元のアバターと並べて見比べることができます。",
                    "Creates a duplicate in the hierarchy with converted materials, keeping the original prefab untouched.\n" +
                    "You can compare the original and converted avatars side by side."),
                    MessageType.Info
                );
            }

            // 複製モードでない場合のみ、既存の参照更新/置換オプションを表示
            using (new EditorGUI.DisabledScope(duplicateInHierarchy))
            {
                updatePrefabReferences = EditorGUILayout.Toggle(
                    L("参照を自動更新", "Auto-update References"),
                    updatePrefabReferences
                );
            }

            if (duplicateInHierarchy)
            {
                // 複製モードの説明（他のオプションは無関係）
            }
            else if (updatePrefabReferences && !replaceOriginal)
            {
                EditorGUILayout.HelpBox(
                    L("変換後、プレハブ内のRenderer参照を新しいマテリアルに自動的に差し替えます。",
                    "After conversion, Renderer references in the prefab will be automatically updated to new materials."),
                    MessageType.Info
                );
            }
            else if (replaceOriginal)
            {
                EditorGUILayout.HelpBox(
                    L("「元を置換」モードでは元のマテリアル自体が変更されるため、参照の更新は不要です。",
                    "In 'Replace Original' mode, the original material is modified in-place, so reference updates are unnecessary."),
                    MessageType.Info
                );
            }

            EditorGUILayout.Space();

            // スキャンボタン
            if (GUILayout.Button(L("マテリアルを再スキャン", "Rescan Materials"), GUILayout.Height(25)))
            {
                ScanPrefabMaterials();
            }

            EditorGUILayout.Space();

            // セクション3: マテリアル一覧
            if (prefabMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    L("このプレハブにはlilToonマテリアルが見つかりませんでした。",
                    "No lilToon materials found in this prefab."),
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.LabelField(
                L($"検出されたlilToonマテリアル: {prefabMaterials.Select(m => m.original).Distinct().Count()}個",
                $"Found {prefabMaterials.Select(m => m.original).Distinct().Count()} lilToon Materials"),
                EditorStyles.boldLabel
            );

            prefabScrollPosition = EditorGUILayout.BeginScrollView(prefabScrollPosition, GUILayout.MinHeight(200));

            // 同一マテリアルでグループ化して表示
            var grouped = prefabMaterials.GroupBy(m => m.original);
            foreach (var group in grouped)
            {
                Material mat = group.Key;
                var entries = group.ToList();
                bool willConvert = entries[0].willConvert;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // マテリアル行
                EditorGUILayout.BeginHorizontal();
                bool newWillConvert = EditorGUILayout.Toggle(willConvert, GUILayout.Width(20));
                if (newWillConvert != willConvert)
                {
                    foreach (var entry in entries)
                    {
                        entry.willConvert = newWillConvert;
                    }
                }

                EditorGUILayout.ObjectField(mat, typeof(Material), false);

                if (entries[0].converted != null)
                {
                    EditorGUILayout.LabelField("→", GUILayout.Width(20));
                    EditorGUILayout.ObjectField(entries[0].converted, typeof(Material), false);
                }

                if (GUILayout.Button(L("個別変換", "Convert"), GUILayout.Width(100)))
                {
                    ConvertSinglePrefabMaterial(mat);
                }

                EditorGUILayout.EndHorizontal();

                // 使用箇所を表示
                EditorGUI.indentLevel++;
                foreach (var entry in entries)
                {
                    EditorGUILayout.LabelField(
                        L($"使用箇所: {entry.rendererPath} [スロット {entry.materialIndex}]",
                        $"Used by: {entry.rendererPath} [Slot {entry.materialIndex}]"),
                        EditorStyles.miniLabel
                    );
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // セクション4: 実行ボタン
            int convertCount = prefabMaterials.Where(m => m.willConvert && m.converted == null)
                                              .Select(m => m.original).Distinct().Count();

            if (convertCount > 0)
            {
                EditorGUILayout.LabelField(
                    L($"変換対象: {convertCount}個のマテリアル",
                    $"Target: {convertCount} materials"),
                    EditorStyles.boldLabel
                );
            }

            GUI.enabled = convertCount > 0;
            string buttonLabel = duplicateInHierarchy
                ? L($"複製を作成して変換 ({convertCount})", $"Duplicate & Convert ({convertCount})")
                : L($"選択したマテリアルを一括変換 ({convertCount})", $"Convert Selected ({convertCount})");
            if (GUILayout.Button(buttonLabel, GUILayout.Height(40)))
            {
                ConvertPrefabMaterials();
            }
            GUI.enabled = true;

            // 前回の複製オブジェクトへの参照
            if (lastDuplicatedObject != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(L("前回の複製:", "Last Duplicate:"), GUILayout.Width(100));
                EditorGUILayout.ObjectField(lastDuplicatedObject, typeof(GameObject), true);
                if (GUILayout.Button(L("選択", "Select"), GUILayout.Width(60)))
                {
                    Selection.activeGameObject = lastDuplicatedObject;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void ScanForLilToonMaterials()
        {
            lilToonMaterials.Clear();

            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");

            foreach (string guid in materialGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && material.shader != null)
                {
                    string shaderName = material.shader.name;

                    // Check if it's a lilToon shader
                    if (shaderName.Contains("lilToon") || shaderName.StartsWith("_lil/"))
                    {
                        lilToonMaterials.Add(material);
                    }
                }
            }

            Debug.Log($"Found {lilToonMaterials.Count} lilToon materials.");
        }

        private void ConvertAllMaterials()
        {
            if (!EditorUtility.DisplayDialog(
                L("すべて変換", "Convert All Materials"),
                L($"{lilToonMaterials.Count}個のマテリアルを変換してもよろしいですか？",
                $"Are you sure you want to convert {lilToonMaterials.Count} materials?"),
                L("はい", "Yes"), L("キャンセル", "Cancel")))
            {
                return;
            }

            int successCount = 0;
            List<ConversionReport> reports = new List<ConversionReport>();

            for (int i = 0; i < lilToonMaterials.Count; i++)
            {
                EditorUtility.DisplayProgressBar(
                    "Converting Materials",
                    $"Converting {i + 1}/{lilToonMaterials.Count}: {lilToonMaterials[i].name}",
                    (float)i / lilToonMaterials.Count
                );

                var report = ConvertMaterialWithReport(lilToonMaterials[i]);
                reports.Add(report);
                if (report.success)
                {
                    successCount++;
                }
            }

            EditorUtility.ClearProgressBar();

            // 変換レポートを表示
            ShowConversionReport(reports, successCount);

            // Rescan
            ScanForLilToonMaterials();
        }

        private bool ConvertMaterial(Material sourceMaterial)
        {
            var report = ConvertMaterialWithReport(sourceMaterial);
            return report.success;
        }

        private ConversionReport ConvertMaterialWithReport(Material sourceMaterial)
        {
            var report = new ConversionReport();
            report.materialName = sourceMaterial.name;

            try
            {
                // Create backup if requested
                if (createBackup)
                {
                    string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                    string backupPath = sourcePath.Replace(".mat", "_lilToon_backup.mat");
                    AssetDatabase.CopyAsset(sourcePath, backupPath);
                    Debug.Log($"Created backup: {backupPath}");
                    report.infos.Add($"バックアップを作成しました: {backupPath}");
                }

                Material targetMaterial;

                if (replaceOriginal)
                {
                    targetMaterial = sourceMaterial;
                }
                else
                {
                    // Create new material
                    targetMaterial = new Material(sourceMaterial);
                    string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                    string newPath = sourcePath.Replace(".mat", "_NataneToon.mat");
                    AssetDatabase.CreateAsset(targetMaterial, newPath);
                    report.infos.Add($"新しいマテリアルを作成しました: {newPath}");
                }

                // Store original properties before changing shader
                var originalProperties = CaptureProperties(sourceMaterial);

                // 複数シャドウレイヤーの検出
                DetectMultipleShadowLayers(originalProperties, report);

                // Find Natane Toon Shader (バリアント自動検出)
                Shader nataneToonShader = DetectNataneShaderVariant(sourceMaterial);
                if (nataneToonShader == null)
                {
                    Debug.LogError("Natane Toon Shader not found! Please make sure it's in your project.");
                    report.success = false;
                    report.warnings.Add("Natane Toon Shaderが見つかりませんでした。");
                    return report;
                }
                report.infos.Add($"シェーダーバリアント: {nataneToonShader.name}");

                // Change shader
                targetMaterial.shader = nataneToonShader;

                // Map properties
                MapPropertiesWithReport(originalProperties, targetMaterial, report);

                // ConversionModeに応じて機能の有効化/無効化を制御
                if (conversionMode == ConversionMode.MinimalSafe)
                {
                    // 旧動作: 基本タブ以外の機能をすべてオフにする
                    DisableNonBasicFeatures(targetMaterial, report);
                }
                else
                {
                    // VisualMatchモード: MapPropertiesWithReportで有効化した機能をそのまま保持
                    report.infos.Add("VisualMatchモード: 変換されたエフェクトを有効状態で保持");
                }

                // プレビュー機能
                if (showPreview)
                {
                    previewMaterial = targetMaterial;
                    Selection.activeObject = targetMaterial;
                }

                EditorUtility.SetDirty(targetMaterial);
                AssetDatabase.SaveAssets();

                Debug.Log($"Successfully converted: {sourceMaterial.name}");
                report.success = true;
                return report;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to convert {sourceMaterial.name}: {e.Message}");
                report.success = false;
                report.warnings.Add($"エラーが発生しました: {e.Message}");
                return report;
            }
        }

        private void DetectMultipleShadowLayers(Dictionary<string, object> properties, ConversionReport report)
        {
            // lilToonの2nd, 3rd shadowレイヤーを検出
            bool has2ndShadow = properties.ContainsKey("_Shadow2ndColor");
            bool has3rdShadow = properties.ContainsKey("_Shadow3rdColor");

            if (has2ndShadow || has3rdShadow)
            {
                report.hasMultipleShadowLayers = true;
                string layerInfo = has3rdShadow ? "3層" : "2層";
                report.infos.Add($"複数シャドウレイヤー（{layerInfo}）を検出。マルチシャドウとして変換します。");
            }
        }

        private Dictionary<string, object> CaptureProperties(Material material)
        {
            var properties = new Dictionary<string, object>();

            // =============================================
            // lilToon の実際のプロパティ名に基づくキャプチャ
            // (GitHub: lilxyzw/lilToon lts.shader より)
            // =============================================

            // === Core ===
            CaptureTexture(material, "_MainTex", properties);
            CaptureColor(material, "_Color", properties);
            CaptureFloat(material, "_Cutoff", properties);

            // === Normal Map ===
            CaptureTexture(material, "_BumpMap", properties);
            CaptureFloat(material, "_BumpScale", properties);

            // === lilToon Feature Toggles (重要！) ===
            CaptureFloat(material, "_UseShadow", properties);
            CaptureFloat(material, "_UseRim", properties);
            CaptureFloat(material, "_UseRimShade", properties);
            CaptureFloat(material, "_UseMatCap", properties);
            CaptureFloat(material, "_UseMatCap2nd", properties);
            CaptureFloat(material, "_UseEmission", properties);
            CaptureFloat(material, "_UseEmission2nd", properties);
            CaptureFloat(material, "_UseOutline", properties);

            // === Shadow (lilToonの実際のプロパティ名: _ShadowBorder, _ShadowBlur) ===
            CaptureColor(material, "_ShadowColor", properties);
            CaptureFloat(material, "_ShadowBorder", properties);
            CaptureFloat(material, "_ShadowBlur", properties);
            CaptureFloat(material, "_ShadowStrength", properties);
            CaptureFloat(material, "_ShadowNormalStrength", properties);

            // Shadow 2nd/3rd
            CaptureColor(material, "_Shadow2ndColor", properties);
            CaptureFloat(material, "_Shadow2ndBorder", properties);
            CaptureFloat(material, "_Shadow2ndBlur", properties);
            CaptureColor(material, "_Shadow3rdColor", properties);
            CaptureFloat(material, "_Shadow3rdBorder", properties);
            CaptureFloat(material, "_Shadow3rdBlur", properties);

            // Shadow textures
            CaptureTexture(material, "_ShadowColorTex", properties);
            CaptureTexture(material, "_ShadowStrengthMask", properties);
            CaptureTexture(material, "_ShadowBorderMask", properties);
            CaptureTexture(material, "_ShadowBlurMask", properties);

            // === Rim Light ===
            CaptureColor(material, "_RimColor", properties);
            CaptureTexture(material, "_RimColorTex", properties);
            CaptureFloat(material, "_RimBorder", properties);
            CaptureFloat(material, "_RimBlur", properties);
            CaptureFloat(material, "_RimFresnelPower", properties);
            CaptureFloat(material, "_RimEnableLighting", properties);
            CaptureFloat(material, "_RimShadowMask", properties);

            // === Outline ===
            CaptureColor(material, "_OutlineColor", properties);
            CaptureFloat(material, "_OutlineWidth", properties);
            CaptureFloat(material, "_OutlineFixWidth", properties);
            CaptureTexture(material, "_OutlineTex", properties);

            // === Emission ===
            CaptureTexture(material, "_EmissionMap", properties);
            CaptureColor(material, "_EmissionColor", properties);
            CaptureTexture(material, "_Emission2ndMap", properties);
            CaptureColor(material, "_Emission2ndColor", properties);

            // === MatCap ===
            CaptureTexture(material, "_MatCapTex", properties);
            CaptureColor(material, "_MatCapColor", properties);
            CaptureFloat(material, "_MatCapBlend", properties);
            CaptureFloat(material, "_MatCapBlendMode", properties);
            // MatCap 2nd
            CaptureTexture(material, "_MatCap2ndTex", properties);
            CaptureColor(material, "_MatCap2ndColor", properties);
            CaptureFloat(material, "_MatCap2ndBlend", properties);
            CaptureFloat(material, "_MatCap2ndBlendMode", properties);

            // === Specular / Surface ===
            CaptureFloat(material, "_Smoothness", properties);
            CaptureFloat(material, "_Metallic", properties);
            CaptureFloat(material, "_SpecularToon", properties);
            CaptureFloat(material, "_SpecularBorder", properties);
            CaptureFloat(material, "_SpecularBlur", properties);

            // === Light Color Limits (lilToon固有) ===
            CaptureFloat(material, "_LightMinLimit", properties);
            CaptureFloat(material, "_LightMaxLimit", properties);
            CaptureFloat(material, "_MonochromeLighting", properties);
            CaptureFloat(material, "_AsUnlit", properties);

            return properties;
        }

        private void CaptureTexture(Material material, string propertyName, Dictionary<string, object> properties)
        {
            if (material.HasProperty(propertyName))
            {
                properties[propertyName] = material.GetTexture(propertyName);
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

        private void MapProperties(Dictionary<string, object> sourceProps, Material targetMaterial)
        {
            var dummyReport = new ConversionReport();
            MapPropertiesWithReport(sourceProps, targetMaterial, dummyReport);
        }

        private void MapPropertiesWithReport(Dictionary<string, object> sourceProps, Material targetMaterial, ConversionReport report)
        {
            // =============================================
            // lilToon → Natane Toon Shader プロパティマッピング
            // lilToonの実際のプロパティ名に基づく (lts.shader)
            // =============================================

            // === Main Texture & Color ===
            SetTextureIfExists(sourceProps, "_MainTex", targetMaterial, "_MainTex");
            SetColorIfExists(sourceProps, "_Color", targetMaterial, "_Color");

            // === Shadow ===
            bool useShadow = GetFloatOr(sourceProps, "_UseShadow", 0) > 0.5f;

            if (sourceProps.ContainsKey("_ShadowColor"))
            {
                Color shadowColor = (Color)sourceProps["_ShadowColor"];

                // ShadowStrength → ShadowColorに適用
                float shadowStrength = GetFloatOr(sourceProps, "_ShadowStrength", 1.0f);
                if (shadowStrength < 0.99f)
                {
                    shadowColor = Color.Lerp(Color.white, shadowColor, shadowStrength);
                    report.infos.Add($"ShadowStrength: {shadowStrength:F2} → ShadowColorに適用");
                }

                targetMaterial.SetColor("_ShadowColor", shadowColor);

                // シャドウカラーモデルの差異をレポートに記載
                // lilToon: indirectCol = albedo * _ShadowColor (乗算方式)
                //   → ShadowColorは各チャネルの減衰率として機能
                // Natane: directResult = lerp(_ShadowColor, (1,1,1), shadingValue)
                //   → ShadowColor自体が影の色（albedoとは後で乗算）
                // この方式の違いにより、特にアルベドが彩度の高い色の場合に影色が異なる場合がある
                if (shadowColor.r < 0.95f || shadowColor.g < 0.95f || shadowColor.b < 0.95f)
                {
                    report.infos.Add(
                        $"Shadow Color: ({shadowColor.r:F2},{shadowColor.g:F2},{shadowColor.b:F2})" +
                        " ※lilToon=乗算方式/Natane=lerp方式のため影色が完全一致しない場合があります");
                }
            }

            // Shadow Border & Blur → Natane Shadow パラメータ
            // =============================================
            // lilToonとNataneのシャドウモデルの違い:
            //
            // lilToon: NdotL = dot(L,N) * 0.5 + 0.5  (Half-Lambert, 範囲[0.5,1.0]前面)
            //   toon = smoothstep(border-blur, border+blur, halfLambert_ndotl)
            //
            // Natane: lightTerm = saturate(raw_ndotl)  ← ApplyLightBlendで[0,1]にクリップ
            //   ToonShading: adjusted = saturate(lightTerm + offset)
            //     steps=1: smoothstep(0.5-sr, 0.5+sr, adjusted)  where sr = sharpness * 0.5
            //
            // Half-Lambert の重要な特性:
            //   前面(ndotl≥0): halfLambert ∈ [0.5, 1.0]
            //   border=0.5の場合: ndotl=0（直角面）で halfLambert=0.5 → toon≈0.5（半分明るい）
            //   → lilToonでは前面が完全な影にならない！
            //
            // Nataneで同じ挙動を再現するため:
            //   offset = 0.5 の場合: ndotl=0 → adjusted=0.5 → toon≈0.5 ← lilToonと一致！
            //   ※ maxOffset クランプを除去（lilToonと同様に前面は完全な影にならなくてOK）
            //
            // 変換式:
            //   offset = 1.5 - 2*border  (Half-Lambert→raw ndotl空間の変換)
            //   sharpness = 2*blur       (HL空間→raw ndotl空間の幅補正)
            //   steps = 1                (lilToonは2トーン = 明暗1境界)
            // =============================================
            if (sourceProps.ContainsKey("_ShadowBorder"))
            {
                float border = (float)sourceProps["_ShadowBorder"];
                float blur = GetFloatOr(sourceProps, "_ShadowBlur", 0.1f);

                // Steps=1: lilToonの基本2トーン（明暗1境界）に対応
                targetMaterial.SetFloat("_ShadowSteps", 1);

                // ShadowSharpness: lilToonのblur幅をNataneのsmoothstep幅に変換
                float sharpness = Mathf.Clamp(blur * 2.0f, 0.01f, 1.0f);
                targetMaterial.SetFloat("_ShadowSharpness", sharpness);

                // ShadowOffset: Half-Lambert空間からraw ndotl空間への変換
                // lilToonのHalf-Lambert (ndotl*0.5+0.5) で border=B の場合:
                //   影境界はraw ndotl = 2*B - 1 で発生
                //   Nataneではoffset = 0.5 + (1-2*B)*0.5 = 1.0 - B に相当
                //   ただし直角面（ndotl=0）で ~50% lit になるのがlilToonの特徴
                //   offset = 0.5 でこれを正確に再現できる
                float shadowOffset = 1.0f - border;
                shadowOffset = Mathf.Clamp(shadowOffset, -0.5f, 0.95f);
                targetMaterial.SetFloat("_ShadowOffset", shadowOffset);

                // ShadowBlend=0, StepBorderSmooth=0: sharpnessだけで幅を制御（二重適用防止）
                targetMaterial.SetFloat("_ShadowBlend", 0);
                targetMaterial.SetFloat("_StepBorderSmooth", 0);

                report.infos.Add($"Shadow: Border={border:F2}→Offset={shadowOffset:F2}, Blur={blur:F2}→Sharpness={sharpness:F3}, Steps=1");
            }
            else
            {
                // デフォルト設定
                targetMaterial.SetFloat("_ShadowSteps", 1);
                targetMaterial.SetFloat("_ShadowSharpness", 0.2f);
                targetMaterial.SetFloat("_ShadowOffset", 0.5f);
            }

            // マルチシャドウレイヤー変換（_UseShadowが有効な場合のみ）
            // lilToonではマテリアルにデフォルトで_Shadow2ndColorが存在するが、
            // _UseShadow=0の場合は使用されていない
            if (useShadow)
            {
                MapMultiShadowLayers(sourceProps, targetMaterial, report);
            }

            // === Shadow Color Texture ===
            // lilToon: _ShadowColorTex → カスタム影色テクスチャ
            // Natane: _ShadowColorTex + _ShadowColorTexStrength
            if (sourceProps.ContainsKey("_ShadowColorTex") && sourceProps["_ShadowColorTex"] != null)
            {
                targetMaterial.SetTexture("_ShadowColorTex", (Texture)sourceProps["_ShadowColorTex"]);
                targetMaterial.SetFloat("_ShadowColorTexStrength", 1.0f);
                report.infos.Add("Shadow Color Texture: マッピング完了");
            }

            // === Shadow Normal Strength → BumpScale 調整 ===
            // lilToon: _ShadowNormalStrength (0-1) → 法線マップがシャドウに与える影響度
            // Nataneでは _BumpScale で法線強度を直接制御する
            // _ShadowNormalStrength < 1.0 の場合、影へのノーマルの影響を弱めたい意図
            // → _BumpScale を ShadowNormalStrength で調整（元のBumpScaleとの乗算）
            float shadowNormalStrength = GetFloatOr(sourceProps, "_ShadowNormalStrength", 1.0f);
            if (shadowNormalStrength < 0.99f)
            {
                float currentBumpScale = GetFloatOr(sourceProps, "_BumpScale", 1.0f);
                float adjustedBumpScale = currentBumpScale * shadowNormalStrength;
                targetMaterial.SetFloat("_BumpScale", adjustedBumpScale);
                report.infos.Add($"Shadow Normal: Strength={shadowNormalStrength:F2} × BumpScale={currentBumpScale:F2} → {adjustedBumpScale:F2}");
            }

            // === Normal Map ===
            SetTextureIfExists(sourceProps, "_BumpMap", targetMaterial, "_BumpMap");
            SetFloatIfExists(sourceProps, "_BumpScale", targetMaterial, "_BumpScale");

            if (sourceProps.ContainsKey("_BumpMap") && sourceProps["_BumpMap"] != null)
            {
                targetMaterial.SetFloat("_UseNormalMap", 1.0f);
                targetMaterial.EnableKeyword("_NORMALMAP");
            }

            // === Rim Light ===
            // lilToon: _UseRim=1 で有効化
            bool useRim = GetFloatOr(sourceProps, "_UseRim", 0) > 0.5f;

            if (useRim)
            {
                targetMaterial.SetFloat("_RimLight", 1.0f);
                targetMaterial.EnableKeyword("_RIM_LIGHT");

                // RimColor
                if (sourceProps.ContainsKey("_RimColor"))
                {
                    targetMaterial.SetColor("_RimColor", (Color)sourceProps["_RimColor"]);
                }

                // RimFresnelPower → RimPower
                float rimPower = GetFloatOr(sourceProps, "_RimFresnelPower", 3.5f);
                targetMaterial.SetFloat("_RimPower", Mathf.Clamp(rimPower, 0.1f, 10f));
                report.infos.Add($"Rim: FresnelPower={rimPower:F2}→RimPower={Mathf.Clamp(rimPower, 0.1f, 10f):F2}");

                // RimIntensity: lilToonでは色のアルファと強度で制御
                targetMaterial.SetFloat("_RimIntensity", 1.0f);

                // RimBorder → RimSpread (逆相関)
                // lilToon: border=0.5→リム中程度, border=0→広い, border=1→狭い
                float rimBorder = GetFloatOr(sourceProps, "_RimBorder", 0.5f);
                // NataneのRimSpreadは0-1で、0=通常、値が大きいほど広がる
                // lilToonのborderが小さいほどリムが広い
                float rimSpread = Mathf.Clamp01(1.0f - rimBorder);
                targetMaterial.SetFloat("_RimSpread", rimSpread);
                report.infos.Add($"Rim: Border={rimBorder:F2}→Spread={rimSpread:F2}");

                // RimBlur → RimPowerの微調整
                float rimBlur = GetFloatOr(sourceProps, "_RimBlur", 0.65f);
                if (rimBlur > 0.01f)
                {
                    float currentPower = targetMaterial.GetFloat("_RimPower");
                    // blurが大きいほどpowerを下げて柔らかくする
                    float adjustedPower = currentPower * (1.0f - rimBlur * 0.5f);
                    targetMaterial.SetFloat("_RimPower", Mathf.Max(adjustedPower, 0.1f));
                    report.infos.Add($"Rim: Blur={rimBlur:F2}→Power調整={adjustedPower:F2}");
                }

                // RimEnableLighting → RimDirStrength
                float rimEnableLighting = GetFloatOr(sourceProps, "_RimEnableLighting", 1.0f);
                targetMaterial.SetFloat("_RimDirStrength", rimEnableLighting);

                // RimShadowMask
                float rimShadowMask = GetFloatOr(sourceProps, "_RimShadowMask", 0.5f);
                targetMaterial.SetFloat("_RimShadowMask", rimShadowMask);

                // RimBlendMode変換
                // lilToon: 0=Normal(lerp), 1=Add, 2=Screen, 3=Multiply
                // → lilBlendColorは全モードで lerp(dst, blended, srcA) を使う
                // Natane: 0=Normal(SoftLight), 1=Soft, 2=Screen, 3=Overlay
                // lilToonのデフォルトは Add(1) だが、lilToon側のRimBlendModeプロパティ名は不明なため
                // キャプチャされていない場合はデフォルト(Add=1)として処理
                // ※ lilToonでは _RimBlendMode プロパティは存在しない（lilBlendColorの引数で直接指定）
                // → lilToonのリムは基本的にAdd合成で固定
                // → Natane側もAdd的な Normal(0) をデフォルトにする
                targetMaterial.SetFloat("_RimBlendMode", 0); // Normal(SoftLight) = Add的な効果

                report.infos.Add($"Rim Light有効化: DirStrength={rimEnableLighting:F2}, ShadowMask={rimShadowMask:F2}, BlendMode=0(Normal)");
            }

            // === Outline ===
            // lilToonでは_UseOutlineはシェーダーバリアントで分かれるが、プロパティとしても存在
            bool hasOutline = false;
            if (sourceProps.ContainsKey("_OutlineWidth"))
            {
                float originalWidth = (float)sourceProps["_OutlineWidth"];
                if (originalWidth > 0)
                {
                    // lilToon: outlineWidth *= 0.01 (内部で1/100スケール)
                    // Natane:  outlineWidth = _OutlineWidth * 0.1 (内部で1/10スケール)
                    // → lilToonの値をNataneに変換するには 0.01/0.1 = 1/10 にスケール
                    float convertedWidth = originalWidth * 0.1f;
                    convertedWidth = Mathf.Clamp(convertedWidth, 0.001f, 1.0f);
                    targetMaterial.SetFloat("_OutlineWidth", convertedWidth);
                    hasOutline = true;

                    report.outlineWidthAdjusted = true;
                    report.originalOutlineWidth = originalWidth;
                    report.convertedOutlineWidth = convertedWidth;
                    report.infos.Add($"Outline Width: lilToon {originalWidth:F4} → Natane {convertedWidth:F4} (×0.1 scale)");
                }
            }

            if (sourceProps.ContainsKey("_OutlineColor"))
            {
                targetMaterial.SetColor("_OutlineColor", (Color)sourceProps["_OutlineColor"]);
                hasOutline = true;
            }

            if (hasOutline)
            {
                targetMaterial.SetFloat("_Outline", 1.0f);
                targetMaterial.EnableKeyword("_OUTLINE");
            }

            // === Emission ===
            bool useEmission = GetFloatOr(sourceProps, "_UseEmission", 0) > 0.5f;

            if (useEmission)
            {
                SetTextureIfExists(sourceProps, "_EmissionMap", targetMaterial, "_EmissionMap");
                if (sourceProps.ContainsKey("_EmissionColor"))
                {
                    targetMaterial.SetColor("_EmissionColor", (Color)sourceProps["_EmissionColor"]);
                }
                targetMaterial.SetFloat("_Emission", 1.0f);
                targetMaterial.EnableKeyword("_EMISSION");
                report.infos.Add("Emission有効化");
            }

            // === MatCap ===
            bool useMatCap = GetFloatOr(sourceProps, "_UseMatCap", 0) > 0.5f;

            if (useMatCap && sourceProps.ContainsKey("_MatCapTex") && sourceProps["_MatCapTex"] != null)
            {
                SetTextureIfExists(sourceProps, "_MatCapTex", targetMaterial, "_MatCapTex");

                targetMaterial.SetFloat("_MatCap", 1.0f);
                targetMaterial.EnableKeyword("_MATCAP");

                // MatCap パラメータの正しいマッピング:
                // lilToon: result = lilBlendColor(base, matCapTex * _MatCapColor, _MatCapBlend * matcap.a, mode)
                //   _MatCapBlend (0-1): 全体ブレンド量 (srcA に乗算)
                //   _MatCapColor: MatCapテクスチャに乗算するカラー
                //
                // Natane: matcap = matCapTex * _MatCapIntensity
                //         result = lerp(preMatCap, blended, _MatCapBlend)
                //   _MatCapIntensity (0-2): テクスチャの明度スケール
                //   _MatCapBlend (0-1): 全体ブレンド量
                //
                // → _MatCapIntensity=1.0 (テクスチャ強度は変えない)
                // → _MatCapBlend = lilToon _MatCapBlend (全体ブレンド量を維持)
                float matCapBlend = GetFloatOr(sourceProps, "_MatCapBlend", 1.0f);
                targetMaterial.SetFloat("_MatCapIntensity", 1.0f);
                targetMaterial.SetFloat("_MatCapBlend", matCapBlend);

                // _MatCapColor が白(1,1,1)でない場合は情報をレポートに記載
                if (sourceProps.ContainsKey("_MatCapColor"))
                {
                    Color matCapColor = (Color)sourceProps["_MatCapColor"];
                    if (matCapColor.r < 0.95f || matCapColor.g < 0.95f || matCapColor.b < 0.95f)
                    {
                        report.warnings.Add(
                            $"MatCap Color ({matCapColor.r:F2},{matCapColor.g:F2},{matCapColor.b:F2}) が白ではありません。" +
                            "lilToonではMatCapテクスチャにカラーを乗算しますが、Nataneでは_MatCapIntensityのみで制御します。" +
                            "必要に応じてMatCapテクスチャ自体を調整してください。");
                    }
                }

                // MatCap Blend Mode変換
                // lilToon: 0=Normal(lerp), 1=Add, 2=Screen, 3=Multiply
                // Natane:  0=Add, 1=Multiply, 2=Replace
                // ★ Normal(0) → Replace(2) が白飛び防止の鍵！
                int lilBlendMode = 1; // lilToonのデフォルトはAdd(1)
                if (sourceProps.ContainsKey("_MatCapBlendMode"))
                {
                    lilBlendMode = (int)(float)sourceProps["_MatCapBlendMode"];
                }
                int nataneBlendMode = ConvertMatCapBlendMode(lilBlendMode);
                targetMaterial.SetFloat("_MatCapBlendMode", nataneBlendMode);

                string[] lilModeNames = {"Normal(lerp)", "Add", "Screen", "Multiply"};
                string[] nataneModeNames = {"Add", "Multiply", "Replace"};
                string lilName = lilBlendMode >= 0 && lilBlendMode < 4 ? lilModeNames[lilBlendMode] : $"Unknown({lilBlendMode})";
                string nataneName = nataneBlendMode >= 0 && nataneBlendMode < 3 ? nataneModeNames[nataneBlendMode] : $"Unknown({nataneBlendMode})";
                report.infos.Add($"MatCap有効化: Blend={matCapBlend:F2}, Intensity=1.0, BlendMode: {lilName}→{nataneName}");
            }

            // === Specular ===
            // lilToonのスペキュラはPBRベースで、_SpecularToon=1/_SpecularBorder=0.5はデフォルト値。
            // ほとんどのトゥーンマテリアルではスペキュラは目立たないため、
            // _Metallic > 0 の場合のみNataneのトゥーンスペキュラを有効化する。
            float metallic = GetFloatOr(sourceProps, "_Metallic", 0);
            if (metallic > 0.01f)
            {
                bool hasSpecular = false;

                if (sourceProps.ContainsKey("_SpecularToon"))
                {
                    float specToon = (float)sourceProps["_SpecularToon"];
                    if (specToon > 0.5f && sourceProps.ContainsKey("_SpecularBorder"))
                    {
                        float specBorder = (float)sourceProps["_SpecularBorder"];
                        float specBlur = GetFloatOr(sourceProps, "_SpecularBlur", 0.0f);

                        float size = Mathf.Clamp01(1.0f - specBorder);
                        float softness = Mathf.Clamp01(specBlur);
                        targetMaterial.SetFloat("_SpecularSize", size);
                        targetMaterial.SetFloat("_SpecularSoftness", Mathf.Max(softness, 0.05f));
                        hasSpecular = true;
                        report.infos.Add($"Specular(Toon): Border={specBorder:F2}→Size={size:F2}, Metallic={metallic:F2}");
                    }
                }

                if (!hasSpecular && sourceProps.ContainsKey("_Smoothness"))
                {
                    float smoothness = (float)sourceProps["_Smoothness"];
                    float specularSize = OptimizeSpecularSize(smoothness);
                    targetMaterial.SetFloat("_SpecularSize", specularSize);
                    hasSpecular = true;
                    report.infos.Add($"Specular(PBR): Smoothness={smoothness:F2}→Size={specularSize:F3}");
                }

                if (hasSpecular)
                {
                    targetMaterial.SetFloat("_Specular", 1.0f);
                    targetMaterial.EnableKeyword("_SPECULAR");
                    targetMaterial.SetColor("_SpecularColor", new Color(1, 1, 1, 1));
                    report.infos.Add("Specular有効化 (Metallic素材)");
                }
            }
            else
            {
                report.infos.Add($"Specularスキップ: Metallic={metallic:F2} (非金属のため無効)");
            }

            // === Alpha Cutoff ===
            if (sourceProps.ContainsKey("_Cutoff"))
            {
                float cutoff = (float)sourceProps["_Cutoff"];
                targetMaterial.SetFloat("_Cutoff", cutoff);
                report.infos.Add($"Alpha Cutoff: {cutoff:F2}");
            }

            // === Brightness Compensation ===
            // Natane's lighting pipeline has a normalize+luminance reconstruction step
            // (Fragment.hlsl:521-524) that darkens the final output.
            //
            // For uniform colors: directResult = normalize(v) × luminance(v) = v/√3
            // → darkening factor = 1/√3 ≈ 0.577 (42% darker)
            //
            // For colored values: factor = luminance(v)/magnitude(v) ≤ 1/√3
            // → colored shadows can be darkened even more (up to 50-55%)
            //
            // lilToon does NOT have this step, so migrated materials appear too dark.
            //
            // Compensation strategy:
            //   _LightIntensity = √3 ≈ 1.732  (applied before normalize, exactly cancels the
            //                                    darkening for uniform colors in ForwardBase)
            //   _Brightness = 1.0              (neutral — user can manually adjust up to 5.0)
            //
            // Math proof for lit areas:
            //   directResult = (1,1,1) × √3 = (√3,√3,√3)
            //   luminance = √3, normalize = (1/√3,1/√3,1/√3)
            //   result = (1/√3) × √3 = (1,1,1) ✓ — perfect match
            // Nataneのnormalize+luminanceステップの補償戦略:
            //
            // Fragment.hlsl:521-524 の normalize+luminance ステップ:
            //   directLum = luminance(directResult)
            //   directLum = clamp(directLum, _LightMinInfluence, _LightMaxInfluence)
            //   directDir = normalize(max(directResult, 0.01))
            //   directResult = directDir * directLum
            //
            // 均一色(a,a,a)でLI=_LightIntensityの場合:
            //   directResult = (a*LI, a*LI, a*LI)
            //   directLum = a*LI, directDir = (1/√3, 1/√3, 1/√3)
            //   result = a*LI/√3 per channel
            //
            // LI=2√3 の場合: result = a*2√3/√3 = 2a → lilToonの2倍明るさに一致
            //
            // _LightIntensity に明るさ補正を集約することで:
            //   - _Brightness=1.0, _Saturation=1.0 のまま自然なインスペクター表示を維持
            //   - ライティングパス内で補正が完結（エフェクトには影響しない）
            //   - ForwardAddにはnormalize+luminanceがないため、ForwardAdd側は
            //     _LightIntensityの増加分がそのまま適用されるが、追加ライトは
            //     _AdditionalLightIntensity(default=0.5)で制御されるため実用上問題ない
            //
            // _LightMaxInfluence: デフォルト2.0ではdirectLum=3.464がクランプされるため、
            //   4.0に拡張してクランプを防止する。
            float lightIntensity = Mathf.Sqrt(3.0f) * 2.0f; // 2√3 ≈ 3.464
            targetMaterial.SetFloat("_LightIntensity", lightIntensity);
            targetMaterial.SetFloat("_LightMaxInfluence", 4.0f);
            targetMaterial.SetFloat("_Brightness", 1.0f);
            targetMaterial.SetFloat("_Saturation", 1.0f);
            report.infos.Add($"色調補正: _LightIntensity={lightIntensity:F3} (2√3), _LightMaxInfluence=4.0, _Brightness=1.0, _Saturation=1.0");

            // === Shadow Floor Compensation ===
            // lilToonのHalf-Lambertでは裏面でもhalfLambert=0.0で、
            // shadowColor自体が最低明度を保持する。
            // Nataneの_ShadowMaxDarknessで影の最低明度を底上げし、
            // 彩度の高いシャドウカラーのnormalize暗化を補償する。
            //
            // また _LightMinInfluence を設定して、
            // ライティング計算結果の最低明度を保証する（lilToonの_LightMinLimitに相当）。
            targetMaterial.SetFloat("_ShadowMaxDarkness", 0.15f);
            targetMaterial.SetFloat("_LightMinInfluence", 0.05f);
            report.infos.Add("Shadow Floor補正: _ShadowMaxDarkness=0.15, _LightMinInfluence=0.05");

            // === GI Intensity Compensation ===
            // lilToonはSH(環境光)を暗黙的にフル強度(1.0相当)で使用する。
            // Nataneのデフォルトは _GIIntensity=0.5 で、環境光が半分になり暗く見える。
            // 移行時はlilToonに合わせて1.0に設定する。
            targetMaterial.SetFloat("_GIIntensity", 1.0f);
            report.infos.Add("GI補正: _GIIntensity=1.0 (lilToonと同等の環境光強度)");

            // === Light Color Limits (lilToon互換) ===
            // lilToon: lightColor = clamp(lightColor, _LightMinLimit, _LightMaxLimit)
            //   _LightMinLimit (default=0.05): 暗いワールドでの最低保証
            //   _LightMaxLimit (default=1.0): 強いライトの上限
            //   _MonochromeLighting (default=0): ライト色のグレースケール化
            //
            // Natane: effectiveLightColor = clamp(effectiveLightColor, _LightColorMin, _LightColorMax)
            //
            // ★重要: _LightIntensity=2√3 の増幅を考慮した変換が必要
            // lilToonではlightColor=1.0で最終ライティング≈1.0（増幅なし）
            // Nataneではlight=1.0 → _LightIntensity=2√3で増幅 → normalize後 ≈2.0
            // → lilToonの_LightMaxLimit=1.0と同等にするには、_LightColorMax = 0.5
            //
            // 一般式: _LightColorMax = lilToon_LightMaxLimit × √3 / _LightIntensity
            //        = lilToon_LightMaxLimit × √3 / (2√3) = lilToon_LightMaxLimit / 2
            float lightMinLimit = GetFloatOr(sourceProps, "_LightMinLimit", 0.05f);
            float lightMaxLimit = GetFloatOr(sourceProps, "_LightMaxLimit", 1.0f);
            float monochromeLighting = GetFloatOr(sourceProps, "_MonochromeLighting", 0.0f);

            // _LightIntensity=2√3 の増幅を逆算してLightColorMaxを設定
            float nataneLightColorMax = lightMaxLimit * 0.5f;
            targetMaterial.SetFloat("_LightColorMin", lightMinLimit);
            targetMaterial.SetFloat("_LightColorMax", nataneLightColorMax);
            targetMaterial.SetFloat("_MonochromeLighting", monochromeLighting);
            report.infos.Add($"ライト制限: lilToon MaxLimit={lightMaxLimit:F2} → Natane ColorMax={nataneLightColorMax:F2} (÷2補正), ColorMin={lightMinLimit:F2}, Monochrome={monochromeLighting:F2}");

            // _AsUnlit → _LightColorMin への反映
            // lilToon _AsUnlit: 0=通常ライティング, 1=完全アンライト
            // → _AsUnlit > 0 の場合、_LightColorMin を上げてアンライト効果を近似する
            float asUnlit = GetFloatOr(sourceProps, "_AsUnlit", 0.0f);
            if (asUnlit > 0.01f)
            {
                float unlitMin = Mathf.Lerp(lightMinLimit, nataneLightColorMax, asUnlit);
                targetMaterial.SetFloat("_LightColorMin", unlitMin);
                report.infos.Add($"AsUnlit={asUnlit:F2} → LightColorMin={unlitMin:F2} (アンライト近似)");
            }
        }

        /// <summary>
        /// sourcePropsからfloatを取得、なければデフォルト値を返す
        /// </summary>
        private float GetFloatOr(Dictionary<string, object> props, string key, float defaultValue)
        {
            if (props.ContainsKey(key))
                return (float)props[key];
            return defaultValue;
        }

        private void SetTextureIfExists(Dictionary<string, object> source, string sourceKey, Material target, string targetKey)
        {
            if (source.ContainsKey(sourceKey) && source[sourceKey] != null)
            {
                target.SetTexture(targetKey, (Texture)source[sourceKey]);
            }
        }

        private void SetColorIfExists(Dictionary<string, object> source, string sourceKey, Material target, string targetKey)
        {
            if (source.ContainsKey(sourceKey))
            {
                target.SetColor(targetKey, (Color)source[sourceKey]);
            }
        }

        private void SetFloatIfExists(Dictionary<string, object> source, string sourceKey, Material target, string targetKey)
        {
            if (source.ContainsKey(sourceKey))
            {
                target.SetFloat(targetKey, (float)source[sourceKey]);
            }
        }

        /// <summary>
        /// SmoothnessからSpecular Sizeへ変換（非線形カーブ）
        /// lilToonのSmoothnessは0-1の範囲、Natane Toon ShaderのSpecularSizeは0.01-1.0が推奨
        /// 1.5乗カーブで中間値をやや小さくし、より自然なハイライトに
        /// </summary>
        private float OptimizeSpecularSize(float smoothness)
        {
            // smoothness: 0 (rough) → 1 (smooth/glossy)
            // specularSize: 0.01 (small highlight) → 0.5 (large highlight)

            float normalized = Mathf.Clamp01(smoothness);
            float curved = Mathf.Pow(normalized, 1.5f); // 1.5乗で中間値をやや小さく
            float specularSize = Mathf.Lerp(0.01f, 0.5f, curved);

            return specularSize;
        }

        /// <summary>
        /// lilToonのMatCapBlendModeをNatane Toon ShaderのBlendModeに変換
        /// lilToon: 0=Normal(lerp), 1=Add, 2=Screen, 3=Multiply
        /// Natane:  0=Add, 1=Multiply, 2=Replace
        ///
        /// ★重要: lilToon Normal(0) = lerp(dst, src, srcA) = アルファブレンド
        ///   → Natane Add(0)にマッピングすると白飛びする！
        ///   → Replace(2) = lerp(base, matcap, blend) が最も近い
        /// </summary>
        private int ConvertMatCapBlendMode(int lilBlendMode)
        {
            switch (lilBlendMode)
            {
                case 0: return 2; // Normal(lerp) → Replace (★白飛び修正: Add→Replace)
                case 1: return 0; // Add → Add
                case 2: return 0; // Screen → Add (近似、NataneにScreen未対応)
                case 3: return 1; // Multiply → Multiply
                default: return 2; // 不明 → Replace (安全)
            }
        }

        /// <summary>
        /// lilToonのRimBlendModeをNatane Toon ShaderのRimBlendModeに変換
        /// lilToon: 0=Normal(lerp), 1=Add, 2=Screen, 3=Multiply
        /// Natane:  0=Normal(SoftLight), 1=Soft, 2=Screen, 3=Overlay
        /// </summary>
        private int ConvertRimBlendMode(int lilBlendMode)
        {
            switch (lilBlendMode)
            {
                case 0: return 2; // Normal(lerp) → Screen (lerpの近似でScreenが最も自然)
                case 1: return 0; // Add → Normal(SoftLight) (加算的効果)
                case 2: return 2; // Screen → Screen
                case 3: return 3; // Multiply → Overlay (減衰系の近似)
                default: return 0;
            }
        }

        /// <summary>
        /// lilToonのシェーダー名からNatane Toon Shaderのバリアントを自動検出
        /// </summary>
        private Shader DetectNataneShaderVariant(Material sourceMaterial)
        {
            string shaderName = sourceMaterial.shader.name.ToLower();

            if (shaderName.Contains("transparent") || shaderName.Contains("fade"))
            {
                Shader transparentShader = Shader.Find("Natane/Toon Shader Transparent");
                if (transparentShader != null) return transparentShader;
            }
            else if (shaderName.Contains("cutout"))
            {
                Shader cutoutShader = Shader.Find("Natane/Toon Shader Cutout");
                if (cutoutShader != null) return cutoutShader;
            }

            // デフォルト: Opaqueバリアント（フォールバック含む）
            return Shader.Find("Natane/Toon Shader");
        }

        /// <summary>
        /// マルチシャドウレイヤー変換（2nd/3rd Shadow）
        /// lilToon: _Shadow2ndColor, _Shadow2ndBorder (0-1), _Shadow3rdColor, _Shadow3rdBorder (0-1)
        /// Natane:  _Shadow2ndColor, _Shadow2ndBorder, _Shadow3rdColor, _Shadow3rdBorder + _USE_MULTI_SHADOW keyword
        /// </summary>
        private void MapMultiShadowLayers(Dictionary<string, object> sourceProps, Material targetMaterial, ConversionReport report)
        {
            bool has2nd = sourceProps.ContainsKey("_Shadow2ndColor");
            bool has3rd = sourceProps.ContainsKey("_Shadow3rdColor");

            if (!has2nd && !has3rd) return;

            // Check if 2nd shadow is actually meaningful (not just default values)
            // lilToon always has _Shadow2ndColor as a property, but it may be unused
            bool meaningful2nd = false;
            if (has2nd)
            {
                Color shadow2nd = (Color)sourceProps["_Shadow2ndColor"];
                // Consider meaningful if alpha > 0 and color isn't pure white (default/unused)
                meaningful2nd = shadow2nd.a > 0.01f &&
                    (shadow2nd.r < 0.99f || shadow2nd.g < 0.99f || shadow2nd.b < 0.99f);
            }

            bool meaningful3rd = false;
            if (has3rd)
            {
                Color shadow3rd = (Color)sourceProps["_Shadow3rdColor"];
                meaningful3rd = shadow3rd.a > 0.01f &&
                    (shadow3rd.r < 0.99f || shadow3rd.g < 0.99f || shadow3rd.b < 0.99f);
            }

            if (!meaningful2nd && !meaningful3rd)
            {
                report.infos.Add("Multi-shadow: 2nd/3rdシャドウはデフォルト値のためスキップ");
                return;
            }

            // Enable multi-shadow keyword
            targetMaterial.EnableKeyword("_USE_MULTI_SHADOW");

            if (meaningful2nd)
            {
                Color shadow2nd = (Color)sourceProps["_Shadow2ndColor"];
                targetMaterial.SetColor("_Shadow2ndColor", shadow2nd);
                float border2nd = GetFloatOr(sourceProps, "_Shadow2ndBorder", 0.15f);
                targetMaterial.SetFloat("_Shadow2ndBorder", border2nd);
                report.infos.Add($"2nd Shadow: Color={shadow2nd}, Border={border2nd:F2}");
            }

            if (meaningful3rd)
            {
                Color shadow3rd = (Color)sourceProps["_Shadow3rdColor"];
                targetMaterial.SetColor("_Shadow3rdColor", shadow3rd);
                float border3rd = GetFloatOr(sourceProps, "_Shadow3rdBorder", 0.25f);
                targetMaterial.SetFloat("_Shadow3rdBorder", border3rd);
                report.infos.Add($"3rd Shadow: Color={shadow3rd}, Border={border3rd:F2}");
            }
        }

        /// <summary>
        /// 基本タブ以外のすべての機能をオフにする
        /// プロパティの値は保持されるが、トグルとキーワードを無効化する
        /// 移行後にユーザーが必要な機能を個別に有効化することを想定
        /// </summary>
        private void DisableNonBasicFeatures(Material material, ConversionReport report)
        {
            // === ライティングタブ ===
            DisableFeature(material, "_SoftLightingMode", "_SOFT_LIGHTING_MODE");
            DisableFeature(material, "_UsePixelVertexLights", "_PIXEL_VERTEX_LIGHTS");
            DisableFeature(material, "_UseLightVolume", "_USE_LIGHT_VOLUME");
            DisableFeature(material, "_LightVolumeSpecular", "_LIGHT_VOLUME_SPECULAR");
            DisableFeature(material, "_LTCGI", "_LTCGI");
            DisableFeature(material, "_UseAO", "_USE_AO");
            DisableFeature(material, "_UseDithering", "_USE_DITHERING");

            // === エフェクトタブ ===
            DisableFeature(material, "_Specular", "_SPECULAR");
            DisableFeature(material, "_RimLight", "_RIM_LIGHT");
            DisableFeature(material, "_RimLight2", "_RIM_LIGHT_2");
            DisableFeature(material, "_SSS", "_SSS");
            DisableFeature(material, "_MatCap", "_MATCAP");
            DisableFeature(material, "_MatCap2", "_MATCAP_2");
            DisableFeature(material, "_MatCap3", "_MATCAP_3");
            DisableFeature(material, "_Glitter", "_GLITTER");
            DisableFeature(material, "_WaterDrip", "_WATER_DRIP");
            DisableFeature(material, "_Hologram", "_HOLOGRAM");
            DisableFeature(material, "_UseHologramNoise", "_HOLOGRAM_NOISE");
            DisableFeature(material, "_Glitch", "_GLITCH");
            DisableFeature(material, "_Outline", "_OUTLINE");
            DisableFeature(material, "_Emission", "_EMISSION");
            DisableFeature(material, "_Dissolve", "_DISSOLVE");
            DisableFeature(material, "_HueShiftEnable", "_HUE_SHIFT");
            DisableFeature(material, "_UseAlphaMask", "_ALPHA_MASK");
            DisableFeature(material, "_AudioLink", "_AUDIOLINK");

            // === 環境タブ ===
            DisableFeature(material, "_Reflection", "_REFLECTION");
            DisableFeature(material, "_Iridescence", "_IRIDESCENCE");
            DisableFeature(material, "_EnvRim", "_ENV_RIM");
            DisableFeature(material, "_Refraction", "_REFRACTION");

            // === 詳細タブ ===
            DisableFeature(material, "_UseNormalMap", "_NORMALMAP");
            DisableFeature(material, "_Parallax", "_PARALLAX");
            DisableFeature(material, "_VAT", "_VAT");
            DisableFeature(material, "_VATNormal", "_VAT_NORMAL");
            DisableFeature(material, "_BackfaceTexture", "_BACKFACE_TEXTURE");
            DisableFeature(material, "_VideoTexture", "_VIDEO_TEXTURE");
            DisableFeature(material, "_DistanceFade", "_DISTANCE_FADE");

            report.infos.Add("基本タブ以外の機能をすべてオフにしました（プロパティ値は保持）");
        }

        /// <summary>
        /// 個別の機能をオフにする（プロパティ値を0にし、キーワードを無効化）
        /// </summary>
        private void DisableFeature(Material material, string propertyName, string keyword)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, 0f);
            }
            material.DisableKeyword(keyword);
        }

        // ===================================
        // プレハブモード用メソッド群
        // ===================================

        /// <summary>
        /// プレハブ内のlilToonマテリアルをスキャンして一覧を構築
        /// </summary>
        private void ScanPrefabMaterials()
        {
            prefabMaterials.Clear();

            if (targetPrefab == null) return;

            Renderer[] renderers = targetPrefab.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                Material[] sharedMats = renderer.sharedMaterials;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    Material mat = sharedMats[i];
                    if (mat == null || mat.shader == null) continue;

                    string shaderName = mat.shader.name;
                    if (shaderName.Contains("lilToon") || shaderName.StartsWith("_lil/"))
                    {
                        prefabMaterials.Add(new PrefabMaterialInfo
                        {
                            original = mat,
                            converted = null,
                            renderer = renderer,
                            materialIndex = i,
                            willConvert = true,
                            rendererPath = GetHierarchyPath(renderer.transform, targetPrefab.transform)
                        });
                    }
                }
            }

            Debug.Log($"プレハブ '{targetPrefab.name}' から {prefabMaterials.Select(m => m.original).Distinct().Count()} 個のlilToonマテリアルを検出しました。");
        }

        /// <summary>
        /// Transform階層パスを取得（プレハブルートからの相対パス）
        /// </summary>
        private string GetHierarchyPath(Transform target, Transform root)
        {
            var parts = new List<string>();
            Transform current = target;

            while (current != null && current != root)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }

            return parts.Count > 0 ? string.Join("/", parts) : target.name;
        }

        /// <summary>
        /// プレハブ内の選択マテリアルを一括変換
        /// </summary>
        private void ConvertPrefabMaterials()
        {
            // 変換対象を取得（重複排除）
            var materialsToConvert = prefabMaterials
                .Where(m => m.willConvert && m.converted == null)
                .Select(m => m.original)
                .Distinct()
                .ToList();

            if (materialsToConvert.Count == 0) return;

            string dialogMessage = duplicateInHierarchy
                ? L($"'{targetPrefab.name}' の複製を作成し、{materialsToConvert.Count} 個のマテリアルを変換しますか？\n元のプレハブは変更されません。",
                    $"Create a duplicate of '{targetPrefab.name}' and convert {materialsToConvert.Count} materials?\nThe original prefab will not be modified.")
                : L($"'{targetPrefab.name}' 内の {materialsToConvert.Count} 個のマテリアルを変換してもよろしいですか？",
                    $"Are you sure you want to convert {materialsToConvert.Count} materials in '{targetPrefab.name}'?");

            if (!EditorUtility.DisplayDialog(
                L("プレハブマテリアル変換", "Convert Prefab Materials"),
                dialogMessage,
                L("はい", "Yes"), L("キャンセル", "Cancel")))
            {
                return;
            }

            // Undoグループ登録
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"lilToon Migration - {targetPrefab.name}");

            int successCount = 0;
            List<ConversionReport> reports = new List<ConversionReport>();
            Dictionary<Material, Material> materialMapping = new Dictionary<Material, Material>();

            for (int i = 0; i < materialsToConvert.Count; i++)
            {
                Material sourceMat = materialsToConvert[i];

                EditorUtility.DisplayProgressBar(
                    "Converting Prefab Materials",
                    $"Converting {i + 1}/{materialsToConvert.Count}: {sourceMat.name}",
                    (float)i / materialsToConvert.Count
                );

                var report = ConvertMaterialWithReport(sourceMat);
                reports.Add(report);

                if (report.success)
                {
                    successCount++;

                    // materialMappingを構築（複製モードでは常に新規マテリアルを使用）
                    if (!replaceOriginal || duplicateInHierarchy)
                    {
                        string sourcePath = AssetDatabase.GetAssetPath(sourceMat);
                        string newPath = sourcePath.Replace(".mat", "_NataneToon.mat");
                        Material newMat = AssetDatabase.LoadAssetAtPath<Material>(newPath);
                        if (newMat != null)
                        {
                            materialMapping[sourceMat] = newMat;
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            if (duplicateInHierarchy && materialMapping.Count > 0)
            {
                // 複製モード: ヒエラルキーに複製を作成し、複製のみマテリアルを差し替え
                DuplicateAndApplyMaterials(materialMapping);
            }
            else if (updatePrefabReferences && !replaceOriginal && materialMapping.Count > 0)
            {
                // 通常モード: 元のプレハブの参照を更新
                UpdatePrefabReferences(materialMapping);
            }

            // Undoグループを閉じる
            Undo.CollapseUndoOperations(undoGroup);

            // レポート表示
            ShowConversionReport(reports, successCount);

            // リスト更新
            ScanPrefabMaterials();
        }

        /// <summary>
        /// 個別のマテリアルを変換（プレハブモード用）
        /// </summary>
        private void ConvertSinglePrefabMaterial(Material sourceMaterial)
        {
            // Undoグループ登録
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"lilToon Migration - {sourceMaterial.name}");

            var report = ConvertMaterialWithReport(sourceMaterial);

            if (report.success)
            {
                string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                string newPath = sourcePath.Replace(".mat", "_NataneToon.mat");
                Material newMat = AssetDatabase.LoadAssetAtPath<Material>(newPath);

                if (newMat != null)
                {
                    var mapping = new Dictionary<Material, Material> { { sourceMaterial, newMat } };

                    if (duplicateInHierarchy)
                    {
                        // 複製モード: 複製を作成してマテリアルを差し替え
                        DuplicateAndApplyMaterials(mapping);
                    }
                    else if (updatePrefabReferences && !replaceOriginal)
                    {
                        // 通常モード: 元のプレハブの参照を更新
                        UpdatePrefabReferences(mapping);
                    }
                }

                ShowConversionReport(new List<ConversionReport> { report }, 1);
            }

            // Undoグループを閉じる
            Undo.CollapseUndoOperations(undoGroup);

            // リスト更新
            ScanPrefabMaterials();
        }

        /// <summary>
        /// プレハブのマテリアル参照を新しいマテリアルに更新
        /// シーン上のインスタンスとプレハブアセットの両方に対応
        /// </summary>
        private void UpdatePrefabReferences(Dictionary<Material, Material> materialMapping)
        {
            if (targetPrefab == null || materialMapping.Count == 0) return;

            // シーン上のインスタンスの場合はプレハブアセットパスを取得
            string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
            bool isSceneInstance = string.IsNullOrEmpty(prefabPath);

            if (isSceneInstance)
            {
                prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetPrefab);
            }

            // プレハブアセットを編集する場合
            if (!string.IsNullOrEmpty(prefabPath))
            {
                GameObject prefabContents = null;
                try
                {
                    prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

                    Renderer[] renderers = prefabContents.GetComponentsInChildren<Renderer>(true);
                    bool changed = false;

                    foreach (var renderer in renderers)
                    {
                        Material[] materials = renderer.sharedMaterials;
                        bool rendererChanged = false;

                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (materials[i] != null && materialMapping.ContainsKey(materials[i]))
                            {
                                materials[i] = materialMapping[materials[i]];
                                rendererChanged = true;
                            }
                        }

                        if (rendererChanged)
                        {
                            renderer.sharedMaterials = materials;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                        Debug.Log($"プレハブ '{System.IO.Path.GetFileName(prefabPath)}' のマテリアル参照を更新しました。");
                    }
                }
                finally
                {
                    if (prefabContents != null)
                    {
                        PrefabUtility.UnloadPrefabContents(prefabContents);
                    }
                }
            }

            // シーン上のインスタンスも直接更新
            if (isSceneInstance)
            {
                Renderer[] sceneRenderers = targetPrefab.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in sceneRenderers)
                {
                    Undo.RecordObject(renderer, "Update Material References");
                    Material[] materials = renderer.sharedMaterials;
                    bool rendererChanged = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null && materialMapping.ContainsKey(materials[i]))
                        {
                            materials[i] = materialMapping[materials[i]];
                            rendererChanged = true;
                        }
                    }

                    if (rendererChanged)
                    {
                        renderer.sharedMaterials = materials;
                    }
                }

                Debug.Log($"シーン上のインスタンス '{targetPrefab.name}' のマテリアル参照を更新しました。");
            }
        }

        /// <summary>
        /// プレハブを複製してヒエラルキーに追加し、複製のみマテリアルを差し替える。
        /// 元のプレハブ/インスタンスは一切変更されない。
        /// </summary>
        private void DuplicateAndApplyMaterials(Dictionary<Material, Material> materialMapping)
        {
            if (targetPrefab == null || materialMapping.Count == 0) return;

            // ---- 1. 複製を作成 ----
            GameObject duplicate;
            string prefabAssetPath = AssetDatabase.GetAssetPath(targetPrefab);
            bool isProjectAsset = !string.IsNullOrEmpty(prefabAssetPath) && !targetPrefab.scene.IsValid();

            if (isProjectAsset)
            {
                // プロジェクトウィンドウのプレハブアセット → シーンにインスタンス化
                duplicate = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
            }
            else
            {
                // シーン上のインスタンス → Instantiateで複製
                duplicate = Object.Instantiate(targetPrefab);
                // 元と同じ親・同じ階層に配置
                duplicate.transform.SetParent(targetPrefab.transform.parent, false);
            }

            // 名前を設定
            duplicate.name = targetPrefab.name + "_NataneToon";
            Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate {targetPrefab.name} for NataneToon Migration");

            // 元と並べて比較しやすいようにオフセット配置
            if (isProjectAsset)
            {
                // プレハブアセットからの新規配置はデフォルト位置でOK
            }
            else
            {
                // シーンインスタンスの複製は横に少しずらす
                Vector3 offset = duplicate.transform.right * GetBoundsWidth(duplicate);
                if (offset.magnitude < 0.1f) offset = Vector3.right * 1.0f;
                duplicate.transform.position = targetPrefab.transform.position + offset;
            }

            // ---- 2. 複製のRendererのマテリアルを差し替え ----
            Renderer[] renderers = duplicate.GetComponentsInChildren<Renderer>(true);
            int replacedCount = 0;

            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                bool rendererChanged = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materialMapping.ContainsKey(materials[i]))
                    {
                        materials[i] = materialMapping[materials[i]];
                        rendererChanged = true;
                        replacedCount++;
                    }
                }

                if (rendererChanged)
                {
                    renderer.sharedMaterials = materials;
                }
            }

            // ---- 3. プレハブリンクを解除（独立したオブジェクトにする） ----
            if (PrefabUtility.IsPartOfPrefabInstance(duplicate))
            {
                PrefabUtility.UnpackPrefabInstance(duplicate, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            // ---- 4. 結果を記録 ----
            lastDuplicatedObject = duplicate;
            Selection.activeGameObject = duplicate;

            Debug.Log(L(
                $"'{targetPrefab.name}' の複製 '{duplicate.name}' をヒエラルキーに作成しました（{replacedCount}個のマテリアルスロットを差し替え）。",
                $"Created duplicate '{duplicate.name}' of '{targetPrefab.name}' in hierarchy ({replacedCount} material slots replaced)."));
        }

        /// <summary>
        /// GameObjectのバウンディングボックス幅を取得（複製配置のオフセット計算用）
        /// </summary>
        private float GetBoundsWidth(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return 1.0f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return Mathf.Max(bounds.size.x, 0.5f);
        }

        /// <summary>
        /// 変換レポートをダイアログで表示
        /// </summary>
        private void ShowConversionReport(List<ConversionReport> reports, int successCount)
        {
            StringBuilder reportText = new StringBuilder();
            reportText.AppendLine(L($"変換完了: {successCount}/{reports.Count}個のマテリアルを正常に変換しました。",
                $"Conversion Complete: Successfully converted {successCount}/{reports.Count} materials."));
            reportText.AppendLine();

            int warningCount = 0;

            foreach (var report in reports)
            {
                if (!report.success)
                {
                    reportText.AppendLine($"x {report.materialName}: {L("失敗", "Failed")}");
                    foreach (var warning in report.warnings)
                    {
                        reportText.AppendLine($"   ! {warning}");
                    }
                    reportText.AppendLine();
                    continue;
                }

                bool hasWarnings = report.warnings.Count > 0;
                string statusIcon = hasWarnings ? "!" : "*";
                reportText.AppendLine($"{statusIcon} {report.materialName}");

                // 警告を表示
                if (hasWarnings)
                {
                    foreach (var warning in report.warnings)
                    {
                        reportText.AppendLine($"   ! {warning}");
                        warningCount++;
                    }
                }

                // 重要な情報のみ表示（Outline調整など）
                if (report.outlineWidthAdjusted)
                {
                    reportText.AppendLine(L($"   > Outline Width調整: {report.originalOutlineWidth:F2} → {report.convertedOutlineWidth:F4}",
                        $"   > Outline Width Adjusted: {report.originalOutlineWidth:F2} -> {report.convertedOutlineWidth:F4}"));
                }
                if (report.hasMultipleShadowLayers)
                {
                    reportText.AppendLine(L($"   i 複数シャドウレイヤーを検出（マルチシャドウとして変換）",
                        $"   i Multiple shadow layers detected (converted as multi-shadow)"));
                }

                reportText.AppendLine();
            }

            reportText.AppendLine(L("=== サマリー ===", "=== Summary ==="));
            reportText.AppendLine(L($"成功: {successCount}個", $"Success: {successCount}"));
            reportText.AppendLine(L($"失敗: {reports.Count - successCount}個", $"Failed: {reports.Count - successCount}"));
            reportText.AppendLine(L($"警告: {warningCount}個", $"Warnings: {warningCount}"));

            // ログに詳細を出力
            Debug.Log("=== 詳細な変換レポート Detailed Conversion Report ===");
            foreach (var report in reports)
            {
                if (report.success && report.infos.Count > 0)
                {
                    Debug.Log($"[{report.materialName}]");
                    foreach (var info in report.infos)
                    {
                        Debug.Log($"  {info}");
                    }
                }
            }

            EditorUtility.DisplayDialog(
                L("変換レポート", "Conversion Report"),
                reportText.ToString(),
                "OK"
            );

            // 警告があるマテリアルがある場合は追加の確認ダイアログ
            if (warningCount > 0)
            {
                bool openConsole = EditorUtility.DisplayDialog(
                    L("警告があります", "Warnings Detected"),
                    L($"{warningCount}個の警告が検出されました。\n詳細はコンソールログを確認してください。\n\nコンソールを開きますか？",
                    $"{warningCount} warnings detected.\nCheck the console log for details.\n\nOpen Console?"),
                    L("コンソールを開く", "Open Console"),
                    L("閉じる", "Close")
                );

                if (openConsole)
                {
                    EditorWindow.GetWindow(System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor"));
                }
            }
        }
    }
}
