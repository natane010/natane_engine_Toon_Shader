using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// 統合マテリアルエディタ - BatchMaterialProcessorとSceneMaterialEditorの機能を統合
    /// Unified Material Editor - Integrates BatchMaterialProcessor and SceneMaterialEditor functionality
    ///
    /// 2つの処理モードを提供:
    /// - Batch Mode: 複数マテリアルの一括処理
    /// - Scene Mode: シーン上のオブジェクトのリアルタイム編集
    /// </summary>
    public class UnifiedMaterialEditor : EditorWindow
    {
        // ========== Mode Selection ==========
        private enum EditorMode { Batch, Scene }
        private EditorMode currentMode = EditorMode.Batch;

        // ========== Common State ==========
        private Vector2 scrollPosition;
        private List<Material> selectedMaterials = new List<Material>();
        private Material currentMaterial;

        // ========== Batch Mode State ==========
        private Vector2 materialListScroll;
        private int selectedTab = 0;
        private string[] tabs = new[] { "パラメータ Parameter", "色 Color", "テクスチャ Texture", "機能 Feature", "バリアント Variant" };

        // Parameter adjustment (Batch Mode)
        private enum AdjustMode { Set, Add, Multiply }
        private string[] floatParameters = new[] { "_ToonSteps", "_ToonSharpness", "_ShadowReceive", "_OutlineWidth",
                                                   "_SpecularIntensity", "_RimIntensity", "_SSSIntensity",
                                                   "_EmissionIntensity", "_ReflectionIntensity", "_Metallic", "_Smoothness" };
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
        private string[] features = new[] { "_SPECULAR", "_RIM", "_SSS", "_MATCAP", "_OUTLINE", "_EMISSION",
                                           "_NORMALMAP", "_REFLECTION", "_ENV_RIM", "_PARALLAX", "_REFRACTION" };
        private bool[] featureStates = new bool[11];

        // Variant conversion (Batch Mode)
        private enum ShaderVariant { Opaque, Cutout, Transparent }
        private ShaderVariant targetVariant = ShaderVariant.Opaque;

        // ========== Scene Mode State ==========
        private GameObject selectedObject;
        private int selectedMaterialIndex = 0;

        // UI折りたたみ状態 (Scene Mode)
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

        [MenuItem("Tools/Natane/マテリアル Material/マテリアルエディタ Material Editor", false, 12)]
        public static void ShowWindow()
        {
            var window = GetWindow<UnifiedMaterialEditor>("マテリアルエディタ Material Editor");
            window.minSize = new Vector2(650, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // Scene Mode用のイベント登録
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            // Scene Mode用のイベント解除
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
        }

        // ========== Header & Mode Selection ==========

        private void DrawHeader()
        {
            NataneToonShaderGUIUtility.DrawToolHeader("統合マテリアルエディタ", "Unified Material Editor", "UnifiedMaterialEditor");
            EditorGUILayout.HelpBox(
                "Batch Mode: 複数マテリアル一括処理 | Scene Mode: リアルタイム編集",
                MessageType.Info);
        }

        private void DrawModeSelector()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("モード Mode:", GUILayout.Width(100));

            EditorGUI.BeginChangeCheck();
            if (GUILayout.Toggle(currentMode == EditorMode.Batch, "Batch Mode 一括処理", EditorStyles.miniButtonLeft))
            {
                currentMode = EditorMode.Batch;
            }
            if (GUILayout.Toggle(currentMode == EditorMode.Scene, "Scene Mode シーン編集", EditorStyles.miniButtonRight))
            {
                currentMode = EditorMode.Scene;
            }
            if (EditorGUI.EndChangeCheck())
            {
                OnModeChanged();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OnModeChanged()
        {
            // モード切り替え時の初期化処理
            if (currentMode == EditorMode.Scene)
            {
                OnSelectionChanged();
            }
            else
            {
                // Batch Modeに切り替え時、Scene Modeで編集していたマテリアルをクリア
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
                EditorGUILayout.HelpBox("マテリアルを選択して一括処理を開始してください\nSelect materials to begin batch processing", MessageType.Info);
            }
        }

        private void DrawBatchMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("マテリアル選択 Material Selection", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("選択を追加 Add Selected", GUILayout.Height(25)))
            {
                AddSelectedMaterials();
            }

            if (GUILayout.Button("全Natane Toon追加 Add All", GUILayout.Height(25)))
            {
                AddAllNataneToonMaterials();
            }

            if (GUILayout.Button("名前で追加 By Name", GUILayout.Height(25)))
            {
                ShowAddByNameDialog();
            }

            if (GUILayout.Button("クリア Clear", GUILayout.Height(25)))
            {
                selectedMaterials.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (selectedMaterials.Count > 0)
            {
                EditorGUILayout.LabelField($"選択中 Selected: {selectedMaterials.Count} マテリアル materials", EditorStyles.boldLabel);

                materialListScroll = EditorGUILayout.BeginScrollView(materialListScroll, GUILayout.Height(100));
                for (int i = selectedMaterials.Count - 1; i >= 0; i--)
                {
                    if (selectedMaterials[i] == null)
                    {
                        selectedMaterials.RemoveAt(i);
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(selectedMaterials[i], typeof(Material), false);
                    if (GUILayout.Button("×", GUILayout.Width(20)))
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
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(25));
        }

        private void DrawBatchTabContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawParameterAdjust(); break;
                case 1: DrawColorAdjust(); break;
                case 2: DrawTextureReplace(); break;
                case 3: DrawFeatureToggle(); break;
                case 4: DrawVariantConvert(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        // ========== Scene Mode UI ==========

        private void DrawSceneMode()
        {
            DrawSceneToolbar();
            EditorGUILayout.Space(10);

            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox("シーンでオブジェクトを選択してください。\nPlease select an object in the scene.", MessageType.Info);
                return;
            }

            if (selectedMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox("選択されたオブジェクトにマテリアルが見つかりませんでした。\nNo materials found on selected object.", MessageType.Warning);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawObjectInfo();
            EditorGUILayout.Space(10);

            DrawSceneMaterialSelector();
            EditorGUILayout.Space(10);

            if (currentMaterial != null)
            {
                DrawSceneMaterialEditor();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSceneToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("更新 Refresh", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RefreshSceneMaterials();
            }

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            filterNataneToonOnly = GUILayout.Toggle(filterNataneToonOnly, "Nataneのみ Only", EditorStyles.toolbarButton, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshSceneMaterials();
            }

            EditorGUILayout.EndHorizontal();

            // 検索フィルター
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("検索 Search:", GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshSceneMaterials();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawObjectInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("選択オブジェクト Selected Object", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("名前 Name:", selectedObject.name);
            EditorGUILayout.LabelField("マテリアル数 Material Count:", selectedMaterials.Count.ToString());
            EditorGUILayout.EndVertical();
        }

        private void DrawSceneMaterialSelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("マテリアル選択 Material Selection", EditorStyles.boldLabel);

            if (selectedMaterials.Count > 1)
            {
                string[] materialNames = selectedMaterials.Select((m, i) => $"{i}: {(m != null ? m.name : "null")}").ToArray();
                EditorGUI.BeginChangeCheck();
                selectedMaterialIndex = EditorGUILayout.Popup("マテリアル Material:", selectedMaterialIndex, materialNames);
                if (EditorGUI.EndChangeCheck())
                {
                    currentMaterial = selectedMaterials[selectedMaterialIndex];
                }
            }
            else
            {
                EditorGUILayout.LabelField("マテリアル Material:", currentMaterial != null ? currentMaterial.name : "null");
            }

            if (currentMaterial != null)
            {
                EditorGUILayout.LabelField("シェーダー Shader:", currentMaterial.shader.name);

                if (!IsNataneToonShader(currentMaterial))
                {
                    EditorGUILayout.HelpBox("このマテリアルはNatane Toon Shaderを使用していません。\nThis material is not using Natane Toon Shader.", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneMaterialEditor()
        {
            // 基本設定
            showBasicSettings = DrawFoldoutSection("基本設定 Basic Settings", showBasicSettings, () =>
            {
                DrawColorProperty("_Color", "メインカラー Main Color");
                DrawFloatProperty("_Alpha", "透明度 Alpha", 0f, 1f);
            });

            // シェーディング設定
            showShadingSettings = DrawFoldoutSection("シェーディング Shading", showShadingSettings, () =>
            {
                DrawColorProperty("_ShadowColor", "影色 Shadow Color");
                DrawIntProperty("_ToonSteps", "トゥーン段階 Toon Steps", 1, 10);
                DrawFloatProperty("_ToonSharpness", "境界シャープネス Sharpness", 0f, 1f);
                DrawFloatProperty("_ShadowReceive", "影の受け取り Shadow Receive", 0f, 1f);
            });

            // スペキュラー設定
            showSpecularSettings = DrawFoldoutSection("スペキュラー Specular", showSpecularSettings, () =>
            {
                if (currentMaterial.HasProperty("_UseSpecular"))
                {
                    DrawToggleProperty("_UseSpecular", "スペキュラーを使用 Use Specular");
                    if (currentMaterial.GetFloat("_UseSpecular") > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        DrawColorProperty("_SpecularColor", "スペキュラー色 Color");
                        DrawFloatProperty("_SpecularIntensity", "強度 Intensity", 0f, 2f);
                        DrawFloatProperty("_SpecularSize", "サイズ Size", 0f, 1f);
                        DrawFloatProperty("_SpecularSharpness", "シャープネス Sharpness", 0f, 1f);
                        EditorGUI.indentLevel--;
                    }
                }
            });

            // リムライト設定
            showRimLightSettings = DrawFoldoutSection("リムライト Rim Light", showRimLightSettings, () =>
            {
                if (currentMaterial.HasProperty("_UseRimLight"))
                {
                    DrawToggleProperty("_UseRimLight", "リムライトを使用 Use Rim Light");
                    if (currentMaterial.GetFloat("_UseRimLight") > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        DrawColorProperty("_RimColor", "リムライト色 Color");
                        DrawFloatProperty("_RimIntensity", "強度 Intensity", 0f, 2f);
                        DrawFloatProperty("_RimPower", "パワー Power", 0.1f, 10f);
                        EditorGUI.indentLevel--;
                    }
                }
            });

            // アウトライン設定
            showOutlineSettings = DrawFoldoutSection("アウトライン Outline", showOutlineSettings, () =>
            {
                if (currentMaterial.HasProperty("_UseOutline"))
                {
                    DrawToggleProperty("_UseOutline", "アウトラインを使用 Use Outline");
                    if (currentMaterial.GetFloat("_UseOutline") > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        DrawColorProperty("_OutlineColor", "アウトライン色 Color");
                        DrawFloatProperty("_OutlineWidth", "幅 Width", 0f, 0.1f);
                        EditorGUI.indentLevel--;
                    }
                }
            });

            // エミッション設定
            showEmissionSettings = DrawFoldoutSection("エミッション Emission", showEmissionSettings, () =>
            {
                if (currentMaterial.HasProperty("_UseEmission"))
                {
                    DrawToggleProperty("_UseEmission", "エミッションを使用 Use Emission");
                    if (currentMaterial.GetFloat("_UseEmission") > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        DrawColorProperty("_EmissionColor", "エミッション色 Color");
                        DrawFloatProperty("_EmissionIntensity", "強度 Intensity", 0f, 5f);
                        EditorGUI.indentLevel--;
                    }
                }
            });

            // 高度な設定
            showAdvancedSettings = DrawFoldoutSection("高度な設定 Advanced", showAdvancedSettings, () =>
            {
                DrawFloatProperty("_Metallic", "メタリック Metallic", 0f, 1f);
                DrawFloatProperty("_Smoothness", "スムースネス Smoothness", 0f, 1f);
                if (currentMaterial.HasProperty("_Cull"))
                {
                    DrawIntProperty("_Cull", "カリングモード Cull Mode", 0, 2);
                }
            });

            EditorGUILayout.Space(10);

            // 操作ボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("プリセットを適用 Apply Preset"))
            {
                ShowPresetMenu();
            }
            if (GUILayout.Button("初期値に戻す Reset"))
            {
                if (EditorUtility.DisplayDialog("確認 Confirm",
                    "マテリアルを初期値に戻しますか？\nReset material to default values?",
                    "はい Yes", "いいえ No"))
                {
                    ResetMaterialToDefault();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // ========== Batch Mode: Parameter Adjustment ==========

        private void DrawParameterAdjust()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("パラメータ一括調整 Batch Parameter Adjustment", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Set = 置き換え replace value, Add = 加算 add to current, Multiply = 乗算 multiply current",
                MessageType.Info);

            EditorGUILayout.Space(5);

            selectedParameter = EditorGUILayout.Popup("パラメータ Parameter", selectedParameter, floatParameters);
            adjustMode = (AdjustMode)EditorGUILayout.EnumPopup("モード Mode", adjustMode);

            string label = adjustMode == AdjustMode.Set ? "新しい値 New Value" :
                          adjustMode == AdjustMode.Add ? "加算量 Add Amount" : "乗算 Multiply By";
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
                    EditorGUILayout.LabelField($"例 Example: {currentValue:F3} → {newValue:F3}");
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("すべてに適用 Apply to All Selected Materials", GUILayout.Height(30)))
            {
                ApplyParameterAdjustment();
            }

            EditorGUILayout.EndVertical();
        }

        // ========== Batch Mode: Color Adjustment ==========

        private void DrawColorAdjust()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("色一括調整 Batch Color Adjustment", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "絶対値で設定、またはHSV値を相対的に調整できます\nCan set absolute color or adjust HSV values relatively.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            selectedColorParam = (ColorParameter)EditorGUILayout.EnumPopup("色パラメータ Color Parameter", selectedColorParam);

            EditorGUILayout.Space(10);

            // Absolute color setting
            EditorGUILayout.LabelField("絶対色設定 Absolute Color Setting", EditorStyles.boldLabel);
            targetColor = EditorGUILayout.ColorField("色を設定 Set Color To", targetColor);

            if (GUILayout.Button("色を設定 Set Color", GUILayout.Height(25)))
            {
                ApplyColorSet();
            }

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // Relative HSV adjustment
            EditorGUILayout.LabelField("相対HSV調整 Relative HSV Adjustment", EditorStyles.boldLabel);

            adjustHue = EditorGUILayout.Toggle("色相調整 Adjust Hue", adjustHue);
            if (adjustHue)
            {
                hueShift = EditorGUILayout.Slider("色相シフト Hue Shift", hueShift, -180f, 180f);
            }

            adjustSaturation = EditorGUILayout.Toggle("彩度調整 Adjust Saturation", adjustSaturation);
            if (adjustSaturation)
            {
                saturationMultiplier = EditorGUILayout.Slider("彩度乗算 Saturation Multiply", saturationMultiplier, 0f, 2f);
            }

            adjustBrightness = EditorGUILayout.Toggle("明度調整 Adjust Brightness", adjustBrightness);
            if (adjustBrightness)
            {
                valueMultiplier = EditorGUILayout.Slider("明度乗算 Brightness Multiply", valueMultiplier, 0f, 2f);
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("HSV調整を適用 Apply HSV Adjustment", GUILayout.Height(25)))
            {
                ApplyHSVAdjustment();
            }

            EditorGUILayout.EndVertical();
        }

        // ========== Batch Mode: Texture Replacement ==========

        private void DrawTextureReplace()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("テクスチャ一括置換 Batch Texture Replacement", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択したすべてのマテリアルのテクスチャを置換します\nReplace textures across all selected materials.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            string[] textureProps = new[] { "_MainTex", "_BumpMap", "_EmissionMap", "_MatCap", "_RampTex",
                                           "_SpecularMask", "_RimMask", "_SSSMask", "_MatCapMask", "_EmissionMask",
                                           "_ReflectionMask", "_ParallaxMap" };
            int selectedProp = System.Array.IndexOf(textureProps, textureProperty);
            if (selectedProp < 0) selectedProp = 0;

            selectedProp = EditorGUILayout.Popup("テクスチャプロパティ Texture Property", selectedProp, textureProps);
            textureProperty = textureProps[selectedProp];

            replacementTexture = (Texture2D)EditorGUILayout.ObjectField(
                "置換テクスチャ Replacement Texture",
                replacementTexture,
                typeof(Texture2D),
                false);

            EditorGUILayout.Space(10);

            int materialsWithThisTexture = selectedMaterials.Count(m =>
                m != null && m.HasProperty(textureProperty) && m.GetTexture(textureProperty) != null);

            EditorGUILayout.LabelField($"{textureProperty}を持つマテリアル: {materialsWithThisTexture}/{selectedMaterials.Count}");

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(replacementTexture == null))
            {
                if (GUILayout.Button("すべてのテクスチャを置換 Replace Texture in All Selected", GUILayout.Height(30)))
                {
                    ApplyTextureReplacement();
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("すべてのテクスチャをクリア Clear Texture in All Selected", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "テクスチャをクリア Clear Texture",
                    $"選択したすべてのマテリアルから{textureProperty}を削除しますか？\nRemove {textureProperty} from all selected materials?",
                    "クリア Clear",
                    "キャンセル Cancel"))
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
            EditorGUILayout.LabelField("機能一括切替 Batch Feature Toggle", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選択したすべてのマテリアルのシェーダー機能を有効/無効にします\nEnable or disable shader features across all selected materials.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            for (int i = 0; i < features.Length; i++)
            {
                featureStates[i] = EditorGUILayout.Toggle(GetFeatureName(features[i]), featureStates[i]);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("選択機能を有効化 Enable Selected Features", GUILayout.Height(25)))
            {
                ApplyFeatureToggle(true);
            }

            if (GUILayout.Button("選択機能を無効化 Disable Selected Features", GUILayout.Height(25)))
            {
                ApplyFeatureToggle(false);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("すべての機能を無効化（最大パフォーマンス）Disable All Features", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "すべての機能を無効化 Disable All Features",
                    "選択したマテリアルのすべてのシェーダー機能を無効化して最大パフォーマンスにしますか？\nDisable all shader features for maximum performance?",
                    "すべて無効化 Disable All",
                    "キャンセル Cancel"))
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
            EditorGUILayout.LabelField("バリアント一括変換 Batch Variant Conversion", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "シェーダーバリアント間（Opaque/Cutout/Transparent）でマテリアルを変換します\nConvert materials between shader variants.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            targetVariant = (ShaderVariant)EditorGUILayout.EnumPopup("ターゲットバリアント Target Variant", targetVariant);

            EditorGUILayout.Space(10);

            // Statistics
            int opaqueCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Toon Shader") && !m.shader.name.Contains("Cutout") && !m.shader.name.Contains("Transparent"));
            int cutoutCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Cutout"));
            int transparentCount = selectedMaterials.Count(m => m != null && m.shader.name.Contains("Transparent"));

            EditorGUILayout.LabelField($"現在の分布 Current Distribution:");
            EditorGUILayout.LabelField($"  Opaque: {opaqueCount}");
            EditorGUILayout.LabelField($"  Cutout: {cutoutCount}");
            EditorGUILayout.LabelField($"  Transparent: {transparentCount}");

            EditorGUILayout.Space(10);

            if (GUILayout.Button($"すべてを{targetVariant}に変換 Convert All to {targetVariant}", GUILayout.Height(30)))
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
                    EditorGUILayout.HelpBox($"エラー Error: {e.Message}", MessageType.Error);
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

        private void DrawToggleProperty(string propertyName, string label)
        {
            if (currentMaterial == null || !currentMaterial.HasProperty(propertyName)) return;

            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle(label, currentMaterial.GetFloat(propertyName) > 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentMaterial, "Change Material Toggle");
                currentMaterial.SetFloat(propertyName, newValue ? 1f : 0f);
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
            string[] guids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && material.shader != null)
                {
                    if (material.shader.name.Contains("Natane") && material.shader.name.Contains("Toon"))
                    {
                        if (!selectedMaterials.Contains(material))
                        {
                            selectedMaterials.Add(material);
                        }
                    }
                }
            }

            Debug.Log($"[UnifiedMaterialEditor] {selectedMaterials.Count}個のNatane Toonマテリアルを見つけました Found {selectedMaterials.Count} materials");
        }

        private void ShowAddByNameDialog()
        {
            string searchTerm = EditorInputDialog.Show("名前でマテリアルを追加 Add Materials by Name", "検索する名前を入力 Enter name:", "");
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

                if (material != null && material.name.ToLower().Contains(searchTerm.ToLower()))
                {
                    if (!selectedMaterials.Contains(material))
                    {
                        selectedMaterials.Add(material);
                        addedCount++;
                    }
                }
            }

            Debug.Log($"[UnifiedMaterialEditor] '{searchTerm}'にマッチする{addedCount}個のマテリアルを追加 Added {addedCount} materials matching '{searchTerm}'");
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
                    if (mat != null)
                    {
                        if (!filterNataneToonOnly || IsNataneToonShader(mat))
                        {
                            if (string.IsNullOrEmpty(searchFilter) || mat.name.ToLower().Contains(searchFilter.ToLower()))
                            {
                                selectedMaterials.Add(mat);
                            }
                        }
                    }
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
            if (mat == null || mat.shader == null) return false;
            return mat.shader.name.Contains("Natane") && mat.shader.name.Contains("Toon");
        }

        // ========== Batch Operations ==========

        private void ApplyParameterAdjustment()
        {
            string paramName = floatParameters[selectedParameter];
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(paramName)) continue;

                Undo.RecordObject(material, "Batch Parameter Adjustment");

                float currentValue = material.GetFloat(paramName);
                float newValue = CalculateNewValue(currentValue, adjustValue, adjustMode);
                material.SetFloat(paramName, newValue);

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "パラメータを調整しました Parameter Adjusted",
                $"{successCount}個のマテリアルの{paramName}を調整しました\nAdjusted {paramName} in {successCount} materials",
                "OK");
        }

        private float CalculateNewValue(float current, float adjust, AdjustMode mode)
        {
            switch (mode)
            {
                case AdjustMode.Set: return adjust;
                case AdjustMode.Add: return current + adjust;
                case AdjustMode.Multiply: return current * adjust;
                default: return current;
            }
        }

        private void ApplyColorSet()
        {
            string colorPropName = GetColorPropertyName(selectedColorParam);
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(colorPropName)) continue;

                Undo.RecordObject(material, "Batch Color Set");
                material.SetColor(colorPropName, targetColor);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "色を設定しました Color Set",
                $"{successCount}個のマテリアルの{colorPropName}を設定しました\nSet {colorPropName} in {successCount} materials",
                "OK");
        }

        private void ApplyHSVAdjustment()
        {
            string colorPropName = GetColorPropertyName(selectedColorParam);
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(colorPropName)) continue;

                Undo.RecordObject(material, "Batch HSV Adjustment");

                Color currentColor = material.GetColor(colorPropName);
                Color newColor = AdjustColorHSV(currentColor);
                material.SetColor(colorPropName, newColor);

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "HSVを調整しました HSV Adjusted",
                $"{successCount}個のマテリアルの{colorPropName}を調整しました\nAdjusted {colorPropName} in {successCount} materials",
                "OK");
        }

        private Color AdjustColorHSV(Color color)
        {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);

            if (adjustHue)
            {
                h = (h + hueShift / 360f) % 1f;
                if (h < 0) h += 1f;
            }

            if (adjustSaturation)
            {
                s = Mathf.Clamp01(s * saturationMultiplier);
            }

            if (adjustBrightness)
            {
                v = Mathf.Clamp01(v * valueMultiplier);
            }

            return Color.HSVToRGB(h, s, v);
        }

        private void ApplyTextureReplacement()
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(textureProperty)) continue;

                Undo.RecordObject(material, "Batch Texture Replace");
                material.SetTexture(textureProperty, replacementTexture);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "テクスチャを置換しました Texture Replaced",
                $"{successCount}個のマテリアルの{textureProperty}を置換しました\nReplaced {textureProperty} in {successCount} materials",
                "OK");
        }

        private void ClearTexture()
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null || !material.HasProperty(textureProperty)) continue;

                Undo.RecordObject(material, "Batch Texture Clear");
                material.SetTexture(textureProperty, null);
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "テクスチャをクリアしました Texture Cleared",
                $"{successCount}個のマテリアルの{textureProperty}をクリアしました\nCleared {textureProperty} in {successCount} materials",
                "OK");
        }

        private void ApplyFeatureToggle(bool enable)
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null) continue;

                Undo.RecordObject(material, "Batch Feature Toggle");

                for (int i = 0; i < features.Length; i++)
                {
                    if (featureStates[i])
                    {
                        if (enable)
                        {
                            material.EnableKeyword(features[i]);
                        }
                        else
                        {
                            material.DisableKeyword(features[i]);
                        }
                    }
                }

                EditorUtility.SetDirty(material);
                successCount++;
            }

            string action = enable ? "有効化しました Enabled" : "無効化しました Disabled";
            int featureCount = featureStates.Count(f => f);

            EditorUtility.DisplayDialog(
                "機能を切り替えました Features Toggled",
                $"{successCount}個のマテリアルの{featureCount}個の機能を{action}\n{action} {featureCount} features in {successCount} materials",
                "OK");
        }

        private void DisableAllFeatures()
        {
            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null) continue;

                Undo.RecordObject(material, "Disable All Features");

                foreach (string feature in features)
                {
                    material.DisableKeyword(feature);
                }

                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "すべての機能を無効化しました All Features Disabled",
                $"{successCount}個のマテリアルのすべての機能を無効化しました\nDisabled all features in {successCount} materials",
                "OK");
        }

        private void ApplyVariantConversion()
        {
            string targetShaderName = GetShaderNameForVariant(targetVariant);
            Shader targetShader = Shader.Find(targetShaderName);

            if (targetShader == null)
            {
                EditorUtility.DisplayDialog("エラー Error", $"シェーダーが見つかりません Shader not found: {targetShaderName}", "OK");
                return;
            }

            int successCount = 0;

            foreach (var material in selectedMaterials)
            {
                if (material == null) continue;

                Undo.RecordObject(material, "Batch Variant Conversion");
                material.shader = targetShader;
                EditorUtility.SetDirty(material);
                successCount++;
            }

            EditorUtility.DisplayDialog(
                "バリアントを変換しました Variant Converted",
                $"{successCount}個のマテリアルを{targetVariant}バリアントに変換しました\nConverted {successCount} materials to {targetVariant} variant",
                "OK");
        }

        // ========== Scene Mode: Preset & Reset ==========

        private void ShowPresetMenu()
        {
            GenericMenu menu = new GenericMenu();

            string[] presetGuids = AssetDatabase.FindAssets("t:NataneToonMaterialPreset");
            if (presetGuids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("プリセットが見つかりません No Presets Found"));
            }
            else
            {
                foreach (string guid in presetGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var preset = AssetDatabase.LoadAssetAtPath<MaterialSystem.NataneToonMaterialPreset>(path);
                    if (preset != null)
                    {
                        menu.AddItem(new GUIContent(preset.presetName), false, () => ApplyPreset(preset));
                    }
                }
            }

            menu.ShowAsContext();
        }

        private void ApplyPreset(MaterialSystem.NataneToonMaterialPreset preset)
        {
            if (preset != null && currentMaterial != null)
            {
                Undo.RecordObject(currentMaterial, "Apply Preset");
                NataneToonMaterialPresetEditor.ApplyPresetWithUIUpdate(preset, currentMaterial);
                Debug.Log($"プリセット '{preset.presetName}' を適用しました Applied preset '{preset.presetName}'");
            }
        }

        private void ResetMaterialToDefault()
        {
            if (currentMaterial == null) return;

            Undo.RecordObject(currentMaterial, "Reset Material");

            if (currentMaterial.HasProperty("_Color"))
                currentMaterial.SetColor("_Color", Color.white);
            if (currentMaterial.HasProperty("_Alpha"))
                currentMaterial.SetFloat("_Alpha", 1f);
            if (currentMaterial.HasProperty("_ShadowColor"))
                currentMaterial.SetColor("_ShadowColor", new Color(0.5f, 0.5f, 0.5f, 1f));
            if (currentMaterial.HasProperty("_ToonSteps"))
                currentMaterial.SetFloat("_ToonSteps", 2);
            if (currentMaterial.HasProperty("_ToonSharpness"))
                currentMaterial.SetFloat("_ToonSharpness", 0.5f);

            EditorUtility.SetDirty(currentMaterial);
            Debug.Log($"マテリアル '{currentMaterial.name}' を初期値に戻しました Reset material '{currentMaterial.name}'");
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
    /// シンプルな入力ダイアログヘルパー
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
