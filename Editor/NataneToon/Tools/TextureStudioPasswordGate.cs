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

    /// <summary>
    /// Simple text input dialog for EditorWindow.
    /// EditorWindow用のシンプルなテキスト入力ダイアログ
    /// </summary>
    internal class EditorInputDialog : EditorWindow
    {
        private string message;
        private string inputText;
        private bool confirmed;
        private bool initialized;
        private static string result;
        private static bool closed;

        public static string Show(string title, string message, string defaultText)
        {
            result = null;
            closed = false;

            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window.message = message;
            window.inputText = defaultText;
            window.minSize = new Vector2(350, 120);
            window.maxSize = new Vector2(350, 120);
            window.ShowModalUtility();

            return closed ? result : null;
        }

        private void OnGUI()
        {
            if (!initialized)
            {
                initialized = true;
                EditorGUI.FocusTextInControl("PasswordField");
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);

            GUI.SetNextControlName("PasswordField");
            inputText = EditorGUILayout.PasswordField(inputText ?? "");

            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("OK", GUILayout.Width(80)))
                {
                    result = inputText;
                    closed = true;
                    Close();
                }
                if (GUILayout.Button("Cancel", GUILayout.Width(80)))
                {
                    result = null;
                    closed = true;
                    Close();
                }
            }

            // Enter key confirms
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                result = inputText;
                closed = true;
                Close();
            }

            // Escape key cancels
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                result = null;
                closed = true;
                Close();
            }
        }
    }
}
