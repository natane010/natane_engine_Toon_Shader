#ifndef NATANE_TOON_THIRD_PARTY_LIGHTING_INCLUDED
#define NATANE_TOON_THIRD_PARTY_LIGHTING_INCLUDED

// =============================================================================
// Third-Party Lighting Integration (Auto-detected)
// Config files determine whether to use package, bundled, or disabled integrations
// =============================================================================

// --- VRC Light Volumes ---
#include "../Config/NataneToonLVConfig.hlsl"

// --- LTCGI (Realtime Area Lights) ---
#include "../Config/NataneToonLTCGIConfig.hlsl"

#if defined(_LTCGI) || defined(NATANE_FORCE_LTCGI_HELPERS)
    #if defined(NATANE_LTCGI_AVAILABLE)
        // Match lilToon's LTCGI path: custom callbacks + package contribution.
        float3 NataneNearestPointOnLine(float3 p, float3 pos1, float3 pos2)
        {
            float3 v = pos2 - pos1;
            float3 w = p - pos1;
            float c1 = dot(w, v);
            float c2 = dot(v, v);

            if (c1 <= 0.0) return pos1;
            if (c2 <= c1) return pos2;

            float b = c1 / max(c2, 1e-8);
            return pos1 + b * v;
        }

        float3 NataneNearestPointOnQuad(float3 p, float3 v[4])
        {
            float3 v1 = NataneNearestPointOnLine(p, v[0], v[1]);
            float3 v2 = NataneNearestPointOnLine(p, v[2], v[3]);
            return NataneNearestPointOnLine(p, v1, v2);
        }

        float3 NataneSafeNormalize(float3 value)
        {
            return value * rsqrt(max(dot(value, value), 1e-8));
        }

        #define Sample(smp,uv) SampleLevel(smp,uv,0)
        #include "Packages/at.pimaker.ltcgi/Shaders/LTCGI_structs.cginc"

        struct NataneLTCGIData
        {
            float3 diffuse;
            float3 specular;
            float3 lightDirection;
            float3 normalWS;
            float3 viewDirectionWS;
        };

        float NataneLTCGIAttenuation(inout NataneLTCGIData ltcgi, in ltcgi_output output)
        {
            float3 center = (output.input.Lw[0] + output.input.Lw[1] + output.input.Lw[2] + output.input.Lw[3]) * 0.25;
            float3 nearest = NataneNearestPointOnQuad(float3(0, 0, 0), output.input.Lw);
            float3 quadNormal = NataneSafeNormalize(cross(output.input.Lw[1] - output.input.Lw[0], output.input.Lw[3] - output.input.Lw[0]));
            float3 lightDir = NataneSafeNormalize(lerp(nearest, center, 0.3));
            float dist = lerp(length(nearest), length(center), 0.3) + 1.0;
            float rim = saturate(1.0 - abs(dot(ltcgi.normalWS, ltcgi.viewDirectionWS)) * 2.0 + dot(ltcgi.normalWS, lightDir));
            float atten = abs(dot(quadNormal, NataneSafeNormalize(nearest))) / pow(dist, 3.0);
            atten *= lerp(rim, 1.0, saturate(dot(ltcgi.viewDirectionWS, lightDir) * 0.5 + 0.5));
            atten = saturate(atten);
            ltcgi.lightDirection += atten * dot(output.color, 0.333333) * lightDir;
            return atten;
        }

        void NataneLTCGIDiffuseCallback(inout NataneLTCGIData ltcgi, in ltcgi_output output)
        {
            float atten = NataneLTCGIAttenuation(ltcgi, output);
            ltcgi.diffuse += atten * output.color * 2.0;
        }

        void NataneLTCGISpecularCallback(inout NataneLTCGIData ltcgi, in ltcgi_output output)
        {
            float atten = NataneLTCGIAttenuation(ltcgi, output);
            ltcgi.specular += atten * output.color;
        }

        #define LTCGI_V2_CUSTOM_INPUT NataneLTCGIData
        #define LTCGI_V2_DIFFUSE_CALLBACK NataneLTCGIDiffuseCallback
        #define LTCGI_V2_SPECULAR_CALLBACK NataneLTCGISpecularCallback
        #include "Packages/at.pimaker.ltcgi/Shaders/LTCGI.cginc"

        void NataneLTCGIContribution(float3 worldPos, float3 worldNormal, float3 viewDir,
            float roughness, float2 uv1,
            out float3 diffuse, out float3 specular)
        {
            NataneLTCGIData ltcgi = (NataneLTCGIData)0;
            ltcgi.normalWS = worldNormal;
            ltcgi.viewDirectionWS = viewDir;
            LTCGI_Contribution(ltcgi, worldPos, worldNormal, viewDir, 1, uv1);
            diffuse = ltcgi.diffuse;
            specular = ltcgi.specular;
        }

        #undef LTCGI_V2_CUSTOM_INPUT
        #undef LTCGI_V2_DIFFUSE_CALLBACK
        #undef LTCGI_V2_SPECULAR_CALLBACK
        #undef Sample
    #else
        // Package not installed: disable LTCGI entirely (matches lilToon behavior).
        // Previously this used SH + Reflection Probe fallback, but that was misleading
        // as it did not represent real LTCGI contribution.
        void NataneLTCGIContribution(float3 worldPos, float3 worldNormal, float3 viewDir,
            float roughness, float2 uv1,
            out float3 diffuse, out float3 specular)
        {
            diffuse = 0;
            specular = 0;
        }
    #endif
#endif

#if defined(_USE_LIGHT_VOLUME) || defined(NATANE_FORCE_LIGHTVOLUME_HELPERS)
    // VRC Light Volumes Integration (lilToon-style bundled approach)
    // パッケージ版を優先、未インストール時はバンドル版を使用
    // バンドル版は RED_SIM 氏の MIT License に基づく同梱
    // See: ThirdParty/VRCLightVolumes/LICENSE.md
    //
    // LightVolumes.cginc は内部で _UdonLightVolumeEnabled == 0 の場合に
    // Unity Light Probes へ自動フォールバックするため、非VRChat環境でも安全
    #if defined(NATANE_VRCLV_AVAILABLE)
        #include "Packages/red.sim.lightvolumes/Shaders/LightVolumes.cginc"
    #else
        #include "../../ThirdParty/VRCLightVolumes/LightVolumes.cginc"
    #endif
#endif

#endif
