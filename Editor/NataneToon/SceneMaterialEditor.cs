using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// シーン上のマテリアルパラメーターをリアルタイムで編集できるエディタウィンドウ
    /// Scene Material Editor for real-time material parameter editing
    /// </summary>
    public class SceneMaterialEditor : EditorWindow
    {
        private Vector2 scrollPosition;
        private GameObject selectedObject;
        private Material[] materials;
        private int selectedMaterialIndex = 0;
        private Material currentMaterial;

        // UI状態
        private bool showBasicSettings = true;
        private bool showShadingSettings = true;
        private bool showSpecularSettings = false;
        private bool showRimLightSettings = false;
        private bool showOutlineSettings = false;
        private bool showEmissionSettings = false;
        private bool showAdvancedSettings = false;

        // フィルター設定
        private bool filterNataneToonOnly = true;
        private string searchFilter = "";

        // プレビュー設定
        private bool autoRefresh = true;
        private float refreshRate = 0.1f;
        private double lastRefreshTime = 0;

        [MenuItem("Tools/Natane/Scene Material Editor")]
        public static void ShowWindow()
        {
            SceneMaterialEditor window = GetWindow<SceneMaterialEditor>("シーンマテリアル編集");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeGameObject != null)
            {
                selectedObject = Selection.activeGameObject;
                RefreshMaterials();
            }
        }

        private void RefreshMaterials()
        {
            if (selectedObject == null)
            {
                materials = null;
                currentMaterial = null;
                return;
            }

            List<Material> materialList = new List<Material>();

            // Renderer から Material を取得
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
                                materialList.Add(mat);
                            }
                        }
                    }
                }
            }

            materials = materialList.ToArray();

            if (materials.Length > 0)
            {
                selectedMaterialIndex = Mathf.Clamp(selectedMaterialIndex, 0, materials.Length - 1);
                currentMaterial = materials[selectedMaterialIndex];
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

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // ヘッダー
            EditorGUILayout.LabelField("シーンマテリアル編集", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scene Material Editor", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            DrawToolbar();
            EditorGUILayout.Space(10);

            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox("シーンでオブジェクトを選択してください。\nPlease select an object in the scene.", MessageType.Info);
                return;
            }

            if (materials == null || materials.Length == 0)
            {
                EditorGUILayout.HelpBox("選択されたオブジェクトにマテリアルが見つかりませんでした。\nNo materials found on selected object.", MessageType.Warning);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawObjectInfo();
            EditorGUILayout.Space(10);

            DrawMaterialSelector();
            EditorGUILayout.Space(10);

            if (currentMaterial != null)
            {
                DrawMaterialEditor();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("更新 Refresh", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RefreshMaterials();
            }

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            filterNataneToonOnly = GUILayout.Toggle(filterNataneToonOnly, "Nataneのみ Only", EditorStyles.toolbarButton, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshMaterials();
            }

            EditorGUILayout.EndHorizontal();

            // 検索フィルター
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("検索 Search:", GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshMaterials();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawObjectInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("選択オブジェクト Selected Object", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("名前 Name:", selectedObject.name);
            EditorGUILayout.LabelField("マテリアル数 Material Count:", materials.Length.ToString());
            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialSelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("マテリアル選択 Material Selection", EditorStyles.boldLabel);

            if (materials.Length > 1)
            {
                string[] materialNames = materials.Select((m, i) => $"{i}: {(m != null ? m.name : "null")}").ToArray();
                EditorGUI.BeginChangeCheck();
                selectedMaterialIndex = EditorGUILayout.Popup("マテリアル Material:", selectedMaterialIndex, materialNames);
                if (EditorGUI.EndChangeCheck())
                {
                    currentMaterial = materials[selectedMaterialIndex];
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
                    EditorGUILayout.HelpBox("このマテリアルはNatane Toon Shaderを使用していません。一部のパラメーターが表示されない場合があります。\nThis material is not using Natane Toon Shader. Some parameters may not be available.", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialEditor()
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
            });

            // リムライト設定
            showRimLightSettings = DrawFoldoutSection("リムライト Rim Light", showRimLightSettings, () =>
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
            });

            // アウトライン設定
            showOutlineSettings = DrawFoldoutSection("アウトライン Outline", showOutlineSettings, () =>
            {
                DrawToggleProperty("_UseOutline", "アウトラインを使用 Use Outline");
                if (currentMaterial.GetFloat("_UseOutline") > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    DrawColorProperty("_OutlineColor", "アウトライン色 Color");
                    DrawFloatProperty("_OutlineWidth", "幅 Width", 0f, 0.1f);
                    EditorGUI.indentLevel--;
                }
            });

            // エミッション設定
            showEmissionSettings = DrawFoldoutSection("エミッション Emission", showEmissionSettings, () =>
            {
                DrawToggleProperty("_UseEmission", "エミッションを使用 Use Emission");
                if (currentMaterial.GetFloat("_UseEmission") > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    DrawColorProperty("_EmissionColor", "エミッション色 Color");
                    DrawFloatProperty("_EmissionIntensity", "強度 Intensity", 0f, 5f);
                    EditorGUI.indentLevel--;
                }
            });

            // 高度な設定
            showAdvancedSettings = DrawFoldoutSection("高度な設定 Advanced", showAdvancedSettings, () =>
            {
                DrawFloatProperty("_Metallic", "メタリック Metallic", 0f, 1f);
                DrawFloatProperty("_Smoothness", "スムースネス Smoothness", 0f, 1f);
                DrawIntProperty("_Cull", "カリングモード Cull Mode", 0, 2);
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
            if (!currentMaterial.HasProperty(propertyName)) return;

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
            if (!currentMaterial.HasProperty(propertyName)) return;

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
            if (!currentMaterial.HasProperty(propertyName)) return;

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
            if (!currentMaterial.HasProperty(propertyName)) return;

            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle(label, currentMaterial.GetFloat(propertyName) > 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentMaterial, "Change Material Toggle");
                currentMaterial.SetFloat(propertyName, newValue ? 1f : 0f);
                EditorUtility.SetDirty(currentMaterial);
            }
        }

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
                Debug.Log($"プリセット '{preset.presetName}' を適用しました（UI更新済み）Applied preset '{preset.presetName}' (UI updated)");
            }
        }

        private void ResetMaterialToDefault()
        {
            if (currentMaterial == null) return;

            Undo.RecordObject(currentMaterial, "Reset Material");

            // 基本的なプロパティをデフォルトに戻す
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

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!autoRefresh) return;

            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastRefreshTime > refreshRate)
            {
                lastRefreshTime = currentTime;
                Repaint();
            }
        }

        private void Update()
        {
            if (autoRefresh)
            {
                Repaint();
            }
        }
    }
}
