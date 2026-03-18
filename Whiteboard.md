# Whiteboard - キーワード同期問題 全体調査 (2026-03-14)

## 全チーム完了 ✅ → 全修正完了 ✅✅

| チーム | 担当領域 | ステータス | 問題数 | 修正状況 |
|--------|---------|------|--------|---------|
| Team A | shader_feature_local ⇔ KeywordMappings 完全一致検証 | ✅ 完了 | 18個不足 | ✅ 修正済 |
| Team B | CBUFFER #ifdef 問題調査 | ✅ 完了 | 80ブロック (Low risk) | ⏭ 対応不要 |
| Team C | Editor内キーワード操作の整合性 | ✅ 完了 | Critical 2 / High 5 | ✅ 修正済 |

## 修正内容

### 1. KeywordMappings に13個追加 (Team A)
**ファイル**: `Editor/NataneToon/Integration/NataneShaderKeywordSynchronizer.cs`
追加キーワード:
- `_ANGEL_RING`, `_DETAIL_MAP`, `_PROCEDURAL_AO`, `_NORMAL_WARP`
- `_SPECULAR_AA`, `_VERTEX_COLOR_SHADOW`, `_FACE_SDF_ROTATION`
- `_SHEEN`, `_SSS_LUT`, `_SURFACE_COVER`, `_TRIPLANAR`, `_HEIGHT_FOG`, `_FUR`

### 2. LilToonMigrationTool Critical修正 (Team C)
**ファイル**: `Editor/NataneToon/Migration/LilToonMigrationTool.cs`
- line 1277: `_STANDARD_TOON` enable前に `_LilToonExactCompatibility = 1.0f` を設定
- line 1809: `_USE_MULTI_SHADOW` enable前に `_UseMultiShadow = 1.0f` を設定

### 3. UnifiedMaterialEditor High修正 (Team C)
**ファイル**: `Editor/NataneToon/Tools/UnifiedMaterialEditor.cs`
- `ApplyFeatureToggle()`: キーワード enable/disable 時に対応プロパティ値も設定
- `DisableAllFeatures()`: 全キーワード disable 時に対応プロパティ値を 0.0f にリセット
- `SetTogglePropertyForKeyword()` ヘルパー追加 (KeywordMappings の逆引き)

### 4. CBUFFER (Team B) - 対応不要
80個の #ifdef ブロック。Built-in RP では名前ベースアップロードのため直接的リスクは Low。

## 2026-03-18 lilToon互換モード脱却 — ワンクリック高品質移行

### チーム構成
| チーム | 担当 | ステータス |
|--------|------|----------|
| Stream A | AutoFixer 新規作成 (`NataneLilToonAutoFixer.cs`) | ✅ 完了 |
| Stream B | シェーダーコード削除 (Fragment/Lighting/Input/Build/.shader) | ✅ 完了 |
| Stream C | ShaderGUI + Helpers UI 修正 | ✅ 完了 |
| Stream D | MigrationTool 修正 + バッチアップグレード | ✅ 完了 |

### 削除対象まとめ
- `_STANDARD_TOON` keyword + pragma (全13シェーダー)
- `LilToonShading()` / `LilBlendColor()` (Lighting.hlsl)
- Fragment.hlsl の `#ifdef _STANDARD_TOON` ブロック5箇所
- `_LilToonExactCompatibility` + `_ST*` プロパティ群
- ShaderGUI: Look Mixer ロック、StandardToon UI、互換トグル
- ShaderGUIHelpers: `DrawStandardToonSettings()`, `IsStandardToonMode()`
- Migration: ExactCompatibility モード
- KeywordSynchronizer: _STANDARD_TOON 導出ロジック

### 保持するメタデータ (HideInInspector)
- `_LilToonMigrated`, `_LilToonMigrationMode`, `_LilToonParityFlags`

---

## 2026-03-18 SDF自動生成UI

- `Editor/NataneToon/GUI/NataneToonSdfAutoGenerator.cs` を追加して、`_ShadowReceiveMask` 優先、なければ `_MainTex` の alpha / grayscale から SDF を自動生成して `_SDFMap` に適用する流れを追加。
- 生成物はマテリアル近傍の `NataneToon/SDF` フォルダーへ保存し、すでに `NataneToon` 配下なら二重に `NataneToon/NataneToon` を作らないように調整。
- `NataneToonShaderGUIHelpers.DrawSDFShadowMapControls()` に `SDFを自動生成して適用` ボタンを追加。`Use SDF Shadow Map` が OFF でも押せて、成功時はそのまま SDF を有効化。
- Unity 実機での import / 見た目確認は未実施。Editor でボタン押下後に `_SDFMap` 自動設定、`_UseSDFMap=1`、`_SDF_MAP` keyword 有効化を確認する必要あり。

## 2026-03-18 Auto Character Setup 計画メモ

- 現状の Workflow / Quick Setup / Preset / Migration / Validator / 補助生成が分散していて、最新ゲーム寄りの見た目までの導線が長い。
- `handoff/43_auto_character_setup_plan.md` を追加して、`Auto Character Setup` を中心とした段階導入計画を整理。
- 最優先は `目的ベースのオーケストレーション` で、既存ツールの再利用を前提に `Workflow切替 -> ベースルック適用 -> SDF/Ramp/SmoothNormal生成 -> Validator` の順でまとめる方針。
- 実装フェーズは Phase 1 orchestrator、Phase 2 補助生成拡張、Phase 3 role-aware 補正、Phase 4 batch/pipeline 対応。

## 2026-03-18 3段階 Auto Setup System 実装完了 (Phase 1 MVP)

### 新規ファイル (7 ファイル)
すべて `Editor/NataneToon/GUI/` に配置:

| # | ファイル | 責務 |
|---|---------|------|
| 1 | `NataneAutoSetupProfiles.cs` | enum定義(Role/Look/Quality) + 15プロファイルテーブル + SetupRecord |
| 2 | `NataneTextureAnalyzer.cs` | K-means色解析(k=6) + 影色/リム色/アウトライン色/スペキュラ色自動導出 |
| 3 | `NataneMeshAnalyzer.cs` | PCA形状分類 + OutlineWidth + NormalFlattenY + SmoothNormal判定 |
| 4 | `NataneFeaturePresetTable.cs` | 機能×Role最適値テーブル(SSS/Specular/RimLight/Outline/MatCap/Emission/Reflection/Parallax/EnvRim) |
| 5 | `NataneCrossFeatureResolver.cs` | 機能間ルール(SSS↔Spec/Outline↔Flatten/HairSpec↔Spec/MatCap↔Refl) + アーティファクト検出・修正 |
| 6 | `NataneAutoSetupReport.cs` | SetupResult データ + パフォーマンス指標 + サマリUI |
| 7 | `NataneAutoSetupHub.cs` | Stage 1 UI + パイプライン統合 + Stage 2 Role-Aware Toggle ロジック |

### 既存ファイル修正 (`NataneToonShaderGUI.cs`)
1. `ApplySharpAnimeStyle/SoftPainting/ToonPbrHybrid/NearPbr/GameCharacter` → `private` → `internal`
2. `SynchronizeKeywordsAndRefreshInspectorCaches` → `private` → `internal`
3. `DrawQuickSetupSection` に「自動」タブ追加 (quickSetupWizardMode == 3)
4. `ApplyBaseStyleForLook` ヘルパーメソッド追加
5. `DrawToggle` に SetupRecord 存在時の Role-Aware 分岐追加

### パイプライン (Stage 1)
1. Undo.RecordObject → 2. Profile取得 → 3. base style適用 → 4. keyword有効化
→ 5. float/color override → 6. テクスチャ色解析 → 7. メッシュ解析
→ 8. SDF/SmoothNormal生成 → 9. CrossFeatureResolver → 10. ArtifactFix
→ 11. KeywordSync → 12. 性能指標 → 13. Record保存

### Stage 2 (Role-Aware Toggle)
DrawToggle で機能ON時に SetupRecord があれば FeaturePresetTable から Role × Look 最適値を自動適用。
非推奨機能は確認ダイアログ表示。
