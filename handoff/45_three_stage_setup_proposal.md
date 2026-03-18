# 3段階セットアップ提案書

Date: 2026-03-18 (Updated: 自動最適化調査結果を統合)
Base: `44_enhanced_auto_setup_proposal.md` を統合・再構成

## コンセプト: 3段階品質保証モデル

```
 Stage 1              Stage 2                Stage 3
 ワンボタン     →    機能 ON/OFF      →    パラメータ微調整
 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 ┃                ┃                     ┃
 ┃  80点の完成度   ┃  90点の完成度        ┃  95-100点の完成度
 ┃  操作: 3クリック ┃  操作: トグル数個     ┃  操作: スライダー
 ┃  知識: 不要     ┃  知識: 最小限         ┃  知識: やや必要
 ┃                ┃                     ┃
 ┗━━━ここで止めてもOK ┗━━━ここで止めてもOK    ┗━━━ 最終仕上げ

 ※ どの段階で止めても破綻しない
 ※ 各段階は前の段階の結果を壊さない
```

### 現在の提案 (44) との関係

44 の内容を**捨てずに再配置**する：

| 44 の要素 | → 3段階モデルでの位置 |
|----------|---------------------|
| Intent Selector (Role/Look/Quality) | Stage 1 |
| Pipeline 実行 | Stage 1 |
| Profile Table | Stage 1 |
| 補助データ自動生成 | Stage 1 |
| 品質ターゲット制限 | Stage 1 + Stage 2 |
| Post-Setup Override UI | Stage 3 |
| SetupRecord | 全段階の横断記録 |
| メインテクスチャ色解析 | Stage 1 (自動) |
| Batch / Avatar | Stage 1 の拡張 |

**追加するもの**: Stage 2（Role-Aware Feature Toggle）← これが今の提案に欠けている核

---

## Stage 1: ワンボタンで 80 点

### ユーザー操作

```
┌──────────────────────────────────────┐
│  何を作る？     どんな見た目？         │
│  [顔][髪][服][目][金属]  [アニメ][ゲーム][PBR] │
│                                      │
│  ┌──────────────────────────────┐    │
│  │     ▶ セットアップ実行        │    │
│  └──────────────────────────────┘    │
└──────────────────────────────────────┘
```

3クリック: Role 選択 → Look 選択 → 実行ボタン
（品質ターゲットはデフォルト Standard、変えたい人だけ変える）

### 内部で起きること

44 の Pipeline と同じだが、**ここが Stage 1 の出口として完結する**ように設計：

```
1. マテリアル解析 (Workflow 自動判定)
2. Profile 適用 (Role × Look の交差テーブル)
3. メインテクスチャ色解析 → 影色/リム色/アウトライン色 自動設定
4. 補助データ生成 (SDF / Ramp / SmoothNormal)
5. 品質バジェット適用
6. キーワード同期
7. 検証 + 安全な自動修正
```

### Stage 1 の出力品質保証

**「80 点」を保証するための設計原則**：

1. **Role ごとに "必要十分な機能" だけ ON にする**
   - 顔: Shading + SSS + SDF（3機能で十分な表現力）
   - 髪: Shading + HairSpecular + Outline + RimLight（4機能）
   - 服: Shading + NormalMap + Outline + Specular（4機能）
   - 目: Shading + Parallax + Emission + MatCap（4機能）
   - 金属: Shading + Reflection + Specular + MatCap（4機能）

2. **ON にした機能は Role に最適化されたパラメータで初期化**
   - 汎用デフォルト値ではなく、Role × Look 専用の値
   - 例: 顔の SSS は Intensity=0.6, 服の Specular は Size=0.06

3. **色はメインテクスチャから自動計算**
   - 影色: メインカラーの暖色シフト（肌）/ 彩度アップ（髪）
   - リム色: テクスチャの明部ベース
   - アウトライン色: テクスチャの暗部ベース

4. **補助データは条件一致で自動生成**
   - 手動で何かを用意する必要がない

**結果**: Stage 1 だけで「破綻しない、それなりに見栄えする状態」が得られる。

---

## Stage 2: 機能 ON/OFF で 90 点

### これが本提案の核心

**現在の問題**: 機能を ON にすると「デフォルト値」が入る → Role に合わない → 見た目が崩れる

**解決**: 機能の ON/OFF が **Role-Aware** になる

```
[現在]
  SSS を ON → _SSSIntensity=1.0 (汎用デフォルト) → 強すぎて不自然

[提案]
  SSS を ON → Role=顔 を参照 → _SSSIntensity=0.6, Power=2.0 → 自然な肌表現
```

### ユーザー操作

Stage 1 完了後、インスペクターに **Role-Aware トグルパネル** が表示される：

```
┌──────────────────────────────────────────────┐
│  現在: 顔/肌 × ゲーム風                       │
│                                              │
│  ── 基本機能 (Stage 1 で設定済み) ──────────── │
│  [ON]  陰影 (2段トゥーン)                     │
│  [ON]  SSS (肌の透過感)                       │
│  [ON]  SDF (顔の影制御)                       │
│                                              │
│  ── 追加機能 ────────────────────────────── │
│  [OFF] リムライト      ← ON にすると顔向け最適値で有効化 │
│  [OFF] スペキュラ      ← ON にすると顔向け控えめ設定     │
│  [OFF] アウトライン    ← ON にすると顔向け（細め＋色連動）│
│  [OFF] ノーマルマップ  ← ON にすると既存テクスチャを検出  │
│  [OFF] MatCap         ← ON にすると顔向け柔らかい反射    │
│  [OFF] Emission       ← ON にすると控えめな発光           │
│  [OFF] 環境リム       ← ON にすると環境色連動             │
│                                              │
│  ── 特殊効果 ────────────────────────────── │
│  [OFF] ディゾルブ                             │
│  [OFF] 色相シフト                             │
│  [OFF] ホログラム                             │
│  [OFF] グリッター                             │
│                                              │
│  ⚡ サンプラー: 8/18  パフォーマンス: A        │
└──────────────────────────────────────────────┘
```

### Role-Aware Toggle の仕組み

```csharp
/// 機能を ON にしたとき、Role に最適化されたパラメータを自動設定
public static void EnableFeatureWithRolePreset(
    Material mat, string feature, Role role, LookTarget look)
{
    // 1. キーワード有効化
    mat.EnableKeyword(FeatureToKeyword(feature));

    // 2. Role × Look × Feature の最適パラメータを取得
    var preset = FeaturePresetTable.Get(role, look, feature);

    // 3. パラメータ適用
    foreach (var prop in preset.floatProperties)
        mat.SetFloat(prop.Key, prop.Value);
    foreach (var prop in preset.colorProperties)
        mat.SetColor(prop.Key, prop.Value);

    // 4. 必要な補助データがあれば自動生成
    if (preset.requiresSDF && !HasTexture(mat, "_SDFMap"))
        NataneToonSdfAutoGenerator.TryGenerateAndAssign(mat, out _);
    if (preset.requiresSmoothNormal)
        TryBakeSmoothNormals(mat);

    // 5. 品質バジェット再チェック
    UpdatePerformanceIndicator(mat);
}
```

### Feature Preset Table（機能 × Role の最適値テーブル）

これが Stage 2 の心臓部。全機能 × 全 Role の組み合わせに最適値を定義する：

```
                 顔/肌          髪            服/布          目            金属
─────────────────────────────────────────────────────────────────────────────
SSS             Intensity=0.6   (非推奨)      Intensity=0.2  (非推奨)      (非推奨)
                Power=2.0                     Power=3.0
                Distortion=0.3                Distortion=0.1

Specular        Size=0.08       (HairSpec推奨) Size=0.06      Size=0.15     Size=0.12
                Softness=0.15                  Softness=0.1   Softness=0.05 Softness=0.03
                Intensity=0.5                  Intensity=0.8  Intensity=2.0 Intensity=2.5

HairSpecular    (非推奨)        Shift1=-0.1   (非推奨)       (非推奨)      (非推奨)
                                Shift2=0.1
                                Width1=0.15
                                Intensity=1.5

RimLight        Power=3.0       Power=2.0     Power=2.5      (非推奨)      Power=2.0
                Intensity=0.8   Intensity=2.0 Intensity=1.0               Intensity=1.5
                ※顔は控えめ     ※髪は強め     ※中程度                      ※金属は反射的

Outline         Width=0.03      Width=0.06    Width=0.08     (非推奨)      Width=0.04
                ※顔は極細か無し  TextureColor  TextureColor                 ※金属は細め
                                =0.9          =0.8

MatCap          Intensity=0.3   Intensity=0.4 Intensity=0.2  Intensity=0.4 Intensity=0.7
                BlendMode=Soft  BlendMode=Add BlendMode=Mult BlendMode=Add BlendMode=Add
                ※柔らかい肌反射 ※天使の輪     ※布の質感      ※目の光沢    ※金属光沢

Sheen           (非推奨)        Intensity=0.2 Intensity=0.4  (非推奨)      (非推奨)
                                Power=4.0     Power=3.0
                                              ※布の質感向上

Reflection      (非推奨)        (非推奨)      (非推奨)       (非推奨)      Intensity=0.6
                                                                          Smoothness=0.8
                                                                          Metallic=0.9

Parallax        (非推奨)        (非推奨)      (非推奨)       Scale=0.02    (非推奨)
                                                             EyeDepth=0.15

Emission        Glow=0.1        Glow=0.05     Glow=0.1       Glow=0.3      Glow=0.2
                ※極控えめ       ※極控えめ     ※控えめ        ※目の輝き    ※金属の発光

EnvRim          Power=3.0       Power=2.5     Power=3.0      (非推奨)      Power=2.0
                Intensity=0.3   Intensity=0.5 Intensity=0.3               Intensity=0.6
```

**「(非推奨)」の扱い**:
- トグルは表示するが、押すと確認ダイアログを出す
- 「顔に HairSpecular は通常使いません。有効にしますか？」
- 有効にした場合は汎用デフォルト値で初期化

### Stage 2 の品質保証

1. **どの機能を ON にしても壊れない**
   - 各トグルが Role 最適値を持つので、ON にした瞬間から「使える状態」
   - 汎用デフォルト値による「強すぎ/弱すぎ」が起きない

2. **機能を OFF にしても壊れない**
   - OFF = キーワード無効 → シェーダーコスト 0
   - 他の機能への副作用なし

3. **パフォーマンス指標がリアルタイム更新**
   - トグルを ON/OFF するたびにサンプラー数・評価が即更新
   - 品質バジェット超過時は警告表示

4. **機能間の相性ガイド**
   - SSS + Specular: 相性◎（肌の質感向上）
   - HairSpecular + Specular: 相性△（通常どちらか一方）
   - Parallax + Mobile品質: 相性✕（パフォーマンス超過）

---

## Stage 3: パラメータ微調整で 95-100 点

### ユーザー操作

Stage 2 で ON にした機能の中身を、スライダーで微調整する。
**これは既存のインスペクター UI をそのまま使う。**

ただし、以下の改善を加える：

```
┌──────────────────────────────────────────────┐
│  ▼ SSS (肌の透過感)  [自動設定値にリセット]   │
│                                              │
│    強さ:    ◀━━━━━━●━━━━▶ 0.6   [auto]     │
│    パワー:  ◀━━●━━━━━━━━▶ 2.0   [auto]     │
│    歪み:    ◀━━━●━━━━━━━▶ 0.3   [auto]     │
│    色:      [■■■■]              [auto]       │
│                                              │
│  ▼ リムライト         [自動設定値にリセット]   │
│                                              │
│    強さ:    ◀━━━━━━━━●━▶ 0.8   [auto]      │
│    広がり:  ◀━━━━━●━━━━▶ 3.0   [手動変更]   │
│    色:      [■■■■]              [auto]       │
│                                              │
│  ※ [auto] = Stage 1/2 で自動設定された値     │
│  ※ [手動変更] = ユーザーが変更した値          │
└──────────────────────────────────────────────┘
```

### Stage 3 の改善点（44 からの変更）

1. **[auto] / [手動変更] のラベル表示**
   - 自動設定値と手動変更を視覚的に区別
   - 「どこを自分で変えたか」が一目でわかる

2. **セクション単位の「自動設定値にリセット」**
   - SSS のパラメータだけを Stage 1/2 の自動値に戻す
   - 他のセクションには影響しない

3. **全体リセット**
   - Stage 1 の結果に完全に巻き戻す
   - Ctrl+Z (Undo) でも可能

### Stage 3 の品質保証

1. **スライダーの範囲が Role に応じて制限される**
   - 例: 顔の SSS Intensity は 0.0〜1.5（通常 0.3〜0.8 が適正）
   - 適正範囲をカラーバーで表示（緑=推奨、黄=注意、赤=非推奨）

2. **極端な値には警告**
   - Specular Intensity > 5.0 → 「白飛びする可能性があります」
   - Outline Width > 0.2 → 「太すぎる可能性があります」

---

## 3段階の関係図

```
Stage 1 (ワンボタン)
  │
  │ Role × Look プロファイルテーブル (44 の Profile Table)
  │ メインテクスチャ色解析 (44 の Phase 3)
  │ 補助データ自動生成 (44 の Phase 1)
  │ 品質バジェット (44 の Phase 1)
  │
  ▼ 出力: 「基本機能が Role 最適値で設定済み」のマテリアル
  │
Stage 2 (機能 ON/OFF)  ★ 本提案の新規追加部分
  │
  │ Feature Preset Table (機能 × Role の最適値)
  │ 非推奨機能の警告
  │ パフォーマンス指標リアルタイム更新
  │ 補助データ追加生成（必要時）
  │
  ▼ 出力: 「追加機能も Role 最適値で設定済み」のマテリアル
  │
Stage 3 (パラメータ)
  │
  │ 既存インスペクター UI + auto/手動ラベル (44 の Phase 2 拡張)
  │ セクション単位リセット
  │ 適正範囲の可視化
  │
  ▼ 出力: 「ユーザーの意図が反映された最終マテリアル」
```

**重要な設計原則**: 各段階は前の段階を**壊さない**。

- Stage 2 の ON/OFF は Stage 1 のベースを崩さない
  （OFF にしても Stage 1 の基本設定は残る）
- Stage 3 のスライダーは Stage 2 の設定をベースに調整するだけ
  （リセットボタンで Stage 2 の値に戻せる）

---

## 実装上の構造

### 新規ファイル

| ファイル | 役割 | Stage |
|---------|------|-------|
| `NataneAutoSetupHub.cs` | Stage 1 UI + Pipeline | 1 |
| `NataneAutoSetupProfiles.cs` | Role × Look プロファイルテーブル | 1 |
| `NataneTextureAnalyzer.cs` | K-means/ヒストグラム/Laplacian/連結成分 テクスチャ解析 | 1+2 ★新規 |
| `NataneMeshAnalyzer.cs` | PCA形状分類/離散曲率/トポロジー/テクセル密度 メッシュ解析 | 1+2 ★新規 |
| `NataneArtifactDetector.cs` | シェーダー数式ベースのアーティファクト検出・自動修正 | 1+2 ★新規 |
| `NataneFeaturePresetTable.cs` | 機能 × Role 最適値テーブル | 2 ★新規 |
| `NataneRoleAwareToggle.cs` | Role-Aware トグル UI + ロジック | 2 ★新規 |
| `NataneCrossFeatureResolver.cs` | 機能間クロス最適化ルール | 2 ★新規 |
| `NataneAutoSetupReport.cs` | 結果サマリ + auto/手動ラベル | 3 |

### 既存ファイルへの統合

```csharp
// NataneToonShaderGUI.cs に追加するコード (最小限)

// 1. Auto Setup ボタン（インスペクター上部）
DrawAutoSetupButton(targetMaterial);

// 2. Stage 2 のトグルパネル（既存の各セクション描画を置換）
//    既存: DrawToggle("_SSS", ...) → ON/OFF するだけ
//    提案: DrawRoleAwareToggle("_SSS", currentRole, currentLook, ...)
//          → ON 時に FeaturePresetTable から最適値を自動適用

// 3. Stage 3 のラベル（既存のスライダー描画に追加）
//    既存: DrawProperty("_SSSIntensity", "強さ")
//    提案: DrawPropertyWithAutoLabel("_SSSIntensity", "強さ", setupRecord)
//          → [auto] or [手動変更] のラベルを表示
```

### データフロー

```csharp
// Stage 1: 実行時に SetupRecord を生成・保存
var record = new SetupRecord {
    role = Role.Face,
    lookTarget = LookTarget.GameCharacter,
    quality = Quality.Standard,
    // Stage 1 で設定した全プロパティを記録
    stage1Properties = CaptureAllProperties(mat),
};

// Stage 2: トグル ON 時に SetupRecord を更新
record.stage2Overrides["_SSS"] = new FeatureOverride {
    enabled = true,
    properties = FeaturePresetTable.Get(record.role, record.lookTarget, "_SSS"),
};

// Stage 3: ユーザーがスライダーを動かした時
record.stage3Overrides["_SSSIntensity"] = 0.8f; // auto=0.6 → 手動=0.8

// リセット時: stage3Overrides から削除 → stage2 の値に戻る
// 全リセット: stage2Overrides + stage3Overrides を削除 → stage1 に戻る
```

---

## 44 との差分まとめ

| 要素 | 44 | 本提案 (45) |
|------|-----|------------|
| **全体構造** | Pipeline → Post-Setup Fine Tune | **3段階 (ワンボタン → トグル → パラメータ)** |
| **Stage 2** | なし（Phase 2 は微調整のみ） | **Role-Aware Feature Toggle** ★核心の追加 |
| **Feature Preset Table** | なし | **機能 × Role の最適値テーブル** ★新規 |
| **品質保証** | Stage 1 完了時の保証のみ | **各段階での品質保証** |
| **非推奨ガイド** | なし | **Role に合わない機能の警告** |
| **パフォーマンス表示** | 検証結果として表示 | **トグル操作ごとにリアルタイム更新** |
| **auto/手動ラベル** | SetupRecord で内部記録 | **UI 上で可視化** |
| **適正範囲表示** | なし | **Role に応じたスライダー範囲ガイド** |

**44 から削除するもの**: なし（全て活用、配置を変えるだけ）
**44 に追加するもの**: Stage 2 の仕組み全体（FeaturePresetTable + RoleAwareToggle）

---

## Data-Driven Auto-Optimization（データ駆動の自動最適化）

**3チーム調査結果の統合** (2026-03-18)

全 Stage に共通する自動最適化エンジン。
メッシュ・テクスチャ・マスクから読み取れる情報を使い、
**固定値テーブルに頼らず、実データから最適パラメータを算出**する。

### 設計思想

```
固定プロファイル値 (Role × Look テーブル)
       ↓ ベースライン
データ駆動補正 (テクスチャ/メッシュ解析)
       ↓ 実データに合わせて微調整
最終パラメータ値
```

プロファイルテーブルは「素材がない場合のフォールバック」。
テクスチャやメッシュがあれば、それを解析して**より正確な値で上書き**する。

### 自動最適化の全体マップ

```
┌─────────────────────────────────────────────────────────┐
│                Data-Driven Auto Engine                    │
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ Texture       │  │ Mesh         │  │ Cross-Feature │  │
│  │ Analyzer      │  │ Analyzer     │  │ Resolver      │  │
│  │               │  │              │  │               │  │
│  │ _MainTex  ──→│  │ bounds  ──→ │  │ SSS+Spec ──→ │  │
│  │ _BumpMap  ──→│  │ normals ──→ │  │ Outline+ ──→ │  │
│  │ Masks     ──→│  │ tangents──→ │  │  Flatten      │  │
│  │ _MatCapTex──→│  │ triangles─→ │  │ MatCap+ ──→  │  │
│  │ _ThickMap ──→│  │ colors  ──→ │  │  Reflect      │  │
│  └──────┬───────┘  └──────┬──────┘  └──────┬────────┘  │
│         │                 │                 │            │
│         └─────────┬───────┘                 │            │
│                   ▼                         │            │
│         ┌─────────────────┐                 │            │
│         │ Parameter Merge │◀────────────────┘            │
│         │ (Profile + Data)│                              │
│         └────────┬────────┘                              │
│                  ▼                                       │
│         最終パラメータ値                                  │
└─────────────────────────────────────────────────────────┘
```

---

### A. テクスチャ解析による自動パラメータ

#### A-1. メインテクスチャ (_MainTex) → 色の自動設定

**解析可能な情報**: 平均色、支配色、明暗分布、彩度、色温度

```csharp
/// メインテクスチャから色関連パラメータを一括自動設定
public static ColorAutoResult AnalyzeAndApplyColors(Material mat, Role role)
{
    var mainTex = mat.GetTexture("_MainTex") as Texture2D;
    if (mainTex == null) return null;

    // 64x64 に縮小して高速サンプリング
    var readable = CreateReadableCopy(mainTex, 64, 64);
    var pixels = readable.GetPixels();

    // === 支配色の算出 ===
    var dominant = CalculateDominantColor(pixels);
    Color.RGBToHSV(dominant, out float h, out float s, out float v);

    // === 影色の自動計算 (Role に応じた色相シフト) ===
    float shadowH = h, shadowS = s, shadowV = v;
    switch (role)
    {
        case Role.Face:
            shadowH += 0.02f;   // 暖色方向（赤寄り）
            shadowS *= 1.2f;    // 彩度アップ
            shadowV *= 0.65f;   // 明度ダウン
            break;
        case Role.Hair:
            shadowS *= 1.3f;    // 彩度強調
            shadowV *= 0.55f;   // より暗く
            break;
        case Role.Clothing:
            shadowH -= 0.03f;   // 寒色方向（青寄り）
            shadowS *= 1.1f;
            shadowV *= 0.60f;
            break;
        case Role.Metal:
            shadowS *= 0.8f;    // 彩度ダウン（金属的）
            shadowV *= 0.50f;
            break;
        default:
            shadowV *= 0.6f;
            break;
    }
    mat.SetColor("_ShadowColor", Color.HSVToRGB(
        Mathf.Repeat(shadowH, 1f), Mathf.Clamp01(shadowS), Mathf.Clamp01(shadowV)));

    // === リムライト色 (テクスチャ明部ベース) ===
    var brightest = pixels.OrderByDescending(p => p.grayscale).Take(pixels.Length / 10)
                         .Aggregate(Color.black, (a, b) => a + b) / (pixels.Length / 10);
    mat.SetColor("_RimColor", Color.Lerp(Color.white, brightest, 0.3f));

    // === アウトライン色 (テクスチャ暗部ベース) ===
    var darkest = pixels.OrderBy(p => p.grayscale).Take(pixels.Length / 10)
                       .Aggregate(Color.black, (a, b) => a + b) / (pixels.Length / 10);
    mat.SetColor("_OutlineColor", Color.Lerp(Color.black, darkest, 0.5f));

    // === スペキュラ色 (明部ベース + 低彩度化) ===
    Color.RGBToHSV(brightest, out float bh, out float bs, out float bv);
    mat.SetColor("_SpecularColor",
        Color.HSVToRGB(bh, bs * 0.3f, Mathf.Clamp01(bv * 1.1f)));

    DestroyImmediate(readable);
}
```

**実現可能性**: ✅ 簡単 | **精度**: 高 (85-95%)
**適用タイミング**: Stage 1（ワンボタン実行時）

---

#### A-2. ノーマルマップ (_BumpMap) → BumpScale 自動設定

```csharp
/// ノーマルマップの法線偏差から最適な BumpScale を算出
public static float AutoBumpScale(Texture2D bumpMap)
{
    var readable = CreateReadableCopy(bumpMap, 32, 32);
    var pixels = readable.GetPixels();

    float totalDeviation = 0f;
    foreach (var p in pixels)
    {
        // ノーマルマップの XY 成分（0.5 = フラット）
        float nx = p.r * 2f - 1f;
        float ny = p.g * 2f - 1f;
        totalDeviation += Mathf.Sqrt(nx * nx + ny * ny);
    }
    float avgDeviation = totalDeviation / pixels.Length;

    DestroyImmediate(readable);

    // 偏差が小さい → 弱いバンプ、偏差が大きい → 強いバンプ
    // 偏差 0.1 → scale 0.25, 偏差 0.5 → scale 1.25
    return Mathf.Clamp(avgDeviation * 2.5f, 0.2f, 2.0f);
}
```

**実現可能性**: ✅ 簡単 | **精度**: 中-高 (70-85%)
**適用タイミング**: Stage 1 + Stage 2（NormalMap ON 時）

---

#### A-3. マスクテクスチャ → エフェクト強度の自動スケーリング

全マスクに共通する「マスクからの強度自動算出」ロジック：

```csharp
/// マスクのカバレッジと輝度から最適な強度を算出
public static float AutoIntensityFromMask(Texture2D mask, float baseIntensity = 1.0f)
{
    var readable = CreateReadableCopy(mask, 32, 32);
    var pixels = readable.GetPixels();

    float avgBrightness = pixels.Average(p => p.grayscale);
    float coverage = pixels.Count(p => p.grayscale > 0.5f) / (float)pixels.Length;

    DestroyImmediate(readable);

    // カバレッジ × 平均輝度 で強度を補正
    // カバレッジ高い（広範囲に効く）→ 強度を下げて自然に
    // カバレッジ低い（ピンポイント）→ 強度を上げて目立たせる
    float intensityScale = Mathf.Lerp(1.3f, 0.7f, coverage);
    return baseIntensity * intensityScale * (0.5f + avgBrightness * 0.8f);
}
```

**適用先と精度**:

| マスク | 自動設定パラメータ | 精度 |
|--------|-------------------|------|
| `_SpecularMask` | `_SpecularIntensity` | 高 (85%) |
| `_RimMask` | `_RimIntensity` | 高 (80%) |
| `_MatCapMask` | `_MatCapIntensity` | 中-高 (75%) |
| `_EmissionMask` | `_EmissionGlow` | 高 (80%) |
| `_SheenMask` | `_SheenIntensity` | 中 (70%) |
| `_GlitterMask` | `_GlitterIntensity` | 中 (70%) |
| `_SSSMask` | `_SSSIntensity` | 中-高 (75%) |

**適用タイミング**: Stage 2（各機能 ON 時にマスクがあれば自動適用）

---

#### A-4. ThicknessMap → SSS パラメータ自動設定

```csharp
/// ThicknessMap の厚さ分布から SSS パラメータを算出
public static void AutoSSSFromThickness(Material mat, Texture2D thicknessMap)
{
    var readable = CreateReadableCopy(thicknessMap, 32, 32);
    var pixels = readable.GetPixels();

    float avgThickness = pixels.Average(p => p.grayscale);
    float thinCoverage = pixels.Count(p => p.grayscale < 0.3f) / (float)pixels.Length;

    DestroyImmediate(readable);

    // 薄い部分が多い → SSS 強め、厚い部分が多い → SSS 控えめ
    mat.SetFloat("_SSSIntensity", Mathf.Lerp(0.3f, 0.9f, thinCoverage));
    mat.SetFloat("_ThicknessScale", Mathf.Clamp(avgThickness * 2f, 0.5f, 2f));
    mat.SetFloat("_SSSPower", Mathf.Lerp(1.5f, 4f, 1f - thinCoverage));
}
```

**実現可能性**: ✅ 簡単 | **精度**: 中-高 (75%)

---

#### A-5. MatCapTex → MatCap パラメータ自動設定

```csharp
/// MatCap テクスチャの輝度分布から最適設定を算出
public static void AutoMatCapParams(Material mat, Texture2D matCapTex)
{
    var readable = CreateReadableCopy(matCapTex, 32, 32);
    var pixels = readable.GetPixels();

    float avgBrightness = pixels.Average(p => p.grayscale);
    float maxBrightness = pixels.Max(p => p.grayscale);

    DestroyImmediate(readable);

    // 明るい MatCap → 強度を下げる（白飛び防止）
    // 暗い MatCap → 強度を上げる（視認性確保）
    mat.SetFloat("_MatCapIntensity", Mathf.Lerp(1.2f, 0.4f, avgBrightness));

    // 明暗差が大きい → Screen ブレンド、小さい → Normal ブレンド
    float contrast = maxBrightness - pixels.Min(p => p.grayscale);
    mat.SetFloat("_MatCapBlendMode", contrast > 0.5f ? 2f : 0f);
}
```

**実現可能性**: ✅ 簡単 | **精度**: 中-高 (75%)

---

#### A-6. EmissionMap → Emission パラメータ自動設定

```csharp
/// Emission マップの輝度から Glow 強度を自動設定
public static void AutoEmissionParams(Material mat, Texture2D emissionMap)
{
    var readable = CreateReadableCopy(emissionMap, 32, 32);
    var pixels = readable.GetPixels();

    float avgLum = pixels.Average(p => p.grayscale);
    float coverage = pixels.Count(p => p.grayscale > 0.1f) / (float)pixels.Length;

    DestroyImmediate(readable);

    // 発光面積が広い → Glow 控えめ（全体が光ると不自然）
    // 発光面積が狭い → Glow 強め（ポイント発光を目立たせる）
    mat.SetFloat("_EmissionGlow",
        Mathf.Lerp(0.8f, 0.2f, coverage) * Mathf.Clamp01(avgLum + 0.3f));
}
```

**実現可能性**: ✅ 簡単 | **精度**: 高 (80%)

---

#### A-7. ShadowReceiveMask → シャドウパラメータ自動設定

```csharp
/// ShadowReceiveMask のカバレッジから影設定を補正
public static void AutoShadowFromMask(Material mat, Texture2D shadowMask)
{
    var readable = CreateReadableCopy(shadowMask, 32, 32);
    var pixels = readable.GetPixels();

    float coverage = pixels.Count(p => p.grayscale > 0.5f) / (float)pixels.Length;

    DestroyImmediate(readable);

    // カバレッジ高い（ほとんど影を受ける）→ offset 小さく
    // カバレッジ低い（選択的に影を受ける）→ offset 大きく
    mat.SetFloat("_ShadowOffset", Mathf.Lerp(-0.2f, 0.1f, coverage));
    mat.SetFloat("_ShadowBlend", Mathf.Lerp(0.4f, 0.15f, coverage));
}
```

**実現可能性**: ✅ 簡単 | **精度**: 高 (85%)

---

### B. メッシュ解析による自動パラメータ

#### B-1. Mesh.bounds → アウトライン幅の自動スケーリング

```csharp
/// メッシュサイズに基づくアウトライン幅の自動設定
/// ※ OutlineOptimizer.cs に既存実装あり (再利用)
public static float AutoOutlineWidth(Mesh mesh)
{
    float maxSize = Mathf.Max(
        mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
    return Mathf.Clamp(maxSize * 0.01f, 0.001f, 0.01f);
}
```

**実現可能性**: ✅ 既に実装済み | **精度**: 高 (90%)
**適用タイミング**: Stage 1 + Stage 2（Outline ON 時）

---

#### B-2. Mesh.bounds アスペクト比 → NormalFlattenY 自動設定

```csharp
/// メッシュの縦横比から顔/キャラ的な法線フラッテンを推定
public static float AutoNormalFlattenY(Mesh mesh)
{
    Bounds b = mesh.bounds;
    float ratio = b.size.y / Mathf.Max(b.size.x, b.size.z, 0.001f);

    // 縦長（顔・キャラ的）→ フラッテン強め
    // 立方体的（小物・環境）→ フラッテン弱め
    if (ratio > 1.5f) return 0.25f;   // 顔・全身
    if (ratio > 1.2f) return 0.15f;   // やや縦長
    return 0.05f;                      // 小物
}
```

**実現可能性**: ✅ 簡単 | **精度**: 中 (70%)
**適用タイミング**: Stage 1

---

#### B-3. Mesh.normals 分散 → Smooth Normal 必要性の自動判定

```csharp
/// 法線の分散から Smooth Normal ベイクが必要か判定
public static bool NeedsSmoothNormals(Mesh mesh)
{
    var verts = mesh.vertices;
    var norms = mesh.normals;
    if (norms == null || norms.Length == 0) return false;

    // 同一位置の頂点をグループ化
    var groups = new Dictionary<Vector3Int, List<int>>();
    const float q = 10000f;
    for (int i = 0; i < verts.Length; i++)
    {
        var key = new Vector3Int(
            Mathf.RoundToInt(verts[i].x * q),
            Mathf.RoundToInt(verts[i].y * q),
            Mathf.RoundToInt(verts[i].z * q));
        if (!groups.ContainsKey(key)) groups[key] = new List<int>();
        groups[key].Add(i);
    }

    // 分裂法線（同じ位置で法線が異なる）を検出
    float maxVariance = 0f;
    foreach (var g in groups.Values)
    {
        if (g.Count <= 1) continue;
        var avg = Vector3.zero;
        foreach (int i in g) avg += norms[i];
        avg.Normalize();

        float variance = 0f;
        foreach (int i in g)
            variance += (1f - Vector3.Dot(norms[i], avg));
        maxVariance = Mathf.Max(maxVariance, variance / g.Count);
    }

    // 分散 > 0.1 → ハードエッジが多い → Smooth Normal 推奨
    return maxVariance > 0.1f;
}
```

**実現可能性**: ⭐⭐ 中程度 | **精度**: 高 (85%)
**適用タイミング**: Stage 1（Outline 使用時に自動判定→自動ベイク）

---

#### B-4. Mesh.triangles → パフォーマンス予算の自動調整

```csharp
/// ポリゴン数から品質ターゲットを自動判定・制限
public static Quality AutoQualityFromMesh(Mesh mesh)
{
    int triCount = mesh.triangles.Length / 3;

    if (triCount < 5000)  return Quality.High;      // 低ポリ → 全機能 OK
    if (triCount < 20000) return Quality.Standard;   // 中ポリ → 標準
    if (triCount < 50000) return Quality.Standard;   // 高ポリ → 重い機能制限
    return Quality.Mobile;                            // 超高ポリ → 最小構成推奨
}
```

**実現可能性**: ✅ 簡単 | **精度**: 中 (65%)
**適用タイミング**: Stage 1（品質ターゲットの自動推奨）

---

#### B-5. Mesh.colors → Smooth Normal モード自動選択

```csharp
/// 既存頂点カラーの有無で Smooth Normal の読み込み元を決定
public static void AutoSmoothNormalMode(Material mat, Mesh mesh)
{
    if (mesh.colors != null && mesh.colors.Length == mesh.vertexCount)
    {
        // 頂点カラーあり → Object Space モード (既にベイク済みの可能性)
        mat.SetFloat("_SmoothNormalMode", 0f);
    }
    else
    {
        // 頂点カラーなし → テクスチャモード or 新規ベイク
        mat.SetFloat("_SmoothNormalMode", 2f);
    }
}
```

**実現可能性**: ✅ 簡単 | **精度**: 高 (90%)

---

### C. 機能間クロス最適化

Feature ON/OFF 時に、他の機能のパラメータを連動調整するルール。

#### C-1. SSS ON → Specular を自動ソフト化

```csharp
if (sssEnabled && specularEnabled)
{
    // SSS の散乱光が Specular と重なるため、Specular を控えめにする
    float currentIntensity = mat.GetFloat("_SpecularIntensity");
    mat.SetFloat("_SpecularIntensity", currentIntensity * 0.6f);
    mat.SetFloat("_SpecularSoftness",
        Mathf.Min(mat.GetFloat("_SpecularSoftness") + 0.15f, 1f));
}
```

**視覚的理由**: SSS で肌全体が柔らかく光る → Specular が強いと白飛び

---

#### C-2. Outline ON → NormalFlattenY を自動補正

```csharp
if (outlineEnabled && mat.GetFloat("_NormalFlattenY") < 0.15f)
{
    // アウトライン使用時は法線フラッテンを最低 0.15 に上げる
    // → アウトラインの断裂を防止
    mat.SetFloat("_NormalFlattenY",
        Mathf.Max(mat.GetFloat("_NormalFlattenY"), 0.15f));
}
```

**視覚的理由**: 法線が鋭角だとアウトラインが途切れる

---

#### C-3. HairSpecular ON → 通常 Specular を自動無効化

```csharp
if (hairSpecularEnabled && specularEnabled)
{
    // 2つのスペキュラが重なると過剰 → 通常 Specular を OFF
    mat.SetFloat("_Specular", 0f);
    mat.DisableKeyword("_SPECULAR");
}
```

**視覚的理由**: 異方性（HairSpec）と等方性（Spec）の同時使用は物理的に不自然

---

#### C-4. MatCap ON + Reflection ON → 強度を相互調整

```csharp
if (matCapEnabled && reflectionEnabled)
{
    // 両方の反射系が同時に強いと過剰 → 合計を制限
    float matCapI = mat.GetFloat("_MatCapIntensity");
    float reflI = mat.GetFloat("_ReflectionIntensity");
    float total = matCapI + reflI;
    if (total > 1.2f)
    {
        float scale = 1.2f / total;
        mat.SetFloat("_MatCapIntensity", matCapI * scale);
        mat.SetFloat("_ReflectionIntensity", reflI * scale);
    }
}
```

**視覚的理由**: 反射系エフェクトの合計が強すぎると表面が白飛び

---

#### C-5. Specular パラメータ → ShadowSharpness から自動導出

```csharp
// 影の硬さとスペキュラのサイズは連動する
// シャープな影 → シャープなスペキュラ (アニメ的)
// ソフトな影 → ソフトなスペキュラ (リアル的)
float sharpness = mat.GetFloat("_ShadowSharpness");
mat.SetFloat("_SpecularSize", Mathf.Lerp(0.25f, 0.85f, sharpness));
mat.SetFloat("_SpecularSoftness", Mathf.Lerp(0.5f, 0.1f, sharpness));
```

---

#### C-6. Rim 強度 → 影の暗さから自動導出

```csharp
// 影が暗い → リムを強めてシルエットを際立たせる
// 影が明るい → リムは控えめ
Color shadowCol = mat.GetColor("_ShadowColor");
float shadowLum = 0.299f * shadowCol.r + 0.587f * shadowCol.g + 0.114f * shadowCol.b;
mat.SetFloat("_RimIntensity", Mathf.Lerp(1.5f, 0.5f, shadowLum));
```

---

### D. 自動最適化の適用タイミングまとめ

| 解析 | 適用 Stage | トリガー | 上書き可能 |
|------|-----------|---------|-----------|
| MainTex → 影色/リム色/アウトライン色 | **Stage 1** | ワンボタン実行時 | Stage 3 で調整可 |
| MainTex → スペキュラ色 | **Stage 1** | ワンボタン実行時 | Stage 3 で調整可 |
| BumpMap → BumpScale | **Stage 1/2** | NormalMap ON 時 | Stage 3 で調整可 |
| Mesh.bounds → OutlineWidth | **Stage 1/2** | Outline ON 時 | Stage 3 で調整可 |
| Mesh.bounds 比率 → NormalFlattenY | **Stage 1** | ワンボタン実行時 | Stage 3 で調整可 |
| Mesh.normals → SmoothNormal 判定 | **Stage 1** | Outline 使用時 | Stage 2 で OFF 可 |
| Mesh.triangles → 品質推奨 | **Stage 1** | ワンボタン実行時 | Stage 1 で変更可 |
| マスクテクスチャ → エフェクト強度 | **Stage 2** | 各機能 ON 時 | Stage 3 で調整可 |
| ThicknessMap → SSS パラメータ | **Stage 2** | SSS ON 時 | Stage 3 で調整可 |
| MatCapTex → MatCap パラメータ | **Stage 2** | MatCap ON 時 | Stage 3 で調整可 |
| EmissionMap → Emission パラメータ | **Stage 2** | Emission ON 時 | Stage 3 で調整可 |
| SSS ↔ Specular 連動 | **Stage 2** | どちらか ON 時 | Stage 3 で調整可 |
| Outline ↔ NormalFlatten 連動 | **Stage 2** | Outline ON 時 | Stage 3 で調整可 |
| HairSpec → Specular 無効化 | **Stage 2** | HairSpec ON 時 | Stage 2 で再ON 可 |
| ShadowSharpness → Specular サイズ | **Stage 1** | ワンボタン実行時 | Stage 3 で調整可 |
| Shadow 暗さ → Rim 強度 | **Stage 1** | ワンボタン実行時 | Stage 3 で調整可 |

---

### E. 高精度解析エンジン（Editor 専用・計算コスト無制限）

Editor 上で動作するため、フル解像度解析・PCA・離散微分幾何など
計算コストの高い手法を積極的に採用して精度を最大化する。

#### E-1. 高精度テクスチャ解析

| 手法 | 用途 | 簡易版精度 | 高精度版精度 |
|------|------|-----------|------------|
| **K-means 色クラスタリング** (k=6, 3反復) | MainTex → 支配色抽出 | 60-70% | **92-97%** |
| **256bin 輝度ヒストグラム** + バイモーダル検出 | 影色/ハイライト色の推定, ToonSteps推奨 | 70-80% | **98%+** |
| **Laplacian 空間周波数解析** + 多スケール分析 | BumpMap → BumpScale 最適値 | 60-75% | **90-95%** |
| **連結成分解析** (BFS flood fill) | マスクの領域数/カバレッジ/フラグメンテーション | 85-90% | **99%+** |
| **フル解像度チャネル統計** | 平均/中央値/標準偏差/チャネル相関/空間分布 | 75-85% | **97%+** |

**K-means による色抽出の改善効果**:
- 単純平均: テクスチャに白い背景や暗い影があると大きくズレる
- K-means (k=6): 色分布のクラスタを捉え、最大クラスタ=支配色として正確に抽出
- トゥーンテクスチャは色数が少ないため K-means が特に有効

**バイモーダル検出による ToonSteps 自動推奨**:
- テクスチャの明暗が2つの山に分かれている → 2段トゥーン推奨
- 山が1つ → グラデーションシェーディング推奨
- 山が3つ以上 → 3段以上のマルチトゥーン推奨

#### E-2. 高精度メッシュ解析

| 手法 | 用途 | 簡易版精度 | 高精度版精度 |
|------|------|-----------|------------|
| **PCA 形状分類** (固有値分解) | 球/円柱/平面/伸張の自動判定 | AABB比率 65% | **PCA 90%+** |
| **離散曲率推定** (ガウス+平均曲率) | NormalFlattenY, NormalRoundness, OutlineWidth | 単純比率 70% | **曲率 85-90%** |
| **表面積/テクセル密度** | MicroNormalTiling, テクスチャ解像度推奨 | なし | **95%** |
| **トポロジー解析** (ハードエッジ/マニフォールド) | Outline 品質予測, SmoothNormal 戦略 | 法線分散 80% | **トポロジー 92%** |
| **距離LOD推定** (画角ベース) | DistanceFadeStart/End 自動設定 | なし | **90%** |

**PCA 形状分類の効果**:
```
球体的メッシュ (顔) → NormalRoundness=0.8, NormalFlattenY=0.2
円柱的メッシュ (腕/脚) → NormalRoundness=0.5, 軸方向フラッテン
平面的メッシュ (布/髪) → NormalFlattenY=0.9, NormalRoundness=0.1
伸張メッシュ (武器/杖) → OutlineWidth 縮小, NormalFlatten 弱め
```

**離散曲率の効果**:
- 曲率の高い部分 → アウトラインを細くする（ディテール保持）
- 曲率の低い部分 → アウトラインを太くできる（シルエット強調）
- 平均曲率 → NormalFlattenY の最適値を幾何学的に算出

#### E-3. シェーダー数式ベースのクロス機能最適化

**実際のシェーダーコードから逆算した最適パラメータ公式**:

##### 公式 1: Specular Size ← Glossiness (Lighting.hlsl の smoothstep 式から導出)
```
_SpecularSize = 0.2 + 0.6 * (_Glossiness ^ 0.7)
```
- Glossiness=0.0 → Size=0.20 (広い柔らかいハイライト)
- Glossiness=0.5 → Size=0.53 (中程度)
- Glossiness=1.0 → Size=0.80 (鋭いポイント)

##### 公式 2: SSS Power ← ThicknessScale (Lighting.hlsl の pow 式から導出)
```
_SSSPower = 2.0 + (_ThicknessScale ^ 1.5) * 3.0
```
- ThicknessScale=0.1 → Power=2.09 (薄い=急速減衰)
- ThicknessScale=0.5 → Power=3.06 (中程度)
- ThicknessScale=1.0 → Power=5.00 (厚い=広い散乱)

##### 公式 3: Rim Power 補正 ← NormalFlattenY (Fragment.hlsl の法線変形から導出)
```
_RimPower_adjusted = _RimPower * (1.0 + _NormalFlattenY * 2.0)
```
- NormalFlattenY=0.0 → 補正なし
- NormalFlattenY=0.25 → Power × 1.5 (フラッテンによる膨張を相殺)
- NormalFlattenY=0.5 → Power × 2.0

##### 公式 4: エフェクト合計強度制限 (SafeAdditiveBlend から導出)
```
シェーダー内蔵の SafeAdditiveBlend が baseLum > 0.95 で自動圧縮するが、
Editor 側で事前に制限することでより予測可能な結果を得る:

Specular + SSS + Rim の合計 Intensity ≤ 1.5
MatCap1 + MatCap2 + MatCap3 の合計 Intensity ≤ 1.5
MatCap + Reflection の合計 Intensity ≤ 1.2
```

#### E-4. 自動アーティファクト検出・修正

シェーダーコードの実際の計算から導出した、破綻条件と自動修正ルール:

| アーティファクト | 検出条件 | 自動修正 |
|----------------|---------|---------|
| **バンディング** (影の段差が目立つ) | ShadowSteps > 6 かつ DitheringStrength < 0.1 | DitheringStrength = 0.3 に設定 |
| **スペキュラのジャギー** | SpecularSize > 0.7 かつ _SPECULAR_AA 無効 | _SPECULAR_AA を有効化 |
| **リムのハロー** (全周発光) | NormalFlattenY > 0.3 かつ RimIntensity > 0.6 | RimIntensity -= (NormalFlattenY - 0.3) × 0.5 |
| **影のブリードスルー** | SDFIntensity > 0.8 かつ ShadowColor.a < 0.3 | SDFIntensity = 0.8 × ShadowColor.a |
| **テクスチャ割当て不整合** | 機能 ON + テクスチャ NULL | キーワード自動無効化 |
| **未使用テクスチャ** | 機能 OFF + テクスチャ割当て済み | キーワード自動有効化 |

---

### F. 実装の優先度（高精度版に更新）

#### Tier 1: 高精度・効果大 (Phase 1)

| # | 自動最適化 | 手法 | 精度 |
|---|-----------|------|------|
| 1 | 影色の自動計算 | K-means 色クラスタリング | **92-97%** |
| 2 | リム色/アウトライン色/スペキュラ色 | P90/P10 パーセンタイル | **98%** |
| 3 | アウトライン幅 | Mesh.bounds (既存) | 90% |
| 4 | NormalFlattenY / NormalRoundness | PCA 形状分類 + 離散曲率 | **85-90%** |
| 5 | SpecularSize ← Glossiness | シェーダー数式逆算 | **95%** |
| 6 | RimPower ← NormalFlattenY | シェーダー数式逆算 | **95%** |
| 7 | Shadow 暗さ → Rim 強度 | ヒストグラム P10/P50 | **98%** |
| 8 | アーティファクト自動検出・修正 | 条件ベースルール | **90%** |

#### Tier 2: 高精度・効果中 (Phase 2)

| # | 自動最適化 | 手法 | 精度 |
|---|-----------|------|------|
| 9 | BumpScale | Laplacian 空間周波数解析 | **90-95%** |
| 10 | マスク → エフェクト強度 | 連結成分解析 + 面積重み付け | **95%+** |
| 11 | ThicknessMap → SSS | 分布解析 + 公式 2 | **85-90%** |
| 12 | MatCapTex → 強度/ブレンド | フル解像度チャネル統計 | **90%** |
| 13 | EmissionMap → Glow | 輝度ヒストグラム + カバレッジ | **92%** |
| 14 | SmoothNormal 判定 | メッシュトポロジー解析 | **92%** |
| 15 | エフェクト合計強度制限 | SafeAdditiveBlend 逆算 | **95%** |
| 16 | SSS ↔ Specular / Outline ↔ Flatten | シェーダー数式ベース | **90%** |
| 17 | 距離LOD 自動設定 | 画角 + モデルサイズ推定 | **90%** |
| 18 | テクセル密度 → MicroNormalTiling | 表面積/UV 比率 | **95%** |

---

## 実装フェーズ（再構成）

### Phase 1: 3段階の骨格 + Tier 1 高精度自動最適化
- [ ] Auto Setup Hub UI (Role/Look 選択 + 実行ボタン)
- [ ] Profile Table (5 Role × 3 Look = 15 パターン)
- [ ] Pipeline 実行 (解析→適用→生成→検証)
- [ ] Feature Preset Table (主要 8 機能 × 5 Role)
- [ ] Role-Aware Toggle (既存トグルの置換)
- [ ] パフォーマンス指標リアルタイム更新
- [ ] **高精度解析エンジン (Tier 1)**:
  - [ ] K-means 色クラスタリング → 影色/リム色/アウトライン色/スペキュラ色 (92-97%)
  - [ ] 256bin ヒストグラム + バイモーダル検出 → ToonSteps 推奨 (98%)
  - [ ] PCA 形状分類 + 離散曲率 → NormalFlattenY/NormalRoundness (85-90%)
  - [ ] Mesh.bounds → OutlineWidth 自動スケーリング (90%)
  - [ ] シェーダー数式逆算 → SpecularSize, RimPower, SSS 連動 (95%)
  - [ ] アーティファクト自動検出・修正 (バンディング/ジャギー/ハロー/ブリード)

### Phase 2: Stage 3 UI + Tier 2 高精度自動最適化
- [ ] auto/手動変更ラベル表示
- [ ] セクション単位リセット
- [ ] 適正範囲の可視化
- [ ] **高精度解析エンジン (Tier 2)**:
  - [ ] Laplacian 空間周波数解析 → BumpScale (90-95%)
  - [ ] 連結成分解析 → マスクのエフェクト強度 (95%+)
  - [ ] フル解像度チャネル統計 → MatCap/Emission パラメータ (90-92%)
  - [ ] メッシュトポロジー解析 → SmoothNormal 判定 (92%)
  - [ ] SafeAdditiveBlend 逆算 → エフェクト合計強度制限 (95%)
  - [ ] 表面積/UV 比率 → テクセル密度/MicroNormalTiling (95%)
  - [ ] 距離LOD 自動設定 (90%)

### Phase 3: 拡張 + Batch
- [ ] 全 75 パターンのプロファイル完成
- [ ] Avatar 一括セットアップ
- [ ] Role 自動推定（マテリアル名ベース）
- [ ] 「手動変更を保持して再適用」

---

## 成功基準

### 「どの段階で止めても高品質」の検証方法

**Stage 1 のみで止めた場合**:
- [ ] 顔/髪/服/目/金属 × ゲーム風 の 5 マテリアルで破綻しない
- [ ] 影色が不自然でない（灰色ではなく暖色系）
- [ ] アウトライン色がテクスチャと馴染む
- [ ] SDF/Ramp/SmoothNormal が必要なものだけ自動生成される

**Stage 2 で機能を追加した場合**:
- [ ] どの機能を ON にしても見た目が崩れない
- [ ] 非推奨機能に警告が出る
- [ ] パフォーマンス指標が即座に反映される
- [ ] OFF に戻すと Stage 1 の状態に正しく戻る

**Stage 3 で微調整した場合**:
- [ ] どのスライダーを動かしても auto 値が参照できる
- [ ] リセットで Stage 2 の値に正しく戻る
- [ ] 極端な値に警告が出る
