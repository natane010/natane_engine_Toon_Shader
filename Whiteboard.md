# Whiteboard - 蜀鈴聞繧ｳ繝ｼ繝芽ｪｿ譟ｻ (2026-03-11)

## 繝√・繝邱ｨ謌・
| 繝√・繝 | 諡・ｽ馴伜沺 | 繝輔ぃ繧､繝ｫ謨ｰ | 迥ｶ諷・|
|--------|---------|-----------|------|
| Team A | 繧ｷ繧ｧ繝ｼ繝繝ｼ HLSL (Include/**/*.hlsl) | ~10 | 売 隱ｿ譟ｻ荳ｭ |
| Team B | 繧ｷ繧ｧ繝ｼ繝繝ｼ繝舌Μ繧｢繝ｳ繝・(.shader 髢薙・驥崎､・ | ~14 | 売 隱ｿ譟ｻ荳ｭ |
| Team C | Editor GUI (GUI/*.cs) | ~15 | 売 隱ｿ譟ｻ荳ｭ |
| Team D | Editor Tools + Integration (Tools/*.cs, Integration/*.cs) | ~40 | 売 隱ｿ譟ｻ荳ｭ |
| Team E | Runtime + Migration + Presets + Help | ~12 | 売 隱ｿ譟ｻ荳ｭ |

## 隱ｿ譟ｻ隕ｳ轤ｹ
1. **繧ｳ繝斐・繧ｳ繝ｼ繝・*: 繝輔ぃ繧､繝ｫ髢薙〒蜷御ｸ/鬘樔ｼｼ縺ｮ繧ｳ繝ｼ繝峨ヶ繝ｭ繝・け
2. **譛ｪ菴ｿ逕ｨ繧ｳ繝ｼ繝・*: 蜻ｼ縺ｳ蜃ｺ縺輔ｌ縺ｦ縺・↑縺・Γ繧ｽ繝・ラ/螟画焚/繝励Ο繝代ユ繧｣
3. **驥崎､・Ο繧ｸ繝・け**: 蜷後§蜃ｦ逅・ｒ逡ｰ縺ｪ繧句ｴ謇縺ｧ螳溯｣・
4. **蜈ｱ騾壼喧蜿ｯ閭ｽ縺ｪ繝代ち繝ｼ繝ｳ**: 繝倥Ν繝代・髢｢謨ｰ縺ｫ謚ｽ蜃ｺ縺ｧ縺阪ｋ郢ｰ繧願ｿ斐＠繝代ち繝ｼ繝ｳ
5. **繝・ャ繝峨さ繝ｼ繝・*: 蛻ｰ驕比ｸ崎・縺ｪ繧ｳ繝ｼ繝峨ヱ繧ｹ

---

## 逋ｺ隕倶ｺ矩・

### Team A: 繧ｷ繧ｧ繝ｼ繝繝ｼ HLSL 笨・(24莉ｶ: HIGH 5 / MEDIUM 9 / LOW 10)
### Team B: 繧ｷ繧ｧ繝ｼ繝繝ｼ繝舌Μ繧｢繝ｳ繝・笨・(10莉ｶ: HIGH 5 / MEDIUM 2 / LOW 3) 窶・**~12,300陦・(67%) 縺後さ繝斐・**
### Team C: Editor GUI 笨・(36莉ｶ: HIGH 6 / MEDIUM 14 / LOW 16)
### Team D: Editor Tools + Integration 笨・(39莉ｶ: HIGH 16 / MEDIUM 16 / LOW 7)
### Team E: Runtime + Migration + Presets 笨・(18莉ｶ: HIGH 5 / MEDIUM 3 / LOW 10)

---

# (莉･荳九・蜑榊屓縺ｮ繧ｵ繝ｳ繝励Λ繝ｼ隱ｿ譟ｻ縺ｮ險倬鹸 - 蜿り・畑縺ｫ菫晄戟)

## 閭梧勹
- sampler2D: 50蛟・(螟画峩縺ｪ縺・
- NOSAMPLER: 38蛟・(30竊・8縲∵眠讖溯・8蛟九・譌｢縺ｫNOSAMPLER)
- samplerCUBE: 2蛟・
- 譁ｰ讖溯・: Real Character Shader (Cavity, MicroNormal, ClearCoat, SkinDualLobe, HairTransmission)

## 繝√・繝邱ｨ謌・

| 繝√・繝蜷・| 諡・ｽ・| 繧ｹ繝・・繧ｿ繧ｹ |
|----------|------|-----------|
| sampling-verifier | 譁ｰ讖溯・縺ｮ繧ｵ繝ｳ繝励Μ繝ｳ繧ｰ豁｣遒ｺ諤ｧ讀懆ｨｼ + 譌｢蟄・ampler2D縺ｮ譛譁ｰ菴ｿ逕ｨ繝代ち繝ｼ繝ｳ遒ｺ隱・| 笨・螳御ｺ・|
| conversion-planner | 蜈ｷ菴鍋噪縺ｪNOSAMPLER螟画鋤險育判遲門ｮ・(tex2Dlod蟇ｾ蠢懷性繧) | 笨・螳御ｺ・|

## sampling-verifier 隱ｿ譟ｻ邨先棡 (2026-03-11)

### 繧ｿ繧ｹ繧ｯ1: 譁ｰ隕・NOSAMPLER 繝・け繧ｹ繝√Ε8蛟九・讀懆ｨｼ

蜈ｨ8繝・け繧ｹ繝√Ε縺ｯ `UNITY_DECLARE_TEX2D_NOSAMPLER` 縺ｧ螳｣險貂医∩縲ゅし繝ｳ繝励Μ繝ｳ繧ｰ讀懆ｨｼ邨先棡:

| 繝・け繧ｹ繝√Ε | 繧ｵ繝ｳ繝励Μ繝ｳ繧ｰ譁ｹ豕・| 蜿ら・繧ｵ繝ｳ繝励Λ繝ｼ | UV | 繝輔ぃ繧､繝ｫ:陦・| 蛻､螳・|
|-----------|-----------------|--------------|-----|-----------|------|
| `_CavityMap` | `NATANE_SAMPLE_SHARED_R` | `_MainTex` | `uv` (繝｡繧､繝ｳUV) | Fragment:604 | OK |
| `_SkinSpecMask` | `NATANE_SAMPLE_SHARED_R` | `_MainTex` | `uv` | Fragment:980 | OK |
| `_HairStrandDirectionMap` | `UNITY_SAMPLE_TEX2D_SAMPLER` | `_MainTex` | `uv` | Lighting:270 | OK |
| `_HairTransmissionMask` | `NATANE_SAMPLE_SHARED_R` | `_MainTex` | `uv` | Lighting:343 | OK |
| `_MicroNormalMap` | `UNITY_SAMPLE_TEX2D_SAMPLER` | `_MainTex` | `uv * _MicroNormalTiling` | Fragment:205 | **豕ｨ諢・* |
| `_TransmissionMask` | `NATANE_SAMPLE_SHARED_R` | `_MainTex` | `uv` | Fragment:1382 | OK |
| `_ClearCoatMask` | `NATANE_SAMPLE_SHARED_R` | `_MainTex` | `uv` | Fragment:1766 | OK |
| `_ClearCoatNormalMap` | `UNITY_SAMPLE_TEX2D_SAMPLER` | `_MainTex` | `uv` | Fragment:1773 | OK |

**豕ｨ諢丈ｺ矩・*:
- `_MicroNormalMap`: UV 縺ｫ `_MicroNormalTiling` 縺ｫ繧医ｋ諡｡螟ｧ縺後°縺九ｋ縲３epeat 繝ｩ繝・ヴ繝ｳ繧ｰ縺悟ｿ・医Ａ_MainTex` 縺ｮ sampler (騾壼ｸｸ Repeat) 繧貞・譛峨＠縺ｦ縺翫ｊ蝠城｡後↑縺励ゅ◆縺縺励ち繧､繝ｪ繝ｳ繧ｰ蛟咲紫縺悟､ｧ縺阪＞蝣ｴ蜷医，lamp 險ｭ螳壹□縺ｨ繝・け繧ｹ繝√Ε縺悟ｼ輔″莨ｸ縺ｰ縺輔ｌ繧九◆繧√√ユ繧ｯ繧ｹ繝√Ε繧､繝ｳ繝昴・繝郁ｨｭ螳壹〒 Wrap Mode = Repeat 繧呈耳螂ｨ縲・
- `_ClearCoatNormalMap`: 繝弱・繝槭Ν繝槭ャ繝励→縺励※菴ｿ逕ｨ縲３epeat 縺ｧ蝠城｡後↑縺励・
- `_HairStrandDirectionMap`: `.rg` 繝√Ε繝阪Ν繧・`* 2.0 - 1.0` 縺ｧ譁ｹ蜷代・繧ｯ繝医Ν縺ｫ螟画鋤縲よｭ｣縺励＞螳溯｣・・

**邨占ｫ・ 蜈ｨ8繝・け繧ｹ繝√Ε縺ｮ繧ｵ繝ｳ繝励Μ繝ｳ繧ｰ縺ｯ豁｣縺励￥螳溯｣・＆繧後※縺・ｋ縲ょ撫鬘後↑縺励・* (笘・命笘・

---

### 繧ｿ繧ｹ繧ｯ2: tex2Dlod 菴ｿ逕ｨ繝・け繧ｹ繝√Ε荳隕ｧ (NOSAMPLER螟画鋤譎ゅ↓迚ｹ谿雁ｯｾ蠢懷ｿ・ｦ・

| sampler2D | 繝輔ぃ繧､繝ｫ | 陦檎分蜿ｷ | 逕ｨ騾・|
|-----------|---------|--------|------|
| `_OutlineNoiseTex` | NataneToonShader.shader | 1102 | 繧｢繧ｦ繝医Λ繧､繝ｳ繝弱う繧ｺ |
| `_OutlineWidthMap` | 蜷・shader variant vertex | 蜷・園 | 繧｢繧ｦ繝医Λ繧､繝ｳ蟷・・繝・・ |
| `_SmoothNormalTex` | NataneToonVertex.hlsl | 196, 蜷ёariant | 繝吶う繧ｯ貂医∩繧ｹ繝繝ｼ繧ｺ豕慕ｷ・|
| `_AudioTexture` | NataneToonUtils.hlsl | 822 | AudioLink |
| `_FurMask` | NataneToonFurShell.hlsl | 120 | 繝輔ぃ繝ｼ繧ｷ繧ｧ繝ｫ鬆らせ |
| `_VATPositionMap` | NataneToonVertex.hlsl | 36, 116, 117 | VAT鬆らせ繧｢繝九Γ繝ｼ繧ｷ繝ｧ繝ｳ |
| `_VATNormalMap` | NataneToonVertex.hlsl | 44 | VAT豕慕ｷ・|
| `_TessDispMap` | NataneToonTessellation.hlsl | 137 | 繝・ャ繧ｻ繝ｬ繝ｼ繧ｷ繝ｧ繝ｳ繝・ぅ繧ｹ繝励Ξ繝ｼ繧ｹ繝｡繝ｳ繝・|

**驥崎ｦ・*: `tex2Dlod` 縺ｯ NOSAMPLER 繝・け繧ｹ繝√Ε縺ｫ蟇ｾ縺励※ `UNITY_SAMPLE_TEX2D_SAMPLER_LOD` 繧剃ｽｿ縺・ｿ・ｦ√′縺ゅｋ (縺ｾ縺溘・ `SamplerState` + `tex.SampleLevel`)縲ゅ％繧後ｉ縺ｮ sampler2D 繧・NOSAMPLER 蛹悶☆繧句ｴ蜷医・蛟句挨蟇ｾ蠢懊′蠢・ｦ√・

---

### 繧ｿ繧ｹ繧ｯ2陬懆ｶｳ: TRANSFORM_TEX 菴ｿ逕ｨ繝・け繧ｹ繝√Ε (迢ｬ閾ｪ _ST 螟画焚縺悟ｿ・ｦ・

| sampler2D | 繝輔ぃ繧､繝ｫ | 逕ｨ騾・|
|-----------|---------|------|
| `_MainTex` | 螟壽焚 | 繝｡繧､繝ｳ繝・け繧ｹ繝√Ε |
| `_GlitchStretchMask` | Fragment:62 | 繧ｰ繝ｪ繝・メ繧ｹ繝医Ξ繝・メ |
| `_AngelRingTex` | Fragment:1366 | 繧ｨ繝ｳ繧ｸ繧ｧ繝ｫ繝ｪ繝ｳ繧ｰ |
| `_SheenMask` | Fragment:1564 | 繧ｷ繝ｼ繝ｳ |
| `_GlitchMask` | Fragment:2129 | 繧ｰ繝ｪ繝・メ |
| `_WCMask` | Fragment:2258 | 豌ｴ蠖ｩ |
| `_FurNoiseTex` | FurShell:153 | 繝輔ぃ繝ｼ繝弱う繧ｺ |
| `_ColorTexture` | Wirelight:319 | 繝ｯ繧､繝､繝ｼ繝ｩ繧､繝・|

---

### 繧ｿ繧ｹ繧ｯ3: sampler2D 50蛟九・譛邨ょ・鬘・

#### 繧ｰ繝ｫ繝ｼ繝輸: Repeat + Bilinear 竊・`sampler_MainTex` 蜈ｱ譛牙庄閭ｽ (24蛟・

| # | sampler2D | 譬ｹ諡 |
|---|-----------|------|
| 1 | `_ShadowColorTex` | 繝｡繧､繝ｳUV縺ｧ菴ｿ逕ｨ縲ヽepeat |
| 2 | `_ShadingGradeMap` | 繝｡繧､繝ｳUV縺ｧ菴ｿ逕ｨ縲ヽepeat |
| 3 | `_SDFMap` | 鬘廼V縲ヽepeat |
| 4 | `_OutlineMask` | 繝｡繧､繝ｳUV縲ヽepeat |
| 5 | `_BumpMap` | 豕慕ｷ壹・繝・・縲ヽepeat (UV scroll蟇ｾ蠢・ |
| 6 | `_DissolveTex` | UV蝓ｺ貅悶ヽepeat |
| 7 | `_DecalTex` | 繝・き繝ｫUV縲ヽepeat |
| 8 | `_HatchTex0` | 繧ｿ繧､繝ｪ繝ｳ繧ｰ縲ヽepeat |
| 9 | `_HatchTex1` | 繧ｿ繧､繝ｪ繝ｳ繧ｰ縲ヽepeat |
| 10 | `_WCGranulationTex` | 繧ｿ繧､繝ｪ繝ｳ繧ｰ縲ヽepeat |
| 11 | `_WCPaperTex` | 繧ｿ繧､繝ｪ繝ｳ繧ｰ縲ヽepeat |
| 12 | `_WCMask` | TRANSFORM_TEX縲ヽepeat |
| 13 | `_DetailAlbedoMap` | 繝・ぅ繝・・繝ｫUV縲ヽepeat |
| 14 | `_DetailNormalMap` | 繝・ぅ繝・・繝ｫUV縲ヽepeat |
| 15 | `_CoverTex` | worldPos UV縲ヽepeat |
| 16 | `_CoverNormalMap` | worldPos UV縲ヽepeat |
| 17 | `_BackfaceTex` | 繝｡繧､繝ｳUV縲ヽepeat |
| 18 | `_GlitchNoiseTex` | 繧ｹ繧ｯ繝ｭ繝ｼ繝ｫUV縲ヽepeat |
| 19 | `_HologramNoiseTex` | 繧ｹ繧ｯ繝ｭ繝ｼ繝ｫUV縲ヽepeat |
| 20 | `_DripMask` | 繝｡繧､繝ｳUV縲ヽepeat |
| 21 | `_SmearMask` | 繝｡繧､繝ｳUV縲ヽepeat |
| 22 | `_VertexAnimMask` | 繝｡繧､繝ｳUV縲ヽepeat |
| 23 | `_GlitchMask` | TRANSFORM_TEX縲ヽepeat |
| 24 | `_GlitchStretchMask` | TRANSFORM_TEX縲ヽepeat |

#### 繧ｰ繝ｫ繝ｼ繝唯: Clamp + Bilinear 竊・`sampler_linear_clamp` 蜈ｱ譛牙庄閭ｽ (6蛟・

| # | sampler2D | 譬ｹ諡 |
|---|-----------|------|
| 1 | `_RampTex` | 1D ramp lookup [0,1]縲，lamp蠢・・|
| 2 | `_SSSLUTTex` | LUT lookup [0,1]縲，lamp蠢・・|
| 3 | `_LUT3DTex` | Color LUT縲，lamp蠢・・|
| 4 | `_SheenMask` | TRANSFORM_TEX縲，lamp謗ｨ螂ｨ |
| 5 | `_HologramMask` | 繝｡繧､繝ｳUV縲，lamp謗ｨ螂ｨ |
| 6 | `_AngelRingTex` | TRANSFORM_TEX迚ｹ谿涯V縲，lamp謗ｨ螂ｨ |

#### 繧ｰ繝ｫ繝ｼ繝佑: 迢ｬ閾ｪ繧ｵ繝ｳ繝励Λ繝ｼ邯ｭ謖・(MatCap遲峨∫峡閾ｪ繝・け繧ｹ繝√Ε險ｭ螳・ (8蛟・

| # | sampler2D | 譬ｹ諡 |
|---|-----------|------|
| 1 | `_MatCapTex` | MatCap UV (viewDir險育ｮ・縲∫峡閾ｪsampler + SamplerState螳｣險貂医∩ |
| 2 | `_MatCapTex2` | 蜷御ｸ・|
| 3 | `_MatCapTex3` | 蜷御ｸ・|
| 4 | `_2ndTex` | Makeup縲∫峡閾ｪsampler + SamplerState螳｣險貂医∩ |
| 5 | `_3rdTex` | Makeup縲∝酔荳・|
| 6 | `_4thTex` | Makeup縲∝酔荳・|
| 7 | `_5thTex` | Makeup縲∝酔荳・|
| 8 | `_EmissionMap` | Emission UV (scroll蟇ｾ蠢・縲∫峡閾ｪsampler + SamplerState螳｣險貂医∩ |

#### 繧ｰ繝ｫ繝ｼ繝優: 迢ｬ閾ｪ邯ｭ謖∝ｿ・・窶・tex2Dlod / 螟夜Κ / 迚ｹ谿顔畑騾・(12蛟・

| # | sampler2D | 譬ｹ諡 |
|---|-----------|------|
| 1 | `_MainTex` | 繝｡繧､繝ｳ繧ｵ繝ｳ繝励Λ繝ｼ貅舌∝､画峩荳榊庄 |
| 2 | `_PBR_MetallicGlossMap` | PBR逕ｨ縲∽ｻ悶・NOSAMPLER縺後％繧後ｒ蜿ら・ |
| 3 | `_OutlineNoiseTex` | **tex2Dlod菴ｿ逕ｨ** (vertex shader) |
| 4 | `_OutlineWidthMap` | **tex2Dlod菴ｿ逕ｨ** (蟄伜惠縺ｯvariant縺ｮ縺ｿ) |
| 5 | `_SmoothNormalTex` | **tex2Dlod菴ｿ逕ｨ** (vertex shader) |
| 6 | `_FurNoiseTex` | TRANSFORM_TEX菴ｿ逕ｨ縲’ragment |
| 7 | `_FurMask` | **tex2Dlod菴ｿ逕ｨ** (vertex shader) |
| 8 | `_VATPositionMap` | **tex2Dlod菴ｿ逕ｨ** (VAT vertex) |
| 9 | `_VATNormalMap` | **tex2Dlod菴ｿ逕ｨ** (VAT vertex) |
| 10 | `_TessDispMap` | **tex2Dlod菴ｿ逕ｨ** (tessellation) |
| 11 | `_AudioTexture` | **螟夜Κ繝・け繧ｹ繝√Ε** (VRChat AudioLink) |
| 12 | `_AudioTexture2D` | **螟夜Κ繝・け繧ｹ繝√Ε** (VRChat AudioLink) |

---

### 蛻・｡槭し繝槭Μ繝ｼ

| 繧ｰ繝ｫ繝ｼ繝・| 蛟区焚 | NOSAMPLER螟画鋤蜿ｯ閭ｽ | 遽邏・し繝ｳ繝励Λ繝ｼ繧ｹ繝ｭ繝・ヨ |
|---------|------|-----------------|-------------------|
| A (Repeat+Bilinear 竊・sampler_MainTex) | 24 | 縺ｯ縺・| 24 |
| B (Clamp+Bilinear 竊・sampler_linear_clamp) | 6 | 縺ｯ縺・| 5 (譁ｰ隕・繧ｹ繝ｭ繝・ヨ) |
| C (迢ｬ閾ｪsampler邯ｭ謖√ヾamplerState譌｢蟄・ | 8 | 螟画鋤荳崎ｦ・| 0 |
| D (tex2Dlod/螟夜Κ/迚ｹ谿・ | 12 | 蛟句挨蟇ｾ蠢・| 隕∵､懆ｨ・|
| **蜷郁ｨ・* | **50** | **30蛟句､画鋤蜿ｯ閭ｽ** | **譛螟ｧ29繧ｹ繝ｭ繝・ヨ遽邏・* |

**迴ｾ蝨ｨ縺ｮ繧ｵ繝ｳ繝励Λ繝ｼ豸郁ｲｻ**: 50 (sampler2D) + 2 (samplerCUBE) = 52 繧ｹ繝ｭ繝・ヨ
**NOSAMPLER螟画鋤蠕後・莠域ｸｬ**: 50 - 29 = 23 繧ｹ繝ｭ繝・ヨ (D3D11荳企剞16莉･荳九↓縺吶ｋ縺ｫ縺ｯ繧ｰ繝ｫ繝ｼ繝優蟇ｾ蠢懊ｂ蠢・ｦ・

### 豕ｨ諢・ 繧ｰ繝ｫ繝ｼ繝優 縺ｮ tex2Dlod 蟇ｾ蠢懈婿驥・
- `UNITY_SAMPLE_TEX2D_SAMPLER_LOD` 繝槭け繝ｭ縺・Unity 2019.4+ 縺ｧ菴ｿ逕ｨ蜿ｯ閭ｽ
- 縺溘□縺・`_OutlineWidthMap` 縺ｯ shader variant 縺ｮ vertex 繝代せ蜀・〒繧､繝ｳ繝ｩ繧､繝ｳ螳｣險縺輔ｌ縺ｦ縺翫ｊ縲！nclude 蛹悶′蜈医↓蠢・ｦ・
- `_AudioTexture` / `_AudioTexture2D` 縺ｯ VRChat 繝ｩ繝ｳ繧ｿ繧､繝縺瑚ｨｭ螳壹☆繧九◆繧∝､画鋤荳榊庄

---

## conversion-planner 隱ｿ譟ｻ邨先棡 (2026-03-11)

### 1. 繝槭け繝ｭ險ｭ險域｡・

#### 1.1 譁ｰ隕上・繧ｯ繝ｭ螳夂ｾｩ (NataneToonInput.hlsl 1004陦御ｻ倩ｿ代↓霑ｽ蜉)

```hlsl
// ===== NOSAMPLER Sampling Macros =====
// 繧ｵ繝ｳ繝励Λ繝ｼ繧ｰ繝ｫ繝ｼ繝・
//   A (Repeat+Bilinear): sampler_MainTex 繧貞・譛・
//   B (Clamp+Bilinear):  sampler_linear_clamp 繧貞・譛・

// --- tex2D 莉｣譖ｿ ---
#define NATANE_SAMPLE_REPEAT(tex, uv)       UNITY_SAMPLE_TEX2D_SAMPLER(tex, _MainTex, uv)
#define NATANE_SAMPLE_CLAMP(tex, uv)        UNITY_SAMPLE_TEX2D_SAMPLER(tex, _linear_clamp, uv)
#define NATANE_SAMPLE_REPEAT_R(tex, uv)     NATANE_SAMPLE_REPEAT(tex, uv).r
#define NATANE_SAMPLE_CLAMP_R(tex, uv)      NATANE_SAMPLE_CLAMP(tex, uv).r

// --- tex2Dlod 莉｣譖ｿ (鬆らせ/繝・ャ繧ｻ繝ｬ繝ｼ繧ｷ繝ｧ繝ｳ/繝輔ぃ繝ｼ繧ｷ繧ｧ繝ｫ逕ｨ) ---
#if defined(UNITY_SEPARATE_TEXTURE_SAMPLER)
    #define NATANE_SAMPLE_REPEAT_LOD(tex, uv, lod) \
        tex.SampleLevel(sampler_MainTex, uv, lod)
    #define NATANE_SAMPLE_CLAMP_LOD(tex, uv, lod) \
        tex.SampleLevel(sampler_linear_clamp, uv, lod)
#else
    // GLES 繝輔か繝ｼ繝ｫ繝舌ャ繧ｯ: NOSAMPLER 縺ｯ sampler2D 縺ｫ螻暮幕縺輔ｌ繧九◆繧・tex2Dlod 縺御ｽｿ縺医ｋ
    #define NATANE_SAMPLE_REPEAT_LOD(tex, uv, lod) \
        tex2Dlod(tex, float4(uv, 0, lod))
    #define NATANE_SAMPLE_CLAMP_LOD(tex, uv, lod) \
        tex2Dlod(tex, float4(uv, 0, lod))
#endif
```

#### 1.2 Clamp 繧ｵ繝ｳ繝励Λ繝ｼ螳｣險 (NataneToonInput.hlsl 1001陦後・逶ｴ蠕後↓霑ｽ蜉)

```hlsl
#if defined(UNITY_SEPARATE_TEXTURE_SAMPLER)
SamplerState sampler_MainTex;          // 譌｢蟄・ Repeat + Bilinear
SamplerState sampler_linear_clamp;     // 譁ｰ隕・ Clamp + Bilinear (Unity螳夂ｾｩ貂医∩蜷・
#endif
```

#### 1.3 譌｢蟄倥・繧ｯ繝ｭ縺ｨ縺ｮ莠呈鋤諤ｧ

| 繝槭け繝ｭ | 逕ｨ騾・| 螟画峩 |
|--------|------|------|
| `NATANE_SAMPLE_SHARED(tex, samplerTex, coord)` | 譌｢蟄朗OSAMPLER (1005陦・ | 螟画峩縺ｪ縺・|
| `NATANE_SAMPLE_SHARED_R(tex, samplerTex, coord)` | 蜷御ｸ・R ch (1006陦・ | 螟画峩縺ｪ縺・|
| `NATANE_SAMPLE_SHARED_BLUR(tex, samplerTex, coord, blur)` | Blur迚・(Utils:659) | 螟画峩縺ｪ縺・|
| `NATANE_SAMPLE_REPEAT(tex, uv)` | **譁ｰ隕・*: samplerTex 蠑墓焚逵∫払迚・| 譁ｰ隕剰ｿｽ蜉 |
| `NATANE_SAMPLE_CLAMP(tex, uv)` | **譁ｰ隕・*: Clamp迚・| 譁ｰ隕剰ｿｽ蜉 |
| `NATANE_SAMPLE_REPEAT_LOD(tex, uv, lod)` | **譁ｰ隕・*: tex2Dlod Repeat迚・| 譁ｰ隕剰ｿｽ蜉 |
| `NATANE_SAMPLE_CLAMP_LOD(tex, uv, lod)` | **譁ｰ隕・*: tex2Dlod Clamp迚・| 譁ｰ隕剰ｿｽ蜉 |

---

### 2. tex2Dlod 縺ｮ NOSAMPLER 蟇ｾ蠢懈婿豕・

#### 2.1 蝠城｡・
`UNITY_SAMPLE_TEX2D_SAMPLER` 縺ｯ蜀・Κ縺ｧ `tex.Sample(sampler, uv)` 繧貞ｱ暮幕縺吶ｋ縺後・
`tex2Dlod` 縺ｫ逶ｸ蠖薙☆繧・`tex.SampleLevel(sampler, uv, lod)` 縺ｫ縺ｯ蟇ｾ蠢懊＠縺ｪ縺・・

#### 2.2 隗｣豎ｺ遲・
`UNITY_SEPARATE_TEXTURE_SAMPLER` 螳夂ｾｩ譎・ `Texture2D.SampleLevel(SamplerState, uv, lod)` 繧堤峩謗･菴ｿ逕ｨ
髱槫ｮ夂ｾｩ譎・(GLES): `tex2Dlod(sampler2D, float4(uv,0,lod))` 縺ｫ繝輔か繝ｼ繝ｫ繝舌ャ繧ｯ

#### 2.3 tex2Dlod 菴ｿ逕ｨ邂・園縺ｮ螟画鋤繝槭ャ繝斐Φ繧ｰ (8邂・園)

| 繝輔ぃ繧､繝ｫ | 陦・| Before | After |
|---------|-----|--------|-------|
| Vertex.hlsl | 36 | `tex2Dlod(_VATPositionMap, float4(vatUV, 0, 0))` | `NATANE_SAMPLE_CLAMP_LOD(_VATPositionMap, vatUV, 0)` |
| Vertex.hlsl | 44 | `tex2Dlod(_VATNormalMap, float4(vatUV, 0, 0))` | `NATANE_SAMPLE_CLAMP_LOD(_VATNormalMap, vatUV, 0)` |
| Vertex.hlsl | 57 | `tex2Dlod(_OutlineNoiseTex, float4(noiseUV, 0, 0))` | `NATANE_SAMPLE_REPEAT_LOD(_OutlineNoiseTex, noiseUV, 0)` |
| Vertex.hlsl | 116 | `tex2Dlod(_VATPositionMap, float4(v.uv.x, frameV_curr, 0, 0))` | `NATANE_SAMPLE_CLAMP_LOD(_VATPositionMap, float2(v.uv.x, frameV_curr), 0)` |
| Vertex.hlsl | 117 | `tex2Dlod(_VATPositionMap, float4(v.uv.x, frameV_prev, 0, 0))` | `NATANE_SAMPLE_CLAMP_LOD(_VATPositionMap, float2(v.uv.x, frameV_prev), 0)` |
| Vertex.hlsl | 196 | `tex2Dlod(_SmoothNormalTex, float4(v.uv, 0, 0))` | `NATANE_SAMPLE_REPEAT_LOD(_SmoothNormalTex, v.uv, 0)` |
| Tessellation.hlsl | 137 | `tex2Dlod(_TessDispMap, float4(dispUV, 0, 0))` | `NATANE_SAMPLE_REPEAT_LOD(_TessDispMap, dispUV, 0)` |
| FurShell.hlsl | 120 | `tex2Dlod(_FurMask, float4(v.uv, 0, 0))` | `NATANE_SAMPLE_REPEAT_LOD(_FurMask, v.uv, 0)` |
| Utils.hlsl | 822 | `tex2Dlod(_AudioTexture, ...)` | **螟画鋤縺励↑縺・* (螟夜Κ繝・け繧ｹ繝√Ε) |

---

### 3. SampleTex2DBlur 邉ｻ髢｢謨ｰ縺ｮ蟇ｾ蠢・

#### 3.1 蝠城｡・
`SampleTex2DBlur(sampler2D tex, ...)` / `SampleTex2DBlur3(sampler2D tex, ...)` 縺ｯ蠑墓焚縺・`sampler2D` 蝙九・
NOSAMPLER 繝・け繧ｹ繝√Ε (`Texture2D` 蝙・ 繧呈ｸ｡縺帙↑縺・・

#### 3.2 蟇ｾ蠢・ NOSAMPLER迚医が繝ｼ繝舌・繝ｭ繝ｼ繝芽ｿｽ蜉 (NataneToonUtils.hlsl)

```hlsl
// SampleTex2DBlurShared 縺ｮ逶ｴ蠕・(~656陦御ｻ倩ｿ・ 縺ｫ霑ｽ蜉:
#if defined(UNITY_SEPARATE_TEXTURE_SAMPLER)
half4 SampleTex2DBlurRepeat(Texture2D tex, float2 uv, float blur)
{
    half4 center = tex.Sample(sampler_MainTex, uv);
    if (blur <= 0.001) return center;
    float2 dx = ddx(uv) * blur * 4.0;
    float2 dy = ddy(uv) * blur * 4.0;
    half4 col = center * 0.4;
    col += tex.Sample(sampler_MainTex, uv + dx) * 0.15;
    col += tex.Sample(sampler_MainTex, uv - dx) * 0.15;
    col += tex.Sample(sampler_MainTex, uv + dy) * 0.15;
    col += tex.Sample(sampler_MainTex, uv - dy) * 0.15;
    return col;
}
half3 SampleTex2DBlur3Repeat(Texture2D tex, float2 uv, float blur)
{
    return SampleTex2DBlurRepeat(tex, uv, blur).rgb;
}
#else
// GLES: NOSAMPLER 縺ｯ sampler2D 縺ｫ繝輔か繝ｼ繝ｫ繝舌ャ繧ｯ 竊・譌｢蟄倬未謨ｰ繧偵◎縺ｮ縺ｾ縺ｾ菴ｿ逕ｨ
#define SampleTex2DBlurRepeat(tex, uv, blur) SampleTex2DBlur(tex, uv, blur)
#define SampleTex2DBlur3Repeat(tex, uv, blur) SampleTex2DBlur3(tex, uv, blur)
#endif
```

#### 3.3 蠖ｱ髻ｿ邂・園

| 菴ｿ逕ｨ邂・園 | 繝・け繧ｹ繝√Ε | Before | After |
|---------|-----------|--------|-------|
| Fragment:1619 | `_MatCapTex` | `SampleTex2DBlur3(_MatCapTex, ...)` | `SampleTex2DBlur3Repeat(_MatCapTex, ...)` |
| Fragment:1664 | `_MatCapTex2` | 蜷御ｸ・| 蜷御ｸ・|
| Fragment:1693 | `_MatCapTex3` | 蜷御ｸ・| 蜷御ｸ・|
| Fragment:1864 | `_EmissionMap` | 蜷御ｸ・| 蜷御ｸ・|
| Fragment:2179 | `_DecalTex` | `SampleTex2DBlur(_DecalTex, ...)` | `SampleTex2DBlurRepeat(_DecalTex, ...)` |

---

### 4. Utils 髢｢謨ｰ縺ｮ蠑墓焚螟画峩縺悟ｿ・ｦ√↑邂・園

莉･荳九・ Utils 髢｢謨ｰ縺ｯ `sampler2D` 蠑墓焚縺ｧ繝・け繧ｹ繝√Ε繧貞女縺大叙縺｣縺ｦ縺翫ｊ縲・
NOSAMPLER 繝・け繧ｹ繝√Ε繧呈ｸ｡縺吝ｴ蜷医・髢｢謨ｰ繧ｷ繧ｰ繝阪メ繝｣縺ｮ螟画峩 or 繧ｪ繝ｼ繝舌・繝ｭ繝ｼ繝峨′蠢・ｦ・

| 髢｢謨ｰ | 繝輔ぃ繧､繝ｫ:陦・| 蠑墓焚繝・け繧ｹ繝√Ε | 蟇ｾ蠢懈婿豕・|
|------|-----------|--------------|---------|
| `CalculateGlitchRGBSplit` | Utils:~1014 | `_MainTex` | **螟画峩荳崎ｦ・* (_MainTex 縺ｯsampler2D邯ｭ謖・ |
| `ApplyGlitchNoise` | Utils:~1033 | `_GlitchNoiseTex`, `_MainTex` | `_GlitchNoiseTex` 縺ｮ縺ｿ NOSAMPLER迚医が繝ｼ繝舌・繝ｭ繝ｼ繝・|
| `ApplyHatching` | Utils:~1537 | `_HatchTex0`, `_HatchTex1` | NOSAMPLER迚医が繝ｼ繝舌・繝ｭ繝ｼ繝・|
| `ApplyWatercolor` | Utils:~1582 | `_WCGranulationTex`, `_WCPaperTex` | NOSAMPLER迚医が繝ｼ繝舌・繝ｭ繝ｼ繝・|
| `ApplyLUT3D` | Utils:~1515 | `_LUT3DTex` | NOSAMPLER迚・(Clamp繧ｵ繝ｳ繝励Λ繝ｼ菴ｿ逕ｨ) |
| `TriplanarSample` | Utils:~1433 | `_MainTex` | **螟画峩荳崎ｦ・* |

---

### 5. SamplerState 蜑企勁蟇ｾ雎｡荳隕ｧ

| SamplerState | Input.hlsl陦・| #if 繧ｬ繝ｼ繝芽｡・| 蜑企勁逅・罰 |
|-------------|-------------|-------------|---------|
| `sampler_2ndTex` | 1014 | 1013-1015 | _2ndTex 縺・NOSAMPLER蛹・竊・sampler_MainTex 繧貞・譛・|
| `sampler_3rdTex` | 1021 | 1020-1022 | 蜷御ｸ・|
| `sampler_4thTex` | 1028 | 1027-1029 | 蜷御ｸ・|
| `sampler_5thTex` | 1035 | 1034-1036 | 蜷御ｸ・|
| `sampler_MatCapTex` | 1109 | 1108-1110 | _MatCapTex 縺・NOSAMPLER蛹・竊・sampler_MainTex 繧貞・譛・|
| `sampler_MatCapTex2` | 1118 | 1117-1119 | 蜷御ｸ・|
| `sampler_MatCapTex3` | 1127 | 1126-1128 | 蜷御ｸ・|
| `sampler_EmissionMap` | 1145 | 1144-1146 | _EmissionMap 縺・NOSAMPLER蛹・竊・sampler_MainTex 繧貞・譛・|

**蜑企勁蟇ｾ雎｡**: 8蛟九・ SamplerState + 縺昴・ `#if defined(UNITY_SEPARATE_TEXTURE_SAMPLER)` 繧ｬ繝ｼ繝峨ヶ繝ｭ繝・け
**菫晄戟**: `sampler_MainTex` (蝓ｺ貅悶し繝ｳ繝励Λ繝ｼ) + `sampler_linear_clamp` (譁ｰ隕剰ｿｽ蜉)

---

### 6. FurShell.hlsl 迚ｹ谿雁ｯｾ蠢・

FurShell.hlsl 縺ｯ NataneToonInput.hlsl 繧・include 縺励↑縺・峡閾ｪ讒区・縲・
`.shader` 縺ｮ Pass 繝悶Ο繝・け蜀・〒 `sampler2D _MainTex`, `_FurNoiseTex`, `_FurMask` 縺悟ｮ｣險縺輔ｌ繧九・

**蟇ｾ蠢懈婿驥・*:
1. `.shader` 縺ｮ Fur Pass 蜀・〒 `UNITY_DECLARE_TEX2D_NOSAMPLER(_FurNoiseTex)` + `UNITY_DECLARE_TEX2D_NOSAMPLER(_FurMask)` 縺ｫ螟画峩
2. `_MainTex` 縺ｯ `sampler2D` 縺ｮ縺ｾ縺ｾ邯ｭ謖・(蝓ｺ貅悶し繝ｳ繝励Λ繝ｼ + `SamplerState sampler_MainTex` 螳｣險繧りｿｽ蜉)
3. FurShell.hlsl 蜀・・ `tex2D(_FurNoiseTex, ...)` 竊・`NATANE_SAMPLE_REPEAT(_FurNoiseTex, ...)`
4. FurShell.hlsl 蜀・・ `tex2Dlod(_FurMask, ...)` 竊・`NATANE_SAMPLE_REPEAT_LOD(_FurMask, ...)`
5. FurShell.hlsl 蜈磯ｭ縺ｫ繝槭け繝ｭ螳夂ｾｩ繧定ｿｽ蜉縺吶ｋ縺九∝・騾壹・繝・ム繝ｼ縺九ｉ include

---

### 7. 繧ｰ繝ｫ繝ｼ繝佑 竊・繧ｰ繝ｫ繝ｼ繝輸 縺ｸ縺ｮ蜀榊・鬘樊署譯・

sampling-verifier 縺ｮ蛻・｡槭〒縺ｯ繧ｰ繝ｫ繝ｼ繝佑 (迢ｬ閾ｪsampler邯ｭ謖・ 縺ｫ8蛟句・鬘槭＆繧後※縺・◆縺後・
**conversion-planner 縺ｨ縺励※縺ｯ繧ｰ繝ｫ繝ｼ繝佑繧ょ・縺ｦNOSAMPLER蛹悶ｒ謗ｨ螂ｨ**:

**逅・罰**:
- `_2ndTex` ・・`_5thTex`: Makeup 繝・け繧ｹ繝√Ε縺ｯ蠢・★ Repeat 繝ｩ繝・ヴ繝ｳ繧ｰ縲Ａsampler_MainTex` 蜈ｱ譛峨〒蝠城｡後↑縺励・
- `_MatCapTex` ・・`_MatCapTex3`: MatCap UV 縺ｯ `[0,1]` 遽・峇蜀・〒險育ｮ励＆繧後ｋ縲・lamp/Repeat 縺ｩ縺｡繧峨〒繧ょ撫鬘後↑縺励３epeat 縺ｧ邨ｱ荳蜿ｯ閭ｽ縲・
- `_EmissionMap`: Emission UV 縺ｯ繧ｹ繧ｯ繝ｭ繝ｼ繝ｫ/蝗櫁ｻ｢繧｢繝九Γ繝ｼ繧ｷ繝ｧ繝ｳ莉倥″縲３epeat 縺梧ｭ｣縺励＞縲・

**蜉ｹ譫・*: 繧ｰ繝ｫ繝ｼ繝佑 8蛟九・ SamplerState 繧貞炎髯､ 竊・**縺輔ｉ縺ｫ8繧ｹ繝ｭ繝・ヨ遽邏・*

---

### 8. 螟画鋤蠕後・繧ｵ繝ｳ繝励Λ繝ｼ謨ｰ隕狗ｩ阪ｂ繧・(譛邨・

| 繧ｫ繝・ざ繝ｪ | Before | After |
|---------|--------|-------|
| sampler2D (sampler豸郁ｲｻ) | 50 | 3 (MainTex + AudioLinkﾃ・) |
| SamplerState 譏守､ｺ螳｣險 | 9 (MainTex+8蛟句挨) | 2 (MainTex + linear_clamp) |
| samplerCUBE | 2 | 2 |
| **蜷郁ｨ医し繝ｳ繝励Λ繝ｼ繧ｹ繝ｭ繝・ヨ** | **譛謔ｪ52** | **譛螟ｧ7** |

**DX11 荳企剞16縺ｫ蟇ｾ縺励※蜊∝・縺ｪ菴呵｣輔ｒ遒ｺ菫晢ｼ・*

---

### 9. 螳溯｣・・ｺ上・謗ｨ螂ｨ

```
Phase 0: 繝槭け繝ｭ螳夂ｾｩ + sampler_linear_clamp 螳｣險霑ｽ蜉
         蟇ｾ雎｡: NataneToonInput.hlsl
         繝ｪ繧ｹ繧ｯ: 縺ｪ縺・(螳｣險霑ｽ蜉縺ｮ縺ｿ)

Phase 1: 繝輔Λ繧ｰ繝｡繝ｳ繝亥ｰら畑繝・け繧ｹ繝√Ε縺ｮ螟画鋤 (41蛟・
         蟇ｾ雎｡: NataneToonInput.hlsl (螳｣險螟画峩)
               NataneToonFragment.hlsl (tex2D 竊・NATANE_SAMPLE_*)
               NataneToonLighting.hlsl (tex2D 竊・NATANE_SAMPLE_*)
         SamplerState 蜑企勁: 8蛟・
         繝ｪ繧ｹ繧ｯ: 荳ｭ (螟画峩邂・園縺悟､壹＞)

Phase 2: Utils 髢｢謨ｰ縺ｮ NOSAMPLER 蟇ｾ蠢・
         蟇ｾ雎｡: NataneToonUtils.hlsl
               - SampleTex2DBlurRepeat / SampleTex2DBlur3Repeat 霑ｽ蜉
               - ApplyHatching, ApplyWatercolor, ApplyLUT3D, ApplyGlitchNoise 縺ｮ繧ｪ繝ｼ繝舌・繝ｭ繝ｼ繝・
         繝ｪ繧ｹ繧ｯ: 荳ｭ (髢｢謨ｰ繧ｷ繧ｰ繝阪メ繝｣螟画峩)

Phase 3: tex2Dlod 繝・け繧ｹ繝√Ε縺ｮ螟画鋤 (6蛟・
         蟇ｾ雎｡: NataneToonVertex.hlsl
               NataneToonTessellation.hlsl
               NataneToonFurShell.hlsl
         繝ｪ繧ｹ繧ｯ: 鬮・(鬆らせ繧ｷ繧ｧ繝ｼ繝繝ｼ螟画峩縲〃R莠呈鋤諤ｧ遒ｺ隱榊ｿ・ｦ・

Phase 4: FurShell.shader 縺ｮ Pass 繝悶Ο繝・け菫ｮ豁｣
         蟇ｾ雎｡: NataneToonShader_Fur.shader, NataneToonShader_Fur_Lite.shader
         繝ｪ繧ｹ繧ｯ: 荳ｭ

Phase 5: 蜈ｨ繝舌Μ繧｢繝ｳ繝・(.shader) 縺ｮ繧ｳ繝ｳ繝代う繝ｫ讀懆ｨｼ
         蟇ｾ雎｡: 蜈ｨ12繧ｷ繧ｧ繝ｼ繝繝ｼ繝舌Μ繧｢繝ｳ繝・
         - Opaque / Cutout / Transparent / Lite蜷・ｨｮ
         - Fur / Background / ScreenEdgeSplit
```

---

### 10. 豕ｨ諢冗せ繝ｻ繝ｪ繧ｹ繧ｯ

#### Critical
1. **_MainTex 縺ｯ螟画鋤遖∵ｭ｢**: 蝓ｺ貅悶し繝ｳ繝励Λ繝ｼ縺ｮ貅先ｳ峨ょ､画鋤縺吶ｋ縺ｨ繧ｵ繝ｳ繝励Λ繝ｼ蜈ｱ譛峨メ繧ｧ繝ｼ繝ｳ縺悟ｴｩ螢翫☆繧九・
2. **_AudioTexture / _AudioTexture2D 縺ｯ螟画鋤遖∵ｭ｢**: VRChat 繝ｩ繝ｳ繧ｿ繧､繝縺・`sampler2D` 縺ｨ縺励※險ｭ螳壹☆繧句､夜Κ繝・け繧ｹ繝√Ε縲・
3. **GLES 繝輔か繝ｼ繝ｫ繝舌ャ繧ｯ**: `UNITY_SEPARATE_TEXTURE_SAMPLER` 譛ｪ螳夂ｾｩ迺ｰ蠅・〒縺ｯ蜈ｨ繝槭け繝ｭ縺梧ｭ｣縺励￥繝輔か繝ｼ繝ｫ繝舌ャ繧ｯ縺吶ｋ縺薙→縲・

#### High
4. **SampleTex2DBlur 邉ｻ**: `sampler2D` 蠑墓焚縺ｮ髢｢謨ｰ縺悟､壽焚縲・OSAMPLER 迚医・繧ｪ繝ｼ繝舌・繝ｭ繝ｼ繝峨′蠢・ｦ√・
5. **VRChat Quest (Vulkan)**: `sampler_linear_clamp` 縺・Vulkan 縺ｧ豁｣縺励￥蜍穂ｽ懊☆繧九°隕∵､懆ｨｼ縲・
6. **Outline Pass**: shader variant 繝輔ぃ繧､繝ｫ蜀・↓繧､繝ｳ繝ｩ繧､繝ｳ螳｣險縺輔ｌ縺溘ユ繧ｯ繧ｹ繝√Ε縺後≠繧九ょ・ .shader 繝輔ぃ繧､繝ｫ縺ｮ遒ｺ隱阪′蠢・ｦ√・

#### Medium
7. **TRANSFORM_TEX 繝槭け繝ｭ**: `TRANSFORM_TEX` 縺ｯ `sampler2D` 縺ｮ `_ST` 螟画焚繧貞盾辣ｧ縺吶ｋ縲・OSAMPLER 繝・け繧ｹ繝√Ε縺ｧ繧・`_ST` 螟画焚縺・CBUFFER 蜀・↓縺ゅｌ縺ｰ蜍穂ｽ懊☆繧九・
8. **ApplyHatching / ApplyWatercolor**: 蠑墓焚縺悟､壹＞髢｢謨ｰ縺ｮ繧ｪ繝ｼ繝舌・繝ｭ繝ｼ繝峨・繧ｳ繝ｼ繝芽・蠑ｵ縺ｮ繝ｪ繧ｹ繧ｯ縲・
# 2026-03-11 隱ｿ譟ｻ繝｡繝｢: Toon / NPR / PBR 繝｢繝ｼ繝画紛逅・

- 迴ｾ蝨ｨ縺ｯ `ShaderType`縲～RenderingMode`縲～_ShadingMode` 縺悟挨霆ｸ縺ｧ蜈ｱ蟄倥＠縺ｦ縺・ｋ縲・
- `_ShadingMode` 縺ｯ `Toon / Gradient / StandardToon / PBRLike` 繧呈戟縺､縲・
- `NPR` 縺ｯ迢ｬ遶九Δ繝ｼ繝峨〒縺ｯ縺ｪ縺上～Hatching / Watercolor / LUT / Kuwahara / Quantize` 縺ｪ縺ｩ縺ｮ蠕梧ｮｵ繧ｹ繧ｿ繧､繝ｫ讖溯・縺ｨ縺励※蟄伜惠縺吶ｋ縲・
- `PBR` 縺ｯ `Background` 繝舌Μ繧｢繝ｳ繝亥ｰら畑 `_PBR` 縺ｨ縲ゝoon邉ｻ `_ShadingMode = PBRLike` 縺ｮ 2 邉ｻ邨ｱ縺後≠繧頑ｦょｿｵ縺悟・縺九ｌ縺ｦ縺・ｋ縲・
- `NataneToonFragment.hlsl` 縺ｫ shading 蛻・ｲ舌→譛邨ょ粋謌舌′髮・ｸｭ縺励※縺翫ｊ縲√％縺薙′繝｢繝ｼ繝画紛逅・・譛螟ｧ繝懊ヨ繝ｫ繝阪ャ繧ｯ縲・
- `NataneToonInput.hlsl` 縺ｯ蟾ｨ螟ｧ縺ｪ蜈ｱ譛・input 螂醍ｴ・↓縺ｪ縺｣縺ｦ縺・※縲ゝoon/NPR/PBR 縺斐→縺ｮ雋ｬ蜍吝・髮｢縺後∪縺蠑ｱ縺・・
- preset 邉ｻ縺ｯ `Style_Toon` / `Style_NPR` 繧呈戟縺､縺後～_ShadingMode` / `_SurfaceModel` / realistic-character 諡｡蠑ｵ蛟､繧剃ｿ晄戟縺励※縺・↑縺・・
- `NataneToonMaterialPreset` 縺ｮ `specularIntensity` 縺ｯ `_SpecularIntensity` 縺ｧ縺ｯ縺ｪ縺・`_SpecularBlend` 縺ｫ邨舌・縺､縺・※縺翫ｊ縲∝ｰ・擂縺ｮ繝｢繝ｼ繝芽ｨｭ險医〒縺ｯ隕狗峩縺怜呵｣懊・
- 謾ｹ菫ｮ縺ｯ `LookMode = Toon / NPR / PBR` 繧・UI 繝ｬ繧､繝､繝ｼ縺ｨ縺励※霑ｽ蜉縺励∵里蟄・`_ShadingMode` 縺ｯ蜀・Κ蛟､縺ｫ蟇・○繧区婿驥昴′螳牙・縲・

### 霑ｽ蜉隕∽ｻｶ繝｡繝｢
- 繝ｦ繝ｼ繧ｶ繝ｼ隕∵悍縺ｯ縲後Δ繝ｼ繝牙・譖ｿ縲阪□縺代〒縺ｪ縺上√卦oon / NPR / PBR 縺ｮ驟榊粋邇・ｒ繝舌・縺ｧ邏ｰ縺九￥險ｭ螳壹＠縺溘＞縲阪・
- 縺薙ｌ縺ｯ謗剃ｻ・enum 縺縺代〒縺ｯ雜ｳ繧翫★縲～Look Mixer` 縺悟ｿ・ｦ√・
- 莉ｮ螂醍ｴ・
  - `_LookMode`
  - `_ToonWeight`
  - `_NprWeight`
  - `_PbrWeight`
- 3 weight 縺ｯ inspector 荳翫〒豁｣隕丞喧陦ｨ遉ｺ縺吶ｋ縲・
- 螳溯｣・ｸ翫・ `NPR` 繧・base shading 縺ｧ縺ｯ縺ｪ縺・post-lighting style stack 縺ｮ驥阪∩縺ｨ縺励※謇ｱ縺・婿縺瑚・辟ｶ縲・
- 蜈医↓ `Toon vs PBR` 縺ｮ surface mix 繧剃ｽ懊ｊ縲√◎縺ｮ荳翫↓ `NPR mix` 繧貞ｾ梧ｮｵ縺ｧ蜷域・縺吶ｋ莠悟ｱ､讒区・縺悟ｮ牙・縲・

### 諛ｸ蠢ｵ轤ｹ縺ｮ隗｣豎ｺ譁ｹ驥・
- `Toon / NPR / PBR` 繧貞ｮ悟・縺ｪ蜷悟・謇ｱ縺・↓縺励↑縺・・
  - `Toon / PBR = surface response`
  - `NPR = style stack`
- `Background _PBR` 縺ｨ `PBRLike` 縺ｯ蜀・Κ螳溯｣・ｒ谿九＠縺､縺､縲ゞI 縺ｧ縺ｯ `Look Mixer > PBR` 縺ｫ邨ｱ荳陦ｨ遉ｺ縺吶ｋ縲・
- `Look Mixer` 縺ｮ蜀・Κ蛟､縺ｯ `normalized weights + resolved runtime weights` 縺ｮ莠梧ｮｵ縺ｫ縺吶ｋ縲・
  - 菫晏ｭ伜､: 繝ｦ繝ｼ繧ｶ繝ｼ蜈･蜉・
  - 螳溯｡悟､: 豁｣隕丞喧蠕後・蛟､
- 譌｢蟄・`_ShadingMode` 縺ｯ蟒・ｭ｢縺帙★ migration source 縺ｨ縺励※谿九☆縲・
- preset / clipboard / file share 縺ｯ `schemaVersion` 繧呈戟縺溘○縺ｦ谿ｵ髫守ｧｻ陦後☆繧九・
- foldout 邂｡逅・・譌ｧ `ShowEnvRim` 邉ｻ繧定ｪｭ繧莠呈鋤螻､繧呈ｮ九＠縺､縺､縲～foldoutPrefsKeys` 縺ｫ荳譛ｬ蛹悶☆繧九・
- shader 蛻・ｲ仙｢怜刈縺ｯ `weight < epsilon` 縺ｧ譫昴ｒ蛻・ｋ險ｭ險医↓縺励※縲・㍾縺ｿ繧ｼ繝ｭ縺ｮ險育ｮ励ｒ驕ｿ縺代ｋ縲・

---

## Shader Logic Audit (2026-03-11)

**Auditor**: Claude Opus 4.6
**Scope**: NataneToonFragment.hlsl, NataneToonLighting.hlsl, NataneToonPBR.hlsl
**Focus**: Division by zero, NaN/Inf propagation, normalize of zero vectors, incorrect math, branch coherence, missing clamps, feature interaction bugs, NOSAMPLER correctness

---

### Finding #1: `normalize(max(directResult, 0.01))` produces biased color direction when components differ
- **File**: `NataneToonFragment.hlsl:886`
- **Severity**: MEDIUM
- **Code**: `half3 directDir = normalize(max(directResult, 0.01));`
- **Problem**: When `directResult` has negative or zero components (e.g., `(-0.1, 0.5, 0.0)`), `max(directResult, 0.01)` clamps them to `0.01`, which biases the color direction. The `normalize()` then extracts a "direction" that no longer represents the original hue. This is used at line 887: `directResult = directDir * directLum;` to reconstruct the color.
- **Impact**: Subtle color shift in dark/saturated areas where lighting multiplies color below zero. Most visible with strong colored lights and dark albedo.
- **Risk**: LOW in practice (values rarely go negative after lighting), but mathematically incorrect.

### Finding #2: `_LightSnapAngle` = 0 causes division by zero in Light Snapping
- **File**: `NataneToonFragment.hlsl:303-307`
- **Severity**: HIGH
- **Code**:
  ```hlsl
  float snapRad = radians(_LightSnapAngle);
  float snappedPhi = round(phi / snapRad) * snapRad;
  float snappedTheta = round(theta / snapRad) * snapRad;
  ```
- **Problem**: If `_LightSnapAngle` is 0 (or very close to 0), `snapRad` becomes 0, causing `phi / snapRad` = Inf and `round(Inf)` = NaN. This NaN propagates to `snappedDir` and then to `lightDir`, corrupting all subsequent lighting.
- **Impact**: Entire mesh renders black or with visual corruption when Light Snap is enabled with angle = 0.
- **Fix**: Add `snapRad = max(snapRad, 0.001)` after the radians conversion.

### Finding #3: `_RefractionIndex` = 0 causes division by zero in Refraction
- **File**: `NataneToonLighting.hlsl:510`
- **Severity**: MEDIUM
- **Code**: `float iorRatio = 1.0 / refractionIndex;`
- **Problem**: If `_RefractionIndex` is set to 0 by user error, this divides by zero producing Inf, which then makes `refract()` return a degenerate vector.
- **Impact**: Visual corruption of refraction effect. The fallback at line 516 (total internal reflection check) won't catch this because Inf dot products produce NaN, not small values.
- **Fix**: `float iorRatio = 1.0 / max(refractionIndex, 0.01);`

### Finding #4: Iridescence HSV-to-RGB uses many dynamic branches
- **File**: `NataneToonLighting.hlsl:674-679`
- **Severity**: LOW (performance)
- **Code**: Chain of `if (h < 1.0) ... else if (h < 2.0) ...` (6 branches)
- **Problem**: GPU warp/wavefront divergence when adjacent pixels have different hue values (likely in iridescence, as hue varies by view angle). This creates up to 6 dynamic branches per pixel.
- **Impact**: Minor GPU performance hit on mobile (Quest). Desktop GPUs handle this well.
- **Note**: Could be replaced with a branchless `frac`/`clamp`/`abs` HSV-to-RGB formula, but severity is low.

### Finding #5: `_ShadowSmoothing` reused for two unrelated purposes
- **File**: `NataneToonFragment.hlsl:452-479` (PCF shadow smoothing) and `700-703` (shadow step smoothing)
- **Severity**: MEDIUM (feature interaction bug)
- **Code**:
  - Line 452: `if (_ShadowSmoothing > 0.001) { /* PCF shadow map filtering */ }`
  - Line 700: `if (_ShadowSmoothing > 0.001) { /* toon step to continuous blending */ }`
- **Problem**: The same `_ShadowSmoothing` parameter controls both PCF shadow map filtering AND toon shadow step smoothing. Increasing smoothing to soften shadow map edges also smooths the toon shading steps, which is an unintended side effect.
- **Impact**: Users cannot independently control shadow map quality and toon step smoothness. Setting `_ShadowSmoothing = 0.5` for softer shadow map also makes toon steps 50% blended with continuous shading.
- **Note**: This is a design issue rather than a bug. The two uses should ideally have separate parameters.

### Finding #6: `screenPos.w` division without protection in PCSS path
- **File**: `NataneToonFragment.hlsl:384`
- **Severity**: LOW
- **Code**: `float2 pcssShadowUV = i._ShadowCoord.xy / i._ShadowCoord.w;`
- **Problem**: `_ShadowCoord.w` could theoretically be zero at degenerate geometry or near-plane clips, causing Inf UV coordinates.
- **Impact**: Extremely rare in practice since `_ShadowCoord` is interpolated from valid clip-space positions. Other paths (line 455, 1823, 2253) have the same pattern.
- **Note**: Not a practical concern but noted for completeness.

### Finding #7: Hair specular `normalize()` on potentially zero-length vectors
- **File**: `NataneToonLighting.hlsl:276, 277, 281, 310, 311`
- **Severity**: LOW
- **Code**:
  ```hlsl
  half3 mappedStrandDir = normalize(worldTangent * strandDirTS.x + worldBinormal * strandDirTS.y);  // 276
  tangent = normalize(lerp(tangent, mappedStrandDir, directionStrength));  // 277
  return normalize(tangent + worldNormal * 0.0001);  // 281
  half3 shiftedTangent1 = normalize(tangent + worldNormal * (_HairSpecShift1 + shiftTexValue));  // 310
  ```
- **Problem**: Line 276 `normalize()` is guarded by `strandLenSq > 0.0001` check at line 273, so it's safe. Lines 277 and 281 are safe because they lerp between non-zero vectors and add a small bias. Lines 310-311 could produce near-zero vectors if `_HairSpecShift` exactly cancels the tangent projection onto normal, but this is extremely unlikely.
- **Impact**: Practically zero risk. The bias at line 281 (`* 0.0001`) is a correct safety measure.

### Finding #8: `_NormalFlattenY` = 1.0 produces zero-length normal in edge case
- **File**: `NataneToonFragment.hlsl:240-241`
- **Severity**: MEDIUM
- **Code**:
  ```hlsl
  worldNormal.y *= (1.0 - _NormalFlattenY);
  worldNormal = normalize(worldNormal);
  ```
- **Problem**: If `_NormalFlattenY = 1.0` AND the original `worldNormal` is pure vertical `(0, 1, 0)` (e.g., a flat horizontal surface), then after flattening `worldNormal = (0, 0, 0)`, and `normalize(0,0,0)` produces NaN.
- **Impact**: Flat horizontal surfaces with full Y-flatten would render as black/corrupted. This is an edge case but achievable with flat-topped geometry (table tops, floors).
- **Fix**: Add `worldNormal = normalize(worldNormal + float3(0, 0, 0.0001))` or clamp `_NormalFlattenY` to `< 0.999`.
- **Note**: Previously identified in Phase 2 audit but still present in the code.

### Finding #9: NOSAMPLER sampler group correctness verification
- **File**: `NataneToonInput.hlsl:1010-1016`, `NataneToonFragment.hlsl` (various), `NataneToonLighting.hlsl` (various)
- **Severity**: PASS (no issues found)
- **Verification Summary**:
  - **Group A (Repeat via `NATANE_SAMPLE_REPEAT` / `sampler_MainTex`)**: `_BumpMap`, `_ShadowColorTex`, `_SDFMap`, `_ShadingGradeMap`, `_DetailAlbedoMap`, `_DetailNormalMap`, `_CoverTex`, `_CoverNormalMap`, `_DissolveTex`, `_2ndTex`-`_5thTex`, `_GlitchStretchMask`, `_GlitchMask`, `_WCMask`, `_SmearMask`, `_DripMask` -- All correctly use Repeat sampling for textures that tile or use animated UVs. **OK**.
  - **Group B (Clamp via `NATANE_SAMPLE_CLAMP` / `sampler_linear_clamp`)**: `_RampTex` (1D ramp, needs clamp), `_SSSLUTTex` (LUT, needs clamp), `_AngelRingTex` (clamp to avoid edge bleeding), `_HologramMask` (clamp), `_SheenMask` (clamp) -- All correctly use Clamp sampling. **OK**.
  - **`NATANE_SAMPLE_SHARED_R` usage**: All mask textures (_2ndTexMask sharing _2ndTex sampler, _MatCapMask sharing _MatCapTex sampler, etc.) correctly inherit the parent texture's sampler, which is ultimately `sampler_MainTex` (Repeat). Since masks use standard UV in [0,1] range, Repeat vs Clamp doesn't matter. **OK**.
  - **PBR Occlusion special case** (Fragment:941): `NATANE_SAMPLE_SHARED_R(_PBR_OcclusionMap, _PBR_MetallicGlossMap, uv)` -- shares sampler with `_PBR_MetallicGlossMap` which is NOSAMPLER using `sampler_MainTex`. **OK**.
- **Conclusion**: The NOSAMPLER conversion is correctly implemented. No sampler group mismatches found.

### Finding #10: `pow()` with potentially negative base in rim/fresnel calculations
- **File**: `NataneToonLighting.hlsl:356, 384, 402, 469, 496`
- **Severity**: PASS (safe)
- **Analysis**: All `pow()` calls in rim/fresnel calculations use a `saturate()` or `max(0, ...)` protected base:
  - Line 356: `pow(rim, power)` where `rim = 1.0 - saturate(dot(n, v))` -- always in [0,1]. **Safe**.
  - Line 384: Same pattern. **Safe**.
  - Line 402: `pow(1.0 - NdotV, _SheenPower)` where `NdotV = max(0, ...)` -- base in [0,1]. **Safe**.
  - Line 469: `pow(1.0 - viewAngle, _FresnelPower)` where `viewAngle = saturate(...)` via smoothstep. **Safe**.
- **Note**: The exponent values (_RimPower, _SheenPower, etc.) could be negative if user sets them so, which would produce 1/x results but not NaN. This is acceptable behavior.

### Finding #11: PBR GGX `NdotH * NdotH * (a2 - 1.0) + 1.0` can be zero when roughness = 0
- **File**: `NataneToonPBR.hlsl:8-9`
- **Severity**: LOW
- **Code**:
  ```hlsl
  half a2 = roughness * roughness;
  half d = NdotH * NdotH * (a2 - 1.0) + 1.0;
  return a2 / (UNITY_PI * d * d + 1e-7);
  ```
- **Problem**: When `roughness = 0`, `a2 = 0`, and `d = NdotH^2 * (-1) + 1 = 1 - NdotH^2`. When `NdotH = 1` (perfect specular alignment), `d = 0` and `d*d = 0`. The `+ 1e-7` epsilon prevents division by zero, so `a2 / (PI * 0 + 1e-7) = 0 / 1e-7 = 0`. This is correct: zero roughness with perfect alignment should produce a Dirac delta, which approximates to a large finite value, but since `a2 = 0` the numerator is also 0.
- **Impact**: No visual artifact. The `+ 1e-7` properly prevents NaN. However, at `roughness = 0` the entire GGX term collapses to 0, meaning perfectly smooth surfaces produce no specular highlight. This is physically incorrect (should be an infinitely bright infinitely small point).
- **Note**: This is a known limitation of the GGX formulation at roughness=0. The PBR path in Fragment.hlsl already clamps roughness to `max(0.04, ...)` (lines 925, 952, 986), so this never triggers in practice. **Safe**.

### Finding #12: Decal blend mode selection skips mode 2 (Overlay)
- **File**: `NataneToonFragment.hlsl:2186-2189`
- **Severity**: MEDIUM
- **Code**:
  ```hlsl
  half isDecalMul = step(0.5, _DecalBlendMode) * step(_DecalBlendMode, 1.5);
  half isDecalReplace = step(2.5, _DecalBlendMode);
  col.rgb = lerp(decalAdd, decalMul, isDecalMul);
  col.rgb = lerp(col.rgb, decalReplace, isDecalReplace);
  ```
- **Problem**: The comment at line 2181 says modes are `0=Add, 1=Multiply, 2=Overlay, 3=Replace`, but the code only handles Add (mode 0), Multiply (mode 1), and Replace (mode >= 2.5, i.e., mode 3). Mode 2 (Overlay) falls through as Add (default). There is no `decalOverlay` calculation or selection.
- **Impact**: Setting Decal blend mode to "Overlay" (mode 2) produces Add blending instead. Users expecting Overlay behavior get incorrect results.

### Finding #13: `_HeightFogEnd - _HeightFogStart` can be zero
- **File**: `NataneToonFragment.hlsl:2408`
- **Severity**: LOW
- **Code**: `float heightFactor = saturate((worldY - _HeightFogStart) / (_HeightFogEnd - _HeightFogStart + 0.001));`
- **Analysis**: The `+ 0.001` epsilon prevents division by zero. **Safe**. However, if `_HeightFogEnd < _HeightFogStart`, the denominator can become negative, inverting the fog direction. This is arguably user error but could be surprising.

### Finding #14: `lerp(gray, col.rgb, _Saturation)` at Fragment:1134 uses float instead of half3
- **File**: `NataneToonFragment.hlsl:1134`
- **Severity**: LOW
- **Code**: `col.rgb = lerp(gray, col.rgb, _Saturation);`
- **Problem**: `gray` is a `half` scalar, `col.rgb` is `half3`. HLSL will broadcast `gray` to `half3(gray, gray, gray)` implicitly. This is technically correct but could be clearer. Same pattern at line 1172.
- **Impact**: No visual issue. Just a style note.

### Finding #15: Emission SSS ForwardBase guard inconsistency
- **File**: `NataneToonFragment.hlsl:1380, 1399`
- **Severity**: LOW
- **Code**:
  ```hlsl
  #if defined(_SSS) && defined(UNITY_PASS_FORWARDBASE)   // Line 1380
      ...
      #ifndef UNITY_PASS_FORWARDBASE                      // Line 1399
          sss *= _AdditionalLightIntensity;
      #endif
  ```
- **Problem**: The `#ifndef UNITY_PASS_FORWARDBASE` block at line 1399 is dead code because it's nested inside `#if defined(UNITY_PASS_FORWARDBASE)`. The SSS ForwardAdd path is therefore unreachable from this code block. Same pattern appears for other effects (Rim Light at 1456, Env Rim at 1602, Offset Rim at 1545).
- **Impact**: No visual bug since SSS is correctly ForwardBase-only. The dead code suggests there may have been intent to support SSS in ForwardAdd that was never completed, or it's defensive coding.

---

### Summary

| Severity | Count | IDs |
|----------|-------|-----|
| HIGH | 1 | #2 |
| MEDIUM | 4 | #1, #3, #5, #8, #12 |
| LOW | 5 | #4, #6, #11, #13, #14, #15 |
| PASS | 3 | #7, #9, #10 |

**Critical findings requiring immediate attention**:
1. **#2 (HIGH)**: `_LightSnapAngle = 0` causes NaN propagation through entire lighting pipeline
2. **#8 (MEDIUM)**: `_NormalFlattenY = 1.0` on horizontal surfaces causes NaN
3. **#12 (MEDIUM)**: Decal Overlay blend mode (mode 2) is silently broken
4. **#3 (MEDIUM)**: `_RefractionIndex = 0` causes Inf in refraction

**NOSAMPLER conversion**: Verified correct. No sampler group mismatches found (Finding #9).

**Overall assessment**: The codebase is well-protected against common GPU shader pitfalls. Most division-by-zero risks are properly guarded with `max()` or `+ epsilon`. The main issues are edge cases with user-configurable parameters that can reach degenerate values.

---

## 2026-03-11 lilToon Migration Exact Match Investigation

### Goal
- When using the lilToon migration tool, the converted Natane material should match the source look as closely as possible.
- Current implementation does **not** guarantee exact parity even in `Visual Match` mode.

### Root Causes Found
- `Editor/NataneToon/Migration/LilToonMigrationTool.cs`
  - `Visual Match` UI text says the result will visually match immediately, but the mapper still uses several approximations.
  - Shadow color mapping explicitly warns that it may differ because lilToon uses multiplicative indirect color while Natane uses lerp-based shading.
  - The migration injects Natane-side defaults for `_LightIntensity`, `_LightMaxInfluence`, `_Brightness`, `_Saturation`, `_ShadowMaxDarkness`, `_LightMinInfluence`, `_GIIntensity`. These are not copied from lilToon and can shift the look.
  - `_ShadowNormalStrength` is folded into `_BumpScale`, but `_BumpScale` is then copied again afterward. This looks like an overwrite bug.
  - `MapMultiShadowLayers()` captures only 2nd/3rd colors and borders. `_Shadow2ndBlur`, `_Shadow3rdBlur`, and shadow masks are captured but not applied.
  - Rim/MatCap/specular/outline conversions are heuristic, not 1:1.
  - MatCap is still disabled by default after migration because the implementation differs. This blocks exact match by design.
- `Shaders/NataneToon/Include/Rendering/NataneToonFragment.hlsl`
  - StandardToon is described as lilToon-exact, but the path still includes Natane-side safety and stabilization behavior.
  - `_USE_RAMP` is evaluated before `_STANDARD_TOON`, so a material with both flags will not take the StandardToon path.
  - StandardToon uses `min(stIndirectCol, stDirectCol)` safety clamp, `CompressLightingForSafeRange(...)`, extra indirect minimums, and additional light composition. These can all shift the final image away from lilToon.
  - ForwardAdd StandardToon path is simplified and not the same as the main StandardToon composition path.
- `Editor/NataneToon/GUI/NataneToonShaderGUIHelpers.cs`
  - Help text overstates the current behavior with `exact same calculation path` / `perfect visual parity`.

### Concrete Fix Proposal
1. Add a dedicated `Exact lilToon Migration` path
   - Introduce a strict migration/profile flag such as `_LILTOON_EXACT_MIGRATION`.
   - Use this only for materials created by the lilToon migration tool.
   - Keep current StandardToon behavior as the general-purpose safe mode.
2. Split StandardToon into `Exact` and `Safe` behavior
   - Exact path should skip Natane-only stabilizers:
     - `CompressLightingForSafeRange(...)`
     - `min(stIndirectCol, stDirectCol)` safety clamp
     - extra `_IndirectLightMinColor` floor
     - non-lilToon additional-light reshaping
   - Exact path should also force `_STANDARD_TOON` precedence above `_USE_RAMP` for migrated materials.
3. Stop injecting Natane defaults in exact mode
   - Do not force `_LightIntensity`, `_Brightness`, `_Saturation`, `_ShadowMaxDarkness`, `_LightMinInfluence`, `_GIIntensity` during exact migration.
   - Only map values that have a source equivalent, or keep exact-mode defaults neutral.
4. Fix clear migration bugs first
   - Preserve `_ShadowNormalStrength` result instead of overwriting it with the source `_BumpScale`.
   - Map 2nd/3rd shadow blur values and shadow masks if the shader supports them, otherwise report them as unsupported exact-match blockers.
   - Use actual rim blend mapping instead of hardcoding `_RimBlendMode = 1` if possible.
5. Define unsupported parity blockers explicitly
   - If Natane cannot reproduce a lilToon feature exactly, the tool should report `exact parity unsupported` instead of silently approximating.
   - Current candidates: MatCap blend semantics, some rim/specular conversions, possibly outline scale if internal width math differs.
6. Add regression validation
   - Build a migration comparison scene that renders source lilToon and migrated Natane side by side under the same lighting.
   - Capture screenshots and compute simple diff metrics for representative cases:
     - basic toon
     - multi-shadow
     - rim
     - matcap
     - emission
     - outline

### Recommended Implementation Order
1. Fix migration bugs and remove misleading UI wording.
2. Add exact-mode shader flag and strict StandardToon branch.
3. Remove forced defaults from exact migration mode.
4. Handle or explicitly reject unsupported parity cases.
5. Add screenshot-diff validation workflow for migration regressions.

## 2026-03-11 lilToon migration parity audit follow-up

- Official lilToon forward BRP path still centers on `lil_pass_forward_normal.hlsl` + `lil_common_frag.hlsl`; the main parity-sensitive block is `lilGetShading()`.
- Code comparison result:
  - current Natane migration is still **not pixel-identical** to lilToon after migration.
  - the new exact-compat slice closes some gaps, but final calc still differs where Natane lacks lilToon-specific branches.
- Gaps confirmed from lilToon source:
  - `ShadowBorderRange` gradation path is not reproduced in Natane StandardToon.
  - `ShadowMaskType` flat/face shadow branches are not reproduced.
  - `BackfaceForceShadow` is not reproduced.
  - `UseRimShade` and `UseEmission2nd` still have no Natane-equivalent runtime path.
- Fixes applied in this pass:
  - copied `OutlineWidthMask` into Natane `_OutlineWidthMap` and enable the width-map keyword during migration.
  - copied exact-compat hidden payloads for lilToon shadow parity:
    - `_ShadowMainStrength`
    - `_Shadow2ndBlur`
    - `_Shadow3rdBlur`
    - `_ShadowStrengthMask`
    - `_ShadowBorderMask`
    - `_ShadowBlurMask`
  - StandardToon exact path now consumes those hidden shadow values for closer lilToon shadow composition.
  - migration report now warns when source material uses still-unsupported lilToon-only branches:
    - `RimShade`
    - `Emission2nd`
    - `ShadowBorderRange`
    - `ShadowMaskType`
    - `ShadowPostAO`
    - `BackfaceForceShadow`

## 2026-03-11 LilToon Mode planning note

- User direction is now:
  - keep current Natane math as default
  - add a checkbox-style `LilToon Mode`
  - when on, migrated materials should switch to lilToon-compatible calculations as much as possible
  - avoid separate shader replacement if possible
- Recommended implementation shape:
  - reuse current hidden exact-compat flag as backend
  - expose it as `LilToon Mode` only for migrated materials
  - continue storing lilToon-only values as hidden payloads
  - expand compatibility in waves:
    - lighting core
    - shadow detail
    - rim / emission2nd / matcap / outline
- Important constraint:
  - full parity without any shader-side branch is not realistic
  - but full shader replacement is also unnecessary; small gated branches in the current shader family are the best compromise

## 2026-03-11 compile fix note

- Unity compile stop was caused by broken `L(...)` string literals near the top of `LilToonMigrationTool.cs`.
- Fixed by restoring those UI strings to UTF-8-safe literals and removing broken quote pairs.
- Re-normalized the touched shader / hlsl files to CRLF so Unity no longer reports mixed line endings for the files edited in this task.

## 2026-03-11 LilToon Inspector UX note

- Current `Shading` UI shows `Look Mixer` first, which is not ideal for migrated lilToon materials.
- Better UX is:
  - show a dedicated `LilToon Migration` card first
  - keep it visible even when compatibility is OFF
  - make the primary action a 2-state switch:
    - `Natane`
    - `lilToon Match`
- The card should show:
  - migrated badge
  - source shader
  - migration mode
  - parity warning count
  - inline unsupported-feature list
- Important implementation constraint:
  - `_LilToonExactCompatibility` alone is not enough for good UX
  - need explicit migration metadata such as `_LilToonMigrated`, `_LilToonMigrationMode`, `_LilToonParityFlags`
- First Inspector slice should not try to hide all Natane controls.
  - Better to disable `Look Mixer` with explanation while `lilToon Match` is ON.

## 2026-03-11 LilToon Inspector UX implementation note

- First slice implemented:
  - hidden migration metadata properties added to the toon shader family
  - migration tool now writes migrated flag, migration mode, parity flags, and source tags
  - Shading section now shows a `LilToon Migration` card before `Look Mixer`
  - `Look Mixer` becomes read-only when `lilToon Match` is ON
- This keeps the default Natane workflow intact while making migrated materials self-explanatory.
- Still pending:
  - host Unity compile verification
  - visual verification in the Inspector
  - possible refinement of the parity warning text and action buttons

## 2026-03-11 GUI compile fix note

- `NataneToonShaderGUI` was directly calling `LilToonMigrationTool.ShowWindow()`.
- This broke compilation because `NataneToon.Editor` does not reference `NataneToon.Editor.Migration`, while the migration asmdef already depends on the editor asmdef.
- Fixed by opening the migration window through runtime lookup:
  - first try reflection on `NataneToon.Editor.LilToonMigrationTool, NataneToon.Editor.Migration`
  - then fall back to `NataneToolMenuPaths.TryOpenByToolKey("LilToonMigration")`
  - show a dialog only if both paths fail

## 2026-03-11 GUI string literal repair note

- Investigated current compile blockers using UTF-8 reads instead of mojibake-prone console output.
- Actual syntax errors were narrowed down to malformed string literals in:
  - `Editor/NataneToon/GUI/NataneToonShaderGUI.cs`
  - `Editor/NataneToon/GUI/NataneToonShaderGUIHelpers.cs`
  - `Editor/NataneToon/Tools/MaterialValidator.cs`
- Replaced the broken search metadata and a few malformed UI/help labels with safe compileable strings.
- Goal is compile recovery first; localized copy can be refined later if needed.

## 2026-03-11 ShaderGUIHelpers rebuild note

- `NataneToonShaderGUIHelpers.cs` developed structural corruption after earlier string-literal cleanup.
- The file was truncated and contained class-scope code leakage around `DrawShadingModeControls`, which caused CS0116/CS1031/CS8124 at line 203.
- Rebuilt the helper as a compile-safe minimal implementation while preserving the public method surface used by `NataneToonShaderGUI`.
- Current verification:
  - no odd-quote lines remain in `NataneToonShaderGUI.cs`
  - no odd-quote lines remain in `NataneToonShaderGUIHelpers.cs`
  - no odd-quote lines remain in `MaterialValidator.cs`

## 2026-03-11 ShaderGUI lower-half recovery note

- `NataneToonShaderGUI.cs` still had severe structural corruption after the earlier string fix pass.
- `DrawLookMixerControls` was repaired locally to remove the malformed `DrawHelpToggle` call, restore missing migration-state locals, and remove an unmatched `EndVertical`.
- From `DrawAdvancedLightingSection` downward, the file had escalating brace leakage (`FINAL_DEPTH=68`) and nested method declarations.
- Recovered the lower half by restoring the `HEAD` version of `NataneToonShaderGUI.cs` starting at `DrawAdvancedLightingSection`, keeping the repaired upper section and look-mixer additions intact.
- Post-recovery verification:
  - `NataneToonShaderGUI.cs` brace depth returns to `FINAL_DEPTH=0`
  - all method declarations in the restored lower half are back at class scope
  - odd-quote scan is clean for `NataneToonShaderGUI.cs`, `NataneToonShaderGUIHelpers.cs`, and `MaterialValidator.cs`

## 2026-03-11 ShaderGUI follow-up compile note

- Remaining compile errors after the structural recovery were unresolved symbols, not parser damage.
- Added a minimal `DrawLilToonMigrationCardIfNeeded()` implementation so the shading section can show a migration notice without breaking compilation.
- Removed the stale `ShaderType.ScreenFX` branch from `DrawNonToonShaderGUI()` because the current `ShaderType` enum only defines `Toon`, `Eye`, `Wirelight`, and `StandardToon`.
- Verification after the follow-up patch:
  - `NataneToonShaderGUI.cs` brace depth is still `FINAL_DEPTH=0`
  - no live `ShaderType.ScreenFX` or `screenFXDrawer` references remain in executable code

## 2026-03-11 Tool compile cleanup note

- `ScreenFXSetupTool.cs` still had a truncated `EditorUtility.DisplayDialog(...)` call around line 71.
- Restored the missing success message argument and closing parenthesis so the tool compiles again.
- `LilToonMigrationTool.cs` had `EditorGUILayout.HelpBox(L("Options", "Options"), EditorStyles.boldLabel)`, which passes a `GUIStyle` where `MessageType` is required.
- Replaced that call with `EditorGUILayout.LabelField(...)` because the intent there is a section header, not an info box.
- Verification after cleanup:
  - `ScreenFXSetupTool.cs` brace depth is `FINAL_DEPTH=0`
  - `LilToonMigrationTool.cs` brace depth is `FINAL_DEPTH=0`

## 2026-03-11 ShaderVariantCollector UI repair note

- `ShaderVariantCollector.cs` had `EditorGUILayout.HelpBox(...)` with four arguments at line 91.
- That call shape matches `ObjectField`, not `HelpBox`, so the collection picker UI had been corrupted.
- Replaced it with `collection = (ShaderVariantCollection)EditorGUILayout.ObjectField(...)`.
- Verification after repair:
  - `ShaderVariantCollector.cs` brace depth is `FINAL_DEPTH=0`

## 2026-03-11 Mojibake audit note

- Ran a UTF-8 scan over `Editor`, `Runtime`, `Shaders`, and `Website` source files for common mojibake patterns.
- `Runtime`, `Shaders`, and `Website` came back clean in this pass.
- `Editor` still has 13 suspicious files.
- High-impact user-visible mojibake remains in:
  - `Editor/NataneToon/NataneToolMenuPaths.cs` (menu path constants, 31 suspicious lines)
  - `Editor/NataneToon/Migration/LilToonMigrationTool.cs` (mixed comments + some UI/header strings, 187 suspicious lines total)
  - `Editor/NataneToon/Tools/MaterialValidator.cs` (`MenuItem` path string plus comments)
- Lower-impact cases appear comment-only or near-comment-only in:
  - `NataneToolHealthStartupCheck.cs`
  - `NataneToonEyeDrawer.cs`
  - `NataneToonShaderGUIUtility.cs`
  - `NataneToonShaderTypeSwitcher.cs`
  - `ShaderPrewarmingEditor.cs`
  - `UnifiedMaterialEditor.cs`
  - `AssetReferenceChecker.cs`
  - `MaterialPresetBrowser.cs`
  - `NataneToonShaderGUI.cs` (remaining hits are comment fragments)

## 2026-03-11 Mojibake cleanup completion

- Repaired the actual UTF-8 mojibake in the targeted Editor files, including menu paths, tool headers, dialog text, inspector labels, and corrupted comment/doc lines.
- Confirmed with explicit UTF-8 reads that `Editor`, `Runtime`, `Shaders`, and `Website` no longer contain the scanned mojibake patterns.
- Important verification note: Windows PowerShell default `Get-Content` misread the BOM-less UTF-8 files and displayed false-positive mojibake; re-checking with `-Encoding UTF8` showed the cleaned source correctly.
- `git diff --check` passed after the cleanup (only existing line-ending warnings remain in unrelated dirty files).

## 2026-03-11 Japanese localization fallback restoration

- `NataneToonLocalization.cs` now restores Japanese when `L(ja, en)` was flattened to identical English strings during the earlier mojibake cleanup.
- Added missing exact fallback entries for the remaining uncovered UI labels, including:
  - `Face Forward Direction`, `Face Right Direction`
  - `Material Scan`, `Migration Mode`, `Migration Version`
  - `Scan for lilToon Materials`
  - `SDF Intensity`, `SDF Map`, `SDF Offset`, `SDF Shadow Map Settings`, `SDF Softness`
  - `Tone Color`
- Added fallback regex coverage for dynamic runtime/editor messages such as:
  - preset counts
  - material feature toggle summaries
  - conversion completion dialogs
  - inspector error popups
  - preset paste confirmation text
  - outline-width adjustment report lines
  - issue-sample truncation notices
- Verification after the patch:
  - `UNCOVERED_FIXED_COUNT=0` against `unique_same_l_strings.txt`
  - no duplicate keys in `JapaneseFallbackExact`
  - `git diff --check -- Editor/NataneToon/GUI/NataneToonLocalization.cs` shows only the existing LF/CRLF warning

## 2026-03-11 Japanese help text fallback expansion

- User reported that JP mode still showed English in descriptions and help boxes.
- Root cause: `L(ja, en)` returned the `ja` argument whenever `ja != en`, even if the `ja` text was still plain English or was corrupted/mojibake.
- Updated `NataneToonLocalization.L(...)` so JP mode now falls back when the `ja` text:
  - has no readable Japanese characters
  - contains obvious broken-localization markers such as halfwidth-katakana mojibake or repeated `?`
- Added exact Japanese fallback entries for the main remaining help/explanation text blocks in:
  - `NataneToonShaderGUIHelpers`
  - `LilToonMigrationTool`
  - `ShaderPrewarmingEditor`
  - `MaterialPresetBrowser`
- Added dynamic translation patterns for preset-application and VTuber preset generation result dialogs.
- Verification after the expansion:
  - no duplicate keys in `JapaneseFallbackExact`
  - `git diff --check -- Editor/NataneToon/GUI/NataneToonLocalization.cs` shows only the existing LF/CRLF warning

## 2026-03-11 Help toggle visibility fix

- User reported that help text still appeared even when help was not expanded.
- Root cause 1: `NataneToonShaderGUI.DrawHelpToggle()` intentionally rendered a collapsed one-line preview before the help button.
- Removed that collapsed preview so hidden help now stays fully hidden until the user expands it.
- Root cause 2: `NataneToonShaderGUIHelpers` still had a few unconditional `EditorGUILayout.HelpBox(...)` calls for explanatory text.
- Converted the shading-section migration note, shadow-blend explanation, StandardToon mode notice, and high-shadow-blend hint to use `drawHelpToggle(...)` instead of unconditional help boxes.
- Also added missing JP fallback entries for Eye drawer help text such as the partial/symmetry UV usage notes.
- Verification after the fix:
  - no duplicate keys in `JapaneseFallbackExact`
  - `git diff --check -- Editor/NataneToon/GUI/NataneToonShaderGUI.cs Editor/NataneToon/GUI/NataneToonShaderGUIHelpers.cs Editor/NataneToon/GUI/NataneToonLocalization.cs` shows only the existing LF/CRLF warning

## 2026-03-11 Blend parameter wrapper compile fix

- After changing `NataneToonShaderGUIHelpers.DrawBlendParameter(...)` to require `sectionKey` and `drawHelpToggle`, the local wrapper in `NataneToonShaderGUI` still forwarded the old 4-argument shape.
- This caused `CS7036` at `NataneToonShaderGUI.cs(6326,36)`.
- Fixed by updating the wrapper to pass:
  - `propertyName` as the help section key
  - `DrawProperty`
  - `DrawHelpToggle`
- Verification after the patch:
  - `git diff --check -- Editor/NataneToon/GUI/NataneToonShaderGUI.cs Editor/NataneToon/GUI/NataneToonShaderGUIHelpers.cs` shows only the existing LF/CRLF warning

## 2026-03-11 Beginner-facing JP UI cleanup and lilToon compatibility naming

- User reported two issues:
  - JP mode still showed English help/explanation text in several editor tools
  - `StandardToon` looked like a normal user-facing mode even though it is really an internal lilToon compatibility base
- Updated the main beginner-facing inspector UI:
  - `Look Mixer` was relabeled and simplified (`見た目ミキサー`, `見た目プリセット`, `トゥーン寄り`, `作風エフェクト`, `立体感`)
  - lilToon migration warnings/help now describe the compatibility workflow in Japanese
  - lilToon parity detail messages are now localized inline instead of remaining raw English
- Updated shading controls:
  - `_ShadingMode` is now shown as a simplified popup with `トゥーン / グラデーション / PBRライク`
  - the old user-facing `StandardToon` wording was replaced with `lilToon互換ベース`
  - internal compatibility settings are still available for migrated/lilToon Match materials, but no longer presented as a normal artistic mode
  - fixed `_STANDARD_TOON` keyword sync so it only follows the actual 2.x compatibility range and no longer catches `PBRLike`
- Updated other editor tools with direct JP help text / mojibake repair:
  - `NataneToonEyeDrawer`
  - `LilToonMigrationTool`
  - `ShaderPrewarmingEditor`
  - `MaterialPresetBrowser`
  - `UnifiedMaterialEditor`
  - `NataneToonShaderTypeSwitcher`
- Also updated `NataneToonLocalization` fallback entries so any leftover old `StandardToon` strings resolve to the new compatibility-base wording.
- Verification after the patch:
  - `git diff --check` for touched files shows only the existing LF/CRLF warnings
  - `rg -n '\?\?\?\?' Editor/NataneToon` returned no matches

## 2026-03-12 D3D11 sampler fix for tessellation and multi-texture masks

- User reported shader compile errors on D3D11:
  - `tessVert`: unrecognized `sampler_maintex`
  - fragment path: undeclared `sampler_2ndTex`
- Root cause:
  - the shared texture macros mixed `UNITY_DECLARE_TEX2D_NOSAMPLER(...)` textures with sampler names derived from other textures
  - on `UNITY_SEPARATE_TEXTURE_SAMPLER`, the code tried to use `sampler_MainTex` / `sampler_2ndTex` style bindings that were not valid for those NOSAMPLER declarations
- Fix:
  - changed the shared/repeat/clamp sampling macros in `NataneToonInput.hlsl` to use Unity inline samplers (`sampler_linear_repeat` / `sampler_linear_clamp`) on the separate-sampler path
  - updated repeat blur/parallax helpers in `NataneToonUtils.hlsl` to use the same inline repeat sampler
  - replaced remaining direct `UNITY_SAMPLE_TEX2D_SAMPLER(..., _MainTex, ...)` calls in `NataneToonLighting.hlsl` and `NataneToonFragment.hlsl` with the shared repeat macros
- Expected outcome:
  - tessellation vertex/hull/domain paths no longer require `sampler_MainTex`
  - makeup mask sampling no longer expands to missing samplers such as `sampler_2ndTex`
- Follow-up:
  - Unity then reported `sampler_linear_repeat` as undeclared on D3D11 because the inline sampler names still need explicit `SamplerState` declarations.
  - Added back `SamplerState sampler_linear_repeat;` and `SamplerState sampler_linear_clamp;` in `NataneToonInput.hlsl`.
- Verification after the patch:
  - `git diff --check -- Shaders/NataneToon/Include/Core/NataneToonInput.hlsl Shaders/NataneToon/Include/Utils/NataneToonUtils.hlsl Shaders/NataneToon/Include/Lighting/NataneToonLighting.hlsl Shaders/NataneToon/Include/Rendering/NataneToonFragment.hlsl` shows only the existing LF/CRLF warnings
