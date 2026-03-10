# TextureGenerator Implementation Backlog

Date: 2026-03-10
Target: `UVTextureGenerator` / `MaskTextureBrushTool`

## How to Use This Backlog

- `P0`: start immediately; these are the tasks that change day-to-day paint feel.
- `P1`: next batch once P0 is stable.
- `P2`: texture-specialized and polish-heavy improvements.

Each task is written so it can become a branch / PR unit.

## P0 Tasks

### TG-P0-01 Input Map and Temporary Tool Routing

Goal:
- Build a single shortcut layer so paint, pan, picker, line mode, zoom, and brush adjustment are routed consistently.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`
- `Editor/NataneToon/Tools/MaskTextureBrushTool.cs`
- new `Editor/NataneToon/Tools/MaskTextureShortcutProfile.cs`

Implementation:
- Introduce `BrushShortcutState` / `CanvasInteractionState`.
- Add temporary actions:
  - `Space` or MMB drag = pan
  - `Ctrl` = value picker
  - `Shift` = line mode
  - `Alt+RMB drag` = size / opacity adjust
  - `Ctrl+Wheel` = zoom
  - `Wheel` = size
- Remove direct hardcoded event logic from brush and canvas handlers.

Done when:
- Brush can stay enabled while temporary actions work.
- Shortcut conflicts are resolved in one place instead of scattered `Event.current` branches.

### TG-P0-02 Brush-Active Navigation

Goal:
- Remove the current requirement to disable brush mode before panning/zooming.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`

Implementation:
- Refactor canvas event flow so navigation has higher priority than stroke sampling when a temporary nav modifier is held.
- Preserve brush state after releasing the modifier.
- Keep zoom centered on cursor position instead of screen center.

Done when:
- Artist can paint, hold pan modifier, move canvas, release, and continue the same workflow without toggling brush mode.

### TG-P0-03 Stroke Undo/Redo History

Goal:
- Make every brush stroke undoable and redoable at stroke granularity.

Target files:
- `Editor/NataneToon/Tools/MaskTextureBrushTool.cs`
- `Editor/NataneToon/Tools/MaskTextureLayerSystem.cs`
- new `Editor/NataneToon/Tools/MaskTextureHistory.cs`

Implementation:
- Convert the existing internal snapshot into a real history entry.
- Store per-stroke before/after pixel arrays for the active layer.
- Add `Undo` / `Redo` commands and UI buttons.
- Ensure history clears or branches correctly after new strokes following redo.

Done when:
- A finished stroke can be undone and redone repeatedly in a long session.
- Non-brush edits that modify the active layer also register history entries or are explicitly excluded.

### TG-P0-04 Line Mode, Picker, and Brush Adjustment UX

Goal:
- Add the minimum paint-tool muscle memory features that users expect on day one.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`
- `Editor/NataneToon/Tools/MaskTextureBrushTool.cs`

Implementation:
- `Shift` straight line from previous anchor to current click.
- `Ctrl` sample grayscale + alpha from current layer or flattened preview.
- On-canvas size/opacity drag with ring / numeric preview.
- Brush settings panel updates live from shortcut changes.

Done when:
- User can draw a controlled straight correction line.
- User can sample an existing value and continue painting without leaving canvas.
- Size/opacity changes are visible before the next stroke lands.

### TG-P0-05 Stroke Stabilizer

Goal:
- Improve hand feel without changing the paint result model.

Target files:
- `Editor/NataneToon/Tools/MaskTextureBrushTool.cs`
- new `Editor/NataneToon/Tools/MaskTextureBrushStabilizer.cs`

Implementation:
- Add stabilizer modes: `Off`, `Basic`, `Stabilized`.
- Implement smoothing on stroke input positions, not as a blur paint mode.
- Add parameters for delay / smoothing strength.

Done when:
- Fast hand motion produces visibly steadier strokes.
- Stabilizer can be toggled without changing brush mode or brush preset.

### TG-P0-06 P0 Help, Shortcut Legend, and Validation

Goal:
- Make the new interaction model discoverable and safe to ship.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`
- `Editor/NataneToon/GUI/NataneToonToolsDocumentation.cs`
- docs under `handoff/` or `Documentation~/`

Implementation:
- Add visible shortcut legend in tool UI.
- Update tool help entries.
- Create a small manual test checklist for mouse and pen workflows.

Done when:
- A first-time user can discover the core shortcuts without external docs.

## P1 Tasks

### TG-P1-01 Brush Preset System

Goal:
- Let artists save and reuse brush behavior instead of rebuilding sliders every session.

Target files:
- `Editor/NataneToon/Tools/MaskTextureBrushTool.cs`
- new `Editor/NataneToon/Tools/MaskTextureBrushPresets.cs`

Implementation:
- Define preset data for size, hardness, opacity, strength, alpha, spacing, stabilizer.
- Support create / rename / delete / duplicate / favorite.
- Store presets in project-safe serialized assets or editor-safe JSON.

Done when:
- At least 3 presets can be saved, recalled, and shared in project scope.

### TG-P1-02 On-Canvas HUD / Popup Palette

Goal:
- Reduce cursor travel to side panels.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`
- new `Editor/NataneToon/Tools/MaskTextureCanvasHud.cs`

Implementation:
- Add a compact overlay or popup with size, opacity, stabilizer, preset selector, and undo/redo.
- Show only when relevant so it stays lightweight.

Done when:
- Common brush tweaks can be made without moving to the right panel.

### TG-P1-03 Canvas Rotate / Reset

Goal:
- Match mainstream drawing workflows for awkward stroke angles.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`

Implementation:
- Add canvas rotation state and reset button / shortcut.
- Keep cursor-to-texture mapping accurate under rotation.

Done when:
- Brush cursor and sampling remain correct after rotate/reset.

### TG-P1-04 Symmetry / Mirror Paint

Goal:
- Speed up bilateral and repeated mask authoring.

Target files:
- `Editor/NataneToon/Tools/MaskTextureBrushTool.cs`
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`

Implementation:
- Start with horizontal / vertical mirror.
- Optionally allow UV center or texture center origin.
- Render symmetry guides on canvas.

Done when:
- A stroke mirrors correctly and remains undoable as one history event.

### TG-P1-05 Spacing and Flow Controls

Goal:
- Give brush feel more depth without exploding complexity.

Target files:
- `Editor/NataneToon/Tools/MaskTextureBrushTool.cs`

Implementation:
- Expose spacing instead of hardcoding it.
- Add optional flow-style accumulation separate from opacity.
- Keep default preset simple.

Done when:
- Users can tune between crisp stamp spacing and more continuous paint feel.

### TG-P1-06 Shortcut Customization

Goal:
- Avoid locking users into a single Natane-only control scheme.

Target files:
- new `Editor/NataneToon/Tools/MaskTextureShortcutProfile.cs`
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`

Implementation:
- Add editable shortcut profile asset or editor prefs.
- Support reset-to-default.

Done when:
- Default shortcuts can be changed without code edits.

## P2 Tasks

### TG-P2-01 Wrap-Around / Tile Preview

Goal:
- Support seamless texture workflows directly in the tool.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`

Implementation:
- Add repeat preview mode using tiled display.
- Allow painting against the tiled visualization if feasible.

Done when:
- Seam issues are visible before export.

### TG-P2-02 UV-Island Constrained Paint

Goal:
- Turn UV awareness into a real advantage over general paint apps.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`
- `Editor/NataneToon/Tools/MaskTextureBrushTool.cs`

Implementation:
- Allow paint only on selected island(s).
- Add expand / shrink margin options around island boundaries.

Done when:
- User can isolate paint to chosen islands without manual masking.

### TG-P2-03 Compare Overlay and Layer Solo Tools

Goal:
- Make inspection faster during mask cleanup.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`
- `Editor/NataneToon/Tools/MaskTextureLayerSystem.cs`

Implementation:
- Add before/after compare mode.
- Add solo / temporary mute for active layer.
- Add split view or hold-to-compare interaction.

Done when:
- Artists can judge the impact of a stroke or layer without exporting.

### TG-P2-04 Canvas-Only Presentation Mode

Goal:
- Maximize usable paint area for focused retouching sessions.

Target files:
- `Editor/NataneToon/Tools/UVTextureGenerator.cs`

Implementation:
- Toggle side panels off.
- Keep HUD and minimal nav visible.

Done when:
- Canvas-only mode is reversible and preserves the current session state.

## Suggested PR Slicing

PR-1:
- TG-P0-01
- TG-P0-02

PR-2:
- TG-P0-03

PR-3:
- TG-P0-04
- TG-P0-05

PR-4:
- TG-P0-06

After that, move to P1 in order.
