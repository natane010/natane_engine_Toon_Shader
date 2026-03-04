using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Dissolve Pattern Generator
    /// ディゾルブパターンジェネレーター
    /// </summary>
    public class DissolvePatternGenerator : EditorWindow
    {
        private Material targetMaterial;
        private Vector2 scrollPosition;
        private static readonly string[] DissolveTextureProperties = { "_DissolveTex", "_DissolveMap" };

        private enum NoiseType { Perlin, Voronoi, Cellular, Random, Gradient }
        private NoiseType noiseType = NoiseType.Perlin;

        private int textureSize = 512;
        private float scale = 5f;
        private float contrast = 1f;

        [MenuItem("Tools/Natane/エフェクト Effects/ディゾルブパターン生成 Dissolve Pattern Generator", false, 43)]
        public static void ShowWindow()
        {
            var window = GetWindow<DissolvePatternGenerator>(L("ディゾルブパターン生成", "Dissolve Pattern Generator"));
            window.minSize = new Vector2(500, 550);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            NataneToonShaderGUIUtility.DrawHeaderWithHelp("ディゾルブパターンジェネレーター", "Dissolve Pattern Generator", "DissolvePatternGenerator");
            EditorGUILayout.LabelField(L("プロシージャルにディゾルブテクスチャを生成", "Generate dissolve textures procedurally"), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            targetMaterial = (Material)EditorGUILayout.ObjectField(L("ターゲット", "Target"), targetMaterial, typeof(Material), false);

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
            EditorGUILayout.LabelField(L("テクスチャ生成", "Texture Generation"), EditorStyles.boldLabel);

            noiseType = (NoiseType)EditorGUILayout.EnumPopup(L("ノイズタイプ", "Noise Type"), noiseType);
            textureSize = EditorGUILayout.IntPopup(L("サイズ", "Size"), textureSize, new[] { "256", "512", "1024" }, new[] { 256, 512, 1024 });
            scale = EditorGUILayout.Slider(L("スケール", "Scale"), scale, 1f, 20f);
            contrast = EditorGUILayout.Slider(L("コントラスト", "Contrast"), contrast, 0.1f, 3f);

            EditorGUILayout.Space(10);

            if (GUILayout.Button(L("テクスチャを生成", "Generate Texture"), GUILayout.Height(30)))
            {
                GenerateDissolveTexture();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDissolveSettings()
        {
            if (targetMaterial == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ディゾルブ設定", "Dissolve Settings"), EditorStyles.boldLabel);

            if (targetMaterial.HasProperty("_DissolveAmount"))
            {
                EditorGUI.BeginChangeCheck();
                float amount = EditorGUILayout.Slider(L("溶解量", "Dissolve Amount"), targetMaterial.GetFloat("_DissolveAmount"), 0f, 1f);
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
                float width = EditorGUILayout.Slider(L("エッジ幅", "Edge Width"), targetMaterial.GetFloat("_DissolveEdgeWidth"), 0f, 0.5f);
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
                Color color = EditorGUILayout.ColorField(new GUIContent(L("エッジ色", "Edge Color")), targetMaterial.GetColor("_DissolveEdgeColor"), true, true, true);
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
            EditorGUILayout.LabelField(L("プレビュー", "Preview"), EditorStyles.boldLabel);

            string dissolveTextureProperty = GetFirstExistingProperty(targetMaterial, DissolveTextureProperties);
            if (targetMaterial != null && !string.IsNullOrEmpty(dissolveTextureProperty))
            {
                Texture2D dissolveTex = targetMaterial.GetTexture(dissolveTextureProperty) as Texture2D;
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
                L("ディゾルブテクスチャを保存", "Save Dissolve Texture"),
                "DissolvePattern",
                "png",
                L("保存場所を選択", "Select save location"));

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
            try
            {
                System.IO.File.WriteAllBytes(path, bytes);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Natane Toon] Failed to write file: {path}\n{e.Message}");
                return;
            }
            AssetDatabase.Refresh();

            Texture2D savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            string dissolveTextureProperty = GetFirstExistingProperty(targetMaterial, DissolveTextureProperties);
            if (targetMaterial != null && !string.IsNullOrEmpty(dissolveTextureProperty))
            {
                Undo.RecordObject(targetMaterial, "Set Dissolve Texture");
                targetMaterial.SetTexture(dissolveTextureProperty, savedTexture);
                if (targetMaterial.HasProperty("_Dissolve"))
                {
                    targetMaterial.SetFloat("_Dissolve", 1f);
                }
                targetMaterial.EnableKeyword("_DISSOLVE");
                EditorUtility.SetDirty(targetMaterial);
            }

            EditorUtility.DisplayDialog(
                L("生成完了", "Generation Complete"),
                L($"ディゾルブテクスチャを生成しました: {path}", $"Dissolve texture generated: {path}"),
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

        private static string GetFirstExistingProperty(Material material, params string[] propertyNames)
        {
            if (material == null || propertyNames == null)
            {
                return null;
            }

            foreach (var propertyName in propertyNames)
            {
                if (!string.IsNullOrEmpty(propertyName) && material.HasProperty(propertyName))
                {
                    return propertyName;
                }
            }

            return null;
        }
    }
}
