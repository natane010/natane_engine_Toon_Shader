# TextureGenerator Paint UX Research

Date: 2026-03-10
Target: `Editor/NataneToon/Tools/UVTextureGenerator.cs`

## Goal

Raise `UVTextureGenerator` / Mask Texture Studio so the brush and canvas interaction does not feel clearly behind mainstream paint tools.

This research focuses on workflow patterns that are proven in Clip Studio Paint, GIMP, and Krita, then filters them for what matters to a grayscale mask/UV paint workflow inside Unity.

## Current State in Natane

Existing strengths:
- Illustration-style 3-column layout already exists: Sub Tool / canvas / layers-output.
- Layer stack, blend modes, lock, transparency lock, filters, 3D preview, UV wireframe, island select, and brush modes are already present.
- Brush has size, hardness, opacity, paint value, alpha, erase, smooth, and a visible cursor.

Current interaction gaps:
- Brush input has no keyboard shortcut layer, no temporary tool switching, and no command handling.
- Canvas pan/zoom is blocked while brush mode is active.
- Scroll wheel only changes brush size; there is no drag-to-size, rotate canvas, reset view, or temporary color/value picker.
- Brush smoothing is only a blur paint mode; there is no stroke stabilizer / lazy stroke / post-correction.
- The brush stores an undo snapshot internally, but the result is not integrated into a usable undo/redo history.
- No brush preset system, no on-canvas quick HUD, no straight line mode, no symmetry, no tile/wrap preview.

## External Benchmark Summary

### Clip Studio Paint

Confirmed patterns from official docs:
- Preferences expose modifier-key behavior, temporary tool switching, fast view operations, drag brush-size preview, minimum drag distance, touch gestures, and auto-scroll behavior.
- Tool Property / Sub Tool Detail split lets common settings stay visible while deeper settings remain accessible.
- Pen pressure can be adjusted from a built-in calibration flow.

Why it matters:
- CSP wins on "always available" navigation and brush adjustment without breaking drawing flow.
- The important lesson is not just feature count, but how often users can stay on-canvas.

### GIMP

Confirmed patterns from official docs:
- Paint tools support straight line drawing with `Shift`, color picking with modifier keys, and dedicated paint options.
- Brush settings include spacing, hardness, force, angle, aspect ratio, dynamics, smooth stroke, lock brush to view, and symmetry painting.
- MyPaint tools expose popup and editor workflows for quick brush access.

Why it matters:
- GIMP shows the minimum viable "serious paint tool" baseline: modifier-based operations, stroke smoothing, symmetry, and brush behavior controls.

### Krita

Confirmed patterns from official docs:
- Popup palette appears directly on canvas and bundles color history, common actions, and navigation support.
- On-canvas brush editor and freehand brush modifiers allow quick size/opacity change, line mode, color sampling, and smoothing/stabilizer behaviors.
- Wrap-around mode exists specifically for seamless texture painting.
- Canvas-only presentation is a first-class mode.

Why it matters:
- Krita is the closest benchmark for texture/mask painting feel: it optimizes "paint, navigate, adjust, continue" loops and includes texture-specific preview modes.

## Core Patterns Repeated Across Tools

These appeared repeatedly across the benchmark tools and should be treated as proven interaction primitives:

1. Temporary modifiers
- Hold a key to pan, sample, line-draw, or resize without leaving the brush.

2. On-canvas adjustment
- Brush size/opacity and some view controls are adjustable directly in the canvas area.

3. Stroke stabilization
- Separate from blur/smooth paint mode. Users expect a stroke engine option that makes hand motion easier to control.

4. Quick access surfaces
- Popup palette, navigator, compact HUD, or sub-tool presets reduce cursor travel.

5. View freedom during painting
- Pan/zoom/rotate/reset must be available without disabling brush mode.

6. Texture-specific preview
- Wrap/tile preview is standard when the output is intended to repeat.

7. Real undo/redo history
- Stroke-level undo is assumed.

## What Natane Actually Needs

### Must-have for parity

- Stroke-level undo/redo
- Temporary pan while brush is active
- Zoom while brush is active
- Brush-size and opacity shortcuts
- Straight line mode
- Value picker / eyedropper for grayscale-alpha targets
- Stroke stabilizer
- Brush presets
- On-canvas quick HUD

### Strongly recommended

- Canvas rotate / reset rotation
- Symmetry / mirror paint
- Spacing control
- Soft preview of stroke radius and hardness while changing size
- Tile / wrap-around preview
- Shortcut customization

### Nice-to-have

- Canvas-only mode
- Recent brush history
- Per-project brush preset asset storage
- UV island constrained paint modes

## Recommended Product Direction

Do not try to clone a full raster illustration app.

Instead, define the target as:

"A fast grayscale mask painting workspace for Unity artists, with mainstream paint-tool muscle memory and UV-specific helpers."

That means the first wins should be around interaction latency and muscle-memory compatibility, not exotic generators.

## Sources

- Clip Studio Paint support / manual:
  - https://support.clip-studio.com/en-us/faq/articles/20200002
  - https://help.clip-studio.com/en-us/manual_en/720_preferences/Preferences.htm
  - https://help.clip-studio.com/en-us/manual_en/240_brushes/Custom_Brush_Settings.htm
  - https://tips.clip-studio.com/en-us/articles/1011
- GIMP official manual:
  - https://docs.gimp.org/3.0/en/gimp-tool-paintbrush.html
  - https://docs.gimp.org/3.0/en/gimp-painting.html
  - https://docs.gimp.org/3.0/en/gimp-using-variable-size-brush.html
  - https://docs.gimp.org/3.0/en/gimp-concepts-dynamics.html
  - https://docs.gimp.org/3.0/en/gimp-symmetry-dialog.html
- Krita official docs:
  - https://docs.krita.org/en/reference_manual/popup-palette.html
  - https://docs.krita.org/en/reference_manual/tools/freehand_brush.html
  - https://docs.krita.org/en/reference_manual/preferences/general_settings.html
  - https://krita.org/en/features/highlights/
