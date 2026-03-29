using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// Tracks the dirty (modified) rectangular region of a canvas during brush strokes.
    /// ブラシストローク中にキャンバスの変更矩形領域を追跡するユーティリティ
    /// </summary>
    internal struct DirtyRect
    {
        public int x, y, width, height;
        public bool isEmpty;

        /// <summary>
        /// An empty dirty rect with no modified region.
        /// 変更領域なしの空ダーティ矩形
        /// </summary>
        public static DirtyRect Empty => new DirtyRect { isEmpty = true };

        /// <summary>
        /// Expand the dirty rect to include a circle at (px, py) with given radius.
        /// 指定半径の円を含むようにダーティ矩形を拡張する
        /// </summary>
        public void Expand(int px, int py, int radius)
        {
            int minX = px - radius;
            int minY = py - radius;
            int maxX = px + radius;
            int maxY = py + radius;

            if (isEmpty)
            {
                x = minX;
                y = minY;
                width = maxX - minX + 1;
                height = maxY - minY + 1;
                isEmpty = false;
            }
            else
            {
                int curMaxX = x + width - 1;
                int curMaxY = y + height - 1;
                int newMinX = Mathf.Min(x, minX);
                int newMinY = Mathf.Min(y, minY);
                int newMaxX = Mathf.Max(curMaxX, maxX);
                int newMaxY = Mathf.Max(curMaxY, maxY);
                x = newMinX;
                y = newMinY;
                width = newMaxX - newMinX + 1;
                height = newMaxY - newMinY + 1;
            }
        }

        /// <summary>
        /// Union with another rectangle.
        /// 他の矩形との和集合を取る
        /// </summary>
        public void ExpandRect(int rx, int ry, int rw, int rh)
        {
            if (rw <= 0 || rh <= 0) return;

            if (isEmpty)
            {
                x = rx;
                y = ry;
                width = rw;
                height = rh;
                isEmpty = false;
            }
            else
            {
                int curMaxX = x + width;
                int curMaxY = y + height;
                int newMinX = Mathf.Min(x, rx);
                int newMinY = Mathf.Min(y, ry);
                int newMaxX = Mathf.Max(curMaxX, rx + rw);
                int newMaxY = Mathf.Max(curMaxY, ry + rh);
                x = newMinX;
                y = newMinY;
                width = newMaxX - newMinX;
                height = newMaxY - newMinY;
            }
        }

        /// <summary>
        /// Clamp to canvas bounds and return a new rect.
        /// キャンバス境界にクランプした新しい矩形を返す
        /// </summary>
        public DirtyRect Clamp(int canvasWidth, int canvasHeight)
        {
            if (isEmpty) return Empty;

            int clampedX = Mathf.Max(0, x);
            int clampedY = Mathf.Max(0, y);
            int maxX = Mathf.Min(x + width, canvasWidth);
            int maxY = Mathf.Min(y + height, canvasHeight);

            int clampedW = maxX - clampedX;
            int clampedH = maxY - clampedY;

            if (clampedW <= 0 || clampedH <= 0)
                return Empty;

            return new DirtyRect
            {
                x = clampedX,
                y = clampedY,
                width = clampedW,
                height = clampedH,
                isEmpty = false
            };
        }

        /// <summary>
        /// Reset to empty state.
        /// 空状態にリセットする
        /// </summary>
        public void Reset()
        {
            isEmpty = true;
            x = 0;
            y = 0;
            width = 0;
            height = 0;
        }

        public override string ToString()
        {
            return isEmpty ? "DirtyRect(Empty)" : $"DirtyRect({x}, {y}, {width}x{height})";
        }
    }
}
