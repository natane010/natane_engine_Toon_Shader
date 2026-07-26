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

#define NATANE_HALFTONE_SPACE_SCREEN 0
#define NATANE_HALFTONE_SPACE_WORLD  1
#define NATANE_HALFTONE_SPACE_UV     2
#define NATANE_HALFTONE_SPACE_OBJECT 3

/// 網点グリッドの座標を作る。
///
/// スクリーン空間は「セル1つあたりのピクセル数」、面に貼り付ける空間は
/// 「単位あたりのセル数」と、本来スケールの意味が逆になる。ここで吸収して
/// 「scale を上げるほど網点が粗くなる」という向きに揃える。
///
/// ワールドは Triplanar-lite（支配的な法線軸に正対する平面を選ぶ）を使う。
/// XZ 平面固定にすると壁やキャラの体のような垂直面で投影が潰れ、
/// 網点が線状に伸びてしまう。
float2 NataneHalftoneCoord(float space, float scale, float surfaceDensity,
                           float2 uv, float3 objPos, float3 worldPos, float3 worldNormal,
                           float2 screenPos)
{
    int mode = (int)(space + 0.5);
    float safeScale = max(scale, 1e-3);

    if (mode == NATANE_HALFTONE_SPACE_WORLD)
    {
        // NataneProjectionCoord のモード 3 = Triplanar-lite
        float2 base = NataneProjectionCoord(3.0, uv, objPos, worldPos, worldNormal);
        return base * (surfaceDensity * (30.0 / safeScale));
    }

    if (mode == NATANE_HALFTONE_SPACE_UV)
    {
        return uv * (surfaceDensity * (30.0 / safeScale));
    }

    if (mode == NATANE_HALFTONE_SPACE_OBJECT)
    {
        // モード 1 = Object。スキニングの影響を受けないので、
        // アニメーションしても網点が張り付いたままになる。
        float2 base = NataneProjectionCoord(1.0, uv, objPos, worldPos, worldNormal);
        return base * (surfaceDensity * (30.0 / safeScale));
    }

    return screenPos / safeScale;
}

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
//
// averageInk はそのパターンが 1 セルを平均どれだけ塗るか（＝そのトーンの濃度）。
// 網点が画面上で 1px 未満に潰れる状況（遠景・浅い角度・ワールド投影の圧縮）では、
// 無理に点を描こうとするとグレーのノイズになる。解像できなくなるにつれて
// 平均インク量へフェードさせることで、遠くでは滑らかな均一トーンに落ち着く。
// 写真製版でモアレを避けるのと同じ考え方。
//
// aaScale はエッジのなじませ幅だけを制御し、この解像判定には影響させない。
// aaScale=0（硬いエッジ）にしても崩れないようにするため。
float NataneHalftoneCoverage(float distanceField, float radius, float averageInk, float aaScale)
{
    float derivative = max(fwidth(distanceField), 1e-6);

    float w = derivative * max(aaScale, 0.0);
    float sharp = 1.0 - smoothstep(radius - w, radius + w, distanceField);

    // セル1つが微分値に対して十分大きいときだけ網点として描く。
    float resolve = saturate(1.0 - derivative / max(radius, 1e-4));

    return lerp(averageInk, sharp, resolve);
}

// ドット網点。tone が濃いほど点が太る。
// 1セルあたりの平均インク量は円の面積そのもの。
float NataneHalftoneDot(float2 coord, float radius, float aaScale)
{
    float2 cell = frac(coord) - 0.5;
    float averageInk = saturate(3.14159265 * radius * radius);
    return NataneHalftoneCoverage(length(cell), radius, averageInk, aaScale);
}

// 万線（平行線）。tone が濃いほど線が太る。
// 平均インク量は線幅の占有率。
float NataneHalftoneLine(float coordAxis, float halfWidth, float aaScale)
{
    float d = abs(frac(coordAxis) - 0.5);
    float averageInk = saturate(halfWidth * 2.0);
    return NataneHalftoneCoverage(d, halfWidth, averageInk, aaScale);
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
