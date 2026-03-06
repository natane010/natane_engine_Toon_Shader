using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Brush mode for mask texture painting.
    /// マスクテクスチャペイント用ブラシモード
    /// </summary>
    internal enum BrushMode { Paint, Erase, Smooth, EraseAlpha }

    /// <summary>
    /// Settings for the mask texture brush tool.
    /// マスクテクスチャブラシツールの設定
    /// </summary>
    [System.Serializable]
    internal class BrushSettings
    {
        public float size = 20f;
        public float hardness = 0.8f;
        public float opacity = 1f;
        public float strength = 1f;
        public float paintAlpha = 1f;
        public BrushMode mode = BrushMode.Paint;
    }

    /// <summary>
    /// UV-space brush for painting directly onto mask textures.
    /// マスクテクスチャに直接ペイントするためのUV空間ブラシ
    /// </summary>
    internal class MaskTextureBrush
    {
        private BrushSettings settings;
        private Vector2 lastStrokePosition;
        private bool isStroking;
        private Color[] undoSnapshot;

        public MaskTextureBrush(BrushSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Calculate brush falloff based on distance, radius, and hardness.
        /// 距離・半径・硬さに基づくブラシフォールオフを計算
        /// </summary>
        public static float BrushFalloff(float distance, float radius, float hardness)
        {
            float t = distance / radius;
            if (t > 1f) return 0f;
            float inner = hardness;
            if (t < inner) return 1f;
            float s = (t - inner) / (1f - inner + 0.0001f);
            return 1f - s * s * (3f - 2f * s);
        }

        /// <summary>
        /// Start a new brush stroke, saving undo snapshot.
        /// 新しいブラシストロークを開始し、アンドゥスナップショットを保存
        /// </summary>
        public void StartStroke(Vector2 position, Color[] pixels)
        {
            isStroking = true;
            lastStrokePosition = position;
            if (pixels != null)
            {
                undoSnapshot = new Color[pixels.Length];
                System.Array.Copy(pixels, undoSnapshot, pixels.Length);
            }
            ApplyStamp(position, pixels, settings, 0, 0);
        }

        /// <summary>
        /// End the current stroke and return the undo snapshot.
        /// 現在のストロークを終了し、アンドゥスナップショットを返す
        /// </summary>
        public Color[] EndStroke()
        {
            isStroking = false;
            var snapshot = undoSnapshot;
            undoSnapshot = null;
            return snapshot;
        }

        /// <summary>
        /// Interpolate stamps from the last position to the new position.
        /// 最後の位置から新しい位置までスタンプを補間
        /// </summary>
        public void StrokeToPosition(Vector2 newPosition, Color[] pixels, int width, int height)
        {
            if (!isStroking) return;

            float spacing = Mathf.Max(settings.size * 0.25f, 1f);
            Vector2 delta = newPosition - lastStrokePosition;
            float distance = delta.magnitude;

            if (distance < 0.5f)
            {
                ApplyStamp(newPosition, pixels, settings, width, height);
                lastStrokePosition = newPosition;
                return;
            }

            Vector2 direction = delta / distance;
            float traveled = 0f;

            while (traveled < distance)
            {
                traveled += spacing;
                if (traveled > distance) traveled = distance;

                Vector2 stampPos = lastStrokePosition + direction * traveled;
                ApplyStamp(stampPos, pixels, settings, width, height);
            }

            lastStrokePosition = newPosition;
        }

        public bool IsStroking => isStroking;

        /// <summary>
        /// Apply a single brush stamp at the given pixel position.
        /// 指定ピクセル位置にブラシスタンプを適用
        /// </summary>
        private static void ApplyStamp(Vector2 center, Color[] pixels, BrushSettings settings, int width, int height)
        {
            if (pixels == null || width <= 0 || height <= 0) return;

            float radius = settings.size;
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + radius));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float falloff = BrushFalloff(dist, radius, settings.hardness);
                    if (falloff <= 0f) continue;

                    int idx = y * width + x;
                    Color current = pixels[idx];
                    float alpha = falloff * settings.opacity;

                    switch (settings.mode)
                    {
                        case BrushMode.Paint:
                        {
                            float target = settings.strength;
                            float targetA = settings.paintAlpha;
                            float r = Mathf.Lerp(current.r, target, alpha);
                            float g = Mathf.Lerp(current.g, target, alpha);
                            float b = Mathf.Lerp(current.b, target, alpha);
                            float a2 = Mathf.Lerp(current.a, targetA, alpha);
                            pixels[idx] = new Color(r, g, b, a2);
                            break;
                        }
                        case BrushMode.Erase:
                        {
                            float r = Mathf.Lerp(current.r, 0f, alpha);
                            float g = Mathf.Lerp(current.g, 0f, alpha);
                            float b = Mathf.Lerp(current.b, 0f, alpha);
                            pixels[idx] = new Color(r, g, b, current.a);
                            break;
                        }
                        case BrushMode.Smooth:
                        {
                            Color avg = GetAverageNeighbors(pixels, x, y, width, height);
                            float r = Mathf.Lerp(current.r, avg.r, alpha);
                            float g = Mathf.Lerp(current.g, avg.g, alpha);
                            float b = Mathf.Lerp(current.b, avg.b, alpha);
                            float a2 = Mathf.Lerp(current.a, avg.a, alpha);
                            pixels[idx] = new Color(r, g, b, a2);
                            break;
                        }
                        case BrushMode.EraseAlpha:
                        {
                            float a2 = Mathf.Lerp(current.a, 0f, alpha);
                            pixels[idx] = new Color(current.r, current.g, current.b, a2);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compute the average color of 3x3 neighborhood.
        /// 3x3近傍の平均色を計算
        /// </summary>
        private static Color GetAverageNeighbors(Color[] pixels, int cx, int cy, int width, int height)
        {
            float r = 0f, g = 0f, b = 0f, a = 0f;
            int count = 0;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                    Color c = pixels[ny * width + nx];
                    r += c.r;
                    g += c.g;
                    b += c.b;
                    a += c.a;
                    count++;
                }
            }

            if (count == 0) return Color.black;
            return new Color(r / count, g / count, b / count, a / count);
        }
    }

    /// <summary>
    /// Renders UV wireframe overlay on the brush canvas using GL drawing.
    /// GLドローイングを使用してブラシキャンバス上にUVワイヤーフレームオーバーレイを描画
    /// </summary>
    internal static class UVWireframeRenderer
    {
        private static Material lineMaterial;

        private static void EnsureLineMaterial()
        {
            if (lineMaterial != null) return;
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        /// <summary>
        /// Draw UV wireframe for the given mesh onto the canvas rect.
        /// 指定メッシュのUVワイヤーフレームをキャンバス矩形に描画
        /// </summary>
        public static void DrawWireframe(Rect canvasRect, Mesh mesh, List<Vector2> uvs)
        {
            if (mesh == null || uvs == null || uvs.Count == 0) return;

            EnsureLineMaterial();
            if (lineMaterial == null) return;

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(new Color(0f, 1f, 0f, 0.3f));

            int[] tris = mesh.triangles;
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int i0 = tris[i];
                int i1 = tris[i + 1];
                int i2 = tris[i + 2];

                if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                Vector2 a = UVToCanvas(uvs[i0], canvasRect);
                Vector2 b = UVToCanvas(uvs[i1], canvasRect);
                Vector2 c = UVToCanvas(uvs[i2], canvasRect);

                DrawLineGL(a, b);
                DrawLineGL(b, c);
                DrawLineGL(c, a);
            }

            GL.End();
            GL.PopMatrix();
        }

        internal static Vector2 UVToCanvas(Vector2 uv, Rect canvasRect)
        {
            return new Vector2(
                canvasRect.x + uv.x * canvasRect.width,
                canvasRect.y + (1f - uv.y) * canvasRect.height
            );
        }

        internal static void DrawLineGL(Vector2 a, Vector2 b)
        {
            GL.Vertex3(a.x, a.y, 0);
            GL.Vertex3(b.x, b.y, 0);
        }

        /// <summary>
        /// Draw UV wireframe with per-island coloring.
        /// アイランドごとに色分けしたUVワイヤーフレームを描画
        /// </summary>
        public static void DrawWireframePerIsland(
            Rect canvasRect, Mesh mesh, List<Vector2> uvs,
            IList<UVTextureGenerator.UVIsland> islands, Color[] islandColors, int hoveredIndex)
        {
            if (mesh == null || uvs == null || uvs.Count == 0 || islands == null) return;

            EnsureLineMaterial();
            if (lineMaterial == null) return;

            int[] tris = mesh.triangles;

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.Begin(GL.LINES);

            for (int idx = 0; idx < islands.Count; idx++)
            {
                var island = islands[idx];
                Color color = idx < islandColors.Length ? islandColors[idx] : Color.gray;
                GL.Color(island.selected
                    ? new Color(color.r, color.g, color.b, 0.8f)
                    : new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 0.15f));

                foreach (int t in island.triangleIndices)
                {
                    int i0 = tris[t * 3 + 0];
                    int i1 = tris[t * 3 + 1];
                    int i2 = tris[t * 3 + 2];
                    if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                    Vector2 a = UVToCanvas(uvs[i0], canvasRect);
                    Vector2 b = UVToCanvas(uvs[i1], canvasRect);
                    Vector2 c = UVToCanvas(uvs[i2], canvasRect);
                    DrawLineGL(a, b);
                    DrawLineGL(b, c);
                    DrawLineGL(c, a);
                }
            }

            // Hovered island highlight
            if (hoveredIndex >= 0 && hoveredIndex < islands.Count)
            {
                GL.Color(new Color(1f, 1f, 1f, 0.9f));
                foreach (int t in islands[hoveredIndex].triangleIndices)
                {
                    int i0 = tris[t * 3 + 0];
                    int i1 = tris[t * 3 + 1];
                    int i2 = tris[t * 3 + 2];
                    if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                    Vector2 a = UVToCanvas(uvs[i0], canvasRect);
                    Vector2 b = UVToCanvas(uvs[i1], canvasRect);
                    Vector2 c = UVToCanvas(uvs[i2], canvasRect);
                    DrawLineGL(a, b);
                    DrawLineGL(b, c);
                    DrawLineGL(c, a);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// Draw filled triangles for selected islands.
        /// 選択アイランドの三角形を半透明塗りつぶしで描画
        /// </summary>
        public static void DrawIslandFill(
            Rect canvasRect, Mesh mesh, List<Vector2> uvs,
            IList<UVTextureGenerator.UVIsland> islands, Color[] islandColors)
        {
            if (mesh == null || uvs == null || uvs.Count == 0 || islands == null) return;

            EnsureLineMaterial();
            if (lineMaterial == null) return;

            int[] tris = mesh.triangles;

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.Begin(GL.TRIANGLES);

            for (int idx = 0; idx < islands.Count; idx++)
            {
                var island = islands[idx];
                if (!island.selected) continue;

                Color color = idx < islandColors.Length ? islandColors[idx] : Color.gray;
                GL.Color(new Color(color.r, color.g, color.b, 0.2f));

                foreach (int t in island.triangleIndices)
                {
                    int i0 = tris[t * 3 + 0];
                    int i1 = tris[t * 3 + 1];
                    int i2 = tris[t * 3 + 2];
                    if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                    Vector2 a = UVToCanvas(uvs[i0], canvasRect);
                    Vector2 b = UVToCanvas(uvs[i1], canvasRect);
                    Vector2 c = UVToCanvas(uvs[i2], canvasRect);
                    GL.Vertex3(a.x, a.y, 0);
                    GL.Vertex3(b.x, b.y, 0);
                    GL.Vertex3(c.x, c.y, 0);
                }
            }

            GL.End();
            GL.PopMatrix();
        }
    }

    /// <summary>
    /// Renders a circular brush cursor on the canvas.
    /// キャンバス上に円形ブラシカーソルを描画
    /// </summary>
    internal static class BrushCursorRenderer
    {
        /// <summary>
        /// Draw a circle cursor at the given center with specified radius and color.
        /// 指定された中心・半径・色で円形カーソルを描画
        /// </summary>
        public static void DrawCursor(Vector2 center, float radius, Color color)
        {
            Handles.color = color;
            int segments = 32;
            Vector3[] points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * 2f * Mathf.PI;
                points[i] = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius, 0);
            }
            Handles.DrawPolyLine(points);
        }

        /// <summary>
        /// Draw cursor with inner hardness circle and outer falloff circle.
        /// 内側の硬さ円と外側のフォールオフ円でカーソルを描画
        /// </summary>
        public static void DrawCursorWithHardness(Vector2 center, float radius, float hardness, Color color)
        {
            DrawCursor(center, radius, color);

            if (hardness > 0.01f && hardness < 0.99f)
            {
                Color innerColor = new Color(color.r, color.g, color.b, color.a * 0.5f);
                DrawCursor(center, radius * hardness, innerColor);
            }
        }
    }

    /// <summary>
    /// UI drawing utilities for brush settings panel.
    /// ブラシ設定パネル用のUI描画ユーティリティ
    /// </summary>
    internal static class BrushSettingsUI
    {
        private static GUIContent[] modeLabels => new GUIContent[]
        {
            new GUIContent(L("ペイント", "Paint"), L("グレースケール値をペイント", "Paint grayscale values")),
            new GUIContent(L("消しゴム", "Erase"), L("黒に消去", "Erase to black")),
            new GUIContent(L("スムーズ", "Smooth"), L("値をスムーズ・ぼかし", "Smooth/blur values")),
            new GUIContent(L("α消去", "Erase Alpha"), L("アルファチャンネルを消去", "Erase alpha channel"))
        };

        /// <summary>
        /// Draw the complete brush settings UI panel.
        /// ブラシ設定UIパネル全体を描画
        /// </summary>
        public static void DrawBrushSettingsUI(BrushSettings settings)
        {
            if (settings == null) return;

            EditorGUILayout.LabelField(L("ブラシ設定", "Brush Settings"), EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(L("モード", "Mode"), GUILayout.Width(100));
                int modeIndex = GUILayout.Toolbar((int)settings.mode, modeLabels);
                settings.mode = (BrushMode)modeIndex;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                settings.size = EditorGUILayout.Slider(
                    new GUIContent(L("サイズ", "Size"), L("ブラシ半径（ピクセル）", "Brush radius in pixels")),
                    settings.size, 1f, 100f);

                settings.hardness = EditorGUILayout.Slider(
                    new GUIContent(L("硬さ", "Hardness"), L("エッジ硬さ: 0=ソフト, 1=ハード", "Edge hardness: 0=soft, 1=hard")),
                    settings.hardness, 0f, 1f);

                settings.opacity = EditorGUILayout.Slider(
                    new GUIContent(L("不透明度", "Opacity"), L("ブラシ不透明度", "Brush opacity")),
                    settings.opacity, 0f, 1f);

                if (settings.mode == BrushMode.Paint)
                {
                    settings.strength = EditorGUILayout.Slider(
                        new GUIContent(L("強度", "Strength"), L("ペイント値（グレースケール 0-1）", "Paint value (grayscale 0-1)")),
                        settings.strength, 0f, 1f);

                    settings.paintAlpha = EditorGUILayout.Slider(
                        new GUIContent(L("アルファ", "Alpha"), L("ペイントアルファ値（0=透明, 1=不透明）", "Paint alpha value (0=transparent, 1=opaque)")),
                        settings.paintAlpha, 0f, 1f);
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(
                    L("ショートカット: スクロール=サイズ変更", "Shortcut: Scroll=Size"),
                    EditorStyles.miniLabel);
            }
        }
    }

    /// <summary>
    /// Handles mouse input on the brush canvas and dispatches to the brush.
    /// ブラシキャンバス上のマウス入力を処理しブラシにディスパッチ
    /// </summary>
    internal static class BrushCanvasInputHandler
    {
        /// <summary>
        /// Process input events for the brush canvas. Returns true if the event was consumed.
        /// ブラシキャンバスの入力イベントを処理。イベントが消費された場合trueを返す
        /// </summary>
        /// <param name="canvasRect">Display area for hit testing (visible canvas region).</param>
        /// <param name="textureRect">Actual texture display rect for coordinate mapping (accounts for zoom/pan).</param>
        /// <param name="brush">Brush instance.</param>
        /// <param name="settings">Brush settings.</param>
        /// <param name="pixels">Pixel array to paint on.</param>
        /// <param name="width">Texture width.</param>
        /// <param name="height">Texture height.</param>
        /// <param name="textureModified">Set to true if pixels were modified.</param>
        public static bool HandleBrushInput(
            Rect canvasRect,
            Rect textureRect,
            MaskTextureBrush brush,
            BrushSettings settings,
            Color[] pixels,
            int width,
            int height,
            out bool textureModified)
        {
            textureModified = false;
            if (brush == null || settings == null || pixels == null) return false;

            Event e = Event.current;
            if (!canvasRect.Contains(e.mousePosition) && !brush.IsStroking)
                return false;

            // Use textureRect for coordinate mapping (zoom/pan aware)
            Vector2 canvasPos = MouseToPixelPos(e.mousePosition, textureRect, width, height);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && canvasRect.Contains(e.mousePosition))
                    {
                        brush.StartStroke(canvasPos, pixels);
                        textureModified = true;
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && brush.IsStroking)
                    {
                        brush.StrokeToPosition(canvasPos, pixels, width, height);
                        textureModified = true;
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && brush.IsStroking)
                    {
                        brush.EndStroke();
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;

                case EventType.ScrollWheel:
                    if (canvasRect.Contains(e.mousePosition))
                    {
                        float delta = -e.delta.y;
                        settings.size = Mathf.Clamp(settings.size + delta * 2f, 1f, 100f);
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Process input events for the brush canvas (legacy overload without separate textureRect).
        /// ブラシキャンバスの入力イベントを処理（textureRect分離なしのレガシーオーバーロード）
        /// </summary>
        public static bool HandleBrushInput(
            Rect canvasRect,
            MaskTextureBrush brush,
            BrushSettings settings,
            Color[] pixels,
            int width,
            int height,
            out bool textureModified)
        {
            return HandleBrushInput(canvasRect, canvasRect, brush, settings, pixels, width, height, out textureModified);
        }

        /// <summary>
        /// Convert mouse position in canvas rect to pixel coordinates.
        /// キャンバス矩形内のマウス位置をピクセル座標に変換
        /// </summary>
        public static Vector2 MouseToPixelPos(Vector2 mousePos, Rect canvasRect, int width, int height)
        {
            float u = (mousePos.x - canvasRect.x) / canvasRect.width;
            float v = (mousePos.y - canvasRect.y) / canvasRect.height;
            v = 1f - v; // Flip Y: screen space top-down → texture space bottom-up
            return new Vector2(u * width, v * height);
        }

        /// <summary>
        /// Convert mouse position in canvas rect to UV coordinates (0-1 range).
        /// キャンバス矩形内のマウス位置をUV座標（0-1範囲）に変換
        /// </summary>
        public static Vector2 MouseToUV(Vector2 mousePos, Rect canvasRect)
        {
            float u = Mathf.Clamp01((mousePos.x - canvasRect.x) / canvasRect.width);
            float v = Mathf.Clamp01((mousePos.y - canvasRect.y) / canvasRect.height);
            v = 1f - v;
            return new Vector2(u, v);
        }

        private static void RequestRepaint()
        {
            if (EditorWindow.focusedWindow != null)
                EditorWindow.focusedWindow.Repaint();
        }
    }
}
