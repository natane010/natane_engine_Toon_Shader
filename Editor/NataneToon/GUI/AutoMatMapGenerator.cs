using System;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace NataneToon.Editor
{
    /// <summary>
    /// Generation parameters for all PBR map generators.
    /// </summary>
    [Serializable]
    public class AutoMatGenerationParams
    {
        // Normal
        public float normalStrength = 1.0f;
        public int normalBlurRadius = 0;

        // Roughness
        public int roughnessBlockSize = 8;
        public float roughnessRemapMin = 0.01f;
        public float roughnessRemapMax = 0.05f;
        public float roughnessSaturationWeight = 0.3f;

        // Metallic
        public float metallicLuminanceThreshold = 0.7f;
        public float metallicSaturationThreshold = 0.3f;
        public Vector2 metallicGoldHueRange = new Vector2(0.083f, 0.139f);   // 30-50 degrees
        public Vector2 metallicCopperHueRange = new Vector2(0.028f, 0.069f); // 10-25 degrees
        public float metallicBias = 0.0f;

        // Height
        public float heightContrast = 1.0f;
        public bool heightInvert = false;
        public int heightBlurRadius = 0;

        // AO
        public int aoRadius = 8;
        public float aoStrength = 0.8f;
        public float aoBias = 0.01f;

        // Curvature
        public bool enableCurvature = false;
        public float curvatureScale = 1.0f;
        public float curvatureRoughnessBlend = 0.2f;
        public float curvatureMetallicBlend = 0.1f;
    }

    /// <summary>
    /// Result of PBR map generation.
    /// </summary>
    public class AutoMatGenerationResult
    {
        public Texture2D height;
        public Texture2D normal;
        public Texture2D roughness;
        public Texture2D metallic;
        public Texture2D ao;
        public Texture2D curvature;
        public float processingTimeMs;
    }

    /// <summary>
    /// Pipeline controller for Compute Shader-based PBR map generation.
    /// </summary>
    public static class AutoMatMapGenerator
    {
        private static ComputeShader _luminanceCS;
        private static ComputeShader _blurCS;
        private static ComputeShader _normalCS;
        private static ComputeShader _roughnessCS;
        private static ComputeShader _metallicCS;
        private static ComputeShader _aoCS;
        private static ComputeShader _curvatureCS;

        /// <summary>
        /// Generate all PBR maps from a source color texture.
        /// </summary>
        public static AutoMatGenerationResult GenerateAll(Texture2D source, AutoMatGenerationParams p, int resolution)
        {
            if (source == null)
            {
                Debug.LogError("[AutoMat] Source texture is null.");
                return null;
            }

            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError("[AutoMat] Compute Shaders are not supported on this platform.");
                return null;
            }

            LoadShaders();

            var sw = Stopwatch.StartNew();
            var result = new AutoMatGenerationResult();

            // Create readable input
            RenderTexture inputRT = CreateRT(resolution);
            Graphics.Blit(source, inputRT);

            try
            {
                // 1. Luminance → Height
                RenderTexture heightRT = CreateRT(resolution);
                DispatchSimple(_luminanceCS, "CSMain", inputRT, heightRT, resolution);

                // Apply contrast
                if (Mathf.Abs(p.heightContrast - 1.0f) > 0.001f || p.heightInvert)
                {
                    // Simple contrast/invert via blit would be ideal, but for now just use as-is
                    // TODO: Add contrast compute shader if needed
                }

                // 2. Optional blur on height
                if (p.heightBlurRadius > 0)
                {
                    RenderTexture blurTemp = CreateRT(resolution);
                    DispatchBlur(heightRT, blurTemp, resolution, p.heightBlurRadius);
                    RenderTexture.ReleaseTemporary(heightRT);
                    heightRT = blurTemp;
                }

                // 3. Normal from height
                RenderTexture normalRT = CreateRT(resolution);
                if (p.normalBlurRadius > 0)
                {
                    RenderTexture blurredHeight = CreateRT(resolution);
                    DispatchBlur(heightRT, blurredHeight, resolution, p.normalBlurRadius);
                    DispatchNormal(blurredHeight, normalRT, resolution, p.normalStrength);
                    RenderTexture.ReleaseTemporary(blurredHeight);
                }
                else
                {
                    DispatchNormal(heightRT, normalRT, resolution, p.normalStrength);
                }

                // 4. Roughness
                RenderTexture roughnessRT = CreateRT(resolution);
                DispatchRoughness(inputRT, roughnessRT, resolution, p);

                // 5. Metallic
                RenderTexture metallicRT = CreateRT(resolution);
                DispatchMetallic(inputRT, metallicRT, resolution, p);

                // 6. AO
                RenderTexture aoRT = CreateRT(resolution);
                DispatchAO(heightRT, aoRT, resolution, p);

                // 7. Curvature (optional)
                RenderTexture curvatureRT = null;
                if (p.enableCurvature)
                {
                    curvatureRT = CreateRT(resolution);
                    DispatchCurvature(normalRT, curvatureRT, resolution, p.curvatureScale);
                }

                // Convert RT → Texture2D
                result.height = RTToTexture2D(heightRT, resolution);
                result.normal = RTToTexture2D(normalRT, resolution);
                result.roughness = RTToTexture2D(roughnessRT, resolution);
                result.metallic = RTToTexture2D(metallicRT, resolution);
                result.ao = RTToTexture2D(aoRT, resolution);
                if (curvatureRT != null)
                    result.curvature = RTToTexture2D(curvatureRT, resolution);

                // Release all RTs
                RenderTexture.ReleaseTemporary(heightRT);
                RenderTexture.ReleaseTemporary(normalRT);
                RenderTexture.ReleaseTemporary(roughnessRT);
                RenderTexture.ReleaseTemporary(metallicRT);
                RenderTexture.ReleaseTemporary(aoRT);
                if (curvatureRT != null)
                    RenderTexture.ReleaseTemporary(curvatureRT);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(inputRT);
            }

            sw.Stop();
            result.processingTimeMs = sw.ElapsedMilliseconds;
            return result;
        }

        /// <summary>
        /// Regenerate only the normal map (when normal params change).
        /// </summary>
        public static Texture2D RegenerateNormal(Texture2D heightMap, AutoMatGenerationParams p, int resolution)
        {
            LoadShaders();
            RenderTexture heightRT = CreateRT(resolution);
            Graphics.Blit(heightMap, heightRT);
            RenderTexture normalRT = CreateRT(resolution);

            try
            {
                if (p.normalBlurRadius > 0)
                {
                    RenderTexture blurred = CreateRT(resolution);
                    DispatchBlur(heightRT, blurred, resolution, p.normalBlurRadius);
                    DispatchNormal(blurred, normalRT, resolution, p.normalStrength);
                    RenderTexture.ReleaseTemporary(blurred);
                }
                else
                {
                    DispatchNormal(heightRT, normalRT, resolution, p.normalStrength);
                }
                return RTToTexture2D(normalRT, resolution);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(heightRT);
                RenderTexture.ReleaseTemporary(normalRT);
            }
        }

        // ===== Compute dispatch helpers =====

        private static void DispatchSimple(ComputeShader cs, string kernel, RenderTexture input, RenderTexture output, int res)
        {
            int k = cs.FindKernel(kernel);
            cs.SetInts("_TexSize", res, res);
            cs.SetTexture(k, "_Input", input);
            cs.SetTexture(k, "_Result", output);
            cs.Dispatch(k, Mathf.CeilToInt(res / 8f), Mathf.CeilToInt(res / 8f), 1);
        }

        private static void DispatchBlur(RenderTexture input, RenderTexture output, int res, int radius)
        {
            float sigma = Mathf.Max(radius / 3f, 0.5f);
            RenderTexture temp = CreateRT(res);

            // Horizontal pass
            int kH = _blurCS.FindKernel("BlurH");
            _blurCS.SetInts("_TexSize", res, res);
            _blurCS.SetInt("_BlurRadius", radius);
            _blurCS.SetFloat("_Sigma", sigma);
            _blurCS.SetTexture(kH, "_Input", input);
            _blurCS.SetTexture(kH, "_Result", temp);
            _blurCS.Dispatch(kH, Mathf.CeilToInt(res / 8f), Mathf.CeilToInt(res / 8f), 1);

            // Vertical pass
            int kV = _blurCS.FindKernel("BlurV");
            _blurCS.SetTexture(kV, "_Input", temp);
            _blurCS.SetTexture(kV, "_Result", output);
            _blurCS.Dispatch(kV, Mathf.CeilToInt(res / 8f), Mathf.CeilToInt(res / 8f), 1);

            RenderTexture.ReleaseTemporary(temp);
        }

        private static void DispatchNormal(RenderTexture heightRT, RenderTexture normalRT, int res, float strength)
        {
            int k = _normalCS.FindKernel("CSMain");
            _normalCS.SetInts("_TexSize", res, res);
            _normalCS.SetFloat("_NormalStrength", strength);
            _normalCS.SetTexture(k, "_Input", heightRT);
            _normalCS.SetTexture(k, "_Result", normalRT);
            _normalCS.Dispatch(k, Mathf.CeilToInt(res / 8f), Mathf.CeilToInt(res / 8f), 1);
        }

        private static void DispatchRoughness(RenderTexture inputRT, RenderTexture outputRT, int res, AutoMatGenerationParams p)
        {
            RenderTexture tempBuffer = CreateRT(res);

            try
            {
                // Pass 1: CalcVariance
                int k1 = _roughnessCS.FindKernel("CalcVariance");
                _roughnessCS.SetInts("_TexSize", res, res);
                _roughnessCS.SetInt("_BlockSize", p.roughnessBlockSize);
                _roughnessCS.SetTexture(k1, "_Input", inputRT);
                _roughnessCS.SetTexture(k1, "_TempBuffer", tempBuffer);
                _roughnessCS.Dispatch(k1, Mathf.CeilToInt(res / 8f), Mathf.CeilToInt(res / 8f), 1);

                // Pass 2: RemapRoughness
                int k2 = _roughnessCS.FindKernel("RemapRoughness");
                _roughnessCS.SetFloat("_RemapMin", p.roughnessRemapMin);
                _roughnessCS.SetFloat("_RemapMax", p.roughnessRemapMax);
                _roughnessCS.SetFloat("_SaturationWeight", p.roughnessSaturationWeight);
                _roughnessCS.SetTexture(k2, "_Input", inputRT);
                _roughnessCS.SetTexture(k2, "_TempBuffer", tempBuffer);
                _roughnessCS.SetTexture(k2, "_Result", outputRT);
                _roughnessCS.Dispatch(k2, Mathf.CeilToInt(res / 8f), Mathf.CeilToInt(res / 8f), 1);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(tempBuffer);
            }
        }

        private static void DispatchMetallic(RenderTexture inputRT, RenderTexture outputRT, int res, AutoMatGenerationParams p)
        {
            int k = _metallicCS.FindKernel("CSMain");
            _metallicCS.SetInts("_TexSize", res, res);
            _metallicCS.SetFloat("_LuminanceThreshold", p.metallicLuminanceThreshold);
            _metallicCS.SetFloat("_SaturationThreshold", p.metallicSaturationThreshold);
            _metallicCS.SetVector("_GoldHueRange", p.metallicGoldHueRange);
            _metallicCS.SetVector("_CopperHueRange", p.metallicCopperHueRange);
            _metallicCS.SetFloat("_Bias", p.metallicBias);
            _metallicCS.SetTexture(k, "_Input", inputRT);
            _metallicCS.SetTexture(k, "_Result", outputRT);
            _metallicCS.Dispatch(k, Mathf.CeilToInt(res / 8f), Mathf.CeilToInt(res / 8f), 1);
        }

        private static void DispatchAO(RenderTexture heightRT, RenderTexture aoRT, int res, AutoMatGenerationParams p)
        {
            int k = _aoCS.FindKernel("CSMain");
            _aoCS.SetInts("_TexSize", res, res);
            _aoCS.SetInt("_AORadius", p.aoRadius);
            _aoCS.SetFloat("_AOStrength", p.aoStrength);
            _aoCS.SetFloat("_AOBias", p.aoBias);
            _aoCS.SetTexture(k, "_Input", heightRT);
            _aoCS.SetTexture(k, "_Result", aoRT);
            _aoCS.Dispatch(k, Mathf.CeilToInt(res / 8f), Mathf.CeilToInt(res / 8f), 1);
        }

        private static void DispatchCurvature(RenderTexture normalRT, RenderTexture curvatureRT, int res, float scale)
        {
            int k = _curvatureCS.FindKernel("CSMain");
            _curvatureCS.SetInts("_TexSize", res, res);
            _curvatureCS.SetFloat("_CurvatureScale", scale);
            _curvatureCS.SetTexture(k, "_Input", normalRT);
            _curvatureCS.SetTexture(k, "_Result", curvatureRT);
            _curvatureCS.Dispatch(k, Mathf.CeilToInt(res / 8f), Mathf.CeilToInt(res / 8f), 1);
        }

        // ===== Utility =====

        private static RenderTexture CreateRT(int resolution)
        {
            var rt = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        private static Texture2D RTToTexture2D(RenderTexture rt, int resolution)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false, true);
            tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            tex.Apply(false, false);
            RenderTexture.active = prev;
            return tex;
        }

        private static void LoadShaders()
        {
            if (_luminanceCS == null)
                _luminanceCS = FindComputeShader("AutoMat_Luminance");
            if (_blurCS == null)
                _blurCS = FindComputeShader("AutoMat_GaussianBlur");
            if (_normalCS == null)
                _normalCS = FindComputeShader("AutoMat_NormalFromHeight");
            if (_roughnessCS == null)
                _roughnessCS = FindComputeShader("AutoMat_RoughnessEstimate");
            if (_metallicCS == null)
                _metallicCS = FindComputeShader("AutoMat_MetallicEstimate");
            if (_aoCS == null)
                _aoCS = FindComputeShader("AutoMat_AmbientOcclusion");
            if (_curvatureCS == null)
                _curvatureCS = FindComputeShader("AutoMat_Curvature");
        }

        private static ComputeShader FindComputeShader(string name)
        {
            string[] guids = AssetDatabase.FindAssets($"t:ComputeShader {name}");
            if (guids.Length == 0)
            {
                Debug.LogError($"[AutoMat] Compute Shader not found: {name}");
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
        }
    }
}
