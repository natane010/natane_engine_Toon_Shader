using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    internal enum BrushStabilizerMode
    {
        Off,
        Basic,
        Stabilized
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
            float baseFactor = settings.mode == BrushStabilizerMode.Basic ? 0.55f : 0.28f;
            float lerpFactor = Mathf.Clamp01(Mathf.Lerp(0.08f, baseFactor, strength));

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
                    settings.strength = EditorGUILayout.Slider("Strength", settings.strength, 0.05f, 1f);
                }
            }
        }
    }
}
