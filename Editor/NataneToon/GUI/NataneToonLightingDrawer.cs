using UnityEditor;
using NataneToon.Editor;
using static NataneToon.Editor.NataneToonLocalization;

/// <summary>
/// Drawer for the Lighting tab of NataneToonShaderGUI.
/// Orchestrates drawing of all lighting-related sections.
/// ライティングタブの描画を担当するDrawerクラス。
/// </summary>
internal static class NataneToonLightingDrawer
{
    public static void Draw(NataneToonShaderGUI gui)
    {
        gui.DrawExpandCollapseButtons((state) => {
            gui.SetFoldout("AdvancedLighting", state); gui.SetFoldout("CastShadowColor", state); gui.SetFoldout("LightSnap", state);
            gui.SetFoldout("AO", state); gui.SetFoldout("Dithering", state);
            gui.SetFoldout("LightVolume", state); gui.SetFoldout("LTCGI", state);
            gui.SetFoldout("BackgroundLightmap", state); gui.SetFoldout("PBR", state);
        });

        // ─── ライティング基本 ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("ライティング基本", "Lighting Basics"));
        gui.FilteredDrawSection(gui.DrawAdvancedLightingSection, L("高度なライティング", "Advanced Lighting"), "AdvancedLighting");
        gui.FilteredDrawSection(gui.DrawCastShadowColorSection, L("キャストシャドウカラー", "Cast Shadow Color"), "CastShadowColor");
        gui.FilteredDrawSection(gui.DrawLightSnapSection, L("ライト方向スナップ", "Light Direction Snap"), "LightSnap");
        gui.FilteredDrawSection(gui.DrawAOSection, "AO", "AO");
        gui.FilteredDrawSection(gui.DrawDitheringSection, L("ディザリング", "Dithering"), "Dithering");

        // ─── 外部ライティング ───
        int extLightCount = gui.CountEnabledKeywords("_USE_LIGHT_VOLUME", "_LTCGI");
        NataneToonShaderGUIUtility.DrawCategoryDivider(
            L($"外部ライティング ({extLightCount}/2)", $"External Lighting ({extLightCount}/2)"));
        gui.FilteredDrawSection(gui.DrawLightVolumeSection, "Light Volume", "LightVolume");
        gui.FilteredDrawSection(gui.DrawLTCGISection, "LTCGI", "LTCGI");

        // ─── 背景シェーダー専用 ───
        if (gui.GetCurrentRenderingMode() == NataneToonShaderGUI.RenderingMode.Background)
        {
            NataneToonShaderGUIUtility.DrawCategoryDivider(L("背景シェーダー専用", "Background Shader Only"));
            gui.FilteredDrawSection(gui.DrawBackgroundLightmapSection, L("背景ライトマップ", "Background Lightmap"), "BackgroundLightmap");
            gui.FilteredDrawSection(gui.DrawPBRSection, L("PBR マテリアル", "PBR Material"), "PBR");
        }
    }
}
