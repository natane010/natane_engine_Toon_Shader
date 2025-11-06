#ifndef NATANE_TOON_FRAGMENT_INCLUDED
#define NATANE_TOON_FRAGMENT_INCLUDED

// Fragment Shader
// Main pixel/fragment rendering function
// Optimized: half precision for better performance, cached luminance calculations
half4 frag(v2f i) : SV_Target
{
    // ===== Parallax Mapping (UV Adjustment) =====
    float2 uv = i.uv;
    #ifdef _PARALLAX
        float3 tangentViewDir = CalculateTangentViewDir(i.worldPos, i.worldTangent, i.worldBinormal, i.worldNormal);
        uv = ParallaxMapping(i.uv, tangentViewDir);
    #endif

    // ===== UV Animation =====
    float2 mainUV = uv;
    #ifdef _MAIN_TEX_ANIMATION
        mainUV = AnimateUV(uv, _MainTexScrollSpeed.xy, _MainTexRotateSpeed);
    #endif

    // ===== Texture Sampling =====
    half4 mainTex = tex2D(_MainTex, mainUV);
    half4 col = mainTex * _Color;

    // ===== Makeup/Detail Textures Blending =====
    // Consolidated texture blending using shared function
    #ifdef _2ND_TEXTURE
        col.rgb = ApplyMakeupTexture(col.rgb, _2ndTex, _2ndTexMask, uv,
            _2ndTexHueShift, _2ndTexSaturation, _2ndTexValue,
            _2ndTexIntensity, _2ndTexBlendMode,
            #ifdef _2ND_TEX_MASK
                true
            #else
                false
            #endif
        );
    #endif

    #ifdef _3RD_TEXTURE
        col.rgb = ApplyMakeupTexture(col.rgb, _3rdTex, _3rdTexMask, uv,
            _3rdTexHueShift, _3rdTexSaturation, _3rdTexValue,
            _3rdTexIntensity, _3rdTexBlendMode,
            #ifdef _3RD_TEX_MASK
                true
            #else
                false
            #endif
        );
    #endif

    #ifdef _4TH_TEXTURE
        col.rgb = ApplyMakeupTexture(col.rgb, _4thTex, _4thTexMask, uv,
            _4thTexHueShift, _4thTexSaturation, _4thTexValue,
            _4thTexIntensity, _4thTexBlendMode,
            #ifdef _4TH_TEX_MASK
                true
            #else
                false
            #endif
        );
    #endif

    #ifdef _5TH_TEXTURE
        col.rgb = ApplyMakeupTexture(col.rgb, _5thTex, _5thTexMask, uv,
            _5thTexHueShift, _5thTexSaturation, _5thTexValue,
            _5thTexIntensity, _5thTexBlendMode,
            #ifdef _5TH_TEX_MASK
                true
            #else
                false
            #endif
        );
    #endif

    // ===== Normal Mapping =====
    // Optimization: Skip normalization if no normal mapping (already normalized in vertex shader)
    #ifdef _NORMALMAP
        half3 normalMap = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
        half3x3 tangentToWorld = half3x3(i.worldTangent, i.worldBinormal, i.worldNormal);
        half3 worldNormal = normalize(mul(normalMap, tangentToWorld));
    #else
        half3 worldNormal = i.worldNormal; // Already normalized in vertex shader
    #endif

    // ===== Shadow Receive Mask Setup =====
    // Sample shadow mask once and use it for all shadow-related calculations
    half shadowReceiveMask = 0.0; // Default: fully receive shadows (black = receive shadows)
    #ifdef _SHADOW_RECEIVE_MASK
        shadowReceiveMask = tex2D(_ShadowReceiveMask, uv).r;
        shadowReceiveMask = ApplySoftMask(shadowReceiveMask); // Smooth mask transitions
    #endif

    // ===== Lighting Setup =====
    half3 lightDir;
    half atten;

    #ifdef USING_DIRECTIONAL_LIGHT
        // Directional light (sun)
        lightDir = normalize(_WorldSpaceLightPos0.xyz);
        atten = SHADOW_ATTENUATION(i);
    #else
        // Point/Spot light
        half3 lightVec = _WorldSpaceLightPos0.xyz - i.worldPos;
        lightDir = normalize(lightVec);
        atten = SHADOW_ATTENUATION(i);

        // Apply distance attenuation for point/spot lights
        half distSqr = dot(lightVec, lightVec);
        atten *= 1.0 / (1.0 + distSqr * 0.1);
    #endif

    // Apply shadow receive strength (allows controlling how much shadows affect this material)
    // マスクの判定を反転: 白（1.0）= 影を受けない、黒（0.0）= 影を受ける
    half shadowStrength = (1.0 - shadowReceiveMask) * _ShadowReceive;
    atten = lerp(1.0, atten, shadowStrength);

    half3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
    half ndotl = max(0.0, dot(worldNormal, lightDir));

    // ===== SDF Shadow Map =====
    // Apply SDF shadow to ndotl before lighting calculations
    ndotl = ApplySDFShadow(uv, ndotl);

    // ===== Backlight Calculation =====
    // Calculate light coming from behind the object (rim-like effect)
    half backlight = 0.0;
    #ifdef UNITY_PASS_FORWARDBASE
        half backlightDot = max(0.0, dot(worldNormal, -lightDir));
        backlight = pow(backlightDot, 4.0) * _BacklightIntensity;
    #endif

    // ===== Toon/Ramp Shading (NiloToon-style) =====
    // Calculate base light term
    half lightTerm = ndotl * atten;

    // Apply shadow receive mask to light term
    // 白いマスク部分（shadowReceiveMask = 1.0）では常に明るく保つ
    // これにより、ndotlの影響を受けずにシェーディングを無効化できる
    lightTerm = lerp(lightTerm, 1.0, shadowReceiveMask);

    // Apply lit area softness - Optimized: removed branching
    // Softness calculation always executes (branch removal for better GPU performance)
    half smoothedLight = smoothstep(0.0, 1.0, lightTerm);
    lightTerm = lerp(lightTerm, smoothedLight, _LitSoftness);

    // Apply dithering to soften shadow boundaries
    #ifdef _USE_DITHERING
        half ditherPattern = DitheringPattern(i.pos.xy, _DitheringScale);
        // Apply dithering to shadow boundary area (around 0.4-0.6 range)
        half ditherRange = saturate(1.0 - abs(lightTerm - 0.5) * 2.0);
        half ditherEffect = (ditherPattern - 0.5) * _DitheringStrength * ditherRange;
        lightTerm = saturate(lightTerm + ditherEffect);
    #endif

    half3 lighting;
    #ifdef _USE_RAMP
        // Use ramp texture for custom shadow gradients
        lighting = RampShading(lightTerm);
    #else
        // Choose between Toon and Gradient shading modes - Optimized: no branching
        // Calculate both modes and blend based on _ShadingMode
        half toonValue = ToonShading(lightTerm, _ShadowSteps, _ShadowSharpness);
        half gradientValue = GradientShading(lightTerm, _ShadingGradientWidth);

        // Blend between modes: 0 = Toon, 1 = Gradient
        half shadingValue = lerp(toonValue, gradientValue, step(HALF_VALUE, _ShadingMode));

        // NiloToon-style shadow color mixing for more vibrant anime look
        // Instead of simple lerp, preserve color saturation in shadows
        half3 litColor = 1.0;
        half3 shadowColor = _ShadowColor.rgb;

        // Apply Shading Grade Map before final lighting
        shadingValue = ApplyShadingGradeMap(uv, shadingValue);

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
    // Optimized: Cache luminance calculations, remove branching
    half3 lightColorInfluenced;

    // Cache light color luminance (used multiple times)
    half lightColorLum = CALC_LUMINANCE(_LightColor0.rgb);

    // Method 1: Multiply light color (traditional - can shift colors)
    half3 colorMultiplied = lighting * _LightColor0.rgb;

    // Method 2: Apply only light color luminance (preserves lighting color)
    half3 luminanceOnly = lighting * lightColorLum;

    // Blend between methods based on LightColorInfluence (no branching)
    // Low influence = preserve lighting color, High influence = apply light color
    lightColorInfluenced = lerp(luminanceOnly, colorMultiplied, _LightColorInfluence);

    lighting = lightColorInfluenced * _LightIntensity;

    // ===== Light Influence Clamping =====
    // Clamp brightness to prevent too dark or too bright results
    // Optimized: Cache luminance calculation
    half lightLum = CALC_LUMINANCE(lighting);
    lightLum = clamp(lightLum, _LightMinInfluence, _LightMaxInfluence);
    lighting = normalize(lighting + 0.001) * lightLum;

    // ===== Ambient Occlusion =====
    #ifdef _USE_AO
        half ao = tex2D(_AOMap, uv).r;
        ao = ApplySoftMask(ao); // Smooth AO transitions
        // Apply AO to darken occluded areas
        // AO of 1.0 = no occlusion (white), AO of 0.0 = full occlusion (black)
        half aoEffect = lerp(1.0, ao, _AOIntensity);
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
            directLightLV *= _IndirectLightIntensity * _GIIntensity * 0.5;  // 50% reduction for more natural look
            indirectLight *= _IndirectLightIntensity * _GIIntensity * 0.5;

            // Apply Shadow Receive Mask to Light Volume
            // マスクされた部分（白）はLight Volumeの影響を軽減
            // Shadow Receive Maskは間接光の影響を制御
            float lvInfluence = (1.0 - shadowReceiveMask);
            indirectLight *= lvInfluence;

            // Apply Light Volume with blend mode
            // NOTE: Branching intentionally kept here as each mode has significantly different computations
            // Removing branches would force execution of all modes, reducing performance
            if (_LightVolumeBlendMode < 0.5) // Add (Default - lilToon style)
            {
                // Light Volume使用時はUnityのAmbient Colorを使用しない（lilToon style）
                // これによりLighting設定のEnvironment Lightingに影響されなくなる
                ambient = float3(0, 0, 0);

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
                // Light Volume使用時はUnityのAmbient Colorを使用しない（lilToon style）
                ambient = float3(0, 0, 0);

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
                lvSpecular *= _LightVolumeIntensity * _GIIntensity;
                // Apply glossiness and matte effect
                lvSpecular *= _Glossiness * (1.0 - _MatteEffect);
                // Use safe additive blending to prevent white-out
                float lvSpecStrength = saturate(length(lvSpecular) * 0.5);
                col.rgb = SafeAdditiveBlend(col.rgb, lvSpecular, lvSpecStrength);
            #endif
        #else
            // Fallback to Unity's built-in light probes
            ambient = ShadeSH9(float4(worldNormal, 1.0));
            // Apply indirect light intensity control and GI intensity
            ambient *= _IndirectLightIntensity * _GIIntensity;
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
    half3 originalAlbedo = col.rgb;
    // Optimized: Cache original luminance (used multiple times)
    half originalLum = CALC_LUMINANCE(originalAlbedo);

    // ===== Improved Color Preservation Lighting =====
    // Instead of directly multiplying, preserve color hue and saturation
    // while applying lighting brightness

    // Cache lighting luminance (already calculated as lightLum above - reuse if possible)
    half lightingLum = CALC_LUMINANCE(lighting);

    // Method 1: Preserve color by applying only luminance change
    // Extract color direction (hue/saturation) from original albedo
    half3 albedoDir = originalAlbedo / max(originalLum, 0.001);

    // Apply lighting luminance to color direction
    // This keeps the original color while adjusting brightness
    half3 preservedLitColor = albedoDir * originalLum * lightingLum;

    // Method 2: Traditional lighting (for blending)
    half3 traditionalLitColor = originalAlbedo * lighting;

    // Blend between preserved color and traditional lighting based on AlbedoPreservation
    // When AlbedoPreservation = 1.0, use fully preserved color (no white-washing)
    // When AlbedoPreservation = 0.0, use traditional lighting
    col.rgb = lerp(traditionalLitColor, preservedLitColor, _AlbedoPreservation);

    // Additional color preservation: prevent color shift in dark areas - Optimized: no branching
    // Dark colors (like black) should stay dark, not become gray
    half darkColorFactor = step(originalLum, 0.1) * step(HALF_VALUE, _AlbedoPreservation);
    half3 darkPreservedColor = min(col.rgb, originalAlbedo * (lightingLum * 1.2));
    col.rgb = lerp(col.rgb, darkPreservedColor, darkColorFactor);

    // 2. Saturation Adjustment - Optimized: removed branching
    // Enhance or reduce color saturation (lerp handles _Saturation=1.0 case efficiently)
    half gray = CALC_LUMINANCE(col.rgb);
    col.rgb = lerp(gray, col.rgb, _Saturation);

    // 3. Overall Brightness Adjustment
    // Final brightness control (applied before effects so they show properly)
    col.rgb *= _Brightness;

    // ===== Specular Highlight =====
    #ifdef _SPECULAR
        half spec = SpecularHighlight(worldNormal, viewDir, lightDir, _SpecularSize, _SpecularSoftness);
        half3 specContrib = spec * _SpecularColor.rgb * _LightColor0.rgb * atten;

        // Apply mask texture with soft blending
        #ifdef _SPECULAR_MASK
            half specMask = tex2D(_SpecularMask, uv).r;
            specMask = ApplySoftMask(specMask); // Smooth mask transitions
            specContrib *= specMask;
        #endif

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            specContrib *= _AdditionalLightIntensity;
        #endif

        // Apply glossiness and matte effect
        half glossFactor = _Glossiness * (1.0 - _MatteEffect);
        specContrib *= glossFactor;

        // Use safe additive blending to prevent white-out
        col.rgb = SafeAdditiveBlend(col.rgb, specContrib, saturate(spec * 0.5 + 0.5));
    #endif

    // ===== Subsurface Scattering =====
    #ifdef _SSS
        half thickness = 1.0;
        #ifdef _THICKNESS_MAP
            // Use thickness map to control SSS per-pixel
            thickness = tex2D(_ThicknessMap, uv).r * _ThicknessScale;
        #else
            // Use uniform thickness
            thickness = _ThicknessScale;
        #endif

        half3 sss = SubsurfaceScattering(worldNormal, lightDir, viewDir, thickness, atten);

        // Apply mask texture with soft blending
        #ifdef _SSS_MASK
            half sssMask = tex2D(_SSSMask, uv).r;
            sssMask = ApplySoftMask(sssMask); // Smooth mask transitions
            sss *= sssMask;
        #endif

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            sss *= _AdditionalLightIntensity;
        #endif

        // Use safe additive blending to prevent white-out
        half sssStrength = saturate(length(sss) * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, sss, sssStrength);
    #endif

    // ===== Rim Light (ForwardBase only) =====
    #if defined(_RIM_LIGHT) && defined(UNITY_PASS_FORWARDBASE)
        half3 rim = RimLighting(worldNormal, viewDir, _RimPower, _RimIntensity);

        // Apply Spread/Glow effect - Optimized: removed branching
        half rimSpreadPower = lerp(_RimPower, max(0.5, _RimPower * 0.3), _RimSpread);
        half3 rimGlow = RimLighting(worldNormal, viewDir, rimSpreadPower, _RimIntensity * _RimSpread * 0.5);
        rim += rimGlow * step(0.001, _RimSpread); // Conditional add without branch

        // Apply mask texture with soft blending
        #ifdef _RIM_MASK
            half rimMask = tex2D(_RimMask, uv).r;
            rimMask = ApplySoftMask(rimMask); // Smooth mask transitions
            rim *= rimMask;
        #endif

        // Apply glossiness and matte effect
        rim *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        half rimStrength = saturate(length(rim) * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, rim, rimStrength);
    #endif

    // ===== Rim Light 2 (ForwardBase only) =====
    #if defined(_RIM_LIGHT_2) && defined(UNITY_PASS_FORWARDBASE)
        // Calculate rim factor (stronger at edges)
        half rim2Factor = 1.0 - saturate(dot(worldNormal, viewDir));
        rim2Factor = pow(rim2Factor, _RimPower2) * _RimIntensity2;
        half3 rim2 = rim2Factor * _RimColor2.rgb;

        // Apply Spread/Glow effect - Optimized: removed branching
        half rim2SpreadPower = lerp(_RimPower2, max(0.5, _RimPower2 * 0.3), _RimSpread2);
        half rim2SpreadFactor = 1.0 - saturate(dot(worldNormal, viewDir));
        rim2SpreadFactor = pow(rim2SpreadFactor, rim2SpreadPower) * _RimIntensity2 * _RimSpread2 * 0.5;
        rim2 += rim2SpreadFactor * _RimColor2.rgb * step(0.001, _RimSpread2); // Conditional add without branch

        // Apply mask texture with soft blending
        #ifdef _RIM_MASK_2
            half rimMask2 = tex2D(_RimMask2, uv).r;
            rimMask2 = ApplySoftMask(rimMask2); // Smooth mask transitions
            rim2 *= rimMask2;
        #endif

        // Apply glossiness and matte effect
        rim2 *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        half rim2Strength = saturate(length(rim2) * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, rim2, rim2Strength);
    #endif

    // ===== Environmental Rim (ForwardBase only) =====
    #if defined(_ENV_RIM) && defined(UNITY_PASS_FORWARDBASE)
        half3 envRim = EnvironmentalRim(worldNormal, viewDir);

        // Apply mask texture with soft blending
        #ifdef _ENV_RIM_MASK
            half envRimMask = tex2D(_EnvRimMask, uv).r;
            envRimMask = ApplySoftMask(envRimMask); // Smooth mask transitions
            envRim *= envRimMask;
        #endif

        // Apply glossiness and matte effect
        envRim *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        half envRimStrength = saturate(length(envRim) * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, envRim, envRimStrength);
    #endif

    // ===== MatCap (ForwardBase only) =====
    #if defined(_MATCAP) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap = tex2D(_MatCapTex, matcapUV).rgb * _MatCapIntensity;

        // Apply mask texture with soft blending
        half matcapMask = 1.0;
        #ifdef _MATCAP_MASK
            matcapMask = tex2D(_MatCapMask, uv).r;
            matcapMask = ApplySoftMask(matcapMask); // Smooth mask transitions
        #endif
        matcap *= matcapMask;

        // Apply glossiness and matte effect
        matcap *= _Glossiness * (1.0 - _MatteEffect);

        // Blend modes: 0=Add (safe), 1=Multiply, 2=Replace - Optimized: no branching
        half matcapStrength = saturate(_MatCapIntensity * matcapMask * HALF_VALUE);
        half3 addResult = SafeAdditiveBlend(col.rgb, matcap, matcapStrength);
        half3 multiplyResult = BlendWithSoftMask(col.rgb, col.rgb * matcap, saturate(_MatCapIntensity * matcapMask));
        half3 replaceResult = BlendWithSoftMask(col.rgb, matcap, saturate(_MatCapIntensity * matcapMask));

        // Select blend mode using lerp
        half isMultiply = step(HALF_VALUE, _MatCapBlendMode) * step(_MatCapBlendMode, 1.5);
        half isReplace = step(1.5, _MatCapBlendMode);
        col.rgb = lerp(addResult, multiplyResult, isMultiply);
        col.rgb = lerp(col.rgb, replaceResult, isReplace);
    #endif

    // ===== Cubemap Reflection (ForwardBase only) =====
    #if defined(_REFLECTION) && defined(UNITY_PASS_FORWARDBASE)
        half3 reflection = CubemapReflection(worldNormal, viewDir, _Smoothness, _Metallic);

        // Apply mask texture with soft blending
        half reflectionMask = 1.0;
        #ifdef _REFLECTION_MASK
            reflectionMask = tex2D(_ReflectionMask, uv).r;
            reflectionMask = ApplySoftMask(reflectionMask); // Smooth mask transitions
        #endif
        reflection *= reflectionMask;

        // Apply glossiness and matte effect
        reflection *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        half reflectionStrength = saturate(length(reflection) * reflectionMask * 0.5);
        col.rgb = SafeAdditiveBlend(col.rgb, reflection, reflectionStrength);
    #endif

    // ===== Refraction (ForwardBase only) =====
    #if defined(_REFRACTION) && defined(UNITY_PASS_FORWARDBASE)
        // Calculate screen UV from screen position
        float2 screenUV = i.screenPos.xy / i.screenPos.w;

        // Apply refraction mask
        float refractionMask = 1.0;
        #ifdef _REFRACTION_MASK
            refractionMask = tex2D(_RefractionMask, uv).r;
            refractionMask = ApplySoftMask(refractionMask);
        #endif

        // Calculate distorted UV based on surface normal and refraction settings
        float2 distortedUV = ApplyRefractionDistortion(
            screenUV,
            worldNormal,
            viewDir,
            _RefractionIntensity * refractionMask,
            _RefractionIndex,
            _RefractionBlur
        );

        // Sample background with optional blur
        float3 refractedColor = SampleGrabTextureWithBlur(distortedUV, _RefractionBlur);

        // Blend refracted color with current color based on alpha and refraction intensity
        // Higher intensity = more refraction visible
        float refractionBlend = _RefractionIntensity * refractionMask * (1.0 - col.a);
        col.rgb = lerp(col.rgb, refractedColor, saturate(refractionBlend));
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
            half pulse = sin(_Time.y * _EmissionPulseSpeed) * 0.5 + 0.5;
            pulse = lerp(1.0 - _EmissionPulseAmplitude, 1.0, pulse);
            emission *= pulse;
        #endif

        // Apply mask texture with soft blending
        half emissionMask = 1.0;
        #ifdef _EMISSION_MASK
            emissionMask = tex2D(_EmissionMask, uv).r;
            emissionMask = ApplySoftMask(emissionMask); // Smooth mask transitions
        #endif
        emission *= emissionMask;

        // Apply Glow/Bloom effect - Optimized: removed branching, use cached luminance
        half emissionLum = CALC_LUMINANCE(emission);
        half3 glow = emission * emissionLum * _EmissionGlow * 2.0;
        emission += glow * step(0.001, _EmissionGlow); // Conditional add without branch

        // Use safe additive blending to prevent white-out
        half emissionStrength = saturate(length(emission) * emissionMask * 0.3);
        col.rgb = SafeAdditiveBlend(col.rgb, emission, emissionStrength);
    #endif

    // ===== Virtual Expression - Hue Shift =====
    // Optimized: removed branching (ApplyHueShift handles _HueShift=0 efficiently)
    #ifdef _HUE_SHIFT
        col.rgb = ApplyHueShift(col.rgb, _HueShift);
    #endif

    // ===== Glitter Effect =====
    #ifdef _GLITTER
        half3 glitter = GlitterEffect(uv, i.worldPos, viewDir, worldNormal);
        col.rgb = SafeAdditiveBlend(col.rgb, glitter, 1.0);
    #endif

    // ===== Iridescence Effect =====
    #ifdef _IRIDESCENCE
        half3 iridescence = IridescenceEffect(worldNormal, viewDir, uv);
        col.rgb = SafeAdditiveBlend(col.rgb, iridescence, 1.0);
    #endif

    // ===== Virtual Expression - Dissolve =====
    #ifdef _DISSOLVE
        // Early exit if dissolve amount is 0 (no effect)
        if (_DissolveAmount > 0.0)
        {
            half dissolveMaskValue = 1.0;

            // Apply mask texture
            #ifdef _DISSOLVE_MASK
                dissolveMaskValue = tex2D(_DissolveMask, uv).r;
            #endif

            float2 dissolveResult = CalculateDissolve(uv, _DissolveAmount, _DissolveEdgeWidth);
            half dissolveAlpha = dissolveResult.x;
            half edgeGlow = dissolveResult.y;

            // Apply mask to edge glow and dissolve effect
            edgeGlow *= dissolveMaskValue;

            // Apply edge glow with safe additive blending
            if (edgeGlow > 0.0)
            {
                half3 dissolveGlow = _DissolveEdgeColor.rgb * edgeGlow * _DissolveEdgeIntensity;
                half dissolveStrength = saturate(edgeGlow * _DissolveEdgeIntensity * 0.5);
                col.rgb = SafeAdditiveBlend(col.rgb, dissolveGlow, dissolveStrength);
            }

            // Clip pixels based on dissolve amount and mask
            clip(dissolveAlpha + (1.0 - dissolveMaskValue));
        }
    #endif

    // ===== Alpha Mask =====
    // Apply alpha mask for partial transparency control
    #ifdef _ALPHA_MASK
        half alphaMask = tex2D(_AlphaMask, uv).r;
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
