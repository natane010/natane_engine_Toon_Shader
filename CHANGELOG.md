# Changelog

All notable changes to Natane Toon Shader will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.13] - 2026-02-25

### Performance
- **AO テクスチャ二重サンプリング除去**: `_USE_RAMP` 分岐の内外で同一の `SampleTex2DBlur1(_AOMap, ...)` を2回呼んでいたのを、分岐前に1回だけサンプリングしてキャッシュするように最適化。テクスチャフェッチ1回削減
- **pow(x, 2.0) → x * x 最適化**: Vertex Animation の Pulse モードで `pow(abs(sin(...)), 2.0)` を乗算に置換。pow() 命令1回削減（5-8 ALU 節約）
- **RimLight Direction 共通部分式除去（CSE）**: Rim Light 1 と Rim Light 2 で `normalize(_RimLightDirection.xyz)` を2回計算していたのを、1回だけ計算してキャッシュ。normalize() 1回削減（5 ALU 節約）

### Changed
- **シェーダーコード構造化コメント追加**: NataneToonInput.hlsl にセクション分けコメント（Core Rendering / Makeup / Lighting / Effects 等）を追加し可読性を向上
- **シェーダーバリアント Properties コメント追加**: 全3バリアント（Opaque/Cutout/Transparent）にセクション分けコメントを追加

---

## [1.2.12] - 2026-02-25

### Added
- **リムライト方向追従（lilToon方式Half-Lambert）**: `_RimDirStrength` パラメータ追加。ライトの方向にリムが追従する機能。0=全方向リム、1=ライト側のみリム。Rim Light 1/2/Environmental Rim の3種に対応
- **リムライト影マスク**: `_RimShadowMask` パラメータ追加。影の領域でリムを抑制する機能。0=影でもリム表示、1=影でリム消失
- **リムライト ForwardAdd 対応**: Rim Light 1/2/Environmental Rim がポイントライト・スポットライトに対応。各ライトの色・距離減衰・`_AdditionalLightIntensity` で変調
- **バックライト ForwardAdd 対応**: バックライトがポイントライト・スポットライトに対応
- **ディレクショナルライト非依存化**: SHフォールバックでポイント/スポット/ベイク環境対応
- **各エフェクト個別距離フェード**: 21エフェクトに個別の距離フェード機能を追加

### Fixed
- **ライト方向フォールバックチェーン強化**: ポイント/スポットライト環境でのライト方向取得を改善

---

## [1.2.10] - 2026-02-24

### Fixed
- **Outlineパス shader_feature_local 宣言欠落修正**: 全3バリアント（Opaque/Cutout/Transparent）のOutlineパスに `_OUTLINE_WIDTH_MAP`、`_OUTLINE_MULTI_COLOR`、`_OUTLINE_MASK` の `shader_feature_local` 宣言を追加。アウトラインのキーワード切り替えが正しく機能するように
- **KajiyaKay ヘアスペキュラー NaN防止**: `KajiyaKaySpecular()` の `pow()` に `saturate()` ラッパーを追加。大きな指数値での未定義動作を防止
- **VR SPI ForwardBase インスタンスID伝播修正**: ForwardBase頂点シェーダーに `UNITY_TRANSFER_INSTANCE_ID(v, o)` を追加。VR Single Pass Instanced環境でのインスタンスID伝播チェーンを完成
- **_VATPadding プロパティ宣言追加**: CBUFFERに定義されていたがProperties{}ブロックに宣言がなかった `_VATPadding` を全3バリアントに追加
- **AO強度GUI追加**: DrawAOSection()に `_AOIntensity` プロパティ表示を追加
- **影色テクスチャToggle制御追加**: DrawShadowColorTextureControls()に `_SHADOW_COLOR_TEX` キーワードトグルを追加
- **リム方向制御GUI追加**: DrawRimLightSection()にリム方向制御トグル・`_RimLightDirection`・`_RimDirectionRange` プロパティ表示を追加

### Added
- **Vertex Animation GUIセクション**: DrawAdvancedTab()にVertex Animation（Wind/Breath/Pulse）の完全なGUIセクションを追加。アニメーション種類・速度・強度・周波数・マスクの制御UI
- **SmoothNormalBaker ツール**: スムースノーマルベイクツールを追加
- **ShaderGUIStyles 分離**: GUI スタイル定義を専用ファイルに分離

---

## [1.2.9] - 2026-02-23

### Added
- **AK:EF Style 3機能追加**: Face SDF回転・ヘアスペキュラー・アウトラインテクスチャカラー

### Changed
- バージョンアップ

---

## [1.2.8] - 2026-02-23

### Fixed
- **Wirelight ジオメトリシェーダー VR SPI 完全修正**: g2f構造体に `UNITY_VERTEX_INPUT_INSTANCE_ID` を追加し、ジオメトリ→フラグメント間のインスタンスID伝播チェーンを完成。フラグメントシェーダーに `UNITY_SETUP_INSTANCE_ID` を追加。VR Single Pass Instanced 環境での片目描画崩れを修正
- **Wirelight pow() 精度喪失防止**: `pow(Triangles, 2.2)` および `pow(col.rgb * brightness, power)` に `max(x, 0.0)` ガードを追加。GPU依存の未定義動作を防止
- **Wirelight Distance Fade pow() ガード**: `pow(distanceFade, power)` に非負ガードを追加
- **Eye texCol 初期化強化**: Dead/Nervous ステートでテクスチャサンプリングを `_UseTexture` チェックの外に移動。TexturePolish に常に有効なテクスチャデータが渡されるように
- **Eye Nervous ループ sizeStep ガード強化**: `_NervousLinesSize` に最小値 0.02 を設定し、ゼロ除算のエッジケースを完全に排除
- **Eye atan2 定義域エラー修正**: IrisCaustics と ExpressionOverlay の `atan2()` にゼロベクトルガード（`dot(localUV, localUV) > 1e-10`）を追加。瞳孔中心でのNaN発生を防止
- **Eye AudioLink バンド選択型変換**: `_BandSelection` を `(int)` で明示的にキャスト。Float→Int の暗黙変換による精度問題を防止
- **Noise 負値保護**: `pattern()` の戻り値を `saturate()` で [0,1] に制限。下流の計算での負値伝播を防止
- **AudioLink ThemeColor 範囲外防止**: `AudioLinkGetThemeColor()` で取得したカラー値に `max(color, 0.0)` の非負保証と `saturate()` によるアルファ制限を追加

### Changed
- **lilToon 移行ツール改善**: 移行時に基本タブ以外の全機能をオフにした状態でマテリアルを生成するように変更。プロパティ値は保持されるため、移行後に必要な機能を個別に有効化可能

---

## [1.2.7] - 2026-02-21

### Fixed
- **VR Single Pass Instanced (SPI) 互換性修正**: Outline / ShadowCaster パスに VR ステレオインスタンシングマクロ一式を追加（`UNITY_VERTEX_INPUT_INSTANCE_ID`, `UNITY_VERTEX_OUTPUT_STEREO`, `UNITY_SETUP_INSTANCE_ID`, `UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO`, `#pragma multi_compile_instancing`）。全3バリアント（Opaque/Cutout/Transparent）対応
- **Fragment シェーダー VR 修正**: `UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX` をフラグメントシェーダー先頭に追加。VR環境でのスクリーンスペーステクスチャサンプリング（シャドウマップ、GrabPass等）が正しい目のインデックスを参照するように
- **GrabPass（屈折）VR ステレオ対応**: `sampler2D _GrabTexture` を `UNITY_DECLARE_SCREENSPACE_TEXTURE(_GrabTexture)` に置換、全サンプリングを `UNITY_SAMPLE_SCREENSPACE_TEXTURE` に変更。VR SPI モードで屈折エフェクトが両眼で正しくレンダリングされるように
- **AudioLink コンパイルエラー修正**: Rim Light / Dissolve 機能無効時に AudioLink の対応サブ機能がコンパイルエラーになる問題を修正（キーワードガード追加）
- **グリッチ RGB Split マゼンタ修正**: RGB Split がライティング未適用の生テクスチャ値と適用済みの値を混合していたため、影部分でマゼンタ色（エラー色）が発生していた問題を修正。デルタ方式に変更しライティングを保持
- **Outline パス VR 修正**: 全3バリアントの Outline フラグメントシェーダーに `UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX` を追加。VR でアウトラインが正しくレンダリングされるように
- **ShadowCaster パス VR 修正**: Opaque/Cutout の ShadowCaster に `UNITY_TRANSFER_INSTANCE_ID` + `UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX` を追加。VR でシャドウが正しく描画されるように
- **Wirelight VR 完全対応**: `#pragma multi_compile_instancing` 追加、全 `shader_feature` を `shader_feature_local` に変更、v2g 構造体にステレオ出力追加、ジオメトリシェーダーで `UNITY_TRANSFER_INSTANCE_ID` / `UNITY_TRANSFER_VERTEX_OUTPUT_STEREO` による頂点→ジオメトリ→フラグメント間のステレオ情報転送を修正

---

## [1.2.5] - 2026-02-19

### Changed
- コードコメント・ドキュメント・UIテキストのブランド参照を一般的な技術用語に統一
- LVブレンドモード名称を「Natural」に統一

### Removed
- YMToon移行ツールを削除

---

## [1.2.4] - 2026-02-19

### Changed
- LVブレンドモード名称を「Natural」に変更。機能の動作（`max(indirect, direct + additional)` 合成）をより直感的に表す名前に

---

## [1.2.3] - 2026-02-19

### Added
- **Natural ライティングパイプライン（max合成方式）**: `max(indirect, direct + additional)` 合成方式を導入。髪と顔が分かれたモデルでも LightVolume / LTCGI の環境色が全メッシュに正しく反映されるように
- **LVブレンドモード Natural(3)**: LightVolume を間接光として max() 合成に参加させる新モード。デフォルトに設定（既存の Add/Multiply/Replace も互換維持）
- **`_IndirectLightMinColor`**: 間接光の最低保証カラー。暗いワールドでもキャラクターが真っ黒にならないよう下限を設定
- **`_ShadowEnvStrength`**: 影への環境色反映強度。環境光の色味を影に反映してより自然なライティングを実現

### Changed
- NdotL 計算を生 dot 値方式（max(0,...) なし）に変更。`_ShadowOffset` との組み合わせでより柔軟な影制御が可能に
- Shadow Attenuation を NdotL から分離し、シェーディング段階に後段適用。クリーンなトゥーン境界と滑らかなシャドウマップ暗化を両立
- AO 適用位置を lighting 均一暗化から shadingValue 段階適用に変更（間接光は 50% 制限、Natural 方式）
- `_GIIntensity` デフォルト値を 0 → 0.5 に変更（既存マテリアルは保存値を使用するため影響なし）

### Fixed
- PCFシャドウのステレオインスタンシング（VR）互換性を修正

---

## [1.2.2] - 2026-02-17

### Added
- **バンドル版 LightVolumes.cginc**: RED_SIM 氏の LightVolumes.cginc を MIT License に基づきバンドル同梱。VRC Light Volumes パッケージ未インストール時でも本物のサンプリングコードが使用され、Light Volume の色を正しく受け取れるように
- パッケージインストール済み環境では引き続きパッケージ版を優先使用

### Changed
- VRC Light Volumes 未検出時のフォールバック方式を ShadeSH9 ベースからバンドル版 LightVolumes.cginc に変更
- エディタ UI のメッセージを「フォールバック」から「バンドル版で全機能利用可能」に更新
- パッケージ未検出時の HelpBox を Warning から Info に変更

### Removed
- ShadeSH9 ベースの互換関数4つ（LightVolumeSH, LightVolumeEvaluate, LightVolumeSpecular, LightVolumesEnabled）を削除。バンドル版が内部で Unity Light Probes への自動フォールバックを提供

---

## [1.2.1] - 2026-02-17

### Added
- **シャドウマップスムージング (PCF)**: ディレクショナルライトのスクリーンスペースシャドウマップに対するPCF 9タップフィルタリングを追加。影エッジの本物のアンチエイリアシングを実現
- **適応型シャドウスムージング**: ポイント/スポットライトに対する適応型中心点のsmoothstepスムージングを追加
- **多段階トゥーンシェーディングの階調なじませ**: `_ShadowSmoothing` でトゥーンステップ境界を連続的なグラデーションにブレンド。多段階影の諧調が滑らかに
- **LTCGIフォールバック実装**: LTCGIパッケージ未インストール時にSH + Reflection Probeで近似する互換レイヤーを追加

### Changed
- シャドウスムージングの処理順序を変更: Smoothing → Shadow Receive Mask（PCFが生のシャドウ値で正しく動作するように）
- シャドウスムージングのヘルプテキストを更新（PCF・多段階なじませの説明追加）

### Fixed
- VCC/VPM URLをカスタムドメイン（natanetoon.com）に変更
- リポジトリ名をnatane_toon_shaderに更新

### Documentation
- ドキュメントサイトを個別ページ構造にリストラクチャリング

---

## [1.1.4] - 2025-01-06

### 🚀 Added - Performance Optimizations
**機能を一切削除せずに大幅な軽量化を実現！**

#### Phase 1: パフォーマンス基盤の最適化
- **Luminanceマクロ**: `LUMA_WEIGHTS`と`CALC_LUMINANCE()`を追加し、重複計算を50%削減
- **Refraction Blur最適化**: テクスチャサンプル数を9→5に削減（45%削減）、品質は維持
- **half精度の活用**: Fragment/Lighting計算を最適化、GPU命令数を20-30%削減（Quest向けに特に効果的）

#### Phase 2: 計算効率の改善
- **SafeAdditiveBlend最適化**: 約40%高速化
- **条件分岐の最適化**: lerp/stepによる分岐削減でGPU効率向上

#### Phase 3: 細かい最適化
- **HSV変換のスキップ**: デフォルト値時に変換を回避
- **Vertex正規化の条件付き最適化**: Normal Map未使用時は正規化をスキップ
- **Tone Mapping関数の最適化**: half精度化とコンパイル時定数の活用

#### 総合効果
- テクスチャサンプル: 45%削減
- GPU命令数: 20-30%削減
- ドット積計算: 50%削減
- GPU分岐: 大幅削減
- **視覚品質**: 変更なし ✨

### 🎨 Added - Refraction Feature
- 物理ベースの屈折計算（Snellの法則）
- IOR（屈折率）調整: 1.0～3.0
- 最適化された5サンプルブラー
- マスクによる部分的な屈折制御

### Changed
- すべての主要計算をhalf精度に最適化
- Luminance計算のキャッシュ化
- 条件分岐をlerp/stepに置き換え

### Performance
- VRChat Quest向けに大幅な軽量化
- アバターパフォーマンスランクの改善が期待
- 既存マテリアルの互換性を完全維持

---

## [1.1.3] - 2024-11-05

### Added
- **レンダリングモード選択**: Opaque/Cutout/Transparent
- **アルファマスク機能**: 部分的な透明度制御
- **3つのシェーダーバリアント**: 用途に応じた最適化

### Changed
- フォルダ構造の整理
- Shader variantsの分離

---

## [1.1.2] - 2024-11-04

### Added
- VTuberプリセット自動生成機能
- フォルダ構造の大幅整理

### Fixed
- ディレクトリ作成エラーの修正

---

## [1.1.1] - 2024-10-31

### Fixed
- インスペクタープロパティ反映の修正
- UIバグの修正

---

## [1.1.0] - 2024-10-30

### Added
- プリセット適用時のインスペクターUI自動更新

### Changed
- UIの応答性向上

---

## [1.0.9] - 2024-10-29

### Added
- キャラクタープリセット23種追加（合計59種）
- アウトラインマスク機能
- ライトカラー影響度調整
- VTuberプリセット改善

---

## [1.0.8] - 2024-10-28

### Added
- プリセット自動生成機能
- バッチ処理機能の改善

---

## [1.0.7] - 2024-10-27

### Added
- シーンマテリアル編集ウィンドウ
- リアルタイム編集機能

### Fixed
- 各種バグ修正

---

## [1.0.6] - 2024-10-26

### Added
- Toon/NPRスタイルプリセット10種追加
- プリセットライブラリの拡充

---

## [1.0.5] - 2024-10-25

### Added
- 包括的なエラーハンドリング
- 安定性の向上

---

## [1.0.4] - 2024-10-24

### Fixed
- ShaderGUIのバグ修正
- UIの安定性向上

---

## [1.0.3] - 2024-10-23

### Fixed
- v1.0.2のバグ修正

---

## [1.0.2] - 2024-10-22

### Changed
- 内部処理の改善

---

## [1.0.1] - 2024-10-21

### Added
- **VRC Light Volumes対応**: 次世代ボクセルベースライティング
- **完全日本語UI**: すべてのインスペクターとエディタが日本語対応

### Changed
- ライティングシステムの拡張

---

## [1.0.0] - 2024-10-20

### Added
- 初回安定版リリース
- 完全日本語UI対応
- 基本的なトゥーンシェーディング機能
- VRChat最適化

---

## 凡例

- `Added`: 新機能
- `Changed`: 既存機能の変更
- `Deprecated`: 非推奨機能
- `Removed`: 削除された機能
- `Fixed`: バグ修正
- `Security`: セキュリティ修正
- `Performance`: パフォーマンス改善
