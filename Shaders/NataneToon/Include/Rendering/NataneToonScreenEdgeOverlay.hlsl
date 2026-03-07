#ifndef NATANE_TOON_SCREEN_EDGE_OVERLAY_INCLUDED
#define NATANE_TOON_SCREEN_EDGE_OVERLAY_INCLUDED

half4 fragScreenEdgeOverlay(v2f i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    #ifndef _SCREEN_EDGE
        discard;
        return 0;
    #else
        float2 screenUV = i.pos.xy / max(_ScreenParams.xy, float2(1.0, 1.0));
        half edgeValue = ApplyScreenEdge(screenUV, _EdgeDepthSensitivity, _EdgeNormalSensitivity, _EdgeWidth);
        half alpha = saturate(edgeValue * _EdgeBlend);

        clip(alpha - 0.0001h);

        half4 overlayColor = half4(_EdgeColor.rgb, alpha);
        UNITY_APPLY_FOG(i.fogCoord, overlayColor);
        return overlayColor;
    #endif
}

#endif // NATANE_TOON_SCREEN_EDGE_OVERLAY_INCLUDED
