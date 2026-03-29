using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    internal enum TilingMode { Off, Tile2x2, Tile3x3 }

    /// <summary>
    /// Tiling preview for seamless texture verification.
    /// シームレステクスチャ検証用タイリングプレビュー
    /// </summary>
    internal static class TilingPreview
    {
        public static TilingMode Mode { get; set; } = TilingMode.Off;
        public static bool IsEnabled => Mode != TilingMode.Off;
        public static int TileCount => Mode == TilingMode.Tile2x2 ? 2 : Mode == TilingMode.Tile3x3 ? 3 : 1;

        /// <summary>
        /// Draw tiled texture preview.
        /// タイルテクスチャプレビューを描画
        /// </summary>
        public static void DrawTiledPreview(Rect canvasArea, Texture2D texture)
        {
            if (Mode == TilingMode.Off || texture == null) return;

            int tiles = TileCount;
            float tileSize = Mathf.Min(canvasArea.width, canvasArea.height) / tiles;

            // Draw dim overlay on main canvas
            EditorGUI.DrawRect(canvasArea, new Color(0f, 0f, 0f, 0.3f));

            // Draw tiled grid
            for (int ty = 0; ty < tiles; ty++)
            {
                for (int tx = 0; tx < tiles; tx++)
                {
                    Rect tileRect = new Rect(
                        canvasArea.x + tx * tileSize,
                        canvasArea.y + ty * tileSize,
                        tileSize, tileSize);
                    EditorGUI.DrawPreviewTexture(tileRect, texture);

                    // Tile border
                    Color borderColor = new Color(1f, 1f, 0f, 0.3f);
                    EditorGUI.DrawRect(new Rect(tileRect.x, tileRect.y, tileRect.width, 1), borderColor);
                    EditorGUI.DrawRect(new Rect(tileRect.x, tileRect.y, 1, tileRect.height), borderColor);
                }
            }
        }

        public static Rect GetInteractiveTextureRect(Rect canvasArea, Rect defaultTextureRect)
        {
            if (!IsEnabled)
                return defaultTextureRect;

            float tileSize = Mathf.Min(canvasArea.width, canvasArea.height) / TileCount;
            return new Rect(canvasArea.x, canvasArea.y, tileSize, tileSize);
        }

        /// <summary>
        /// Draw tiling mode toggle in toolbar.
        /// ツールバーにタイリングモードトグルを描画
        /// </summary>
        public static void DrawToolbarToggle()
        {
            string label = Mode == TilingMode.Off ? "1x1" : Mode == TilingMode.Tile2x2 ? "2x2" : "3x3";
            if (GUILayout.Button(new GUIContent(label, L("タイリングプレビュー切替", "Toggle tiling preview")),
                EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                switch (Mode)
                {
                    case TilingMode.Off: Mode = TilingMode.Tile2x2; break;
                    case TilingMode.Tile2x2: Mode = TilingMode.Tile3x3; break;
                    case TilingMode.Tile3x3: Mode = TilingMode.Off; break;
                }
            }
        }
    }
}
