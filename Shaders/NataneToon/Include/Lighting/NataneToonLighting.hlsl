#ifndef NATANE_TOON_LIGHTING_INCLUDED
#define NATANE_TOON_LIGHTING_INCLUDED

// Include LightVolumes.cginc for VRC Light Volumes support
// This must come after UnityCG.cginc
#if defined(_USE_LIGHT_VOLUME)
    // Fallback macros if LightVolumes.cginc is not available
    #ifndef LIGHT_VOLUMES_INCLUDED
        #define LIGHT_VOLUMES_INCLUDED
        // Fallback to Unity's built-in light probes if VRC Light Volumes is not available
        void LightVolumeSH(float3 worldPos, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b)
        {
            // Use Unity's built-in SH sampling as fallback
            float3 ambient = ShadeSH9(float4(0, 1, 0, 1));
            L0 = ambient;
            L1r = float3(0, 0, 0);
            L1g = float3(0, 0, 0);
            L1b = float3(0, 0, 0);
        }

        float3 LightVolumeEvaluate(float3 worldNormal, float3 L0, float3 L1r, float3 L1g, float3 L1b)
        {
            // Simple directional evaluation
            float3 normal = normalize(worldNormal);
            return L0 + L1r * normal.x + L1g * normal.y + L1b * normal.z;
        }

        float3 LightVolumeSpecular(float3 albedo, float smoothness, float metallic, float3 worldNormal, float3 viewDir, float3 L0, float3 L1r, float3 L1g, float3 L1b)
        {
            // Simplified specular fallback
            float3 reflectDir = reflect(-viewDir, worldNormal);
            float spec = pow(max(0, dot(reflectDir, float3(0, 1, 0))), smoothness * 50.0);
            return spec * L0 * lerp(0.04, 1.0, metallic);
        }
    #endif
#endif

// Lighting Calculation Functions

// Toon Shading with adjustable steps and sharpness
// Creates cel-shaded stepped lighting effect (NiloToon-style)
float ToonShading(float ndotl, float steps, float sharpness)
{
    // Apply shadow offset to adjust shadow threshold
    ndotl = ndotl + _ShadowOffset;

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
    float stepValue = 1.0 / steps;

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
    // Apply shadow offset to adjust shadow threshold
    ndotl = ndotl + _ShadowOffset;

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

        // Start with base shadow color (1st level)
        float3 shadowColor = _ShadowColor.rgb;

        // Apply 2nd shadow level (intermediate shadow)
        if (shadowFactor < _Shadow2ndBorder)
        {
            // Blend towards 2nd shadow color
            float blend2nd = smoothstep(_Shadow2ndBorder - 0.05, _Shadow2ndBorder, shadowFactor);
            shadowColor = lerp(_Shadow2ndColor.rgb, shadowColor, blend2nd);
        }

        // Apply 3rd shadow level (deepest shadow)
        if (shadowFactor < _Shadow3rdBorder)
        {
            // Blend towards 3rd shadow color
            float blend3rd = smoothstep(_Shadow3rdBorder - 0.05, _Shadow3rdBorder, shadowFactor);
            shadowColor = lerp(_Shadow3rdColor.rgb, _Shadow2ndColor.rgb, blend3rd);
        }

        return shadowColor * baseColor;
    #else
        // Single shadow color mode
        return _ShadowColor.rgb * baseColor;
    #endif
}

// Specular Highlight (Anime Style)
// Creates sharp, stylized specular reflections
float SpecularHighlight(float3 normal, float3 viewDir, float3 lightDir, float size, float softness)
{
    float3 halfVector = normalize(lightDir + viewDir);
    float ndoth = max(0.0, dot(normal, halfVector));

    // Create sharp specular with controllable size and softness
    float spec = smoothstep(1.0 - size - softness, 1.0 - size + softness, ndoth);
    return spec;
}

// Rim Light Calculation
// Creates highlights at grazing angles (edges of objects)
float3 RimLighting(float3 normal, float3 viewDir, float power, float intensity)
{
    float rim = 1.0 - saturate(dot(normal, viewDir));
    rim = pow(rim, power) * intensity;
    return rim * _RimColor.rgb;
}

// Subsurface Scattering (Translucency)
// Simulates light passing through thin or translucent materials
float3 SubsurfaceScattering(float3 normal, float3 lightDir, float3 viewDir, float thickness, float atten)
{
    // Distort the normal for more realistic scattering effect
    float3 distortedNormal = normal + normalize(viewDir) * _SSSDistortion;

    // Calculate back-lit effect (light passing through the object)
    float backLight = max(0.0, dot(-normalize(distortedNormal), lightDir));

    // Apply power function for falloff and multiply by inverse thickness
    // Thicker areas scatter less light
    backLight = pow(backLight, _SSSPower) * (1.0 - thickness);

    // Apply intensity, attenuation, and light color
    backLight *= _SSSIntensity * atten;

    return backLight * _SSSColor.rgb * _LightColor0.rgb;
}

// Cubemap Reflection (Environment Mapping)
// Samples a cubemap based on reflection vector for realistic environment reflections
float3 CubemapReflection(float3 worldNormal, float3 viewDir, float smoothness, float metallic)
{
    // Calculate reflection vector
    float3 reflectDir = reflect(-viewDir, worldNormal);

    // Calculate mip level based on smoothness (roughness = 1 - smoothness)
    float roughness = 1.0 - smoothness;
    float mipLevel = roughness * 7.0; // Assume 8 mip levels (0-7)

    // Sample cubemap with calculated mip level for roughness effect
    float4 reflectionSample = texCUBElod(_ReflectionCube, float4(reflectDir, mipLevel));

    // Apply reflection color tint
    float3 reflection = reflectionSample.rgb * _ReflectionColor.rgb;

    // Fresnel effect - objects reflect more at grazing angles
    float viewAngle = saturate(dot(worldNormal, viewDir));

    // Apply softness to Fresnel transition
    // Softness creates a more gradual transition between reflected and non-reflected areas
    if (_FresnelSoftness > 0.001)
    {
        // Soften the Fresnel curve by adjusting the input
        float softRange = _FresnelSoftness * 0.5;
        viewAngle = smoothstep(softRange, 1.0 - softRange, viewAngle);
    }

    float fresnel = pow(1.0 - viewAngle, _FresnelPower);

    // Metallic surfaces reflect more, non-metallic reflect at grazing angles
    float reflectionStrength = lerp(fresnel, 1.0, metallic);

    // Apply blend mode (0 = Additive, 1 = Overlay)
    // Additive: Simply adds reflection to base color
    // Overlay: Blends reflection more naturally with base color
    float blendFactor = lerp(1.0, reflectionStrength, _ReflectionBlendMode);

    return reflection * reflectionStrength * _ReflectionIntensity * blendFactor;
}

// Environmental Rim (Low-angle environment reflections)
// Simulates reflections at grazing angles from environment cubemap
float3 EnvironmentalRim(float3 worldNormal, float3 viewDir)
{
    // Calculate reflection vector
    float3 reflectDir = reflect(-viewDir, worldNormal);

    // Sample environment cubemap
    float3 envColor = texCUBE(_EnvRimCube, reflectDir).rgb;

    // Calculate rim factor (stronger at edges)
    float rim = 1.0 - saturate(dot(worldNormal, viewDir));
    rim = pow(rim, _EnvRimPower);

    // Apply color tint and intensity
    return envColor * _EnvRimColor.rgb * rim * _EnvRimIntensity;
}

// Refraction Calculation
// Calculates refracted view direction for transparent materials
float3 CalculateRefraction(float3 worldNormal, float3 viewDir, float refractionIndex)
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

    return refractDir;
}

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
float3 GlitterEffect(float2 uv, float3 worldPos, float3 viewDir, float3 normal)
{
    #ifdef _GLITTER
        // Create random glitter pattern using world position
        float3 glitterPos = worldPos * _GlitterSize * 50.0;

        // Generate pseudo-random values using sine functions
        float glitterRandom = frac(sin(dot(glitterPos, float3(12.9898, 78.233, 45.164))) * 43758.5453);

        // Apply density threshold
        float glitterMask = step(1.0 - _GlitterDensity, glitterRandom);

        // Animate glitter using time
        float glitterTime = _Time.y * _GlitterSpeed;
        float glitterFlicker = frac(glitterRandom * 10.0 + glitterTime);
        glitterFlicker = smoothstep(0.3, 0.7, glitterFlicker); // Pulse animation

        // Calculate view-dependent glitter intensity (sparkles more when viewed at certain angles)
        float viewDot = max(0.0, dot(normal, viewDir));
        float viewFactor = pow(viewDot, 2.0);

        // Combine all factors
        float glitter = glitterMask * glitterFlicker * viewFactor;

        // Apply user mask if enabled
        #ifdef _GLITTER_MASK
            float maskValue = tex2D(_GlitterMask, uv).r;
            glitter *= maskValue;
        #endif

        return glitter * _GlitterColor.rgb * _GlitterIntensity;
    #else
        return float3(0, 0, 0);
    #endif
}

// Iridescence Effect
// Creates rainbow-like color shifts based on viewing angle
float3 IridescenceEffect(float3 normal, float3 viewDir, float2 uv)
{
    #ifdef _IRIDESCENCE
        // Calculate view-dependent angle
        float viewAngle = saturate(dot(normal, viewDir));

        // Create color shift based on view angle and size parameter
        float hueShift = (1.0 - viewAngle) * _IridescenceSize;
        hueShift = frac(hueShift + _IridescenceHueShift);

        // Convert hue to RGB (simplified HSV to RGB conversion)
        float3 iridColor;
        float h = hueShift * 6.0;
        float c = 1.0;
        float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));

        if (h < 1.0) iridColor = float3(c, x, 0);
        else if (h < 2.0) iridColor = float3(x, c, 0);
        else if (h < 3.0) iridColor = float3(0, c, x);
        else if (h < 4.0) iridColor = float3(0, x, c);
        else if (h < 5.0) iridColor = float3(x, 0, c);
        else iridColor = float3(c, 0, x);

        // Apply color tint
        iridColor *= _IridescenceColor.rgb;

        // Apply mask if enabled
        #ifdef _IRIDESCENCE_MASK
            float maskValue = tex2D(_IridescenceMask, uv).r;
            iridColor *= maskValue;
        #endif

        // Apply intensity and view-dependent falloff
        float falloff = pow(1.0 - viewAngle, 2.0);
        return iridColor * _IridescenceIntensity * falloff;
    #else
        return float3(0, 0, 0);
    #endif
}

#endif // NATANE_TOON_LIGHTING_INCLUDED
