using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEngine;

namespace NataneToon.Editor
{
    public static class NataneBuildPreparationService
    {
        private static long lastPreparedIndexTicks;
        private static bool hasPreparedThisSession;

        public static NataneBuildPreparationReport PrepareForBuild(bool forceRefresh = false, bool logSummary = true)
        {
            NataneAssetIndexService.EnsureLoaded();

            var report = new NataneBuildPreparationReport
            {
                buildMode = NataneBuildPolicySettings.instance.BuildMode.ToString(),
                pendingAssetsBefore = NataneAssetIndexService.GetPendingAssetCount()
            };

            try
            {
                bool needsIndexRefresh = forceRefresh || !NataneAssetIndexStore.IndexExists || NataneAssetIndexService.HasPendingWork;
                if (needsIndexRefresh)
                {
                    NataneAssetIndexService.RunSynchronousCatchUp();
                    report.indexRebuilt = true;
                }

                if (!NataneAssetIndexStore.TryLoadIndex(out NataneAssetIndexData data) || data == null)
                {
                    NataneAssetIndexService.RebuildAll(showProgress: false);
                    NataneAssetIndexStore.TryLoadIndex(out data);
                    report.indexRebuilt = true;
                }

                if (data == null)
                {
                    throw new BuildFailedException("Natane asset index could not be created.");
                }

                bool needsManifestRefresh = forceRefresh ||
                                            !NataneAssetIndexStore.ManifestExists ||
                                            !hasPreparedThisSession ||
                                            lastPreparedIndexTicks != data.generatedAtUtcTicks;

                if (needsManifestRefresh)
                {
                    NataneShaderUsageManifest manifest = NataneAssetIndexService.BuildShaderUsageManifest();
                    NataneAssetIndexStore.SaveManifest(manifest);
                    report.manifestRegenerated = true;
                    lastPreparedIndexTicks = data.generatedAtUtcTicks;
                    hasPreparedThisSession = true;
                }

                report.materialCount = data.materials.Count;
                report.prefabCount = data.prefabs.Count;

                if (report.materialCount == 0)
                {
                    report.warnings.Add("No indexed materials were found for Natane build preparation.");
                }
            }
            catch (Exception ex)
            {
                report.errors.Add(ex.Message);
                NataneAssetIndexStore.SaveReport(report);

                if (NataneBuildPolicySettings.instance.BuildMode == NataneBuildMode.Strict)
                {
                    throw;
                }

                Debug.LogWarning($"[Natane Build Prep] {ex.Message}");
                return report;
            }

            NataneAssetIndexStore.SaveReport(report);

            if (logSummary)
            {
                string warningSuffix = report.warnings.Count > 0
                    ? $" warnings={report.warnings.Count}"
                    : string.Empty;
                Debug.Log($"[Natane Build Prep] mode={report.buildMode} indexRebuilt={report.indexRebuilt} manifestRegenerated={report.manifestRegenerated} materials={report.materialCount} prefabs={report.prefabCount}{warningSuffix}");
            }

            return report;
        }

        internal static bool TryLoadManifest(out NataneShaderUsageManifest manifest)
        {
            if (!NataneAssetIndexStore.TryLoadManifest(out manifest) || manifest == null)
            {
                PrepareForBuild(forceRefresh: true, logSummary: false);
                return NataneAssetIndexStore.TryLoadManifest(out manifest) && manifest != null;
            }

            return true;
        }

        public static HashSet<string> LoadKeywordWhitelist()
        {
            if (!TryLoadManifest(out NataneShaderUsageManifest manifest))
            {
                return new HashSet<string>(StringComparer.Ordinal) { string.Empty };
            }

            var whitelist = new HashSet<string>(StringComparer.Ordinal) { string.Empty };
            for (int i = 0; i < manifest.shaders.Count; i++)
            {
                ShaderUsageManifestEntry entry = manifest.shaders[i];
                if (entry.keywordSetKeys == null)
                {
                    continue;
                }

                for (int j = 0; j < entry.keywordSetKeys.Count; j++)
                {
                    whitelist.Add(entry.keywordSetKeys[j] ?? string.Empty);
                }
            }

            return whitelist;
        }

        public static Dictionary<string, List<string[]>> LoadShaderKeywordSets()
        {
            if (!TryLoadManifest(out NataneShaderUsageManifest manifest))
            {
                return new Dictionary<string, List<string[]>>(StringComparer.Ordinal);
            }

            return manifest.shaders.ToDictionary(
                entry => entry.shaderName,
                entry => entry.keywordSetKeys
                    .Select(key => string.IsNullOrEmpty(key) ? Array.Empty<string>() : key.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    .ToList(),
                StringComparer.Ordinal);
        }
    }
}
