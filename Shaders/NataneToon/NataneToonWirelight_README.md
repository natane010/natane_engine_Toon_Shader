# NataneToon Wirelight シェーダー

## 概要

NataneToon Wirelightは、高度なワイヤーフレームエフェクトシェーダーです。ジオメトリシェーダーを使用して、メッシュの三角形とエッジを可視化し、サイバーパンク/ネオン風の表現を実現します。

## 主要機能

### 1. ジオメトリシェーダーによるワイヤーフレーム表現
- 三角形の辺と頂点を可視化
- 重心座標(Barycentric Coordinates)を使用したエッジ検出
- シャープモードとラウンドモードの切り替え

### 2. ケージング(頂点丸め込み)
- 頂点位置をグリッドに吸着させてローポリ化
- 2つのケージ設定をノイズで補間
- メッシュを法線方向に押し出す機能

### 3. 多層ノイズアニメーション
- Fractal Brownian Motion (FBM)による有機的なパターン
- 3層の歪みによる複雑な模様生成
- アニメーション制御(速度・方向)

### 4. OKLAB色空間による色相シフト
- 知覚的に均一な色変換
- RGBよりも自然で滑らかな色相変化
- 自動色相回転

### 5. Bayerマトリックスディザリング
- 4x4ディザーパターン
- 三角形の面と辺で異なるディザー密度
- レトロ風ドットパターン

### 6. AudioLink統合
- 音楽反応型のエクストルード(メッシュ押し出し)
- カラー遷移の音楽制御
- Chronotensityによる高度なタイミング制御
- AudioLinkテーマカラー対応
- Cyber Data Streamの帯域連動

### 7. 頂点カラーベースの位置保持
- アニメーションやシェイプキーに影響されないノイズパターン
- 頂点カラーに元の位置をベイク可能

### 8. VRC最適化モード
- **Performance Mode**: Quality / Balanced / Lite
- **Distance Fade**: 遠距離でアルファを自動減衰
- **Alpha Clip**: 低アルファ領域をカットして重なり負荷を削減

### 9. Cyber Data Stream
- 走査線状の「データ流」エフェクト
- 密度・速度・強度・ジッターを個別制御
- AudioLinkでデータ流量を連動可能

## セットアップ

### 1. シェーダーの適用

1. マテリアルを作成
2. Shader → Natane → Toon Shader Wirelightを選択
3. メッシュレンダラーにマテリアルを適用

### 2. 基本設定

#### ケージ設定(Cage Vertex Rounding)

- **Cage 1 Rounding Factor**: 頂点丸め係数(1-200)
  - 高い値 = 粗いグリッド(ローポリ感が強い)
  - 推奨: 30-100

- **Extrude 1 Push Amount**: メッシュ押し出し量(0-0.2)
  - メッシュを法線方向に膨らませる
  - 推奨: 0.01-0.05

- **Cage 2 / Extrude 2**: 第2のケージ設定
  - ノイズで自動的に補間される

#### 三角形表示(Triangle Visualization)

- **Triangle Roundness**: 0=シャープ(辺), 1=ラウンド(頂点)
  - 0: 三角形の辺が表示される
  - 1: 三角形の頂点が強調される

- **Triangle Edge Width**: 辺/頂点の幅(0.001-0.1)
  - 推奨: 0.01

- **Triangle Edge Sharpness**: 辺の鋭さ(0.1-10)
  - 高い値 = 鋭いエッジ
  - 推奨: 2-3

#### ディザリング(Dithering Pattern)

- **Face Dither Density**: 面のディザー密度(0-50)
  - 推奨: 10-20

- **Edge Dither Density**: 辺のディザー密度(0-50)
  - 推奨: 10-20

#### ノイズアニメーション(Noise Animation)

- **Noise Scale**: ノイズの大きさ(X成分)
  - 推奨: 5-20
  - 小さい値 = 大きなパターン

- **Movement Dir XYZ Speed W**: 移動方向と速度
  - XYZ: 方向ベクトル
  - W: 速度
  - 例: (0, 0.1, 0, 1) = Y軸方向に速度1で上昇

- **Noise Intensity**: ノイズの強度(0-50)
  - 推奨: 15-30

#### 色設定(Dual Color System)

- **Color 1 / Color 2**: 2つのHDRカラー
  - ノイズで自動的に補間される
  - HDR対応(Intensity > 1で発光)

- **Color 1/2 Hue Shift**: OKLAB空間での色相シフト(0-1)
  - 0 = シフトなし
  - 0.5 = 180度回転
  - 1.0 = 360度回転(元に戻る)

- **Auto Hue Shift C1 C2**: 自動色相回転速度
  - X: Color 1の回転速度
  - Y: Color 2の回転速度
  - 例: (0.05, -0.05) = 反対方向に回転

- **Color Transition Width**: 2色の遷移幅(0-1)
  - 推奨: 0.1-0.3

### 3. AudioLink設定

AudioLinkを有効にするには:

1. **Enable AudioLink**をON
2. VRChatワールドにAudioLinkプレハブを配置
3. 各パラメータを調整:

#### AudioLinkパラメータ

- **AL Extrude Intensity**: 音楽によるメッシュ押し出し強度(0-0.2)
- **AL Extrude Band**: 周波数帯域
  - 0: Bass(低音)
  - 1: Low Mid(中低音)
  - 2: High Mid(中高音)
  - 3: Treble(高音)

- **AL Color Intensity**: 音楽による色遷移の強度(0-1)
- **AL Color Band**: 色制御の周波数帯域

- **AL Chronotensity Intensity**: ノイズ移動のタイミング制御(0-10)
- **AL Chrono Mode**: タイミングモード
  - 0: Accel - 強度に比例して加速
  - 1: Accel Smooth - 上記のスムーズ版
  - 2: Oscillate - 強度に応じた往復運動
  - 3: Oscillate Smooth - 上記のスムーズ版
  - 4: Dark Move - 暗い時に移動、明るい時に停止
  - 5: Dark Move Smooth - 上記のスムーズ版
  - 6: Bidirectional - 暗い時は正方向、明るい時は逆方向
  - 7: Bidirectional Smooth - 上記のスムーズ版

- **Color 1/2 Theme**: AudioLinkテーマカラーの使用
  - None: 使用しない
  - Theme 1-4: AudioLinkのテーマカラー1-4を使用
- **AL Cyber Data Stream**: データストリームの連動強度
- **AL Data Band**: データストリーム連動の周波数帯域

## VRC最適化設定（新規）

### 1. Performance Mode
- **Quality**: 見た目優先
- **Balanced**: 品質と負荷のバランス
- **Lite**: 負荷優先（クロマ/グリッチ強度を内部で抑制）

### 2. Distance Fade
- `Fade Start Distance` から `Fade End Distance` にかけて透明化
- 遠距離の重なり描画を減らし、ワールド全体負荷を抑制

### 3. Alpha Clip
- 低アルファ領域をカット
- 半透明オーバードローが重い場面で有効
- 推奨しきい値: `0.06 - 0.15`

## プリセット例

### プリセット1: サイバーパンクネオン

```
[Cage]
Cage 1: 100
Extrude 1: 0.05

[Triangle]
Triangle Roundness: 0.3
Triangle Edge Width: 0.015
Triangle Edge Sharpness: 2.5

[Dither]
Face Dither: 15
Edge Dither: 20

[Noise]
Noise Scale: 15
Movement: (0, 0.2, 0, 0.5)
Noise Intensity: 25

[Color]
Color 1: RGB(0, 128, 255) Intensity: 2.0
Color 2: RGB(255, 0, 128) Intensity: 2.0
Auto Hue Shift: (0.05, -0.05)
Color Brightness: 1.5
```

### プリセット2: ローポリグリッド(静止)

```
[Cage]
Cage 1: 30
Extrude 1: 0.01

[Triangle]
Triangle Roundness: 0
Triangle Edge Width: 0.01
Triangle Edge Sharpness: 3

[Noise]
Noise Scale: 10
Movement: (0, 0, 0, 0)  // 静止
Noise Intensity: 15

[Color]
Color 1: RGB(255, 255, 255)
Color 2: RGB(128, 128, 128)
Color Brightness: 1.0
```

### プリセット3: オーガニックフロー

```
[Cage]
Cage 1: 80
Extrude 1: 0.03
Cage 2: 120
Extrude 2: 0.01

[Triangle]
Triangle Roundness: 0.8  // 丸い頂点
Triangle Edge Width: 0.02
Triangle Edge Sharpness: 2.0

[Noise]
Noise Scale: 5  // 大きなパターン
Movement: (0.1, 0.2, 0.1, 0.5)
Noise Intensity: 30

[Color]
Color 1: RGB(255, 128, 0) Intensity: 1.5
Color 2: RGB(0, 255, 128) Intensity: 1.5
Auto Hue Shift: (0.1, -0.1)
Color Transition: 0.2
```

### プリセット4: AudioLink反応(クラブ向け)

```
[Cage]
Cage 1: 60
Extrude 1: 0.02

[AudioLink]
Enable AudioLink: ON
AL Extrude Intensity: 0.1
AL Extrude Band: Bass (0)
AL Color Intensity: 0.5
AL Color Band: Treble (3)
AL Chronotensity: 2.0
AL Chrono Mode: Oscillate (2)

[Color]
Color 1 Theme: Theme 1
Color 2 Theme: Theme 2
```

### プリセット5: エネルギーシールド

```
[Cage]
Cage 1: 150
Extrude 1: 0.01
Cage 2: 150
Extrude 2: 0.005

[Triangle]
Triangle Roundness: 1.0  // フルラウンド（六角形の角）
Triangle Edge Width: 0.008
Triangle Edge Sharpness: 3.5

[Dither]
Face Dither: 25
Edge Dither: 30

[Noise]
Noise Scale: 8
Movement: (0, 0.05, 0, 0.3)  // ゆっくり上昇
Noise Intensity: 18
Noise Offset: 0.2

[Color]
Color 1: RGB(77, 179, 255) Intensity: 2.5  // 明るい青
Color 2: RGB(128, 230, 255) Intensity: 1.8  // シアン
Auto Hue Shift: (0.02, -0.02)  // 微妙な色相変化
Color Transition: 0.15
Color Brightness: 1.3

[Rendering]
Source Blend: One (1)  // 加算合成
Dest Blend: One (1)
```

**特徴**:
- 細かい六角形パターンでシールド風
- ゆっくりとした脈動でエネルギー感
- 加算合成で明るく発光
- 青白いエネルギーカラー

### プリセット6: ホログラムグリッド

```
[Cage]
Cage 1: 40
Extrude 1: 0.015
Cage 2: 60
Extrude 2: 0.01

[Triangle]
Triangle Roundness: 0.1  // シャープなエッジ
Triangle Edge Width: 0.012
Triangle Edge Sharpness: 2.8

[Dither]
Face Dither: 8  // スキャンライン風
Edge Dither: 18

[Noise]
Noise Scale: 12
Movement: (0, 0.3, 0, 1.5)  // 速い上昇
Noise Intensity: 22
Noise Offset: 0.4

[Color]
Color 1: RGB(0, 255, 204) Intensity: 1.5  // シアン
Color 2: RGB(51, 204, 255) Intensity: 1.2  // 薄い青
Auto Hue Shift: (0, 0)  // 色相固定
Color Transition: 0.25
Color Brightness: 1.2
Multiply Alpha to Color: 0.3  // 透明度

[Rendering]
Standard Alpha Blend (デフォルト)
```

**特徴**:
- 大きめのグリッドでホログラム風
- 速いノイズ移動でグリッチ感
- スキャンラインのようなディザーパターン
- 青緑のホログラムカラー
- 半透明で浮遊感

### プリセット7: VRC軽量(PC)

```
[VRC Optimization]
Performance Mode: Lite
Distance Fade: ON (Start 8 / End 20)
Alpha Clip: ON (Threshold 0.12)

[Cyber]
Wire Style: Default
Scanline/Chroma/Glitch/Data Stream: OFF
```

**特徴**:
- PC-VRC向けの軽量運用テンプレート
- 半透明重なり時の破綻・過負荷を抑える

## パフォーマンスに関する注意

### 負荷が高い理由

1. **ジオメトリシェーダー**: 頂点数が3倍になる
2. **多層ノイズ計算**: リアルタイムで複雑な計算
3. **OKLAB色空間変換**: 累乗計算(cbrt)を含む
4. **AudioLink**: テクスチャサンプリング

### VRChatでの推奨使用法

| 用途 | ポリゴン数 | パフォーマンスランク |
|-----|----------|------------------|
| 小物アクセサリー | ~500 | Good ~ Medium |
| エフェクトパーツ | ~1000 | Medium ~ Poor |
| 背景オブジェクト | ~2000 | Poor |
| メインの服 | 3000+ | Very Poor(非推奨) |

### 最適化のヒント

1. **ポリゴン数を減らす**: できるだけ少ないポリゴンのメッシュに適用
2. **Noise Scaleを下げる**: 計算量が減る
3. **Dither Densityを調整**: 低い値で軽量化
4. **AudioLinkを無効化**: 使わない場合はOFFに
5. **装飾的なパーツのみに使用**: メインメッシュには使用しない

### Quest対応

**非対応** - ジオメトリシェーダーはQuest(Android)では使用できません。
PC版VRChatでのみ動作します。

## 頂点カラーベイク(オプション)

メッシュがアニメーションやシェイプキーで変形する場合、ノイズパターンが歪んでしまいます。
これを防ぐために、元の頂点位置を頂点カラーにベイクします。

### 方法1: Poiyomi Vertex Color Baker使用(推奨)

1. Unity上部メニュー → Poi → Tools → Vertex Color Baker
2. モデルをドラッグ&ドロップ
3. 「Bake Vertex Positions」をクリック
4. シェーダーで「Use Vertex Color Position」をON

### 方法2: Blenderで手動

1. Blenderでモデルを開く
2. 頂点カラーレイヤーを作成
3. Python Script:
```python
import bpy
import bmesh

obj = bpy.context.active_object
mesh = obj.data
bm = bmesh.from_edit_mesh(mesh)

# 頂点カラーレイヤーを取得/作成
if not mesh.vertex_colors:
    mesh.vertex_colors.new()
color_layer = mesh.vertex_colors.active

# 各頂点の位置をRGBに格納
for poly in mesh.polygons:
    for loop_index in poly.loop_indices:
        loop = mesh.loops[loop_index]
        vert = mesh.vertices[loop.vertex_index]
        # ワールド座標をカラーに変換
        world_pos = obj.matrix_world @ vert.co
        color_layer.data[loop_index].color = (
            world_pos.x,
            world_pos.y,
            world_pos.z,
            1.0
        )
```

4. Unityにエクスポート
5. シェーダーで「Use Vertex Color Position」をON

## レンダリング設定

### ブレンディング

- **Source Blend**: SrcAlpha (5) - デフォルト
- **Dest Blend**: OneMinusSrcAlpha (10) - デフォルト
- 加算合成にしたい場合: Source=One(1), Dest=One(1)

### Zバッファ

- **Z Write**: Off - 透明オブジェクトのため
- **Z Test**: LEqual (4) - 通常の深度テスト

### レンダーキュー

- **Render Queue Override**: 2501-3000
- 推奨: 2501以上(スカイボックスより手前に描画)

### ステンシル

完全なステンシル制御が可能です:
- **Stencil Reference**: 0-255
- **Stencil Compare**: 比較関数
- **Stencil Pass/Fail/ZFail**: ステンシル操作

## トラブルシューティング

### Q: エフェクトが表示されない

**確認事項**:
1. PC版VRChatでプレイしているか(Quest非対応)
2. Render Queueが2501以上か
3. Mask Textureが真っ黒になっていないか
4. Enable WirelightがONか
5. GPUがGeometry Shaderに対応しているか

### Q: パターンがメッシュ変形で壊れる

**解決策**:
1. 頂点カラーに位置をベイク(上記参照)
2. 「Use Vertex Color Position」をON

### Q: パフォーマンスが悪い

**最適化方法**:
1. Noise Scaleを下げる(10以下)
2. ポリゴン数を減らす(1000以下推奨)
3. Dither Densityを下げる(10以下)
4. AudioLinkを無効化

### Q: 色が変

**確認事項**:
1. Color 1/2がHDRカラーになっているか
2. Color BrightnessとPowerを調整
3. Hue Shiftの値を確認(0-1の範囲)
4. AudioLink Theme Colorが適切か

## 技術詳細

### シェーダーステージ

1. **Vertex Shader**: ケージ変換、ノイズ計算
2. **Geometry Shader**: 重心座標の割り当て
3. **Fragment Shader**: 色計算、ディザリング、最終合成

### ノイズ生成アルゴリズム

```hlsl
// 3層のFBM(Fractal Brownian Motion)
Layer 1: fbm(position)
Layer 2: fbm(position + Layer1 * 4)
Layer 3: fbm(position + Layer2 * 4)
```

### OKLAB色空間

- **L**: 明度(Lightness)
- **a**: 赤-緑(Red-Green)
- **b**: 黄-青(Yellow-Blue)

RGB → LMS(cone response) → OKLAB → 色相回転 → RGB

## ライセンスとクレジット

### 作者

- **NataneToon Wirelight**: NataneToon開発チーム

## サポート

問題が発生した場合は、以下の情報を含めて報告してください:

1. Unity バージョン
2. VRChat SDK バージョン
3. 使用しているマテリアル設定
4. エラーメッセージ(あれば)
5. スクリーンショット

---

**注意**: このシェーダーはPC版VRChat専用です。Quest版では動作しません。
ジオメトリシェーダーは負荷が高いため、装飾的なパーツに限定して使用することを推奨します。
