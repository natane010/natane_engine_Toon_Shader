using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Renders pixel grid overlay at high zoom levels using GL.Lines for performance.
    /// 高ズーム時にピクセルグリッドオーバーレイをGL.Linesで高速描画
    /// </summary>
    internal static class PixelGridRenderer
    {
        private static Material lineMaterial;
        private const float MinZoomForGrid = 4f; // 400%

        public static bool ShouldDraw(float canvasZoom) => canvasZoom >= MinZoomForGrid;

        private static void EnsureLineMaterial()
        {
            if (lineMaterial != null) return;
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        /// <summary>
        /// Draw pixel grid on the canvas using GL.Lines.
        /// キャンバスにピクセルグリッドをGL.Linesで描画
        /// </summary>
        public static void DrawGrid(Rect texRect, int texWidth, int texHeight, float zoom)
        {
            if (zoom < MinZoomForGrid) return;
            float pixelSize = texRect.width / texWidth;
            if (pixelSize < 3f) return;

            EnsureLineMaterial();
            if (lineMaterial == null) return;

            Color gridColor = new Color(1f, 1f, 1f, Mathf.Clamp01((zoom - MinZoomForGrid) * 0.15f + 0.05f));

            // Compute visible range to avoid drawing off-screen lines
            Rect clipRect = GUIClip_GetVisibleRect();
            float visLeft = Mathf.Max(texRect.x, clipRect.x);
            float visRight = Mathf.Min(texRect.xMax, clipRect.xMax);
            float visTop = Mathf.Max(texRect.y, clipRect.y);
            float visBottom = Mathf.Min(texRect.yMax, clipRect.yMax);

            if (visLeft >= visRight || visTop >= visBottom) return;

            int xStart = Mathf.Max(0, Mathf.FloorToInt((visLeft - texRect.x) / pixelSize));
            int xEnd = Mathf.Min(texWidth, Mathf.CeilToInt((visRight - texRect.x) / pixelSize));
            int yStart = Mathf.Max(0, Mathf.FloorToInt((visTop - texRect.y) / pixelSize));
            int yEnd = Mathf.Min(texHeight, Mathf.CeilToInt((visBottom - texRect.y) / pixelSize));

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(gridColor);

            // Vertical lines
            for (int x = xStart; x <= xEnd; x++)
            {
                float screenX = texRect.x + x * pixelSize;
                GL.Vertex3(screenX, visTop, 0);
                GL.Vertex3(screenX, visBottom, 0);
            }

            // Horizontal lines
            for (int y = yStart; y <= yEnd; y++)
            {
                float screenY = texRect.y + y * pixelSize;
                GL.Vertex3(visLeft, screenY, 0);
                GL.Vertex3(visRight, screenY, 0);
            }

            GL.End();
            GL.PopMatrix();
        }

        private static Rect GUIClip_GetVisibleRect()
        {
            // Try to get the visible rect from GUIClip via reflection, fallback to large rect
            try
            {
                var type = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
                if (type != null)
                {
                    var prop = type.GetProperty("visibleRect",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);
                    if (prop != null)
                        return (Rect)prop.GetValue(null);
                }
            }
            catch { }
            return new Rect(0, 0, Screen.width, Screen.height);
        }
    }
}
