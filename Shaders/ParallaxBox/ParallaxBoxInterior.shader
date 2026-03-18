Shader "Custom/ParallaxBoxInterior"
{
    // Runtime parallax shader for box-interior background.
    // Renders on the inside faces of a box using captured colour + depth textures
    // with parallax occlusion mapping to recreate a sense of 3D depth.

    Properties
    {
        _ColorTex        ("Color Texture",   2D)            = "white" {}
        _DepthTex        ("Depth Texture",   2D)            = "black" {}

        [Header(Box)]
        _BoxCenter       ("Box Center (world)", Vector)     = (0,0,0,0)
        _BoxHalfSize     ("Box Half Size",      Vector)     = (5,5,5,0)

        [Header(Capture)]
        _CaptureFarPlane ("Capture Far Plane", Float)       = 100.0

        [Header(Parallax)]
        _DepthScale      ("Depth Scale",       Range(0,2))  = 0.5
        _ParallaxSteps   ("Parallax Steps",    Range(1,32)) = 8

        [Header(Attenuation)]
        _MinParallaxDist ("Min Parallax Dist", Float)       = 0.5
        _MaxParallaxDist ("Max Parallax Dist", Float)       = 3.0
        _GrazingAngleFade("Grazing Angle Fade", Range(0.01,1)) = 0.15

        [Header(Border)]
        _UVBorderClamp   ("UV Border Clamp",   Range(0,0.1)) = 0.01
    }

    SubShader
    {
        Tags { "Queue"="Geometry-1" "RenderType"="Opaque" }
        LOD 200

        // Cull Off so that the box is visible from the inside.
        Cull Off

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            // --- Properties ---------------------------------------------------
            sampler2D _ColorTex;
            sampler2D _DepthTex;
            float4    _ColorTex_ST;

            float4 _BoxCenter;
            float4 _BoxHalfSize;
            float  _CaptureFarPlane;

            float  _DepthScale;
            int    _ParallaxSteps;

            float  _MinParallaxDist;
            float  _MaxParallaxDist;
            float  _GrazingAngleFade;
            float  _UVBorderClamp;

            // --- Structures ---------------------------------------------------
            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float2 uv      : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                float3 worldNorm  : TEXCOORD2;
                float3 tViewDir   : TEXCOORD3; // view dir in tangent space
                float  faceDist   : TEXCOORD4; // perpendicular dist from camera to face plane
                float  distToCam  : TEXCOORD5; // world distance to camera
                UNITY_FOG_COORDS(6)
            };

            // --- Vertex Shader ------------------------------------------------
            v2f vert(appdata v)
            {
                v2f o;
                o.pos       = UnityObjectToClipPos(v.vertex);
                o.uv        = TRANSFORM_TEX(v.uv, _ColorTex);
                o.worldPos  = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNorm = UnityObjectToWorldNormal(v.normal);

                // Build tangent-space matrix
                float3 worldTangent  = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(o.worldNorm, worldTangent) * v.tangent.w;

                float3 worldViewDir = _WorldSpaceCameraPos.xyz - o.worldPos;
                o.distToCam = length(worldViewDir);
                worldViewDir = normalize(worldViewDir);

                // Transform view direction into tangent space
                o.tViewDir.x = dot(worldTangent,  worldViewDir);
                o.tViewDir.y = dot(worldBinormal,  worldViewDir);
                o.tViewDir.z = dot(o.worldNorm,    worldViewDir);

                // Perpendicular distance from camera to the surface plane
                o.faceDist = abs(dot(worldViewDir, o.worldNorm)) * o.distToCam;

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            // --- Fragment Shader ----------------------------------------------
            fixed4 frag(v2f i) : SV_Target
            {
                float3 tView = normalize(i.tViewDir);

                // === Attenuation 1: Distance fade ===
                // Fade parallax to zero when camera is too close to the face.
                float distAtten = smoothstep(_MinParallaxDist, _MaxParallaxDist, i.distToCam);

                // === Attenuation 2: Grazing-angle fade ===
                // Prevent infinite stretching at shallow angles.
                float grazingAtten = smoothstep(0.0, _GrazingAngleFade, abs(tView.z));

                // === Attenuation 3: Position correction ===
                // Scale depth offset based on how far the camera has moved from
                // the original capture centre (box centre).
                float3 camOffset   = _WorldSpaceCameraPos.xyz - _BoxCenter.xyz;
                float  offsetRatio = length(camOffset) / max(length(_BoxHalfSize.xyz), 0.001);
                float  posCorrection = saturate(1.0 - offsetRatio * 0.5);

                float totalAtten = distAtten * grazingAtten * posCorrection;

                // === Iterative parallax offset ===
                float2 uv = i.uv;
                float  safeFaceDist = max(i.faceDist, 0.01);

                float2 viewXY = tView.xy / max(abs(tView.z), 0.001);

                for (int step = 0; step < _ParallaxSteps; step++)
                {
                    float depth = tex2Dlod(_DepthTex, float4(uv, 0, 0)).r * _CaptureFarPlane;
                    float displacement = depth * _DepthScale * posCorrection / safeFaceDist;
                    uv = i.uv + viewXY * displacement * totalAtten;
                }

                // Clamp UVs to avoid border artefacts
                float border = _UVBorderClamp;
                uv = clamp(uv, border, 1.0 - border);

                fixed4 col = tex2D(_ColorTex, uv);

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
