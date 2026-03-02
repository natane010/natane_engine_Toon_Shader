using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// ヒエラルキー一括マテリアル編集ツール
    /// Hierarchy Material Batch Editor
    ///
    /// ヒエラルキー配下のNataneToonShader使用マテリアルの
    /// テクスチャ以外の数値プロパティを一括変更する。
    /// </summary>
    public class HierarchyMaterialBatchEditor : EditorWindow
    {
        // ========== Enums ==========
        private enum AdjustMode { Set, Add, Multiply }
        private enum PropertyFilter { All, FloatRange, Color, Vector }

        // ========== Target Selection ==========
        private GameObject rootObject;
        private List<MaterialEntry> scannedMaterials = new List<MaterialEntry>();
        private bool[] materialSelection;

        // ========== Property Browser ==========
        private List<ShaderPropertyInfo> shaderProperties = new List<ShaderPropertyInfo>();
        private bool[] propertySelection;
        private PropertyFilter propertyFilter = PropertyFilter.All;
        private string propertySearchFilter = "";

        // ========== Edit Settings ==========
        private AdjustMode adjustMode = AdjustMode.Set;

        // Per-property edit values
        private Dictionary<string, float> floatEditValues = new Dictionary<string, float>();
        private Dictionary<string, Color> colorEditValues = new Dictionary<string, Color>();
        private Dictionary<string, Vector4> vectorEditValues = new Dictionary<string, Vector4>();

        // ========== UI State ==========
        private Vector2 scrollPosition;
        private bool foldoutMaterials = true;
        private bool foldoutProperties = true;
        private bool foldoutApply = true;

        // ========== Structs ==========
        private struct MaterialEntry
        {
            public Material material;
            public string hierarchyPath;
        }

        private struct ShaderPropertyInfo
        {
            public string name;
            public string description;
            public ShaderUtil.ShaderPropertyType type;
            public float rangeMin;
            public float rangeMax;
        }

        [MenuItem("Tools/Natane/マテリアル Material/ヒエラルキー一括編集 Hierarchy Batch Editor", false, 15)]
        public static void ShowWindow()
        {
            var window = GetWindow<HierarchyMaterialBatchEditor>(
                L("ヒエラルキー一括編集", "Hierarchy Batch Editor"));
            window.minSize = new Vector2(550, 600);
            window.Show();
        }

        private void OnGUI()
        {
            NataneToonShaderGUIUtility.DrawToolHeader(
                "ヒエラルキー一括マテリアル編集",
                "Hierarchy Material Batch Editor",
                "HierarchyBatchEditor");

            EditorGUILayout.Space(4);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawTargetSelection();
            EditorGUILayout.Space(4);

            if (scannedMaterials.Count > 0)
            {
                DrawMaterialList();
                EditorGUILayout.Space(4);
                DrawPropertyBrowser();
                EditorGUILayout.Space(4);
                DrawApplySection();
            }

            EditorGUILayout.EndScrollView();
        }

        // ========== Target Selection ==========
        private void DrawTargetSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                L("ターゲット選択", "Target Selection"),
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            rootObject = (GameObject)EditorGUILayout.ObjectField(
                L("ルートオブジェクト", "Root Object"),
                rootObject,
                typeof(GameObject),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                // Clear previous scan when root changes
                scannedMaterials.Clear();
                shaderProperties.Clear();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(rootObject == null))
            {
                if (NataneToonShaderGUIUtility.DrawPrimaryButton(
                    L("スキャン", "Scan"), 200, 28))
                {
                    ScanHierarchy();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (scannedMaterials.Count > 0)
            {
                NataneToonShaderGUIUtility.DrawSuccessBox(
                    L($"{scannedMaterials.Count} 個のNataneToonマテリアルが見つかりました",
                      $"Found {scannedMaterials.Count} NataneToon material(s)"));
            }
            else if (rootObject != null)
            {
                EditorGUILayout.HelpBox(
                    L("「スキャン」を押してマテリアルを検索してください",
                      "Press \"Scan\" to search for materials"),
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        // ========== Scan Hierarchy ==========
        private void ScanHierarchy()
        {
            scannedMaterials.Clear();
            shaderProperties.Clear();
            floatEditValues.Clear();
            colorEditValues.Clear();
            vectorEditValues.Clear();

            if (rootObject == null) return;

            var renderers = rootObject.GetComponentsInChildren<Renderer>(true);
            var uniqueMaterials = new HashSet<Material>();

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (!IsNataneToonShader(mat)) continue;
                    if (uniqueMaterials.Contains(mat)) continue;

                    uniqueMaterials.Add(mat);

                    // Build hierarchy path
                    string path = GetRelativeHierarchyPath(renderer.transform, rootObject.transform);

                    scannedMaterials.Add(new MaterialEntry
                    {
                        material = mat,
                        hierarchyPath = path
                    });
                }
            }

            materialSelection = new bool[scannedMaterials.Count];
            for (int i = 0; i < materialSelection.Length; i++)
                materialSelection[i] = true;

            // Build property list from first material's shader
            if (scannedMaterials.Count > 0)
            {
                BuildPropertyList(scannedMaterials[0].material.shader);
            }
        }

        private static bool IsNataneToonShader(Material material)
        {
            if (material == null || material.shader == null) return false;
            string name = material.shader.name;
            return name.Contains("Natane") && name.Contains("Toon");
        }

        private static string GetRelativeHierarchyPath(Transform child, Transform root)
        {
            var parts = new List<string>();
            var current = child;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            parts.Reverse();
            return parts.Count > 0 ? string.Join("/", parts) : child.name;
        }

        // ========== Build Property List ==========
        private void BuildPropertyList(Shader shader)
        {
            shaderProperties.Clear();
            floatEditValues.Clear();
            colorEditValues.Clear();
            vectorEditValues.Clear();

            int count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; i++)
            {
                var type = ShaderUtil.GetPropertyType(shader, i);

                // Exclude textures
                if (type == ShaderUtil.ShaderPropertyType.TexEnv) continue;

                string propName = ShaderUtil.GetPropertyName(shader, i);
                string propDesc = ShaderUtil.GetPropertyDescription(shader, i);

                var info = new ShaderPropertyInfo
                {
                    name = propName,
                    description = propDesc,
                    type = type,
                    rangeMin = 0f,
                    rangeMax = 1f
                };

                if (type == ShaderUtil.ShaderPropertyType.Range)
                {
                    info.rangeMin = ShaderUtil.GetRangeLimits(shader, i, 1);
                    info.rangeMax = ShaderUtil.GetRangeLimits(shader, i, 2);
                }

                shaderProperties.Add(info);

                // Initialize default edit values
                if (type == ShaderUtil.ShaderPropertyType.Float || type == ShaderUtil.ShaderPropertyType.Range)
                {
                    if (!floatEditValues.ContainsKey(propName))
                        floatEditValues[propName] = adjustMode == AdjustMode.Multiply ? 1f : 0f;
                }
                else if (type == ShaderUtil.ShaderPropertyType.Color)
                {
                    if (!colorEditValues.ContainsKey(propName))
                        colorEditValues[propName] = Color.white;
                }
                else if (type == ShaderUtil.ShaderPropertyType.Vector)
                {
                    if (!vectorEditValues.ContainsKey(propName))
                        vectorEditValues[propName] = Vector4.zero;
                }
            }

            propertySelection = new bool[shaderProperties.Count];
        }

        // ========== Material List ==========
        private void DrawMaterialList()
        {
            foldoutMaterials = NataneToonShaderGUIUtility.DrawFoldoutSection(
                L("マテリアル一覧", "Material List"),
                foldoutMaterials,
                () =>
                {
                    // Select All / Deselect All
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(L("全て選択", "Select All"), GUILayout.Height(22)))
                    {
                        for (int i = 0; i < materialSelection.Length; i++)
                            materialSelection[i] = true;
                    }
                    if (GUILayout.Button(L("全て解除", "Deselect All"), GUILayout.Height(22)))
                    {
                        for (int i = 0; i < materialSelection.Length; i++)
                            materialSelection[i] = false;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2);

                    // Material entries
                    for (int i = 0; i < scannedMaterials.Count; i++)
                    {
                        var entry = scannedMaterials[i];
                        EditorGUILayout.BeginHorizontal();

                        materialSelection[i] = EditorGUILayout.Toggle(materialSelection[i], GUILayout.Width(20));

                        // Material icon + name
                        EditorGUILayout.ObjectField(entry.material, typeof(Material), false, GUILayout.Width(180));

                        // Hierarchy path
                        EditorGUILayout.LabelField(entry.hierarchyPath,
                            EditorStyles.miniLabel, GUILayout.MinWidth(100));

                        EditorGUILayout.EndHorizontal();
                    }

                    int selectedCount = materialSelection.Count(s => s);
                    EditorGUILayout.LabelField(
                        L($"選択中: {selectedCount} / {scannedMaterials.Count}",
                          $"Selected: {selectedCount} / {scannedMaterials.Count}"),
                        EditorStyles.centeredGreyMiniLabel);
                });
        }

        // ========== Property Browser ==========
        private void DrawPropertyBrowser()
        {
            foldoutProperties = NataneToonShaderGUIUtility.DrawFoldoutSection(
                L("プロパティブラウザ", "Property Browser"),
                foldoutProperties,
                () =>
                {
                    // Filter tabs
                    string[] filterLabels = new[]
                    {
                        L("全て", "All"),
                        "Float / Range",
                        "Color",
                        "Vector"
                    };
                    propertyFilter = (PropertyFilter)GUILayout.Toolbar((int)propertyFilter, filterLabels);

                    EditorGUILayout.Space(2);

                    // Search filter
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(L("検索", "Search"), GUILayout.Width(40));
                    propertySearchFilter = EditorGUILayout.TextField(propertySearchFilter);
                    if (GUILayout.Button("X", GUILayout.Width(22)))
                        propertySearchFilter = "";
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2);

                    // Edit mode
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(L("編集モード", "Edit Mode"), GUILayout.Width(80));
                    string[] modeLabels = new[]
                    {
                        L("設定 (Set)", "Set"),
                        L("加算 (Add)", "Add"),
                        L("乗算 (Multiply)", "Multiply")
                    };
                    adjustMode = (AdjustMode)GUILayout.Toolbar((int)adjustMode, modeLabels);
                    EditorGUILayout.EndHorizontal();

                    NataneToonShaderGUIUtility.DrawSeparator();

                    // Property select/deselect
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(L("全プロパティ選択", "Select All Props"), GUILayout.Height(20)))
                    {
                        for (int i = 0; i < propertySelection.Length; i++)
                        {
                            if (ShouldShowProperty(shaderProperties[i]))
                                propertySelection[i] = true;
                        }
                    }
                    if (GUILayout.Button(L("全プロパティ解除", "Deselect All Props"), GUILayout.Height(20)))
                    {
                        for (int i = 0; i < propertySelection.Length; i++)
                            propertySelection[i] = false;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(4);

                    // Property list
                    DrawPropertyEntries();
                });
        }

        private void DrawPropertyEntries()
        {
            // Get a reference material for current value preview
            Material refMaterial = null;
            for (int i = 0; i < scannedMaterials.Count; i++)
            {
                if (materialSelection[i])
                {
                    refMaterial = scannedMaterials[i].material;
                    break;
                }
            }

            for (int i = 0; i < shaderProperties.Count; i++)
            {
                var prop = shaderProperties[i];
                if (!ShouldShowProperty(prop)) continue;

                EditorGUILayout.BeginHorizontal();

                // Checkbox
                propertySelection[i] = EditorGUILayout.Toggle(propertySelection[i], GUILayout.Width(20));

                // Property name + description
                string displayLabel = string.IsNullOrEmpty(prop.description)
                    ? prop.name
                    : $"{prop.description} ({prop.name})";
                EditorGUILayout.LabelField(displayLabel, GUILayout.Width(250));

                // Current value preview (from first selected material)
                if (refMaterial != null && refMaterial.HasProperty(prop.name))
                {
                    DrawCurrentValuePreview(refMaterial, prop);
                }

                EditorGUILayout.EndHorizontal();

                // Edit control (only if selected)
                if (propertySelection[i])
                {
                    EditorGUI.indentLevel += 2;
                    DrawEditControl(prop);
                    EditorGUI.indentLevel -= 2;
                    EditorGUILayout.Space(2);
                }
            }
        }

        private void DrawCurrentValuePreview(Material mat, ShaderPropertyInfo prop)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                switch (prop.type)
                {
                    case ShaderUtil.ShaderPropertyType.Float:
                        EditorGUILayout.FloatField(mat.GetFloat(prop.name), GUILayout.Width(60));
                        break;
                    case ShaderUtil.ShaderPropertyType.Range:
                        EditorGUILayout.FloatField(mat.GetFloat(prop.name), GUILayout.Width(60));
                        break;
                    case ShaderUtil.ShaderPropertyType.Color:
                        EditorGUILayout.ColorField(mat.GetColor(prop.name), GUILayout.Width(60));
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        var v = mat.GetVector(prop.name);
                        EditorGUILayout.LabelField($"({v.x:F1},{v.y:F1},{v.z:F1},{v.w:F1})",
                            EditorStyles.miniLabel, GUILayout.Width(120));
                        break;
                }
            }
        }

        private void DrawEditControl(ShaderPropertyInfo prop)
        {
            switch (prop.type)
            {
                case ShaderUtil.ShaderPropertyType.Float:
                    if (!floatEditValues.ContainsKey(prop.name))
                        floatEditValues[prop.name] = adjustMode == AdjustMode.Multiply ? 1f : 0f;
                    floatEditValues[prop.name] = EditorGUILayout.FloatField(
                        L("値", "Value"), floatEditValues[prop.name]);
                    break;

                case ShaderUtil.ShaderPropertyType.Range:
                    if (!floatEditValues.ContainsKey(prop.name))
                        floatEditValues[prop.name] = adjustMode == AdjustMode.Multiply ? 1f : 0f;
                    if (adjustMode == AdjustMode.Set)
                    {
                        floatEditValues[prop.name] = EditorGUILayout.Slider(
                            L("値", "Value"), floatEditValues[prop.name],
                            prop.rangeMin, prop.rangeMax);
                    }
                    else
                    {
                        floatEditValues[prop.name] = EditorGUILayout.FloatField(
                            L("値", "Value"), floatEditValues[prop.name]);
                    }
                    break;

                case ShaderUtil.ShaderPropertyType.Color:
                    if (!colorEditValues.ContainsKey(prop.name))
                        colorEditValues[prop.name] = Color.white;
                    colorEditValues[prop.name] = EditorGUILayout.ColorField(
                        L("色", "Color"), colorEditValues[prop.name]);
                    break;

                case ShaderUtil.ShaderPropertyType.Vector:
                    if (!vectorEditValues.ContainsKey(prop.name))
                        vectorEditValues[prop.name] = Vector4.zero;
                    vectorEditValues[prop.name] = EditorGUILayout.Vector4Field(
                        L("ベクトル", "Vector"), vectorEditValues[prop.name]);
                    break;
            }
        }

        private bool ShouldShowProperty(ShaderPropertyInfo prop)
        {
            // Filter by type
            switch (propertyFilter)
            {
                case PropertyFilter.FloatRange:
                    if (prop.type != ShaderUtil.ShaderPropertyType.Float &&
                        prop.type != ShaderUtil.ShaderPropertyType.Range)
                        return false;
                    break;
                case PropertyFilter.Color:
                    if (prop.type != ShaderUtil.ShaderPropertyType.Color)
                        return false;
                    break;
                case PropertyFilter.Vector:
                    if (prop.type != ShaderUtil.ShaderPropertyType.Vector)
                        return false;
                    break;
            }

            // Filter by search text
            if (!string.IsNullOrEmpty(propertySearchFilter))
            {
                string lower = propertySearchFilter.ToLowerInvariant();
                if (!prop.name.ToLowerInvariant().Contains(lower) &&
                    !prop.description.ToLowerInvariant().Contains(lower))
                    return false;
            }

            return true;
        }

        // ========== Apply Section ==========
        private void DrawApplySection()
        {
            foldoutApply = NataneToonShaderGUIUtility.DrawFoldoutSection(
                L("適用", "Apply"),
                foldoutApply,
                () =>
                {
                    int selectedMatCount = materialSelection.Count(s => s);
                    int selectedPropCount = propertySelection.Count(s => s);

                    EditorGUILayout.HelpBox(
                        L($"対象: {selectedMatCount} マテリアル × {selectedPropCount} プロパティ\n" +
                          $"モード: {GetAdjustModeLabel()}",
                          $"Target: {selectedMatCount} material(s) x {selectedPropCount} property(ies)\n" +
                          $"Mode: {GetAdjustModeLabel()}"),
                        MessageType.Info);

                    EditorGUILayout.Space(4);

                    using (new EditorGUI.DisabledScope(selectedMatCount == 0 || selectedPropCount == 0))
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        if (NataneToonShaderGUIUtility.DrawPrimaryButton(
                            L("適用", "Apply"), 250, 32))
                        {
                            if (EditorUtility.DisplayDialog(
                                L("確認", "Confirm"),
                                L($"{selectedMatCount} 個のマテリアルの {selectedPropCount} 個のプロパティを変更します。\nよろしいですか？",
                                  $"Modify {selectedPropCount} property(ies) on {selectedMatCount} material(s).\nProceed?"),
                                L("適用", "Apply"),
                                L("キャンセル", "Cancel")))
                            {
                                ApplyChanges();
                            }
                        }

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                });
        }

        private string GetAdjustModeLabel()
        {
            switch (adjustMode)
            {
                case AdjustMode.Set: return L("設定 (Set)", "Set");
                case AdjustMode.Add: return L("加算 (Add)", "Add");
                case AdjustMode.Multiply: return L("乗算 (Multiply)", "Multiply");
                default: return "";
            }
        }

        // ========== Apply Logic ==========
        private void ApplyChanges()
        {
            int modifiedCount = 0;
            int skippedCount = 0;

            for (int mi = 0; mi < scannedMaterials.Count; mi++)
            {
                if (!materialSelection[mi]) continue;
                var mat = scannedMaterials[mi].material;

                Undo.RecordObject(mat, "Hierarchy Batch Edit");

                for (int pi = 0; pi < shaderProperties.Count; pi++)
                {
                    if (!propertySelection[pi]) continue;
                    var prop = shaderProperties[pi];

                    if (!mat.HasProperty(prop.name))
                    {
                        skippedCount++;
                        continue;
                    }

                    ApplyPropertyChange(mat, prop);
                    modifiedCount++;
                }

                EditorUtility.SetDirty(mat);
            }

            string message = L(
                $"完了: {modifiedCount} 件の変更を適用しました",
                $"Done: Applied {modifiedCount} change(s)");
            if (skippedCount > 0)
            {
                message += "\n" + L(
                    $"({skippedCount} 件はプロパティ未対応のためスキップ)",
                    $"({skippedCount} skipped due to missing properties)");
            }

            EditorUtility.DisplayDialog(
                L("適用完了", "Apply Complete"),
                message,
                "OK");

            Repaint();
        }

        private void ApplyPropertyChange(Material mat, ShaderPropertyInfo prop)
        {
            switch (prop.type)
            {
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    ApplyFloat(mat, prop);
                    break;
                case ShaderUtil.ShaderPropertyType.Color:
                    ApplyColor(mat, prop);
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    ApplyVector(mat, prop);
                    break;
            }
        }

        private void ApplyFloat(Material mat, ShaderPropertyInfo prop)
        {
            float editVal = floatEditValues.ContainsKey(prop.name) ? floatEditValues[prop.name] : 0f;
            float current = mat.GetFloat(prop.name);
            float newValue;

            switch (adjustMode)
            {
                case AdjustMode.Set:
                    newValue = editVal;
                    break;
                case AdjustMode.Add:
                    newValue = current + editVal;
                    break;
                case AdjustMode.Multiply:
                    newValue = current * editVal;
                    break;
                default:
                    return;
            }

            // Clamp for Range type
            if (prop.type == ShaderUtil.ShaderPropertyType.Range)
            {
                newValue = Mathf.Clamp(newValue, prop.rangeMin, prop.rangeMax);
            }

            mat.SetFloat(prop.name, newValue);
        }

        private void ApplyColor(Material mat, ShaderPropertyInfo prop)
        {
            Color editVal = colorEditValues.ContainsKey(prop.name) ? colorEditValues[prop.name] : Color.white;
            Color current = mat.GetColor(prop.name);
            Color newValue;

            switch (adjustMode)
            {
                case AdjustMode.Set:
                    newValue = editVal;
                    break;
                case AdjustMode.Add:
                    newValue = new Color(
                        current.r + editVal.r,
                        current.g + editVal.g,
                        current.b + editVal.b,
                        current.a + editVal.a);
                    break;
                case AdjustMode.Multiply:
                    newValue = current * editVal;
                    break;
                default:
                    return;
            }

            mat.SetColor(prop.name, newValue);
        }

        private void ApplyVector(Material mat, ShaderPropertyInfo prop)
        {
            Vector4 editVal = vectorEditValues.ContainsKey(prop.name) ? vectorEditValues[prop.name] : Vector4.zero;
            Vector4 current = mat.GetVector(prop.name);
            Vector4 newValue;

            switch (adjustMode)
            {
                case AdjustMode.Set:
                    newValue = editVal;
                    break;
                case AdjustMode.Add:
                    newValue = current + editVal;
                    break;
                case AdjustMode.Multiply:
                    newValue = new Vector4(
                        current.x * editVal.x,
                        current.y * editVal.y,
                        current.z * editVal.z,
                        current.w * editVal.w);
                    break;
                default:
                    return;
            }

            mat.SetVector(prop.name, newValue);
        }
    }
}
