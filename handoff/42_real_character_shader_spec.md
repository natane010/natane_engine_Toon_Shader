# Real Character Shader Implementation Spec

Date: 2026-03-11
Scope: `NataneToonShader` realistic-character foundation for Built-in RP

## Goal

Add a realistic-character foundation without breaking the existing toon workflow.

This phase focuses on:
- material classification for character surfaces
- micro-detail normal workflow
- cavity and specular occlusion
- transmission masking for skin-like SSS

This phase intentionally does not include:
- full energy-conserving layered clear coat refinement
- physically-based eye stack overhaul
- full hair transmission model
- screen-space SSS

## Design Rules

- Keep existing toon and standard-toon behavior intact by default.
- Add new controls as opt-in properties with safe defaults.
- Avoid new shader keywords unless they are necessary.
- Reuse existing AO / SSS / reflection structure instead of creating a parallel pipeline.
- Treat this package as artist-facing tooling, so GUI and validation have to stay readable.

## New Material Contract

### Surface Classification

- `_SurfaceModel`
  - `0 = Default`
  - `1 = Skin`
  - `2 = Hair`
  - `3 = Eye`
  - `4 = Cloth`

### Micro Detail

- `_MicroNormalMap`
- `_MicroNormalScale`
- `_MicroNormalTiling`
- `_MicroNormalStrength`

### Micro Occlusion

- `_CavityMap`
- `_CavityStrength`
- `_SpecularOcclusionStrength`

### Transmission

- `_TransmissionMask`
- `_TransmissionStrength`

### Clear Coat

- `_ClearCoatIntensity`
- `_ClearCoatSmoothness`
- `_ClearCoatMask`
- `_ClearCoatNormalMap`
- `_ClearCoatNormalScale`
- `_ClearCoatFresnelPower`

### Skin Dual-Lobe

- `_SkinSpecPrimaryStrength`
- `_SkinSpecSecondaryStrength`
- `_SkinSpecSecondarySmoothness`
- `_SkinSpecSecondaryColor`
- `_SkinSpecFresnelPower`
- `_SkinSpecMask`

### Hair Direction / Transmission

- `_HairStrandDirectionMap`
- `_HairStrandDirectionStrength`
- `_HairTransmissionColor`
- `_HairTransmissionStrength`
- `_HairTransmissionPower`
- `_HairTransmissionMask`

### Eye Realism

- `_UseRealisticEye`
- `_IrisDepth`
- `_IrisDepthRadius`
- `_LimbalRingColor`
- `_LimbalRingWidth`
- `_LimbalRingIntensity`
- `_ScleraTint`
- `_ScleraShadowStrength`
- `_CorneaSpecColor`
- `_CorneaSpecIntensity`
- `_CorneaSpecSmoothness`
- `_CorneaFresnelPower`

## Checklist

- [x] SPEC-00 Define first implementation slice and defer high-risk features
- [x] SPEC-01 Add implementation spec to `handoff`
- [x] SPEC-02 Add `SurfaceModel` property to the toon shader family
- [x] SPEC-03 Add shared realistic-character properties to shader property blocks
- [x] SPEC-04 Add CBUFFER / texture declarations for new properties
- [x] SPEC-05 Add GUI exposure for surface model, micro normal, cavity, specular occlusion, transmission
- [x] SPEC-06 Blend micro normal into the existing normal pipeline
- [x] SPEC-07 Compute cavity visibility and specular occlusion in the fragment path
- [x] SPEC-08 Apply specular occlusion to PBR-like, stylized specular, hair specular, reflection, LTCGI/light-volume specular
- [x] SPEC-09 Apply transmission mask to SSS contribution
- [x] SPEC-10 Extend validator texture property coverage for the new maps
- [x] SPEC-11 Add clear coat / coat mask / coat normal
- [x] SPEC-12 Add skin dual-lobe specular
- [x] SPEC-13 Add hair transmission / strand-direction workflow
- [x] SPEC-14 Extend `NataneToonEye.shader` for realistic eye stack
- [ ] SPEC-15 Add real-character presets
- [ ] SPEC-16 Add dedicated validator rules per `SurfaceModel`
- [ ] SPEC-17 Unity visual validation in a host project

## File Ownership

- Shader properties:
  - `Shaders/NataneToon/NataneToonShader.shader`
  - `Shaders/NataneToon/Variants/*.shader`
- Runtime inputs and shading:
  - `Shaders/NataneToon/Include/Core/NataneToonInput.hlsl`
  - `Shaders/NataneToon/Include/Lighting/NataneToonLighting.hlsl`
  - `Shaders/NataneToon/Include/Rendering/NataneToonFragment.hlsl`
- Editor tooling:
  - `Editor/NataneToon/GUI/NataneToonShaderGUI.cs`
  - `Editor/NataneToon/GUI/NataneToonShaderGUIHelpers.cs`
  - `Editor/NataneToon/Tools/MaterialValidator.cs`

## Validation Checklist

- [ ] Existing toon materials still render with no visible change when new properties stay at defaults
- [ ] PBR-like mode shows the new realistic controls in the inspector
- [ ] Micro normal adds fine breakup without replacing the base normal map
- [ ] Cavity + specular occlusion suppresses highlights inside pores and creases
- [ ] Transmission mask limits SSS to expected regions
- [ ] Reflection and hair/specular respect the same occlusion visibility
- [ ] Transparent / cutout / lite variants still expose the new properties without inspector errors
- [ ] `git diff --check` passes

## Next Slice

After this phase, the next practical slice is:
1. presets and validator rules
2. surface-model-specific validator rules
3. Unity visual validation in a host project
