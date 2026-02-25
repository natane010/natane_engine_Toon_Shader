// NataneToonFurShell.hlsl - Shell-based fur vertex/fragment shader
// Called from NataneToonShader_Fur.shader with FUR_SHELL_INDEX defined per pass

#ifndef NATANE_TOON_FUR_SHELL_INCLUDED
#define NATANE_TOON_FUR_SHELL_INCLUDED

#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Lighting.cginc"

// Shell parameters
#define FUR_SHELL_COUNT 16
#define FUR_LAYER ((float)FUR_SHELL_INDEX / (float)(FUR_SHELL_COUNT - 1))

struct appdata_fur {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f_fur {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float furLayer : TEXCOORD3;
    UNITY_FOG_COORDS(4)
    UNITY_VERTEX_OUTPUT_STEREO
};

// Shared samplers and variables (from NataneToonInput.hlsl via the .shader CBUFFER)
// These are declared in the .shader Pass block that includes this file

v2f_fur furVert(appdata_fur v)
{
    v2f_fur o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float layer = FUR_LAYER;
    o.furLayer = layer;
    o.uv = v.uv;

    // Shell offset along normal
    float3 offset = v.normal * layer * _FurLength;

    // Gravity (quadratic falloff for natural droop)
    offset.y -= _FurGravity * layer * layer;

    // Wind animation
    float windPhase = _Time.y * _FurWindSpeed;
    float3 windOffset = _FurWindDirection.xyz * sin(windPhase + v.vertex.x * 2.0) * _FurWindStrength * layer;
    offset += windOffset;

    // Apply mask - scale shell offset by mask value
    float mask = tex2Dlod(_FurMask, float4(v.uv, 0, 0)).r;
    offset *= mask;

    float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz + offset, 1.0));
    o.worldPos = worldPos.xyz;
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.pos = UnityWorldToClipPos(worldPos);

    UNITY_TRANSFER_FOG(o, o.pos);
    return o;
}

fixed4 furFrag(v2f_fur i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float layer = i.furLayer;

    // Distance-based LOD: fade out upper shells at distance
    float camDist = distance(i.worldPos, _WorldSpaceCameraPos);
    float lodFade = saturate(1.0 - (camDist - _FurLODDistance) / max(_FurLODDistance, 0.001));
    // Upper layers get clipped first at distance
    float lodThreshold = lerp(_FurLODMinLayers / (float)FUR_SHELL_COUNT, 1.0, lodFade);
    clip(lodThreshold - layer - 0.001);

    // Noise-based alpha cutoff
    float2 furUV = TRANSFORM_TEX(i.uv, _FurNoiseTex) * _FurDensity;
    float furNoise = tex2D(_FurNoiseTex, furUV).r;

    // Height-based alpha: upper layers have less fur
    float alpha = furNoise * (1.0 - layer);
    clip(alpha - _FurAlphaCutoff);

    // Base color from main texture
    float4 mainColor = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));

    // Root-to-tip color gradient
    float3 furRootColor = _FurRootColor.rgb;
    float3 furTipColor = _FurTipColor.rgb;
    float3 furColor = lerp(furRootColor, furTipColor, layer);

    // Blend fur color with main texture
    float3 finalColor = lerp(mainColor.rgb, furColor, _FurColorBlend);

    // Self-occlusion (AO): root is darker, tip is brighter
    float ao = lerp(1.0 - _FurAO, 1.0, layer);
    finalColor *= ao;

    // Simple toon lighting
    float3 worldNormal = normalize(i.worldNormal);
    float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
    float NdotL = dot(worldNormal, lightDir);
    float toonShading = smoothstep(-0.1, 0.3, NdotL);

    // Self-shadow: inner layers receive less light
    float selfShadow = lerp(1.0 - _FurShadowStrength, 1.0, layer * 0.7 + 0.3);
    toonShading *= selfShadow;

    // Apply lighting
    float3 lightColor = _LightColor0.rgb;
    float3 ambient = ShadeSH9(float4(worldNormal, 1.0));
    finalColor *= (lightColor * toonShading + ambient);

    // Specular highlight on fur tips
    if (_FurSpecular > 0.001)
    {
        float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
        float3 halfDir = normalize(lightDir + viewDir);
        float spec = pow(max(0, dot(worldNormal, halfDir)), 40.0) * _FurSpecular * layer;
        finalColor += lightColor * spec;
    }

    // Rim light
    if (_FurRimLight > 0.001)
    {
        float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
        float rim = 1.0 - saturate(dot(viewDir, worldNormal));
        rim = pow(rim, 3.0) * _FurRimLight * layer;
        finalColor += lightColor * rim;
    }

    fixed4 col = fixed4(finalColor, alpha);
    UNITY_APPLY_FOG(i.fogCoord, col);
    return col;
}

#endif // NATANE_TOON_FUR_SHELL_INCLUDED
