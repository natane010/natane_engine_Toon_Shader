using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// Material validation and optimization checker
    /// Provides VRChat optimization checks, performance ratings, and auto-fix suggestions
    /// </summary>
    public class MaterialValidator : EditorWindow
    {
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

        [MenuItem("Tools/Natane/Material Validator", false, 60)]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialValidator>("マテリアル検証ツール");
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
            EditorGUILayout.LabelField("マテリアル検証 & 最適化ツール", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "VRChatの最適化、パフォーマンス問題、一般的な問題についてマテリアルを検証します。\n" +
                "可能な限りで自動修正の提案が提供されます。",
                MessageType.Info);
        }

        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("検証するマテリアル", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("選択されたマテリアルを追加", GUILayout.Height(25)))
            {
                AddSelectedMaterials();
            }

            if (GUILayout.Button("すべてのNatane Toonマテリアルを追加", GUILayout.Height(25)))
            {
                AddAllNataneToonMaterials();
            }

            if (GUILayout.Button("リストをクリア", GUILayout.Height(25)))
            {
                materialsToValidate.Clear();
                validationResults.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Display material list
            if (materialsToValidate.Count == 0)
            {
                EditorGUILayout.HelpBox("マテリアルが選択されていません。上のボタンを使用してマテリアルを追加してください。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"選択されたマテリアル ({materialsToValidate.Count}):", EditorStyles.miniLabel);

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
            EditorGUILayout.LabelField("検証設定", EditorStyles.boldLabel);

            checkVRChatOptimization = EditorGUILayout.ToggleLeft("VRChat最適化チェック", checkVRChatOptimization);
            checkTextureSize = EditorGUILayout.ToggleLeft("テクスチャサイズチェック", checkTextureSize);
            checkPerformance = EditorGUILayout.ToggleLeft("パフォーマンス評価チェック", checkPerformance);
            checkUnusedFeatures = EditorGUILayout.ToggleLeft("未使用機能チェック", checkUnusedFeatures);
            checkTextureCompression = EditorGUILayout.ToggleLeft("テクスチャ圧縮チェック", checkTextureCompression);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("すべて検証", GUILayout.Height(30)))
            {
                ValidateAllMaterials();
            }

            using (new EditorGUI.DisabledScope(!autoFixAvailable))
            {
                if (GUILayout.Button("すべての問題を自動修正", GUILayout.Height(30)))
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
            EditorGUILayout.LabelField("検証結果", EditorStyles.boldLabel);

            // Summary
            int errors = validationResults.Count(r => r.severity == ValidationSeverity.Error);
            int warnings = validationResults.Count(r => r.severity == ValidationSeverity.Warning);
            int infos = validationResults.Count(r => r.severity == ValidationSeverity.Info);

            EditorGUILayout.BeginHorizontal();
            GUI.color = errors > 0 ? Color.red : Color.white;
            EditorGUILayout.LabelField($"エラー: {errors}", EditorStyles.boldLabel, GUILayout.Width(100));
            GUI.color = warnings > 0 ? Color.yellow : Color.white;
            EditorGUILayout.LabelField($"警告: {warnings}", EditorStyles.boldLabel, GUILayout.Width(100));
            GUI.color = Color.white;
            EditorGUILayout.LabelField($"情報: {infos}", EditorStyles.boldLabel, GUILayout.Width(100));
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
            EditorGUILayout.LabelField("問題:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(result.issue, EditorStyles.wordWrappedLabel);

            // Suggestion
            if (!string.IsNullOrEmpty(result.suggestion))
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("提案:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(result.suggestion, EditorStyles.wordWrappedLabel);
            }

            // Auto-fix button
            if (result.autoFixAction != null)
            {
                EditorGUILayout.Space(3);
                if (GUILayout.Button("自動修正", GUILayout.Height(20)))
                {
                    result.autoFixAction.Invoke();
                    EditorUtility.DisplayDialog("自動修正が適用されました",
                        $"マテリアルの問題を修正しました: {result.material.name}", "OK");
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
            string[] guids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && material.shader != null)
                {
                    if (material.shader.name.Contains("Natane") && material.shader.name.Contains("Toon"))
                    {
                        if (!materialsToValidate.Contains(material))
                        {
                            materialsToValidate.Add(material);
                        }
                    }
                }
            }

            Debug.Log($"[MaterialValidator] {materialsToValidate.Count}個のNatane Toonマテリアルが見つかりました");
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

            Debug.Log($"[MaterialValidator] 検証完了。{validationResults.Count}個の問題が見つかりました。");
        }

        private void ValidateVRChatOptimization(Material material)
        {
            // Check texture memory usage
            long totalMemory = 0;
            var textures = new[] { "_MainTex", "_BumpMap", "_EmissionMap", "_MatCap", "_RampTex",
                                   "_DissolveMap", "_ThicknessMap", "_SpecularMask", "_RimMask",
                                   "_SSSMask", "_MatCapMask", "_EmissionMask", "_DissolveMask",
                                   "_ReflectionMask", "_EnvRimMask", "_ParallaxMap", "_RefractionMask" };

            foreach (var texProp in textures)
            {
                if (material.HasProperty(texProp))
                {
                    var tex = material.GetTexture(texProp) as Texture2D;
                    if (tex != null)
                    {
                        long memory = CalculateTextureMemory(tex);
                        totalMemory += memory;

                        // Check individual texture size
                        if (tex.width > 2048 || tex.height > 2048)
                        {
                            validationResults.Add(new ValidationResult
                            {
                                material = material,
                                category = "VRChat",
                                severity = ValidationSeverity.Warning,
                                issue = $"テクスチャ '{texProp}' のサイズは {tex.width}x{tex.height} で、VRChatの推奨 2048x2048 を超えています",
                                suggestion = "パフォーマンスを向上させるために 2048x2048 以下にリサイズすることを検討してください",
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
                    issue = $"合計テクスチャメモリ: {totalMemory / (1024 * 1024)}MB はVRChatの推奨 40MB を超えています",
                    suggestion = "テクスチャサイズを縮小するか、テクスチャアトラスを使用してください"
                });
            }
        }

        private void ValidateTextureSize(Material material)
        {
            var textures = new[] { "_MainTex", "_BumpMap", "_EmissionMap", "_MatCap", "_RampTex", "_ParallaxMap" };

            foreach (var texProp in textures)
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
                                issue = $"テクスチャ '{texProp}' ({tex.width}x{tex.height}) は2の累乗ではありません",
                                suggestion = "GPU パフォーマンスを向上させるために 2の累乗 (256, 512, 1024, 2048) を使用してください"
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
                                issue = $"ランプテクスチャは {tex.width}x{tex.height} で、不必要に大きいサイズです",
                                suggestion = "ランプテクスチャは通常 256x16 以下のサイズで十分です"
                            });
                        }
                    }
                }
            }
        }

        private void ValidatePerformance(Material material)
        {
            int activeFeatures = CountActiveFeatures(material);

            if (activeFeatures > 10)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Warning,
                    issue = $"{activeFeatures}個の機能が有効 - パフォーマンスへの大きな影響 (評価: D)",
                    suggestion = "パフォーマンスを改善するために未使用の機能を無効にしてください"
                });
            }
            else if (activeFeatures > 6)
            {
                validationResults.Add(new ValidationResult
                {
                    material = material,
                    category = "Performance",
                    severity = ValidationSeverity.Info,
                    issue = $"{activeFeatures}個の機能が有効 - パフォーマンスへの中程度の影響 (評価: C)",
                    suggestion = "モバイル/VR用に重要度の低い機能を無効にすることを検討してください"
                });
            }
        }

        private void ValidateUnusedFeatures(Material material)
        {
            // Check for enabled features without required textures
            var featureChecks = new Dictionary<string, string[]>
            {
                { "_MATCAP", new[] { "_MatCap" } },
                { "_NORMALMAP", new[] { "_BumpMap" } },
                { "_EMISSION", new[] { "_EmissionMap" } },
                { "_DISSOLVE", new[] { "_DissolveMap" } },
                { "_SSS", new[] { "_ThicknessMap" } },
                { "_PARALLAX", new[] { "_ParallaxMap" } }
            };

            foreach (var check in featureChecks)
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
                            issue = $"機能 '{keyword}' は有効ですがテクスチャが割り当てられていません",
                            suggestion = $"テクスチャを割り当てるか、パフォーマンスを節約するために機能を無効にしてください",
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
                                    issue = $"テクスチャ '{texProp}' は非圧縮です (形式: {platformSettings.format})",
                                    suggestion = "PC用はDXT/BC圧縮、モバイル用はASTCを使用してください",
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
                "自動修正完了",
                $"{fixedCount}個の問題を自動修正しました。\n" +
                $"残りの問題は手動で対応する必要があります。",
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
            string[] keywords = new[]
            {
                "_SPECULAR", "_RIM", "_SSS", "_MATCAP", "_OUTLINE", "_EMISSION",
                "_EMISSION_ANIMATION", "_DISSOLVE", "_HUE_SHIFT", "_NORMALMAP",
                "_REFLECTION", "_ENV_RIM", "_PARALLAX", "_REFRACTION"
            };

            foreach (string keyword in keywords)
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
    }
}
