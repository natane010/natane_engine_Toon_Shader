using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    internal enum BrushStabilizerMode
    {
        Off,
        Basic,
        Stabilized,
        String,            // ストリングメソッド: ペン先から紐で引っ張るような安定化
        WeightedSmoothing  // ガウシアン重み付け: Krita方式の距離ベース平滑化
    }

    [System.Serializable]
    internal class BrushStabilizerSettings
    {
        public BrushStabilizerMode mode = BrushStabilizerMode.Off;
        public float strength = 0.45f;
    }

    internal sealed class MaskTextureBrushStabilizer
    {
        private bool hasPoint;
        private Vector2 filteredPoint;

        public void Reset()
        {
            hasPoint = false;
        }

        public void Reset(Vector2 startPoint)
        {
            hasPoint = true;
            filteredPoint = startPoint;
        }

        public void EndStroke()
        {
            hasPoint = false;
        }

        public Vector2 Filter(Vector2 rawPoint, BrushStabilizerSettings settings)
        {
            if (settings == null || settings.mode == BrushStabilizerMode.Off)
            {
                filteredPoint = rawPoint;
                hasPoint = true;
                return rawPoint;
            }

            if (!hasPoint)
            {
                Reset(rawPoint);
                return rawPoint;
            }

            float strength = Mathf.Clamp01(settings.strength);

            if (settings.mode == BrushStabilizerMode.String)
            {
                // ストリングメソッド: 一定長の紐でペン先から描画点を引っ張る
                // ペンが紐の長さを超えて移動した場合のみ描画点が追従する
                float stringLength = Mathf.Lerp(5f, 60f, strength);
                float dist = Vector2.Distance(rawPoint, filteredPoint);
                if (dist > stringLength)
                {
                    Vector2 dir = (rawPoint - filteredPoint).normalized;
                    filteredPoint = rawPoint - dir * stringLength;
                }
                // dist <= stringLength の場合、描画点は移動しない（紐の範囲内）
                return filteredPoint;
            }

            if (settings.mode == BrushStabilizerMode.WeightedSmoothing)
            {
                // Krita-style Gaussian weighted smoothing
                // Krita方式ガウシアン重み付けスムージング
                float dist = Vector2.Distance(rawPoint, filteredPoint);
                float sigma = Mathf.Max(dist / 3f, 0.5f);
                float weight = Mathf.Exp(-0.5f * (dist * dist) / (sigma * sigma));
                float factor = Mathf.Lerp(0.1f, 0.8f, strength);
                filteredPoint = Vector2.Lerp(filteredPoint, rawPoint, Mathf.Max(factor, weight));
                return filteredPoint;
            }

            float baseFactor = settings.mode == BrushStabilizerMode.Basic ? 0.7f : 0.4f;
            float lerpFactor = Mathf.Clamp01(Mathf.Lerp(0.15f, baseFactor, strength));

            filteredPoint = Vector2.Lerp(filteredPoint, rawPoint, lerpFactor);
            return filteredPoint;
        }
    }

    internal static class BrushStabilizerUI
    {
        public static void DrawSettings(BrushStabilizerSettings settings)
        {
            if (settings == null)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Stabilizer", EditorStyles.boldLabel);
                settings.mode = (BrushStabilizerMode)EditorGUILayout.EnumPopup("Mode", settings.mode);

                if (settings.mode != BrushStabilizerMode.Off)
                {
                    string strengthLabel;
                    switch (settings.mode)
                    {
                        case BrushStabilizerMode.String:
                            strengthLabel = "String Length";
                            break;
                        case BrushStabilizerMode.WeightedSmoothing:
                            strengthLabel = "Smoothness";
                            break;
                        default:
                            strengthLabel = "Strength";
                            break;
                    }
                    settings.strength = EditorGUILayout.Slider(strengthLabel, settings.strength, 0.05f, 1f);

                    if (settings.mode == BrushStabilizerMode.String)
                    {
                        float displayLength = Mathf.Lerp(5f, 60f, settings.strength);
                        EditorGUILayout.HelpBox($"紐の長さ: {displayLength:F0}px — ペンが紐を超えて移動すると描画点が追従します", MessageType.Info);
                    }
                    else if (settings.mode == BrushStabilizerMode.WeightedSmoothing)
                    {
                        EditorGUILayout.HelpBox("Krita方式: 距離に基づくガウシアン重み付けスムージング", MessageType.Info);
                    }
                }
            }
        }
    }
}
