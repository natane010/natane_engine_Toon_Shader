using UnityEditor;
using NataneToon.Editor;
using static NataneToon.Editor.NataneToonLocalization;

/// <summary>
/// Drawer for the Effects tab of NataneToonShaderGUI.
/// Orchestrates drawing of all effects-related sections.
/// エフェクトタブの描画を担当するDrawerクラス。
/// </summary>
internal static class NataneToonEffectsDrawer
{
    public static void Draw(NataneToonShaderGUI gui)
    {
        gui.DrawExpandCollapseButtons((state) => {
            gui.SetFoldout("Specular", state); gui.SetFoldout("HairSpecular", state); gui.SetFoldout("RimLight", state); gui.SetFoldout("SSS", state);
            gui.SetFoldout("MatCap", state); gui.SetFoldout("ProceduralMatCap", state); gui.SetFoldout("Glitter", state); gui.SetFoldout("Drip", state); gui.SetFoldout("Smear", state); gui.SetFoldout("Fur", state); gui.SetFoldout("Decal", state);
            gui.SetFoldout("Hologram", state); gui.SetFoldout("Outline", state); gui.SetFoldout("Emission", state);
            gui.SetFoldout("VirtualExpression", state); gui.SetFoldout("AudioLink", state); gui.SetFoldout("SurfaceCover", state);
        });

        // ─── 光源エフェクト ───
        int lightCount = gui.CountEnabledKeywords("_SPECULAR", "_HAIR_SPECULAR", "_RIM_LIGHT", "_SSS");
        NataneToonShaderGUIUtility.DrawCategoryDivider(
            L($"光源エフェクト ({lightCount}/4)", $"Light Source Effects ({lightCount}/4)"));
        gui.FilteredDrawSection(gui.DrawSpecularSection, L("スペキュラー", "Specular"), "Specular");
        gui.FilteredDrawSection(gui.DrawHairSpecularSection, L("ヘアスペキュラー", "Hair Specular"), "HairSpecular");
        gui.FilteredDrawSection(gui.DrawRimLightSection, L("リムライト", "Rim Light"), "RimLight");
        gui.FilteredDrawSection(gui.DrawSSSSection, "SSS", "SSS");

        // ─── 表面エフェクト ───
        int surfaceCount = gui.CountEnabledKeywords("_MATCAP", "_PROCEDURAL_MATCAP", "_GLITTER", "_WATER_DRIP", "_SMEAR", "_FUR", "_DECAL", "_SURFACE_COVER");
        NataneToonShaderGUIUtility.DrawCategoryDivider(
            L($"表面エフェクト ({surfaceCount}/8)", $"Surface Effects ({surfaceCount}/8)"));
        gui.FilteredDrawSection(gui.DrawMatCapSection, "MatCap", "MatCap");
        gui.FilteredDrawSection(gui.DrawProceduralMatCapSection, L("プロシージャルMatCap", "Procedural MatCap"), "ProceduralMatCap");
        gui.FilteredDrawSection(gui.DrawGlitterSection, L("グリッター", "Glitter"), "Glitter");
        gui.FilteredDrawSection(gui.DrawDripSection, L("雫エフェクト", "Drip Effect"), "Drip");
        gui.FilteredDrawSection(gui.DrawSmearSection, L("スミア", "Smear"), "Smear");
        gui.FilteredDrawSection(gui.DrawFurSection, L("ファー", "Fur"), "Fur");
        gui.FilteredDrawSection(gui.DrawDecalSection, L("デカール", "Decal"), "Decal");
        gui.FilteredDrawSection(gui.DrawSurfaceCoverSection, L("サーフェスカバー", "Surface Cover"), "SurfaceCover");

        // ─── ビジュアルエフェクト ───
        int visualCount = gui.CountEnabledKeywords("_COLOR_QUANTIZE", "_HOLOGRAM", "_OUTLINE", "_EMISSION", "_AUDIOLINK");
        NataneToonShaderGUIUtility.DrawCategoryDivider(
            L($"ビジュアルエフェクト ({visualCount}/5)", $"Visual Effects ({visualCount}/5)"));
        gui.FilteredDrawSection(gui.DrawHologramSection, L("ホログラム＆グリッチ", "Hologram & Glitch"), "Hologram");
        gui.FilteredDrawSection(gui.DrawIllustrationStyleSection, L("イラスト調スタイル", "Illustration Style"), "IllustrationStyle");
        gui.FilteredDrawSection(gui.DrawOutlineSection, L("アウトライン", "Outline"), "Outline");
        gui.FilteredDrawSection(gui.DrawEmissionSection, L("エミッション", "Emission"), "Emission");
        gui.FilteredDrawSection(gui.DrawVirtualExpressionSection, L("バーチャル表現", "Virtual Expression"), "VirtualExpression");
        gui.FilteredDrawSection(gui.DrawAudioLinkSection, "AudioLink", "AudioLink");
    }
}
