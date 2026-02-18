# Natane Screen FX Overlay

`Natane/Screen FX Overlay` は、VRCワールド向けのカスタム画面効果シェーダーです。
`GrabPass` で描画済み画面を取得し、フルスクリーンQuad上で画面加工します。

## 特徴
- Posterize + Edge Darken でトゥーン調の画面演出
- Chromatic Aberration / Vignette / Scanline / Grain を1マテリアルで調整
- ランタイムスクリプト不要（Editorで配置して使える）

## クイックセットアップ
1. メニュー `Tools/Natane/エフェクト Effects/スクリーンエフェクト設定 Screen FX Setup` を実行
2. カメラ直下に `NataneScreenFXOverlay` が作成される
3. 生成された `NataneScreenFXOverlay.mat` を調整

## 推奨パラメータ（Natane Toon併用）

### Anime Boost
- `_Intensity`: `0.85`
- `_PosterizeStrength`: `0.20`
- `_PosterizeSteps`: `8`
- `_EdgeStrength`: `0.30`
- `_Vignette`: `0.08`
- `_GrainStrength`: `0.03`

### Dreamy / Soft
- `_Intensity`: `0.70`
- `_PosterizeStrength`: `0.08`
- `_ChromaticAberration`: `0.15`
- `_Vignette`: `0.18`
- `_VignetteSoftness`: `0.45`
- `_ScanlineStrength`: `0.05`

### Club / Cyber
- `_Intensity`: `1.0`
- `_PosterizeStrength`: `0.35`
- `_EdgeStrength`: `0.45`
- `_ChromaticAberration`: `0.40`
- `_ScanlineStrength`: `0.20`
- `_GrainStrength`: `0.10`

## 注意
- `GrabPass` は負荷が高くなるため、Questでは効果量を控えめにしてください。
- Overlay Quad が見えない場合は、カメラの Culling Mask とレイヤー設定を確認してください。
