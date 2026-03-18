Shader "AutoMat/PBRLit_Builtin"
{
    Properties
    {
        _BaseMap ("Base Map (Albedo)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        _MetallicMap ("Metallic Map", 2D) = "black" {}
        [Toggle(_METALLIC_MAP)] _UseMetallicMap ("Use Metallic Map", Float) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        _RoughnessMap ("Roughness Map", 2D) = "white" {}
        [Toggle(_USE_ROUGHNESS_MAP)] _UseRoughnessMap ("Use Roughness Map", Float) = 0

        _SmoothnessMap ("Smoothness Map", 2D) = "white" {}
        [Toggle(_SMOOTHNESS_MAP)] _UseSmoothnessMap ("Use Smoothness Map", Float) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        _BumpMap ("Normal Map", 2D) = "bump" {}
        [Toggle(_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 0
        _BumpScale ("Normal Strength", Range(0, 2)) = 1.0

        _HeightMap ("Height Map", 2D) = "black" {}
        [Toggle(_HEIGHTMAP)] _UseHeightMap ("Use Height Map", Float) = 0
        _HeightScale ("Height Scale", Range(0, 0.1)) = 0.02
        _HeightSteps ("Height Steps", Range(4, 32)) = 16

        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        [Toggle(_OCCLUSION_MAP)] _UseOcclusionMap ("Use Occlusion Map", Float) = 0
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1.0

        _EmissionMap ("Emission Map", 2D) = "black" {}
        [Toggle(_EMISSION)] _UseEmission ("Use Emission", Float) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)

        // Debug
        [Toggle(_DEBUG_ALBEDO)] _DebugAlbedo ("Debug: Albedo", Float) = 0
        [Toggle(_DEBUG_NORMAL)] _DebugNormal ("Debug: Normal", Float) = 0
        [Toggle(_DEBUG_ROUGHNESS)] _DebugRoughness ("Debug: Roughness", Float) = 0
        [Toggle(_DEBUG_METALLIC)] _DebugMetallic ("Debug: Metallic", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        // ===== ForwardBase =====
        Pass
        {
            Name "FORWARD_BASE"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma target 3.5
            #pragma vertex AutoMatVert
            #pragma fragment AutoMatFrag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #pragma shader_feature_local _METALLIC_MAP
            #pragma shader_feature_local _USE_ROUGHNESS_MAP
            #pragma shader_feature_local _SMOOTHNESS_MAP
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _HEIGHTMAP
            #pragma shader_feature_local _OCCLUSION_MAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _DEBUG_ALBEDO
            #pragma shader_feature_local _DEBUG_NORMAL
            #pragma shader_feature_local _DEBUG_ROUGHNESS
            #pragma shader_feature_local _DEBUG_METALLIC

            #include "Include/AutoMat_Common.hlsl"
            #include "Include/AutoMat_SurfaceData.hlsl"
            #include "Include/AutoMat_PBRLighting.hlsl"
            ENDCG
        }

        // ===== ForwardAdd =====
        Pass
        {
            Name "FORWARD_ADD"
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma target 3.5
            #pragma vertex AutoMatVert
            #pragma fragment AutoMatFragAdd
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog

            #pragma shader_feature_local _METALLIC_MAP
            #pragma shader_feature_local _USE_ROUGHNESS_MAP
            #pragma shader_feature_local _SMOOTHNESS_MAP
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _HEIGHTMAP
            #pragma shader_feature_local _OCCLUSION_MAP

            #include "Include/AutoMat_Common.hlsl"
            #include "Include/AutoMat_SurfaceData.hlsl"
            #include "Include/AutoMat_PBRLighting.hlsl"
            ENDCG
        }

        // ===== ShadowCaster =====
        Pass
        {
            Name "SHADOW_CASTER"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"

            struct v2f_shadow
            {
                V2F_SHADOW_CASTER;
            };

            v2f_shadow vertShadow(appdata_base v)
            {
                v2f_shadow o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
                return o;
            }

            half4 fragShadow(v2f_shadow i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDCG
        }
    }

    CustomEditor "NataneToon.Editor.AutoMatShaderGUI"
    FallBack "Standard"
}
