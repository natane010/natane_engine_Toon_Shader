Shader "Hidden/Reduction/BakeNormal"
{
    // Replacement shader for capturing world-space normals via Camera.RenderWithShader().
    // Outputs world normal encoded as RGB: n * 0.5 + 0.5.

    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff  ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    // ---------------------------------------------------------------
    // SubShader 0 : Opaque geometry
    // ---------------------------------------------------------------
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float3 worldNorm  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos       = UnityObjectToClipPos(v.vertex);
                o.worldNorm = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNorm);
                return float4(n * 0.5 + 0.5, 1.0);
            }
            ENDCG
        }
    }

    // ---------------------------------------------------------------
    // SubShader 1 : TransparentCutout geometry (alpha-tested)
    // ---------------------------------------------------------------
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float     _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldNorm  : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos       = UnityObjectToClipPos(v.vertex);
                o.uv        = v.uv;
                o.worldNorm = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float alpha = tex2D(_MainTex, i.uv).a;
                clip(alpha - _Cutoff);
                float3 n = normalize(i.worldNorm);
                return float4(n * 0.5 + 0.5, 1.0);
            }
            ENDCG
        }
    }

    // ---------------------------------------------------------------
    // SubShader 2 : Transparent geometry - skip (no meaningful normal)
    // ---------------------------------------------------------------
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            ColorMask 0
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            float4 vert(appdata v) : SV_POSITION
            {
                return UnityObjectToClipPos(v.vertex);
            }

            float4 frag() : SV_Target
            {
                discard;
                return 0;
            }
            ENDCG
        }
    }

    Fallback Off
}
