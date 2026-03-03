#ifndef NATANE_TOON_FRAGMENT_INCLUDED
#define NATANE_TOON_FRAGMENT_INCLUDED

// ===== Fragment Shader Constants =====
#define DIST_FADE_BLUR_SCALE     0.5   // 距離フェードぼかしスケール
#define PCF_TEXEL_SCALE          3.0   // PCF シャドウマップテクセルスケール
#define PCF_SAMPLE_COUNT         9.0   // PCF サンプル合計数
#define SMOOTH_WIDTH_SCALE       0.3   // シャドウスムーズ幅スケール
#define BACKLIGHT_MIN_POWER      0.5   // バックライト最小パワー
#define BACKLIGHT_SCALE          4.0   // バックライト基本スケール
#define BACKLIGHT_BLUR_INFLUENCE 0.8   // バックライトブラー影響度
#define AO_INDIRECT_STRENGTH     0.5   // AO 間接光強度
#define SH_INDIRECT_BLEND        0.85  // SH 間接光ブレンド率

// Fragment Shader
// Main pixel/fragment rendering function
// Optimized: half precision for better performance, cached luminance calculations
half4 frag(v2f i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    // ===== Mirror Control (VRChat) =====
    #ifdef _MIRROR_CONTROL
    {
        // _MirrorMode: 0=Both, 1=Mirror Only, 2=Non-Mirror Only
        if (_MirrorMode > 0.5 && _MirrorMode < 1.5 && _VRChatMirrorMode < 0.5)
            discard; // Mirror-only mode: discard in normal view
        if (_MirrorMode > 1.5 && _VRChatMirrorMode > 0.5)
            discard; // Non-mirror mode: discard in mirror view
    }
    #endif

    // ===== Parallax Mapping (UV Adjustment) =====
    // このセクションの処理:
    // 視差マッピングによりテクスチャ座標を視線方向に基づいてオフセットし、
    // ハイトマップから擬似的な凹凸の奥行き表現を生成する。
    // 全後続テクスチャサンプリングの基準UVとなる。
    float2 uv = i.uv;
    #if defined(_EYE_PARALLAX) && !defined(_PARALLAX)
    {
        // 軽量アイパララックス: ビュー方向のXY成分でUVオフセット
        float3 eyeViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
        float3 eyeViewTS = mul((float3x3)UNITY_MATRIX_V, eyeViewDir);
        float2 eyeOffset = eyeViewTS.xy * _EyeParallaxDepth;
        uv += eyeOffset;
    }
    #endif
    #ifdef _PARALLAX
        float3 tangentViewDir = CalculateTangentViewDir(i.worldPos, i.worldTangent, i.worldBinormal, i.worldNormal);
        uv = ParallaxMapping(i.uv, tangentViewDir);
    #endif

    // ===== UV Animation =====
    float2 mainUV = uv;
    #ifdef _MAIN_TEX_ANIMATION
        mainUV = AnimateUV(uv, _MainTexScrollSpeed.xy, _MainTexRotateSpeed);
    #endif

    // ===== Glitch Stretch (UV modification before main texture sampling) =====
    #if defined(_GLITCH_STRETCH) && defined(UNITY_PASS_FORWARDBASE)
    {
        half stretchMaskVal = tex2D(_GlitchStretchMask, TRANSFORM_TEX(mainUV, _GlitchStretchMask)).r;
        // Mask Scale: マスク値を増幅（1.0=等倍、5.0=5倍ブースト）
        stretchMaskVal = saturate(stretchMaskVal * _GlitchStretchMaskScale);
        half stretchTrigger = step(1.0 - _GlitchStretchFrequency,
            frac(sin(_Time.y * _GlitchStretchSpeed) * 43758.5453));
        if (stretchTrigger > 0.5 && stretchMaskVal > 0.001)
        {
            float2 stretchedUV = CalculateGlitchStretchUV(mainUV,
                _GlitchStretchIntensity * stretchMaskVal, _GlitchStretchSpeed,
                _GlitchStretchBlockSize, _GlitchStretchFrequency);
            mainUV = stretchedUV;
        }
    }
    #endif

    // ===== Texture Sampling =====
    #ifdef _TRIPLANAR
        half4 mainTex = TriplanarSample(_MainTex, i.worldPos, i.worldNormal, _TriplanarScale, _TriplanarBlendSharpness);
    #else
        half4 mainTex = tex2D(_MainTex, mainUV);
    #endif
    half4 col = mainTex * _Color;

    // ===== Gradient Base Color =====
    #ifdef _GRADIENT_BASE_COLOR
    {
        half3 gradColor = CalculateGradientColor(i.worldPos,
            _GradientTopColor.rgb, _GradientBottomColor.rgb,
            _GradientAxis, _GradientSpace, _GradientStart, _GradientEnd);
        half3 preGradient = col.rgb;
        col.rgb *= gradColor;
        col.rgb = ApplyEffectBlendPost(preGradient, col.rgb, _GradientBlend, _GradientBlendMode);
    }
    #endif

    // ===== Makeup/Detail Textures Blending =====
    // このセクションの処理:
    // メインテクスチャの上に2nd〜5thテクスチャを順番に重ね合わせる。
    // 各テクスチャはUVアニメーション、HSV調整、マスク、ブレンドモード
    // （Add/Multiply/Overlay/Screen）を個別に持ち、メイクアップ表現を実現する。
    #ifdef _2ND_TEXTURE
    {
        float2 _2ndAnimUV = AnimateUVIfNeeded(uv, _2ndTexScrollSpeed.xy, _2ndTexRotateSpeed);
        col.rgb = ApplyMakeupTexture(col.rgb, _2ndTex, _2ndTexMask, _2ndAnimUV, uv,
            _2ndTexHueShift, _2ndTexSaturation, _2ndTexValue,
            _2ndTexIntensity, _2ndTexBlendMode,
            true
        );
    }
    #endif

    #ifdef _3RD_TEXTURE
    {
        float2 _3rdAnimUV = AnimateUVIfNeeded(uv, _3rdTexScrollSpeed.xy, _3rdTexRotateSpeed);
        col.rgb = ApplyMakeupTexture(col.rgb, _3rdTex, _3rdTexMask, _3rdAnimUV, uv,
            _3rdTexHueShift, _3rdTexSaturation, _3rdTexValue,
            _3rdTexIntensity, _3rdTexBlendMode,
            true
        );
    }
    #endif

    #ifdef _4TH_TEXTURE
    {
        float2 _4thAnimUV = AnimateUVIfNeeded(uv, _4thTexScrollSpeed.xy, _4thTexRotateSpeed);
        col.rgb = ApplyMakeupTexture(col.rgb, _4thTex, _4thTexMask, _4thAnimUV, uv,
            _4thTexHueShift, _4thTexSaturation, _4thTexValue,
            _4thTexIntensity, _4thTexBlendMode,
            true
        );
    }
    #endif

    #ifdef _5TH_TEXTURE
    {
        float2 _5thAnimUV = AnimateUVIfNeeded(uv, _5thTexScrollSpeed.xy, _5thTexRotateSpeed);
        col.rgb = ApplyMakeupTexture(col.rgb, _5thTex, _5thTexMask, _5thAnimUV, uv,
            _5thTexHueShift, _5thTexSaturation, _5thTexValue,
            _5thTexIntensity, _5thTexBlendMode,
            true
        );
    }
    #endif

    // ===== Surface Cover (Snow/Sand Accumulation) =====
    #ifdef _SURFACE_COVER
    {
        float3 safeCoverDirA = normalize(_CoverDirection.xyz + float3(0, 0.0001, 0));
        float coverDot = dot(i.worldNormal, safeCoverDirA);
        float coverFactor = saturate((coverDot - _CoverThreshold) * _CoverBlendSharpness) * _CoverAmount;
        half4 coverSample = tex2D(_CoverTex, i.worldPos.xz * _CoverTiling) * _CoverColor;
        col.rgb = lerp(col.rgb, coverSample.rgb, coverFactor);
    }
    #endif

    // ===== Screen-Tone Overlay =====
    #ifdef _SCREEN_TONE
    {
        half screenToneMask = SampleTex2DBlur1(_ScreenToneMask, uv, _ScreenToneBlur);
        screenToneMask = ApplySoftMask(screenToneMask);
        half3 preScreenTone = col.rgb;
        col.rgb = ApplyScreenTone(col.rgb, i.pos.xy, screenToneMask);
        col.rgb = ApplyEffectBlendPost(preScreenTone, col.rgb, _ScreenToneBlend, _ScreenToneBlendMode);
    }
    #endif

    // ===== Normal Mapping =====
    // Optimization: Skip normalization if no normal mapping (already normalized in vertex shader)
    #ifdef _NORMALMAP
        float2 bumpUV = AnimateUVIfNeeded(uv, _BumpMapScrollSpeed.xy, _BumpMapRotateSpeed);
        half3 normalMap = UnpackScaleNormal(tex2D(_BumpMap, bumpUV), _BumpScale);
        half3x3 tangentToWorld = half3x3(i.worldTangent, i.worldBinormal, i.worldNormal);
        half3 worldNormal = normalize(mul(normalMap, tangentToWorld));
    #else
        half3 worldNormal = i.worldNormal; // Already normalized in vertex shader
    #endif

    // ===== Detail Map (Secondary UV) =====
    #ifdef _DETAIL_MAP
    {
        float2 detailUV = (_DetailUVSet > 0.5) ? i.uv1 : uv;
        detailUV *= _DetailTiling;
        half4 detailAlbedo = tex2D(_DetailAlbedoMap, detailUV);
        col.rgb = lerp(col.rgb, col.rgb * detailAlbedo.rgb * 2.0, _DetailAlbedoScale * detailAlbedo.a);
        #ifdef _NORMALMAP
            half3 detailNormalTS = UnpackScaleNormal(tex2D(_DetailNormalMap, detailUV), _DetailNormalScale);
            // Transform detail normal from tangent space to world space using TBN matrix
            half3 detailNormalWS = normalize(mul(detailNormalTS, tangentToWorld));
            worldNormal = normalize(lerp(worldNormal, detailNormalWS, _DetailAlbedoScale));
        #endif
    }
    #endif

    // ===== Surface Cover Normal Blending =====
    #ifdef _SURFACE_COVER
    #ifdef _NORMALMAP
    {
        float3 safeCoverDir = normalize(_CoverDirection.xyz + float3(0, 0.0001, 0));
        float coverDotN = dot(worldNormal, safeCoverDir);
        float coverFactorN = saturate((coverDotN - _CoverThreshold) * _CoverBlendSharpness) * _CoverAmount;
        half3 coverNormTS = UnpackNormal(tex2D(_CoverNormalMap, i.worldPos.xz * _CoverTiling));
        // Remap from tangent space (xz projection: tangent=X, bitangent=Z, normal=Y)
        half3 coverNormWS = half3(coverNormTS.x, coverNormTS.z, coverNormTS.y);
        worldNormal = normalize(lerp(worldNormal, coverNormWS, coverFactorN));
    }
    #endif
    #endif

    // ===== Procedural Normal Warping =====
    #ifdef _NORMAL_WARP
    {
        // 1. 球状法線: オブジェクト空間位置を正規化
        float3 sphereNormal = normalize(i.objectPos);
        // ワールド空間に変換
        float3 sphereNormalWS = normalize(mul((float3x3)unity_ObjectToWorld, sphereNormal));

        // 2. 球状法線ブレンド
        worldNormal = normalize(lerp(worldNormal, sphereNormalWS, _NormalRoundness));

        // 3. Y成分フラッテン（ワールド空間で適用）
        if (_NormalFlattenY > 0.001)
        {
            worldNormal.y *= (1.0 - _NormalFlattenY);
            worldNormal = normalize(worldNormal);
        }
    }
    #endif

    // ===== Shadow Receive Mask Setup =====
    // Sample shadow mask once and use it for all shadow-related calculations
    half shadowReceiveMask = 0.0; // Default: fully receive shadows (black = receive shadows)
    #ifdef _SHADOW_RECEIVE_MASK
        shadowReceiveMask = tex2D(_ShadowReceiveMask, uv).r;
        shadowReceiveMask = ApplySoftMask(shadowReceiveMask); // Smooth mask transitions
    #endif

    // ===== Lighting Setup =====
    // このセクションの処理:
    // ライト方向と有効ライトカラーを決定するフォールバックチェーン。
    // 1) ディレクショナルライト → 2) 頂点ライト（ポイント/スポット）
    // → 3) SH Light Probe の順に最適な光源を選択する。
    half3 lightDir;
    half3 effectiveLightColor;

    #ifdef UNITY_PASS_FORWARDBASE
    {
        half dirLightLum = CALC_LUMINANCE(_LightColor0.rgb);
        if (dirLightLum > 0.01)
        {
            // 1. ディレクショナルライトあり: 通常処理
            lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
            effectiveLightColor = _LightColor0.rgb;
        }
        else
        {
            // ディレクショナルライトなし: フォールバックチェーン
            // 2. ポイント/スポットライトから方向を取得（ForwardBase vertex light 配列）
            half3 vlDir, vlColor;
            GetBrightestVertexLight(i.worldPos, vlDir, vlColor);
            half vlLum = CALC_LUMINANCE(vlColor);

            if (vlLum > 0.01)
            {
                lightDir = vlDir;
                effectiveLightColor = vlColor;
            }
            else
            {
                // 3. SH Light Probe から方向を取得
                lightDir = GetSHDominantLightDirection();
                effectiveLightColor = GetSHFallbackLightColor();
            }
        }
    }
    #else
        // ForwardAdd: 常に実ライトを使用
        lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
        effectiveLightColor = _LightColor0.rgb;
    #endif

    // ===== Light Direction Snapping (shadow stabilization) =====
    #ifdef _LIGHT_SNAP
    {
        // Quantize light direction to discrete angles to prevent shadow flickering
        float snapRad = radians(_LightSnapAngle);
        float phi = atan2(lightDir.z, lightDir.x);
        float theta = acos(clamp(lightDir.y, -1.0, 1.0));
        float snappedPhi = round(phi / snapRad) * snapRad;
        float snappedTheta = round(theta / snapRad) * snapRad;
        float3 snappedDir = float3(
            sin(snappedTheta) * cos(snappedPhi),
            cos(snappedTheta),
            sin(snappedTheta) * sin(snappedPhi)
        );
        lightDir = normalize(lerp(lightDir, snappedDir, 1.0 - _LightSnapSmooth));
    }
    #endif

    // ===== Light Color Correction (lilToon互換: LightMinLimit/LightMaxLimit相当) =====
    // ライトカラーの値域をクランプし、過度な明暗を防止する。
    // _LightColorMin (default=0): 暗いワールドでもキャラが見える最低保証
    // _LightColorMax (default=1): 強いライトでもテクスチャが白飛びしない上限
    // _MonochromeLighting (default=0): ライトカラーの色味を除去（グレースケール化）
    effectiveLightColor = clamp(effectiveLightColor, _LightColorMin, _LightColorMax);
    half lightGray = CALC_LUMINANCE(effectiveLightColor);
    effectiveLightColor = lerp(effectiveLightColor, half3(lightGray, lightGray, lightGray), _MonochromeLighting);

    // ===== StandardToon v2: lilToon-compatible light color =====
    #ifdef _STANDARD_TOON
        half3 stLightColor;
        half3 stIndLightColor;
        #ifdef UNITY_PASS_FORWARDBASE
            // lilToon: lightColor = MAINLIGHT + SHToon
            // SHToon = ShadeSH9(lightDir * 0.666666)
            float3 stSHToon = max(0, ShadeSH9(float4(lightDir * 0.666666, 1.0)));
            stLightColor = effectiveLightColor + stSHToon;
            // Re-apply clamping (lilToon clamps after SH addition)
            stLightColor = clamp(stLightColor, _LightColorMin, _LightColorMax);
            // 最低保証: ライトカラーがゼロにならないようにする
            stLightColor = max(stLightColor, half3(0.001, 0.001, 0.001));
            half stGray = CALC_LUMINANCE(stLightColor);
            stLightColor = lerp(stLightColor, half3(stGray, stGray, stGray), _MonochromeLighting);
            // AsUnlit: applied to lightColor directly (lilToon behavior)
            stLightColor = lerp(stLightColor, half3(1, 1, 1), _STAsUnlit);
            // Indirect: SHToonMin (opposite direction)
            stIndLightColor = saturate(ShadeSH9(float4(-lightDir * 0.666666, 1.0)));
        #else
            // ForwardAdd: just use the per-light color
            stLightColor = effectiveLightColor;
            stIndLightColor = half3(0, 0, 0);
        #endif
    #endif

    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

    // ===== Per-Effect Distance Fade (early calculation) =====
    #ifdef _DISTANCE_FADE
        float distFadeBlurRange = _DistFadeBlur * (_DistanceFadeEnd - _DistanceFadeStart) * DIST_FADE_BLUR_SCALE;
        half distanceFade = CalculateDistanceFade(i.worldPos, _DistanceFadeStart - distFadeBlurRange, _DistanceFadeEnd + distFadeBlurRange);

        // Near fade: camera too close → transparent
        if (_NearFadeEnd > _NearFadeStart + 0.001)
        {
            float cameraDist = length(_WorldSpaceCameraPos - i.worldPos);
            float nearFadeBlurRange = _DistFadeBlur * (_NearFadeEnd - _NearFadeStart) * DIST_FADE_BLUR_SCALE;
            half nearFade = saturate(
                (cameraDist - (_NearFadeStart - nearFadeBlurRange)) /
                max((_NearFadeEnd + nearFadeBlurRange) - (_NearFadeStart - nearFadeBlurRange), 0.01)
            );
            distanceFade *= nearFade;
        }
    #endif

    // ===== PCSS / Shadow Map Smoothing (PCF + Adaptive) =====
    // PCSS (Percentage Closer Soft Shadows) — スクリーンスペース近似:
    //   遮蔽物に近い影はシャープ、遠い影はソフトになるコンタクトハードニング効果。
    //   ブロッカー探索 → ペナンブラ推定 → 可変幅PCF の3フェーズ。
    // Shadow Map Smoothing (従来):
    //   ディレクショナルライト: PCF 3x3 (9-tap) フィルタ。
    //   ポイント/スポットライト: fwidth ベースの適応型 smoothstep。
    #if defined(UNITY_PASS_FORWARDBASE) && defined(SHADOWS_SCREEN) && !defined(UNITY_NO_SCREENSPACE_SHADOWS)
        #ifdef _PCSS
        {
            // === PCSS: Percentage Closer Soft Shadows (Screen-Space Approximation) ===
            half pcssOriginalAtten = atten; // 元の atten を保存（Blend 用）
            float2 pcssShadowUV = i._ShadowCoord.xy / i._ShadowCoord.w;
            float pcssReceiverDepth = LinearEyeDepth(
                UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, pcssShadowUV).r);

            // Blur: ブロッカー探索半径も拡張して、よりぼかす
            float pcssBlurScale = 1.0 + _PCSSBlur * 4.0;

            // --- Phase 1: Blocker Search ---
            float2 pcssSearchTexelSize = _PCSSBlockerSearchRadius * pcssBlurScale / _ScreenParams.xy;
            float pcssAvgBlockerDepth = 0.0;
            float pcssBlockerCount = 0.0;
            int pcssSamples = clamp((int)_PCSSSampleCount, 8, 32);

            [loop]
            for (int bi = 0; bi < pcssSamples; bi++)
            {
                float2 bOffset = PoissonDisk32[bi] * pcssSearchTexelSize;
                float bShadowVal = UNITY_SAMPLE_SCREENSPACE_TEXTURE(
                    _ShadowMapTexture, pcssShadowUV + bOffset).r;

                if (bShadowVal < 0.5) // In shadow
                {
                    float bSampleDepth = LinearEyeDepth(
                        UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, pcssShadowUV + bOffset).r);
                    pcssAvgBlockerDepth += bSampleDepth;
                    pcssBlockerCount += 1.0;
                }
            }

            if (pcssBlockerCount > 0.0)
            {
                pcssAvgBlockerDepth /= pcssBlockerCount;

                // --- Phase 2: Penumbra Estimation ---
                float pcssDepthDiff = max(pcssReceiverDepth - pcssAvgBlockerDepth, 0.001);
                float pcssPenumbra = pcssDepthDiff * _PCSSLightSize / max(pcssAvgBlockerDepth, 0.01);
                float pcssFilterRadius = clamp(
                    pcssPenumbra * _PCSSSoftness,
                    _PCSSMinFilterRadius,
                    _PCSSMaxFilterRadius);
                // Blur: フィルタ半径にもブラースケールを適用
                float2 pcssFilterTexelSize = pcssFilterRadius * pcssBlurScale / _ScreenParams.xy;

                // --- Phase 3: Variable-Width Poisson Disk PCF ---
                half pcssSum = 0.0;
                [loop]
                for (int fi = 0; fi < pcssSamples; fi++)
                {
                    float2 fOffset = PoissonDisk32[fi] * pcssFilterTexelSize;
                    pcssSum += UNITY_SAMPLE_SCREENSPACE_TEXTURE(
                        _ShadowMapTexture, pcssShadowUV + fOffset).r;
                }
                atten = pcssSum / (half)pcssSamples;
            }
            else
            {
                // No blockers found — fully lit or fully shadowed
                atten = UNITY_SAMPLE_SCREENSPACE_TEXTURE(
                    _ShadowMapTexture, pcssShadowUV).r;
            }

            // --- Blend + BlendMode: PCSS結果を元のattenとブレンド ---
            // ApplyEffectBlendPost (half3版) を使い、atten をグレースケール色として処理
            half3 pcssPreColor = half3(pcssOriginalAtten, pcssOriginalAtten, pcssOriginalAtten);
            half3 pcssPostColor = half3(atten, atten, atten);
            atten = ApplyEffectBlendPost(pcssPreColor, pcssPostColor, _PCSSBlend, _PCSSBlendMode).r;
        }
        #else
        if (_ShadowSmoothing > 0.001)
        {
            // --- Directional Light: PCF 9-tap on screen-space shadow map ---
            float2 shadowUV = i._ShadowCoord.xy / i._ShadowCoord.w;
            float2 texelSize = _ShadowSmoothing * PCF_TEXEL_SCALE / _ScreenParams.xy;

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
            atten = pcfShadow / PCF_SAMPLE_COUNT;
        }
        #endif
    #else
        if (_ShadowSmoothing > 0.001)
        {
            // --- Point/Spot Light: Adaptive smoothstep ---
            half attenDeriv = fwidth(atten);
            half adaptiveCenter = clamp(atten, 0.1, 0.9);
            half smoothWidth = max(attenDeriv, _ShadowSmoothing * SMOOTH_WIDTH_SCALE);
            atten = smoothstep(adaptiveCenter - smoothWidth, adaptiveCenter + smoothWidth, atten);
        }
    #endif

    // Apply shadow receive strength (allows controlling how much shadows affect this material)
    // マスクの判定を反転: 白（1.0）= 影を受けない、黒（0.0）= 影を受ける
    half shadowStrength = (1.0 - shadowReceiveMask) * _ShadowReceive;
    atten = lerp(1.0, atten, shadowStrength);

    half3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

    // ===== Smooth Normal Shading Blend =====
    // Blend world normal with smooth normal for softer shadow boundaries on hard-edge models
    half3 shadingNormal = worldNormal;
    #ifdef _SMOOTH_NORMAL
        if (_SmoothNormalShadingBlend > 0.001)
        {
            half3 smoothN = normalize(i.smoothWorldNormal);
            shadingNormal = normalize(lerp(worldNormal, smoothN, _SmoothNormalShadingBlend));
        }
    #endif
    half ndotl = dot(shadingNormal, lightDir);

    // ===== SDF Shadow Map =====
    // Apply SDF shadow to ndotl before lighting calculations
    ndotl = ApplySDFShadow(uv, ndotl, lightDir, i.worldPos);

    // ===== Vertex Color Shadow Threshold =====
    #ifdef _VERTEX_COLOR_SHADOW
    {
        // 頂点カラーR値で影閾値をオフセット（R=0.5がデフォルト）
        half vcOffset = (i.color.r - _VCShadowThreshold) + _VCShadowPush;
        ndotl = saturate(ndotl + vcOffset);
    }
    #endif

    // ===== Wrapped Diffuse =====
    // (NdotL + wrap) / (1 + wrap) — 0=Lambert, 0.5=Half-Lambert, 1=Uniform
    #ifndef _STANDARD_TOON
    if (_WrapAmount > 0.001)
    {
        ndotl = (ndotl + _WrapAmount) / (1.0 + _WrapAmount);
    }
    #endif

    // ===== StandardToon: Half-Lambert =====
    #ifdef _STANDARD_TOON
        // Half-Lambert: maps [-1,1] → [0,1], front faces always ≥ 0.5
        ndotl = saturate(ndotl * 0.5 + 0.5);
    #endif

    // ===== Backlight Calculation =====
    // Calculate light coming from behind the object (rim-like effect)
    half backlight = 0.0;
    {
        half backlightDot = max(0.0, dot(worldNormal, -lightDir));
        float backlightPowerBlurred = max(BACKLIGHT_MIN_POWER, BACKLIGHT_SCALE * (1.0 - _BacklightBlur * BACKLIGHT_BLUR_INFLUENCE));
        backlight = pow(backlightDot, backlightPowerBlurred) * _BacklightIntensity;
    }

    // ===== Toon/Ramp Shading =====
    // このセクションの処理:
    // NdotL を基にトゥーンシェーディング（階段状）またはランプテクスチャで
    // 陰影の明暗を計算し、AO・ディザリング・シャドウアッテネーションを適用して
    // 最終的な shadingValue（0=影、1=明るい）と shadowColor を決定する。
    // Calculate base light term
    half lightTerm = ndotl;

    // Apply shadow receive mask to light term
    // 白いマスク部分（shadowReceiveMask = 1.0）では常に明るく保つ
    // これにより、ndotlの影響を受けずにシェーディングを無効化できる
    lightTerm = lerp(lightTerm, 1.0, shadowReceiveMask);

    #ifndef _STANDARD_TOON
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
    #endif
    // StandardToon: lightTerm をそのまま使用（lilToon互換）

    // ===== Toon/Ramp Shading =====
    half3 lighting;
    half shadingValue;
    half3 shadowColor;
    half aoForIndirect = 1.0;

    // ===== AO (pre-calculate before shading branch) =====
    half aoEffect = 1.0;
    #ifdef _USE_AO
        half ao = SampleTex2DBlur1(_AOMap, uv, _AOBlur);
        ao = ApplySoftMask(ao);
        aoEffect = lerp(1.0, ao, _AOIntensity);
        aoForIndirect = lerp(1.0, ao, _AOIntensity * AO_INDIRECT_STRENGTH);
    #endif

    // ===== Procedural AO (Height-based) =====
    #ifdef _PROCEDURAL_AO
    {
        // オブジェクト空間Y座標からAOグラデーション生成
        float proceduralAO = smoothstep(0.0, max(_ProceduralAOSoftness, 0.01),
                                         i.objectPos.y + _ProceduralAOHeightOffset);
        proceduralAO = lerp(1.0, proceduralAO, _ProceduralAOIntensity);
        // 既存のaoEffectに乗算で合成
        aoEffect *= proceduralAO;
    }
    #endif

    #ifdef _USE_RAMP
        half rampInput = lightTerm;
        #ifdef _USE_AO
            rampInput *= aoEffect;
        #endif
        // Shadow attenuation (separated from NdotL for clean toon boundaries)
        rampInput *= atten;
        // Use ramp texture for custom shadow gradients
        lighting = RampShading(rampInput);
        shadingValue = saturate(rampInput + clamp(_ShadowOffset, -1.0, 1.0));
        shadowColor = lighting;
    #elif defined(_STANDARD_TOON)
        // ===== StandardToon v2: lilToon-exact pipeline =====
        // Tooning
        half stToon = LilToonShading(lightTerm, _STShadowBorder, _STShadowBlur);

        // Shadow attenuation (lilToon: lns *= lerp(1, calculatedShadow, _ShadowReceive))
        stToon *= atten;

        // Shadow strength (lilToon: lns = lerp(1, lns, _ShadowStrength))
        stToon = lerp(1.0, stToon, _STShadowStrength);

        // AO
        #ifdef _USE_AO
            stToon *= lerp(1.0, aoEffect, _AOBlend);
        #endif

        shadingValue = stToon;

        // ---- Shadow color computation (lilToon model: multiplicative with albedo) ----
        half3 stAlbedo = col.rgb;

        // Shadow color texture: lilToon style (multiplicative tinting)
        // _ShadowColorTexStrength で制御（既存Natane方式との互換）
        // デフォルト白テクスチャ + Strength=0 → stIndirectCol = stAlbedo * _ShadowColor.rgb（正常動作）
        half3 stShadowColorTexSample = tex2D(_ShadowColorTex, uv).rgb;
        half3 stTintedAlbedo = lerp(stAlbedo, stAlbedo * stShadowColorTexSample, _ShadowColorTexStrength);
        half3 stIndirectCol = stTintedAlbedo * _ShadowColor.rgb;

        // Multi-shadow (lilToon sequential lerp with alpha)
        #ifdef _USE_MULTI_SHADOW
            // 2nd shadow
            half toon2 = LilToonShading(lightTerm, _Shadow2ndBorder, _STShadowBlur);
            half3 st2ndCol = stAlbedo * _Shadow2ndColor.rgb;
            half alpha2 = _Shadow2ndColor.a - _Shadow2ndColor.a * toon2;
            stIndirectCol = lerp(stIndirectCol, st2ndCol, alpha2);

            // 3rd shadow
            half toon3 = LilToonShading(lightTerm, _Shadow3rdBorder, _STShadowBlur);
            half3 st3rdCol = stAlbedo * _Shadow3rdColor.rgb;
            half alpha3 = _Shadow3rdColor.a - _Shadow3rdColor.a * toon3;
            stIndirectCol = lerp(stIndirectCol, st3rdCol, alpha3);
        #endif

        // Direct color & indirect color with light color
        half3 stDirectCol = stAlbedo * stLightColor;
        stIndirectCol *= stLightColor;

        // Shadow Environment Strength: lighten shadows with indirect light
        #ifdef UNITY_PASS_FORWARDBASE
            stIndirectCol = lerp(stIndirectCol, stAlbedo,
                                 saturate(stIndLightColor * _STShadowEnvStrength));
        #endif

        // Safety clamp: shadow never brighter than lit
        stIndirectCol = min(stIndirectCol, stDirectCol);

        // Store for later composition
        shadowColor = _ShadowColor.rgb;
        // lighting は STEP 5 で計算される（LV パス用に初期値を設定）
        lighting = lerp(shadowColor, half3(1, 1, 1), shadingValue);
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

        // ===== Shadow Edge Noise (hand-drawn shadow boundaries) =====
        #ifdef _SHADOW_EDGE_NOISE
        {
            // Only apply noise near shadow boundaries (shadingValue 0.2-0.8)
            float edgeMask = 1.0 - saturate(abs(shadingValue - 0.5) * 4.0);
            // Value noise from world position
            float2 noiseUV = i.worldPos.xz * _ShadowNoiseScale * 0.01;
            noiseUV += _Time.y * _ShadowNoiseSpeed * 0.1;
            float noise = frac(sin(dot(noiseUV, float2(12.9898, 78.233))) * 43758.5453);
            noise = noise * 2.0 - 1.0; // remap to [-1, 1]
            shadingValue += noise * _ShadowNoiseIntensity * edgeMask;
            shadingValue = saturate(shadingValue);
        }
        #endif

        // ===== AO (apply cached AO to shading stage) =====
        #ifdef _USE_AO
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

        // ===== Cast Shadow Color Control =====
        #ifdef _CAST_SHADOW_COLOR
        {
            // Detect cast shadow: low attenuation but surface facing light
            float castShadowMask = saturate((1.0 - atten) * saturate(ndotl + 0.5));
            shadowColor = lerp(shadowColor, shadowColor * _CastShadowTint.rgb, castShadowMask * _CastShadowIntensity);
        }
        #endif

        // Preserve hue and saturation better in shadows
        lighting = lerp(shadowColor, litColor, shadingValue);
    #endif

    // ===== Shadow Color HSV Shift =====
    if (abs(_ShadowHueShift) > 0.001 || abs(_ShadowSaturation - 1.0) > 0.001)
    {
        float3 shadowHSV = RGBtoHSV(shadowColor);
        shadowHSV.x = frac(shadowHSV.x + _ShadowHueShift);
        shadowHSV.y = saturate(shadowHSV.y * _ShadowSaturation);
        shadowColor = HSVtoRGB(shadowHSV);
    }

    // Apply shadow max darkness limit (prevents shadows from being too black)
    shadowColor = max(shadowColor, saturate(_ShadowMaxDarkness));

    // ===== Backlight Calculation (used in ForwardBase Step 4) =====
    // (backlight was already calculated above at line ~128)

    // ===== ForwardBase: Natural Lighting Pipeline =====
    // このセクションの処理:
    // 5ステップのライティング合成パイプライン:
    // STEP 1: 間接光（Light Volume / SH Light Probe）
    // STEP 2: 影の環境色（LV使用時のみ） → STEP 3: 直接光（ライトカラー適用）
    // STEP 4: 追加光（頂点ライト・バックライト・LTCGI）
    // STEP 5: 最終合成（LVブレンドモード選択 or max合成）
    #ifdef UNITY_PASS_FORWARDBASE
        // ========== STEP 1: Indirect Light ==========
        half3 indirectResult = half3(0, 0, 0);
        float3 ambient = float3(0, 0, 0);

        #if defined(_BACKGROUND_MODE) && defined(LIGHTMAP_ON)
            // Background: ライトマップから間接光を取得
            half3 lmColor = SampleNataneLightmap(i.lightmapUV, worldNormal) * _LightmapIntensity;
            indirectResult = lmColor;
            ambient = float3(0, 0, 0);
            // ライトマップ輝度でトゥーンシェーディング結果を調整
            half lmLum = saturate(CALC_LUMINANCE(lmColor));
            shadingValue = lerp(shadingValue, lmLum, _LightmapToonInfluence);
            // Recompute lighting with updated shadingValue
            lighting = lerp(shadowColor, half3(1, 1, 1), shadingValue);
        #elif defined(_USE_LIGHT_VOLUME)
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

            // StandardToon: stDirectCol/stIndirectCol は stLightColor ベースで計算済み。
            // stLightColor にはディレクショナルライト + SH が含まれるため、
            // STEP 5 では追加のLVブーストなしで正しくレンダリングされる。
        #else
            // Fallback: Unity Light Probes
            // L0 (uniform ambient) for indirect
            float3 shAverage = ShadeSH9(float4(0, 0, 0, 1));
            indirectResult = max(0, shAverage);

            // Directional SH for ambient contribution
            float3 shDirect = ShadeSH9(float4(worldNormal, 1.0));
            float3 shIndirect = ShadeSH9(float4(-worldNormal, 1.0));
            ambient = lerp(shIndirect, shDirect, SH_INDIRECT_BLEND);
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
        #ifdef _STANDARD_TOON
            // StandardToon v2: lilToon-exact composition handled in STEP 5
            // stDirectCol, stIndirectCol, shadingValue computed in shading branch
            half3 directResult = half3(0, 0, 0); // placeholder, replaced in STEP 5
        #elif defined(_USE_RAMP)
            half3 directResult = lighting; // Ramp already provides colored shadow-to-lit
        #else
            half3 directResult = lerp(shadowColor, half3(1, 1, 1), shadingValue);
        #endif

        #ifndef _STANDARD_TOON
        // Light color application (with LightColorInfluence preservation)
        half lightColorLum = CALC_LUMINANCE(effectiveLightColor);
        half3 colorMultiplied = directResult * saturate(effectiveLightColor);
        half3 luminanceOnly = directResult * lightColorLum;
        half3 lightColorInfluenced = lerp(luminanceOnly, colorMultiplied, _LightColorInfluence);
        directResult = lightColorInfluenced * max(0.0, _LightIntensity);

        // Light influence clamping
        half directLum = CALC_LUMINANCE(directResult);
        directLum = clamp(directLum, _LightMinInfluence, _LightMaxInfluence);
        half3 directDir = normalize(max(directResult, 0.01));
        directResult = directDir * directLum;
        #endif

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
        half backlightBlendFaded = _BacklightBlend;
        #ifdef _DISTANCE_FADE
            backlightBlendFaded *= lerp(1.0, distanceFade, _BacklightDistFade);
        #endif
        additionalResult += backlight * _BacklightColor.rgb * effectiveLightColor * backlightBlendFaded;

        // LTCGI (diffuse → additional, specular → saved for later)
        #if defined(_LTCGI)
            float3 ltcgiDiffuse = 0;
            float3 ltcgiSpecular = 0;
            LTCGI_Contribution(i.worldPos, worldNormal, viewDir,
                1.0 - _Smoothness, float2(0, 0),
                ltcgiDiffuse, ltcgiSpecular);
            additionalResult += ltcgiDiffuse * _LTCGIIntensity * _LTCGIBlend;
        #endif

        // ========== PBR Direct + Indirect Specular (Background only) ==========
        #ifdef _PBR
        {
            half2 metallicGloss = tex2D(_PBR_MetallicGlossMap, uv).ra;
            half metallic = metallicGloss.x * _PBR_Metallic;
            half smoothness = metallicGloss.y * _PBR_Smoothness;
            half roughness = max(0.04, 1.0 - smoothness);

            // F0: non-metal=0.04, metal=albedo color
            half3 F0 = lerp(half3(0.04, 0.04, 0.04), col.rgb, metallic);

            // Energy conservation: metals reduce diffuse
            directResult *= (1.0 - metallic);

            // GGX direct specular
            half3 pbrSpec = NatanePBRSpecular(worldNormal, viewDir, lightDir,
                roughness, F0, effectiveLightColor, atten);
            additionalResult += pbrSpec;

            // Indirect specular (reflection probes)
            half3 indirectSpec = NatanePBRIndirectSpecular(worldNormal, viewDir, i.worldPos,
                roughness, F0);
            half pbrOcclusion = tex2D(_PBR_OcclusionMap, uv).r;
            pbrOcclusion = lerp(1.0, pbrOcclusion, _PBR_OcclusionStrength);
            indirectSpec *= pbrOcclusion * _PBR_ReflectionIntensity;
            additionalResult += indirectSpec;
        }
        #endif

        // ========== STEP 5: Final Composition ==========
        #ifdef _USE_LIGHT_VOLUME
            #ifdef _STANDARD_TOON
            // StandardToon + LV: lilToon互換合成
            // stDirectCol/stIndirectCol は stLightColor ベースで計算済みのため
            // 非LVパスと同じ合成ロジックで正しくレンダリングされる。
            // LV の環境光は preLightVolume → _LightVolumeBlend で反映される。
            {
                half3 stResult = lerp(stIndirectCol, stDirectCol, shadingValue);
                stResult += additionalResult * col.rgb;
                lighting = stResult;
            }
            #else
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
            #endif

            // Light Volume Specular (additive on albedo)
            #ifdef _LIGHT_VOLUME_SPECULAR
                float3 lvSpecular = LightVolumeSpecular(col.rgb, _Smoothness, _Metallic,
                    worldNormal, viewDir, L0, L1r, L1g, L1b);
                lvSpecular *= _LightVolumeIntensity * _GIIntensity;
                lvSpecular *= _Glossiness;
                lvSpecular = ApplyMatteQuality(lvSpecular, col.rgb, _MatteEffect);
                float lvSpecStrength = saturate(length(lvSpecular) * 0.5);
                col.rgb = SafeAdditiveBlend(col.rgb, lvSpecular, lvSpecStrength);
            #endif

            lighting = lerp(preLightVolume, lighting, _LightVolumeBlend);
        #else
            #ifdef _STANDARD_TOON
                // ========== StandardToon v2: lilToon-compatible composition ==========
                // lilToon's final composition: lerp(indirectCol, directCol, toon)
                half3 stResult = lerp(stIndirectCol, stDirectCol, shadingValue);

                // Add additional lights (vertex lights, backlight, LTCGI) scaled by albedo
                lighting = stResult + additionalResult * col.rgb;
                // Note: 'lighting' here already includes albedo for StandardToon
                // 黒防止は _LightColorMin (= lilToon _LightMinLimit) で保証
            #else
                // Non-LV: max() composition + directional ambient
                lighting = max(indirectResult, directResult + additionalResult);
                lighting += ambient;
            #endif
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
        #ifdef _STANDARD_TOON
            // StandardToon v2: simple additional light contribution (lilToon-style)
            lighting = lerp(shadowColor, half3(1, 1, 1), shadingValue);
            lighting *= _LightColor0.rgb;
            lighting *= max(0.0, _AdditionalLightIntensity);
        #else
            lighting = lerp(shadowColor, half3(1, 1, 1), shadingValue);
            half lightColorLum_add = CALC_LUMINANCE(_LightColor0.rgb);
            half3 colorMul_add = lighting * _LightColor0.rgb;
            half3 lumOnly_add = lighting * lightColorLum_add;
            lighting = lerp(lumOnly_add, colorMul_add, _LightColorInfluence);
            lighting *= max(0.0, _LightIntensity);
            lighting *= max(0.0, _AdditionalLightIntensity);
        #endif
    #endif

    // Store original texture color before lighting application
    half3 originalAlbedo = col.rgb;

    #ifndef UNITY_PASS_FORWARDBASE
        // ForwardAdd pass should output only additional light contribution.
        // Keep this path minimal to avoid over-brightening and reduce per-light cost.
        col.rgb = originalAlbedo * lighting * _Brightness;

        // Backlight contribution in ForwardAdd
        {
            half backlightBlendFaded_add = _BacklightBlend;
            #ifdef _DISTANCE_FADE
                backlightBlendFaded_add *= lerp(1.0, distanceFade, _BacklightDistFade);
            #endif
            col.rgb += originalAlbedo * backlight * _BacklightColor.rgb * _LightColor0.rgb * atten * _AdditionalLightIntensity * backlightBlendFaded_add;
        }
    #else
        #ifdef _STANDARD_TOON
            // StandardToon v2: lighting already includes albedo (computed in STEP 5)
            col.rgb = lighting;
            half gray = CALC_LUMINANCE(col.rgb);
            col.rgb = lerp(gray, col.rgb, _Saturation);
            col.rgb *= _Brightness;
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
    #endif

    // ===== Halftone Shadow =====
    #if defined(_HALFTONE_SHADOW)
    {
        // shadowFactor: 0=lit, 1=shadow
        float shadowArea = smoothstep(_HalftoneShadowThreshold + _HalftoneShadowSoftness,
                                       _HalftoneShadowThreshold - _HalftoneShadowSoftness,
                                       shadingValue);
        // Circle halftone pattern from screen position
        float2 htPos = i.pos.xy / _HalftoneShadowScale;
        float2 htCenter = floor(htPos) + 0.5;
        float htDist = length(htPos - htCenter);
        // Dot size proportional to shadow intensity
        float htDot = step(htDist, shadowArea * 0.5);
        // Apply halftone
        col.rgb = lerp(col.rgb, _HalftoneShadowColor.rgb * col.rgb,
                       htDot * _HalftoneShadowIntensity * _HalftoneShadowBlend);
    }
    #endif

    // ================================================================
    // ===== STAGE A: Illustration Style — Color Transform =====
    // ================================================================

    #if defined(_COLOR_QUANTIZE) && defined(UNITY_PASS_FORWARDBASE)
    {
        half qMask = tex2D(_QuantizeMask, uv).r;
        half3 quantized;
        if (_QuantizeMode > 0.5)
        {
            // HSV quantization
            quantized = QuantizeColorHSV(col.rgb, _QuantizeHueLevels, _QuantizeSatLevels,
                _QuantizeValLevels, _QuantizeDither, StabilizeDitherCoord(i.pos.xy));
        }
        else
        {
            // RGB quantization
            quantized = QuantizeColorRGB(col.rgb, _QuantizeLevels, _QuantizeDither, StabilizeDitherCoord(i.pos.xy));
        }
        col.rgb = lerp(col.rgb, quantized, _QuantizeBlend * qMask);
    }
    #endif

    #if defined(_LUT_3D) && defined(UNITY_PASS_FORWARDBASE)
    {
        half3 lutColor = ApplyLUT3D(saturate(col.rgb), _LUT3DTex, _LUT3DSize);
        col.rgb = lerp(col.rgb, lutColor, _LUT3DIntensity);
    }
    #endif

    #if defined(_HATCHING) && defined(UNITY_PASS_FORWARDBASE)
    {
        half hMask = tex2D(_HatchingMask, uv).r;
        // Use luminance of current color as proxy for shading value
        half hatchShading = dot(col.rgb, half3(0.299, 0.587, 0.114));
        col.rgb = ApplyHatching(col.rgb, uv, hatchShading, hMask,
            _HatchTex0, _HatchTex1, _HatchingTiling, _HatchingColor, _HatchingBlend);
    }
    #endif

    // ===== Post-Lighting Effects =====
    // このセクションの処理:
    // ライティング適用後に各種視覚エフェクトを順番に加算合成する。
    // Specular → Hair Specular → SSS → Rim Light (1/2) → Offset Rim → Env Rim
    // → MatCap (1/2/3) → Reflection → Refraction → Emission → Hue Shift
    // → AudioLink → Glitter → Iridescence → Drip → Hologram → Glitch → Decal → Dissolve
    // 各エフェクトは SafeAdditiveBlend で白飛びを防ぎ、距離フェードにも対応する。

    // ===== Specular Highlight =====
    #ifdef _SPECULAR
        float specSoftnessBlurred = _SpecularSoftness + _SpecularBlur * 0.3;
        half spec = SpecularHighlight(worldNormal, viewDir, lightDir, _SpecularSize, specSoftnessBlurred);

        // Boundary Dithering: apply dither pattern at specular highlight edges
        #if defined(_SPECULAR_DITHER)
        {
            float2 specDitherScreenUV = i.screenPos.xy / max(i.screenPos.w, 0.0001);
            float2 specDitherScreenPos = specDitherScreenUV * _ScreenParams.xy;
            float specDitherThreshold = NataneGetDitherThreshold(StabilizeDitherCoord(specDitherScreenPos), _SpecularDitherScale);
            float ditheredSpec = step(specDitherThreshold, spec);
            spec = lerp(spec, ditheredSpec, _SpecularDitherStrength);
        }
        #endif

        half3 specContrib = spec * _SpecularColor.rgb * _SpecularIntensity * effectiveLightColor * atten;

        // Apply mask texture with soft blending
        float2 specMaskUV = AnimateUVIfNeeded(uv, _SpecularMaskScrollSpeed.xy, _SpecularMaskRotateSpeed);
        half specMask = tex2D(_SpecularMask, specMaskUV).r;
        specMask = ApplySoftMask(specMask); // Smooth mask transitions
        specContrib *= specMask;

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            specContrib *= _AdditionalLightIntensity;
        #endif

        // Apply glossiness and matte material quality
        specContrib *= _Glossiness;
        specContrib = ApplyMatteQuality(specContrib, col.rgb, _MatteEffect);

        // Use safe additive blending to prevent white-out
        half3 preSpec = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, specContrib, saturate(length(specContrib) * 0.8));
        half specBlendFaded = _SpecularBlend;
        #ifdef _DISTANCE_FADE
            specBlendFaded *= lerp(1.0, distanceFade, _SpecularDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preSpec, col.rgb, specBlendFaded, _SpecularBlendMode);
    #endif

    // ===== Hair Specular (Kajiya-Kay) =====
    #ifdef _HAIR_SPECULAR
        half3 hairSpec = HairSpecularHighlight(worldNormal, i.worldTangent, i.worldBinormal,
                                                viewDir, lightDir, uv);

        // Boundary Dithering: apply dither pattern at hair specular highlight edges
        #if defined(_SPECULAR_DITHER)
        {
            float2 hairDitherScreenUV = i.screenPos.xy / max(i.screenPos.w, 0.0001);
            float2 hairDitherScreenPos = hairDitherScreenUV * _ScreenParams.xy;
            float hairDitherThreshold = NataneGetDitherThreshold(StabilizeDitherCoord(hairDitherScreenPos), _SpecularDitherScale);
            float hairSpecIntensity = max(max(hairSpec.r, hairSpec.g), hairSpec.b);
            float hairDitherMask = lerp(1.0, step(hairDitherThreshold, hairSpecIntensity), _SpecularDitherStrength);
            hairSpec *= hairDitherMask;
        }
        #endif

        hairSpec *= effectiveLightColor * atten;

        // Apply additional light intensity scaling in ForwardAdd pass
        #ifndef UNITY_PASS_FORWARDBASE
            hairSpec *= _AdditionalLightIntensity;
        #endif

        // Apply glossiness and matte material quality
        hairSpec *= _Glossiness;
        hairSpec = ApplyMatteQuality(hairSpec, col.rgb, _MatteEffect);

        // Use safe additive blending to prevent white-out
        half hairSpecStrength = saturate(length(hairSpec) * 0.8);
        half3 preHairSpec = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, hairSpec, hairSpecStrength);
        half hairSpecBlendFaded = _HairSpecBlend;
        #ifdef _DISTANCE_FADE
            hairSpecBlendFaded *= lerp(1.0, distanceFade, _HairSpecDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preHairSpec, col.rgb, hairSpecBlendFaded, _HairSpecBlendMode);
    #endif

    // ===== Angel Ring (天使の輪) =====
    #if defined(_ANGEL_RING)
    {
        // MatCapベースUV: ビュー空間法線のY成分でリング位置を決定
        float2 angelUV = float2(
            dot(normalize(UNITY_MATRIX_V[0].xyz), worldNormal) * 0.5 + 0.5,
            dot(normalize(UNITY_MATRIX_V[1].xyz), worldNormal) * 0.5 + 0.5
        );
        // Y方向にオフセット（リングの位置調整）
        angelUV.y += _AngelRingOffset;
        // リングパターン: Y座標のガウシアンで幅を制御
        float ringFactor = exp(-pow((angelUV.y - 0.5) / max(_AngelRingWidth, 0.01), 2.0));
        // テクスチャサンプリング
        half4 angelTex = tex2D(_AngelRingTex, TRANSFORM_TEX(angelUV, _AngelRingTex));
        half3 angelColor = angelTex.rgb * _AngelRingColor.rgb * ringFactor * _AngelRingIntensity;
        // ブレンド
        half3 preAngel = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, angelColor, saturate(ringFactor * angelTex.a * _AngelRingColor.a));
        half angelBlendFaded = _AngelRingBlend;
        #ifdef _DISTANCE_FADE
            angelBlendFaded *= distanceFade;
        #endif
        col.rgb = ApplyEffectBlendPost(preAngel, col.rgb, angelBlendFaded, _AngelRingBlendMode);
    }
    #endif

    // ===== Subsurface Scattering =====
    #ifdef _SSS
        half thickness = tex2D(_ThicknessMap, uv).r * _ThicknessScale;

        #if defined(_SSS_LUT)
            half3 sss = SubsurfaceScatteringLUT(ndotl, worldNormal, i.worldPos, thickness);
        #else
            float sssPowerBlurred = max(0.1, _SSSPower * (1.0 - _SSSBlur * 0.8));
            half3 sss = SubsurfaceScattering(worldNormal, lightDir, viewDir, thickness, atten, sssPowerBlurred);
        #endif

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
        half sssBlendFaded = _SSSBlend;
        #ifdef _DISTANCE_FADE
            sssBlendFaded *= lerp(1.0, distanceFade, _SSSDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preSSS, col.rgb, sssBlendFaded, _SSSBlendMode);
    #endif

    // ===== Rim Light Direction (pre-calculate for both Rim Light 1 & 2) =====
    #if defined(_RIM_LIGHT) || defined(_RIM_LIGHT_2)
        half3 rimDirNormalized = normalize(_RimLightDirection.xyz);
    #endif

    // ===== Rim Light =====
    #if defined(_RIM_LIGHT)
        {
            float rimPowerBlurred = max(0.1, _RimPower * (1.0 - _RimBlur * 0.8));
            half rimSpreadPower = lerp(rimPowerBlurred, max(0.5, rimPowerBlurred * 0.3), _RimSpread);

            // Fresnel rim (same for both passes: camera-based edge detection)
            half3 rim = RimLighting(worldNormal, viewDir, rimPowerBlurred, _RimIntensity);
            half3 rimGlow = RimLighting(worldNormal, viewDir, rimSpreadPower, _RimIntensity * _RimSpread * 0.5);
            rim += rimGlow * step(0.001, _RimSpread);

            // Apply mask texture with soft blending
            float2 rimMaskUV = AnimateUVIfNeeded(uv, _RimMaskScrollSpeed.xy, _RimMaskRotateSpeed);
            half rimMask = tex2D(_RimMask, rimMaskUV).r;
            rimMask = ApplySoftMask(rimMask); // Smooth mask transitions
            rim *= rimMask;

            if (_RimDirectionRange > 0.001)
            {
                half rimDirectionMask1 = smoothstep(-_RimDirectionRange, _RimDirectionRange, dot(worldNormal, rimDirNormalized));
                rim *= rimDirectionMask1;
            }

            // Apply glossiness and matte material quality
            rim *= _Glossiness;
            rim = ApplyMatteQuality(rim, col.rgb, _MatteEffect);

            // Light direction-linked rim masking (lilToon-style Half-Lambert)
            // Half-Lambert maps NdotL from [-1,1] to [0,1] — rim follows actual light direction
            {
                half rimHalfLambert = dot(worldNormal, lightDir) * 0.5 + 0.5;
                rim *= lerp(1.0, rimHalfLambert, _RimDirStrength);
            }
            // Shadow-based rim suppression (independent of direction)
            rim *= lerp(1.0, shadingValue, _RimShadowMask);

            // ForwardAdd: per-light color and attenuation (same pattern as Specular/SSS)
            #ifndef UNITY_PASS_FORWARDBASE
                rim *= effectiveLightColor * atten * _AdditionalLightIntensity;
            #endif

            // Use safe additive blending to prevent white-out
            half rimStrength = saturate(length(rim) * 0.5);
            half3 preRim = col.rgb;
            col.rgb = SafeAdditiveBlend(col.rgb, rim, rimStrength);
            half rimBlendFaded = _RimBlend;
            #ifdef _DISTANCE_FADE
                rimBlendFaded *= lerp(1.0, distanceFade, _RimDistFade);
            #endif
            col.rgb = ApplyEffectBlendPost(preRim, col.rgb, rimBlendFaded, _RimBlendMode);
        }
    #endif

    // ===== Rim Light 2 =====
    #if defined(_RIM_LIGHT_2)
        float rim2PowerBlurred = max(0.1, _RimPower2 * (1.0 - _Rim2Blur * 0.8));
        half rim2SpreadPower = lerp(rim2PowerBlurred, max(0.5, rim2PowerBlurred * 0.3), _RimSpread2);

        // Fresnel rim (same for both passes)
        half rim2Factor = 1.0 - saturate(dot(worldNormal, viewDir));
        rim2Factor = pow(rim2Factor, rim2PowerBlurred) * _RimIntensity2;
        half3 rim2 = rim2Factor * _RimColor2.rgb;
        half rim2SpreadFactor = 1.0 - saturate(dot(worldNormal, viewDir));
        rim2SpreadFactor = pow(rim2SpreadFactor, rim2SpreadPower) * _RimIntensity2 * _RimSpread2 * 0.5;
        rim2 += rim2SpreadFactor * _RimColor2.rgb * step(0.001, _RimSpread2);

        // Apply mask texture with soft blending
        float2 rimMask2UV = AnimateUVIfNeeded(uv, _RimMask2ScrollSpeed.xy, _RimMask2RotateSpeed);
        half rimMask2 = tex2D(_RimMask2, rimMask2UV).r;
        rimMask2 = ApplySoftMask(rimMask2); // Smooth mask transitions
        rim2 *= rimMask2;

        if (_RimDirectionRange > 0.001)
        {
            half rimDirectionMask2 = smoothstep(-_RimDirectionRange, _RimDirectionRange, dot(worldNormal, rimDirNormalized));
            rim2 *= rimDirectionMask2;
        }

        // Apply glossiness and matte material quality
        rim2 *= _Glossiness;
        rim2 = ApplyMatteQuality(rim2, col.rgb, _MatteEffect);

        // Light direction-linked rim masking (lilToon-style Half-Lambert)
        {
            half rim2HalfLambert = dot(worldNormal, lightDir) * 0.5 + 0.5;
            rim2 *= lerp(1.0, rim2HalfLambert, _RimDirStrength);
        }
        // Shadow-based rim suppression
        rim2 *= lerp(1.0, shadingValue, _RimShadowMask);

        // ForwardAdd: per-light color and attenuation
        #ifndef UNITY_PASS_FORWARDBASE
            rim2 *= effectiveLightColor * atten * _AdditionalLightIntensity;
        #endif

        // Use fast additive blending (secondary effect)
        half rim2Strength = saturate(length(rim2) * 0.5);
        half3 preRim2 = col.rgb;
        col.rgb = SafeAdditiveBlendFast(col.rgb, rim2, rim2Strength);
        half rim2BlendFaded = _RimBlend2;
        #ifdef _DISTANCE_FADE
            rim2BlendFaded *= lerp(1.0, distanceFade, _Rim2DistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preRim2, col.rgb, rim2BlendFaded, _RimBlendMode2);
    #endif

    // ===== Offset Rim Light =====
    #if defined(_OFFSET_RIM_LIGHT)
        float offsetRimPowerBlurred = max(0.1, _OffsetRimPower * (1.0 - _OffsetRimBlur * 0.8));

        // Offset rim uses lightDir for light direction linking
        half3 offsetRim = OffsetRimLighting(worldNormal, viewDir, lightDir, offsetRimPowerBlurred, _OffsetRimIntensity);

        // Apply mask texture
        half offsetRimMask = tex2D(_OffsetRimMask, uv).r;
        offsetRimMask = ApplySoftMask(offsetRimMask);
        offsetRim *= offsetRimMask;

        // Apply shadow mask (suppress rim in shadowed areas)
        offsetRim *= lerp(1.0, shadingValue, _OffsetRimShadowMask);

        // Apply glossiness and matte material quality
        offsetRim *= _Glossiness;
        offsetRim = ApplyMatteQuality(offsetRim, col.rgb, _MatteEffect);

        // ForwardAdd: per-light color and attenuation
        #ifndef UNITY_PASS_FORWARDBASE
            offsetRim *= effectiveLightColor * atten * _AdditionalLightIntensity;
        #endif

        // Safe additive blend
        half offsetRimStrength = saturate(length(offsetRim) * 0.5);
        half3 preOffsetRim = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, offsetRim, offsetRimStrength);
        half offsetRimBlendFaded = _OffsetRimBlend;
        #ifdef _DISTANCE_FADE
            offsetRimBlendFaded *= lerp(1.0, distanceFade, _OffsetRimDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preOffsetRim, col.rgb, offsetRimBlendFaded, _OffsetRimBlendMode);
    #endif

    // ===== Sheen (Fabric Luster) =====
    #if defined(_SHEEN)
    {
        half3 sheen = SheenHighlight(worldNormal, viewDir, lightDir);
        half sheenMask = tex2D(_SheenMask, TRANSFORM_TEX(uv, _SheenMask)).r;
        sheen *= sheenMask;
        sheen = ApplyMatteQuality(sheen, col.rgb, _MatteEffect);
        half3 preSheen = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, sheen, saturate(length(sheen) * 0.5));
        half sheenBlendFaded = _SheenBlend;
        #ifdef _DISTANCE_FADE
            sheenBlendFaded *= distanceFade;
        #endif
        col.rgb = ApplyEffectBlendPost(preSheen, col.rgb, sheenBlendFaded, _SheenBlendMode);
    }
    #endif

    // ===== Environmental Rim =====
    #if defined(_ENV_RIM)
        float envRimPowerBlurred = max(0.1, _EnvRimPower * (1.0 - _EnvRimBlur * 0.8));

        // Fresnel + cubemap rim (same for both passes)
        half3 envRim = EnvironmentalRim(worldNormal, viewDir, envRimPowerBlurred);

        // Apply mask texture with soft blending
        half envRimMask = tex2D(_EnvRimMask, uv).r;
        envRimMask = ApplySoftMask(envRimMask); // Smooth mask transitions
        envRim *= envRimMask;

        // Apply glossiness and matte material quality
        envRim *= _Glossiness;
        envRim = ApplyMatteQuality(envRim, col.rgb, _MatteEffect);

        // Light direction-linked rim masking (lilToon-style Half-Lambert)
        {
            half envRimHalfLambert = dot(worldNormal, lightDir) * 0.5 + 0.5;
            envRim *= lerp(1.0, envRimHalfLambert, _RimDirStrength);
        }
        // Shadow-based rim suppression
        envRim *= lerp(1.0, shadingValue, _RimShadowMask);

        // ForwardAdd: per-light color and attenuation
        #ifndef UNITY_PASS_FORWARDBASE
            envRim *= effectiveLightColor * atten * _AdditionalLightIntensity;
        #endif

        // Use safe additive blending to prevent white-out
        half envRimStrength = saturate(length(envRim) * 0.5);
        half3 preEnvRim = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, envRim, envRimStrength);
        half envRimBlendFaded = _EnvRimBlend;
        #ifdef _DISTANCE_FADE
            envRimBlendFaded *= lerp(1.0, distanceFade, _EnvRimDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preEnvRim, col.rgb, envRimBlendFaded, _EnvRimBlendMode);
    #endif

    // ===== MatCap (ForwardBase only) =====
    #if defined(_MATCAP) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap = SampleTex2DBlur3(_MatCapTex, matcapUV, _MatCapBlur) * _MatCapIntensity;

        // Apply mask texture with soft blending
        half matcapMask = tex2D(_MatCapMask, uv).r;
        matcapMask = ApplySoftMask(matcapMask); // Smooth mask transitions
        matcap *= matcapMask;

        #ifdef _STANDARD_TOON
            // StandardToon: MatCap は常に Add モード（Multiply/Replace は黒レンダリングの原因になるため）
            half3 preMatCap = col.rgb;
            half matcapStrength = saturate(_MatCapIntensity * matcapMask);
            col.rgb = SafeAdditiveBlend(col.rgb, matcap, matcapStrength);
            half matCapBlendFaded = _MatCapBlend;
            #ifdef _DISTANCE_FADE
                matCapBlendFaded *= lerp(1.0, distanceFade, _MatCapDistFade);
            #endif
            col.rgb = lerp(preMatCap, col.rgb, matCapBlendFaded);
        #else
            // Apply glossiness and matte material quality
            matcap *= _Glossiness;
            matcap = ApplyMatteQuality(matcap, col.rgb, _MatteEffect);

            // Blend modes: 0=Add (safe), 1=Multiply, 2=Replace - Optimized: no branching
            half3 preMatCap = col.rgb;
            half matcapStrength = saturate(_MatCapIntensity * matcapMask);
            half3 addResult = SafeAdditiveBlend(col.rgb, matcap, matcapStrength);
            half3 multiplyResult = BlendWithSoftMask(col.rgb, col.rgb * matcap, saturate(_MatCapIntensity * matcapMask));
            half3 replaceResult = BlendWithSoftMask(col.rgb, matcap, saturate(_MatCapIntensity * matcapMask));

            // Select blend mode using lerp
            half isMultiply = step(HALF_VALUE, _MatCapBlendMode) * step(_MatCapBlendMode, 1.5);
            half isReplace = step(1.5, _MatCapBlendMode);
            col.rgb = lerp(addResult, multiplyResult, isMultiply);
            col.rgb = lerp(col.rgb, replaceResult, isReplace);
            half matCapBlendFaded = _MatCapBlend;
            #ifdef _DISTANCE_FADE
                matCapBlendFaded *= lerp(1.0, distanceFade, _MatCapDistFade);
            #endif
            col.rgb = lerp(preMatCap, col.rgb, matCapBlendFaded);
        #endif
    #endif

    // ===== MatCap 2 (ForwardBase only) =====
    #ifndef _QUEST_LITE
    #if defined(_MATCAP_2) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV2 = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap2 = SampleTex2DBlur3(_MatCapTex2, matcapUV2, _MatCap2Blur) * _MatCapIntensity2;

        half matcapMask2 = tex2D(_MatCapMask2, uv).r;
        matcapMask2 = ApplySoftMask(matcapMask2);
        matcap2 *= matcapMask2;
        matcap2 *= _Glossiness;
        matcap2 = ApplyMatteQuality(matcap2, col.rgb, _MatteEffect);

        half3 preMatCap2 = col.rgb;
        half matcapStrength2 = saturate(_MatCapIntensity2 * matcapMask2);
        half3 addResult2 = SafeAdditiveBlend(col.rgb, matcap2, matcapStrength2);
        half3 multiplyResult2 = BlendWithSoftMask(col.rgb, col.rgb * matcap2, saturate(_MatCapIntensity2 * matcapMask2));
        half3 replaceResult2 = BlendWithSoftMask(col.rgb, matcap2, saturate(_MatCapIntensity2 * matcapMask2));

        half isMultiply2 = step(HALF_VALUE, _MatCapBlendMode2) * step(_MatCapBlendMode2, 1.5);
        half isReplace2 = step(1.5, _MatCapBlendMode2);
        col.rgb = lerp(addResult2, multiplyResult2, isMultiply2);
        col.rgb = lerp(col.rgb, replaceResult2, isReplace2);
        half matCap2BlendFaded = _MatCapBlend2;
        #ifdef _DISTANCE_FADE
            matCap2BlendFaded *= lerp(1.0, distanceFade, _MatCap2DistFade);
        #endif
        col.rgb = lerp(preMatCap2, col.rgb, matCap2BlendFaded);
    #endif
    #endif // !_QUEST_LITE

    // ===== MatCap 3 (ForwardBase only) =====
    #ifndef _QUEST_LITE
    #if defined(_MATCAP_3) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV3 = CalculateMatCapUV(worldNormal, viewDir);
        half3 matcap3 = SampleTex2DBlur3(_MatCapTex3, matcapUV3, _MatCap3Blur) * _MatCapIntensity3;

        half matcapMask3 = tex2D(_MatCapMask3, uv).r;
        matcapMask3 = ApplySoftMask(matcapMask3);
        matcap3 *= matcapMask3;
        matcap3 *= _Glossiness;
        matcap3 = ApplyMatteQuality(matcap3, col.rgb, _MatteEffect);

        half3 preMatCap3 = col.rgb;
        half matcapStrength3 = saturate(_MatCapIntensity3 * matcapMask3);
        half3 addResult3 = SafeAdditiveBlend(col.rgb, matcap3, matcapStrength3);
        half3 multiplyResult3 = BlendWithSoftMask(col.rgb, col.rgb * matcap3, saturate(_MatCapIntensity3 * matcapMask3));
        half3 replaceResult3 = BlendWithSoftMask(col.rgb, matcap3, saturate(_MatCapIntensity3 * matcapMask3));

        half isMultiply3 = step(HALF_VALUE, _MatCapBlendMode3) * step(_MatCapBlendMode3, 1.5);
        half isReplace3 = step(1.5, _MatCapBlendMode3);
        col.rgb = lerp(addResult3, multiplyResult3, isMultiply3);
        col.rgb = lerp(col.rgb, replaceResult3, isReplace3);
        half matCap3BlendFaded = _MatCapBlend3;
        #ifdef _DISTANCE_FADE
            matCap3BlendFaded *= lerp(1.0, distanceFade, _MatCap3DistFade);
        #endif
        col.rgb = lerp(preMatCap3, col.rgb, matCap3BlendFaded);
    #endif
    #endif // !_QUEST_LITE

    // ===== Procedural MatCap (ForwardBase only) =====
    #if defined(_PROCEDURAL_MATCAP) && defined(UNITY_PASS_FORWARDBASE)
    {
        float2 procMatCapUV = CalculateMatCapUV(worldNormal, viewDir);
        // Spherical gradient from view-space normal
        half gradient = pow(saturate(1.0 - length(procMatCapUV - 0.5) * 2.0), _ProcMatCapPower);
        // Fresnel rim enhancement
        half procFresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), _ProcMatCapFresnelPower);
        half3 procMatCap = _ProcMatCapColor.rgb * (gradient + procFresnel) * _ProcMatCapIntensity;

        half3 preProcMatCap = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, procMatCap, saturate(_ProcMatCapIntensity * 0.5));
        half procMatCapBlendFaded = _ProcMatCapBlend;
        #ifdef _DISTANCE_FADE
            procMatCapBlendFaded *= lerp(1.0, distanceFade, 0.5);
        #endif
        col.rgb = ApplyEffectBlendPost(preProcMatCap, col.rgb, procMatCapBlendFaded, _ProcMatCapBlendMode);
    }
    #endif

    // ===== Cubemap Reflection (ForwardBase only) =====
    #if defined(_REFLECTION) && defined(UNITY_PASS_FORWARDBASE)
        half3 reflection = CubemapReflection(worldNormal, viewDir, _Smoothness, _Metallic);

        // Apply mask texture with soft blending
        half reflectionMask = tex2D(_ReflectionMask, uv).r;
        reflectionMask = ApplySoftMask(reflectionMask); // Smooth mask transitions
        reflection *= reflectionMask;

        // Apply glossiness and matte material quality
        reflection *= _Glossiness;
        reflection = ApplyMatteQuality(reflection, col.rgb, _MatteEffect);

        // Use safe additive blending to prevent white-out
        half reflectionStrength = saturate(length(reflection) * reflectionMask * 0.5);
        half3 preReflection = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, reflection, reflectionStrength);
        half reflectionBlendFaded = _ReflectionBlend;
        #ifdef _DISTANCE_FADE
            reflectionBlendFaded *= lerp(1.0, distanceFade, _ReflectionDistFade);
        #endif
        col.rgb = lerp(preReflection, col.rgb, reflectionBlendFaded);
    #endif

    // ===== Fake Environment Reflection (Cubemap-free, ForwardBase only) =====
    #if defined(_FAKE_REFLECTION) && defined(UNITY_PASS_FORWARDBASE)
    {
        // Reflect view direction around surface normal
        float3 reflectDir = reflect(-viewDir, worldNormal);
        // Sky-ground gradient based on reflection Y component
        float skyFactor = saturate(reflectDir.y * _FakeReflSmoothness * 5.0 + 0.5);
        half3 fakeRefl = lerp(_FakeReflGroundColor.rgb, _FakeReflSkyColor.rgb, skyFactor);
        // Fresnel: stronger reflection at grazing angles
        half fresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), _FakeReflFresnelPower);
        fakeRefl *= fresnel * _FakeReflIntensity;

        half3 preFakeRefl = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, fakeRefl, saturate(fresnel * _FakeReflIntensity * 0.5));
        half fakeReflBlendFaded = _FakeReflBlend;
        #ifdef _DISTANCE_FADE
            fakeReflBlendFaded *= lerp(1.0, distanceFade, 0.5);
        #endif
        col.rgb = ApplyEffectBlendPost(preFakeRefl, col.rgb, fakeReflBlendFaded, _FakeReflBlendMode);
    }
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
        half refractionBlendFaded = _RefractionBlend;
        #ifdef _DISTANCE_FADE
            refractionBlendFaded *= lerp(1.0, distanceFade, _RefractionDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preRefraction, col.rgb, refractionBlendFaded, _RefractionBlendMode);
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
        float2 emMaskUV = AnimateUVIfNeeded(uv, _EmissionMaskScrollSpeed.xy, _EmissionMaskRotateSpeed);
        half emissionMask = tex2D(_EmissionMask, emMaskUV).r;
        emissionMask = ApplySoftMask(emissionMask); // Smooth mask transitions
        emission *= emissionMask;

        // Mirror emission multiplier (VRChat)
        #ifdef _MIRROR_CONTROL
            emission *= lerp(1.0, _MirrorEmissionMultiplier, step(0.5, _VRChatMirrorMode));
        #endif

        // Apply Glow/Bloom effect - Optimized: removed branching, use cached luminance
        half emissionLum = CALC_LUMINANCE(emission);
        half3 glow = emission * emissionLum * _EmissionGlow * 2.0;
        emission += glow * step(0.001, _EmissionGlow); // Conditional add without branch

        // Use safe additive blending to prevent white-out
        half emissionStrength = saturate(length(emission) * emissionMask * 0.6);
        half3 preEmission = col.rgb;
        col.rgb = SafeAdditiveBlend(col.rgb, emission, emissionStrength);
        half emissionBlendFaded = _EmissionBlend;
        #ifdef _DISTANCE_FADE
            emissionBlendFaded *= lerp(1.0, distanceFade, _EmissionDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preEmission, col.rgb, emissionBlendFaded, _EmissionBlendMode);
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
            float2 alDissolveUV = AnimateUVIfNeeded(uv, _DissolveTexScrollSpeed.xy, _DissolveTexRotateSpeed);
            float2 alDissolveResult = CalculateDissolve(alDissolveUV, alDissolve * _AudioLinkDissolveIntensity, 0.1);
            half3 alDissolveGlow = _DissolveEdgeColor.rgb * alDissolveResult.y * 2.0;
            col.rgb = SafeAdditiveBlendFast(col.rgb, alDissolveGlow, saturate(alDissolveResult.y));
        }
        #endif
        half audioLinkBlendFaded = _AudioLinkBlend;
        #ifdef _DISTANCE_FADE
            audioLinkBlendFaded *= lerp(1.0, distanceFade, _AudioLinkDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preAL, col.rgb, audioLinkBlendFaded, _AudioLinkBlendMode);
    }
    #endif

    // ===== Glitter Effect =====
    #ifndef _QUEST_LITE
    #if defined(_GLITTER) && defined(UNITY_PASS_FORWARDBASE)
        float2 glitterMaskUV = AnimateUVIfNeeded(uv, _GlitterMaskScrollSpeed.xy, _GlitterMaskRotateSpeed);
        half3 glitter = GlitterEffect(glitterMaskUV, i.worldPos, viewDir, worldNormal, lightDir, _GlitterBlur);
        glitter = ApplyMatteQuality(glitter, col.rgb, _MatteEffect);
        half3 preGlitter = col.rgb;
        col.rgb = SafeAdditiveBlendFast(col.rgb, glitter, 1.0);
        half glitterBlendFaded = _GlitterBlend;
        #ifdef _DISTANCE_FADE
            glitterBlendFaded *= lerp(1.0, distanceFade, _GlitterDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preGlitter, col.rgb, glitterBlendFaded, _GlitterBlendMode);
    #endif
    #endif // !_QUEST_LITE

    // ===== Iridescence Effect =====
    #if defined(_IRIDESCENCE) && defined(UNITY_PASS_FORWARDBASE)
        float iridSizeBlurred = lerp(_IridescenceSize, _IridescenceSize * 3.0, _IridescenceBlur);
        half3 iridescence = IridescenceEffect(worldNormal, viewDir, uv, iridSizeBlurred);
        iridescence = ApplyMatteQuality(iridescence, col.rgb, _MatteEffect);
        half3 preIridescence = col.rgb;
        col.rgb = SafeAdditiveBlendFast(col.rgb, iridescence, 1.0);
        half iridescenceBlendFaded = _IridescenceBlend;
        #ifdef _DISTANCE_FADE
            iridescenceBlendFaded *= lerp(1.0, distanceFade, _IridescenceDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preIridescence, col.rgb, iridescenceBlendFaded, _IridescenceBlendMode);
    #endif

    // ===== SMEAR EFFECT (スミア / 残像エフェクト) =====
    #ifdef _SMEAR
    {
        float smearStretch = i.smearStretchFactor;

        if (smearStretch > 0.001)
        {
            float3 smearDir;
            if (_SmearAutoMagnitude > 0.5)
            {
                float speed = length(_SmearDirection.xyz);
                smearDir = (speed > 0.001) ? _SmearDirection.xyz / speed : float3(0, 0, 1);
            }
            else
            {
                smearDir = normalize(_SmearDirection.xyz + float3(0.0001, 0.0001, 0.0001));
            }

            // Apply mask
            float2 smearMaskUV = AnimateUVIfNeeded(uv, _SmearMaskScrollSpeed.xy, _SmearMaskRotateSpeed);
            half smearMask = tex2D(_SmearMask, smearMaskUV).r;
            smearMask = ApplySoftMask(smearMask);

            // Trail
            half3 smearTrail = CalculateSmearTrail(uv, col.rgb, smearDir, worldNormal, smearStretch, _SmearTrailLength, _SmearTrailFade);

            // Glow
            half3 smearGlow = CalculateSmearGlow(worldNormal, viewDir, smearDir, smearStretch,
                                                  _SmearGlowColor, _SmearGlowIntensity, _SmearGlowPower);

            // Emission
            half3 smearEmission = _SmearEmissionColor.rgb * _SmearEmission * smearStretch;

            // Combine
            half3 smearEffect = (smearTrail + smearGlow + smearEmission) * smearMask;

            // Store pre-smear color for blending
            half3 preSmearColor = col.rgb;

            // Apply blend
            half3 smearBlended = SafeAdditiveBlend(col.rgb, smearEffect, _SmearBlur);
            col.rgb = ApplyEffectBlendPost(preSmearColor, smearBlended, _SmearBlend, _SmearBlendMode);

            // Distance fade
            #ifdef _DISTANCE_FADE
                col.rgb = lerp(col.rgb, preSmearColor, distanceFade * _SmearDistFade);
            #endif
        }
    }
    #endif

    // ===== Water Drip Effect (ForwardBase only) =====
    #ifndef _QUEST_LITE
    #if defined(_WATER_DRIP) && defined(UNITY_PASS_FORWARDBASE)
    {
        float2 dripMaskUV = AnimateUVIfNeeded(uv, _DripMaskScrollSpeed.xy, _DripMaskRotateSpeed);
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
        half dripBlendFaded = _DripBlend;
        #ifdef _DISTANCE_FADE
            dripBlendFaded *= lerp(1.0, distanceFade, _DripDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preDrip, col.rgb, dripBlendFaded, _DripBlendMode);
    }
    #endif
    #endif // !_QUEST_LITE

    // ===== Hologram Effect (ForwardBase only) =====
    #ifndef _QUEST_LITE
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
        float2 holoMaskUV = AnimateUVIfNeeded(uv, _HologramMaskScrollSpeed.xy, _HologramMaskRotateSpeed);
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
        half hologramBlendFaded = _HologramBlend;
        #ifdef _DISTANCE_FADE
            hologramBlendFaded *= lerp(1.0, distanceFade, _HologramDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preHolo, col.rgb, hologramBlendFaded, _HologramBlendMode);
        col.a = ApplyEffectBlendPostAlpha(preHoloAlpha, col.a, hologramBlendFaded);
    }
    #endif
    #endif // !_QUEST_LITE

    // ===== Glitch Effect (ForwardBase only) =====
    #ifndef _QUEST_LITE
    #if defined(_GLITCH) && defined(UNITY_PASS_FORWARDBASE)
    {
        half3 preGlitch = col.rgb;

        // Glitch mask sampling with scale amplification
        half glitchMask = tex2D(_GlitchMask, TRANSFORM_TEX(uv, _GlitchMask)).r;
        // Mask Scale: マスク値を増幅して「白い部分をさらに強く」できる
        // Scale=1: 等倍（従来通り）、Scale=5: マスク0.2→1.0に増幅
        glitchMask = saturate(glitchMask * _GlitchMaskScale);

        // Glitch trigger (random occurrence)
        // Mask Affects Frequency: マスク値が頻度にも影響（白い部分ほど頻繁にグリッチ発生）
        half effectiveFrequency = lerp(_GlitchFrequency, _GlitchFrequency * glitchMask, _GlitchMaskAffectsFrequency);
        half glitchTrigger = step(1.0 - effectiveFrequency,
            frac(sin(_Time.y * _GlitchSpeed) * 43758.5453));

        // Apply mask to intensity (amplified by MaskScale)
        half maskedGlitchIntensity = _GlitchIntensity * glitchMask;

        if (glitchTrigger > 0.5 && maskedGlitchIntensity > 0.001)
        {
            // UV distortion
            float2 glitchUV = CalculateGlitchUV(uv,
                maskedGlitchIntensity, _GlitchSpeed, _GlitchBlockSize);

            // RGB split — Mask Affects RGB Split: マスクでRGBスプリットも制御
            half maskedRGBSplit = lerp(_GlitchRGBSplitIntensity,
                _GlitchRGBSplitIntensity * glitchMask, _GlitchMaskAffectsRGBSplit);
            half3 splitColor = CalculateGlitchRGBSplit(
                col.rgb, glitchUV, _MainTex, maskedRGBSplit);
            col.rgb = lerp(col.rgb, splitColor, maskedGlitchIntensity);
        }
        // Apply noise texture for additional glitch variety
        col.rgb = ApplyGlitchNoise(col.rgb, uv, _GlitchNoiseTex,
            _GlitchNoiseTex_ST, _GlitchNoiseScrollSpeed,
            _GlitchNoiseIntensity * glitchMask, _GlitchNoiseMode, _MainTex);
        // Blur dampens glitch distortion by blending back towards original
        col.rgb = lerp(col.rgb, preGlitch, _GlitchBlur * 0.5);
        half glitchBlendFaded = _GlitchBlend;
        #ifdef _DISTANCE_FADE
            glitchBlendFaded *= lerp(1.0, distanceFade, _GlitchDistFade);
        #endif
        col.rgb = ApplyEffectBlendPost(preGlitch, col.rgb, glitchBlendFaded, _GlitchBlendMode);
    }
    #endif
    #endif // !_QUEST_LITE

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
            half decalBlendFaded = _DecalBlend;
            #ifdef _DISTANCE_FADE
                decalBlendFaded *= lerp(1.0, distanceFade, _DecalDistFade);
            #endif
            col.rgb = lerp(preDecal, col.rgb, decalBlendFaded);
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
            float2 dissolveUV = AnimateUVIfNeeded(uv, _DissolveTexScrollSpeed.xy, _DissolveTexRotateSpeed);

            // Compute dissolve noise based on coordinate mode
            float dissolveNoise;
            if (_DissolveCoordMode > 0.5)
            {
                float3 dissolveCoordPos = _DissolveCoordMode < 1.5
                    ? i.worldPos
                    : mul(unity_WorldToObject, float4(i.worldPos, 1.0)).xyz;
                float axisVal = _DissolveWorldAxis < 0.5 ? dissolveCoordPos.x
                    : (_DissolveWorldAxis < 1.5 ? dissolveCoordPos.y : dissolveCoordPos.z);
                float posNoise = saturate((axisVal - _DissolveWorldMin) / max(_DissolveWorldMax - _DissolveWorldMin, 0.01));
                float texNoise = tex2D(_DissolveTex, dissolveUV).r;
                dissolveNoise = lerp(posNoise, posNoise * texNoise, _DissolveNoiseBlend);
            }
            else
            {
                dissolveNoise = tex2D(_DissolveTex, dissolveUV).r;
            }
            float2 dissolveResult = CalculateDissolveFromNoise(dissolveNoise, _DissolveAmount, dissolveEdgeBlurred);
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

    // ================================================================
    // ===== STAGE B: Illustration Style — Screen Space Effects =====
    // ================================================================

    #if defined(UNITY_PASS_FORWARDBASE) && (defined(_WATERCOLOR) || defined(_SOFT_FILTER) || defined(_KUWAHARA_FILTER) || defined(_SCREEN_EDGE) || defined(_COLOR_BLEEDING) || defined(_CHROMATIC_ABERRATION))
    {
        float2 illustGrabUV = i.screenPos.xy / i.screenPos.w;
        float2 illustScreenUV = i.pos.xy / _ScreenParams.xy;

        #ifdef _WATERCOLOR
        {
            half wcMask = tex2D(_WCMask, TRANSFORM_TEX(uv, _WCMask)).r;
            half wcShading = dot(col.rgb, half3(0.299, 0.587, 0.114));
            col.rgb = ApplyWatercolor(col.rgb, uv, illustScreenUV, wcShading, wcMask,
                _WCGranulationTex, _WCGranulationTex_ST, _WCPaperTex, _WCPaperTex_ST,
                _WCEdgeDarkening, _WCWetEdge, _WCGranulation, _WCPaperIntensity, _WCPaperTiling, _WCBlend);
        }
        #endif

        #ifdef _SOFT_FILTER
        {
            col.rgb = ApplySoftFilter(col.rgb, illustGrabUV, _SoftFilterRadius,
                _SoftFilterBlend, _SoftFilterThreshold, _SoftFilterMode);
        }
        #endif

        #ifdef _KUWAHARA_FILTER
        {
            col.rgb = ApplyKuwaharaFilter(illustGrabUV, (int)_KuwaharaRadius, _KuwaharaBlend, col.rgb);
        }
        #endif

        #ifdef _SCREEN_EDGE
        {
            half edgeValue = ApplyScreenEdge(illustScreenUV, _EdgeDepthSensitivity, _EdgeNormalSensitivity, _EdgeWidth);
            col.rgb = lerp(col.rgb, _EdgeColor.rgb, edgeValue * _EdgeBlend);
        }
        #endif

        #ifdef _COLOR_BLEEDING
        {
            col.rgb = ApplyColorBleeding(col.rgb, illustGrabUV, _BleedingRadius, _BleedingBlend);
        }
        #endif

        #ifdef _CHROMATIC_ABERRATION
        {
            col.rgb = ApplyChromaticAberration(illustGrabUV, _CAIntensity, _CABlend, col.rgb);
        }
        #endif
    }
    #endif

    // ===== Alpha Mask =====
    // Apply alpha mask for partial transparency control
    #ifdef _ALPHA_MASK
        half alphaMask = tex2D(_AlphaMask, uv).r;
        col.a *= alphaMask;
    #endif

    // ===== Height Fade (Local Height-Based Transparency) =====
    #ifdef _HEIGHT_FADE
    {
        half heightFade = CalculateHeightFade(i.worldPos, _HeightFadeStart, _HeightFadeEnd,
            _HeightFadeAxis, _HeightFadeSpace, _HeightFadeInvert);

        // Edge glow at fade boundary
        if (_HeightFadeEdgeWidth > 0.001)
        {
            float edgeLower = smoothstep(0.0, _HeightFadeEdgeWidth, heightFade);
            float edgeUpper = smoothstep(_HeightFadeEdgeWidth, _HeightFadeEdgeWidth * 2.0, heightFade);
            float edge = edgeLower * (1.0 - edgeUpper);
            col.rgb = lerp(col.rgb, _HeightFadeEdgeColor.rgb, edge * _HeightFadeEdgeColor.a);
        }

        if (_HeightFadeMode < 0.5)
        {
            half preHeightAlpha = col.a;
            col.a *= heightFade;
            col.a = ApplyEffectBlendPostAlpha(preHeightAlpha, col.a, _HeightFadeBlend);
        }
        else if (_HeightFadeMode < 1.5)
        {
            clip(heightFade - 0.001);
        }
        else
        {
            float ditherThreshold = DitheringPattern(i.pos.xy, max(_HeightFadeDitherScale, 1.0));
            clip(heightFade - ditherThreshold);
        }
    }
    #endif

    // ===== Intersection Fade (Object Intersection Transparency) =====
    #ifndef _QUEST_LITE
    #ifdef _INTERSECTION_FADE
    {
        float2 intersectScreenUV = i.screenPos.xy / max(i.screenPos.w, 0.0001);
        float sceneDepth = LinearEyeDepth(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, intersectScreenUV).r);
        float fragDepth = i.screenPos.w;
        float depthDiff = sceneDepth - fragDepth;
        half intersectionFade = saturate(depthDiff / max(_IntersectionFadeDistance, 0.001));

        // Edge highlight at intersection
        if (_IntersectionFadeEdgeWidth > 0.001)
        {
            float edge = 1.0 - smoothstep(0.0, _IntersectionFadeEdgeWidth, depthDiff);
            col.rgb = lerp(col.rgb, _IntersectionFadeEdgeColor.rgb, edge * _IntersectionFadeEdgeColor.a);
        }

        if (_IntersectionFadeMode < 0.5)
        {
            half preIntersectAlpha = col.a;
            col.a *= intersectionFade;
            col.a = ApplyEffectBlendPostAlpha(preIntersectAlpha, col.a, _IntersectionFadeBlend);
        }
        else if (_IntersectionFadeMode < 1.5)
        {
            clip(intersectionFade - 0.001);
        }
        else
        {
            float ditherThreshold = DitheringPattern(i.pos.xy, max(_IntersectionFadeDitherScale, 1.0));
            clip(intersectionFade - ditherThreshold);
        }
    }
    #endif
    #endif // !_QUEST_LITE

    // ===== Distance Fade (Global Alpha) =====
    // このセクションの処理:
    // カメラからの距離に基づいてオブジェクト全体の透明度を制御する。
    // Alpha モード: 距離に応じて滑らかにフェードアウト。
    // Simplify モード: 一定距離でハードカリング（オーバードロー削減）。
    // Dithering モード: Bayer パターンのディザリングでオペーク向けフェード。
    // Note: distanceFade value was already computed early for per-effect fading
    #ifdef _DISTANCE_FADE
        if (_DistanceFadeMode < 0.5)
        {
            // Alpha mode: smooth fade-out with distance.
            half preDistAlpha = col.a;
            col.a *= distanceFade;
            col.a = ApplyEffectBlendPostAlpha(preDistAlpha, col.a, _DistanceFadeBlend);
        }
        else if (_DistanceFadeMode < 1.5)
        {
            // Simplify mode: hard cull at distance for stronger overdraw reduction.
            clip(distanceFade - 0.001);
        }
        else
        {
            // Dithering mode: ordered dithering clip for opaque-friendly fade.
            float ditherThreshold = DitheringPattern(i.pos.xy, max(_DistFadeDitherScale, 1.0));
            clip(distanceFade - ditherThreshold);
        }
    #endif

    // ===== Height Fog (Material-Based Fog) =====
    #ifdef _HEIGHT_FOG
    {
        float worldY = i.worldPos.y;
        float heightFactor = saturate((worldY - _HeightFogStart) / (_HeightFogEnd - _HeightFogStart + 0.001));
        if (_HeightFogMode > 0.5)
            heightFactor = 1.0 - exp(-heightFactor * 3.0);
        float fogAmount = (1.0 - heightFactor) * _HeightFogDensity;
        col.rgb = lerp(col.rgb, _HeightFogColor.rgb, fogAmount);
    }
    #endif

    // ===== Depth-based Color Fade (Aerial Perspective per Material) =====
    #if defined(_DEPTH_COLOR_FADE)
    {
        float camDist = length(_WorldSpaceCameraPos - i.worldPos);
        float depthFactor = saturate((camDist - _DepthFadeStart) / max(_DepthFadeEnd - _DepthFadeStart, 0.001));
        // Exponential falloff for more natural aerial perspective
        depthFactor = 1.0 - exp(-depthFactor * 3.0);
        // Desaturate
        half lum = dot(col.rgb, half3(0.299, 0.587, 0.114));
        col.rgb = lerp(col.rgb, half3(lum, lum, lum), depthFactor * _DepthFadeDesaturation);
        // Blend toward atmosphere color
        col.rgb = lerp(col.rgb, _DepthFadeColor.rgb, depthFactor * _DepthFadeIntensity);
    }
    #endif

    // ===== Final Color Blending (Highlight & Shadow Smoothing) =====
    // Apply final smoothing to prevent harsh white/black spots
    // This is applied at the very end before fog for the most natural result
    #ifdef UNITY_PASS_FORWARDBASE
        col.rgb = ApplyFinalColorBlending(col.rgb);
    #endif

    // ===== Hashed / Dithering Alpha =====
    #if defined(_HASHED_ALPHA)
        float2 hashedScreenUV = i.screenPos.xy / max(i.screenPos.w, 0.0001);
        float2 hashedScreenPos = hashedScreenUV * _ScreenParams.xy;
        clip(ApplyHashedAlpha(col.a, StabilizeDitherCoord(hashedScreenPos), i.worldPos.xz, _HashedAlphaScale));
    #elif defined(_DITHERING_ALPHA)
        float2 ditherScreenUV = i.screenPos.xy / max(i.screenPos.w, 0.0001);
        float2 ditherScreenPos = ditherScreenUV * _ScreenParams.xy;
        clip(ApplyDitheringAlpha(col.a, StabilizeDitherCoord(ditherScreenPos), max(_DitheringAlphaScale, 1.0)));
    #endif

    // ===== Fog =====
    UNITY_APPLY_FOG(i.fogCoord, col);

    return col;
}

#endif // NATANE_TOON_FRAGMENT_INCLUDED
