using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// Lightweight toast notification system for the texture studio.
    /// テクスチャスタジオ用軽量トースト通知システム
    /// </summary>
    internal static class ToastNotification
    {
        private struct Toast
        {
            public string message;
            public double startTime;
            public float duration;
            public Color color;
        }

        private static readonly List<Toast> activeToasts = new List<Toast>();
        private static GUIStyle toastStyle;
        private const float DefaultDuration = 1.5f;
        private const float FadeOutTime = 0.4f;

        private static readonly Color DefaultBg = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        private static readonly Color SuccessBg = new Color(0.1f, 0.35f, 0.15f, 0.9f);
        private static readonly Color WarningBg = new Color(0.4f, 0.35f, 0.1f, 0.9f);

        private static void EnsureStyle()
        {
            if (toastStyle != null) return;
            toastStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 6, 6),
                wordWrap = false,
                normal = { textColor = Color.white }
            };
        }

        /// <summary>Show a toast notification.</summary>
        public static void Show(string message, float duration = DefaultDuration)
        {
            Show(message, DefaultBg, duration);
        }

        /// <summary>Show a success toast (green tinted).</summary>
        public static void ShowSuccess(string message)
        {
            Show(message, SuccessBg, DefaultDuration);
        }

        /// <summary>Show a warning toast (yellow tinted).</summary>
        public static void ShowWarning(string message)
        {
            Show(message, WarningBg, 2f);
        }

        public static void Show(string message, Color bgColor, float duration)
        {
            // Replace existing toast with same message
            for (int i = activeToasts.Count - 1; i >= 0; i--)
            {
                if (activeToasts[i].message == message)
                    activeToasts.RemoveAt(i);
            }
            activeToasts.Add(new Toast
            {
                message = message,
                startTime = EditorApplication.timeSinceStartup,
                duration = duration,
                color = bgColor
            });
        }

        /// <summary>
        /// Draw active toasts. Call this in OnGUI after canvas drawing.
        /// アクティブなトーストを描画。キャンバス描画後のOnGUIで呼び出す。
        /// </summary>
        public static void DrawToasts(Rect canvasArea)
        {
            EnsureStyle();
            double now = EditorApplication.timeSinceStartup;
            float yOffset = 8f;

            for (int i = activeToasts.Count - 1; i >= 0; i--)
            {
                var toast = activeToasts[i];
                float elapsed = (float)(now - toast.startTime);

                if (elapsed > toast.duration)
                {
                    activeToasts.RemoveAt(i);
                    continue;
                }

                // Calculate alpha (fade out in last FadeOutTime seconds)
                float alpha = 1f;
                float fadeStart = toast.duration - FadeOutTime;
                if (elapsed > fadeStart)
                    alpha = 1f - (elapsed - fadeStart) / FadeOutTime;

                // Measure text
                Vector2 textSize = toastStyle.CalcSize(new GUIContent(toast.message));
                float toastWidth = textSize.x + 24f;
                float toastHeight = 28f;

                // Position: top-center of canvas
                Rect toastRect = new Rect(
                    canvasArea.center.x - toastWidth * 0.5f,
                    canvasArea.y + yOffset,
                    toastWidth, toastHeight);

                // Draw background
                Color bgColor = toast.color;
                bgColor.a *= alpha;
                EditorGUI.DrawRect(toastRect, bgColor);

                // Draw rounded border
                Color borderColor = new Color(0.5f, 0.5f, 0.5f, 0.3f * alpha);
                EditorGUI.DrawRect(new Rect(toastRect.x, toastRect.y, toastRect.width, 1), borderColor);
                EditorGUI.DrawRect(new Rect(toastRect.x, toastRect.yMax - 1, toastRect.width, 1), borderColor);
                EditorGUI.DrawRect(new Rect(toastRect.x, toastRect.y, 1, toastRect.height), borderColor);
                EditorGUI.DrawRect(new Rect(toastRect.xMax - 1, toastRect.y, 1, toastRect.height), borderColor);

                // Draw text
                Color prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(toastRect, toast.message, toastStyle);
                GUI.color = prevColor;

                yOffset += toastHeight + 4f;
            }

            // Request repaint if toasts are active
            if (activeToasts.Count > 0 && EditorWindow.focusedWindow != null)
                EditorWindow.focusedWindow.Repaint();
        }

        /// <summary>Whether any toasts are currently visible.</summary>
        public static bool HasActiveToasts => activeToasts.Count > 0;
    }
}
