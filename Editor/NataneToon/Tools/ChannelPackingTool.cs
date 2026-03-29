using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// RGBA channel packing tool - combines up to 4 grayscale masks into one texture.
    /// RGBAチャンネルパッキングツール - 最大4つのグレースケールマスクを1テクスチャに統合
    /// </summary>
    internal class ChannelPackingTool : EditorWindow
    {
        private Texture2D channelR;
        private Texture2D channelG;
        private Texture2D channelB;
        private Texture2D channelA;
        private int outputSize = 512;
        private Texture2D preview;

        private string labelR = "Shadow Mask";
        private string labelG = "Emission Mask";
        private string labelB = "Rim Mask";
        private string labelA = "Outline Width";

        [MenuItem("Tools/Natane/Channel Packing Tool", false, 155)]
        public static void Open()
        {
            var w = GetWindow<ChannelPackingTool>(true,
                L("チャンネルパッキング", "Channel Packing"));
            w.minSize = new Vector2(420, 450);
            w.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(L("チャンネルパッキングツール", "Channel Packing Tool"),
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            EditorGUILayout.HelpBox(
                L("4つのグレースケールマスクをRGBAチャンネルに統合します。",
                  "Combines up to 4 grayscale masks into RGBA channels of one texture."),
                MessageType.Info);

            EditorGUILayout.Space(4);
            outputSize = EditorGUILayout.IntPopup(L("出力サイズ", "Output Size"), outputSize,
                new[] { "256", "512", "1024", "2048" }, new[] { 256, 512, 1024, 2048 });

            EditorGUILayout.Space(6);

            // Channel inputs
            DrawChannelSlot("R", ref channelR, ref labelR, new Color(1f, 0.3f, 0.3f));
            DrawChannelSlot("G", ref channelG, ref labelG, new Color(0.3f, 1f, 0.3f));
            DrawChannelSlot("B", ref channelB, ref labelB, new Color(0.3f, 0.3f, 1f));
            DrawChannelSlot("A", ref channelA, ref labelA, new Color(0.7f, 0.7f, 0.7f));

            EditorGUILayout.Space(8);

            // Preview
            if (preview != null)
            {
                EditorGUILayout.LabelField(L("プレビュー", "Preview"), EditorStyles.boldLabel);
                Rect r = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
                EditorGUI.DrawPreviewTexture(r, preview);
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("プレビュー更新", "Update Preview"), GUILayout.Height(28)))
                UpdatePreview();
            if (GUILayout.Button(L("PNGで保存", "Save as PNG"), GUILayout.Height(28)))
                SavePNG();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawChannelSlot(string channel, ref Texture2D tex, ref string label, Color tint)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = tint;
            EditorGUILayout.LabelField(channel, new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 16, alignment = TextAnchor.MiddleCenter }, GUILayout.Width(24));
            GUI.backgroundColor = prevBg;

            EditorGUILayout.BeginVertical();
            label = EditorGUILayout.TextField(label, GUILayout.Width(150));
            tex = (Texture2D)EditorGUILayout.ObjectField(tex, typeof(Texture2D), false, GUILayout.Width(220));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void UpdatePreview()
        {
            if (preview != null) DestroyImmediate(preview);
            preview = Pack(outputSize);
            Repaint();
        }

        public Texture2D Pack(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float r = SampleChannel(channelR, u, v);
                    float g = SampleChannel(channelG, u, v);
                    float b = SampleChannel(channelB, u, v);
                    float a = SampleChannel(channelA, u, v);
                    tex.SetPixel(x, y, new Color(r, g, b, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private float SampleChannel(Texture2D source, float u, float v)
        {
            if (source == null) return 0f;
            Color c = source.GetPixelBilinear(u, v);
            return (c.r + c.g + c.b) / 3f;
        }

        private void SavePNG()
        {
            var packed = Pack(outputSize);
            if (packed == null) return;
            string path = EditorUtility.SaveFilePanel("Save Packed Texture", "Assets", "packed_mask", "png");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, packed.EncodeToPNG());
                AssetDatabase.Refresh();
            }
            DestroyImmediate(packed);
        }

        private void OnDestroy()
        {
            if (preview != null) DestroyImmediate(preview);
        }
    }
}
