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
    float _AlbedoPreservation;
    float _Saturation;
    float _Brightness;

    // Surface Finish
    float _Glossiness;
    float _MatteEffect;

    // Final Color Blending
    float _FinalHighlightBlend;
    float _HighlightThreshold;
    float _FinalShadowBlend;
    float _ShadowThreshold;

    // ===== SECTION 2: Makeup Textures (マルチレイヤー) =====
    // 2nd〜5thテクスチャレイヤー（各8パラメータ）
    // メインテクスチャの上に重ねるメイクアップ/デカール用マルチレイヤーシステム

    // Makeup Textures
    float4 _2ndTex_ST;
    float _2ndTexHueShift;
    float _2ndTexSaturation;
    float _2ndTexValue;
    float _2ndTexIntensity;
    float _2ndTexBlendMode;
    float4 _2ndTexScrollSpeed;
    float _2ndTexRotateSpeed;

    float4 _3rdTex_ST;
    float _3rdTexHueShift;
    float _3rdTexSaturation;
    float _3rdTexValue;
    float _3rdTexIntensity;
    float _3rdTexBlendMode;
    float4 _3rdTexScrollSpeed;
    float _3rdTexRotateSpeed;

    float4 _4thTex_ST;
    float _4thTexHueShift;
    float _4thTexSaturation;
    float _4thTexValue;
    float _4thTexIntensity;
    float _4thTexBlendMode;
    float4 _4thTexScrollSpeed;
    float _4thTexRotateSpeed;

    float4 _5thTex_ST;
    float _5thTexHueShift;
    float _5thTexSaturation;
    float _5thTexValue;
    float _5thTexIntensity;
    float _5thTexBlendMode;
    float4 _5thTexScrollSpeed;
    float _5thTexRotateSpeed;

    // Screen-Tone Overlay
    #if defined(_SCREEN_TONE)
    half4 _ScreenToneColor;
    float _ScreenToneScale;
    float _ScreenToneThreshold;
    float _ScreenToneBlend;
    float _ScreenToneBlendMode;
    float _ScreenToneBlur;
    #endif

    // Gradient Base Color
    #if defined(_GRADIENT_BASE_COLOR)
    half4 _GradientTopColor;
    half4 _GradientBottomColor;
    float _GradientAxis;
    float _GradientSpace;
    float _GradientStart;
    float _GradientEnd;
    float _GradientBlend;
    float _GradientBlendMode;
    #endif

    // ===== SECTION 3: Lighting & Shading (ライティング基本) =====
    // シェーディングモード、マルチトーン、SDFシャドウマップ、影設定
    // トゥーンシェーディングの核となるライティング計算パラメータ群

    // Shading
    float _ShadingMode;
    float _ShadingGradientWidth;
    half4 _ShadowColor;
    half4 _Shadow2ndColor;
    float _Shadow2ndBorder;
    half4 _Shadow3rdColor;
    float _Shadow3rdBorder;
    float _ShadowSteps;
    float _ShadowSharpness;
    float _StepBorderSmooth;
    float _ShadowOffset;
    float _LitSoftness;
    float _ShadowBlend;

    // SDF Shadow Map
    float _SDFIntensity;
    float _SDFSoftness;
    float _SDFOffset;

    // Face SDF Rotation
    float4 _FaceForwardDirection;
    float4 _FaceRightDirection;

    // Shading Grade Map
    float _ShadingGradeScale;

    // Ambient Occlusion
    float _AOIntensity;
    float _AOBlend;
    float _AOBlendMode;
    float _AOBlur;

    // Dithering
    float _DitheringScale;
    float _DitheringStrength;
    float _DitheringBlend;
    float _DitheringBlur;

    // Advanced Lighting Controls
    float _SoftLightingIntensity;
    float _LightIntensity;
    float _IndirectLightIntensity;
    float _GIIntensity; // Environment Reflection (GI/Light Probes) intensity control
    float _LightColorInfluence;
    float _ShadowReceive;
    float _ShadowSmoothing;
    float _ShadowMaxDarkness;
    float _SmoothNormalShadingBlend;
    float _SmoothNormalMode;
    float _LightMinInfluence;
    float _LightMaxInfluence;
    float _LightBlend;
    float _HighlightSoftness;
    float _BacklightIntensity;
    half4 _BacklightColor;
    float _BacklightBlend;
    float _BacklightBlendMode;
    float _BacklightBlur;
    float _AdditionalLightIntensity;

    // VRC Light Volumes
    float _LightVolumeIntensity;
    float _LightVolumeBlendMode;
    float _LightVolumeBlend;

    // Indirect Lighting (min color + max composition)
    half4 _IndirectLightMinColor;
    float _ShadowEnvStrength;

    // ===== SECTION 4: Effects - Light Based (光源依存エフェクト) =====
    // スペキュラ、ヘアスペキュラ、リムライト、バックライト
    // ライト方向に依存して変化するエフェクト群

    // Specular
    #if defined(_SPECULAR)
    half4 _SpecularColor;
    float _SpecularSize;
    float _SpecularSoftness;
    float _SpecularBlend;
    float _SpecularBlendMode;
    float _SpecularBlur;
    float4 _SpecularMaskScrollSpeed;
    float _SpecularMaskRotateSpeed;
    #endif

    // Hair Specular (Kajiya-Kay)
    #if defined(_HAIR_SPECULAR)
    half4 _HairSpecColor1;
    float _HairSpecShift1;
    float _HairSpecWidth1;
    half4 _HairSpecColor2;
    float _HairSpecShift2;
    float _HairSpecWidth2;
    float _HairSpecIntensity;
    float _HairSpecBlend;
    float _HairSpecBlendMode;
    #endif

    // Rim Light
    #if defined(_RIM_LIGHT)
    half4 _RimColor;
    float _RimPower;
    float _RimIntensity;
    float _RimSpread;
    float _RimBlend;
    float _RimBlendMode;
    float _RimBlur;
    float4 _RimMaskScrollSpeed;
    float _RimMaskRotateSpeed;
    #endif

    // Rim Light 2
    #if defined(_RIM_LIGHT_2)
    half4 _RimColor2;
    float _RimPower2;
    float _RimIntensity2;
    float _RimSpread2;
    float _RimBlend2;
    float _RimBlendMode2;
    float _Rim2Blur;
    float4 _RimMask2ScrollSpeed;
    float _RimMask2RotateSpeed;
    #endif

    // Offset Rim Light
    #if defined(_OFFSET_RIM_LIGHT)
    half4 _OffsetRimColor;
    float _OffsetRimPower;
    float _OffsetRimIntensity;
    float _OffsetRimOffsetX;
    float _OffsetRimOffsetY;
    float _OffsetRimUseLightDir;
    float _OffsetRimLightDirStrength;
    float _OffsetRimSharpness;
    float _OffsetRimShadowMask;
    float _OffsetRimBlend;
    float _OffsetRimBlendMode;
    float _OffsetRimBlur;
    #endif

    // ===== SECTION 5: Effects - View Based (視線依存エフェクト) =====
    // MatCap、リフレクション、環境リム、イリデッセンス
    // カメラ/視線方向に依存して変化するエフェクト群

    // MatCap
    #if defined(_MATCAP)
    float _MatCapIntensity;
    float _MatCapBlendMode;
    float _MatCapBlend;
    float _MatCapBlur;
    #endif

    // ===== SECTION 6: Effects - Emission (発光効果) =====
    // エミッション、グリッター、ホログラム、グリッチ
    // 自己発光・特殊視覚効果系のエフェクト群

    // Glitter
    #if defined(_GLITTER)
    half4 _GlitterColor;
    float _GlitterSize;
    float _GlitterDensity;
    float _GlitterSpeed;
    float _GlitterIntensity;
    float _GlitterBlend;
    float _GlitterBlendMode;
    float _GlitterBlur;
    float4 _GlitterMaskScrollSpeed;
    float _GlitterMaskRotateSpeed;
    #endif

    // Outline
    #if defined(_OUTLINE)
    half4 _OutlineColor;
    float _OutlineWidth;
    #endif

    // Emission
    #if defined(_EMISSION)
    half4 _EmissionColor;
    float _EmissionGlow;
    float _EmissionBlend;
    float _EmissionBlendMode;
    float _EmissionBlur;
    #endif

    // ===== SECTION 7: Surface Modification (表面変形) =====
    // ノーマルマップ、パララックス、リフラクション、ディゾルブ、ドリップ、デカール
    // 表面の見た目や形状を変形・修飾するエフェクト群

    // Normal Map
    float _BumpScale;
    // Normal Map UV Animation
    float4 _BumpMapScrollSpeed;
    float _BumpMapRotateSpeed;

    // ===== SECTION 8: Advanced Lighting (高度なライティング) =====
    // SSS（サブサーフェス・スキャタリング）、Light Volume、LTCGI
    // リアルタイム光学シミュレーション系の高度なライティングエフェクト

    // Subsurface Scattering
    #if defined(_SSS)
    half4 _SSSColor;
    float _SSSIntensity;
    float _SSSPower;
    float _SSSDistortion;
    float _ThicknessScale;
    float _SSSBlend;
    float _SSSBlendMode;
    float _SSSBlur;
    #endif

    // Virtual Expression - Dissolve
    #if defined(_DISSOLVE)
    float _DissolveAmount;
    float _DissolveEdgeWidth;
    half4 _DissolveEdgeColor;
    float _DissolveEdgeIntensity;
    float _DissolveBlend;
    float _DissolveBlendMode;
    float _DissolveBlur;
    float4 _DissolveTexScrollSpeed;
    float _DissolveTexRotateSpeed;
    float _DissolveCoordMode;
    float _DissolveWorldAxis;
    float _DissolveWorldMin;
    float _DissolveWorldMax;
    float _DissolveNoiseBlend;
    #endif

    // Virtual Expression - Hue Shift
    #if defined(_HUE_SHIFT)
    float _HueShift;
    float _HueShiftBlend;
    float _HueShiftBlur;
    #endif

    // Virtual Expression - Emission Animation
    #if defined(_EMISSION)
    float _EmissionScrollSpeed;
    float _EmissionScrollSpeedY;
    float _EmissionRotateSpeed;
    float4 _EmissionMaskScrollSpeed;
    float _EmissionMaskRotateSpeed;
    float _EmissionPulseSpeed;
    float _EmissionPulseAmplitude;
    #endif

    // Smoothness/Metallic - shared by Reflection, Light Volume Specular, LTCGI
    #if defined(_REFLECTION) || defined(_USE_LIGHT_VOLUME) || defined(_LTCGI)
    float _Smoothness;
    float _Metallic;
    #endif

    // Cubemap Reflection (Environment Mapping)
    #if defined(_REFLECTION)
    half4 _ReflectionColor;
    float _ReflectionIntensity;
    float _FresnelPower;
    float _FresnelSoftness;
    float _ReflectionBlendMode;
    float _ReflectionBlend;
    #endif

    // Iridescence
    #if defined(_IRIDESCENCE)
    half4 _IridescenceColor;
    float _IridescenceIntensity;
    float _IridescenceHueShift;
    float _IridescenceSize;
    float _IridescenceBlend;
    float _IridescenceBlendMode;
    float _IridescenceBlur;
    #endif

    // Environmental Rim
    #if defined(_ENV_RIM)
    half4 _EnvRimColor;
    float _EnvRimPower;
    float _EnvRimIntensity;
    float _EnvRimBlend;
    float _EnvRimBlendMode;
    float _EnvRimBlur;
    #endif

    // Parallax Mapping
    #if defined(_PARALLAX)
    float _ParallaxScale;
    float _ParallaxMinSamples;
    float _ParallaxMaxSamples;
    #endif

    // Refraction
    #if defined(_REFRACTION)
    float _RefractionIndex;
    float _RefractionIntensity;
    float _RefractionBlur;
    float _RefractionBlend;
    float _RefractionBlendMode;
    #endif

    // MatCap 2 & 3
    #if defined(_MATCAP_2)
    float _MatCapIntensity2;
    float _MatCapBlendMode2;
    float _MatCapBlend2;
    float _MatCap2Blur;
    #endif
    #if defined(_MATCAP_3)
    float _MatCapIntensity3;
    float _MatCapBlendMode3;
    float _MatCapBlend3;
    float _MatCap3Blur;
    #endif

    // Rim Direction Control
    float4 _RimLightDirection;
    float _RimDirectionRange;
    float _RimDirStrength;
    float _RimShadowMask;

    // Shadow Color Texture
    float _ShadowColorTexStrength;

    // Outline Multi-Color
    half4 _OutlineColor2;
    float _OutlineColorMix;

    // AudioLink
    #if defined(_AUDIOLINK)
    float _AudioLinkEmissionBand;
    float _AudioLinkEmissionIntensity;
    float _AudioLinkRimBand;
    float _AudioLinkRimIntensity;
    float _AudioLinkHueBand;
    float _AudioLinkHueShiftIntensity;
    float _AudioLinkDissolveBand;
    float _AudioLinkDissolveIntensity;
    float _AudioLinkOutlineBand;
    float _AudioLinkOutlineIntensity;
    float _AudioLinkBlend;
    float _AudioLinkBlendMode;
    #endif

    // ===== SECTION 9: Distance Fade System (距離フェード) =====
    // グローバルフェード設定 + 各エフェクト個別のフェード制御
    // カメラ距離に応じたLOD/パフォーマンス最適化システム

    // Distance Fade
    #if defined(_DISTANCE_FADE)
    float _DistanceFadeStart;
    float _DistanceFadeEnd;
    float _DistanceFadeMode;
    float _DistanceFadeBlend;
    float _DistFadeBlur;
    float _NearFadeStart;
    float _NearFadeEnd;
    float _DistFadeDitherScale;
    // ===== Per-Effect Distance Fade Blend =====
    // 各エフェクトに個別の距離フェードブレンド (0=フェード無し, 1=完全フェード)
    // Effect List:
    //   Specular / HairSpec / SSS
    //   Rim(1,2) / OffsetRim / EnvRim
    //   MatCap(1,2,3) / Reflection / Refraction
    //   Emission / AudioLink / Glitter / Iridescence
    //   Drip / Hologram / Glitch / Decal / Backlight
    float _SpecularDistFade;
    float _HairSpecDistFade;
    float _SSSDistFade;
    float _RimDistFade;
    float _Rim2DistFade;
    float _OffsetRimDistFade;
    float _EnvRimDistFade;
    float _MatCapDistFade;
    float _MatCap2DistFade;
    float _MatCap3DistFade;
    float _ReflectionDistFade;
    float _RefractionDistFade;
    float _EmissionDistFade;
    float _AudioLinkDistFade;
    float _GlitterDistFade;
    float _IridescenceDistFade;
    float _DripDistFade;
    float _HologramDistFade;
    float _GlitchDistFade;
    float _DecalDistFade;
    float _BacklightDistFade;
    float _SmearDistFade;
    #endif

    // Height Fade
    #if defined(_HEIGHT_FADE)
    float _HeightFadeStart;
    float _HeightFadeEnd;
    float _HeightFadeAxis;
    float _HeightFadeSpace;
    float _HeightFadeInvert;
    float _HeightFadeMode;
    float _HeightFadeBlend;
    float _HeightFadeDitherScale;
    float _HeightFadeEdgeWidth;
    half4 _HeightFadeEdgeColor;
    #endif

    // Intersection Fade
    #if defined(_INTERSECTION_FADE)
    float _IntersectionFadeDistance;
    float _IntersectionFadeMode;
    float _IntersectionFadeBlend;
    float _IntersectionFadeDitherScale;
    float _IntersectionFadeEdgeWidth;
    half4 _IntersectionFadeEdgeColor;
    #endif

    // ===== SECTION 10: Vertex & Special Features =====
    // 頂点アニメーション、VAT、テッセレーション、AudioLink、ディザリング
    // 頂点変形・外部連携・特殊レンダリング機能

    // Vertex Animation
    #if defined(_VERTEX_ANIMATION)
    float _VertexAnimSpeed;
    float _VertexAnimStrength;
    float _VertexAnimFrequency;
    float _VertexAnimType;
    #endif

    // Hologram & Glitch (conditionally compiled for optimization)
    #if defined(_HOLOGRAM)
    float _HologramScanlineSpeed;
    float _HologramScanlineIntensity;
    float _HologramFlickerSpeed;
    float _HologramFlickerAmount;
    half4 _HologramColor;
    float _HologramEdgeGlowPower;
    float _HologramEdgeGlowIntensity;
    float _HologramScanlineDensity;
    float _HologramScanlineWidth;
    float _HologramAlpha;
    float _HologramNoiseIntensity;
    float _HologramNoiseSpeed;
    float _HologramMonochrome;
    float _HologramBlend;
    float _HologramBlendMode;
    float _HologramBlur;
    float4 _HologramMaskScrollSpeed;
    float _HologramMaskRotateSpeed;
    #endif
    #if defined(_GLITCH)
    float _GlitchIntensity;
    float _GlitchSpeed;
    float _GlitchBlockSize;
    float _GlitchRGBSplitIntensity;
    float _GlitchFrequency;
    float _GlitchBlend;
    float _GlitchBlendMode;
    float _GlitchBlur;
    #endif

    // Decal
    #if defined(_DECAL)
    half4 _DecalColor;
    float4 _DecalPosition;
    float _DecalRotation;
    float _DecalScale;
    float _DecalBlendMode;
    float _DecalBlend;
    float _DecalBlur;
    #endif

    // Backface Texture
    #if defined(_BACKFACE_TEXTURE)
    half4 _BackfaceColor;
    float _BackfaceBlend;
    float _BackfaceBlendMode;
    #endif

    // Video Texture
    #if defined(_VIDEO_TEXTURE)
    float _VideoEmission;
    float _VideoBlend;
    float _VideoBlendMode;
    #endif

    // LTCGI
    #if defined(_LTCGI)
    float _LTCGIIntensity;
    float _LTCGISpecular;
    float _LTCGIBlend;
    float _LTCGIBlendMode;
    #endif

    // Dithering Alpha
    #if defined(_DITHERING_ALPHA)
    float _DitheringAlphaScale;
    #endif

    // Water Drip Effect
    #if defined(_WATER_DRIP)
    half4 _DripColor;
    float _DripSpeed;
    float _DripDensity;
    float _DripSize;
    float _DripTrailLength;
    float _DripIntensity;
    float _DripSharpness;
    float _DripBlend;
    float _DripBlendMode;
    float _DripBlur;
    float4 _DripMaskScrollSpeed;
    float _DripMaskRotateSpeed;
    #endif

    // Vertex Animation Texture (VAT)
    #if defined(_VAT)
    float4 _VATPositionMap_ST;
    float4 _VATNormalMap_ST;
    float _VATNumOfFrames;
    float _VATSpeed;
    float _VATIntensity;
    float _VATPadding;
    float _VATPositionMin;
    float _VATPositionMax;
    float _VATNormalMin;
    float _VATNormalMax;
    float _VATPackingMode;
    #endif

    // Tessellation
    #if defined(_TESSELLATION)
    float _TessFactor;
    float _TessPhongStrength;
    float _TessNormalSmooth;
    float _TessDistanceMin;
    float _TessDistanceMax;
    float _TessDispStrength;
    float _TessDispOffset;
    #endif

    // Smear Effect (スミア / 残像エフェクト)
    #if defined(_SMEAR)
    float _SmearStretch;
    float4 _SmearDirection;
    float _SmearNoiseScale;
    float _SmearNoiseStrength;
    float _SmearTrailLength;
    float _SmearTrailFade;
    half4 _SmearGlowColor;
    float _SmearGlowIntensity;
    float _SmearGlowPower;
    float _SmearEmission;
    half4 _SmearEmissionColor;
    float _SmearBlend;
    float _SmearBlendMode;
    float _SmearBlur;
    float4 _SmearMaskScrollSpeed;
    float _SmearMaskRotateSpeed;
    float _SmearAutoMagnitude;
    float _SmearMotionSensitivity;
    float _SmearVATVelocity;
    #endif

    // ===== 11. Fur (Shell-Based) =====
    #if defined(_FUR)
    float _FurLength;
    float _FurDensity;
    float _FurAlphaCutoff;
    float _FurGravity;
    half4 _FurRootColor;
    half4 _FurTipColor;
    float _FurColorBlend;
    float _FurAO;
    float _FurShadowStrength;
    float4 _FurWindDirection;
    float _FurWindSpeed;
    float _FurWindStrength;
    float _FurSpecular;
    float _FurRimLight;
    float _FurLODDistance;
    float _FurLODMinLayers;
    float4 _FurNoiseTex_ST;
    #endif

    // ===== 12. Background Mode (背景モード) =====
    #if defined(_BACKGROUND_MODE)
        float _LightmapToonInfluence;
        float _LightmapIntensity;
    #endif

    // ===== 13. PBR Mode (物理ベースレンダリング) =====
    #if defined(_PBR)
        float _PBR_Metallic;
        float _PBR_Smoothness;
        float _PBR_OcclusionStrength;
        float _PBR_ReflectionIntensity;
    #endif

    // ===== 14. Detail Map (ディテールマップ) =====
    #if defined(_DETAIL_MAP)
        float _DetailNormalScale;
        float _DetailAlbedoScale;
        float _DetailUVSet;
        float _DetailTiling;
    #endif

    // ===== 15. Triplanar Mapping (トライプレーナー) =====
    #if defined(_TRIPLANAR)
        float _TriplanarScale;
        float _TriplanarBlendSharpness;
        float _TriplanarOffsetX;
        float _TriplanarOffsetY;
        float _TriplanarOffsetZ;
    #endif

    // ===== 16. Height Fog (ハイトフォグ) =====
    #if defined(_HEIGHT_FOG)
        half4 _HeightFogColor;
        float _HeightFogStart;
        float _HeightFogEnd;
        float _HeightFogDensity;
        float _HeightFogMode;
    #endif

    // ===== 17. Surface Cover (雪/砂堆積) =====
    #if defined(_SURFACE_COVER)
        half4 _CoverColor;
        float _CoverAmount;
        float _CoverThreshold;
        float _CoverBlendSharpness;
        float _CoverTiling;
        float4 _CoverDirection;
    #endif

    // ===== 18. Mirror Control (ミラー対応) =====
    #if defined(_MIRROR_CONTROL)
        float _MirrorMode;
        float _MirrorEmissionMultiplier;
    #endif

    // ===== 19. Quest Lite (Quest軽量パス) =====
    // No CBUFFER properties needed - keyword only
CBUFFER_END

// VRChat Mirror Mode global variable (set by VRChat runtime)
#if defined(_MIRROR_CONTROL)
float _VRChatMirrorMode; // 0=Normal view, 1=Inside mirror
#endif

// Texture samplers (must be outside CBUFFER per HLSL specification)
// Main
sampler2D _MainTex;

// Makeup Textures
sampler2D _2ndTex;
sampler2D _2ndTexMask;
sampler2D _3rdTex;
sampler2D _3rdTexMask;
sampler2D _4thTex;
sampler2D _4thTexMask;
sampler2D _5thTex;
sampler2D _5thTexMask;

// Screen-Tone
#if defined(_SCREEN_TONE)
sampler2D _ScreenToneMask;
#endif

// Shading
sampler2D _RampTex;
sampler2D _ShadowReceiveMask;
sampler2D _ShadowColorTex;

// SDF & Grade Maps
sampler2D _SDFMap;
sampler2D _ShadingGradeMap;

// Ambient Occlusion
sampler2D _AOMap;

// Specular
#if defined(_SPECULAR)
sampler2D _SpecularMask;
#endif

// Hair Specular
#if defined(_HAIR_SPECULAR)
sampler2D _HairSpecMask;
sampler2D _HairSpecShiftTex;
#endif

// Rim Light
#if defined(_RIM_LIGHT)
sampler2D _RimMask;
#endif
#if defined(_RIM_LIGHT_2)
sampler2D _RimMask2;
#endif
#if defined(_OFFSET_RIM_LIGHT)
sampler2D _OffsetRimMask;
#endif

// MatCap
#if defined(_MATCAP)
sampler2D _MatCapTex;
#endif
#if defined(_MATCAP)
sampler2D _MatCapMask;
#endif
#if defined(_MATCAP_2)
sampler2D _MatCapTex2;
#endif
#if defined(_MATCAP_2)
sampler2D _MatCapMask2;
#endif
#if defined(_MATCAP_3)
sampler2D _MatCapTex3;
#endif
#if defined(_MATCAP_3)
sampler2D _MatCapMask3;
#endif

// Glitter
#if defined(_GLITTER)
sampler2D _GlitterMask;
#endif

// Outline
#if defined(_OUTLINE)
sampler2D _OutlineMask;
#endif

// Emission
#if defined(_EMISSION)
sampler2D _EmissionMap;
#endif
#if defined(_EMISSION)
sampler2D _EmissionMask;
#endif

// Normal Map
#if defined(_NORMALMAP)
sampler2D _BumpMap;
#endif

// Subsurface Scattering
#if defined(_SSS)
sampler2D _ThicknessMap;
sampler2D _SSSMask;
#endif

// Dissolve
#if defined(_DISSOLVE)
sampler2D _DissolveTex;
sampler2D _DissolveMask;
#endif

// Alpha Mask
#if defined(_ALPHA_MASK)
sampler2D _AlphaMask;
#endif

// Reflection
#if defined(_REFLECTION)
sampler2D _ReflectionMask;
#endif

// Iridescence
#if defined(_IRIDESCENCE)
sampler2D _IridescenceMask;
#endif

// Environmental Rim
#if defined(_ENV_RIM)
sampler2D _EnvRimMask;
#endif

// Parallax
#if defined(_PARALLAX)
sampler2D _ParallaxMap;
#endif

// Refraction
#if defined(_REFRACTION)
sampler2D _RefractionMask;
#endif

// Decal
#if defined(_DECAL)
sampler2D _DecalTex;
#endif

// Backface
#if defined(_BACKFACE_TEXTURE)
sampler2D _BackfaceTex;
#endif

// Video
#if defined(_VIDEO_TEXTURE)
sampler2D _VideoTex;
#endif

// Vertex Animation
#if defined(_VERTEX_ANIMATION)
sampler2D _VertexAnimMask;
#endif

// Water Drip
#if defined(_WATER_DRIP)
sampler2D _DripMask;
#endif

// Smear
#if defined(_SMEAR)
sampler2D _SmearMask;
#endif

// Fur
#if defined(_FUR)
sampler2D _FurNoiseTex;
sampler2D _FurMask;
#endif

// PBR
#if defined(_PBR)
sampler2D _PBR_MetallicGlossMap;
sampler2D _PBR_OcclusionMap;
#endif

// Smooth Normal Texture (for Mode 2: Baked Normal Texture)
#if defined(_SMOOTH_NORMAL)
sampler2D _SmoothNormalTex;
#endif

// Hologram (conditionally compiled)
#if defined(_HOLOGRAM)
sampler2D _HologramMask;
#endif
#if defined(_HOLOGRAM_NOISE)
sampler2D _HologramNoiseTex;
#endif

// VAT
#if defined(_VAT)
sampler2D _VATPositionMap;
sampler2D _VATNormalMap;
#endif

// Tessellation Displacement
#if defined(_TESS_DISPLACEMENT)
sampler2D _TessDispMap;
#endif

// Detail Map
#if defined(_DETAIL_MAP)
sampler2D _DetailAlbedoMap;
sampler2D _DetailNormalMap;
#endif

// Surface Cover
#if defined(_SURFACE_COVER)
sampler2D _CoverTex;
sampler2D _CoverNormalMap;
#endif

// Cubemap samplers (outside CBUFFER)
#if defined(_REFLECTION)
samplerCUBE _ReflectionCube;
#endif
#if defined(_ENV_RIM)
samplerCUBE _EnvRimCube;
#endif

// Intersection Fade
#if defined(_INTERSECTION_FADE)
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
#endif

// GrabPass texture for Refraction — declared in NataneToonUtils.hlsl (VR stereo-aware)

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
    #if defined(_BACKGROUND_MODE) || defined(_DETAIL_MAP)
        float2 uv1 : TEXCOORD1;  // Lightmap UV / Detail UV
    #endif
    #ifdef _SMOOTH_NORMAL
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
    #if defined(_REFRACTION) || defined(_PARALLAX) || defined(_DISSOLVE) || defined(_DITHERING_ALPHA) || defined(_INTERSECTION_FADE)
        float4 screenPos : TEXCOORD7; // For GrabPass (Refraction) / Dithering / Intersection Fade
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
    #ifdef _DETAIL_MAP
        float2 uv1 : TEXCOORD12;
    #endif
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif // NATANE_TOON_INPUT_INCLUDED
