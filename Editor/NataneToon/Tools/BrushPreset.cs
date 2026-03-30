using UnityEngine;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    [System.Serializable]
    internal class BrushPreset
    {
        public string name;
        public float size = 20f;
        public float hardness = 0.8f;
        public float opacity = 1f;
        public float flow = 1f;
        public bool pressureFlowEnabled = false;
        public float strength = 1f;
        public float paintAlpha = 1f;
        public Color paintColor = Color.white;
        public bool colorMode = false;
        public BrushMode mode = BrushMode.Paint;
        public bool pressureOpacityEnabled = true;
        public bool pressureSizeEnabled = false;
        // Note: AnimationCurves can't be serialized to JSON easily, so store key presets as enum
        public PressureCurveType pressureCurveType = PressureCurveType.Linear;

        // Natural feel settings / 自然な描き心地設定
        public bool pressureHardnessEnabled = false;
        public bool entryExitEnabled = false;
        public float entryLength = 20f;
        public float exitLength = 20f;
        public float pressureSizeMin = 0f;
        public float pressureDeadZone = 0f;
        public bool velocitySizeEnabled = false;
        public float velocitySizeInfluence = 0.3f;
        public BrushStabilizerMode stabilizerMode = BrushStabilizerMode.Off;
        public float stabilizerStrength = 0.45f;
        public float stabilizerDelayDistance = 0f;
        public bool mouseSpeedPressureEnabled = false;

        // Krita-compatible sensor/parameter mapping / Krita互換センサー/パラメータマッピング
        public bool tiltSizeEnabled = false;
        public float tiltSizeInfluence = 0.5f;
        public bool tiltRotationEnabled = false;
        public bool drawingAngleRotationEnabled = false;
        public bool speedOpacityEnabled = false;
        public float speedOpacityInfluence = 0.3f;
        public bool speedHardnessEnabled = false;
        public float speedHardnessInfluence = 0.3f;
        public float strokeRandomSizeJitter = 0f;
        public float strokeRandomOpacityJitter = 0f;
        public bool airbrushMode = false;
        public float airbrushRate = 0.05f;

        public enum PressureCurveType { Linear, SCurve, Logarithmic, Exponential, Natural }

        public void ApplyTo(BrushSettings settings)
        {
            settings.size = size;
            settings.hardness = hardness;
            settings.opacity = opacity;
            settings.strength = strength;
            settings.paintAlpha = paintAlpha;
            settings.paintColor = paintColor;
            settings.colorMode = colorMode;
            settings.mode = mode;
            settings.pressureOpacityEnabled = pressureOpacityEnabled;
            settings.pressureSizeEnabled = pressureSizeEnabled;
            settings.flow = flow;
            settings.pressureFlowEnabled = pressureFlowEnabled;
            settings.pressureFlowCurve = pressureFlowEnabled
                ? CreateCurve(pressureCurveType) : AnimationCurve.Linear(0f, 0f, 1f, 1f);
            // Apply curve type
            settings.pressureOpacityCurve = CreateCurve(pressureCurveType);
            settings.pressureSizeCurve = CreateCurve(pressureCurveType);

            // Natural feel settings / 自然な描き心地設定
            settings.pressureHardnessEnabled = pressureHardnessEnabled;
            settings.pressureHardnessCurve = pressureHardnessEnabled
                ? CreateCurve(pressureCurveType) : AnimationCurve.Linear(0f, 0f, 1f, 1f);
            settings.entryExitEnabled = entryExitEnabled;
            settings.entryLength = entryLength;
            settings.exitLength = exitLength;
            settings.pressureSizeMin = pressureSizeMin;
            settings.pressureDeadZone = pressureDeadZone;
            settings.velocitySizeEnabled = velocitySizeEnabled;
            settings.velocitySizeInfluence = velocitySizeInfluence;
            settings.stabilizer.mode = stabilizerMode;
            settings.stabilizer.strength = stabilizerStrength;
            settings.stabilizer.delayDistance = stabilizerDelayDistance;
            settings.mouseSpeedPressureEnabled = mouseSpeedPressureEnabled;

            // Krita-compatible sensor/parameter mapping / Krita互換センサー/パラメータマッピング
            settings.tiltSizeEnabled = tiltSizeEnabled;
            settings.tiltSizeInfluence = tiltSizeInfluence;
            settings.tiltRotationEnabled = tiltRotationEnabled;
            settings.drawingAngleRotationEnabled = drawingAngleRotationEnabled;
            settings.speedOpacityEnabled = speedOpacityEnabled;
            settings.speedOpacityInfluence = speedOpacityInfluence;
            settings.speedHardnessEnabled = speedHardnessEnabled;
            settings.speedHardnessInfluence = speedHardnessInfluence;
            settings.strokeRandomSizeJitter = strokeRandomSizeJitter;
            settings.strokeRandomOpacityJitter = strokeRandomOpacityJitter;
            settings.airbrushMode = airbrushMode;
            settings.airbrushRate = airbrushRate;
        }

        public static BrushPreset CreateFrom(BrushSettings settings, string name)
        {
            return new BrushPreset
            {
                name = name,
                size = settings.size,
                hardness = settings.hardness,
                opacity = settings.opacity,
                strength = settings.strength,
                paintAlpha = settings.paintAlpha,
                mode = settings.mode,
                pressureOpacityEnabled = settings.pressureOpacityEnabled,
                pressureSizeEnabled = settings.pressureSizeEnabled,
                flow = settings.flow,
                pressureFlowEnabled = settings.pressureFlowEnabled,
                paintColor = settings.paintColor,
                colorMode = settings.colorMode,
                pressureHardnessEnabled = settings.pressureHardnessEnabled,
                entryExitEnabled = settings.entryExitEnabled,
                entryLength = settings.entryLength,
                exitLength = settings.exitLength,
                pressureSizeMin = settings.pressureSizeMin,
                pressureDeadZone = settings.pressureDeadZone,
                velocitySizeEnabled = settings.velocitySizeEnabled,
                velocitySizeInfluence = settings.velocitySizeInfluence,
                stabilizerMode = settings.stabilizer.mode,
                stabilizerStrength = settings.stabilizer.strength,
                stabilizerDelayDistance = settings.stabilizer.delayDistance,
                mouseSpeedPressureEnabled = settings.mouseSpeedPressureEnabled,
                // Krita-compatible sensor/parameter mapping
                tiltSizeEnabled = settings.tiltSizeEnabled,
                tiltSizeInfluence = settings.tiltSizeInfluence,
                tiltRotationEnabled = settings.tiltRotationEnabled,
                drawingAngleRotationEnabled = settings.drawingAngleRotationEnabled,
                speedOpacityEnabled = settings.speedOpacityEnabled,
                speedOpacityInfluence = settings.speedOpacityInfluence,
                speedHardnessEnabled = settings.speedHardnessEnabled,
                speedHardnessInfluence = settings.speedHardnessInfluence,
                strokeRandomSizeJitter = settings.strokeRandomSizeJitter,
                strokeRandomOpacityJitter = settings.strokeRandomOpacityJitter,
                airbrushMode = settings.airbrushMode,
                airbrushRate = settings.airbrushRate,
            };
        }

        public static AnimationCurve CreateCurve(PressureCurveType type)
        {
            switch (type)
            {
                case PressureCurveType.SCurve:
                    return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                case PressureCurveType.Logarithmic:
                    return new AnimationCurve(
                        new Keyframe(0f, 0f, 3f, 3f),
                        new Keyframe(1f, 1f, 0.2f, 0.2f));
                case PressureCurveType.Exponential:
                    return new AnimationCurve(
                        new Keyframe(0f, 0f, 0.2f, 0.2f),
                        new Keyframe(1f, 1f, 3f, 3f));
                case PressureCurveType.Natural:
                    return new AnimationCurve(
                        new Keyframe(0f, 0f, 2f, 2f),
                        new Keyframe(0.5f, 0.65f, 0.8f, 0.8f),
                        new Keyframe(1f, 1f, 0.4f, 0.4f));
                default: // Linear
                    return AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
        }

        public static List<BrushPreset> CreateDefaultPresets()
        {
            return new List<BrushPreset>
            {
                // === Clip Studio互換プリセット ===

                // Gペン: クリスタのGペン互換。筆圧でサイズのみ変化（不透明度100%固定）
                new BrushPreset
                {
                    name = "Gペン",
                    size = 8f, hardness = 1f, opacity = 1f, strength = 1f,
                    pressureOpacityEnabled = false,
                    pressureSizeEnabled = true,
                    pressureSizeMin = 0f,
                    pressureCurveType = PressureCurveType.Natural,
                    pressureDeadZone = 0.01f,
                    mouseSpeedPressureEnabled = false,
                },
                // 丸ペン: クリスタの丸ペン互換。Gペンより細く、急なカーブ
                new BrushPreset
                {
                    name = "丸ペン",
                    size = 3f, hardness = 1f, opacity = 1f, strength = 1f,
                    pressureOpacityEnabled = false,
                    pressureSizeEnabled = true,
                    pressureSizeMin = 0f,
                    pressureCurveType = PressureCurveType.Logarithmic,
                    pressureDeadZone = 0.01f,
                    mouseSpeedPressureEnabled = false,
                },

                // === スタンダードプリセット ===

                // ソフト丸: 筆タッチ。筆圧でサイズ+不透明度が変化
                new BrushPreset
                {
                    name = "ソフト丸",
                    size = 20f, hardness = 0.3f, opacity = 0.8f, strength = 1f,
                    pressureOpacityEnabled = true,
                    pressureSizeEnabled = true,
                    pressureSizeMin = 0.3f,
                    pressureCurveType = PressureCurveType.Natural,
                    pressureHardnessEnabled = true,
                    entryExitEnabled = true, entryLength = 15f, exitLength = 15f,
                    pressureDeadZone = 0.02f,
                    mouseSpeedPressureEnabled = false,
                },
                // ハード丸: ペンタッチ。筆圧でサイズ変化
                new BrushPreset
                {
                    name = "ハード丸",
                    size = 15f, hardness = 0.95f, opacity = 1f, strength = 1f,
                    pressureOpacityEnabled = false,
                    pressureSizeEnabled = true,
                    pressureSizeMin = 0.1f,
                    pressureCurveType = PressureCurveType.Natural,
                    entryExitEnabled = true, entryLength = 10f, exitLength = 10f,
                    pressureDeadZone = 0.02f,
                    mouseSpeedPressureEnabled = false,
                },
                // エアブラシ: ふんわり塗り（時間ベーススペーシング付き）
                new BrushPreset
                {
                    name = "エアブラシ",
                    size = 40f, hardness = 0.1f, opacity = 0.3f, flow = 0.5f, pressureFlowEnabled = true, strength = 0.5f,
                    pressureOpacityEnabled = true,
                    pressureSizeEnabled = true,
                    pressureSizeMin = 0.4f,
                    pressureCurveType = PressureCurveType.SCurve,
                    airbrushMode = true,
                    airbrushRate = 0.05f,
                },
                // 鉛筆: 細い線。筆圧でサイズ+硬さ+不透明度変化
                new BrushPreset
                {
                    name = "鉛筆",
                    size = 10f, hardness = 0.5f, opacity = 1f, flow = 0.8f, pressureFlowEnabled = true, strength = 1f,
                    pressureOpacityEnabled = true,
                    pressureSizeEnabled = true,
                    pressureSizeMin = 0.2f,
                    pressureCurveType = PressureCurveType.Natural,
                    pressureHardnessEnabled = true,
                    entryExitEnabled = true, entryLength = 12f, exitLength = 12f,
                    pressureDeadZone = 0.02f,
                    mouseSpeedPressureEnabled = false,
                },

                // === 消しゴムプリセット ===

                // ソフト消しゴム
                new BrushPreset
                {
                    name = "ソフト消しゴム",
                    size = 25f, hardness = 0.2f, opacity = 0.7f, mode = BrushMode.Erase,
                    pressureOpacityEnabled = true,
                    pressureSizeEnabled = true,
                    pressureSizeMin = 0.3f,
                    pressureCurveType = PressureCurveType.SCurve,
                    mouseSpeedPressureEnabled = false,
                },
                // ハード消しゴム
                new BrushPreset
                {
                    name = "ハード消しゴム",
                    size = 15f, hardness = 0.9f, opacity = 1f, mode = BrushMode.Erase,
                    pressureCurveType = PressureCurveType.Linear,
                },
            };
        }
    }

    [System.Serializable]
    internal class BrushPresetCollection
    {
        public List<BrushPreset> presets = new List<BrushPreset>();

        public string ToJson() => JsonUtility.ToJson(this);
        public static BrushPresetCollection FromJson(string json)
        {
            try { return JsonUtility.FromJson<BrushPresetCollection>(json); }
            catch { return new BrushPresetCollection { presets = BrushPreset.CreateDefaultPresets() }; }
        }
    }
}
