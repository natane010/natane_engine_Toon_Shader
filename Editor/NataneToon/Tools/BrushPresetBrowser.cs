using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    internal static class BrushPresetBrowser
    {
        private static BrushPresetCollection collection;
        private static int selectedIndex = -1;
        private const string PrefsKey = "NataneToon_BrushPresets";

        private static void EnsureLoaded()
        {
            if (collection != null) return;
            string json = EditorPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json))
                collection = new BrushPresetCollection { presets = BrushPreset.CreateDefaultPresets() };
            else
                collection = BrushPresetCollection.FromJson(json);
        }

        private static void Save()
        {
            if (collection == null) return;
            EditorPrefs.SetString(PrefsKey, collection.ToJson());
        }

        public static List<BrushPreset> GetPresets()
        {
            EnsureLoaded();
            return collection.presets;
        }

        /// <summary>
        /// Draw the preset browser grid. Returns true if a preset was applied.
        /// プリセットブラウザグリッドを描画。プリセットが適用された場合trueを返す。
        /// </summary>
        public static bool DrawPresetBrowser(BrushSettings currentSettings)
        {
            EnsureLoaded();
            bool applied = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("プリセット", "Presets"), EditorStyles.boldLabel);

            // Preset grid
            int columns = 3;
            for (int i = 0; i < collection.presets.Count; i++)
            {
                if (i % columns == 0) EditorGUILayout.BeginHorizontal();

                var preset = collection.presets[i];
                bool isSelected = (i == selectedIndex);
                Color prevBg = GUI.backgroundColor;
                if (isSelected) GUI.backgroundColor = new Color(0.45f, 0.67f, 0.96f);

                string label = preset.name;
                if (label.Length > 8) label = label.Substring(0, 7) + "\u2026";

                if (GUILayout.Button(new GUIContent(label, preset.name),
                    GUILayout.Height(28), GUILayout.MinWidth(60)))
                {
                    selectedIndex = i;
                    preset.ApplyTo(currentSettings);
                    applied = true;
                }
                GUI.backgroundColor = prevBg;

                if (i % columns == columns - 1 || i == collection.presets.Count - 1)
                    EditorGUILayout.EndHorizontal();
            }

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("+", L("現在の設定をプリセットとして保存", "Save current settings as preset")),
                GUILayout.Width(25)))
            {
                var newPreset = BrushPreset.CreateFrom(currentSettings,
                    L($"\u30d7\u30ea\u30bb\u30c3\u30c8 {collection.presets.Count + 1}", $"Preset {collection.presets.Count + 1}"));
                collection.presets.Add(newPreset);
                selectedIndex = collection.presets.Count - 1;
                Save();
            }

            GUI.enabled = selectedIndex >= 0 && selectedIndex < collection.presets.Count;
            if (GUILayout.Button(new GUIContent("-", L("選択プリセットを削除", "Delete selected preset")),
                GUILayout.Width(25)))
            {
                collection.presets.RemoveAt(selectedIndex);
                selectedIndex = Mathf.Clamp(selectedIndex, 0, collection.presets.Count - 1);
                Save();
            }

            if (GUILayout.Button(new GUIContent(L("上書", "Save"), L("現在の設定で選択プリセットを上書き", "Overwrite selected preset")),
                GUILayout.Width(48)))
            {
                var updated = BrushPreset.CreateFrom(currentSettings, collection.presets[selectedIndex].name);
                collection.presets[selectedIndex] = updated;
                Save();
            }
            GUI.enabled = true;

            if (GUILayout.Button(new GUIContent(L("初期化", "Reset"), L("デフォルトプリセットに戻す", "Reset to default presets")),
                GUILayout.Width(52)))
            {
                collection.presets = BrushPreset.CreateDefaultPresets();
                selectedIndex = -1;
                Save();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            return applied;
        }
    }
}
