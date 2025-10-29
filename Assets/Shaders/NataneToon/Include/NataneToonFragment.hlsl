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
    #else
        // ===== Additional Light Intensity Control (ForwardAdd pass) =====
        // Scale down additional lights to prevent over-brightening with multiple lights
        lighting *= _AdditionalLightIntensity;
    #endif

    // Apply calculated lighting to base color
    col.rgb *= lighting;

    // ===== Specular Highlight =====
    #ifdef _SPECULAR
        float spec = SpecularHighlight(worldNormal, viewDir, lightDir, _SpecularSize, _SpecularSoftness);
        float3 specContrib = spec * _SpecularColor.rgb * _LightColor0.rgb * atten;

        // Apply mask texture
        #ifdef _SPECULAR_MASK
            float specMask = tex2D(_SpecularMask, i.uv).r;
            specContrib *= specMask;
        #endif

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            specContrib *= _AdditionalLightIntensity;
        #endif

        col.rgb += specContrib;
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

        // Apply mask texture
        #ifdef _SSS_MASK
            float sssMask = tex2D(_SSSMask, i.uv).r;
            sss *= sssMask;
        #endif

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            sss *= _AdditionalLightIntensity;
        #endif

        col.rgb += sss;
    #endif

    // ===== Rim Light (ForwardBase only) =====
    #if defined(_RIM_LIGHT) && defined(UNITY_PASS_FORWARDBASE)
        float3 rim = RimLighting(worldNormal, viewDir, _RimPower, _RimIntensity);

        // Apply mask texture
        #ifdef _RIM_MASK
            float rimMask = tex2D(_RimMask, i.uv).r;
            rim *= rimMask;
        #endif

        col.rgb += rim;
    #endif

    // ===== MatCap (ForwardBase only) =====
    #if defined(_MATCAP) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap = tex2D(_MatCapTex, matcapUV).rgb * _MatCapIntensity;

        // Apply mask texture
        #ifdef _MATCAP_MASK
            float matcapMask = tex2D(_MatCapMask, i.uv).r;
            matcap *= matcapMask;
        #endif

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
        float2 emissionUV = i.uv;

        // Apply scrolling animation
        #ifdef _EMISSION_SCROLL
            emissionUV += float2(_Time.y * _EmissionScrollSpeed, 0.0);
        #endif

        half3 emission = tex2D(_EmissionMap, emissionUV).rgb * _EmissionColor.rgb;

        // Apply pulse animation
        #ifdef _EMISSION_PULSE
            float pulse = sin(_Time.y * _EmissionPulseSpeed) * 0.5 + 0.5;
            pulse = lerp(1.0 - _EmissionPulseAmplitude, 1.0, pulse);
            emission *= pulse;
        #endif

        // Apply mask texture
        #ifdef _EMISSION_MASK
            float emissionMask = tex2D(_EmissionMask, i.uv).r;
            emission *= emissionMask;
        #endif

        col.rgb += emission;
    #endif

    // ===== Virtual Expression - Hue Shift =====
    #ifdef _HUE_SHIFT
        if (_HueShift > 0.001)
        {
            col.rgb = ApplyHueShift(col.rgb, _HueShift);
        }
    #endif

    // ===== Virtual Expression - Dissolve =====
    #ifdef _DISSOLVE
        float dissolveMaskValue = 1.0;

        // Apply mask texture
        #ifdef _DISSOLVE_MASK
            dissolveMaskValue = tex2D(_DissolveMask, i.uv).r;
        #endif

        float2 dissolveResult = CalculateDissolve(i.uv, _DissolveAmount, _DissolveEdgeWidth);
        float dissolveAlpha = dissolveResult.x;
        float edgeGlow = dissolveResult.y;

        // Apply mask to edge glow and dissolve effect
        edgeGlow *= dissolveMaskValue;

        // Apply edge glow
        if (edgeGlow > 0.0)
        {
            col.rgb += _DissolveEdgeColor.rgb * edgeGlow * _DissolveEdgeIntensity;
        }

        // Clip pixels based on dissolve amount and mask
        clip(dissolveAlpha + (1.0 - dissolveMaskValue));
    #endif

    // ===== Fog =====
    UNITY_APPLY_FOG(i.fogCoord, col);

    return col;
}

#endif // NATANE_TOON_FRAGMENT_INCLUDED
