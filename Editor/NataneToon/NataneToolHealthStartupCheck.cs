using System;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Startup health check for Natane tool assemblies.
    /// エディタ起動時のNataneツール アセンブリ健全性チェック。
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
                    "Natane Toon Shader - ツール異常検出",
                    "一部のNatane Toon Shaderツールが正常に動作しない可能性があります。\n" +
                    "アセンブリまたは型の解決に問題が検出されました。\n\n" +
                    "Some Natane Toon Shader tools may not function correctly.\n" +
                    "Assembly or type resolution issues were detected.\n\n" +
                    "診断ツールで詳細を確認してください。",
                    "診断ツールを開く Open Diagnostics",
                    "後で確認する Later");

                if (openDiagnostics)
                {
                    NataneToolHealthValidator.ShowWindow();
                }
            }
        }
    }
}
