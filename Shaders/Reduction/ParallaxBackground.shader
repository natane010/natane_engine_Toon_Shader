Shader "Custom/ParallaxBackground"
{
    // Runtime parallax occlusion mapping shader for curved background surfaces.
    // Uses colour, normal map, and height map to recreate 3D depth on simplified geometry.

    Properties
    {
        _MainTex       ("Color Texture",  2D)              = "white" {}
        _BumpMap       ("Normal Map",     2D)              = "bump"  {}
        _HeightMap     ("Height Map",     2D)              = "black" {}

        [Header(Parallax)]
        _Parallax      ("Parallax Depth", Range(0, 0.2))   = 0.08
        _ParallaxSteps ("Max Steps",      Range(4, 64))    = 16

        [Header(Edge)]
        _EdgeFadeWidth ("Edge Fade Width", Range(0, 0.2))  = 0.05

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Geometry-1" "RenderType"="Opaque" }
        LOD 200
        Cull [_Cull]

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            // --- Properties ---
            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _BumpMap;
            sampler2D _HeightMap;

            float _Parallax;
            int   _ParallaxSteps;
            float _EdgeFadeWidth;

            // --- Structures ---
            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float2 uv      : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 tViewDir : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            // --- Vertex Shader ---
            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Build TBN matrix
                float3 worldNormal  = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;

                // View direction in world space
                float3 worldViewDir = normalize(_WorldSpaceCameraPos.xyz - o.worldPos);

                // Transform view direction to tangent space
                o.tViewDir.x = dot(worldTangent,  worldViewDir);
                o.tViewDir.y = dot(worldBinormal, worldViewDir);
                o.tViewDir.z = dot(worldNormal,   worldViewDir);

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            // --- Fragment Shader ---
            fixed4 frag(v2f i) : SV_Target
            {
                float3 tView = normalize(i.tViewDir);

                // Adaptive step count: more steps at grazing angles
                float angleFactor = 1.0 - abs(tView.z);
                int numSteps = (int)lerp((float)_ParallaxSteps * 0.25, (float)_ParallaxSteps, angleFactor);
                numSteps = max(numSteps, 4);

                float stepSize = 1.0 / (float)numSteps;

                // Parallax direction in UV space
                float2 deltaUV = tView.xy / max(abs(tView.z), 0.001) * _Parallax / (float)numSteps;

                // === Linear search phase ===
                float2 currentUV = i.uv;
                float  currentLayerDepth = 0.0;
                float  currentHeight = 1.0 - tex2Dlod(_HeightMap, float4(currentUV, 0, 0)).r;

                float2 prevUV = currentUV;
                float  prevLayerDepth = currentLayerDepth;
                float  prevHeight = currentHeight;

                [loop]
                for (int step = 0; step < 64; step++)
                {
                    if (step >= numSteps) break;
                    if (currentLayerDepth >= currentHeight) break;

                    prevUV = currentUV;
                    prevLayerDepth = currentLayerDepth;
                    prevHeight = currentHeight;

                    currentUV -= deltaUV;
                    currentLayerDepth += stepSize;
                    currentHeight = 1.0 - tex2Dlod(_HeightMap, float4(currentUV, 0, 0)).r;
                }

                // === Binary refinement phase (2 iterations) ===
                [unroll]
                for (int b = 0; b < 2; b++)
                {
                    float2 midUV = (prevUV + currentUV) * 0.5;
                    float  midLayer = (prevLayerDepth + currentLayerDepth) * 0.5;
                    float  midHeight = 1.0 - tex2Dlod(_HeightMap, float4(midUV, 0, 0)).r;

                    if (midLayer >= midHeight)
                    {
                        currentUV = midUV;
                        currentLayerDepth = midLayer;
                        currentHeight = midHeight;
                    }
                    else
                    {
                        prevUV = midUV;
                        prevLayerDepth = midLayer;
                        prevHeight = midHeight;
                    }
                }

                // Linear interpolation between last two samples
                float afterDepth  = currentHeight - currentLayerDepth;
                float beforeDepth = prevHeight - prevLayerDepth;
                float weight = afterDepth / (afterDepth - beforeDepth + 0.0001);
                float2 finalUV = lerp(currentUV, prevUV, weight);

                // === Edge fade ===
                float2 edgeDist = min(finalUV, 1.0 - finalUV);
                float  edgeFade = smoothstep(0.0, max(_EdgeFadeWidth, 0.001), min(edgeDist.x, edgeDist.y));

                // Clamp UVs to valid range
                finalUV = clamp(finalUV, 0.001, 0.999);

                // Sample textures
                fixed4 col = tex2D(_MainTex, finalUV);
                float3 normal = UnpackNormal(tex2D(_BumpMap, finalUV));

                // Simple directional lighting using normal map
                float ndotl = saturate(dot(normal, float3(0, 0, 1)));
                col.rgb *= lerp(0.8, 1.0, ndotl);

                // Apply edge fade
                col.a *= edgeFade;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    Fallback "Unlit/Texture"
}
