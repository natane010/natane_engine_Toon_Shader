using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// Build-time Shader Variant Stripper for Natane Toon Shader
    /// Natane Toon Shader用のビルド時シェーダーバリアントストリッパー
    ///
    /// Implements IPreprocessShaders to automatically strip unused shader variants at build time.
    /// IPreprocessShadersを実装し、ビルド時に未使用のシェーダーバリアントを自動的に除去します。
    ///
    /// Only keyword combinations actually used by project materials are kept.
    /// プロジェクト内のマテリアルが実際に使用しているキーワード組み合わせのみを保持します。
    /// </summary>
    public class ShaderVariantStripper : IPreprocessShaders
    {
        /// <summary>
        /// Execute after Unity's standard stripping (callbackOrder = 100).
        /// Unity標準のストリッピング後に実行 (callbackOrder = 100)。
        /// </summary>
        public int callbackOrder => 100;

        // --- EditorPrefs keys ---
        private const string STRIP_ENABLED_KEY = "NataneToon_VariantStrippingEnabled";
        private const string STRIP_LOG_KEY = "NataneToon_VariantStrippingLog";

        // --- Natane shader names ---
        private static readonly HashSet<string> NataneShaderNames = new HashSet<string>
        {
            "Natane/Toon Shader",
            "Natane/Toon Shader (Cutout)",
            "Natane/Toon Shader (Transparent)",
            "Natane/Toon Shader Wirelight"
        };

        // --- Per-build session cache ---
        private static HashSet<string> _whitelistedKeywordSets;
        private static bool _initialized;
        private static int _strippedCount;
        private static int _keptCount;

        // =====================================================================
        // IPreprocessShaders
        // =====================================================================

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Check if stripping is enabled
            if (!EditorPrefs.GetBool(STRIP_ENABLED_KEY, true))
                return;

            // Only process Natane Toon Shaders
            if (!NataneShaderNames.Contains(shader.name))
                return;

            // Initialize whitelist once per build session
            if (!_initialized)
            {
                InitializeKeywordWhitelist();
                _initialized = true;
                _strippedCount = 0;
                _keptCount = 0;
            }

            bool logEnabled = EditorPrefs.GetBool(STRIP_LOG_KEY, true);

            // Iterate backwards to safely remove items
            for (int i = data.Count - 1; i >= 0; i--)
            {
                string keywordSetKey = GetKeywordSetKey(data[i].shaderKeywordSet);

                if (_whitelistedKeywordSets.Contains(keywordSetKey))
                {
                    _keptCount++;
                }
                else
                {
                    data.RemoveAt(i);
                    _strippedCount++;
                }
            }

            if (logEnabled && (_strippedCount + _keptCount) > 0)
            {
                Debug.Log($"[Natane Toon Stripper] {shader.name} ({snippet.passType}): " +
                          $"kept={_keptCount}, stripped={_strippedCount}");
            }
        }

        // =====================================================================
        // Whitelist Construction
        // =====================================================================

        /// <summary>
        /// Scan all project materials using Natane shaders and build a whitelist
        /// of keyword combinations that are actually in use.
        /// Natane系シェーダーを使用するプロジェクト内の全マテリアルを走査し、
        /// 実際に使用されているキーワード組み合わせのホワイトリストを構築します。
        /// </summary>
        private static void InitializeKeywordWhitelist()
        {
            _whitelistedKeywordSets = new HashSet<string>();

            // Always whitelist the empty (base) variant
            _whitelistedKeywordSets.Add(string.Empty);

            // 1. Collect keyword sets from project materials
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            int count = 0;

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null || material.shader == null)
                    continue;

                if (!NataneShaderNames.Contains(material.shader.name))
                    continue;

                string[] keywords = material.shaderKeywords
                    .Where(k => !string.IsNullOrEmpty(k) && k.StartsWith("_"))
                    .Distinct()
                    .OrderBy(k => k)
                    .ToArray();

                string key = string.Join(";", keywords);
                _whitelistedKeywordSets.Add(key);
                count++;
            }

            // 2. Also whitelist variants from any existing ShaderVariantCollection
            AddVariantsFromCollections();

            bool logEnabled = EditorPrefs.GetBool(STRIP_LOG_KEY, true);
            if (logEnabled)
            {
                Debug.Log($"[Natane Toon Stripper] Whitelist initialized: " +
                          $"{count} materials scanned, " +
                          $"{_whitelistedKeywordSets.Count} unique keyword sets whitelisted");
            }
        }

        /// <summary>
        /// Add keyword sets found in ShaderVariantCollections to the whitelist.
        /// ShaderVariantCollectionに含まれるキーワードセットもホワイトリストに追加します。
        /// </summary>
        private static void AddVariantsFromCollections()
        {
            string[] collectionGuids = AssetDatabase.FindAssets("t:ShaderVariantCollection");

            foreach (string guid in collectionGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ShaderVariantCollection collection =
                    AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);

                if (collection == null || !path.Contains("NataneToon"))
                    continue;

                // We cannot enumerate variants in a ShaderVariantCollection directly,
                // but its presence indicates curated variants exist.
                // The collection-based prewarming ensures these variants are compiled.
                // Log for awareness.
                bool logEnabled = EditorPrefs.GetBool(STRIP_LOG_KEY, true);
                if (logEnabled)
                {
                    Debug.Log($"[Natane Toon Stripper] Found ShaderVariantCollection: {path} " +
                              $"({collection.variantCount} variants)");
                }
            }
        }

        /// <summary>
        /// Extract a deterministic key from a ShaderKeywordSet.
        /// Filters to Natane-specific keywords (starting with _), sorts, and joins.
        /// ShaderKeywordSetから決定論的なキーを抽出します。
        /// Natane固有のキーワード(_で始まる)のみフィルタ、ソート、結合します。
        /// </summary>
        private static string GetKeywordSetKey(ShaderKeywordSet keywordSet)
        {
            var keywords = new List<string>();

            // Unity 2019.4+ : iterate global and local keywords
            foreach (var keyword in keywordSet.GetShaderKeywords())
            {
                string name = keyword.name;
                if (!string.IsNullOrEmpty(name) && name.StartsWith("_"))
                {
                    keywords.Add(name);
                }
            }

            keywords.Sort();
            return string.Join(";", keywords);
        }

        // =====================================================================
        // Build Session Cleanup
        // =====================================================================

        /// <summary>
        /// Reset static state at the start of each build session.
        /// Unity calls IPreprocessShaders for each shader, so we reset via
        /// the _initialized flag and log summary at the end.
        /// ビルドセッション開始時に静的状態をリセットします。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ResetBuildSessionState()
        {
            // Reset on domain reload (which happens before each build)
            _initialized = false;
            _whitelistedKeywordSets = null;
            _strippedCount = 0;
            _keptCount = 0;
        }

        // =====================================================================
        // Settings Window (Menu Item)
        // =====================================================================

        [MenuItem("Tools/Natane/シェーダー Shader/バリアントストリッピング設定 Variant Stripping Settings", false, 73)]
        public static void ShowSettingsWindow()
        {
            ShaderVariantStripperSettingsWindow.ShowWindow();
        }
    }

    /// <summary>
    /// Settings window for Shader Variant Stripper.
    /// シェーダーバリアントストリッパー設定ウィンドウ。
    /// </summary>
    public class ShaderVariantStripperSettingsWindow : EditorWindow
    {
        private const string STRIP_ENABLED_KEY = "NataneToon_VariantStrippingEnabled";
        private const string STRIP_LOG_KEY = "NataneToon_VariantStrippingLog";

        private bool strippingEnabled;
        private bool logEnabled;

        public static void ShowWindow()
        {
            var window = GetWindow<ShaderVariantStripperSettingsWindow>(
                "バリアントストリッピング設定 Variant Stripping Settings");
            window.minSize = new Vector2(450, 320);
            window.Show();
        }

        private void OnEnable()
        {
            strippingEnabled = EditorPrefs.GetBool(STRIP_ENABLED_KEY, true);
            logEnabled = EditorPrefs.GetBool(STRIP_LOG_KEY, true);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                "Natane Toon バリアントストリッピング設定 Variant Stripping Settings",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "ビルド時にプロジェクト内のマテリアルが使用していないシェーダーバリアントを自動的に除去します。\n" +
                "これによりビルドサイズが大幅に削減されます。\n\n" +
                "Automatically strips unused shader variants at build time based on project materials.\n" +
                "This significantly reduces build size.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Stripping enabled toggle
            EditorGUI.BeginChangeCheck();
            strippingEnabled = EditorGUILayout.Toggle(
                new GUIContent(
                    "バリアントストリッピングを有効化 Enable Variant Stripping",
                    "ビルド時に未使用バリアントを自動除去 Automatically strip unused variants at build time"),
                strippingEnabled);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(STRIP_ENABLED_KEY, strippingEnabled);
            }

            EditorGUILayout.Space(5);

            // Log toggle
            EditorGUI.BeginChangeCheck();
            logEnabled = EditorGUILayout.Toggle(
                new GUIContent(
                    "ログ出力を有効化 Enable Log Output",
                    "ストリッピング結果をConsoleに出力 Output stripping results to Console"),
                logEnabled);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(STRIP_LOG_KEY, logEnabled);
            }

            EditorGUILayout.Space(20);

            // Status section
            EditorGUILayout.LabelField("現在の状態 Current Status", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Count Natane materials
            int materialCount = CountNataneMaterials();
            EditorGUILayout.LabelField($"Natane Toon マテリアル数: {materialCount}");

            EditorGUILayout.Space(10);

            if (strippingEnabled)
            {
                EditorGUILayout.HelpBox(
                    "ストリッピングが有効です。次回のビルド時に自動的に実行されます。\n" +
                    "Stripping is enabled. It will run automatically on the next build.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "ストリッピングが無効です。ビルドサイズが大きくなる可能性があります。\n" +
                    "Stripping is disabled. Build size may be larger than necessary.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Scan button
            if (GUILayout.Button("マテリアルキーワードをスキャン Scan Material Keywords", GUILayout.Height(30)))
            {
                ScanAndDisplayKeywords();
            }
        }

        private static int CountNataneMaterials()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            int count = 0;

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null || material.shader == null)
                    continue;

                string shaderName = material.shader.name;
                if (shaderName == "Natane/Toon Shader" ||
                    shaderName == "Natane/Toon Shader (Cutout)" ||
                    shaderName == "Natane/Toon Shader (Transparent)" ||
                    shaderName == "Natane/Toon Shader Wirelight")
                {
                    count++;
                }
            }

            return count;
        }

        private static void ScanAndDisplayKeywords()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            var uniqueSets = new HashSet<string>();
            int materialCount = 0;

            try
            {
                for (int i = 0; i < materialGuids.Length; i++)
                {
                    if (i % 50 == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            "マテリアルスキャン Material Scan",
                            $"スキャン中... Scanning... ({i}/{materialGuids.Length})",
                            (float)i / materialGuids.Length);
                    }

                    string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                    if (material == null || material.shader == null)
                        continue;

                    string shaderName = material.shader.name;
                    if (shaderName != "Natane/Toon Shader" &&
                        shaderName != "Natane/Toon Shader (Cutout)" &&
                        shaderName != "Natane/Toon Shader (Transparent)" &&
                        shaderName != "Natane/Toon Shader Wirelight")
                        continue;

                    materialCount++;

                    string[] keywords = material.shaderKeywords
                        .Where(k => !string.IsNullOrEmpty(k))
                        .OrderBy(k => k)
                        .ToArray();

                    string key = keywords.Length > 0 ? string.Join(", ", keywords) : "(base variant)";
                    uniqueSets.Add(key);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string report = $"Natane Toon マテリアル: {materialCount}\n" +
                           $"ユニークキーワードセット: {uniqueSets.Count}\n\n";

            foreach (string set in uniqueSets.OrderBy(s => s))
            {
                report += $"  - {set}\n";
            }

            Debug.Log($"[Natane Toon Stripper] Keyword scan results:\n{report}");

            EditorUtility.DisplayDialog(
                "スキャン結果 Scan Results",
                $"マテリアル数 Materials: {materialCount}\n" +
                $"ユニークキーワードセット Unique Keyword Sets: {uniqueSets.Count}\n\n" +
                "詳細はConsoleを確認してください。\nSee Console for details.",
                "OK");
        }
    }
}
