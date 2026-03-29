using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    [System.Serializable]
    internal class ShortcutBinding
    {
        public string actionId;
        public string displayName;
        public string category; // カテゴリ分け (Tools, Color, Canvas, Edit, etc.)
        public KeyCode keyCode;
        public bool ctrl;
        public bool shift;
        public bool alt;

        // Default values for reset / デフォルト値（リセット用）
        [System.NonSerialized] public KeyCode defaultKeyCode;
        [System.NonSerialized] public bool defaultCtrl;
        [System.NonSerialized] public bool defaultShift;
        [System.NonSerialized] public bool defaultAlt;

        public string ToDisplayString()
        {
            var sb = new System.Text.StringBuilder();
            if (ctrl)  sb.Append("Ctrl+");
            if (shift) sb.Append("Shift+");
            if (alt)   sb.Append("Alt+");
            sb.Append(keyCode.ToString());
            return sb.ToString();
        }

        public bool IsModified =>
            keyCode != defaultKeyCode || ctrl != defaultCtrl ||
            shift != defaultShift || alt != defaultAlt;

        public void ResetToDefault()
        {
            keyCode = defaultKeyCode;
            ctrl = defaultCtrl;
            shift = defaultShift;
            alt = defaultAlt;
        }

        public void StoreDefaults()
        {
            defaultKeyCode = keyCode;
            defaultCtrl = ctrl;
            defaultShift = shift;
            defaultAlt = alt;
        }
    }

    [System.Serializable]
    internal sealed class MaskTextureShortcutProfile
    {
        public float brushSizeDragSensitivity = 0.25f;
        public float brushOpacityDragSensitivity = 0.005f;
        public bool allowMiddleMousePan = true;
        public bool allowSpacePan = true;
        public bool allowAltLeftPan = true;

        public System.Collections.Generic.List<ShortcutBinding> bindings = new System.Collections.Generic.List<ShortcutBinding>();

        public static MaskTextureShortcutProfile CreateDefault()
        {
            var p = new MaskTextureShortcutProfile();
            string catEdit = "Edit";
            string catTool = "Tools";
            string catBrush = "Brush";
            string catCanvas = "Canvas";
            string catColor = "Color";

            p.bindings = new System.Collections.Generic.List<ShortcutBinding>
            {
                // Edit
                new ShortcutBinding { actionId = "Undo", displayName = "Undo", category = catEdit, keyCode = KeyCode.Z, ctrl = true },
                new ShortcutBinding { actionId = "Redo", displayName = "Redo", category = catEdit, keyCode = KeyCode.Y, ctrl = true },
                new ShortcutBinding { actionId = "Save", displayName = "Save Project", category = catEdit, keyCode = KeyCode.S, ctrl = true },
                new ShortcutBinding { actionId = "Open", displayName = "Open Project", category = catEdit, keyCode = KeyCode.O, ctrl = true },
                // Tools
                new ShortcutBinding { actionId = "ToolBrush", displayName = "Brush Tool", category = catTool, keyCode = KeyCode.B },
                new ShortcutBinding { actionId = "ToolEraser", displayName = "Eraser Tool", category = catTool, keyCode = KeyCode.E },
                new ShortcutBinding { actionId = "Eyedropper", displayName = "Eyedropper", category = catTool, keyCode = KeyCode.I },
                new ShortcutBinding { actionId = "QuickMask", displayName = "Quick Mask", category = catTool, keyCode = KeyCode.Q },
                // Brush
                new ShortcutBinding { actionId = "BrushSizeUp", displayName = "Brush Size Up", category = catBrush, keyCode = KeyCode.RightBracket },
                new ShortcutBinding { actionId = "BrushSizeDown", displayName = "Brush Size Down", category = catBrush, keyCode = KeyCode.LeftBracket },
                new ShortcutBinding { actionId = "FlipHorizontal", displayName = "Flip Horizontal", category = catBrush, keyCode = KeyCode.H },
                // Canvas
                new ShortcutBinding { actionId = "FitCanvas", displayName = "Fit Canvas", category = catCanvas, keyCode = KeyCode.F },
                new ShortcutBinding { actionId = "ToggleUV", displayName = "Toggle UV", category = catCanvas, keyCode = KeyCode.U },
                new ShortcutBinding { actionId = "ZoomIn", displayName = "Zoom In", category = catCanvas, keyCode = KeyCode.Equals, ctrl = true },
                new ShortcutBinding { actionId = "ZoomOut", displayName = "Zoom Out", category = catCanvas, keyCode = KeyCode.Minus, ctrl = true },
                // Color
                new ShortcutBinding { actionId = "SwapColors", displayName = "Swap FG/BG", category = catColor, keyCode = KeyCode.X },
                new ShortcutBinding { actionId = "DefaultColors", displayName = "Default Colors", category = catColor, keyCode = KeyCode.D },
                // System
                new ShortcutBinding { actionId = "CommandPalette", displayName = "Command Palette", category = catEdit, keyCode = KeyCode.P, ctrl = true, shift = true },
                new ShortcutBinding { actionId = "Help", displayName = "Help", category = catEdit, keyCode = KeyCode.F1 },
            };

            // Store defaults for reset functionality
            foreach (var b in p.bindings) b.StoreDefaults();
            return p;
        }

        public static MaskTextureShortcutProfile CreatePhotoshopLike()
        {
            var p = CreateDefault();
            // Override Redo to Ctrl+Shift+Z
            foreach (var b in p.bindings)
            {
                if (b.actionId == "Redo") { b.keyCode = KeyCode.Z; b.ctrl = true; b.shift = true; }
                else if (b.actionId == "FitCanvas") { b.keyCode = KeyCode.Alpha0; b.ctrl = true; }
            }
            return p;
        }

        public static MaskTextureShortcutProfile CreateClipStudioLike()
        {
            var p = CreateDefault();
            foreach (var b in p.bindings)
            {
                if (b.actionId == "FitCanvas") { b.keyCode = KeyCode.Alpha0; b.ctrl = true; }
                else if (b.actionId == "Redo") { b.keyCode = KeyCode.Z; b.ctrl = true; b.shift = true; }
            }
            return p;
        }

        public string ToJson() => JsonUtility.ToJson(this);
        public static MaskTextureShortcutProfile FromJson(string json)
        {
            try { return JsonUtility.FromJson<MaskTextureShortcutProfile>(json); }
            catch { return CreateDefault(); }
        }
    }

    [System.Serializable]
    internal sealed class MaskTextureShortcutState
    {
        public bool isAdjustingBrush;
        public bool isSpacePanHeld;
        public Vector2 adjustStartMousePosition;
        public float adjustStartSize;
        public float adjustStartOpacity;
        public bool hasLineAnchor;
        public Vector2 lineAnchorPixel;

        public void ResetTransient()
        {
            isAdjustingBrush = false;
            isSpacePanHeld = false;
        }
    }

    internal static class MaskTextureShortcutUtility
    {
        public static void UpdateKeyState(Event e, MaskTextureShortcutProfile profile, MaskTextureShortcutState state)
        {
            if (e == null || state == null || profile == null)
                return;

            if (EditorGUIUtility.editingTextField)
                return;

            if (!profile.allowSpacePan)
                return;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            {
                state.isSpacePanHeld = true;
            }
            else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Space)
            {
                state.isSpacePanHeld = false;
            }
        }

        public static bool IsPanEvent(Event e, MaskTextureShortcutProfile profile, MaskTextureShortcutState state)
        {
            if (e == null || profile == null || state == null)
                return false;

            return (profile.allowMiddleMousePan && e.button == 2)
                || (profile.allowSpacePan && state.isSpacePanHeld && e.button == 0)
                || (profile.allowAltLeftPan && e.alt && e.button == 0);
        }

        public static bool IsZoomWheelEvent(Event e, MaskTextureShortcutProfile profile)
        {
            return e != null && e.type == EventType.ScrollWheel && IsActionKey(e);
        }

        public static bool IsBrushAdjustStart(Event e, MaskTextureShortcutProfile profile)
        {
            return e != null && e.type == EventType.MouseDown && e.alt && e.button == 1;
        }

        public static bool IsPickerEvent(Event e, MaskTextureShortcutProfile profile)
        {
            return e != null && e.type == EventType.MouseDown && e.button == 0 && IsActionKey(e);
        }

        public static bool IsLineEvent(Event e, MaskTextureShortcutProfile profile)
        {
            return e != null && e.type == EventType.MouseDown && e.button == 0 && e.shift && !IsActionKey(e);
        }

        public static bool IsActionKey(Event e)
        {
            return e != null && (e.control || e.command);
        }

        public static bool MatchesBinding(Event e, ShortcutBinding binding)
        {
            if (binding == null || e.keyCode != binding.keyCode) return false;
            if (binding.ctrl  && !(e.control || e.command)) return false;
            if (binding.shift && !e.shift) return false;
            if (binding.alt   && !e.alt)   return false;
            return true;
        }

        public static ShortcutBinding FindBinding(MaskTextureShortcutProfile profile, string actionId)
        {
            if (profile?.bindings == null) return null;
            foreach (var b in profile.bindings)
                if (b.actionId == actionId) return b;
            return null;
        }

        /// <summary>
        /// Find duplicate binding (same key combo assigned to different action).
        /// 重複バインディング検出（同じキーコンボが別アクションに割り当てられている）
        /// </summary>
        public static ShortcutBinding FindDuplicate(MaskTextureShortcutProfile profile, ShortcutBinding target)
        {
            if (profile?.bindings == null || target == null) return null;
            foreach (var b in profile.bindings)
            {
                if (b.actionId == target.actionId) continue;
                if (b.keyCode == target.keyCode && b.ctrl == target.ctrl &&
                    b.shift == target.shift && b.alt == target.alt)
                    return b;
            }
            return null;
        }
    }
}
