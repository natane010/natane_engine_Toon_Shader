using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using NataneToon.Editor;
using static NataneToon.Editor.NataneToonLocalization;

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
    // --- UI Layout Constants ---
    /// <summary>Height of the main tab toolbar</summary>
    private const int TAB_HEIGHT = 30;

    /// <summary>Vertical spacing between sections</summary>
    private const int SECTION_SPACING = 5;

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
        Transparent = 2,
        Fur = 3,
        Background = 4
    }

    private static string[] RenderingModeLabels => new string[]
    {
        L("不透明", "Opaque"),
        L("カットアウト", "Cutout"),
        L("半透明", "Transparent"),
        L("ファー", "Fur"),
        L("背景 (Background)", "Background")
    };

    // ===== UI STATE =====
    // Tab index for category navigation
    private int selectedTab = 0;
    private string[] TabNames => new string[]
    {
        L("テクスチャ&色", "Texture & Color"), L("ライト&影", "Light & Shadow"), L("エフェクト", "Effects"), L("環境&反射", "Environment & Reflection"), L("詳細設定", "Advanced")
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
        new[] { "ScreenTone", "スクリーントーン 網点 ドット トーン", "screen tone halftone dot pattern overlay" },
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
        new[] { "Smear", "スミア 残像 ストレッチ トレイル グロー", "smear afterimage stretch trail glow" },
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
        new[] { "GradientBaseColor", "グラデーション ベースカラー 位置", "gradient base color position tint" },
        new[] { "HeightFade", "高さフェード ハイトフェード ローカル", "height fade local position transparency" },
        new[] { "IntersectionFade", "交差フェード 交差点 深度", "intersection fade depth contact" },
        new[] { "DistanceFade", "距離フェード", "distance fade lod" },
        new[] { "Stencil", "ステンシル マスク", "stencil mask buffer" },
        new[] { "Fur", "ファー 毛皮 シェル 毛 ケモ", "fur shell hair strand pelt" },
        new[] { "BackgroundLightmap", "ライトマップ 背景 ベイク GI", "lightmap background bake gi" },
        new[] { "PBR", "PBR 物理 メタリック スムーズネス 反射", "pbr metallic smoothness reflection probe" },
        new[] { "Rendering", "レンダリング設定 描画タイプ", "rendering mode opaque cutout transparent" },
        new[] { "DetailMap", "ディテールマップ セカンダリUV 詳細", "detail map secondary uv close-up" },
        new[] { "Triplanar", "トライプレーナー 3軸投影 UV不要", "triplanar projection no uv rock terrain" },
        new[] { "HeightFog", "ハイトフォグ 高さ霧 マテリアルフォグ", "height fog material fog mist atmosphere" },
        new[] { "SurfaceCover", "サーフェスカバー 雪 砂 堆積", "surface cover snow sand accumulation" },
        new[] { "MirrorControl", "ミラー VRChat 鏡", "mirror control vrchat reflection" },
        new[] { "QuestLite", "Quest軽量 モバイル パフォーマンス", "quest lite mobile performance optimization" },
    };

    // ===== SHADER TYPE DRAWER INSTANCES =====
    private NataneToon.Editor.NataneToonEyeDrawer eyeDrawer;
    private NataneToon.Editor.NataneToonWirelightDrawer wirelightDrawer;
    private NataneToon.Editor.NataneToonScreenFXDrawer screenFXDrawer;

    // ===== FOLDOUT STATE MANAGEMENT =====
    // Foldout states are per-material and persisted using EditorPrefs via Dictionary
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    // Mapping from foldout key to EditorPrefs suffix (preserves backward-compatible key names)
    // Most keys follow "Show" + key pattern, but some have legacy names
    private static readonly Dictionary<string, string> foldoutPrefsKeys = new Dictionary<string, string>
    {
        { "Presets", "ShowPresets" },
        { "Performance", "ShowPerformance" },
        { "MainTexture", "ShowBasic" },
        { "MakeupTextures", "ShowMakeupTextures" },
        { "Shading", "ShowShading" },
        { "AdvancedLighting", "ShowAdvancedLighting" },
        { "LightVolume", "ShowLightVolume" },
        { "Specular", "ShowSpecular" },
        { "HairSpecular", "ShowHairSpecular" },
        { "RimLight", "ShowRimLight" },
        { "SSS", "ShowSSS" },
        { "MatCap", "ShowMatCap" },
        { "Glitter", "ShowGlitter" },
        { "Drip", "ShowDrip" },
        { "Smear", "ShowSmear" },
        { "Hologram", "ShowHologram" },
        { "Outline", "ShowOutline" },
        { "Emission", "ShowEmission" },
        { "VirtualExpression", "ShowVirtualExpression" },
        { "NormalMap", "ShowNormalMap" },
        { "Reflection", "ShowReflection" },
        { "Iridescence", "ShowIridescence" },
        { "EnvironmentalRim", "ShowEnvironmentalRim" },
        { "Parallax", "ShowParallax" },
        { "Refraction", "ShowRefraction" },
        { "Rendering", "ShowAdvanced" },
        { "VertexAnimation", "ShowVertexAnimation" },
        { "VAT", "ShowVAT" },
        { "Tessellation", "ShowTessellation" },
        { "LTCGI", "ShowLTCGI" },
        { "AO", "ShowAO" },
        { "Dithering", "ShowDithering" },
        { "Decal", "ShowDecal" },
        { "Backface", "ShowBackface" },
        { "Video", "ShowVideo" },
        { "AudioLink", "ShowAudioLink" },
        { "DistanceFade", "ShowDistanceFade" },
        { "GradientBaseColor", "ShowGradientBaseColor" },
        { "HeightFade", "ShowHeightFade" },
        { "IntersectionFade", "ShowIntersectionFade" },
        { "FeatureOverview", "ShowFeatureOverview" },
        { "Fur", "ShowFur" },
        { "BackgroundLightmap", "ShowBackgroundLightmap" },
        { "PBR", "ShowPBR" },
    };

    // Default values: keys listed here default to true; all others default to false
    private static readonly HashSet<string> foldoutDefaultTrue = new HashSet<string>
    {
        "Presets", "Performance", "MainTexture", "Shading"
    };

    private bool GetFoldout(string key)
    {
        if (!foldoutStates.ContainsKey(key))
        {
            bool defaultVal = foldoutDefaultTrue.Contains(key);
            string prefsKey = foldoutPrefsKeys.ContainsKey(key) ? foldoutPrefsKeys[key] : "Show" + key;
            foldoutStates[key] = EditorPrefs.GetBool(
                NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, prefsKey), defaultVal);
        }
        return foldoutStates[key];
    }

    private void SetFoldout(string key, bool value)
    {
        foldoutStates[key] = value;
        string prefsKey = foldoutPrefsKeys.ContainsKey(key) ? foldoutPrefsKeys[key] : "Show" + key;
        EditorPrefs.SetBool(
            NataneToonMaterialPresetEditor.GetMaterialPrefsKey(targetMaterial, prefsKey), value);
    }

    // Foldout state initialization tracking
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
                EditorGUILayout.HelpBox(L("マテリアルエディタの初期化に失敗しました。", "Failed to initialize material editor."), MessageType.Error);
                return;
            }

            // Clear foldout cache on material change so GetFoldout re-reads EditorPrefs
            int currentMaterialId = this.targetMaterial.GetInstanceID();
            if (_lastLoadedMaterialInstanceId != currentMaterialId)
            {
                foldoutStates.Clear();
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

            // ===== Rendering Type Dropdown (below Shader Type) =====
            {
                RenderingMode currentMode = GetCurrentRenderingMode();
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup(L("描画タイプ", "Rendering Type"), (int)currentMode, RenderingModeLabels);
                if (EditorGUI.EndChangeCheck())
                {
                    SetRenderingMode((RenderingMode)newIndex);
                }
            }

            // Validate and fix shader keywords (ensures keywords match property values)
            ValidateAndFixKeywords();

            // ===== Compact Header =====
            DrawCompactHeader();

            // ===== Tab Navigation =====
            EditorGUI.BeginChangeCheck();
            selectedTab = GUILayout.Toolbar(selectedTab, TabNames, GUILayout.Height(TAB_HEIGHT));
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
            GUILayout.Label(L("検索:", "Search:"), GUILayout.Width(35));
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

            // Foldout states are persisted immediately via SetFoldout() - no batch save needed.
        }
        catch (ExitGUIException)
        {
            throw; // ExitGUIException is used internally by Unity IMGUI - must not be caught
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox(L($"インスペクターの描画中にエラーが発生しました: {e.Message}", $"An error occurred while drawing the inspector: {e.Message}"), MessageType.Error);
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
            EditorGUILayout.HelpBox(L($"{sectionName}セクションの描画中にエラーが発生しました: {e.Message}", $"Error drawing {sectionName} section: {e.Message}"), MessageType.Warning);
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
        SetFoldout("MainTexture", DrawBoxedSection(L("メインテクスチャ", "Main Texture"), GetFoldout("MainTexture"), SectionCategory.Basic));
        if (GetFoldout("MainTexture"))
        {
            DrawProperty("_MainTex", L("メインテクスチャ", "Main Texture"));
            DrawColorProperty("_Color", L("カラー", "Color"));

            // Main Texture Animation
            EditorGUILayout.Space(SECTION_SPACING);
            bool mainTexAnim = DrawToggle("_MAIN_TEX_ANIMATION", "_MainTexAnimation", L("メインテクスチャアニメーション", "Main Texture Animation"));
            if (mainTexAnim)
            {
                DrawUVAnimationSettings("_MainTexScrollSpeed", "_MainTexRotateSpeed", L("メインテクスチャ", "Main Texture"));
                DrawHelpToggle("MainTexAnimation",
                    L("📌 メインテクスチャアニメーション:\n" +
                    "メインテクスチャのUV座標をスクロール・回転させます。\n\n" +
                    "• スクロール速度 XY: X方向とY方向のスクロール速度\n" +
                    "• 回転速度: UV座標の回転速度（ラジアン/秒）\n\n" +
                    "💡 流水表現やホログラムパターンの移動に使用できます。",
                    "📌 Main Texture Animation:\n" +
                    "Scrolls and rotates the main texture UV coordinates.\n\n" +
                    "• Scroll Speed XY: Scroll speed in X and Y directions\n" +
                    "• Rotation Speed: UV rotation speed (radians/sec)\n\n" +
                    "💡 Useful for flowing water or moving hologram patterns."),
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(L("カラー保持・強化設定", "Color Preservation & Enhancement"), EditorStyles.boldLabel);

            DrawProperty("_AlbedoPreservation", L("テクスチャカラー保持", "Texture Color Preservation"));
            DrawHelpToggle("AlbedoPreservation",
                L("🎨 テクスチャカラー保持（改善版・色相維持）:\n" +
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
                "🎨 Texture Color Preservation (Improved, Hue-Preserving):\n" +
                "Preserves original texture colors while applying lighting brightness.\n" +
                "Instead of colors 'washing out to white', they 'brighten naturally'.\n\n" +
                "✨ Features:\n" +
                "  - Prevents dark colors from turning white under light\n" +
                "  - Adjusts only brightness while preserving hue & saturation\n" +
                "  - Specular and rim light respond naturally to color\n\n" +
                "🎛️ Recommended:\n" +
                "• 0 = Traditional lighting (colors shift easily)\n" +
                "• 0.7-0.9 = Strong preservation (dark colors stay natural) ★Recommended\n" +
                "• 1.0 = Full preservation (most natural colors)\n\n" +
                "💡 Effects:\n" +
                "  - Black clothes won't turn gray/white under light\n" +
                "  - Red clothes won't turn pink/white under light\n" +
                "  - Dark colors brighten while keeping their hue\n" +
                "  - Shadow/shading brightness fully maintained\n" +
                "  - Fixes Light Volume white-out issues\n\n" +
                "⚠️ Note:\n" +
                "High values reduce light color influence.\n" +
                "Use ~0.5 if you want colored lights to affect the material."),
                MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_Saturation", L("彩度", "Saturation"));
            DrawHelpToggle("Saturation",
                L("✨ 彩度調整:\n" +
                "最終カラーの彩度（色の鮮やかさ）を調整します。\n" +
                "• 0 = モノクロ（グレースケール）\n" +
                "• 1 = デフォルト（元の彩度）\n" +
                "• 1.5-2.0 = 鮮やかな色合い",
                "✨ Saturation Adjustment:\n" +
                "Adjusts the saturation (color vividness) of the final color.\n" +
                "• 0 = Monochrome (grayscale)\n" +
                "• 1 = Default (original saturation)\n" +
                "• 1.5-2.0 = Vivid colors"),
                MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_Brightness", L("全体明度", "Overall Brightness"));
            DrawHelpToggle("Brightness",
                L("💡 全体明度調整:\n" +
                "最終的な明るさを調整します。\n" +
                "• 0.5-0.9 = 暗めに\n" +
                "• 1.0 = デフォルト\n" +
                "• 1.1-2.0 = 明るめに\n" +
                "• 2.0-5.0 = 大幅に明るく（移行マテリアル補正用）",
                "💡 Overall Brightness:\n" +
                "Adjusts the final brightness.\n" +
                "• 0.5-0.9 = Darker\n" +
                "• 1.0 = Default\n" +
                "• 1.1-2.0 = Brighter\n" +
                "• 2.0-5.0 = Much brighter (for migration material correction)"),
                MessageType.Info);

            // 3.1 Parameter Interaction Warning: Color Preservation System
            float albedoPreservation = targetMaterial.GetFloat("_AlbedoPreservation");
            float saturation = targetMaterial.GetFloat("_Saturation");

            if (albedoPreservation > 0.7f && saturation > 1.3f)
            {
                EditorGUILayout.HelpBox(
                    L("⚠️ パラメータ相互作用の警告\n\n" +
                    "「テクスチャカラー保持」と「彩度」が両方とも高い値です。\n" +
                    "色が非常に鮮やかになりすぎる可能性があります。\n\n" +
                    "推奨:\n" +
                    "• カラー保持 > 0.7 なら、彩度は 0.8-1.2 に\n" +
                    "• 彩度 > 1.3 なら、カラー保持は 0.3-0.6 に",
                    "⚠️ Parameter Interaction Warning\n\n" +
                    "Both 'Albedo Preservation' and 'Saturation' are set to high values.\n" +
                    "This may result in overly vivid colors.\n\n" +
                    "Recommended:\n" +
                    "• If Albedo Preservation > 0.7, set Saturation to 0.8-1.2\n" +
                    "• If Saturation > 1.3, set Albedo Preservation to 0.3-0.6"),
                    MessageType.Warning);
            }

            // Final Color Blending Section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(L("最終カラーブレンディング", "Final Color Blending"), EditorStyles.boldLabel);
            DrawHelpToggle("FinalColorBlending",
                L("🎨 最終なじませ処理（白飛び・黒つぶれ防止）:\n" +
                "すべてのエフェクト適用後の最終段階で、明るすぎる部分と暗すぎる部分を\n" +
                "周囲となじませて、より自然で滑らかな見た目にします。",
                "🎨 Final Blending (Prevent Blow-out & Crush):\n" +
                "After all effects are applied, blends overly bright and dark areas\n" +
                "with surroundings for a more natural, smooth appearance."),
                MessageType.None);

            EditorGUILayout.Space();
            DrawProperty("_FinalHighlightBlend", L("ハイライトなじませ（白飛び防止）", "Highlight Blend (Prevent Blow-out)"));
            DrawProperty("_HighlightThreshold", L("ハイライト閾値", "Highlight Threshold"));
            DrawHelpToggle("HighlightBlend",
                L("✨ ハイライトなじませ（トーンマッピング搭載）:\n" +
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
                "✨ Highlight Blend (with Tone Mapping):\n" +
                "Naturally blends overly bright areas (highlights).\n\n" +
                "📊 Processing Flow (4 stages):\n" +
                "1️⃣ Smooth Shoulder Tone Mapping\n" +
                "   - Naturally compresses bright areas above 60% luminance\n" +
                "   - Prevents blow-out while preserving color vividness\n" +
                "2️⃣ Highlight Blend (above threshold)\n" +
                "   - 30% desaturation to suppress excessive saturation\n" +
                "   - Softens boundaries smoothly\n" +
                "3️⃣ Gentle Clamp (allows up to 1.05)\n" +
                "   - Avoids hard cutoff\n\n" +
                "🎛️ Parameters:\n" +
                "• Blend 0 = No effect, 1 = Maximum compression\n" +
                "• Threshold: Additional blend above this value (Recommended: 0.75)\n\n" +
                "💡 Recommended:\n" +
                "  - Normal: Blend 0.3-0.5, Threshold 0.75\n" +
                "  - Strong: Blend 0.7-0.9, Threshold 0.6\n" +
                "  - Maximum: Blend 1.0, Threshold 0.5"),
                MessageType.Info);

            EditorGUILayout.Space();
            DrawProperty("_FinalShadowBlend", L("シャドーなじませ（黒つぶれ防止）", "Shadow Blend (Prevent Crush)"));
            DrawProperty("_ShadowThreshold", L("シャドー閾値", "Shadow Threshold"));
            DrawHelpToggle("ShadowBlend",
                L("🌙 シャドーなじませ:\n" +
                "暗すぎる部分（シャドー）を周囲となじませます。\n" +
                "• なじませ 0 = 効果なし、1 = 最大\n" +
                "• 閾値: この値より暗い部分に適用（推奨: 0.25）\n\n" +
                "💡 効果:\n" +
                "　・黒つぶれを防止し、ディテールを保持\n" +
                "　・影の境界を柔らかく\n" +
                "　・わずかにシャドーを持ち上げて自然に",
                "🌙 Shadow Blend:\n" +
                "Blends overly dark areas (shadows) with surroundings.\n" +
                "• Blend 0 = No effect, 1 = Maximum\n" +
                "• Threshold: Applied to areas darker than this (Recommended: 0.25)\n\n" +
                "💡 Effects:\n" +
                "  - Prevents black crush, preserves detail\n" +
                "  - Softens shadow boundaries\n" +
                "  - Slightly lifts shadows for natural look"),
                MessageType.Info);

        }
        EndBoxedSection(GetFoldout("MainTexture"));
    }

    private void DrawSurfaceFinishSection()
    {
        // Glossiness control
        DrawProperty("_Glossiness", L("光沢度（グロッシネス）", "Glossiness"));
        DrawHelpToggle("Glossiness",
            L("✨ 光沢度（Glossiness）:\n" +
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
            "✨ Glossiness:\n" +
            "Controls the intensity of all reflective effects\n" +
            "(specular, reflection, rim light, etc.) at once.\n\n" +
            "• 0 = No gloss (fully matte)\n" +
            "• 0.5 = Medium gloss\n" +
            "• 1.0 = Maximum gloss (default)\n\n" +
            "💡 Affected effects:\n" +
            "  - Specular highlights\n" +
            "  - Rim Light (1 & 2)\n" +
            "  - Environmental Rim\n" +
            "  - MatCap\n" +
            "  - Cubemap Reflection\n" +
            "  - Light Volume Specular\n\n" +
            "※ Does not affect Emission"),
            MessageType.Info);

        EditorGUILayout.Space();

        // Matte Effect (additional reduction)
        DrawProperty("_MatteEffect", L("マット効果（追加の光沢抑制）", "Matte Effect (Additional Gloss Reduction)"));
        DrawHelpToggle("MatteEffect",
            L("🎨 マット効果:\n" +
            "光沢度に加えて、さらに光沢を減らすための\n" +
            "追加パラメータです。\n\n" +
            "• 0 = 光沢度のみで制御\n" +
            "• 0.5 = 光沢度の50%に減少\n" +
            "• 1.0 = 完全にマット（光沢ゼロ）\n\n" +
            "💡 使い方:\n" +
            "光沢度と組み合わせて、より細かい調整が可能です。\n" +
            "例: 光沢度0.8 × マット効果0.3 = 実質56%の光沢",
            "🎨 Matte Effect:\n" +
            "An additional parameter to further reduce gloss\n" +
            "on top of the Glossiness setting.\n\n" +
            "• 0 = Controlled by Glossiness only\n" +
            "• 0.5 = Reduced to 50% of Glossiness\n" +
            "• 1.0 = Fully matte (zero gloss)\n\n" +
            "💡 Usage:\n" +
            "Combine with Glossiness for finer control.\n" +
            "Example: Glossiness 0.8 x Matte 0.3 = effective 56% gloss"),
            MessageType.Info);
    }

    private void DrawMakeupTexturesSection()
    {
        SetFoldout("MakeupTextures", DrawBoxedSection(L("追加テクスチャ（2nd〜5th）", "Additional Textures (2nd-5th)"), GetFoldout("MakeupTextures"), SectionCategory.Basic));
        if (GetFoldout("MakeupTextures"))
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
            EditorGUILayout.Space(SECTION_SPACING);

            DrawHelpToggle("MakeupTextures",
                L("メイクアップテクスチャ（透過PNG対応・HSVカラー調整）\n" +
                "最大4つの追加テクスチャで合成。アルファチャンネルで適用範囲を制御。",
                "Makeup Textures (Transparent PNG, HSV Color Adjustment)\n" +
                "Composite up to 4 additional textures. Alpha channel controls application area."),
                MessageType.None);

            EditorGUILayout.Space(SECTION_SPACING);

            // Draw each makeup texture layer using helper method
            DrawMakeupTextureLayer("2nd", "2ND_TEXTURE", "2ND_TEX_MASK", L("ハイライト、アイシャドウ", "Highlights, Eye Shadow"));
            EditorGUILayout.Space(10);

            DrawMakeupTextureLayer("3rd", "3RD_TEXTURE", "3RD_TEX_MASK", L("チーク", "Blush"));
            EditorGUILayout.Space(10);

            DrawMakeupTextureLayer("4th", "4TH_TEXTURE", "4TH_TEX_MASK", L("グリッター、シャドウ", "Glitter, Shadow"));
            EditorGUILayout.Space(10);

            DrawMakeupTextureLayer("5th", "5TH_TEXTURE", "5TH_TEX_MASK", L("細部のハイライト、細部のシャドウ", "Detail Highlights, Detail Shadows"));
        }
        EndBoxedSection(GetFoldout("MakeupTextures"));
    }

    /// <summary>
    /// Helper method to draw a single makeup texture layer with all its properties
    /// Reduces code duplication for 2nd, 3rd, 4th, 5th textures
    /// </summary>
    private void DrawMakeupTextureLayer(string layerName, string textureKeyword, string maskKeyword, string usageHint)
    {
        bool useTexture = DrawToggle($"_{textureKeyword}", $"_Use{layerName}Texture", L($"{layerName} Textureを有効化", $"Enable {layerName} Texture"));

        if (useTexture)
        {
            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField(L($"{layerName} Texture設定", $"{layerName} Texture Settings"), EditorStyles.boldLabel);

            // Texture and HSV controls
            DrawProperty($"_{layerName}Tex", $"{layerName} Texture");
            DrawProperty($"_{layerName}TexHueShift", "Hue Shift");
            DrawProperty($"_{layerName}TexSaturation", "Saturation");
            DrawProperty($"_{layerName}TexValue", "Brightness");

            DrawHelpToggle("MakeupTextureHSV",
                L("💡 HSVカラー調整:\n" +
                "• Hue=0, Sat=1, Value=1 = テクスチャそのまま\n" +
                "• Hue Shift = 色相を変更（-0.5～0.5）\n" +
                "• Saturation = 彩度調整（0～2、1=元の彩度）\n" +
                "• Brightness = 明度調整（0～2、1=元の明度）",
                "💡 HSV Color Adjustment:\n" +
                "• Hue=0, Sat=1, Value=1 = Original texture\n" +
                "• Hue Shift = Change hue (-0.5 to 0.5)\n" +
                "• Saturation = Saturation adjustment (0-2, 1=original)\n" +
                "• Brightness = Brightness adjustment (0-2, 1=original)"),
                MessageType.None);

            // Intensity and blend mode
            DrawProperty($"_{layerName}TexIntensity", L("強度", "Intensity"));
            DrawProperty($"_{layerName}TexBlendMode", L("ブレンドモード", "Blend Mode"));

            DrawHelpToggle("MakeupTextureBlendMode",
                L($"ブレンドモード:\n" +
                "• Add: 加算（ハイライトに最適）\n" +
                $"• Multiply: 乗算（{usageHint}に最適）\n" +
                "• Overlay: オーバーレイ（自然なメイク）\n" +
                "• Screen: スクリーン（柔らかいハイライト）\n\n" +
                "💡 透過PNG対応：\n" +
                "テクスチャのアルファチャンネルが適用強度として使用されます",
                $"Blend Mode:\n" +
                "• Add: Additive (ideal for highlights)\n" +
                $"• Multiply: Multiply (ideal for {usageHint})\n" +
                "• Overlay: Overlay (natural makeup)\n" +
                "• Screen: Screen (soft highlights)\n\n" +
                "💡 Transparent PNG supported:\n" +
                "Texture alpha channel is used as application intensity"),
                MessageType.Info);

            // Optional mask
            EditorGUILayout.Space();
            DrawProperty($"_{layerName}TexMask", L($"{layerName} Texマスク", $"{layerName} Tex Mask"));
            DrawHelpToggle("MakeupTextureMask",
                L("白 = テクスチャ適用、黒 = 適用なし\n" +
                "マスクはアルファチャンネルと乗算されます",
                "White = Apply texture, Black = No application\n" +
                "Mask is multiplied with the alpha channel"),
                MessageType.Info);

            // UV Animation
            DrawUVAnimationSettings($"_{layerName}TexScrollSpeed", $"_{layerName}TexRotateSpeed", $"{layerName} Texture");
        }
    }

    private void DrawScreenToneSection()
    {
        SetFoldout("ScreenTone", DrawBoxedSection(L("スクリーントーン（網点オーバーレイ）", "Screen Tone (Halftone Overlay)"), GetFoldout("ScreenTone"), SectionCategory.Basic, "_SCREEN_TONE"));
        if (GetFoldout("ScreenTone"))
        {
            bool enableScreenTone = DrawToggle("_SCREEN_TONE", "_ScreenTone", L("スクリーントーンを有効化", "Enable Screen Tone"));
            if (enableScreenTone)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_ScreenToneColor", L("トーンカラー", "Tone Color"));
                DrawProperty("_ScreenToneMask", L("マスクテクスチャ", "Mask Texture"));
                DrawProperty("_ScreenToneScale", L("パターンサイズ（ドットの大きさ）", "Pattern Size (Dot Size)"));
                DrawProperty("_ScreenToneThreshold", L("ドット密度（0=なし〜1=全面）", "Dot Density (0=None to 1=Full)"));
                DrawHelpToggle("ScreenTone",
                    L("スクリーントーン（網点オーバーレイ）:\n" +
                    "漫画やイラスト調の網点パターンをモデル表面に適用します。\n\n" +
                    "・トーンカラー: 網点の色を指定\n" +
                    "・マスクテクスチャ: 白=表示、黒=非表示\n" +
                    "・パターンサイズ: 値が大きいほど網点が大きい（1-200）\n" +
                    "・ドット密度: 値が大きいほど網点が多い（0-1）\n\n" +
                    "影の境界ディザリングとは別の機能です。",
                    "Screen Tone (Halftone Overlay):\n" +
                    "Applies halftone dot patterns to the model surface for manga/illustration style.\n\n" +
                    "- Tone Color: Specifies halftone dot color\n" +
                    "- Mask Texture: White=Show, Black=Hide\n" +
                    "- Pattern Size: Larger = bigger dots (1-200)\n" +
                    "- Dot Density: Larger = more dots (0-1)\n\n" +
                    "This is separate from shadow boundary dithering."),
                    MessageType.Info);
                DrawBlendControls(materialEditor, targetMaterial, "_ScreenToneBlend", "_ScreenToneBlendMode", "_ScreenToneBlur");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("ScreenTone"));
    }

    private void DrawShadingSection()
    {
        SetFoldout("Shading", DrawBoxedSection(L("トゥーンシェーディング", "Toon Shading"), GetFoldout("Shading"), SectionCategory.Shading));
        if (GetFoldout("Shading"))
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
        EndBoxedSection(GetFoldout("Shading"));
    }

    private void DrawAdvancedLightingSection()
    {
        SetFoldout("AdvancedLighting", DrawBoxedSection(L("ライティング詳細", "Advanced Lighting"), GetFoldout("AdvancedLighting"), SectionCategory.Lighting));
        if (GetFoldout("AdvancedLighting"))
        {

            // Soft Lighting Mode
            EditorGUILayout.LabelField(L("ソフトライティングモード", "Soft Lighting Mode"), EditorStyles.boldLabel);
            bool softLightingMode = DrawToggle("_SOFT_LIGHTING_MODE", "_SoftLightingMode", L("ソフトライティングモード（グローバル）", "Soft Lighting Mode (Global)"));
            if (softLightingMode)
            {
                DrawProperty("_SoftLightingIntensity", L("ソフトライティング強度", "Soft Lighting Intensity"));
                DrawHelpToggle("SoftLightingMode",
                    L("🌟 ソフトライティングモード:\n" +
                    "すべてのライティングとシェーディングを柔らかくします。\n" +
                    "このモードを有効にすると、影の境界、ライトの強さ、\n" +
                    "ハイライトなどが全体的に柔らかく調整されます。\n\n" +
                    "• 強度 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
                    "• 強度 0.5-0.7 = かなり柔らかい\n" +
                    "• 強度 0.7-1.0 = 非常に柔らかい\n\n" +
                    "💡 イラスト調や水彩画風の柔らかな表現に最適です。",
                    "🌟 Soft Lighting Mode:\n" +
                    "Softens all lighting and shading globally.\n" +
                    "When enabled, shadow boundaries, light intensity,\n" +
                    "and highlights are all softened.\n\n" +
                    "• Intensity 0.3-0.5 = Moderate softness (Recommended)\n" +
                    "• Intensity 0.5-0.7 = Quite soft\n" +
                    "• Intensity 0.7-1.0 = Very soft\n\n" +
                    "💡 Ideal for illustration or watercolor-style soft expressions."),
                    MessageType.Info);
                EditorGUILayout.Space();
            }

            // Global Light Controls
            EditorGUILayout.LabelField(L("グローバルライト制御", "Global Light Controls"), EditorStyles.boldLabel);
            DrawProperty("_LightIntensity", L("ライト強度（グローバル）", "Light Intensity (Global)"));
            DrawHelpToggle("LightIntensity", L("全体的なライティングの強さを制御します。0 = ライトなし、1 = 標準、1.73 = 移行補正値、5 = 最大", "Controls overall lighting strength. 0 = No light, 1 = Standard, 1.73 = Migration correction, 5 = Maximum"), MessageType.Info);

            DrawProperty("_IndirectLightIntensity", L("間接光の強度", "Indirect Light Intensity"));
            DrawHelpToggle("IndirectLightIntensity", L("環境光やライトプローブからの間接照明の強さを制御します。", "Controls the intensity of indirect illumination from ambient light and light probes."), MessageType.Info);

            DrawProperty("_GIIntensity", L("GI強度（環境反射）", "GI Intensity (Environment Reflection)"));
            DrawHelpToggle("GIIntensity", L("環境反射（Light Probes/GI）の影響度を制御します。\n• 0 = 環境反射を完全に無効化（環境光の影響を受けない）\n• 0.5 = 環境反射を50%に軽減\n• 1 = 通常通り環境反射を適用\nVRChatで暗いワールドやライティングが強すぎるワールドで、見た目を安定させるために使用します。", "Controls the influence of environment reflection (Light Probes/GI).\n• 0 = Fully disable environment reflection\n• 0.5 = Reduce to 50%\n• 1 = Apply normally\nUse to stabilize appearance in dark or overly bright VRChat worlds."), MessageType.Info);

            // Indirect Lighting Controls
            DrawColorProperty("_IndirectLightMinColor", L("間接光の最低色", "Indirect Light Minimum Color"));
            DrawHelpToggle("IndirectLightMinColor", L("間接光の最低保証カラーです。\n暗いワールドでもキャラクターが真っ黒にならないよう、間接光の下限を設定します。\n• 黒 (0,0,0) = 制限なし（環境光に完全依存）\n• 暗いグレー = 最低限の明るさを保証\nNatural ライティングパイプラインで使用されます。", "Minimum guaranteed indirect light color.\nSets a lower bound so characters don't turn completely black in dark worlds.\n• Black (0,0,0) = No limit (fully depends on ambient)\n• Dark gray = Guarantees minimum brightness\nUsed in the Natural lighting pipeline."), MessageType.Info);

            DrawProperty("_ShadowEnvStrength", L("影への環境色反映", "Shadow Environment Color"));
            DrawHelpToggle("ShadowEnvStrength", L("影の色に環境光の色をどの程度反映するかを制御します。\n• 0 = 影は純粋に暗くなるだけ（従来の挙動）\n• 1 = 影に環境光の色が完全に反映される\n環境光の色味を影にも反映させることで、より自然なライティングを実現します。", "Controls how much ambient color is reflected in shadows.\n• 0 = Shadows simply darken (traditional behavior)\n• 1 = Ambient color fully reflected in shadows\nReflecting ambient color in shadows achieves more natural lighting."), MessageType.Info);

            DrawProperty("_LightColorInfluence", L("ライトカラー影響度", "Light Color Influence"));
            DrawHelpToggle("LightColorInfluence", L("ライトの色がマテリアルに与える影響を制御します。\n• 0 = ライトの色を無視（白色光として処理）\n• 1 = ライトの色を完全に反映\n• 0.5 = 中間（推奨）", "Controls how much light color affects the material.\n• 0 = Ignore light color (treat as white)\n• 1 = Fully reflect light color\n• 0.5 = Middle (Recommended)"), MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(L("シャドウ設定", "Shadow Settings"), EditorStyles.boldLabel);
            DrawProperty("_ShadowReceive", L("影の受け取り", "Shadow Receive"));
            DrawHelpToggle("ShadowReceive", L("他のオブジェクトからの影がこのマテリアルに与える影響を制御します。1 = 完全な影、0 = 影なし。", "Controls how shadows from other objects affect this material. 1 = Full shadow, 0 = No shadow."), MessageType.Info);

            DrawProperty("_ShadowSmoothing", L("影のスムージング", "Shadow Smoothing"));
            DrawHelpToggle("ShadowSmoothing",
                L("シャドウスムージング:\n" +
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
                "Shadow Smoothing:\n" +
                "Blends shadow jaggies and banding for smoother results.\n\n" +
                "[Multi-step Toon Shading]\n" +
                "Blends step boundaries into continuous gradations\n" +
                "to reduce visible steps.\n\n" +
                "[Shadow Map (Directional Light)]\n" +
                "Smooths shadow edges with PCF 9-tap.\n\n" +
                "[Shadow Map (Point/Spot Light)]\n" +
                "Blurs shadow edges with adaptive smoothing.\n\n" +
                "• 0 = No smoothing (Default)\n" +
                "• 0.1-0.3 = Light smoothing (Recommended)\n" +
                "• 0.5-1.0 = Heavy smoothing\n\n" +
                "Use when multi-step shadow banding is visible or\n" +
                "shadow edges appear jagged."),
                MessageType.Info);

            DrawProperty("_ShadowMaxDarkness", L("影の最大暗さ", "Shadow Maximum Darkness"));
            DrawHelpToggle("ShadowMaxDarkness", L("影の最小明るさです。0 = 完全に暗い、1 = 暗くならない。影が真っ黒になりすぎるのを防ぎます。", "Minimum shadow brightness. 0 = Fully dark, 1 = No darkening. Prevents shadows from becoming too black."), MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(L("ライトカラー制限", "Light Color Limits"), EditorStyles.boldLabel);
            DrawProperty("_LightColorMin", L("ライト色の下限", "Light Color Min"));
            DrawProperty("_LightColorMax", L("ライト色の上限", "Light Color Max"));
            DrawHelpToggle("LightColorLimits", L(
                "ライトカラーの値域をクランプします（lilToonの_LightMinLimit/_LightMaxLimitに相当）。\n" +
                "• 下限 (default=0): 暗いワールドでもキャラが最低限見える保証\n" +
                "  - 0.05 = VRChat推奨（真っ暗を防止）\n" +
                "• 上限 (default=1): 強いライトでテクスチャが白飛びするのを防止\n" +
                "  - 1.0 = ライト色がそのまま（標準）\n" +
                "  - 0.8 = やや抑えめ（白飛び防止）",
                "Clamps effective light color (equivalent to lilToon's _LightMinLimit/_LightMaxLimit).\n" +
                "• Min (default=0): Guarantees minimum visibility in dark worlds\n" +
                "  - 0.05 = VRChat recommended (prevents total darkness)\n" +
                "• Max (default=1): Prevents texture white-out under bright lights\n" +
                "  - 1.0 = Light color as-is (standard)\n" +
                "  - 0.8 = Slightly reduced (prevents blowout)"), MessageType.Info);
            DrawProperty("_MonochromeLighting", L("モノクロライティング", "Monochrome Lighting"));
            DrawHelpToggle("MonochromeLighting", L(
                "ライトカラーの色味を除去してグレースケール化します（lilToonの_MonochromeLightingに相当）。\n" +
                "• 0 = ライトの色をそのまま適用\n" +
                "• 1 = ライトの明るさのみ（色なし）\n" +
                "色付きライトでテクスチャの色味が変わりすぎる場合に使用します。",
                "Removes color tint from light and converts to grayscale (equivalent to lilToon's _MonochromeLighting).\n" +
                "• 0 = Apply light color as-is\n" +
                "• 1 = Light brightness only (no color)\n" +
                "Use when colored lights distort texture colors too much."), MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(L("ライト影響範囲", "Light Influence Range"), EditorStyles.boldLabel);
            DrawProperty("_LightMinInfluence", L("ライトの最小影響", "Light Min Influence"));
            DrawProperty("_LightMaxInfluence", L("ライトの最大影響", "Light Max Influence"));
            DrawHelpToggle("LightInfluenceRange", L("normalize後の輝度値の最小/最大を制御します。最小値は暗くなりすぎを防ぎ、最大値は露出オーバーを防ぎます。", "Controls min/max of luminance after normalize step. Min prevents excessive darkness, max prevents overexposure."), MessageType.Info);

            EditorGUILayout.Space();
            DrawBlendParameter(
                "_LightBlend",
                L("ライトのなじませ（柔らかさ）", "Light Blending (Softness)"),
                L("✨ ライトのなじませ調整:\n" +
                "ライティングの変化を周囲となじませて、より滑らかにします。\n" +
                "• 0 = シャープな変化（デフォルト）\n" +
                "• 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
                "• 0.7-1.0 = 非常に柔らかい変化\n\n" +
                "💡 ライトの強弱が急激すぎる場合に調整してください。",
                "✨ Light Blending Adjustment:\n" +
                "Blends lighting changes with surroundings for smoother results.\n" +
                "• 0 = Sharp changes (Default)\n" +
                "• 0.3-0.5 = Moderate softness (Recommended)\n" +
                "• 0.7-1.0 = Very soft changes\n\n" +
                "💡 Adjust when light intensity changes are too abrupt."));

            EditorGUILayout.Space();
            DrawBlendParameter(
                "_HighlightSoftness",
                L("ハイライトの柔らかさ", "Highlight Softness"),
                L("✨ ハイライトのなじませ調整:\n" +
                "明るい部分（ハイライト）を周囲となじませます。\n" +
                "• 0 = シャープなハイライト（デフォルト）\n" +
                "• 0.3-0.5 = 適度な柔らかさ（推奨）\n" +
                "• 0.7-1.0 = 非常に柔らかいハイライト\n\n" +
                "💡 ハイライトが強すぎる場合や、\n" +
                "より柔らかい印象が欲しい場合に調整してください。",
                "✨ Highlight Blending Adjustment:\n" +
                "Blends bright areas (highlights) with surroundings.\n" +
                "• 0 = Sharp highlights (Default)\n" +
                "• 0.3-0.5 = Moderate softness (Recommended)\n" +
                "• 0.7-1.0 = Very soft highlights\n\n" +
                "💡 Adjust when highlights are too strong or\n" +
                "you want a softer impression."));

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
                    L("⚠️ 過度なソフトネス警告\n\n" +
                    "複数のソフトネスパラメータが高い値です。\n" +
                    "シェーディングが非常にぼやけます。\n\n" +
                    "推奨:\n" +
                    "メインパラメータ1-2個を選び、\n" +
                    "他は 0.3 以下に抑えてください。",
                    "⚠️ Excessive Softness Warning\n\n" +
                    "Multiple softness parameters are set to high values.\n" +
                    "Shading will become very blurry.\n\n" +
                    "Recommended:\n" +
                    "Choose 1-2 main parameters and\n" +
                    "keep others below 0.3."),
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
            DrawProperty("_BacklightIntensity", L("逆光の強さ", "Backlight Intensity"));
            if (targetMaterial.GetFloat("_BacklightIntensity") > 0)
            {
                DrawColorProperty("_BacklightColor", L("逆光の色", "Backlight Color"));
                DrawHelpToggle("Backlight", L("逆光はオブジェクトの背後に光がある時に照明を追加し、リムライトのような効果を作ります。", "Backlight adds illumination when light comes from behind the object, creating a rim-light-like effect."), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_BacklightBlend", "_BacklightBlendMode", "_BacklightBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_BacklightDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }
            }

            EditorGUILayout.Space();
            DrawProperty("_AdditionalLightIntensity", L("追加ライトの強さ", "Additional Light Intensity"));
            DrawHelpToggle("AdditionalLight", L("追加ライト（ForwardAddパス）の強度を制御します。低い値は複数のライトを使用する際の明るくなりすぎを防ぎます。0 = 追加ライトなし、1 = 最大強度。", "Controls additional light (ForwardAdd pass) intensity. Lower values prevent over-brightness with multiple lights. 0 = No additional lights, 1 = Max intensity."), MessageType.Info);

            // --- Vertex Light Settings ---
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(L("頂点ライト設定", "Vertex Light Settings"), EditorStyles.boldLabel);

            bool enablePixelVertexLights = DrawToggle("_PIXEL_VERTEX_LIGHTS", "_UsePixelVertexLights", L("ピクセル精度の頂点ライト", "Pixel-Precision Vertex Lights"));
            DrawHelpToggle("PixelVertexLights",
                L("頂点ライト（最大4灯の追加Point/Spotライト）をフラグメントシェーダーでトゥーンスタイルに計算します。\n" +
                "品質は向上しますが、負荷が増加します。\n" +
                "オフの場合でも頂点ライトは通常精度（頂点補間）で処理されます。\n" +
                "※ ForwardBaseパスでのみ有効です。",
                "Calculates vertex lights (up to 4 additional Point/Spot lights) in toon-style in the fragment shader.\n" +
                "Quality improves but increases GPU load.\n" +
                "When off, vertex lights are still processed at normal precision (vertex interpolation).\n" +
                "※ Only active in ForwardBase pass."),
                MessageType.Info);
        }
        EndBoxedSection(GetFoldout("AdvancedLighting"));
    }

    private void DrawLightVolumeSection()
    {
        SetFoldout("LightVolume", DrawBoxedSection(L("VRC ライトボリューム", "VRC Light Volumes"), GetFoldout("LightVolume"), SectionCategory.Lighting, "_USE_LIGHT_VOLUME"));
        if (GetFoldout("LightVolume"))
        {

            // Show package detection status
            bool packageInstalled = NataneToon.Editor.VRCLightVolumesAutoDetector.IsPackageInstalled();
            if (packageInstalled)
            {
                EditorGUILayout.HelpBox(
                    L("VRC Light Volumes パッケージ: 検出済み\n" +
                    "LightVolumes.cginc を使用します（対応ワールドで自動的に動作）",
                    "VRC Light Volumes Package: Detected\n" +
                    "Using LightVolumes.cginc (works automatically in compatible worlds)"),
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    L("VRC Light Volumes パッケージ: 未検出\n" +
                    "バンドル版LightVolumes.cgincで全機能が利用可能です\n\n" +
                    "パッケージ版を使用する場合:\n" +
                    "VCC から red.sim.lightvolumes をインストールしてください\n" +
                    "https://redsim.github.io/vpmlisting/",
                    "VRC Light Volumes Package: Not Detected\n" +
                    "All features available via bundled LightVolumes.cginc\n\n" +
                    "To use the package version:\n" +
                    "Install red.sim.lightvolumes from VCC\n" +
                    "https://redsim.github.io/vpmlisting/"),
                    MessageType.Info);
            }

            EditorGUILayout.Space(3);

            bool enableLightVolume = DrawToggle("_USE_LIGHT_VOLUME", "_UseLightVolume", L("Light Volumeを有効化", "Enable Light Volume"));

            if (enableLightVolume)
            {
                EditorGUI.indentLevel++;
                DrawHelpToggle("LightVolumeIntro",
                    L("VRC Light Volumesはボクセルベースの次世代ライティングシステムです。\n" +
                    "対応ワールドで自動的に高品質な部分照明が適用されます。\n",
                    "VRC Light Volumes is a voxel-based next-generation lighting system.\n" +
                    "High-quality partial lighting is automatically applied in compatible worlds.\n") +
                    (packageInstalled
                        ? L("現在、本物のLightVolumes.cgincが使用されています。", "Currently using the real LightVolumes.cginc.")
                        : L("パッケージ未検出のため、バンドル版LightVolumes.cgincが使用されます。", "Package not detected; using bundled LightVolumes.cginc.")),
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_LightVolumeIntensity", L("Light Volumeの強さ", "Light Volume Intensity"));
                DrawHelpToggle("LightVolumeIntensity", L("Light Volumeライティングの強度を制御します。1 = 完全強度、0 = 無効。", "Controls Light Volume lighting intensity. 1 = Full strength, 0 = Disabled."), MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_LightVolumeBlendMode", L("ブレンドモード", "Blend Mode"));
                DrawHelpToggle("LightVolumeBlendMode",
                    L("Light Volumeブレンドモード:\n" +
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
                    "Light Volume Blend Mode:\n" +
                    "• Add(0): Integrate direct light to ambient, indirect as rim light\n" +
                    "  (Default, most natural)\n" +
                    "• Multiply(1): Multiply with existing lighting (darkening effect)\n" +
                    "• Replace(2): Replace with Light Volume (full control)\n" +
                    "• Natural(3): Participate in max() blend as indirect light.\n" +
                    "  Environment color correctly reflected on all meshes (Recommended)\n\n" +
                    "Add Mode:\n" +
                    "  Separates Light Volume direct and indirect light.\n" +
                    "  Indirect light creates natural rim effect for more dimensional look.\n" +
                    "  Shadow Receive Mask controls indirect light influence.\n\n" +
                    "Natural Mode:\n" +
                    "  Blends Light Volume lighting with indirect light min color via max().\n" +
                    "  Environment color uniformly reflected on all meshes for color consistency."),
                    MessageType.Info);

                EditorGUILayout.Space();
                bool enableSpecular = DrawToggle("_LIGHT_VOLUME_SPECULAR", "_LightVolumeSpecular", L("Light Volume スペキュラー", "Light Volume Specular"));
                if (enableSpecular)
                {
                    DrawHelpToggle("LightVolumeSpecular",
                        L("Light Volumeからカラースペキュラーを生成します。\n",
                        "Generates color specular from Light Volume.\n") +
                        (packageInstalled
                            ? L("本物のLightVolumeSpecular関数が使用されます。アバターに推奨。", "Using real LightVolumeSpecular function. Recommended for avatars.")
                            : L("バンドル版LightVolumeSpecularが使用されます。", "Using bundled LightVolumeSpecular.")),
                        MessageType.Info);
                }

                EditorGUILayout.Space();
                DrawHelpToggle("LightVolumeNotes",
                    L("注意：\n" +
                    "• ワールドとアバター両方が対応している必要があります\n" +
                    "• 非対応環境では自動的にUnityのライトプローブにフォールバックします\n" +
                    "• ハッシュタグ #VRCLightVolumesReady で対応ワールドを検索できます\n",
                    "Notes:\n" +
                    "• Both world and avatar must be compatible\n" +
                    "• Automatically falls back to Unity light probes in unsupported environments\n" +
                    "• Search compatible worlds with #VRCLightVolumesReady\n") +
                    (packageInstalled ? "" : L("• Tools > Natane > VRChat > VRC Light Volumes 再検出 で手動検出も可能です", "• Manual detection available via Tools > Natane > VRChat > VRC Light Volumes Re-detect")),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_LightVolumeBlend");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("LightVolume"));
    }

    private void DrawLTCGISection()
    {
        SetFoldout("LTCGI", DrawBoxedSection(L("LTCGI（リアルタイムエリアライト）", "LTCGI (Real-time Area Light)"), GetFoldout("LTCGI"), SectionCategory.Lighting, "_LTCGI"));
        if (GetFoldout("LTCGI"))
        {

            // Show package detection status
            bool packageInstalled = NataneToon.Editor.LTCGIAutoDetector.IsPackageInstalled();
            if (packageInstalled)
            {
                EditorGUILayout.HelpBox(
                    L("LTCGI パッケージ: 検出済み\n" +
                    "LTCGI.cginc を使用します（対応ワールドで自動的に動作）",
                    "LTCGI Package: Detected\n" +
                    "Using LTCGI.cginc (works automatically in compatible worlds)"),
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    L("LTCGI パッケージ: 未検出（フォールバック使用中）\n" +
                    "Unity SH + Reflection Probes による近似ライティングを使用します\n" +
                    "本物のLTCGIを使用するには: VCC から at.pimaker.ltcgi をインストール",
                    "LTCGI Package: Not Detected (Fallback in use)\n" +
                    "Using approximate lighting via Unity SH + Reflection Probes\n" +
                    "To use real LTCGI: Install at.pimaker.ltcgi from VCC"),
                    MessageType.Info);
            }

            EditorGUILayout.Space(3);

            bool enableLTCGI = DrawToggle("_LTCGI", "_LTCGI", L("LTCGIを有効化", "Enable LTCGI"));

            if (enableLTCGI)
            {
                EditorGUI.indentLevel++;

                if (packageInstalled)
                {
                    DrawHelpToggle("LTCGIIntro",
                        L("LTCGI は Linearly Transformed Cosines によるリアルタイムエリアライトシステムです。\n" +
                        "対応ワールドのスクリーンやエリアライトから自動的に照明を受けます。",
                        "LTCGI is a real-time area light system using Linearly Transformed Cosines.\n" +
                        "Automatically receives illumination from screens and area lights in compatible worlds."),
                        MessageType.Info);
                }
                else
                {
                    DrawHelpToggle("LTCGIIntro",
                        L("フォールバックモード:\n" +
                        "LTCGIパッケージが未インストールのため、Unity SH + Reflection Probes による近似実装を使用しています。\n" +
                        "エリアライトの局所性は再現されませんが、環境光ベースの照明寄与が得られます。\n\n" +
                        "本物のLTCGIを使用するには at.pimaker.ltcgi をインストールしてください。",
                        "Fallback Mode:\n" +
                        "LTCGI package is not installed, using approximate implementation via Unity SH + Reflection Probes.\n" +
                        "Area light locality is not reproduced, but ambient-based lighting contribution is available.\n\n" +
                        "Install at.pimaker.ltcgi to use real LTCGI."),
                        MessageType.Info);
                }

                EditorGUILayout.Space();
                DrawProperty("_LTCGIIntensity", L("LTCGI 強度", "LTCGI Intensity"));
                DrawHelpToggle("LTCGIIntensity", L("LTCGIライティングの全体的な強度を制御します。\n1 = 完全強度、0 = 無効。", "Controls overall LTCGI lighting intensity.\n1 = Full strength, 0 = Disabled."), MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_LTCGISpecular", L("LTCGI スペキュラー", "LTCGI Specular"));
                DrawHelpToggle("LTCGISpecular", L("LTCGIからのスペキュラー（反射光）の強度を制御します。\nエリアライトの映り込み表現に使用されます。", "Controls specular (reflected light) intensity from LTCGI.\nUsed for area light reflection rendering."), MessageType.Info);

                EditorGUILayout.Space();
                DrawHelpToggle("LTCGINotes",
                    L("注意：\n" +
                    "• ワールドにLTCGIが設定されている必要があります\n" +
                    "• LTCGIはスクリーン、エリアライト等のリアルタイム照明に対応\n" +
                    "• AudioLink対応ワールドでは音楽連動照明も可能\n" +
                    "• Tools > Natane > VRChat > LTCGI 再検出 で手動検出も可能です",
                    "Notes:\n" +
                    "• LTCGI must be set up in the world\n" +
                    "• LTCGI supports real-time illumination from screens, area lights, etc.\n" +
                    "• Music-linked lighting is possible in AudioLink-compatible worlds\n" +
                    "• Manual detection available via Tools > Natane > VRChat > LTCGI Re-detect"),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_LTCGIBlend", "_LTCGIBlendMode");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("LTCGI"));
    }

    private void DrawSpecularSection()
    {
        SetFoldout("Specular", DrawBoxedSection(L("スペキュラー反射", "Specular Reflection"), GetFoldout("Specular"), SectionCategory.Effects, "_SPECULAR"));
        if (GetFoldout("Specular"))
        {

            bool enableSpecular = DrawToggle("_SPECULAR", "_Specular", L("スペキュラーを有効化", "Enable Specular"));

            if (enableSpecular)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_SpecularColor", L("スペキュラーの色", "Specular Color"));
                DrawProperty("_SpecularSize", L("スペキュラーのサイズ", "Specular Size"));
                DrawProperty("_SpecularSoftness", L("スペキュラーの柔らかさ", "Specular Softness"));

                EditorGUILayout.Space();
                DrawProperty("_SpecularMask", L("スペキュラーマスク", "Specular Mask"));
                DrawHelpToggle("SpecularMask", L("白 = スペキュラーあり、黒 = スペキュラーなし", "White = Specular on, Black = Specular off"), MessageType.Info);
                DrawUVAnimationSettings("_SpecularMaskScrollSpeed", "_SpecularMaskRotateSpeed", L("スペキュラーマスク", "Specular Mask"));

                DrawBlendControls(materialEditor, targetMaterial, "_SpecularBlend", "_SpecularBlendMode", "_SpecularBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_SpecularDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Specular"));
    }

    private void DrawHairSpecularSection()
    {
        SetFoldout("HairSpecular", DrawBoxedSection(L("ヘアハイライト（Kajiya-Kay）", "Hair Highlight (Kajiya-Kay)"), GetFoldout("HairSpecular"), SectionCategory.Effects, "_HAIR_SPECULAR"));
        if (GetFoldout("HairSpecular"))
        {

            bool enableHairSpec = DrawToggle("_HAIR_SPECULAR", "_HairSpecular", L("ヘアスペキュラーを有効化", "Enable Hair Specular"));

            if (enableHairSpec)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField(L("プライマリローブ", "Primary Lobe"), EditorStyles.boldLabel);
                DrawColorProperty("_HairSpecColor1", L("プライマリスペキュラー色", "Primary Specular Color"));
                DrawProperty("_HairSpecShift1", L("プライマリタンジェントシフト", "Primary Tangent Shift"));
                DrawProperty("_HairSpecWidth1", L("プライマリスペキュラー幅", "Primary Specular Width"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("セカンダリローブ", "Secondary Lobe"), EditorStyles.boldLabel);
                DrawColorProperty("_HairSpecColor2", L("セカンダリスペキュラー色", "Secondary Specular Color"));
                DrawProperty("_HairSpecShift2", L("セカンダリタンジェントシフト", "Secondary Tangent Shift"));
                DrawProperty("_HairSpecWidth2", L("セカンダリスペキュラー幅", "Secondary Specular Width"));

                EditorGUILayout.Space(SECTION_SPACING);
                DrawProperty("_HairSpecIntensity", L("ヘアスペキュラー強度", "Hair Specular Intensity"));

                EditorGUILayout.Space(SECTION_SPACING);
                DrawProperty("_HairSpecMask", L("ヘアスペキュラーマスク", "Hair Specular Mask"));
                DrawProperty("_HairSpecShiftTex", L("シフトテクスチャ", "Shift Texture"));

                DrawBlendControls(materialEditor, targetMaterial, "_HairSpecBlend", "_HairSpecBlendMode", null);

                DrawHelpToggle("HairSpecular",
                    L("💇 ヘアスペキュラー（Kajiya-Kay）:\n" +
                    "髪の毛専用の異方性スペキュラーハイライトです。\n" +
                    "「天使の輪」（エンジェルリング）効果を再現します。\n\n" +
                    "• プライマリローブ: メインのハイライト\n" +
                    "• セカンダリローブ: 補助的なハイライト\n" +
                    "• タンジェントシフト: ハイライトの位置をずらす\n" +
                    "• シフトテクスチャ: ピクセルごとのシフト制御\n\n" +
                    "💡 使い方:\n" +
                    "髪のマテリアルに使用し、光源に応じたリアルな\n" +
                    "髪のハイライトを表現します。",
                    "💇 Hair Specular (Kajiya-Kay):\n" +
                    "Anisotropic specular highlights specifically for hair.\n" +
                    "Reproduces the 'Angel Ring' (halo) effect.\n\n" +
                    "• Primary Lobe: Main highlight\n" +
                    "• Secondary Lobe: Secondary highlight\n" +
                    "• Tangent Shift: Shifts highlight position\n" +
                    "• Shift Texture: Per-pixel shift control\n\n" +
                    "💡 Usage:\n" +
                    "Use on hair materials for realistic\n" +
                    "light-responsive hair highlights."),
                    MessageType.Info);

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_HairSpecDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("HairSpecular"));
    }

    private void DrawRimLightSection()
    {
        SetFoldout("RimLight", DrawBoxedSection(L("リムライト（輪郭光）", "Rim Light (Edge Light)"), GetFoldout("RimLight"), SectionCategory.Effects, "_RIM_LIGHT"));
        if (GetFoldout("RimLight"))
        {

            bool enableRimLight = DrawToggle("_RIM_LIGHT", "_RimLight", L("リムライトを有効化", "Enable Rim Light"));

            if (enableRimLight)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_RimColor", L("リムライトの色", "Rim Light Color"));
                DrawProperty("_RimPower", L("リムライトのパワー", "Rim Light Power"));
                DrawProperty("_RimIntensity", L("リムライトの強さ", "Rim Light Intensity"));

                EditorGUILayout.Space();
                DrawProperty("_RimSpread", L("リムの広がり（グロー）", "Rim Spread (Glow)"));
                DrawHelpToggle("RimSpread",
                    L("✨ リムの広がり（グロー効果）:\n" +
                    "リムライトを滲ませて広げ、柔らかく発光している\n" +
                    "ような効果を追加します。\n\n" +
                    "• 0 = 広がりなし（シャープなリム）\n" +
                    "• 0.3-0.5 = 適度な広がり（推奨）\n" +
                    "• 0.7-1.0 = 大きな広がり（強いグロー）",
                    "✨ Rim Spread (Glow Effect):\n" +
                    "Bleeds and spreads the rim light for a soft\n" +
                    "glowing effect.\n\n" +
                    "• 0 = No spread (sharp rim)\n" +
                    "• 0.3-0.5 = Moderate spread (Recommended)\n" +
                    "• 0.7-1.0 = Large spread (strong glow)"),
                    MessageType.None);

                EditorGUILayout.Space();
                DrawProperty("_RimMask", L("リムマスク", "Rim Mask"));
                DrawHelpToggle("RimMask", L("白 = リムライトあり、黒 = リムライトなし", "White = Rim light on, Black = Rim light off"), MessageType.Info);
                DrawUVAnimationSettings("_RimMaskScrollSpeed", "_RimMaskRotateSpeed", L("リムマスク", "Rim Mask"));

                DrawBlendControls(materialEditor, targetMaterial, "_RimBlend", "_RimBlendMode", "_RimBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_RimDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Rim Light 2
            bool enableRimLight2 = DrawToggle("_RIM_LIGHT_2", "_RimLight2", L("リムライト2を有効化（多段階リム）", "Enable Rim Light 2 (Multi-layer)"));

            if (enableRimLight2)
            {
                EditorGUI.indentLevel++;
                DrawHelpToggle("RimLight2",
                    L("🌟 多段階リムライト:\n" +
                    "2つのリムライトレイヤーを重ねることで、\n" +
                    "より複雑で立体的なリムライト表現が可能です。\n\n" +
                    "💡 おすすめ設定:\n" +
                    "リムライト1: 明るい色、低いパワー（内側の輪郭）\n" +
                    "リムライト2: 淡い色、高いパワー（外側の輪郭）",
                    "🌟 Multi-layer Rim Light:\n" +
                    "Layer two rim lights for more complex\n" +
                    "and three-dimensional rim light effects.\n\n" +
                    "💡 Recommended:\n" +
                    "Rim Light 1: Bright color, low power (inner contour)\n" +
                    "Rim Light 2: Soft color, high power (outer contour)"),
                    MessageType.None);

                DrawColorProperty("_RimColor2", L("リムライト2の色", "Rim Light 2 Color"));
                DrawProperty("_RimPower2", L("リムライト2のパワー", "Rim Light 2 Power"));
                DrawProperty("_RimIntensity2", L("リムライト2の強さ", "Rim Light 2 Intensity"));

                EditorGUILayout.Space();
                DrawProperty("_RimSpread2", L("リムの広がり2（グロー）", "Rim Spread 2 (Glow)"));
                DrawHelpToggle("RimSpread2",
                    L("✨ リムの広がり（グロー効果）:\n" +
                    "リムライト2を滲ませて広げます。",
                    "✨ Rim Spread (Glow Effect):\n" +
                    "Bleeds and spreads Rim Light 2."),
                    MessageType.None);

                EditorGUILayout.Space();
                DrawProperty("_RimMask2", L("リムマスク2", "Rim Mask 2"));
                DrawHelpToggle("RimMask2", L("白 = リムライトあり、黒 = リムライトなし", "White = Rim light on, Black = Rim light off"), MessageType.Info);
                DrawUVAnimationSettings("_RimMask2ScrollSpeed", "_RimMask2RotateSpeed", L("リムマスク2", "Rim Mask 2"));

                DrawBlendControls(materialEditor, targetMaterial, "_RimBlend2", "_RimBlendMode2", "_Rim2Blur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_Rim2DistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Offset Rim Light
            bool enableOffsetRimLight = DrawToggle("_OFFSET_RIM_LIGHT", "_OffsetRimLight", L("オフセットリムライトを有効化", "Enable Offset Rim Light"));

            if (enableOffsetRimLight)
            {
                EditorGUI.indentLevel++;
                DrawHelpToggle("OffsetRimLight",
                    L("🌙 オフセットリムライト:\n" +
                    "通常のリムライトとは異なり、リムの発生位置を\n" +
                    "特定の方向にオフセット（偏移）させます。\n" +
                    "2Dアニメのような指向性のあるハイライト表現が可能です。\n\n" +
                    "💡 おすすめ設定:\n" +
                    "• X=0.3, Y=0.1: 右上からの光を表現\n" +
                    "• ライト方向連動ON: 自動的にライトに追従\n" +
                    "• シャープネス=0.5: トゥーン調のくっきりリム",
                    "🌙 Offset Rim Light:\n" +
                    "Unlike regular rim light, offsets the rim\n" +
                    "occurrence position in a specific direction.\n" +
                    "Enables directional highlights like 2D anime.\n\n" +
                    "💡 Recommended:\n" +
                    "• X=0.3, Y=0.1: Light from upper right\n" +
                    "• Light Direction Link ON: Auto-follows light\n" +
                    "• Sharpness=0.5: Crisp toon-style rim"),
                    MessageType.None);

                DrawColorProperty("_OffsetRimColor", L("オフセットリムカラー", "Offset Rim Color"));
                DrawProperty("_OffsetRimPower", L("パワー（幅）", "Power (Width)"));
                DrawProperty("_OffsetRimIntensity", L("強度", "Intensity"));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(L("オフセット方向", "Offset Direction"), EditorStyles.boldLabel);
                DrawProperty("_OffsetRimOffsetX", L("X方向オフセット", "X Direction Offset"));
                DrawProperty("_OffsetRimOffsetY", L("Y方向オフセット", "Y Direction Offset"));
                DrawHelpToggle("OffsetRimDir",
                    L("📐 オフセット方向:\n" +
                    "ビュー空間でリムの発生位置をずらします。\n" +
                    "• X正 = 右方向にリム, X負 = 左方向\n" +
                    "• Y正 = 上方向にリム, Y負 = 下方向\n" +
                    "• 例: X=0.3, Y=0 で右側にリムが偏る",
                    "📐 Offset Direction:\n" +
                    "Shifts rim occurrence position in view space.\n" +
                    "• X+ = Rim to right, X- = to left\n" +
                    "• Y+ = Rim upward, Y- = downward\n" +
                    "• Example: X=0.3, Y=0 biases rim to the right"),
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_OffsetRimUseLightDir", L("ライト方向連動", "Light Direction Linked"));
                DrawProperty("_OffsetRimLightDirStrength", L("ライト方向の強さ", "Light Direction Strength"));
                DrawHelpToggle("OffsetRimLightDir",
                    L("💡 ライト方向連動:\n" +
                    "ONにすると、メインライトの方向に基づいて\n" +
                    "自動的にリムのオフセットが計算されます。\n" +
                    "手動オフセットと併用可能です。",
                    "💡 Light Direction Link:\n" +
                    "When ON, rim offset is automatically calculated\n" +
                    "based on main light direction.\n" +
                    "Can be used alongside manual offset."),
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_OffsetRimSharpness", L("シャープネス（トゥーン）", "Sharpness (Toon)"));
                DrawHelpToggle("OffsetRimSharpness",
                    L("✂ シャープネス:\n" +
                    "• 0 = 柔らかいグラデーション\n" +
                    "• 0.3-0.5 = 適度なトゥーン調（推奨）\n" +
                    "• 0.8-1.0 = くっきりセル調",
                    "✂ Sharpness:\n" +
                    "• 0 = Soft gradient\n" +
                    "• 0.3-0.5 = Moderate toon-style (Recommended)\n" +
                    "• 0.8-1.0 = Crisp cel-style"),
                    MessageType.Info);

                DrawProperty("_OffsetRimShadowMask", L("影マスク強度", "Shadow Mask Intensity"));
                DrawHelpToggle("OffsetRimShadowMask",
                    L("🌑 影マスク:\n" +
                    "影になっている部分のリムを抑制します。\n" +
                    "• 0 = 影でもリムが出る\n" +
                    "• 0.5 = 影部分で半減（推奨）\n" +
                    "• 1.0 = 影部分でリムが完全に消える",
                    "🌑 Shadow Mask:\n" +
                    "Suppresses rim in shadowed areas.\n" +
                    "• 0 = Rim appears even in shadow\n" +
                    "• 0.5 = Halved in shadow (Recommended)\n" +
                    "• 1.0 = Rim fully disappears in shadow"),
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_OffsetRimMask", L("マスクテクスチャ", "Mask Texture"));
                DrawHelpToggle("OffsetRimMask", L("白 = リムあり、黒 = リムなし", "White = Rim on, Black = Rim off"), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_OffsetRimBlend", "_OffsetRimBlendMode", "_OffsetRimBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_OffsetRimDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Rim Light Direction Masking (lilToon-style)
            DrawProperty("_RimDirStrength", L("ライト方向追従", "Light Direction Follow"));
            DrawProperty("_RimShadowMask", L("影マスク", "Shadow Mask"));
            DrawHelpToggle("RimDirMasking",
                L("💡 リムライト方向制御:\n" +
                "• ライト方向追従: ライトの方向にリムを追従させる強度\n" +
                "  0=全方向にリム、1=ライト側のみリム\n" +
                "• 影マスク: 影の部分でリムを抑制する強度\n" +
                "  0=影でもリム表示、1=影でリム消失\n\n" +
                "※ ポイントライト・スポットライトにも対応",
                "💡 Rim Light Direction Control:\n" +
                "• Light Direction Follow: Rim follows light direction\n" +
                "  0=Rim in all directions, 1=Rim only on lit side\n" +
                "• Shadow Mask: Suppresses rim in shadow\n" +
                "  0=Rim in shadow, 1=Rim disappears in shadow\n\n" +
                "※ Also supports Point/Spot lights"),
                MessageType.Info);

            EditorGUILayout.Space(SECTION_SPACING);

            // Rim Direction Control (manual)
            bool enableRimDirControl = DrawToggle("_RIM_DIRECTION_CONTROL", "_RimDirectionControl", L("手動方向制御を有効化", "Enable Manual Direction Control"));

            if (enableRimDirControl)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_RimLightDirection", L("リムライト方向", "Rim Light Direction"));
                DrawProperty("_RimDirectionRange", L("方向範囲", "Direction Range"));
                DrawHelpToggle("RimDirectionControl",
                    L("🧭 手動リム方向制御:\n" +
                    "リムライトの発生方向を手動で指定して制限します。\n" +
                    "ライトの位置に関係なく、特定の方向からのみ\n" +
                    "リムが見えるように制御します。\n\n" +
                    "• リムライト方向: リムを発生させる方向ベクトル\n" +
                    "• 方向範囲: 許容する角度の広さ（0=狭い、1=広い）",
                    "🧭 Manual Rim Direction Control:\n" +
                    "Manually specify and restrict rim light direction.\n" +
                    "Controls rim visibility from specific directions\n" +
                    "regardless of light position.\n\n" +
                    "• Rim Light Direction: Direction vector for rim generation\n" +
                    "• Direction Range: Allowed angle width (0=narrow, 1=wide)"),
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("RimLight"));
    }

    private void DrawSSSSection()
    {
        SetFoldout("SSS", DrawBoxedSection(L("半透明表現（SSS）", "Subsurface Scattering (SSS)"), GetFoldout("SSS"), SectionCategory.Effects, "_SSS"));
        if (GetFoldout("SSS"))
        {

            bool enableSSS = DrawToggle("_SSS", "_SSS", L("SSSを有効化", "Enable SSS"));

            if (enableSSS)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_SSSColor", L("SSSの色", "SSS Color"));
                DrawProperty("_SSSIntensity", L("SSSの強さ", "SSS Intensity"));
                DrawProperty("_SSSPower", L("SSSのパワー", "SSS Power"));
                DrawProperty("_SSSDistortion", L("SSSの歪み", "SSS Distortion"));

                EditorGUILayout.Space();
                DrawProperty("_ThicknessMap", L("厚さマップ", "Thickness Map"));
                DrawHelpToggle("ThicknessMap", L("白 = 薄い（SSSが強い）、黒 = 厚い（SSSが弱い）", "White = Thin (strong SSS), Black = Thick (weak SSS)"), MessageType.Info);

                DrawProperty("_ThicknessScale", L("厚さのスケール", "Thickness Scale"));

                EditorGUILayout.Space();
                DrawProperty("_SSSMask", L("SSSマスク", "SSS Mask"));
                DrawHelpToggle("SSSMask", L("白 = SSSあり、黒 = SSSなし", "White = SSS on, Black = SSS off"), MessageType.Info);

                DrawHelpToggle("SSSInfo", L("SSSはオブジェクトを通過する光をシミュレートします。肌、葉、薄い素材に最適です。", "SSS simulates light passing through objects. Ideal for skin, leaves, and thin materials."), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_SSSBlend", "_SSSBlendMode", "_SSSBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_SSSDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("SSS"));
    }

    private void DrawMatCapSection()
    {
        SetFoldout("MatCap", DrawBoxedSection(L("マットキャップ（MatCap）", "MatCap"), GetFoldout("MatCap"), SectionCategory.Effects, "_MATCAP"));
        if (GetFoldout("MatCap"))
        {

            bool enableMatCap = DrawToggle("_MATCAP", "_MatCap", L("MatCapを有効化", "Enable MatCap"));

            if (enableMatCap)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_MatCapTex", L("MatCapテクスチャ", "MatCap Texture"));
                DrawProperty("_MatCapIntensity", L("MatCapの強さ", "MatCap Intensity"));
                DrawProperty("_MatCapBlendMode", L("MatCapのブレンドモード", "MatCap Blend Mode"));

                EditorGUILayout.Space();
                DrawProperty("_MatCapMask", L("MatCapマスク", "MatCap Mask"));
                DrawHelpToggle("MatCapMask", L("白 = MatCapあり、黒 = MatCapなし", "White = MatCap on, Black = MatCap off"), MessageType.Info);

                DrawHelpToggle("MatCapInfo", L("MatCapテクスチャは球面反射マップである必要があります。", "MatCap textures must be spherical reflection maps."), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_MatCapBlend", null, "_MatCapBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_MatCapDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // MatCap 2
            bool enableMatCap2 = DrawToggle("_MATCAP_2", "_MatCap2", L("MatCap 2を有効化", "Enable MatCap 2"));

            if (enableMatCap2)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_MatCapTex2", L("MatCap 2 テクスチャ", "MatCap 2 Texture"));
                DrawProperty("_MatCapIntensity2", L("MatCap 2の強さ", "MatCap 2 Intensity"));
                DrawProperty("_MatCapBlendMode2", L("MatCap 2のブレンドモード", "MatCap 2 Blend Mode"));

                EditorGUILayout.Space();
                DrawProperty("_MatCapMask2", L("MatCap 2マスク", "MatCap 2 Mask"));
                DrawHelpToggle("MatCapMask2", L("白 = MatCap適用、黒 = 適用なし", "White = MatCap applied, Black = Not applied"), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_MatCapBlend2", null, "_MatCap2Blur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_MatCap2DistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // MatCap 3
            bool enableMatCap3 = DrawToggle("_MATCAP_3", "_MatCap3", L("MatCap 3を有効化", "Enable MatCap 3"));

            if (enableMatCap3)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_MatCapTex3", L("MatCap 3 テクスチャ", "MatCap 3 Texture"));
                DrawProperty("_MatCapIntensity3", L("MatCap 3の強さ", "MatCap 3 Intensity"));
                DrawProperty("_MatCapBlendMode3", L("MatCap 3のブレンドモード", "MatCap 3 Blend Mode"));

                EditorGUILayout.Space();
                DrawProperty("_MatCapMask3", L("MatCap 3マスク", "MatCap 3 Mask"));
                DrawHelpToggle("MatCapMask3", L("白 = MatCap適用、黒 = 適用なし", "White = MatCap applied, Black = Not applied"), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_MatCapBlend3", null, "_MatCap3Blur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_MatCap3DistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("MatCap"));
    }

    private void DrawGlitterSection()
    {
        SetFoldout("Glitter", DrawBoxedSection(L("グリッター（ラメ）", "Glitter"), GetFoldout("Glitter"), SectionCategory.Effects, "_GLITTER"));
        if (GetFoldout("Glitter"))
        {

            bool enableGlitter = DrawToggle("_GLITTER", "_Glitter", L("グリッターを有効化", "Enable Glitter"));

            if (enableGlitter)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("グリッター設定", "Glitter Settings"), EditorStyles.boldLabel);

                DrawColorProperty("_GlitterColor", L("グリッター色", "Glitter Color"));
                DrawProperty("_GlitterSize", L("グリッターサイズ", "Glitter Size"));
                DrawProperty("_GlitterDensity", L("グリッター密度", "Glitter Density"));
                DrawProperty("_GlitterSpeed", L("グリッター速度", "Glitter Speed"));
                DrawProperty("_GlitterIntensity", L("グリッター強度", "Glitter Intensity"));

                EditorGUILayout.Space(SECTION_SPACING);

                DrawProperty("_GlitterMask", L("グリッターマスク", "Glitter Mask"));
                DrawHelpToggle("GlitterMask",
                    L("グリッターマスクのR(赤)チャンネルを使用してグリッターの表示領域を制御します。\n" +
                    "• 白 (1.0): グリッターを完全に表示\n" +
                    "• 黒 (0.0): グリッターを非表示\n" +
                    "• グレー: 部分的に表示",
                    "Uses the R (red) channel of the glitter mask to control display area.\n" +
                    "• White (1.0): Fully show glitter\n" +
                    "• Black (0.0): Hide glitter\n" +
                    "• Gray: Partially show"),
                    MessageType.Info);
                DrawUVAnimationSettings("_GlitterMaskScrollSpeed", "_GlitterMaskRotateSpeed", L("グリッターマスク", "Glitter Mask"));

                EditorGUILayout.Space(SECTION_SPACING);
                DrawHelpToggle("GlitterInfo",
                    L("📍 グリッター:\n" +
                    "キラキラと輝くハイライト効果を追加します。\n\n" +
                    "• サイズ: グリッターの粒子サイズ\n" +
                    "• 密度: グリッターの出現頻度（0=少ない、1=多い）\n" +
                    "• 速度: グリッターの点滅速度\n" +
                    "• 強度: グリッターの明るさ\n\n" +
                    "💡 使い方:\n" +
                    "衣装やアクセサリーに華やかさを追加したい場合に使用します。",
                    "📍 Glitter:\n" +
                    "Adds sparkling highlight effects.\n\n" +
                    "• Size: Glitter particle size\n" +
                    "• Density: Glitter frequency (0=few, 1=many)\n" +
                    "• Speed: Glitter blink speed\n" +
                    "• Intensity: Glitter brightness\n\n" +
                    "💡 Usage:\n" +
                    "Use to add sparkle to costumes and accessories."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_GlitterBlend", "_GlitterBlendMode", "_GlitterBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_GlitterDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Glitter"));
    }

    private void DrawDripSection()
    {
        SetFoldout("Drip", DrawBoxedSection(L("雫エフェクト", "Drip Effect"), GetFoldout("Drip"), SectionCategory.Effects, "_WATER_DRIP"));
        if (GetFoldout("Drip"))
        {

            bool enableDrip = DrawToggle("_WATER_DRIP", "_WaterDrip", L("雫エフェクトを有効化", "Enable Drip Effect"));

            if (enableDrip)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("雫エフェクト設定", "Drip Effect Settings"), EditorStyles.boldLabel);

                DrawColorProperty("_DripColor", L("雫の色", "Drip Color"));
                DrawProperty("_DripSpeed", L("雫の速度", "Drip Speed"));
                DrawProperty("_DripDensity", L("雫の密度", "Drip Density"));
                DrawProperty("_DripSize", L("雫のサイズ", "Drip Size"));
                DrawProperty("_DripTrailLength", L("雫の尾の長さ", "Drip Trail Length"));
                DrawProperty("_DripIntensity", L("雫の強度", "Drip Intensity"));
                DrawProperty("_DripSharpness", L("雫のシャープさ", "Drip Sharpness"));

                EditorGUILayout.Space(SECTION_SPACING);

                DrawProperty("_DripMask", L("雫マスク", "Drip Mask"));
                DrawHelpToggle("DripMask",
                    L("雫マスクのR(赤)チャンネルを使用して雫の表示領域を制御します。\n" +
                    "• 白 (1.0): 雫を完全に表示\n" +
                    "• 黒 (0.0): 雫を非表示\n" +
                    "• グレー: 部分的に表示",
                    "Uses the R (red) channel of the drip mask to control display area.\n" +
                    "• White (1.0): Fully show drips\n" +
                    "• Black (0.0): Hide drips\n" +
                    "• Gray: Partially show"),
                    MessageType.Info);
                DrawUVAnimationSettings("_DripMaskScrollSpeed", "_DripMaskRotateSpeed", L("雫マスク", "Drip Mask"));

                EditorGUILayout.Space(SECTION_SPACING);
                DrawHelpToggle("DripInfo",
                    L("💧 雫エフェクト（ウォータードリップ）:\n" +
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
                    "💧 Drip Effect (Water Drip):\n" +
                    "Adds water droplet dripping down surfaces.\n\n" +
                    "• Speed: Drip falling speed\n" +
                    "• Density: Drip frequency (0=few, 1=many)\n" +
                    "• Size: Drip size\n" +
                    "• Tail Length: Length of water trail\n" +
                    "• Intensity: Effect brightness\n" +
                    "• Sharpness: Drip edge clarity\n\n" +
                    "💡 Usage:\n" +
                    "Ideal for rain-soaked clothing or glass surfaces.\n" +
                    "Use mask texture to limit drip areas."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_DripBlend", "_DripBlendMode", "_DripBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_DripDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Drip"));
    }

    private void DrawSmearSection()
    {
        SetFoldout("Smear", DrawBoxedSection(L("スミア（残像エフェクト）", "Smear (Afterimage Effect)"), GetFoldout("Smear"), SectionCategory.Effects, "_SMEAR"));
        if (GetFoldout("Smear"))
        {

            bool enableSmear = DrawToggle("_SMEAR", "_Smear", L("スミア (残像エフェクト) を有効化", "Enable Smear (Afterimage)"));

            if (enableSmear)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("スミア基本設定", "Smear Basic Settings"), EditorStyles.boldLabel);

                DrawProperty("_SmearStretch", L("ストレッチ量 (Auto時は最大値)", "Stretch Amount (Max when Auto)"));
                DrawProperty("_SmearDirection", L("スミア方向 (Animator制御)", "Smear Direction (Animator Control)"));
                DrawProperty("_SmearNoiseScale", L("ノイズスケール", "Noise Scale"));
                DrawProperty("_SmearNoiseStrength", L("ノイズ強度", "Noise Intensity"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("速度自動検出モード", "Speed Auto-Detection Mode"), EditorStyles.boldLabel);

                DrawProperty("_SmearAutoMagnitude", L("速度自動検出 (Auto Magnitude)", "Speed Auto-Detect (Auto Magnitude)"));
                DrawProperty("_SmearMotionSensitivity", L("モーション感度", "Motion Sensitivity"));

                // VAT Velocity toggle: only show when VAT is enabled
                if (targetMaterial.IsKeywordEnabled("_VAT"))
                {
                    DrawProperty("_SmearVATVelocity", L("VAT速度連動", "VAT Velocity Linked"));
                }

                DrawHelpToggle("SmearAutoMode",
                    L("🏃 スミア自動モード:\n" +
                    "「速度自動検出」をONにすると、_SmearDirectionベクトルの大きさから\n" +
                    "ストレッチ量を自動計算します。ストレッチ量は最大値として機能します。\n\n" +
                    "【VRChat】Animatorで VelocityX/Y/Z → _SmearDirection にマッピング\n" +
                    "【VAT連動】VAT有効時、アニメーションの動きから自動でスミア方向と強度を算出",
                    "🏃 Smear Auto Mode:\n" +
                    "When 'Auto Velocity Detection' is ON, stretch amount is\n" +
                    "auto-calculated from _SmearDirection vector magnitude.\n" +
                    "Stretch amount acts as maximum value.\n\n" +
                    "[VRChat] Map VelocityX/Y/Z to _SmearDirection in Animator\n" +
                    "[VAT Link] When VAT is enabled, auto-calculates smear direction and intensity from animation"),
                    MessageType.Info);

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("トレイル設定", "Trail Settings"), EditorStyles.boldLabel);

                DrawProperty("_SmearTrailLength", L("トレイル長", "Trail Length"));
                DrawProperty("_SmearTrailFade", L("トレイル減衰", "Trail Fade"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("グロー・エミッション", "Glow & Emission"), EditorStyles.boldLabel);

                DrawColorProperty("_SmearGlowColor", L("グローカラー", "Glow Color"));
                DrawProperty("_SmearGlowIntensity", L("グロー強度", "Glow Intensity"));
                DrawProperty("_SmearGlowPower", L("フレネルパワー", "Fresnel Power"));
                DrawProperty("_SmearEmission", L("エミッション強度", "Emission Intensity"));
                DrawColorProperty("_SmearEmissionColor", L("エミッションカラー", "Emission Color"));

                EditorGUILayout.Space(SECTION_SPACING);

                DrawProperty("_SmearMask", L("スミアマスク", "Smear Mask"));
                DrawHelpToggle("SmearMask",
                    L("スミアマスクのR(赤)チャンネルを使用してスミアの表示領域を制御します。\n" +
                    "• 白 (1.0): スミアを完全に表示\n" +
                    "• 黒 (0.0): スミアを非表示\n" +
                    "• グレー: 部分的に表示",
                    "Uses the R (red) channel of the smear mask to control display area.\n" +
                    "• White (1.0): Fully show smear\n" +
                    "• Black (0.0): Hide smear\n" +
                    "• Gray: Partially show"),
                    MessageType.Info);
                DrawUVAnimationSettings("_SmearMaskScrollSpeed", "_SmearMaskRotateSpeed", L("スミアマスク", "Smear Mask"));

                EditorGUILayout.Space(SECTION_SPACING);
                DrawHelpToggle("SmearInfo",
                    L("💨 スミア（残像エフェクト）:\n" +
                    "高速移動時の残像・ストレッチ表現を追加します。\n\n" +
                    "• ストレッチ量: 頂点の引き伸ばし量 (Auto時は最大値)\n" +
                    "• スミア方向: Animatorで制御する移動方向ベクトル\n" +
                    "• 速度自動検出: ベクトルの大きさからストレッチ量を自動計算\n" +
                    "• モーション感度: 速度→ストレッチ変換の感度\n" +
                    "• VAT速度連動: VATアニメーションの速度から自動でスミア駆動\n" +
                    "• ノイズ: ストレッチにランダムな歪みを加える\n" +
                    "• トレイル: 残像の長さと減衰\n" +
                    "• グロー: フレネルベースの光沢効果\n" +
                    "• エミッション: 残像部分の発光\n\n" +
                    "💡 使い方:\n" +
                    "アクションシーンやダッシュ演出に適しています。\n" +
                    "Animatorから_SmearDirectionを制御して動的な残像を表現できます。\n" +
                    "VRChatではVelocityX/Y/Zを_SmearDirectionにマッピングし、\n" +
                    "速度自動検出ONで移動速度に連動したスミアが実現できます。\n" +
                    "マスクテクスチャでスミアが適用される領域を制限できます。",
                    "💨 Smear (Afterimage Effect):\n" +
                    "Adds motion blur/stretch effects for fast movement.\n\n" +
                    "• Stretch Amount: Vertex stretch amount (max value in Auto mode)\n" +
                    "• Smear Direction: Movement direction vector controlled by Animator\n" +
                    "• Auto Velocity Detection: Auto-calculates stretch from vector magnitude\n" +
                    "• Motion Sensitivity: Velocity-to-stretch conversion sensitivity\n" +
                    "• VAT Velocity Link: Auto-drives smear from VAT animation velocity\n" +
                    "• Noise: Adds random distortion to stretch\n" +
                    "• Trail: Afterimage length and decay\n" +
                    "• Glow: Fresnel-based gloss effect\n" +
                    "• Emission: Afterimage glow\n\n" +
                    "💡 Usage:\n" +
                    "Ideal for action scenes and dash effects.\n" +
                    "Control _SmearDirection from Animator for dynamic afterimages.\n" +
                    "In VRChat, map VelocityX/Y/Z to _SmearDirection and\n" +
                    "enable Auto Velocity Detection for speed-linked smear.\n" +
                    "Use mask texture to limit smear areas."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_SmearBlend", "_SmearBlendMode", "_SmearBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_SmearDistFade", L("スミア距離フェード強度", "Smear Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Smear"));
    }

    private void DrawFurSection()
    {
        SetFoldout("Fur", DrawBoxedSection(L("ファー（シェルベース毛皮）", "Fur (Shell-Based)"), GetFoldout("Fur"), SectionCategory.Effects, "_FUR"));
        if (GetFoldout("Fur"))
        {
            bool enableFur = DrawToggle("_FUR", "_Fur", L("ファーを有効化", "Enable Fur"));

            // Auto shader switching: _FUR ON on non-Fur shader → switch to Fur variant
            if (enableFur && targetMaterial != null && targetMaterial.shader != null)
            {
                bool isFurShader = targetMaterial.shader.name.Contains("Fur");
                if (!isFurShader)
                {
                    SetRenderingMode(RenderingMode.Fur);
                }
            }

            if (enableFur)
            {
                EditorGUI.indentLevel++;

                // Performance warning
                EditorGUILayout.HelpBox(
                    L("ファーは非常にGPU負荷が高い機能です。\n" +
                    "16シェルパスにより描画コールが大幅に増加します。\n" +
                    "VRChat Questでは使用しないでください。",
                    "Fur is a very GPU-intensive feature.\n" +
                    "16 shell passes significantly increase draw calls.\n" +
                    "Do not use on VRChat Quest."),
                    MessageType.Warning);

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("基本設定", "Basic Settings"), EditorStyles.boldLabel);
                DrawProperty("_FurLength", L("ファーの長さ", "Fur Length"));
                DrawProperty("_FurDensity", L("ファー密度", "Fur Density"));
                DrawProperty("_FurAlphaCutoff", L("アルファカットオフ", "Alpha Cutoff"));
                DrawProperty("_FurNoiseTex", L("ノイズテクスチャ", "Noise Texture"));
                DrawProperty("_FurMask", L("ファーマスク", "Fur Mask"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("カラー", "Color"), EditorStyles.boldLabel);
                DrawColorProperty("_FurRootColor", L("根元カラー", "Root Color"));
                DrawColorProperty("_FurTipColor", L("先端カラー", "Tip Color"));
                DrawProperty("_FurColorBlend", L("メインカラー混合比", "Main Color Mix Ratio"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("物理", "Physics"), EditorStyles.boldLabel);
                DrawProperty("_FurGravity", L("重力", "Gravity"));
                DrawProperty("_FurWindStrength", L("風の強さ", "Wind Strength"));
                DrawProperty("_FurWindSpeed", L("風の速度", "Wind Speed"));
                DrawProperty("_FurWindDirection", L("風の方向", "Wind Direction"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("シェーディング", "Shading"), EditorStyles.boldLabel);
                DrawProperty("_FurAO", L("セルフオクルージョン", "Self Occlusion"));
                DrawProperty("_FurShadowStrength", L("セルフシャドウ強度", "Self Shadow Intensity"));
                DrawProperty("_FurSpecular", L("スペキュラ", "Specular"));
                DrawProperty("_FurRimLight", L("リムライト", "Rim Light"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("パフォーマンス", "Performance"), EditorStyles.boldLabel);
                DrawProperty("_FurLODDistance", L("LOD開始距離", "LOD Start Distance"));
                DrawProperty("_FurLODMinLayers", L("最小レイヤー数", "Minimum Layers"));

                DrawHelpToggle("Fur",
                    L("ファー（シェルベース毛皮）:\n\n" +
                    "メッシュを法線方向に複数レイヤーで押し出し、\n" +
                    "ノイズテクスチャでアルファカットオフすることで\n" +
                    "毛皮の外見を再現します。\n\n" +
                    "【基本設定】\n" +
                    "・ファーの長さ: 毛の長さ（0.01〜0.05推奨）\n" +
                    "・ファー密度: ノイズテクスチャのタイリング\n" +
                    "・アルファカットオフ: 低い値=密な毛、高い値=まばらな毛\n" +
                    "・ノイズテクスチャ: 毛の分布パターン\n" +
                    "・ファーマスク: 白=毛あり、黒=毛なし\n\n" +
                    "【カラー】\n" +
                    "・根元カラー: 毛の根元の色（暗め推奨）\n" +
                    "・先端カラー: 毛の先端の色\n" +
                    "・メインカラー混合比: メインテクスチャとの混合\n\n" +
                    "【物理】\n" +
                    "・重力: 毛が下に垂れる強さ\n" +
                    "・風: 風による揺れアニメーション\n\n" +
                    "【シェーディング】\n" +
                    "・セルフオクルージョン: 根元が暗くなる効果\n" +
                    "・セルフシャドウ: 毛の内部の影\n\n" +
                    "【パフォーマンス】\n" +
                    "・LOD距離: この距離からレイヤーを減らし始めます\n" +
                    "・最小レイヤー: 遠距離での最低レイヤー数\n\n" +
                    "※ 描画タイプを「ファー」に設定してください。\n" +
                    "※ ファーは16シェルパスを使用し、GPUに高負荷です。\n" +
                    "※ VRChat Questでは使用しないでください。",
                    "Fur (Shell-Based Fur):\n\n" +
                    "Extrudes mesh in normal direction across multiple layers,\n" +
                    "using noise texture alpha cutoff to reproduce\n" +
                    "the appearance of fur.\n\n" +
                    "[Basic Settings]\n" +
                    "- Fur Length: Hair length (0.01-0.05 recommended)\n" +
                    "- Fur Density: Noise texture tiling\n" +
                    "- Alpha Cutoff: Low=dense fur, High=sparse fur\n" +
                    "- Noise Texture: Fur distribution pattern\n" +
                    "- Fur Mask: White=fur, Black=no fur\n\n" +
                    "[Color]\n" +
                    "- Root Color: Color at fur base (darker recommended)\n" +
                    "- Tip Color: Color at fur tips\n" +
                    "- Main Color Mix: Blend with main texture\n\n" +
                    "[Physics]\n" +
                    "- Gravity: How much fur droops downward\n" +
                    "- Wind: Wind sway animation\n\n" +
                    "[Shading]\n" +
                    "- Self Occlusion: Darkening at fur base\n" +
                    "- Self Shadow: Shadow inside fur\n\n" +
                    "[Performance]\n" +
                    "- LOD Distance: Start reducing layers from this distance\n" +
                    "- Min Layers: Minimum layers at far distance\n\n" +
                    "* Set rendering type to 'Fur'.\n" +
                    "* Fur uses 16 shell passes and is GPU-intensive.\n" +
                    "* Do not use on VRChat Quest."),
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Fur"));
    }

    private void DrawBackgroundLightmapSection()
    {
        RenderingMode currentMode = GetCurrentRenderingMode();
        if (currentMode != RenderingMode.Background) return;

        SetFoldout("BackgroundLightmap", DrawBoxedSection(L("背景ライトマップ設定", "Background Lightmap Settings"), GetFoldout("BackgroundLightmap"), SectionCategory.Lighting));
        if (GetFoldout("BackgroundLightmap"))
        {
            EditorGUI.indentLevel++;
            DrawProperty("_LightmapToonInfluence", L("ライトマップ トゥーン影響度", "Lightmap Toon Influence"));
            DrawProperty("_LightmapIntensity", L("ライトマップ明度", "Lightmap Brightness"));
            EditorGUILayout.HelpBox(
                L("ライトマップを使用するには Mesh Renderer の \"Contribute GI\" を有効にしてベイクしてください。\n" +
                "0=リアルGI, 0.5=ハイブリッド推奨, 1=フルトゥーン",
                "To use lightmaps, enable \"Contribute GI\" on the Mesh Renderer and bake.\n" +
                "0=Real GI, 0.5=Hybrid (Recommended), 1=Full Toon"),
                MessageType.Info);
            EditorGUI.indentLevel--;
        }
        EndBoxedSection(GetFoldout("BackgroundLightmap"));
    }

    private void DrawPBRSection()
    {
        RenderingMode currentMode = GetCurrentRenderingMode();
        if (currentMode != RenderingMode.Background) return;

        SetFoldout("PBR", DrawBoxedSection(L("PBR マテリアル設定", "PBR Material Settings"), GetFoldout("PBR"), SectionCategory.Lighting, "_PBR"));
        if (GetFoldout("PBR"))
        {
            bool enablePBR = DrawToggle("_PBR", "_EnablePBR", L("PBR ライティング", "PBR Lighting"));
            if (enablePBR)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_PBR_MetallicGlossMap", L("メタリック/スムーズネスマップ", "Metallic/Smoothness Map"));
                DrawProperty("_PBR_Metallic", L("メタリック", "Metallic"));
                DrawProperty("_PBR_Smoothness", L("スムーズネス", "Smoothness"));
                EditorGUILayout.Space(SECTION_SPACING);
                DrawProperty("_PBR_OcclusionMap", L("アンビエントオクルージョンマップ", "Ambient Occlusion Map"));
                DrawProperty("_PBR_OcclusionStrength", L("AO 強度", "AO Intensity"));
                EditorGUILayout.Space(SECTION_SPACING);
                DrawProperty("_PBR_ReflectionIntensity", L("リフレクションプローブ強度", "Reflection Probe Intensity"));
                EditorGUILayout.HelpBox(
                    L("PBR はトゥーンシェーディングの上に物理ベースの反射を加えます。\n" +
                    "金属/光沢テクスチャで質感を表現できます。",
                    "PBR adds physically-based reflections on top of toon shading.\n" +
                    "Express material quality with metallic/smoothness textures."),
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("PBR"));
    }

    private void DrawHologramSection()
    {
        SetFoldout("Hologram", DrawBoxedSection(L("ホログラム / グリッチ", "Hologram / Glitch"), GetFoldout("Hologram"), SectionCategory.Effects, "_HOLOGRAM"));
        if (GetFoldout("Hologram"))
        {

            bool enableHolo = DrawToggle("_HOLOGRAM", "_Hologram", L("ホログラムを有効化", "Enable Hologram"));
            if (enableHolo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("ホログラム基本設定", "Hologram Basic Settings"), EditorStyles.boldLabel);
                DrawColorProperty("_HologramColor", L("ホログラム色", "Hologram Color"));
                DrawProperty("_HologramMonochrome", L("モノクロ化", "Monochrome"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("スキャンライン", "Scanlines"), EditorStyles.boldLabel);
                DrawProperty("_HologramScanlineSpeed", L("スキャンライン速度", "Scanline Speed"));
                DrawProperty("_HologramScanlineIntensity", L("スキャンライン強度", "Scanline Intensity"));
                DrawProperty("_HologramScanlineDensity", L("スキャンライン密度", "Scanline Density"));
                DrawProperty("_HologramScanlineWidth", L("スキャンライン幅", "Scanline Width"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("エッジグロウ (Fresnel)", "Edge Glow (Fresnel)"), EditorStyles.boldLabel);
                DrawProperty("_HologramEdgeGlowPower", L("エッジグロウ範囲", "Edge Glow Range"));
                DrawProperty("_HologramEdgeGlowIntensity", L("エッジグロウ強度", "Edge Glow Intensity"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("透明度・フリッカー", "Transparency & Flicker"), EditorStyles.boldLabel);
                DrawProperty("_HologramAlpha", L("ホログラム透明度", "Hologram Alpha"));
                DrawProperty("_HologramFlickerSpeed", L("フリッカー速度", "Flicker Speed"));
                DrawProperty("_HologramFlickerAmount", L("フリッカー量", "Flicker Amount"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("ノイズ歪み", "Noise Distortion"), EditorStyles.boldLabel);
                DrawProperty("_HologramNoiseIntensity", L("ノイズ強度", "Noise Intensity"));
                DrawProperty("_HologramNoiseSpeed", L("ノイズ速度", "Noise Speed"));

                DrawProperty("_HologramMask", L("ホログラムマスク", "Hologram Mask"));
                DrawUVAnimationSettings("_HologramMaskScrollSpeed", "_HologramMaskRotateSpeed", L("ホログラムマスク", "Hologram Mask"));

                bool useNoiseTex = DrawToggle("_HOLOGRAM_NOISE", "_UseHologramNoise", L("ノイズテクスチャを使用", "Use Noise Texture"));
                if (useNoiseTex)
                    DrawProperty("_HologramNoiseTex", L("ノイズテクスチャ", "Noise Texture"));

                DrawBlendControls(materialEditor, targetMaterial, "_HologramBlend", "_HologramBlendMode", "_HologramBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_HologramDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(SECTION_SPACING);
            bool enableGlitch = DrawToggle("_GLITCH", "_Glitch", L("グリッチを有効化", "Enable Glitch"));
            if (enableGlitch)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("グリッチ設定", "Glitch Settings"), EditorStyles.boldLabel);
                DrawProperty("_GlitchIntensity", L("グリッチ強度", "Glitch Intensity"));
                DrawProperty("_GlitchSpeed", L("グリッチ速度", "Glitch Speed"));
                DrawProperty("_GlitchBlockSize", L("ブロックサイズ", "Block Size"));
                DrawProperty("_GlitchRGBSplitIntensity", L("RGBスプリット強度", "RGB Split Intensity"));
                DrawProperty("_GlitchFrequency", L("グリッチ発生頻度", "Glitch Frequency"));

                DrawBlendControls(materialEditor, targetMaterial, "_GlitchBlend", "_GlitchBlendMode", "_GlitchBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_GlitchDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(SECTION_SPACING);
            DrawHelpToggle("HologramInfo",
                L("🔷 ホログラム＆グリッチ:\n" +
                "SF/サイバーパンク風のホログラム表現を追加します。\n\n" +
                "• スキャンライン: 3層構成の走査線エフェクト\n" +
                "• エッジグロウ: Fresnelベースの縁光り\n" +
                "• モノクロ化: 色をホログラム色に統一\n" +
                "• 透明度: Fresnel連動の自動透明化\n" +
                "• グリッチ: ランダムなUV歪み＋RGB色ずれ\n\n" +
                "💡 Transparent バリアントとの組み合わせで\n" +
                "よりリアルなホログラム投影を実現できます。",
                "🔷 Hologram & Glitch:\n" +
                "Adds sci-fi/cyberpunk hologram effects.\n\n" +
                "• Scanlines: 3-layer scanline effect\n" +
                "• Edge Glow: Fresnel-based edge glow\n" +
                "• Monochrome: Unify colors to hologram tint\n" +
                "• Transparency: Fresnel-linked auto transparency\n" +
                "• Glitch: Random UV distortion + RGB shift\n\n" +
                "💡 Combine with Transparent variant for\n" +
                "more realistic hologram projection."),
                MessageType.Info);
        }
        EndBoxedSection(GetFoldout("Hologram"));
    }

    private void DrawOutlineSection()
    {
        SetFoldout("Outline", DrawBoxedSection(L("アウトライン（輪郭線）", "Outline (Contour)"), GetFoldout("Outline"), SectionCategory.Effects, "_OUTLINE"));
        if (GetFoldout("Outline"))
        {

            bool enableOutline = DrawToggle("_OUTLINE", "_Outline", L("アウトラインを有効化", "Enable Outline"));

            if (enableOutline)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("アウトライン設定", "Outline Settings"), EditorStyles.boldLabel);

                DrawProperty("_OutlineMode", L("描画方法", "Draw Method"));
                DrawProperty("_OutlineWidth", L("アウトラインの幅", "Outline Width"));
                DrawColorProperty("_OutlineColor", L("アウトラインの色", "Outline Color"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("アウトラインマスク", "Outline Mask"), EditorStyles.boldLabel);

                DrawProperty("_OutlineMask", L("アウトラインマスク (R)", "Outline Mask (R)"));
                DrawHelpToggle("OutlineMask",
                    L("アウトラインマスクのR(赤)チャンネルを使用してアウトラインの表示を制御します。\n" +
                    "• 白 (1.0): アウトラインを完全に表示\n" +
                    "• 黒 (0.0): アウトラインを非表示\n" +
                    "• グレー: 部分的に表示",
                    "Uses the R (red) channel of the outline mask to control outline display.\n" +
                    "• White (1.0): Fully show outline\n" +
                    "• Black (0.0): Hide outline\n" +
                    "• Gray: Partially show"),
                    MessageType.Info);

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("アウトライン幅マップ", "Outline Width Map"), EditorStyles.boldLabel);

                DrawProperty("_OutlineWidthMap", L("アウトライン幅マップ (R)", "Outline Width Map (R)"));
                DrawHelpToggle("OutlineWidthMap",
                    L("アウトライン幅マップのR(赤)チャンネルを使用してアウトラインの幅を制御します。\n" +
                    "• 白 (1.0): 通常の幅\n" +
                    "• 黒 (0.0): 幅ゼロ（アウトライン非表示）\n" +
                    "• グレー: 部分的な幅\n\n" +
                    "💡 使い方:\n" +
                    "部位ごとにアウトラインの太さを調整したい場合に使用します。\n" +
                    "例: 顔は細く、体は太くなど。",
                    "Uses the R (red) channel of the outline width map to control outline width.\n" +
                    "• White (1.0): Normal width\n" +
                    "• Black (0.0): Zero width (no outline)\n" +
                    "• Gray: Partial width\n\n" +
                    "💡 Usage:\n" +
                    "Use to adjust outline thickness per body part.\n" +
                    "Example: Thin on face, thick on body."),
                    MessageType.Info);

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("マルチカラーアウトライン", "Multi-Color Outline"), EditorStyles.boldLabel);

                DrawColorProperty("_OutlineColor2", L("アウトラインの色 2", "Outline Color 2"));
                DrawProperty("_OutlineColorMix", L("カラーミックス", "Color Mix"));
                DrawHelpToggle("OutlineMultiColor",
                    L("📍 マルチカラーアウトライン:\n" +
                    "2色のグラデーションアウトラインを作成します。\n\n" +
                    "• 色 2: 2番目の色\n" +
                    "• カラーミックス: 2色の混合度（0-1）\n\n" +
                    "💡 使い方:\n" +
                    "アウトラインにグラデーションやアニメーション効果を追加したい場合に使用します。",
                    "📍 Multi-Color Outline:\n" +
                    "Creates a 2-color gradient outline.\n\n" +
                    "• Color 2: Second color\n" +
                    "• Color Mix: Blend between 2 colors (0-1)\n\n" +
                    "💡 Usage:\n" +
                    "Use to add gradient or animation effects to outlines."),
                    MessageType.Info);

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("テクスチャカラーアウトライン", "Texture Color Outline"), EditorStyles.boldLabel);

                bool texColor = DrawToggle("_OUTLINE_TEXTURE_COLOR", "_OutlineTextureColor", L("テクスチャ連動カラー", "Texture-Based Color"));
                if (texColor)
                {
                    EditorGUI.indentLevel++;
                    DrawProperty("_OutlineTexColorBlend", L("テクスチャカラーブレンド", "Texture Color Blend"));
                    DrawProperty("_OutlineTexColorDarken", L("テクスチャカラー暗化", "Texture Color Darken"));
                    DrawHelpToggle("OutlineTextureColor",
                        L("🎨 テクスチャカラーアウトライン:\n" +
                        "アウトラインの色をベーステクスチャから取得します。\n" +
                        "アークナイツ：エンドフィールドスタイルのアウトラインを再現。\n\n" +
                        "• ブレンド: テクスチャ色の混合度\n" +
                        "• 暗化: テクスチャ色をどの程度暗くするか\n\n" +
                        "💡 使い方:\n" +
                        "肌色→暗い肌色、髪色→暗い髪色のように\n" +
                        "自然なアウトラインカラーを自動生成します。\n" +
                        "マルチカラーアウトラインと組み合わせ可能です。",
                        "🎨 Texture Color Outline:\n" +
                        "Gets outline color from the base texture.\n" +
                        "Reproduces Arknights: Endfield style outlines.\n\n" +
                        "• Blend: Texture color mix amount\n" +
                        "• Darken: How much to darken texture color\n\n" +
                        "💡 Usage:\n" +
                        "Auto-generates natural outline colors like\n" +
                        "skin tone to dark skin, hair color to dark hair.\n" +
                        "Can be combined with multi-color outline."),
                        MessageType.Info);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("スムース法線（Smooth Normal）", "Smooth Normals"), EditorStyles.boldLabel);

                bool useSmoothNormal = DrawToggle("_SMOOTH_NORMAL", "_SmoothNormal", L("スムース法線を使用", "Use Smooth Normals"));
                if (useSmoothNormal)
                {
                    EditorGUI.indentLevel++;
                    DrawProperty("_SmoothNormalMode", L("法線ソース", "Normal Source"));

                    // Show texture field only when Mode 2 (Baked Normal Texture) is selected
                    MaterialProperty smoothNormalMode = FindProperty("_SmoothNormalMode", properties, false);
                    if (smoothNormalMode != null && smoothNormalMode.floatValue > 1.5f)
                    {
                        DrawProperty("_SmoothNormalTex", L("スムース法線テクスチャ", "Smooth Normal Texture"));
                    }

                    EditorGUILayout.Space(3);
                    DrawProperty("_SmoothNormalShadingBlend", L("シェーディングブレンド", "Shading Blend"));

                    DrawHelpToggle("SmoothNormal",
                        L("スムース法線（Smooth Object Normal）:\n" +
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
                        "Smooth Normal (Smooth Object Normal):\n" +
                        "Fixes outline splitting on models with hard edges.\n" +
                        "Also smooths shadow transitions.\n\n" +
                        "• Vertex Color ObjectSpace: Simplest. Requires pre-bake with bake tool.\n" +
                        "• Vertex Color TangentSpace: Compatible with normal maps. lilToon compatible.\n" +
                        "• Texture: Highest quality. Uses baked normal map texture.\n\n" +
                        "Shading Blend:\n" +
                        "0 = Apply to outline only (no effect on shadows)\n" +
                        "0.3-0.5 = Recommended. Smooths hard edge shadows while keeping detail\n" +
                        "1.0 = Full smooth normal shading (smoothest shadows)\n\n" +
                        "💡 Usage:\n" +
                        "1. Bake via Tools > Natane > Mesh > Smooth Normal Bake\n" +
                        "2. Apply baked mesh to model\n" +
                        "3. Enable 'Use Smooth Normal' here\n" +
                        "4. Adjust shadow smoothness with Shading Blend"),
                        MessageType.Info);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("コーナーギャップ修正", "Corner Gap Fix"), EditorStyles.boldLabel);

                // _SMOOTH_NORMAL OFF時のみ警告+フォールバック表示
                MaterialProperty smoothNormalProp = FindProperty("_SmoothNormal", properties, false);
                bool isSmoothNormalOff = smoothNormalProp == null || smoothNormalProp.floatValue < 0.5f;
                if (isSmoothNormalOff)
                {
                    EditorGUILayout.HelpBox(
                        L("ハードエッジのモデルでアウトラインが割れる場合、" +
                        "「スムース法線」の使用が最善の解決策です。\n" +
                        "以下のパラメータはスムース法線が使えない場合の簡易対策です。",
                        "If outlines break on hard-edge models, " +
                        "using 'Smooth Normals' is the best solution.\n" +
                        "The following parameters are workarounds when smooth normals cannot be used."),
                        MessageType.Info);
                    DrawProperty("_OutlineCornerSmooth", L("コーナースムージング", "Corner Smoothing"));
                    DrawHelpToggle("OutlineCornerSmooth",
                        L("コーナースムージング:\n" +
                        "頂点法線を頂点位置方向（オブジェクト中心→頂点）にブレンドします。\n" +
                        "ハードエッジのコーナーで法線がバラバラになるのを緩和します。\n\n" +
                        "• 0: 無効（デフォルト）\n" +
                        "• 0.3〜0.5: 推奨。コーナーギャップを軽減しつつ形状を維持\n" +
                        "• 1.0: 完全に位置方向の法線を使用（球体状に膨張）\n\n" +
                        "⚠️ スムース法線ベイクの方がより正確な結果が得られます。",
                        "Corner Smoothing:\n" +
                        "Blends vertex normals towards position direction (center to vertex).\n" +
                        "Reduces normals scattering at hard-edge corners.\n\n" +
                        "• 0: Disabled (Default)\n" +
                        "• 0.3-0.5: Recommended. Reduces corner gaps while maintaining shape\n" +
                        "• 1.0: Fully use position-direction normals (spherical expansion)\n\n" +
                        "⚠️ Smooth normal baking provides more accurate results."),
                        MessageType.Info);
                }

                // エッジ幅補正は常時表示
                DrawProperty("_OutlineEdgeCompensation", L("エッジ幅補正", "Edge Width Compensation"));
                DrawHelpToggle("OutlineEdgeCompensation",
                    L("エッジ幅補正:\n" +
                    "シャープエッジ部分でアウトライン幅を自動縮小します。\n" +
                    "元の法線と使用中の法線の不一致度を検出し、\n" +
                    "差が大きい箇所ほどアウトラインを細くします。\n\n" +
                    "• 0: 無効（デフォルト）\n" +
                    "• 0.5〜1.0: 推奨。コーナーの突出を抑制\n\n" +
                    "💡 スムース法線のON/OFFに関わらず使用可能です。",
                    "Edge Width Compensation:\n" +
                    "Auto-shrinks outline width at sharp edges.\n" +
                    "Detects mismatch between original and current normals,\n" +
                    "making outlines thinner where difference is large.\n\n" +
                    "• 0: Disabled (default)\n" +
                    "• 0.5-1.0: Recommended. Suppresses corner protrusion\n\n" +
                    "💡 Works regardless of Smooth Normal ON/OFF."),
                    MessageType.Info);

                EditorGUILayout.Space(SECTION_SPACING);

                float outlineMode = targetMaterial.GetFloat("_OutlineMode");
                if (outlineMode < FLOAT_COMPARISON_THRESHOLD)
                {
                    DrawHelpToggle("OutlineInvertedHull",
                        L("【反転ハル方式】\n" +
                        "法線方向に頂点を押し出してアウトラインを描画します。\n" +
                        "• 利点: 一般的に安定した結果、距離補正により遠近で一貫した太さ\n" +
                        "• 欠点: ローポリモデルやハードエッジで乱れる場合があります\n" +
                        "• カメラ距離による自動調整機能を搭載",
                        "[Inverted Hull Method]\n" +
                        "Draws outline by pushing vertices along normals.\n" +
                        "• Pros: Generally stable, distance correction gives consistent width\n" +
                        "• Cons: May distort on low-poly models or hard edges\n" +
                        "• Includes auto camera distance adjustment"),
                        MessageType.Info);
                }
                else
                {
                    DrawHelpToggle("OutlineBackface",
                        L("【背面法】\n" +
                        "メッシュを拡大して背面を描画します。\n" +
                        "• 利点: スムーズなアウトライン、ハイポリモデルに適しています\n" +
                        "• 欠点: 内部構造が見える場合があります\n" +
                        "• 距離補正により遠くでも視認性を維持",
                        "[Backface Method]\n" +
                        "Draws outline by scaling mesh and rendering backfaces.\n" +
                        "• Pros: Smooth outlines, ideal for high-poly models\n" +
                        "• Cons: Internal structure may be visible\n" +
                        "• Distance correction maintains visibility at far range"),
                        MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Outline"));
    }

    private void DrawEmissionSection()
    {
        SetFoldout("Emission", DrawBoxedSection(L("エミッション（発光）", "Emission (Glow)"), GetFoldout("Emission"), SectionCategory.Effects, "_EMISSION"));
        if (GetFoldout("Emission"))
        {

            bool enableEmission = DrawToggle("_EMISSION", "_Emission", L("エミッションを有効化", "Enable Emission"));

            if (enableEmission)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_EmissionColor", L("エミッションの色", "Emission Color"), true);
                DrawProperty("_EmissionMap", L("エミッションマップ", "Emission Map"));

                EditorGUILayout.Space();
                DrawEmissionUVAnimationSettings();

                DrawProperty("_EmissionPulseSpeed", L("パルス速度", "Pulse Speed"));
                DrawProperty("_EmissionPulseAmplitude", L("パルスの振幅", "Pulse Amplitude"));

                EditorGUILayout.Space();
                DrawProperty("_EmissionMask", L("エミッションマスク", "Emission Mask"));
                DrawHelpToggle("EmissionMask", L("白 = エミッションあり、黒 = エミッションなし", "White = Emission on, Black = Emission off"), MessageType.Info);
                DrawUVAnimationSettings("_EmissionMaskScrollSpeed", "_EmissionMaskRotateSpeed", L("エミッションマスク", "Emission Mask"));

                EditorGUILayout.Space();
                DrawProperty("_EmissionGlow", L("エミッショングロー（ブルーム）", "Emission Glow (Bloom)"));
                DrawHelpToggle("EmissionGlow",
                    L("✨ エミッショングロー（ブルーム効果）:\n" +
                    "発光部分を滲ませて明るく広げ、柔らかく\n" +
                    "輝いているような効果を追加します。\n\n" +
                    "• 0 = グローなし（シャープな発光）\n" +
                    "• 0.3-0.5 = 適度なグロー（推奨）\n" +
                    "• 0.7-1.0 = 強いグロー（強烈な輝き）\n\n" +
                    "💡 ヒント:\n" +
                    "HDRカラーと組み合わせると、より鮮やかな\n" +
                    "発光効果が得られます。",
                    "✨ Emission Glow (Bloom Effect):\n" +
                    "Bleeds and brightens emission areas for a soft\n" +
                    "glowing effect.\n\n" +
                    "• 0 = No glow (sharp emission)\n" +
                    "• 0.3-0.5 = Moderate glow (Recommended)\n" +
                    "• 0.7-1.0 = Strong glow (intense shine)\n\n" +
                    "💡 Tip:\n" +
                    "Combine with HDR colors for even more\n" +
                    "vivid emission effects."),
                    MessageType.None);

                DrawBlendControls(materialEditor, targetMaterial, "_EmissionBlend", "_EmissionBlendMode", "_EmissionBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_EmissionDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Emission"));
    }

    private void DrawVirtualExpressionSection()
    {
        SetFoldout("VirtualExpression", DrawBoxedSection(L("バーチャル表現", "Virtual Expression"), GetFoldout("VirtualExpression"), SectionCategory.Effects));
        if (GetFoldout("VirtualExpression"))
        {

            // Dissolve Effect
            bool enableDissolve = DrawToggle("_DISSOLVE", "_Dissolve", L("ディゾルブを有効化", "Enable Dissolve"));
            if (enableDissolve)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_DissolveAmount", L("ディゾルブ量", "Dissolve Amount"));
                DrawHelpToggle("DissolveAmount", L("0 = 完全に表示、1 = 完全に消滅", "0 = Fully visible, 1 = Fully dissolved"), MessageType.Info);

                DrawProperty("_DissolveTex", L("ディゾルブテクスチャ（ノイズ）", "Dissolve Texture (Noise)"));
                DrawUVAnimationSettings("_DissolveTexScrollSpeed", "_DissolveTexRotateSpeed", L("ディゾルブ", "Dissolve"));
                DrawProperty("_DissolveEdgeWidth", L("エッジの幅", "Edge Width"));
                DrawColorProperty("_DissolveEdgeColor", L("エッジの色", "Edge Color"), true);
                DrawProperty("_DissolveEdgeIntensity", L("エッジの強さ", "Edge Intensity"));

                EditorGUILayout.Space();
                DrawProperty("_DissolveMask", L("ディゾルブマスク", "Dissolve Mask"));
                DrawHelpToggle("DissolveMask", L("白 = ディゾルブあり、黒 = ディゾルブなし", "White = Dissolve on, Black = Dissolve off"), MessageType.Info);

                DrawHelpToggle("DissolveInfo", L("ディゾルブはVRChatアバターの出現アニメーションに最適な消滅・分解エフェクトを作成します。ディゾルブ量パラメータをアニメーションさせることで、オブジェクトを出現または消滅させることができます。", "Dissolve creates vanishing/disintegrating effects ideal for VRChat avatar appearance animations. Animate the dissolve amount parameter to make objects appear or disappear."), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_DissolveBlend", "_DissolveBlendMode", "_DissolveBlur");

                EditorGUILayout.Space();
                DrawProperty("_DissolveCoordMode", L("座標モード", "Coordinate Mode"));
                float dissolveCoordMode = FindProperty("_DissolveCoordMode", properties).floatValue;
                if (dissolveCoordMode > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    DrawProperty("_DissolveWorldAxis", L("軸方向", "Axis Direction"));
                    DrawProperty("_DissolveWorldMin", L("最小値", "Minimum Value"));
                    DrawProperty("_DissolveWorldMax", L("最大値", "Maximum Value"));
                    DrawProperty("_DissolveNoiseBlend", L("ノイズテクスチャブレンド", "Noise Texture Blend"));
                    DrawHelpToggle("DissolveCoordMode",
                        L("🌍 ワールド座標ディゾルブ:\n" +
                        "UV座標ではなく、ワールド/ローカル座標で\n" +
                        "ディゾルブを制御します。\n\n" +
                        "• 軸方向: ディゾルブの進行軸（X/Y/Z）\n" +
                        "• 最小/最大値: ディゾルブ範囲の座標\n" +
                        "• ノイズブレンド: テクスチャノイズの混合量\n" +
                        "  0=座標のみ、1=テクスチャと完全混合",
                        "🌍 World Coordinate Dissolve:\n" +
                        "Controls dissolve using world/local coordinates\n" +
                        "instead of UV coordinates.\n\n" +
                        "• Axis: Dissolve progression axis (X/Y/Z)\n" +
                        "• Min/Max: Dissolve range coordinates\n" +
                        "• Noise Blend: Texture noise blend amount\n" +
                        "  0=coordinates only, 1=full texture blend"),
                        MessageType.Info);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Hue Shift
            bool enableHueShift = DrawToggle("_HUE_SHIFT", "_HueShiftEnable", L("色相シフトを有効化", "Enable Hue Shift"));
            if (enableHueShift)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_HueShift", L("色相シフト", "Hue Shift"));
                DrawHelpToggle("HueShift", L("マテリアル全体の色相を変更します。0 = 変更なし、0.5 = 補色、1 = 完全な回転。VRChatでの色変更エフェクトに最適です。", "Changes the hue of the entire material. 0 = No change, 0.5 = Complementary, 1 = Full rotation. Ideal for color-changing effects in VRChat."), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_HueShiftBlend", null, "_HueShiftBlur");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Alpha Mask
            bool useAlphaMask = DrawToggle("_ALPHA_MASK", "_UseAlphaMask", L("アルファマスクを使用", "Use Alpha Mask"));
            if (useAlphaMask)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_AlphaMask", L("アルファマスク", "Alpha Mask"));
                DrawHelpToggle("AlphaMask",
                    L("🎭 アルファマスク（部分的な半透明）:\n\n" +
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
                    "🎭 Alpha Mask (Partial Transparency):\n\n" +
                    "Uses the R channel of the texture to partially\n" +
                    "control material transparency.\n\n" +
                    "• White (1.0) = Fully opaque\n" +
                    "• Gray (0.5) = Semi-transparent\n" +
                    "• Black (0.0) = Fully transparent\n\n" +
                    "💡 Uses:\n" +
                    "  - Make only parts of glass transparent\n" +
                    "  - Make parts of clothing see-through\n" +
                    "  - Gradient edge fade-out\n" +
                    "  - Complex shape transparency control\n\n" +
                    "⚠️ Note:\n" +
                    "Set rendering mode to 'Transparent'\n" +
                    "to use semi-transparency effects."),
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("VirtualExpression"));
    }

    private void DrawNormalMapSection()
    {
        SetFoldout("NormalMap", DrawBoxedSection(L("ノーマルマップ", "Normal Map"), GetFoldout("NormalMap"), SectionCategory.Advanced, "_NORMALMAP"));
        if (GetFoldout("NormalMap"))
        {
            bool useNormalMap = DrawToggle("_NORMALMAP", "_UseNormalMap", L("ノーマルマップを使用", "Use Normal Map"));

            if (useNormalMap)
            {
                DrawProperty("_BumpMap", L("ノーマルマップ", "Normal Map"));
                DrawProperty("_BumpScale", L("ノーマルのスケール", "Normal Scale"));
                DrawUVAnimationSettings("_BumpMapScrollSpeed", "_BumpMapRotateSpeed", L("ノーマルマップ", "Normal Map"));
            }
        }
        EndBoxedSection(GetFoldout("NormalMap"));
    }

    private void DrawReflectionSection()
    {
        SetFoldout("Reflection", DrawBoxedSection(L("反射 / キューブマップ", "Reflection / Cubemap"), GetFoldout("Reflection"), SectionCategory.Environment, "_REFLECTION"));
        if (GetFoldout("Reflection"))
        {
            bool enableReflection = DrawToggle("_REFLECTION", "_Reflection", L("リフレクションを有効化", "Enable Reflection"));

            if (enableReflection)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_ReflectionCube", L("リフレクションキューブマップ", "Reflection Cubemap"));
                DrawColorProperty("_ReflectionColor", L("リフレクションの色", "Reflection Color"));
                DrawProperty("_ReflectionIntensity", L("リフレクションの強さ", "Reflection Intensity"));
                DrawProperty("_Smoothness", L("滑らかさ（光沢）", "Smoothness (Gloss)"));
                DrawProperty("_Metallic", L("メタリック", "Metallic"));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(L("フレネル設定（柔らかい反射）", "Fresnel Settings (Soft Reflection)"), EditorStyles.boldLabel);
                DrawProperty("_FresnelPower", L("フレネルパワー", "Fresnel Power"));
                DrawProperty("_FresnelSoftness", L("フレネルソフトネス", "Fresnel Softness"));
                DrawHelpToggle("FresnelSoftness",
                    L("✨ フレネルソフトネス:\n" +
                    "反射の境界を柔らかくして、より自然で芸術的な反射を実現します。\n" +
                    "• 0 = シャープな境界（現実的）\n" +
                    "• 0.5-0.7 = 柔らかい境界（推奨）\n" +
                    "• 1.0 = 非常に柔らかい境界（芸術的）",
                    "✨ Fresnel Softness:\n" +
                    "Softens reflection boundaries for more natural, artistic reflections.\n" +
                    "• 0 = Sharp boundary (realistic)\n" +
                    "• 0.5-0.7 = Soft boundary (Recommended)\n" +
                    "• 1.0 = Very soft boundary (artistic)"),
                    MessageType.None);

                DrawProperty("_ReflectionBlendMode", L("ブレンドモード", "Blend Mode"));
                DrawHelpToggle("ReflectionBlendMode",
                    L("• 0 = 加算（明るく追加）\n" +
                    "• 1 = オーバーレイ（自然な統合）",
                    "• 0 = Additive (brighter addition)\n" +
                    "• 1 = Overlay (natural integration)"),
                    MessageType.Info);

                EditorGUILayout.Space();
                DrawProperty("_ReflectionMask", L("リフレクションマスク", "Reflection Mask"));
                DrawHelpToggle("ReflectionMask", L("白 = リフレクションあり、黒 = リフレクションなし", "White = Reflection on, Black = Reflection off"), MessageType.Info);

                DrawHelpToggle("ReflectionInfo", L("キューブマップを使用して環境反射をシミュレートします。金属やガラスなどの反射素材に最適です。", "Simulates environment reflections using cubemaps. Ideal for reflective materials like metal and glass."), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_ReflectionBlend");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_ReflectionDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Reflection"));
    }

    private void DrawIridescenceSection()
    {
        SetFoldout("Iridescence", DrawBoxedSection(L("イリデッセンス（玉虫色）", "Iridescence"), GetFoldout("Iridescence"), SectionCategory.Environment, "_IRIDESCENCE"));
        if (GetFoldout("Iridescence"))
        {
            bool enableIridescence = DrawToggle("_IRIDESCENCE", "_Iridescence", L("イリデッセンスを有効化", "Enable Iridescence"));

            if (enableIridescence)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("イリデッセンス設定", "Iridescence Settings"), EditorStyles.boldLabel);

                DrawColorProperty("_IridescenceColor", L("イリデッセンスの色", "Iridescence Color"));
                DrawProperty("_IridescenceIntensity", L("強度", "Intensity"));
                DrawProperty("_IridescenceHueShift", L("色相シフト", "Hue Shift"));
                DrawProperty("_IridescenceSize", L("サイズ（周波数）", "Size (Frequency)"));

                EditorGUILayout.Space(SECTION_SPACING);

                DrawProperty("_IridescenceMask", L("イリデッセンスマスク", "Iridescence Mask"));
                DrawHelpToggle("IridescenceMask",
                    L("イリデッセンスマスクのR(赤)チャンネルを使用してイリデッセンスの表示領域を制御します。\n" +
                    "• 白 (1.0): イリデッセンスを完全に表示\n" +
                    "• 黒 (0.0): イリデッセンスを非表示\n" +
                    "• グレー: 部分的に表示",
                    "Uses the R (red) channel of the iridescence mask to control display area.\n" +
                    "• White (1.0): Fully show iridescence\n" +
                    "• Black (0.0): Hide iridescence\n" +
                    "• Gray: Partially show"),
                    MessageType.Info);

                EditorGUILayout.Space(SECTION_SPACING);
                DrawHelpToggle("IridescenceInfo",
                    L("📍 イリデッセンス（Iridescence）:\n" +
                    "見る角度によって色が変化する玉虫色効果を追加します。\n\n" +
                    "• 色: ベースとなる色調\n" +
                    "• 強度: 効果の明るさ\n" +
                    "• 色相シフト: 色の変化開始位置\n" +
                    "• サイズ: 色の変化の周波数（大きいほど早く変化）\n\n" +
                    "💡 使い方:\n" +
                    "シャボン玉、オイルスリック、昆虫の羽、魚の鱗など、\n" +
                    "光の干渉による虹色効果を表現したい場合に使用します。",
                    "📍 Iridescence:\n" +
                    "Adds color-shifting effect that changes with viewing angle.\n\n" +
                    "• Color: Base color tone\n" +
                    "• Intensity: Effect brightness\n" +
                    "• Hue Shift: Color change start position\n" +
                    "• Size: Color change frequency (larger=faster change)\n\n" +
                    "💡 Usage:\n" +
                    "Use for soap bubbles, oil slick, insect wings, fish scales,\n" +
                    "or any surface requiring rainbow interference effects."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_IridescenceBlend", "_IridescenceBlendMode", "_IridescenceBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_IridescenceDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Iridescence"));
    }

    private void DrawEnvironmentalRimSection()
    {
        SetFoldout("EnvironmentalRim", DrawBoxedSection(L("環境リム", "Environmental Rim"), GetFoldout("EnvironmentalRim"), SectionCategory.Environment, "_ENV_RIM"));
        if (GetFoldout("EnvironmentalRim"))
        {
            bool enableEnvRim = DrawToggle("_ENV_RIM", "_EnvRim", L("環境リムを有効化", "Enable Environmental Rim"));

            if (enableEnvRim)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_EnvRimCube", L("環境キューブマップ", "Environment Cubemap"));
                DrawColorProperty("_EnvRimColor", L("環境リムの色", "Environmental Rim Color"));
                DrawProperty("_EnvRimPower", L("環境リムのパワー", "Environmental Rim Power"));
                DrawProperty("_EnvRimIntensity", L("環境リムの強さ", "Environmental Rim Intensity"));

                EditorGUILayout.Space();
                DrawProperty("_EnvRimMask", L("環境リムマスク", "Environmental Rim Mask"));
                DrawHelpToggle("EnvRimMask", L("白 = 環境リムあり、黒 = 環境リムなし", "White = Environmental rim on, Black = Environmental rim off"), MessageType.Info);

                DrawHelpToggle("EnvRimInfo", L("キューブマップを使用して環境に基づいたリムライト効果を作成します。", "Creates environment-based rim light effects using cubemaps."), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_EnvRimBlend", "_EnvRimBlendMode", "_EnvRimBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_EnvRimDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("EnvironmentalRim"));
    }

    private void DrawParallaxSection()
    {
        SetFoldout("Parallax", DrawBoxedSection(L("視差マッピング（パララックス）", "Parallax Mapping"), GetFoldout("Parallax"), SectionCategory.Advanced, "_PARALLAX"));
        if (GetFoldout("Parallax"))
        {
            bool enableParallax = DrawToggle("_PARALLAX", "_Parallax", L("視差マッピングを有効化", "Enable Parallax Mapping"));

            if (enableParallax)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_ParallaxMap", L("高さマップ", "Height Map"));
                DrawProperty("_ParallaxScale", L("視差のスケール", "Parallax Scale"));
                DrawProperty("_ParallaxMinSamples", L("最小サンプル数", "Min Samples"));
                DrawProperty("_ParallaxMaxSamples", L("最大サンプル数", "Max Samples"));

                DrawHelpToggle("ParallaxInfo", L("視差マッピングは高さマップを使用してサーフェスに深度の錯覚を作成します。石や壁などの素材に最適です。", "Parallax mapping uses height maps to create depth illusions on surfaces. Ideal for stone and wall materials."), MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Parallax"));
    }

    private void DrawVertexAnimationSection()
    {
        SetFoldout("VertexAnimation", DrawBoxedSection(L("頂点アニメーション（風/呼吸/脈動）", "Vertex Animation (Wind/Breath/Pulse)"), GetFoldout("VertexAnimation"), SectionCategory.Advanced, "_VERTEX_ANIMATION"));
        if (GetFoldout("VertexAnimation"))
        {
            bool enableVertexAnim = DrawToggle("_VERTEX_ANIMATION", "_VertexAnimation", L("頂点アニメーションを有効化", "Enable Vertex Animation"));

            if (enableVertexAnim)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                DrawProperty("_VertexAnimType", L("アニメーション種類", "Animation Type"));
                DrawProperty("_VertexAnimSpeed", L("速度", "Speed"));
                DrawProperty("_VertexAnimStrength", L("強度", "Intensity"));
                DrawProperty("_VertexAnimFrequency", L("周波数", "Frequency"));

                EditorGUILayout.Space(SECTION_SPACING);
                bool useVertexAnimMask = DrawToggle("_VERTEX_ANIM_MASK", "_UseVertexAnimMask", L("マスク使用", "Use Mask"));
                if (useVertexAnimMask)
                {
                    DrawProperty("_VertexAnimMask", L("頂点アニメーションマスク", "Vertex Animation Mask"));
                }
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("VertexAnimation"));
    }

    private void DrawVATSection()
    {
        SetFoldout("VAT", DrawBoxedSection(L("VAT（頂点アニメーション）", "VAT (Vertex Animation Texture)"), GetFoldout("VAT"), SectionCategory.Advanced, "_VAT"));
        if (GetFoldout("VAT"))
        {
            bool enableVAT = DrawToggle("_VAT", "_VAT", L("VATアニメーションを有効化", "Enable VAT Animation"));

            if (enableVAT)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("VAT設定（Houdini互換）", "VAT Settings (Houdini Compatible)"), EditorStyles.boldLabel);

                DrawProperty("_VATPositionMap", L("VAT位置マップ", "VAT Position Map"));
                DrawProperty("_VATNumOfFrames", L("フレーム数", "Number of Frames"));
                DrawProperty("_VATSpeed", L("アニメーション速度", "Animation Speed"));
                DrawProperty("_VATIntensity", L("アニメーション強度", "Animation Intensity"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("VAT位置パラメータ", "VAT Position Parameters"), EditorStyles.boldLabel);
                DrawProperty("_VATPositionMin", L("位置最小値", "Position Min"));
                DrawProperty("_VATPositionMax", L("位置最大値", "Position Max"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("VAT法線マップ（オプション）", "VAT Normal Map (Optional)"), EditorStyles.boldLabel);

                bool useVATNormal = DrawToggle("_VAT_NORMAL", "_VATNormal", L("VAT法線マップを使用", "Use VAT Normal Map"));
                if (useVATNormal)
                {
                    DrawProperty("_VATNormalMap", L("VAT法線マップ", "VAT Normal Map"));
                    DrawProperty("_VATNormalMin", L("法線最小値", "Normal Min"));
                    DrawProperty("_VATNormalMax", L("法線最大値", "Normal Max"));
                }

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("VAT詳細設定", "VAT Advanced Settings"), EditorStyles.boldLabel);
                DrawProperty("_VATPackingMode", L("パッキングモード", "Packing Mode"));
                DrawHelpToggle("VATPackingMode",
                    L("• Absolute (0): 絶対位置モード - 頂点位置を直接置き換えます\n" +
                    "• Offset (1): オフセットモード - 元の位置にオフセットを追加します（推奨）",
                    "• Absolute (0): Absolute position mode - directly replaces vertex positions\n" +
                    "• Offset (1): Offset mode - adds offset to original position (Recommended)"),
                    MessageType.Info);

                EditorGUILayout.Space(SECTION_SPACING);
                DrawHelpToggle("VATInfo",
                    L("📍 VAT（Vertex Animation Texture）:\n" +
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
                    "📍 VAT (Vertex Animation Texture):\n" +
                    "Plays vertex animations created in Houdini as textures.\n\n" +
                    "[Setup]\n" +
                    "1. Export using VAT (ROP Output Driver) in Houdini\n" +
                    "2. Set Position Map (.exr) as position map\n" +
                    "3. Optionally set Normal Map (.exr) as normal map\n" +
                    "4. Match frame count with Houdini settings\n\n" +
                    "[Parameters]\n" +
                    "• Position Min/Max: Export range from Houdini (usually -1 to 1)\n" +
                    "• Speed: Animation playback speed multiplier\n" +
                    "• Intensity: Animation effect strength (0-2)\n\n" +
                    "💡 Usage:\n" +
                    "Bake and play back fluid, cloth, and complex deformation\n" +
                    "animations that are too heavy for real-time physics.\n\n" +
                    "⚠ Note:\n" +
                    "• VAT textures use more memory than regular textures\n" +
                    "• Choose appropriate resolution and frame count for VRChat"),
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("VAT"));
    }

    private void DrawTessellationSection()
    {
        SetFoldout("Tessellation", DrawBoxedSection(L("テッセレーション（曲面スムージング）", "Tessellation (Surface Smoothing)"), GetFoldout("Tessellation"), SectionCategory.Advanced, "_TESSELLATION"));
        if (GetFoldout("Tessellation"))
        {
            bool enableTess = DrawToggle("_TESSELLATION", "_Tessellation", L("テッセレーションを有効化", "Enable Tessellation"));
            if (enableTess)
            {
                DrawProperty("_TessFactor", L("テッセレーション係数", "Tessellation Factor"));
                DrawHelpToggle("TessFactor",
                    L("🔷 テッセレーション係数:\n" +
                    "メッシュの分割数を制御します。\n\n" +
                    "• 1 = 分割なし\n" +
                    "• 2-4 = 軽いスムージング（推奨）\n" +
                    "• 8-16 = 高品質（高負荷）\n\n" +
                    "⚠ VR では両目分のコストがかかります。",
                    "🔷 Tessellation Factor:\n" +
                    "Controls mesh subdivision count.\n\n" +
                    "• 1 = No subdivision\n" +
                    "• 2-4 = Light smoothing (Recommended)\n" +
                    "• 8-16 = High quality (high cost)\n\n" +
                    "⚠ VR incurs cost for both eyes."),
                    MessageType.Info);
                DrawProperty("_TessPhongStrength", L("Phong スムージング強度", "Phong Smoothing Strength"));
                DrawHelpToggle("TessPhong",
                    L("🔷 Phong スムージング:\n" +
                    "頂点法線を使って三角形を曲面に膨らませます。\n\n" +
                    "• 0 = フラット（分割のみ）\n" +
                    "• 0.3-0.5 = 自然な丸み（推奨）\n" +
                    "• 1.0 = 最大スムージング",
                    "🔷 Phong Smoothing:\n" +
                    "Inflates triangles into curved surfaces using vertex normals.\n\n" +
                    "• 0 = Flat (subdivision only)\n" +
                    "• 0.3-0.5 = Natural roundness (Recommended)\n" +
                    "• 1.0 = Maximum smoothing"),
                    MessageType.Info);
                DrawProperty("_TessNormalSmooth", L("法線スムージング強度", "Normal Smoothing Strength"));
                DrawHelpToggle("TessNormalSmooth",
                    L("💡 法線スムージング:\n" +
                    "テッセレーション後の法線の滑らかさを制御します。\n" +
                    "光のあたり方・影の境界の柔らかさに影響します。\n\n" +
                    "• 0 = 元のメッシュ法線をそのまま使用\n" +
                    "• 0.5 = 適度にスムーズ（推奨）\n" +
                    "• 1.0 = 最大スムーズ（光が非常に柔らかくあたる）",
                    "💡 Normal Smoothing:\n" +
                    "Controls normal smoothness after tessellation.\n" +
                    "Affects lighting and shadow boundary softness.\n\n" +
                    "• 0 = Use original mesh normals as-is\n" +
                    "• 0.5 = Moderately smooth (Recommended)\n" +
                    "• 1.0 = Maximum smooth (very soft lighting)"),
                    MessageType.Info);
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("距離LOD", "Distance LOD"), EditorStyles.boldLabel);
                DrawProperty("_TessDistanceMin", L("最小距離（最大テッセレーション）", "Min Distance (Max Tessellation)"));
                DrawProperty("_TessDistanceMax", L("最大距離（テッセレーション無効）", "Max Distance (Tessellation Off)"));
                DrawHelpToggle("TessDistance",
                    L("📏 距離LOD:\n" +
                    "カメラからの距離でテッセレーション係数を自動調整します。\n\n" +
                    "• 最小距離以内 = 設定した係数で分割\n" +
                    "• 最大距離以遠 = テッセレーションOFF\n" +
                    "• その間 = 滑らかに遷移\n\n" +
                    "💡 VRChat ではパフォーマンスのため最大距離を10-20mに設定推奨。",
                    "📏 Distance LOD:\n" +
                    "Auto-adjusts tessellation factor based on camera distance.\n\n" +
                    "• Within min distance = Subdivide at set factor\n" +
                    "• Beyond max distance = Tessellation OFF\n" +
                    "• In between = Smooth transition\n\n" +
                    "💡 For VRChat, set max distance to 10-20m for performance."),
                    MessageType.Info);

                EditorGUILayout.Space(SECTION_SPACING);
                bool enableDisp = DrawToggle("_TESS_DISPLACEMENT", "_TessDisplacement", L("ディスプレイスメントマップを有効化", "Enable Displacement Map"));
                if (enableDisp)
                {
                    DrawProperty("_TessDispMap", L("ディスプレイスメントマップ", "Displacement Map"));
                    DrawProperty("_TessDispStrength", L("ディスプレイスメント強度", "Displacement Strength"));
                    DrawProperty("_TessDispOffset", L("ディスプレイスメントオフセット", "Displacement Offset"));
                    DrawHelpToggle("TessDisp",
                        L("🗻 ディスプレイスメントマップ:\n" +
                        "ハイトマップを使って実際にメッシュの形状を変化させます。\n" +
                        "影や光の反応が変わり、細かい凹凸表現が可能です。\n\n" +
                        "• グレースケールテクスチャを使用（白=高い, 黒=低い）\n" +
                        "• 強度: + = 外側に押し出す, - = 内側に凹む\n" +
                        "• オフセット: 基準面を調整\n\n" +
                        "⚠ テッセレーション係数が低いと効果が粗くなります。",
                        "🗻 Displacement Map:\n" +
                        "Deforms actual mesh shape using a height map.\n" +
                        "Changes shadow and light response for fine detail.\n\n" +
                        "• Uses grayscale texture (white=high, black=low)\n" +
                        "• Intensity: + = push outward, - = push inward\n" +
                        "• Offset: Adjusts reference plane\n\n" +
                        "⚠ Effect looks coarse at low tessellation factors."),
                        MessageType.Info);
                }
            }
        }
        EndBoxedSection(GetFoldout("Tessellation"));
    }

    private void DrawRefractionSection()
    {
        SetFoldout("Refraction", DrawBoxedSection(L("屈折（リフラクション）", "Refraction"), GetFoldout("Refraction"), SectionCategory.Environment, "_REFRACTION"));
        if (GetFoldout("Refraction"))
        {
            bool enableRefraction = DrawToggle("_REFRACTION", "_Refraction", L("屈折を有効化", "Enable Refraction"));

            if (enableRefraction)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_RefractionIndex", L("屈折率（IOR）", "Index of Refraction (IOR)"));
                DrawProperty("_RefractionIntensity", L("屈折の強さ", "Refraction Intensity"));
                DrawProperty("_RefractionBlur", L("屈折のぼかし", "Refraction Blur"));

                EditorGUILayout.Space();
                DrawProperty("_RefractionMask", L("屈折マスク", "Refraction Mask"));
                DrawHelpToggle("RefractionMask", L("白 = 屈折あり、黒 = 屈折なし", "White = Refraction on, Black = Refraction off"), MessageType.Info);

                DrawHelpToggle("RefractionInfo", L("屈折はガラスや水などの透明素材で光の曲がりをシミュレートします。透明マテリアルに最適です。", "Refraction simulates light bending in transparent materials like glass and water. Ideal for transparent materials."), MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_RefractionBlend", "_RefractionBlendMode");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_RefractionDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Refraction"));
    }

    private void DrawRenderingSection()
    {
        SetFoldout("Rendering", DrawBoxedSection(L("レンダリング設定", "Rendering Settings"), GetFoldout("Rendering"), SectionCategory.Advanced));
        if (GetFoldout("Rendering"))
        {

            // Rendering Mode Selection
            EditorGUILayout.LabelField(L("レンダリングモード", "Rendering Mode"), EditorStyles.boldLabel);
            RenderingMode currentMode = GetCurrentRenderingMode();

            EditorGUI.BeginChangeCheck();
            RenderingMode newMode = (RenderingMode)(EditorGUILayout.Popup(L("描画タイプ", "Rendering Type"), (int)currentMode, RenderingModeLabels));
            if (EditorGUI.EndChangeCheck())
            {
                SetRenderingMode(newMode);
            }

            DrawHelpToggle("RenderingMode",
                L("🎨 レンダリングモード:\n\n" +
                "• 不透明: 標準的な不透明オブジェクト\n" +
                "  - 肌、服、硬い物体など\n" +
                "  - 最も高速で推奨\n\n" +
                "• カットアウト: アルファ閾値による透過\n" +
                "  - 髪の毛、葉っぱ、フェンスなど\n" +
                "  - アルファ値が0.5以上で表示、未満で非表示\n" +
                "  - 半透明ではなく、完全に透明か不透明かの2択\n\n" +
                "• 半透明: 滑らかな透過\n" +
                "  - ガラス、水、煙、エフェクトなど\n" +
                "  - アルファ値に応じて段階的に透過\n" +
                "  - 最も負荷が高い\n\n" +
                "・ファー: シェルベース毛皮レンダリング\n" +
                "  - 16シェルパスで毛皮を表現\n" +
                "  - GPU負荷が非常に高い（Quest非推奨）\n\n" +
                "• 背景: 背景・環境オブジェクト専用\n" +
                "  - ライトマップ統合（ベイクGI対応）\n" +
                "  - PBRマテリアル（メタリック/スムーズネス）\n" +
                "  - ワールド制作に最適\n\n" +
                "注意:\n" +
                "モードを変更すると、内部的に適切なシェーダーバリアントに\n" +
                "切り替わりますが、すべてのプロパティは保持されます。",
                "🎨 Rendering Mode:\n\n" +
                "• Opaque: Standard opaque objects\n" +
                "  - Skin, clothing, solid objects etc.\n" +
                "  - Fastest, recommended\n\n" +
                "• Cutout: Alpha threshold transparency\n" +
                "  - Hair, leaves, fences etc.\n" +
                "  - Visible at alpha >= 0.5, hidden below\n" +
                "  - Binary: fully transparent or fully opaque\n\n" +
                "• Transparent: Smooth transparency\n" +
                "  - Glass, water, smoke, effects etc.\n" +
                "  - Gradual transparency based on alpha\n" +
                "  - Highest cost\n\n" +
                "• Fur: Shell-based fur rendering\n" +
                "  - 16 shell passes for fur\n" +
                "  - Very high GPU cost (not recommended for Quest)\n\n" +
                "• Background: Background/environment objects\n" +
                "  - Lightmap integration (baked GI support)\n" +
                "  - PBR material (metallic/smoothness)\n" +
                "  - Best for world creation\n\n" +
                "Note:\n" +
                "Changing mode internally switches to the appropriate\n" +
                "shader variant, but all properties are preserved."),
                MessageType.Info);

            // Cutout の場合にAlpha Cutoff スライダーを表示
            if (currentMode == RenderingMode.Cutout)
            {
                EditorGUILayout.Space(SECTION_SPACING);
                MaterialProperty cutoffProp = FindProperty("_Cutoff", properties, false);
                if (cutoffProp != null)
                {
                    DrawProperty("_Cutoff", L("アルファカットオフ", "Alpha Cutoff"));
                    DrawHelpToggle("AlphaCutoff",
                        L("テクスチャの透明度がこの値以下のピクセルを非表示にします。\n" +
                        "• 0.5 = デフォルト（推奨）\n" +
                        "• 値を下げると: より多くのピクセルが表示される\n" +
                        "• 値を上げると: より多くのピクセルが非表示になる",
                        "Hides pixels with alpha below this value.\n" +
                        "• 0.5 = Default (Recommended)\n" +
                        "• Lower value: More pixels become visible\n" +
                        "• Higher value: More pixels become hidden"),
                        MessageType.Info);
                }
            }

            EditorGUILayout.Space(10);

            // Advanced Rendering Settings
            EditorGUILayout.LabelField(L("詳細設定", "Advanced Settings"), EditorStyles.boldLabel);
            DrawProperty("_Cull", L("カリングモード", "Culling Mode"));
            DrawProperty("_ZWrite", L("Z書き込み", "Z Write"));

            // Show blend mode properties for Transparent mode
            if (currentMode == RenderingMode.Transparent)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(L("ブレンドモード", "Blend Mode"), EditorStyles.boldLabel);

                // Blend preset selector
                MaterialProperty srcBlendProp = FindProperty("_SrcBlend", properties, false);
                MaterialProperty dstBlendProp = FindProperty("_DstBlend", properties, false);
                if (srcBlendProp != null && dstBlendProp != null)
                {
                    int currentPreset = GetBlendPresetIndex((int)srcBlendProp.floatValue, (int)dstBlendProp.floatValue);
                    string[] presetNames = new string[] {
                        L("Alpha Blend（標準半透明）", "Alpha Blend (Standard)"),
                        L("Additive（加算）", "Additive"),
                        L("Premultiplied Alpha（事前乗算）", "Premultiplied Alpha"),
                        L("Multiplicative（乗算）", "Multiplicative"),
                        L("カスタム", "Custom")
                    };

                    EditorGUI.BeginChangeCheck();
                    int newPreset = EditorGUILayout.Popup(L("ブレンドプリセット", "Blend Preset"), currentPreset, presetNames);
                    if (EditorGUI.EndChangeCheck() && newPreset != 4) // 4 = Custom, don't change
                    {
                        ApplyBlendPreset(newPreset, srcBlendProp, dstBlendProp);
                    }

                    DrawProperty("_SrcBlend", L("ソースブレンド", "Source Blend"));
                    DrawProperty("_DstBlend", L("宛先ブレンド", "Destination Blend"));
                }

                DrawHelpToggle("BlendMode",
                    L("🎨 ブレンドモード:\n" +
                    "半透明の合成方法を制御します。\n\n" +
                    "プリセット:\n" +
                    "• Alpha Blend: 標準的な半透明（SrcAlpha / OneMinusSrcAlpha）\n" +
                    "• Additive: 加算合成・光るエフェクト向け（One / One）\n" +
                    "• Premultiplied Alpha: 事前乗算アルファ（One / OneMinusSrcAlpha）\n" +
                    "• Multiplicative: 乗算合成・影向け（DstColor / Zero）\n" +
                    "• カスタム: SrcBlend/DstBlendを直接指定\n\n" +
                    "💡 通常は Alpha Blend を推奨します。",
                    "🎨 Blend Mode:\n" +
                    "Controls transparency compositing method.\n\n" +
                    "Presets:\n" +
                    "• Alpha Blend: Standard transparency (SrcAlpha / OneMinusSrcAlpha)\n" +
                    "• Additive: Additive blending for glow effects (One / One)\n" +
                    "• Premultiplied Alpha: Pre-multiplied alpha (One / OneMinusSrcAlpha)\n" +
                    "• Multiplicative: Multiply blending for shadows (DstColor / Zero)\n" +
                    "• Custom: Directly specify SrcBlend/DstBlend\n\n" +
                    "💡 Alpha Blend is recommended for most cases."),
                    MessageType.Info);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("Stencil", "Stencil"), EditorStyles.boldLabel);
            DrawProperty("_StencilRef", L("参照値 (Reference)", "Reference Value"));
            DrawProperty("_StencilComp", L("比較関数 (Comparison)", "Comparison Function"));
            DrawProperty("_StencilOp", L("Pass操作（テスト成功時）", "Pass Operation (On Success)"));
            DrawProperty("_StencilFail", L("Fail操作（ステンシル不合格時）", "Fail Operation (Stencil Fail)"));
            DrawProperty("_StencilZFail", L("ZFail操作（深度不合格時）", "ZFail Operation (Depth Fail)"));
            DrawProperty("_StencilReadMask", L("読み取りマスク", "Read Mask"));
            DrawProperty("_StencilWriteMask", L("書き込みマスク", "Write Mask"));
            DrawHelpToggle("Stencil",
                L("🎭 ステンシル:\n" +
                "ステンシルバッファを使用して描画マスクを制御します。\n\n" +
                "• Reference: 比較/書き込み用の参照値（0-255）\n" +
                "• Comparison: バッファ値と参照値の比較方法\n" +
                "  - Always: 常に合格 / Equal: 一致時のみ / NotEqual: 不一致時のみ\n" +
                "• Pass: ステンシル＆深度テスト両方合格時の操作\n" +
                "• Fail: ステンシルテスト不合格時の操作\n" +
                "• ZFail: ステンシル合格・深度テスト不合格時の操作\n" +
                "  - Keep: 変更なし / Replace: 参照値で上書き / Zero: 0にする\n" +
                "• Read/Write Mask: ビットマスク（255=全ビット）\n\n" +
                "💡 デフォルト値（Comp=Always, Op=Keep）では既存動作に影響しません。\n" +
                "💡 同じRef値を持つオブジェクト間でマスク効果を実現できます。\n\n" +
                "使用例:\n" +
                "• 書き込み側: Ref=1, Comp=Always, Pass=Replace\n" +
                "• 読み取り側: Ref=1, Comp=Equal, Pass=Keep",
                "🎭 Stencil:\n" +
                "Controls drawing masks using the stencil buffer.\n\n" +
                "• Reference: Reference value for comparison/writing (0-255)\n" +
                "• Comparison: How to compare buffer value with reference\n" +
                "  - Always: Always pass / Equal: Match only / NotEqual: Mismatch only\n" +
                "• Pass: Operation when both stencil & depth tests pass\n" +
                "• Fail: Operation when stencil test fails\n" +
                "• ZFail: Operation when stencil passes but depth fails\n" +
                "  - Keep: No change / Replace: Overwrite with ref / Zero: Set to 0\n" +
                "• Read/Write Mask: Bit mask (255=all bits)\n\n" +
                "💡 Default values (Comp=Always, Op=Keep) don't affect existing behavior.\n" +
                "💡 Achieve masking effects between objects with matching Ref values.\n\n" +
                "Examples:\n" +
                "• Write side: Ref=1, Comp=Always, Pass=Replace\n" +
                "• Read side: Ref=1, Comp=Equal, Pass=Keep"),
                MessageType.Info);

        }
        EndBoxedSection(GetFoldout("Rendering"));
    }

    private void DrawAOSection()
    {
        SetFoldout("AO", DrawBoxedSection(L("アンビエントオクルージョン（AO）", "Ambient Occlusion (AO)"), GetFoldout("AO"), SectionCategory.Lighting, "_USE_AO"));
        if (GetFoldout("AO"))
        {
            bool enableAO = DrawToggle("_USE_AO", "_UseAO", L("AO を有効化", "Enable AO"));
            if (enableAO)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_AOMap", L("AO マップ", "AO Map"));
                DrawProperty("_AOIntensity", L("AO強度", "AO Intensity"));
                DrawHelpToggle("AO",
                    L("🌑 アンビエントオクルージョン (AO):\n" +
                    "テクスチャの暗い部分で陰影を追加し、奥行き感を出します。\n" +
                    "• AO マップ: 白=影なし、黒=完全な影\n" +
                    "• AO強度: 0=効果なし、1=最大効果\n\n" +
                    "💡 キャラクターの脇の下や首元などの影表現に最適です。",
                    "🌑 Ambient Occlusion (AO):\n" +
                    "Adds shading in darker areas for depth.\n" +
                    "• AO Map: White=no shadow, Black=full shadow\n" +
                    "• AO Intensity: 0=no effect, 1=max effect\n\n" +
                    "💡 Ideal for shadow expression under arms or around the neck."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_AOBlend", "_AOBlendMode", "_AOBlur");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("AO"));
    }

    private void DrawDitheringSection()
    {
        SetFoldout("Dithering", DrawBoxedSection(L("ディザリング（スクリーントーン）", "Dithering (Screen Tone)"), GetFoldout("Dithering"), SectionCategory.Lighting, "_USE_DITHERING"));
        if (GetFoldout("Dithering"))
        {
            bool enableDithering = DrawToggle("_USE_DITHERING", "_UseDithering", L("ディザリング影の境界を有効化", "Enable Dithering Shadow Boundary"));
            if (enableDithering)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_DitheringScale", L("パターンサイズ", "Pattern Size"));
                DrawProperty("_DitheringStrength", L("境界のソフトネス", "Boundary Softness"));
                DrawHelpToggle("Dithering",
                    L("🔲 ディザリング影の境界:\n" +
                    "影の境界にディザパターンを適用して、滑らかなトーン遷移を実現します。\n" +
                    "• パターンサイズ: ディザパターンの大きさ（1-100）\n" +
                    "• ソフトネス: 境界のぼかし具合（0-1）\n\n" +
                    "💡 漫画やイラスト調のスクリーントーン表現に最適です。",
                    "🔲 Dithered Shadow Boundary:\n" +
                    "Applies dither pattern to shadow boundaries for smooth tone transitions.\n" +
                    "• Pattern Size: Dither pattern size (1-100)\n" +
                    "• Softness: Boundary blur amount (0-1)\n\n" +
                    "💡 Ideal for manga/illustration-style screen tone effects."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_DitheringBlend", null, "_DitheringBlur");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Dithering"));
    }

    private void DrawDecalSection()
    {
        SetFoldout("Decal", DrawBoxedSection(L("デカール（貼り付け）", "Decal"), GetFoldout("Decal"), SectionCategory.Effects, "_DECAL"));
        if (GetFoldout("Decal"))
        {
            bool enableDecal = DrawToggle("_DECAL", "_Decal", L("デカールを有効化", "Enable Decal"));
            if (enableDecal)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_DecalTex", L("デカールテクスチャ", "Decal Texture"));
                DrawColorProperty("_DecalColor", L("デカールカラー", "Decal Color"));
                DrawProperty("_DecalPosition", L("デカール位置 (XY)", "Decal Position (XY)"));
                DrawProperty("_DecalRotation", L("回転", "Rotation"));
                DrawProperty("_DecalScale", L("スケール", "Scale"));
                DrawProperty("_DecalBlendMode", L("合成モード", "Blend Mode"));
                DrawHelpToggle("Decal",
                    L("🏷️ デカール:\n" +
                    "メッシュ表面にテクスチャを貼り付けます。\n" +
                    "• 位置: UV空間でのデカール位置\n" +
                    "• 合成モード: Add/Multiply/Overlay/Replace\n\n" +
                    "💡 ロゴやマーク、タトゥー表現に最適です。",
                    "🏷️ Decal:\n" +
                    "Applies a texture onto the mesh surface.\n" +
                    "• Position: Decal position in UV space\n" +
                    "• Blend Mode: Add/Multiply/Overlay/Replace\n\n" +
                    "💡 Ideal for logos, marks, and tattoo effects."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_DecalBlend", null, "_DecalBlur");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_DecalDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Decal"));
    }

    private void DrawBackfaceSection()
    {
        SetFoldout("Backface", DrawBoxedSection(L("裏面テクスチャ", "Backface Texture"), GetFoldout("Backface"), SectionCategory.Advanced, "_BACKFACE_TEXTURE"));
        if (GetFoldout("Backface"))
        {
            bool enableBackface = DrawToggle("_BACKFACE_TEXTURE", "_BackfaceTexture", L("裏面テクスチャを有効化", "Enable Backface Texture"));
            if (enableBackface)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_BackfaceTex", L("裏面テクスチャ", "Backface Texture"));
                DrawColorProperty("_BackfaceColor", L("裏面カラー", "Backface Color"));
                DrawHelpToggle("BackfaceTexture",
                    L("🔄 裏面テクスチャ:\n" +
                    "ポリゴンの裏面に別のテクスチャを表示します。\n" +
                    "• カリングが Off の場合に裏面が見えます\n\n" +
                    "💡 服の裏地や紙の裏面表現に最適です。",
                    "🔄 Backface Texture:\n" +
                    "Displays a different texture on polygon backfaces.\n" +
                    "• Backfaces are visible when culling is Off\n\n" +
                    "💡 Ideal for clothing lining or paper backside."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_BackfaceBlend", "_BackfaceBlendMode");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Backface"));
    }

    private void DrawVideoSection()
    {
        SetFoldout("Video", DrawBoxedSection(L("ビデオテクスチャ", "Video Texture"), GetFoldout("Video"), SectionCategory.Advanced, "_VIDEO_TEXTURE"));
        if (GetFoldout("Video"))
        {
            bool enableVideo = DrawToggle("_VIDEO_TEXTURE", "_VideoTexture", L("ビデオテクスチャを有効化", "Enable Video Texture"));
            if (enableVideo)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_VideoTex", L("ビデオレンダーテクスチャ", "Video Render Texture"));
                DrawProperty("_VideoEmission", L("ビデオエミッション強度", "Video Emission Intensity"));
                DrawHelpToggle("VideoTexture",
                    L("📺 ビデオテクスチャ:\n" +
                    "RenderTexture を使用してビデオ映像をマテリアルに表示します。\n" +
                    "• エミッション: ビデオの発光強度\n\n" +
                    "💡 VRChat でのスクリーン表示やモニター表現に最適です。",
                    "📺 Video Texture:\n" +
                    "Displays video content on materials using RenderTexture.\n" +
                    "• Emission: Video glow intensity\n\n" +
                    "💡 Ideal for VRChat screen displays and monitors."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_VideoBlend", "_VideoBlendMode");
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Video"));
    }

    private void DrawAudioLinkSection()
    {
        SetFoldout("AudioLink", DrawBoxedSection(L("AudioLink（音楽連動）", "AudioLink (Music Reactive)"), GetFoldout("AudioLink"), SectionCategory.Effects, "_AUDIOLINK"));
        if (GetFoldout("AudioLink"))
        {
            bool enableAudioLink = DrawToggle("_AUDIOLINK", "_AudioLink", L("AudioLink を有効化", "Enable AudioLink"));
            if (enableAudioLink)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(L("エミッション連動", "Emission Linked"), EditorStyles.boldLabel);
                DrawProperty("_AudioLinkEmissionBand", L("周波数帯域", "Frequency Band"));
                DrawProperty("_AudioLinkEmissionIntensity", L("エミッション強度", "Emission Intensity"));

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(L("リムライト連動", "Rim Light Linked"), EditorStyles.boldLabel);
                DrawProperty("_AudioLinkRimBand", L("周波数帯域", "Frequency Band"));
                DrawProperty("_AudioLinkRimIntensity", L("リムライト強度", "Rim Light Intensity"));

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(L("色相シフト連動", "Hue Shift Linked"), EditorStyles.boldLabel);
                DrawProperty("_AudioLinkHueBand", L("周波数帯域", "Frequency Band"));
                DrawProperty("_AudioLinkHueShiftIntensity", L("色相シフト強度", "Hue Shift Intensity"));

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(L("ディゾルブ連動", "Dissolve Linked"), EditorStyles.boldLabel);
                DrawProperty("_AudioLinkDissolveBand", L("周波数帯域", "Frequency Band"));
                DrawProperty("_AudioLinkDissolveIntensity", L("ディゾルブ強度", "Dissolve Intensity"));

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(L("アウトライン連動", "Outline Linked"), EditorStyles.boldLabel);
                DrawProperty("_AudioLinkOutlineBand", L("周波数帯域", "Frequency Band"));
                DrawProperty("_AudioLinkOutlineIntensity", L("アウトライン強度", "Outline Intensity"));

                DrawHelpToggle("AudioLink",
                    L("🎵 AudioLink:\n" +
                    "AudioLink 対応ワールドで音楽に連動したエフェクトを実現します。\n" +
                    "• 周波数帯域: Bass/Low Mid/High Mid/Treble\n" +
                    "• 各エフェクトを個別に有効化できます\n" +
                    "• Chronotensity: 時間経過によるアニメーション\n\n" +
                    "💡 VRChat のクラブやライブイベント向けに最適です。",
                    "🎵 AudioLink:\n" +
                    "Enables music-reactive effects in AudioLink-compatible worlds.\n" +
                    "• Frequency Bands: Bass/Low Mid/High Mid/Treble\n" +
                    "• Each effect can be enabled individually\n" +
                    "• Chronotensity: Time-based animation\n\n" +
                    "💡 Ideal for VRChat clubs and live events."),
                    MessageType.Info);

                DrawBlendControls(materialEditor, targetMaterial, "_AudioLinkBlend", "_AudioLinkBlendMode");

                // Per-effect distance fade
                if (targetMaterial.IsKeywordEnabled("_DISTANCE_FADE"))
                {
                    DrawProperty("_AudioLinkDistFade", L("距離フェード強度", "Distance Fade Intensity"));
                }

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("AudioLink"));
    }

    private void DrawGradientBaseColorSection()
    {
        SetFoldout("GradientBaseColor", DrawBoxedSection(L("グラデーションベースカラー", "Gradient Base Color"), GetFoldout("GradientBaseColor"), SectionCategory.Basic, "_GRADIENT_BASE_COLOR"));
        if (GetFoldout("GradientBaseColor"))
        {
            bool enableGradient = DrawToggle("_GRADIENT_BASE_COLOR", "_GradientBaseColor", L("グラデーションベースカラーを有効化", "Enable Gradient Base Color"));
            if (enableGradient)
            {
                EditorGUI.indentLevel++;
                DrawColorProperty("_GradientTopColor", L("上部カラー", "Top Color"));
                DrawColorProperty("_GradientBottomColor", L("下部カラー", "Bottom Color"));
                DrawProperty("_GradientAxis", L("グラデーション軸", "Gradient Axis"));
                DrawProperty("_GradientSpace", L("座標空間", "Coordinate Space"));
                DrawProperty("_GradientStart", L("開始位置", "Start Position"));
                DrawProperty("_GradientEnd", L("終了位置", "End Position"));
                DrawProperty("_GradientBlendMode", L("ブレンドモード", "Blend Mode"));
                DrawProperty("_GradientBlend", L("ブレンド強度", "Blend Intensity"));
                DrawHelpToggle("GradientBaseColor",
                    L("🎨 グラデーションベースカラー:\n" +
                    "ベースカラーに位置ベースのグラデーションを適用します。\n\n" +
                    "• 上部/下部カラー: グラデーションの両端の色\n" +
                    "• 軸: グラデーション方向（X/Y/Z）\n" +
                    "• 座標空間: ローカル（オブジェクト基準）またはワールド\n" +
                    "• ブレンドモード: Normal/Soft/Screen/Overlay\n" +
                    "• ブレンド: 0=元のテクスチャ色、1=フルグラデーション",
                    "🎨 Gradient Base Color:\n" +
                    "Applies position-based gradient to the base color.\n\n" +
                    "• Top/Bottom Color: Colors at gradient endpoints\n" +
                    "• Axis: Gradient direction (X/Y/Z)\n" +
                    "• Coordinate Space: Local (object-based) or World\n" +
                    "• Blend Mode: Normal/Soft/Screen/Overlay\n" +
                    "• Blend: 0=original texture color, 1=full gradient"),
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("GradientBaseColor"));
    }

    private void DrawHeightFadeSection()
    {
        SetFoldout("HeightFade", DrawBoxedSection(L("高さフェード", "Height Fade"), GetFoldout("HeightFade"), SectionCategory.Advanced, "_HEIGHT_FADE"));
        if (GetFoldout("HeightFade"))
        {
            bool enableHeightFade = DrawToggle("_HEIGHT_FADE", "_HeightFade", L("高さフェードを有効化", "Enable Height Fade"));
            if (enableHeightFade)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_HeightFadeAxis", L("フェード軸", "Fade Axis"));
                DrawProperty("_HeightFadeSpace", L("座標空間", "Coordinate Space"));
                DrawProperty("_HeightFadeStart", L("フェード開始位置", "Fade Start Position"));
                DrawProperty("_HeightFadeEnd", L("フェード終了位置", "Fade End Position"));
                DrawProperty("_HeightFadeInvert", L("方向を反転", "Invert Direction"));
                DrawProperty("_HeightFadeMode", L("フェードモード", "Fade Mode"));
                DrawHelpToggle("HeightFade",
                    L("📐 高さフェード:\n" +
                    "位置（高さ）に応じてオブジェクトをフェードします。\n\n" +
                    "• 軸: フェード方向（X/Y/Z）\n" +
                    "• 座標空間: ローカル（オブジェクト基準）またはワールド\n" +
                    "• Alpha: 透明度でフェード\n" +
                    "• Clip: ハードクリップ\n" +
                    "• Dithering: ディザパターンでクリップ\n\n" +
                    "💡 地面から消えるエフェクトや、高さに応じた表示制御に最適です。",
                    "📐 Height Fade:\n" +
                    "Fades objects based on position (height).\n\n" +
                    "• Axis: Fade direction (X/Y/Z)\n" +
                    "• Coordinate Space: Local (object-based) or World\n" +
                    "• Alpha: Fade with transparency\n" +
                    "• Clip: Hard clip\n" +
                    "• Dithering: Dither pattern clip\n\n" +
                    "💡 Ideal for ground-disappearing effects or height-based visibility."),
                    MessageType.Info);

                // Show blend only in Alpha mode
                float heightFadeMode = FindProperty("_HeightFadeMode", properties).floatValue;
                if (heightFadeMode < 0.5f)
                {
                    DrawProperty("_HeightFadeBlend", L("ブレンド強度", "Blend Intensity"));
                }

                // Show dither scale only in Dithering mode
                if (heightFadeMode > 1.5f)
                {
                    DrawProperty("_HeightFadeDitherScale", L("ディザスケール", "Dither Scale"));
                }

                DrawProperty("_HeightFadeEdgeWidth", L("エッジグロー幅", "Edge Glow Width"));
                float edgeWidth = FindProperty("_HeightFadeEdgeWidth", properties).floatValue;
                if (edgeWidth > MIN_PARAMETER_VALUE)
                {
                    DrawColorProperty("_HeightFadeEdgeColor", L("エッジグローカラー", "Edge Glow Color"), true);
                }
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("HeightFade"));
    }

    private void DrawIntersectionFadeSection()
    {
        SetFoldout("IntersectionFade", DrawBoxedSection(L("オブジェクト交差フェード", "Intersection Fade"), GetFoldout("IntersectionFade"), SectionCategory.Advanced, "_INTERSECTION_FADE"));
        if (GetFoldout("IntersectionFade"))
        {
            bool enableIntersectionFade = DrawToggle("_INTERSECTION_FADE", "_IntersectionFade", L("交差フェードを有効化", "Enable Intersection Fade"));
            if (enableIntersectionFade)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_IntersectionFadeDistance", L("フェード距離", "Fade Distance"));
                DrawProperty("_IntersectionFadeMode", L("フェードモード", "Fade Mode"));
                DrawHelpToggle("IntersectionFade",
                    L("🔀 オブジェクト交差フェード:\n" +
                    "他のオブジェクトとの交差部分を透明にします。\n\n" +
                    "• フェード距離: 交差部分のフェード範囲\n" +
                    "• Alpha: 透明度でフェード\n" +
                    "• Clip: ハードクリップ\n" +
                    "• Dithering: ディザパターンでクリップ\n\n" +
                    "💡 地面との交差部分や、オブジェクト間の滑らかな接続に最適です。\n" +
                    "⚠️ 深度テクスチャが必要です（カメラのDepthTextureMode）。",
                    "🔀 Object Intersection Fade:\n" +
                    "Makes intersection areas with other objects transparent.\n\n" +
                    "• Fade Distance: Intersection fade range\n" +
                    "• Alpha: Fade with transparency\n" +
                    "• Clip: Hard clip\n" +
                    "• Dithering: Dither pattern clip\n\n" +
                    "💡 Ideal for ground intersection or smooth object connections.\n" +
                    "⚠️ Requires depth texture (camera DepthTextureMode)."),
                    MessageType.Info);

                // Show blend only in Alpha mode
                float intersectionFadeMode = FindProperty("_IntersectionFadeMode", properties).floatValue;
                if (intersectionFadeMode < 0.5f)
                {
                    DrawProperty("_IntersectionFadeBlend", L("ブレンド強度", "Blend Intensity"));
                }

                // Show dither scale only in Dithering mode
                if (intersectionFadeMode > 1.5f)
                {
                    DrawProperty("_IntersectionFadeDitherScale", L("ディザスケール", "Dither Scale"));
                }

                DrawProperty("_IntersectionFadeEdgeWidth", L("エッジ幅", "Edge Width"));
                float intersectionEdgeWidth = FindProperty("_IntersectionFadeEdgeWidth", properties).floatValue;
                if (intersectionEdgeWidth > MIN_PARAMETER_VALUE)
                {
                    DrawColorProperty("_IntersectionFadeEdgeColor", L("エッジカラー", "Edge Color"), true);
                }
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("IntersectionFade"));
    }

    private void DrawDistanceFadeSection()
    {
        SetFoldout("DistanceFade", DrawBoxedSection(L("距離フェード", "Distance Fade"), GetFoldout("DistanceFade"), SectionCategory.Advanced, "_DISTANCE_FADE"));
        if (GetFoldout("DistanceFade"))
        {
            bool enableDistanceFade = DrawToggle("_DISTANCE_FADE", "_DistanceFade", L("距離フェードを有効化", "Enable Distance Fade"));
            if (enableDistanceFade)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_DistanceFadeStart", L("フェード開始距離", "Fade Start Distance"));
                DrawProperty("_DistanceFadeEnd", L("フェード終了距離", "Fade End Distance"));
                DrawProperty("_DistanceFadeMode", L("フェードモード", "Fade Mode"));
                DrawHelpToggle("DistanceFade",
                    L("📏 距離フェード:\n" +
                    "カメラからの距離に応じてオブジェクトをフェードします。\n" +
                    "• 開始距離: フェードが始まる距離\n" +
                    "• 終了距離: 完全に消える距離\n" +
                    "• Alpha: 透明度でフェード\n" +
                    "• Simplify: エフェクトを簡略化（ハードクリップ）\n" +
                    "• Dithering: ディザパターンで段階的にクリップ\n\n" +
                    "💡 パフォーマンス最適化に有効です。遠距離のオブジェクト描画負荷を軽減します。\n" +
                    "💡 Ditheringモードは不透明マテリアルでも自然なフェードが可能です。",
                    "📏 Distance Fade:\n" +
                    "Fades objects based on camera distance.\n" +
                    "• Start Distance: Distance where fade begins\n" +
                    "• End Distance: Distance where fully invisible\n" +
                    "• Alpha: Fade with transparency\n" +
                    "• Simplify: Simplified effect (hard clip)\n" +
                    "• Dithering: Gradual dither pattern clip\n\n" +
                    "💡 Effective for performance optimization. Reduces draw cost for distant objects.\n" +
                    "💡 Dithering mode enables natural fade even on opaque materials."),
                    MessageType.Info);

                // Show dither scale only in Dithering mode
                if (targetMaterial.GetFloat("_DistanceFadeMode") > 1.5)
                {
                    DrawProperty("_DistFadeDitherScale", L("ディザスケール", "Dither Scale"));
                }

                DrawBlendControls(materialEditor, targetMaterial, "_DistanceFadeBlend", null, "_DistFadeBlur");

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("近距離フェード", "Near Fade"), EditorStyles.boldLabel);
                DrawProperty("_NearFadeStart", L("開始距離（完全透明）", "Start Distance (Fully Transparent)"));
                DrawProperty("_NearFadeEnd", L("終了距離（完全表示）", "End Distance (Fully Visible)"));
                DrawHelpToggle("NearFade",
                    L("📏 近距離フェード:\n" +
                    "カメラが近すぎるとオブジェクトが透明になります。\n" +
                    "VRChatのアバター近接フェードに最適。\n\n" +
                    "• 開始距離: この距離以下で完全に透明\n" +
                    "• 終了距離: この距離以上で完全に表示\n" +
                    "• 推奨値: 開始0.1〜0.2, 終了0.3〜0.5",
                    "📏 Near Fade:\n" +
                    "Objects become transparent when camera is too close.\n" +
                    "Ideal for VRChat avatar proximity fade.\n\n" +
                    "• Start Distance: Fully transparent below this distance\n" +
                    "• End Distance: Fully visible above this distance\n" +
                    "• Recommended: Start 0.1-0.2, End 0.3-0.5"),
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("DistanceFade"));
    }

    // ===== Phase 2/3: New Background Shader Features =====

    private void DrawDetailMapSection()
    {
        SetFoldout("DetailMap", DrawBoxedSection(L("ディテールマップ", "Detail Map"), GetFoldout("DetailMap"), SectionCategory.Advanced, "_DETAIL_MAP"));
        if (GetFoldout("DetailMap"))
        {
            bool enableDetailMap = DrawToggle("_DETAIL_MAP", "_DetailMap", L("ディテールマップを有効化", "Enable Detail Map"));
            if (enableDetailMap)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("ディテールマップ設定", "Detail Map Settings"), EditorStyles.boldLabel);

                DrawProperty("_DetailAlbedoMap", L("ディテール アルベド", "Detail Albedo Map"));
                DrawProperty("_DetailAlbedoScale", L("アルベド影響度", "Albedo Scale"));
                DrawProperty("_DetailNormalMap", L("ディテール法線マップ", "Detail Normal Map"));
                DrawProperty("_DetailNormalScale", L("法線マップ強度", "Normal Scale"));
                DrawProperty("_DetailTiling", L("タイリング", "Tiling"));
                DrawProperty("_DetailUVSet", L("UVセット", "UV Set"));

                DrawHelpToggle("DetailMap",
                    L("📍 ディテールマップ:\n" +
                    "近距離でのテクスチャ解像感を向上させます。\n" +
                    "セカンダリUV（UV1）を使用可能。\n\n" +
                    "• アルベド: メインテクスチャに乗算ブレンド\n" +
                    "• 法線マップ: 既存法線に加算ブレンド\n" +
                    "• タイリング: テクスチャの繰り返し回数\n\n" +
                    "💡 背景の壁面や床の細かい質感表現に最適。",
                    "📍 Detail Map:\n" +
                    "Improves texture resolution at close range.\n" +
                    "Can use secondary UV (UV1).\n\n" +
                    "• Albedo: Multiply blend with main texture\n" +
                    "• Normal: Additive blend with existing normals\n" +
                    "• Tiling: Texture repeat count\n\n" +
                    "💡 Perfect for detailed wall and floor textures."),
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("DetailMap"));
    }

    private void DrawTriplanarSection()
    {
        SetFoldout("Triplanar", DrawBoxedSection(L("トライプレーナー", "Triplanar Mapping"), GetFoldout("Triplanar"), SectionCategory.Advanced, "_TRIPLANAR"));
        if (GetFoldout("Triplanar"))
        {
            bool enableTriplanar = DrawToggle("_TRIPLANAR", "_Triplanar", L("トライプレーナーを有効化", "Enable Triplanar Mapping"));
            if (enableTriplanar)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("トライプレーナー設定", "Triplanar Settings"), EditorStyles.boldLabel);

                DrawProperty("_TriplanarScale", L("テクスチャスケール", "Texture Scale"));
                DrawProperty("_TriplanarBlendSharpness", L("ブレンドシャープネス", "Blend Sharpness"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("軸オフセット", "Axis Offset"), EditorStyles.boldLabel);
                DrawProperty("_TriplanarOffsetX", L("Xオフセット", "X Offset"));
                DrawProperty("_TriplanarOffsetY", L("Yオフセット", "Y Offset"));
                DrawProperty("_TriplanarOffsetZ", L("Zオフセット", "Z Offset"));

                DrawHelpToggle("Triplanar",
                    L("📍 トライプレーナーマッピング:\n" +
                    "UV展開なしで3軸からテクスチャを投影します。\n" +
                    "岩、洞窟、地形などUVが不要な場面に最適。\n\n" +
                    "• スケール: テクスチャの大きさ\n" +
                    "• シャープネス: 軸間ブレンドの鋭さ（1=滑らか、8=鋭い）\n\n" +
                    "⚠️ メインテクスチャのUVサンプリングを置き換えます。",
                    "📍 Triplanar Mapping:\n" +
                    "Projects texture from 3 axes without UV unwrapping.\n" +
                    "Perfect for rocks, caves, terrain with no UV needed.\n\n" +
                    "• Scale: Texture size\n" +
                    "• Sharpness: Axis blend sharpness (1=smooth, 8=sharp)\n\n" +
                    "⚠️ Replaces main texture UV sampling."),
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("Triplanar"));
    }

    private void DrawHeightFogSection()
    {
        SetFoldout("HeightFog", DrawBoxedSection(L("ハイトフォグ", "Height Fog"), GetFoldout("HeightFog"), SectionCategory.Environment, "_HEIGHT_FOG"));
        if (GetFoldout("HeightFog"))
        {
            bool enableHeightFog = DrawToggle("_HEIGHT_FOG", "_HeightFog", L("ハイトフォグを有効化", "Enable Height Fog"));
            if (enableHeightFog)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("ハイトフォグ設定", "Height Fog Settings"), EditorStyles.boldLabel);

                DrawColorProperty("_HeightFogColor", L("フォグカラー", "Fog Color"));
                DrawProperty("_HeightFogStart", L("開始高さ (Y)", "Start Height (Y)"));
                DrawProperty("_HeightFogEnd", L("終了高さ (Y)", "End Height (Y)"));
                DrawProperty("_HeightFogDensity", L("フォグ密度", "Fog Density"));
                DrawProperty("_HeightFogMode", L("フォグモード", "Fog Mode"));

                DrawHelpToggle("HeightFog",
                    L("🌫️ ハイトフォグ:\n" +
                    "ワールドY座標に基づくマテリアルレベルのフォグ効果。\n" +
                    "VRChatではポストプロセスが使えないため、マテリアルで実装。\n\n" +
                    "• 開始高さ: フォグが始まるY座標\n" +
                    "• 終了高さ: フォグが完全にかかるY座標\n" +
                    "• Linear: 直線的な減衰\n" +
                    "• Exponential: 指数関数的な減衰（自然な霧）\n\n" +
                    "💡 幻想的な雰囲気や朝霧の表現に最適。",
                    "🌫️ Height Fog:\n" +
                    "Material-level fog based on world Y coordinate.\n" +
                    "Implemented in material since VRChat has no post-processing.\n\n" +
                    "• Start Height: Y coordinate where fog begins\n" +
                    "• End Height: Y coordinate where fog is fully applied\n" +
                    "• Linear: Linear falloff\n" +
                    "• Exponential: Exponential falloff (natural fog)\n\n" +
                    "💡 Perfect for fantasy atmosphere and morning mist."),
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("HeightFog"));
    }

    private void DrawSurfaceCoverSection()
    {
        SetFoldout("SurfaceCover", DrawBoxedSection(L("サーフェスカバー（雪/砂）", "Surface Cover (Snow/Sand)"), GetFoldout("SurfaceCover"), SectionCategory.Effects, "_SURFACE_COVER"));
        if (GetFoldout("SurfaceCover"))
        {
            bool enableCover = DrawToggle("_SURFACE_COVER", "_SurfaceCover", L("サーフェスカバーを有効化", "Enable Surface Cover"));
            if (enableCover)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("サーフェスカバー設定", "Surface Cover Settings"), EditorStyles.boldLabel);

                DrawProperty("_CoverTex", L("カバーテクスチャ", "Cover Texture"));
                DrawColorProperty("_CoverColor", L("カバーカラー", "Cover Color"));
                DrawProperty("_CoverNormalMap", L("カバー法線マップ", "Cover Normal Map"));
                DrawProperty("_CoverAmount", L("カバー量", "Cover Amount"));
                DrawProperty("_CoverThreshold", L("法線閾値", "Normal Threshold"));
                DrawProperty("_CoverBlendSharpness", L("ブレンドシャープネス", "Blend Sharpness"));
                DrawProperty("_CoverTiling", L("タイリング", "Tiling"));

                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("カバー方向", "Cover Direction"), EditorStyles.boldLabel);
                DrawProperty("_CoverDirection", L("方向ベクトル", "Direction Vector"));

                DrawHelpToggle("SurfaceCover",
                    L("❄️ サーフェスカバー:\n" +
                    "ワールド空間の上方向にテクスチャを重ねて雪や砂を表現。\n\n" +
                    "• カバー量: 全体の堆積量 (0=なし、1=最大)\n" +
                    "• 法線閾値: 上向きの面のみにカバー (0=全面、1=真上のみ)\n" +
                    "• シャープネス: ブレンド境界の鋭さ\n" +
                    "• 方向: デフォルト(0,1,0)=上から。変更で斜め方向も可能。\n\n" +
                    "💡 季節表現（雪景色）やファンタジー環境に有効。",
                    "❄️ Surface Cover:\n" +
                    "Overlays texture on upward-facing surfaces for snow/sand.\n\n" +
                    "• Cover Amount: Overall accumulation (0=none, 1=max)\n" +
                    "• Threshold: Only cover upward faces (0=all, 1=top only)\n" +
                    "• Sharpness: Blend edge sharpness\n" +
                    "• Direction: Default (0,1,0)=from above. Change for angled cover.\n\n" +
                    "💡 Great for seasonal (snow) and fantasy environments."),
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("SurfaceCover"));
    }

    private void DrawMirrorControlSection()
    {
        SetFoldout("MirrorControl", DrawBoxedSection(L("ミラー対応", "Mirror Control"), GetFoldout("MirrorControl"), SectionCategory.Advanced, "_MIRROR_CONTROL"));
        if (GetFoldout("MirrorControl"))
        {
            bool enableMirror = DrawToggle("_MIRROR_CONTROL", "_MirrorControl", L("ミラー制御を有効化", "Enable Mirror Control"));
            if (enableMirror)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SECTION_SPACING);
                EditorGUILayout.LabelField(L("ミラー制御設定", "Mirror Control Settings"), EditorStyles.boldLabel);

                DrawProperty("_MirrorMode", L("表示モード", "Display Mode"));
                DrawProperty("_MirrorEmissionMultiplier", L("ミラー内エミッション倍率", "Mirror Emission Multiplier"));

                DrawHelpToggle("MirrorControl",
                    L("🪞 ミラー対応:\n" +
                    "VRChatミラー内での描画を制御します。\n\n" +
                    "• 両方表示: 通常・ミラー両方で表示\n" +
                    "• ミラーのみ: ミラー内でのみ表示\n" +
                    "• ミラー以外のみ: 通常時のみ表示\n\n" +
                    "💡 ミラー限定の隠し装飾や、ミラー内のパフォーマンス最適化に活用。",
                    "🪞 Mirror Control:\n" +
                    "Controls rendering in VRChat mirrors.\n\n" +
                    "• Both: Show in normal and mirror view\n" +
                    "• Mirror Only: Show only in mirror\n" +
                    "• Non-Mirror Only: Show only in normal view\n\n" +
                    "💡 Use for hidden mirror decorations or mirror performance optimization."),
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("MirrorControl"));
    }

    private void DrawQuestLiteSection()
    {
        SetFoldout("QuestLite", DrawBoxedSection(L("Quest軽量パス", "Quest Lite"), GetFoldout("QuestLite"), SectionCategory.Advanced, "_QUEST_LITE"));
        if (GetFoldout("QuestLite"))
        {
            bool enableQuestLite = DrawToggle("_QUEST_LITE", "_QuestLite", L("Quest軽量モードを有効化", "Enable Quest Lite Mode"));
            if (enableQuestLite)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox(
                    L("⚡ Quest軽量モードが有効です。以下のエフェクトが自動的にスキップされます:\n" +
                    "• MatCap 2nd / 3rd\n" +
                    "• グリッター / 水滴\n" +
                    "• ホログラム / グリッチ\n" +
                    "• インターセクションフェード\n\n" +
                    "これにより、モバイルGPUでのパフォーマンスが30-50%改善します。",
                    "⚡ Quest Lite mode is active. The following effects are automatically skipped:\n" +
                    "• MatCap 2nd / 3rd\n" +
                    "• Glitter / Water Drip\n" +
                    "• Hologram / Glitch\n" +
                    "• Intersection Fade\n\n" +
                    "This improves mobile GPU performance by 30-50%."),
                    MessageType.Warning);

                EditorGUI.indentLevel--;
            }
        }
        EndBoxedSection(GetFoldout("QuestLite"));
    }

    /// <summary>
    /// Get current rendering mode based on shader name
    /// </summary>
    private RenderingMode GetCurrentRenderingMode()
    {
        if (targetMaterial == null || targetMaterial.shader == null)
            return RenderingMode.Opaque;

        string shaderName = targetMaterial.shader.name;

        if (shaderName.Contains("Background"))
            return RenderingMode.Background;
        else if (shaderName.Contains("Fur"))
            return RenderingMode.Fur;
        else if (shaderName.Contains("Transparent"))
            return RenderingMode.Transparent;
        else if (shaderName.Contains("Cutout"))
            return RenderingMode.Cutout;
        else
            return RenderingMode.Opaque;
    }

    /// <summary>
    /// Set rendering mode by switching to appropriate shader variant
    /// </summary>
    private bool SetRenderingMode(RenderingMode mode)
    {
        if (targetMaterial == null)
        {
            NataneToon.Editor.NataneToonErrorDialog.ShowNullMaterialError(L("描画タイプの変更", "Change Rendering Mode"));
            return false;
        }

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
            case RenderingMode.Fur:
                newShaderName = baseShaderName + " (Fur)";
                break;
            case RenderingMode.Background:
                newShaderName = baseShaderName + " (Background)";
                break;
        }

        // Find the shader
        Shader newShader = Shader.Find(newShaderName);
        if (newShader == null)
        {
            Debug.LogError($"[NataneToonShaderGUI] Shader not found: {newShaderName}");
            NataneToon.Editor.NataneToonErrorDialog.ShowShaderNotFoundError(newShaderName);
            return false;
        }

        // Check if already using this shader
        if (targetMaterial.shader == newShader)
            return true;

        // Record undo
        Undo.RecordObject(targetMaterial, "Change Rendering Mode");

        // Switch shader (properties with same names are preserved)
        targetMaterial.shader = newShader;

        // Disable fur keyword when switching away from Fur mode
        if (mode != RenderingMode.Fur)
        {
            targetMaterial.SetFloat("_Fur", 0);
            targetMaterial.DisableKeyword("_FUR");
        }

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

            case RenderingMode.Fur:
                targetMaterial.SetFloat("_ZWrite", 0);
                targetMaterial.SetFloat("_Fur", 1);
                targetMaterial.EnableKeyword("_FUR");
                targetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;

            case RenderingMode.Background:
                targetMaterial.SetFloat("_ZWrite", 1);
                targetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                break;
        }

        // Mark as dirty
        EditorUtility.SetDirty(targetMaterial);

        // Repaint inspector
        if (materialEditor != null)
        {
            materialEditor.Repaint();
        }

        return true;
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

    // Blend mode preset helpers
    private int GetBlendPresetIndex(int srcBlend, int dstBlend)
    {
        if (srcBlend == 5 && dstBlend == 10) return 0; // Alpha Blend (SrcAlpha / OneMinusSrcAlpha)
        if (srcBlend == 1 && dstBlend == 1) return 1;  // Additive (One / One)
        if (srcBlend == 1 && dstBlend == 10) return 2;  // Premultiplied Alpha (One / OneMinusSrcAlpha)
        if (srcBlend == 2 && dstBlend == 0) return 3;   // Multiplicative (DstColor / Zero)
        return 4; // Custom
    }

    private void ApplyBlendPreset(int preset, MaterialProperty srcBlend, MaterialProperty dstBlend)
    {
        switch (preset)
        {
            case 0: srcBlend.floatValue = 5; dstBlend.floatValue = 10; break; // Alpha Blend
            case 1: srcBlend.floatValue = 1; dstBlend.floatValue = 1; break;  // Additive
            case 2: srcBlend.floatValue = 1; dstBlend.floatValue = 10; break; // Premultiplied Alpha
            case 3: srcBlend.floatValue = 2; dstBlend.floatValue = 0; break;  // Multiplicative
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

        EditorGUILayout.Space(SECTION_SPACING);
        EditorGUILayout.LabelField(L("── ブレンド＆ブラー ──", "── Blend & Blur ──"), EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        if (hasBlendMode)
        {
            MaterialProperty blendModeMat = FindProperty(blendModeProp, properties, false);
            if (blendModeMat != null)
                materialEditor.ShaderProperty(blendModeMat, L("ブレンドモード", "Blend Mode"));
        }
        if (hasBlend)
        {
            MaterialProperty blendMat = FindProperty(blendProp, properties, false);
            if (blendMat != null)
                materialEditor.ShaderProperty(blendMat, L("ブレンド", "Blend"));
        }
        if (hasBlur)
        {
            MaterialProperty blurMat = FindProperty(blurProp, properties, false);
            if (blurMat != null)
                materialEditor.ShaderProperty(blurMat, L("ブラー", "Blur"));
        }

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// Draw UV animation settings (scroll speed XY + rotation speed) for any texture
    /// </summary>
    private void DrawUVAnimationSettings(string scrollProp, string rotateProp, string label)
    {
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField(L($"UVアニメーション ({label})", $"UV Animation ({label})"), EditorStyles.miniLabel);
        DrawProperty(scrollProp, L("スクロール速度 XY", "Scroll Speed XY"));
        DrawProperty(rotateProp, L("回転速度", "Rotation Speed"));
    }

    /// <summary>
    /// Draw Emission-specific UV animation settings (backward-compatible Float×2 + Float layout)
    /// </summary>
    private void DrawEmissionUVAnimationSettings()
    {
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField(L("UVアニメーション (エミッション)", "UV Animation (Emission)"), EditorStyles.miniLabel);
        DrawProperty("_EmissionScrollSpeed", L("スクロール速度 X", "Scroll Speed X"));
        DrawProperty("_EmissionScrollSpeedY", L("スクロール速度 Y", "Scroll Speed Y"));
        DrawProperty("_EmissionRotateSpeed", L("回転速度", "Rotation Speed"));
    }

    /// <summary>
    /// Draw presets and sharing section
    /// </summary>
    private void DrawPresetsSection()
    {
        SetFoldout("Presets", DrawBoxedSection(L("マテリアルプリセット＆共有", "Material Presets & Sharing"), GetFoldout("Presets"), SectionCategory.Basic));
        if (GetFoldout("Presets"))
        {
            NataneToonShaderGUIUtility.DrawMaterialActionsToolbar(targetMaterial, materialEditor);
        }
        EndBoxedSection(GetFoldout("Presets"));
    }

    /// <summary>
    /// Draw Feature Overview panel - shows all feature ON/OFF states in a compact grid
    /// 機能一覧パネル - 全機能のON/OFF状態をコンパクトなグリッドで表示
    /// </summary>
    private void DrawFeatureOverviewSection()
    {
        SetFoldout("FeatureOverview", DrawBoxedSection(L("機能一覧", "Feature Overview"), GetFoldout("FeatureOverview"), SectionCategory.Basic));
        if (GetFoldout("FeatureOverview"))
        {
            // Feature keywords and display names for the overview grid
            string[][] features = new string[][]
            {
                new[] { "_SPECULAR", L("スペキュラー", "Specular") },
                new[] { "_HAIR_SPECULAR", L("ヘアハイライト", "Hair Highlight") },
                new[] { "_RIM_LIGHT", L("リムライト", "Rim Light") },
                new[] { "_SSS", "SSS" },
                new[] { "_MATCAP", "MatCap" },
                new[] { "_GLITTER", L("グリッター", "Glitter") },
                new[] { "_WATER_DRIP", L("雫", "Drip") },
                new[] { "_SMEAR", L("スミア", "Smear") },
                new[] { "_HOLOGRAM", L("ホログラム", "Hologram") },
                new[] { "_DECAL", L("デカール", "Decal") },
                new[] { "_OUTLINE", L("アウトライン", "Outline") },
                new[] { "_EMISSION", L("エミッション", "Emission") },
                new[] { "_AUDIOLINK", "AudioLink" },
                new[] { "_REFLECTION", L("リフレクション", "Reflection") },
                new[] { "_IRIDESCENCE", L("イリデッセンス", "Iridescence") },
                new[] { "_ENV_RIM", L("環境リム", "Env Rim") },
                new[] { "_REFRACTION", L("屈折", "Refraction") },
                new[] { "_NORMALMAP", L("ノーマルマップ", "Normal Map") },
                new[] { "_PARALLAX", L("パララックス", "Parallax") },
                new[] { "_VERTEX_ANIMATION", L("頂点アニメーション", "Vertex Anim") },
                new[] { "_VAT", "VAT" },
                new[] { "_USE_AO", "AO" },
                new[] { "_USE_DITHERING", L("ディザリング", "Dithering") },
                new[] { "_USE_LIGHT_VOLUME", "Light Volume" },
                new[] { "_DISTANCE_FADE", L("距離フェード", "Dist Fade") },
                new[] { "_BACKFACE_TEXTURE", L("裏面", "Backface") },
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
            EditorGUILayout.LabelField(L($"有効機能: {enabledCount}/{features.Length}", $"Active Features: {enabledCount}/{features.Length}"), GUILayout.Width(120));
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
        EndBoxedSection(GetFoldout("FeatureOverview"));
    }

    /// <summary>
    /// Draw performance indicator section
    /// </summary>
    private void DrawPerformanceSection()
    {
        SetFoldout("Performance", DrawBoxedSection(L("パフォーマンス", "Performance"), GetFoldout("Performance"), SectionCategory.Basic));
        if (GetFoldout("Performance"))
        {
            NataneToonShaderGUIUtility.DrawPerformanceIndicator(targetMaterial);

            DrawHelpToggle("PerformanceHint",
                L("ヒント：使用していない機能を無効化するとパフォーマンスが向上します。\n" +
                "チェックボックスのある機能はオン/オフの切り替えが可能です。",
                "Hint: Disabling unused features improves performance.\n" +
                "Features with checkboxes can be toggled on/off."),
                MessageType.Info);
        }
        EndBoxedSection(GetFoldout("Performance"));
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

    // LoadFoldoutStates/SaveFoldoutStates removed: Dictionary-based GetFoldout/SetFoldout
    // handles lazy loading from EditorPrefs and immediate persistence on change.

    // ===== UI HELPER METHODS =====

    /// <summary>
    /// Draw expand all / collapse all buttons for a tab
    /// タブ用の全展開/全折畳ボタンを描画
    /// </summary>
    private void DrawExpandCollapseButtons(System.Action<bool> setAllFoldouts)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(L("全展開", "Expand All"), EditorStyles.miniButtonLeft, GUILayout.Width(50)))
        {
            setAllFoldouts(true);
        }
        if (GUILayout.Button(L("全折畳", "Collapse All"), EditorStyles.miniButtonRight, GUILayout.Width(50)))
        {
            setAllFoldouts(false);
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

        // Language toggle button
        string langLabel = IsJapanese ? "JP" : "EN";
        if (GUILayout.Button(langLabel, GUILayout.Width(30), GUILayout.Height(20)))
        {
            NataneToonLocalization.ToggleLanguage();
            materialEditor?.Repaint();
        }

        // Quick validation button
        if (GUILayout.Button(new GUIContent(L("更新", "Sync"), L("キーワード検証", "Keyword Validation")), GUILayout.Width(40), GUILayout.Height(20)))
        {
            ValidateAndFixKeywords();
        }

        // Expand/Collapse buttons
        if (GUILayout.Button(L("展開", "Expand"), GUILayout.Width(50), GUILayout.Height(20)))
        {
            ExpandAllSections(true);

            // Force repaint and exit GUI to prevent layout conflicts
            if (materialEditor != null)
            {
                materialEditor.Repaint();
            }
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button(L("折畳", "Collapse"), GUILayout.Width(50), GUILayout.Height(20)))
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
        EditorGUILayout.Space(SECTION_SPACING);
    }

    // ===== TAB DRAWING METHODS =====

    /// <summary>
    /// Draw Basic tab content (Main Texture, Surface Finish, Makeup, Shading)
    /// </summary>
    private void DrawBasicTab()
    {
        SafeDrawSection(DrawPresetsSection, L("プリセット", "Presets"));
        SafeDrawSection(DrawFeatureOverviewSection, L("機能一覧", "Feature Overview"));
        SafeDrawSection(DrawPerformanceSection, L("パフォーマンス", "Performance"));
        SafeDrawSection(DrawQuickSetupSection, L("クイックセットアップ", "Quick Setup")); // 3.2 Quick Setup
        EditorGUILayout.Space(SECTION_SPACING);

        SafeDrawSection(DrawMainTextureSection, L("メインテクスチャ", "Main Texture"));
        DrawSurfaceFinishSection();
        EditorGUILayout.Space(SECTION_SPACING);
        SafeDrawSection(DrawMakeupTexturesSection, L("メイクアップテクスチャ", "Makeup Textures"));
        SafeDrawSection(DrawScreenToneSection, L("スクリーントーン", "Screen Tone"));
        SafeDrawSection(DrawGradientBaseColorSection, L("グラデーションベースカラー", "Gradient Base Color"));
        SafeDrawSection(DrawShadingSection, L("シェーディング", "Shading"));
    }

    /// <summary>
    /// Draw Lighting tab content
    /// </summary>
    private void DrawLightingTab()
    {
        DrawExpandCollapseButtons((state) => {
            SetFoldout("AdvancedLighting", state); SetFoldout("AO", state); SetFoldout("Dithering", state);
            SetFoldout("LightVolume", state); SetFoldout("LTCGI", state);
            SetFoldout("BackgroundLightmap", state); SetFoldout("PBR", state);
        });
        // ─── ライティング基本 ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("ライティング基本", "Lighting Basics"));
        SafeDrawSection(DrawAdvancedLightingSection, L("高度なライティング", "Advanced Lighting"));
        SafeDrawSection(DrawAOSection, "AO");
        SafeDrawSection(DrawDitheringSection, L("ディザリング", "Dithering"));

        // ─── 外部ライティング ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("外部ライティング", "External Lighting"));
        SafeDrawSection(DrawLightVolumeSection, "Light Volume");
        SafeDrawSection(DrawLTCGISection, "LTCGI");

        // ─── 背景シェーダー専用 ───
        if (GetCurrentRenderingMode() == RenderingMode.Background)
        {
            NataneToonShaderGUIUtility.DrawCategoryDivider(L("背景シェーダー専用", "Background Shader Only"));
            SafeDrawSection(DrawBackgroundLightmapSection, L("背景ライトマップ", "Background Lightmap"));
            SafeDrawSection(DrawPBRSection, L("PBR マテリアル", "PBR Material"));
        }
    }

    /// <summary>
    /// Draw Effects tab content
    /// </summary>
    private void DrawEffectsTab()
    {
        DrawExpandCollapseButtons((state) => {
            SetFoldout("Specular", state); SetFoldout("HairSpecular", state); SetFoldout("RimLight", state); SetFoldout("SSS", state);
            SetFoldout("MatCap", state); SetFoldout("Glitter", state); SetFoldout("Drip", state); SetFoldout("Smear", state); SetFoldout("Fur", state); SetFoldout("Decal", state);
            SetFoldout("Hologram", state); SetFoldout("Outline", state); SetFoldout("Emission", state);
            SetFoldout("VirtualExpression", state); SetFoldout("AudioLink", state); SetFoldout("SurfaceCover", state);
        });
        // ─── 光源エフェクト ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("光源エフェクト", "Light Source Effects"));
        SafeDrawSection(DrawSpecularSection, L("スペキュラー", "Specular"));
        SafeDrawSection(DrawHairSpecularSection, L("ヘアスペキュラー", "Hair Specular"));
        SafeDrawSection(DrawRimLightSection, L("リムライト", "Rim Light"));
        SafeDrawSection(DrawSSSSection, "SSS");

        // ─── 表面エフェクト ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("表面エフェクト", "Surface Effects"));
        SafeDrawSection(DrawMatCapSection, "MatCap");
        SafeDrawSection(DrawGlitterSection, L("グリッター", "Glitter"));
        SafeDrawSection(DrawDripSection, L("雫エフェクト", "Drip Effect"));
        SafeDrawSection(DrawSmearSection, L("スミア", "Smear"));
        SafeDrawSection(DrawFurSection, L("ファー", "Fur"));
        SafeDrawSection(DrawDecalSection, L("デカール", "Decal"));
        SafeDrawSection(DrawSurfaceCoverSection, L("サーフェスカバー", "Surface Cover"));

        // ─── ビジュアルエフェクト ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("ビジュアルエフェクト", "Visual Effects"));
        SafeDrawSection(DrawHologramSection, L("ホログラム＆グリッチ", "Hologram & Glitch"));
        SafeDrawSection(DrawOutlineSection, L("アウトライン", "Outline"));
        SafeDrawSection(DrawEmissionSection, L("エミッション", "Emission"));
        SafeDrawSection(DrawVirtualExpressionSection, L("バーチャル表現", "Virtual Expression"));
        SafeDrawSection(DrawAudioLinkSection, "AudioLink");
    }

    /// <summary>
    /// Draw Environment tab content
    /// </summary>
    private void DrawEnvironmentTab()
    {
        DrawExpandCollapseButtons((state) => {
            SetFoldout("Reflection", state); SetFoldout("Iridescence", state);
            SetFoldout("EnvironmentalRim", state); SetFoldout("Refraction", state);
            SetFoldout("HeightFog", state);
        });
        SafeDrawSection(DrawReflectionSection, L("リフレクション", "Reflection"));
        SafeDrawSection(DrawIridescenceSection, L("イリデッセンス", "Iridescence"));
        SafeDrawSection(DrawEnvironmentalRimSection, L("環境リム", "Environmental Rim"));
        SafeDrawSection(DrawRefractionSection, L("屈折", "Refraction"));
        SafeDrawSection(DrawHeightFogSection, L("ハイトフォグ", "Height Fog"));
    }

    /// <summary>
    /// Draw Advanced tab content
    /// </summary>
    private void DrawAdvancedTab()
    {
        DrawExpandCollapseButtons((state) => {
            SetFoldout("NormalMap", state); SetFoldout("Parallax", state); SetFoldout("VertexAnimation", state); SetFoldout("VAT", state);
            SetFoldout("Backface", state); SetFoldout("Video", state); SetFoldout("HeightFade", state); SetFoldout("IntersectionFade", state);
            SetFoldout("DistanceFade", state); SetFoldout("Rendering", state); SetFoldout("Tessellation", state);
            SetFoldout("DetailMap", state); SetFoldout("Triplanar", state); SetFoldout("MirrorControl", state); SetFoldout("QuestLite", state);
        });
        // ─── マッピング ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("マッピング", "Mapping"));
        SafeDrawSection(DrawNormalMapSection, L("ノーマルマップ", "Normal Map"));
        SafeDrawSection(DrawParallaxSection, L("視差マッピング", "Parallax Mapping"));
        SafeDrawSection(DrawDetailMapSection, L("ディテールマップ", "Detail Map"));
        SafeDrawSection(DrawTriplanarSection, L("トライプレーナー", "Triplanar Mapping"));

        // ─── アニメーション＆特殊 ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("アニメーション＆特殊", "Animation & Special"));
        SafeDrawSection(DrawVertexAnimationSection, L("頂点アニメーション（風/呼吸/脈動）", "Vertex Animation (Wind/Breath/Pulse)"));
        SafeDrawSection(DrawVATSection, L("VAT（頂点アニメーション）", "VAT (Vertex Animation Texture)"));
        SafeDrawSection(DrawTessellationSection, L("テッセレーション（曲面スムージング）", "Tessellation (Surface Smoothing)"));
        SafeDrawSection(DrawBackfaceSection, L("裏面テクスチャ", "Backface Texture"));
        SafeDrawSection(DrawVideoSection, L("ビデオテクスチャ", "Video Texture"));
        SafeDrawSection(DrawHeightFadeSection, L("高さフェード", "Height Fade"));
        SafeDrawSection(DrawIntersectionFadeSection, L("オブジェクト交差フェード", "Intersection Fade"));
        SafeDrawSection(DrawDistanceFadeSection, L("距離フェード", "Distance Fade"));

        // ─── VRChat＆パフォーマンス ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("VRChat＆パフォーマンス", "VRChat & Performance"));
        SafeDrawSection(DrawMirrorControlSection, L("ミラー対応", "Mirror Control"));
        SafeDrawSection(DrawQuestLiteSection, L("Quest軽量パス", "Quest Lite"));

        // ─── レンダリング ───
        NataneToonShaderGUIUtility.DrawCategoryDivider(L("レンダリング", "Rendering"));
        SafeDrawSection(DrawRenderingSection, L("レンダリング", "Rendering"));
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
            EditorGUILayout.HelpBox(L($"「{query}」に一致するセクションが見つかりませんでした。", $"No sections found matching \"{query}\"."), MessageType.Info);
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
            case "ScreenTone": return DrawScreenToneSection;
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
            case "Smear": return DrawSmearSection;
            case "Fur": return DrawFurSection;
            case "BackgroundLightmap": return DrawBackgroundLightmapSection;
            case "PBR": return DrawPBRSection;
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
            case "GradientBaseColor": return DrawGradientBaseColorSection;
            case "HeightFade": return DrawHeightFadeSection;
            case "IntersectionFade": return DrawIntersectionFadeSection;
            case "DistanceFade": return DrawDistanceFadeSection;
            case "Rendering": return DrawRenderingSection;
            case "DetailMap": return DrawDetailMapSection;
            case "Triplanar": return DrawTriplanarSection;
            case "HeightFog": return DrawHeightFogSection;
            case "SurfaceCover": return DrawSurfaceCoverSection;
            case "MirrorControl": return DrawMirrorControlSection;
            case "QuestLite": return DrawQuestLiteSection;
            default: return null;
        }
    }

    /// <summary>
    /// 3.2 Quick Setup Section - Quick preset buttons for beginners
    /// </summary>
    private void DrawQuickSetupSection()
    {
        EditorGUILayout.Space(SECTION_SPACING);
        EditorGUILayout.LabelField(L("🎨 クイックセットアップ", "🎨 Quick Setup"), EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(L("シャープなアニメ調", "Sharp Anime Style"), GUILayout.Height(40)))
        {
            Undo.RecordObject(targetMaterial, "Apply Sharp Anime Style");
            ApplySharpAnimeStyle(targetMaterial);
        }

        if (GUILayout.Button(L("柔らかい塗り調", "Soft Painting Style"), GUILayout.Height(40)))
        {
            Undo.RecordObject(targetMaterial, "Apply Soft Painting Style");
            ApplySoftPaintingStyle(targetMaterial);
        }

        EditorGUILayout.EndHorizontal();

        // Surface finish buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(L("マット", "Matte"), GUILayout.Height(25)))
        {
            Undo.RecordObject(targetMaterial, "Apply Matte Surface");
            targetMaterial.SetFloat("_Glossiness", 0.0f);
            targetMaterial.SetFloat("_MatteEffect", 1.0f);
            EditorUtility.SetDirty(targetMaterial);
        }

        if (GUILayout.Button(L("グロッシー", "Glossy"), GUILayout.Height(25)))
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
            EditorGUILayout.LabelField(L("推奨: ", "Recommended: ") + recommendedRange, miniStyle);
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
        string buttonLabel = showHelp ? L("ヘルプを非表示", "Hide Help") : L("ヘルプを表示", "Show Help");
        GUIContent helpContent = new GUIContent($"{icon} {buttonLabel}", L("クリックでヘルプを表示/非表示", "Click to show/hide help"));

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
        EditorGUILayout.Space(SECTION_SPACING);

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
        foreach (var key in foldoutPrefsKeys.Keys)
        {
            SetFoldout(key, expand);
        }

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

            // Smear
            ("_Smear", "_SMEAR"),

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

        // StandardToon keyword sync (derived from _ShadingMode, not a simple toggle)
        if (targetMaterial.HasProperty("_ShadingMode"))
        {
            bool shouldBeStandardToon = targetMaterial.GetFloat("_ShadingMode") >= 1.5f;
            bool isStandardToon = targetMaterial.IsKeywordEnabled("_STANDARD_TOON");
            if (shouldBeStandardToon != isStandardToon)
            {
                if (shouldBeStandardToon)
                    targetMaterial.EnableKeyword("_STANDARD_TOON");
                else
                    targetMaterial.DisableKeyword("_STANDARD_TOON");
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
