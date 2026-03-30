using UnityEngine;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// Flood fill (bucket) tool for mask texture painting.
    /// マスクテクスチャ用フラッドフィル（バケツ）ツール
    /// </summary>
    internal static class MaskTextureFillTool
    {
        /// <summary>
        /// Apply flood fill starting from the given pixel position.
        /// 指定ピクセル位置からフラッドフィルを適用
        /// </summary>
        public static void FloodFill(
            Color[] pixels, int width, int height,
            int startX, int startY,
            float fillValue, float fillAlpha,
            float tolerance, bool contiguous)
        {
            if (pixels == null || width <= 0 || height <= 0) return;
            startX = Mathf.Clamp(startX, 0, width - 1);
            startY = Mathf.Clamp(startY, 0, height - 1);

            Color targetColor = pixels[startY * width + startX];
            Color fillColor = new Color(fillValue, fillValue, fillValue, fillAlpha);

            if (contiguous)
                FloodFillContiguous(pixels, width, height, startX, startY, targetColor, fillColor, tolerance);
            else
                FloodFillGlobal(pixels, width, height, targetColor, fillColor, tolerance);
        }

        /// <summary>
        /// Apply flood fill with a full Color value.
        /// フルカラー値でフラッドフィルを適用
        /// </summary>
        public static void FloodFill(
            Color[] pixels, int width, int height,
            int startX, int startY,
            Color fillColor,
            float tolerance, bool contiguous)
        {
            if (pixels == null || width <= 0 || height <= 0) return;
            startX = Mathf.Clamp(startX, 0, width - 1);
            startY = Mathf.Clamp(startY, 0, height - 1);

            Color targetColor = pixels[startY * width + startX];
            if (contiguous)
                FloodFillContiguous(pixels, width, height, startX, startY, targetColor, fillColor, tolerance);
            else
                FloodFillGlobal(pixels, width, height, targetColor, fillColor, tolerance);
        }

        private static void FloodFillContiguous(
            Color[] pixels, int width, int height,
            int startX, int startY, Color targetColor, Color fillColor, float tolerance)
        {
            bool[] visited = new bool[width * height];
            Queue<int> queue = new Queue<int>();

            int startIdx = startY * width + startX;
            queue.Enqueue(startIdx);
            visited[startIdx] = true;

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                int x = idx % width;
                int y = idx / width;

                pixels[idx] = fillColor;

                // Check 4 neighbors
                TryEnqueue(queue, visited, pixels, width, height, x + 1, y, targetColor, tolerance);
                TryEnqueue(queue, visited, pixels, width, height, x - 1, y, targetColor, tolerance);
                TryEnqueue(queue, visited, pixels, width, height, x, y + 1, targetColor, tolerance);
                TryEnqueue(queue, visited, pixels, width, height, x, y - 1, targetColor, tolerance);
            }
        }

        private static void TryEnqueue(
            Queue<int> queue, bool[] visited, Color[] pixels,
            int width, int height, int x, int y,
            Color targetColor, float tolerance)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            int idx = y * width + x;
            if (visited[idx]) return;

            if (ColorDistance(pixels[idx], targetColor) <= tolerance)
            {
                visited[idx] = true;
                queue.Enqueue(idx);
            }
        }

        private static void FloodFillGlobal(
            Color[] pixels, int width, int height,
            Color targetColor, Color fillColor, float tolerance)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                if (ColorDistance(pixels[i], targetColor) <= tolerance)
                    pixels[i] = fillColor;
            }
        }

        private static float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            float da = a.a - b.a;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db + da * da);
        }

        /// <summary>
        /// Fill area with a tiling pattern texture.
        /// タイリングパターンテクスチャで領域を塗りつぶし
        /// </summary>
        public static void PatternFill(Color[] pixels, int width, int height, Texture2D pattern, float patternScale = 1f, float opacity = 1f)
        {
            if (pixels == null || pattern == null || width <= 0 || height <= 0) return;
            if (patternScale <= 0f) patternScale = 1f;

            int patW = pattern.width;
            int patH = pattern.height;
            Color[] patPixels = pattern.GetPixels();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Tiling: wrap pattern coordinates
                    // タイリング: パターン座標をラップ
                    int px = Mathf.FloorToInt((x / patternScale) % patW);
                    int py = Mathf.FloorToInt((y / patternScale) % patH);
                    if (px < 0) px += patW;
                    if (py < 0) py += patH;

                    Color patColor = patPixels[py * patW + px];
                    int idx = y * width + x;
                    pixels[idx] = Color.Lerp(pixels[idx], patColor, patColor.a * opacity);
                }
            }
        }

        /// <summary>
        /// Fill selection mask area with a tiling pattern.
        /// 選択マスク領域をタイリングパターンで塗りつぶし
        /// </summary>
        public static void PatternFillMasked(Color[] pixels, bool[] selectionMask, int width, int height, Texture2D pattern, float patternScale = 1f, float opacity = 1f)
        {
            if (pixels == null || selectionMask == null || pattern == null) return;
            if (width <= 0 || height <= 0) return;
            if (patternScale <= 0f) patternScale = 1f;

            int patW = pattern.width;
            int patH = pattern.height;
            Color[] patPixels = pattern.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                if (!selectionMask[i]) continue;
                int x = i % width, y = i / width;
                int px = Mathf.FloorToInt((x / patternScale) % patW);
                int py = Mathf.FloorToInt((y / patternScale) % patH);
                if (px < 0) px += patW;
                if (py < 0) py += patH;
                Color patColor = patPixels[py * patW + px];
                pixels[i] = Color.Lerp(pixels[i], patColor, patColor.a * opacity);
            }
        }
    }

    /// <summary>
    /// Settings for the fill tool.
    /// </summary>
    [System.Serializable]
    internal class FillToolSettings
    {
        public float fillValue = 1f;
        public float fillAlpha = 1f;
        public float tolerance = 0.1f;
        public bool contiguous = true;
        public Color fillColor = Color.white;
        public bool colorMode = false;
    }
}
