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

---

Date: 2026-03-11
Scope: Real character shader P0 foundation

Implemented in this pass:
- Added `SurfaceModel` to the toon shader family for `Default / Skin / Hair / Eye / Cloth`.
- Added shared realistic-character properties:
  - `CavityMap`
  - `CavityStrength`
  - `SpecularOcclusionStrength`
  - `MicroNormalMap`
  - `MicroNormalScale`
  - `MicroNormalTiling`
  - `MicroNormalStrength`
  - `TransmissionMask`
  - `TransmissionStrength`
- Extended the fragment path to:
  - blend micro-normal detail into the world normal
  - compute cavity visibility and specular occlusion
  - apply specular occlusion to stylized specular, hair specular, PBR-like specular, cubemap reflection, LTCGI specular, and light-volume specular
  - apply transmission masking to SSS
- Extended the inspector to expose:
  - `SurfaceModel` in PBR-like mode
  - micro normal controls
  - micro occlusion controls
  - transmission controls
- Extended `MaterialValidator` texture coverage for the new maps.

Not implemented in this pass:
- clear coat / dual-normal coat
- skin dual-lobe specular
- realistic eye stack changes
- hair transmission rework

Validation status:
- `git diff --check`: pending after the current pass
- Unity visual verification: pending

Date: 2026-03-11
Scope: Real character shader clear coat slice

Implemented in this pass:
- Added clear coat properties to the toon shader family:
  - `ClearCoatIntensity`
  - `ClearCoatSmoothness`
  - `ClearCoatMask`
  - `ClearCoatNormalMap`
  - `ClearCoatNormalScale`
  - `ClearCoatFresnelPower`
- Exposed clear coat controls in the Reflection section of the inspector.
- Added a forward-base clear coat layer in the fragment path:
  - separate normal map for the coat
  - GGX direct specular
  - reflection-probe indirect specular
  - grazing-angle fresnel weighting
  - integration with existing specular occlusion
- Extended `MaterialValidator` texture lists for the coat maps.

Known limits:
- Clear coat currently runs in the forward-base path only.
- Coat blending is additive top-layer approximation, not full energy-conserving layered BRDF yet.
- There is no dedicated coat distance-fade or preset bundle yet.

Date: 2026-03-11
Scope: Real character shader skin dual-lobe slice

Implemented in this pass:
- Added skin dual-lobe properties to the toon shader family:
  - `SkinSpecPrimaryStrength`
  - `SkinSpecSecondaryStrength`
  - `SkinSpecSecondarySmoothness`
  - `SkinSpecSecondaryColor`
  - `SkinSpecFresnelPower`
  - `SkinSpecMask`
- Extended the PBR-like fragment path for `SurfaceModel = Skin`:
  - primary lobe scaling for the base PBR-like highlight
  - secondary GGX direct specular lobe
  - secondary reflection-probe indirect specular lobe
  - mask-based region control
  - grazing-angle fresnel weighting
  - reuse of the shared specular occlusion path
- Exposed the new skin lobe controls in the Specular inspector foldout when `PBR-like` is active.
- Extended `MaterialValidator` texture coverage for `SkinSpecMask`.

Known limits:
- Skin dual-lobe currently only affects the `PBR-like` shading path.
- The secondary lobe uses the base surface normal; there is no dedicated oil-film normal yet.
- There is no surface-model-specific validator or preset bundle yet.

Date: 2026-03-11
Scope: Real character shader hair direction / transmission slice

Implemented in this pass:
- Added hair realism properties to the shader family:
  - `HairStrandDirectionMap`
  - `HairStrandDirectionStrength`
  - `HairTransmissionColor`
  - `HairTransmissionStrength`
  - `HairTransmissionPower`
  - `HairTransmissionMask`
- Extended hair lighting helpers to:
  - resolve strand direction from a direction map blended against the default binormal flow
  - keep the extension scoped to `SurfaceModel = Hair`
  - add a transmission / backscatter lobe driven by light-from-behind alignment
- Extended the fragment hair-spec block to combine Kajiya-Kay specular and hair transmission in the same blend / fade path.
- Exposed the new controls in the Hair Specular inspector section.
- Extended `MaterialValidator` texture coverage for the new hair maps.

Known limits:
- Hair transmission is still an analytic forward approximation, not a Marschner-style multiple-scattering model.
- The strand direction map uses tangent/binormal planar flow; there is no dedicated tangent import workflow yet.
- There is still no hair-specific preset bundle or validation contract for required masks.

Date: 2026-03-11
Scope: Real character eye shader realism slice

Implemented in this pass:
- Extended `NataneToonEye.shader` with an opt-in realistic eye stack:
  - iris depth offset
  - limbal ring shading
  - sclera tint / shadow shaping
  - cornea-like top highlight and fresnel
- Routed iris depth through the eye UV preparation path so iris-driven overlays and highlights can sit under the cornea layer more naturally.
- Extended `NataneToonEyeDrawer.cs` with a dedicated `Realistic Eye` section for the new controls.

Known limits:
- The current eye realism stack is analytic and texture-free; there is no dedicated cornea normal, wet-line mask, or mesh-based iris depth yet.
- Lighting remains stylized/unlit-adjacent rather than a full physically-based eye BRDF.
- Unity visual verification is still pending.

Date: 2026-03-11
Scope: Look Mixer first implementation slice

Implemented in this pass:
- Added `Look Mixer` shader properties to the main shader and all packaged variants:
  - `_LookMode`
  - `_ToonWeight`
  - `_NprWeight`
  - `_PbrWeight`
- Extended `NataneToonInput.hlsl` so the fragment path can resolve the new look weights.
- Added `Look Mixer` controls to the Shading inspector:
  - legacy compatibility notice
  - `Look Mode` popup
  - Toon / NPR / PBR sliders
  - starter preset buttons (`Pure Toon`, `Soft NPR`, `Toon-PBR Hybrid`, `Near PBR`)
- Updated Quick Setup presets so `Sharp Anime Style` and `Soft Painting Style` also write explicit look-mixer values.
- Extended preset / share serialization:
  - `MaterialParameterData` now stores look mode, look weights, shading mode, surface model, and schema version
  - `MaterialParameterShareSystem` now exports `schemaVersion`
  - legacy materials are converted to look-mixer-compatible preset data on export/readback
- Fixed the preset contract so `specularIntensity` maps to `_SpecularIntensity`, and added explicit `specularBlend` storage for `_SpecularBlend`.
- Added shader-side weight resolution helpers:
  - legacy materials keep old behavior through runtime fallback
  - explicit Look Mixer materials blend Toon/PBR at the base shading stage
  - NPR weight now scales the illustration stack (Quantize, LUT, Hatching, Watercolor, Soft Filter, Kuwahara, Screen Edge, Color Bleeding, Chromatic Aberration)
- Wired `PBR Weight` into keyword synchronization so `_PBR_LIKE` activates when the explicit look mixer asks for PBR response.
- Added `MaterialValidator` checks for obvious Look Mixer mismatch cases:
  - base shading nearly disabled
  - high `PBR Weight` but weak smoothness / metallic / reflection setup
  - high `NPR Weight` with no NPR illustration keywords enabled

Known limits:
- This first slice treats `NPR Weight` as an independent style strength, while Toon/PBR blend the base response. It is not a strict 3-way normalized mix yet.
- `StandardToon` and `Ramp` paths still mostly follow their existing base shading behavior; the new continuous Toon/PBR mix is strongest in the non-Standard stylized path plus the PBR/PBR-like specular blocks.
- Foldout-state unification between the main inspector and preset editor is still pending.
- Unity visual verification and shader compile verification in a host project are still pending.

Date: 2026-03-11
Scope: lilToon migration P0 fixes + Exact Compatibility first slice

Implemented in this pass:
- Added a new lilToon migration conversion mode:
  - `Exact Compatibility (Preview)`
  - `Visual Match (Approximate)`
  - `Minimal Safe (Legacy)`
- Updated migration tool messaging so `Visual Match` no longer promises perfect parity.
- Fixed the migration normal-map bug:
  - `_ShadowNormalStrength` now scales the copied bump scale without being overwritten afterward.
- Fixed rim blend migration:
  - `_RimBlendMode` is now captured from source properties
  - `ConvertRimBlendMode()` is now used instead of hardcoding Add
- Added a hidden migrated-material flag across the toon shader family:
  - `_LilToonExactCompatibility`
- Wired the exact flag through:
  - main shader
  - packaged toon variants
  - `NataneToonInput.hlsl`
  - `LilToonMigrationTool`
  - preset / share serialization
- Added a parity-focused StandardToon first slice in the fragment path:
  - skip the `min(stIndirectCol, stDirectCol)` safety clamp when exact compatibility is active
  - skip `CompressLightingForSafeRange(...)` in StandardToon composition when exact compatibility is active
  - skip the extra light-color floor `max(..., 0.001)` when exact compatibility is active
- Stopped injecting Natane-specific lighting / GI defaults in exact compatibility migration mode.

Known limits:
- This is a first slice, not a finished exact-match implementation.
- MatCap, outline unit parity, specular semantic parity, and multi-shadow blur / mask parity are still not exact.
- The stricter StandardToon branch is runtime-flag based and intentionally avoids adding new shader keywords.
- Unity visual diff validation has not been run yet.

Date: 2026-03-11
Scope: lilToon migration parity audit + unsupported-parameter follow-up

Implemented in this pass:
- Compared Natane StandardToon migration path against official lilToon forward/shadow code to verify parity assumptions.
- Confirmed that migrated output is still not mathematically identical yet; remaining blockers are now documented instead of being silent.
- Added missing migration coverage for outline width masking:
  - lilToon `_OutlineWidthMask` now maps to Natane `_OutlineWidthMap`
  - `_UseOutlineWidthMap` and `_OUTLINE_WIDTH_MAP` are enabled during migration when needed
- Added hidden exact-compat shadow payloads across the Natane toon shader family:
  - `_ShadowMainStrength`
  - `_Shadow2ndBlur`
  - `_Shadow3rdBlur`
  - `_ShadowStrengthMask`
  - `_ShadowBorderMask`
  - `_ShadowBlurMask`
- Wired those hidden values into the StandardToon exact-compat fragment path:
  - per-layer shadow blur now respects lilToon 2nd/3rd blur values
  - shadow border / blur masks now influence StandardToon exact shadow shaping
  - shadow main strength now affects the indirect shadow color path
- Added migration warnings for lilToon branches that still do not have exact Natane equivalents:
  - `_UseRimShade`
  - `_UseEmission2nd`
  - `_ShadowBorderRange`
  - `_BackfaceForceShadow`
  - `_ShadowMaskType`
  - `_ShadowPostAO`

Known limits after this pass:
- Exact Compatibility is closer for shadow layering, but still not pixel-identical.
- `ShadowBorderRange`, flat/face shadow mask types, backface-force-shadow, rim shade, and second emission still need real runtime support for full parity.
- Unity compile / screenshot diff validation has still not been run in a host project.

Date: 2026-03-11
Scope: LilToon Migration Inspector UX first slice

Implemented in this pass:
- Added persisted lilToon migration metadata across the toon shader family:
  - `_LilToonMigrated`
  - `_LilToonMigrationMode`
  - `_LilToonParityFlags`
- The migration tool now writes:
  - migrated flag
  - migration mode
  - parity warning bitmask
  - override tags for source shader and migration version
- Preset / share serialization now keeps the lilToon migration metadata and tags.
- Added a `LilToon Migration` card to the top of the `Shading` section:
  - migrated badge
  - `Natane / lilToon Match` two-state switch
  - source shader
  - migration mode
  - parity warning count
  - parity detail foldout
  - quick actions to reapply lilToon match or reopen the migration tool
- `Look Mixer` is now read-only while `lilToon Match` is ON, including starter preset buttons.
- Toggling `lilToon Match` from the Inspector now:
  - updates `_LilToonExactCompatibility`
  - forces `_ShadingMode = StandardToon` when enabling compatibility mode

Known limits after this pass:
- Old migrated materials created before metadata existed may not show the new card unless they still have `_LilToonExactCompatibility` enabled or are re-migrated.
- Warning details are currently summarized from a parity bitmask, not from a full per-feature runtime diff.
- Unity compile verification and in-editor visual validation are still pending.
