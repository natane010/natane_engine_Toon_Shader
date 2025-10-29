#ifndef NATANE_TOON_CORE_INCLUDED
#define NATANE_TOON_CORE_INCLUDED

// Properties
sampler2D _MainTex;
float4 _MainTex_ST;
fixed4 _Color;

// Shading
sampler2D _RampTex;
fixed4 _ShadowColor;
float _ShadowSteps;
float _ShadowSharpness;
float _ShadowOffset;

// Specular
fixed4 _SpecularColor;
float _SpecularSize;
float _SpecularSoftness;

// Rim Light
fixed4 _RimColor;
float _RimPower;
float _RimIntensity;

// MatCap
sampler2D _MatCapTex;
float _MatCapIntensity;
float _MatCapBlendMode;

// Emission
fixed4 _EmissionColor;
sampler2D _EmissionMap;

// Normal Map
sampler2D _BumpMap;
float _BumpScale;

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float3 worldTangent : TEXCOORD3;
    float3 worldBinormal : TEXCOORD4;
    UNITY_FOG_COORDS(5)
    SHADOW_COORDS(6)
};

// Calculate MatCap UV
float2 CalculateMatCapUV(float3 worldNormal, float3 viewDir)
{
    float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
    float2 matcapUV = viewNormal.xy * 0.5 + 0.5;
    return matcapUV;
}

// Toon Shading with steps
float ToonShading(float ndotl, float steps, float sharpness)
{
    // Apply shadow offset
    ndotl = ndotl + _ShadowOffset;

    // Calculate stepped lighting
    float toon = floor(ndotl * steps) / steps;

    // Smooth the edges based on sharpness
    float edge = frac(ndotl * steps);
    toon += smoothstep(0.5 - sharpness * 0.5, 0.5 + sharpness * 0.5, edge) / steps;

    return saturate(toon);
}

// Ramp Texture Shading
float3 RampShading(float ndotl)
{
    float2 rampUV = float2(saturate(ndotl + _ShadowOffset), 0.5);
    return tex2D(_RampTex, rampUV).rgb;
}

// Specular Highlight (Anime Style)
float SpecularHighlight(float3 normal, float3 viewDir, float3 lightDir, float size, float softness)
{
    float3 halfVector = normalize(lightDir + viewDir);
    float ndoth = max(0, dot(normal, halfVector));

    // Create sharp specular
    float spec = smoothstep(1.0 - size - softness, 1.0 - size + softness, ndoth);
    return spec;
}

// Rim Light Calculation
float3 RimLighting(float3 normal, float3 viewDir, float power, float intensity)
{
    float rim = 1.0 - saturate(dot(normal, viewDir));
    rim = pow(rim, power) * intensity;
    return rim * _RimColor.rgb;
}

// Vertex Shader
v2f vert(appdata v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

    // Transform normal and tangent to world space
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
    o.worldBinormal = cross(o.worldNormal, o.worldTangent) * v.tangent.w;

    UNITY_TRANSFER_FOG(o, o.pos);
    TRANSFER_SHADOW(o);

    return o;
}

// Fragment Shader
fixed4 frag(v2f i) : SV_Target
{
    // Sample textures
    fixed4 mainTex = tex2D(_MainTex, i.uv);
    fixed4 col = mainTex * _Color;

    // Normal mapping
    float3 worldNormal = i.worldNormal;
    #ifdef _NORMALMAP
        float3 normalMap = UnpackScaleNormal(tex2D(_BumpMap, i.uv), _BumpScale);
        float3x3 tangentToWorld = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);
        worldNormal = normalize(mul(normalMap, tangentToWorld));
    #endif

    // Lighting setup
    float3 lightDir;
    float atten;

    #ifdef USING_DIRECTIONAL_LIGHT
        lightDir = normalize(_WorldSpaceLightPos0.xyz);
        atten = SHADOW_ATTENUATION(i);
    #else
        float3 lightVec = _WorldSpaceLightPos0.xyz - i.worldPos;
        lightDir = normalize(lightVec);
        atten = SHADOW_ATTENUATION(i);

        // Point/Spot light attenuation
        float distSqr = dot(lightVec, lightVec);
        atten *= 1.0 / (1.0 + distSqr * 0.1);
    #endif

    float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
    float ndotl = max(0, dot(worldNormal, lightDir));

    // Toon Shading or Ramp
    float3 lighting;
    #ifdef _USE_RAMP
        lighting = RampShading(ndotl * atten);
    #else
        float toon = ToonShading(ndotl * atten, _ShadowSteps, _ShadowSharpness);
        lighting = lerp(_ShadowColor.rgb, float3(1, 1, 1), toon);
    #endif

    // Apply light color
    lighting *= _LightColor0.rgb;

    // Add ambient lighting (only in base pass)
    #ifdef UNITY_PASS_FORWARDBASE
        lighting += ShadeSH9(float4(worldNormal, 1.0));
    #endif

    // Apply lighting to color
    col.rgb *= lighting;

    // Specular Highlight
    #ifdef _SPECULAR
        float spec = SpecularHighlight(worldNormal, viewDir, lightDir, _SpecularSize, _SpecularSoftness);
        col.rgb += spec * _SpecularColor.rgb * _LightColor0.rgb * atten;
    #endif

    // Rim Light (only in base pass)
    #if defined(_RIM_LIGHT) && defined(UNITY_PASS_FORWARDBASE)
        float3 rim = RimLighting(worldNormal, viewDir, _RimPower, _RimIntensity);
        col.rgb += rim;
    #endif

    // MatCap (only in base pass)
    #if defined(_MATCAP) && defined(UNITY_PASS_FORWARDBASE)
        float2 matcapUV = CalculateMatCapUV(worldNormal, viewDir);
        fixed3 matcap = tex2D(_MatCapTex, matcapUV).rgb * _MatCapIntensity;

        // Blend modes
        if (_MatCapBlendMode < 0.5) // Add
            col.rgb += matcap;
        else if (_MatCapBlendMode < 1.5) // Multiply
            col.rgb *= matcap;
        else // Replace
            col.rgb = lerp(col.rgb, matcap, _MatCapIntensity);
    #endif

    // Emission (only in base pass)
    #if defined(_EMISSION) && defined(UNITY_PASS_FORWARDBASE)
        fixed3 emission = tex2D(_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
        col.rgb += emission;
    #endif

    // Apply fog
    UNITY_APPLY_FOG(i.fogCoord, col);

    return col;
}

#endif // NATANE_TOON_CORE_INCLUDED
