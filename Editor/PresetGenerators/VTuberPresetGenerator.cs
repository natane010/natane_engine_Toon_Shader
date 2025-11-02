using UnityEngine;
using UnityEditor;
using NataneToon.MaterialSystem;
using System.IO;

namespace NataneToon.Editor
{
    /// <summary>
    /// Generator for VTuber-optimized material presets
    /// Creates professional presets for VTuber character rendering
    /// </summary>
    public static class VTuberPresetGenerator
    {
        private const string PRESET_PATH = "Assets/NataneToon/Runtime/Presets/VTuber/";

        [MenuItem("Natane/Generate VTuber Presets")]
        public static void GenerateAllPresets()
        {
            // Ensure directory exists
            if (!Directory.Exists(PRESET_PATH))
            {
                Directory.CreateDirectory(PRESET_PATH);
            }

            GenerateCharacterSkinPreset();
            GenerateCharacterHairPreset();
            GenerateCharacterClothingPreset();
            GenerateCharacterEyesPreset();
            GenerateLivePerformancePreset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[VTuberPresetGenerator] VTuber向けプリセットを生成しました！");
            EditorUtility.DisplayDialog("完了", "VTuber向けプリセットの生成が完了しました。\n\nAssets/NataneToon/Runtime/Presets/VTuber/ を確認してください。", "OK");
        }

        /// <summary>
        /// キャラクター肌用プリセット - 透明感のある柔らかい質感
        /// </summary>
        private static void GenerateCharacterSkinPreset()
        {
            var preset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
            preset.presetName = "VTuber - キャラクター肌";
            preset.description = "VTuberキャラクターの肌に最適化されたプリセット。\n" +
                               "・柔らかいセルシェーディング\n" +
                               "・SSSで透明感と血色感\n" +
                               "・リムライトで立体感\n" +
                               "・NiloToon互換の高品質レンダリング";
            preset.category = PresetCategory.Character_Skin;
            preset.author = "Natane Toon Shader";
            preset.version = "1.0";

            var p = preset.parameters;

            // 基本設定
            p.mainColor = new Color(1.0f, 0.95f, 0.9f, 1f); // 自然な肌色
            p.alpha = 1f;

            // シェーディング - NiloToonスタイル
            p.shadowColor = new Color(0.85f, 0.7f, 0.65f, 1f); // 温かみのある影
            p.toonSteps = 2; // クリーンなアニメ調
            p.toonSharpness = 0.08f; // シャープだが柔らかい境界
            p.shadowReceive = 0.8f; // 影を受けすぎない
            p.shadowIntensityMax = 0.3f; // 影を暗くしすぎない
            p.lightInfluence = 1.2f;
            p.backlight = 0.2f;

            // SSS - 肌の透明感
            p.useSSS = true;
            p.sssColor = new Color(1f, 0.6f, 0.5f, 1f); // 血色感
            p.sssIntensity = 0.4f;
            p.sssDistortion = 0.3f;
            p.sssPower = 2.5f;
            p.sssScale = 0.8f;

            // リムライト - 立体感
            p.useRimLight = true;
            p.rimColor = new Color(1f, 0.95f, 0.9f, 1f);
            p.rimIntensity = 0.6f;
            p.rimPower = 4f;

            // スペキュラー - 柔らかいハイライト
            p.useSpecular = true;
            p.specularColor = new Color(1f, 1f, 1f, 1f);
            p.specularIntensity = 0.3f;
            p.specularSize = 0.2f;
            p.specularSharpness = 0.4f;

            // アウトライン
            p.useOutline = true;
            p.outlineColor = new Color(0.3f, 0.2f, 0.15f, 1f); // 肌より暗いアウトライン
            p.outlineWidth = 0.003f; // 細めのアウトライン

            // レンダリング
            p.renderQueue = 2000;
            p.cullMode = 2; // Back

            AssetDatabase.CreateAsset(preset, PRESET_PATH + "VTuber_CharacterSkin.asset");
            Debug.Log("生成: VTuber - キャラクター肌プリセット");
        }

        /// <summary>
        /// キャラクター髪用プリセット - ツヤのあるアニメ調ヘア
        /// </summary>
        private static void GenerateCharacterHairPreset()
        {
            var preset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
            preset.presetName = "VTuber - キャラクター髪";
            preset.description = "VTuberキャラクターの髪に最適化されたプリセット。\n" +
                               "・シャープなハイライト\n" +
                               "・MatCapで光沢感\n" +
                               "・アニメ調のツヤ表現\n" +
                               "・NiloToon互換の高品質レンダリング";
            preset.category = PresetCategory.Character_Hair;
            preset.author = "Natane Toon Shader";
            preset.version = "1.0";

            var p = preset.parameters;

            // 基本設定
            p.mainColor = new Color(0.3f, 0.2f, 0.15f, 1f); // 濃い茶髪（例）
            p.alpha = 1f;

            // シェーディング - NiloToonスタイル
            p.shadowColor = new Color(0.15f, 0.1f, 0.08f, 1f);
            p.toonSteps = 2;
            p.toonSharpness = 0.05f; // よりシャープ
            p.shadowReceive = 0.9f;
            p.shadowIntensityMax = 0.2f;
            p.lightInfluence = 1.3f;
            p.backlight = 0.3f;

            // スペキュラー - アニメ調の強いハイライト
            p.useSpecular = true;
            p.specularColor = new Color(1f, 1f, 1f, 1f);
            p.specularIntensity = 1.2f;
            p.specularSize = 0.15f;
            p.specularSharpness = 0.9f; // シャープなハイライト

            // MatCap - 光沢感
            p.useMatCap = true;
            p.matCapIntensity = 0.4f;
            p.matCapBlendMode = 0; // Add

            // リムライト
            p.useRimLight = true;
            p.rimColor = new Color(0.8f, 0.7f, 0.6f, 1f);
            p.rimIntensity = 0.8f;
            p.rimPower = 3f;

            // アウトライン
            p.useOutline = true;
            p.outlineColor = new Color(0.1f, 0.05f, 0.05f, 1f);
            p.outlineWidth = 0.004f;

            // レンダリング
            p.renderQueue = 2000;
            p.cullMode = 0; // Off (両面描画で髪のボリューム感)

            AssetDatabase.CreateAsset(preset, PRESET_PATH + "VTuber_CharacterHair.asset");
            Debug.Log("生成: VTuber - キャラクター髪プリセット");
        }

        /// <summary>
        /// キャラクター服用プリセット - クリーンなアニメ調
        /// </summary>
        private static void GenerateCharacterClothingPreset()
        {
            var preset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
            preset.presetName = "VTuber - キャラクター服";
            preset.description = "VTuberキャラクターの服に最適化されたプリセット。\n" +
                               "・クリーンなセルシェーディング\n" +
                               "・明瞭なアウトライン\n" +
                               "・シンプルで美しい表現\n" +
                               "・NiloToon互換の高品質レンダリング";
            preset.category = PresetCategory.Character_Clothing;
            preset.author = "Natane Toon Shader";
            preset.version = "1.0";

            var p = preset.parameters;

            // 基本設定
            p.mainColor = new Color(0.9f, 0.9f, 0.95f, 1f); // 白い服（例）
            p.alpha = 1f;

            // シェーディング - NiloToonスタイル
            p.shadowColor = new Color(0.7f, 0.7f, 0.75f, 1f);
            p.toonSteps = 2;
            p.toonSharpness = 0.1f;
            p.shadowReceive = 1f;
            p.shadowIntensityMax = 0.25f;
            p.lightInfluence = 1f;
            p.backlight = 0.1f;

            // スペキュラー - 控えめ
            p.useSpecular = false;

            // リムライト - 輪郭の強調
            p.useRimLight = true;
            p.rimColor = new Color(1f, 1f, 1f, 1f);
            p.rimIntensity = 0.4f;
            p.rimPower = 5f;

            // アウトライン - しっかりした輪郭
            p.useOutline = true;
            p.outlineColor = new Color(0f, 0f, 0f, 1f);
            p.outlineWidth = 0.005f;

            // レンダリング
            p.renderQueue = 2000;
            p.cullMode = 2; // Back

            AssetDatabase.CreateAsset(preset, PRESET_PATH + "VTuber_CharacterClothing.asset");
            Debug.Log("生成: VTuber - キャラクター服プリセット");
        }

        /// <summary>
        /// キャラクター目用プリセット - 明るくキラキラした瞳
        /// </summary>
        private static void GenerateCharacterEyesPreset()
        {
            var preset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
            preset.presetName = "VTuber - キャラクター目";
            preset.description = "VTuberキャラクターの目に最適化されたプリセット。\n" +
                               "・明るくキラキラした表現\n" +
                               "・エミッションで輝き\n" +
                               "・スペキュラーでハイライト\n" +
                               "・NiloToon互換の高品質レンダリング";
            preset.category = PresetCategory.Character_Eyes;
            preset.author = "Natane Toon Shader";
            preset.version = "1.0";

            var p = preset.parameters;

            // 基本設定
            p.mainColor = new Color(0.3f, 0.6f, 0.9f, 1f); // 青い目（例）
            p.alpha = 1f;

            // シェーディング - 明るめ
            p.shadowColor = new Color(0.4f, 0.5f, 0.7f, 1f);
            p.toonSteps = 3; // 少し多めのステップで柔らかく
            p.toonSharpness = 0.15f;
            p.shadowReceive = 0.5f; // 影を受けにくく
            p.shadowIntensityMax = 0.4f;
            p.lightInfluence = 1.5f; // 明るく
            p.backlight = 0.4f;

            // スペキュラー - 強いハイライト
            p.useSpecular = true;
            p.specularColor = new Color(1f, 1f, 1f, 1f);
            p.specularIntensity = 1.5f;
            p.specularSize = 0.3f;
            p.specularSharpness = 0.95f;

            // エミッション - 輝き
            p.useEmission = true;
            p.emissionColor = new Color(0.5f, 0.7f, 1f, 1f);
            p.emissionIntensity = 0.3f;

            // リムライト
            p.useRimLight = true;
            p.rimColor = new Color(1f, 1f, 1f, 1f);
            p.rimIntensity = 1f;
            p.rimPower = 3f;

            // アウトライン - 細く
            p.useOutline = true;
            p.outlineColor = new Color(0.1f, 0.1f, 0.2f, 1f);
            p.outlineWidth = 0.002f;

            // レンダリング
            p.renderQueue = 2000;
            p.cullMode = 2; // Back

            AssetDatabase.CreateAsset(preset, PRESET_PATH + "VTuber_CharacterEyes.asset");
            Debug.Log("生成: VTuber - キャラクター目プリセット");
        }

        /// <summary>
        /// ライブパフォーマンス用プリセット - パフォーマンス重視
        /// </summary>
        private static void GenerateLivePerformancePreset()
        {
            var preset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
            preset.presetName = "VTuber - ライブパフォーマンス";
            preset.description = "VTuberライブ配信・パフォーマンスに最適化されたプリセット。\n" +
                               "・高パフォーマンス設定\n" +
                               "・必要最小限の機能\n" +
                               "・クリーンで安定したレンダリング\n" +
                               "・NiloToon互換の軽量設定";
            preset.category = PresetCategory.Style_Toon;
            preset.author = "Natane Toon Shader";
            preset.version = "1.0";

            var p = preset.parameters;

            // 基本設定
            p.mainColor = Color.white;
            p.alpha = 1f;

            // シェーディング - シンプル
            p.shadowColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            p.toonSteps = 2;
            p.toonSharpness = 0.1f;
            p.shadowReceive = 1f;
            p.shadowIntensityMax = 0.2f;
            p.lightInfluence = 1f;
            p.backlight = 0f;

            // エフェクトは最小限
            p.useSpecular = false;
            p.useRimLight = false;
            p.useSSS = false;
            p.useMatCap = false;
            p.useEmission = false;
            p.useReflection = false;
            p.useEnvRim = false;
            p.useParallax = false;
            p.useRefraction = false;

            // アウトライン - クリーンに
            p.useOutline = true;
            p.outlineColor = Color.black;
            p.outlineWidth = 0.004f;

            // レンダリング
            p.renderQueue = 2000;
            p.cullMode = 2; // Back

            AssetDatabase.CreateAsset(preset, PRESET_PATH + "VTuber_LivePerformance.asset");
            Debug.Log("生成: VTuber - ライブパフォーマンスプリセット");
        }
    }
}
