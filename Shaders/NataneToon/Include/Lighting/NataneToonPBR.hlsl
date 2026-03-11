#ifndef NATANE_TOON_PBR_INCLUDED
#define NATANE_TOON_PBR_INCLUDED

// GGX Normal Distribution Function (Trowbridge-Reitz)
half NataneGGX_D(half NdotH, half roughness)
{
    half a2 = roughness * roughness;
    half d = NdotH * NdotH * (a2 - 1.0) + 1.0;
    return a2 / (UNITY_PI * d * d + 1e-7);
}

// Smith-GGX Geometry Function (Height-Correlated)
half NataneGGX_G(half NdotL, half NdotV, half roughness)
{
    half a2 = roughness * roughness;
    half gL = NdotV * sqrt(NdotL * NdotL * (1.0 - a2) + a2);
    half gV = NdotL * sqrt(NdotV * NdotV * (1.0 - a2) + a2);
    return 0.5 / (gL + gV + 1e-7);
}

// Schlick Fresnel
half3 NataneSchlickFresnel(half cosTheta, half3 F0)
{
    return F0 + (1.0 - F0) * pow(saturate(1.0 - cosTheta), 5.0);
}

// Full PBR Direct Specular (Cook-Torrance microfacet BRDF)
half3 NatanePBRSpecular(half3 normal, half3 viewDir, half3 lightDir,
                         half roughness, half3 F0, half3 lightColor, half atten)
{
    half3 H = normalize(viewDir + lightDir);
    half NdotH = max(0.001, dot(normal, H));
    half NdotL = max(0.001, dot(normal, lightDir));
    half NdotV = max(0.001, dot(normal, viewDir));
    half VdotH = max(0.001, dot(viewDir, H));

    half D = NataneGGX_D(NdotH, roughness);
    half G = NataneGGX_G(NdotL, NdotV, roughness);
    half3 F = NataneSchlickFresnel(VdotH, F0);

    half3 spec = D * G * F;
    return spec * lightColor * atten * NdotL;
}

// Indirect Specular from Unity Reflection Probes
half3 NatanePBRIndirectSpecular(half3 worldNormal, half3 viewDir, half3 worldPos,
                                half roughness, half3 F0)
{
    half3 reflDir = reflect(-viewDir, worldNormal);
    half mipLevel = roughness * 7.0;

    // Box projection for Reflection Probe 0 (if enabled)
    #if UNITY_SPECCUBE_BOX_PROJECTION
        half3 reflDir0 = BoxProjectedCubemapDirection(reflDir, worldPos,
            unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    #else
        half3 reflDir0 = reflDir;
    #endif

    half4 envSample = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflDir0, mipLevel);
    half3 envColor = DecodeHDR(envSample, unity_SpecCube0_HDR);

    // Approximate environment BRDF (Karis 2014 split-sum approximation)
    half NdotV = max(0.001, dot(worldNormal, viewDir));
    half3 F = NataneSchlickFresnel(NdotV, F0);
    half surfaceReduction = 1.0 / (roughness * roughness + 1.0);
    half grazingTerm = saturate((1.0 - roughness) + max(F0.r, max(F0.g, F0.b)));
    half3 indirectF = lerp(F, half3(grazingTerm, grazingTerm, grazingTerm),
                           pow(1.0 - NdotV, 5.0));

    return envColor * indirectF * surfaceReduction;
}

#endif
