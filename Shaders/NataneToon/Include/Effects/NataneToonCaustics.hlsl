// NataneToonCaustics.hlsl
// Surface Caustics (サーフェス・コースティクス).
//
// Flowing light patterns across the surface. Procedural mode uses a single
// 3x3-cell Voronoi F1 with a sin warp (cheap, no multi-octave loop); texture
// mode multiplies two scrolling samples (done in the fragment because
// _CausticsTex is NOSAMPLER). All helpers here are pure. On Quest the fragment
// forces the sin-only lite path via _QUEST_LITE (see NataneCausticsPatternLite).
#ifndef NATANE_TOON_CAUSTICS_INCLUDED
#define NATANE_TOON_CAUSTICS_INCLUDED

float2 NataneCaustics_Hash2(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

// Animated Voronoi F1 distance (single 3x3 cell scan). Returns the distance to
// the nearest jittered feature point in [0, ~1].
float NataneCaustics_Voronoi(float2 uv, float time)
{
    float2 g = floor(uv);
    float2 f = frac(uv);
    float md = 8.0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 o = float2(x, y);
            float2 r = NataneCaustics_Hash2(g + o);
            r = 0.5 + 0.5 * sin(time + 6.2831853 * r); // animate the feature point
            float2 diff = o + r - f;
            md = min(md, dot(diff, diff));
        }
    }
    return sqrt(md);
}

// Full-quality procedural caustic pattern in [0,1] (bright thin ridges).
float NataneCausticsPattern(float2 uv, float time, float distortion)
{
    float2 w = float2(sin(uv.y * 3.0 + time), cos(uv.x * 3.0 + time)) * distortion;
    float v = NataneCaustics_Voronoi(uv + w, time);
    return saturate(1.0 - v);
}

// Cheap Quest fallback: crossed sin fields, no cell scan.
float NataneCausticsPatternLite(float2 uv, float time, float distortion)
{
    float2 w = float2(sin(uv.y * 2.0 + time), cos(uv.x * 2.0 + time)) * distortion;
    float2 p = uv + w;
    float a = sin(p.x * 6.2831853 + time) * sin(p.y * 6.2831853 + time * 1.3);
    float b = sin((p.x + p.y) * 4.0 - time * 0.7);
    return saturate(abs(a) * 0.6 + abs(b) * 0.4);
}

// Build the 2D sampling coordinate from a coordinate-space selector.
// space: 0 UV, 1 Object, 2 World, 3 Triplanar-lite (dominant world axis plane).
float2 NataneCausticsCoord(float space, float2 uv, float3 objPos, float3 worldPos, float3 worldNormal)
{
    if (space < 0.5) return uv;
    if (space < 1.5) return objPos.xy;
    if (space < 2.5) return worldPos.xz;
    // Triplanar-lite: pick the plane facing away from the dominant normal axis.
    float3 an = abs(worldNormal);
    if (an.y >= an.x && an.y >= an.z) return worldPos.xz;
    if (an.x >= an.z)                 return worldPos.zy;
    return worldPos.xy;
}

#endif // NATANE_TOON_CAUSTICS_INCLUDED
