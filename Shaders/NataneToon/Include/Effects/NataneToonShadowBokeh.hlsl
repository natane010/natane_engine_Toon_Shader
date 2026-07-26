// NataneToonShadowBokeh.hlsl
// Shadow Bokeh / 影の玉ボケ（木漏れ日）.
//
// 影の領域に、木漏れ日のような柔らかい円形の光斑を落とす。座標は
// NataneLightPlaneCoord で光源方向に垂直な平面へ投影するため、カメラを
// 動かしても光斑が面に貼り付いたままになり、ライトを回すと追従する。
//
// パターンはセルグリッドで、セルごとにハッシュで中心・半径・明るさを散らす。
// 重なりは max() で合成する。加算だと重なった箇所が飽和して玉に見えなくなる。
// Quest では _QUEST_LITE 側の Lite 版（3x3 走査をやめた近似）を使う。
//
// ここは純粋関数のみ。テクスチャのサンプリングと合成はフラグメント側で行う。
#ifndef NATANE_TOON_SHADOW_BOKEH_INCLUDED
#define NATANE_TOON_SHADOW_BOKEH_INCLUDED

// セル座標 -> [0,1) の2値。中心のジッターに使う。
float2 NataneShadowBokeh_Hash2(float2 p)
{
    return NataneHash22(p, float2(127.1, 311.7), float2(269.5, 183.3));
}

// セル座標 -> [0,1)。半径と明るさの散らしに使う。
float NataneShadowBokeh_Hash1(float2 p)
{
    return NataneHash21(p, float2(41.37, 289.51));
}

// 円 or 多角形（絞り羽根風）の距離。
// blades が 3 未満なら円。角度方向の変調で近似しており、追加サンプルは要らない。
float NataneShadowBokeh_ShapeDistance(float2 delta, float blades)
{
    float d = length(delta);
    if (blades < 2.5) return d;

    // 正多角形の内接距離への補正。角度に対する周期的な膨らみを掛ける。
    float a = atan2(delta.y, delta.x);
    float seg = 6.2831853 / blades;
    float wedge = abs(frac(a / seg + 0.5) - 0.5) * seg;
    return d * cos(wedge) / max(cos(seg * 0.5), 1e-4);
}

// 玉ボケパターン [0,1]。3x3 セル走査。
//
// size      : セルに対する玉の半径（0-1 付近）
// softness  : 縁のぼけ幅（0 で硬い円、1 でほぼグラデーション）
// blades    : 絞り羽根の枚数。2 以下で円
// rimGain   : 外周の持ち上げ量。レンズの玉ボケらしい縁の明るさを作る
float NataneShadowBokehPattern(float2 coord, float size, float softness, float blades, float rimGain)
{
    float2 cell = floor(coord);
    float2 f = coord - cell;

    float result = 0.0;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y);
            float2 id = cell + offset;

            // セルごとに中心をずらす。端に寄せすぎると隣と重なって粒が潰れるので 0.2-0.8 に収める。
            float2 jitter = 0.2 + 0.6 * NataneShadowBokeh_Hash2(id);
            float h = NataneShadowBokeh_Hash1(id);

            // 半径と明るさを散らす。全部同じ大きさだと機械的な水玉に見える。
            float radius = size * (0.55 + 0.45 * h);
            float brightness = 0.45 + 0.55 * frac(h * 7.13);

            float d = NataneShadowBokeh_ShapeDistance(offset + jitter - f, blades);

            float edge = max(radius * (1.0 - saturate(softness)), 1e-4);
            float disc = 1.0 - smoothstep(edge, radius, d);

            // 外周をわずかに持ち上げる。玉ボケは縁が明るく見える。
            float rim = saturate(1.0 - abs(d - radius * 0.82) / max(radius * 0.35, 1e-4));
            disc = saturate(disc + rim * rimGain * disc);

            result = max(result, disc * brightness);
        }
    }

    return saturate(result);
}

// Quest 向けの軽量版。3x3 のセル走査をやめ、交差する sin 場で粒を近似する。
// 粒の散らばりは劣るが、命令数は大幅に少ない。
float NataneShadowBokehPatternLite(float2 coord, float size, float softness)
{
    float2 cell = floor(coord);
    float2 f = coord - cell;

    float2 jitter = 0.25 + 0.5 * NataneShadowBokeh_Hash2(cell);
    float h = NataneShadowBokeh_Hash1(cell);

    float radius = size * (0.6 + 0.4 * h);
    float d = length(jitter - f);

    float edge = max(radius * (1.0 - saturate(softness)), 1e-4);
    float disc = 1.0 - smoothstep(edge, radius, d);

    return saturate(disc * (0.5 + 0.5 * h));
}

#endif // NATANE_TOON_SHADOW_BOKEH_INCLUDED
