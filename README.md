# Natane Toon Shader

汎用的なセルルック/NPR調に対応したUnity Built-in Render Pipeline用のトゥーンシェーダーです。
lilToon、NovaShader、NiloToon、PoiyomiToon、YMToonなどの人気トゥーンシェーダーを参考に設計されています。

## ✨ 新機能

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
- **発光（Emission）** - HDRカラー対応の自己発光
- **アウトライン** - 法線押し出し方式のアウトライン（太さ、色調整可能）

### テクスチャ
- **メインテクスチャ** - アルベドマップ
- **法線マップ** - ノーマルマップ対応（強度調整可能）
- **発光マップ** - エミッションテクスチャ
- **ランプテクスチャ** - カスタムライティンググラデーション
- **Thicknessマップ** - SSS用の厚みマップ（白=薄い、黒=厚い）

## インストール

1. このリポジトリをクローンまたはダウンロード
2. `Assets` フォルダをUnityプロジェクトにコピー
3. マテリアルを作成し、Shader を `Natane/Toon Shader` に設定

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

## 更新履歴

### v1.3.0 (2025-10-29)
- **高度なライティング制御** 機能追加
  - Shadow Receive: 影の受け取り強度制御
  - Shadow Max Darkness: 影の濃さ上限設定
  - Light Min/Max Influence: 光の影響範囲制限
  - Backlight: バックライト（逆光）効果
- アーティスティックな制御を強化
- 全バリアント対応

### v1.2.0 (2025-10-29)
- **Subsurface Scattering (SSS)** 機能追加
  - 肌、耳、指などの透過表現
  - Thickness Map 対応
  - 強度、色、歪みを調整可能
- SSS 用の ShaderGUI 追加
- 全バリアント（Opaque/Cutout/Transparent）でSSS対応

### v1.1.0 (2025-10-29)
- HLSL形式への変換
- YMToon / MToon 移行ツール追加
- Cutout / Transparent バリアント追加
- GPU Instancing 対応
- VRChat 最適化

### v1.0.0 (2025-10-29)
- 初回リリース
- セルシェーディング、リムライト、MatCap、アウトライン、スペキュラ機能実装
- lilToon 移行ツール実装
- カスタムShaderGUI実装
- Built-in Render Pipeline対応
