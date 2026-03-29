using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// P/Invoke bridge to the native brush DLL (required).
    /// ネイティブブラシDLLへのP/Invokeブリッジ（必須）
    /// </summary>
    internal static class NativeBrushBridge
    {
        private const string DllName = "NataneBrushNative";

        // ===== DllImport declarations =====

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetNativeCapabilities();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ApplyBrushDab(
            IntPtr canvas, int w, int h,
            float cx, float cy, float radius, float hardness, float opacity,
            float r, float g, float b, float a, int mode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void BlendLayerRegion(
            IntPtr bottom, IntPtr top, IntPtr result,
            int w, int h,
            int regionX, int regionY, int regionW, int regionH,
            float opacity, int blendMode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void BlendPixelsFull(
            IntPtr bottom, IntPtr top, IntPtr result,
            int totalPixels, float opacity, int blendMode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GaussianBlurSeparable(
            IntPtr pixels, int w, int h, float sigma, int radius);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GenerateBrushMask(
            IntPtr mask, int diameter, float hardness);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SobelEdgeDetect(
            IntPtr pixels, int w, int h, float strength);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AdjustLevels(
            IntPtr pixels, int totalPixels,
            float inBlack, float inWhite, float gamma,
            float outBlack, float outWhite);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Desaturate(IntPtr pixels, int totalPixels);

        // ===== Initialization =====

        private static bool initialized;
        private static int cachedVersion;
        private static int cachedCapabilities;

        /// <summary>
        /// Initialize the native bridge. Called automatically on first use.
        /// ネイティブブリッジを初期化。初回使用時に自動呼び出し
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (initialized) return;
            try
            {
                cachedVersion = GetVersion();
                cachedCapabilities = GetNativeCapabilities();
                initialized = true;

                string simd = "";
                if ((cachedCapabilities & 1) != 0) simd += "SSE ";
                if ((cachedCapabilities & 2) != 0) simd += "AVX2 ";
                if ((cachedCapabilities & 4) != 0) simd += "NEON ";
                if (string.IsNullOrEmpty(simd)) simd = "Scalar ";

                Debug.Log($"[NataneBrushNative] v{cachedVersion} loaded ({simd.Trim()})");
            }
            catch (DllNotFoundException)
            {
                Debug.LogError("[NataneBrushNative] DLL not found! Texture Studio requires NataneBrushNative.dll in Plugins/x86_64/");
            }
            catch (EntryPointNotFoundException e)
            {
                Debug.LogError($"[NataneBrushNative] DLL version mismatch: {e.Message}");
            }
        }

        /// <summary>
        /// Native API version / ネイティブAPIバージョン
        /// </summary>
        public static int Version => cachedVersion;

        /// <summary>
        /// SIMD capability bitmask (1=SSE, 2=AVX2, 4=NEON)
        /// SIMD機能ビットマスク
        /// </summary>
        public static int Capabilities => cachedCapabilities;

        // ===== Helper =====

        /// <summary>
        /// Pin a Color[] array and return its pointer. Must Free the handle after use.
        /// Color[]配列をピン留めしてポインタを返す。使用後にhandleをFreeすること
        /// </summary>
        public static IntPtr PinArray(Color[] array, out GCHandle handle)
        {
            handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            return handle.AddrOfPinnedObject();
        }

        /// <summary>
        /// Pin a float[] array and return its pointer.
        /// float[]配列をピン留めしてポインタを返す
        /// </summary>
        public static IntPtr PinArray(float[] array, out GCHandle handle)
        {
            handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            return handle.AddrOfPinnedObject();
        }
    }
}
