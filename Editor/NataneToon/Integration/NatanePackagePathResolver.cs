using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace NataneToon.Editor
{
    public static class NatanePackagePathResolver
    {
        private const string ResolverScriptRelativeAssetPath = "Editor/NataneToon/Integration/NatanePackagePathResolver.cs";
        private const string LightingRelativeAssetPath = "Shaders/NataneToon/Include/Lighting/NataneToonLighting.hlsl";
        private const string ConfigRelativeAssetDirectory = "Shaders/NataneToon/Include/Config";
        private static bool hasCachedPackageRoot;
        private static bool cachedPackageRootResolved;
        private static string cachedRootAssetPath;
        private static string cachedRootFullPath;

        static NatanePackagePathResolver()
        {
            EditorApplication.projectChanged += InvalidateCache;
        }

        public static bool TryResolvePackageAssetPath(string relativeAssetPath, out string assetPath)
        {
            assetPath = null;
            if (!TryResolvePackageRoot(out string rootAssetPath, out _))
            {
                return false;
            }

            assetPath = CombineAssetPath(rootAssetPath, relativeAssetPath);
            return true;
        }

        public static bool TryResolvePackageRootAssetPath(out string rootAssetPath)
        {
            return TryResolvePackageRoot(out rootAssetPath, out _);
        }

        public static bool TryResolveShaderSupportPaths(
            string configFileName,
            out string configAssetPath,
            out string configFullPath,
            out string lightingAssetPath)
        {
            configAssetPath = null;
            configFullPath = null;
            lightingAssetPath = null;

            if (!TryResolvePackageRoot(out string rootAssetPath, out string rootFullPath))
            {
                return false;
            }

            lightingAssetPath = CombineAssetPath(rootAssetPath, LightingRelativeAssetPath);
            configAssetPath = CombineAssetPath(rootAssetPath, ConfigRelativeAssetDirectory, configFileName);
            configFullPath = Path.Combine(rootFullPath, "Shaders", "NataneToon", "Include", "Config", configFileName);
            return true;
        }

        private static bool TryResolvePackageRoot(out string rootAssetPath, out string rootFullPath)
        {
            if (hasCachedPackageRoot)
            {
                rootAssetPath = cachedRootAssetPath;
                rootFullPath = cachedRootFullPath;
                return cachedPackageRootResolved;
            }

            foreach (string guid in AssetDatabase.FindAssets($"{Path.GetFileNameWithoutExtension(ResolverScriptRelativeAssetPath)} t:MonoScript"))
            {
                string scriptAssetPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (!scriptAssetPath.EndsWith(ResolverScriptRelativeAssetPath, StringComparison.Ordinal))
                {
                    continue;
                }

                int rootLength = scriptAssetPath.Length - ResolverScriptRelativeAssetPath.Length - 1;
                if (rootLength <= 0)
                {
                    continue;
                }

                string candidateRoot = scriptAssetPath.Substring(0, rootLength);
                string candidateFullPath = ResolvePhysicalRootPath(candidateRoot);
                if (string.IsNullOrEmpty(candidateFullPath))
                {
                    continue;
                }

                string lightingAssetPath = CombineAssetPath(candidateRoot, LightingRelativeAssetPath);
                if (!AssetExists(lightingAssetPath))
                {
                    continue;
                }

                CachePackageRoot(candidateRoot, candidateFullPath, true);
                rootAssetPath = candidateRoot;
                rootFullPath = candidateFullPath;
                return true;
            }

            CachePackageRoot(null, null, false);
            rootAssetPath = null;
            rootFullPath = null;
            return false;
        }

        private static void InvalidateCache()
        {
            hasCachedPackageRoot = false;
            cachedPackageRootResolved = false;
            cachedRootAssetPath = null;
            cachedRootFullPath = null;
        }

        private static void CachePackageRoot(string rootAssetPath, string rootFullPath, bool resolved)
        {
            hasCachedPackageRoot = true;
            cachedPackageRootResolved = resolved;
            cachedRootAssetPath = rootAssetPath;
            cachedRootFullPath = rootFullPath;
        }

        private static bool AssetExists(string assetPath)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
            {
                return true;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
        }

        private static string ResolvePhysicalRootPath(string assetRootPath)
        {
            if (assetRootPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                string directPath = Path.GetFullPath(assetRootPath);
                return Directory.Exists(directPath) ? directPath : null;
            }

            if (!assetRootPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                return null;
            }

            string resolvedPackagePath = TryGetResolvedPackagePath(assetRootPath);
            if (!string.IsNullOrEmpty(resolvedPackagePath) && Directory.Exists(resolvedPackagePath))
            {
                return resolvedPackagePath;
            }

            return null;
        }

        private static string TryGetResolvedPackagePath(string assetRootPath)
        {
            PackageInfo packageInfo = PackageInfo.FindForAssetPath(assetRootPath);
            return packageInfo?.resolvedPath;
        }

        private static string CombineAssetPath(params string[] segments)
        {
            return string.Join("/", segments).Replace("\\", "/");
        }
    }
}
