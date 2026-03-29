using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Quick mask system - paint a temporary mask that converts to selection.
    /// クイックマスクシステム - 一時マスクをペイントして選択範囲に変換
    /// </summary>
    internal class QuickMaskSystem
    {
        private bool active;
        private Color[] maskPixels;
        private int width, height;
        private Texture2D overlayTexture;
        private Color overlayColor = new Color(1f, 0f, 0f, 0.5f);

        public bool IsActive => active;
        public Color[] MaskPixels => maskPixels;

        /// <summary>
        /// Toggle quick mask mode on/off.
        /// クイックマスクモードの切替
        /// </summary>
        public void Toggle(int texWidth, int texHeight)
        {
            if (active)
            {
                Deactivate();
            }
            else
            {
                Activate(texWidth, texHeight);
            }
        }

        public void Activate(int texWidth, int texHeight)
        {
            width = texWidth;
            height = texHeight;
            maskPixels = new Color[width * height];
            // Initialize to transparent (no mask)
            for (int i = 0; i < maskPixels.Length; i++)
                maskPixels[i] = Color.clear;
            active = true;
        }

        public void Deactivate()
        {
            active = false;
            if (overlayTexture != null)
            {
                Object.DestroyImmediate(overlayTexture);
                overlayTexture = null;
            }
        }

        /// <summary>
        /// Convert the quick mask to a MaskTextureSelection.
        /// クイックマスクをMaskTextureSelectionに変換
        /// </summary>
        public bool[] ConvertToSelection()
        {
            if (maskPixels == null) return null;
            bool[] selection = new bool[maskPixels.Length];
            for (int i = 0; i < maskPixels.Length; i++)
            {
                // Painted areas (alpha > 0.5) become selected
                selection[i] = maskPixels[i].a > 0.5f;
            }
            Deactivate();
            return selection;
        }

        /// <summary>
        /// Draw red overlay on canvas showing the quick mask.
        /// キャンバスにクイックマスクの赤オーバーレイを描画
        /// </summary>
        public void DrawOverlay(Rect texRect)
        {
            if (!active || maskPixels == null) return;

            // Update overlay texture
            if (overlayTexture == null || overlayTexture.width != width || overlayTexture.height != height)
            {
                if (overlayTexture != null) Object.DestroyImmediate(overlayTexture);
                overlayTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                overlayTexture.filterMode = FilterMode.Point;
                overlayTexture.hideFlags = HideFlags.HideAndDontSave;
            }

            // Generate overlay pixels
            Color[] overlayPixels = new Color[width * height];
            for (int i = 0; i < maskPixels.Length; i++)
            {
                float maskVal = maskPixels[i].a;
                if (maskVal > 0.01f)
                    overlayPixels[i] = new Color(overlayColor.r, overlayColor.g, overlayColor.b, maskVal * overlayColor.a);
                else
                    overlayPixels[i] = Color.clear;
            }

            overlayTexture.SetPixels(overlayPixels);
            overlayTexture.Apply();

            // Draw overlay
            Color prevColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(texRect, overlayTexture);
            GUI.color = prevColor;
        }

        /// <summary>
        /// Draw quick mask status badge on canvas.
        /// キャンバスにクイックマスクステータスバッジを描画
        /// </summary>
        public void DrawStatusBadge(Rect canvasArea)
        {
            if (!active) return;

            Rect badgeRect = new Rect(canvasArea.x + 6, canvasArea.y + 6, 100, 22);
            EditorGUI.DrawRect(badgeRect, new Color(0.6f, 0f, 0f, 0.8f));
            GUI.Label(badgeRect, L(" Q マスク編集中", " Q Quick Mask"),
                new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                });
        }

        public void Dispose()
        {
            if (overlayTexture != null)
            {
                Object.DestroyImmediate(overlayTexture);
                overlayTexture = null;
            }
            maskPixels = null;
        }
    }
}
