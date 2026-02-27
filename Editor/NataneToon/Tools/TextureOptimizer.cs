using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Texture optimization tool
    /// テクスチャ最適化ツール
    /// Automatically optimizes textures for performance
    /// テクスチャを自動的にパフォーマンス最適化
    /// </summary>
    public class TextureOptimizer : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<Texture2D> texturesToOptimize = new List<Texture2D>();
        private List<OptimizationResult> results = new List<OptimizationResult>();

        // Settings
        private int maxTextureSize = 2048;
        private bool enableCompression = true;
        private bool generateMipmaps = true;
        private TextureImporterCompression compressionQuality = TextureImporterCompression.Compressed;

        private class OptimizationResult
        {
            public Texture2D texture;
            public string issue;
            public string fix;
            public long memoryBefore;
            public long memoryAfter;
        }

        [MenuItem("Tools/Natane/最適化 Optimization/テクスチャ最適化 Texture Optimizer", false, 32)]
        public static void ShowWindow()
        {
            var window = GetWindow<TextureOptimizer>(L("テクスチャ最適化", "Texture Optimizer"));
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            NataneToonShaderGUIUtility.DrawToolHeader("テクスチャ最適化ツール", "Texture Optimizer", "TextureOptimizer");
            EditorGUILayout.HelpBox(
                L("パフォーマンス向上のためにテクスチャを最適化",
                  "Optimize textures for better performance"),
                MessageType.Info);

            EditorGUILayout.Space(10);

            DrawSettings();
            EditorGUILayout.Space(10);
            DrawTextureList();
            EditorGUILayout.Space(10);
            DrawResults();
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("最適化設定", "Optimization Settings"), EditorStyles.boldLabel);

            maxTextureSize = EditorGUILayout.IntPopup(L("最大テクスチャサイズ", "Max Texture Size"), maxTextureSize,
                new[] { "512", "1024", "2048", "4096" },
                new[] { 512, 1024, 2048, 4096 });

            enableCompression = EditorGUILayout.Toggle(L("圧縮を有効化", "Enable Compression"), enableCompression);
            if (enableCompression)
            {
                compressionQuality = (TextureImporterCompression)EditorGUILayout.EnumPopup(L("品質", "Quality"), compressionQuality);
            }

            generateMipmaps = EditorGUILayout.Toggle(L("ミップマップを生成", "Generate Mipmaps"), generateMipmaps);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("選択テクスチャを追加", "Add Selected Textures"))) AddSelectedTextures();
            if (GUILayout.Button(L("プロジェクトをスキャン", "Scan Project"))) ScanProject();
            if (GUILayout.Button(L("クリア", "Clear"))) texturesToOptimize.Clear();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (texturesToOptimize.Count > 0)
            {
                if (GUILayout.Button(L("分析して最適化", "Analyze & Optimize"), GUILayout.Height(30)))
                {
                    AnalyzeAndOptimize();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTextureList()
        {
            if (texturesToOptimize.Count == 0) return;

            EditorGUILayout.LabelField(L($"最適化するテクスチャ ({texturesToOptimize.Count})", $"Textures to Optimize ({texturesToOptimize.Count})"), EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            for (int i = texturesToOptimize.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(texturesToOptimize[i], typeof(Texture2D), false);
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    texturesToOptimize.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawResults()
        {
            if (results.Count == 0) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("最適化結果", "Optimization Results"), EditorStyles.boldLabel);

            long totalSaved = 0;
            foreach (var result in results)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(result.texture.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(L($"問題: {result.issue}", $"Issue: {result.issue}"));
                EditorGUILayout.LabelField(L($"修正: {result.fix}", $"Fix: {result.fix}"));
                if (result.memoryAfter < result.memoryBefore)
                {
                    long saved = result.memoryBefore - result.memoryAfter;
                    EditorGUILayout.LabelField(L($"節約メモリ: {saved / 1024}KB", $"Memory saved: {saved / 1024}KB"));
                    totalSaved += saved;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L($"合計節約メモリ: {totalSaved / (1024 * 1024)}MB", $"Total memory saved: {totalSaved / (1024 * 1024)}MB"), EditorStyles.boldLabel);

            EditorGUILayout.EndVertical();
        }

        private void AddSelectedTextures()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D tex && !texturesToOptimize.Contains(tex))
                {
                    texturesToOptimize.Add(tex);
                }
            }
        }

        private void ScanProject()
        {
            texturesToOptimize.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Texture2D");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null && NeedsOptimization(tex))
                {
                    texturesToOptimize.Add(tex);
                }
            }

            Debug.Log($"[TextureOptimizer] Found {texturesToOptimize.Count} textures needing optimization");
        }

        private bool NeedsOptimization(Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;

            return tex.width > maxTextureSize || tex.height > maxTextureSize ||
                   importer.textureCompression == TextureImporterCompression.Uncompressed;
        }

        private void AnalyzeAndOptimize()
        {
            results.Clear();
            int optimizedCount = 0;

            foreach (var tex in texturesToOptimize)
            {
                if (tex == null) continue;

                string path = AssetDatabase.GetAssetPath(tex);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var result = new OptimizationResult
                {
                    texture = tex,
                    memoryBefore = CalculateTextureMemory(tex, importer)
                };

                bool modified = false;

                // Check size
                if (tex.width > maxTextureSize || tex.height > maxTextureSize)
                {
                    importer.maxTextureSize = maxTextureSize;
                    result.issue = $"Size {tex.width}x{tex.height} exceeds max {maxTextureSize}";
                    result.fix = $"Resized to max {maxTextureSize}";
                    modified = true;
                }

                // Check compression
                if (enableCompression && importer.textureCompression != compressionQuality)
                {
                    importer.textureCompression = compressionQuality;
                    if (string.IsNullOrEmpty(result.issue))
                    {
                        result.issue = "Uncompressed";
                        result.fix = $"Applied {compressionQuality} compression";
                    }
                    modified = true;
                }

                // Mipmaps
                if (importer.mipmapEnabled != generateMipmaps)
                {
                    importer.mipmapEnabled = generateMipmaps;
                    modified = true;
                }

                if (modified)
                {
                    importer.SaveAndReimport();
                    result.memoryAfter = CalculateTextureMemory(tex, importer);
                    results.Add(result);
                    optimizedCount++;
                }
            }

            EditorUtility.DisplayDialog(
                L("最適化完了", "Optimization Complete"),
                L($"{optimizedCount}個のテクスチャを最適化しました", $"Optimized {optimizedCount} textures"),
                "OK");
        }

        private long CalculateTextureMemory(Texture2D tex, TextureImporter importer)
        {
            int width = Mathf.Min(tex.width, importer.maxTextureSize);
            int height = Mathf.Min(tex.height, importer.maxTextureSize);
            int bytesPerPixel = 4;

            if (importer.textureCompression == TextureImporterCompression.Compressed)
            {
                bytesPerPixel = 1; // DXT5 approximation
            }

            return width * height * bytesPerPixel;
        }
    }
}
