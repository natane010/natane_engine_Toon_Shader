Shader "Natane/Toon Shader (Transparent)"
{
    Properties
    {
        [Header(Main Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [Header(Shading)]
        [Toggle(_USE_RAMP)] _UseRamp ("Use Ramp Texture", Float) = 0
        _RampTex ("Ramp Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _ShadowSteps ("Shadow Steps", Range(1, 10)) = 2
        _ShadowSharpness ("Shadow Sharpness", Range(0.001, 1)) = 0.1
        _ShadowOffset ("Shadow Offset", Range(-1, 1)) = 0

        [Header(Advanced Lighting)]
        _ShadowReceive ("Shadow Receive", Range(0, 1)) = 1
        _ShadowMaxDarkness ("Shadow Max Darkness", Range(0, 1)) = 0
        _LightMinInfluence ("Light Min Influence", Range(0, 1)) = 0
        _LightMaxInfluence ("Light Max Influence", Range(1, 5)) = 2
        _BacklightIntensity ("Backlight Intensity", Range(0, 2)) = 0
        _BacklightColor ("Backlight Color", Color) = (1, 1, 1, 1)

        [Header(Specular)]
        [Toggle(_SPECULAR)] _Specular ("Enable Specular", Float) = 0
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularSize ("Specular Size", Range(0, 1)) = 0.1
        _SpecularSoftness ("Specular Softness", Range(0.001, 1)) = 0.05

        [Header(Rim Light)]
        [Toggle(_RIM_LIGHT)] _RimLight ("Enable Rim Light", Float) = 0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1, 10)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1

        [Header(Subsurface Scattering)]
        [Toggle(_SSS)] _SSS ("Enable SSS", Float) = 0
        _SSSColor ("SSS Color", Color) = (1, 0.5, 0.5, 1)
        _SSSIntensity ("SSS Intensity", Range(0, 2)) = 1
        _SSSPower ("SSS Power", Range(0.1, 10)) = 3
        _SSSDistortion ("SSS Distortion", Range(0, 1)) = 0.5
        [Toggle(_THICKNESS_MAP)] _UseThicknessMap ("Use Thickness Map", Float) = 0
        _ThicknessMap ("Thickness Map", 2D) = "white" {}
        _ThicknessScale ("Thickness Scale", Range(0, 1)) = 0.5

        [Header(MatCap)]
        [Toggle(_MATCAP)] _MatCap ("Enable MatCap", Float) = 0
        _MatCapTex ("MatCap Texture", 2D) = "black" {}
        _MatCapIntensity ("MatCap Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Replace,2)] _MatCapBlendMode ("MatCap Blend Mode", Float) = 0

        [Header(Outline)]
        [Toggle(_OUTLINE)] _Outline ("Enable Outline", Float) = 0
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.01
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)

        [Header(Emission)]
        [Toggle(_EMISSION)] _Emission ("Enable Emission", Float) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission Map", 2D) = "white" {}

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(Off,0,On,1)] _ZWrite ("Z Write", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        // Outline Pass
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "ForwardBase" }
            Cull Front
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _OUTLINE
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };

            float _OutlineWidth;
            float4 _OutlineColor;
            float _Outline;

            v2f vert(appdata v)
            {
                v2f o;

                #ifdef _OUTLINE
                    float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                    float2 offset = TransformViewToProjection(norm.xy);

                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.pos.xy += offset * o.pos.z * _OutlineWidth;
                #else
                    o.pos = float4(0, 0, 0, 0);
                #endif

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                #ifdef _OUTLINE
                    fixed4 col = _OutlineColor;
                    UNITY_APPLY_FOG(i.fogCoord, col);
                    return col;
                #else
                    discard;
                    return fixed4(0, 0, 0, 0);
                #endif
            }
            ENDCG
        }

        // Base Forward Pass
        Pass
        {
            Name "FORWARD_BASE"
            Tags { "LightMode" = "ForwardBase" }
            Cull [_Cull]
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature _USE_RAMP
            #pragma shader_feature _SPECULAR
            #pragma shader_feature _RIM_LIGHT
            #pragma shader_feature _SSS
            #pragma shader_feature _THICKNESS_MAP
            #pragma shader_feature _MATCAP
            #pragma shader_feature _EMISSION
            #pragma shader_feature _NORMALMAP
            #define TRANSPARENT_VARIANT

            #include "Include/NataneToonCore.hlsl"

            ENDCG
        }

        // Additional Forward Pass
        Pass
        {
            Name "FORWARD_ADD"
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            ZWrite Off
            Cull [_Cull]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature _USE_RAMP
            #pragma shader_feature _SPECULAR
            #pragma shader_feature _SSS
            #pragma shader_feature _THICKNESS_MAP
            #pragma shader_feature _NORMALMAP
            #define TRANSPARENT_VARIANT

            #include "Include/NataneToonCore.hlsl"

            ENDCG
        }
    }

    CustomEditor "NataneToonShaderGUI"
    FallBack "Transparent/Diffuse"
}
