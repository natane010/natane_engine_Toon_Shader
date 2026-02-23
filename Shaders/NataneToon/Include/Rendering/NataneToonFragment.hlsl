#ifndef NATANE_TOON_FRAGMENT_INCLUDED
#define NATANE_TOON_FRAGMENT_INCLUDED

// Fragment Shader
// Main pixel/fragment rendering function
// Optimized: half precision for better performance, cached luminance calculations
half4 frag(v2f i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

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
    {
        float2 _2ndAnimUV = uv;
        if (dot(_2ndTexScrollSpeed.xy, _2ndTexScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_2ndTexRotateSpeed) > EPSILON)
        {
            _2ndAnimUV = AnimateUV(uv, _2ndTexScrollSpeed.xy, _2ndTexRotateSpeed);
        }
        col.rgb = ApplyMakeupTexture(col.rgb, _2ndTex, _2ndTexMask, _2ndAnimUV, uv,
            _2ndTexHueShift, _2ndTexSaturation, _2ndTexValue,
            _2ndTexIntensity, _2ndTexBlendMode,
            true
        );
    }
    #endif

    #ifdef _3RD_TEXTURE
    {
        float2 _3rdAnimUV = uv;
        if (dot(_3rdTexScrollSpeed.xy, _3rdTexScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_3rdTexRotateSpeed) > EPSILON)
        {
            _3rdAnimUV = AnimateUV(uv, _3rdTexScrollSpeed.xy, _3rdTexRotateSpeed);
        }
        col.rgb = ApplyMakeupTexture(col.rgb, _3rdTex, _3rdTexMask, _3rdAnimUV, uv,
            _3rdTexHueShift, _3rdTexSaturation, _3rdTexValue,
            _3rdTexIntensity, _3rdTexBlendMode,
            true
        );
    }
    #endif

    #ifdef _4TH_TEXTURE
    {
        float2 _4thAnimUV = uv;
        if (dot(_4thTexScrollSpeed.xy, _4thTexScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_4thTexRotateSpeed) > EPSILON)
        {
            _4thAnimUV = AnimateUV(uv, _4thTexScrollSpeed.xy, _4thTexRotateSpeed);
        }
        col.rgb = ApplyMakeupTexture(col.rgb, _4thTex, _4thTexMask, _4thAnimUV, uv,
            _4thTexHueShift, _4thTexSaturation, _4thTexValue,
            _4thTexIntensity, _4thTexBlendMode,
            true
        );
    }
    #endif

    #ifdef _5TH_TEXTURE
    {
        float2 _5thAnimUV = uv;
        if (dot(_5thTexScrollSpeed.xy, _5thTexScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_5thTexRotateSpeed) > EPSILON)
        {
            _5thAnimUV = AnimateUV(uv, _5thTexScrollSpeed.xy, _5thTexRotateSpeed);
        }
        col.rgb = ApplyMakeupTexture(col.rgb, _5thTex, _5thTexMask, _5thAnimUV, uv,
            _5thTexHueShift, _5thTexSaturation, _5thTexValue,
            _5thTexIntensity, _5thTexBlendMode,
            true
        );
    }
    #endif

    // ===== Normal Mapping =====
    // Optimization: Skip normalization if no normal mapping (already normalized in vertex shader)
    #ifdef _NORMALMAP
        float2 bumpUV = uv;
        if (dot(_BumpMapScrollSpeed.xy, _BumpMapScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_BumpMapRotateSpeed) > EPSILON)
        {
            bumpUV = AnimateUV(uv, _BumpMapScrollSpeed.xy, _BumpMapRotateSpeed);
        }
        half3 normalMap = UnpackScaleNormal(tex2D(_BumpMap, bumpUV), _BumpScale);
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
    // Unity's built-in attenuation handles directional/point/spot and shadow maps consistently.
    half3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

    // ===== Shadow Map Smoothing (PCF + Adaptive) =====
    // シャドウマップのジャギーを軽減
    // ディレクショナル: PCF 9-tap で本物のアンチエイリアシング
    // ポイント/スポット: 適応型 smoothstep でエッジをぼかす
    if (_ShadowSmoothing > 0.001)
    {
        #if defined(UNITY_PASS_FORWARDBASE) && defined(SHADOWS_SCREEN) && !defined(UNITY_NO_SCREENSPACE_SHADOWS)
            // --- Directional Light: PCF 9-tap on screen-space shadow map ---
            float2 shadowUV = i._ShadowCoord.xy / i._ShadowCoord.w;
            float2 texelSize = _ShadowSmoothing * 3.0 / _ScreenParams.xy;

            half pcfShadow = 0;
            [unroll]
            for (int sx = -1; sx <= 1; sx++)
            {
                [unroll]
                for (int sy = -1; sy <= 1; sy++)
                {
                    pcfShadow += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ShadowMapTexture, shadowUV + float2(sx, sy) * texelSize).r;
                }
            }
            atten = pcfShadow / 9.0;
        #else
            // --- Point/Spot Light: Adaptive smoothstep ---
            half attenDeriv = fwidth(atten);
            half adaptiveCenter = clamp(atten, 0.1, 0.9);
            half smoothWidth = max(attenDeriv, _ShadowSmoothing * 0.3);
            atten = smoothstep(adaptiveCenter - smoothWidth, adaptiveCenter + smoothWidth, atten);
        #endif
    }

    // Apply shadow receive strength (allows controlling how much shadows affect this material)
    // マスクの判定を反転: 白（1.0）= 影を受けない、黒（0.0）= 影を受ける
    half shadowStrength = (1.0 - shadowReceiveMask) * _ShadowReceive;
    atten = lerp(1.0, atten, shadowStrength);

    half3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
    half ndotl = dot(worldNormal, lightDir);

    // ===== SDF Shadow Map =====
    // Apply SDF shadow to ndotl before lighting calculations
    ndotl = ApplySDFShadow(uv, ndotl, lightDir, i.worldPos);

    // ===== Backlight Calculation =====
    // Calculate light coming from behind the object (rim-like effect)
    half backlight = 0.0;
    #ifdef UNITY_PASS_FORWARDBASE
        half backlightDot = max(0.0, dot(worldNormal, -lightDir));
        float backlightPowerBlurred = max(0.5, 4.0 * (1.0 - _BacklightBlur * 0.8));
        backlight = pow(backlightDot, backlightPowerBlurred) * _BacklightIntensity;
    #endif

    // ===== Toon/Ramp Shading =====
    // Calculate base light term
    half lightTerm = ndotl;

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
        float ditherStrengthBlurred = saturate(_DitheringStrength + _DitheringBlur * 0.5);
        half ditherEffect = (ditherPattern - 0.5) * ditherStrengthBlurred * ditherRange;
        half preDitherLightTerm = lightTerm;
        lightTerm = saturate(lightTerm + ditherEffect);
        lightTerm = lerp(preDitherLightTerm, lightTerm, _DitheringBlend);
    #endif

    // Apply unified lighting softness controls (Light Blend / Highlight Softness).
    lightTerm = ApplyLightBlend(lightTerm);

    // ===== Toon/Ramp Shading =====
    half3 lighting;
    half shadingValue;
    half3 shadowColor;
    half aoForIndirect = 1.0;

    #ifdef _USE_RAMP
        // ===== AO (apply before ramp sampling for accurate shadow contribution) =====
        half rampInput = lightTerm;
        #ifdef _USE_AO
            half ao = SampleTex2DBlur1(_AOMap, uv, _AOBlur);
            ao = ApplySoftMask(ao);
            half aoEffect = lerp(1.0, ao, _AOIntensity);
            aoForIndirect = lerp(1.0, ao, _AOIntensity * 0.5);
            rampInput *= aoEffect;
        #endif
        // Shadow attenuation (separated from NdotL for clean toon boundaries)
        rampInput *= atten;
        // Use ramp texture for custom shadow gradients
        lighting = RampShading(rampInput);
        shadingValue = saturate(rampInput + clamp(_ShadowOffset, -1.0, 1.0));
        shadowColor = lighting;
    #else
        // Choose between Toon and Gradient shading modes - Optimized: no branching
        // Calculate both modes and blend based on _ShadingMode
        half toonValue = ToonShading(lightTerm, _ShadowSteps, _ShadowSharpness);

        // ===== Shadow Step Smoothing =====
        // 多段階トゥーンシェーディングの階調をなじませる
        // 量子化されたステップを連続的なライティングに向けてブレンド
        if (_ShadowSmoothing > 0.001)
        {
            half continuousShading = saturate(lightTerm + clamp(_ShadowOffset, -1.0, 1.0));
            toonValue = lerp(toonValue, continuousShading, _ShadowSmoothing);
        }

        half gradientValue = GradientShading(lightTerm, _ShadingGradientWidth);

        // Blend between modes: 0 = Toon, 1 = Gradient
        shadingValue = lerp(toonValue, gradientValue, step(HALF_VALUE, _ShadingMode));

        // Apply Shading Grade Map before final lighting
        shadingValue = ApplyShadingGradeMap(uv, shadingValue);

        // ===== AO (apply to shading stage for accurate shadow contribution) =====
        #ifdef _USE_AO
            half ao = SampleTex2DBlur1(_AOMap, uv, _AOBlur);
            ao = ApplySoftMask(ao);
            half aoEffect = lerp(1.0, ao, _AOIntensity);
            aoForIndirect = lerp(1.0, ao, _AOIntensity * 0.5);
            half preShadingAO = shadingValue;
            shadingValue *= aoEffect;
            // Apply AO blend (how much AO affects the shading)
            shadingValue = lerp(preShadingAO, shadingValue, _AOBlend);
        #endif

        // ===== Shadow Attenuation (separated from NdotL for clean toon boundaries) =====
        shadingValue *= atten;

        // Vibrant shadow color mixing for anime look
        half3 litColor = 1.0;

        // Use multi-shadow tone settings (or fallback to single shadow color)
        shadowColor = MultiToneShadowColor(shadingValue, half3(1.0, 1.0, 1.0));

        // Optional texture-driven shadow tinting for richer NPR shadow control
        half3 shadowColorTex = tex2D(_ShadowColorTex, uv).rgb;
        half3 texturedShadowColor = shadowColor * shadowColorTex;
        shadowColor = lerp(shadowColor, texturedShadowColor, saturate(_ShadowColorTexStrength));

        // Preserve hue and saturation better in shadows
        lighting = lerp(shadowColor, litColor, shadingValue);
    #endif

    // Apply shadow max darkness limit (prevents shadows from being too black)
    shadowColor = max(shadowColor, saturate(_ShadowMaxDarkness));

    // ===== Backlight Calculation (used in ForwardBase Step 4) =====
    // (backlight was already calculated above at line ~128)

    // ===== ForwardBase: Natural Lighting Pipeline =====
    #ifdef UNITY_PASS_FORWARDBASE
        // ========== STEP 1: Indirect Light ==========
        half3 indirectResult = half3(0, 0, 0);
        float3 ambient = float3(0, 0, 0);

        #ifdef _USE_LIGHT_VOLUME
            half3 preLightVolume = lighting;
            // Sample Light Volume SH coefficients
            float3 L0, L1r, L1g, L1b;
            LightVolumeSH(i.worldPos, L0, L1r, L1g, L1b);

            // Evaluate direct and indirect lighting from Light Volume
            float3 directLightLV = LightVolumeEvaluate(worldNormal, L0, L1r, L1g, L1b);
            float3 indirectLightLV = LightVolumeEvaluate(-worldNormal, L0, L1r, L1g, L1b);

            // Gentle clamp to prevent over-brightening
            directLightLV = min(directLightLV, float3(1.1, 1.1, 1.1));
            indirectLightLV = min(indirectLightLV, float3(1.1, 1.1, 1.1));

            // Apply intensity controls
            directLightLV *= _LightVolumeIntensity;
            indirectLightLV *= _LightVolumeIntensity;

            // Apply Shadow Receive Mask to indirect Light Volume
            float lvInfluence = (1.0 - shadowReceiveMask);
            indirectLightLV *= lvInfluence;

            indirectResult = indirectLightLV;

            // LV使用時はUnityのAmbient Colorを使用しない（LVが環境光を提供するため）
            ambient = float3(0, 0, 0);
        #else
            // Fallback: Unity Light Probes
            // L0 (uniform ambient) for indirect
            float3 shAverage = ShadeSH9(float4(0, 0, 0, 1));
            indirectResult = max(0, shAverage);

            // Directional SH for ambient contribution
            float3 shDirect = ShadeSH9(float4(worldNormal, 1.0));
            float3 shIndirect = ShadeSH9(float4(-worldNormal, 1.0));
            ambient = lerp(shIndirect, shDirect, 0.85);
            ambient *= _IndirectLightIntensity * _GIIntensity;
        #endif

        // Indirect light minimum color (prevents completely dark characters in dark worlds)
        indirectResult = max(indirectResult, _IndirectLightMinColor.rgb);
        indirectResult *= _IndirectLightIntensity * _GIIntensity;
        // AO on indirect light (50% strength to preserve ambient visibility)
        indirectResult *= aoForIndirect;

        // ========== STEP 2: Shadow Environment Color (environment-aware shadow tinting) ==========
        #ifdef _USE_LIGHT_VOLUME
            shadowColor = lerp(shadowColor, half3(1, 1, 1),
                              saturate(indirectResult * _ShadowEnvStrength));
        #endif

        // ========== STEP 3: Direct Light ==========
        #ifdef _USE_RAMP
            half3 directResult = lighting; // Ramp already provides colored shadow-to-lit
        #else
            half3 directResult = lerp(shadowColor, half3(1, 1, 1), shadingValue);
        #endif

        // Light color application (with LightColorInfluence preservation)
        half lightColorLum = CALC_LUMINANCE(_LightColor0.rgb);
        half3 colorMultiplied = directResult * saturate(_LightColor0.rgb);
        half3 luminanceOnly = directResult * lightColorLum;
        half3 lightColorInfluenced = lerp(luminanceOnly, colorMultiplied, _LightColorInfluence);
        directResult = lightColorInfluenced * max(0.0, _LightIntensity);

        // Light influence clamping
        half directLum = CALC_LUMINANCE(directResult);
        directLum = clamp(directLum, _LightMinInfluence, _LightMaxInfluence);
        half3 directDir = normalize(max(directResult, 0.01));
        directResult = directDir * directLum;

        // ========== STEP 4: Additional Light ==========
        half3 additionalResult = half3(0, 0, 0);

        // Vertex Lights
        #if defined(_PIXEL_VERTEX_LIGHTS) && defined(VERTEXLIGHT_ON)
            additionalResult += CalculateVertexLightsPixelPrecision(
                i.worldPos, worldNormal, _ShadowSteps, _ShadowSharpness);
        #elif defined(VERTEXLIGHT_ON)
            additionalResult += i.vertexLightColor;
        #endif
        additionalResult *= _AdditionalLightIntensity;

        // Backlight (apply blend amount)
        additionalResult += backlight * _BacklightColor.rgb * _LightColor0.rgb * _BacklightBlend;

        // LTCGI (diffuse → additional, specular → saved for later)
        #if defined(_LTCGI)
            float3 ltcgiDiffuse = 0;
            float3 ltcgiSpecular = 0;
            LTCGI_Contribution(i.worldPos, worldNormal, viewDir,
                1.0 - _Smoothness, float2(0, 0),
                ltcgiDiffuse, ltcgiSpecular);
            additionalResult += ltcgiDiffuse * _LTCGIIntensity * _LTCGIBlend;
        #endif

        // ========== STEP 5: Final Composition ==========
        #ifdef _USE_LIGHT_VOLUME
            if (_LightVolumeBlendMode < 0.5) // Add (Legacy)
            {
                lighting = directResult + additionalResult;
                float3 lvAddition = saturate(directLightLV - lighting);
                lighting += lvAddition;
                // Indirect as subtle rim
                float3 viewDirForLV = normalize(_WorldSpaceCameraPos - i.worldPos);
                float rimFactor = 1.0 - saturate(dot(worldNormal, viewDirForLV));
                rimFactor = rimFactor * rimFactor * rimFactor;
                float3 indirectAddition = saturate(indirectLightLV - lighting);
                lighting += indirectAddition * rimFactor;
                lighting += indirectResult;
            }
            else if (_LightVolumeBlendMode < 1.5) // Multiply
            {
                lighting = directResult + additionalResult;
                lighting *= lerp(float3(1, 1, 1), directLightLV, 1.0);
                lighting += indirectResult;
            }
            else if (_LightVolumeBlendMode < 2.5) // Replace
            {
                lighting = lerp(directResult + additionalResult, directLightLV, 1.0);
                lighting += indirectResult;
            }
            else // Natural (>= 2.5) — DEFAULT
            {
                // LV is treated as indirect light; max() ensures environment color is always visible
                half3 totalIndirect = max(indirectResult, directLightLV);
                lighting = max(totalIndirect, directResult + additionalResult);
            }

            // Light Volume Specular (additive on albedo)
            #ifdef _LIGHT_VOLUME_SPECULAR
                float3 lvSpecular = LightVolumeSpecular(col.rgb, _Smoothness, _Metallic,
                    worldNormal, viewDir, L0, L1r, L1g, L1b);
                lvSpecular *= _LightVolumeIntensity * _GIIntensity;
                lvSpecular *= _Glossiness * (1.0 - _MatteEffect);
                float lvSpecStrength = saturate(length(lvSpecular) * 0.5);
                col.rgb = SafeAdditiveBlend(col.rgb, lvSpecular, lvSpecStrength);
            #endif

            lighting = lerp(preLightVolume, lighting, _LightVolumeBlend);
        #else
            // Non-LV: max() composition + directional ambient
            lighting = max(indirectResult, directResult + additionalResult);
            lighting += ambient;
        #endif

        // LTCGI Specular (additive on col after lighting composition)
        #if defined(_LTCGI)
        {
            half3 preLTCGI_col = col.rgb;
            col.rgb += ltcgiSpecular * _LTCGISpecular * _LTCGIIntensity;
            col.rgb = ApplyEffectBlendPost(preLTCGI_col, col.rgb, _LTCGIBlend, _LTCGIBlendMode);
        }
        #endif

    #else
        // ===== ForwardAdd: Additional Light Contribution =====
        lighting = lerp(shadowColor, half3(1, 1, 1), shadingValue);
        half lightColorLum_add = CALC_LUMINANCE(_LightColor0.rgb);
        half3 colorMul_add = lighting * _LightColor0.rgb;
        half3 lumOnly_add = lighting * lightColorLum_add;
        lighting = lerp(lumOnly_add, colorMul_add, _LightColorInfluence);
        lighting *= max(0.0, _LightIntensity);
        lighting *= max(0.0, _AdditionalLightIntensity);
    #endif

    // Store original texture color before lighting application
    half3 originalAlbedo = col.rgb;

    #ifndef UNITY_PASS_FORWARDBASE
        // ForwardAdd pass should output only additional light contribution.
        // Keep this path minimal to avoid over-brightening and reduce per-light cost.
        col.rgb = originalAlbedo * lighting * _Brightness;
    #else
        // Optimized: Cache original luminance (used multiple times)
        half originalLum = CALC_LUMINANCE(originalAlbedo);

        // ===== Improved Color Preservation Lighting =====
        // Instead of directly multiplying, preserve color hue and saturation
        // while applying lighting brightness

        // Cache lighting luminance (already calculated as lightLum above - reuse if possible)
        half lightingLum = CALC_LUMINANCE(lighting);

        // Method 1: Preserve color by applying only luminance change
        // Extract color direction (hue/saturation) from original albedo
        half3 albedoDir = originalAlbedo / max(originalLum, 0.01);

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
    #endif

    // ===== Specular Highlight =====
    #ifdef _SPECULAR
        float specSoftnessBlurred = _SpecularSoftness + _SpecularBlur * 0.3;
        half spec = SpecularHighlight(worldNormal, viewDir, lightDir, _SpecularSize, specSoftnessBlurred);
        half3 specContrib = spec * _SpecularColor.rgb * _LightColor0.rgb * atten;

        // Apply mask texture with soft blending
        float2 specMaskUV = uv;
        if (dot(_SpecularMaskScrollSpeed.xy, _SpecularMaskScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_SpecularMaskRotateSpeed) > EPSILON)
        {
            specMaskUV = AnimateUV(uv, _SpecularMaskScrollSpeed.xy, _SpecularMaskRotateSpeed);
        }
        half specMask = tex2D(_SpecularMask, specMaskUV).r;
        specMask = ApplySoftMask(specMask); // Smooth mask transitions
        specContrib *= specMask;

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            specContrib *= _AdditionalLightIntensity;
        #endif

        // Apply glossiness and matte effect
        half glossFactor = _Glossiness * (1.0 - _MatteEffect);
        specContrib *= glossFactor;

        // Use safe additive blending to prevent white-out
        half3 preSpec = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, specContrib, saturate(spec * 0.5 + 0.5));
        col.rgb = ApplyEffectBlendPost(preSpec, col.rgb, _SpecularBlend, _SpecularBlendMode);
    #endif

    // ===== Hair Specular (Kajiya-Kay) =====
    #ifdef _HAIR_SPECULAR
        half3 hairSpec = HairSpecularHighlight(worldNormal, i.worldTangent, i.worldBinormal,
                                                viewDir, lightDir, uv);
        hairSpec *= _LightColor0.rgb * atten;

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            hairSpec *= _AdditionalLightIntensity;
        #endif

        // Apply glossiness and matte effect
        hairSpec *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        half hairSpecStrength = saturate(length(hairSpec) * 0.5);
        half3 preHairSpec = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, hairSpec, hairSpecStrength);
        col.rgb = ApplyEffectBlendPost(preHairSpec, col.rgb, _HairSpecBlend, _HairSpecBlendMode);
    #endif

    // ===== Subsurface Scattering =====
    #ifdef _SSS
        half thickness = tex2D(_ThicknessMap, uv).r * _ThicknessScale;

        float sssPowerBlurred = max(0.1, _SSSPower * (1.0 - _SSSBlur * 0.8));
        half3 sss = SubsurfaceScattering(worldNormal, lightDir, viewDir, thickness, atten, sssPowerBlurred);

        // Apply mask texture with soft blending
        half sssMask = tex2D(_SSSMask, uv).r;
        sssMask = ApplySoftMask(sssMask); // Smooth mask transitions
        sss *= sssMask;

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            sss *= _AdditionalLightIntensity;
        #endif

        // Use safe additive blending to prevent white-out
        half sssStrength = saturate(length(sss) * 0.5);
        half3 preSSS = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, sss, sssStrength);
        col.rgb = ApplyEffectBlendPost(preSSS, col.rgb, _SSSBlend, _SSSBlendMode);
    #endif

    // ===== Rim Light (ForwardBase only) =====
    #if defined(_RIM_LIGHT) && defined(UNITY_PASS_FORWARDBASE)
        float rimPowerBlurred = max(0.1, _RimPower * (1.0 - _RimBlur * 0.8));
        half3 rim = RimLighting(worldNormal, viewDir, rimPowerBlurred, _RimIntensity);

        // Apply Spread/Glow effect - Optimized: removed branching
        half rimSpreadPower = lerp(rimPowerBlurred, max(0.5, rimPowerBlurred * 0.3), _RimSpread);
        half3 rimGlow = RimLighting(worldNormal, viewDir, rimSpreadPower, _RimIntensity * _RimSpread * 0.5);
        rim += rimGlow * step(0.001, _RimSpread); // Conditional add without branch

        // Apply mask texture with soft blending
        float2 rimMaskUV = uv;
        if (dot(_RimMaskScrollSpeed.xy, _RimMaskScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_RimMaskRotateSpeed) > EPSILON)
        {
            rimMaskUV = AnimateUV(uv, _RimMaskScrollSpeed.xy, _RimMaskRotateSpeed);
        }
        half rimMask = tex2D(_RimMask, rimMaskUV).r;
        rimMask = ApplySoftMask(rimMask); // Smooth mask transitions
        rim *= rimMask;

        if (_RimDirectionRange > 0.001)
        {
            half3 rimDirection1 = normalize(_RimLightDirection.xyz);
            half rimDirectionMask1 = smoothstep(-_RimDirectionRange, _RimDirectionRange, dot(worldNormal, rimDirection1));
            rim *= rimDirectionMask1;
        }

        // Apply glossiness and matte effect
        rim *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        half rimStrength = saturate(length(rim) * 0.5);
        half3 preRim = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, rim, rimStrength);
        col.rgb = ApplyEffectBlendPost(preRim, col.rgb, _RimBlend, _RimBlendMode);
    #endif

    // ===== Rim Light 2 (ForwardBase only) =====
    #if defined(_RIM_LIGHT_2) && defined(UNITY_PASS_FORWARDBASE)
        // Calculate rim factor (stronger at edges)
        half rim2Factor = 1.0 - saturate(dot(worldNormal, viewDir));
        float rim2PowerBlurred = max(0.1, _RimPower2 * (1.0 - _Rim2Blur * 0.8));
        rim2Factor = pow(rim2Factor, rim2PowerBlurred) * _RimIntensity2;
        half3 rim2 = rim2Factor * _RimColor2.rgb;

        // Apply Spread/Glow effect - Optimized: removed branching
        half rim2SpreadPower = lerp(rim2PowerBlurred, max(0.5, rim2PowerBlurred * 0.3), _RimSpread2);
        half rim2SpreadFactor = 1.0 - saturate(dot(worldNormal, viewDir));
        rim2SpreadFactor = pow(rim2SpreadFactor, rim2SpreadPower) * _RimIntensity2 * _RimSpread2 * 0.5;
        rim2 += rim2SpreadFactor * _RimColor2.rgb * step(0.001, _RimSpread2); // Conditional add without branch

        // Apply mask texture with soft blending
        float2 rimMask2UV = uv;
        if (dot(_RimMask2ScrollSpeed.xy, _RimMask2ScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_RimMask2RotateSpeed) > EPSILON)
        {
            rimMask2UV = AnimateUV(uv, _RimMask2ScrollSpeed.xy, _RimMask2RotateSpeed);
        }
        half rimMask2 = tex2D(_RimMask2, rimMask2UV).r;
        rimMask2 = ApplySoftMask(rimMask2); // Smooth mask transitions
        rim2 *= rimMask2;

        if (_RimDirectionRange > 0.001)
        {
            half3 rimDirection2 = normalize(_RimLightDirection.xyz);
            half rimDirectionMask2 = smoothstep(-_RimDirectionRange, _RimDirectionRange, dot(worldNormal, rimDirection2));
            rim2 *= rimDirectionMask2;
        }

        // Apply glossiness and matte effect
        rim2 *= _Glossiness * (1.0 - _MatteEffect);

        // Use fast additive blending (secondary effect)
        half rim2Strength = saturate(length(rim2) * 0.5);
        half3 preRim2 = col.rgb;
        col.rgb = SafeAdditiveBlendFast(col.rgb, rim2, rim2Strength);
        col.rgb = ApplyEffectBlendPost(preRim2, col.rgb, _RimBlend2, _RimBlendMode2);
    #endif

    // ===== Environmental Rim (ForwardBase only) =====
    #if defined(_ENV_RIM) && defined(UNITY_PASS_FORWARDBASE)
        float envRimPowerBlurred = max(0.1, _EnvRimPower * (1.0 - _EnvRimBlur * 0.8));
        half3 envRim = EnvironmentalRim(worldNormal, viewDir, envRimPowerBlurred);

        // Apply mask texture with soft blending
        half envRimMask = tex2D(_EnvRimMask, uv).r;
        envRimMask = ApplySoftMask(envRimMask); // Smooth mask transitions
        envRim *= envRimMask;

        // Apply glossiness and matte effect
        envRim *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        half envRimStrength = saturate(length(envRim) * 0.5);
        half3 preEnvRim = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, envRim, envRimStrength);
        col.rgb = ApplyEffectBlendPost(preEnvRim, col.rgb, _EnvRimBlend, _EnvRimBlendMode);
    #endif

    // ===== MatCap (ForwardBase only) =====
    #if defined(_MATCAP) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap = SampleTex2DBlur3(_MatCapTex, matcapUV, _MatCapBlur) * _MatCapIntensity;

        // Apply mask texture with soft blending
        half matcapMask = tex2D(_MatCapMask, uv).r;
        matcapMask = ApplySoftMask(matcapMask); // Smooth mask transitions
        matcap *= matcapMask;

        // Apply glossiness and matte effect
        matcap *= _Glossiness * (1.0 - _MatteEffect);

        // Blend modes: 0=Add (safe), 1=Multiply, 2=Replace - Optimized: no branching
        half3 preMatCap = col.rgb;
        half matcapStrength = saturate(_MatCapIntensity * matcapMask * HALF_VALUE);
        half3 addResult = SafeAdditiveBlend(col.rgb, matcap, matcapStrength);
        half3 multiplyResult = BlendWithSoftMask(col.rgb, col.rgb * matcap, saturate(_MatCapIntensity * matcapMask));
        half3 replaceResult = BlendWithSoftMask(col.rgb, matcap, saturate(_MatCapIntensity * matcapMask));

        // Select blend mode using lerp
        half isMultiply = step(HALF_VALUE, _MatCapBlendMode) * step(_MatCapBlendMode, 1.5);
        half isReplace = step(1.5, _MatCapBlendMode);
        col.rgb = lerp(addResult, multiplyResult, isMultiply);
        col.rgb = lerp(col.rgb, replaceResult, isReplace);
        col.rgb = lerp(preMatCap, col.rgb, _MatCapBlend);
    #endif

    // ===== MatCap 2 (ForwardBase only) =====
    #if defined(_MATCAP_2) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV2 = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap2 = SampleTex2DBlur3(_MatCapTex2, matcapUV2, _MatCap2Blur) * _MatCapIntensity2;

        half matcapMask2 = tex2D(_MatCapMask2, uv).r;
        matcapMask2 = ApplySoftMask(matcapMask2);
        matcap2 *= matcapMask2;
        matcap2 *= _Glossiness * (1.0 - _MatteEffect);

        half3 preMatCap2 = col.rgb;
        half matcapStrength2 = saturate(_MatCapIntensity2 * matcapMask2 * HALF_VALUE);
        half3 addResult2 = SafeAdditiveBlend(col.rgb, matcap2, matcapStrength2);
        half3 multiplyResult2 = BlendWithSoftMask(col.rgb, col.rgb * matcap2, saturate(_MatCapIntensity2 * matcapMask2));
        half3 replaceResult2 = BlendWithSoftMask(col.rgb, matcap2, saturate(_MatCapIntensity2 * matcapMask2));

        half isMultiply2 = step(HALF_VALUE, _MatCapBlendMode2) * step(_MatCapBlendMode2, 1.5);
        half isReplace2 = step(1.5, _MatCapBlendMode2);
        col.rgb = lerp(addResult2, multiplyResult2, isMultiply2);
        col.rgb = lerp(col.rgb, replaceResult2, isReplace2);
        col.rgb = lerp(preMatCap2, col.rgb, _MatCapBlend2);
    #endif

    // ===== MatCap 3 (ForwardBase only) =====
    #if defined(_MATCAP_3) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV3 = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap3 = SampleTex2DBlur3(_MatCapTex3, matcapUV3, _MatCap3Blur) * _MatCapIntensity3;

        half matcapMask3 = tex2D(_MatCapMask3, uv).r;
        matcapMask3 = ApplySoftMask(matcapMask3);
        matcap3 *= matcapMask3;
        matcap3 *= _Glossiness * (1.0 - _MatteEffect);

        half3 preMatCap3 = col.rgb;
        half matcapStrength3 = saturate(_MatCapIntensity3 * matcapMask3 * HALF_VALUE);
        half3 addResult3 = SafeAdditiveBlend(col.rgb, matcap3, matcapStrength3);
        half3 multiplyResult3 = BlendWithSoftMask(col.rgb, col.rgb * matcap3, saturate(_MatCapIntensity3 * matcapMask3));
        half3 replaceResult3 = BlendWithSoftMask(col.rgb, matcap3, saturate(_MatCapIntensity3 * matcapMask3));

        half isMultiply3 = step(HALF_VALUE, _MatCapBlendMode3) * step(_MatCapBlendMode3, 1.5);
        half isReplace3 = step(1.5, _MatCapBlendMode3);
        col.rgb = lerp(addResult3, multiplyResult3, isMultiply3);
        col.rgb = lerp(col.rgb, replaceResult3, isReplace3);
        col.rgb = lerp(preMatCap3, col.rgb, _MatCapBlend3);
    #endif

    // ===== Cubemap Reflection (ForwardBase only) =====
    #if defined(_REFLECTION) && defined(UNITY_PASS_FORWARDBASE)
        half3 reflection = CubemapReflection(worldNormal, viewDir, _Smoothness, _Metallic);

        // Apply mask texture with soft blending
        half reflectionMask = tex2D(_ReflectionMask, uv).r;
        reflectionMask = ApplySoftMask(reflectionMask); // Smooth mask transitions
        reflection *= reflectionMask;

        // Apply glossiness and matte effect
        reflection *= _Glossiness * (1.0 - _MatteEffect);

        // Use safe additive blending to prevent white-out
        half reflectionStrength = saturate(length(reflection) * reflectionMask * 0.5);
        half3 preReflection = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, reflection, reflectionStrength);
        col.rgb = lerp(preReflection, col.rgb, _ReflectionBlend);
    #endif

    // ===== Refraction (ForwardBase only) =====
    #if defined(_REFRACTION) && defined(UNITY_PASS_FORWARDBASE)
        // Calculate screen UV from screen position
        float2 screenUV = i.screenPos.xy / i.screenPos.w;

        // Apply refraction mask
        float refractionMask = tex2D(_RefractionMask, uv).r;
        refractionMask = ApplySoftMask(refractionMask);

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
        half3 preRefraction = col.rgb;
        col.rgb = lerp(col.rgb, refractedColor, saturate(refractionBlend));
        col.rgb = ApplyEffectBlendPost(preRefraction, col.rgb, _RefractionBlend, _RefractionBlendMode);
    #endif

    // ===== Emission (ForwardBase only) =====
    #if defined(_EMISSION) && defined(UNITY_PASS_FORWARDBASE)
        float2 emissionUV = uv;

        // Apply UV animation (scroll XY + rotation)
        if (abs(_EmissionScrollSpeed) > 0.001 || abs(_EmissionScrollSpeedY) > 0.001 || abs(_EmissionRotateSpeed) > 0.001)
        {
            emissionUV = AnimateUV(uv, float2(_EmissionScrollSpeed, _EmissionScrollSpeedY), _EmissionRotateSpeed);
        }

        half3 emission = SampleTex2DBlur3(_EmissionMap, emissionUV, _EmissionBlur) * _EmissionColor.rgb;

        // Apply pulse animation
        if (_EmissionPulseSpeed > 0.001)
        {
            half pulse = sin(_Time.y * _EmissionPulseSpeed) * 0.5 + 0.5;
            pulse = lerp(1.0 - _EmissionPulseAmplitude, 1.0, pulse);
            emission *= pulse;
        }

        // Apply mask texture with soft blending
        float2 emMaskUV = uv;
        if (dot(_EmissionMaskScrollSpeed.xy, _EmissionMaskScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_EmissionMaskRotateSpeed) > EPSILON)
        {
            emMaskUV = AnimateUV(uv, _EmissionMaskScrollSpeed.xy, _EmissionMaskRotateSpeed);
        }
        half emissionMask = tex2D(_EmissionMask, emMaskUV).r;
        emissionMask = ApplySoftMask(emissionMask); // Smooth mask transitions
        emission *= emissionMask;

        // Apply Glow/Bloom effect - Optimized: removed branching, use cached luminance
        half emissionLum = CALC_LUMINANCE(emission);
        half3 glow = emission * emissionLum * _EmissionGlow * 2.0;
        emission += glow * step(0.001, _EmissionGlow); // Conditional add without branch

        // Use safe additive blending to prevent white-out
        half emissionStrength = saturate(length(emission) * emissionMask * 0.3);
        half3 preEmission = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, emission, emissionStrength);
        col.rgb = ApplyEffectBlendPost(preEmission, col.rgb, _EmissionBlend, _EmissionBlendMode);
    #endif

    // ===== Virtual Expression - Hue Shift =====
    // Optimized: removed branching (ApplyHueShift handles _HueShift=0 efficiently)
    #if defined(_HUE_SHIFT) && defined(UNITY_PASS_FORWARDBASE)
        half3 preHue = col.rgb;
        float hueShiftBlurred = _HueShift * (1.0 - _HueShiftBlur * 0.7);
        col.rgb = ApplyHueShift(col.rgb, hueShiftBlurred);
        col.rgb = lerp(preHue, col.rgb, _HueShiftBlend);
    #endif

    // ===== AudioLink Integration (ForwardBase only) =====
    #if defined(_AUDIOLINK) && defined(UNITY_PASS_FORWARDBASE)
    {
        half3 preAL = col.rgb;
        // AudioLink Emission - modulate emission brightness with audio
        if (_AudioLinkEmissionIntensity > 0.001)
        {
            half alEmission = SampleAudioLink(_AudioLinkEmissionBand);
            #if defined(_EMISSION)
                col.rgb = SafeAdditiveBlendFast(col.rgb, _EmissionColor.rgb * alEmission * _AudioLinkEmissionIntensity, 1.0);
            #endif
        }

        // AudioLink Rim - audio-reactive rim light (requires Rim Light feature)
        #if defined(_RIM_LIGHT)
        if (_AudioLinkRimIntensity > 0.001)
        {
            half alRim = SampleAudioLink(_AudioLinkRimBand);
            half alRimFactor = 1.0 - saturate(dot(worldNormal, viewDir));
            alRimFactor = alRimFactor * alRimFactor * alRimFactor;
            col.rgb = SafeAdditiveBlendFast(col.rgb, _RimColor.rgb * alRim * _AudioLinkRimIntensity * alRimFactor, 1.0);
        }
        #endif

        // AudioLink Hue Shift - audio-reactive color shift
        if (_AudioLinkHueShiftIntensity > 0.001)
        {
            half alHue = SampleAudioLink(_AudioLinkHueBand);
            col.rgb = ApplyHueShift(col.rgb, alHue * _AudioLinkHueShiftIntensity);
        }

        // AudioLink Dissolve - audio-reactive dissolve (requires Dissolve feature)
        #if defined(_DISSOLVE)
        if (_AudioLinkDissolveIntensity > 0.001)
        {
            half alDissolve = SampleAudioLink(_AudioLinkDissolveBand);
            float2 alDissolveUV = uv;
            if (dot(_DissolveTexScrollSpeed.xy, _DissolveTexScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_DissolveTexRotateSpeed) > EPSILON)
            {
                alDissolveUV = AnimateUV(uv, _DissolveTexScrollSpeed.xy, _DissolveTexRotateSpeed);
            }
            float2 alDissolveResult = CalculateDissolve(alDissolveUV, alDissolve * _AudioLinkDissolveIntensity, 0.1);
            half3 alDissolveGlow = _DissolveEdgeColor.rgb * alDissolveResult.y * 2.0;
            col.rgb = SafeAdditiveBlendFast(col.rgb, alDissolveGlow, saturate(alDissolveResult.y));
        }
        #endif
        col.rgb = ApplyEffectBlendPost(preAL, col.rgb, _AudioLinkBlend, _AudioLinkBlendMode);
    }
    #endif

    // ===== Glitter Effect =====
    #if defined(_GLITTER) && defined(UNITY_PASS_FORWARDBASE)
        float2 glitterMaskUV = uv;
        if (dot(_GlitterMaskScrollSpeed.xy, _GlitterMaskScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_GlitterMaskRotateSpeed) > EPSILON)
        {
            glitterMaskUV = AnimateUV(uv, _GlitterMaskScrollSpeed.xy, _GlitterMaskRotateSpeed);
        }
        half3 glitter = GlitterEffect(glitterMaskUV, i.worldPos, viewDir, worldNormal, _GlitterBlur);
        half3 preGlitter = col.rgb;
        col.rgb = SafeAdditiveBlendFast(col.rgb, glitter, 1.0);
        col.rgb = ApplyEffectBlendPost(preGlitter, col.rgb, _GlitterBlend, _GlitterBlendMode);
    #endif

    // ===== Iridescence Effect =====
    #if defined(_IRIDESCENCE) && defined(UNITY_PASS_FORWARDBASE)
        float iridSizeBlurred = lerp(_IridescenceSize, _IridescenceSize * 3.0, _IridescenceBlur);
        half3 iridescence = IridescenceEffect(worldNormal, viewDir, uv, iridSizeBlurred);
        half3 preIridescence = col.rgb;
        col.rgb = SafeAdditiveBlendFast(col.rgb, iridescence, 1.0);
        col.rgb = ApplyEffectBlendPost(preIridescence, col.rgb, _IridescenceBlend, _IridescenceBlendMode);
    #endif

    // ===== Water Drip Effect (ForwardBase only) =====
    #if defined(_WATER_DRIP) && defined(UNITY_PASS_FORWARDBASE)
    {
        float2 dripMaskUV = uv;
        if (dot(_DripMaskScrollSpeed.xy, _DripMaskScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_DripMaskRotateSpeed) > EPSILON)
        {
            dripMaskUV = AnimateUV(uv, _DripMaskScrollSpeed.xy, _DripMaskRotateSpeed);
        }
        half dripMaskValue = tex2D(_DripMask, dripMaskUV).r;
        dripMaskValue = ApplySoftMask(dripMaskValue);

        float dripSharpnessBlurred = max(0.1, _DripSharpness * (1.0 - _DripBlur * 0.8));
        half3 drip = CalculateDripEffectFast(
            i.worldPos, _Time.y, _DripColor.rgb, _DripSpeed,
            _DripDensity, _DripSize, _DripTrailLength,
            _DripIntensity, dripSharpnessBlurred
        );
        drip *= dripMaskValue;
        half3 preDrip = col.rgb;
        col.rgb = SafeAdditiveBlendFast(col.rgb, drip, 1.0);
        col.rgb = ApplyEffectBlendPost(preDrip, col.rgb, _DripBlend, _DripBlendMode);
    }
    #endif

    // ===== Hologram Effect (ForwardBase only) =====
    #if defined(_HOLOGRAM) && defined(UNITY_PASS_FORWARDBASE)
    {
        half3 preHolo = col.rgb;
        half preHoloAlpha = col.a;
        float2 holoUV = uv;

        // Noise distortion
        #if defined(_HOLOGRAM_NOISE)
            holoUV = CalculateHologramNoiseDistortion(holoUV,
                _HologramNoiseIntensity, _HologramNoiseSpeed);
        #endif

        // Hologram mask
        float2 holoMaskUV = uv;
        if (dot(_HologramMaskScrollSpeed.xy, _HologramMaskScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_HologramMaskRotateSpeed) > EPSILON)
        {
            holoMaskUV = AnimateUV(uv, _HologramMaskScrollSpeed.xy, _HologramMaskRotateSpeed);
        }
        half holoMask = tex2D(_HologramMask, holoMaskUV).r;

        // Multi-layer scanline (blur widens scanline width for softer effect)
        float holoWidthBlurred = _HologramScanlineWidth + _HologramBlur * 0.3;
        half scanline = CalculateHologramScanline(holoUV,
            _HologramScanlineSpeed, _HologramScanlineIntensity,
            _HologramScanlineDensity, holoWidthBlurred);
        col.rgb *= lerp(1.0, scanline, holoMask);

        // Fresnel edge glow
        half3 edgeGlow = CalculateHologramEdgeGlow(
            worldNormal, viewDir, _HologramColor.rgb,
            _HologramEdgeGlowPower, _HologramEdgeGlowIntensity);
        col.rgb += edgeGlow * holoMask;

        // Hologram color / monochrome
        col.rgb = lerp(col.rgb,
            ApplyHologramColor(col.rgb, _HologramColor.rgb, _HologramMonochrome),
            holoMask);

        // Flicker
        half flicker = CalculateHologramFlicker(
            _HologramFlickerSpeed, _HologramFlickerAmount);
        col.a *= lerp(1.0, flicker, holoMask);

        // Hologram alpha (Fresnel-linked transparency)
        col.a = lerp(col.a,
            CalculateHologramAlpha(worldNormal, viewDir, col.a, _HologramAlpha),
            holoMask);
        col.rgb = ApplyEffectBlendPost(preHolo, col.rgb, _HologramBlend, _HologramBlendMode);
        col.a = ApplyEffectBlendPostAlpha(preHoloAlpha, col.a, _HologramBlend);
    }
    #endif

    // ===== Glitch Effect (ForwardBase only) =====
    #if defined(_GLITCH) && defined(UNITY_PASS_FORWARDBASE)
    {
        half3 preGlitch = col.rgb;
        // Glitch trigger (random occurrence)
        half glitchTrigger = step(1.0 - _GlitchFrequency,
            frac(sin(_Time.y * _GlitchSpeed) * 43758.5453));

        if (glitchTrigger > 0.5)
        {
            // UV distortion
            float2 glitchUV = CalculateGlitchUV(uv,
                _GlitchIntensity, _GlitchSpeed, _GlitchBlockSize);

            // RGB split
            half3 splitColor = CalculateGlitchRGBSplit(
                col.rgb, glitchUV, _MainTex, _GlitchRGBSplitIntensity);
            col.rgb = lerp(col.rgb, splitColor, _GlitchIntensity);
        }
        // Blur dampens glitch distortion by blending back towards original
        col.rgb = lerp(col.rgb, preGlitch, _GlitchBlur * 0.5);
        col.rgb = ApplyEffectBlendPost(preGlitch, col.rgb, _GlitchBlend, _GlitchBlendMode);
    }
    #endif

    // ===== Decal System (ForwardBase only) =====
    #if defined(_DECAL) && defined(UNITY_PASS_FORWARDBASE)
        float2 decalUV = CalculateDecalUV(uv, _DecalPosition.xy, _DecalRotation, _DecalScale);
        // Only apply within decal bounds (CalculateDecalUV returns -1,-1 if out of bounds)
        half decalInBounds = step(0.0, decalUV.x) * step(decalUV.x, 1.0) * step(0.0, decalUV.y) * step(decalUV.y, 1.0);
        if (decalInBounds > 0.5)
        {
            half3 preDecal = col.rgb;
            half4 decalSample = SampleTex2DBlur(_DecalTex, decalUV, _DecalBlur) * _DecalColor;
            half decalAlpha = decalSample.a;
            // Blend modes: 0=Add, 1=Multiply, 2=Overlay, 3=Replace
            half3 decalAdd = SafeAdditiveBlendFast(col.rgb, decalSample.rgb * decalAlpha, 1.0);
            half3 decalMul = lerp(col.rgb, col.rgb * decalSample.rgb, decalAlpha);
            half3 decalReplace = lerp(col.rgb, decalSample.rgb, decalAlpha);

            half isDecalMul = step(0.5, _DecalBlendMode) * step(_DecalBlendMode, 1.5);
            half isDecalReplace = step(2.5, _DecalBlendMode);
            col.rgb = lerp(decalAdd, decalMul, isDecalMul);
            col.rgb = lerp(col.rgb, decalReplace, isDecalReplace);
            col.rgb = lerp(preDecal, col.rgb, _DecalBlend);
        }
    #endif

    // ===== Virtual Expression - Dissolve =====
    #ifdef _DISSOLVE
        // Early exit if dissolve amount is 0 (no effect)
        if (_DissolveAmount > 0.0)
        {
            half dissolveMaskValue = 1.0;

            // Apply mask texture
            dissolveMaskValue = tex2D(_DissolveMask, uv).r;

            float dissolveEdgeBlurred = _DissolveEdgeWidth + _DissolveBlur * 0.15;
            float2 dissolveUV = uv;
            if (dot(_DissolveTexScrollSpeed.xy, _DissolveTexScrollSpeed.xy) > (EPSILON * EPSILON) || abs(_DissolveTexRotateSpeed) > EPSILON)
            {
                dissolveUV = AnimateUV(uv, _DissolveTexScrollSpeed.xy, _DissolveTexRotateSpeed);
            }
            float2 dissolveResult = CalculateDissolve(dissolveUV, _DissolveAmount, dissolveEdgeBlurred);
            half dissolveAlpha = dissolveResult.x;
            half edgeGlow = dissolveResult.y;

            // Apply mask to edge glow and dissolve effect
            edgeGlow *= dissolveMaskValue;

            // Apply edge glow with safe additive blending (branchless).
            half3 dissolveGlow = _DissolveEdgeColor.rgb * edgeGlow * _DissolveEdgeIntensity;
            half dissolveStrength = saturate(edgeGlow * _DissolveEdgeIntensity * 0.5);
            half3 preDissolve = col.rgb;
            col.rgb = SafeAdditiveBlend(col.rgb, dissolveGlow, dissolveStrength);
            col.rgb = ApplyEffectBlendPost(preDissolve, col.rgb, _DissolveBlend, _DissolveBlendMode);

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

    // ===== Distance Fade =====
    #ifdef _DISTANCE_FADE
        float fadeRange = _DistFadeBlur * (_DistanceFadeEnd - _DistanceFadeStart) * 0.5;
        half distanceFade = CalculateDistanceFade(i.worldPos, _DistanceFadeStart - fadeRange, _DistanceFadeEnd + fadeRange);

        if (_DistanceFadeMode < 0.5)
        {
            // Alpha mode: smooth fade-out with distance.
            half preDistAlpha = col.a;
            col.a *= distanceFade;
            col.a = ApplyEffectBlendPostAlpha(preDistAlpha, col.a, _DistanceFadeBlend);
        }
        else
        {
            // Simplify mode: hard cull at distance for stronger overdraw reduction.
            clip(distanceFade - 0.001);
        }
    #endif

    // ===== Final Color Blending (Highlight & Shadow Smoothing) =====
    // Apply final smoothing to prevent harsh white/black spots
    // This is applied at the very end before fog for the most natural result
    #ifdef UNITY_PASS_FORWARDBASE
        col.rgb = ApplyFinalColorBlending(col.rgb);
    #endif

    // ===== Dithering Alpha =====
    #ifdef _DITHERING_ALPHA
        float2 ditherScreenUV = i.screenPos.xy / max(i.screenPos.w, 0.0001);
        float2 ditherScreenPos = ditherScreenUV * _ScreenParams.xy;
        clip(ApplyDitheringAlpha(col.a, ditherScreenPos, max(_DitheringAlphaScale, 1.0)));
    #endif

    // ===== Fog =====
    UNITY_APPLY_FOG(i.fogCoord, col);

    return col;
}

#endif // NATANE_TOON_FRAGMENT_INCLUDED
