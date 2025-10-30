#ifndef NATANE_TOON_INPUT_INCLUDED
#define NATANE_TOON_INPUT_INCLUDED

// Properties and Structures
CBUFFER_START(UnityPerMaterial)
    // Main Texture
    sampler2D _MainTex;
    float4 _MainTex_ST;
    half4 _Color;

    // Shading
    sampler2D _RampTex;
    half4 _ShadowColor;
    float _ShadowSteps;
    float _ShadowSharpness;
    float _ShadowOffset;

    // Advanced Lighting Controls
    float _ShadowReceive;
    float _ShadowMaxDarkness;
    float _LightMinInfluence;
    float _LightMaxInfluence;
    float _BacklightIntensity;
    half4 _BacklightColor;
    float _AdditionalLightIntensity;

    // Specular
    half4 _SpecularColor;
    float _SpecularSize;
    float _SpecularSoftness;
    sampler2D _SpecularMask;

    // Rim Light
    half4 _RimColor;
    float _RimPower;
    float _RimIntensity;
    sampler2D _RimMask;

    // MatCap
    sampler2D _MatCapTex;
    float _MatCapIntensity;
    float _MatCapBlendMode;
    sampler2D _MatCapMask;

    // Emission
    half4 _EmissionColor;
    sampler2D _EmissionMap;
    sampler2D _EmissionMask;

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
    sampler2D _ReflectionMask;

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
CBUFFER_END

// Cubemap samplers (outside CBUFFER)
samplerCUBE _ReflectionCube;
samplerCUBE _EnvRimCube;

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
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif // NATANE_TOON_INPUT_INCLUDED
