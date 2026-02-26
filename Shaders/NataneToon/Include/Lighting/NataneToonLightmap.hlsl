#ifndef NATANE_TOON_LIGHTMAP_INCLUDED
#define NATANE_TOON_LIGHTMAP_INCLUDED

#ifdef _BACKGROUND_MODE

half3 SampleNataneLightmap(float2 lightmapUV, half3 worldNormal)
{
    #ifdef DIRLIGHTMAP_COMBINED
        half4 lightmapDir = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, lightmapUV);
        half4 lightmapData = UNITY_SAMPLE_TEX2D(unity_Lightmap, lightmapUV);
        return DecodeDirectionalLightmap(DecodeLightmap(lightmapData), lightmapDir, worldNormal);
    #elif defined(LIGHTMAP_ON)
        return DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, lightmapUV));
    #else
        return half3(0, 0, 0);
    #endif
}

#endif // _BACKGROUND_MODE
#endif
