using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Rim Light Direction Visualizer
    /// リムライト方向ビジュアライザー
    /// </summary>
    public class RimLightDirectionVisualizer : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;
        private Vector3 rimDirection = new Vector3(0, 1, 0);

        [MenuItem("Tools/Natane/Rim Light Direction Visualizer", false, 136)]
        public static void ShowWindow()
        {
            var window = GetWindow<RimLightDirectionVisualizer>("リムライト方向 Rim Light");
            window.minSize = new Vector2(500, 450);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("リムライト方向ビジュアライザー Rim Light Direction Visualizer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("リムライトの方向を視覚的に調整\nVisually adjust rim light direction", MessageType.Info);
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField("ターゲット Target", targetMaterial, typeof(Material), false);

            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox("マテリアルを選択してください\nSelect a material", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawRimSettings();
            EditorGUILayout.Space(10);
            DrawDirectionControl();
            EditorGUILayout.Space(10);
            DrawPresets();

            EditorGUILayout.EndScrollView();
        }

        private void DrawRimSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("リムライト設定 Rim Light Settings", EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_UseRimLight"))
            {
                EditorGUI.BeginChangeCheck();
                bool useRim = targetMaterial.GetFloat("_UseRimLight") > 0.5f;
                useRim = EditorGUILayout.Toggle("リムライト有効 Enable", useRim);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Toggle Rim Light");
                    targetMaterial.SetFloat("_UseRimLight", useRim ? 1f : 0f);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_RimColor"))
            {
                EditorGUI.BeginChangeCheck();
                Color color = EditorGUILayout.ColorField("色 Color", targetMaterial.GetColor("_RimColor"));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Rim Color");
                    targetMaterial.SetColor("_RimColor", color);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_RimIntensity"))
            {
                EditorGUI.BeginChangeCheck();
                float intensity = EditorGUILayout.Slider("強度 Intensity", targetMaterial.GetFloat("_RimIntensity"), 0f, 2f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Rim Intensity");
                    targetMaterial.SetFloat("_RimIntensity", intensity);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_RimPower"))
            {
                EditorGUI.BeginChangeCheck();
                float power = EditorGUILayout.Slider("パワー Power", targetMaterial.GetFloat("_RimPower"), 0.1f, 10f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Rim Power");
                    targetMaterial.SetFloat("_RimPower", power);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDirectionControl()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("方向制御 Direction Control", EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_RimDirection"))
            {
                EditorGUI.BeginChangeCheck();
                Vector4 dir = targetMaterial.GetVector("_RimDirection");
                rimDirection = new Vector3(dir.x, dir.y, dir.z);
                rimDirection = EditorGUILayout.Vector3Field("方向 Direction", rimDirection);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Rim Direction");
                    targetMaterial.SetVector("_RimDirection", new Vector4(rimDirection.x, rimDirection.y, rimDirection.z, 0));
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            // Spherical coordinates for easier control
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("簡易コントロール Easy Control", EditorStyles.miniBoldLabel);

            float azimuth = Mathf.Atan2(rimDirection.x, rimDirection.z) * Mathf.Rad2Deg;
            float elevation = Mathf.Asin(rimDirection.y / rimDirection.magnitude) * Mathf.Rad2Deg;

            EditorGUI.BeginChangeCheck();
            azimuth = EditorGUILayout.Slider("方位角 Azimuth", azimuth, -180f, 180f);
            elevation = EditorGUILayout.Slider("仰角 Elevation", elevation, -90f, 90f);
            if (EditorGUI.EndChangeCheck())
            {
                float azimuthRad = azimuth * Mathf.Deg2Rad;
                float elevationRad = elevation * Mathf.Deg2Rad;
                rimDirection = new Vector3(
                    Mathf.Sin(azimuthRad) * Mathf.Cos(elevationRad),
                    Mathf.Sin(elevationRad),
                    Mathf.Cos(azimuthRad) * Mathf.Cos(elevationRad)
                ).normalized;

                if (targetMaterial.HasProperty("_RimDirection"))
                {
                    Undo.RecordObject(targetMaterial, "Change Rim Direction");
                    targetMaterial.SetVector("_RimDirection", new Vector4(rimDirection.x, rimDirection.y, rimDirection.z, 0));
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPresets()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("方向プリセット Direction Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("上 Top"))
                ApplyDirectionPreset(new Vector3(0, 1, 0));
            if (GUILayout.Button("下 Bottom"))
                ApplyDirectionPreset(new Vector3(0, -1, 0));
            if (GUILayout.Button("左 Left"))
                ApplyDirectionPreset(new Vector3(-1, 0, 0));
            if (GUILayout.Button("右 Right"))
                ApplyDirectionPreset(new Vector3(1, 0, 0));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("前 Front"))
                ApplyDirectionPreset(new Vector3(0, 0, 1));
            if (GUILayout.Button("後 Back"))
                ApplyDirectionPreset(new Vector3(0, 0, -1));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplyDirectionPreset(Vector3 direction)
        {
            rimDirection = direction.normalized;
            if (targetMaterial.HasProperty("_RimDirection"))
            {
                Undo.RecordObject(targetMaterial, "Apply Rim Direction Preset");
                targetMaterial.SetVector("_RimDirection", new Vector4(rimDirection.x, rimDirection.y, rimDirection.z, 0));
                EditorUtility.SetDirty(targetMaterial);
            }
        }
    }
}
