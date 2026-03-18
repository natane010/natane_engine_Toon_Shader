// AutoMat_PBRLighting.hlsl
// Fragment shader: PBR lighting for Built-in RP (GGX + SH + Reflection Probe)

#ifndef AUTOMAT_PBRLIGHTING_INCLUDED
#define AUTOMAT_PBRLIGHTING_INCLUDED

// ===== Vertex Shader =====
v2f AutoMatVert(appdata v)
{
    v2f o;
    UNITY_INITIALIZE_OUTPUT(v2f, o);

    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
    o.worldBitangent = cross(o.worldNormal, o.worldTangent) * v.tangent.w;
    o.viewDir = UnityWorldSpaceViewDir(o.worldPos);

    TRANSFER_SHADOW(o);
    UNITY_TRANSFER_FOG(o, o.pos);
    return o;
}

// ===== Fragment Shader =====
half4 AutoMatFrag(v2f i) : SV_Target
{
    // Build TBN matrix
    float3 T = normalize(i.worldTangent);
    float3 B = normalize(i.worldBitangent);
    float3 N = normalize(i.worldNormal);
    float3x3 TBN = float3x3(T, B, N);
    float3 viewDirTS = mul(TBN, normalize(i.viewDir));

    // Sample surface data
    AutoMatSurfaceData surf = InitAutoMatSurface(i.uv, viewDirTS);

    // Debug modes
    #ifdef _DEBUG_ALBEDO
    return half4(surf.albedo, 1);
    #endif
    #ifdef _DEBUG_NORMAL
    return half4(surf.normal * 0.5 + 0.5, 1);
    #endif
    #ifdef _DEBUG_ROUGHNESS
    half r = 1.0 - surf.smoothness;
    return half4(r, r, r, 1);
    #endif
    #ifdef _DEBUG_METALLIC
    return half4(surf.metallic, surf.metallic, surf.metallic, 1);
    #endif

    // Transform normal to world space
    float3 worldNormal = normalize(mul(surf.normal, TBN));
    float3 viewDir = normalize(i.viewDir);

    // Light
    float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
    float3 halfDir = normalize(lightDir + viewDir);
    float NdotL = saturate(dot(worldNormal, lightDir));
    float NdotV = saturate(dot(worldNormal, viewDir));
    float NdotH = saturate(dot(worldNormal, halfDir));
    float VdotH = saturate(dot(viewDir, halfDir));

    // Shadow
    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

    // PBR: Metallic workflow
    half3 specColor;
    half oneMinusReflectivity;
    half3 diffColor = DiffuseAndSpecularFromMetallic(
        surf.albedo, surf.metallic, specColor, oneMinusReflectivity);

    float perceptualRoughness = 1.0 - surf.smoothness;
    float roughness = max(perceptualRoughness * perceptualRoughness, 0.002);

    // Diffuse (Lambert)
    half3 diffuse = diffColor * NdotL;

    // Specular (GGX via Unity BRDF)
    float D = GGXTerm(NdotH, roughness);
    float V_term = SmithJointGGXVisibilityTerm(NdotL, NdotV, roughness);
    float3 F = FresnelTerm(specColor, VdotH);
    half3 specular = D * V_term * F * UNITY_PI;
    specular = max(0, specular);

    // Direct lighting
    half3 directLight = (diffuse + specular * NdotL) * _LightColor0.rgb * atten;

    // Indirect: SH for diffuse
    half3 ambient = ShadeSH9(half4(worldNormal, 1.0)) * diffColor * surf.occlusion;

    // Indirect: Reflection probe for specular
    half3 reflDir = reflect(-viewDir, worldNormal);
    half mip = perceptualRoughness * 6.0;
    half4 envSample = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflDir, mip);
    half3 envColor = DecodeHDR(envSample, unity_SpecCube0_HDR);
    half surfaceReduction = 1.0 / (roughness * roughness + 1.0);
    half grazingTerm = saturate(surf.smoothness + (1.0 - oneMinusReflectivity));
    half3 indirectSpec = envColor * surfaceReduction * FresnelLerp(specColor, grazingTerm, NdotV) * surf.occlusion;

    half3 finalColor = directLight + ambient + indirectSpec + surf.emission;

    // Fog
    half4 col = half4(finalColor, 1.0);
    UNITY_APPLY_FOG(i.fogCoord, col);

    return col;
}

// ===== ForwardAdd Fragment =====
half4 AutoMatFragAdd(v2f i) : SV_Target
{
    float3 T = normalize(i.worldTangent);
    float3 B = normalize(i.worldBitangent);
    float3 N = normalize(i.worldNormal);
    float3x3 TBN = float3x3(T, B, N);
    float3 viewDirTS = mul(TBN, normalize(i.viewDir));

    AutoMatSurfaceData surf = InitAutoMatSurface(i.uv, viewDirTS);
    float3 worldNormal = normalize(mul(surf.normal, TBN));
    float3 viewDir = normalize(i.viewDir);
    float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
    float3 halfDir = normalize(lightDir + viewDir);

    float NdotL = saturate(dot(worldNormal, lightDir));
    float NdotV = saturate(dot(worldNormal, viewDir));
    float NdotH = saturate(dot(worldNormal, halfDir));
    float VdotH = saturate(dot(viewDir, halfDir));

    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

    half3 specColor;
    half oneMinusReflectivity;
    half3 diffColor = DiffuseAndSpecularFromMetallic(
        surf.albedo, surf.metallic, specColor, oneMinusReflectivity);

    float roughness = max((1.0 - surf.smoothness) * (1.0 - surf.smoothness), 0.002);
    float D = GGXTerm(NdotH, roughness);
    float V_term = SmithJointGGXVisibilityTerm(NdotL, NdotV, roughness);
    float3 F = FresnelTerm(specColor, VdotH);

    half3 diffuse = diffColor * NdotL;
    half3 specular = max(0, D * V_term * F * UNITY_PI) * NdotL;

    half3 color = (diffuse + specular) * _LightColor0.rgb * atten;

    half4 col = half4(color, 1.0);
    UNITY_APPLY_FOG_COLOR(i.fogCoord, col, half4(0, 0, 0, 0));
    return col;
}

#endif // AUTOMAT_PBRLIGHTING_INCLUDED
