using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    internal enum GradientMode { Linear, Radial, Angle }
    internal enum GradientColorMode { ForegroundToBackground, ForegroundToTransparent }

    /// <summary>
    /// Canvas gradient tool for drawing gradients directly on layers.
    /// レイヤーに直接グラデーションを描画するキャンバスグラデーションツール
    /// </summary>
    internal static class GradientTool
    {
        private static Vector2 startPoint;
        private static Vector2 endPoint;
        private static bool isDragging;
        private static Color[] previewBackup;

        public static GradientMode Mode { get; set; } = GradientMode.Linear;
        public static GradientColorMode ColorMode { get; set; } = GradientColorMode.ForegroundToBackground;

        public static bool IsDragging => isDragging;
        public static Vector2 StartPoint => startPoint;
        public static Vector2 EndPoint => endPoint;

        public static void BeginDrag(Vector2 pixelPos, Color[] pixels)
        {
            startPoint = pixelPos;
            endPoint = pixelPos;
            isDragging = true;
            if (pixels != null)
            {
                previewBackup = new Color[pixels.Length];
                System.Array.Copy(pixels, previewBackup, pixels.Length);
            }
        }

        public static void UpdateDrag(Vector2 pixelPos)
        {
            if (!isDragging) return;
            endPoint = pixelPos;
        }

        public static void EndDrag(Color[] pixels, int width, int height, Color fg, Color bg)
        {
            if (!isDragging) return;
            isDragging = false;

            // Restore backup then apply final gradient
            if (previewBackup != null && pixels != null)
                System.Array.Copy(previewBackup, pixels, pixels.Length);

            ApplyGradient(pixels, width, height, startPoint, endPoint, fg, bg);
            previewBackup = null;
        }

        public static void CancelDrag(Color[] pixels)
        {
            if (!isDragging) return;
            isDragging = false;
            if (previewBackup != null && pixels != null)
                System.Array.Copy(previewBackup, pixels, pixels.Length);
            previewBackup = null;
        }

        public static void ApplyGradient(
            Color[] pixels, int width, int height,
            Vector2 start, Vector2 end,
            Color startColor, Color endColor)
        {
            if (pixels == null || width <= 0 || height <= 0) return;

            float dist = Vector2.Distance(start, end);
            if (dist < 0.001f) return;

            Color cEnd = ColorMode == GradientColorMode.ForegroundToTransparent
                ? new Color(startColor.r, startColor.g, startColor.b, 0f) : endColor;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = CalculateGradientT(new Vector2(x, y), start, end, dist);
                    t = Mathf.Clamp01(t);
                    Color gradColor = Color.Lerp(startColor, cEnd, t);
                    int idx = y * width + x;
                    // Alpha composite
                    float srcA = gradColor.a;
                    float dstA = pixels[idx].a;
                    float outA = srcA + dstA * (1f - srcA);
                    if (outA > 0.0001f)
                    {
                        float outR = (gradColor.r * srcA + pixels[idx].r * dstA * (1f - srcA)) / outA;
                        float outG = (gradColor.g * srcA + pixels[idx].g * dstA * (1f - srcA)) / outA;
                        float outB = (gradColor.b * srcA + pixels[idx].b * dstA * (1f - srcA)) / outA;
                        pixels[idx] = new Color(outR, outG, outB, outA);
                    }
                }
            }
        }

        private static float CalculateGradientT(Vector2 point, Vector2 start, Vector2 end, float length)
        {
            switch (Mode)
            {
                case GradientMode.Linear:
                    Vector2 dir = (end - start) / length;
                    return Vector2.Dot(point - start, dir) / length;
                case GradientMode.Radial:
                    return Vector2.Distance(point, start) / length;
                case GradientMode.Angle:
                    Vector2 refDir = (end - start).normalized;
                    Vector2 pDir = (point - start);
                    float angle = Mathf.Atan2(pDir.y, pDir.x) - Mathf.Atan2(refDir.y, refDir.x);
                    if (angle < 0) angle += 2f * Mathf.PI;
                    return angle / (2f * Mathf.PI);
                default:
                    return 0f;
            }
        }

        public static void DrawSettingsUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Mode = (GradientMode)EditorGUILayout.EnumPopup(
                    L("グラデーション", "Gradient"), Mode);
                ColorMode = (GradientColorMode)EditorGUILayout.EnumPopup(
                    L("カラーモード", "Color Mode"), ColorMode);
                EditorGUILayout.HelpBox(
                    L("キャンバス上で2点をドラッグしてグラデーションを描画",
                      "Drag two points on canvas to draw gradient"),
                    MessageType.Info);
            }
        }
    }
}
