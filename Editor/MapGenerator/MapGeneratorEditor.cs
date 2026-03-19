using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// =============================================================================
// MapGenL — Lightweight localization helper for MapGenerator (JP/EN)
// Follows the same L("日本語", "English") pattern as NataneToonLocalization
// =============================================================================

public static class MapGenL
{
    private const string LANG_PREFS_KEY = "MapGen_Language";
    private static int _lang = -1; // -1 = uninitialized

    public static bool IsJapanese
    {
        get
        {
            if (_lang < 0) _lang = EditorPrefs.GetInt(LANG_PREFS_KEY, -1);
            if (_lang < 0)
            {
                // Auto-detect from system locale
                _lang = (Application.systemLanguage == SystemLanguage.Japanese) ? 1 : 0;
                EditorPrefs.SetInt(LANG_PREFS_KEY, _lang);
            }
            return _lang == 1;
        }
    }

    public static void ToggleLanguage()
    {
        _lang = IsJapanese ? 0 : 1;
        EditorPrefs.SetInt(LANG_PREFS_KEY, _lang);
    }

    /// <summary>Returns Japanese text if IsJapanese, otherwise English.</summary>
    public static string L(string ja, string en) => IsJapanese ? ja : en;
}

// =============================================================================
// MapGeneratorEngine — Standalone texture map generation engine (Editor-only)
// =============================================================================

/// <summary>
/// Engine for generating texture maps from mesh geometry and albedo textures.
/// All methods are Editor-only (uses AssetDatabase, EditorUtility, etc.)
/// </summary>
public static class MapGeneratorEngine
{
    // Cached compute shaders
    private static ComputeShader _uvBakeCS;
    private static ComputeShader _blurCS;
    private static ComputeShader _normalCS;
    private static ComputeShader _dilationCS;
    private static ComputeShader _channelPackCS;

    // ===== Public Generation API =====

    /// <summary>
    /// Generate Normal Map. When mesh is provided, bakes mesh curvature into UV space
    /// as a height map, then applies Sobel filter. Albedo detail is blended on top.
    /// Falls back to albedo-only Sobel when no mesh is given.
    /// </summary>
    public static Texture2D GenerateNormalMap(Texture2D albedo, MapGenSettings s,
        Mesh mesh = null, Renderer renderer = null)
    {
        int w = s.outputResolution.x, h = s.outputResolution.y;

        // Build height map from mesh geometry (curvature → UV bake)
        Texture2D meshHeight = null;
        if (mesh != null && mesh.uv != null && mesh.uv.Length > 0)
        {
            float[] curvature = MapGenUtils.ComputeVertexCurvature(mesh);

            // Also compute position-based height along local Y for extra detail
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            float[] posHeight = new float[vertices.Length];
            Bounds bounds = mesh.bounds;
            float boundsHeight = Mathf.Max(bounds.size.magnitude, 0.001f);

            for (int i = 0; i < vertices.Length; i++)
            {
                // Combine curvature with position-projected height
                float projH = Vector3.Dot(vertices[i] - bounds.center, normals[i]) / boundsHeight;
                posHeight[i] = Mathf.Clamp01(0.5f + projH * 0.5f);
            }

            // Blend curvature and position height
            float[] height = new float[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                height[i] = curvature[i] * 0.6f + posHeight[i] * 0.4f;
            }

            meshHeight = BakeAndPostProcess(mesh, height, w, h, s.dilationPixels, s.normalBlurRadius);
        }

        // Build albedo luminance height (for texture detail)
        Texture2D albedoHeight = null;
        if (albedo != null)
        {
            Texture2D readable = MapGenUtils.MakeReadable(albedo, w, h);
            if (readable != null)
            {
                Color[] pixels = readable.GetPixels();
                Color[] grayPixels = new Color[w * h];
                for (int i = 0; i < pixels.Length; i++)
                {
                    float lum = 0.299f * pixels[i].r + 0.587f * pixels[i].g + 0.114f * pixels[i].b;
                    grayPixels[i] = new Color(lum, lum, lum, 1f);
                }

                if (s.normalBlurRadius > 0)
                    MapGenUtils.GaussianBlur(grayPixels, w, h, s.normalBlurRadius);

                albedoHeight = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
                albedoHeight.SetPixels(grayPixels);
                albedoHeight.Apply(false, false);
                UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        // Determine final height map source
        Texture2D finalHeight;
        if (meshHeight != null && albedoHeight != null)
        {
            // Blend: mesh geometry (primary) + albedo detail (secondary)
            Color[] meshPx = meshHeight.GetPixels();
            Color[] albPx = albedoHeight.GetPixels();
            Color[] blended = new Color[w * h];
            for (int i = 0; i < blended.Length; i++)
            {
                // Mesh height as base, albedo adds subtle detail
                float mh = meshPx[i].r;
                float ah = albPx[i].r;
                float h2 = mh * 0.7f + ah * 0.3f;
                blended[i] = new Color(h2, h2, h2, 1f);
            }
            finalHeight = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
            finalHeight.SetPixels(blended);
            finalHeight.Apply(false, false);
            UnityEngine.Object.DestroyImmediate(meshHeight);
            UnityEngine.Object.DestroyImmediate(albedoHeight);
        }
        else if (meshHeight != null)
        {
            finalHeight = meshHeight;
        }
        else if (albedoHeight != null)
        {
            finalHeight = albedoHeight;
        }
        else
        {
            return null;
        }

        // Apply Sobel filter to height map → normal map
        try
        {
            if (SystemInfo.supportsComputeShaders)
            {
                LoadShaders();
                if (_normalCS != null)
                    return GenerateNormalGPU(finalHeight, s, w, h);
            }
            return GenerateNormalCPU(finalHeight, s, w, h);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(finalHeight);
        }
    }

    public static Texture2D GenerateAOMap(Mesh mesh, Renderer renderer, MapGenSettings s)
    {
        if (mesh == null || renderer == null) return null;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Transform xform = renderer.transform;

        float rayLength = s.aoMaxDistance > 0 ? s.aoMaxDistance : mesh.bounds.size.magnitude * 0.5f;
        float[] ao = new float[vertices.Length];

        GameObject tempObj = null;
        try
        {
            tempObj = MapGenUtils.CreateTempMeshCollider(mesh, xform, out MeshCollider collider);
            int layerMask = 1 << 31;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (i % 200 == 0)
                {
                    float progress = (float)i / vertices.Length;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "AO Map", $"Raycasting... ({i}/{vertices.Length})", progress))
                    {
                        return null;
                    }
                }

                Vector3 worldPos = xform.TransformPoint(vertices[i]);
                Vector3 worldNormal = xform.TransformDirection(normals[i]).normalized;

                // Outward hemisphere raycasting
                int hits = 0;
                for (int r = 0; r < s.aoRayCount; r++)
                {
                    Vector3 dir = MapGenUtils.GetHemisphereDirection(worldNormal, r, s.aoRayCount);
                    Ray ray = new Ray(worldPos + worldNormal * 0.001f, dir);
                    if (Physics.Raycast(ray, rayLength, layerMask))
                        hits++;
                }

                // Inward raycasting for concave detection (eye sockets, cavities)
                int inwardRayCount = Mathf.Max(1, s.aoRayCount / 4);
                float inwardRayLength = rayLength * 0.3f;
                int inwardHits = 0;
                for (int r = 0; r < inwardRayCount; r++)
                {
                    Vector3 inwardDir = -MapGenUtils.GetHemisphereDirection(worldNormal, r, inwardRayCount);
                    Ray inwardRay = new Ray(worldPos - worldNormal * 0.002f, inwardDir);
                    if (Physics.Raycast(inwardRay, inwardRayLength, layerMask))
                        inwardHits++;
                }
                float inwardOcclusion = (float)inwardHits / inwardRayCount;

                float outwardOcclusion = 1.0f - (float)hits / s.aoRayCount;
                ao[i] = Mathf.Pow(outwardOcclusion * (1f - inwardOcclusion * 0.5f), s.aoIntensity);
            }

            int w = s.outputResolution.x, h = s.outputResolution.y;
            return BakeAndPostProcess(mesh, ao, w, h, s.aoDilation, 2);
        }
        finally
        {
            if (tempObj != null) UnityEngine.Object.DestroyImmediate(tempObj);
            EditorUtility.ClearProgressBar();
        }
    }

    public static Texture2D GenerateCurvatureMap(Mesh mesh, MapGenSettings s)
    {
        if (mesh == null) return null;

        float[] curvature = MapGenUtils.ComputeVertexCurvature(mesh);
        float[] concavity = MapGenUtils.ComputeVertexConcavity(mesh);

        // Blend concavity into curvature and apply multiplier
        for (int i = 0; i < curvature.Length; i++)
        {
            float combined = curvature[i] - concavity[i] * 0.3f;

            if (Mathf.Abs(s.curvatureMultiplier - 1.0f) > 0.001f)
            {
                combined = 0.5f + (combined - 0.5f) * s.curvatureMultiplier;
            }

            curvature[i] = Mathf.Clamp01(combined);
        }

        int w = s.outputResolution.x, h = s.outputResolution.y;
        return BakeAndPostProcess(mesh, curvature, w, h, s.curvatureDilation, 2);
    }

    public static Texture2D GenerateRoughnessMap(Texture2D albedo, MapGenSettings s)
    {
        if (albedo == null) return null;
        int w = s.outputResolution.x, h = s.outputResolution.y;

        Texture2D readable = MapGenUtils.MakeReadable(albedo, w, h);
        if (readable == null) return null;

        try
        {
            Color[] pixels = readable.GetPixels();
            Color[] result = new Color[w * h];

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float luminance = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

                float maxC = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                float minC = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                float saturation = maxC > 0.001f ? (maxC - minC) / maxC : 0f;

                float roughness = s.roughnessBaseline
                    - luminance * s.luminanceInfluence
                    - saturation * s.saturationInfluence;
                roughness = Mathf.Clamp01(roughness);

                if (s.invertToSmoothness)
                    roughness = 1.0f - roughness;

                result[i] = new Color(roughness, roughness, roughness, 1.0f);
            }

            if (s.roughnessBlur > 0)
                MapGenUtils.GaussianBlur(result, w, h, s.roughnessBlur);

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
            tex.SetPixels(result);
            tex.Apply(false, false);
            return tex;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(readable);
        }
    }

    public static Texture2D GenerateShadowMap(Mesh mesh, Renderer renderer, MapGenSettings s)
    {
        if (mesh == null || renderer == null) return null;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Transform xform = renderer.transform;

        Vector3 lightDir = s.shadowLightDir.normalized;
        float rayLength = mesh.bounds.size.magnitude;
        float spreadRad = s.shadowSpreadAngle * Mathf.Deg2Rad;
        float[] shadow = new float[vertices.Length];

        GameObject tempObj = null;
        try
        {
            tempObj = MapGenUtils.CreateTempMeshCollider(mesh, xform, out MeshCollider collider);
            int layerMask = 1 << 31;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (i % 200 == 0)
                {
                    float progress = (float)i / vertices.Length;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Shadow Map", $"Raycasting... ({i}/{vertices.Length})", progress))
                    {
                        return null;
                    }
                }

                Vector3 worldPos = xform.TransformPoint(vertices[i]);
                Vector3 worldNormal = xform.TransformDirection(normals[i]).normalized;

                float ndotl = Mathf.Max(0, Vector3.Dot(worldNormal, -lightDir));

                int occluded = 0;
                for (int r = 0; r < s.shadowRayCount; r++)
                {
                    // Spread rays around light direction
                    Vector3 dir = SpreadDirection(-lightDir, spreadRad, r, s.shadowRayCount);
                    Ray ray = new Ray(worldPos + worldNormal * 0.001f, dir);
                    if (Physics.Raycast(ray, rayLength, layerMask))
                        occluded++;
                }

                float occlusionRate = (float)occluded / s.shadowRayCount;
                shadow[i] = ndotl * (1.0f - occlusionRate * s.shadowIntensity);
            }

            int w = s.outputResolution.x, h = s.outputResolution.y;
            return BakeAndPostProcess(mesh, shadow, w, h, s.shadowDilation, 2);
        }
        finally
        {
            if (tempObj != null) UnityEngine.Object.DestroyImmediate(tempObj);
            EditorUtility.ClearProgressBar();
        }
    }

    public static Texture2D GenerateControlMap(MapGenSettings s, MapGenResult sources)
    {
        if (sources == null) return null;
        int w = s.outputResolution.x, h = s.outputResolution.y;

        // Try GPU path
        if (SystemInfo.supportsComputeShaders)
        {
            LoadShaders();
            if (_channelPackCS != null)
            {
                Texture2D result = GenerateControlMapGPU(s, sources, w, h);
                if (result != null) return result;
            }
        }

        // CPU fallback
        return GenerateControlMapCPU(s, sources, w, h);
    }

    /// <summary>
    /// Generate all enabled maps and return results.
    /// </summary>
    public static MapGenResult GenerateAll(MapGenerator gen)
    {
        if (gen == null) return null;

        var sw = Stopwatch.StartNew();
        var result = new MapGenResult();

        Mesh mesh = gen.targetMesh;
        Renderer renderer = gen.targetRenderer;
        Texture2D albedo = gen.albedoTexture;
        MapGenSettings s = gen.settings;

        try
        {
            if (s.generateNormal && (albedo != null || mesh != null))
            {
                EditorUtility.DisplayProgressBar("Map Generator", "Generating Normal Map...", 0.0f);
                result.normal = GenerateNormalMap(albedo, s, mesh, renderer);
                gen.lastNormalMap = result.normal;
            }

            if (s.generateAO && mesh != null && renderer != null)
            {
                EditorUtility.DisplayProgressBar("Map Generator", "Generating AO Map...", 0.15f);
                result.ao = GenerateAOMap(mesh, renderer, s);
                gen.lastAOMap = result.ao;
            }

            if (s.generateCurvature && mesh != null)
            {
                EditorUtility.DisplayProgressBar("Map Generator", "Generating Curvature Map...", 0.4f);
                result.curvature = GenerateCurvatureMap(mesh, s);
                gen.lastCurvatureMap = result.curvature;
            }

            if (s.generateRoughness && albedo != null)
            {
                EditorUtility.DisplayProgressBar("Map Generator", "Generating Roughness Map...", 0.55f);
                result.roughness = GenerateRoughnessMap(albedo, s);
                gen.lastRoughnessMap = result.roughness;
            }

            if (s.generateShadow && mesh != null && renderer != null)
            {
                EditorUtility.DisplayProgressBar("Map Generator", "Generating Shadow Map...", 0.65f);
                result.shadow = GenerateShadowMap(mesh, renderer, s);
                gen.lastShadowMap = result.shadow;
            }

            if (s.generateControl)
            {
                EditorUtility.DisplayProgressBar("Map Generator", "Generating Control Map...", 0.85f);
                result.controlMap = GenerateControlMap(s, result);
                gen.lastControlMap = result.controlMap;
            }

            // Save all maps
            EditorUtility.DisplayProgressBar("Map Generator", "Saving textures...", 0.95f);
            SaveAllMaps(gen, result);

            if (s.autoAssignToMaterial && renderer != null && renderer.sharedMaterial != null)
            {
                AssignToMaterial(renderer.sharedMaterial, result, s);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        sw.Stop();
        result.processingTimeMs = sw.ElapsedMilliseconds;
        return result;
    }

    // ===== Save & Assign =====

    public static string SaveTexture(Texture2D tex, string folder, string name,
        OutputFormat format, bool isNormalMap)
    {
        if (tex == null) return null;

        string ext = format == OutputFormat.TGA ? "tga" : format == OutputFormat.EXR ? "exr" : "png";
        string assetPath = $"{folder}/{name}.{ext}";

        // Ensure unique path
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        // Encode
        byte[] bytes;
        switch (format)
        {
            case OutputFormat.TGA:
                bytes = tex.EncodeToTGA();
                break;
            case OutputFormat.EXR:
                bytes = tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
                break;
            default:
                bytes = tex.EncodeToPNG();
                break;
        }

        // Write to disk
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot)) return null;

        string absolutePath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        string dir = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        try
        {
            File.WriteAllBytes(absolutePath, bytes);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapGenerator] Failed to save texture: {e.Message}");
            return null;
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        ConfigureImporter(assetPath, isNormalMap);
        return assetPath;
    }

    // ===== Internal Helpers =====

    private static void SaveAllMaps(MapGenerator gen, MapGenResult result)
    {
        MapGenSettings s = gen.settings;
        string folder = EnsureFolderHierarchy(s.outputFolder);
        string baseName = SanitizeFileName(gen.gameObject.name);

        if (result.normal != null)
            SaveTexture(result.normal, folder, $"{baseName}_Normal", s.outputFormat, true);
        if (result.ao != null)
            SaveTexture(result.ao, folder, $"{baseName}_AO", s.outputFormat, false);
        if (result.curvature != null)
            SaveTexture(result.curvature, folder, $"{baseName}_Curvature", s.outputFormat, false);
        if (result.roughness != null)
            SaveTexture(result.roughness, folder, $"{baseName}_Roughness", s.outputFormat, false);
        if (result.shadow != null)
            SaveTexture(result.shadow, folder, $"{baseName}_Shadow", s.outputFormat, false);
        if (result.controlMap != null)
            SaveTexture(result.controlMap, folder, $"{baseName}_Control", s.outputFormat, false);
    }

    private static void AssignToMaterial(Material mat, MapGenResult result, MapGenSettings s)
    {
        Undo.RecordObject(mat, "MapGenerator: Assign Maps");

        string folder = s.outputFolder;

        // Normal Map → _BumpMap + _UseNormalMap + _NORMALMAP keyword (NataneToon)
        if (result.normal != null)
        {
            Texture2D saved = FindSavedTexture(folder, "Normal");
            if (saved != null) AssignMap(mat, saved, "_BumpMap", "_UseNormalMap", "_NORMALMAP", "_USE_NORMAL_MAP");
        }

        // AO Map → _AOMap + _UseAO + _USE_AO keyword (NataneToon)
        if (result.ao != null)
        {
            Texture2D saved = FindSavedTexture(folder, "AO");
            if (saved != null) AssignMap(mat, saved, "_AOMap", "_UseAO", "_USE_AO");
        }

        // Curvature → _CavityMap (NataneToon)
        if (result.curvature != null)
        {
            Texture2D saved = FindSavedTexture(folder, "Curvature");
            if (saved != null) AssignMap(mat, saved, "_CavityMap");
        }

        // Shadow → _ShadowReceiveMask (NataneToon)
        if (result.shadow != null)
        {
            Texture2D saved = FindSavedTexture(folder, "Shadow");
            if (saved != null) AssignMap(mat, saved, "_ShadowReceiveMask");
        }

        // Roughness → _RoughnessMap + keyword (NataneToon AutoMat)
        // Fallback: Standard Shader _MetallicGlossMap
        if (result.roughness != null)
        {
            Texture2D saved = FindSavedTexture(folder, "Roughness");
            if (saved != null) AssignMap(mat, saved, "_RoughnessMap", "_UseRoughnessMap", "_USE_ROUGHNESS_MAP");
        }

        EditorUtility.SetDirty(mat);
    }

    /// <summary>
    /// Assign a texture to a material property with optional toggle float and keywords.
    /// Tries each property name — skips if the material doesn't have it.
    /// </summary>
    private static void AssignMap(Material mat, Texture2D tex, string texProp,
        string toggleFloat = null, params string[] keywords)
    {
        if (mat.HasProperty(texProp))
        {
            mat.SetTexture(texProp, tex);
        }

        if (toggleFloat != null && mat.HasProperty(toggleFloat))
        {
            mat.SetFloat(toggleFloat, 1f);
        }

        foreach (string kw in keywords)
        {
            if (!string.IsNullOrEmpty(kw))
                mat.EnableKeyword(kw);
        }
    }

    private static Texture2D FindSavedTexture(string folder, string suffix)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return null;
        string[] guids = AssetDatabase.FindAssets($"t:Texture2D", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.EndsWith($"_{suffix}"))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return null;
    }

    private static void ConfigureImporter(string assetPath, bool isNormalMap)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        if (isNormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
        }
        else
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
        }

        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    // ===== Normal Map GPU/CPU =====

    private static Texture2D GenerateNormalGPU(Texture2D readable, MapGenSettings s, int w, int h)
    {
        RenderTexture inputRT = CreateRT(w, h);
        Graphics.Blit(readable, inputRT);

        RenderTexture heightRT = CreateRT(w, h);
        RenderTexture normalRT = CreateRT(w, h);

        try
        {
            // Convert to grayscale (use luminance via simple blit — compute input is already linear)
            // We use the input directly as the "height" since NormalFromHeight reads .r
            // First blur if needed, then Sobel
            if (s.normalBlurRadius > 0 && _blurCS != null)
            {
                DispatchBlur(inputRT, heightRT, w, h, s.normalBlurRadius);
            }
            else
            {
                Graphics.Blit(inputRT, heightRT);
            }

            // Sobel → Normal
            int k = _normalCS.FindKernel("CSMain");
            _normalCS.SetInts("_TexSize", w, h);
            _normalCS.SetFloat("_NormalStrength", s.normalStrength);
            _normalCS.SetTexture(k, "_Input", heightRT);
            _normalCS.SetTexture(k, "_Result", normalRT);
            _normalCS.Dispatch(k, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);

            return RTToTexture2D(normalRT, w, h);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(inputRT);
            RenderTexture.ReleaseTemporary(heightRT);
            RenderTexture.ReleaseTemporary(normalRT);
        }
    }

    private static Texture2D GenerateNormalCPU(Texture2D readable, MapGenSettings s, int w, int h)
    {
        Color[] pixels = readable.GetPixels();

        // Convert to grayscale height
        float[] height = new float[w * h];
        for (int i = 0; i < pixels.Length; i++)
        {
            height[i] = 0.299f * pixels[i].r + 0.587f * pixels[i].g + 0.114f * pixels[i].b;
        }

        // Optional blur on height
        if (s.normalBlurRadius > 0)
        {
            Color[] heightColors = new Color[w * h];
            for (int i = 0; i < height.Length; i++)
                heightColors[i] = new Color(height[i], height[i], height[i], 1);
            MapGenUtils.GaussianBlur(heightColors, w, h, s.normalBlurRadius);
            for (int i = 0; i < height.Length; i++)
                height[i] = heightColors[i].r;
        }

        // Sobel filter
        Color[] result = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float tl = SampleHeight(height, w, h, x - 1, y - 1);
                float t  = SampleHeight(height, w, h, x,     y - 1);
                float tr = SampleHeight(height, w, h, x + 1, y - 1);
                float l  = SampleHeight(height, w, h, x - 1, y);
                float r  = SampleHeight(height, w, h, x + 1, y);
                float bl = SampleHeight(height, w, h, x - 1, y + 1);
                float b  = SampleHeight(height, w, h, x,     y + 1);
                float br = SampleHeight(height, w, h, x + 1, y + 1);

                float dx = (tr + 2 * r + br) - (tl + 2 * l + bl);
                float dy = (bl + 2 * b + br) - (tl + 2 * t + tr);

                Vector3 normal = new Vector3(-dx * s.normalStrength, dy * s.normalStrength, 1.0f).normalized;
                result[y * w + x] = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z * 0.5f + 0.5f,
                    1.0f);
            }
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
        tex.SetPixels(result);
        tex.Apply(false, false);
        return tex;
    }

    private static float SampleHeight(float[] height, int w, int h, int x, int y)
    {
        x = Mathf.Clamp(x, 0, w - 1);
        y = Mathf.Clamp(y, 0, h - 1);
        return height[y * w + x];
    }

    // ===== Control Map GPU/CPU =====

    private static Texture2D GenerateControlMapGPU(MapGenSettings s, MapGenResult sources, int w, int h)
    {
        RenderTexture resultRT = CreateRT(w, h);

        var channelTextures = new Dictionary<ControlMapChannel, Texture2D>
        {
            { ControlMapChannel.AO, sources.ao },
            { ControlMapChannel.Curvature, sources.curvature },
            { ControlMapChannel.Roughness, sources.roughness },
            { ControlMapChannel.Smoothness, sources.roughness }, // same texture, smoothness is already inverted if needed
            { ControlMapChannel.Shadow, sources.shadow },
        };

        try
        {
            int k = _channelPackCS.FindKernel("CSMain");
            _channelPackCS.SetInts("_TexSize", w, h);

            SetChannelSource(k, "R", s.channelR, channelTextures, resultRT, w, h);
            SetChannelSource(k, "G", s.channelG, channelTextures, resultRT, w, h);
            SetChannelSource(k, "B", s.channelB, channelTextures, resultRT, w, h);
            SetChannelSource(k, "A", s.channelA, channelTextures, resultRT, w, h);

            _channelPackCS.SetTexture(k, "_Result", resultRT);
            _channelPackCS.Dispatch(k, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);

            return RTToTexture2D(resultRT, w, h);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(resultRT);
        }
    }

    private static void SetChannelSource(int kernel, string channel, ControlMapChannel source,
        Dictionary<ControlMapChannel, Texture2D> textures, RenderTexture dummyRT, int w, int h)
    {
        Texture2D tex = null;
        if (source != ControlMapChannel.None && textures.ContainsKey(source))
            tex = textures[source];

        string sourceName = $"_Source{channel}";
        string useName = $"_UseSource{channel}";
        string defaultName = $"_Default{channel}";

        if (tex != null)
        {
            RenderTexture rt = CreateRT(w, h);
            Graphics.Blit(tex, rt);
            _channelPackCS.SetTexture(kernel, sourceName, rt);
            _channelPackCS.SetInt(useName, 1);
            // Note: these RTs leak, but are temporary for this frame
        }
        else
        {
            _channelPackCS.SetTexture(kernel, sourceName, dummyRT); // bind something
            _channelPackCS.SetInt(useName, 0);
            float defaultVal = (source == ControlMapChannel.None) ? 1.0f : 0.0f;
            _channelPackCS.SetFloat(defaultName, defaultVal);
        }
    }

    private static Texture2D GenerateControlMapCPU(MapGenSettings s, MapGenResult sources, int w, int h)
    {
        Color[] result = new Color[w * h];

        Color[] rSrc = GetChannelPixels(s.channelR, sources, w, h);
        Color[] gSrc = GetChannelPixels(s.channelG, sources, w, h);
        Color[] bSrc = GetChannelPixels(s.channelB, sources, w, h);
        Color[] aSrc = GetChannelPixels(s.channelA, sources, w, h);

        for (int i = 0; i < w * h; i++)
        {
            result[i] = new Color(
                rSrc != null ? rSrc[i].r : 1.0f,
                gSrc != null ? gSrc[i].r : 1.0f,
                bSrc != null ? bSrc[i].r : 1.0f,
                aSrc != null ? aSrc[i].r : 1.0f);
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
        tex.SetPixels(result);
        tex.Apply(false, false);
        return tex;
    }

    private static Color[] GetChannelPixels(ControlMapChannel channel, MapGenResult sources, int w, int h)
    {
        Texture2D source = null;
        switch (channel)
        {
            case ControlMapChannel.AO: source = sources.ao; break;
            case ControlMapChannel.Curvature: source = sources.curvature; break;
            case ControlMapChannel.Roughness:
            case ControlMapChannel.Smoothness: source = sources.roughness; break;
            case ControlMapChannel.Shadow: source = sources.shadow; break;
            case ControlMapChannel.NormalR:
            case ControlMapChannel.NormalG: source = sources.normal; break;
            case ControlMapChannel.None: return null;
        }

        if (source == null) return null;

        // Resize to target if needed
        if (source.width != w || source.height != h)
        {
            Texture2D resized = MapGenUtils.MakeReadable(source, w, h);
            Color[] pixels = resized.GetPixels();
            UnityEngine.Object.DestroyImmediate(resized);

            // For NormalG, swap to G channel
            if (channel == ControlMapChannel.NormalG)
            {
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(pixels[i].g, pixels[i].g, pixels[i].g, pixels[i].a);
            }
            return pixels;
        }

        Color[] src = source.GetPixels();
        if (channel == ControlMapChannel.NormalG)
        {
            for (int i = 0; i < src.Length; i++)
                src[i] = new Color(src[i].g, src[i].g, src[i].g, src[i].a);
        }
        return src;
    }

    // ===== Bake + PostProcess Pipeline =====

    private static Texture2D BakeAndPostProcess(Mesh mesh, float[] vertexData, int w, int h,
        int dilationPixels, int blurRadius)
    {
        // Try GPU bake
        if (SystemInfo.supportsComputeShaders)
        {
            LoadShaders();
            if (_uvBakeCS != null)
            {
                Texture2D gpuResult = BakeScalarToUVGPU(mesh, vertexData, w, h);
                if (gpuResult != null)
                {
                    // GPU dilation
                    if (dilationPixels > 0 && _dilationCS != null)
                        gpuResult = ApplyDilationGPU(gpuResult, w, h, dilationPixels);
                    // GPU blur
                    if (blurRadius > 0 && _blurCS != null)
                        gpuResult = ApplyBlurGPU(gpuResult, w, h, blurRadius);
                    return gpuResult;
                }
            }
        }

        // CPU fallback
        Color[] pixels = MapGenUtils.RasterizeToUV(mesh, vertexData, w, h);
        if (dilationPixels > 0) MapGenUtils.Dilate(pixels, w, h, dilationPixels);
        if (blurRadius > 0) MapGenUtils.GaussianBlur(pixels, w, h, blurRadius);

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static Texture2D BakeScalarToUVGPU(Mesh mesh, float[] vertexData, int w, int h)
    {
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;
        int triCount = triangles.Length / 3;

        ComputeBuffer uvBuffer = new ComputeBuffer(uvs.Length, sizeof(float) * 2);
        ComputeBuffer triBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        ComputeBuffer dataBuffer = new ComputeBuffer(vertexData.Length, sizeof(float));

        try
        {
            uvBuffer.SetData(uvs);
            triBuffer.SetData(triangles);
            dataBuffer.SetData(vertexData);

            RenderTexture rt = CreateRT(w, h);
            try
            {
                int kernel = _uvBakeCS.FindKernel("CSMain");
                _uvBakeCS.SetInts("_TexSize", w, h);
                _uvBakeCS.SetInt("_TriangleCount", triCount);
                _uvBakeCS.SetBuffer(kernel, "_UVs", uvBuffer);
                _uvBakeCS.SetBuffer(kernel, "_Triangles", triBuffer);
                _uvBakeCS.SetBuffer(kernel, "_VertexData", dataBuffer);
                _uvBakeCS.SetTexture(kernel, "_Result", rt);
                _uvBakeCS.Dispatch(kernel, Mathf.CeilToInt(triCount / 64f), 1, 1);

                return RTToTexture2D(rt, w, h);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }
        finally
        {
            uvBuffer.Release();
            triBuffer.Release();
            dataBuffer.Release();
        }
    }

    private static Texture2D ApplyDilationGPU(Texture2D input, int w, int h, int iterations)
    {
        RenderTexture rtA = CreateRT(w, h);
        RenderTexture rtB = CreateRT(w, h);
        Graphics.Blit(input, rtA);
        UnityEngine.Object.DestroyImmediate(input);

        try
        {
            int kernel = _dilationCS.FindKernel("CSMain");
            _dilationCS.SetInts("_TexSize", w, h);
            int groups = Mathf.CeilToInt(w / 8f);
            int groupsH = Mathf.CeilToInt(h / 8f);

            for (int i = 0; i < iterations; i++)
            {
                _dilationCS.SetTexture(kernel, "_Input", rtA);
                _dilationCS.SetTexture(kernel, "_Result", rtB);
                _dilationCS.Dispatch(kernel, groups, groupsH, 1);

                // Swap
                var temp = rtA;
                rtA = rtB;
                rtB = temp;
            }

            return RTToTexture2D(rtA, w, h);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(rtA);
            RenderTexture.ReleaseTemporary(rtB);
        }
    }

    private static Texture2D ApplyBlurGPU(Texture2D input, int w, int h, int radius)
    {
        RenderTexture inputRT = CreateRT(w, h);
        RenderTexture outputRT = CreateRT(w, h);
        Graphics.Blit(input, inputRT);
        UnityEngine.Object.DestroyImmediate(input);

        try
        {
            DispatchBlur(inputRT, outputRT, w, h, radius);
            return RTToTexture2D(outputRT, w, h);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(inputRT);
            RenderTexture.ReleaseTemporary(outputRT);
        }
    }

    // ===== Compute Dispatch Helpers =====

    private static void DispatchBlur(RenderTexture input, RenderTexture output, int w, int h, int radius)
    {
        float sigma = Mathf.Max(radius / 3f, 0.5f);
        RenderTexture temp = CreateRT(w, h);

        try
        {
            int kH = _blurCS.FindKernel("BlurH");
            _blurCS.SetInts("_TexSize", w, h);
            _blurCS.SetInt("_BlurRadius", radius);
            _blurCS.SetFloat("_Sigma", sigma);
            _blurCS.SetTexture(kH, "_Input", input);
            _blurCS.SetTexture(kH, "_Result", temp);
            _blurCS.Dispatch(kH, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);

            int kV = _blurCS.FindKernel("BlurV");
            _blurCS.SetTexture(kV, "_Input", temp);
            _blurCS.SetTexture(kV, "_Result", output);
            _blurCS.Dispatch(kV, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(temp);
        }
    }

    // ===== Shadow Ray Spread =====

    private static Vector3 SpreadDirection(Vector3 baseDir, float spreadRad, int index, int total)
    {
        if (total <= 1 || spreadRad < 0.001f) return baseDir;

        float goldenRatio = (1.0f + Mathf.Sqrt(5.0f)) * 0.5f;
        float theta = 2.0f * Mathf.PI * index / goldenRatio;
        float r = spreadRad * Mathf.Sqrt((float)index / total);

        // Create perpendicular basis
        Vector3 up = Mathf.Abs(baseDir.y) < 0.999f ? Vector3.up : Vector3.right;
        Vector3 tangent = Vector3.Cross(up, baseDir).normalized;
        Vector3 bitangent = Vector3.Cross(baseDir, tangent);

        Vector3 offset = tangent * (Mathf.Cos(theta) * r) + bitangent * (Mathf.Sin(theta) * r);
        return (baseDir + offset).normalized;
    }

    // ===== RT Utilities =====

    internal static RenderTexture CreateRT(int w, int h)
    {
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }

    internal static Texture2D RTToTexture2D(RenderTexture rt, int w, int h)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply(false, false);
        RenderTexture.active = prev;
        return tex;
    }

    // ===== Shader Loading =====

    private static void LoadShaders()
    {
        if (_uvBakeCS == null) _uvBakeCS = FindComputeShader("MapGen_UVBake");
        if (_blurCS == null) _blurCS = FindComputeShader("MapGen_GaussianBlur");
        if (_normalCS == null) _normalCS = FindComputeShader("MapGen_NormalFromHeight");
        if (_dilationCS == null) _dilationCS = FindComputeShader("MapGen_Dilation");
        if (_channelPackCS == null) _channelPackCS = FindComputeShader("MapGen_ChannelPack");
    }

    internal static ComputeShader FindComputeShader(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"t:ComputeShader {name}");
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[MapGenerator] Compute Shader not found: {name} (falling back to CPU)");
            return null;
        }
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
    }

    // ===== Path Utilities =====

    internal static string EnsureFolderHierarchy(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath)) return assetPath;

        string[] segments = assetPath.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[i]);
            current = next;
        }
        return current;
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(value) ? "Generated" : value;
    }
}

// =============================================================================
// MapGeneratorEditor — Custom Inspector
// =============================================================================

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    // Foldout states
    private bool _foldCommon = true;
    private bool _foldNormal = true;
    private bool _foldAO = true;
    private bool _foldCurvature = true;
    private bool _foldRoughness = true;
    private bool _foldShadow = true;
    private bool _foldControl = true;

    // Preview textures (scaled down)
    private Dictionary<string, Texture2D> _previews = new Dictionary<string, Texture2D>();

    public override void OnInspectorGUI()
    {
        MapGenerator gen = (MapGenerator)target;
        serializedObject.Update();

        // Auto-detect info
        EditorGUILayout.HelpBox(
            $"{MapGenL.L("レンダラー:", "Renderer:")} {(gen.targetRenderer != null ? gen.targetRenderer.GetType().Name : "None")}\n" +
            $"{MapGenL.L("メッシュ:", "Mesh:")} {(gen.targetMesh != null ? gen.targetMesh.name + $" ({gen.targetMesh.vertexCount} verts)" : "None")}\n" +
            $"{MapGenL.L("アルベド:", "Albedo:")} {(gen.albedoTexture != null ? gen.albedoTexture.name : "None")}",
            MessageType.Info);

        // Language toggle
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(MapGenL.IsJapanese ? "English" : "日本語", GUILayout.Width(80)))
        {
            MapGenL.ToggleLanguage();
        }
        EditorGUILayout.EndHorizontal();

        // ===== Common Settings =====
        _foldCommon = EditorGUILayout.Foldout(_foldCommon, MapGenL.L("共通設定", "Common Settings"), true, EditorStyles.foldoutHeader);
        if (_foldCommon)
        {
            EditorGUI.indentLevel++;
            gen.albedoTexture = (Texture2D)EditorGUILayout.ObjectField(MapGenL.L("アルベドテクスチャ", "Albedo Texture"), gen.albedoTexture, typeof(Texture2D), false);
            gen.settings.outputResolution = EditorGUILayout.Vector2IntField(MapGenL.L("出力解像度", "Output Resolution"), gen.settings.outputResolution);

            EditorGUILayout.BeginHorizontal();
            gen.settings.outputFolder = EditorGUILayout.TextField(MapGenL.L("出力フォルダ", "Output Folder"), gen.settings.outputFolder);
            if (GUILayout.Button(MapGenL.L("参照", "Browse"), GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel(MapGenL.L("出力フォルダを選択", "Select Output Folder"), "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                        gen.settings.outputFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();

            gen.settings.outputFormat = (OutputFormat)EditorGUILayout.EnumPopup(MapGenL.L("出力形式", "Output Format"), gen.settings.outputFormat);
            gen.settings.autoAssignToMaterial = EditorGUILayout.Toggle(MapGenL.L("マテリアルに自動割り当て", "Auto Assign to Material"), gen.settings.autoAssignToMaterial);
            gen.settings.dilationPixels = EditorGUILayout.IntSlider(MapGenL.L("ダイレーションピクセル", "Dilation Pixels"), gen.settings.dilationPixels, 0, 16);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // ===== Generate All Button =====
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button(MapGenL.L("★ 全マップ生成", "★ Generate All Maps"), GUILayout.Height(40)))
        {
            Undo.RecordObject(gen, "Generate All Maps");
            MapGenResult result = MapGeneratorEngine.GenerateAll(gen);
            if (result != null)
            {
                EditorUtility.DisplayDialog("Map Generator",
                    MapGenL.L($"全マップの生成が完了しました！\n処理時間: {result.processingTimeMs:F0}ms",
                              $"All maps generated successfully!\nTime: {result.processingTimeMs:F0}ms"),
                    "OK");
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // ===== Normal Map =====
        DrawMapSection(ref _foldNormal, MapGenL.L("ノーマルマップ", "Normal Map"), ref gen.settings.generateNormal, () =>
        {
            gen.settings.normalStrength = EditorGUILayout.Slider(MapGenL.L("強度", "Strength"), gen.settings.normalStrength, 0.1f, 10f);
            gen.settings.normalBlurRadius = EditorGUILayout.IntSlider(MapGenL.L("ぼかし半径", "Blur Radius"), gen.settings.normalBlurRadius, 0, 10);
            DrawPreviewAndButton(gen.lastNormalMap, "Normal", () =>
            {
                Undo.RecordObject(gen, "Generate Normal Map");
                gen.lastNormalMap = MapGeneratorEngine.GenerateNormalMap(gen.albedoTexture, gen.settings,
                    gen.targetMesh, gen.targetRenderer);
                SaveSingleMap(gen, gen.lastNormalMap, "Normal", true);
            }, gen.albedoTexture != null || gen.targetMesh != null);
        });

        // ===== AO Map =====
        DrawMapSection(ref _foldAO, MapGenL.L("AOマップ", "AO Map"), ref gen.settings.generateAO, () =>
        {
            gen.settings.aoRayCount = EditorGUILayout.IntSlider(MapGenL.L("レイ数", "Ray Count"), gen.settings.aoRayCount, 8, 256);
            gen.settings.aoMaxDistance = EditorGUILayout.FloatField(MapGenL.L("最大距離", "Max Distance"), gen.settings.aoMaxDistance);
            gen.settings.aoIntensity = EditorGUILayout.Slider(MapGenL.L("強度", "Intensity"), gen.settings.aoIntensity, 0.1f, 5f);
            gen.settings.aoDilation = EditorGUILayout.IntSlider(MapGenL.L("ダイレーション", "Dilation"), gen.settings.aoDilation, 0, 16);
            DrawPreviewAndButton(gen.lastAOMap, "AO", () =>
            {
                Undo.RecordObject(gen, "Generate AO Map");
                gen.lastAOMap = MapGeneratorEngine.GenerateAOMap(gen.targetMesh, gen.targetRenderer, gen.settings);
                SaveSingleMap(gen, gen.lastAOMap, "AO", false);
            }, gen.targetMesh != null);
        });

        // ===== Curvature Map =====
        DrawMapSection(ref _foldCurvature, MapGenL.L("カーブチャーマップ", "Curvature Map"), ref gen.settings.generateCurvature, () =>
        {
            gen.settings.curvatureMultiplier = EditorGUILayout.Slider(MapGenL.L("倍率", "Multiplier"), gen.settings.curvatureMultiplier, 0.1f, 5f);
            gen.settings.curvatureDilation = EditorGUILayout.IntSlider(MapGenL.L("ダイレーション", "Dilation"), gen.settings.curvatureDilation, 0, 16);
            DrawPreviewAndButton(gen.lastCurvatureMap, "Curvature", () =>
            {
                Undo.RecordObject(gen, "Generate Curvature Map");
                gen.lastCurvatureMap = MapGeneratorEngine.GenerateCurvatureMap(gen.targetMesh, gen.settings);
                SaveSingleMap(gen, gen.lastCurvatureMap, "Curvature", false);
            }, gen.targetMesh != null);
        });

        // ===== Roughness Map =====
        DrawMapSection(ref _foldRoughness, MapGenL.L("ラフネス/スムーズネスマップ", "Roughness/Smoothness Map"), ref gen.settings.generateRoughness, () =>
        {
            gen.settings.roughnessBaseline = EditorGUILayout.Slider(MapGenL.L("ベースライン", "Baseline"), gen.settings.roughnessBaseline, 0f, 1f);
            gen.settings.luminanceInfluence = EditorGUILayout.Slider(MapGenL.L("輝度の影響", "Luminance Influence"), gen.settings.luminanceInfluence, 0f, 1f);
            gen.settings.saturationInfluence = EditorGUILayout.Slider(MapGenL.L("彩度の影響", "Saturation Influence"), gen.settings.saturationInfluence, 0f, 1f);
            gen.settings.invertToSmoothness = EditorGUILayout.Toggle(MapGenL.L("スムーズネスに反転", "Invert to Smoothness"), gen.settings.invertToSmoothness);
            gen.settings.roughnessBlur = EditorGUILayout.IntSlider(MapGenL.L("ぼかし半径", "Blur Radius"), gen.settings.roughnessBlur, 0, 10);
            DrawPreviewAndButton(gen.lastRoughnessMap, "Roughness", () =>
            {
                Undo.RecordObject(gen, "Generate Roughness Map");
                gen.lastRoughnessMap = MapGeneratorEngine.GenerateRoughnessMap(gen.albedoTexture, gen.settings);
                SaveSingleMap(gen, gen.lastRoughnessMap, "Roughness", false);
            }, gen.albedoTexture != null);
        });

        // ===== Shadow Map =====
        DrawMapSection(ref _foldShadow, MapGenL.L("シャドウマップ", "Shadow Map"), ref gen.settings.generateShadow, () =>
        {
            gen.settings.shadowLightDir = EditorGUILayout.Vector3Field(MapGenL.L("ライト方向", "Light Direction"), gen.settings.shadowLightDir);
            gen.settings.shadowRayCount = EditorGUILayout.IntSlider(MapGenL.L("レイ数", "Ray Count"), gen.settings.shadowRayCount, 4, 128);
            gen.settings.shadowSpreadAngle = EditorGUILayout.Slider(MapGenL.L("拡散角度", "Spread Angle"), gen.settings.shadowSpreadAngle, 0f, 30f);
            gen.settings.shadowIntensity = EditorGUILayout.Slider(MapGenL.L("強度", "Intensity"), gen.settings.shadowIntensity, 0f, 2f);
            gen.settings.shadowDilation = EditorGUILayout.IntSlider(MapGenL.L("ダイレーション", "Dilation"), gen.settings.shadowDilation, 0, 16);
            DrawPreviewAndButton(gen.lastShadowMap, "Shadow", () =>
            {
                Undo.RecordObject(gen, "Generate Shadow Map");
                gen.lastShadowMap = MapGeneratorEngine.GenerateShadowMap(gen.targetMesh, gen.targetRenderer, gen.settings);
                SaveSingleMap(gen, gen.lastShadowMap, "Shadow", false);
            }, gen.targetMesh != null);
        });

        // ===== Control Map =====
        DrawMapSection(ref _foldControl, MapGenL.L("コントロールマップ", "Control Map"), ref gen.settings.generateControl, () =>
        {
            gen.settings.channelR = (ControlMapChannel)EditorGUILayout.EnumPopup(MapGenL.L("Rチャンネル", "R Channel"), gen.settings.channelR);
            gen.settings.channelG = (ControlMapChannel)EditorGUILayout.EnumPopup(MapGenL.L("Gチャンネル", "G Channel"), gen.settings.channelG);
            gen.settings.channelB = (ControlMapChannel)EditorGUILayout.EnumPopup(MapGenL.L("Bチャンネル", "B Channel"), gen.settings.channelB);
            gen.settings.channelA = (ControlMapChannel)EditorGUILayout.EnumPopup(MapGenL.L("Aチャンネル", "A Channel"), gen.settings.channelA);
            DrawPreviewAndButton(gen.lastControlMap, "Control", () =>
            {
                Undo.RecordObject(gen, "Generate Control Map");
                var sources = new MapGenResult
                {
                    normal = gen.lastNormalMap,
                    ao = gen.lastAOMap,
                    curvature = gen.lastCurvatureMap,
                    roughness = gen.lastRoughnessMap,
                    shadow = gen.lastShadowMap
                };
                gen.lastControlMap = MapGeneratorEngine.GenerateControlMap(gen.settings, sources);
                SaveSingleMap(gen, gen.lastControlMap, "Control", false);
            }, true);
        });

        EditorGUILayout.Space(10);

        // ===== Open Detailed Window =====
        if (GUILayout.Button(MapGenL.L("詳細ウィンドウを開く...", "Open Detailed Window..."), GUILayout.Height(25)))
        {
            MapGeneratorWindow.Open(gen);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(gen);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ===== Scene GUI for Shadow Light Direction Handle =====

    private void OnSceneGUI()
    {
        MapGenerator gen = (MapGenerator)target;
        if (gen.targetRenderer == null || !gen.settings.generateShadow) return;

        Vector3 center = gen.targetRenderer.bounds.center;
        float handleSize = HandleUtility.GetHandleSize(center) * 1.5f;

        // Draw light direction arrow
        Vector3 lightEnd = center + gen.settings.shadowLightDir.normalized * handleSize;

        Handles.color = Color.yellow;
        Handles.DrawLine(center, lightEnd);
        Handles.ConeHandleCap(0, lightEnd, Quaternion.LookRotation(gen.settings.shadowLightDir), handleSize * 0.15f, EventType.Repaint);

        // Draggable handle
        EditorGUI.BeginChangeCheck();
        Vector3 newEnd = Handles.PositionHandle(lightEnd, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gen, "Change Shadow Light Direction");
            gen.settings.shadowLightDir = (newEnd - center).normalized;
            EditorUtility.SetDirty(gen);
        }
    }

    // ===== UI Helpers =====

    private void DrawMapSection(ref bool foldout, string title, ref bool enabled, Action content)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
        enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(20));
        EditorGUILayout.EndHorizontal();

        if (foldout && enabled)
        {
            EditorGUI.indentLevel++;
            content();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewAndButton(Texture2D preview, string mapName, Action generateAction, bool canGenerate)
    {
        EditorGUILayout.BeginHorizontal();

        // 64x64 preview
        if (preview != null)
        {
            GUILayout.Label("", GUILayout.Width(64), GUILayout.Height(64));
            Rect previewRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawPreviewTexture(previewRect, preview);
        }
        else
        {
            GUILayout.Label(MapGenL.L("プレビューなし", "No preview"), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(64), GUILayout.Height(64));
        }

        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(!canGenerate);
        if (GUILayout.Button(MapGenL.L($"{mapName} のみ生成", $"Generate {mapName} Only"), GUILayout.Height(30), GUILayout.Width(180)))
        {
            generateAction?.Invoke();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
    }

    private void SaveSingleMap(MapGenerator gen, Texture2D tex, string suffix, bool isNormal)
    {
        if (tex == null) return;
        string folder = MapGeneratorEngine.EnsureFolderHierarchy(gen.settings.outputFolder);
        string baseName = gen.gameObject.name;
        foreach (char c in Path.GetInvalidFileNameChars()) baseName = baseName.Replace(c, '_');

        string path = MapGeneratorEngine.SaveTexture(tex, folder, $"{baseName}_{suffix}",
            gen.settings.outputFormat, isNormal);

        if (path != null)
        {
            Debug.Log($"[MapGenerator] Saved {suffix} map to: {path}");
        }
    }
}

// =============================================================================
// MapGeneratorWindow — Detailed EditorWindow with full preview
// =============================================================================

public class MapGeneratorWindow : EditorWindow
{
    private MapGenerator _target;
    private Vector2 _scrollLeft;
    private Vector2 _scrollRight;
    private int _selectedPreview = 0;
    private string[] _previewNames = { "Normal", "AO", "Curvature", "Roughness", "Shadow", "Control" };

    [MenuItem("Tools/MapGenerator/Map Generator Window")]
    public static void Open()
    {
        var window = GetWindow<MapGeneratorWindow>(MapGenL.L("マップジェネレーター", "Map Generator"));
        window.minSize = new Vector2(800, 500);
    }

    public static void Open(MapGenerator target)
    {
        var window = GetWindow<MapGeneratorWindow>(MapGenL.L("マップジェネレーター", "Map Generator"));
        window._target = target;
        window.minSize = new Vector2(800, 500);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // ===== Left Panel: Settings =====
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.45f));
        _scrollLeft = EditorGUILayout.BeginScrollView(_scrollLeft);

        EditorGUILayout.LabelField(MapGenL.L("マップジェネレーター", "Map Generator"), EditorStyles.boldLabel);

        // Language toggle
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(MapGenL.IsJapanese ? "English" : "日本語", GUILayout.Width(80)))
        {
            MapGenL.ToggleLanguage();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Target selection
        _target = (MapGenerator)EditorGUILayout.ObjectField(MapGenL.L("ターゲット", "Target"), _target, typeof(MapGenerator), true);

        if (_target == null)
        {
            EditorGUILayout.HelpBox(MapGenL.L("MapGenerator コンポーネントを持つ GameObject を選択するか、ここにドラッグしてください。", "Select a GameObject with MapGenerator component, or drag it here."), MessageType.Info);

            // Try to find from selection
            if (Selection.activeGameObject != null)
            {
                MapGenerator found = Selection.activeGameObject.GetComponent<MapGenerator>();
                if (found != null)
                    _target = found;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            return;
        }

        EditorGUILayout.Space(5);

        // Info
        EditorGUILayout.HelpBox(
            $"{MapGenL.L("メッシュ:", "Mesh:")} {(_target.targetMesh != null ? _target.targetMesh.name : "None")}\n" +
            $"{MapGenL.L("頂点数:", "Vertices:")} {(_target.targetMesh != null ? _target.targetMesh.vertexCount.ToString() : "N/A")}\n" +
            $"{MapGenL.L("アルベド:", "Albedo:")} {(_target.albedoTexture != null ? _target.albedoTexture.name : "None")}",
            MessageType.Info);

        EditorGUILayout.Space(5);

        // Settings
        var s = _target.settings;

        EditorGUILayout.LabelField(MapGenL.L("出力設定", "Output Settings"), EditorStyles.boldLabel);
        s.outputResolution = EditorGUILayout.Vector2IntField(MapGenL.L("解像度", "Resolution"), s.outputResolution);
        s.outputFolder = EditorGUILayout.TextField(MapGenL.L("出力フォルダ", "Output Folder"), s.outputFolder);
        s.outputFormat = (OutputFormat)EditorGUILayout.EnumPopup(MapGenL.L("形式", "Format"), s.outputFormat);
        s.autoAssignToMaterial = EditorGUILayout.Toggle(MapGenL.L("自動割り当て", "Auto Assign"), s.autoAssignToMaterial);
        s.dilationPixels = EditorGUILayout.IntSlider(MapGenL.L("ダイレーション", "Dilation"), s.dilationPixels, 0, 16);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(MapGenL.L("ノーマルマップ", "Normal Map"), EditorStyles.boldLabel);
        s.generateNormal = EditorGUILayout.Toggle(MapGenL.L("有効", "Enable"), s.generateNormal);
        if (s.generateNormal)
        {
            s.normalStrength = EditorGUILayout.Slider(MapGenL.L("強度", "Strength"), s.normalStrength, 0.1f, 10f);
            s.normalBlurRadius = EditorGUILayout.IntSlider(MapGenL.L("ぼかし", "Blur"), s.normalBlurRadius, 0, 10);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(MapGenL.L("AOマップ", "AO Map"), EditorStyles.boldLabel);
        s.generateAO = EditorGUILayout.Toggle(MapGenL.L("有効", "Enable"), s.generateAO);
        if (s.generateAO)
        {
            s.aoRayCount = EditorGUILayout.IntSlider(MapGenL.L("レイ数", "Rays"), s.aoRayCount, 8, 256);
            s.aoMaxDistance = EditorGUILayout.FloatField(MapGenL.L("最大距離", "Max Distance"), s.aoMaxDistance);
            s.aoIntensity = EditorGUILayout.Slider(MapGenL.L("強度", "Intensity"), s.aoIntensity, 0.1f, 5f);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(MapGenL.L("カーブチャーマップ", "Curvature Map"), EditorStyles.boldLabel);
        s.generateCurvature = EditorGUILayout.Toggle(MapGenL.L("有効", "Enable"), s.generateCurvature);
        if (s.generateCurvature)
        {
            s.curvatureMultiplier = EditorGUILayout.Slider(MapGenL.L("倍率", "Multiplier"), s.curvatureMultiplier, 0.1f, 5f);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(MapGenL.L("ラフネスマップ", "Roughness Map"), EditorStyles.boldLabel);
        s.generateRoughness = EditorGUILayout.Toggle(MapGenL.L("有効", "Enable"), s.generateRoughness);
        if (s.generateRoughness)
        {
            s.roughnessBaseline = EditorGUILayout.Slider(MapGenL.L("ベースライン", "Baseline"), s.roughnessBaseline, 0f, 1f);
            s.luminanceInfluence = EditorGUILayout.Slider(MapGenL.L("輝度", "Luminance"), s.luminanceInfluence, 0f, 1f);
            s.saturationInfluence = EditorGUILayout.Slider(MapGenL.L("彩度", "Saturation"), s.saturationInfluence, 0f, 1f);
            s.invertToSmoothness = EditorGUILayout.Toggle(MapGenL.L("スムーズネスに反転", "Invert to Smoothness"), s.invertToSmoothness);
            s.roughnessBlur = EditorGUILayout.IntSlider(MapGenL.L("ぼかし", "Blur"), s.roughnessBlur, 0, 10);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(MapGenL.L("シャドウマップ", "Shadow Map"), EditorStyles.boldLabel);
        s.generateShadow = EditorGUILayout.Toggle(MapGenL.L("有効", "Enable"), s.generateShadow);
        if (s.generateShadow)
        {
            s.shadowLightDir = EditorGUILayout.Vector3Field(MapGenL.L("ライト方向", "Light Dir"), s.shadowLightDir);
            s.shadowRayCount = EditorGUILayout.IntSlider(MapGenL.L("レイ数", "Rays"), s.shadowRayCount, 4, 128);
            s.shadowSpreadAngle = EditorGUILayout.Slider(MapGenL.L("拡散", "Spread"), s.shadowSpreadAngle, 0f, 30f);
            s.shadowIntensity = EditorGUILayout.Slider(MapGenL.L("強度", "Intensity"), s.shadowIntensity, 0f, 2f);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(MapGenL.L("コントロールマップ", "Control Map"), EditorStyles.boldLabel);
        s.generateControl = EditorGUILayout.Toggle(MapGenL.L("有効", "Enable"), s.generateControl);
        if (s.generateControl)
        {
            s.channelR = (ControlMapChannel)EditorGUILayout.EnumPopup(MapGenL.L("Rチャンネル", "R"), s.channelR);
            s.channelG = (ControlMapChannel)EditorGUILayout.EnumPopup(MapGenL.L("Gチャンネル", "G"), s.channelG);
            s.channelB = (ControlMapChannel)EditorGUILayout.EnumPopup(MapGenL.L("Bチャンネル", "B"), s.channelB);
            s.channelA = (ControlMapChannel)EditorGUILayout.EnumPopup(MapGenL.L("Aチャンネル", "A"), s.channelA);
        }

        EditorGUILayout.Space(10);

        // Generate button
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button(MapGenL.L("★ 全マップ生成", "★ Generate All Maps"), GUILayout.Height(35)))
        {
            Undo.RecordObject(_target, "Generate All Maps");
            MapGenResult result = MapGeneratorEngine.GenerateAll(_target);
            if (result != null)
            {
                EditorUtility.DisplayDialog("Map Generator",
                    MapGenL.L($"完了！ ({result.processingTimeMs:F0}ms)", $"Done! ({result.processingTimeMs:F0}ms)"), "OK");
                Repaint();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // ===== Right Panel: Preview =====
        EditorGUILayout.BeginVertical();
        _scrollRight = EditorGUILayout.BeginScrollView(_scrollRight);

        EditorGUILayout.LabelField(MapGenL.L("プレビュー", "Preview"), EditorStyles.boldLabel);
        _selectedPreview = GUILayout.Toolbar(_selectedPreview, _previewNames);

        Texture2D previewTex = GetPreviewTexture(_selectedPreview);
        if (previewTex != null)
        {
            float panelWidth = position.width * 0.55f - 20;
            float aspect = (float)previewTex.height / previewTex.width;
            float previewSize = Mathf.Min(panelWidth, 512);
            Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize * aspect);
            EditorGUI.DrawPreviewTexture(previewRect, previewTex);

            EditorGUILayout.LabelField($"{previewTex.width} x {previewTex.height}", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUILayout.HelpBox(MapGenL.L("プレビューがありません。先にマップを生成してください。", "No preview available. Generate maps first."), MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        if (GUI.changed && _target != null)
            EditorUtility.SetDirty(_target);
    }

    private Texture2D GetPreviewTexture(int index)
    {
        if (_target == null) return null;
        switch (index)
        {
            case 0: return _target.lastNormalMap;
            case 1: return _target.lastAOMap;
            case 2: return _target.lastCurvatureMap;
            case 3: return _target.lastRoughnessMap;
            case 4: return _target.lastShadowMap;
            case 5: return _target.lastControlMap;
            default: return null;
        }
    }
}

// =============================================================================
// MapGeneratorShortcuts — Context menus & one-click generation without component
// =============================================================================

public static class MapGeneratorShortcuts
{
    // ===== Renderer context menu (right-click on any Renderer component) =====

    [MenuItem("CONTEXT/Renderer/Generate All Maps (MapGenerator)")]
    private static void GenerateFromRenderer(MenuCommand command)
    {
        Renderer renderer = command.context as Renderer;
        if (renderer == null) return;
        RunOneShot(renderer.gameObject);
    }

    [MenuItem("CONTEXT/Renderer/Generate All Maps (MapGenerator)", true)]
    private static bool GenerateFromRendererValidate(MenuCommand command)
    {
        return command.context is Renderer;
    }

    // ===== MeshFilter context menu =====

    [MenuItem("CONTEXT/MeshFilter/Generate All Maps (MapGenerator)")]
    private static void GenerateFromMeshFilter(MenuCommand command)
    {
        MeshFilter mf = command.context as MeshFilter;
        if (mf == null) return;
        RunOneShot(mf.gameObject);
    }

    // ===== SkinnedMeshRenderer context menu =====

    [MenuItem("CONTEXT/SkinnedMeshRenderer/Generate All Maps (MapGenerator)")]
    private static void GenerateFromSkinned(MenuCommand command)
    {
        SkinnedMeshRenderer smr = command.context as SkinnedMeshRenderer;
        if (smr == null) return;
        RunOneShot(smr.gameObject);
    }

    // ===== Tools menu — works on selected GameObject =====

    [MenuItem("Tools/MapGenerator/Generate Maps from Selection")]
    private static void GenerateFromSelection()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Map Generator",
                MapGenL.L("シーン内のRendererを持つGameObjectを選択してください。", "Please select a GameObject with a Renderer in the Scene."), "OK");
            return;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
        {
            EditorUtility.DisplayDialog("Map Generator",
                MapGenL.L("選択されたGameObjectにRendererコンポーネントがありません。", "Selected GameObject does not have a Renderer component."), "OK");
            return;
        }

        RunOneShot(go);
    }

    [MenuItem("Tools/MapGenerator/Generate Maps from Selection", true)]
    private static bool GenerateFromSelectionValidate()
    {
        return Selection.activeGameObject != null
            && Selection.activeGameObject.GetComponent<Renderer>() != null;
    }

    // ===== GameObject menu (Hierarchy right-click) =====

    [MenuItem("GameObject/Generate All Maps (MapGenerator)", false, 49)]
    private static void GenerateFromHierarchy()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) return;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
        {
            EditorUtility.DisplayDialog("Map Generator",
                MapGenL.L("選択されたGameObjectにRendererコンポーネントがありません。", "Selected GameObject does not have a Renderer component."), "OK");
            return;
        }

        RunOneShot(go);
    }

    [MenuItem("GameObject/Generate All Maps (MapGenerator)", true, 49)]
    private static bool GenerateFromHierarchyValidate()
    {
        return Selection.activeGameObject != null
            && Selection.activeGameObject.GetComponent<Renderer>() != null;
    }

    // ===== Core: One-shot generation (auto-add component → generate → keep component) =====

    private static void RunOneShot(GameObject go)
    {
        // Get or add MapGenerator component
        MapGenerator gen = go.GetComponent<MapGenerator>();
        bool wasAdded = false;

        if (gen == null)
        {
            Undo.RecordObject(go, "MapGenerator: Add Component & Generate");
            gen = Undo.AddComponent<MapGenerator>(go);
            wasAdded = true;
        }

        // Ensure auto-detection runs
        gen.targetRenderer = go.GetComponent<Renderer>();
        gen.targetMesh = MapGenUtils.GetMesh(gen.targetRenderer);
        if (gen.albedoTexture == null && gen.targetRenderer.sharedMaterial != null)
        {
            if (gen.targetRenderer.sharedMaterial.HasProperty("_MainTex"))
                gen.albedoTexture = gen.targetRenderer.sharedMaterial.mainTexture as Texture2D;
        }

        // Validate
        if (gen.targetMesh == null)
        {
            EditorUtility.DisplayDialog("Map Generator", MapGenL.L("このオブジェクトにメッシュが見つかりません。", "No mesh found on this object."), "OK");
            if (wasAdded) Undo.PerformUndo();
            return;
        }

        if (gen.targetMesh.uv == null || gen.targetMesh.uv.Length == 0)
        {
            EditorUtility.DisplayDialog("Map Generator", MapGenL.L("メッシュにUVデータがありません。", "Mesh has no UV data."), "OK");
            if (wasAdded) Undo.PerformUndo();
            return;
        }

        // Generate
        Undo.RecordObject(gen, "MapGenerator: Generate All Maps");
        MapGenResult result = MapGeneratorEngine.GenerateAll(gen);

        if (result != null)
        {
            EditorUtility.DisplayDialog("Map Generator",
                MapGenL.L(
                    $"全マップの生成が完了しました！\n処理時間: {result.processingTimeMs:F0}ms\n出力先: {gen.settings.outputFolder}",
                    $"All maps generated successfully!\nTime: {result.processingTimeMs:F0}ms\nOutput: {gen.settings.outputFolder}"),
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Map Generator",
                MapGenL.L("生成に失敗またはキャンセルされました。Consoleを確認してください。", "Generation failed or was cancelled. Check Console for details."), "OK");
        }
    }
}

// =============================================================================
// MapGeneratorMaterialExtension — Inject map generation buttons into Material Inspector
// =============================================================================

[InitializeOnLoad]
public static class MapGeneratorMaterialExtension
{
    private static bool _foldout;
    private static bool _migrationFoldout;
    private static MapGenSettings _settings = new MapGenSettings();

    static MapGeneratorMaterialExtension()
    {
        Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
    }

    private static void OnPostHeaderGUI(Editor editor)
    {
        // Only for MaterialEditor
        if (!(editor is MaterialEditor)) return;
        Material mat = editor.target as Material;
        if (mat == null) return;

        // Shader type check: only show Map Generator for NataneToon, migration button for lilToon
        string shaderName = mat.shader != null ? mat.shader.name : "";
        bool isNatane = shaderName.StartsWith("Natane/", System.StringComparison.Ordinal);
        bool isLilToon = shaderName.IndexOf("lilToon", System.StringComparison.OrdinalIgnoreCase) >= 0
                      || shaderName.StartsWith("_lil/", System.StringComparison.OrdinalIgnoreCase);

        if (!isNatane)
        {
            if (isLilToon)
            {
                DrawLilToonMigrationButton(mat);
            }
            return;
        }

        // Find renderer + mesh for this material in scene
        Renderer renderer = FindRendererForMaterial(mat);
        Mesh mesh = renderer != null ? MapGenUtils.GetMesh(renderer) : null;
        Texture2D albedo = GetAlbedo(mat);
        bool hasMesh = mesh != null && mesh.uv != null && mesh.uv.Length > 0;

        // Draw foldout section
        EditorGUILayout.Space(2);
        _foldout = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout, MapGenL.L("☆ マップジェネレーター", "☆ Map Generator"));

        if (_foldout)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Info line
            string meshInfo = hasMesh ? $"{mesh.name} ({mesh.vertexCount} verts)" : MapGenL.L("シーンにメッシュなし", "No mesh in scene");
            string albedoInfo = albedo != null ? albedo.name : MapGenL.L("アルベドなし", "No albedo");
            EditorGUILayout.LabelField($"{MapGenL.L("メッシュ:", "Mesh:")} {meshInfo}  |  {MapGenL.L("アルベド:", "Albedo:")} {albedoInfo}", EditorStyles.miniLabel);

            EditorGUILayout.Space(3);

            // Resolution
            _settings.outputResolution = EditorGUILayout.Vector2IntField(MapGenL.L("解像度", "Resolution"), _settings.outputResolution);

            // Output folder
            EditorGUILayout.BeginHorizontal();
            _settings.outputFolder = EditorGUILayout.TextField(MapGenL.L("出力フォルダ", "Output Folder"), _settings.outputFolder);
            if (GUILayout.Button("...", GUILayout.Width(25)))
            {
                string selected = EditorUtility.OpenFolderPanel(MapGenL.L("出力フォルダ", "Output Folder"), "Assets", "");
                if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
                    _settings.outputFolder = "Assets" + selected.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();

            _settings.autoAssignToMaterial = EditorGUILayout.Toggle(MapGenL.L("自動割り当て", "Auto Assign"), _settings.autoAssignToMaterial);

            EditorGUILayout.Space(5);

            // ===== Generate All =====
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            EditorGUI.BeginDisabledGroup(!hasMesh && albedo == null);
            if (GUILayout.Button(MapGenL.L("★ 全マップ生成", "★ Generate All Maps"), GUILayout.Height(28)))
            {
                GenerateAllFromMaterial(mat, renderer, mesh, albedo);
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(3);

            // ===== Individual map buttons =====
            EditorGUILayout.LabelField(MapGenL.L("個別マップ", "Individual Maps"), EditorStyles.boldLabel);

            // Normal Map → _BumpMap + _UseNormalMap + _NORMALMAP
            EditorGUILayout.BeginHorizontal();
            _settings.normalStrength = EditorGUILayout.Slider(MapGenL.L("Normal 強度", "Normal Strength"), _settings.normalStrength, 0.1f, 10f);
            EditorGUI.BeginDisabledGroup(albedo == null && !hasMesh);
            if (GUILayout.Button(MapGenL.L("生成", "Generate"), GUILayout.Width(70), GUILayout.Height(18)))
            {
                Texture2D result = MapGeneratorEngine.GenerateNormalMap(albedo, _settings, mesh, renderer);
                SaveAndAssignSingleNatane(mat, result, "Normal", true,
                    "_BumpMap", "_UseNormalMap", "_NORMALMAP", "_USE_NORMAL_MAP");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // AO Map → _AOMap + _UseAO + _USE_AO
            EditorGUILayout.BeginHorizontal();
            _settings.aoRayCount = EditorGUILayout.IntSlider(MapGenL.L("AO レイ数", "AO Rays"), _settings.aoRayCount, 8, 256);
            EditorGUI.BeginDisabledGroup(!hasMesh);
            if (GUILayout.Button(MapGenL.L("生成", "Generate"), GUILayout.Width(70), GUILayout.Height(18)))
            {
                Texture2D result = MapGeneratorEngine.GenerateAOMap(mesh, renderer, _settings);
                SaveAndAssignSingleNatane(mat, result, "AO", false,
                    "_AOMap", "_UseAO", "_USE_AO");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Curvature → _CavityMap
            EditorGUILayout.BeginHorizontal();
            _settings.curvatureMultiplier = EditorGUILayout.Slider(MapGenL.L("曲率", "Curvature"), _settings.curvatureMultiplier, 0.1f, 5f);
            EditorGUI.BeginDisabledGroup(!hasMesh);
            if (GUILayout.Button(MapGenL.L("生成", "Generate"), GUILayout.Width(70), GUILayout.Height(18)))
            {
                Texture2D result = MapGeneratorEngine.GenerateCurvatureMap(mesh, _settings);
                SaveAndAssignSingleNatane(mat, result, "Curvature", false,
                    "_CavityMap");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Roughness → _RoughnessMap + _UseRoughnessMap + _USE_ROUGHNESS_MAP
            EditorGUILayout.BeginHorizontal();
            _settings.roughnessBaseline = EditorGUILayout.Slider(MapGenL.L("ラフネス", "Roughness"), _settings.roughnessBaseline, 0f, 1f);
            EditorGUI.BeginDisabledGroup(albedo == null);
            if (GUILayout.Button(MapGenL.L("生成", "Generate"), GUILayout.Width(70), GUILayout.Height(18)))
            {
                Texture2D result = MapGeneratorEngine.GenerateRoughnessMap(albedo, _settings);
                SaveAndAssignSingleNatane(mat, result, "Roughness", false,
                    "_RoughnessMap", "_UseRoughnessMap", "_USE_ROUGHNESS_MAP");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Shadow → _ShadowReceiveMask
            EditorGUILayout.BeginHorizontal();
            _settings.shadowLightDir = EditorGUILayout.Vector3Field(MapGenL.L("シャドウ方向", "Shadow Dir"), _settings.shadowLightDir);
            EditorGUI.BeginDisabledGroup(!hasMesh);
            if (GUILayout.Button(MapGenL.L("生成", "Generate"), GUILayout.Width(70), GUILayout.Height(18)))
            {
                Texture2D result = MapGeneratorEngine.GenerateShadowMap(mesh, renderer, _settings);
                SaveAndAssignSingleNatane(mat, result, "Shadow", false,
                    "_ShadowReceiveMask");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (!hasMesh)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox(
                    "メッシュベースのマップ (AO, Curvature, Shadow) はシーンにこのマテリアルを使用する Renderer が必要です。",
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ===== lilToon Migration Button =====

    private static void DrawLilToonMigrationButton(Material mat)
    {
        EditorGUILayout.Space(2);
        _migrationFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
            _migrationFoldout,
            MapGenL.L("🔀 NataneToon に移行", "🔀 Migrate to NataneToon"));

        if (_migrationFoldout)
        {
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1.0f, 0.8f, 0.5f, 0.3f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            EditorGUILayout.LabelField(
                MapGenL.L(
                    "このマテリアルは lilToon シェーダーを使用しています。\nNatane Toon Shader に移行できます。",
                    "This material uses lilToon shader.\nYou can migrate it to Natane Toon Shader."),
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(4);
            if (GUILayout.Button(
                MapGenL.L("lilToon 移行ツールを開く", "Open lilToon Migration Tool"),
                GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem(
                    "Tools/Natane/移行 Migration/lilToon Migration Tool");
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ===== Generate All from Material context =====

    private static void GenerateAllFromMaterial(Material mat, Renderer renderer, Mesh mesh, Texture2D albedo)
    {
        var sw = Stopwatch.StartNew();
        string folder = MapGeneratorEngine.EnsureFolderHierarchy(_settings.outputFolder);
        string baseName = SanitizeFileName(mat.name);

        try
        {
            int done = 0;
            int total = 6;

            // Normal
            EditorUtility.DisplayProgressBar("Map Generator", MapGenL.L("ノーマルマップ...", "Normal Map..."), (float)done / total);
            Texture2D normal = (albedo != null || mesh != null) ? MapGeneratorEngine.GenerateNormalMap(albedo, _settings, mesh, renderer) : null;
            if (normal != null) MapGeneratorEngine.SaveTexture(normal, folder, $"{baseName}_Normal", _settings.outputFormat, true);
            done++;

            // AO
            EditorUtility.DisplayProgressBar("Map Generator", MapGenL.L("AOマップ...", "AO Map..."), (float)done / total);
            Texture2D ao = mesh != null && renderer != null ? MapGeneratorEngine.GenerateAOMap(mesh, renderer, _settings) : null;
            if (ao != null) MapGeneratorEngine.SaveTexture(ao, folder, $"{baseName}_AO", _settings.outputFormat, false);
            done++;

            // Curvature
            EditorUtility.DisplayProgressBar("Map Generator", MapGenL.L("カーブチャーマップ...", "Curvature Map..."), (float)done / total);
            Texture2D curvature = mesh != null ? MapGeneratorEngine.GenerateCurvatureMap(mesh, _settings) : null;
            if (curvature != null) MapGeneratorEngine.SaveTexture(curvature, folder, $"{baseName}_Curvature", _settings.outputFormat, false);
            done++;

            // Roughness
            EditorUtility.DisplayProgressBar("Map Generator", MapGenL.L("ラフネスマップ...", "Roughness Map..."), (float)done / total);
            Texture2D roughness = albedo != null ? MapGeneratorEngine.GenerateRoughnessMap(albedo, _settings) : null;
            if (roughness != null) MapGeneratorEngine.SaveTexture(roughness, folder, $"{baseName}_Roughness", _settings.outputFormat, false);
            done++;

            // Shadow
            EditorUtility.DisplayProgressBar("Map Generator", MapGenL.L("シャドウマップ...", "Shadow Map..."), (float)done / total);
            Texture2D shadow = mesh != null && renderer != null ? MapGeneratorEngine.GenerateShadowMap(mesh, renderer, _settings) : null;
            if (shadow != null) MapGeneratorEngine.SaveTexture(shadow, folder, $"{baseName}_Shadow", _settings.outputFormat, false);
            done++;

            // Control
            EditorUtility.DisplayProgressBar("Map Generator", MapGenL.L("コントロールマップ...", "Control Map..."), (float)done / total);
            var sources = new MapGenResult { normal = normal, ao = ao, curvature = curvature, roughness = roughness, shadow = shadow };
            Texture2D control = MapGeneratorEngine.GenerateControlMap(_settings, sources);
            if (control != null) MapGeneratorEngine.SaveTexture(control, folder, $"{baseName}_Control", _settings.outputFormat, false);

            // Auto assign (NataneToon property names)
            if (_settings.autoAssignToMaterial)
            {
                Undo.RecordObject(mat, "MapGenerator: Assign Maps");

                // Normal → _BumpMap + _UseNormalMap + _NORMALMAP
                AssignIfExists(mat, normal, folder, baseName, "Normal",
                    "_BumpMap", "_UseNormalMap", "_NORMALMAP", "_USE_NORMAL_MAP");

                // AO → _AOMap + _UseAO + _USE_AO
                AssignIfExists(mat, ao, folder, baseName, "AO",
                    "_AOMap", "_UseAO", "_USE_AO");

                // Curvature → _CavityMap
                AssignIfExists(mat, curvature, folder, baseName, "Curvature",
                    "_CavityMap");

                // Shadow → _ShadowReceiveMask
                AssignIfExists(mat, shadow, folder, baseName, "Shadow",
                    "_ShadowReceiveMask");

                // Roughness → _RoughnessMap + _UseRoughnessMap + _USE_ROUGHNESS_MAP
                AssignIfExists(mat, roughness, folder, baseName, "Roughness",
                    "_RoughnessMap", "_UseRoughnessMap", "_USE_ROUGHNESS_MAP");

                EditorUtility.SetDirty(mat);
            }

            // Cleanup temp textures
            if (normal != null) UnityEngine.Object.DestroyImmediate(normal);
            if (ao != null) UnityEngine.Object.DestroyImmediate(ao);
            if (curvature != null) UnityEngine.Object.DestroyImmediate(curvature);
            if (roughness != null) UnityEngine.Object.DestroyImmediate(roughness);
            if (shadow != null) UnityEngine.Object.DestroyImmediate(shadow);
            if (control != null) UnityEngine.Object.DestroyImmediate(control);

            sw.Stop();
            EditorUtility.DisplayDialog("Map Generator",
                MapGenL.L(
                    $"全マップの生成が完了しました！\n処理時間: {sw.ElapsedMilliseconds}ms\n出力先: {folder}",
                    $"All maps generated!\nTime: {sw.ElapsedMilliseconds}ms\nOutput: {folder}"), "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ===== Save & assign a single map (NataneToon property names) =====

    private static void SaveAndAssignSingleNatane(Material mat, Texture2D tex, string suffix,
        bool isNormal, string texProp, string toggleFloat = null, params string[] keywords)
    {
        if (tex == null)
        {
            Debug.LogWarning($"[MapGenerator] {suffix} generation returned null.");
            return;
        }

        string folder = MapGeneratorEngine.EnsureFolderHierarchy(_settings.outputFolder);
        string baseName = SanitizeFileName(mat.name);
        string path = MapGeneratorEngine.SaveTexture(tex, folder, $"{baseName}_{suffix}",
            _settings.outputFormat, isNormal);

        if (path != null && _settings.autoAssignToMaterial && mat.HasProperty(texProp))
        {
            Texture2D saved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (saved != null)
            {
                Undo.RecordObject(mat, $"MapGenerator: Assign {suffix}");
                mat.SetTexture(texProp, saved);

                if (toggleFloat != null && mat.HasProperty(toggleFloat))
                    mat.SetFloat(toggleFloat, 1f);

                foreach (string kw in keywords)
                {
                    if (!string.IsNullOrEmpty(kw))
                        mat.EnableKeyword(kw);
                }

                EditorUtility.SetDirty(mat);
            }
        }

        UnityEngine.Object.DestroyImmediate(tex);
        Debug.Log($"[MapGenerator] {suffix} map saved to: {path}");
    }

    private static void AssignIfExists(Material mat, Texture2D tempTex,
        string folder, string baseName, string suffix,
        string texProp, string toggleFloat = null, params string[] keywords)
    {
        if (tempTex == null || !mat.HasProperty(texProp)) return;

        // Find saved asset in the folder
        string[] guids = AssetDatabase.FindAssets($"{baseName}_{suffix} t:Texture2D", new[] { folder });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Texture2D saved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (saved != null)
            {
                mat.SetTexture(texProp, saved);

                if (toggleFloat != null && mat.HasProperty(toggleFloat))
                    mat.SetFloat(toggleFloat, 1f);

                foreach (string kw in keywords)
                {
                    if (!string.IsNullOrEmpty(kw))
                        mat.EnableKeyword(kw);
                }
            }
        }
    }

    // ===== Helpers =====

    private static Renderer FindRendererForMaterial(Material mat)
    {
        if (mat == null) return null;
        var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
        foreach (var r in renderers)
        {
            if (r.sharedMaterials == null) continue;
            foreach (var m in r.sharedMaterials)
            {
                if (m == mat) return r;
            }
        }
        return null;
    }

    private static Texture2D GetAlbedo(Material mat)
    {
        if (mat == null) return null;
        if (mat.HasProperty("_MainTex"))
            return mat.GetTexture("_MainTex") as Texture2D;
        return null;
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return string.IsNullOrWhiteSpace(value) ? "Generated" : value;
    }
}
