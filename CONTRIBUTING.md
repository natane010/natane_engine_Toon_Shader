# 貢献ガイド

Natane Toon Shaderへの貢献を歓迎します！このドキュメントでは、プロジェクトに貢献する方法を説明します。

## 貢献の方法

### バグ報告

バグを見つけた場合は、以下の情報を含めてIssueを作成してください：

1. **環境情報**
   - Unityバージョン
   - レンダーパイプライン（Built-in）
   - OS（Windows/Mac/Linux）
   - グラフィックスAPI（DirectX/OpenGL/Metal/Vulkan）

2. **再現手順**
   - バグを再現する具体的な手順
   - 使用しているマテリアル設定
   - スクリーンショットがあれば添付

3. **期待される動作**
   - 何が起こるべきだったか

4. **実際の動作**
   - 実際に何が起こったか

### 機能リクエスト

新機能の提案は歓迎します！以下を含めてください：

1. **機能の説明**
   - 何を実現したいか
   - なぜその機能が必要か

2. **使用例**
   - どのような場面で使用するか
   - 既存の機能で代替できないか

3. **参考資料**
   - 他のシェーダーでの実装例
   - 技術資料やチュートリアルのリンク

## プルリクエストのガイドライン

### 開発環境のセットアップ

1. このリポジトリをフォーク
2. フォークをクローン
```bash
git clone https://github.com/YOUR_USERNAME/natane_engine_Toon_Shader.git
cd natane_engine_Toon_Shader
```

3. 新しいブランチを作成
```bash
git checkout -b feature/your-feature-name
```

### コーディング規約

#### シェーダーコード

```csharp
// 良い例
float CalculateRimLight(float3 normal, float3 viewDir)
{
    float rim = 1.0 - saturate(dot(normal, viewDir));
    return pow(rim, _RimPower) * _RimIntensity;
}

// 変数名は意味のある名前を使用
float ndotl = dot(normal, lightDir);  // Good
float x = dot(normal, lightDir);      // Bad
```

**ルール**:
- 関数名: PascalCase（例: `CalculateToonShading`）
- 変数名: camelCase（例: `worldNormal`, `ndotl`）
- 定数: UPPER_SNAKE_CASE（例: `MAX_LIGHTS`）
- インデント: 4スペース
- コメント: 複雑な処理には説明を追加

#### C#コード (ShaderGUI)

```csharp
// Unityの標準規約に従う
public class NataneToonShaderGUI : ShaderGUI
{
    private MaterialProperty[] properties;  // private: camelCase
    private static bool showSettings = true;

    // メソッド: PascalCase
    private void DrawMainTextureSection()
    {
        // ...
    }
}
```

### コミットメッセージ

明確で説明的なコミットメッセージを書いてください：

```
feat: Add subsurface scattering support
fix: Correct outline rendering on mobile devices
docs: Update quick start guide
refactor: Optimize specular calculation
perf: Reduce texture lookups in fragment shader
```

**プレフィックス**:
- `feat`: 新機能
- `fix`: バグ修正
- `docs`: ドキュメント変更
- `refactor`: リファクタリング
- `perf`: パフォーマンス改善
- `test`: テスト追加
- `chore`: ビルド/ツール関連

### プルリクエストのプロセス

1. **変更を加える**
   - コードを書く
   - テストする（複数のUnityバージョン、デバイスで確認）

2. **コミット**
```bash
git add .
git commit -m "feat: Add your feature description"
```

3. **プッシュ**
```bash
git push origin feature/your-feature-name
```

4. **プルリクエストを作成**
   - GitHubでプルリクエストを作成
   - 変更内容を明確に説明
   - スクリーンショットや動画を添付（該当する場合）

5. **レビュー待ち**
   - レビュアーからのフィードバックに対応
   - 必要に応じて修正

### テストの実施

プルリクエストを送る前に、以下をテストしてください：

- [ ] Unityでシェーダーが正しくコンパイルされる
- [ ] マテリアルインスペクタが正しく表示される
- [ ] すべての機能が期待通りに動作する
- [ ] パフォーマンスに問題がない
- [ ] ドキュメントを更新した（新機能の場合）

## 開発のヒント

### シェーダーデバッグ

```csharp
// デバッグ用の可視化
// Normal
return fixed4(worldNormal * 0.5 + 0.5, 1);

// NdotL
return fixed4(ndotl.xxx, 1);

// Attenuation
return fixed4(atten.xxx, 1);
```

### パフォーマンステスト

- Unity Profilerを使用
- Frame Debuggerで描画パスを確認
- モバイルデバイスで実機テスト

## コミュニティ

- 質問や議論は GitHub Discussions で
- バグ報告は GitHub Issues で
- コードレビューは丁寧に、建設的に

## 行動規範

- 敬意を持って対応する
- 建設的なフィードバックを心がける
- 多様性を尊重する
- オープンで協力的な態度を保つ

## ライセンス

貢献したコードは、プロジェクトのライセンス（MIT License）の下で公開されることに同意したものとみなします。

## 質問がある場合

わからないことがあれば、遠慮なく Issue で質問してください！

## 謝辞

すべての貢献者に感謝します！

---

Happy coding! 🎨
