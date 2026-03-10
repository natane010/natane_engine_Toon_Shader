using System;
using UnityEditor;
using UnityEngine;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    /// <summary>
    /// Startup health check for Natane tool assemblies.
    /// 繧ｨ繝・ぅ繧ｿ襍ｷ蜍墓凾縺ｮNatane繝・・繝ｫ 繧｢繧ｻ繝ｳ繝悶Μ蛛･蜈ｨ諤ｧ繝√ぉ繝・け縲・
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
                    L("Natane Toon Shader - Tool Issue Detected", "Natane Toon Shader - Tool Issue Detected"),
                    L("Some Natane Toon Shader tools may not function correctly.\n", "Some Natane Toon Shader tools may not function correctly.\n" +
                      "Assembly or type resolution issues were detected.\n\n" +
                      "Open Diagnostics to review the details."),
                    L("Open Diagnostics", "Open Diagnostics"),
                    L("Later", "Later"));

                if (openDiagnostics)
                {
                    NataneToolHealthValidator.ShowWindow();
                }
            }
        }
    }
}
