using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Reference image overlay for the texture studio canvas.
    /// テクスチャスタジオキャンバス用参考画像オーバーレイ
    /// </summary>
    internal class ReferenceImageOverlay
    {
        private Texture2D referenceImage;
        private float opacity = 0.5f;
        private Vector2 position = Vector2.zero;
        private float scale = 1f;
        private bool visible = true;
        private bool isDragging;
        private Vector2 dragStartMouse;
        private Vector2 dragStartPos;

        public bool HasImage => referenceImage != null;
        public bool Visible { get => visible; set => visible = value; }

        public void SetImage(Texture2D image)
        {
            referenceImage = image;
            if (image != null) visible = true;
        }

        public void ClearImage()
        {
            referenceImage = null;
            visible = false;
        }

        /// <summary>
        /// Draw the reference image overlay on the canvas.
        /// キャンバスに参考画像オーバーレイを描画
        /// </summary>
        public void Draw(Rect canvasArea)
        {
            if (!visible || referenceImage == null) return;

            float w = referenceImage.width * scale;
            float h = referenceImage.height * scale;
            Rect imgRect = new Rect(
                canvasArea.x + position.x,
                canvasArea.y + position.y,
                w, h);

            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, opacity);
            GUI.DrawTexture(imgRect, referenceImage, ScaleMode.ScaleToFit);
            GUI.color = prevColor;

            // Draw border when hovered
            if (canvasArea.Contains(Event.current.mousePosition))
            {
                Color borderColor = new Color(1f, 0.8f, 0f, 0.4f * opacity);
                EditorGUI.DrawRect(new Rect(imgRect.x, imgRect.y, imgRect.width, 1), borderColor);
                EditorGUI.DrawRect(new Rect(imgRect.x, imgRect.yMax - 1, imgRect.width, 1), borderColor);
                EditorGUI.DrawRect(new Rect(imgRect.x, imgRect.y, 1, imgRect.height), borderColor);
                EditorGUI.DrawRect(new Rect(imgRect.xMax - 1, imgRect.y, 1, imgRect.height), borderColor);
            }
        }

        /// <summary>
        /// Handle drag to reposition the reference image.
        /// ドラッグで参考画像を移動
        /// </summary>
        public bool HandleInput(Rect canvasArea)
        {
            if (!visible || referenceImage == null) return false;

            Event e = Event.current;
            float w = referenceImage.width * scale;
            float h = referenceImage.height * scale;
            Rect imgRect = new Rect(
                canvasArea.x + position.x,
                canvasArea.y + position.y,
                w, h);

            if (e.type == EventType.MouseDown && e.button == 0 && e.alt && imgRect.Contains(e.mousePosition))
            {
                isDragging = true;
                dragStartMouse = e.mousePosition;
                dragStartPos = position;
                e.Use();
                return true;
            }

            if (e.type == EventType.MouseDrag && isDragging)
            {
                position = dragStartPos + (e.mousePosition - dragStartMouse);
                e.Use();
                return true;
            }

            if (e.type == EventType.MouseUp && isDragging)
            {
                isDragging = false;
                e.Use();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Draw reference image settings UI.
        /// 参考画像設定UIを描画
        /// </summary>
        public void DrawSettingsUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("参考画像", "Reference Image"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            referenceImage = (Texture2D)EditorGUILayout.ObjectField(
                L("画像", "Image"), referenceImage, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && referenceImage != null)
                visible = true;

            if (referenceImage != null)
            {
                visible = EditorGUILayout.Toggle(L("表示", "Visible"), visible);
                opacity = EditorGUILayout.Slider(L("不透明度", "Opacity"), opacity, 0f, 1f);
                scale = EditorGUILayout.Slider(L("スケール", "Scale"), scale, 0.1f, 3f);
                EditorGUILayout.HelpBox(
                    L("Alt+ドラッグで移動", "Alt+Drag to reposition"),
                    MessageType.Info);

                if (GUILayout.Button(L("リセット", "Reset")))
                {
                    position = Vector2.zero;
                    scale = 1f;
                    opacity = 0.5f;
                }
            }
            EditorGUILayout.EndVertical();
        }
    }
}
