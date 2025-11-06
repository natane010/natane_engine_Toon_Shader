using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// lilToon から Natane Toon Shader への自動移行ツール
/// </summary>
public class LilToonMigrationTool : EditorWindow
{
    private List<Material> lilToonMaterials = new List<Material>();
    private Vector2 scrollPosition;
    private bool createBackup = true;
    private bool replaceOriginal = false;

    [MenuItem("Tools/Natane/lilToon Migration Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<LilToonMigrationTool>("lilToon Migration");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    private void OnEnable()
    {
        ScanForLilToonMaterials();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("lilToon to Natane Toon Shader Migration Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool automatically converts lilToon materials to Natane Toon Shader.\n" +
            "It will map properties as closely as possible and preserve texture references.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Options
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        createBackup = EditorGUILayout.Toggle("Create Backup", createBackup);
        replaceOriginal = EditorGUILayout.Toggle("Replace Original (Destructive)", replaceOriginal);

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
        if (GUILayout.Button("Scan for lilToon Materials", GUILayout.Height(30)))
        {
            ScanForLilToonMaterials();
        }

        EditorGUILayout.Space();

        // Materials list
        EditorGUILayout.LabelField($"Found {lilToonMaterials.Count} lilToon Materials", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        foreach (var material in lilToonMaterials)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(material, typeof(Material), false);

            if (GUILayout.Button("Convert", GUILayout.Width(80)))
            {
                ConvertMaterial(material);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Convert all button
        GUI.enabled = lilToonMaterials.Count > 0;
        if (GUILayout.Button("Convert All Materials", GUILayout.Height(40)))
        {
            ConvertAllMaterials();
        }
        GUI.enabled = true;
    }

    private void ScanForLilToonMaterials()
    {
        lilToonMaterials.Clear();

        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");

        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null && material.shader != null)
            {
                string shaderName = material.shader.name;

                // Check if it's a lilToon shader
                if (shaderName.Contains("lilToon") || shaderName.StartsWith("_lil/"))
                {
                    lilToonMaterials.Add(material);
                }
            }
        }

        Debug.Log($"Found {lilToonMaterials.Count} lilToon materials.");
    }

    private void ConvertAllMaterials()
    {
        if (!EditorUtility.DisplayDialog(
            "Convert All Materials",
            $"Are you sure you want to convert {lilToonMaterials.Count} materials?",
            "Yes", "Cancel"))
        {
            return;
        }

        int successCount = 0;

        for (int i = 0; i < lilToonMaterials.Count; i++)
        {
            EditorUtility.DisplayProgressBar(
                "Converting Materials",
                $"Converting {i + 1}/{lilToonMaterials.Count}: {lilToonMaterials[i].name}",
                (float)i / lilToonMaterials.Count
            );

            if (ConvertMaterial(lilToonMaterials[i]))
            {
                successCount++;
            }
        }

        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog(
            "Conversion Complete",
            $"Successfully converted {successCount}/{lilToonMaterials.Count} materials.",
            "OK"
        );

        // Rescan
        ScanForLilToonMaterials();
    }

    private bool ConvertMaterial(Material sourceMaterial)
    {
        try
        {
            // Create backup if requested
            if (createBackup)
            {
                string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                string backupPath = sourcePath.Replace(".mat", "_lilToon_backup.mat");
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

            // Find Natane Toon Shader
            Shader nataneToonShader = Shader.Find("Natane/Toon Shader");
            if (nataneToonShader == null)
            {
                Debug.LogError("Natane Toon Shader not found! Please make sure it's in your project.");
                return false;
            }

            // Store original properties before changing shader
            var originalProperties = CaptureProperties(sourceMaterial);

            // Change shader
            targetMaterial.shader = nataneToonShader;

            // Map properties
            MapProperties(originalProperties, targetMaterial);

            EditorUtility.SetDirty(targetMaterial);
            AssetDatabase.SaveAssets();

            Debug.Log($"Successfully converted: {sourceMaterial.name}");
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

        // Textures
        CaptureTexture(material, "_MainTex", properties);
        CaptureTexture(material, "_BumpMap", properties);
        CaptureTexture(material, "_EmissionMap", properties);
        CaptureTexture(material, "_MatCapTex", properties);

        // Colors
        CaptureColor(material, "_Color", properties);
        CaptureColor(material, "_ShadowColor", properties);
        CaptureColor(material, "_EmissionColor", properties);

        // Floats
        CaptureFloat(material, "_Cutoff", properties);
        CaptureFloat(material, "_BumpScale", properties);

        // lilToon specific properties
        CaptureTexture(material, "_lilMainTex", properties);
        CaptureColor(material, "_lilColor", properties);
        CaptureColor(material, "_lilShadowColor", properties);
        CaptureFloat(material, "_lilShadowBorder", properties);
        CaptureFloat(material, "_lilShadowBlur", properties);

        // Rim light
        CaptureColor(material, "_RimColor", properties);
        CaptureFloat(material, "_RimPower", properties);
        CaptureFloat(material, "_RimFresnelPower", properties);

        // Outline
        CaptureFloat(material, "_OutlineWidth", properties);
        CaptureColor(material, "_OutlineColor", properties);

        // Emission
        CaptureTexture(material, "_EmissionMap", properties);
        CaptureColor(material, "_EmissionColor", properties);

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

    private void MapProperties(Dictionary<string, object> sourceProps, Material targetMaterial)
    {
        // Main Texture
        SetTextureIfExists(sourceProps, "_MainTex", targetMaterial, "_MainTex");
        SetTextureIfExists(sourceProps, "_lilMainTex", targetMaterial, "_MainTex");

        // Color
        SetColorIfExists(sourceProps, "_Color", targetMaterial, "_Color");
        SetColorIfExists(sourceProps, "_lilColor", targetMaterial, "_Color");

        // Shadow Color
        if (sourceProps.ContainsKey("_lilShadowColor"))
        {
            targetMaterial.SetColor("_ShadowColor", (Color)sourceProps["_lilShadowColor"]);
            targetMaterial.EnableKeyword("_USE_RAMP");
        }
        else
        {
            SetColorIfExists(sourceProps, "_ShadowColor", targetMaterial, "_ShadowColor");
        }

        // Shadow Settings
        if (sourceProps.ContainsKey("_lilShadowBorder"))
        {
            float border = (float)sourceProps["_lilShadowBorder"];
            targetMaterial.SetFloat("_ShadowOffset", Mathf.Lerp(-0.5f, 0.5f, border));
        }

        if (sourceProps.ContainsKey("_lilShadowBlur"))
        {
            float blur = (float)sourceProps["_lilShadowBlur"];
            targetMaterial.SetFloat("_ShadowSharpness", Mathf.Lerp(0.001f, 0.5f, 1.0f - blur));
        }

        // Normal Map
        SetTextureIfExists(sourceProps, "_BumpMap", targetMaterial, "_BumpMap");
        SetFloatIfExists(sourceProps, "_BumpScale", targetMaterial, "_BumpScale");

        if (sourceProps.ContainsKey("_BumpMap") && sourceProps["_BumpMap"] != null)
        {
            targetMaterial.SetFloat("_UseNormalMap", 1.0f);
            targetMaterial.EnableKeyword("_NORMALMAP");
        }

        // Rim Light
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
        else if (sourceProps.ContainsKey("_RimPower"))
        {
            SetFloatIfExists(sourceProps, "_RimPower", targetMaterial, "_RimPower");
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
                targetMaterial.SetFloat("_OutlineWidth", Mathf.Clamp(width * 0.01f, 0, 0.1f));
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

        // MatCap
        SetTextureIfExists(sourceProps, "_MatCapTex", targetMaterial, "_MatCapTex");
        if (sourceProps.ContainsKey("_MatCapTex") && sourceProps["_MatCapTex"] != null)
        {
            targetMaterial.SetFloat("_MatCap", 1.0f);
            targetMaterial.EnableKeyword("_MATCAP");
        }

        // Default settings for good toon shading
        if (!sourceProps.ContainsKey("_lilShadowBorder"))
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
