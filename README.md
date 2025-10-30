# Natane Toon Shader

[![Version](https://img.shields.io/badge/version-1.00.1-blue)](https://github.com/natane010/natane_engine_Toon_Shader/releases/tag/v1.00.1)
[![Unity](https://img.shields.io/badge/Unity-2019.4+-black)](https://unity.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![VRC Light Volumes](https://img.shields.io/badge/VRC_Light_Volumes-対応-brightgreen)](https://github.com/REDSIM/VRCLightVolumes)

汎用的なセルルック/NPR調に対応したUnity Built-in Render Pipeline用のトゥーンシェーダーです。
lilToon、NovaShader、NiloToon、PoiyomiToon、YMToonなどの人気トゥーンシェーダーを参考に設計されています。

**完全日本語UI対応** - すべてのインスペクターとエディタウィンドウが日本語で利用可能

## 📌 ブランチ情報

- **v1.00.1** - 最新安定版（日本語UI完全対応 + VRC Light Volumes対応）
- **v1.00.0** - 初回安定版（日本語UI完全対応）

特定のバージョンをインストールする場合は、タグを使用してください（例：`#v1.00.1`）。

## ✨ 新機能

### 🌟 VRC Light Volumes 対応 v1.00.1（NEW!）
次世代ボクセルベースのライティングシステムに完全対応しました：

- **ボクセルベースライティング**: VRChatワールドで部分的な照明が可能に
- **自動フォールバック**: 非対応環境では自動的にUnityライトプローブを使用
- **カラースペキュラー**: Light Volumeからリアルなスペキュラーを生成
- **強度調整**: Light Volumeライティングの強度を自由に調整
- **完全日本語UI**: インスペクターとカスタムエディタが完全日本語化

#### VRC Light Volumes とは？
RED_SIMが開発したVRChat向けの次世代ライティングシステムで、従来のライトプローブよりも正確な部分照明を実現します。自動販売機の光が上半身だけを照らすなど、よりリアルな光の表現が可能になります。

**対応ワールドを探す**: ハッシュタグ `#VRCLightVolumesReady` で検索

### 🛠️ デザイナー支援ツール v1.11.0
非エンジニアのデザイナーでも安心して使える強力なツールセットを追加しました：

#### 1. マテリアルバリデーター
- **VRChat最適化チェック**: テクスチャサイズ・メモリ使用量を自動検証
- **パフォーマンス評価**: A/B/C/D評価でマテリアルの重さを可視化
- **自動修正機能**: ワンクリックで問題を自動修正
- **未使用機能検出**: 有効だが使っていない機能を自動検出
- メニュー: `Tools > Natane > Material Validator`

#### 2. インタラクティブヘルプシステム
- **24用語の用語集**: セルシェーディング、SSS、Fresnel、POMなど技術用語を平易に解説
- **6つのチュートリアル**: 初級〜上級まで段階的に学習
- **トラブルシューティング**: よくある問題の解決方法
- **25以上のTips**: パフォーマンス、ワークフロー、品質向上のコツ
- メニュー: `Tools > Natane > Interactive Help`

#### 3. バッチマテリアル処理
- **5つの操作モード**: パラメータ調整、色調整、テクスチャ置換、機能切替、バリアント変換
- **一括操作**: 数百のマテリアルを一度に編集
- **HSV色調整**: 色相シフト、彩度、明度の相対調整
- **Undo対応**: すべての操作を取り消し可能
- メニュー: `Tools > Natane > Batch Material Processor`

#### 4. マテリアルプレビューウィンドウ
- **リアルタイム3Dプレビュー**: マテリアルの見た目を即座に確認
- **5種類の形状**: 球、立方体、円柱、平面、トーラス
- **ライティング調整**: 環境光、ライト色、強度を自由に変更
- **インタラクティブ操作**: ドラッグで回転、スクロールでズーム
- メニュー: `Tools > Natane > Material Preview`

#### 5. カラーパレット管理
- **プロジェクト全体の色管理**: 統一された色使いを実現
- **名前付き色エントリ**: 色に名前と説明を付けて管理
- **一括適用**: 選択したマテリアル全てにパレット色を適用
- **チーム共有**: ScriptableObjectでチーム全体の色を統一
- メニュー: `Tools > Natane > Color Palette Manager`

#### 6. テクスチャオプティマイザー
- **自動最適化**: テクスチャを自動的にパフォーマンス最適化
- **プロジェクトスキャン**: 最適化が必要なテクスチャを自動検出
- **メモリ削減表示**: 最適化前後のメモリ使用量を可視化
- **一括処理**: 複数テクスチャを一度に最適化
- メニュー: `Tools > Natane > Texture Optimizer`

**デザイナーへのメリット:**
- 技術的な知識がなくても高品質なマテリアルを作成可能
- 自動検証で問題を未然に防止
- バッチ処理で作業時間を大幅短縮
- 学習リソースが充実、自己解決が容易

### HLSL対応
- モダンな `.hlsl` 形式でシェーダーコードを記述
- より良いパフォーマンスと互換性
- GPU Instancing 対応

### 複数の自動移行ツール
- **lilToon Migration Tool**: lilToonからの自動変換
- **YMToon Migration Tool**: YMToon/MToonからの自動変換（VRChat向け）
- **Batch Material Converter**: 汎用マテリアル変換ツール

### マルチバリアント対応
- **Opaque**: 標準的な不透明シェーダー
- **Cutout**: 透過切り抜き（髪の毛、葉っぱなど）
- **Transparent**: 半透明（ガラス、水など）

詳細は [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) を参照してください。

### 🎨 マテリアルプリセット & 共有システム（v1.10.0）
- **ビジュアルプリセットブラウザ**: サムネイル付きでプリセットを視覚的に選択
- **26種類のデフォルトプリセット**: キャラクター、小道具、環境、エフェクト
- **ワンクリック適用**: 初心者でもプロ品質のマテリアルを即座に作成
- **パラメータ共有**: ファイルまたはクリップボード経由で他のデザイナーと共有
- **プリセット作成**: 既存マテリアルから独自プリセットを簡単作成
- **パフォーマンス表示**: マテリアルの重さを視覚的に確認（A/B/C/D評価）
- **デザイナーフレンドリー**: 非エンジニアでも直感的に操作可能

## 特徴

### コア機能
- **セルシェーディング** - 段階的な影表現（ステップ数、シャープネス調整可能）
- **カスタム影色** - 影の色を自由にカスタマイズ
- **ライトランプテクスチャ** - グラデーションテクスチャでライティングを細かく制御
- **複数ライト対応** - ForwardBaseとForwardAddパスで複数光源に対応
- **環境光対応** - Unity の Ambient Color に対応
- **高度なライティング制御** - 影の受け取り強度、影の濃さ上限、光の影響範囲、バックライト

### エフェクト
- **リムライト** - エッジ発光効果（色、強度、範囲調整可能）
- **サブサーフェススキャッタリング（SSS）** - 肌や葉など薄い物体の透過表現（Thickness Map対応）
- **MatCap** - スフィアマップによる疑似反射・質感表現（Add/Multiply/Replaceブレンド）
- **スペキュラハイライト** - アニメ調のシャープなハイライト
- **発光（Emission）** - HDRカラー対応の自己発光（スクロール・脈動アニメーション対応）
- **アウトライン** - 法線押し出し方式のアウトライン（太さ、色調整可能）

### バーチャル表現（VRChat対応）
- **Dissolve（溶解効果）** - アバターの出現/消失演出、境界発光エフェクト付き
- **Hue Shift（色相変更）** - リアルタイムでの色変更、服装変更ギミックに最適
- **Emission Animation** - 発光のスクロール・脈動アニメーション

### マスクテクスチャ対応
- **エフェクト別マスクテクスチャ** - 各エフェクトをピクセル単位で制御
  - Specular Mask - スペキュラハイライトの制御
  - Rim Mask - リムライトの制御
  - SSS Mask - サブサーフェススキャッタリングの制御
  - MatCap Mask - MatCapエフェクトの制御
  - Emission Mask - 発光の制御
  - Dissolve Mask - 溶解効果の制御
- 白色（1.0）= 完全適用、黒色（0.0）= 適用なし
- グレースケールで部分的な制御が可能

### テクスチャ
- **メインテクスチャ** - アルベドマップ
- **法線マップ** - ノーマルマップ対応（強度調整可能）
- **発光マップ** - エミッションテクスチャ
- **ランプテクスチャ** - カスタムライティンググラデーション
- **Thicknessマップ** - SSS用の厚みマップ（白=薄い、黒=厚い）
- **Dissolveテクスチャ** - 溶解効果用のノイズテクスチャ
- **マスクテクスチャ** - 各エフェクト用のマスク（グレースケール、赤チャンネル使用）

## インストール

### Unity Package Manager（推奨）

**Git URLからインストール:**

1. Unityプロジェクトを開く
2. メニューから **Window** → **Package Manager** を選択
3. 左上の **+** ボタンをクリック
4. **Add package from git URL...** を選択
5. 以下のURLを入力して **Add** をクリック:
   ```
   https://github.com/natane010/natane_engine_Toon_Shader.git
   ```

**特定のバージョンをインストール:**

タグまたはブランチを指定することで特定のバージョンをインストールできます:

**タグ指定（推奨）:**
```
https://github.com/natane010/natane_engine_Toon_Shader.git#v1.11.2
```

**ブランチ指定:**
```
https://github.com/natane010/natane_engine_Toon_Shader.git#release/v1.11.2
```

### 手動インストール

1. [Releases](https://github.com/natane010/natane_engine_Toon_Shader/releases)から最新版をダウンロード
2. ダウンロードしたzipファイルを解凍
3. 解凍したフォルダをUnityプロジェクトの `Packages` ディレクトリに配置
   - または、`Assets` フォルダにコピーすることも可能
4. マテリアルを作成し、Shader を `Natane/Toon Shader` に設定

## パッケージ構造

Unity Package Manager互換の標準構造を採用しています：

```
Packages/com.natane.toonshader/
├── package.json                                   # パッケージマニフェスト
├── Editor/                                        # エディタースクリプト
│   ├── NataneToon/
│   │   ├── NataneToonShaderGUI.cs                # カスタムインスペクター
│   │   ├── MaterialValidator.cs                   # マテリアル検証ツール
│   │   ├── BatchMaterialProcessor.cs              # バッチ処理ツール
│   │   ├── InteractiveHelpSystem.cs               # ヘルプシステム
│   │   ├── MaterialPresetBrowser.cs               # プリセットブラウザ
│   │   ├── ColorPaletteManager.cs                 # カラーパレット管理
│   │   ├── TextureOptimizer.cs                    # テクスチャ最適化
│   │   ├── ShaderVariantCollector.cs              # Variant収集ツール
│   │   ├── ShaderPrewarmingEditor.cs              # Shader事前ウォーミング
│   │   └── MigrationTools/
│   │       ├── LilToonMigrationTool.cs            # lilToon変換ツール
│   │       ├── YMToonMigrationTool.cs             # YMToon変換ツール
│   │       └── BatchMaterialConverter.cs          # 汎用変換ツール
│   └── ParticleSystem/
│       └── ParticleEffectEditorWindow.cs          # パーティクルエディタ
├── Runtime/                                       # ランタイムスクリプト
│   ├── MaterialSystem/
│   │   ├── NataneToonMaterialPreset.cs           # マテリアルプリセット
│   │   ├── MaterialParameterShareSystem.cs        # パラメータ共有
│   │   └── ColorPalette.cs                        # カラーパレット
│   └── ParticleSystem/
│       ├── ParticleEffectPreset.cs                # パーティクルプリセット
│       ├── ParticleEffectSpawner.cs               # パーティクルスポナー
│       └── ParticleAutoDestroy.cs                 # 自動破棄
├── Shaders/                                       # シェーダー
│   └── NataneToon/
│       ├── NataneToonShader.shader                # メインシェーダー（Opaque）
│       ├── Variants/
│       │   ├── NataneToonShader_Cutout.shader    # 透過切り抜き
│       │   └── NataneToonShader_Transparent.shader # 半透明
│       └── Include/
│           ├── NataneToonCore.hlsl                # コア統合
│           ├── NataneToonInput.hlsl               # プロパティ定義
│           ├── NataneToonLighting.hlsl            # ライティング
│           ├── NataneToonUtils.hlsl               # ユーティリティ
│           ├── NataneToonVertex.hlsl              # 頂点シェーダー
│           └── NataneToonFragment.hlsl            # フラグメント
└── ShaderVariants/
    └── NataneToonShaderVariants.shadervariants   # Variant Collection
```

### モジュール構成の利点

- **NataneToonInput.hlsl**: プロパティと構造体を一元管理。新しいパラメータの追加が容易
- **NataneToonLighting.hlsl**: ライティング関数を分離。新しいシェーディング手法の追加が簡単
- **NataneToonUtils.hlsl**: 汎用的なユーティリティ関数。他のシェーダーでも再利用可能
- **NataneToonVertex.hlsl**: 頂点処理を独立化。頂点変形などのカスタマイズが容易
- **NataneToonFragment.hlsl**: フラグメント処理を独立化。エフェクトの追加・変更が明確

この構造により、特定の機能の修正や拡張時に関連ファイルのみを編集すれば良く、コードの保守性が大幅に向上しています。

## 🎨 マテリアルプリセットの使い方（デザイナー向け）

Natane Toon Shaderは、非エンジニアのデザイナーでも簡単に使えるプリセットシステムを提供しています。

### デフォルトプリセットの生成

初回セットアップ：

1. `Tools > Natane > Generate Default Presets` を実行
2. 26種類のプリセットが `Assets/MaterialPresets` に生成されます
3. プリセットブラウザで確認できます

**含まれるプリセット:**
- **キャラクター**: 肌（柔らか/リアル）、髪（標準/光沢）、服（布/革）、目
- **小道具**: 金属（光沢/ブラシ仕上げ）、プラスチック（光沢/マット）、木材、布
- **環境**: 草、葉、石、コンクリート
- **エフェクト**: ガラス（透明/曇り）、水、発光、ネオン、ホログラム

### プリセットブラウザの使い方

1. **ブラウザを開く**:
   - `Tools > Natane > Material Preset Browser` を実行

2. **マテリアルを選択**:
   - Projectウィンドウからマテリアルをドラッグ＆ドロップ
   - または"Target Material"フィールドをクリックして選択

3. **プリセットを適用**:
   - サムネイル付きでプリセットが表示されます
   - カテゴリフィルタで絞り込み可能
   - 検索バーで名前検索
   - プリセットの"Apply"ボタンをクリック

### マテリアルインスペクターから直接操作

マテリアルを選択してインスペクターを見ると：

1. **Material Presets & Sharing** セクションが表示されます
2. **Preset Browser**: プリセットブラウザを開く
3. **Save as Preset**: 現在の設定からプリセット作成
4. **Export to File**: パラメータをファイルに保存（.ntmaterial形式）
5. **Copy/Paste**: クリップボード経由でコピー＆ペースト

### パラメータの共有方法

#### 方法1: ファイル共有（推奨）

**エクスポート:**
1. マテリアルインスペクターで"Export to File"をクリック
2. 保存場所とファイル名を指定
3. `.ntmaterial` ファイルが生成されます

**インポート:**
1. マテリアルプリセットブラウザを開く
2. 対象マテリアルを選択
3. "Import from File"をクリック
4. `.ntmaterial` ファイルを選択

#### 方法2: クリップボード共有（高速）

**コピー:**
1. マテリアルインスペクターで"Copy"をクリック
2. パラメータがクリップボードにコピーされます

**ペースト:**
1. 別のマテリアルを選択
2. "Paste"ボタンをクリック
3. 確認ダイアログで内容を確認して"Paste"

### プリセットの作成

独自プリセットを作成する方法：

1. **既存マテリアルから作成**:
   - マテリアルインスペクターで"Save as Preset"をクリック
   - プリセット名とカテゴリを設定
   - サムネイル画像を追加（オプション）
   - 説明文を記入（オプション）

2. **ゼロから作成**:
   - プリセットブラウザで"Create Preset"をクリック
   - プリセットアセットが作成されます
   - インスペクターで詳細を編集

### パフォーマンス表示

マテリアルインスペクターの**Performance**セクションで：

- **Excellent (A)**: 3機能以下 - とても軽量
- **Good (B)**: 4-6機能 - 標準的な負荷
- **Fair (C)**: 7-9機能 - やや重い
- **Heavy (D)**: 10機能以上 - 重い

使用していない機能はOFFにしてパフォーマンスを向上させましょう。

### ヒントとコツ

- **学習教材として**: プリセットを適用した後、パラメータを見て設定を学べます
- **ベースとして使用**: プリセットを適用してから微調整するのが効率的
- **チーム共有**: プロジェクトカラーやスタイルをプリセット化してチーム共有
- **バックアップ**: 重要な設定は必ずプリセットやファイルに保存

## 他のシェーダーからの移行

既にlilToonやYMToonを使用している場合、自動移行ツールを使用できます：

### lilToonから
1. `Tools > Natane > lilToon Migration Tool` を開く
2. `Scan for lilToon Materials` をクリック
3. `Convert All Materials` をクリック

### YMToonから（VRChat向け）
1. `Tools > Natane > YMToon Migration Tool` を開く
2. `Scan for YMToon Materials` をクリック
3. バリアント（Opaque/Cutout/Transparent）を確認
4. `Convert All Materials` をクリック

詳細は [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) を参照してください。

## Shader Variant 最適化

Natane Toon Shaderは多機能なため、多数のshader variantsが生成されます。ビルドサイズとロード時間を最適化するため、ShaderVariantCollectionの使用を推奨します。

### ShaderVariantCollectionの作成

1. **エディタツールを使用（推奨）**:
   - `Tools > Natane > Shader Variant Collector` を開く
   - "Create New Collection" をクリック
   - 必要なオプションを選択して "Collect Variants" をクリック

2. **推奨設定**:
   - **Basic Variants**: 基本的な組み合わせ（必須）
   - **Advanced Variants**: 高度な機能（推奨）
   - **Virtual Expression**: VRChat向け機能（VRChat使用時のみ）
   - **All Combinations**: テストのみ（⚠️ 本番ビルドでは非推奨）

### Prewarmingの設定

エディター専用の自動プリウォームシステムを使用します（**VRChat対応 - ランタイムスクリプト不要**）：

1. **自動設定（推奨）**:
   - ビルド時に自動的にシェーダーをプリウォーム
   - `Tools > Natane > Shader Prewarming > Settings` で設定変更可能
   - デフォルトで有効

2. **手動実行**:
   - `Tools > Natane > Shader Prewarming > Prewarm All Shaders` を実行
   - または `Tools > Natane > Shader Prewarming > Prewarm Shader Variant Collection` を実行

3. **動作仕様**:
   - エディター内でのみ動作（ランタイムでは実行されない）
   - VRChat SDKのビルド前に自動実行
   - シーン内のすべてのNatane Toonマテリアルを自動検出

4. **ランタイムプリウォーム（非VRChatのみ）**:
   - VRChat以外のプロジェクトでランタイムプリウォームが必要な場合
   - `Tools > Natane > Shader Prewarming > Settings` を開く
   - "Runtime Prewarming (Non-VRChat Only)" セクションで "Generate Runtime Prewarming Script" をクリック
   - 生成された `RuntimeShaderPrewarming.cs` をGameObjectにアタッチ
   - ⚠️ **警告**: VRChatでは動作しません

### 効果

- ✅ ビルドサイズを50-80%削減
- ✅ ロード時間を大幅に短縮
- ✅ 実行時のスタッター（カクつき）を防止

詳細は [SHADER_VARIANTS.md](SHADER_VARIANTS.md) を参照してください。

## 使い方

### 基本的な使い方

1. **マテリアルの作成**
   - Project ウィンドウで右クリック → Create → Material
   - Shader ドロップダウンから `Natane/Toon Shader` を選択

2. **基本設定**
   - Main Texture: キャラクターのアルベドテクスチャを設定
   - Color: 全体的な色調整

3. **シェーディング設定**
   - Shadow Steps: 影のステップ数（2-3が一般的）
   - Shadow Sharpness: 影の境界のシャープさ
   - Shadow Color: 影の色（青みがかった影などが可能）
   - Shadow Offset: 影の位置調整

### 機能別ガイド

#### セルシェーディング（基本）
```
Shadow Steps: 2-3
Shadow Sharpness: 0.05-0.1
Shadow Color: 青みがかったグレー（例: RGB 0.5, 0.5, 0.6）
```

#### ライトランプテクスチャ
1. `Use Ramp Texture` にチェック
2. Ramp Texture にグラデーションテクスチャを設定
   - 左側が暗部、右側が明部
   - 横長のテクスチャ（例: 256x4ピクセル）

#### リムライト
1. `Enable Rim Light` にチェック
2. Rim Color: リムの色（明るい色推奨）
3. Rim Power: リムの範囲（3-5が一般的）
4. Rim Intensity: リムの強さ

#### MatCap
1. `Enable MatCap` にチェック
2. MatCap Texture: 球体マップテクスチャを設定
3. MatCap Blend Mode:
   - Add: 加算合成（光沢感）
   - Multiply: 乗算合成（陰影）
   - Replace: 置き換え（金属質感）

#### アウトライン
1. `Enable Outline` にチェック
2. Outline Width: 0.005-0.02が一般的
3. Outline Color: 通常は黒または濃い色

#### スペキュラハイライト
1. `Enable Specular` にチェック
2. Specular Size: ハイライトのサイズ
3. Specular Softness: ハイライトのぼかし具合

#### サブサーフェススキャッタリング（SSS）
1. `Enable SSS` にチェック
2. SSS Color: 透過光の色（肌の場合は赤みがかった色）
3. SSS Intensity: 透過の強さ（0.5-1.5が一般的）
4. SSS Power: 透過の範囲（3-5が一般的）
5. SSS Distortion: 透過の歪み（0.3-0.7が一般的）
6. Use Thickness Map: オプション（白=薄い、黒=厚い）
7. Thickness Scale: 厚みの倍率

**用途**: 肌、耳、指、葉っぱ、紙など薄い素材

#### 高度なライティング制御
1. **Shadow Receive** (0-1): 他のオブジェクトからの影の受け取り強度
   - 1 = 完全に影を受ける
   - 0 = 影を全く受けない
   - 用途: キャラクターが影で暗くなりすぎるのを防ぐ

2. **Shadow Max Darkness** (0-1): 影の最大の暗さ
   - 0 = 完全に黒
   - 1 = 影が明るい（暗くならない）
   - 推奨値: 0.2-0.4 （影が真っ黒にならない）

3. **Light Min/Max Influence**: 光の明るさの範囲制御
   - Min: 最低限の明るさ（暗すぎを防ぐ）
   - Max: 最大の明るさ（明るすぎを防ぐ）
   - 推奨値: Min 0, Max 2

4. **Backlight Intensity** (0-2): バックライト（逆光）の強さ
   - 光が背後にある時の明るさ
   - リムライトに似た効果
   - **Backlight Color**: バックライトの色

5. **Additional Light Intensity** (0-1): 追加ライトの影響度
   - ForwardAdd パス（追加ライト）での明るさの倍率
   - 複数のライトがある場合の明るくなりすぎを防ぐ
   - 0 = 追加ライトの影響なし
   - 0.5 = 追加ライトの影響を半分に
   - 1 = 追加ライトの影響を100%受ける
   - 推奨値: 0.3-0.5 （ライトの数によらず一定の明るさを保つ）
   - 用途: 複数のライトを使用する環境で、明るくなりすぎるのを防ぐ

#### マスクテクスチャの使用方法

各エフェクトには対応するマスクテクスチャを設定できます：

**1. マスクテクスチャの準備**
- グレースケール画像を用意（赤チャンネルのみ使用）
- 白色（RGB 255,255,255）= エフェクト100%適用
- 黒色（RGB 0,0,0）= エフェクト適用なし
- グレー（中間値）= 部分的な適用

**2. マスクの適用**
1. エフェクトを有効化（例: `Enable Specular`）
2. `Use [Effect] Mask` にチェック
3. マスクテクスチャを設定

**使用例**:
- **Specular Mask**: 目や唇だけにハイライトを適用
- **Rim Mask**: 髪や服にのみリムライトを適用
- **SSS Mask**: 肌や耳にのみSSSを適用
- **MatCap Mask**: 髪の特定部分にのみMatCapを適用
- **Emission Mask**: 発光パターンの制御
- **Dissolve Mask**: 溶解効果の領域制御

#### バーチャル表現（VRChat向け）

**1. Dissolve（溶解効果）**
- `Enable Dissolve` にチェック
- **Dissolve Amount** (0-1): 溶解の進行度
  - 0 = 完全に表示
  - 1 = 完全に消失
  - アニメーションで制御して出現/消失演出を作成
- **Dissolve Texture**: ノイズテクスチャを設定（白黒画像）
  - Perlin NoiseやVoronoiノイズが最適
- **Dissolve Edge Width** (0-0.5): 境界線の幅
  - 0.05-0.15 が一般的
- **Dissolve Edge Color**: 境界線の発光色
  - HDRカラー対応、明るい色で発光効果
- **Dissolve Edge Intensity** (0-10): 境界線の発光強度
  - 1-3 が一般的、5以上で非常に強い発光

**使用例**: VRChatアバターの登場/退場アニメーション、変身エフェクト

**2. Hue Shift（色相変更）**
- `Enable Hue Shift` にチェック
- **Hue Shift** (0-1): 色相のシフト量
  - 0 = 変更なし
  - 0.5 = 補色（反対色）
  - 1 = 1回転（元の色に戻る）
  - アニメーションで制御して色変更ギミックを作成

**使用例**: VRChatアバターの服装色変更、カラーバリエーション、魔法エフェクト

**3. Emission Animation（発光アニメーション）**
- `Enable Emission` にチェック後、以下を設定：

- **Emission Scroll**: 発光テクスチャのスクロール
  - `Emission Scroll` にチェック
  - `Scroll Speed`: スクロール速度（1 = 標準、2 = 2倍速）
  - 用途: 流れる光のエフェクト、サイバー感

- **Emission Pulse**: 発光の脈動
  - `Emission Pulse` にチェック
  - `Pulse Speed`: 脈動速度（1 = 1秒1回、2 = 2倍速）
  - `Pulse Amplitude` (0-1): 脈動の振幅（0.5 = 50%明滅）
  - 用途: 心臓の鼓動、呼吸、警告灯

## フォルダ構造

```
Assets/
├── Shaders/
│   ├── NataneToonShader.shader            # メインシェーダー（Opaque）
│   ├── NataneToonShader_Cutout.shader     # Cutoutバリアント
│   ├── NataneToonShader_Transparent.shader # Transparentバリアント
│   └── Include/
│       └── NataneToonCore.hlsl             # コア機能実装（HLSL）
├── Editor/
│   ├── NataneToonShaderGUI.cs              # カスタムインスペクタGUI
│   ├── LilToonMigrationTool.cs             # lilToon移行ツール
│   ├── YMToonMigrationTool.cs              # YMToon移行ツール
│   └── BatchMaterialConverter.cs           # 汎用マテリアル変換ツール
├── Materials/
│   └── Examples/                            # サンプルマテリアル用
└── Textures/
    └── Ramps/                               # ランプテクスチャ用
```

## 技術仕様

### シェーダーパス
1. **OUTLINE** - アウトライン描画（Cull Front）
2. **FORWARD_BASE** - メインライティング、環境光、追加エフェクト
3. **FORWARD_ADD** - 追加ライト（加算ブレンド）
4. **SHADOW_CASTER** - シャドウキャスター

### シェーダーキーワード
- `_USE_RAMP` - ランプテクスチャ使用
- `_SPECULAR` - スペキュラハイライト
- `_RIM_LIGHT` - リムライト
- `_MATCAP` - MatCap
- `_OUTLINE` - アウトライン
- `_EMISSION` - 発光
- `_NORMALMAP` - 法線マップ

### パフォーマンス

- **軽量**: 基本機能のみ使用時は非常に高速
- **スケーラブル**: 必要な機能のみ有効化してパフォーマンス調整可能
- **モバイル対応**: Built-in Pipeline対応でモバイルデバイスでも動作

## 参考にしたシェーダー

- **lilToon** - 多機能で軽量なトゥーンシェーダー
- **NovaShader** - シンプルで高速なトゥーンシェーダー
- **NiloToon** - モバイル最適化されたトゥーンシェーダー
- **PoiyomiToon** - 非常に多機能なトゥーンシェーダー
- **YMToon** - VRChat向けに最適化された軽量トゥーンシェーダー

## Tips

### 美しいセルシェーディングのコツ
1. Shadow Steps は 2-3 に設定
2. Shadow Color は完全な黒ではなく、少し彩度のある色を使用
3. Shadow Offset で影の位置を微調整

### アニメ調の質感
1. Specular を有効にしてサイズを小さく（0.05-0.15）
2. Rim Light で輪郭を強調
3. MatCap で髪の光沢感を追加

### ゲーム向け最適化
1. 不要な機能は無効化
2. Normal Map は必要な場合のみ使用
3. Outline Width は控えめに（描画負荷軽減）

## トラブルシューティング

### アウトラインが表示されない
- `Enable Outline` がチェックされているか確認
- Outline Width を大きくしてみる
- モデルの法線が正しいか確認

### 影が表示されない
- Directional Light が配置されているか確認
- Shadow Steps を2以上に設定
- Shadow Sharpness を調整

### MatCapが正しく表示されない
- MatCap Texture が正しく設定されているか確認
- テクスチャが球体マップ形式か確認

## ライセンス

MIT License

## 貢献

プルリクエストやイシューの報告を歓迎します！

## パーティクルシステム 🎆

Unityのパーティクルシステムを使って簡単にエフェクトを作成できるエディタ拡張が含まれています。

### 主な機能
- **ビジュアルエディタ**: `Tools > Natane > Particle Effect Editor`
- **プリセットシステム**: 再利用可能なエフェクト設定
- **7種類のテンプレート**: 爆発、炎、煙、魔法、電撃、水、回復
- **リアルタイムプレビュー**: 変更を即座に確認
- **簡単なスポーン**: スクリプトから簡単に生成

詳細は [PARTICLE_SYSTEM_GUIDE.md](PARTICLE_SYSTEM_GUIDE.md) を参照してください。

## 更新履歴

### v1.00.1 (2025-10-31)
**VRC Light Volumes対応 + 完全日本語UI**

#### 新機能
- **🌟 VRC Light Volumes完全対応**
  - ボクセルベースの次世代ライティングシステム実装
  - 自動フォールバック機能（非対応環境ではUnityライトプローブを使用）
  - Light Volumeスペキュラー生成（R/G/B方向別カラースペキュラー）
  - 強度調整可能（0-1）
  - VRChatワールドで部分的な照明が可能に

- **🎌 完全日本語UI対応**
  - すべてのインスペクターセクションを日本語化（14セクション）
  - すべてのカスタムエディタウィンドウを日本語化（9ツール）
  - プロパティラベル、ヘルプテキストを完全日本語化
  - 技術用語の説明も日本語で提供

#### 技術仕様
- 球面調和関数(SH)による正確な環境ライティング
- フォールバックシステムの自動実装
- シェーダーキーワード: `_USE_LIGHT_VOLUME`, `_LIGHT_VOLUME_SPECULAR`

### v1.0.0 (2025-10-30)
**初回安定版リリース（日本語UIフル対応）**

#### 主要機能
- **🎨 セルシェーディング/NPR機能**
  - ステップ数・シャープネス調整可能なトゥーンシェーディング
  - カスタムランプテクスチャ対応
  - Subsurface Scattering (SSS)
  - リムライト、MatCap、スペキュラー
  - アウトライン（反転ハル方式）
  - エミッション（スクロール・パルスアニメーション対応）

- **🌈 バーチャル表現（VRChat向け）**
  - Dissolve（溶解効果）- 出現/消失演出
  - Hue Shift（色相変更）- リアルタイム色変更
  - 境界線発光エフェクト（HDR対応）

- **🏞️ 背景・環境対応**
  - Cubemap Reflection（環境マッピング）
  - Environmental Rim（環境リム）
  - Parallax Mapping（視差マッピング、POM実装）
  - Refraction（屈折効果）

- **🎭 マスクテクスチャシステム**
  - 全エフェクトにマスク対応（Specular/Rim/SSS/MatCap/Emission/Dissolve）
  - ピクセル単位でのエフェクト制御

- **🛠️ デザイナー支援ツール**
  - **Material Validator**: VRChat最適化チェック、A/B/C/D評価
  - **Interactive Help System**: 24用語集、6チュートリアル、トラブルシューティング
  - **Batch Material Processor**: 5つの操作モード、一括編集機能
  - **Material Preview Window**: リアルタイム3Dプレビュー
  - **Color Palette Manager**: プロジェクト全体の色統一
  - **Texture Optimizer**: 自動テクスチャ最適化

- **📦 マテリアルプリセットシステム**
  - 26種類のデフォルトプリセット
  - ビジュアルプリセットブラウザ
  - JSON形式でのインポート/エクスポート
  - クリップボード共有対応

- **⚡ パフォーマンス最適化**
  - Shader Variant最適化システム
  - Shader Prewarming（エディター専用、VRChat対応）
  - GPU Instancing対応
  - 未使用機能の自動除外

- **🔄 移行ツール**
  - lilToon自動移行ツール
  - YMToon/MToon移行ツール
  - バッチマテリアル変換ツール

- **🎮 パーティクルシステム**
  - ParticleEffectPresetシステム
  - 7種類のプリセットテンプレート
  - リアルタイムプレビュー機能

#### 技術仕様
- Unity 2019.4以上対応
- Built-in Render Pipeline
- 3つのシェーダーバリアント（Opaque/Cutout/Transparent）
- HLSL形式のモジュール化されたコード構成
- MIT License
