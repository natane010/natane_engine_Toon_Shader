using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    internal static class ZoomController
    {
        private static readonly float[] ZoomPresets = { 25f, 50f, 100f, 150f, 200f, 400f, 800f };
        private static GUIStyle zoomLabelStyle;
        private static GUIStyle zoomButtonStyle;

        private static void EnsureStyles()
        {
            if (zoomLabelStyle != null) return;
            zoomLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            zoomButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10,
                padding = new RectOffset(2, 2, 1, 1),
                fixedHeight = 18
            };
        }

        /// <summary>
        /// Draw zoom controls in toolbar. Returns updated zoom value.
        /// ツールバーにズームコントロールを描画。更新されたズーム値を返す。
        /// </summary>
        public static float DrawToolbarZoom(float currentZoom)
        {
            EnsureStyles();
            float zoom = currentZoom;

            // Zoom out button (no Begin/EndHorizontal - caller provides the horizontal layout)
            if (GUILayout.Button("-", zoomButtonStyle, GUILayout.Width(20)))
                zoom = GetPreviousPreset(zoom);

            // Zoom slider
            float logZoom = Mathf.Log(zoom, 2f);
            float logMin = Mathf.Log(0.25f, 2f);
            float logMax = Mathf.Log(8f, 2f);
            EditorGUI.BeginChangeCheck();
            logZoom = GUILayout.HorizontalSlider(logZoom, logMin, logMax, GUILayout.Width(80));
            if (EditorGUI.EndChangeCheck())
                zoom = Mathf.Pow(2f, logZoom);

            // Zoom in button
            if (GUILayout.Button("+", zoomButtonStyle, GUILayout.Width(20)))
                zoom = GetNextPreset(zoom);

            // Zoom percentage display
            string zoomText = $"{zoom * 100f:F0}%";
            GUILayout.Label(zoomText, zoomLabelStyle, GUILayout.Width(48));

            // Fit button
            if (GUILayout.Button(L("全体", "Fit"), EditorStyles.miniButton, GUILayout.Width(32)))
                zoom = 1f;
            return zoom;
        }

        /// <summary>
        /// Draw floating zoom indicator on canvas.
        /// キャンバス上にフローティングズームインジケータを描画
        /// </summary>
        public static void DrawCanvasZoomBadge(Rect canvasArea, float zoom)
        {
            string text = $"{zoom * 100f:F0}%";
            Vector2 size = new Vector2(50, 18);
            Rect badgeRect = new Rect(
                canvasArea.xMax - size.x - 6,
                canvasArea.yMax - size.y - 6,
                size.x, size.y);

            EditorGUI.DrawRect(badgeRect, new Color(0f, 0f, 0f, 0.5f));
            GUI.Label(badgeRect, text, new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            });
        }

        public static float ZoomToPreset(float zoom, float targetPercent)
        {
            return targetPercent / 100f;
        }

        private static float GetNextPreset(float currentZoom)
        {
            float currentPercent = currentZoom * 100f;
            foreach (float preset in ZoomPresets)
                if (preset > currentPercent + 1f) return preset / 100f;
            return ZoomPresets[ZoomPresets.Length - 1] / 100f;
        }

        private static float GetPreviousPreset(float currentZoom)
        {
            float currentPercent = currentZoom * 100f;
            for (int i = ZoomPresets.Length - 1; i >= 0; i--)
                if (ZoomPresets[i] < currentPercent - 1f) return ZoomPresets[i] / 100f;
            return ZoomPresets[0] / 100f;
        }
    }
}
