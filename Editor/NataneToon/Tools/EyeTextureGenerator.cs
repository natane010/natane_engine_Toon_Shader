using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Procedural eye/iris texture generator for Eye shader.
    /// Eye シェーダー用プロシージャル瞳テクスチャジェネレータ
    /// </summary>
    internal class EyeTextureGenerator : EditorWindow
    {
        private int textureSize = 512;
        private Color irisColor = new Color(0.3f, 0.5f, 0.8f);
        private Color irisEdgeColor = new Color(0.1f, 0.2f, 0.4f);
        private Color pupilColor = Color.black;
        private float pupilSize = 0.3f;
        private float irisSize = 0.8f;
        private Color highlightColor = Color.white;
        private float highlightSize = 0.12f;
        private Vector2 highlightOffset = new Vector2(-0.15f, 0.2f);
        private float highlightSize2 = 0.06f;
        private Vector2 highlightOffset2 = new Vector2(0.1f, -0.15f);
        private int irisRays = 0;
        private float irisRayIntensity = 0.3f;
        private Color scleraColor = Color.white;
        private Texture2D preview;

        [MenuItem("Tools/Natane/Eye Texture Generator", false, 152)]
        public static void Open()
        {
            var w = GetWindow<EyeTextureGenerator>(true, "Eye Texture Generator");
            w.minSize = new Vector2(400, 550);
            w.ShowUtility();
        }

        private void OnEnable() { UpdatePreview(); }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(L("瞳テクスチャジェネレータ", "Eye Texture Generator"),
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });

            EditorGUI.BeginChangeCheck();

            textureSize = EditorGUILayout.IntPopup(L("サイズ", "Size"), textureSize,
                new[] { "256", "512", "1024" }, new[] { 256, 512, 1024 });

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(L("虹彩", "Iris"), EditorStyles.boldLabel);
            irisColor = EditorGUILayout.ColorField(L("虹彩カラー", "Iris Color"), irisColor);
            irisEdgeColor = EditorGUILayout.ColorField(L("虹彩エッジ", "Iris Edge"), irisEdgeColor);
            irisSize = EditorGUILayout.Slider(L("虹彩サイズ", "Iris Size"), irisSize, 0.3f, 1f);
            irisRays = EditorGUILayout.IntSlider(L("放射模様", "Rays"), irisRays, 0, 32);
            if (irisRays > 0)
                irisRayIntensity = EditorGUILayout.Slider(L("放射強度", "Ray Intensity"), irisRayIntensity, 0f, 1f);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(L("瞳孔", "Pupil"), EditorStyles.boldLabel);
            pupilColor = EditorGUILayout.ColorField(L("瞳孔カラー", "Pupil Color"), pupilColor);
            pupilSize = EditorGUILayout.Slider(L("瞳孔サイズ", "Pupil Size"), pupilSize, 0.05f, 0.6f);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(L("ハイライト", "Highlights"), EditorStyles.boldLabel);
            highlightColor = EditorGUILayout.ColorField(L("色", "Color"), highlightColor);
            highlightSize = EditorGUILayout.Slider(L("サイズ1", "Size 1"), highlightSize, 0f, 0.3f);
            highlightOffset = EditorGUILayout.Vector2Field(L("位置1", "Pos 1"), highlightOffset);
            highlightSize2 = EditorGUILayout.Slider(L("サイズ2", "Size 2"), highlightSize2, 0f, 0.2f);
            highlightOffset2 = EditorGUILayout.Vector2Field(L("位置2", "Pos 2"), highlightOffset2);

            scleraColor = EditorGUILayout.ColorField(L("白目", "Sclera"), scleraColor);

            if (EditorGUI.EndChangeCheck()) UpdatePreview();

            EditorGUILayout.Space(4);
            if (preview != null)
            {
                Rect r = GUILayoutUtility.GetRect(180, 180, GUILayout.Width(180), GUILayout.Height(180));
                r.x += (EditorGUIUtility.currentViewWidth - 180) * 0.5f;
                EditorGUI.DrawPreviewTexture(r, preview);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("PNGで保存", "Save PNG"), GUILayout.Height(28)))
            {
                string path = EditorUtility.SaveFilePanel("Save Eye Texture", "Assets", "eye_iris", "png");
                if (!string.IsNullOrEmpty(path))
                {
                    System.IO.File.WriteAllBytes(path, preview.EncodeToPNG());
                    AssetDatabase.Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void UpdatePreview()
        {
            if (preview != null) DestroyImmediate(preview);
            preview = Generate();
            Repaint();
        }

        private Texture2D Generate()
        {
            var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            float half = textureSize * 0.5f;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float nx = (x - half) / half;
                    float ny = (y - half) / half;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);

                    // Outside iris = sclera
                    if (dist > irisSize)
                    {
                        tex.SetPixel(x, y, scleraColor);
                        continue;
                    }

                    // Iris
                    float irisT = dist / irisSize;
                    Color col = Color.Lerp(irisColor, irisEdgeColor, irisT * irisT);

                    // Iris rays
                    if (irisRays > 0)
                    {
                        float angle = Mathf.Atan2(ny, nx);
                        float ray = Mathf.Abs(Mathf.Sin(angle * irisRays * 0.5f));
                        ray = Mathf.Pow(ray, 2f) * irisRayIntensity * (1f - irisT);
                        col = Color.Lerp(col, irisColor * 1.3f, ray);
                    }

                    // Pupil
                    if (dist < pupilSize)
                    {
                        float pupilT = Mathf.SmoothStep(pupilSize, pupilSize * 0.8f, dist);
                        col = Color.Lerp(col, pupilColor, pupilT);
                    }

                    // Iris edge darkening
                    float edgeDark = Mathf.SmoothStep(irisSize * 0.85f, irisSize, dist);
                    col = Color.Lerp(col, irisEdgeColor * 0.5f, edgeDark);

                    // Highlights
                    float h1Dist = Vector2.Distance(new Vector2(nx, ny), highlightOffset);
                    if (h1Dist < highlightSize)
                    {
                        float h1T = 1f - Mathf.SmoothStep(0f, highlightSize, h1Dist);
                        col = Color.Lerp(col, highlightColor, h1T);
                    }

                    float h2Dist = Vector2.Distance(new Vector2(nx, ny), highlightOffset2);
                    if (h2Dist < highlightSize2)
                    {
                        float h2T = 1f - Mathf.SmoothStep(0f, highlightSize2, h2Dist);
                        col = Color.Lerp(col, highlightColor, h2T * 0.7f);
                    }

                    col.a = 1f;
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private void OnDestroy() { if (preview != null) DestroyImmediate(preview); }
    }
}
