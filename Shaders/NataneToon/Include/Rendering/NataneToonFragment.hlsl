#ifndef NATANE_TOON_FRAGMENT_INCLUDED
#define NATANE_TOON_FRAGMENT_INCLUDED

// Fragment Shader
// Main pixel/fragment rendering function
half4 frag(v2f i) : SV_Target
{
    // ===== Parallax Mapping (UV Adjustment) =====
    float2 uv = i.uv;
    #ifdef _PARALLAX
        float3 tangentViewDir = CalculateTangentViewDir(i.worldPos, i.worldTangent, i.worldBinormal, i.worldNormal);
        uv = ParallaxMapping(i.uv, tangentViewDir);
    #endif

    // ===== Texture Sampling =====
    half4 mainTex = tex2D(_MainTex, uv);
    half4 col = mainTex * _Color;

    // ===== 2nd Texture (Makeup) Blending =====
    #ifdef _2ND_TEXTURE
        half4 tex2nd = tex2D(_2ndTex, uv);
        float tex2ndMask = tex2nd.a; // Use alpha channel from texture

        #ifdef _2ND_TEX_MASK
            tex2ndMask *= tex2D(_2ndTexMask, uv).r; // Multiply with mask if enabled
        #endif

        // Apply HSV adjustments to texture (default values: hueShift=0, saturation=1, value=1 preserve original colors)
        float3 tex2ndAdjusted = ApplyHSVAdjustment(tex2nd.rgb, _2ndTexHueShift, _2ndTexSaturation, _2ndTexValue);

        col.rgb = ApplyBlendMode(col.rgb, tex2ndAdjusted, float3(1, 1, 1), _2ndTexIntensity * tex2ndMask, _2ndTexBlendMode);
    #endif

    // ===== 3rd Texture (Makeup) Blending =====
    #ifdef _3RD_TEXTURE
        half4 tex3rd = tex2D(_3rdTex, uv);
        float tex3rdMask = tex3rd.a; // Use alpha channel from texture

        #ifdef _3RD_TEX_MASK
            tex3rdMask *= tex2D(_3rdTexMask, uv).r; // Multiply with mask if enabled
        #endif

        // Apply HSV adjustments to texture (default values: hueShift=0, saturation=1, value=1 preserve original colors)
        float3 tex3rdAdjusted = ApplyHSVAdjustment(tex3rd.rgb, _3rdTexHueShift, _3rdTexSaturation, _3rdTexValue);

        col.rgb = ApplyBlendMode(col.rgb, tex3rdAdjusted, float3(1, 1, 1), _3rdTexIntensity * tex3rdMask, _3rdTexBlendMode);
    #endif

    // ===== 4th Texture (Makeup) Blending =====
    #ifdef _4TH_TEXTURE
        half4 tex4th = tex2D(_4thTex, uv);
        float tex4thMask = tex4th.a; // Use alpha channel from texture

        #ifdef _4TH_TEX_MASK
            tex4thMask *= tex2D(_4thTexMask, uv).r; // Multiply with mask if enabled
        #endif

        // Apply HSV adjustments to texture (default values: hueShift=0, saturation=1, value=1 preserve original colors)
        float3 tex4thAdjusted = ApplyHSVAdjustment(tex4th.rgb, _4thTexHueShift, _4thTexSaturation, _4thTexValue);

        col.rgb = ApplyBlendMode(col.rgb, tex4thAdjusted, float3(1, 1, 1), _4thTexIntensity * tex4thMask, _4thTexBlendMode);
    #endif

    // ===== 5th Texture (Makeup) Blending =====
    #ifdef _5TH_TEXTURE
        half4 tex5th = tex2D(_5thTex, uv);
        float tex5thMask = tex5th.a; // Use alpha channel from texture

        #ifdef _5TH_TEX_MASK
            tex5thMask *= tex2D(_5thTexMask, uv).r; // Multiply with mask if enabled
        #endif

        // Apply HSV adjustments to texture (default values: hueShift=0, saturation=1, value=1 preserve original colors)
        float3 tex5thAdjusted = ApplyHSVAdjustment(tex5th.rgb, _5thTexHueShift, _5thTexSaturation, _5thTexValue);

        col.rgb = ApplyBlendMode(col.rgb, tex5thAdjusted, float3(1, 1, 1), _5thTexIntensity * tex5thMask, _5thTexBlendMode);
    #endif

    // ===== Normal Mapping =====
    float3 worldNormal = normalize(i.worldNormal);
    #ifdef _NORMALMAP
        float3 normalMap = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
        float3x3 tangentToWorld = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);
        worldNormal = normalize(mul(normalMap, tangentToWorld));
    #endif

    // ===== Shadow Receive Mask Setup =====
    // Sample shadow mask once and use it for all shadow-related calculations
    float shadowReceiveMask = 0.0; // Default: fully receive shadows (black = receive shadows)
    #ifdef _SHADOW_RECEIVE_MASK
        shadowReceiveMask = tex2D(_ShadowReceiveMask, uv).r;
        shadowReceiveMask = ApplySoftMask(shadowReceiveMask); // Smooth mask transitions
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
    // マスクの判定を反転: 白（1.0）= 影を受けない、黒（0.0）= 影を受ける
    float shadowStrength = (1.0 - shadowReceiveMask) * _ShadowReceive;
    atten = lerp(1.0, atten, shadowStrength);

    float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
    float ndotl = max(0.0, dot(worldNormal, lightDir));

    // ===== Backlight Calculation =====
    // Calculate light coming from behind the object (rim-like effect)
    float backlight = 0.0;
    #ifdef UNITY_PASS_FORWARDBASE
        float backlightDot = max(0.0, dot(worldNormal, -lightDir));
        backlight = pow(backlightDot, 4.0) * _BacklightIntensity;
    #endif

    // ===== Toon/Ramp Shading (NiloToon-style) =====
    // Calculate base light term
    float lightTerm = ndotl * atten;

    // Apply shadow receive mask to light term
    // 白いマスク部分（shadowReceiveMask = 1.0）では常に明るく保つ
    // これにより、ndotlの影響を受けずにシェーディングを無効化できる
    lightTerm = lerp(lightTerm, 1.0, shadowReceiveMask);

    // Apply lit area softness BEFORE toon shading for visible effect
    if (_LitSoftness > 0.001)
    {
        // Add smoothstep to soften the light transition
        // This blends between the original sharp lighting and smoothed lighting
        float smoothedLight = smoothstep(0.0, 1.0, lightTerm);
        lightTerm = lerp(lightTerm, smoothedLight, _LitSoftness);
    }

    // Apply dithering to soften shadow boundaries
    #ifdef _USE_DITHERING
        float ditherPattern = DitheringPattern(i.pos.xy, _DitheringScale);
        // Apply dithering to shadow boundary area (around 0.4-0.6 range)
        float ditherRange = saturate(1.0 - abs(lightTerm - 0.5) * 2.0);
        float ditherEffect = (ditherPattern - 0.5) * _DitheringStrength * ditherRange;
        lightTerm = saturate(lightTerm + ditherEffect);
    #endif

    float3 lighting;
    #ifdef _USE_RAMP
        // Use ramp texture for custom shadow gradients
        lighting = RampShading(lightTerm);
    #else
        // Choose between Toon and Gradient shading modes
        float shadingValue;
        if (_ShadingMode < 0.5)
        {
            // Toon Mode: Stepped cel-shading
            shadingValue = ToonShading(lightTerm, _ShadowSteps, _ShadowSharpness);
        }
        else
        {
            // Gradient Mode: Smooth gradient shading
            shadingValue = GradientShading(lightTerm, _ShadingGradientWidth);
        }

        // NiloToon-style shadow color mixing for more vibrant anime look
        // Instead of simple lerp, preserve color saturation in shadows
        float3 litColor = half3(1.0, 1.0, 1.0);
        float3 shadowColor = _ShadowColor.rgb;

        // Preserve hue and saturation better in shadows
        // This creates more vibrant, anime-style shadows
        lighting = lerp(shadowColor, litColor, shadingValue);

        // Apply gamma correction for more accurate color mixing
        lighting = pow(lighting, 2.2);
        lighting = pow(lighting, 1.0 / 2.2);
    #endif

    // Apply shadow max darkness limit (prevents shadows from being too black)
    lighting = max(lighting, _ShadowMaxDarkness);

    // ===== Improved Light Color Application =====
    // Apply light color influence more carefully to preserve material colors
    float3 lightColorInfluenced;

    if (_LightColorInfluence > 0.001)
    {
        // Calculate light color luminance
        float lightColorLuminance = dot(_LightColor0.rgb, float3(0.299, 0.587, 0.114));

        // Extract lighting luminance before light color application
        float baseLightingLuminance = dot(lighting, float3(0.299, 0.587, 0.114));

        // Method 1: Multiply light color (traditional - can shift colors)
        float3 colorMultiplied = lighting * _LightColor0.rgb;

        // Method 2: Apply only light color luminance (preserves lighting color)
        float3 luminanceOnly = lighting * lightColorLuminance;

        // Blend between methods based on LightColorInfluence
        // Low influence = preserve lighting color, High influence = apply light color
        lightColorInfluenced = lerp(luminanceOnly, colorMultiplied, _LightColorInfluence);
    }
    else
    {
        lightColorInfluenced = lighting;
    }

    lighting = lightColorInfluenced * _LightIntensity;

    // ===== Light Influence Clamping =====
    // Clamp brightness to prevent too dark or too bright results
    float lightLuminance = dot(lighting, float3(0.299, 0.587, 0.114));
    lightLuminance = clamp(lightLuminance, _LightMinInfluence, _LightMaxInfluence);
    lighting = normalize(lighting + 0.001) * lightLuminance;

    // ===== Ambient Occlusion =====
    #ifdef _USE_AO
        float ao = tex2D(_AOMap, uv).r;
        ao = ApplySoftMask(ao); // Smooth AO transitions
        // Apply AO to darken occluded areas
        // AO of 1.0 = no occlusion (white), AO of 0.0 = full occlusion (black)
        float aoEffect = lerp(1.0, ao, _AOIntensity);
        lighting *= aoEffect;
    #endif

    // ===== Ambient and Backlight (ForwardBase only) =====
    #ifdef UNITY_PASS_FORWARDBASE
        // Add ambient lighting from environment
        float3 ambient;
        float3 indirectLight = float3(0, 0, 0);

        // VRC Light Volumes integration (OpenLit/lilToon style)
        #ifdef _USE_LIGHT_VOLUME
            // Sample Light Volume SH coefficients
            float3 L0, L1r, L1g, L1b;
            LightVolumeSH(i.worldPos, L0, L1r, L1g, L1b);

            // Evaluate direct and indirect lighting from Light Volume (OpenLit style)
            // Direct light: evaluate with normal (bright side)
            float3 directLightLV = LightVolumeEvaluate(worldNormal, L0, L1r, L1g, L1b);

            // Indirect light: evaluate with negative normal (dark side)
            indirectLight = LightVolumeEvaluate(-worldNormal, L0, L1r, L1g, L1b);

            // Gentle clamp to prevent over-brightening (allow slight overshoot for natural look)
            directLightLV = min(directLightLV, float3(1.1, 1.1, 1.1));
            indirectLight = min(indirectLight, float3(1.1, 1.1, 1.1));

            // Apply intensity controls (scale down for natural appearance)
            // Light Volumeは非常に明るくなりがちなので、より控えめに
            directLightLV *= _IndirectLightIntensity * 0.5;  // 50% reduction for more natural look
            indirectLight *= _IndirectLightIntensity * 0.5;

            // Apply Shadow Receive Mask to Light Volume
            // マスクされた部分（白）はLight Volumeの影響を軽減
            // Shadow Receive Maskは間接光の影響を制御
            float lvInfluence = (1.0 - shadowReceiveMask);
            indirectLight *= lvInfluence;

            // Apply Light Volume with blend mode
            if (_LightVolumeBlendMode < 0.5) // Add (Default - lilToon style)
            {
                // Use Unity's light probes as base ambient
                ambient = ShadeSH9(float4(worldNormal, 1.0)) * _IndirectLightIntensity;

                // Add Light Volume contribution (lilToon style - only add the difference)
                // Direct light: add only what's brighter than current lighting
                float3 lvAddition = saturate(directLightLV - lighting);
                lighting += lvAddition * _LightVolumeIntensity;

                // Integrate indirect light as fake rim (lilToon style)
                // Calculate rim factor based on view and normal (more subtle)
                float3 viewDirForLV = normalize(_WorldSpaceCameraPos - i.worldPos);
                float rimFactor = 1.0 - saturate(dot(worldNormal, viewDirForLV));
                rimFactor = pow(rimFactor, 3.0); // More subtle rim (power 3 instead of 2)

                // Add indirect light to main lighting as rim (only the difference)
                // マスクされた部分では、この追加ライティングも抑制される
                float3 indirectAddition = saturate(indirectLight - lighting);
                lighting += indirectAddition * rimFactor * _LightVolumeIntensity;
            }
            else if (_LightVolumeBlendMode < 1.5) // Multiply
            {
                // Use Unity's light probes as base ambient
                ambient = ShadeSH9(float4(worldNormal, 1.0)) * _IndirectLightIntensity;

                // Use as modulation factor
                lighting *= lerp(float3(1, 1, 1), directLightLV, _LightVolumeIntensity);
            }
            else // Replace (>= 1.5)
            {
                // Replace existing lighting completely
                ambient = float3(0, 0, 0);
                lighting = lerp(lighting, directLightLV, _LightVolumeIntensity);
            }

            // Add Light Volume specular if enabled
            #ifdef _LIGHT_VOLUME_SPECULAR
                float3 lvSpecular = LightVolumeSpecular(col.rgb, _Smoothness, _Metallic, worldNormal, viewDir, L0, L1r, L1g, L1b);
                lvSpecular *= _LightVolumeIntensity;
                // Apply glossiness and matte effect
                lvSpecular *= _Glossiness * (1.0 - _MatteEffect);
                // Use safe additive blending to prevent white-out
                float lvSpecStrength = saturate(length(lvSpecular) * 0.5);
                col.rgb = SafeAdditiveBlend(col.rgb, lvSpecular, lvSpecStrength);
            #endif
        #else
            // Fallback to Unity's built-in light probes
            ambient = ShadeSH9(float4(worldNormal, 1.0));
            // Apply indirect light intensity control
            ambient *= _IndirectLightIntensity;
        #endif

        // Add ambient lighting
        lighting += ambient;

        // Add backlight effect
        lighting += backlight * _BacklightColor.rgb * _LightColor0.rgb;
    #else
        // ===== Additional Light Intensity Control (ForwardAdd pass) =====
        // Scale down additional lights to prevent over-brightening with multiple lights
        lighting *= _AdditionalLightIntensity;
    #endif

    // Store original texture color before lighting application
    float3 originalAlbedo = col.rgb;
    float originalLuminance = dot(originalAlbedo, float3(0.299, 0.587, 0.114));

    // ===== Improved Color Preservation Lighting =====
    // Instead of directly multiplying, preserve color hue and saturation
    // while applying lighting brightness

    // Calculate lighting luminance
    float lightingLuminance = dot(lighting, float3(0.299, 0.587, 0.114));

    // Method 1: Preserve color by applying only luminance change
    // Extract color direction (hue/saturation) from original albedo
    float3 albedoDirection = originalAlbedo / max(originalLuminance, 0.001);

    // Apply lighting luminance to color direction
    // This keeps the original color while adjusting brightness
    float3 preservedLitColor = albedoDirection * originalLuminance * lightingLuminance;

    // Method 2: Traditional lighting (for blending)
    float3 traditionalLitColor = originalAlbedo * lighting;

    // Blend between preserved color and traditional lighting based on AlbedoPreservation
    // When AlbedoPreservation = 1.0, use fully preserved color (no white-washing)
    // When AlbedoPreservation = 0.0, use traditional lighting
    col.rgb = lerp(traditionalLitColor, preservedLitColor, _AlbedoPreservation);

    // Additional color preservation: prevent color shift in dark areas
    // Dark colors (like black) should stay dark, not become gray
    if (originalLuminance < 0.1 && _AlbedoPreservation > 0.5)
    {
        // For very dark colors, preserve the darkness
        col.rgb = min(col.rgb, originalAlbedo * (lightingLuminance * 1.2));
    }

    // 2. Saturation Adjustment
    // Enhance or reduce color saturation
    if (abs(_Saturation - 1.0) > 0.001)
    {
        float3 gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
        col.rgb = lerp(gray, col.rgb, _Saturation);
    }

    // 3. Overall Brightness Adjustment
    // Final brightness control (applied before effects so they show properly)
    col.rgb *= _Brightness;

    // ===== Specular Highlight =====
    #ifdef _SPECULAR
        float spec = SpecularHighlight(worldNormal, viewDir, lightDir, _SpecularSize, _SpecularSoftness);
        float3 specContrib = spec * _SpecularColor.rgb * _LightColor0.rgb * atten;

        // Apply mask texture with soft blending
        #ifdef _SPECULAR_MASK
            float specMask = tex2D(_SpecularMask, uv).r;
            specMask = ApplySoftMask(specMask); // Smooth mask transitions
            specContrib *= specMask;
        #endif

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            specContrib *= _AdditionalLightIntensity;
        #endif

        // Apply glossiness and matte effect
        float glossFactor = _Glossiness * (1.0 - _MatteEffect);
        specContrib *= glossFactor;

        // Use safe additive blending to prevent white-out
        col.rgb = SafeAdditiveBlend(col.rgb, specContrib, saturate(spec * 0.5 + 0.5));
    #endif

    // ===== Subsurface Scattering =====
    #ifdef _SSS
        float thickness = 1.0;
        #ifdef _THICKNESS_MAP
            // Use thickness map to control SSS per-pixel
            thickness = tex2D(_ThicknessMap, uv).r * _ThicknessScale;
        #else
            // Use uniform thickness
            thickness = _ThicknessScale;
        #endif

        float3 sss = SubsurfaceScattering(worldNormal, lightDir, viewDir, thickness, atten);

        // Apply mask texture with soft blending
        #ifdef _SSS_MASK
            float sssMask = tex2D(_SSSMask, uv).r;
            sssMask = ApplySoftMask(sssMask); // Smooth mask transitions
            sss *= sssMask;
        #endif

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            sss *= _AdditionalLightIntensity;
        #endif

        // Use safe additive blending to prevent white-out
        float sssStrength = saturate(length(sss) * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, sss, sssStrength);
    #endif

    // ===== Rim Light (ForwardBase only) =====
    #if defined(_RIM_LIGHT) && defined(UNITY_PASS_FORWARDBASE)
        float3 rim = RimLighting(worldNormal, viewDir, _RimPower, _RimIntensity);

        // Apply Spread/Glow effect
        if (_RimSpread > 0.001)
        {
            // Create a softer, wider rim for glow effect
            float rimSpreadPower = lerp(_RimPower, max(0.5, _RimPower * 0.3), _RimSpread);
            float3 rimGlow = RimLighting(worldNormal, viewDir, rimSpreadPower, _RimIntensity * _RimSpread * 0.5);
            rim += rimGlow;
        }

        // Apply mask texture with soft blending
        #ifdef _RIM_MASK
            float rimMask = tex2D(_RimMask, uv).r;
            rimMask = ApplySoftMask(rimMask); // Smooth mask transitions
            rim *= rimMask;
        #endif

        // Apply glossiness and matte effect
        rim *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        float rimStrength = saturate(length(rim) * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, rim, rimStrength);
    #endif

    // ===== Rim Light 2 (ForwardBase only) =====
    #if defined(_RIM_LIGHT_2) && defined(UNITY_PASS_FORWARDBASE)
        // Calculate rim factor (stronger at edges)
        float rim2Factor = 1.0 - saturate(dot(worldNormal, viewDir));
        rim2Factor = pow(rim2Factor, _RimPower2) * _RimIntensity2;
        float3 rim2 = rim2Factor * _RimColor2.rgb;

        // Apply Spread/Glow effect
        if (_RimSpread2 > 0.001)
        {
            // Create a softer, wider rim for glow effect
            float rim2SpreadPower = lerp(_RimPower2, max(0.5, _RimPower2 * 0.3), _RimSpread2);
            float rim2SpreadFactor = 1.0 - saturate(dot(worldNormal, viewDir));
            rim2SpreadFactor = pow(rim2SpreadFactor, rim2SpreadPower) * _RimIntensity2 * _RimSpread2 * 0.5;
            rim2 += rim2SpreadFactor * _RimColor2.rgb;
        }

        // Apply mask texture with soft blending
        #ifdef _RIM_MASK_2
            float rimMask2 = tex2D(_RimMask2, uv).r;
            rimMask2 = ApplySoftMask(rimMask2); // Smooth mask transitions
            rim2 *= rimMask2;
        #endif

        // Apply glossiness and matte effect
        rim2 *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        float rim2Strength = saturate(length(rim2) * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, rim2, rim2Strength);
    #endif

    // ===== Environmental Rim (ForwardBase only) =====
    #if defined(_ENV_RIM) && defined(UNITY_PASS_FORWARDBASE)
        float3 envRim = EnvironmentalRim(worldNormal, viewDir);

        // Apply mask texture with soft blending
        #ifdef _ENV_RIM_MASK
            float envRimMask = tex2D(_EnvRimMask, uv).r;
            envRimMask = ApplySoftMask(envRimMask); // Smooth mask transitions
            envRim *= envRimMask;
        #endif

        // Apply glossiness and matte effect
        envRim *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        float envRimStrength = saturate(length(envRim) * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, envRim, envRimStrength);
    #endif

    // ===== MatCap (ForwardBase only) =====
    #if defined(_MATCAP) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap = tex2D(_MatCapTex, matcapUV).rgb * _MatCapIntensity;

        // Apply mask texture with soft blending
        float matcapMask = 1.0;
        #ifdef _MATCAP_MASK
            matcapMask = tex2D(_MatCapMask, uv).r;
            matcapMask = ApplySoftMask(matcapMask); // Smooth mask transitions
        #endif
        matcap *= matcapMask;

        // Apply glossiness and matte effect
        matcap *= _Glossiness * (1.0 - _MatteEffect);

        // Blend modes: 0=Add (safe), 1=Multiply, 2=Replace
        if (_MatCapBlendMode < 0.5) // Add - use safe additive to prevent white-out
        {
            float matcapStrength = saturate(_MatCapIntensity * matcapMask * 0.5);
            col.rgb = SafeAdditiveBlend(col.rgb, matcap, matcapStrength);
        }
        else if (_MatCapBlendMode < 1.5) // Multiply
        {
            col.rgb = BlendWithSoftMask(col.rgb, col.rgb * matcap, saturate(_MatCapIntensity * matcapMask));
        }
        else // Replace
        {
            col.rgb = BlendWithSoftMask(col.rgb, matcap, saturate(_MatCapIntensity * matcapMask));
        }
    #endif

    // ===== Cubemap Reflection (ForwardBase only) =====
    #if defined(_REFLECTION) && defined(UNITY_PASS_FORWARDBASE)
        float3 reflection = CubemapReflection(worldNormal, viewDir, _Smoothness, _Metallic);

        // Apply mask texture with soft blending
        float reflectionMask = 1.0;
        #ifdef _REFLECTION_MASK
            reflectionMask = tex2D(_ReflectionMask, uv).r;
            reflectionMask = ApplySoftMask(reflectionMask); // Smooth mask transitions
        #endif
        reflection *= reflectionMask;

        // Apply glossiness and matte effect
        reflection *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        float reflectionStrength = saturate(length(reflection) * reflectionMask * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, reflection, reflectionStrength);
    #endif

    // ===== Emission (ForwardBase only) =====
    #if defined(_EMISSION) && defined(UNITY_PASS_FORWARDBASE)
        float2 emissionUV = uv;

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

        // Apply mask texture with soft blending
        float emissionMask = 1.0;
        #ifdef _EMISSION_MASK
            emissionMask = tex2D(_EmissionMask, uv).r;
            emissionMask = ApplySoftMask(emissionMask); // Smooth mask transitions
        #endif
        emission *= emissionMask;

        // Apply Glow/Bloom effect
        if (_EmissionGlow > 0.001)
        {
            // Calculate luminance of emission
            float emissionLuminance = dot(emission, float3(0.299, 0.587, 0.114));
            // Add glow proportional to emission brightness
            float3 glow = emission * emissionLuminance * _EmissionGlow * 2.0;
            emission += glow;
        }

        // Use safe additive blending to prevent white-out
        float emissionStrength = saturate(length(emission) * emissionMask * 0.3);
        col.rgb = SafeAdditiveBlend(col.rgb, emission, emissionStrength);
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
        // Early exit if dissolve amount is 0 (no effect)
        if (_DissolveAmount > 0.0)
        {
            float dissolveMaskValue = 1.0;

            // Apply mask texture
            #ifdef _DISSOLVE_MASK
                dissolveMaskValue = tex2D(_DissolveMask, uv).r;
            #endif

            float2 dissolveResult = CalculateDissolve(uv, _DissolveAmount, _DissolveEdgeWidth);
            float dissolveAlpha = dissolveResult.x;
            float edgeGlow = dissolveResult.y;

            // Apply mask to edge glow and dissolve effect
            edgeGlow *= dissolveMaskValue;

            // Apply edge glow with safe additive blending
            if (edgeGlow > 0.0)
            {
                float3 dissolveGlow = _DissolveEdgeColor.rgb * edgeGlow * _DissolveEdgeIntensity;
                float dissolveStrength = saturate(edgeGlow * _DissolveEdgeIntensity * 0.5);
                col.rgb = SafeAdditiveBlend(col.rgb, dissolveGlow, dissolveStrength);
            }

            // Clip pixels based on dissolve amount and mask
            clip(dissolveAlpha + (1.0 - dissolveMaskValue));
        }
    #endif

    // ===== Alpha Mask =====
    // Apply alpha mask for partial transparency control
    #ifdef _ALPHA_MASK
        float alphaMask = tex2D(_AlphaMask, uv).r;
        col.a *= alphaMask;
    #endif

    // ===== Final Color Blending (Highlight & Shadow Smoothing) =====
    // Apply final smoothing to prevent harsh white/black spots
    // This is applied at the very end before fog for the most natural result
    col.rgb = ApplyFinalColorBlending(col.rgb);

    // ===== Fog =====
    UNITY_APPLY_FOG(i.fogCoord, col);

    return col;
}

#endif // NATANE_TOON_FRAGMENT_INCLUDED
