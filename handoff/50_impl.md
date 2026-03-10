# TextureGenerator Paint UX Implementation Notes

Date: 2026-03-10
Scope: P0 interaction foundation for `UVTextureGenerator`

## Implemented

- Brush-active navigation
  - Canvas pan now works while brush stays enabled.
  - Supported by `Space + LMB`, `MMB`, and compatibility `Alt + LMB`.
  - `Ctrl/Cmd + Wheel` now zooms the canvas.

- Brush interaction shortcuts
  - `Ctrl/Cmd + Click` picks grayscale and alpha from the active layer.
  - `Shift + Click` creates a straight-line stroke from the last anchor.
  - `Alt + RMB drag` changes brush size and opacity with an on-canvas overlay.

- Stroke history
  - Stroke-level undo/redo was wired into tool-local history.
  - Canvas header now exposes `Undo` / `Redo`.
  - `Ctrl/Cmd + Z`, `Ctrl/Cmd + Shift + Z`, `Ctrl/Cmd + Y` are handled.

- Stabilizer
  - Added `Off / Basic / Stabilized`.
  - Stabilizer now affects input positions instead of paint mode behavior.

- Visual guidance
  - Straight-line mode now shows a line guide from anchor to cursor.
  - Interaction status text is shown below zoom info.

## Files Touched

- `Editor/NataneToon/Tools/UVTextureGenerator.cs`
- `Editor/NataneToon/Tools/MaskTextureBrushTool.cs`
- `Editor/NataneToon/Tools/MaskTextureHistory.cs`
- `Editor/NataneToon/Tools/MaskTextureShortcutProfile.cs`
- `Editor/NataneToon/Tools/MaskTextureBrushStabilizer.cs`

## Known Limits

- Undo/redo is tool-local history, not Unity global Undo.
- Picker currently samples from the active layer, not flattened composite output.
- Straight line uses the last anchor model; it does not yet support preview snapping or constrained angles.
- Wrap-around preview, preset system, symmetry, and shortcut customization are still pending P1/P2.

## Next Recommended Step

Move to P1 in this order:
1. Brush presets
2. On-canvas HUD / popup
3. Canvas rotate / reset
4. Symmetry / mirror
