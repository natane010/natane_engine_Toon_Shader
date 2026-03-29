using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// HSV color picker panel with color wheel, sliders, and history.
    /// HSVカラーホイール、スライダー、履歴付きカラーピッカーパネル
    /// </summary>
    internal class ColorPickerPanel
    {
        // Colors
        private Color foregroundColor = Color.white;
        private Color backgroundColor = Color.black;
        private float hue, saturation, value;

        // History
        private List<Color> colorHistory = new List<Color>();
        private const int MaxHistory = 16;

        // Textures
        private Texture2D wheelTexture;
        private Texture2D svTexture;
        private int wheelSize = 128;
        private int svSize = 100;
        private float lastGeneratedHue = -1f;

        // Interaction
        private bool isDraggingHue;
        private bool isDraggingSV;
        private string hexInput = "FFFFFFFF";
        private Vector2 panelScrollPosition;

        // Ring geometry
        private const float OuterRadiusFrac = 0.48f;
        private const float InnerRadiusFrac = 0.38f;

        // === Public API ===

        public Color ForegroundColor
        {
            get => foregroundColor;
            set
            {
                foregroundColor = value;
                Color.RGBToHSV(value, out hue, out saturation, out this.value);
                hexInput = ColorUtility.ToHtmlStringRGBA(value);
            }
        }

        public Color BackgroundColor
        {
            get => backgroundColor;
            set => backgroundColor = value;
        }

        public void SwapColors()
        {
            var tmp = foregroundColor;
            foregroundColor = backgroundColor;
            backgroundColor = tmp;
            Color.RGBToHSV(foregroundColor, out hue, out saturation, out value);
            hexInput = ColorUtility.ToHtmlStringRGBA(foregroundColor);
        }

        public void ResetDefaults()
        {
            foregroundColor = Color.white;
            backgroundColor = Color.black;
            Color.RGBToHSV(foregroundColor, out hue, out saturation, out value);
            hexInput = ColorUtility.ToHtmlStringRGBA(foregroundColor);
        }

        public void SetFromCanvas(Color picked)
        {
            ForegroundColor = picked;
            AddToHistory(picked);
        }

        public void AddToHistory(Color c)
        {
            // Remove duplicate if exists
            for (int i = colorHistory.Count - 1; i >= 0; i--)
            {
                if (ColorApproxEqual(colorHistory[i], c))
                {
                    colorHistory.RemoveAt(i);
                    break;
                }
            }
            colorHistory.Insert(0, c);
            if (colorHistory.Count > MaxHistory)
                colorHistory.RemoveAt(colorHistory.Count - 1);
        }

        private static bool ColorApproxEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f &&
                   Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.01f;
        }

        // === Drawing ===

        public void DrawPanel(float panelWidth, float maxHeight = 0f)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("カラー", "Color"), EditorStyles.boldLabel);

            bool useInternalScroll = maxHeight > 0f;
            float contentWidth = Mathf.Max(96f, panelWidth - (useInternalScroll ? 34f : 20f));
            if (useInternalScroll)
            {
                float scrollHeight = Mathf.Max(150f, maxHeight - 22f);
                panelScrollPosition = EditorGUILayout.BeginScrollView(
                    panelScrollPosition,
                    GUILayout.Height(scrollHeight),
                    GUILayout.ExpandWidth(true));
            }

            // Color wheel
            float wheelDisplaySize = Mathf.Clamp(contentWidth, 112f, 160f);
            Rect wheelRect = GUILayoutUtility.GetRect(wheelDisplaySize, wheelDisplaySize);
            DrawColorWheel(wheelRect);

            EditorGUILayout.Space(4);

            // Foreground / Background
            DrawForegroundBackgroundFields();

            EditorGUILayout.Space(4);

            // RGB Sliders
            DrawRGBSliders();

            EditorGUILayout.Space(2);

            // HSV Sliders
            DrawHSVSliders();

            EditorGUILayout.Space(2);

            // Hex input
            DrawHexInput();

            EditorGUILayout.Space(4);

            // Color history
            DrawColorHistory();

            if (useInternalScroll)
                EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // === Color Wheel ===

        private static Rect GetCenteredSquareRect(Rect rect)
        {
            float size = Mathf.Min(rect.width, rect.height);
            return new Rect(
                rect.x + (rect.width - size) * 0.5f,
                rect.y + (rect.height - size) * 0.5f,
                size,
                size);
        }

        private void DrawColorWheel(Rect rect)
        {
            rect = GetCenteredSquareRect(rect);
            EnsureWheelTexture();
            EnsureSVTexture();

            // Draw wheel
            if (wheelTexture != null)
                GUI.DrawTexture(rect, wheelTexture);

            // Draw SV square in center
            float innerSize = rect.width * InnerRadiusFrac * 2f * 0.7f;
            Rect svRect = new Rect(
                rect.center.x - innerSize * 0.5f,
                rect.center.y - innerSize * 0.5f,
                innerSize, innerSize);
            if (svTexture != null)
                GUI.DrawTexture(svRect, svTexture);

            // Draw hue marker on ring
            float hueAngle = hue * 2f * Mathf.PI;
            float ringRadius = rect.width * (OuterRadiusFrac + InnerRadiusFrac) * 0.5f;
            Vector2 center = rect.center;
            Vector2 hueMarker = center + new Vector2(
                Mathf.Cos(hueAngle) * ringRadius,
                -Mathf.Sin(hueAngle) * ringRadius);
            DrawCircleMarker(hueMarker, 5f, Color.white);

            // Draw SV marker
            Vector2 svPos = new Vector2(
                svRect.x + saturation * Mathf.Max(1f, svRect.width - 1f),
                svRect.y + (1f - value) * Mathf.Max(1f, svRect.height - 1f));
            DrawCircleMarker(svPos, 4f, value > 0.5f ? Color.black : Color.white);

            // Handle input
            HandleWheelInput(rect, svRect);
        }

        private void HandleWheelInput(Rect wheelRect, Rect svRect)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 mousePos = e.mousePosition;
                Vector2 center = wheelRect.center;
                float dist = Vector2.Distance(mousePos, center);
                float outerR = wheelRect.width * OuterRadiusFrac;
                float innerR = wheelRect.width * InnerRadiusFrac;

                if (dist >= innerR && dist <= outerR)
                {
                    isDraggingHue = true;
                    UpdateHueFromMouse(mousePos, center);
                    e.Use();
                }
                else if (svRect.Contains(mousePos))
                {
                    isDraggingSV = true;
                    UpdateSVFromMouse(mousePos, svRect);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDrag && e.button == 0)
            {
                if (isDraggingHue)
                {
                    UpdateHueFromMouse(e.mousePosition, wheelRect.center);
                    e.Use();
                }
                else if (isDraggingSV)
                {
                    UpdateSVFromMouse(e.mousePosition, svRect);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (isDraggingHue || isDraggingSV)
                {
                    isDraggingHue = false;
                    isDraggingSV = false;
                    AddToHistory(foregroundColor);
                    e.Use();
                }
            }
        }

        private void UpdateHueFromMouse(Vector2 mousePos, Vector2 center)
        {
            Vector2 dir = mousePos - center;
            float angle = Mathf.Atan2(-dir.y, dir.x);
            if (angle < 0) angle += 2f * Mathf.PI;
            hue = angle / (2f * Mathf.PI);
            UpdateColorFromHSV();
        }

        private void UpdateSVFromMouse(Vector2 mousePos, Rect svRect)
        {
            float svWidth = Mathf.Max(1f, svRect.width - 1f);
            float svHeight = Mathf.Max(1f, svRect.height - 1f);
            saturation = Mathf.Clamp01((mousePos.x - svRect.x) / svWidth);
            value = 1f - Mathf.Clamp01((mousePos.y - svRect.y) / svHeight);
            UpdateColorFromHSV();
        }

        private void UpdateColorFromHSV()
        {
            float alpha = foregroundColor.a;
            foregroundColor = Color.HSVToRGB(hue, saturation, value);
            foregroundColor.a = alpha;
            hexInput = ColorUtility.ToHtmlStringRGBA(foregroundColor);
        }

        // === Texture Generation ===

        private void EnsureWheelTexture()
        {
            if (wheelTexture != null) return;
            wheelTexture = new Texture2D(wheelSize, wheelSize, TextureFormat.RGBA32, false);
            wheelTexture.hideFlags = HideFlags.HideAndDontSave;

            float center = wheelSize * 0.5f;
            float outerR = wheelSize * OuterRadiusFrac;
            float innerR = wheelSize * InnerRadiusFrac;

            for (int y = 0; y < wheelSize; y++)
            {
                for (int x = 0; x < wheelSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist >= innerR && dist <= outerR)
                    {
                        // GUI Y grows downward, so invert Y here to match marker/input orientation.
                        float angle = Mathf.Atan2(-dy, dx);
                        if (angle < 0) angle += 2f * Mathf.PI;
                        float h = angle / (2f * Mathf.PI);
                        wheelTexture.SetPixel(x, y, Color.HSVToRGB(h, 1f, 1f));
                    }
                    else
                    {
                        wheelTexture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            wheelTexture.Apply();
        }

        private void EnsureSVTexture()
        {
            if (svTexture != null && Mathf.Abs(lastGeneratedHue - hue) < 0.001f) return;

            if (svTexture == null)
            {
                svTexture = new Texture2D(svSize, svSize, TextureFormat.RGBA32, false);
                svTexture.hideFlags = HideFlags.HideAndDontSave;
            }

            lastGeneratedHue = hue;
            for (int y = 0; y < svSize; y++)
            {
                // Texture y=0 is bottom (black/V=0), y=max is top (bright/V=1) matching Unity standard.
                float v = (float)y / (svSize - 1);
                for (int x = 0; x < svSize; x++)
                {
                    float s = (float)x / (svSize - 1);
                    svTexture.SetPixel(x, y, Color.HSVToRGB(hue, s, v));
                }
            }
            svTexture.Apply();
        }

        // === UI Components ===

        private void DrawForegroundBackground()
        {
            // Photoshop-style overlapping foreground/background preview
            Rect totalRect = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));
            float previewX = totalRect.x + (totalRect.width - 70) * 0.5f;

            // Background color (smaller, behind)
            Rect bgRect = new Rect(previewX + 22, totalRect.y + 14, 36, 36);
            EditorGUI.DrawRect(new Rect(bgRect.x - 1, bgRect.y - 1, bgRect.width + 2, bgRect.height + 2), Color.black);
            EditorGUI.DrawRect(bgRect, backgroundColor);

            // Foreground color (larger, in front)
            Rect fgRect = new Rect(previewX, totalRect.y, 40, 40);
            EditorGUI.DrawRect(new Rect(fgRect.x - 1, fgRect.y - 1, fgRect.width + 2, fgRect.height + 2), Color.black);
            EditorGUI.DrawRect(fgRect, foregroundColor);

            // Swap button (X) - small arrow icon
            Rect swapRect = new Rect(previewX + 44, totalRect.y, 18, 18);
            if (GUI.Button(swapRect, new GUIContent("\u21c4", L("前景色/背景色入替 (X)", "Swap FG/BG (X)")),
                new GUIStyle(EditorStyles.miniButton) { fontSize = 11, padding = new RectOffset(0, 0, 0, 0) }))
                SwapColors();

            // Reset button (D) - small
            Rect resetRect = new Rect(previewX - 16, totalRect.y + 34, 16, 16);
            if (GUI.Button(resetRect, new GUIContent("\u25fc", L("デフォルト色 (D)", "Default Colors (D)")),
                new GUIStyle(EditorStyles.miniButton) { fontSize = 8, padding = new RectOffset(0, 0, 0, 0) }))
                ResetDefaults();

            // Click to open color picker
            if (Event.current.type == EventType.MouseDown && fgRect.Contains(Event.current.mousePosition))
            {
                // Let Unity's color picker handle it
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseDown && bgRect.Contains(Event.current.mousePosition))
            {
                SwapColors(); // Quick: click BG to swap
                Event.current.Use();
            }
        }

        private void DrawForegroundBackgroundFields()
        {
            Rect totalRect = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
            float clusterWidth = 68f;
            float previewX = totalRect.x + Mathf.Max(0f, (totalRect.width - clusterWidth) * 0.5f);

            Rect bgRect = new Rect(previewX + 24f, totalRect.y + 10f, 34f, 34f);
            Rect fgRect = new Rect(previewX, totalRect.y, 38f, 38f);
            EditorGUI.DrawRect(new Rect(bgRect.x - 1f, bgRect.y - 1f, bgRect.width + 2f, bgRect.height + 2f), Color.black);
            EditorGUI.DrawRect(bgRect, backgroundColor);
            EditorGUI.DrawRect(new Rect(fgRect.x - 1f, fgRect.y - 1f, fgRect.width + 2f, fgRect.height + 2f), Color.black);
            EditorGUI.DrawRect(fgRect, foregroundColor);

            EditorGUI.BeginChangeCheck();
            Color newForeground = EditorGUILayout.ColorField(
                new GUIContent(L("前景色", "Foreground")),
                foregroundColor,
                true,
                true,
                false);
            Color newBackground = EditorGUILayout.ColorField(
                new GUIContent(L("背景色", "Background")),
                backgroundColor,
                true,
                true,
                false);
            if (EditorGUI.EndChangeCheck())
            {
                ForegroundColor = newForeground;
                BackgroundColor = newBackground;
                AddToHistory(foregroundColor);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("入替 (X)", "Swap (X)"), EditorStyles.miniButton))
            {
                SwapColors();
                AddToHistory(foregroundColor);
            }
            if (GUILayout.Button(L("初期値 (D)", "Default (D)"), EditorStyles.miniButton))
            {
                ResetDefaults();
                AddToHistory(foregroundColor);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRGBSliders()
        {
            EditorGUI.BeginChangeCheck();
            float r = EditorGUILayout.Slider("R", foregroundColor.r, 0f, 1f);
            float g = EditorGUILayout.Slider("G", foregroundColor.g, 0f, 1f);
            float b = EditorGUILayout.Slider("B", foregroundColor.b, 0f, 1f);
            float a = EditorGUILayout.Slider("A", foregroundColor.a, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                ForegroundColor = new Color(r, g, b, a);
                AddToHistory(foregroundColor);
            }
        }

        private void DrawHSVSliders()
        {
            EditorGUI.BeginChangeCheck();
            float newH = EditorGUILayout.Slider("H", hue, 0f, 1f);
            float newS = EditorGUILayout.Slider("S", saturation, 0f, 1f);
            float newV = EditorGUILayout.Slider("V", value, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                hue = newH;
                saturation = newS;
                value = newV;
                UpdateColorFromHSV();
                AddToHistory(foregroundColor);
            }
        }

        private void DrawHexInput()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("#", GUILayout.Width(12));
            EditorGUI.BeginChangeCheck();
            hexInput = EditorGUILayout.TextField(hexInput, GUILayout.MinWidth(80));
            if (EditorGUI.EndChangeCheck())
            {
                string hex = hexInput.TrimStart('#');
                if (hex.Length == 6) hex += "FF";
                if (ColorUtility.TryParseHtmlString("#" + hex, out Color parsed))
                {
                    ForegroundColor = parsed;
                    AddToHistory(foregroundColor);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColorHistory()
        {
            if (colorHistory.Count == 0) return;

            EditorGUILayout.LabelField(L("履歴", "History"), EditorStyles.miniLabel);

            int cols = 8;
            int rows = Mathf.CeilToInt((float)colorHistory.Count / cols);
            float cellSize = 18f;

            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < cols; col++)
                {
                    int idx = row * cols + col;
                    if (idx < colorHistory.Count)
                    {
                        Rect cellRect = GUILayoutUtility.GetRect(cellSize, cellSize,
                            GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                        EditorGUI.DrawRect(cellRect, colorHistory[idx]);

                        // Border
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.gray);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - 1, cellRect.width, 1), Color.gray);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.gray);
                        EditorGUI.DrawRect(new Rect(cellRect.xMax - 1, cellRect.y, 1, cellRect.height), Color.gray);

                        if (Event.current.type == EventType.MouseDown &&
                            cellRect.Contains(Event.current.mousePosition))
                        {
                            ForegroundColor = colorHistory[idx];
                            AddToHistory(foregroundColor);
                            Event.current.Use();
                        }
                    }
                    else
                    {
                        GUILayout.Space(cellSize);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawCircleMarker(Vector2 center, float radius, Color color)
        {
            Handles.color = color;
            Handles.DrawSolidDisc(new Vector3(center.x, center.y, 0), Vector3.forward, radius);
            Handles.color = new Color(1f - color.r, 1f - color.g, 1f - color.b, 1f);
            Handles.DrawWireDisc(new Vector3(center.x, center.y, 0), Vector3.forward, radius);
        }

        public void Dispose()
        {
            if (wheelTexture != null) Object.DestroyImmediate(wheelTexture);
            if (svTexture != null) Object.DestroyImmediate(svTexture);
            wheelTexture = null;
            svTexture = null;
        }
    }
}
