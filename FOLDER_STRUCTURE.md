# 📁 Folder Structure

このドキュメントは、Natane Toon Shaderのフォルダ構造と各ファイルの役割を説明します。

## 🎨 Shaders フォルダ

```
Shaders/NataneToon/
├── NataneToonShader.shader           # メインシェーダー (Opaque)
├── Variants/                          # シェーダーバリアント
│   ├── NataneToonShader_Cutout.shader       # アルファカットアウト用
│   └── NataneToonShader_Transparent.shader  # 半透明用
└── Include/                           # インクルードファイル（機能別に整理）
    ├── Core/                          # 基本定義・構造
    │   ├── NataneToonInput.hlsl            # プロパティとデータ構造の定義
    │   ├── NataneToonCore.hlsl             # メインインクルードファイル（すべてのモジュールを統合）
    │   └── NataneToonVertex.hlsl           # 頂点シェーダー
    ├── Lighting/                      # ライティング計算
    │   └── NataneToonLighting.hlsl         # トゥーンシェーディング、グラデーションなど
    ├── Rendering/                     # フラグメントシェーダー
    │   └── NataneToonFragment.hlsl         # ピクセルレンダリングロジック
    └── Utils/                         # ユーティリティ関数
        └── NataneToonUtils.hlsl            # 色変換、マスク、トーンマッピングなど
```

### 📝 各ファイルの役割

#### Core/
- **NataneToonInput.hlsl**: すべてのシェーダープロパティ、サンプラー、構造体を定義
- **NataneToonCore.hlsl**: すべてのモジュールを統合するメインインクルードファイル
- **NataneToonVertex.hlsl**: 頂点変換、法線計算、アウトライン生成

#### Lighting/
- **NataneToonLighting.hlsl**: トゥーンシェーディング、グラデーションシェーディング、スペキュラー、リムライト、SSSなどのライティング計算

#### Rendering/
- **NataneToonFragment.hlsl**: メインのフラグメントシェーダー。テクスチャサンプリング、ライティング適用、エフェクト合成、最終カラー出力

#### Utils/
- **NataneToonUtils.hlsl**: HSV変換、マスクスムージング、トーンマッピング、ブレンドモードなどのユーティリティ関数

## 🔧 Editor フォルダ

```
Editor/NataneToon/
├── GUI/                               # ShaderGUIとヘルプシステム
│   ├── NataneToonShaderGUI.cs              # カスタムマテリアルインスペクター
│   ├── NataneToonShaderGUIUtility.cs       # GUI共通ユーティリティ
│   └── InteractiveHelpSystem.cs            # インタラクティブヘルプシステム
├── Presets/                           # プリセット管理
│   ├── NataneToonMaterialPresetEditor.cs   # プリセットエディタ
│   ├── MaterialPresetBrowser.cs            # プリセットブラウザ
│   ├── DefaultPresetGenerator.cs           # デフォルトプリセット生成
│   └── ColorPaletteManager.cs              # カラーパレット管理
├── Tools/                             # 各種ツール
│   ├── BatchMaterialProcessor.cs           # バッチマテリアル処理
│   ├── MaterialValidator.cs                # マテリアル検証
│   ├── MaterialPreviewWindow.cs            # プレビューウィンドウ
│   ├── SceneMaterialEditor.cs              # シーンマテリアル編集
│   ├── TextureOptimizer.cs                 # テクスチャ最適化
│   ├── ShaderPrewarmingEditor.cs           # シェーダープリウォーミング
│   └── ShaderVariantCollector.cs           # バリアントコレクション
└── Migration/                         # マイグレーションツール
    ├── BatchMaterialConverter.cs           # バッチ変換ツール
    ├── LilToonMigrationTool.cs             # lilToonからの移行
    └── YMToonMigrationTool.cs              # YMToonからの移行

Editor/PresetGenerators/
└── VTuberPresetGenerator.cs           # VTuber向けプリセット生成

Editor/ParticleSystem/
└── ParticleEffectEditorWindow.cs     # パーティクルエフェクトエディタ
```

### 📝 各カテゴリの役割

#### GUI/
マテリアルインスペクターのカスタムUI、タブナビゲーション、ヘルプシステム、プロパティ描画

#### Presets/
マテリアルプリセットの作成、保存、読み込み、管理。キャラクター向けプリセット、カラーパレット管理

#### Tools/
開発効率化ツール。バッチ処理、最適化、検証、プレビュー、シーン編集

#### Migration/
他のシェーダーシステムからの移行を支援するツール

## 🔄 インクルード構造

```
NataneToonShader.shader
└── Include/Core/NataneToonCore.hlsl
    ├── Core/NataneToonInput.hlsl
    ├── Utils/NataneToonUtils.hlsl
    ├── Lighting/NataneToonLighting.hlsl
    ├── Core/NataneToonVertex.hlsl
    └── Rendering/NataneToonFragment.hlsl
```

## 💡 開発ガイドライン

### 新しいエフェクトを追加する場合:
1. **Utils/** に必要なユーティリティ関数を追加
2. **Input.hlsl** にプロパティを定義
3. **Fragment.hlsl** または **Lighting.hlsl** にロジックを実装
4. **GUI/** の ShaderGUI にUI要素を追加

### 新しいライティングモデルを追加する場合:
1. **Lighting/NataneToonLighting.hlsl** に関数を追加
2. **Fragment.hlsl** から呼び出す
3. **GUI/** でパラメータUIを追加

### 新しいエディタツールを追加する場合:
1. 機能に応じて **GUI/**, **Presets/**, **Tools/** のいずれかに配置
2. 命名規則: `[機能名][Editor|Tool|Manager].cs`

## 📊 ファイルサイズの目安

- **Fragment.hlsl**: ~600行（メインレンダリングロジック）
- **Lighting.hlsl**: ~300行（ライティング計算）
- **Utils.hlsl**: ~400行（ユーティリティ関数）
- **Input.hlsl**: ~230行（プロパティ定義）
- **ShaderGUI.cs**: ~2000行（インスペクターUI）

## 🔍 トラブルシューティング

### シェーダーコンパイルエラー
1. **Core/NataneToonCore.hlsl** のインクルード順序を確認
2. 相対パスが正しいか確認（`Core/`, `Utils/`, `Lighting/`, `Rendering/`）

### エディタスクリプトが見つからない
1. **namespace** が正しいか確認（`NataneToon.Editor`）
2. **Assembly Definition** が正しく参照されているか確認

## 🎯 メンテナンス

### 定期的に確認すべきこと:
- [ ] 使用されていないプロパティの削除
- [ ] コメントの更新
- [ ] ユーティリティ関数の重複チェック
- [ ] GUI要素の一貫性確認

---

**Last Updated**: 2025-11-06
**Shader Version**: v1.1.2+
