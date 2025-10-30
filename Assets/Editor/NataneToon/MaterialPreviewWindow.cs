using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Enhanced material preview window with lighting control
    /// Provides realtime 3D preview with adjustable lighting
    /// </summary>
    public class MaterialPreviewWindow : EditorWindow
    {
        private Material previewMaterial;
        private PreviewRenderUtility previewRenderUtility;
        private GameObject previewObject;
        private enum PreviewShape { Sphere, Cube, Cylinder, Plane, Torus }
        private PreviewShape currentShape = PreviewShape.Sphere;

        private Vector2 drag;
        private float zoom = 5f;

        // Lighting
        private Color ambientColor = Color.gray;
        private Color lightColor = Color.white;
        private float lightIntensity = 1f;
        private Vector2 lightRotation = new Vector2(50f, 50f);

        [MenuItem("Tools/Natane/Material Preview", false, 90)]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialPreviewWindow>("Material Preview");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            previewRenderUtility = new PreviewRenderUtility();
            previewRenderUtility.camera.transform.position = new Vector3(0, 0, -6);
            previewRenderUtility.camera.transform.rotation = Quaternion.identity;
            CreatePreviewObject();
        }

        private void OnDisable()
        {
            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
                previewRenderUtility = null;
            }
            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
            }
        }

        private void OnGUI()
        {
            DrawControls();
            DrawPreview();
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            previewMaterial = (Material)EditorGUILayout.ObjectField("Material", previewMaterial, typeof(Material), false);

            currentShape = (PreviewShape)EditorGUILayout.EnumPopup("Preview Shape", currentShape);
            if (GUI.changed) CreatePreviewObject();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);

            ambientColor = EditorGUILayout.ColorField("Ambient Color", ambientColor);
            lightColor = EditorGUILayout.ColorField("Light Color", lightColor);
            lightIntensity = EditorGUILayout.Slider("Light Intensity", lightIntensity, 0f, 2f);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Reset View")) ResetView();
            if (GUILayout.Button("Auto-Select Material") && Selection.activeObject is Material)
            {
                previewMaterial = Selection.activeObject as Material;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.HelpBox("Drag to rotate • Scroll to zoom", MessageType.Info);
        }

        private void DrawPreview()
        {
            if (previewMaterial == null || previewRenderUtility == null) return;

            Rect r = GUILayoutUtility.GetRect(10, 10000, 10, 10000);

            // Handle input
            if (Event.current.type == EventType.MouseDrag && r.Contains(Event.current.mousePosition))
            {
                drag += Event.current.delta * 0.5f;
                Event.current.Use();
                Repaint();
            }

            if (Event.current.type == EventType.ScrollWheel && r.Contains(Event.current.mousePosition))
            {
                zoom += Event.current.delta.y * 0.5f;
                zoom = Mathf.Clamp(zoom, 2f, 15f);
                Event.current.Use();
                Repaint();
            }

            // Setup lighting
            previewRenderUtility.lights[0].intensity = lightIntensity;
            previewRenderUtility.lights[0].color = lightColor;
            previewRenderUtility.ambientColor = ambientColor;

            // Position camera
            previewRenderUtility.camera.transform.position = Vector3.back * zoom;
            previewRenderUtility.camera.transform.LookAt(Vector3.zero);

            // Rotate object
            previewObject.transform.rotation = Quaternion.Euler(drag.y, -drag.x, 0);

            // Apply material
            var renderer = previewObject.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = previewMaterial;

            // Render
            previewRenderUtility.BeginPreview(r, GUIStyle.none);
            previewRenderUtility.DrawMesh(
                previewObject.GetComponent<MeshFilter>().sharedMesh,
                previewObject.transform.localToWorldMatrix,
                previewMaterial,
                0);
            previewRenderUtility.camera.Render();
            Texture resultRender = previewRenderUtility.EndPreview();

            GUI.DrawTexture(r, resultRender, ScaleMode.StretchToFill, false);
        }

        private void CreatePreviewObject()
        {
            if (previewObject != null) DestroyImmediate(previewObject);

            previewObject = GameObject.CreatePrimitive(GetPrimitiveType());
            previewObject.hideFlags = HideFlags.HideAndDontSave;
            previewRenderUtility.AddSingleGO(previewObject);
        }

        private PrimitiveType GetPrimitiveType()
        {
            switch (currentShape)
            {
                case PreviewShape.Sphere: return PrimitiveType.Sphere;
                case PreviewShape.Cube: return PrimitiveType.Cube;
                case PreviewShape.Cylinder: return PrimitiveType.Cylinder;
                case PreviewShape.Plane: return PrimitiveType.Plane;
                default: return PrimitiveType.Sphere;
            }
        }

        private void ResetView()
        {
            drag = Vector2.zero;
            zoom = 5f;
            lightRotation = new Vector2(50f, 50f);
            ambientColor = Color.gray;
            lightColor = Color.white;
            lightIntensity = 1f;
        }
    }
}
