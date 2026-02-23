Shader "Natane/Toon Shader Wirelight"
{
    Properties
    {
        [Header(Main Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [Header(Wirelight Effect)]
        [Toggle(_WIRELIGHT)] _Wirelight ("Enable Wirelight", Float) = 1

        [Header(Cage Vertex Rounding)]
        _Cage ("Cage 1 Rounding Factor", Range(1, 200)) = 50
        _Extrude ("Extrude 1 Push Amount", Range(0, 0.2)) = 0.02
        _Cage2 ("Cage 2 Rounding Factor", Range(1, 200)) = 50
        _Extrude2 ("Extrude 2 Push Amount", Range(0, 0.2)) = 0
        _RoundingFudge ("Rounding Blend Offset", Range(-1, 1)) = 0
        _RoundingPow ("Rounding Blend Power", Range(0.1, 5)) = 1
        _ExtrudeFudge ("Extrude Blend Offset", Range(-1, 1)) = 0
        _ExtrudePow ("Extrude Blend Power", Range(0.1, 5)) = 1

        [Header(Triangle Visualization)]
        _TriangleShape ("Triangle Roundness 0=Sharp 1=Round", Range(0, 1)) = 0
        _TriangleNumerator ("Triangle Edge Width", Range(0.001, 0.1)) = 0.01
        _TrianglePower ("Triangle Edge Sharpness", Range(0.1, 10)) = 2.2
        _TriangleRoundnessBreakout ("Additional Roundness", Range(-1, 1)) = 0

        [Header(Dithering Pattern)]
        _DitherMultiple ("Face Dither Density", Range(0, 50)) = 15
        _LineMultiple ("Edge Dither Density", Range(0, 50)) = 15
        _DitherFudge ("Dither Threshold Adjust", Range(-1, 1)) = 0

        [Header(Noise Animation)]
        _NoiseSettings ("Noise Scale XYZ Rotation", Vector) = (10, 1, 0, 0)
        _NoiseMovementDirection ("Movement Dir XYZ Speed W", Vector) = (0, 0.1, 0, 1)
        _NoiseSmoothstep ("Noise Range Min Max", Vector) = (0, 2, 0, 0)
        _NoiseSubtract ("Noise Offset", Range(-1, 1)) = 0.3
        _NoiseMultiple ("Noise Intensity", Range(0, 50)) = 20

        [Header(Dual Color System)]
        [HDR] _Col ("Color 1", Color) = (1, 1, 1, 1)
        _ColHueShift ("Color 1 Hue Shift", Range(0, 1)) = 0
        [HDR] _Col2 ("Color 2", Color) = (1, 1, 1, 1)
        _Col2HueShift ("Color 2 Hue Shift", Range(0, 1)) = 0
        _AutoHueShift ("Auto Hue Shift C1 C2", Vector) = (0, 0, 0, 0)
        _ColorRange ("Color Transition Width", Range(0, 1)) = 0.1
        _ColorBrightness ("Color Brightness", Range(0, 3)) = 1
        _ColorPower ("Color Gamma Power", Range(0.1, 3)) = 1
        _MultAlphaAndColor ("Multiply Alpha to Color", Range(0, 1)) = 0

        [Header(Textures)]
        _ColorTexture ("Color Texture", 2D) = "white" {}
        _MaskTexture ("Mask Texture", 2D) = "white" {}

        [Header(Space Selector)]
        [Toggle(_USE_VERTEX_COLOR_POS)] _SpaceSelector ("Use Vertex Color Position", Float) = 0

        [Header(AudioLink)]
        [Toggle(_AUDIOLINK)] _AudioLink ("Enable AudioLink", Float) = 0
        _ExtrudeAudiolink ("AL Extrude Intensity", Range(0, 0.2)) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _ExtrudeBand ("AL Extrude Band", Int) = 0
        _ColorFudgeAudiolink ("AL Color Intensity", Range(0, 1)) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _ColorFudgeBand ("AL Color Band", Int) = 0
        _NoiseMovementChronoAudiolink ("AL Chronotensity Intensity", Range(0, 10)) = 0
        [Enum(Accel,0,Accel Smooth,1,Oscillate,2,Oscillate Smooth,3,Dark Move,4,Dark Move Smooth,5,Bidirectional,6,Bidirectional Smooth,7)] _NoiseMovementChronoMode ("AL Chrono Mode", Int) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _NoiseMovementChronoBand ("AL Chrono Band", Int) = 0
        [Enum(None,0,Theme 1,1,Theme 2,2,Theme 3,3,Theme 4,4)] _ColorOneTheme ("Color 1 Theme", Int) = 0
        [Enum(None,0,Theme 1,1,Theme 2,2,Theme 3,3,Theme 4,4)] _ColorTwoTheme ("Color 2 Theme", Int) = 0
        _InvertCol ("Invert Theme Colors", Range(0, 1)) = 0

        [Header(Cyber Wire Mode)]
        [Enum(Default,0,Cyber Wire,1)] _WireStyleMode ("Wire Style", Int) = 0
        [HDR] _CyberNeonColor ("Cyber Neon Color", Color) = (0.2, 1.0, 1.3, 1)
        _CyberEdgeBoost ("Cyber Edge Boost", Range(0, 5)) = 1.5
        _CyberPulseSpeed ("Cyber Pulse Speed", Range(0, 20)) = 4
        _CyberPulseIntensity ("Cyber Pulse Intensity", Range(0, 2)) = 0.6

        [Header(Cyber Optional FX)]
        [Toggle(_CYBER_SCANLINE)] _CyberScanline ("Enable Scanline FX", Float) = 0
        _CyberScanlineDensity ("Scanline Density", Range(10, 1200)) = 480
        _CyberScanlineSpeed ("Scanline Speed", Range(-10, 10)) = 1.0
        _CyberScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.2
        [Toggle(_CYBER_CHROMA)] _CyberChromaShift ("Enable Chroma Shift", Float) = 0
        _CyberChromaAmount ("Chroma Amount", Range(0, 2)) = 0.2
        [Toggle(_CYBER_GLITCH)] _CyberGlitch ("Enable Glitch Flicker", Float) = 0
        _CyberGlitchStrength ("Glitch Strength", Range(0, 1)) = 0.25
        _CyberGlitchSpeed ("Glitch Speed", Range(0.1, 20)) = 4.0

        [Header(AudioLink Cyber)]
        _CyberAudioPulse ("AL Cyber Pulse", Range(0, 2)) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _CyberAudioPulseBand ("AL Pulse Band", Int) = 2
        _CyberAudioGlitch ("AL Cyber Glitch", Range(0, 1)) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _CyberAudioGlitchBand ("AL Glitch Band", Int) = 3
        _CyberAudioData ("AL Cyber Data Stream", Range(0, 1)) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _CyberAudioDataBand ("AL Data Band", Int) = 1

        [Header(Cyber Data Stream)]
        [Toggle(_CYBER_DATASTREAM)] _CyberDataStream ("Enable Data Stream", Float) = 0
        _CyberDataDensity ("Data Density", Range(10, 800)) = 260
        _CyberDataSpeed ("Data Speed", Range(-10, 10)) = 2.2
        _CyberDataStrength ("Data Strength", Range(0, 2)) = 0.45
        _CyberDataJitter ("Data Jitter", Range(0, 1)) = 0.2

        [Header(VRC Optimization)]
        [Enum(Quality,0,Balanced,1,Lite,2)] _VRCPerfMode ("Performance Mode", Float) = 1
        [Toggle(_WL_DISTANCE_FADE)] _UseWLDistanceFade ("Use Distance Fade", Float) = 0
        _WLDistanceFadeStart ("Fade Start Distance", Range(0, 100)) = 10
        _WLDistanceFadeEnd ("Fade End Distance", Range(0.1, 200)) = 24
        _WLDistanceFadePower ("Fade Power", Range(0.5, 4)) = 1.2
        [Toggle] _UseWLAlphaClip ("Use Alpha Clip", Float) = 0
        _WLAlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.08

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlphaWL ("Source Blend", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlphaWL ("Dest Blend", Int) = 10
        [Enum(Off,0,On,1)] _ZWriteWL ("Z Write", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestWL ("Z Test", Int) = 4

        [Header(Stencil)]
        _StencilRef ("Stencil Reference", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilCompareFunctionWL ("Stencil Compare", Int) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPassOpWL ("Stencil Pass", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFailOpWL ("Stencil Fail", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFailOpWL ("Stencil ZFail", Int) = 0

        [Header(Queue)]
        [IntRange] _RenderQueue ("Render Queue Override", Range(2500, 3000)) = 2501
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "WIRELIGHT"
            Tags { "LightMode" = "ForwardBase" }

            Blend [_SrcBlendAlphaWL] [_DstBlendAlphaWL]
            ZWrite [_ZWriteWL]
            ZTest [_ZTestWL]
            Cull Off

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilCompareFunctionWL]
                Pass [_StencilPassOpWL]
                Fail [_StencilFailOpWL]
                ZFail [_StencilZFailOpWL]
            }

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _WIRELIGHT
            #pragma shader_feature_local _USE_VERTEX_COLOR_POS
            #pragma shader_feature_local _AUDIOLINK
            #pragma shader_feature_local _CYBER_SCANLINE
            #pragma shader_feature_local _CYBER_CHROMA
            #pragma shader_feature_local _CYBER_GLITCH
            #pragma shader_feature_local _CYBER_DATASTREAM
            #pragma shader_feature_local _WL_DISTANCE_FADE

            #include "UnityCG.cginc"
            #include "Include/Wirelight/MorgansNoiseFunctions.cginc"
            #include "Include/Wirelight/OKLAB.cginc"
            #include "Include/Wirelight/WirelightAudioLink.cginc"

            // Bayer matrix for dithering (4x4)
            static const float bayerMatrix[4][4] = {
                { 0.0,  8.0,  2.0, 10.0 },
                {12.0,  4.0, 14.0,  6.0 },
                { 3.0, 11.0,  1.0,  9.0 },
                {15.0,  7.0, 13.0,  5.0 }
            };

            // Properties
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            // Cage parameters
            float _Cage, _Cage2;
            float _Extrude, _Extrude2;
            float _RoundingFudge, _RoundingPow;
            float _ExtrudeFudge, _ExtrudePow;

            // Triangle parameters
            float _TriangleShape;
            float _TriangleNumerator;
            float _TrianglePower;
            float _TriangleRoundnessBreakout;

            // Dithering
            float _DitherMultiple;
            float _LineMultiple;
            float _DitherFudge;

            // Noise
            float4 _NoiseSettings;
            float4 _NoiseMovementDirection;
            float2 _NoiseSmoothstep;
            float _NoiseSubtract;
            float _NoiseMultiple;

            // Colors
            float4 _Col, _Col2;
            float _ColHueShift, _Col2HueShift;
            float2 _AutoHueShift;
            float _ColorRange;
            float _ColorBrightness;
            float _ColorPower;
            float _MultAlphaAndColor;

            // Textures
            sampler2D _ColorTexture;
            float4 _ColorTexture_ST;
            sampler2D _MaskTexture;
            float4 _MaskTexture_ST;

            // AudioLink
            float _ExtrudeAudiolink;
            int _ExtrudeBand;
            float _ColorFudgeAudiolink;
            int _ColorFudgeBand;
            float _NoiseMovementChronoAudiolink;
            int _NoiseMovementChronoMode;
            int _NoiseMovementChronoBand;
            int _ColorOneTheme;
            int _ColorTwoTheme;
            float _InvertCol;

            // Cyber wire mode
            float _WireStyleMode;
            float4 _CyberNeonColor;
            float _CyberEdgeBoost;
            float _CyberPulseSpeed;
            float _CyberPulseIntensity;
            float _CyberScanlineDensity;
            float _CyberScanlineSpeed;
            float _CyberScanlineStrength;
            float _CyberChromaAmount;
            float _CyberGlitchStrength;
            float _CyberGlitchSpeed;
            float _CyberAudioPulse;
            int _CyberAudioPulseBand;
            float _CyberAudioGlitch;
            int _CyberAudioGlitchBand;
            float _CyberAudioData;
            int _CyberAudioDataBand;
            float _CyberDataDensity;
            float _CyberDataSpeed;
            float _CyberDataStrength;
            float _CyberDataJitter;

            // VRC optimization
            float _VRCPerfMode;
            float _WLDistanceFadeStart;
            float _WLDistanceFadeEnd;
            float _WLDistanceFadePower;
            float _UseWLAlphaClip;
            float _WLAlphaClipThreshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
                float3 wp : TEXCOORD0;
                float4 col : COLOR;
                float2 uv : TEXCOORD1;
                float2 coluv : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 bary : TEXCOORD0;
                float4 col : COLOR;
                float2 uv : TEXCOORD1;
                float2 coluv : TEXCOORD2;
                float4 suv : TEXCOORD3;
                float3 wp : TEXCOORD5;
                float3 worldPos : TEXCOORD6;
                UNITY_FOG_COORDS(4)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Vertex shader
            v2g vert(appdata v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.coluv = TRANSFORM_TEX(v.uv, _ColorTexture);

                // Calculate noise offset with Chronotensity if AudioLink enabled
                float chronoTime = 0;
                #ifdef _AUDIOLINK
                    chronoTime = _NoiseMovementChronoAudiolink *
                                AudioLinkGetChronoTime(_NoiseMovementChronoMode, _NoiseMovementChronoBand);
                #endif

                float3 noiseOffset = _NoiseMovementDirection.xyz *
                                   (_Time.y * _NoiseMovementDirection.w + chronoTime);

                // Choose position source: vertex position or vertex color
                #ifdef _USE_VERTEX_COLOR_POS
                    float3 worldPos = v.color.rgb + noiseOffset;
                #else
                    float3 worldPos = mul(UNITY_MATRIX_M, v.vertex).xyz + noiseOffset;
                #endif

                o.wp = worldPos;

                // Calculate noise value for this vertex
                float noiseValue = pattern(
                    worldPos,
                    _NoiseSettings.x,
                    _NoiseSettings.y,
                    _NoiseSettings.z,
                    _NoiseSettings.w
                );

                // Interpolate cage parameters based on noise
                float cageBlend = pow(saturate(noiseValue + _RoundingFudge), _RoundingPow);
                float finalCage = lerp(_Cage, _Cage2, cageBlend);

                // Interpolate extrude with AudioLink modulation
                float extrudeBlend = pow(saturate(noiseValue + _ExtrudeFudge), _ExtrudePow);
                float audioLinkExtrude = 0;
                #ifdef _AUDIOLINK
                    audioLinkExtrude = _ExtrudeAudiolink * AudioLinkGetBandIntensity(_ExtrudeBand);
                #endif
                float finalExtrude = lerp(_Extrude, _Extrude2 + audioLinkExtrude, extrudeBlend);

                // Apply cage transformation
                float4 worldVertex = mul(UNITY_MATRIX_M, v.vertex + float4(v.normal * finalExtrude, 0));
                float4 roundedVertex = round(finalCage * worldVertex) / finalCage;
                v.vertex = mul(unity_WorldToObject, roundedVertex);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.col = float4(noiseValue, noiseValue, noiseValue, 1);
                o.worldPos = mul(UNITY_MATRIX_M, v.vertex).xyz;

                return o;
            }

            // Geometry shader - assigns barycentric coordinates
            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                UNITY_SETUP_INSTANCE_ID(IN[0]);

                g2f o;
                UNITY_INITIALIZE_OUTPUT(g2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Calculate triangle center for noise sampling
                float3 center = (IN[0].wp + IN[1].wp + IN[2].wp) / 3.0;
                float centerNoise = pattern(
                    center,
                    _NoiseSettings.x,
                    _NoiseSettings.y,
                    _NoiseSettings.z,
                    _NoiseSettings.w
                );

                // Output three vertices with barycentric coordinates
                for (int i = 0; i < 3; i++)
                {
                    UNITY_TRANSFER_INSTANCE_ID(IN[i], o);
                    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[i], o);

                    o.pos = IN[i].pos;
                    o.bary = float3(
                        i == 0 ? 1 : 0,
                        i == 1 ? 1 : 0,
                        i == 2 ? 1 : 0
                    );
                    o.col = float4(centerNoise, centerNoise, centerNoise, 1);
                    o.uv = IN[i].uv;
                    o.coluv = IN[i].coluv;
                    o.suv = ComputeScreenPos(IN[i].pos);
                    o.wp = IN[i].wp;
                    o.worldPos = IN[i].worldPos;

                    UNITY_TRANSFER_FOG(o, o.pos);
                    triStream.Append(o);
                }
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            // Fragment shader
            fixed4 frag(g2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // When _WIRELIGHT keyword is disabled, discard all fragments
                // This allows the toggle to completely hide the wirelight effect
                #ifndef _WIRELIGHT
                    discard;
                    return fixed4(0, 0, 0, 0);
                #endif

                // Get colors with hue shift and AudioLink theme
                float4 color1 = _Col * tex2D(_ColorTexture, i.coluv);
                float4 color2 = _Col2 * tex2D(_ColorTexture, i.coluv);

                // Apply AudioLink theme colors
                #ifdef _AUDIOLINK
                    if (_ColorOneTheme > 0)
                    {
                        color1 = AudioLinkGetThemeColor(_ColorOneTheme - 1, _InvertCol);
                    }
                    if (_ColorTwoTheme > 0)
                    {
                        color2 = AudioLinkGetThemeColor(_ColorTwoTheme - 1, _InvertCol);
                    }
                #endif

                // Apply hue shift in OKLAB space
                color1 = OKLcolShift(color1, _ColHueShift + _AutoHueShift.x * _Time.y);
                color2 = OKLcolShift(color2, _Col2HueShift + _AutoHueShift.y * _Time.y);

                // VRC optimization factors
                float perfLerp = saturate(_VRCPerfMode * 0.5); // 0=quality, 0.5=balanced, 1=lite
                float perfLite = step(1.5, _VRCPerfMode);

                // Calculate triangle edges
                float minBary = min(min(i.bary.r, i.bary.g), i.bary.b);
                float safeMinBary = max(1e-4, minBary);
                float baryProd = max(1e-5, i.bary.r * i.bary.g * i.bary.b);
                float triPower = lerp(_TrianglePower, max(0.5, _TrianglePower * 0.85), perfLerp);
                float SharpTris = pow(saturate(_TriangleNumerator * rcp(safeMinBary)), triPower);
                float RoundedTris = pow(saturate((_TriangleNumerator / 10.0) * rcp(baryProd)), triPower);
                float Triangles = saturate(lerp(SharpTris, RoundedTris, _TriangleShape + _TriangleRoundnessBreakout));

                // Calculate noise mask
                float noiseStrength = lerp(_NoiseMultiple, _NoiseMultiple * 0.75, perfLerp);
                float noise = noiseStrength *
                             smoothstep(_NoiseSmoothstep.x, _NoiseSmoothstep.y, i.col.r - _NoiseSubtract) *
                             tex2D(_MaskTexture, i.uv).r;

                // Bayer dithering
                float2 screenPos = (i.suv.xy / max(i.suv.w, 0.0001)) * _ScreenParams.xy;
                uint dithx = (uint)(screenPos.x) % 4;
                uint dithy = (uint)(screenPos.y) % 4;
                float ditherFace = lerp(_DitherMultiple, _DitherMultiple * 0.8, perfLerp);
                float ditherLine = lerp(_LineMultiple, _LineMultiple * 0.7, perfLerp);
                float threshold = noise * ditherFace + Triangles * noise * ditherLine;
                float dither = (threshold > bayerMatrix[dithy][dithx]) ? saturate(noise + _DitherFudge) : 0.0;

                // Color blending with AudioLink modulation
                float colorFudge = 0;
                #ifdef _AUDIOLINK
                    colorFudge = _ColorFudgeAudiolink * AudioLinkGetBandIntensity(_ColorFudgeBand);
                #endif
                float colorBlend = saturate(noise + _ColorRange + colorFudge);
                float4 baseColor = lerp(color2, color1, colorBlend);

                // Calculate relative luminance for alpha
                float relLum = dot(baseColor.rgb, float3(0.2126, 0.7152, 0.0722));

                // Final color composition
                float alpha = Triangles * noise + lerp(1, 0, Triangles) * dither;

                fixed4 col;
                col.rgb = (baseColor.rgb * lerp(1, 0, Triangles)) +           // Face color
                          (Triangles * baseColor.rgb) +                        // Edge color
                          (pow(max(Triangles, 0.0), 2.2) * noise * 3 * baseColor.rgb);  // Extra glow

                col.a = saturate(alpha * relLum + pow(max(Triangles, 0.0), 2.2) * noise * 5 * relLum);

                // Apply brightness and gamma
                col.rgb = pow(max(col.rgb * _ColorBrightness, 0.0), _ColorPower) *
                         lerp(1, col.a, _MultAlphaAndColor);

                // Cyber wire style layer
                if (_WireStyleMode > 0.5)
                {
                    float audioPulse = 0;
                    float audioGlitch = 0;
                    float audioData = 0;
                    #ifdef _AUDIOLINK
                        audioPulse = _CyberAudioPulse * AudioLinkGetBandIntensity(_CyberAudioPulseBand);
                        audioGlitch = _CyberAudioGlitch * AudioLinkGetBandIntensity(_CyberAudioGlitchBand);
                        audioData = _CyberAudioData * AudioLinkGetBandIntensity(_CyberAudioDataBand);
                    #endif

                    float pulseWave = sin(_Time.y * _CyberPulseSpeed + noise * 6.2831853) * 0.5 + 0.5;
                    float pulse = 1.0 + (pulseWave * _CyberPulseIntensity) + audioPulse;

                    float edgeMask = saturate((pow(Triangles, 1.2) * 1.5 + dither * 0.5) * (_CyberEdgeBoost * 0.5));
                    float3 neonLayer = _CyberNeonColor.rgb * edgeMask * pulse;

                    // Blend as a bright wire pass while preserving base fill.
                    col.rgb += neonLayer * 0.6;
                    col.a = saturate(col.a + edgeMask * 0.2 + audioPulse * 0.15);

                    #if defined(_CYBER_SCANLINE) || defined(_CYBER_GLITCH)
                        float2 screenUvFx = i.suv.xy / i.suv.w;
                    #endif

                    #ifdef _CYBER_SCANLINE
                        float scan = sin(screenUvFx.y * _CyberScanlineDensity + _Time.y * _CyberScanlineSpeed * 6.2831853) * 0.5 + 0.5;
                        float scanStrength = saturate((_CyberScanlineStrength + audioPulse * 0.15) * lerp(1.0, 0.75, perfLerp));
                        col.rgb *= lerp(1.0, scan, scanStrength);
                    #endif

                    #ifdef _CYBER_CHROMA
                        float chromaAmount = _CyberChromaAmount * (1.0 - perfLite * 0.7);
                        float2 shift = float2((chromaAmount * (0.0008 + edgeMask * 0.0015)), 0);
                        float rShift = tex2D(_MaskTexture, i.uv + shift).r;
                        float bShift = tex2D(_MaskTexture, i.uv - shift).r;
                        col.r += rShift * chromaAmount * 0.25;
                        col.b += bShift * chromaAmount * 0.25;
                    #endif

                    #ifdef _CYBER_GLITCH
                        float glitchStrength = _CyberGlitchStrength * (1.0 - perfLite * 0.65);
                        float glitchSpeed = lerp(_CyberGlitchSpeed, max(0.5, _CyberGlitchSpeed * 0.6), perfLerp);
                        float block = floor(screenUvFx.y * 140.0);
                        float timeBlock = floor(_Time.y * glitchSpeed);
                        float random = Hash21(float2(block, timeBlock));
                        float glitchThreshold = saturate(1.0 - (glitchStrength + audioGlitch));
                        float glitchMask = step(glitchThreshold, random);
                        float glitchBoost = 1.0 + glitchMask * (0.35 + audioGlitch);
                        col.rgb *= glitchBoost;
                        col.rgb = lerp(col.rgb, col.bgr, glitchMask * 0.2);
                    #endif

                    #ifdef _CYBER_DATASTREAM
                        float2 dataUV = i.uv * float2(max(1.0, _CyberDataDensity * 0.08), max(1.0, _CyberDataDensity * 0.24));
                        float rowId = floor(dataUV.y);
                        float streamTime = floor(_Time.y * (abs(_CyberDataSpeed) + 0.25));
                        float jitterSeed = Hash21(float2(rowId, streamTime));
                        float jitter = (jitterSeed - 0.5) * _CyberDataJitter;
                        float streamPhase = frac(dataUV.y - _Time.y * _CyberDataSpeed + noise * 0.2 + jitter);
                        float streamLine = smoothstep(0.94, 1.0, streamPhase);
                        float packet = step(0.86, Hash21(float2(floor(dataUV.x * 0.5), rowId + streamTime)));
                        float dataStrength = (_CyberDataStrength * lerp(1.0, 0.75, perfLerp)) + audioData;
                        float dataMask = saturate((streamLine * (0.65 + packet * 0.35)) * edgeMask);
                        col.rgb += _CyberNeonColor.rgb * dataMask * dataStrength;
                        col.a = saturate(col.a + dataMask * (0.12 + audioData * 0.1));
                    #endif
                }

                #ifdef _WL_DISTANCE_FADE
                    float fadeRange = max(0.001, _WLDistanceFadeEnd - _WLDistanceFadeStart);
                    float distanceFade = saturate((_WLDistanceFadeEnd - distance(i.worldPos, _WorldSpaceCameraPos)) / fadeRange);
                    distanceFade = pow(max(distanceFade, 0.0), _WLDistanceFadePower);
                    col.a *= distanceFade;
                #endif

                if (_UseWLAlphaClip > 0.5)
                {
                    clip(col.a - _WLAlphaClipThreshold);
                }

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }

            ENDCG
        }
    }

    CustomEditor "NataneToonShaderGUI"
    FallBack "Diffuse"
}
