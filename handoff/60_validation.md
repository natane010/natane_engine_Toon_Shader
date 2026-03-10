# TextureGenerator Validation Checklist

Date: 2026-03-10
Target: `UVTextureGenerator` paint interaction P0

## Required Manual Checks

- Open `Tools > Natane > UV Texture Generator`.
- Enable brush mode and confirm painting still works on an unlocked layer.
- While brush is enabled:
  - `Space + LMB` pans the canvas.
  - `MMB` pans the canvas.
  - `Alt + LMB` pans the canvas.
  - `Ctrl/Cmd + Wheel` zooms at cursor position.
  - `Wheel` changes brush size.

- Shortcut checks
  - `Ctrl/Cmd + Click` picks active-layer grayscale and alpha into brush settings.
  - `Shift + Click` first sets a line anchor, second click paints a straight line.
  - `Alt + RMB drag` changes size on X and opacity on Y.

- History checks
  - Brush stroke can undo/redo from header buttons.
  - Brush stroke can undo with `Ctrl/Cmd + Z`.
  - Brush stroke can redo with `Ctrl/Cmd + Shift + Z` and `Ctrl/Cmd + Y`.
  - After undo, a new stroke clears the redo branch.

- Stabilizer checks
  - `Off`, `Basic`, `Stabilized` all draw successfully.
  - `Stabilized` visibly smooths fast hand motion more than `Basic`.

- Safety checks
  - Locked layer does not paint.
  - Transparent-pixel lock still blocks paint into fully transparent areas.
  - UV island select mode still works when brush is off.
  - 3D preview refreshes after paint, line stroke, undo, and redo.

## Not Validated Here

- Unity compile in a host project
- Pen tablet hardware behavior
- Extremely large texture sizes under long sessions
