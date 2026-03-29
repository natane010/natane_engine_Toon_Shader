using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Library of commonly used texture/mask presets.
    /// よく使うテクスチャ/マスクプリセットのライブラリ
    /// </summary>
    internal static class TexturePresetLibrary
    {
        internal struct TexturePreset
        {
            public string name;
            public string description;
            public string category;
            public System.Func<int, int, Color[]> generator;
        }

        private static List<TexturePreset> presets;

        public static List<TexturePreset> GetPresets()
        {
            if (presets != null) return presets;

            presets = new List<TexturePreset>
            {
                // Shadow masks
                new TexturePreset {
                    name = L("上半分シャドウ", "Top Half Shadow"),
                    category = L("影マスク", "Shadow Mask"),
                    description = L("上半分が白、下半分が黒のマスク", "White top, black bottom"),
                    generator = (w, h) => GradientVertical(w, h, Color.white, Color.black)
                },
                new TexturePreset {
                    name = L("ソフトシャドウ", "Soft Shadow"),
                    category = L("影マスク", "Shadow Mask"),
                    description = L("ソフトな縦グラデーション", "Soft vertical gradient"),
                    generator = (w, h) => GradientVertical(w, h, Color.white, new Color(0.3f, 0.3f, 0.3f))
                },
                new TexturePreset {
                    name = L("中央ハイライト", "Center Highlight"),
                    category = L("影マスク", "Shadow Mask"),
                    description = L("中央が明るいラジアルマスク", "Bright center radial mask"),
                    generator = (w, h) => RadialGradient(w, h, Color.white, Color.black)
                },
                // Emission masks
                new TexturePreset {
                    name = L("全面エミッション", "Full Emission"),
                    category = L("エミッション", "Emission"),
                    generator = (w, h) => SolidColor(w, h, Color.white)
                },
                new TexturePreset {
                    name = L("エッジグロー", "Edge Glow"),
                    category = L("エミッション", "Emission"),
                    generator = (w, h) => RadialGradient(w, h, Color.black, Color.white)
                },
                // Outline masks
                new TexturePreset {
                    name = L("均一アウトライン", "Uniform Outline"),
                    category = L("アウトライン", "Outline"),
                    generator = (w, h) => SolidColor(w, h, Color.white)
                },
                new TexturePreset {
                    name = L("上部細アウトライン", "Thin Top Outline"),
                    category = L("アウトライン", "Outline"),
                    generator = (w, h) => GradientVertical(w, h, new Color(0.3f, 0.3f, 0.3f), Color.white)
                },
                // SSS masks
                new TexturePreset {
                    name = L("肌SSS", "Skin SSS"),
                    category = "SSS",
                    description = L("肌の散乱光用マスク", "Subsurface scattering for skin"),
                    generator = (w, h) => SolidColor(w, h, new Color(0.7f, 0.7f, 0.7f))
                },
                // Specular masks
                new TexturePreset {
                    name = L("均一スペキュラ", "Uniform Specular"),
                    category = L("スペキュラ", "Specular"),
                    generator = (w, h) => SolidColor(w, h, new Color(0.5f, 0.5f, 0.5f))
                },
                new TexturePreset {
                    name = L("メタリック", "Metallic"),
                    category = L("スペキュラ", "Specular"),
                    generator = (w, h) => SolidColor(w, h, Color.white)
                },
            };
            return presets;
        }

        // Generator helpers
        private static Color[] SolidColor(int w, int h, Color color)
        {
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            return px;
        }

        private static Color[] GradientVertical(int w, int h, Color top, Color bottom)
        {
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            return px;
        }

        private static Color[] RadialGradient(int w, int h, Color center, Color edge)
        {
            Color[] px = new Color[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            float maxDist = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float t = Mathf.Clamp01(dist / maxDist);
                    px[y * w + x] = Color.Lerp(center, edge, t);
                }
            }
            return px;
        }

        /// <summary>
        /// Draw preset browser UI. Returns selected preset's pixels or null.
        /// プリセットブラウザUIを描画。選択されたプリセットのピクセルまたはnullを返す。
        /// </summary>
        public static Color[] DrawPresetBrowser(int width, int height, ref Vector2 scrollPos)
        {
            var allPresets = GetPresets();
            Color[] result = null;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("テクスチャプリセット", "Texture Presets"), EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));

            string lastCategory = "";
            foreach (var preset in allPresets)
            {
                if (preset.category != lastCategory)
                {
                    lastCategory = preset.category;
                    EditorGUILayout.LabelField(preset.category,
                        new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold,
                            normal = { textColor = new Color(0.55f, 0.78f, 1f) } });
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(preset.name, EditorStyles.miniButton, GUILayout.Height(22)))
                    result = preset.generator(width, height);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            return result;
        }
    }
}
