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
            updatePrefabReferences = EditorGUILayout.Toggle(
                L("参照を自動更新", "Auto-update References"),
                updatePrefabReferences
            );
            if (updatePrefabReferences && !replaceOriginal)
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
            if (GUILayout.Button(L($"選択したマテリアルを一括変換 ({convertCount})", $"Convert Selected ({convertCount})"), GUILayout.Height(40)))
            {
                ConvertPrefabMaterials();
            }
            GUI.enabled = true;
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
            bool has2ndShadow = properties.ContainsKey("_Shadow2ndColor") ||
                               properties.ContainsKey("_lilShadow2ndColor");
            bool has3rdShadow = properties.ContainsKey("_Shadow3rdColor") ||
                               properties.ContainsKey("_lilShadow3rdColor");

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

            // Textures
            CaptureTexture(material, "_MainTex", properties);
            CaptureTexture(material, "_BumpMap", properties);
            CaptureTexture(material, "_EmissionMap", properties);
            CaptureTexture(material, "_MatCapTex", properties);

            // Colors
            CaptureColor(material, "_Color", properties);
            CaptureColor(material, "_ShadowColor", properties);
            CaptureColor(material, "_EmissionColor", properties);

            // Floats
            CaptureFloat(material, "_Cutoff", properties);
            CaptureFloat(material, "_BumpScale", properties);

            // lilToon specific properties
            CaptureTexture(material, "_lilMainTex", properties);
            CaptureColor(material, "_lilColor", properties);
            CaptureColor(material, "_lilShadowColor", properties);
            CaptureFloat(material, "_lilShadowBorder", properties);
            CaptureFloat(material, "_lilShadowBlur", properties);

            // 複数シャドウレイヤーの検出用
            CaptureColor(material, "_Shadow2ndColor", properties);
            CaptureColor(material, "_Shadow3rdColor", properties);
            CaptureColor(material, "_lilShadow2ndColor", properties);
            CaptureColor(material, "_lilShadow3rdColor", properties);

            // Shadow extended
            CaptureFloat(material, "_ShadowStrength", properties);
            CaptureFloat(material, "_ShadowNormalStrength", properties);
            CaptureFloat(material, "_Shadow2ndBorder", properties);
            CaptureFloat(material, "_Shadow3rdBorder", properties);
            CaptureFloat(material, "_lilShadow2ndBorder", properties);
            CaptureFloat(material, "_lilShadow3rdBorder", properties);

            // Rim light
            CaptureColor(material, "_RimColor", properties);
            CaptureFloat(material, "_RimPower", properties);
            CaptureFloat(material, "_RimFresnelPower", properties);

            // Rim extended
            CaptureFloat(material, "_RimMainStrength", properties);
            CaptureFloat(material, "_RimEnableLighting", properties);
            CaptureFloat(material, "_RimBlendMode", properties);

            // Outline
            CaptureFloat(material, "_OutlineWidth", properties);
            CaptureColor(material, "_OutlineColor", properties);

            // Outline extended
            CaptureFloat(material, "_OutlineFixWidth", properties);
            CaptureFloat(material, "_OutlineEnableLighting", properties);

            // Emission
            CaptureTexture(material, "_EmissionMap", properties);
            CaptureColor(material, "_EmissionColor", properties);

            // MatCap properties (拡張)
            CaptureFloat(material, "_MatCapBlend", properties);
            CaptureFloat(material, "_MatCapMainStrength", properties);
            CaptureFloat(material, "_MatCapBlendMode", properties);
            CaptureColor(material, "_MatCapColor", properties);

            // Specular / Surface properties
            CaptureFloat(material, "_Smoothness", properties);
            CaptureFloat(material, "_Metallic", properties);
            CaptureFloat(material, "_Reflectance", properties);
            CaptureFloat(material, "_SpecularBorder", properties);
            CaptureFloat(material, "_SpecularBlur", properties);
            CaptureFloat(material, "_SpecularToon", properties);
            CaptureFloat(material, "_ApplySpecular", properties);

            // Rim Light properties (拡張)
            CaptureFloat(material, "_RimBorder", properties);
            CaptureFloat(material, "_RimBlur", properties);

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
            // Main Texture
            SetTextureIfExists(sourceProps, "_MainTex", targetMaterial, "_MainTex");
            SetTextureIfExists(sourceProps, "_lilMainTex", targetMaterial, "_MainTex");

            // Color
            SetColorIfExists(sourceProps, "_Color", targetMaterial, "_Color");
            SetColorIfExists(sourceProps, "_lilColor", targetMaterial, "_Color");

            // Shadow Color
            if (sourceProps.ContainsKey("_lilShadowColor"))
            {
                Color shadowColor = (Color)sourceProps["_lilShadowColor"];

                // ShadowStrength → ShadowColorのアルファで近似
                if (sourceProps.ContainsKey("_ShadowStrength"))
                {
                    float strength = (float)sourceProps["_ShadowStrength"];
                    shadowColor = Color.Lerp(Color.white, shadowColor, strength);
                    report.infos.Add($"ShadowStrength: {strength:F2} → ShadowColorに適用");
                }

                targetMaterial.SetColor("_ShadowColor", shadowColor);
                targetMaterial.EnableKeyword("_USE_RAMP");
            }
            else if (sourceProps.ContainsKey("_ShadowColor"))
            {
                Color shadowColor = (Color)sourceProps["_ShadowColor"];

                // ShadowStrength → ShadowColorのアルファで近似
                if (sourceProps.ContainsKey("_ShadowStrength"))
                {
                    float strength = (float)sourceProps["_ShadowStrength"];
                    shadowColor = Color.Lerp(Color.white, shadowColor, strength);
                    report.infos.Add($"ShadowStrength: {strength:F2} → ShadowColorに適用");
                }

                targetMaterial.SetColor("_ShadowColor", shadowColor);
            }

            // Shadow Settings - 改善された変換 (Step 4)
            if (sourceProps.ContainsKey("_lilShadowBorder"))
            {
                float border = (float)sourceProps["_lilShadowBorder"];
                float blur = sourceProps.ContainsKey("_lilShadowBlur") ? (float)sourceProps["_lilShadowBlur"] : 0.2f;

                // ShadowSteps: lilToonは基本2トーン（明暗1境界）
                targetMaterial.SetFloat("_ShadowSteps", 2);

                // ShadowOffset: border 0.5 = 中央、0→暗い側、1→明るい側
                float shadowOffset = border - 0.5f;
                targetMaterial.SetFloat("_ShadowOffset", shadowOffset);
                report.infos.Add($"Shadow Border: {border:F3} → Shadow Offset: {shadowOffset:F3}");

                // ShadowBlend: lilToonのblurをNataneのblendに変換
                // blur=0 → 鋭い境界(blend=0), blur=1 → 柔らかい(blend=0.8)
                float blend = Mathf.Clamp01(blur * 0.8f);
                targetMaterial.SetFloat("_ShadowBlend", blend);

                // ShadowSharpness: blurの逆数系
                // blur小 → sharpness大 (鋭い境界)
                float sharpness = Mathf.Lerp(0.3f, 0.02f, blur);
                targetMaterial.SetFloat("_ShadowSharpness", sharpness);

                // StepBorderSmooth: blurに比例
                targetMaterial.SetFloat("_StepBorderSmooth", blur * 0.3f);

                report.infos.Add($"Shadow Blur: {blur:F3} → Blend: {blend:F3}, Sharpness: {sharpness:F3}, StepBorderSmooth: {blur * 0.3f:F3}");

                // 極端な値の警告
                if (blur < 0.05f)
                {
                    report.warnings.Add("Shadow Blurが非常に小さい値です。エッジが鋭すぎる可能性があります。");
                }
                else if (blur > 0.8f)
                {
                    report.warnings.Add("Shadow Blurが非常に大きい値です。シャドウが不明瞭になる可能性があります。");
                }
            }
            else
            {
                // Default settings for good toon shading
                targetMaterial.SetFloat("_ShadowSteps", 2);
                targetMaterial.SetFloat("_ShadowSharpness", 0.1f);
            }

            // マルチシャドウレイヤー変換 (Step 5)
            MapMultiShadowLayers(sourceProps, targetMaterial, report);

            // Normal Map
            SetTextureIfExists(sourceProps, "_BumpMap", targetMaterial, "_BumpMap");
            SetFloatIfExists(sourceProps, "_BumpScale", targetMaterial, "_BumpScale");

            if (sourceProps.ContainsKey("_BumpMap") && sourceProps["_BumpMap"] != null)
            {
                targetMaterial.SetFloat("_UseNormalMap", 1.0f);
                targetMaterial.EnableKeyword("_NORMALMAP");
            }

            // Rim Light - 改善された変換 (Step 6)
            bool hasRim = false;
            float rimIntensity = 1.0f;

            if (sourceProps.ContainsKey("_RimColor"))
            {
                Color rimColor = (Color)sourceProps["_RimColor"];
                if (rimColor.a > 0 || rimColor.maxColorComponent > 0)
                {
                    targetMaterial.SetColor("_RimColor", rimColor);
                    hasRim = true;

                    // Rim Intensityのデフォルト値を設定
                    rimIntensity = Mathf.Max(rimColor.maxColorComponent, 0.5f);
                }
            }

            // RimMainStrength → RimIntensity（最重要！）
            if (sourceProps.ContainsKey("_RimMainStrength"))
            {
                float strength = (float)sourceProps["_RimMainStrength"];
                rimIntensity = strength;
                hasRim = hasRim || strength > 0.01f;
                report.infos.Add($"RimMainStrength: {strength:F2} → RimIntensity");
            }

            if (sourceProps.ContainsKey("_RimFresnelPower"))
            {
                float power = (float)sourceProps["_RimFresnelPower"];
                targetMaterial.SetFloat("_RimPower", Mathf.Clamp(power, 0.1f, 10f));
                hasRim = true;
                report.infos.Add($"Rim Fresnel Power: {power:F2} → Rim Power: {Mathf.Clamp(power, 0.1f, 10f):F2}");
            }
            else if (sourceProps.ContainsKey("_RimPower"))
            {
                SetFloatIfExists(sourceProps, "_RimPower", targetMaterial, "_RimPower");
                hasRim = true;
            }

            if (hasRim)
            {
                targetMaterial.SetFloat("_RimLight", 1.0f);
                targetMaterial.EnableKeyword("_RIM_LIGHT");

                // Rim Intensity設定
                if (targetMaterial.HasProperty("_RimIntensity"))
                {
                    targetMaterial.SetFloat("_RimIntensity", rimIntensity);
                }
                report.infos.Add($"Rim Light有効化。Intensity: {rimIntensity:F2}");

                // Rim Spread設定（_RimBorderから計算）
                if (sourceProps.ContainsKey("_RimBorder"))
                {
                    float border = (float)sourceProps["_RimBorder"];
                    // borderが小さい→広がりが大きい（逆相関）
                    float spread = Mathf.Lerp(3.0f, 0.5f, border);
                    targetMaterial.SetFloat("_RimSpread", spread);
                    report.infos.Add($"Rim Border: {border:F2} → Rim Spread: {spread:F2}");
                }
                else
                {
                    targetMaterial.SetFloat("_RimSpread", 2.0f);
                }

                // Rim Blur処理
                if (sourceProps.ContainsKey("_RimBlur"))
                {
                    float blur = (float)sourceProps["_RimBlur"];
                    if (blur > 0.01f)
                    {
                        float currentPower = targetMaterial.GetFloat("_RimPower");
                        // blurの影響を50%に（旧30%から改善）
                        float adjustedPower = currentPower * (1.0f - blur * 0.5f);
                        targetMaterial.SetFloat("_RimPower", Mathf.Max(adjustedPower, 0.1f));
                        report.infos.Add($"Rim Blur: {blur:F2} → Rim Power調整: {adjustedPower:F2}");
                    }
                }

                // RimEnableLighting → RimDirStrength
                if (sourceProps.ContainsKey("_RimEnableLighting"))
                {
                    float enableLighting = (float)sourceProps["_RimEnableLighting"];
                    targetMaterial.SetFloat("_RimDirStrength", enableLighting);
                    report.infos.Add($"RimEnableLighting: {enableLighting:F2} → RimDirStrength");
                }

                // RimBlendMode マッピング
                if (sourceProps.ContainsKey("_RimBlendMode"))
                {
                    int lilMode = (int)(float)sourceProps["_RimBlendMode"];
                    int nataneMode = ConvertRimBlendMode(lilMode);
                    targetMaterial.SetFloat("_RimBlendMode", nataneMode);
                    report.infos.Add($"Rim Blend Mode: {lilMode} → {nataneMode} ({GetRimBlendModeName(nataneMode)})");
                }

                // RimShadowMask: 影部分ではリムを少し抑える
                targetMaterial.SetFloat("_RimShadowMask", 0.3f);
            }

            // Outline - 変換後の確認ダイアログ
            bool hasOutline = false;
            if (sourceProps.ContainsKey("_OutlineWidth"))
            {
                float originalWidth = (float)sourceProps["_OutlineWidth"];
                if (originalWidth > 0)
                {
                    // lilToonのアウトライン幅をNatane Toon Shaderの単位に変換
                    float convertedWidth = Mathf.Clamp(originalWidth * 0.01f, 0, 0.1f);
                    targetMaterial.SetFloat("_OutlineWidth", convertedWidth);
                    hasOutline = true;

                    // 変換レポートに記録
                    report.outlineWidthAdjusted = true;
                    report.originalOutlineWidth = originalWidth;
                    report.convertedOutlineWidth = convertedWidth;
                    report.infos.Add($"Outline Width: {originalWidth:F3} → {convertedWidth:F4} (スケール調整済み)");

                    // 極端な値の警告
                    if (originalWidth > 10f)
                    {
                        report.warnings.Add($"Outline Widthが大きすぎる可能性があります（元の値: {originalWidth:F2}）。変換後の見た目を確認してください。");
                    }
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

                // OutlineFixWidth情報をレポートに記録
                if (sourceProps.ContainsKey("_OutlineFixWidth"))
                {
                    float fixWidth = (float)sourceProps["_OutlineFixWidth"];
                    if (fixWidth > 0.5f)
                    {
                        report.infos.Add("Outline FixWidth: 有効（距離に依存しない固定幅）");
                    }
                }
            }

            // Emission
            SetTextureIfExists(sourceProps, "_EmissionMap", targetMaterial, "_EmissionMap");

            bool hasEmission = false;
            if (sourceProps.ContainsKey("_EmissionColor"))
            {
                Color emissionColor = (Color)sourceProps["_EmissionColor"];
                if (emissionColor.maxColorComponent > 0)
                {
                    targetMaterial.SetColor("_EmissionColor", emissionColor);
                    hasEmission = true;
                }
            }

            if (hasEmission || (sourceProps.ContainsKey("_EmissionMap") && sourceProps["_EmissionMap"] != null))
            {
                targetMaterial.SetFloat("_Emission", 1.0f);
                targetMaterial.EnableKeyword("_EMISSION");
            }

            // MatCap - 改善された変換処理
            SetTextureIfExists(sourceProps, "_MatCapTex", targetMaterial, "_MatCapTex");
            if (sourceProps.ContainsKey("_MatCapTex") && sourceProps["_MatCapTex"] != null)
            {
                targetMaterial.SetFloat("_MatCap", 1.0f);
                targetMaterial.EnableKeyword("_MATCAP");

                // MatCap Intensity変換
                if (sourceProps.ContainsKey("_MatCapBlend"))
                {
                    float matCapBlend = (float)sourceProps["_MatCapBlend"];
                    targetMaterial.SetFloat("_MatCapIntensity", matCapBlend);
                    report.infos.Add($"MatCap Intensity: {matCapBlend:F2}");
                }
                else if (sourceProps.ContainsKey("_MatCapMainStrength"))
                {
                    float strength = (float)sourceProps["_MatCapMainStrength"];
                    targetMaterial.SetFloat("_MatCapIntensity", strength);
                    report.infos.Add($"MatCap Intensity: {strength:F2} (from MainStrength)");
                }
                else
                {
                    targetMaterial.SetFloat("_MatCapIntensity", 0.8f);
                    report.infos.Add("MatCap Intensity: 0.8 (デフォルト値)");
                }

                // MatCap Blend Mode変換
                if (sourceProps.ContainsKey("_MatCapBlendMode"))
                {
                    int lilBlendMode = (int)(float)sourceProps["_MatCapBlendMode"];
                    int nataneBlendMode = ConvertMatCapBlendMode(lilBlendMode);
                    targetMaterial.SetFloat("_MatCapBlendMode", nataneBlendMode);
                    report.infos.Add($"MatCap Blend Mode: {lilBlendMode} → {nataneBlendMode} ({GetMatCapBlendModeName(nataneBlendMode)})");
                }
                else
                {
                    targetMaterial.SetFloat("_MatCapBlendMode", 0);
                }

                // MatCapColor情報をレポートに記録
                if (sourceProps.ContainsKey("_MatCapColor"))
                {
                    Color matCapColor = (Color)sourceProps["_MatCapColor"];
                    if (matCapColor != Color.white)
                    {
                        report.infos.Add($"MatCapColor: {matCapColor}（テクスチャに事前乗算されていない場合は手動調整が必要）");
                    }
                }
            }

            // Specular - 改善された変換 (Step 7)
            bool hasSpecular = false;
            float specularIntensity = 0f;

            // lilToon SpecularToon=1の場合、NataneのAnime-style smoothstepに近い
            if (sourceProps.ContainsKey("_SpecularToon"))
            {
                float specToon = (float)sourceProps["_SpecularToon"];
                if (specToon > 0.5f && sourceProps.ContainsKey("_SpecularBorder"))
                {
                    float specBorder = (float)sourceProps["_SpecularBorder"];
                    // lilToon Toon Specular: smoothstep(border, border+blur, ndoth)
                    // Natane: smoothstep(1-size-softness, 1-size+softness, ndoth)
                    // → size = 1 - border
                    float size = Mathf.Clamp01(1.0f - specBorder);
                    targetMaterial.SetFloat("_SpecularSize", size);
                    hasSpecular = true;
                    specularIntensity = 1.0f;
                    report.infos.Add($"SpecularToon: border={specBorder:F2} → SpecularSize: {size:F3}");
                }
            }

            if (sourceProps.ContainsKey("_Smoothness"))
            {
                float smoothness = (float)sourceProps["_Smoothness"];
                if (smoothness > 0.01f)
                {
                    if (!hasSpecular) // SpecularToonで既に設定済みでない場合
                    {
                        float specularSize = OptimizeSpecularSize(smoothness);
                        targetMaterial.SetFloat("_SpecularSize", specularSize);
                        report.infos.Add($"Smoothness: {smoothness:F2} → Specular Size: {specularSize:F3}");
                    }
                    hasSpecular = true;
                    specularIntensity = Mathf.Max(specularIntensity, smoothness);
                }
            }

            if (sourceProps.ContainsKey("_Metallic"))
            {
                float metallic = (float)sourceProps["_Metallic"];
                if (metallic > 0.01f)
                {
                    specularIntensity = Mathf.Max(specularIntensity, metallic);
                    hasSpecular = true;
                    report.infos.Add($"Metallic: {metallic:F2} (スペキュラー強度に反映)");
                }
            }

            if (sourceProps.ContainsKey("_Reflectance"))
            {
                float reflectance = (float)sourceProps["_Reflectance"];
                if (reflectance > 0.01f)
                {
                    specularIntensity = Mathf.Max(specularIntensity, reflectance * 0.8f);
                    hasSpecular = true;
                    report.infos.Add($"Reflectance: {reflectance:F2} (スペキュラー強度に反映)");
                }
            }

            if (sourceProps.ContainsKey("_SpecularBlur"))
            {
                float specBlur = (float)sourceProps["_SpecularBlur"];
                float softness = Mathf.Clamp01(specBlur);
                targetMaterial.SetFloat("_SpecularSoftness", softness);
                hasSpecular = true;
                report.infos.Add($"Specular Blur: {specBlur:F2} → Specular Softness: {softness:F2}");
            }

            if (hasSpecular)
            {
                targetMaterial.SetFloat("_Specular", 1.0f);
                targetMaterial.EnableKeyword("_SPECULAR");

                if (!sourceProps.ContainsKey("_SpecularColor"))
                {
                    targetMaterial.SetColor("_SpecularColor", new Color(1, 1, 1, 1));
                }
                if (!sourceProps.ContainsKey("_SpecularBlur"))
                {
                    targetMaterial.SetFloat("_SpecularSoftness", 0.3f);
                }

                report.infos.Add($"Specular有効化 (強度: {specularIntensity:F2})");
            }

            // Surface Properties - Glossiness/Matte変換
            if (sourceProps.ContainsKey("_Smoothness"))
            {
                float smoothness = (float)sourceProps["_Smoothness"];
                targetMaterial.SetFloat("_Glossiness", smoothness);

                float matteEffect = 1.0f - smoothness;
                targetMaterial.SetFloat("_MatteEffect", matteEffect);

                report.infos.Add($"Surface Finish: Glossiness={smoothness:F2}, Matte={matteEffect:F2}");
            }

            // Alpha Cutoff (Step 8) - Cutoutバリアントで重要
            if (sourceProps.ContainsKey("_Cutoff"))
            {
                float cutoff = (float)sourceProps["_Cutoff"];
                targetMaterial.SetFloat("_Cutoff", cutoff);
                report.infos.Add($"Alpha Cutoff: {cutoff:F2}");
            }
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
        /// Shadow Blurから最適なShadow Sharpnessへ変換（非線形カーブ）
        /// lilToonのblurは0-1の範囲で、Natane Toon Shaderのsharpnessは0.001-0.5の範囲
        /// 自然な見た目のために指数カーブを使用
        /// </summary>
        private float OptimizeShadowSharpness(float blur)
        {
            // blur: 0 (sharp) → 1 (blur)
            // sharpness: 0.5 (sharp) → 0.001 (blur)

            // 非線形変換で自然な見た目に
            float normalized = 1.0f - blur; // 反転（blurが大きい→sharpnessが小さい）
            float curved = Mathf.Pow(normalized, 2.0f); // 二次曲線で中間値をより鋭く
            float sharpness = Mathf.Lerp(0.001f, 0.5f, curved);

            return sharpness;
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
        /// </summary>
        private int ConvertMatCapBlendMode(int lilBlendMode)
        {
            // lilToon MatCap Blend Modes (推定):
            // 0 = Add
            // 1 = Multiply
            // 2 = Screen
            // 3 = Overlay
            //
            // Natane Toon Shader MatCap Blend Modes:
            // 0 = Add
            // 1 = Multiply
            // 2 = Screen
            // 3 = Overlay

            // 多くの場合、同じ順序なのでそのまま返す
            // ただし、範囲チェックは必要
            return Mathf.Clamp(lilBlendMode, 0, 3);
        }

        /// <summary>
        /// MatCap Blend Modeの名前を取得（レポート用）
        /// </summary>
        private string GetMatCapBlendModeName(int blendMode)
        {
            switch (blendMode)
            {
                case 0: return "Add";
                case 1: return "Multiply";
                case 2: return "Screen";
                case 3: return "Overlay";
                default: return "Unknown";
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
        /// </summary>
        private void MapMultiShadowLayers(Dictionary<string, object> sourceProps, Material targetMaterial, ConversionReport report)
        {
            bool has2nd = TryGetColor(sourceProps, "_Shadow2ndColor", "_lilShadow2ndColor", out Color shadow2nd);
            bool has3rd = TryGetColor(sourceProps, "_Shadow3rdColor", "_lilShadow3rdColor", out Color shadow3rd);

            if (!has2nd && !has3rd) return;

            // Enable multi-shadow keyword
            targetMaterial.EnableKeyword("_USE_MULTI_SHADOW");

            if (has2nd)
            {
                targetMaterial.SetColor("_Shadow2ndColor", shadow2nd);
                float border2nd = TryGetFloat(sourceProps, "_Shadow2ndBorder", "_lilShadow2ndBorder", 0.5f);
                targetMaterial.SetFloat("_Shadow2ndBorder", border2nd);
                report.infos.Add($"2nd Shadow: Color={shadow2nd}, Border={border2nd:F2}");
            }

            if (has3rd)
            {
                targetMaterial.SetColor("_Shadow3rdColor", shadow3rd);
                float border3rd = TryGetFloat(sourceProps, "_Shadow3rdBorder", "_lilShadow3rdBorder", 0.3f);
                targetMaterial.SetFloat("_Shadow3rdBorder", border3rd);
                report.infos.Add($"3rd Shadow: Color={shadow3rd}, Border={border3rd:F2}");
            }
        }

        /// <summary>
        /// lilToonのRimBlendModeをNatane Toon ShaderのRimBlendModeに変換
        /// lilToon: 0=Add, 1=Screen, 2=Multiply
        /// Natane:  0=Add, 1=Multiply, 2=Screen, 3=Overlay
        /// </summary>
        private int ConvertRimBlendMode(int lilMode)
        {
            switch (lilMode)
            {
                case 0: return 0; // Add → Add
                case 1: return 2; // Screen → Screen
                case 2: return 1; // Multiply → Multiply
                default: return 0; // Default to Add
            }
        }

        /// <summary>
        /// Rim Blend Modeの名前を取得（レポート用）
        /// </summary>
        private string GetRimBlendModeName(int blendMode)
        {
            switch (blendMode)
            {
                case 0: return "Add";
                case 1: return "Multiply";
                case 2: return "Screen";
                case 3: return "Overlay";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// 2つのプロパティ名候補からColorを取得（lilプレフィックス/非プレフィックス両対応）
        /// </summary>
        private bool TryGetColor(Dictionary<string, object> props, string key1, string key2, out Color color)
        {
            if (props.ContainsKey(key1) && props[key1] is Color c1)
            {
                color = c1;
                return true;
            }
            if (props.ContainsKey(key2) && props[key2] is Color c2)
            {
                color = c2;
                return true;
            }
            color = Color.white;
            return false;
        }

        /// <summary>
        /// 2つのプロパティ名候補からfloatを取得（lilプレフィックス/非プレフィックス両対応）
        /// </summary>
        private float TryGetFloat(Dictionary<string, object> props, string key1, string key2, float defaultValue)
        {
            if (props.ContainsKey(key1))
            {
                return (float)props[key1];
            }
            if (props.ContainsKey(key2))
            {
                return (float)props[key2];
            }
            return defaultValue;
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

            if (!EditorUtility.DisplayDialog(
                L("プレハブマテリアル変換", "Convert Prefab Materials"),
                L($"'{targetPrefab.name}' 内の {materialsToConvert.Count} 個のマテリアルを変換してもよろしいですか？",
                $"Are you sure you want to convert {materialsToConvert.Count} materials in '{targetPrefab.name}'?"),
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

                    // materialMappingを構築
                    if (!replaceOriginal)
                    {
                        // 新しいマテリアルのパスを取得
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

            // プレハブ参照を更新
            if (updatePrefabReferences && !replaceOriginal && materialMapping.Count > 0)
            {
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
                // プレハブ参照の更新
                if (updatePrefabReferences && !replaceOriginal)
                {
                    string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                    string newPath = sourcePath.Replace(".mat", "_NataneToon.mat");
                    Material newMat = AssetDatabase.LoadAssetAtPath<Material>(newPath);

                    if (newMat != null)
                    {
                        var mapping = new Dictionary<Material, Material> { { sourceMaterial, newMat } };
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
