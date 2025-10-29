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
- **発光（Emission）** - HDRカラー対応の自己発光（スクロール・脈動アニメーション対応）
- **アウトライン** - 法線押し出し方式のアウトライン（太さ、色調整可能）

### バーチャル表現（VRChat対応）
- **Dissolve（溶解効果）** - アバターの出現/消失演出、境界発光エフェクト付き
- **Hue Shift（色相変更）** - リアルタイムでの色変更、服装変更ギミックに最適
- **Emission Animation** - 発光のスクロール・脈動アニメーション

### テクスチャ
- **メインテクスチャ** - アルベドマップ
- **法線マップ** - ノーマルマップ対応（強度調整可能）
- **発光マップ** - エミッションテクスチャ
- **ランプテクスチャ** - カスタムライティンググラデーション
- **Thicknessマップ** - SSS用の厚みマップ（白=薄い、黒=厚い）
- **Dissolveテクスチャ** - 溶解効果用のノイズテクスチャ

## インストール

1. このリポジトリをクローンまたはダウンロード
2. `Assets` フォルダをUnityプロジェクトにコピー
3. マテリアルを作成し、Shader を `Natane/Toon Shader` に設定

## プロジェクト構造

プロジェクトは機能ごとにモジュール化された構造になっており、拡張性と可読性を重視しています：

```
Assets/
├── Shaders/
│   └── NataneToon/
│       ├── NataneToonShader.shader              # メインシェーダー（Opaque）
│       ├── Variants/
│       │   ├── NataneToonShader_Cutout.shader   # 透過切り抜きバリアント
│       │   └── NataneToonShader_Transparent.shader  # 半透明バリアント
│       └── Include/
│           ├── NataneToonCore.hlsl              # コア統合ファイル
│           ├── NataneToonInput.hlsl             # プロパティと構造体定義
│           ├── NataneToonLighting.hlsl          # ライティング計算関数
│           ├── NataneToonUtils.hlsl             # ユーティリティ関数
│           ├── NataneToonVertex.hlsl            # 頂点シェーダー
│           └── NataneToonFragment.hlsl          # フラグメントシェーダー
├── Editor/
│   └── NataneToon/
│       ├── NataneToonShaderGUI.cs               # カスタムインスペクター
│       └── MigrationTools/
│           ├── LilToonMigrationTool.cs          # lilToon変換ツール
│           ├── YMToonMigrationTool.cs           # YMToon変換ツール
│           └── BatchMaterialConverter.cs        # 汎用変換ツール
├── Materials/
│   └── Examples/                                 # サンプルマテリアル
└── Textures/
    └── Ramps/                                    # ランプテクスチャ
```

### モジュール構成の利点

- **NataneToonInput.hlsl**: プロパティと構造体を一元管理。新しいパラメータの追加が容易
- **NataneToonLighting.hlsl**: ライティング関数を分離。新しいシェーディング手法の追加が簡単
- **NataneToonUtils.hlsl**: 汎用的なユーティリティ関数。他のシェーダーでも再利用可能
- **NataneToonVertex.hlsl**: 頂点処理を独立化。頂点変形などのカスタマイズが容易
- **NataneToonFragment.hlsl**: フラグメント処理を独立化。エフェクトの追加・変更が明確

この構造により、特定の機能の修正や拡張時に関連ファイルのみを編集すれば良く、コードの保守性が大幅に向上しています。

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

5. **Additional Light Intensity** (0-1): 追加ライトの影響度
   - ForwardAdd パス（追加ライト）での明るさの倍率
   - 複数のライトがある場合の明るくなりすぎを防ぐ
   - 0 = 追加ライトの影響なし
   - 0.5 = 追加ライトの影響を半分に
   - 1 = 追加ライトの影響を100%受ける
   - 推奨値: 0.3-0.5 （ライトの数によらず一定の明るさを保つ）
   - 用途: 複数のライトを使用する環境で、明るくなりすぎるのを防ぐ

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

## 更新履歴

### v1.5.0 (2025-10-29)
- **バーチャル表現機能** 追加（VRChat向け）
  - Dissolve（溶解効果）: アバターの出現/消失演出
    - ノイズテクスチャベースの溶解
    - 境界線の発光エフェクト（HDRカラー対応）
    - Dissolve Amountパラメータでアニメーション制御可能
  - Hue Shift（色相変更）: リアルタイムでの色変更
    - RGB ↔ HSV 変換による色相シフト
    - 服装色変更ギミックに最適
  - Emission Animation（発光アニメーション）強化
    - Emission Scroll: 発光テクスチャのスクロール
    - Emission Pulse: 発光の脈動エフェクト
- 全機能が全シェーダーバリアント（Opaque/Cutout/Transparent）で利用可能
- ShaderGUIに詳細な説明とヘルプボックスを追加

### v1.4.0 (2025-10-29)
- **プロジェクト構造の再編成**
  - モジュール化されたHLSLファイル構成（Input/Lighting/Utils/Vertex/Fragment）
  - ディレクトリの整理（NataneToon/配下に統一）
  - 可読性と拡張性の大幅向上
- **追加ライト制御** 機能追加
  - Additional Light Intensity: 複数ライト時の明るさ制御
  - ForwardAdd パスでの明るくなりすぎを防止
  - ライトの数によらず一定の明るさを維持可能

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
