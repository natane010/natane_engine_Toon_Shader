# Shader Style and Optimization Audit

Date: 2026-02-13

## 1) Optimization Status (Current)

### Implemented in this update
- Editor prewarm optimization:
  - `Shader.WarmupAllShaders()` was previously called once per matched material.
  - It is now called once after material scan (`Editor/NataneToon/Tools/ShaderPrewarmingEditor.cs`), which avoids redundant full-project warmup calls.
- Fragment/utility micro-optimizations:
  - Replaced `length(v) < t` checks with squared-length checks (`dot(v, v) < t^2`) in refraction paths.
  - Removed a dynamic branch in dissolve edge glow application (branchless blend path).
  - Replaced UV scroll magnitude check from `length(scrollSpeed)` to squared-length compare.

### Observed remaining hotspots
- Very high keyword surface area in main shader passes (many `shader_feature` toggles).
- `GrabPass` usage (refraction) remains expensive on lower-end VR targets.
- Optional heavy features (parallax, refraction blur, multi-layer effects) can stack cost quickly.

### Practical result
- CPU-side Editor prewarm cost is improved.
- GPU-side cost is incrementally improved in hot utility paths.
- Biggest runtime wins still depend on per-material feature discipline and variant curation.

## 2) Rendering Style Positioning

This is a code-structure and parameter-model comparison against common usage patterns.

### Relative similarity (estimated)
- Anime-style cel-shading: **High**
- lilToon-like: **High**
- Effect-heavy style: **Medium**

### Why
- アニメ調セルシェーディング寄り:
  - Toon step + sharpness + shadow color-centric lighting flow.
  - Shadow color mixing and stylized cel boundary controls.
- lilToon寄り:
  - Multi-layer parameter richness (MatCap/masks/rim/spec/SSS etc.).
  - Workflow-oriented controls for VRChat and Light Volume integration.
- エフェクト重視スタイル寄り:
  - Feature breadth and effect stack are similar.
  - But shading philosophy is less physically-layered and more cel/shadow-grade centered than typical effect-heavy tuning.

## 3) Tuning Direction to Match Each Look

### More Anime-Style Cel-Shading
- Keep:
  - `_ShadingMode = Toon`
  - Lower `_ShadowSteps` (2-3), higher edge clarity
  - Minimal reflections/FX
- Reduce:
  - Extra layers (multi MatCap, heavy emission dynamics)

### More lilToon-like
- Keep:
  - Multi-shadow usage
  - MatCap + Rim + mask workflows
  - Light Volume integration
- Add:
  - Subtle softness and AO controls while preserving cel hierarchy

### More Effect-Heavy Style
- Increase:
  - Emission dynamics, stylized effect stacks, refraction/iridescence interplay
- Watch:
  - Feature accumulation can hurt VR target performance quickly

## 4) Added VRC Screen FX (for combined expression)

Added:
- Shader: `Shaders/NataneToon/Effects/NataneScreenFXOverlay.shader`
- Setup tool: `Editor/NataneToon/Tools/ScreenFXSetupTool.cs`
- Usage notes: `Shaders/NataneToon/Effects/NataneScreenFX_README.md`

Purpose:
- Provide a camera-attached, custom post-like overlay effect for VRC worlds without runtime scripts.
- Designed to layer on top of Natane Toon rendering for stronger presentation control.
