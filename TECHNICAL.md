# 技術仕様書

Natane Toon Shaderの技術的な詳細とカスタマイズガイドです。

## アーキテクチャ

### ファイル構成

```
Assets/
├── Shaders/
│   ├── NataneToonShader.shader      # メインシェーダーファイル
│   └── Include/
│       └── NataneToonCore.cginc     # コア機能実装
└── Editor/
    └── NataneToonShaderGUI.cs       # カスタムインスペクタ
```

### シェーダーパス構成

#### 1. OUTLINE Pass
- **目的**: アウトライン描画
- **Cull Mode**: Front（前面カリング）
- **手法**: 法線押し出し（Inverted Hull）
- **実行条件**: `_OUTLINE` キーワードが有効な場合

#### 2. FORWARD_BASE Pass
- **目的**: メインライティング、環境光、各種エフェクト
- **Light Mode**: ForwardBase
- **機能**:
  - セルシェーディング/ランプシェーディング
  - スペキュラハイライト
  - リムライト
  - MatCap
  - エミッション
  - 環境光
  - シャドウ受信

#### 3. FORWARD_ADD Pass
- **目的**: 追加ライト処理
- **Light Mode**: ForwardAdd
- **Blend Mode**: One One（加算ブレンド）
- **機能**:
  - セルシェーディング/ランプシェーディング
  - スペキュラハイライト
  - 複数ライト対応

#### 4. SHADOW_CASTER Pass
- **目的**: シャドウキャスティング
- **Light Mode**: ShadowCaster
- **機能**: 他のオブジェクトへの影の投影

## ライティングアルゴリズム

### セルシェーディング

```csharp
float ToonShading(float ndotl, float steps, float sharpness)
{
    // Shadow Offsetを適用
    ndotl = ndotl + _ShadowOffset;

    // ステップ化
    float toon = floor(ndotl * steps) / steps;

    // エッジのスムージング
    float edge = frac(ndotl * steps);
    toon += smoothstep(0.5 - sharpness * 0.5,
                       0.5 + sharpness * 0.5,
                       edge) / steps;

    return saturate(toon);
}
```

**パラメータ**:
- `ndotl`: 法線とライト方向の内積
- `steps`: 影のステップ数（1-10）
- `sharpness`: エッジのシャープネス（0.001-1）

### ランプテクスチャシェーディング

```csharp
float3 RampShading(float ndotl)
{
    float2 rampUV = float2(saturate(ndotl + _ShadowOffset), 0.5);
    return tex2D(_RampTex, rampUV).rgb;
}
```

**テクスチャ仕様**:
- 横方向にグラデーション（左=暗、右=明）
- 推奨サイズ: 256x4 または 512x4
- フォーマット: RGB、Clamp

### スペキュラハイライト

```csharp
float SpecularHighlight(float3 normal, float3 viewDir,
                       float3 lightDir, float size, float softness)
{
    float3 halfVector = normalize(lightDir + viewDir);
    float ndoth = max(0, dot(normal, halfVector));

    // アニメ調のシャープなスペキュラ
    float spec = smoothstep(1.0 - size - softness,
                           1.0 - size + softness,
                           ndoth);
    return spec;
}
```

**特徴**:
- Blinn-Phongベースのハーフベクトル使用
- smoothstepでアニメ調のシャープなエッジ
- サイズとソフトネスで調整可能

### リムライト

```csharp
float3 RimLighting(float3 normal, float3 viewDir,
                  float power, float intensity)
{
    float rim = 1.0 - saturate(dot(normal, viewDir));
    rim = pow(rim, power) * intensity;
    return rim * _RimColor.rgb;
}
```

**特徴**:
- Fresnel効果ベース
- Powerパラメータで範囲制御
- Intensityで強度調整

### MatCap

```csharp
float2 CalculateMatCapUV(float3 worldNormal, float3 viewDir)
{
    float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
    float2 matcapUV = viewNormal.xy * 0.5 + 0.5;
    return matcapUV;
}
```

**ブレンドモード**:
- **Add (0)**: `col.rgb += matcap` - 光沢、ハイライト
- **Multiply (1)**: `col.rgb *= matcap` - 陰影、暗部
- **Replace (2)**: `col.rgb = lerp(col.rgb, matcap, intensity)` - 金属質感

### アウトライン

```csharp
// 頂点シェーダー
float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
float2 offset = TransformViewToProjection(norm.xy);

o.pos = UnityObjectToClipPos(v.vertex);
o.pos.xy += offset * o.pos.z * _OutlineWidth;
```

**特徴**:
- ビュー空間での法線押し出し
- 距離に応じた太さ調整（`o.pos.z`で乗算）
- 前面カリングで内側のみ描画

## シェーダーキーワード

| キーワード | 機能 | パフォーマンス影響 |
|-----------|------|------------------|
| `_USE_RAMP` | ランプテクスチャ | 小（テクスチャ読み込み1回） |
| `_SPECULAR` | スペキュラハイライト | 小（計算少量） |
| `_RIM_LIGHT` | リムライト | 小（計算少量） |
| `_MATCAP` | MatCap | 中（テクスチャ読み込み+座標計算） |
| `_OUTLINE` | アウトライン | 中（追加パス） |
| `_EMISSION` | 発光 | 小（テクスチャ読み込み1回） |
| `_NORMALMAP` | 法線マップ | 中（テクスチャ+接線空間計算） |

## パフォーマンス最適化

### レベル1: 最軽量（モバイル向け）
```
有効: セルシェーディングのみ
無効: すべてのエフェクト
Shadow Steps: 2
```
**用途**: ローエンドモバイル、多数のキャラクター

### レベル2: 軽量（標準モバイル）
```
有効: セルシェーディング、リムライト、アウトライン
無効: MatCap、法線マップ
Shadow Steps: 2
```
**用途**: スタンダードなモバイルゲーム

### レベル3: 標準（PC/コンソール）
```
有効: すべての基本機能
オプション: MatCap、法線マップ
Shadow Steps: 2-3
```
**用途**: PC、コンソールゲーム

### レベル4: 高品質
```
有効: すべての機能
法線マップ: 使用
Shadow Steps: 3-5
```
**用途**: ハイエンドPC、映像制作

## カスタマイズガイド

### 新しいエフェクトの追加

1. **Propertiesに追加**
```csharp
[Toggle(_NEW_FEATURE)] _NewFeature ("Enable New Feature", Float) = 0
_NewFeatureParam ("Parameter", Range(0, 1)) = 0.5
```

2. **#pragma shader_featureを追加**
```csharp
#pragma shader_feature _NEW_FEATURE
```

3. **変数宣言（NataneToonCore.cginc）**
```csharp
float _NewFeatureParam;
```

4. **実装（frag関数内）**
```csharp
#ifdef _NEW_FEATURE
    // Your code here
#endif
```

5. **ShaderGUIに追加**
```csharp
bool enableFeature = DrawToggle("_NEW_FEATURE", "_NewFeature", "Enable New Feature");
if (enableFeature)
{
    DrawProperty("_NewFeatureParam", "Parameter");
}
```

### カスタムライティングモデルの追加

`NataneToonCore.cginc` の `frag` 関数内でライティング計算をカスタマイズ:

```csharp
// 既存のライティング
#ifdef _USE_RAMP
    lighting = RampShading(ndotl * atten);
#else
    float toon = ToonShading(ndotl * atten, _ShadowSteps, _ShadowSharpness);
    lighting = lerp(_ShadowColor.rgb, float3(1, 1, 1), toon);
#endif

// カスタムライティングを追加
#ifdef _CUSTOM_LIGHTING
    lighting = YourCustomLightingFunction(ndotl, atten);
#endif
```

## デバッグとトラブルシューティング

### デバッグビューの追加

```csharp
// Normal可視化
return fixed4(worldNormal * 0.5 + 0.5, 1);

// NdotL可視化
return fixed4(ndotl.xxx, 1);

// Shadow可視化
return fixed4(atten.xxx, 1);
```

### よくある問題

#### アウトラインがちらつく
**原因**: モデルの法線が不正
**解決**:
- モデルの法線を再計算
- Smoothingを適切に設定
- Outline Widthを調整

#### 影の境界がガタガタ
**原因**: Shadow Sharpnessが高すぎる
**解決**: Sharpnessを0.05-0.1に設定

#### MatCapが回転する
**原因**: 正常動作（ビュー空間ベースのため）
**解決**: ワールド空間MatCapが必要な場合はコード修正

## ライセンスとクレジット

このシェーダーは以下のシェーダーを参考に設計されています：
- lilToon
- NovaShader
- NiloToon
- PoiyomiToon

MITライセンスの下で配布されています。
