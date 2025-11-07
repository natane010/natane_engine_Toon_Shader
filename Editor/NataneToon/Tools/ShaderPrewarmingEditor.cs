using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

namespace NataneToon.Editor
{
    /// <summary>
    /// Editor-only Shader Prewarming for Natane Toon Shader
    /// Natane Toon Shader用のエディタ専用シェーダープリウォーミング
    /// Automatically warms up shader variants before build to prevent runtime compilation stutters
    /// ビルド前にシェーダーバリアントを自動的にウォームアップし、実行時のコンパイルのスタッターを防止
    /// Safe for VRChat - no runtime scripts required
    /// VRChat対応 - ランタイムスクリプト不要
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
                EditorUtility.DisplayDialog(
                    "シェーダープリウォーミング完了 Shader Prewarming Complete",
                    message,
                    "OK");
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
                        Shader.WarmupAllShaders();
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
    /// シェーダープリウォーミング設定ウィンドウ
    /// </summary>
    public class ShaderPrewarmingSettingsWindow : EditorWindow
    {
        private const string PREWARM_ENABLED_KEY = "NataneToon_PrewarmOnBuild";
        private const string AUTO_FIND_ENABLED_KEY = "NataneToon_AutoFindVariants";
        private const string RUNTIME_SCRIPT_PATH = "Assets/Scripts/RuntimeShaderPrewarming.cs";

        private bool prewarmOnBuild;
        private bool autoFindMaterials;
        private bool runtimeScriptExists;
        private Vector2 scrollPosition;

        public static void ShowWindow()
        {
            var window = GetWindow<ShaderPrewarmingSettingsWindow>("シェーダープリウォーミング設定 Shader Prewarming Settings");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        private void OnEnable()
        {
            prewarmOnBuild = EditorPrefs.GetBool(PREWARM_ENABLED_KEY, true);
            autoFindMaterials = EditorPrefs.GetBool(AUTO_FIND_ENABLED_KEY, true);
            CheckRuntimeScriptExists();
        }

        private void CheckRuntimeScriptExists()
        {
            runtimeScriptExists = System.IO.File.Exists(RUNTIME_SCRIPT_PATH);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Natane Toon シェーダープリウォーミング設定 Shader Prewarming Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "シェーダープリウォーミングはシェーダーバリアントを事前にコンパイルしてゲームプレイ中のスタッターを防ぎます。\n" +
                "これはVRChatワールドやアバターにとって特に重要です。\n" +
                "Shader prewarming compiles shader variants in advance to prevent stuttering during gameplay.\n" +
                "This is especially important for VRChat worlds and avatars.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // === Editor-Only Prewarming Section ===
            EditorGUILayout.LabelField("エディタ専用プリウォーミング (VRChat対応) Editor-Only Prewarming (VRChat Safe)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Prewarm on build setting
            EditorGUI.BeginChangeCheck();
            prewarmOnBuild = EditorGUILayout.Toggle(
                new GUIContent(
                    "ビルド時にプリウォーム Prewarm on Build",
                    "ビルド前に自動的にシェーダーをプリウォーム Automatically prewarm shaders before building"),
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
                    "マテリアルを自動検索 Auto-Find Materials",
                    "プロジェクト内のすべてのNatane Toonマテリアルを自動的に検索してプリウォーム Automatically find and prewarm all Natane Toon materials in the project"),
                autoFindMaterials);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(AUTO_FIND_ENABLED_KEY, autoFindMaterials);
            }

            EditorGUILayout.Space(20);

            // Manual prewarm button
            if (GUILayout.Button("今すぐシェーダーをプリウォーム Prewarm Shaders Now", GUILayout.Height(30)))
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
                "注意: このシステムはUnityエディタでのみ実行され、ランタイムでは実行されないため、VRChatに対応しています。\n" +
                "Note: This system is VRChat-safe as it only runs in the Unity Editor, not at runtime.",
                MessageType.None);

            EditorGUILayout.Space(20);
            DrawSeparator();
            EditorGUILayout.Space(20);

            // === Runtime Prewarming Section ===
            EditorGUILayout.LabelField("ランタイムプリウォーミング (VRChat以外のみ) Runtime Prewarming (Non-VRChat Only)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "警告: ランタイムプリウォーミングスクリプトはVRChatでは動作しません！\n" +
                "ランタイムシェーダープリウォーミングが必要な非VRChatプロジェクトのみで有効にしてください。\n" +
                "WARNING: Runtime prewarming scripts DO NOT work in VRChat!\n" +
                "Only enable this for non-VRChat projects where you need runtime shader prewarming.",
                MessageType.Warning);

            EditorGUILayout.Space(10);

            // Runtime script status
            CheckRuntimeScriptExists();

            if (runtimeScriptExists)
            {
                EditorGUILayout.HelpBox(
                    "✓ ランタイムプリウォーミングスクリプトが有効です Runtime prewarming script is ENABLED\n" +
                    "場所 Location: " + RUNTIME_SCRIPT_PATH,
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "✗ ランタイムプリウォーミングスクリプトが無効です Runtime prewarming script is DISABLED",
                    MessageType.None);
            }

            EditorGUILayout.Space(10);

            // Generate/Delete buttons
            using (new EditorGUI.DisabledScope(runtimeScriptExists))
            {
                if (GUILayout.Button("ランタイムプリウォーミングスクリプトを生成 Generate Runtime Prewarming Script", GUILayout.Height(30)))
                {
                    GenerateRuntimeScript();
                }
            }

            EditorGUILayout.Space(5);

            using (new EditorGUI.DisabledScope(!runtimeScriptExists))
            {
                if (GUILayout.Button("ランタイムプリウォーミングスクリプトを削除 Delete Runtime Prewarming Script", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog(
                        "ランタイムスクリプトを削除 Delete Runtime Script",
                        "ランタイムプリウォーミングスクリプトを削除してもよろしいですか？\n" +
                        "Are you sure you want to delete the runtime prewarming script?\n\n" +
                        "削除するファイル This will remove: " + RUNTIME_SCRIPT_PATH,
                        "削除 Delete",
                        "キャンセル Cancel"))
                    {
                        DeleteRuntimeScript();
                    }
                }
            }

            EditorGUILayout.Space(10);

            if (runtimeScriptExists)
            {
                EditorGUILayout.HelpBox(
                    "使用方法: シーン内のGameObjectにRuntimeShaderPrewarmingコンポーネントを追加し、ShaderVariantCollectionを割り当ててください。\n" +
                    "Usage: Add the RuntimeShaderPrewarming component to a GameObject in your scene and assign the ShaderVariantCollection.",
                    MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space(5);
        }

        private void GenerateRuntimeScript()
        {
            string scriptContent = @"using UnityEngine;

/// <summary>
/// Runtime Shader Prewarming Script for Natane Toon Shader
/// Warms up shader variants at runtime to prevent compilation stutters
///
/// WARNING: This script does NOT work in VRChat!
/// For VRChat projects, use the editor-only prewarming system instead.
/// </summary>
public class RuntimeShaderPrewarming : MonoBehaviour
{
    [Header(""Shader Variant Collection"")]
    [Tooltip(""Reference to the ShaderVariantCollection asset"")]
    public ShaderVariantCollection shaderVariants;

    [Header(""Prewarming Options"")]
    [Tooltip(""Warm shaders on Awake (immediate)"")]
    public bool prewarmOnAwake = true;

    [Tooltip(""Warm shaders on Start (delayed)"")]
    public bool prewarmOnStart = false;

    [Tooltip(""Show debug logs"")]
    public bool showDebugLogs = false;

    private void Awake()
    {
        if (prewarmOnAwake && shaderVariants != null)
        {
            PrewarmShaders();
        }
    }

    private void Start()
    {
        if (prewarmOnStart && !prewarmOnAwake && shaderVariants != null)
        {
            PrewarmShaders();
        }
    }

    /// <summary>
    /// Manually prewarm shader variants
    /// Call this method when you want to prewarm shaders at a specific time
    /// </summary>
    public void PrewarmShaders()
    {
        if (shaderVariants == null)
        {
            Debug.LogWarning(""[RuntimeShaderPrewarming] No ShaderVariantCollection assigned!"");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($""[RuntimeShaderPrewarming] Starting shader prewarming...\n"" +
                     $""Shader Count: {shaderVariants.shaderCount}\n"" +
                     $""Variant Count: {shaderVariants.variantCount}"");
        }

        float startTime = Time.realtimeSinceStartup;

        // Warm up all shader variants in the collection
        shaderVariants.WarmUp();

        float elapsedTime = Time.realtimeSinceStartup - startTime;

        if (showDebugLogs)
        {
            Debug.Log($""[RuntimeShaderPrewarming] Shader prewarming completed in {elapsedTime:F3} seconds"");
        }
    }
}
";

            try
            {
                // Ensure directory exists
                string directory = System.IO.Path.GetDirectoryName(RUNTIME_SCRIPT_PATH);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // Write script file
                System.IO.File.WriteAllText(RUNTIME_SCRIPT_PATH, scriptContent);

                // Refresh AssetDatabase
                AssetDatabase.Refresh();

                Debug.Log($"[Natane Toon] Runtime prewarming script generated at: {RUNTIME_SCRIPT_PATH}");
                EditorUtility.DisplayDialog(
                    "スクリプトを生成しました Script Generated",
                    "ランタイムプリウォーミングスクリプトが正常に生成されました！\n" +
                    "Runtime prewarming script has been generated successfully!\n\n" +
                    "場所 Location: " + RUNTIME_SCRIPT_PATH + "\n\n" +
                    "使用方法: シーン内のGameObjectにこのコンポーネントを追加し、ShaderVariantCollectionを割り当ててください。\n" +
                    "Usage: Add this component to a GameObject in your scene and assign the ShaderVariantCollection.",
                    "OK");

                CheckRuntimeScriptExists();

                // Ping the asset in the Project window
                EditorApplication.delayCall += () =>
                {
                    var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(RUNTIME_SCRIPT_PATH);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                    }
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Natane Toon] Failed to generate runtime script: {ex.Message}");
                EditorUtility.DisplayDialog(
                    "エラー Error",
                    "ランタイムスクリプトの生成に失敗しました:\nFailed to generate runtime script:\n" + ex.Message,
                    "OK");
            }
        }

        private void DeleteRuntimeScript()
        {
            try
            {
                if (System.IO.File.Exists(RUNTIME_SCRIPT_PATH))
                {
                    // Delete the script file
                    AssetDatabase.DeleteAsset(RUNTIME_SCRIPT_PATH);
                    AssetDatabase.Refresh();

                    Debug.Log($"[Natane Toon] Runtime prewarming script deleted: {RUNTIME_SCRIPT_PATH}");
                    EditorUtility.DisplayDialog(
                        "スクリプトを削除しました Script Deleted",
                        "ランタイムプリウォーミングスクリプトが正常に削除されました。\nRuntime prewarming script has been deleted successfully.",
                        "OK");

                    CheckRuntimeScriptExists();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Natane Toon] Failed to delete runtime script: {ex.Message}");
                EditorUtility.DisplayDialog(
                    "エラー Error",
                    "ランタイムスクリプトの削除に失敗しました:\nFailed to delete runtime script:\n" + ex.Message,
                    "OK");
            }
        }
    }
}
