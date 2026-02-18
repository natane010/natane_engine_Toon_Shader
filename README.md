# Natane Toon Shader

[![Version](https://img.shields.io/badge/version-1.2.0-blue)](https://github.com/natane010/natane_toon_shader/releases)
[![Unity](https://img.shields.io/badge/Unity-2019.4+-black)](https://unity.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

Unity Built-in Render Pipeline向けの、セルルック/NPR表現用シェーダーパッケージです。
VRChat運用を意識した機能（AudioLink、VRC Light Volumes、LTCGI、最適化ツール）を同梱しています。

## 特徴

- `Natane/Toon Shader` を中心に、`Cutout` / `Transparent` バリアントを提供
- 瞳専用 `Natane/Eye`（左右対称UV・マスク運用・表情オーバーレイ対応）
- 特殊表現 `Natane/Toon Shader Wirelight`（Cyber Wireモード、AudioLink連動）
- 画面効果 `Natane/Screen FX Overlay`（GrabPassベースのカスタム画面演出）
- **統合インスペクターUI**: 全シェーダーを1つのインスペクターからドロップダウンで切り替え可能
- 主要Editorツールを `Tools/Natane/...` に集約（日本語UI）

## 動作環境

- Unity: `2019.4+`（Built-in Render Pipeline想定）
- パッケージバージョン: `1.2.0`（`package.json`準拠）

## 収録シェーダー

- `Natane/Toon Shader`
  - 5レイヤー合成（2nd-5th Texture + Mask）
  - Toon/Gradient影、Multi Shadow、Ramp
  - Specular / Rim(2層) / SSS / MatCap(最大3層) / Glitter
  - Normal Map / Cubemap Reflection / Environmental Rim
  - Emission / Dissolve / Refraction / Parallax / Iridescence
  - Hue Shift / Alpha Mask / SDF Map / Shading Grade Map / AO / Shadow Color Texture
  - AudioLink（Emission/Rim/Hue/Dissolve/Outline/Chronotensity）
  - Distance Fade / Vertex Animation / Hologram / Glitch / Decal / Backlight
  - VAT(Houdini) / Main Tex Animation / Backface Texture / Video Texture / LTCGI / Dithering Alpha
- `Natane/Toon Shader (Cutout)` — Opaque版の軽量バリアント（一部高度機能を省略）
- `Natane/Toon Shader (Transparent)` — Cutout同等 + Refraction対応
- `Natane/Toon Shader Wirelight`
  - ジオメトリベースのワイヤー表現
  - Cyber Wireモード（Scanline / Chroma / Glitch）
  - AudioLink連動（パルス・色・グリッチ）
  - Cyber Data Stream（データストリーム表現）
- `Natane/Eye`
  - Eye State: `Normal / Star / Heart / Dead / Nervous`
  - Expression Preset: `Normal / Surprised / Crying`
  - Expression Overlay: `Spiral / Tearful / Shock Rings`
  - 左右分離: Dual Center / Symmetry / Right Eye UV Mirror
  - Eye Region Mask（顔と目が同一マテリアルでも運用可能）
  - Texture Polish / Iris Caustics / Iris Ring Pulse / AudioLink
  - Bubble Effect / Vignette / Transparency Dither / Stencil制御
  - Inner Mesh Priority（BlendShape時の内側描画優先）
- `Natane/Screen FX Overlay`
  - Posterize / Edge Darken / Chromatic Aberration / Vignette / Scanline / Grain

## 主要ツール（`Tools/Natane`）

- `Dashboard`
- `ヘルプ Help`
- `マテリアル Material`
  - マテリアル検証 / マテリアルエディタ / マテリアルプレビュー / マテリアル比較 / メイクアップレイヤー管理
- `プリセット Presets`
  - Material Preset Browser / カラーパレット管理 / デフォルトプリセット生成 / 全プリセット再生成
- `エフェクト Effects`
  - シャドウ調整ウィザード / MatCapレイヤーコンポーザー / ディゾルブパターン生成 / リムライト方向ビジュアライザー / スクリーンエフェクト設定
- `最適化 Optimization`
  - パフォーマンスバジェット / テクスチャ最適化 / アウトライン最適化 / 屈折品質バランサー
- `移行 Migration`
  - lilToon移行 / 一括マテリアル変換 / プレハブバリアント変換
- `シェーダー Shader`
  - シェーダーバリアント収集 / シェーダープリウォーミング
- `ユーティリティ Utility`
  - UVテクスチャ生成 / パーティクルエフェクトエディタ / VTuberプリセット生成
- `VRChat`
  - VRCライトボリュームヘルパー / VRC Light Volumes 再検出 / LTCGI 再検出

## 統合インスペクターUI

v1.2.0 より、全シェーダー（Toon / Eye / Wirelight / Screen FX）のインスペクターが統合されました。

- マテリアルを選択すると、インスペクター最上部に **「シェーダータイプ」ドロップダウン** が表示されます
- ドロップダウンから `Toon` / `Eye` / `Wirelight` / `Screen FX` を選択するだけでシェーダーが切り替わります
- 切り替え時は確認ダイアログが表示され、Undo（Ctrl+Z）にも対応しています
- 各シェーダーに最適化された専用UIが自動的に表示されます
  - **Toon**: 5タブ構成（基本/ライティング/エフェクト/環境/詳細）+ レンダリングモード切替（Opaque/Cutout/Transparent）
  - **Eye**: 20セクション構成（瞳状態/デュアルセンター/表情オーバーレイ/虹彩エフェクト等）
  - **Wirelight**: 12セクション + 9種プリセットボタン（サイバーパンク/ホログラム/AudioLink等）
  - **Screen FX**: 4セクション構成（ブレンド/トゥーン化/画面歪み/シネマティック）

## サードパーティライティング統合

### VRC Light Volumes（ボクセル型ライティング）

[VRC Light Volumes](https://github.com/REDSIM/VRCLightVolumes) パッケージがプロジェクトにインストールされていると、**自動的に検出**され、本物の `LightVolumes.cginc` を使用します。

- **自動検出**: `red.sim.lightvolumes` パッケージのインストール/アンインストールを自動検知
- **対応ワールドで自動動作**: マテリアルで「Light Volume有効」をONにするだけで、対応ワールドのボクセルライティングを自動的に受けます
- **フォールバック**: パッケージ未インストール時はUnityライトプローブに自動フォールバック
- **3ブレンドモード**: Add（デフォルト）/ Multiply / Replace
- **スペキュラー対応**: Light Volumeからのカラースペキュラー生成
- **手動再検出**: `Tools > Natane > VRChat > VRC Light Volumes 再検出`

### LTCGI（リアルタイムエリアライト）

[LTCGI](https://github.com/PiMaker/ltcgi) パッケージがプロジェクトにインストールされていると、**自動的に検出**され、本物の `LTCGI.cginc` を使用します。

- **自動検出**: `at.pimaker.ltcgi` パッケージのインストール/アンインストールを自動検知
- **対応ワールドで自動動作**: マテリアルで「LTCGI有効」をONにするだけで、スクリーンやエリアライトからの照明を自動的に受けます
- **アバターモード**: `LTCGI_AVATAR_MODE` を自動設定（ライトマップUV不要）
- **Diffuse + Specular**: エリアライトからの拡散光・反射光を個別に強度調整可能
- **手動再検出**: `Tools > Natane > VRChat > LTCGI 再検出`
- **パッケージ未検出時**: インスペクターUIが自動的に無効化され、インストール案内を表示

### 統合アーキテクチャ

両システムとも同じ自動検出パターンを採用しています：

```
パッケージインストール → エディタ起動時に自動検出 → Config hlsl生成
→ シェーダー再コンパイル → 本物のcginc関数を使用
→ 対応ワールドで自動的にライティングを受ける
```

## クイックスタート

### 1. Toon Shader

1. Materialを作成し、Shaderを `Natane/Toon Shader` に設定（またはインスペクターのシェーダータイプで「Toon」を選択）
2. 必要に応じて `Cutout` / `Transparent` バリアントに切替
3. まずは `Shadow Steps`, `Shadow Sharpness`, `Rim`, `Outline` を調整

### 2. Eye Shader

1. 目用Materialを作成し、Shaderを `Natane/Eye` に設定（またはインスペクターのシェーダータイプで「Eye」を選択）
2. 目UVが左右対称配置の場合:
   - `Eye Center Mode = Symmetry From Center1`
   - `Symmetry Pivot X` を調整
   - 必要に応じて `Mirror Right Eye UV` をON
3. 顔と目が同一マテリアルの場合:
   - `Use Eye Region Mask` をON
   - `Eye Region Mask` と `Mask Channel` を設定

### 3. Wirelight + Cyber

1. `Natane/Toon Shader Wirelight` を適用（またはインスペクターのシェーダータイプで「Wirelight」を選択）
2. `Wire Style = Cyber Wire` を選択
3. `Enable AudioLink` と `AudioLink Cyber` を必要に応じてON

### 4. Screen FX Overlay

1. `Tools/Natane/エフェクト Effects/スクリーンエフェクト設定 Screen FX Setup` を実行
2. 生成マテリアルを調整して画面演出を追加

## 最適化ガイド（要点）

- 不要機能はトグルでOFFにし、キーワード数を抑える
- VR向けでは `Refraction`, `GrabPass系`, 多重エフェクトの同時使用を最小化
- `Shader Variant Collector` と `Shader Prewarming` を併用して実機負荷を安定化
- 半透明重なりの破綻対策として、用途に応じて `Cutout` や `Dithering Alpha` を検討

## スタイル指向（参考）

- パラメータ調整により、フラットなアニメ調からリッチなNPR表現まで幅広いセルシェーディングスタイルに対応します
- 既定構成は、NPRセル影を軸にした実用寄りのバランスです
- 詳細監査: `Documentation~/SHADER_STYLE_AND_OPTIMIZATION_AUDIT.md`

## ドキュメント

- 変更履歴: `CHANGELOG.md`
- 移行: `Documentation~/MIGRATION_GUIDE.md`
- クイックスタート: `Documentation~/QUICK_START.md`
- 技術資料: `Documentation~/TECHNICAL.md`
- Eye詳細: `Shaders/NataneToon/Eye/NataneToonEye使用方法.md`
- Eyeテクスチャ運用: `Shaders/NataneToon/Eye/NataneToonEye_テクスチャ対応.md`
- Wirelight詳細: `Shaders/NataneToon/NataneToonWirelight_README.md`
- ScreenFX詳細: `Shaders/NataneToon/Effects/NataneScreenFX_README.md`
- ランプテクスチャ: `Documentation~/RAMP_TEXTURE_GUIDE.md`
- パーティクル: `Documentation~/PARTICLE_SYSTEM_GUIDE.md`
- コントリビュート: `Documentation~/CONTRIBUTING.md`

## ライセンス

MIT License。詳細は `LICENSE` を参照してください。
