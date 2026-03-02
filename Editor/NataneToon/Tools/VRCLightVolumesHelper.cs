using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// VRC Light Volumes Integration Helper
    /// </summary>
    public class VRCLightVolumesHelper : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;

        private enum LightVolumeQuality
        {
            Low,
            Medium,
            High,
            Ultra
        }

        private LightVolumeQuality quality = LightVolumeQuality.Medium;

        private string[] BlendModeNames => new[]
        {
            L("加算", "Add"),
            L("乗算", "Multiply"),
            L("置換", "Replace"),
            L("自然", "Natural")
        };

        [MenuItem("Tools/Natane/VRChat/VRCライトボリュームヘルパー VRC Light Volumes Helper", false, 61)]
        public static void ShowWindow()
        {
            var window = GetWindow<VRCLightVolumesHelper>(L("VRCライトボリュームヘルパー", "VRC Light Volumes Helper"));
            window.minSize = new Vector2(500, 460);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            NataneToonShaderGUIUtility.DrawHeaderWithHelp("VRC Light Volumes ヘルパー", "VRC Light Volumes Helper", "VRCLightVolumes");
            EditorGUILayout.LabelField(L("現行ShaderのLight Volume設定を調整します。", "Adjust Light Volume settings for the current shader."), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField(L("ターゲット", "Target"), targetMaterial, typeof(Material), false);
            if (targetMaterial == null)
            {
                EditorGUILayout.HelpBox(L("マテリアルを選択してください", "Select a material"), MessageType.Info);
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
            EditorGUILayout.LabelField(L("クイック設定", "Quick Setup"), EditorStyles.boldLabel);

            quality = (LightVolumeQuality)EditorGUILayout.EnumPopup(L("品質プリセット", "Quality Preset"), quality);
            if (GUILayout.Button(L("プリセットを適用", "Apply Preset"), GUILayout.Height(30)))
            {
                ApplyQualityPreset(quality);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailedSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("詳細設定", "Detailed Settings"), EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_UseLightVolume"))
            {
                EditorGUI.BeginChangeCheck();
                bool useLV = targetMaterial.GetFloat("_UseLightVolume") > 0.5f;
                useLV = EditorGUILayout.Toggle(L("Light Volumeを使用", "Use Light Volume"), useLV);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Toggle Light Volume");
                    SetLightVolumeEnabled(useLV);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_LightVolumeIntensity"))
            {
                EditorGUI.BeginChangeCheck();
                float intensity = EditorGUILayout.Slider(L("強度", "Intensity"), targetMaterial.GetFloat("_LightVolumeIntensity"), 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change LV Intensity");
                    targetMaterial.SetFloat("_LightVolumeIntensity", intensity);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_LightVolumeBlendMode"))
            {
                EditorGUI.BeginChangeCheck();
                int blendMode = (int)targetMaterial.GetFloat("_LightVolumeBlendMode");
                blendMode = EditorGUILayout.Popup(L("ブレンドモード", "Blend Mode"), blendMode, BlendModeNames);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change LV Blend Mode");
                    targetMaterial.SetFloat("_LightVolumeBlendMode", blendMode);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_LightVolumeBlend"))
            {
                EditorGUI.BeginChangeCheck();
                float blend = EditorGUILayout.Slider(L("ブレンド", "Blend"), targetMaterial.GetFloat("_LightVolumeBlend"), 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change LV Blend");
                    targetMaterial.SetFloat("_LightVolumeBlend", blend);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_LightVolumeSpecular"))
            {
                EditorGUI.BeginChangeCheck();
                bool specular = targetMaterial.GetFloat("_LightVolumeSpecular") > 0.5f;
                specular = EditorGUILayout.Toggle(L("スペキュラー連動", "Specular"), specular);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Toggle LV Specular");
                    targetMaterial.SetFloat("_LightVolumeSpecular", specular ? 1f : 0f);
                    if (specular)
                    {
                        targetMaterial.EnableKeyword("_LIGHT_VOLUME_SPECULAR");
                    }
                    else
                    {
                        targetMaterial.DisableKeyword("_LIGHT_VOLUME_SPECULAR");
                    }
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTesting()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("確認", "Check"), EditorStyles.boldLabel);

            bool useLVKeyword = targetMaterial.IsKeywordEnabled("_USE_LIGHT_VOLUME");
            bool useLVProp = targetMaterial.HasProperty("_UseLightVolume") && targetMaterial.GetFloat("_UseLightVolume") > 0.5f;
            bool lvSpecKeyword = targetMaterial.IsKeywordEnabled("_LIGHT_VOLUME_SPECULAR");

            EditorGUILayout.HelpBox(
                L($"UseLightVolume: {(useLVProp ? "ON" : "OFF")}, Keyword: {(useLVKeyword ? "ON" : "OFF")}, Specular Keyword: {(lvSpecKeyword ? "ON" : "OFF")}",
                  $"UseLightVolume: {(useLVProp ? "ON" : "OFF")}, Keyword: {(useLVKeyword ? "ON" : "OFF")}, Specular Keyword: {(lvSpecKeyword ? "ON" : "OFF")}"),
                MessageType.Info);

            if (GUILayout.Button(L("マテリアルを選択", "Ping Material"), GUILayout.Height(24)))
            {
                EditorGUIUtility.PingObject(targetMaterial);
                Selection.activeObject = targetMaterial;
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyQualityPreset(LightVolumeQuality preset)
        {
            Undo.RecordObject(targetMaterial, "Apply LV Preset");
            SetLightVolumeEnabled(true);

            switch (preset)
            {
                case LightVolumeQuality.Low:
                    SetFloatIfExists("_LightVolumeIntensity", 0.5f);
                    SetFloatIfExists("_LightVolumeBlend", 0.7f);
                    SetFloatIfExists("_LightVolumeBlendMode", 1f);
                    SetSpecular(false);
                    break;

                case LightVolumeQuality.Medium:
                    SetFloatIfExists("_LightVolumeIntensity", 0.8f);
                    SetFloatIfExists("_LightVolumeBlend", 0.9f);
                    SetFloatIfExists("_LightVolumeBlendMode", 3f);
                    SetSpecular(false);
                    break;

                case LightVolumeQuality.High:
                    SetFloatIfExists("_LightVolumeIntensity", 1.0f);
                    SetFloatIfExists("_LightVolumeBlend", 1.0f);
                    SetFloatIfExists("_LightVolumeBlendMode", 3f);
                    SetSpecular(true);
                    break;

                case LightVolumeQuality.Ultra:
                    SetFloatIfExists("_LightVolumeIntensity", 1.0f);
                    SetFloatIfExists("_LightVolumeBlend", 1.0f);
                    SetFloatIfExists("_LightVolumeBlendMode", 0f);
                    SetSpecular(true);
                    break;
            }

            EditorUtility.SetDirty(targetMaterial);
            EditorUtility.DisplayDialog(L("適用完了", "Applied"), L($"{preset}プリセットを適用しました", $"Applied {preset} preset"), "OK");
        }

        private void SetLightVolumeEnabled(bool enabled)
        {
            if (targetMaterial.HasProperty("_UseLightVolume"))
            {
                targetMaterial.SetFloat("_UseLightVolume", enabled ? 1f : 0f);
            }

            if (enabled)
            {
                targetMaterial.EnableKeyword("_USE_LIGHT_VOLUME");
            }
            else
            {
                targetMaterial.DisableKeyword("_USE_LIGHT_VOLUME");
            }
        }

        private void SetSpecular(bool enabled)
        {
            if (targetMaterial.HasProperty("_LightVolumeSpecular"))
            {
                targetMaterial.SetFloat("_LightVolumeSpecular", enabled ? 1f : 0f);
            }

            if (enabled)
            {
                targetMaterial.EnableKeyword("_LIGHT_VOLUME_SPECULAR");
            }
            else
            {
                targetMaterial.DisableKeyword("_LIGHT_VOLUME_SPECULAR");
            }
        }

        private void SetFloatIfExists(string propertyName, float value)
        {
            if (targetMaterial.HasProperty(propertyName))
            {
                targetMaterial.SetFloat(propertyName, value);
            }
        }
    }
}
