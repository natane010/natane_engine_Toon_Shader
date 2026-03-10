using System.Collections.Generic;
using UnityEngine;

namespace NataneToon.Editor
{
    internal sealed class MaskTextureHistoryEntry
    {
        public readonly MaskTextureLayer layer;
        public readonly Color[] beforePixels;
        public readonly Color[] afterPixels;
        public readonly string label;

        public MaskTextureHistoryEntry(MaskTextureLayer layer, Color[] beforePixels, Color[] afterPixels, string label)
        {
            this.layer = layer;
            this.beforePixels = MaskTextureHistory.ClonePixels(beforePixels);
            this.afterPixels = MaskTextureHistory.ClonePixels(afterPixels);
            this.label = label ?? "Paint";
        }

        public bool CanApply
        {
            get
            {
                return layer != null
                    && layer.pixels != null
                    && beforePixels != null
                    && afterPixels != null
                    && layer.pixels.Length == beforePixels.Length
                    && layer.pixels.Length == afterPixels.Length;
            }
        }

        public void ApplyBefore()
        {
            if (!CanApply)
                return;

            System.Array.Copy(beforePixels, layer.pixels, beforePixels.Length);
        }

        public void ApplyAfter()
        {
            if (!CanApply)
                return;

            System.Array.Copy(afterPixels, layer.pixels, afterPixels.Length);
        }
    }

    internal sealed class MaskTextureHistory
    {
        private const int MaxEntries = 64;

        private readonly List<MaskTextureHistoryEntry> entries = new List<MaskTextureHistoryEntry>();
        private int cursor;

        public bool CanUndo => cursor > 0;
        public bool CanRedo => cursor < entries.Count;

        public string NextUndoLabel => CanUndo ? entries[cursor - 1].label : string.Empty;
        public string NextRedoLabel => CanRedo ? entries[cursor].label : string.Empty;

        public void Clear()
        {
            entries.Clear();
            cursor = 0;
        }

        public bool Record(MaskTextureLayer layer, Color[] beforePixels, Color[] afterPixels, string label)
        {
            if (layer == null || beforePixels == null || afterPixels == null)
                return false;

            if (beforePixels.Length == 0 || afterPixels.Length == 0)
                return false;

            if (beforePixels.Length != afterPixels.Length)
                return false;

            if (PixelsEqual(beforePixels, afterPixels))
                return false;

            if (cursor < entries.Count)
                entries.RemoveRange(cursor, entries.Count - cursor);

            entries.Add(new MaskTextureHistoryEntry(layer, beforePixels, afterPixels, label));
            cursor = entries.Count;

            if (entries.Count > MaxEntries)
            {
                entries.RemoveAt(0);
                cursor = Mathf.Max(0, cursor - 1);
            }

            return true;
        }

        public bool Push(MaskTextureLayer layer, string label, Color[] beforePixels, Color[] afterPixels)
        {
            return Record(layer, beforePixels, afterPixels, label);
        }

        public bool Undo()
        {
            return Undo(null);
        }

        public bool Undo(MaskLayerStack layerStack)
        {
            if (!CanUndo)
                return false;

            if (layerStack != null && !layerStack.Layers.Contains(entries[cursor - 1].layer))
                return false;

            cursor--;
            entries[cursor].ApplyBefore();
            return true;
        }

        public bool Redo()
        {
            return Redo(null);
        }

        public bool Redo(MaskLayerStack layerStack)
        {
            if (!CanRedo)
                return false;

            if (layerStack != null && !layerStack.Layers.Contains(entries[cursor].layer))
                return false;

            entries[cursor].ApplyAfter();
            cursor++;
            return true;
        }

        public static Color[] ClonePixels(Color[] source)
        {
            if (source == null)
                return null;

            Color[] clone = new Color[source.Length];
            System.Array.Copy(source, clone, source.Length);
            return clone;
        }

        private static bool PixelsEqual(Color[] a, Color[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }
    }
}
