using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// Filters and processes brush pressure input for advanced pen tablet features.
    /// 筆圧入力のフィルタリングと高度なペンタブレット機能の処理
    /// </summary>
    internal sealed class BrushPressureFilter
    {
        // EMA smoothing state
        private bool hasValue;
        private float filteredPressure;
        private float previousTime;

        // Mouse speed simulation state
        private bool hasMousePosition;
        private Vector2 lastMousePosition;
        private float lastMouseTime;

        // Stroke taper state
        private float strokeTotalDistance;
        private float strokeDistance;

        // Speed smoothing state
        private float smoothedSpeed;

        // Stroke speed tracking for exit taper
        private float lastStrokeSpeed;

        /// <summary>Reset all filter state at stroke start.</summary>
        public void Reset()
        {
            hasValue = false;
            filteredPressure = 0f;
            previousTime = 0f;
            hasMousePosition = false;
            strokeTotalDistance = 0f;
            strokeDistance = 0f;
            smoothedSpeed = 0f;
            lastStrokeSpeed = 0f;
        }

        /// <summary>Set estimated total stroke distance for taper calculation.</summary>
        public void SetStrokeTotalDistance(float totalDistance)
        {
            strokeTotalDistance = totalDistance;
        }

        /// <summary>Accumulate stroke distance for taper progress tracking.</summary>
        public void AddStrokeDistance(float distance)
        {
            strokeDistance += distance;
        }

        /// <summary>Current accumulated stroke distance.</summary>
        public float StrokeDistance => strokeDistance;

        /// <summary>Current stroke speed (pixels/sec) for exit taper.</summary>
        public float LastStrokeSpeed => lastStrokeSpeed;

        /// <summary>Update stroke speed for exit taper calculation.</summary>
        public void UpdateStrokeSpeed(float distance, float deltaTime)
        {
            if (deltaTime > 0.001f)
                lastStrokeSpeed = Mathf.Lerp(lastStrokeSpeed, distance / deltaTime, 0.4f);
        }

        /// <summary>
        /// Apply EMA pressure smoothing.
        /// EMA（指数移動平均）筆圧スムージングを適用
        /// </summary>
        /// <param name="rawPressure">Raw pressure value from pen tablet (0-1).</param>
        /// <param name="smoothingEnabled">Whether smoothing is enabled in settings.</param>
        /// <param name="smoothingStrength">Smoothing strength (0=no smoothing, 1=maximum smoothing).</param>
        /// <returns>Filtered pressure value.</returns>
        public float FilterPressure(float rawPressure, bool smoothingEnabled, float smoothingStrength, float deadZone = 0f)
        {
            // Apply dead zone: remap pressure above dead zone to 0-1 range
            // デッドゾーン適用: デッドゾーン以上の筆圧を0-1範囲にリマップ
            if (deadZone > 0f && rawPressure > 0f)
            {
                if (rawPressure <= deadZone)
                    rawPressure = 0f;
                else
                    rawPressure = (rawPressure - deadZone) / (1f - deadZone);
            }

            if (!smoothingEnabled || smoothingStrength <= 0f)
            {
                filteredPressure = rawPressure;
                hasValue = true;
                return rawPressure;
            }

            if (!hasValue)
            {
                filteredPressure = rawPressure;
                hasValue = true;
                previousTime = Time.realtimeSinceStartup;
                return rawPressure;
            }

            // EMA: higher strength = more smoothing (slower response)
            float alpha = Mathf.Clamp01(1f - smoothingStrength * 0.9f);
            filteredPressure = Mathf.Lerp(filteredPressure, rawPressure, alpha);
            previousTime = Time.realtimeSinceStartup;
            return filteredPressure;
        }

        /// <summary>
        /// Simulate pressure from mouse movement speed.
        /// マウス移動速度から筆圧をシミュレーション
        /// </summary>
        /// <param name="mousePosition">Current mouse position in pixels.</param>
        /// <param name="enabled">Whether mouse speed pressure is enabled.</param>
        /// <param name="speedMin">Minimum speed threshold (pixels/frame).</param>
        /// <param name="speedMax">Maximum speed threshold (pixels/frame).</param>
        /// <param name="speedCurve">Speed to pressure mapping curve.</param>
        /// <returns>Simulated pressure value (0-1).</returns>
        public float SimulateMousePressure(Vector2 mousePosition, bool enabled, float speedMin, float speedMax, AnimationCurve speedCurve)
        {
            if (!enabled || speedCurve == null)
                return 1f;

            if (!hasMousePosition)
            {
                hasMousePosition = true;
                lastMousePosition = mousePosition;
                lastMouseTime = Time.realtimeSinceStartup;
                return 1f;
            }

            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - lastMouseTime;
            if (deltaTime < 0.001f) deltaTime = 0.016f; // fallback ~60fps

            float distance = Vector2.Distance(mousePosition, lastMousePosition);
            float speed = distance / deltaTime;

            lastMousePosition = mousePosition;
            lastMouseTime = currentTime;

            // EMA smoothing on speed to reduce noise
            // 速度のEMAスムージングでノイズ軽減
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 0.3f);

            // Normalize speed to 0-1 range based on min/max thresholds
            float normalizedSpeed = Mathf.Clamp01((smoothedSpeed - speedMin) / Mathf.Max(speedMax - speedMin, 0.001f));

            // Evaluate curve: typically fast = low pressure, slow = high pressure
            return Mathf.Clamp01(speedCurve.Evaluate(normalizedSpeed));
        }

        /// <summary>
        /// Calculate entry/exit taper multiplier based on stroke distance and speed.
        /// ストローク距離と速度に基づく入り抜きテーパー乗数を計算
        /// Entry: pixel-based (first N pixels). Exit: speed-based (Clip Studio style).
        /// 入り: ピクセルベース。抜き: 速度ベース（クリスタ方式）。
        /// </summary>
        public float CalculateTaper(bool enabled, float entryPixels, float exitPixels,
            AnimationCurve entryCurve, AnimationCurve exitCurve, float currentSpeed = -1f)
        {
            if (!enabled) return 1f;

            // Entry taper: first entryPixels of stroke distance
            // 入りテーパー: ストローク開始からentryPixelsの距離
            if (entryPixels > 0.5f && strokeDistance < entryPixels && entryCurve != null)
            {
                float t = Mathf.Clamp01(strokeDistance / entryPixels);
                return Mathf.Clamp01(entryCurve.Evaluate(t));
            }

            // Exit taper: speed-based detection (Clip Studio style)
            // 抜きテーパー: 速度ベース検知（クリスタ方式）
            // When pen decelerates below threshold, begin tapering
            if (exitPixels > 0.5f && currentSpeed >= 0f && exitCurve != null)
            {
                // exitPixels is used as the speed threshold (pixels/sec)
                // Higher exitPixels = higher speed threshold = easier to trigger exit taper
                float speedThreshold = exitPixels * 8f;  // Convert to reasonable speed range
                if (currentSpeed < speedThreshold)
                {
                    float t = Mathf.Clamp01(currentSpeed / speedThreshold);
                    return Mathf.Clamp01(exitCurve.Evaluate(1f - t));
                }
            }

            return 1f;
        }
    }
}
