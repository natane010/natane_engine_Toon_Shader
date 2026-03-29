using UnityEditor;
using NataneToon.Editor;
using static NataneToon.Editor.NataneToonLocalization;

/// <summary>
/// Drawer for the Environment tab of NataneToonShaderGUI.
/// Orchestrates drawing of all environment/reflection-related sections.
/// 環境タブの描画を担当するDrawerクラス。
/// </summary>
internal static class NataneToonEnvironmentDrawer
{
    public static void Draw(NataneToonShaderGUI gui)
    {
        gui.DrawExpandCollapseButtons((state) => {
            gui.SetFoldout("Reflection", state); gui.SetFoldout("FakeReflection", state); gui.SetFoldout("Iridescence", state);
            gui.SetFoldout("EnvironmentalRim", state); gui.SetFoldout("Refraction", state);
            gui.SetFoldout("HeightFog", state);
        });

        gui.FilteredDrawSection(gui.DrawReflectionSection, L("リフレクション", "Reflection"), "Reflection");
        gui.FilteredDrawSection(gui.DrawFakeReflectionSection, L("フェイクリフレクション", "Fake Reflection"), "FakeReflection");
        gui.FilteredDrawSection(gui.DrawIridescenceSection, L("イリデッセンス", "Iridescence"), "Iridescence");
        gui.FilteredDrawSection(gui.DrawEnvironmentalRimSection, L("環境リム", "Environmental Rim"), "EnvironmentalRim");
        gui.FilteredDrawSection(gui.DrawRefractionSection, L("屈折", "Refraction"), "Refraction");
        gui.FilteredDrawSection(gui.DrawHeightFogSection, L("ハイトフォグ", "Height Fog"), "HeightFog");
        gui.FilteredDrawSection(gui.DrawDepthColorFadeSection, L("深度カラーフェード", "Depth Color Fade"), "DepthColorFade");
    }
}
