using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

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

        private const string PREWARM_ENABLED_KEY = "NataneToon_PrewarmOnBuild";
        private const string AUTO_FIND_ENABLED_KEY = "NataneToon_AutoFindVariants";

        // Natane shader names for filtering
        private static readonly HashSet<string> NataneShaderNames = new HashSet<string>
        {
            "Natane/Toon Shader",
            "Natane/Toon Shader (Cutout)",
            "Natane/Toon Shader (Transparent)",
            "Natane/Toon Shader (Lite)",
            "Natane/Toon Shader (Cutout Lite)",
            "Natane/Toon Shader (Transparent Lite)",
            "Natane/Toon Shader (Fur)",
            "Natane/Toon Shader (Fur Lite)",
            "Natane/Toon Shader (Background)",
            "Natane/Toon Shader Wirelight",
            "Natane/Eye",
            "Natane/Screen FX Overlay"
        };

        /// <summary>
        /// Automatically prewarm shaders before build.
        /// Also auto-collects variants if the collection is empty.
        /// ビルド前にシェーダーを自動プリウォーム。
        /// コレクションが空の場合は自動収集も実行。
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!EditorPrefs.GetBool(PREWARM_ENABLED_KEY, true))
                return;

            Debug.Log("[Natane Toon] Pre-build shader prewarming started...");

            // Auto-collect if collection is empty
            ShaderVariantCollection collection = FindShaderVariantCollection();
            if (collection != null && collection.variantCount == 0)
            {
                Debug.Log("[Natane Toon] コレクションが空です。マテリアルから自動収集します... " +
                          "Collection is empty. Auto-collecting from materials...");
                AutoCollectVariantsFromMaterials(collection);
            }

            PrewarmShaders(false);
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

        [MenuItem("Tools/Natane/シェーダー Shader/シェーダープリウォーミング Shader Prewarming/すべてウォームアップ Prewarm All Shaders", false, 72)]
        private static void PrewarmShadersMenu()
        {
            PrewarmShaders(true);
        }

        [MenuItem("Tools/Natane/シェーダー Shader/シェーダープリウォーミング Shader Prewarming/バリアントコレクションをウォームアップ Prewarm Shader Variant Collection", false, 721)]
        private static void PrewarmShaderVariantCollectionMenu()
        {
            ShaderVariantCollection collection = FindShaderVariantCollection();
            if (collection != null)
            {
                PrewarmShaderVariantCollection(collection);
            }
        }

        [MenuItem("Tools/Natane/シェーダー Shader/シェーダープリウォーミング Shader Prewarming/設定 Settings", false, 722)]
        private static void ShowSettings()
        {
            ShaderPrewarmingSettingsWindow.ShowWindow();
        }

        /// <summary>
        /// Main prewarming logic with progress bar support.
        /// プログレスバー対応のメインプリウォーミングロジック。
        /// </summary>
        private static void PrewarmShaders(bool showDialog)
        {
            float startTime = Time.realtimeSinceStartup;
            int totalWarmed = 0;

            try
            {
                // Stage 1: Prewarm ShaderVariantCollection if exists
                EditorUtility.DisplayProgressBar(
                    L("シェーダープリウォーミング", "Shader Prewarming"),
                    L("ShaderVariantCollectionを検索中...", "Finding ShaderVariantCollection..."),
                    0.1f);

                ShaderVariantCollection collection = FindShaderVariantCollection();
                if (collection != null)
                {
                    EditorUtility.DisplayProgressBar(
                        L("シェーダープリウォーミング", "Shader Prewarming"),
                        L($"コレクションをウォームアップ中... ({collection.variantCount} variants)...",
                        $"Warming up collection ({collection.variantCount} variants)..."),
                        0.3f);

                    PrewarmShaderVariantCollection(collection);
                    totalWarmed += collection.variantCount;
                }

                // Stage 2: Auto-find and prewarm all Natane Toon Shader materials
                if (EditorPrefs.GetBool(AUTO_FIND_ENABLED_KEY, true))
                {
                    EditorUtility.DisplayProgressBar(
                        L("シェーダープリウォーミング", "Shader Prewarming"),
                        L("Natane Toonマテリアルをウォームアップ中...", "Warming up Natane Toon materials..."),
                        0.6f);

                    int materialCount = PrewarmAllNataneToonMaterials();
                    totalWarmed += materialCount;
                }

                EditorUtility.DisplayProgressBar(
                    L("シェーダープリウォーミング", "Shader Prewarming"),
                    L("完了", "Complete!"),
                    1.0f);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            float elapsedTime = Time.realtimeSinceStartup - startTime;

            string message = $"[Natane Toon] Shader prewarming completed!\n" +
                           $"Time: {elapsedTime:F3} seconds\n" +
                           $"Variants/Materials warmed: {totalWarmed}";

            Debug.Log(message);

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    L("シェーダープリウォーミング完了", "Shader Prewarming Complete"),
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
        /// using a temporary ShaderVariantCollection (targeted, not Shader.WarmupAllShaders).
        /// Natane Toon Shaderを使用する全マテリアルを一時ShaderVariantCollectionで
        /// ターゲット指定でプリウォームします。
        /// </summary>
        private static int PrewarmAllNataneToonMaterials()
        {
            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
            int materialCount = 0;
            HashSet<string> uniqueShaderNames = new HashSet<string>();

            // Create a temporary ShaderVariantCollection for targeted warmup
            ShaderVariantCollection tempCollection = new ShaderVariantCollection();

            foreach (string guid in materialGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null || material.shader == null)
                    continue;

                if (!NataneShaderNames.Contains(material.shader.name))
                    continue;

                materialCount++;
                uniqueShaderNames.Add(material.shader.name);

                // Add base variant for this shader
                var variant = new ShaderVariantCollection.ShaderVariant
                {
                    shader = material.shader,
                    passType = PassType.ForwardBase,
                    keywords = new string[] { }
                };

                if (!tempCollection.Contains(variant))
                {
                    tempCollection.Add(variant);
                }
            }

            // Warm up only Natane Toon shader variants
            if (materialCount > 0)
            {
                tempCollection.WarmUp();
                Debug.Log($"[Natane Toon] Prewarmed {materialCount} Natane Toon materials " +
                          $"across {uniqueShaderNames.Count} shaders (targeted warmup)");
            }

            return materialCount;
        }

        /// <summary>
        /// Dynamically find the ShaderVariantCollection asset.
        /// Searches multiple known paths to support both Assets and UPM package layouts.
        /// ShaderVariantCollectionアセットを動的に検索します。
        /// Assets配置とUPMパッケージ配置の両方をサポートします。
        /// </summary>
        private static ShaderVariantCollection FindShaderVariantCollection()
        {
            string resolvedPath = FindShaderVariantPath();

            if (!string.IsNullOrEmpty(resolvedPath))
            {
                ShaderVariantCollection collection =
                    AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(resolvedPath);
                if (collection != null)
                    return collection;
            }

            // Fallback: search all ShaderVariantCollections by name
            string[] collectionGUIDs = AssetDatabase.FindAssets("t:ShaderVariantCollection");

            foreach (string guid in collectionGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ShaderVariantCollection collection =
                    AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);

                if (collection != null && collection.name.Contains("NataneToon"))
                {
                    Debug.Log($"[Natane Toon] Found ShaderVariantCollection at: {path}");
                    return collection;
                }
            }

            Debug.LogWarning("[Natane Toon] No ShaderVariantCollection found. " +
                           "Use Tools > Natane > Shader > Shader Variant Collector to create one.");
            return null;
        }

        /// <summary>
        /// Dynamically resolve the ShaderVariantCollection file path.
        /// Checks AssetDatabase search first, then known fallback paths.
        /// ShaderVariantCollectionファイルパスを動的に解決します。
        /// まずAssetDatabase検索、次に既知のフォールバックパスを確認します。
        /// </summary>
        private static string FindShaderVariantPath()
        {
            // 1. Search via AssetDatabase
            string[] guids = AssetDatabase.FindAssets("NataneToonShaderVariants t:ShaderVariantCollection");
            if (guids.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }

            // 2. Check known fallback paths
            string[] knownPaths = new string[]
            {
                "Assets/natane_engine_Toon_Shader/ShaderVariants/NataneToonShaderVariants.shadervariants",
                "Packages/com.natane.toonshader/ShaderVariants/NataneToonShaderVariants.shadervariants",
                "Assets/ShaderVariants/NataneToonShaderVariants.shadervariants",
            };

            foreach (string path in knownPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path) != null)
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Shader pass type configuration for auto-collection.
        /// 自動収集用のシェーダーパスタイプ設定。
        /// </summary>
        private struct ShaderPassConfig
        {
            public string name;
            public bool hasForwardAdd;
            public bool hasShadowCaster;
            public bool hasMeta;

            public ShaderPassConfig(string name, bool hasForwardAdd, bool hasShadowCaster, bool hasMeta = false)
            {
                this.name = name;
                this.hasForwardAdd = hasForwardAdd;
                this.hasShadowCaster = hasShadowCaster;
                this.hasMeta = hasMeta;
            }
        }

        private static readonly ShaderPassConfig[] AllShaderConfigs = new ShaderPassConfig[]
        {
            new ShaderPassConfig("Natane/Toon Shader",                   true,  true),
            new ShaderPassConfig("Natane/Toon Shader (Cutout)",           true,  true),
            new ShaderPassConfig("Natane/Toon Shader (Transparent)",      true,  false),
            new ShaderPassConfig("Natane/Toon Shader (Lite)",             true,  true),
            new ShaderPassConfig("Natane/Toon Shader (Cutout Lite)",      true,  true),
            new ShaderPassConfig("Natane/Toon Shader (Transparent Lite)", true,  false),
            new ShaderPassConfig("Natane/Toon Shader (Fur)",              true,  true),
            new ShaderPassConfig("Natane/Toon Shader (Fur Lite)",         true,  true),
            new ShaderPassConfig("Natane/Toon Shader (Background)",       true,  true, true),
            new ShaderPassConfig("Natane/Toon Shader Wirelight",          false, false),
            new ShaderPassConfig("Natane/Eye",                            false, false),
            new ShaderPassConfig("Natane/Screen FX Overlay",              false, false),
        };

        /// <summary>
        /// Auto-collect variants from project materials into the given collection.
        /// Runs when the collection is empty at build time.
        /// プロジェクトマテリアルからバリアントを自動収集します。
        /// ビルド時にコレクションが空の場合に実行されます。
        /// </summary>
        private static void AutoCollectVariantsFromMaterials(ShaderVariantCollection collection)
        {
            if (collection == null) return;

            // Collect per-shader keyword sets from project materials
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            var perShaderKeywordSets = new Dictionary<string, HashSet<string>>();
            int nataneMaterialCount = 0;

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null || material.shader == null)
                    continue;

                string shaderName = material.shader.name;
                if (!NataneShaderNames.Contains(shaderName))
                    continue;

                nataneMaterialCount++;

                string[] keywords = material.shaderKeywords
                    .Where(k => !string.IsNullOrEmpty(k))
                    .Distinct()
                    .OrderBy(k => k)
                    .ToArray();

                string key = string.Join(";", keywords);
                if (!perShaderKeywordSets.ContainsKey(shaderName))
                    perShaderKeywordSets[shaderName] = new HashSet<string>();

                perShaderKeywordSets[shaderName].Add(key);
            }

            int totalAdded = 0;
            int uniqueKeywordSets = 0;

            foreach (var config in AllShaderConfigs)
            {
                Shader shader = Shader.Find(config.name);
                if (shader == null) continue;

                // Always include base variant
                var keywordSets = new List<string[]> { new string[] { } };

                if (perShaderKeywordSets.ContainsKey(config.name))
                {
                    foreach (string keySet in perShaderKeywordSets[config.name])
                    {
                        if (!string.IsNullOrEmpty(keySet))
                            keywordSets.Add(keySet.Split(';'));
                    }
                }

                uniqueKeywordSets += keywordSets.Count;

                foreach (string[] keywords in keywordSets)
                {
                    totalAdded += AddVariantSafe(collection, shader, PassType.ForwardBase, keywords);
                    if (config.hasForwardAdd)
                        totalAdded += AddVariantSafe(collection, shader, PassType.ForwardAdd, keywords);
                }

                if (config.hasShadowCaster)
                    totalAdded += AddVariantSafe(collection, shader, PassType.ShadowCaster, new string[] { });
                if (config.hasMeta)
                    totalAdded += AddVariantSafe(collection, shader, PassType.Meta, new string[] { });
            }

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Natane Toon] Auto-collected {totalAdded} variants from {nataneMaterialCount} materials " +
                      $"({uniqueKeywordSets} unique keyword sets across all shaders)");
        }

        /// <summary>
        /// Safely add a variant to a collection. Returns 1 if added, 0 if skipped.
        /// </summary>
        private static int AddVariantSafe(ShaderVariantCollection collection, Shader shader,
            PassType passType, string[] keywords)
        {
            if (shader == null || collection == null) return 0;

            var variant = new ShaderVariantCollection.ShaderVariant
            {
                shader = shader,
                passType = passType,
                keywords = keywords
            };

            if (!collection.Contains(variant))
            {
                collection.Add(variant);
                return 1;
            }

            return 0;
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
            var window = GetWindow<ShaderPrewarmingSettingsWindow>(L("シェーダープリウォーミング設定", "Shader Prewarming Settings"));
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
            EditorGUILayout.LabelField(L("Natane Toon シェーダープリウォーミング設定", "Natane Toon Shader Prewarming Settings"), EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                L("シェーダープリウォーミングはシェーダーバリアントを事前にコンパイルしてゲームプレイ中のスタッターを防ぎます。\n" +
                "これはVRChatワールドやアバターにとって特に重要です。",
                "Shader prewarming compiles shader variants in advance to prevent stuttering during gameplay.\n" +
                "This is especially important for VRChat worlds and avatars."),
                MessageType.Info);

            EditorGUILayout.Space(10);

            // === Editor-Only Prewarming Section ===
            EditorGUILayout.LabelField(L("エディタ専用プリウォーミング (VRChat対応)", "Editor-Only Prewarming (VRChat Safe)"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Prewarm on build setting
            EditorGUI.BeginChangeCheck();
            prewarmOnBuild = EditorGUILayout.Toggle(
                new GUIContent(
                    L("ビルド時にプリウォーム", "Prewarm on Build"),
                    L("ビルド前に自動的にシェーダーをプリウォーム", "Automatically prewarm shaders before building")),
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
                    L("マテリアルを自動検索", "Auto-Find Materials"),
                    L("プロジェクト内のすべてのNatane Toonマテリアルを自動的に検索してプリウォーム", "Automatically find and prewarm all Natane Toon materials in the project")),
                autoFindMaterials);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(AUTO_FIND_ENABLED_KEY, autoFindMaterials);
            }

            EditorGUILayout.Space(20);

            // Manual prewarm button
            if (GUILayout.Button(L("今すぐシェーダーをプリウォーム", "Prewarm Shaders Now"), GUILayout.Height(30)))
            {
                EditorApplication.delayCall += () =>
                {
                    // Use reflection to call private method
                    var method = typeof(ShaderPrewarmingEditor).GetMethod(
                        "PrewarmShaders",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    method.Invoke(null, new object[] { true });
                };
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                L("注意: このシステムはUnityエディタでのみ実行され、ランタイムでは実行されないため、VRChatに対応しています。",
                "Note: This system is VRChat-safe as it only runs in the Unity Editor, not at runtime."),
                MessageType.None);

            EditorGUILayout.Space(20);
            DrawSeparator();
            EditorGUILayout.Space(20);

            // === Runtime Prewarming Section ===
            EditorGUILayout.LabelField(L("ランタイムプリウォーミング (VRChat以外のみ)", "Runtime Prewarming (Non-VRChat Only)"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                L("警告: ランタイムプリウォーミングスクリプトはVRChatでは動作しません！\n" +
                "ランタイムシェーダープリウォーミングが必要な非VRChatプロジェクトのみで有効にしてください。",
                "WARNING: Runtime prewarming scripts DO NOT work in VRChat!\n" +
                "Only enable this for non-VRChat projects where you need runtime shader prewarming."),
                MessageType.Warning);

            EditorGUILayout.Space(10);

            // Runtime script status
            CheckRuntimeScriptExists();

            if (runtimeScriptExists)
            {
                EditorGUILayout.HelpBox(
                    L("ランタイムプリウォーミングスクリプトが有効です", "Runtime prewarming script is ENABLED") + "\n" +
                    L("場所", "Location") + ": " + RUNTIME_SCRIPT_PATH,
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    L("ランタイムプリウォーミングスクリプトが無効です", "Runtime prewarming script is DISABLED"),
                    MessageType.None);
            }

            EditorGUILayout.Space(10);

            // Generate/Delete buttons
            using (new EditorGUI.DisabledScope(runtimeScriptExists))
            {
                if (GUILayout.Button(L("ランタイムプリウォーミングスクリプトを生成", "Generate Runtime Prewarming Script"), GUILayout.Height(30)))
                {
                    GenerateRuntimeScript();
                }
            }

            EditorGUILayout.Space(5);

            using (new EditorGUI.DisabledScope(!runtimeScriptExists))
            {
                if (GUILayout.Button(L("ランタイムプリウォーミングスクリプトを削除", "Delete Runtime Prewarming Script"), GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog(
                        L("ランタイムスクリプトを削除", "Delete Runtime Script"),
                        L("ランタイムプリウォーミングスクリプトを削除してもよろしいですか？",
                        "Are you sure you want to delete the runtime prewarming script?") + "\n\n" +
                        L("削除するファイル", "This will remove") + ": " + RUNTIME_SCRIPT_PATH,
                        L("削除", "Delete"),
                        L("キャンセル", "Cancel")))
                    {
                        DeleteRuntimeScript();
                    }
                }
            }

            EditorGUILayout.Space(10);

            if (runtimeScriptExists)
            {
                EditorGUILayout.HelpBox(
                    L("使用方法: シーン内のGameObjectにRuntimeShaderPrewarmingコンポーネントを追加し、ShaderVariantCollectionを割り当ててください。",
                    "Usage: Add the RuntimeShaderPrewarming component to a GameObject in your scene and assign the ShaderVariantCollection."),
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
                    L("スクリプトを生成しました", "Script Generated"),
                    L("ランタイムプリウォーミングスクリプトが正常に生成されました！",
                    "Runtime prewarming script has been generated successfully!") + "\n\n" +
                    L("場所", "Location") + ": " + RUNTIME_SCRIPT_PATH + "\n\n" +
                    L("使用方法: シーン内のGameObjectにこのコンポーネントを追加し、ShaderVariantCollectionを割り当ててください。",
                    "Usage: Add this component to a GameObject in your scene and assign the ShaderVariantCollection."),
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
                    L("エラー", "Error"),
                    L("ランタイムスクリプトの生成に失敗しました:", "Failed to generate runtime script:") + "\n" + ex.Message,
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
                        L("スクリプトを削除しました", "Script Deleted"),
                        L("ランタイムプリウォーミングスクリプトが正常に削除されました。",
                        "Runtime prewarming script has been deleted successfully."),
                        "OK");

                    CheckRuntimeScriptExists();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Natane Toon] Failed to delete runtime script: {ex.Message}");
                EditorUtility.DisplayDialog(
                    L("エラー", "Error"),
                    L("ランタイムスクリプトの削除に失敗しました:", "Failed to delete runtime script:") + "\n" + ex.Message,
                    "OK");
            }
        }
    }
}
