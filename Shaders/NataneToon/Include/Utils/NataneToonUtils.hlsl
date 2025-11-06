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

// Apply HSV adjustments to RGB color
// hueShift: -0.5 to 0.5 range (0 = no shift)
// saturation: 0-2 range (1 = no change, 0 = grayscale, 2 = double saturation)
// value: 0-2 range (1 = no change, 0 = black, 2 = double brightness)
float3 ApplyHSVAdjustment(float3 rgb, float hueShift, float saturation, float value)
{
    // Convert to HSV
    float3 hsv = RGBtoHSV(rgb);

    // Apply adjustments
    hsv.x = frac(hsv.x + hueShift); // Hue shift with wrapping
    hsv.y *= saturation;             // Saturation multiply
    hsv.z *= value;                  // Value/brightness multiply

    // Clamp saturation and value to valid ranges
    hsv.y = saturate(hsv.y);
    hsv.z = saturate(hsv.z);

    // Convert back to RGB
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

// ===== Mask Smoothing Functions =====

// Smooth mask transition for better blending
// Applies feathering to mask edges to create smooth transitions
// featherAmount: 0-1 range, controls the amount of edge softening (default: 0.1)
float SmoothMask(float mask, float featherAmount)
{
    // Apply smoothstep for smoother transitions
    // The feather creates a gradient zone around the mask edges
    float feather = saturate(featherAmount * 0.5); // Scale to reasonable range
    return smoothstep(feather, 1.0 - feather, mask);
}

// Apply mask with automatic edge smoothing
// Creates natural transitions between masked and unmasked areas
float ApplySoftMask(float mask)
{
    // Default feather amount for smooth transitions
    const float DEFAULT_FEATHER = 0.15; // 15% edge softening
    return SmoothMask(mask, DEFAULT_FEATHER);
}

// Blend with soft edges (for effect application)
// Blends base and effect colors with smooth mask transition
float3 BlendWithSoftMask(float3 baseColor, float3 effectColor, float mask)
{
    // Apply soft masking for smooth transitions
    float softMask = ApplySoftMask(mask);
    return lerp(baseColor, effectColor, softMask);
}

// ===== Tone Mapping Functions =====

// Reinhard tone mapping - smooth compression of bright values
// Provides natural rolloff for highlights without hard clipping
float3 ReinhardToneMapping(float3 color, float whitePoint)
{
    // Extended Reinhard with adjustable white point
    float luminance = dot(color, float3(0.299, 0.587, 0.114));
    float mappedLuminance = luminance * (1.0 + luminance / (whitePoint * whitePoint)) / (1.0 + luminance);

    // Preserve color ratios while adjusting luminance
    float3 result = color * (mappedLuminance / (luminance + 0.001));
    return result;
}

// Filmic tone mapping (ACES approximation)
// Provides cinematic look with natural highlight compression
float3 FilmicToneMapping(float3 color)
{
    // ACES approximation by Krzysztof Narkowicz
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;

    float3 result = saturate((color * (a * color + b)) / (color * (c * color + d) + e));
    return result;
}

// Smooth shoulder tone mapping
// Custom function for smooth highlight rolloff with configurable shoulder strength
float3 SmoothShoulderToneMapping(float3 color, float shoulderStrength)
{
    if (shoulderStrength < 0.001) return color;

    float luminance = dot(color, float3(0.299, 0.587, 0.114));

    // Apply smooth shoulder curve to bright areas
    float shoulderStart = 0.6; // Start compressing at 60% brightness
    if (luminance > shoulderStart)
    {
        float excess = luminance - shoulderStart;
        float maxExcess = 1.0 - shoulderStart;

        // Smooth compression curve using smoothstep
        float compressionFactor = smoothstep(0.0, maxExcess, excess);
        float compressed = shoulderStart + excess * (1.0 - compressionFactor * shoulderStrength * 0.7);

        // Apply luminance adjustment while preserving color
        color = color * (compressed / (luminance + 0.001));
    }

    return color;
}

// ===== Final Color Blending Functions =====

// Apply final highlight blend (white smoothing)
// Smooths bright areas to prevent harsh white spots
float3 ApplyFinalHighlightBlend(float3 color, float blendAmount, float threshold)
{
    if (blendAmount < 0.001) return color;

    // Calculate luminance
    float luminance = dot(color, float3(0.299, 0.587, 0.114));

    // Only process highlights above threshold
    if (luminance > threshold)
    {
        // Calculate how much above threshold
        float highlightFactor = (luminance - threshold) / (1.0 - threshold);
        highlightFactor = saturate(highlightFactor);

        // Calculate blend target (slightly desaturated and softened)
        float3 averageColor = float3(luminance, luminance, luminance);
        float3 blendTarget = lerp(color, averageColor, 0.3); // 30% desaturation

        // Apply smoothing with feathering
        float smoothFactor = smoothstep(0.0, 1.0, highlightFactor) * blendAmount;
        color = lerp(color, blendTarget, smoothFactor);
    }

    return color;
}

// Apply final shadow blend (dark smoothing)
// Smooths dark areas to prevent harsh black spots
float3 ApplyFinalShadowBlend(float3 color, float blendAmount, float threshold)
{
    if (blendAmount < 0.001) return color;

    // Calculate luminance
    float luminance = dot(color, float3(0.299, 0.587, 0.114));

    // Only process shadows below threshold
    if (luminance < threshold)
    {
        // Calculate how much below threshold
        float shadowFactor = (threshold - luminance) / threshold;
        shadowFactor = saturate(shadowFactor);

        // Calculate blend target (slightly lifted and softened)
        float3 liftedColor = color + float3(0.05, 0.05, 0.05); // Lift shadows slightly
        liftedColor = saturate(liftedColor);

        // Calculate average for softening
        float3 averageColor = float3(luminance, luminance, luminance);
        float3 blendTarget = lerp(liftedColor, averageColor, 0.2); // 20% towards gray

        // Apply smoothing with feathering
        float smoothFactor = smoothstep(0.0, 1.0, shadowFactor) * blendAmount;
        color = lerp(color, blendTarget, smoothFactor);
    }

    return color;
}

// Apply both highlight and shadow blending with tone mapping
// This is the main function to call for final color processing
float3 ApplyFinalColorBlending(float3 color)
{
    // Step 1: Apply smooth shoulder tone mapping for natural highlight compression
    // This prevents harsh white clipping while maintaining color vibrancy
    if (_FinalHighlightBlend > 0.001)
    {
        color = SmoothShoulderToneMapping(color, _FinalHighlightBlend);
    }

    // Step 2: Apply highlight blend for additional smoothing
    // This further softens bright areas above threshold
    color = ApplyFinalHighlightBlend(color, _FinalHighlightBlend * 0.5, _HighlightThreshold);

    // Step 3: Apply shadow blend to prevent black crushing
    color = ApplyFinalShadowBlend(color, _FinalShadowBlend, _ShadowThreshold);

    // Step 4: Gentle clamp to prevent hard cutoff - preserve values slightly above 1.0
    // This allows some headroom for natural brightness while preventing extreme values
    return min(color, float3(1.05, 1.05, 1.05));
}

// Safe additive blending - prevents harsh white spots
// Uses smooth compression for values approaching 1.0
// Also reduces strength on dark colors to prevent unnatural white highlights on black
float3 SafeAdditiveBlend(float3 baseColor, float3 additiveColor, float strength)
{
    // Calculate current luminance
    float baseLuminance = dot(baseColor, float3(0.299, 0.587, 0.114));

    // Reduce additive strength as base gets brighter (prevent white-out)
    float compressionFactor = 1.0 - smoothstep(0.6, 0.95, baseLuminance);

    // NEW: Also reduce strength on very dark colors (prevent white highlights on black)
    // Dark colors should receive darker highlights
    float darknessFactor = smoothstep(0.0, 0.2, baseLuminance);

    // Combine both factors
    float finalStrength = strength * compressionFactor * darknessFactor;

    // For dark base colors, tint the additive color towards the base color hue
    // This makes highlights feel more natural on colored surfaces
    float3 tintedAdditive = additiveColor;
    if (baseLuminance < 0.3 && baseLuminance > 0.01)
    {
        // Extract base color direction (hue/saturation)
        float3 baseDirection = normalize(baseColor + float3(0.001, 0.001, 0.001));
        // Tint the additive color with the base color direction
        float tintAmount = (0.3 - baseLuminance) / 0.3; // More tint for darker colors
        tintedAdditive = lerp(additiveColor, additiveColor * baseDirection * 2.0, tintAmount * 0.5);
    }

    // Apply compressed additive with strength control
    float3 result = baseColor + (tintedAdditive * finalStrength);

    // Soft clamp using smoothstep compression instead of hard saturate
    float resultLuminance = dot(result, float3(0.299, 0.587, 0.114));
    if (resultLuminance > 0.95)
    {
        // Compress values above 0.95 smoothly
        float compression = smoothstep(0.95, 1.2, resultLuminance);
        result = lerp(result, float3(0.98, 0.98, 0.98), compression * 0.5);
    }

    return result;
}

// ===== Blend Mode Functions for Makeup Textures =====
// Returns blended color based on blend mode

// Overlay blend mode
float3 BlendOverlay(float3 base, float3 blend)
{
    float3 result;
    result.r = base.r < 0.5 ? (2.0 * base.r * blend.r) : (1.0 - 2.0 * (1.0 - base.r) * (1.0 - blend.r));
    result.g = base.g < 0.5 ? (2.0 * base.g * blend.g) : (1.0 - 2.0 * (1.0 - base.g) * (1.0 - blend.g));
    result.b = base.b < 0.5 ? (2.0 * base.b * blend.b) : (1.0 - 2.0 * (1.0 - base.b) * (1.0 - blend.b));
    return result;
}

// Screen blend mode
float3 BlendScreen(float3 base, float3 blend)
{
    return 1.0 - (1.0 - base) * (1.0 - blend);
}

// Apply blend mode to base color with blend texture (with soft masking)
// blendMode: 0=Add, 1=Multiply, 2=Overlay, 3=Screen
// intensity: blend strength (already includes mask value)
float3 ApplyBlendMode(float3 baseColor, float3 blendTexture, float3 blendColor, float intensity, float blendMode)
{
    // Apply soft masking to intensity for smoother transitions
    float softIntensity = ApplySoftMask(intensity);

    float3 blendResult = blendTexture * blendColor;
    float3 result = baseColor;

    if (blendMode < 0.5) // Add
    {
        result = baseColor + blendResult * softIntensity;
    }
    else if (blendMode < 1.5) // Multiply
    {
        result = baseColor * lerp(float3(1, 1, 1), blendResult, softIntensity);
    }
    else if (blendMode < 2.5) // Overlay
    {
        result = lerp(baseColor, BlendOverlay(baseColor, blendResult), softIntensity);
    }
    else // Screen (>= 2.5)
    {
        result = lerp(baseColor, BlendScreen(baseColor, blendResult), softIntensity);
    }

    return result;
}

// ===== Refraction Functions =====

// Calculate screen position for GrabPass sampling
// Returns screen-space UV coordinates for the current pixel
float4 ComputeGrabScreenPos(float4 pos)
{
    #if UNITY_UV_STARTS_AT_TOP
        float scale = -1.0;
    #else
        float scale = 1.0;
    #endif

    float4 o = pos * 0.5;
    o.xy = float2(o.x, o.y * scale) + o.w;
    o.zw = pos.zw;
    return o;
}

// Apply refraction distortion to screen UV
// Returns distorted UV for sampling GrabTexture
float2 ApplyRefractionDistortion(float2 screenUV, float3 worldNormal, float3 viewDir, float intensity, float refractionIndex, float blur)
{
    // Calculate refraction using Snell's law
    // IOR ratio: from air (1.0) to material (refractionIndex)
    float iorRatio = 1.0 / refractionIndex;

    // Refract the view direction through the surface
    float3 refractDir = refract(-viewDir, worldNormal, iorRatio);

    // If total internal reflection occurs, use reflection instead
    if (length(refractDir) < 0.01)
    {
        refractDir = reflect(-viewDir, worldNormal);
    }

    // Calculate distortion offset based on refracted direction
    // Project refraction direction to screen space
    float2 distortion = refractDir.xy * intensity * 0.1;

    // Apply distortion to UV
    float2 distortedUV = screenUV + distortion;

    // Keep UV in valid range [0, 1]
    distortedUV = saturate(distortedUV);

    return distortedUV;
}

// Sample GrabTexture with optional blur
// Blur is approximated using multiple samples
float3 SampleGrabTextureWithBlur(float2 uv, float blurAmount)
{
    if (blurAmount < 0.01)
    {
        // No blur, single sample
        return tex2D(_GrabTexture, uv).rgb;
    }

    // Simple box blur with 9 samples
    float3 color = float3(0, 0, 0);
    float blurRadius = blurAmount * 0.01; // Scale blur amount

    // 3x3 kernel
    float weight = 1.0 / 9.0;
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            float2 offset = float2(x, y) * blurRadius * _GrabTexture_TexelSize.xy;
            color += tex2D(_GrabTexture, uv + offset).rgb * weight;
        }
    }

    return color;
}

#endif // NATANE_TOON_UTILS_INCLUDED
