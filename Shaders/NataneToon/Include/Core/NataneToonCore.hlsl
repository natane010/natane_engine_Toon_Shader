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
// Organized by functionality for better maintainability
#include "NataneToonInput.hlsl"
#include "../Utils/NataneToonUtils.hlsl"
#include "../Lighting/NataneToonLighting.hlsl"
#include "NataneToonVertex.hlsl"
#include "../Rendering/NataneToonFragment.hlsl"

#endif // NATANE_TOON_CORE_INCLUDED
