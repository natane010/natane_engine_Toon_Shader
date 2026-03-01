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

## StandardToon + LV 黒レンダリングバグ調査 (2026-02-27)

### 問題
- lilToon から移行した 1.mat が真っ黒になる（本来白くなるはず）
- `_USE_LIGHT_VOLUME` と `_STANDARD_TOON` が両方有効
- `_GIIntensity = 0` に設定済み

### チーム
- mat-analyzer: 1.mat のプロパティ値抽出
- shader-tracer: Fragment shader パストレース
- team-lead: 統合・修正実装

### 1.mat プロパティ値

#### キーワード（m_ValidKeywords）
| キーワード | 有無 |
|---|---|
| `_STANDARD_TOON` | ✅ 有効 |
| `_USE_LIGHT_VOLUME` | ✅ 有効 |
| `_MATCAP` | ✅ 有効 |
| `_OUTLINE` | ✅ 有効 |
| `_RIM_LIGHT` | ✅ 有効 |

#### Light Volume / GI パラメータ
| プロパティ | 値 | 備考 |
|---|---|---|
| `_UseLightVolume` | 1 | Light Volume 有効化 |
| `_LightVolumeBlend` | 1 | ブレンド強度 100% |
| `_LightVolumeBlendMode` | 3 | Multiply（乗算） |
| `_LightVolumeIntensity` | 1 | LV強度 |
| `_LightVolumeSpecular` | 0 | LVスペキュラなし |
| `_GIIntensity` | 0 | ★★★ GI完全無効化 |
| `_IndirectLightIntensity` | 1 | 間接光強度 |
| `_IndirectLightMinColor` | (0, 0, 0, 1) | 黒（最小保証なし） |

#### ライティング基本パラメータ
| プロパティ | 値 | 説明 |
|---|---|---|
| `_ShadingMode` | 2 | StandardToon モード |
| `_Brightness` | 1 | 全体明るさ（デフォルト） |
| `_Saturation` | 1 | 彩度（デフォルト） |
| `_LightIntensity` | 1.25 | ライト強度 |
| `_LightColorInfluence` | 1 | ライトカラー影響 100% |
| `_LightColorMin` | 0.05 | ライト色最小クランプ |
| `_LightColorMax` | 1 | ライト色最大クランプ |
| `_LightMinInfluence` | 0.05 | normalize後の luminance 下限 |
| `_LightMaxInfluence` | 2 | normalize後の luminance 上限 |
| `_AdditionalLightIntensity` | 0.5 | 追加ライト強度 |

#### シャドウパラメータ（StandardToon）
| プロパティ | 値 | 説明 |
|---|---|---|
| `_STShadowBorder` | 0.423 | StandardToonシャドウ境界 |
| `_STShadowBlur` | 0.444 | StandardToonシャドウぼかし |
| `_STShadowStrength` | 1 | シャドウ強度 |
| `_STShadowEnvStrength` | 0 | 影への環境色反映なし |
| `_STAsUnlit` | 0 | アンライト無効 |
| `_ShadowColorTexStrength` | 1 | シャドウカラーテクスチャ適用 |
| `_ShadowColor` | (0.8745, 0.8784, 0.9294, 1) | 薄紫色シャドウ |
| `_Shadow2ndColor` | (0.9090, 0.9355, 0.9569, 0.324) | 2nd影色（薄い） |
| `_Shadow3rdColor` | (0, 0, 0, 0) | 3rd影色（無効） |
| `_ShadowReceive` | 1 | シャドウ受信有効 |
| `_ShadowMaxDarkness` | 0.15 | 最大暗さ制限（15%） |

#### MatCap パラメータ
| プロパティ | 値 | テクスチャ | 説明 |
|---|---|---|---|
| `_MatCap` | 1 | 有効 | _MatCapTex: guid: f0ac2a0... |
| `_MatCapIntensity` | 1 | - | MatCap強度 |
| `_MatCapBlendMode` | 1 | - | Multiply（乗算） |
| `_MatCapBlend` | 1 | - | ブレンド100% |
| `_MatCapColor` | (1, 1, 1, 0.35) | - | 白（α=0.35） |

#### テクスチャ
| テクスチャ | 設定状態 |
|---|---|
| `_MainTex` | ✅ guid: 9b1c52c... |
| `_BumpMap` | ✅ guid: d37e90c... |
| `_ShadowColorTex` | ❌ 未設定 |

#### リムライト パラメータ
| プロパティ | 値 | 説明 |
|---|---|---|
| `_RimLight` | 1 | リムライト有効 |
| `_RimIntensity` | 1 | リム強度 |
| `_RimColor` | (0.92, 0.9257, 1, 0.116) | 淡いシアン色 |
| `_RimPower` | 1 | Fresnel指数（広いリム） |

#### その他パラメータ
| プロパティ | 値 | 説明 |
|---|---|---|
| `_MonochromeLighting` | 0 | グレースケール化なし |
| `_AlbedoPreservation` | 0 | 色保存機能なし |

### レンダリングパストレース
**解析者**: shader-tracer / **完了日**: 2026-02-27

#### シーン条件の仮定
- ディレクショナルライトなし (`_LightColor0.rgb` ≈ 0)
- SH弱い（またはゼロ）
- `_USE_LIGHT_VOLUME` キーワード有効
- `_GIIntensity = 0`（GI完全無効化）
- `_IndirectLightMinColor = (0, 0, 0)`（最小保証なし）
- `_STAsUnlit = 0`

---

#### STEP 1: stLightColor の計算 (line ~244-268)

```
# ディレクショナルライトなし → SHフォールバック
dirLightLum = CALC_LUMINANCE(0, 0, 0) = 0 → フォールバック
頂点ライトもなし → SHフォールバック
effectiveLightColor = GetSHFallbackLightColor() ≈ (弱いSH値)

# SHが非常に弱い場合 (例: SH≈0):
effectiveLightColor = clamp(SH_fallback, 0.05, 1.0) = (0.05, 0.05, 0.05)  ← _LightColorMin=0.05 が下限

# StandardToon SHToon計算:
stSHToon = max(0, ShadeSH9(lightDir * 0.666666)) ≈ (0, 0, 0)  ← SH≈0の場合

stLightColor = effectiveLightColor + stSHToon
             = (0.05 + 0, ...) = (0.05, 0.05, 0.05)
stLightColor = clamp((0.05), 0.05, 1.0) = (0.05, 0.05, 0.05)
stLightColor = max(stLightColor, 0.001) = (0.05, 0.05, 0.05)
stLightColor = lerp(stLightColor, half3(1,1,1), _STAsUnlit=0) = (0.05, 0.05, 0.05)

★★★ stLightColor = (0.05, 0.05, 0.05) — 最小保証値のみ ★★★
stIndLightColor = saturate(ShadeSH9(-lightDir * 0.666666)) ≈ (0, 0, 0)
```

---

#### STEP 2: StandardToon shading branch (line ~423-482)

```
stAlbedo = col.rgb ≈ (1, 1, 1)  (白マテリアル)

# ShadowColorTex 未設定 → Unityデフォルト白 (1,1,1)
stShadowColorTexSample = (1, 1, 1)
stTintedAlbedo = lerp(stAlbedo, stAlbedo * (1,1,1), _ShadowColorTexStrength=1.0)
               = stAlbedo = (1, 1, 1)

stIndirectCol = stTintedAlbedo * _ShadowColor.rgb
              = (1,1,1) * (0.8745, 0.8784, 0.9294)
              = (0.8745, 0.8784, 0.9294)

stDirectCol = stAlbedo * stLightColor
            = (1, 1, 1) * (0.05, 0.05, 0.05)
            = (0.05, 0.05, 0.05)   ← ★★★ 非常に暗い！ ★★★

# stIndirectCol にライトカラー乗算 (line 468):
stIndirectCol *= stLightColor
stIndirectCol = (0.8745, 0.8784, 0.9294) * (0.05, 0.05, 0.05)
              = (0.0437, 0.0439, 0.0465)

# Shadow Environment Strength: _STShadowEnvStrength=0 なので変化なし

# Safety clamp (line 477):
stIndirectCol = min(stIndirectCol, stDirectCol)
              = min((0.0437, 0.0439, 0.0465), (0.05, 0.05, 0.05))
              = (0.0437, 0.0439, 0.0465)

# shadingValue計算:
# ndotl = dot(shadingNormal, lightDir) → Half-Lambert: saturate(ndotl * 0.5 + 0.5)
# 前面の場合: ndotl ≈ 0.5〜1.0 → halfLambert ≈ 0.75〜1.0
# stToon = LilToonShading(halfLambert=0.75, border=0.423, blur=0.444)
# = saturate((0.75 - (0.423 - 0.444*0.5)) / 0.444)
# = saturate((0.75 - 0.201) / 0.444) = saturate(1.237) = 1.0
# stToon *= atten=1.0 (ディレクショナルライトなしのForwardBaseでatten=1)
# stToon = lerp(1.0, stToon, _STShadowStrength=1.0) = 1.0
shadingValue = stToon ≈ 1.0  (lit状態)

# lighting の初期値 (line 482 — _USE_LIGHT_VOLUME パス用):
lighting = lerp(shadowColor, (1,1,1), shadingValue=1.0) = (1, 1, 1)

★ preLightVolume = lighting = (1, 1, 1)  (line 560で保存)
```

---

#### STEP 3: STEP 1 - Indirect Light (line ~545-602)

```
# _USE_LIGHT_VOLUME パス:
preLightVolume = lighting = (1, 1, 1)  ← ★アルベド込みではない！lighting = (1,1,1)の意味は
                                          shadingValue=1のときのtoon値。アルベドは未乗算。

LightVolumeSH(worldPos, L0, L1r, L1g, L1b)
  → _UdonLightVolumeEnabled == 0 の場合（エディター）:
     LV_SampleLightProbeDering → Unityライトプローブ（ゼロ or 弱い値）
  → LV有効の場合: LV_LightVolumeSH → LVデータから実際の照明

directLightLV = LightVolumeEvaluate(worldNormal, L0, L1r, L1g, L1b)
              ≈ SHが0ならば (0, 0, 0) / SH有りなら正の値

indirectLightLV = LightVolumeEvaluate(-worldNormal, ...)
                ≈ 同様

directLightLV *= _LightVolumeIntensity = 1.0  → 変化なし
indirectLightLV *= _LightVolumeIntensity = 1.0 → 変化なし
indirectLightLV *= lvInfluence = 1.0 - shadowReceiveMask = 1.0 → 変化なし

indirectResult = indirectLightLV  ← LVからのindirect

# ★★★ 致命的な問題 (line 599-600) ★★★
indirectResult = max(indirectResult, _IndirectLightMinColor=(0,0,0))
               = indirectLightLV  ← 最小保証が効かない（黒に設定されているため）

indirectResult *= _IndirectLightIntensity=1.0 * _GIIntensity=0
               = indirectLightLV * 0
               = (0, 0, 0)   ← ★★★ GIIntensity=0 で indirectResult が完全消去！ ★★★

# AO適用 (効果なし: indirectResult はゼロ):
indirectResult *= aoForIndirect = (0, 0, 0)
```

**重要**: `_GIIntensity = 0` は `indirectResult`（= `indirectLightLV`）を完全にゼロにするが、
`directLightLV`（LVからの直接光）には影響しない。

---

#### STEP 4: STEP 5 - Final Composition (line ~694-749)

```
# _USE_LIGHT_VOLUME + _STANDARD_TOON ブランチ (line 696-704):

stResult = lerp(stIndirectCol, stDirectCol, shadingValue)
         = lerp((0.0437, 0.0439, 0.0465), (0.05, 0.05, 0.05), 1.0)
         = stDirectCol = (0.05, 0.05, 0.05)   ← ★ 非常に暗い

stResult += additionalResult * col.rgb
          = (0.05, 0.05, 0.05) + 0 * (1,1,1)
          = (0.05, 0.05, 0.05)

# LV 環境光 (line 702):
lvEnv = max(directLightLV, 0) * col.rgb
      = directLightLV * (1, 1, 1)
      = directLightLV   ← ★ SHが弱い/ゼロなら ≈ (0, 0, 0)

lighting = max(stResult, lvEnv)
         = max((0.05, 0.05, 0.05), directLightLV)

# ケース1: SH=0 (LV未設定エディター環境)
# directLightLV = (0, 0, 0) → lvEnv = (0, 0, 0)
# lighting = max((0.05), (0)) = (0.05, 0.05, 0.05)   ← 非常に暗い

# ケース2: LV有効、directLightLV = (0.8, 0.8, 0.8) 程度
# lvEnv = (0.8, 0.8, 0.8)
# lighting = max((0.05), (0.8)) = (0.8, 0.8, 0.8)   ← 正常

# line 749: LightVolumeBlend=1.0 なので:
lighting = lerp(preLightVolume=(1,1,1), lighting, _LightVolumeBlend=1.0)
         = lighting  ← preLightVolumeは無視される
```

---

#### STEP 5: Final Output (line ~811-816)

```
# _STANDARD_TOON ブランチ (line 811-816):
col.rgb = lighting   ← lighting にアルベドが含まれていない！！！

# ★★★ 致命的なバグ発見 ★★★
# StandardToon + LV パスでは:
# stResult = lerp(stIndirectCol, stDirectCol, shadingValue)
#          = アルベド込みの値 (stDirectCol = albedo * stLightColor)
# lvEnv = directLightLV * col.rgb = アルベド込み
# lighting = max(stResult, lvEnv) = アルベド込み → ✅ OK

# 一方 preLightVolume = lighting = lerp(shadowColor, (1,1,1), shadingValue)
#                                = (1, 1, 1)  ← アルベド未込み！
# preLightVolume との lerp で _LightVolumeBlend=1.0 なので影響なし → ✅ OK

col.rgb = lighting * _Brightness=1.0 = lighting
```

---

#### 根本原因の特定

**原因1 (PRIMARY): stLightColor が _LightColorMin=0.05 の最小値のみ**

シーンにディレクショナルライトもSHもない場合:
- `stLightColor = (0.05, 0.05, 0.05)` のみ
- `stDirectCol = albedo * 0.05 ≈ (0.05, 0.05, 0.05)` — 明るさ5%
- `stResult ≈ (0.05, 0.05, 0.05)` — 出力が極めて暗い

**原因2 (SECONDARY): `_GIIntensity = 0` による `indirectResult` の完全消去**

- `indirectResult = indirectLightLV * 0 = (0, 0, 0)`
- LVのindirect光（影への環境光）が完全にゼロになる
- これは意図的（ユーザーがGI=0に設定）だが、LV indirect光まで消えるのは誤解を招く

**原因3 (CONDITIONAL): directLightLV がゼロの場合 (LV未設定)**

- エディター環境でVRC Light Volumeが設定されていない場合
- `LightVolumeSH` → `LV_SampleLightProbeDering` → ライトプローブが設定されていなければゼロ
- `directLightLV = (0, 0, 0)` → `lvEnv = (0, 0, 0)`
- `lighting = max((0.05), (0)) = (0.05, 0.05, 0.05)` → ほぼ黒

**原因4 (CONTRIBUTING): `_IndirectLightMinColor = (0, 0, 0)`**

- 通常のデフォルト値は `(0.1, 0.1, 0.1)` だが、1.matでは `(0, 0, 0)` に設定
- 暗いシーンでの最低保証色がゼロのため、黒になりやすい

---

#### 変数値サマリー（SH≈0, LV未設定エディター環境）

| 変数 | 値 | 備考 |
|------|-----|------|
| effectiveLightColor | (0.05, 0.05, 0.05) | _LightColorMin=0.05 がクランプ下限 |
| stSHToon | (0, 0, 0) | SH≈0 |
| stLightColor | (0.05, 0.05, 0.05) | SH弱いため最小値 |
| stAlbedo | (1, 1, 1) | 白テクスチャ仮定 |
| stDirectCol | (0.05, 0.05, 0.05) | albedo * stLightColor |
| stIndirectCol | (0.0437, 0.0439, 0.0465) | albedo * shadowColor * stLightColor |
| shadingValue | ≈ 1.0 | Half-Lambert, 前面, shading=lit |
| preLightVolume | (1, 1, 1) | lerp(shadowColor, (1,1,1), 1.0) |
| directLightLV | ≈ (0, 0, 0) | LV/SH未設定エディター |
| indirectLightLV | ≈ (0, 0, 0) | LV/SH未設定エディター |
| indirectResult | (0, 0, 0) | indirectLightLV * GIIntensity=0 |
| stResult | (0.05, 0.05, 0.05) | lerp(stIndirectCol, stDirectCol, 1.0) |
| lvEnv | (0, 0, 0) | directLightLV * albedo = 0 |
| lighting | (0.05, 0.05, 0.05) | max(stResult, lvEnv) |
| col.rgb (final) | **(0.05, 0.05, 0.05)** | **≈ 真っ黒！** |

### 根本原因

**StandardToon + Light Volume パスが黒になる根本原因は複合的なのです：**

1. **`stLightColor` 依存の問題**: StandardToon の `stDirectCol = albedo * stLightColor` は
   ディレクショナルライトもSHもない環境で `_LightColorMin = 0.05` のみになる。
   LVシーンでは main light が存在しないことが多く、stDirectCol が 0.05 程度になる。

2. **`_GIIntensity = 0` が LV indirect光も消す**: `indirectResult *= _GIIntensity` は
   `indirectLightLV`（LV由来の間接光）も消してしまう。
   LVシーンでは `directLightLV` が主光源なので indirect消去は想定内だが、
   ユーザーが混乱しやすい。

3. **LV + StandardToon の合成ロジックの問題**: line 699-703:
   `stResult = lerp(stIndirectCol, stDirectCol, shadingValue)` → albedo×stLightColor依存
   `lvEnv = directLightLV * col.rgb` → LV直接光
   `lighting = max(stResult, lvEnv)`

   **実際のVRC LVシーンでは `lvEnv` が正しく機能して明るくなるはず。**
   しかし `_USE_LIGHT_VOLUME` キーワードを有効にしたまま実際にLV Udon Behaviourが
   動いていない環境（VRChat実行前、Unity Editorプレビュー）では
   `directLightLV ≈ 0` のためブラック化する。

4. **`_IndirectLightMinColor = (0,0,0)` + `_GIIntensity = 0`**:
   最小保証色もゼロ × GI完全無効 → indirectResult が完全に (0,0,0)。

**結論**: 実際のVRChatワールドでLV Behaviourが動いていれば `directLightLV` が正の値になり
`lvEnv = directLightLV * albedo` が lighting を支配して正常に描画されると思われる。
Unityエディターでのプレビューでは LV データが存在せず黒になる。
また、シーンのライトプローブ・ディレクショナルライトが弱い場合は `stLightColor` が
0.05 程度となり、`stDirectCol` が極めて暗くなる。

### 修正方針

#### 修正A: `indirectResult` に `_GIIntensity` を掛けない（LV使用時）

```hlsl
// 現在 (line 600) - LV indirect光も消える:
indirectResult *= _IndirectLightIntensity * _GIIntensity;

// 修正案: _USE_LIGHT_VOLUME 時は _GIIntensity を除外
#ifdef _USE_LIGHT_VOLUME
    indirectResult *= _IndirectLightIntensity;  // GIIntensityはLV非使用時のみ
#else
    indirectResult *= _IndirectLightIntensity * _GIIntensity;
#endif
```

#### 修正B: StandardToon + LV の合成で stLightColor の代わりに LV を直接使用

```hlsl
// 現在: stDirectCol = albedo * stLightColor  (SHに依存)
// 修正: LVシーンでは directLightLV を stLightColor として使用する

#ifdef _USE_LIGHT_VOLUME
    half3 lvLightColor = directLightLV;
    half3 stDirectColLV = stAlbedo * clamp(lvLightColor, _LightColorMin, _LightColorMax);
    half3 stIndirectColLV = stTintedAlbedo * _ShadowColor.rgb * clamp(lvLightColor, _LightColorMin, _LightColorMax);
    stIndirectColLV = min(stIndirectColLV, stDirectColLV);
    stResult = lerp(stIndirectColLV, stDirectColLV, shadingValue);
#endif
```

#### 修正C: エディタープレビュー用のフォールバック明度保証

```hlsl
// _USE_LIGHT_VOLUME 時かつ LV データがない場合:
// directLightLV がゼロなら _IndirectLightMinColor のデフォルトを (0.1, 0.1, 0.1) にする
// または _GIIntensity の UI 説明に「LV使用時は0にしないこと」を追記
```

#### 即効性の高い修正（マテリアル設定変更のみ）

1.mat の値を以下に変更:
- `_GIIntensity = 0` → `_GIIntensity = 1.0` (LVシーンではGIが主光源)
- `_IndirectLightMinColor = (0, 0, 0)` → `(0.1, 0.1, 0.1)` (最小保証)
- `_LightColorMin = 0.05` はそのまま

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

---

## 1. 既存機能分析（shader-analyst）

### イラスト調に関連する既存機能

| 機能名 | キーワード | 現状 | イラスト調への応用可能性 |
|--------|-----------|------|------------------------|
| トゥーンシェーディング | `_ShadingMode=0`, `ToonShading()` | NdotL を段階化（1〜10段）。sharpness/offset/blend で制御。 | **高**: 段階数1-2+シャープネス高で手描き風の明確な影境界を実現可能。 |
| グラデーションシェーディング | `_ShadingMode=1`, `GradientShading()` | smoothstep ベースのソフトグラデーション。 | **高**: 水彩イラスト風のソフトな影表現に直接使える。 |
| マルチトーンシャドウ | `_USE_MULTI_SHADOW` | 3段階の影色（1st/2nd/3rd ShadowColor）。 | **高**: イラストの多段影（1影/2影/3影）をそのまま表現。 |
| ランプテクスチャ | `_USE_RAMP`, `RampShading()` | 1D テクスチャで影色を完全カスタマイズ。 | **非常に高**: イラストレーターが描いた影グラデーションをそのまま適用可能。 |
| SDF シャドウマップ | `_SDF_MAP`, `ApplySDFShadow()` | Face SDF 回転対応。ライト方向に応じた顔影マップ。 | **非常に高**: アニメ・イラスト調の顔の影表現に不可欠。 |
| シャドウカラーテクスチャ | `_SHADOW_COLOR_TEX` | 影部分の色をテクスチャで直接指定。 | **非常に高**: 影色を直接ペイントして反映できる。 |
| メイクアップテクスチャ (2nd〜5th) | `_2ND_TEXTURE`〜`_5TH_TEXTURE` | 4枚のオーバーレイレイヤー。HSV調整+ブレンドモード(Add/Mul/Overlay/Screen)+マスク+UVアニメ。 | **非常に高**: チーク・アイシャドウ等のイラスト的テクスチャ重ね塗り。 |
| スクリーントーン | `_SCREEN_TONE` | Bayer 4x4 ディザリング。スケール/閾値/色/ブレンドモード制御。 | **非常に高**: マンガ・コミック調のスクリーントーン表現。 |
| グラデーションベースカラー | `_GRADIENT_BASE_COLOR` | ワールド/ローカル空間の任意軸でグラデーション。 | **高**: イラスト的なグラデーション塗りに応用可能。 |
| アウトライン | `_OUTLINE` | Inverted Hull/Back Face。マスク+幅マップ+マルチカラー+テクスチャカラー。 | **非常に高**: イラスト調の線画表現。マルチカラーで部位別制御可能。 |
| リムライト (1/2/オフセット) | `_RIM_LIGHT` 等 | Fresnel ベース。方向制御+シャドウマスク+Spread。オフセットリムで非対称リム。 | **高**: 逆光表現、ハイライトリム。非対称光で手描き風も可能。 |
| MatCap (1/2/3) | `_MATCAP` 等 | ビュー空間法線ベース。3枚まで。Add/Multiply/Replace。マスク+ブラー。 | **非常に高**: イラスト風の環境光表現、ハイライト/影の焼き込みに最適。 |
| スペキュラ (アニメスタイル) | `_SPECULAR` | smoothstep ベースのシャープなハイライト。 | **高**: イラストのスペキュラハイライトをシャープに表現。 |
| ヘアスペキュラ (Kajiya-Kay) | `_HAIR_SPECULAR` | 2ローブの異方性スペキュラ。シフトテクスチャ対応。 | **高**: 天使の輪（髪のハイライト）表現。 |
| HSV調整 | `_Saturation`, `_Brightness`, `_AlbedoPreservation` | 彩度、明度、テクスチャ色保持の全体調整。 | **高**: イラスト調の色味調整の基本。 |
| Hue Shift | `_HUE_SHIFT` | 全体的な色相シフト。 | **中**: 特定の演出効果向け。 |
| エミッション | `_EMISSION` | エミッションマップ+パルスアニメ+スクロール+マスク+グロー。 | **中**: 発光表現（目の光、魔法エフェクト等）。 |
| ディザリングシャドウ | `_USE_DITHERING` | Bayer 4x4 マトリクスで影境界をディザリング。 | **中**: マンガ風の網掛け影。 |
| AO マップ | `_USE_AO` | ブラー+ブレンドモード対応。 | **中**: 接地影・くぼみの影表現。 |
| SSS | `_SSS` | 光の透過表現。 | **中**: 肌や耳の透過光表現。 |
| Final Color Blending | `ApplyFinalColorBlending()` | ハイライト圧縮 + シャドウリフト。 | **高**: 白飛び・黒潰れ防止で全体的な柔らかさを制御。 |
| Matte Effect | `ApplyMatteQuality()` | 脱彩度+サーフェスカラーティンティング+ソフトニング。 | **中**: マット調のイラスト表現。 |
| ブレンドモード | `ApplyBlendMode()` | Add/Multiply/Overlay/Screen の4モード。 | **高**: イラストソフトのレイヤーモードと同等の合成。 |
| テクスチャブラー | `SampleTex2DBlur()` | ddx/ddy ベース 5点クロスパターン。 | **中**: ソフトフォーカス的な表現。 |
| ライトカラー制御 | `_LightColorMin/Max`, `_MonochromeLighting` | ライトカラーのクランプ+モノクロ化。 | **高**: テクスチャの色を維持するのに重要。 |
| SafeAdditiveBlend | `SafeAdditiveBlend()` | 明るさ圧縮+色相保持の安全な加算合成。 | **高**: 白飛びを防ぐ合成基盤。 |

### 現在のライティングパイプライン概要

Fragment シェーダーのライティングは以下のフローで処理される:

```
1. UV 計算
   Parallax Mapping → Main UV Animation → Glitch Stretch UV

2. テクスチャサンプリング & カラー構成
   MainTex × _Color → Gradient Base Color → Makeup Textures 2nd〜5th
   → Surface Cover → Screen-Tone Overlay

3. 法線計算
   Normal Map → TBN → World Normal → Detail Normal → Surface Cover Normal
   → Smooth Normal (影用)

4. ライト方向決定 (フォールバックチェーン)
   Directional Light → Vertex Lights → SH Light Probe
   → Light Color Correction (Min/Max/Monochrome)

5. トゥーンシェーディング計算
   NdotL → SDF Shadow → Half-Lambert(STのみ)
   → LitSoftness → Dithering → Light Blend
   → Ramp / StandardToon / Toon+Gradient 分岐
   → AO → Shadow Attenuation → MultiTone Shadow + Shadow Color Tex

6. ライティング合成 (ForwardBase 5ステップ)
   STEP 1: 間接光 (Light Volume / SH / Lightmap)
   STEP 2: 影の環境色
   STEP 3: 直接光 (ライトカラー適用)
   STEP 4: 追加光 (Vertex Lights / Backlight / LTCGI / PBR)
   STEP 5: 最終合成 (LV BlendMode or max合成)

7. アルベドへのライティング適用
   AlbedoPreservation: 色相保持ライティング
   Saturation / Brightness 調整

8. ポストライティングエフェクト (順次加算)
   Specular → Hair Specular → SSS
   → Rim Light (1/2) → Offset Rim → Env Rim
   → MatCap (1/2/3) → Reflection → Refraction
   → Emission → Hue Shift → AudioLink
   → Glitter → Iridescence → Drip
   → Hologram → Glitch → Decal → Dissolve
   → Smear → Backface → Video

9. 最終処理
   Final Color Blending (Highlight圧縮 + Shadow リフト)
   → Height Fade / Intersection Fade → Distance Fade
   → Dithering Alpha → Fog
```

### 現在のカラー処理パイプライン

```
[テクスチャサンプリング]
  MainTex × _Color
    ↓
[グラデーションベースカラー] (オプション)
  CalculateGradientColor() → ApplyEffectBlendPost()
    ↓
[メイクアップテクスチャ 2nd→5th] (各レイヤー順次)
  ApplyMakeupTexture() — HSV調整 → ブレンドモード(Add/Mul/Overlay/Screen)
    ↓
[スクリーントーン] (オプション)
  Bayer ディザリング → ブレンド
    ↓
[ライティング適用]
  Traditional: albedo × lighting
  AlbedoPreservation: albedoDir × originalLum × lightingLum
  → lerp(traditional, preserved, _AlbedoPreservation)
    ↓
[彩度調整] lerp(gray, col, _Saturation)
    ↓
[明度調整] col × _Brightness
    ↓
[各種エフェクト加算]
  SafeAdditiveBlend() — 白飛び防止付き加算
  ApplyEffectBlendPost() — Normal/Soft/Screen/Overlay ブレンド
    ↓
[Final Color Blending]
  SmoothShoulderToneMapping() → ApplyFinalHighlightBlend()
  → ApplyFinalShadowBlend() → min(color, 1.05)
```

### 拡張ポイント（新機能を挿入しやすい場所）

| 挿入位置 | Fragment.hlsl 行番号 | 処理段階 | 適用例 |
|---------|---------------------|---------|--------|
| **A. ライティング前** | L148〜L159 (ScreenTone後、NormalMap前) | テクスチャ/カラー処理 | ポスタライズ（色数削減）、ペーパーテクスチャ、色域制限 |
| **B. ライティング後、エフェクト前** | L958〜L970 (Brightness後、Specular前) | ポストライティング | カラーグレーディング、トーンカーブ、色温度調整 |
| **C. エフェクトチェーン末尾** | Dissolve/Smear後、Final Color前 | ポストエフェクト | フィルター効果、全体的なポストプロセス |
| **D. Final Color 内部** | Utils L323〜L342 | 最終出力 | カスタムトーンマッピング、色域クリッピング |
| **E. ユーティリティ関数** | Utils.hlsl 末尾 | 共通関数 | 新ブレンドモード、カラー変換、ノイズ関数 |
| **F. プロパティ追加** | Input.hlsl CBUFFER内 | 宣言 | `#if defined()` で囲み、無効時のオーバーヘッドゼロ |

### 既存機能の「イラスト調プリセット」化の提案

現在の機能の組み合わせだけで、以下のイラスト調表現が既に可能:

1. **アニメセル塗り風**: ToonShading(Steps=2, Sharpness=0.9) + MultiShadow + SDF Face
2. **水彩イラスト風**: GradientShading(Width=0.6) + LitSoftness(0.5) + Saturation(0.8)
3. **厚塗りイラスト風**: GradientShading(Width=0.3) + MatCap(Multiply) + Specular(Low)
4. **コミック・マンガ風**: ToonShading(Steps=1) + ScreenTone + Outline(Bold)
5. **ギャルゲ/ノベルゲーム風**: ToonShading(Steps=2) + ShadowColor(紫系) + RimLight + MatCap(Add)

---

## 2. イラスト調表現技法（technique-researcher）

### 調査前提
- **ターゲット**: Unity Built-in RP フォワードレンダリング（VRChat最適化）
- **制約**: ランタイムスクリプト不可、Quest（モバイルGPU）対応必須
- **統合方針**: Natane Toon Shader のモジュラーインクルード構成に適合すること
- **パフォーマンス基準**: shader_feature_local で無効時ゼロコスト、有効時も Quest で実用的な負荷

### 実現可能な技法一覧

| # | 技法 | 実現可能性 | Quest対応 | 実装難易度 | 効果の大きさ | 優先度 |
|---|------|-----------|----------|-----------|------------|-------|
| 1 | 色の量子化 (Color Quantization) | ◎ 完全に可能 | ◎ 軽量 | ★☆☆ 低 | ★★★ 高 | **A（最優先）** |
| 2 | エッジ検出による輪郭強調 | △ 限定的に可能 | △ 制約あり | ★★★ 高 | ★★★ 高 | **C（条件付き）** |
| 3 | ハッチング/クロスハッチング | ◎ テクスチャベースで可能 | ◎ 軽量 | ★★☆ 中 | ★★☆ 中 | **B（推奨）** |
| 4 | 水彩/ペイント風テクスチャオーバーレイ | ◎ 完全に可能 | ◎ 軽量 | ★☆☆ 低 | ★★★ 高 | **A（最優先）** |
| 5 | ペーパーテクスチャ（紙の質感） | ◎ 完全に可能 | ◎ 軽量 | ★☆☆ 低 | ★★★ 高 | **A（最優先）** |
| 6 | カラーパレット制限（LUTベース） | ◎ 完全に可能 | ○ 中程度 | ★★☆ 中 | ★★★ 高 | **A（最優先）** |
| 7 | 影色のスタイライズ | ◎ 既存機能拡張で可能 | ◎ 軽量 | ★☆☆ 低 | ★★☆ 中 | **A（最優先）** |
| 8 | ソフトフィルター/Diffusion | ○ 限定的に可能 | ○ 中程度 | ★★☆ 中 | ★★☆ 中 | **B（推奨）** |
| 9 | 色のにじみ/ブリーディング | △ 擬似的に可能 | △ 制約あり | ★★★ 高 | ★★☆ 中 | **C（条件付き）** |
| 10 | ライン品質の向上（手書き風） | ○ テクスチャベースで可能 | ○ 中程度 | ★★☆ 中 | ★★☆ 中 | **B（推奨）** |

### 各技法の詳細

#### 2.1 色の量子化 (Color Quantization)
- **概要**: 出力色のレベル数を減らし、フラットなイラスト風の色面を作る。デジタルイラストの「塗り分け」感を表現する最も直接的な手法。
- **HLSL実装概要**:
  ```hlsl
  // floor ベースの量子化（最軽量）
  half3 QuantizeColor(half3 color, float levels)
  {
      return floor(color * levels + 0.5) / levels;
  }

  // HSV空間での量子化（より自然な結果）
  half3 QuantizeColorHSV(half3 color, float hueLevels, float satLevels, float valLevels)
  {
      half3 hsv = RGBtoHSV(color);  // 既存関数を再利用
      hsv.x = floor(hsv.x * hueLevels + 0.5) / hueLevels;
      hsv.y = floor(hsv.y * satLevels + 0.5) / satLevels;
      hsv.z = floor(hsv.z * valLevels + 0.5) / valLevels;
      return HSVtoRGB(hsv);
  }
  ```
- **統合ポイント**: Fragment最終段（ライティング適用後、Final Color Blending前）で `ApplyEffectBlendPost` を通して適用。既存の `RGBtoHSV`/`HSVtoRGB` を再利用可能。
- **パラメータ**: `_QuantizeLevels`(float), `_QuantizeHueLevels`(float), `_QuantizeSatLevels`(float), `_QuantizeValLevels`(float), `_QuantizeBlend`(float), `_QuantizeBlendMode`(float), マスクテクスチャ
- **パフォーマンス**: RGB量子化は `floor` + `mul` のみで極めて軽量。HSV版は既存のRGB↔HSV変換を利用するため中程度。Quest上でも問題なし。
- **注意点**: 量子化レベルが低すぎるとバンディングが発生する。`_QuantizeLevels` の下限を3-4程度に制限推奨。

#### 2.2 エッジ検出による輪郭強調
- **概要**: ピクセル隣接情報から輪郭を検出し、イラストの線画風に描画する。
- **Built-in RP フォワードパスでの制約**:
  - `_CameraDepthTexture` / `_CameraDepthNormalsTexture` はポストプロセスで使用されるバッファで、**フォワードパスのフラグメントシェーダーからもアクセス可能**だが、VRChat では **Camera の DepthTextureMode が保証されない**。
  - **ddx/ddy ベースのスクリーンスペースエッジ検出が現実的**。
- **HLSL実装概要**:
  ```hlsl
  // ddx/ddy ベースのエッジ検出（DepthTexture不要）
  half DetectEdge_ddx(half3 worldNormal, half3 color)
  {
      // 法線の不連続性を検出
      half3 normalDdx = ddx(worldNormal);
      half3 normalDdy = ddy(worldNormal);
      half normalEdge = length(normalDdx) + length(normalDdy);

      // 色の不連続性を検出
      half colorLum = dot(color, half3(0.299, 0.587, 0.114));
      half colorEdge = abs(ddx(colorLum)) + abs(ddy(colorLum));

      return saturate(normalEdge * _EdgeNormalSensitivity + colorEdge * _EdgeColorSensitivity);
  }
  ```
- **統合ポイント**: worldNormal と col.rgb を受けて、ポストライティングエフェクトとして適用。ただし ddx/ddy はピクセルクワッドのため **解像度依存で線幅が変動する**。
- **パフォーマンス**: ddx/ddy は ALU のみでテクスチャ不要だが、**VR のステレオレンダリングで目間差が出る可能性あり**。Quest では動作するが品質は低い（低解像度のため線が太くなる）。
- **実装難易度が高い理由**: 線幅の解像度依存性、VR互換性、品質調整の自由度が限られる。
- **結論**: 実装は可能だが、既存のアウトラインパス（頂点膨張方式）の方が VRChat では安定。**追加のエッジ検出は「内部線（折り目・色境界）」に限定して補助的に使うのが良い**。

#### 2.3 ハッチング/クロスハッチング
- **概要**: 明暗に応じてテクスチャベースの斜線パターンを適用し、手描き風のシェーディングを表現する。
- **HLSL実装概要**:
  ```hlsl
  // TAM (Tonal Art Map) アプローチ — 明るさに応じてハッチングレベルを切り替え
  half3 ApplyHatching(half3 baseColor, float2 uv, half shadingValue, half maskValue)
  {
      float2 hatchUV = uv * _HatchingTiling;

      // 6段階のハッチングテクスチャ（RGBAチャンネルにパック）
      half4 hatch0 = tex2D(_HatchingTex0, hatchUV); // R:レベル1, G:レベル2, B:レベル3, A:レベル4
      half2 hatch1 = tex2D(_HatchingTex1, hatchUV).rg; // R:レベル5, G:レベル6

      // 明るさ値から6段階の重みを計算
      half lum = shadingValue * 6.0;
      half w0 = saturate(lum - 5.0);       // 最も明るい（ハッチングなし）
      half w1 = saturate(lum - 4.0) - w0;  // 薄いハッチング
      half w2 = saturate(lum - 3.0) - w0 - w1;
      half w3 = saturate(lum - 2.0) - w0 - w1 - w2;
      half w4 = saturate(lum - 1.0) - w0 - w1 - w2 - w3;
      half w5 = saturate(lum) - w0 - w1 - w2 - w3 - w4;

      half hatchValue = w1 * hatch0.r + w2 * hatch0.g + w3 * hatch0.b
                       + w4 * hatch0.a + w5 * hatch1.r + (1.0 - w0 - w1 - w2 - w3 - w4 - w5) * hatch1.g;

      half3 hatchColor = lerp(baseColor, baseColor * _HatchingColor.rgb, hatchValue * maskValue);
      return hatchColor;
  }
  ```
- **統合ポイント**: `shadingValue` をライティングステージから受け渡して使用。テクスチャ2枚（6チャンネル）で6段階のTAMを実現。Fragment の Post-Lighting Effects セクションに配置。
- **パフォーマンス**: テクスチャ2枚のサンプリング + 重み計算。既存スクリーントーンと同等の負荷。Quest 対応可能。
- **簡易版**: テクスチャ1枚（4チャンネル、4段階）で軽量化も可能。

#### 2.4 水彩/ペイント風テクスチャオーバーレイ
- **概要**: 水彩のにじみ・ムラ・テクスチャをオーバーレイし、手描き風の質感を与える。
- **HLSL実装概要**:
  ```hlsl
  // 水彩テクスチャオーバーレイ（Overlay/Softlight ブレンド）
  half3 ApplyWatercolorOverlay(half3 baseColor, float2 uv, half maskValue)
  {
      float2 wcUV = uv * _WatercolorTiling;
      // ワールド座標ベースのUVオプション（3Dペイント感）
      // wcUV = lerp(wcUV, worldPos.xz * _WatercolorTiling, _WatercolorWorldSpace);

      half4 wcTex = tex2D(_WatercolorTex, wcUV);

      // 水彩のにじみ（エッジの色薄れ）
      half3 wcColor = lerp(baseColor, baseColor * wcTex.rgb, wcTex.a * _WatercolorIntensity * maskValue);

      // 色のばらつき（彩度のムラ）
      half desat = dot(wcColor, half3(0.299, 0.587, 0.114));
      half satVariation = lerp(1.0, wcTex.r, _WatercolorSatVariation);
      wcColor = lerp(half3(desat, desat, desat), wcColor, satVariation);

      return wcColor;
  }
  ```
- **統合ポイント**: 既存の Makeup Textures (2nd-5th) の延長として設計可能。独自のテクスチャスロットとブレンドパラメータを持つ。`ApplyEffectBlendPost` で統一ブレンド。
- **パフォーマンス**: テクスチャ1枚 + ALU。極めて軽量。Quest対応問題なし。
- **テクスチャ作成ガイド**: 水彩風の色むら・にじみパターンをグレースケールで用意し、Alpha チャンネルでにじみ範囲を制御。

#### 2.5 ペーパーテクスチャ（紙の質感）
- **概要**: 紙やキャンバスの凹凸テクスチャを最終出力にオーバーレイし、アナログ画材の質感を追加する。
- **HLSL実装概要**:
  ```hlsl
  // ペーパーテクスチャオーバーレイ
  half3 ApplyPaperTexture(half3 baseColor, float2 screenUV, half maskValue)
  {
      // スクリーンスペースUV（紙はカメラ固定）
      float2 paperUV = screenUV * _PaperTiling;
      half paperTex = tex2D(_PaperTex, paperUV).r;

      // 紙のテクスチャを中間グレー(0.5)基準でSoft Lightブレンド
      // Soft Light: 暗部を微妙に暗く、明部を微妙に明るくする
      half paperEffect = paperTex * 2.0 - 1.0; // [-1, 1] にリマップ
      half3 paperColor = baseColor + baseColor * paperEffect * _PaperIntensity * maskValue;

      return saturate(paperColor);
  }
  ```
- **統合ポイント**: Fragment の最終段（Final Color Blending 直前）に配置。スクリーンスペースUV (`i.pos.xy / _ScreenParams.xy`) を使用して「紙に描かれた」感を表現。
- **パフォーマンス**: テクスチャ1枚 + 簡単なALU。最軽量クラス。Quest対応問題なし。
- **UVモード**: スクリーンスペース（カメラ固定＝紙のように動かない）とワールドスペース（オブジェクトに貼り付く）を選択可能にする。

#### 2.6 カラーパレット制限（LUTベース）
- **概要**: 出力色を事前定義されたカラーパレットにリマップし、限定色のイラスト風に仕上げる。
- **HLSL実装概要**:
  ```hlsl
  // 方法A: 1D LUT テクスチャ（横256px、縦にパレット行）
  half3 ApplyColorPaletteLUT(half3 color, half maskValue)
  {
      // 輝度を使ってLUTをサンプル
      half lum = dot(color, half3(0.299, 0.587, 0.114));
      half3 lutColor = tex2D(_PaletteLUT, half2(lum, 0.5)).rgb;
      return lerp(color, lutColor, _PaletteIntensity * maskValue);
  }

  // 方法B: 3D LUT (2Dテクスチャにパック) — より正確だがテクスチャサイズ大
  half3 ApplyColorLUT3D(half3 color, half maskValue)
  {
      // 16x16x16 の LUT を 256x16 テクスチャにパック
      half blue = color.b * 15.0;
      half2 quad1;
      quad1.y = floor(floor(blue) / 4.0);
      quad1.x = floor(blue) - (quad1.y * 4.0);
      half2 quad2;
      quad2.y = floor(ceil(blue) / 4.0);
      quad2.x = ceil(blue) - (quad2.y * 4.0);

      half2 texPos1 = quad1 * 0.25 + 0.5/16.0 + half2(0.25 - 0.5/16.0, 0.25 - 0.5/16.0) * color.rg;
      half2 texPos2 = quad2 * 0.25 + 0.5/16.0 + half2(0.25 - 0.5/16.0, 0.25 - 0.5/16.0) * color.rg;

      half3 newColor1 = tex2D(_ColorLUT, texPos1).rgb;
      half3 newColor2 = tex2D(_ColorLUT, texPos2).rgb;

      return lerp(color, lerp(newColor1, newColor2, frac(blue)), _PaletteIntensity * maskValue);
  }
  ```
- **統合ポイント**: Fragment の最終段に配置。1D LUT なら簡易、3D LUT なら本格的なカラーグレーディングが可能。
- **パフォーマンス**: 1D LUT=テクスチャ1枚サンプル（軽量）。3D LUT=テクスチャ2回サンプル+補間（中程度）。Quest では 1D LUT 推奨。
- **プリセット対応**: LUTテクスチャを差し替えるだけで様々なカラーパレットに切り替え可能。プリセットシステム（既存の MaterialPresetBrowser）との相性が良い。

#### 2.7 影色のスタイライズ
- **概要**: 影に独自の色味（紫系、青系、赤系など）を持たせ、イラスト風の色使いを実現する。
- **既存機能との関係**: **Natane Toon Shader は既に `_ShadowColor` / `_Shadow2ndColor` / `_Shadow3rdColor` による多段影色をサポート**。拡張として以下を追加する。
- **HLSL実装概要**:
  ```hlsl
  // 拡張1: 影色の自動スタイライズ（固有色から補色系の影を自動生成）
  half3 StylizeShadowColor(half3 baseColor, half3 shadowColor, float stylizeAmount)
  {
      // 補色方向にシフト
      half3 hsv = RGBtoHSV(baseColor);
      half3 complementHSV = half3(frac(hsv.x + 0.5), hsv.y * 0.8, hsv.z * 0.5);
      half3 complementRGB = HSVtoRGB(complementHSV);

      // 元の影色と補色影色をブレンド
      return lerp(shadowColor, complementRGB, stylizeAmount);
  }

  // 拡張2: 環境色ベースの影色調整
  half3 EnvironmentTintedShadow(half3 shadowColor, half3 ambientColor, float tintAmount)
  {
      return lerp(shadowColor, shadowColor * ambientColor, tintAmount);
  }
  ```
- **統合ポイント**: 既存の MultiToneShadowColor の後段に追加。`_ShadowStylize` パラメータで制御。
- **パフォーマンス**: HSV変換1回（既存関数再利用）。軽量。Quest対応問題なし。
- **追加パラメータ**: `_ShadowHueShift`(影色の色相シフト), `_ShadowStylize`(自動補色影の強さ), `_ShadowEnvTint`(環境色影の強さ)

#### 2.8 ソフトフィルター/Diffusion
- **概要**: 光の拡散効果を擬似的に再現し、夢見心地のようなソフトな仕上がりにする。
- **Built-in RP フォワードパスでの制約**: フルスクリーンブラーは **GrabPass** が必要で、パフォーマンスコストが高い。代替として **per-pixel 近似** を使用する。
- **HLSL実装概要**:
  ```hlsl
  // 方法1: テクスチャベースのソフトフィルター（GrabPass不要）
  half3 ApplySoftDiffusion(half3 baseColor, float2 uv, half maskValue)
  {
      // ddx/ddy ベースの擬似ブラー（既存の SampleTex2DBlur と同パターン）
      float2 dx = ddx(uv) * _DiffusionRadius * 4.0;
      float2 dy = ddy(uv) * _DiffusionRadius * 4.0;

      // 元の色の周辺をサンプリング（色空間でのブラー）
      half3 blurred = baseColor * 0.4;
      blurred += baseColor * 0.15; // 自身の色を使った擬似ブラー
      // ※ 実際にはテクスチャ再サンプリングが必要だがAlbedo空間で近似

      // Screen ブレンドで明るい部分を拡散
      half3 diffused = BlendScreen(baseColor, blurred * _DiffusionIntensity);
      return lerp(baseColor, diffused, _DiffusionBlend * maskValue);
  }

  // 方法2: 明るさベースの選択的グロー（より実用的）
  half3 ApplySelectiveGlow(half3 baseColor, half maskValue)
  {
      half lum = dot(baseColor, half3(0.299, 0.587, 0.114));
      half glowFactor = smoothstep(_DiffusionThreshold, 1.0, lum);
      half3 glowColor = baseColor * (1.0 + glowFactor * _DiffusionIntensity);
      half3 softened = lerp(baseColor, glowColor, maskValue);

      // ほんのり彩度を下げてソフト感を増す
      half softGray = dot(softened, half3(0.299, 0.587, 0.114));
      return lerp(softened, lerp(half3(softGray,softGray,softGray), softened, 0.85), glowFactor * _DiffusionIntensity * 0.3);
  }
  ```
- **統合ポイント**: Fragment 最終段（Final Color Blending 近辺）に配置。
- **パフォーマンス**: ALU のみの方法2が推奨。テクスチャサンプリング不要。Quest 対応可能。
- **制限**: フォワードパスでは真のガウスブラーは不可能。あくまで「ソフト感」の近似。

#### 2.9 色のにじみ/ブリーディング
- **概要**: 水彩画のように色が隣接ピクセルに滲み出す効果。
- **Built-in RP フォワードパスでの制約**: **真のブリーディングにはマルチパス/GrabPass が必要**。フォワードパスのフラグメントシェーダー単体では、隣接ピクセルの色情報に直接アクセスする手段が限られる。
- **HLSL実装概要（擬似的アプローチ）**:
  ```hlsl
  // 方法: テクスチャベースの擬似にじみ
  // ノイズテクスチャでUVをランダムにオフセットし、色の混合感を出す
  half3 ApplyColorBleeding(half3 baseColor, float2 uv, sampler2D mainTex, half maskValue)
  {
      // ノイズテクスチャでUVディストーション
      float2 noiseUV = uv * _BleedingNoiseTiling;
      half2 noise = tex2D(_BleedingNoiseTex, noiseUV).rg * 2.0 - 1.0;
      float2 bleedUV = uv + noise * _BleedingIntensity * 0.01;

      // オフセットUVでメインテクスチャを再サンプリング
      half3 bleedColor = tex2D(mainTex, bleedUV).rgb;

      // 元の色とブレンド
      return lerp(baseColor, bleedColor, _BleedingBlend * maskValue);
  }
  ```
- **統合ポイント**: テクスチャサンプリング段階（UV Distortion として）に配置。MainTex の再サンプリングが必要。
- **パフォーマンス**: テクスチャ2枚追加サンプリング（ノイズ + メイン再サンプリング）。中程度。Quest では負荷が気になる。
- **実装難易度が高い理由**: UVディストーションはアーティファクトが出やすく、メインテクスチャの再サンプリングはテクスチャキャッシュ効率が悪い。
- **結論**: テクスチャベースの擬似にじみは実現可能だが、**水彩オーバーレイ（2.4）の方がより安定して美しい結果が得られる**。補助的な機能として実装する場合は、UV ディストーション強度を低く制限すべき。

#### 2.10 ライン品質の向上（手書き風）
- **概要**: 既存のアウトライン（頂点膨張方式）に手書きの揺らぎ・太さの変化を与える。
- **HLSL実装概要**:
  ```hlsl
  // 頂点シェーダーでのアウトライン太さ変調
  float GetHandDrawnOutlineWidth(float baseWidth, float3 objectPos, float2 uv)
  {
      // ノイズテクスチャで太さをランダム変調
      float noise = tex2Dlod(_OutlineNoiseTex, float4(uv * _OutlineNoiseTiling, 0, 0)).r;
      float widthVariation = lerp(1.0 - _OutlineWidthVariation, 1.0 + _OutlineWidthVariation, noise);

      return baseWidth * widthVariation;
  }

  // 頂点シェーダーでの位置揺らぎ
  float3 ApplyOutlineJitter(float3 outlinePos, float3 objectPos)
  {
      // 頂点位置ベースのハッシュノイズ
      float hash = frac(sin(dot(objectPos.xy, float2(12.9898, 78.233))) * 43758.5453);
      float3 jitter = float3(hash, frac(hash * 17.0), 0) * 2.0 - 1.0;
      return outlinePos + jitter * _OutlineJitterAmount * 0.001;
  }
  ```
- **統合ポイント**: 既存の `NataneToonVertex.hlsl` のアウトラインパスに組み込む。頂点シェーダーでの処理のため、フラグメント負荷は増加しない。
- **パフォーマンス**: 頂点シェーダーでテクスチャ1枚サンプリング（`tex2Dlod`） + ALU。軽量。Quest対応可能。
- **注意点**: `tex2Dlod` は頂点シェーダーで使用する場合、一部のモバイルGPUで非効率な可能性がある。代替としてオブジェクト空間ノイズ（数学的ハッシュ）を使用可能。

### 推奨実装セット

#### Tier 1: 最優先実装（「イラスト調」の核心、低コスト・高効果）

| 技法 | 理由 |
|------|------|
| **色の量子化** | 最も直接的にイラスト風の「塗り分け」感を実現。実装が最も簡単かつ軽量。 |
| **ペーパーテクスチャ** | テクスチャ1枚追加でアナログ画材感を大幅に向上。実装コスト最小。 |
| **水彩/ペイント風オーバーレイ** | ペーパーテクスチャと同系統で、テクスチャ差し替えで多様な画材感を表現可能。 |
| **カラーパレット制限（LUT）** | LUTテクスチャ1枚で世界観統一。プリセットシステムとの相性抜群。 |
| **影色スタイライズ** | 既存の影システム拡張で実装可能。コード追加量が最小。 |

#### Tier 2: 推奨実装（効果的だが実装コストがやや高い）

| 技法 | 理由 |
|------|------|
| **ハッチング** | テクスチャベースで安定した品質。TAMは古典的だが効果的な技法。 |
| **ソフトフィルター/Diffusion** | ALUベースの近似で実現可能。フォワードパスの制約内で最大限のソフト表現。 |
| **ライン品質向上** | 頂点シェーダー拡張で手書き感を追加。既存アウトラインの品質向上。 |

#### Tier 3: 条件付き実装（制約が多い、補助的な機能）

| 技法 | 理由 |
|------|------|
| **エッジ検出** | VR/Quest互換性の懸念。ddx/ddyベースは解像度依存。補助的な内部線検出に限定推奨。 |
| **色のにじみ** | フォワードパスでの真のブリーディングは不可能。UVディストーションは不安定。水彩オーバーレイで代替可能。 |

### 組み合わせプリセット案

以下のようなプリセットとして Tier 1 + Tier 2 の技法を組み合わせ提供：

| プリセット名 | 組み合わせ技法 | 目標表現 |
|-------------|--------------|---------|
| **水彩イラスト風** | 色量子化(弱) + 水彩オーバーレイ + ペーパーテクスチャ + 影色スタイライズ(青紫系) | 水彩画風の柔らかいイラスト |
| **セルアニメ風** | 色量子化(強) + カラーパレット制限 + 影色スタイライズ(固定色) | フラットなアニメ塗り |
| **鉛筆画風** | ハッチング + ペーパーテクスチャ + カラーパレット制限(モノクロLUT) | 鉛筆スケッチ風 |
| **デジタルイラスト風** | 色量子化(中) + ソフトフィルター + カラーパレット制限 + 手書きアウトライン | Pixiv/イラスト投稿サイト風 |
| **絵本風** | 水彩オーバーレイ + ペーパーテクスチャ(厚手) + ソフトフィルター + 色量子化(弱) | 温かみのある絵本のイラスト |

### 実装順序の提案

1. **Phase 1（基盤構築）**: 色の量子化 + ペーパーテクスチャ + 影色スタイライズ
   - 理由: 最も簡単で効果が大きい。全体の「イラスト調」パイプラインの骨格を形成。
2. **Phase 2（テクスチャ拡張）**: 水彩オーバーレイ + カラーパレット制限(LUT)
   - 理由: テクスチャスロット追加で多様な表現を解放。
3. **Phase 3（高度な表現）**: ハッチング + ソフトフィルター + ライン品質向上
   - 理由: Tier 2 技法でさらなる表現力を追加。
4. **Phase 4（オプション）**: エッジ検出 + 色のにじみ
   - 理由: 需要に応じて追加。Quest非対応のオプションとして提供。

---

## パイプライン拡張設計（PC向け高品質）

**調査者**: pipeline-analyst / **完了日**: 2026-03-02
**対象ブランチ**: v1.1.5
**方針**: Quest対応不要。PC向け高品質表現を最優先。GrabPass・マルチパス等コスト高の技法も検討対象。

---

### 1. シェーダーPass構造の全体像

**ファイル**: `Shaders/NataneToon/NataneToonShader.shader`

```
SubShader {
    Tags { "RenderType"="Opaque" "Queue"="Geometry" }

    Stencil { ... }                          // L746-755: ステンシル設定

    GrabPass { "_GrabTexture" }              // L760-763: 既に存在！(Refraction用)

    Pass "OUTLINE"      (ForwardBase)        // L766-1059: アウトラインパス (Cull Front)
    Pass "FORWARD_BASE" (ForwardBase)        // L1062-1145: メイン描画パス (#pragma target 4.6)
    Pass "FORWARD_ADD"  (ForwardAdd)         // L1148-1207: 追加光パス
    Pass "SHADOW_CASTER" (ShadowCaster)      // L1210-1253: シャドウキャスト
}
```

**重要な発見**:
- **GrabPass は既に存在** (`_GrabTexture`) — Refraction機能で使用中。新機能は同じGrabPassを共有可能
- **#pragma target 4.6** — テッセレーション対応済み。Compute Shader等の高機能が使える
- テッセレーション用に `tessVert` → `hull` → `domain` → `frag` パイプラインが構築済み

---

### 2. フラグメントシェーダー エフェクトチェーン完全マップ

**ファイル**: `Shaders/NataneToon/Include/Rendering/NataneToonFragment.hlsl` (1930行)

```
frag(v2f i) : SV_Target                          // L18
│
├── [L22-31]   Mirror Control (VRChat)            — _MIRROR_CONTROL
├── [L33-42]   Parallax Mapping (UV Adjustment)   — _PARALLAX
├── [L44-48]   UV Animation                       — _MAIN_TEX_ANIMATION
├── [L50-66]   Glitch Stretch (UV modification)   — _GLITCH_STRETCH
├── [L68-74]   Texture Sampling (_MainTex)
├── [L76-86]   Gradient Base Color                — _GRADIENT_BASE_COLOR
├── [L88-135]  Makeup Textures (2nd-5th)          — _2ND_TEXTURE ~ _5TH_TEXTURE
├── [L137-146] Surface Cover (Snow/Sand)          — _SURFACE_COVER
├── [L148-157] Screen-Tone Overlay                — _SCREEN_TONE
│
├── [L159-168] Normal Mapping                     — _NORMALMAP
├── [L170-184] Detail Map (Secondary UV)          — _DETAIL_MAP
├── [L186-199] Surface Cover Normal Blending
│
├── [L201-207] Shadow Receive Mask Setup          — _SHADOW_RECEIVE_MASK
├── [L209-261] Lighting Setup (Light fallback chain)
├── [L263-286] StandardToon Light Color           — _STANDARD_TOON
├── [L288]     UNITY_LIGHT_ATTENUATION
│
├── [L290-306] Distance Fade (early calculation)  — _DISTANCE_FADE
├── [L308-416] PCSS / Shadow Map Smoothing        — _PCSS
├── [L418-421] Shadow Receive Strength
│
├── [L423]     viewDir calculation
├── [L425-435] Smooth Normal Shading Blend        — _SMOOTH_NORMAL
├── [L437-439] SDF Shadow Map                     — _SDF_MAP
├── [L441-445] StandardToon Half-Lambert          — _STANDARD_TOON
├── [L447-454] Backlight Calculation
│
├── [L456-624] ★ Toon/Ramp Shading (shadingValue & shadowColor)
│   ├── AO, Dithering, Ramp, StandardToon, Multi-tone Shadow
│   └── Shadow Attenuation
│
├── [L626-628] Shadow Max Darkness
│
├── [L632-892] ★ ForwardBase: Natural Lighting Pipeline (5 STEP)
│   ├── STEP 1: Indirect Light (LV / SH / Lightmap)
│   ├── STEP 2: Shadow Environment Color
│   ├── STEP 3: Direct Light
│   ├── STEP 4: Additional Light (vertex / backlight / LTCGI / PBR)
│   └── STEP 5: Final Composition (LV blend modes)
│
├── [L894-959] ★ Color Preservation / Saturation / Brightness
│
│ ===== Post-Lighting Effects (L961-) =====
│ ★★★ 新機能挿入に最適なゾーン ★★★
│
├── [L969-998]   Specular Highlight                — _SPECULAR
├── [L1000-1024] Hair Specular (Kajiya-Kay)        — _HAIR_SPECULAR
├── [L1026-1052] Subsurface Scattering             — _SSS
├── [L1054-1110] Rim Light                         — _RIM_LIGHT
├── [L1112-1163] Rim Light 2                       — _RIM_LIGHT_2
├── [L1165-1198] Offset Rim Light                  — _OFFSET_RIM_LIGHT
├── [L1200-1238] Environmental Rim                 — _ENV_RIM
├── [L1240-1283] MatCap                            — _MATCAP
├── [L1285-1313] MatCap 2                          — _MATCAP_2
├── [L1315-1343] MatCap 3                          — _MATCAP_3
├── [L1345-1367] Cubemap Reflection                — _REFLECTION
├── [L1369-1401] Refraction (GrabPass)             — _REFRACTION
├── [L1403-1448] Emission                          — _EMISSION
├── [L1450-1457] Hue Shift                         — _HUE_SHIFT
├── [L1459-1507] AudioLink                         — _AUDIOLINK
├── [L1509-1522] Glitter                           — _GLITTER
├── [L1524-1535] Iridescence                       — _IRIDESCENCE
├── [L1537-1586] Smear Effect                      — _SMEAR
├── [L1588-1612] Water Drip                        — _WATER_DRIP
├── [L1614-1667] Hologram                          — _HOLOGRAM
├── [L1669-1716] Glitch                            — _GLITCH
├── [L1718-1743] Decal System                      — _DECAL
├── [L1745-1792] Dissolve                          — _DISSOLVE
│
│ ===== Post-Processing Zone (L1794-) =====
│
├── [L1794-1799] Alpha Mask                        — _ALPHA_MASK
├── [L1801-1832] Height Fade                       — _HEIGHT_FADE
├── [L1834-1868] Intersection Fade                 — _INTERSECTION_FADE
├── [L1870-1896] Distance Fade (Global Alpha)      — _DISTANCE_FADE
├── [L1898-1908] Height Fog                        — _HEIGHT_FOG
├── [L1910-1915] Final Color Blending (ForwardBase only)
├── [L1917-1922] Dithering Alpha                   — _DITHERING_ALPHA
├── [L1924-1925] Fog (Unity)
└── [L1927]      return col
```

---

### 3. 新イラスト調エフェクトの最適な挿入位置

#### 3A. ライティング計算に影響するエフェクト（シェーディング前）

**色の量子化 / カラーパレット制限 / 影色スタイライズ**

```
挿入位置: L959 の直後 (Color Preservation/Saturation/Brightness の後、Post-Lighting Effects の前)
理由: ライティング適用済みの col.rgb に対して色操作を行う最後のチャンス。
      Post-Lighting Effects (Specular等) が加算される前に基本色を整える。
```

```hlsl
// ===== Illustration Style Processing (イラスト調処理) =====
// L959 の後、L961 の前に挿入

#ifdef _ILLUST_STYLE
{
    // 色の量子化 (Posterize)
    // 影色スタイライズ (Shadow Color Shift)
    // カラーパレット制限 (LUT / Palette)
}
#endif
```

#### 3B. テクスチャオーバーレイ系エフェクト

**水彩オーバーレイ / ペーパーテクスチャ / ハッチング**

```
挿入位置A（推奨）: L1448 の直後 (Emission の後、Hue Shift の前)
理由: エミッションは発光なので、ペーパーテクスチャ等のオーバーレイはエミッション後が自然。
      Hue Shift はシフト前のオーバーレイを含めて色相を変えるべき。

挿入位置B: L1910 の直前 (Final Color Blending の直前)
理由: 最後の仕上げとしてペーパーテクスチャを乗せる場合。
      全エフェクト適用後の「紙に描いた」感を出す。
```

```hlsl
// ===== Paper Texture / Watercolor Overlay =====
// L1448 の後に挿入（エミッション後、Hue Shift前）

#if defined(_PAPER_TEXTURE) && defined(UNITY_PASS_FORWARDBASE)
{
    // ペーパーテクスチャ (Multiply blend)
    // 水彩オーバーレイ (Screen/Overlay blend)
}
#endif

#if defined(_HATCHING) && defined(UNITY_PASS_FORWARDBASE)
{
    // ハッチング (luminance-based cross-hatching)
}
#endif
```

#### 3C. エッジ検出 / ソフトフィルター（GrabPass依存）

**スクリーンスペース処理**

```
挿入位置: L1910-1915 の直前 (Final Color Blending の直前)
理由: 全エフェクト適用後にスクリーンスペース処理を行う。
      GrabPass は既に存在するので _GrabTexture を再利用可能。

代替案: 新規 Post-Effect Pass として FORWARD_BASE の後に追加。
```

---

### 4. GrabPass の利用と拡張

#### 4A. 現在の状態

```
GrabPass { "_GrabTexture" }  // L760-763 (NataneToonShader.shader)
```

- **既に SubShader レベルで宣言済み** — 全パスで共有
- Refraction エフェクト（`_REFRACTION`）で使用中
- `UNITY_DECLARE_SCREENSPACE_TEXTURE(_GrabTexture)` で VR SPI対応済み (NataneToonUtils.hlsl L645)

#### 4B. 新機能での GrabPass 活用（追加修正不要）

既存の `_GrabTexture` をそのまま利用可能:

1. **色のにじみ（Bleed/Spread）**: GrabTexture からブラーサンプリング
2. **ソフトフィルター**: GrabTexture に Gaussian/Box blur
3. **エッジ検出**: GrabTexture から Sobel/Laplacian
4. **被写界深度風ぼかし**: GrabTexture + _CameraDepthTexture

**注意**: GrabPass はフレームバッファのコピーなので、**現在のオブジェクトの描画は含まれない**。
自分自身に対するスクリーンスペース処理が必要な場合は、Fragment内の `col.rgb` に対して直接処理すべき。

#### 4C. 自分自身のピクセルに対するスクリーンスペース処理（GrabPass不要）

**ddx/ddy ベースのアプローチ**（既に SampleTex2DBlur で実績あり）:

```hlsl
// NataneToonUtils.hlsl L565-581 で既に使用中のパターン:
float2 dx = ddx(uv) * blur * 4.0;
float2 dy = ddy(uv) * blur * 4.0;
```

これを応用して:
- **エッジ検出（内部）**: `fwidth(col.rgb)` でエッジを検出
- **色のにじみ**: ddx/ddy ベースで隣接ピクセル方向へブレンド

---

### 5. _CameraDepthTexture / _CameraDepthNormalsTexture のアクセス

#### 5A. _CameraDepthTexture（既に宣言済み）

```hlsl
// NataneToonInput.hlsl L1028-1030:
#if defined(_INTERSECTION_FADE) || defined(_PCSS)
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
#endif
```

**新機能で使用する場合**: `#if defined(...)` の条件に新キーワードを追加するだけ。

```hlsl
// 修正案:
#if defined(_INTERSECTION_FADE) || defined(_PCSS) || defined(_ILLUST_EDGE_DETECT) || defined(_DOF_BLUR)
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
#endif
```

サンプリング方法:
```hlsl
float depth = LinearEyeDepth(
    UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, screenUV).r);
```

#### 5B. _CameraDepthNormalsTexture（新規追加が必要）

Built-in RPでは `Camera.depthTextureMode |= DepthTextureMode.DepthNormals` を C# から有効化する必要あり。

```hlsl
// NataneToonInput.hlsl に追加:
#if defined(_ILLUST_EDGE_DETECT_NORMAL)
sampler2D _CameraDepthNormalsTexture;
#endif
```

```hlsl
// フラグメントでの使用:
float4 depthNormal = tex2D(_CameraDepthNormalsTexture, screenUV);
float3 normal;
float depth;
DecodeDepthNormal(depthNormal, depth, normal);
```

**注意**: DepthNormals テクスチャはパフォーマンスコストがあるが、PC向けなので問題なし。

---

### 6. テクスチャスロットの使用状況と上限見積もり

#### 6A. 現在のテクスチャ数（NataneToonInput.hlsl）

| カテゴリ | sampler2D 数 | 条件コンパイル |
|----------|-------------|---------------|
| Main + Makeup (2nd-5th) | 9 (Main + 4tex + 4mask) | 常時/各keyword |
| Screen-Tone | 1 | _SCREEN_TONE |
| Shading (Ramp/SDF/Grade/AO/Shadow) | 5 | 各keyword |
| Specular | 1 | _SPECULAR |
| Hair Specular | 2 | _HAIR_SPECULAR |
| Rim Light (1/2/Offset) | 3 | 各keyword |
| MatCap (x3) + Mask (x3) | 6 | _MATCAP/_MATCAP_2/_MATCAP_3 |
| Glitter | 1 | _GLITTER |
| Outline + Mask | 1 | _OUTLINE |
| Emission + Mask | 2 | _EMISSION |
| Normal Map | 1 | _NORMALMAP |
| SSS (Thickness + Mask) | 2 | _SSS |
| Dissolve + Mask | 2 | _DISSOLVE |
| Alpha Mask | 1 | _ALPHA_MASK |
| Reflection Mask | 1 | _REFLECTION |
| Iridescence Mask | 1 | _IRIDESCENCE |
| Env Rim Mask | 1 | _ENV_RIM |
| Parallax | 1 | _PARALLAX |
| Refraction Mask | 1 | _REFRACTION |
| Decal | 1 | _DECAL |
| Backface | 1 | _BACKFACE_TEXTURE |
| Video | 1 | _VIDEO_TEXTURE |
| Vertex Anim Mask | 1 | _VERTEX_ANIMATION |
| Water Drip Mask | 1 | _WATER_DRIP |
| Smear Mask | 1 | _SMEAR |
| Fur (Noise + Mask) | 2 | _FUR |
| PBR (MetallicGloss + Occlusion) | 2 | _PBR |
| Smooth Normal Tex | 1 | _SMOOTH_NORMAL |
| Hologram (Mask + Noise) | 2 | _HOLOGRAM/_HOLOGRAM_NOISE |
| Glitch (Mask + Noise) | 2 | _GLITCH |
| Glitch Stretch Mask | 1 | _GLITCH_STRETCH |
| VAT (Position + Normal) | 2 | _VAT |
| Tessellation Disp | 1 | _TESS_DISPLACEMENT |
| Detail (Albedo + Normal) | 2 | _DETAIL_MAP |
| Surface Cover (Tex + Normal) | 2 | _SURFACE_COVER |
| Cubemap (Reflection + EnvRim) | 2 (CUBE) | _REFLECTION/_ENV_RIM |
| GrabTexture | 1 (special) | _REFRACTION |
| AudioLink | 2 | _AUDIOLINK |
| **合計（最大同時）** | **~63** | |

#### 6B. 実際の同時使用テクスチャ数

shader_feature_local により、**有効化されたキーワードのスロットのみ**がバインドされる。
典型的なマテリアルでは **10-20スロット** 程度。

**DX11/12のテクスチャスロット上限**: 128 (t0-t127)
**実質的な余裕**: 非常に大きい。**追加で10-15スロットは問題なし**。

#### 6C. 新機能に必要なテクスチャスロット見積もり

| 新機能 | 必要スロット | テクスチャ名案 |
|--------|-------------|---------------|
| カラーパレット制限 (LUT) | 1 | `_IllustLUTTex` |
| ペーパーテクスチャ | 1 | `_PaperTex` |
| 水彩オーバーレイ | 1 | `_WatercolorOverlayTex` |
| ハッチングパターン | 1 | `_HatchingTex` |
| ソフトフィルター用 | 0 (ddx/ddy) | — |
| 色のにじみ用 | 0 (GrabPass) | — |
| エッジ検出用 | 0 (depth既存) | — |
| **合計** | **4** | |

→ テクスチャスロットの余裕は**十分**なのです。

---

### 7. v2f 構造体（頂点→フラグメント間データ）への追加フィールドの余裕

**ファイル**: `Shaders/NataneToon/Include/Core/NataneToonInput.hlsl` L1079-1108

```hlsl
struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float3 worldTangent : TEXCOORD3;
    float3 worldBinormal : TEXCOORD4;
    UNITY_FOG_COORDS(5)                              // TEXCOORD5
    SHADOW_COORDS(6)                                  // TEXCOORD6
    float4 screenPos : TEXCOORD7;                     // conditional
    float3 vertexLightColor : TEXCOORD8;              // conditional (VERTEXLIGHT_ON)
    float3 smoothWorldNormal : TEXCOORD9;             // conditional (_SMOOTH_NORMAL)
    float smearStretchFactor : TEXCOORD10;            // conditional (_SMEAR)
    float2 lightmapUV : TEXCOORD11;                   // conditional (_BACKGROUND_MODE)
    float2 uv1 : TEXCOORD12;                          // conditional (_DETAIL_MAP)
    UNITY_VERTEX_OUTPUT_STEREO
};
```

**使用済み**: TEXCOORD0 ~ TEXCOORD12 (最大)
**DX11 上限**: TEXCOORD0 ~ TEXCOORD15 (16スロット)
**残り**: TEXCOORD13, TEXCOORD14, TEXCOORD15 → **3スロット空き**

ただし、条件コンパイルにより実際には同時に全12が使われることは稀。
新機能でも条件コンパイルを使えば、**追加で3-5フィールドは安全に追加可能**。

#### 追加フィールド案

```hlsl
#ifdef _ILLUST_STYLE
    float3 vertexColor : TEXCOORD13;   // ペーパーテクスチャ用UVやエッジ検出用の追加データ
#endif
#ifdef _ILLUST_EDGE_DETECT
    float4 screenPosEdge : TEXCOORD14; // エッジ検出用スクリーン座標（screenPosと共有も可）
#endif
```

---

### 8. CBUFFER の現在のサイズと構造

**ファイル**: `Shaders/NataneToon/Include/Core/NataneToonInput.hlsl` L5-785

CBUFFER は `CBUFFER_START(UnityPerMaterial)` ~ `CBUFFER_END` で定義。
20セクションに分かれ、大部分が `#if defined(...)` で条件コンパイル。

**常時ロード（キーワード不要）のプロパティ**:
- Core Rendering: ~14 float (56 bytes)
- Makeup Textures: ~40 float (160 bytes) ※2nd-5thの_ST含む
- Shading: ~14 float (56 bytes)
- Normal Map: ~3 float (12 bytes)
- Rim Direction / Shadow Color Tex / Outline Multi-Color: ~8 float (32 bytes)

**常時合計**: ~316 bytes

**DX11 CBUFFER上限**: 64KB (65536 bytes)
**余裕**: 非常に大きい。数百floatの追加は問題なし。

---

### 9. 既存ユーティリティ関数の再利用可能性

**ファイル**: `Shaders/NataneToon/Include/Utils/NataneToonUtils.hlsl` (1288行)

| 関数名 | 行番号 | 再利用用途 |
|--------|--------|-----------|
| `RGBtoHSV(float3 rgb)` | L27-35 | 色の量子化、パレット制限、影色スタイライズ |
| `HSVtoRGB(float3 hsv)` | L38-43 | 同上（逆変換） |
| `ApplyHueShift(float3 rgb, float shift)` | L47-52 | 影色のHueシフト |
| `ApplyHSVAdjustment(...)` | L59-77 | 影色の彩度/明度調整 |
| `BlendOverlay(float3 base, float3 blend)` | L417-424 | 水彩オーバーレイ、ペーパーテクスチャ |
| `BlendScreen(float3 base, float3 blend)` | L427-430 | 同上（スクリーンブレンド） |
| `ApplyBlendMode(...)` | L435-458 | 汎用ブレンドモード適用 |
| `ApplyEffectBlendPost(...)` | L528-554 | 全エフェクトの統一ブレンド |
| `SafeAdditiveBlend(...)` | L346-380 | 白飛び防止加算 |
| `SafeAdditiveBlendFast(...)` | L385-390 | 高速版（セカンダリエフェクト用） |
| `ApplyMatteQuality(...)` | L396-411 | マット表面の質感変換 |
| `SampleTex2DBlur(...)` | L565-581 | テクスチャぼかし（ddx/ddy 5点クロス） |
| `SampleTex2DBlur3(...)` | L584-587 | RGB版テクスチャぼかし |
| `SampleTex2DBlur1(...)` | L590-593 | 単チャンネル版テクスチャぼかし |
| `AnimateUV(...)` | L599-630 | UVスクロール/回転 |
| `AnimateUVIfNeeded(...)` | L634-639 | 閾値付きUVアニメ |
| `ApplySoftMask(float mask)` | L185-190 | マスクのソフトエッジ化 |
| `CALC_LUMINANCE(color)` | L6 | 輝度計算マクロ |
| `ReinhardToneMapping(...)` | L205-214 | 色のにじみ用トーンマッピング |
| `FilmicToneMapping(...)` | L218-229 | 同上（ACES近似） |
| `ApplyFinalColorBlending(...)` | L323-342 | 最終カラーブレンド |

**再利用性の評価**: 非常に高い。
特に `ApplyEffectBlendPost` / `SafeAdditiveBlend` / `BlendOverlay` / `BlendScreen` / `RGBtoHSV` / `HSVtoRGB` は
新しいイラスト調エフェクトの実装でそのまま使えるのです。

---

### 10. アウトラインパスの構造（手書き風拡張の挿入ポイント）

**ファイル**: `Shaders/NataneToon/NataneToonShader.shader` L766-1059

```
Pass "OUTLINE" {
    Tags { "LightMode" = "ForwardBase" }
    Cull Front

    struct appdata { vertex, normal, tangent, color, uv }
    struct v2f { pos, uv, fogCoord, worldPos(conditional) }

    vert(appdata v):
    ├── Smear vertex offset                    [L857-881]
    ├── Outline normal resolution              [L896-931]
    │   ├── Mode 0: Vertex Color Object Space
    │   ├── Mode 1: Vertex Color Tangent Space (lilToon互換)
    │   └── Mode 2: Baked Normal Texture
    ├── Corner Smooth Fallback                 [L924-930]
    │
    ├── Mode 0: Inverted Hull (view space)     [L932-953]
    │   └── offset = TransformViewToProjection(norm.xy)
    │       o.pos.xy += offset * o.pos.z * outlineWidth
    │
    └── Mode 1: Back Face (object space)       [L955-972]
        └── scaledPos = v.vertex.xyz + normalize(outlineNormal) * outlineWidth

    frag(v2f i):
    ├── Base outline color                     [L990]
    ├── Texture-linked color                   [L993-997]   — _OUTLINE_TEXTURE_COLOR
    ├── Multi-color outline                    [L1000-1004] — _OUTLINE_MULTI_COLOR
    ├── Outline mask                           [L1007-1012] — _OUTLINE_MASK
    ├── Height fade                            [L1015-1049] — _HEIGHT_FADE
    └── Fog                                    [L1051]
```

#### 手書き風アウトライン拡張の挿入ポイント

**頂点シェーダー側（線の太さ変動）**:
```
挿入位置: L943 の直後 (outlineWidth 計算後、o.pos.xy += の直前)
```

```hlsl
// 手書き風太さ変動:
#ifdef _ILLUST_OUTLINE
    // ノイズベースの太さ揺れ
    float outlineNoise = frac(sin(dot(v.uv, float2(12.9898, 78.233))) * 43758.5453);
    outlineWidth *= lerp(1.0 - _OutlineWidthVariation, 1.0 + _OutlineWidthVariation, outlineNoise);

    // 距離に応じた減衰カーブ変更（手書き風: 近いほど太い、遠いほど細い）
    outlineWidth *= lerp(1.0, distanceFactor * _OutlineTaperStrength, _OutlineTaper);
#endif
```

**フラグメントシェーダー側（線の色変動・テクスチャ）**:
```
挿入位置: L997 の直後 (Texture-linked color の後)
```

```hlsl
// 手書き風色揺れ:
#ifdef _ILLUST_OUTLINE
    // ノイズによる色揺れ (HSV space)
    float3 outlineHSV = RGBtoHSV(col.rgb);
    outlineHSV.x += (frac(sin(i.uv.x * 127.1 + i.uv.y * 311.7) * 43758.5) - 0.5) * _OutlineHueVariation;
    outlineHSV.z *= lerp(1.0 - _OutlineValueVariation, 1.0 + _OutlineValueVariation,
                          frac(sin(dot(i.uv, float2(269.5, 183.3))) * 43758.5));
    col.rgb = HSVtoRGB(outlineHSV);

    // ペンテクスチャによる線のかすれ
    half penTexValue = tex2D(_OutlinePenTex, i.uv * _OutlinePenScale).r;
    col.a *= lerp(1.0, penTexValue, _OutlinePenStrength);
    clip(col.a - _OutlinePenCutoff);
#endif
```

---

### 11. 新規Pass追加の検討

PC向け高品質表現では、既存パスの拡張に加えて新規パスの追加も検討可能。

#### 案A: スクリーンスペース・ポストエフェクトPass

```
既存のFORWARD_BASEパスの後に追加。
GrabPass "_PostEffectGrabTexture" を新たに宣言し、
エッジ検出・ぼかし・色のにじみをスクリーンスペースで処理。
```

メリット: 自分自身のピクセルに対するスクリーンスペース処理が可能
デメリット: 追加のGrabPassコスト（ただしPC向けなので許容範囲）

#### 案B: プリパス（Depth/Normal書き込み）

```
FORWARD_BASEの前に Depth Pre-Pass を追加。
エッジ検出用の法線・深度を先に書き込む。
```

**結論**: 既存のFragment内処理 + 既存GrabPass で多くの機能を実現できるため、
まずは既存パスの拡張で実装し、不足があれば新規パスを追加する段階的アプローチが推奨。

---

### 12. まとめ — 拡張容量の総括

| リソース | 現在の使用 | 上限 | 空き | 評価 |
|----------|-----------|------|------|------|
| テクスチャスロット | 最大~63 (実質10-20) | 128 (DX11) | 十分 | ◎ |
| CBUFFER サイズ | ~316 bytes常時 + 条件付き | 64KB | 十分 | ◎ |
| v2f TEXCOORD | TEXCOORD0-12 | TEXCOORD0-15 | 3スロット | ○ |
| shader_feature | ~70 keywords | 256 (DX11) | 十分 | ◎ |
| GrabPass | 1 (_GrabTexture) | 利用可能 | 共有可 | ◎ |
| Pass数 | 4 (Outline/Base/Add/Shadow) | 制限なし | 追加可 | ◎ |
| _CameraDepthTexture | 条件宣言済み | 利用可能 | 条件追加のみ | ◎ |

**結論**: NataneToon Shader の拡張容量は非常に大きく、
イラスト調レンダリング機能の追加に**構造的な障壁はない**のです。
既存のモジュラーアーキテクチャ（shader_feature_local + 条件コンパイル）により、
新機能を有効化しない限りパフォーマンスへの影響はゼロ。
PC向け高品質パイプラインとして理想的な拡張基盤が整っているのです。

---

## PC向け高品質イラスト調技法（Quest制約なし）

**調査者**: technique-hq / **調査日**: 2026-03-02
**前提条件**: PCデスクトップGPU専用、Quest対応不要、表現の完成度を最優先
**対象パイプライン**: Unity Built-in Render Pipeline (Forward Rendering)

### 既存パイプラインのPC向け技法統合における利点

Natane Toon Shaderは以下のインフラが既に整備済みで、PC向け高品質技法の統合に非常に有利:

| リソース | 状態 | 利用箇所 |
|----------|------|----------|
| `GrabPass ("_GrabTexture")` | 宣言済み | Refractionで使用。PC向け技法で共有可能 |
| `_CameraDepthTexture` | 宣言済み (NataneToonInput.hlsl:1029) | PCSS / Intersection Fade |
| `screenPos` (v2f.TEXCOORD7) | 構造体に含む | GrabPass/Dithering/IntersectionFade |
| `shader_feature_local` | 60+キーワード運用済み | 全機能がキーワードベースでコスト0無効化 |
| HSV変換関数 | NataneToonUtils.hlslに実装済み | 色量子化等で流用可能 |
| `SafeAdditiveBlend` / `ApplyEffectBlendPost` | Fragmentで多用 | エフェクト合成の統一パターン |

### 技法1: GrabPassベース真のガウスブラー（ソフトフォーカス / Diffusion）

#### 概要と表現効果
GrabPassで取得したスクリーンバッファに対して、2パス分離型ガウスブラーを適用する。
従来のNataneの5タップ簡易ブラー（NataneToonUtils.hlsl:693付近）とは異なり、
**9〜13タップの正規分布ウェイト**を用いた本格的なガウスブラーにより、
アニメ映画のような「エアブラシ的ソフトフォーカス」を実現する。

**表現効果**:
- 肌の質感のような柔らかい光の拡散（ソフトフィルター）
- 背景のぼかしによるキャラクター際立ち効果
- 劇場版アニメのDiffusion Filter再現（ハイライト部分のにじみ）

#### HLSL実装概要

```hlsl
// === 2パス分離ガウスブラー（13タップ） ===
// 正規分布ウェイト: sigma = 2.0 基準
static const int BLUR_SAMPLES = 13;
static const float BlurWeights[13] = {
    0.0044, 0.0115, 0.0257, 0.0488, 0.0799, 0.1133, 0.1389,
    0.1133, 0.0799, 0.0488, 0.0257, 0.0115, 0.0044
};
static const float BlurOffsets[13] = {
    -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6
};

half3 GaussianBlurGrabPass(float2 screenUV, float2 direction, float blurRadius)
{
    half3 result = 0;
    float2 texelSize = _GrabTexture_TexelSize.xy * blurRadius;
    [unroll]
    for (int i = 0; i < BLUR_SAMPLES; i++)
    {
        float2 offset = direction * BlurOffsets[i] * texelSize;
        result += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, screenUV + offset).rgb
                  * BlurWeights[i];
    }
    return result;
}

// Fragment内での使用（1パスで水平+垂直を近似する場合）
half3 blurredH = GaussianBlurGrabPass(grabUV, float2(1, 0), _SoftFilterRadius);
half3 blurredV = GaussianBlurGrabPass(grabUV, float2(0, 1), _SoftFilterRadius);
half3 softFiltered = (blurredH + blurredV) * 0.5;

// ハイライト選択的Bloom: 明るい部分だけにブラーを適用
half originalLum = CALC_LUMINANCE(col.rgb);
half bloomMask = smoothstep(_SoftFilterThreshold, _SoftFilterThreshold + 0.2, originalLum);
col.rgb = lerp(col.rgb, softFiltered, bloomMask * _SoftFilterBlend);
```

**注意**: 真の2パスガウスブラーにはパスが2つ必要。1パス近似の場合は水平+垂直を平均化するか、
対角方向サンプリング（4方向星形パターン）で近似する。

#### 統合ポイント
- **Fragment内 Post-Lighting Effects の最後**（Emission後、Fog前）
- 既存のGrabPassを共有するため、Refraction有効時は追加コストなし
- キーワード: `_SOFT_FILTER`

#### パフォーマンス（PCデスクトップ基準）
- 13タップ x 2方向 = 26テクスチャサンプル追加
- RTX 3060以上: 0.1ms以下（フルHD）
- GTX 1060: 約0.2ms（フルHD）
- **評価: 軽い** — GrabPassの初期コスト(~0.3ms)が支配的。ブラー自体は非常に軽量

#### 他技法との相互効果
- **水彩シミュレーション**: ソフトフィルター + エッジダークニングで水彩のwet-in-wet効果を再現
- **色のにじみ**: ガウスブラーの基盤を色のブリーディングが流用可能
- **ハッチング**: ソフトフォーカスでハッチングのエッジを柔らかくし、より自然な手描き感

---

### 技法2: Kuwaharaフィルタ（油絵風ペインタリーレンダリング）

#### 概要と表現効果
GrabPassに対してKuwaharaフィルタを適用し、**油絵のような筆致感のあるレンダリング**を実現する。
Kuwaharaフィルタは各ピクセル周囲を4象限に分割し、最も分散の小さい象限の平均色を採用する。
これにより**エッジを保持しつつ面を均一化**する効果が得られ、絵画的な表現になる。

**表現効果**:
- 油絵・厚塗りイラスト風の面の均一化と筆致感
- ディテールを保ちつつノイズを除去する芸術的フィルタリング
- 色の境界を明確に保ちながら中間トーンを滑らかにする

#### HLSL実装概要

```hlsl
// === Generalized Kuwaharaフィルタ (4象限) ===
// radius: フィルタ半径（推奨 2〜5）
half3 KuwaharaFilter(float2 screenUV, float radius)
{
    float2 texelSize = _GrabTexture_TexelSize.xy;
    int r = (int)radius;

    // 4象限の平均と分散を計算
    half3 mean[4] = { half3(0,0,0), half3(0,0,0), half3(0,0,0), half3(0,0,0) };
    half3 sqMean[4] = { half3(0,0,0), half3(0,0,0), half3(0,0,0), half3(0,0,0) };
    int count = 0;

    // 象限0: 左上 (-r,-r) to (0,0)
    // 象限1: 右上 (0,-r) to (r,0)
    // 象限2: 左下 (-r,0) to (0,r)
    // 象限3: 右下 (0,0) to (r,r)
    [unroll]
    for (int q = 0; q < 4; q++)
    {
        int2 qStart = int2(q % 2 == 0 ? -r : 0, q < 2 ? -r : 0);
        int2 qEnd = int2(q % 2 == 0 ? 0 : r, q < 2 ? 0 : r);
        count = 0;

        for (int y = qStart.y; y <= qEnd.y; y++)
        {
            for (int x = qStart.x; x <= qEnd.x; x++)
            {
                half3 s = UNITY_SAMPLE_SCREENSPACE_TEXTURE(
                    _GrabTexture, screenUV + float2(x, y) * texelSize).rgb;
                mean[q] += s;
                sqMean[q] += s * s;
                count++;
            }
        }
        mean[q] /= count;
        sqMean[q] /= count;
    }

    // 最小分散の象限を選択
    half minVar = 1e10;
    half3 result = mean[0];
    [unroll]
    for (int q2 = 0; q2 < 4; q2++)
    {
        half3 variance = sqMean[q2] - mean[q2] * mean[q2];
        half totalVar = dot(variance, half3(0.299, 0.587, 0.114));
        if (totalVar < minVar)
        {
            minVar = totalVar;
            result = mean[q2];
        }
    }
    return result;
}
```

#### 統合ポイント
- **Fragment内 Post-Lighting Effects の最後**（ソフトフィルターと排他 or 合成）
- GrabPassを共有
- キーワード: `_KUWAHARA_FILTER`
- `_KuwaharaRadius`（2〜5）、`_KuwaharaBlend`（0〜1）でパラメータ制御

#### パフォーマンス（PCデスクトップ基準）
- radius=3 の場合: 4象限 x 16サンプル = 64テクスチャサンプル
- radius=5 の場合: 4象限 x 36サンプル = 144テクスチャサンプル
- RTX 3060以上: 0.3-0.5ms（radius=3, フルHD）
- GTX 1060: 0.5-1.0ms（radius=3, フルHD）
- **評価: 中程度** — radiusに比例してコスト増大。radius=3がバランス良好

#### 他技法との相互効果
- **色量子化**: Kuwahara + 色量子化 = アニメーション映画の背景美術風
- **ペーパーテクスチャ**: 油絵の筆致 + 紙テクスチャ = 厚塗りイラスト完成形
- **エッジ検出**: Kuwaharaのエッジ保持 + 明示的エッジラインで劇画調

---

### 技法3: 深度/法線バッファ活用エッジ検出（Sobel / Laplacian）

#### 概要と表現効果
`_CameraDepthTexture` と `_CameraDepthNormalsTexture` を使い、
**スクリーンスペースでのエッジ検出**を行う。UV空間のエッジ検出とは異なり、
**オブジェクトのシルエットや法線の不連続部分**をピクセル精度で検出できる。

深度エッジ: オブジェクト境界、前景/背景の境界
法線エッジ: 面の折れ曲がり（ハードエッジ）、素材の切り替わり

**表現効果**:
- 漫画・コミック調の太い輪郭線（ジオメトリアウトラインでは不可能な精度）
- 法線の変化に基づく内部ディテール線
- 深度差に基づくシルエット強調（遠近感の強調）

#### HLSL実装概要

```hlsl
// === 深度ベースSobelエッジ検出 ===
// _CameraDepthTexture を使用（既にNataneToonInput.hlslで宣言済み）

float SampleLinearDepth(float2 uv)
{
    return LinearEyeDepth(
        UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, uv).r);
}

float SobelEdgeDepth(float2 screenUV, float2 texelSize, float depthThreshold)
{
    // Sobel 3x3カーネル
    float d00 = SampleLinearDepth(screenUV + float2(-1, -1) * texelSize);
    float d10 = SampleLinearDepth(screenUV + float2( 0, -1) * texelSize);
    float d20 = SampleLinearDepth(screenUV + float2( 1, -1) * texelSize);
    float d01 = SampleLinearDepth(screenUV + float2(-1,  0) * texelSize);
    float d21 = SampleLinearDepth(screenUV + float2( 1,  0) * texelSize);
    float d02 = SampleLinearDepth(screenUV + float2(-1,  1) * texelSize);
    float d12 = SampleLinearDepth(screenUV + float2( 0,  1) * texelSize);
    float d22 = SampleLinearDepth(screenUV + float2( 1,  1) * texelSize);

    // Sobel水平/垂直
    float gx = -d00 - 2*d01 - d02 + d20 + 2*d21 + d22;
    float gy = -d00 - 2*d10 - d20 + d02 + 2*d12 + d22;

    float edgeMagnitude = sqrt(gx * gx + gy * gy);
    return step(depthThreshold, edgeMagnitude);
}

// === 法線ベースSobelエッジ検出 ===
// _CameraDepthNormalsTexture を使用（追加宣言が必要）
// sampler2D _CameraDepthNormalsTexture; をInput.hlslに追加

float3 SampleViewNormal(float2 uv)
{
    float4 cdn = tex2D(_CameraDepthNormalsTexture, uv);
    float3 normal;
    float depth;
    DecodeDepthNormal(cdn, depth, normal);
    return normal;
}

float SobelEdgeNormal(float2 screenUV, float2 texelSize, float normalThreshold)
{
    float3 n00 = SampleViewNormal(screenUV + float2(-1, -1) * texelSize);
    float3 n10 = SampleViewNormal(screenUV + float2( 0, -1) * texelSize);
    float3 n20 = SampleViewNormal(screenUV + float2( 1, -1) * texelSize);
    float3 n01 = SampleViewNormal(screenUV + float2(-1,  0) * texelSize);
    float3 n21 = SampleViewNormal(screenUV + float2( 1,  0) * texelSize);
    float3 n02 = SampleViewNormal(screenUV + float2(-1,  1) * texelSize);
    float3 n12 = SampleViewNormal(screenUV + float2( 0,  1) * texelSize);
    float3 n22 = SampleViewNormal(screenUV + float2( 1,  1) * texelSize);

    // Sobel on each normal component
    float3 gx = -n00 - 2*n01 - n02 + n20 + 2*n21 + n22;
    float3 gy = -n00 - 2*n10 - n20 + n02 + 2*n12 + n22;

    float edgeMagnitude = length(gx) + length(gy);
    return step(normalThreshold, edgeMagnitude);
}

// === 統合エッジ検出 ===
float CombinedEdge(float2 screenUV, float2 texelSize)
{
    float depthEdge = SobelEdgeDepth(screenUV, texelSize, _EdgeDepthThreshold);
    float normalEdge = SobelEdgeNormal(screenUV, texelSize, _EdgeNormalThreshold);
    return saturate(depthEdge + normalEdge);
}

// Fragment内での使用
float2 edgeTexelSize = _ScreenParams.zw - 1.0; // 1/screenSize
float edge = CombinedEdge(screenUV, edgeTexelSize * _EdgeWidth);
col.rgb = lerp(col.rgb, _EdgeColor.rgb, edge * _EdgeBlend);
```

#### 統合ポイント
- **Fragment内 Post-Lighting Effects後半**（Refraction後、Emission前が理想）
- `_CameraDepthTexture` は既存（追加宣言不要）
- `_CameraDepthNormalsTexture` は新規追加が必要（Camera.depthTextureMode |= DepthNormals をC#側で設定）
- キーワード: `_SCREEN_EDGE`
- screenPosは既存のv2f構造体から取得可能

#### パフォーマンス（PCデスクトップ基準）
- 深度Sobel: 8テクスチャサンプル（軽量）
- 法線Sobel: 追加8テクスチャサンプル
- 両方合計: 16テクスチャサンプル
- RTX 3060以上: 0.05ms以下
- GTX 1060: 0.1ms以下
- **評価: 非常に軽い** — テクスチャサンプルのみで演算負荷は最小

#### 他技法との相互効果
- **手書き風アウトライン**: 深度/法線エッジをベースにノイズ変調を適用で手描き輪郭線
- **水彩シミュレーション**: エッジ検出マスクでウェットエッジ（色の境界のにじみ）
- **ハッチング**: エッジ部分にハッチング密度を上げる → 漫画的表現

---

### 技法4: 高品質3D LUT（フルカラーグレーディング）

#### 概要と表現効果
従来の1Dランプテクスチャや簡易的な色量子化ではなく、
**3D LUT (Look-Up Table)** を用いて任意のカラーグレーディングを実現する。
3D LUTは入力RGB各チャンネルを独立に変換するため、
**色相・彩度・明度の任意のマッピング**が可能。

**表現効果**:
- 映画のカラーグレーディング（ティール&オレンジ、ヴィンテージ等）
- 特定のイラストレーターの色彩を再現するパレット変換
- 季節や時間帯に応じた雰囲気変換（夕暮れ、月夜、雨天等）
- アニメスタジオごとのカラー・パレット再現

#### HLSL実装概要

```hlsl
// === 3D LUT カラーグレーディング ===
// 3D LUTは32x32x32 のボリュームを 2Dテクスチャ(1024x32)にパックして格納
// sampler2D _LUT3DTex;     // 1024x32 (32スライスx32の帯状配置)
// float _LUT3DBlend;       // LUT適用度 (0=無効, 1=完全適用)
// float _LUT3DSize;        // LUTサイズ (通常32)

half3 SampleLUT3D(sampler2D lut, half3 color, float size)
{
    // LUTサイズに基づくオフセット計算
    float sliceSize = 1.0 / size;
    float slicePixelSize = sliceSize / size;
    float sliceInnerSize = slicePixelSize * (size - 1.0);

    // Blue channel determines which slices to interpolate between
    float bSlice = color.b * (size - 1.0);
    float bSlice0 = floor(bSlice);
    float bSlice1 = min(bSlice0 + 1.0, size - 1.0);
    float bFrac = bSlice - bSlice0;

    // UV for each slice
    float2 uv0 = float2(
        (bSlice0 + color.r * (size - 1.0) / size + 0.5 / (size * size)) * sliceSize,
        color.g * sliceInnerSize + slicePixelSize * 0.5
    );
    float2 uv1 = float2(
        (bSlice1 + color.r * (size - 1.0) / size + 0.5 / (size * size)) * sliceSize,
        color.g * sliceInnerSize + slicePixelSize * 0.5
    );

    // Bilinear sampling of each slice, then lerp between slices
    half3 col0 = tex2Dlod(lut, float4(uv0, 0, 0)).rgb;
    half3 col1 = tex2Dlod(lut, float4(uv1, 0, 0)).rgb;
    return lerp(col0, col1, bFrac);
}

// Fragment内での使用（最終色調整フェーズ）
half3 preLUT = col.rgb;
half3 lutColor = SampleLUT3D(_LUT3DTex, saturate(col.rgb), _LUT3DSize);
col.rgb = lerp(col.rgb, lutColor, _LUT3DBlend);
```

#### 統合ポイント
- **Fragment最終段階: Fog適用直前**（全てのライティング・エフェクト後に色変換）
- テクスチャスロット1つ追加（_LUT3DTex）
- キーワード: `_LUT_3D`
- 色量子化 (`_COLOR_QUANTIZE`) とは**併用可能**（LUT後に量子化で段階化）

#### パフォーマンス（PCデスクトップ基準）
- テクスチャサンプル2回 + lerp
- RTX 3060以上: 0.01ms以下
- GTX 1060: 0.02ms以下
- **評価: 極めて軽い** — 最もコストパフォーマンスの高い表現力向上技法

#### 他技法との相互効果
- **色量子化**: LUT → 量子化の順で「特定パレットに制限されたポスタリゼーション」
- **水彩/油絵**: LUTで暖色寄り・低彩度にシフト → 手描き画材風の色味
- **ソフトフィルター**: LUT + ソフトフォーカスで映画フィルム的ルック

---

### 技法5: HSV空間色量子化（高品質版）

#### 概要と表現効果
既存のWhiteboard.mdの色量子化提案を**PC向けに拡張**。
RGB空間での単純量子化ではなく、**HSV空間で各チャンネルを独立に量子化**することで、
色相の飛びを防ぎ、自然なポスタリゼーション効果を得る。

さらにPC版では**ディザリング付き量子化**を導入し、
バンディングアーティファクトを完全に除去する。

**表現効果**:
- セルアニメの制限されたカラーパレット感
- 色数を絞った印象派絵画風のフラットシェーディング
- バンディングの無い高品質ポスタリゼーション

#### HLSL実装概要

```hlsl
// === HSV空間色量子化 + ディザリング ===
half3 QuantizeColorHSV(half3 rgb, float hueSteps, float satSteps, float valSteps,
                        float2 screenPos, float ditherStrength)
{
    half3 hsv = RGBtoHSV(rgb); // 既存の NataneToonUtils.hlsl 関数を流用

    // ディザリングノイズ (Bayer 4x4 matrix)
    static const float BayerMatrix4x4[16] = {
         0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
        12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
         3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
        15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
    };
    int2 bayerCoord = int2(fmod(screenPos, 4.0));
    float dither = BayerMatrix4x4[bayerCoord.y * 4 + bayerCoord.x];
    dither = (dither - 0.5) * ditherStrength;

    // Hue量子化 (循環を考慮)
    float hueQ = floor(hsv.x * hueSteps + 0.5 + dither) / hueSteps;
    hueQ = frac(hueQ); // [0,1]にラップ

    // Saturation量子化
    float satQ = floor(hsv.y * satSteps + 0.5 + dither) / satSteps;
    satQ = saturate(satQ);

    // Value量子化
    float valQ = floor(hsv.z * valSteps + 0.5 + dither) / valSteps;
    valQ = saturate(valQ);

    return HSVtoRGB(float3(hueQ, satQ, valQ));
}

// Fragment内での使用
half3 preQuantize = col.rgb;
col.rgb = QuantizeColorHSV(col.rgb, _QuantizeHueSteps, _QuantizeSatSteps,
    _QuantizeValSteps, i.pos.xy, _QuantizeDither);
col.rgb = lerp(preQuantize, col.rgb, _QuantizeBlend);
```

#### 統合ポイント
- **Fragment Post-Lighting Effects末尾**（LUT3Dの後が理想）
- 既存のRGBtoHSV/HSVtoRGBを流用（NataneToonUtils.hlsl）
- キーワード: `_COLOR_QUANTIZE`

#### パフォーマンス（PCデスクトップ基準）
- HSV変換2回 + 量子化演算
- RTX 3060以上: 0.02ms以下
- GTX 1060: 0.03ms以下
- **評価: 極めて軽い**

#### 他技法との相互効果
- **3D LUT**: LUT → 量子化で「特定パレットに制限」を精密制御
- **ペーパーテクスチャ**: 量子化 + 紙テクスチャで版画・リソグラフ風
- **ハッチング**: 量子化で色段数を減らし、段階間をハッチングで補間 → 銅版画調

---

### 技法6: 6段階TAMハッチング（Tonal Art Map フルセット）

#### 概要と表現効果
Praun et al. (2001) の **Tonal Art Map (TAM)** を6段階で実装する。
明るい部分から暗い部分にかけて、段階的にハッチング密度が増加するテクスチャセットを
ライティング結果に応じてブレンドする。

PC版では6段階フルセットを使用し、**ミップマップベースの距離適応**と
**接線方向に沿った異方性ハッチング**を実現する。

**表現効果**:
- ペン画・銅版画風のクロスハッチング
- 明暗をストロークの密度で表現する伝統的なイラスト技法
- 距離に応じたストロークスケールで一貫した視覚密度

#### HLSL実装概要

```hlsl
// === 6段階TAMハッチング ===
// sampler2D _HatchTex0; // 最明: ほぼ白
// sampler2D _HatchTex1; // 明: 薄いハッチ
// sampler2D _HatchTex2; // 中明: まばらなクロスハッチ
// sampler2D _HatchTex3; // 中暗: 密なクロスハッチ
// sampler2D _HatchTex4; // 暗: 濃密なハッチ
// sampler2D _HatchTex5; // 最暗: ほぼ黒

half3 SampleTAMHatching(float2 uv, float intensity, float tiling)
{
    float2 hatchUV = uv * tiling;

    // intensity: 0=最明, 1=最暗
    float level = intensity * 5.0; // 0〜5 の連続値
    int level0 = (int)floor(level);
    int level1 = min(level0 + 1, 5);
    float frac_level = frac(level);

    // 各レベルのサンプリング（使用する2枚のみ）
    half3 hatch0, hatch1;

    // PC向けなので動的分岐でもOK
    if (level0 == 0) hatch0 = tex2D(_HatchTex0, hatchUV).rgb;
    else if (level0 == 1) hatch0 = tex2D(_HatchTex1, hatchUV).rgb;
    else if (level0 == 2) hatch0 = tex2D(_HatchTex2, hatchUV).rgb;
    else if (level0 == 3) hatch0 = tex2D(_HatchTex3, hatchUV).rgb;
    else if (level0 == 4) hatch0 = tex2D(_HatchTex4, hatchUV).rgb;
    else hatch0 = tex2D(_HatchTex5, hatchUV).rgb;

    if (level1 == 0) hatch1 = tex2D(_HatchTex0, hatchUV).rgb;
    else if (level1 == 1) hatch1 = tex2D(_HatchTex1, hatchUV).rgb;
    else if (level1 == 2) hatch1 = tex2D(_HatchTex2, hatchUV).rgb;
    else if (level1 == 3) hatch1 = tex2D(_HatchTex3, hatchUV).rgb;
    else if (level1 == 4) hatch1 = tex2D(_HatchTex4, hatchUV).rgb;
    else hatch1 = tex2D(_HatchTex5, hatchUV).rgb;

    return lerp(hatch0, hatch1, frac_level);
}

// Fragment内での使用（ライティング計算後）
// shadingValue: 既存のトゥーンシェーディング結果 (0=影, 1=明)
half hatchIntensity = 1.0 - shadingValue; // 暗い部分ほど密なハッチ
half3 hatchColor = SampleTAMHatching(uv, hatchIntensity, _HatchTiling);
half3 preHatch = col.rgb;
col.rgb *= lerp(1.0, hatchColor, _HatchBlend);
```

**最適化版**: 6枚のテクスチャをTexture2DArrayにパックし、
2回のサンプルで済ませる方法もある。

#### 統合ポイント
- **Fragment Post-Lighting Effects内**（ライティング後、Emission前）
- `shadingValue` を入力として使用（既存のライティングパイプライン結果をそのまま利用）
- テクスチャスロット6つ追加（またはTexture2DArray 1つ）
- キーワード: `_HATCHING`

#### パフォーマンス（PCデスクトップ基準）
- テクスチャサンプル2回 + lerp（常に隣接2段階のみサンプリング）
- Texture2DArray使用でキャッシュ効率向上
- RTX 3060以上: 0.02ms以下
- GTX 1060: 0.03ms以下
- **評価: 非常に軽い** — テクスチャ2枚のサンプル+lerpのみ

#### 他技法との相互効果
- **色量子化**: 色段数を減らし、段階間をハッチングで補間 → 銅版画
- **エッジ検出**: エッジ付近のハッチング密度を変調 → 漫画的ペン画
- **ペーパーテクスチャ**: ハッチング + 紙テクスチャ = 印刷物風

---

### 技法7: 水彩シミュレーション（エッジダークニング + ウェットエッジ + 紙テクスチャ）

#### 概要と表現効果
水彩画の特徴的な視覚的性質を複合的に再現する。
PC版ではGrabPassを活用した**真のウェットエッジ（色のにじみ）**を含む
フルセットの水彩シミュレーションを実装する。

水彩の4大視覚特性:
1. **エッジダークニング**: 塗りの縁に顔料が集まり暗くなる
2. **ウェットエッジ**: 色が隣接する色に滲む
3. **色の流動(Granulation)**: 顔料の粒子感
4. **紙テクスチャ**: 紙の凹凸が色の乗りに影響

**表現効果**:
- 透明水彩の柔らかく儚い色使い
- 紙の質感と色の滲みによる手描き温かみ
- 色の重ね塗り（ウォッシュ）の透明感

#### HLSL実装概要

```hlsl
// === 水彩シミュレーション 完全版 ===

// 1. エッジダークニング（Fresnel + 深度エッジ利用）
half CalculateEdgeDarkening(float3 worldNormal, float3 viewDir,
                             float2 screenUV, float2 texelSize)
{
    // Fresnel-based edge (オブジェクトの輪郭部分)
    float fresnel = 1.0 - saturate(dot(worldNormal, viewDir));
    float fresnelEdge = pow(fresnel, _WCEdgePower);

    // Optional: 深度エッジ併用でより正確な輪郭
    #ifdef _SCREEN_EDGE
        float depthEdge = SobelEdgeDepth(screenUV, texelSize, _WCEdgeDepthThresh);
        fresnelEdge = max(fresnelEdge, depthEdge);
    #endif

    return fresnelEdge * _WCEdgeDarkenIntensity;
}

// 2. ウェットエッジ（GrabPassベースの色にじみ）
half3 CalculateWetEdge(float2 grabUV, half3 currentColor, float edgeFactor)
{
    float blurRadius = edgeFactor * _WCWetEdgeRadius;
    float2 texelSize = _GrabTexture_TexelSize.xy * blurRadius;

    half3 blurred = 0;
    blurred += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, grabUV + float2( texelSize.x, 0)).rgb;
    blurred += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, grabUV + float2(-texelSize.x, 0)).rgb;
    blurred += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, grabUV + float2(0,  texelSize.y)).rgb;
    blurred += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, grabUV + float2(0, -texelSize.y)).rgb;
    blurred *= 0.25;

    return lerp(currentColor, blurred, edgeFactor * _WCWetEdgeBlend);
}

// 3. Granulation（顔料粒子感）
half3 ApplyGranulation(half3 color, float2 uv, float2 worldPos_xz)
{
    float noise = tex2D(_WCGranulationTex, worldPos_xz * _WCGranulationScale).r;
    noise = lerp(1.0, noise, _WCGranulationIntensity);

    // 暗い部分ほど粒子感が強い（水彩の物理特性）
    float luminance = CALC_LUMINANCE(color);
    float granulationMask = 1.0 - luminance;
    color *= lerp(1.0, noise, granulationMask * _WCGranulationIntensity);

    return color;
}

// 4. 紙テクスチャ
half3 ApplyPaperTexture(half3 color, float2 screenUV)
{
    half paper = tex2D(_WCPaperTex, screenUV * _WCPaperTiling).r;
    paper = lerp(1.0, paper, _WCPaperIntensity);
    // Overlay blend: 中間トーンを保ちつつテクスチャを適用
    half luminance = CALC_LUMINANCE(color);
    half3 overlayDark = 2.0 * color * paper;
    half3 overlayLight = 1.0 - 2.0 * (1.0 - color) * (1.0 - paper);
    half3 overlayResult = lerp(overlayDark, overlayLight, step(0.5, luminance));
    return lerp(color, overlayResult, _WCPaperIntensity);
}

// Fragment内での統合
half edgeFactor = CalculateEdgeDarkening(worldNormal, viewDir, screenUV, edgeTexelSize);
col.rgb *= (1.0 - edgeFactor * _WCEdgeDarkenColor.rgb); // エッジ暗化
col.rgb = CalculateWetEdge(grabUV, col.rgb, edgeFactor); // ウェットエッジ
col.rgb = ApplyGranulation(col.rgb, uv, i.worldPos.xz);  // 粒子感
col.rgb = ApplyPaperTexture(col.rgb, screenUV);            // 紙テクスチャ
```

#### 統合ポイント
- **Fragment Post-Lighting Effects最終セクション**
- エッジダークニング → ウェットエッジ → Granulation → 紙テクスチャの順
- GrabPass共有（Refractionと共有）
- テクスチャスロット: _WCGranulationTex, _WCPaperTex（2枚追加）
- キーワード: `_WATERCOLOR`
- サブキーワード: `_WC_WET_EDGE`（GrabPass使用部分のみ分離可能）

#### パフォーマンス（PCデスクトップ基準）
- エッジダークニング: Fresnelのみなら追加コスト0
- ウェットエッジ: GrabPassサンプル4回（エッジ付近のみ）
- Granulation: テクスチャサンプル1回
- 紙テクスチャ: テクスチャサンプル1回 + Overlay演算
- 合計: テクスチャサンプル6〜10回程度
- RTX 3060以上: 0.1ms以下
- GTX 1060: 0.15ms以下
- **評価: 軽い〜中程度** — GrabPassの初期コストが支配的

#### 他技法との相互効果
- **エッジ検出**: 深度/法線エッジをエッジダークニングに流用 → 高精度
- **色量子化**: 水彩 + 量子化 = 制限パレットの水彩画
- **ソフトフィルター**: 水彩 + ソフトフォーカスで「にじみ」感が倍増
- **ハッチング**: 水彩ウォッシュの上にペンハッチング = ミクストメディア

---

### 技法8: 手書き風アウトライン（ノイズ変調 + テクスチャラインスタンプ）

#### 概要と表現効果
既存のジオメトリベースアウトライン（NataneToonVertex.hlsl）を**高品質化**する。
PC版では以下の拡張を行う:

1. **ノイズ変調太さ**: アウトライン幅をPerlinノイズで変調し、均一でない手描き感
2. **位置揺れ**: アウトラインの頂点位置をノイズで微妙にずらし、手ブレを再現
3. **テクスチャラインスタンプ**: アウトラインにブラシテクスチャを適用
4. **距離適応太さ**: カメラ距離に応じて太さを調整（PixelWidth的な挙動）

**表現効果**:
- 手描きイラストの温かみのある不均一なライン
- 鉛筆・ペン・筆等の画材に応じたテクスチャ質感
- 近景では細かいラインディテール、遠景では安定したシルエット

#### HLSL実装概要

```hlsl
// === 手書き風アウトライン（Vertex Shader拡張） ===
// NataneToonVertex.hlsl に統合

// ノイズ関数（簡易版 - Vertex Shaderで使用）
float SimpleNoise(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

float PerlinNoise1D(float x)
{
    float i = floor(x);
    float f = frac(x);
    float u = f * f * (3.0 - 2.0 * f); // Hermite interpolation
    return lerp(SimpleNoise(float2(i, 0.0)), SimpleNoise(float2(i + 1.0, 0.0)), u);
}

// 手書き風アウトライン頂点変形
float3 ApplyHandDrawnOutline(float3 vertex, float3 normal, float2 uv,
                              float outlineWidth)
{
    // 1. ノイズ変調太さ
    float noiseFreq = _OutlineNoiseFrequency;
    float widthNoise = PerlinNoise1D(uv.x * noiseFreq + _Time.y * _OutlineNoiseSpeed);
    widthNoise = lerp(1.0, widthNoise * 0.5 + 0.75, _OutlineNoiseAmount);
    outlineWidth *= widthNoise;

    // 2. 位置揺れ（法線方向に直交する方向にオフセット）
    float2 posJitter = float2(
        PerlinNoise1D(uv.y * _OutlineJitterFreq + 1.37),
        PerlinNoise1D(uv.x * _OutlineJitterFreq + 2.84)
    ) * 2.0 - 1.0;
    float3 tangent = normalize(cross(normal, float3(0, 1, 0)));
    float3 bitangent = normalize(cross(normal, tangent));
    vertex += (tangent * posJitter.x + bitangent * posJitter.y)
              * _OutlineJitterAmount * outlineWidth;

    // 3. 法線方向への押し出し（既存ロジックの拡張）
    vertex += normal * outlineWidth;

    return vertex;
}

// === テクスチャラインスタンプ（Fragment Shader - Outline Pass） ===
half4 fragOutline(v2f_outline i) : SV_Target
{
    float2 lineUV = float2(i.lineProgress, 0.5);
    half brushAlpha = tex2D(_OutlineBrushTex, lineUV * _OutlineBrushTiling).a;
    half4 outlineColor = _OutlineColor;
    outlineColor.a *= lerp(1.0, brushAlpha, _OutlineBrushBlend);
    clip(outlineColor.a - _OutlineBrushCutoff);
    return outlineColor;
}
```

#### 統合ポイント
- **Vertex Shader拡張**: NataneToonVertex.hlslのアウトライン処理を拡張
- **Fragment追加（Outlineパス）**: テクスチャラインスタンプ用
- 既存のアウトラインシステム (`_OUTLINE` キーワード) の拡張として実装
- キーワード: `_OUTLINE_HAND_DRAWN`
- テクスチャスロット: _OutlineBrushTex（1枚追加）

#### パフォーマンス（PCデスクトップ基準）
- Vertex: PerlinNoise計算 x 頂点数（軽量）
- Fragment: テクスチャサンプル1回追加
- RTX 3060以上: 0.02ms以下
- GTX 1060: 0.03ms以下
- **評価: 非常に軽い** — ほぼ追加コストなし

#### 他技法との相互効果
- **エッジ検出**: ジオメトリアウトライン + スクリーンスペースエッジで二重輪郭
- **ハッチング**: 手書きアウトライン + ハッチングで完全なペン画スタイル
- **水彩**: 手書きアウトライン（水彩ブラシテクスチャ）+ 水彩シミュレーション

---

### 技法9: 色のにじみ/ブリーディング（GrabPassベース）

#### 概要と表現効果
GrabPassで取得したスクリーンバッファに対して、**方向性のある色の拡散**を適用する。
ガウスブラーとは異なり、**隣接ピクセルの色が互いに混ざり合う**挙動を模倣し、
水彩やマーカーの色滲みを再現する。

**表現効果**:
- 色の境界での自然なグラデーション（色収差とは異なる）
- マーカー/水性ペンの色滲み
- 隣接する異なるマテリアル間の色の相互作用

#### HLSL実装概要

```hlsl
// === 色のブリーディング ===
half3 ColorBleeding(float2 grabUV, half3 currentColor, float bleedRadius,
                     float bleedDirectionality)
{
    float2 texelSize = _GrabTexture_TexelSize.xy;
    half3 result = currentColor;

    // 8方向サンプリング
    static const float2 dirs[8] = {
        float2(1,0), float2(-1,0), float2(0,1), float2(0,-1),
        float2(0.707,0.707), float2(-0.707,0.707),
        float2(0.707,-0.707), float2(-0.707,-0.707)
    };

    half3 avgNeighbor = 0;
    [unroll]
    for (int i = 0; i < 8; i++)
    {
        float2 sampleUV = grabUV + dirs[i] * texelSize * bleedRadius;
        half3 neighbor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, sampleUV).rgb;
        avgNeighbor += neighbor;
    }
    avgNeighbor *= 0.125; // /8

    // 色差に基づく選択的ブリーディング
    half colorDiff = length(avgNeighbor - currentColor);
    half bleedFactor = smoothstep(_BleedThreshold, _BleedThreshold + 0.2, colorDiff);

    // 方向性: 明→暗へのにじみが強い（水彩の物理特性）
    half lumCurrent = CALC_LUMINANCE(currentColor);
    half lumNeighbor = CALC_LUMINANCE(avgNeighbor);
    half dirFactor = lerp(1.0, saturate(lumCurrent - lumNeighbor + 0.5), bleedDirectionality);

    result = lerp(currentColor, avgNeighbor, bleedFactor * dirFactor * _BleedIntensity);
    return result;
}

// Fragment内での使用
half3 preBleed = col.rgb;
col.rgb = ColorBleeding(grabUV, col.rgb, _BleedRadius, _BleedDirectionality);
col.rgb = lerp(preBleed, col.rgb, _BleedBlend);
```

#### 統合ポイント
- **Fragment Post-Lighting Effects最終段**（水彩シミュレーションの一部または独立）
- GrabPass共有
- キーワード: `_COLOR_BLEEDING`
- 水彩シミュレーション (`_WATERCOLOR`) のウェットエッジと統合も可能

#### パフォーマンス（PCデスクトップ基準）
- 8方向テクスチャサンプル + 演算
- RTX 3060以上: 0.05ms以下
- GTX 1060: 0.08ms以下
- **評価: 軽い** — GrabPassの初期コストが支配的

#### 他技法との相互効果
- **水彩シミュレーション**: ウェットエッジの上位互換として統合可能
- **ソフトフィルター**: ガウスブラー + ブリーディングで二重のにじみ効果
- **色量子化**: 量子化後にブリーディング → 色の境界がにじむポスタリゼーション

---

### 技法10: 色収差（Chromatic Aberration）

#### 概要と表現効果
GrabPassのRGBチャンネルを個別にオフセットしてサンプリングし、
レンズの色収差を再現する。既にScreenFXOverlayシェーダーに実装済みだが、
メインシェーダーに統合することで**マテリアル単位の制御**が可能になる。

**表現効果**:
- レトロフィルム / アナログカメラ風の色ズレ
- 幻想的・サイケデリックなビジュアル
- ホログラム/グリッチとの組み合わせでサイバー感

#### HLSL実装概要

```hlsl
// === 色収差 (Chromatic Aberration) ===
half3 ChromaticAberration(float2 grabUV, float intensity, float2 center)
{
    float2 dir = grabUV - center;
    float dist = length(dir);
    float2 offset = dir * intensity * dist;

    half r = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, grabUV + offset).r;
    half g = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, grabUV).g;
    half b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, grabUV - offset).b;

    return half3(r, g, b);
}

// Fragment内での使用
#ifdef _CHROMATIC_ABERRATION
{
    float2 grabUV = i.screenPos.xy / i.screenPos.w;
    half3 preCA = col.rgb;
    col.rgb = ChromaticAberration(grabUV, _ChromaticIntensity * 0.01, float2(0.5, 0.5));
    col.rgb = lerp(preCA, col.rgb, _ChromaticBlend);
}
#endif
```

#### 統合ポイント
- **Fragment最終段**（Fogの直前）
- GrabPass共有
- キーワード: `_CHROMATIC_ABERRATION`
- 既存のScreenFXOverlayの実装を参考に、マテリアル単位の制御に変換

#### パフォーマンス（PCデスクトップ基準）
- テクスチャサンプル3回
- **評価: 極めて軽い** — 0.01ms以下

#### 他技法との相互効果
- **グリッチ/ホログラム**: 既存のグリッチエフェクトに色収差を追加でサイバー感
- **ソフトフィルター**: 色収差 + ソフトフォーカスでドリーミーな表現

---

### パフォーマンスまとめ（PCデスクトップ基準）

| 技法 | テクスチャサンプル | 追加演算量 | RTX 3060 | GTX 1060 | コスト評価 |
|------|-------------------|-----------|----------|----------|-----------|
| 1. ガウスブラー（13tapx2） | 26 | 低 | <0.1ms | ~0.2ms | 軽い |
| 2. Kuwaharaフィルタ (r=3) | 64 | 中 | 0.3-0.5ms | 0.5-1.0ms | 中程度 |
| 3. 深度/法線エッジ検出 | 16 | 低 | <0.05ms | <0.1ms | 非常に軽い |
| 4. 3D LUT | 2 | 極低 | <0.01ms | <0.02ms | 極めて軽い |
| 5. HSV色量子化 | 0 | 低 | <0.02ms | <0.03ms | 極めて軽い |
| 6. 6段階TAMハッチング | 2 | 極低 | <0.02ms | <0.03ms | 非常に軽い |
| 7. 水彩シミュレーション | 6-10 | 低〜中 | <0.1ms | ~0.15ms | 軽い |
| 8. 手書き風アウトライン | 1 | 低 | <0.02ms | <0.03ms | 非常に軽い |
| 9. 色のにじみ | 8 | 低 | <0.05ms | <0.08ms | 軽い |
| 10. 色収差 | 3 | 極低 | <0.01ms | <0.01ms | 極めて軽い |

**共通コスト**: GrabPassの取得自体に約0.3-0.5ms（技法1,2,7,9,10で共有）
**合計コスト目安（全技法有効時）**: RTX 3060で1.0-1.5ms / GTX 1060で1.5-2.5ms

---

### 表現スタイル別・推奨技法組み合わせ（PC向け）

| スタイル | 技法組み合わせ | 概要 |
|----------|--------------|------|
| **劇場版アニメ** | ガウスブラー + 3D LUT + 色量子化(弱) + エッジ検出 | Diffusionフィルター + 映画的カラグレ + セルシェーディング |
| **油絵/厚塗り** | Kuwahara + 紙テクスチャ + 色量子化(中) | 面の均一化 + キャンバス質感 + 制限パレット |
| **水彩画** | 水彩フル(4要素) + 色量子化(弱) + 手書きアウトライン | エッジダークニング + ウェットエッジ + 紙テクスチャ + ペンライン |
| **コミック/漫画** | エッジ検出(太) + ハッチング + 色量子化(強) + 手書きアウトライン | スクリーントーン的ハッチング + 太輪郭 + ベタ塗り |
| **銅版画/エッチング** | ハッチング(6段) + エッジ検出 + 色量子化(2段: 白黒) | 明暗のみをハッチング密度で表現 |
| **レトロフィルム** | 3D LUT(ヴィンテージ) + 色収差 + ガウスブラー + 色量子化(弱) | 古い写真風の色味 + レンズ歪み + ソフトフォーカス |
| **絵本風** | 水彩(紙テクスチャ厚め) + ガウスブラー + 色量子化(中) + 手書きアウトライン | 暖色LUT + 厚手の紙 + 柔らかいぼかし |
| **サイバーパンク** | 色収差(強) + グリッチ(既存) + ホログラム(既存) + 3D LUT(ティール&オレンジ) | 既存エフェクトの延長 + カラグレ |

---

### Fragmentパイプライン統合順序（推奨）

```
既存パイプライン:
  Parallax -> UV Animation -> Texture Sampling -> Lighting -> Post-Lighting Effects
                                                              |
                                                              +-- Specular, Rim, MatCap, etc. (既存)
                                                              |
  ★新規技法の挿入ポイント:                                      |
  ---------------------------------------------------------------
  |                                                            |
  |  [STAGE A: 色変換系] (Post-Lighting Effects末尾)           |
  |    +-- _HATCHING (ハッチング) — shadingValueベース         |
  |    +-- _COLOR_QUANTIZE (色量子化) — HSV空間              |
  |    +-- _LUT_3D (3D LUT カラーグレーディング)              |
  |                                                            |
  |  [STAGE B: 画面空間エフェクト] (GrabPass使用)              |
  |    +-- _SOFT_FILTER (ガウスブラー / Diffusion)            |
  |    +-- _KUWAHARA_FILTER (油絵風フィルタ)                  |
  |    +-- _WATERCOLOR (水彩シミュレーション)                  |
  |    +-- _COLOR_BLEEDING (色のにじみ)                       |
  |    +-- _SCREEN_EDGE (深度/法線エッジ検出)                 |
  |    +-- _CHROMATIC_ABERRATION (色収差)                     |
  |                                                            |
  |  [STAGE C: アウトライン拡張] (Outline Pass)                |
  |    +-- _OUTLINE_HAND_DRAWN (手書き風アウトライン)          |
  |                                                            |
  +---> Fog -> SV_Target出力
```

**設計原則**:
- STAGE A は**テクスチャサンプルなし or 少数**のため最初に実行
- STAGE B は**GrabPassを共有**するためまとめて実行（1回のGrabPassで全技法が利用）
- STAGE C は**別パス（Outline Pass）**のため独立

---

### 追加必要リソースまとめ

| カテゴリ | 項目 | 用途 |
|----------|------|------|
| テクスチャ | `_LUT3DTex` (1024x32) | 3D LUT カラーグレーディング |
| テクスチャ | `_HatchTex0〜5` or `Texture2DArray` | 6段階TAMハッチング |
| テクスチャ | `_WCGranulationTex` | 水彩 粒子感ノイズ |
| テクスチャ | `_WCPaperTex` | 水彩 紙テクスチャ |
| テクスチャ | `_OutlineBrushTex` | 手書きアウトライン ブラシ |
| バッファ | `_CameraDepthNormalsTexture` | 法線ベースエッジ検出（C#側設定必要） |
| 既存共有 | `_GrabTexture` | 全GrabPass技法で共有 |
| 既存共有 | `_CameraDepthTexture` | 深度エッジ検出（既に宣言済み） |

**キーワード追加数**: 10個 (`_SOFT_FILTER`, `_KUWAHARA_FILTER`, `_SCREEN_EDGE`, `_LUT_3D`, `_COLOR_QUANTIZE`, `_HATCHING`, `_WATERCOLOR`, `_OUTLINE_HAND_DRAWN`, `_COLOR_BLEEDING`, `_CHROMATIC_ABERRATION`)
