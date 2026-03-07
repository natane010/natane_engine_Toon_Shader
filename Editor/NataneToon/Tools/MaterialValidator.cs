using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Material validation and optimization checker
    /// マテリアル検証と最適化チェッカー
    /// Provides VRChat optimization checks, performance ratings, and auto-fix suggestions
    /// VRChat最適化チェック、パフォーマンス評価、自動修正提案を提供
    /// </summary>
    public class MaterialValidator : EditorWindow
    {
        private const string NataneShaderPrefix = "Natane/Toon Shader";

        private static readonly string[] VrchatTextureProperties =
        {
            "_MainTex", "_BumpMap", "_EmissionMap", "_MatCapTex", "_MatCapTex2", "_MatCapTex3", "_RampTex",
            "_DissolveTex", "_DissolveMap", "_ThicknessMap", "_SpecularMask", "_RimMask",
            "_SSSMask", "_MatCapMask", "_EmissionMask", "_DissolveMask",
            "_ReflectionMask", "_EnvRimMask", "_ParallaxMap", "_RefractionMask"
        };

        private static readonly string[] TextureSizeProperties =
        {
            "_MainTex", "_BumpMap", "_EmissionMap", "_MatCapTex", "_MatCapTex2", "_MatCapTex3", "_RampTex", "_ParallaxMap"
        };

        private static readonly string[] ActiveFeatureKeywords =
        {
            "_SPECULAR", "_RIM_LIGHT", "_RIM_LIGHT_2", "_OFFSET_RIM_LIGHT", "_SSS", "_MATCAP", "_OUTLINE", "_EMISSION",
            "_DISSOLVE", "_HUE_SHIFT", "_NORMALMAP", "_REFLECTION", "_ENV_RIM", "_PARALLAX", "_REFRACTION",
            "_IRIDESCENCE", "_GLITTER", "_MATCAP_2", "_MATCAP_3", "_AUDIOLINK", "_HOLOGRAM", "_GLITCH",
            "_HOLOGRAM_NOISE", "_DECAL", "_VAT", "_VERTEX_ANIMATION", "_PIXEL_VERTEX_LIGHTS", "_DETAIL_MAP",
            "_TRIPLANAR", "_HEIGHT_FOG", "_SURFACE_COVER", "_MIRROR_CONTROL", "_QUEST_LITE", "_WATER_DRIP",
            "_VIDEO_TEXTURE", "_INTERSECTION_FADE", "_SCREEN_TONE", "_SCREEN_EDGE", "_HATCHING", "_USE_LIGHT_VOLUME",
            "_LTCGI", "_HAIR_SPECULAR", "_WATERCOLOR", "_SMEAR", "_BACKFACE_TEXTURE", "_FUR"
        };

        private static readonly Dictionary<string, string[]> UnusedFeatureTextureRequirements = new Dictionary<string, string[]>
        {
            { "_MATCAP", new[] { "_MatCapTex" } },
            { "_MATCAP_2", new[] { "_MatCapTex2" } },
            { "_MATCAP_3", new[] { "_MatCapTex3" } },
            { "_NORMALMAP", new[] { "_BumpMap" } },
            { "_EMISSION", new[] { "_EmissionMap" } },
            { "_DISSOLVE", new[] { "_DissolveTex", "_DissolveMap" } },
            { "_SSS", new[] { "_ThicknessMap" } },
            { "_PARALLAX", new[] { "_ParallaxMap" } }
        };

        private Vector2 scrollPosition;
        private List<Material> materialsToValidate = new List<Material>();
        private List<ValidationResult> validationResults = new List<ValidationResult>();
        private bool autoFixAvailable = false;

        // Validation settings
        private bool checkVRChatOptimization = true;
        private bool checkTextureSize = true;
        private bool checkPerformance = true;
        private bool checkUnusedFeatures = true;
        private bool checkTextureCompression = true;

        private enum ValidationSeverity
        {
            Info,
            Warning,
            Error
        }

        private class ValidationResult
        {
            public Material material;
            public string issue;
            public ValidationSeverity severity;
            public string suggestion;
            public System.Action autoFixAction;
            public string category;
        }

        [MenuItem("Tools/Natane/マテリアル Material/マテリアル検証 Material Validator _m", false, 11)]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialValidator>(L("マテリアル検証", "Material Validator"));
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(5);
            DrawMaterialSelection();
            EditorGUILayout.Space(5);
            DrawValidationSettings();
            EditorGUILayout.Space(10);
            DrawValidationResults();
        }

        private void DrawHeader()
        {
            NataneToonShaderGUIUtility.DrawToolHeader("マテリアル検証&最適化", "Material Validator & Optimizer", "MaterialValidator");
            EditorGUILayout.HelpBox(
                L("VRChat最適化、パフォーマンス問題、一般的な問題についてマテリアルを検証します。\n可能な場合は自動修正の提案が提供されます。",
                  "Validates materials for VRChat optimization, performance issues, and common problems.\nAuto-fix suggestions provided where possible."),
                MessageType.Info);
        }

        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("検証するマテリアル", "Materials to Validate"), EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(L("選択マテリアルを追加", "Add Selected Materials"), GUILayout.Height(25)))
            {
                AddSelectedMaterials();
            }

            if (GUILayout.Button(L("全Natane Toonマテリアルを追加", "Add All Natane Toon Materials"), GUILayout.Height(25)))
            {
                AddAllNataneToonMaterials();
            }

            if (GUILayout.Button(L("リストをクリア", "Clear List"), GUILayout.Height(25)))
            {
                materialsToValidate.Clear();
                validationResults.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Display material list
            if (materialsToValidate.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    L("マテリアルが選択されていません。上のボタンを使用してマテリアルを追加してください。",
                      "No materials selected. Use the buttons above to add materials."),
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField(L($"選択マテリアル ({materialsToValidate.Count}):", $"Selected Materials ({materialsToValidate.Count}):"), EditorStyles.miniLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
                for (int i = materialsToValidate.Count - 1; i >= 0; i--)
                {
                    if (materialsToValidate[i] == null)
                    {
                        materialsToValidate.RemoveAt(i);
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(materialsToValidate[i], typeof(Material), false);
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        materialsToValidate.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("検証設定", "Validation Settings"), EditorStyles.boldLabel);

            checkVRChatOptimization = EditorGUILayout.ToggleLeft(L("VRChat最適化チェック", "VRChat Optimization Check"), checkVRChatOptimization);
            checkTextureSize = EditorGUILayout.ToggleLeft(L("テクスチャサイズチェック", "Texture Size Check"), checkTextureSize);
            checkPerformance = EditorGUILayout.ToggleLeft(L("パフォーマンス評価チェック", "Performance Rating Check"), checkPerformance);
            checkUnusedFeatures = EditorGUILayout.ToggleLeft(L("未使用機能チェック", "Unused Features Check"), checkUnusedFeatures);
            checkTextureCompression = EditorGUILayout.ToggleLeft(L("テクスチャ圧縮チェック", "Texture Compression Check"), checkTextureCompression);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(L("すべて検証", "Validate All"), GUILayout.Height(30)))
            {
                ValidateAllMaterials();
            }

            using (new EditorGUI.DisabledScope(!autoFixAvailable))
            {
                if (GUILayout.Button(L("すべての問題を自動修正", "Auto-Fix All Issues"), GUILayout.Height(30)))
                {
                    AutoFixAllIssues();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationResults()
        {
            if (validationResults.Count == 0) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("検証結果", "Validation Results"), EditorStyles.boldLabel);

            // Summary
            int errors = validationResults.Count(r => r.severity == ValidationSeverity.Error);
            int warnings = validationResults.Count(r => r.severity == ValidationSeverity.Warning);
            int infos = validationResults.Count(r => r.severity == ValidationSeverity.Info);

            EditorGUILayout.BeginHorizontal();
            GUI.color = errors > 0 ? Color.red : Color.white;
            EditorGUILayout.LabelField(L($"エラー: {errors}", $"Errors: {errors}"), EditorStyles.boldLabel, GUILayout.Width(150));
            GUI.color = warnings > 0 ? Color.yellow : Color.white;
            EditorGUILayout.LabelField(L($"警告: {warnings}", $"Warnings: {warnings}"), EditorStyles.boldLabel, GUILayout.Width(150));
            GUI.color = Color.white;
            EditorGUILayout.LabelField(L($"情報: {infos}", $"Info: {infos}"), EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Results list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var result in validationResults)
            {
                DrawValidationResult(result);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawValidationResult(ValidationResult result)
        {
            Color bgColor = Color.white;
            switch (result.severity)
            {
                case ValidationSeverity.Error:
                    bgColor = new Color(1f, 0.5f, 0.5f, 0.3f);
                    break;
                case ValidationSeverity.Warning:
                    bgColor = new Color(1f, 1f, 0.5f, 0.3f);
                    break;
                case ValidationSeverity.Info:
                    bgColor = new Color(0.5f, 0.8f, 1f, 0.3f);
                    break;
            }

            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Header
            EditorGUILayout.BeginHorizontal();

            // Severity icon
            string icon = result.severity == ValidationSeverity.Error ? "⛔" :
                         result.severity == ValidationSeverity.Warning ? "⚠️" : "ℹ️";
            EditorGUILayout.LabelField(icon, GUILayout.Width(20));

            // Category and material
            EditorGUILayout.LabelField($"[{result.category}] {result.material.name}", EditorStyles.boldLabel);

            // Ping button
            if (GUILayout.Button("→", GUILayout.Width(30)))
            {
                EditorGUIUtility.PingObject(result.material);
                Selection.activeObject = result.material;
            }

            EditorGUILayout.EndHorizontal();

            // Issue description
            EditorGUILayout.LabelField(L("問題:", "Issue:"), EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(result.issue, EditorStyles.wordWrappedLabel);

            // Suggestion
            if (!string.IsNullOrEmpty(result.suggestion))
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(L("提案:", "Suggestion:"), EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(result.suggestion, EditorStyles.wordWrappedLabel);
            }

            // Auto-fix button
            if (result.autoFixAction != null)
            {
                EditorGUILayout.Space(3);
                if (GUILayout.Button(L("自動修正", "Auto-Fix"), GUILayout.Height(20)))
                {
                    result.autoFixAction.Invoke();
                    EditorUtility.DisplayDialog(
                        L("自動修正を適用しました", "Auto-Fix Applied"),
                        L($"マテリアルの問題を修正しました: {result.material.name}",
                          $"Fixed issue for material: {result.material.name}"),
                        "OK");
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void AddSelectedMaterials()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Material material)
                {
                    if (!materialsToValidate.Contains(material))
                    {
                        materialsToValidate.Add(material);
                    }
                }
            }
        }

        private void AddAllNataneToonMaterials()
        {
            foreach (Material material in NataneMaterialAssetCache.GetMaterialsByShaderPrefix(NataneShaderPrefix))
            {
                if (material != null && !materialsToValidate.Contains(material))
                {
                    materialsToValidate.Add(material);
                }
            }

            Debug.Log($"[MaterialValidator] Found {materialsToValidate.Count} Natane Toon materials");
        }

        private void ValidateAllMaterials()
        {
            validationResults.Clear();
            autoFixAvailable = false;

            foreach (var material in materialsToValidate)
            {
                if (material == null) continue;

                if (checkVRChatOptimization) ValidateVRChatOptimization(material);
                if (checkTextureSize) ValidateTextureSize(material);
                if (checkPerformance) ValidatePerformance(material);
                if (checkUnusedFeatures) ValidateUnusedFeatures(material);
                if (checkTextureCompression) ValidateTextureCompression(material);
            }

            autoFixAvailable = validationResults.Any(r => r.autoFixAction != null);

            Debug.Log($"[MaterialValidator] Validation complete. Found {validationResults.Count} issues.");
        }

        private void ValidateVRChatOptimization(Material material)
        {
            ValidateDependencyPackages(material);

            // Check texture memory usage
            long totalMemory = 0;
            var countedTextures = new HashSet<Texture2D>();

            foreach (string texProp in VrchatTextureProperties)
            {
                if (material.HasProperty(texProp))
                {
                    var tex = material.GetTexture(texProp) as Texture2D;
                    if (tex != null)
                    {
                        // Count each Texture2D once even if reused across multiple slots.
                        if (countedTextures.Add(tex))
                        {
                            long memory = CalculateTextureMemory(tex);
                            totalMemory += memory;
                        }

                        // Check individual texture size
                        if (tex.width > 2048 || tex.height > 2048)
                        {
                            validationResults.Add(new ValidationResult
                            {
                                material = material,
                                category = "VRChat",
                                severity = ValidationSeverity.Warning,
                                issue = $"Texture '{texProp}' is {tex.width}x{tex.height}, exceeds VRChat recommended 2048x2048",
                                suggestion = "Consider resizing to 2048x2048 or lower for better performance",
                                autoFixAction = null // Can't auto-resize textures safely
                            });
                        }
                    }
                }
            }

            // Total memory check (VRChat recommends < 40MB per material)
            if (totalMemory > 40 * 1024 * 1024)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "VRChat",
                    severity = ValidationSeverity.Error,
                    issue = $"Total texture memory: {totalMemory / (1024 * 1024)}MB exceeds VRChat recommended 40MB",
                    suggestion = "Reduce texture sizes or use texture atlases"
                });
            }
        }

        private void ValidateTextureSize(Material material)
        {
            foreach (string texProp in TextureSizeProperties)
            {
                if (material.HasProperty(texProp))
                {
                    var tex = material.GetTexture(texProp) as Texture2D;
                    if (tex != null)
                    {
                        // Check if power of 2
                        if (!IsPowerOfTwo(tex.width) || !IsPowerOfTwo(tex.height))
                        {
                            validationResults.Add(new ValidationResult
                            {
                                material = material,
                                category = "Texture",
                                severity = ValidationSeverity.Warning,
                                issue = $"Texture '{texProp}' ({tex.width}x{tex.height}) is not power-of-2",
                                suggestion = "Use power-of-2 dimensions (256, 512, 1024, 2048) for better GPU performance"
                            });
                        }

                        // Check for excessive size on simple textures
                        if (texProp == "_RampTex" && (tex.width > 256 || tex.height > 256))
                        {
                            validationResults.Add(new ValidationResult
                            {
                                material = material,
                                category = "Texture",
                                severity = ValidationSeverity.Info,
                                issue = $"Ramp texture is {tex.width}x{tex.height}, unnecessarily large",
                                suggestion = "Ramp textures typically only need to be 256x16 or smaller"
                            });
                        }
                    }
                }
            }
        }

        private void ValidatePerformance(Material material)
        {
            int activeFeatures = CountActiveFeatures(material);
            NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate samplerEstimate =
                NataneToonSamplerBudgetEstimator.Estimate(material);

            string samplerSummary = BuildSamplerContributorSummary(samplerEstimate, 4);

            if (samplerEstimate.IsOverLimit)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Error,
                    issue = L(
                        $"推定 Sampler 上限を超えています ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})",
                        $"Estimated sampler usage exceeds the limit ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})"),
                    suggestion = L(
                        $"重い機能を無効化して上限内に戻してください。主な要因: {samplerSummary}",
                        $"Disable heavy features to get back under the limit. Main contributors: {samplerSummary}")
                });
            }
            else if (samplerEstimate.HasCriticalLightingCombo)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Warning,
                    issue = L(
                        "Light Volume + LTCGI + Hatching は Sampler 制限に引っかかりやすい危険な組み合わせです",
                        "Light Volume + LTCGI + Hatching is a high-risk sampler combination"),
                    suggestion = L(
                        "髪や追加テクスチャを盛る前に、Hatching か第三者ライティング機能の構成を見直してください。",
                        "Review Hatching or third-party lighting before adding more heavy texture features.")
                });
            }
            else if (samplerEstimate.HasScreenSpaceLightingCombo)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Warning,
                    issue = L(
                        "Light Volume + LTCGI + Screen Edge は Sampler 制限に届きやすい危険な組み合わせです",
                        "Light Volume + LTCGI + Screen Edge is a high-risk sampler combination"),
                    suggestion = L(
                        $"Screen Edge か第三者ライティングの構成を見直してください。主な要因: {samplerSummary}",
                        $"Review Screen Edge or third-party lighting. Main contributors: {samplerSummary}")
                });
            }
            else if (samplerEstimate.HasLightVolumeLtcgiCombo)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Warning,
                    issue = L(
                        $"Light Volume と LTCGI を同時使用しています (推定 {samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})",
                        $"Light Volume and LTCGI are enabled together (estimated {samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})"),
                    suggestion = L(
                        $"他の重い機能を追加する前に Sampler 余裕を確認してください。主な要因: {samplerSummary}",
                        $"Check sampler headroom before enabling more heavy features. Main contributors: {samplerSummary}")
                });
            }
            else if (samplerEstimate.IsNearLimit)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Warning,
                    issue = L(
                        $"推定 Sampler 数が上限付近です ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})",
                        $"Estimated sampler usage is near the limit ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})"),
                    suggestion = L(
                        $"新しい重い機能を追加する前に構成を見直してください。主な要因: {samplerSummary}",
                        $"Review the current setup before enabling more heavy features. Main contributors: {samplerSummary}")
                });
            }
            else if (samplerEstimate.IsWarning)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Info,
                    issue = L(
                        $"推定 Sampler 数は注意域です ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})",
                        $"Estimated sampler usage is in the caution range ({samplerEstimate.EstimatedSamplers}/{samplerEstimate.Limit})"),
                    suggestion = L(
                        $"Light Volume や LTCGI を追加する前に現在の構成を確認してください。主な要因: {samplerSummary}",
                        $"Check the current setup before enabling Light Volume or LTCGI. Main contributors: {samplerSummary}")
                });
            }

            if (activeFeatures > 12)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Warning,
                    issue = L(
                        $"{activeFeatures} 個の機能が有効です。GPU負荷が高めです (Rating: D)",
                        $"{activeFeatures} features are enabled. GPU cost is heavy (Rating: D)"),
                    suggestion = L(
                        "使っていない機能を無効化し、Quest や VR 向けなら構成を簡素化してください。",
                        "Disable unused features and simplify the setup for Quest or VR targets.")
                });
            }
            else if (activeFeatures > 8 && !samplerEstimate.IsWarning)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Info,
                    issue = L(
                        $"{activeFeatures} 個の機能が有効です。中程度のGPU負荷です (Rating: C)",
                        $"{activeFeatures} features are enabled. GPU cost is moderate (Rating: C)"),
                    suggestion = L(
                        "必要性の低い機能はオフにすると、モバイルやVR向けに余裕ができます。",
                        "Disabling less important features will add headroom for mobile or VR.")
                });
            }
        }

        private void ValidateUnusedFeatures(Material material)
        {
            foreach (KeyValuePair<string, string[]> check in UnusedFeatureTextureRequirements)
            {
                if (material.IsKeywordEnabled(check.Key))
                {
                    bool hasTexture = false;
                    foreach (var texProp in check.Value)
                    {
                        if (material.HasProperty(texProp) && material.GetTexture(texProp) != null)
                        {
                            hasTexture = true;
                            break;
                        }
                    }

                    if (!hasTexture)
                    {
                        string keyword = check.Key;
                        validationResults.Add(new ValidationResult
                        {
                            material = material,
                            category = "Unused Feature",
                            severity = ValidationSeverity.Warning,
                            issue = $"Feature '{keyword}' is enabled but no texture assigned",
                            suggestion = $"Either assign a texture or disable the feature to save performance",
                            autoFixAction = () =>
                            {
                                material.DisableKeyword(keyword);
                                EditorUtility.SetDirty(material);
                            }
                        });
                    }
                }
            }
        }

        private void ValidateTextureCompression(Material material)
        {
            var textures = new[] { "_MainTex", "_BumpMap", "_EmissionMap" };

            foreach (var texProp in textures)
            {
                if (material.HasProperty(texProp))
                {
                    var tex = material.GetTexture(texProp) as Texture2D;
                    if (tex != null)
                    {
                        string path = AssetDatabase.GetAssetPath(tex);
                        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                        if (importer != null)
                        {
                            var platformSettings = importer.GetDefaultPlatformTextureSettings();

                            if (platformSettings.format == TextureImporterFormat.RGBA32 ||
                                platformSettings.format == TextureImporterFormat.RGB24)
                            {
                                validationResults.Add(new ValidationResult
                                {
                                    material = material,
                                    category = "Compression",
                                    severity = ValidationSeverity.Warning,
                                    issue = $"Texture '{texProp}' is uncompressed (format: {platformSettings.format})",
                                    suggestion = "Use DXT/BC compression for PC, ASTC for mobile",
                                    autoFixAction = () =>
                                    {
                                        importer.textureCompression = TextureImporterCompression.Compressed;
                                        importer.SaveAndReimport();
                                        EditorUtility.SetDirty(material);
                                    }
                                });
                            }
                        }
                    }
                }
            }
        }

        private void AutoFixAllIssues()
        {
            int fixedCount = 0;

            foreach (var result in validationResults)
            {
                if (result.autoFixAction != null)
                {
                    result.autoFixAction.Invoke();
                    fixedCount++;
                }
            }

            EditorUtility.DisplayDialog(
                L("自動修正完了", "Auto-Fix Complete"),
                L($"{fixedCount}個の問題を自動的に修正しました。\n残りの問題は手動での対処が必要です。",
                  $"Fixed {fixedCount} issues automatically.\nRemaining issues require manual intervention."),
                "OK");

            // Re-validate
            ValidateAllMaterials();
        }

        private long CalculateTextureMemory(Texture2D tex)
        {
            if (tex == null) return 0;

            // Rough estimation based on format
            int bytesPerPixel = 4; // Assume RGBA32

            string path = AssetDatabase.GetAssetPath(tex);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                var format = importer.GetDefaultPlatformTextureSettings().format;
                switch (format)
                {
                    case TextureImporterFormat.DXT1:
                        bytesPerPixel = 0; // 0.5 bytes per pixel
                        return tex.width * tex.height / 2;
                    case TextureImporterFormat.DXT5:
                        bytesPerPixel = 1;
                        break;
                    case TextureImporterFormat.RGBA32:
                    case TextureImporterFormat.ARGB32:
                        bytesPerPixel = 4;
                        break;
                    case TextureImporterFormat.RGB24:
                        bytesPerPixel = 3;
                        break;
                }
            }

            return tex.width * tex.height * bytesPerPixel;
        }

        private int CountActiveFeatures(Material material)
        {
            int count = 0;

            foreach (string keyword in ActiveFeatureKeywords)
            {
                if (material.IsKeywordEnabled(keyword))
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        private void ValidateDependencyPackages(Material material)
        {
            if (material.IsKeywordEnabled("_USE_LIGHT_VOLUME") &&
                !NataneDependencyStatus.IsVRCLightVolumesInstalled())
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "VRChat",
                    severity = ValidationSeverity.Warning,
                    issue = L(
                        "Light Volume が有効ですが、VRC Light Volumes パッケージが検出されていません",
                        "Light Volume is enabled, but the VRC Light Volumes package is not detected"),
                    suggestion = L(
                        "Tools > Natane > VRChat > VRC Light Volumes 再検出 を実行するか、不要なら Light Volume を無効化してください。",
                        "Run Tools > Natane > VRChat > VRC Light Volumes Re-detect, or disable Light Volume if you do not need it.")
                });
            }

            if (material.IsKeywordEnabled("_LTCGI") &&
                !NataneDependencyStatus.IsLTCGIInstalled())
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "VRChat",
                    severity = ValidationSeverity.Warning,
                    issue = L(
                        "LTCGI が有効ですが、LTCGI パッケージが検出されていません",
                        "LTCGI is enabled, but the LTCGI package is not detected"),
                    suggestion = L(
                        "Tools > Natane > VRChat > LTCGI 再検出 を実行するか、不要なら LTCGI を無効化してください。",
                        "Run Tools > Natane > VRChat > LTCGI Re-detect, or disable LTCGI if you do not need it.")
                });
            }
        }

        private string BuildSamplerContributorSummary(
            NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate estimate,
            int maxCount)
        {
            if (estimate.Contributors == null || estimate.Contributors.Length == 0)
            {
                return L("主要因なし", "No major contributors");
            }

            int count = Mathf.Min(maxCount, estimate.Contributors.Length);
            string[] parts = new string[count];

            for (int i = 0; i < count; i++)
            {
                parts[i] = NataneToonSamplerBudgetEstimator.FormatContributor(estimate.Contributors[i]);
            }

            return string.Join(", ", parts);
        }
    }
}
