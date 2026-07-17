// NataneToonBuildSettings.hlsl
// Auto-managed by NataneBuildFeatureOptimizer.
// During builds, this file is rewritten to contain only the features actually used.
// In the editor (non-build), all features are enabled by default.
// DO NOT EDIT MANUALLY - changes will be overwritten during builds.

#ifndef NATANE_BUILD_SETTINGS_INCLUDED
#define NATANE_BUILD_SETTINGS_INCLUDED

// Default: all features enabled (editor mode).
// During VRChat/platform builds, NataneBuildFeatureOptimizer rewrites this file
// to #define only the features actually referenced by project materials,
// ensuring unused features are stripped even if shader keywords are desynchronized.
#define NATANE_BUILD_ALL_FEATURES

// -----------------------------------------------------------------
// Build-time keyword guard: when NATANE_BUILD_ALL_FEATURES is NOT
// defined (i.e. during a build), force-#undef any shader keyword
// whose corresponding NATANE_FEATURE_* is absent. This prevents
// desynchronized keywords from enabling features the user disabled.
// -----------------------------------------------------------------
#ifndef NATANE_BUILD_ALL_FEATURES

// Main Texture Animation
#if defined(_MAIN_TEX_ANIMATION) && !defined(NATANE_FEATURE_MAIN_TEX_ANIMATION)
    #undef _MAIN_TEX_ANIMATION
#endif

// Makeup Textures
#if defined(_2ND_TEXTURE) && !defined(NATANE_FEATURE_2ND_TEXTURE)
    #undef _2ND_TEXTURE
#endif
#if defined(_3RD_TEXTURE) && !defined(NATANE_FEATURE_3RD_TEXTURE)
    #undef _3RD_TEXTURE
#endif
#if defined(_4TH_TEXTURE) && !defined(NATANE_FEATURE_4TH_TEXTURE)
    #undef _4TH_TEXTURE
#endif
#if defined(_5TH_TEXTURE) && !defined(NATANE_FEATURE_5TH_TEXTURE)
    #undef _5TH_TEXTURE
#endif

// Shading
#if defined(_USE_RAMP) && !defined(NATANE_FEATURE_USE_RAMP)
    #undef _USE_RAMP
#endif
#if defined(_USE_MULTI_SHADOW) && !defined(NATANE_FEATURE_USE_MULTI_SHADOW)
    #undef _USE_MULTI_SHADOW
#endif
#if defined(_SHADOW_RECEIVE_MASK) && !defined(NATANE_FEATURE_SHADOW_RECEIVE_MASK)
    #undef _SHADOW_RECEIVE_MASK
#endif
#if defined(_USE_AO) && !defined(NATANE_FEATURE_USE_AO)
    #undef _USE_AO
#endif
#if defined(_USE_DITHERING) && !defined(NATANE_FEATURE_USE_DITHERING)
    #undef _USE_DITHERING
#endif
#if defined(_SDF_MAP) && !defined(NATANE_FEATURE_SDF_MAP)
    #undef _SDF_MAP
#endif
#if defined(_SHADING_GRADE_MAP) && !defined(NATANE_FEATURE_SHADING_GRADE_MAP)
    #undef _SHADING_GRADE_MAP
#endif

// Lighting
#if defined(_SOFT_LIGHTING_MODE) && !defined(NATANE_FEATURE_SOFT_LIGHTING_MODE)
    #undef _SOFT_LIGHTING_MODE
#endif
#if defined(_USE_LIGHT_VOLUME) && !defined(NATANE_FEATURE_USE_LIGHT_VOLUME)
    #undef _USE_LIGHT_VOLUME
#endif
#if defined(_LIGHT_VOLUME_SPECULAR) && !defined(NATANE_FEATURE_LIGHT_VOLUME_SPECULAR)
    #undef _LIGHT_VOLUME_SPECULAR
#endif
#if defined(_PIXEL_VERTEX_LIGHTS) && !defined(NATANE_FEATURE_PIXEL_VERTEX_LIGHTS)
    #undef _PIXEL_VERTEX_LIGHTS
#endif

// Specular
#if defined(_SPECULAR) && !defined(NATANE_FEATURE_SPECULAR)
    #undef _SPECULAR
#endif
#if defined(_SPECULAR_DITHER) && !defined(NATANE_FEATURE_SPECULAR_DITHER)
    #undef _SPECULAR_DITHER
#endif
#if defined(_HAIR_SPECULAR) && !defined(NATANE_FEATURE_HAIR_SPECULAR)
    #undef _HAIR_SPECULAR
#endif

// Rim Light
#if defined(_RIM_LIGHT) && !defined(NATANE_FEATURE_RIM_LIGHT)
    #undef _RIM_LIGHT
#endif
#if defined(_RIM_LIGHT_2) && !defined(NATANE_FEATURE_RIM_LIGHT_2)
    #undef _RIM_LIGHT_2
#endif
#if defined(_OFFSET_RIM_LIGHT) && !defined(NATANE_FEATURE_OFFSET_RIM_LIGHT)
    #undef _OFFSET_RIM_LIGHT
#endif

// SSS
#if defined(_SSS) && !defined(NATANE_FEATURE_SSS)
    #undef _SSS
#endif

// MatCap
#if defined(_MATCAP) && !defined(NATANE_FEATURE_MATCAP)
    #undef _MATCAP
#endif
#if defined(_MATCAP_2) && !defined(NATANE_FEATURE_MATCAP_2)
    #undef _MATCAP_2
#endif
#if defined(_MATCAP_3) && !defined(NATANE_FEATURE_MATCAP_3)
    #undef _MATCAP_3
#endif

// Glitter
#if defined(_GLITTER) && !defined(NATANE_FEATURE_GLITTER)
    #undef _GLITTER
#endif
#if defined(_GLINTS_ADVANCED) && !defined(NATANE_FEATURE_GLINTS_ADVANCED)
    #undef _GLINTS_ADVANCED
#endif

// Reflection
#if defined(_REFLECTION) && !defined(NATANE_FEATURE_REFLECTION)
    #undef _REFLECTION
#endif

// Iridescence
#if defined(_IRIDESCENCE) && !defined(NATANE_FEATURE_IRIDESCENCE)
    #undef _IRIDESCENCE
#endif

// Environmental Rim
#if defined(_ENV_RIM) && !defined(NATANE_FEATURE_ENV_RIM)
    #undef _ENV_RIM
#endif

// Outline
#if defined(_OUTLINE) && !defined(NATANE_FEATURE_OUTLINE)
    #undef _OUTLINE
#endif
#if defined(_OUTLINE_TEXTURE_COLOR) && !defined(NATANE_FEATURE_OUTLINE_TEXTURE_COLOR)
    #undef _OUTLINE_TEXTURE_COLOR
#endif
#if defined(_SMOOTH_NORMAL) && !defined(NATANE_FEATURE_SMOOTH_NORMAL)
    #undef _SMOOTH_NORMAL
#endif

// Emission
#if defined(_EMISSION) && !defined(NATANE_FEATURE_EMISSION)
    #undef _EMISSION
#endif

// Normal Map
#if defined(_NORMALMAP) && !defined(NATANE_FEATURE_NORMALMAP)
    #undef _NORMALMAP
#endif

// Virtual Expression
#if defined(_DISSOLVE) && !defined(NATANE_FEATURE_DISSOLVE)
    #undef _DISSOLVE
#endif
#if defined(_ALPHA_MASK) && !defined(NATANE_FEATURE_ALPHA_MASK)
    #undef _ALPHA_MASK
#endif
#if defined(_HUE_SHIFT) && !defined(NATANE_FEATURE_HUE_SHIFT)
    #undef _HUE_SHIFT
#endif

// Parallax
#if defined(_PARALLAX) && !defined(NATANE_FEATURE_PARALLAX)
    #undef _PARALLAX
#endif

// Refraction
#if defined(_REFRACTION) && !defined(NATANE_FEATURE_REFRACTION)
    #undef _REFRACTION
#endif

// AudioLink
#if defined(_AUDIOLINK) && !defined(NATANE_FEATURE_AUDIOLINK)
    #undef _AUDIOLINK
#endif

// Distance Fade
#if defined(_DISTANCE_FADE) && !defined(NATANE_FEATURE_DISTANCE_FADE)
    #undef _DISTANCE_FADE
#endif

// Vertex Animation
#if defined(_VERTEX_ANIMATION) && !defined(NATANE_FEATURE_VERTEX_ANIMATION)
    #undef _VERTEX_ANIMATION
#endif

// VAT
#if defined(_VAT) && !defined(NATANE_FEATURE_VAT)
    #undef _VAT
#endif
#if defined(_VAT_NORMAL) && !defined(NATANE_FEATURE_VAT_NORMAL)
    #undef _VAT_NORMAL
#endif

// Water Drip
#if defined(_WATER_DRIP) && !defined(NATANE_FEATURE_WATER_DRIP)
    #undef _WATER_DRIP
#endif

// Smear
#if defined(_SMEAR) && !defined(NATANE_FEATURE_SMEAR)
    #undef _SMEAR
#endif

// Hologram / Glitch
#if defined(_HOLOGRAM) && !defined(NATANE_FEATURE_HOLOGRAM)
    #undef _HOLOGRAM
#endif
#if defined(_GLITCH) && !defined(NATANE_FEATURE_GLITCH)
    #undef _GLITCH
#endif
#if defined(_GLITCH_STRETCH) && !defined(NATANE_FEATURE_GLITCH_STRETCH)
    #undef _GLITCH_STRETCH
#endif
#if defined(_HOLOGRAM_NOISE) && !defined(NATANE_FEATURE_HOLOGRAM_NOISE)
    #undef _HOLOGRAM_NOISE
#endif

// Illustration Style
#if defined(_COLOR_QUANTIZE) && !defined(NATANE_FEATURE_COLOR_QUANTIZE)
    #undef _COLOR_QUANTIZE
#endif
#if defined(_LUT_3D) && !defined(NATANE_FEATURE_LUT_3D)
    #undef _LUT_3D
#endif
#if defined(_HATCHING) && !defined(NATANE_FEATURE_HATCHING)
    #undef _HATCHING
#endif
#if defined(_WATERCOLOR) && !defined(NATANE_FEATURE_WATERCOLOR)
    #undef _WATERCOLOR
#endif
#if defined(_SOFT_FILTER) && !defined(NATANE_FEATURE_SOFT_FILTER)
    #undef _SOFT_FILTER
#endif
#if defined(_KUWAHARA_FILTER) && !defined(NATANE_FEATURE_KUWAHARA_FILTER)
    #undef _KUWAHARA_FILTER
#endif
#if defined(_SCREEN_EDGE) && !defined(NATANE_FEATURE_SCREEN_EDGE)
    #undef _SCREEN_EDGE
#endif
#if defined(_COLOR_BLEEDING) && !defined(NATANE_FEATURE_COLOR_BLEEDING)
    #undef _COLOR_BLEEDING
#endif
#if defined(_CHROMATIC_ABERRATION) && !defined(NATANE_FEATURE_CHROMATIC_ABERRATION)
    #undef _CHROMATIC_ABERRATION
#endif
#if defined(_OUTLINE_HAND_DRAWN) && !defined(NATANE_FEATURE_OUTLINE_HAND_DRAWN)
    #undef _OUTLINE_HAND_DRAWN
#endif

// Decal
#if defined(_DECAL) && !defined(NATANE_FEATURE_DECAL)
    #undef _DECAL
#endif

// Backface Texture
#if defined(_BACKFACE_TEXTURE) && !defined(NATANE_FEATURE_BACKFACE_TEXTURE)
    #undef _BACKFACE_TEXTURE
#endif

// Video Texture
#if defined(_VIDEO_TEXTURE) && !defined(NATANE_FEATURE_VIDEO_TEXTURE)
    #undef _VIDEO_TEXTURE
#endif

// LTCGI
#if defined(_LTCGI) && !defined(NATANE_FEATURE_LTCGI)
    #undef _LTCGI
#endif

// Screen Tone
#if defined(_SCREEN_TONE) && !defined(NATANE_FEATURE_SCREEN_TONE)
    #undef _SCREEN_TONE
#endif

// Gradient Base Color
#if defined(_GRADIENT_BASE_COLOR) && !defined(NATANE_FEATURE_GRADIENT_BASE_COLOR)
    #undef _GRADIENT_BASE_COLOR
#endif

// Blue Noise Dither
#if defined(_BLUE_NOISE_DITHER) && !defined(NATANE_FEATURE_BLUE_NOISE_DITHER)
    #undef _BLUE_NOISE_DITHER
#endif

// Dithering Alpha
#if defined(_DITHERING_ALPHA) && !defined(NATANE_FEATURE_DITHERING_ALPHA)
    #undef _DITHERING_ALPHA
#endif
#if defined(_HASHED_ALPHA) && !defined(NATANE_FEATURE_HASHED_ALPHA)
    #undef _HASHED_ALPHA
#endif

// PCSS Soft Shadow
#if defined(_PCSS) && !defined(NATANE_FEATURE_PCSS)
    #undef _PCSS
#endif

// Outline sub-keywords
#if defined(_OUTLINE_MASK) && !defined(NATANE_FEATURE_OUTLINE_MASK)
    #undef _OUTLINE_MASK
#endif
#if defined(_OUTLINE_WIDTH_MAP) && !defined(NATANE_FEATURE_OUTLINE_WIDTH_MAP)
    #undef _OUTLINE_WIDTH_MAP
#endif
#if defined(_OUTLINE_MULTI_COLOR) && !defined(NATANE_FEATURE_OUTLINE_MULTI_COLOR)
    #undef _OUTLINE_MULTI_COLOR
#endif

// Halftone Shadow
#if defined(_HALFTONE_SHADOW) && !defined(NATANE_FEATURE_HALFTONE_SHADOW)
    #undef _HALFTONE_SHADOW
#endif

// Shadow Edge Noise
#if defined(_SHADOW_EDGE_NOISE) && !defined(NATANE_FEATURE_SHADOW_EDGE_NOISE)
    #undef _SHADOW_EDGE_NOISE
#endif

// Cast Shadow Color
#if defined(_CAST_SHADOW_COLOR) && !defined(NATANE_FEATURE_CAST_SHADOW_COLOR)
    #undef _CAST_SHADOW_COLOR
#endif

// Light Snap
#if defined(_LIGHT_SNAP) && !defined(NATANE_FEATURE_LIGHT_SNAP)
    #undef _LIGHT_SNAP
#endif

// Procedural MatCap
#if defined(_PROCEDURAL_MATCAP) && !defined(NATANE_FEATURE_PROCEDURAL_MATCAP)
    #undef _PROCEDURAL_MATCAP
#endif

// Fake Reflection
#if defined(_FAKE_REFLECTION) && !defined(NATANE_FEATURE_FAKE_REFLECTION)
    #undef _FAKE_REFLECTION
#endif

// Perspective Flat
#if defined(_PERSPECTIVE_FLAT) && !defined(NATANE_FEATURE_PERSPECTIVE_FLAT)
    #undef _PERSPECTIVE_FLAT
#endif

// Face Ortho Projection
#if defined(_FACE_ORTHO) && !defined(NATANE_FEATURE_FACE_ORTHO)
    #undef _FACE_ORTHO
#endif
#if defined(_FACE_ORTHO_MASK) && !defined(NATANE_FEATURE_FACE_ORTHO_MASK)
    #undef _FACE_ORTHO_MASK
#endif

// Mirror / Camera Alternate Texture
#if defined(_MIRROR_TEXTURE) && !defined(NATANE_FEATURE_MIRROR_TEXTURE)
    #undef _MIRROR_TEXTURE
#endif

// Height Fade / Intersection Fade
#if defined(_HEIGHT_FADE) && !defined(NATANE_FEATURE_HEIGHT_FADE)
    #undef _HEIGHT_FADE
#endif
#if defined(_INTERSECTION_FADE) && !defined(NATANE_FEATURE_INTERSECTION_FADE)
    #undef _INTERSECTION_FADE
#endif

// Tessellation
#if defined(_TESSELLATION) && !defined(NATANE_FEATURE_TESSELLATION)
    #undef _TESSELLATION
#endif
#if defined(_TESS_DISPLACEMENT) && !defined(NATANE_FEATURE_TESS_DISPLACEMENT)
    #undef _TESS_DISPLACEMENT
#endif

// Depth Color Fade
#if defined(_DEPTH_COLOR_FADE) && !defined(NATANE_FEATURE_DEPTH_COLOR_FADE)
    #undef _DEPTH_COLOR_FADE
#endif

// Mirror / Camera Control
#if defined(_MIRROR_CONTROL) && !defined(NATANE_FEATURE_MIRROR_CONTROL)
    #undef _MIRROR_CONTROL
#endif

// Angel Ring
#if defined(_ANGEL_RING) && !defined(NATANE_FEATURE_ANGEL_RING)
    #undef _ANGEL_RING
#endif

// Detail Map
#if defined(_DETAIL_MAP) && !defined(NATANE_FEATURE_DETAIL_MAP)
    #undef _DETAIL_MAP
#endif

// Procedural AO
#if defined(_PROCEDURAL_AO) && !defined(NATANE_FEATURE_PROCEDURAL_AO)
    #undef _PROCEDURAL_AO
#endif

// Normal Warp
#if defined(_NORMAL_WARP) && !defined(NATANE_FEATURE_NORMAL_WARP)
    #undef _NORMAL_WARP
#endif

// Specular Anti-Aliasing
#if defined(_SPECULAR_AA) && !defined(NATANE_FEATURE_SPECULAR_AA)
    #undef _SPECULAR_AA
#endif

// Vertex Color Shadow
#if defined(_VERTEX_COLOR_SHADOW) && !defined(NATANE_FEATURE_VERTEX_COLOR_SHADOW)
    #undef _VERTEX_COLOR_SHADOW
#endif

// Face SDF Rotation
#if defined(_FACE_SDF_ROTATION) && !defined(NATANE_FEATURE_FACE_SDF_ROTATION)
    #undef _FACE_SDF_ROTATION
#endif

// Sheen
#if defined(_SHEEN) && !defined(NATANE_FEATURE_SHEEN)
    #undef _SHEEN
#endif

// SSS LUT
#if defined(_SSS_LUT) && !defined(NATANE_FEATURE_SSS_LUT)
    #undef _SSS_LUT
#endif

// Surface Cover
#if defined(_SURFACE_COVER) && !defined(NATANE_FEATURE_SURFACE_COVER)
    #undef _SURFACE_COVER
#endif

// Triplanar
#if defined(_TRIPLANAR) && !defined(NATANE_FEATURE_TRIPLANAR)
    #undef _TRIPLANAR
#endif

// Height Fog
#if defined(_HEIGHT_FOG) && !defined(NATANE_FEATURE_HEIGHT_FOG)
    #undef _HEIGHT_FOG
#endif

// Fur
#if defined(_FUR) && !defined(NATANE_FEATURE_FUR)
    #undef _FUR
#endif

// Eye Parallax
#if defined(_EYE_PARALLAX) && !defined(NATANE_FEATURE_EYE_PARALLAX)
    #undef _EYE_PARALLAX
#endif

#endif // !NATANE_BUILD_ALL_FEATURES

#endif // NATANE_BUILD_SETTINGS_INCLUDED
