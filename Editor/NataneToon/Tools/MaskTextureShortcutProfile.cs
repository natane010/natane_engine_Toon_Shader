using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    [System.Serializable]
    internal sealed class MaskTextureShortcutProfile
    {
        public float brushSizeDragSensitivity = 0.25f;
        public float brushOpacityDragSensitivity = 0.005f;
        public bool allowMiddleMousePan = true;
        public bool allowSpacePan = true;
        public bool allowAltLeftPan = true;
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
    }
}
