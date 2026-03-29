using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    internal enum SymmetryMode { None, HorizontalMirror, VerticalMirror, Both, Radial }

    /// <summary>
    /// Symmetry drawing system for mirror/radial brush strokes.
    /// ミラー/放射状ブラシストローク用対称描画システム
    /// </summary>
    internal static class SymmetryDrawing
    {
        public static SymmetryMode Mode { get; set; } = SymmetryMode.None;
        public static int RadialCount { get; set; } = 6;
        public static Vector2 SymmetryCenter { get; set; } = new Vector2(0.5f, 0.5f);

        /// <summary>
        /// Get all mirrored positions for a given pixel position.
        /// 指定ピクセル位置のミラー位置をすべて取得
        /// </summary>
        public static Vector2[] GetMirroredPositions(Vector2 pixelPos, int width, int height)
        {
            if (Mode == SymmetryMode.None)
                return new[] { pixelPos };

            float cx = SymmetryCenter.x * width;
            float cy = SymmetryCenter.y * height;

            switch (Mode)
            {
                case SymmetryMode.HorizontalMirror:
                    return new[]
                    {
                        pixelPos,
                        new Vector2(2f * cx - pixelPos.x, pixelPos.y)
                    };
                case SymmetryMode.VerticalMirror:
                    return new[]
                    {
                        pixelPos,
                        new Vector2(pixelPos.x, 2f * cy - pixelPos.y)
                    };
                case SymmetryMode.Both:
                    float mx = 2f * cx - pixelPos.x;
                    float my = 2f * cy - pixelPos.y;
                    return new[]
                    {
                        pixelPos,
                        new Vector2(mx, pixelPos.y),
                        new Vector2(pixelPos.x, my),
                        new Vector2(mx, my)
                    };
                case SymmetryMode.Radial:
                    int count = Mathf.Max(2, RadialCount);
                    Vector2[] positions = new Vector2[count];
                    float dx = pixelPos.x - cx;
                    float dy = pixelPos.y - cy;
                    float baseAngle = Mathf.Atan2(dy, dx);
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float step = 2f * Mathf.PI / count;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = baseAngle + step * i;
                        positions[i] = new Vector2(
                            cx + Mathf.Cos(angle) * radius,
                            cy + Mathf.Sin(angle) * radius);
                    }
                    return positions;
                default:
                    return new[] { pixelPos };
            }
        }

        /// <summary>
        /// Draw symmetry guide lines on the canvas.
        /// キャンバス上に対称ガイドラインを描画
        /// </summary>
        public static void DrawGuideLines(Rect canvasRect, int width, int height)
        {
            if (Mode == SymmetryMode.None) return;

            float cx = canvasRect.x + SymmetryCenter.x * canvasRect.width;
            float cy = canvasRect.y + (1f - SymmetryCenter.y) * canvasRect.height;
            Color guideColor = new Color(0f, 1f, 1f, 0.5f);
            Handles.color = guideColor;

            if (Mode == SymmetryMode.HorizontalMirror || Mode == SymmetryMode.Both)
            {
                Handles.DrawLine(
                    new Vector3(cx, canvasRect.y, 0),
                    new Vector3(cx, canvasRect.yMax, 0));
            }
            if (Mode == SymmetryMode.VerticalMirror || Mode == SymmetryMode.Both)
            {
                Handles.DrawLine(
                    new Vector3(canvasRect.x, cy, 0),
                    new Vector3(canvasRect.xMax, cy, 0));
            }
            if (Mode == SymmetryMode.Radial)
            {
                int count = Mathf.Max(2, RadialCount);
                float step = 2f * Mathf.PI / count;
                float lineLen = Mathf.Max(canvasRect.width, canvasRect.height);
                for (int i = 0; i < count; i++)
                {
                    float angle = step * i;
                    Vector3 endPt = new Vector3(
                        cx + Mathf.Cos(angle) * lineLen,
                        cy - Mathf.Sin(angle) * lineLen, 0);
                    Handles.DrawLine(new Vector3(cx, cy, 0), endPt);
                }
            }
        }

        public static void DrawSettingsUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Mode = (SymmetryMode)EditorGUILayout.EnumPopup(
                    L("対称モード", "Symmetry"), Mode);
                if (Mode == SymmetryMode.Radial)
                    RadialCount = EditorGUILayout.IntSlider(
                        L("分割数", "Divisions"), RadialCount, 2, 16);
                if (Mode != SymmetryMode.None)
                    SymmetryCenter = EditorGUILayout.Vector2Field(
                        L("中心 (UV)", "Center (UV)"), SymmetryCenter);
            }
        }
    }
}
