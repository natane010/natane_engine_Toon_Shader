#ifndef NATANE_TOON_CORE_INCLUDED
#define NATANE_TOON_CORE_INCLUDED

// Natane Toon Shader Core
// Modular cel-shading/NPR shader for Unity Built-in Render Pipeline
//
// Architecture:
// - NataneToonInput.hlsl: Properties and data structures
// - NataneToonUtils.hlsl: Utility functions
// - NataneToonLighting.hlsl: Lighting calculation functions
// - NataneToonVertex.hlsl: Vertex shader
// - NataneToonFragment.hlsl: Fragment shader

// Unity includes
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

// Natane Toon Shader modules
#include "NataneToonInput.hlsl"
#include "NataneToonUtils.hlsl"
#include "NataneToonLighting.hlsl"
#include "NataneToonVertex.hlsl"
#include "NataneToonFragment.hlsl"

#endif // NATANE_TOON_CORE_INCLUDED
