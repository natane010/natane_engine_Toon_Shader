using System;
using UnityEditor;
using UnityEngine;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    /// <summary>
    /// Startup health check for Natane tool assemblies.
    ///
    /// Runs once per day via [InitializeOnLoad] + EditorApplication.delayCall.
    /// Only shows a dialog when critical errors are detected.
    /// </summary>
    [InitializeOnLoad]
    public static class NataneToolHealthStartupCheck
    {
        private const string PREFS_KEY = "NataneToon_LastHealthCheck";

        static NataneToolHealthStartupCheck()
        {
            EditorApplication.delayCall += RunStartupCheck;
        }

        private static void RunStartupCheck()
        {
            if (Application.isBatchMode)
                return;

            // Run once per day only
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastCheck = EditorPrefs.GetString(PREFS_KEY, "");

            if (string.Equals(lastCheck, today, StringComparison.Ordinal))
                return;

            EditorPrefs.SetString(PREFS_KEY, today);

            var health = NataneToolHealthValidator.GetOverallHealth();

            if (health == NataneToolHealthValidator.HealthStatus.Error)
            {
                bool openDiagnostics = EditorUtility.DisplayDialog(
                    L("Natane Toon Shader - ツール異常を検出", "Natane Toon Shader - Tool Issue Detected"),
                    L("一部の Natane Toon Shader ツールが正しく動作しない可能性があります。\n" +
                      "アセンブリまたは型解決の問題を検出しました。\n\n" +
                      "詳細確認のため診断を開きますか？",
                      "Some Natane Toon Shader tools may not function correctly.\n" +
                      "Assembly or type resolution issues were detected.\n\n" +
                      "Open Diagnostics to review the details."),
                    L("診断を開く", "Open Diagnostics"),
                    L("あとで", "Later"));

                if (openDiagnostics)
                {
                    NataneToolHealthValidator.ShowWindow();
                }
            }
        }
    }
}
