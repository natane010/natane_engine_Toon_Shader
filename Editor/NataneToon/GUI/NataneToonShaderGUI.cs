using UnityEngine;
using UnityEditor;
using System;
using NataneToon.Editor;

/// <summary>
/// Custom shader GUI for Natane Toon Shader
///
/// Provides a user-friendly, organized interface for the NataneToon shader system
/// with support for:
/// - Material presets and sharing
/// - Performance indicators
/// - Makeup texture layers (up to 4 layers with HSV adjustment)
/// - Advanced shading and lighting controls
/// - VRC Light Volumes integration
/// - Multiple surface effects (Specular, Rim Light, SSS, MatCap, etc.)
///
/// The GUI is organized into logical categories with foldout sections for better
/// navigation and reduced clutter.
/// </summary>
public class NataneToonShaderGUI : ShaderGUI
{
    // ===== CONSTANTS =====
    /// <summary>Threshold for float comparisons (e.g., checking if a toggle is enabled)</summary>
    private const float FLOAT_COMPARISON_THRESHOLD = 0.5f;

    /// <summary>Number of makeup texture layers supported (2nd, 3rd, 4th, 5th)</summary>
    private const int MAKEUP_TEXTURE_COUNT = 4;

    /// <summary>Number of multi-tone shadow levels supported</summary>
    private const int MULTI_SHADOW_LEVELS = 3; // 1st, 2nd, 3rd

    /// <summary>Minimum value to consider a parameter active (avoid floating point issues)</summary>
    private const float MIN_PARAMETER_VALUE = 0.001f;

    // ===== CACHED GUI STYLES =====
    private static GUIStyle _cachedHeaderTitleStyle;
    private static GUIStyle CachedHeaderTitleStyle
    {
        get
        {
            if (_cachedHeaderTitleStyle == null)
            {
                _cachedHeaderTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                _cachedHeaderTitleStyle.fontSize = 14;
            }
            return _cachedHeaderTitleStyle;
        }
    }

    private static GUIStyle _cachedHelpPreviewStyle;
    private static GUIStyle CachedHelpPreviewStyle
    {
        get
        {
            if (_cachedHelpPreviewStyle == null)
            {
                _cachedHelpPreviewStyle = new GUIStyle(EditorStyles.miniLabel);
                _cachedHelpPreviewStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            }
            return _cachedHelpPreviewStyle;
        }
    }

    private static GUIStyle _cachedCategoryBoxStyle;
    private static GUIStyle CachedCategoryBoxStyle
    {
        get
        {
            if (_cachedCategoryBoxStyle == null)
            {
                _cachedCategoryBoxStyle = new GUIStyle(GUI.skin.box);
                _cachedCategoryBoxStyle.padding = new RectOffset(10, 10, 5, 5);
            }
            return _cachedCategoryBoxStyle;
        }
    }

    private static GUIStyle _cachedCategoryTitleStyle;
    private static GUIStyle CachedCategoryTitleStyle
    {
        get
        {
            if (_cachedCategoryTitleStyle == null)
            {
                _cachedCategoryTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                _cachedCategoryTitleStyle.fontSize = 13;
            }
            return _cachedCategoryTitleStyle;
        }
    }

    private static GUIStyle _cachedCategoryDescStyle;
    private static GUIStyle CachedCategoryDescStyle
    {
        get
        {
            if (_cachedCategoryDescStyle == null)
            {
                _cachedCategoryDescStyle = new GUIStyle(EditorStyles.miniLabel);
                _cachedCategoryDescStyle.wordWrap = true;
            }
            return _cachedCategoryDescStyle;
        }
    }

    private static GUIStyle _cachedHDRToggleStyle;
    private static GUIStyle CachedHDRToggleStyle
    {
        get
        {
            if (_cachedHDRToggleStyle == null)
            {
                _cachedHDRToggleStyle = new GUIStyle(EditorStyles.miniButton);
                _cachedHDRToggleStyle.fontSize = 9;
                _cachedHDRToggleStyle.fixedWidth = 36;
                _cachedHDRToggleStyle.fixedHeight = 16;
                _cachedHDRToggleStyle.padding = new RectOffset(2, 2, 1, 1);
            }
            return _cachedHDRToggleStyle;
        }
    }

    // ===== FIELD REFERENCES =====
    /// <summary>All shader properties for the current material</summary>
    private MaterialProperty[] properties;

    /// <summary>Unity's material editor instance</summary>
    private MaterialEditor materialEditor;

    /// <summary>Target material being edited</summary>
    private Material targetMaterial;

    // ===== RENDERING MODE =====
    public enum RenderingMode
    {
        Opaque = 0,
        Cutout = 1,
        Transparent = 2
    }

    // ===== UI STATE =====
    // Tab index for category navigation
    private int selectedTab = 0;
    private readonly string[] tabNames = new string[]
    {
        "テクスチャ&色", "ライト&影", "エフェクト", "環境&反射", "詳細設定"
    };

    // ===== SEARCH STATE =====
    private string searchQuery = "";
    /// <summary>
    /// Section search data: pairs of (Japanese name, English keywords) for search matching
    /// </summary>
    private static readonly string[][] sectionSearchData = new string[][]
    {
        // { drawMethodSuffix, japanese, english }
        new[] { "MainTexture", "メインテクスチャ", "main texture color" },
        new[] { "MakeupTextures", "追加テクスチャ メイクアップ 2nd 3rd 4th 5th", "makeup texture layer" },
        new[] { "Shading", "シェーディング トゥーン 影 陰", "shading toon shadow" },
        new[] { "AdvancedLighting", "ライティング詳細 光源", "advanced lighting" },
        new[] { "AO", "アンビエントオクルージョン AO", "ambient occlusion ao" },
        new[] { "Dithering", "ディザリング スクリーントーン", "dithering screen tone" },
        new[] { "LightVolume", "VRC ライトボリューム", "vrc light volume" },
        new[] { "LTCGI", "LTCGI リアルタイムエリアライト", "ltcgi area light" },
        new[] { "Specular", "スペキュラー反射", "specular reflection" },
        new[] { "HairSpecular", "ヘアハイライト ヘアスペキュラー", "hair specular highlight kajiya" },
        new[] { "RimLight", "リムライト 輪郭光 オフセットリム", "rim light edge offset" },
        new[] { "SSS", "半透明 SSS サブサーフェス", "sss subsurface scattering translucent" },
        new[] { "MatCap", "マットキャップ MatCap", "matcap sphere map" },
        new[] { "Glitter", "グリッター ラメ", "glitter sparkle" },
        new[] { "Drip", "雫 エフェクト ドリップ", "drip water drop" },
        new[] { "Hologram", "ホログラム グリッチ", "hologram glitch" },
        new[] { "Decal", "デカール 貼り付け", "decal sticker" },
        new[] { "Outline", "アウトライン 輪郭線 スムース法線", "outline contour smooth normal" },
        new[] { "Emission", "エミッション 発光", "emission glow" },
        new[] { "VirtualExpression", "バーチャル表現 ディゾルブ", "virtual expression dissolve" },
        new[] { "AudioLink", "AudioLink 音楽連動", "audiolink music reactive" },
        new[] { "Reflection", "反射 リフレクション キューブマップ", "reflection cubemap" },
        new[] { "Iridescence", "イリデッセンス 玉虫色", "iridescence" },
        new[] { "EnvironmentalRim", "環境リム", "environmental rim" },
        new[] { "Refraction", "屈折 リフラクション", "refraction ior" },
        new[] { "NormalMap", "ノーマルマップ 法線", "normal map bump" },
        new[] { "Parallax", "視差マッピング パララックス", "parallax height map" },
        new[] { "VertexAnimation", "頂点アニメーション 風 呼吸 脈動", "vertex animation wind breath pulse" },
        new[] { "VAT", "VAT 頂点アニメーション Houdini", "vat vertex animation texture houdini" },
        new[] { "Tessellation", "テッセレーション 曲面 スムージング", "tessellation smoothing phong" },
        new[] { "Backface", "裏面テクスチャ", "backface texture back" },
        new[] { "Video", "ビデオテクスチャ", "video texture render" },
        new[] { "DistanceFade", "距離フェード", "distance fade lod" },
        new[] { "Rendering", "レンダリング設定 描画タイプ", "rendering mode opaque cutout transparent" },
    };

    // ===== SHADER TYPE DRAWER INSTANCES =====
    private NataneToon.Editor.NataneToonEyeDrawer eyeDrawer;
    private NataneToon.Editor.NataneToonWirelightDrawer wirelightDrawer;
    private NataneToon.Editor.NataneToonScreenFXDrawer screenFXDrawer;

    // ===== FOLDOUT STATES =====
    // Foldout states are per-material and persisted using EditorPrefs
    private bool showPresets;
    private bool showPerformance;
    private bool showMainTexture;
    private bool showMakeupTextures;
    private bool showShading;
    private bool showAdvancedLighting;
    private bool showLightVolume;
    private bool showSpecular;
    private bool showRimLight;
    private bool showSSS;
    private bool showMatCap;
    private bool showGlitter;
    private bool showDrip;
    private bool showHologram;
    private bool showOutline;
    private bool showEmission;
    private bool showVirtualExpression;
    private bool showNormalMap;
    private bool showReflection;
    private bool showIridescence;
    private bool showEnvironmentalRim;
    private bool showParallax;
    private bool showRefraction;
    private bool showRendering;
    private bool showVAT;
    private bool showTessellation;
    private bool showVertexAnimation;
    private bool showLTCGI;
    private bool showAO;
    private bool showDithering;
    private bool showDecal;
    private bool showBackface;
    private bool showVideo;
    private bool showAudioLink;
    private bool showDistanceFade;
    private bool showHairSpecular;
    private bool showFeatureOverview;

    // Foldout state initialization tracking
    private bool _foldoutStatesLoaded = false;
    private int _lastLoadedMaterialInstanceId = -1;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        try
        {
            this.materialEditor = materialEditor;
            this.properties = properties;
            this.targetMaterial = materialEditor.target as Material;

            // Validate that we have valid references
            if (this.materialEditor == null || this.properties == null || this.targetMaterial == null)
            {
                EditorGUILayout.HelpBox("マテリアルエディタの初期化に失敗しました。", MessageType.Error);
                return;
            }

            // Load foldout states from EditorPrefs only on first call or material change
            int currentMaterialId = this.targetMaterial.GetInstanceID();
            if (!_foldoutStatesLoaded || _lastLoadedMaterialInstanceId != currentMaterialId)
            {
                LoadFoldoutStates();
                _foldoutStatesLoaded = true;
                _lastLoadedMaterialInstanceId = currentMaterialId;
            }
            LoadUIState();

            // ===== Shader Type Dropdown =====
            bool shouldReturn;
            bool isNonToon = NataneToon.Editor.NataneToonShaderTypeSwitcher.DrawShaderTypeDropdown(targetMaterial, materialEditor, out shouldReturn);
            if (shouldReturn)
            {
                GUIUtility.ExitGUI();
                return;
            }
            if (isNonToon)
            {
                DrawNonToonShaderGUI(NataneToon.Editor.NataneToonShaderTypeSwitcher.DetectShaderType(targetMaterial));
                return;
            }

            // Validate and fix shader keywords (ensures keywords match property values)
            ValidateAndFixKeywords();

            // ===== Compact Header =====
            DrawCompactHeader();

            // ===== Tab Navigation =====
            EditorGUI.BeginChangeCheck();
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
            if (EditorGUI.EndChangeCheck())
            {
                SaveUIState();
                GUI.FocusControl(null); // Clear focus to update UI

                // Force repaint and exit GUI to prevent layout conflicts
                if (materialEditor != null)
                {
                    materialEditor.Repaint();
                }
                GUIUtility.ExitGUI();
            }

            // ===== Search Bar =====
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("検索:", GUILayout.Width(35));
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);
            if (!string.IsNullOrEmpty(searchQuery) && GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                searchQuery = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);

            // ===== Tab Content or Search Results =====
            if (!string.IsNullOrEmpty(searchQuery))
            {
                DrawSearchResults(searchQuery);
            }
            else
            {
                switch (selectedTab)
                {
                    case 0: // テクスチャ&色
                        DrawBasicTab();
                        break;
                    case 1: // ライト&影
                        DrawLightingTab();
                        break;
                    case 2: // エフェクト
                        DrawEffectsTab();
                        break;
                    case 3: // 環境&反射
                        DrawEnvironmentTab();
                        break;
                    case 4: // 詳細設定
                        DrawAdvancedTab();
                        break;
                }
            }

            // Save foldout states at the end of each OnGUI call.
            // This ensures that field values updated from DrawBoxedSection return values
            // are correctly persisted to EditorPrefs.
            SaveFoldoutStates();
        }
        catch (ExitGUIException)
        {
            throw; // ExitGUIException is used internally by Unity IMGUI - must not be caught
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"インスペクターの描画中にエラーが発生しました: {e.Message}", MessageType.Error);
            UnityEngine.Debug.LogException(e);
        }
    }

    private void SafeDrawSection(System.Action drawAction, string sectionName)
    {
        try
        {
            drawAction?.Invoke();
        }
        catch (ExitGUIException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"{sectionName}セクションの描画中にエラーが発生しました: {e.Message}", MessageType.Warning);
            UnityEngine.Debug.LogWarning($"[NataneToonShaderGUI] Error drawing {sectionName} section: {e.Message}");
        }
    }

    // ===== BOXED SECTION STYLES (Cached) =====
    private static GUIStyle _boxedSectionOuter;
    private static GUIStyle BoxedSectionOuter
    {
        get
        {
            if (_boxedSectionOuter == null)
            {
                _boxedSectionOuter = new GUIStyle(EditorStyles.helpBox);
                _boxedSectionOuter.padding = new RectOffset(0, 0, 0, 0);
                _boxedSectionOuter.margin = new RectOffset(0, 0, 2, 2);
            }
            return _boxedSectionOuter;
        }
    }

    private static GUIStyle _boxedHeaderFoldout;
    private static GUIStyle BoxedHeaderFoldout
    {
        get
        {
            if (_boxedHeaderFoldout == null)
            {
                _boxedHeaderFoldout = new GUIStyle(EditorStyles.foldoutHeader);
                _boxedHeaderFoldout.fontStyle = FontStyle.Bold;
                _boxedHeaderFoldout.fontSize = 12;
            }
            return _boxedHeaderFoldout;
        }
    }

    private static GUIStyle _badgeStyleOn;
    private static GUIStyle BadgeStyleOn
    {
        get
        {
            if (_badgeStyleOn == null)
            {
                _badgeStyleOn = new GUIStyle(EditorStyles.miniLabel);
                _badgeStyleOn.normal.textColor = Color.white;
                _badgeStyleOn.fontStyle = FontStyle.Bold;
                _badgeStyleOn.fontSize = 9;
                _badgeStyleOn.alignment = TextAnchor.MiddleCenter;
                _badgeStyleOn.padding = new RectOffset(4, 4, 1, 1);
            }
            return _badgeStyleOn;
        }
    }

    private static GUIStyle _badgeStyleOff;
    private static GUIStyle BadgeStyleOff
    {
        get
        {
            if (_badgeStyleOff == null)
            {
                _badgeStyleOff = new GUIStyle(EditorStyles.miniLabel);
                _badgeStyleOff.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                _badgeStyleOff.fontSize = 9;
                _badgeStyleOff.alignment = TextAnchor.MiddleCenter;
                _badgeStyleOff.padding = new RectOffset(4, 4, 1, 1);
            }
            return _badgeStyleOff;
        }
    }

    // ===== BOXED SECTION DRAWING =====

    /// <summary>
    /// Draw a boxed section with colored header, accent bar, and optional ON/OFF badge.
    /// lilToon boxOuter/boxInner pattern.
    /// Returns the foldout state.
    /// </summary>
    private bool DrawBoxedSection(string title, bool foldout, SectionCategory category, string toggleKeyword = null)
    {
        Color catColor = NataneToonShaderGUIStyles.GetSectionColor(category);
        bool isDark = EditorGUIUtility.isProSkin;

        // Outer box
        EditorGUILayout.Space(2);
        Rect outerRect = EditorGUILayout.BeginVertical(BoxedSectionOuter);

        // Reserve header space (single row, fixed height)
        Rect headerRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));

        // Draw header background + accent bar on Repaint
        if (Event.current.type == EventType.Repaint)
        {
            float bgAlpha = isDark ? 0.10f : 0.05f;
            Color headerBg = new Color(catColor.r, catColor.g, catColor.b, bgAlpha);
            EditorGUI.DrawRect(headerRect, headerBg);

            // Left accent bar (3px)
            Rect accentRect = new Rect(headerRect.x, headerRect.y, 3, headerRect.height);
            EditorGUI.DrawRect(accentRect, catColor);
        }

        // Handle click on entire header to toggle foldout
        if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
        {
            foldout = !foldout;
            Event.current.Use();
        }

        // Draw foldout arrow + title (non-interactive, just visual)
        Rect foldoutRect = new Rect(headerRect.x + 6, headerRect.y + 2, headerRect.width - 50, headerRect.height - 4);
        EditorGUI.Foldout(foldoutRect, foldout, title, true, BoxedHeaderFoldout);

        // ON/OFF badge (drawn at right side of header)
        if (!string.IsNullOrEmpty(toggleKeyword))
        {
            bool isEnabled = targetMaterial.IsKeywordEnabled(toggleKeyword);
            Rect badgeRect = new Rect(headerRect.xMax - 38, headerRect.y + 4, 32, 16);
            if (isEnabled)
            {
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
                GUI.Label(badgeRect, "ON", BadgeStyleOn);
                GUI.backgroundColor = oldBg;
            }
            else
            {
                GUI.Label(badgeRect, "OFF", BadgeStyleOff);
            }
        }

        EditorGUILayout.Space(2);

        if (foldout)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Space(2);
        }

        return foldout;
    }

    /// <summary>
    /// End a boxed section. Must be called after DrawBoxedSection.
    /// </summary>
    private void EndBoxedSection(bool wasExpanded)
    {
        if (wasExpanded)
        {
            GUILayout.Space(4);
            EditorGUILayout.EndVertical(); // content area
        }
        EditorGUILayout.EndVertical(); // outer box
    }

    private void DrawMainTextureSection()
    {
        showMainTexture = DrawBoxedSection("メインテクスチャ", showMainTexture, SectionCategory.Basic);
        if (showMainTexture)
        {
            DrawProperty("_MainTex", "メインテクスチャ");
            DrawColorProperty("_Color", "カラー");

            // Main Texture Animation
            EditorGUILayout.Space(5);
            bool mainTexAnim = DrawToggle("_MAIN_TEX_ANIMATION", "_MainTexAnimation", "メインテクスチャアニメーション");
            if (mainTexAnim)
            {
                DrawUVAnimationSettings("_MainTexScrollSpeed", "_MainTexRotateSpeed", "メインテクスチャ");
                DrawHelpToggle("MainTexAnimation",
                    "📌 メインテクスチャアニメーション:\n" +
                    "メインテクスチャのUV座標をスクロール・回転させます。\n\n" +
                    "• スクロール速度 XY: X方向とY方向のスクロール速度\n" +
                    "• 回転速度: UV座標の回転速度（ラジアン/秒）\n\n" +
                    "💡 流水表現やホログラムパターンの移動に使用できます。",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("カラー保持・強化設定", EditorStyles.boldLabel);

            DrawProperty("_AlbedoPreservation", "テクスチャカラー保持");
            DrawHelpToggle("AlbedoPreservation",
                "🎨 テクスチャカラー保持（改善版・色相維持）:\n" +
                "テクスチャの元の色を保持しながら、ライティングの明暗効果を適用します。\n" +
                "ライティングで「白くなる」のではなく「本来の色のまま明るくなる」表現を実現。\n\n" +
                "✨ 新機能:\n" +
                "　・黒などの暗い色が光で白くなる問題を解決\n" +
                "　・色相・彩度を保持したまま明るさだけを調整\n" +
                "　・スペキュラーやリムライトも色に応じて自然に\n\n" +
                "🎛️ 推奨設定:\n" +
                "• 0 = 従来のライティング（色が変わりやすい）\n" +
                "• 0.7-0.9 = 強い色保持（黒や暗い色も自然に）★推奨\n" +
                "• 1.0 = 完全な色保持（最も自然な色）\n\n" +
                "💡 効果:\n" +
                "　・黒い服が光で灰色/白っぽくならない\n" +
                "　・赤い服が光でピンク/白にならない\n" +
                "　・暗い色が本来の色相を保ったまま明るくなる\n" +
                "　・影やシェーディングの明暗は完全に維持\n" +
                "　・Light Volumeの白飛び問題も解決\n\n" +
                "⚠️ 注意:\n" +
                "値を上げすぎると、ライトカラーの影響が減ります。\n" +
                "青いライトで青く照らしたい場合は0.5程度に。",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_Saturation", "彩度");
            DrawHelpToggle("Saturation",
                "✨ 彩度調整:\n" +
                "最終カラーの彩度（色の鮮やかさ）を調整します。\n" +
                "• 0 = モノクロ（グレースケール）\n" +
                "• 1 = デフォルト（元の彩度）\n" +
                "• 1.5-2.0 = 鮮やかな色合い",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_Brightness", "全体明度");
            DrawHelpToggle("Brightness",
                "💡 全体明度調整:\n" +
                "最終的な明るさを微調整します。\n" +
                "• 0.5-0.9 = 暗めに\n" +
                "• 1.0 = デフォルト\n" +
                "• 1.1-1.5 = 明るめに",
                MessageType.Info);

            // 3.1 Parameter Interaction Warning: Color Preservation System
            float albedoPreservation = targetMaterial.GetFloat("_AlbedoPreservation");
            float saturation = targetMaterial.GetFloat("_Saturation");

            if (albedoPreservation > 0.7f && saturation > 1.3f)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ パラメータ相互作用の警告 / Parameter Interaction Warning\n\n" +
                    "「テクスチャカラー保持」と「彩度」が両方とも高い値です。\n" +
                    "色が非常に鮮やかになりすぎる可能性があります。\n\n" +
                    "推奨 / Recommended:\n" +
                    "• カラー保持 > 0.7 なら、彩度は 0.8-1.2 に\n" +
                    "• 彩度 > 1.3 なら、カラー保持は 0.3-0.6 に\n\n" +
                    "Both 'Albedo Preservation' and 'Saturation' are set to high values.\n" +
                    "This may result in overly vivid colors.\n\n" +
                    "• If Albedo Preservation > 0.7, set Saturation to 0.8-1.2\n" +
                    "• If Saturation > 1.3, set Albedo Preservation to 0.3-0.6",
                    MessageType.Warning);
            }

            // Final Color Blending Section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("最終カラーブレンディング", EditorStyles.boldLabel);
            DrawHelpToggle("FinalColorBlending",
                "🎨 最終なじませ処理（白飛び・黒つぶれ防止）:\n" +
                "すべてのエフェクト適用後の最終段階で、明るすぎる部分と暗すぎる部分を\n" +
                "周囲となじませて、より自然で滑らかな見た目にします。",
                MessageType.None);

            EditorGUILayout.Space();
            DrawProperty("_FinalHighlightBlend", "ハイライトなじませ（白飛び防止）");
            DrawProperty("_HighlightThreshold", "ハイライト閾値");
            DrawHelpToggle("HighlightBlend",
                "✨ ハイライトなじませ（トーンマッピング搭載）:\n" +
                "明るすぎる部分（ハイライト）を自然になじませます。\n\n" +
                "📊 処理フロー（4段階）:\n" +
                "1️⃣ スムーズショルダートーンマッピング\n" +
                "   　・輝度60%以上の明るい部分を自然に圧縮\n" +
                "   　・色の鮮やかさを保ちながら白飛びを防止\n" +
                "2️⃣ ハイライトブレンド（閾値以上）\n" +
                "   　・30%のデサチュレーションで過度な彩度を抑制\n" +
                "   　・境界を柔らかくスムーズに\n" +
                "3️⃣ ジェントルクランプ（1.05まで許容）\n" +
                "   　・ハードカットオフを回避\n\n" +
                "🎛️ パラメータ:\n" +
                "• なじませ 0 = 効果なし、1 = 最大圧縮\n" +
                "• 閾値: この値より明るい部分に追加ブレンド（推奨: 0.75）\n\n" +
                "💡 おすすめ設定:\n" +
                "　・通常: なじませ0.3-0.5、閾値0.75\n" +
                "　・強め: なじませ0.7-0.9、閾値0.6\n" +
                "　・最大: なじませ1.0、閾値0.5",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_FinalShadowBlend", "シャドーなじませ（黒つぶれ防止）");
            DrawProperty("_ShadowThreshold", "シャドー閾値");
            DrawHelpToggle("ShadowBlend",
                "🌙 シャドーなじませ:\n" +
                "暗すぎる部分（シャドー）を周囲となじませます。\n" +
                "• なじませ 0 = 効果なし、1 = 最大\n" +
                "• 閾値: この値より暗い部分に適用（推奨: 0.25）\n\n" +
                "💡 効果:\n" +
                "　・黒つぶれを防止し、ディテールを保持\n" +
                "　・影の境界を柔らかく\n" +
                "　・わずかにシャドーを持ち上げて自然に",
                MessageType.Info);

        }
        EndBoxedSection(showMainTexture);
    }

    private void DrawSurfaceFinishSection()
    {
        // Glossiness control
        DrawProperty("_Glossiness", "光沢度（グロッシネス）");
        DrawHelpToggle("Glossiness",
            "✨ 光沢度（Glossiness）:\n" +
            "スペキュラー、リフレクション、リムライトなどの\n" +
            "反射系エフェクトの強度を一括で調整します。\n\n" +
            "• 0 = 光沢なし（完全マット）\n" +
            "• 0.5 = 中程度の光沢\n" +
            "• 1.0 = 最大光沢（デフォルト）\n\n" +
            "💡 適用されるエフェクト:\n" +
            "　・スペキュラーハイライト\n" +
            "　・リムライト（1 & 2）\n" +
            "　・環境リム\n" +
            "　・MatCap\n" +
            "　・キューブマップリフレクション\n" +
            "　・Light Volume Specular\n\n" +
            "※ Emission（発光）には影響しません",
            MessageType.Info);

        EditorGUILayout.Space();

        // Matte Effect (additional reduction)
        DrawProperty("_MatteEffect", "マット効果（追加の光沢抑制）");
        DrawHelpToggle("MatteEffect",
            "🎨 マット効果:\n" +
            "光沢度に加えて、さらに光沢を減らすための\n" +
            "追加パラメータです。\n\n" +
            "• 0 = 光沢度のみで制御\n" +
            "• 0.5 = 光沢度の50%に減少\n" +
            "• 1.0 = 完全にマット（光沢ゼロ）\n\n" +
            "💡 使い方:\n" +
            "光沢度と組み合わせて、より細かい調整が可能です。\n" +
            "例: 光沢度0.8 × マット効果0.3 = 実質56%の光沢",
            MessageType.Info);
    }

    private void DrawMakeupTexturesSection()
    {
        showMakeupTextures = DrawBoxedSection("追加テクスチャ（2nd〜5th）", showMakeupTextures, SectionCategory.Basic);
        if (showMakeupTextures)
        {
            // Compact ON/OFF summary for all 4 layers
            EditorGUILayout.BeginHorizontal();
            string[] layerNames = { "2nd", "3rd", "4th", "5th" };
            string[] layerKeywords = { "_2ND_TEXTURE", "_3RD_TEXTURE", "_4TH_TEXTURE", "_5TH_TEXTURE" };
            for (int i = 0; i < layerNames.Length; i++)
            {
                bool layerOn = targetMaterial.IsKeywordEnabled(layerKeywords[i]);
                Color badgeCol = layerOn ? new Color(0.2f, 0.7f, 0.3f, 0.9f) : new Color(0.4f, 0.4f, 0.4f, 0.4f);
                Color textCol = layerOn ? Color.white : new Color(0.6f, 0.6f, 0.6f);
                string label = $"{layerNames[i]}:{(layerOn ? "ON" : "OFF")}";
                Rect r = GUILayoutUtility.GetRect(new GUIContent(label), EditorStyles.miniLabel, GUILayout.Height(18));
                if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(r, badgeCol);
                var oc = GUI.contentColor; GUI.contentColor = textCol;
                GUI.Label(r, label, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = layerOn ? FontStyle.Bold : FontStyle.Normal });
                GUI.contentColor = oc;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            DrawHelpToggle("MakeupTextures",
                "メイクアップテクスチャ（透過PNG対応・HSVカラー調整）\n" +
                "最大4つの追加テクスチャで合成。アルファチャンネルで適用範囲を制御。",
                MessageType.None);

            EditorGUILayout.Space(5);

            // Draw each makeup texture layer using helper method
            DrawMakeupTextureLayer("2nd", "2ND_TEXTURE", "2ND_TEX_MASK", "ハイライト、アイシャドウ");
            EditorGUILayout.Space(10);

            DrawMakeupTextureLayer("3rd", "3RD_TEXTURE", "3RD_TEX_MASK", "チーク");
            EditorGUILayout.Space(10);

            DrawMakeupTextureLayer("4th", "4TH_TEXTURE", "4TH_TEX_MASK", "グリッター、シャドウ");
            EditorGUILayout.Space(10);

            DrawMakeupTextureLayer("5th", "5TH_TEXTURE", "5TH_TEX_MASK", "細部のハイライト、細部のシャドウ");
        }
        EndBoxedSection(showMakeupTextures);
    }

    /// <summary>
    /// Helper method to draw a single makeup texture layer with all its properties
    /// Reduces code duplication for 2nd, 3rd, 4th, 5th textures
    /// </summary>
    private void DrawMakeupTextureLayer(string layerName, string textureKeyword, string maskKeyword, string usageHint)
    {
        bool useTexture = DrawToggle($"_{textureKeyword}", $"_Use{layerName}Texture", $"{layerName} Textureを有効化");

        if (useTexture)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"{layerName} Texture設定", EditorStyles.boldLabel);

            // Texture and HSV controls
            DrawProperty($"_{layerName}Tex", $"{layerName} Texture");
            DrawProperty($"_{layerName}TexHueShift", "Hue Shift");
            DrawProperty($"_{layerName}TexSaturation", "Saturation");
            DrawProperty($"_{layerName}TexValue", "Brightness");

            DrawHelpToggle("MakeupTextureHSV",
                "💡 HSVカラー調整:\n" +
                "• Hue=0, Sat=1, Value=1 = テクスチャそのまま\n" +
                "• Hue Shift = 色相を変更（-0.5～0.5）\n" +
                "• Saturation = 彩度調整（0～2、1=元の彩度）\n" +
                "• Brightness = 明度調整（0～2、1=元の明度）",
                MessageType.None);

            // Intensity and blend mode
            DrawProperty($"_{layerName}TexIntensity", "強度");
            DrawProperty($"_{layerName}TexBlendMode", "ブレンドモード");

            DrawHelpToggle("MakeupTextureBlendMode",
                $"ブレンドモード:\n" +
                "• Add: 加算（ハイライトに最適）\n" +
                $"• Multiply: 乗算（{usageHint}に最適）\n" +
                "• Overlay: オーバーレイ（自然なメイク）\n" +
                "• Screen: スクリーン（柔らかいハイライト）\n\n" +
                "💡 透過PNG対応：\n" +
                "テクスチャのアルファチャンネルが適用強度として使用されます",
                MessageType.Info);

            // Optional mask
            EditorGUILayout.Space();
            DrawProperty($"_{layerName}TexMask", $"{layerName} Texマスク");
            DrawHelpToggle("MakeupTextureMask",
                "白 = テクスチャ適用、黒 = 適用なし\n" +
                "マスクはアルファチャンネルと乗算されます",
                MessageType.Info);

            // UV Animation
            DrawUVAnimationSettings($"_{layerName}TexScrollSpeed", $"_{layerName}TexRotateSpeed", $"{layerName} Texture");
        }
    }

    private void DrawShadingSection()
    {
        showShading = DrawBoxedSection("トゥーンシェーディング", showShading, SectionCategory.Shading);
        if (showShading)
        {
            // Delegate content to helper class for better code organization
            // ヘルパークラスにコンテンツ描画を委譲（コード整理のため）
            NataneToonShaderGUIHelpers.DrawShadingSectionContent(
                properties,
                DrawToggle,
                DrawProperty,
                DrawHelpToggle,
                FindProperty
            );
        }
        EndBoxedSection(showShading);
    }

    private void DrawAdvancedLightingSection()
    {
        showAdvancedLighting = DrawBoxedSection("ライティング詳細", showAdvancedLighting, SectionCategory.Lighting);
        if (showAdvancedLighting)
        {

            // Soft Lighting Mode
            EditorGUILayout.LabelField("ソフトライティングモード", EditorStyles.boldLabel);
            bool softLightingMode = DrawToggle("_SOFT_LIGHTING_MODE", "_SoftLightingMode", "ソフトライティングモード（グローバル）");
            if (softLightingMode)
            {
                DrawProperty("_SoftLightingIntensity", "ソフトライティング強度");
                DrawHelpToggle("SoftLightingMode",
                    "🌟 ソフトライティングモード:\n" +
                    "すべてのライティングとシェーディングを柔らかくします。\n" +
                    "このモードを有効にすると、影の境界、ライトの強さ、\n" +
                    "ハイライトなどが全体的に柔らかく調整されます。\n\n" +
                    "• 強度 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
                    "• 強度 0.5-0.7 = かなり柔らかい\n" +
                    "• 強度 0.7-1.0 = 非常に柔らかい\n\n" +
                    "💡 イラスト調や水彩画風の柔らかな表現に最適です。",
                    MessageType.Info);
                EditorGUILayout.Space();
            }

            // Global Light Controls
            EditorGUILayout.LabelField("グローバルライト制御", EditorStyles.boldLabel);
            DrawProperty("_LightIntensity", "ライト強度（グローバル）");
            DrawHelpToggle("LightIntensity", "全体的なライティングの強さを制御します。0 = ライトなし、1 = 標準、2 = 明るい", MessageType.Info);

            DrawProperty("_IndirectLightIntensity", "間接光の強度");
            DrawHelpToggle("IndirectLightIntensity", "環境光やライトプローブからの間接照明の強さを制御します。", MessageType.Info);

            DrawProperty("_GIIntensity", "GI強度（環境反射）");
            DrawHelpToggle("GIIntensity", "環境反射（Light Probes/GI）の影響度を制御します。\n• 0 = 環境反射を完全に無効化（環境光の影響を受けない）\n• 0.5 = 環境反射を50%に軽減\n• 1 = 通常通り環境反射を適用\nVRChatで暗いワールドやライティングが強すぎるワールドで、見た目を安定させるために使用します。", MessageType.Info);

            // Indirect Lighting Controls
            DrawColorProperty("_IndirectLightMinColor", "間接光の最低色");
            DrawHelpToggle("IndirectLightMinColor", "間接光の最低保証カラーです。\n暗いワールドでもキャラクターが真っ黒にならないよう、間接光の下限を設定します。\n• 黒 (0,0,0) = 制限なし（環境光に完全依存）\n• 暗いグレー = 最低限の明るさを保証\nNatural ライティングパイプラインで使用されます。", MessageType.Info);

            DrawProperty("_ShadowEnvStrength", "影への環境色反映");
            DrawHelpToggle("ShadowEnvStrength", "影の色に環境光の色をどの程度反映するかを制御します。\n• 0 = 影は純粋に暗くなるだけ（従来の挙動）\n• 1 = 影に環境光の色が完全に反映される\n環境光の色味を影にも反映させることで、より自然なライティングを実現します。", MessageType.Info);

            DrawProperty("_LightColorInfluence", "ライトカラー影響度");
            DrawHelpToggle("LightColorInfluence", "ライトの色がマテリアルに与える影響を制御します。\n• 0 = ライトの色を無視（白色光として処理）\n• 1 = ライトの色を完全に反映\n• 0.5 = 中間（推奨）", MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("シャドウ設定", EditorStyles.boldLabel);
            DrawProperty("_ShadowReceive", "影の受け取り");
            DrawHelpToggle("ShadowReceive", "他のオブジェクトからの影がこのマテリアルに与える影響を制御します。1 = 完全な影、0 = 影なし。", MessageType.Info);

            DrawProperty("_ShadowSmoothing", "影のスムージング");
            DrawHelpToggle("ShadowSmoothing",
                "シャドウスムージング:\n" +
                "影のジャギーや諧調をなじませます。\n\n" +
                "【多段階トゥーンシェーディング】\n" +
                "ステップ境界の諧調を連続的なグラデーションに\n" +
                "ブレンドし、段差を目立たなくします。\n\n" +
                "【シャドウマップ（ディレクショナルライト）】\n" +
                "PCF 9タップで影エッジを滑らかにします。\n\n" +
                "【シャドウマップ（ポイント/スポットライト）】\n" +
                "適応型スムージングで影エッジをぼかします。\n\n" +
                "• 0 = スムージングなし（デフォルト）\n" +
                "• 0.1-0.3 = 軽いスムージング（推奨）\n" +
                "• 0.5-1.0 = 強いスムージング\n\n" +
                "多段階影の諧調が気になる場合や、影エッジが\n" +
                "ギザギザな場合に使用してください。",
                MessageType.Info);

            DrawProperty("_ShadowMaxDarkness", "影の最大暗さ");
            DrawHelpToggle("ShadowMaxDarkness", "影の最小明るさです。0 = 完全に暗い、1 = 暗くならない。影が真っ黒になりすぎるのを防ぎます。", MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ライト影響範囲", EditorStyles.boldLabel);
            DrawProperty("_LightMinInfluence", "ライトの最小影響");
            DrawProperty("_LightMaxInfluence", "ライトの最大影響");
            DrawHelpToggle("LightInfluenceRange", "最小/最大で明るさの範囲を制御します。最小値は暗くなりすぎを防ぎ、最大値は露出オーバーを防ぎます。", MessageType.Info);

            EditorGUILayout.Space();
            DrawBlendParameter(
                "_LightBlend",
                "ライトのなじませ（柔らかさ）",
                "✨ ライトのなじませ調整:\n" +
                "ライティングの変化を周囲となじませて、より滑らかにします。\n" +
                "• 0 = シャープな変化（デフォルト）\n" +
                "• 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
                "• 0.7-1.0 = 非常に柔らかい変化\n\n" +
                "💡 ライトの強弱が急激すぎる場合に調整してください。");

            EditorGUILayout.Space();
            DrawBlendParameter(
                "_HighlightSoftness",
                "ハイライトの柔らかさ",
                "✨ ハイライトのなじませ調整:\n" +
                "明るい部分（ハイライト）を周囲となじませます。\n" +
                "• 0 = シャープなハイライト（デフォルト）\n" +
                "• 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
                "• 0.7-1.0 = 非常に柔らかいハイライト\n\n" +
                "💡 ハイライトが強すぎる場合や、\n" +
                "より柔らかい印象が欲しい場合に調整してください。");

            // 3.1 Parameter Interaction Warning: Edge Softness System
            float shadowBlend = targetMaterial.GetFloat("_ShadowBlend");
            float lightBlend = targetMaterial.GetFloat("_LightBlend");
            float highlightSoftness = targetMaterial.GetFloat("_HighlightSoftness");

            int softnessCount = 0;
            if (shadowBlend > 0.5f) softnessCount++;
            if (lightBlend > 0.5f) softnessCount++;
            if (highlightSoftness > 0.5f) softnessCount++;

            if (softnessCount >= 3)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ 過度なソフトネス警告 / Excessive Softness Warning\n\n" +
                    "複数のソフトネスパラメータが高い値です。\n" +
                    "シェーディングが非常にぼやけます。\n\n" +
                    "推奨 / Recommended:\n" +
                    "メインパラメータ1-2個を選び、\n" +
                    "他は 0.3 以下に抑えてください。\n\n" +
                    "Multiple softness parameters are set to high values.\n" +
                    "Shading will become very blurry.\n\n" +
                    "Choose 1-2 main parameters and keep others below 0.3.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
            DrawProperty("_BacklightIntensity", "逆光の強さ");
            if (targetMaterial.GetFloat("_BacklightIntensity") > 0)
            {
                DrawColorProperty("_BacklightColor", "逆光の色");
                DrawHelpToggle("Backlight", "逆光はオブジェクトの背後に光がある時に照明を追加し、リムライトのような効果を作ります。", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_BacklightBlend", "_BacklightBlendMode", "_BacklightBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_BacklightDistFade", "距離フェード強度");
                }
            }

            EditorGUILayout.Space();
            DrawProperty("_AdditionalLightIntensity", "追加ライトの強さ");
            DrawHelpToggle("AdditionalLight", "追加ライト（ForwardAddパス）の強度を制御します。低い値は複数のライトを使用する際の明るくなりすぎを防ぎます。0 = 追加ライトなし、1 = 最大強度。", MessageType.Info);

            // --- Vertex Light Settings ---
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("頂点ライト設定", EditorStyles.boldLabel);

            bool enablePixelVertexLights = DrawToggle("_PIXEL_VERTEX_LIGHTS", "_UsePixelVertexLights", "ピクセル精度の頂点ライト");
            DrawHelpToggle("PixelVertexLights",
                "頂点ライト（最大4灯の追加Point/Spotライト）をフラグメントシェーダーでトゥーンスタイルに計算します。\n" +
                "品質は向上しますが、負荷が増加します。\n" +
                "オフの場合でも頂点ライトは通常精度（頂点補間）で処理されます。\n" +
                "※ ForwardBaseパスでのみ有効です。",
                MessageType.Info);
        }
        EndBoxedSection(showAdvancedLighting);
    }

    private void DrawLightVolumeSection()
    {
        showLightVolume = DrawBoxedSection("VRC ライトボリューム", showLightVolume, SectionCategory.Lighting, "_USE_LIGHT_VOLUME");
        if (showLightVolume)
        {

            // Show package detection status
            bool packageInstalled = NataneToon.Editor.VRCLightVolumesAutoDetector.IsPackageInstalled();
            if (packageInstalled)
            {
                EditorGUILayout.HelpBox(
                    "VRC Light Volumes パッケージ: 検出済み\n" +
                    "LightVolumes.cginc を使用します（対応ワールドで自動的に動作）",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "VRC Light Volumes パッケージ: 未検出\n" +
                    "バンドル版LightVolumes.cgincで全機能が利用可能です\n\n" +
                    "パッケージ版を使用する場合:\n" +
                    "VCC から red.sim.lightvolumes をインストールしてください\n" +
                    "https://redsim.github.io/vpmlisting/",
                    MessageType.Info);
            }

            EditorGUILayout.Space(3);

            bool enableLightVolume = DrawToggle("_USE_LIGHT_VOLUME", "_UseLightVolume", "Light Volumeを有効化");

            if (enableLightVolume)
            {
                EditorGUI.indentLevel++;
                DrawHelpToggle("LightVolumeIntro",
                    "VRC Light Volumesはボクセルベースの次世代ライティングシステムです。\n" +
                    "対応ワールドで自動的に高品質な部分照明が適用されます。\n" +
                    (packageInstalled
                        ? "現在、本物のLightVolumes.cgincが使用されています。"
                        : "パッケージ未検出のため、バンドル版LightVolumes.cgincが使用されます。"),
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_LightVolumeIntensity", "Light Volumeの強さ");
                DrawHelpToggle("LightVolumeIntensity", "Light Volumeライティングの強度を制御します。1 = 完全強度、0 = 無効。", MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_LightVolumeBlendMode", "ブレンドモード");
                DrawHelpToggle("LightVolumeBlendMode",
                    "Light Volumeブレンドモード:\n" +
                    "• Add(0): 直接光をアンビエントに、間接光をリムライトとして統合\n" +
                    "  　　　（デフォルト、最も自然）\n" +
                    "• Multiply(1): 既存のライティングと乗算（暗くなる効果）\n" +
                    "• Replace(2): Light Volumeで置き換え（完全に制御）\n" +
                    "• Natural(3): 間接光として max() 合成に参加させます。\n" +
                    "  　環境光の色がすべてのメッシュに正しく反映されます（推奨）\n\n" +
                    "Addモード:\n" +
                    "　Light Volumeの直接光と間接光を分離。\n" +
                    "　間接光は自然なリム効果として統合され、より立体的な表現に。\n" +
                    "　Shadow Receive Maskで間接光の影響を制御できます。\n\n" +
                    "Naturalモード:\n" +
                    "　Light Volumeの照明を間接光の最低色と max() で合成。\n" +
                    "　環境色が全メッシュに均一に反映され、色味の統一感が向上します。",
                    MessageType.Info);

                EditorGUILayout.Space();
                bool enableSpecular = DrawToggle("_LIGHT_VOLUME_SPECULAR", "_LightVolumeSpecular", "Light Volume スペキュラー");
                if (enableSpecular)
                {
                    DrawHelpToggle("LightVolumeSpecular",
                        "Light Volumeからカラースペキュラーを生成します。\n" +
                        (packageInstalled
                            ? "本物のLightVolumeSpecular関数が使用されます。アバターに推奨。"
                            : "バンドル版LightVolumeSpecularが使用されます。"),
                        MessageType.Info);
                }

                EditorGUILayout.Space();
                DrawHelpToggle("LightVolumeNotes",
                    "注意：\n" +
                    "• ワールドとアバター両方が対応している必要があります\n" +
                    "• 非対応環境では自動的にUnityのライトプローブにフォールバックします\n" +
                    "• ハッシュタグ #VRCLightVolumesReady で対応ワールドを検索できます\n" +
                    (packageInstalled ? "" : "• Tools > Natane > VRChat > VRC Light Volumes 再検出 で手動検出も可能です"),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_LightVolumeBlend");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showLightVolume);
    }

    private void DrawLTCGISection()
    {
        showLTCGI = DrawBoxedSection("LTCGI（リアルタイムエリアライト）", showLTCGI, SectionCategory.Lighting, "_LTCGI");
        if (showLTCGI)
        {

            // Show package detection status
            bool packageInstalled = NataneToon.Editor.LTCGIAutoDetector.IsPackageInstalled();
            if (packageInstalled)
            {
                EditorGUILayout.HelpBox(
                    "LTCGI パッケージ: 検出済み\n" +
                    "LTCGI.cginc を使用します（対応ワールドで自動的に動作）",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "LTCGI パッケージ: 未検出（フォールバック使用中）\n" +
                    "Unity SH + Reflection Probes による近似ライティングを使用します\n" +
                    "本物のLTCGIを使用するには: VCC から at.pimaker.ltcgi をインストール",
                    MessageType.Info);
            }

            EditorGUILayout.Space(3);

            bool enableLTCGI = DrawToggle("_LTCGI", "_LTCGI", "LTCGIを有効化");

            if (enableLTCGI)
            {
                EditorGUI.indentLevel++;

                if (packageInstalled)
                {
                    DrawHelpToggle("LTCGIIntro",
                        "LTCGI は Linearly Transformed Cosines によるリアルタイムエリアライトシステムです。\n" +
                        "対応ワールドのスクリーンやエリアライトから自動的に照明を受けます。",
                        MessageType.Info);
                }
                else
                {
                    DrawHelpToggle("LTCGIIntro",
                        "フォールバックモード:\n" +
                        "LTCGIパッケージが未インストールのため、Unity SH + Reflection Probes による近似実装を使用しています。\n" +
                        "エリアライトの局所性は再現されませんが、環境光ベースの照明寄与が得られます。\n\n" +
                        "本物のLTCGIを使用するには at.pimaker.ltcgi をインストールしてください。",
                        MessageType.Info);
                }

                EditorGUILayout.Space();
                DrawProperty("_LTCGIIntensity", "LTCGI 強度");
                DrawHelpToggle("LTCGIIntensity", "LTCGIライティングの全体的な強度を制御します。\n1 = 完全強度、0 = 無効。", MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_LTCGISpecular", "LTCGI スペキュラー");
                DrawHelpToggle("LTCGISpecular", "LTCGIからのスペキュラー（反射光）の強度を制御します。\nエリアライトの映り込み表現に使用されます。", MessageType.Info);

                EditorGUILayout.Space();
                DrawHelpToggle("LTCGINotes",
                    "注意：\n" +
                    "• ワールドにLTCGIが設定されている必要があります\n" +
                    "• LTCGIはスクリーン、エリアライト等のリアルタイム照明に対応\n" +
                    "• AudioLink対応ワールドでは音楽連動照明も可能\n" +
                    "• Tools > Natane > VRChat > LTCGI 再検出 で手動検出も可能です",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_LTCGIBlend", "_LTCGIBlendMode");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showLTCGI);
    }

    private void DrawSpecularSection()
    {
        showSpecular = DrawBoxedSection("スペキュラー反射", showSpecular, SectionCategory.Effects, "_SPECULAR");
        if (showSpecular)
        {

            bool enableSpecular = DrawToggle("_SPECULAR", "_Specular", "スペキュラーを有効化");

            if (enableSpecular)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_SpecularColor", "スペキュラーの色");
                DrawProperty("_SpecularSize", "スペキュラーのサイズ");
                DrawProperty("_SpecularSoftness", "スペキュラーの柔らかさ");

                EditorGUILayout.Space();
                DrawProperty("_SpecularMask", "スペキュラーマスク");
                DrawHelpToggle("SpecularMask", "白 = スペキュラーあり、黒 = スペキュラーなし", MessageType.Info);
                DrawUVAnimationSettings("_SpecularMaskScrollSpeed", "_SpecularMaskRotateSpeed", "スペキュラーマスク");

                DrawBlendControls(materialEditor, targetMaterial, "_SpecularBlend", "_SpecularBlendMode", "_SpecularBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_SpecularDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showSpecular);
    }

    private void DrawHairSpecularSection()
    {
        showHairSpecular = DrawBoxedSection("ヘアハイライト（Kajiya-Kay）", showHairSpecular, SectionCategory.Effects, "_HAIR_SPECULAR");
        if (showHairSpecular)
        {

            bool enableHairSpec = DrawToggle("_HAIR_SPECULAR", "_HairSpecular", "ヘアスペキュラーを有効化");

            if (enableHairSpec)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("プライマリローブ", EditorStyles.boldLabel);
                DrawColorProperty("_HairSpecColor1", "プライマリスペキュラー色");
                DrawProperty("_HairSpecShift1", "プライマリタンジェントシフト");
                DrawProperty("_HairSpecWidth1", "プライマリスペキュラー幅");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("セカンダリローブ", EditorStyles.boldLabel);
                DrawColorProperty("_HairSpecColor2", "セカンダリスペキュラー色");
                DrawProperty("_HairSpecShift2", "セカンダリタンジェントシフト");
                DrawProperty("_HairSpecWidth2", "セカンダリスペキュラー幅");

                EditorGUILayout.Space(5);
                DrawProperty("_HairSpecIntensity", "ヘアスペキュラー強度");

                EditorGUILayout.Space(5);
                DrawProperty("_HairSpecMask", "ヘアスペキュラーマスク");
                DrawProperty("_HairSpecShiftTex", "シフトテクスチャ");

                DrawBlendControls(materialEditor, targetMaterial, "_HairSpecBlend", "_HairSpecBlendMode", null);

                DrawHelpToggle("HairSpecular",
                    "💇 ヘアスペキュラー（Kajiya-Kay）:\n" +
                    "髪の毛専用の異方性スペキュラーハイライトです。\n" +
                    "「天使の輪」（エンジェルリング）効果を再現します。\n\n" +
                    "• プライマリローブ: メインのハイライト\n" +
                    "• セカンダリローブ: 補助的なハイライト\n" +
                    "• タンジェントシフト: ハイライトの位置をずらす\n" +
                    "• シフトテクスチャ: ピクセルごとのシフト制御\n\n" +
                    "💡 使い方:\n" +
                    "髪のマテリアルに使用し、光源に応じたリアルな\n" +
                    "髪のハイライトを表現します。",
                    MessageType.Info);

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_HairSpecDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showHairSpecular);
    }

    private void DrawRimLightSection()
    {
        showRimLight = DrawBoxedSection("リムライト（輪郭光）", showRimLight, SectionCategory.Effects, "_RIM_LIGHT");
        if (showRimLight)
        {

            bool enableRimLight = DrawToggle("_RIM_LIGHT", "_RimLight", "リムライトを有効化");

            if (enableRimLight)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_RimColor", "リムライトの色");
                DrawProperty("_RimPower", "リムライトのパワー");
                DrawProperty("_RimIntensity", "リムライトの強さ");

                EditorGUILayout.Space();
                DrawProperty("_RimSpread", "リムの広がり（グロー）");
                DrawHelpToggle("RimSpread",
                    "✨ リムの広がり（グロー効果）:\n" +
                    "リムライトを滲ませて広げ、柔らかく発光している\n" +
                    "ような効果を追加します。\n\n" +
                    "• 0 = 広がりなし（シャープなリム）\n" +
                    "• 0.3-0.5 = 適度な広がり（推奨）\n" +
                    "• 0.7-1.0 = 大きな広がり（強いグロー）",
                    MessageType.None);

                EditorGUILayout.Space();
                DrawProperty("_RimMask", "リムマスク");
                DrawHelpToggle("RimMask", "白 = リムライトあり、黒 = リムライトなし", MessageType.Info);
                DrawUVAnimationSettings("_RimMaskScrollSpeed", "_RimMaskRotateSpeed", "リムマスク");

                DrawBlendControls(materialEditor, targetMaterial, "_RimBlend", "_RimBlendMode", "_RimBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_RimDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Rim Light 2
            bool enableRimLight2 = DrawToggle("_RIM_LIGHT_2", "_RimLight2", "リムライト2を有効化（多段階リム）");

            if (enableRimLight2)
            {
                EditorGUI.indentLevel++;
                DrawHelpToggle("RimLight2",
                    "🌟 多段階リムライト:\n" +
                    "2つのリムライトレイヤーを重ねることで、\n" +
                    "より複雑で立体的なリムライト表現が可能です。\n\n" +
                    "💡 おすすめ設定:\n" +
                    "リムライト1: 明るい色、低いパワー（内側の輪郭）\n" +
                    "リムライト2: 淡い色、高いパワー（外側の輪郭）",
                    MessageType.None);

                DrawColorProperty("_RimColor2", "リムライト2の色");
                DrawProperty("_RimPower2", "リムライト2のパワー");
                DrawProperty("_RimIntensity2", "リムライト2の強さ");

                EditorGUILayout.Space();
                DrawProperty("_RimSpread2", "リムの広がり2（グロー）");
                DrawHelpToggle("RimSpread2",
                    "✨ リムの広がり（グロー効果）:\n" +
                    "リムライト2を滲ませて広げます。",
                    MessageType.None);

                EditorGUILayout.Space();
                DrawProperty("_RimMask2", "リムマスク2");
                DrawHelpToggle("RimMask2", "白 = リムライトあり、黒 = リムライトなし", MessageType.Info);
                DrawUVAnimationSettings("_RimMask2ScrollSpeed", "_RimMask2RotateSpeed", "リムマスク2");

                DrawBlendControls(materialEditor, targetMaterial, "_RimBlend2", "_RimBlendMode2", "_Rim2Blur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_Rim2DistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Offset Rim Light
            bool enableOffsetRimLight = DrawToggle("_OFFSET_RIM_LIGHT", "_OffsetRimLight", "オフセットリムライトを有効化");

            if (enableOffsetRimLight)
            {
                EditorGUI.indentLevel++;
                DrawHelpToggle("OffsetRimLight",
                    "🌙 オフセットリムライト:\n" +
                    "通常のリムライトとは異なり、リムの発生位置を\n" +
                    "特定の方向にオフセット（偏移）させます。\n" +
                    "2Dアニメのような指向性のあるハイライト表現が可能です。\n\n" +
                    "💡 おすすめ設定:\n" +
                    "• X=0.3, Y=0.1: 右上からの光を表現\n" +
                    "• ライト方向連動ON: 自動的にライトに追従\n" +
                    "• シャープネス=0.5: トゥーン調のくっきりリム",
                    MessageType.None);

                DrawColorProperty("_OffsetRimColor", "オフセットリムカラー");
                DrawProperty("_OffsetRimPower", "パワー（幅）");
                DrawProperty("_OffsetRimIntensity", "強度");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("オフセット方向", EditorStyles.boldLabel);
                DrawProperty("_OffsetRimOffsetX", "X方向オフセット");
                DrawProperty("_OffsetRimOffsetY", "Y方向オフセット");
                DrawHelpToggle("OffsetRimDir",
                    "📐 オフセット方向:\n" +
                    "ビュー空間でリムの発生位置をずらします。\n" +
                    "• X正 = 右方向にリム, X負 = 左方向\n" +
                    "• Y正 = 上方向にリム, Y負 = 下方向\n" +
                    "• 例: X=0.3, Y=0 で右側にリムが偏る",
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_OffsetRimUseLightDir", "ライト方向連動");
                DrawProperty("_OffsetRimLightDirStrength", "ライト方向の強さ");
                DrawHelpToggle("OffsetRimLightDir",
                    "💡 ライト方向連動:\n" +
                    "ONにすると、メインライトの方向に基づいて\n" +
                    "自動的にリムのオフセットが計算されます。\n" +
                    "手動オフセットと併用可能です。",
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_OffsetRimSharpness", "シャープネス（トゥーン）");
                DrawHelpToggle("OffsetRimSharpness",
                    "✂ シャープネス:\n" +
                    "• 0 = 柔らかいグラデーション\n" +
                    "• 0.3-0.5 = 適度なトゥーン調（推奨）\n" +
                    "• 0.8-1.0 = くっきりセル調",
                    MessageType.Info);

                DrawProperty("_OffsetRimShadowMask", "影マスク強度");
                DrawHelpToggle("OffsetRimShadowMask",
                    "🌑 影マスク:\n" +
                    "影になっている部分のリムを抑制します。\n" +
                    "• 0 = 影でもリムが出る\n" +
                    "• 0.5 = 影部分で半減（推奨）\n" +
                    "• 1.0 = 影部分でリムが完全に消える",
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_OffsetRimMask", "マスクテクスチャ");
                DrawHelpToggle("OffsetRimMask", "白 = リムあり、黒 = リムなし", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_OffsetRimBlend", "_OffsetRimBlendMode", "_OffsetRimBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_OffsetRimDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Rim Light Direction Masking (lilToon-style)
            DrawProperty("_RimDirStrength", "ライト方向追従");
            DrawProperty("_RimShadowMask", "影マスク");
            DrawHelpToggle("RimDirMasking",
                "💡 リムライト方向制御:\n" +
                "• ライト方向追従: ライトの方向にリムを追従させる強度\n" +
                "  0=全方向にリム、1=ライト側のみリム\n" +
                "• 影マスク: 影の部分でリムを抑制する強度\n" +
                "  0=影でもリム表示、1=影でリム消失\n\n" +
                "※ ポイントライト・スポットライトにも対応",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Rim Direction Control (manual)
            bool enableRimDirControl = DrawToggle("_RIM_DIRECTION_CONTROL", "_RimDirectionControl", "手動方向制御を有効化");

            if (enableRimDirControl)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_RimLightDirection", "リムライト方向");
                DrawProperty("_RimDirectionRange", "方向範囲");
                DrawHelpToggle("RimDirectionControl",
                    "🧭 手動リム方向制御:\n" +
                    "リムライトの発生方向を手動で指定して制限します。\n" +
                    "ライトの位置に関係なく、特定の方向からのみ\n" +
                    "リムが見えるように制御します。\n\n" +
                    "• リムライト方向: リムを発生させる方向ベクトル\n" +
                    "• 方向範囲: 許容する角度の広さ（0=狭い、1=広い）",
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showRimLight);
    }

    private void DrawSSSSection()
    {
        showSSS = DrawBoxedSection("半透明表現（SSS）", showSSS, SectionCategory.Effects, "_SSS");
        if (showSSS)
        {

            bool enableSSS = DrawToggle("_SSS", "_SSS", "SSSを有効化");

            if (enableSSS)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_SSSColor", "SSSの色");
                DrawProperty("_SSSIntensity", "SSSの強さ");
                DrawProperty("_SSSPower", "SSSのパワー");
                DrawProperty("_SSSDistortion", "SSSの歪み");

                EditorGUILayout.Space();
                DrawProperty("_ThicknessMap", "厚さマップ");
                DrawHelpToggle("ThicknessMap", "白 = 薄い（SSSが強い）、黒 = 厚い（SSSが弱い）", MessageType.Info);

                DrawProperty("_ThicknessScale", "厚さのスケール");

                EditorGUILayout.Space();
                DrawProperty("_SSSMask", "SSSマスク");
                DrawHelpToggle("SSSMask", "白 = SSSあり、黒 = SSSなし", MessageType.Info);

                DrawHelpToggle("SSSInfo", "SSSはオブジェクトを通過する光をシミュレートします。肌、葉、薄い素材に最適です。", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_SSSBlend", "_SSSBlendMode", "_SSSBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_SSSDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showSSS);
    }

    private void DrawMatCapSection()
    {
        showMatCap = DrawBoxedSection("マットキャップ（MatCap）", showMatCap, SectionCategory.Effects, "_MATCAP");
        if (showMatCap)
        {

            bool enableMatCap = DrawToggle("_MATCAP", "_MatCap", "MatCapを有効化");

            if (enableMatCap)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_MatCapTex", "MatCapテクスチャ");
                DrawProperty("_MatCapIntensity", "MatCapの強さ");
                DrawProperty("_MatCapBlendMode", "MatCapのブレンドモード");

                EditorGUILayout.Space();
                DrawProperty("_MatCapMask", "MatCapマスク");
                DrawHelpToggle("MatCapMask", "白 = MatCapあり、黒 = MatCapなし", MessageType.Info);

                DrawHelpToggle("MatCapInfo", "MatCapテクスチャは球面反射マップである必要があります。", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_MatCapBlend", null, "_MatCapBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_MatCapDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // MatCap 2
            bool enableMatCap2 = DrawToggle("_MATCAP_2", "_MatCap2", "MatCap 2を有効化");

            if (enableMatCap2)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_MatCapTex2", "MatCap 2 テクスチャ");
                DrawProperty("_MatCapIntensity2", "MatCap 2の強さ");
                DrawProperty("_MatCapBlendMode2", "MatCap 2のブレンドモード");

                EditorGUILayout.Space();
                DrawProperty("_MatCapMask2", "MatCap 2マスク");
                DrawHelpToggle("MatCapMask2", "白 = MatCap適用、黒 = 適用なし", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_MatCapBlend2", null, "_MatCap2Blur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_MatCap2DistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // MatCap 3
            bool enableMatCap3 = DrawToggle("_MATCAP_3", "_MatCap3", "MatCap 3を有効化");

            if (enableMatCap3)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_MatCapTex3", "MatCap 3 テクスチャ");
                DrawProperty("_MatCapIntensity3", "MatCap 3の強さ");
                DrawProperty("_MatCapBlendMode3", "MatCap 3のブレンドモード");

                EditorGUILayout.Space();
                DrawProperty("_MatCapMask3", "MatCap 3マスク");
                DrawHelpToggle("MatCapMask3", "白 = MatCap適用、黒 = 適用なし", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_MatCapBlend3", null, "_MatCap3Blur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_MatCap3DistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showMatCap);
    }

    private void DrawGlitterSection()
    {
        showGlitter = DrawBoxedSection("グリッター（ラメ）", showGlitter, SectionCategory.Effects, "_GLITTER");
        if (showGlitter)
        {

            bool enableGlitter = DrawToggle("_GLITTER", "_Glitter", "グリッターを有効化");

            if (enableGlitter)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("グリッター設定", EditorStyles.boldLabel);

                DrawColorProperty("_GlitterColor", "グリッター色");
                DrawProperty("_GlitterSize", "グリッターサイズ");
                DrawProperty("_GlitterDensity", "グリッター密度");
                DrawProperty("_GlitterSpeed", "グリッター速度");
                DrawProperty("_GlitterIntensity", "グリッター強度");

                EditorGUILayout.Space(5);

                DrawProperty("_GlitterMask", "グリッターマスク");
                DrawHelpToggle("GlitterMask",
                    "グリッターマスクのR(赤)チャンネルを使用してグリッターの表示領域を制御します。\n" +
                    "• 白 (1.0): グリッターを完全に表示\n" +
                    "• 黒 (0.0): グリッターを非表示\n" +
                    "• グレー: 部分的に表示",
                    MessageType.Info);
                DrawUVAnimationSettings("_GlitterMaskScrollSpeed", "_GlitterMaskRotateSpeed", "グリッターマスク");

                EditorGUILayout.Space(5);
                DrawHelpToggle("GlitterInfo",
                    "📍 グリッター:\n" +
                    "キラキラと輝くハイライト効果を追加します。\n\n" +
                    "• サイズ: グリッターの粒子サイズ\n" +
                    "• 密度: グリッターの出現頻度（0=少ない、1=多い）\n" +
                    "• 速度: グリッターの点滅速度\n" +
                    "• 強度: グリッターの明るさ\n\n" +
                    "💡 使い方:\n" +
                    "衣装やアクセサリーに華やかさを追加したい場合に使用します。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_GlitterBlend", "_GlitterBlendMode", "_GlitterBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_GlitterDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showGlitter);
    }

    private void DrawDripSection()
    {
        showDrip = DrawBoxedSection("雫エフェクト", showDrip, SectionCategory.Effects, "_WATER_DRIP");
        if (showDrip)
        {

            bool enableDrip = DrawToggle("_WATER_DRIP", "_WaterDrip", "雫エフェクトを有効化");

            if (enableDrip)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("雫エフェクト設定", EditorStyles.boldLabel);

                DrawColorProperty("_DripColor", "雫の色");
                DrawProperty("_DripSpeed", "雫の速度");
                DrawProperty("_DripDensity", "雫の密度");
                DrawProperty("_DripSize", "雫のサイズ");
                DrawProperty("_DripTrailLength", "雫の尾の長さ");
                DrawProperty("_DripIntensity", "雫の強度");
                DrawProperty("_DripSharpness", "雫のシャープさ");

                EditorGUILayout.Space(5);

                DrawProperty("_DripMask", "雫マスク");
                DrawHelpToggle("DripMask",
                    "雫マスクのR(赤)チャンネルを使用して雫の表示領域を制御します。\n" +
                    "• 白 (1.0): 雫を完全に表示\n" +
                    "• 黒 (0.0): 雫を非表示\n" +
                    "• グレー: 部分的に表示",
                    MessageType.Info);
                DrawUVAnimationSettings("_DripMaskScrollSpeed", "_DripMaskRotateSpeed", "雫マスク");

                EditorGUILayout.Space(5);
                DrawHelpToggle("DripInfo",
                    "💧 雫エフェクト（ウォータードリップ）:\n" +
                    "水滴が表面を滴り落ちるような表現を追加します。\n\n" +
                    "• 速度: 雫が落ちる速度\n" +
                    "• 密度: 雫の出現頻度（0=少ない、1=多い）\n" +
                    "• サイズ: 雫の大きさ\n" +
                    "• 尾の長さ: 雫が残す水跡の長さ\n" +
                    "• 強度: エフェクトの明るさ\n" +
                    "• シャープさ: 雫の輪郭の鮮明さ\n\n" +
                    "💡 使い方:\n" +
                    "雨に濡れた衣装やガラス表面の表現に適しています。\n" +
                    "マスクテクスチャで雫が流れる領域を制限できます。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_DripBlend", "_DripBlendMode", "_DripBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_DripDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showDrip);
    }

    private void DrawHologramSection()
    {
        showHologram = DrawBoxedSection("ホログラム / グリッチ", showHologram, SectionCategory.Effects, "_HOLOGRAM");
        if (showHologram)
        {

            bool enableHolo = DrawToggle("_HOLOGRAM", "_Hologram", "ホログラムを有効化");
            if (enableHolo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("ホログラム基本設定", EditorStyles.boldLabel);
                DrawColorProperty("_HologramColor", "ホログラム色");
                DrawProperty("_HologramMonochrome", "モノクロ化");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("スキャンライン", EditorStyles.boldLabel);
                DrawProperty("_HologramScanlineSpeed", "スキャンライン速度");
                DrawProperty("_HologramScanlineIntensity", "スキャンライン強度");
                DrawProperty("_HologramScanlineDensity", "スキャンライン密度");
                DrawProperty("_HologramScanlineWidth", "スキャンライン幅");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("エッジグロウ (Fresnel)", EditorStyles.boldLabel);
                DrawProperty("_HologramEdgeGlowPower", "エッジグロウ範囲");
                DrawProperty("_HologramEdgeGlowIntensity", "エッジグロウ強度");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("透明度・フリッカー", EditorStyles.boldLabel);
                DrawProperty("_HologramAlpha", "ホログラム透明度");
                DrawProperty("_HologramFlickerSpeed", "フリッカー速度");
                DrawProperty("_HologramFlickerAmount", "フリッカー量");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("ノイズ歪み", EditorStyles.boldLabel);
                DrawProperty("_HologramNoiseIntensity", "ノイズ強度");
                DrawProperty("_HologramNoiseSpeed", "ノイズ速度");

                DrawProperty("_HologramMask", "ホログラムマスク");
                DrawUVAnimationSettings("_HologramMaskScrollSpeed", "_HologramMaskRotateSpeed", "ホログラムマスク");

                bool useNoiseTex = DrawToggle("_HOLOGRAM_NOISE", "_UseHologramNoise", "ノイズテクスチャを使用");
                if (useNoiseTex)
                    DrawProperty("_HologramNoiseTex", "ノイズテクスチャ");

                DrawBlendControls(materialEditor, targetMaterial, "_HologramBlend", "_HologramBlendMode", "_HologramBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_HologramDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            bool enableGlitch = DrawToggle("_GLITCH", "_Glitch", "グリッチを有効化");
            if (enableGlitch)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("グリッチ設定", EditorStyles.boldLabel);
                DrawProperty("_GlitchIntensity", "グリッチ強度");
                DrawProperty("_GlitchSpeed", "グリッチ速度");
                DrawProperty("_GlitchBlockSize", "ブロックサイズ");
                DrawProperty("_GlitchRGBSplitIntensity", "RGBスプリット強度");
                DrawProperty("_GlitchFrequency", "グリッチ発生頻度");

                DrawBlendControls(materialEditor, targetMaterial, "_GlitchBlend", "_GlitchBlendMode", "_GlitchBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_GlitchDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            DrawHelpToggle("HologramInfo",
                "🔷 ホログラム＆グリッチ:\n" +
                "SF/サイバーパンク風のホログラム表現を追加します。\n\n" +
                "• スキャンライン: 3層構成の走査線エフェクト\n" +
                "• エッジグロウ: Fresnelベースの縁光り\n" +
                "• モノクロ化: 色をホログラム色に統一\n" +
                "• 透明度: Fresnel連動の自動透明化\n" +
                "• グリッチ: ランダムなUV歪み＋RGB色ずれ\n\n" +
                "💡 Transparent バリアントとの組み合わせで\n" +
                "よりリアルなホログラム投影を実現できます。",
                MessageType.Info);
        }
        EndBoxedSection(showHologram);
    }

    private void DrawOutlineSection()
    {
        showOutline = DrawBoxedSection("アウトライン（輪郭線）", showOutline, SectionCategory.Effects, "_OUTLINE");
        if (showOutline)
        {

            bool enableOutline = DrawToggle("_OUTLINE", "_Outline", "アウトラインを有効化");

            if (enableOutline)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("アウトライン設定", EditorStyles.boldLabel);

                DrawProperty("_OutlineMode", "描画方法");
                DrawProperty("_OutlineWidth", "アウトラインの幅");
                DrawColorProperty("_OutlineColor", "アウトラインの色");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("アウトラインマスク", EditorStyles.boldLabel);

                DrawProperty("_OutlineMask", "アウトラインマスク (R)");
                DrawHelpToggle("OutlineMask",
                    "アウトラインマスクのR(赤)チャンネルを使用してアウトラインの表示を制御します。\n" +
                    "• 白 (1.0): アウトラインを完全に表示\n" +
                    "• 黒 (0.0): アウトラインを非表示\n" +
                    "• グレー: 部分的に表示",
                    MessageType.Info);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("アウトライン幅マップ", EditorStyles.boldLabel);

                DrawProperty("_OutlineWidthMap", "アウトライン幅マップ (R)");
                DrawHelpToggle("OutlineWidthMap",
                    "アウトライン幅マップのR(赤)チャンネルを使用してアウトラインの幅を制御します。\n" +
                    "• 白 (1.0): 通常の幅\n" +
                    "• 黒 (0.0): 幅ゼロ（アウトライン非表示）\n" +
                    "• グレー: 部分的な幅\n\n" +
                    "💡 使い方:\n" +
                    "部位ごとにアウトラインの太さを調整したい場合に使用します。\n" +
                    "例: 顔は細く、体は太くなど。",
                    MessageType.Info);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("マルチカラーアウトライン", EditorStyles.boldLabel);

                DrawColorProperty("_OutlineColor2", "アウトラインの色 2");
                DrawProperty("_OutlineColorMix", "カラーミックス");
                DrawHelpToggle("OutlineMultiColor",
                    "📍 マルチカラーアウトライン:\n" +
                    "2色のグラデーションアウトラインを作成します。\n\n" +
                    "• 色 2: 2番目の色\n" +
                    "• カラーミックス: 2色の混合度（0-1）\n\n" +
                    "💡 使い方:\n" +
                    "アウトラインにグラデーションやアニメーション効果を追加したい場合に使用します。",
                    MessageType.Info);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("テクスチャカラーアウトライン", EditorStyles.boldLabel);

                bool texColor = DrawToggle("_OUTLINE_TEXTURE_COLOR", "_OutlineTextureColor", "テクスチャ連動カラー");
                if (texColor)
                {
                    EditorGUI.indentLevel++;
                    DrawProperty("_OutlineTexColorBlend", "テクスチャカラーブレンド");
                    DrawProperty("_OutlineTexColorDarken", "テクスチャカラー暗化");
                    DrawHelpToggle("OutlineTextureColor",
                        "🎨 テクスチャカラーアウトライン:\n" +
                        "アウトラインの色をベーステクスチャから取得します。\n" +
                        "アークナイツ：エンドフィールドスタイルのアウトラインを再現。\n\n" +
                        "• ブレンド: テクスチャ色の混合度\n" +
                        "• 暗化: テクスチャ色をどの程度暗くするか\n\n" +
                        "💡 使い方:\n" +
                        "肌色→暗い肌色、髪色→暗い髪色のように\n" +
                        "自然なアウトラインカラーを自動生成します。\n" +
                        "マルチカラーアウトラインと組み合わせ可能です。",
                        MessageType.Info);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("スムース法線（Smooth Normal）", EditorStyles.boldLabel);

                bool useSmoothNormal = DrawToggle("_SMOOTH_NORMAL", "_SmoothNormal", "スムース法線を使用");
                if (useSmoothNormal)
                {
                    EditorGUI.indentLevel++;
                    DrawProperty("_SmoothNormalMode", "法線ソース");

                    // Show texture field only when Mode 2 (Baked Normal Texture) is selected
                    MaterialProperty smoothNormalMode = FindProperty("_SmoothNormalMode", properties, false);
                    if (smoothNormalMode != null && smoothNormalMode.floatValue > 1.5f)
                    {
                        DrawProperty("_SmoothNormalTex", "スムース法線テクスチャ");
                    }

                    EditorGUILayout.Space(3);
                    DrawProperty("_SmoothNormalShadingBlend", "シェーディングブレンド");

                    DrawHelpToggle("SmoothNormal",
                        "スムース法線（Smooth Object Normal）:\n" +
                        "ハードエッジのあるモデルでアウトラインが割れる問題を解決します。\n" +
                        "また、影の出方をなじませて滑らかにすることもできます。\n\n" +
                        "• 頂点カラー ObjectSpace: 最もシンプル。ベイクツールで事前にベイク必要。\n" +
                        "• 頂点カラー TangentSpace: ノーマルマップと共存可能。lilToon互換。\n" +
                        "• テクスチャ: 最高品質。ベイクドノーマルマップテクスチャを使用。\n\n" +
                        "シェーディングブレンド:\n" +
                        "0 = アウトラインのみに適用（影には影響なし）\n" +
                        "0.3〜0.5 = 推奨。ハードエッジの影をなじませつつディテールを維持\n" +
                        "1.0 = 完全にスムース法線でシェーディング（影が最も滑らか）\n\n" +
                        "💡 使い方:\n" +
                        "1. Tools > Natane > メッシュ > スムース法線ベイク でベイク実行\n" +
                        "2. ベイクしたメッシュをモデルに適用\n" +
                        "3. ここで「スムース法線を使用」を有効化\n" +
                        "4. シェーディングブレンドで影のなじみ具合を調整",
                        MessageType.Info);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(5);

                float outlineMode = targetMaterial.GetFloat("_OutlineMode");
                if (outlineMode < FLOAT_COMPARISON_THRESHOLD)
                {
                    DrawHelpToggle("OutlineInvertedHull",
                        "【反転ハル方式】\n" +
                        "法線方向に頂点を押し出してアウトラインを描画します。\n" +
                        "• 利点: 一般的に安定した結果、距離補正により遠近で一貫した太さ\n" +
                        "• 欠点: ローポリモデルやハードエッジで乱れる場合があります\n" +
                        "• カメラ距離による自動調整機能を搭載",
                        MessageType.Info);
                }
                else
                {
                    DrawHelpToggle("OutlineBackface",
                        "【背面法】\n" +
                        "メッシュを拡大して背面を描画します。\n" +
                        "• 利点: スムーズなアウトライン、ハイポリモデルに適しています\n" +
                        "• 欠点: 内部構造が見える場合があります\n" +
                        "• 距離補正により遠くでも視認性を維持",
                        MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showOutline);
    }

    private void DrawEmissionSection()
    {
        showEmission = DrawBoxedSection("エミッション（発光）", showEmission, SectionCategory.Effects, "_EMISSION");
        if (showEmission)
        {

            bool enableEmission = DrawToggle("_EMISSION", "_Emission", "エミッションを有効化");

            if (enableEmission)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_EmissionColor", "エミッションの色", true);
                DrawProperty("_EmissionMap", "エミッションマップ");

                EditorGUILayout.Space();
                DrawEmissionUVAnimationSettings();

                DrawProperty("_EmissionPulseSpeed", "パルス速度");
                DrawProperty("_EmissionPulseAmplitude", "パルスの振幅");

                EditorGUILayout.Space();
                DrawProperty("_EmissionMask", "エミッションマスク");
                DrawHelpToggle("EmissionMask", "白 = エミッションあり、黒 = エミッションなし", MessageType.Info);
                DrawUVAnimationSettings("_EmissionMaskScrollSpeed", "_EmissionMaskRotateSpeed", "エミッションマスク");

                EditorGUILayout.Space();
                DrawProperty("_EmissionGlow", "エミッショングロー（ブルーム）");
                DrawHelpToggle("EmissionGlow",
                    "✨ エミッショングロー（ブルーム効果）:\n" +
                    "発光部分を滲ませて明るく広げ、柔らかく\n" +
                    "輝いているような効果を追加します。\n\n" +
                    "• 0 = グローなし（シャープな発光）\n" +
                    "• 0.3-0.5 = 適度なグロー（推奨）\n" +
                    "• 0.7-1.0 = 強いグロー（強烈な輝き）\n\n" +
                    "💡 ヒント:\n" +
                    "HDRカラーと組み合わせると、より鮮やかな\n" +
                    "発光効果が得られます。",
                    MessageType.None);

                DrawBlendControls(materialEditor, targetMaterial, "_EmissionBlend", "_EmissionBlendMode", "_EmissionBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_EmissionDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showEmission);
    }

    private void DrawVirtualExpressionSection()
    {
        showVirtualExpression = DrawBoxedSection("バーチャル表現", showVirtualExpression, SectionCategory.Effects);
        if (showVirtualExpression)
        {

            // Dissolve Effect
            bool enableDissolve = DrawToggle("_DISSOLVE", "_Dissolve", "ディゾルブを有効化");
            if (enableDissolve)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_DissolveAmount", "ディゾルブ量");
                DrawHelpToggle("DissolveAmount", "0 = 完全に表示、1 = 完全に消滅", MessageType.Info);

                DrawProperty("_DissolveTex", "ディゾルブテクスチャ（ノイズ）");
                DrawUVAnimationSettings("_DissolveTexScrollSpeed", "_DissolveTexRotateSpeed", "ディゾルブ");
                DrawProperty("_DissolveEdgeWidth", "エッジの幅");
                DrawColorProperty("_DissolveEdgeColor", "エッジの色", true);
                DrawProperty("_DissolveEdgeIntensity", "エッジの強さ");

                EditorGUILayout.Space();
                DrawProperty("_DissolveMask", "ディゾルブマスク");
                DrawHelpToggle("DissolveMask", "白 = ディゾルブあり、黒 = ディゾルブなし", MessageType.Info);

                DrawHelpToggle("DissolveInfo", "ディゾルブはVRChatアバターの出現アニメーションに最適な消滅・分解エフェクトを作成します。ディゾルブ量パラメータをアニメーションさせることで、オブジェクトを出現または消滅させることができます。", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_DissolveBlend", "_DissolveBlendMode", "_DissolveBlur");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Hue Shift
            bool enableHueShift = DrawToggle("_HUE_SHIFT", "_HueShiftEnable", "色相シフトを有効化");
            if (enableHueShift)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_HueShift", "色相シフト");
                DrawHelpToggle("HueShift", "マテリアル全体の色相を変更します。0 = 変更なし、0.5 = 補色、1 = 完全な回転。VRChatでの色変更エフェクトに最適です。", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_HueShiftBlend", null, "_HueShiftBlur");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Alpha Mask
            bool useAlphaMask = DrawToggle("_ALPHA_MASK", "_UseAlphaMask", "アルファマスクを使用");
            if (useAlphaMask)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_AlphaMask", "アルファマスク");
                DrawHelpToggle("AlphaMask",
                    "🎭 アルファマスク（部分的な半透明）:\n\n" +
                    "テクスチャのRチャンネルを使用して、マテリアルの透明度を\n" +
                    "部分的に制御します。\n\n" +
                    "• 白（1.0）= 完全に不透明\n" +
                    "• グレー（0.5）= 半透明\n" +
                    "• 黒（0.0）= 完全に透明\n\n" +
                    "💡 用途:\n" +
                    "　・ガラスの一部だけを透明に\n" +
                    "　・服の一部を半透明に（シースルー効果）\n" +
                    "　・グラデーションで端を透明に（フェードアウト）\n" +
                    "　・複雑な形状の透明度制御\n\n" +
                    "⚠️ 注意:\n" +
                    "半透明効果を使用するには、レンダリングモードを\n" +
                    "「Transparent」に設定してください。",
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showVirtualExpression);
    }

    private void DrawNormalMapSection()
    {
        showNormalMap = DrawBoxedSection("ノーマルマップ", showNormalMap, SectionCategory.Advanced, "_NORMALMAP");
        if (showNormalMap)
        {
            bool useNormalMap = DrawToggle("_NORMALMAP", "_UseNormalMap", "ノーマルマップを使用");

            if (useNormalMap)
            {
                DrawProperty("_BumpMap", "ノーマルマップ");
                DrawProperty("_BumpScale", "ノーマルのスケール");
                DrawUVAnimationSettings("_BumpMapScrollSpeed", "_BumpMapRotateSpeed", "ノーマルマップ");
            }
        }
        EndBoxedSection(showNormalMap);
    }

    private void DrawReflectionSection()
    {
        showReflection = DrawBoxedSection("反射 / キューブマップ", showReflection, SectionCategory.Environment, "_REFLECTION");
        if (showReflection)
        {
            bool enableReflection = DrawToggle("_REFLECTION", "_Reflection", "リフレクションを有効化");

            if (enableReflection)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_ReflectionCube", "リフレクションキューブマップ");
                DrawColorProperty("_ReflectionColor", "リフレクションの色");
                DrawProperty("_ReflectionIntensity", "リフレクションの強さ");
                DrawProperty("_Smoothness", "滑らかさ（光沢）");
                DrawProperty("_Metallic", "メタリック");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("フレネル設定（柔らかい反射）", EditorStyles.boldLabel);
                DrawProperty("_FresnelPower", "フレネルパワー");
                DrawProperty("_FresnelSoftness", "フレネルソフトネス");
                DrawHelpToggle("FresnelSoftness",
                    "✨ フレネルソフトネス:\n" +
                    "反射の境界を柔らかくして、より自然で芸術的な反射を実現します。\n" +
                    "• 0 = シャープな境界（現実的）\n" +
                    "• 0.5-0.7 = 柔らかい境界（推奨）\n" +
                    "• 1.0 = 非常に柔らかい境界（芸術的）",
                    MessageType.None);

                DrawProperty("_ReflectionBlendMode", "ブレンドモード");
                DrawHelpToggle("ReflectionBlendMode",
                    "• 0 = 加算（明るく追加）\n" +
                    "• 1 = オーバーレイ（自然な統合）",
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_ReflectionMask", "リフレクションマスク");
                DrawHelpToggle("ReflectionMask", "白 = リフレクションあり、黒 = リフレクションなし", MessageType.Info);

                DrawHelpToggle("ReflectionInfo", "キューブマップを使用して環境反射をシミュレートします。金属やガラスなどの反射素材に最適です。", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_ReflectionBlend");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_ReflectionDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showReflection);
    }

    private void DrawIridescenceSection()
    {
        showIridescence = DrawBoxedSection("イリデッセンス（玉虫色）", showIridescence, SectionCategory.Environment, "_IRIDESCENCE");
        if (showIridescence)
        {
            bool enableIridescence = DrawToggle("_IRIDESCENCE", "_Iridescence", "イリデッセンスを有効化");

            if (enableIridescence)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("イリデッセンス設定", EditorStyles.boldLabel);

                DrawColorProperty("_IridescenceColor", "イリデッセンスの色");
                DrawProperty("_IridescenceIntensity", "強度");
                DrawProperty("_IridescenceHueShift", "色相シフト");
                DrawProperty("_IridescenceSize", "サイズ（周波数）");

                EditorGUILayout.Space(5);

                DrawProperty("_IridescenceMask", "イリデッセンスマスク");
                DrawHelpToggle("IridescenceMask",
                    "イリデッセンスマスクのR(赤)チャンネルを使用してイリデッセンスの表示領域を制御します。\n" +
                    "• 白 (1.0): イリデッセンスを完全に表示\n" +
                    "• 黒 (0.0): イリデッセンスを非表示\n" +
                    "• グレー: 部分的に表示",
                    MessageType.Info);

                EditorGUILayout.Space(5);
                DrawHelpToggle("IridescenceInfo",
                    "📍 イリデッセンス（Iridescence）:\n" +
                    "見る角度によって色が変化する玉虫色効果を追加します。\n\n" +
                    "• 色: ベースとなる色調\n" +
                    "• 強度: 効果の明るさ\n" +
                    "• 色相シフト: 色の変化開始位置\n" +
                    "• サイズ: 色の変化の周波数（大きいほど早く変化）\n\n" +
                    "💡 使い方:\n" +
                    "シャボン玉、オイルスリック、昆虫の羽、魚の鱗など、\n" +
                    "光の干渉による虹色効果を表現したい場合に使用します。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_IridescenceBlend", "_IridescenceBlendMode", "_IridescenceBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_IridescenceDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showIridescence);
    }

    private void DrawEnvironmentalRimSection()
    {
        showEnvironmentalRim = DrawBoxedSection("環境リム", showEnvironmentalRim, SectionCategory.Environment, "_ENV_RIM");
        if (showEnvironmentalRim)
        {
            bool enableEnvRim = DrawToggle("_ENV_RIM", "_EnvRim", "環境リムを有効化");

            if (enableEnvRim)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_EnvRimCube", "環境キューブマップ");
                DrawColorProperty("_EnvRimColor", "環境リムの色");
                DrawProperty("_EnvRimPower", "環境リムのパワー");
                DrawProperty("_EnvRimIntensity", "環境リムの強さ");

                EditorGUILayout.Space();
                DrawProperty("_EnvRimMask", "環境リムマスク");
                DrawHelpToggle("EnvRimMask", "白 = 環境リムあり、黒 = 環境リムなし", MessageType.Info);

                DrawHelpToggle("EnvRimInfo", "キューブマップを使用して環境に基づいたリムライト効果を作成します。", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_EnvRimBlend", "_EnvRimBlendMode", "_EnvRimBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_EnvRimDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showEnvironmentalRim);
    }

    private void DrawParallaxSection()
    {
        showParallax = DrawBoxedSection("視差マッピング（パララックス）", showParallax, SectionCategory.Advanced, "_PARALLAX");
        if (showParallax)
        {
            bool enableParallax = DrawToggle("_PARALLAX", "_Parallax", "視差マッピングを有効化");

            if (enableParallax)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_ParallaxMap", "高さマップ");
                DrawProperty("_ParallaxScale", "視差のスケール");
                DrawProperty("_ParallaxMinSamples", "最小サンプル数");
                DrawProperty("_ParallaxMaxSamples", "最大サンプル数");

                DrawHelpToggle("ParallaxInfo", "視差マッピングは高さマップを使用してサーフェスに深度の錯覚を作成します。石や壁などの素材に最適です。", MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showParallax);
    }

    private void DrawVertexAnimationSection()
    {
        showVertexAnimation = DrawBoxedSection("頂点アニメーション（風/呼吸/脈動）", showVertexAnimation, SectionCategory.Advanced, "_VERTEX_ANIMATION");
        if (showVertexAnimation)
        {
            bool enableVertexAnim = DrawToggle("_VERTEX_ANIMATION", "_VertexAnimation", "頂点アニメーションを有効化");

            if (enableVertexAnim)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                DrawProperty("_VertexAnimType", "アニメーション種類");
                DrawProperty("_VertexAnimSpeed", "速度");
                DrawProperty("_VertexAnimStrength", "強度");
                DrawProperty("_VertexAnimFrequency", "周波数");

                EditorGUILayout.Space(5);
                bool useVertexAnimMask = DrawToggle("_VERTEX_ANIM_MASK", "_UseVertexAnimMask", "マスク使用");
                if (useVertexAnimMask)
                {
                    DrawProperty("_VertexAnimMask", "頂点アニメーションマスク");
                }
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showVertexAnimation);
    }

    private void DrawVATSection()
    {
        showVAT = DrawBoxedSection("VAT（頂点アニメーション）", showVAT, SectionCategory.Advanced, "_VAT");
        if (showVAT)
        {
            bool enableVAT = DrawToggle("_VAT", "_VAT", "VATアニメーションを有効化");

            if (enableVAT)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("VAT設定（Houdini互換）", EditorStyles.boldLabel);

                DrawProperty("_VATPositionMap", "VAT位置マップ");
                DrawProperty("_VATNumOfFrames", "フレーム数");
                DrawProperty("_VATSpeed", "アニメーション速度");
                DrawProperty("_VATIntensity", "アニメーション強度");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("VAT位置パラメータ", EditorStyles.boldLabel);
                DrawProperty("_VATPositionMin", "位置最小値");
                DrawProperty("_VATPositionMax", "位置最大値");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("VAT法線マップ（オプション）", EditorStyles.boldLabel);

                bool useVATNormal = DrawToggle("_VAT_NORMAL", "_VATNormal", "VAT法線マップを使用");
                if (useVATNormal)
                {
                    DrawProperty("_VATNormalMap", "VAT法線マップ");
                    DrawProperty("_VATNormalMin", "法線最小値");
                    DrawProperty("_VATNormalMax", "法線最大値");
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("VAT詳細設定", EditorStyles.boldLabel);
                DrawProperty("_VATPackingMode", "パッキングモード");
                DrawHelpToggle("VATPackingMode",
                    "• Absolute (0): 絶対位置モード - 頂点位置を直接置き換えます\n" +
                    "• Offset (1): オフセットモード - 元の位置にオフセットを追加します（推奨）",
                    MessageType.Info);

                EditorGUILayout.Space(5);
                DrawHelpToggle("VATInfo",
                    "📍 VAT（Vertex Animation Texture）:\n" +
                    "Houdiniで作成した頂点アニメーションをテクスチャとして再生します。\n\n" +
                    "【セットアップ方法】\n" +
                    "1. Houdiniで VAT（ROP Output Driver）を使用してエクスポート\n" +
                    "2. Position Map（.exr）を位置マップとして設定\n" +
                    "3. 必要に応じてNormal Map（.exr）を法線マップとして設定\n" +
                    "4. フレーム数をHoudiniの設定と一致させる\n\n" +
                    "【パラメータ説明】\n" +
                    "• 位置最小値/最大値: Houdiniエクスポート時の範囲（通常 -1 ～ 1）\n" +
                    "• 速度: アニメーション再生速度の倍率\n" +
                    "• 強度: アニメーション効果の強さ（0-2）\n\n" +
                    "💡 使い方:\n" +
                    "流体シミュレーション、布シミュレーション、複雑な変形アニメーションなど、\n" +
                    "リアルタイムでは重すぎる物理演算をベイクして再生できます。\n\n" +
                    "⚠ 注意:\n" +
                    "• VATテクスチャは通常のテクスチャよりメモリを多く使用します\n" +
                    "• VRChatでは適切な解像度とフレーム数を選択してください",
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showVAT);
    }

    private void DrawTessellationSection()
    {
        showTessellation = DrawBoxedSection("テッセレーション（曲面スムージング）", showTessellation, SectionCategory.Advanced, "_TESSELLATION");
        if (showTessellation)
        {
            bool enableTess = DrawToggle("_TESSELLATION", "_Tessellation", "テッセレーションを有効化");
            if (enableTess)
            {
                DrawProperty("_TessFactor", "テッセレーション係数");
                DrawHelpToggle("TessFactor",
                    "🔷 テッセレーション係数:\n" +
                    "メッシュの分割数を制御します。\n\n" +
                    "• 1 = 分割なし\n" +
                    "• 2-4 = 軽いスムージング（推奨）\n" +
                    "• 8-16 = 高品質（高負荷）\n\n" +
                    "⚠ VR では両目分のコストがかかります。",
                    MessageType.Info);
                DrawProperty("_TessPhongStrength", "Phong スムージング強度");
                DrawHelpToggle("TessPhong",
                    "🔷 Phong スムージング:\n" +
                    "頂点法線を使って三角形を曲面に膨らませます。\n\n" +
                    "• 0 = フラット（分割のみ）\n" +
                    "• 0.3-0.5 = 自然な丸み（推奨）\n" +
                    "• 1.0 = 最大スムージング",
                    MessageType.Info);
                DrawProperty("_TessNormalSmooth", "法線スムージング強度");
                DrawHelpToggle("TessNormalSmooth",
                    "💡 法線スムージング:\n" +
                    "テッセレーション後の法線の滑らかさを制御します。\n" +
                    "光のあたり方・影の境界の柔らかさに影響します。\n\n" +
                    "• 0 = 元のメッシュ法線をそのまま使用\n" +
                    "• 0.5 = 適度にスムーズ（推奨）\n" +
                    "• 1.0 = 最大スムーズ（光が非常に柔らかくあたる）",
                    MessageType.Info);
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("距離LOD", EditorStyles.boldLabel);
                DrawProperty("_TessDistanceMin", "最小距離（最大テッセレーション）");
                DrawProperty("_TessDistanceMax", "最大距離（テッセレーション無効）");
                DrawHelpToggle("TessDistance",
                    "📏 距離LOD:\n" +
                    "カメラからの距離でテッセレーション係数を自動調整します。\n\n" +
                    "• 最小距離以内 = 設定した係数で分割\n" +
                    "• 最大距離以遠 = テッセレーションOFF\n" +
                    "• その間 = 滑らかに遷移\n\n" +
                    "💡 VRChat ではパフォーマンスのため最大距離を10-20mに設定推奨。",
                    MessageType.Info);

                EditorGUILayout.Space(5);
                bool enableDisp = DrawToggle("_TESS_DISPLACEMENT", "_TessDisplacement", "ディスプレイスメントマップを有効化");
                if (enableDisp)
                {
                    DrawProperty("_TessDispMap", "ディスプレイスメントマップ");
                    DrawProperty("_TessDispStrength", "ディスプレイスメント強度");
                    DrawProperty("_TessDispOffset", "ディスプレイスメントオフセット");
                    DrawHelpToggle("TessDisp",
                        "🗻 ディスプレイスメントマップ:\n" +
                        "ハイトマップを使って実際にメッシュの形状を変化させます。\n" +
                        "影や光の反応が変わり、細かい凹凸表現が可能です。\n\n" +
                        "• グレースケールテクスチャを使用（白=高い, 黒=低い）\n" +
                        "• 強度: + = 外側に押し出す, - = 内側に凹む\n" +
                        "• オフセット: 基準面を調整\n\n" +
                        "⚠ テッセレーション係数が低いと効果が粗くなります。",
                        MessageType.Info);
                }
            }
        }
        EndBoxedSection(showTessellation);
    }

    private void DrawRefractionSection()
    {
        showRefraction = DrawBoxedSection("屈折（リフラクション）", showRefraction, SectionCategory.Environment, "_REFRACTION");
        if (showRefraction)
        {
            bool enableRefraction = DrawToggle("_REFRACTION", "_Refraction", "屈折を有効化");

            if (enableRefraction)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_RefractionIndex", "屈折率（IOR）");
                DrawProperty("_RefractionIntensity", "屈折の強さ");
                DrawProperty("_RefractionBlur", "屈折のぼかし");

                EditorGUILayout.Space();
                DrawProperty("_RefractionMask", "屈折マスク");
                DrawHelpToggle("RefractionMask", "白 = 屈折あり、黒 = 屈折なし", MessageType.Info);

                DrawHelpToggle("RefractionInfo", "屈折はガラスや水などの透明素材で光の曲がりをシミュレートします。透明マテリアルに最適です。", MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_RefractionBlend", "_RefractionBlendMode");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_RefractionDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showRefraction);
    }

    private void DrawRenderingSection()
    {
        showRendering = DrawBoxedSection("レンダリング設定", showRendering, SectionCategory.Advanced);
        if (showRendering)
        {

            // Rendering Mode Selection
            EditorGUILayout.LabelField("レンダリングモード", EditorStyles.boldLabel);
            RenderingMode currentMode = GetCurrentRenderingMode();

            EditorGUI.BeginChangeCheck();
            RenderingMode newMode = (RenderingMode)EditorGUILayout.EnumPopup("描画タイプ", currentMode);
            if (EditorGUI.EndChangeCheck())
            {
                SetRenderingMode(newMode);
            }

            DrawHelpToggle("RenderingMode",
                "🎨 レンダリングモード:\n\n" +
                "• Opaque（不透明）: 標準的な不透明オブジェクト\n" +
                "  - 肌、服、硬い物体など\n" +
                "  - 最も高速で推奨\n\n" +
                "• Cutout（切り抜き）: アルファ閾値による透過\n" +
                "  - 髪の毛、葉っぱ、フェンスなど\n" +
                "  - アルファ値が0.5以上で表示、未満で非表示\n" +
                "  - 半透明ではなく、完全に透明か不透明かの2択\n\n" +
                "• Transparent（半透明）: 滑らかな透過\n" +
                "  - ガラス、水、煙、エフェクトなど\n" +
                "  - アルファ値に応じて段階的に透過\n" +
                "  - 最も負荷が高い\n\n" +
                "💡 注意:\n" +
                "モードを変更すると、内部的に適切なシェーダーバリアントに\n" +
                "切り替わりますが、すべてのプロパティは保持されます。",
                MessageType.Info);

            // Cutout の場合にAlpha Cutoff スライダーを表示
            if (currentMode == RenderingMode.Cutout)
            {
                EditorGUILayout.Space(5);
                MaterialProperty cutoffProp = FindProperty("_Cutoff", properties, false);
                if (cutoffProp != null)
                {
                    DrawProperty("_Cutoff", "アルファカットオフ");
                    DrawHelpToggle("AlphaCutoff",
                        "テクスチャの透明度がこの値以下のピクセルを非表示にします。\n" +
                        "• 0.5 = デフォルト（推奨）\n" +
                        "• 値を下げると: より多くのピクセルが表示される\n" +
                        "• 値を上げると: より多くのピクセルが非表示になる",
                        MessageType.Info);
                }
            }

            EditorGUILayout.Space(10);

            // Advanced Rendering Settings
            EditorGUILayout.LabelField("詳細設定", EditorStyles.boldLabel);
            DrawProperty("_Cull", "カリングモード");
            DrawProperty("_ZWrite", "Z書き込み");

            // Show blend mode properties for Transparent mode
            if (currentMode == RenderingMode.Transparent)
            {
                DrawProperty("_SrcBlend", "ソースブレンド");
                DrawProperty("_DstBlend", "宛先ブレンド");
                DrawHelpToggle("BlendMode",
                    "🎨 ブレンドモード:\n" +
                    "半透明の合成方法を制御します。\n\n" +
                    "標準設定:\n" +
                    "• ソースブレンド: SrcAlpha (5)\n" +
                    "• 宛先ブレンド: OneMinusSrcAlpha (10)\n\n" +
                    "通常は変更する必要はありません。",
                    MessageType.Info);
            }

        }
        EndBoxedSection(showRendering);
    }

    private void DrawAOSection()
    {
        showAO = DrawBoxedSection("アンビエントオクルージョン（AO）", showAO, SectionCategory.Lighting, "_USE_AO");
        if (showAO)
        {
            bool enableAO = DrawToggle("_USE_AO", "_UseAO", "AO を有効化");
            if (enableAO)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_AOMap", "AO マップ");
                DrawProperty("_AOIntensity", "AO強度");
                DrawHelpToggle("AO",
                    "🌑 アンビエントオクルージョン (AO):\n" +
                    "テクスチャの暗い部分で陰影を追加し、奥行き感を出します。\n" +
                    "• AO マップ: 白=影なし、黒=完全な影\n" +
                    "• AO強度: 0=効果なし、1=最大効果\n\n" +
                    "💡 キャラクターの脇の下や首元などの影表現に最適です。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_AOBlend", "_AOBlendMode", "_AOBlur");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showAO);
    }

    private void DrawDitheringSection()
    {
        showDithering = DrawBoxedSection("ディザリング（スクリーントーン）", showDithering, SectionCategory.Lighting, "_USE_DITHERING");
        if (showDithering)
        {
            bool enableDithering = DrawToggle("_USE_DITHERING", "_UseDithering", "ディザリング影の境界を有効化");
            if (enableDithering)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_DitheringScale", "パターンサイズ");
                DrawProperty("_DitheringStrength", "境界のソフトネス");
                DrawHelpToggle("Dithering",
                    "🔲 ディザリング影の境界:\n" +
                    "影の境界にディザパターンを適用して、滑らかなトーン遷移を実現します。\n" +
                    "• パターンサイズ: ディザパターンの大きさ（1-100）\n" +
                    "• ソフトネス: 境界のぼかし具合（0-1）\n\n" +
                    "💡 漫画やイラスト調のスクリーントーン表現に最適です。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_DitheringBlend", null, "_DitheringBlur");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showDithering);
    }

    private void DrawDecalSection()
    {
        showDecal = DrawBoxedSection("デカール（貼り付け）", showDecal, SectionCategory.Effects, "_DECAL");
        if (showDecal)
        {
            bool enableDecal = DrawToggle("_DECAL", "_Decal", "デカールを有効化");
            if (enableDecal)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_DecalTex", "デカールテクスチャ");
                DrawColorProperty("_DecalColor", "デカールカラー");
                DrawProperty("_DecalPosition", "デカール位置 (XY)");
                DrawProperty("_DecalRotation", "回転");
                DrawProperty("_DecalScale", "スケール");
                DrawProperty("_DecalBlendMode", "合成モード");
                DrawHelpToggle("Decal",
                    "🏷️ デカール:\n" +
                    "メッシュ表面にテクスチャを貼り付けます。\n" +
                    "• 位置: UV空間でのデカール位置\n" +
                    "• 合成モード: Add/Multiply/Overlay/Replace\n\n" +
                    "💡 ロゴやマーク、タトゥー表現に最適です。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_DecalBlend", null, "_DecalBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_DecalDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showDecal);
    }

    private void DrawBackfaceSection()
    {
        showBackface = DrawBoxedSection("裏面テクスチャ", showBackface, SectionCategory.Advanced, "_BACKFACE_TEXTURE");
        if (showBackface)
        {
            bool enableBackface = DrawToggle("_BACKFACE_TEXTURE", "_BackfaceTexture", "裏面テクスチャを有効化");
            if (enableBackface)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_BackfaceTex", "裏面テクスチャ");
                DrawColorProperty("_BackfaceColor", "裏面カラー");
                DrawHelpToggle("BackfaceTexture",
                    "🔄 裏面テクスチャ:\n" +
                    "ポリゴンの裏面に別のテクスチャを表示します。\n" +
                    "• カリングが Off の場合に裏面が見えます\n\n" +
                    "💡 服の裏地や紙の裏面表現に最適です。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_BackfaceBlend", "_BackfaceBlendMode");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showBackface);
    }

    private void DrawVideoSection()
    {
        showVideo = DrawBoxedSection("ビデオテクスチャ", showVideo, SectionCategory.Advanced, "_VIDEO_TEXTURE");
        if (showVideo)
        {
            bool enableVideo = DrawToggle("_VIDEO_TEXTURE", "_VideoTexture", "ビデオテクスチャを有効化");
            if (enableVideo)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_VideoTex", "ビデオレンダーテクスチャ");
                DrawProperty("_VideoEmission", "ビデオエミッション強度");
                DrawHelpToggle("VideoTexture",
                    "📺 ビデオテクスチャ:\n" +
                    "RenderTexture を使用してビデオ映像をマテリアルに表示します。\n" +
                    "• エミッション: ビデオの発光強度\n\n" +
                    "💡 VRChat でのスクリーン表示やモニター表現に最適です。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_VideoBlend", "_VideoBlendMode");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showVideo);
    }

    private void DrawAudioLinkSection()
    {
        showAudioLink = DrawBoxedSection("AudioLink（音楽連動）", showAudioLink, SectionCategory.Effects, "_AUDIOLINK");
        if (showAudioLink)
        {
            bool enableAudioLink = DrawToggle("_AUDIOLINK", "_AudioLink", "AudioLink を有効化");
            if (enableAudioLink)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("エミッション連動", EditorStyles.boldLabel);
                DrawProperty("_AudioLinkEmissionBand", "周波数帯域");
                DrawProperty("_AudioLinkEmissionIntensity", "エミッション強度");

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("リムライト連動", EditorStyles.boldLabel);
                DrawProperty("_AudioLinkRimBand", "周波数帯域");
                DrawProperty("_AudioLinkRimIntensity", "リムライト強度");

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("色相シフト連動", EditorStyles.boldLabel);
                DrawProperty("_AudioLinkHueBand", "周波数帯域");
                DrawProperty("_AudioLinkHueShiftIntensity", "色相シフト強度");

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("ディゾルブ連動", EditorStyles.boldLabel);
                DrawProperty("_AudioLinkDissolveBand", "周波数帯域");
                DrawProperty("_AudioLinkDissolveIntensity", "ディゾルブ強度");

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("アウトライン連動", EditorStyles.boldLabel);
                DrawProperty("_AudioLinkOutlineBand", "周波数帯域");
                DrawProperty("_AudioLinkOutlineIntensity", "アウトライン強度");

                DrawHelpToggle("AudioLink",
                    "🎵 AudioLink:\n" +
                    "AudioLink 対応ワールドで音楽に連動したエフェクトを実現します。\n" +
                    "• 周波数帯域: Bass/Low Mid/High Mid/Treble\n" +
                    "• 各エフェクトを個別に有効化できます\n" +
                    "• Chronotensity: 時間経過によるアニメーション\n\n" +
                    "💡 VRChat のクラブやライブイベント向けに最適です。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_AudioLinkBlend", "_AudioLinkBlendMode");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_AudioLinkDistFade", "距離フェード強度");
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showAudioLink);
    }

    private void DrawDistanceFadeSection()
    {
        showDistanceFade = DrawBoxedSection("距離フェード", showDistanceFade, SectionCategory.Advanced, "_DISTANCE_FADE");
        if (showDistanceFade)
        {
            bool enableDistanceFade = DrawToggle("_DISTANCE_FADE", "_DistanceFade", "距離フェードを有効化");
            if (enableDistanceFade)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_DistanceFadeStart", "フェード開始距離");
                DrawProperty("_DistanceFadeEnd", "フェード終了距離");
                DrawProperty("_DistanceFadeMode", "フェードモード");
                DrawHelpToggle("DistanceFade",
                    "📏 距離フェード:\n" +
                    "カメラからの距離に応じてオブジェクトをフェードします。\n" +
                    "• 開始距離: フェードが始まる距離\n" +
                    "• 終了距離: 完全に消える距離\n" +
                    "• Alpha: 透明度でフェード\n" +
                    "• Simplify: エフェクトを簡略化\n\n" +
                    "💡 パフォーマンス最適化に有効です。遠距離のオブジェクト描画負荷を軽減します。",
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_DistanceFadeBlend", null, "_DistFadeBlur");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(showDistanceFade);
    }

    /// <summary>
    /// Get current rendering mode based on shader name
    /// </summary>
    private RenderingMode GetCurrentRenderingMode()
    {
        if (targetMaterial == null || targetMaterial.shader == null)
            return RenderingMode.Opaque;

        string shaderName = targetMaterial.shader.name;

        if (shaderName.Contains("Transparent"))
            return RenderingMode.Transparent;
        else if (shaderName.Contains("Cutout"))
            return RenderingMode.Cutout;
        else
            return RenderingMode.Opaque;
    }

    /// <summary>
    /// Set rendering mode by switching to appropriate shader variant
    /// </summary>
    private void SetRenderingMode(RenderingMode mode)
    {
        if (targetMaterial == null)
            return;

        // Get base shader name
        string baseShaderName = "Natane/Toon Shader";
        string newShaderName = baseShaderName;

        // Determine shader variant based on mode
        switch (mode)
        {
            case RenderingMode.Opaque:
                newShaderName = baseShaderName;
                break;
            case RenderingMode.Cutout:
                newShaderName = baseShaderName + " (Cutout)";
                break;
            case RenderingMode.Transparent:
                newShaderName = baseShaderName + " (Transparent)";
                break;
        }

        // Find the shader
        Shader newShader = Shader.Find(newShaderName);
        if (newShader == null)
        {
            Debug.LogError($"[NataneToonShaderGUI] Shader not found: {newShaderName}");
            return;
        }

        // Check if already using this shader
        if (targetMaterial.shader == newShader)
            return;

        // Record undo
        Undo.RecordObject(targetMaterial, "Change Rendering Mode");

        // Switch shader (properties with same names are preserved)
        targetMaterial.shader = newShader;

        // Set default properties based on mode
        switch (mode)
        {
            case RenderingMode.Opaque:
                targetMaterial.SetFloat("_ZWrite", 1);
                targetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                break;

            case RenderingMode.Cutout:
                targetMaterial.SetFloat("_ZWrite", 1);
                targetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                break;

            case RenderingMode.Transparent:
                targetMaterial.SetFloat("_ZWrite", 0);
                targetMaterial.SetFloat("_SrcBlend", 5); // SrcAlpha
                targetMaterial.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
                targetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
        }

        // Mark as dirty
        EditorUtility.SetDirty(targetMaterial);

        // Repaint inspector
        if (materialEditor != null)
        {
            materialEditor.Repaint();
        }
    }

    /// <summary>
    /// Toon以外のシェーダー用GUIを描画 (委譲パターン)
    /// </summary>
    private void DrawNonToonShaderGUI(NataneToon.Editor.ShaderType type)
    {
        switch (type)
        {
            case NataneToon.Editor.ShaderType.Eye:
                if (eyeDrawer == null) eyeDrawer = new NataneToon.Editor.NataneToonEyeDrawer();
                eyeDrawer.Initialize(materialEditor, properties, targetMaterial);
                eyeDrawer.Draw();
                break;

            case NataneToon.Editor.ShaderType.Wirelight:
                if (wirelightDrawer == null) wirelightDrawer = new NataneToon.Editor.NataneToonWirelightDrawer();
                wirelightDrawer.Initialize(materialEditor, properties, targetMaterial);
                wirelightDrawer.Draw();
                break;

            case NataneToon.Editor.ShaderType.ScreenFX:
                if (screenFXDrawer == null) screenFXDrawer = new NataneToon.Editor.NataneToonScreenFXDrawer();
                screenFXDrawer.Initialize(materialEditor, properties, targetMaterial);
                screenFXDrawer.Draw();
                break;
        }
    }

    private void DrawProperty(string propertyName, string label)
    {
        MaterialProperty property = FindProperty(propertyName, properties, false);
        if (property != null)
        {
            materialEditor.ShaderProperty(property, label);
        }
    }

    private void DrawColorProperty(string propertyName, string label, bool defaultHDR = false)
    {
        MaterialProperty property = FindProperty(propertyName, properties, false);
        if (property == null) return;

        string prefsKey = "NataneToon_HDR_" + propertyName;
        bool isHDR = EditorPrefs.GetBool(prefsKey, defaultHDR);

        EditorGUILayout.BeginHorizontal();

        // Draw color field with HDR support
        EditorGUI.BeginChangeCheck();
        Rect colorRect = EditorGUILayout.GetControlRect(true);
        Color colorValue = EditorGUI.ColorField(
            colorRect,
            new GUIContent(label),
            property.colorValue,
            true,  // showEyedropper
            true,  // showAlpha
            isHDR  // hdr
        );
        if (EditorGUI.EndChangeCheck())
        {
            property.colorValue = colorValue;
        }

        // HDR toggle button
        var oldBgColor = GUI.backgroundColor;
        if (isHDR)
        {
            GUI.backgroundColor = new Color(1.0f, 0.85f, 0.2f); // gold
        }
        if (GUILayout.Button("HDR", CachedHDRToggleStyle))
        {
            isHDR = !isHDR;
            EditorPrefs.SetBool(prefsKey, isHDR);
        }
        GUI.backgroundColor = oldBgColor;

        EditorGUILayout.EndHorizontal();
    }

    private bool DrawToggle(string keyword, string propertyName, string label)
    {
        MaterialProperty property = FindProperty(propertyName, properties, false);
        if (property == null)
        {
            return false;
        }

        EditorGUI.BeginChangeCheck();
        bool enabled = property.floatValue > FLOAT_COMPARISON_THRESHOLD;

        // Create a horizontal layout for toggle with visual indicator
        EditorGUILayout.BeginHorizontal();

        // Draw toggle
        enabled = EditorGUILayout.Toggle(label, enabled);

        // Visual indicator
        string statusIcon = enabled ? "✓" : "✗";
        Color statusColor = enabled ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.6f, 0.6f, 0.6f);

        var oldColor = GUI.color;
        GUI.color = statusColor;
        GUILayout.Label(statusIcon, GUILayout.Width(20));
        GUI.color = oldColor;

        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            property.floatValue = enabled ? 1.0f : 0.0f;

            // Set shader keyword
            if (enabled)
                targetMaterial.EnableKeyword(keyword);
            else
                targetMaterial.DisableKeyword(keyword);
        }

        return enabled;
    }

    /// <summary>
    /// Draw a blend/softness parameter with help text (delegates to helper class)
    /// </summary>
    private void DrawBlendParameter(string propertyName, string label, string helpText)
    {
        NataneToonShaderGUIHelpers.DrawBlendParameter(
            propertyName,
            label,
            helpText,
            DrawProperty);
    }

    /// <summary>
    /// Draw blend controls (blend mode + blend amount + optional blur) for an effect section
    /// Groups controls under a sub-section header with proper indentation
    /// </summary>
    private void DrawBlendControls(MaterialEditor materialEditor, Material material, string blendProp, string blendModeProp = null, string blurProp = null)
    {
        bool hasBlend = material.HasProperty(blendProp);
        bool hasBlendMode = blendModeProp != null && material.HasProperty(blendModeProp);
        bool hasBlur = blurProp != null && material.HasProperty(blurProp);

        if (!hasBlend && !hasBlendMode && !hasBlur) return;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("── ブレンド＆ブラー ──", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        if (hasBlendMode)
        {
            MaterialProperty blendModeMat = FindProperty(blendModeProp, properties, false);
            if (blendModeMat != null)
                materialEditor.ShaderProperty(blendModeMat, "ブレンドモード");
        }
        if (hasBlend)
        {
            MaterialProperty blendMat = FindProperty(blendProp, properties, false);
            if (blendMat != null)
                materialEditor.ShaderProperty(blendMat, "ブレンド");
        }
        if (hasBlur)
        {
            MaterialProperty blurMat = FindProperty(blurProp, properties, false);
            if (blurMat != null)
                materialEditor.ShaderProperty(blurMat, "ブラー");
        }

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// Draw UV animation settings (scroll speed XY + rotation speed) for any texture
    /// </summary>
    private void DrawUVAnimationSettings(string scrollProp, string rotateProp, string label)
    {
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField($"UVアニメーション ({label})", EditorStyles.miniLabel);
        DrawProperty(scrollProp, "スクロール速度 XY");
        DrawProperty(rotateProp, "回転速度");
    }

    /// <summary>
    /// Draw Emission-specific UV animation settings (backward-compatible Float×2 + Float layout)
    /// </summary>
    private void DrawEmissionUVAnimationSettings()
    {
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("UVアニメーション (エミッション)", EditorStyles.miniLabel);
        DrawProperty("_EmissionScrollSpeed", "スクロール速度 X");
        DrawProperty("_EmissionScrollSpeedY", "スクロール速度 Y");
        DrawProperty("_EmissionRotateSpeed", "回転速度");
    }

    /// <summary>
    /// Draw presets and sharing section
    /// </summary>
    private void DrawPresetsSection()
    {
        showPresets = DrawBoxedSection("マテリアルプリセット＆共有", showPresets, SectionCategory.Basic);
        if (showPresets)
        {
            NataneToonShaderGUIUtility.DrawMaterialActionsToolbar(targetMaterial, materialEditor);
        }
        EndBoxedSection(showPresets);
    }

    /// <summary>
    /// Draw Feature Overview panel - shows all feature ON/OFF states in a compact grid
    /// 機能一覧パネル - 全機能のON/OFF状態をコンパクトなグリッドで表示
    /// </summary>
    private void DrawFeatureOverviewSection()
    {
        showFeatureOverview = DrawBoxedSection("機能一覧", showFeatureOverview, SectionCategory.Basic);
        if (showFeatureOverview)
        {
            // Feature keywords and display names for the overview grid
            string[][] features = new string[][]
            {
                new[] { "_SPECULAR", "スペキュラー" },
                new[] { "_HAIR_SPECULAR", "ヘアハイライト" },
                new[] { "_RIM_LIGHT", "リムライト" },
                new[] { "_SSS", "SSS" },
                new[] { "_MATCAP", "MatCap" },
                new[] { "_GLITTER", "グリッター" },
                new[] { "_WATER_DRIP", "雫" },
                new[] { "_HOLOGRAM", "ホログラム" },
                new[] { "_DECAL", "デカール" },
                new[] { "_OUTLINE", "アウトライン" },
                new[] { "_EMISSION", "エミッション" },
                new[] { "_AUDIOLINK", "AudioLink" },
                new[] { "_REFLECTION", "リフレクション" },
                new[] { "_IRIDESCENCE", "イリデッセンス" },
                new[] { "_ENV_RIM", "環境リム" },
                new[] { "_REFRACTION", "屈折" },
                new[] { "_NORMALMAP", "ノーマルマップ" },
                new[] { "_PARALLAX", "パララックス" },
                new[] { "_VERTEX_ANIMATION", "頂点アニメーション" },
                new[] { "_VAT", "VAT" },
                new[] { "_USE_AO", "AO" },
                new[] { "_USE_DITHERING", "ディザリング" },
                new[] { "_USE_LIGHT_VOLUME", "Light Volume" },
                new[] { "_DISTANCE_FADE", "距離フェード" },
                new[] { "_BACKFACE_TEXTURE", "裏面" },
            };

            int enabledCount = 0;
            int columns = 4;

            for (int i = 0; i < features.Length; i++)
            {
                if (i % columns == 0)
                    EditorGUILayout.BeginHorizontal();

                bool isEnabled = targetMaterial.IsKeywordEnabled(features[i][0]);
                if (isEnabled) enabledCount++;

                Color badgeColor = isEnabled ? new Color(0.2f, 0.7f, 0.3f, 0.9f) : new Color(0.4f, 0.4f, 0.4f, 0.4f);
                Color textColor = isEnabled ? Color.white : new Color(0.6f, 0.6f, 0.6f);

                Rect btnRect = GUILayoutUtility.GetRect(new GUIContent(features[i][1]), EditorStyles.miniButton, GUILayout.Height(20));

                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(btnRect, badgeColor);
                }

                var oldColor = GUI.contentColor;
                GUI.contentColor = textColor;
                GUI.Label(btnRect, features[i][1], new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = isEnabled ? FontStyle.Bold : FontStyle.Normal
                });
                GUI.contentColor = oldColor;

                if (i % columns == columns - 1 || i == features.Length - 1)
                    EditorGUILayout.EndHorizontal();
            }

            // Fill remaining columns if last row is incomplete
            int remainder = features.Length % columns;
            if (remainder != 0)
            {
                // Row was already ended above
            }

            EditorGUILayout.Space(4);

            // Performance rating bar
            string rating;
            Color ratingColor;
            if (enabledCount <= 3) { rating = "A"; ratingColor = NataneToonColorPalette.PerformanceA; }
            else if (enabledCount <= 6) { rating = "B"; ratingColor = NataneToonColorPalette.PerformanceB; }
            else if (enabledCount <= 9) { rating = "C"; ratingColor = NataneToonColorPalette.PerformanceC; }
            else { rating = "D"; ratingColor = NataneToonColorPalette.PerformanceD; }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"有効機能: {enabledCount}/{features.Length}", GUILayout.Width(120));
            Rect barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
                float fillWidth = barRect.width * ((float)enabledCount / features.Length);
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, fillWidth, barRect.height), ratingColor);
            }
            EditorGUILayout.LabelField($"Rating: {rating}", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
        }
        EndBoxedSection(showFeatureOverview);
    }

    /// <summary>
    /// Draw performance indicator section
    /// </summary>
    private void DrawPerformanceSection()
    {
        showPerformance = DrawBoxedSection("パフォーマンス", showPerformance, SectionCategory.Basic);
        if (showPerformance)
        {
            NataneToonShaderGUIUtility.DrawPerformanceIndicator(targetMaterial);

            DrawHelpToggle("PerformanceHint",
                "ヒント：使用していない機能を無効化するとパフォーマンスが向上します。\n" +
                "チェックボックスのある機能はオン/オフの切り替えが可能です。",
                MessageType.Info);
        }
        EndBoxedSection(showPerformance);
    }

    /// <summary>
    /// Load UI state (selected tab) from EditorPrefs
    /// </summary>
    private void LoadUIState()
    {
        selectedTab = EditorPrefs.GetInt(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "SelectedTab"), 0);
    }

    /// <summary>
    /// Save UI state (selected tab) to EditorPrefs
    /// </summary>
    private void SaveUIState()
    {
        EditorPrefs.SetInt(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "SelectedTab"), selectedTab);
    }

    /// <summary>
    /// Load foldout states from EditorPrefs for this specific material
    /// </summary>
    private void LoadFoldoutStates()
    {
        showPresets = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPresets"), true);
        showPerformance = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPerformance"), true);
        showMainTexture = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowBasic"), true);
        showMakeupTextures = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowMakeupTextures"), false);
        showShading = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowShading"), true);
        showAdvancedLighting = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAdvancedLighting"), false);
        showLightVolume = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowLightVolume"), false);
        showSpecular = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowSpecular"), false);
        showRimLight = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowRimLight"), false);
        showSSS = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowSSS"), false);
        showMatCap = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowMatCap"), false);
        showGlitter = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowGlitter"), false);
        showDrip = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowDrip"), false);
        showHologram = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowHologram"), false);
        showOutline = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowOutline"), false);
        showEmission = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowEmission"), false);
        showVirtualExpression = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVirtualExpression"), false);
        showNormalMap = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowNormalMap"), false);
        showReflection = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowReflection"), false);
        showIridescence = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowIridescence"), false);
        showEnvironmentalRim = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowEnvironmentalRim"), false);
        showParallax = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowParallax"), false);
        showRefraction = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowRefraction"), false);
        showRendering = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAdvanced"), false);
        showVertexAnimation = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVertexAnimation"), false);
        showVAT = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVAT"), false);
        showLTCGI = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowLTCGI"), false);
        showAO = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAO"), false);
        showDithering = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowDithering"), false);
        showDecal = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowDecal"), false);
        showBackface = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowBackface"), false);
        showVideo = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVideo"), false);
        showAudioLink = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAudioLink"), false);
        showDistanceFade = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowDistanceFade"), false);
        showHairSpecular = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowHairSpecular"), false);
        showFeatureOverview = EditorPrefs.GetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowFeatureOverview"), false);
    }

    /// <summary>
    /// Save foldout states to EditorPrefs for this specific material
    /// </summary>
    private void SaveFoldoutStates()
    {
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPresets"), showPresets);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowPerformance"), showPerformance);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowBasic"), showMainTexture);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowMakeupTextures"), showMakeupTextures);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowShading"), showShading);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAdvancedLighting"), showAdvancedLighting);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowLightVolume"), showLightVolume);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowSpecular"), showSpecular);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowRimLight"), showRimLight);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowSSS"), showSSS);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowMatCap"), showMatCap);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowGlitter"), showGlitter);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowDrip"), showDrip);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowHologram"), showHologram);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowOutline"), showOutline);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowEmission"), showEmission);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVirtualExpression"), showVirtualExpression);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowNormalMap"), showNormalMap);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowReflection"), showReflection);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowIridescence"), showIridescence);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowEnvironmentalRim"), showEnvironmentalRim);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowParallax"), showParallax);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowRefraction"), showRefraction);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAdvanced"), showRendering);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVertexAnimation"), showVertexAnimation);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVAT"), showVAT);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowLTCGI"), showLTCGI);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAO"), showAO);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowDithering"), showDithering);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowDecal"), showDecal);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowBackface"), showBackface);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowVideo"), showVideo);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowAudioLink"), showAudioLink);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowDistanceFade"), showDistanceFade);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowHairSpecular"), showHairSpecular);
        EditorPrefs.SetBool(NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "ShowFeatureOverview"), showFeatureOverview);
    }

    // ===== UI HELPER METHODS =====

    /// <summary>
    /// Draw expand all / collapse all buttons for a tab
    /// タブ用の全展開/全折畳ボタンを描画
    /// </summary>
    private void DrawExpandCollapseButtons(System.Action<bool> setAllFoldouts)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("全展開", EditorStyles.miniButtonLeft, GUILayout.Width(50)))
        {
            setAllFoldouts(true);
            SaveFoldoutStates();
        }
        if (GUILayout.Button("全折畳", EditorStyles.miniButtonRight, GUILayout.Width(50)))
        {
            setAllFoldouts(false);
            SaveFoldoutStates();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }

    /// <summary>
    /// Draw compact header with quick actions
    /// </summary>
    private void DrawCompactHeader()
    {
        EditorGUILayout.BeginHorizontal();

        // Title (compact)
        EditorGUILayout.LabelField("Natane Toon Shader", CachedHeaderTitleStyle, GUILayout.Width(200));

        GUILayout.FlexibleSpace();

        // Quick validation button
        if (GUILayout.Button(new GUIContent("更新", "キーワード検証"), GUILayout.Width(40), GUILayout.Height(20)))
        {
            ValidateAndFixKeywords();
        }

        // Expand/Collapse buttons
        if (GUILayout.Button("展開", GUILayout.Width(50), GUILayout.Height(20)))
        {
            ExpandAllSections(true);

            // Force repaint and exit GUI to prevent layout conflicts
            if (materialEditor != null)
            {
                materialEditor.Repaint();
            }
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("折畳", GUILayout.Width(50), GUILayout.Height(20)))
        {
            ExpandAllSections(false);

            // Force repaint and exit GUI to prevent layout conflicts
            if (materialEditor != null)
            {
                materialEditor.Repaint();
            }
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    // ===== TAB DRAWING METHODS =====

    /// <summary>
    /// Draw Basic tab content (Main Texture, Surface Finish, Makeup, Shading)
    /// </summary>
    private void DrawBasicTab()
    {
        SafeDrawSection(DrawPresetsSection, "プリセット");
        SafeDrawSection(DrawFeatureOverviewSection, "機能一覧");
        SafeDrawSection(DrawPerformanceSection, "パフォーマンス");
        SafeDrawSection(DrawQuickSetupSection, "クイックセットアップ"); // 3.2 Quick Setup
        EditorGUILayout.Space(5);

        SafeDrawSection(DrawMainTextureSection, "メインテクスチャ");
        DrawSurfaceFinishSection();
        EditorGUILayout.Space(5);
        SafeDrawSection(DrawMakeupTexturesSection, "メイクアップテクスチャ");
        SafeDrawSection(DrawShadingSection, "シェーディング");
    }

    /// <summary>
    /// Draw Lighting tab content
    /// </summary>
    private void DrawLightingTab()
    {
        DrawExpandCollapseButtons((state) => {
            showAdvancedLighting = state; showAO = state; showDithering = state;
            showLightVolume = state; showLTCGI = state;
        });
        // ─── ライティング基本 ───
        NataneToonShaderGUIUtility.DrawCategoryDivider("ライティング基本");
        SafeDrawSection(DrawAdvancedLightingSection, "高度なライティング");
        SafeDrawSection(DrawAOSection, "AO");
        SafeDrawSection(DrawDitheringSection, "ディザリング");

        // ─── 外部ライティング ───
        NataneToonShaderGUIUtility.DrawCategoryDivider("外部ライティング");
        SafeDrawSection(DrawLightVolumeSection, "Light Volume");
        SafeDrawSection(DrawLTCGISection, "LTCGI");
    }

    /// <summary>
    /// Draw Effects tab content
    /// </summary>
    private void DrawEffectsTab()
    {
        DrawExpandCollapseButtons((state) => {
            showSpecular = state; showHairSpecular = state; showRimLight = state; showSSS = state;
            showMatCap = state; showGlitter = state; showDrip = state; showDecal = state;
            showHologram = state; showOutline = state; showEmission = state;
            showVirtualExpression = state; showAudioLink = state;
        });
        // ─── 光源エフェクト ───
        NataneToonShaderGUIUtility.DrawCategoryDivider("光源エフェクト");
        SafeDrawSection(DrawSpecularSection, "スペキュラー");
        SafeDrawSection(DrawHairSpecularSection, "ヘアスペキュラー");
        SafeDrawSection(DrawRimLightSection, "リムライト");
        SafeDrawSection(DrawSSSSection, "SSS");

        // ─── 表面エフェクト ───
        NataneToonShaderGUIUtility.DrawCategoryDivider("表面エフェクト");
        SafeDrawSection(DrawMatCapSection, "MatCap");
        SafeDrawSection(DrawGlitterSection, "グリッター");
        SafeDrawSection(DrawDripSection, "雫エフェクト");
        SafeDrawSection(DrawDecalSection, "デカール");

        // ─── ビジュアルエフェクト ───
        NataneToonShaderGUIUtility.DrawCategoryDivider("ビジュアルエフェクト");
        SafeDrawSection(DrawHologramSection, "ホログラム＆グリッチ");
        SafeDrawSection(DrawOutlineSection, "アウトライン");
        SafeDrawSection(DrawEmissionSection, "エミッション");
        SafeDrawSection(DrawVirtualExpressionSection, "バーチャル表現");
        SafeDrawSection(DrawAudioLinkSection, "AudioLink");
    }

    /// <summary>
    /// Draw Environment tab content
    /// </summary>
    private void DrawEnvironmentTab()
    {
        DrawExpandCollapseButtons((state) => {
            showReflection = state; showIridescence = state;
            showEnvironmentalRim = state; showRefraction = state;
        });
        SafeDrawSection(DrawReflectionSection, "リフレクション");
        SafeDrawSection(DrawIridescenceSection, "イリデッセンス");
        SafeDrawSection(DrawEnvironmentalRimSection, "環境リム");
        SafeDrawSection(DrawRefractionSection, "屈折");
    }

    /// <summary>
    /// Draw Advanced tab content
    /// </summary>
    private void DrawAdvancedTab()
    {
        DrawExpandCollapseButtons((state) => {
            showNormalMap = state; showParallax = state; showVertexAnimation = state; showVAT = state;
            showBackface = state; showVideo = state; showDistanceFade = state;
            showRendering = state;
        });
        // ─── マッピング ───
        NataneToonShaderGUIUtility.DrawCategoryDivider("マッピング");
        SafeDrawSection(DrawNormalMapSection, "ノーマルマップ");
        SafeDrawSection(DrawParallaxSection, "視差マッピング");

        // ─── アニメーション＆特殊 ───
        NataneToonShaderGUIUtility.DrawCategoryDivider("アニメーション＆特殊");
        SafeDrawSection(DrawVertexAnimationSection, "頂点アニメーション（風/呼吸/脈動）");
        SafeDrawSection(DrawVATSection, "VAT（頂点アニメーション）");
        SafeDrawSection(DrawTessellationSection, "テッセレーション（曲面スムージング）");
        SafeDrawSection(DrawBackfaceSection, "裏面テクスチャ");
        SafeDrawSection(DrawVideoSection, "ビデオテクスチャ");
        SafeDrawSection(DrawDistanceFadeSection, "距離フェード");

        // ─── レンダリング ───
        NataneToonShaderGUIUtility.DrawCategoryDivider("レンダリング");
        SafeDrawSection(DrawRenderingSection, "レンダリング");
    }

    /// <summary>
    /// Draw search results - shows only sections matching the query
    /// 検索結果を描画 - クエリにマッチするセクションのみ表示
    /// </summary>
    private void DrawSearchResults(string query)
    {
        string lowerQuery = query.ToLowerInvariant();
        bool anyMatch = false;

        for (int i = 0; i < sectionSearchData.Length; i++)
        {
            string methodKey = sectionSearchData[i][0];
            string japanese = sectionSearchData[i][1].ToLowerInvariant();
            string english = sectionSearchData[i][2].ToLowerInvariant();

            if (japanese.Contains(lowerQuery) || english.Contains(lowerQuery))
            {
                anyMatch = true;
                System.Action drawAction = GetSectionDrawAction(methodKey);
                if (drawAction != null)
                {
                    SafeDrawSection(drawAction, methodKey);
                }
            }
        }

        if (!anyMatch)
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox($"「{query}」に一致するセクションが見つかりませんでした。\nNo sections found matching \"{query}\".", MessageType.Info);
        }
    }

    /// <summary>
    /// Get the draw action for a section by its method key
    /// </summary>
    private System.Action GetSectionDrawAction(string methodKey)
    {
        switch (methodKey)
        {
            case "MainTexture": return DrawMainTextureSection;
            case "MakeupTextures": return DrawMakeupTexturesSection;
            case "Shading": return DrawShadingSection;
            case "AdvancedLighting": return DrawAdvancedLightingSection;
            case "AO": return DrawAOSection;
            case "Dithering": return DrawDitheringSection;
            case "LightVolume": return DrawLightVolumeSection;
            case "LTCGI": return DrawLTCGISection;
            case "Specular": return DrawSpecularSection;
            case "HairSpecular": return DrawHairSpecularSection;
            case "RimLight": return DrawRimLightSection;
            case "SSS": return DrawSSSSection;
            case "MatCap": return DrawMatCapSection;
            case "Glitter": return DrawGlitterSection;
            case "Drip": return DrawDripSection;
            case "Hologram": return DrawHologramSection;
            case "Decal": return DrawDecalSection;
            case "Outline": return DrawOutlineSection;
            case "Emission": return DrawEmissionSection;
            case "VirtualExpression": return DrawVirtualExpressionSection;
            case "AudioLink": return DrawAudioLinkSection;
            case "Reflection": return DrawReflectionSection;
            case "Iridescence": return DrawIridescenceSection;
            case "EnvironmentalRim": return DrawEnvironmentalRimSection;
            case "Refraction": return DrawRefractionSection;
            case "NormalMap": return DrawNormalMapSection;
            case "Parallax": return DrawParallaxSection;
            case "VertexAnimation": return DrawVertexAnimationSection;
            case "VAT": return DrawVATSection;
            case "Tessellation": return DrawTessellationSection;
            case "Backface": return DrawBackfaceSection;
            case "Video": return DrawVideoSection;
            case "DistanceFade": return DrawDistanceFadeSection;
            case "Rendering": return DrawRenderingSection;
            default: return null;
        }
    }

    /// <summary>
    /// 3.2 Quick Setup Section - Quick preset buttons for beginners
    /// </summary>
    private void DrawQuickSetupSection()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🎨 クイックセットアップ / Quick Setup", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("シャープなアニメ調\nSharp Anime Style", GUILayout.Height(40)))
        {
            Undo.RecordObject(targetMaterial, "Apply Sharp Anime Style");
            ApplySharpAnimeStyle(targetMaterial);
        }

        if (GUILayout.Button("柔らかい塗り調\nSoft Painting Style", GUILayout.Height(40)))
        {
            Undo.RecordObject(targetMaterial, "Apply Soft Painting Style");
            ApplySoftPaintingStyle(targetMaterial);
        }

        EditorGUILayout.EndHorizontal();

        // Surface finish buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("マット / Matte", GUILayout.Height(25)))
        {
            Undo.RecordObject(targetMaterial, "Apply Matte Surface");
            targetMaterial.SetFloat("_Glossiness", 0.0f);
            targetMaterial.SetFloat("_MatteEffect", 1.0f);
            EditorUtility.SetDirty(targetMaterial);
        }

        if (GUILayout.Button("グロッシー / Glossy", GUILayout.Height(25)))
        {
            Undo.RecordObject(targetMaterial, "Apply Glossy Surface");
            targetMaterial.SetFloat("_Glossiness", 1.0f);
            targetMaterial.SetFloat("_MatteEffect", 0.0f);
            EditorUtility.SetDirty(targetMaterial);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// Apply Sharp Anime Style preset (3.2 Quick Setup)
    /// </summary>
    private void ApplySharpAnimeStyle(Material mat)
    {
        mat.SetFloat("_ShadingMode", 0); // Toon
        mat.SetFloat("_ShadowSteps", 2);
        mat.SetFloat("_ShadowSharpness", 0.05f);
        mat.SetFloat("_ShadowBlend", 0);
        mat.SetFloat("_LightBlend", 0);
        mat.SetFloat("_AlbedoPreservation", 0.8f);
        mat.SetFloat("_FinalHighlightBlend", 0.3f);
        EditorUtility.SetDirty(mat);
    }

    /// <summary>
    /// Apply Soft Painting Style preset (3.2 Quick Setup)
    /// </summary>
    private void ApplySoftPaintingStyle(Material mat)
    {
        mat.SetFloat("_ShadingMode", 1); // Gradient
        mat.SetFloat("_ShadingGradientWidth", 0.3f);
        mat.SetFloat("_LitSoftness", 0.5f);
        mat.SetFloat("_ShadowBlend", 0.5f);
        mat.SetFloat("_LightBlend", 0.4f);
        mat.SetFloat("_AlbedoPreservation", 0.7f);
        EditorUtility.SetDirty(mat);
    }

    /// <summary>
    /// 3.3 Draw property with range indicator - Visual feedback for parameter values
    /// </summary>
    private void DrawPropertyWithRangeInfo(string propertyName, string label, string recommendedRange = null)
    {
        MaterialProperty property = FindProperty(propertyName, properties, false);
        if (property == null) return;

        // Draw main property
        materialEditor.ShaderProperty(property, label);

        // Show recommended range
        if (recommendedRange != null)
        {
            GUIStyle miniStyle = new GUIStyle(EditorStyles.miniLabel);
            miniStyle.normal.textColor = new Color(0.5f, 0.8f, 1.0f);
            EditorGUILayout.LabelField("推奨 / Recommended: " + recommendedRange, miniStyle);
        }

        // Draw visual bar for Range/Float properties
        if (property.type == MaterialProperty.PropType.Range || property.type == MaterialProperty.PropType.Float)
        {
            float value = property.floatValue;

            // For Range type properties
            if (property.type == MaterialProperty.PropType.Range)
            {
                float min = property.rangeLimits.x;
                float max = property.rangeLimits.y;
                float normalized = Mathf.InverseLerp(min, max, value);

                // Bar color (blue → orange gradient)
                Color barColor = Color.Lerp(
                    new Color(0.3f, 0.6f, 1.0f),
                    new Color(1.0f, 0.6f, 0.3f),
                    normalized
                );

                Rect barRect = EditorGUILayout.GetControlRect(false, 3);
                EditorGUI.DrawRect(
                    new Rect(barRect.x, barRect.y, barRect.width * normalized, barRect.height),
                    barColor
                );
            }
        }

        EditorGUILayout.Space(2);
    }

    /// <summary>
    /// Draw help toggle button and help box
    /// Returns true if help is shown
    /// </summary>
    /// <summary>
    /// Draw help toggle with collapsible preview (3.4 UX Improvement)
    /// </summary>
    private bool DrawHelpToggle(string sectionKey, string helpText, MessageType messageType = MessageType.Info)
    {
        // Get help state for this section (default: collapsed)
        string prefsKey = NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, "Help_" + sectionKey);
        bool showHelp = EditorPrefs.GetBool(prefsKey, false); // Default: false (collapsed)

        // Temporarily reset indent so help buttons are always left-aligned
        int savedIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        EditorGUILayout.BeginHorizontal();

        // Show preview when collapsed (left side)
        if (!showHelp && !string.IsNullOrEmpty(helpText))
        {
            string firstLine = helpText.Split('\n')[0];
            if (firstLine.Length > 60) firstLine = firstLine.Substring(0, 57) + "...";

            GUILayout.Label(firstLine, CachedHelpPreviewStyle);
        }

        // Push button to the right
        GUILayout.FlexibleSpace();

        // Help toggle button with icon (right-aligned)
        string icon = showHelp ? "▼" : "▶";
        string buttonLabel = showHelp ? "ヘルプを非表示" : "ヘルプを表示";
        GUIContent helpContent = new GUIContent($"{icon} {buttonLabel}", "クリックでヘルプを表示/非表示");

        if (GUILayout.Button(helpContent, EditorStyles.miniButton, GUILayout.Width(100)))
        {
            showHelp = !showHelp;
            EditorPrefs.SetBool(prefsKey, showHelp);
        }

        EditorGUILayout.EndHorizontal();

        // Show full help box when expanded
        if (showHelp && !string.IsNullOrEmpty(helpText))
        {
            EditorGUILayout.HelpBox(helpText, messageType);
            EditorGUILayout.Space(3);
        }

        // Restore indent
        EditorGUI.indentLevel = savedIndent;

        return showHelp;
    }

    /// <summary>
    /// Draw category header with description
    /// </summary>
    private void DrawCategoryHeader(string title, string description)
    {
        EditorGUILayout.Space(5);

        // Background box
        EditorGUILayout.BeginVertical(CachedCategoryBoxStyle);

        // Title with icon
        EditorGUILayout.LabelField("▣ " + title, CachedCategoryTitleStyle);

        // Description
        if (!string.IsNullOrEmpty(description))
        {
            EditorGUILayout.LabelField(description, CachedCategoryDescStyle);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(3);
    }

    /// <summary>
    /// Expand or collapse all sections
    /// </summary>
    private void ExpandAllSections(bool expand)
    {
        showPresets = expand;
        showPerformance = expand;
        showMainTexture = expand;
        showMakeupTextures = expand;
        showShading = expand;
        showAdvancedLighting = expand;
        showLightVolume = expand;
        showLTCGI = expand;
        showSpecular = expand;
        showRimLight = expand;
        showSSS = expand;
        showMatCap = expand;
        showGlitter = expand;
        showDrip = expand;
        showHologram = expand;
        showOutline = expand;
        showEmission = expand;
        showVirtualExpression = expand;
        showNormalMap = expand;
        showReflection = expand;
        showIridescence = expand;
        showEnvironmentalRim = expand;
        showParallax = expand;
        showRefraction = expand;
        showRendering = expand;
        showVertexAnimation = expand;
        showVAT = expand;

        SaveFoldoutStates();

        // Force repaint
        if (materialEditor != null)
        {
            materialEditor.Repaint();
        }
    }

    /// <summary>
    /// Called when a new shader is assigned to a material
    /// </summary>
    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        // Validate keywords after shader assignment
        targetMaterial = material;
        ValidateAndFixKeywords();
    }

    /// <summary>
    /// Validate and fix shader keywords based on property values
    /// This ensures keywords are in sync with material properties even if not toggled manually
    /// </summary>
    private void ValidateAndFixKeywords()
    {
        if (targetMaterial == null) return;

        // Define all toggle properties and their corresponding keywords
        // Property names must match exactly with [Toggle(_KEYWORD)] _PropertyName in .shader
        var keywordMappings = new (string propertyName, string keyword)[]
        {
            // Main Texture Animation
            ("_MainTexAnimation", "_MAIN_TEX_ANIMATION"),

            // Makeup Textures
            ("_Use2ndTexture", "_2ND_TEXTURE"),
            ("_Use3rdTexture", "_3RD_TEXTURE"),
            ("_Use4thTexture", "_4TH_TEXTURE"),
            ("_Use5thTexture", "_5TH_TEXTURE"),

            // Shading
            ("_UseRamp", "_USE_RAMP"),
            ("_UseMultiShadow", "_USE_MULTI_SHADOW"),
            ("_UseShadowReceiveMask", "_SHADOW_RECEIVE_MASK"),
            ("_UseAO", "_USE_AO"),
            ("_UseDithering", "_USE_DITHERING"),
            ("_UseSDFMap", "_SDF_MAP"),
            ("_UseGradeMap", "_SHADING_GRADE_MAP"),
            // Lighting
            ("_SoftLightingMode", "_SOFT_LIGHTING_MODE"),
            ("_UseLightVolume", "_USE_LIGHT_VOLUME"),
            ("_LightVolumeSpecular", "_LIGHT_VOLUME_SPECULAR"),
            ("_UsePixelVertexLights", "_PIXEL_VERTEX_LIGHTS"),

            // Specular
            ("_Specular", "_SPECULAR"),

            // Rim Light
            ("_RimLight", "_RIM_LIGHT"),
            ("_RimLight2", "_RIM_LIGHT_2"),
            ("_OffsetRimLight", "_OFFSET_RIM_LIGHT"),

            // SSS
            ("_SSS", "_SSS"),

            // MatCap
            ("_MatCap", "_MATCAP"),
            ("_MatCap2", "_MATCAP_2"),
            ("_MatCap3", "_MATCAP_3"),

            // Glitter
            ("_Glitter", "_GLITTER"),

            // Reflection
            ("_Reflection", "_REFLECTION"),

            // Iridescence
            ("_Iridescence", "_IRIDESCENCE"),

            // Environmental Rim
            ("_EnvRim", "_ENV_RIM"),

            // Outline
            ("_Outline", "_OUTLINE"),
            ("_OutlineTextureColor", "_OUTLINE_TEXTURE_COLOR"),
            ("_SmoothNormal", "_SMOOTH_NORMAL"),

            // Emission
            ("_Emission", "_EMISSION"),

            // Normal Map
            ("_UseNormalMap", "_NORMALMAP"),

            // Virtual Expression
            ("_Dissolve", "_DISSOLVE"),
            ("_UseAlphaMask", "_ALPHA_MASK"),
            ("_HueShiftEnable", "_HUE_SHIFT"),

            // Parallax
            ("_Parallax", "_PARALLAX"),

            // Refraction
            ("_Refraction", "_REFRACTION"),

            // AudioLink
            ("_AudioLink", "_AUDIOLINK"),

            // Distance Fade
            ("_DistanceFade", "_DISTANCE_FADE"),

            // Vertex Animation
            ("_VertexAnimation", "_VERTEX_ANIMATION"),

            // VAT
            ("_VAT", "_VAT"),
            ("_VATNormal", "_VAT_NORMAL"),

            // Hologram / Glitch
            ("_Hologram", "_HOLOGRAM"),
            ("_Glitch", "_GLITCH"),
            ("_UseHologramNoise", "_HOLOGRAM_NOISE"),

            // Decal
            ("_Decal", "_DECAL"),

            // Backface Texture
            ("_BackfaceTexture", "_BACKFACE_TEXTURE"),

            // Video Texture
            ("_VideoTexture", "_VIDEO_TEXTURE"),

            // LTCGI
            ("_LTCGI", "_LTCGI"),

            // Dithering Alpha
            ("_DitheringAlpha", "_DITHERING_ALPHA")
        };

        bool anyChanges = false;

        foreach (var mapping in keywordMappings)
        {
            // Check if property exists
            if (!targetMaterial.HasProperty(mapping.propertyName))
                continue;

            // Get current property value (0 or 1)
            float propertyValue = targetMaterial.GetFloat(mapping.propertyName);
            bool shouldBeEnabled = propertyValue >= 0.5f;

            // Get current keyword state
            bool isEnabled = targetMaterial.IsKeywordEnabled(mapping.keyword);

            // Fix if mismatch
            if (shouldBeEnabled != isEnabled)
            {
                if (shouldBeEnabled)
                    targetMaterial.EnableKeyword(mapping.keyword);
                else
                    targetMaterial.DisableKeyword(mapping.keyword);

                anyChanges = true;
            }
        }

        // Mark material as dirty if changes were made
        if (anyChanges)
        {
            EditorUtility.SetDirty(targetMaterial);
        }
    }
}
