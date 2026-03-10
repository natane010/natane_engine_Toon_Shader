using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using NataneToon.MaterialSystem;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// 鬩搾ｽｨ繝ｻ・ｱ髯ｷ・ｷ陋ｹ・ｻ郢晢ｽｻ驛｢譏ｴ繝ｻ・取㏍・ｹ・ｧ繝ｻ・｢驛｢譎｢・ｽ・ｫ驛｢・ｧ繝ｻ・ｨ驛｢譏ｴ繝ｻ邵ｺ繝ｻ・ｹ・ｧ繝ｻ・ｿ - BatchMaterialProcessor驍ｵ・ｺ繝ｻ・ｨSceneMaterialEditor驍ｵ・ｺ繝ｻ・ｮ髫ｶ蛹・ｽｺ・ｯ郢晢ｽｻ驛｢・ｧ陜｣・､繝ｻ・ｵ繝ｻ・ｱ髯ｷ・ｷ郢晢ｽｻ
    /// Unified Material Editor - Integrates BatchMaterialProcessor and SceneMaterialEditor functionality
    ///
    /// 2驍ｵ・ｺ繝ｻ・､驍ｵ・ｺ繝ｻ・ｮ髯ｷ繝ｻ・ｽ・ｦ鬨ｾ繝ｻ繝ｻ・守坩・ｹ譎｢・ｽ・ｼ驛｢譎擾ｽｳ・ｨ繝ｻ螳夲ｽｬ・ｰ髯應ｼ夲ｽｽ・ｾ郢晢ｽｻ
    /// - Batch Mode: 鬮ｫ髦ｪ繝ｻ霎溷､ゑｽｹ譎・ｽｧ・ｭ郢晢ｽｦ驛｢譎｢・ｽ・ｪ驛｢・ｧ繝ｻ・｢驛｢譎｢・ｽ・ｫ驍ｵ・ｺ繝ｻ・ｮ髣包ｽｳ・つ髫ｲ・｡繝ｻ・ｬ髯ｷ繝ｻ・ｽ・ｦ鬨ｾ繝ｻ繝ｻ
    /// - Scene Mode: 驛｢・ｧ繝ｻ・ｷ驛｢譎｢・ｽ・ｼ驛｢譎｢・ｽ・ｳ髣包ｽｳ驗呻ｽｫ郢晢ｽｻ驛｢・ｧ繝ｻ・ｪ驛｢譎・§邵ｺ螟ゑｽｹ・ｧ繝ｻ・ｧ驛｢・ｧ繝ｻ・ｯ驛｢譎冗樟郢晢ｽｻ驛｢譎｢・ｽ・ｪ驛｢・ｧ繝ｻ・｢驛｢譎｢・ｽ・ｫ驛｢・ｧ繝ｻ・ｿ驛｢・ｧ繝ｻ・､驛｢譎｢・｣・ｰ鬩搾ｽｱ繝ｻ・ｨ鬯ｮ・ｮ郢晢ｽｻ
    /// </summary>
    public class UnifiedMaterialEditor : EditorWindow
    {
        // ========== Mode Selection ==========
        private enum EditorMode { Batch, Scene }
        private EditorMode currentMode = EditorMode.Batch;

        // ========== Common State ==========
        private Vector2 windowScrollPosition;
        private List<Material> selectedMaterials = new List<Material>();
        private Material currentMaterial;

        // ========== Batch Mode State ==========
        private Vector2 materialListScroll;
        private int selectedTab = 0;

        private string[] GetTabs() => new[] { L("Parameter", "Parameter"), L("Color", "Color"), L("Texture", "Texture"), L("Feature", "Feature"), L("Variant", "Variant") };

        // Parameter adjustment (Batch Mode)
        private enum AdjustMode { Set, Add, Multiply }
        private string[] floatParameters = new[] { "_ShadowSteps", "_ShadowSharpness", "_ShadowReceive", "_OutlineWidth",
                                                   "_SpecularBlend", "_RimIntensity", "_SSSIntensity",
                                                   "_EmissionGlow", "_ReflectionIntensity", "_Metallic", "_Smoothness" };
        private int selectedParameter = 0;
        private AdjustMode adjustMode = AdjustMode.Set;
        private float adjustValue = 1.0f;

        // Color adjustment (Batch Mode)
        private enum ColorParameter { MainColor, ShadowColor, RimColor, EmissionColor, OutlineColor, SpecularColor }
        private ColorParameter selectedColorParam = ColorParameter.MainColor;
        private Color targetColor = Color.white;
        private bool adjustHue = false;
        private bool adjustSaturation = false;
        private bool adjustBrightness = false;
        private float hueShift = 0f;
        private float saturationMultiplier = 1f;
        private float valueMultiplier = 1f;

        // Texture replacement (Batch Mode)
        private string textureProperty = "_MainTex";
        private Texture2D replacementTexture;

        // Feature toggle (Batch Mode)
        private string[] features = new[] { "_SPECULAR", "_RIM_LIGHT", "_SSS", "_MATCAP", "_OUTLINE", "_EMISSION",
                                           "_NORMALMAP", "_REFLECTION", "_ENV_RIM", "_PARALLAX", "_REFRACTION",
                                           "_DETAIL_MAP", "_TRIPLANAR", "_HEIGHT_FOG",
                                           "_SURFACE_COVER", "_MIRROR_CONTROL", "_QUEST_LITE",
                                           "_WATER_DRIP", "_VIDEO_TEXTURE", "_INTERSECTION_FADE" };
        private bool[] featureStates;

        // Variant conversion (Batch Mode)
        private enum ShaderVariant { Opaque, Cutout, Transparent }
        private ShaderVariant targetVariant = ShaderVariant.Opaque;

        // ========== Scene Mode State ==========
        private GameObject selectedObject;
        private int selectedMaterialIndex = 0;

        // UI髫ｰ螢ｼﾂ・･繝ｻ鬘費ｽｸ・ｺ雋・ｪ陞ｺ驍ｵ・ｺ繝ｻ・ｿ髴托ｽ･繝ｻ・ｶ髫ｲ・ｷ郢晢ｽｻ(Scene Mode)
        private bool showBasicSettings = true;
        private bool showShadingSettings = true;
        private bool showSpecularSettings = false;
        private bool showRimLightSettings = false;
        private bool showOutlineSettings = false;
        private bool showEmissionSettings = false;
        private bool showAdvancedSettings = false;

        // ========== Common Settings ==========
        private bool filterNataneToonOnly = true;
        private string searchFilter = "";
        private const float CompactLayoutWidth = 720f;
        private const float NarrowLayoutWidth = 600f;

        [MenuItem("Tools/Natane/Material/Unified Material Editor", false, 12)]
        public static void ShowWindow()
        {
            var window = GetWindow<UnifiedMaterialEditor>(L("Material Editor", "Material Editor"));
            window.minSize = new Vector2(650, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // Scene Mode鬨ｾ蛹・ｽｽ・ｨ驍ｵ・ｺ繝ｻ・ｮ驛｢・ｧ繝ｻ・､驛｢譎冗函・趣ｽｦ驛｢譎√＃陋ｹ・ｳ鬯ｪ・ｭ繝ｻ・ｲ
            featureStates = new bool[features.Length];

            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            // Scene Mode鬨ｾ蛹・ｽｽ・ｨ驍ｵ・ｺ繝ｻ・ｮ驛｢・ｧ繝ｻ・､驛｢譎冗函・趣ｽｦ驛｢譎槭Γ繝ｻ・ｧ繝ｻ・｣鬯ｮ・ｯ繝ｻ・､
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (currentMode == EditorMode.Scene && Selection.activeGameObject != null)
            {
                selectedObject = Selection.activeGameObject;
                RefreshSceneMaterials();
            }
        }

        private void OnGUI()
        {
            windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition);
            DrawHeader();
            EditorGUILayout.Space(5);

            DrawModeSelector();
            EditorGUILayout.Space(5);

            if (currentMode == EditorMode.Batch)
            {
                DrawBatchMode();
            }
            else
            {
                DrawSceneMode();
            }

            EditorGUILayout.EndScrollView();
        }

        // ========== Header & Mode Selection ==========

        private void DrawHeader()
        {
            NataneToonShaderGUIUtility.DrawToolHeader("Unified Material Editor", "Unified Material Editor", "UnifiedMaterialEditor");
            EditorGUILayout.HelpBox(
                L("Batch Mode: Batch processing | Scene Mode: Realtime editing", "Batch Mode: Batch processing | Scene Mode: Realtime editing"),
                MessageType.Info);
        }

        private void DrawModeSelector()
        {
            bool compactLayout = IsCompactLayout();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("Mode", "Mode"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            if (compactLayout)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(currentMode == EditorMode.Batch, L("Batch Mode", "Batch Mode"), EditorStyles.miniButtonLeft))
                {
                    currentMode = EditorMode.Batch;
                }
                if (GUILayout.Toggle(currentMode == EditorMode.Scene, L("Scene Mode", "Scene Mode"), EditorStyles.miniButtonRight))
                {
                    currentMode = EditorMode.Scene;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(currentMode == EditorMode.Batch, L("Batch Mode", "Batch Mode"), EditorStyles.miniButtonLeft))
                {
                    currentMode = EditorMode.Batch;
                }
                if (GUILayout.Toggle(currentMode == EditorMode.Scene, L("Scene Mode", "Scene Mode"), EditorStyles.miniButtonRight))
                {
                    currentMode = EditorMode.Scene;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
            {
                OnModeChanged();
            }

            EditorGUILayout.EndVertical();
        }

        private void OnModeChanged()
        {
            // 驛｢譎｢・ｽ・｢驛｢譎｢・ｽ・ｼ驛｢譎臥櫨郢晢ｽｻ驛｢・ｧ鬯・､ｧ・ｴ蟶ｷ・ｸ・ｺ陜捺ｺｷ繝ｻ驍ｵ・ｺ繝ｻ・ｮ髯具ｽｻ隴弱・・・刹・ｹ鬮｢ﾂ郢晢ｽｻ鬨ｾ繝ｻ繝ｻ
            if (currentMode == EditorMode.Scene)
            {
                OnSelectionChanged();
            }
            else
            {
                // Batch Mode驍ｵ・ｺ繝ｻ・ｫ髯具ｽｻ郢晢ｽｻ繝ｻ鬘假ｽｭ蜴・ｽｽ・ｿ驍ｵ・ｺ陜捺ｺｷ繝ｻ驍ｵ・ｲ郢晢ｽｾcene Mode驍ｵ・ｺ繝ｻ・ｧ鬩搾ｽｱ繝ｻ・ｨ鬯ｮ・ｮ郢晢ｽｻ繝ｻ・ｰ驍ｵ・ｺ繝ｻ・ｦ驍ｵ・ｺ郢晢ｽｻ隨ｳ繝ｻ・ｹ譎・ｽｧ・ｭ郢晢ｽｦ驛｢譎｢・ｽ・ｪ驛｢・ｧ繝ｻ・｢驛｢譎｢・ｽ・ｫ驛｢・ｧ陋幢ｽｵ邵ｺ驢搾ｽｹ譎｢・ｽ・ｪ驛｢・ｧ繝ｻ・｢
                currentMaterial = null;
            }
        }

        // ========== Batch Mode UI ==========

        private void DrawBatchMode()
        {
            DrawBatchMaterialSelection();
            EditorGUILayout.Space(5);

            if (selectedMaterials.Count > 0)
            {
                DrawTabs();
                EditorGUILayout.Space(5);
                DrawBatchTabContent();
            }
            else
            {
                EditorGUILayout.HelpBox(L("Select materials to begin batch processing", "Select materials to begin batch processing"), MessageType.Info);
            }
        }

        private void DrawBatchMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("Material Selection", "Material Selection"), EditorStyles.boldLabel);
            DrawBatchSelectionButtons();

            EditorGUILayout.Space(5);

            if (selectedMaterials.Count > 0)
            {
                EditorGUILayout.LabelField(L($"Selected: {selectedMaterials.Count} materials", $"Selected: {selectedMaterials.Count} materials"), EditorStyles.boldLabel);

                materialListScroll = EditorGUILayout.BeginScrollView(materialListScroll, GUILayout.Height(GetAdaptiveListHeight(100f, 220f, 0.2f)));
                for (int i = selectedMaterials.Count - 1; i >= 0; i--)
                {
                    if (selectedMaterials[i] == null)
                    {
                        selectedMaterials.RemoveAt(i);
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(selectedMaterials[i], typeof(Material), false);
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        selectedMaterials.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTabs()
        {
            string[] tabs = GetTabs();
            if (IsCompactLayout())
            {
                int columns = position.width < NarrowLayoutWidth ? 2 : 3;
                selectedTab = GUILayout.SelectionGrid(selectedTab, tabs, columns, EditorStyles.miniButton);
            }
            else
            {
                selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(25));
            }
        }

        private void DrawBatchTabContent()
        {
            switch (selectedTab)
            {
                case 0: DrawParameterAdjust(); break;
                case 1: DrawColorAdjust(); break;
                case 2: DrawTextureReplace(); break;
                case 3: DrawFeatureToggle(); break;
                case 4: DrawVariantConvert(); break;
            }
        }

        // ========== Scene Mode UI ==========

        private void DrawSceneMode()
        {
            DrawSceneToolbar();
            EditorGUILayout.Space(10);

            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox(L("Please select an object in the scene.", "Please select an object in the scene."), MessageType.Info);
                return;
            }

            if (selectedMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox(L("No materials found on selected object.", "No materials found on selected object."), MessageType.Warning);
                return;
            }

            DrawObjectInfo();
            EditorGUILayout.Space(10);

            DrawSceneMaterialSelector();
            EditorGUILayout.Space(10);

            if (currentMaterial != null)
            {
                DrawSceneMaterialEditor();
            }
        }

        private void DrawSceneToolbar()
        {
            bool compactLayout = IsCompactLayout();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(L("Refresh", "Refresh"), EditorStyles.toolbarButton))
            {
                RefreshSceneMaterials();
            }

            if (!compactLayout)
            {
                GUILayout.FlexibleSpace();
            }

            EditorGUI.BeginChangeCheck();
            filterNataneToonOnly = GUILayout.Toggle(filterNataneToonOnly, L("Natane Only", "Natane Only"), EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshSceneMaterials();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L("Search", "Search") + ":", GUILayout.Width(compactLayout ? 55f : 80f));
            EditorGUI.BeginChangeCheck();
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button(L("Clear", "Clear"), GUILayout.Width(60f)))
            {
                searchFilter = "";
                RefreshSceneMaterials();
                GUI.FocusControl(null);
            }
            if (EditorGUI.EndChangeCheck())
            {
                RefreshSceneMaterials();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawObjectInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("Selected Object", "Selected Object"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(L("Name:", "Name:"), selectedObject.name);
            EditorGUILayout.LabelField(L("Material Count:", "Material Count:"), selectedMaterials.Count.ToString());
            EditorGUILayout.EndVertical();
        }

        private void DrawSceneMaterialSelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("Material Selection", "Material Selection"), EditorStyles.boldLabel);

            if (selectedMaterials.Count > 1)
            {
                string[] materialNames = selectedMaterials.Select((m, i) => $"{i}: {(m != null ? m.name : "null")}").ToArray();
                EditorGUI.BeginChangeCheck();
                selectedMaterialIndex = EditorGUILayout.Popup(L("Material:", "Material:"), selectedMaterialIndex, materialNames);
                if (EditorGUI.EndChangeCheck())
                {
                    currentMaterial = selectedMaterials[selectedMaterialIndex];
                }
            }
            else
            {
                EditorGUILayout.LabelField(L("Material:", "Material:"), currentMaterial != null ? currentMaterial.name : "null");
            }

            if (currentMaterial != null)
            {
                EditorGUILayout.LabelField(L("Shader:", "Shader:"), currentMaterial.shader.name);

                if (!IsNataneToonShader(currentMaterial))
                {
                    EditorGUILayout.HelpBox(L("This material is not using Natane Toon Shader.", "This material is not using Natane Toon Shader."), MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneMaterialEditor()
        {
            // 髯憺屮・ｽ・ｺ髫ｴ蟷｢・ｽ・ｬ鬮ｫ・ｪ繝ｻ・ｭ髯橸ｽｳ郢晢ｽｻ
            showBasicSettings = DrawFoldoutSection(L("Basic Settings", "Basic Settings"), showBasicSettings, () =>
            {
                DrawColorProperty("_Color", L("Main Color", "Main Color"));
                DrawFloatProperty("_Alpha", L("Alpha", "Alpha"), 0f, 1f);
            });

            // 驛｢・ｧ繝ｻ・ｷ驛｢・ｧ繝ｻ・ｧ驛｢譎｢・ｽ・ｼ驛｢譏ｴ繝ｻ邵ｺ繝ｻ・ｹ譎｢・ｽ・ｳ驛｢・ｧ繝ｻ・ｰ鬮ｫ・ｪ繝ｻ・ｭ髯橸ｽｳ郢晢ｽｻ
            showShadingSettings = DrawFoldoutSection(L("Shading", "Shading"), showShadingSettings, () =>
            {
                DrawColorProperty("_ShadowColor", L("Shadow Color", "Shadow Color"));
                DrawIntProperty("_ShadowSteps", L("Toon Steps", "Toon Steps"), 1, 10);
                DrawFloatProperty("_ShadowSharpness", L("Sharpness", "Sharpness"), 0f, 1f);
                DrawFloatProperty("_ShadowReceive", L("Shadow Receive", "Shadow Receive"), 0f, 1f);
            });

            // 驛｢・ｧ繝ｻ・ｹ驛｢譎擾ｽ｣・ｹ邵ｺ蜀暦ｽｹ譎｢・ｽ・･驛｢譎｢・ｽ・ｩ驛｢譎｢・ｽ・ｼ鬮ｫ・ｪ繝ｻ・ｭ髯橸ｽｳ郢晢ｽｻ
            showSpecularSettings = DrawFoldoutSection(L("Specular", "Specular"), showSpecularSettings, () =>
            {
                if (currentMaterial.HasProperty("_Specular"))
                {
                    DrawToggleProperty("_Specular", L("Use Specular", "Use Specular"), "_SPECULAR");
                    if (currentMaterial.GetFloat("_Specular") > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        DrawColorProperty("_SpecularColor", L("Color", "Color"));
                        DrawFloatProperty("_SpecularBlend", L("Intensity", "Intensity"), 0f, 1f);
                        DrawFloatProperty("_SpecularSize", L("Size", "Size"), 0f, 1f);
                        DrawFloatProperty("_SpecularSoftness", L("Sharpness", "Sharpness"), 0f, 1f);
                        EditorGUI.indentLevel--;
                    }
                }
            });

            // 驛｢譎｢・ｽ・ｪ驛｢譎｢・｣・ｰ驛｢譎｢・ｽ・ｩ驛｢・ｧ繝ｻ・､驛｢譎槭Γ繝ｻ・ｨ繝ｻ・ｭ髯橸ｽｳ郢晢ｽｻ
            showRimLightSettings = DrawFoldoutSection(L("Rim Light", "Rim Light"), showRimLightSettings, () =>
            {
                if (currentMaterial.HasProperty("_RimLight"))
                {
                    DrawToggleProperty("_RimLight", L("Use Rim Light", "Use Rim Light"), "_RIM_LIGHT");
                    if (currentMaterial.GetFloat("_RimLight") > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        DrawColorProperty("_RimColor", L("Color", "Color"));
                        DrawFloatProperty("_RimIntensity", L("Intensity", "Intensity"), 0f, 2f);
                        DrawFloatProperty("_RimPower", L("Power", "Power"), 0.1f, 10f);
                        EditorGUI.indentLevel--;
                    }
                }
            });

            // 驛｢・ｧ繝ｻ・｢驛｢・ｧ繝ｻ・ｦ驛｢譎冗樟・主ｸｷ・ｹ・ｧ繝ｻ・､驛｢譎｢・ｽ・ｳ鬮ｫ・ｪ繝ｻ・ｭ髯橸ｽｳ郢晢ｽｻ
            showOutlineSettings = DrawFoldoutSection(L("Outline", "Outline"), showOutlineSettings, () =>
            {
                if (currentMaterial.HasProperty("_Outline"))
                {
                    DrawToggleProperty("_Outline", L("Use Outline", "Use Outline"), "_OUTLINE");
                    if (currentMaterial.GetFloat("_Outline") > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        DrawColorProperty("_OutlineColor", L("Color", "Color"));
                        DrawFloatProperty("_OutlineWidth", L("Width", "Width"), 0f, 0.1f);
                        EditorGUI.indentLevel--;
                    }
                }
            });

            // 驛｢・ｧ繝ｻ・ｨ驛｢譎・ｽｺ蛟･ﾎ暮Δ・ｧ繝ｻ・ｷ驛｢譎｢・ｽ・ｧ驛｢譎｢・ｽ・ｳ鬮ｫ・ｪ繝ｻ・ｭ髯橸ｽｳ郢晢ｽｻ
            showEmissionSettings = DrawFoldoutSection(L("Emission", "Emission"), showEmissionSettings, () =>
            {
                if (currentMaterial.HasProperty("_Emission"))
                {
                    DrawToggleProperty("_Emission", L("Use Emission", "Use Emission"), "_EMISSION");
                    if (currentMaterial.GetFloat("_Emission") > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        DrawColorProperty("_EmissionColor", L("Color", "Color"));
                        DrawFloatProperty("_EmissionGlow", L("Intensity", "Intensity"), 0f, 1f);
                        EditorGUI.indentLevel--;
                    }
                }
            });

            // 鬯ｯ・ｮ闔ｨ諛ｶ・ｽ・ｺ繝ｻ・ｦ驍ｵ・ｺ繝ｻ・ｪ鬮ｫ・ｪ繝ｻ・ｭ髯橸ｽｳ郢晢ｽｻ
            showAdvancedSettings = DrawFoldoutSection(L("Advanced", "Advanced"), showAdvancedSettings, () =>
            {
                DrawFloatProperty("_Metallic", L("Metallic", "Metallic"), 0f, 1f);
                DrawFloatProperty("_Smoothness", L("Smoothness", "Smoothness"), 0f, 1f);
                if (currentMaterial.HasProperty("_Cull"))
                {
                    DrawIntProperty("_Cull", L("Cull Mode", "Cull Mode"), 0, 2);
                }
            });

            EditorGUILayout.Space(10);

            // 髫ｰ・ｫ陜｣・ｺ繝ｻ・ｽ隲帷ｿｫ繝ｻ驛｢・ｧ繝ｻ・ｿ驛｢譎｢・ｽ・ｳ
            if (IsCompactLayout())
            {
                if (GUILayout.Button(L("Apply Preset", "Apply Preset")))
                {
                    ShowPresetMenu();
                }
                if (GUILayout.Button(L("Reset", "Reset")))
                {
                    if (EditorUtility.DisplayDialog(L("Confirm", "Confirm"),
                        L("Reset material to default values?", "Reset material to default values?"),
                        L("Yes", "Yes"), L("No", "No")))
                    {
                        ResetMaterialToDefault();
                    }
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(L("Apply Preset", "Apply Preset")))
                {
                    ShowPresetMenu();
                }
                if (GUILayout.Button(L("Reset", "Reset")))
                {
                    if (EditorUtility.DisplayDialog(L("Confirm", "Confirm"),
                        L("Reset material to default values?", "Reset material to default values?"),
                        L("Yes", "Yes"), L("No", "No")))
                    {
                        ResetMaterialToDefault();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // ========== Batch Mode: Parameter Adjustment ==========

        private void DrawParameterAdjust()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("Batch Parameter Adjustment", "Batch Parameter Adjustment"), EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                L("Set = Replace value, Add = Add to current, Multiply = Multiply current", "Set = Replace value, Add = Add to current, Multiply = Multiply current"),
                MessageType.Info);

            EditorGUILayout.Space(5);

            selectedParameter = EditorGUILayout.Popup(L("Parameter", "Parameter"), selectedParameter, floatParameters);
            adjustMode = (AdjustMode)EditorGUILayout.EnumPopup(L("Mode", "Mode"), adjustMode);

            string label = adjustMode == AdjustMode.Set ? L("New Value", "New Value") :
                          adjustMode == AdjustMode.Add ? L("Add Amount", "Add Amount") : L("Multiply By", "Multiply By");
            adjustValue = EditorGUILayout.FloatField(label, adjustValue);

            EditorGUILayout.Space(10);

            // Preview
            if (selectedMaterials.Count > 0 && selectedMaterials[0] != null)
            {
                string paramName = floatParameters[selectedParameter];
                if (selectedMaterials[0].HasProperty(paramName))
                {
                    float currentValue = selectedMaterials[0].GetFloat(paramName);
                    float newValue = CalculateNewValue(currentValue, adjustValue, adjustMode);
                    EditorGUILayout.LabelField(L($"Example: {currentValue:F3} -> {newValue:F3}", $"Example: {currentValue:F3} -> {newValue:F3}"));
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button(L("Apply to All Selected Materials", "Apply to All Selected Materials"), GUILayout.Height(30)))
            {
                ApplyParameterAdjustment();
            }

            EditorGUILayout.EndVertical();
        }

        // ========== Batch Mode: Color Adjustment ==========

        private void DrawColorAdjust()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("Batch Color Adjustment", "Batch Color Adjustment"), EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                L("Can set absolute color or adjust HSV values relatively.", "Can set absolute color or adjust HSV values relatively."),
                MessageType.Info);

            EditorGUILayout.Space(5);

            selectedColorParam = (ColorParameter)EditorGUILayout.EnumPopup(L("Color Parameter", "Color Parameter"), selectedColorParam);

            EditorGUILayout.Space(10);

            // Absolute color setting
            EditorGUILayout.LabelField(L("Absolute Color Setting", "Absolute Color Setting"), EditorStyles.boldLabel);
            targetColor = EditorGUILayout.ColorField(L("Set Color To", "Set Color To"), targetColor);

            if (GUILayout.Button(L("Set Color", "Set Color"), GUILayout.Height(25)))
            {
                ApplyColorSet();
            }

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // Relative HSV adjustment
            EditorGUILayout.LabelField(L("Relative HSV Adjustment", "Relative HSV Adjustment"), EditorStyles.boldLabel);

            adjustHue = EditorGUILayout.Toggle(L("Adjust Hue", "Adjust Hue"), adjustHue);
            if (adjustHue)
            {
                hueShift = EditorGUILayout.Slider(L("Hue Shift", "Hue Shift"), hueShift, -180f, 180f);
            }

            adjustSaturation = EditorGUILayout.Toggle(L("Adjust Saturation", "Adjust Saturation"), adjustSaturation);
            if (adjustSaturation)
            {
                saturationMultiplier = EditorGUILayout.Slider(L("Saturation Multiply", "Saturation Multiply"), saturationMultiplier, 0f, 2f);
            }

            adjustBrightness = EditorGUILayout.Toggle(L("Adjust Brightness", "Adjust Brightness"), adjustBrightness);
            if (adjustBrightness)
            {
                valueMultiplier = EditorGUILayout.Slider(L("Brightness Multiply", "Brightness Multiply"), valueMultiplier, 0f, 2f);
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button(L("Apply HSV Adjustment", "Apply HSV Adjustment"), GUILayout.Height(25)))
            {
                ApplyHSVAdjustment();
            }

            EditorGUILayout.EndVertical();
        }

        // ========== Batch Mode: Texture Replacement ==========

        private void DrawTextureReplace()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("Batch Texture Replacement", "Batch Texture Replacement"), EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                L("Replace textures across all selected materials.", "Replace textures across all selected materials."),
                MessageType.Info);

            EditorGUILayout.Space(5);

            string[] textureProps = new[] { "_MainTex", "_BumpMap", "_EmissionMap", "_MatCapTex", "_MatCapTex2", "_MatCapTex3", "_RampTex",
                                           "_SpecularMask", "_RimMask", "_SSSMask", "_MatCapMask", "_MatCapMask2", "_MatCapMask3", "_EmissionMask",
                                           "_ReflectionMask", "_ParallaxMap" };
            int selectedProp = System.Array.IndexOf(textureProps, textureProperty);
            if (selectedProp < 0) selectedProp = 0;

            selectedProp = EditorGUILayout.Popup(L("Texture Property", "Texture Property"), selectedProp, textureProps);
            textureProperty = textureProps[selectedProp];

            replacementTexture = (Texture2D)EditorGUILayout.ObjectField(
                L("Replacement Texture", "Replacement Texture"),
                replacementTexture,
                typeof(Texture2D),
                false);

            EditorGUILayout.Space(10);

            int materialsWithThisTexture = selectedMaterials.Count(m =>
                m != null && m.HasProperty(textureProperty) && m.GetTexture(textureProperty) != null);

            EditorGUILayout.LabelField(L($"Materials with {textureProperty}: {materialsWithThisTexture}/{selectedMaterials.Count}", $"Materials with {textureProperty}: {materialsWithThisTexture}/{selectedMaterials.Count}"));

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(replacementTexture == null))
            {
                if (GUILayout.Button(L("Replace Texture in All Selected", "Replace Texture in All Selected"), GUILayout.Height(30)))
                {
                    ApplyTextureReplacement();
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button(L("Clear Texture in All Selected", "Clear Texture in All Selected"), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    L("Clear Texture", "Clear Texture"),
                    L($"Remove {textureProperty} from all selected materials?", $"Remove {textureProperty} from all selected materials?"),
                    L("Clear", "Clear"),
                    L("Cancel", "Cancel")))
                {
                    ClearTexture();
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ========== Batch Mode: Feature Toggle ==========

        private void DrawFeatureToggle()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("Batch Feature Toggle", "Batch Feature Toggle"), EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                L("Enable or disable shader features across all selected materials.", "Enable or disable shader features across all selected materials."),
                MessageType.Info);

            EditorGUILayout.Space(5);

            for (int i = 0; i < features.Length; i++)
            {
                featureStates[i] = EditorGUILayout.Toggle(GetFeatureName(features[i]), featureStates[i]);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(L("Enable Selected Features", "Enable Selected Features"), GUILayout.Height(25)))
            {
                ApplyFeatureToggle(true);
            }

            if (GUILayout.Button(L("Disable Selected Features", "Disable Selected Features"), GUILayout.Height(25)))
            {
                ApplyFeatureToggle(false);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button(L("Disable All Features (Max Performance)", "Disable All Features (Max Performance)"), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    L("Disable All Features", "Disable All Features"),
                    L("Disable all shader features for maximum performance?", "Disable all shader features for maximum performance?"),
                    L("Disable All", "Disable All"),
                    L("Cancel", "Cancel")))
                {
                    DisableAllFeatures();
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ========== Batch Mode: Variant Conversion ==========

        private void DrawVariantConvert()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("Batch Variant Conversion", "Batch Variant Conversion"), EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                L("Convert materials between shader variants (Opaque/Cutout/Transparent).", "Convert materials between shader variants (Opaque/Cutout/Transparent)."),
                MessageType.Info);

            EditorGUILayout.Space(5);

            targetVariant = (ShaderVariant)EditorGUILayout.EnumPopup(L("Target Variant", "Target Variant"), targetVariant);

            EditorGUILayout.Space(10);

            // Statistics
            int opaqueCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Toon Shader") && !m.shader.name.Contains("Cutout") && !m.shader.name.Contains("Transparent"));
            int cutoutCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Cutout"));
            int transparentCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Transparent"));

            EditorGUILayout.LabelField(L("Current Distribution:", "Current Distribution:"));
            EditorGUILayout.LabelField($"  Opaque: {opaqueCount}");
            EditorGUILayout.LabelField($"  Cutout: {cutoutCount}");
            EditorGUILayout.LabelField($"  Transparent: {transparentCount}");

            EditorGUILayout.Space(10);

            if (GUILayout.Button(L($"Convert All to {targetVariant}", $"Convert All to {targetVariant}"), GUILayout.Height(30)))
            {
                ApplyVariantConversion();
            }

            EditorGUILayout.EndVertical();
        }

        // ========== Common UI Helper Methods ==========

        private bool DrawFoldoutSection(string title, bool foldout, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
            if (foldout)
            {
                EditorGUI.indentLevel++;
                try
                {
                    drawContent?.Invoke();
                }
                catch (System.Exception e)
                {
                    EditorGUILayout.HelpBox(L($"Error: {e.Message}", $"Error: {e.Message}"), MessageType.Error);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            return foldout;
        }

        private void DrawColorProperty(string propertyName, string label)
        {
            if (currentMaterial == null || !currentMaterial.HasProperty(propertyName)) return;

            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUILayout.ColorField(label, currentMaterial.GetColor(propertyName));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentMaterial, "Change Material Color");
                currentMaterial.SetColor(propertyName, newColor);
                EditorUtility.SetDirty(currentMaterial);
            }
        }

        private void DrawFloatProperty(string propertyName, string label, float min, float max)
        {
            if (currentMaterial == null || !currentMaterial.HasProperty(propertyName)) return;

            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.Slider(label, currentMaterial.GetFloat(propertyName), min, max);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentMaterial, "Change Material Float");
                currentMaterial.SetFloat(propertyName, newValue);
                EditorUtility.SetDirty(currentMaterial);
            }
        }

        private void DrawIntProperty(string propertyName, string label, int min, int max)
        {
            if (currentMaterial == null || !currentMaterial.HasProperty(propertyName)) return;

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUILayout.IntSlider(label, (int)currentMaterial.GetFloat(propertyName), min, max);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentMaterial, "Change Material Int");
                currentMaterial.SetFloat(propertyName, newValue);
                EditorUtility.SetDirty(currentMaterial);
            }
        }

        private void DrawToggleProperty(string propertyName, string label, string keyword = null)
        {
            if (currentMaterial == null || !currentMaterial.HasProperty(propertyName)) return;

            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle(label, currentMaterial.GetFloat(propertyName) > 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentMaterial, "Change Material Toggle");
                currentMaterial.SetFloat(propertyName, newValue ? 1f : 0f);
                if (!string.IsNullOrEmpty(keyword))
                {
                    if (newValue)
                    {
                        currentMaterial.EnableKeyword(keyword);
                    }
                    else
                    {
                        currentMaterial.DisableKeyword(keyword);
                    }
                }
                EditorUtility.SetDirty(currentMaterial);
            }
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space(5);
        }

        private void DrawBatchSelectionButtons()
        {
            if (IsCompactLayout())
            {
                EditorGUILayout.BeginHorizontal();
                DrawBatchSelectionButton(L("Add Selected", "Add Selected"), AddSelectedMaterials);
                DrawBatchSelectionButton(L("Add All", "Add All"), AddAllNataneToonMaterials);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                DrawBatchSelectionButton(L("By Name", "By Name"), ShowAddByNameDialog);
                DrawBatchSelectionButton(L("Clear", "Clear"), () => selectedMaterials.Clear());
                EditorGUILayout.EndHorizontal();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawBatchSelectionButton(L("Add Selected", "Add Selected"), AddSelectedMaterials);
            DrawBatchSelectionButton(L("Add All", "Add All"), AddAllNataneToonMaterials);
            DrawBatchSelectionButton(L("By Name", "By Name"), ShowAddByNameDialog);
            DrawBatchSelectionButton(L("Clear", "Clear"), () => selectedMaterials.Clear());
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBatchSelectionButton(string label, System.Action action)
        {
            if (GUILayout.Button(label, GUILayout.Height(25)))
            {
                action?.Invoke();
            }
        }

        private bool IsCompactLayout()
        {
            return position.width < CompactLayoutWidth;
        }

        private float GetAdaptiveListHeight(float minHeight, float maxHeight, float ratio)
        {
            return Mathf.Clamp(position.height * ratio, minHeight, maxHeight);
        }

        // ========== Material Management ==========

        private void AddSelectedMaterials()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Material material)
                {
                    if (!selectedMaterials.Contains(material))
                    {
                        selectedMaterials.Add(material);
                    }
                }
            }
        }

        private void AddAllNataneToonMaterials()
        {
            foreach (MaterialIndexEntry entry in NataneAssetIndexService.EnumerateMaterialEntries(materialEntry => materialEntry.isNataneShader))
            {
                Material material = NataneAssetIndexService.LoadMaterial(entry);
                if (material != null && !selectedMaterials.Contains(material))
                {
                    selectedMaterials.Add(material);
                }
            }

            Debug.Log($"[UnifiedMaterialEditor] Added {selectedMaterials.Count} Natane Toon materials.");
        }

        private void ShowAddByNameDialog()
        {
            string searchTerm = EditorInputDialog.Show(
                L("Add Materials by Name", "Add Materials by Name"),
                L("Enter name:", "Enter name:"),
                string.Empty);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                AddMaterialsByName(searchTerm);
            }
        }

        private void AddMaterialsByName(string searchTerm)
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int addedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                if (material.name.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (!selectedMaterials.Contains(material))
                {
                    selectedMaterials.Add(material);
                    addedCount++;
                }
            }

            Debug.Log($"[UnifiedMaterialEditor] Added {addedCount} materials matching '{searchTerm}'.");
        }

        private void RefreshSceneMaterials()
        {
            selectedMaterials.Clear();

            if (selectedObject == null)
            {
                currentMaterial = null;
                return;
            }

            Renderer renderer = selectedObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null)
                    {
                        continue;
                    }

                    if (filterNataneToonOnly && !IsNataneToonShader(mat))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(searchFilter) &&
                        mat.name.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    selectedMaterials.Add(mat);
                }
            }

            if (selectedMaterials.Count > 0)
            {
                selectedMaterialIndex = Mathf.Clamp(selectedMaterialIndex, 0, selectedMaterials.Count - 1);
                currentMaterial = selectedMaterials[selectedMaterialIndex];
            }
            else
            {
                currentMaterial = null;
            }
        }

        private bool IsNataneToonShader(Material mat)
        {
            if (mat == null || mat.shader == null)
            {
                return false;
            }

            return mat.shader.name.Contains("Natane") && mat.shader.name.Contains("Toon");
        }

        // ========== Batch Operations ==========

        private void ApplyParameterAdjustment()
        {
            string paramName = floatParameters[selectedParameter];
            int successCount = 0;

            foreach (Material material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(paramName))
                {
                    continue;
                }

                Undo.RecordObject(material, "Batch Parameter Adjustment");

                float currentValue = material.GetFloat(paramName);
                float newValue = CalculateNewValue(currentValue, adjustValue, adjustMode);
                material.SetFloat(paramName, newValue);

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                L("Parameter Adjusted", "Parameter Adjusted"),
                L($"Adjusted {paramName} in {successCount} materials", $"Adjusted {paramName} in {successCount} materials"),
                "OK");
        }

        private float CalculateNewValue(float current, float adjust, AdjustMode mode)
        {
            switch (mode)
            {
                case AdjustMode.Set:
                    return adjust;
                case AdjustMode.Add:
                    return current + adjust;
                case AdjustMode.Multiply:
                    return current * adjust;
                default:
                    return current;
            }
        }

        private void ApplyColorSet()
        {
            string colorPropName = GetColorPropertyName(selectedColorParam);
            int successCount = 0;

            foreach (Material material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(colorPropName))
                {
                    continue;
                }

                Undo.RecordObject(material, "Batch Color Set");
                material.SetColor(colorPropName, targetColor);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                L("Color Set", "Color Set"),
                L($"Set {colorPropName} in {successCount} materials", $"Set {colorPropName} in {successCount} materials"),
                "OK");
        }

        private void ApplyHSVAdjustment()
        {
            string colorPropName = GetColorPropertyName(selectedColorParam);
            int successCount = 0;

            foreach (Material material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(colorPropName))
                {
                    continue;
                }

                Undo.RecordObject(material, "Batch HSV Adjustment");

                Color currentColor = material.GetColor(colorPropName);
                Color newColor = AdjustColorHSV(currentColor);
                material.SetColor(colorPropName, newColor);

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                L("HSV Adjusted", "HSV Adjusted"),
                L($"Adjusted {colorPropName} in {successCount} materials", $"Adjusted {colorPropName} in {successCount} materials"),
                "OK");
        }

        private Color AdjustColorHSV(Color color)
        {
            float h;
            float s;
            float v;
            Color.RGBToHSV(color, out h, out s, out v);

            if (adjustHue)
            {
                h = (h + hueShift / 360f) % 1f;
                if (h < 0f)
                {
                    h += 1f;
                }
            }

            if (adjustSaturation)
            {
                s = Mathf.Clamp01(s * saturationMultiplier);
            }

            if (adjustBrightness)
            {
                v = Mathf.Clamp01(v * valueMultiplier);
            }

            Color result = Color.HSVToRGB(h, s, v);
            result.a = color.a;
            return result;
        }

        private void ApplyTextureReplacement()
        {
            int successCount = 0;

            foreach (Material material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(textureProperty))
                {
                    continue;
                }

                Undo.RecordObject(material, "Batch Texture Replace");
                material.SetTexture(textureProperty, replacementTexture);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                L("Texture Replaced", "Texture Replaced"),
                L($"Replaced {textureProperty} in {successCount} materials", $"Replaced {textureProperty} in {successCount} materials"),
                "OK");
        }

        private void ClearTexture()
        {
            int successCount = 0;

            foreach (Material material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(textureProperty))
                {
                    continue;
                }

                Undo.RecordObject(material, "Batch Texture Clear");
                material.SetTexture(textureProperty, null);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                L("Texture Cleared", "Texture Cleared"),
                L($"Cleared {textureProperty} in {successCount} materials", $"Cleared {textureProperty} in {successCount} materials"),
                "OK");
        }

        private void ApplyFeatureToggle(bool enable)
        {
            int successCount = 0;

            foreach (Material material in selectedMaterials)
            {
                if (material == null)
                {
                    continue;
                }

                Undo.RecordObject(material, "Batch Feature Toggle");

                for (int i = 0; i < features.Length; i++)
                {
                    if (!featureStates[i])
                    {
                        continue;
                    }

                    if (enable)
                    {
                        material.EnableKeyword(features[i]);
                    }
                    else
                    {
                        material.DisableKeyword(features[i]);
                    }
                }

                EditorUtility.SetDirty(material);
                successCount++;
            }

            int featureCount = featureStates.Count(f => f);
            string action = enable ? "Enabled" : "Disabled";

            EditorUtility.DisplayDialog(
                L("Features Toggled", "Features Toggled"),
                L($"{action} {featureCount} features in {successCount} materials", $"{action} {featureCount} features in {successCount} materials"),
                "OK");
        }

        private void DisableAllFeatures()
        {
            int successCount = 0;

            foreach (Material material in selectedMaterials)
            {
                if (material == null)
                {
                    continue;
                }

                Undo.RecordObject(material, "Disable All Features");

                foreach (string feature in features)
                {
                    material.DisableKeyword(feature);
                }

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                L("All Features Disabled", "All Features Disabled"),
                L($"Disabled all features in {successCount} materials", $"Disabled all features in {successCount} materials"),
                "OK");
        }

        private void ApplyVariantConversion()
        {
            string targetShaderName = GetShaderNameForVariant(targetVariant);
            Shader targetShader = Shader.Find(targetShaderName);

            if (targetShader == null)
            {
                EditorUtility.DisplayDialog(
                    L("Error", "Error"),
                    L($"Shader not found: {targetShaderName}", $"Shader not found: {targetShaderName}"),
                    "OK");
                return;
            }

            int successCount = 0;

            foreach (Material material in selectedMaterials)
            {
                if (material == null)
                {
                    continue;
                }

                Undo.RecordObject(material, "Batch Variant Conversion");
                material.shader = targetShader;
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                L("Variant Converted", "Variant Converted"),
                L($"Converted {successCount} materials to {targetVariant} variant", $"Converted {successCount} materials to {targetVariant} variant"),
                "OK");
        }

        // ========== Scene Mode: Preset & Reset ==========

        private void ShowPresetMenu()
        {
            GenericMenu menu = new GenericMenu();

            string[] presetGuids = AssetDatabase.FindAssets("t:NataneToonMaterialPreset");
            if (presetGuids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent(L("No Presets Found", "No Presets Found")));
            }
            else
            {
                foreach (string guid in presetGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    NataneToonMaterialPreset preset = AssetDatabase.LoadAssetAtPath<NataneToonMaterialPreset>(path);
                    if (preset != null)
                    {
                        menu.AddItem(new GUIContent(preset.presetName), false, () => ApplyPreset(preset));
                    }
                }
            }

            menu.ShowAsContext();
        }

        private void ApplyPreset(NataneToonMaterialPreset preset)
        {
            if (preset == null || currentMaterial == null)
            {
                return;
            }

            Undo.RecordObject(currentMaterial, "Apply Preset");
            NataneToonMaterialPresetEditor.ApplyPresetWithUIUpdate(preset, currentMaterial);
            EditorUtility.SetDirty(currentMaterial);
            Debug.Log($"Applied preset '{preset.presetName}' to material '{currentMaterial.name}'.");
        }

        private void ResetMaterialToDefault()
        {
            if (currentMaterial == null)
            {
                return;
            }

            Undo.RecordObject(currentMaterial, "Reset Material");

            if (currentMaterial.HasProperty("_Color"))
            {
                currentMaterial.SetColor("_Color", Color.white);
            }
            if (currentMaterial.HasProperty("_Alpha"))
            {
                currentMaterial.SetFloat("_Alpha", 1f);
            }
            if (currentMaterial.HasProperty("_ShadowColor"))
            {
                currentMaterial.SetColor("_ShadowColor", new Color(0.5f, 0.5f, 0.5f, 1f));
            }
            if (currentMaterial.HasProperty("_ShadowSteps"))
            {
                currentMaterial.SetFloat("_ShadowSteps", 2f);
            }
            if (currentMaterial.HasProperty("_ShadowSharpness"))
            {
                currentMaterial.SetFloat("_ShadowSharpness", 0.5f);
            }

            EditorUtility.SetDirty(currentMaterial);
            Debug.Log($"Reset material '{currentMaterial.name}' to default values.");
        }

        // ========== Helper Methods ==========

        private string GetColorPropertyName(ColorParameter param)
        {
            switch (param)
            {
                case ColorParameter.MainColor: return "_Color";
                case ColorParameter.ShadowColor: return "_ShadowColor";
                case ColorParameter.RimColor: return "_RimColor";
                case ColorParameter.EmissionColor: return "_EmissionColor";
                case ColorParameter.OutlineColor: return "_OutlineColor";
                case ColorParameter.SpecularColor: return "_SpecularColor";
                default: return "_Color";
            }
        }

        private string GetFeatureName(string keyword)
        {
            return keyword.Replace("_", " ").Trim();
        }

        private string GetShaderNameForVariant(ShaderVariant variant)
        {
            switch (variant)
            {
                case ShaderVariant.Opaque: return "Natane/Toon Shader";
                case ShaderVariant.Cutout: return "Natane/Toon Shader Cutout";
                case ShaderVariant.Transparent: return "Natane/Toon Shader Transparent";
                default: return "Natane/Toon Shader";
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (currentMode == EditorMode.Scene)
            {
                Repaint();
            }
        }

        private void Update()
        {
            if (currentMode == EditorMode.Scene)
            {
                Repaint();
            }
        }
    }

    /// <summary>
    /// Simple input dialog helper
    /// 驛｢・ｧ繝ｻ・ｷ驛｢譎｢・ｽ・ｳ驛｢譎丞ｹｲ・取刮・ｸ・ｺ繝ｻ・ｪ髯ｷ闌ｨ・ｽ・･髯ｷ迚呻ｽｸ蜷ｶﾎ帝Δ・ｧ繝ｻ・､驛｢・ｧ繝ｻ・｢驛｢譎｢・ｽ・ｭ驛｢・ｧ繝ｻ・ｰ驛｢譎渉・･・取刮・ｹ譏懶ｽｻ・｣郢晢ｽｻ
    /// </summary>
    public static class EditorInputDialog
    {
        public static string Show(string title, string message, string defaultValue)
        {
            // Unity doesn't have built-in input dialog, so we return default for now
            // In real implementation, would create a custom EditorWindow
            return defaultValue;
        }
    }
}
