using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Blend modes for mask texture layer compositing.
    /// マスクテクスチャレイヤー合成用ブレンドモード
    /// </summary>
    internal enum MaskBlendMode
    {
        Normal, Multiply, Add, Subtract, Overlay, Screen,
        ColorDodge, ColorBurn, SoftLight, HardLight,
        Difference, Exclusion,
        Hue, Saturation, ColorBlend, Luminosity
    }

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
        public bool lockTransparentPixels;
        public bool isClippingMask;
        public Color[] mask;
        public bool maskEnabled = true;
        [System.NonSerialized] public bool editingMask;
        public bool maskLinked = true;
        [System.NonSerialized] private Texture2D maskThumbnailCache;
        [System.NonSerialized] private int maskThumbnailHash;

        public Vector2 transformOffset = Vector2.zero;
        public Vector2 transformScale = Vector2.one;

        // Layer group support / レイヤーグループサポート
        public bool isGroup;                     // True if this is a group header / グループヘッダーの場合true
        public bool isGroupExpanded = true;       // UI expanded state / UI展開状態
        public int groupDepth;                    // Nesting depth (0=root) / ネスト深度（0=ルート）
        public int parentGroupIndex = -1;         // Index of parent group (-1=root) / 親グループのインデックス

        public bool HasTransform =>
            !Mathf.Approximately(transformOffset.x, 0f) ||
            !Mathf.Approximately(transformOffset.y, 0f) ||
            !Mathf.Approximately(transformScale.x, 1f) ||
            !Mathf.Approximately(transformScale.y, 1f);

        public enum SourceType { Empty, Noise, UVMask, Gradient, MeshInfo, Paint, Import }
        public SourceType sourceType;

        // Thumbnail cache
        [System.NonSerialized] private Texture2D thumbnailCache;
        [System.NonSerialized] private int thumbnailHash;
        private const int ThumbnailSize = 40;

        public MaskTextureLayer(string name, int width, int height)
        {
            this.name = name;
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
            this.pixels = new Color[this.width * this.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
        }

        public Texture2D GetThumbnail()
        {
            int currentHash = ComputePixelHash();
            bool shouldRefresh = Event.current == null || Event.current.type == EventType.Repaint || currentHash != thumbnailHash;
            if (thumbnailCache != null && !shouldRefresh)
                return thumbnailCache;

            thumbnailHash = currentHash;
            if (thumbnailCache == null)
                thumbnailCache = new Texture2D(ThumbnailSize, ThumbnailSize, TextureFormat.RGBA32, false);

            // Downsample pixels to 40x40
            for (int ty = 0; ty < ThumbnailSize; ty++)
            {
                float v = (float)ty / (ThumbnailSize - 1);
                for (int tx = 0; tx < ThumbnailSize; tx++)
                {
                    float u = (float)tx / (ThumbnailSize - 1);
                    thumbnailCache.SetPixel(tx, ty, SampleBilinear(u, v));
                }
            }
            thumbnailCache.Apply();
            return thumbnailCache;
        }

        private int ComputePixelHash()
        {
            return ComputeArrayHash(pixels, false);
        }

        public void DisposeThumbnail()
        {
            if (thumbnailCache != null)
            {
                UnityEngine.Object.DestroyImmediate(thumbnailCache);
                thumbnailCache = null;
            }
        }

        public MaskTextureLayer Clone()
        {
            var clone = new MaskTextureLayer(name + " Copy", width, height);
            clone.visible = visible;
            clone.opacity = opacity;
            clone.blendMode = blendMode;
            clone.locked = locked;
            clone.lockTransparentPixels = lockTransparentPixels;
            if (mask != null)
            {
                clone.mask = new Color[mask.Length];
                System.Array.Copy(mask, clone.mask, mask.Length);
            }
            clone.maskEnabled = maskEnabled;
            clone.maskLinked = maskLinked;
            clone.isClippingMask = isClippingMask;
            clone.sourceType = sourceType;
            clone.transformOffset = transformOffset;
            clone.transformScale = transformScale;
            clone.isGroup = isGroup;
            clone.isGroupExpanded = isGroupExpanded;
            clone.groupDepth = groupDepth;
            clone.parentGroupIndex = parentGroupIndex;
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
            Fill(value, 1f);
        }

        public void Fill(float value, float alpha)
        {
            Fill(new Color(value, value, value, alpha));
        }

        public void Clear()
        {
            Fill(Color.clear);
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
            if (pixels == null || pixels.Length == 0) return Color.clear;

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
        /// Sample with layer transform applied (offset + scale in UV space).
        /// レイヤートランスフォーム適用済みサンプリング
        /// </summary>
        public Color SampleBilinearTransformed(float u, float v)
        {
            float tu = (u - 0.5f - transformOffset.x) / Mathf.Max(transformScale.x, 0.001f) + 0.5f;
            float tv = (v - 0.5f - transformOffset.y) / Mathf.Max(transformScale.y, 0.001f) + 0.5f;
            return SampleBilinear(tu, tv);
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
            if (pixels == null) return Color.clear;
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

        private static Texture2D MakeReadableCopy(Texture2D source)
        {
            if (source == null)
                return null;

            RenderTexture rt = RenderTexture.GetTemporary(
                source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;

                var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply(false, false);
                return readable;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// Import pixel data from a Texture2D (resized to layer dimensions if needed).
        /// Texture2Dからピクセルデータをインポートする（必要に応じてレイヤーサイズにリサイズ）
        /// </summary>
        public void ImportFromTexture(Texture2D source)
        {
            if (source == null) return;

            Texture2D readableSource = source;
            Texture2D tempReadable = null;

            if (!source.isReadable)
            {
                tempReadable = MakeReadableCopy(source);
                readableSource = tempReadable != null ? tempReadable : source;
            }

            try
            {
                if (readableSource.width == width && readableSource.height == height)
                {
                    var srcPixels = readableSource.GetPixels();
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
                            pixels[y * width + x] = readableSource.GetPixelBilinear(u, v);
                        }
                    }
                }
                sourceType = SourceType.Import;
            }
            finally
            {
                if (tempReadable != null)
                    UnityEngine.Object.DestroyImmediate(tempReadable);
            }
        }

        public void CreateMask()
        {
            mask = new Color[width * height];
            for (int i = 0; i < mask.Length; i++)
                mask[i] = Color.white;
            maskEnabled = true;
        }

        public void CreateMaskFromAlpha()
        {
            mask = new Color[width * height];
            for (int i = 0; i < pixels.Length && i < mask.Length; i++)
            {
                float a = pixels[i].a;
                mask[i] = new Color(a, a, a, 1f);
            }
            maskEnabled = true;
        }

        public void DeleteMask()
        {
            mask = null;
            maskEnabled = false;
            editingMask = false;
            if (maskThumbnailCache != null)
            {
                UnityEngine.Object.DestroyImmediate(maskThumbnailCache);
                maskThumbnailCache = null;
            }
        }

        public void InvertMask()
        {
            if (mask == null) return;
            for (int i = 0; i < mask.Length; i++)
                mask[i] = new Color(1f - mask[i].r, 1f - mask[i].g, 1f - mask[i].b, mask[i].a);
        }

        public void ApplyMask()
        {
            if (mask == null) return;
            for (int i = 0; i < pixels.Length && i < mask.Length; i++)
            {
                float maskVal = (mask[i].r + mask[i].g + mask[i].b) / 3f;
                pixels[i] = new Color(pixels[i].r, pixels[i].g, pixels[i].b, pixels[i].a * maskVal);
            }
            DeleteMask();
        }

        public Texture2D GetMaskThumbnail()
        {
            if (mask == null) return null;
            int currentHash = ComputeMaskHash();
            bool shouldRefresh = Event.current == null || Event.current.type == EventType.Repaint || currentHash != maskThumbnailHash;
            if (maskThumbnailCache != null && !shouldRefresh)
                return maskThumbnailCache;
            maskThumbnailHash = currentHash;
            if (maskThumbnailCache == null)
                maskThumbnailCache = new Texture2D(ThumbnailSize, ThumbnailSize, TextureFormat.RGBA32, false);
            for (int ty = 0; ty < ThumbnailSize; ty++)
            {
                float v = (float)ty / (ThumbnailSize - 1);
                for (int tx = 0; tx < ThumbnailSize; tx++)
                {
                    float u = (float)tx / (ThumbnailSize - 1);
                    maskThumbnailCache.SetPixel(tx, ty, SampleMaskBilinear(u, v));
                }
            }
            maskThumbnailCache.Apply();
            return maskThumbnailCache;
        }

        public Color SampleMaskBilinear(float u, float v)
        {
            if (mask == null || mask.Length == 0) return Color.white;
            u = Mathf.Clamp01(u); v = Mathf.Clamp01(v);
            float fx = u * (width - 1), fy = v * (height - 1);
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            int x1 = Mathf.Min(x0 + 1, width - 1), y1 = Mathf.Min(y0 + 1, height - 1);
            float tx = fx - x0, ty2 = fy - y0;
            Color c00 = mask[y0 * width + x0], c10 = mask[y0 * width + x1];
            Color c01 = mask[y1 * width + x0], c11 = mask[y1 * width + x1];
            return Color.Lerp(Color.Lerp(c00, c10, tx), Color.Lerp(c01, c11, tx), ty2);
        }

        private int ComputeMaskHash()
        {
            return ComputeArrayHash(mask, true);
        }

        internal int GetPixelStateHash()
        {
            return ComputePixelHash();
        }

        internal int GetMaskStateHash()
        {
            return ComputeMaskHash();
        }

        private static int ComputeArrayHash(Color[] source, bool grayscaleOnly)
        {
            if (source == null || source.Length == 0) return 0;

            unchecked
            {
                int hash = 17;
                int step = Mathf.Max(1, source.Length / 64);
                for (int i = 0; i < source.Length; i += step)
                {
                    Color pixel = source[i];
                    hash = hash * 31 + (int)(pixel.r * 255f);
                    if (!grayscaleOnly)
                    {
                        hash = hash * 31 + (int)(pixel.g * 255f);
                        hash = hash * 31 + (int)(pixel.b * 255f);
                        hash = hash * 31 + (int)(pixel.a * 255f);
                    }
                }
                return hash;
            }
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
        private Color[] flattenCache;
        private int flattenCacheHash;
        private bool flattenCacheValid;

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
            InvalidateFlattenCache();
            return layer;
        }

        public void AddLayer(MaskTextureLayer layer)
        {
            if (layer == null) return;
            Layers.Add(layer);
            ActiveLayerIndex = Layers.Count - 1;
            InvalidateFlattenCache();
        }

        public void RemoveLayer(int index)
        {
            if (index < 0 || index >= Layers.Count) return;
            if (Layers.Count <= 1) return; // Keep at least one layer
            Layers.RemoveAt(index);
            ActiveLayerIndex = Mathf.Clamp(ActiveLayerIndex, 0, Layers.Count - 1);
            InvalidateFlattenCache();
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
            InvalidateFlattenCache();
        }

        public void DuplicateLayer(int index)
        {
            if (index < 0 || index >= Layers.Count) return;
            var clone = Layers[index].Clone();
            Layers.Insert(index + 1, clone);
            ActiveLayerIndex = index + 1;
            InvalidateFlattenCache();
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
                InvalidateFlattenCache();
                return;
            }

            int w = lower.width;
            int h = lower.height;
            for (int i = 0; i < w * h; i++)
            {
                Color topPixel = (upper.width == w && upper.height == h && upper.pixels != null)
                    ? upper.pixels[i] : Color.clear;
                lower.pixels[i] = BlendPixels(lower.pixels[i], topPixel, upper.blendMode, upper.opacity);
            }
            Layers.RemoveAt(index);
            ActiveLayerIndex = Mathf.Clamp(index - 1, 0, Layers.Count - 1);
            InvalidateFlattenCache();
        }

        public void InvalidateFlattenCache()
        {
            flattenCacheValid = false;
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
            int stateHash = ComputeFlattenStateHash();
            if (flattenCacheValid && flattenCache != null && flattenCache.Length == w * h && flattenCacheHash == stateHash)
                return flattenCache;

            Color[] result = new Color[w * h];

            for (int layerIdx = 0; layerIdx < Layers.Count; layerIdx++)
            {
                var layer = Layers[layerIdx];
                if (!layer.visible || layer.opacity <= 0f) continue;
                // Skip group headers (they don't render pixels directly)
                // グループヘッダーはスキップ（直接ピクセルを描画しない）
                if (layer.isGroup) continue;
                // Skip layers inside invisible parent groups
                // 非表示の親グループ内のレイヤーをスキップ
                if (!IsLayerEffectivelyVisible(layerIdx)) continue;
                if (layer.pixels == null) continue;

                bool sameSize = (layer.width == w && layer.height == h && layer.pixels.Length == result.Length);
                bool hasLayerMask = layer.mask != null && layer.maskEnabled;
                bool supportsGpuBlend = layer.blendMode <= MaskBlendMode.Exclusion;

                // Try GPU path for same-size layers without transform / 同サイズ・変形なしレイヤーはGPUパスを試行
                if (sameSize && !layer.HasTransform && !layer.isClippingMask && !hasLayerMask && supportsGpuBlend &&
                    MaskTextureComputeDispatcher.TryFlattenLayerGPU(
                        result, layer.pixels, result, w, h, layer.opacity, layer.blendMode))
                {
                    continue;
                }

                // Pre-compute clipping mask base layer index / クリッピングマスクのベースレイヤーインデックスを事前計算
                int clipBaseIdx = -1;
                bool clipBaseSameSize = false;
                if (layer.isClippingMask && layerIdx > 0)
                {
                    for (int bi = layerIdx - 1; bi >= 0; bi--)
                    {
                        if (!Layers[bi].isClippingMask)
                        {
                            clipBaseIdx = bi;
                            clipBaseSameSize = (Layers[bi].width == w && Layers[bi].height == h);
                            break;
                        }
                    }
                }

                // Pre-compute layer mask grayscale / レイヤーマスクのグレースケールを事前計算
                float[] maskGray = null;
                if (hasLayerMask && sameSize)
                {
                    maskGray = new float[result.Length];
                    Color[] mask = layer.mask;
                    for (int mi = 0; mi < result.Length; mi++)
                    {
                        maskGray[mi] = (mask[mi].r + mask[mi].g + mask[mi].b) * 0.333333f;
                    }
                }

                // CPU fallback / CPUフォールバック
                for (int i = 0; i < result.Length; i++)
                {
                    Color topPixel;
                    if (layer.HasTransform)
                    {
                        int x = i % w;
                        int y = i / w;
                        float u = (float)x / Mathf.Max(1, w - 1);
                        float v = (float)y / Mathf.Max(1, h - 1);
                        topPixel = layer.SampleBilinearTransformed(u, v);
                    }
                    else if (sameSize)
                    {
                        topPixel = layer.pixels[i];
                    }
                    else
                    {
                        int x = i % w;
                        int y = i / w;
                        float u = (float)x / Mathf.Max(1, w - 1);
                        float v = (float)y / Mathf.Max(1, h - 1);
                        topPixel = layer.SampleBilinear(u, v);
                    }
                    // Apply clipping mask / クリッピングマスク適用
                    if (clipBaseIdx >= 0)
                    {
                        float clipAlpha = clipBaseSameSize && Layers[clipBaseIdx].pixels != null
                            ? Layers[clipBaseIdx].pixels[i].a : 0f;
                        topPixel = new Color(topPixel.r, topPixel.g, topPixel.b, topPixel.a * clipAlpha);
                    }

                    // Apply layer mask / レイヤーマスク適用
                    if (maskGray != null)
                    {
                        topPixel = new Color(topPixel.r, topPixel.g, topPixel.b, topPixel.a * maskGray[i]);
                    }

                    result[i] = BlendPixels(result[i], topPixel, layer.blendMode, layer.opacity);
                }
            }
            flattenCache = result;
            flattenCacheHash = stateHash;
            flattenCacheValid = true;
            return flattenCache;
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

        private int ComputeFlattenStateHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Width;
                hash = hash * 31 + Height;
                hash = hash * 31 + Layers.Count;

                for (int i = 0; i < Layers.Count; i++)
                {
                    MaskTextureLayer layer = Layers[i];
                    if (layer == null)
                    {
                        hash = hash * 31;
                        continue;
                    }

                    hash = hash * 31 + (layer.visible ? 1 : 0);
                    hash = hash * 31 + Mathf.RoundToInt(layer.opacity * 1000f);
                    hash = hash * 31 + (int)layer.blendMode;
                    hash = hash * 31 + (layer.locked ? 1 : 0);
                    hash = hash * 31 + (layer.lockTransparentPixels ? 1 : 0);
                    hash = hash * 31 + (layer.isClippingMask ? 1 : 0);
                    hash = hash * 31 + (layer.maskEnabled ? 1 : 0);
                    hash = hash * 31 + layer.width;
                    hash = hash * 31 + layer.height;
                    hash = hash * 31 + Mathf.RoundToInt(layer.transformOffset.x * 1000f);
                    hash = hash * 31 + Mathf.RoundToInt(layer.transformOffset.y * 1000f);
                    hash = hash * 31 + Mathf.RoundToInt(layer.transformScale.x * 1000f);
                    hash = hash * 31 + Mathf.RoundToInt(layer.transformScale.y * 1000f);
                    hash = hash * 31 + layer.GetPixelStateHash();

                    if (layer.mask != null)
                    {
                        hash = hash * 31 + 1;
                        hash = hash * 31 + layer.GetMaskStateHash();
                    }
                }

                return hash;
            }
        }

        /// <summary>
        /// Blend two pixels using the specified blend mode and opacity.
        /// 指定ブレンドモードと不透明度で2つのピクセルをブレンドする
        /// </summary>
        internal static Color BlendPixels(Color bottom, Color top, MaskBlendMode mode, float opacity)
        {
            float effectiveAlpha = Mathf.Clamp01(top.a * opacity);
            if (effectiveAlpha <= 0.0001f)
                return bottom;

            Color blendedRgb = BlendRgb(bottom, top, mode);
            return CompositeAlpha(bottom, blendedRgb, effectiveAlpha);
        }

        private static Color BlendRgb(Color bottom, Color top, MaskBlendMode mode)
        {
            switch (mode)
            {
                case MaskBlendMode.Normal:
                    return new Color(top.r, top.g, top.b, 1f);
                case MaskBlendMode.Multiply:
                    return new Color(
                        bottom.r * top.r,
                        bottom.g * top.g,
                        bottom.b * top.b, 1f);
                case MaskBlendMode.Add:
                    return new Color(
                        Mathf.Clamp01(bottom.r + top.r),
                        Mathf.Clamp01(bottom.g + top.g),
                        Mathf.Clamp01(bottom.b + top.b), 1f);
                case MaskBlendMode.Subtract:
                    return new Color(
                        Mathf.Clamp01(bottom.r - top.r),
                        Mathf.Clamp01(bottom.g - top.g),
                        Mathf.Clamp01(bottom.b - top.b), 1f);
                case MaskBlendMode.Overlay:
                    return new Color(
                        OverlayChannel(bottom.r, top.r),
                        OverlayChannel(bottom.g, top.g),
                        OverlayChannel(bottom.b, top.b), 1f);
                case MaskBlendMode.Screen:
                    return new Color(
                        1f - (1f - bottom.r) * (1f - top.r),
                        1f - (1f - bottom.g) * (1f - top.g),
                        1f - (1f - bottom.b) * (1f - top.b), 1f);
                case MaskBlendMode.ColorDodge:
                    return new Color(
                        DodgeChannel(bottom.r, top.r),
                        DodgeChannel(bottom.g, top.g),
                        DodgeChannel(bottom.b, top.b), 1f);
                case MaskBlendMode.ColorBurn:
                    return new Color(
                        BurnChannel(bottom.r, top.r),
                        BurnChannel(bottom.g, top.g),
                        BurnChannel(bottom.b, top.b), 1f);
                case MaskBlendMode.SoftLight:
                    return new Color(
                        SoftLightChannel(bottom.r, top.r),
                        SoftLightChannel(bottom.g, top.g),
                        SoftLightChannel(bottom.b, top.b), 1f);
                case MaskBlendMode.HardLight:
                    return new Color(
                        OverlayChannel(top.r, bottom.r),
                        OverlayChannel(top.g, bottom.g),
                        OverlayChannel(top.b, bottom.b), 1f);
                case MaskBlendMode.Difference:
                    return new Color(
                        Mathf.Abs(bottom.r - top.r),
                        Mathf.Abs(bottom.g - top.g),
                        Mathf.Abs(bottom.b - top.b), 1f);
                case MaskBlendMode.Exclusion:
                    return new Color(
                        bottom.r + top.r - 2f * bottom.r * top.r,
                        bottom.g + top.g - 2f * bottom.g * top.g,
                        bottom.b + top.b - 2f * bottom.b * top.b, 1f);
                case MaskBlendMode.Hue:
                case MaskBlendMode.Saturation:
                case MaskBlendMode.ColorBlend:
                case MaskBlendMode.Luminosity:
                    return HSLBlend(bottom, top, mode);
                default:
                    return new Color(top.r, top.g, top.b, 1f);
            }
        }

        private static Color CompositeAlpha(Color bottom, Color topRgb, float topAlpha)
        {
            float bottomAlpha = Mathf.Clamp01(bottom.a);
            float outAlpha = topAlpha + bottomAlpha * (1f - topAlpha);
            if (outAlpha <= 0.0001f)
                return Color.clear;

            float outR = ((topRgb.r * topAlpha) + (bottom.r * bottomAlpha * (1f - topAlpha))) / outAlpha;
            float outG = ((topRgb.g * topAlpha) + (bottom.g * bottomAlpha * (1f - topAlpha))) / outAlpha;
            float outB = ((topRgb.b * topAlpha) + (bottom.b * bottomAlpha * (1f - topAlpha))) / outAlpha;
            return new Color(outR, outG, outB, outAlpha);
        }

        private static float OverlayChannel(float a, float b)
        {
            return a < 0.5f ? 2f * a * b : 1f - 2f * (1f - a) * (1f - b);
        }

        private static float DodgeChannel(float b, float t)
        {
            return t >= 1f ? 1f : Mathf.Clamp01(b / (1f - t));
        }

        private static float BurnChannel(float b, float t)
        {
            return t <= 0f ? 0f : Mathf.Clamp01(1f - (1f - b) / t);
        }

        private static float SoftLightChannel(float b, float t)
        {
            if (t < 0.5f)
                return b - (1f - 2f * t) * b * (1f - b);
            float d = b <= 0.25f ? ((16f * b - 12f) * b + 4f) * b : Mathf.Sqrt(b);
            return b + (2f * t - 1f) * (d - b);
        }

        private static Color HSLBlend(Color bottom, Color top, MaskBlendMode mode)
        {
            float bH, bS, bL, tH, tS, tL;
            RGBToHSL(bottom, out bH, out bS, out bL);
            RGBToHSL(top, out tH, out tS, out tL);

            float rH = bH, rS = bS, rL = bL;
            switch (mode)
            {
                case MaskBlendMode.Hue:        rH = tH; break;
                case MaskBlendMode.Saturation: rS = tS; break;
                case MaskBlendMode.ColorBlend: rH = tH; rS = tS; break;
                case MaskBlendMode.Luminosity: rL = tL; break;
            }
            Color result = HSLToRGB(rH, rS, rL);
            result.a = 1f;
            return result;
        }

        private static void RGBToHSL(Color c, out float h, out float s, out float l)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            l = (max + min) * 0.5f;

            if (Mathf.Approximately(max, min))
            {
                h = 0f; s = 0f; return;
            }

            float d = max - min;
            s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

            if (Mathf.Approximately(max, c.r))
                h = (c.g - c.b) / d + (c.g < c.b ? 6f : 0f);
            else if (Mathf.Approximately(max, c.g))
                h = (c.b - c.r) / d + 2f;
            else
                h = (c.r - c.g) / d + 4f;
            h /= 6f;
        }

        private static Color HSLToRGB(float h, float s, float l)
        {
            if (Mathf.Approximately(s, 0f))
                return new Color(l, l, l, 1f);

            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;
            float r = HueToRGB(p, q, h + 1f / 3f);
            float g = HueToRGB(p, q, h);
            float b = HueToRGB(p, q, h - 1f / 3f);
            return new Color(r, g, b, 1f);
        }

        private static float HueToRGB(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 0.5f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        /// <summary>
        /// Check if partial flatten is possible (all layers same size, no transforms, no clipping).
        /// 部分フラットンが可能かチェック
        /// </summary>
        public bool CanPartialFlatten()
        {
            if (Layers.Count == 0) return false;
            int w = Width;
            int h = Height;

            for (int i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                if (!layer.visible || layer.opacity <= 0f) continue;
                if (layer.pixels == null) continue;

                // Must be same size / 同サイズでなければならない
                if (layer.width != w || layer.height != h) return false;
                // No transforms / トランスフォームなし
                if (layer.HasTransform) return false;
                // No clipping masks / クリッピングマスクなし
                if (layer.isClippingMask) return false;
                // No layer masks / レイヤーマスクなし
                if (layer.mask != null && layer.maskEnabled) return false;
            }
            return true;
        }

        /// <summary>
        /// Flatten only the specified region into the existing result array.
        /// 指定領域のみを既存の結果配列にフラットン
        /// </summary>
        public void FlattenRegion(Color[] result, int rx, int ry, int rw, int rh)
        {
            if (result == null || Layers.Count == 0) return;
            int w = Width;
            int h = Height;
            if (result.Length != w * h) return;

            // Clamp region to canvas / 領域をキャンバスにクランプ
            int x0 = Mathf.Max(0, rx);
            int y0 = Mathf.Max(0, ry);
            int x1 = Mathf.Min(w, rx + rw);
            int y1 = Mathf.Min(h, ry + rh);
            if (x0 >= x1 || y0 >= y1) return;

            // Clear the region first / 領域をまずクリア
            for (int y = y0; y < y1; y++)
            {
                int rowBase = y * w;
                for (int x = x0; x < x1; x++)
                    result[rowBase + x] = Color.clear;
            }

            // Blend each visible layer into the region / 各可視レイヤーを領域にブレンド
            for (int layerIdx = 0; layerIdx < Layers.Count; layerIdx++)
            {
                var layer = Layers[layerIdx];
                if (!layer.visible || layer.opacity <= 0f) continue;
                if (layer.isGroup) continue;
                if (!IsLayerEffectivelyVisible(layerIdx)) continue;
                if (layer.pixels == null) continue;

                bool sameSize = (layer.width == w && layer.height == h && layer.pixels.Length == result.Length);
                if (!sameSize || layer.HasTransform || layer.isClippingMask ||
                    (layer.mask != null && layer.maskEnabled))
                {
                    // Fall back: complex layer found — do a full flatten instead
                    // 複雑なレイヤーが見つかった場合はフルフラットンにフォールバック
                    Color[] full = Flatten();
                    if (full != null && full != result)
                    {
                        for (int y = y0; y < y1; y++)
                        {
                            int rowBase = y * w;
                            for (int x = x0; x < x1; x++)
                                result[rowBase + x] = full[rowBase + x];
                        }
                    }
                    return;
                }

                // Native region blend / ネイティブ領域ブレンド
                {
                    GCHandle hResult = default, hTop = default;
                    try
                    {
                        IntPtr pResult = NativeBrushBridge.PinArray(result, out hResult);
                        IntPtr pTop = NativeBrushBridge.PinArray(layer.pixels, out hTop);
                        NativeBrushBridge.BlendLayerRegion(
                            pResult, pTop, pResult,
                            w, h, x0, y0, x1 - x0, y1 - y0,
                            layer.opacity, (int)layer.blendMode);
                        continue;
                    }
                    catch (Exception)
                    {
                        // DLL call failed, fall through to CPU path
                    }
                    finally
                    {
                        if (hResult.IsAllocated) hResult.Free();
                        if (hTop.IsAllocated) hTop.Free();
                    }
                }

                // CPU fallback for the region / 領域のCPUフォールバック
                for (int y = y0; y < y1; y++)
                {
                    int rowBase = y * w;
                    for (int x = x0; x < x1; x++)
                    {
                        int idx = rowBase + x;
                        result[idx] = BlendPixels(result[idx], layer.pixels[idx], layer.blendMode, layer.opacity);
                    }
                }
            }
        }

        // ===== Layer Group Management / レイヤーグループ管理 =====

        /// <summary>
        /// Create a new layer group.
        /// 新しいレイヤーグループを作成
        /// </summary>
        public MaskTextureLayer AddGroup(string name)
        {
            var group = new MaskTextureLayer(name, Width, Height);
            group.isGroup = true;
            group.isGroupExpanded = true;
            Layers.Add(group);
            ActiveLayerIndex = Layers.Count - 1;
            InvalidateFlattenCache();
            return group;
        }

        /// <summary>
        /// Create a new layer group at a specific index.
        /// 特定のインデックスに新しいレイヤーグループを作成
        /// </summary>
        public MaskTextureLayer InsertGroup(string name, int index)
        {
            var group = new MaskTextureLayer(name, Width, Height);
            group.isGroup = true;
            group.isGroupExpanded = true;
            index = Mathf.Clamp(index, 0, Layers.Count);
            Layers.Insert(index, group);
            ActiveLayerIndex = index;
            InvalidateFlattenCache();
            return group;
        }

        /// <summary>
        /// Get all layers belonging to a group (direct children only).
        /// グループに属する全レイヤーを取得（直接の子のみ）
        /// </summary>
        public List<MaskTextureLayer> GetGroupChildren(int groupIndex)
        {
            var children = new List<MaskTextureLayer>();
            if (groupIndex < 0 || groupIndex >= Layers.Count || !Layers[groupIndex].isGroup)
                return children;

            int targetDepth = Layers[groupIndex].groupDepth + 1;
            for (int i = groupIndex + 1; i < Layers.Count; i++)
            {
                if (Layers[i].groupDepth < targetDepth) break;
                if (Layers[i].groupDepth == targetDepth)
                    children.Add(Layers[i]);
            }
            return children;
        }

        /// <summary>
        /// Get all layers belonging to a group (all descendants).
        /// グループに属する全レイヤーを取得（全子孫）
        /// </summary>
        public List<MaskTextureLayer> GetGroupDescendants(int groupIndex)
        {
            var descendants = new List<MaskTextureLayer>();
            if (groupIndex < 0 || groupIndex >= Layers.Count || !Layers[groupIndex].isGroup)
                return descendants;

            int baseDepth = Layers[groupIndex].groupDepth;
            for (int i = groupIndex + 1; i < Layers.Count; i++)
            {
                if (Layers[i].groupDepth <= baseDepth) break;
                descendants.Add(Layers[i]);
            }
            return descendants;
        }

        /// <summary>
        /// Toggle group expanded/collapsed state.
        /// グループの展開/折りたたみ状態を切り替え
        /// </summary>
        public void ToggleGroupExpanded(int groupIndex)
        {
            if (groupIndex >= 0 && groupIndex < Layers.Count && Layers[groupIndex].isGroup)
                Layers[groupIndex].isGroupExpanded = !Layers[groupIndex].isGroupExpanded;
        }

        /// <summary>
        /// Check if a layer is visible considering parent group visibility.
        /// 親グループの表示状態を考慮してレイヤーが表示されるかチェック
        /// </summary>
        public bool IsLayerEffectivelyVisible(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= Layers.Count) return false;
            var layer = Layers[layerIndex];
            if (!layer.visible) return false;

            // Walk up the hierarchy to check parent group visibility
            // 階層を上方向に走査して親グループの表示状態を確認
            if (layer.parentGroupIndex >= 0 && layer.parentGroupIndex < Layers.Count)
            {
                var parent = Layers[layer.parentGroupIndex];
                if (!parent.visible) return false;
                return IsLayerEffectivelyVisible(layer.parentGroupIndex);
            }
            return true;
        }

        /// <summary>
        /// Add a layer into a group at the end of the group's children.
        /// グループの子の末尾にレイヤーを追加
        /// </summary>
        public void AddLayerToGroup(int groupIndex, MaskTextureLayer layer)
        {
            if (groupIndex < 0 || groupIndex >= Layers.Count || !Layers[groupIndex].isGroup || layer == null)
                return;

            layer.groupDepth = Layers[groupIndex].groupDepth + 1;
            layer.parentGroupIndex = groupIndex;

            // Find the end of the group
            int insertAt = groupIndex + 1;
            int baseDepth = Layers[groupIndex].groupDepth;
            for (int i = groupIndex + 1; i < Layers.Count; i++)
            {
                if (Layers[i].groupDepth <= baseDepth) break;
                insertAt = i + 1;
            }

            Layers.Insert(insertAt, layer);
            ActiveLayerIndex = insertAt;
            InvalidateFlattenCache();
        }

        /// <summary>
        /// Remove a group and optionally all its children.
        /// グループとオプションでその全子を削除
        /// </summary>
        public void RemoveGroup(int groupIndex, bool removeChildren)
        {
            if (groupIndex < 0 || groupIndex >= Layers.Count || !Layers[groupIndex].isGroup)
                return;

            if (removeChildren)
            {
                var descendants = GetGroupDescendants(groupIndex);
                // Remove from bottom up to preserve indices
                for (int i = descendants.Count - 1; i >= 0; i--)
                {
                    int idx = Layers.IndexOf(descendants[i]);
                    if (idx >= 0) Layers.RemoveAt(idx);
                }
            }
            else
            {
                // Ungroup: promote children to parent depth
                int baseDepth = Layers[groupIndex].groupDepth;
                int parentGroup = Layers[groupIndex].parentGroupIndex;
                for (int i = groupIndex + 1; i < Layers.Count; i++)
                {
                    if (Layers[i].groupDepth <= baseDepth) break;
                    Layers[i].groupDepth--;
                    if (Layers[i].groupDepth == baseDepth)
                        Layers[i].parentGroupIndex = parentGroup;
                }
            }

            Layers.RemoveAt(groupIndex);
            ActiveLayerIndex = Mathf.Clamp(ActiveLayerIndex, 0, Mathf.Max(0, Layers.Count - 1));
            InvalidateFlattenCache();
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
        private static GUIStyle activeLayerNameFieldStyle;
        private static GUIStyle layerBadgeStyle;
        private static GUIStyle activeLayerInfoStyle;

        private static readonly Color ActiveBadgeColor = new Color(0.28f, 0.58f, 0.95f);
        private static readonly Color MaskBadgeColor = new Color(0.18f, 0.72f, 0.72f);
        private static readonly Color LayerBadgeColor = new Color(0.42f, 0.42f, 0.42f);
        private static readonly Color LockBadgeColor = new Color(0.78f, 0.32f, 0.32f);
        private static readonly Color AlphaLockBadgeColor = new Color(0.68f, 0.56f, 0.2f);

        private static void EnsureStyles()
        {
            if (activeLayerStyle == null)
            {
                activeLayerStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(4, 4, 2, 2),
                    margin = new RectOffset(0, 0, 1, 1)
                };
                inactiveLayerStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(4, 4, 2, 2),
                    margin = new RectOffset(0, 0, 1, 1)
                };
                activeLayerNameFieldStyle = new GUIStyle(EditorStyles.textField)
                {
                    fontStyle = FontStyle.Bold
                };
                layerBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    clipping = TextClipping.Clip,
                    fixedHeight = 16f,
                    padding = new RectOffset(5, 5, 1, 1),
                    normal = { textColor = Color.white }
                };
                activeLayerInfoStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(6, 6, 4, 4),
                    margin = new RectOffset(0, 0, 2, 2)
                };
            }
        }

        private static void DrawLayerBadge(string text, Color color, float width)
        {
            Rect badgeRect = GUILayoutUtility.GetRect(
                width,
                layerBadgeStyle.fixedHeight,
                layerBadgeStyle,
                GUILayout.Width(width),
                GUILayout.Height(layerBadgeStyle.fixedHeight));
            EditorGUI.DrawRect(badgeRect, color);
            EditorGUI.LabelField(badgeRect, text, layerBadgeStyle);
        }

        private static bool TryImportTexture(MaskLayerStack stack, Texture2D tex)
        {
            if (tex == null || stack == null) return false;
            var layer = stack.AddLayer(tex.name);
            layer.ImportFromTexture(tex);
            layer.sourceType = MaskTextureLayer.SourceType.Import;
            return true;
        }

        /// <summary>
        /// Draw the full layer panel UI.
        /// レイヤーパネルUIを描画する
        /// </summary>
        /// <param name="stack">The layer stack to draw.</param>
        /// <param name="scrollPosition">Scroll position reference for the layer list.</param>
        /// <returns>True if any value changed (for repaint).</returns>
        public static bool DrawLayerPanel(MaskLayerStack stack, ref Vector2 scrollPosition, float minListHeight = 240f, float panelHeight = 0f)
        {
            EnsureStyles();
            bool changed = false;
            bool showHeaderActiveInfo = false;

            if (panelHeight > 0f)
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(panelHeight), GUILayout.ExpandHeight(true));
            else
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField(L("レイヤー", "Layers"), EditorStyles.boldLabel);

            // Toolbar
            changed |= DrawToolbar(stack);

            // Active layer info
            if (showHeaderActiveInfo && stack.ActiveLayer != null)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal(activeLayerInfoStyle);
                DrawLayerBadge(L("ACTIVE", "ACTIVE"), ActiveBadgeColor, 58f);
                GUILayout.Space(4f);
                EditorGUILayout.LabelField(
                    L($"アクティブ: {stack.ActiveLayer.name} ({stack.ActiveLayer.width}x{stack.ActiveLayer.height})",
                      $"Active: {stack.ActiveLayer.name} ({stack.ActiveLayer.width}x{stack.ActiveLayer.height})"),
                    EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(3);

            // Layer list (top layer = highest index, drawn first)
            float preferredMinListHeight = Mathf.Max(220f, minListHeight);
            float headerHeightBudget = 68f;
            float scrollViewHeight = panelHeight > 0f
                ? Mathf.Max(preferredMinListHeight, panelHeight - headerHeightBudget)
                : preferredMinListHeight;
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.Height(scrollViewHeight),
                GUILayout.ExpandHeight(panelHeight <= 0f));

            for (int i = stack.Layers.Count - 1; i >= 0; i--)
            {
                changed |= DrawLayerEntry(stack, i);
            }

            EditorGUILayout.EndScrollView();

            // Drag & drop texture import
            Rect panelRect = GUILayoutUtility.GetLastRect();
            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                bool hasTexture = false;
                foreach (var obj in DragAndDrop.objectReferences)
                    if (obj is Texture2D) { hasTexture = true; break; }

                if (hasTexture)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                            if (obj is Texture2D tex)
                                changed |= TryImportTexture(stack, tex);
                        evt.Use();
                    }
                }
            }

            EditorGUILayout.EndVertical();
            return changed;
        }

        private static bool DrawToolbar(MaskLayerStack stack)
        {
            bool changed = false;
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("+", L("新規レイヤー追加", "Add new layer")), GUILayout.Width(25)))
            {
                stack.AddLayer($"Layer {stack.Layers.Count + 1}");
                changed = true;
            }

            GUI.enabled = stack.Layers.Count > 1;
            if (GUILayout.Button(new GUIContent("-", L("レイヤー削除", "Remove layer")), GUILayout.Width(25)))
            {
                stack.RemoveLayer(stack.ActiveLayerIndex);
                changed = true;
            }
            GUI.enabled = true;

            GUI.enabled = stack.ActiveLayerIndex < stack.Layers.Count - 1;
            if (GUILayout.Button(new GUIContent("\u25B2", L("上へ移動", "Move up")), GUILayout.Width(25)))
            {
                stack.MoveLayer(stack.ActiveLayerIndex, stack.ActiveLayerIndex + 1);
                changed = true;
            }
            GUI.enabled = true;

            GUI.enabled = stack.ActiveLayerIndex > 0;
            if (GUILayout.Button(new GUIContent("\u25BC", L("下へ移動", "Move down")), GUILayout.Width(25)))
            {
                stack.MoveLayer(stack.ActiveLayerIndex, stack.ActiveLayerIndex - 1);
                changed = true;
            }
            GUI.enabled = true;

            GUI.enabled = stack.Layers.Count > 0;
            if (GUILayout.Button(new GUIContent(L("複製", "Dup"), L("レイヤーを複製", "Duplicate layer")), GUILayout.Width(48)))
            {
                stack.DuplicateLayer(stack.ActiveLayerIndex);
                changed = true;
            }

            GUI.enabled = stack.ActiveLayerIndex > 0 && stack.Layers.Count > 1;
            if (GUILayout.Button(new GUIContent(L("結合", "Merge"), L("下のレイヤーと結合", "Merge down")), GUILayout.Width(48)))
            {
                stack.MergeDown(stack.ActiveLayerIndex);
                changed = true;
            }

            GUI.enabled = true;

            if (GUILayout.Button(
                new GUIContent(L("読込", "Import"), L("テクスチャをレイヤーとして追加", "Import texture as new layer")),
                GUILayout.Width(44)))
            {
                string path = EditorUtility.OpenFilePanelWithFilters(
                    L("テクスチャを開く", "Open Texture"),
                    "Assets",
                    new[] { "Image files", "png,tga,exr,jpg,jpeg,psd", "All files", "*" });

                if (!string.IsNullOrEmpty(path))
                {
                    string assetPath = path.StartsWith(Application.dataPath)
                        ? "Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/')
                        : null;
                    Texture2D tex = assetPath != null
                        ? AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath)
                        : null;
                    if (tex == null)
                    {
                        byte[] data = System.IO.File.ReadAllBytes(path);
                        tex = new Texture2D(2, 2);
                        tex.LoadImage(data);
                        tex.name = System.IO.Path.GetFileNameWithoutExtension(path);
                    }
                    if (TryImportTexture(stack, tex))
                        changed = true;
                }
            }

            EditorGUILayout.EndHorizontal();
            return changed;
        }

        private static void AddBlendModeCategory(GenericMenu menu, MaskTextureLayer layer, string category, params MaskBlendMode[] modes)
        {
            foreach (var mode in modes)
            {
                var m = mode;
                menu.AddItem(
                    new GUIContent($"{category}/{m}"),
                    layer.blendMode == m,
                    () => { layer.blendMode = m; });
            }
        }

        private static bool DrawLayerEntry(MaskLayerStack stack, int index)
        {
            bool changed = false;
            var layer = stack.Layers[index];
            bool isActive = (index == stack.ActiveLayerIndex);

            var bgColor = GUI.backgroundColor;
            if (isActive)
                GUI.backgroundColor = new Color(0.72f, 0.86f, 1f);

            // Clipping mask indent
            if (layer.isClippingMask)
            {
                GUILayout.Space(16);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("\u21B3", GUILayout.Width(14));
            }

            // Make entire row clickable
            Rect entryRect = EditorGUILayout.BeginVertical(isActive ? activeLayerStyle : inactiveLayerStyle);

            // Row 1: visibility, thumbnail, name, lock
            EditorGUILayout.BeginHorizontal();

            // Visibility toggle (eye icon)
            EditorGUI.BeginChangeCheck();
            layer.visible = EditorGUILayout.Toggle(layer.visible, GUILayout.Width(15));
            if (EditorGUI.EndChangeCheck()) changed = true;

            // Thumbnail (48x48)
            Texture2D thumb = (layer.editingMask && layer.mask != null)
                ? layer.GetMaskThumbnail()
                : layer.GetThumbnail();
            if (thumb != null)
            {
                Rect thumbRect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
                EditorGUI.DrawPreviewTexture(thumbRect, thumb);
            }

            // Name and state badges
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            bool showStatusRow = isActive || layer.mask != null || layer.locked || layer.lockTransparentPixels;
            if (showStatusRow)
            {
                EditorGUILayout.BeginHorizontal();
                if (isActive)
                    DrawLayerBadge(L("ACTIVE", "ACTIVE"), ActiveBadgeColor, 58f);
                if (layer.mask != null)
                    DrawLayerBadge(
                        layer.editingMask ? L("MASK", "MASK") : L("LAYER", "LAYER"),
                        layer.editingMask ? MaskBadgeColor : LayerBadgeColor,
                        54f);
                if (layer.locked)
                    DrawLayerBadge(L("LOCK", "LOCK"), LockBadgeColor, 48f);
                else if (layer.lockTransparentPixels)
                    DrawLayerBadge(L("ALPHA", "ALPHA"), AlphaLockBadgeColor, 50f);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.BeginChangeCheck();
            layer.name = EditorGUILayout.TextField(
                layer.name,
                isActive ? activeLayerNameFieldStyle : EditorStyles.textField,
                GUILayout.MinWidth(60));
            if (EditorGUI.EndChangeCheck()) changed = true;
            EditorGUILayout.EndVertical();

            // Combined lock button (3-state cycle) with improved icons
            string lockLabel;
            string lockTooltip;
            if (layer.locked)
            {
                lockLabel = "\u26D4";
                lockTooltip = L("完全ロック (クリックで透明ロックへ)", "Full Lock (click for transparent lock)");
            }
            else if (layer.lockTransparentPixels)
            {
                lockLabel = "\u26BF";
                lockTooltip = L("透明ロック (クリックで解除)", "Transparent Lock (click to unlock)");
            }
            else
            {
                lockLabel = "\u26AA";
                lockTooltip = L("未ロック (クリックでロック)", "Unlocked (click to lock)");
            }

            if (GUILayout.Button(new GUIContent(lockLabel, lockTooltip), GUILayout.Width(22)))
            {
                // Cycle: unlocked -> locked -> transparent lock -> unlocked
                if (!layer.locked && !layer.lockTransparentPixels)
                {
                    layer.locked = true;
                    layer.lockTransparentPixels = false;
                }
                else if (layer.locked)
                {
                    layer.locked = false;
                    layer.lockTransparentPixels = true;
                }
                else
                {
                    layer.locked = false;
                    layer.lockTransparentPixels = false;
                }
                changed = true;
            }

            EditorGUILayout.EndHorizontal();

            // Row 2: blend mode (GenericMenu with categories) + opacity
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent(layer.blendMode.ToString(), L("レイヤーブレンドモード", "Layer blend mode")),
                EditorStyles.popup, GUILayout.Width(100)))
            {
                var menu = new GenericMenu();
                AddBlendModeCategory(menu, layer, L("基本", "Basic"), MaskBlendMode.Normal);
                menu.AddSeparator("");
                AddBlendModeCategory(menu, layer, L("暗く", "Darken"), MaskBlendMode.Multiply, MaskBlendMode.ColorBurn);
                menu.AddSeparator("");
                AddBlendModeCategory(menu, layer, L("明るく", "Lighten"), MaskBlendMode.Screen, MaskBlendMode.Add, MaskBlendMode.ColorDodge);
                menu.AddSeparator("");
                AddBlendModeCategory(menu, layer, L("コントラスト", "Contrast"), MaskBlendMode.Overlay, MaskBlendMode.SoftLight, MaskBlendMode.HardLight);
                menu.AddSeparator("");
                AddBlendModeCategory(menu, layer, L("差分", "Difference"), MaskBlendMode.Subtract, MaskBlendMode.Difference, MaskBlendMode.Exclusion);
                menu.AddSeparator("");
                AddBlendModeCategory(menu, layer, L("HSL", "HSL"), MaskBlendMode.Hue, MaskBlendMode.Saturation, MaskBlendMode.ColorBlend, MaskBlendMode.Luminosity);
                menu.ShowAsContext();
            }

            EditorGUI.BeginChangeCheck();
            layer.opacity = EditorGUILayout.Slider(
                new GUIContent("", L("レイヤー不透明度", "Layer opacity")),
                layer.opacity, 0f, 1f);
            if (EditorGUI.EndChangeCheck()) changed = true;

            EditorGUILayout.EndHorizontal();

            // Layer transform (active layer only)
            if (isActive)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                layer.transformOffset = EditorGUILayout.Vector2Field(
                    new GUIContent(L("オフセット", "Offset")),
                    layer.transformOffset);
                if (EditorGUI.EndChangeCheck()) changed = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                layer.transformScale = EditorGUILayout.Vector2Field(
                    new GUIContent(L("スケール", "Scale")),
                    layer.transformScale);
                layer.transformScale = new Vector2(
                    Mathf.Max(0.01f, layer.transformScale.x),
                    Mathf.Max(0.01f, layer.transformScale.y));
                if (EditorGUI.EndChangeCheck()) changed = true;
                EditorGUILayout.EndHorizontal();

                // Row 3: Mask operations (active layer only)
                EditorGUILayout.BeginHorizontal();
                if (layer.mask == null)
                {
                    if (GUILayout.Button(L("マスク作成", "Add Mask"), EditorStyles.miniButton))
                    {
                        layer.CreateMask();
                        changed = true;
                    }
                    if (GUILayout.Button(L("αからマスク", "Mask from Alpha"), EditorStyles.miniButton))
                    {
                        layer.CreateMaskFromAlpha();
                        changed = true;
                    }
                }
                else
                {
                    layer.maskEnabled = EditorGUILayout.Toggle(
                        GUIContent.none, layer.maskEnabled, GUILayout.Width(15));
                    if (GUILayout.Button(
                        layer.editingMask ? L("レイヤー編集", "Edit Layer") : L("マスク編集", "Edit Mask"),
                        EditorStyles.miniButton))
                    {
                        layer.editingMask = !layer.editingMask;
                        changed = true;
                    }
                    if (GUILayout.Button(L("反転", "Inv"), EditorStyles.miniButton, GUILayout.Width(38)))
                    {
                        layer.InvertMask();
                        changed = true;
                    }
                    if (GUILayout.Button(L("適用", "Apply"), EditorStyles.miniButton, GUILayout.Width(42)))
                    {
                        layer.ApplyMask();
                        changed = true;
                    }
                    if (GUILayout.Button(L("削除", "Del"), EditorStyles.miniButton, GUILayout.Width(38)))
                    {
                        layer.DeleteMask();
                        changed = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // Right-click context menu
            if (Event.current.type == EventType.ContextClick && entryRect.Contains(Event.current.mousePosition))
            {
                stack.ActiveLayerIndex = index;
                var contextMenu = new GenericMenu();
                contextMenu.AddItem(new GUIContent(L("複製", "Duplicate")), false, () => { stack.DuplicateLayer(index); });
                if (index > 0 && stack.Layers.Count > 1)
                    contextMenu.AddItem(new GUIContent(L("下と結合", "Merge Down")), false, () => { stack.MergeDown(index); });
                else
                    contextMenu.AddDisabledItem(new GUIContent(L("下と結合", "Merge Down")));
                if (stack.Layers.Count > 1)
                    contextMenu.AddItem(new GUIContent(L("削除", "Delete")), false, () => { stack.RemoveLayer(index); });
                else
                    contextMenu.AddDisabledItem(new GUIContent(L("削除", "Delete")));
                contextMenu.AddSeparator("");
                contextMenu.AddItem(new GUIContent(L("クリッピングマスク", "Clipping Mask")), layer.isClippingMask, () => { layer.isClippingMask = !layer.isClippingMask; });
                contextMenu.AddSeparator("");
                if (layer.mask == null)
                {
                    contextMenu.AddItem(new GUIContent(L("マスク作成", "Add Mask")), false, () => { layer.CreateMask(); });
                    contextMenu.AddItem(new GUIContent(L("αからマスク", "Mask from Alpha")), false, () => { layer.CreateMaskFromAlpha(); });
                }
                else
                {
                    contextMenu.AddItem(new GUIContent(L("マスク反転", "Invert Mask")), false, () => { layer.InvertMask(); });
                    contextMenu.AddItem(new GUIContent(L("マスク適用", "Apply Mask")), false, () => { layer.ApplyMask(); });
                    contextMenu.AddItem(new GUIContent(L("マスク削除", "Delete Mask")), false, () => { layer.DeleteMask(); });
                }
                contextMenu.AddSeparator("");
                contextMenu.AddItem(new GUIContent(L("レイヤーをクリア", "Clear Layer")), false, () => { layer.Clear(); });
                contextMenu.ShowAsContext();
                Event.current.Use();
                changed = true;
            }

            // Row click for selection (check if click was on the entry but not on a specific control)
            if (Event.current.type == EventType.MouseUp && entryRect.Contains(Event.current.mousePosition))
            {
                if (stack.ActiveLayerIndex != index)
                {
                    stack.ActiveLayerIndex = index;
                    changed = true;
                }
            }

            // Close clipping mask indent
            if (layer.isClippingMask)
            {
                EditorGUILayout.EndHorizontal();
            }

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
            EditorGUILayout.LabelField(L("レイヤー", "Layers"), EditorStyles.boldLabel, GUILayout.Width(80));

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
