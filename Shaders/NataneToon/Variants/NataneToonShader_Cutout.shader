Shader "Natane/Toon Shader (Cutout)"
{
    Properties
    {
        [Header(Main Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Shading)]
        [Toggle(_USE_RAMP)] _UseRamp ("Use Ramp Texture", Float) = 0
        _RampTex ("Ramp Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _ShadowSteps ("Shadow Steps", Range(1, 10)) = 2
        _ShadowSharpness ("Shadow Sharpness", Range(0.001, 1)) = 0.1
        _ShadowOffset ("Shadow Offset", Range(-1, 1)) = 0

        [Header(Advanced Lighting)]
        _LightIntensity ("Light Intensity (Global)", Range(0, 2)) = 1
        _IndirectLightIntensity ("Indirect Light Intensity", Range(0, 2)) = 1
        _ShadowReceive ("Shadow Receive", Range(0, 1)) = 1
        _ShadowMaxDarkness ("Shadow Max Darkness", Range(0, 1)) = 0
        _LightMinInfluence ("Light Min Influence", Range(0, 1)) = 0
        _LightMaxInfluence ("Light Max Influence", Range(1, 5)) = 2
        _BacklightIntensity ("Backlight Intensity", Range(0, 2)) = 0
        _BacklightColor ("Backlight Color", Color) = (1, 1, 1, 1)
        _AdditionalLightIntensity ("Additional Light Intensity", Range(0, 1)) = 0.5

        [Header(VRC Light Volumes)]
        [Toggle(_USE_LIGHT_VOLUME)] _UseLightVolume ("Use Light Volume", Float) = 1
        _LightVolumeIntensity ("Light Volume Intensity", Range(0, 1)) = 1
        [Toggle(_LIGHT_VOLUME_SPECULAR)] _LightVolumeSpecular ("Light Volume Specular", Float) = 0

        [Header(Specular)]
        [Toggle(_SPECULAR)] _Specular ("Enable Specular", Float) = 0
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularSize ("Specular Size", Range(0, 1)) = 0.1
        _SpecularSoftness ("Specular Softness", Range(0.001, 1)) = 0.05
        [Toggle(_SPECULAR_MASK)] _UseSpecularMask ("Use Specular Mask", Float) = 0
        _SpecularMask ("Specular Mask", 2D) = "white" {}

        [Header(Rim Light)]
        [Toggle(_RIM_LIGHT)] _RimLight ("Enable Rim Light", Float) = 0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1, 10)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1
        [Toggle(_RIM_MASK)] _UseRimMask ("Use Rim Mask", Float) = 0
        _RimMask ("Rim Mask", 2D) = "white" {}

        [Header(Subsurface Scattering)]
        [Toggle(_SSS)] _SSS ("Enable SSS", Float) = 0
        _SSSColor ("SSS Color", Color) = (1, 0.5, 0.5, 1)
        _SSSIntensity ("SSS Intensity", Range(0, 2)) = 1
        _SSSPower ("SSS Power", Range(0.1, 10)) = 3
        _SSSDistortion ("SSS Distortion", Range(0, 1)) = 0.5
        [Toggle(_THICKNESS_MAP)] _UseThicknessMap ("Use Thickness Map", Float) = 0
        _ThicknessMap ("Thickness Map", 2D) = "white" {}
        _ThicknessScale ("Thickness Scale", Range(0, 1)) = 0.5
        [Toggle(_SSS_MASK)] _UseSSS_Mask ("Use SSS Mask", Float) = 0
        _SSSMask ("SSS Mask", 2D) = "white" {}

        [Header(MatCap)]
        [Toggle(_MATCAP)] _MatCap ("Enable MatCap", Float) = 0
        _MatCapTex ("MatCap Texture", 2D) = "black" {}
        _MatCapIntensity ("MatCap Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Replace,2)] _MatCapBlendMode ("MatCap Blend Mode", Float) = 0
        [Toggle(_MATCAP_MASK)] _UseMatCapMask ("Use MatCap Mask", Float) = 0
        _MatCapMask ("MatCap Mask", 2D) = "white" {}

        [Header(Outline)]
        [Toggle(_OUTLINE)] _Outline ("Enable Outline", Float) = 0
        [Enum(Inverted Hull,0,Back Face,1)] _OutlineMode ("Outline Mode", Float) = 0
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.01
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)

        [Header(Emission)]
        [Toggle(_EMISSION)] _Emission ("Enable Emission", Float) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        [Toggle(_EMISSION_SCROLL)] _EmissionScroll ("Emission Scroll", Float) = 0
        _EmissionScrollSpeed ("Emission Scroll Speed", Float) = 1
        [Toggle(_EMISSION_PULSE)] _EmissionPulse ("Emission Pulse", Float) = 0
        _EmissionPulseSpeed ("Emission Pulse Speed", Float) = 1
        _EmissionPulseAmplitude ("Emission Pulse Amplitude", Range(0, 1)) = 0.5
        [Toggle(_EMISSION_MASK)] _UseEmissionMask ("Use Emission Mask", Float) = 0
        _EmissionMask ("Emission Mask", 2D) = "white" {}

        [Header(Virtual Expression)]
        [Toggle(_DISSOLVE)] _Dissolve ("Enable Dissolve", Float) = 0
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveTex ("Dissolve Texture (Noise)", 2D) = "white" {}
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0, 0.5)) = 0.1
        [HDR] _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1, 0.5, 0, 1)
        _DissolveEdgeIntensity ("Dissolve Edge Intensity", Range(0, 10)) = 2
        [Toggle(_DISSOLVE_MASK)] _UseDissolveMask ("Use Dissolve Mask", Float) = 0
        _DissolveMask ("Dissolve Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_HUE_SHIFT)] _HueShiftEnable ("Enable Hue Shift", Float) = 0
        _HueShift ("Hue Shift", Range(0, 1)) = 0

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1

        [Header(Cubemap Reflection)]
        [Toggle(_REFLECTION)] _Reflection ("Enable Reflection", Float) = 0
        _ReflectionCube ("Reflection Cubemap", CUBE) = "black" {}
        _ReflectionColor ("Reflection Color", Color) = (1, 1, 1, 1)
        _ReflectionIntensity ("Reflection Intensity", Range(0, 2)) = 1
        _Smoothness ("Smoothness (Glossiness)", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        _FresnelPower ("Fresnel Power", Range(0, 10)) = 5
        [Toggle(_REFLECTION_MASK)] _UseReflectionMask ("Use Reflection Mask", Float) = 0
        _ReflectionMask ("Reflection Mask", 2D) = "white" {}

        [Header(Environmental Rim)]
        [Toggle(_ENV_RIM)] _EnvRim ("Enable Environmental Rim", Float) = 0
        _EnvRimCube ("Environment Cubemap", CUBE) = "black" {}
        _EnvRimColor ("Env Rim Color", Color) = (1, 1, 1, 1)
        _EnvRimPower ("Env Rim Power", Range(0.1, 10)) = 3
        _EnvRimIntensity ("Env Rim Intensity", Range(0, 5)) = 1
        [Toggle(_ENV_RIM_MASK)] _UseEnvRimMask ("Use Env Rim Mask", Float) = 0
        _EnvRimMask ("Env Rim Mask", 2D) = "white" {}

        [Header(Parallax Mapping)]
        [Toggle(_PARALLAX)] _Parallax ("Enable Parallax", Float) = 0
        _ParallaxMap ("Height Map", 2D) = "grey" {}
        _ParallaxScale ("Parallax Scale", Range(0, 0.1)) = 0.02
        _ParallaxMinSamples ("Min Samples", Range(4, 16)) = 4
        _ParallaxMaxSamples ("Max Samples", Range(16, 64)) = 32

        [Header(Refraction)]
        [Toggle(_REFRACTION)] _Refraction ("Enable Refraction", Float) = 0
        _RefractionIndex ("Refraction Index (IOR)", Range(1, 3)) = 1.5
        _RefractionIntensity ("Refraction Intensity", Range(0, 1)) = 1
        _RefractionBlur ("Refraction Blur", Range(0, 1)) = 0
        [Toggle(_REFRACTION_MASK)] _UseRefractionMask ("Use Refraction Mask", Float) = 0
        _RefractionMask ("Refraction Mask", 2D) = "white" {}

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
        [Enum(Off,0,On,1)] _ZWrite ("Z Write", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
        }

        // Outline Pass
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "ForwardBase" }
            Cull Front
            ZWrite [_ZWrite]

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
            float _OutlineMode;

            v2f vert(appdata v)
            {
                v2f o;

                #ifdef _OUTLINE
                    // Calculate distance compensation for consistent outline width (NiloToon-style)
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    float distanceToCamera = distance(worldPos, _WorldSpaceCameraPos);
                    float distanceFactor = distanceToCamera * 0.1; // Scale factor for distance compensation

                    if (_OutlineMode < 0.5)
                    {
                        // Mode 0: Inverted Hull - Extrusion along normals in view space
                        // Improved for better consistency at different angles
                        float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                        float2 offset = TransformViewToProjection(norm.xy);

                        o.pos = UnityObjectToClipPos(v.vertex);

                        // Apply distance compensation for consistent outline width
                        float outlineWidth = _OutlineWidth * (1.0 + distanceFactor);
                        o.pos.xy += offset * o.pos.z * outlineWidth;
                    }
                    else
                    {
                        // Mode 1: Back Face - Scale up vertices along normals in object space
                        // Improved with distance compensation
                        float outlineWidth = _OutlineWidth * 10.0 * (1.0 + distanceFactor * 0.5);
                        float3 scaledPos = v.vertex.xyz + normalize(v.normal) * outlineWidth;
                        o.pos = UnityObjectToClipPos(float4(scaledPos, 1.0));
                    }
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

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature _USE_RAMP
            #pragma shader_feature _USE_LIGHT_VOLUME
            #pragma shader_feature _LIGHT_VOLUME_SPECULAR
            #pragma shader_feature _SPECULAR
            #pragma shader_feature _SPECULAR_MASK
            #pragma shader_feature _RIM_LIGHT
            #pragma shader_feature _RIM_MASK
            #pragma shader_feature _SSS
            #pragma shader_feature _SSS_MASK
            #pragma shader_feature _THICKNESS_MAP
            #pragma shader_feature _MATCAP
            #pragma shader_feature _MATCAP_MASK
            #pragma shader_feature _EMISSION
            #pragma shader_feature _EMISSION_MASK
            #pragma shader_feature _EMISSION_SCROLL
            #pragma shader_feature _EMISSION_PULSE
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _DISSOLVE
            #pragma shader_feature _DISSOLVE_MASK
            #pragma shader_feature _HUE_SHIFT
            #pragma shader_feature _REFLECTION
            #pragma shader_feature _REFLECTION_MASK
            #pragma shader_feature _ENV_RIM
            #pragma shader_feature _ENV_RIM_MASK
            #pragma shader_feature _PARALLAX
            #pragma shader_feature _REFRACTION
            #pragma shader_feature _REFRACTION_MASK
            #define CUTOUT_VARIANT

            float _Cutoff;

            #include "../Include/NataneToonCore.hlsl"

            half4 frag_cutout(v2f i) : SV_Target
            {
                half4 col = frag(i);
                clip(col.a - _Cutoff);
                return col;
            }

            #define frag frag_cutout

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
            #pragma shader_feature _SPECULAR_MASK
            #pragma shader_feature _SSS
            #pragma shader_feature _SSS_MASK
            #pragma shader_feature _THICKNESS_MAP
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _DISSOLVE
            #pragma shader_feature _DISSOLVE_MASK
            #pragma shader_feature _HUE_SHIFT
            #pragma shader_feature _PARALLAX
            #pragma shader_feature _REFRACTION
            #pragma shader_feature _REFRACTION_MASK
            #define CUTOUT_VARIANT

            float _Cutoff;

            #include "../Include/NataneToonCore.hlsl"

            half4 frag_cutout(v2f i) : SV_Target
            {
                half4 col = frag(i);
                clip(col.a - _Cutoff);
                return col;
            }

            #define frag frag_cutout

            ENDCG
        }

        // Shadow Caster Pass
        Pass
        {
            Name "SHADOW_CASTER"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                fixed4 texcol = tex2D(_MainTex, i.uv);
                clip(texcol.a - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    CustomEditor "NataneToonShaderGUI"
    FallBack "Transparent/Cutout/Diffuse"
}
