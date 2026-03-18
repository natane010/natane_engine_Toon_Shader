# Auto Character Setup Plan

Date: 2026-03-18
Target: `NataneToonShaderGUI` / `LilToonMigrationTool` / 補助生成ツール群

## ゴール

Natane Toon Shader の設定コストを下げて、できるだけ少ない操作で
「最新 3D ゲームのキャラクター寄り」の見た目に到達できる導線を作る。

ここで目指すのは完全自動の魔法変換ではなく、
少数の意図だけを選べば安全に土台が整う `guided auto setup` です。

対象ユーザー:
- Unity に不慣れな利用者
- まず破綻なく見た目を作りたいアーティスト
- lilToon から移行した後に Natane 向けへ寄せたい利用者

## 現在の課題

1. 機能は多いが、目的ベースでまとまっていない
- Workflow
- Quick Setup
- Look Mixer
- Presets
- Shadow Wizard
- Migration
- Validator

上記が別々に存在していて、何をどの順で使うか分かりづらい。

2. 見た目づくりに必要な補助データ生成が個別操作
- SDF
- Ramp
- Smooth Normal

これらは見た目に効くが、初心者には生成の必要性自体が伝わりにくい。

3. lilToon 移行後のマテリアルで、Natane 向け正規化の意図が伝わりづらい
- 互換寄りで維持するのか
- Natane 向けへ寄せるのか

この分岐が UI と自動処理でまだ一本化されていない。

4. 最終状態の妥当性確認が手動依存
- パフォーマンス過多
- 不足テクスチャ
- 不要機能の有効化
- 役割に対して過剰な feature 構成

## 設計方針

### 1. 目的ベースでまとめる

「機能を個別に選ぶ UI」ではなく、
「何を作りたいか」から逆算して設定をまとめて適用する。

### 2. 既存ツールを作り直さずオーケストレーションする

新規開発の中心は 1 つの司令塔 UI と実行パイプラインにして、
既存の資産を内部的に再利用する。

再利用候補:
- `NataneToonShaderGUI` の Workflow / Quick Setup / Category Preset
- `LilToonMigrationTool`
- `ShadowAdjustmentWizard`
- `SmoothNormalBaker`
- `NataneToonSdfAutoGenerator`
- `MaterialPresetBrowser`
- `MaterialValidator`
- `NataneCrossVariantEditor`

### 3. 非破壊・再実行可能を優先する

- 既存マテリアルはなるべく GUID を維持
- 補助生成物は再生成可能な場所へ保存
- 何を自動適用したか結果サマリを出す
- もう一度実行しても破綻しにくい構造にする

### 4. フル自動より少数選択 + 自動補完

最初にユーザーに選ばせるのは最小限にする。

想定入力:
- 素材の役割
- 目標ルック
- 品質ターゲット
- Natane ベースか lilToon 移行ベースか

## 最優先で自動化すべき項目

### Priority A: Auto Character Setup ハブ

Inspector 上部に `Auto Character Setup` セクションを追加する。

主な入力:
- Role
  - Face / Skin
  - Hair
  - Eyes
  - Clothing
  - Props
- Look Target
  - Sharp Anime
  - Soft Painting
  - Game Character
  - Toon-PBR Hybrid
  - Near PBR
- Workflow Base
  - Natane
  - lilToon Migration
- Quality Target
  - Mobile
  - Standard
  - High

主ボタン:
- `解析して自動設定`
- `補助マップだけ再生成`
- `結果を検証`

### Priority B: Workflow 正規化

Auto Character Setup 実行時に最初に決めるべきもの:
- Natane 仕様で編集するのか
- lilToon 移行仕様で保持するのか

これを後段のプリセット適用前に必ず確定する。

### Priority C: 補助データの自動生成

初期実装で自動生成対象にするもの:
- SDF
- Ramp
- Smooth Normal

条件例:
- 顔 / 影制御が有効なら SDF 候補を解析
- Ramp 使用モードなら自動生成
- Outline を使うなら Smooth Normal 生成候補を提示または自動実行

### Priority D: 見た目プリセットの目的別適用

既存の Quick Setup / Category Preset / Game Character Style を、
Role と Look Target に応じて内部で選択する。

例:
- Face + Game Character
  - Game Character をベース
  - SDF 強め
  - Rim / Specular は控えめ
- Hair + Game Character
  - Hair Specular 優先
  - Rim は強め
  - Outline は中程度
- Clothing + Near PBR
  - Toon-PBR Hybrid or Near PBR ベース

### Priority E: 自動検証と軽微な自動修正

最後に Validator 相当の確認を流して、問題を結果サマリに出す。

最低限チェック:
- sampler 予算
- 不足テクスチャ
- 未使用 feature
- Role に対して過剰な feature

自動修正の範囲:
- keyword / toggle 同期
- 明らかな不要 feature の無効化
- `_UseSDFMap` や `_USE_RAMP` などの整合性調整

## 実行パイプライン案

`Auto Character Setup` 実行時の基本順序:

1. 入力マテリアル解析
- shader type
- workflow 状態
- active feature
- 利用可能テクスチャ
- migrated material かどうか

2. Workflow 確定
- Natane
- lilToon migration

3. ベースルック適用
- Quick Setup or Category Preset
- Role 補正

4. 補助生成物の作成 / 再利用
- SDF
- Ramp
- Smooth Normal

5. feature 整理
- 必要機能だけ ON
- 不要機能は OFF
- toggle と keyword を同期

6. 最終補正
- 役割別のパラメータ補正
- 品質ターゲット別の軽量化

7. 検証
- Validator
- sampler budget
- 足りない要素のレポート

8. 結果サマリ表示
- 何を自動で設定したか
- 何を生成したか
- 何が未解決か

## フェーズ分割

## Phase 1: Orchestrator 最小版

目的:
- まず 1 ボタン導線を成立させる

実装内容:
- `Auto Character Setup` UI
- Workflow 切替
- Quick Setup / Category Preset の内部呼び出し
- SDF 自動生成呼び出し
- Ramp 自動生成呼び出し
- 結果サマリ表示

成功条件:
- 新規 Natane マテリアルで 1 回の操作後に大きく破綻しない
- lilToon 移行材でも Natane 寄り / 互換寄りを選べる

## Phase 2: 補助生成の拡張

目的:
- 見た目品質に効く補助データをより自動化する

実装内容:
- Smooth Normal Baker の統合
- Role 別の生成条件最適化
- 生成物の保存場所と再実行ルール整備

成功条件:
- Outline 使用時に Smooth Normal 不足が減る
- 顔マテリアルで SDF 設定の手作業が大きく減る

## Phase 3: Role-aware ルック適用

目的:
- 同じプリセットを全部にかける雑さをなくす

実装内容:
- Face / Hair / Eyes / Clothing ごとの補正テーブル
- `Game Character` の role 別派生
- Quality Target に応じた feature 削減

成功条件:
- 顔・髪・服で同じ見た目セットをかけても破綻しにくい
- 高品質 / 軽量の差が分かりやすい

## Phase 4: Batch / Pipeline 対応

目的:
- 単一マテリアルだけでなく運用に乗せる

実装内容:
- 複数選択マテリアル一括適用
- Prefab / Avatar 単位の適用
- 変換結果レポート保存

成功条件:
- キャラクター一式をまとめて整えられる
- 再実行しても参照や GUID が壊れにくい

## MVP の非対応項目

初期版では無理に入れないもの:
- 完全自動の役割推定
- 画像解析ベースの高度な質感判定
- すべての補助マスク自動生成
- どんな入力でも最新 AAA 品質へ保証すること

ここは過剰に約束しない。

## 検証観点

### 基本確認
- 新規 Natane マテリアルで実行できる
- lilToon 移行材で実行できる
- Undo が効く
- 再実行で致命的に崩れない

### 見た目確認
- Face / Hair / Clothing の 3 種で大きく破綻しない
- Game Character プロファイルで現代ゲーム寄りの土台になる
- Natane / lilToon migration workflow の差が UI 上で明確

### 運用確認
- 補助生成物の保存場所が分かりやすい
- GUID を壊さない
- Auto setup 後に何が行われたか追える

## 実装開始順

1. `NataneToonShaderGUI` に `Auto Character Setup` UI を追加
2. Workflow 切替と既存 Quick Setup の統合呼び出し
3. SDF / Ramp の自動生成統合
4. 結果サマリ UI 追加
5. Smooth Normal 統合
6. Validator 連携
7. 複数マテリアル一括適用

## 期待される効果

- 初心者が「どこから触ればいいか」で止まりにくくなる
- 既存機能を殺さず、入口だけを強くできる
- lilToon 移行後の整え直しが楽になる
- 見た目づくりの初速が大幅に上がる
- 将来的に pipeline 的な batch 化へ伸ばしやすい
