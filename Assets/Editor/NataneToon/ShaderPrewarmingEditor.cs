using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

namespace NataneToon.Editor
{
    /// <summary>
    /// Editor-only Shader Prewarming for Natane Toon Shader
    /// Automatically warms up shader variants before build to prevent runtime compilation stutters
    /// Safe for VRChat - no runtime scripts required
    /// </summary>
    public class ShaderPrewarmingEditor : IPreprocessBuildWithReport
    {
        // Build callback priority (lower = earlier execution)
        public int callbackOrder => 0;

        private const string SHADER_VARIANT_PATH = "Assets/ShaderVariants/NataneToonShaderVariants.shadervariants";
        private const string PREWARM_ENABLED_KEY = "NataneToon_PrewarmOnBuild";
        private const string AUTO_FIND_ENABLED_KEY = "NataneToon_AutoFindVariants";

        /// <summary>
        /// Automatically prewarm shaders before build
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (EditorPrefs.GetBool(PREWARM_ENABLED_KEY, true))
            {
                Debug.Log("[Natane Toon] Pre-build shader prewarming started...");
                PrewarmShaders(true);
            }
        }

        /// <summary>
        /// Initialize shader prewarming on Unity editor load
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            // Optional: Prewarm shaders when Unity starts
            // Uncomment if you want automatic prewarming on editor startup
            // PrewarmShaders(false);
        }

        [MenuItem("Tools/Natane/Shader Prewarming/Prewarm All Shaders", false, 100)]
        private static void PrewarmShadersMenu()
        {
            PrewarmShaders(true);
        }

        [MenuItem("Tools/Natane/Shader Prewarming/Prewarm Shader Variant Collection", false, 101)]
        private static void PrewarmShaderVariantCollectionMenu()
        {
            ShaderVariantCollection collection = FindShaderVariantCollection();
            if (collection != null)
            {
                PrewarmShaderVariantCollection(collection);
            }
        }

        [MenuItem("Tools/Natane/Shader Prewarming/Settings", false, 120)]
        private static void ShowSettings()
        {
            ShaderPrewarmingSettingsWindow.ShowWindow();
        }

        /// <summary>
        /// Main prewarming logic
        /// </summary>
        private static void PrewarmShaders(bool showDialog)
        {
            float startTime = Time.realtimeSinceStartup;
            int totalWarmed = 0;

            // Method 1: Prewarm ShaderVariantCollection if exists
            ShaderVariantCollection collection = FindShaderVariantCollection();
            if (collection != null)
            {
                PrewarmShaderVariantCollection(collection);
                totalWarmed += collection.variantCount;
            }

            // Method 2: Auto-find and prewarm all Natane Toon Shader materials
            if (EditorPrefs.GetBool(AUTO_FIND_ENABLED_KEY, true))
            {
                int materialCount = PrewarmAllNataneToonMaterials();
                totalWarmed += materialCount;
            }

            float elapsedTime = Time.realtimeSinceStartup - startTime;

            string message = $"[Natane Toon] Shader prewarming completed!\n" +
                           $"Time: {elapsedTime:F3} seconds\n" +
                           $"Variants/Materials warmed: {totalWarmed}";

            Debug.Log(message);

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Shader Prewarming Complete", message, "OK");
            }
        }

        /// <summary>
        /// Prewarm a ShaderVariantCollection
        /// </summary>
        private static void PrewarmShaderVariantCollection(ShaderVariantCollection collection)
        {
            if (collection == null) return;

            Debug.Log($"[Natane Toon] Prewarming ShaderVariantCollection: {collection.name}\n" +
                     $"Shader Count: {collection.shaderCount}\n" +
                     $"Variant Count: {collection.variantCount}");

            collection.WarmUp();
        }

        /// <summary>
        /// Find all materials using Natane Toon Shader and prewarm them
        /// </summary>
        private static int PrewarmAllNataneToonMaterials()
        {
            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
            int count = 0;

            foreach (string guid in materialGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && material.shader != null)
                {
                    string shaderName = material.shader.name;

                    // Check if it's a Natane Toon Shader
                    if (shaderName.Contains("Natane") && shaderName.Contains("Toon"))
                    {
                        // Prewarm by forcing shader compilation
                        material.shader.WarmupAllShaders();
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                Debug.Log($"[Natane Toon] Prewarmed {count} Natane Toon materials");
            }

            return count;
        }

        /// <summary>
        /// Find ShaderVariantCollection asset
        /// </summary>
        private static ShaderVariantCollection FindShaderVariantCollection()
        {
            // Try to find at the default path first
            ShaderVariantCollection collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(SHADER_VARIANT_PATH);

            if (collection != null)
            {
                return collection;
            }

            // If not found, search all ShaderVariantCollections
            string[] collectionGUIDs = AssetDatabase.FindAssets("t:ShaderVariantCollection");

            foreach (string guid in collectionGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);

                if (collection != null && collection.name.Contains("NataneToon"))
                {
                    Debug.Log($"[Natane Toon] Found ShaderVariantCollection at: {path}");
                    return collection;
                }
            }

            Debug.LogWarning("[Natane Toon] No ShaderVariantCollection found. Create one at: " + SHADER_VARIANT_PATH);
            return null;
        }
    }

    /// <summary>
    /// Settings window for Shader Prewarming
    /// </summary>
    public class ShaderPrewarmingSettingsWindow : EditorWindow
    {
        private const string PREWARM_ENABLED_KEY = "NataneToon_PrewarmOnBuild";
        private const string AUTO_FIND_ENABLED_KEY = "NataneToon_AutoFindVariants";

        private bool prewarmOnBuild;
        private bool autoFindMaterials;

        public static void ShowWindow()
        {
            var window = GetWindow<ShaderPrewarmingSettingsWindow>("Shader Prewarming Settings");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnEnable()
        {
            prewarmOnBuild = EditorPrefs.GetBool(PREWARM_ENABLED_KEY, true);
            autoFindMaterials = EditorPrefs.GetBool(AUTO_FIND_ENABLED_KEY, true);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Natane Toon Shader Prewarming Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Shader prewarming compiles shader variants in advance to prevent stuttering during gameplay.\n" +
                "This is especially important for VRChat worlds and avatars.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Prewarm on build setting
            EditorGUI.BeginChangeCheck();
            prewarmOnBuild = EditorGUILayout.Toggle(
                new GUIContent(
                    "Prewarm on Build",
                    "Automatically prewarm shaders before building"),
                prewarmOnBuild);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(PREWARM_ENABLED_KEY, prewarmOnBuild);
            }

            EditorGUILayout.Space(5);

            // Auto-find materials setting
            EditorGUI.BeginChangeCheck();
            autoFindMaterials = EditorGUILayout.Toggle(
                new GUIContent(
                    "Auto-Find Materials",
                    "Automatically find and prewarm all Natane Toon materials in the project"),
                autoFindMaterials);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(AUTO_FIND_ENABLED_KEY, autoFindMaterials);
            }

            EditorGUILayout.Space(20);

            // Manual prewarm button
            if (GUILayout.Button("Prewarm Shaders Now", GUILayout.Height(30)))
            {
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayProgressBar("Shader Prewarming", "Prewarming shaders...", 0.5f);

                    try
                    {
                        // Use reflection to call private method
                        var method = typeof(ShaderPrewarmingEditor).GetMethod(
                            "PrewarmShaders",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        method.Invoke(null, new object[] { true });
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                };
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Note: This system is VRChat-safe as it only runs in the Unity Editor, not at runtime.",
                MessageType.None);
        }
    }
}
