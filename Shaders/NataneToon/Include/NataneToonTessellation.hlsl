#ifndef NATANE_TOON_TESSELLATION_INCLUDED
#define NATANE_TOON_TESSELLATION_INCLUDED

#ifdef _TESSELLATION

// Tessellation Control Point Structure
struct TessellationControlPoint
{
    float4 vertex : INTERNALTESSPOS;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TEXCOORD0;
};

// Tessellation Factors Structure
struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

// Distance-based Tessellation Factor Calculation
float CalcDistanceTessFactor(float4 vertex, float minDist, float maxDist, float tessellationFactor)
{
    float3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;
    float dist = distance(worldPos, _WorldSpaceCameraPos);
    float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0);
    return f * tessellationFactor;
}

// Patch Constant Function
TessellationFactors PatchConstantFunction(InputPatch<TessellationControlPoint, 3> patch)
{
    TessellationFactors f;

    // Calculate distance-based tessellation factors for each edge
    float minDist = _TessellationMinDistance;
    float maxDist = _TessellationMaxDistance;
    float tessFactor = _TessellationFactor;

    f.edge[0] = CalcDistanceTessFactor(patch[0].vertex, minDist, maxDist, tessFactor);
    f.edge[1] = CalcDistanceTessFactor(patch[1].vertex, minDist, maxDist, tessFactor);
    f.edge[2] = CalcDistanceTessFactor(patch[2].vertex, minDist, maxDist, tessFactor);
    f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) / 3.0;

    return f;
}

// Hull Shader
[domain("tri")]
[outputcontrolpoints(3)]
[outputtopology("triangle_cw")]
[partitioning("integer")]
[patchconstantfunc("PatchConstantFunction")]
TessellationControlPoint hull(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OutputControlPointID)
{
    return patch[id];
}

// Domain Shader
[domain("tri")]
v2f domain(TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
{
    appdata data;

    // Interpolate using barycentric coordinates
    #define DOMAIN_INTERPOLATE(fieldName) data.fieldName = \
        patch[0].fieldName * barycentricCoordinates.x + \
        patch[1].fieldName * barycentricCoordinates.y + \
        patch[2].fieldName * barycentricCoordinates.z;

    DOMAIN_INTERPOLATE(vertex)
    DOMAIN_INTERPOLATE(normal)
    DOMAIN_INTERPOLATE(tangent)
    DOMAIN_INTERPOLATE(uv)

    // Apply displacement mapping
    float height = tex2Dlod(_DisplacementMap, float4(data.uv, 0, 0)).r;
    data.vertex.xyz += data.normal * (height - 0.5) * _DisplacementStrength * 2.0;

    // Call the standard vertex shader
    return vert(data);
}

#endif // _TESSELLATION

#endif // NATANE_TOON_TESSELLATION_INCLUDED
