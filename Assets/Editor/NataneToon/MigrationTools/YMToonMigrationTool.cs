using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// YMToon から Natane Toon Shader への自動移行ツール
/// </summary>
public class YMToonMigrationTool : EditorWindow
{
    private List<Material> ymToonMaterials = new List<Material>();
    private Vector2 scrollPosition;
    private bool createBackup = true;
    private bool replaceOriginal = false;
    private bool autoDetectVariant = true;

    [MenuItem("Tools/Natane/YMToon Migration Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<YMToonMigrationTool>("YMToon Migration");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    private void OnEnable()
    {
        ScanForYMToonMaterials();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("YMToon to Natane Toon Shader Migration Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool automatically converts YMToon materials to Natane Toon Shader.\n" +
            "It will detect Opaque/Cutout/Transparent variants and convert accordingly.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Options
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        createBackup = EditorGUILayout.Toggle("Create Backup", createBackup);
        replaceOriginal = EditorGUILayout.Toggle("Replace Original (Destructive)", replaceOriginal);
        autoDetectVariant = EditorGUILayout.Toggle("Auto Detect Variant", autoDetectVariant);

        if (replaceOriginal)
        {
            EditorGUILayout.HelpBox(
                "WARNING: This will permanently modify your original materials! " +
                "Make sure you have a backup of your project.",
                MessageType.Warning
            );
        }

        EditorGUILayout.Space();

        // Scan button
        if (GUILayout.Button("Scan for YMToon Materials", GUILayout.Height(30)))
        {
            ScanForYMToonMaterials();
        }

        EditorGUILayout.Space();

        // Materials list
        EditorGUILayout.LabelField($"Found {ymToonMaterials.Count} YMToon Materials", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        foreach (var material in ymToonMaterials)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(material, typeof(Material), false);

            // Show detected variant
            string variant = DetectVariant(material);
            EditorGUILayout.LabelField(variant, GUILayout.Width(100));

            if (GUILayout.Button("Convert", GUILayout.Width(80)))
            {
                ConvertMaterial(material);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Convert all button
        GUI.enabled = ymToonMaterials.Count > 0;
        if (GUILayout.Button("Convert All Materials", GUILayout.Height(40)))
        {
            ConvertAllMaterials();
        }
        GUI.enabled = true;
    }

    private void ScanForYMToonMaterials()
    {
        ymToonMaterials.Clear();

        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");

        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null && material.shader != null)
            {
                string shaderName = material.shader.name;

                // Check if it's a YMToon shader
                if (shaderName.Contains("YMToon") ||
                    shaderName.Contains("MToon") && !shaderName.Contains("lilToon"))
                {
                    ymToonMaterials.Add(material);
                }
            }
        }

        Debug.Log($"Found {ymToonMaterials.Count} YMToon materials.");
    }

    private string DetectVariant(Material material)
    {
        if (material == null || material.shader == null)
            return "Unknown";

        string shaderName = material.shader.name.ToLower();

        if (shaderName.Contains("transparent"))
            return "Transparent";
        else if (shaderName.Contains("cutout"))
            return "Cutout";
        else
            return "Opaque";
    }

    private void ConvertAllMaterials()
    {
        if (!EditorUtility.DisplayDialog(
            "Convert All Materials",
            $"Are you sure you want to convert {ymToonMaterials.Count} materials?",
            "Yes", "Cancel"))
        {
            return;
        }

        int successCount = 0;

        for (int i = 0; i < ymToonMaterials.Count; i++)
        {
            EditorUtility.DisplayProgressBar(
                "Converting Materials",
                $"Converting {i + 1}/{ymToonMaterials.Count}: {ymToonMaterials[i].name}",
                (float)i / ymToonMaterials.Count
            );

            if (ConvertMaterial(ymToonMaterials[i]))
            {
                successCount++;
            }
        }

        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog(
            "Conversion Complete",
            $"Successfully converted {successCount}/{ymToonMaterials.Count} materials.",
            "OK"
        );

        // Rescan
        ScanForYMToonMaterials();
    }

    private bool ConvertMaterial(Material sourceMaterial)
    {
        try
        {
            // Create backup if requested
            if (createBackup)
            {
                string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                string backupPath = sourcePath.Replace(".mat", "_YMToon_backup.mat");
                AssetDatabase.CopyAsset(sourcePath, backupPath);
                Debug.Log($"Created backup: {backupPath}");
            }

            Material targetMaterial;

            if (replaceOriginal)
            {
                targetMaterial = sourceMaterial;
            }
            else
            {
                // Create new material
                targetMaterial = new Material(sourceMaterial);
                string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                string newPath = sourcePath.Replace(".mat", "_NataneToon.mat");
                AssetDatabase.CreateAsset(targetMaterial, newPath);
            }

            // Detect variant
            string variant = "Opaque";
            if (autoDetectVariant)
            {
                variant = DetectVariant(sourceMaterial);
            }

            // Find appropriate Natane Toon Shader
            string shaderPath = "Natane/Toon Shader";
            if (variant == "Cutout")
                shaderPath = "Natane/Toon Shader (Cutout)";
            else if (variant == "Transparent")
                shaderPath = "Natane/Toon Shader (Transparent)";

            Shader nataneToonShader = Shader.Find(shaderPath);
            if (nataneToonShader == null)
            {
                Debug.LogError($"Natane Toon Shader '{shaderPath}' not found!");
                return false;
            }

            // Store original properties
            var originalProperties = CaptureProperties(sourceMaterial);

            // Change shader
            targetMaterial.shader = nataneToonShader;

            // Map properties
            MapProperties(originalProperties, targetMaterial, variant);

            EditorUtility.SetDirty(targetMaterial);
            AssetDatabase.SaveAssets();

            Debug.Log($"Successfully converted: {sourceMaterial.name} ({variant})");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to convert {sourceMaterial.name}: {e.Message}");
            return false;
        }
    }

    private Dictionary<string, object> CaptureProperties(Material material)
    {
        var properties = new Dictionary<string, object>();

        // Textures - YMToon property names
        CaptureTexture(material, "_MainTex", properties);
        CaptureTexture(material, "_BumpMap", properties);
        CaptureTexture(material, "_EmissionMap", properties);
        CaptureTexture(material, "_SphereAdd", properties); // MatCap
        CaptureTexture(material, "_Shade1Tex", properties);
        CaptureTexture(material, "_Shade2Tex", properties);

        // Colors
        CaptureColor(material, "_Color", properties);
        CaptureColor(material, "_ShadeColor", properties);
        CaptureColor(material, "_RimColor", properties);
        CaptureColor(material, "_EmissionColor", properties);
        CaptureColor(material, "_OutlineColor", properties);

        // Floats
        CaptureFloat(material, "_Cutoff", properties);
        CaptureFloat(material, "_BumpScale", properties);
        CaptureFloat(material, "_ShadeShift", properties);
        CaptureFloat(material, "_ShadeToony", properties);
        CaptureFloat(material, "_RimLightingMix", properties);
        CaptureFloat(material, "_RimFresnelPower", properties);
        CaptureFloat(material, "_RimLift", properties);
        CaptureFloat(material, "_OutlineWidth", properties);
        CaptureFloat(material, "_OutlineScaledMaxDistance", properties);

        return properties;
    }

    private void CaptureTexture(Material material, string propertyName, Dictionary<string, object> properties)
    {
        if (material.HasProperty(propertyName))
        {
            properties[propertyName] = material.GetTexture(propertyName);
        }
    }

    private void CaptureColor(Material material, string propertyName, Dictionary<string, object> properties)
    {
        if (material.HasProperty(propertyName))
        {
            properties[propertyName] = material.GetColor(propertyName);
        }
    }

    private void CaptureFloat(Material material, string propertyName, Dictionary<string, object> properties)
    {
        if (material.HasProperty(propertyName))
        {
            properties[propertyName] = material.GetFloat(propertyName);
        }
    }

    private void MapProperties(Dictionary<string, object> sourceProps, Material targetMaterial, string variant)
    {
        // Main Texture
        SetTextureIfExists(sourceProps, "_MainTex", targetMaterial, "_MainTex");

        // Color
        SetColorIfExists(sourceProps, "_Color", targetMaterial, "_Color");

        // Shadow Color - YMToon uses _ShadeColor
        if (sourceProps.ContainsKey("_ShadeColor"))
        {
            targetMaterial.SetColor("_ShadowColor", (Color)sourceProps["_ShadeColor"]);
        }

        // Shadow Settings from YMToon
        if (sourceProps.ContainsKey("_ShadeShift"))
        {
            // ShadeShift affects shadow threshold
            float shadeShift = (float)sourceProps["_ShadeShift"];
            targetMaterial.SetFloat("_ShadowOffset", Mathf.Clamp(shadeShift * 0.5f, -1f, 1f));
        }

        if (sourceProps.ContainsKey("_ShadeToony"))
        {
            // ShadeToony affects sharpness
            float shadeToony = (float)sourceProps["_ShadeToony"];
            targetMaterial.SetFloat("_ShadowSharpness", Mathf.Lerp(0.001f, 0.3f, 1.0f - shadeToony));
            targetMaterial.SetFloat("_ShadowSteps", 2);
        }

        // Normal Map
        SetTextureIfExists(sourceProps, "_BumpMap", targetMaterial, "_BumpMap");
        SetFloatIfExists(sourceProps, "_BumpScale", targetMaterial, "_BumpScale");

        if (sourceProps.ContainsKey("_BumpMap") && sourceProps["_BumpMap"] != null)
        {
            targetMaterial.SetFloat("_UseNormalMap", 1.0f);
            targetMaterial.EnableKeyword("_NORMALMAP");
        }

        // Rim Light - YMToon has specific rim properties
        bool hasRim = false;
        if (sourceProps.ContainsKey("_RimColor"))
        {
            Color rimColor = (Color)sourceProps["_RimColor"];
            if (rimColor.a > 0 || rimColor.maxColorComponent > 0)
            {
                targetMaterial.SetColor("_RimColor", rimColor);
                hasRim = true;
            }
        }

        if (sourceProps.ContainsKey("_RimFresnelPower"))
        {
            float power = (float)sourceProps["_RimFresnelPower"];
            targetMaterial.SetFloat("_RimPower", Mathf.Clamp(power, 0.1f, 10f));
            hasRim = true;
        }

        if (sourceProps.ContainsKey("_RimLift"))
        {
            float lift = (float)sourceProps["_RimLift"];
            targetMaterial.SetFloat("_RimIntensity", Mathf.Clamp(lift * 2f, 0f, 5f));
            hasRim = true;
        }

        if (hasRim)
        {
            targetMaterial.SetFloat("_RimLight", 1.0f);
            targetMaterial.EnableKeyword("_RIM_LIGHT");
        }

        // Outline
        bool hasOutline = false;
        if (sourceProps.ContainsKey("_OutlineWidth"))
        {
            float width = (float)sourceProps["_OutlineWidth"];
            if (width > 0)
            {
                // YMToon uses different scale
                targetMaterial.SetFloat("_OutlineWidth", Mathf.Clamp(width * 0.1f, 0, 0.1f));
                hasOutline = true;
            }
        }

        if (sourceProps.ContainsKey("_OutlineColor"))
        {
            targetMaterial.SetColor("_OutlineColor", (Color)sourceProps["_OutlineColor"]);
            hasOutline = true;
        }

        if (hasOutline)
        {
            targetMaterial.SetFloat("_Outline", 1.0f);
            targetMaterial.EnableKeyword("_OUTLINE");
        }

        // Emission
        SetTextureIfExists(sourceProps, "_EmissionMap", targetMaterial, "_EmissionMap");

        bool hasEmission = false;
        if (sourceProps.ContainsKey("_EmissionColor"))
        {
            Color emissionColor = (Color)sourceProps["_EmissionColor"];
            if (emissionColor.maxColorComponent > 0)
            {
                targetMaterial.SetColor("_EmissionColor", emissionColor);
                hasEmission = true;
            }
        }

        if (hasEmission || (sourceProps.ContainsKey("_EmissionMap") && sourceProps["_EmissionMap"] != null))
        {
            targetMaterial.SetFloat("_Emission", 1.0f);
            targetMaterial.EnableKeyword("_EMISSION");
        }

        // MatCap - YMToon uses _SphereAdd
        if (sourceProps.ContainsKey("_SphereAdd") && sourceProps["_SphereAdd"] != null)
        {
            targetMaterial.SetTexture("_MatCapTex", (Texture)sourceProps["_SphereAdd"]);
            targetMaterial.SetFloat("_MatCap", 1.0f);
            targetMaterial.SetFloat("_MatCapBlendMode", 0); // Add mode
            targetMaterial.EnableKeyword("_MATCAP");
        }

        // Cutout specific
        if (variant == "Cutout")
        {
            SetFloatIfExists(sourceProps, "_Cutoff", targetMaterial, "_Cutoff");
        }

        // Set default values for good toon shading
        if (!sourceProps.ContainsKey("_ShadeToony"))
        {
            targetMaterial.SetFloat("_ShadowSteps", 2);
            targetMaterial.SetFloat("_ShadowSharpness", 0.1f);
        }
    }

    private void SetTextureIfExists(Dictionary<string, object> source, string sourceKey, Material target, string targetKey)
    {
        if (source.ContainsKey(sourceKey) && source[sourceKey] != null)
        {
            target.SetTexture(targetKey, (Texture)source[sourceKey]);
        }
    }

    private void SetColorIfExists(Dictionary<string, object> source, string sourceKey, Material target, string targetKey)
    {
        if (source.ContainsKey(sourceKey))
        {
            target.SetColor(targetKey, (Color)source[sourceKey]);
        }
    }

    private void SetFloatIfExists(Dictionary<string, object> source, string sourceKey, Material target, string targetKey)
    {
        if (source.ContainsKey(sourceKey))
        {
            target.SetFloat(targetKey, (float)source[sourceKey]);
        }
    }
}
