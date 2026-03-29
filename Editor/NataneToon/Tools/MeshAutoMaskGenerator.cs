using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Generates mask textures automatically from mesh data.
    /// メッシュデータからマスクテクスチャを自動生成
    /// </summary>
    internal static class MeshAutoMaskGenerator
    {
        internal enum MaskType
        {
            Curvature,      // 曲率 (凸=白, 凹=黒)
            AmbientOcclusion, // 簡易AO
            Cavity,          // キャビティ (凹部分のみ)
            NormalFacing,    // 法線方向 (上向き=白)
            VertexColor,     // 頂点カラー
            PositionGradient // 位置グラデーション (Y軸)
        }

        /// <summary>
        /// Generate a mask texture from mesh data.
        /// メッシュデータからマスクテクスチャを生成
        /// </summary>
        public static Color[] Generate(Mesh mesh, List<Vector2> uvs, int width, int height,
            MaskType maskType, float sensitivity = 1f, Vector3 direction = default)
        {
            if (mesh == null || uvs == null || uvs.Count == 0)
                return null;

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.black;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            if (vertices == null || normals == null || triangles == null)
                return pixels;

            // Calculate per-vertex values
            float[] vertexValues = new float[vertices.Length];

            switch (maskType)
            {
                case MaskType.Curvature:
                    vertexValues = CalculateCurvature(mesh, sensitivity);
                    break;
                case MaskType.AmbientOcclusion:
                    vertexValues = CalculateSimpleAO(mesh, sensitivity);
                    break;
                case MaskType.Cavity:
                    vertexValues = CalculateCavity(mesh, sensitivity);
                    break;
                case MaskType.NormalFacing:
                    if (direction == default) direction = Vector3.up;
                    for (int i = 0; i < vertices.Length && i < normals.Length; i++)
                        vertexValues[i] = Mathf.Clamp01(Vector3.Dot(normals[i], direction.normalized) * sensitivity);
                    break;
                case MaskType.PositionGradient:
                    if (direction == default) direction = Vector3.up;
                    Bounds bounds = mesh.bounds;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        float projected = Vector3.Dot(vertices[i] - bounds.min, direction.normalized);
                        float range = Vector3.Dot(bounds.size, direction.normalized);
                        vertexValues[i] = Mathf.Clamp01(projected / Mathf.Max(range, 0.001f));
                    }
                    break;
                case MaskType.VertexColor:
                    Color[] colors = mesh.colors;
                    if (colors != null && colors.Length == vertices.Length)
                    {
                        for (int i = 0; i < vertices.Length; i++)
                            vertexValues[i] = (colors[i].r + colors[i].g + colors[i].b) / 3f;
                    }
                    break;
            }

            // Rasterize triangles to texture using UV coordinates
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                int i0 = triangles[t], i1 = triangles[t + 1], i2 = triangles[t + 2];
                if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                Vector2 uv0 = uvs[i0], uv1 = uvs[i1], uv2 = uvs[i2];
                float v0 = i0 < vertexValues.Length ? vertexValues[i0] : 0;
                float v1 = i1 < vertexValues.Length ? vertexValues[i1] : 0;
                float v2 = i2 < vertexValues.Length ? vertexValues[i2] : 0;

                RasterizeTriangle(pixels, width, height, uv0, uv1, uv2, v0, v1, v2);
            }

            return pixels;
        }

        private static float[] CalculateCurvature(Mesh mesh, float sensitivity)
        {
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] tris = mesh.triangles;
            float[] curvature = new float[verts.Length];
            int[] counts = new int[verts.Length];

            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
                float c01 = 1f - Vector3.Dot(normals[i0], normals[i1]);
                float c12 = 1f - Vector3.Dot(normals[i1], normals[i2]);
                float c20 = 1f - Vector3.Dot(normals[i2], normals[i0]);
                float avg = (c01 + c12 + c20) / 3f;
                curvature[i0] += avg; curvature[i1] += avg; curvature[i2] += avg;
                counts[i0]++; counts[i1]++; counts[i2]++;
            }

            for (int i = 0; i < curvature.Length; i++)
            {
                if (counts[i] > 0) curvature[i] /= counts[i];
                curvature[i] = Mathf.Clamp01(curvature[i] * sensitivity * 5f);
            }
            return curvature;
        }

        private static float[] CalculateSimpleAO(Mesh mesh, float sensitivity)
        {
            // Simple AO based on vertex accessibility (concavity estimation)
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            float[] ao = new float[verts.Length];

            for (int i = 0; i < verts.Length; i++)
            {
                float occlusion = 0f;
                int samples = 0;
                for (int j = 0; j < verts.Length; j += Mathf.Max(1, verts.Length / 100))
                {
                    if (i == j) continue;
                    Vector3 dir = (verts[j] - verts[i]);
                    float dist = dir.magnitude;
                    if (dist > 0.001f && dist < sensitivity)
                    {
                        float dot = Vector3.Dot(normals[i], dir.normalized);
                        if (dot > 0) occlusion += dot / (dist * dist + 0.1f);
                        samples++;
                    }
                }
                ao[i] = samples > 0 ? 1f - Mathf.Clamp01(occlusion / samples * 0.5f) : 1f;
            }
            return ao;
        }

        private static float[] CalculateCavity(Mesh mesh, float sensitivity)
        {
            float[] curvature = CalculateCurvature(mesh, sensitivity);
            // Cavity = only concave parts (curvature above threshold)
            for (int i = 0; i < curvature.Length; i++)
                curvature[i] = Mathf.Clamp01(curvature[i] * 2f - 0.5f);
            return curvature;
        }

        private static void RasterizeTriangle(Color[] pixels, int w, int h,
            Vector2 uv0, Vector2 uv1, Vector2 uv2, float v0, float v1, float v2)
        {
            // Simple scanline rasterization
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(uv0.x, Mathf.Min(uv1.x, uv2.x)) * w), 0, w - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(uv0.x, Mathf.Max(uv1.x, uv2.x)) * w), 0, w - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(uv0.y, Mathf.Min(uv1.y, uv2.y)) * h), 0, h - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(uv0.y, Mathf.Max(uv1.y, uv2.y)) * h), 0, h - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / w, (y + 0.5f) / h);
                    Vector3 bary = Barycentric(p, uv0, uv1, uv2);
                    if (bary.x >= -0.01f && bary.y >= -0.01f && bary.z >= -0.01f)
                    {
                        float val = bary.x * v0 + bary.y * v1 + bary.z * v2;
                        pixels[y * w + x] = new Color(val, val, val, 1f);
                    }
                }
            }
        }

        private static Vector3 Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 v0 = c - a, v1 = b - a, v2 = p - a;
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d02 = Vector2.Dot(v0, v2);
            float d11 = Vector2.Dot(v1, v1);
            float d12 = Vector2.Dot(v1, v2);
            float inv = 1f / (d00 * d11 - d01 * d01 + 0.0001f);
            float u = (d11 * d02 - d01 * d12) * inv;
            float v = (d00 * d12 - d01 * d02) * inv;
            return new Vector3(1f - u - v, v, u);
        }

        /// <summary>
        /// Draw the mask generator settings UI.
        /// マスクジェネレータ設定UIを描画
        /// </summary>
        public static void DrawSettingsUI(ref MaskType maskType, ref float sensitivity, ref Vector3 direction)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(L("メッシュ自動マスク", "Mesh Auto Mask"), EditorStyles.boldLabel);
                maskType = (MaskType)EditorGUILayout.EnumPopup(L("タイプ", "Type"), maskType);
                sensitivity = EditorGUILayout.Slider(L("感度", "Sensitivity"), sensitivity, 0.1f, 5f);
                if (maskType == MaskType.NormalFacing || maskType == MaskType.PositionGradient)
                    direction = EditorGUILayout.Vector3Field(L("方向", "Direction"), direction);
            }
        }
    }
}
