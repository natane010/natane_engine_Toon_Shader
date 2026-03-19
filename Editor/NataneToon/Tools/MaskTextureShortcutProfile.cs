using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    [System.Serializable]
    internal class ShortcutBinding
    {
        public string actionId;
        public string displayName;
        public KeyCode keyCode;
        public bool ctrl;
        public bool shift;
        public bool alt;

        public string ToDisplayString()
        {
            var sb = new System.Text.StringBuilder();
            if (ctrl)  sb.Append("Ctrl+");
            if (shift) sb.Append("Shift+");
            if (alt)   sb.Append("Alt+");
            sb.Append(keyCode.ToString());
            return sb.ToString();
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
            p.bindings = new System.Collections.Generic.List<ShortcutBinding>
            {
                new ShortcutBinding { actionId = "Undo", displayName = "Undo", keyCode = KeyCode.Z, ctrl = true },
                new ShortcutBinding { actionId = "Redo", displayName = "Redo", keyCode = KeyCode.Y, ctrl = true },
                new ShortcutBinding { actionId = "BrushSizeUp", displayName = "Brush Size Up", keyCode = KeyCode.RightBracket },
                new ShortcutBinding { actionId = "BrushSizeDown", displayName = "Brush Size Down", keyCode = KeyCode.LeftBracket },
                new ShortcutBinding { actionId = "FitCanvas", displayName = "Fit Canvas", keyCode = KeyCode.F },
                new ShortcutBinding { actionId = "ToggleUV", displayName = "Toggle UV", keyCode = KeyCode.U },
                new ShortcutBinding { actionId = "ToggleBrush", displayName = "Toggle Brush", keyCode = KeyCode.B },
                new ShortcutBinding { actionId = "FlipHorizontal", displayName = "Flip Horizontal", keyCode = KeyCode.H },
                new ShortcutBinding { actionId = "ZoomIn", displayName = "Zoom In", keyCode = KeyCode.Equals, ctrl = true },
                new ShortcutBinding { actionId = "ZoomOut", displayName = "Zoom Out", keyCode = KeyCode.Minus, ctrl = true },
            };
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
    }
}
