using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// VS Code-style command palette for texture studio.
    /// テクスチャスタジオ用VSCode風コマンドパレット
    /// </summary>
    internal class StudioCommandPalette : EditorWindow
    {
        private string searchText = "";
        private int selectedIndex;
        private Vector2 scrollPosition;
        private List<CommandEntry> allCommands;
        private List<CommandEntry> filteredCommands;
        private System.Action<string> executeCallback;

        private static GUIStyle searchStyle;
        private static GUIStyle itemStyle;
        private static GUIStyle selectedItemStyle;
        private static GUIStyle shortcutStyle;
        private static GUIStyle categoryStyle;
        private static GUIStyle footerCountStyle;

        // Cached colors
        private static readonly Color PanelBackground = new Color(0.18f, 0.18f, 0.2f, 1f);
        private static readonly Color PanelBorder = new Color(0.4f, 0.5f, 0.7f, 0.6f);
        private static readonly Color SelectedItemBg = new Color(0.25f, 0.4f, 0.65f, 0.8f);
        private static readonly Color HoverItemBg = new Color(0.25f, 0.25f, 0.3f, 0.5f);
        private static readonly Color FooterBg = new Color(0.15f, 0.15f, 0.17f);

        internal struct CommandEntry
        {
            public string id;
            public string displayName;
            public string category;
            public string shortcut;
            public System.Action action;
        }

        public static void Open(List<CommandEntry> commands, System.Action<string> onExecute = null)
        {
            var window = CreateInstance<StudioCommandPalette>();
            window.allCommands = commands ?? new List<CommandEntry>();
            window.filteredCommands = new List<CommandEntry>(window.allCommands);
            window.executeCallback = onExecute;
            window.titleContent = new GUIContent(L("コマンドパレット", "Command Palette"));

            // Position at center of focused window
            Vector2 size = new Vector2(450, 350);
            if (focusedWindow != null)
            {
                var parentPos = focusedWindow.position;
                window.position = new Rect(
                    parentPos.x + (parentPos.width - size.x) * 0.5f,
                    parentPos.y + 80,
                    size.x, size.y);
            }
            else
            {
                window.position = new Rect(
                    Screen.currentResolution.width * 0.5f - size.x * 0.5f,
                    Screen.currentResolution.height * 0.3f,
                    size.x, size.y);
            }

            window.ShowPopup();
            window.Focus();
        }

        private static void EnsureStyles()
        {
            if (searchStyle != null) return;

            searchStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                fontSize = 14,
                fixedHeight = 32,
                margin = new RectOffset(8, 8, 8, 4)
            };

            itemStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                padding = new RectOffset(12, 12, 4, 4),
                fixedHeight = 28,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            selectedItemStyle = new GUIStyle(itemStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            shortcutStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleRight,
                padding = new RectOffset(0, 12, 0, 0),
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            categoryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                padding = new RectOffset(12, 0, 0, 0),
                normal = { textColor = new Color(0.5f, 0.7f, 1f) }
            };
            footerCountStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
        }

        private void OnGUI()
        {
            EnsureStyles();

            // Background
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), PanelBackground);
            // Border
            Color borderColor = PanelBorder;
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 2), borderColor);
            EditorGUI.DrawRect(new Rect(0, position.height - 2, position.width, 2), borderColor);
            EditorGUI.DrawRect(new Rect(0, 0, 2, position.height), borderColor);
            EditorGUI.DrawRect(new Rect(position.width - 2, 0, 2, position.height), borderColor);

            // Search field
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("CommandSearch");
            searchText = EditorGUILayout.TextField(searchText, searchStyle);
            if (EditorGUI.EndChangeCheck())
            {
                FilterCommands();
                selectedIndex = 0;
            }
            EditorGUI.FocusTextInControl("CommandSearch");

            // Handle keyboard
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.DownArrow:
                        selectedIndex = Mathf.Min(selectedIndex + 1, filteredCommands.Count - 1);
                        e.Use();
                        break;
                    case KeyCode.UpArrow:
                        selectedIndex = Mathf.Max(selectedIndex - 1, 0);
                        e.Use();
                        break;
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        ExecuteSelected();
                        e.Use();
                        break;
                    case KeyCode.Escape:
                        Close();
                        e.Use();
                        break;
                }
            }

            // Results list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            string lastCategory = "";
            for (int i = 0; i < filteredCommands.Count; i++)
            {
                var cmd = filteredCommands[i];

                // Category header
                if (!string.IsNullOrEmpty(cmd.category) && cmd.category != lastCategory)
                {
                    lastCategory = cmd.category;
                    EditorGUILayout.LabelField(cmd.category, categoryStyle);
                }

                // Item
                bool isSelected = (i == selectedIndex);
                Rect itemRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));

                if (isSelected)
                    EditorGUI.DrawRect(itemRect, SelectedItemBg);
                else if (itemRect.Contains(e.mousePosition))
                    EditorGUI.DrawRect(itemRect, HoverItemBg);

                GUI.Label(itemRect, cmd.displayName, isSelected ? selectedItemStyle : itemStyle);

                if (!string.IsNullOrEmpty(cmd.shortcut))
                    GUI.Label(itemRect, cmd.shortcut, shortcutStyle);

                // Click to execute
                if (e.type == EventType.MouseDown && itemRect.Contains(e.mousePosition))
                {
                    selectedIndex = i;
                    ExecuteSelected();
                    e.Use();
                }
            }

            if (filteredCommands.Count == 0)
            {
                EditorGUILayout.LabelField(
                    L("一致するコマンドがありません", "No matching commands"),
                    itemStyle);
            }

            EditorGUILayout.EndScrollView();

            // Count
            Rect footerRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(footerRect, FooterBg);
            GUI.Label(footerRect, $"  {filteredCommands.Count}/{allCommands.Count}", footerCountStyle);

            // Close if focus lost
            if (focusedWindow != this && e.type == EventType.Repaint)
                Close();
        }

        private void FilterCommands()
        {
            if (string.IsNullOrEmpty(searchText))
            {
                filteredCommands = new List<CommandEntry>(allCommands);
                return;
            }

            string lower = searchText.ToLowerInvariant();
            filteredCommands = allCommands
                .Where(c => c.displayName.ToLowerInvariant().Contains(lower)
                    || (!string.IsNullOrEmpty(c.id) && c.id.ToLowerInvariant().Contains(lower))
                    || (!string.IsNullOrEmpty(c.category) && c.category.ToLowerInvariant().Contains(lower)))
                .ToList();
        }

        private void ExecuteSelected()
        {
            if (selectedIndex >= 0 && selectedIndex < filteredCommands.Count)
            {
                var cmd = filteredCommands[selectedIndex];
                Close();
                cmd.action?.Invoke();
                executeCallback?.Invoke(cmd.id);
            }
        }

        /// <summary>
        /// Create the default command list for texture studio.
        /// テクスチャスタジオ用デフォルトコマンドリストを作成
        /// </summary>
        public static List<CommandEntry> CreateDefaultCommands(
            System.Action<string> toolSwitch,
            System.Action<string> filterApply,
            System.Action<string> layerAction)
        {
            var commands = new List<CommandEntry>();

            // Tools
            string toolCat = L("ツール", "Tools");
            commands.Add(new CommandEntry { id = "tool.brush", displayName = L("ブラシ", "Brush"), category = toolCat, shortcut = "B", action = () => toolSwitch?.Invoke("Brush") });
            commands.Add(new CommandEntry { id = "tool.eraser", displayName = L("消しゴム", "Eraser"), category = toolCat, shortcut = "E", action = () => toolSwitch?.Invoke("Eraser") });
            commands.Add(new CommandEntry { id = "tool.fill", displayName = L("塗りつぶし", "Fill"), category = toolCat, shortcut = "G", action = () => toolSwitch?.Invoke("Fill") });
            commands.Add(new CommandEntry { id = "tool.eyedropper", displayName = L("スポイト", "Eyedropper"), category = toolCat, shortcut = "I", action = () => toolSwitch?.Invoke("Eyedropper") });
            commands.Add(new CommandEntry { id = "tool.gradient", displayName = L("グラデーション", "Gradient"), category = toolCat, shortcut = "", action = () => toolSwitch?.Invoke("Gradient") });
            commands.Add(new CommandEntry { id = "tool.move", displayName = L("移動", "Move"), category = toolCat, shortcut = "V", action = () => toolSwitch?.Invoke("Move") });

            // Filters
            string filterCat = L("フィルター", "Filters");
            commands.Add(new CommandEntry { id = "filter.blur", displayName = L("ガウシアンブラー", "Gaussian Blur"), category = filterCat, action = () => filterApply?.Invoke("blur") });
            commands.Add(new CommandEntry { id = "filter.sharpen", displayName = L("シャープ", "Sharpen"), category = filterCat, action = () => filterApply?.Invoke("sharpen") });
            commands.Add(new CommandEntry { id = "filter.edge", displayName = L("エッジ検出", "Edge Detection"), category = filterCat, action = () => filterApply?.Invoke("edge") });
            commands.Add(new CommandEntry { id = "filter.levels", displayName = L("レベル補正", "Levels"), category = filterCat, action = () => filterApply?.Invoke("levels") });
            commands.Add(new CommandEntry { id = "filter.threshold", displayName = L("二値化", "Threshold"), category = filterCat, action = () => filterApply?.Invoke("threshold") });
            commands.Add(new CommandEntry { id = "filter.hsl", displayName = L("色相/彩度/明度", "HSL Adjust"), category = filterCat, action = () => filterApply?.Invoke("hsl") });
            commands.Add(new CommandEntry { id = "filter.posterize", displayName = L("ポスタリゼーション", "Posterize"), category = filterCat, action = () => filterApply?.Invoke("posterize") });
            commands.Add(new CommandEntry { id = "filter.desaturate", displayName = L("彩度除去", "Desaturate"), category = filterCat, action = () => filterApply?.Invoke("desaturate") });

            // Layer
            string layerCat = L("レイヤー", "Layers");
            commands.Add(new CommandEntry { id = "layer.add", displayName = L("レイヤー追加", "Add Layer"), category = layerCat, action = () => layerAction?.Invoke("add") });
            commands.Add(new CommandEntry { id = "layer.duplicate", displayName = L("レイヤー複製", "Duplicate Layer"), category = layerCat, action = () => layerAction?.Invoke("duplicate") });
            commands.Add(new CommandEntry { id = "layer.merge", displayName = L("下のレイヤーと結合", "Merge Down"), category = layerCat, action = () => layerAction?.Invoke("merge") });
            commands.Add(new CommandEntry { id = "layer.flatten", displayName = L("全レイヤー統合", "Flatten All"), category = layerCat, action = () => layerAction?.Invoke("flatten") });
            commands.Add(new CommandEntry { id = "layer.mask.add", displayName = L("マスク作成", "Add Mask"), category = layerCat, action = () => layerAction?.Invoke("mask.add") });

            return commands;
        }
    }
}
