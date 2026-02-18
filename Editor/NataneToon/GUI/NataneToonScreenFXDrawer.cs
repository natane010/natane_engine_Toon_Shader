using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Screen FX Overlay シェーダー用のDrawer
    /// NataneToonShaderGUIから委譲される
    /// </summary>
    public class NataneToonScreenFXDrawer : NataneToonShaderGUITab
    {
        private bool showBlend = true;
        private bool showToonize = true;
        private bool showDistortion = true;
        private bool showCinematic = true;

        private string prefsPrefix;

        public override void Initialize(MaterialEditor materialEditor, MaterialProperty[] properties, Material targetMaterial)
        {
            base.Initialize(materialEditor, properties, targetMaterial);
            prefsPrefix = NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "SFX_");
            LoadFoldoutStates();
        }

        public override void LoadFoldoutStates()
        {
            if (targetMaterial == null) return;
            showBlend = EditorPrefs.GetBool(prefsPrefix + "Blend", true);
            showToonize = EditorPrefs.GetBool(prefsPrefix + "Toonize", true);
            showDistortion = EditorPrefs.GetBool(prefsPrefix + "Distortion", true);
            showCinematic = EditorPrefs.GetBool(prefsPrefix + "Cinematic", true);
        }

        public override void SaveFoldoutStates()
        {
            if (targetMaterial == null) return;
            EditorPrefs.SetBool(prefsPrefix + "Blend", showBlend);
            EditorPrefs.SetBool(prefsPrefix + "Toonize", showToonize);
            EditorPrefs.SetBool(prefsPrefix + "Distortion", showDistortion);
            EditorPrefs.SetBool(prefsPrefix + "Cinematic", showCinematic);
        }

        public override void Draw()
        {
            DrawHeader();
            EditorGUILayout.Space(5);

            SafeDrawSection(() => DrawBlendSection(), "Blend");
            SafeDrawSection(() => DrawToonizeSection(), "Toonize");
            SafeDrawSection(() => DrawDistortionSection(), "Distortion");
            SafeDrawSection(() => DrawCinematicSection(), "Cinematic");

            EditorGUILayout.Space(10);
            DrawFooter();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.6f, 0.8f) }
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ナタネ スクリーンFXオーバーレイ", headerStyle);
            EditorGUILayout.EndVertical();
        }

        private void DrawBlendSection()
        {
            EditorGUI.BeginChangeCheck();
            showBlend = EditorGUILayout.Foldout(showBlend, "ブレンド / エフェクト基本", true, EditorStyles.foldoutHeader);
            if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

            if (showBlend)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_Intensity", "エフェクト強度");
                DrawProperty("_TintColor", "ティントカラー");
                DrawProperty("_Contrast", "コントラスト");
                DrawProperty("_Saturation", "彩度");
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Intensityで全体のエフェクト適用率を調整できます。0=オフ、1=フル適用。", MessageType.None);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawToonizeSection()
        {
            EditorGUI.BeginChangeCheck();
            showToonize = EditorGUILayout.Foldout(showToonize, "トゥーン化 / ポスタリゼーション", true, EditorStyles.foldoutHeader);
            if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

            if (showToonize)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_PosterizeStrength", "ポスタリゼーション強度");
                DrawProperty("_PosterizeSteps", "ポスタリゼーション段数");
                DrawProperty("_EdgeStrength", "エッジ暗化強度");
                DrawProperty("_EdgeThreshold", "エッジしきい値");
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("ポスタリゼーションで画面全体をトゥーン調にします。Steps数が少ないほど強い効果。", MessageType.None);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawDistortionSection()
        {
            EditorGUI.BeginChangeCheck();
            showDistortion = EditorGUILayout.Foldout(showDistortion, "画面歪み / 色収差", true, EditorStyles.foldoutHeader);
            if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

            if (showDistortion)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_ChromaticAberration", "色収差");
                DrawProperty("_AberrationScale", "収差スケール");
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("色収差でRGBの分離エフェクトを適用します。値を大きくするほど強い歪み。", MessageType.None);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawCinematicSection()
        {
            EditorGUI.BeginChangeCheck();
            showCinematic = EditorGUILayout.Foldout(showCinematic, "シネマティック", true, EditorStyles.foldoutHeader);
            if (EditorGUI.EndChangeCheck()) SaveFoldoutStates();

            if (showCinematic)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("ビネット", EditorStyles.miniBoldLabel);
                DrawProperty("_Vignette", "ビネット強度");
                DrawProperty("_VignetteSoftness", "ビネット柔らかさ");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("スキャンライン", EditorStyles.miniBoldLabel);
                DrawProperty("_ScanlineStrength", "スキャンライン強度");
                DrawProperty("_ScanlineDensity", "スキャンライン密度");
                DrawProperty("_ScanlineSpeed", "スキャンライン速度");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("フィルムグレイン", EditorStyles.miniBoldLabel);
                DrawProperty("_GrainStrength", "グレイン強度");
                DrawProperty("_GrainScale", "グレインスケール");
                DrawProperty("_GrainSpeed", "グレイン速度");

                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("ビネット=画面端の暗化、スキャンライン=CRTモニター風、グレイン=フィルムノイズ", MessageType.None);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle footerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            EditorGUILayout.LabelField("ナタネ スクリーンFXオーバーレイ v1.1", footerStyle);
            EditorGUILayout.LabelField("GrabPassを使用 - パフォーマンスに注意", footerStyle);
            EditorGUILayout.EndVertical();
        }
    }
}
