# TextureGenerator Paint UX Improvement Plan

Date: 2026-03-10
Target: `UVTextureGenerator` / `MaskTextureBrushTool`

## Product Goal

Make Mask Texture Studio feel competitive with mainstream paint tools for mask authoring, without turning it into a full Photoshop/Krita clone.

Primary users:
- Shader artists
- TA / Pipeline users
- Technical artists doing grayscale mask cleanup

Primary workflow:
- Generate base mask
- Paint corrections on top
- Validate in 2D + 3D preview
- Export / assign to shader property

## Phase 0: Input Foundation

Implement first because every later UX improvement depends on this layer.

Tasks:
- Add a centralized input map for brush mode, pan, zoom, line mode, picker, and size/opacity adjustment.
- Split canvas interaction into:
  - navigation controller
  - brush stroke controller
  - overlay / HUD controller
- Add explicit command handling for undo/redo and keyboard shortcuts.
- Make navigation available even while brush mode stays enabled.

Acceptance:
- User can keep brush enabled and still pan/zoom without mode switching.
- Temporary actions do not desync brush state.

## Phase 1: Paint Parity Essentials

Tasks:
- Implement stroke-level undo/redo history for layer pixel arrays.
- Add temporary pan while brush is active.
- Add brush-size and opacity shortcuts with on-canvas preview.
- Add straight line mode.
- Add grayscale/alpha value picker from canvas.
- Add stroke stabilizer with at least Off / Basic / Stabilized presets.

Acceptance:
- A user can correct masks continuously without repeatedly moving to side panels.
- The tool supports the common "paint -> pan -> sample -> resize -> paint" loop.

## Phase 2: Workflow Acceleration

Tasks:
- Add brush presets with project-local persistence.
- Add quick HUD or popup panel near the cursor for brush essentials.
- Add spacing control and optional alpha-flow style behavior.
- Add canvas rotation and reset.
- Add symmetry / mirror mode.

Acceptance:
- Repeated mask retouch tasks can be done mostly from the canvas.
- Artists can save and recall brush behaviors instead of rebuilding settings every session.

## Phase 3: Texture-Specific Advantages

Tasks:
- Add wrap-around / tile preview mode.
- Add UV-island constrained paint mode.
- Add seam-aware preview overlay.
- Add before/after split preview and temporary solo/mute for active layer.

Acceptance:
- Texture-specific tasks become easier here than in general-purpose paint tools.
- The tool gains a reason to exist beyond "basic painting inside Unity."

## Proposed Backlog

### P0

- Central input map
- Brush-active pan/zoom
- Stroke-level undo/redo
- Brush size/opacity shortcuts
- Straight line mode
- Value picker
- Stabilizer

### P1

- Brush presets
- Quick HUD / popup palette
- Canvas rotate/reset
- Symmetry / mirror
- Spacing control
- Shortcut customization

### P2

- Wrap-around preview
- UV-island constrained painting
- Layer solo / compare overlay
- Canvas-only presentation mode

## Suggested Implementation Order in Code

1. `MaskTextureBrushTool.cs`
- Introduce `BrushShortcutState`, `StrokeSession`, `BrushPreset`, `BrushStabilizer`.

2. `UVTextureGenerator.cs`
- Add `CanvasInteractionState`, command routing, HUD drawing, and temporary tool handling.

3. `MaskTextureLayerSystem.cs`
- Add history entries and layer-state diffs for undo/redo.

4. Optional new files
- `MaskTextureBrushPresets.cs`
- `MaskTextureCanvasHud.cs`
- `MaskTextureHistory.cs`
- `MaskTextureShortcutProfile.cs`

## Design Rules

- Keep brush enabled as the default working state.
- Avoid destructive hidden behavior; every temporary mode should be visible.
- Prefer muscle-memory-compatible defaults over custom Natane-only shortcuts.
- Make grayscale-mask-specific actions faster than generic painting apps.
- Store presets and shortcuts in a reusable, project-safe format.

## Validation Checklist

- Mouse only workflow works
- Pen tablet workflow works
- Undo/redo survives long sessions
- Brush cursor remains accurate across zoom levels
- Navigation never corrupts strokes
- Tile preview matches exported texture
- 3D preview stays in sync after every stroke
