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
// Creates cel-shaded stepped lighting effect
float ToonShading(float ndotl, float steps, float sharpness)
{
    // Apply shadow offset to adjust shadow threshold
    ndotl = ndotl + _ShadowOffset;

    // Calculate stepped lighting (quantize to steps)
    float toon = floor(ndotl * steps) / steps;

    // Smooth the edges based on sharpness parameter
    // Higher sharpness = sharper shadow boundaries
    float edge = frac(ndotl * steps);
    toon += smoothstep(0.5 - sharpness * 0.5, 0.5 + sharpness * 0.5, edge) / steps;

    return saturate(toon);
}

// Ramp Texture Shading
// Uses a gradient texture to control shadow colors
float3 RampShading(float ndotl)
{
    float2 rampUV = float2(saturate(ndotl + _ShadowOffset), 0.5);
    return tex2D(_RampTex, rampUV).rgb;
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
    float fresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), _FresnelPower);

    // Metallic surfaces reflect more, non-metallic reflect at grazing angles
    float reflectionStrength = lerp(fresnel, 1.0, metallic);

    return reflection * reflectionStrength * _ReflectionIntensity;
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

#endif // NATANE_TOON_LIGHTING_INCLUDED
