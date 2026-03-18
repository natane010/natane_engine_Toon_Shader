# Enhanced Auto Character Setup - 改善提案書

Date: 2026-03-18
Base: `handoff/43_auto_character_setup_plan.md` を発展させたもの

## コンセプト

### 元提案との違い

元提案は「少数の意図を選んで土台を整える guided auto setup」。
本提案はそれをさらに進めて：

1. **操作モデルの転換**: 「1個ずつ触るUI」→「目的を伝えて一括実行」
2. **補助データの完全自動生成**: SDF/Ramp/SmoothNormal を条件判定込みで自動実行
3. **Post-Setup 微調整レイヤー**: 自動設定後に「何を変えたか」を可視化し、部分的に上書き可能

### 目指す体験

```
[Before] ユーザーの現状
  マテリアル作成 → テクスチャ割り当て → Quick Setup 選択 →
  SDF 生成 → Ramp 設定 → Outline 調整 → Rim 調整 →
  Specular 調整 → SSS 調整 → Validator 実行 → 手動修正
  → 約 15-30 分 / マテリアル、専門知識必要

[After] 本提案の目標
  マテリアル選択 → Role 選択 → 「セットアップ実行」ボタン
  → 約 10 秒で完了、後から気になる部分だけ微調整
```

---

## アーキテクチャ

### 全体構成

```
┌─────────────────────────────────────────────────┐
│           Auto Setup Hub (UI Layer)              │
│  ┌──────────┐ ┌──────────┐ ┌──────────────────┐ │
│  │  Intent   │ │ Preview  │ │  Result Summary  │ │
│  │  Selector │ │  Panel   │ │  + Fine Tune     │ │
│  └──────┬───┘ └────▲─────┘ └────────▲─────────┘ │
│         │          │                │             │
│  ┌──────▼──────────┴────────────────┴───────────┐│
│  │         Setup Pipeline (Orchestrator)         ││
│  │                                               ││
│  │  1.Analyze → 2.Resolve → 3.Apply → 4.Generate││
│  │  → 5.Validate → 6.Report                     ││
│  └──┬───────┬────────┬────────┬────────┬────────┘│
│     │       │        │        │        │          │
│  ┌──▼──┐┌──▼───┐┌───▼──┐┌───▼───┐┌───▼──┐      │
│  │Quick ││Cate- ││ SDF  ││Smooth ││Vali- │      │
│  │Setup ││gory  ││ Auto ││Normal ││dator │      │
│  │Style ││Preset││ Gen  ││Baker  ││      │      │
│  └──────┘└──────┘└──────┘└───────┘└──────┘      │
│              既存ツール群 (変更なし)               │
└─────────────────────────────────────────────────┘
```

### 新規作成ファイル

| ファイル | 役割 |
|---------|------|
| `Editor/NataneToon/GUI/NataneAutoSetupHub.cs` | UI + オーケストレーター本体 |
| `Editor/NataneToon/GUI/NataneAutoSetupPipeline.cs` | パイプライン実行エンジン |
| `Editor/NataneToon/GUI/NataneAutoSetupProfiles.cs` | Role × Look のプロファイルテーブル |
| `Editor/NataneToon/GUI/NataneAutoSetupReport.cs` | 結果サマリ + 微調整 UI |

既存ファイルは**変更しない**（内部メソッドを public 化する最小変更のみ）。

---

## Phase 1: Intent-Based Setup (最優先)

### 1.1 Intent Selector（意図入力UI）

ユーザーが選ぶのは**3つだけ**：

```
┌─────────────────────────────────────┐
│  🎮 Auto Character Setup            │
│                                     │
│  ① 何を作る？                       │
│  ┌─────┐┌─────┐┌─────┐┌─────┐     │
│  │ 顔  ││ 髪  ││ 服  ││ 目  │     │
│  │ 肌  ││     ││ 布  ││     │     │
│  └──┬──┘└──┬──┘└──┬──┘└──┬──┘     │
│  ┌─────┐┌─────┐┌──────┐            │
│  │金属 ││小物 ││背景  │            │
│  └─────┘└─────┘└──────┘            │
│                                     │
│  ② どんな見た目？                    │
│  ┌────────┐┌────────┐┌──────────┐  │
│  │アニメ風││ゲーム風 ││PBR寄り   │  │
│  │(Sharp  ││(Modern ││(Near PBR)│  │
│  │ Anime) ││ Game)  ││          │  │
│  └────────┘└────────┘└──────────┘  │
│  ┌──────────┐┌───────────┐         │
│  │絵画調    ││ハイブリッド│         │
│  │(Soft     ││(Toon-PBR  │         │
│  │ Painting)││ Hybrid)   │         │
│  └──────────┘└───────────┘         │
│                                     │
│  ③ 品質ターゲット                    │
│  ┌────────┐┌────────┐┌──────────┐  │
│  │モバイル ││標準    ││ハイエンド │  │
│  │(Quest) ││(PC)   ││(PC+)    │  │
│  └────────┘└────────┘└──────────┘  │
│                                     │
│  ┌─────────────────────────────┐    │
│  │   ▶ セットアップ実行         │    │
│  └─────────────────────────────┘    │
│  ┌─────────────────────────────┐    │
│  │   🔄 補助マップだけ再生成    │    │
│  └─────────────────────────────┘    │
└─────────────────────────────────────┘
```

**ポイント**:
- ラジオボタンではなくトグルボタン（視覚的に選びやすい）
- 選択するとプレビューパネルにリアルタイム反映（Phase 2）
- デフォルト値: 顔/肌 + ゲーム風 + 標準（最も一般的な組み合わせ）
- lilToon 移行マテリアルを自動検出して Workflow を内部判定

### 1.2 Setup Pipeline（実行パイプライン）

「セットアップ実行」ボタン押下で以下が**一括実行**される：

```csharp
public class SetupPipeline
{
    public static SetupResult Execute(Material mat, SetupIntent intent)
    {
        var result = new SetupResult();

        Undo.RecordObject(mat, "Auto Character Setup");

        // Step 1: マテリアル解析
        var analysis = AnalyzeMaterial(mat);
        result.analysis = analysis;

        // Step 2: Workflow 自動判定
        var workflow = ResolveWorkflow(mat, analysis);
        result.workflow = workflow;

        // Step 3: ベースルック適用（Role × Look の交差テーブルから）
        var profile = ProfileTable.Get(intent.role, intent.lookTarget);
        ApplyProfile(mat, profile);
        result.appliedProfile = profile;

        // Step 4: Role 固有の補正
        ApplyRoleCorrections(mat, intent.role, intent.quality);

        // Step 5: 補助データ自動生成
        result.generatedAssets = GenerateAuxiliaryData(mat, intent);

        // Step 6: 品質ターゲット適用（不要 feature 削除）
        ApplyQualityBudget(mat, intent.quality);

        // Step 7: キーワード同期
        SynchronizeKeywords(mat);

        // Step 8: 検証
        result.validationIssues = ValidateMaterial(mat);

        // Step 9: 自動修正（安全なもののみ）
        AutoFixSafeIssues(mat, result.validationIssues);

        return result;
    }
}
```

### 1.3 Profile Table（プロファイルテーブル）

Role × LookTarget の交差で**最適なパラメータセット**を決定：

#### 顔/肌 × ゲーム風 (最も需要が高い)
```
基本: ApplyGameCharacterStyle ベース
追加:
  - SSS ON (Intensity=0.6, Power=2.0, Distortion=0.3)
  - SDF 自動生成 & 適用
  - RimLight 控えめ (Intensity=0.8)
  - Specular 控えめ (Intensity=0.5, Softness=0.15)
  - NormalFlattenY=0.3 (顔の法線を球面寄りに)
  - ShadowColor を肌色ベースで自動計算
  - Outline OFF（顔にはアウトラインなしが主流）
```

#### 髪 × ゲーム風
```
基本: ApplyGameCharacterStyle ベース
追加:
  - HairSpecular ON (Shift1=-0.1, Shift2=0.1, Width1=0.15, Width2=0.08)
  - RimLight 強め (Intensity=2.0, Power=2.0)
  - Specular OFF（HairSpecular に置換）
  - Outline ON (Width=0.06, TextureColor=0.9)
  - SmoothNormal 自動生成
  - SSS OFF
```

#### 服/布 × ゲーム風
```
基本: ApplyGameCharacterStyle ベース
追加:
  - NormalMap 有効化（あれば）
  - Specular ON (Size=0.06, Softness=0.1)
  - Sheen ON (Intensity=0.3, Power=3.0) ← 布の質感
  - Outline ON (Width=0.08)
  - SmoothNormal 自動生成
  - SSS OFF
```

#### 目 × ゲーム風
```
基本: 専用プロファイル
  - Parallax ON (Scale=0.02, EyeParallaxDepth=0.15)
  - Emission ON (Glow=0.3, 微かな光)
  - MatCap ON (Intensity=0.4, BlendMode=Add)
  - Specular ON (Size=0.15, Intensity=2.0)
  - Outline OFF
  - SSS OFF
  - RimLight OFF
```

#### 金属 × ゲーム風
```
基本: Toon-PBR Hybrid ベース
  - Reflection ON (Intensity=0.6, Smoothness=0.8, Metallic=0.9)
  - MatCap ON (金属的な MatCap)
  - Specular ON (Size=0.12, Intensity=2.5)
  - Outline ON (Width=0.04, 細め)
  - Glossiness=0.7
  - MatteEffect=0.1
```

**全組み合わせテーブル** (5 Role × 5 Look × 3 Quality = 75 パターン):
- Phase 1 では主要 15 パターン (5 Role × 3 Look[Anime/Game/PBR]) を実装
- 残りは Phase 3 で段階的に追加

### 1.4 補助データ自動生成の判定ロジック

```csharp
static GeneratedAssets GenerateAuxiliaryData(Material mat, SetupIntent intent)
{
    var assets = new GeneratedAssets();

    // SDF: 顔 Role かつ SDF 未設定の場合に自動生成
    if (intent.role == Role.Face && !HasTexture(mat, "_SDFMap"))
    {
        if (NataneToonSdfAutoGenerator.TryGenerateAndAssign(mat, out string msg))
            assets.Add("SDF Map", msg);
    }

    // Ramp: Ramp モード使用時かつ Ramp 未設定
    if (mat.IsKeywordEnabled("_USE_RAMP") && !HasTexture(mat, "_RampTex"))
    {
        var ramp = GenerateDefaultRamp(intent.role, intent.lookTarget);
        mat.SetTexture("_RampTex", ramp);
        assets.Add("Ramp Texture", "自動生成");
    }

    // Smooth Normal: Outline 使用時に自動ベイク
    if (mat.IsKeywordEnabled("_OUTLINE") && !HasSmoothNormals(mat))
    {
        // メッシュが取得可能な場合のみ
        // (選択中の Renderer から取得)
        var mesh = TryGetMeshFromSelection();
        if (mesh != null)
        {
            var smoothed = SmoothNormalBaker.BakeSmoothNormals(mesh, true);
            assets.Add("Smooth Normals", "頂点カラーにベイク済み");
        }
    }

    return assets;
}
```

### 1.5 品質ターゲットによる Feature 制限

```
Mobile (Quest):
  - MatCap: 最大1個
  - テクスチャサンプラー: 最大12
  - Parallax OFF
  - PCSS OFF
  - Refraction OFF
  - Tessellation OFF
  - パフォーマンス目標: A-B 評価

Standard (PC):
  - MatCap: 最大2個
  - テクスチャサンプラー: 最大18
  - Parallax OK (MinSamples=4, MaxSamples=8)
  - PCSS OFF
  - パフォーマンス目標: B-C 評価

High (PC+):
  - 制限なし
  - MatCap: 最大3個
  - テクスチャサンプラー: 制限なし
  - 全機能利用可能
```

---

## Phase 2: Post-Setup Fine Tune（後から微調整）

**これが元提案にない最大の追加要素**。

### 2.1 Result Summary + Override UI

セットアップ完了後に表示される「結果サマリ + 微調整パネル」：

```
┌───────────────────────────────────────────────┐
│  ✅ セットアップ完了                            │
│                                               │
│  適用: 顔/肌 × ゲーム風 × 標準品質             │
│  Workflow: Natane ネイティブ                    │
│                                               │
│  ── 自動設定された項目 ──────────────────────── │
│                                               │
│  ▼ 陰影 (Shading)               [リセット]    │
│    影の段数: ██████░░░░ 2段                    │
│    影の硬さ: ████████░░ 0.03                   │
│    影色:     [■■■■] (自動計算: メイン色の暖色系)│
│                                               │
│  ▼ SSS (肌の透過感)             [リセット]     │
│    強さ:     ██████░░░░ 0.6                    │
│    パワー:   ██░░░░░░░░ 2.0                    │
│                                               │
│  ▼ リムライト                    [リセット]    │
│    強さ:     ████████░░ 0.8                    │
│    広がり:   █████░░░░░ 2.5                    │
│    色:       [■■■■] 白                         │
│                                               │
│  ▼ スペキュラ (ハイライト)       [リセット]     │
│    強さ:     █████░░░░░ 0.5                    │
│    サイズ:   ████░░░░░░ 0.08                   │
│                                               │
│  ▼ アウトライン                  [リセット]    │
│    ☐ OFF (顔には非推奨)                        │
│                                               │
│  ── 自動生成された補助データ ──────────────── │
│    ✅ SDF Map (生成済み: face_sdf.png)         │
│    ⬚ Smooth Normal (メッシュ未選択のためスキップ)│
│                                               │
│  ── 検証結果 ──────────────────────────────── │
│    ✅ サンプラー数: 8/18 (余裕あり)             │
│    ✅ パフォーマンス: A 評価                     │
│    ⚠️ ノーマルマップ未設定 (任意)               │
│                                               │
│  ┌────────────────┐ ┌────────────────────┐    │
│  │ 全体を Undo    │ │ このまま確定する    │    │
│  └────────────────┘ └────────────────────┘    │
└───────────────────────────────────────────────┘
```

### 2.2 微調整の設計思想

**原則: 自動設定は「出発点」、ユーザーは「差分」だけ触る**

1. **セクション単位のリセット**: 「陰影だけ元に戻す」が可能
2. **スライダーで即座に調整**: 自動設定値をベースに微調整
3. **全体 Undo**: 1回の Undo で全自動設定を巻き戻せる
4. **再実行可能**: 意図を変えて再実行しても破綻しない

### 2.3 自動設定値の記録

```csharp
// マテリアルの EditorPrefs に自動設定の記録を保存
// → 後から「何が自動で設定されたか」を常に参照可能
[Serializable]
public class SetupRecord
{
    public string role;
    public string lookTarget;
    public string quality;
    public string timestamp;
    public Dictionary<string, float> appliedFloats;
    public Dictionary<string, Color> appliedColors;
    public List<string> enabledKeywords;
    public List<string> generatedAssets;
}
```

この記録があることで：
- 後から「自動設定された値」と「手動で変えた値」の差分がわかる
- 再実行時に「手動変更を保持したまま再適用」が可能（Phase 3）
- インスペクターで「この値は自動設定 / 手動変更」の表示が可能

---

## Phase 3: Smart Corrections（賢い補正）

### 3.1 シャドウカラー自動計算

メインカラーから影色を自動生成：

```csharp
static Color CalculateShadowColor(Color mainColor, Role role)
{
    Color.RGBToHSV(mainColor, out float h, out float s, out float v);

    switch (role)
    {
        case Role.Face:
            // 肌: 暖色方向にシフト + 彩度アップ + 明度ダウン
            h += 0.02f; // 赤寄り
            s *= 1.2f;
            v *= 0.65f;
            break;
        case Role.Hair:
            // 髪: 色相維持 + 彩度アップ + 明度ダウン
            s *= 1.3f;
            v *= 0.55f;
            break;
        case Role.Clothing:
            // 服: 青紫方向にシフト + 明度ダウン
            h -= 0.03f; // 青寄り
            s *= 1.1f;
            v *= 0.60f;
            break;
        default:
            v *= 0.6f;
            break;
    }

    return Color.HSVToRGB(Mathf.Repeat(h, 1f),
                           Mathf.Clamp01(s),
                           Mathf.Clamp01(v));
}
```

### 3.2 Ramp テクスチャ自動生成

Role × Look に応じた Ramp を自動生成：

```
顔 × ゲーム風:  [明→暗] 2段, 境界やや柔らかめ, 暖色シフト
髪 × ゲーム風:  [明→暗] 2段, 境界シャープ, 彩度高め
服 × ゲーム風:  [明→暗] 2-3段, 中間色あり
目:             Ramp 不使用（Parallax + Emission で表現）
金属:           [明→暗] グラデーション（PBR 寄り）

顔 × アニメ風:  [明→暗] 2段, 完全シャープ境界
髪 × アニメ風:  [明→暗] 2段, 完全シャープ境界
```

### 3.3 メインテクスチャからの色解析

```csharp
// メインテクスチャの平均色を解析して各種色を自動設定
static ColorAnalysis AnalyzeMainTexture(Texture2D mainTex)
{
    // テクスチャを縮小してサンプリング（高速化）
    var downscaled = DownscaleTexture(mainTex, 64, 64);
    var pixels = downscaled.GetPixels();

    // 平均色・最頻色・明暗範囲を計算
    var avgColor = AverageColor(pixels);
    var dominantColor = DominantColor(pixels);  // K-means 簡易版
    var brightest = BrightestRegion(pixels);
    var darkest = DarkestRegion(pixels);

    return new ColorAnalysis
    {
        averageColor = avgColor,
        dominantColor = dominantColor,
        suggestedShadowColor = CalculateShadowColor(dominantColor, role),
        suggestedRimColor = Color.Lerp(Color.white, brightest, 0.3f),
        suggestedOutlineColor = Color.Lerp(Color.black, darkest, 0.5f),
        suggestedSpecularColor = Color.Lerp(Color.white, brightest, 0.2f),
    };
}
```

---

## Phase 4: Batch & Avatar 対応

### 4.1 Avatar 一括セットアップ

```
┌──────────────────────────────────────┐
│  🎭 Avatar Auto Setup               │
│                                      │
│  選択中: MyAvatar (Prefab)           │
│                                      │
│  検出されたマテリアル:                │
│  ┌──────────────────────────────┐    │
│  │ ☑ Face_Mat    → 顔/肌        │    │
│  │ ☑ Hair_Mat    → 髪           │    │
│  │ ☑ Body_Mat    → 服/布        │    │
│  │ ☑ Eye_Mat     → 目           │    │
│  │ ☑ Metal_Mat   → 金属         │    │
│  └──────────────────────────────┘    │
│                                      │
│  ルック: [ゲーム風 ▼]  品質: [標準 ▼] │
│                                      │
│  ┌────────────────────────────┐      │
│  │  ▶ 全マテリアルをセットアップ │      │
│  └────────────────────────────┘      │
└──────────────────────────────────────┘
```

**Role 自動推定ロジック**:
- マテリアル名に "face", "skin", "body" → 顔/肌
- マテリアル名に "hair" → 髪
- マテリアル名に "eye" → 目
- マテリアル名に "cloth", "dress", "shirt" → 服/布
- マテリアル名に "metal", "armor", "weapon" → 金属
- 上記に該当しない場合 → ユーザーに選択させる（ドロップダウン）

---

## 既存コードとの統合方針

### 変更が必要な既存ファイル（最小変更）

| ファイル | 変更内容 |
|---------|---------|
| `NataneToonShaderGUI.cs` | Style 適用メソッド 5 つを `internal` に変更 |
| `NataneToonShaderGUI.cs` | `ApplyCategoryPreset` を `internal static` に変更 |
| `NataneToonSdfAutoGenerator.cs` | 変更不要（既に public static） |
| `SmoothNormalBaker.cs` | 変更不要（既に public static） |
| `MaterialValidator.cs` | 変更不要（既に public static） |

### NataneToonShaderGUI.cs への Hub 統合

Inspector 上部に Auto Setup Hub のエントリーポイントを追加：

```csharp
// NataneToonShaderGUI.cs の OnInspectorGUI 先頭付近
if (GUILayout.Button("🎮 Auto Character Setup", EditorStyles.miniButton))
{
    NataneAutoSetupHub.ShowWindow(targetMaterial);
}
```

---

## フェーズ別の実装スコープ

### Phase 1: MVP（まず動くもの）
- [ ] `NataneAutoSetupHub.cs` - Intent Selector UI
- [ ] `NataneAutoSetupPipeline.cs` - パイプライン実行
- [ ] `NataneAutoSetupProfiles.cs` - 主要 15 プロファイル (5 Role × 3 Look)
- [ ] `NataneAutoSetupReport.cs` - 結果サマリ表示
- [ ] SDF / Ramp / SmoothNormal の自動判定 & 生成統合
- [ ] 品質ターゲットによる Feature 制限
- [ ] 全体 Undo 対応
- [ ] `NataneToonShaderGUI.cs` への統合ボタン

### Phase 2: 微調整 & プレビュー
- [ ] Post-Setup Override UI（セクション別スライダー + リセット）
- [ ] SetupRecord の保存 & 参照
- [ ] 自動設定 vs 手動変更の可視化
- [ ] シャドウカラー自動計算（メインテクスチャ解析）
- [ ] プレビューパネル（Before / After）

### Phase 3: 拡張プロファイル & 賢い補正
- [ ] 全 75 パターンのプロファイル完成
- [ ] Ramp テクスチャ自動生成
- [ ] メインテクスチャ色解析による色設定自動化
- [ ] 「手動変更を保持して再適用」機能
- [ ] lilToon 移行後の Natane 向け最適化パス

### Phase 4: Batch & Avatar
- [ ] Avatar / Prefab 一括セットアップ
- [ ] Role 自動推定（名前ベース）
- [ ] 複数マテリアル同時処理
- [ ] 変換レポート保存

---

## 元提案からの改善点まとめ

| 観点 | 元提案 (43) | 本提案 (44) |
|------|------------|------------|
| **入力** | Role + Look + Quality + Workflow の 4 選択 | Role + Look + Quality の 3 選択（Workflow は自動判定） |
| **出力** | 設定適用 + サマリ | 設定適用 + サマリ + **微調整 UI** |
| **補助生成** | SDF / Ramp / SmoothNormal を「条件で提示または自動実行」 | **条件判定込みで完全自動実行**（生成結果をサマリに表示） |
| **色設定** | Quick Setup ベースの固定色 | **メインテクスチャから自動計算**（Phase 2） |
| **後から調整** | 通常のインスペクター操作 | **専用の Override UI**（何が自動設定されたか可視化） |
| **Batch** | Phase 4 で複数マテリアル | Phase 4 で **Avatar 単位 + Role 自動推定** |
| **既存コード** | オーケストレーション（変更なし方針） | 同方針 + **最小の public 化変更のみ** |

---

## 成功基準

### Phase 1 完了時
- [ ] 新規 Natane マテリアルで「3 クリック → 破綻しない見た目」が成立
- [ ] lilToon 移行マテリアルでも同様に動作
- [ ] Undo が 1 回で全巻き戻し
- [ ] 再実行で致命的に崩れない
- [ ] Face / Hair / Clothing / Eyes / Metal の 5 Role で差が出る

### 全 Phase 完了時
- [ ] 「テクスチャを割り当てて Role を選ぶだけ」で 80% の見た目が完成
- [ ] 残り 20% は微調整 UI で直感的に調整可能
- [ ] Avatar 一体を 1 分以内にセットアップ完了
- [ ] 専門知識なしでも「最新 3D ゲーム風」の品質に到達可能
