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

        [MenuItem("Tools/Natane/Generate Default Presets", false, 200)]
        public static void ShowWindow()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Generate Default Presets",
                "This will create a library of default material presets in:\n" +
                PRESET_FOLDER + "\n\n" +
                "These presets provide starting points for common material types.\n" +
                "Existing presets will not be overwritten.",
                "Generate",
                "Cancel");

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
            createdCount += CreatePreset("Character_Skin_Soft", PresetCategory.Character_Skin, CreateSkinSoftPreset(), "Soft anime-style skin with SSS");
            createdCount += CreatePreset("Character_Skin_Realistic", PresetCategory.Character_Skin, CreateSkinRealisticPreset(), "More realistic skin with enhanced SSS");
            createdCount += CreatePreset("Character_Hair_Standard", PresetCategory.Character_Hair, CreateHairStandardPreset(), "Standard anime hair with specular highlights");
            createdCount += CreatePreset("Character_Hair_Glossy", PresetCategory.Character_Hair, CreateHairGlossyPreset(), "Glossy hair with strong specular");
            createdCount += CreatePreset("Character_Clothing_Fabric", PresetCategory.Character_Clothing, CreateClothingFabricPreset(), "Standard fabric material");
            createdCount += CreatePreset("Character_Clothing_Leather", PresetCategory.Character_Clothing, CreateClothingLeatherPreset(), "Leather material with specular");
            createdCount += CreatePreset("Character_Eyes_Standard", PresetCategory.Character_Eyes, CreateEyesStandardPreset(), "Standard anime eyes with highlight");

            // Props presets
            createdCount += CreatePreset("Props_Metal_Shiny", PresetCategory.Props_Metal, CreateMetalShinyPreset(), "Shiny metal with reflection");
            createdCount += CreatePreset("Props_Metal_Brushed", PresetCategory.Props_Metal, CreateMetalBrushedPreset(), "Brushed metal finish");
            createdCount += CreatePreset("Props_Plastic_Glossy", PresetCategory.Props_Plastic, CreatePlasticGlossyPreset(), "Glossy plastic material");
            createdCount += CreatePreset("Props_Plastic_Matte", PresetCategory.Props_Plastic, CreatePlasticMattePreset(), "Matte plastic material");
            createdCount += CreatePreset("Props_Wood_Natural", PresetCategory.Props_Wood, CreateWoodNaturalPreset(), "Natural wood finish");
            createdCount += CreatePreset("Props_Fabric_Soft", PresetCategory.Props_Fabric, CreateFabricSoftPreset(), "Soft fabric material");

            // Environment presets
            createdCount += CreatePreset("Environment_Nature_Grass", PresetCategory.Environment_Nature, CreateGrassPreset(), "Grass with SSS");
            createdCount += CreatePreset("Environment_Nature_Leaves", PresetCategory.Environment_Nature, CreateLeavesPreset(), "Leaves with SSS and rim");
            createdCount += CreatePreset("Environment_Architecture_Stone", PresetCategory.Environment_Architecture, CreateStonePreset(), "Stone architecture");
            createdCount += CreatePreset("Environment_Architecture_Concrete", PresetCategory.Environment_Architecture, CreateConcretePreset(), "Concrete material");

            // Effects presets
            createdCount += CreatePreset("Effects_Glass_Clear", PresetCategory.Effects_Transparent, CreateGlassClearPreset(), "Clear glass with refraction");
            createdCount += CreatePreset("Effects_Glass_Frosted", PresetCategory.Effects_Transparent, CreateGlassFrostedPreset(), "Frosted glass");
            createdCount += CreatePreset("Effects_Water_Clear", PresetCategory.Effects_Transparent, CreateWaterClearPreset(), "Clear water with reflection");
            createdCount += CreatePreset("Effects_Emission_Glow", PresetCategory.Effects_Emission, CreateEmissionGlowPreset(), "Glowing emission effect");
            createdCount += CreatePreset("Effects_Emission_Neon", PresetCategory.Effects_Emission, CreateEmissionNeonPreset(), "Neon light effect");
            createdCount += CreatePreset("Effects_Hologram", PresetCategory.Effects_Special, CreateHologramPreset(), "Hologram effect with rim and emission");

            // Toon/NPR Style presets
            createdCount += CreatePreset("Style_Toon_Classic_Cell", PresetCategory.Style_Toon, CreateClassicCellShadingPreset(), "伝統的なセルシェーディング - 2段階の影、シャープな境界");
            createdCount += CreatePreset("Style_Toon_Soft", PresetCategory.Style_Toon, CreateSoftToonPreset(), "柔らかいトゥーン - 3段階の影、柔らかい境界");
            createdCount += CreatePreset("Style_Toon_Hard_Edge", PresetCategory.Style_Toon, CreateHardEdgeToonPreset(), "ハードエッジトゥーン - 強いコントラスト、太いアウトライン");
            createdCount += CreatePreset("Style_Toon_Retro_80s", PresetCategory.Style_Toon, CreateRetro80sPreset(), "80年代アニメ風 - 強いリムライト、低彩度影");
            createdCount += CreatePreset("Style_Toon_Retro_90s", PresetCategory.Style_Toon, CreateRetro90sPreset(), "90年代アニメ風 - グラデーション影、スペキュラ");
            createdCount += CreatePreset("Style_NPR_Comic_Book", PresetCategory.Style_NPR, CreateComicBookPreset(), "アメコミ風 - 太いアウトライン、高コントラスト");
            createdCount += CreatePreset("Style_NPR_Pastel", PresetCategory.Style_NPR, CreatePastelToonPreset(), "パステル調 - 明るい色、柔らかい影");
            createdCount += CreatePreset("Style_NPR_Ink_Wash", PresetCategory.Style_NPR, CreateInkWashPreset(), "墨絵風 - モノクロ、強いコントラスト");
            createdCount += CreatePreset("Style_NPR_Flat_Color", PresetCategory.Style_NPR, CreateFlatColorPreset(), "フラットカラー - 影なし、単色");
            createdCount += CreatePreset("Style_NPR_Watercolor", PresetCategory.Style_NPR, CreateWatercolorPreset(), "水彩画風 - 柔らかい影、低コントラスト");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Presets Generated",
                $"Successfully created {createdCount} default material presets!\n\n" +
                $"Location: {PRESET_FOLDER}\n\n" +
                "Open the Material Preset Browser to use them:\n" +
                "Tools > Natane > Material Preset Browser",
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

        // Toon Style Presets
        private static MaterialParameterData CreateClassicCellShadingPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.95f, 0.95f, 0.95f, 1f),
                shadowColor = new Color(0.4f, 0.4f, 0.5f, 1f),
                toonSteps = 2,
                toonSharpness = 0.95f,
                useOutline = true,
                outlineWidth = 0.003f,
                outlineColor = new Color(0.1f, 0.1f, 0.1f, 1f)
            };
        }

        private static MaterialParameterData CreateSoftToonPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.95f, 0.95f, 0.95f, 1f),
                shadowColor = new Color(0.5f, 0.5f, 0.6f, 1f),
                toonSteps = 3,
                toonSharpness = 0.3f,
                useOutline = true,
                outlineWidth = 0.002f,
                outlineColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                useRimLight = true,
                rimColor = new Color(0.9f, 0.9f, 1f, 1f),
                rimIntensity = 0.3f,
                rimPower = 3f
            };
        }

        private static MaterialParameterData CreateHardEdgeToonPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(1f, 1f, 1f, 1f),
                shadowColor = new Color(0.2f, 0.2f, 0.3f, 1f),
                toonSteps = 2,
                toonSharpness = 1f,
                useOutline = true,
                outlineWidth = 0.006f,
                outlineColor = new Color(0f, 0f, 0f, 1f),
                useSpecular = true,
                specularIntensity = 0.8f,
                specularSize = 0.02f,
                specularSharpness = 0.95f
            };
        }

        private static MaterialParameterData CreateRetro80sPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.9f, 0.85f, 0.9f, 1f),
                shadowColor = new Color(0.35f, 0.3f, 0.4f, 1f),
                toonSteps = 2,
                toonSharpness = 0.8f,
                useOutline = true,
                outlineWidth = 0.004f,
                outlineColor = new Color(0.15f, 0.1f, 0.2f, 1f),
                useRimLight = true,
                rimColor = new Color(1f, 0.8f, 1f, 1f),
                rimIntensity = 0.8f,
                rimPower = 2f,
                useSpecular = true,
                specularIntensity = 0.6f,
                specularSize = 0.05f
            };
        }

        private static MaterialParameterData CreateRetro90sPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.95f, 0.95f, 1f, 1f),
                shadowColor = new Color(0.45f, 0.45f, 0.55f, 1f),
                toonSteps = 3,
                toonSharpness = 0.5f,
                useOutline = true,
                outlineWidth = 0.003f,
                outlineColor = new Color(0.1f, 0.1f, 0.15f, 1f),
                useSpecular = true,
                specularIntensity = 0.7f,
                specularSize = 0.03f,
                specularSharpness = 0.85f,
                useRimLight = true,
                rimColor = new Color(0.9f, 0.9f, 1f, 1f),
                rimIntensity = 0.4f,
                rimPower = 4f
            };
        }

        // NPR Style Presets
        private static MaterialParameterData CreateComicBookPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(1f, 0.95f, 0.9f, 1f),
                shadowColor = new Color(0.25f, 0.2f, 0.25f, 1f),
                toonSteps = 2,
                toonSharpness = 1f,
                useOutline = true,
                outlineWidth = 0.008f,
                outlineColor = new Color(0f, 0f, 0f, 1f),
                useSpecular = true,
                specularIntensity = 0.9f,
                specularSize = 0.01f,
                specularSharpness = 0.98f,
                useRimLight = true,
                rimColor = new Color(1f, 1f, 1f, 1f),
                rimIntensity = 0.6f,
                rimPower = 2.5f
            };
        }

        private static MaterialParameterData CreatePastelToonPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(1f, 0.95f, 0.95f, 1f),
                shadowColor = new Color(0.85f, 0.75f, 0.8f, 1f),
                toonSteps = 3,
                toonSharpness = 0.2f,
                useOutline = true,
                outlineWidth = 0.002f,
                outlineColor = new Color(0.4f, 0.3f, 0.35f, 1f),
                useRimLight = true,
                rimColor = new Color(1f, 0.9f, 0.95f, 1f),
                rimIntensity = 0.5f,
                rimPower = 5f
            };
        }

        private static MaterialParameterData CreateInkWashPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                shadowColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                toonSteps = 3,
                toonSharpness = 0.4f,
                useOutline = true,
                outlineWidth = 0.005f,
                outlineColor = new Color(0f, 0f, 0f, 1f)
            };
        }

        private static MaterialParameterData CreateFlatColorPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.95f, 0.95f, 0.95f, 1f),
                shadowColor = new Color(0.95f, 0.95f, 0.95f, 1f),
                toonSteps = 1,
                toonSharpness = 0f,
                useOutline = true,
                outlineWidth = 0.004f,
                outlineColor = new Color(0f, 0f, 0f, 1f)
            };
        }

        private static MaterialParameterData CreateWatercolorPreset()
        {
            return new MaterialParameterData
            {
                mainColor = new Color(0.95f, 0.95f, 1f, 1f),
                shadowColor = new Color(0.6f, 0.6f, 0.7f, 1f),
                toonSteps = 4,
                toonSharpness = 0.1f,
                useOutline = true,
                outlineWidth = 0.002f,
                outlineColor = new Color(0.3f, 0.3f, 0.4f, 1f),
                useRimLight = true,
                rimColor = new Color(0.9f, 0.9f, 1f, 1f),
                rimIntensity = 0.2f,
                rimPower = 6f
            };
        }
    }
}
