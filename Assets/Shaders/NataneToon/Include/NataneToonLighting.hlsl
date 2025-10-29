#ifndef NATANE_TOON_LIGHTING_INCLUDED
#define NATANE_TOON_LIGHTING_INCLUDED

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

#endif // NATANE_TOON_LIGHTING_INCLUDED
