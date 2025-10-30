using UnityEngine;
using UnityEditor;
using System.IO;
using NataneToon.MaterialSystem;

namespace NataneToon.Editor
{
    /// <summary>
    /// Tool to generate default material presets for designers
    /// Creates a comprehensive library of common material types
    /// </summary>
    public class DefaultPresetGenerator : EditorWindow
    {
        private const string PRESET_FOLDER = "Assets/MaterialPresets";

        [MenuItem("Tools/Natane/デフォルトプリセットを生成", false, 200)]
        public static void ShowWindow()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "デフォルトプリセットを生成",
                "以下のフォルダにデフォルトマテリアルプリセットのライブラリを作成します:\n" +
                PRESET_FOLDER + "\n\n" +
                "これらのプリセットは、一般的なマテリアルタイプの開始点を提供します。\n" +
                "既存のプリセットは上書きされません。",
                "生成",
                "キャンセル");

            if (proceed)
            {
                GenerateAllPresets();
            }
        }

        private static void GenerateAllPresets()
        {
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(PRESET_FOLDER))
            {
                string parentFolder = "Assets";
                string folderName = "MaterialPresets";
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }

            int createdCount = 0;

            // Character presets
            createdCount += CreatePreset("Character_Skin_Soft", PresetCategory.Character_Skin, CreateSkinSoftPreset(), "SSSを使用したソフトアニメ調スキン");
            createdCount += CreatePreset("Character_Skin_Realistic", PresetCategory.Character_Skin, CreateSkinRealisticPreset(), "強化されたSSSを備えたより現実的なスキン");
            createdCount += CreatePreset("Character_Hair_Standard", PresetCategory.Character_Hair, CreateHairStandardPreset(), "スペキュラーハイライト付き標準アニメヘア");
            createdCount += CreatePreset("Character_Hair_Glossy", PresetCategory.Character_Hair, CreateHairGlossyPreset(), "強いスペキュラーを備えた光沢のあるヘア");
            createdCount += CreatePreset("Character_Clothing_Fabric", PresetCategory.Character_Clothing, CreateClothingFabricPreset(), "標準的な布地マテリアル");
            createdCount += CreatePreset("Character_Clothing_Leather", PresetCategory.Character_Clothing, CreateClothingLeatherPreset(), "スペキュラー付きレザーマテリアル");
            createdCount += CreatePreset("Character_Eyes_Standard", PresetCategory.Character_Eyes, CreateEyesStandardPreset(), "ハイライト付き標準アニメの目");

            // Props presets
            createdCount += CreatePreset("Props_Metal_Shiny", PresetCategory.Props_Metal, CreateMetalShinyPreset(), "反射付き光沢メタル");
            createdCount += CreatePreset("Props_Metal_Brushed", PresetCategory.Props_Metal, CreateMetalBrushedPreset(), "ブラッシュメタル仕上げ");
            createdCount += CreatePreset("Props_Plastic_Glossy", PresetCategory.Props_Plastic, CreatePlasticGlossyPreset(), "光沢プラスチックマテリアル");
            createdCount += CreatePreset("Props_Plastic_Matte", PresetCategory.Props_Plastic, CreatePlasticMattePreset(), "マットプラスチックマテリアル");
            createdCount += CreatePreset("Props_Wood_Natural", PresetCategory.Props_Wood, CreateWoodNaturalPreset(), "ナチュラルウッド仕上げ");
            createdCount += CreatePreset("Props_Fabric_Soft", PresetCategory.Props_Fabric, CreateFabricSoftPreset(), "ソフト布地マテリアル");

            // Environment presets
            createdCount += CreatePreset("Environment_Nature_Grass", PresetCategory.Environment_Nature, CreateGrassPreset(), "SSSを使用した草");
            createdCount += CreatePreset("Environment_Nature_Leaves", PresetCategory.Environment_Nature, CreateLeavesPreset(), "SSSとリムライト付き葉");
            createdCount += CreatePreset("Environment_Architecture_Stone", PresetCategory.Environment_Architecture, CreateStonePreset(), "石造建築");
            createdCount += CreatePreset("Environment_Architecture_Concrete", PresetCategory.Environment_Architecture, CreateConcretePreset(), "コンクリートマテリアル");

            // Effects presets
            createdCount += CreatePreset("Effects_Glass_Clear", PresetCategory.Effects_Transparent, CreateGlassClearPreset(), "屈折付きクリアガラス");
            createdCount += CreatePreset("Effects_Glass_Frosted", PresetCategory.Effects_Transparent, CreateGlassFrostedPreset(), "フロストガラス");
            createdCount += CreatePreset("Effects_Water_Clear", PresetCategory.Effects_Transparent, CreateWaterClearPreset(), "反射付きクリアウォーター");
            createdCount += CreatePreset("Effects_Emission_Glow", PresetCategory.Effects_Emission, CreateEmissionGlowPreset(), "グロウエミッションエフェクト");
            createdCount += CreatePreset("Effects_Emission_Neon", PresetCategory.Effects_Emission, CreateEmissionNeonPreset(), "ネオンライトエフェクト");
            createdCount += CreatePreset("Effects_Hologram", PresetCategory.Effects_Special, CreateHologramPreset(), "リムとエミッション付きホログラムエフェクト");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "プリセット生成完了",
                $"{createdCount}個のデフォルトマテリアルプリセットが正常に作成されました!\n\n" +
                $"場所: {PRESET_FOLDER}\n\n" +
                "マテリアルプリセットブラウザーで使用します:\n" +
                "Tools > Natane > マテリアルプリセットブラウザー",
                "OK");

            Debug.Log($"[DefaultPresetGenerator] Created {createdCount} default presets in {PRESET_FOLDER}");
        }

        private static int CreatePreset(string name, PresetCategory category, MaterialParameterData parameters, string description)
        {
            string path = Path.Combine(PRESET_FOLDER, $"{name}.asset");

            // Check if already exists
            if (File.Exists(path))
            {
                Debug.Log($"[DefaultPresetGenerator] Skipping existing preset: {name}");
                return 0;
            }

            var preset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
            preset.presetName = name.Replace("_", " ");
            preset.category = category;
            preset.description = description;
            preset.parameters = parameters;
            preset.author = "Natane Toon Shader";
            preset.version = "1.0";
            preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");

            AssetDatabase.CreateAsset(preset, path);
            Debug.Log($"[DefaultPresetGenerator] Created preset: {name}");
            return 1;
        }

        // Character Skin Presets
        private static MaterialParameterData CreateSkinSoftPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(1f, 0.9f, 0.85f, 1f),
                shadowColor = new Color(0.85f, 0.7f, 0.65f, 1f),
                toonSteps = 2,
                toonSharpness = 0.3f,
                useSSS = true,
                sssColor = new Color(1f, 0.7f, 0.6f, 1f),
                sssIntensity = 0.4f,
                sssPower = 3f,
                useRimLight = true,
                rimColor = new Color(1f, 0.9f, 0.85f, 1f),
                rimIntensity = 0.3f,
                rimPower = 4f
            };
        }

        private static MaterialParameterData CreateSkinRealisticPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(1f, 0.88f, 0.8f, 1f),
                shadowColor = new Color(0.8f, 0.65f, 0.6f, 1f),
                toonSteps = 3,
                toonSharpness = 0.5f,
                useSSS = true,
                sssColor = new Color(1f, 0.6f, 0.5f, 1f),
                sssIntensity = 0.6f,
                sssPower = 2.5f,
                useSpecular = true,
                specularColor = new Color(1f, 0.95f, 0.9f, 1f),
                specularIntensity = 0.2f,
                specularSize = 0.05f,
                useRimLight = true,
                rimIntensity = 0.4f,
                rimPower = 3f
            };
        }

        // Character Hair Presets
        private static MaterialParameterData CreateHairStandardPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.3f, 0.2f, 0.15f, 1f),
                shadowColor = new Color(0.15f, 0.1f, 0.08f, 1f),
                toonSteps = 2,
                toonSharpness = 0.7f,
                useSpecular = true,
                specularColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                specularIntensity = 0.5f,
                specularSize = 0.02f,
                specularSharpness = 0.95f,
                useOutline = true,
                outlineColor = new Color(0.1f, 0.05f, 0.05f, 1f),
                outlineWidth = 0.08f
            };
        }

        private static MaterialParameterData CreateHairGlossyPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.2f, 0.15f, 0.1f, 1f),
                shadowColor = new Color(0.1f, 0.08f, 0.05f, 1f),
                toonSteps = 2,
                toonSharpness = 0.8f,
                useSpecular = true,
                specularColor = Color.white,
                specularIntensity = 0.8f,
                specularSize = 0.015f,
                specularSharpness = 0.98f,
                useMatCap = true,
                matCapIntensity = 0.3f,
                matCapBlendMode = 0, // Add
                useOutline = true,
                outlineWidth = 0.08f
            };
        }

        // Clothing Presets
        private static MaterialParameterData CreateClothingFabricPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.6f, 0.6f, 0.8f, 1f),
                shadowColor = new Color(0.3f, 0.3f, 0.5f, 1f),
                toonSteps = 2,
                toonSharpness = 0.5f,
                useOutline = true,
                outlineColor = new Color(0.2f, 0.2f, 0.3f, 1f),
                outlineWidth = 0.05f
            };
        }

        private static MaterialParameterData CreateClothingLeatherPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.3f, 0.2f, 0.15f, 1f),
                shadowColor = new Color(0.15f, 0.1f, 0.08f, 1f),
                toonSteps = 2,
                toonSharpness = 0.6f,
                useSpecular = true,
                specularColor = new Color(0.8f, 0.8f, 0.8f, 1f),
                specularIntensity = 0.4f,
                specularSize = 0.05f,
                useOutline = true,
                outlineWidth = 0.05f
            };
        }

        // Eyes Preset
        private static MaterialParameterData CreateEyesStandardPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.3f, 0.5f, 0.8f, 1f),
                toonSteps = 1,
                toonSharpness = 0.9f,
                useSpecular = true,
                specularColor = Color.white,
                specularIntensity = 1f,
                specularSize = 0.03f,
                specularSharpness = 0.98f,
                useEmission = true,
                emissionColor = new Color(0.3f, 0.5f, 0.8f, 1f),
                emissionIntensity = 0.2f
            };
        }

        // Metal Presets
        private static MaterialParameterData CreateMetalShinyPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.8f, 0.8f, 0.8f, 1f),
                shadowColor = new Color(0.4f, 0.4f, 0.4f, 1f),
                toonSteps = 2,
                toonSharpness = 0.8f,
                useReflection = true,
                reflectionIntensity = 0.8f,
                smoothness = 0.9f,
                metallic = 0.9f,
                fresnelPower = 5f,
                useSpecular = true,
                specularIntensity = 0.9f,
                specularSize = 0.03f,
                specularSharpness = 0.95f
            };
        }

        private static MaterialParameterData CreateMetalBrushedPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.7f, 0.7f, 0.7f, 1f),
                shadowColor = new Color(0.35f, 0.35f, 0.35f, 1f),
                toonSteps = 3,
                toonSharpness = 0.6f,
                useReflection = true,
                reflectionIntensity = 0.5f,
                smoothness = 0.6f,
                metallic = 0.8f,
                useSpecular = true,
                specularIntensity = 0.6f,
                specularSize = 0.05f
            };
        }

        // Plastic Presets
        private static MaterialParameterData CreatePlasticGlossyPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.9f, 0.3f, 0.3f, 1f),
                shadowColor = new Color(0.5f, 0.15f, 0.15f, 1f),
                toonSteps = 2,
                toonSharpness = 0.7f,
                useSpecular = true,
                specularIntensity = 0.8f,
                specularSize = 0.04f,
                specularSharpness = 0.9f,
                useReflection = true,
                reflectionIntensity = 0.3f,
                smoothness = 0.8f
            };
        }

        private static MaterialParameterData CreatePlasticMattePreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.7f, 0.7f, 0.9f, 1f),
                shadowColor = new Color(0.35f, 0.35f, 0.5f, 1f),
                toonSteps = 2,
                toonSharpness = 0.5f,
                useSpecular = true,
                specularIntensity = 0.3f,
                specularSize = 0.1f
            };
        }

        // Wood Preset
        private static MaterialParameterData CreateWoodNaturalPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.6f, 0.4f, 0.2f, 1f),
                shadowColor = new Color(0.3f, 0.2f, 0.1f, 1f),
                toonSteps = 3,
                toonSharpness = 0.4f,
                useSpecular = true,
                specularIntensity = 0.2f,
                specularSize = 0.08f
            };
        }

        // Fabric Preset
        private static MaterialParameterData CreateFabricSoftPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.8f, 0.7f, 0.6f, 1f),
                shadowColor = new Color(0.5f, 0.4f, 0.3f, 1f),
                toonSteps = 2,
                toonSharpness = 0.4f,
                useSSS = true,
                sssColor = new Color(0.9f, 0.8f, 0.7f, 1f),
                sssIntensity = 0.3f
            };
        }

        // Nature Presets
        private static MaterialParameterData CreateGrassPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.3f, 0.6f, 0.3f, 1f),
                shadowColor = new Color(0.15f, 0.35f, 0.15f, 1f),
                toonSteps = 2,
                toonSharpness = 0.5f,
                useSSS = true,
                sssColor = new Color(0.5f, 0.9f, 0.5f, 1f),
                sssIntensity = 0.5f,
                cullMode = 0 // Double-sided
            };
        }

        private static MaterialParameterData CreateLeavesPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.3f, 0.7f, 0.3f, 1f),
                shadowColor = new Color(0.15f, 0.4f, 0.15f, 1f),
                toonSteps = 2,
                toonSharpness = 0.5f,
                useSSS = true,
                sssColor = new Color(0.6f, 1f, 0.6f, 1f),
                sssIntensity = 0.7f,
                useRimLight = true,
                rimColor = new Color(0.7f, 1f, 0.7f, 1f),
                rimIntensity = 0.4f,
                cullMode = 0 // Double-sided
            };
        }

        // Architecture Presets
        private static MaterialParameterData CreateStonePreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.6f, 0.6f, 0.55f, 1f),
                shadowColor = new Color(0.3f, 0.3f, 0.28f, 1f),
                toonSteps = 3,
                toonSharpness = 0.4f
            };
        }

        private static MaterialParameterData CreateConcretePreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.7f, 0.7f, 0.7f, 1f),
                shadowColor = new Color(0.4f, 0.4f, 0.4f, 1f),
                toonSteps = 2,
                toonSharpness = 0.5f
            };
        }

        // Glass Presets
        private static MaterialParameterData CreateGlassClearPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.9f, 0.95f, 1f, 0.1f),
                alpha = 0.1f,
                toonSteps = 1,
                useRefraction = true,
                refractionIndex = 1.5f,
                refractionIntensity = 1f,
                useReflection = true,
                reflectionIntensity = 0.6f,
                smoothness = 0.98f,
                metallic = 0f,
                fresnelPower = 5f,
                useSpecular = true,
                specularIntensity = 0.9f,
                specularSize = 0.01f,
                specularSharpness = 0.99f
            };
        }

        private static MaterialParameterData CreateGlassFrostedPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.9f, 0.95f, 1f, 0.3f),
                alpha = 0.3f,
                toonSteps = 1,
                useRefraction = true,
                refractionIndex = 1.5f,
                refractionIntensity = 0.5f,
                useReflection = true,
                reflectionIntensity = 0.3f,
                smoothness = 0.5f
            };
        }

        // Water Preset
        private static MaterialParameterData CreateWaterClearPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.3f, 0.5f, 0.8f, 0.5f),
                alpha = 0.5f,
                toonSteps = 2,
                useRefraction = true,
                refractionIndex = 1.33f,
                refractionIntensity = 0.8f,
                useReflection = true,
                reflectionIntensity = 0.5f,
                smoothness = 0.9f,
                useSpecular = true,
                specularIntensity = 0.7f,
                specularSize = 0.02f
            };
        }

        // Emission Presets
        private static MaterialParameterData CreateEmissionGlowPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.2f, 0.4f, 0.8f, 1f),
                toonSteps = 1,
                useEmission = true,
                emissionColor = new Color(0.3f, 0.6f, 1f, 1f),
                emissionIntensity = 2f
            };
        }

        private static MaterialParameterData CreateEmissionNeonPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(1f, 0.2f, 0.8f, 1f),
                toonSteps = 1,
                useEmission = true,
                emissionColor = new Color(1f, 0f, 0.8f, 1f),
                emissionIntensity = 3f,
                useEmissionAnimation = true,
                emissionAnimationType = 1, // Pulse
                emissionPulseSpeed = 2f,
                emissionPulseMin = 0.5f,
                emissionPulseMax = 1f
            };
        }

        // Hologram Preset
        private static MaterialParameterData CreateHologramPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.3f, 0.8f, 1f, 0.5f),
                alpha = 0.5f,
                toonSteps = 1,
                useEmission = true,
                emissionColor = new Color(0.5f, 1f, 1f, 1f),
                emissionIntensity = 1.5f,
                useEmissionAnimation = true,
                emissionAnimationType = 0, // Scroll
                emissionScrollSpeed = 0.5f,
                useRimLight = true,
                rimColor = new Color(0.5f, 1f, 1f, 1f),
                rimIntensity = 1f,
                rimPower = 2f,
                useEnvRim = true,
                envRimIntensity = 0.5f
            };
        }
    }
}
