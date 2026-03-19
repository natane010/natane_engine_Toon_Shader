using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Output format for generated texture maps.
/// </summary>
public enum OutputFormat
{
    PNG,
    TGA,
    EXR
}

/// <summary>
/// Available sources for Control Map channel packing.
/// </summary>
public enum ControlMapChannel
{
    None,
    AO,
    Curvature,
    Roughness,
    Smoothness,
    Shadow,
    NormalR,
    NormalG
}

/// <summary>
/// Settings for all map generation operations.
/// </summary>
[Serializable]
public class MapGenSettings
{
    // Common
    public Vector2Int outputResolution = new Vector2Int(2048, 2048);
    public string outputFolder = "Assets/GeneratedMaps";
    public OutputFormat outputFormat = OutputFormat.PNG;
    public bool autoAssignToMaterial = true;
    public int dilationPixels = 4;

    // Normal Map
    public bool generateNormal = true;
    public float normalStrength = 1.0f;
    public int normalBlurRadius = 1;

    // AO Map
    public bool generateAO = true;
    public int aoRayCount = 64;
    public float aoMaxDistance = 1.0f;
    public float aoIntensity = 1.0f;
    public int aoDilation = 4;

    // Curvature Map
    public bool generateCurvature = true;
    public float curvatureMultiplier = 1.0f;
    public int curvatureDilation = 4;

    // Roughness/Smoothness Map
    public bool generateRoughness = true;
    public float roughnessBaseline = 0.5f;
    public float luminanceInfluence = 0.3f;
    public float saturationInfluence = 0.2f;
    public bool invertToSmoothness = true;
    public int roughnessBlur = 1;

    // Shadow Map
    public bool generateShadow = true;
    public Vector3 shadowLightDir = new Vector3(0, -1, 0.5f).normalized;
    public int shadowRayCount = 32;
    public float shadowSpreadAngle = 5.0f;
    public float shadowIntensity = 1.0f;
    public int shadowDilation = 4;

    // Control Map
    public bool generateControl = true;
    public ControlMapChannel channelR = ControlMapChannel.AO;
    public ControlMapChannel channelG = ControlMapChannel.Curvature;
    public ControlMapChannel channelB = ControlMapChannel.Shadow;
    public ControlMapChannel channelA = ControlMapChannel.Smoothness;
}

/// <summary>
/// Result container for generated maps.
/// </summary>
public class MapGenResult
{
    public Texture2D normal;
    public Texture2D ao;
    public Texture2D curvature;
    public Texture2D roughness;
    public Texture2D shadow;
    public Texture2D controlMap;
    public float processingTimeMs;
}

/// <summary>
/// MonoBehaviour component for generating texture maps from mesh and albedo.
/// Attach to any GameObject with a Renderer to auto-detect mesh and albedo.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class MapGenerator : MonoBehaviour
{
    [HideInInspector] public Renderer targetRenderer;
    [HideInInspector] public Mesh targetMesh;
    public Texture2D albedoTexture;

    public MapGenSettings settings = new MapGenSettings();

    // Last generation results (for preview)
    [HideInInspector] public Texture2D lastNormalMap;
    [HideInInspector] public Texture2D lastAOMap;
    [HideInInspector] public Texture2D lastCurvatureMap;
    [HideInInspector] public Texture2D lastRoughnessMap;
    [HideInInspector] public Texture2D lastShadowMap;
    [HideInInspector] public Texture2D lastControlMap;

    private void OnValidate()
    {
        AutoDetect();
    }

    private void Awake()
    {
        AutoDetect();
    }

    private void AutoDetect()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            targetMesh = MapGenUtils.GetMesh(targetRenderer);

            if (albedoTexture == null && targetRenderer.sharedMaterial != null)
            {
                if (targetRenderer.sharedMaterial.HasProperty("_MainTex"))
                    albedoTexture = targetRenderer.sharedMaterial.mainTexture as Texture2D;
            }
        }
    }
}

/// <summary>
/// Static utility methods for map generation (no Editor dependency).
/// </summary>
public static class MapGenUtils
{
    /// <summary>
    /// Create a readable copy of a texture via RenderTexture blit.
    /// Uses Linear color space to prevent sRGB double-conversion.
    /// </summary>
    public static Texture2D MakeReadable(Texture2D source, int width, int height)
    {
        if (source == null) return null;

        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        try
        {
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D readable = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readable.Apply(false, false);
            RenderTexture.active = prev;
            return readable;
        }
        finally
        {
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    /// <summary>
    /// CPU triangle rasterizer: bake per-vertex scalar data to UV space.
    /// </summary>
    public static Color[] RasterizeToUV(Mesh mesh, float[] vertexValues, int w, int h)
    {
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;
        Color[] pixels = new Color[w * h];

        for (int t = 0; t < triangles.Length; t += 3)
        {
            int i0 = triangles[t], i1 = triangles[t + 1], i2 = triangles[t + 2];
            RasterizeTriangleScalar(pixels, w, h,
                uvs[i0], uvs[i1], uvs[i2],
                vertexValues[i0], vertexValues[i1], vertexValues[i2]);
        }

        return pixels;
    }

    /// <summary>
    /// Fill empty (alpha=0) pixels by expanding from neighbors.
    /// </summary>
    public static void Dilate(Color[] pixels, int w, int h, int iterations)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            Color[] copy = (Color[])pixels.Clone();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (copy[idx].a > 0.5f) continue;

                    float sumR = 0, sumG = 0, sumB = 0;
                    int count = 0;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                            Color n = copy[ny * w + nx];
                            if (n.a > 0.5f)
                            {
                                sumR += n.r; sumG += n.g; sumB += n.b;
                                count++;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        pixels[idx] = new Color(sumR / count, sumG / count, sumB / count, 1.0f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// CPU separable Gaussian blur.
    /// </summary>
    public static void GaussianBlur(Color[] pixels, int w, int h, int radius)
    {
        if (radius <= 0) return;

        float sigma = Mathf.Max(radius / 3f, 0.5f);
        float[] weights = new float[radius * 2 + 1];
        float weightSum = 0;
        for (int i = -radius; i <= radius; i++)
        {
            float w2 = Mathf.Exp(-0.5f * i * i / (sigma * sigma));
            weights[i + radius] = w2;
            weightSum += w2;
        }
        for (int i = 0; i < weights.Length; i++) weights[i] /= weightSum;

        // Horizontal pass
        Color[] temp = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color sum = Color.clear;
                for (int i = -radius; i <= radius; i++)
                {
                    int sx = Mathf.Clamp(x + i, 0, w - 1);
                    sum += pixels[y * w + sx] * weights[i + radius];
                }
                temp[y * w + x] = sum;
            }
        }

        // Vertical pass
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color sum = Color.clear;
                for (int i = -radius; i <= radius; i++)
                {
                    int sy = Mathf.Clamp(y + i, 0, h - 1);
                    sum += temp[sy * w + x] * weights[i + radius];
                }
                pixels[y * w + x] = sum;
            }
        }
    }

    /// <summary>
    /// Create a temporary MeshCollider on layer 31 for isolated raycasting.
    /// </summary>
    public static GameObject CreateTempMeshCollider(Mesh mesh, Transform source, out MeshCollider collider)
    {
        var go = new GameObject("_MapGenTempCollider") { hideFlags = HideFlags.HideAndDontSave };
        go.layer = 31;
        go.transform.position = source.position;
        go.transform.rotation = source.rotation;
        go.transform.localScale = source.lossyScale;
        collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        return go;
    }

    /// <summary>
    /// Get mesh from Renderer (supports MeshRenderer and SkinnedMeshRenderer).
    /// </summary>
    public static Mesh GetMesh(Renderer renderer)
    {
        if (renderer == null) return null;

        if (renderer is SkinnedMeshRenderer smr)
        {
            return smr.sharedMesh;
        }

        if (renderer is MeshRenderer)
        {
            var mf = renderer.GetComponent<MeshFilter>();
            return mf != null ? mf.sharedMesh : null;
        }

        return null;
    }

    /// <summary>
    /// Get a baked mesh snapshot from SkinnedMeshRenderer (for posed state).
    /// Falls back to sharedMesh for MeshRenderer.
    /// </summary>
    public static Mesh GetBakedMesh(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer smr)
        {
            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);
            return bakedMesh;
        }

        return GetMesh(renderer);
    }

    /// <summary>
    /// Fibonacci hemisphere sampling for uniform ray distribution.
    /// </summary>
    public static Vector3 GetHemisphereDirection(Vector3 normal, int index, int total)
    {
        float goldenRatio = (1.0f + Mathf.Sqrt(5.0f)) * 0.5f;
        float theta = 2.0f * Mathf.PI * index / goldenRatio;
        float cosPhiSq = 1.0f - (float)(index + 0.5f) / total;
        float cosPhi = Mathf.Sqrt(cosPhiSq);
        float sinPhi = Mathf.Sqrt(1.0f - cosPhiSq);

        Vector3 localDir = new Vector3(
            sinPhi * Mathf.Cos(theta),
            cosPhi,
            sinPhi * Mathf.Sin(theta));

        return RotateToNormal(localDir, normal);
    }

    /// <summary>
    /// Build adjacency map from triangle indices: for each vertex, collect neighbor vertex indices.
    /// </summary>
    public static Dictionary<int, HashSet<int>> BuildAdjacency(int[] triangles, int vertexCount)
    {
        var adjacency = new Dictionary<int, HashSet<int>>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            AddEdge(adjacency, a, b);
            AddEdge(adjacency, a, c);
            AddEdge(adjacency, b, c);
        }
        return adjacency;
    }

    /// <summary>
    /// Compute per-vertex curvature from neighbor normal differences.
    /// Returns values centered at 0.5 (flat), >0.5 = convex, <0.5 = concave.
    /// </summary>
    public static float[] ComputeVertexCurvature(Mesh mesh)
    {
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        var adjacency = BuildAdjacency(triangles, normals.Length);

        float[] curvature = new float[normals.Length];
        for (int v = 0; v < normals.Length; v++)
        {
            if (!adjacency.TryGetValue(v, out HashSet<int> neighbors) || neighbors.Count == 0)
            {
                curvature[v] = 0.5f;
                continue;
            }

            float sum = 0f;
            foreach (int n in neighbors)
            {
                sum += 1.0f - Vector3.Dot(normals[v], normals[n]);
            }
            float mean = sum / neighbors.Count;
            curvature[v] = Mathf.Clamp01(0.5f + mean * 0.5f);
        }

        return curvature;
    }

    /// <summary>
    /// Compute per-vertex concavity using position-based and normal-based analysis.
    /// Returns values from 0 (flat) to 1 (deeply concave).
    /// Useful for detecting concave geometry like anime eye sockets.
    /// </summary>
    public static float[] ComputeVertexConcavity(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;
        int vertexCount = vertices.Length;
        float[] concavity = new float[vertexCount];

        var adjacency = BuildAdjacency(triangles, vertexCount);

        for (int i = 0; i < vertexCount; i++)
        {
            if (!adjacency.TryGetValue(i, out HashSet<int> neighbors) || neighbors.Count == 0)
                continue;

            float positionConcavity = 0f;
            float normalConcavity = 0f;
            Vector3 normal = normals[i];

            foreach (int neighbor in neighbors)
            {
                // Position-based: neighbor above surface = concave
                Vector3 toNeighbor = vertices[neighbor] - vertices[i];
                float dot = Vector3.Dot(toNeighbor.normalized, normal);
                positionConcavity += Mathf.Max(0f, dot);

                // Normal-based: diverging normals = concave
                float normalDot = Vector3.Dot(normals[i], normals[neighbor]);
                normalConcavity += Mathf.Max(0f, 1f - normalDot);
            }

            positionConcavity /= neighbors.Count;
            normalConcavity /= neighbors.Count;

            concavity[i] = Mathf.Clamp01(positionConcavity * 0.6f + normalConcavity * 0.4f);
        }

        return concavity;
    }

    // ===== Private Helpers =====

    private static Vector3 RotateToNormal(Vector3 localDir, Vector3 normal)
    {
        Vector3 up = Mathf.Abs(normal.y) < 0.999f ? Vector3.up : Vector3.right;
        Vector3 tangent = Vector3.Cross(up, normal).normalized;
        Vector3 bitangent = Vector3.Cross(normal, tangent);
        return tangent * localDir.x + normal * localDir.y + bitangent * localDir.z;
    }

    private static void AddEdge(Dictionary<int, HashSet<int>> adjacency, int a, int b)
    {
        if (!adjacency.TryGetValue(a, out HashSet<int> setA))
        {
            setA = new HashSet<int>();
            adjacency[a] = setA;
        }
        setA.Add(b);

        if (!adjacency.TryGetValue(b, out HashSet<int> setB))
        {
            setB = new HashSet<int>();
            adjacency[b] = setB;
        }
        setB.Add(a);
    }

    private static void RasterizeTriangleScalar(Color[] pixels, int w, int h,
        Vector2 uv0, Vector2 uv1, Vector2 uv2,
        float d0, float d1, float d2)
    {
        Vector2 p0 = new Vector2(uv0.x * w, uv0.y * h);
        Vector2 p1 = new Vector2(uv1.x * w, uv1.y * h);
        Vector2 p2 = new Vector2(uv2.x * w, uv2.y * h);

        int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x))), 0, w - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x))), 0, w - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y))), 0, h - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y))), 0, h - 1);

        float area = Cross2D(p1 - p0, p2 - p0);
        if (Mathf.Abs(area) < 0.001f) return;
        float invArea = 1.0f / area;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = Cross2D(p1 - p0, p - p0) * invArea;
                float w1 = Cross2D(p2 - p1, p - p1) * invArea;
                float w2 = 1.0f - w0 - w1;

                if (w0 >= -0.001f && w1 >= -0.001f && w2 >= -0.001f)
                {
                    float val = d0 * w2 + d1 * w0 + d2 * w1;
                    pixels[y * w + x] = new Color(val, val, val, 1.0f);
                }
            }
        }
    }

    private static float Cross2D(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
