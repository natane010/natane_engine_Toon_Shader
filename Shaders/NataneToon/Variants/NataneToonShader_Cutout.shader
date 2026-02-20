Shader "Natane/Toon Shader (Cutout)"
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
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Color Preservation)]
        _AlbedoPreservation ("Texture Color Preservation", Range(0, 1)) = 0
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Brightness ("Overall Brightness", Range(0.5, 1.5)) = 1

        [Header(Final Color Blending)]
        _FinalHighlightBlend ("Highlight Compression Prevent White Blowout", Range(0, 1)) = 0
        _HighlightThreshold ("Highlight Threshold Start Point", Range(0, 1)) = 0.75
        _FinalShadowBlend ("Shadow Lift Prevent Black Crush", Range(0, 1)) = 0
        _ShadowThreshold ("Shadow Threshold Start Point", Range(0, 1)) = 0.25

        [Header(Surface Finish)]
        _Glossiness ("Glossiness Overall Gloss", Range(0, 1)) = 1
        _MatteEffect ("Matte Effect Reduce Gloss", Range(0, 1)) = 0

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
        _2ndTexScrollSpeed ("2nd Tex Scroll Speed XY", Vector) = (0,0,0,0)
        _2ndTexRotateSpeed ("2nd Tex Rotate Speed", Float) = 0
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
        _3rdTexScrollSpeed ("3rd Tex Scroll Speed XY", Vector) = (0,0,0,0)
        _3rdTexRotateSpeed ("3rd Tex Rotate Speed", Float) = 0
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
        _4thTexScrollSpeed ("4th Tex Scroll Speed XY", Vector) = (0,0,0,0)
        _4thTexRotateSpeed ("4th Tex Rotate Speed", Float) = 0
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
        _5thTexScrollSpeed ("5th Tex Scroll Speed XY", Vector) = (0,0,0,0)
        _5thTexRotateSpeed ("5th Tex Rotate Speed", Float) = 0

        [Header(Shading)]
        [Toggle(_USE_RAMP)] _UseRamp ("Use Ramp Texture", Float) = 0
        _RampTex ("Ramp Texture", 2D) = "white" {}
        [Enum(Toon,0,Gradient,1)] _ShadingMode ("Shading Mode", Float) = 0
        _ShadingGradientWidth ("Gradient Width", Range(0.001, 1)) = 0.2
        _ShadowColor ("Shadow Color 1st", Color) = (0.5, 0.5, 0.5, 1)
        _ShadowSteps ("Shadow Steps", Range(1, 10)) = 2
        _ShadowSharpness ("Shadow Sharpness", Range(0.001, 1)) = 0.1
        _ShadowOffset ("Shadow Offset", Range(-1, 1)) = 0
        _LitSoftness ("Lit Area Softness Global Smoothstep", Range(0, 1)) = 0
        _ShadowBlend ("Shadow Blend Softness", Range(0, 1)) = 0
        [Toggle(_USE_MULTI_SHADOW)] _UseMultiShadow ("Use Multi-tone Shadow", Float) = 0
        _Shadow2ndColor ("Shadow Color 2nd", Color) = (0.35, 0.35, 0.35, 1)
        _Shadow2ndBorder ("2nd Shadow Border", Range(0, 1)) = 0.3
        _Shadow3rdColor ("Shadow Color 3rd", Color) = (0.2, 0.2, 0.2, 1)
        _Shadow3rdBorder ("3rd Shadow Border", Range(0, 1)) = 0.15
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
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _AOBlendMode ("AO Blend Mode", Float) = 0
        _AOBlend ("AO Blend", Range(0, 1)) = 1
        _AOBlur ("AO Blur", Range(0, 1)) = 0
        [Toggle(_USE_DITHERING)] _UseDithering ("Use Dithering Shadow Edge Only", Float) = 0
        _DitheringScale ("Dithering Scale Pattern Size", Range(1, 100)) = 10
        _DitheringStrength ("Dithering Strength Boundary Softness", Range(0, 1)) = 0.5
        _DitheringBlend ("Dithering Blend", Range(0, 1)) = 1
        _DitheringBlur ("Dithering Blur", Range(0, 1)) = 0
        [Space(10)]
        [Toggle(_SHADOW_COLOR_TEX)] _UseShadowColorTex ("Use Shadow Color Texture", Float) = 0
        _ShadowColorTex ("Shadow Color Texture", 2D) = "white" {}
        _ShadowColorTexStrength ("Shadow Color Tex Strength", Range(0, 1)) = 1

        [Header(Advanced Lighting)]
        [Toggle(_SOFT_LIGHTING_MODE)] _SoftLightingMode ("Soft Lighting Mode Global", Float) = 0
        _SoftLightingIntensity ("Soft Lighting Intensity", Range(0, 1)) = 0.5
        _LightIntensity ("Light Intensity Global", Range(0, 2)) = 1
        _IndirectLightIntensity ("Indirect Light Intensity", Range(0, 2)) = 1
        [Header(Environment Reflection Control)]
        _GIIntensity ("GI Intensity (環境反射強度)", Range(0, 1)) = 0.5
        _LightColorInfluence ("Light Color Influence", Range(0, 1)) = 1
        _ShadowReceive ("Shadow Receive", Range(0, 1)) = 1
        _ShadowSmoothing ("Shadow Map Smoothing", Range(0, 1)) = 0
        _ShadowMaxDarkness ("Shadow Max Darkness", Range(0, 1)) = 0
        _LightMinInfluence ("Light Min Influence", Range(0, 1)) = 0
        _LightMaxInfluence ("Light Max Influence", Range(1, 5)) = 2
        _LightBlend ("Light Blend Softness", Range(0, 1)) = 0
        _HighlightSoftness ("Highlight Softness", Range(0, 1)) = 0
        _BacklightIntensity ("Backlight Intensity", Range(0, 2)) = 0
        _BacklightColor ("Backlight Color", Color) = (1, 1, 1, 1)
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _BacklightBlendMode ("Backlight Blend Mode", Float) = 0
        _BacklightBlend ("Backlight Blend", Range(0, 1)) = 1
        _BacklightBlur ("Backlight Blur", Range(0, 1)) = 0
        _AdditionalLightIntensity ("Additional Light Intensity", Range(0, 1)) = 0.5
        _IndirectLightMinColor ("Indirect Light Min Color (間接光最低色)", Color) = (0.1, 0.1, 0.1, 1)
        _ShadowEnvStrength ("Shadow Env Strength (影への環境色反映)", Range(0, 1)) = 0
        [Toggle(_PIXEL_VERTEX_LIGHTS)] _UsePixelVertexLights("Pixel Vertex Lights", Float) = 0

        [Header(VRC Light Volumes)]
        [Toggle(_USE_LIGHT_VOLUME)] _UseLightVolume ("Use Light Volume", Float) = 1
        _LightVolumeIntensity ("Light Volume Intensity", Range(0, 1)) = 1
        [Enum(Add,0,Multiply,1,Replace,2,Natural,3)] _LightVolumeBlendMode ("Light Volume Blend Mode", Float) = 3
        [Toggle(_LIGHT_VOLUME_SPECULAR)] _LightVolumeSpecular ("Light Volume Specular", Float) = 0
        _LightVolumeBlend ("Light Volume Blend", Range(0, 1)) = 1

        [Header(Specular)]
        [Toggle(_SPECULAR)] _Specular ("Enable Specular", Float) = 0
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularSize ("Specular Size", Range(0, 1)) = 0.1
        _SpecularSoftness ("Specular Softness", Range(0.001, 1)) = 0.05
        [Toggle(_SPECULAR_MASK)] _UseSpecularMask ("Use Specular Mask", Float) = 0
        _SpecularMask ("Specular Mask", 2D) = "white" {}
        _SpecularMaskScrollSpeed ("Specular Mask Scroll Speed XY", Vector) = (0,0,0,0)
        _SpecularMaskRotateSpeed ("Specular Mask Rotate Speed", Float) = 0
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _SpecularBlendMode ("Specular Blend Mode", Float) = 0
        _SpecularBlend ("Specular Blend", Range(0, 1)) = 1
        _SpecularBlur ("Specular Blur", Range(0, 1)) = 0

        [Header(Rim Light)]
        [Toggle(_RIM_LIGHT)] _RimLight ("Enable Rim Light", Float) = 0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1, 10)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1
        _RimSpread ("Rim Spread (Glow)", Range(0, 1)) = 0
        [Toggle(_RIM_MASK)] _UseRimMask ("Use Rim Mask", Float) = 0
        _RimMask ("Rim Mask", 2D) = "white" {}
        _RimMaskScrollSpeed ("Rim Mask Scroll Speed XY", Vector) = (0,0,0,0)
        _RimMaskRotateSpeed ("Rim Mask Rotate Speed", Float) = 0
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _RimBlendMode ("Rim Blend Mode", Float) = 0
        _RimBlend ("Rim Blend", Range(0, 1)) = 1
        _RimBlur ("Rim Blur", Range(0, 1)) = 0
        [Toggle(_RIM_LIGHT_2)] _RimLight2 ("Enable Rim Light 2", Float) = 0
        _RimColor2 ("Rim Color 2", Color) = (0.5,0.8,1,1)
        _RimPower2 ("Rim Power 2", Range(0.1, 10)) = 5
        _RimIntensity2 ("Rim Intensity 2", Range(0, 5)) = 0.5
        _RimSpread2 ("Rim Spread 2 (Glow)", Range(0, 1)) = 0
        [Toggle(_RIM_MASK_2)] _UseRimMask2 ("Use Rim Mask 2", Float) = 0
        _RimMask2 ("Rim Mask 2", 2D) = "white" {}
        _RimMask2ScrollSpeed ("Rim Mask 2 Scroll Speed XY", Vector) = (0,0,0,0)
        _RimMask2RotateSpeed ("Rim Mask 2 Rotate Speed", Float) = 0
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _RimBlendMode2 ("Rim 2 Blend Mode", Float) = 0
        _RimBlend2 ("Rim 2 Blend", Range(0, 1)) = 1
        _Rim2Blur ("Rim 2 Blur", Range(0, 1)) = 0
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
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _SSSBlendMode ("SSS Blend Mode", Float) = 0
        _SSSBlend ("SSS Blend", Range(0, 1)) = 1
        _SSSBlur ("SSS Blur", Range(0, 1)) = 0

        [Header(MatCap)]
        [Toggle(_MATCAP)] _MatCap ("Enable MatCap", Float) = 0
        _MatCapTex ("MatCap Texture", 2D) = "black" {}
        _MatCapIntensity ("MatCap Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Replace,2)] _MatCapBlendMode ("MatCap Blend Mode", Float) = 0
        [Toggle(_MATCAP_MASK)] _UseMatCapMask ("Use MatCap Mask", Float) = 0
        _MatCapMask ("MatCap Mask", 2D) = "white" {}
        _MatCapBlend ("MatCap Blend", Range(0, 1)) = 1
        _MatCapBlur ("MatCap Blur", Range(0, 1)) = 0
        [Space(10)]
        [Toggle(_MATCAP_2)] _MatCap2 ("Enable MatCap 2", Float) = 0
        _MatCapTex2 ("MatCap Texture 2", 2D) = "black" {}
        _MatCapIntensity2 ("MatCap 2 Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Replace,2)] _MatCapBlendMode2 ("MatCap 2 Blend Mode", Float) = 0
        [Toggle(_MATCAP_MASK_2)] _UseMatCapMask2 ("Use MatCap 2 Mask", Float) = 0
        _MatCapMask2 ("MatCap 2 Mask", 2D) = "white" {}
        _MatCapBlend2 ("MatCap 2 Blend", Range(0, 1)) = 1
        _MatCap2Blur ("MatCap 2 Blur", Range(0, 1)) = 0
        [Space(10)]
        [Toggle(_MATCAP_3)] _MatCap3 ("Enable MatCap 3", Float) = 0
        _MatCapTex3 ("MatCap Texture 3", 2D) = "black" {}
        _MatCapIntensity3 ("MatCap 3 Intensity", Range(0, 2)) = 1
        [Enum(Add,0,Multiply,1,Replace,2)] _MatCapBlendMode3 ("MatCap 3 Blend Mode", Float) = 0
        [Toggle(_MATCAP_MASK_3)] _UseMatCapMask3 ("Use MatCap 3 Mask", Float) = 0
        _MatCapMask3 ("MatCap 3 Mask", 2D) = "white" {}
        _MatCapBlend3 ("MatCap 3 Blend", Range(0, 1)) = 1
        _MatCap3Blur ("MatCap 3 Blur", Range(0, 1)) = 0

        [Header(Glitter)]
        [Toggle(_GLITTER)] _Glitter ("Enable Glitter", Float) = 0
        _GlitterColor ("Glitter Color", Color) = (1,1,1,1)
        _GlitterSize ("Glitter Size", Range(0, 1)) = 0.1
        _GlitterDensity ("Glitter Density", Range(0, 1)) = 0.5
        _GlitterSpeed ("Glitter Speed", Float) = 1
        _GlitterIntensity ("Glitter Intensity", Range(0, 2)) = 1
        [Toggle(_GLITTER_MASK)] _UseGlitterMask ("Use Glitter Mask", Float) = 0
        _GlitterMask ("Glitter Mask", 2D) = "white" {}
        _GlitterMaskScrollSpeed ("Glitter Mask Scroll Speed XY", Vector) = (0,0,0,0)
        _GlitterMaskRotateSpeed ("Glitter Mask Rotate Speed", Float) = 0
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _GlitterBlendMode ("Glitter Blend Mode", Float) = 0
        _GlitterBlend ("Glitter Blend", Range(0, 1)) = 1
        _GlitterBlur ("Glitter Blur", Range(0, 1)) = 0

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
        _EmissionGlow ("Emission Glow (Bloom)", Range(0, 1)) = 0
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _EmissionBlendMode ("Emission Blend Mode", Float) = 0
        _EmissionBlend ("Emission Blend", Range(0, 1)) = 1
        _EmissionBlur ("Emission Blur", Range(0, 1)) = 0
        [Toggle(_EMISSION_SCROLL)] _EmissionScroll ("Emission Scroll", Float) = 0
        _EmissionScrollSpeed ("Emission Scroll Speed", Float) = 1
        _EmissionScrollSpeedY ("Emission Scroll Speed Y", Float) = 0
        _EmissionRotateSpeed ("Emission Rotate Speed", Float) = 0
        [Toggle(_EMISSION_PULSE)] _EmissionPulse ("Emission Pulse", Float) = 0
        _EmissionPulseSpeed ("Emission Pulse Speed", Float) = 1
        _EmissionPulseAmplitude ("Emission Pulse Amplitude", Range(0, 1)) = 0.5
        [Toggle(_EMISSION_MASK)] _UseEmissionMask ("Use Emission Mask", Float) = 0
        _EmissionMask ("Emission Mask", 2D) = "white" {}
        _EmissionMaskScrollSpeed ("Emission Mask Scroll Speed XY", Vector) = (0,0,0,0)
        _EmissionMaskRotateSpeed ("Emission Mask Rotate Speed", Float) = 0

        [Header(Virtual Expression)]
        [Toggle(_DISSOLVE)] _Dissolve ("Enable Dissolve", Float) = 0
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveTex ("Dissolve Texture (Noise)", 2D) = "white" {}
        _DissolveTexScrollSpeed ("Dissolve Tex Scroll Speed XY", Vector) = (0,0,0,0)
        _DissolveTexRotateSpeed ("Dissolve Tex Rotate Speed", Float) = 0
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0, 0.5)) = 0.1
        [HDR] _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1, 0.5, 0, 1)
        _DissolveEdgeIntensity ("Dissolve Edge Intensity", Range(0, 10)) = 2
        [Toggle(_DISSOLVE_MASK)] _UseDissolveMask ("Use Dissolve Mask", Float) = 0
        _DissolveMask ("Dissolve Mask", 2D) = "white" {}
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _DissolveBlendMode ("Dissolve Blend Mode", Float) = 0
        _DissolveBlend ("Dissolve Blend", Range(0, 1)) = 1
        _DissolveBlur ("Dissolve Blur", Range(0, 1)) = 0
        [Space(10)]
        [Toggle(_ALPHA_MASK)] _UseAlphaMask ("Use Alpha Mask", Float) = 0
        _AlphaMask ("Alpha Mask", 2D) = "white" {}
        [Space(10)]
        [Toggle(_HUE_SHIFT)] _HueShiftEnable ("Enable Hue Shift", Float) = 0
        _HueShift ("Hue Shift", Range(0, 1)) = 0
        _HueShiftBlend ("Hue Shift Blend", Range(0, 1)) = 1
        _HueShiftBlur ("Hue Shift Blur", Range(0, 1)) = 0

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1
        _BumpMapScrollSpeed ("Normal Map Scroll Speed XY", Vector) = (0,0,0,0)
        _BumpMapRotateSpeed ("Normal Map Rotate Speed", Float) = 0

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
        _ReflectionBlend ("Reflection Blend", Range(0, 1)) = 1

        [Header(Iridescence)]
        [Toggle(_IRIDESCENCE)] _Iridescence ("Enable Iridescence", Float) = 0
        _IridescenceColor ("Iridescence Color", Color) = (1, 1, 1, 1)
        _IridescenceIntensity ("Intensity", Range(0, 2)) = 0.5
        _IridescenceHueShift ("Hue Shift", Range(0, 1)) = 0.5
        _IridescenceSize ("Size (Frequency)", Range(0, 10)) = 1
        [Toggle(_IRIDESCENCE_MASK)] _UseIridescenceMask ("Use Iridescence Mask", Float) = 0
        _IridescenceMask ("Iridescence Mask", 2D) = "white" {}
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _IridescenceBlendMode ("Iridescence Blend Mode", Float) = 0
        _IridescenceBlend ("Iridescence Blend", Range(0, 1)) = 1
        _IridescenceBlur ("Iridescence Blur", Range(0, 1)) = 0

        [Header(Environmental Rim)]
        [Toggle(_ENV_RIM)] _EnvRim ("Enable Environmental Rim", Float) = 0
        _EnvRimCube ("Environment Cubemap", CUBE) = "black" {}
        _EnvRimColor ("Env Rim Color", Color) = (1, 1, 1, 1)
        _EnvRimPower ("Env Rim Power", Range(0.1, 10)) = 3
        _EnvRimIntensity ("Env Rim Intensity", Range(0, 5)) = 1
        [Toggle(_ENV_RIM_MASK)] _UseEnvRimMask ("Use Env Rim Mask", Float) = 0
        _EnvRimMask ("Env Rim Mask", 2D) = "white" {}
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _EnvRimBlendMode ("Env Rim Blend Mode", Float) = 0
        _EnvRimBlend ("Env Rim Blend", Range(0, 1)) = 1
        _EnvRimBlur ("Env Rim Blur", Range(0, 1)) = 0

        [Header(Parallax Mapping WARNING Performance Heavy in VR)]
        [Toggle(_PARALLAX)] _Parallax ("Enable Parallax Max 64 Samples", Float) = 0
        _ParallaxMap ("Height Map", 2D) = "grey" {}
        _ParallaxScale ("Parallax Scale Distortion Strength", Range(0, 0.1)) = 0.02
        _ParallaxMinSamples ("Min Samples Flat View", Range(4, 16)) = 4
        _ParallaxMaxSamples ("Max Samples Steep View", Range(16, 64)) = 32

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
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _AudioLinkBlendMode ("AudioLink Blend Mode", Float) = 0
        _AudioLinkBlend ("AudioLink Blend", Range(0, 1)) = 1

        [Header(Distance Fade VRChat Optimization)]
        [Toggle(_DISTANCE_FADE)] _DistanceFade ("Enable Distance Fade", Float) = 0
        _DistanceFadeStart ("Fade Start Distance", Float) = 10
        _DistanceFadeEnd ("Fade End Distance", Float) = 20
        [Enum(Alpha,0,Simplify,1)] _DistanceFadeMode ("Fade Mode", Float) = 0
        _DistanceFadeBlend ("Distance Fade Blend", Range(0, 1)) = 1
        _DistFadeBlur ("Distance Fade Blur", Range(0, 1)) = 0

        [Header(Vertex Offset Animation Wind Breathing)]
        [Toggle(_VERTEX_ANIMATION)] _VertexAnimation ("Enable Vertex Animation", Float) = 0
        _VertexAnimSpeed ("Animation Speed", Float) = 1
        _VertexAnimStrength ("Animation Strength", Range(0, 1)) = 0.1
        _VertexAnimFrequency ("Animation Frequency", Range(0, 10)) = 1
        [Enum(Wave,0,Breath,1,Wind,2,Pulse,3)] _VertexAnimType ("Animation Type", Float) = 2
        [Toggle(_VERTEX_ANIM_MASK)] _UseVertexAnimMask ("Use Vertex Anim Mask", Float) = 0
        _VertexAnimMask ("Vertex Anim Mask", 2D) = "white" {}

        [Header(Hologram Glitch Effect)]
        [Toggle(_HOLOGRAM)] _Hologram ("Enable Hologram", Float) = 0
        _HologramColor ("Hologram Color", Color) = (0,1,1,1)
        _HologramScanlineSpeed ("Scanline Speed", Float) = 1
        _HologramScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        _HologramScanlineDensity ("Scanline Density", Float) = 100
        _HologramScanlineWidth ("Scanline Width", Range(0, 1)) = 0.5
        _HologramFlickerSpeed ("Flicker Speed", Float) = 5
        _HologramFlickerAmount ("Flicker Amount", Range(0, 1)) = 0.3
        _HologramEdgeGlowPower ("Edge Glow Power", Range(0.1, 10)) = 3
        _HologramEdgeGlowIntensity ("Edge Glow Intensity", Range(0, 5)) = 1
        _HologramAlpha ("Hologram Alpha", Range(0, 1)) = 0
        _HologramMonochrome ("Monochrome", Range(0, 1)) = 0
        _HologramNoiseIntensity ("Noise Intensity", Range(0, 1)) = 0
        _HologramNoiseSpeed ("Noise Speed", Float) = 1
        [Toggle(_HOLOGRAM_MASK)] _UseHologramMask ("Use Hologram Mask", Float) = 0
        _HologramMask ("Hologram Mask", 2D) = "white" {}
        _HologramMaskScrollSpeed ("Hologram Mask Scroll Speed XY", Vector) = (0,0,0,0)
        _HologramMaskRotateSpeed ("Hologram Mask Rotate Speed", Float) = 0
        [Toggle(_HOLOGRAM_NOISE)] _UseHologramNoise ("Use Noise Texture", Float) = 0
        _HologramNoiseTex ("Noise Texture", 2D) = "white" {}
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _HologramBlendMode ("Hologram Blend Mode", Float) = 0
        _HologramBlend ("Hologram Blend", Range(0, 1)) = 1
        _HologramBlur ("Hologram Blur", Range(0, 1)) = 0
        [Toggle(_GLITCH)] _Glitch ("Enable Glitch", Float) = 0
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.5
        _GlitchSpeed ("Glitch Speed", Float) = 1
        _GlitchBlockSize ("Glitch Block Size", Range(0.01, 1)) = 0.1
        _GlitchRGBSplitIntensity ("RGB Split Intensity", Range(0, 1)) = 0.5
        _GlitchFrequency ("Glitch Frequency", Range(0, 1)) = 0.3
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _GlitchBlendMode ("Glitch Blend Mode", Float) = 0
        _GlitchBlend ("Glitch Blend", Range(0, 1)) = 1
        _GlitchBlur ("Glitch Blur", Range(0, 1)) = 0

        [Header(Decal System Stickers)]
        [Toggle(_DECAL)] _Decal ("Enable Decal", Float) = 0
        _DecalTex ("Decal Texture", 2D) = "white" {}
        _DecalColor ("Decal Color", Color) = (1,1,1,1)
        _DecalPosition ("Decal Position XY", Vector) = (0,0,0,0)
        _DecalRotation ("Decal Rotation", Range(0, 360)) = 0
        _DecalScale ("Decal Scale", Float) = 1
        [Enum(Add,0,Multiply,1,Overlay,2,Replace,3)] _DecalBlendMode ("Decal Blend Mode", Float) = 0
        _DecalBlend ("Decal Blend", Range(0, 1)) = 1
        _DecalBlur ("Decal Blur", Range(0, 1)) = 0

        [Header(Backface Texture Cloth Interior)]
        [Toggle(_BACKFACE_TEXTURE)] _BackfaceTexture ("Enable Backface Texture", Float) = 0
        _BackfaceTex ("Backface Texture", 2D) = "white" {}
        _BackfaceColor ("Backface Color", Color) = (1,1,1,1)
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _BackfaceBlendMode ("Backface Blend Mode", Float) = 0
        _BackfaceBlend ("Backface Blend", Range(0, 1)) = 1

        [Header(Video Render Texture Screen Display)]
        [Toggle(_VIDEO_TEXTURE)] _VideoTexture ("Enable Video Texture", Float) = 0
        _VideoTex ("Video Render Texture", 2D) = "black" {}
        _VideoEmission ("Video Emission", Range(0, 5)) = 1
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _VideoBlendMode ("Video Blend Mode", Float) = 0
        _VideoBlend ("Video Blend", Range(0, 1)) = 1

        [Header(LTCGI Realtime GI Support)]
        [Toggle(_LTCGI)] _LTCGI ("Enable LTCGI", Float) = 0
        _LTCGIIntensity ("LTCGI Intensity", Range(0, 2)) = 1
        _LTCGISpecular ("LTCGI Specular", Range(0, 1)) = 0.5
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _LTCGIBlendMode ("LTCGI Blend Mode", Float) = 0
        _LTCGIBlend ("LTCGI Blend", Range(0, 1)) = 1

        [Header(Water Drip Effect)]
        [Toggle(_WATER_DRIP)] _WaterDrip ("Enable Water Drip", Float) = 0
        _DripColor ("Drip Color", Color) = (0.7, 0.85, 1.0, 1)
        _DripSpeed ("Drip Speed", Range(0.1, 5)) = 1
        _DripDensity ("Drip Density", Range(0, 1)) = 0.5
        _DripSize ("Drip Size", Range(0.01, 0.5)) = 0.15
        _DripTrailLength ("Drip Trail Length", Range(0, 3)) = 1.0
        _DripIntensity ("Drip Intensity", Range(0, 2)) = 1
        _DripSharpness ("Drip Sharpness", Range(0.5, 5)) = 2
        [Toggle(_DRIP_MASK)] _UseDripMask ("Use Drip Mask", Float) = 0
        _DripMask ("Drip Mask", 2D) = "white" {}
        _DripMaskScrollSpeed ("Drip Mask Scroll Speed XY", Vector) = (0,0,0,0)
        _DripMaskRotateSpeed ("Drip Mask Rotate Speed", Float) = 0
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _DripBlendMode ("Drip Blend Mode", Float) = 0
        _DripBlend ("Drip Blend", Range(0, 1)) = 1
        _DripBlur ("Drip Blur", Range(0, 1)) = 0

        [Header(Dithering Alpha Transparent Dithering)]
        [Toggle(_DITHERING_ALPHA)] _DitheringAlpha ("Enable Dithering Alpha", Float) = 0
        _DitheringAlphaScale ("Dithering Alpha Scale", Range(1, 100)) = 10

        [Header(Refraction)]
        [Toggle(_REFRACTION)] _Refraction ("Enable Refraction", Float) = 0
        _RefractionIndex ("Refraction Index IOR", Range(1, 3)) = 1.5
        _RefractionIntensity ("Refraction Intensity", Range(0, 1)) = 1
        _RefractionBlur ("Refraction Blur", Range(0, 1)) = 0
        [Toggle(_REFRACTION_MASK)] _UseRefractionMask ("Use Refraction Mask", Float) = 0
        _RefractionMask ("Refraction Mask", 2D) = "white" {}
        [Enum(Normal,0,Soft,1,Screen,2,Overlay,3)] _RefractionBlendMode ("Refraction Blend Mode", Float) = 0
        _RefractionBlend ("Refraction Blend", Range(0, 1)) = 1

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
        [Enum(Off,0,On,1)] _ZWrite ("Z Write", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
        }

        // NOTE: GrabPass is required for _REFRACTION feature.
        // For materials without refraction, consider using the non-GrabPass variant for better performance.
        // TODO: Create NataneToonShader_NoRefraction variant without GrabPass
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
            #pragma shader_feature_local _OUTLINE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON DIRLIGHTMAP_COMBINED LIGHTMAP_SHADOW_MIXING SHADOWS_SHADOWMASK

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
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
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.uv = v.uv;

                #ifdef _OUTLINE
                    // Calculate distance compensation for consistent outline width
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
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

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
            #pragma shader_feature_local _MAIN_TEX_ANIMATION
            #pragma shader_feature_local _2ND_TEXTURE
            #pragma shader_feature_local _3RD_TEXTURE
            #pragma shader_feature_local _4TH_TEXTURE
            #pragma shader_feature_local _5TH_TEXTURE
            #pragma shader_feature_local _USE_RAMP
            #pragma shader_feature_local _USE_MULTI_SHADOW
            #pragma shader_feature_local _SHADOW_RECEIVE_MASK
            #pragma shader_feature_local _SDF_MAP
            #pragma shader_feature_local _SHADING_GRADE_MAP
            #pragma shader_feature_local _USE_AO
            #pragma shader_feature_local _USE_DITHERING
            #pragma shader_feature_local _SOFT_LIGHTING_MODE
            #pragma shader_feature_local _USE_LIGHT_VOLUME
            #pragma shader_feature_local _LIGHT_VOLUME_SPECULAR
            #pragma shader_feature_local _SPECULAR
            #pragma shader_feature_local _RIM_LIGHT
            #pragma shader_feature_local _RIM_LIGHT_2
            #pragma shader_feature_local _SSS
            #pragma shader_feature_local _MATCAP
            #pragma shader_feature_local _GLITTER
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _DISSOLVE
            #pragma shader_feature_local _ALPHA_MASK
            #pragma shader_feature_local _HUE_SHIFT
            #pragma shader_feature_local _REFLECTION
            #pragma shader_feature_local _IRIDESCENCE
            #pragma shader_feature_local _ENV_RIM
            #pragma shader_feature_local _PARALLAX
            #pragma shader_feature_local _REFRACTION
            #pragma shader_feature_local _MATCAP_2
            #pragma shader_feature_local _MATCAP_3
            #pragma shader_feature_local _AUDIOLINK
            #pragma shader_feature_local _DISTANCE_FADE
            #pragma shader_feature_local _VERTEX_ANIMATION
            #pragma shader_feature_local _HOLOGRAM
            #pragma shader_feature_local _GLITCH
            #pragma shader_feature_local _HOLOGRAM_NOISE
            #pragma shader_feature_local _DECAL
            #pragma shader_feature_local _BACKFACE_TEXTURE
            #pragma shader_feature_local _VIDEO_TEXTURE
            #pragma shader_feature_local _LTCGI
            #pragma shader_feature_local _WATER_DRIP
            #pragma shader_feature_local _DITHERING_ALPHA
            #pragma shader_feature_local _VAT
            #pragma shader_feature_local _VAT_NORMAL
            #pragma shader_feature_local _PIXEL_VERTEX_LIGHTS
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON DIRLIGHTMAP_COMBINED LIGHTMAP_SHADOW_MIXING SHADOWS_SHADOWMASK
            #define CUTOUT_VARIANT

            float _Cutoff;

            #include "../Include/Core/NataneToonCore.hlsl"

            half4 frag_cutout(v2f i) : SV_Target
            {
                half4 col = frag(i);
                clip(col.a - _Cutoff);
                return col;
            }

            #define frag frag_cutout

            ENDCG
        }

        // Additional Forward Pass
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
            #pragma multi_compile_fwdadd
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _MAIN_TEX_ANIMATION
            #pragma shader_feature_local _2ND_TEXTURE
            #pragma shader_feature_local _3RD_TEXTURE
            #pragma shader_feature_local _4TH_TEXTURE
            #pragma shader_feature_local _5TH_TEXTURE
            #pragma shader_feature_local _USE_RAMP
            #pragma shader_feature_local _SHADOW_RECEIVE_MASK
            #pragma shader_feature_local _USE_MULTI_SHADOW
            #pragma shader_feature_local _SOFT_LIGHTING_MODE
            #pragma shader_feature_local _DISTANCE_FADE
            #pragma shader_feature_local _DITHERING_ALPHA
            #pragma shader_feature_local _SDF_MAP
            #pragma shader_feature_local _SHADING_GRADE_MAP
            #pragma shader_feature_local _USE_AO
            #pragma shader_feature_local _USE_DITHERING
            #pragma shader_feature_local _SPECULAR
            #pragma shader_feature_local _SSS
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _DISSOLVE
            #pragma shader_feature_local _ALPHA_MASK
            #pragma shader_feature_local _PARALLAX
            #pragma shader_feature_local _VAT
            #pragma shader_feature_local _VAT_NORMAL
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON DIRLIGHTMAP_COMBINED LIGHTMAP_SHADOW_MIXING SHADOWS_SHADOWMASK
            #define CUTOUT_VARIANT

            float _Cutoff;

            #include "../Include/Core/NataneToonCore.hlsl"

            half4 frag_cutout(v2f i) : SV_Target
            {
                half4 col = frag(i);
                clip(col.a - _Cutoff);
                return col;
            }

            #define frag frag_cutout

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
            #pragma multi_compile_instancing
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON DIRLIGHTMAP_COMBINED LIGHTMAP_SHADOW_MIXING SHADOWS_SHADOWMASK

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 texcol = tex2D(_MainTex, i.uv);
                clip(texcol.a - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    CustomEditor "NataneToonShaderGUI"
    FallBack "Transparent/Cutout/Diffuse"
}
