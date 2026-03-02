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
    /// ヒエラルキー配下のNataneToonShader使用マテリアルを
    /// インスペクターと同一のUIで一括編集する。
    /// </summary>
    public class HierarchyMaterialBatchEditor : EditorWindow
    {
        // ========== Target Selection ==========
        private GameObject rootObject;
        private List<MaterialEntry> scannedMaterials = new List<MaterialEntry>();
        private bool[] materialSelection;

        // ========== Embedded Editor ==========
        private MaterialEditor embeddedEditor;
        private bool foldoutEditor = true;

        // ========== UI State ==========
        private Vector2 scrollPosition;
        private bool foldoutMaterials = true;

        // ========== Structs ==========
        private struct MaterialEntry
        {
            public Material material;
            public string hierarchyPath;
        }

        [MenuItem("Tools/Natane/マテリアル Material/ヒエラルキー一括編集 Hierarchy Batch Editor", false, 15)]
        public static void ShowWindow()
        {
            var window = GetWindow<HierarchyMaterialBatchEditor>(
                L("ヒエラルキー一括編集", "Hierarchy Batch Editor"));
            window.minSize = new Vector2(550, 600);
            window.Show();
        }

        private void OnDisable()
        {
            CleanupEmbeddedEditor();
        }

        private void CleanupEmbeddedEditor()
        {
            if (embeddedEditor != null)
            {
                DestroyImmediate(embeddedEditor);
                embeddedEditor = null;
            }
        }

        private void RebuildMaterialEditor()
        {
            CleanupEmbeddedEditor();

            var selected = new List<Material>();
            for (int i = 0; i < scannedMaterials.Count; i++)
            {
                if (materialSelection != null && i < materialSelection.Length && materialSelection[i])
                    selected.Add(scannedMaterials[i].material);
            }

            if (selected.Count > 0)
            {
                embeddedEditor = (MaterialEditor)UnityEditor.Editor.CreateEditor(
                    selected.ToArray(), typeof(MaterialEditor));
            }
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
                DrawMaterialInspector();
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
                CleanupEmbeddedEditor();
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
            CleanupEmbeddedEditor();

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

            // Build embedded editor from all selected materials
            RebuildMaterialEditor();
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
                        RebuildMaterialEditor();
                    }
                    if (GUILayout.Button(L("全て解除", "Deselect All"), GUILayout.Height(22)))
                    {
                        for (int i = 0; i < materialSelection.Length; i++)
                            materialSelection[i] = false;
                        RebuildMaterialEditor();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2);

                    // Material entries
                    for (int i = 0; i < scannedMaterials.Count; i++)
                    {
                        var entry = scannedMaterials[i];
                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.BeginChangeCheck();
                        materialSelection[i] = EditorGUILayout.Toggle(materialSelection[i], GUILayout.Width(20));
                        if (EditorGUI.EndChangeCheck())
                        {
                            RebuildMaterialEditor();
                        }

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

        // ========== Material Inspector (Embedded) ==========
        private void DrawMaterialInspector()
        {
            foldoutEditor = NataneToonShaderGUIUtility.DrawFoldoutSection(
                L("マテリアルエディタ", "Material Editor"),
                foldoutEditor,
                () =>
                {
                    int selectedCount = materialSelection != null ? materialSelection.Count(s => s) : 0;

                    if (selectedCount == 0)
                    {
                        EditorGUILayout.HelpBox(
                            L("マテリアルを選択してください",
                              "Please select material(s) to edit"),
                            MessageType.Info);
                        return;
                    }

                    // Rebuild if needed (e.g. after domain reload)
                    if (embeddedEditor == null)
                    {
                        RebuildMaterialEditor();
                    }

                    if (embeddedEditor == null) return;

                    EditorGUILayout.HelpBox(
                        L($"{selectedCount} 個のマテリアルを同時編集中（変更はリアルタイムで反映されます）",
                          $"Editing {selectedCount} material(s) simultaneously (changes apply in real-time)"),
                        MessageType.Info);

                    EditorGUILayout.Space(4);

                    // Draw the material header (preview sphere)
                    embeddedEditor.DrawHeader();

                    // Draw the full NataneToonShaderGUI inspector
                    embeddedEditor.OnInspectorGUI();
                });
        }
    }
}
