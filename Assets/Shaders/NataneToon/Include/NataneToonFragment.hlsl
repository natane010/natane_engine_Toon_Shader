#ifndef NATANE_TOON_FRAGMENT_INCLUDED
#define NATANE_TOON_FRAGMENT_INCLUDED

// Fragment Shader
// Main pixel/fragment rendering function
half4 frag(v2f i) : SV_Target
{
    // ===== Texture Sampling =====
    half4 mainTex = tex2D(_MainTex, i.uv);
    half4 col = mainTex * _Color;

    // ===== Normal Mapping =====
    float3 worldNormal = normalize(i.worldNormal);
    #ifdef _NORMALMAP
        float3 normalMap = UnpackScaleNormal(tex2D(_BumpMap, i.uv), _BumpScale);
        float3x3 tangentToWorld = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);
        worldNormal = normalize(mul(normalMap, tangentToWorld));
    #endif

    // ===== Lighting Setup =====
    float3 lightDir;
    float atten;

    #ifdef USING_DIRECTIONAL_LIGHT
        // Directional light (sun)
        lightDir = normalize(_WorldSpaceLightPos0.xyz);
        atten = SHADOW_ATTENUATION(i);
    #else
        // Point/Spot light
        float3 lightVec = _WorldSpaceLightPos0.xyz - i.worldPos;
        lightDir = normalize(lightVec);
        atten = SHADOW_ATTENUATION(i);

        // Apply distance attenuation for point/spot lights
        float distSqr = dot(lightVec, lightVec);
        atten *= 1.0 / (1.0 + distSqr * 0.1);
    #endif

    // Apply shadow receive strength (allows controlling how much shadows affect this material)
    atten = lerp(1.0, atten, _ShadowReceive);

    float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
    float ndotl = max(0.0, dot(worldNormal, lightDir));

    // ===== Backlight Calculation =====
    // Calculate light coming from behind the object (rim-like effect)
    float backlight = 0.0;
    #ifdef UNITY_PASS_FORWARDBASE
        float backlightDot = max(0.0, dot(worldNormal, -lightDir));
        backlight = pow(backlightDot, 4.0) * _BacklightIntensity;
    #endif

    // ===== Toon/Ramp Shading =====
    float3 lighting;
    #ifdef _USE_RAMP
        // Use ramp texture for custom shadow gradients
        lighting = RampShading(ndotl * atten);
    #else
        // Use stepped cel-shading
        float toon = ToonShading(ndotl * atten, _ShadowSteps, _ShadowSharpness);
        lighting = lerp(_ShadowColor.rgb, half3(1.0, 1.0, 1.0), toon);
    #endif

    // Apply shadow max darkness limit (prevents shadows from being too black)
    lighting = max(lighting, _ShadowMaxDarkness);

    // Apply light color
    lighting *= _LightColor0.rgb;

    // ===== Light Influence Clamping =====
    // Clamp brightness to prevent too dark or too bright results
    float lightLuminance = dot(lighting, float3(0.299, 0.587, 0.114));
    lightLuminance = clamp(lightLuminance, _LightMinInfluence, _LightMaxInfluence);
    lighting = normalize(lighting + 0.001) * lightLuminance;

    // ===== Ambient and Backlight (ForwardBase only) =====
    #ifdef UNITY_PASS_FORWARDBASE
        // Add ambient lighting from environment
        float3 ambient = ShadeSH9(float4(worldNormal, 1.0));
        lighting += ambient;

        // Add backlight effect
        lighting += backlight * _BacklightColor.rgb * _LightColor0.rgb;
    #endif

    // Apply calculated lighting to base color
    col.rgb *= lighting;

    // ===== Specular Highlight =====
    #ifdef _SPECULAR
        float spec = SpecularHighlight(worldNormal, viewDir, lightDir, _SpecularSize, _SpecularSoftness);
        col.rgb += spec * _SpecularColor.rgb * _LightColor0.rgb * atten;
    #endif

    // ===== Subsurface Scattering =====
    #ifdef _SSS
        float thickness = 1.0;
        #ifdef _THICKNESS_MAP
            // Use thickness map to control SSS per-pixel
            thickness = tex2D(_ThicknessMap, i.uv).r * _ThicknessScale;
        #else
            // Use uniform thickness
            thickness = _ThicknessScale;
        #endif

        float3 sss = SubsurfaceScattering(worldNormal, lightDir, viewDir, thickness, atten);
        col.rgb += sss;
    #endif

    // ===== Rim Light (ForwardBase only) =====
    #if defined(_RIM_LIGHT) && defined(UNITY_PASS_FORWARDBASE)
        float3 rim = RimLighting(worldNormal, viewDir, _RimPower, _RimIntensity);
        col.rgb += rim;
    #endif

    // ===== MatCap (ForwardBase only) =====
    #if defined(_MATCAP) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap = tex2D(_MatCapTex, matcapUV).rgb * _MatCapIntensity;

        // Blend modes: 0=Add, 1=Multiply, 2=Replace
        if (_MatCapBlendMode < 0.5) // Add
            col.rgb += matcap;
        else if (_MatCapBlendMode < 1.5) // Multiply
            col.rgb *= matcap;
        else // Replace
            col.rgb = lerp(col.rgb, matcap, _MatCapIntensity);
    #endif

    // ===== Emission (ForwardBase only) =====
    #if defined(_EMISSION) && defined(UNITY_PASS_FORWARDBASE)
        half3 emission = tex2D(_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
        col.rgb += emission;
    #endif

    // ===== Fog =====
    UNITY_APPLY_FOG(i.fogCoord, col);

    return col;
}

#endif // NATANE_TOON_FRAGMENT_INCLUDED
