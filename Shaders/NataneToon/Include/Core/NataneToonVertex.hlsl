#ifndef NATANE_TOON_VERTEX_INCLUDED
#define NATANE_TOON_VERTEX_INCLUDED

// VAT Helper Functions
#ifdef _VAT
// Decode position data from VAT texture
float3 DecodeVATPosition(float4 rawData, float minVal, float maxVal)
{
    // Decode from normalized [0,1] range back to original range
    float3 position = rawData.rgb;
    position = position * (maxVal - minVal) + minVal;
    return position;
}

// Decode normal data from VAT texture
float3 DecodeVATNormal(float4 rawData, float minVal, float maxVal)
{
    // Decode from normalized [0,1] range back to original range
    float3 normal = rawData.rgb;
    normal = normal * 2.0 - 1.0; // Convert from [0,1] to [-1,1]
    return normalize(normal);
}

// Apply VAT animation to vertex
void ApplyVAT(inout float4 vertex, inout float3 normal, float2 uv)
{
    // Calculate frame to sample based on time
    float frame = frac(_Time.y * _VATSpeed) * _VATNumOfFrames;

    // Calculate V coordinate for the frame
    // V coordinate represents the frame, U coordinate represents the vertex ID
    float frameV = (frame + 0.5) / _VATNumOfFrames;

    // Sample position map
    float2 vatUV = float2(uv.x, frameV);
    float4 positionData = tex2Dlod(_VATPositionMap, float4(vatUV, 0, 0));
    float3 vatPosition = DecodeVATPosition(positionData, _VATPositionMin, _VATPositionMax);

    // Apply position offset with intensity control
    vertex.xyz += vatPosition * _VATIntensity;

    // Sample and apply normal map if available
    #ifdef _VAT_NORMAL
        float4 normalData = tex2Dlod(_VATNormalMap, float4(vatUV, 0, 0));
        float3 vatNormal = DecodeVATNormal(normalData, _VATNormalMin, _VATNormalMax);
        normal = lerp(normal, vatNormal, _VATIntensity);
    #endif
}
#endif

// Vertex Shader
// Transforms vertices and prepares data for fragment shader
v2f vert(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    // Apply VAT animation before transforming to clip space
    #ifdef _VAT
        ApplyVAT(v.vertex, v.normal, v.uv);
    #endif

    // Smear vertex stretch
    #ifdef _SMEAR
    {
        float3 rawDir = _SmearDirection.xyz;
        float3 smearDir;
        float smearAmount;

        if (_SmearAutoMagnitude > 0.5)
        {
            // Auto mode: direction vector の大きさ = 速度
            float speed = length(rawDir);
            smearDir = (speed > 0.001) ? rawDir / speed : float3(0, 0, 1);
            smearAmount = speed * _SmearMotionSensitivity;
            smearAmount = min(smearAmount, _SmearStretch); // _SmearStretch を最大値として使用
        }
        else
        {
            // Manual mode: 従来通り
            smearDir = normalize(rawDir + float3(0.0001, 0.0001, 0.0001));
            smearAmount = _SmearStretch;
        }

        // VAT velocity integration: derive smear from VAT animation speed
        #ifdef _VAT
        if (_SmearVATVelocity > 0.5)
        {
            float frame = frac(_Time.y * _VATSpeed) * _VATNumOfFrames;
            float prevFrame = frac((_Time.y - unity_DeltaTime.x) * _VATSpeed) * _VATNumOfFrames;

            float frameV_curr = (frame + 0.5) / _VATNumOfFrames;
            float frameV_prev = (prevFrame + 0.5) / _VATNumOfFrames;

            float4 posCurr = tex2Dlod(_VATPositionMap, float4(v.uv.x, frameV_curr, 0, 0));
            float4 posPrev = tex2Dlod(_VATPositionMap, float4(v.uv.x, frameV_prev, 0, 0));

            float3 vatVel = (DecodeVATPosition(posCurr, _VATPositionMin, _VATPositionMax)
                           - DecodeVATPosition(posPrev, _VATPositionMin, _VATPositionMax))
                           / max(unity_DeltaTime.x, 0.001);

            float vatSpeed = length(vatVel);
            if (vatSpeed > 0.01)
            {
                smearDir = vatVel / vatSpeed;
                smearAmount += vatSpeed * _SmearMotionSensitivity;
                smearAmount = min(smearAmount, _SmearStretch);
            }
        }
        #endif

        float3 worldNorm = UnityObjectToWorldNormal(v.normal);
        float dirMask = saturate(dot(worldNorm, smearDir));

        // Simple noise using vertex position
        float noise = frac(sin(dot(v.vertex.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
        noise = lerp(1.0, noise, _SmearNoiseStrength * _SmearNoiseScale * 0.2);

        float3 offset = smearDir * smearAmount * dirMask * noise;
        // Transform offset from world to object space
        offset = mul((float3x3)unity_WorldToObject, offset);
        v.vertex.xyz += offset;

        o.smearStretchFactor = dirMask * smearAmount;
    }
    #endif

    // Transform vertex to clip space
    o.pos = UnityObjectToClipPos(v.vertex);

    // Calculate UV coordinates with tiling and offset
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

    // Transform position to world space
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

    // Transform normal, tangent, and binormal to world space for lighting calculations
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);

    // Calculate binormal (bitangent) using cross product
    // tangent.w contains handedness information for correct orientation
    o.worldBinormal = cross(o.worldNormal, o.worldTangent) * v.tangent.w * unity_WorldTransformParams.w;

    // Smooth Normal: Decode from vertex color and transform to world space for shading
    #ifdef _SMOOTH_NORMAL
    {
        float3 smoothNormalOS = v.normal; // fallback to original
        if (_SmoothNormalMode < 0.5)
        {
            // Mode 0: Vertex Color Object Space
            smoothNormalOS = v.color.rgb * 2.0 - 1.0;
        }
        else if (_SmoothNormalMode < 1.5)
        {
            // Mode 1: Vertex Color Tangent Space (lilToon compatible)
            float3 smoothTS = v.color.rgb * 2.0 - 1.0;
            float3 binormal = cross(v.normal, v.tangent.xyz) * v.tangent.w;
            float3x3 tbnOS = float3x3(v.tangent.xyz, binormal, v.normal);
            smoothNormalOS = mul(smoothTS, tbnOS);
        }
        else
        {
            // Mode 2: Baked Normal Texture (tangent space, same as outline pass)
            float3 bakedNormal = tex2Dlod(_SmoothNormalTex, float4(v.uv, 0, 0)).rgb * 2.0 - 1.0;
            float3 binormal = cross(v.normal, v.tangent.xyz) * v.tangent.w;
            float3x3 tbnOS = float3x3(v.tangent.xyz, binormal, v.normal);
            smoothNormalOS = mul(bakedNormal, tbnOS);
        }
        o.smoothWorldNormal = UnityObjectToWorldNormal(normalize(smoothNormalOS));
    }
    #endif

    // Calculate screen position for GrabPass (Refraction) / Dithering Alpha / Intersection Fade
    #if defined(_REFRACTION) || defined(_PARALLAX) || defined(_DISSOLVE) || defined(_DITHERING_ALPHA) || defined(_INTERSECTION_FADE)
        o.screenPos = ComputeScreenPos(o.pos);
    #endif

    // Lightmap UV (Background mode only)
    #ifdef _BACKGROUND_MODE
        o.lightmapUV = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
    #endif

    // Detail Map UV1 pass-through
    #ifdef _DETAIL_MAP
        o.uv1 = v.uv1;
    #endif

    // Transfer fog and shadow coordinates
    UNITY_TRANSFER_FOG(o, o.pos);
    TRANSFER_SHADOW(o);

    // ForwardBase vertex light calculation (4 point lights)
    // multi_compile_fwdbase already defines VERTEXLIGHT_ON when vertex lights are available
    // Skip when _PIXEL_VERTEX_LIGHTS is enabled (fragment shader handles it instead)
    #if defined(VERTEXLIGHT_ON) && !defined(_PIXEL_VERTEX_LIGHTS)
        o.vertexLightColor = Shade4PointLights(
            unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
            unity_LightColor[0].rgb, unity_LightColor[1].rgb,
            unity_LightColor[2].rgb, unity_LightColor[3].rgb,
            unity_4LightAtten0,
            o.worldPos, o.worldNormal
        );
    #endif

    return o;
}

#endif // NATANE_TOON_VERTEX_INCLUDED
