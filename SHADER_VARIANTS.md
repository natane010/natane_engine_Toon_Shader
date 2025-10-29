# Shader Variant 最適化ガイド

## 概要

Natane Toon Shaderは多機能なシェーダーであり、多数のshader keywordを持っています。これにより、膨大な数のshader variantsが生成される可能性があります。

### Shader Variantsとは？

Shader variantsは、shader keywordの組み合わせによって生成される、シェーダーの異なるバージョンです。例えば：

- `_EMISSION`がON/OFFで2つのvariant
- `_NORMALMAP`がON/OFFで2つのvariant
- 両方の組み合わせで2×2=4つのvariant

Natane Toon Shaderには12個以上のkeywordがあるため、理論上は **2^12 = 4,096** 以上のvariantsが存在します。

### 問題点

1. **ビルドサイズの増加**: すべてのvariantsをビルドに含めると、数十MB～数百MBになる可能性
2. **ロード時間の増加**: 初回使用時にshaderをコンパイルするため、スタッター（カクつき）が発生
3. **メモリ使用量の増加**: 使用しないvariantsもメモリに読み込まれる

## 解決策: ShaderVariantCollection

ShaderVariantCollectionを使用することで：

✅ **使用するvariantsのみをビルドに含める**
✅ **事前にシェーダーをウォームアップして、実行時のスタッターを防止**
✅ **ビルドサイズとメモリ使用量を削減**

## 使用方法

### 1. ShaderVariantCollectionの作成

#### 方法A: エディタツールを使用（推奨）

1. Unity Editorで `Tools > Natane > Shader Variant Collector` を開く
2. "Create New Collection" をクリック
3. 保存場所を選択（推奨: `Assets/ShaderVariants/`）

#### 方法B: 手動作成

1. Projectウィンドウで右クリック
2. `Create > Shader Variant Collection` を選択
3. `NataneToonShaderVariants` と名前を付ける

### 2. Variantsの収集

#### エディタツールで収集（推奨）

1. `Tools > Natane > Shader Variant Collector` を開く
2. 作成したCollectionを選択
3. 必要なオプションをチェック：
   - **Include Basic Variants**: 基本的な組み合わせ（推奨）
   - **Include Advanced Variants**: 高度な機能の組み合わせ
   - **Include Virtual Expression**: バーチャル表現機能
4. "Collect Variants" をクリック

推奨設定で約100-200のvariantsが収集されます。

#### 手動で収集

1. シーン内でNatane Toon Shaderを使用したマテリアルを配置
2. `Edit > Rendering > Shader Stripping` で "Log Shader Compilation" を有効化
3. Play Modeに入り、すべての機能を表示
4. Console に出力されたvariantsを確認
5. ShaderVariantCollection に手動で追加

### 3. Prewarmingの設定

#### 方法A: スクリプトで自動Prewarming

1. 空のGameObjectを作成
2. `ShaderPrewarming` スクリプトをアタッチ
3. `Shader Variants` フィールドに作成したCollectionをアサイン
4. `Prewarm On Awake` にチェック

```csharp
public class ShaderPrewarming : MonoBehaviour
{
    public ShaderVariantCollection shaderVariants;
    public bool prewarmOnAwake = true;

    void Awake()
    {
        if (prewarmOnAwake && shaderVariants != null)
        {
            shaderVariants.WarmUp();
        }
    }
}
```

#### 方法B: 手動でPrewarming

```csharp
ShaderVariantCollection collection = Resources.Load<ShaderVariantCollection>("NataneToonShaderVariants");
collection.WarmUp();
```

### 4. ビルド設定

#### Graphics Settingsでの設定

1. `Edit > Project Settings > Graphics` を開く
2. `Preloaded Shaders` に作成したCollectionを追加
3. これにより、ビルド時に自動的にvariantsがプリロードされる

#### Shader Stripping設定

1. `Edit > Project Settings > Graphics` を開く
2. `Shader Stripping` セクションで以下を設定：
   - `Lightmap Modes`: カスタム設定（不要なものを除外）
   - `Fog Modes`: 使用するフォグモードのみ
   - `Instancing Variants`: Keep All（GPU Instancing使用時）

## Variant収集オプション

### Basic Variants（基本）

最もよく使われる組み合わせ：
- No features（すべてOFF）
- Normal Map のみ
- Emission のみ
- Emission + Normal Map
- Use Ramp のみ
- Use Ramp + Normal Map

**推奨度**: ★★★★★
**Variant数**: 約18個（3シェーダー × 6組み合わせ）

### Advanced Variants（高度）

高度な機能の組み合わせ：
- Specular
- Rim Light
- MatCap
- SSS (Subsurface Scattering)
- SSS + Thickness Map
- 上記の組み合わせ

**推奨度**: ★★★★☆
**Variant数**: 約90個（3シェーダー × 30組み合わせ）

### Virtual Expression Variants（バーチャル表現）

VRChat向けの機能：
- Dissolve
- Hue Shift
- Emission Scroll
- Emission Pulse
- 上記の組み合わせ

**推奨度**: ★★★☆☆（VRChat使用時は★★★★★）
**Variant数**: 約60個（3シェーダー × 20組み合わせ）

### All Combinations（全組み合わせ）

⚠️ **警告**: すべての組み合わせを含めます

**推奨度**: ★☆☆☆☆
**Variant数**: 約24,000個以上
**用途**: テストやデバッグのみ。本番ビルドでは使用しないでください。

## 推奨設定

### VRChatアバター用

```
✓ Include Basic Variants
✓ Include Advanced Variants
✓ Include Virtual Expression Variants
✗ Include All Combinations
```

**合計**: 約150-200 variants
**ビルドサイズ増加**: 約10-20 MB

### 一般的なゲーム用

```
✓ Include Basic Variants
✓ Include Advanced Variants
✗ Include Virtual Expression Variants
✗ Include All Combinations
```

**合計**: 約100-150 variants
**ビルドサイズ増加**: 約5-15 MB

### モバイル/WebGL用（最小化）

```
✓ Include Basic Variants
✗ Include Advanced Variants
✗ Include Virtual Expression Variants
✗ Include All Combinations
```

**合計**: 約20-30 variants
**ビルドサイズ増加**: 約2-5 MB

## パフォーマンス最適化Tips

### 1. 不要なKeywordの削除

使用しない機能は、シェーダーから完全に削除することを検討：

```hlsl
// 例: MatCapを使用しない場合、以下をコメントアウト
// #pragma shader_feature _MATCAP
```

### 2. Multi-compileをShader_featureに変更

すべてのvariantsを含める必要がない場合：

```hlsl
// Before
#pragma multi_compile _ _NORMALMAP

// After
#pragma shader_feature _NORMALMAP
```

### 3. Stripping設定の最適化

Project Settings > Graphics > Shader Strippingで：
- **Lightmap Modes**: Custom（使用するもののみ）
- **Fog Modes**: Custom（使用するもののみ）
- **Instancing Variants**: Strip Unused（GPU Instancing不使用時）

### 4. ビルド後の分析

ビルドログを確認して、実際に含まれたvariant数を確認：

1. Build SettingsでDevelopment Buildを有効化
2. ビルド実行
3. Editor.logでshader variant情報を確認

## トラブルシューティング

### Q: ビルド後に特定の機能が動作しない

A: 該当するshader variantがCollectionに含まれていない可能性があります。
- エディタツールで該当する組み合わせを追加
- または、`Include All Combinations`で一度ビルドして確認

### Q: ビルドサイズが大きすぎる

A: 以下を確認：
- `Include All Combinations`がOFFか確認
- Graphics SettingsのPreloaded Shadersを確認
- 使用していないシェーダーバリアント（Cutout/Transparent）を削除

### Q: 実行時にスタッターが発生する

A: Prewarmingが正しく動作しているか確認：
- ShaderPrewarmingスクリプトがアタッチされているか
- CollectionがAssignされているか
- Awake/Startで実行されているか
- ログでWarmUp完了を確認

### Q: 収集されたvariant数が予想と異なる

A: エディタツールのEstimated Variantsは概算です。実際の数は：
- 重複排除により減少する可能性
- ForwardBase/ForwardAddの両方を含むため2倍になる
- 3つのシェーダーバリアント分含まれる

## まとめ

Shader Variant最適化は、以下のバランスを取ることが重要です：

📊 **ビルドサイズ** ↔️ **機能の完全性**
⚡ **ロード時間** ↔️ **実行時の柔軟性**
💾 **メモリ使用量** ↔️ **品質**

推奨設定：
1. **開発中**: Include All Combinations（全機能テスト）
2. **最適化段階**: エディタツールで必要なvariantsのみ収集
3. **リリース**: 最小限のvariantsで最終ビルド

適切なShaderVariantCollectionの使用により、**ビルドサイズを50-80%削減**、**ロード時間を大幅に短縮**できます。
