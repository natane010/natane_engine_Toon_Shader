# Natane Particle System - 使用ガイド

Unity のパーティクルシステムを使って簡単にエフェクトを作成できるエディタ拡張です。

## 🎯 特徴

- **ビジュアルエディタ**: 直感的なGUIでパーティクルエフェクトを作成
- **プリセットシステム**: 再利用可能なエフェクト設定を保存
- **リアルタイムプレビュー**: 変更を即座に確認
- **豊富なテンプレート**: 爆発、炎、煙、魔法など7種類のテンプレート
- **簡単なスポーン**: スクリプトから簡単にエフェクトを生成

## 📦 ファイル構成

```
Assets/
├── Scripts/ParticleSystem/
│   ├── ParticleEffectPreset.cs          # プリセットデータ（ScriptableObject）
│   ├── ParticleAutoDestroy.cs           # 自動削除コンポーネント
│   └── ParticleEffectSpawner.cs         # エフェクトスポナー
└── Editor/ParticleSystem/
    └── ParticleEffectEditorWindow.cs    # エディタウィンドウ
```

## 🚀 使い方

### 1. エディタウィンドウを開く

```
メニュー: Tools > Natane > Particle Effect Editor
```

### 2. 新しいプリセットを作成

1. **New Preset** ボタンをクリック
2. 保存場所とファイル名を指定
3. プリセットが作成され、エディタに読み込まれます

### 3. テンプレートから開始（推奨）

1. **Load Template** ボタンをクリック
2. 以下から選択：
   - **Explosion** - 爆発エフェクト
   - **Fire** - 炎エフェクト
   - **Smoke** - 煙エフェクト
   - **Magic Sparkles** - 魔法のきらめき
   - **Electric** - 電撃エフェクト
   - **Water Splash** - 水しぶき
   - **Heal Effect** - 回復エフェクト
   - **Custom** - カスタム（空白）

### 4. パラメータを調整

#### Main Settings（基本設定）
- **Duration**: エフェクトの持続時間
- **Looping**: ループ再生するか
- **Start Lifetime**: パーティクルの寿命
- **Start Speed**: 初速度
- **Start Size**: 初期サイズ
- **Start Color**: 初期色
- **Gravity**: 重力の影響
- **Max Particles**: 最大パーティクル数

#### Emission（発生）
- **Rate Over Time**: 毎秒の発生数
- **Use Burst**: バースト発生を使用
  - **Burst Count**: バースト時の発生数
  - **Burst Time**: バースト発生時刻

#### Shape（形状）
- **Shape Type**: 発生形状（コーン、球など）
- **Angle**: 発生角度
- **Radius**: 半径

#### Color Over Lifetime（色の変化）
- **Enable**: 色変化を有効化
- **Color Gradient**: 色のグラデーション

#### Size Over Lifetime（サイズの変化）
- **Enable**: サイズ変化を有効化
- **Size Curve**: サイズのカーブ

#### Velocity Over Lifetime（速度の変化）
- **Enable**: 速度変化を有効化
- **Velocity**: 速度ベクトル

#### Rotation（回転）
- **Enable**: 回転を有効化
- **Rotation Speed**: 回転速度（度/秒）

#### Renderer（レンダリング）
- **Render Mode**: レンダリングモード
- **Material**: マテリアル

#### Trails（軌跡）
- **Enable**: 軌跡を有効化
- **Lifetime**: 軌跡の寿命
- **Min Vertex Distance**: 最小頂点距離
- **Trail Material**: 軌跡用マテリアル

### 5. プレビュー

- **Play**: エフェクトを再生
- **Stop**: 停止
- **Restart**: 再起動
- **Auto Play on Change**: 変更時に自動再生

### 6. 保存と適用

- **Save**: プリセットを保存
- **Apply to Scene**: 選択中のオブジェクトに適用（なければ新規作成）

## 💻 スクリプトからの使用

### ParticleEffectSpawner コンポーネントを使用

```csharp
using NataneParticleSystem;
using UnityEngine;

public class EffectExample : MonoBehaviour
{
    [SerializeField] private ParticleEffectSpawner spawner;

    void Start()
    {
        // デフォルトエフェクトをスポーン
        spawner.SpawnDefaultEffect();

        // 名前を指定してスポーン
        spawner.SpawnEffect("Explosion");

        // 位置を指定してスポーン
        spawner.SpawnEffect("Fire", transform.position);
    }
}
```

### プリセットから直接生成

```csharp
using NataneParticleSystem;
using UnityEngine;

public class DirectSpawn : MonoBehaviour
{
    [SerializeField] private ParticleEffectPreset explosionPreset;

    void OnCollisionEnter(Collision collision)
    {
        // 衝突位置に爆発エフェクトを生成
        Vector3 position = collision.contacts[0].point;
        explosionPreset.CreateParticleEffect(position, Quaternion.identity);
    }
}
```

### 既存のParticleSystemに適用

```csharp
using NataneParticleSystem;
using UnityEngine;

public class ApplyPreset : MonoBehaviour
{
    [SerializeField] private ParticleEffectPreset preset;
    [SerializeField] private ParticleSystem targetPS;

    void Start()
    {
        // 既存のパーティクルシステムにプリセットを適用
        preset.ApplyToParticleSystem(targetPS);
    }
}
```

## 🎨 テンプレート詳細

### Explosion（爆発）
- **用途**: 爆発、衝撃波
- **特徴**: バースト発生、球形拡散、色が白→オレンジ→黒に変化

### Fire（炎）
- **用途**: 炎、松明、燃焼エフェクト
- **特徴**: 連続発生、上昇、色が黄→オレンジ→赤に変化

### Smoke（煙）
- **用途**: 煙、蒸気、霧
- **特徴**: ゆっくり上昇、徐々に拡大、回転

### Magic Sparkles（魔法のきらめき）
- **用途**: 魔法エフェクト、回復、バフ
- **特徴**: 球形発生、上昇、回転、青〜紫のグラデーション

### Electric（電撃）
- **用途**: 電気、稲妻、エネルギー
- **特徴**: 短寿命、青白い発光、ランダム配置

### Water Splash（水しぶき）
- **用途**: 水しぶき、雨、水中エフェクト
- **特徴**: バースト、重力で落下、青色

### Heal Effect（回復）
- **用途**: 回復魔法、ヒール、バフ
- **特徴**: 上昇、緑→黄のグラデーション、柔らかい動き

## 🔧 カスタマイズのヒント

### 爆発エフェクトをカスタマイズ
1. **Explosion** テンプレートから開始
2. **Burst Count** を増やして密度を上げる
3. **Start Speed** を調整して広がり方を変更
4. **Color Gradient** で色を変更（火炎弾なら赤系、氷なら青系）

### 連続エフェクト（炎、煙など）
1. **Fire** または **Smoke** テンプレートから開始
2. **Looping** を有効化
3. **Emission Rate** で密度を調整
4. **Velocity Over Lifetime** で動きをつける

### 魔法エフェクト
1. **Magic Sparkles** テンプレートから開始
2. **Color Gradient** で魔法の属性色に変更
3. **Rotation Speed** で回転を調整
4. **Trails** を有効化して軌跡をつける

## ⚡ パフォーマンス最適化

1. **Max Particles** を適切に設定
   - モバイル: 50〜200
   - PC: 200〜1000
   - 背景エフェクト: 控えめに

2. **Looping** エフェクトは慎重に使用
   - 必要なときだけPlay/Stop
   - 不要になったら必ずDestroy

3. **Auto Destroy** を活用
   - ワンショットエフェクトは自動削除
   - メモリリークを防止

## 🐛 トラブルシューティング

### エフェクトが見えない
- **Material** が設定されているか確認
- **Start Color** のアルファ値を確認
- **Max Particles** が0でないか確認
- カメラの向きと位置を確認

### エフェクトが消えない
- **Looping** が有効になっていないか確認
- **ParticleAutoDestroy** コンポーネントが付いているか確認
- スクリプトから明示的にDestroyしているか確認

### パフォーマンスが悪い
- **Max Particles** を減らす
- 同時に再生するエフェクト数を制限
- **Emission Rate** を下げる
- 不要なモジュールを無効化

## 📚 参考リンク

- [Unity Particle System マニュアル](https://docs.unity3d.com/Manual/ParticleSystems.html)
- [Unity Particle System スクリプトリファレンス](https://docs.unity3d.com/ScriptReference/ParticleSystem.html)

## 🎓 チュートリアル

### 基本的なエフェクトの作成

1. エディタを開く
2. **New Preset** で新規作成
3. **Load Template** から「Explosion」を選択
4. パラメータを調整：
   - Start Size: 0.3 → 1.0 に変更（大きい爆発）
   - Burst Count: 100 → 200 に変更（密度を上げる）
5. **Save** で保存
6. **Apply to Scene** でシーンに配置
7. Playボタンで確認

### カスタム魔法エフェクトの作成

1. **Load Template** から「Magic Sparkles」を選択
2. Color Gradientを変更：
   - 開始: 明るい紫（#FF00FF）
   - 終了: 濃い青（#0000FF）
3. Shape設定を変更：
   - Shape Type: Cone
   - Angle: 5度（細いビーム）
4. Trails を有効化：
   - Enable: チェック
   - Lifetime: 0.5
5. 保存して使用

## 💡 Tips

- **プリセットは再利用**: 一度作ったプリセットは何度でも使用可能
- **テンプレートから始める**: ゼロから作るよりテンプレートをカスタマイズする方が早い
- **プレビューを活用**: Auto Play on Change をオンにして即座に確認
- **グループ化**: 複数のエフェクトを組み合わせて複雑な表現を作成
- **マテリアルが重要**: 適切なマテリアルで見た目が大きく変わる

## 🎯 今後の拡張予定

- [ ] Sub Emitter対応
- [ ] Collision Module対応
- [ ] Noise Module対応
- [ ] エフェクトライブラリパネル
- [ ] プリセットのプレビューサムネイル
- [ ] エフェクトの組み合わせ機能
