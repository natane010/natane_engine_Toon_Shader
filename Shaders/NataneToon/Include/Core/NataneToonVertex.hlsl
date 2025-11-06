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

    // Apply VAT animation before transforming to clip space
    #ifdef _VAT
        ApplyVAT(v.vertex, v.normal, v.uv);
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

    // Calculate screen position for GrabPass (Refraction)
    o.screenPos = ComputeScreenPos(o.pos);

    // Transfer fog and shadow coordinates
    UNITY_TRANSFER_FOG(o, o.pos);
    TRANSFER_SHADOW(o);

    return o;
}

#endif // NATANE_TOON_VERTEX_INCLUDED
