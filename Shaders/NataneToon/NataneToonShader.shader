Shader "Natane/Toon Shader"
{
    Properties
    {
        [Header(Main Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        [Space(10)]
        [Toggle(_MAIN_TEX_ANIMATION)] _MainTexAnimation ("Main Tex Animation", Float) = 0
        _MainTexScrollSpeed ("Scroll Speed XY", Vector) = (0,0,0,0)
        _MainTexRotateSpeed ("Rotate Speed", Float) = 0

        [Header(Color Preservation)]
        _AlbedoPreservation ("Texture Color Preservation", Range(0, 1)) = 0
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Brightness ("Overall Brightness", Range(0.5, 1.5)) = 1

        [Header(Surface Finish)]
        _Glossiness ("Glossiness Overall Gloss", Range(0, 1)) = 1
        _MatteEffect ("Matte Effect Reduce Gloss", Range(0, 1)) = 0

        [Header(Final Color Blending)]
        _FinalHighlightBlend ("Highlight Compression Prevent White Blowout", Range(0, 1)) = 0
        _HighlightThreshold ("Highlight Threshold Start Point", Range(0, 1)) = 0.75
        _FinalShadowBlend ("Shadow Lift Prevent Black Crush", Range(0, 1)) = 0
        _ShadowThreshold ("Shadow Threshold Start Point", Range(0, 1)) = 0.25

        [Header(Makeup Textures)]
        [Toggle(_2ND_TEXTURE)] _Use2ndTexture ("Enable 2nd Texture", Float) = 0
        _2ndTex ("Texture Makeup", 2D) = "white" {}
        _2ndTexHueShift ("Hue Shift", Range(-0.5, 0.5)) = 0
        _2ndTexSaturation ("Saturation", Range(0, 2)) = 1
        _2ndTexValue ("Brightness", Range(0, 2)) = 1
        _2ndTexIntensity ("2nd Tex Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Overlay,2,Screen,3)] _2ndTexBlendMode ("2nd Tex Blend Mode", Float) = 0
        [Toggle(_2ND_TEX_MASK)] _Use2ndTexMask ("Use 2nd Tex Mask", Float) = 0
        _2ndTexMask ("2nd Tex Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_3RD_TEXTURE)] _Use3rdTexture ("Enable 3rd Texture", Float) = 0
        _3rdTex ("Texture Makeup", 2D) = "white" {}
        _3rdTexHueShift ("Hue Shift", Range(-0.5, 0.5)) = 0
        _3rdTexSaturation ("Saturation", Range(0, 2)) = 1
        _3rdTexValue ("Brightness", Range(0, 2)) = 1
        _3rdTexIntensity ("3rd Tex Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Overlay,2,Screen,3)] _3rdTexBlendMode ("3rd Tex Blend Mode", Float) = 0
        [Toggle(_3RD_TEX_MASK)] _Use3rdTexMask ("Use 3rd Tex Mask", Float) = 0
        _3rdTexMask ("3rd Tex Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_4TH_TEXTURE)] _Use4thTexture ("Enable 4th Texture", Float) = 0
        _4thTex ("Texture Makeup", 2D) = "white" {}
        _4thTexHueShift ("Hue Shift", Range(-0.5, 0.5)) = 0
        _4thTexSaturation ("Saturation", Range(0, 2)) = 1
        _4thTexValue ("Brightness", Range(0, 2)) = 1
        _4thTexIntensity ("4th Tex Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Overlay,2,Screen,3)] _4thTexBlendMode ("4th Tex Blend Mode", Float) = 0
        [Toggle(_4TH_TEX_MASK)] _Use4thTexMask ("Use 4th Tex Mask", Float) = 0
        _4thTexMask ("4th Tex Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_5TH_TEXTURE)] _Use5thTexture ("Enable 5th Texture", Float) = 0
        _5thTex ("Texture Makeup", 2D) = "white" {}
        _5thTexHueShift ("Hue Shift", Range(-0.5, 0.5)) = 0
        _5thTexSaturation ("Saturation", Range(0, 2)) = 1
        _5thTexValue ("Brightness", Range(0, 2)) = 1
        _5thTexIntensity ("5th Tex Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Overlay,2,Screen,3)] _5thTexBlendMode ("5th Tex Blend Mode", Float) = 0
        [Toggle(_5TH_TEX_MASK)] _Use5thTexMask ("Use 5th Tex Mask", Float) = 0
        _5thTexMask ("5th Tex Mask", 2D) = "white" {}

        [Header(Shading)]
        [Enum(Toon,0,Gradient,1)] _ShadingMode ("Shading Mode", Float) = 0
        _ShadingGradientWidth ("Gradient Width", Range(0.001, 1)) = 0.2
        [Toggle(_USE_RAMP)] _UseRamp ("Use Ramp Texture", Float) = 0
        _RampTex ("Ramp Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color 1st", Color) = (0.5, 0.5, 0.5, 1)
        [Toggle(_USE_MULTI_SHADOW)] _UseMultiShadow ("Use Multi-tone Shadow", Float) = 0
        _Shadow2ndColor ("Shadow Color 2nd", Color) = (0.35, 0.35, 0.35, 1)
        _Shadow2ndBorder ("2nd Shadow Border", Range(0, 1)) = 0.3
        _Shadow3rdColor ("Shadow Color 3rd", Color) = (0.2, 0.2, 0.2, 1)
        _Shadow3rdBorder ("3rd Shadow Border", Range(0, 1)) = 0.15
        _ShadowSteps ("Shadow Steps", Range(1, 10)) = 2
        _ShadowSharpness ("Shadow Sharpness", Range(0.001, 1)) = 0.1
        _ShadowOffset ("Shadow Offset", Range(-1, 1)) = 0
        _LitSoftness ("Lit Area Softness Global Smoothstep", Range(0, 1)) = 0
        _ShadowBlend ("Shadow Blend Softness", Range(0, 1)) = 0
        [Toggle(_SHADOW_RECEIVE_MASK)] _UseShadowReceiveMask ("Use Shadow Receive Mask", Float) = 0
        _ShadowReceiveMask ("Shadow Receive Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_SDF_MAP)] _UseSDFMap ("Use SDF Shadow Map", Float) = 0
        _SDFMap ("SDF Shadow Map", 2D) = "white" {}
        _SDFIntensity ("SDF Intensity", Range(0, 1)) = 0.5
        _SDFSoftness ("SDF Softness", Range(0, 1)) = 0.1
        _SDFOffset ("SDF Offset", Range(-1, 1)) = 0
        [Space(10)]
        [Toggle(_SHADING_GRADE_MAP)] _UseGradeMap ("Use Shading Grade Map", Float) = 0
        _ShadingGradeMap ("Shading Grade Map", 2D) = "white" {}
        _ShadingGradeScale ("Shading Grade Scale", Range(-1, 1)) = 0
        [Toggle(_USE_AO)] _UseAO ("Use Ambient Occlusion", Float) = 0
        _AOMap ("AO Map", 2D) = "white" {}
        _AOIntensity ("AO Intensity", Range(0, 1)) = 1
        [Toggle(_USE_DITHERING)] _UseDithering ("Use Dithering Shadow Edge Only", Float) = 0
        _DitheringScale ("Dithering Scale Pattern Size", Range(1, 100)) = 10
        _DitheringStrength ("Dithering Strength Boundary Softness", Range(0, 1)) = 0.5
        [Space(10)]
        [Toggle(_SHADOW_COLOR_TEX)] _UseShadowColorTex ("Use Shadow Color Texture", Float) = 0
        _ShadowColorTex ("Shadow Color Texture", 2D) = "white" {}
        _ShadowColorTexStrength ("Shadow Color Tex Strength", Range(0, 1)) = 1

        [Header(Advanced Lighting)]
        [Toggle(_SOFT_LIGHTING_MODE)] _SoftLightingMode ("Soft Lighting Mode Global", Float) = 0
        _SoftLightingIntensity ("Soft Lighting Intensity", Range(0, 1)) = 0.5
        _LightIntensity ("Light Intensity Global", Range(0, 2)) = 1
        _IndirectLightIntensity ("Indirect Light Intensity", Range(0, 2)) = 1
        _LightColorInfluence ("Light Color Influence", Range(0, 1)) = 1
        _ShadowReceive ("Shadow Receive", Range(0, 1)) = 1
        _ShadowMaxDarkness ("Shadow Max Darkness", Range(0, 1)) = 0
        _LightMinInfluence ("Light Min Influence", Range(0, 1)) = 0
        _LightMaxInfluence ("Light Max Influence", Range(1, 5)) = 2
        _LightBlend ("Light Blend Softness", Range(0, 1)) = 0
        _HighlightSoftness ("Highlight Softness", Range(0, 1)) = 0
        _BacklightIntensity ("Backlight Intensity", Range(0, 2)) = 0
        _BacklightColor ("Backlight Color", Color) = (1, 1, 1, 1)
        _AdditionalLightIntensity ("Additional Light Intensity", Range(0, 1)) = 0.5

        [Header(VRC Light Volumes)]
        [Toggle(_USE_LIGHT_VOLUME)] _UseLightVolume ("Use Light Volume", Float) = 1
        _LightVolumeIntensity ("Light Volume Intensity", Range(0, 1)) = 1
        [Enum(Add,0,Multiply,1,Replace,2)] _LightVolumeBlendMode ("Light Volume Blend Mode", Float) = 0
        [Toggle(_LIGHT_VOLUME_SPECULAR)] _LightVolumeSpecular ("Light Volume Specular", Float) = 0

        [Header(Specular)]
        [Toggle(_SPECULAR)] _Specular ("Enable Specular", Float) = 0
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularSize ("Specular Size", Range(0, 1)) = 0.1
        _SpecularSoftness ("Specular Softness", Range(0.001, 1)) = 0.05
        [Toggle(_SPECULAR_MASK)] _UseSpecularMask ("Use Specular Mask", Float) = 0
        _SpecularMask ("Specular Mask", 2D) = "white" {}

        [Header(Rim Light)]
        [Toggle(_RIM_LIGHT)] _RimLight ("Enable Rim Light", Float) = 0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1, 10)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1
        _RimSpread ("Rim Spread Glow", Range(0, 1)) = 0
        [Toggle(_RIM_MASK)] _UseRimMask ("Use Rim Mask", Float) = 0
        _RimMask ("Rim Mask", 2D) = "white" {}
        [Toggle(_RIM_LIGHT_2)] _RimLight2 ("Enable Rim Light 2", Float) = 0
        _RimColor2 ("Rim Color 2", Color) = (0.5,0.8,1,1)
        _RimPower2 ("Rim Power 2", Range(0.1, 10)) = 5
        _RimIntensity2 ("Rim Intensity 2", Range(0, 5)) = 0.5
        _RimSpread2 ("Rim Spread Glow", Range(0, 1)) = 0
        [Toggle(_RIM_MASK_2)] _UseRimMask2 ("Use Rim Mask 2", Float) = 0
        _RimMask2 ("Rim Mask 2", 2D) = "white" {}
        [Space(10)]
        [Toggle(_RIM_DIRECTION_CONTROL)] _RimDirectionControl ("Rim Direction Control", Float) = 0
        _RimLightDirection ("Rim Light Direction", Vector) = (0,1,0,0)
        _RimDirectionRange ("Direction Range", Range(0, 1)) = 0.5

        [Header(Subsurface Scattering)]
        [Toggle(_SSS)] _SSS ("Enable SSS", Float) = 0
        _SSSColor ("SSS Color", Color) = (1, 0.5, 0.5, 1)
        _SSSIntensity ("SSS Intensity", Range(0, 5)) = 1.5
        _SSSPower ("SSS Power", Range(0.1, 10)) = 3
        _SSSDistortion ("SSS Distortion", Range(0, 1)) = 0.5
        [Toggle(_THICKNESS_MAP)] _UseThicknessMap ("Use Thickness Map", Float) = 0
        _ThicknessMap ("Thickness Map", 2D) = "white" {}
        _ThicknessScale ("Thickness Scale", Range(0, 1)) = 0.2
        [Toggle(_SSS_MASK)] _UseSSS_Mask ("Use SSS Mask", Float) = 0
        _SSSMask ("SSS Mask", 2D) = "white" {}

        [Header(MatCap)]
        [Toggle(_MATCAP)] _MatCap ("Enable MatCap", Float) = 0
        _MatCapTex ("MatCap Texture", 2D) = "black" {}
        _MatCapIntensity ("MatCap Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Replace,2)] _MatCapBlendMode ("MatCap Blend Mode", Float) = 0
        [Toggle(_MATCAP_MASK)] _UseMatCapMask ("Use MatCap Mask", Float) = 0
        _MatCapMask ("MatCap Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_MATCAP_2)] _MatCap2 ("Enable MatCap 2", Float) = 0
        _MatCapTex2 ("MatCap Texture 2", 2D) = "black" {}
        _MatCapIntensity2 ("MatCap 2 Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Replace,2)] _MatCapBlendMode2 ("MatCap 2 Blend Mode", Float) = 0
        [Toggle(_MATCAP_MASK_2)] _UseMatCapMask2 ("Use MatCap 2 Mask", Float) = 0
        _MatCapMask2 ("MatCap 2 Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_MATCAP_3)] _MatCap3 ("Enable MatCap 3", Float) = 0
        _MatCapTex3 ("MatCap Texture 3", 2D) = "black" {}
        _MatCapIntensity3 ("MatCap 3 Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Replace,2)] _MatCapBlendMode3 ("MatCap 3 Blend Mode", Float) = 0
        [Toggle(_MATCAP_MASK_3)] _UseMatCapMask3 ("Use MatCap 3 Mask", Float) = 0
        _MatCapMask3 ("MatCap 3 Mask", 2D) = "white" {}

        [Header(Glitter)]
        [Toggle(_GLITTER)] _Glitter ("Enable Glitter", Float) = 0
        _GlitterColor ("Glitter Color", Color) = (1,1,1,1)
        _GlitterSize ("Glitter Size", Range(0, 1)) = 0.1
        _GlitterDensity ("Glitter Density", Range(0, 1)) = 0.5
        _GlitterSpeed ("Glitter Speed", Float) = 1
        _GlitterIntensity ("Glitter Intensity", Range(0, 2)) = 1
        [Toggle(_GLITTER_MASK)] _UseGlitterMask ("Use Glitter Mask", Float) = 0
        _GlitterMask ("Glitter Mask", 2D) = "white" {}

        [Header(Outline)]
        [Toggle(_OUTLINE)] _Outline ("Enable Outline", Float) = 0
        [Enum(Inverted Hull,0,Back Face,1)] _OutlineMode ("Outline Mode", Float) = 0
        _OutlineWidth ("Outline Width", Range(0, 1)) = 0.1
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        [Toggle(_OUTLINE_MASK)] _UseOutlineMask ("Use Outline Mask", Float) = 0
        _OutlineMask ("Outline Mask", 2D) = "white" {}
        [Toggle(_OUTLINE_WIDTH_MAP)] _UseOutlineWidthMap ("Use Outline Width Map", Float) = 0
        _OutlineWidthMap ("Outline Width Map", 2D) = "white" {}
        [Space(10)]
        [Toggle(_OUTLINE_MULTI_COLOR)] _OutlineMultiColor ("Multi-Color Outline", Float) = 0
        _OutlineColor2 ("Outline Color 2", Color) = (0.5,0,0,1)
        _OutlineColorMix ("Color Mix", Range(0, 1)) = 0.5

        [Header(Emission)]
        [Toggle(_EMISSION)] _Emission ("Enable Emission", Float) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        [Toggle(_EMISSION_SCROLL)] _EmissionScroll ("Emission Scroll", Float) = 0
        _EmissionScrollSpeed ("Emission Scroll Speed", Float) = 1
        [Toggle(_EMISSION_PULSE)] _EmissionPulse ("Emission Pulse", Float) = 0
        _EmissionPulseSpeed ("Emission Pulse Speed", Float) = 1
        _EmissionPulseAmplitude ("Emission Pulse Amplitude", Range(0, 1)) = 0.5
        [Toggle(_EMISSION_MASK)] _UseEmissionMask ("Use Emission Mask", Float) = 0
        _EmissionMask ("Emission Mask", 2D) = "white" {}
        _EmissionGlow ("Emission Glow Bloom", Range(0, 1)) = 0

        [Header(Virtual Expression)]
        [Toggle(_DISSOLVE)] _Dissolve ("Enable Dissolve", Float) = 0
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveTex ("Dissolve Texture Noise", 2D) = "white" {}
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0, 0.5)) = 0.1
        [HDR] _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1, 0.5, 0, 1)
        _DissolveEdgeIntensity ("Dissolve Edge Intensity", Range(0, 10)) = 2
        [Toggle(_DISSOLVE_MASK)] _UseDissolveMask ("Use Dissolve Mask", Float) = 0
        _DissolveMask ("Dissolve Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_ALPHA_MASK)] _UseAlphaMask ("Use Alpha Mask", Float) = 0
        _AlphaMask ("Alpha Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_HUE_SHIFT)] _HueShiftEnable ("Enable Hue Shift", Float) = 0
        _HueShift ("Hue Shift", Range(0, 1)) = 0

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1

        [Header(Cubemap Reflection)]
        [Toggle(_REFLECTION)] _Reflection ("Enable Reflection", Float) = 0
        _ReflectionCube ("Reflection Cubemap", CUBE) = "black" {}
        _ReflectionColor ("Reflection Color", Color) = (1, 1, 1, 1)
        _ReflectionIntensity ("Reflection Intensity", Range(0, 2)) = 1
        _Smoothness ("Smoothness Glossiness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        _FresnelPower ("Fresnel Power", Range(0, 10)) = 5
        _FresnelSoftness ("Fresnel Softness", Range(0, 1)) = 0
        _ReflectionBlendMode ("Reflection Blend Mode", Range(0, 1)) = 0
        [Toggle(_REFLECTION_MASK)] _UseReflectionMask ("Use Reflection Mask", Float) = 0
        _ReflectionMask ("Reflection Mask", 2D) = "white" {}

        [Header(Iridescence)]
        [Toggle(_IRIDESCENCE)] _Iridescence ("Enable Iridescence", Float) = 0
        _IridescenceColor ("Iridescence Color", Color) = (1, 1, 1, 1)
        _IridescenceIntensity ("Intensity", Range(0, 2)) = 0.5
        _IridescenceHueShift ("Hue Shift", Range(0, 1)) = 0.5
        _IridescenceSize ("Size Frequency", Range(0, 10)) = 1
        [Toggle(_IRIDESCENCE_MASK)] _UseIridescenceMask ("Use Iridescence Mask", Float) = 0
        _IridescenceMask ("Iridescence Mask", 2D) = "white" {}

        [Header(Environmental Rim)]
        [Toggle(_ENV_RIM)] _EnvRim ("Enable Environmental Rim", Float) = 0
        _EnvRimCube ("Environment Cubemap", CUBE) = "black" {}
        _EnvRimColor ("Env Rim Color", Color) = (1, 1, 1, 1)
        _EnvRimPower ("Env Rim Power", Range(0.1, 10)) = 3
        _EnvRimIntensity ("Env Rim Intensity", Range(0, 5)) = 1
        [Toggle(_ENV_RIM_MASK)] _UseEnvRimMask ("Use Env Rim Mask", Float) = 0
        _EnvRimMask ("Env Rim Mask", 2D) = "white" {}

        [Header(Parallax Mapping WARNING Performance Heavy in VR)]
        [Toggle(_PARALLAX)] _Parallax ("Enable Parallax Max 64 Samples", Float) = 0
        _ParallaxMap ("Height Map", 2D) = "grey" {}
        _ParallaxScale ("Parallax Scale Distortion Strength", Range(0, 0.1)) = 0.02
        _ParallaxMinSamples ("Min Samples Flat View", Range(4, 16)) = 4
        _ParallaxMaxSamples ("Max Samples Steep View", Range(16, 64)) = 32

        [Header(Refraction)]
        [Toggle(_REFRACTION)] _Refraction ("Enable Refraction", Float) = 0
        _RefractionIndex ("Refraction Index IOR", Range(1, 3)) = 1.5
        _RefractionIntensity ("Refraction Intensity", Range(0, 1)) = 1
        _RefractionBlur ("Refraction Blur", Range(0, 1)) = 0
        [Toggle(_REFRACTION_MASK)] _UseRefractionMask ("Use Refraction Mask", Float) = 0
        _RefractionMask ("Refraction Mask", 2D) = "white" {}

        [Header(AudioLink VRChat Club Events)]
        [Toggle(_AUDIOLINK)] _AudioLink ("Enable AudioLink", Float) = 0
        [Toggle(_AUDIOLINK_EMISSION)] _AudioLinkEmission ("AudioLink Emission", Float) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _AudioLinkEmissionBand ("Emission Band", Float) = 0
        _AudioLinkEmissionIntensity ("Emission Intensity", Range(0, 5)) = 1
        [Toggle(_AUDIOLINK_RIM)] _AudioLinkRim ("AudioLink Rim Light", Float) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _AudioLinkRimBand ("Rim Band", Float) = 0
        _AudioLinkRimIntensity ("Rim Intensity", Range(0, 5)) = 1
        [Toggle(_AUDIOLINK_HUE_SHIFT)] _AudioLinkHueShift ("AudioLink Hue Shift", Float) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _AudioLinkHueBand ("Hue Band", Float) = 0
        _AudioLinkHueShiftIntensity ("Hue Shift Intensity", Range(0, 1)) = 0.5
        [Toggle(_AUDIOLINK_DISSOLVE)] _AudioLinkDissolve ("AudioLink Dissolve", Float) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _AudioLinkDissolveBand ("Dissolve Band", Float) = 0
        _AudioLinkDissolveIntensity ("Dissolve Intensity", Range(0, 1)) = 0.5
        [Toggle(_AUDIOLINK_OUTLINE)] _AudioLinkOutline ("AudioLink Outline", Float) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3)] _AudioLinkOutlineBand ("Outline Band", Float) = 0
        _AudioLinkOutlineIntensity ("Outline Intensity", Range(0, 1)) = 0.5
        [Toggle(_AUDIOLINK_CHRONOTENSITY)] _AudioLinkChronotensity ("Use Chronotensity", Float) = 0

        [Header(Distance Fade VRChat Optimization)]
        [Toggle(_DISTANCE_FADE)] _DistanceFade ("Enable Distance Fade", Float) = 0
        _DistanceFadeStart ("Fade Start Distance", Float) = 10
        _DistanceFadeEnd ("Fade End Distance", Float) = 20
        [Enum(Alpha,0,Simplify,1)] _DistanceFadeMode ("Fade Mode", Float) = 0

        [Header(Vertex Offset Animation Wind Breathing)]
        [Toggle(_VERTEX_ANIMATION)] _VertexAnimation ("Enable Vertex Animation", Float) = 0
        _VertexAnimSpeed ("Animation Speed", Float) = 1
        _VertexAnimStrength ("Animation Strength", Range(0, 1)) = 0.1
        _VertexAnimFrequency ("Animation Frequency", Range(0, 10)) = 1
        [Enum(Wave,0,Breath,1,Wind,2,Pulse,3)] _VertexAnimType ("Animation Type", Float) = 2
        [Toggle(_VERTEX_ANIM_MASK)] _UseVertexAnimMask ("Use Vertex Anim Mask", Float) = 0
        _VertexAnimMask ("Vertex Anim Mask", 2D) = "white" {}

        [Header(Vertex Animation Texture Houdini VAT)]
        [Toggle(_VAT)] _VAT ("Enable VAT Animation", Float) = 0
        _VATPositionMap ("VAT Position Map", 2D) = "black" {}
        [Toggle(_VAT_NORMAL)] _VATNormal ("Use VAT Normal Map", Float) = 0
        _VATNormalMap ("VAT Normal Map", 2D) = "black" {}
        _VATNumOfFrames ("Number of Frames", Float) = 24
        _VATSpeed ("Animation Speed", Float) = 1
        _VATIntensity ("Animation Intensity", Range(0, 2)) = 1
        _VATPositionMin ("Position Min Value", Float) = -1
        _VATPositionMax ("Position Max Value", Float) = 1
        _VATNormalMin ("Normal Min Value", Float) = -1
        _VATNormalMax ("Normal Max Value", Float) = 1
        [Space(10)]
        [Enum(Absolute,0,Offset,1)] _VATPackingMode ("VAT Packing Mode", Float) = 1

        [Header(Hologram Glitch Effect)]
        [Toggle(_HOLOGRAM)] _Hologram ("Enable Hologram", Float) = 0
        _HologramScanlineSpeed ("Scanline Speed", Float) = 1
        _HologramScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        _HologramFlickerSpeed ("Flicker Speed", Float) = 5
        _HologramFlickerAmount ("Flicker Amount", Range(0, 1)) = 0.3
        [Toggle(_GLITCH)] _Glitch ("Enable Glitch", Float) = 0
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.5
        _GlitchSpeed ("Glitch Speed", Float) = 1
        _GlitchBlockSize ("Glitch Block Size", Range(0, 1)) = 0.1

        [Header(Decal System Stickers)]
        [Toggle(_DECAL)] _Decal ("Enable Decal", Float) = 0
        _DecalTex ("Decal Texture", 2D) = "white" {}
        _DecalColor ("Decal Color", Color) = (1,1,1,1)
        _DecalPosition ("Decal Position XY", Vector) = (0,0,0,0)
        _DecalRotation ("Decal Rotation", Range(0, 360)) = 0
        _DecalScale ("Decal Scale", Float) = 1
        [Enum(Add,0,Multiply,1,Overlay,2,Replace,3)] _DecalBlendMode ("Decal Blend Mode", Float) = 0

        [Header(Backface Texture Cloth Interior)]
        [Toggle(_BACKFACE_TEXTURE)] _BackfaceTexture ("Enable Backface Texture", Float) = 0
        _BackfaceTex ("Backface Texture", 2D) = "white" {}
        _BackfaceColor ("Backface Color", Color) = (1,1,1,1)

        [Header(Video Render Texture Screen Display)]
        [Toggle(_VIDEO_TEXTURE)] _VideoTexture ("Enable Video Texture", Float) = 0
        _VideoTex ("Video Render Texture", 2D) = "black" {}
        _VideoEmission ("Video Emission", Range(0, 5)) = 1

        [Header(LTCGI Realtime GI Support)]
        [Toggle(_LTCGI)] _LTCGI ("Enable LTCGI", Float) = 0
        _LTCGIIntensity ("LTCGI Intensity", Range(0, 2)) = 1
        _LTCGISpecular ("LTCGI Specular", Range(0, 1)) = 0.5

        [Header(Dithering Alpha Transparent Dithering)]
        [Toggle(_DITHERING_ALPHA)] _DitheringAlpha ("Enable Dithering Alpha", Float) = 0
        _DitheringAlphaScale ("Dithering Alpha Scale", Range(1, 100)) = 10

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
        [Enum(Off,0,On,1)] _ZWrite ("Z Write", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        // GrabPass for Refraction
        // Captures the screen content behind the object
        // Note: Refraction on opaque objects has limited use
        GrabPass
        {
            "_GrabTexture"
        }

        // Outline Pass
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "ForwardBase" }
            Cull Front
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _OUTLINE
            #pragma shader_feature _OUTLINE_MASK
            #pragma shader_feature _OUTLINE_WIDTH_MAP
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            float _OutlineWidth;
            float4 _OutlineColor;
            float4 _OutlineColor2;
            float _OutlineColorMix;
            float _Outline;
            float _OutlineMode;
            sampler2D _OutlineMask;
            sampler2D _OutlineWidthMap;

            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv;

                #ifdef _OUTLINE
                    // Calculate distance compensation for consistent outline width (NiloToon-style)
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    float distanceToCamera = distance(worldPos, _WorldSpaceCameraPos);
                    float distanceFactor = distanceToCamera * 0.1; // Scale factor for distance compensation

                    // Get outline width from map if enabled
                    float widthMultiplier = 1.0;
                    #ifdef _OUTLINE_WIDTH_MAP
                        widthMultiplier = tex2Dlod(_OutlineWidthMap, float4(v.uv, 0, 0)).r;
                    #endif

                    if (_OutlineMode < 0.5)
                    {
                        // Mode 0: Inverted Hull - Extrusion along normals in view space
                        // Improved for better consistency at different angles
                        float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                        float2 offset = TransformViewToProjection(norm.xy);

                        o.pos = UnityObjectToClipPos(v.vertex);

                        // Apply distance compensation for consistent outline width
                        // Scale down by 0.01 to maintain original scale with new range (0-1)
                        float outlineWidth = _OutlineWidth * 0.01 * (1.0 + distanceFactor) * widthMultiplier;
                        o.pos.xy += offset * o.pos.z * outlineWidth;
                    }
                    else
                    {
                        // Mode 1: Back Face - Scale up vertices along normals in object space
                        // Improved with distance compensation
                        // Scale down by 0.1 to maintain original scale with new range (0-1)
                        float outlineWidth = _OutlineWidth * 0.1 * (1.0 + distanceFactor * 0.5) * widthMultiplier;
                        float3 scaledPos = v.vertex.xyz + normalize(v.normal) * outlineWidth;
                        o.pos = UnityObjectToClipPos(float4(scaledPos, 1.0));
                    }
                #else
                    o.pos = float4(0, 0, 0, 0);
                #endif

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                #ifdef _OUTLINE
                    fixed4 col = _OutlineColor;

                    // Apply multi-color outline
                    #ifdef _OUTLINE_MULTI_COLOR
                        // Mix between two colors based on UV or other parameter
                        float mixFactor = frac(i.uv.y * 5.0 + _Time.y * 0.5); // Animated gradient
                        col.rgb = lerp(_OutlineColor.rgb, _OutlineColor2.rgb, mixFactor * _OutlineColorMix);
                    #endif

                    // Apply outline mask
                    #ifdef _OUTLINE_MASK
                        float outlineMask = tex2D(_OutlineMask, i.uv).r;
                        col.a *= outlineMask;
                        // Discard pixels where outline is fully masked out
                        clip(col.a - 0.01);
                    #endif

                    UNITY_APPLY_FOG(i.fogCoord, col);
                    return col;
                #else
                    discard;
                    return fixed4(0, 0, 0, 0);
                #endif
            }
            ENDCG
        }

        // Base Forward Pass
        Pass
        {
            Name "FORWARD_BASE"
            Tags { "LightMode" = "ForwardBase" }
            Cull [_Cull]
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature _MAIN_TEX_ANIMATION
            #pragma shader_feature _2ND_TEXTURE
            #pragma shader_feature _2ND_TEX_MASK
            #pragma shader_feature _3RD_TEXTURE
            #pragma shader_feature _3RD_TEX_MASK
            #pragma shader_feature _4TH_TEXTURE
            #pragma shader_feature _4TH_TEX_MASK
            #pragma shader_feature _5TH_TEXTURE
            #pragma shader_feature _5TH_TEX_MASK
            #pragma shader_feature _USE_RAMP
            #pragma shader_feature _USE_MULTI_SHADOW
            #pragma shader_feature _SHADOW_RECEIVE_MASK
            #pragma shader_feature _SDF_MAP
            #pragma shader_feature _SHADING_GRADE_MAP
            #pragma shader_feature _USE_AO
            #pragma shader_feature _USE_DITHERING
            #pragma shader_feature _SOFT_LIGHTING_MODE
            #pragma shader_feature _USE_LIGHT_VOLUME
            #pragma shader_feature _LIGHT_VOLUME_SPECULAR
            #pragma shader_feature _SPECULAR
            #pragma shader_feature _SPECULAR_MASK
            #pragma shader_feature _RIM_LIGHT
            #pragma shader_feature _RIM_MASK
            #pragma shader_feature _RIM_LIGHT_2
            #pragma shader_feature _RIM_MASK_2
            #pragma shader_feature _SSS
            #pragma shader_feature _SSS_MASK
            #pragma shader_feature _THICKNESS_MAP
            #pragma shader_feature _MATCAP
            #pragma shader_feature _MATCAP_MASK
            #pragma shader_feature _GLITTER
            #pragma shader_feature _GLITTER_MASK
            #pragma shader_feature _EMISSION
            #pragma shader_feature _EMISSION_MASK
            #pragma shader_feature _EMISSION_SCROLL
            #pragma shader_feature _EMISSION_PULSE
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _DISSOLVE
            #pragma shader_feature _DISSOLVE_MASK
            #pragma shader_feature _ALPHA_MASK
            #pragma shader_feature _HUE_SHIFT
            #pragma shader_feature _REFLECTION
            #pragma shader_feature _REFLECTION_MASK
            #pragma shader_feature _IRIDESCENCE
            #pragma shader_feature _IRIDESCENCE_MASK
            #pragma shader_feature _ENV_RIM
            #pragma shader_feature _ENV_RIM_MASK
            #pragma shader_feature _PARALLAX
            #pragma shader_feature _REFRACTION
            #pragma shader_feature _REFRACTION_MASK
            #pragma shader_feature _MATCAP_2
            #pragma shader_feature _MATCAP_MASK_2
            #pragma shader_feature _MATCAP_3
            #pragma shader_feature _MATCAP_MASK_3
            #pragma shader_feature _RIM_DIRECTION_CONTROL
            #pragma shader_feature _SHADOW_COLOR_TEX
            #pragma shader_feature _OUTLINE_MULTI_COLOR
            #pragma shader_feature _AUDIOLINK
            #pragma shader_feature _AUDIOLINK_EMISSION
            #pragma shader_feature _AUDIOLINK_RIM
            #pragma shader_feature _AUDIOLINK_HUE_SHIFT
            #pragma shader_feature _AUDIOLINK_DISSOLVE
            #pragma shader_feature _AUDIOLINK_OUTLINE
            #pragma shader_feature _AUDIOLINK_CHRONOTENSITY
            #pragma shader_feature _DISTANCE_FADE
            #pragma shader_feature _VERTEX_ANIMATION
            #pragma shader_feature _VERTEX_ANIM_MASK
            #pragma shader_feature _HOLOGRAM
            #pragma shader_feature _GLITCH
            #pragma shader_feature _DECAL
            #pragma shader_feature _BACKFACE_TEXTURE
            #pragma shader_feature _VIDEO_TEXTURE
            #pragma shader_feature _LTCGI
            #pragma shader_feature _DITHERING_ALPHA
            #pragma shader_feature _VAT
            #pragma shader_feature _VAT_NORMAL

            #include "Include/Core/NataneToonCore.hlsl"

            ENDCG
        }

        // Additional Forward Pass for multiple lights
        Pass
        {
            Name "FORWARD_ADD"
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            ZWrite Off
            Cull [_Cull]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature _MAIN_TEX_ANIMATION
            #pragma shader_feature _2ND_TEXTURE
            #pragma shader_feature _2ND_TEX_MASK
            #pragma shader_feature _3RD_TEXTURE
            #pragma shader_feature _3RD_TEX_MASK
            #pragma shader_feature _4TH_TEXTURE
            #pragma shader_feature _4TH_TEX_MASK
            #pragma shader_feature _5TH_TEXTURE
            #pragma shader_feature _5TH_TEX_MASK
            #pragma shader_feature _USE_RAMP
            #pragma shader_feature _USE_MULTI_SHADOW
            #pragma shader_feature _SHADOW_RECEIVE_MASK
            #pragma shader_feature _SDF_MAP
            #pragma shader_feature _SHADING_GRADE_MAP
            #pragma shader_feature _USE_AO
            #pragma shader_feature _USE_DITHERING
            #pragma shader_feature _SOFT_LIGHTING_MODE
            #pragma shader_feature _SPECULAR
            #pragma shader_feature _SPECULAR_MASK
            #pragma shader_feature _SSS
            #pragma shader_feature _SSS_MASK
            #pragma shader_feature _THICKNESS_MAP
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _DISSOLVE
            #pragma shader_feature _DISSOLVE_MASK
            #pragma shader_feature _ALPHA_MASK
            #pragma shader_feature _HUE_SHIFT
            #pragma shader_feature _PARALLAX
            #pragma shader_feature _REFRACTION
            #pragma shader_feature _REFRACTION_MASK
            #pragma shader_feature _MATCAP_2
            #pragma shader_feature _MATCAP_MASK_2
            #pragma shader_feature _MATCAP_3
            #pragma shader_feature _MATCAP_MASK_3
            #pragma shader_feature _RIM_DIRECTION_CONTROL
            #pragma shader_feature _SHADOW_COLOR_TEX
            #pragma shader_feature _OUTLINE_MULTI_COLOR
            #pragma shader_feature _AUDIOLINK
            #pragma shader_feature _AUDIOLINK_EMISSION
            #pragma shader_feature _AUDIOLINK_RIM
            #pragma shader_feature _AUDIOLINK_HUE_SHIFT
            #pragma shader_feature _AUDIOLINK_DISSOLVE
            #pragma shader_feature _AUDIOLINK_OUTLINE
            #pragma shader_feature _AUDIOLINK_CHRONOTENSITY
            #pragma shader_feature _DISTANCE_FADE
            #pragma shader_feature _VERTEX_ANIMATION
            #pragma shader_feature _VERTEX_ANIM_MASK
            #pragma shader_feature _HOLOGRAM
            #pragma shader_feature _GLITCH
            #pragma shader_feature _DECAL
            #pragma shader_feature _BACKFACE_TEXTURE
            #pragma shader_feature _VIDEO_TEXTURE
            #pragma shader_feature _LTCGI
            #pragma shader_feature _DITHERING_ALPHA
            #pragma shader_feature _VAT
            #pragma shader_feature _VAT_NORMAL

            #include "Include/Core/NataneToonCore.hlsl"

            ENDCG
        }

        // Shadow Caster Pass
        Pass
        {
            Name "SHADOW_CASTER"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    CustomEditor "NataneToonShaderGUI"
    FallBack "Diffuse"
}
