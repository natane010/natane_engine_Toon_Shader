using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    internal enum CanvasBackgroundType { Dark, Light, Checkerboard, Custom }

    internal class CanvasBackgroundSettings
    {
        public CanvasBackgroundType type = CanvasBackgroundType.Checkerboard;
        public Color customColor = new Color(0.3f, 0.3f, 0.3f);
        public int checkerSize = 8;

        public void DrawBackground(Rect canvasArea)
        {
            switch (type)
            {
                case CanvasBackgroundType.Dark:
                    EditorGUI.DrawRect(canvasArea, new Color(0.12f, 0.12f, 0.12f));
                    break;
                case CanvasBackgroundType.Light:
                    EditorGUI.DrawRect(canvasArea, new Color(0.75f, 0.75f, 0.75f));
                    break;
                case CanvasBackgroundType.Checkerboard:
                    EditorGUI.DrawRect(canvasArea, new Color(0.15f, 0.15f, 0.15f));
                    DrawCheckerboard(canvasArea, checkerSize);
                    break;
                case CanvasBackgroundType.Custom:
                    EditorGUI.DrawRect(canvasArea, customColor);
                    break;
            }
        }

        private static void DrawCheckerboard(Rect rect, int cellSize)
        {
            Color light = new Color(0.22f, 0.22f, 0.22f);
            Color dark = new Color(0.18f, 0.18f, 0.18f);

            int cols = Mathf.CeilToInt(rect.width / cellSize);
            int rows = Mathf.CeilToInt(rect.height / cellSize);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if ((row + col) % 2 == 0) continue;
                    Rect cellRect = new Rect(
                        rect.x + col * cellSize,
                        rect.y + row * cellSize,
                        Mathf.Min(cellSize, rect.xMax - (rect.x + col * cellSize)),
                        Mathf.Min(cellSize, rect.yMax - (rect.y + row * cellSize)));
                    EditorGUI.DrawRect(cellRect, light);
                }
            }
        }

        public void DrawSettingsUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("背景", "Background"), EditorStyles.boldLabel);
            type = (CanvasBackgroundType)EditorGUILayout.EnumPopup(L("タイプ", "Type"), type);
            if (type == CanvasBackgroundType.Custom)
                customColor = EditorGUILayout.ColorField(L("色", "Color"), customColor);
            if (type == CanvasBackgroundType.Checkerboard)
                checkerSize = EditorGUILayout.IntSlider(L("セルサイズ", "Cell Size"), checkerSize, 4, 32);
            EditorGUILayout.EndVertical();
        }
    }
}
