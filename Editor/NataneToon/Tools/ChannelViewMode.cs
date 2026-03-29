using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    internal enum ChannelViewMode { RGBA, Red, Green, Blue, Alpha }

    /// <summary>
    /// Channel view mode for canvas display filtering.
    /// キャンバス表示フィルタリング用チャンネル表示モード
    /// </summary>
    internal static class ChannelView
    {
        public static ChannelViewMode CurrentMode { get; set; } = ChannelViewMode.RGBA;

        /// <summary>
        /// Apply channel view filter to a pixel array for display.
        /// 表示用にピクセル配列にチャンネルビューフィルタを適用
        /// Creates a copy; does not modify the source.
        /// </summary>
        public static Color[] ApplyView(Color[] source, int width, int height)
        {
            return ApplyView(source, width, height, null);
        }

        public static Color[] ApplyView(Color[] source, int width, int height, Color[] destination)
        {
            if (source == null || CurrentMode == ChannelViewMode.RGBA)
                return source;

            if (destination == null || destination.Length != source.Length)
                destination = new Color[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                Color c = source[i];
                switch (CurrentMode)
                {
                    case ChannelViewMode.Red:
                        destination[i] = new Color(c.r, c.r, c.r, 1f);
                        break;
                    case ChannelViewMode.Green:
                        destination[i] = new Color(c.g, c.g, c.g, 1f);
                        break;
                    case ChannelViewMode.Blue:
                        destination[i] = new Color(c.b, c.b, c.b, 1f);
                        break;
                    case ChannelViewMode.Alpha:
                        destination[i] = new Color(c.a, c.a, c.a, 1f);
                        break;
                }
            }

            return destination;
        }

        /// <summary>
        /// Draw channel view toggle buttons in the toolbar.
        /// ツールバーにチャンネル表示トグルボタンを描画
        /// </summary>
        public static void DrawToolbarButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(L("CH", "CH"), EditorStyles.miniLabel, GUILayout.Width(20));

            string[] labels = { "RGBA", "R", "G", "B", "A" };
            ChannelViewMode[] modes = {
                ChannelViewMode.RGBA, ChannelViewMode.Red,
                ChannelViewMode.Green, ChannelViewMode.Blue, ChannelViewMode.Alpha
            };
            Color[] colors = {
                Color.white,
                new Color(1f, 0.3f, 0.3f),
                new Color(0.3f, 1f, 0.3f),
                new Color(0.3f, 0.3f, 1f),
                Color.gray
            };

            for (int i = 0; i < labels.Length; i++)
            {
                Color prev = GUI.backgroundColor;
                if (CurrentMode == modes[i])
                    GUI.backgroundColor = colors[i];
                if (GUILayout.Button(labels[i], EditorStyles.miniButton, GUILayout.Width(36)))
                    CurrentMode = modes[i];
                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
