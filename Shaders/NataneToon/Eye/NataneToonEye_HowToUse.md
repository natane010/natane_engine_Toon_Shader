# NataneToon Eye Shader 使用方法

## 概要

NataneToon Eye Shader は、NeonGamer Anime Shader の高度な目エフェクト機能を、NataneToonシリーズとして統合したシェーダーです。VRChat アバターの目に生き生きとした表現を与えることができます。

## 特徴

### NeonGamer Anime Shader から継承された機能

✅ **5つの目の状態 (Eye States)**
- Normal: 通常の瞳（パララックス + グレア）
- Star: 星型エフェクト（回転アニメーション）
- Heart: ハート型エフェクト（パルスアニメーション）
- Dead: 死状態（グラデーション）
- Nervous: 緊張状態（動的な円形ライン）

✅ **高度なエフェクト**
- パララックス（視差）効果による奥行き表現
- AudioLink 統合（音楽連動）
- バブルエフェクト（光沢表現）
- ビネット効果（エッジ暗化/透明化）
- 色相・彩度・コントラスト調整

✅ **手続き的レンダリング**
- SDF (Signed Distance Functions) による正確な形状
- リアルタイムアニメーション
- カスタマイズ可能なパラメータ

## セットアップ

### 1. マテリアルの作成

1. Unity プロジェクトビューで右クリック
2. Create → Material
3. マテリアル名を設定（例: `EyeMaterial`）
4. Inspector でシェーダーを `Natane/Eye` に変更

### 2. 基本設定

#### Eye State Settings
```
Eye State: Normal (初期値)
```
5つの状態から選択:
- Normal: 通常の瞳
- Star: 星型
- Heart: ハート型
- Dead: 死
- Nervous: 緊張

#### Main Eye Settings
```
Eye Base Texture: 目のベーステクスチャ（オプション）
Parallax Strength: 0.8 ～ 1.0 (視差効果の強度)
Main Glare Color: (1, 1, 1) ～ (2, 2, 2) (グレアの色、HDR)
Background Color: (1, 1, 1) (背景色)
Pupil Size: 0.4 (瞳孔のサイズ)
```

## 各Eye Stateの詳細設定

### Normal State（通常状態）

**パラメータ**:
```
Pupil Color: (0, 0, 0) 瞳孔の色
Detail Brightness: 0.2 ディテールの明るさ
```

**特徴**:
- パララックス効果による奥行き
- 光沢（グレア）エフェクト
- バブルエフェクト対応

### Star State（星状態）

**パラメータ**:
```
Star Color: (1, 1, 1) ～ (4, 4, 4) HDR対応
Rocking Sharpness: 0.4 揺れの鋭さ（0=滑らか、1=鋭い）
Rocking Angle: 12 揺れの角度（0-90度）
Rocking Speed: 0.5 揺れの速度（0-1）
```

**アニメーション**:
- サイン波ベースの回転
- 瞳孔内に星型SDFを合成
- リアルタイムで揺れる

**推奨設定**:
```
Star Color: (2, 2, 3) 明るい青白色
Rocking Sharpness: 0.3 滑らかな動き
Rocking Angle: 15 適度な揺れ
Rocking Speed: 0.4 ゆったりとした動き
```

### Heart State（ハート状態）

**パラメータ**:
```
Heart Color: (1, 1, 1) ～ (4, 1, 2) HDR対応
Pulse Speed: 0.5 パルスの速度（0-1）
Pulse Power: 0.3 パルスの強度（0-1）
```

**アニメーション**:
- ハート型SDFによる形状
- 拍動アニメーション（拡大縮小）
- 2つのパルス波の重ね合わせ

**推奨設定（恋愛表現）**:
```
Heart Color: (4, 1, 2) 明るいピンク
Pulse Speed: 0.6 やや速い鼓動
Pulse Power: 0.35 適度な拡大
```

### Dead State（死状態）

**パラメータ**:
```
Gradient Offset: 0 グラデーションのオフセット（0-1）
Top Color: (0, 0, 0) 上部の色（暗色推奨）
Bottom Color: (1, 1, 1) 下部の色（明色推奨）
```

**特徴**:
- 上下グラデーション
- 瞳孔周りのビネット効果
- シンプルで静的

**推奨設定**:
```
Top Color: (0.1, 0.1, 0.1) ダークグレー
Bottom Color: (0.7, 0.7, 0.7) ライトグレー
Gradient Offset: 0.1 わずかに上へシフト
```

### Nervous State（緊張状態）

**パラメータ**:
```
Background Color: (1, 1, 1) 背景色
Lines Color: (0, 0, 0) ラインの色
Random Seed: 0 ランダムシード（異なる値で異なるパターン）
Lines Offset: 0.1 ラインのオフセット（0-1）
Lines Size: 0.8 ラインのサイズ（0-1）
Lines Thickness: 0.1 ラインの太さ（0-1）
```

**アニメーション**:
- 8つの円形ライン
- ランダムな位置配置
- 時間ベースのアニメーション（10FPS更新）

**推奨設定（恐怖表現）**:
```
Background Color: (1, 1, 0.9) わずかに黄色味
Lines Color: (0, 0, 0) 黒
Random Seed: 42 固定パターン
Lines Offset: 0.15 適度なランダム性
Lines Size: 0.75 中サイズの円
Lines Thickness: 0.12 やや太いライン
```

## エフェクト設定

### Hue and Color Settings（色調整）

```
Contrast: 0.5 コントラスト（0-1）
Saturation: 0 彩度（0-1、0=グレースケール、1=最大彩度）
Hue Offset: 0 色相オフセット（0-1、0.5で180度回転）
Hue Speed: 0 色相アニメーション速度（0-2）
```

**使用例**:
```
// レインボーアニメーション
Hue Speed: 0.5
Saturation: 0.3

// モノクロ
Saturation: 1.0
Contrast: 0.6
```

### Bubble Effect（バブルエフェクト）

```
Bubble Size: 0.08 バブルのサイズ（0-1）
Bubble Brightness: 0.1 バブルの明るさ（0-1）
Wobble Speed: 0.5 揺れの速度（0-1）
Wobble Strength: 0.1 揺れの強度（0-1）
```

**特徴**:
- 3つの異なるサイズのグレアポイント
- サイン波ベースの揺れアニメーション
- 目の表面の光沢を表現

**推奨設定（ガラス質な目）**:
```
Bubble Size: 0.1
Bubble Brightness: 0.2
Wobble Speed: 0.3 ゆっくりした動き
Wobble Strength: 0.08 控えめな揺れ
```

### Vignette Effect（ビネット）

```
Enable Transparency: OFF 透明度を有効化
Dither Scale: 0.5 ディザーのスケール（0-1）
Thickness: 0.4 ビネットの厚さ（0-1）
Falloff: 0.15 フォールオフの滑らかさ（0-1）
```

**特徴**:
- エッジの暗化/透明化
- ディザリングによるアルファ透過

**推奨設定**:
```
Enable Transparency: OFF （通常はOFF）
Thickness: 0.35 ～ 0.45
Falloff: 0.1 ～ 0.2 （滑らか）
```

### Audio Link（音楽連動）

```
Audio Band: Lows (低音に反応)
Audio Intensity: 1.0 強度（0-10）
Minimum Brightness: 1.0 最小明るさ（0-1）
```

**帯域の選択**:
- **Lows (0)**: ベース、キック → 力強い動き
- **Mids (2)**: ボーカル、スネア → 中程度の反応
- **Highs (3)**: ハイハット、シンバル → 細かい反応
- **Average (4)**: 全帯域の平均 → 安定した反応

**使用方法**:
1. シーンに AudioLink プレハブを配置
2. Audio Band を選択
3. Audio Intensity で強度を調整
4. Minimum Brightness で最低輝度を設定（1.0 = 音がない時も通常の明るさ）

**推奨設定（クラブ向け）**:
```
Audio Band: Lows
Audio Intensity: 3.0 強めに反応
Minimum Brightness: 0.5 音がない時は暗め
Main Glare Color: (0, 2, 4) 青色HDR
```

## レンダリング設定

### Rendering Settings

通常はデフォルト値のままで問題ありません。高度な用途向けの設定です。

**Stencil Settings**:
- VRChat での特殊なレンダリング制御
- 他のオブジェクトとの組み合わせ

**Blend Settings**:
- デフォルト: SrcAlpha / OneMinusSrcAlpha （半透明）
- 変更する場合は透明度の仕組みを理解してから

**Depth Settings**:
- ZTest: LEqual （通常）
- ZWrite: On （通常）

**Culling**:
- Back （背面カリング、通常）
- 両面表示する場合は Off

## 実用例

### 1. 基本的な目（Normal）

```
Eye State: Normal
Parallax Strength: 0.9
Main Glare Color: (1.5, 1.5, 1.5)
Pupil Size: 0.4
Pupil Color: (0, 0, 0)
Detail Brightness: 0.2
Bubble Size: 0.08
Bubble Brightness: 0.1
```

### 2. キラキラした目（Star + AudioLink）

```
Eye State: Star
Star Color: (3, 3, 4)
Rocking Sharpness: 0.35
Rocking Angle: 18
Rocking Speed: 0.5
Audio Band: Highs
Audio Intensity: 2.5
Hue Speed: 0.3
```

### 3. 恋愛モード（Heart）

```
Eye State: Heart
Heart Color: (4, 1, 2)
Pulse Speed: 0.55
Pulse Power: 0.32
Main Glare Color: (2, 1.5, 1.5)
Bubble Brightness: 0.15
```

### 4. ホラー表現（Nervous）

```
Eye State: Nervous
Background Color: (1, 0.95, 0.85)
Lines Color: (0.1, 0, 0)
Random Seed: 666
Lines Offset: 0.2
Lines Thickness: 0.15
Contrast: 0.7
```

### 5. 死亡状態（Dead）

```
Eye State: Dead
Top Color: (0.05, 0.05, 0.05)
Bottom Color: (0.6, 0.6, 0.6)
Gradient Offset: 0.15
Vignette Thickness: 0.5
```

## パフォーマンス

### 負荷の目安

- **Normal**: 中程度（パララックス + グレア）
- **Star**: やや重い（SDF + 回転計算）
- **Heart**: やや重い（SDF + パルス計算）
- **Dead**: 軽量（グラデーションのみ）
- **Nervous**: 中～やや重い（8回ループ）

### 最適化のヒント

1. **不要なエフェクトを無効化**:
   ```
   Bubble Brightness: 0 → バブル無効
   Detail Brightness: 0 → ディテール無効
   Vignette Thickness: 0 → ビネット無効
   ```

2. **AudioLink未使用時**:
   ```
   Audio Intensity: 0
   Minimum Brightness: 1
   ```

3. **アニメーション停止時**:
   ```
   Hue Speed: 0
   Pulse Speed: 0
   Rocking Speed: 0
   ```

## トラブルシューティング

### パララックスが効かない

- `Parallax Strength` が 0 でないか確認
- メッシュに正しい法線・接線が設定されているか確認
- カメラからの距離が適切か確認

### AudioLink が反応しない

- シーンに AudioLink プレハブが配置されているか
- AudioLink のパッケージが正しくインポートされているか
- `Audio Intensity` が 0 より大きいか

### アニメーションが動かない

- Unity がプレイモードになっているか
- 対応する Speed パラメータが 0 より大きいか
- タイムスケールが 0 でないか（Time.timeScale）

### 色がおかしい

- `Contrast` の値を確認（0.5が標準）
- `Saturation` の値を確認（0が標準）
- `Hue Offset` が意図した値か確認

## まとめ

NataneToon Eye Shader は、NeonGamer Anime Shader の高度な機能を継承し、VRChat アバターの目に多彩な表現を与えます。

**主な利点**:
- ✅ 5つの異なる目の状態による豊かな感情表現
- ✅ パララックス効果による立体感
- ✅ AudioLink による音楽連動演出
- ✅ 手続き的レンダリングによる正確な形状
- ✅ 詳細なパラメータ調整が可能
- ✅ NataneToonシリーズとの統合

アバターの感情や状態を効果的に伝え、VRChat でのコミュニケーションをより豊かにします。
