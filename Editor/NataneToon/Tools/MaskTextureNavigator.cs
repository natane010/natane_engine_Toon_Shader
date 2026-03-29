using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Minimap navigator widget for the mask texture canvas.
    /// マスクテクスチャキャンバス用ミニマップナビゲーター
    /// Shows a thumbnail of the full texture with a rectangle indicating the current viewport.
    /// Dragging the rectangle pans the canvas.
    /// </summary>
    internal static class MaskTextureNavigator
    {
        private static bool isDragging;
        private const float NavSize = 120f;

        /// <summary>
        /// Draw the navigator minimap. Returns updated pan value if user drags the viewport rect.
        /// ナビゲーターミニマップを描画。ビューポート矩形をドラッグするとパン値を更新。
        /// </summary>
        /// <param name="texture">The full texture to show as thumbnail.</param>
        /// <param name="canvasZoom">Current canvas zoom level.</param>
        /// <param name="canvasPan">Current canvas pan offset.</param>
        /// <param name="canvasDisplaySize">Size of the canvas display area in pixels.</param>
        /// <returns>Updated pan value.</returns>
        public static Vector2 DrawNavigator(Texture2D texture, float canvasZoom, Vector2 canvasPan, float canvasDisplaySize)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ナビゲーター", "Navigator"), EditorStyles.miniLabel);

            Rect navRect = GUILayoutUtility.GetRect(NavSize, NavSize, GUILayout.Width(NavSize), GUILayout.Height(NavSize));

            // Draw background
            EditorGUI.DrawRect(navRect, new Color(0.12f, 0.12f, 0.12f));

            // Draw texture thumbnail
            if (texture != null)
            {
                EditorGUI.DrawPreviewTexture(navRect, texture);
            }

            // Calculate viewport rectangle
            if (canvasZoom > 1f && texture != null)
            {
                float viewRatio = 1f / canvasZoom;
                float vpWidth = navRect.width * viewRatio;
                float vpHeight = navRect.height * viewRatio;

                // Pan offset to normalized coordinates
                float maxPanX = canvasDisplaySize * (canvasZoom - 1f) * 0.5f;
                float maxPanY = canvasDisplaySize * (canvasZoom - 1f) * 0.5f;
                float normPanX = maxPanX > 0 ? -canvasPan.x / maxPanX : 0f;
                float normPanY = maxPanY > 0 ? -canvasPan.y / maxPanY : 0f;

                float vpX = navRect.x + (navRect.width - vpWidth) * 0.5f * (1f + normPanX);
                float vpY = navRect.y + (navRect.height - vpHeight) * 0.5f * (1f + normPanY);

                Rect vpRect = new Rect(vpX, vpY, vpWidth, vpHeight);

                // Draw viewport rectangle
                Color vpColor = new Color(1f, 1f, 1f, 0.7f);
                // Top edge
                EditorGUI.DrawRect(new Rect(vpRect.x, vpRect.y, vpRect.width, 1), vpColor);
                // Bottom edge
                EditorGUI.DrawRect(new Rect(vpRect.x, vpRect.yMax - 1, vpRect.width, 1), vpColor);
                // Left edge
                EditorGUI.DrawRect(new Rect(vpRect.x, vpRect.y, 1, vpRect.height), vpColor);
                // Right edge
                EditorGUI.DrawRect(new Rect(vpRect.xMax - 1, vpRect.y, 1, vpRect.height), vpColor);

                // Semi-transparent overlay outside viewport
                Color dimColor = new Color(0f, 0f, 0f, 0.4f);
                // Top
                EditorGUI.DrawRect(new Rect(navRect.x, navRect.y, navRect.width, vpRect.y - navRect.y), dimColor);
                // Bottom
                EditorGUI.DrawRect(new Rect(navRect.x, vpRect.yMax, navRect.width, navRect.yMax - vpRect.yMax), dimColor);
                // Left
                EditorGUI.DrawRect(new Rect(navRect.x, vpRect.y, vpRect.x - navRect.x, vpRect.height), dimColor);
                // Right
                EditorGUI.DrawRect(new Rect(vpRect.xMax, vpRect.y, navRect.xMax - vpRect.xMax, vpRect.height), dimColor);

                // Handle drag on navigator to pan canvas
                canvasPan = HandleNavigatorDrag(navRect, vpRect, canvasPan, canvasZoom, canvasDisplaySize);
            }

            EditorGUILayout.EndVertical();
            return canvasPan;
        }

        private static Vector2 HandleNavigatorDrag(Rect navRect, Rect vpRect, Vector2 canvasPan, float canvasZoom, float canvasDisplaySize)
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && navRect.Contains(e.mousePosition))
                    {
                        isDragging = true;
                        // Jump viewport center to click position
                        canvasPan = ClickToPan(e.mousePosition, navRect, canvasZoom, canvasDisplaySize);
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (isDragging)
                    {
                        canvasPan = ClickToPan(e.mousePosition, navRect, canvasZoom, canvasDisplaySize);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (isDragging)
                    {
                        isDragging = false;
                        e.Use();
                    }
                    break;
            }

            return canvasPan;
        }

        private static Vector2 ClickToPan(Vector2 mousePos, Rect navRect, float canvasZoom, float canvasDisplaySize)
        {
            // Convert click position in navigator to normalized coordinates (-1 to 1)
            float normX = ((mousePos.x - navRect.center.x) / (navRect.width * 0.5f));
            float normY = ((mousePos.y - navRect.center.y) / (navRect.height * 0.5f));

            // Convert to pan offset
            float maxPanX = canvasDisplaySize * (canvasZoom - 1f) * 0.5f;
            float maxPanY = canvasDisplaySize * (canvasZoom - 1f) * 0.5f;

            return new Vector2(
                Mathf.Clamp(-normX * maxPanX, -maxPanX, maxPanX),
                Mathf.Clamp(-normY * maxPanY, -maxPanY, maxPanY));
        }
    }
}
