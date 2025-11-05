using UnityEngine;
using System;
using System.Collections.Generic;

namespace NataneToon.MaterialSystem
{
    /// <summary>
    /// Material preset category for organization
    /// </summary>
    public enum PresetCategory
    {
        Character_Skin,
        Character_Hair,
        Character_Clothing,
        Character_Eyes,
        Props_Metal,
        Props_Plastic,
        Props_Wood,
        Props_Fabric,
        Environment_Nature,
        Environment_Architecture,
        Effects_Transparent,
        Effects_Emission,
        Effects_Special,
        Style_Toon,
        Style_NPR,
        Custom
    }

    /// <summary>
    /// Serializable material parameter data for sharing and presets
    /// </summary>
    [Serializable]
    public class MaterialParameterData
    {
        // Basic Properties
        public Color mainColor = Color.white;
        public float alpha = 1f;

        // Shading
        public Color shadowColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        public int toonSteps = 2;
        public float toonSharpness = 0.5f;
        public float shadowReceive = 1f;
        public float shadowIntensityMax = 0.8f;
        public float lightInfluence = 1f;
        public float lightColorInfluence = 1f;
        public float backlight = 0f;

        // Specular
        public bool useSpecular = false;
        public Color specularColor = Color.white;
        public float specularIntensity = 1f;
        public float specularSize = 0.1f;
        public float specularSharpness = 0.9f;
        public bool useSpecularMask = false;

        // Rim Light
        public bool useRimLight = false;
        public Color rimColor = Color.white;
        public float rimIntensity = 1f;
        public float rimPower = 3f;
        public bool useRimMask = false;

        // SSS (Subsurface Scattering)
        public bool useSSS = false;
        public Color sssColor = new Color(1f, 0.5f, 0.5f, 1f);
        public float sssIntensity = 0.5f;
        public float sssDistortion = 0.5f;
        public float sssPower = 3f;
        public float sssScale = 1f;
        public bool useSSSMask = false;

        // MatCap
        public bool useMatCap = false;
        public float matCapIntensity = 1f;
        public int matCapBlendMode = 0; // 0=Add, 1=Multiply, 2=Replace
        public bool useMatCapMask = false;

        // Outline
        public bool useOutline = false;
        public Color outlineColor = Color.black;
        public float outlineWidth = 0.1f;
        public bool useOutlineMask = false;

        // Emission
        public bool useEmission = false;
        public Color emissionColor = Color.white;
        public float emissionIntensity = 1f;
        public bool useEmissionMask = false;

        // Emission Animation
        public bool useEmissionAnimation = false;
        public int emissionAnimationType = 0; // 0=Scroll, 1=Pulse
        public float emissionScrollSpeed = 1f;
        public float emissionPulseSpeed = 1f;
        public float emissionPulseMin = 0f;
        public float emissionPulseMax = 1f;

        // Virtual Expression
        public bool useDissolve = false;
        public float dissolveAmount = 0f;
        public Color dissolveEdgeColor = Color.cyan;
        public float dissolveEdgeWidth = 0.1f;
        public bool useHueShift = false;
        public float hueShift = 0f;

        // Background/Environment Features
        public bool useReflection = false;
        public float reflectionIntensity = 1f;
        public float smoothness = 0.5f;
        public float metallic = 0f;
        public float fresnelPower = 5f;
        public bool useReflectionMask = false;

        public bool useEnvRim = false;
        public Color envRimColor = Color.white;
        public float envRimPower = 3f;
        public float envRimIntensity = 1f;
        public bool useEnvRimMask = false;

        public bool useParallax = false;
        public float parallaxScale = 0.02f;
        public float parallaxMinSamples = 8f;
        public float parallaxMaxSamples = 16f;

        public bool useRefraction = false;
        public float refractionIndex = 1.5f;
        public float refractionIntensity = 1f;
        public bool useRefractionMask = false;

        // Normal Map
        public float normalMapIntensity = 1f;

        // Rendering
        public int renderQueue = 2000;
        public int cullMode = 2; // 0=Off, 1=Front, 2=Back
    }

    /// <summary>
    /// Material preset for Natane Toon Shader
    /// Stores all material parameters and can be applied to materials
    /// </summary>
    [CreateAssetMenu(fileName = "New Material Preset", menuName = "Natane/Material Preset", order = 1)]
    public class NataneToonMaterialPreset : ScriptableObject
    {
        [Header("Preset Information")]
        public string presetName = "New Preset";
        [TextArea(3, 5)]
        public string description = "";
        public PresetCategory category = PresetCategory.Custom;
        public Texture2D thumbnail;

        [Header("Material Parameters")]
        public MaterialParameterData parameters = new MaterialParameterData();

        [Header("Metadata")]
        public string author = "";
        public string version = "1.0";
        public string createdDate = "";

        /// <summary>
        /// Apply this preset to a material
        /// </summary>
        public void ApplyToMaterial(Material material)
        {
            if (material == null)
            {
                Debug.LogError("[NataneToonMaterialPreset] Material is null");
                return;
            }

            // Verify shader compatibility
            if (!material.shader.name.Contains("Natane") || !material.shader.name.Contains("Toon"))
            {
                Debug.LogWarning($"[NataneToonMaterialPreset] Material '{material.name}' is not using Natane Toon Shader. Some parameters may not apply.");
            }

            var p = parameters;

            // Basic Properties
            if (material.HasProperty("_Color")) material.SetColor("_Color", p.mainColor);
            if (material.HasProperty("_Alpha")) material.SetFloat("_Alpha", p.alpha);

            // Shading
            if (material.HasProperty("_ShadowColor")) material.SetColor("_ShadowColor", p.shadowColor);
            if (material.HasProperty("_ToonSteps")) material.SetFloat("_ToonSteps", p.toonSteps);
            if (material.HasProperty("_ToonSharpness")) material.SetFloat("_ToonSharpness", p.toonSharpness);
            if (material.HasProperty("_ShadowReceive")) material.SetFloat("_ShadowReceive", p.shadowReceive);
            if (material.HasProperty("_ShadowIntensityMax")) material.SetFloat("_ShadowIntensityMax", p.shadowIntensityMax);
            if (material.HasProperty("_LightInfluence")) material.SetFloat("_LightInfluence", p.lightInfluence);
            if (material.HasProperty("_LightColorInfluence")) material.SetFloat("_LightColorInfluence", p.lightColorInfluence);
            if (material.HasProperty("_Backlight")) material.SetFloat("_Backlight", p.backlight);

            // Specular
            SetKeyword(material, "_SPECULAR", p.useSpecular);
            if (material.HasProperty("_SpecularColor")) material.SetColor("_SpecularColor", p.specularColor);
            if (material.HasProperty("_SpecularIntensity")) material.SetFloat("_SpecularIntensity", p.specularIntensity);
            if (material.HasProperty("_SpecularSize")) material.SetFloat("_SpecularSize", p.specularSize);
            if (material.HasProperty("_SpecularSharpness")) material.SetFloat("_SpecularSharpness", p.specularSharpness);
            SetKeyword(material, "_SPECULAR_MASK", p.useSpecularMask);

            // Rim Light
            SetKeyword(material, "_RIM", p.useRimLight);
            if (material.HasProperty("_RimColor")) material.SetColor("_RimColor", p.rimColor);
            if (material.HasProperty("_RimIntensity")) material.SetFloat("_RimIntensity", p.rimIntensity);
            if (material.HasProperty("_RimPower")) material.SetFloat("_RimPower", p.rimPower);
            SetKeyword(material, "_RIM_MASK", p.useRimMask);

            // SSS
            SetKeyword(material, "_SSS", p.useSSS);
            if (material.HasProperty("_SSSColor")) material.SetColor("_SSSColor", p.sssColor);
            if (material.HasProperty("_SSSIntensity")) material.SetFloat("_SSSIntensity", p.sssIntensity);
            if (material.HasProperty("_SSSDistortion")) material.SetFloat("_SSSDistortion", p.sssDistortion);
            if (material.HasProperty("_SSSPower")) material.SetFloat("_SSSPower", p.sssPower);
            if (material.HasProperty("_SSSScale")) material.SetFloat("_SSSScale", p.sssScale);
            SetKeyword(material, "_SSS_MASK", p.useSSSMask);

            // MatCap
            SetKeyword(material, "_MATCAP", p.useMatCap);
            if (material.HasProperty("_MatCapIntensity")) material.SetFloat("_MatCapIntensity", p.matCapIntensity);
            if (material.HasProperty("_MatCapBlendMode")) material.SetFloat("_MatCapBlendMode", p.matCapBlendMode);
            SetKeyword(material, "_MATCAP_MASK", p.useMatCapMask);

            // Outline
            SetKeyword(material, "_OUTLINE", p.useOutline);
            if (material.HasProperty("_OutlineColor")) material.SetColor("_OutlineColor", p.outlineColor);
            if (material.HasProperty("_OutlineWidth")) material.SetFloat("_OutlineWidth", p.outlineWidth);
            SetKeyword(material, "_OUTLINE_MASK", p.useOutlineMask);

            // Emission
            SetKeyword(material, "_EMISSION", p.useEmission);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", p.emissionColor);
            if (material.HasProperty("_EmissionIntensity")) material.SetFloat("_EmissionIntensity", p.emissionIntensity);
            SetKeyword(material, "_EMISSION_MASK", p.useEmissionMask);

            // Emission Animation
            SetKeyword(material, "_EMISSION_ANIMATION", p.useEmissionAnimation);
            if (material.HasProperty("_EmissionAnimationType")) material.SetFloat("_EmissionAnimationType", p.emissionAnimationType);
            if (material.HasProperty("_EmissionScrollSpeed")) material.SetFloat("_EmissionScrollSpeed", p.emissionScrollSpeed);
            if (material.HasProperty("_EmissionPulseSpeed")) material.SetFloat("_EmissionPulseSpeed", p.emissionPulseSpeed);
            if (material.HasProperty("_EmissionPulseMin")) material.SetFloat("_EmissionPulseMin", p.emissionPulseMin);
            if (material.HasProperty("_EmissionPulseMax")) material.SetFloat("_EmissionPulseMax", p.emissionPulseMax);

            // Virtual Expression
            SetKeyword(material, "_DISSOLVE", p.useDissolve);
            if (material.HasProperty("_DissolveAmount")) material.SetFloat("_DissolveAmount", p.dissolveAmount);
            if (material.HasProperty("_DissolveEdgeColor")) material.SetColor("_DissolveEdgeColor", p.dissolveEdgeColor);
            if (material.HasProperty("_DissolveEdgeWidth")) material.SetFloat("_DissolveEdgeWidth", p.dissolveEdgeWidth);
            SetKeyword(material, "_HUE_SHIFT", p.useHueShift);
            if (material.HasProperty("_HueShift")) material.SetFloat("_HueShift", p.hueShift);

            // Background/Environment
            SetKeyword(material, "_REFLECTION", p.useReflection);
            if (material.HasProperty("_ReflectionIntensity")) material.SetFloat("_ReflectionIntensity", p.reflectionIntensity);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", p.smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", p.metallic);
            if (material.HasProperty("_FresnelPower")) material.SetFloat("_FresnelPower", p.fresnelPower);
            SetKeyword(material, "_REFLECTION_MASK", p.useReflectionMask);

            SetKeyword(material, "_ENV_RIM", p.useEnvRim);
            if (material.HasProperty("_EnvRimColor")) material.SetColor("_EnvRimColor", p.envRimColor);
            if (material.HasProperty("_EnvRimPower")) material.SetFloat("_EnvRimPower", p.envRimPower);
            if (material.HasProperty("_EnvRimIntensity")) material.SetFloat("_EnvRimIntensity", p.envRimIntensity);
            SetKeyword(material, "_ENV_RIM_MASK", p.useEnvRimMask);

            SetKeyword(material, "_PARALLAX", p.useParallax);
            if (material.HasProperty("_ParallaxScale")) material.SetFloat("_ParallaxScale", p.parallaxScale);
            if (material.HasProperty("_ParallaxMinSamples")) material.SetFloat("_ParallaxMinSamples", p.parallaxMinSamples);
            if (material.HasProperty("_ParallaxMaxSamples")) material.SetFloat("_ParallaxMaxSamples", p.parallaxMaxSamples);

            SetKeyword(material, "_REFRACTION", p.useRefraction);
            if (material.HasProperty("_RefractionIndex")) material.SetFloat("_RefractionIndex", p.refractionIndex);
            if (material.HasProperty("_RefractionIntensity")) material.SetFloat("_RefractionIntensity", p.refractionIntensity);
            SetKeyword(material, "_REFRACTION_MASK", p.useRefractionMask);

            // Normal Map
            if (material.HasProperty("_NormalMapIntensity")) material.SetFloat("_NormalMapIntensity", p.normalMapIntensity);

            // Rendering
            material.renderQueue = p.renderQueue;
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", p.cullMode);

            Debug.Log($"[NataneToonMaterialPreset] Applied preset '{presetName}' to material '{material.name}'");
        }

        /// <summary>
        /// Create preset from existing material
        /// </summary>
        public void CreateFromMaterial(Material material)
        {
            if (material == null)
            {
                Debug.LogError("[NataneToonMaterialPreset] Material is null");
                return;
            }

            var p = parameters;

            // Basic Properties
            if (material.HasProperty("_Color")) p.mainColor = material.GetColor("_Color");
            if (material.HasProperty("_Alpha")) p.alpha = material.GetFloat("_Alpha");

            // Shading
            if (material.HasProperty("_ShadowColor")) p.shadowColor = material.GetColor("_ShadowColor");
            if (material.HasProperty("_ToonSteps")) p.toonSteps = (int)material.GetFloat("_ToonSteps");
            if (material.HasProperty("_ToonSharpness")) p.toonSharpness = material.GetFloat("_ToonSharpness");
            if (material.HasProperty("_ShadowReceive")) p.shadowReceive = material.GetFloat("_ShadowReceive");
            if (material.HasProperty("_ShadowIntensityMax")) p.shadowIntensityMax = material.GetFloat("_ShadowIntensityMax");
            if (material.HasProperty("_LightInfluence")) p.lightInfluence = material.GetFloat("_LightInfluence");
            if (material.HasProperty("_Backlight")) p.backlight = material.GetFloat("_Backlight");

            // Specular
            p.useSpecular = material.IsKeywordEnabled("_SPECULAR");
            if (material.HasProperty("_SpecularColor")) p.specularColor = material.GetColor("_SpecularColor");
            if (material.HasProperty("_SpecularIntensity")) p.specularIntensity = material.GetFloat("_SpecularIntensity");
            if (material.HasProperty("_SpecularSize")) p.specularSize = material.GetFloat("_SpecularSize");
            if (material.HasProperty("_SpecularSharpness")) p.specularSharpness = material.GetFloat("_SpecularSharpness");
            p.useSpecularMask = material.IsKeywordEnabled("_SPECULAR_MASK");

            // Rim Light
            p.useRimLight = material.IsKeywordEnabled("_RIM");
            if (material.HasProperty("_RimColor")) p.rimColor = material.GetColor("_RimColor");
            if (material.HasProperty("_RimIntensity")) p.rimIntensity = material.GetFloat("_RimIntensity");
            if (material.HasProperty("_RimPower")) p.rimPower = material.GetFloat("_RimPower");
            p.useRimMask = material.IsKeywordEnabled("_RIM_MASK");

            // SSS
            p.useSSS = material.IsKeywordEnabled("_SSS");
            if (material.HasProperty("_SSSColor")) p.sssColor = material.GetColor("_SSSColor");
            if (material.HasProperty("_SSSIntensity")) p.sssIntensity = material.GetFloat("_SSSIntensity");
            if (material.HasProperty("_SSSDistortion")) p.sssDistortion = material.GetFloat("_SSSDistortion");
            if (material.HasProperty("_SSSPower")) p.sssPower = material.GetFloat("_SSSPower");
            if (material.HasProperty("_SSSScale")) p.sssScale = material.GetFloat("_SSSScale");
            p.useSSSMask = material.IsKeywordEnabled("_SSS_MASK");

            // MatCap
            p.useMatCap = material.IsKeywordEnabled("_MATCAP");
            if (material.HasProperty("_MatCapIntensity")) p.matCapIntensity = material.GetFloat("_MatCapIntensity");
            if (material.HasProperty("_MatCapBlendMode")) p.matCapBlendMode = (int)material.GetFloat("_MatCapBlendMode");
            p.useMatCapMask = material.IsKeywordEnabled("_MATCAP_MASK");

            // Outline
            p.useOutline = material.IsKeywordEnabled("_OUTLINE");
            if (material.HasProperty("_OutlineColor")) p.outlineColor = material.GetColor("_OutlineColor");
            if (material.HasProperty("_OutlineWidth")) p.outlineWidth = material.GetFloat("_OutlineWidth");

            // Emission
            p.useEmission = material.IsKeywordEnabled("_EMISSION");
            if (material.HasProperty("_EmissionColor")) p.emissionColor = material.GetColor("_EmissionColor");
            if (material.HasProperty("_EmissionIntensity")) p.emissionIntensity = material.GetFloat("_EmissionIntensity");
            p.useEmissionMask = material.IsKeywordEnabled("_EMISSION_MASK");

            // Emission Animation
            p.useEmissionAnimation = material.IsKeywordEnabled("_EMISSION_ANIMATION");
            if (material.HasProperty("_EmissionAnimationType")) p.emissionAnimationType = (int)material.GetFloat("_EmissionAnimationType");
            if (material.HasProperty("_EmissionScrollSpeed")) p.emissionScrollSpeed = material.GetFloat("_EmissionScrollSpeed");
            if (material.HasProperty("_EmissionPulseSpeed")) p.emissionPulseSpeed = material.GetFloat("_EmissionPulseSpeed");
            if (material.HasProperty("_EmissionPulseMin")) p.emissionPulseMin = material.GetFloat("_EmissionPulseMin");
            if (material.HasProperty("_EmissionPulseMax")) p.emissionPulseMax = material.GetFloat("_EmissionPulseMax");

            // Virtual Expression
            p.useDissolve = material.IsKeywordEnabled("_DISSOLVE");
            if (material.HasProperty("_DissolveAmount")) p.dissolveAmount = material.GetFloat("_DissolveAmount");
            if (material.HasProperty("_DissolveEdgeColor")) p.dissolveEdgeColor = material.GetColor("_DissolveEdgeColor");
            if (material.HasProperty("_DissolveEdgeWidth")) p.dissolveEdgeWidth = material.GetFloat("_DissolveEdgeWidth");
            p.useHueShift = material.IsKeywordEnabled("_HUE_SHIFT");
            if (material.HasProperty("_HueShift")) p.hueShift = material.GetFloat("_HueShift");

            // Background/Environment
            p.useReflection = material.IsKeywordEnabled("_REFLECTION");
            if (material.HasProperty("_ReflectionIntensity")) p.reflectionIntensity = material.GetFloat("_ReflectionIntensity");
            if (material.HasProperty("_Smoothness")) p.smoothness = material.GetFloat("_Smoothness");
            if (material.HasProperty("_Metallic")) p.metallic = material.GetFloat("_Metallic");
            if (material.HasProperty("_FresnelPower")) p.fresnelPower = material.GetFloat("_FresnelPower");
            p.useReflectionMask = material.IsKeywordEnabled("_REFLECTION_MASK");

            p.useEnvRim = material.IsKeywordEnabled("_ENV_RIM");
            if (material.HasProperty("_EnvRimColor")) p.envRimColor = material.GetColor("_EnvRimColor");
            if (material.HasProperty("_EnvRimPower")) p.envRimPower = material.GetFloat("_EnvRimPower");
            if (material.HasProperty("_EnvRimIntensity")) p.envRimIntensity = material.GetFloat("_EnvRimIntensity");
            p.useEnvRimMask = material.IsKeywordEnabled("_ENV_RIM_MASK");

            p.useParallax = material.IsKeywordEnabled("_PARALLAX");
            if (material.HasProperty("_ParallaxScale")) p.parallaxScale = material.GetFloat("_ParallaxScale");
            if (material.HasProperty("_ParallaxMinSamples")) p.parallaxMinSamples = material.GetFloat("_ParallaxMinSamples");
            if (material.HasProperty("_ParallaxMaxSamples")) p.parallaxMaxSamples = material.GetFloat("_ParallaxMaxSamples");

            p.useRefraction = material.IsKeywordEnabled("_REFRACTION");
            if (material.HasProperty("_RefractionIndex")) p.refractionIndex = material.GetFloat("_RefractionIndex");
            if (material.HasProperty("_RefractionIntensity")) p.refractionIntensity = material.GetFloat("_RefractionIntensity");
            p.useRefractionMask = material.IsKeywordEnabled("_REFRACTION_MASK");

            // Normal Map
            if (material.HasProperty("_NormalMapIntensity")) p.normalMapIntensity = material.GetFloat("_NormalMapIntensity");

            // Rendering
            p.renderQueue = material.renderQueue;
            if (material.HasProperty("_Cull")) p.cullMode = (int)material.GetFloat("_Cull");

            Debug.Log($"[NataneToonMaterialPreset] Created preset from material '{material.name}'");
        }

        private void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }
    }
}
