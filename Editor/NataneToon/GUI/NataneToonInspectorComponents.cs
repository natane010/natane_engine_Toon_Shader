using UnityEditor;
using UnityEngine;
using static NataneToon.Editor.NataneToonLocalization;

namespace NataneToon.Editor
{
    public enum NataneInspectorStatus
    {
        Neutral,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Natane系インスペクターで共有する表示部品。
    /// Unity標準の描画を基礎にして、テーマやDPIが変わっても視認性を保つ。
    /// </summary>
    public static class NataneToonInspectorComponents
    {
        private static GUIStyle _titleStyle;
        private static GUIStyle _subtitleStyle;
        private static GUIStyle _chipStyle;
        private static GUIStyle _groupStyle;
        private static GUIStyle _inlineMessageStyle;
        private static GUIStyle _captionStyle;
        private static GUIStyle _hintStyle;
        private static bool _cachedDarkTheme;

        private static void EnsureStyles()
        {
            bool dark = EditorGUIUtility.isProSkin;
            if (_titleStyle != null && _cachedDarkTheme == dark) return;

            _cachedDarkTheme = dark;
            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            _titleStyle.normal.textColor = NataneToonColorPalette.PrimaryText;

            _subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            _subtitleStyle.normal.textColor = NataneToonColorPalette.SecondaryText;

            _chipStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(7, 7, 1, 1),
                clipping = TextClipping.Clip
            };

            _groupStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 0, 0, 0)
            };
            _groupStyle.normal.textColor = NataneToonColorPalette.SecondaryText;

            _inlineMessageStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                padding = new RectOffset(8, 8, 5, 5)
            };
            _inlineMessageStyle.normal.textColor = NataneToonColorPalette.SecondaryText;

            // 折りたたみ中に出す一行説明。控えめな色でスキャンの邪魔をしない。
            _captionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(9, 6, 0, 2)
            };
            _captionStyle.normal.textColor = new Color(
                NataneToonColorPalette.SecondaryText.r,
                NataneToonColorPalette.SecondaryText.g,
                NataneToonColorPalette.SecondaryText.b,
                dark ? 0.72f : 0.78f);

            // プロパティ下の「推奨: ...」ヒント。青系で目に留まりやすく、でも主張しすぎない。
            _hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            _hintStyle.normal.textColor = dark
                ? new Color(0.55f, 0.78f, 1f, 0.9f)
                : new Color(0.18f, 0.42f, 0.72f, 1f);
        }

        /// <summary>
        /// 難易度に対応する色（バッジのドット用）。
        /// 緑=Basic（安心して触れる）/ 橙=Intermediate / 赤=Advanced。
        /// </summary>
        public static Color GetDifficultyColor(NataneInspectorDifficulty difficulty)
        {
            switch (difficulty)
            {
                case NataneInspectorDifficulty.Basic:
                    return NataneToonColorPalette.Success;
                case NataneInspectorDifficulty.Intermediate:
                    return NataneToonColorPalette.Warning;
                case NataneInspectorDifficulty.Advanced:
                    return NataneToonColorPalette.Error;
                default:
                    return NataneToonColorPalette.SecondaryText;
            }
        }

        public static string GetDifficultyLabel(NataneInspectorDifficulty difficulty)
        {
            bool jp = IsJapanese;
            switch (difficulty)
            {
                case NataneInspectorDifficulty.Basic:
                    return jp ? "初級" : "Basic";
                case NataneInspectorDifficulty.Intermediate:
                    return jp ? "中級" : "Intermediate";
                case NataneInspectorDifficulty.Advanced:
                    return jp ? "上級" : "Advanced";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// セクションヘッダー上に難易度ドットを描く。tooltip付きで初心者が意味を確認できる。
        /// </summary>
        public static void DrawDifficultyDot(Rect headerRect, NataneInspectorDifficulty difficulty, bool hasToggle)
        {
            EnsureStyles();
            float size = 8f;
            // トグルバッジ(ON/OFF)がある場合はその左に、無い場合は右端寄せに配置。
            float x = hasToggle ? headerRect.xMax - 60f : headerRect.xMax - 16f;
            Rect dotRect = new Rect(x, headerRect.y + headerRect.height * 0.5f - size * 0.5f, size, size);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(dotRect, GetDifficultyColor(difficulty));

            Rect hoverRect = new Rect(dotRect.x - 3f, dotRect.y - 4f, size + 6f, size + 8f);
            GUI.Label(hoverRect, new GUIContent(string.Empty,
                (IsJapanese ? "難易度: " : "Difficulty: ") + GetDifficultyLabel(difficulty)));
        }

        /// <summary>
        /// 折りたたみ中のセクションに、見た目の結果を表す一行説明を控えめに描く。
        /// </summary>
        public static void DrawCollapsedDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return;
            EnsureStyles();
            Rect rect = EditorGUILayout.GetControlRect(false, 14f);
            GUI.Label(rect, description, _captionStyle);
        }

        /// <summary>
        /// プロパティ直下に「推奨: ...」の一行ヒントを描く。
        /// </summary>
        public static void DrawRecommendationHint(string hint)
        {
            if (string.IsNullOrEmpty(hint)) return;
            EnsureStyles();
            EditorGUILayout.LabelField((IsJapanese ? "推奨: " : "Recommended: ") + hint, _hintStyle);
        }

        public static bool IsCompact => EditorGUIUtility.currentViewWidth < NataneUIConstants.INSPECTOR_WIDTH_COMPACT;
        public static bool IsNarrow => EditorGUIUtility.currentViewWidth < NataneUIConstants.INSPECTOR_WIDTH_NARROW;
        public static bool IsWide => EditorGUIUtility.currentViewWidth >= NataneUIConstants.INSPECTOR_WIDTH_WIDE;

        public static void DrawInspectorHeader(
            string title,
            string subtitle,
            string variant,
            string sampler,
            NataneInspectorStatus samplerStatus,
            System.Action onLanguageClick,
            System.Action onMenuClick)
        {
            EnsureStyles();

            Rect panelRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(panelRect, NataneToonColorPalette.PanelBackgroundElevated);
                EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, 3f, panelRect.height), NataneToonColorPalette.BrandPrimary);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title, _titleStyle, GUILayout.Height(19f));
            if (!string.IsNullOrEmpty(subtitle))
                EditorGUILayout.LabelField(subtitle, _subtitleStyle, GUILayout.Height(16f));
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            if (!IsCompact && !string.IsNullOrEmpty(variant))
                DrawStatusChip(variant, NataneInspectorStatus.Neutral, 86f);
            DrawStatusChip(sampler, samplerStatus, IsCompact ? 64f : 82f);

            if (GUILayout.Button(IsJapanese ? "JP" : "EN", EditorStyles.miniButton, GUILayout.Width(30f), GUILayout.Height(20f)))
                onLanguageClick?.Invoke();
            if (onMenuClick != null && GUILayout.Button("⋮", EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(20f)))
                onMenuClick.Invoke();
            EditorGUILayout.EndHorizontal();

            if (IsCompact && !string.IsNullOrEmpty(variant))
            {
                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField(variant, _subtitleStyle);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(NataneUIConstants.INSPECTOR_SPACE_XS);
        }

        public static void DrawStatusChip(string text, NataneInspectorStatus status, float width = 72f)
        {
            EnsureStyles();
            Color background;
            Color foreground;
            switch (status)
            {
                case NataneInspectorStatus.Success:
                    background = new Color(NataneToonColorPalette.Success.r, NataneToonColorPalette.Success.g, NataneToonColorPalette.Success.b, 0.22f);
                    foreground = EditorGUIUtility.isProSkin ? new Color(0.63f, 0.92f, 0.68f) : new Color(0.12f, 0.46f, 0.20f);
                    break;
                case NataneInspectorStatus.Warning:
                    background = new Color(NataneToonColorPalette.Warning.r, NataneToonColorPalette.Warning.g, NataneToonColorPalette.Warning.b, 0.24f);
                    foreground = EditorGUIUtility.isProSkin ? new Color(1f, 0.82f, 0.45f) : new Color(0.55f, 0.33f, 0.02f);
                    break;
                case NataneInspectorStatus.Error:
                    background = new Color(NataneToonColorPalette.Error.r, NataneToonColorPalette.Error.g, NataneToonColorPalette.Error.b, 0.24f);
                    foreground = EditorGUIUtility.isProSkin ? new Color(1f, 0.62f, 0.62f) : new Color(0.60f, 0.10f, 0.10f);
                    break;
                default:
                    background = EditorGUIUtility.isProSkin
                        ? new Color(1f, 1f, 1f, 0.08f)
                        : new Color(0f, 0f, 0f, 0.07f);
                    foreground = NataneToonColorPalette.SecondaryText;
                    break;
            }

            Rect rect = GUILayoutUtility.GetRect(width, 20f, GUILayout.Width(width), GUILayout.Height(20f));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, background);

            Color old = _chipStyle.normal.textColor;
            _chipStyle.normal.textColor = foreground;
            GUI.Label(rect, text, _chipStyle);
            _chipStyle.normal.textColor = old;
        }

        public static void DrawGroupHeader(string label, string summary = null)
        {
            EnsureStyles();
            EditorGUILayout.Space(NataneUIConstants.INSPECTOR_SPACE_SM);
            Rect rect = EditorGUILayout.GetControlRect(false, 18f);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), NataneToonColorPalette.Divider);
            }
            GUI.Label(new Rect(rect.x, rect.y, rect.width * 0.65f, rect.height), label, _groupStyle);
            if (!string.IsNullOrEmpty(summary))
            {
                GUIStyle right = new GUIStyle(_groupStyle) { alignment = TextAnchor.MiddleRight };
                GUI.Label(rect, summary, right);
            }
            EditorGUILayout.Space(2f);
        }

        public static void DrawInlineMessage(string message, NataneInspectorStatus status)
        {
            EnsureStyles();
            Color accent = status == NataneInspectorStatus.Error
                ? NataneToonColorPalette.Error
                : status == NataneInspectorStatus.Warning
                    ? NataneToonColorPalette.Warning
                    : status == NataneInspectorStatus.Success
                        ? NataneToonColorPalette.Success
                        : NataneToonColorPalette.Info;

            float height = _inlineMessageStyle.CalcHeight(new GUIContent(message), Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 42f));
            Rect rect = GUILayoutUtility.GetRect(0f, height + 6f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(accent.r, accent.g, accent.b, EditorGUIUtility.isProSkin ? 0.10f : 0.08f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accent);
            }
            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width - 6f, rect.height - 4f), message, _inlineMessageStyle);
        }

        public static int DrawTabBar(int selectedTab, string[] labels, string[] compactLabels)
        {
            string[] visibleLabels = IsNarrow && compactLabels != null ? compactLabels : labels;
            return GUILayout.Toolbar(selectedTab, visibleLabels, GUILayout.Height(29f));
        }
    }
}
