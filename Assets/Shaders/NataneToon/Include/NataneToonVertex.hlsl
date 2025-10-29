#ifndef NATANE_TOON_VERTEX_INCLUDED
#define NATANE_TOON_VERTEX_INCLUDED

// Vertex Shader
// Transforms vertices and prepares data for fragment shader
v2f vert(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

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

    // Transfer fog and shadow coordinates
    UNITY_TRANSFER_FOG(o, o.pos);
    TRANSFER_SHADOW(o);

    return o;
}

#endif // NATANE_TOON_VERTEX_INCLUDED
