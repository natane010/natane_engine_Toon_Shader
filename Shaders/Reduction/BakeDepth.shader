Shader "Hidden/Reduction/BakeDepth"
{
    // Replacement shader for capturing linear depth via Camera.RenderWithShader().
    // Outputs linear depth normalised to [0,1] range: LinearEyeDepth / FarPlane.

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
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float  depth : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.depth = -UnityObjectToViewPos(v.vertex).z;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                return i.depth / _ProjectionParams.z;
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
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float  depth : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.depth = -UnityObjectToViewPos(v.vertex).z;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                float alpha = tex2D(_MainTex, i.uv).a;
                clip(alpha - _Cutoff);
                return i.depth / _ProjectionParams.z;
            }
            ENDCG
        }
    }

    // ---------------------------------------------------------------
    // SubShader 2 : Transparent geometry - skip (no meaningful depth)
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

            float frag() : SV_Target
            {
                discard;
                return 0;
            }
            ENDCG
        }
    }

    Fallback Off
}
