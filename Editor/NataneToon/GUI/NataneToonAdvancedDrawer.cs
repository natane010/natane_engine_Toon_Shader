using UnityEditor;
using NataneToon.Editor;
using static NataneToon.Editor.NataneToonLocalization;

/// <summary>
/// Drawer for the Advanced tab of NataneToonShaderGUI.
/// Orchestrates drawing of all advanced/mapping/animation/rendering sections.
/// 詳細タブの描画を担当するDrawerクラス。
/// </summary>
internal static class NataneToonAdvancedDrawer
{
    public static void Draw(NataneToonShaderGUI gui)
    {
        gui.DrawExpandCollapseButtons((state) => {
            gui.SetFoldout("NormalMap", state); gui.SetFoldout("Parallax", state); gui.SetFoldout("VertexAnimation", state); gui.SetFoldout("VAT", state);
            gui.SetFoldout("Backface", state); gui.SetFoldout("Video", state); gui.SetFoldout("HeightFade", state); gui.SetFoldout("IntersectionFade", state);
            gui.SetFoldout("DistanceFade", state); gui.SetFoldout("PerspectiveFlat", state); gui.SetFoldout("Rendering", state); gui.SetFoldout("Tessellation", state);
            gui.SetFoldout("DetailMap", state); gui.SetFoldout("Triplanar", state); gui.SetFoldout("MirrorControl", state); gui.SetFoldout("QuestLite", state);
        });

        // ─── マッピング ───
        int mapCount = gui.CountEnabledKeywords("_NORMALMAP", "_PARALLAX");
        NataneToonShaderGUIUtility.DrawCategoryDivider(
            L($"マッピング ({mapCount}/2)", $"Mapping ({mapCount}/2)"));
        gui.FilteredDrawSection(gui.DrawNormalMapSection, L("ノーマルマップ", "Normal Map"), "NormalMap");
        gui.FilteredDrawSection(gui.DrawParallaxSection, L("視差マッピング", "Parallax Mapping"), "Parallax");
        gui.FilteredDrawSection(gui.DrawDetailMapSection, L("ディテールマップ", "Detail Map"), "DetailMap");
        gui.FilteredDrawSection(gui.DrawTriplanarSection, L("トライプレーナー", "Triplanar Mapping"), "Triplanar");

        // ─── アニメーション＆特殊 ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("アニメーション＆特殊", "Animation & Special"));
        gui.FilteredDrawSection(gui.DrawVertexAnimationSection, L("頂点アニメーション（風/呼吸/脈動）", "Vertex Animation (Wind/Breath/Pulse)"), "VertexAnimation");
        gui.FilteredDrawSection(gui.DrawVATSection, L("VAT（頂点アニメーション）", "VAT (Vertex Animation Texture)"), "VAT");
        gui.FilteredDrawSection(gui.DrawTessellationSection, L("テッセレーション（曲面スムージング）", "Tessellation (Surface Smoothing)"), "Tessellation");
        gui.FilteredDrawSection(gui.DrawBackfaceSection, L("裏面テクスチャ", "Backface Texture"), "Backface");
        gui.FilteredDrawSection(gui.DrawVideoSection, L("ビデオテクスチャ", "Video Texture"), "Video");
        gui.FilteredDrawSection(gui.DrawHeightFadeSection, L("高さフェード", "Height Fade"), "HeightFade");
        gui.FilteredDrawSection(gui.DrawIntersectionFadeSection, L("オブジェクト交差フェード", "Intersection Fade"), "IntersectionFade");
        gui.FilteredDrawSection(gui.DrawDistanceFadeSection, L("距離フェード", "Distance Fade"), "DistanceFade");
        gui.FilteredDrawSection(gui.DrawPerspectiveFlatSection, L("パースフラット", "Perspective Flatten"), "PerspectiveFlat");

        // ─── VRChat＆パフォーマンス ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("VRChat＆パフォーマンス", "VRChat & Performance"));
        gui.FilteredDrawSection(gui.DrawMirrorControlSection, L("ミラー・カメラ制御", "Mirror / Camera Control"), "MirrorControl");
        gui.FilteredDrawSection(gui.DrawQuestLiteSection, L("Quest軽量パス", "Quest Lite"), "QuestLite");

        // ─── レンダリング ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("レンダリング", "Rendering"));
        gui.FilteredDrawSection(gui.DrawRenderingSection, L("レンダリング", "Rendering"), "Rendering");
    }
}
