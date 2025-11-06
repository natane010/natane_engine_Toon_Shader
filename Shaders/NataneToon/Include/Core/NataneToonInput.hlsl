#ifndef NATANE_TOON_INPUT_INCLUDED
#define NATANE_TOON_INPUT_INCLUDED

// Properties and Structures
CBUFFER_START(UnityPerMaterial)
    // Main Texture
    sampler2D _MainTex;
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
    sampler2D _2ndTex;
    float4 _2ndTex_ST;
    float _2ndTexHueShift;
    float _2ndTexSaturation;
    float _2ndTexValue;
    float _2ndTexIntensity;
    float _2ndTexBlendMode;
    sampler2D _2ndTexMask;

    sampler2D _3rdTex;
    float4 _3rdTex_ST;
    float _3rdTexHueShift;
    float _3rdTexSaturation;
    float _3rdTexValue;
    float _3rdTexIntensity;
    float _3rdTexBlendMode;
    sampler2D _3rdTexMask;

    sampler2D _4thTex;
    float4 _4thTex_ST;
    float _4thTexHueShift;
    float _4thTexSaturation;
    float _4thTexValue;
    float _4thTexIntensity;
    float _4thTexBlendMode;
    sampler2D _4thTexMask;

    sampler2D _5thTex;
    float4 _5thTex_ST;
    float _5thTexHueShift;
    float _5thTexSaturation;
    float _5thTexValue;
    float _5thTexIntensity;
    float _5thTexBlendMode;
    sampler2D _5thTexMask;

    // Shading
    float _ShadingMode;
    float _ShadingGradientWidth;
    sampler2D _RampTex;
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
    sampler2D _ShadowReceiveMask;

    // SDF Shadow Map
    sampler2D _SDFMap;
    float _SDFIntensity;
    float _SDFSoftness;
    float _SDFOffset;

    // Shading Grade Map
    sampler2D _ShadingGradeMap;
    float _ShadingGradeScale;

    // Ambient Occlusion
    sampler2D _AOMap;
    float _AOIntensity;

    // Dithering
    float _DitheringScale;
    float _DitheringStrength;

    // Advanced Lighting Controls
    float _SoftLightingIntensity;
    float _LightIntensity;
    float _IndirectLightIntensity;
    float _LightColorInfluence;
    float _ShadowReceive;
    float _ShadowMaxDarkness;
    float _LightMinInfluence;
    float _LightMaxInfluence;
    float _LightBlend;
    float _HighlightSoftness;
    float _BacklightIntensity;
    half4 _BacklightColor;
    float _AdditionalLightIntensity;

    // VRC Light Volumes
    float _LightVolumeIntensity;
    float _LightVolumeBlendMode;

    // Specular
    half4 _SpecularColor;
    float _SpecularSize;
    float _SpecularSoftness;
    sampler2D _SpecularMask;

    // Rim Light
    half4 _RimColor;
    float _RimPower;
    float _RimIntensity;
    float _RimSpread;
    sampler2D _RimMask;

    // Rim Light 2
    half4 _RimColor2;
    float _RimPower2;
    float _RimIntensity2;
    float _RimSpread2;
    sampler2D _RimMask2;

    // MatCap
    sampler2D _MatCapTex;
    float _MatCapIntensity;
    float _MatCapBlendMode;
    sampler2D _MatCapMask;

    // Glitter
    half4 _GlitterColor;
    float _GlitterSize;
    float _GlitterDensity;
    float _GlitterSpeed;
    float _GlitterIntensity;
    sampler2D _GlitterMask;

    // Outline
    half4 _OutlineColor;
    float _OutlineWidth;
    sampler2D _OutlineMask;

    // Emission
    half4 _EmissionColor;
    sampler2D _EmissionMap;
    sampler2D _EmissionMask;
    float _EmissionGlow;

    // Normal Map
    sampler2D _BumpMap;
    float _BumpScale;

    // Subsurface Scattering
    half4 _SSSColor;
    float _SSSIntensity;
    float _SSSPower;
    float _SSSDistortion;
    sampler2D _ThicknessMap;
    float _ThicknessScale;
    sampler2D _SSSMask;

    // Virtual Expression - Dissolve
    float _DissolveAmount;
    sampler2D _DissolveTex;
    float _DissolveEdgeWidth;
    half4 _DissolveEdgeColor;
    float _DissolveEdgeIntensity;
    sampler2D _DissolveMask;

    // Virtual Expression - Alpha Mask
    sampler2D _AlphaMask;

    // Virtual Expression - Hue Shift
    float _HueShift;

    // Virtual Expression - Emission Animation
    float _EmissionScrollSpeed;
    float _EmissionPulseSpeed;
    float _EmissionPulseAmplitude;

    // Cubemap Reflection (Environment Mapping)
    half4 _ReflectionColor;
    float _ReflectionIntensity;
    float _Smoothness;
    float _Metallic;
    float _FresnelPower;
    float _FresnelSoftness;
    float _ReflectionBlendMode;
    sampler2D _ReflectionMask;

    // Iridescence
    half4 _IridescenceColor;
    float _IridescenceIntensity;
    float _IridescenceHueShift;
    float _IridescenceSize;
    sampler2D _IridescenceMask;

    // Environmental Rim
    half4 _EnvRimColor;
    float _EnvRimPower;
    float _EnvRimIntensity;
    sampler2D _EnvRimMask;

    // Parallax Mapping
    float _ParallaxScale;
    float _ParallaxMinSamples;
    float _ParallaxMaxSamples;
    sampler2D _ParallaxMap;

    // Refraction
    float _RefractionIndex;
    float _RefractionIntensity;
    float _RefractionBlur;
    sampler2D _RefractionMask;

    // MatCap 2 & 3
    sampler2D _MatCapTex2;
    float _MatCapIntensity2;
    float _MatCapBlendMode2;
    sampler2D _MatCapMask2;
    sampler2D _MatCapTex3;
    float _MatCapIntensity3;
    float _MatCapBlendMode3;
    sampler2D _MatCapMask3;

    // Rim Direction Control
    float4 _RimLightDirection;
    float _RimDirectionRange;

    // Shadow Color Texture
    sampler2D _ShadowColorTex;
    float _ShadowColorTexStrength;

    // Outline Multi-Color
    half4 _OutlineColor2;
    float _OutlineColorMix;

    // AudioLink
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

    // Distance Fade
    float _DistanceFadeStart;
    float _DistanceFadeEnd;
    float _DistanceFadeMode;

    // Vertex Animation
    float _VertexAnimSpeed;
    float _VertexAnimStrength;
    float _VertexAnimFrequency;
    float _VertexAnimType;
    sampler2D _VertexAnimMask;

    // Hologram & Glitch
    float _HologramScanlineSpeed;
    float _HologramScanlineIntensity;
    float _HologramFlickerSpeed;
    float _HologramFlickerAmount;
    float _GlitchIntensity;
    float _GlitchSpeed;
    float _GlitchBlockSize;

    // Decal
    sampler2D _DecalTex;
    half4 _DecalColor;
    float4 _DecalPosition;
    float _DecalRotation;
    float _DecalScale;
    float _DecalBlendMode;

    // Backface Texture
    sampler2D _BackfaceTex;
    half4 _BackfaceColor;

    // Video Texture
    sampler2D _VideoTex;
    float _VideoEmission;

    // LTCGI
    float _LTCGIIntensity;
    float _LTCGISpecular;

    // Dithering Alpha
    float _DitheringAlphaScale;
CBUFFER_END

// Cubemap samplers (outside CBUFFER)
samplerCUBE _ReflectionCube;
samplerCUBE _EnvRimCube;

// GrabPass texture for Refraction
sampler2D _GrabTexture;
float4 _GrabTexture_TexelSize;

// AudioLink texture (VRChat)
sampler2D _AudioTexture;
sampler2D _AudioTexture2D;

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
    float4 screenPos : TEXCOORD7; // For GrabPass (Refraction)
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif // NATANE_TOON_INPUT_INCLUDED
