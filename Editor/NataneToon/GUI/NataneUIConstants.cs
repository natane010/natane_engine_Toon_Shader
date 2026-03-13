using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// エディターツール共通のUI定数定義
    /// ウィンドウサイズ・ボタン高さ・スペーシング・パディングを統一管理する
    /// </summary>
    public static class NataneUIConstants
    {
        // ===== Window Sizes =====
        public const float WINDOW_WIDTH_STANDARD  = 600f;
        public const float WINDOW_HEIGHT_STANDARD = 500f;
        public const float WINDOW_WIDTH_NARROW    = 400f;
        public const float WINDOW_HEIGHT_TALL     = 700f;

        // ===== Button Heights =====
        public const float BUTTON_HEIGHT_SMALL    = 22f;
        public const float BUTTON_HEIGHT_STANDARD = 25f;
        public const float BUTTON_HEIGHT_LARGE    = 30f;

        // ===== Spacing =====
        public const float SPACE_TINY     = 2f;
        public const float SPACE_SMALL    = 5f;
        public const float SPACE_STANDARD = 10f;
        public const float SPACE_LARGE    = 15f;

        // ===== Padding =====
        public const float INDENT_STANDARD = 15f;

        // ===== Tab Bar =====
        public const float TAB_BAR_HEIGHT = 28f;
        public const float TAB_BUTTON_MIN_WIDTH = 80f;
        public const float TAB_BUTTON_PADDING = 4f;
        public const float MATERIAL_FIELD_HEIGHT = 22f;

        // ===== Tab Active Indicator (H-5) =====
        public const float TAB_INDICATOR_HEIGHT = 2f;
        public static readonly Color TAB_ACTIVE_INDICATOR_COLOR = new Color(0.3f, 0.6f, 1f, 1f);

        // ===== Inspector Header =====
        public const float HEADER_LANG_BUTTON_WIDTH = 30f;
        public const float HEADER_ACTION_BUTTON_WIDTH = 50f;
        public const float HEADER_SMALL_BUTTON_HEIGHT = 20f;

        // ===== Search Bar =====
        public const float SEARCH_LABEL_WIDTH = 60f;
        public const float SEARCH_CLEAR_BUTTON_WIDTH = 45f;

        // ===== Toggle Status =====
        public const float TOGGLE_CHECKBOX_WIDTH = 16f;
        public const float TOGGLE_STATUS_ICON_WIDTH = 18f;
    }
}
