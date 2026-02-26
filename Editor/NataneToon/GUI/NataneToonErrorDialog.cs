using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    /// <summary>
    /// シェーダー切り替え時のエラーダイアログユーティリティ
    /// Utility class for displaying error dialogs during shader switching
    /// </summary>
    public static class NataneToonErrorDialog
    {
        private static string DIALOG_TITLE => L("シェーダーエラー", "Shader Error");

        /// <summary>
        /// 原因と対策を含むエラーダイアログを表示
        /// </summary>
        public static void ShowError(string summary, string cause, string remedy)
        {
            string message = $"{summary}\n\n" +
                             $"{L("【原因】", "[Cause]")}\n{cause}\n\n" +
                             $"{L("【対策】", "[Remedy]")}\n{remedy}";
            EditorUtility.DisplayDialog(DIALOG_TITLE, message, "OK");
        }

        // ===== プリセットメッセージ =====

        public static void ShowShaderNotFoundError(string shaderName)
        {
            ShowError(
                L($"シェーダー '{shaderName}' が見つかりませんでした。",
                  $"Shader '{shaderName}' was not found."),
                L("シェーダーファイルが削除されたか、コンパイルエラーが発生している可能性があります。",
                  "The shader file may have been deleted or a compilation error may have occurred."),
                L("1. Unity コンソールでコンパイルエラーを確認してください\n" +
                  "2. パッケージが正しくインストールされているか確認してください\n" +
                  "3. 問題が続く場合はパッケージを再インポートしてください",
                  "1. Check the Unity Console for compilation errors\n" +
                  "2. Verify the package is correctly installed\n" +
                  "3. Re-import the package if the problem persists")
            );
        }

        public static void ShowNullMaterialError(string operation)
        {
            ShowError(
                L($"マテリアルが無効なため、{operation}を実行できません。",
                  $"Cannot perform '{operation}' because the material is invalid."),
                L("マテリアルの参照が失われました。Inspector が古い状態になっている可能性があります。",
                  "The material reference has been lost. The Inspector may be in a stale state."),
                L("1. マテリアルを再度選択してください\n" +
                  "2. Inspector のロックを解除・再ロックしてください",
                  "1. Re-select the material\n" +
                  "2. Unlock and re-lock the Inspector")
            );
        }
    }
}
