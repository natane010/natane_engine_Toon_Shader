using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// VRC Light Volumes Integration Helper
    /// VRC Light Volumes統合ヘルパー
    /// </summary>
    public class VRCLightVolumesHelper : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;

        private enum LightVolumeQuality { Low, Medium, High, Ultra }
        private LightVolumeQuality quality = LightVolumeQuality.Medium;

        [MenuItem("Tools/Natane/VRC Light Volumes Helper", false, 134)]
        public static void ShowWindow()
        {
            var window = GetWindow<VRCLightVolumesHelper>("VRC Light Volumes");
            window.minSize = new Vector2(500, 450);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("VRC Light Volumes統合ヘルパー Helper", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("VRChat Light Volumesの設定を簡単に\nEasy setup for VRChat Light Volumes", MessageType.Info);
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField("ターゲット Target", targetMaterial, typeof(Material), false);

            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox("マテリアルを選択してください\nSelect a material", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawQuickSetup();
            EditorGUILayout.Space(10);
            DrawDetailedSettings();
            EditorGUILayout.Space(10);
            DrawTesting();

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuickSetup()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("クイック設定 Quick Setup", EditorStyles.boldLabel);

            quality = (LightVolumeQuality)EditorGUILayout.EnumPopup("品質プリセット Quality Preset", quality);

            if (GUILayout.Button("プリセットを適用 Apply Preset", GUILayout.Height(30)))
            {
                ApplyQualityPreset(quality);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailedSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("詳細設定 Detailed Settings", EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_UseLightVolume"))
            {
                EditorGUI.BeginChangeCheck();
                bool useLV = targetMaterial.GetFloat("_UseLightVolume") > 0.5f;
                useLV = EditorGUILayout.Toggle("Light Volumeを使用 Use", useLV);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Toggle Light Volume");
                    targetMaterial.SetFloat("_UseLightVolume", useLV ? 1f : 0f);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_LightVolumeIntensity"))
            {
                EditorGUI.BeginChangeCheck();
                float intensity = EditorGUILayout.Slider("強度 Intensity", targetMaterial.GetFloat("_LightVolumeIntensity"), 0f, 2f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change LV Intensity");
                    targetMaterial.SetFloat("_LightVolumeIntensity", intensity);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_LightVolumeFalloff"))
            {
                EditorGUI.BeginChangeCheck();
                float falloff = EditorGUILayout.Slider("減衰 Falloff", targetMaterial.GetFloat("_LightVolumeFalloff"), 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change LV Falloff");
                    targetMaterial.SetFloat("_LightVolumeFalloff", falloff);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTesting()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("テスト Test", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "VRChatワールドでLight Volumesが有効な場所でテストしてください\n" +
                "Test in VRChat world where Light Volumes are enabled",
                MessageType.Info);

            if (GUILayout.Button("デバッグモードを有効化 Enable Debug Mode"))
            {
                if (targetMaterial.HasProperty("_LightVolumeDebug"))
                {
                    targetMaterial.SetFloat("_LightVolumeDebug", 1f);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyQualityPreset(LightVolumeQuality preset)
        {
            Undo.RecordObject(targetMaterial, "Apply LV Preset");

            if (targetMaterial.HasProperty("_UseLightVolume"))
                targetMaterial.SetFloat("_UseLightVolume", 1f);

            switch (preset)
            {
                case LightVolumeQuality.Low:
                    if (targetMaterial.HasProperty("_LightVolumeIntensity"))
                        targetMaterial.SetFloat("_LightVolumeIntensity", 0.5f);
                    if (targetMaterial.HasProperty("_LightVolumeFalloff"))
                        targetMaterial.SetFloat("_LightVolumeFalloff", 0.8f);
                    break;

                case LightVolumeQuality.Medium:
                    if (targetMaterial.HasProperty("_LightVolumeIntensity"))
                        targetMaterial.SetFloat("_LightVolumeIntensity", 1f);
                    if (targetMaterial.HasProperty("_LightVolumeFalloff"))
                        targetMaterial.SetFloat("_LightVolumeFalloff", 0.5f);
                    break;

                case LightVolumeQuality.High:
                    if (targetMaterial.HasProperty("_LightVolumeIntensity"))
                        targetMaterial.SetFloat("_LightVolumeIntensity", 1.5f);
                    if (targetMaterial.HasProperty("_LightVolumeFalloff"))
                        targetMaterial.SetFloat("_LightVolumeFalloff", 0.3f);
                    break;

                case LightVolumeQuality.Ultra:
                    if (targetMaterial.HasProperty("_LightVolumeIntensity"))
                        targetMaterial.SetFloat("_LightVolumeIntensity", 2f);
                    if (targetMaterial.HasProperty("_LightVolumeFalloff"))
                        targetMaterial.SetFloat("_LightVolumeFalloff", 0.1f);
                    break;
            }

            EditorUtility.SetDirty(targetMaterial);
            EditorUtility.DisplayDialog("適用完了 Applied", $"{preset}プリセットを適用しました\nApplied {preset} preset", "OK");
        }
    }
}
