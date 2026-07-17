using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// セクションカテゴリ - Inspector UIの各セクションに対応
    /// </summary>
    public enum SectionCategory
    {
        Basic,       // 基本設定 (水色)
        Shading,     // シェーディング (紫)
        Lighting,    // ライティング (橙)
        Effects,     // エフェクト (緑)
        Environment, // 環境 (ティール)
        Advanced     // 詳細設定 (赤茶)
    }

    /// <summary>
    /// Natane Toon Shader Inspector UI用のキャッシュ済みGUIStyleとセクションカラー定義
    /// </summary>
    public static class NataneToonShaderGUIStyles
    {
        // ===== Toggle Status Colors =====
        public static readonly Color ToggleEnabledColor    = new Color(0.3f, 0.8f, 0.3f);
        public static readonly Color ToggleDisabledColor   = new Color(0.6f, 0.6f, 0.6f);
        public static readonly Color ToggleBlockedColor    = new Color(0.9f, 0.6f, 0.2f);
        public static readonly Color SamplerBlockHintColor = new Color(0.92f, 0.66f, 0.22f);

        /// <summary>
        /// セクションカテゴリに対応するカラーを返す
        /// </summary>
        public static Color GetSectionColor(SectionCategory category)
        {
            switch (category)
            {
                case SectionCategory.Basic:       return NataneToonColorPalette.SectionBasic;
                case SectionCategory.Shading:     return NataneToonColorPalette.SectionShading;
                case SectionCategory.Lighting:    return NataneToonColorPalette.SectionLighting;
                case SectionCategory.Effects:     return NataneToonColorPalette.SectionEffects;
                case SectionCategory.Environment: return NataneToonColorPalette.SectionEnvironment;
                case SectionCategory.Advanced:    return NataneToonColorPalette.SectionAdvanced;
                default:                          return NataneToonColorPalette.SectionBasic;
            }
        }

        // ===== Cached GUIStyles =====

        private static GUIStyle _boxOuter;
        private static bool _boxOuterDarkApplied;
        /// <summary>
        /// セクション外枠ボックス - helpBoxベース、padding(8,8,4,4)、下マージン付き
        /// P-21: ダークテーマ時にコントラストを強化
        /// </summary>
        public static GUIStyle BoxOuter
        {
            get
            {
                if (_boxOuter == null)
                {
                    _boxOuter = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(8, 8, 4, 4),
                        margin = new RectOffset(0, 0, 0, 6)
                    };
                    _boxOuterDarkApplied = false;
                }
                // P-21: Dark theme contrast enhancement
                if (EditorGUIUtility.isProSkin && !_boxOuterDarkApplied)
                {
                    Texture2D bgTex = new Texture2D(1, 1);
                    bgTex.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.25f, 0.3f));
                    bgTex.Apply();
                    bgTex.hideFlags = HideFlags.HideAndDontSave;
                    _boxOuter.normal.background = bgTex;
                    _boxOuterDarkApplied = true;
                }
                return _boxOuter;
            }
        }

        private static GUIStyle _boxInner;
        /// <summary>
        /// 内部コンテンツエリア - インデント付き、padding(12,8,4,4)
        /// </summary>
        public static GUIStyle BoxInner
        {
            get
            {
                if (_boxInner == null)
                {
                    _boxInner = new GUIStyle()
                    {
                        padding = new RectOffset(12, 8, 4, 4),
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                }
                return _boxInner;
            }
        }

        private static GUIStyle _sectionHeader;
        /// <summary>
        /// セクションヘッダー - 太字ラベル、fontSize 12
        /// </summary>
        public static GUIStyle SectionHeader
        {
            get
            {
                if (_sectionHeader == null)
                {
                    _sectionHeader = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12
                    };
                }
                return _sectionHeader;
            }
        }

        private static GUIStyle _sectionHeaderFoldout;
        /// <summary>
        /// フォールドアウトヘッダースタイル - 太字、fontSize 12
        /// </summary>
        public static GUIStyle SectionHeaderFoldout
        {
            get
            {
                if (_sectionHeaderFoldout == null)
                {
                    _sectionHeaderFoldout = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 12
                    };
                }
                return _sectionHeaderFoldout;
            }
        }

        private static GUIStyle _toggleBadgeOn;
        /// <summary>
        /// ON状態バッジ - 緑背景、白文字、小ラベル
        /// </summary>
        public static GUIStyle ToggleBadgeOn
        {
            get
            {
                if (_toggleBadgeOn == null)
                {
                    _toggleBadgeOn = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(4, 4, 1, 1),
                        fontStyle = FontStyle.Bold,
                        fontSize = 9
                    };
                    _toggleBadgeOn.normal.textColor = Color.white;
                    // 緑背景用テクスチャ
                    Texture2D bgTex = new Texture2D(1, 1);
                    bgTex.SetPixel(0, 0, new Color(0.2f, 0.7f, 0.3f, 1f));
                    bgTex.Apply();
                    bgTex.hideFlags = HideFlags.HideAndDontSave;
                    _toggleBadgeOn.normal.background = bgTex;
                }
                return _toggleBadgeOn;
            }
        }

        private static GUIStyle _toggleBadgeOff;
        /// <summary>
        /// OFF状態バッジ - グレー背景、ミュート文字
        /// </summary>
        public static GUIStyle ToggleBadgeOff
        {
            get
            {
                if (_toggleBadgeOff == null)
                {
                    _toggleBadgeOff = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(4, 4, 1, 1),
                        fontSize = 9
                    };
                    _toggleBadgeOff.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                    // グレー背景用テクスチャ
                    Texture2D bgTex = new Texture2D(1, 1);
                    bgTex.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                    bgTex.Apply();
                    bgTex.hideFlags = HideFlags.HideAndDontSave;
                    _toggleBadgeOff.normal.background = bgTex;
                }
                return _toggleBadgeOff;
            }
        }

        private static GUIStyle _categoryDividerLabel;
        /// <summary>
        /// カテゴリ区切りラベル - 中央揃え miniLabel、ミュートカラー
        /// </summary>
        public static GUIStyle CategoryDividerLabel
        {
            get
            {
                if (_categoryDividerLabel == null)
                {
                    _categoryDividerLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter
                    };
                    _categoryDividerLabel.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                }
                return _categoryDividerLabel;
            }
        }

        private static GUIStyle _searchField;
        /// <summary>
        /// 検索フィールド - テキスト入力用スタイル
        /// </summary>
        public static GUIStyle SearchField
        {
            get
            {
                if (_searchField == null)
                {
                    _searchField = new GUIStyle(EditorStyles.toolbarSearchField)
                    {
                        margin = new RectOffset(4, 4, 4, 4)
                    };
                }
                return _searchField;
            }
        }

        // P-17: Dependency hint label for disabled feature sections
        private static GUIStyle _dependencyHintLabel;
        /// <summary>
        /// 無効な機能セクションに表示するヒントラベル - イタリック、右揃え、半透明
        /// </summary>
        public static GUIStyle DependencyHintLabel
        {
            get
            {
                if (_dependencyHintLabel == null)
                {
                    _dependencyHintLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontStyle = FontStyle.Italic,
                        alignment = TextAnchor.MiddleRight
                    };
                    _dependencyHintLabel.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                }
                return _dependencyHintLabel;
            }
        }

        // ===== Helper Methods =====

        /// <summary>
        /// 指定されたRectの左端に3px幅のアクセントバーを描画する
        /// </summary>
        public static void DrawAccentBar(Rect rect, Color color)
        {
            Rect barRect = new Rect(rect.x, rect.y, 3f, rect.height);
            EditorGUI.DrawRect(barRect, color);
        }

        /// <summary>
        /// 指定されたRectにセクションカテゴリカラーの背景を描画する
        /// Dark/Lightテーマに応じてアルファ値を調整
        /// </summary>
        public static void DrawSectionBackground(Rect rect, SectionCategory category)
        {
            Color color = GetSectionColor(category);
            float alpha = EditorGUIUtility.isProSkin ? 0.15f : 0.08f;
            Color bgColor = new Color(color.r, color.g, color.b, alpha);
            EditorGUI.DrawRect(rect, bgColor);
        }
    }
}
