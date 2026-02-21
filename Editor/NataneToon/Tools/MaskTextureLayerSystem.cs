using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// Blend modes for mask texture layer compositing.
    /// マスクテクスチャレイヤー合成用ブレンドモード
    /// </summary>
    internal enum MaskBlendMode { Normal, Multiply, Add, Subtract, Overlay, Screen }

    /// <summary>
    /// A single layer in the mask texture layer stack.
    /// マスクテクスチャレイヤースタック内の単一レイヤー
    /// </summary>
    [System.Serializable]
    internal class MaskTextureLayer
    {
        public string name;
        public bool visible = true;
        public float opacity = 1f;
        public MaskBlendMode blendMode = MaskBlendMode.Normal;
        public Color[] pixels;
        public int width, height;
        public bool locked;

        public enum SourceType { Empty, Noise, UVMask, Gradient, MeshInfo, Paint, Import }
        public SourceType sourceType;

        public MaskTextureLayer(string name, int width, int height)
        {
            this.name = name;
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
            this.pixels = new Color[this.width * this.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.black;
        }

        public MaskTextureLayer Clone()
        {
            var clone = new MaskTextureLayer(name + " Copy", width, height);
            clone.visible = visible;
            clone.opacity = opacity;
            clone.blendMode = blendMode;
            clone.locked = locked;
            clone.sourceType = sourceType;
            if (pixels != null && pixels.Length > 0)
            {
                System.Array.Copy(pixels, clone.pixels, pixels.Length);
            }
            return clone;
        }

        /// <summary>
        /// Fill the entire layer with a uniform color.
        /// レイヤー全体を均一な色で塗りつぶす
        /// </summary>
        public void Fill(Color color)
        {
            if (pixels == null) return;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
        }

        /// <summary>
        /// Fill the layer with a grayscale value.
        /// レイヤーをグレースケール値で塗りつぶす
        /// </summary>
        public void Fill(float value)
        {
            Fill(new Color(value, value, value, 1f));
        }

        /// <summary>
        /// Invert all pixel values in the layer.
        /// レイヤー内の全ピクセル値を反転する
        /// </summary>
        public void Invert()
        {
            if (pixels == null) return;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(
                    1f - pixels[i].r,
                    1f - pixels[i].g,
                    1f - pixels[i].b,
                    pixels[i].a);
            }
        }

        /// <summary>
        /// Get pixel at UV coordinates with bilinear sampling.
        /// UV座標からバイリニアサンプリングでピクセルを取得
        /// </summary>
        public Color SampleBilinear(float u, float v)
        {
            if (pixels == null || pixels.Length == 0) return Color.black;

            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            float fx = u * (width - 1);
            float fy = v * (height - 1);
            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);
            float tx = fx - x0;
            float ty = fy - y0;

            Color c00 = pixels[y0 * width + x0];
            Color c10 = pixels[y0 * width + x1];
            Color c01 = pixels[y1 * width + x0];
            Color c11 = pixels[y1 * width + x1];

            Color c0 = Color.Lerp(c00, c10, tx);
            Color c1 = Color.Lerp(c01, c11, tx);
            return Color.Lerp(c0, c1, ty);
        }

        /// <summary>
        /// Set a pixel value at the given coordinates (clamped).
        /// 指定座標にピクセル値を設定する（クランプ済み）
        /// </summary>
        public void SetPixel(int x, int y, Color color)
        {
            if (pixels == null) return;
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            pixels[y * width + x] = color;
        }

        /// <summary>
        /// Get a pixel value at the given coordinates (clamped).
        /// 指定座標のピクセル値を取得する（クランプ済み）
        /// </summary>
        public Color GetPixel(int x, int y)
        {
            if (pixels == null) return Color.black;
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            return pixels[y * width + x];
        }

        /// <summary>
        /// Convert layer contents to a Texture2D.
        /// レイヤー内容をTexture2Dに変換する
        /// </summary>
        public Texture2D ToTexture2D()
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Import pixel data from a Texture2D (resized to layer dimensions if needed).
        /// Texture2Dからピクセルデータをインポートする（必要に応じてレイヤーサイズにリサイズ）
        /// </summary>
        public void ImportFromTexture(Texture2D source)
        {
            if (source == null) return;

            if (source.width == width && source.height == height)
            {
                var srcPixels = source.GetPixels();
                System.Array.Copy(srcPixels, pixels, Mathf.Min(srcPixels.Length, pixels.Length));
            }
            else
            {
                // Bilinear resample from source
                for (int y = 0; y < height; y++)
                {
                    float v = (float)y / Mathf.Max(1, height - 1);
                    for (int x = 0; x < width; x++)
                    {
                        float u = (float)x / Mathf.Max(1, width - 1);
                        pixels[y * width + x] = source.GetPixelBilinear(u, v);
                    }
                }
            }
            sourceType = SourceType.Import;
        }
    }

    /// <summary>
    /// Manages a stack of mask texture layers with blend operations.
    /// ブレンド操作付きマスクテクスチャレイヤースタックを管理する
    /// </summary>
    internal class MaskLayerStack
    {
        public List<MaskTextureLayer> Layers { get; private set; } = new List<MaskTextureLayer>();
        public int ActiveLayerIndex { get; set; }

        public MaskTextureLayer ActiveLayer =>
            (ActiveLayerIndex >= 0 && ActiveLayerIndex < Layers.Count) ? Layers[ActiveLayerIndex] : null;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public MaskLayerStack(int width = 512, int height = 512)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
        }

        public MaskTextureLayer AddLayer(string name)
        {
            return AddLayer(name, Width, Height);
        }

        public MaskTextureLayer AddLayer(string name, int width, int height)
        {
            var layer = new MaskTextureLayer(name, width, height);
            Layers.Add(layer);
            ActiveLayerIndex = Layers.Count - 1;
            return layer;
        }

        public void AddLayer(MaskTextureLayer layer)
        {
            if (layer == null) return;
            Layers.Add(layer);
            ActiveLayerIndex = Layers.Count - 1;
        }

        public void RemoveLayer(int index)
        {
            if (index < 0 || index >= Layers.Count) return;
            if (Layers.Count <= 1) return; // Keep at least one layer
            Layers.RemoveAt(index);
            ActiveLayerIndex = Mathf.Clamp(ActiveLayerIndex, 0, Layers.Count - 1);
        }

        public void MoveLayer(int from, int to)
        {
            if (from < 0 || from >= Layers.Count) return;
            to = Mathf.Clamp(to, 0, Layers.Count - 1);
            if (from == to) return;
            var layer = Layers[from];
            Layers.RemoveAt(from);
            Layers.Insert(to, layer);
            ActiveLayerIndex = to;
        }

        public void DuplicateLayer(int index)
        {
            if (index < 0 || index >= Layers.Count) return;
            var clone = Layers[index].Clone();
            Layers.Insert(index + 1, clone);
            ActiveLayerIndex = index + 1;
        }

        public void MergeDown(int index)
        {
            if (index <= 0 || index >= Layers.Count) return;
            var upper = Layers[index];
            var lower = Layers[index - 1];
            if (!upper.visible || upper.opacity <= 0f)
            {
                Layers.RemoveAt(index);
                ActiveLayerIndex = Mathf.Clamp(ActiveLayerIndex, 0, Layers.Count - 1);
                return;
            }

            int w = lower.width;
            int h = lower.height;
            for (int i = 0; i < w * h; i++)
            {
                Color topPixel = (upper.width == w && upper.height == h && upper.pixels != null)
                    ? upper.pixels[i] : Color.black;
                lower.pixels[i] = BlendPixels(lower.pixels[i], topPixel, upper.blendMode, upper.opacity);
            }
            Layers.RemoveAt(index);
            ActiveLayerIndex = Mathf.Clamp(index - 1, 0, Layers.Count - 1);
        }

        /// <summary>
        /// Flatten all visible layers into a single pixel array.
        /// 表示中の全レイヤーを単一ピクセル配列に統合する
        /// </summary>
        public Color[] Flatten()
        {
            if (Layers.Count == 0) return null;
            int w = Width;
            int h = Height;
            Color[] result = new Color[w * h];
            for (int i = 0; i < result.Length; i++)
                result[i] = new Color(0f, 0f, 0f, 0f);

            for (int layerIdx = 0; layerIdx < Layers.Count; layerIdx++)
            {
                var layer = Layers[layerIdx];
                if (!layer.visible || layer.opacity <= 0f) continue;
                if (layer.pixels == null) continue;

                bool sameSize = (layer.width == w && layer.height == h && layer.pixels.Length == result.Length);

                for (int i = 0; i < result.Length; i++)
                {
                    Color topPixel;
                    if (sameSize)
                    {
                        topPixel = layer.pixels[i];
                    }
                    else
                    {
                        // Resample if layer dimensions differ
                        int x = i % w;
                        int y = i / w;
                        float u = (float)x / Mathf.Max(1, w - 1);
                        float v = (float)y / Mathf.Max(1, h - 1);
                        topPixel = layer.SampleBilinear(u, v);
                    }
                    result[i] = BlendPixels(result[i], topPixel, layer.blendMode, layer.opacity);
                }
            }
            return result;
        }

        /// <summary>
        /// Flatten all layers into a Texture2D.
        /// 全レイヤーをTexture2Dに統合する
        /// </summary>
        public Texture2D FlattenToTexture()
        {
            var pixels = Flatten();
            if (pixels == null) return null;
            var tex = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Blend two pixels using the specified blend mode and opacity.
        /// 指定ブレンドモードと不透明度で2つのピクセルをブレンドする
        /// </summary>
        internal static Color BlendPixels(Color bottom, Color top, MaskBlendMode mode, float opacity)
        {
            Color blended;
            switch (mode)
            {
                case MaskBlendMode.Normal:
                    blended = top;
                    break;
                case MaskBlendMode.Multiply:
                    blended = new Color(
                        bottom.r * top.r,
                        bottom.g * top.g,
                        bottom.b * top.b, top.a);
                    break;
                case MaskBlendMode.Add:
                    blended = new Color(
                        Mathf.Clamp01(bottom.r + top.r),
                        Mathf.Clamp01(bottom.g + top.g),
                        Mathf.Clamp01(bottom.b + top.b), top.a);
                    break;
                case MaskBlendMode.Subtract:
                    blended = new Color(
                        Mathf.Clamp01(bottom.r - top.r),
                        Mathf.Clamp01(bottom.g - top.g),
                        Mathf.Clamp01(bottom.b - top.b), top.a);
                    break;
                case MaskBlendMode.Overlay:
                    blended = new Color(
                        OverlayChannel(bottom.r, top.r),
                        OverlayChannel(bottom.g, top.g),
                        OverlayChannel(bottom.b, top.b), top.a);
                    break;
                case MaskBlendMode.Screen:
                    blended = new Color(
                        1f - (1f - bottom.r) * (1f - top.r),
                        1f - (1f - bottom.g) * (1f - top.g),
                        1f - (1f - bottom.b) * (1f - top.b), top.a);
                    break;
                default:
                    blended = top;
                    break;
            }
            return Color.Lerp(bottom, blended, opacity);
        }

        private static float OverlayChannel(float a, float b)
        {
            return a < 0.5f ? 2f * a * b : 1f - 2f * (1f - a) * (1f - b);
        }
    }

    /// <summary>
    /// Editor UI for the layer panel, drawn within other editor windows.
    /// レイヤーパネルのエディターUI（他のエディターウィンドウ内に描画）
    /// </summary>
    internal static class MaskLayerPanelUI
    {
        private static GUIStyle activeLayerStyle;
        private static GUIStyle inactiveLayerStyle;

        private static void EnsureStyles()
        {
            if (activeLayerStyle == null)
            {
                activeLayerStyle = new GUIStyle(EditorStyles.helpBox);
                inactiveLayerStyle = new GUIStyle(EditorStyles.helpBox);
            }
        }

        /// <summary>
        /// Draw the full layer panel UI.
        /// レイヤーパネルUIを描画する
        /// </summary>
        /// <param name="stack">The layer stack to draw.</param>
        /// <param name="scrollPosition">Scroll position reference for the layer list.</param>
        /// <returns>True if any value changed (for repaint).</returns>
        public static bool DrawLayerPanel(MaskLayerStack stack, ref Vector2 scrollPosition)
        {
            EnsureStyles();
            bool changed = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("レイヤー Layers", EditorStyles.boldLabel);

            // Toolbar
            changed |= DrawToolbar(stack);

            EditorGUILayout.Space(3);

            // Layer list (top layer = highest index, drawn first)
            float listHeight = Mathf.Min(stack.Layers.Count * 52f + 10f, 250f);
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition, GUILayout.Height(Mathf.Max(listHeight, 60f)));

            for (int i = stack.Layers.Count - 1; i >= 0; i--)
            {
                changed |= DrawLayerEntry(stack, i);
            }

            EditorGUILayout.EndScrollView();

            // Active layer info
            if (stack.ActiveLayer != null)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(
                    $"アクティブ: {stack.ActiveLayer.name} ({stack.ActiveLayer.width}x{stack.ActiveLayer.height})",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            return changed;
        }

        private static bool DrawToolbar(MaskLayerStack stack)
        {
            bool changed = false;
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("+", "新規レイヤー追加 Add new layer"), GUILayout.Width(25)))
            {
                stack.AddLayer($"Layer {stack.Layers.Count + 1}");
                changed = true;
            }

            GUI.enabled = stack.Layers.Count > 1;
            if (GUILayout.Button(new GUIContent("-", "レイヤー削除 Remove layer"), GUILayout.Width(25)))
            {
                stack.RemoveLayer(stack.ActiveLayerIndex);
                changed = true;
            }
            GUI.enabled = true;

            GUI.enabled = stack.ActiveLayerIndex < stack.Layers.Count - 1;
            if (GUILayout.Button(new GUIContent("\u25B2", "上へ移動 Move up"), GUILayout.Width(25)))
            {
                stack.MoveLayer(stack.ActiveLayerIndex, stack.ActiveLayerIndex + 1);
                changed = true;
            }
            GUI.enabled = true;

            GUI.enabled = stack.ActiveLayerIndex > 0;
            if (GUILayout.Button(new GUIContent("\u25BC", "下へ移動 Move down"), GUILayout.Width(25)))
            {
                stack.MoveLayer(stack.ActiveLayerIndex, stack.ActiveLayerIndex - 1);
                changed = true;
            }
            GUI.enabled = true;

            GUI.enabled = stack.Layers.Count > 0;
            if (GUILayout.Button(new GUIContent("複製", "レイヤーを複製 Duplicate layer"), GUILayout.Width(40)))
            {
                stack.DuplicateLayer(stack.ActiveLayerIndex);
                changed = true;
            }

            GUI.enabled = stack.ActiveLayerIndex > 0 && stack.Layers.Count > 1;
            if (GUILayout.Button(new GUIContent("結合", "下のレイヤーと結合 Merge down"), GUILayout.Width(40)))
            {
                stack.MergeDown(stack.ActiveLayerIndex);
                changed = true;
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            return changed;
        }

        private static bool DrawLayerEntry(MaskLayerStack stack, int index)
        {
            bool changed = false;
            var layer = stack.Layers[index];
            bool isActive = (index == stack.ActiveLayerIndex);

            var bgColor = GUI.backgroundColor;
            if (isActive)
                GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Row 1: selection, visibility, name, lock
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(isActive ? "\u25CF" : "\u25CB", GUILayout.Width(20)))
            {
                stack.ActiveLayerIndex = index;
                changed = true;
            }

            EditorGUI.BeginChangeCheck();
            layer.visible = EditorGUILayout.Toggle(layer.visible, GUILayout.Width(15));
            if (EditorGUI.EndChangeCheck()) changed = true;

            EditorGUI.BeginChangeCheck();
            layer.name = EditorGUILayout.TextField(layer.name, GUILayout.MinWidth(60));
            if (EditorGUI.EndChangeCheck()) changed = true;

            string lockLabel = layer.locked ? "L" : "U";
            string lockTooltip = layer.locked ? "ロック解除 Unlock" : "ロック Lock";
            if (GUILayout.Button(new GUIContent(lockLabel, lockTooltip), GUILayout.Width(22)))
            {
                layer.locked = !layer.locked;
                changed = true;
            }

            EditorGUILayout.EndHorizontal();

            // Row 2: blend mode + opacity
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            layer.blendMode = (MaskBlendMode)EditorGUILayout.EnumPopup(layer.blendMode, GUILayout.Width(80));
            if (EditorGUI.EndChangeCheck()) changed = true;

            EditorGUI.BeginChangeCheck();
            layer.opacity = EditorGUILayout.Slider(layer.opacity, 0f, 1f);
            if (EditorGUI.EndChangeCheck()) changed = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUI.backgroundColor = bgColor;

            return changed;
        }

        /// <summary>
        /// Draw a compact layer panel for use in smaller UI areas.
        /// 小さいUI領域用のコンパクトレイヤーパネルを描画する
        /// </summary>
        public static bool DrawCompactLayerPanel(MaskLayerStack stack, ref Vector2 scrollPosition)
        {
            bool changed = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("レイヤー", EditorStyles.boldLabel, GUILayout.Width(60));

            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                stack.AddLayer($"Layer {stack.Layers.Count + 1}");
                changed = true;
            }

            GUI.enabled = stack.Layers.Count > 1;
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                stack.RemoveLayer(stack.ActiveLayerIndex);
                changed = true;
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition, GUILayout.Height(Mathf.Min(stack.Layers.Count * 22f + 5f, 120f)));

            for (int i = stack.Layers.Count - 1; i >= 0; i--)
            {
                var layer = stack.Layers[i];
                bool isActive = (i == stack.ActiveLayerIndex);

                EditorGUILayout.BeginHorizontal();

                var style = isActive ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(layer.name, style))
                {
                    stack.ActiveLayerIndex = i;
                    changed = true;
                }

                EditorGUI.BeginChangeCheck();
                layer.visible = EditorGUILayout.Toggle(layer.visible, GUILayout.Width(15));
                if (EditorGUI.EndChangeCheck()) changed = true;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            return changed;
        }
    }
}
