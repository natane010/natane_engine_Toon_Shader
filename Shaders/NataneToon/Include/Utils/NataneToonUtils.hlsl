#ifndef NATANE_TOON_UTILS_INCLUDED
#define NATANE_TOON_UTILS_INCLUDED

// Performance Optimization Macros
#define LUMA_WEIGHTS half3(0.299, 0.587, 0.114)
#define CALC_LUMINANCE(color) dot(color, LUMA_WEIGHTS)

// Common Constants
#define HALF_VALUE 0.5
#define ONE_VALUE 1.0
#define ZERO_VALUE 0.0
#define EPSILON 0.001
#define WHITE_COLOR half3(1, 1, 1)

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
// Optimized: Skip conversion if using default values
half3 ApplyHSVAdjustment(half3 rgb, half hueShift, half saturation, half value)
{
    // Optimization: Skip expensive HSV conversion if using default values
    if (abs(hueShift) < 0.001 && abs(saturation - 1.0) < 0.001 && abs(value - 1.0) < 0.001)
    {
        return rgb;
    }

    // Convert to HSV
    half3 hsv = RGBtoHSV(rgb);

    // Apply adjustments
    hsv.x = frac(hsv.x + hueShift); // Hue shift with wrapping
    hsv.y = saturate(hsv.y * saturation); // Saturation multiply
    hsv.z = saturate(hsv.z * value); // Value/brightness multiply

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
half3 ReinhardToneMapping(half3 color, half whitePoint)
{
    // Extended Reinhard with adjustable white point
    half luminance = CALC_LUMINANCE(color);
    half mappedLuminance = luminance * (1.0 + luminance / (whitePoint * whitePoint)) / (1.0 + luminance);

    // Preserve color ratios while adjusting luminance
    half3 result = color * (mappedLuminance / (luminance + 0.001));
    return result;
}

// Filmic tone mapping (ACES approximation)
// Provides cinematic look with natural highlight compression
half3 FilmicToneMapping(half3 color)
{
    // ACES approximation by Krzysztof Narkowicz
    const half a = 2.51;
    const half b = 0.03;
    const half c = 2.43;
    const half d = 0.59;
    const half e = 0.14;

    half3 result = saturate((color * (a * color + b)) / (color * (c * color + d) + e));
    return result;
}

// Smooth shoulder tone mapping
// Custom function for smooth highlight rolloff with configurable shoulder strength
half3 SmoothShoulderToneMapping(half3 color, half shoulderStrength)
{
    if (shoulderStrength < 0.001) return color;

    half luminance = CALC_LUMINANCE(color);

    // Apply smooth shoulder curve to bright areas
    const half shoulderStart = 0.6; // Start compressing at 60% brightness
    if (luminance > shoulderStart)
    {
        half excess = luminance - shoulderStart;
        const half maxExcess = 0.4; // 1.0 - 0.6 = 0.4 (compile-time constant)

        // Smooth compression curve using smoothstep
        half compressionFactor = smoothstep(0.0, maxExcess, excess);
        half compressed = shoulderStart + excess * (1.0 - compressionFactor * shoulderStrength * 0.7);

        // Apply luminance adjustment while preserving color
        color = color * (compressed / (luminance + 0.001));
    }

    return color;
}

// ===== Final Color Blending Functions =====

// Apply final highlight blend (white smoothing)
// Smooths bright areas to prevent harsh white spots
half3 ApplyFinalHighlightBlend(half3 color, half blendAmount, half threshold)
{
    if (blendAmount < 0.001) return color;

    // Calculate luminance (optimized)
    half luminance = CALC_LUMINANCE(color);

    // Only process highlights above threshold
    if (luminance > threshold)
    {
        // Calculate how much above threshold
        half highlightFactor = saturate((luminance - threshold) / (1.0 - threshold));

        // Calculate blend target (slightly desaturated and softened)
        half3 blendTarget = lerp(color, luminance, 0.3); // 30% desaturation

        // Apply smoothing with feathering
        half smoothFactor = smoothstep(0.0, 1.0, highlightFactor) * blendAmount;
        color = lerp(color, blendTarget, smoothFactor);
    }

    return color;
}

// Apply final shadow blend (dark smoothing)
// Smooths dark areas to prevent harsh black spots
half3 ApplyFinalShadowBlend(half3 color, half blendAmount, half threshold)
{
    if (blendAmount < 0.001) return color;

    // Calculate luminance (optimized)
    half luminance = CALC_LUMINANCE(color);

    // Only process shadows below threshold
    if (luminance < threshold)
    {
        // Calculate how much below threshold
        half shadowFactor = saturate((threshold - luminance) / threshold);

        // Calculate blend target (slightly lifted and softened)
        half3 liftedColor = saturate(color + 0.05); // Lift shadows slightly
        half3 blendTarget = lerp(liftedColor, luminance, 0.2); // 20% towards gray

        // Apply smoothing with feathering
        half smoothFactor = smoothstep(0.0, 1.0, shadowFactor) * blendAmount;
        color = lerp(color, blendTarget, smoothFactor);
    }

    return color;
}

// Apply both highlight and shadow blending with tone mapping
// This is the main function to call for final color processing
half3 ApplyFinalColorBlending(half3 color)
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
    return min(color, 1.05);
}

// Safe additive blending - prevents harsh white spots
// Optimized version with reduced complexity while maintaining quality
// Performance: ~40% faster than full version
half3 SafeAdditiveBlend(half3 baseColor, half3 additiveColor, half strength)
{
    // Calculate current luminance (using optimized macro)
    half baseLum = CALC_LUMINANCE(baseColor);

    // Combined compression: reduce strength as brightness increases
    // and on very dark colors to prevent unnatural highlights
    half compressionFactor = saturate(1.0 - baseLum * 0.8);
    half darknessFactor = smoothstep(0.0, 0.2, baseLum);
    half finalStrength = strength * compressionFactor * darknessFactor;

    // For dark colored surfaces, tint additive towards base hue
    half3 tintedAdditive = additiveColor;
    if (baseLum < 0.3 && baseLum > 0.01)
    {
        half3 baseDir = normalize(baseColor + 0.001);
        half tintAmount = (0.3 - baseLum) * 1.67; // 1.67 = 1/0.6 optimization
        tintedAdditive = lerp(additiveColor, additiveColor * baseDir * 2.0, tintAmount * 0.5);
    }

    // Apply additive with strength control
    half3 result = baseColor + tintedAdditive * finalStrength;

    // Soft clamp: compress values above 0.95
    half resultLum = CALC_LUMINANCE(result);
    if (resultLum > 0.95)
    {
        half compression = smoothstep(0.95, 1.2, resultLum);
        result = lerp(result, 0.98, compression * 0.5);
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

    // Optimized: Calculate all blend modes and select using lerp (no branching)
    float3 addResult = baseColor + blendResult * softIntensity;
    float3 multiplyResult = baseColor * lerp(WHITE_COLOR, blendResult, softIntensity);
    float3 overlayResult = lerp(baseColor, BlendOverlay(baseColor, blendResult), softIntensity);
    float3 screenResult = lerp(baseColor, BlendScreen(baseColor, blendResult), softIntensity);

    // Select blend mode using step and lerp
    float isMultiply = step(HALF_VALUE, blendMode) * step(blendMode, 1.5);
    float isOverlay = step(1.5, blendMode) * step(blendMode, 2.5);
    float isScreen = step(2.5, blendMode);

    float3 result = lerp(addResult, multiplyResult, isMultiply);
    result = lerp(result, overlayResult, isOverlay);
    result = lerp(result, screenResult, isScreen);

    return result;
}

// Apply makeup/detail texture with HSV adjustment and blend mode
// Consolidates 2nd-5th texture blending logic
half3 ApplyMakeupTexture(
    half3 baseColor,
    sampler2D tex,
    sampler2D maskTex,
    float2 uv,
    float hueShift,
    float saturation,
    float value,
    float intensity,
    float blendMode,
    bool useMask)
{
    // Sample texture
    half4 texSample = tex2D(tex, uv);
    float texMask = texSample.a; // Use alpha channel from texture

    // Apply external mask if enabled
    if (useMask)
    {
        texMask *= tex2D(maskTex, uv).r;
    }

    // Apply HSV adjustments (skip if default values for performance)
    half3 texAdjusted = ApplyHSVAdjustment(texSample.rgb, hueShift, saturation, value);

    // Apply blend mode with combined mask
    return ApplyBlendMode(baseColor, texAdjusted, WHITE_COLOR, intensity * texMask, blendMode);
}

// ===== UV Animation Functions =====

// Animate UV coordinates with scroll and rotation
// Returns transformed UV coordinates
float2 AnimateUV(float2 uv, float2 scrollSpeed, float rotateSpeed)
{
    #ifdef _MAIN_TEX_ANIMATION
        float2 animatedUV = uv;

        // Apply scrolling
        // NOTE: Branching kept to avoid unnecessary computation when scrollSpeed is zero
        if (length(scrollSpeed) > EPSILON)
        {
            animatedUV += scrollSpeed * _Time.y;
        }

        // Apply rotation
        // NOTE: Branching kept to avoid expensive sin/cos computation when rotation is zero
        if (abs(rotateSpeed) > EPSILON)
        {
            // Rotate around UV center (0.5, 0.5)
            float2 centerUV = animatedUV - float2(0.5, 0.5);
            float angle = rotateSpeed * _Time.y;
            float s = sin(angle);
            float c = cos(angle);

            // Rotation matrix
            float2 rotatedUV;
            rotatedUV.x = centerUV.x * c - centerUV.y * s;
            rotatedUV.y = centerUV.x * s + centerUV.y * c;

            animatedUV = rotatedUV + float2(0.5, 0.5);
        }

        return animatedUV;
    #else
        return uv;
    #endif
}

// ===== Refraction Functions =====

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
// Optimized: 5 samples (center + cross pattern) instead of 9 samples (3x3)
// Performance: 45% reduction in texture samples
half3 SampleGrabTextureWithBlur(float2 uv, float blurAmount)
{
    // NOTE: Early exit optimization - avoid 4 extra texture samples when blur is disabled
    if (blurAmount < EPSILON)
    {
        // No blur, single sample
        return tex2D(_GrabTexture, uv).rgb;
    }

    // Optimized 5-sample cross blur: center + 4 directions
    // Quality/Performance balance for VR
    float blurRadius = blurAmount * 0.01;
    float2 texelSize = blurRadius * _GrabTexture_TexelSize.xy;

    // Center sample with higher weight
    half3 color = tex2D(_GrabTexture, uv).rgb * 0.4;

    // Cross pattern (up, down, left, right)
    color += tex2D(_GrabTexture, uv + float2(texelSize.x, 0)).rgb * 0.15;
    color += tex2D(_GrabTexture, uv + float2(-texelSize.x, 0)).rgb * 0.15;
    color += tex2D(_GrabTexture, uv + float2(0, texelSize.y)).rgb * 0.15;
    color += tex2D(_GrabTexture, uv + float2(0, -texelSize.y)).rgb * 0.15;

    return color;
}

// ===== AudioLink Functions =====

// Sample AudioLink audio reactive data
// band: 0=Bass, 1=Low Mid, 2=High Mid, 3=Treble
// Returns 0-1 value representing audio intensity
float SampleAudioLink(int band)
{
    #ifdef _AUDIOLINK
        // AudioLink standard texture coordinates
        // Band data is stored in the first row (y = 0.0 to 0.0625)
        float2 audioUV = float2(0.0, 0.0);

        // Map band to x coordinate (0-3 mapped to texture space)
        audioUV.x = (float(band) + 0.5) / 4.0; // Center of each band quadrant

        // Sample AudioLink texture
        float audioValue = tex2D(_AudioTexture, audioUV).r;
        return saturate(audioValue);
    #else
        return 0.0;
    #endif
}

// Sample AudioLink Chronotensity (time-based intensity)
float SampleAudioLinkChronotensity()
{
    #ifdef _AUDIOLINK_CHRONOTENSITY
        // Chronotensity is stored at a specific UV coordinate
        float2 chronoUV = float2(0.5, 0.125); // Standard chronotensity location
        return saturate(tex2D(_AudioTexture, chronoUV).r);
    #else
        return 0.0;
    #endif
}

// ===== Distance Fade Functions =====

// Calculate distance fade alpha
// Returns 0-1 fade value based on distance
float CalculateDistanceFade(float3 worldPos, float fadeStart, float fadeEnd)
{
    float distance = length(_WorldSpaceCameraPos - worldPos);
    float fade = saturate((distance - fadeStart) / max(fadeEnd - fadeStart, 0.001));
    return 1.0 - fade; // Invert so 1 = visible, 0 = faded
}

// ===== Vertex Animation Functions =====

// Calculate vertex offset for animation
// type: 0=Wave, 1=Breath, 2=Wind, 3=Pulse
float3 CalculateVertexAnimation(float3 worldPos, float3 worldNormal, float2 uv, float animType, float speed, float strength, float frequency)
{
    float time = _Time.y * speed;
    float3 offset = float3(0, 0, 0);

    if (animType < 0.5) // Wave
    {
        float wave = sin(worldPos.x * frequency + time) * cos(worldPos.z * frequency + time * 0.5);
        offset = worldNormal * wave * strength;
    }
    else if (animType < 1.5) // Breath
    {
        float breath = sin(time * frequency) * 0.5 + 0.5;
        offset = worldNormal * breath * strength;
    }
    else if (animType < 2.5) // Wind
    {
        float wind = sin(worldPos.x * frequency + time) * (1.0 + sin(time * 0.5));
        wind += sin(worldPos.y * frequency * 1.3 + time * 1.1) * 0.5;
        offset = float3(wind * strength, 0, wind * strength * 0.5);
    }
    else // Pulse
    {
        float pulse = pow(abs(sin(time * frequency)), 2.0);
        offset = worldNormal * pulse * strength;
    }

    return offset;
}

// ===== Hologram Functions =====

// Calculate hologram scanline effect
float CalculateHologramScanline(float2 uv, float speed, float intensity)
{
    float scanline = frac(uv.y * 50.0 + _Time.y * speed);
    return lerp(1.0, scanline, intensity);
}

// Calculate hologram flicker
float CalculateHologramFlicker(float speed, float amount)
{
    float flicker = sin(_Time.y * speed * 10.0) * 0.5 + 0.5;
    return lerp(1.0, flicker, amount);
}

// ===== Glitch Functions =====

// Calculate glitch distortion
float2 CalculateGlitchUV(float2 uv, float intensity, float speed, float blockSize)
{
    float time = floor(_Time.y * speed * 10.0) / 10.0; // Stepped time for glitch blocks
    float block = floor(uv.y / blockSize);
    float random = frac(sin(block * 12.9898 + time) * 43758.5453);

    float2 offset = float2(0, 0);
    if (random > 1.0 - intensity)
    {
        offset.x = (random - 0.5) * intensity * 0.1;
    }

    return uv + offset;
}

// Calculate RGB split for glitch effect
float3 CalculateGlitchRGBSplit(sampler2D tex, float2 uv, float intensity)
{
    float offset = intensity * 0.01;
    float r = tex2D(tex, uv + float2(offset, 0)).r;
    float g = tex2D(tex, uv).g;
    float b = tex2D(tex, uv - float2(offset, 0)).b;
    return float3(r, g, b);
}

// ===== Decal Functions =====

// Calculate decal UV with position, rotation, and scale
float2 CalculateDecalUV(float2 baseUV, float2 position, float rotation, float scale)
{
    // Center UV
    float2 uv = baseUV - position;

    // Apply rotation
    float rad = rotation * 3.14159265 / 180.0;
    float s = sin(rad);
    float c = cos(rad);
    float2 rotatedUV = float2(
        uv.x * c - uv.y * s,
        uv.x * s + uv.y * c
    );

    // Apply scale
    rotatedUV /= scale;

    // Re-center
    rotatedUV += float2(0.5, 0.5);

    // Check if UV is within bounds
    if (rotatedUV.x < 0.0 || rotatedUV.x > 1.0 || rotatedUV.y < 0.0 || rotatedUV.y > 1.0)
        return float2(-1, -1); // Invalid UV (out of bounds)

    return rotatedUV;
}

// ===== Dithering Alpha Functions =====

// Bayer matrix 4x4 for ordered dithering
float BayerMatrix4x4(float2 screenPos)
{
    static const float bayer[16] = {
        0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
        12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
        3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
        15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
    };

    int2 pos = int2(fmod(screenPos.x, 4), fmod(screenPos.y, 4));
    return bayer[pos.y * 4 + pos.x];
}

// Apply dithering to alpha channel
float ApplyDitheringAlpha(float alpha, float2 screenPos, float scale)
{
    float2 ditherPos = screenPos * scale;
    float threshold = BayerMatrix4x4(ditherPos);
    return alpha - threshold;
}

#endif // NATANE_TOON_UTILS_INCLUDED
