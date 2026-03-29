using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Texture brush system for custom brush tip textures.
    /// カスタムブラシチップテクスチャ用テクスチャブラシシステム
    /// </summary>
    [System.Serializable]
    internal class TextureBrushSettings
    {
        public Texture2D brushTipTexture;
        public bool enableRotation;
        public float rotationAngle;
        public bool randomRotation;
        public bool enableSizeJitter;
        public float sizeJitterAmount = 0.2f;
        public bool enableOpacityJitter;
        public float opacityJitterAmount = 0.1f;
        public bool enableScatter;
        public float scatterAmount = 0.3f;
    }

    internal static class TextureBrushSystem
    {
        private static System.Random jitterRng = new System.Random();

        /// <summary>
        /// Sample the brush tip texture at normalized coordinates with rotation.
        /// 回転付きで正規化座標でブラシチップテクスチャをサンプリング
        /// Returns falloff multiplier (0-1).
        /// </summary>
        public static float SampleBrushTip(
            Texture2D tipTexture, float normalizedX, float normalizedY,
            float rotationRad)
        {
            if (tipTexture == null) return 1f;

            // Rotate around center
            float cx = normalizedX - 0.5f;
            float cy = normalizedY - 0.5f;
            float cos = Mathf.Cos(rotationRad);
            float sin = Mathf.Sin(rotationRad);
            float rx = cx * cos - cy * sin + 0.5f;
            float ry = cx * sin + cy * cos + 0.5f;

            if (rx < 0f || rx > 1f || ry < 0f || ry > 1f) return 0f;

            Color sample = tipTexture.GetPixelBilinear(rx, ry);
            return (sample.r + sample.g + sample.b) / 3f * sample.a;
        }

        /// <summary>
        /// Get jittered stamp parameters for a single stamp.
        /// 単一スタンプのジッター適用パラメータを取得
        /// </summary>
        public static void GetJitteredParams(
            TextureBrushSettings settings, float baseSize, float baseOpacity,
            out float size, out float opacity, out float rotation, out Vector2 scatter)
        {
            size = baseSize;
            opacity = baseOpacity;
            rotation = 0f;
            scatter = Vector2.zero;

            if (settings == null) return;

            if (settings.enableSizeJitter)
            {
                float jitter = (float)(jitterRng.NextDouble() * 2.0 - 1.0) * settings.sizeJitterAmount;
                size = baseSize * (1f + jitter);
            }

            if (settings.enableOpacityJitter)
            {
                float jitter = (float)(jitterRng.NextDouble() * 2.0 - 1.0) * settings.opacityJitterAmount;
                opacity = Mathf.Clamp01(baseOpacity * (1f + jitter));
            }

            if (settings.enableRotation)
            {
                if (settings.randomRotation)
                    rotation = (float)(jitterRng.NextDouble() * 2.0 * System.Math.PI);
                else
                    rotation = settings.rotationAngle * Mathf.Deg2Rad;
            }

            if (settings.enableScatter)
            {
                float scatterDist = (float)jitterRng.NextDouble() * settings.scatterAmount * baseSize;
                float scatterAngle = (float)(jitterRng.NextDouble() * 2.0 * System.Math.PI);
                scatter = new Vector2(
                    Mathf.Cos(scatterAngle) * scatterDist,
                    Mathf.Sin(scatterAngle) * scatterDist);
            }
        }

        /// <summary>
        /// Draw texture brush settings UI.
        /// テクスチャブラシ設定UIを描画
        /// </summary>
        public static void DrawSettingsUI(TextureBrushSettings settings)
        {
            if (settings == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(L("テクスチャブラシ", "Texture Brush"), EditorStyles.boldLabel);

                settings.brushTipTexture = (Texture2D)EditorGUILayout.ObjectField(
                    L("ブラシチップ", "Brush Tip"),
                    settings.brushTipTexture, typeof(Texture2D), false);

                if (settings.brushTipTexture != null)
                {
                    // Preview
                    Rect previewRect = GUILayoutUtility.GetRect(48, 48,
                        GUILayout.Width(48), GUILayout.Height(48));
                    EditorGUI.DrawPreviewTexture(previewRect, settings.brushTipTexture);

                    settings.enableRotation = EditorGUILayout.Toggle(
                        L("回転", "Rotation"), settings.enableRotation);
                    if (settings.enableRotation)
                    {
                        EditorGUI.indentLevel++;
                        settings.randomRotation = EditorGUILayout.Toggle(
                            L("ランダム回転", "Random"), settings.randomRotation);
                        if (!settings.randomRotation)
                            settings.rotationAngle = EditorGUILayout.Slider(
                                L("角度", "Angle"), settings.rotationAngle, 0f, 360f);
                        EditorGUI.indentLevel--;
                    }

                    settings.enableSizeJitter = EditorGUILayout.Toggle(
                        L("サイズジッター", "Size Jitter"), settings.enableSizeJitter);
                    if (settings.enableSizeJitter)
                        settings.sizeJitterAmount = EditorGUILayout.Slider(
                            L("量", "Amount"), settings.sizeJitterAmount, 0f, 1f);

                    settings.enableOpacityJitter = EditorGUILayout.Toggle(
                        L("不透明度ジッター", "Opacity Jitter"), settings.enableOpacityJitter);
                    if (settings.enableOpacityJitter)
                        settings.opacityJitterAmount = EditorGUILayout.Slider(
                            L("量", "Amount"), settings.opacityJitterAmount, 0f, 1f);

                    settings.enableScatter = EditorGUILayout.Toggle(
                        L("散布", "Scatter"), settings.enableScatter);
                    if (settings.enableScatter)
                        settings.scatterAmount = EditorGUILayout.Slider(
                            L("量", "Amount"), settings.scatterAmount, 0f, 2f);
                }
            }
        }

        /// <summary>
        /// Create default brush tip textures.
        /// デフォルトブラシチップテクスチャを作成
        /// </summary>
        public static Texture2D CreateDefaultSoftTip(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            float center = size * 0.5f;
            float radius = center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float val = 1f - Mathf.Clamp01(dist / radius);
                    val = val * val * (3f - 2f * val); // Smoothstep
                    tex.SetPixel(x, y, new Color(val, val, val, val));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
