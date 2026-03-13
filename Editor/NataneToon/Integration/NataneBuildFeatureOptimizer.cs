using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// lilToon 方式のビルド時機能最適化。
    /// ビルド前: プロジェクト内のマテリアル・AnimationClip をスキャンし、
    ///           実際に使用されている機能だけを #define した NataneToonBuildSettings.hlsl を生成。
    /// ビルド後: デフォルト（全機能有効）に復元。
    /// </summary>
    internal sealed class NataneBuildFeatureOptimizer : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        // キーワード同期 (-100) の後、他のビルド処理の前に実行
        public int callbackOrder => -90;

        private static readonly string BuildSettingsPath = FindBuildSettingsHlslPath();

        private static readonly string DefaultContent =
@"// NataneToonBuildSettings.hlsl
// Auto-managed by NataneBuildFeatureOptimizer.
// During builds, this file is rewritten to contain only the features actually used.
// In the editor (non-build), all features are enabled by default.
// DO NOT EDIT MANUALLY - changes will be overwritten during builds.

#ifndef NATANE_BUILD_SETTINGS_INCLUDED
#define NATANE_BUILD_SETTINGS_INCLUDED

// Default: all features enabled (editor mode).
// During VRChat/platform builds, NataneBuildFeatureOptimizer rewrites this file
// to #define only the features actually referenced by project materials,
// ensuring unused features are stripped even if shader keywords are desynchronized.
#define NATANE_BUILD_ALL_FEATURES

#endif // NATANE_BUILD_SETTINGS_INCLUDED
";

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                var usedFeatures = CollectUsedFeatures();
                WriteBuildSettingsHlsl(usedFeatures);
                ReimportShaders();

                Debug.Log($"[NataneToonShader] ビルド時機能最適化: {usedFeatures.Count}/{NataneShaderKeywordSynchronizer.KeywordMappings.Length} 機能を有効化");
            }
            catch (Exception ex)
            {
                // 最適化失敗時はデフォルト（全機能有効）に戻してビルドを止めない
                Debug.LogWarning($"[NataneToonShader] ビルド時機能最適化に失敗しました。全機能有効で続行します: {ex.Message}");
                RestoreDefaultBuildSettings();
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            RestoreDefaultBuildSettings();
        }

        /// <summary>
        /// プロジェクト内の全 Natane マテリアルと AnimationClip をスキャンし、
        /// 使用されている機能キーワードを収集する。
        /// </summary>
        private static HashSet<string> CollectUsedFeatures()
        {
            var usedKeywords = new HashSet<string>(StringComparer.Ordinal);

            // 1. マテリアルスキャン
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null)
                    continue;

                if (!NataneShaderCatalog.IsNataneShader(material.shader.name))
                    continue;

                foreach (var mapping in NataneShaderKeywordSynchronizer.KeywordMappings)
                {
                    if (!material.HasProperty(mapping.propertyName))
                        continue;

                    if (material.GetFloat(mapping.propertyName) >= 0.5f)
                    {
                        usedKeywords.Add(mapping.keyword);
                    }
                }
            }

            // 2. AnimationClip スキャン
            // アニメーションで機能の ON/OFF を切り替えるケースに対応。
            // いずれかのキーフレームで値 >= 0.5 なら「使用される可能性あり」と判定する。
            string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip");
            foreach (string guid in animGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // FBX 内の AnimationClip はサブアセットとして格納されている
                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));

                foreach (var clip in clips)
                {
                    ScanAnimationClipForFeatures(clip, usedKeywords);
                }
            }

            return usedKeywords;
        }

        /// <summary>
        /// AnimationClip のバインディングをスキャンし、Natane の Toggle プロパティが
        /// アニメーションで有効化されるケースを検出する。
        /// </summary>
        private static void ScanAnimationClipForFeatures(AnimationClip clip, HashSet<string> usedKeywords)
        {
            if (clip == null)
                return;

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                // マテリアルプロパティのアニメーションは "material._PropertyName" 形式
                if (!binding.propertyName.StartsWith("material.", StringComparison.Ordinal))
                    continue;

                string materialPropertyName = binding.propertyName.Substring("material.".Length);

                // KeywordMappings に存在するプロパティか確認
                foreach (var mapping in NataneShaderKeywordSynchronizer.KeywordMappings)
                {
                    if (!string.Equals(materialPropertyName, mapping.propertyName, StringComparison.Ordinal))
                        continue;

                    // いずれかのキーフレームで >= 0.5 なら使用と判定
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve != null)
                    {
                        foreach (var key in curve.keys)
                        {
                            if (key.value >= 0.5f)
                            {
                                usedKeywords.Add(mapping.keyword);
                                break;
                            }
                        }
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// 使用機能のみを #define した NataneToonBuildSettings.hlsl を書き出す。
        /// </summary>
        private static void WriteBuildSettingsHlsl(HashSet<string> usedKeywords)
        {
            string path = BuildSettingsPath;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[NataneToonShader] NataneToonBuildSettings.hlsl が見つかりません。スキップします。");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("// NataneToonBuildSettings.hlsl");
            sb.AppendLine("// Auto-generated by NataneBuildFeatureOptimizer before build.");
            sb.AppendLine("// DO NOT EDIT - this file will be restored after build completes.");
            sb.AppendLine();
            sb.AppendLine("#ifndef NATANE_BUILD_SETTINGS_INCLUDED");
            sb.AppendLine("#define NATANE_BUILD_SETTINGS_INCLUDED");
            sb.AppendLine();
            sb.AppendLine("// Build-time feature defines (only used features are enabled).");

            // キーワードをソートして出力（再現性のため）
            var sortedKeywords = usedKeywords.OrderBy(k => k, StringComparer.Ordinal);
            foreach (string keyword in sortedKeywords)
            {
                // _MATCAP → NATANE_FEATURE_MATCAP
                string featureDefine = "NATANE_FEATURE" + keyword; // keyword already starts with _
                sb.AppendLine($"#define {featureDefine}");
            }

            sb.AppendLine();
            sb.AppendLine("#endif // NATANE_BUILD_SETTINGS_INCLUDED");

            string fullPath = Path.GetFullPath(path);
            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));
        }

        /// <summary>
        /// NataneToonBuildSettings.hlsl をデフォルト（全機能有効）に復元する。
        /// </summary>
        private static void RestoreDefaultBuildSettings()
        {
            string path = BuildSettingsPath;
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                string fullPath = Path.GetFullPath(path);
                File.WriteAllText(fullPath, DefaultContent, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log("[NataneToonShader] ビルド設定を復元しました（全機能有効）");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NataneToonShader] ビルド設定の復元に失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// Natane シェーダーを再インポートして再コンパイルを強制する。
        /// </summary>
        private static void ReimportShaders()
        {
            string path = BuildSettingsPath;
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// NataneToonBuildSettings.hlsl のアセットパスを検索する。
        /// </summary>
        private static string FindBuildSettingsHlslPath()
        {
            string[] guids = AssetDatabase.FindAssets("NataneToonBuildSettings t:TextAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("NataneToonBuildSettings.hlsl", StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            // フォールバック: Glob 検索
            guids = AssetDatabase.FindAssets("NataneToonBuildSettings");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("NataneToonBuildSettings.hlsl", StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return null;
        }

        /// <summary>
        /// ビルドがキャンセルされた場合のフォールバック復元。
        /// エディタ起動時に BuildSettings が非デフォルトのままなら復元する。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void EnsureDefaultOnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                string path = FindBuildSettingsHlslPath();
                if (string.IsNullOrEmpty(path))
                    return;

                string fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                    return;

                string content = File.ReadAllText(fullPath);
                if (!content.Contains("NATANE_BUILD_ALL_FEATURES"))
                {
                    Debug.Log("[NataneToonShader] ビルド設定がビルド状態のまま残っていました。復元します。");
                    RestoreDefaultBuildSettings();
                }
            };
        }
    }
}
