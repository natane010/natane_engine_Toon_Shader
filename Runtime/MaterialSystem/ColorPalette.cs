using UnityEngine;
using System;
using System.Collections.Generic;

namespace NataneToon.MaterialSystem
{
    /// <summary>
    /// Color palette for project-wide color management
    /// Allows synchronization of colors across materials
    /// プロジェクト全体のカラー管理用のカラーパレット
    /// マテリアル間での色の同期を可能にする
    /// </summary>
    [CreateAssetMenu(fileName = "New Color Palette", menuName = "Natane/Color Palette", order = 2)]
    public class ColorPalette : ScriptableObject
    {
        [Serializable]
        public class ColorEntry
        {
            public string name = "New Color";
            public Color color = Color.white;
            public string description = "";
        }

        public List<ColorEntry> colors = new List<ColorEntry>();

        public Color GetColor(string colorName)
        {
            var entry = colors.Find(c => c.name == colorName);
            return entry != null ? entry.color : Color.white;
        }

        public void SetColor(string colorName, Color color)
        {
            var entry = colors.Find(c => c.name == colorName);
            if (entry != null)
            {
                entry.color = color;
            }
            else
            {
                colors.Add(new ColorEntry { name = colorName, color = color });
            }
        }
    }
}
