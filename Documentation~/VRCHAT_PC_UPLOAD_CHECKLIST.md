# VRChat PCアップロード前チェックリスト

Natane Toon Shader を **PC版VRChatアバター** で使う前に確認したい項目です。
Quest/Android 向けの最適化や互換性は別途確認してください。

## 最低限ここだけ

- [ ] Unity Console に **Shader error / compile error** が出ていない
- [ ] アバター上で **ピンク表示のマテリアルがない**
- [ ] 使用マテリアルが `Natane/Toon Shader` 系、または意図した Natane 系シェーダーになっている
- [ ] Inspector 上部の **推定 Sampler 負荷** が上限超過になっていない
- [ ] `Light Volume` / `LTCGI` を使う場合、プロジェクト側でパッケージ検出済み
- [ ] `Tools > Natane > マテリアル Material > マテリアル検証 Material Validator` で問題を確認済み

## 詳細チェック

### 1. シェーダーコンパイル

- Unity Console に赤いエラーが残っていないことを確認します
- 特に以下が出ていたらアップロード前に止めます
  - `Shader error`
  - `undeclared identifier`
  - `sampler register index exceeded`
  - `Parse error`
- シーンビューか Game ビューで、アバターを回して **ピンク化・真っ黒・法線崩れ** がないか確認します

### 2. マテリアル割り当て

- アバターの Renderer に割り当たっているマテリアルが、意図した Natane シェーダーバリアントか確認します
- 迷ったら以下を基準にします
  - 通常: `Natane/Toon Shader`
  - カットアウト: `Natane/Toon Shader (Cutout)`
  - 透過: `Natane/Toon Shader (Transparent)`
  - 毛皮: `Natane/Toon Shader (Fur)`
- 透過を使わなくてよい部位は、できるだけ Opaque / Cutout に戻します

### 3. 推定 Sampler 負荷

- Inspector 上部の **推定 Sampler 負荷** を確認します
- `上限` またはそれに近い状態なら、重い機能の同時使用を減らします
- 特に詰まりやすい組み合わせ:
  - `Light Volume` + `LTCGI`
  - `Hatching` + 複数追加テクスチャ
  - `Hair Specular` + `Screen Tone` + `Iridescence`
- 髪や衣装で mask を多用する場合も、表示が危険寄りなら一度見直します

### 4. VRChat連携オプション

- `Light Volume` は、VRC Light Volumes パッケージが検出されているときだけ有効化します
- `LTCGI` は、LTCGI パッケージが検出されているときだけ有効化します
- どちらも **対応ワールドで使う予定がないなら OFF 推奨** です
- 迷ったら以下を実行します
  - `Tools > Natane > VRChat > VRC Light Volumes 再検出`
  - `Tools > Natane > VRChat > LTCGI 再検出`

### 5. Natane ツールでの確認

- `Tools > Natane > マテリアル Material > マテリアル検証 Material Validator` を開きます
- アバター配下のマテリアルを検証して、重大な警告がないか確認します
- 必要なら `Tools > Natane > 最適化 Optimization > パフォーマンスバジェット Performance Budget Tool` で全体傾向も見ます

### 6. 初回アップロード前の安定化

- 多機能マテリアルを多く使う場合は、Shader のプリウォームを検討します
- 必要に応じて以下を使います
  - `Tools > Natane > シェーダー Shader > シェーダープリウォーミング Shader Prewarming > 設定 Settings`
  - `Tools > Natane > シェーダー Shader > シェーダーバリアント収集 Shader Variant Collector`
- 初回だけ見た目が崩れる、読み込み直後に一瞬差し替わる、みたいな違和感があるときに有効です

### 7. Unity上の最終目視

- Play Mode で以下を確認します
  - 正面 / 横 / 後ろで影が破綻していない
  - 顔、髪、衣装で光り方が極端に違和感ない
  - 透過やカットアウトが意図どおり
  - Emission / Rim / Outline が強すぎない
- ミラー想定の見た目も気になる場合は、鏡相当の確認環境で一度見ます

## PC向けの補足

- このチェックリストは **PC版VRChatアバター向け** です
- Quest/Android 版を同梱する場合は、別マテリアルや別シェーダー構成を用意してください
- PC で通っても、Quest では同じ構成をそのまま使えないことがあります
