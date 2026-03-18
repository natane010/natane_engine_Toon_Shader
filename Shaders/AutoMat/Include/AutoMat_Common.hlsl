// AutoMat_Common.hlsl
// CBUFFER and texture declarations for AutoMat PBR Lit (Built-in RP)

#ifndef AUTOMAT_COMMON_INCLUDED
#define AUTOMAT_COMMON_INCLUDED

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"

// ===== Textures =====
sampler2D _BaseMap;
sampler2D _MetallicMap;
sampler2D _RoughnessMap;
sampler2D _SmoothnessMap;
sampler2D _BumpMap;
sampler2D _HeightMap;
sampler2D _OcclusionMap;
sampler2D _EmissionMap;

// ===== CBUFFER =====
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;

    half _Metallic;
    half _Smoothness;

    half _BumpScale;

    float _HeightScale;
    float _HeightSteps;

    half _OcclusionStrength;

    half4 _EmissionColor;
CBUFFER_END

// ===== Structs =====
struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    float3 worldNormal : TEXCOORD2;
    float3 worldTangent : TEXCOORD3;
    float3 worldBitangent : TEXCOORD4;
    float3 viewDir : TEXCOORD5;
    SHADOW_COORDS(6)
    UNITY_FOG_COORDS(7)
};

#endif // AUTOMAT_COMMON_INCLUDED
