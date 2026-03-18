# NataneToon Eye Shader - テクスチャ対応機能

## 概要

NataneToon Eye Shader v2 では、各 Eye State でテクスチャを使用できるようになりました。手続き的レンダリング（SDF）とテクスチャを組み合わせることで、より詳細で表現力豊かな目の表現が可能です。

## 新機能

### ✨ テクスチャ制御機能

各 Eye State に専用のテクスチャスロットが追加されました：

- ✅ **Normal State Texture**: 通常状態用テクスチャ
- ✅ **Star State Texture**: 星状態用テクスチャ
- ✅ **Heart State Texture**: ハート状態用テクスチャ
- ✅ **Dead State Texture**: 死状態用テクスチャ
- ✅ **Nervous State Texture**: 緊張状態用テクスチャ

### 🎯 主な特徴

1. **視差効果の適用**
   - すべてのテクスチャにパララックス（視差）効果が適用されます
   - `Parallax Strength` で強度を調整可能
   - 目に立体感と奥行きを与えます

2. **手続き的エフェクトとの統合**
   - テクスチャと SDF エフェクトを併用可能
   - `Texture Blend` でブレンド比率を調整
   - グレア、バブル、ビネットなどのエフェクトは引き続き使用可能

3. **柔軟な制御**
   - `Use Texture` トグルでテクスチャの有効/無効を切り替え
   - テクスチャなしでも従来の手続き的レンダリングが動作

## セットアップ方法

### 基本設定

#### 1. テクスチャの有効化

```
Main Eye Settings:
├─ Use Texture: ON
└─ Texture Blend: 0.0 ～ 1.0
   ├─ 0.0: 完全に手続き的レンダリング
   ├─ 0.5: 50/50 ブレンド
   └─ 1.0: 完全にテクスチャ
```

#### 2. テクスチャの設定

各 Eye State セクションにテクスチャスロットが追加されています：

```
Eye State Normal:
├─ Normal State Texture: 通常状態の目テクスチャ
├─ Pupil Color: 瞳孔の色（手続き的）
└─ Detail Brightness: ディテールの明るさ

Eye State Star:
├─ Star State Texture: 星状態の目テクスチャ
├─ Star Color: 星の色（手続き的）
├─ Rocking Sharpness: 揺れの鋭さ
├─ Rocking Angle: 揺れの角度
└─ Rocking Speed: 揺れの速度

Eye State Heart:
├─ Heart State Texture: ハート状態の目テクスチャ
├─ Heart Color: ハートの色（手続き的）
├─ Pulse Speed: パルスの速度
└─ Pulse Power: パルスの強度

Eye State Dead:
├─ Dead State Texture: 死状態の目テクスチャ
├─ Gradient Offset: グラデーションオフセット
├─ Top Color: 上部の色
└─ Bottom Color: 下部の色

Eye State Nervous:
├─ Nervous State Texture: 緊張状態の目テクスチャ
├─ Background Color: 背景色
├─ Lines Color: ラインの色（手続き的）
└─ その他のライン設定...
```

## 使用例

### 例1: テクスチャベースの通常の目

```
Eye State: Normal
Use Texture: ON
Texture Blend: 1.0 (完全にテクスチャ)
Normal State Texture: eye_normal.png
Parallax Strength: 0.9
Main Glare Color: (1.5, 1.5, 1.5)
Bubble Brightness: 0.1
```

**結果**: テクスチャに視差効果が適用され、グレアとバブルが追加されます。

### 例2: テクスチャ + 手続き的エフェクトのブレンド

```
Eye State: Star
Use Texture: ON
Texture Blend: 0.6 (60% テクスチャ、40% 手続き的)
Star State Texture: eye_base.png
Star Color: (3, 3, 4) 明るい青白色
Rocking Speed: 0.5
Parallax Strength: 1.0
```

**結果**: テクスチャをベースに、手続き的な星型SDFが40%の強度で合成され、揺れアニメーションが適用されます。

### 例3: テクスチャのみ（エフェクトなし）

```
Eye State: Normal
Use Texture: ON
Texture Blend: 1.0
Normal State Texture: eye_detailed.png
Parallax Strength: 0.8
Main Glare Color: (0, 0, 0, 0) アルファ0で無効化
Bubble Brightness: 0
Detail Brightness: 0
```

**結果**: 純粋にテクスチャのみが表示され、手続き的エフェクトは適用されません。

### 例4: Dead状態のテクスチャ + グラデーション

```
Eye State: Dead
Use Texture: ON
Texture Blend: 0.5
Dead State Texture: eye_dead_base.png
Top Color: (0.1, 0.1, 0.1, 0.5) 半透明の暗色
Bottom Color: (0.6, 0.6, 0.6, 0.3) 半透明の明色
Gradient Offset: 0.1
```

**結果**: テクスチャに半透明のグラデーションがオーバーレイされます。

### 例5: Nervous状態のテクスチャ + 動的ライン

```
Eye State: Nervous
Use Texture: ON
Texture Blend: 0.7
Nervous State Texture: eye_base.png
Lines Color: (0.1, 0, 0, 1) 暗赤色
Lines Thickness: 0.12
Random Seed: 42
```

**結果**: テクスチャベースに、手続き的な動的ラインが30%の強度で合成されます。

## テクスチャ作成のヒント

### 推奨テクスチャ仕様

```
解像度: 512x512 ～ 2048x2048
フォーマット: PNG (アルファチャンネル対応)
UV マッピング: 0-1 の標準範囲
推奨アスペクト比: 1:1 (正方形)
```

### テクスチャデザインガイド

#### Normal State
- **瞳孔**: 中央に配置
- **虹彩**: 瞳孔の周囲に詳細なパターン
- **ハイライト**: 白い光沢部分は控えめに（シェーダーで追加されるため）
- **透明度**: 必要に応じてアルファチャンネルで外周を透明化

#### Star State
- ベースは Normal と同じ
- 星型の装飾は手続き的に追加されるため、テクスチャには不要
- または、テクスチャ自体に星型を描画し、`Texture Blend: 1.0` で使用

#### Heart State
- ベースは Normal と同じ
- ハート型は手続き的に追加されるため、テクスチャには不要
- ピンク系の色味にすると雰囲気が出る

#### Dead State
- 彩度を落とした灰色調
- グラデーションはシェーダーで追加されるため、均一な色でOK
- 血管などのディテールがあると効果的

#### Nervous State
- ベースは Normal と同じ
- 動的ラインは手続き的に追加されるため、テクスチャには不要
- わずかに黄色味を帯びた色にすると緊張感が出る

### レイヤー構成の例

Photoshop/GIMP でのレイヤー構成例:

```
レイヤー構成:
├─ [最上層] ハイライト（白、軽く）
├─ 虹彩ディテール（パターン）
├─ 虹彩ベース（グラデーション）
├─ 瞳孔（黒）
└─ [最下層] 背景（白または透明）
```

## パララックス効果について

### 視差の仕組み

```
Parallax Strength: 視差の強度
├─ 0.0: 視差なし（平面）
├─ 0.5: 弱い視差
├─ 0.8: 適度な視差（推奨）
└─ 1.0: 強い視差
```

### 視差とテクスチャの関係

パララックスUVは以下のように計算されます：

```hlsl
pupilUV = GenerateParallaxUV(baseUV, viewDir, -0.25 * _MainParallax)
```

- `viewDir`: カメラからの視線方向
- `-0.25`: 深度オフセット（負の値で奥に引っ込む）
- `_MainParallax`: ユーザー設定値

**結果**: カメラの角度に応じてテクスチャがわずかにシフトし、立体感が生まれます。

### 深度レイヤー

シェーダーでは2つの深度レイヤーを使用：

1. **glareUV**: `-0.2 * _MainParallax` （浅い層、グレア用）
2. **pupilUV**: `-0.25 * _MainParallax` （深い層、瞳孔用）

これにより、グレアが手前、瞳孔が奥に見える立体構造を実現。

## ブレンドモードの詳細

### Texture Blend の動作

#### Normal/Star/Heart State:
```hlsl
if (_UseTexture > 0.5) {
    texCol = tex2D(_StateTexture, pupilUV);
    col.rgb = texCol.rgb * _BackgroundColor.rgb;
    // 手続き的エフェクト（グレア、星、ハート）は後から追加
}
```

- `Texture Blend: 1.0`: テクスチャが完全に使用され、手続き的背景は無視
- `Texture Blend: 0.0`: 手続き的背景のみ（実際には Use Texture を OFF にするべき）

#### Dead/Nervous State:
```hlsl
proceduralCol = [手続き的計算];
texCol = tex2D(_StateTexture, pupilUV);
finalCol = lerp(proceduralCol, texCol * _BackgroundColor, _TextureBlend);
```

- `Texture Blend: 0.0`: 完全に手続き的
- `Texture Blend: 0.5`: 50/50 ブレンド
- `Texture Blend: 1.0`: 完全にテクスチャ

## トラブルシューティング

### テクスチャが表示されない

1. **Use Texture が ON になっているか確認**
   ```
   Main Eye Settings > Use Texture: ON
   ```

2. **テクスチャが設定されているか確認**
   ```
   対応する Eye State セクションのテクスチャスロットを確認
   ```

3. **Texture Blend が 0 でないか確認**
   ```
   Texture Blend: 0.5 ～ 1.0 に設定
   ```

### テクスチャがぼやける

- テクスチャ解像度を上げる（1024x1024 以上）
- Unity のテクスチャインポート設定:
  ```
  Filter Mode: Trilinear
  Aniso Level: 4 ～ 16
  Max Size: 2048 以上
  ```

### 視差効果が効かない

- `Parallax Strength` が 0 でないか確認
- メッシュに正しい法線と接線が設定されているか確認
- カメラを動かして視線方向を変えてみる

### 手続き的エフェクトとテクスチャの境界が目立つ

- `Texture Blend` を調整（0.6 ～ 0.8 あたりが自然）
- テクスチャの色を `Background Color` に近づける
- グレアやバブルの強度を調整

## パフォーマンス

### テクスチャサンプリングのコスト

```
コスト評価:
├─ テクスチャなし（手続き的のみ）: 基準
├─ テクスチャあり（Normal/Star/Heart）: +1 テクスチャサンプリング
└─ テクスチャあり（Dead/Nervous）: +1 テクスチャサンプリング
```

**追加コスト**: 非常に軽微（1回のテクスチャサンプリング）

### 最適化のヒント

1. **テクスチャ圧縮**:
   ```
   Format: DXT5/BC7 (アルファあり)
   Max Size: 1024 (モバイル向け)
   ```

2. **ミップマップ**:
   ```
   Generate Mip Maps: ON （推奨）
   ```

3. **不要なエフェクトを無効化**:
   ```
   Bubble Brightness: 0
   Detail Brightness: 0
   Main Glare Color Alpha: 0
   ```

## まとめ

NataneToon Eye Shader のテクスチャ対応機能により、以下が可能になりました：

✅ **視差効果付きテクスチャレンダリング**
- 各 Eye State に専用テクスチャを設定可能
- パララックスによる立体感

✅ **柔軟なブレンド**
- テクスチャと手続き的エフェクトの併用
- ブレンド比率の調整

✅ **高い表現力**
- 詳細なテクスチャ + リアルタイムアニメーション
- グレア、バブル、ビネットなどのエフェクト

✅ **後方互換性**
- テクスチャなしでも従来通り動作
- 既存のマテリアルに影響なし

この機能により、VRChat アバターの目により豊かで詳細な表現が可能になります。
