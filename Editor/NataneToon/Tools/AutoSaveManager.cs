using UnityEngine;
using UnityEditor;
using System.IO;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Auto-save manager for texture studio projects.
    /// テクスチャスタジオプロジェクトの自動保存マネージャー
    /// </summary>
    internal class AutoSaveManager
    {
        private double lastSaveTime;
        private double lastManualSaveTime;
        private bool enabled = true;
        private float intervalMinutes = 5f;
        private string autoSavePath;
        private bool hasUnsavedChanges;
        private int changeCounter;

        private static GUIStyle statusStyle;

        public bool Enabled { get => enabled; set => enabled = value; }
        public float IntervalMinutes { get => intervalMinutes; set => intervalMinutes = Mathf.Max(1f, value); }
        public bool HasUnsavedChanges => hasUnsavedChanges;
        public double LastManualSaveTime => lastManualSaveTime;

        public AutoSaveManager()
        {
            autoSavePath = Path.Combine(Path.GetTempPath(), "NataneTex_autosave.nataneTex");
            // Note: Do NOT call EditorApplication.timeSinceStartup here.
            // Unity forbids it in ScriptableObject/EditorWindow constructors.
            // lastSaveTime will be initialized on first CheckAutoSave call.
            lastSaveTime = -1;
        }

        /// <summary>
        /// Call when any change is made (brush stroke, fill, filter, etc.)
        /// 変更があった時に呼び出す
        /// </summary>
        public void MarkDirty()
        {
            hasUnsavedChanges = true;
            changeCounter++;
        }

        /// <summary>
        /// Call when manual save is performed.
        /// 手動保存時に呼び出す
        /// </summary>
        public void MarkSaved()
        {
            hasUnsavedChanges = false;
            lastManualSaveTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Check if auto-save should be performed. Call in OnGUI or Update.
        /// 自動保存を実行すべきか確認。OnGUIまたはUpdateで呼び出す。
        /// </summary>
        /// <returns>True if auto-save was triggered.</returns>
        public bool CheckAutoSave(System.Action<string> saveAction)
        {
            if (!enabled || !hasUnsavedChanges || saveAction == null) return false;

            double now = EditorApplication.timeSinceStartup;
            // Initialize lastSaveTime on first call (deferred from constructor)
            if (lastSaveTime < 0) lastSaveTime = now;
            if (now - lastSaveTime < intervalMinutes * 60.0) return false;

            lastSaveTime = now;
            try
            {
                saveAction(autoSavePath);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[TextureStudio] Auto-save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check for auto-save recovery on startup.
        /// 起動時に自動保存リカバリを確認
        /// </summary>
        /// <returns>Path to recovery file if exists, null otherwise.</returns>
        public string CheckRecovery()
        {
            if (File.Exists(autoSavePath))
            {
                var fileInfo = new FileInfo(autoSavePath);
                if ((System.DateTime.Now - fileInfo.LastWriteTime).TotalHours < 24)
                    return autoSavePath;
            }
            return null;
        }

        /// <summary>
        /// Show recovery dialog.
        /// リカバリダイアログを表示
        /// </summary>
        public bool ShowRecoveryDialog()
        {
            string recoveryPath = CheckRecovery();
            if (recoveryPath == null) return false;

            var fileInfo = new FileInfo(recoveryPath);
            string timeStr = fileInfo.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss");

            return EditorUtility.DisplayDialog(
                L("自動保存の復元", "Auto-Save Recovery"),
                L($"未保存の自動バックアップが見つかりました。\n日時: {timeStr}\n\n復元しますか？",
                  $"An unsaved auto-backup was found.\nDate: {timeStr}\n\nRestore it?"),
                L("復元", "Restore"),
                L("破棄", "Discard"));
        }

        /// <summary>
        /// Delete the auto-save file.
        /// 自動保存ファイルを削除
        /// </summary>
        public void ClearAutoSave()
        {
            if (File.Exists(autoSavePath))
                File.Delete(autoSavePath);
        }

        /// <summary>
        /// Draw auto-save status indicator in status bar.
        /// ステータスバーに自動保存状態インジケータを描画
        /// </summary>
        public string GetStatusText()
        {
            if (!enabled) return "";

            if (!hasUnsavedChanges)
                return L("保存済み", "Saved");

            double elapsed = EditorApplication.timeSinceStartup - lastManualSaveTime;
            if (elapsed < 60)
                return L($"変更あり ({elapsed:F0}秒前に保存)", $"Modified (saved {elapsed:F0}s ago)");

            int minutes = Mathf.FloorToInt((float)(elapsed / 60.0));
            return L($"変更あり ({minutes}分前に保存)", $"Modified (saved {minutes}m ago)");
        }

        /// <summary>
        /// Draw auto-save settings UI.
        /// 自動保存設定UIを描画
        /// </summary>
        public void DrawSettingsUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("自動保存", "Auto Save"), EditorStyles.boldLabel);
            enabled = EditorGUILayout.Toggle(L("有効", "Enabled"), enabled);
            if (enabled)
            {
                intervalMinutes = EditorGUILayout.Slider(
                    L("間隔 (分)", "Interval (min)"), intervalMinutes, 1f, 30f);
            }
            EditorGUILayout.EndVertical();
        }
    }
}
