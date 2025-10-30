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

        [MenuItem("Tools/Natane/Shader Prewarming/すべてのシェーダーをプリウォーム", false, 100)]
        private static void PrewarmShadersMenu()
        {
            PrewarmShaders(true);
        }

        [MenuItem("Tools/Natane/Shader Prewarming/シェーダーバリアントコレクションをプリウォーム", false, 101)]
        private static void PrewarmShaderVariantCollectionMenu()
        {
            ShaderVariantCollection collection = FindShaderVariantCollection();
            if (collection != null)
            {
                PrewarmShaderVariantCollection(collection);
            }
        }

        [MenuItem("Tools/Natane/Shader Prewarming/設定", false, 120)]
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

            string message = $"[Natane Toon] シェーダープリウォーム完了!\n" +
                           $"時間: {elapsedTime:F3} 秒\n" +
                           $"プリウォーム済みバリアント/マテリアル: {totalWarmed}";

            Debug.Log(message);

            if (showDialog)
            {
                EditorUtility.DisplayDialog("シェーダープリウォーム完了", message, "OK");
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
            var window = GetWindow<ShaderPrewarmingSettingsWindow>("シェーダープリウォーム設定");
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
            EditorGUILayout.LabelField("Natane Toon シェーダープリウォーム設定", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "シェーダープリウォームはシェーダーバリアントを事前にコンパイルしてゲームプレイ中のスタッタリングを防ぎます。\n" +
                "これは特にVRChatワールドとアバターに重要です。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // === Editor-Only Prewarming Section ===
            EditorGUILayout.LabelField("エディターのみのプリウォーム (VRChat 対応)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Prewarm on build setting
            EditorGUI.BeginChangeCheck();
            prewarmOnBuild = EditorGUILayout.Toggle(
                new GUIContent(
                    "ビルド時にプリウォーム",
                    "ビルド前に自動的にシェーダーをプリウォームします"),
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
                    "マテリアルを自動検索",
                    "プロジェクト内のすべてのNatane Toonマテリアルを自動検索してプリウォームします"),
                autoFindMaterials);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(AUTO_FIND_ENABLED_KEY, autoFindMaterials);
            }

            EditorGUILayout.Space(20);

            // Manual prewarm button
            if (GUILayout.Button("今すぐシェーダーをプリウォーム", GUILayout.Height(30)))
            {
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayProgressBar("シェーダープリウォーム", "シェーダーをプリウォーム中...", 0.5f);

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
                "注意: このシステムはUnityエディターでのみ実行されるため、VRChat対応です。ランタイムでは実行されません。",
                MessageType.None);

            EditorGUILayout.Space(20);
            DrawSeparator();
            EditorGUILayout.Space(20);

            // === Runtime Prewarming Section ===
            EditorGUILayout.LabelField("ランタイムプリウォーム (VRChat以外のみ)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "警告: ランタイムプリウォームスクリプトはVRChatで動作しません!\n" +
                "ランタイムシェーダープリウォームが必要なVRChat以外のプロジェクトでのみ有効にしてください。",
                MessageType.Warning);

            EditorGUILayout.Space(10);

            // Runtime script status
            CheckRuntimeScriptExists();

            if (runtimeScriptExists)
            {
                EditorGUILayout.HelpBox(
                    "✓ ランタイムプリウォームスクリプトが有効です\n" +
                    "場所: " + RUNTIME_SCRIPT_PATH,
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "✗ ランタイムプリウォームスクリプトが無効です",
                    MessageType.None);
            }

            EditorGUILayout.Space(10);

            // Generate/Delete buttons
            using (new EditorGUI.DisabledScope(runtimeScriptExists))
            {
                if (GUILayout.Button("ランタイムプリウォームスクリプトを生成", GUILayout.Height(30)))
                {
                    GenerateRuntimeScript();
                }
            }

            EditorGUILayout.Space(5);

            using (new EditorGUI.DisabledScope(!runtimeScriptExists))
            {
                if (GUILayout.Button("ランタイムプリウォームスクリプトを削除", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog(
                        "ランタイムスクリプトを削除",
                        "ランタイムプリウォームスクリプトを削除してもよろしいですか?\n\n" +
                        "削除対象: " + RUNTIME_SCRIPT_PATH,
                        "削除",
                        "キャンセル"))
                    {
                        DeleteRuntimeScript();
                    }
                }
            }

            EditorGUILayout.Space(10);

            if (runtimeScriptExists)
            {
                EditorGUILayout.HelpBox(
                    "使い方: RuntimeShaderPrewarmingコンポーネントをシーン内のGameObjectに追加し、ShaderVariantCollectionを割り当ててください。",
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

                Debug.Log($"[Natane Toon] ランタイムプリウォームスクリプトが生成されました: {RUNTIME_SCRIPT_PATH}");
                EditorUtility.DisplayDialog(
                    "スクリプト生成完了",
                    "ランタイムプリウォームスクリプトが正常に生成されました!\n\n" +
                    "場所: " + RUNTIME_SCRIPT_PATH + "\n\n" +
                    "使い方: このコンポーネントをシーン内のGameObjectに追加し、ShaderVariantCollectionを割り当ててください。",
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
                Debug.LogError($"[Natane Toon] ランタイムスクリプトの生成に失敗しました: {ex.Message}");
                EditorUtility.DisplayDialog("エラー", "ランタイムスクリプトの生成に失敗しました:\n" + ex.Message, "OK");
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

                    Debug.Log($"[Natane Toon] ランタイムプリウォームスクリプトが削除されました: {RUNTIME_SCRIPT_PATH}");
                    EditorUtility.DisplayDialog(
                        "スクリプト削除完了",
                        "ランタイムプリウォームスクリプトが正常に削除されました。",
                        "OK");

                    CheckRuntimeScriptExists();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Natane Toon] ランタイムスクリプトの削除に失敗しました: {ex.Message}");
                EditorUtility.DisplayDialog("エラー", "ランタイムスクリプトの削除に失敗しました:\n" + ex.Message, "OK");
            }
        }
    }
}
