using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Visual ramp/gradient texture editor for toon shadow control.
    /// トゥーン影制御用ビジュアルランプ/グラデーションテクスチャエディタ
    /// </summary>
    internal class RampTextureEditor : EditorWindow
    {
        private Gradient gradient;
        private Texture2D previewTexture;
        private int textureWidth = 256;
        private int textureHeight = 16;
        private bool horizontal = true;
        private int steps; // 0 = smooth, 2+ = stepped
        private Material targetMaterial;
        private string targetProperty = "_ShadowRamp";

        // Presets
        private static readonly GradientColorKey[][] presetKeys = new[]
        {
            // Soft 2-tone
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.6f, 0.7f), 0.5f), new GradientColorKey(new Color(0.3f, 0.3f, 0.4f), 1f) },
            // Hard 2-tone (anime)
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 0.49f), new GradientColorKey(new Color(0.5f, 0.5f, 0.6f), 0.51f), new GradientColorKey(new Color(0.5f, 0.5f, 0.6f), 1f) },
            // 3-tone
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 0.3f), new GradientColorKey(new Color(0.7f, 0.7f, 0.75f), 0.35f), new GradientColorKey(new Color(0.7f, 0.7f, 0.75f), 0.65f), new GradientColorKey(new Color(0.35f, 0.35f, 0.45f), 0.7f), new GradientColorKey(new Color(0.35f, 0.35f, 0.45f), 1f) },
            // Warm skin
            new[] { new GradientColorKey(new Color(1f, 0.95f, 0.9f), 0f), new GradientColorKey(new Color(0.9f, 0.6f, 0.5f), 0.5f), new GradientColorKey(new Color(0.5f, 0.25f, 0.25f), 1f) },
        };
        private static readonly string[] presetNames = { "Soft 2-tone", "Hard Anime", "3-tone", "Warm Skin" };

        [MenuItem("Tools/Natane/Ramp Texture Editor", false, 150)]
        public static void Open()
        {
            var w = GetWindow<RampTextureEditor>(true, "Ramp Texture Editor");
            w.minSize = new Vector2(400, 300);
            w.ShowUtility();
        }

        public static void Open(Material mat, string property)
        {
            var w = GetWindow<RampTextureEditor>(true, "Ramp Texture Editor");
            w.targetMaterial = mat;
            w.targetProperty = property;
            w.minSize = new Vector2(400, 300);
            w.ShowUtility();
        }

        private void OnEnable()
        {
            if (gradient == null)
            {
                gradient = new Gradient();
                gradient.colorKeys = presetKeys[0];
                gradient.alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
            }
            UpdatePreview();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(L("ランプテクスチャエディタ", "Ramp Texture Editor"),
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            EditorGUILayout.Space(4);

            // Presets
            EditorGUILayout.LabelField(L("プリセット", "Presets"), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < presetNames.Length; i++)
            {
                if (GUILayout.Button(presetNames[i], EditorStyles.miniButton))
                {
                    gradient.colorKeys = presetKeys[i];
                    UpdatePreview();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            // Gradient editor
            EditorGUILayout.LabelField(L("グラデーション", "Gradient"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            gradient = EditorGUILayout.GradientField(L("カラーランプ", "Color Ramp"), gradient);
            if (EditorGUI.EndChangeCheck())
                UpdatePreview();

            EditorGUILayout.Space(4);

            // Settings
            EditorGUILayout.BeginHorizontal();
            textureWidth = EditorGUILayout.IntPopup(L("幅", "Width"), textureWidth,
                new[] { "64", "128", "256", "512" }, new[] { 64, 128, 256, 512 }, GUILayout.Width(220));
            steps = EditorGUILayout.IntSlider(L("段階", "Steps"), steps, 0, 16);
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                UpdatePreview();

            EditorGUILayout.Space(4);

            // Preview
            if (previewTexture != null)
            {
                Rect previewRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(previewRect, previewTexture);
            }

            EditorGUILayout.Space(8);

            // Target material
            targetMaterial = (Material)EditorGUILayout.ObjectField(
                L("ターゲットマテリアル", "Target Material"), targetMaterial, typeof(Material), false);
            targetProperty = EditorGUILayout.TextField(L("プロパティ", "Property"), targetProperty);

            EditorGUILayout.Space(4);

            // Actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("マテリアルに適用", "Apply to Material"), GUILayout.Height(30)))
                ApplyToMaterial();
            if (GUILayout.Button(L("PNGで保存", "Save as PNG"), GUILayout.Height(30)))
                SaveAsPNG();
            EditorGUILayout.EndHorizontal();
        }

        private void UpdatePreview()
        {
            if (previewTexture != null) DestroyImmediate(previewTexture);
            previewTexture = GenerateRampTexture(textureWidth, textureHeight, gradient, steps);
            Repaint();
        }

        public static Texture2D GenerateRampTexture(int width, int height, Gradient grad, int stepCount)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = stepCount > 0 ? FilterMode.Point : FilterMode.Bilinear;

            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                if (stepCount > 0)
                    t = Mathf.Floor(t * stepCount) / stepCount;
                Color c = grad.Evaluate(t);
                for (int y = 0; y < height; y++)
                    tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private void ApplyToMaterial()
        {
            if (targetMaterial == null || previewTexture == null) return;
            if (!targetMaterial.HasProperty(targetProperty)) return;
            Undo.RecordObject(targetMaterial, "Apply Ramp Texture");
            // Create asset copy
            var copy = new Texture2D(previewTexture.width, previewTexture.height, TextureFormat.RGBA32, false);
            copy.SetPixels(previewTexture.GetPixels());
            copy.Apply();
            copy.wrapMode = TextureWrapMode.Clamp;
            copy.filterMode = steps > 0 ? FilterMode.Point : FilterMode.Bilinear;
            targetMaterial.SetTexture(targetProperty, copy);
            EditorUtility.SetDirty(targetMaterial);
        }

        private void SaveAsPNG()
        {
            if (previewTexture == null) return;
            string path = EditorUtility.SaveFilePanel("Save Ramp Texture", "Assets", "ramp", "png");
            if (string.IsNullOrEmpty(path)) return;
            System.IO.File.WriteAllBytes(path, previewTexture.EncodeToPNG());
            AssetDatabase.Refresh();
        }

        private void OnDestroy()
        {
            if (previewTexture != null) DestroyImmediate(previewTexture);
        }
    }
}
