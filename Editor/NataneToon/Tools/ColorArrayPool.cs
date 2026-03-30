using System.Collections.Generic;
using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// Object pool for Color[] arrays to reduce GC allocations in texture operations.
    /// テクスチャ操作でのGCアロケーション削減用Color[]配列オブジェクトプール
    /// </summary>
    internal static class ColorArrayPool
    {
        private static readonly Dictionary<int, Stack<Color[]>> pools = new Dictionary<int, Stack<Color[]>>();
        private static readonly Dictionary<int, Stack<float[]>> floatPools = new Dictionary<int, Stack<float[]>>();
        private const int MaxPooledPerSize = 4;

        /// <summary>
        /// Get a Color[] array of the specified length. May return a recycled array.
        /// 指定長のColor[]配列を取得。リサイクル配列を返す可能性あり。
        /// </summary>
        public static Color[] Get(int length)
        {
            if (length <= 0) return new Color[0];

            if (pools.TryGetValue(length, out var stack) && stack.Count > 0)
                return stack.Pop();

            return new Color[length];
        }

        /// <summary>
        /// Get a Color[] for a texture of the given dimensions.
        /// 指定サイズのテクスチャ用Color[]を取得。
        /// </summary>
        public static Color[] Get(int width, int height)
        {
            return Get(width * height);
        }

        /// <summary>
        /// Return a Color[] array to the pool for reuse.
        /// Color[]配列をプールに返却して再利用可能にする。
        /// </summary>
        public static void Release(Color[] array)
        {
            if (array == null || array.Length == 0) return;

            int key = array.Length;
            if (!pools.ContainsKey(key))
                pools[key] = new Stack<Color[]>();

            if (pools[key].Count < MaxPooledPerSize)
            {
                pools[key].Push(array);
            }
        }

        /// <summary>
        /// Get a float[] array of the specified length. May return a recycled array.
        /// 指定長のfloat[]配列を取得。リサイクル配列を返す可能性あり。
        /// </summary>
        public static float[] GetFloat(int length)
        {
            if (length <= 0) return new float[0];

            if (floatPools.TryGetValue(length, out var stack) && stack.Count > 0)
                return stack.Pop();

            return new float[length];
        }

        /// <summary>
        /// Return a float[] array to the pool for reuse.
        /// float[]配列をプールに返却して再利用可能にする。
        /// </summary>
        public static void ReleaseFloat(float[] array)
        {
            if (array == null || array.Length == 0) return;

            int key = array.Length;
            if (!floatPools.ContainsKey(key))
                floatPools[key] = new Stack<float[]>();

            if (floatPools[key].Count < MaxPooledPerSize)
            {
                floatPools[key].Push(array);
            }
        }

        /// <summary>
        /// Clear all pooled arrays (call on domain reload).
        /// 全プール配列をクリア（ドメインリロード時に呼ぶ）。
        /// </summary>
        public static void Clear()
        {
            pools.Clear();
            floatPools.Clear();
        }

        /// <summary>Current pool statistics for debugging.</summary>
        public static int PooledCount
        {
            get
            {
                int count = 0;
                foreach (var stack in pools.Values)
                    count += stack.Count;
                return count;
            }
        }
    }
}
