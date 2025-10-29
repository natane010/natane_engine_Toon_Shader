using UnityEngine;
using UnityEditor;
using System;

public class NataneToonShaderGUI : ShaderGUI
{
    private MaterialProperty[] properties;
    private MaterialEditor materialEditor;
    private Material targetMaterial;

    // Foldout states
    private static bool showMainTexture = true;
    private static bool showShading = true;
    private static bool showAdvancedLighting = true;
    private static bool showSpecular = true;
    private static bool showRimLight = true;
    private static bool showSSS = true;
    private static bool showMatCap = true;
    private static bool showOutline = true;
    private static bool showEmission = true;
    private static bool showNormalMap = true;
    private static bool showRendering = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.materialEditor = materialEditor;
        this.properties = properties;
        this.targetMaterial = materialEditor.target as Material;

        EditorGUILayout.LabelField("Natane Toon Shader", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawMainTextureSection();
        DrawShadingSection();
        DrawAdvancedLightingSection();
        DrawSpecularSection();
        DrawRimLightSection();
        DrawSSSSection();
        DrawMatCapSection();
        DrawOutlineSection();
        DrawEmissionSection();
        DrawNormalMapSection();
        DrawRenderingSection();
    }

    private void DrawMainTextureSection()
    {
        showMainTexture = EditorGUILayout.Foldout(showMainTexture, "Main Texture", true, EditorStyles.foldoutHeader);
        if (showMainTexture)
        {
            EditorGUI.indentLevel++;
            DrawProperty("_MainTex", "Main Texture");
            DrawProperty("_Color", "Color");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawShadingSection()
    {
        showShading = EditorGUILayout.Foldout(showShading, "Shading", true, EditorStyles.foldoutHeader);
        if (showShading)
        {
            EditorGUI.indentLevel++;

            bool useRamp = DrawToggle("_USE_RAMP", "_UseRamp", "Use Ramp Texture");

            if (useRamp)
            {
                DrawProperty("_RampTex", "Ramp Texture");
                EditorGUILayout.HelpBox("Ramp texture should be a gradient from dark (left) to bright (right).", MessageType.Info);
            }
            else
            {
                DrawProperty("_ShadowColor", "Shadow Color");
                DrawProperty("_ShadowSteps", "Shadow Steps");
                DrawProperty("_ShadowSharpness", "Shadow Sharpness");
            }

            DrawProperty("_ShadowOffset", "Shadow Offset");

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawAdvancedLightingSection()
    {
        showAdvancedLighting = EditorGUILayout.Foldout(showAdvancedLighting, "Advanced Lighting", true, EditorStyles.foldoutHeader);
        if (showAdvancedLighting)
        {
            EditorGUI.indentLevel++;

            DrawProperty("_ShadowReceive", "Shadow Receive");
            EditorGUILayout.HelpBox("Controls how much shadows from other objects affect this material. 1 = full shadows, 0 = no shadows.", MessageType.Info);

            DrawProperty("_ShadowMaxDarkness", "Shadow Max Darkness");
            EditorGUILayout.HelpBox("Minimum brightness in shadows. 0 = fully dark, 1 = no darkening. Prevents shadows from being too black.", MessageType.Info);

            DrawProperty("_LightMinInfluence", "Light Min Influence");
            DrawProperty("_LightMaxInfluence", "Light Max Influence");
            EditorGUILayout.HelpBox("Min/Max control the brightness range. Min prevents too dark, Max prevents overexposure.", MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_BacklightIntensity", "Backlight Intensity");
            if (targetMaterial.GetFloat("_BacklightIntensity") > 0)
            {
                DrawProperty("_BacklightColor", "Backlight Color");
                EditorGUILayout.HelpBox("Backlight adds illumination when light is behind the object, creating a rim-like effect.", MessageType.Info);
            }

            EditorGUILayout.Space();
            DrawProperty("_AdditionalLightIntensity", "Additional Light Intensity");
            EditorGUILayout.HelpBox("Controls the intensity of additional lights (ForwardAdd pass). Lower values prevent over-brightening when using multiple lights. 0 = no additional lights, 1 = full intensity.", MessageType.Info);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawSpecularSection()
    {
        showSpecular = EditorGUILayout.Foldout(showSpecular, "Specular", true, EditorStyles.foldoutHeader);
        if (showSpecular)
        {
            EditorGUI.indentLevel++;

            bool enableSpecular = DrawToggle("_SPECULAR", "_Specular", "Enable Specular");

            if (enableSpecular)
            {
                DrawProperty("_SpecularColor", "Specular Color");
                DrawProperty("_SpecularSize", "Specular Size");
                DrawProperty("_SpecularSoftness", "Specular Softness");
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawRimLightSection()
    {
        showRimLight = EditorGUILayout.Foldout(showRimLight, "Rim Light", true, EditorStyles.foldoutHeader);
        if (showRimLight)
        {
            EditorGUI.indentLevel++;

            bool enableRimLight = DrawToggle("_RIM_LIGHT", "_RimLight", "Enable Rim Light");

            if (enableRimLight)
            {
                DrawProperty("_RimColor", "Rim Color");
                DrawProperty("_RimPower", "Rim Power");
                DrawProperty("_RimIntensity", "Rim Intensity");
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawSSSSection()
    {
        showSSS = EditorGUILayout.Foldout(showSSS, "Subsurface Scattering (SSS)", true, EditorStyles.foldoutHeader);
        if (showSSS)
        {
            EditorGUI.indentLevel++;

            bool enableSSS = DrawToggle("_SSS", "_SSS", "Enable SSS");

            if (enableSSS)
            {
                DrawProperty("_SSSColor", "SSS Color");
                DrawProperty("_SSSIntensity", "SSS Intensity");
                DrawProperty("_SSSPower", "SSS Power");
                DrawProperty("_SSSDistortion", "SSS Distortion");

                EditorGUILayout.Space();
                bool useThicknessMap = DrawToggle("_THICKNESS_MAP", "_UseThicknessMap", "Use Thickness Map");

                if (useThicknessMap)
                {
                    DrawProperty("_ThicknessMap", "Thickness Map");
                    EditorGUILayout.HelpBox("White = thin (more SSS), Black = thick (less SSS)", MessageType.Info);
                }

                DrawProperty("_ThicknessScale", "Thickness Scale");
                EditorGUILayout.HelpBox("SSS simulates light passing through the object. Great for skin, leaves, and thin materials.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawMatCapSection()
    {
        showMatCap = EditorGUILayout.Foldout(showMatCap, "MatCap", true, EditorStyles.foldoutHeader);
        if (showMatCap)
        {
            EditorGUI.indentLevel++;

            bool enableMatCap = DrawToggle("_MATCAP", "_MatCap", "Enable MatCap");

            if (enableMatCap)
            {
                DrawProperty("_MatCapTex", "MatCap Texture");
                DrawProperty("_MatCapIntensity", "MatCap Intensity");
                DrawProperty("_MatCapBlendMode", "MatCap Blend Mode");
                EditorGUILayout.HelpBox("MatCap texture should be a spherical reflection map.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawOutlineSection()
    {
        showOutline = EditorGUILayout.Foldout(showOutline, "Outline", true, EditorStyles.foldoutHeader);
        if (showOutline)
        {
            EditorGUI.indentLevel++;

            bool enableOutline = DrawToggle("_OUTLINE", "_Outline", "Enable Outline");

            if (enableOutline)
            {
                DrawProperty("_OutlineWidth", "Outline Width");
                DrawProperty("_OutlineColor", "Outline Color");
                EditorGUILayout.HelpBox("Outline uses inverted hull method. May not work well on very low-poly models.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawEmissionSection()
    {
        showEmission = EditorGUILayout.Foldout(showEmission, "Emission", true, EditorStyles.foldoutHeader);
        if (showEmission)
        {
            EditorGUI.indentLevel++;

            bool enableEmission = DrawToggle("_EMISSION", "_Emission", "Enable Emission");

            if (enableEmission)
            {
                DrawProperty("_EmissionColor", "Emission Color");
                DrawProperty("_EmissionMap", "Emission Map");
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawNormalMapSection()
    {
        showNormalMap = EditorGUILayout.Foldout(showNormalMap, "Normal Map", true, EditorStyles.foldoutHeader);
        if (showNormalMap)
        {
            EditorGUI.indentLevel++;

            bool useNormalMap = DrawToggle("_NORMALMAP", "_UseNormalMap", "Use Normal Map");

            if (useNormalMap)
            {
                DrawProperty("_BumpMap", "Normal Map");
                DrawProperty("_BumpScale", "Normal Scale");
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawRenderingSection()
    {
        showRendering = EditorGUILayout.Foldout(showRendering, "Rendering", true, EditorStyles.foldoutHeader);
        if (showRendering)
        {
            EditorGUI.indentLevel++;
            DrawProperty("_Cull", "Cull Mode");
            DrawProperty("_ZWrite", "Z Write");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawProperty(string propertyName, string label)
    {
        MaterialProperty property = FindProperty(propertyName, properties);
        materialEditor.ShaderProperty(property, label);
    }

    private bool DrawToggle(string keyword, string propertyName, string label)
    {
        MaterialProperty property = FindProperty(propertyName, properties);

        EditorGUI.BeginChangeCheck();
        bool enabled = EditorGUILayout.Toggle(label, property.floatValue > 0.5f);

        if (EditorGUI.EndChangeCheck())
        {
            property.floatValue = enabled ? 1.0f : 0.0f;

            // Set shader keyword
            if (enabled)
                targetMaterial.EnableKeyword(keyword);
            else
                targetMaterial.DisableKeyword(keyword);
        }

        return enabled;
    }
}
