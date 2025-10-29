#ifndef NATANE_TOON_UTILS_INCLUDED
#define NATANE_TOON_UTILS_INCLUDED

// Utility Functions

// Calculate MatCap UV coordinates from world normal
// MatCap uses view-space normals to create sphere-mapped effects
float2 CalculateMatCapUV(float3 worldNormal, float3 viewDir)
{
    float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
    float2 matcapUV = viewNormal.xy * 0.5 + 0.5;
    return matcapUV;
}

#endif // NATANE_TOON_UTILS_INCLUDED
