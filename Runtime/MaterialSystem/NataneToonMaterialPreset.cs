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

        // Detail Map (Background)
        public bool useDetailMap = false;
        public float detailAlbedoScale = 0.5f;
        public float detailNormalScale = 1f;
        public float detailUVSet = 0f;
        public float detailTiling = 1f;

        // Triplanar (Background)
        public bool useTriplanar = false;
        public float triplanarScale = 1f;
        public float triplanarBlendSharpness = 4f;

        // Height Fog (Background)
        public bool useHeightFog = false;
        public Color heightFogColor = new Color(0.7f, 0.8f, 0.9f, 1f);
        public float heightFogStart = 0f;
        public float heightFogEnd = 10f;
        public float heightFogDensity = 0.5f;

        // Surface Cover (Background)
        public bool useSurfaceCover = false;
        public Color coverColor = Color.white;
        public float coverAmount = 0.5f;
        public float coverThreshold = 0.3f;
        public float coverBlendSharpness = 4f;
        public float coverTiling = 0.1f;

        // Mirror Control (VRChat)
        public bool useMirrorControl = false;
        public float mirrorMode = 0f;
        public float mirrorEmissionMultiplier = 1f;

        // Quest Lite
        public bool useQuestLite = false;

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
            if (material.HasProperty("_ShadowSteps")) material.SetFloat("_ShadowSteps", p.toonSteps);
            if (material.HasProperty("_ShadowSharpness")) material.SetFloat("_ShadowSharpness", p.toonSharpness);
            if (material.HasProperty("_ShadowReceive")) material.SetFloat("_ShadowReceive", p.shadowReceive);
            if (material.HasProperty("_ShadowMaxDarkness")) material.SetFloat("_ShadowMaxDarkness", p.shadowIntensityMax);
            if (material.HasProperty("_LightMinInfluence")) material.SetFloat("_LightMinInfluence", p.lightInfluence);
            if (material.HasProperty("_LightColorInfluence")) material.SetFloat("_LightColorInfluence", p.lightColorInfluence);
            if (material.HasProperty("_BacklightIntensity")) material.SetFloat("_BacklightIntensity", p.backlight);

            // Specular
            SetKeyword(material, "_SPECULAR", p.useSpecular);
            if (material.HasProperty("_SpecularColor")) material.SetColor("_SpecularColor", p.specularColor);
            if (material.HasProperty("_SpecularSize")) material.SetFloat("_SpecularSize", p.specularSize);
            if (material.HasProperty("_SpecularSoftness")) material.SetFloat("_SpecularSoftness", p.specularSharpness);
            if (material.HasProperty("_SpecularBlend")) material.SetFloat("_SpecularBlend", Mathf.Clamp01(p.specularIntensity));
            SetKeyword(material, "_SPECULAR_MASK", p.useSpecularMask);

            // Rim Light
            SetKeyword(material, "_RIM_LIGHT", p.useRimLight);
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
            if (material.HasProperty("_ThicknessScale")) material.SetFloat("_ThicknessScale", p.sssScale);
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
            if (material.HasProperty("_EmissionGlow")) material.SetFloat("_EmissionGlow", p.emissionIntensity);
            SetKeyword(material, "_EMISSION_MASK", p.useEmissionMask);

            // Emission Animation (controlled by _EMISSION keyword, no separate animation keyword)
            if (material.HasProperty("_EmissionScrollSpeed")) material.SetFloat("_EmissionScrollSpeed", p.emissionScrollSpeed);
            if (material.HasProperty("_EmissionPulseSpeed")) material.SetFloat("_EmissionPulseSpeed", p.emissionPulseSpeed);
            if (material.HasProperty("_EmissionPulseAmplitude")) material.SetFloat("_EmissionPulseAmplitude", p.emissionPulseMax);

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
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", p.normalMapIntensity);

            // Detail Map
            SetKeyword(material, "_DETAIL_MAP", p.useDetailMap);
            if (material.HasProperty("_DetailAlbedoScale")) material.SetFloat("_DetailAlbedoScale", p.detailAlbedoScale);
            if (material.HasProperty("_DetailNormalScale")) material.SetFloat("_DetailNormalScale", p.detailNormalScale);
            if (material.HasProperty("_DetailUVSet")) material.SetFloat("_DetailUVSet", p.detailUVSet);
            if (material.HasProperty("_DetailTiling")) material.SetFloat("_DetailTiling", p.detailTiling);

            // Triplanar
            SetKeyword(material, "_TRIPLANAR", p.useTriplanar);
            if (material.HasProperty("_TriplanarScale")) material.SetFloat("_TriplanarScale", p.triplanarScale);
            if (material.HasProperty("_TriplanarBlendSharpness")) material.SetFloat("_TriplanarBlendSharpness", p.triplanarBlendSharpness);

            // Height Fog
            SetKeyword(material, "_HEIGHT_FOG", p.useHeightFog);
            if (material.HasProperty("_HeightFogColor")) material.SetColor("_HeightFogColor", p.heightFogColor);
            if (material.HasProperty("_HeightFogStart")) material.SetFloat("_HeightFogStart", p.heightFogStart);
            if (material.HasProperty("_HeightFogEnd")) material.SetFloat("_HeightFogEnd", p.heightFogEnd);
            if (material.HasProperty("_HeightFogDensity")) material.SetFloat("_HeightFogDensity", p.heightFogDensity);

            // Surface Cover
            SetKeyword(material, "_SURFACE_COVER", p.useSurfaceCover);
            if (material.HasProperty("_CoverColor")) material.SetColor("_CoverColor", p.coverColor);
            if (material.HasProperty("_CoverAmount")) material.SetFloat("_CoverAmount", p.coverAmount);
            if (material.HasProperty("_CoverThreshold")) material.SetFloat("_CoverThreshold", p.coverThreshold);
            if (material.HasProperty("_CoverBlendSharpness")) material.SetFloat("_CoverBlendSharpness", p.coverBlendSharpness);
            if (material.HasProperty("_CoverTiling")) material.SetFloat("_CoverTiling", p.coverTiling);

            // Mirror Control
            SetKeyword(material, "_MIRROR_CONTROL", p.useMirrorControl);
            if (material.HasProperty("_MirrorMode")) material.SetFloat("_MirrorMode", p.mirrorMode);
            if (material.HasProperty("_MirrorEmissionMultiplier")) material.SetFloat("_MirrorEmissionMultiplier", p.mirrorEmissionMultiplier);

            // Quest Lite
            SetKeyword(material, "_QUEST_LITE", p.useQuestLite);

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
            if (material.HasProperty("_ShadowSteps")) p.toonSteps = (int)material.GetFloat("_ShadowSteps");
            if (material.HasProperty("_ShadowSharpness")) p.toonSharpness = material.GetFloat("_ShadowSharpness");
            if (material.HasProperty("_ShadowReceive")) p.shadowReceive = material.GetFloat("_ShadowReceive");
            if (material.HasProperty("_ShadowMaxDarkness")) p.shadowIntensityMax = material.GetFloat("_ShadowMaxDarkness");
            if (material.HasProperty("_LightMinInfluence")) p.lightInfluence = material.GetFloat("_LightMinInfluence");
            if (material.HasProperty("_LightColorInfluence")) p.lightColorInfluence = material.GetFloat("_LightColorInfluence");
            if (material.HasProperty("_BacklightIntensity")) p.backlight = material.GetFloat("_BacklightIntensity");

            // Specular
            p.useSpecular = material.IsKeywordEnabled("_SPECULAR");
            if (material.HasProperty("_SpecularColor")) p.specularColor = material.GetColor("_SpecularColor");
            if (material.HasProperty("_SpecularSize")) p.specularSize = material.GetFloat("_SpecularSize");
            if (material.HasProperty("_SpecularSoftness")) p.specularSharpness = material.GetFloat("_SpecularSoftness");
            if (material.HasProperty("_SpecularBlend")) p.specularIntensity = material.GetFloat("_SpecularBlend");
            p.useSpecularMask = material.IsKeywordEnabled("_SPECULAR_MASK");

            // Rim Light
            p.useRimLight = material.IsKeywordEnabled("_RIM_LIGHT");
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
            if (material.HasProperty("_ThicknessScale")) p.sssScale = material.GetFloat("_ThicknessScale");
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
            if (material.HasProperty("_EmissionGlow")) p.emissionIntensity = material.GetFloat("_EmissionGlow");
            p.useEmissionMask = material.IsKeywordEnabled("_EMISSION_MASK");

            // Emission Animation (controlled by _EMISSION keyword, no separate animation keyword)
            if (material.HasProperty("_EmissionScrollSpeed")) p.emissionScrollSpeed = material.GetFloat("_EmissionScrollSpeed");
            if (material.HasProperty("_EmissionPulseSpeed")) p.emissionPulseSpeed = material.GetFloat("_EmissionPulseSpeed");
            if (material.HasProperty("_EmissionPulseAmplitude")) p.emissionPulseMax = material.GetFloat("_EmissionPulseAmplitude");

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
            if (material.HasProperty("_BumpScale")) p.normalMapIntensity = material.GetFloat("_BumpScale");

            // Detail Map
            p.useDetailMap = material.IsKeywordEnabled("_DETAIL_MAP");
            if (material.HasProperty("_DetailAlbedoScale")) p.detailAlbedoScale = material.GetFloat("_DetailAlbedoScale");
            if (material.HasProperty("_DetailNormalScale")) p.detailNormalScale = material.GetFloat("_DetailNormalScale");
            if (material.HasProperty("_DetailUVSet")) p.detailUVSet = material.GetFloat("_DetailUVSet");
            if (material.HasProperty("_DetailTiling")) p.detailTiling = material.GetFloat("_DetailTiling");

            // Triplanar
            p.useTriplanar = material.IsKeywordEnabled("_TRIPLANAR");
            if (material.HasProperty("_TriplanarScale")) p.triplanarScale = material.GetFloat("_TriplanarScale");
            if (material.HasProperty("_TriplanarBlendSharpness")) p.triplanarBlendSharpness = material.GetFloat("_TriplanarBlendSharpness");

            // Height Fog
            p.useHeightFog = material.IsKeywordEnabled("_HEIGHT_FOG");
            if (material.HasProperty("_HeightFogColor")) p.heightFogColor = material.GetColor("_HeightFogColor");
            if (material.HasProperty("_HeightFogStart")) p.heightFogStart = material.GetFloat("_HeightFogStart");
            if (material.HasProperty("_HeightFogEnd")) p.heightFogEnd = material.GetFloat("_HeightFogEnd");
            if (material.HasProperty("_HeightFogDensity")) p.heightFogDensity = material.GetFloat("_HeightFogDensity");

            // Surface Cover
            p.useSurfaceCover = material.IsKeywordEnabled("_SURFACE_COVER");
            if (material.HasProperty("_CoverColor")) p.coverColor = material.GetColor("_CoverColor");
            if (material.HasProperty("_CoverAmount")) p.coverAmount = material.GetFloat("_CoverAmount");
            if (material.HasProperty("_CoverThreshold")) p.coverThreshold = material.GetFloat("_CoverThreshold");
            if (material.HasProperty("_CoverBlendSharpness")) p.coverBlendSharpness = material.GetFloat("_CoverBlendSharpness");
            if (material.HasProperty("_CoverTiling")) p.coverTiling = material.GetFloat("_CoverTiling");

            // Mirror Control
            p.useMirrorControl = material.IsKeywordEnabled("_MIRROR_CONTROL");
            if (material.HasProperty("_MirrorMode")) p.mirrorMode = material.GetFloat("_MirrorMode");
            if (material.HasProperty("_MirrorEmissionMultiplier")) p.mirrorEmissionMultiplier = material.GetFloat("_MirrorEmissionMultiplier");

            // Quest Lite
            p.useQuestLite = material.IsKeywordEnabled("_QUEST_LITE");

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
