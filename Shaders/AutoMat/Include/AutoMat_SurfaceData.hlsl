// AutoMat_SurfaceData.hlsl
// Surface data structure and initialization (sampling, POM, Roughness→Smoothness)

#ifndef AUTOMAT_SURFACEDATA_INCLUDED
#define AUTOMAT_SURFACEDATA_INCLUDED

struct AutoMatSurfaceData
{
    half3 albedo;
    half metallic;
    half smoothness;
    half3 normal;        // tangent-space
    half occlusion;
    half3 emission;
};

// ===== Parallax Occlusion Mapping =====
#ifdef _HEIGHTMAP
float2 ParallaxOcclusionMapping(float2 uv, float3 viewDirTS)
{
    float stepCount = _HeightSteps;
    float layerDepth = 1.0 / stepCount;
    float currentDepth = 0.0;

    float2 deltaUV = viewDirTS.xy / max(viewDirTS.z, 0.001) * _HeightScale / stepCount;

    float2 currentUV = uv;
    float currentHeight = tex2Dlod(_HeightMap, float4(currentUV, 0, 0)).r;

    [loop]
    for (int i = 0; i < (int)stepCount; i++)
    {
        if (currentDepth >= currentHeight)
            break;
        currentUV -= deltaUV;
        currentHeight = tex2Dlod(_HeightMap, float4(currentUV, 0, 0)).r;
        currentDepth += layerDepth;
    }

    // Interpolate between previous and current
    float2 prevUV = currentUV + deltaUV;
    float afterDepth = currentHeight - currentDepth;
    float beforeDepth = tex2Dlod(_HeightMap, float4(prevUV, 0, 0)).r - (currentDepth - layerDepth);
    float weight = afterDepth / max(abs(afterDepth - beforeDepth), 0.0001);
    return lerp(currentUV, prevUV, weight);
}
#endif

// ===== Surface initialization =====
AutoMatSurfaceData InitAutoMatSurface(float2 uv, float3 viewDirTS)
{
    AutoMatSurfaceData s;

    // POM UV offset
    #ifdef _HEIGHTMAP
    uv = ParallaxOcclusionMapping(uv, viewDirTS);
    #endif

    // Albedo
    half4 baseCol = tex2D(_BaseMap, uv) * _BaseColor;
    s.albedo = baseCol.rgb;

    // Metallic
    #ifdef _METALLIC_MAP
    s.metallic = tex2D(_MetallicMap, uv).r * _Metallic;
    #else
    s.metallic = _Metallic;
    #endif

    // Smoothness (from Roughness or Smoothness map)
    #ifdef _USE_ROUGHNESS_MAP
    half roughness = tex2D(_RoughnessMap, uv).r;
    s.smoothness = 1.0 - roughness;
    #elif defined(_SMOOTHNESS_MAP)
    s.smoothness = tex2D(_SmoothnessMap, uv).r * _Smoothness;
    #else
    s.smoothness = _Smoothness;
    #endif

    // Normal
    #ifdef _NORMALMAP
    half4 normalSample = tex2D(_BumpMap, uv);
    s.normal = UnpackScaleNormal(normalSample, _BumpScale);
    #else
    s.normal = half3(0, 0, 1);
    #endif

    // Occlusion
    #ifdef _OCCLUSION_MAP
    s.occlusion = lerp(1.0, tex2D(_OcclusionMap, uv).r, _OcclusionStrength);
    #else
    s.occlusion = 1.0;
    #endif

    // Emission
    #ifdef _EMISSION
    s.emission = tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;
    #else
    s.emission = 0;
    #endif

    return s;
}

#endif // AUTOMAT_SURFACEDATA_INCLUDED
