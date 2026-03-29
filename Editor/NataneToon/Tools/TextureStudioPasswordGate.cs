using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// [TEMPORARY] Password gate for Texture Studio pre-release access.
    /// [一時的] テクスチャスタジオのプレリリースアクセス用パスワードゲート
    /// Remove this entire file at official release.
    /// 正式リリース時にこのファイルごと削除してください。
    /// </summary>
    internal static class TextureStudioPasswordGate
    {
        private const string PASSWORD = "natane2025";
        private static bool authenticated;

        /// <summary>
        /// Verify access. Returns true if already authenticated or password is correct.
        /// アクセスを検証。既に認証済みまたはパスワードが正しい場合にtrueを返す。
        /// </summary>
        public static bool Verify()
        {
            if (authenticated) return true;

            string input = EditorInputDialog.Show(
                "Texture Studio",
                "パスワードを入力してください / Enter password:",
                "");

            if (input == null)
                return false; // Cancelled

            if (input == PASSWORD)
            {
                authenticated = true;
                return true;
            }

            EditorUtility.DisplayDialog(
                "Texture Studio",
                "パスワードが正しくありません。\nIncorrect password.",
                "OK");
            return false;
        }

        /// <summary>Reset authentication (e.g., on domain reload).</summary>
        [InitializeOnLoadMethod]
        private static void ResetOnLoad()
        {
            authenticated = false;
        }
    }
}
