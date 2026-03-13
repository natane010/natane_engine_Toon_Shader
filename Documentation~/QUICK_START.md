# クイックスタートガイド

Natane Toon Shaderを5分で始めるための簡単なガイドです。

## ステップ1: インストール

1. このリポジトリをクローンまたはダウンロード
2. Unityプロジェクトを開く
3. `Assets` フォルダの内容をプロジェクトの `Assets` フォルダにコピー

## ステップ2: 最初のマテリアル作成

1. Projectウィンドウで右クリック
2. `Create` → `Material` を選択
3. 作成されたマテリアルを選択
4. Inspectorの `Shader` ドロップダウンをクリック
5. `Natane` → `Toon Shader` を選択

## ステップ3: クイックセットアップ（推奨）

マテリアルを作成したら、インスペクターの一番上にある「クイックセットアップ」セクションを使うのが最も簡単です。

### ガイド付きモード（デフォルト）

3ステップで基本ルックが完成します:

1. **用途を選択**: キャラクター / 背景・小物 / エフェクト
2. **スタイルを選択**: 以下のプリセットからワンクリックで適用（Ctrl+Zで戻せます）
   - シャープなアニメ調
   - 柔らかい塗り調
   - ⭐ ゲームキャラクター風（2段影＋リムライト＋スペキュラー＋アウトライン）
   - Toon-PBR ハイブリッド
   - Near PBR
3. **質感を調整**: マット / グロッシー

### 全表示モード

「全表示」タブに切り替えると、全プリセットボタンが一度に表示されます。
各ボタンにマウスを重ねると、適用されるパラメータ一覧がツールチップで確認できます。

## ステップ4: 手動で基本設定（オプション）

### シンプルなセルシェーディング
```
[Main Texture]
- Color: 白 (1, 1, 1, 1)

[Shading]
- Shadow Steps: 3
- Shadow Sharpness: 0.1
- Shadow Color: 薄いグレー (0.6, 0.6, 0.65, 1)
- Shadow Offset: 0
```

これで基本的なセルシェーディングが完成です！

## ステップ5: エフェクトを追加（オプション）

### アウトラインを追加
```
[Outline]
- Enable Outline: チェック
- Outline Width: 0.01
- Outline Color: 黒 (0, 0, 0, 1)
```

### リムライトを追加
```
[Rim Light]
- Enable Rim Light: チェック
- Rim Color: 白または明るい色
- Rim Power: 3
- Rim Intensity: 1
```

### スペキュラハイライトを追加
```
[Specular]
- Enable Specular: チェック
- Specular Color: 白 (1, 1, 1, 1)
- Specular Size: 0.1
- Specular Softness: 0.05
```

## ステップ6: マテリアルを適用

1. Hierarchyでオブジェクトを選択
2. Inspector で作成したマテリアルをドラッグ＆ドロップ

完成です！

## おすすめプリセット

### アニメキャラクター（肌）
```
Shadow Steps: 2
Shadow Sharpness: 0.05
Shadow Color: RGB(0.7, 0.65, 0.6)
Rim Light: 有効、白色、Power 4
Specular: 無効
```

### アニメキャラクター（髪）
```
Shadow Steps: 2
Shadow Sharpness: 0.1
Shadow Color: RGB(0.5, 0.5, 0.55)
Rim Light: 有効、白色、Power 3
Specular: 有効、Size 0.08, Softness 0.03
MatCap: オプション（光沢用）
```

### アニメキャラクター（服）
```
Shadow Steps: 2
Shadow Sharpness: 0.15
Shadow Color: RGB(0.5, 0.5, 0.6)
Rim Light: 有効、淡い色、Power 5
Specular: 無効
```

### ロボット/メカ
```
Shadow Steps: 3
Shadow Sharpness: 0.2
Shadow Color: RGB(0.4, 0.4, 0.5)
Rim Light: 有効、青白い色、Power 2
Specular: 有効、Size 0.15, Softness 0.08
MatCap: 有効（金属質感）
```

### ゲームキャラクター風（⭐おすすめ）
```
Shadow Steps: 2
Shadow Sharpness: 0.03
Shadow Color: RGB(0.62, 0.52, 0.54)
Rim Light: 有効、白色、Power 2.5、Intensity 1.5
Specular: 有効、Size 0.08、Softness 0.05、Intensity 1.2
Outline: 有効、Width 0.08、テクスチャカラー連動
```
Quick Setupの「⭐ ゲームキャラクター風」ボタンでワンクリック設定できます。

## 次のステップ

より詳しい情報は [README.md](README.md) を参照してください。

### インスペクターの便利機能

- **Ctrl+1~5**: タブ切替ショートカット
- **▼ ジャンプメニュー**: 検索バー横のボタンから、現在タブのセクションにワンクリックジャンプ
- **🔍 全タブ検索**: 検索バーでセクション名やキーワードを入力すると全タブ横断で検索
- **機能一覧**: 各機能のON/OFF状態を一目で確認、クリックでトグル可能

### VRChat PCアップロード前チェック

- PC版VRChatアバターへ組み込む前に [VRCHAT_PC_UPLOAD_CHECKLIST.md](VRCHAT_PC_UPLOAD_CHECKLIST.md) を確認してください
- `推定 Sampler 負荷` が危険寄りのままアップロードしないのが安全です
- `Light Volume` / `LTCGI` は対応パッケージと使用ワールドが揃っているときだけ有効化するのがよきです

### さらに学ぶ
- カスタムランプテクスチャの作成
- 法線マップの使用
- MatCapテクスチャの活用
- 複数ライトの設定

楽しいシェーダーライフを！
