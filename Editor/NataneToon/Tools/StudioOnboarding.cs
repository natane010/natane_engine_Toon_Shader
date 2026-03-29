using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Shortcut help overlay / onboarding window for texture studio.
    /// テクスチャスタジオ用ショートカットヘルプオーバーレイ
    /// </summary>
    internal class StudioOnboarding : EditorWindow
    {
        private Vector2 scrollPosition;
        private static GUIStyle headerStyle;
        private static GUIStyle keyStyle;
        private static GUIStyle descStyle;
        private static GUIStyle sectionStyle;

        public static void Open()
        {
            var window = GetWindow<StudioOnboarding>(true,
                L("ショートカット一覧", "Keyboard Shortcuts"));
            window.minSize = new Vector2(420, 500);
            window.maxSize = new Vector2(500, 700);
            window.ShowUtility();
        }

        private static void EnsureStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 8, 8),
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(4, 0, 6, 2),
                normal = { textColor = new Color(0.55f, 0.78f, 1f) }
            };

            keyStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                padding = new RectOffset(0, 8, 2, 2),
                normal = { textColor = new Color(1f, 0.85f, 0.5f) }
            };

            descStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 0, 2, 2),
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
        }

        private void OnGUI()
        {
            EnsureStyles();

            EditorGUILayout.LabelField(L("テクスチャスタジオ ショートカット", "Texture Studio Shortcuts"), headerStyle);
            EditorGUILayout.Space(4);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawSection(L("ツール", "Tools"), new[]
            {
                ("B", L("ブラシ", "Brush")),
                ("E", L("消しゴム", "Eraser")),
                ("G", L("塗りつぶし", "Fill")),
                ("V", L("移動", "Move")),
                ("I", L("スポイト", "Eyedropper")),
                ("M", L("矩形選択", "Rectangle Select")),
                ("L", L("投げ縄選択", "Lasso Select")),
                ("Q", L("クイックマスク", "Quick Mask")),
            });

            DrawSection(L("カラー", "Color"), new[]
            {
                ("X", L("前景色/背景色切替", "Swap FG/BG Colors")),
                ("D", L("デフォルト色にリセット", "Reset to Default Colors")),
            });

            DrawSection(L("ブラシ", "Brush"), new[]
            {
                ("] / [", L("サイズ +/-", "Size Up/Down")),
                ("0-9", L("不透明度 (0=100%, 1=10%...9=90%)", "Opacity")),
                ("Shift+0-9", L("強度", "Strength")),
                ("Alt+右ドラッグ", L("サイズ(X) / 不透明度(Y) 調整", "Size(X) / Opacity(Y) Adjust")),
                ("Shift+クリック", L("直線描画（前回位置から）", "Straight Line")),
            });

            DrawSection(L("キャンバス", "Canvas"), new[]
            {
                (L("中クリック", "MMB"), L("パン（手のひら）", "Pan")),
                ("Space+ドラッグ", L("パン", "Pan")),
                ("Ctrl+スクロール", L("ズーム", "Zoom")),
                ("F", L("全体表示", "Fit Canvas")),
                ("R+ドラッグ", L("キャンバス回転", "Rotate Canvas")),
                ("U", L("UVワイヤーフレーム表示", "Toggle UV Wireframe")),
                ("Tab", L("ポップアップパレット", "Popup Palette")),
            });

            DrawSection(L("編集", "Edit"), new[]
            {
                ("Ctrl+Z", L("元に戻す", "Undo")),
                ("Ctrl+Y", L("やり直し", "Redo")),
                ("Ctrl+S", L("プロジェクト保存", "Save Project")),
                ("Ctrl+O", L("プロジェクトを開く", "Open Project")),
                ("Ctrl+Shift+P", L("コマンドパレット", "Command Palette")),
                ("F1", L("このヘルプを表示", "Show This Help")),
            });

            EditorGUILayout.EndScrollView();
        }

        private void DrawSection(string title, (string key, string desc)[] items)
        {
            EditorGUILayout.LabelField(title, sectionStyle);

            foreach (var (key, desc) in items)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(key, keyStyle, GUILayout.Width(140));
                EditorGUILayout.LabelField(desc, descStyle);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);
        }
    }
}
