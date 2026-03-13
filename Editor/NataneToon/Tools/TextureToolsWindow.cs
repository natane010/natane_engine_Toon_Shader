using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;

    /// <summary>
    /// テクスチャツールのタブ列挙型
    /// </summary>
    public enum TextureToolsTab
    {
        SmoothNormalBaker,
        UVTextureGenerator,
        MaskChannelPacker
    }

    /// <summary>
    /// Texture Tools Window
    /// テクスチャツール
    /// Consolidates texture-related tools into a single tabbed window:
    ///   Smooth Normal Baker / UV Texture Generator / Mask Channel Packer
    /// </summary>
    public class TextureToolsWindow : NataneTabbedToolWindow<TextureToolsTab>
    {
        // ===== Window metadata =====
        protected override string WindowTitleJP => "テクスチャツール";
        protected override string WindowTitleEN => "Texture Tools";
        protected override Vector2 DefaultMinSize => new Vector2(600, 500);

        // ===== Tab factory =====
        protected override INataneToolTab CreateTab(TextureToolsTab tab)
        {
            switch (tab)
            {
                case TextureToolsTab.SmoothNormalBaker:  return new SmoothNormalBakerTab();
                case TextureToolsTab.UVTextureGenerator: return new UVTextureGeneratorTab();
                case TextureToolsTab.MaskChannelPacker:  return new MaskChannelPackerTab();
                default:                                 return new SmoothNormalBakerTab();
            }
        }

        // ===== Menu items =====
        [MenuItem("Tools/Natane/テクスチャ Texture/テクスチャツール Texture Tools", false, 10)]
        public static void ShowWindow()
        {
            var w = GetWindow<TextureToolsWindow>();
            w.OpenToTab(TextureToolsTab.SmoothNormalBaker);
        }

        /// <summary>
        /// 指定タブを開いた状態でウィンドウを表示する。
        /// オプションでマテリアルコンテキストを渡せる。
        /// </summary>
        public static void ShowTab(TextureToolsTab tab, Material material = null)
        {
            var w = GetWindow<TextureToolsWindow>();
            w.OpenToTab(tab, material);
        }

        // =================================================================
        //  Stub tab adapters (placeholder until full migration)
        // =================================================================

        /// <summary>
        /// 法線ベイク スタブタブ
        /// </summary>
        private sealed class SmoothNormalBakerTab : INataneToolTab
        {
            public string TabLabel   => L("法線ベイク", "Normal");
            public string TabTooltip => L("メッシュのスムース法線をベイク", "Bake smooth normals for meshes");
            public string HelpToolKey => "SmoothNormalBaker";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("法線ベイク - このタブは統合中です",
                      "Smooth Normal Baker - This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧スムース法線ベイクを開く", "Open Legacy Smooth Normal Baker"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("SmoothNormalBaker");
                }
            }
        }

        /// <summary>
        /// UVテクスチャ スタブタブ
        /// </summary>
        private sealed class UVTextureGeneratorTab : INataneToolTab
        {
            public string TabLabel   => L("UVテクスチャ", "UV");
            public string TabTooltip => L("UVレイアウトからテクスチャを生成", "Generate textures from UV layouts");
            public string HelpToolKey => "UVTextureGenerator";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("UVテクスチャ - このタブは統合中です",
                      "UV Texture Generator - This tab is being integrated"),
                    MessageType.Info);

                EditorGUILayout.Space(5);
                if (GUILayout.Button(
                    L("旧UVテクスチャジェネレーターを開く", "Open Legacy UV Texture Generator"),
                    GUILayout.Height(25)))
                {
                    NataneToolMenuPaths.TryOpenByToolKey("UVTextureGenerator");
                }
            }
        }

        /// <summary>
        /// マスクパック スタブタブ
        /// </summary>
        private sealed class MaskChannelPackerTab : INataneToolTab
        {
            public string TabLabel   => L("マスクパック", "Mask");
            public string TabTooltip => L("マスクテクスチャのチャンネルをパック", "Pack mask texture channels");
            public string HelpToolKey => "MaskTextureChannelPacker";
            public bool RequiresMaterial => false;

            public void OnTabEnable(EditorWindow parentWindow) { }
            public void OnTabDisable() { }
            public void OnTabDestroy() { }

            public void OnTabGUI(Material contextMaterial)
            {
                EditorGUILayout.HelpBox(
                    L("マスクチャンネルパッカー — 新規ツール・開発中です",
                      "Mask Channel Packer — New tool, under development"),
                    MessageType.Info);
            }
        }
    }
}
