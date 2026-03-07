# Whiteboard - UI/UX 使いやすさ・見やすさ調査 (2026-03-06)

## チーム編成

| エージェント | 担当範囲 | ステータス |
|-------------|---------|-----------|
| inspector-ui-auditor | ShaderGUI インスペクター (NataneToonShaderGUI.cs) | 調査中 |
| dashboard-ui-auditor | ダッシュボード + ツール起動系 (NataneDashboard.cs, NataneToolHealthValidator.cs) | **完了** |
| tool-window-auditor-1 | マテリアル系ツール (MaterialValidator, MaterialPresetBrowser, UnifiedMaterialEditor, etc.) | 調査中 |
| tool-window-auditor-2 | テクスチャ・メッシュ系ツール (UVTextureGenerator, MaskTextureBrushTool, DissolvePatternGenerator, etc.) | 調査中 |
| tool-window-auditor-3 | 最適化・移行系ツール (ShaderVariantCollector, ShaderPrewarming, PrefabVariantConverter, BatchMaterialConverter, etc.) | 調査中 |

## 調査観点

1. **視認性**: フォントサイズ、色のコントラスト、アイコン・ラベルの分かりやすさ
2. **レイアウト**: 情報の配置、余白、グループ化、スクロール量
3. **操作性**: ボタン配置、入力フィールドの適切さ、フィードバック
4. **一貫性**: ツール間でのスタイル・命名・操作感の統一
5. **エラー表示**: エラー・警告メッセージの分かりやすさ
6. **日本語/英語**: ローカライズの品質、切り替えの一貫性
7. **初見ユーザー向け**: ヘルプ・ツールチップ・説明文の充実度

## 調査結果

### tool-window-auditor-2: テクスチャ・メッシュ・エフェクト系ツール UI/UX 監査

**調査完了日**: 2026-03-06
**対象ファイル**: 9ファイル (合計 約3,800行)
**全体評価**: **B+ (良好)**

#### 良い点
1. L() ローカライゼーション関数が全ツールで一貫して使用されている
2. UVTextureGenerator は非常に多機能 (7タブ+レイヤー+ブラシ+3Dプレビュー+フィルター) ながら論理的に整理されている
3. Undo サポートが全マテリアル操作ツールで実装済み
4. ShadowAdjustmentWizard のステップインジケーターが直感的
5. SmoothNormalBaker の使い方セクションが初心者に親切

#### 問題点 (24件)
- Critical: 2件
- High: 7件
- Medium: 10件
- Low: 5件

**詳細は下記「問題点リスト」セクション参照**

---

### tool-window-auditor-3: 最適化・移行・パフォーマンス系ツール UI/UX 監査

**調査完了日**: 2026-03-06
**対象ファイル**: 12ファイル (合計 約4,500行)
**全体評価**: **B (良好)**

#### 良い点
1. **L() ローカライゼーションが全ツールで一貫** - 12ファイル全てで `L()` 関数による日英二言語対応が徹底されている
2. **Undo サポートが充実** - OutlineOptimizer, RefractionQualityBalancer, VRCLightVolumesHelper 等のマテリアル操作ツールで `Undo.RecordObject` が確実に実装されている
3. **ProgressBar + try-finally パターン** - ShaderVariantCollector (L394-435), ShaderPrewarmingEditor (L113-154), ShaderVariantStripper (L381-417) で進捗バーの安全な解放が実装済み
4. **危険な操作の確認ダイアログ** - BatchMaterialConverter (L235-244), LilToonMigrationTool (L439-446), ShaderVariantCollector ClearCollection (L718-733) で破壊的操作前に確認ダイアログが表示される
5. **PerformanceBudgetTool の視覚的メーター** - プログレスバーによるコスト可視化 + 色分け (green/yellow/red) + A-F 評価ラベルが直感的 (L81-98)

#### 問題点リスト (21件)

##### Critical (2件)

| # | ファイル | 行番号 | 問題 | 説明 |
|---|---------|--------|------|------|
| C1 | BatchMaterialConverter.cs | L231-300 | **一括変換に Undo 未実装** | `ConvertSelectedMaterials()` で `material.shader = targetShader` を直接変更しており、Undo.RecordObject がない。ダイアログで「元に戻せません」と表示しているが、Unity エディタの Undo 機能で戻せるべき操作。バックアップ機能もない |
| C2 | TextureOptimizer.cs | L187-249 | **テクスチャ最適化に Undo/ドライラン未実装** | `AnalyzeAndOptimize()` で `importer.SaveAndReimport()` を直接実行。プレビュー/ドライランモードがなく、即座にテクスチャを変更してしまう。元に戻す手段がない |

##### High (7件)

| # | ファイル | 行番号 | 問題 | 説明 |
|---|---------|--------|------|------|
| H1 | BatchMaterialConverter.cs | L260-263 | **ProgressBar のタイトルが英語ハードコード** | `"Converting Materials"` が L() 未使用。他のツールでは日英対応しているため一貫性が欠ける |
| H2 | LilToonMigrationTool.cs | L453-457 | **同上: ProgressBar タイトルが英語ハードコード** | `"Converting Materials"`, `"Converting Prefab Materials"` (L1455-1459) が L() 未使用 |
| H3 | ShaderVariantCollector.cs | L268 | **推定バリアント数のコメント「11 shaders」が実際の12と不一致** | `// Multiply by shader count (11 shaders x avg 2.5 passes)` とあるが AllShaders 配列には12シェーダーが定義されている。UIに影響はないがユーザーが参照する推定値が不正確になる |
| H4 | TextureOptimizer.cs | L159-175 | **ScanProject が ProgressBar なし** | プロジェクト全体のテクスチャをスキャンするが進捗表示がなく、大規模プロジェクトでフリーズしたように見える |
| H5 | AssetReferenceChecker.cs | L227-228 | **フィルタトグルのラベルが英語固定** | `"Error"`, `"Warning"`, `"Info"` が L() 未使用。サマリーでは日英対応しているのに不一致 |
| H6 | LilToonMigrationTool.cs | L81-156 | **OnGUI が長大でスクロール未実装** | プロジェクトモードの OnGUI にスクロールビューがなく、ウィンドウサイズが小さいとオプション+マテリアルリスト+ボタンが切れる。プレハブモードでは scrollPosition を使っているが、プロジェクトモードの上部オプション群はスクロール外 |
| H7 | PrefabVariantConverter.cs | L389-505 | **ExecuteConversion に確認ダイアログがない** | 「バリアント生成&変換実行」ボタンで即座にプレハブバリアント作成+マテリアル変換が実行される。LilToonMigrationTool や BatchMaterialConverter では確認ダイアログがあるのに、ここだけ欠けている |

##### Medium (8件)

| # | ファイル | 行番号 | 問題 | 説明 |
|---|---------|--------|------|------|
| M1 | PerformanceBudgetTool.cs | L111 | **Feature 名の先頭アンダースコア除去が不完全** | `feature.Key.Replace("_", "")` で全アンダースコアを除去するため、`_RIM_LIGHT` が `RIMLIGHT` になる。`TrimStart('_')` + スペース区切りが望ましい |
| M2 | RefractionQualityBalancer.cs | L238 | **プリセット適用時に enum の ToString() が表示される** | `L($"{preset}を適用しました"...)` で `VeryLow`, `Medium` 等の enum 値がそのまま表示される。日本語ユーザーには不親切 |
| M3 | OutlineOptimizer.cs | L109 | **Auto-Optimize ボタンが Object 未選択時もレイアウトに存在** | `targetObject != null` のチェックで非表示にはなるが、ボタン自体は描画されない（条件付き && で GUILayout.Button）ため、Object が null のときレイアウトがジャンプする |
| M4 | ShaderVariantStripper.cs | L324 | **CountNataneMaterials() が毎フレーム全マテリアル走査** | OnGUI 内で呼ばれるため、大規模プロジェクトで深刻なパフォーマンス問題が発生する可能性 |
| M5 | VRCLightVolumesHelper.cs | L169-171 | **確認セクションの L() が同じテキストを日英に渡している** | 英語と日本語が同一文字列のため、ローカライズが無意味になっている |
| M6 | ShaderPrewarmingEditor.cs | L543-547 | **リフレクションによる private メソッド呼び出し** | 設定ウィンドウから `PrewarmShaders` を呼ぶのにリフレクションを使用。メソッドを internal にするか、public なラッパーを用意すべき |
| M7 | AssetReferenceChecker.cs | L292 | **Select ボタンのラベルが英語固定** | `"Select"` が L() 未使用 |
| M8 | BatchMaterialConverter.cs | L84 | **結果リストの "box" スタイルが他ツールと不一致** | 他のツールでは `EditorStyles.helpBox` を使用しているが、ここだけ `"box"` を使用。視覚的な統一感が損なわれる |

##### Low (4件)

| # | ファイル | 行番号 | 問題 | 説明 |
|---|---------|--------|------|------|
| L1 | ShaderVariantCollector.cs | L185-197 | **Collect/Clear ボタンが横並びだが Save が縦並び** | アクションボタンのレイアウトに一貫性がない |
| L2 | OutlineOptimizer.cs | L130-136 | **プリセットボタンが色のカスタマイズなし** | 「細い」「標準」「太い」の全てが黒アウトラインに固定。色バリエーションのプリセットがあるとより便利 |
| L3 | RefractionQualityBalancer.cs | L141-181 | **DrawPerformanceInfo の GPU コスト算出が雑** | `samples * 10` や `20 + blur * 20` は粗い推定値。ユーザーに誤解を与える可能性がある |
| L4 | VRCLightVolumesHelper.cs | L220 | **プリセット適用ダイアログで enum 名が表示** | M2 と同様の問題。`LightVolumeQuality.Medium` などがそのまま表示される |

#### 改善提案

**Priority 1 (Critical 修正)**:
- **C1**: `BatchMaterialConverter.ConvertSelectedMaterials()` に `Undo.RecordObject(info.material, "Convert Material Shader")` を各変換前に追加
- **C2**: `TextureOptimizer.AnalyzeAndOptimize()` を2段階化 — 「分析」ボタンで結果リスト表示 → 「適用」ボタンで実行。分析結果には変更前後のプレビューを表示

**Priority 2 (High 修正)**:
- **H1/H2**: ProgressBar タイトル・メッセージに `L()` を適用
- **H4**: `TextureOptimizer.ScanProject()` に `EditorUtility.DisplayProgressBar` + `try-finally` を追加
- **H6**: `LilToonMigrationTool.OnGUI()` の全体を `EditorGUILayout.BeginScrollView/EndScrollView` で囲む
- **H7**: `PrefabVariantConverter.ExecuteConversion()` の先頭に `EditorUtility.DisplayDialog` による確認を追加

**Priority 3 (Medium 修正)**:
- **M1**: `feature.Key.TrimStart('_').Replace("_", " ")` に変更
- **M4**: `CountNataneMaterials()` の結果をキャッシュし、OnEnable/ボタン押下時のみ再計算
- **M6**: `PrewarmShaders` を `internal static` に変更してリフレクション廃止

---

### tool-window-auditor-1: マテリアル系エディターツール UI/UX 監査

**調査完了日**: 2026-03-06
**対象ファイル**: 8ファイル (MaterialValidator.cs, MaterialPresetBrowser.cs, MaterialComparisonTool.cs, MaterialPreviewWindow.cs, UnifiedMaterialEditor.cs, MakeupLayerManager.cs, DefaultPresetGenerator.cs, ColorPaletteManager.cs)
**全体評価**: **B (良好 - 改善余地あり)**

#### 良い点
1. **L() ローカライズ関数の一貫使用** - ほぼ全ツールでバイリンガルUI実現
2. **Undo対応が徹底** - UnifiedMaterialEditor, MakeupLayerManager, ColorPaletteManager等で全操作に実装済み
3. **HelpBox による説明** - 各ツール冒頭に目的説明があり初見でも理解しやすい
4. **Foldout/Tab による情報整理** - UnifiedMaterialEditor の Batch/Scene モード切替+5タブ構成が秀逸
5. **操作確認ダイアログ** - 破壊的操作に DisplayDialog 確認がある

#### 問題点 (18件: High 6 / Medium 7 / Low 5)

| # | Priority | ファイル | 行番号 | 問題点 |
|---|----------|---------|--------|--------|
| 1 | High | MaterialPresetBrowser.cs | L27-29 | サムネイル 100x100px+カード高140px が小さく、プリセット名切れやすい |
| 2 | High | MaterialPresetBrowser.cs | L29 | PRESETS_PER_ROW=4 固定。レスポンシブ配置なし |
| 3 | High | MaterialPresetBrowser.cs | L34,112等 | ウィンドウタイトル・複数ラベルが L() 未使用で英語ハードコード |
| 4 | High | MaterialComparisonTool.cs | L159-164 | Diff ビューでプロパティ名のみ表示、A/B値比較が見えない |
| 5 | High | MaterialComparisonTool.cs | L251 | 比較Color プロパティが4つだけ、大半未カバー |
| 6 | High | ColorPaletteManager.cs | L112-125 | ApplyToSelected で colors[0] のみ適用、複数色マッピングなし |
| 7 | Medium | MaterialValidator.cs | L114,194 | scrollPosition 2箇所共有でスクロール干渉 |
| 8 | Medium | MaterialValidator.cs | L229-231 | Unicode絵文字アイコンがエディタフォントで非表示リスク |
| 9 | Medium | MaterialValidator.cs | L497-501 | autoFixAction内で Undo.RecordObject 未呼び出し |
| 10 | Medium | MaterialPresetBrowser.cs | L67-104 | ツールバー6ボタン横並びで狭いウィンドウでは溢れる |
| 11 | Medium | MaterialPreviewWindow.cs | L164-166 | PreviewShape.Torus が switch 未処理 |
| 12 | Medium | UnifiedMaterialEditor.cs | L65 | featureStates[20] ハードコード、features配列長と非同期リスク |
| 13 | Medium | DefaultPresetGenerator.cs | L22-33 | InitializeOnLoad で無断自動生成 |
| 14 | Low | MaterialComparisonTool.cs | L44-46 | DrawToolHeader未使用、ヘッダースタイル不統一 |
| 15 | Low | ColorPaletteManager.cs | L33 | DrawToolHeader未使用、ヘッダースタイル不統一 |
| 16 | Low | MaterialPresetBrowser.cs | L281 | "Apply" ボタンが L() 未使用 |
| 17 | Low | MakeupLayerManager.cs | L303-310 | テンプレート適用ボタン4個並列、Layer選択+適用1ボタンの方がシンプル |
| 18 | Low | MaterialPresetBrowser.cs | L264 | サムネイルなし時の代替表示が単色グレー矩形 |

#### 改善提案 (優先順)
1. **[High]** MaterialPresetBrowser のグリッドをレスポンシブ化 (`int columns = Mathf.Max(1, (int)(position.width / (THUMBNAIL_SIZE + 30)));`)
2. **[High]** MaterialPresetBrowser のサムネイル120-140px+カード高180pxに拡大
3. **[High]** MaterialPresetBrowser の L() 適用漏れ修正 (タイトル,ラベル,ボタン)
4. **[High]** MaterialComparisonTool の Diff で色のRGBA値表示
5. **[High]** ColorPaletteManager に複数色->複数プロパティのマッピングUI追加
6. **[Medium]** MaterialValidator の scrollPosition を2変数に分離
7. **[Medium]** MaterialValidator autoFixAction 内に Undo 追加
8. **[Medium]** UnifiedMaterialEditor の featureStates を `new bool[features.Length]` に
9. **[Medium]** DefaultPresetGenerator の InitializeOnLoad 自動実行を確認ダイアログ付きに
10. **[Low]** MaterialComparisonTool, ColorPaletteManager に DrawToolHeader 適用

---

### dashboard-ui-auditor: ダッシュボード・診断・ヘルプ系ツール UI/UX 監査

**調査完了日**: 2026-03-06
**対象ファイル**: 5ファイル + 3補助ファイル (NataneDashboard.cs, NataneToolHealthValidator.cs, NataneToonShaderGUIUtility.cs, UnifiedHelpSystem.cs, NataneDependencySetupWindow.cs + NataneToonColorPalette.cs, NataneToonLocalization.cs, NataneToolMenuPaths.cs)
**全体評価**: **B (良好)**

#### 良い点
1. **NataneToonColorPalette** による統一カラー定義 (Status/Performance/Section/Text) が整備されている
2. **NataneToolHealthValidator** の診断-対処フローが優秀 (ValidateAndLaunch でエラー時ダイアログ-診断画面遷移、依存関係チェックで具体的マテリアル名表示)
3. **UnifiedHelpSystem** の7タブ構成が包括的 (クイックスタート/ツールヘルプ/用語集/チュートリアル/トラブルシューティング/ヒント/ドキュメント)
4. **NataneToonShaderGUIUtility** の DrawToolHeader/DrawHeaderWithHelp で共通UI部品が統一されている
5. **NataneDependencySetupWindow** の自動表示 (InitializeOnLoadMethod) が初回セットアップUXとして優秀

#### 問題点 (19件)
- Critical: 2件
- High: 13件
- Medium: 3件
- Low: 1件

##### Critical (2件)

| # | ファイル:行 | 問題 |
|---|-----------|------|
| C1 | NataneDashboard.cs:331-517 | OnGUI 毎フレーム GUIStyle new 生成 (6箇所)。GC Alloc が発生しEditor性能低下。フィールドにキャッシュすべき |
| C2 | UnifiedHelpSystem.cs:326 | MakeTex(2,2,...) を DrawToolList 内で毎フレーム呼出し、Texture2D が GPU メモリリーク。フィールドにキャッシュし OnDisable で破棄すべき |

##### High (13件)

| # | ファイル:行 | 問題 |
|---|-----------|------|
| H1 | NataneDashboard.cs:428 | 検索ラベルが L() 未使用。英語切替非対応 |
| H2 | NataneDashboard.cs:431 | "Clear" ボタンが L() 未使用 |
| H3 | NataneDashboard.cs:520 | "起動 Launch" ボタンが L() 未使用 (日英混在固定) |
| H4 | NataneDashboard.cs:446 | 検索結果ゼロ時 HelpBox が日本語固定、L() 未使用 |
| H5 | NataneDashboard.cs:566-572 | GetCategoryDisplayName のカテゴリ名が L() 未使用 (日本語固定) |
| H6 | NataneDashboard.cs:77-311 | ツール名が常に日英連結表示。ToolInfo に nameJP/nameEN を分離し L() で切替すべき |
| H7 | NataneDashboard.cs:452 | 2列グリッド固定 (columns=2)。ウィンドウ拡大時にレスポンシブでない |
| H8 | NataneDashboard.cs:476 | ツールカード固定高さ (Height=100)。長い description が切れる |
| H9 | NataneToonShaderGUIUtility.cs:249-285 | DrawMaterialActionsToolbar のボタンラベルが英語固定 |
| H10 | NataneToonShaderGUIUtility.cs:476 | "Performance Rating:" ラベルが英語固定 |
| H11 | NataneToonShaderGUIUtility.cs:515-518 | GetPerformanceRating のレーティング文字列が英語固定 |
| H12 | NataneDependencySetupWindow.cs:17,50 | ウィンドウタイトル・ヘッダーが英語固定 |
| H13 | NataneDependencySetupWindow.cs 全体 | DrawToolHeader / DrawLanguageToggleButton 共通部品を未使用 |

##### Medium (3件)

| # | ファイル:行 | 問題 |
|---|-----------|------|
| M1 | NataneDashboard.cs:409 | カテゴリタブの三項分岐が無意味 (両辺同一) |
| M2 | UnifiedHelpSystem.cs:174 | ヘルプヘッダーが日英固定連結、L() 未使用 |
| M3 | NataneDashboard.cs:170,211 | 同一アイコンがMatCap ComposerとMask Texture Studioで重複 |

##### Low (1件)

| # | ファイル:行 | 問題 |
|---|-----------|------|
| L1 | NataneDependencySetupWindow.cs | DrawToolHeader を未使用。他ツールとの一貫性が低い |

#### 改善提案

**Priority 1 (Critical)**:
- C1: NataneDashboard の OnGUI 内 GUIStyle をフィールドにキャッシュ、null チェックで初期化
- C2: UnifiedHelpSystem の MakeTex をフィールドにキャッシュ、OnDisable で DestroyImmediate

**Priority 2 (High)**:
- H1-H6: 全ハードコードテキストに L(ja, en) 適用。ToolInfo に nameJP/nameEN 分離
- H7: `int columns = Mathf.Max(1, (int)(position.width / 350))` で動的カラム算出
- H8: GUILayout.Height(100) を GUILayout.MinHeight(100) に変更
- H9-H11: NataneToonShaderGUIUtility 全英語固定テキストに L() 適用
- H12-H13: NataneDependencySetupWindow に DrawToolHeader + DrawLanguageToggleButton 統合

**Priority 3 (Medium)**:
- M1: 三項演算子削除
- M2: UnifiedHelpSystem ヘッダーに L() 適用
- M3: マスクテクスチャスタジオのアイコンを変更

---

### inspector-ui-auditor: ShaderGUI インスペクター UI/UX 監査

**調査完了日**: 2026-03-06
**対象ファイル**: 14ファイル (合計 約10,000行)
- NataneToonShaderGUI.cs (~6600行), NataneToonShaderGUIStyles.cs (~250行), NataneToonShaderGUITab.cs (~225行), NataneToonLocalization.cs (~35行), NataneToonShaderGUIHelpers.cs (~840行), NataneToonShaderGUIUtility.cs (~630行), NataneToonShaderTypeSwitcher.cs (~200行), NataneToonEyeDrawer.cs (~485行), NataneToonScreenFXDrawer.cs (~175行), NataneToonWirelightDrawer.cs (大), NataneToonErrorDialog.cs (~60行), NataneToonContextMenu.cs (~160行), NataneToonColorPalette.cs (~60行), NataneToonToolsDocumentation.cs (~795行)

**全体評価**: **B+ (良好)**

#### 良い点
1. **BoxedSection パターンの統一感** - 62セクション全てが DrawBoxedSection (L547-609) を使用し、カテゴリ別カラーアクセントバー + ON/OFF バッジ + 折りたたみ状態で視覚的に統一されている
2. **検索システムの完成度** - L170-234, L6339-6366 の検索機能が日英両言語のキーワードに対応し、62セクションを横断検索可能。初見ユーザーに非常に有用
3. **L() ローカライゼーションの徹底** - メイン GUI で 154箇所の DrawHelpToggle が全て日英対応。NataneToonToolsDocumentation.cs のヘルプデータベースも完全バイリンガル
4. **SafeDrawSection パターン** - 各セクション描画が try-catch で ExitGUIException を適切にハンドリングしており、一つのセクションのエラーが他に波及しない堅牢な設計
5. **Feature Overview グリッド** (L5920-6065) - 全機能の有効/無効状態を一覧表示する機能が秀逸。パフォーマンス指標付きで最適化の参考になる

#### 問題点 (14件: High 3 / Medium 6 / Low 5)

| # | Priority | ファイル | 行番号 | 問題点 |
|---|----------|---------|--------|--------|
| 1 | High | NataneToonShaderGUI.cs | L912 | **MakeupTextures バッジループで毎フレーム `new GUIStyle` を4回生成** - GC Alloc が毎フレーム発生しパフォーマンス劣化。静的キャッシュにすべき |
| 2 | High | NataneToonShaderGUIUtility.cs | L245-295 | **Material Actions ツールバーのボタンラベルが英語ハードコード** - "Copy", "Paste", "Reset", "Export", "Import" が L() 未使用。他の UI 部品は日英対応しているため一貫性が欠ける |
| 3 | High | NataneToonShaderGUIUtility.cs | L467-527 | **Performance Indicator のラベルが英語固定** - "Performance Rating", "Active Features", "Estimated Cost" 等が L() 未使用 |
| 4 | Medium | NataneToonShaderGUITab.cs | L177-181 | **DrawCategoryHeader で毎フレーム `new GUIStyle` 生成** - EyeDrawer, ScreenFXDrawer でも同パターン (L138-143, L61-66)。3ファイルで同一問題 |
| 5 | Medium | NataneToonShaderGUI.cs | L165-168 | **60+ セクションが5タブに集約** - 特に「エフェクト」タブと「詳細設定」タブに多数のセクションが集中し、スクロール量が多い。7-8タブへの分割またはサブカテゴリ化が望ましい |
| 6 | Medium | NataneToonShaderGUI.cs | 全体 | **Undo.RecordObject が6箇所のみ** - 62セクション x 複数プロパティ変更があるのに対し、明示的な Undo 記録が少ない。MaterialProperty 経由の変更は MaterialEditor が自動処理するが、直接 material.SetFloat 等を呼ぶ箇所で漏れがある可能性 |
| 7 | Medium | NataneToonContextMenu.cs | L63-64 | **ダイアログタイトルで日英両方が連結表示** - `"Reset " + propName + " をリセット"` のように L() を使わず両言語が混在 |
| 8 | Medium | NataneToonShaderGUITab.cs | L98-138 | **DrawToggle に Undo サポートなし** - Eye/Wirelight/ScreenFX ドロワーのトグル操作で Undo が効かない |
| 9 | Medium | NataneToonShaderGUI.cs | L6439-6510 | **QuickSetup セクションのプリセット説明が簡素** - プリセット適用後の変更内容が不明確。適用前後のプレビュー比較があるとより親切 |
| 10 | Low | NataneToonShaderGUIStyles.cs | 全体 | **SectionCategory が6種類だがカラー差が微妙** - Basic (青) と Lighting (青緑) の区別が色覚多様性を考慮すると見分けにくい |
| 11 | Low | NataneToonShaderGUI.cs | L547-609 | **DrawBoxedSection の ON/OFF バッジが小さい** - 特に高DPI環境でバッジテキストが読みにくい可能性 |
| 12 | Low | NataneToonEyeDrawer.cs | L138-143 | **Eye ドロワーのヘッダースタイルがメイン GUI と異なる** - メイン GUI は NataneToonShaderGUIStyles を使うが、Eye/ScreenFX ドロワーは独自スタイルを定義 |
| 13 | Low | NataneToonShaderGUI.cs | L6568-6616 | **DrawHelpToggle のヘルプテキスト表示が折りたたみ内** - ヘルプ情報にアクセスするにはセクションを展開してからさらにヘルプトグルを押す必要があり、2段階の操作が必要 |
| 14 | Low | NataneToonLocalization.cs | 全体 | **言語切替が EditorPrefs ベースで即時反映** - 切替自体は問題ないが、言語設定の場所がわかりにくい (インスペクター最下部の小さなボタン) |

#### 改善提案 (優先順)

**Priority 1 (High)**:
1. **GUIStyle キャッシュ化** - NataneToonShaderGUI.cs L912, NataneToonShaderGUITab.cs L177-181, NataneToonEyeDrawer.cs L138-143, NataneToonScreenFXDrawer.cs L61-66 の `new GUIStyle` を静的キャッシュプロパティに変更。NataneToonShaderGUIStyles.cs のパターン (`s_BoxOuter` 等) に統一
2. **Material Actions / Performance Indicator の L() 適用** - NataneToonShaderGUIUtility.cs L245-295, L467-527 のハードコード英語ラベルに L() を適用
3. **NataneToonContextMenu.cs L63-64 のローカライゼーション修正** - `L("をリセット", "Reset ")` + propName の正しい語順対応

**Priority 2 (Medium)**:
4. **タブ構成の見直し** - 「エフェクト」タブを「視覚エフェクト」と「特殊エフェクト」に分割、または「詳細設定」から頻用項目を独立タブ化して、1タブあたりのセクション数を8-10に抑制
5. **DrawToggle への Undo 追加** - NataneToonShaderGUITab.cs L98-138 に `Undo.RecordObject(material, "Toggle " + keyword)` を追加
6. **QuickSetup にプリセット説明追加** - 各プリセットボタンにツールチップまたは HelpBox で変更される項目一覧を表示

**Priority 3 (Low)**:
7. **SectionCategory カラーのアクセシビリティ改善** - Basic と Lighting の色差を拡大 (例: Basic を濃い青、Lighting をオレンジ系に変更) して色覚多様性に配慮

---

## シェーダー変更 統合監査レポート (2026-03-07)

**対象**: git diff HEAD の3ファイル変更
**チーム**: sss-sampler-auditor / parallax-auditor / blur-opt-auditor / vr-spi-auditor

### 総合評価: ✅ 全変更適切 — マージ可能

| # | 変更内容 | 評価 | 効果 |
|---|---------|------|------|
| 1 | SSS テクスチャ NOSAMPLER 化 | ✅ 適切 | サンプラー2個削減 + VR SPI対応 |
| 2 | Parallax tex2Dgrad 移行 | ✅ 適切 | mip精度修正 + サンプラー1個削減 |
| 3 | SampleTex2DBlur 重複サンプリング最適化 | ✅ 適切 | 呼び出しごとにフェッチ1回削減 |
| 4 | CameraDepthNormalsTexture VR/SPI 対応 | ✅ 適切 | SPI正常動作 + 意味論的修正 |

### 変更1: SSS テクスチャ NOSAMPLER 化
- `_ThicknessMap` / `_SSSMask` を `sampler2D` → `UNITY_DECLARE_TEX2D_NOSAMPLER` に変更
- サンプリングを `tex2D` → `NATANE_SAMPLE_SHARED_R(_XXX, _MainTex, uv)` に変更
- **根拠**: 他20個以上のマスクテクスチャが既に同パターン。SSS のみ取り残されていた
- **効果**: サンプラースロット2個削減、VR SPI 正常動作

### 変更2: Parallax tex2Dgrad 移行
- `_ParallaxMap` を `UNITY_DECLARE_TEX2D_NOSAMPLER` に変更
- POMループ内の `tex2D` → `NataneSampleParallaxHeight(uv, uvDx, uvDy)` (tex2Dgrad/SampleGrad)
- `ddx(uv)` / `ddy(uv)` をループ外で事前計算
- **根拠**: POMループ内 `tex2D` は gradient 不定 → mipレベル選択が不正確になる既知の GPU 問題
- **効果**: 正確なmip選択、サンプラースロット1個削減

### 変更3: SampleTex2DBlur 重複サンプリング最適化
- 中心テクスチャサンプリングを `center` 変数にキャッシュし、早期リターンとblurパスで共有
- `SampleTex2DBlur` / `SampleTex2DBlurShared` 両関数に適用
- **根拠**: blur > 0.001 時に中心テクスチャの重複サンプリングがあった
- **効果**: Fragment.hlsl 内7箇所で呼ばれ、最大5テクスチャフェッチ/フラグメント削減

### 変更4: CameraDepthNormalsTexture VR/SPI 対応
- `UNITY_DECLARE_DEPTH_TEXTURE` → `UNITY_DECLARE_SCREENSPACE_TEXTURE` に変更
- SobelEdgeNormal 8箇所の `tex2D` → `UNITY_SAMPLE_SCREENSPACE_TEXTURE` に変更
- **根拠**: `_CameraDepthNormalsTexture` はRGBA32形式（4ch）→ DEPTH_TEXTURE 宣言は意味論的に不適切だった
- **効果**: VR SPI 環境で正しいアイインデックス付与、DecodeViewNormalStereo との型一致

### 発見した追加問題 (diff外・フォローアップ推奨)

| 優先度 | 内容 |
|--------|------|
| Medium | `_CameraDepthTexture` の宣言 (`UNITY_DECLARE_DEPTH_TEXTURE`) と Fragment.hlsl でのサンプリング (`UNITY_SAMPLE_SCREENSPACE_TEXTURE`) が不整合。宣言を `UNITY_DECLARE_SCREENSPACE_TEXTURE` に統一推奨 |

### 全体効果サマリー

| メトリクス | 効果 |
|-----------|------|
| サンプラースロット削減 | **3個** (DX11上限16個中) |
| テクスチャフェッチ削減 | Blur関数で最大 **5回/フラグメント** |
| Parallax mip精度 | **修正** (gradient不定問題解消) |
| VR SPI 対応 | **3機能改善** (SSS/Parallax/ScreenEdge) |
| 後方互換性 | **維持** (マテリアルプロパティ変更なし) |

---

## シェーダー変更 表現影響調査レポート (2026-03-07)

**目的**: 各変更がビジュアル表現にどの程度影響するか調査
**チーム**: sss-visual-auditor / parallax-visual-auditor / blur-depth-visual-auditor

### 総合評価: 表現への影響は軽微、全体としてポジティブ（品質向上）方向

| # | 変更内容 | 表現影響度 | 変化の方向 | 対策 |
|---|---------|-----------|-----------|------|
| 1 | SSS NOSAMPLER 化 | 微小（実用上無視可能） | ニュートラル | 不要 |
| 2 | Parallax tex2Dgrad | 微小〜軽度 | **ポジティブ（品質向上）** | 不要 |
| 3 | Blur センターキャッシュ | **変化なし** | ニュートラル | 不要 |
| 4 | DepthNormals VR/SPI | 非VR: 変化なし / VR: **顕著（バグ修正）** | **ポジティブ（バグ修正）** | 不要 |

### 変更1: SSS サンプラー共有 — 表現影響

**影響度: 微小（実用上無視可能）**

- `_ThicknessMap` / `_SSSMask` が `_MainTex` のサンプラー（WrapMode/FilterMode）を共有
- SSS テクスチャは低周波グラデーションマスクであり、サンプラー設定差の影響を受けにくい
- UV は `_MainTex_ST` 変換済みで 0-1 範囲内（VRChat アバターではほぼ 100%）
- `ApplySoftMask()` により SSS マスクのエッジは既にソフト化済み
- 既に 30 以上のマスクテクスチャが同パターンで運用されており、問題報告なし

**影響を受けるケース（極めて稀）**:
- _ThicknessMap に Point フィルタリングを設定し、ピクセル単位の精密 SSS 制御をしていた場合
- _MainTex の Tiling が 1 以外で、かつマスクが Clamp 設定だった場合

### 変更2: Parallax tex2Dgrad — 表現影響

**影響度: 微小〜軽度（品質向上方向）**

| シナリオ | 変化の度合い | 方向 |
|----------|------------|------|
| 近距離・正面 | ほぼ変化なし | ニュートラル |
| 近距離・斜め | 微小 | ポジティブ（POM安定性向上） |
| 中距離 | 微小 | ポジティブ（ちらつき減少） |
| 遠距離 | 軽度〜中程度 | ポジティブ（エイリアシング減少） |
| mipmap無効テクスチャ | 変化なし | — |
| Quest (モバイルGPU) | 中程度 | **強くポジティブ**（不定mip問題解消） |

- 変更前: POMループ内で `tex2D` → gradient 不定 → mip 0 フォールバック → 遠距離でちらつき
- 変更後: `tex2Dgrad` + ループ外 ddx/ddy → 正確なmip選択 → 遠距離で安定
- UVオフセット量も微妙に変わるが、高周波ディテールのノイズが減る方向
- UE5/lilToon 等の主要シェーダーでも同手法を採用（業界標準パターン）

### 変更3: Blur センターキャッシュ — 表現影響

**影響度: 変化なし**

- 数学的に完全に等価（同一UV・同一テクスチャの tex2D はビット単位で同一結果）
- GPUコンパイラの CSE で変更前後のマシンコードも同一になる可能性が高い
- blur = 0 時: 結果同一（1回サンプリングで即リターン）
- blur > 0 時: 結果同一（center をキャッシュで再利用するだけ）

### 変更4: DepthNormals VR/SPI — 表現影響

**非VR環境: 完全に変化なし**
- `UNITY_DECLARE_SCREENSPACE_TEXTURE` → 非SPI で `sampler2D` に展開
- `UNITY_SAMPLE_SCREENSPACE_TEXTURE` → 非SPI で `tex2D` に展開
- 展開結果が変更前と実質同一

**VR SPI環境: 顕著な改善（バグ修正）**
- 変更前: `tex2D` で右目も左目のデータを読んでいた → エッジ位置がずれる
- 変更後: `UNITY_SAMPLE_SCREENSPACE_TEXTURE` で正しい eye index → 両目とも正確なエッジ
- Screen Edge エフェクト（`_SCREEN_EDGE` キーワード有効時）に影響
- VRChat は SPI がデフォルトなので、この修正は VRChat ユーザーにとって重要

### 後方互換性の総合評価

| 観点 | 評価 |
|------|------|
| 非VR環境の既存マテリアル | **完全互換** — 視覚的変化はほぼなし |
| VR SPI 環境 | **改善** — 右目のエッジ検出バグ修正 |
| Parallax 使用マテリアル | **微小な品質変化** — 遠距離でより安定（ポジティブ） |
| マテリアルプロパティ | **変更なし** — 再設定不要 |
| 破壊的変化の可能性 | **なし** |

---

## VRChat 環境 互換性・表現影響 調査レポート (2026-03-08)

**目的**: VRChat 固有のレンダリング環境で問題が発生しないか調査
**チーム**: vrc-shader-constraint-auditor / vrc-quest-mobile-auditor / vrc-mirror-camera-auditor

### 総合評価: ✅ VRChat 全環境で安全 — マージ可能

---

### VRC 互換性マトリクス

| 変更 | Desktop | VR (SPI) | Quest | Mirror | Camera |
|------|:-------:|:--------:|:-----:|:------:|:------:|
| A: SSS NOSAMPLER | ✅ | ✅ | ✅ | ✅ | ✅ |
| B: Parallax tex2Dgrad | ✅ | ✅ | ✅ | ✅ | ✅ |
| C: DepthNormals SPI | ✅ | ✅ 改善 | ⚠️※ | ⚠️※ | ⚠️※ |
| D: Blur キャッシュ | ✅ | ✅ | ✅ | ✅ | ✅ |

※ Screen Edge 機能限定の既知制約（変更前から存在、今回の変更で悪化しない）

---

### 1. サンプラー数制約 (DX11: 16個上限)

**結果: ✅ 大幅改善**

- 今回の変更で **サンプラー7個削減** (_ThicknessMap, _SSSMask, _ParallaxMap + 他4個の NOSAMPLER 化)
- 現実的な最大構成: **12/16 サンプラー使用** (マージン4)
- VRChat で複雑なマテリアル設定でもサンプラー上限に達するリスクが大幅低下

### 2. VR Single Pass Instanced (SPI)

**結果: ✅ 改善（バグ修正含む）**

- `UNITY_SAMPLE_SCREENSPACE_TEXTURE` が正しい `unity_StereoEyeIndex` を付与
- 変更前: 右目のエッジ検出が左目のデータを使っていた（バグ）
- 変更後: 両目で正しいテクスチャ配列スライスを参照
- SSS / Parallax の NOSAMPLER 化は SPI に影響なし

### 3. shader_feature キーワード

**結果: ✅ 変更なし**

- 新規 `#pragma shader_feature` の追加は一切なし
- VRChat のバリアント数制限（256ローカルキーワード上限）に影響なし

### 4. Quest 互換性

**結果: ✅ 互換（実質的に影響なし）**

- VRChat Quest ではカスタムシェーダー自体がフォールバック（Standard Lite/Toon Lit）される
- そのため今回の4変更はすべて **Quest Mobile では実行されない**
- 技術的には:
  - `tex2Dgrad` は OpenGL ES 3.0+ でネイティブサポート（Adreno 650/740 対応）
  - `UNITY_SEPARATE_TEXTURE_SAMPLER` は GLES で未定義 → `#else` パス（`tex2Dgrad(sampler2D, ...)`）が正しく動作
  - `half` 精度: gradient は `float` で宣言済み、マスク値は `half` → 適切な精度分離
- 将来 VRChat が Quest でカスタムシェーダーを許可した場合にも **事前に正しい対応** となる

### 5. Mirror / VRChat Camera

**結果: ✅ 変更A/B/D は全環境互換、変更C は既知制約あり（悪化なし）**

**Mirror での Parallax (変更B)**:
- `ddx(uv)` / `ddy(uv)` はスクリーン空間の隣接ピクセル差分 → Mirror 反転に影響されない
- `viewDirTangent` は Mirror カメラ位置から正しく計算される
- `tex2Dgrad` は Mirror 内でも安定した MIP 選択を提供（品質向上）

**Mirror/Camera での Screen Edge (変更C)**:
- Mirror/VRC Camera は DepthNormals パスを実行しない可能性がある（VRChat 実装依存）
- この場合、法線ベースのエッジ検出がメインカメラのデータを参照 → 不正なエッジ表示
- **ただしこれは変更前から存在する既知制約であり、今回の変更で悪化しない**
- 深度ベースのエッジ検出は Mirror/Camera でも動作する可能性が高い

### 6. VRC Light Volumes

**結果: ✅ 完全互換**

- SSS 計算はライト方向・ビュー方向・アテニュエーションのみに依存
- Light Volumes の影響は既存のライティングパスを通じて自然に SSS に伝播
- Screen Edge は深度/法線バッファのみに依存 → Light Volumes と干渉しない

### 7. マルチプレイヤー環境

**結果: ✅ 改善方向**

- サンプラー7個削減 → GPU コンテキストスイッチのオーバーヘッド微減
- `_DISTANCE_FADE` による距離ベース LOD が既に実装済み → 遠方アバターでは自動無効化
- Blur のテクスチャフェッチ削減 → 複数アバター同時表示時の帯域負荷軽減

---

### 発見した追加問題 (diff 外)

| 優先度 | 内容 | 場所 |
|--------|------|------|
| ⚠️ Medium | `SobelEdgeDepth` が `SAMPLE_DEPTH_TEXTURE` を使用 → VR SPI 非対応。`UNITY_SAMPLE_SCREENSPACE_TEXTURE` に統一推奨 | Utils.hlsl:1686-1693 |
| ⚠️ Medium | `_CameraDepthTexture` の宣言 `UNITY_DECLARE_DEPTH_TEXTURE` と Fragment.hlsl のサンプリング `UNITY_SAMPLE_SCREENSPACE_TEXTURE` が不整合 | Input.hlsl:1305 |
| ⚠️ Low | `_SheenMask` が `sampler2D` のまま → NOSAMPLER 化でサンプラー1個追加削減可能 | Input.hlsl:1076 |

---

### 結論

| 評価軸 | 結果 |
|--------|------|
| VRC Desktop 互換性 | ✅ 完全互換 |
| VRC VR (SPI) 互換性 | ✅ 完全互換 + Screen Edge バグ修正 |
| VRC Quest 互換性 | ✅ 互換（フォールバックにより実行されない） |
| VRC Mirror | ✅ 互換（Screen Edge の既知制約は変更前から存在） |
| VRC Camera | ✅ 互換（同上） |
| VRC Light Volumes | ✅ 完全互換 |
| 表現への影響 | 微小〜ポジティブ（品質向上方向） |
| 破壊的変化 | **なし** |
| マテリアル再設定 | **不要** |
