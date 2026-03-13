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

---

# Look Mixer Implementation Plan

Date: 2026-03-11
Target: `NataneToonShader` / `NataneToonShaderGUI` / `NataneToonMaterialPreset`

## Product Goal

Allow artists to start from one-touch looks while still tuning the final result with explicit `Toon / NPR / PBR` blend weights.

Primary users:
- Character artists
- Technical artists
- Shader look-development users

Primary workflow:
- Choose a starter look preset
- Adjust `Toon / NPR / PBR` balance with bars
- Fine tune detailed parameters
- Save / share / validate the result as a reusable preset

## Phase 0: Contract Foundation

Implement first because every UI / shader decision depends on a stable data contract.

Tasks:
- Add shared properties for:
  - `_LookMode`
  - `_ToonWeight`
  - `_NprWeight`
  - `_PbrWeight`
- Add runtime-side preset/share fields for the look mixer.
- Add schema/version-aware import compatibility for shared material data.
- Keep legacy `_ShadingMode` as a migration source instead of removing it.

Acceptance:
- All toon-family shader variants expose the same look mixer properties.
- Preset assets and `.ntmaterial` export/import preserve look mixer values.
- Existing materials still load without data loss.

## Phase 1: Inspector UX

Tasks:
- Add a `Look Mixer` section at the top of Shading.
- Add starter presets such as:
  - `Pure Toon`
  - `Soft NPR`
  - `Toon-PBR Hybrid`
  - `Near PBR`
- Add 3 sliders for `Toon / NPR / PBR`.
- Normalize weights after edits so the total remains stable.
- Show concise help text explaining:
  - `Toon/PBR` = base surface response
  - `NPR` = style stack intensity

Acceptance:
- A user can pick a preset and then refine the blend with sliders.
- Weight edits feel stable and do not require manual math by the user.

## Phase 2: Shader Architecture

Tasks:
- Split the non-StandardToon shading flow into:
  - stylized base response
  - PBR-like base response
  - NPR style stack
- Blend `Toon` and `PBR` at the surface-response level.
- Apply `NPR` as a post-lighting style mix.
- Keep keyword growth minimal by using float weights where possible.
- Preserve legacy behavior when weights are not authored yet.

Acceptance:
- `Toon=1 / NPR=0 / PBR=0` matches the existing default look closely.
- `PBR` weight visibly increases physically-based response without breaking stylized controls.
- `NPR` weight increases illustration-style processing without requiring a separate shader.

## Phase 3: Tooling / Validation

Tasks:
- Extend validator rules for look mixer states:
  - high `PBR` weight without reflection/smoothness support
  - high `NPR` weight without NPR stack features enabled
- Update quick setup to use look mixer presets.
- Extend preset generators so starter presets can author valid mixer states.
- Keep foldout state management compatible with the current GUI key system.

Acceptance:
- Users get actionable warnings when the mix implies missing support data.
- One-touch setup and saved presets agree with the new look mixer model.

## Proposed Backlog

### P0

- Look mixer shader properties
- Preset/share schema extension
- Legacy migration fallback
- Inspector look mixer section

### P1

- Toon/PBR base-response mixing
- NPR style-stack weighting
- Quick setup + preset generator update

### P2

- Validator rules per look mix
- Surface-model-specific recommendations
- Host Unity visual validation and polish

## Suggested Implementation Order in Code

1. `Runtime/MaterialSystem/NataneToonMaterialPreset.cs`
- Add look mixer fields and migration-safe apply/readback.

2. `Runtime/MaterialSystem/MaterialParameterShareSystem.cs`
- Add schema versioning for `.ntmaterial` payloads.

3. `Editor/NataneToon/GUI/NataneToonShaderGUI.cs`
- Add look mixer UI and normalization logic.

4. `Shaders/NataneToon/Include/Core/NataneToonInput.hlsl`
- Add shared look mixer inputs.

5. `Shaders/NataneToon/Include/Rendering/NataneToonFragment.hlsl`
- Implement base shading mix and NPR post-style weighting.

6. `Editor/NataneToon/Tools/MaterialValidator.cs`
- Add look-mix-aware validation.

## Design Rules

- Treat `Toon` and `PBR` as the base lighting contract.
- Treat `NPR` as style processing layered on top.
- Do not force users to understand internal `_ShadingMode` values.
- Keep old materials visually stable by default.
- Prefer gradual migration over destructive one-shot conversion.

## Validation Checklist

- Existing materials render acceptably with the new properties at defaults.
- Look mixer sliders normalize correctly and survive save/load.
- Preset save/load keeps mixer values.
- Clipboard/file sharing keeps mixer values.
- `Pure Toon`, `Soft NPR`, and `Near PBR` each produce visibly distinct looks.
- No unexpected shader keyword explosion is introduced.

---

# lilToon Migration P0 + Exact Compatibility Plan

Date: 2026-03-11

## Goal

- Fix obvious visual-regression bugs in the lilToon migration flow.
- Start a dedicated `Exact Compatibility` path for migrated materials instead of relying only on approximate Natane-native remapping.

## P0 Tasks

### P0-1. Fix migration bugs that directly change the look

Tasks:
- Fix `_ShadowNormalStrength` so its bump-scale adjustment is not overwritten by the later normal-map copy.
- Replace the hardcoded rim blend assignment with the existing `ConvertRimBlendMode()` mapping.
- Review `Visual Match` wording so the tool stops promising perfect immediate parity unless the exact path is actually active.

Acceptance:
- Migrated materials preserve the intended normal/shadow response more closely.
- Rim blend mode is sourced from lilToon data instead of being forced to `Add`.
- UI wording no longer overpromises the current behavior.

### P0-2. Preserve migrated exact-mode intent in saved data

Tasks:
- Add a persisted material flag for lilToon exact compatibility.
- Ensure preset/share export-import preserves the flag.

Acceptance:
- A migrated exact-compat material stays exact-compat after preset or `.ntmaterial` round-trip.

## Exact Compatibility First Slice

### EC-1. Add a dedicated migrated-material flag

Tasks:
- Add a hidden shader property such as `_LilToonExactCompatibility`.
- Write the flag from the migration tool when `Exact Compatibility` mode is selected.
- Keep the implementation float-based to avoid shader variant explosion.

Acceptance:
- Migrated materials can opt into parity-focused logic without affecting unrelated Natane materials.

### EC-2. Add a new migration mode

Tasks:
- Extend `LilToonMigrationTool` conversion mode selection to include:
  - `Exact Compatibility`
  - `Visual Match`
  - `Minimal Safe`
- Make `Exact Compatibility` the mode that enables the hidden flag and parity-focused shader path.
- Keep `Visual Match` as the Natane-friendly approximation path.

Acceptance:
- Users can explicitly choose between parity-first migration and Natane-native approximation.

### EC-3. Parity-focused StandardToon branch

Tasks:
- In the StandardToon shading path, use the exact-compat flag to:
  - prioritize StandardToon over Ramp when both are enabled
  - skip Natane-only safety clamp `min(stIndirectCol, stDirectCol)`
  - skip `CompressLightingForSafeRange(...)` in the StandardToon final composition
- Keep the branch scoped to migrated exact-compat materials only.

Acceptance:
- Exact-compat materials avoid the biggest Natane-only post-lighting deviations.
- Non-migrated StandardToon materials keep their current behavior.

### EC-4. Stop forcing Natane tuning defaults in exact mode

Tasks:
- Do not inject Natane-specific defaults such as `_ShadowMaxDarkness`, `_LightMinInfluence`, `_GIIntensity` in exact mode.
- Keep approximation-oriented defaults only in the non-exact migration path.

Acceptance:
- Exact-compat materials start closer to source lilToon behavior.

## Follow-up Backlog

### EC-P1

- Add exact handling or explicit unsupported warnings for:
  - MatCap semantics
  - multi-shadow blur / masks
  - outline scale parity
  - specular semantic mismatch

### EC-P2

- Add screenshot-diff regression validation between source lilToon and migrated Natane materials.
- Build representative sample scenes for:
  - basic toon
  - multi-shadow
  - rim
  - matcap
  - emission
  - outline

## 2026-03-11 LilToon Mode Plan

Goal:
- lilToon migration materials should auto-configure as far as possible and match lilToon visually.
- Users keep current Natane behavior by default.
- A checkbox-style `LilToon Mode` enables lilToon-compatible calculations only for migrated materials that need parity.

Constraints:
- Avoid a separate shader asset if possible.
- Avoid global behavior regressions for existing Natane materials.
- Keep keyword/variant growth minimal.

### LM-1. Product Direction

Decision:
- Expose one material-level toggle:
  - `LilToon Mode`
- Behavior:
  - Off: current Natane calculations
  - On: lilToon-compatible calculation branches for migrated parameters

Acceptance:
- Users can explicitly opt into parity mode without changing existing Natane assets.
- Migration reports can recommend enabling `LilToon Mode` when source features require it.

### LM-2. Data Contract

Tasks:
- Reuse and formalize the hidden compatibility flag already introduced for exact migration.
- Store lilToon-only source parameters as hidden payload values instead of dropping them during migration.
- Group payloads by system:
  - shadow parity payload
  - rim parity payload
  - matcap parity payload
  - outline parity payload
  - emission parity payload

Acceptance:
- Migrated materials preserve enough source data to switch parity mode on/off without re-running migration.

### LM-3. Migration Tool Flow

Tasks:
- Add migration presets:
  - `Natane Native`
  - `LilToon Mode`
- In `LilToon Mode`, automatically:
  - enable the compatibility flag
  - copy all parity-relevant source parameters into hidden properties
  - apply visible Natane parameters only where direct equivalents exist
- For unsupported branches, do not silently approximate; mark them in the report.

Acceptance:
- Running migration once produces a material that can be toggled between Natane-native and lilToon-compatible behavior.

### LM-4. Inspector / UX

Tasks:
- Add a visible `LilToon Mode` checkbox in the material inspector when the material has lilToon migration metadata.
- Add a compact info panel:
  - source shader name
  - migration mode used
  - parity warnings count
- Add one-click actions:
  - `Enable LilToon Mode`
  - `Revert to Natane Mode`

Acceptance:
- Artists can understand which materials are migrated and which mode they are currently using.

### LM-5. Compatibility Wave 1: Lighting Core

Tasks:
- Keep compatibility branches localized to the current shader family.
- In `LilToon Mode`, prefer lilToon-compatible handling for:
  - light color min/max / monochrome / as-unlit
  - StandardToon shadow composition
  - exact shadow environment lift
  - removal of Natane-only safety/compression branches already identified

Acceptance:
- The largest light/shadow deltas are controlled by the compatibility toggle instead of global shader behavior changes.

### LM-6. Compatibility Wave 2: Shadow Detail

Tasks:
- Finish parity handling for:
  - `ShadowBorderRange`
  - `BackfaceForceShadow`
  - `ShadowMaskType`
  - `ShadowPostAO`
  - shadow mask textures and per-layer blur
- Keep this path active only when `LilToon Mode` is on.

Acceptance:
- lilToon multi-shadow and masked-shadow materials no longer depend on warnings for the common cases.

### LM-7. Compatibility Wave 3: Feature Systems

Tasks:
- Implement or emulate lilToon-compatible behavior for:
  - rim shade
  - emission 2nd
  - matcap blend semantics
  - outline width / width mask parity
- If a feature still cannot be matched exactly, keep warning text explicit and feature-specific.

Acceptance:
- Remaining parity gaps are reduced to a short, explicit list.

### LM-8. Validation Pipeline

Tasks:
- Build a dedicated comparison scene:
  - source lilToon material
  - migrated Natane material with `LilToon Mode` off
  - migrated Natane material with `LilToon Mode` on
- Validate by screenshot diff on representative cases.
- Define pass/fail thresholds per category:
  - base toon
  - shadow layers
  - rim
  - matcap
  - emission
  - outline

Acceptance:
- `LilToon Mode` quality is judged by visual diff, not by subjective spot checks only.

### LM-9. Rollout Order

1. Formalize `LilToon Mode` toggle and hidden payload contract.
2. Finish migration-side payload capture and inspector exposure.
3. Complete lighting core parity.
4. Complete shadow-detail parity.
5. Complete rim / emission2nd / matcap / outline parity.
6. Add screenshot-diff validation and tune thresholds.

### LM-10. Risk Notes

- Full parity without any shader-side branching is not realistic.
- The safer compromise is:
  - no shader replacement
  - minimal, gated compatibility branches inside the existing shader family
  - compatibility active only when `LilToon Mode` is enabled
- This keeps the blast radius small while still making exact-match work feasible.

## 2026-03-11 LilToon Inspector UX concrete proposal

### Goal

- Make migrated lilToon materials understandable at a glance in the Inspector.
- Keep Natane-native editing as the default mental model.
- Prevent artists from getting lost between `Look Mixer`, `StandardToon`, and the hidden lilToon compatibility backend.

### Current UX problems

- `Look Mixer` is shown first, but migrated-material state is not shown first.
- `StandardToon` help exists, but the user still has to infer:
  - whether the material came from lilToon
  - whether compatibility math is active
  - which remaining gaps still need manual review
- `_LilToonExactCompatibility` exists only as hidden backend state, so `OFF` state loses obvious migration identity in the Inspector.

### Proposed Inspector layout

Place a dedicated `LilToon Migration` card at the top of the `Shading` section, before `Look Mixer`.

```text
Shading
├─ LilToon Migration
│  ├─ badge: Migrated from lilToon
│  ├─ mode switch: [ Natane ] [ lilToon Match ]
│  ├─ summary row:
│  │  ├─ Source: lilToon
│  │  ├─ Migration: Exact Compatibility / Visual Match / Minimal Safe
│  │  └─ Warnings: 3
│  ├─ note:
│  │  ├─ Natane: uses current Natane shading and Look Mixer
│  │  └─ lilToon Match: prioritizes lilToon-compatible lighting/shadow behavior
│  ├─ parity details foldout
│  │  ├─ unsupported: Rim Shade
│  │  ├─ unsupported: Emission 2nd
│  │  └─ warning: ShadowMaskType requires review
│  └─ actions
│     ├─ Reset lilToon Match Values
│     └─ Open Migration Tool
├─ Look Mixer
│  └─ disabled or de-emphasized when lilToon Match is ON
└─ StandardToon / Toon / Gradient / PBR-like content
```

ASCII mock for the same layout:

```text
Shading
|- LilToon Migration
|  |- badge: Migrated from lilToon
|  |- mode switch: [ Natane ] [ lilToon Match ]
|  |- summary row:
|  |  |- Source: lilToon
|  |  |- Migration: Exact Compatibility / Visual Match / Minimal Safe
|  |  `- Warnings: 3
|  |- note:
|  |  |- Natane: uses current Natane shading and Look Mixer
|  |  `- lilToon Match: prioritizes lilToon-compatible lighting/shadow behavior
|  |- parity details foldout
|  |  |- unsupported: Rim Shade
|  |  |- unsupported: Emission 2nd
|  |  `- warning: ShadowMaskType requires review
|  `- actions
|     |- Reset lilToon Match Values
|     `- Open Migration Tool
|- Look Mixer
|  `- disabled or de-emphasized when lilToon Match is ON
`- StandardToon / Toon / Gradient / PBR-like content
```

### UX behavior rules

1. Only show the `LilToon Migration` card when the material has explicit migration metadata.
2. Default state after lilToon migration should be `lilToon Match = ON`.
3. `Natane` and `lilToon Match` should be a two-state button UI, not a bare checkbox.
4. While `lilToon Match` is ON:
   - keep `Look Mixer` visible but disabled with an explanation
   - keep advanced Natane-native art controls visible
   - show a warning that heavy manual edits can break parity
5. While `Natane` is ON:
   - enable `Look Mixer`
   - keep the migration card visible so the material still reads as migrated
6. Unsupported parity gaps must be surfaced inline, not only in one-time migration reports.

### Why this is more usable

- It answers the artist's first three questions immediately:
  - `What kind of material is this?`
  - `Which shading behavior is active right now?`
  - `Why does it still differ from the source?`
- It avoids the current ambiguity where `Look Mixer` appears first even for migrated materials that should initially prioritize parity.
- It preserves both workflows:
  - quick parity confirmation
  - later Natane-native art direction

### Data contract needed for this UX

Add explicit migration metadata so the Inspector can be accurate even when lilToon compatibility is turned off.

- Hidden material float:
  - `_LilToonMigrated`
- Hidden material float enum:
  - `_LilToonMigrationMode`
    - `0 = Unknown`
    - `1 = ExactCompatibility`
    - `2 = VisualMatch`
    - `3 = MinimalSafe`
- Hidden material int bitmask:
  - `_LilToonParityFlags`
- Material override tags:
  - `NataneLilToonSourceShader`
  - `NataneLilToonMigrationVersion`

### Parity flag proposal

Use a compact flag enum for inline UI warnings.

- `RimShadeUnsupported`
- `Emission2ndUnsupported`
- `ShadowBorderRangeUnsupported`
- `ShadowMaskTypeUnsupported`
- `BackfaceForceShadowUnsupported`
- `ShadowPostAOUnsupported`
- `MatCapNeedsReview`
- `OutlineNeedsReview`

### Inspector implementation breakdown

#### A. Metadata and backend state

Files:

- `Editor/NataneToon/Migration/LilToonMigrationTool.cs`
- `Shaders/NataneToon/NataneToonShader.shader`
- `Shaders/NataneToon/Variants/NataneToonShader_*.shader`
- `Runtime/MaterialSystem/NataneToonMaterialPreset.cs`
- `Runtime/MaterialSystem/MaterialParameterShareSystem.cs`

Tasks:

- Persist `_LilToonMigrated`, `_LilToonMigrationMode`, `_LilToonParityFlags`.
- Persist source shader name and migration version via override tags.
- Ensure presets / sharing do not drop the new migration metadata.

Acceptance:

- Migrated materials are still recognized as migrated after preset save/load and parameter sharing.

#### B. Inspector card

Files:

- `Editor/NataneToon/GUI/NataneToonShaderGUI.cs`
- optionally `Editor/NataneToon/GUI/NataneToonShaderGUIHelpers.cs`

Tasks:

- Add `DrawLilToonMigrationCard()` before `DrawLookMixerControls()`.
- Read migration metadata and render:
  - badge
  - two-state mode switch
  - source shader / migration mode / warning count
  - parity detail foldout
  - small action row
- Disable `Look Mixer` when compatibility mode is active.

Sketch:

```csharp
private void DrawShadingSection()
{
    ...
    DrawLilToonMigrationCardIfNeeded();
    DrawLookMixerControls();
    NataneToonShaderGUIHelpers.DrawShadingSectionContent(...);
}

private void DrawLilToonMigrationCardIfNeeded()
{
    if (!IsLilToonMigratedMaterial(targetMaterial))
    {
        return;
    }

    bool lilToonMode = GetLilToonExactCompatibility(targetMaterial);
    DrawLilToonModeSelection(lilToonMode);
    DrawLilToonMigrationSummary(targetMaterial);
    DrawLilToonParityWarnings(targetMaterial);
}
```

Acceptance:

- A migrated material is self-explanatory at the top of the `Shading` section.

#### C. UI state behavior

Files:

- `Editor/NataneToon/GUI/NataneToonShaderGUI.cs`

Tasks:

- Introduce helpers:
  - `IsLilToonMigratedMaterial(Material material)`
  - `GetLilToonMigrationMode(Material material)`
  - `GetLilToonParityFlags(Material material)`
  - `DrawLilToonModeSelection(...)`
- When `lilToon Match` is ON:
  - set `_LilToonExactCompatibility = 1`
  - show `Look Mixer` as informational only
- When `Natane` is ON:
  - set `_LilToonExactCompatibility = 0`
  - restore normal `Look Mixer` editing

Acceptance:

- Artists can switch between parity mode and Natane-native editing without losing context.

#### D. Migration report continuity

Files:

- `Editor/NataneToon/Migration/LilToonMigrationTool.cs`

Tasks:

- Map existing conversion warnings into `_LilToonParityFlags`.
- Keep console report output, but treat the Inspector card as the long-lived user surface.

Acceptance:

- Important review items remain visible after the migration window is closed.

### Recommended implementation order

1. Add migration metadata contract.
2. Add Inspector card and mode switch.
3. Gate `Look Mixer` behavior based on lilToon mode.
4. Surface parity flags in the Inspector.
5. Add reset / reopen actions if needed.

### Non-goals for the first UI slice

- Full screenshot diff integration inside the Inspector.
- Perfect source-shader name recovery for old materials migrated before metadata existed.
- Hiding all Natane controls while lilToon mode is on.

The first slice should focus on clarity, not on over-automating the Inspector.
