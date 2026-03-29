using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Renders pixel grid overlay at high zoom levels.
    /// 高ズーム時にピクセルグリッドオーバーレイを描画
    /// </summary>
    internal static class PixelGridRenderer
    {
        private static Material lineMaterial;
        private const float MinZoomForGrid = 4f; // 400%

        public static bool ShouldDraw(float canvasZoom) => canvasZoom >= MinZoomForGrid;

        /// <summary>
        /// Draw pixel grid on the canvas.
        /// キャンバスにピクセルグリッドを描画
        /// </summary>
        public static void DrawGrid(Rect texRect, int texWidth, int texHeight, float zoom)
        {
            if (zoom < MinZoomForGrid) return;

            float pixelSize = texRect.width / texWidth;
            if (pixelSize < 3f) return; // Don't draw if pixels are too small

            Color gridColor = new Color(1f, 1f, 1f, Mathf.Clamp01((zoom - MinZoomForGrid) * 0.15f + 0.05f));

            // Vertical lines
            for (int x = 0; x <= texWidth; x++)
            {
                float screenX = texRect.x + x * pixelSize;
                if (screenX < texRect.x || screenX > texRect.xMax) continue;
                EditorGUI.DrawRect(new Rect(screenX, texRect.y, 1, texRect.height), gridColor);
            }

            // Horizontal lines
            for (int y = 0; y <= texHeight; y++)
            {
                float screenY = texRect.y + y * pixelSize;
                if (screenY < texRect.y || screenY > texRect.yMax) continue;
                EditorGUI.DrawRect(new Rect(texRect.x, screenY, texRect.width, 1), gridColor);
            }
        }
    }
}
