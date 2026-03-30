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
        WeightedSmoothing, // ガウシアン重み付け: Krita方式の距離ベース平滑化
        QueueStabilizer,   // キューベース: Krita方式の遅延安定化
        PixelPerfect       // ピクセルパーフェクト: ピクセルアート用グリッド量子化
    }

    [System.Serializable]
    internal class BrushStabilizerSettings
    {
        public BrushStabilizerMode mode = BrushStabilizerMode.Off;
        public float strength = 0.45f;
        public float delayDistance = 0f;  // Pixels to move before first dab / 最初のダブまでの最小移動距離
    }

    internal sealed class MaskTextureBrushStabilizer
    {
        private bool hasPoint;
        private Vector2 filteredPoint;

        // Queue stabilizer state (Krita-style)
        private readonly System.Collections.Generic.Queue<Vector2> pointQueue
            = new System.Collections.Generic.Queue<Vector2>();
        private int queueCapacity = 8;

        // Delay distance state
        private float accumulatedDistance;
        private bool delayComplete;

        public void Reset()
        {
            hasPoint = false;
            pointQueue.Clear();
            accumulatedDistance = 0f;
            delayComplete = false;
        }

        public void Reset(Vector2 startPoint)
        {
            hasPoint = true;
            filteredPoint = startPoint;
            pointQueue.Clear();
            pointQueue.Enqueue(startPoint);
            accumulatedDistance = 0f;
            delayComplete = false;
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

            // Delay distance: don't draw until pen has moved enough
            // 遅延距離: ペンが十分移動するまで描画しない
            if (settings.delayDistance > 0f && !delayComplete && hasPoint)
            {
                accumulatedDistance += Vector2.Distance(rawPoint, filteredPoint);
                if (accumulatedDistance < settings.delayDistance)
                {
                    // Don't update filteredPoint - suppress drawing
                    return filteredPoint;  // Return old position (no movement)
                }
                delayComplete = true;
            }

            if (settings.mode == BrushStabilizerMode.QueueStabilizer)
            {
                // Krita-style queue stabilizer: average of last N points
                // Krita方式キュースタビライザー: 直近N点の平均
                queueCapacity = Mathf.Max(2, Mathf.RoundToInt(Mathf.Lerp(3f, 20f, strength)));
                pointQueue.Enqueue(rawPoint);
                while (pointQueue.Count > queueCapacity)
                    pointQueue.Dequeue();

                Vector2 avg = Vector2.zero;
                foreach (var pt in pointQueue)
                    avg += pt;
                avg /= pointQueue.Count;

                filteredPoint = avg;
                return filteredPoint;
            }

            if (settings.mode == BrushStabilizerMode.PixelPerfect)
            {
                // Pixel perfect: quantize to pixel grid
                // ピクセルパーフェクト: ピクセルグリッドに量子化
                filteredPoint = new Vector2(
                    Mathf.Round(rawPoint.x),
                    Mathf.Round(rawPoint.y));
                hasPoint = true;
                return filteredPoint;
            }

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
                        case BrushStabilizerMode.QueueStabilizer:
                            strengthLabel = "Queue Size";
                            break;
                        case BrushStabilizerMode.PixelPerfect:
                            strengthLabel = "Grid Size";
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
                    else if (settings.mode == BrushStabilizerMode.QueueStabilizer)
                    {
                        int displaySize = Mathf.RoundToInt(Mathf.Lerp(3f, 20f, settings.strength));
                        EditorGUILayout.HelpBox($"Krita方式: 直近{displaySize}点の平均で安定化。遅延あり", MessageType.Info);
                    }
                    else if (settings.mode == BrushStabilizerMode.PixelPerfect)
                    {
                        EditorGUILayout.HelpBox("ピクセルアート用: ブラシ位置をピクセルグリッドに量子化", MessageType.Info);
                    }

                    if (settings.mode != BrushStabilizerMode.Off && settings.mode != BrushStabilizerMode.PixelPerfect)
                    {
                        settings.delayDistance = EditorGUILayout.Slider(
                            "Delay Distance", settings.delayDistance, 0f, 30f);
                        if (settings.delayDistance > 0.1f)
                            EditorGUILayout.HelpBox($"描画開始まで{settings.delayDistance:F0}px移動が必要", MessageType.Info);
                    }
                }
            }
        }
    }
}
