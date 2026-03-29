using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Rich status bar for the texture studio canvas.
    /// テクスチャスタジオキャンバス用リッチステータスバー
    /// </summary>
    internal static class StudioStatusBar
    {
        internal const float PreferredHeight = 28f;

        private static GUIStyle statusStyle;
        private static GUIStyle statusBoldStyle;
        private static GUIStyle colorPatchStyle;

        private static readonly Color BarBackground = new Color(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color BarTopBorder = new Color(0.1f, 0.1f, 0.1f);
        private static readonly Color SeparatorColor = new Color(0.3f, 0.3f, 0.3f);

        private static void EnsureStyles()
        {
            if (statusStyle != null) return;
            statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false,
                padding = new RectOffset(4, 4, 0, 0),
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            statusBoldStyle = new GUIStyle(statusStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
        }

        /// <summary>
        /// Draw the status bar at the bottom of the canvas area.
        /// キャンバスエリア下部にステータスバーを描画
        /// </summary>
        public static void Draw(
            Rect barRect,
            string toolName,
            Vector2 pixelCoord,
            int texWidth, int texHeight,
            Color pixelColor,
            float zoom,
            string layerName,
            float brushSize,
            float brushOpacity,
            string extraStatus = null)
        {
            EnsureStyles();

            EditorGUI.DrawRect(barRect, BarBackground);
            // Top border line
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width, 1), BarTopBorder);

            GUI.BeginGroup(barRect);

            float x = 6f;
            float y = 4f;
            float h = Mathf.Max(1f, barRect.height - 8f);

            // Tool name
            DrawLabel(ref x, y, h, toolName, statusBoldStyle, 70f);
            DrawSeparator(ref x, y, h);

            // Pixel coordinates
            int px = Mathf.Clamp(Mathf.RoundToInt(pixelCoord.x), 0, texWidth - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(pixelCoord.y), 0, texHeight - 1);
            DrawLabel(ref x, y, h, $"X:{px} Y:{py}", statusStyle, 100f);
            DrawSeparator(ref x, y, h);

            // Pixel color patch + hex
            Rect patchRect = new Rect(x, y + 2f, 14f, Mathf.Max(8f, h - 4f));
            EditorGUI.DrawRect(patchRect, Color.black); // border
            EditorGUI.DrawRect(new Rect(patchRect.x + 1, patchRect.y + 1, patchRect.width - 2, patchRect.height - 2), pixelColor);
            x += 18f;
            string hex = ColorUtility.ToHtmlStringRGB(pixelColor);
            DrawLabel(ref x, y, h, $"#{hex}", statusStyle, 60f);

            // RGB values
            DrawLabel(ref x, y, h, $"R:{pixelColor.r:F2} G:{pixelColor.g:F2} B:{pixelColor.b:F2}", statusStyle, 160f);
            DrawSeparator(ref x, y, h);

            // Brush info
            DrawLabel(ref x, y, h, $"{brushSize:F0}px / {brushOpacity:P0}", statusStyle, 90f);
            DrawSeparator(ref x, y, h);

            // Zoom
            DrawLabel(ref x, y, h, $"{zoom:F0}%", statusStyle, 45f);
            DrawSeparator(ref x, y, h);

            // Layer name
            string layerDisplay = string.IsNullOrEmpty(layerName) ? "-" : layerName;
            if (layerDisplay.Length > 15) layerDisplay = layerDisplay.Substring(0, 14) + "…";
            DrawLabel(ref x, y, h, layerDisplay, statusStyle, 100f);

            // Texture size
            DrawLabel(ref x, y, h, $"{texWidth}x{texHeight}", statusStyle, 60f);

            // Extra status message (right-aligned)
            if (!string.IsNullOrEmpty(extraStatus))
            {
                float rightWidth = Mathf.Min(220f, Mathf.Max(120f, barRect.width * 0.32f));
                float rightX = Mathf.Max(x + 8f, barRect.width - rightWidth - 6f);
                GUI.Label(new Rect(rightX, y, rightWidth, h), extraStatus, statusStyle);
            }

            GUI.EndGroup();
        }

        private static void DrawLabel(ref float x, float y, float h, string text, GUIStyle style, float width)
        {
            GUI.Label(new Rect(x, y, width, h), text, style);
            x += width;
        }

        private static void DrawSeparator(ref float x, float y, float h)
        {
            x += 4f;
            EditorGUI.DrawRect(new Rect(x, y + 3f, 1f, h - 6f), SeparatorColor);
            x += 5f;
        }
    }
}
