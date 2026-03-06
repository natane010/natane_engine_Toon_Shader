using UnityEditor;
using UnityEngine;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    public class NataneDependencySetupWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Natane/VRChat/Natane Dependency Setup";
        private const string AutoOpenPrefsKey = "NataneToon.DependencySetup.AutoOpened";
        private static string WindowTitle => L("依存関係セットアップ", "Natane Dependency Setup");

        private Vector2 scrollPosition;

        [MenuItem(MenuPath, false, 61)]
        public static void ShowWindow()
        {
            NataneDependencySetupWindow window = GetWindow<NataneDependencySetupWindow>(WindowTitle);
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        [InitializeOnLoadMethod]
        private static void RegisterAutoOpen()
        {
            EditorApplication.delayCall += TryAutoOpen;
        }

        private static void TryAutoOpen()
        {
            if (EditorPrefs.GetBool(AutoOpenPrefsKey, false))
            {
                return;
            }

            if (!NataneDependencyStatus.IsUnityPackageInstall() ||
                !NataneDependencyStatus.HasRecommendedDependenciesNotInstalled())
            {
                return;
            }

            EditorPrefs.SetBool(AutoOpenPrefsKey, true);
            ShowWindow();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            NataneToonShaderGUIUtility.DrawToolHeader("依存関係セットアップ", "Natane Dependency Setup", nameof(NataneDependencySetupWindow));
            EditorGUILayout.HelpBox(
                L(
                    "VRC Light Volumes / LTCGI は任意の追加パッケージです。\n" +
                    "VCC project では VCC listing から、通常の Unity project では UPM Git から追加できます。",
                    "VRC Light Volumes / LTCGI are optional add-on packages.\n" +
                    "Use the VCC listing for VRChat projects, or UPM Git for standard Unity projects."),
                MessageType.Info);

            DrawProjectContext();

            if (NataneDependencyInstaller.HasStatusMessage)
            {
                EditorGUILayout.HelpBox(NataneDependencyInstaller.StatusMessage, NataneDependencyInstaller.StatusType);
            }

            foreach (NataneDependencyInfo dependency in NataneDependencyStatus.GetSupportedDependencies())
            {
                DrawDependencyCard(dependency);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(L("再検出 Re-detect", "Re-detect")))
                {
                    NataneDependencyStatus.RefreshThirdPartyConfigs();
                }

                if (GUILayout.Button(L("ドキュメント Docs", "Docs")))
                {
                    Application.OpenURL("https://github.com/natane010/natane_toon_shader");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawProjectContext()
        {
            NataneInstallRecommendation recommendation = NataneDependencyStatus.GetRecommendedInstallMethod();
            string installModeText = recommendation == NataneInstallRecommendation.Vcc
                ? L("検出した project 種別: VRChat / VCC project", "Detected project type: VRChat / VCC project")
                : L("検出した project 種別: Standard Unity project", "Detected project type: Standard Unity project");

            EditorGUILayout.LabelField(installModeText, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(
                recommendation == NataneInstallRecommendation.Vcc
                    ? L("推奨ルート: VCC listing から追加", "Recommended route: Add packages through VCC listing pages.")
                    : L("推奨ルート: UPM Git URL から追加", "Recommended route: Install packages through UPM Git URLs."),
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);
        }

        private void DrawDependencyCard(in NataneDependencyInfo dependency)
        {
            bool installed = NataneDependencyStatus.IsInstalled(dependency);
            bool compactLayout = position.width < 560f;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(dependency.DisplayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    installed
                        ? L($"導入済み: {dependency.PackageId}", $"Installed: {dependency.PackageId}")
                        : L($"未導入（任意）: {dependency.PackageId}", $"Not installed (optional): {dependency.PackageId}"),
                    EditorStyles.wordWrappedMiniLabel);

                if (compactLayout)
                {
                    if (GUILayout.Button(L("VCC Listing", "VCC Listing")))
                    {
                        NataneDependencyInstaller.OpenVccListing(dependency);
                    }

                    using (new EditorGUI.DisabledScope(installed || NataneDependencyInstaller.IsInstallInProgress))
                    {
                        string installLabel = NataneDependencyInstaller.IsInstallInProgress
                            ? L("インストール中...", "Installing...")
                            : L("UPM Git で追加", "Install via UPM Git");
                        if (GUILayout.Button(installLabel))
                        {
                            NataneDependencyInstaller.TryStartInstall(dependency);
                        }
                    }
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(L("VCC Listing", "VCC Listing")))
                        {
                            NataneDependencyInstaller.OpenVccListing(dependency);
                        }

                        using (new EditorGUI.DisabledScope(installed || NataneDependencyInstaller.IsInstallInProgress))
                        {
                            string installLabel = NataneDependencyInstaller.IsInstallInProgress
                                ? L("インストール中...", "Installing...")
                                : L("UPM Git で追加", "Install via UPM Git");
                            if (GUILayout.Button(installLabel))
                            {
                                NataneDependencyInstaller.TryStartInstall(dependency);
                            }
                        }
                    }
                }

                if (installed)
                {
                    EditorGUILayout.HelpBox(
                        L("検出済みです。実パッケージのパスを使用します。", "Detected and ready. Shader config will use the real package path."),
                        MessageType.Info);
                }
                else
                {
                    string hint = dependency.DisplayName == "LTCGI"
                        ? L("LTCGI は任意です。LTCGI を使うマテリアルがなければ未導入のままで問題ありません。",
                            "LTCGI is optional. If no material uses LTCGI, you can leave it uninstalled.")
                        : L("VRC Light Volumes は任意です。バンドル fallback でも動作しますが、package 版を推奨します。",
                            "VRC Light Volumes is optional. The bundled fallback still works, but the package version is recommended.");
                    EditorGUILayout.HelpBox(hint, MessageType.Warning);
                }
            }
        }
    }
}
