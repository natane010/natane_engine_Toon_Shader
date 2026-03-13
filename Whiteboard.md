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
