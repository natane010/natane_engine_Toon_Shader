using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    internal enum NataneBuildMode
    {
        Passive,
        AutoPrepare,
        Strict
    }

    [FilePath("ProjectSettings/NataneToonBuildPolicy.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NataneBuildPolicySettings : ScriptableSingleton<NataneBuildPolicySettings>
    {
        [SerializeField] private NataneBuildMode buildMode = NataneBuildMode.AutoPrepare;
        [SerializeField] private bool autoIndexInEditor = true;
        [SerializeField] private int pageSize = 100;
        [SerializeField] private int issueSampleLimit = 500;
        [SerializeField] private int editorUpdateBudgetMs = 8;
        [SerializeField] private int fullRebuildDirtyThreshold = 10000;
        [SerializeField] private List<string> indexedMaterialFolders = new List<string> { "Assets" };
        [SerializeField] private List<string> indexedPrefabFolders = new List<string> { "Assets" };

        internal NataneBuildMode BuildMode
        {
            get => buildMode;
            set => buildMode = value;
        }

        internal bool AutoIndexInEditor
        {
            get => autoIndexInEditor;
            set => autoIndexInEditor = value;
        }

        public int PageSize
        {
            get => Mathf.Max(25, pageSize);
            set => pageSize = Mathf.Max(25, value);
        }

        public int IssueSampleLimit
        {
            get => Mathf.Max(50, issueSampleLimit);
            set => issueSampleLimit = Mathf.Max(50, value);
        }

        internal int EditorUpdateBudgetMs
        {
            get => Mathf.Clamp(editorUpdateBudgetMs, 1, 50);
            set => editorUpdateBudgetMs = Mathf.Clamp(value, 1, 50);
        }

        internal int FullRebuildDirtyThreshold
        {
            get => Mathf.Max(1000, fullRebuildDirtyThreshold);
            set => fullRebuildDirtyThreshold = Mathf.Max(1000, value);
        }

        internal IReadOnlyList<string> IndexedMaterialFolders => indexedMaterialFolders;
        internal IReadOnlyList<string> IndexedPrefabFolders => indexedPrefabFolders;

        public string[] GetMaterialSearchFolders()
        {
            return SanitizeFolders(indexedMaterialFolders);
        }

        public string[] GetPrefabSearchFolders()
        {
            return SanitizeFolders(indexedPrefabFolders);
        }

        public void SaveSettings()
        {
            Save(true);
        }

        private static string[] SanitizeFolders(List<string> source)
        {
            var folders = new List<string>();
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    string folder = NormalizeFolder(source[i]);
                    if (!string.IsNullOrEmpty(folder) && !folders.Contains(folder))
                    {
                        folders.Add(folder);
                    }
                }
            }

            if (folders.Count == 0)
            {
                folders.Add("Assets");
            }

            return folders.ToArray();
        }

        private static string NormalizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return null;
            }

            folder = folder.Replace("\\", "/").Trim();
            while (folder.EndsWith("/"))
            {
                folder = folder.Substring(0, folder.Length - 1);
            }

            return folder;
        }
    }
}
