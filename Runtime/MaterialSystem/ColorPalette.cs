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

        [NonSerialized] private Dictionary<string, ColorEntry> _cache;

        private void RebuildCache()
        {
            _cache = new Dictionary<string, ColorEntry>(colors.Count);
            foreach (var entry in colors)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.name))
                    _cache[entry.name] = entry;
            }
        }

        public Color GetColor(string colorName)
        {
            if (_cache == null) RebuildCache();
            if (_cache.TryGetValue(colorName, out var entry))
                return entry.color;
            return Color.white;
        }

        public void SetColor(string colorName, Color color)
        {
            if (_cache == null) RebuildCache();
            if (_cache.TryGetValue(colorName, out var entry))
            {
                entry.color = color;
            }
            else
            {
                var newEntry = new ColorEntry { name = colorName, color = color };
                colors.Add(newEntry);
                _cache[colorName] = newEntry;
            }
        }
    }
}
