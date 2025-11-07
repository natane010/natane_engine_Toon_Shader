using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Dissolve Pattern Generator
    /// ディゾルブパターンジェネレーター
    /// </summary>
    public class DissolvePatternGenerator : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;

        private enum NoiseType { Perlin, Voronoi, Cellular, Random, Gradient }
        private NoiseType noiseType = NoiseType.Perlin;

        private int textureSize = 512;
        private float scale = 5f;
        private float contrast = 1f;

        [MenuItem("Tools/Natane/Dissolve Pattern Generator", false, 138)]
        public static void ShowWindow()
        {
            var window = GetWindow<DissolvePatternGenerator>("ディゾルブ生成 Dissolve");
            window.minSize = new Vector2(500, 550);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ディゾルブパターンジェネレーター Dissolve Pattern Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("プロシージャルにディゾルブテクスチャを生成\nGenerate dissolve textures procedurally", MessageType.Info);
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField("ターゲット Target", targetMaterial, typeof(Material), false);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawGenerator();
            EditorGUILayout.Space(10);
            DrawDissolveSettings();
            EditorGUILayout.Space(10);
            DrawPreview();

            EditorGUILayout.EndScrollView();
        }

        private void DrawGenerator()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("テクスチャ生成 Texture Generation", EditorStyles.boldLabel);

            noiseType = (NoiseType)EditorGUILayout.EnumPopup("ノイズタイプ Noise Type", noiseType);
            textureSize = EditorGUILayout.IntPopup("サイズ Size", textureSize, new[] { "256", "512", "1024" }, new[] { 256, 512, 1024 });
            scale = EditorGUILayout.Slider("スケール Scale", scale, 1f, 20f);
            contrast = EditorGUILayout.Slider("コントラスト Contrast", contrast, 0.1f, 3f);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("テクスチャを生成 Generate Texture", GUILayout.Height(30)))
            {
                GenerateDissolveTexture();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDissolveSettings()
        {
            if (targetMaterial == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ディゾルブ設定 Dissolve Settings", EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_DissolveAmount"))
            {
                EditorGUI.BeginChangeCheck();
                float amount = EditorGUILayout.Slider("溶解量 Amount", targetMaterial.GetFloat("_DissolveAmount"), 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Dissolve Amount");
                    targetMaterial.SetFloat("_DissolveAmount", amount);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_DissolveEdgeWidth"))
            {
                EditorGUI.BeginChangeCheck();
                float width = EditorGUILayout.Slider("エッジ幅 Edge Width", targetMaterial.GetFloat("_DissolveEdgeWidth"), 0f, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Edge Width");
                    targetMaterial.SetFloat("_DissolveEdgeWidth", width);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            if (targetMaterial.HasProperty("_DissolveEdgeColor"))
            {
                EditorGUI.BeginChangeCheck();
                Color color = EditorGUILayout.ColorField(new GUIContent("エッジ色 Edge Color"), targetMaterial.GetColor("_DissolveEdgeColor"), true, true, true);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMaterial, "Change Edge Color");
                    targetMaterial.SetColor("_DissolveEdgeColor", color);
                    EditorUtility.SetDirty(targetMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("プレビュー Preview", EditorStyles.boldLabel);

            if (targetMaterial != null && targetMaterial.HasProperty("_DissolveMap"))
            {
                Texture2D dissolveTex = targetMaterial.GetTexture("_DissolveMap") as Texture2D;
                if (dissolveTex != null)
                {
                    Rect previewRect = GUILayoutUtility.GetRect(200, 200);
                    EditorGUI.DrawPreviewTexture(previewRect, dissolveTex, null, ScaleMode.ScaleToFit);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void GenerateDissolveTexture()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "ディゾルブテクスチャを保存 Save Dissolve Texture",
                "DissolvePattern",
                "png",
                "保存場所を選択 Choose location");

            if (string.IsNullOrEmpty(path)) return;

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float value = GenerateNoiseValue(x, y);
                    value = Mathf.Pow(value, contrast);
                    Color color = new Color(value, value, value);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            Texture2D savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (targetMaterial != null && targetMaterial.HasProperty("_DissolveMap"))
            {
                Undo.RecordObject(targetMaterial, "Set Dissolve Texture");
                targetMaterial.SetTexture("_DissolveMap", savedTexture);
                targetMaterial.EnableKeyword("_DISSOLVE");
                EditorUtility.SetDirty(targetMaterial);
            }

            EditorUtility.DisplayDialog(
                "生成完了 Generated",
                $"ディゾルブテクスチャを生成しました\nGenerated dissolve texture: {path}",
                "OK");
        }

        private float GenerateNoiseValue(int x, int y)
        {
            float fx = x / (float)textureSize * scale;
            float fy = y / (float)textureSize * scale;

            switch (noiseType)
            {
                case NoiseType.Perlin:
                    return Mathf.PerlinNoise(fx, fy);

                case NoiseType.Voronoi:
                    return VoronoiNoise(fx, fy);

                case NoiseType.Cellular:
                    return CellularNoise(fx, fy);

                case NoiseType.Random:
                    return Random.value;

                case NoiseType.Gradient:
                    return Mathf.Clamp01((fx + fy) / (scale * 2));

                default:
                    return 0f;
            }
        }

        private float VoronoiNoise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);

            float minDist = float.MaxValue;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector2 point = new Vector2(xi + i, yi + j);
                    Random.InitState(GetHashCode(point));
                    point += new Vector2(Random.value, Random.value);

                    float dist = Vector2.Distance(new Vector2(x, y), point);
                    minDist = Mathf.Min(minDist, dist);
                }
            }

            return Mathf.Clamp01(minDist / 1.414f);
        }

        private float CellularNoise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);

            Random.InitState(GetHashCode(new Vector2(xi, yi)));
            return Random.value;
        }

        private int GetHashCode(Vector2 v)
        {
            return ((int)v.x * 73856093) ^ ((int)v.y * 19349663);
        }
    }
}
