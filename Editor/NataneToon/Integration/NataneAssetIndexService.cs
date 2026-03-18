using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    public static class NataneAssetIndexService
    {
        private static NataneAssetIndexData data;
        private static bool loaded;
        private static bool savePending;
        private static bool fullRebuildQueued;

        private static readonly HashSet<string> dirtyMaterialPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> dirtyPrefabPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> deletedMaterialPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> deletedPrefabPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, MaterialIndexEntry> materialsByGuid;
        private static Dictionary<string, MaterialIndexEntry> materialsByPath;

        internal static bool HasPendingWork => fullRebuildQueued || GetPendingAssetCount() > 0;

        internal static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            if (!NataneAssetIndexStore.TryLoadIndex(out data) || data == null)
            {
                data = new NataneAssetIndexData();
            }

            loaded = true;
            RebuildLookups();
        }

        internal static int GetPendingAssetCount()
        {
            return dirtyMaterialPaths.Count + dirtyPrefabPaths.Count + deletedMaterialPaths.Count + deletedPrefabPaths.Count;
        }

        internal static void MarkAssetsDirty(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            EnsureLoaded();

            QueuePaths(importedAssets, deleted: false);
            QueuePaths(movedAssets, deleted: false);
            QueuePaths(deletedAssets, deleted: true);
            QueuePaths(movedFromAssetPaths, deleted: true);

            if (GetPendingAssetCount() >= NataneBuildPolicySettings.instance.FullRebuildDirtyThreshold)
            {
                fullRebuildQueued = true;
            }
        }

        internal static int ProcessPendingWork(int budgetMs)
        {
            EnsureLoaded();

            if (!NataneBuildPolicySettings.instance.AutoIndexInEditor)
            {
                return 0;
            }

            return ProcessPendingWorkInternal(budgetMs, allowFullRebuild: true);
        }

        internal static int RunSynchronousCatchUp()
        {
            EnsureLoaded();
            return ProcessPendingWorkInternal(int.MaxValue, allowFullRebuild: true);
        }

        internal static void RebuildAll(bool showProgress = false)
        {
            EnsureLoaded();
            RebuildAllInternal(showProgress);
            SaveIfNeeded();
        }

        public static IEnumerable<MaterialIndexEntry> EnumerateMaterialEntries(Func<MaterialIndexEntry, bool> predicate = null)
        {
            EnsureLoaded();
            return predicate == null ? data.materials : data.materials.Where(predicate);
        }

        public static List<MaterialIndexEntry> GetMaterialEntriesPage(Func<MaterialIndexEntry, bool> predicate, int pageIndex, int pageSize, out int totalCount)
        {
            EnsureLoaded();
            IEnumerable<MaterialIndexEntry> query = predicate == null ? data.materials : data.materials.Where(predicate);
            totalCount = query.Count();
            return query
                .OrderBy(entry => entry.name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.path, StringComparer.OrdinalIgnoreCase)
                .Skip(Mathf.Max(0, pageIndex) * Mathf.Max(1, pageSize))
                .Take(Mathf.Max(1, pageSize))
                .ToList();
        }

        public static List<PrefabDependencyEntry> GetPrefabsUsingMaterialGuids(ISet<string> materialGuids)
        {
            EnsureLoaded();
            if (materialGuids == null || materialGuids.Count == 0)
            {
                return new List<PrefabDependencyEntry>();
            }

            return data.prefabs
                .Where(entry => entry.materialGuids != null && entry.materialGuids.Any(materialGuids.Contains))
                .OrderBy(entry => entry.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static MaterialIndexEntry TryGetMaterialEntry(string guid)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(guid) || materialsByGuid == null)
            {
                return null;
            }

            materialsByGuid.TryGetValue(guid, out MaterialIndexEntry entry);
            return entry;
        }

        public static Material LoadMaterial(string guid)
        {
            MaterialIndexEntry entry = TryGetMaterialEntry(guid);
            return entry == null ? null : AssetDatabase.LoadAssetAtPath<Material>(entry.path);
        }

        public static Material LoadMaterial(MaterialIndexEntry entry)
        {
            return entry == null ? null : AssetDatabase.LoadAssetAtPath<Material>(entry.path);
        }

        public static GameObject LoadPrefab(PrefabDependencyEntry entry)
        {
            return entry == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(entry.path);
        }

        internal static int CountMaterialsByShaderPrefix(string shaderPrefix)
        {
            EnsureLoaded();
            return data.materials.Count(entry =>
                !string.IsNullOrEmpty(entry.shaderName) &&
                entry.shaderName.StartsWith(shaderPrefix, StringComparison.OrdinalIgnoreCase));
        }

        internal static NataneShaderUsageManifest BuildShaderUsageManifest()
        {
            EnsureLoaded();

            var manifest = new NataneShaderUsageManifest();
            foreach (IGrouping<string, MaterialIndexEntry> group in data.materials
                .Where(entry => entry.isNataneShader)
                .GroupBy(entry => entry.shaderName, StringComparer.Ordinal))
            {
                var entry = new ShaderUsageManifestEntry
                {
                    shaderName = group.Key,
                    keywordSetKeys = group
                        .Select(material => material.keywordSetKey ?? string.Empty)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(key => key, StringComparer.Ordinal)
                        .ToList()
                };

                if (!entry.keywordSetKeys.Contains(string.Empty))
                {
                    entry.keywordSetKeys.Insert(0, string.Empty);
                }

                manifest.shaders.Add(entry);
            }

            manifest.shaders = manifest.shaders
                .OrderBy(entry => entry.shaderName, StringComparer.Ordinal)
                .ToList();
            return manifest;
        }

        private static int ProcessPendingWorkInternal(int budgetMs, bool allowFullRebuild)
        {
            EnsureLoaded();

            if (allowFullRebuild && fullRebuildQueued)
            {
                // 自動トリガーでの全再構築は行わない。インクリメンタル処理にフォールバックする。
                // 全再構築は Tools > Natane > Rebuild Asset Index から手動実行すること。
                fullRebuildQueued = false;
            }

            int processed = 0;
            DateTime start = DateTime.UtcNow;

            while (TryProcessSinglePendingAsset())
            {
                processed++;
                if (budgetMs != int.MaxValue &&
                    (DateTime.UtcNow - start).TotalMilliseconds >= budgetMs)
                {
                    break;
                }
            }

            SaveIfNeeded();
            return processed;
        }

        private static bool TryProcessSinglePendingAsset()
        {
            if (TryTakeNext(deletedMaterialPaths, out string deletedMaterialPath))
            {
                RemoveMaterialByPath(deletedMaterialPath);
                return true;
            }

            if (TryTakeNext(deletedPrefabPaths, out string deletedPrefabPath))
            {
                RemovePrefabByPath(deletedPrefabPath);
                return true;
            }

            if (TryTakeNext(dirtyMaterialPaths, out string dirtyMaterialPath))
            {
                UpsertMaterialEntry(dirtyMaterialPath);
                return true;
            }

            if (TryTakeNext(dirtyPrefabPaths, out string dirtyPrefabPath))
            {
                UpsertPrefabEntry(dirtyPrefabPath);
                return true;
            }

            return false;
        }

        private static void QueuePaths(string[] paths, bool deleted)
        {
            if (paths == null)
            {
                return;
            }

            for (int i = 0; i < paths.Length; i++)
            {
                string path = NormalizeAssetPath(paths[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (IsMaterialPath(path))
                {
                    if (deleted)
                    {
                        deletedMaterialPaths.Add(path);
                        dirtyMaterialPaths.Remove(path);
                    }
                    else if (IsPathInMaterialScope(path))
                    {
                        dirtyMaterialPaths.Add(path);
                        deletedMaterialPaths.Remove(path);
                    }
                }
                else if (IsPrefabPath(path))
                {
                    if (deleted)
                    {
                        deletedPrefabPaths.Add(path);
                        dirtyPrefabPaths.Remove(path);
                    }
                    else if (IsPathInPrefabScope(path))
                    {
                        dirtyPrefabPaths.Add(path);
                        deletedPrefabPaths.Remove(path);
                    }
                }
            }
        }

        private static void RebuildAllInternal(bool showProgress)
        {
            string[] materialFolders = NataneBuildPolicySettings.instance.GetMaterialSearchFolders();
            string[] prefabFolders = NataneBuildPolicySettings.instance.GetPrefabSearchFolders();

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", materialFolders);
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", prefabFolders);

            var rebuiltData = new NataneAssetIndexData();
            int total = materialGuids.Length + prefabGuids.Length;
            int progress = 0;

            try
            {
                for (int i = 0; i < materialGuids.Length; i++)
                {
                    if (showProgress && !Application.isBatchMode)
                    {
                        EditorUtility.DisplayProgressBar("Natane Asset Index", $"Indexing materials... ({i + 1}/{materialGuids.Length})", total == 0 ? 0f : (float)progress / total);
                    }

                    string path = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(materialGuids[i]));
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    MaterialIndexEntry entry = CreateMaterialEntry(material, path);
                    if (entry != null)
                    {
                        rebuiltData.materials.Add(entry);
                    }
                    progress++;
                }

                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    if (showProgress && !Application.isBatchMode)
                    {
                        EditorUtility.DisplayProgressBar("Natane Asset Index", $"Indexing prefabs... ({i + 1}/{prefabGuids.Length})", total == 0 ? 1f : (float)progress / total);
                    }

                    string path = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(prefabGuids[i]));
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    PrefabDependencyEntry entry = CreatePrefabEntry(prefab, path);
                    if (entry != null)
                    {
                        rebuiltData.prefabs.Add(entry);
                    }
                    progress++;
                }
            }
            finally
            {
                if (showProgress && !Application.isBatchMode)
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            data = rebuiltData;
            savePending = true;
            fullRebuildQueued = false;
            dirtyMaterialPaths.Clear();
            dirtyPrefabPaths.Clear();
            deletedMaterialPaths.Clear();
            deletedPrefabPaths.Clear();
            RebuildLookups();
        }

        private static void UpsertMaterialEntry(string path)
        {
            path = NormalizeAssetPath(path);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!IsPathInMaterialScope(path) || AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(Material))
            {
                RemoveMaterialByPath(path);
                return;
            }

            MaterialIndexEntry entry = CreateMaterialEntry(AssetDatabase.LoadAssetAtPath<Material>(path), path);
            RemoveMaterialByPath(path);
            if (entry != null)
            {
                data.materials.Add(entry);
                savePending = true;
                RebuildLookups();
            }
        }

        private static void UpsertPrefabEntry(string path)
        {
            path = NormalizeAssetPath(path);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!IsPathInPrefabScope(path) || AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(GameObject))
            {
                RemovePrefabByPath(path);
                return;
            }

            PrefabDependencyEntry entry = CreatePrefabEntry(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
            RemovePrefabByPath(path);
            if (entry != null)
            {
                data.prefabs.Add(entry);
                savePending = true;
            }
        }

        private static MaterialIndexEntry CreateMaterialEntry(Material material, string path)
        {
            if (material == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string shaderName = material.shader != null ? material.shader.name : string.Empty;
            return new MaterialIndexEntry
            {
                guid = guid,
                path = path,
                name = material.name,
                shaderName = shaderName,
                keywordSetKey = GetKeywordSetKey(material.shaderKeywords),
                isNataneShader = NataneShaderCatalog.IsNataneShader(shaderName),
                isLilToonShader = NataneShaderCatalog.IsLilToonShader(shaderName),
                usesLightVolume = material.HasProperty("_UseLightVolume") && material.GetFloat("_UseLightVolume") > 0.5f,
                usesLtcgi = material.HasProperty("_LTCGI") && material.GetFloat("_LTCGI") > 0.5f
            };
        }

        private static PrefabDependencyEntry CreatePrefabEntry(GameObject prefab, string path)
        {
            if (prefab == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var materialGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (material == null)
                    {
                        continue;
                    }

                    string materialPath = AssetDatabase.GetAssetPath(material);
                    string materialGuid = AssetDatabase.AssetPathToGUID(materialPath);
                    if (!string.IsNullOrEmpty(materialGuid))
                    {
                        materialGuids.Add(materialGuid);
                    }
                }
            }

            return new PrefabDependencyEntry
            {
                guid = guid,
                path = path,
                name = prefab.name,
                materialGuids = materialGuids.OrderBy(value => value, StringComparer.Ordinal).ToList()
            };
        }

        private static void RemoveMaterialByPath(string path)
        {
            if (data.materials.RemoveAll(entry => string.Equals(entry.path, path, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                savePending = true;
                RebuildLookups();
            }
        }

        private static void RemovePrefabByPath(string path)
        {
            if (data.prefabs.RemoveAll(entry => string.Equals(entry.path, path, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                savePending = true;
            }
        }

        private static void SaveIfNeeded()
        {
            if (!savePending)
            {
                return;
            }

            NataneAssetIndexStore.SaveIndex(data);
            savePending = false;
        }

        private static void RebuildLookups()
        {
            materialsByGuid = data.materials
                .Where(entry => !string.IsNullOrEmpty(entry.guid))
                .GroupBy(entry => entry.guid, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

            materialsByPath = data.materials
                .Where(entry => !string.IsNullOrEmpty(entry.path))
                .GroupBy(entry => entry.path, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
        }

        private static string GetKeywordSetKey(string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
            {
                return string.Empty;
            }

            return string.Join(";",
                keywords
                    .Where(keyword => !string.IsNullOrEmpty(keyword))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(keyword => keyword, StringComparer.Ordinal));
        }

        private static bool TryTakeNext(HashSet<string> source, out string value)
        {
            if (source.Count > 0)
            {
                value = source.First();
                source.Remove(value);
                return true;
            }

            value = null;
            return false;
        }

        private static bool IsMaterialPath(string path)
        {
            return path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPrefabPath(string path)
        {
            return path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPathInMaterialScope(string path)
        {
            return IsPathInFolders(path, NataneBuildPolicySettings.instance.GetMaterialSearchFolders());
        }

        private static bool IsPathInPrefabScope(string path)
        {
            return IsPathInFolders(path, NataneBuildPolicySettings.instance.GetPrefabSearchFolders());
        }

        private static bool IsPathInFolders(string path, string[] folders)
        {
            path = NormalizeAssetPath(path);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            for (int i = 0; i < folders.Length; i++)
            {
                string folder = NormalizeAssetPath(folders[i]);
                if (string.IsNullOrEmpty(folder))
                {
                    continue;
                }

                if (string.Equals(path, folder, StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? null : path.Replace("\\", "/");
        }
    }
}
