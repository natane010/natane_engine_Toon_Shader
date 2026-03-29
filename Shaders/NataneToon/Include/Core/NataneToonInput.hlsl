#ifndef NATANE_TOON_INPUT_INCLUDED
#define NATANE_TOON_INPUT_INCLUDED

// Properties and Structures
CBUFFER_START(UnityPerMaterial)

    // ===== SECTION 1: Core Rendering (主表面) =====
    // メインテクスチャ、カラー、アルファ、サーフェス設定
    // シェーダーの基本的な表面描画に必要なパラメータ群

    // Main Texture
    float4 _MainTex_ST;
    half4 _Color;

    // Main Texture Animation
    float4 _MainTexScrollSpeed;
    float _MainTexRotateSpeed;

    // Color Preservation
    half _AlbedoPreservation;
    half _Saturation;
    half _Brightness;

    // Surface Finish
    half _Glossiness;
    half _MatteEffect;

    // Final Color Blending
    half _FinalHighlightBlend;
    half _HighlightThreshold;
    half _FinalShadowBlend;
    half _ShadowThreshold;

    // ===== SECTION 2: Makeup Textures (マルチレイヤー) =====
    // 2nd〜5thテクスチャレイヤー（各8パラメータ）
    // メインテクスチャの上に重ねるメイクアップ/デカール用マルチレイヤーシステム

    // Makeup Textures
    float4 _2ndTex_ST;
    half _2ndTexHueShift;
    half _2ndTexSaturation;
    half _2ndTexValue;
    half _2ndTexIntensity;
    half _2ndTexBlendMode;
    float4 _2ndTexScrollSpeed;
    float _2ndTexRotateSpeed;

    float4 _3rdTex_ST;
    half _3rdTexHueShift;
    half _3rdTexSaturation;
    half _3rdTexValue;
    half _3rdTexIntensity;
    half _3rdTexBlendMode;
    float4 _3rdTexScrollSpeed;
    float _3rdTexRotateSpeed;

    float4 _4thTex_ST;
    half _4thTexHueShift;
    half _4thTexSaturation;
    half _4thTexValue;
    half _4thTexIntensity;
    half _4thTexBlendMode;
    float4 _4thTexScrollSpeed;
    float _4thTexRotateSpeed;

    float4 _5thTex_ST;
    half _5thTexHueShift;
    half _5thTexSaturation;
    half _5thTexValue;
    half _5thTexIntensity;
    half _5thTexBlendMode;
    float4 _5thTexScrollSpeed;
    float _5thTexRotateSpeed;

    // Screen-Tone Overlay
    #if defined(_SCREEN_TONE)
    half4 _ScreenToneColor;
    half _ScreenToneScale;
    half _ScreenToneThreshold;
    half _ScreenToneBlend;
    half _ScreenToneBlendMode;
    half _ScreenToneBlur;
    #endif

    // Halftone Shadow
    #if defined(_HALFTONE_SHADOW)
    half4 _HalftoneShadowColor;
    half _HalftoneShadowScale;
    half _HalftoneShadowThreshold;
    half _HalftoneShadowSoftness;
    half _HalftoneShadowIntensity;
    half _HalftoneShadowBlend;
    #endif

    // Gradient Base Color
    #if defined(_GRADIENT_BASE_COLOR)
    half4 _GradientTopColor;
    half4 _GradientBottomColor;
    half _GradientAxis;
    half _GradientSpace;
    half _GradientStart;
    half _GradientEnd;
    half _GradientBlend;
    half _GradientBlendMode;
    #endif

    // ===== SECTION 3: Lighting & Shading (ライティング基本) =====
    // シェーディングモード、マルチトーン、SDFシャドウマップ、影設定
    // トゥーンシェーディングの核となるライティング計算パラメータ群

    // Shading
    half _ShadingMode;
    half _SurfaceModel;
    half _LookMode;
    half _ToonWeight;
    half _NprWeight;
    half _PbrWeight;
    half _ShadowMainStrength;
    half _Shadow2ndBlur;
    half _Shadow3rdBlur;
    half _ShadingGradientWidth;
    half4 _ShadowColor;
    half _ShadowHueShift;
    half _ShadowSaturation;
    half4 _Shadow2ndColor;
    half _Shadow2ndBorder;
    half4 _Shadow3rdColor;
    half _Shadow3rdBorder;
    half _ShadowSteps;
    half _ShadowSharpness;
    half _StepBorderSmooth;
    half _ShadowOffset;
    half _WrapAmount;
    half _LitSoftness;
    half _ShadowBlend;

    // Vertex Color Shadow Threshold
    #if defined(_VERTEX_COLOR_SHADOW)
    half _VCShadowThreshold;
    half _VCShadowPush;
    #endif

    // SDF Shadow Map
    half _SDFIntensity;
    half _SDFSoftness;
    half _SDFOffset;

    // Face SDF Rotation
    float4 _FaceForwardDirection;
    float4 _FaceRightDirection;

    // Shading Grade Map
    half _ShadingGradeScale;

    // Ambient Occlusion
    half _AOIntensity;
    half _AOBlend;
    half _AOBlendMode;
    half _AOBlur;
    half _CavityStrength;
    half _SpecularOcclusionStrength;
    half _SkinSpecPrimaryStrength;
    half _SkinSpecSecondaryStrength;
    half _SkinSpecSecondarySmoothness;
    half4 _SkinSpecSecondaryColor;
    half _SkinSpecFresnelPower;

    // Procedural AO
    #if defined(_PROCEDURAL_AO)
    half _ProceduralAOHeightOffset;
    half _ProceduralAOIntensity;
    half _ProceduralAOSoftness;
    #endif

    // Normal Warping
    #if defined(_NORMAL_WARP)
    half _NormalFlattenY;
    half _NormalRoundness;
    #endif

    // Dithering
    half _DitheringScale;
    half _DitheringStrength;
    half _DitheringBlend;
    half _DitheringBlur;
    half _DitherStabilize;
    #if defined(_BLUE_NOISE_DITHER)
    half _BlueNoiseTemporal;
    half _BlueNoiseAmount;
    #endif

    // Advanced Lighting Controls
    half _SoftLightingIntensity;
    half _LightIntensity;
    half _IndirectLightIntensity;
    half _GIIntensity; // Environment Reflection (GI/Light Probes) intensity control
    half _LightColorInfluence;
    half _ShadowReceive;
    half _ShadowSmoothing;
    half _ShadowMaxDarkness;
    half _SmoothNormalShadingBlend;
    half _SmoothNormalMode;
    half _LightColorMin;
    half _LightColorMax;
    half _MonochromeLighting;
    half _LightMinInfluence;
    half _LightMaxInfluence;
    half _LightBlend;
    half _HighlightSoftness;
    half _BacklightIntensity;
    half4 _BacklightColor;
    half _BacklightBlend;
    half _BacklightBlendMode;
    half _BacklightBlur;
    half _AdditionalLightIntensity;

    // VRC Light Volumes
    half _LightVolumeIntensity;
    half _LightVolumeBlendMode;
    half _LightVolumeBlend;

    // Indirect Lighting (min color + max composition)
    half4 _IndirectLightMinColor;
    half _ShadowEnvStrength;

    // ===== SECTION 4: Effects - Light Based (光源依存エフェクト) =====
    // スペキュラ、ヘアスペキュラ、リムライト、バックライト
    // ライト方向に依存して変化するエフェクト群

    // Specular
    #if defined(_SPECULAR)
    half4 _SpecularColor;
    half _SpecularSize;
    half _SpecularSoftness;
    half _SpecularIntensity;
    half _SpecularBlend;
    half _SpecularBlendMode;
    half _SpecularBlur;
    float4 _SpecularMaskScrollSpeed;
    float _SpecularMaskRotateSpeed;
    #endif
    #if defined(_SPECULAR_AA)
    half _SpecularAAStrength;
    #endif
    #if defined(_SPECULAR_DITHER)
    half _SpecularDitherScale;
    half _SpecularDitherStrength;
    #endif

    // Hair Specular (Kajiya-Kay)
    #if defined(_HAIR_SPECULAR)
    half4 _HairSpecColor1;
    half _HairSpecShift1;
    half _HairSpecWidth1;
    half4 _HairSpecColor2;
    half _HairSpecShift2;
    half _HairSpecWidth2;
    half _HairSpecIntensity;
    half _HairSpecBlend;
    half _HairSpecBlendMode;
    half _HairStrandDirectionStrength;
    half4 _HairTransmissionColor;
    half _HairTransmissionStrength;
    half _HairTransmissionPower;
    #endif

    #if defined(_ANGEL_RING)
    float4 _AngelRingTex_ST;
    half4 _AngelRingColor;
    half _AngelRingOffset;
    half _AngelRingWidth;
    half _AngelRingIntensity;
    half _AngelRingBlend;
    half _AngelRingBlendMode;
    #endif

    // Rim Light
    #if defined(_RIM_LIGHT)
    half4 _RimColor;
    half _RimPower;
    half _RimIntensity;
    half _RimSpread;
    half _RimBlend;
    half _RimBlendMode;
    half _RimBlur;
    float4 _RimMaskScrollSpeed;
    float _RimMaskRotateSpeed;
    #endif

    // Rim Light 2
    #if defined(_RIM_LIGHT_2)
    half4 _RimColor2;
    half _RimPower2;
    half _RimIntensity2;
    half _RimSpread2;
    half _RimBlend2;
    half _RimBlendMode2;
    half _Rim2Blur;
    float4 _RimMask2ScrollSpeed;
    float _RimMask2RotateSpeed;
    #endif

    // Offset Rim Light
    #if defined(_OFFSET_RIM_LIGHT)
    half4 _OffsetRimColor;
    half _OffsetRimPower;
    half _OffsetRimIntensity;
    half _OffsetRimOffsetX;
    half _OffsetRimOffsetY;
    half _OffsetRimUseLightDir;
    half _OffsetRimLightDirStrength;
    half _OffsetRimSharpness;
    half _OffsetRimShadowMask;
    half _OffsetRimBlend;
    half _OffsetRimBlendMode;
    half _OffsetRimBlur;
    #endif

    // Sheen
    #if defined(_SHEEN)
    float4 _SheenMask_ST;
    half4 _SheenColor;
    half _SheenIntensity;
    half _SheenPower;
    half _SheenBlend;
    half _SheenBlendMode;
    #endif

    // ===== SECTION 5: Effects - View Based (視線依存エフェクト) =====
    // MatCap、リフレクション、環境リム、イリデッセンス
    // カメラ/視線方向に依存して変化するエフェクト群

    // MatCap
    #if defined(_MATCAP)
    half _MatCapIntensity;
    half _MatCapBlendMode;
    half _MatCapBlend;
    half _MatCapBlur;
    #endif

    // ===== SECTION 6: Effects - Emission (発光効果) =====
    // エミッション、グリッター、ホログラム、グリッチ
    // 自己発光・特殊視覚効果系のエフェクト群

    // Glitter
    #if defined(_GLITTER)
    half4 _GlitterColor;
    half _GlitterSize;
    half _GlitterDensity;
    half _GlitterSpeed;
    half _GlitterIntensity;
    half _GlitterBlend;
    half _GlitterBlendMode;
    half _GlitterBlur;
    float4 _GlitterMaskScrollSpeed;
    float _GlitterMaskRotateSpeed;
    #endif
    #if defined(_GLINTS_ADVANCED)
    half _GlintsSharpness;
    half _GlintsTemporal;
    half _GlintsNormalJitter;
    #endif

    // Outline
    #if defined(_OUTLINE)
    half4 _OutlineColor;
    half _OutlineWidth;
    half _OutlineDistCompMax;
    #endif

    // Outline Texture Color HSV
    #if defined(_OUTLINE_TEXTURE_COLOR)
    half _OutlineTexColorHueShift;
    half _OutlineTexColorSaturation;
    #endif

    // Emission
    #if defined(_EMISSION)
    half4 _EmissionColor;
    half _EmissionGlow;
    half _EmissionBlend;
    half _EmissionBlendMode;
    half _EmissionBlur;
    #endif

    // ===== SECTION 7: Surface Modification (表面変形) =====
    // ノーマルマップ、パララックス、リフラクション、ディゾルブ、ドリップ、デカール
    // 表面の見た目や形状を変形・修飾するエフェクト群

    // Normal Map
    float4 _BumpMap_ST;
    half _BumpScale;
    half _MicroNormalScale;
    half _MicroNormalTiling;
    half _MicroNormalStrength;
    // Normal Map UV Animation
    float4 _BumpMapScrollSpeed;
    float _BumpMapRotateSpeed;

    // ===== SECTION 8: Advanced Lighting (高度なライティング) =====
    // SSS（サブサーフェス・スキャタリング）、Light Volume、LTCGI
    // リアルタイム光学シミュレーション系の高度なライティングエフェクト

    // Subsurface Scattering
    #if defined(_SSS)
    half4 _SSSColor;
    half _SSSIntensity;
    half _SSSPower;
    half _SSSDistortion;
    half _ThicknessScale;
    half _SSSBlend;
    half _SSSBlendMode;
    half _SSSBlur;
    half _TransmissionStrength;
    #endif

    // SSS LUT (Pre-integrated Subsurface Scattering)
    #if defined(_SSS_LUT)
    half _SSSLUTScale;
    #endif

    // Virtual Expression - Dissolve
    #if defined(_DISSOLVE)
    half _DissolveAmount;
    half _DissolveEdgeWidth;
    half4 _DissolveEdgeColor;
    half _DissolveEdgeIntensity;
    half _DissolveBlend;
    half _DissolveBlendMode;
    half _DissolveBlur;
    float4 _DissolveTexScrollSpeed;
    float _DissolveTexRotateSpeed;
    half _DissolveCoordMode;
    half _DissolveWorldAxis;
    float _DissolveWorldMin;
    float _DissolveWorldMax;
    half _DissolveNoiseBlend;
    #endif

    // Virtual Expression - Hue Shift
    #if defined(_HUE_SHIFT)
    half _HueShift;
    half _HueShiftBlend;
    half _HueShiftBlur;
    #endif

    // Virtual Expression - Emission Animation
    #if defined(_EMISSION)
    float _EmissionScrollSpeed;
    float _EmissionScrollSpeedY;
    float _EmissionRotateSpeed;
    float4 _EmissionMaskScrollSpeed;
    float _EmissionMaskRotateSpeed;
    half _EmissionPulseSpeed;
    half _EmissionPulseAmplitude;
    #endif

    // Smoothness/Metallic - shared by reflection and realistic-character workflows
    half _Smoothness;
    half _Metallic;

    // Cubemap Reflection (Environment Mapping)
    #if defined(_REFLECTION)
    half4 _ReflectionColor;
    half _ReflectionIntensity;
    half _FresnelPower;
    half _FresnelSoftness;
    half _ReflectionBlendMode;
    half _ReflectionBlend;
    #endif
    half _ClearCoatIntensity;
    half _ClearCoatSmoothness;
    half _ClearCoatNormalScale;
    half _ClearCoatFresnelPower;

    // Iridescence
    #if defined(_IRIDESCENCE)
    half4 _IridescenceColor;
    half _IridescenceIntensity;
    half _IridescenceHueShift;
    half _IridescenceSize;
    half _IridescenceBlend;
    half _IridescenceBlendMode;
    half _IridescenceBlur;
    #endif

    // Environmental Rim
    #if defined(_ENV_RIM)
    half4 _EnvRimColor;
    half _EnvRimPower;
    half _EnvRimIntensity;
    half _EnvRimBlend;
    half _EnvRimBlendMode;
    half _EnvRimBlur;
    #endif

    // Parallax Mapping
    #if defined(_PARALLAX)
    float _Parallax;
    float _ParallaxScale;
    float _ParallaxMinSamples;
    float _ParallaxMaxSamples;
    #endif

    #if defined(_EYE_PARALLAX)
    float _EyeParallaxDepth;
    #endif

    // Refraction
    #if defined(_REFRACTION)
    half _RefractionIndex;
    half _RefractionIntensity;
    half _RefractionBlur;
    half _RefractionBlend;
    half _RefractionBlendMode;
    #endif

    // MatCap 2 & 3
    #if defined(_MATCAP_2)
    half _MatCapIntensity2;
    half _MatCapBlendMode2;
    half _MatCapBlend2;
    half _MatCap2Blur;
    #endif
    #if defined(_MATCAP_3)
    half _MatCapIntensity3;
    half _MatCapBlendMode3;
    half _MatCapBlend3;
    half _MatCap3Blur;
    #endif

    // Rim Direction Control
    float4 _RimLightDirection;
    half _RimDirectionRange;
    half _RimDirStrength;
    half _RimShadowMask;

    // Shadow Color Texture
    half _ShadowColorTexStrength;

    // Outline Multi-Color
    half4 _OutlineColor2;
    half _OutlineColorMix;

    // AudioLink
    #if defined(_AUDIOLINK)
    half _AudioLinkEmissionBand;
    half _AudioLinkEmissionIntensity;
    half _AudioLinkRimBand;
    half _AudioLinkRimIntensity;
    half _AudioLinkHueBand;
    half _AudioLinkHueShiftIntensity;
    half _AudioLinkDissolveBand;
    half _AudioLinkDissolveIntensity;
    half _AudioLinkOutlineBand;
    half _AudioLinkOutlineIntensity;
    half _AudioLinkBlend;
    half _AudioLinkBlendMode;
    #endif

    // ===== SECTION 9: Distance Fade System (距離フェード) =====
    // グローバルフェード設定 + 各エフェクト個別のフェード制御
    // カメラ距離に応じたLOD/パフォーマンス最適化システム

    // Distance Fade
    #if defined(_DISTANCE_FADE)
    float _DistanceFadeStart;
    float _DistanceFadeEnd;
    half _DistanceFadeMode;
    half _DistanceFadeBlend;
    half _DistFadeBlur;
    float _NearFadeStart;
    float _NearFadeEnd;
    half _DistFadeDitherScale;
    // ===== Per-Effect Distance Fade Blend =====
    // 各エフェクトに個別の距離フェードブレンド (0=フェード無し, 1=完全フェード)
    // Effect List:
    //   Specular / HairSpec / SSS
    //   Rim(1,2) / OffsetRim / EnvRim
    //   MatCap(1,2,3) / Reflection / Refraction
    //   Emission / AudioLink / Glitter / Iridescence
    //   Drip / Hologram / Glitch / Decal / Backlight
    half _SpecularDistFade;
    half _HairSpecDistFade;
    half _SSSDistFade;
    half _RimDistFade;
    half _Rim2DistFade;
    half _OffsetRimDistFade;
    half _EnvRimDistFade;
    half _MatCapDistFade;
    half _MatCap2DistFade;
    half _MatCap3DistFade;
    half _ReflectionDistFade;
    half _RefractionDistFade;
    half _EmissionDistFade;
    half _AudioLinkDistFade;
    half _GlitterDistFade;
    half _IridescenceDistFade;
    half _DripDistFade;
    half _HologramDistFade;
    half _GlitchDistFade;
    half _DecalDistFade;
    half _Decal2DistFade;
    half _Decal3DistFade;
    half _Decal4DistFade;
    half _BacklightDistFade;
    half _SmearDistFade;
    #endif

    // Height Fade
    #if defined(_HEIGHT_FADE)
    float _HeightFadeStart;
    float _HeightFadeEnd;
    half _HeightFadeAxis;
    half _HeightFadeSpace;
    half _HeightFadeInvert;
    half _HeightFadeMode;
    half _HeightFadeBlend;
    half _HeightFadeDitherScale;
    half _HeightFadeEdgeWidth;
    half4 _HeightFadeEdgeColor;
    #endif

    // Intersection Fade
    #if defined(_INTERSECTION_FADE)
    half _IntersectionFadeDistance;
    half _IntersectionFadeMode;
    half _IntersectionFadeBlend;
    half _IntersectionFadeDitherScale;
    half _IntersectionFadeEdgeWidth;
    half4 _IntersectionFadeEdgeColor;
    #endif

    // ===== SECTION 10: Vertex & Special Features =====
    // 頂点アニメーション、VAT、テッセレーション、AudioLink、ディザリング
    // 頂点変形・外部連携・特殊レンダリング機能

    // Vertex Animation
    #if defined(_VERTEX_ANIMATION)
    half _VertexAnimSpeed;
    half _VertexAnimStrength;
    half _VertexAnimFrequency;
    half _VertexAnimType;
    #endif

    // Hologram & Glitch (conditionally compiled for optimization)
    #if defined(_HOLOGRAM)
    half _HologramScanlineSpeed;
    half _HologramScanlineIntensity;
    half _HologramFlickerSpeed;
    half _HologramFlickerAmount;
    half4 _HologramColor;
    half _HologramEdgeGlowPower;
    half _HologramEdgeGlowIntensity;
    half _HologramScanlineDensity;
    half _HologramScanlineWidth;
    half _HologramAlpha;
    half _HologramNoiseIntensity;
    half _HologramNoiseSpeed;
    half _HologramMonochrome;
    half _HologramBlend;
    half _HologramBlendMode;
    half _HologramBlur;
    float4 _HologramMaskScrollSpeed;
    float _HologramMaskRotateSpeed;
    #endif
    #if defined(_GLITCH)
    half _GlitchIntensity;
    half _GlitchSpeed;
    half _GlitchBlockSize;
    half _GlitchRGBSplitIntensity;
    half _GlitchFrequency;
    half _GlitchBlend;
    half _GlitchBlendMode;
    half _GlitchBlur;
    float4 _GlitchMask_ST;
    half _GlitchMaskScale;
    half _GlitchMaskAffectsRGBSplit;
    half _GlitchMaskAffectsFrequency;
    float4 _GlitchNoiseTex_ST;
    half _GlitchNoiseIntensity;
    float4 _GlitchNoiseScrollSpeed;
    half _GlitchNoiseMode;
    #endif

    // ===== Glitch Stretch =====
    #if defined(_GLITCH_STRETCH)
    half _GlitchStretchIntensity;
    half _GlitchStretchSpeed;
    half _GlitchStretchBlockSize;
    half _GlitchStretchFrequency;
    float4 _GlitchStretchMask_ST;
    half _GlitchStretchMaskScale;
    #endif

    // Decal
    #if defined(_DECAL)
    half4 _DecalColor;
    float4 _DecalPosition;
    float _DecalRotation;
    float _DecalScale;
    half _DecalBlendMode;
    half _DecalBlend;
    half _DecalBlur;
    #endif

    // Decal Layer 2
    #if defined(_DECAL2)
    half4 _DecalColor2;
    float4 _DecalPosition2;
    float _DecalRotation2;
    float _DecalScale2;
    half _DecalBlendMode2;
    half _DecalBlend2;
    half _DecalBlur2;
    #endif

    // Decal Layer 3
    #if defined(_DECAL3)
    half4 _DecalColor3;
    float4 _DecalPosition3;
    float _DecalRotation3;
    float _DecalScale3;
    half _DecalBlendMode3;
    half _DecalBlend3;
    half _DecalBlur3;
    #endif

    // Decal Layer 4
    #if defined(_DECAL4)
    half4 _DecalColor4;
    float4 _DecalPosition4;
    float _DecalRotation4;
    float _DecalScale4;
    half _DecalBlendMode4;
    half _DecalBlend4;
    half _DecalBlur4;
    #endif

    // Backface Texture
    #if defined(_BACKFACE_TEXTURE)
    half4 _BackfaceColor;
    half _BackfaceBlend;
    half _BackfaceBlendMode;
    #endif

    // Video Texture
    #if defined(_VIDEO_TEXTURE)
    half _VideoEmission;
    half _VideoBlend;
    half _VideoBlendMode;
    #endif

    // LTCGI
    #if defined(_LTCGI)
    half _LTCGIIntensity;
    half _LTCGISpecular;
    half _LTCGIBlend;
    half _LTCGIBlendMode;
    #endif

    // Dithering Alpha
    #if defined(_DITHERING_ALPHA)
    half _DitheringAlphaScale;
    #endif
    #if defined(_HASHED_ALPHA)
    half _HashedAlphaScale;
    #endif

    // Water Drip Effect
    #if defined(_WATER_DRIP)
    half4 _DripColor;
    half _DripSpeed;
    half _DripDensity;
    half _DripSize;
    half _DripTrailLength;
    half _DripIntensity;
    half _DripSharpness;
    half _DripBlend;
    half _DripBlendMode;
    half _DripBlur;
    float4 _DripMaskScrollSpeed;
    float _DripMaskRotateSpeed;
    #endif

    // Vertex Animation Texture (VAT)
    #if defined(_VAT)
    float4 _VATPositionMap_ST;
    float4 _VATNormalMap_ST;
    half _VATNumOfFrames;
    half _VATSpeed;
    half _VATIntensity;
    half _VATPadding;
    float _VATPositionMin;
    float _VATPositionMax;
    float _VATNormalMin;
    float _VATNormalMax;
    half _VATPackingMode;
    #endif

    // Tessellation
    #if defined(_TESSELLATION)
    half _TessFactor;
    half _TessPhongStrength;
    half _TessNormalSmooth;
    float _TessDistanceMin;
    float _TessDistanceMax;
    half _TessDispStrength;
    half _TessDispOffset;
    #endif

    // Smear Effect (スミア / 残像エフェクト)
    #if defined(_SMEAR)
    half _SmearStretch;
    float4 _SmearDirection;
    half _SmearNoiseScale;
    half _SmearNoiseStrength;
    half _SmearTrailLength;
    half _SmearTrailFade;
    half4 _SmearGlowColor;
    half _SmearGlowIntensity;
    half _SmearGlowPower;
    half _SmearEmission;
    half4 _SmearEmissionColor;
    half _SmearBlend;
    half _SmearBlendMode;
    half _SmearBlur;
    float4 _SmearMaskScrollSpeed;
    float _SmearMaskRotateSpeed;
    half _SmearAutoMagnitude;
    half _SmearMotionSensitivity;
    half _SmearVATVelocity;
    #endif

    // ===== 11. Fur (Shell-Based) =====
    #if defined(_FUR)
    half _FurLength;
    half _FurDensity;
    half _FurAlphaCutoff;
    half _FurGravity;
    half4 _FurRootColor;
    half4 _FurTipColor;
    half _FurColorBlend;
    half _FurAO;
    half _FurShadowStrength;
    float4 _FurWindDirection;
    half _FurWindSpeed;
    half _FurWindStrength;
    half _FurSpecular;
    half _FurRimLight;
    half _FurLODDistance;
    half _FurLODMinLayers;
    float4 _FurNoiseTex_ST;
    #endif

    // ===== 12. Background Mode (背景モード) =====
    #if defined(_BACKGROUND_MODE)
        half _LightmapToonInfluence;
        half _LightmapIntensity;
    #endif

    // ===== 13. PBR Mode (物理ベースレンダリング) =====
    #if defined(_PBR)
        half _PBR_Metallic;
        half _PBR_Smoothness;
        half _PBR_OcclusionStrength;
        half _PBR_ReflectionIntensity;
    #endif

    // ===== 14. Detail Map (ディテールマップ) =====
    #if defined(_DETAIL_MAP)
        half _DetailNormalScale;
        half _DetailAlbedoScale;
        half _DetailUVSet;
        half _DetailTiling;
    #endif

    // ===== 15. Triplanar Mapping (トライプレーナー) =====
    #if defined(_TRIPLANAR)
        half _TriplanarScale;
        half _TriplanarBlendSharpness;
        float _TriplanarOffsetX;
        float _TriplanarOffsetY;
        float _TriplanarOffsetZ;
    #endif

    // ===== 16. Height Fog (ハイトフォグ) =====
    #if defined(_HEIGHT_FOG)
        half4 _HeightFogColor;
        float _HeightFogStart;
        float _HeightFogEnd;
        half _HeightFogDensity;
        half _HeightFogMode;
    #endif

    // ===== 17. Surface Cover (雪/砂堆積) =====
    #if defined(_SURFACE_COVER)
        half4 _CoverColor;
        half _CoverAmount;
        half _CoverThreshold;
        half _CoverBlendSharpness;
        half _CoverTiling;
        float4 _CoverDirection;
    #endif

    // ===== 18. Mirror / Camera Control (ミラー・カメラ制御) =====
    #if defined(_MIRROR_CONTROL)
        half _MirrorMode;
        half _CameraMode;
        half _MirrorEmissionMultiplier;
    #endif

    // ===== 19. Quest Lite (Quest軽量パス) =====
    // No CBUFFER properties needed - keyword only

    // ===== 20. PCSS (Percentage Closer Soft Shadows) =====
    #if defined(_PCSS)
    half _PCSSLightSize;
    half _PCSSSoftness;
    half _PCSSBlockerSearchRadius;
    half _PCSSMinFilterRadius;
    half _PCSSMaxFilterRadius;
    half _PCSSSampleCount;
    half _PCSSBlendMode;
    half _PCSSBlend;
    half _PCSSBlur;
    #endif

    // ===== 21. Illustration Style (イラスト風技法) =====
    #ifdef _COLOR_QUANTIZE
    half _QuantizeMode;
    half _QuantizeLevels;
    half _QuantizeHueLevels;
    half _QuantizeSatLevels;
    half _QuantizeValLevels;
    half _QuantizeDither;
    half _QuantizeBlend;
    #endif
    #ifdef _LUT_3D
    half _LUT3DIntensity;
    half _LUT3DSize;
    #endif
    #ifdef _HATCHING
    half _HatchingTiling;
    half4 _HatchingColor;
    half _HatchingBlend;
    #endif
    #ifdef _WATERCOLOR
    half _WCEdgeDarkening;
    half _WCWetEdge;
    half _WCGranulation;
    half _WCPaperIntensity;
    half _WCPaperTiling;
    half _WCBlend;
    float4 _WCGranulationTex_ST;
    float4 _WCPaperTex_ST;
    float4 _WCMask_ST;
    #endif
    #ifdef _SOFT_FILTER
    half _SoftFilterRadius;
    half _SoftFilterBlend;
    half _SoftFilterThreshold;
    half _SoftFilterMode;
    #endif
    #ifdef _KUWAHARA_FILTER
    half _KuwaharaRadius;
    half _KuwaharaBlend;
    #endif
    #ifdef _SCREEN_EDGE
    half4 _EdgeColor;
    half _EdgeWidth;
    half _EdgeDepthSensitivity;
    half _EdgeNormalSensitivity;
    half _EdgeBlend;
    #endif
    #ifdef _COLOR_BLEEDING
    half _BleedingRadius;
    half _BleedingBlend;
    #endif
    #ifdef _CHROMATIC_ABERRATION
    half _CAIntensity;
    half _CABlend;
    #endif
    #ifdef _OUTLINE_HAND_DRAWN
    half _OutlineNoiseTiling;
    half _OutlineWidthVariation;
    half _OutlineJitterAmount;
    #endif
    #if defined(_PROCEDURAL_MATCAP)
    half4 _ProcMatCapColor;
    half _ProcMatCapPower;
    half _ProcMatCapIntensity;
    half _ProcMatCapFresnelPower;
    half _ProcMatCapBlend;
    half _ProcMatCapBlendMode;
    #endif
    #if defined(_FAKE_REFLECTION)
    half4 _FakeReflSkyColor;
    half4 _FakeReflGroundColor;
    half _FakeReflIntensity;
    half _FakeReflFresnelPower;
    half _FakeReflSmoothness;
    half _FakeReflBlend;
    half _FakeReflBlendMode;
    #endif
    #if defined(_SHADOW_EDGE_NOISE)
    half _ShadowNoiseScale;
    half _ShadowNoiseIntensity;
    half _ShadowNoiseSpeed;
    #endif
    #if defined(_LIGHT_SNAP)
    half _LightSnapAngle;
    half _LightSnapSmooth;
    #endif
    #if defined(_CAST_SHADOW_COLOR)
    half4 _CastShadowTint;
    half _CastShadowIntensity;
    #endif
    #if defined(_PERSPECTIVE_FLAT)
    half _PerspectiveFlatAmount;
    #endif
    #if defined(_DEPTH_COLOR_FADE)
    half4 _DepthFadeColor;
    float _DepthFadeStart;
    float _DepthFadeEnd;
    half _DepthFadeIntensity;
    half _DepthFadeDesaturation;
    #endif

    // ===== Feature Toggle Properties =====
    // [Toggle(_KEYWORD)] properties used for runtime feature guards.
    // These must be in the CBUFFER for the if(_Prop >= 0.5) checks
    // in Fragment.hlsl to compile.
    half _MirrorControl;
    half _MainTexAnimation;
    half _GlitchStretch;
    half _Triplanar;
    half _GradientBaseColor;
    half _Use2ndTexture;
    half _Use3rdTexture;
    half _Use4thTexture;
    half _Use5thTexture;
    half _SurfaceCover;
    half _ScreenTone;
    half _UseNormalMap;
    half _DetailMap;
    half _NormalWarp;
    half _UseShadowReceiveMask;
    half _LightSnap;
    half _DistanceFade;
    half _UsePCSS;
    half _VertexColorShadow;
    half _ProceduralAO;
    half _ShadowEdgeNoise;
    half _CastShadowColorEnable;
    half _HalftoneShadow;
    half _UseColorQuantize;
    half _UseLUT3D;
    half _UseHatching;
    half _AngelRing;
    // _SSS: property name == keyword name (_SSS).
    // Unity auto-generates the uniform from Properties block,
    // so explicit CBUFFER declaration would cause redefinition.
    // When keyword active: _SSS is preprocessor define (1) → if(1 >= 0.5) → true.
    // When keyword inactive: _SSS is the auto-generated float uniform.
    half _RimLight;
    half _RimLight2;
    half _OffsetRimLight;
    half _Sheen;
    half _EnvRim;
    half _Specular;
    half _HairSpecular;
    half _MatCap;
    half _MatCap2;
    half _MatCap3;
    half _ProceduralMatCap;
    half _Reflection;
    half _FakeReflection;
    half _Refraction;
    half _Emission;
    half _HueShiftEnable;
    half _AudioLink;
    half _Glitter;
    half _Iridescence;
    half _Smear;
    half _WaterDrip;
    half _Hologram;
    half _Glitch;
    half _Decal;
    half _Decal2;
    half _Decal3;
    half _Decal4;
    half _Dissolve;
    half _UseAlphaMask;
    half _HeightFade;
    half _IntersectionFade;
    half _HeightFog;
    half _DepthColorFade;
    half _HashedAlpha;
    half _DitheringAlpha;
    half _UseWatercolor;
    half _UseSoftFilter;
    half _UseKuwahara;
    half _UseScreenEdge;
    half _UseColorBleeding;
    half _UseChromaticAberration;
    half _IDMask;

    // ===== 22. ID Mask System (領域マスクシステム) =====
    #if defined(_IDMASK)
    float4 _IDMaskTex_ST;
    half4 _IDMaskColor1;
    half4 _IDMaskColor2;
    half4 _IDMaskColor3;
    half4 _IDMaskColor4;
    half _IDMaskBlendMode;
    #endif

    // ===== 23. Flipbook Animation (フリップブックアニメーション) =====
    #if defined(_FLIPBOOK)
    float4 _FlipbookTex_ST;
    half4 _FlipbookColor;
    half _FlipbookColumns;
    half _FlipbookRows;
    half _FlipbookSpeed;
    half _FlipbookBlendMode;
    half _FlipbookAlpha;
    #endif
    half _Flipbook;

CBUFFER_END

// ===== VRChat Shader Globals (set by VRChat runtime) =====
// These uniform variables are provided by VRChat and available in all worlds.
// Always declared globally so view-dependent effects can compensate for mirror flipping,
// regardless of whether the user has enabled the Mirror/Camera Control feature.
// Reference: https://creators.vrchat.com/worlds/udon/vrc-graphics/vrchat-shader-globals/

// Mirror Detection
// Guarded: may already be declared by NataneToonUtils.hlsl for standalone passes (e.g. FurShell)
#ifndef NATANE_VRCHAT_GLOBALS_DECLARED
#define NATANE_VRCHAT_GLOBALS_DECLARED
float _VRChatMirrorMode;      // 0=Normal, 1=Mirror(VR), 2=Mirror(Desktop)
float _VRChatCameraMode;      // 0=Normal, 1=VR handheld camera, 2=Desktop camera, 3=Screenshot
#endif
float _VRChatFaceMirrorMode;  // Face mirror mode

// Camera Detection (extended - always declared here)
float _VRChatCameraMask;      // Camera layer mask

// Camera Positions & Rotations
float3 _VRChatMirrorCameraPos;    // Mirror camera world position
float3 _VRChatScreenCameraPos;    // Screen camera world position
float4 _VRChatScreenCameraRot;    // Screen camera rotation (quaternion)
float3 _VRChatPhotoCameraPos;     // Photo camera world position
float4 _VRChatPhotoCameraRot;     // Photo camera rotation (quaternion)

// Texture samplers (must be outside CBUFFER per HLSL specification)
// Main
sampler2D _MainTex;
#if defined(UNITY_SEPARATE_TEXTURE_SAMPLER)
SamplerState sampler_linear_repeat;
SamplerState sampler_linear_clamp;
#endif

// ===== NOSAMPLER Sampling Macros =====
// Group A (Repeat+Bilinear): share sampler_linear_repeat
// Group B (Clamp+Bilinear):  share sampler_linear_clamp
#if defined(UNITY_SEPARATE_TEXTURE_SAMPLER)
#ifndef NATANE_SAMPLE_SHARED
    // Use Unity's inline sampler names so tessellation/vertex programs do not rely on
    // a texture-bound sampler declaration that may not exist for NOSAMPLER textures.
    #define NATANE_SAMPLE_SHARED(tex, samplerTex, coord) tex.Sample(sampler_linear_repeat, coord)
    #define NATANE_SAMPLE_SHARED_R(tex, samplerTex, coord) NATANE_SAMPLE_SHARED(tex, samplerTex, coord).r
#endif

    #define NATANE_SAMPLE_REPEAT(tex, uv)   tex.Sample(sampler_linear_repeat, uv)
    #define NATANE_SAMPLE_REPEAT_R(tex, uv) NATANE_SAMPLE_REPEAT(tex, uv).r
    #define NATANE_SAMPLE_CLAMP(tex, uv)    tex.Sample(sampler_linear_clamp, uv)
    #define NATANE_SAMPLE_CLAMP_R(tex, uv)  NATANE_SAMPLE_CLAMP(tex, uv).r

    // tex2Dlod replacements for vertex/tessellation/fur shell
    #define NATANE_SAMPLE_REPEAT_LOD(tex, uv, lod) tex.SampleLevel(sampler_linear_repeat, uv, lod)
    #define NATANE_SAMPLE_CLAMP_LOD(tex, uv, lod)  tex.SampleLevel(sampler_linear_clamp, uv, lod)
#else
    #ifndef NATANE_SAMPLE_SHARED
    #define NATANE_SAMPLE_SHARED(tex, samplerTex, coord) UNITY_SAMPLE_TEX2D_SAMPLER(tex, samplerTex, coord)
    #define NATANE_SAMPLE_SHARED_R(tex, samplerTex, coord) NATANE_SAMPLE_SHARED(tex, samplerTex, coord).r
    #endif

    #define NATANE_SAMPLE_REPEAT(tex, uv)   UNITY_SAMPLE_TEX2D_SAMPLER(tex, _MainTex, uv)
    #define NATANE_SAMPLE_REPEAT_R(tex, uv) NATANE_SAMPLE_REPEAT(tex, uv).r
    #define NATANE_SAMPLE_CLAMP(tex, uv)    UNITY_SAMPLE_TEX2D_SAMPLER(tex, _linear_clamp, uv)
    #define NATANE_SAMPLE_CLAMP_R(tex, uv)  NATANE_SAMPLE_CLAMP(tex, uv).r

    #define NATANE_SAMPLE_REPEAT_LOD(tex, uv, lod) tex2Dlod(tex, float4(uv, 0, lod))
    #define NATANE_SAMPLE_CLAMP_LOD(tex, uv, lod)  tex2Dlod(tex, float4(uv, 0, lod))
#endif

// Makeup Textures
#if defined(_2ND_TEXTURE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_2ndTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_2ndTexMask);
#endif
#if defined(_3RD_TEXTURE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_3rdTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_3rdTexMask);
#endif
#if defined(_4TH_TEXTURE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_4thTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_4thTexMask);
#endif
#if defined(_5TH_TEXTURE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_5thTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_5thTexMask);
#endif

// Screen-Tone
#if defined(_SCREEN_TONE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_ScreenToneMask);
#endif

// Shading
#if defined(_USE_RAMP)
UNITY_DECLARE_TEX2D_NOSAMPLER(_RampTex);
#endif
#if defined(_SHADOW_RECEIVE_MASK)
UNITY_DECLARE_TEX2D_NOSAMPLER(_ShadowReceiveMask);
#endif
UNITY_DECLARE_TEX2D_NOSAMPLER(_ShadowColorTex);

// SDF & Grade Maps
#if defined(_SDF_MAP)
UNITY_DECLARE_TEX2D_NOSAMPLER(_SDFMap);
#endif
#if defined(_SHADING_GRADE_MAP)
UNITY_DECLARE_TEX2D_NOSAMPLER(_ShadingGradeMap);
#endif

// Ambient Occlusion
#if defined(_USE_AO)
UNITY_DECLARE_TEX2D_NOSAMPLER(_AOMap);
#endif
UNITY_DECLARE_TEX2D_NOSAMPLER(_CavityMap);

// Specular
#if defined(_SPECULAR)
UNITY_DECLARE_TEX2D_NOSAMPLER(_SpecularMask);
#endif
UNITY_DECLARE_TEX2D_NOSAMPLER(_SkinSpecMask);

// Hair Specular
#if defined(_HAIR_SPECULAR)
UNITY_DECLARE_TEX2D_NOSAMPLER(_HairSpecMask);
UNITY_DECLARE_TEX2D_NOSAMPLER(_HairSpecShiftTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_HairStrandDirectionMap);
UNITY_DECLARE_TEX2D_NOSAMPLER(_HairTransmissionMask);
#endif

// Angel Ring
#if defined(_ANGEL_RING)
UNITY_DECLARE_TEX2D_NOSAMPLER(_AngelRingTex);
#endif

// Rim Light
#if defined(_RIM_LIGHT)
UNITY_DECLARE_TEX2D_NOSAMPLER(_RimMask);
#endif
#if defined(_RIM_LIGHT_2)
UNITY_DECLARE_TEX2D_NOSAMPLER(_RimMask2);
#endif
#if defined(_OFFSET_RIM_LIGHT)
UNITY_DECLARE_TEX2D_NOSAMPLER(_OffsetRimMask);
#endif
#if defined(_SHEEN)
UNITY_DECLARE_TEX2D_NOSAMPLER(_SheenMask);
#endif

// MatCap
#if defined(_MATCAP)
UNITY_DECLARE_TEX2D_NOSAMPLER(_MatCapTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_MatCapMask);
#endif
#if defined(_MATCAP_2)
UNITY_DECLARE_TEX2D_NOSAMPLER(_MatCapTex2);
UNITY_DECLARE_TEX2D_NOSAMPLER(_MatCapMask2);
#endif
#if defined(_MATCAP_3)
UNITY_DECLARE_TEX2D_NOSAMPLER(_MatCapTex3);
UNITY_DECLARE_TEX2D_NOSAMPLER(_MatCapMask3);
#endif

// Glitter
#if defined(_GLITTER)
UNITY_DECLARE_TEX2D_NOSAMPLER(_GlitterMask);
#endif

// Outline
#if defined(_OUTLINE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_OutlineMask);
#endif

// Emission
#if defined(_EMISSION)
UNITY_DECLARE_TEX2D_NOSAMPLER(_EmissionMap);
#endif
#if defined(_EMISSION)
UNITY_DECLARE_TEX2D_NOSAMPLER(_EmissionMask);
#endif

// Normal Map
#if defined(_NORMALMAP)
UNITY_DECLARE_TEX2D_NOSAMPLER(_BumpMap);
#endif
UNITY_DECLARE_TEX2D_NOSAMPLER(_MicroNormalMap);

// Subsurface Scattering
#if defined(_SSS)
UNITY_DECLARE_TEX2D_NOSAMPLER(_ThicknessMap);
UNITY_DECLARE_TEX2D_NOSAMPLER(_TransmissionMask);
UNITY_DECLARE_TEX2D_NOSAMPLER(_SSSMask);
#endif

// SSS LUT
#if defined(_SSS_LUT)
UNITY_DECLARE_TEX2D_NOSAMPLER(_SSSLUTTex);
#endif

// Dissolve
#if defined(_DISSOLVE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_DissolveTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_DissolveMask);
#endif

// Alpha Mask
#if defined(_ALPHA_MASK)
UNITY_DECLARE_TEX2D_NOSAMPLER(_AlphaMask);
#endif

// Reflection
#if defined(_REFLECTION)
UNITY_DECLARE_TEX2D_NOSAMPLER(_ReflectionMask);
#endif
UNITY_DECLARE_TEX2D_NOSAMPLER(_ClearCoatMask);
UNITY_DECLARE_TEX2D_NOSAMPLER(_ClearCoatNormalMap);

// Iridescence
#if defined(_IRIDESCENCE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_IridescenceMask);
#endif

// Environmental Rim
#if defined(_ENV_RIM)
UNITY_DECLARE_TEX2D_NOSAMPLER(_EnvRimMask);
#endif

// Parallax
#if defined(_PARALLAX)
UNITY_DECLARE_TEX2D_NOSAMPLER(_ParallaxMap);
#endif

// Refraction
#if defined(_REFRACTION)
UNITY_DECLARE_TEX2D_NOSAMPLER(_RefractionMask);
#endif

// Decal
#if defined(_DECAL)
UNITY_DECLARE_TEX2D_NOSAMPLER(_DecalTex);
#endif

// Decal Layer 2
#if defined(_DECAL2)
UNITY_DECLARE_TEX2D_NOSAMPLER(_DecalTex2);
#endif

// Decal Layer 3
#if defined(_DECAL3)
UNITY_DECLARE_TEX2D_NOSAMPLER(_DecalTex3);
#endif

// Decal Layer 4
#if defined(_DECAL4)
UNITY_DECLARE_TEX2D_NOSAMPLER(_DecalTex4);
#endif

// Backface
#if defined(_BACKFACE_TEXTURE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_BackfaceTex);
#endif

// Video
#if defined(_VIDEO_TEXTURE)
sampler2D _VideoTex;
#endif

// Vertex Animation
#if defined(_VERTEX_ANIMATION)
UNITY_DECLARE_TEX2D_NOSAMPLER(_VertexAnimMask);
#endif

// Water Drip
#if defined(_WATER_DRIP)
UNITY_DECLARE_TEX2D_NOSAMPLER(_DripMask);
#endif

// Smear
#if defined(_SMEAR)
UNITY_DECLARE_TEX2D_NOSAMPLER(_SmearMask);
#endif

// Fur
#if defined(_FUR)
UNITY_DECLARE_TEX2D_NOSAMPLER(_FurNoiseTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_FurMask);
#endif

// PBR
#if defined(_PBR)
UNITY_DECLARE_TEX2D_NOSAMPLER(_PBR_MetallicGlossMap);
UNITY_DECLARE_TEX2D_NOSAMPLER(_PBR_OcclusionMap);
#endif

// Smooth Normal Texture (for Mode 2: Baked Normal Texture)
#if defined(_SMOOTH_NORMAL)
UNITY_DECLARE_TEX2D_NOSAMPLER(_SmoothNormalTex);
#endif

// Hologram (conditionally compiled)
#if defined(_HOLOGRAM)
UNITY_DECLARE_TEX2D_NOSAMPLER(_HologramMask);
#endif
#if defined(_HOLOGRAM_NOISE)
UNITY_DECLARE_TEX2D_NOSAMPLER(_HologramNoiseTex);
#endif

// Glitch Mask
#if defined(_GLITCH)
UNITY_DECLARE_TEX2D_NOSAMPLER(_GlitchMask);
UNITY_DECLARE_TEX2D_NOSAMPLER(_GlitchNoiseTex);
#endif

// Glitch Stretch Mask
#if defined(_GLITCH_STRETCH)
UNITY_DECLARE_TEX2D_NOSAMPLER(_GlitchStretchMask);
#endif

// Illustration Style Textures
#ifdef _COLOR_QUANTIZE
UNITY_DECLARE_TEX2D_NOSAMPLER(_QuantizeMask);
#endif
#ifdef _LUT_3D
UNITY_DECLARE_TEX2D_NOSAMPLER(_LUT3DTex);
#endif
#ifdef _HATCHING
UNITY_DECLARE_TEX2D_NOSAMPLER(_HatchTex0);
UNITY_DECLARE_TEX2D_NOSAMPLER(_HatchTex1);
UNITY_DECLARE_TEX2D_NOSAMPLER(_HatchingMask);
#endif
#ifdef _WATERCOLOR
UNITY_DECLARE_TEX2D_NOSAMPLER(_WCGranulationTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_WCPaperTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_WCMask);
#endif
#ifdef _SCREEN_EDGE
UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture);
#endif
#ifdef _OUTLINE_HAND_DRAWN
UNITY_DECLARE_TEX2D_NOSAMPLER(_OutlineNoiseTex);
#endif

// ID Mask
#if defined(_IDMASK)
UNITY_DECLARE_TEX2D_NOSAMPLER(_IDMaskTex);
#endif

// Flipbook Animation
#if defined(_FLIPBOOK)
UNITY_DECLARE_TEX2D_NOSAMPLER(_FlipbookTex);
#endif

// VAT
#if defined(_VAT)
UNITY_DECLARE_TEX2D_NOSAMPLER(_VATPositionMap);
UNITY_DECLARE_TEX2D_NOSAMPLER(_VATNormalMap);
#endif

// Tessellation Displacement
#if defined(_TESS_DISPLACEMENT)
UNITY_DECLARE_TEX2D_NOSAMPLER(_TessDispMap);
#endif

// Detail Map
#if defined(_DETAIL_MAP)
UNITY_DECLARE_TEX2D_NOSAMPLER(_DetailAlbedoMap);
UNITY_DECLARE_TEX2D_NOSAMPLER(_DetailNormalMap);
#endif

// Surface Cover
#if defined(_SURFACE_COVER)
UNITY_DECLARE_TEX2D_NOSAMPLER(_CoverTex);
UNITY_DECLARE_TEX2D_NOSAMPLER(_CoverNormalMap);
#endif

// Cubemap samplers (outside CBUFFER)
#if defined(_REFLECTION)
samplerCUBE _ReflectionCube;
#endif
#if defined(_ENV_RIM)
samplerCUBE _EnvRimCube;
#endif

// Intersection Fade / PCSS / Screen Edge — shared _CameraDepthTexture declaration
#if defined(_INTERSECTION_FADE) || defined(_PCSS) || defined(_SCREEN_EDGE)
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
#endif

// PCSS Poisson Disk sampling pattern (32 samples)
#if defined(_PCSS)
static const float2 PoissonDisk32[32] = {
    float2(-0.94201624, -0.39906216), float2( 0.94558609, -0.76890725),
    float2(-0.09418410, -0.92938870), float2( 0.34495938,  0.29387760),
    float2(-0.91588581,  0.45771432), float2(-0.81544232, -0.87912464),
    float2(-0.38277543,  0.27676845), float2( 0.97484398,  0.75648379),
    float2( 0.44323325, -0.97511554), float2( 0.53742981, -0.47373420),
    float2(-0.26496911, -0.41893023), float2( 0.79197514,  0.19090188),
    float2(-0.24188840,  0.99706507), float2(-0.81409955,  0.91437590),
    float2( 0.19984126,  0.78641367), float2( 0.14383161, -0.14100790),
    float2(-0.44451373, -0.69745003), float2( 0.69546413, -0.16150797),
    float2(-0.65731890,  0.68906659), float2( 0.36949191,  0.56157024),
    float2(-0.10214935, -0.18408868), float2( 0.83618390,  0.48918439),
    float2(-0.56318188, -0.29645988), float2( 0.27004808, -0.68117476),
    float2(-0.73403048,  0.09498067), float2( 0.47089072,  0.97014981),
    float2(-0.95723576, -0.09001040), float2( 0.09698980,  0.41944910),
    float2( 0.60395759, -0.74880689), float2(-0.47938831,  0.56906949),
    float2( 0.85680531,  0.91739876), float2(-0.33726816, -0.97149441)
};
#endif

// GrabPass texture for Refraction — declared in NataneToonUtils.hlsl (VR stereo-aware)
// Uses "_nataneBackgroundTexture" named GrabPass (unique name to avoid interference with other shaders)

// AudioLink texture (VRChat)
#if defined(_AUDIOLINK)
sampler2D _AudioTexture;
sampler2D _AudioTexture2D;
#endif

// Vertex Input Structure
struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TEXCOORD0;
    #if defined(_BACKGROUND_MODE) || defined(_DETAIL_MAP) || defined(_LTCGI)
        float2 uv1 : TEXCOORD1;  // Lightmap UV / Detail UV / LTCGI UV
    #endif
    #if defined(_SMOOTH_NORMAL) || defined(_VERTEX_COLOR_SHADOW)
        float4 color : COLOR;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Vertex to Fragment Structure
struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float3 worldTangent : TEXCOORD3;
    float3 worldBinormal : TEXCOORD4;
    UNITY_FOG_COORDS(5)
    SHADOW_COORDS(6)
    #if defined(_REFRACTION) || defined(_PARALLAX) || defined(_DISSOLVE) || defined(_DITHERING_ALPHA) || defined(_HASHED_ALPHA) || defined(_INTERSECTION_FADE) || defined(_SOFT_FILTER) || defined(_KUWAHARA_FILTER) || defined(_COLOR_BLEEDING) || defined(_CHROMATIC_ABERRATION) || defined(_SCREEN_EDGE) || defined(_WATERCOLOR) || defined(_SPECULAR_DITHER)
        float4 screenPos : TEXCOORD7; // For GrabPass / Dithering / Intersection Fade / Illustration Style / Specular Dither
    #endif
    #if defined(VERTEXLIGHT_ON) && !defined(_PIXEL_VERTEX_LIGHTS)
        float3 vertexLightColor : TEXCOORD8;
    #endif
    #ifdef _SMOOTH_NORMAL
        float3 smoothWorldNormal : TEXCOORD9;
    #endif
    #ifdef _SMEAR
        float smearStretchFactor : TEXCOORD10;
    #endif
    #ifdef _BACKGROUND_MODE
        float2 lightmapUV : TEXCOORD11;
    #endif
    #if defined(_DETAIL_MAP) || defined(_LTCGI)
        float2 uv1 : TEXCOORD12;
    #endif
    #ifdef _VERTEX_COLOR_SHADOW
        half4 color : COLOR;
    #endif
    #if defined(_PROCEDURAL_AO) || defined(_NORMAL_WARP)
        float3 objectPos : TEXCOORD13;
    #endif
    #if defined(_NORMAL_WARP)
        float3 objectNormal : TEXCOORD14;
    #endif
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif // NATANE_TOON_INPUT_INCLUDED
