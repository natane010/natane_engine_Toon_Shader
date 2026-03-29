using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// GPU compute shader dispatcher for mask texture operations.
    /// マスクテクスチャ操作用GPU Compute Shaderディスパッチャー
    /// Provides GPU-accelerated versions of brush stamps, filters, and layer compositing
    /// with automatic CPU fallback for unsupported platforms.
    /// </summary>
    internal static class MaskTextureComputeDispatcher
    {
        // Compute shader references (lazy loaded)
        private static ComputeShader _brushStampCS;
        private static ComputeShader _gaussianBlurCS;
        private static ComputeShader _sobelEdgeCS;
        private static ComputeShader _layerFlattenCS;

        private static bool _shadersLoaded;
        private static bool _gpuSupported = true;

        /// <summary>Whether GPU compute is available on this system.</summary>
        public static bool IsGPUAvailable
        {
            get
            {
                if (!_gpuSupported) return false;
                if (!SystemInfo.supportsComputeShaders)
                {
                    _gpuSupported = false;
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Load all compute shaders lazily.
        /// 全Compute Shaderを遅延ロード
        /// </summary>
        private static void LoadShaders()
        {
            if (_shadersLoaded) return;
            _shadersLoaded = true;

            _brushStampCS = FindComputeShader("MaskTex_BrushStamp");
            _gaussianBlurCS = FindComputeShader("MaskTex_GaussianBlur");
            _sobelEdgeCS = FindComputeShader("MaskTex_SobelEdge");
            _layerFlattenCS = FindComputeShader("MaskTex_LayerFlatten");
        }

        private static ComputeShader FindComputeShader(string name)
        {
            string[] guids = AssetDatabase.FindAssets($"t:ComputeShader {name}");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[MaskTexture] Compute Shader not found: {name} (falling back to CPU)");
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
        }

        /// <summary>Force reload of all compute shaders.</summary>
        public static void ReloadShaders()
        {
            _shadersLoaded = false;
            _brushStampCS = null;
            _gaussianBlurCS = null;
            _sobelEdgeCS = null;
            _layerFlattenCS = null;
            LoadShaders();
        }

        // === RT Pool ===
        private static readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.Stack<RenderTexture>> rtPool
            = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Stack<RenderTexture>>();

        private static RenderTexture GetPooledRT(int w, int h)
        {
            int key = w * 10000 + h;
            if (rtPool.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                var rt = stack.Pop();
                if (rt != null && rt.IsCreated()) return rt;
            }
            var newRT = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat);
            newRT.enableRandomWrite = true;
            newRT.Create();
            return newRT;
        }

        private static void ReleasePooledRT(RenderTexture rt)
        {
            if (rt == null) return;
            int key = rt.width * 10000 + rt.height;
            if (!rtPool.ContainsKey(key))
                rtPool[key] = new System.Collections.Generic.Stack<RenderTexture>();
            if (rtPool[key].Count < 4) // Max 4 per size
                rtPool[key].Push(rt);
            else
                RenderTexture.ReleaseTemporary(rt);
        }

        /// <summary>Clear the RT pool (call on domain reload or cleanup).</summary>
        public static void ClearPool()
        {
            foreach (var stack in rtPool.Values)
            {
                while (stack.Count > 0)
                {
                    var rt = stack.Pop();
                    if (rt != null) RenderTexture.ReleaseTemporary(rt);
                }
            }
            rtPool.Clear();
            ClearTexturePool();
        }

        // === Texture2D Pool ===
        // Texture2Dオブジェクトプール（毎回の生成/破棄を回避）
        private static readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.Stack<Texture2D>> texPool
            = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Stack<Texture2D>>();

        private static Texture2D GetPooledTexture(int w, int h)
        {
            int key = w * 10000 + h;
            if (texPool.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                var tex = stack.Pop();
                if (tex != null) return tex;
            }
            var newTex = new Texture2D(w, h, TextureFormat.RGBAFloat, false);
            newTex.hideFlags = HideFlags.HideAndDontSave;
            return newTex;
        }

        private static void ReleasePooledTexture(Texture2D tex)
        {
            if (tex == null) return;
            int key = tex.width * 10000 + tex.height;
            if (!texPool.ContainsKey(key))
                texPool[key] = new System.Collections.Generic.Stack<Texture2D>();
            if (texPool[key].Count < 2) // Max 2 per size
                texPool[key].Push(tex);
            else
                Object.DestroyImmediate(tex);
        }

        /// <summary>Clear the Texture2D pool.</summary>
        public static void ClearTexturePool()
        {
            foreach (var stack in texPool.Values)
            {
                while (stack.Count > 0)
                {
                    var tex = stack.Pop();
                    if (tex != null) Object.DestroyImmediate(tex);
                }
            }
            texPool.Clear();
        }

        // ===== RT Utilities =====

        private static void PixelsToRT(Color[] pixels, int w, int h, RenderTexture rt)
        {
            Texture2D tex = GetPooledTexture(w, h);
            try
            {
                tex.SetPixels(pixels);
                tex.Apply();
                Graphics.Blit(tex, rt);
            }
            finally
            {
                ReleasePooledTexture(tex);
            }
        }

        private static void RTToPixels(RenderTexture rt, Color[] pixels, int w, int h)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = GetPooledTexture(w, h);
            try
            {
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                var result = tex.GetPixels();
                System.Array.Copy(result, pixels, Mathf.Min(result.Length, pixels.Length));
            }
            finally
            {
                RenderTexture.active = prev;
                ReleasePooledTexture(tex);
            }
        }

        /// <summary>
        /// Read back only a partial region from a RenderTexture into the pixels array.
        /// RenderTextureからバウンディングボックス領域のみを読み戻す（部分リードバック）
        /// </summary>
        private static void RTToPixelsPartial(RenderTexture rt, Color[] pixels, int fullWidth, int fullHeight,
            int bboxMinX, int bboxMinY, int bboxWidth, int bboxHeight)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = GetPooledTexture(bboxWidth, bboxHeight);
            try
            {
                tex.ReadPixels(new Rect(bboxMinX, bboxMinY, bboxWidth, bboxHeight), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                var partialPixels = tex.GetPixels();
                // バウンディングボックス領域のみをピクセル配列にコピー
                for (int y = 0; y < bboxHeight; y++)
                {
                    int srcOffset = y * bboxWidth;
                    int dstOffset = (bboxMinY + y) * fullWidth + bboxMinX;
                    System.Array.Copy(partialPixels, srcOffset, pixels, dstOffset, bboxWidth);
                }
            }
            finally
            {
                RenderTexture.active = prev;
                ReleasePooledTexture(tex);
            }
        }

        // ===== Brush Stamp GPU =====

        /// <summary>
        /// Apply brush stamp on GPU. Only effective for large brushes (radius > 30).
        /// GPUでブラシスタンプを適用。大きいブラシ（半径30以上）でのみ有効。
        /// </summary>
        /// <returns>True if GPU execution succeeded, false to fall back to CPU.</returns>
        public static bool TryBrushStampGPU(
            Color[] pixels, int width, int height,
            Vector2 center, float radius, float hardness, float opacity,
            float strength, float paintAlpha, BrushMode mode, bool eraseRgb,
            bool colorMode = false, Color? brushColor = null)
        {
            if (!IsGPUAvailable || radius < 30f) return false;

            LoadShaders();
            if (_brushStampCS == null) return false;

            string kernelName;
            switch (mode)
            {
                case BrushMode.Paint: kernelName = "BrushStampPaint"; break;
                case BrushMode.Erase:
                case BrushMode.EraseAlpha: kernelName = "BrushStampErase"; break;
                case BrushMode.Smooth: kernelName = "BrushStampSmooth"; break;
                default: return false;
            }

            try
            {
                int kernel = _brushStampCS.FindKernel(kernelName);
                RenderTexture inputRT = GetPooledRT(width, height);
                RenderTexture resultRT = GetPooledRT(width, height);

                try
                {
                    PixelsToRT(pixels, width, height, inputRT);

                    // Calculate bounding box
                    int bboxMinX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
                    int bboxMinY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
                    int bboxMaxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius));
                    int bboxMaxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + radius));
                    int bboxW = bboxMaxX - bboxMinX + 1;
                    int bboxH = bboxMaxY - bboxMinY + 1;

                    if (bboxW <= 0 || bboxH <= 0)
                        return true; // Nothing to do

                    _brushStampCS.SetInts("_TexSize", width, height);
                    _brushStampCS.SetInts("_BrushBBoxMin", bboxMinX, bboxMinY);
                    _brushStampCS.SetFloats("_BrushCenter", center.x, center.y);
                    _brushStampCS.SetFloat("_BrushRadius", radius);
                    _brushStampCS.SetFloat("_BrushHardness", hardness);
                    _brushStampCS.SetFloat("_BrushOpacity", opacity);
                    _brushStampCS.SetFloat("_BrushStrength", strength);
                    _brushStampCS.SetFloat("_BrushPaintAlpha", paintAlpha);
                    _brushStampCS.SetInt("_EraseRgb", (mode == BrushMode.EraseAlpha) ? 0 : (eraseRgb ? 1 : 0));
                    _brushStampCS.SetInt("_UseColorMode", colorMode ? 1 : 0);
                    if (colorMode && brushColor.HasValue)
                    {
                        var c = brushColor.Value;
                        _brushStampCS.SetFloats("_BrushColor", c.r, c.g, c.b, c.a);
                    }
                    _brushStampCS.SetTexture(kernel, "_Input", inputRT);
                    _brushStampCS.SetTexture(kernel, "_Result", resultRT);

                    // Dispatch only over bounding box area
                    _brushStampCS.Dispatch(kernel,
                        Mathf.CeilToInt(bboxW / 8f),
                        Mathf.CeilToInt(bboxH / 8f), 1);

                    // 変更された領域のみを読み戻す（部分リードバック）
                    RTToPixelsPartial(resultRT, pixels, width, height,
                        bboxMinX, bboxMinY, bboxW, bboxH);
                    return true;
                }
                finally
                {
                    ReleasePooledRT(inputRT);
                    ReleasePooledRT(resultRT);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MaskTexture] GPU brush stamp failed: {ex.Message}. Falling back to CPU.");
                return false;
            }
        }

        // ===== Gaussian Blur GPU =====

        /// <summary>
        /// Apply Gaussian blur on GPU.
        /// GPUでガウシアンブラーを適用
        /// </summary>
        /// <returns>True if GPU execution succeeded.</returns>
        public static bool TryGaussianBlurGPU(Color[] pixels, int width, int height, float sigma)
        {
            if (!IsGPUAvailable) return false;

            LoadShaders();
            if (_gaussianBlurCS == null) return false;

            int radius = Mathf.CeilToInt(sigma * 3f);
            if (radius < 1) radius = 1;

            try
            {
                RenderTexture inputRT = GetPooledRT(width, height);
                RenderTexture tempRT = GetPooledRT(width, height);
                RenderTexture resultRT = GetPooledRT(width, height);

                try
                {
                    PixelsToRT(pixels, width, height, inputRT);

                    int dispatchX = Mathf.CeilToInt(width / 8f);
                    int dispatchY = Mathf.CeilToInt(height / 8f);

                    // Horizontal pass
                    int kH = _gaussianBlurCS.FindKernel("BlurH");
                    _gaussianBlurCS.SetInts("_TexSize", width, height);
                    _gaussianBlurCS.SetInt("_BlurRadius", radius);
                    _gaussianBlurCS.SetFloat("_Sigma", sigma);
                    _gaussianBlurCS.SetTexture(kH, "_Input", inputRT);
                    _gaussianBlurCS.SetTexture(kH, "_Result", tempRT);
                    _gaussianBlurCS.Dispatch(kH, dispatchX, dispatchY, 1);

                    // Vertical pass
                    int kV = _gaussianBlurCS.FindKernel("BlurV");
                    _gaussianBlurCS.SetTexture(kV, "_Input", tempRT);
                    _gaussianBlurCS.SetTexture(kV, "_Result", resultRT);
                    _gaussianBlurCS.Dispatch(kV, dispatchX, dispatchY, 1);

                    RTToPixels(resultRT, pixels, width, height);
                    return true;
                }
                finally
                {
                    ReleasePooledRT(inputRT);
                    ReleasePooledRT(tempRT);
                    ReleasePooledRT(resultRT);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MaskTexture] GPU Gaussian blur failed: {ex.Message}. Falling back to CPU.");
                return false;
            }
        }

        // ===== Sobel Edge GPU =====

        /// <summary>
        /// Apply Sobel edge detection on GPU.
        /// GPUでSobelエッジ検出を適用
        /// </summary>
        public static bool TrySobelEdgeGPU(Color[] pixels, int width, int height, float strength)
        {
            if (!IsGPUAvailable) return false;

            LoadShaders();
            if (_sobelEdgeCS == null) return false;

            try
            {
                RenderTexture inputRT = GetPooledRT(width, height);
                RenderTexture resultRT = GetPooledRT(width, height);

                try
                {
                    PixelsToRT(pixels, width, height, inputRT);

                    int kernel = _sobelEdgeCS.FindKernel("SobelEdge");
                    _sobelEdgeCS.SetInts("_TexSize", width, height);
                    _sobelEdgeCS.SetFloat("_Strength", strength);
                    _sobelEdgeCS.SetTexture(kernel, "_Input", inputRT);
                    _sobelEdgeCS.SetTexture(kernel, "_Result", resultRT);
                    _sobelEdgeCS.Dispatch(kernel,
                        Mathf.CeilToInt(width / 8f),
                        Mathf.CeilToInt(height / 8f), 1);

                    RTToPixels(resultRT, pixels, width, height);
                    return true;
                }
                finally
                {
                    ReleasePooledRT(inputRT);
                    ReleasePooledRT(resultRT);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MaskTexture] GPU Sobel failed: {ex.Message}. Falling back to CPU.");
                return false;
            }
        }

        // ===== Layer Flatten GPU =====

        /// <summary>
        /// Flatten a single layer onto the accumulated result on GPU.
        /// GPUで単一レイヤーを累積結果に合成
        /// </summary>
        public static bool TryFlattenLayerGPU(
            Color[] bottomPixels, Color[] topPixels,
            Color[] resultPixels, int width, int height,
            float layerOpacity, MaskBlendMode blendMode)
        {
            if (!IsGPUAvailable) return false;

            LoadShaders();
            if (_layerFlattenCS == null) return false;

            try
            {
                RenderTexture bottomRT = GetPooledRT(width, height);
                RenderTexture topRT = GetPooledRT(width, height);
                RenderTexture resultRT = GetPooledRT(width, height);

                try
                {
                    PixelsToRT(bottomPixels, width, height, bottomRT);
                    PixelsToRT(topPixels, width, height, topRT);

                    int kernel = _layerFlattenCS.FindKernel("FlattenLayer");
                    _layerFlattenCS.SetInts("_TexSize", width, height);
                    _layerFlattenCS.SetFloat("_LayerOpacity", layerOpacity);
                    _layerFlattenCS.SetInt("_BlendMode", (int)blendMode);
                    _layerFlattenCS.SetInt("_HasMask", 0);
                    _layerFlattenCS.SetTexture(kernel, "_Bottom", bottomRT);
                    _layerFlattenCS.SetTexture(kernel, "_Top", topRT);
                    _layerFlattenCS.SetTexture(kernel, "_MaskTex", topRT);
                    _layerFlattenCS.SetTexture(kernel, "_Result", resultRT);
                    _layerFlattenCS.Dispatch(kernel,
                        Mathf.CeilToInt(width / 8f),
                        Mathf.CeilToInt(height / 8f), 1);

                    RTToPixels(resultRT, resultPixels, width, height);
                    return true;
                }
                finally
                {
                    ReleasePooledRT(bottomRT);
                    ReleasePooledRT(topRT);
                    ReleasePooledRT(resultRT);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MaskTexture] GPU layer flatten failed: {ex.Message}. Falling back to CPU.");
                return false;
            }
        }
    }
}
