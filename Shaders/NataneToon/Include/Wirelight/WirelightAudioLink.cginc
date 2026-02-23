// AudioLink integration for Wirelight effects
// Provides audio-reactive functionality for VRChat worlds

#ifndef WIRELIGHT_AUDIOLINK_INCLUDED
#define WIRELIGHT_AUDIOLINK_INCLUDED

// AudioLink sampling positions
#define ALPASS_AUDIOLINK                uint2(0, 4)
#define ALPASS_THEME_COLOR0             uint2(0, 0)
#define ALPASS_THEME_COLOR1             uint2(1, 0)
#define ALPASS_THEME_COLOR2             uint2(2, 0)
#define ALPASS_THEME_COLOR3             uint2(3, 0)
#define ALPASS_CHRONOTENSITY            uint2(16, 4)

#ifdef _AUDIOLINK
    sampler2D _AudioTexture;
    uniform float _AudioLinkAvailable;

    // Check if AudioLink is available
    bool AudioLinkIsAvailable()
    {
        #if defined(_AUDIOLINK)
            return _AudioLinkAvailable > 0;
        #else
            return false;
        #endif
    }

    // Sample AudioLink data texture
    float4 AudioLinkData(uint2 pos)
    {
        #if defined(_AUDIOLINK)
            if (AudioLinkIsAvailable())
            {
                return tex2Dlod(_AudioTexture, float4(pos / 64.0, 0, 0));
            }
        #endif
        return float4(0, 0, 0, 0);
    }

    // Get Chronotensity value (timing-based animation control)
    // mode: 0-7 different timing modes
    // band: 0-3 frequency band (0=Bass, 1=Low Mid, 2=High Mid, 3=Treble)
    float AudioLinkGetChronoTime(int mode, int band)
    {
        #if defined(_AUDIOLINK)
            if (!AudioLinkIsAvailable()) return 0.0;

            float intensity = AudioLinkData(ALPASS_AUDIOLINK + uint2(0, band)).r;
            float time = _Time.y;

            switch (mode)
            {
                case 0: // Accelerate with intensity
                    return time * (1.0 + intensity);

                case 1: // Smooth accelerate
                    return time * (1.0 + smoothstep(0, 1, intensity));

                case 2: // Oscillate based on intensity
                    return sin(time * (1.0 + intensity * 2.0));

                case 3: // Smooth oscillate
                    return sin(time * (1.0 + smoothstep(0, 1, intensity) * 2.0));

                case 4: // Move when dark, stop when bright
                    return time * (1.0 - intensity);

                case 5: // Smooth move when dark
                    return time * (1.0 - smoothstep(0, 1, intensity));

                case 6: // Forward when dark, backward when bright
                    return time * (1.0 - intensity * 2.0);

                case 7: // Smooth bidirectional
                    return time * (1.0 - smoothstep(0, 1, intensity) * 2.0);

                default:
                    return time;
            }
        #endif
        return _Time.y;
    }

    // Get AudioLink theme color
    // colorIndex: 0-3 for the four theme colors
    // invert: whether to invert the color
    float4 AudioLinkGetThemeColor(int colorIndex, float invert)
    {
        #if defined(_AUDIOLINK)
            if (!AudioLinkIsAvailable()) return float4(1, 1, 1, 1);

            float4 color = float4(1, 1, 1, 1);

            switch (colorIndex)
            {
                case 0:
                    color = AudioLinkData(ALPASS_THEME_COLOR0);
                    break;
                case 1:
                    color = AudioLinkData(ALPASS_THEME_COLOR1);
                    break;
                case 2:
                    color = AudioLinkData(ALPASS_THEME_COLOR2);
                    break;
                case 3:
                    color = AudioLinkData(ALPASS_THEME_COLOR3);
                    break;
            }

            // Ensure non-negative values from AudioLink data
            color = max(color, 0.0);
            color.a = saturate(color.a);

            // Apply inversion if requested
            if (invert > 0.5)
            {
                color.rgb = 1.0 - saturate(color.rgb);
            }

            return color;
        #endif
        return float4(1, 1, 1, 1);
    }

    // Get frequency band intensity
    // band: 0=Bass, 1=Low Mid, 2=High Mid, 3=Treble
    float AudioLinkGetBandIntensity(int band)
    {
        #if defined(_AUDIOLINK)
            if (!AudioLinkIsAvailable()) return 0.0;
            return AudioLinkData(ALPASS_AUDIOLINK + uint2(0, band)).r;
        #endif
        return 0.0;
    }

#else
    // Fallback functions when AudioLink is disabled
    bool AudioLinkIsAvailable() { return false; }
    float4 AudioLinkData(uint2 pos) { return float4(0, 0, 0, 0); }
    float AudioLinkGetChronoTime(int mode, int band) { return _Time.y; }
    float4 AudioLinkGetThemeColor(int colorIndex, float invert) { return float4(1, 1, 1, 1); }
    float AudioLinkGetBandIntensity(int band) { return 0.0; }
#endif

#endif // WIRELIGHT_AUDIOLINK_INCLUDED
