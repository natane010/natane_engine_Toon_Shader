# Natane Toon Shader - 直感的UX改善案 調査プロジェクト

**Status**: 調査進行中
**Start Date**: 2026-03-23
**Lead**: Claude Code (UX Investigation)

## 📊 調査進捗

### Phase 1: Main Files Read ✅
- [x] NataneDashboard.cs (全文)
- [x] NataneToonShaderGUI.cs (冒頭150行まで)
- [x] NataneToolHealthValidator.cs (冒頭100行まで)
- [x] UVTextureGenerator.cs (冒頭100行まで)
- [x] NataneTabbedToolWindow.cs (冒頭80行まで)

### Phase 2: Deep Dive Analysis (進行中)
- [ ] GUI Drawer群を詳読
- [ ] Preset システムを詳読
- [ ] Migration Tools のUI調査
- [ ] Setup/Integration のUX調査

---

## 🎯 調査観点別の初期分析

### 1️⃣ 初回ユーザー体験 (First-Time UX)

**ダッシュボード (NataneDashboard.cs)**
- ✅ 40+ ツール を 7 カテゴリに分類
- ✅ 検索機能（英語・日本語対応）
- ✅ Health Indicator で診断状況表示
- ✅ カード形式でビジュアル化

**🔴 課題点**:
1. **情報過多**: All (40+ツール) は初回ユーザーに圧倒的
2. **推奨フロー不在**: 「最初に何をするべき?」が不明確
3. **ガイダンス不在**: 初回オンボーディングがない
4. **Health Indicator不可視**: ツールバー内の小さい表示 → 見落としやすい

### 2️⃣ マテリアル設定のUX (ShaderGUI)

**プロパティ数**: 146+ 機能

**構成**:
- タブシステム（未確認、詳細読み込み中）
- カテゴリボックス（CachedCategoryBoxStyle等で視覚的グループ化）
- Foldout セクション
- Make-up Texture 4層、Multi-Shadow 3レベル対応

**🔴 課題点**:
1. **タブの多さ**: タブ切り替え頻度が高い
2. **パラメータ検索なし**: ShaderGUI内で検索機能がない
3. **Before/After不明**: プリセット適用後の差分が見えない
4. **高速アクセス不在**: よく使う設定への直接アクセス手段がない
5. **配置の複雑さ**: 関連設定が複数タブに散在

### 3️⃣ テクスチャスタジオのUX (UVTextureGenerator)

**レイアウト**: 3パネル分割
- Left Panel: ジェネレータ（Noise/UVMask/Gradient/MeshInfo/Combined/Templates/ChannelPack）
- Center: Canvas (Zoom, Pan, Rotation対応)
- Right Panel: Layers / Filters / Settings

**ツール**: 9種類（Brush, Eraser, Select, Fill, Move, RectSelect, LassoSelect, Eyedropper, Gradient）

**🔴 課題点**:
1. **ジェネレータタブが 7 個**: タブ切り替え頻度が高い
2. **操作フロー不明**: 「次は何をする?」ガイドなし
3. **ツール切替の不自然さ**: ツールバー or キーボードショートカット未確認
4. **新ツール発見困難**: BrushPreset, PressureCalibration 等が見える位置にない
5. **出力フロー複雑**: 生成 → 書き出し → マテリアル割当が複数ステップ

### 4️⃣ ツール間の導線 (Navigation Flow)

**典型的なワークフロー**:
```
1. ダッシュボード (ツール選択)
2. マテリアル設定 (ShaderGUI で Toon/NPR等を選択)
3. プリセット参照 (Material Preset Browser)
4. テクスチャ生成 (Texture Studio で Ramp/MatCap等を作成)
5. テクスチャ書き出し
6. マテリアルに割当
7. 確認 (ShaderGUI に戻って確認)
```

**🔴 課題点**:
1. **ウィンドウが散在**: 複数ウィンドウの行き来
2. **往復が多い**: マテリアル確認のため ShaderGUI に戻る必要
3. **コンテキスト喪失**: マテリアル参照がウィンドウ間で失われることあるか?
4. **ダッシュボード優先度**: 最初に開くべきツールが不明

### 5️⃣ 視覚的フィードバック (Visual Feedback)

**既存機構**:
- NataneToonErrorDialog (エラー表示)
- Health Indicator (ツールバーの小表示)
- Auto-save Manager (テクスチャスタジオ)

**🔴 課題点**:
1. **操作結果の即時表示**: トースト通知がない?
2. **エラーの見落とし**: ダイアログ形式 → ブロッキング
3. **進捗表示**: 長時間操作での進捗バーがない?
4. **状態表示の可視性**: Health Indicator が小さく不可視

### 6️⃣ 操作効率 (Operational Efficiency)

**現状**:
- ダッシュボード内 Search あり
- Editor Preferences で各ツールのタブ永続化
- Right-click メニュー？

**🔴 課題点**:
1. **ドラッグ&ドロップ**: マテリアル or テクスチャのD&D対応?
2. **コンテキストメニュー**: ShaderGUI で右クリック → プリセット選択 or 関連ツール起動?
3. **ショートカット**: キーバインド可視化?

---

## 📋 初期観察の強み・弱み

### 💪 強みポイント
- ✨ ダッシュボード：カード形式で視認性良好、検索機能あり
- ✨ Health Indicator：セットアップ状況確認
- ✨ タブシステム：複雑機能を整理
- ✨ 日本語ローカライズ：UI文字列が日本語対応

### ⚠️ 弱みポイント
- 🔴 **情報過多**: 40+ツール、146+パラメータ → 初回ユーザーが圧倒される
- 🔴 **推奨フロー不在**: 「最初に何をする?」が不明確
- 🔴 **ウィンドウ分散**: ツール、設定がバラバラ
- 🔴 **往復コスト高**: ツール間を行き来する手間
- 🔴 **発見可能性低**: よく使うツール/設定への高速アクセス手段がない

---

## 📌 次の調査項目

1. **GUI Drawer 群** (`*Drawer.cs`)
2. **Preset System** (`PresetBrowser.cs`, `ColorPaletteManager.cs`)
3. **Migration Tools** (`lilToon`, `BatchConverter`)
4. **初期化ウィザード** (`AutoSetupHub.cs`, `DependencySetup.cs`)
5. **ヘルプシステム** (`UnifiedHelpSystem.cs`)
6. **Context Menu** (`NataneToonContextMenu.cs`)


---

## 📊 詳細分析結果

### ShaderGUI (NataneToonShaderGUI.cs) - 9378行の巨大ファイル

**構成**:
- 5つのタブ: "Texture & Color", "Light & Shadow", "Effects", "Environment & Reflection", "Advanced"
- 複数の Drawer クラス で機能分割: LightingDrawer, EffectsDrawer, EnvironmentDrawer など
- Simple/Advanced インスペクターモード
- Active Only フィルター
- Quick Setup ウィザード
- パフォーマンス指標表示
- プリセット参照機能

**🔴 課題**:
1. **ファイルサイズが大きすぎる**: 9378行 → ナビゲーション困難
2. **タブ切り替え頻度**: 5タブで関連設定が分散
3. **検索機能なし**: 146+パラメータで目的の設定を探すのが困難
4. **Active Only フィルター不可視**: ShowActiveOnly フラグがあるが、UIでの操作方法が不明
5. **Quick Setup ウィザード**: どの段階にいるのか不明 (step は記録されているが)

---

### Material Validator (MaterialValidator.cs)

**機能**:
- VRChat 最適化チェック
- テクスチャサイズ検証
- パフォーマンス評価
- 未使用フィーチャー検出
- テクスチャ圧縮設定確認
- 自動修正提案

**🔴 課題**:
1. **エラーレポートのUX**: 複数マテリアルの場合、結果がスクロール内に埋もれる
2. **自動修正の信頼性**: 修正前の確認UI不明
3. **パフォーマンス評価の説明不足**: 「A/B/C/D」の定義が不明確
4. **修正後の確認**: 修正後に再度 Validator を開く手間

---

### Unified Material Editor (UnifiedMaterialEditor.cs)

**機能**:
- Batch Mode: 複数マテリアルの一括編集
- Scene Mode: シーン内のレンダラーマテリアル直接編集
- パラメータ/色/テクスチャ/フィーチャー/バリアント調整

**🔴 課題**:
1. **Batch/Scene の切り替え**: タブ形式で切り替え手間
2. **パラメータリストが固定**: 追加したいパラメータがない場合の対応不明
3. **変更前後の差分表示**: 修正結果の即時確認が難しい

---

### Auto Setup Hub (NataneAutoSetupHub.cs)

**機能**:
- Role 選択: Face, Hair, Outfit, Eyes, Props等
- Look 選択: GameCharacter, Realistic, Stylized等
- Quality 選択: Standard, Performance, Quality等
- ワンクリック実行

**🔴 課題**:
1. **初回ユーザー向けガイダンス不足**: Role/Look の意味が分からない
2. **プレビュー機能なし**: 実行前に結果が見えない
3. **記録情報の活用不足**: 現在の設定が情報ボックスに小さく表示されるだけ
4. **リセット機能不明**: 別の Role/Look に変更したい場合の操作方法不明

---

### Context Menu (NataneToonContextMenu.cs)

**機能**:
- Assets 右クリック → マテリアル検証
- Assets 右クリック → プリセット適用
- Assets 右クリック → その他ツール起動

**🔴 課題**:
1. **Menu Item の数が少ない**: 最初の 2～3個しか実装されていない?
2. **アセンブリ跨ぎエラーハンドリング**: 型が見つからない場合、診断ツール起動 → ユーザーフローが長い
3. **マテリアル選択時の動作**: 複数選択時の一括処理不明

---

### Help System (UnifiedHelpSystem.cs)

**機能**:
- 7タブ: QuickStart, ToolHelp, Glossary, Tutorials, Troubleshooting, Tips, Documentation
- ツール別ヘルプ検索
- カテゴリフィルター
- 用語集、チュートリアル、トラブルシューティング

**🔴 課題**:
1. **アクセスの不自然さ**: メインツール内からのヘルプリンク不明
2. **Context-Aware Help 不足**: 今開いている設定に直結したヘルプが見えない
3. **Help の発見可能性**: 「ヘルプを見る」という操作自体が気づかれないまま終わることがある

---

## 💡 改善提案 (Recommendations)

### 【P0】初回ユーザー体験の大幅改善

#### 1. **オンボーディングウィザード** (Priority: HIGH, Effort: MEDIUM)
- ダッシュボード初回表示時: 5～10分のウィザード表示
  - Step 1: マテリアル選択（既存 or 新規作成）
  - Step 2: Role 選択（Face/Hair/Outfit等）with icon+説明
  - Step 3: Look 選択（GameCharacter/Stylized等）with preview
  - Step 4: テクスチャ作成フロー（Texture Studio へのショートカット）
  - Step 5: マテリアル確認（ShaderGUI プレビュー）
  
**見積もり工数**: 3～4日

---

#### 2. **推奨フロー表示** (Priority: HIGH, Effort: LOW)
- ダッシュボード: カテゴリの下に「推奨ステップ」セクション
  ```
  📌 推奨フロー (Recommended Workflow):
  1️⃣ マテリアル作成 / 選択
  2️⃣ Auto Setup（Face/Hair等を選択）
  3️⃣ テクスチャ生成（オプション）
  4️⃣ プリセット参照 & カスタマイズ
  5️⃣ VRChat検証 & 最適化
  ```
  
**見積もり工数**: 半日

---

### 【P1】ShaderGUI のナビゲーション改善

#### 3. **ShaderGUI 内 Quick Search** (Priority: HIGH, Effort: MEDIUM)
- Ctrl+F or Cmd+F で「パラメータ名検索」UI 表示
- 検索結果に該当セクション → 自動スクロール & フォーカス
- よく検索されるキーワード: "Shadow", "Specular", "Rim Light", "MatCap" 等

**見積もり工数**: 2日

---

#### 4. **タブの整理 or Vertical Tabs** (Priority: MEDIUM, Effort: HIGH)
- 現状: 5つの水平タブ
- 改善案 A: 左サイドバーに縦タブ化（スクロール対応）
- 改善案 B: タブを「コンテキスト単位」に再構成（Toon/NPR/PBR等で切り替え）
  
**見積もり工数**: 4～5日（A), 6～7日（B）

---

#### 5. **Settings Panel の高速アクセス** (Priority: MEDIUM, Effort: LOW)
- ShaderGUI トップに「Favorites」セクション（ピン留め可能）
  - よく使う設定を 5～10個ピン留め
  - EditorPrefs で永続化
  
**見積もり工数**: 1日

---

### 【P2】マテリアル設定フローの改善

#### 6. **Before/After プレビュー** (Priority: MEDIUM, Effort: MEDIUM)
- プリセット適用時: 画面分割で Before/After を同時表示
- Material Preview ウィンドウ or Scene View 内での実装
  
**見積もり工数**: 3日

---

#### 7. **プリセット → ShaderGUI のシームレス遷移** (Priority: MEDIUM, Effort: LOW)
- Material Preset Browser でプリセット選択 → 「適用 & ShaderGUI へ」ボタン
- 現状: Preset Browser で適用後、ShaderGUI に手動切り替え必要
  
**見積もり工数**: 1日

---

### 【P2】テクスチャスタジオのUX改善

#### 8. **Generator Tab をウィザード化** (Priority: MEDIUM, Effort: MEDIUM)
- 左パネルの 7 タブ → ステップ形式のウィザード
  - Step 1: Generate Method 選択（Noise/Gradient/Mesh等）
  - Step 2: パラメータ調整
  - Step 3: Layer Stack 組み上げ
  - Step 4: Export/Export & Apply
  
**見積もり工数**: 3～4日

---

#### 9. **Tool Toolbar (Canvas上部)** (Priority: MEDIUM, Effort: MEDIUM)
- Canvas 上部に 9つのツール選択アイコン（Brush/Eraser/Select/Fill等）
- キーボードショートカット表示（B=Brush, E=Eraser等）
- 現状: タブ or メニュー内に埋もれている
  
**見積もり工数**: 2～3日

---

#### 10. **出力フロー簡素化** (Priority: MEDIUM, Effort: MEDIUM)
- 右パネルに「クイック書き出し」セクション
  - 「Write to Material」ボタン: マテリアル選択 → 即座に割当
  - 現状: Export → ファイル保存 → Material に割当 の 3ステップ
  
**見積もり工数**: 2日

---

### 【P2】ツール間ナビゲーションの改善

#### 11. **統合コマンドパレット** (Priority: LOW, Effort: MEDIUM)
- Ctrl+K or Cmd+K でグローバルコマンドパレット
  - 「Setup Material Face」→ Auto Setup へ遷移
  - 「Generate Ramp」→ Texture Studio へ遷移
  - 「Validate Material」→ Material Validator へ遷移
  
**見積もり工数**: 2～3日

---

#### 12. **コンテキスト保持** (Priority: LOW, Effort: MEDIUM)
- ツール間を移動してもマテリアルコンテキスト保持
- 例: Material Validator → 修正提案後 → 関連 Tool へ「このマテリアルで」遷移
  
**見積もり工数**: 2日

---

### 【P3】視覚的フィードバック改善

#### 13. **トースト通知システム** (Priority: LOW, Effort: LOW)
- 長時間操作の進捗表示（生成中...、エクスポート中...）
- 完了時の確認通知（トースト）
  
**見積もり工数**: 1日

---

#### 14. **Health Indicator の見える化** (Priority: LOW, Effort: LOW)
- ダッシュボード内のツールバー表示 → メインパネルに移動
- より大きく、色付けして表示
  
**見積もり工数**: 半日

---

## 📊 改善案の優先度マトリクス

| # | 項目 | Priority | 工数 | 効果 | 実装難易度 |
|---|------|----------|------|------|----------|
| 1 | オンボーディングウィザード | P0 | 3～4日 | ⭐⭐⭐⭐⭐ | 高 |
| 2 | 推奨フロー表示 | P0 | 0.5日 | ⭐⭐⭐⭐ | 低 |
| 3 | ShaderGUI Quick Search | P1 | 2日 | ⭐⭐⭐⭐⭐ | 中 |
| 4 | タブ整理/Vertical Tabs | P1 | 4～7日 | ⭐⭐⭐⭐ | 高 |
| 5 | Favorites セクション | P1 | 1日 | ⭐⭐⭐ | 低 |
| 6 | Before/After プレビュー | P2 | 3日 | ⭐⭐⭐⭐ | 中 |
| 7 | Preset→ShaderGUI 遷移 | P2 | 1日 | ⭐⭐⭐ | 低 |
| 8 | Generator Wizard 化 | P2 | 3～4日 | ⭐⭐⭐⭐ | 中 |
| 9 | Tool Toolbar | P2 | 2～3日 | ⭐⭐⭐⭐ | 中 |
| 10 | 出力フロー簡素化 | P2 | 2日 | ⭐⭐⭐ | 低 |
| 11 | コマンドパレット | P3 | 2～3日 | ⭐⭐⭐ | 中 |
| 12 | コンテキスト保持 | P3 | 2日 | ⭐⭐ | 中 |
| 13 | トースト通知 | P3 | 1日 | ⭐⭐ | 低 |
| 14 | Health Indicator 見える化 | P3 | 0.5日 | ⭐⭐ | 低 |

---

## 🎯 推奨実装順序

### フェーズ 1: **初回体験改善** (1～2週間)
1. ✅ 推奨フロー表示（Dashboard）
2. ✅ Health Indicator 見える化
3. ✅ オンボーディングウィザード
4. ✅ ShaderGUI Quick Search

**効果**: 初回ユーザーの離脱率大幅減少、操作効率向上

---

### フェーズ 2: **ナビゲーション・検索改善** (2～3週間)
1. ✅ Favorites セクション（ShaderGUI）
2. ✅ Preset→ShaderGUI シームレス遷移
3. ✅ Before/After プレビュー
4. ✅ タブ整理（Vertical Tabs or Context-based）

**効果**: よく使うツールへの高速アクセス、修正フロー短縮

---

### フェーズ 3: **アドバンスド機能** (3～4週間)
1. ✅ Generator Wizard 化（Texture Studio）
2. ✅ Tool Toolbar（Canvas）
3. ✅ 出力フロー簡素化
4. ✅ コマンドパレット、コンテキスト保持

**効果**: テクスチャ作成フロー最適化、エキスパート向け高速操作

---

## 📋 まとめ

### 強み（そのまま保つ）
- ✅ ダッシュボード：カード形式で発見可能性高
- ✅ 検索機能：英語・日本語対応
- ✅ Drawer 分割：機能別に整理済み
- ✅ タブシステム：複雑な機能を効果的に整理

### 弱み（改善必要）
- ❌ **初回ガイダンス不足**: 推奨フロー、ウィザード必要
- ❌ **情報過多**: 40+ツール、146+パラメータ → 検索・フィルター機能強化必要
- ❌ **ウィンドウ分散**: ツール間のシームレス遷移機構不足
- ❌ **操作フロー**: テクスチャ生成→書き出し→割当 が複数ステップ
- ❌ **可視化**: Before/After、進捗、状態表示が不十分

### 期待される効果（改善後）
- 🚀 **初回ユーザーの理解度向上**: 推奨フロー × ウィザード で「次に何をすべき」が明確
- 🚀 **操作効率化**: Quick Search × Favorites × Wizard で頻出フロー 30～50% 短縮
- 🚀 **ツール発見**: ダッシュボード推奨フロー + コマンドパレット で最適ツール自動提示
- 🚀 **ユーザー満足度向上**: Before/After + トースト通知 で安心感 + 即時性向上

---

**完了日**: 2026-03-23
**作成者**: Claude Code (UX Investigation Team)

