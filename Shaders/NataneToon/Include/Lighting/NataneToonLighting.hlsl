#ifndef NATANE_TOON_LIGHTING_INCLUDED
#define NATANE_TOON_LIGHTING_INCLUDED

// =============================================================================
// Third-Party Lighting Integration (Auto-detected)
// Config files determine whether to use real cginc or fallback
// =============================================================================

// --- VRC Light Volumes ---
#include "../Config/NataneToonLVConfig.hlsl"

// --- LTCGI (Realtime Area Lights) ---
#include "../Config/NataneToonLTCGIConfig.hlsl"

#if defined(_LTCGI)
    #if defined(NATANE_LTCGI_AVAILABLE)
        #define LTCGI_AVATAR_MODE
        #include "Packages/at.pimaker.ltcgi/Shaders/LTCGI.cginc"
    #else
        // Fallback: SH + Reflection Probe で LTCGI を近似
        // 0.3倍スケールでエリアライトの局所性を近似
        void LTCGI_Contribution(float3 worldPos, float3 worldNormal, float3 viewDir,
            float roughness, float2 lightmapUV,
            inout float3 diffuse, inout float3 specular)
        {
            float3 shDirect = ShadeSH9(float4(worldNormal, 1.0));
            float3 shIndirect = ShadeSH9(float4(-worldNormal, 1.0));
            diffuse = max(0, lerp(shIndirect, shDirect, 0.85)) * 0.3;

            float3 reflDir = reflect(-viewDir, worldNormal);
            float mipLevel = roughness * 7.0;
            half4 envSample = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflDir, mipLevel);
            specular = DecodeHDR(envSample, unity_SpecCube0_HDR) * 0.3;
        }
    #endif
#endif

#if defined(_USE_LIGHT_VOLUME)
    // VRC Light Volumes Integration (lilToon-style bundled approach)
    // パッケージ版を優先、未インストール時はバンドル版を使用
    // バンドル版は RED_SIM 氏の MIT License に基づく同梱
    // See: ThirdParty/VRCLightVolumes/LICENSE.md
    //
    // LightVolumes.cginc は内部で _UdonLightVolumeEnabled == 0 の場合に
    // Unity Light Probes へ自動フォールバックするため、非VRChat環境でも安全
    #if defined(NATANE_VRCLV_AVAILABLE)
        #include "Packages/red.sim.lightvolumes/Shaders/LightVolumes.cginc"
    #else
        #include "../../ThirdParty/VRCLightVolumes/LightVolumes.cginc"
    #endif
#endif

// Lighting Calculation Functions

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
    float smoothRange = sharpness * 0.5;
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
half3 CalculateVertexLightsPixelPrecision(float3 worldPos, half3 worldNormal, float shadowSteps, float shadowSharpness)
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
        float atten = 1.0 / (1.0 + distSq * unity_4LightAtten0[i]);
        totalLight += unity_LightColor[i].rgb * toonLight * atten;
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
    // 4x4 Bayer matrix for dithering
    float4x4 bayerMatrix = float4x4(
        0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
        12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
        3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
        15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
    );

    // Scale screen position and get matrix indices
    float2 scaledPos = screenPos * scale;
    int2 matrixPos = int2(fmod(scaledPos.x, 4.0), fmod(scaledPos.y, 4.0));

    // Return dithering value
    return bayerMatrix[matrixPos.x][matrixPos.y];
}

// Ramp Texture Shading
// Uses a gradient texture to control shadow colors
float3 RampShading(float ndotl)
{
    float2 rampUV = float2(saturate(ndotl + _ShadowOffset), 0.5);
    return tex2D(_RampTex, rampUV).rgb;
}

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
        // _ShadowBlend=0: 0.05 (backward compatible), _ShadowBlend=1: 0.3 (wide blend)
        float blendWidth = max(0.01, lerp(0.05, 0.3, _ShadowBlend));

        // Branchless multi-tone shadow blending using smoothstep + lerp
        float blend2nd = smoothstep(border2nd - blendWidth, border2nd, shadowFactor);
        float blend3rd = smoothstep(border3rd - blendWidth, border3rd, shadowFactor);
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

    // Create sharp specular with controllable size and softness
    half spec = smoothstep(1.0 - size - softness, 1.0 - size + softness, ndoth);
    return spec;
}
#endif // _SPECULAR

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
float ApplySDFShadow(float2 uv, float ndotl)
{
    #ifdef _SDF_MAP
        // Sample SDF map (white = lit, black = shadow)
        float sdfValue = tex2D(_SDFMap, uv).r;

        // Apply offset to adjust shadow threshold
        sdfValue = saturate(sdfValue + _SDFOffset);

        // Apply softness to blend shadow edges
        float shadowEdge = _SDFSoftness * 0.5;
        float sdfShadow = smoothstep(0.5 - shadowEdge, 0.5 + shadowEdge, sdfValue);

        // Combine SDF shadow with lighting shadow using intensity control
        // Higher intensity = more pronounced SDF shadows
        return lerp(ndotl, ndotl * sdfShadow, _SDFIntensity);
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
half3 GlitterEffect(float2 uv, float3 worldPos, half3 viewDir, half3 normal, float blur)
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

        // Animate glitter using time
        half glitterTime = _Time.y * _GlitterSpeed;
        half glitterFlicker = frac(glitterRandom * 10.0 + glitterTime);
        glitterFlicker = smoothstep(0.3, 0.7, glitterFlicker); // Pulse animation

        // Calculate view-dependent glitter intensity (sparkles more when viewed at certain angles)
        half viewDot = max(0.0, dot(normal, viewDir));
        half viewFactor = viewDot * viewDot;

        // Combine all factors
        half glitter = glitterMask * glitterFlicker * viewFactor;

        // Apply user mask
        half maskValue = tex2D(_GlitterMask, uv).r;
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
        half maskValue = tex2D(_IridescenceMask, uv).r;
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
