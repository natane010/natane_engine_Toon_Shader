using UnityEngine;
using UnityEditor;
using NataneParticleSystem;
using System.Collections.Generic;

namespace NataneParticleSystemEditor
{
    /// <summary>
    /// Particle Effect Editor Window - Visual editor for creating particle effects
    /// パーティクルエフェクトエディタウィンドウ - パーティクルエフェクト作成用のビジュアルエディタ
    /// </summary>
    public class ParticleEffectEditorWindow : EditorWindow
    {
        private ParticleEffectPreset currentPreset;
        private GameObject previewObject;
        private ParticleSystem previewParticleSystem;
        private Vector2 scrollPosition;
        private bool autoPlay = true;

        // UI State
        private bool showMainSettings = true;
        private bool showEmissionSettings = true;
        private bool showShapeSettings = true;
        private bool showColorSettings = true;
        private bool showSizeSettings = true;
        private bool showVelocitySettings = false;
        private bool showRotationSettings = false;
        private bool showRenderSettings = true;
        private bool showTrailSettings = false;

        // Template presets
        private static string[] templateNames = new string[]
        {
            "Explosion",
            "Fire",
            "Smoke",
            "Magic Sparkles",
            "Electric",
            "Water Splash",
            "Heal Effect",
            "Custom"
        };

        [MenuItem("Tools/Natane/パーティクルエフェクトエディタ Particle Effect Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ParticleEffectEditorWindow>("パーティクルエフェクトエディタ Particle Effect Editor");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnEnable()
        {
            // Load or create preview object
            if (previewObject == null)
            {
                CreatePreviewObject();
            }
        }

        private void OnDisable()
        {
            // Clean up preview object
            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawPresetSelection();

            if (currentPreset != null)
            {
                EditorGUILayout.Space(10);
                DrawPresetInfo();

                EditorGUILayout.Space(10);
                DrawMainSettings();

                EditorGUILayout.Space(10);
                DrawEmissionSettings();

                EditorGUILayout.Space(10);
                DrawShapeSettings();

                EditorGUILayout.Space(10);
                DrawColorSettings();

                EditorGUILayout.Space(10);
                DrawSizeSettings();

                EditorGUILayout.Space(10);
                DrawVelocitySettings();

                EditorGUILayout.Space(10);
                DrawRotationSettings();

                EditorGUILayout.Space(10);
                DrawRenderSettings();

                EditorGUILayout.Space(10);
                DrawTrailSettings();

                EditorGUILayout.Space(20);
                DrawPreviewControls();
            }
            else
            {
                EditorGUILayout.HelpBox("Create or select a preset to start editing", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New Preset", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                CreateNewPreset();
            }

            if (GUILayout.Button("Load Template", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ShowTemplateMenu();
            }

            GUILayout.FlexibleSpace();

            if (currentPreset != null)
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SavePreset();
                }

                if (GUILayout.Button("Apply to Scene", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    ApplyToSelectedObject();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPresetSelection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Current Preset", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            currentPreset = (ParticleEffectPreset)EditorGUILayout.ObjectField(
                "Preset", currentPreset, typeof(ParticleEffectPreset), false);

            if (EditorGUI.EndChangeCheck() && currentPreset != null)
            {
                ApplyPresetToPreview();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPresetInfo()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Preset Information", EditorStyles.boldLabel);

            currentPreset.presetName = EditorGUILayout.TextField("Name", currentPreset.presetName);
            currentPreset.description = EditorGUILayout.TextArea(currentPreset.description, GUILayout.Height(60));
            currentPreset.category = (EffectCategory)EditorGUILayout.EnumPopup("Category", currentPreset.category);
            currentPreset.previewIcon = (Sprite)EditorGUILayout.ObjectField("Icon", currentPreset.previewIcon, typeof(Sprite), false);

            EditorGUILayout.EndVertical();
        }

        private void DrawMainSettings()
        {
            showMainSettings = EditorGUILayout.Foldout(showMainSettings, "Main Settings", true, EditorStyles.foldoutHeader);

            if (showMainSettings)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();

                currentPreset.duration = EditorGUILayout.FloatField("Duration", currentPreset.duration);
                currentPreset.looping = EditorGUILayout.Toggle("Looping", currentPreset.looping);
                currentPreset.startLifetime = EditorGUILayout.Slider("Start Lifetime", currentPreset.startLifetime, 0.1f, 10f);
                currentPreset.startSpeed = EditorGUILayout.Slider("Start Speed", currentPreset.startSpeed, 0f, 20f);
                currentPreset.startSize = EditorGUILayout.Slider("Start Size", currentPreset.startSize, 0.1f, 5f);
                currentPreset.startColor = EditorGUILayout.ColorField("Start Color", currentPreset.startColor);
                currentPreset.gravityModifier = EditorGUILayout.Slider("Gravity", currentPreset.gravityModifier, -2f, 2f);
                currentPreset.maxParticles = EditorGUILayout.IntSlider("Max Particles", currentPreset.maxParticles, 10, 10000);

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyPresetToPreview();
                    EditorUtility.SetDirty(currentPreset);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawEmissionSettings()
        {
            showEmissionSettings = EditorGUILayout.Foldout(showEmissionSettings, "Emission", true, EditorStyles.foldoutHeader);

            if (showEmissionSettings)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();

                currentPreset.emissionRate = EditorGUILayout.Slider("Rate Over Time", currentPreset.emissionRate, 0f, 100f);

                EditorGUILayout.Space(5);
                currentPreset.useBurst = EditorGUILayout.Toggle("Use Burst", currentPreset.useBurst);

                if (currentPreset.useBurst)
                {
                    EditorGUI.indentLevel++;
                    currentPreset.burstCount = EditorGUILayout.IntSlider("Burst Count", currentPreset.burstCount, 1, 1000);
                    currentPreset.burstTime = EditorGUILayout.Slider("Burst Time", currentPreset.burstTime, 0f, 5f);
                    EditorGUI.indentLevel--;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyPresetToPreview();
                    EditorUtility.SetDirty(currentPreset);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawShapeSettings()
        {
            showShapeSettings = EditorGUILayout.Foldout(showShapeSettings, "Shape", true, EditorStyles.foldoutHeader);

            if (showShapeSettings)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();

                currentPreset.shapeType = (ParticleSystemShapeType)EditorGUILayout.EnumPopup("Shape Type", currentPreset.shapeType);
                currentPreset.shapeAngle = EditorGUILayout.Slider("Angle", currentPreset.shapeAngle, 0f, 90f);
                currentPreset.shapeRadius = EditorGUILayout.Slider("Radius", currentPreset.shapeRadius, 0.1f, 10f);

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyPresetToPreview();
                    EditorUtility.SetDirty(currentPreset);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawColorSettings()
        {
            showColorSettings = EditorGUILayout.Foldout(showColorSettings, "Color Over Lifetime", true, EditorStyles.foldoutHeader);

            if (showColorSettings)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();

                currentPreset.useColorOverLifetime = EditorGUILayout.Toggle("Enable", currentPreset.useColorOverLifetime);

                if (currentPreset.useColorOverLifetime)
                {
                    if (currentPreset.colorGradient == null)
                    {
                        currentPreset.colorGradient = new Gradient();
                    }
                    currentPreset.colorGradient = EditorGUILayout.GradientField("Color Gradient", currentPreset.colorGradient);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyPresetToPreview();
                    EditorUtility.SetDirty(currentPreset);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawSizeSettings()
        {
            showSizeSettings = EditorGUILayout.Foldout(showSizeSettings, "Size Over Lifetime", true, EditorStyles.foldoutHeader);

            if (showSizeSettings)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();

                currentPreset.useSizeOverLifetime = EditorGUILayout.Toggle("Enable", currentPreset.useSizeOverLifetime);

                if (currentPreset.useSizeOverLifetime)
                {
                    currentPreset.sizeOverLifetime = EditorGUILayout.CurveField("Size Curve", currentPreset.sizeOverLifetime);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyPresetToPreview();
                    EditorUtility.SetDirty(currentPreset);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawVelocitySettings()
        {
            showVelocitySettings = EditorGUILayout.Foldout(showVelocitySettings, "Velocity Over Lifetime", true, EditorStyles.foldoutHeader);

            if (showVelocitySettings)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();

                currentPreset.useVelocityOverLifetime = EditorGUILayout.Toggle("Enable", currentPreset.useVelocityOverLifetime);

                if (currentPreset.useVelocityOverLifetime)
                {
                    currentPreset.velocityOverLifetime = EditorGUILayout.Vector3Field("Velocity", currentPreset.velocityOverLifetime);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyPresetToPreview();
                    EditorUtility.SetDirty(currentPreset);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawRotationSettings()
        {
            showRotationSettings = EditorGUILayout.Foldout(showRotationSettings, "Rotation", true, EditorStyles.foldoutHeader);

            if (showRotationSettings)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();

                currentPreset.useRotation = EditorGUILayout.Toggle("Enable", currentPreset.useRotation);

                if (currentPreset.useRotation)
                {
                    currentPreset.rotationSpeed = EditorGUILayout.Slider("Rotation Speed", currentPreset.rotationSpeed, -360f, 360f);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyPresetToPreview();
                    EditorUtility.SetDirty(currentPreset);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawRenderSettings()
        {
            showRenderSettings = EditorGUILayout.Foldout(showRenderSettings, "Renderer", true, EditorStyles.foldoutHeader);

            if (showRenderSettings)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();

                currentPreset.renderMode = (ParticleSystemRenderMode)EditorGUILayout.EnumPopup("Render Mode", currentPreset.renderMode);
                currentPreset.particleMaterial = (Material)EditorGUILayout.ObjectField("Material", currentPreset.particleMaterial, typeof(Material), false);

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyPresetToPreview();
                    EditorUtility.SetDirty(currentPreset);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawTrailSettings()
        {
            showTrailSettings = EditorGUILayout.Foldout(showTrailSettings, "Trails", true, EditorStyles.foldoutHeader);

            if (showTrailSettings)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();

                currentPreset.useTrails = EditorGUILayout.Toggle("Enable", currentPreset.useTrails);

                if (currentPreset.useTrails)
                {
                    currentPreset.trailLifetime = EditorGUILayout.Slider("Lifetime", currentPreset.trailLifetime, 0.1f, 5f);
                    currentPreset.trailMinVertexDistance = EditorGUILayout.Slider("Min Vertex Distance", currentPreset.trailMinVertexDistance, 0.01f, 1f);
                    currentPreset.trailMaterial = (Material)EditorGUILayout.ObjectField("Trail Material", currentPreset.trailMaterial, typeof(Material), false);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyPresetToPreview();
                    EditorUtility.SetDirty(currentPreset);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawPreviewControls()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Preview Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Play", GUILayout.Height(30)))
            {
                PlayPreview();
            }

            if (GUILayout.Button("Stop", GUILayout.Height(30)))
            {
                StopPreview();
            }

            if (GUILayout.Button("Restart", GUILayout.Height(30)))
            {
                RestartPreview();
            }

            EditorGUILayout.EndHorizontal();

            autoPlay = EditorGUILayout.Toggle("Auto Play on Change", autoPlay);

            if (previewParticleSystem != null)
            {
                EditorGUILayout.LabelField($"Particles: {previewParticleSystem.particleCount}");
                EditorGUILayout.LabelField($"Is Playing: {previewParticleSystem.isPlaying}");
            }

            EditorGUILayout.EndVertical();
        }

        private void CreatePreviewObject()
        {
            previewObject = new GameObject("Particle Preview");
            previewObject.hideFlags = HideFlags.HideAndDontSave;
            previewParticleSystem = previewObject.AddComponent<ParticleSystem>();

            // Position preview object in scene
            if (SceneView.lastActiveSceneView != null)
            {
                previewObject.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 5f;
            }
        }

        private void ApplyPresetToPreview()
        {
            if (currentPreset != null && previewParticleSystem != null)
            {
                currentPreset.ApplyToParticleSystem(previewParticleSystem);

                if (autoPlay)
                {
                    previewParticleSystem.Play();
                }
            }
        }

        private void PlayPreview()
        {
            if (previewParticleSystem != null)
            {
                previewParticleSystem.Play();
            }
        }

        private void StopPreview()
        {
            if (previewParticleSystem != null)
            {
                previewParticleSystem.Stop();
            }
        }

        private void RestartPreview()
        {
            if (previewParticleSystem != null)
            {
                previewParticleSystem.Clear();
                previewParticleSystem.Play();
            }
        }

        private void CreateNewPreset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Particle Effect Preset",
                "New Particle Preset",
                "asset",
                "Create a new particle effect preset");

            if (!string.IsNullOrEmpty(path))
            {
                ParticleEffectPreset newPreset = ScriptableObject.CreateInstance<ParticleEffectPreset>();
                AssetDatabase.CreateAsset(newPreset, path);
                AssetDatabase.SaveAssets();

                currentPreset = newPreset;
                Selection.activeObject = newPreset;

                ApplyPresetToPreview();
            }
        }

        private void SavePreset()
        {
            if (currentPreset != null)
            {
                EditorUtility.SetDirty(currentPreset);
                AssetDatabase.SaveAssets();
                Debug.Log($"Preset '{currentPreset.presetName}' saved successfully!");
            }
        }

        private void ApplyToSelectedObject()
        {
            if (Selection.activeGameObject != null)
            {
                ParticleSystem ps = Selection.activeGameObject.GetComponent<ParticleSystem>();
                if (ps == null)
                {
                    ps = Selection.activeGameObject.AddComponent<ParticleSystem>();
                }

                currentPreset.ApplyToParticleSystem(ps);
                Debug.Log($"Applied preset to {Selection.activeGameObject.name}");
            }
            else
            {
                // Create new GameObject with particle system
                GameObject go = currentPreset.CreateParticleEffect(Vector3.zero, Quaternion.identity);
                Selection.activeGameObject = go;
                Debug.Log($"Created new particle effect: {go.name}");
            }
        }

        private void ShowTemplateMenu()
        {
            GenericMenu menu = new GenericMenu();

            foreach (string templateName in templateNames)
            {
                menu.AddItem(new GUIContent(templateName), false, () => LoadTemplate(templateName));
            }

            menu.ShowAsContext();
        }

        private void LoadTemplate(string templateName)
        {
            if (currentPreset == null)
            {
                EditorUtility.DisplayDialog("No Preset", "Please create or select a preset first", "OK");
                return;
            }

            ApplyTemplate(currentPreset, templateName);
            ApplyPresetToPreview();
            EditorUtility.SetDirty(currentPreset);

            Debug.Log($"Applied template: {templateName}");
        }

        public static void ApplyTemplate(ParticleEffectPreset preset, string templateName)
        {
            preset.presetName = templateName;

            switch (templateName)
            {
                case "Explosion":
                    // Explosion template
                    preset.category = EffectCategory.Explosion;
                    preset.duration = 2f;
                    preset.looping = false;
                    preset.startLifetime = 1f;
                    preset.startSpeed = 10f;
                    preset.startSize = 0.5f;
                    preset.startColor = new Color(1f, 0.5f, 0f, 1f);
                    preset.gravityModifier = 0.2f;
                    preset.maxParticles = 100;
                    preset.emissionRate = 0f;
                    preset.useBurst = true;
                    preset.burstCount = 100;
                    preset.burstTime = 0f;
                    preset.shapeType = ParticleSystemShapeType.Sphere;
                    preset.shapeRadius = 0.1f;
                    preset.useColorOverLifetime = true;
                    preset.colorGradient = CreateExplosionGradient();
                    preset.useSizeOverLifetime = true;
                    preset.sizeOverLifetime = AnimationCurve.EaseInOut(0, 1, 1, 0);
                    break;

                case "Fire":
                    // Fire template
                    preset.category = EffectCategory.Fire;
                    preset.duration = 5f;
                    preset.looping = true;
                    preset.startLifetime = 1.5f;
                    preset.startSpeed = 2f;
                    preset.startSize = 0.5f;
                    preset.startColor = new Color(1f, 0.8f, 0f, 1f);
                    preset.gravityModifier = -0.5f;
                    preset.maxParticles = 50;
                    preset.emissionRate = 30f;
                    preset.useBurst = false;
                    preset.shapeType = ParticleSystemShapeType.Cone;
                    preset.shapeAngle = 15f;
                    preset.shapeRadius = 0.3f;
                    preset.useColorOverLifetime = true;
                    preset.colorGradient = CreateFireGradient();
                    preset.useSizeOverLifetime = true;
                    preset.sizeOverLifetime = AnimationCurve.Linear(0, 0.5f, 1, 1.5f);
                    break;

                case "Smoke":
                    // Smoke template
                    preset.category = EffectCategory.Smoke;
                    preset.duration = 5f;
                    preset.looping = true;
                    preset.startLifetime = 3f;
                    preset.startSpeed = 1f;
                    preset.startSize = 1f;
                    preset.startColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                    preset.gravityModifier = -0.2f;
                    preset.maxParticles = 30;
                    preset.emissionRate = 10f;
                    preset.useBurst = false;
                    preset.shapeType = ParticleSystemShapeType.Cone;
                    preset.shapeAngle = 20f;
                    preset.shapeRadius = 0.5f;
                    preset.useColorOverLifetime = true;
                    preset.colorGradient = CreateSmokeGradient();
                    preset.useSizeOverLifetime = true;
                    preset.sizeOverLifetime = AnimationCurve.EaseInOut(0, 0.5f, 1, 2f);
                    preset.useRotation = true;
                    preset.rotationSpeed = 45f;
                    break;

                case "Magic Sparkles":
                    // Magic sparkles template
                    preset.category = EffectCategory.Magic;
                    preset.duration = 2f;
                    preset.looping = true;
                    preset.startLifetime = 1f;
                    preset.startSpeed = 3f;
                    preset.startSize = 0.1f;
                    preset.startColor = new Color(0.5f, 0.5f, 1f, 1f);
                    preset.gravityModifier = -0.3f;
                    preset.maxParticles = 100;
                    preset.emissionRate = 50f;
                    preset.useBurst = false;
                    preset.shapeType = ParticleSystemShapeType.Sphere;
                    preset.shapeRadius = 0.5f;
                    preset.useColorOverLifetime = true;
                    preset.colorGradient = CreateMagicGradient();
                    preset.useSizeOverLifetime = true;
                    preset.sizeOverLifetime = AnimationCurve.EaseInOut(0, 0.2f, 1, 0);
                    preset.useRotation = true;
                    preset.rotationSpeed = 180f;
                    break;

                case "Electric":
                    // Electric template
                    preset.category = EffectCategory.Electric;
                    preset.duration = 1f;
                    preset.looping = true;
                    preset.startLifetime = 0.2f;
                    preset.startSpeed = 0f;
                    preset.startSize = 0.5f;
                    preset.startColor = new Color(0.5f, 0.8f, 1f, 1f);
                    preset.gravityModifier = 0f;
                    preset.maxParticles = 50;
                    preset.emissionRate = 100f;
                    preset.useBurst = false;
                    preset.shapeType = ParticleSystemShapeType.Sphere;
                    preset.shapeRadius = 1f;
                    preset.useColorOverLifetime = true;
                    preset.colorGradient = CreateElectricGradient();
                    preset.useSizeOverLifetime = true;
                    preset.sizeOverLifetime = AnimationCurve.Linear(0, 1, 1, 0);
                    break;

                case "Water Splash":
                    // Water splash template
                    preset.category = EffectCategory.Water;
                    preset.duration = 1f;
                    preset.looping = false;
                    preset.startLifetime = 0.8f;
                    preset.startSpeed = 8f;
                    preset.startSize = 0.2f;
                    preset.startColor = new Color(0.3f, 0.6f, 1f, 0.8f);
                    preset.gravityModifier = 1.5f;
                    preset.maxParticles = 50;
                    preset.emissionRate = 0f;
                    preset.useBurst = true;
                    preset.burstCount = 50;
                    preset.burstTime = 0f;
                    preset.shapeType = ParticleSystemShapeType.Cone;
                    preset.shapeAngle = 45f;
                    preset.shapeRadius = 0.1f;
                    preset.useColorOverLifetime = true;
                    preset.colorGradient = CreateWaterGradient();
                    preset.useSizeOverLifetime = true;
                    preset.sizeOverLifetime = AnimationCurve.Linear(0, 0.5f, 1, 0.1f);
                    break;

                case "Heal Effect":
                    // Heal effect template
                    preset.category = EffectCategory.Magic;
                    preset.duration = 2f;
                    preset.looping = false;
                    preset.startLifetime = 1.5f;
                    preset.startSpeed = 2f;
                    preset.startSize = 0.3f;
                    preset.startColor = new Color(0.5f, 1f, 0.5f, 1f);
                    preset.gravityModifier = -0.5f;
                    preset.maxParticles = 50;
                    preset.emissionRate = 30f;
                    preset.useBurst = false;
                    preset.shapeType = ParticleSystemShapeType.Cone;
                    preset.shapeAngle = 10f;
                    preset.shapeRadius = 0.5f;
                    preset.useColorOverLifetime = true;
                    preset.colorGradient = CreateHealGradient();
                    preset.useSizeOverLifetime = true;
                    preset.sizeOverLifetime = AnimationCurve.EaseInOut(0, 0.2f, 1, 0.5f);
                    break;
            }
        }

        // Gradient creation helpers
        private static Gradient CreateExplosionGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 0.5f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(0.3f, 0.1f, 0f), 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static Gradient CreateFireGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 0f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(0.5f, 0f, 0f), 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static Gradient CreateSmokeGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(0.5f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static Gradient CreateMagicGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 1f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.5f, 1f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(0.5f, 0f, 1f), 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static Gradient CreateElectricGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.3f, 0.5f, 1f), 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static Gradient CreateWaterGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(new Color(0.5f, 0.8f, 1f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.2f, 0.4f, 0.8f), 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(0.8f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static Gradient CreateHealGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 0.8f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.5f, 1f, 0.5f), 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }
    }
}
