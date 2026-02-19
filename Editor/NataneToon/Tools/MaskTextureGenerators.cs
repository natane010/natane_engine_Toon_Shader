using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    // ================================================================
    // Gradient Types and Parameters
    // ================================================================

    internal enum GradientType { Linear, Radial, Angular, HeightBased }

    internal class GradientParams
    {
        public float angle;
        public Vector2 center = new Vector2(0.5f, 0.5f);
        public float radius = 0.5f;
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        public bool invert;
    }

    // ================================================================
    // Mesh Info Types
    // ================================================================

    internal enum MeshInfoType { Curvature, NormalDirection, VertexColor }

    // ================================================================
    // GradientGenerator
    // ================================================================

    internal static class GradientGenerator
    {
        /// <summary>
        /// Generate gradient into pixels array based on UV-space coordinates
        /// UV座標ベースでピクセル配列にグラデーションを生成
        /// </summary>
        public static void Generate(Color[] pixels, int size, GradientType type, GradientParams param)
        {
            if (type == GradientType.HeightBased) return; // Requires mesh data, use GenerateHeightBased

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float v = (float)y / (size - 1);
                    float value = 0f;

                    switch (type)
                    {
                        case GradientType.Linear:
                            float rad = param.angle * Mathf.Deg2Rad;
                            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                            value = Vector2.Dot(new Vector2(u, v) - param.center + dir * 0.5f, dir);
                            value = Mathf.Clamp01(value);
                            break;

                        case GradientType.Radial:
                            float dist = Vector2.Distance(new Vector2(u, v), param.center);
                            value = Mathf.Clamp01(1f - dist / Mathf.Max(param.radius, 0.001f));
                            break;

                        case GradientType.Angular:
                            float dx = u - param.center.x;
                            float dy = v - param.center.y;
                            value = (Mathf.Atan2(dy, dx) + Mathf.PI) / (2f * Mathf.PI);
                            value = (value + param.angle / 360f) % 1f;
                            break;
                    }

                    if (param.curve != null && param.curve.length > 0)
                        value = param.curve.Evaluate(value);

                    if (param.invert) value = 1f - value;
                    value = Mathf.Clamp01(value);
                    pixels[y * size + x] = new Color(value, value, value);
                }
            }
        }

        /// <summary>
        /// Height-based gradient: rasterizes vertex Y coordinate to UV space
        /// 高さベースグラデーション: 頂点のY座標をUV空間にラスタライズ
        /// </summary>
        public static void GenerateHeightBased(Color[] pixels, int size, Mesh mesh, List<Vector2> uvs,
            AnimationCurve curve, bool invert)
        {
            var vertices = mesh.vertices;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (i < uvs.Count)
                {
                    minY = Mathf.Min(minY, vertices[i].y);
                    maxY = Mathf.Max(maxY, vertices[i].y);
                }
            }

            float rangeY = maxY - minY;
            if (rangeY < 0.0001f) rangeY = 1f;

            float capturedMinY = minY;
            float capturedRangeY = rangeY;

            MeshDataRasterizer.RasterizeWithVertexData(pixels, size, mesh, uvs,
                vertex => (vertex.y - capturedMinY) / capturedRangeY, curve, invert);
        }
    }

    // ================================================================
    // MeshInfoGenerator
    // ================================================================

    internal static class MeshInfoGenerator
    {
        /// <summary>
        /// Generate curvature map: average angular difference between vertex normal and neighbors
        /// 曲率マップ生成: 頂点法線と隣接頂点法線の平均角度差
        /// </summary>
        public static void GenerateCurvature(Color[] pixels, int size, Mesh mesh, List<Vector2> uvs, float sensitivity)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            int[] triangles = mesh.triangles;
            int vertexCount = vertices.Length;

            if (normals == null || normals.Length == 0)
            {
                mesh.RecalculateNormals();
                normals = mesh.normals;
            }

            // Step 1: Build adjacency graph from triangle indices
            var adjacency = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < vertexCount; i++)
                adjacency[i] = new HashSet<int>();

            int triCount = triangles.Length / 3;
            for (int t = 0; t < triCount; t++)
            {
                int i0 = triangles[t * 3 + 0];
                int i1 = triangles[t * 3 + 1];
                int i2 = triangles[t * 3 + 2];

                adjacency[i0].Add(i1);
                adjacency[i0].Add(i2);
                adjacency[i1].Add(i0);
                adjacency[i1].Add(i2);
                adjacency[i2].Add(i0);
                adjacency[i2].Add(i1);
            }

            // Step 2: Compute per-vertex curvature
            float[] curvatureValues = new float[vertexCount];
            float maxCurvature = 0f;

            for (int i = 0; i < vertexCount; i++)
            {
                if (adjacency[i].Count == 0)
                {
                    curvatureValues[i] = 0f;
                    continue;
                }

                float sum = 0f;
                Vector3 ni = normals[i].normalized;

                foreach (int neighbor in adjacency[i])
                {
                    if (neighbor < normals.Length)
                    {
                        Vector3 nn = normals[neighbor].normalized;
                        sum += 1f - Vector3.Dot(ni, nn);
                    }
                }

                curvatureValues[i] = sum / adjacency[i].Count;
                maxCurvature = Mathf.Max(maxCurvature, curvatureValues[i]);
            }

            // Step 3: Normalize curvature values to [0,1]
            if (maxCurvature > 0.0001f)
            {
                float invMax = sensitivity / maxCurvature;
                for (int i = 0; i < vertexCount; i++)
                {
                    curvatureValues[i] = Mathf.Clamp01(curvatureValues[i] * invMax);
                }
            }

            // Step 4: Rasterize to UV space
            MeshDataRasterizer.RasterizeWithVertexValues(pixels, size, mesh, uvs, curvatureValues);
        }

        /// <summary>
        /// Generate normal direction map: dot(normal, targetDir) mapped to [0,1]
        /// 法線方向マップ生成: dot(法線, 目標方向) を [0,1] にマッピング
        /// </summary>
        public static void GenerateNormalDirection(Color[] pixels, int size, Mesh mesh, List<Vector2> uvs,
            Vector3 targetDirection, float threshold)
        {
            var normals = mesh.normals;

            if (normals == null || normals.Length == 0)
            {
                mesh.RecalculateNormals();
                normals = mesh.normals;
            }

            Vector3 dir = targetDirection.normalized;

            float[] values = new float[normals.Length];
            for (int i = 0; i < normals.Length; i++)
            {
                float dot = Vector3.Dot(normals[i].normalized, dir);
                // Map from [-1,1] to [0,1] and apply threshold
                float mapped = (dot + 1f) * 0.5f;
                values[i] = mapped >= threshold ? mapped : 0f;
            }

            MeshDataRasterizer.RasterizeWithVertexValues(pixels, size, mesh, uvs, values);
        }

        /// <summary>
        /// Generate vertex color channel map
        /// 頂点カラーチャンネルマップ生成
        /// </summary>
        public static void GenerateVertexColor(Color[] pixels, int size, Mesh mesh, List<Vector2> uvs, int colorChannel)
        {
            var colors = mesh.colors;

            if (colors == null || colors.Length == 0)
            {
                // Fill with white if no vertex colors
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = Color.white;
                return;
            }

            float[] values = new float[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                switch (colorChannel)
                {
                    case 0: values[i] = colors[i].r; break;
                    case 1: values[i] = colors[i].g; break;
                    case 2: values[i] = colors[i].b; break;
                    case 3: values[i] = colors[i].a; break;
                    default: values[i] = colors[i].r; break;
                }
            }

            MeshDataRasterizer.RasterizeWithVertexValues(pixels, size, mesh, uvs, values);
        }
    }

    // ================================================================
    // MeshDataRasterizer - Triangle rasterizer with vertex data interpolation
    // メッシュデータラスタライザー - 頂点データ補間付き三角形ラスタライザー
    // ================================================================

    internal static class MeshDataRasterizer
    {
        /// <summary>
        /// Rasterize triangles with per-vertex scalar data interpolated via barycentric coordinates.
        /// Uses a callback function to extract scalar value from vertex position.
        /// 重心座標で頂点スカラーデータを補間しながら三角形をラスタライズ
        /// </summary>
        public static void RasterizeWithVertexData(Color[] pixels, int size, Mesh mesh, List<Vector2> uvs,
            System.Func<Vector3, float> vertexToValue, AnimationCurve curve = null, bool invert = false)
        {
            var vertices = mesh.vertices;
            float[] values = new float[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                values[i] = vertexToValue(vertices[i]);
            }

            RasterizeWithVertexValues(pixels, size, mesh, uvs, values, curve, invert);
        }

        /// <summary>
        /// Rasterize triangles with pre-computed per-vertex scalar values.
        /// 事前計算済みの頂点スカラー値で三角形をラスタライズ
        /// </summary>
        public static void RasterizeWithVertexValues(Color[] pixels, int size, Mesh mesh, List<Vector2> uvs,
            float[] vertexValues, AnimationCurve curve = null, bool invert = false)
        {
            int[] triangles = mesh.triangles;
            int triCount = triangles.Length / 3;

            for (int t = 0; t < triCount; t++)
            {
                int i0 = triangles[t * 3 + 0];
                int i1 = triangles[t * 3 + 1];
                int i2 = triangles[t * 3 + 2];

                // Skip triangles with out-of-range indices
                if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;
                if (i0 >= vertexValues.Length || i1 >= vertexValues.Length || i2 >= vertexValues.Length) continue;

                Vector2 uv0 = uvs[i0];
                Vector2 uv1 = uvs[i1];
                Vector2 uv2 = uvs[i2];

                float val0 = vertexValues[i0];
                float val1 = vertexValues[i1];
                float val2 = vertexValues[i2];

                RasterizeTriangleWithValues(pixels, size, uv0, uv1, uv2, val0, val1, val2, curve, invert);
            }
        }

        /// <summary>
        /// Rasterize a single triangle with barycentric interpolation of scalar values.
        /// 重心座標補間によるスカラー値付き三角形ラスタライズ
        /// </summary>
        private static void RasterizeTriangleWithValues(Color[] pixels, int size,
            Vector2 v0, Vector2 v1, Vector2 v2,
            float val0, float val1, float val2,
            AnimationCurve curve, bool invert)
        {
            // Convert UV to pixel coordinates
            float px0 = v0.x * (size - 1);
            float py0 = v0.y * (size - 1);
            float px1 = v1.x * (size - 1);
            float py1 = v1.y * (size - 1);
            float px2 = v2.x * (size - 1);
            float py2 = v2.y * (size - 1);

            // Bounding box
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(px0, Mathf.Min(px1, px2))), 0, size - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(px0, Mathf.Max(px1, px2))), 0, size - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(py0, Mathf.Min(py1, py2))), 0, size - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(py0, Mathf.Max(py1, py2))), 0, size - 1);

            // Precompute denominator for barycentric coordinates
            float denom = (py1 - py2) * (px0 - px2) + (px2 - px1) * (py0 - py2);
            if (Mathf.Abs(denom) < 1e-8f) return; // Degenerate triangle

            float invDenom = 1f / denom;

            bool hasCurve = curve != null && curve.length > 0;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float w0 = ((py1 - py2) * (px - px2) + (px2 - px1) * (py - py2)) * invDenom;
                    float w1 = ((py2 - py0) * (px - px2) + (px0 - px2) * (py - py2)) * invDenom;
                    float w2 = 1f - w0 - w1;

                    if (w0 >= 0f && w1 >= 0f && w2 >= 0f)
                    {
                        float interpolated = w0 * val0 + w1 * val1 + w2 * val2;

                        if (hasCurve)
                            interpolated = curve.Evaluate(interpolated);

                        if (invert)
                            interpolated = 1f - interpolated;

                        interpolated = Mathf.Clamp01(interpolated);
                        pixels[y * size + x] = new Color(interpolated, interpolated, interpolated);
                    }
                }
            }
        }
    }
}
