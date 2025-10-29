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

    // Rim Light
    half4 _RimColor;
    float _RimPower;
    float _RimIntensity;

    // MatCap
    sampler2D _MatCapTex;
    float _MatCapIntensity;
    float _MatCapBlendMode;

    // Emission
    half4 _EmissionColor;
    sampler2D _EmissionMap;

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
CBUFFER_END

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
