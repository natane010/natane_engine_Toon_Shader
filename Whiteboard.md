# Natane Toon Shader - 開発進捗Whiteboard

---

## lilToon完全移行調査 (liltoon-migration-research team)

### 目的
lilToon → Natane Toon Shader の移行で、見た目を変えずに完全移行する方法を確立する。

### 現在判明している問題
1. **MatCap白飛び**: lilToonのNormal(0)=lerp合成をNataneのAdd(0)=加算合成にマッピング → 白飛び
2. **全体的な暗さ**: normalize+luminanceステップ、Half-Lambert vs raw NdotL の差異
3. **彩度低下**: normalizeが色を均一化する副作用
4. **アウトライン太さ**: 内部スケール10倍差（修正済み）
5. **シャドウ形状差**: Half-Lambert vs raw NdotL のトーンカーブ差異

### 調査タスク

| # | タスク | 担当 | Status |
|---|--------|------|--------|
| 1 | lilToonシェーダーパイプライン完全解析 | researcher-liltoon | DONE |
| 2 | Nataneシェーダーパイプライン完全解析 | researcher-natane | |
| 3 | パイプライン差異のGap分析＋修正プラン策定 | planner | |

### 調査結果 (各エージェントが記入)

#### lilToon パイプライン解析結果
**調査者**: researcher-liltoon / **完了日**: 2026-02-27
**ソース**: [lilxyzw/lilToon GitHub](https://github.com/lilxyzw/lilToon) (v2.1.2, master branch)

---

##### 1. ライティングパイプライン全体フロー

**ファイル構成** (Include階層):
```
lts.shader (Properties + Pass定義)
└── lil_common_frag.hlsl (メインフラグメントパイプライン)
    ├── lil_common_input.hlsl (プロパティ宣言・CBUFFER)
    ├── lil_common_functions.hlsl (ユーティリティ関数)
    ├── lil_common_macro.hlsl (パイプライン抽象化マクロ)
    └── openlit_core.hlsl (ライティングコア・SH計算)
```

**パイプライン処理順序**:
```
1. Vertex Stage → 位置変換、法線/接線計算
2. Fragment Stage:
   a. テクスチャサンプリング (MainTex, 2nd/3rd layers)
   b. アルファ処理 (Alpha Mask, Dissolve, Cutout)
   c. ★ライティング計算 (lilGetShading)
   d. ★MatCap適用
   e. ★リムライト適用
   f. ★エミッション適用
   g. フォグ適用
   h. SV_Target出力
```

**NdotL計算 (Half-Lambert)**:
```hlsl
// lil_common_frag.hlsl 内 lilGetShading()
float aastrength = _AAStrength;
float4 lns = 1.0;

// ★ Half-Lambert: ndotl * 0.5 + 0.5 (範囲: [0.0, 1.0])
// 前面(ndotl>=0): halfLambert ∈ [0.5, 1.0]
// 裏面(ndotl<0):  halfLambert ∈ [0.0, 0.5)
lns.x = saturate(dot(fd.L, N1) * 0.5 + 0.5);  // 1st shadow用
lns.y = saturate(dot(fd.L, N2) * 0.5 + 0.5);  // 2nd shadow用 (カスタム法線対応)
```

**ライトカラー処理 (LIL_CORRECT_LIGHTCOLOR)**:
```hlsl
// lil_common_macro.hlsl
#define LIL_CORRECT_LIGHTCOLOR_PS(lightColor) \
    lightColor = clamp(lightColor, _LightMinLimit, _LightMaxLimit); \
    lightColor = lerp(lightColor, lilGray(lightColor), _MonochromeLighting); \
    lightColor = lerp(lightColor, 1.0, _AsUnlit)

// 処理順序:
// 1. clamp: _LightMinLimit(0.05) ～ _LightMaxLimit(1.0) でクランプ
// 2. モノクロ: _MonochromeLighting で彩度を除去（グレースケール化）
// 3. アンライト: _AsUnlit=1.0 で lightColor=1.0 (完全アンライト)
```

**SH(環境光)計算**:
```hlsl
// openlit_core.hlsl
void ShadeSH9ToonDouble(float3 V, out float3 shMax, out float3 shMin)
{
    float3 N = V;
    float4 vB = N.xyzz * N.yzzx;
    // L2 SH
    float3 res = float3(olSHAr.w, olSHAg.w, olSHAb.w);
    res.r += dot(olSHBr, vB);
    res.g += dot(olSHBg, vB);
    res.b += dot(olSHBb, vB);
    res += olSHC.rgb * (N.x * N.x - N.y * N.y);
    // L1 SH (方向別)
    float3 l1;
    l1.r = dot(olSHAr.rgb, N);
    l1.g = dot(olSHAg.rgb, N);
    l1.b = dot(olSHAb.rgb, N);
    shMax = res + l1;       // 最も明るい方向のSH
    // 最も明るい方向の法線を計算して最小SHを求める
    N = normalize(olSHAr.rgb + olSHAg.rgb + olSHAb.rgb);
    l1.r = dot(olSHAr.rgb, N);
    l1.g = dot(olSHAg.rgb, N);
    l1.b = dot(olSHAb.rgb, N);
    shMin = res + l1;       // 最も暗い方向のSH
}
```

**ライト方向計算**:
```hlsl
// openlit_core.hlsl
void ComputeLightDirection(out float3 lightDirection, float4 lightDirectionOverride)
{
    float3 mainDir = OPENLIT_LIGHT_DIRECTION * OpenLitLuminance(OPENLIT_LIGHT_COLOR);
    float3 sh9Dir = olSHAr.xyz * 0.333333 + olSHAg.xyz * 0.333333 + olSHAb.xyz * 0.333333;
    float3 sh9DirAbs = float3(sh9Dir.x, abs(sh9Dir.y), sh9Dir.z);
    float3 customDir = ComputeCustomLightDirection(lightDirectionOverride);
    lightDirection = normalize(sh9DirAbs + mainDir + customDir);
}
// ★ SHの方向とメインライトの方向を輝度で重み付け合成
// → VRChatワールドでSHのみの環境でも正しい方向を得る
```

---

##### 2. シャドウモデル（最重要）

**lilTooning関数群** (トゥーンステップ関数):
```hlsl
// lil_common_functions.hlsl

// ★ コア関数: border±blur/2 の範囲でリニア補間 → saturate
float lilTooningNoSaturateScale(float aascale, float value, float border, float blur)
{
    float borderMin = saturate(border - blur * 0.5);
    float borderMax = saturate(border + blur * 0.5);
    return (value - borderMin) / saturate(borderMax - borderMin);
}

float lilTooningScale(float aascale, float value, float border, float blur)
{
    return saturate(lilTooningNoSaturateScale(aascale, value, border, blur));
}

// blur=0のオーバーロード: ハードステップ
float lilTooningNoSaturateScale(float aascale, float value, float border)
{
    return step(border, value);
}
```

**数学的に重要**: `lilTooningScale` はsmoothstepではなく、**リニア補間+saturate**。
```
toon = saturate((halfLambert - (border - blur*0.5)) / blur)
      = saturate((halfLambert - borderMin) / (borderMax - borderMin))
```

**Shadow計算の正確な式**:
```hlsl
// lil_common_frag.hlsl 内 lilGetShading()

// Step 1: Half-Lambert NdotL
lns.x = saturate(dot(fd.L, N1) * 0.5 + 0.5);

// Step 2: Toon量子化
lns.x = lilTooningScale(aastrength, lns.x, _ShadowBorder, shadowBlur);

// Step 3: ShadowStrength適用 (ガンマ補正付き)
float shadowStrength = _ShadowStrength;
#ifdef LIL_COLORSPACE_GAMMA
    shadowStrength = lilSRGBToLinear(shadowStrength);
#endif
lns.x = lerp(1.0, lns.x, shadowStrength);
// shadowStrength=1.0: フル影 / =0.0: 影なし（常にlit）

// Step 4: Direct/Indirect色の合成
fd.col.rgb = lerp(indirectCol, directCol, lns.x);
// lns.x=1: directCol(lit) / lns.x=0: indirectCol(shadow)
```

**デフォルト値とRange**:
| プロパティ | Range | Default | 説明 |
|-----------|-------|---------|------|
| `_ShadowBorder` | (0, 1) | **0.5** | Half-Lambert空間での影境界位置 |
| `_ShadowBlur` | (0, 1) | **0.1** | 影境界のぼかし幅 |
| `_ShadowStrength` | (0, 1) | **1.0** | 影の強さ (0=影なし) |
| `_ShadowNormalStrength` | (0, 1) | **1.0** | 法線マップの影への影響度 |
| `_ShadowColor` | Color | **(0.82, 0.76, 0.85, 1.0)** | 1st影色 (薄紫がデフォルト) |

**Half-Lambertの重要な性質**:
- `_ShadowBorder=0.5` の場合:
  - 直角面 (raw ndotl=0): halfLambert = 0.5 → lilTooningScale境界上 → toon ≈ 0.5
  - **前面は完全な影にならない**（Half-Lambertの特性）
  - 裏面 (raw ndotl<0): halfLambert < 0.5 → 影に入る

---

##### 3. マルチシャドウ（2nd, 3rd）の合成方法

```hlsl
// lil_common_frag.hlsl

// 1st Shadow色 (影のベース)
float3 indirectCol = lerp(fd.albedo, shadowColorTex.rgb, shadowColorTex.a) * _ShadowColor.rgb;

// 2nd Shadow (より深い影)
lns.y = lilTooningScale(aastrength, lns.y, _Shadow2ndBorder, shadow2ndBlur);
shadow2ndColorTex.rgb = lerp(fd.albedo, shadow2ndColorTex.rgb, shadow2ndColorTex.a) * _Shadow2ndColor.rgb;
lns.y = _Shadow2ndColor.a - lns.y * _Shadow2ndColor.a;
// ★ _Shadow2ndColor.a がマスター強度。toon値が高いほど2nd影は薄くなる
indirectCol = lerp(indirectCol, shadow2ndColorTex.rgb, lns.y);

// 3rd Shadow (最も深い影)
#if defined(LIL_FEATURE_SHADOW_3RD)
    lns.z = lilTooningScale(aastrength, lns.z, _Shadow3rdBorder, shadow3rdBlur);
    shadow3rdColorTex.rgb = lerp(fd.albedo, shadow3rdColorTex.rgb, shadow3rdColorTex.a) * _Shadow3rdColor.rgb;
    lns.z = _Shadow3rdColor.a - lns.z * _Shadow3rdColor.a;
    indirectCol = lerp(indirectCol, shadow3rdColorTex.rgb, lns.z);
#endif

// ★ 合成は「段階的lerp」: 1st → 2nd → 3rd と重ねがけ
// → αが0.0なら2nd/3rd影は無効
```

**2nd/3rd Shadow デフォルト値**:
| プロパティ | Range | Default | 説明 |
|-----------|-------|---------|------|
| `_Shadow2ndColor` | Color | **(0.68, 0.66, 0.79, 1.0)** | 2nd影色 |
| `_Shadow2ndBorder` | (0, 1) | **0.15** | 2nd影境界 (1stより暗い領域) |
| `_Shadow2ndBlur` | (0, 1) | **0.1** | 2nd影ぼかし |
| `_Shadow3rdColor` | Color | **(0, 0, 0, 0)** | 3rd影色 (**alpha=0でデフォルト無効**) |
| `_Shadow3rdBorder` | (0, 1) | **0.25** | 3rd影境界 |
| `_Shadow3rdBlur` | (0, 1) | **0.1** | 3rd影ぼかし |

---

##### 4. MatCapブレンドモード（★最重要★）

**lilBlendColor関数** (全ブレンドモード):
```hlsl
// lil_common_functions.hlsl
float3 lilBlendColor(float3 dstCol, float3 srcCol, float3 srcA, uint blendMode)
{
    float3 ad = dstCol + srcCol;
    float3 mu = dstCol * srcCol;
    float3 outCol;
    if(blendMode == 0) outCol = srcCol;                  // ★ Normal: ソースカラー置換
    if(blendMode == 1) outCol = ad;                      // Add: 加算
    if(blendMode == 2) outCol = max(ad - mu, dstCol);    // Screen: 1-(1-a)*(1-b)
    if(blendMode == 3) outCol = mu;                      // Multiply: 乗算
    return lerp(dstCol, outCol, srcA);
    // ★ 最終的にsrcA (= _MatCapBlend * matCapColor.a * mask) でブレンド
}
```

**MatCap適用コード**:
```hlsl
// lil_common_frag.hlsl
fd.col.rgb = lilBlendColor(fd.col.rgb, matCapColor.rgb,
    _MatCapBlend * matCapColor.a * matCapMask, _MatCapBlendMode);
```

**★ Normal(0)の正確な挙動**:
```hlsl
// blendMode=0 (Normal) の展開:
outCol = srcCol;                           // matCapColor.rgb をそのまま
result = lerp(dstCol, srcCol, srcA);       // アルファブレンド
// = dstCol * (1 - srcA) + srcCol * srcA
// = 元の色 × (1-強度) + MatCap色 × 強度

// 例: base=(0.8,0.7,0.6), matcap=(0.9,0.9,0.9), blend=0.5
// result = (0.8,0.7,0.6) * 0.5 + (0.9,0.9,0.9) * 0.5
//        = (0.85, 0.80, 0.75)  ← 白飛びしない！
```

**→ Normal(0) = lerp (アルファブレンド) であり、Add(加算)ではない！**
**→ Natane移行時に Add(0) にマッピングすると白飛びする根本原因**

**各ブレンドモードの対応表**:
| lilToon | Mode | HLSL | Natane対応候補 |
|---------|------|------|--------------|
| **0=Normal** | `lerp(dst, src, srcA)` | アルファブレンド | **Replace(2)が最も近い** |
| **1=Add** | `lerp(dst, dst+src, srcA)` | 加算 | Add(0) |
| **2=Screen** | `lerp(dst, max(dst+src-dst*src, dst), srcA)` | スクリーン | (Natane未対応) |
| **3=Multiply** | `lerp(dst, dst*src, srcA)` | 乗算 | Multiply(1) |

**デフォルト値**:
| プロパティ | Type/Range | Default | 説明 |
|-----------|-----------|---------|------|
| `_MatCapBlend` | Range(0,1) | **1.0** | MatCap強度 |
| `_MatCapBlendMode` | Int | **1 (=Add)** | ★デフォルトはAdd! |

---

##### 5. リムライトモデル

```hlsl
// lil_common_frag.hlsl

// Step 1: Fresnel計算
float nvabs = abs(dot(N, fd.headV));      // |NdotV|
float rim = pow(saturate(1.0 - nvabs), _RimFresnelPower);
// 1-|NdotV| のPower乗 → エッジほど明るい

// Step 2: Toon量子化 (Border + Blur)
rim = lilTooningScale(_AAStrength, rim, _RimBorder, _RimBlur);
// ★ シャドウと同じlilTooningScale関数を使用

// Step 3: RimEnableLighting (ライトカラーの影響度)
float3 rimLightColor = lerp(1.0, fd.lightColor, _RimEnableLighting);
// _RimEnableLighting = 0.0: リムはライト非依存 (常に一定の明るさ)
// _RimEnableLighting = 1.0: リムはライトカラーで色付け

// Step 4: リムカラーをブレンド
float3 rimColor = _RimColor.rgb * rimLightColor;
fd.col.rgb = lilBlendColor(fd.col.rgb, rimColor, rim * _RimColor.a, _RimBlendMode);
// ★ MatCapと同じlilBlendColor関数を使用（Normal/Add/Screen/Mulから選択可能）
```

**デフォルト値**:
| プロパティ | Range | Default | 説明 |
|-----------|-------|---------|------|
| `_RimFresnelPower` | (0.01, 50) | **3.5** | Fresnel指数 (大きいほど細いリム) |
| `_RimBorder` | (0, 1) | **0.5** | リム境界位置 |
| `_RimBlur` | (0, 1) | **0.65** | リムぼかし幅 (**デフォルトで柔らかい**) |
| `_RimEnableLighting` | (0, 1) | **1.0** | ライト影響度 |
| `_RimShadowMask` | (0, 1) | **0.5** | 影部分のリム抑制度 |

---

##### 6. スペキュラモデル

```hlsl
// lil_common_frag.hlsl
float nh = dot(N, H);  // NdotH (ハーフベクトル)

// ★ _SpecularToon = true の場合: Toonスペキュラ
if(_SpecularToon)
    spec = lilTooningScale(_AAStrength, pow(nh, 1.0/fd.roughness), _SpecularBorder, _SpecularBlur);
    // pow(NdotH, 1/roughness) を border/blur でトゥーン量子化

// ★ _SpecularToon = false の場合: 通常PBRスペキュラ (GGX)
```

**デフォルト値**:
| プロパティ | Type/Range | Default | 説明 |
|-----------|-----------|---------|------|
| `_SpecularToon` | Int (bool) | **1** | ★デフォルトON (トゥーンスペキュラ) |
| `_SpecularBorder` | (0, 1) | **0.5** | スペキュラ境界 |
| `_SpecularBlur` | (0, 1) | **0.0** | ★デフォルト0 = ハードステップ |
| `_Smoothness` | (0, 1) | - | PBRスムースネス |
| `_Metallic` | (0, 1) | - | メタリック度 |

---

##### 7. 重要な定数・デフォルト値・暗黙的保証

**_LightMinLimit** (★暗黙的な明度保証):
```hlsl
// lil_common_macro.hlsl - LIL_CORRECT_LIGHTCOLOR マクロ内
lightColor = clamp(lightColor, _LightMinLimit, _LightMaxLimit);
// _LightMinLimit = 0.05 (default)
// → ライトカラーが0.05未満にならない = 完全な真っ暗を防止
// → VRChatの暗いワールドでもキャラが最低限見える
```

**_AsUnlit** (アンライト度):
```hlsl
lightColor = lerp(lightColor, 1.0, _AsUnlit);
// _AsUnlit = 0.0 (default): 通常ライティング
// _AsUnlit = 1.0: lightColor = 1.0 (完全アンライト)
// _AsUnlit = 0.5: ライト50%+固定50%
```

**全プロパティ一覧**:
| プロパティ | Range | Default | 効果 |
|-----------|-------|---------|------|
| `_LightMinLimit` | (0, 1) | **0.05** | ライトカラー最低保証 |
| `_LightMaxLimit` | (0, 10) | **1.0** | ライトカラー最大制限 |
| `_AsUnlit` | (0, 1) | **0.0** | アンライト度 |
| `_MonochromeLighting` | (0, 1) | **0.0** | ライトのモノクロ化 |
| `_VertexLightStrength` | (0, 1) | **0.0** | 頂点ライト強度 |
| `_lilDirectionalLightStrength` | (0, 1) | **1.0** | ディレクショナルライト強度 |
| `_LightDirectionOverride` | Vector | **(0.001, 0.002, 0.001, 0)** | ライト方向オーバーライド |

---

##### 8. 最終出力フロー（まとめ）

```
[テクスチャサンプリング] fd.albedo = MainTex * _Color

[ライトカラー処理]
  lightColor = MainLightColor + SHToon
  lightColor = clamp(lightColor, _LightMinLimit, _LightMaxLimit)  ← 明度保証
  lightColor = lerp(lightColor, gray(lightColor), _MonochromeLighting)
  lightColor = lerp(lightColor, 1.0, _AsUnlit)                    ← アンライト

[シャドウ計算 (lilGetShading)]
  halfLambert = dot(L, N) * 0.5 + 0.5                ← ★Half-Lambert
  toon = lilTooningScale(halfLambert, border, blur)   ← リニア補間+saturate
  toon = lerp(1.0, toon, _ShadowStrength)
  directCol = albedo
  indirectCol = albedo * _ShadowColor
              → lerp(1st, 2nd, alpha2)                ← マルチシャドウ
              → lerp(prev, 3rd, alpha3)
  shadedColor = lerp(indirectCol, directCol, toon)

[MatCap適用]
  matcapUV = viewSpaceNormal * 0.5 + 0.5
  matCapColor = tex2D(_MatCapTex, matcapUV) * _MatCapColor
  shadedColor = lilBlendColor(shadedColor, matCapColor,
                  _MatCapBlend * matCapColor.a, _MatCapBlendMode)
  // ★ Normal(0) = lerp, Add(1), Screen(2), Mul(3)

[リムライト適用]
  rim = pow(1 - |NdotV|, _RimFresnelPower)
  rim = lilTooningScale(rim, _RimBorder, _RimBlur)
  rimColor = _RimColor * lerp(1, lightColor, _RimEnableLighting)
  shadedColor = lilBlendColor(shadedColor, rimColor, rim * alpha, blendMode)

[ライトカラー乗算]
  finalColor = shadedColor * lightColor

[エミッション加算]
  finalColor += emissionColor * emissionIntensity

[フォグ適用]
  → SV_Target出力
```

**★ lilToonの特徴的な点（Nataneとの主な差異）**:
1. **Half-Lambert使用**: NdotL * 0.5 + 0.5 → 前面は完全影にならない
2. **normalize+luminance なし**: ライト色をそのまま乗算（暗化なし）
3. **_LightMinLimit**: ライト色のclamp下限（0.05）で暗すぎない保証
4. **lilTooningScale**: smoothstepではなくリニア補間+saturate
5. **MatCap Normal(0) = lerp**: アルファブレンド（加算ではない）
6. **_ShadowColor乗算方式**: albedo * shadowColor（Nataneはlerp方式）

#### Natane パイプライン解析結果

**解析者**: researcher-natane / **解析日**: 2026-02-27
**対象ファイル**: Fragment.hlsl (~1500行), Lighting.hlsl (~592行), Input.hlsl (~1033行), NataneToonShader.shader (~999行+)

---

##### 1. ライティングパイプライン全体フロー (ForwardBase)

```
[入力] MainTex * _Color → col.rgb (albedo)
  ↓
[NdotL計算] dot(shadingNormal, lightDir)   ← SmoothNormal blend可
  ↓  Fragment.hlsl:305
[SDF Shadow] ApplySDFShadow(uv, ndotl, lightDir, worldPos)
  ↓  Fragment.hlsl:309
[lightTerm] = ndotl
  ↓
[Shadow Receive Mask] lightTerm = lerp(lightTerm, 1.0, shadowReceiveMask)
  ↓  Fragment.hlsl:331
[Lit Softness] lightTerm = lerp(lightTerm, smoothstep(0,1,lightTerm), _LitSoftness)
  ↓  Fragment.hlsl:336
[Dithering] 境界領域にBayerパターン加算 (optional)
  ↓  Fragment.hlsl:346
[ApplyLightBlend] smoothstep(0,1, lightTerm*(1+_LightBlend)) + HighlightSoftness
  ↓  Fragment.hlsl:351 → Lighting.hlsl:146-170
[ToonShading or GradientShading]
  ↓  Fragment.hlsl:382-396
  │  ToonShading(lightTerm, _ShadowSteps, _ShadowSharpness)
  │    ├─ ndotl += clamp(_ShadowOffset, -1, 1)
  │    ├─ stepValue = 1/max(steps, 1)
  │    ├─ currentStep = floor(ndotl * steps)
  │    ├─ stepPosition = frac(ndotl * steps)
  │    ├─ smoothRange = saturate(sharpness + _StepBorderSmooth) * 0.5
  │    ├─ smoothedStep = smoothstep(0.5-smoothRange, 0.5+smoothRange, stepPosition)
  │    └─ toon = (currentStep + smoothedStep) * stepValue
  │
[ShadingGradeMap] shadingValue = saturate(shadingValue + gradeAdjust)
  ↓  Fragment.hlsl:399
[AO適用] shadingValue *= aoEffect → lerp(pre, after, _AOBlend)
  ↓  Fragment.hlsl:402-407
[Shadow Attenuation] shadingValue *= atten
  ↓  Fragment.hlsl:410
[Shadow Color計算]
  │  MultiToneShadowColor(shadingValue, (1,1,1))
  │    ├─ _ShadowColor.rgb (1st shadow)
  │    ├─ lerp(_Shadow2ndColor, shadowColor, blend2nd)  ← smoothstep境界
  │    └─ lerp(_Shadow3rdColor, shadowColor, blend3rd)  ← smoothstep境界
  │  * ShadowColorTex適用
  ↓  Fragment.hlsl:416-421
[ShadowMaxDarkness] shadowColor = max(shadowColor, saturate(_ShadowMaxDarkness))
  ↓  Fragment.hlsl:428
[lighting合成] lighting = lerp(shadowColor, (1,1,1), shadingValue)
  ↓  Fragment.hlsl:424

======== STEP 1: Indirect Light ========
  indirectResult = SH Light Probe or Light Volume or Lightmap
  indirectResult = max(indirectResult, _IndirectLightMinColor)
  indirectResult *= _IndirectLightIntensity * _GIIntensity * aoForIndirect
  ↓  Fragment.hlsl:441-498

======== STEP 3: Direct Light ========
  directResult = lerp(shadowColor, (1,1,1), shadingValue)
  ↓
  [Light Color適用]
    lightColorLum = luminance(effectiveLightColor)
    colorMultiplied = directResult * saturate(effectiveLightColor)
    luminanceOnly = directResult * lightColorLum
    lightColorInfluenced = lerp(luminanceOnly, colorMultiplied, _LightColorInfluence)
    directResult = lightColorInfluenced * _LightIntensity
  ↓  Fragment.hlsl:514-518

  ★★★ [normalize + luminanceステップ] ★★★  ← 最重要！
    directLum = luminance(directResult)
    directLum = clamp(directLum, _LightMinInfluence, _LightMaxInfluence)
    directDir = normalize(max(directResult, 0.01))
    directResult = directDir * directLum
  ↓  Fragment.hlsl:521-524

======== STEP 4: Additional Light ========
  additionalResult = vertexLights * _AdditionalLightIntensity
  additionalResult += backlight * _BacklightColor * effectiveLightColor
  additionalResult += ltcgiDiffuse (optional)
  ↓  Fragment.hlsl:526-553

======== STEP 5: Final Composition ========
  [非LV] lighting = max(indirectResult, directResult + additionalResult) + ambient
  [LV Natural] lighting = max(max(indirect, directLV), directResult + additionalResult)
  ↓  Fragment.hlsl:584-633

======== Albedo × Lighting ========
  originalAlbedo = col.rgb
  lightingLum = luminance(lighting)
  albedoDir = originalAlbedo / max(luminance(originalAlbedo), 0.01)
  preservedLitColor = albedoDir * originalLum * lightingLum
  traditionalLitColor = originalAlbedo * lighting
  col.rgb = lerp(traditionalLitColor, preservedLitColor, _AlbedoPreservation)
  ↓  Fragment.hlsl:672-696

  [Saturation] col.rgb = lerp(gray, col.rgb, _Saturation)
  ↓  Fragment.hlsl:707

  [Brightness] col.rgb *= _Brightness
  ↓  Fragment.hlsl:711
```

**重要**: NataneはHalf-Lambertを使わない。raw NdotL = dot(N,L) を直接使用。

---

##### 2. normalize + luminance ステップの数学的分析 (Fragment.hlsl:521-524)

```hlsl
// STEP 3 の最終段: ライトの色情報を正規化し、輝度で再スケール
half directLum = CALC_LUMINANCE(directResult);        // dot(c, (0.299,0.587,0.114))
directLum = clamp(directLum, _LightMinInfluence, _LightMaxInfluence);
half3 directDir = normalize(max(directResult, 0.01)); // 色方向ベクトル
directResult = directDir * directLum;                  // 方向×輝度で再構成
```

**数学的影響**:

normalize(v) = v / |v| (ユークリッドノルム)

```
例: directResult = (0.5, 0.5, 0.5) (均一グレー)
  |v| = sqrt(0.25+0.25+0.25) = sqrt(0.75) ≈ 0.866
  normalize(v) = (0.577, 0.577, 0.577)
  luminance = 0.5*0.299 + 0.5*0.587 + 0.5*0.114 = 0.5
  結果 = (0.577, 0.577, 0.577) * 0.5 = (0.289, 0.289, 0.289)
  暗化率 = 0.289 / 0.5 = 0.577 = 1/sqrt(3)  ← 均一色の場合
```

**一般式**: normalizeによるチャネルあたりの変化
```
入力: v = (r, g, b)
出力: normalize(v) * luminance(v) = v/|v| * dot(v, w)
  ここで w = (0.299, 0.587, 0.114)
  |v| = sqrt(r^2 + g^2 + b^2)

暗化率（チャネル平均） = luminance(v) / |v|
```

**典型的な暗化率の計算**:

| 色 | 値 | |v| (magnitude) | luminance | 暗化率 (lum/mag) | 備考 |
|---|---|---|---|---|---|
| 均一グレー (v,v,v) | (0.5, 0.5, 0.5) | 0.866 | 0.500 | **0.577** (1/sqrt(3)) | 均一色は常にこの比率 |
| 純白 (1,1,1) | (1, 1, 1) | 1.732 | 1.000 | **0.577** | 同上 |
| 肌色シャドウ | (0.6, 0.4, 0.3) | 0.781 | 0.449 | **0.575** | ほぼ均一色と同じ |
| 暖色シャドウ | (0.7, 0.3, 0.2) | 0.806 | 0.399 | **0.495** | やや強い暗化 |
| 純赤 (1,0,0) | (1, 0, 0) | 1.000 | 0.299 | **0.299** | 極端に暗くなる |
| 純緑 (0,1,0) | (0, 1, 0) | 1.000 | 0.587 | **0.587** | luminance重み大 |
| 純青 (0,0,1) | (0, 0, 1) | 1.000 | 0.114 | **0.114** | 最も暗化 |
| ライトカラー白 | (1, 1, 1)*LI | = 1.732*LI | 1.0*LI | **0.577** | _LightIntensity=1時 |

**彩度への影響**:
normalizeは色の「方向」（色相+彩度のベクトル方向）を保存し、長さを1にする。
luminanceで再スケールするため、**luminance/magnitudeの比率が全チャネルに均等に乗る**。
つまり色相は変わらないが、**彩度が高い色（1つのチャネルが支配的）ほど暗くなる**。

**_LightMinInfluence/_LightMaxInfluenceの効果**:
- _LightMinInfluence (default: 0, range: 0-1): directLumの下限。暗すぎる環境での最低保証明るさ
- _LightMaxInfluence (default: 2, range: 1-5): directLumの上限。明るすぎるライトの抑制

---

##### 3. MatCapブレンドモード (Fragment.hlsl:1005-1082)

```hlsl
// MatCap UV計算（ビュー空間法線からスフィアマッピング）
float2 matcapUV = CalculateMatCapUV(worldNormal, viewDir);
half3 matcap = tex2D(_MatCapTex, matcapUV) * _MatCapIntensity;
matcap *= matcapMask;  // マスク適用
matcap *= _Glossiness; // グロッシネス適用
matcap = ApplyMatteQuality(matcap, col.rgb, _MatteEffect); // マット質感

// 3つのブレンド結果を事前計算（分岐なし最適化）
half matcapStrength = saturate(_MatCapIntensity * matcapMask);
half3 addResult    = SafeAdditiveBlend(col.rgb, matcap, matcapStrength);
half3 multiplyResult = BlendWithSoftMask(col.rgb, col.rgb * matcap, saturate(_MatCapIntensity * matcapMask));
half3 replaceResult  = BlendWithSoftMask(col.rgb, matcap, saturate(_MatCapIntensity * matcapMask));

// step関数でブレンドモード選択
half isMultiply = step(0.5, _MatCapBlendMode) * step(_MatCapBlendMode, 1.5); // mode==1
half isReplace  = step(1.5, _MatCapBlendMode);                                // mode==2
col.rgb = lerp(addResult, multiplyResult, isMultiply);
col.rgb = lerp(col.rgb, replaceResult, isReplace);

// _MatCapBlend でエフェクトブレンド
col.rgb = lerp(preMatCap, col.rgb, _MatCapBlend);
```

**各モード詳細**:

| Mode | _MatCapBlendMode | HLSL | 効果 |
|---|---|---|---|
| **Add (0)** | 0 | `SafeAdditiveBlend(base, matcap, strength)` | 加算合成（白飛び防止付き） |
| **Multiply (1)** | 1 | `BlendWithSoftMask(base, base*matcap, strength)` | 乗算合成（暗くなる） |
| **Replace (2)** | 2 | `BlendWithSoftMask(base, matcap, strength)` | 置換（MatCapで上書き） |

**Add(0)の白飛び問題**:
```hlsl
// SafeAdditiveBlend の実装:
half baseLum = luminance(baseColor);
half compressionFactor = saturate(1.0 - baseLum * 0.4);
half darknessFactor = smoothstep(0.0, 0.05, baseLum);
half finalStrength = strength * max(compressionFactor * darknessFactor, 0.15);
result = baseColor + additiveColor * finalStrength;
// → soft clamp: resultLum > 0.95 で圧縮

// 例: baseColor=(0.8, 0.8, 0.8), matcap=(0.5, 0.5, 0.5), strength=0.5
//   baseLum = 0.8
//   compressionFactor = saturate(1 - 0.32) = 0.68
//   darknessFactor = 1.0 (baseLum >> 0.05)
//   finalStrength = 0.5 * max(0.68, 0.15) = 0.34
//   result = (0.8,0.8,0.8) + (0.5,0.5,0.5) * 0.34 = (0.97, 0.97, 0.97)
//   → 白飛びぎりぎり！soft clampが発動して0.95以上を圧縮
```

**lilToon Normal(0)=lerp との差異**:
lilToonのNormal(0)は `lerp(base, matcap, blend)` = 線形補間（白飛びしない）。
NataneのAdd(0)は `base + matcap * strength` = 加算（白飛びする）。
→ lilToonからの移行時、MatCapBlendMode=0 は使用すべきでない。**Replace(2)が最も近い**。

---

##### 4. ForwardBase vs ForwardAdd の処理差異

**ForwardAdd パス** (Fragment.hlsl:644-670):
```hlsl
// ForwardAddは追加ライトの寄与のみ出力
lighting = lerp(shadowColor, (1,1,1), shadingValue);

// ライトカラー適用（ForwardBaseと同じロジック）
lightColorLum_add = luminance(_LightColor0.rgb);
colorMul_add = lighting * _LightColor0.rgb;
lumOnly_add = lighting * lightColorLum_add;
lighting = lerp(lumOnly_add, colorMul_add, _LightColorInfluence);
lighting *= _LightIntensity;
lighting *= _AdditionalLightIntensity;  ← ForwardAdd固有

// ★★★ normalize+luminanceステップは ForwardAdd には無い ★★★
// directLum/directDir の処理は UNITY_PASS_FORWARDBASE 内のみ

// 最終出力
col.rgb = originalAlbedo * lighting * _Brightness;
// + backlight contribution
```

**重要な差異**:
- ForwardAdd には normalize+luminance ステップ（line 521-524）が**ない**
- ForwardAdd には AlbedoPreservation が**ない** (直接 albedo * lighting * _Brightness)
- ForwardAdd には Saturation 調整が**ない**
- ForwardAdd には _AdditionalLightIntensity 乗算が追加
- ForwardAdd には間接光(indirect)が**ない**
- MatCap, Emission, Glitter等のForwardBase限定エフェクトは ForwardAdd で**スキップ**
- Specular, SSS, RimLight は両パスで処理（ForwardAddでは `*= _AdditionalLightIntensity` 追加）

---

##### 5. 全プロパティのデフォルト値とRange

**コアライティングパラメータ**:

| プロパティ | Default | Range | 説明 |
|---|---|---|---|
| `_Brightness` | 1 | (0.5, 5.0) | 全体明るさ乗算 |
| `_Saturation` | 1 | (0, 2) | 彩度調整 |
| `_LightIntensity` | 1 | (0, 5) | ライト強度乗算 |
| `_LightColorInfluence` | 1 | (0, 1) | ライトカラーの影響度(0=luminanceのみ) |
| `_IndirectLightIntensity` | 1 | (0, 2) | 間接光強度 |
| `_GIIntensity` | 0.5 | (0, 1) | GI/Light Probe強度 |
| `_LightMinInfluence` | 0 | (0, 1) | normalize後のluminance下限 |
| `_LightMaxInfluence` | 2 | (1, 5) | normalize後のluminance上限 |
| `_AdditionalLightIntensity` | 0.5 | (0, 1) | ForwardAdd追加ライト強度 |

**シャドウパラメータ**:

| プロパティ | Default | Range | 説明 |
|---|---|---|---|
| `_ShadowColor` | (0.5,0.5,0.5,1) | Color | 1stシャドウカラー |
| `_Shadow2ndColor` | (0.35,0.35,0.35,1) | Color | 2ndシャドウカラー |
| `_Shadow2ndBorder` | 0.3 | (0, 1) | 2ndシャドウ境界位置 |
| `_Shadow3rdColor` | (0.2,0.2,0.2,1) | Color | 3rdシャドウカラー |
| `_Shadow3rdBorder` | 0.15 | (0, 1) | 3rdシャドウ境界位置 |
| `_ShadowSteps` | 2 | (1, 10) | トゥーン階調数 |
| `_ShadowSharpness` | 0.1 | (0.001, 1) | シャドウ境界の鋭さ |
| `_StepBorderSmooth` | 0 | (0, 1) | 段階境界なじませ幅 |
| `_ShadowOffset` | 0 | (-1, 1) | シャドウ閾値オフセット |
| `_ShadowBlend` | 0 | (0, 1) | シャドウ境界の柔らかさ |
| `_ShadowMaxDarkness` | 0 | (0, 1) | シャドウの最大暗さ制限 |
| `_ShadowReceive` | 1 | (0, 1) | シャドウマップ受信強度 |
| `_ShadowSmoothing` | 0 | (0, 1) | シャドウマップAA(PCF) |
| `_LitSoftness` | 0 | (0, 1) | 明部のsmoothstep |
| `_LightBlend` | 0 | (0, 1) | ライティング柔軟化 |
| `_HighlightSoftness` | 0 | (0, 1) | ハイライト柔軟化 |
| `_ShadowEnvStrength` | 0 | (0, 1) | 影への環境色反映 |
| `_ShadingMode` | 0 | Enum(Toon=0,Gradient=1) | シェーディングモード |
| `_ShadingGradientWidth` | 0.2 | (0.001, 1) | グラデーション幅 |

**色調整パラメータ**:

| プロパティ | Default | Range | 説明 |
|---|---|---|---|
| `_AlbedoPreservation` | 0 | (0, 1) | テクスチャ色保存度 |
| `_Glossiness` | 1 | (0, 1) | 全体グロッシネス |
| `_MatteEffect` | 0 | (0, 1) | マット効果 |
| `_FinalHighlightBlend` | 0 | (0, 1) | ハイライト圧縮(白飛び防止) |
| `_HighlightThreshold` | 0.75 | (0, 1) | 圧縮開始閾値 |
| `_FinalShadowBlend` | 0 | (0, 1) | シャドウリフト(黒潰れ防止) |
| `_ShadowThreshold` | 0.25 | (0, 1) | リフト開始閾値 |

**MatCapパラメータ**:

| プロパティ | Default | Range | 説明 |
|---|---|---|---|
| `_MatCapIntensity` | 1 | (0, 2) | MatCap強度 |
| `_MatCapBlendMode` | 0 | Enum(Add=0,Multiply=1,Replace=2) | ブレンドモード |
| `_MatCapBlend` | 1 | (0, 1) | エフェクト全体ブレンド |
| `_MatCapBlur` | 0 | (0, 1) | MatCapぼかし |

**リムライトパラメータ**:

| プロパティ | Default | Range | 説明 |
|---|---|---|---|
| `_RimColor` | (1,1,1,1) | Color | リムカラー |
| `_RimPower` | 3 | (0.1, 10) | リムパワー(Fresnel指数) |
| `_RimIntensity` | 1 | (0, 5) | リム強度 |
| `_RimSpread` | 0 | (0, 1) | リムグロー拡散 |
| `_RimBlendMode` | 0 | Enum(Normal=0,Soft=1,Screen=2,Overlay=3) | ブレンドモード |
| `_RimBlend` | 1 | (0, 1) | エフェクトブレンド |
| `_RimDirStrength` | 0 | (0, 1) | ライト方向連動強度 |
| `_RimShadowMask` | 0 | (0, 1) | シャドウマスク強度 |

**アウトラインパラメータ**:

| プロパティ | Default | Range | 説明 |
|---|---|---|---|
| `_OutlineWidth` | 0.1 | (0, 1) | アウトライン幅 (内部: *0.1 スケール) |
| `_OutlineColor` | (0,0,0,1) | Color | アウトラインカラー |
| `_OutlineMode` | 0 | Enum(InvertedHull=0,BackFace=1) | アウトライン方式 |

**バックライトパラメータ**:

| プロパティ | Default | Range | 説明 |
|---|---|---|---|
| `_BacklightIntensity` | 0 | (0, 2) | バックライト強度 |
| `_BacklightColor` | (1,1,1,1) | Color | バックライトカラー |

**IndirectLightMinColor**: Default = (0.1, 0.1, 0.1, 1) — 暗いワールドでの最低保証色

---

##### 6. ToonShading関数の詳細 (Lighting.hlsl:57-93)

```hlsl
float ToonShading(float ndotl, float steps, float sharpness) {
    // 1. ShadowOffset適用
    ndotl = saturate(ndotl + clamp(_ShadowOffset, -1, 1));

    // 2. SoftLighting/ShadowBlend でsharpness調整
    //    _ShadowBlend > 0: sharpness拡大 → 境界がぼやける

    // 3. 階段関数
    stepValue = 1.0 / max(steps, 1.0);
    currentStep = floor(ndotl * steps);
    stepPosition = frac(ndotl * steps);

    // 4. アンチエイリアス
    smoothRange = saturate(sharpness + _StepBorderSmooth) * 0.5;
    smoothedStep = smoothstep(0.5 - smoothRange, 0.5 + smoothRange, stepPosition);

    // 5. 最終値
    toon = (currentStep + smoothedStep) * stepValue;
    return saturate(toon);
}
```

**重要な挙動**:
- steps=2, sharpness=0.1, offset=0 の場合:
  - ndotl < 0 → toon = 0 (完全影)
  - ndotl = 0.25 → currentStep=0, stepPos=0.5, toon ≈ 0.5 * 0.5 = 0.25
  - ndotl = 0.5 → currentStep=1, stepPos=0.0, toon = 0.5
  - ndotl = 0.75 → currentStep=1, stepPos=0.5, toon ≈ 0.75
  - ndotl = 1.0 → toon = 1.0

---

##### 7. パイプライン全体のサマリー図

```
[Albedo]──────┐
              ↓
         ForwardBase
              │
   ┌──────────┴──────────┐
   │   Lighting Pipeline  │
   │                      │
   │  NdotL (raw, no HL) │   ← Half-Lambert不使用
   │  → ToonShading       │
   │  → Shadow Color lerp │
   │  → LightColor適用    │
   │  → ★normalize+lum★  │   ← directResult暗化
   │  → clamp(min,max)    │
   │  → max(indirect,     │
   │       direct+addl)   │
   └──────────┬──────────┘
              │
   ┌──────────┴──────────┐
   │  Albedo Application  │
   │                      │
   │  AlbedoPreservation  │
   │  → Saturation        │
   │  → _Brightness       │
   └──────────┬──────────┘
              │
   ┌──────────┴──────────┐
   │  Post-Lighting FX    │
   │                      │
   │  Specular → HairSpec │
   │  → SSS → Rim(1,2)   │
   │  → OffsetRim → EnvRim│
   │  → MatCap(1,2,3)    │
   │  → Reflection        │
   │  → Refraction        │
   │  → Emission          │
   │  → HueShift          │
   │  → AudioLink         │
   │  → Glitter           │
   │  → Iridescence       │
   │  → Smear → Drip      │
   │  → Hologram → Glitch │
   │  → Decal → Dissolve  │
   │  (各: SafeAddBlend   │
   │   + EffectBlendPost) │
   └──────────┬──────────┘
              │
         Final Output
```

---

##### 8. 移行に特に重要なポイントまとめ

1. **normalize+luminance (line 521-524)**: 均一色で1/sqrt(3) ≈ 0.577倍に暗化。彩度が高いほど暗化が強い。_LightMinInfluence=0, _LightMaxInfluence=2 がデフォルト。
2. **Half-Lambert不使用**: lilToonの NdotL*0.5+0.5 に対し、Nataneは raw NdotL + _ShadowOffset。シャドウ領域が広くなる。
3. **MatCap Add(0) ≠ lilToon Normal(0)**: Natane Add=加算、lilToon Normal=lerp。移行時はReplace(2)が最も近い。
4. **AlbedoPreservation (default=0)**: 0の場合は伝統的な albedo*lighting。1にすると色方向保存。
5. **_GIIntensity (default=0.5)**: lilToonより低いデフォルト → 暗めの環境光。
6. **Post-lighting FX は SafeAdditiveBlend 経由**: baseLum > 0.95 で自動圧縮。
7. **ForwardAdd にはnormalize+luminanceステップなし**: ForwardBaseのみの暗化。

#### Gap分析＋修正プラン
**作成者**: team-lead / **作成日**: 2026-02-27

---

##### パイプライン差異マトリクス（重要度順）

| # | 項目 | lilToon | Natane | 影響度 | 修正方法 |
|---|------|---------|--------|--------|----------|
| 1 | **MatCap Normal(0)** | `lerp(dst, src, srcA)` (アルファブレンド) | Add(0)=`base + matcap*str` (加算) | ★★★ 白飛び | `ConvertMatCapBlendMode`: Normal(0)→**Replace(2)** |
| 2 | **normalize+luminance** | なし | `normalize(v)*luminance(v)` → 0.577倍暗化 | ★★★ 暗い | `_Brightness=2.0, _Saturation=1.5, _LightIntensity=√3` (実装済み) |
| 3 | **Half-Lambert vs raw NdotL** | `ndotl*0.5+0.5` (前面≥0.5) | `saturate(ndotl)` (前面≥0) | ★★★ 影が広い | `_ShadowOffset=1-border` (実装済み) |
| 4 | **GIIntensity** | 暗黙的に1.0 (SH減衰なし) | デフォルト0.5 | ★★ 環境光暗い | 移行時 `_GIIntensity=1.0` 設定 |
| 5 | **Rim BlendMode** | lilBlendColor: Normal(0)=lerp | Natane: Normal(0)=Soft Light | ★★ リム形状違い | BlendMode変換ロジック追加 |
| 6 | **Shadow色モデル** | `albedo * shadowColor` (乗算) | `lerp(shadowColor, white, toon)` | ★★ 影色違い | シャドウカラー事前乗算変換 |
| 7 | **lilTooningScale** | リニア補間+saturate | smoothstep (ToonShading) | ★ 境界形状微差 | `_ShadowSharpness`調整で近似 (実装済み) |
| 8 | **Outline内部スケール** | `*0.01` | `*0.1` | ★★★ 10倍太い | `*0.1`スケール変換 (実装済み) |
| 9 | **_LightMinLimit** | `clamp(lightColor, 0.05, max)` | `_LightMinInfluence` | ★ 暗部保証 | `_LightMinInfluence=0.05` (実装済み) |
| 10 | **MatCap _MatCapColor乗算** | `matCapTex * _MatCapColor` | `matCapTex * _MatCapIntensity` | ★ 色味差 | `_MatCapColor`が白でない場合レポート警告 |

##### 今回の修正対象（未実装分）

**修正A: MatCap BlendMode変換の修正** (★★★)
```
現在:  lilToon Normal(0) → Natane Add(0)     ← 白飛びの原因
修正後: lilToon Normal(0) → Natane Replace(2)  ← lerpに最も近い
        lilToon Add(1)    → Natane Add(0)
        lilToon Screen(2) → Natane Add(0)     (Screen未対応のため近似)
        lilToon Mul(3)    → Natane Multiply(1)
```

**修正B: GIIntensity移行補正** (★★)
```
lilToonはGIを暗黙的に1.0相当で使用。
Nataneのデフォルト0.5では環境光が不足して暗くなる。
→ 移行時に _GIIntensity = 1.0 を設定
```

**修正C: Rim Light BlendMode変換** (★★)
```
lilToon RimBlendMode:
  0=Normal (lerp), 1=Add, 2=Screen, 3=Multiply
Natane RimBlendMode:
  0=Normal(SoftLight), 1=Soft, 2=Screen, 3=Overlay

マッピング:
  lilToon Normal(0) → Natane Screen(2)  (lerpの近似としてScreenが最も白飛びしない)
  lilToon Add(1)    → Natane Normal(0)  (加算的な効果)
  lilToon Screen(2) → Natane Screen(2)
  lilToon Mul(3)    → Natane Overlay(3) (減衰系の近似)
```

**修正D: シャドウカラー事前乗算変換** (★★)
```
lilToon: indirectCol = albedo * _ShadowColor  (乗算)
  → ShadowColor=(0.82,0.76,0.85)の場合、albedoの各チャネルを82%/76%/85%に

Natane: directResult = lerp(_ShadowColor, (1,1,1), shadingValue)
  → ShadowColor自体が影の色。albedoは後で乗算。

変換: Natane ShadowColor = lilToon ShadowColor のままでは
  albedo * shadowColor ≠ lerp(shadowColor, white, t) * albedo
  白い肌(0.9,0.8,0.7): lilToon影 = (0.74,0.61,0.60) / Natane影 = shadowColor自体

→ 完全な再現はシェーダー変更なしでは不可能。
→ 実用的妥協: lilToon ShadowColor をそのまま使用 + レポートに差異を注記
   (lilToonのshadowColorは乗算係数なので、Nataneのlerp方式とは意味が異なる)
```

**修正E: MatCap _MatCapBlend → _MatCapBlend + Intensity分離** (★)
```
lilToon: matcap = matCapTex * _MatCapColor
         result = lilBlendColor(base, matcap, _MatCapBlend * matcap.a, mode)
Natane:  matcap = matCapTex * _MatCapIntensity
         result = lerp(preMatCap, blended, _MatCapBlend)

移行:
  _MatCapIntensity = 1.0 (テクスチャの強度は変えない)
  _MatCapBlend = lilToon _MatCapBlend (全体ブレンド量)
  ※ _MatCapColor が白でない場合は情報をレポートに記載
```

##### 実装済み修正（確認）

| 修正 | 状態 | コミット |
|------|------|---------|
| Outline width ×0.1 | ✅ 実装済み | 48afc99 |
| _Brightness=2.0, _Saturation=1.5 | ✅ 実装済み | e65b990 |
| _LightIntensity=√3 | ✅ 実装済み | e65b990 |
| _ShadowOffset=1-border | ✅ 実装済み | 54cd535 |
| _ShadowMaxDarkness=0.15 | ✅ 実装済み | 54cd535 |
| _LightMinInfluence=0.05 | ✅ 実装済み | 54cd535 |
| マルチシャドウ2nd/3rd | ✅ 実装済み | plan対応 |
| シェーダーバリアント検出 | ✅ 実装済み | plan対応 |
| ConversionMode UI | ✅ 実装済み | plan対応 |

---

## 過去の調査記録

<details>
<summary>Background Shader Research (完了)</summary>

### Background Shader Research (bg-shader-research team)

### Task 1: 背景シェーダー現状分析 ✓ COMPLETED
**実施者**: agent-a / **完了日**: 2026-02-27

### 統合調査レポート
- シェーダーパス: 5パス (Outline / ForwardBase / ForwardAdd / ShadowCaster / Meta)
- 実装済み機能数: 80+
- Shader Keywords: 40+
- 完成度: A (プロダクション品質)

</details>

<details>
<summary>v1.3.0 Release (完了)</summary>

- nav.js bilingual support
- styles.css lang-toggle style
- package.json → 1.3.0
- CHANGELOG.md v1.3.0 entry

</details>

<details>
<summary>Tool UI Localization (完了)</summary>

- 33ファイルのJP/ENローカライズ完了
- DrawToolHeader に JP/EN 切り替えボタン追加
- 完了日: 2026-02-27

</details>
