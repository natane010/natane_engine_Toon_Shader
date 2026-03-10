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
        public bool eraseRgb = true;
        public float alphaEpsilon = 0.001f;
        public BrushMode mode = BrushMode.Paint;
        public BrushStabilizerSettings stabilizer = new BrushStabilizerSettings();
    }

    internal readonly struct BrushStrokeCommit
    {
        public BrushStrokeCommit(Color[] beforePixels, Color[] afterPixels, Vector2 lastPixelPosition)
        {
            BeforePixels = beforePixels;
            AfterPixels = afterPixels;
            LastPixelPosition = lastPixelPosition;
        }

        public Color[] BeforePixels { get; }
        public Color[] AfterPixels { get; }
        public Vector2 LastPixelPosition { get; }
        public bool HasValue => BeforePixels != null && AfterPixels != null;
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
            StartStroke(position, pixels, 0, 0, false);
        }

        public void StartStroke(
            Vector2 position,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels)
        {
            isStroking = true;
            lastStrokePosition = position;
            if (pixels != null)
            {
                undoSnapshot = new Color[pixels.Length];
                System.Array.Copy(pixels, undoSnapshot, pixels.Length);
            }
            ApplyStamp(position, pixels, settings, width, height, lockTransparentPixels);
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
            StrokeToPosition(newPosition, pixels, width, height, false);
        }

        public void StrokeToPosition(
            Vector2 newPosition,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels)
        {
            if (!isStroking) return;

            float spacing = Mathf.Max(settings.size * 0.25f, 1f);
            Vector2 delta = newPosition - lastStrokePosition;
            float distance = delta.magnitude;

            if (distance < 0.5f)
            {
                ApplyStamp(newPosition, pixels, settings, width, height, lockTransparentPixels);
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
                ApplyStamp(stampPos, pixels, settings, width, height, lockTransparentPixels);
            }

            lastStrokePosition = newPosition;
        }

        public bool IsStroking => isStroking;
        public Vector2 LastStrokePosition => lastStrokePosition;

        public void PaintStraightLine(
            Vector2 startPosition,
            Vector2 endPosition,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels)
        {
            float distance = Vector2.Distance(startPosition, endPosition);
            if (distance <= 0.001f)
            {
                ApplyStamp(endPosition, pixels, settings, width, height, lockTransparentPixels);
                return;
            }

            float spacing = Mathf.Max(settings.size * 0.25f, 1f);
            Vector2 direction = (endPosition - startPosition).normalized;

            for (float traveled = 0f; traveled <= distance; traveled += spacing)
            {
                ApplyStamp(startPosition + direction * traveled, pixels, settings, width, height, lockTransparentPixels);
            }

            ApplyStamp(endPosition, pixels, settings, width, height, lockTransparentPixels);
        }

        /// <summary>
        /// Apply a single brush stamp at the given pixel position.
        /// 指定ピクセル位置にブラシスタンプを適用
        /// </summary>
        private static void ApplyStamp(
            Vector2 center,
            Color[] pixels,
            BrushSettings settings,
            int width,
            int height,
            bool lockTransparentPixels)
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
                    if (!CanModifyPixel(current, lockTransparentPixels, settings.alphaEpsilon))
                        continue;

                    float alpha = falloff * settings.opacity;

                    switch (settings.mode)
                    {
                        case BrushMode.Paint:
                            pixels[idx] = ApplyPaint(current, alpha, settings);
                            break;
                        case BrushMode.Erase:
                            pixels[idx] = ApplyErase(current, alpha, settings.eraseRgb);
                            break;
                        case BrushMode.Smooth:
                            pixels[idx] = ApplySmooth(current, GetAverageNeighbors(pixels, x, y, width, height), alpha);
                            break;
                        case BrushMode.EraseAlpha:
                            pixels[idx] = ApplyErase(current, alpha, false);
                            break;
                    }
                }
            }
        }

        private static bool CanModifyPixel(Color current, bool lockTransparentPixels, float alphaEpsilon)
        {
            return !lockTransparentPixels || current.a > alphaEpsilon;
        }

        private static Color ApplyPaint(Color current, float alpha, BrushSettings settings)
        {
            float target = settings.strength;
            float targetA = settings.paintAlpha;
            float r = Mathf.Lerp(current.r, target, alpha);
            float g = Mathf.Lerp(current.g, target, alpha);
            float b = Mathf.Lerp(current.b, target, alpha);
            float a2 = Mathf.Lerp(current.a, targetA, alpha);
            return new Color(r, g, b, a2);
        }

        private static Color ApplyErase(Color current, float alpha, bool eraseRgb)
        {
            float a2 = Mathf.Lerp(current.a, 0f, alpha);
            if (!eraseRgb)
                return new Color(current.r, current.g, current.b, a2);

            float r = Mathf.Lerp(current.r, 0f, alpha);
            float g = Mathf.Lerp(current.g, 0f, alpha);
            float b = Mathf.Lerp(current.b, 0f, alpha);
            return new Color(r, g, b, a2);
        }

        private static Color ApplySmooth(Color current, Color avg, float alpha)
        {
            float r = Mathf.Lerp(current.r, avg.r, alpha);
            float g = Mathf.Lerp(current.g, avg.g, alpha);
            float b = Mathf.Lerp(current.b, avg.b, alpha);
            float a2 = Mathf.Lerp(current.a, avg.a, alpha);
            return new Color(r, g, b, a2);
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

            if (count == 0) return Color.clear;
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

        public static void DrawLineGuide(Vector2 start, Vector2 end, Color color)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(2f, new Vector3(start.x, start.y, 0f), new Vector3(end.x, end.y, 0f));
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
                else if (settings.mode == BrushMode.Erase)
                {
                    settings.eraseRgb = EditorGUILayout.Toggle(
                        new GUIContent("Clear RGB", "Also clear RGB while erasing"),
                        settings.eraseRgb);
                }

                EditorGUILayout.Space(4);
                settings.stabilizer.mode = (BrushStabilizerMode)EditorGUILayout.EnumPopup(
                    new GUIContent(L("手ブレ補正", "Stabilizer"), L("ストローク入力を安定化", "Stabilize stroke input")),
                    settings.stabilizer.mode);

                if (settings.stabilizer.mode != BrushStabilizerMode.Off)
                {
                    settings.stabilizer.strength = EditorGUILayout.Slider(
                        new GUIContent(L("補正強度", "Stabilizer Strength"), L("大きいほど補正を強くします", "Higher values stabilize more")),
                        settings.stabilizer.strength, 0f, 1f);
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(
                    L("ショートカット: スクロール=サイズ変更", "Shortcut: Scroll=Size"),
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    "Ctrl+Click=Pick / Shift+Click=Line / Alt+RMB=Size+Opacity",
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
            BrushStrokeCommit strokeCommit;
            return HandleBrushInput(
                canvasRect, textureRect, brush, settings, pixels, width, height, false, null, out textureModified, out strokeCommit);
        }

        public static bool HandleBrushInput(
            Rect canvasRect,
            Rect textureRect,
            MaskTextureBrush brush,
            BrushSettings settings,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels,
            out bool textureModified)
        {
            BrushStrokeCommit strokeCommit;
            return HandleBrushInput(
                canvasRect, textureRect, brush, settings, pixels, width, height, lockTransparentPixels, null, out textureModified, out strokeCommit);
        }

        public static bool HandleBrushInput(
            Rect canvasRect,
            Rect textureRect,
            MaskTextureBrush brush,
            BrushSettings settings,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels,
            MaskTextureBrushStabilizer stabilizer,
            out bool textureModified,
            out BrushStrokeCommit strokeCommit)
        {
            textureModified = false;
            strokeCommit = default;
            if (brush == null || settings == null || pixels == null) return false;

            Event e = Event.current;
            if (!canvasRect.Contains(e.mousePosition) && !brush.IsStroking)
                return false;

            // Use textureRect for coordinate mapping (zoom/pan aware)
            Vector2 rawCanvasPos = MouseToPixelPos(e.mousePosition, textureRect, width, height);
            Vector2 canvasPos = FilterBrushPosition(rawCanvasPos, settings, stabilizer);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && canvasRect.Contains(e.mousePosition))
                    {
                        stabilizer?.Reset();
                        canvasPos = FilterBrushPosition(rawCanvasPos, settings, stabilizer);
                        brush.StartStroke(canvasPos, pixels, width, height, lockTransparentPixels);
                        textureModified = true;
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && brush.IsStroking)
                    {
                        brush.StrokeToPosition(canvasPos, pixels, width, height, lockTransparentPixels);
                        textureModified = true;
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && brush.IsStroking)
                    {
                        Color[] beforePixels = brush.EndStroke();
                        strokeCommit = CreateStrokeCommit(beforePixels, pixels, rawCanvasPos);
                        stabilizer?.Reset();
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
            BrushStrokeCommit strokeCommit;
            return HandleBrushInput(
                canvasRect, canvasRect, brush, settings, pixels, width, height, false, null, out textureModified, out strokeCommit);
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

        public static Vector2 PixelToCanvasPos(Vector2 pixelPos, Rect canvasRect, int width, int height)
        {
            float u = width > 0 ? pixelPos.x / Mathf.Max(1f, width) : 0f;
            float v = height > 0 ? 1f - (pixelPos.y / Mathf.Max(1f, height)) : 1f;
            return new Vector2(
                canvasRect.x + u * canvasRect.width,
                canvasRect.y + v * canvasRect.height);
        }

        private static void RequestRepaint()
        {
            if (EditorWindow.focusedWindow != null)
                EditorWindow.focusedWindow.Repaint();
        }

        private static Vector2 FilterBrushPosition(Vector2 rawCanvasPos, BrushSettings settings, MaskTextureBrushStabilizer stabilizer)
        {
            if (stabilizer == null)
                return rawCanvasPos;

            return stabilizer.Filter(rawCanvasPos, settings.stabilizer);
        }

        private static BrushStrokeCommit CreateStrokeCommit(Color[] beforePixels, Color[] pixels, Vector2 lastPixelPosition)
        {
            if (beforePixels == null || pixels == null)
                return default;

            return new BrushStrokeCommit(beforePixels, ClonePixels(pixels), lastPixelPosition);
        }

        private static Color[] ClonePixels(Color[] pixels)
        {
            if (pixels == null)
                return null;

            Color[] clone = new Color[pixels.Length];
            System.Array.Copy(pixels, clone, pixels.Length);
            return clone;
        }
    }
}
