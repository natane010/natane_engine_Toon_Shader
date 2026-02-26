using UnityEditor;

namespace NataneToon.Editor
{
    public static class NataneToonLocalization
    {
        private const string LANG_PREFS_KEY = "NataneToon_Language";
        private static int _language = -1; // -1 = uninitialized

        public static bool IsJapanese
        {
            get
            {
                if (_language < 0)
                    _language = EditorPrefs.GetInt(LANG_PREFS_KEY, 1);
                return _language == 1;
            }
        }

        public static void ToggleLanguage()
        {
            _language = IsJapanese ? 0 : 1;
            EditorPrefs.SetInt(LANG_PREFS_KEY, _language);
        }

        /// <summary>
        /// Returns ja when Japanese, en when English.
        /// Single bool branch, zero allocation, no dictionary lookup.
        /// </summary>
        public static string L(string ja, string en)
        {
            return IsJapanese ? ja : en;
        }
    }
}
