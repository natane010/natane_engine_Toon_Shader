using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NataneToon.Editor
{
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
            var window = GetWindow<LilToonMigrationTool>("lilToon移行ツール lilToon Migration");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            ScanForLilToonMaterials();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("lilToon to Natane Toon Shader Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("lilToon から Natane Toon Shader への移行ツール", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "このツールはlilToonマテリアルを自動的にNatane Toon Shaderに変換します。\n" +
                "プロパティをできるだけ近い形でマッピングし、テクスチャ参照を保持します。\n" +
                "変換後は自動調整機能により最適な設定が適用されます。\n\n" +
                "This tool automatically converts lilToon materials to Natane Toon Shader.\n" +
                "It will map properties as closely as possible and preserve texture references.\n" +
                "Auto-adjustment features will apply optimal settings after conversion.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Options
            EditorGUILayout.LabelField("オプション Options", EditorStyles.boldLabel);
            createBackup = EditorGUILayout.Toggle("バックアップを作成 Create Backup", createBackup);
            replaceOriginal = EditorGUILayout.Toggle("元を置換（破壊的） Replace Original (Destructive)", replaceOriginal);
            showPreview = EditorGUILayout.Toggle("変換後プレビュー表示 Show Preview After Conversion", showPreview);

            if (replaceOriginal)
            {
                EditorGUILayout.HelpBox(
                    "警告: この操作は元のマテリアルを永久に変更します！\n" +
                    "プロジェクトのバックアップがあることを確認してください。\n\n" +
                    "WARNING: This will permanently modify your original materials! " +
                    "Make sure you have a backup of your project.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space();

            // Scan button
            if (GUILayout.Button("lilToonマテリアルをスキャン Scan for lilToon Materials", GUILayout.Height(30)))
            {
                ScanForLilToonMaterials();
            }

            EditorGUILayout.Space();

            // Materials list
            EditorGUILayout.LabelField($"見つかったマテリアル Found {lilToonMaterials.Count} lilToon Materials", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var material in lilToonMaterials)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(material, typeof(Material), false);

                if (GUILayout.Button("変換 Convert", GUILayout.Width(80)))
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
            if (GUILayout.Button("すべて変換 Convert All Materials", GUILayout.Height(40)))
            {
                ConvertAllMaterials();
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
                "すべて変換 Convert All Materials",
                $"{lilToonMaterials.Count}個のマテリアルを変換してもよろしいですか？\nAre you sure you want to convert {lilToonMaterials.Count} materials?",
                "はい Yes", "キャンセル Cancel"))
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

                // Find Natane Toon Shader
                Shader nataneToonShader = Shader.Find("Natane/Toon Shader");
                if (nataneToonShader == null)
                {
                    Debug.LogError("Natane Toon Shader not found! Please make sure it's in your project.");
                    report.success = false;
                    report.warnings.Add("Natane Toon Shaderが見つかりませんでした。");
                    return report;
                }

                // Store original properties before changing shader
                var originalProperties = CaptureProperties(sourceMaterial);

                // 複数シャドウレイヤーの検出
                DetectMultipleShadowLayers(originalProperties, report);

                // Change shader
                targetMaterial.shader = nataneToonShader;

                // Map properties
                MapPropertiesWithReport(originalProperties, targetMaterial, report);

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
                report.warnings.Add($"複数のシャドウレイヤー（{layerInfo}）が検出されました。Natane Toon Shaderでは最初のシャドウレイヤーのみが変換されます。");
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

            // Rim light
            CaptureColor(material, "_RimColor", properties);
            CaptureFloat(material, "_RimPower", properties);
            CaptureFloat(material, "_RimFresnelPower", properties);

            // Outline
            CaptureFloat(material, "_OutlineWidth", properties);
            CaptureColor(material, "_OutlineColor", properties);

            // Emission
            CaptureTexture(material, "_EmissionMap", properties);
            CaptureColor(material, "_EmissionColor", properties);

            // MatCap properties (拡張)
            CaptureFloat(material, "_MatCapBlend", properties);
            CaptureFloat(material, "_MatCapMainStrength", properties);
            CaptureFloat(material, "_MatCapBlendMode", properties);

            // Specular / Surface properties
            CaptureFloat(material, "_Smoothness", properties);
            CaptureFloat(material, "_Metallic", properties);
            CaptureFloat(material, "_Reflectance", properties);
            CaptureFloat(material, "_SpecularBorder", properties);
            CaptureFloat(material, "_SpecularBlur", properties);

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
                targetMaterial.SetColor("_ShadowColor", (Color)sourceProps["_lilShadowColor"]);
                targetMaterial.EnableKeyword("_USE_RAMP");
            }
            else
            {
                SetColorIfExists(sourceProps, "_ShadowColor", targetMaterial, "_ShadowColor");
            }

            // Shadow Settings - 改善された自動最適化
            if (sourceProps.ContainsKey("_lilShadowBorder"))
            {
                float border = (float)sourceProps["_lilShadowBorder"];
                targetMaterial.SetFloat("_ShadowOffset", Mathf.Lerp(-0.5f, 0.5f, border));
                report.infos.Add($"Shadow Border: {border:F3} → Shadow Offset: {Mathf.Lerp(-0.5f, 0.5f, border):F3}");
            }

            if (sourceProps.ContainsKey("_lilShadowBlur"))
            {
                float blur = (float)sourceProps["_lilShadowBlur"];
                // 改善されたShadow Sharpness変換（非線形カーブで自然な見た目に）
                float sharpness = OptimizeShadowSharpness(blur);
                targetMaterial.SetFloat("_ShadowSharpness", sharpness);
                report.infos.Add($"Shadow Blur: {blur:F3} → Shadow Sharpness: {sharpness:F3} (最適化済み)");

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

            // Normal Map
            SetTextureIfExists(sourceProps, "_BumpMap", targetMaterial, "_BumpMap");
            SetFloatIfExists(sourceProps, "_BumpScale", targetMaterial, "_BumpScale");

            if (sourceProps.ContainsKey("_BumpMap") && sourceProps["_BumpMap"] != null)
            {
                targetMaterial.SetFloat("_UseNormalMap", 1.0f);
                targetMaterial.EnableKeyword("_NORMALMAP");
            }

            // Rim Light - デフォルト値設定とIntensity調整
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
                    report.infos.Add($"Rim Light有効化。Intensity: {rimIntensity:F2} (自動設定)");
                }
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

                // Rim Intensityが未設定の場合はデフォルト値を設定
                if (targetMaterial.HasProperty("_RimIntensity"))
                {
                    targetMaterial.SetFloat("_RimIntensity", rimIntensity);
                }

                // Rim Spread設定（_RimBorderから計算）
                if (sourceProps.ContainsKey("_RimBorder"))
                {
                    float border = (float)sourceProps["_RimBorder"];
                    // borderが小さい→広がりが大きい（逆相関）
                    // border: 0 → spread: 3.0 (広い), border: 1 → spread: 0.5 (狭い)
                    float spread = Mathf.Lerp(3.0f, 0.5f, border);
                    targetMaterial.SetFloat("_RimSpread", spread);
                    report.infos.Add($"Rim Border: {border:F2} → Rim Spread: {spread:F2}");
                }
                else
                {
                    // デフォルト値
                    targetMaterial.SetFloat("_RimSpread", 2.0f);
                }

                // Rim Blur処理（_RimBlurがある場合）
                if (sourceProps.ContainsKey("_RimBlur"))
                {
                    float blur = (float)sourceProps["_RimBlur"];
                    // lilToonのRimBlurは通常0-1の範囲
                    // Nataneでは_RimPowerでエッジの鋭さを制御
                    // blurが大きい→powerを小さくして柔らかく
                    if (blur > 0.01f)
                    {
                        float currentPower = targetMaterial.GetFloat("_RimPower");
                        float adjustedPower = currentPower * (1.0f - blur * 0.3f); // blurの影響を30%に制限
                        targetMaterial.SetFloat("_RimPower", Mathf.Max(adjustedPower, 0.1f));
                        report.infos.Add($"Rim Blur: {blur:F2} → Rim Power調整: {adjustedPower:F2}");
                    }
                }
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
            bool hasMatCap = false;

            if (sourceProps.ContainsKey("_MatCapTex") && sourceProps["_MatCapTex"] != null)
            {
                hasMatCap = true;
                targetMaterial.SetFloat("_MatCap", 1.0f);
                targetMaterial.EnableKeyword("_MATCAP");

                // MatCap Intensity変換
                if (sourceProps.ContainsKey("_MatCapBlend"))
                {
                    float blend = (float)sourceProps["_MatCapBlend"];
                    targetMaterial.SetFloat("_MatCapIntensity", blend);
                    report.infos.Add($"MatCap Intensity: {blend:F2}");
                }
                else if (sourceProps.ContainsKey("_MatCapMainStrength"))
                {
                    float strength = (float)sourceProps["_MatCapMainStrength"];
                    targetMaterial.SetFloat("_MatCapIntensity", strength);
                    report.infos.Add($"MatCap Intensity: {strength:F2} (from MainStrength)");
                }
                else
                {
                    // デフォルト値: lilToonのデフォルトは通常1.0なので、やや控えめに設定
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
                    // デフォルト値: Add (0)
                    targetMaterial.SetFloat("_MatCapBlendMode", 0);
                }
            }

            // Specular - 新規追加
            bool hasSpecular = false;
            float specularIntensity = 0f;

            if (sourceProps.ContainsKey("_Smoothness"))
            {
                float smoothness = (float)sourceProps["_Smoothness"];
                if (smoothness > 0.01f)
                {
                    // SmoothnessをSpecularSizeに変換（非線形カーブ）
                    float specularSize = OptimizeSpecularSize(smoothness);
                    targetMaterial.SetFloat("_SpecularSize", specularSize);
                    hasSpecular = true;
                    specularIntensity = smoothness; // Intensityの計算に使用
                    report.infos.Add($"Smoothness: {smoothness:F2} → Specular Size: {specularSize:F3}");
                }
            }

            if (sourceProps.ContainsKey("_Metallic"))
            {
                float metallic = (float)sourceProps["_Metallic"];
                if (metallic > 0.01f)
                {
                    // Metallicは通常スペキュラーの強度に影響する
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
                    // Reflectanceもスペキュラー強度に影響
                    specularIntensity = Mathf.Max(specularIntensity, reflectance * 0.8f);
                    hasSpecular = true;
                    report.infos.Add($"Reflectance: {reflectance:F2} (スペキュラー強度に反映)");
                }
            }

            if (sourceProps.ContainsKey("_SpecularBlur"))
            {
                float blur = (float)sourceProps["_SpecularBlur"];
                // SpecularBlurをSpecularSoftnessに変換
                float softness = Mathf.Clamp01(blur);
                targetMaterial.SetFloat("_SpecularSoftness", softness);
                hasSpecular = true;
                report.infos.Add($"Specular Blur: {blur:F2} → Specular Softness: {softness:F2}");
            }

            if (hasSpecular)
            {
                targetMaterial.SetFloat("_Specular", 1.0f);
                targetMaterial.EnableKeyword("_SPECULAR");

                // Specular Colorのデフォルト値
                if (!sourceProps.ContainsKey("_SpecularColor"))
                {
                    targetMaterial.SetColor("_SpecularColor", new Color(1, 1, 1, 1));
                }

                // Specular Softnessのデフォルト値
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

                // 逆のマット効果も設定
                float matteEffect = 1.0f - smoothness;
                targetMaterial.SetFloat("_MatteEffect", matteEffect);

                report.infos.Add($"Surface Finish: Glossiness={smoothness:F2}, Matte={matteEffect:F2}");
            }

            // Default settings for good toon shading
            if (!sourceProps.ContainsKey("_lilShadowBorder"))
            {
                targetMaterial.SetFloat("_ShadowSteps", 2);
                targetMaterial.SetFloat("_ShadowSharpness", 0.1f);
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
        /// 変換レポートをダイアログで表示
        /// </summary>
        private void ShowConversionReport(List<ConversionReport> reports, int successCount)
        {
            StringBuilder reportText = new StringBuilder();
            reportText.AppendLine($"変換完了: {successCount}/{reports.Count}個のマテリアルを正常に変換しました。");
            reportText.AppendLine($"Conversion Complete: Successfully converted {successCount}/{reports.Count} materials.");
            reportText.AppendLine();

            int warningCount = 0;
            int infoCount = 0;

            foreach (var report in reports)
            {
                if (!report.success)
                {
                    reportText.AppendLine($"x {report.materialName}: 失敗");
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
                    reportText.AppendLine($"   > Outline Width調整: {report.originalOutlineWidth:F2} → {report.convertedOutlineWidth:F4}");
                }
                if (report.hasMultipleShadowLayers)
                {
                    reportText.AppendLine($"   i 複数シャドウレイヤーを検出（最初のレイヤーのみ変換）");
                }

                reportText.AppendLine();
            }

            reportText.AppendLine("=== サマリー Summary ===");
            reportText.AppendLine($"成功: {successCount}個");
            reportText.AppendLine($"失敗: {reports.Count - successCount}個");
            reportText.AppendLine($"警告: {warningCount}個");

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
                "変換レポート Conversion Report",
                reportText.ToString(),
                "OK"
            );

            // 警告があるマテリアルがある場合は追加の確認ダイアログ
            if (warningCount > 0)
            {
                bool openConsole = EditorUtility.DisplayDialog(
                    "警告があります Warnings Detected",
                    $"{warningCount}個の警告が検出されました。\n詳細はコンソールログを確認してください。\n\nコンソールを開きますか？\n\n{warningCount} warnings detected.\nCheck the console log for details.\n\nOpen Console?",
                    "コンソールを開く Open Console",
                    "閉じる Close"
                );

                if (openConsole)
                {
                    EditorWindow.GetWindow(System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor"));
                }
            }
        }
    }
}
