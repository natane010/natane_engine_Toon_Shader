#ifndef NATANE_TOON_INPUT_INCLUDED
#define NATANE_TOON_INPUT_INCLUDED

// Properties and Structures
CBUFFER_START(UnityPerMaterial)
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
    float _ShadowOffset;
    float _LitSoftness;
    float _ShadowBlend;

    // SDF Shadow Map
    float _SDFIntensity;
    float _SDFSoftness;
    float _SDFOffset;

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

    // MatCap
    #if defined(_MATCAP)
    float _MatCapIntensity;
    float _MatCapBlendMode;
    float _MatCapBlend;
    float _MatCapBlur;
    #endif

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

    // Normal Map
    float _BumpScale;
    // Normal Map UV Animation
    float4 _BumpMapScrollSpeed;
    float _BumpMapRotateSpeed;

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

    // Distance Fade
    #if defined(_DISTANCE_FADE)
    float _DistanceFadeStart;
    float _DistanceFadeEnd;
    float _DistanceFadeMode;
    float _DistanceFadeBlend;
    float _DistFadeBlur;
    #endif

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
CBUFFER_END

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

// Rim Light
#if defined(_RIM_LIGHT)
sampler2D _RimMask;
#endif
#if defined(_RIM_LIGHT_2)
sampler2D _RimMask2;
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

// Cubemap samplers (outside CBUFFER)
#if defined(_REFLECTION)
samplerCUBE _ReflectionCube;
#endif
#if defined(_ENV_RIM)
samplerCUBE _EnvRimCube;
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
    #if defined(_REFRACTION) || defined(_PARALLAX) || defined(_DISSOLVE) || defined(_DITHERING_ALPHA)
        float4 screenPos : TEXCOORD7; // For GrabPass (Refraction) / Dithering
    #endif
    #if defined(VERTEXLIGHT_ON) && !defined(_PIXEL_VERTEX_LIGHTS)
        float3 vertexLightColor : TEXCOORD8;
    #endif
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif // NATANE_TOON_INPUT_INCLUDED
