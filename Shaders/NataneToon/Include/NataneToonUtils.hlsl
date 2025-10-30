#ifndef NATANE_TOON_UTILS_INCLUDED
#define NATANE_TOON_UTILS_INCLUDED

// Utility Functions

// Calculate MatCap UV coordinates from world normal
// MatCap uses view-space normals to create sphere-mapped effects
float2 CalculateMatCapUV(float3 worldNormal, float3 viewDir)
{
    float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
    float2 matcapUV = viewNormal.xy * 0.5 + 0.5;
    return matcapUV;
}

// RGB to HSV conversion
float3 RGBtoHSV(float3 rgb)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// HSV to RGB conversion
float3 HSVtoRGB(float3 hsv)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
}

// Apply Hue Shift to RGB color
// shift: 0-1 range (0 = no shift, 1 = full rotation)
float3 ApplyHueShift(float3 rgb, float shift)
{
    float3 hsv = RGBtoHSV(rgb);
    hsv.x = frac(hsv.x + shift); // Wrap hue around [0, 1]
    return HSVtoRGB(hsv);
}

// Calculate Dissolve alpha and edge glow
// returns: x = alpha (for clipping), y = edge glow intensity
float2 CalculateDissolve(float2 uv, float dissolveAmount, float edgeWidth)
{
    float dissolveNoise = tex2D(_DissolveTex, uv).r;

    // Calculate alpha for clipping
    float dissolveAlpha = dissolveNoise - dissolveAmount;

    // Calculate edge glow (peaks at the dissolve boundary)
    float edgeGlow = 0.0;
    if (dissolveAlpha > 0.0 && dissolveAlpha < edgeWidth)
    {
        edgeGlow = 1.0 - (dissolveAlpha / edgeWidth);
    }

    return float2(dissolveAlpha, edgeGlow);
}

// Parallax Occlusion Mapping
// Creates the illusion of depth by offsetting texture coordinates based on height map
// Returns adjusted UV coordinates
float2 ParallaxMapping(float2 uv, float3 viewDirTangent)
{
    // Calculate number of layers based on view angle
    // More layers when viewing at steep angles for better quality
    float numLayers = lerp(_ParallaxMaxSamples, _ParallaxMinSamples, abs(dot(float3(0, 0, 1), viewDirTangent)));

    // Calculate the size of each layer
    float layerDepth = 1.0 / numLayers;

    // Depth of current layer
    float currentLayerDepth = 0.0;

    // Calculate UV offset per layer
    float2 deltaUV = viewDirTangent.xy * _ParallaxScale / (viewDirTangent.z * numLayers);

    // Initial values
    float2 currentUV = uv;
    float currentDepthMapValue = tex2D(_ParallaxMap, currentUV).r;

    // Parallax Occlusion Mapping loop
    [loop]
    for (int i = 0; i < (int)numLayers && currentLayerDepth < currentDepthMapValue; i++)
    {
        // Shift UV along direction of view
        currentUV -= deltaUV;

        // Get depth value at current UV
        currentDepthMapValue = tex2D(_ParallaxMap, currentUV).r;

        // Get depth of next layer
        currentLayerDepth += layerDepth;
    }

    // Interpolation for smoother result (steep parallax mapping)
    float2 prevUV = currentUV + deltaUV;
    float afterDepth = currentDepthMapValue - currentLayerDepth;
    float beforeDepth = tex2D(_ParallaxMap, prevUV).r - currentLayerDepth + layerDepth;

    // Interpolation weight
    float weight = afterDepth / (afterDepth - beforeDepth);

    // Final UV coordinates
    float2 finalUV = lerp(currentUV, prevUV, weight);

    return finalUV;
}

// Calculate tangent space view direction for parallax mapping
// Requires world position, world tangent, world binormal, and world normal
float3 CalculateTangentViewDir(float3 worldPos, float3 worldTangent, float3 worldBinormal, float3 worldNormal)
{
    float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

    // Create TBN matrix (tangent, binormal, normal)
    float3x3 TBN = float3x3(worldTangent, worldBinormal, worldNormal);

    // Transform view direction to tangent space
    float3 tangentViewDir = mul(TBN, viewDir);

    return tangentViewDir;
}

#endif // NATANE_TOON_UTILS_INCLUDED
