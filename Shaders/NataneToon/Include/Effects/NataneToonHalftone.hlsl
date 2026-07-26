// NataneToonHalftone.hlsl
// Manga halftone / 漫画網点.
//
// 影の濃さを離散的な「トーンの号数」に量子化し、その段階に応じて網点の粒度を
// 変える。実物のスクリーントーンが 10%・20%・30% と決まった濃度で売られている
// のと同じ考え方で、連続的に太らせるより漫画らしくなる。
//
// 網点はドット / 万線 / クロスハッチを選べ、角度を付けられる（漫画の網点は
// 45° が基本）。エッジは画面微分でアンチエイリアスするので、拡大しても
// 階段状にならない。
//
// ここは純粋関数のみ。座標の作り方と色の合成はフラグメント側で行う。
#ifndef NATANE_TOON_HALFTONE_INCLUDED
#define NATANE_TOON_HALFTONE_INCLUDED

#define NATANE_HALFTONE_DOT        0
#define NATANE_HALFTONE_LINE       1
#define NATANE_HALFTONE_CROSSHATCH 2

// 影の濃さを段階に量子化する。levels <= 1 なら量子化しない（連続）。
// 段の中央値を返すので、最も薄い段でも網点が消えず、最も濃い段でも潰れない。
float NataneHalftoneQuantizeTone(float tone, float levels)
{
    if (levels < 1.5) return tone;
    float n = floor(levels);
    return saturate((floor(saturate(tone) * n) + 0.5) / n);
}

// 網点グリッドの回転。漫画の網点は 45° に振るのが基本。
float2 NataneHalftoneRotate(float2 p, float angleDegrees)
{
    float a = radians(angleDegrees);
    float s, c;
    sincos(a, s, c);
    return float2(p.x * c - p.y * s, p.x * s + p.y * c);
}

// 距離場をアンチエイリアスして「インク量」に変換する。
// aaScale=0 で硬いエッジ、1 で 1px 相当。fwidth を使うので距離に依らず一定幅。
float NataneHalftoneCoverage(float distance, float radius, float aaScale)
{
    float w = max(fwidth(distance) * aaScale, 1e-5);
    return 1.0 - smoothstep(radius - w, radius + w, distance);
}

// ドット網点。tone が濃いほど点が太る。
float NataneHalftoneDot(float2 coord, float radius, float aaScale)
{
    float2 cell = frac(coord) - 0.5;
    return NataneHalftoneCoverage(length(cell), radius, aaScale);
}

// 万線（平行線）。tone が濃いほど線が太る。
float NataneHalftoneLine(float coordAxis, float halfWidth, float aaScale)
{
    float d = abs(frac(coordAxis) - 0.5);
    return NataneHalftoneCoverage(d, halfWidth, aaScale);
}

/// 網点パターンを評価してインク量 [0,1] を返す。
///
/// coord    : 網点グリッド座標（スケールと回転を適用済み）
/// tone     : 影の濃さ [0,1]。0=塗らない、1=最も濃い
/// pattern  : 0 ドット / 1 万線 / 2 クロスハッチ
/// dotMin   : tone=0 付近での網点サイズ
/// dotMax   : tone=1 での網点サイズ
/// aaScale  : アンチエイリアス幅の倍率
float NataneHalftonePattern(float2 coord, float tone, float pattern,
                            float dotMin, float dotMax, float aaScale)
{
    float size = lerp(dotMin, dotMax, saturate(tone)) * 0.5;
    int mode = (int)(pattern + 0.5);

    if (mode == NATANE_HALFTONE_LINE)
    {
        return NataneHalftoneLine(coord.x, size, aaScale);
    }

    if (mode == NATANE_HALFTONE_CROSSHATCH)
    {
        // 薄いうちは一方向だけ、濃くなると直交方向が重なる。
        // 実際の漫画のカケアミもこの順で密度を上げる。
        float first = NataneHalftoneLine(coord.x, size, aaScale);
        float secondTone = saturate(tone * 2.0 - 1.0);
        float secondSize = lerp(dotMin, dotMax, secondTone) * 0.5;
        float second = NataneHalftoneLine(coord.y, secondSize, aaScale) * step(0.001, secondTone);
        return max(first, second);
    }

    return NataneHalftoneDot(coord, size, aaScale);
}

#endif // NATANE_TOON_HALFTONE_INCLUDED
