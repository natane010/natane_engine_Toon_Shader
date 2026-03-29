using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Procedural MatCap texture generator.
    /// プロシージャルMatCapテクスチャジェネレータ
    /// </summary>
    internal class MatCapGenerator : EditorWindow
    {
        private int textureSize = 256;
        private Color baseColor = new Color(0.8f, 0.8f, 0.85f);
        private Color shadowColor = new Color(0.3f, 0.3f, 0.4f);
        private Color highlightColor = Color.white;
        private float highlightPower = 3f;
        private float highlightIntensity = 0.8f;
        private float rimPower = 2f;
        private Color rimColor = new Color(0.5f, 0.5f, 0.6f, 0.5f);
        private float shadowSoftness = 0.5f;
        private Texture2D preview;
        private Material targetMaterial;
        private string targetProperty = "_MatCapTex";

        [MenuItem("Tools/Natane/MatCap Generator", false, 151)]
        public static void Open()
        {
            var w = GetWindow<MatCapGenerator>(true, "MatCap Generator");
            w.minSize = new Vector2(380, 480);
            w.ShowUtility();
        }

        private void OnEnable() { UpdatePreview(); }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(L("MatCap ジェネレータ", "MatCap Generator"),
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });

            EditorGUI.BeginChangeCheck();

            textureSize = EditorGUILayout.IntPopup(L("サイズ", "Size"), textureSize,
                new[] { "128", "256", "512" }, new[] { 128, 256, 512 });
            baseColor = EditorGUILayout.ColorField(L("ベースカラー", "Base Color"), baseColor);
            shadowColor = EditorGUILayout.ColorField(L("シャドウカラー", "Shadow Color"), shadowColor);
            shadowSoftness = EditorGUILayout.Slider(L("影ソフト", "Shadow Soft"), shadowSoftness, 0f, 1f);
            highlightColor = EditorGUILayout.ColorField(L("ハイライトカラー", "Highlight"), highlightColor);
            highlightPower = EditorGUILayout.Slider(L("ハイライト強度", "Highlight Power"), highlightPower, 1f, 20f);
            highlightIntensity = EditorGUILayout.Slider(L("ハイライト量", "Highlight Amount"), highlightIntensity, 0f, 1f);
            rimColor = EditorGUILayout.ColorField(L("リムカラー", "Rim Color"), rimColor);
            rimPower = EditorGUILayout.Slider(L("リム幅", "Rim Power"), rimPower, 0.5f, 10f);

            if (EditorGUI.EndChangeCheck()) UpdatePreview();

            EditorGUILayout.Space(4);
            if (preview != null)
            {
                Rect r = GUILayoutUtility.GetRect(200, 200, GUILayout.Width(200), GUILayout.Height(200));
                r.x += (EditorGUIUtility.currentViewWidth - 200) * 0.5f;
                EditorGUI.DrawPreviewTexture(r, preview);
            }

            EditorGUILayout.Space(4);
            targetMaterial = (Material)EditorGUILayout.ObjectField(
                L("マテリアル", "Material"), targetMaterial, typeof(Material), false);
            targetProperty = EditorGUILayout.TextField(L("プロパティ", "Property"), targetProperty);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("適用", "Apply"), GUILayout.Height(28)))
                ApplyToMaterial();
            if (GUILayout.Button(L("保存", "Save PNG"), GUILayout.Height(28)))
                SavePNG();
            EditorGUILayout.EndHorizontal();
        }

        private void UpdatePreview()
        {
            if (preview != null) DestroyImmediate(preview);
            preview = Generate(textureSize, baseColor, shadowColor, shadowSoftness,
                highlightColor, highlightPower, highlightIntensity, rimColor, rimPower);
            Repaint();
        }

        public static Texture2D Generate(int size, Color baseCol, Color shadowCol, float shadowSoft,
            Color highlightCol, float hlPow, float hlInt, Color rimCol, float rimPow)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - half) / half;
                    float ny = (y - half) / half;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);

                    if (dist > 1f) { tex.SetPixel(x, y, Color.clear); continue; }

                    float nz = Mathf.Sqrt(Mathf.Max(0, 1f - nx * nx - ny * ny));

                    // Hemisphere shading (top-left light)
                    float ndotl = Mathf.Clamp01(nx * -0.4f + ny * 0.5f + nz * 0.7f);
                    float shadow = Mathf.Lerp(Mathf.SmoothStep(0f, 1f, ndotl), ndotl, shadowSoft);
                    Color col = Color.Lerp(shadowCol, baseCol, shadow);

                    // Highlight (specular)
                    float spec = Mathf.Pow(Mathf.Max(0, nz), hlPow) * hlInt;
                    col = Color.Lerp(col, highlightCol, spec);

                    // Rim
                    float rim = Mathf.Pow(1f - nz, rimPow) * rimCol.a;
                    col = Color.Lerp(col, new Color(rimCol.r, rimCol.g, rimCol.b, 1f), rim);

                    col.a = 1f;
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private void ApplyToMaterial()
        {
            if (targetMaterial == null || preview == null) return;
            if (!targetMaterial.HasProperty(targetProperty)) return;
            Undo.RecordObject(targetMaterial, "Apply MatCap");
            targetMaterial.SetTexture(targetProperty, Instantiate(preview));
            EditorUtility.SetDirty(targetMaterial);
        }

        private void SavePNG()
        {
            if (preview == null) return;
            string path = EditorUtility.SaveFilePanel("Save MatCap", "Assets", "matcap", "png");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, preview.EncodeToPNG());
                AssetDatabase.Refresh();
            }
        }

        private void OnDestroy() { if (preview != null) DestroyImmediate(preview); }
    }
}
