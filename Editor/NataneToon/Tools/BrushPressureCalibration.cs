using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Editor window for brush pressure calibration.
    /// ブラシ筆圧キャリブレーション用エディターウィンドウ
    /// </summary>
    internal class BrushPressureCalibration : EditorWindow
    {
        private List<float> recordedPressures = new List<float>();
        private bool isRecording;
        private float recordedMin = 1f;
        private float recordedMax = 0f;
        private AnimationCurve resultCurve;
        private AnimationCurve targetCurve; // Reference to the curve to update

        // Callback to apply the generated curve
        private System.Action<AnimationCurve> onCalibrationComplete;

        public static void Open(System.Action<AnimationCurve> callback)
        {
            var window = GetWindow<BrushPressureCalibration>(true,
                L("筆圧キャリブレーション", "Pressure Calibration"), true);
            window.minSize = new Vector2(400, 350);
            window.maxSize = new Vector2(500, 450);
            window.onCalibrationComplete = callback;
            window.Reset();
            window.ShowUtility();
        }

        private void Reset()
        {
            recordedPressures.Clear();
            isRecording = false;
            recordedMin = 1f;
            recordedMax = 0f;
            resultCurve = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(
                L("筆圧キャリブレーション", "Pressure Calibration"),
                EditorStyles.boldLabel);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                L("下のエリアでペンを使ってストロークを描いてください。\n軽いタッチから強い筆圧まで様々な力で描くことで、\nあなたのペンタブレットに最適な筆圧カーブを生成します。",
                  "Draw strokes in the area below with your pen.\nVary pressure from light to heavy to generate\nan optimal pressure curve for your tablet."),
                MessageType.Info);

            EditorGUILayout.Space(4);

            // Drawing area
            Rect drawArea = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.Height(150), GUILayout.ExpandWidth(true));
            drawArea = EditorGUI.IndentedRect(drawArea);
            EditorGUI.DrawRect(drawArea, new Color(0.15f, 0.15f, 0.15f));

            // Draw recorded pressure as a graph
            if (recordedPressures.Count > 1)
            {
                Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
                for (int i = 1; i < recordedPressures.Count; i++)
                {
                    float x0 = drawArea.x + (float)(i - 1) / recordedPressures.Count * drawArea.width;
                    float x1 = drawArea.x + (float)i / recordedPressures.Count * drawArea.width;
                    float y0 = drawArea.yMax - recordedPressures[i - 1] * drawArea.height;
                    float y1 = drawArea.yMax - recordedPressures[i] * drawArea.height;
                    Handles.DrawLine(new Vector3(x0, y0, 0), new Vector3(x1, y1, 0));
                }
            }

            // Handle pen input
            Event e = Event.current;
            if (drawArea.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    isRecording = true;
                    recordedPressures.Clear();
                    recordedMin = 1f;
                    recordedMax = 0f;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && e.button == 0 && isRecording)
                {
                    float pressure = e.pressure > 0.001f ? e.pressure : 0.5f;
                    recordedPressures.Add(pressure);
                    recordedMin = Mathf.Min(recordedMin, pressure);
                    recordedMax = Mathf.Max(recordedMax, pressure);
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseUp && e.button == 0 && isRecording)
                {
                    isRecording = false;
                    if (recordedPressures.Count > 5)
                        GenerateCurve();
                    e.Use();
                    Repaint();
                }
            }

            EditorGUILayout.Space(4);

            // Status
            string statusText;
            if (isRecording)
                statusText = L($"記録中... ({recordedPressures.Count} サンプル)", $"Recording... ({recordedPressures.Count} samples)");
            else if (recordedPressures.Count > 0)
                statusText = L($"記録完了: 最小={recordedMin:F3}, 最大={recordedMax:F3}", $"Recorded: min={recordedMin:F3}, max={recordedMax:F3}");
            else
                statusText = L("ペンでストロークを描いてください", "Draw a stroke with your pen");
            EditorGUILayout.LabelField(statusText, EditorStyles.miniLabel);

            // Result curve
            if (resultCurve != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(
                    L("生成された補正カーブ", "Generated Correction Curve"),
                    EditorStyles.boldLabel);
                resultCurve = EditorGUILayout.CurveField(resultCurve, GUILayout.Height(60));
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(L("リセット", "Reset")))
            {
                Reset();
                Repaint();
            }

            GUI.enabled = resultCurve != null;
            if (GUILayout.Button(L("適用", "Apply")))
            {
                onCalibrationComplete?.Invoke(resultCurve);
                Close();
            }
            GUI.enabled = true;

            if (GUILayout.Button(L("キャンセル", "Cancel")))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void GenerateCurve()
        {
            if (recordedPressures.Count < 5) return;

            // Generate a correction curve that maps the tablet's actual pressure range
            // to a normalized 0-1 output
            float range = recordedMax - recordedMin;
            if (range < 0.01f)
            {
                // Not enough variation, use linear
                resultCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                return;
            }

            // Build a curve that normalizes the detected pressure range
            // and applies a slight S-curve for more natural feel
            var keys = new Keyframe[5];

            // Map: input pressure -> desired output
            // We create a curve that stretches the detected range to full 0-1
            keys[0] = new Keyframe(0f, 0f);
            keys[1] = new Keyframe(recordedMin, 0.05f);
            keys[2] = new Keyframe(Mathf.Lerp(recordedMin, recordedMax, 0.5f), 0.5f);
            keys[3] = new Keyframe(recordedMax, 0.95f);
            keys[4] = new Keyframe(1f, 1f);

            resultCurve = new AnimationCurve(keys);

            // Smooth tangents for natural feel
            for (int i = 0; i < resultCurve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(resultCurve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(resultCurve, i, AnimationUtility.TangentMode.Auto);
            }
        }
    }
}
