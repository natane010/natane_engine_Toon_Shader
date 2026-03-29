using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// Selection modes for the mask texture selection tool.
    /// </summary>
    internal enum SelectionMode { Replace, Add, Subtract }

    /// <summary>
    /// Manages pixel selection mask for mask texture operations.
    /// マスクテクスチャ操作用のピクセル選択マスクを管理
    /// </summary>
    internal class MaskTextureSelection
    {
        private bool[] mask;
        private int width;
        private int height;
        private bool hasSelection;

        public bool HasSelection => hasSelection;
        public int Width => width;
        public int Height => height;

        public MaskTextureSelection(int width, int height)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
            mask = new bool[this.width * this.height];
            hasSelection = false;
        }

        public bool IsSelected(int x, int y)
        {
            if (!hasSelection) return true; // No selection = all selected
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            return mask[y * width + x];
        }

        public void Clear()
        {
            for (int i = 0; i < mask.Length; i++)
                mask[i] = false;
            hasSelection = false;
        }

        public void SelectAll()
        {
            for (int i = 0; i < mask.Length; i++)
                mask[i] = true;
            hasSelection = true;
        }

        public void Invert()
        {
            for (int i = 0; i < mask.Length; i++)
                mask[i] = !mask[i];
            hasSelection = true;
            // Check if anything is selected
            bool any = false;
            for (int i = 0; i < mask.Length; i++)
                if (mask[i]) { any = true; break; }
            hasSelection = any;
        }

        /// <summary>
        /// Select pixels within a rectangle (in pixel coordinates).
        /// </summary>
        public void SelectRect(int minX, int minY, int maxX, int maxY, SelectionMode mode)
        {
            minX = Mathf.Clamp(minX, 0, width - 1);
            minY = Mathf.Clamp(minY, 0, height - 1);
            maxX = Mathf.Clamp(maxX, 0, width - 1);
            maxY = Mathf.Clamp(maxY, 0, height - 1);

            if (mode == SelectionMode.Replace)
                Clear();

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int idx = y * width + x;
                    switch (mode)
                    {
                        case SelectionMode.Replace:
                        case SelectionMode.Add:
                            mask[idx] = true;
                            break;
                        case SelectionMode.Subtract:
                            mask[idx] = false;
                            break;
                    }
                }
            }
            UpdateHasSelection();
        }

        /// <summary>
        /// Select pixels inside a polygon defined by points (in pixel coordinates).
        /// ポリゴン内のピクセルを選択（投げ縄選択）
        /// </summary>
        public void SelectLasso(List<Vector2> points, SelectionMode mode)
        {
            if (points == null || points.Count < 3) return;

            if (mode == SelectionMode.Replace)
                Clear();

            // Get bounding box of polygon
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Mathf.Min(minX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxX = Mathf.Max(maxX, p.x);
                maxY = Mathf.Max(maxY, p.y);
            }

            int iMinX = Mathf.Max(0, Mathf.FloorToInt(minX));
            int iMinY = Mathf.Max(0, Mathf.FloorToInt(minY));
            int iMaxX = Mathf.Min(width - 1, Mathf.CeilToInt(maxX));
            int iMaxY = Mathf.Min(height - 1, Mathf.CeilToInt(maxY));

            for (int y = iMinY; y <= iMaxY; y++)
            {
                for (int x = iMinX; x <= iMaxX; x++)
                {
                    if (PointInPolygon(new Vector2(x + 0.5f, y + 0.5f), points))
                    {
                        int idx = y * width + x;
                        switch (mode)
                        {
                            case SelectionMode.Replace:
                            case SelectionMode.Add:
                                mask[idx] = true;
                                break;
                            case SelectionMode.Subtract:
                                mask[idx] = false;
                                break;
                        }
                    }
                }
            }
            UpdateHasSelection();
        }

        /// <summary>
        /// Get the bounding rect of the current selection.
        /// </summary>
        public Rect GetBounds()
        {
            if (!hasSelection) return new Rect(0, 0, width, height);
            int minX = width, minY = height, maxX = 0, maxY = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (mask[y * width + x])
                    {
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private void UpdateHasSelection()
        {
            hasSelection = false;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i]) { hasSelection = true; return; }
            }
        }

        /// <summary>
        /// Point-in-polygon test using ray casting algorithm.
        /// </summary>
        private static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            bool inside = false;
            int j = polygon.Count - 1;
            for (int i = 0; i < polygon.Count; j = i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }

    /// <summary>
    /// Renders selection overlay (marching ants or semi-transparent mask).
    /// 選択範囲のオーバーレイ描画
    /// </summary>
    internal static class SelectionRenderer
    {
        /// <summary>
        /// Draw selection overlay on the canvas.
        /// </summary>
        public static void DrawSelectionOverlay(
            Rect canvasRect, MaskTextureSelection selection,
            int texWidth, int texHeight, float zoom, Vector2 pan)
        {
            if (selection == null || !selection.HasSelection) return;

            Rect bounds = selection.GetBounds();
            // Convert pixel bounds to canvas coordinates
            float scaleX = canvasRect.width * zoom / texWidth;
            float scaleY = canvasRect.height * zoom / texHeight;
            float offsetX = canvasRect.x + (canvasRect.width - canvasRect.width * zoom) * 0.5f + pan.x;
            float offsetY = canvasRect.y + (canvasRect.height - canvasRect.height * zoom) * 0.5f + pan.y;

            // Draw dashed rectangle for selection bounds
            Rect selRect = new Rect(
                offsetX + bounds.x * scaleX,
                offsetY + (texHeight - bounds.y - bounds.height) * scaleY,
                bounds.width * scaleX,
                bounds.height * scaleY);

            // Draw selection border
            Handles.color = Color.white;
            Vector3[] corners = new Vector3[5]
            {
                new Vector3(selRect.xMin, selRect.yMin, 0),
                new Vector3(selRect.xMax, selRect.yMin, 0),
                new Vector3(selRect.xMax, selRect.yMax, 0),
                new Vector3(selRect.xMin, selRect.yMax, 0),
                new Vector3(selRect.xMin, selRect.yMin, 0),
            };
            Handles.DrawDottedLines(corners, 4f);

            // Semi-transparent overlay outside selection
            Color overlayColor = new Color(0f, 0f, 0f, 0.3f);
            // Top
            EditorGUI.DrawRect(new Rect(canvasRect.x, canvasRect.y, canvasRect.width, selRect.y - canvasRect.y), overlayColor);
            // Bottom
            EditorGUI.DrawRect(new Rect(canvasRect.x, selRect.yMax, canvasRect.width, canvasRect.yMax - selRect.yMax), overlayColor);
            // Left
            EditorGUI.DrawRect(new Rect(canvasRect.x, selRect.y, selRect.x - canvasRect.x, selRect.height), overlayColor);
            // Right
            EditorGUI.DrawRect(new Rect(selRect.xMax, selRect.y, canvasRect.xMax - selRect.xMax, selRect.height), overlayColor);
        }

        /// <summary>
        /// Draw rectangle selection preview during drag.
        /// </summary>
        public static void DrawRectSelectionPreview(Vector2 start, Vector2 end)
        {
            Rect rect = new Rect(
                Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y),
                Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));

            EditorGUI.DrawRect(rect, new Color(0.3f, 0.6f, 1f, 0.15f));
            Handles.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            Handles.DrawWireCube(
                new Vector3(rect.center.x, rect.center.y, 0),
                new Vector3(rect.width, rect.height, 0));
        }

        /// <summary>
        /// Draw lasso selection preview with polygon points.
        /// </summary>
        public static void DrawLassoPreview(List<Vector2> points)
        {
            if (points == null || points.Count < 2) return;
            Handles.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            for (int i = 1; i < points.Count; i++)
            {
                Handles.DrawLine(
                    new Vector3(points[i - 1].x, points[i - 1].y, 0),
                    new Vector3(points[i].x, points[i].y, 0));
            }
            // Close the polygon
            if (points.Count > 2)
            {
                Handles.DrawDottedLine(
                    new Vector3(points[points.Count - 1].x, points[points.Count - 1].y, 0),
                    new Vector3(points[0].x, points[0].y, 0), 3f);
            }
        }
    }
}
