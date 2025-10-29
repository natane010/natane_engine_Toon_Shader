# ランプテクスチャ作成ガイド

ランプテクスチャは、ライティングを細かく制御するためのグラデーションテクスチャです。

## ランプテクスチャとは

ランプテクスチャは、光の強度（NdotL: 法線とライト方向の内積）を色にマッピングするための1次元的なルックアップテクスチャです。

```
暗部（左） ←――――――――――――――――→ 明部（右）
[影の色]    [中間色]    [ハイライト色]
```

## 基本的な作成方法

### Photoshop / GIMP / Krita

1. **新規画像を作成**
   - サイズ: 256x4 ピクセル（または512x4）
   - カラーモード: RGB

2. **グラデーションを描画**
   - ツール: グラデーションツール
   - 方向: 左から右へ水平
   - 左側: 暗い色（影）
   - 右側: 明るい色（光）

3. **エクスポート**
   - フォーマット: PNG or TGA
   - 圧縮: なし（可能な場合）

4. **Unityでインポート**
   - Wrap Mode: **Clamp**（重要！）
   - Filter Mode: Bilinear
   - Max Size: 256 or 512

### Unity Gradient Tool（スクリプト）

```csharp
// ランプテクスチャ生成スクリプト例
using UnityEngine;
using UnityEditor;

public class RampTextureGenerator
{
    [MenuItem("Tools/Generate Ramp Texture")]
    static void GenerateRampTexture()
    {
        int width = 256;
        int height = 4;

        Texture2D rampTex = new Texture2D(width, height, TextureFormat.RGB24, false);

        // グラデーション定義
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 0.3f, 0.4f), 0.0f),  // 影
                new GradientColorKey(new Color(0.7f, 0.7f, 0.8f), 0.5f),  // 中間
                new GradientColorKey(Color.white, 1.0f)                    // 光
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            }
        );

        // ピクセルを埋める
        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            Color color = gradient.Evaluate(t);

            for (int y = 0; y < height; y++)
            {
                rampTex.SetPixel(x, y, color);
            }
        }

        rampTex.Apply();

        // 保存
        byte[] bytes = rampTex.EncodeToPNG();
        System.IO.File.WriteAllBytes("Assets/Textures/Ramps/CustomRamp.png", bytes);
        AssetDatabase.Refresh();

        Debug.Log("Ramp texture generated!");
    }
}
```

## プリセットランプ例

### 1. シンプル2段階
```
0.0 - 0.5: RGB(0.6, 0.6, 0.7) 影
0.5 - 1.0: RGB(1.0, 1.0, 1.0) 光
```
**用途**: 基本的なセルシェーディング

### 2. 3段階グラデーション
```
0.0 - 0.3: RGB(0.4, 0.4, 0.5) 暗部
0.3 - 0.6: RGB(0.7, 0.7, 0.75) 中間
0.6 - 1.0: RGB(1.0, 1.0, 1.0) 明部
```
**用途**: より複雑な陰影表現

### 3. アニメ調（暖色）
```
0.0 - 0.4: RGB(0.6, 0.5, 0.4) 暖かい影
0.4 - 0.7: RGB(0.9, 0.85, 0.8) 中間
0.7 - 1.0: RGB(1.0, 1.0, 0.95) 明部（少し暖色）
```
**用途**: 肌、暖かいキャラクター

### 4. クール（寒色）
```
0.0 - 0.4: RGB(0.4, 0.45, 0.6) 青みがかった影
0.4 - 0.7: RGB(0.7, 0.75, 0.85) 中間
0.7 - 1.0: RGB(0.95, 0.95, 1.0) 明部（少し青白い）
```
**用途**: 金属、冷たい質感

### 5. ハイコントラスト
```
0.0 - 0.45: RGB(0.2, 0.2, 0.3) 濃い影
0.45 - 0.55: RGB(0.5, 0.5, 0.6) 急激な遷移
0.55 - 1.0: RGB(1.0, 1.0, 1.0) 明るいハイライト
```
**用途**: ドラマチックな表現、強いコントラスト

### 6. ソフトグラデーション
```
スムーズなグラデーション:
0.0: RGB(0.5, 0.5, 0.6)
0.25: RGB(0.65, 0.65, 0.7)
0.5: RGB(0.8, 0.8, 0.85)
0.75: RGB(0.9, 0.9, 0.95)
1.0: RGB(1.0, 1.0, 1.0)
```
**用途**: リアル寄りのトゥーン

## 高度なテクニック

### マルチステップランプ
複数の段階を持つランプで、複雑なライティングを表現:

```
0.0 - 0.2: 非常に暗い影（コアシャドウ）
0.2 - 0.4: 影
0.4 - 0.6: 中間調
0.6 - 0.8: 明部
0.8 - 1.0: ハイライト
```

### カラフルランプ
影に色を付けて独特の表現:

```
0.0 - 0.3: RGB(0.4, 0.3, 0.6) 紫がかった影
0.3 - 0.6: RGB(0.7, 0.6, 0.7) 淡い紫
0.6 - 1.0: RGB(1.0, 0.95, 1.0) 明部
```

### 逆ランプ（リムライト効果）
通常とは逆のグラデーション:

```
0.0: RGB(1.0, 1.0, 1.0) 明るい（影側が明るい）
1.0: RGB(0.5, 0.5, 0.6) 暗い（光側が暗い）
```

## Tips

### 1. Wrap ModeをClampに設定
必ず **Clamp** に設定してください。Repeatにすると端が繰り返され、意図しない結果になります。

### 2. 低解像度でOK
256x4や128x4で十分です。高解像度は不要です。

### 3. 影の色は黒を避ける
完全な黒（RGB 0, 0, 0）ではなく、少し明るい色を使うと自然に見えます。

### 4. 彩度を持たせる
グレースケールだけでなく、少し色を加えると表現力が増します。

### 5. 参照を作る
好きなアニメやゲームのスクリーンショットから影の色をスポイトで取ると良いです。

## トラブルシューティング

### 境界線がぼやける
- Filter Modeを Point にしてみる（ピクセルアート風）
- または、ランプテクスチャのコントラストを上げる

### 色が意図と違う
- Unityインポート設定でsRGBになっているか確認
- Color Space: Gamma or Linear に応じて調整

### ランプが効かない
- シェーダーで `Use Ramp Texture` にチェックが入っているか確認
- Wrap Mode が Clamp になっているか確認

## サンプルランプテクスチャの場所

このプロジェクトのサンプルランプテクスチャは:
```
Assets/Textures/Ramps/
```
に配置することを推奨します。

## 次のステップ

ランプテクスチャをマスターしたら:
1. キャラクターの部位ごとに異なるランプを試す
2. 時間帯によってランプを切り替える
3. ランプをアニメーションさせて特殊効果を作る

楽しいランプテクスチャライフを！
