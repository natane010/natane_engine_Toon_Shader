using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Shader Variant Collection Tool for Natane Toon Shader
/// Collects and manages shader variants to optimize build size and loading times
/// </summary>
public class ShaderVariantCollector : EditorWindow
{
    private ShaderVariantCollection collection;
    private bool includeBasic = true;
    private bool includeAdvanced = true;
    private bool includeVirtualExpression = true;
    private bool includeAllCombinations = false;

    private Vector2 scrollPosition;
    private int estimatedVariants = 0;

    [MenuItem("Tools/Natane/Shader Variant Collector")]
    public static void ShowWindow()
    {
        GetWindow<ShaderVariantCollector>("Shader Variant Collector");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Natane Toon Shader Variant Collector", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool creates a ShaderVariantCollection to:\n" +
            "- Reduce build size by excluding unused variants\n" +
            "- Improve loading times with pre-warmed shaders\n" +
            "- Prevent shader compilation stutters at runtime",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Collection Reference
        collection = (ShaderVariantCollection)EditorGUILayout.ObjectField(
            "Variant Collection",
            collection,
            typeof(ShaderVariantCollection),
            false
        );

        if (collection == null)
        {
            EditorGUILayout.HelpBox(
                "No collection selected. Click 'Create New Collection' to create one.",
                MessageType.Warning
            );

            if (GUILayout.Button("Create New Collection"))
            {
                CreateNewCollection();
            }

            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Variant Options", EditorStyles.boldLabel);

        includeBasic = EditorGUILayout.Toggle("Include Basic Variants", includeBasic);
        EditorGUILayout.HelpBox("Common combinations: No features, Outline only, Emission only", MessageType.None);

        includeAdvanced = EditorGUILayout.Toggle("Include Advanced Variants", includeAdvanced);
        EditorGUILayout.HelpBox("Advanced combinations: SSS, MatCap, Specular, Rim Light", MessageType.None);

        includeVirtualExpression = EditorGUILayout.Toggle("Include Virtual Expression", includeVirtualExpression);
        EditorGUILayout.HelpBox("Virtual expression: Dissolve, Hue Shift, Emission Animations", MessageType.None);

        EditorGUILayout.Space();
        includeAllCombinations = EditorGUILayout.Toggle("Include All Combinations (WARNING)", includeAllCombinations);

        if (includeAllCombinations)
        {
            EditorGUILayout.HelpBox(
                "This will create thousands of variants and significantly increase build size!\n" +
                "Only use for testing or if you need every possible combination.",
                MessageType.Warning
            );
        }

        EditorGUILayout.Space();

        // Estimate variants
        estimatedVariants = EstimateVariantCount();
        EditorGUILayout.LabelField($"Estimated Variants: {estimatedVariants}", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // Action Buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Collect Variants", GUILayout.Height(30)))
        {
            CollectVariants();
        }

        if (GUILayout.Button("Clear Collection", GUILayout.Height(30)))
        {
            ClearCollection();
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Save Collection", GUILayout.Height(30)))
        {
            SaveCollection();
        }

        EditorGUILayout.Space();

        // Current Collection Info
        if (collection != null)
        {
            EditorGUILayout.LabelField("Current Collection Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Shader Count: {collection.shaderCount}");
            EditorGUILayout.LabelField($"Variant Count: {collection.variantCount}");
        }
    }

    private void CreateNewCollection()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Shader Variant Collection",
            "NataneToonShaderVariants",
            "shadervariants",
            "Choose a location to save the shader variant collection",
            "Assets/ShaderVariants"
        );

        if (!string.IsNullOrEmpty(path))
        {
            collection = new ShaderVariantCollection();
            AssetDatabase.CreateAsset(collection, path);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", "Shader Variant Collection created successfully!", "OK");
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

        // Multiply by 3 for shader variants (Opaque, Cutout, Transparent)
        count *= 3;

        return count;
    }

    private void CollectVariants()
    {
        if (collection == null)
        {
            EditorUtility.DisplayDialog("Error", "Please create or select a collection first!", "OK");
            return;
        }

        collection.Clear();

        // Find shaders
        Shader opaqueShader = Shader.Find("Natane/Toon Shader");
        Shader cutoutShader = Shader.Find("Natane/Toon Shader (Cutout)");
        Shader transparentShader = Shader.Find("Natane/Toon Shader (Transparent)");

        if (opaqueShader == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Natane Toon Shaders!", "OK");
            return;
        }

        List<string[]> variantCombinations = GenerateVariantCombinations();

        int totalVariants = 0;

        foreach (string[] keywords in variantCombinations)
        {
            // Add to all three shader variants
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
        }

        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Success",
            $"Collected {totalVariants} shader variants!\n" +
            $"Shader Count: {collection.shaderCount}\n" +
            $"Variant Count: {collection.variantCount}",
            "OK"
        );
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
            new string[] { "_SSS", "_THICKNESS_MAP" },
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
            new string[] { "_EMISSION", "_EMISSION_SCROLL" },
            new string[] { "_EMISSION", "_EMISSION_PULSE" },
            new string[] { "_EMISSION", "_EMISSION_SCROLL", "_EMISSION_PULSE" },
            new string[] { "_DISSOLVE", "_EMISSION" },
            new string[] { "_HUE_SHIFT", "_EMISSION", "_EMISSION_PULSE" },
        };
    }

    private List<string[]> GenerateAllCombinations()
    {
        // WARNING: This generates thousands of combinations!
        List<string[]> combinations = new List<string[]>();

        string[] allKeywords = new string[]
        {
            "_USE_RAMP", "_SPECULAR", "_RIM_LIGHT", "_SSS", "_THICKNESS_MAP",
            "_MATCAP", "_EMISSION", "_EMISSION_SCROLL", "_EMISSION_PULSE",
            "_NORMALMAP", "_DISSOLVE", "_HUE_SHIFT"
        };

        // Generate all possible combinations (2^12 = 4096 combinations)
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
                "Clear Collection",
                "Are you sure you want to clear all variants from this collection?",
                "Yes",
                "No"))
            {
                collection.Clear();
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Success", "Collection cleared!", "OK");
            }
        }
    }

    private void SaveCollection()
    {
        if (collection != null)
        {
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", "Collection saved!", "OK");
        }
    }
}
