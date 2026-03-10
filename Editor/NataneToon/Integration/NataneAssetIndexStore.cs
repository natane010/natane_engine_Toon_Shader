using System;
using System.IO;
using UnityEngine;

namespace NataneToon.Editor
{
    internal static class NataneAssetIndexStore
    {
        private const string IndexFileName = "asset-index-v1.json";
        private const string ManifestFileName = "shader-usage-manifest-v1.json";

        private static string ProjectRootPath => Path.GetDirectoryName(Application.dataPath).Replace("\\", "/");
        internal static string IndexPath => Path.Combine(ProjectRootPath, "Library", "NataneToon", "Index", IndexFileName);
        internal static string ManifestPath => Path.Combine(ProjectRootPath, "Library", "NataneToon", "Build", ManifestFileName);
        internal static string ReportDirectoryPath => Path.Combine(ProjectRootPath, "Library", "NataneToon", "Reports");

        internal static bool IndexExists => File.Exists(IndexPath);
        internal static bool ManifestExists => File.Exists(ManifestPath);

        internal static bool TryLoadIndex(out NataneAssetIndexData data)
        {
            data = null;
            if (!IndexExists)
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<NataneAssetIndexData>(File.ReadAllText(IndexPath));
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Natane Asset Index] Failed to load index: {ex.Message}");
                data = null;
                return false;
            }
        }

        internal static void SaveIndex(NataneAssetIndexData data)
        {
            if (data == null)
            {
                return;
            }

            data.generatedAtUtcTicks = DateTime.UtcNow.Ticks;
            EnsureDirectory(Path.GetDirectoryName(IndexPath));
            File.WriteAllText(IndexPath, JsonUtility.ToJson(data, false));
        }

        internal static bool TryLoadManifest(out NataneShaderUsageManifest manifest)
        {
            manifest = null;
            if (!ManifestExists)
            {
                return false;
            }

            try
            {
                manifest = JsonUtility.FromJson<NataneShaderUsageManifest>(File.ReadAllText(ManifestPath));
                return manifest != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Natane Build Prep] Failed to load shader usage manifest: {ex.Message}");
                manifest = null;
                return false;
            }
        }

        internal static void SaveManifest(NataneShaderUsageManifest manifest)
        {
            if (manifest == null)
            {
                return;
            }

            manifest.generatedAtUtcTicks = DateTime.UtcNow.Ticks;
            EnsureDirectory(Path.GetDirectoryName(ManifestPath));
            File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, false));
        }

        internal static void SaveReport(NataneBuildPreparationReport report)
        {
            if (report == null)
            {
                return;
            }

            report.generatedAtUtcTicks = DateTime.UtcNow.Ticks;
            EnsureDirectory(ReportDirectoryPath);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string path = Path.Combine(ReportDirectoryPath, $"build-prep-{timestamp}.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
