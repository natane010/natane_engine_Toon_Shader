using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditorInternal;
using UnityEngine;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    internal static class NataneDependencyInstaller
    {
        private static AddRequest addRequest;
        private static string pendingPackageLabel;
        private static string statusMessage;
        private static MessageType statusType = MessageType.Info;

        internal static bool IsInstallInProgress => addRequest != null;
        internal static bool HasStatusMessage => !string.IsNullOrEmpty(statusMessage);
        internal static string StatusMessage => statusMessage;
        internal static MessageType StatusType => statusType;

        internal static void OpenVccListing(in NataneDependencyInfo dependency)
        {
            Application.OpenURL(dependency.VccListingUrl);
        }

        internal static bool TryStartInstall(in NataneDependencyInfo dependency)
        {
            if (addRequest != null)
            {
                statusMessage = L("別のパッケージインストールが進行中です。", "Another package install is already running.");
                statusType = MessageType.Warning;
                InternalEditorUtility.RepaintAllViews();
                return false;
            }

            pendingPackageLabel = dependency.DisplayName;
            statusMessage = L(
                $"{dependency.DisplayName} を UPM Git 経由でインストールしています...",
                $"Installing {dependency.DisplayName} via UPM Git...");
            statusType = MessageType.Info;

            addRequest = Client.Add(dependency.UpmGitUrl);
            EditorApplication.update -= PollAddRequest;
            EditorApplication.update += PollAddRequest;
            InternalEditorUtility.RepaintAllViews();
            return true;
        }

        private static void PollAddRequest()
        {
            if (addRequest == null || !addRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= PollAddRequest;

            if (addRequest.Status == StatusCode.Success)
            {
                NataneDependencyStatus.RefreshThirdPartyConfigs();
                AssetDatabase.Refresh();
                statusMessage = L(
                    $"{pendingPackageLabel} のインストールが完了しました。",
                    $"{pendingPackageLabel} was installed successfully.");
                statusType = MessageType.Info;
            }
            else
            {
                string errorMessage = addRequest.Error != null
                    ? addRequest.Error.message
                    : L("不明な Package Manager エラーです。", "Unknown package manager error.");
                statusMessage = L(
                    $"{pendingPackageLabel} のインストールに失敗しました: {errorMessage}",
                    $"Failed to install {pendingPackageLabel}: {errorMessage}");
                statusType = MessageType.Error;
            }

            addRequest = null;
            pendingPackageLabel = null;
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
