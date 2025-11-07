using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Outline Optimization Tool
    /// アウトライン最適化ツール
    /// </summary>
    public class OutlineOptimizer : EditorWindow
    {
        private Material targetMaterial;
        private GameObject targetObject;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Natane/Outline Optimizer", false, 135)]
        public static void ShowWindow()
        {
            var window = GetWindow<OutlineOptimizer>("アウトライン最適化 Outline");
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("アウトライン最適化ツール Outline Optimizer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("アウトライン幅を自動最適化\nAuto-optimize outline width", MessageType.Info);
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField("マテリアル Material", targetMaterial, typeof(Material), false);
            targetObject = (GameObject)EditorGUILayout.ObjectField("オブジェクト Object", targetObject, typeof(GameObject), true);

            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox("マテリアルを選択してください\nSelect a material", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawOutlineSettings();
            EditorGUILayout.Space(10);
            DrawAutoOptimization();
            EditorGUILayout.Space(10);
            DrawPresets();

            EditorGUILayout.EndScrollView();
        }

        private void DrawOutlineSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("アウトライン設定 Outline Settings", EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_UseOutline"))
            {
                EditorGUI.BeginChangeCheck();
                bool useOutline = targetMaterial.GetFloat("_UseOutline") > 0.5f;
                useOutline = EditorGUILayout.Toggle("アウトライン有効 Enable", useOutline);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Toggle Outline");
                    targetMaterial.SetFloat("_UseOutline", useOutline ? 1f : 0f);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_OutlineWidth"))
            {
                EditorGUI.BeginChangeCheck();
                float width = EditorGUILayout.Slider("幅 Width", targetMaterial.GetFloat("_OutlineWidth"), 0f, 0.1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Outline Width");
                    targetMaterial.SetFloat("_OutlineWidth", width);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_OutlineColor"))
            {
                EditorGUI.BeginChangeCheck();
                Color color = EditorGUILayout.ColorField("色 Color", targetMaterial.GetColor("_OutlineColor"));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Outline Color");
                    targetMaterial.SetColor("_OutlineColor", color);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAutoOptimization()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("自動最適化 Auto Optimization", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "メッシュサイズに基づいてアウトライン幅を自動調整\n" +
                "Auto-adjust outline width based on mesh size",
                MessageType.Info);

            if (targetObject != null && GUILayout.Button("幅を自動最適化 Auto-Optimize Width", GUILayout.Height(30)))
            {
                OptimizeOutlineWidth();
            }

            if (GUILayout.Button("アウトラインをクリア Clear Outline", GUILayout.Height(25)))
            {
                if (targetMaterial.HasProperty("_UseOutline"))
                    targetMaterial.SetFloat("_UseOutline", 0f);
                EditorUtility.SetDirty(targetMaterial);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPresets()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("プリセット Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("細い Thin"))
                ApplyPreset(0.001f, Color.black);
            if (GUILayout.Button("標準 Normal"))
                ApplyPreset(0.003f, Color.black);
            if (GUILayout.Button("太い Thick"))
                ApplyPreset(0.006f, Color.black);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void OptimizeOutlineWidth()
        {
            if (targetObject == null) return;

            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("エラー Error", "MeshFilterが見つかりません\nMeshFilter not found", "OK");
                return;
            }

            Bounds bounds = meshFilter.sharedMesh.bounds;
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float optimalWidth = Mathf.Clamp(size * 0.01f, 0.001f, 0.01f);

            Undo.RecordObject(targetMaterial, "Optimize Outline");
            if (targetMaterial.HasProperty("_OutlineWidth"))
                targetMaterial.SetFloat("_OutlineWidth", optimalWidth);
            EditorUtility.SetDirty(targetMaterial);

            EditorUtility.DisplayDialog(
                "最適化完了 Optimized",
                $"最適な幅を設定しました: {optimalWidth:F4}\nSet optimal width: {optimalWidth:F4}",
                "OK");
        }

        private void ApplyPreset(float width, Color color)
        {
            Undo.RecordObject(targetMaterial, "Apply Outline Preset");
            if (targetMaterial.HasProperty("_UseOutline"))
                targetMaterial.SetFloat("_UseOutline", 1f);
            if (targetMaterial.HasProperty("_OutlineWidth"))
                targetMaterial.SetFloat("_OutlineWidth", width);
            if (targetMaterial.HasProperty("_OutlineColor"))
                targetMaterial.SetColor("_OutlineColor", color);
            EditorUtility.SetDirty(targetMaterial);
        }
    }
}
