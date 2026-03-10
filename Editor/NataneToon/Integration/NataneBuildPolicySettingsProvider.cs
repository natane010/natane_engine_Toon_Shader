using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    internal static class NataneBuildPolicySettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Natane Toon/Scalability", SettingsScope.Project)
            {
                label = "Natane Toon Scalability",
                guiHandler = _ => DrawSettingsGui()
            };
        }

        private static void DrawSettingsGui()
        {
            NataneBuildPolicySettings settings = NataneBuildPolicySettings.instance;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Policy", EditorStyles.boldLabel);
            settings.BuildMode = (NataneBuildMode)EditorGUILayout.EnumPopup("Build Mode", settings.BuildMode);
            settings.AutoIndexInEditor = EditorGUILayout.Toggle("Auto Index In Editor", settings.AutoIndexInEditor);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor UX", EditorStyles.boldLabel);
            settings.PageSize = EditorGUILayout.IntField("Page Size", settings.PageSize);
            settings.IssueSampleLimit = EditorGUILayout.IntField("Issue Sample Limit", settings.IssueSampleLimit);
            settings.EditorUpdateBudgetMs = EditorGUILayout.IntSlider("Index Update Budget (ms)", settings.EditorUpdateBudgetMs, 1, 50);
            settings.FullRebuildDirtyThreshold = EditorGUILayout.IntField("Full Rebuild Threshold", settings.FullRebuildDirtyThreshold);

            EditorGUILayout.Space();
            DrawFolderList("Indexed Material Folders", settings.IndexedMaterialFolders, value => ReplaceFolders(settings.IndexedMaterialFolders, value, true));
            DrawFolderList("Indexed Prefab Folders", settings.IndexedPrefabFolders, value => ReplaceFolders(settings.IndexedPrefabFolders, value, false));

            EditorGUILayout.Space();
            if (GUILayout.Button("Save Settings", GUILayout.Height(28f)))
            {
                settings.SaveSettings();
            }
        }

        private static void DrawFolderList(string label, IReadOnlyList<string> folders, System.Action<List<string>> assign)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            List<string> editable = new List<string>(folders);

            for (int i = 0; i < editable.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                editable[i] = EditorGUILayout.TextField(editable[i]);
                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    editable.RemoveAt(i);
                    assign(editable);
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Folder"))
            {
                editable.Add("Assets");
                assign(editable);
                return;
            }

            assign(editable);
        }

        private static void ReplaceFolders(IReadOnlyList<string> current, List<string> next, bool materialFolders)
        {
            NataneBuildPolicySettings settings = NataneBuildPolicySettings.instance;
            var target = materialFolders ? new List<string>(settings.IndexedMaterialFolders) : new List<string>(settings.IndexedPrefabFolders);
            bool changed = target.Count != next.Count;
            if (!changed)
            {
                for (int i = 0; i < target.Count; i++)
                {
                    if (target[i] != next[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            var serializedObject = new SerializedObject(settings);
            var propertyName = materialFolders ? "indexedMaterialFolders" : "indexedPrefabFolders";
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = next.Count;
            for (int i = 0; i < next.Count; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = next[i];
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            settings.SaveSettings();
        }
    }
}
