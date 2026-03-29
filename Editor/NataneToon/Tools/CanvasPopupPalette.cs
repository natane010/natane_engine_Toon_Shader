using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Floating popup palette for quick brush settings on the canvas.
    /// キャンバス上のクイックブラシ設定用フローティングポップアップパレット
    /// Similar to Krita's Pop-up Palette.
    /// </summary>
    internal class CanvasPopupPalette : PopupWindowContent
    {
        private BrushSettings brushSettings;
        private System.Action<int> onToolSwitch; // 0=Brush, 1=Eraser, 2=Fill
        private List<BrushPreset> recentPresets;

        public CanvasPopupPalette(BrushSettings settings, System.Action<int> toolSwitchCallback)
        {
            this.brushSettings = settings;
            this.onToolSwitch = toolSwitchCallback;
            // Get recent presets from browser
            var allPresets = BrushPresetBrowser.GetPresets();
            recentPresets = allPresets != null && allPresets.Count > 0
                ? allPresets.GetRange(0, Mathf.Min(4, allPresets.Count))
                : new List<BrushPreset>();
        }

        public override Vector2 GetWindowSize()
        {
            bool isColor = brushSettings != null && brushSettings.colorMode;
            return new Vector2(210, isColor ? 320 : 260);
        }

        public override void OnGUI(Rect rect)
        {
            if (brushSettings == null) return;

            EditorGUILayout.Space(4);

            // Size slider
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(L("サイズ", "Size"), EditorStyles.miniLabel, GUILayout.Width(56));
            brushSettings.size = EditorGUILayout.Slider(brushSettings.size, 1f, 100f);
            EditorGUILayout.EndHorizontal();

            // Opacity slider
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(L("不透明度", "Opacity"), EditorStyles.miniLabel, GUILayout.Width(56));
            brushSettings.opacity = EditorGUILayout.Slider(brushSettings.opacity, 0f, 1f);
            EditorGUILayout.EndHorizontal();

            // Hardness slider
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(L("硬さ", "Hard"), EditorStyles.miniLabel, GUILayout.Width(50));
            brushSettings.hardness = EditorGUILayout.Slider(brushSettings.hardness, 0f, 1f);
            EditorGUILayout.EndHorizontal();

            // Strength (paint value)
            if (brushSettings.mode == BrushMode.Paint)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(L("強度", "Value"), EditorStyles.miniLabel, GUILayout.Width(50));
                brushSettings.strength = EditorGUILayout.Slider(brushSettings.strength, 0f, 1f);
                EditorGUILayout.EndHorizontal();
            }

            // Color mode: foreground/background color fields
            if (brushSettings.colorMode)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                brushSettings.paintColor = EditorGUILayout.ColorField(
                    GUIContent.none, brushSettings.paintColor,
                    true, true, false, GUILayout.Height(20));
                if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    var tmp = brushSettings.paintColor;
                    brushSettings.paintColor = brushSettings.backgroundColor;
                    brushSettings.backgroundColor = tmp;
                }
                brushSettings.backgroundColor = EditorGUILayout.ColorField(
                    GUIContent.none, brushSettings.backgroundColor,
                    true, true, false, GUILayout.Height(20));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            // Tool switch buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("B", L("ブラシ", "Brush")), GUILayout.Height(24)))
            {
                onToolSwitch?.Invoke(0);
                editorWindow.Close();
            }
            if (GUILayout.Button(new GUIContent("E", L("消しゴム", "Eraser")), GUILayout.Height(24)))
            {
                onToolSwitch?.Invoke(1);
                editorWindow.Close();
            }
            if (GUILayout.Button(new GUIContent("G", L("バケツ", "Fill")), GUILayout.Height(24)))
            {
                onToolSwitch?.Invoke(2);
                editorWindow.Close();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Recent presets
            if (recentPresets.Count > 0)
            {
                GUILayout.Label(L("プリセット", "Presets"), EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                foreach (var preset in recentPresets)
                {
                    string label = preset.name;
                    if (label.Length > 6) label = label.Substring(0, 5) + "…";
                    if (GUILayout.Button(new GUIContent(label, preset.name),
                        EditorStyles.miniButton, GUILayout.Height(22)))
                    {
                        preset.ApplyTo(brushSettings);
                        editorWindow.Close();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            // Color history (recent 8 colors from color picker)
            EditorGUILayout.Space(2);
            GUILayout.Label(L("最近の色", "Recent"), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            // This needs access to colorHistory from ColorPickerPanel
            // For now, draw 8 placeholder color blocks
            for (int ci = 0; ci < 8; ci++)
            {
                Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                Color blockColor = ci == 0 ? brushSettings.paintColor : new Color(0.3f, 0.3f, 0.3f);
                EditorGUI.DrawRect(colorRect, blockColor);
                EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y, colorRect.width, 1), Color.gray);
                EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.yMax - 1, colorRect.width, 1), Color.gray);
                EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y, 1, colorRect.height), Color.gray);
                EditorGUI.DrawRect(new Rect(colorRect.xMax - 1, colorRect.y, 1, colorRect.height), Color.gray);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
