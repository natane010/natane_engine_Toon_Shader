#ifndef NATANE_TOON_LIGHTING_INCLUDED
#define NATANE_TOON_LIGHTING_INCLUDED

#include "NataneToonThirdPartyLighting.hlsl"

// Lighting Calculation Functions

// lilToon-compatible toon shading (linear interpolation, NOT smoothstep)
// lilTooningScale: saturate((value - borderMin) / (borderMax - borderMin))
// Used by StandardToon mode (_ShadingMode = 2) for exact lilToon parity
// v2: fwidth-based anti-aliasing added (lilToon LIL_ANTIALIAS_MODE != 0)
#ifdef _STANDARD_TOON
float LilToonShading(float value, float border, float blur)
{
    float borderMin = saturate(border - blur * 0.5);
    float borderMax = saturate(border + blur * 0.5);
    // fwidth-based AA for smoother shadow boundary (matches lilToon's antialias mode)
    float aa = fwidth(value) * 0.5;
    return saturate((value - borderMin) / max(borderMax - borderMin + aa, 0.0001));
}

// lilToon-compatible color blend function
// Supports 4 blend modes: 0=Normal(Replace), 1=Add, 2=Screen, 3=Multiply
half3 LilBlendColor(half3 dstCol, half3 srcCol, half srcA, uint blendMode)
{
    half3 ad = dstCol + srcCol;
    half3 mu = dstCol * srcCol;
    half3 outCol = srcCol;                                              // 0: Normal (Replace)
    outCol = (blendMode == 1) ? ad : outCol;                           // 1: Add
    outCol = (blendMode == 2) ? max(ad - mu, dstCol) : outCol;        // 2: Screen
    outCol = (blendMode == 3) ? mu : outCol;                           // 3: Multiply
    return lerp(dstCol, outCol, srcA);
}
#endif

// Toon Shading with adjustable steps and sharpness
// Creates cel-shaded stepped lighting effect
float ToonShading(float ndotl, float steps, float sharpness)
{
    // Apply shadow offset to adjust shadow threshold (clamped to prevent extreme values)
    ndotl = saturate(ndotl + clamp(_ShadowOffset, -1.0, 1.0));

    // Apply shadow blend (softness) if enabled
    #ifdef _SOFT_LIGHTING_MODE
        sharpness = lerp(sharpness, sharpness * 2.0, _SoftLightingIntensity);
    #endif

    // Apply shadow blend parameter for individual softness control
    if (_ShadowBlend > 0.001)
    {
        sharpness = lerp(sharpness, sharpness * (1.0 + _ShadowBlend * 2.0), _ShadowBlend);
    }

    // For anime-style clean shadows, use a sharper threshold approach
    // This creates more distinct light/shadow boundaries
    float stepValue = 1.0 / max(steps, 1.0);

    // Calculate which step we're on
    float currentStep = floor(ndotl * steps);

    // Calculate position within the current step
    float stepPosition = frac(ndotl * steps);

    // Apply anti-aliasing to prevent harsh pixelation while maintaining sharpness
    // Use smaller smoothstep range for cleaner anime look
    // _StepBorderSmooth を追加して段階境界のなじませ幅を拡張
    float smoothRange = saturate(sharpness + _StepBorderSmooth) * 0.5;
    float smoothedStep = smoothstep(0.5 - smoothRange, 0.5 + smoothRange, stepPosition);

    // Combine for final toon value with better precision
    float toon = (currentStep + smoothedStep) * stepValue;

    return saturate(toon);
}

// Gradient Shading
// Creates smooth gradient lighting effect for softer appearance
float GradientShading(float ndotl, float gradientWidth)
{
    // Apply shadow offset to adjust shadow threshold (clamped to prevent extreme values)
    ndotl = saturate(ndotl + clamp(_ShadowOffset, -1.0, 1.0));

    // Apply soft lighting mode to make gradient even softer
    #ifdef _SOFT_LIGHTING_MODE
        gradientWidth = lerp(gradientWidth, gradientWidth * 1.5, _SoftLightingIntensity);
    #endif

    // Apply shadow blend for additional softness control
    if (_ShadowBlend > 0.001)
    {
        gradientWidth = lerp(gradientWidth, gradientWidth * (1.0 + _ShadowBlend), _ShadowBlend);
    }

    // Calculate shadow boundary position (0.5 is the default lit/shadow boundary)
    float shadowBoundary = 0.5;

    // Create smooth gradient using smoothstep
    // gradientWidth controls the softness of the transition
    float halfWidth = gradientWidth * 0.5;
    float gradient = smoothstep(shadowBoundary - halfWidth, shadowBoundary + halfWidth, ndotl);

    return saturate(gradient);
}

// Toon-style vertex light calculation at pixel precision
// Provides higher quality per-pixel toon shading for vertex lights (ForwardBase 4 point lights)
// while keeping a clear directional falloff so point lights do not behave like a flat color wash.
half3 CalculateVertexLightsPixelPrecision(
    float3 worldPos,
    half3 worldNormal,
    float shadowSteps,
    float shadowSharpness,
    float shadingMode,
    float gradientWidth)
{
    half3 totalLight = 0;
    UNITY_UNROLL
    for (int i = 0; i < 4; i++)
    {
        float3 lightPos = float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]);
        float3 toLight = lightPos - worldPos;
        float distSq = max(dot(toLight, toLight), 0.000001);
        half3 lightDir = toLight * rsqrt(distSq);
        half ndotl = max(0, dot(worldNormal, lightDir));
        half toonLight = ToonShading(ndotl, shadowSteps, shadowSharpness);
        half gradientLight = GradientShading(ndotl, gradientWidth);
        half useGradient = step(HALF_VALUE, shadingMode) * (1.0 - step(1.5, shadingMode));
        half shapedLight = lerp(toonLight, gradientLight, useGradient);
        shapedLight *= ndotl;
        float atten = 1.0 / (1.0 + distSq * unity_4LightAtten0[i]);
        totalLight += unity_LightColor[i].rgb * shapedLight * atten;
    }
    return totalLight;
}

// Apply Light Blend (Softness)
// Softens lighting transitions for smoother appearance
float ApplyLightBlend(float lightValue)
{
    #ifdef _SOFT_LIGHTING_MODE
        // Global soft lighting mode
        float softness = _SoftLightingIntensity;
        lightValue = smoothstep(0.0, 1.0, lightValue * (1.0 + softness));
    #endif

    // Individual light blend control
    if (_LightBlend > 0.001)
    {
        lightValue = smoothstep(0.0, 1.0, lightValue * (1.0 + _LightBlend));
    }

    // Highlight softness
    if (_HighlightSoftness > 0.001 && lightValue > 0.7)
    {
        // Soften highlights (bright areas)
        float highlightFactor = (lightValue - 0.7) / 0.3; // Normalize to 0-1 for highlight range
        highlightFactor = smoothstep(0.0, 1.0, highlightFactor * (1.0 - _HighlightSoftness));
        lightValue = lerp(0.7, 1.0, highlightFactor);
    }

    return saturate(lightValue);
}

// Dithering Pattern (Bayer Matrix)
// Creates a dithering effect for softer shadow transitions
float DitheringPattern(float2 screenPos, float scale)
{
    return NataneGetDitherThreshold(StabilizeDitherCoord(screenPos), scale);
}

// Ramp Texture Shading
// Uses a gradient texture to control shadow colors
#if defined(_USE_RAMP)
float3 RampShading(float ndotl)
{
    float2 rampUV = float2(saturate(ndotl + _ShadowOffset), 0.5);
    return tex2D(_RampTex, rampUV).rgb;
}
#endif

// Multi-tone Shadow Colors
// Applies multiple shadow color tones based on lighting intensity
float3 MultiToneShadowColor(float shadowFactor, float3 baseColor)
{
    #ifdef _USE_MULTI_SHADOW
        // Calculate which shadow level to use based on shadow factor
        // shadowFactor: 0 = darkest, 1 = brightest

        // Validate and sort shadow border values (ensure 3rd < 2nd) - branchless
        float border2nd = max(_Shadow2ndBorder, _Shadow3rdBorder);
        float border3rd = min(_Shadow2ndBorder, _Shadow3rdBorder);

        // Calculate blend width from _ShadowBlend parameter
        // _ShadowBlend=0: 0.02 (sharp, backward compatible), _ShadowBlend=1: 0.5 (wide smooth fade)
        float blendWidth = max(0.01, lerp(0.02, 0.5, _ShadowBlend) + _StepBorderSmooth * 0.3);

        // Symmetric multi-tone shadow blending using smoothstep centered on border
        // This creates a natural, even fade around each shadow boundary
        float blend2nd = smoothstep(border2nd - blendWidth * 0.5, border2nd + blendWidth * 0.5, shadowFactor);
        float blend3rd = smoothstep(border3rd - blendWidth * 0.5, border3rd + blendWidth * 0.5, shadowFactor);
        float3 shadowColor = _ShadowColor.rgb;
        shadowColor = lerp(_Shadow2ndColor.rgb, shadowColor, blend2nd);
        shadowColor = lerp(_Shadow3rdColor.rgb, shadowColor, blend3rd);

        return shadowColor * baseColor;
    #else
        // Single shadow color mode
        return _ShadowColor.rgb * baseColor;
    #endif
}

// Specular Highlight (Anime Style)
// Creates sharp, stylized specular reflections
#if defined(_SPECULAR)
half SpecularHighlight(half3 normal, half3 viewDir, half3 lightDir, half size, half softness)
{
    half3 halfVector = normalize(lightDir + viewDir);
    half ndoth = max(0.0, dot(normal, halfVector));

    half edgeMin = 1.0 - size - softness;
    half edgeMax = 1.0 - size + softness;

    #if defined(_SPECULAR_AA)
        // Expand threshold by derivatives to reduce high-frequency shimmer.
        half aaWidth = max(fwidth(ndoth) * _SpecularAAStrength, 0.0005);
        edgeMin -= aaWidth;
        edgeMax += aaWidth;
    #endif

    half spec = smoothstep(edgeMin, edgeMax, ndoth);
    return spec;
}
#endif // _SPECULAR

// Hair Specular (Kajiya-Kay)
#if defined(_HAIR_SPECULAR)
half KajiyaKaySpecular(half3 shiftedTangent, half3 halfVector, half exponent)
{
    half TdotH = dot(shiftedTangent, halfVector);
    half sinTH = sqrt(max(0.001, 1.0 - TdotH * TdotH));

    #if defined(_SPECULAR_AA)
        half aaFactor = 1.0 + fwidth(TdotH) * _SpecularAAStrength * 128.0;
        exponent = max(1.0, exponent / aaFactor);
    #endif

    return saturate(pow(sinTH, exponent));
}

half3 HairSpecularHighlight(half3 worldNormal, half3 worldTangent, half3 worldBinormal,
                             half3 viewDir, half3 lightDir, float2 uv)
{
    half3 halfVec = normalize(lightDir + viewDir);

    // Use binormal as primary tangent direction (hair strands typically follow V-axis)
    half3 tangent = worldBinormal;

    // Sample shift texture if enabled
    half shiftTexValue = 0.0;
    #ifdef _HAIR_SPEC_SHIFT_TEX
        shiftTexValue = NATANE_SAMPLE_SHARED_R(_HairSpecShiftTex, _MainTex, uv) - 0.5;
    #endif

    // Shift tangent along normal for each lobe
    half3 shiftedTangent1 = normalize(tangent + worldNormal * (_HairSpecShift1 + shiftTexValue));
    half3 shiftedTangent2 = normalize(tangent + worldNormal * (_HairSpecShift2 + shiftTexValue));

    // Calculate two specular lobes
    half spec1 = KajiyaKaySpecular(shiftedTangent1, halfVec, _HairSpecWidth1);
    half spec2 = KajiyaKaySpecular(shiftedTangent2, halfVec, _HairSpecWidth2);

    // Combine lobes with their colors
    half3 specular = spec1 * _HairSpecColor1.rgb + spec2 * _HairSpecColor2.rgb;

    // Apply mask if enabled
    #ifdef _HAIR_SPEC_MASK
        half mask = NATANE_SAMPLE_SHARED_R(_HairSpecMask, _MainTex, uv);
        specular *= mask;
    #endif

    return specular * _HairSpecIntensity;
}
#endif // _HAIR_SPECULAR

// Rim Light Calculation
// Creates highlights at grazing angles (edges of objects)
#if defined(_RIM_LIGHT)
half3 RimLighting(half3 normal, half3 viewDir, half power, half intensity)
{
    half rim = 1.0 - saturate(dot(normal, viewDir));
    rim = pow(rim, power) * intensity;
    return rim * _RimColor.rgb;
}
#endif // _RIM_LIGHT

// Offset Rim Light Calculation
// Creates rim highlights with manual XY offset and optional light direction linking
#if defined(_OFFSET_RIM_LIGHT)
half3 OffsetRimLighting(half3 normal, half3 viewDir, half3 lightDir, half power, half intensity)
{
    // ビュー空間に法線を変換してXYオフセットを適用
    float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, normal);

    // マニュアルオフセット
    float2 manualOffset = float2(_OffsetRimOffsetX, _OffsetRimOffsetY);

    // ライト方向連動オフセット
    float3 viewLightDir = mul((float3x3)UNITY_MATRIX_V, lightDir);
    float2 lightOffset = viewLightDir.xy * _OffsetRimLightDirStrength;

    // オフセット合成（マニュアル + ライト方向 * UseLightDir）
    float2 totalOffset = manualOffset + lightOffset * _OffsetRimUseLightDir;

    // オフセット適用
    float3 offsetViewNormal = normalize(float3(viewNormal.xy + totalOffset, viewNormal.z));

    // Fresnel計算（オフセット後）
    half rim = 1.0 - saturate(dot(offsetViewNormal, float3(0, 0, 1)));
    rim = pow(rim, power);

    // トゥーンシャープネス
    rim = smoothstep(_OffsetRimSharpness - 0.01, _OffsetRimSharpness + max(0.02, (1.0 - _OffsetRimSharpness) * 0.5), rim);

    rim *= intensity;
    return rim * _OffsetRimColor.rgb;
}
#endif // _OFFSET_RIM_LIGHT

// Sheen (Fabric Luster)
// Simulates the sheen effect of fabric materials at grazing angles
#if defined(_SHEEN)
half3 SheenHighlight(half3 normal, half3 viewDir, half3 lightDir)
{
    half NdotV = max(0.0, dot(normal, viewDir));
    half NdotL = max(0.0, dot(normal, lightDir));
    // Charlie sheen approximation: grazing angle fabric luster
    half sheen = pow(1.0 - NdotV, _SheenPower) * NdotL;
    return saturate(sheen) * _SheenColor.rgb * _SheenIntensity;
}
#endif // _SHEEN

// Subsurface Scattering (Translucency)
// Simulates light passing through thin or translucent materials
#if defined(_SSS)
half3 SubsurfaceScattering(half3 normal, half3 lightDir, half3 viewDir, half thickness, half atten, float powerOverride)
{
    // Distort the normal for more realistic scattering effect
    half3 distortedNormal = normal + normalize(viewDir) * _SSSDistortion;

    // Calculate back-lit effect (light passing through the object)
    half backLight = max(0.0, dot(-normalize(distortedNormal), lightDir));

    // Apply power function for falloff and multiply by inverse thickness
    // Thicker areas scatter less light
    backLight = pow(backLight, powerOverride) * (1.0 - thickness);

    // Apply intensity, attenuation, and light color
    backLight *= _SSSIntensity * atten;

    return backLight * _SSSColor.rgb * _LightColor0.rgb;
}
#endif // _SSS

// Pre-integrated Subsurface Scattering using LUT
// Uses a 2D lookup table indexed by NdotL and curvature for physically-based SSS
#if defined(_SSS) && defined(_SSS_LUT)
half3 SubsurfaceScatteringLUT(half ndotl, half3 normal, half3 worldPos, half thickness)
{
    // Curvature calculation (using fwidth)
    half curvature = saturate(length(fwidth(normal)) / max(length(fwidth(worldPos)), 0.0001) * 0.5);
    // LUT UV: X=NdotL(0~1), Y=curvature(0~1)
    float2 lutUV = float2(ndotl * 0.5 + 0.5, curvature * (1.0 - thickness));
    half3 sssLUT = tex2D(_SSSLUTTex, lutUV).rgb;
    return sssLUT * _SSSColor.rgb * _SSSLUTScale;
}
#endif

// Cubemap Reflection (Environment Mapping)
// Samples a cubemap based on reflection vector for realistic environment reflections
#if defined(_REFLECTION)
half3 CubemapReflection(half3 worldNormal, half3 viewDir, half smoothness, half metallic)
{
    // Calculate reflection vector
    half3 reflectDir = reflect(-viewDir, worldNormal);

    // Calculate mip level based on smoothness (roughness = 1 - smoothness)
    half roughness = 1.0 - smoothness;
    half mipLevel = roughness * 7.0; // Assume 8 mip levels (0-7)

    // Sample cubemap with calculated mip level for roughness effect
    half4 reflectionSample = texCUBElod(_ReflectionCube, float4(reflectDir, mipLevel));

    // Apply reflection color tint
    half3 reflection = reflectionSample.rgb * _ReflectionColor.rgb;

    // Fresnel effect - objects reflect more at grazing angles
    half viewAngle = saturate(dot(worldNormal, viewDir));

    // Apply softness to Fresnel transition - Optimized: removed branching
    // Softness creates a more gradual transition between reflected and non-reflected areas
    half softRange = _FresnelSoftness * 0.5;
    viewAngle = smoothstep(softRange, 1.0 - softRange, viewAngle);

    half fresnel = pow(1.0 - viewAngle, _FresnelPower);

    // Metallic surfaces reflect more, non-metallic reflect at grazing angles
    half reflectionStrength = lerp(fresnel, 1.0, metallic);

    // Apply blend mode (0 = Additive, 1 = Overlay)
    // Additive: Simply adds reflection to base color
    // Overlay: Blends reflection more naturally with base color
    half blendFactor = lerp(1.0, reflectionStrength, _ReflectionBlendMode);

    return reflection * reflectionStrength * _ReflectionIntensity * blendFactor;
}
#endif // _REFLECTION

// Environmental Rim (Low-angle environment reflections)
// Simulates reflections at grazing angles from environment cubemap
#if defined(_ENV_RIM)
half3 EnvironmentalRim(half3 worldNormal, half3 viewDir, float powerOverride)
{
    // Calculate reflection vector
    half3 reflectDir = reflect(-viewDir, worldNormal);

    // Sample environment cubemap
    half3 envColor = texCUBE(_EnvRimCube, reflectDir).rgb;

    // Calculate rim factor (stronger at edges)
    half rim = 1.0 - saturate(dot(worldNormal, viewDir));
    rim = pow(rim, powerOverride);

    // Apply color tint and intensity
    return envColor * _EnvRimColor.rgb * rim * _EnvRimIntensity;
}
#endif // _ENV_RIM

// Refraction Calculation
// Calculates refracted view direction for transparent materials
#if defined(_REFRACTION)
float3 CalculateRefraction(float3 worldNormal, float3 viewDir, float refractionIndex)
{
    // Calculate refraction using Snell's law
    // IOR ratio: from air (1.0) to material (refractionIndex)
    float iorRatio = 1.0 / refractionIndex;

    // Refract the view direction through the surface
    float3 refractDir = refract(-viewDir, worldNormal, iorRatio);

    // If total internal reflection occurs, use reflection instead
    if (dot(refractDir, refractDir) < 0.0001)
    {
        refractDir = reflect(-viewDir, worldNormal);
    }

    return refractDir;
}
#endif // _REFRACTION

// SDF Shadow Map
// Uses signed distance field to add directional-independent shadows (like face shadows)
float ApplySDFShadow(float2 uv, float ndotl, float3 lightDir, float3 worldPos)
{
    #ifdef _SDF_MAP
        #ifdef _FACE_SDF_ROTATION
            // Transform face directions from object to world space
            float3 faceForward = normalize(mul((float3x3)unity_ObjectToWorld, _FaceForwardDirection.xyz));
            float3 faceRight = normalize(mul((float3x3)unity_ObjectToWorld, _FaceRightDirection.xyz));
            float3 faceUp = cross(faceForward, faceRight);

            // Project light direction onto face plane (remove vertical component)
            // NaN guard: if lightDir is zero or parallel to faceUp, fallback to faceForward
            float3 rawLightDirFlat = lightDir - dot(lightDir, faceUp) * faceUp;
            float flatLen = length(rawLightDirFlat);
            float3 lightDirFlat = (flatLen > 0.001) ? (rawLightDirFlat / flatLen) : faceForward;

            // Calculate light direction relative to face
            float FdotL = dot(faceForward, lightDirFlat);
            float RdotL = dot(faceRight, lightDirFlat);

            // Mirror UV.x when light comes from the left
            float2 sdfUV = uv;
            sdfUV.x = (RdotL < 0) ? (1.0 - sdfUV.x) : sdfUV.x;

            // Sample SDF map
            float sdfValue = tex2D(_SDFMap, sdfUV).r;

            // Threshold based on forward dot light
            float threshold = FdotL * 0.5 + 0.5 + _SDFOffset;

            // Apply softness
            float shadowEdge = _SDFSoftness * 0.5;
            float sdfShadow = smoothstep(threshold - shadowEdge, threshold + shadowEdge, sdfValue);

            // SDF fully controls face shadow when rotation is enabled
            return lerp(ndotl, sdfShadow, _SDFIntensity);
        #else
            // Original UV-fixed behavior
            float sdfValue = tex2D(_SDFMap, uv).r;

            // Apply offset to adjust shadow threshold
            sdfValue = saturate(sdfValue + _SDFOffset);

            // Apply softness to blend shadow edges
            float shadowEdge = _SDFSoftness * 0.5;
            float sdfShadow = smoothstep(0.5 - shadowEdge, 0.5 + shadowEdge, sdfValue);

            // Combine SDF shadow with lighting shadow using intensity control
            return lerp(ndotl, ndotl * sdfShadow, _SDFIntensity);
        #endif
    #else
        return ndotl;
    #endif
}

// Shading Grade Map
// Adjusts shadow intensity per-pixel for fine control
float ApplyShadingGradeMap(float2 uv, float shadowFactor)
{
    #ifdef _SHADING_GRADE_MAP
        // Sample shading grade map (0.5 = neutral, <0.5 = darker, >0.5 = lighter)
        float gradeValue = tex2D(_ShadingGradeMap, uv).r;

        // Remap from 0-1 to -1 to +1 range, then scale by user parameter
        float gradeAdjust = (gradeValue - 0.5) * 2.0 * _ShadingGradeScale;

        // Apply grade adjustment to shadow factor
        shadowFactor = saturate(shadowFactor + gradeAdjust);

        return shadowFactor;
    #else
        return shadowFactor;
    #endif
}

// Glitter Effect
// Creates sparkly/shimmery effect on surfaces
half3 GlitterEffect(float2 uv, float3 worldPos, half3 viewDir, half3 normal, half3 lightDir, float blur)
{
    #ifdef _GLITTER
        // Create random glitter pattern using world position
        float3 glitterPos = worldPos * _GlitterSize * 50.0;

        // Generate pseudo-random values using sine functions
        half glitterRandom = frac(sin(dot(glitterPos, float3(12.9898, 78.233, 45.164))) * 43758.5453);

        // Apply density threshold (blur softens the hard step into smoothstep)
        half densityThreshold = 1.0 - _GlitterDensity;
        half glitterMask = lerp(step(densityThreshold, glitterRandom),
            smoothstep(densityThreshold - 0.3, densityThreshold, glitterRandom), blur);

        half glitter = 0.0;
        #if defined(_GLINTS_ADVANCED)
            // Approximate physically-plausible glints using micro-normal perturbation and NdotH lobe.
            float3 cell = floor(glitterPos * 4.0);
            float cellHash = frac(sin(dot(cell, float3(95.435, 74.231, 11.973))) * 43758.5453);
            float3 randDir = normalize(float3(
                frac(cellHash * 13.37) * 2.0 - 1.0,
                frac(cellHash * 7.91) * 2.0 - 1.0,
                frac(cellHash * 5.23) * 2.0 - 1.0));
            half3 microNormal = normalize(normal + randDir * _GlintsNormalJitter);
            half3 halfVec = normalize(viewDir + lightDir);
            half ndoth = saturate(dot(microNormal, halfVec));
            half lobe = pow(ndoth, _GlintsSharpness);

            half temporal = frac(cellHash * 21.7 + _Time.y * _GlitterSpeed * _GlintsTemporal);
            temporal = smoothstep(0.2, 0.8, temporal);
            glitter = glitterMask * lobe * temporal;
        #else
            // Animate glitter using time
            half glitterTime = _Time.y * _GlitterSpeed;
            half glitterFlicker = frac(glitterRandom * 10.0 + glitterTime);
            glitterFlicker = smoothstep(0.3, 0.7, glitterFlicker); // Pulse animation

            // Calculate view-dependent glitter intensity (sparkles more when viewed at certain angles)
            half viewDot = max(0.0, dot(normal, viewDir));
            half viewFactor = viewDot * viewDot;
            glitter = glitterMask * glitterFlicker * viewFactor;
        #endif

        // Apply user mask
        half maskValue = NATANE_SAMPLE_SHARED_R(_GlitterMask, _MainTex, uv);
        glitter *= maskValue;

        return glitter * _GlitterColor.rgb * _GlitterIntensity;
    #else
        return 0;
    #endif
}

// Iridescence Effect
// Creates rainbow-like color shifts based on viewing angle
half3 IridescenceEffect(half3 normal, half3 viewDir, float2 uv, float sizeOverride)
{
    #ifdef _IRIDESCENCE
        // Calculate view-dependent angle
        half viewAngle = saturate(dot(normal, viewDir));

        // Create color shift based on view angle and size parameter
        half hueShift = (1.0 - viewAngle) * sizeOverride;
        hueShift = frac(hueShift + _IridescenceHueShift);

        // Convert hue to RGB (simplified HSV to RGB conversion)
        half3 iridColor;
        half h = hueShift * 6.0;
        half c = 1.0;
        half x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));

        if (h < 1.0) iridColor = half3(c, x, 0);
        else if (h < 2.0) iridColor = half3(x, c, 0);
        else if (h < 3.0) iridColor = half3(0, c, x);
        else if (h < 4.0) iridColor = half3(0, x, c);
        else if (h < 5.0) iridColor = half3(x, 0, c);
        else iridColor = half3(c, 0, x);

        // Apply color tint
        iridColor *= _IridescenceColor.rgb;

        // Apply mask
        half maskValue = NATANE_SAMPLE_SHARED_R(_IridescenceMask, _MainTex, uv);
        iridColor *= maskValue;

        // Apply intensity and view-dependent falloff
        half oneMinusView = 1.0 - viewAngle;
        half falloff = oneMinusView * oneMinusView;
        return iridColor * _IridescenceIntensity * falloff;
    #else
        return 0;
    #endif
}

#endif // NATANE_TOON_LIGHTING_INCLUDED
