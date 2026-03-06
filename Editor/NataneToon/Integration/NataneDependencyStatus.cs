using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    internal enum NataneInstallRecommendation
    {
        None,
        Vcc,
        UpmGit
    }

    internal readonly struct NataneDependencyInfo
    {
        public NataneDependencyInfo(
            string displayName,
            string packageId,
            string assetPath,
            string vccListingUrl,
            string upmGitUrl)
        {
            DisplayName = displayName;
            PackageId = packageId;
            AssetPath = assetPath;
            VccListingUrl = vccListingUrl;
            UpmGitUrl = upmGitUrl;
        }

        public string DisplayName { get; }
        public string PackageId { get; }
        public string AssetPath { get; }
        public string VccListingUrl { get; }
        public string UpmGitUrl { get; }
    }

    internal static class NataneDependencyStatus
    {
        internal static readonly NataneDependencyInfo VRCLightVolumes = new NataneDependencyInfo(
            "VRC Light Volumes",
            "red.sim.lightvolumes",
            "Packages/red.sim.lightvolumes/Shaders/LightVolumes.cginc",
            "https://redsim.github.io/vpmlisting/",
            "https://github.com/REDSIM/VRCLightVolumes.git?path=/Packages/red.sim.lightvolumes");

        internal static readonly NataneDependencyInfo LTCGI = new NataneDependencyInfo(
            "LTCGI",
            "at.pimaker.ltcgi",
            "Packages/at.pimaker.ltcgi/Shaders/LTCGI.cginc",
            "https://vpm.pimaker.at",
            "https://github.com/PiMaker/ltcgi.git");

        internal static NataneDependencyInfo[] GetSupportedDependencies()
        {
            return new[] { VRCLightVolumes, LTCGI };
        }

        internal static bool IsInstalled(in NataneDependencyInfo dependency)
        {
            string packageFolder = $"Packages/{dependency.PackageId}";
            return AssetDatabase.IsValidFolder(packageFolder) &&
                   AssetDatabase.LoadMainAssetAtPath(dependency.AssetPath) != null;
        }

        internal static bool HasRecommendedDependenciesNotInstalled()
        {
            foreach (NataneDependencyInfo dependency in GetSupportedDependencies())
            {
                if (!IsInstalled(dependency))
                {
                    return true;
                }
            }

            return false;
        }

        internal static NataneInstallRecommendation GetRecommendedInstallMethod()
        {
            return IsVrChatProject() ? NataneInstallRecommendation.Vcc : NataneInstallRecommendation.UpmGit;
        }

        internal static bool IsVrChatProject()
        {
            string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return false;
            }

            string manifestJson = File.ReadAllText(manifestPath);
            return manifestJson.IndexOf("\"com.vrchat.", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsUnityPackageInstall()
        {
            return NatanePackagePathResolver.TryResolvePackageRootAssetPath(out string rootAssetPath) &&
                   rootAssetPath.StartsWith("Assets/", StringComparison.Ordinal);
        }

        internal static void RefreshThirdPartyConfigs()
        {
            InvokeDetectorIfAvailable("NataneToon.Editor.VRCLightVolumesAutoDetector");
            InvokeDetectorIfAvailable("NataneToon.Editor.LTCGIAutoDetector");
        }

        private static void InvokeDetectorIfAvailable(string fullTypeName)
        {
            Type detectorType = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => string.Equals(assembly.GetName().Name, "NataneToon.Editor", StringComparison.Ordinal))
                .Select(assembly => assembly.GetType(fullTypeName, false))
                .FirstOrDefault(type => type != null);

            if (detectorType == null)
            {
                Debug.LogWarning($"[NataneToon] Detector type not found: {fullTypeName}");
                return;
            }

            MethodInfo detectMethod = detectorType.GetMethod(
                "DetectAndConfigure",
                BindingFlags.Public | BindingFlags.Static);

            if (detectMethod == null)
            {
                Debug.LogWarning($"[NataneToon] DetectAndConfigure() not found on {fullTypeName}");
                return;
            }

            detectMethod.Invoke(null, null);
        }
    }
}
