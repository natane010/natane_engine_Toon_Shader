using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Shader Variant Collection Tool for Natane Toon Shader
    /// Natane Toon Shaderのシェーダーバリアントコレクションツール
    /// Collects and manages shader variants to optimize build size and loading times
    /// シェーダーバリアントを収集・管理してビルドサイズと読み込み時間を最適化
    /// </summary>
    public class ShaderVariantCollector : EditorWindow
    {
        private ShaderVariantCollection collection;
        private bool includeBasic = true;
        private bool includeAdvanced = true;
        private bool includeVirtualExpression = true;
        private bool includeAllCombinations = false;
        private bool collectFromUsedMaterialKeywords = true;

        private Vector2 scrollPosition;
        private int estimatedVariants = 0;

        [MenuItem("Tools/Natane/シェーダー Shader/シェーダーバリアント収集 Shader Variant Collector", false, 71)]
        public static void ShowWindow()
        {
            GetWindow<ShaderVariantCollector>(L("シェーダーバリアント収集", "Shader Variant Collector"));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(L("Natane Toon シェーダーバリアント収集", "Natane Toon Shader Variant Collector"), EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                L("このツールはShaderVariantCollectionを作成します。\n" +
                "- 未使用バリアントを除外してビルドサイズを削減\n" +
                "- プリウォームでロード時間を改善\n" +
                "- 実行時のシェーダーコンパイルのスタッターを防止",
                "This tool creates a ShaderVariantCollection to:\n" +
                "- Reduce build size by excluding unused variants\n" +
                "- Improve loading times with pre-warmed shaders\n" +
                "- Prevent shader compilation stutters at runtime"),
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Collection Reference
            collection = (ShaderVariantCollection)EditorGUILayout.ObjectField(
                L("バリアントコレクション", "Variant Collection"),
                collection,
                typeof(ShaderVariantCollection),
                false
            );

            if (collection == null)
            {
                EditorGUILayout.HelpBox(
                    L("コレクションが選択されていません。「新規コレクション作成」をクリックして作成してください。",
                    "No collection selected. Click 'Create New Collection' to create one."),
                    MessageType.Warning
                );

                if (GUILayout.Button(L("新規コレクション作成", "Create New Collection")))
                {
                    CreateNewCollection();
                }

                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(L("バリアントオプション", "Variant Options"), EditorStyles.boldLabel);
            collectFromUsedMaterialKeywords = EditorGUILayout.Toggle(
                L("プロジェクト内マテリアルのキーワードのみ収集", "Collect Used Material Keywords Only"),
                collectFromUsedMaterialKeywords
            );
            if (collectFromUsedMaterialKeywords)
            {
                EditorGUILayout.HelpBox(
                    L("Natane Toon系マテリアルを走査し、実際に使われているキーワードの組み合わせのみを収集します。",
                    "Scan Natane Toon materials and collect only actually used keyword sets."),
                    MessageType.Info
                );
            }
            EditorGUI.BeginDisabledGroup(collectFromUsedMaterialKeywords);
            includeBasic = EditorGUILayout.Toggle(L("基本バリアントを含む", "Include Basic Variants"), includeBasic);
            EditorGUILayout.HelpBox(L("一般的な組み合わせ: 機能なし、アウトラインのみ、エミッションのみ", "Common combinations: No features, Outline only, Emission only"), MessageType.None);

            includeAdvanced = EditorGUILayout.Toggle(L("高度なバリアントを含む", "Include Advanced Variants"), includeAdvanced);
            EditorGUILayout.HelpBox(L("高度な組み合わせ: SSS、MatCap、スペキュラー、リムライト", "Advanced combinations: SSS, MatCap, Specular, Rim Light"), MessageType.None);

            includeVirtualExpression = EditorGUILayout.Toggle(L("バーチャル表現を含む", "Include Virtual Expression"), includeVirtualExpression);
            EditorGUILayout.HelpBox(L("バーチャル表現: ディゾルブ、色相シフト、エミッションアニメーション", "Virtual expression: Dissolve, Hue Shift, Emission Animations"), MessageType.None);

            EditorGUILayout.Space();
            includeAllCombinations = EditorGUILayout.Toggle(L("全組み合わせを含む (警告)", "Include All Combinations (WARNING)"), includeAllCombinations);

            if (includeAllCombinations)
            {
                EditorGUILayout.HelpBox(
                    L("これは数千バリアントを作成し、ビルドサイズを大幅に増やします。\n" +
                    "テストまたはすべての組み合わせが必要な場合のみ使用してください。",
                    "This will create thousands of variants and significantly increase build size!\n" +
                    "Only use for testing or if you need every possible combination."),
                    MessageType.Warning
                );
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            // Estimate variants
            if (collectFromUsedMaterialKeywords)
            {
                EditorGUILayout.LabelField(L("推定バリアント数: マテリアルキーワードセット (自動)", "Estimated Variants: material keyword sets (auto)"), EditorStyles.boldLabel);
            }
            else
            {
                estimatedVariants = EstimateVariantCount();
                EditorGUILayout.LabelField($"{L("推定バリアント数", "Estimated Variants")}: {estimatedVariants}", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space();

            // Action Buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(L("バリアントを収集", "Collect Variants"), GUILayout.Height(30)))
            {
                CollectVariants();
            }

            if (GUILayout.Button(L("コレクションをクリア", "Clear Collection"), GUILayout.Height(30)))
            {
                ClearCollection();
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button(L("コレクションを保存", "Save Collection"), GUILayout.Height(30)))
            {
                SaveCollection();
            }

            EditorGUILayout.Space();

            // Current Collection Info
            if (collection != null)
            {
                EditorGUILayout.LabelField(L("現在のコレクション情報", "Current Collection Info"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{L("シェーダー数", "Shader Count")}: {collection.shaderCount}");
                EditorGUILayout.LabelField($"{L("バリアント数", "Variant Count")}: {collection.variantCount}");

                // Empty collection warning
                if (collection.variantCount == 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox(
                        L("コレクションが空です。「バリアントを収集」を実行してバリアントを追加してください。\n" +
                        "空のコレクションではビルド最適化やプリウォーミングの効果がありません。",
                        "Collection is empty. Run 'Collect Variants' to add variants.\n" +
                        "An empty collection provides no build optimization or prewarming benefit."),
                        MessageType.Warning
                    );
                }
            }
        }

        private void CreateNewCollection()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                L("シェーダーバリアントコレクションを作成", "Create Shader Variant Collection"),
                "NataneToonShaderVariants",
                "shadervariants",
                L("シェーダーバリアントコレクションの保存場所を選択", "Choose a location to save the shader variant collection"),
                "Assets/ShaderVariants"
            );

            if (!string.IsNullOrEmpty(path))
            {
                collection = new ShaderVariantCollection();
                AssetDatabase.CreateAsset(collection, path);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(
                    L("成功", "Success"),
                    L("シェーダーバリアントコレクションが正常に作成されました。", "Shader Variant Collection created successfully!"),
                    "OK");
            }
        }

        private int EstimateVariantCount()
        {
            int count = 0;

            if (includeBasic)
                count += 10; // Basic combinations

            if (includeAdvanced)
                count += 30; // Advanced feature combinations

            if (includeVirtualExpression)
                count += 20; // Virtual expression combinations

            if (includeAllCombinations)
                count = 8192; // 2^13 keywords (rough estimate)

            // Multiply by 4 for shader variants (Opaque, Cutout, Transparent, Wirelight)
            count *= 4;

            return count;
        }

        private void CollectVariants()
        {
            if (collection == null)
            {
                EditorUtility.DisplayDialog(
                    L("エラー", "Error"),
                    L("最初にコレクションを作成または選択してください。", "Please create or select a collection first!"),
                    "OK");
                return;
            }

            collection.Clear();

            // Find shaders
            Shader opaqueShader = Shader.Find("Natane/Toon Shader");
            Shader cutoutShader = Shader.Find("Natane/Toon Shader (Cutout)");
            Shader transparentShader = Shader.Find("Natane/Toon Shader (Transparent)");
            Shader wirelightShader = Shader.Find("Natane/Toon Shader Wirelight");

            if (opaqueShader == null)
            {
                EditorUtility.DisplayDialog(
                    L("エラー", "Error"),
                    L("Natane Toon Shaderが見つかりませんでした。", "Could not find Natane Toon Shaders!"),
                    "OK");
                return;
            }

            List<string[]> variantCombinations = collectFromUsedMaterialKeywords
                ? CollectKeywordSetsFromProjectMaterials()
                : GenerateVariantCombinations();

            int totalVariants = 0;

            foreach (string[] keywords in variantCombinations)
            {
                // Add to all shader variants (Opaque, Cutout, Transparent, Wirelight)
                if (opaqueShader != null)
                {
                    AddVariant(opaqueShader, PassType.ForwardBase, keywords);
                    AddVariant(opaqueShader, PassType.ForwardAdd, keywords);
                    totalVariants += 2;
                }

                if (cutoutShader != null)
                {
                    AddVariant(cutoutShader, PassType.ForwardBase, keywords);
                    AddVariant(cutoutShader, PassType.ForwardAdd, keywords);
                    totalVariants += 2;
                }

                if (transparentShader != null)
                {
                    AddVariant(transparentShader, PassType.ForwardBase, keywords);
                    AddVariant(transparentShader, PassType.ForwardAdd, keywords);
                    totalVariants += 2;
                }

                if (wirelightShader != null)
                {
                    AddVariant(wirelightShader, PassType.ForwardBase, keywords);
                    totalVariants += 1;
                }
            }

            // Add ShadowCaster pass with empty keywords for each shader
            if (opaqueShader != null)
            {
                AddVariant(opaqueShader, PassType.ShadowCaster, new string[] { });
                totalVariants += 1;
            }
            if (cutoutShader != null)
            {
                AddVariant(cutoutShader, PassType.ShadowCaster, new string[] { });
                totalVariants += 1;
            }
            if (transparentShader != null)
            {
                AddVariant(transparentShader, PassType.ShadowCaster, new string[] { });
                totalVariants += 1;
            }

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            string modeSuffix = collectFromUsedMaterialKeywords
                ? $"\nKeyword Sets: {variantCombinations.Count}"
                : string.Empty;

            EditorUtility.DisplayDialog(
                L("成功", "Success"),
                L($"{totalVariants}個のシェーダーバリアントを収集しました。",
                $"Collected {totalVariants} shader variants!") + "\n" +
                $"{L("シェーダー数", "Shader Count")}: {collection.shaderCount}\n" +
                $"{L("バリアント数", "Variant Count")}: {collection.variantCount}" +
                modeSuffix,
                "OK"
            );
        }

        /// <summary>
        /// Collect keyword sets from all Natane Toon materials in the project.
        /// プロジェクト内のNatane Toonマテリアルからキーワードセットを収集します。
        /// Also used by ShaderPrewarmingEditor for auto-collection.
        /// ShaderPrewarmingEditorの自動収集でも使用されます。
        /// </summary>
        public List<string[]> CollectKeywordSetsFromProjectMaterials()
        {
            var combinations = new List<string[]>();
            var uniqueSets = new HashSet<string>();
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");

            try
            {
                for (int i = 0; i < materialGuids.Length; i++)
                {
                    // Show progress bar every 50 materials
                    if (i % 50 == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            L("マテリアルスキャン", "Material Scan"),
                            L($"マテリアルを走査中... ({i}/{materialGuids.Length})",
                            $"Scanning materials... ({i}/{materialGuids.Length})"),
                            (float)i / materialGuids.Length);
                    }

                    string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null || material.shader == null)
                    {
                        continue;
                    }

                    if (!IsNataneToonShader(material.shader.name))
                    {
                        continue;
                    }

                    string[] keywords = material.shaderKeywords
                        .Where(keyword => !string.IsNullOrEmpty(keyword))
                        .Distinct()
                        .OrderBy(keyword => keyword)
                        .ToArray();

                    string key = string.Join(";", keywords);
                    if (uniqueSets.Add(key))
                    {
                        combinations.Add(keywords);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Always include the base variant.
            if (uniqueSets.Add(string.Empty))
            {
                combinations.Insert(0, new string[] { });
            }

            return combinations;
        }

        private static bool IsNataneToonShader(string shaderName)
        {
            return shaderName == "Natane/Toon Shader" ||
                   shaderName == "Natane/Toon Shader (Cutout)" ||
                   shaderName == "Natane/Toon Shader (Transparent)" ||
                   shaderName == "Natane/Toon Shader Wirelight";
        }

        private List<string[]> GenerateVariantCombinations()
        {
            List<string[]> combinations = new List<string[]>();

            if (includeAllCombinations)
            {
                // Generate all possible combinations (warning: huge!)
                combinations.AddRange(GenerateAllCombinations());
            }
            else
            {
                // Generate curated combinations
                if (includeBasic)
                {
                    combinations.AddRange(GenerateBasicVariants());
                }

                if (includeAdvanced)
                {
                    combinations.AddRange(GenerateAdvancedVariants());
                }

                if (includeVirtualExpression)
                {
                    combinations.AddRange(GenerateVirtualExpressionVariants());
                }
            }

            return combinations;
        }

        private List<string[]> GenerateBasicVariants()
        {
            return new List<string[]>
            {
                new string[] { }, // No keywords (base variant)
                new string[] { "_NORMALMAP" },
                new string[] { "_EMISSION" },
                new string[] { "_EMISSION", "_NORMALMAP" },
                new string[] { "_USE_RAMP" },
                new string[] { "_USE_RAMP", "_NORMALMAP" },
            };
        }

        private List<string[]> GenerateAdvancedVariants()
        {
            return new List<string[]>
            {
                new string[] { "_SPECULAR" },
                new string[] { "_SPECULAR", "_NORMALMAP" },
                new string[] { "_RIM_LIGHT" },
                new string[] { "_RIM_LIGHT", "_NORMALMAP" },
                new string[] { "_MATCAP" },
                new string[] { "_MATCAP", "_NORMALMAP" },
                new string[] { "_SSS" },
                new string[] { "_SSS", "_NORMALMAP" },
                new string[] { "_SSS", "_NORMALMAP" },
                new string[] { "_SPECULAR", "_RIM_LIGHT", "_NORMALMAP" },
                new string[] { "_EMISSION", "_MATCAP", "_NORMALMAP" },
            };
        }

        private List<string[]> GenerateVirtualExpressionVariants()
        {
            return new List<string[]>
            {
                new string[] { "_DISSOLVE" },
                new string[] { "_DISSOLVE", "_NORMALMAP" },
                new string[] { "_HUE_SHIFT" },
                new string[] { "_HUE_SHIFT", "_NORMALMAP" },
                new string[] { "_DISSOLVE", "_HUE_SHIFT" },
                new string[] { "_EMISSION" },
                new string[] { "_EMISSION", "_NORMALMAP" },
                new string[] { "_DISSOLVE", "_EMISSION" },
                new string[] { "_HUE_SHIFT", "_EMISSION" },
            };
        }

        private List<string[]> GenerateAllCombinations()
        {
            // WARNING: This generates thousands of combinations!
            List<string[]> combinations = new List<string[]>();

            string[] allKeywords = new string[]
            {
                "_USE_RAMP", "_SPECULAR", "_RIM_LIGHT", "_SSS",
                "_MATCAP", "_EMISSION",
                "_NORMALMAP", "_DISSOLVE", "_HUE_SHIFT"
            };

            // Generate all possible combinations (2^9 = 512 combinations)
            int totalCombinations = 1 << allKeywords.Length;

            for (int i = 0; i < totalCombinations; i++)
            {
                List<string> keywords = new List<string>();

                for (int j = 0; j < allKeywords.Length; j++)
                {
                    if ((i & (1 << j)) != 0)
                    {
                        keywords.Add(allKeywords[j]);
                    }
                }

                combinations.Add(keywords.ToArray());
            }

            return combinations;
        }

        private void AddVariant(Shader shader, PassType passType, string[] keywords)
        {
            ShaderVariantCollection.ShaderVariant variant = new ShaderVariantCollection.ShaderVariant
            {
                shader = shader,
                passType = passType,
                keywords = keywords
            };

            if (!collection.Contains(variant))
            {
                collection.Add(variant);
            }
        }

        private void ClearCollection()
        {
            if (collection != null)
            {
                if (EditorUtility.DisplayDialog(
                    L("コレクションをクリア", "Clear Collection"),
                    L("このコレクションからすべてのバリアントをクリアしてもよろしいですか？",
                    "Are you sure you want to clear all variants from this collection?"),
                    L("はい", "Yes"),
                    L("いいえ", "No")))
                {
                    collection.Clear();
                    EditorUtility.SetDirty(collection);
                    AssetDatabase.SaveAssets();
                    EditorUtility.DisplayDialog(
                        L("成功", "Success"),
                        L("コレクションをクリアしました。", "Collection cleared!"),
                        "OK");
                }
            }
        }

        private void SaveCollection()
        {
            if (collection != null)
            {
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(
                    L("成功", "Success"),
                    L("コレクションを保存しました。", "Collection saved!"),
                    "OK");
            }
        }
    }
}
