using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// UV Texture Generator - Comprehensive mask texture creation tool.
    /// UVテクスチャ生成ツール - 包括的なマスクテクスチャ作成ツール
    /// Integrates noise, UV mask, gradient, mesh info, combined, templates, and channel packing.
    /// </summary>
    public class UVTextureGenerator : EditorWindow
    {
        // ===== Enums =====
        private enum GeneratorTab { Noise, UVMask, Gradient, MeshInfo, Combined, Templates, ChannelPack }
        private enum NoiseType { Perlin, Voronoi, Cellular, FBM, Value }
        private enum FillMode { Solid, BoundaryGradient }
        private enum GradientDirection { Inward, Outward }
        private enum CombineMode { Multiply, MaskOnly }
        private enum StudioTool { Brush, Eraser, Select, Fill, Move, RectSelect, LassoSelect, Eyedropper, Gradient }
        private enum CanvasColorMode { Mask, Color }
        private const float LeftPanelMinWidth = 240f;
        private const float RightPanelMinWidth = 300f;
        private const float CompactLeftPanelMinWidth = 200f;
        private const float CompactRightPanelMinWidth = 240f;
        private const float NarrowLeftPanelMinWidth = 180f;
        private const float NarrowRightPanelMinWidth = 220f;
        private const float DefaultCanvasMinWidth = 320f;
        private const float CompactCanvasMinWidth = 220f;
        private const float NarrowCanvasMinWidth = 180f;
        private const float SplitterWidth = 6f;
        private const float IconToolbarWidth = 38f;
        private const float StudioHorizontalPadding = 28f;
        private float leftPanelWidth = 320f;
        private float rightPanelWidth = 380f;
        private bool isDraggingLeftSplitter;
        private bool isDraggingRightSplitter;

        // ===== Tab State =====
        private GeneratorTab currentTab = GeneratorTab.Noise;
        private Vector2 scrollPosition;
        private Vector2 rightPanelScrollPosition;

        // ===== Canvas State =====
        private Texture2D previewTexture;
        private Texture2D canvasPreviewTexture;
        private Color[] previewPixels;
        private Color[] canvasDisplayBuffer;
        private Color32[] previewUploadBuffer;
        private Color32[] canvasUploadBuffer;
        private int previewTextureSize;
        private bool previewRefreshPending;
        private bool previewRefreshNeedsLiveLink;
        private double nextPreviewRefreshTime;
        private float canvasZoom = 1f;
        private Vector2 canvasPan;
        private bool isDraggingCanvas;
        private Vector2 lastCanvasMousePos;
        private bool showUVWireframe;

        // ===== Layer System =====
        private MaskLayerStack layerStack;
        private Vector2 layerScrollPosition;
        private bool layerPanelFoldout = true;

        // ===== Filter State =====
        private bool filterFoldout;
        private float filterBlurSigma = 1f;
        private float filterLevelInputMin;
        private float filterLevelInputMax = 1f;
        private float filterLevelGamma = 1f;
        private float filterLevelOutputMin;
        private float filterLevelOutputMax = 1f;
        private float filterEdgeStrength = 1f;
        private float filterSharpenAmount = 0.5f;
        private float filterSharpenSigma = 1f;
        private float filterThreshold = 0.5f;


        // ===== Auto-save =====
        private AutoSaveManager autoSave = new AutoSaveManager();

        // ===== Canvas features =====
        private CanvasBackgroundSettings canvasBackground = new CanvasBackgroundSettings();
        private ReferenceImageOverlay referenceOverlay = new ReferenceImageOverlay();
        private QuickMaskSystem quickMask = new QuickMaskSystem();
        private float canvasRotation;

        // ===== Right Panel Tab =====
        private int rightPanelTab; // 0=Layers, 1=Filters, 2=Settings

        // ===== Tool System =====
        private StudioTool activeTool = StudioTool.Brush;
        private CanvasColorMode canvasColorMode = CanvasColorMode.Mask;
        private ColorPickerPanel colorPicker = new ColorPickerPanel();
        private BrushSettings brushSettings = new BrushSettings();
        private BrushSettings eraserSettings = new BrushSettings
        {
            mode = BrushMode.Erase, size = 20f, hardness = 0.8f, opacity = 1f,
            pressureOpacityEnabled = true, pressureSizeEnabled = false
        };
        private MaskTextureBrush brush;
        private bool brushEnabled;
        private MaskTextureHistory brushHistory = new MaskTextureHistory();
        private MaskTextureShortcutProfile shortcutProfile = new MaskTextureShortcutProfile();
        private MaskTextureShortcutState shortcutState = new MaskTextureShortcutState();
        private MaskTextureBrushStabilizer brushStabilizer = new MaskTextureBrushStabilizer();
        private DirtyRect brushDirtyRect = DirtyRect.Empty;
        private string interactionStatus;
        private bool settingsPanelOpen;
        private Vector2 settingsPanelScroll;
        private string rebindingActionId;

        // ===== Filter Preview =====
        private bool filterPreviewActive;
        private Color[] filterPreviewPixels;
        private Color[] filterPreviewBackup;

        // ===== Opacity Number Key Input =====
        private float lastDigitTime;
        private int firstDigit = -1;
        private bool waitingForSecondDigit;

        // ===== Fill Tool =====
        private FillToolSettings fillSettings = new FillToolSettings();

        // ===== Selection Tool =====
        private MaskTextureSelection selection;
        private bool isSelectDragging;
        private Vector2 selectDragStart;
        private List<Vector2> lassoPoints = new List<Vector2>();

        // ===== Move Tool =====
        private bool isMoveDragging;
        private Vector2 moveDragStartMouse;
        private Vector2 moveDragStartOffset;

        // ===== Project File =====
        private string currentProjectPath;

        // ===== Material Assignment =====
        private Material targetMaterial;
        private int selectedPropertyIndex;
        private string selectedPropertyName;
        private const double InteractivePreviewRefreshInterval = 1.0 / 30.0;

        // ===== Live Link =====
        private MaskTextureLiveLink liveLink;

        // ===== Internal Accessors for Sub-Windows =====
        internal Texture2D CurrentPreviewTexture => previewTexture;
        internal Mesh CurrentMesh => ExtractMesh();
        internal Material CurrentTargetMaterial
        {
            get => targetMaterial;
            set => SetTargetMaterial(value);
        }
        internal int CurrentPropertyIndex
        {
            get => selectedPropertyIndex;
            set
            {
                if (value >= 0 && value < maskProperties.Length)
                    SetSelectedPropertyName(maskProperties[value]);
                else
                {
                    selectedPropertyIndex = value;
                    selectedPropertyName = null;
                }
            }
        }
        internal string CurrentPropertyName
        {
            get => selectedPropertyName;
            set => SetSelectedPropertyName(value);
        }
        internal MaskTextureLiveLink LiveLink => liveLink;
        internal static string[] MaskPropertyNames => maskProperties;

        // ===== Noise Parameters =====
        private NoiseType noiseType = NoiseType.Perlin;
        private int textureSize = 2048;
        private float scale = 5f;
        private int seed;
        private float contrast = 1f;
        private bool invert;
        private int octaves = 4;
        private float lacunarity = 2f;
        private float persistence = 0.5f;
        private Vector2 offset;

        // ===== Noise Alpha Parameters =====
        private bool noiseAlphaEnabled;
        private NoiseType noiseAlphaType = NoiseType.Perlin;
        private float noiseAlphaScale = 5f;
        private int noiseAlphaSeed = 42;
        private float noiseAlphaContrast = 1f;
        private bool noiseAlphaInvert;

        // ===== UV Mask Parameters =====
        private Object meshSource;
        private int uvChannel;
        private int maskTextureSize = 2048;
        private int uvMaterialSlot = -1;
        private FillMode fillMode = FillMode.Solid;
        private GradientDirection gradientDirection = GradientDirection.Inward;
        private int gradientWidth = 10;
        private bool maskInvert;
        private List<UVIsland> islands = new List<UVIsland>();
        private Vector2 islandScrollPosition;
        private bool islandsFoldout = true;
        private bool islandSelectMode;
        private int hoveredIslandIndex = -1;
        private Color[] islandColors;
        private Mesh cachedUvMesh;
        private int cachedUvChannel = -1;
        private int cachedUvMaterialSlot = int.MinValue;
        private readonly List<Vector2> cachedUvList = new List<Vector2>();
        private int[] cachedUvTriangles = new int[0];

        // ===== Gradient Parameters =====
        private GradientType gradientType = GradientType.Linear;
        private GradientParams gradientParams = new GradientParams();

        // ===== Mesh Info Parameters =====
        private MeshInfoType meshInfoType = MeshInfoType.Curvature;
        private float curvatureSensitivity = 1f;
        private Vector3 normalDirection = Vector3.up;
        private float normalThreshold = 0.3f;
        private int vertexColorChannel;

        // ===== Combined Parameters =====
        private CombineMode combineMode = CombineMode.Multiply;

        // ===== Template Parameters =====
        private MaskTemplate selectedTemplate = MaskTemplate.FaceSSS;
        private Vector2 templateScrollPosition;

        // ===== Cached Colors (avoid per-frame GC allocation) =====
        private static readonly Color HighlightBlue = new Color(0.45f, 0.67f, 0.96f);
        private static readonly Color LiveGreen = new Color(0.3f, 1f, 0.3f);
        private static readonly Color SymmetryCyan = new Color(0f, 1f, 1f, 0.8f);
        private static readonly Color UnsavedWarning = new Color(1f, 0.8f, 0.4f);
        private static readonly Color SavedGreen = new Color(0.5f, 0.8f, 0.5f);
        private static readonly Color ModeBadgeColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color ModeBadgeInactive = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color RebindOrange = new Color(1f, 0.5f, 0.2f);
        private static readonly Color DuplicateRed = new Color(1f, 0.3f, 0.3f);
        private static readonly Color ModifiedGold = new Color(1f, 0.85f, 0.5f);
        private static readonly Color CategoryBlue = new Color(0.55f, 0.78f, 1f);
        private static readonly Color TemplateSelectedBg = new Color(0.6f, 0.8f, 1f);
        private static readonly Color EraserCursorPink = new Color(1f, 0.6f, 0.6f);
        private static readonly Color MaskCursorCyan = new Color(0f, 1f, 1f);
        private static readonly Color LineGuideYellow = new Color(1f, 1f, 0f, 0.8f);
        private static readonly Color IslandHighlight = new Color(1f, 1f, 0f, 0.1f);
        private static readonly Color ColorModeBadgeBg = new Color(0.2f, 0.4f, 0.7f, 0.8f);
        private static readonly Color MaskModeBadgeBg = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        private static readonly Color CheckerLight = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color CheckerDark = new Color(0.25f, 0.25f, 0.25f);
        private static readonly Color SplitterActive = new Color(0.4f, 0.6f, 0.9f, 0.8f);
        private static readonly Color SplitterInactive = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        // ===== Cached GUIStyles =====
        private static GUIStyle s_toolButtonStyle;
        private static GUIStyle s_toolButtonActiveStyle;
        private static GUIStyle s_tabStyle;
        private static GUIStyle s_subTabStyle;
        private static GUIStyle s_subTabSelectedStyle;
        private static GUIStyle s_sectionHeaderStyle;
        private static GUIStyle s_categoryLabelStyle;
        private static GUIStyle s_shortcutNameStyle;
        private static GUIStyle s_shortcutModifiedStyle;
        private static GUIStyle s_modifiedCountStyle;
        private static GUIStyle s_allDefaultStyle;
        private static GUIStyle s_emptyCanvasStyle;
        private static GUIStyle s_modeBadgeStyle;

        private static void EnsureStyles()
        {
            if (s_toolButtonStyle != null) return;
            s_toolButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(0, 0, 0, 0)
            };
            s_toolButtonActiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 0)
            };
            s_tabStyle = new GUIStyle(EditorStyles.toolbarButton) { fontSize = 11, fixedHeight = 24 };
            s_subTabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                fixedHeight = 24
            };
            s_subTabSelectedStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                fixedHeight = 24
            };
            s_sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            s_categoryLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = CategoryBlue },
                padding = new RectOffset(0, 0, 4, 2)
            };
            s_shortcutNameStyle = new GUIStyle(EditorStyles.label) { fontSize = 11 };
            s_shortcutModifiedStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = ModifiedGold }
            };
            s_modifiedCountStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal = { textColor = ModifiedGold }
            };
            s_allDefaultStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal = { textColor = SavedGreen }
            };
            s_emptyCanvasStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            s_modeBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }

        // ===== Tab Labels =====
        private static string[] tabLabels => new[] {
            L("ノイズ", "Noise"), L("UVマスク", "UV Mask"), L("グラデ", "Gradient"), L("メッシュ", "Mesh"), L("複合", "Combined"), L("テンプレ", "Template"), L("CHパック", "CH Pack")
        };
        private static readonly int[] textureSizes = { 256, 512, 1024, 2048, 4096 };
        private static readonly string[] textureSizeLabels = { "256", "512", "1024", "2048", "4096" };

        // ===== Mask Properties =====
        /// <summary>Returns the BrushSettings for the currently active tool.</summary>
        private BrushSettings ActiveBrushSettings =>
            activeTool == StudioTool.Eraser ? eraserSettings : brushSettings;

        private void SwitchTool(StudioTool tool)
        {
            activeTool = tool;
            islandSelectMode = false;
            brushEnabled = false;

            switch (tool)
            {
                case StudioTool.Brush:
                case StudioTool.Eraser:
                    brushEnabled = true;
                    brush = new MaskTextureBrush(ActiveBrushSettings);
                    break;
                case StudioTool.Select:
                    islandSelectMode = true;
                    break;
                case StudioTool.Fill:
                case StudioTool.Move:
                case StudioTool.RectSelect:
                case StudioTool.LassoSelect:
                case StudioTool.Eyedropper:
                case StudioTool.Gradient:
                    // These tools don't use brush but are canvas-interactive
                    break;
            }

            // Toast notification for tool switch
            string[] toolNames = { L("ブラシ","Brush"), L("消しゴム","Eraser"), L("UV選択","Select"), L("塗りつぶし","Fill"), L("移動","Move"), L("矩形選択","Rect"), L("投げ縄","Lasso"), L("スポイト","Eyedropper"), L("グラデーション","Gradient") };
            int ti = (int)tool;
            if (ti >= 0 && ti < toolNames.Length)
                ToastNotification.Show(toolNames[ti]);

            Repaint();
        }

        private static readonly string[] maskProperties = {
            "_SpecularMask", "_RimMask", "_RimMask2", "_SSSMask",
            "_MatCapMask", "_MatCapMask2", "_MatCapMask3",
            "_GlitterMask", "_EmissionMask", "_DissolveTex", "_DissolveMask",
            "_AlphaMask", "_OutlineMask", "_OutlineWidthMap",
            "_IridescenceMask", "_EnvRimMask", "_ReflectionMask", "_RefractionMask",
            "_ShadowReceiveMask", "_2ndTexMask", "_3rdTexMask", "_4thTexMask", "_5thTexMask"
        };

        private static readonly string[] LivePreferredPropertyOrder = {
            "_MainTex", "_BaseMap", "_BaseColorMap",
            "_SpecularMask", "_RimMask", "_EmissionMask",
            "_DissolveTex", "_DissolveMask", "_AlphaMask"
        };

        internal static void GetMaterialTexturePropertyOptions(Material material, List<string> propertyNames, List<string> propertyLabels = null)
        {
            propertyNames?.Clear();
            propertyLabels?.Clear();
            if (material == null || material.shader == null)
                return;

            Shader shader = material.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                if (string.IsNullOrEmpty(propertyName) || !material.HasProperty(propertyName))
                    continue;

                propertyNames?.Add(propertyName);
                if (propertyLabels != null)
                {
                    string description = ShaderUtil.GetPropertyDescription(shader, i);
                    propertyLabels.Add(string.IsNullOrEmpty(description) || description == propertyName
                        ? propertyName
                        : $"{description} ({propertyName})");
                }
            }
        }

        private string GetSelectedPropertyName()
        {
            if (!string.IsNullOrEmpty(selectedPropertyName))
                return selectedPropertyName;

            if (selectedPropertyIndex >= 0 && selectedPropertyIndex < maskProperties.Length)
                return maskProperties[selectedPropertyIndex];

            return null;
        }

        private void SetTargetMaterial(Material material)
        {
            bool shouldRebind = liveLink != null && liveLink.IsEnabled;
            if (shouldRebind)
                liveLink.Disable();

            targetMaterial = material;
            EnsureValidSelectedProperty();

            if (shouldRebind)
                EnableLiveLink();
        }

        private void SetSelectedPropertyName(string propertyName)
        {
            string normalizedName = string.IsNullOrEmpty(propertyName) ? null : propertyName;
            bool shouldRebind = liveLink != null && liveLink.IsEnabled;
            if (shouldRebind)
                liveLink.Disable();

            selectedPropertyName = normalizedName;
            selectedPropertyIndex = System.Array.IndexOf(maskProperties, normalizedName);

            if (shouldRebind)
                EnableLiveLink();
        }

        private void EnsureValidSelectedProperty()
        {
            if (targetMaterial == null)
            {
                selectedPropertyIndex = System.Array.IndexOf(maskProperties, selectedPropertyName);
                return;
            }

            var propertyNames = new List<string>();
            GetMaterialTexturePropertyOptions(targetMaterial, propertyNames);
            if (propertyNames.Count == 0)
            {
                selectedPropertyName = null;
                selectedPropertyIndex = -1;
                return;
            }

            if (string.IsNullOrEmpty(selectedPropertyName) || !propertyNames.Contains(selectedPropertyName))
                selectedPropertyName = FindBestLiveProperty(targetMaterial, selectedPropertyName, false) ?? propertyNames[0];

            selectedPropertyIndex = System.Array.IndexOf(maskProperties, selectedPropertyName);
        }

        private static Material FindFirstMaterial(Renderer renderer)
        {
            if (renderer == null)
                return null;

            Material[] materials = renderer.sharedMaterials;
            if (materials != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                        return materials[i];
                }
            }

            return renderer.sharedMaterial;
        }

        private static Material FindFirstMaterial(GameObject gameObject)
        {
            if (gameObject == null)
                return null;

            Material directMaterial = FindFirstMaterial(gameObject.GetComponent<Renderer>());
            if (directMaterial != null)
                return directMaterial;

            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material childMaterial = FindFirstMaterial(renderers[i]);
                if (childMaterial != null)
                    return childMaterial;
            }

            return null;
        }

        private static Material FindFirstMaterial(Component component)
        {
            if (component == null)
                return null;

            if (component is Renderer renderer)
                return FindFirstMaterial(renderer);

            Material directMaterial = FindFirstMaterial(component.GetComponent<Renderer>());
            if (directMaterial != null)
                return directMaterial;

            return FindFirstMaterial(component.gameObject);
        }

        private static bool HasEditableTexture(Material material, string propertyName)
        {
            return material != null
                && !string.IsNullOrEmpty(propertyName)
                && material.HasProperty(propertyName)
                && material.GetTexture(propertyName) is Texture2D;
        }

        private static string FindBestLiveProperty(Material material, string currentPropertyName, bool requireEditableTexture)
        {
            if (material == null)
                return null;

            var propertyNames = new List<string>();
            GetMaterialTexturePropertyOptions(material, propertyNames);
            if (propertyNames.Count == 0)
                return null;

            bool IsUsable(string propertyName)
            {
                if (string.IsNullOrEmpty(propertyName) || !propertyNames.Contains(propertyName))
                    return false;

                return !requireEditableTexture || HasEditableTexture(material, propertyName);
            }

            if (IsUsable(currentPropertyName))
                return currentPropertyName;

            for (int i = 0; i < LivePreferredPropertyOrder.Length; i++)
            {
                string preferredProperty = LivePreferredPropertyOrder[i];
                if (IsUsable(preferredProperty))
                    return preferredProperty;
            }

            for (int i = 0; i < propertyNames.Count; i++)
            {
                string propertyName = propertyNames[i];
                if (IsUsable(propertyName))
                    return propertyName;
            }

            if (!string.IsNullOrEmpty(currentPropertyName) && propertyNames.Contains(currentPropertyName))
                return currentPropertyName;

            for (int i = 0; i < LivePreferredPropertyOrder.Length; i++)
            {
                string preferredProperty = LivePreferredPropertyOrder[i];
                if (propertyNames.Contains(preferredProperty))
                    return preferredProperty;
            }

            return propertyNames[0];
        }

        private Material GetMeshSourceMaterial()
        {
            if (meshSource == null)
                return null;

            if (meshSource is Material material)
                return material;

            if (meshSource is GameObject gameObject)
                return FindFirstMaterial(gameObject);

            if (meshSource is Component component)
                return FindFirstMaterial(component);

            return FindFirstMaterial(ExtractRenderer());
        }

        private Material GetSuggestedLiveMaterial()
        {
            if (Selection.activeObject is Material selectedMaterial)
                return selectedMaterial;

            if (Selection.activeObject is Component selectedComponent)
            {
                Material componentMaterial = FindFirstMaterial(selectedComponent);
                if (componentMaterial != null)
                    return componentMaterial;
            }

            Material selectionMaterial = FindFirstMaterial(Selection.activeGameObject);
            if (selectionMaterial != null)
                return selectionMaterial;

            return GetMeshSourceMaterial();
        }

        private bool IsCanvasEffectivelyEmpty()
        {
            if (previewTexture == null)
                return true;

            if (layerStack == null || layerStack.Layers == null || layerStack.Layers.Count == 0)
                return true;

            if (layerStack.Layers.Count > 1)
                return false;

            MaskTextureLayer layer = layerStack.ActiveLayer ?? layerStack.Layers[0];
            if (layer == null || layer.pixels == null || layer.pixels.Length == 0)
                return true;

            if (layer.sourceType != MaskTextureLayer.SourceType.Empty)
                return false;

            int step = Mathf.Max(1, layer.pixels.Length / 64);
            for (int i = 0; i < layer.pixels.Length; i += step)
            {
                Color pixel = layer.pixels[i];
                if (pixel.maxColorComponent > 0.001f || pixel.a > 0.001f)
                    return false;
            }

            return true;
        }

        private bool TryUseSelectionForLive(out string message)
        {
            Material suggestedMaterial = GetSuggestedLiveMaterial();
            if (suggestedMaterial == null)
            {
                message = L(
                    "Hierarchy か Project でマテリアル、または Renderer を持つオブジェクトを選択してください。",
                    "Select a material or a GameObject with a Renderer in the Hierarchy or Project.");
                return false;
            }

            CurrentTargetMaterial = suggestedMaterial;
            string propertyName = GetSelectedPropertyName();
            message = string.IsNullOrEmpty(propertyName)
                ? L($"選択中のマテリアルを設定しました: {suggestedMaterial.name}",
                    $"Assigned selected material: {suggestedMaterial.name}")
                : L($"選択中を設定: {suggestedMaterial.name} / {propertyName}",
                    $"Assigned selection: {suggestedMaterial.name} / {propertyName}");
            return true;
        }

        private bool TryPrepareLiveLinkTarget(bool allowAutoImport, bool preferSelection, out string message)
        {
            Material selectedMaterial = preferSelection ? GetSuggestedLiveMaterial() : null;
            bool selectionOverridesTarget = selectedMaterial != null && selectedMaterial != targetMaterial;
            if (selectionOverridesTarget)
            {
                CurrentTargetMaterial = selectedMaterial;
            }
            else if (targetMaterial == null)
            {
                if (!TryUseSelectionForLive(out message))
                    return false;
            }
            else
            {
                EnsureValidSelectedProperty();
            }

            string propertyName = GetSelectedPropertyName();
            if (allowAutoImport)
            {
                string importProperty = FindBestLiveProperty(targetMaterial, propertyName, true);
                if (!string.IsNullOrEmpty(importProperty) && importProperty != propertyName)
                {
                    CurrentPropertyName = importProperty;
                    propertyName = importProperty;
                }
            }

            if (targetMaterial == null || string.IsNullOrEmpty(propertyName) || !targetMaterial.HasProperty(propertyName))
            {
                message = L(
                    "対象マテリアルに使えるテクスチャプロパティが見つかりません。Settings の Material Assignment を確認してください。",
                    "No editable texture property is available on the target material. Check Material Assignment in Settings.");
                return false;
            }

            if (allowAutoImport && (selectionOverridesTarget || IsCanvasEffectivelyEmpty()))
            {
                Texture currentTexture = targetMaterial.GetTexture(propertyName);
                if (currentTexture is Texture2D sourceTexture)
                {
                    try
                    {
                        LoadLiveTextureForEditing(sourceTexture, propertyName, selectionOverridesTarget || IsCanvasEffectivelyEmpty());
                        message = L(
                            $"LIVE 用に {targetMaterial.name} / {propertyName} をベースレイヤーとして読み込みました。",
                            $"Loaded {targetMaterial.name} / {propertyName} as the LIVE base layer.");
                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Natane Toon] LIVE texture import failed: {targetMaterial.name} / {propertyName}\n{ex}");
                        message = L(
                            $"既存テクスチャの読込に失敗しました: {targetMaterial.name} / {propertyName}",
                            $"Failed to import the current texture: {targetMaterial.name} / {propertyName}");
                        return false;
                    }
                }
            }

            message = L(
                $"LIVE 接続先: {targetMaterial.name} / {propertyName}",
                $"LIVE target: {targetMaterial.name} / {propertyName}");
            return true;
        }

        private string GetLiveSetupSummary()
        {
            if (liveLink != null && liveLink.IsEnabled && targetMaterial != null)
            {
                string boundProperty = string.IsNullOrEmpty(liveLink.BoundPropertyName)
                    ? GetSelectedPropertyName()
                    : liveLink.BoundPropertyName;
                return L(
                    $"LIVE 接続中: {targetMaterial.name} / {boundProperty}",
                    $"LIVE connected: {targetMaterial.name} / {boundProperty}");
            }

            Material suggestedMaterial = GetSuggestedLiveMaterial();
            if (targetMaterial == null)
            {
                return suggestedMaterial != null
                    ? L(
                        $"いちばん簡単: 『選択中を使う』か LIVE を押すと {suggestedMaterial.name} を自動で使います。",
                        $"Easiest: click 'Use Selection' or LIVE to use {suggestedMaterial.name} automatically.")
                    : L(
                        "いちばん簡単: Hierarchy か Project でマテリアルかオブジェクトを選んでから LIVE を押します。",
                        "Easiest: select a material or GameObject, then press LIVE.");
            }

            string propertyName = GetSelectedPropertyName();
            if (string.IsNullOrEmpty(propertyName) || !targetMaterial.HasProperty(propertyName))
            {
                return L(
                    "このマテリアルで編集したいテクスチャプロパティを選んでください。",
                    "Choose the texture property you want to edit on this material.");
            }

            string autoImportProperty = FindBestLiveProperty(targetMaterial, propertyName, true);

            if (suggestedMaterial != null && suggestedMaterial != targetMaterial)
            {
                string suggestedProperty = FindBestLiveProperty(suggestedMaterial, propertyName, true)
                    ?? FindBestLiveProperty(suggestedMaterial, propertyName, false);
                return L(
                    string.IsNullOrEmpty(suggestedProperty)
                        ? $"Quick LIVE は選択中の {suggestedMaterial.name} へ切り替えて開始します。"
                        : $"Quick LIVE は選択中の {suggestedMaterial.name} / {suggestedProperty} へ切り替えて開始します。",
                    string.IsNullOrEmpty(suggestedProperty)
                        ? $"Quick LIVE will switch to the selected material: {suggestedMaterial.name}."
                        : $"Quick LIVE will switch to the selected material: {suggestedMaterial.name} / {suggestedProperty}.");
            }

            Texture currentTexture = targetMaterial.GetTexture(propertyName);
            if (!string.IsNullOrEmpty(autoImportProperty) && autoImportProperty != propertyName && IsCanvasEffectivelyEmpty())
            {
                return L(
                    $"準備OK: {targetMaterial.name}。LIVE を押すと {autoImportProperty} の現在テクスチャをベースレイヤーへ自動で読み込みます。",
                    $"Ready: {targetMaterial.name}. Press LIVE to auto-load the current texture from {autoImportProperty} as a base layer.");
            }

            if (currentTexture is Texture2D && IsCanvasEffectivelyEmpty())
            {
                return L(
                    $"準備OK: {targetMaterial.name} / {propertyName}。LIVE を押すと現在のテクスチャをベースレイヤーへ自動で読み込みます。",
                    $"Ready: {targetMaterial.name} / {propertyName}. Press LIVE to auto-load the current texture as a base layer.");
            }

            return L(
                $"準備OK: {targetMaterial.name} / {propertyName}。LIVE を押すとリアルタイム反映します。",
                $"Ready: {targetMaterial.name} / {propertyName}. Press LIVE for real-time preview.");
        }

        private void ToggleLiveLinkFromUI(bool allowAutoImport, bool preferSelection)
        {
            if (liveLink != null && liveLink.IsEnabled)
            {
                DisableLiveLink();
                ToastNotification.Show(L("LIVE を解除しました", "LIVE disabled"));
                return;
            }

            if (!TryPrepareLiveLinkTarget(allowAutoImport, preferSelection, out string message))
            {
                rightPanelTab = 2;
                ToastNotification.ShowWarning(message);
                return;
            }

            if (liveLink == null || !liveLink.IsEnabled)
                EnableLiveLink();

            if (liveLink != null && liveLink.IsEnabled)
            {
                ToastNotification.ShowSuccess(message);
                return;
            }

            rightPanelTab = 2;
            ToastNotification.ShowWarning(L(
                "LIVE を開始できませんでした。Settings の Material Assignment を確認してください。",
                "Could not start LIVE. Check Material Assignment in Settings."));
        }

        [MenuItem("Tools/Natane/テクスチャスタジオ Texture Studio", false, 141)]
        public static void ShowWindow()
        {
            // [TEMPORARY] Password gate - remove at official release
            // [一時的] パスワードゲート - 正式リリース時に削除
            if (!TextureStudioPasswordGate.Verify()) return;

            var window = GetWindow<UVTextureGenerator>(L("テクスチャスタジオ", "Texture Studio"));
            window.minSize = new Vector2(920, 640);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            if (layerStack == null)
                layerStack = new MaskLayerStack(textureSize, textureSize);
            if (layerStack.Layers.Count == 0)
                layerStack.AddLayer("Base Layer");
            if (brushHistory == null)
                brushHistory = new MaskTextureHistory();
            if (shortcutProfile == null)
                shortcutProfile = new MaskTextureShortcutProfile();
            if (shortcutState == null)
                shortcutState = new MaskTextureShortcutState();
            if (brushStabilizer == null)
                brushStabilizer = new MaskTextureBrushStabilizer();
            if (liveLink == null)
                liveLink = new MaskTextureLiveLink();
            brush = new MaskTextureBrush(ActiveBrushSettings);
            string savedProfile = EditorPrefs.GetString("NataneToon_ShortcutProfile", "");
            if (!string.IsNullOrEmpty(savedProfile))
                shortcutProfile = MaskTextureShortcutProfile.FromJson(savedProfile);
            if (shortcutProfile.bindings == null || shortcutProfile.bindings.Count == 0)
            {
                var def = MaskTextureShortcutProfile.CreateDefault();
                shortcutProfile.bindings = def.bindings;
            }
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private void OnDisable()
        {
            shortcutState?.ResetTransient();
            brushStabilizer?.Reset();
            liveLink?.Disable();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }

        private void OnDestroy()
        {
            if (previewTexture != null)
                DestroyImmediate(previewTexture);
            if (canvasPreviewTexture != null && canvasPreviewTexture != previewTexture)
                DestroyImmediate(canvasPreviewTexture);
            canvasPreviewTexture = null;
            previewTexture = null;
            liveLink?.Dispose();
            liveLink = null;
        }

        private void OnLostFocus()
        {
            shortcutState?.ResetTransient();
            brushStabilizer?.Reset();
            isDraggingCanvas = false;
        }

        private float GetResponsiveLeftPanelMinWidth()
        {
            if (position.width < 920f)
                return NarrowLeftPanelMinWidth;

            return position.width < 1120f ? CompactLeftPanelMinWidth : LeftPanelMinWidth;
        }

        private float GetResponsiveRightPanelMinWidth()
        {
            if (position.width < 920f)
                return NarrowRightPanelMinWidth;

            return position.width < 1120f ? CompactRightPanelMinWidth : RightPanelMinWidth;
        }

        private float GetResponsiveCanvasMinWidth()
        {
            if (position.width < 920f)
                return NarrowCanvasMinWidth;

            return position.width < 1120f ? CompactCanvasMinWidth : DefaultCanvasMinWidth;
        }

        private float GetStudioBodyAvailableWidth()
        {
            return Mathf.Max(0f, position.width - IconToolbarWidth - SplitterWidth * 2f - StudioHorizontalPadding);
        }

        private float GetCanvasViewportWidth()
        {
            return Mathf.Max(0f, GetStudioBodyAvailableWidth() - leftPanelWidth - rightPanelWidth);
        }

        private float GetResponsiveColorPickerMaxHeight()
        {
            float availableHeight = Mathf.Max(0f, position.height - 96f);
            return Mathf.Clamp(availableHeight * 0.36f, 180f, 440f);
        }

        private void NormalizeStudioLayout()
        {
            float leftMin = GetResponsiveLeftPanelMinWidth();
            float rightMin = GetResponsiveRightPanelMinWidth();
            float canvasMin = GetResponsiveCanvasMinWidth();
            float availableWidth = GetStudioBodyAvailableWidth();

            leftPanelWidth = Mathf.Max(leftPanelWidth, leftMin);
            rightPanelWidth = Mathf.Max(rightPanelWidth, rightMin);

            float overflow = leftPanelWidth + rightPanelWidth + canvasMin - availableWidth;
            if (overflow > 0f)
            {
                float rightFlex = Mathf.Max(0f, rightPanelWidth - rightMin);
                float reduceRight = Mathf.Min(overflow, rightFlex);
                rightPanelWidth -= reduceRight;
                overflow -= reduceRight;

                float leftFlex = Mathf.Max(0f, leftPanelWidth - leftMin);
                float reduceLeft = Mathf.Min(overflow, leftFlex);
                leftPanelWidth -= reduceLeft;
                overflow -= reduceLeft;
            }

            float leftMax = Mathf.Max(leftMin, availableWidth - rightPanelWidth - canvasMin);
            leftPanelWidth = Mathf.Min(leftPanelWidth, leftMax);

            float rightMax = Mathf.Max(rightMin, availableWidth - leftPanelWidth - canvasMin);
            rightPanelWidth = Mathf.Min(rightPanelWidth, rightMax);
        }

        // ================================================================
        // Main GUI
        // ================================================================

        private void OnGUI()
        {
            // Ensure fields are initialized (Unity may reset them on recompile/domain reload)
            if (autoSave == null) autoSave = new AutoSaveManager();
            if (canvasBackground == null) canvasBackground = new CanvasBackgroundSettings();
            if (referenceOverlay == null) referenceOverlay = new ReferenceImageOverlay();
            if (quickMask == null) quickMask = new QuickMaskSystem();
            if (colorPicker == null) colorPicker = new ColorPickerPanel();
            if (brushSettings == null) brushSettings = new BrushSettings();
            if (eraserSettings == null) eraserSettings = new BrushSettings { mode = BrushMode.Erase, size = 20f, hardness = 0.8f, opacity = 1f };
            if (fillSettings == null) fillSettings = new FillToolSettings();
            if (shortcutProfile == null) shortcutProfile = new MaskTextureShortcutProfile();
            if (shortcutState == null) shortcutState = new MaskTextureShortcutState();
            if (brushStabilizer == null) brushStabilizer = new MaskTextureBrushStabilizer();
            if (brushHistory == null) brushHistory = new MaskTextureHistory();

            FlushPendingPreviewRefresh();
            HandleGlobalInput(Event.current);
            NormalizeStudioLayout();

            // Auto-save check
            autoSave.CheckAutoSave(path => SaveProjectToPath(path));

            // Step 2: Command Bar (full-width top bar)
            DrawCommandBar();

            // Main content area: Icon Toolbar + Left Panel + Canvas + Right Panel
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            // Step 3: Icon Toolbar (32px vertical strip)
            DrawIconToolbar();

            // Left Panel (sub-tools + tool properties + brush settings)
            DrawLeftStudioPanel();
            DrawSplitter(ref isDraggingLeftSplitter, ref leftPanelWidth,
                GetResponsiveLeftPanelMinWidth(),
                Mathf.Max(GetResponsiveLeftPanelMinWidth(),
                    GetStudioBodyAvailableWidth() - rightPanelWidth - GetResponsiveCanvasMinWidth()),
                false);

            // Center Canvas
            DrawCenterStudioPanel();

            // Right Panel (layers + filters + export)
            DrawSplitter(ref isDraggingRightSplitter, ref rightPanelWidth,
                GetResponsiveRightPanelMinWidth(),
                Mathf.Max(GetResponsiveRightPanelMinWidth(),
                    GetStudioBodyAvailableWidth() - leftPanelWidth - GetResponsiveCanvasMinWidth()),
                true);
            DrawRightStudioPanel();

            EditorGUILayout.EndHorizontal();
            return;

            /*
            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case GeneratorTab.Noise:
                    DrawNoiseTab();
                    break;
                case GeneratorTab.UVMask:
                    DrawUVMaskTab();
                    break;
                case GeneratorTab.Gradient:
                    DrawGradientTab();
                    break;
                case GeneratorTab.MeshInfo:
                    DrawMeshInfoTab();
                    break;
                case GeneratorTab.Combined:
                    DrawCombinedTab();
                    break;
                case GeneratorTab.Templates:
                    DrawTemplatesTab();
                    break;
                case GeneratorTab.ChannelPack:
                    DrawChannelPackTab();
                    break;
            }

            EditorGUILayout.Space(10);
            DrawCanvas();

            // Layer panel
            EditorGUILayout.Space(5);
            layerPanelFoldout = NataneToonShaderGUIUtility.DrawFoldoutHeader(
                L("レイヤー", "Layers"), layerPanelFoldout);
            if (layerPanelFoldout)
            {
                bool layerChanged = MaskLayerPanelUI.DrawLayerPanel(layerStack, ref layerScrollPosition);
                if (layerChanged)
                    RefreshPreviewFromLayers();
            }

            // Filter panel
            EditorGUILayout.Space(5);
            DrawFilterPanel();

            // Brush settings
            if (brushEnabled)
            {
                EditorGUILayout.Space(5);
                BrushSettingsUI.DrawBrushSettingsUI(brushSettings);
            }

            // 3D Preview
            EditorGUILayout.Space(5);
            Draw3DPreviewSection();

            // Export
            EditorGUILayout.Space(5);
            MaskTextureExporter.DrawExportUI(previewTexture);

            // Material assignment
            EditorGUILayout.Space(5);
            DrawMaterialAssignment();

            EditorGUILayout.EndScrollView();
            */
        }

        /// <summary>
        /// Step 2: Full-width command bar at the top of the window (2 rows).
        /// ウィンドウ上部の全幅コマンドバー（2段構成）
        /// </summary>
        private void DrawCommandBar()
        {
            // === Row 1: File / Edit / Mode ===
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Undo / Redo (icon-style compact)
            GUI.enabled = brushHistory != null && brushHistory.CanUndo;
            string undoTip = brushHistory != null && brushHistory.CanUndo
                ? L("元に戻す", "Undo") + ": " + brushHistory.NextUndoLabel : L("元に戻す", "Undo");
            if (GUILayout.Button(new GUIContent("\u21a9", undoTip), EditorStyles.toolbarButton, GUILayout.Width(26)))
            {
                UndoBrushStroke();
                ToastNotification.Show(L("元に戻す", "Undo") + ": " + brushHistory.NextUndoLabel);
            }
            GUI.enabled = brushHistory != null && brushHistory.CanRedo;
            string redoTip = brushHistory != null && brushHistory.CanRedo
                ? L("やり直し", "Redo") + ": " + brushHistory.NextRedoLabel : L("やり直し", "Redo");
            if (GUILayout.Button(new GUIContent("\u21aa", redoTip), EditorStyles.toolbarButton, GUILayout.Width(26)))
            {
                RedoBrushStroke();
                ToastNotification.Show(L("やり直し", "Redo") + ": " + brushHistory.NextRedoLabel);
            }
            GUI.enabled = true;

            GUILayout.Space(2);
            DrawToolbarSeparator();
            GUILayout.Space(2);

            // Save / Open
            if (GUILayout.Button(new GUIContent("\U0001f4be", L("保存 (Ctrl+S)", "Save (Ctrl+S)")), EditorStyles.toolbarButton, GUILayout.Width(26)))
                SaveProject();
            if (GUILayout.Button(new GUIContent("\U0001f4c2", L("開く (Ctrl+O)", "Open (Ctrl+O)")), EditorStyles.toolbarButton, GUILayout.Width(26)))
                OpenProject();

            GUILayout.Space(2);
            DrawToolbarSeparator();
            GUILayout.Space(2);

            // Canvas color mode toggle (prominent)
            Color prevModeBg = GUI.backgroundColor;
            GUI.backgroundColor = canvasColorMode == CanvasColorMode.Color
                ? ModeBadgeColor : ModeBadgeInactive;
            EditorGUI.BeginChangeCheck();
            canvasColorMode = (CanvasColorMode)EditorGUILayout.EnumPopup(
                canvasColorMode, EditorStyles.toolbarPopup, GUILayout.Width(70));
            if (EditorGUI.EndChangeCheck())
            {
                bool isColor = canvasColorMode == CanvasColorMode.Color;
                brushSettings.colorMode = isColor;
                eraserSettings.colorMode = false;
                fillSettings.colorMode = isColor;
                if (isColor)
                {
                    brushSettings.paintColor = colorPicker.ForegroundColor;
                    brushSettings.backgroundColor = colorPicker.BackgroundColor;
                    fillSettings.fillColor = colorPicker.ForegroundColor;
                }
                else
                {
                    brushSettings.strength = colorPicker.ForegroundColor.grayscale;
                    brushSettings.paintAlpha = colorPicker.ForegroundColor.a;
                    fillSettings.fillValue = brushSettings.strength;
                    fillSettings.fillAlpha = brushSettings.paintAlpha;
                }
                RefreshCanvasPreviewTexture();
                ToastNotification.Show(isColor ? L("カラーモード", "Color Mode") : L("マスクモード", "Mask Mode"));
            }
            GUI.backgroundColor = prevModeBg;

            GUILayout.Space(2);
            DrawToolbarSeparator();
            GUILayout.Space(2);

            // Channel view - dropdown instead of 5 buttons
            ChannelViewMode prevChannel = ChannelView.CurrentMode;
            ChannelView.CurrentMode = (ChannelViewMode)EditorGUILayout.EnumPopup(
                ChannelView.CurrentMode, EditorStyles.toolbarPopup, GUILayout.Width(60));
            if (ChannelView.CurrentMode != prevChannel)
            {
                RefreshCanvasPreviewTexture();
                ToastNotification.Show($"Channel: {ChannelView.CurrentMode}");
            }

            GUILayout.FlexibleSpace();

            // Sub-window buttons
            if (GUILayout.Button("3D", EditorStyles.toolbarButton, GUILayout.Width(28)))
                MaskTexture3DPreviewWindow.Open();
            if (GUILayout.Button(new GUIContent("\U0001f4e4", L("書出", "Export")), EditorStyles.toolbarButton, GUILayout.Width(26)))
                MaskTextureOutputWindow.Open();

            string selectedProperty = GetSelectedPropertyName();

            // Quick write to material (1-click)
            if (targetMaterial != null && !string.IsNullOrEmpty(selectedProperty) && previewTexture != null)
            {
                if (GUILayout.Button(new GUIContent("\U0001f4dd", L("マテリアルに書き込み", "Write to Material")),
                    EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    WriteTextureToMaterial();
                    ToastNotification.ShowSuccess(L("マテリアルに書き込み完了", "Written to Material"));
                }
            }

            GUILayout.Space(2);

            // Live Link toggle
            var prevEnabled = GUI.enabled;
            GUI.enabled = true;
            var prevBg = GUI.backgroundColor;
            if (liveLink != null && liveLink.IsEnabled)
                GUI.backgroundColor = LiveGreen;
            string liveTooltip = liveLink != null && liveLink.IsEnabled
                ? L("LIVE を解除して元のテクスチャ参照に戻します。", "Disable LIVE and restore the original texture binding.")
                : L("かんたん LIVE: 選択中マテリアルを自動で使い、空キャンバスなら現在のテクスチャも読み込んで接続します。",
                    "Quick LIVE: auto-use the selected material and load the current texture when the canvas is empty.");
            if (GUILayout.Button(new GUIContent("LIVE", liveTooltip), EditorStyles.toolbarButton, GUILayout.Width(44)))
            {
                ToggleLiveLinkFromUI(true, true);
            }
            GUI.backgroundColor = prevBg;
            GUI.enabled = prevEnabled;

            GUILayout.Space(2);

            // Tiling preview toggle
            TilingPreview.DrawToolbarToggle();

            // Settings gear -> switch to Settings tab in right panel
            bool isSettingsTab = (rightPanelTab == 2);
            var prevBgSettings = GUI.backgroundColor;
            if (isSettingsTab) GUI.backgroundColor = HighlightBlue;
            if (GUILayout.Button(new GUIContent("\u2699", L("設定", "Settings")), EditorStyles.toolbarButton, GUILayout.Width(26)))
                rightPanelTab = (rightPanelTab == 2) ? 0 : 2;
            GUI.backgroundColor = prevBgSettings;

            // Help
            if (GUILayout.Button(new GUIContent("?", L("ショートカット一覧 (F1)", "Shortcuts (F1)")), EditorStyles.toolbarButton, GUILayout.Width(22)))
                StudioOnboarding.Open();

            EditorGUILayout.EndHorizontal();

            // === Row 2: Tool properties ===
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Brush Size slider (compact)
            var settings = ActiveBrushSettings;
            GUILayout.Label(L("サイズ", "Size"), EditorStyles.miniLabel, GUILayout.Width(46));
            settings.size = EditorGUILayout.Slider(settings.size, 1f, 100f, GUILayout.Width(110));

            GUILayout.Space(4);

            // Opacity slider (compact)
            GUILayout.Label(L("不透明度", "Opac"), EditorStyles.miniLabel, GUILayout.Width(54));
            settings.opacity = EditorGUILayout.Slider(settings.opacity, 0f, 1f, GUILayout.Width(90));

            GUILayout.Space(4);
            DrawToolbarSeparator();
            GUILayout.Space(4);

            // Zoom controls
            canvasZoom = ZoomController.DrawToolbarZoom(canvasZoom);

            GUILayout.Space(4);
            DrawToolbarSeparator();
            GUILayout.Space(4);

            // UV wireframe toggle
            showUVWireframe = GUILayout.Toggle(showUVWireframe,
                new GUIContent("UV", L("UVワイヤーフレーム表示", "Show UV wireframe")),
                EditorStyles.toolbarButton, GUILayout.Width(28));
            if (showUVWireframe)
                DrawUvMaterialSlotToolbar();

            // Symmetry indicator
            if (SymmetryDrawing.Mode != SymmetryMode.None)
            {
                Color prevSymBg = GUI.backgroundColor;
                GUI.backgroundColor = SymmetryCyan;
                GUILayout.Label($"\u27d0 {SymmetryDrawing.Mode}", EditorStyles.toolbarButton, GUILayout.Width(80));
                GUI.backgroundColor = prevSymBg;
            }

            GUILayout.FlexibleSpace();

            // Auto-save status
            if (autoSave != null)
            {
                string saveStatus = autoSave.GetStatusText();
                if (!string.IsNullOrEmpty(saveStatus))
                {
                    Color statusColor = autoSave.HasUnsavedChanges ? UnsavedWarning : SavedGreen;
                    var saveStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, normal = { textColor = statusColor } };
                    GUILayout.Label(saveStatus, saveStyle, GUILayout.Width(120));
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawToolbarSeparator()
        {
            GUILayout.Box("", GUILayout.Width(1), GUILayout.Height(16));
        }

        /// <summary>
        /// Step 3: 32px vertical icon toolbar on the left edge.
        /// 左端の32px縦アイコンツールバー
        /// </summary>
        private void DrawIconToolbar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(38), GUILayout.ExpandHeight(true));
            GUILayout.Space(4);

            Color prevBg = GUI.backgroundColor;
            DrawToolButton("B", L("ブラシ (B)", "Brush (B)"), StudioTool.Brush, ref prevBg);
            DrawToolButton("E", L("消しゴム (E)", "Eraser (E)"), StudioTool.Eraser, ref prevBg);
            DrawToolButton("V", L("移動 (V)", "Move (V)"), StudioTool.Move, ref prevBg);
            DrawToolButton("G", L("塗りつぶし (G)", "Fill (G)"), StudioTool.Fill, ref prevBg);
            DrawToolButton("M", L("矩形選択 (M)", "Rect Select (M)"), StudioTool.RectSelect, ref prevBg);
            DrawToolButton("L", L("投げ縄 (L)", "Lasso (L)"), StudioTool.LassoSelect, ref prevBg);
            DrawToolButton("I", L("スポイト (I)", "Eyedropper (I)"), StudioTool.Eyedropper, ref prevBg);
            DrawToolButton("\u2207", L("グラデーション", "Gradient"), StudioTool.Gradient, ref prevBg);

            GUILayout.Space(8);

            // Select tool (only when UV Mask tab with islands)
            if (currentTab == GeneratorTab.UVMask && islands.Count > 0)
                DrawToolButton("S", L("UV選択", "UV Select"), StudioTool.Select, ref prevBg);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawToolButton(string label, string tooltip, StudioTool tool, ref Color prevBg)
        {
            bool isActive = (activeTool == tool);
            if (isActive) GUI.backgroundColor = HighlightBlue;

            EnsureStyles();
            var style = isActive ? s_toolButtonActiveStyle : s_toolButtonStyle;

            if (GUILayout.Button(new GUIContent(label, tooltip), style, GUILayout.Width(34), GUILayout.Height(34)))
            {
                SwitchTool(tool);
                ToastNotification.Show(tooltip);
            }
            GUI.backgroundColor = prevBg;
        }

        private void DrawLeftStudioPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(leftPanelWidth), GUILayout.ExpandHeight(true));

            // Color picker OUTSIDE scroll (always visible in color mode)
            if (canvasColorMode == CanvasColorMode.Color)
            {
                colorPicker.DrawPanel(leftPanelWidth - 20f, GetResponsiveColorPickerMaxHeight());
                brushSettings.paintColor = colorPicker.ForegroundColor;
                brushSettings.backgroundColor = colorPicker.BackgroundColor;
                fillSettings.fillColor = colorPicker.ForegroundColor;
                EditorGUILayout.Space(2);
            }

            // Sub tool list as horizontal tabs
            DrawSubToolTabBar();

            EditorGUILayout.Space(4);

            // Scrollable content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            // Tool properties
            DrawCurrentToolPanel();

            // Symmetry settings
            EditorGUILayout.Space(4);
            SymmetryDrawing.DrawSettingsUI();

            // Brush/Eraser/Fill settings in left panel
            if (activeTool == StudioTool.Brush || activeTool == StudioTool.Eraser)
            {
                EditorGUILayout.Space(6);

                // Preset browser
                if (BrushPresetBrowser.DrawPresetBrowser(ActiveBrushSettings))
                    brush = new MaskTextureBrush(ActiveBrushSettings);

                EditorGUILayout.Space(4);
                string toolLabel = activeTool == StudioTool.Eraser
                    ? L("消しゴム設定", "Eraser Settings")
                    : L("ブラシ設定", "Brush Settings");
                EditorGUILayout.LabelField(toolLabel, EditorStyles.boldLabel);
                BrushSettingsUI.DrawBrushSettingsUI(ActiveBrushSettings);
            }
            else if (activeTool == StudioTool.Fill)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(L("塗りつぶし設定", "Fill Settings"), EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (canvasColorMode == CanvasColorMode.Color)
                    {
                        fillSettings.fillColor = EditorGUILayout.ColorField(
                            L("塗り色", "Fill Color"), fillSettings.fillColor);
                    }
                    else
                    {
                        fillSettings.fillValue = EditorGUILayout.Slider(L("値", "Value"), fillSettings.fillValue, 0f, 1f);
                        fillSettings.fillAlpha = EditorGUILayout.Slider(L("アルファ", "Alpha"), fillSettings.fillAlpha, 0f, 1f);
                    }
                    fillSettings.tolerance = EditorGUILayout.Slider(L("許容値", "Tolerance"), fillSettings.tolerance, 0f, 1f);
                    fillSettings.contiguous = EditorGUILayout.Toggle(L("隣接のみ", "Contiguous"), fillSettings.contiguous);
                }
            }
            else if (activeTool == StudioTool.Eyedropper)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(L("スポイト", "Eyedropper"), EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    L("キャンバスをクリックして色を取得します。", "Click on canvas to pick a color."),
                    MessageType.Info);
            }
            else if (activeTool == StudioTool.Gradient)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(L("グラデーション", "Gradient"), EditorStyles.boldLabel);
                GradientTool.DrawSettingsUI();
            }
            else if (activeTool == StudioTool.Move)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(L("移動ツール", "Move Tool"), EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    L("キャンバス上でドラッグしてレイヤーを移動します。", "Drag on canvas to move the active layer."),
                    MessageType.Info);
            }
            else if (activeTool == StudioTool.RectSelect || activeTool == StudioTool.LassoSelect)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(L("選択ツール", "Selection Tool"), EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        activeTool == StudioTool.RectSelect
                            ? L("ドラッグで矩形選択", "Drag to select rectangle")
                            : L("ドラッグで投げ縄選択", "Drag to lasso select"),
                        EditorStyles.miniLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(L("全選択", "Select All"), EditorStyles.miniButton))
                    {
                        EnsureSelection();
                        selection.SelectAll();
                    }
                    if (GUILayout.Button(L("選択解除", "Deselect"), EditorStyles.miniButton))
                    {
                        selection?.Clear();
                    }
                    if (GUILayout.Button(L("反転", "Invert"), EditorStyles.miniButton))
                    {
                        EnsureSelection();
                        selection.Invert();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private void DrawCenterStudioPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawCanvas();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightStudioPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(rightPanelWidth), GUILayout.ExpandHeight(true));

            // Navigator minimap
            if (canvasZoom > 1f && previewTexture != null)
            {
                canvasPan = MaskTextureNavigator.DrawNavigator(previewTexture, canvasZoom, canvasPan,
                    Mathf.Min(Mathf.Max(GetCanvasViewportWidth(), GetResponsiveCanvasMinWidth()), position.height - 190f));
            }

            // Tab bar
            EnsureStyles();
            EditorGUILayout.BeginHorizontal();
            string[] rpTabs = { L("レイヤー", "Layers"), L("フィルター", "Filters"), L("設定", "Settings") };
            for (int i = 0; i < rpTabs.Length; i++)
            {
                Color prevBg = GUI.backgroundColor;
                if (rightPanelTab == i) GUI.backgroundColor = HighlightBlue;
                if (GUILayout.Toggle(rightPanelTab == i, rpTabs[i], s_tabStyle))
                    rightPanelTab = i;
                GUI.backgroundColor = prevBg;
            }
            EditorGUILayout.EndHorizontal();

            switch (rightPanelTab)
            {
                case 0: // Layers
                    float navigatorHeightBudget = (canvasZoom > 1f && previewTexture != null) ? 170f : 26f;
                    float layerPanelHeight = Mathf.Max(320f, position.height - navigatorHeightBudget - 32f);
                    float layerScrollMinHeight = Mathf.Max(260f, layerPanelHeight - 92f);
                    bool layerChanged = MaskLayerPanelUI.DrawLayerPanel(layerStack, ref layerScrollPosition, layerScrollMinHeight, layerPanelHeight);
                    if (layerChanged)
                    {
                        bool livePreviewActive = liveLink != null && liveLink.IsEnabled;
                        if (livePreviewActive && Event.current.type == EventType.MouseDrag)
                        {
                            RequestPreviewRefresh(true, false);
                            RequestLiveLinkSync(false);
                        }
                        else
                        {
                            RequestPreviewRefresh(
                                Event.current.type != EventType.MouseDrag,
                                livePreviewActive);
                        }
                    }
                    break;
                case 1: // Filters
                    rightPanelScrollPosition = EditorGUILayout.BeginScrollView(rightPanelScrollPosition, GUILayout.ExpandHeight(true));
                    DrawFilterPanel();
                    EditorGUILayout.EndScrollView();
                    break;
                case 2: // Settings
                    rightPanelScrollPosition = EditorGUILayout.BeginScrollView(rightPanelScrollPosition, GUILayout.ExpandHeight(true));
                    DrawSettingsPanel();
                    // Reference image settings
                    if (referenceOverlay != null) referenceOverlay.DrawSettingsUI();
                    // Canvas background settings
                    if (canvasBackground != null) canvasBackground.DrawSettingsUI();
                    // Auto-save settings
                    if (autoSave != null) autoSave.DrawSettingsUI();
                    EditorGUILayout.EndScrollView();
                    break;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSubToolTabBar()
        {
            EnsureStyles();
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < tabLabels.Length; i++)
            {
                bool isSelected = (int)currentTab == i;
                var style = isSelected ? s_subTabSelectedStyle : s_subTabStyle;

                Color prevBg = GUI.backgroundColor;
                if (isSelected) GUI.backgroundColor = HighlightBlue;

                if (GUILayout.Toggle(isSelected, tabLabels[i], style, GUILayout.MinWidth(38)))
                    currentTab = (GeneratorTab)i;

                GUI.backgroundColor = prevBg;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMaterialAssignmentSection()
        {
            EnsureStyles();
            EditorGUILayout.LabelField(L("マテリアル割当", "Material Assignment"), s_sectionHeaderStyle);
            EditorGUILayout.Space(2);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Material suggestedMaterial = GetSuggestedLiveMaterial();
                EditorGUILayout.HelpBox(GetLiveSetupSummary(), MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(L("選択中を使う", "Use Selection")))
                    {
                        if (TryUseSelectionForLive(out string selectionMessage))
                            ToastNotification.ShowSuccess(selectionMessage);
                        else
                            ToastNotification.ShowWarning(selectionMessage);
                    }

                    if (GUILayout.Button(liveLink != null && liveLink.IsEnabled
                        ? L("LIVE解除", "Disable LIVE")
                        : L("かんたんLIVE", "Quick LIVE")))
                    {
                        ToggleLiveLinkFromUI(true, true);
                    }
                }

                if (targetMaterial == null && suggestedMaterial != null)
                {
                    EditorGUILayout.LabelField(
                        L($"選択候補: {suggestedMaterial.name}", $"Selection: {suggestedMaterial.name}"),
                        EditorStyles.miniLabel);
                }

                EditorGUILayout.Space(2);

                Material newMaterial = (Material)EditorGUILayout.ObjectField(
                    L("ターゲットマテリアル", "Target Material"),
                    targetMaterial,
                    typeof(Material),
                    false);
                if (newMaterial != targetMaterial)
                    CurrentTargetMaterial = newMaterial;

                if (targetMaterial == null)
                {
                    EditorGUILayout.HelpBox(
                        L("LIVE編集したいマテリアルを指定してください。", "Assign a material to enable LIVE editing."),
                        MessageType.Info);
                    return;
                }

                var propertyNames = new List<string>();
                var propertyLabels = new List<string>();
                GetMaterialTexturePropertyOptions(targetMaterial, propertyNames, propertyLabels);
                if (propertyNames.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        L("このマテリアルに編集可能なテクスチャプロパティが見つかりません。",
                          "No editable texture properties were found on this material."),
                        MessageType.Warning);
                    return;
                }

                string currentProperty = GetSelectedPropertyName();
                int propertyIndex = propertyNames.IndexOf(currentProperty);
                if (propertyIndex < 0)
                    propertyIndex = 0;

                int newPropertyIndex = EditorGUILayout.Popup(
                    L("割当先プロパティ", "Target Property"),
                    propertyIndex,
                    propertyLabels.ToArray());
                if (newPropertyIndex != propertyIndex)
                    CurrentPropertyName = propertyNames[newPropertyIndex];

                string effectiveProperty = propertyNames[Mathf.Clamp(newPropertyIndex, 0, propertyNames.Count - 1)];
                Texture currentTexture = targetMaterial.GetTexture(effectiveProperty);
                EditorGUILayout.ObjectField(
                    L("現在のテクスチャ", "Current Texture"),
                    currentTexture,
                    typeof(Texture),
                    false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = currentTexture is Texture2D;
                    if (GUILayout.Button(L("マテリアルから読込", "Load From Material")))
                        ImportTextureForEditing(currentTexture as Texture2D, effectiveProperty);
                    GUI.enabled = true;

                    bool canLiveLink = targetMaterial != null
                        && !string.IsNullOrEmpty(effectiveProperty)
                        && targetMaterial.HasProperty(effectiveProperty);
                    GUI.enabled = canLiveLink || (liveLink != null && liveLink.IsEnabled);
                    if (GUILayout.Button(liveLink != null && liveLink.IsEnabled
                        ? L("LIVE解除", "Disable LIVE")
                        : L("LIVE接続", "Enable LIVE")))
                    {
                        ToggleLiveLinkFromUI(false, false);
                    }
                    GUI.enabled = true;
                }

                if (liveLink != null && liveLink.IsEnabled)
                {
                    EditorGUILayout.HelpBox(
                        L("接続中: " + liveLink.BoundPropertyName,
                          "Connected: " + liveLink.BoundPropertyName),
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        L("未接続 — 上の LIVE 接続 かコマンドバーの [LIVE] を使います。",
                          "Disconnected — use the LIVE button above or in the command bar."),
                        EditorStyles.miniLabel);
                }
            }
        }

        private void DrawSettingsPanel()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawMaterialAssignmentSection();
            EditorGUILayout.Space(8);

            // === Input / Sensitivity ===
            EnsureStyles();
            EditorGUILayout.LabelField(L("入力設定", "Input Settings"), s_sectionHeaderStyle);
            EditorGUILayout.Space(2);

            shortcutProfile.brushSizeDragSensitivity = EditorGUILayout.Slider(
                new GUIContent(L("サイズ感度", "Size Sensitivity"),
                    L("Alt+RMBドラッグのサイズ感度", "Alt+RMB drag size sensitivity")),
                shortcutProfile.brushSizeDragSensitivity, 0.05f, 1f);
            shortcutProfile.brushOpacityDragSensitivity = EditorGUILayout.Slider(
                new GUIContent(L("不透明度感度", "Opacity Sensitivity"),
                    L("Alt+RMBドラッグの不透明度感度", "Alt+RMB drag opacity sensitivity")),
                shortcutProfile.brushOpacityDragSensitivity, 0.001f, 0.02f);

            shortcutProfile.allowMiddleMousePan = EditorGUILayout.Toggle(
                L("中クリックでパン", "Middle Mouse Pan"), shortcutProfile.allowMiddleMousePan);
            shortcutProfile.allowSpacePan = EditorGUILayout.Toggle(
                L("スペースでパン", "Space Pan"), shortcutProfile.allowSpacePan);
            shortcutProfile.allowAltLeftPan = EditorGUILayout.Toggle(
                L("Alt+左クリックでパン", "Alt+Left Pan"), shortcutProfile.allowAltLeftPan);

            EditorGUILayout.Space(8);

            // === Shortcut Key Customization ===
            EditorGUILayout.LabelField(L("ショートカットキー設定", "Shortcut Key Settings"),
                s_sectionHeaderStyle);
            EditorGUILayout.Space(2);

            // Preset buttons
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L("プリセット:", "Preset:"), GUILayout.Width(60));
            if (GUILayout.Button(L("デフォルト", "Default"), EditorStyles.miniButton))
            {
                shortcutProfile = MaskTextureShortcutProfile.CreateDefault();
                AutoSaveShortcuts();
                ToastNotification.ShowSuccess(L("デフォルトに戻しました", "Reset to Default"));
            }
            if (GUILayout.Button("Photoshop", EditorStyles.miniButton))
            {
                shortcutProfile = MaskTextureShortcutProfile.CreatePhotoshopLike();
                AutoSaveShortcuts();
                ToastNotification.ShowSuccess(L("Photoshop風に変更", "Photoshop style applied"));
            }
            if (GUILayout.Button("ClipStudio", EditorStyles.miniButton))
            {
                shortcutProfile = MaskTextureShortcutProfile.CreateClipStudioLike();
                AutoSaveShortcuts();
                ToastNotification.ShowSuccess(L("ClipStudio風に変更", "ClipStudio style applied"));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Rebinding hint
            if (rebindingActionId != null)
            {
                EditorGUILayout.HelpBox(
                    L("任意のキーを押してください（Escでキャンセル）\nCtrl/Shift/Altとの組み合わせも可能です",
                      "Press any key (Esc to cancel)\nCtrl/Shift/Alt combos supported"),
                    MessageType.Info);
            }

            // Ensure bindings exist
            if (shortcutProfile.bindings == null || shortcutProfile.bindings.Count == 0)
                shortcutProfile = MaskTextureShortcutProfile.CreateDefault();

            // Category styles (cached)

            // Draw bindings grouped by category
            settingsPanelScroll = EditorGUILayout.BeginScrollView(settingsPanelScroll, GUILayout.Height(260));

            string lastCategory = "";
            foreach (var binding in shortcutProfile.bindings)
            {
                // Category header
                string cat = binding.category ?? "";
                if (cat != lastCategory)
                {
                    if (!string.IsNullOrEmpty(lastCategory))
                        EditorGUILayout.Space(4);

                    string catDisplay = cat;
                    switch (cat)
                    {
                        case "Edit": catDisplay = L("編集", "Edit"); break;
                        case "Tools": catDisplay = L("ツール", "Tools"); break;
                        case "Brush": catDisplay = L("ブラシ", "Brush"); break;
                        case "Canvas": catDisplay = L("キャンバス", "Canvas"); break;
                        case "Color": catDisplay = L("カラー", "Color"); break;
                    }
                    EditorGUILayout.LabelField(catDisplay, s_categoryLabelStyle);
                    lastCategory = cat;
                }

                // Binding row
                EditorGUILayout.BeginHorizontal();

                // Action name (italic if modified)
                bool isModified = binding.IsModified;
                EditorGUILayout.LabelField(binding.displayName,
                    isModified ? s_shortcutModifiedStyle : s_shortcutNameStyle,
                    GUILayout.Width(130));

                // Key binding button
                bool isRebinding = (rebindingActionId == binding.actionId);
                Color prevBg = GUI.backgroundColor;
                if (isRebinding)
                    GUI.backgroundColor = RebindOrange; // Orange when rebinding

                // Check for duplicate
                var duplicate = MaskTextureShortcutUtility.FindDuplicate(shortcutProfile, binding);
                if (duplicate != null && !isRebinding)
                    GUI.backgroundColor = DuplicateRed; // Red if duplicate

                string keyLabel = isRebinding
                    ? "[ ... ]"
                    : binding.ToDisplayString();

                if (GUILayout.Button(keyLabel, GUILayout.Width(120), GUILayout.Height(20)))
                {
                    rebindingActionId = isRebinding ? null : binding.actionId;
                }
                GUI.backgroundColor = prevBg;

                // Duplicate warning icon
                if (duplicate != null && !isRebinding)
                {
                    GUILayout.Label(new GUIContent("⚠",
                        L($"重複: {duplicate.displayName}", $"Conflict: {duplicate.displayName}")),
                        GUILayout.Width(18));
                }
                else
                {
                    GUILayout.Space(18);
                }

                // Reset button (only if modified)
                if (isModified)
                {
                    if (GUILayout.Button(new GUIContent("↩",
                        L("デフォルトに戻す", "Reset to default")),
                        EditorStyles.miniButton, GUILayout.Width(22)))
                    {
                        binding.ResetToDefault();
                        AutoSaveShortcuts();
                    }
                }
                else
                {
                    GUILayout.Space(24);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Footer: modified count + reset all
            int modifiedCount = 0;
            foreach (var b in shortcutProfile.bindings)
                if (b.IsModified) modifiedCount++;

            EditorGUILayout.BeginHorizontal();
            if (modifiedCount > 0)
            {
                EditorGUILayout.LabelField(
                    L($"{modifiedCount}個のキーが変更されています", $"{modifiedCount} key(s) modified"),
                    s_modifiedCountStyle);
                if (GUILayout.Button(L("全てリセット", "Reset All"), EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    shortcutProfile = MaskTextureShortcutProfile.CreateDefault();
                    AutoSaveShortcuts();
                    ToastNotification.Show(L("全ショートカットをリセット", "All shortcuts reset"));
                }
            }
            else
            {
                EditorGUILayout.LabelField(
                    L("全てデフォルト", "All default"),
                    s_allDefaultStyle);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void AutoSaveShortcuts()
        {
            if (shortcutProfile != null)
                EditorPrefs.SetString("NataneToon_ShortcutProfile", shortcutProfile.ToJson());
        }

        private void DrawCurrentToolPanel()
        {
            switch (currentTab)
            {
                case GeneratorTab.Noise:
                    DrawNoiseTab();
                    break;
                case GeneratorTab.UVMask:
                    DrawUVMaskTab();
                    break;
                case GeneratorTab.Gradient:
                    DrawGradientTab();
                    break;
                case GeneratorTab.MeshInfo:
                    DrawMeshInfoTab();
                    break;
                case GeneratorTab.Combined:
                    DrawCombinedTab();
                    break;
                case GeneratorTab.Templates:
                    DrawTemplatesTab();
                    break;
                case GeneratorTab.ChannelPack:
                    DrawChannelPackTab();
                    break;
            }
        }

        private string GetCurrentTabDisplayName()
        {
            return GetStudioTabDisplayName(currentTab);
        }

        private static string GetStudioTabDisplayName(GeneratorTab tab)
        {
            switch (tab)
            {
                case GeneratorTab.Noise:
                    return L("ノイズ", "Noise");
                case GeneratorTab.UVMask:
                    return L("UVマスク", "UV Mask");
                case GeneratorTab.Gradient:
                    return L("グラデーション", "Gradient");
                case GeneratorTab.MeshInfo:
                    return L("メッシュ情報", "Mesh Info");
                case GeneratorTab.Combined:
                    return L("合成", "Combine");
                case GeneratorTab.Templates:
                    return L("テンプレート", "Templates");
                case GeneratorTab.ChannelPack:
                    return L("チャンネルパック", "Channel Pack");
                default:
                    return L("ツール", "Tool");
            }
        }

        private static string GetStudioTabTooltip(GeneratorTab tab)
        {
            switch (tab)
            {
                case GeneratorTab.Noise: return L("パーリン・ボロノイ等のノイズ生成", "Generate Perlin, Voronoi and other noise");
                case GeneratorTab.UVMask: return L("UVアイランド選択でマスク生成", "Generate masks from UV island selection");
                case GeneratorTab.Gradient: return L("線形・放射状・高さベースのグラデーション", "Linear, radial, and height-based gradients");
                case GeneratorTab.MeshInfo: return L("曲率・法線方向・頂点カラーの可視化", "Visualize curvature, normals, vertex colors");
                case GeneratorTab.Combined: return L("ノイズとUVマスクの合成", "Combine noise with UV masks");
                case GeneratorTab.Templates: return L("プリセットテンプレートを適用", "Apply preset templates");
                case GeneratorTab.ChannelPack: return L("複数テクスチャをRGBAチャンネルに結合", "Pack multiple textures into RGBA channels");
                default: return "";
            }
        }

        // ================================================================
        // Tab 1: Noise Generation
        // ================================================================

        private void DrawNoiseTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ノイズ設定", "Noise Settings"), EditorStyles.boldLabel);

            noiseType = (NoiseType)EditorGUILayout.EnumPopup(
                new GUIContent(L("ノイズタイプ", "Noise Type"), L("ノイズアルゴリズムの種類", "Type of noise algorithm")),
                noiseType);
            textureSize = EditorGUILayout.IntPopup(
                L("テクスチャサイズ", "Texture Size"),
                textureSize, textureSizeLabels, textureSizes);
            scale = EditorGUILayout.Slider(
                new GUIContent(L("スケール", "Scale"), L("ノイズの拡大率", "Noise zoom level")),
                scale, 0.5f, 50f);
            seed = EditorGUILayout.IntField(
                new GUIContent(L("シード", "Seed"), L("乱数シード値", "Random seed value")),
                seed);
            contrast = EditorGUILayout.Slider(
                new GUIContent(L("コントラスト", "Contrast"), L("明暗のコントラスト調整", "Brightness contrast adjustment")),
                contrast, 0.1f, 3f);
            invert = EditorGUILayout.Toggle(
                new GUIContent(L("反転", "Invert"), L("白黒を反転", "Invert black and white")),
                invert);

            if (noiseType == NoiseType.FBM)
            {
                NataneToonShaderGUIUtility.DrawSeparator();
                EditorGUILayout.LabelField(L("FBM設定", "FBM Settings"), EditorStyles.boldLabel);
                octaves = EditorGUILayout.IntSlider(L("オクターブ", "Octaves"), octaves, 1, 8);
                lacunarity = EditorGUILayout.Slider(L("ラクナリティ", "Lacunarity"), lacunarity, 1f, 4f);
                persistence = EditorGUILayout.Slider(L("パーシステンス", "Persistence"), persistence, 0f, 1f);
            }

            offset = EditorGUILayout.Vector2Field(L("オフセット", "Offset"), offset);

            // Alpha noise settings
            NataneToonShaderGUIUtility.DrawSeparator();
            noiseAlphaEnabled = EditorGUILayout.Toggle(
                new GUIContent(L("アルファノイズ有効", "Enable Alpha Noise"),
                    L("ノイズからアルファチャンネルを生成", "Generate alpha channel from noise")),
                noiseAlphaEnabled);

            if (noiseAlphaEnabled)
            {
                EditorGUI.indentLevel++;
                noiseAlphaType = (NoiseType)EditorGUILayout.EnumPopup(L("αノイズタイプ", "Alpha Noise Type"), noiseAlphaType);
                noiseAlphaScale = EditorGUILayout.Slider(L("αスケール", "Alpha Scale"), noiseAlphaScale, 0.5f, 50f);
                noiseAlphaSeed = EditorGUILayout.IntField(L("αシード", "Alpha Seed"), noiseAlphaSeed);
                noiseAlphaContrast = EditorGUILayout.Slider(L("αコントラスト", "Alpha Contrast"), noiseAlphaContrast, 0.1f, 3f);
                noiseAlphaInvert = EditorGUILayout.Toggle(L("α反転", "Alpha Invert"), noiseAlphaInvert);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("プレビュー生成", "Generate Preview"), GUILayout.Height(30)))
            {
                GenerateNoiseTexture();
            }
            if (GUILayout.Button(L("レイヤーに追加", "Add to Layer"), GUILayout.Height(30)))
            {
                GenerateNoiseToLayer();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void GenerateNoiseTexture()
        {
            int size = textureSize;
            Color[] pixels = GenerateNoisePixels(size);
            UpdatePreview(pixels, size);
        }

        private void GenerateNoiseToLayer()
        {
            EnsureLayerStack();
            int size = layerStack.Width;
            Color[] pixels = GenerateNoisePixels(size);
            var layer = layerStack.ActiveLayer;
            if (layer != null && layer.pixels != null && layer.pixels.Length == pixels.Length)
            {
                System.Array.Copy(pixels, layer.pixels, pixels.Length);
                layer.sourceType = MaskTextureLayer.SourceType.Noise;
            }
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
        }

        private Color[] GenerateNoisePixels(int size)
        {
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = (float)x / size * scale + offset.x;
                    float fy = (float)y / size * scale + offset.y;
                    float value = NoiseGenerator.Generate(noiseType, fx, fy, seed, octaves, lacunarity, persistence);
                    value = Mathf.Pow(Mathf.Clamp01(value), contrast);
                    if (invert) value = 1f - value;

                    float alpha = 1f;
                    if (noiseAlphaEnabled)
                    {
                        float afx = (float)x / size * noiseAlphaScale + offset.x;
                        float afy = (float)y / size * noiseAlphaScale + offset.y;
                        alpha = NoiseGenerator.Generate(noiseAlphaType, afx, afy, noiseAlphaSeed, octaves, lacunarity, persistence);
                        alpha = Mathf.Pow(Mathf.Clamp01(alpha), noiseAlphaContrast);
                        if (noiseAlphaInvert) alpha = 1f - alpha;
                    }

                    pixels[y * size + x] = new Color(value, value, value, alpha);
                }
            }
            return pixels;
        }

        // ================================================================
        // Tab 2: UV Island Mask
        // ================================================================

        private void DrawUVMaskTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("メッシュ設定", "Mesh Settings"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            meshSource = EditorGUILayout.ObjectField(L("メッシュソース", "Mesh Source"), meshSource, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
                InvalidateUvCache();

            uvChannel = EditorGUILayout.IntPopup(L("UVチャンネル", "UV Channel"), uvChannel, new[] { "UV0", "UV1", "UV2", "UV3" }, new[] { 0, 1, 2, 3 });
            maskTextureSize = EditorGUILayout.IntPopup(L("テクスチャサイズ", "Texture Size"), maskTextureSize, textureSizeLabels, textureSizes);

            EditorGUILayout.Space(5);

            if (GUILayout.Button(L("アイランドを検出", "Detect Islands"), GUILayout.Height(25)))
                DetectIslands();

            EditorGUILayout.EndVertical();

            if (islands.Count > 0)
            {
                DrawIslandList();
                DrawFillSettings();

                EditorGUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(L("プレビュー生成", "Generate Preview"), GUILayout.Height(30)))
                    GenerateUVMaskTexture();
                if (GUILayout.Button(L("レイヤーに追加", "Add to Layer"), GUILayout.Height(30)))
                    GenerateUVMaskToLayer();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawIslandList()
        {
            EditorGUILayout.Space(5);
            islandsFoldout = NataneToonShaderGUIUtility.DrawFoldoutHeader(
                L($"検出アイランド ({islands.Count})", $"Detected Islands ({islands.Count})"), islandsFoldout);

            if (!islandsFoldout) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("全選択", "Select All"), GUILayout.Height(20)))
            {
                foreach (var island in islands) island.selected = true;
                EditorApplication.delayCall += () => GenerateUVMaskTexture();
            }
            if (GUILayout.Button(L("全解除", "Deselect All"), GUILayout.Height(20)))
            {
                foreach (var island in islands) island.selected = false;
                EditorApplication.delayCall += () => GenerateUVMaskTexture();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            float listHeight = Mathf.Min(islands.Count * 22f, 200f);
            islandScrollPosition = EditorGUILayout.BeginScrollView(islandScrollPosition, GUILayout.Height(listHeight));

            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < islands.Count; i++)
            {
                var island = islands[i];

                if (islandSelectMode && i == hoveredIslandIndex)
                {
                    Rect highlightRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(highlightRect, IslandHighlight);
                    GUILayout.Space(-20);
                }

                Color labelColor = (islandColors != null && i < islandColors.Length)
                    ? islandColors[i] : Color.white;
                var style = new GUIStyle(EditorStyles.toggle);
                if (islandSelectMode) style.normal.textColor = labelColor;

                island.selected = EditorGUILayout.ToggleLeft(
                    L($"アイランド #{i}  (\u25B3{island.triangleCount}, 面積: {island.area:F3})",
                      $"Island #{i}  (\u25B3{island.triangleCount}, Area: {island.area:F3})"),
                    island.selected, style);
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorApplication.delayCall += () => GenerateUVMaskTexture();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawFillSettings()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("フィル設定", "Fill Settings"), EditorStyles.boldLabel);

            fillMode = (FillMode)EditorGUILayout.EnumPopup(L("フィルモード", "Fill Mode"), fillMode);

            if (fillMode == FillMode.BoundaryGradient)
            {
                gradientWidth = EditorGUILayout.IntSlider(L("グラデーション幅 (px)", "Gradient Width (px)"), gradientWidth, 1, 100);
                gradientDirection = (GradientDirection)EditorGUILayout.EnumPopup(L("方向", "Direction"), gradientDirection);
            }

            maskInvert = EditorGUILayout.Toggle(L("反転", "Invert"), maskInvert);

            EditorGUILayout.EndVertical();
        }

        private void InvalidateUvCache()
        {
            cachedUvMesh = null;
            cachedUvChannel = -1;
            cachedUvMaterialSlot = int.MinValue;
            cachedUvTriangles = new int[0];
            cachedUvList.Clear();
            islands.Clear();
            hoveredIslandIndex = -1;
        }

        private int GetResolvedUvMaterialSlot(Mesh mesh)
        {
            if (mesh == null || mesh.subMeshCount <= 0 || uvMaterialSlot < 0)
                return -1;

            return Mathf.Clamp(uvMaterialSlot, 0, mesh.subMeshCount - 1);
        }

        private void EnsureUvCache(Mesh mesh)
        {
            if (mesh == null)
                return;

            int resolvedSlot = GetResolvedUvMaterialSlot(mesh);
            if (cachedUvMesh == mesh && cachedUvChannel == uvChannel && cachedUvMaterialSlot == resolvedSlot && cachedUvTriangles != null)
                return;

            cachedUvMesh = mesh;
            cachedUvChannel = uvChannel;
            cachedUvMaterialSlot = resolvedSlot;
            cachedUvList.Clear();
            mesh.GetUVs(uvChannel, cachedUvList);
            cachedUvTriangles = resolvedSlot < 0 ? mesh.triangles : mesh.GetTriangles(resolvedSlot);
        }

        private List<Vector2> GetCachedUvList(Mesh mesh)
        {
            EnsureUvCache(mesh);
            return cachedUvList;
        }

        private int[] GetActiveUvTriangles(Mesh mesh)
        {
            EnsureUvCache(mesh);
            return cachedUvTriangles ?? new int[0];
        }

        private void DrawUvMaterialSlotToolbar()
        {
            Mesh mesh = ExtractMesh();
            if (mesh == null || mesh.subMeshCount <= 0)
                return;

            var labels = new List<string> { L("All", "All") };
            Renderer renderer = ExtractRenderer();
            Material[] materials = renderer != null ? renderer.sharedMaterials : null;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                string materialName = materials != null && i < materials.Length && materials[i] != null
                    ? materials[i].name
                    : $"{L("Slot", "Slot")} {i}";
                labels.Add($"[{i}] {materialName}");
            }

            int popupIndex = Mathf.Clamp(uvMaterialSlot + 1, 0, labels.Count - 1);
            int newPopupIndex = EditorGUILayout.Popup(popupIndex, labels.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(120));
            if (newPopupIndex != popupIndex)
            {
                bool hadIslands = islands != null && islands.Count > 0;
                uvMaterialSlot = newPopupIndex - 1;
                InvalidateUvCache();
                if (hadIslands)
                    DetectIslands();
            }
        }

        private Mesh ExtractMesh()
        {
            if (meshSource == null) return null;
            if (meshSource is Mesh mesh) return mesh;
            if (meshSource is GameObject go)
            {
                var smr = go.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && smr.sharedMesh != null) return smr.sharedMesh;
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) return mf.sharedMesh;
            }
            if (meshSource is MeshFilter meshFilter && meshFilter.sharedMesh != null)
                return meshFilter.sharedMesh;
            if (meshSource is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                return skinned.sharedMesh;
            return null;
        }

        private Renderer ExtractRenderer()
        {
            if (meshSource is GameObject go)
                return go.GetComponent<Renderer>();
            if (meshSource is MeshFilter mf)
                return mf.GetComponent<Renderer>();
            if (meshSource is SkinnedMeshRenderer smr)
                return smr;
            return null;
        }

        private void EnableLiveLink()
        {
            if (liveLink == null) liveLink = new MaskTextureLiveLink();
            if (targetMaterial == null)
                return;

            string propName = GetSelectedPropertyName();
            if (string.IsNullOrEmpty(propName) || !targetMaterial.HasProperty(propName))
                return;

            int size = previewTexture != null ? previewTexture.width : textureSize;
            liveLink.Enable(targetMaterial, propName, size);

            if (liveLink.IsEnabled && previewTexture != null)
                liveLink.UpdateTextureImmediate(previewTexture);
        }

        private void DisableLiveLink()
        {
            liveLink?.Disable();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                liveLink?.Disable();
        }

        private void OnBeforeAssemblyReload()
        {
            liveLink?.Disable();
        }

        private void DetectIslands()
        {
            Mesh mesh = ExtractMesh();
            if (mesh == null)
            {
                EditorUtility.DisplayDialog(L("エラー", "Error"),
                    L("メッシュを取得できません。\nMesh, GameObject, MeshFilter, SkinnedMeshRenderer を指定してください。",
                      "Cannot get mesh.\nPlease specify a Mesh, GameObject, MeshFilter, or SkinnedMeshRenderer."), "OK");
                return;
            }

            List<Vector2> uvs = GetCachedUvList(mesh);

            if (uvs.Count == 0)
            {
                EditorUtility.DisplayDialog(L("エラー", "Error"), L($"UV{uvChannel} が存在しません。", $"UV{uvChannel} does not exist."), "OK");
                return;
            }

            islands = UVIslandDetector.DetectIslands(mesh, uvs, GetActiveUvTriangles(mesh));
            GenerateIslandColors();
            hoveredIslandIndex = -1;

            if (islands.Count == 0)
                EditorUtility.DisplayDialog(L("情報", "Info"), L("アイランドが検出されませんでした。", "No islands were detected."), "OK");
        }

        private void GenerateIslandColors()
        {
            if (islands == null || islands.Count == 0) { islandColors = null; return; }
            islandColors = new Color[islands.Count];
            for (int i = 0; i < islands.Count; i++)
            {
                float hue = (float)i / islands.Count;
                islandColors[i] = Color.HSVToRGB(hue, 0.7f, 0.9f);
            }
        }

        private static bool PointInTriangleUV(Vector2 p, Vector2 v0, Vector2 v1, Vector2 v2)
        {
            float denom = (v1.y - v2.y) * (v0.x - v2.x) + (v2.x - v1.x) * (v0.y - v2.y);
            if (Mathf.Abs(denom) < 1e-8f) return false;
            float invDenom = 1f / denom;
            float w0 = ((v1.y - v2.y) * (p.x - v2.x) + (v2.x - v1.x) * (p.y - v2.y)) * invDenom;
            float w1 = ((v2.y - v0.y) * (p.x - v2.x) + (v0.x - v2.x) * (p.y - v2.y)) * invDenom;
            float w2 = 1f - w0 - w1;
            return w0 >= 0f && w1 >= 0f && w2 >= 0f;
        }

        private int FindIslandAtUV(Vector2 uv, Mesh mesh, List<Vector2> uvs)
        {
            int[] triangles = GetActiveUvTriangles(mesh);
            for (int i = 0; i < islands.Count; i++)
            {
                if (!islands[i].uvBounds.Contains(uv)) continue;
                foreach (int t in islands[i].triangleIndices)
                {
                    int i0 = triangles[t * 3 + 0];
                    int i1 = triangles[t * 3 + 1];
                    int i2 = triangles[t * 3 + 2];
                    if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;
                    if (PointInTriangleUV(uv, uvs[i0], uvs[i1], uvs[i2]))
                        return i;
                }
            }
            return -1;
        }

        private void HandleIslandClickInput(Rect canvasArea, Rect texRect)
        {
            Event e = Event.current;
            if (!canvasArea.Contains(e.mousePosition)) { hoveredIslandIndex = -1; return; }

            Mesh mesh = ExtractMesh();
            if (mesh == null || islands.Count == 0) return;

            List<Vector2> uvs = GetCachedUvList(mesh);
            if (uvs.Count == 0) return;

            if (e.type == EventType.MouseMove)
            {
                Vector2 uv = BrushCanvasInputHandler.MouseToUV(e.mousePosition, texRect);
                int newHover = FindIslandAtUV(uv, mesh, uvs);
                if (newHover != hoveredIslandIndex)
                {
                    hoveredIslandIndex = newHover;
                    Repaint();
                }
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Vector2 uv = BrushCanvasInputHandler.MouseToUV(e.mousePosition, texRect);
                int hitIndex = FindIslandAtUV(uv, mesh, uvs);
                if (hitIndex >= 0)
                {
                    islands[hitIndex].selected = !islands[hitIndex].selected;
                    EditorApplication.delayCall += () => GenerateUVMaskTexture();
                    e.Use();
                    Repaint();
                }
            }
        }

        private Color[] GenerateUVMaskPixels(int size)
        {
            Mesh mesh = ExtractMesh();
            if (mesh == null) return null;

            List<Vector2> uvs = GetCachedUvList(mesh);
            if (uvs.Count == 0) return null;

            Color[] pixels = new Color[size * size];
            bool[] selectedFlags = islands.Select(i => i.selected).ToArray();
            TriangleRasterizer.RasterizeIslands(pixels, size, GetActiveUvTriangles(mesh), uvs, islands, selectedFlags);

            if (fillMode == FillMode.BoundaryGradient)
                BoundaryGradient.Apply(pixels, size, gradientWidth, gradientDirection);

            if (maskInvert)
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    float v = 1f - pixels[i].r;
                    pixels[i] = new Color(v, v, v, 1f);
                }
            }

            return pixels;
        }

        private void GenerateUVMaskTexture()
        {
            int size = maskTextureSize;
            Color[] pixels = GenerateUVMaskPixels(size);
            if (pixels != null) UpdatePreview(pixels, size);
        }

        private void GenerateUVMaskToLayer()
        {
            EnsureLayerStack();
            int size = layerStack.Width;
            Color[] pixels = GenerateUVMaskPixels(size);
            if (pixels == null) return;
            var layer = layerStack.ActiveLayer;
            if (layer != null && layer.pixels != null && layer.pixels.Length == pixels.Length)
            {
                System.Array.Copy(pixels, layer.pixels, pixels.Length);
                layer.sourceType = MaskTextureLayer.SourceType.UVMask;
            }
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
        }

        // ================================================================
        // Tab 3: Gradient
        // ================================================================

        private void DrawGradientTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("グラデーション設定", "Gradient Settings"), EditorStyles.boldLabel);

            textureSize = EditorGUILayout.IntPopup(L("テクスチャサイズ", "Texture Size"), textureSize, textureSizeLabels, textureSizes);
            gradientType = (GradientType)EditorGUILayout.EnumPopup(L("タイプ", "Type"), gradientType);

            if (gradientType == GradientType.HeightBased)
            {
                DrawMeshSourceField();
                EditorGUILayout.HelpBox(
                    L("高さベースグラデーションにはメッシュが必要です。", "Height-based gradient requires a mesh."),
                    MessageType.Info);
            }
            else
            {
                if (gradientType == GradientType.Linear || gradientType == GradientType.Angular)
                    gradientParams.angle = EditorGUILayout.Slider(L("角度", "Angle"), gradientParams.angle, 0f, 360f);

                if (gradientType == GradientType.Radial)
                    gradientParams.radius = EditorGUILayout.Slider(L("半径", "Radius"), gradientParams.radius, 0.01f, 2f);

                gradientParams.center = EditorGUILayout.Vector2Field(L("中心", "Center"), gradientParams.center);
            }

            gradientParams.curve = EditorGUILayout.CurveField(L("カーブ", "Curve"), gradientParams.curve);
            gradientParams.invert = EditorGUILayout.Toggle(L("反転", "Invert"), gradientParams.invert);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("プレビュー生成", "Generate Preview"), GUILayout.Height(30)))
                GenerateGradientTexture();
            if (GUILayout.Button(L("レイヤーに追加", "Add to Layer"), GUILayout.Height(30)))
                GenerateGradientToLayer();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void GenerateGradientTexture()
        {
            int size = textureSize;
            Color[] pixels = GenerateGradientPixels(size);
            if (pixels != null) UpdatePreview(pixels, size);
        }

        private void GenerateGradientToLayer()
        {
            EnsureLayerStack();
            int size = layerStack.Width;
            Color[] pixels = GenerateGradientPixels(size);
            if (pixels == null) return;
            var layer = layerStack.ActiveLayer;
            if (layer != null && layer.pixels != null && layer.pixels.Length == pixels.Length)
            {
                System.Array.Copy(pixels, layer.pixels, pixels.Length);
                layer.sourceType = MaskTextureLayer.SourceType.Gradient;
            }
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
        }

        private Color[] GenerateGradientPixels(int size)
        {
            Color[] pixels = new Color[size * size];

            if (gradientType == GradientType.HeightBased)
            {
                Mesh mesh = ExtractMesh();
                if (mesh == null)
                {
                    EditorUtility.DisplayDialog(L("エラー", "Error"), L("メッシュを取得できません。", "Cannot get mesh."), "OK");
                    return null;
                }
                List<Vector2> uvs = new List<Vector2>();
                mesh.GetUVs(uvChannel, uvs);
                if (uvs.Count == 0)
                {
                    EditorUtility.DisplayDialog(L("エラー", "Error"), L($"UV{uvChannel} が存在しません。", $"UV{uvChannel} does not exist."), "OK");
                    return null;
                }
                GradientGenerator.GenerateHeightBased(pixels, size, mesh, uvs,
                    gradientParams.curve, gradientParams.invert);
            }
            else
            {
                GradientGenerator.Generate(pixels, size, gradientType, gradientParams);
            }

            return pixels;
        }

        // ================================================================
        // Tab 4: Mesh Info
        // ================================================================

        private void DrawMeshInfoTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("メッシュ情報", "Mesh Info Settings"), EditorStyles.boldLabel);

            DrawMeshSourceField();
            textureSize = EditorGUILayout.IntPopup(L("テクスチャサイズ", "Texture Size"), textureSize, textureSizeLabels, textureSizes);
            meshInfoType = (MeshInfoType)EditorGUILayout.EnumPopup(L("タイプ", "Type"), meshInfoType);

            switch (meshInfoType)
            {
                case MeshInfoType.Curvature:
                    curvatureSensitivity = EditorGUILayout.Slider(L("感度", "Sensitivity"), curvatureSensitivity, 0.1f, 5f);
                    break;
                case MeshInfoType.NormalDirection:
                    normalDirection = EditorGUILayout.Vector3Field(L("方向", "Direction"), normalDirection);
                    normalThreshold = EditorGUILayout.Slider(L("閾値", "Threshold"), normalThreshold, 0f, 1f);
                    break;
                case MeshInfoType.VertexColor:
                    vertexColorChannel = EditorGUILayout.IntPopup(L("チャンネル", "Channel"), vertexColorChannel,
                        new[] { "R", "G", "B", "A" }, new[] { 0, 1, 2, 3 });
                    break;
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("プレビュー生成", "Generate Preview"), GUILayout.Height(30)))
                GenerateMeshInfoTexture();
            if (GUILayout.Button(L("レイヤーに追加", "Add to Layer"), GUILayout.Height(30)))
                GenerateMeshInfoToLayer();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private Color[] GenerateMeshInfoPixels(int size)
        {
            Mesh mesh = ExtractMesh();
            if (mesh == null)
            {
                EditorUtility.DisplayDialog(L("エラー", "Error"), L("メッシュを取得できません。", "Cannot get mesh."), "OK");
                return null;
            }
            List<Vector2> uvs = GetCachedUvList(mesh);
            if (uvs.Count == 0)
            {
                EditorUtility.DisplayDialog(L("エラー", "Error"), L($"UV{uvChannel} が存在しません。", $"UV{uvChannel} does not exist."), "OK");
                return null;
            }

            Color[] pixels = new Color[size * size];

            switch (meshInfoType)
            {
                case MeshInfoType.Curvature:
                    MeshInfoGenerator.GenerateCurvature(pixels, size, mesh, uvs, curvatureSensitivity);
                    break;
                case MeshInfoType.NormalDirection:
                    MeshInfoGenerator.GenerateNormalDirection(pixels, size, mesh, uvs, normalDirection, normalThreshold);
                    break;
                case MeshInfoType.VertexColor:
                    MeshInfoGenerator.GenerateVertexColor(pixels, size, mesh, uvs, vertexColorChannel);
                    break;
            }

            return pixels;
        }

        private void GenerateMeshInfoTexture()
        {
            int size = textureSize;
            Color[] pixels = GenerateMeshInfoPixels(size);
            if (pixels != null) UpdatePreview(pixels, size);
        }

        private void GenerateMeshInfoToLayer()
        {
            EnsureLayerStack();
            int size = layerStack.Width;
            Color[] pixels = GenerateMeshInfoPixels(size);
            if (pixels == null) return;
            var layer = layerStack.ActiveLayer;
            if (layer != null && layer.pixels != null && layer.pixels.Length == pixels.Length)
            {
                System.Array.Copy(pixels, layer.pixels, pixels.Length);
                layer.sourceType = MaskTextureLayer.SourceType.MeshInfo;
            }
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
        }

        // ================================================================
        // Tab 5: Combined
        // ================================================================

        private void DrawCombinedTab()
        {
            // Mesh settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("メッシュ設定", "Mesh Settings"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            meshSource = EditorGUILayout.ObjectField(L("メッシュソース", "Mesh Source"), meshSource, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
                InvalidateUvCache();

            uvChannel = EditorGUILayout.IntPopup(L("UVチャンネル", "UV Channel"), uvChannel, new[] { "UV0", "UV1", "UV2", "UV3" }, new[] { 0, 1, 2, 3 });

            EditorGUILayout.Space(5);

            if (GUILayout.Button(L("アイランドを検出", "Detect Islands"), GUILayout.Height(25)))
                DetectIslands();

            EditorGUILayout.EndVertical();

            if (islands.Count > 0)
                DrawIslandList();

            // Noise settings
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ノイズ設定", "Noise Settings"), EditorStyles.boldLabel);

            noiseType = (NoiseType)EditorGUILayout.EnumPopup(L("ノイズタイプ", "Noise Type"), noiseType);
            textureSize = EditorGUILayout.IntPopup(L("テクスチャサイズ", "Texture Size"), textureSize, textureSizeLabels, textureSizes);
            scale = EditorGUILayout.Slider(L("スケール", "Scale"), scale, 0.5f, 50f);
            seed = EditorGUILayout.IntField(L("シード", "Seed"), seed);
            contrast = EditorGUILayout.Slider(L("コントラスト", "Contrast"), contrast, 0.1f, 3f);

            if (noiseType == NoiseType.FBM)
            {
                NataneToonShaderGUIUtility.DrawSeparator();
                EditorGUILayout.LabelField(L("FBM設定", "FBM Settings"), EditorStyles.boldLabel);
                octaves = EditorGUILayout.IntSlider(L("オクターブ", "Octaves"), octaves, 1, 8);
                lacunarity = EditorGUILayout.Slider(L("ラクナリティ", "Lacunarity"), lacunarity, 1f, 4f);
                persistence = EditorGUILayout.Slider(L("パーシステンス", "Persistence"), persistence, 0f, 1f);
            }

            offset = EditorGUILayout.Vector2Field(L("オフセット", "Offset"), offset);
            EditorGUILayout.EndVertical();

            // Combine settings
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("合成設定", "Combine Settings"), EditorStyles.boldLabel);

            combineMode = (CombineMode)EditorGUILayout.EnumPopup(L("合成モード", "Combine Mode"), combineMode);
            invert = EditorGUILayout.Toggle(L("反転", "Invert"), invert);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            if (GUILayout.Button(L("テクスチャを生成", "Generate Texture"), GUILayout.Height(30)))
                GenerateCombinedTexture();
        }

        private void GenerateCombinedTexture()
        {
            Mesh mesh = ExtractMesh();
            if (mesh == null)
            {
                EditorUtility.DisplayDialog(L("エラー", "Error"), L("メッシュを取得できません。", "Cannot get mesh."), "OK");
                return;
            }

            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(uvChannel, uvs);
            if (uvs.Count == 0)
            {
                EditorUtility.DisplayDialog(L("エラー", "Error"), L($"UV{uvChannel} が存在しません。", $"UV{uvChannel} does not exist."), "OK");
                return;
            }

            int size = textureSize;

            Color[] maskPixels = new Color[size * size];
            bool[] selectedFlags = islands.Select(i => i.selected).ToArray();
            TriangleRasterizer.RasterizeIslands(maskPixels, size, GetActiveUvTriangles(mesh), uvs, islands, selectedFlags);

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = (float)x / size * scale + offset.x;
                    float fy = (float)y / size * scale + offset.y;
                    float noiseValue = NoiseGenerator.Generate(noiseType, fx, fy, seed, octaves, lacunarity, persistence);
                    noiseValue = Mathf.Pow(Mathf.Clamp01(noiseValue), contrast);

                    float maskValue = maskPixels[y * size + x].r;
                    float combined;

                    switch (combineMode)
                    {
                        case CombineMode.Multiply:
                            combined = maskValue * noiseValue;
                            break;
                        case CombineMode.MaskOnly:
                            combined = maskValue > 0f ? noiseValue : 0f;
                            break;
                        default:
                            combined = noiseValue;
                            break;
                    }

                    if (invert) combined = 1f - combined;
                    pixels[y * size + x] = new Color(combined, combined, combined, 1f);
                }
            }

            UpdatePreview(pixels, size);
        }

        // ================================================================
        // Tab 6: Templates
        // ================================================================

        private void DrawTemplatesTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("テンプレート", "Templates"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                L("テンプレートを選択してレイヤースタックに適用します。", "Select a template to apply to the layer stack."),
                MessageType.Info);

            DrawMeshSourceField();

            EditorGUILayout.Space(5);

            var templates = MaskTextureTemplates.GetAllTemplateInfos();

            templateScrollPosition = EditorGUILayout.BeginScrollView(
                templateScrollPosition, GUILayout.Height(Mathf.Min(templates.Count * 60f, 300f)));

            foreach (var (template, info) in templates)
            {
                bool isSelected = (template == selectedTemplate);
                var bgColor = GUI.backgroundColor;
                if (isSelected)
                    GUI.backgroundColor = TemplateSelectedBg;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(isSelected ? "\u25CF" : "\u25CB", GUILayout.Width(20)))
                    selectedTemplate = template;

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"{info.nameJP} / {info.nameEN}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(info.descriptionJP, EditorStyles.wordWrappedMiniLabel);
                if (info.requiresMesh)
                    EditorGUILayout.LabelField(L("* メッシュ必要", "* Mesh required"), EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = bgColor;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            if (GUILayout.Button(L("テンプレートを適用", "Apply Template"), GUILayout.Height(30)))
            {
                ApplySelectedTemplate();
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplySelectedTemplate()
        {
            EnsureLayerStack();
            Mesh mesh = ExtractMesh();
            List<Vector2> uvs = null;
            if (mesh != null)
            {
                uvs = new List<Vector2>();
                mesh.GetUVs(uvChannel, uvs);
            }
            MaskTextureTemplates.ApplyTemplate(selectedTemplate, layerStack, layerStack.Width, mesh, uvs);
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
        }

        // ================================================================
        // Tab 7: Channel Pack
        // ================================================================

        private void DrawChannelPackTab()
        {
            MaskTextureChannelPacker.DrawChannelPackUI();
        }

        // ================================================================
        // Canvas with Zoom/Pan and Brush Support
        // ================================================================

        private void DrawCanvas()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            // Step 5: Simplified canvas header (controls moved to command bar)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L("キャンバス", "Canvas"), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Island select button only when UV Mask tab
            if (currentTab == GeneratorTab.UVMask && islands.Count > 0 && activeTool == StudioTool.Select)
            {
                EditorGUILayout.LabelField(L("アイランド選択モード", "Island Select Mode"), EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // Reserve the status bar explicitly so the bottom line never gets clipped.
            const float canvasStatusGap = 4f;
            const float canvasStatusBottomPadding = 4f;
            Rect canvasContentRect = GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            float statusBarHeight = Mathf.Max(1f, Mathf.Min(StudioStatusBar.PreferredHeight, canvasContentRect.height - canvasStatusBottomPadding));
            Rect statusBarRect = new Rect(
                canvasContentRect.x,
                canvasContentRect.yMax - canvasStatusBottomPadding - statusBarHeight,
                canvasContentRect.width,
                statusBarHeight);
            Rect canvasRow = new Rect(
                canvasContentRect.x,
                canvasContentRect.y,
                canvasContentRect.width,
                Mathf.Max(1f, statusBarRect.y - canvasContentRect.y - canvasStatusGap));
            float availableWidth = Mathf.Max(1f, canvasRow.width);
            float availableHeight = Mathf.Max(1f, canvasRow.height);
            float canvasDisplaySize = Mathf.Max(1f, Mathf.Min(960f, Mathf.Min(availableWidth, availableHeight)));
            Rect canvasArea = new Rect(
                canvasRow.x + Mathf.Max(0f, (canvasRow.width - canvasDisplaySize) * 0.5f),
                canvasRow.y + Mathf.Max(0f, (canvasRow.height - canvasDisplaySize) * 0.5f),
                canvasDisplaySize,
                canvasDisplaySize);

            // Background (null-safe: Unity serialization may reset fields)
            if (canvasBackground == null) canvasBackground = new CanvasBackgroundSettings();
            canvasBackground.DrawBackground(canvasArea);

            Texture2D canvasTexture = canvasPreviewTexture != null ? canvasPreviewTexture : previewTexture;
            if (canvasTexture != null)
            {
                // Calculate zoomed/panned rect
                float texSize = canvasDisplaySize * canvasZoom;
                float offsetX = canvasArea.x + (canvasDisplaySize - texSize) * 0.5f + canvasPan.x;
                float offsetY = canvasArea.y + (canvasDisplaySize - texSize) * 0.5f + canvasPan.y;
                Rect texRect = new Rect(offsetX, offsetY, texSize, texSize);
                Rect paintInputRect = TilingPreview.GetInteractiveTextureRect(canvasArea, texRect);
                bool useTiledPaintInput = TilingPreview.IsEnabled;

                // Clip to canvas
                GUI.BeginClip(canvasArea);
                Rect clippedRect = new Rect(
                    texRect.x - canvasArea.x,
                    texRect.y - canvasArea.y,
                    texRect.width, texRect.height);
                EditorGUI.DrawPreviewTexture(clippedRect, canvasTexture, null, ScaleMode.ScaleToFit);

                // UV wireframe overlay
                if (showUVWireframe)
                {
                    Mesh mesh = ExtractMesh();
                    if (mesh != null)
                    {
                        List<Vector2> uvs = GetCachedUvList(mesh);
                        if (uvs.Count > 0)
                        {
                            int[] triangles = GetActiveUvTriangles(mesh);
                            if (islandSelectMode && islands.Count > 0 && islandColors != null)
                            {
                                UVWireframeRenderer.DrawIslandFill(clippedRect, mesh, uvs, islands, islandColors, triangles);
                                UVWireframeRenderer.DrawWireframePerIsland(clippedRect, mesh, uvs, islands, islandColors, hoveredIslandIndex, triangles);
                            }
                            else
                            {
                                UVWireframeRenderer.DrawWireframe(clippedRect, mesh, uvs, triangles);
                            }
                        }
                    }
                }

                GUI.EndClip();

                // Pixel grid at high zoom
                if (PixelGridRenderer.ShouldDraw(canvasZoom))
                    PixelGridRenderer.DrawGrid(texRect, layerStack != null ? layerStack.Width : textureSize, layerStack != null ? layerStack.Height : textureSize, canvasZoom);

                MaskTextureLayer activeLayer = layerStack != null ? layerStack.ActiveLayer : null;
                HandleCanvasShortcuts(canvasArea, paintInputRect, activeLayer, useTiledPaintInput);

                // Brush input handling (mask edit mode aware)
                if (brushEnabled && activeLayer != null && Event.current.type != EventType.Used)
                {
                    // Determine target pixels: layer mask or layer content
                    Color[] targetPixels = (activeLayer.editingMask && activeLayer.mask != null)
                        ? activeLayer.mask : activeLayer.pixels;
                    bool lockTransparent = (activeLayer.editingMask) ? false : activeLayer.lockTransparentPixels;

                    if (targetPixels != null && !activeLayer.locked)
                    {
                        bool modified;
                        BrushStrokeCommit strokeCommit;
                        BrushCanvasInputHandler.HandleBrushInput(
                            canvasArea, paintInputRect, brush, ActiveBrushSettings,
                            targetPixels, activeLayer.width, activeLayer.height,
                            lockTransparent,
                            brushStabilizer,
                            useTiledPaintInput,
                            out modified,
                            out strokeCommit);
                        if (modified)
                        {
                            activeLayer.sourceType = MaskTextureLayer.SourceType.Paint;
                            layerStack.InvalidateFlattenCache();

                            // Track dirty region from brush position / ブラシ位置からダーティ領域を追跡
                            int brushRadius = Mathf.CeilToInt(ActiveBrushSettings.size * 0.5f) + 1;
                            Vector2 pixelPos = BrushCanvasInputHandler.MouseToPixelPos(
                                Event.current.mousePosition, paintInputRect, activeLayer.width, activeLayer.height);
                            brushDirtyRect.Expand(
                                Mathf.RoundToInt(pixelPos.x),
                                Mathf.RoundToInt(pixelPos.y),
                                brushRadius);

                            bool livePreviewActive = liveLink != null && liveLink.IsEnabled;
                            if (strokeCommit.HasValue)
                                RequestPreviewRefreshFromBrush(livePreviewActive);
                            else if (livePreviewActive)
                            {
                                RequestPreviewRefreshFromBrush(false);
                                RequestLiveLinkSync(false);
                            }
                            else
                                RequestPreviewRefreshFromBrush(false);
                        }

                        if (strokeCommit.HasValue)
                        {
                            brushHistory.Record(activeLayer, strokeCommit.BeforePixels, strokeCommit.AfterPixels, "Brush Stroke");
                            SetLineAnchor(strokeCommit.LastPixelPosition);
                        }
                    }
                }

                // Brush cursor / line preview
                if (brushEnabled && canvasArea.Contains(Event.current.mousePosition))
                {
                    var curSettings = ActiveBrushSettings;
                    float cursorRadius = curSettings.size *
                        (paintInputRect.width / Mathf.Max(1, layerStack != null ? layerStack.Width : textureSize));
                    Color cursorColor = activeTool == StudioTool.Eraser ? EraserCursorPink
                        : (activeLayer != null && activeLayer.editingMask) ? MaskCursorCyan : Color.white;
                    BrushCursorRenderer.DrawCursorWithHardness(
                        Event.current.mousePosition, cursorRadius, curSettings.hardness,
                        cursorColor);

                    if (shortcutState.hasLineAnchor && Event.current.shift && activeLayer != null)
                    {
                        Vector2 lineStart = BrushCanvasInputHandler.PixelToCanvasPos(
                            shortcutState.lineAnchorPixel, paintInputRect, activeLayer.width, activeLayer.height);
                        BrushCursorRenderer.DrawLineGuide(lineStart, Event.current.mousePosition, LineGuideYellow);
                    }

                    if (shortcutState.isAdjustingBrush)
                        DrawBrushAdjustOverlay(Event.current.mousePosition);
                    Repaint();
                }

                // Fill tool input
                if (activeTool == StudioTool.Fill && activeLayer != null && !activeLayer.locked
                    && Event.current.type == EventType.MouseDown && Event.current.button == 0
                    && canvasArea.Contains(Event.current.mousePosition))
                {
                    HandleFillToolInput(canvasArea, paintInputRect, activeLayer, useTiledPaintInput);
                }

                // Move tool input
                if (activeTool == StudioTool.Move && activeLayer != null)
                {
                    HandleMoveToolInput(canvasArea, texRect, activeLayer);
                }

                // Eyedropper tool input
                if (activeTool == StudioTool.Eyedropper && activeLayer != null
                    && Event.current.type == EventType.MouseDown && Event.current.button == 0
                    && canvasArea.Contains(Event.current.mousePosition))
                {
                    Vector2 eyePixelPos = BrushCanvasInputHandler.MouseToPixelPos(
                        Event.current.mousePosition, paintInputRect, activeLayer.width, activeLayer.height);
                    if (useTiledPaintInput)
                        eyePixelPos = MaskTextureBrush.WrapPixelPosition(eyePixelPos, activeLayer.width, activeLayer.height);
                    int epx = Mathf.Clamp(Mathf.RoundToInt(eyePixelPos.x), 0, activeLayer.width - 1);
                    int epy = Mathf.Clamp(Mathf.RoundToInt(eyePixelPos.y), 0, activeLayer.height - 1);
                    Color picked = activeLayer.pixels[epy * activeLayer.width + epx];
                    colorPicker.SetFromCanvas(picked);
                    brushSettings.paintColor = picked;
                    interactionStatus = $"Picked: ({picked.r:F2}, {picked.g:F2}, {picked.b:F2})";
                    Event.current.Use();
                    Repaint();
                }

                // Gradient tool input
                if (activeTool == StudioTool.Gradient && activeLayer != null && !activeLayer.locked
                    && canvasArea.Contains(Event.current.mousePosition))
                {
                    HandleGradientToolInput(canvasArea, paintInputRect, activeLayer);
                }

                // Rectangle/Lasso select input
                if ((activeTool == StudioTool.RectSelect || activeTool == StudioTool.LassoSelect)
                    && activeLayer != null)
                {
                    HandleSelectionToolInput(canvasArea, texRect, activeLayer);
                }

                // Symmetry guide lines
                SymmetryDrawing.DrawGuideLines(canvasArea,
                    layerStack != null ? layerStack.Width : textureSize,
                    layerStack != null ? layerStack.Height : textureSize);

                // Selection overlay
                if (selection != null && selection.HasSelection)
                {
                    SelectionRenderer.DrawSelectionOverlay(canvasArea, selection,
                        layerStack != null ? layerStack.Width : textureSize,
                        layerStack != null ? layerStack.Height : textureSize,
                        canvasZoom, canvasPan);
                }

                // Step 9: Right-click context menu
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1
                    && canvasArea.Contains(Event.current.mousePosition))
                {
                    ShowCanvasContextMenu();
                    Event.current.Use();
                }

                // Island click selection
                if (islandSelectMode && !brushEnabled && islands.Count > 0)
                    HandleIslandClickInput(canvasArea, texRect);
            }
            else
            {
                EditorGUI.LabelField(canvasArea,
                    L("「テクスチャを生成」を押すとプレビューが表示されます", "Click 'Generate' to see preview"),
                    s_emptyCanvasStyle);
            }

            // Canvas overlays
            if (canvasTexture != null)
            {
                // Tiling preview overlay
                TilingPreview.DrawTiledPreview(canvasArea, canvasTexture);

                // Mode badge (top-left)
                {
                    string modeBadge = canvasColorMode == CanvasColorMode.Color
                        ? L("\U0001f3a8 COLOR", "\U0001f3a8 COLOR") : L("\u25d0 MASK", "\u25d0 MASK");
                    Color badgeColor = canvasColorMode == CanvasColorMode.Color
                        ? ColorModeBadgeBg : MaskModeBadgeBg;
                    Rect badgeRect = new Rect(canvasArea.x + 6, canvasArea.y + 6, 70, 20);
                    EditorGUI.DrawRect(badgeRect, badgeColor);
                    EnsureStyles();
                    GUI.Label(badgeRect, modeBadge, s_modeBadgeStyle);
                }

                // Zoom badge (bottom-right)
                ZoomController.DrawCanvasZoomBadge(canvasArea, canvasZoom);

                // Quick mask overlay + badge
                if (quickMask != null && quickMask.IsActive)
                {
                    quickMask.DrawStatusBadge(canvasArea);
                }

                // Toast notifications
                ToastNotification.DrawToasts(canvasArea);

                // Reference image overlay
                if (referenceOverlay != null) referenceOverlay.Draw(canvasArea);
            }

            // Rich status bar
            MaskTextureLayer statusLayer = layerStack != null ? layerStack.ActiveLayer : null;
            Vector2 mousePixelPos = Vector2.zero;
            Color mousePixelColor = Color.clear;
            if (canvasTexture != null && canvasArea.Contains(Event.current.mousePosition) && statusLayer != null)
            {
                float texSize = canvasArea.width * canvasZoom;
                float offsetX = canvasArea.x + (canvasArea.width - texSize) * 0.5f + canvasPan.x;
                float offsetY = canvasArea.y + (canvasArea.height - texSize) * 0.5f + canvasPan.y;
                Rect texRect2 = new Rect(offsetX, offsetY, texSize, texSize);
                Rect statusInputRect = TilingPreview.GetInteractiveTextureRect(canvasArea, texRect2);
                mousePixelPos = BrushCanvasInputHandler.MouseToPixelPos(
                    Event.current.mousePosition, statusInputRect, statusLayer.width, statusLayer.height);
                if (TilingPreview.IsEnabled)
                    mousePixelPos = MaskTextureBrush.WrapPixelPosition(mousePixelPos, statusLayer.width, statusLayer.height);
                int spx = Mathf.Clamp(Mathf.RoundToInt(mousePixelPos.x), 0, statusLayer.width - 1);
                int spy = Mathf.Clamp(Mathf.RoundToInt(mousePixelPos.y), 0, statusLayer.height - 1);
                if (statusLayer.pixels != null)
                    mousePixelColor = statusLayer.pixels[spy * statusLayer.width + spx];
            }
            string toolName = activeTool.ToString();
            string layerName = statusLayer != null ? statusLayer.name : "";
            int tw = layerStack != null ? layerStack.Width : textureSize;
            int th = layerStack != null ? layerStack.Height : textureSize;
            StudioStatusBar.Draw(statusBarRect, toolName, mousePixelPos, tw, th, mousePixelColor,
                canvasZoom * 100f, layerName, ActiveBrushSettings.size, ActiveBrushSettings.opacity,
                interactionStatus);

            EditorGUILayout.EndVertical();
        }

        private bool HandleCanvasInput(Rect canvasArea)
        {
            Event e = Event.current;
            if (!canvasArea.Contains(e.mousePosition) && !isDraggingCanvas) return false;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (MaskTextureShortcutUtility.IsPanEvent(e, shortcutProfile, shortcutState))
                    {
                        isDraggingCanvas = true;
                        lastCanvasMousePos = e.mousePosition;
                        e.Use();
                        return true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (isDraggingCanvas)
                    {
                        canvasPan += e.mousePosition - lastCanvasMousePos;
                        lastCanvasMousePos = e.mousePosition;
                        e.Use();
                        Repaint();
                        return true;
                    }
                    break;
                case EventType.MouseUp:
                    if (isDraggingCanvas)
                    {
                        isDraggingCanvas = false;
                        e.Use();
                        return true;
                    }
                    break;
                case EventType.ScrollWheel:
                    if (canvasArea.Contains(e.mousePosition) && MaskTextureShortcutUtility.IsZoomWheelEvent(e, shortcutProfile))
                    {
                        float zoomDelta = -e.delta.y * 0.05f;
                        ZoomCanvasAt(canvasArea, e.mousePosition, zoomDelta);
                        e.Use();
                        Repaint();
                        return true;
                    }
                    break;
            }

            return false;
        }

        private void HandleGlobalInput(Event e)
        {
            MaskTextureShortcutUtility.UpdateKeyState(e, shortcutProfile, shortcutState);

            // Rebind capture
            if (rebindingActionId != null && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    rebindingActionId = null;
                }
                else
                {
                    var binding = MaskTextureShortcutUtility.FindBinding(shortcutProfile, rebindingActionId);
                    if (binding != null)
                    {
                        binding.keyCode = e.keyCode;
                        binding.ctrl = e.control || e.command;
                        binding.shift = e.shift;
                        binding.alt = e.alt;

                        // Check for duplicates and notify
                        var dup = MaskTextureShortcutUtility.FindDuplicate(shortcutProfile, binding);
                        if (dup != null)
                            ToastNotification.ShowWarning(
                                L($"⚠ キー重複: {dup.displayName}", $"⚠ Key conflict: {dup.displayName}"));
                        else
                            ToastNotification.Show($"{binding.displayName} → {binding.ToDisplayString()}");

                        AutoSaveShortcuts();
                    }
                    rebindingActionId = null;
                }
                e.Use();
                Repaint();
                return;
            }

            if (e == null || e.type != EventType.KeyDown || EditorGUIUtility.editingTextField)
                return;

            // Ctrl/Cmd modified shortcuts
            if (MaskTextureShortcutUtility.IsActionKey(e))
            {
                if (e.keyCode == KeyCode.Z)
                {
                    if (e.shift)
                        RedoBrushStroke();
                    else
                        UndoBrushStroke();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Y)
                {
                    RedoBrushStroke();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.S && !e.shift)
                {
                    SaveProject();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.O && !e.shift)
                {
                    OpenProject();
                    e.Use();
                }
                return;
            }

            // Non-modifier shortcuts (B, E, V, G, M, L, [, ], U, F, number keys etc.)
            if (!e.control && !e.command && !e.alt)
            {
                // Number key → opacity (Photoshop style)
                if (HandleOpacityShortcut(e)) return;

                // Direct tool shortcuts (not in binding system for simplicity)
                switch (e.keyCode)
                {
                    case KeyCode.V:
                        SwitchTool(StudioTool.Move);
                        e.Use(); Repaint(); return;
                    case KeyCode.G:
                        SwitchTool(StudioTool.Fill);
                        e.Use(); Repaint(); return;
                    case KeyCode.M:
                        SwitchTool(StudioTool.RectSelect);
                        e.Use(); Repaint(); return;
                    case KeyCode.Tab:
                        ShowPopupPalette();
                        e.Use(); return;
                    case KeyCode.F1:
                        StudioOnboarding.Open();
                        e.Use(); return;
                    case KeyCode.Q:
                        if (quickMask == null) quickMask = new QuickMaskSystem();
                        int qw = layerStack != null ? layerStack.Width : textureSize;
                        int qh = layerStack != null ? layerStack.Height : textureSize;
                        quickMask.Toggle(qw, qh);
                        ToastNotification.Show(quickMask.IsActive ? L("クイックマスク ON", "Quick Mask ON") : L("クイックマスク OFF", "Quick Mask OFF"));
                        e.Use(); return;
                }

                // Check shortcut bindings
                foreach (var binding in shortcutProfile.bindings)
                {
                    if (!MaskTextureShortcutUtility.MatchesBinding(e, binding)) continue;

                    switch (binding.actionId)
                    {
                        case "ToolBrush":
                            SwitchTool(StudioTool.Brush);
                            e.Use(); return;
                        case "ToolEraser":
                            SwitchTool(StudioTool.Eraser);
                            e.Use(); return;
                        case "ToggleBrush":
                            brushEnabled = !brushEnabled;
                            if (brushEnabled) islandSelectMode = false;
                            e.Use(); Repaint(); return;
                        case "BrushSizeUp":
                            ActiveBrushSettings.size = Mathf.Clamp(ActiveBrushSettings.size + 2f, 1f, 100f);
                            e.Use(); Repaint(); return;
                        case "BrushSizeDown":
                            ActiveBrushSettings.size = Mathf.Clamp(ActiveBrushSettings.size - 2f, 1f, 100f);
                            e.Use(); Repaint(); return;
                        case "ToggleUV":
                            showUVWireframe = !showUVWireframe;
                            e.Use(); Repaint(); return;
                        case "FitCanvas":
                            canvasZoom = 1f; canvasPan = Vector2.zero;
                            e.Use(); Repaint(); return;
                        case "SwapColors":
                            colorPicker.SwapColors();
                            brushSettings.paintColor = colorPicker.ForegroundColor;
                            brushSettings.backgroundColor = colorPicker.BackgroundColor;
                            e.Use(); Repaint(); return;
                        case "Eyedropper":
                            SwitchTool(StudioTool.Eyedropper);
                            e.Use(); Repaint(); return;
                        case "DefaultColors":
                            colorPicker.ResetDefaults();
                            brushSettings.paintColor = colorPicker.ForegroundColor;
                            brushSettings.backgroundColor = colorPicker.BackgroundColor;
                            e.Use(); Repaint(); return;
                        case "CommandPalette":
                            OpenCommandPalette();
                            e.Use(); return;
                    }
                }
            }

            // Ctrl+Shift+P → Command Palette
            if ((e.control || e.command) && e.shift && e.keyCode == KeyCode.P)
            {
                OpenCommandPalette();
                e.Use(); return;
            }

            // Shift+number → strength
            if (e.shift && !e.control && !e.command && !e.alt)
            {
                if (HandleStrengthShortcut(e)) return;
            }
        }

        private void OpenCommandPalette()
        {
            var commands = StudioCommandPalette.CreateDefaultCommands(
                toolId => {
                    switch (toolId)
                    {
                        case "Brush": SwitchTool(StudioTool.Brush); break;
                        case "Eraser": SwitchTool(StudioTool.Eraser); break;
                        case "Fill": SwitchTool(StudioTool.Fill); break;
                        case "Eyedropper": SwitchTool(StudioTool.Eyedropper); break;
                        case "Gradient": SwitchTool(StudioTool.Gradient); break;
                        case "Move": SwitchTool(StudioTool.Move); break;
                    }
                },
                filterId => { /* filter application - can integrate later */ },
                layerAction => {
                    if (layerStack == null) return;
                    switch (layerAction)
                    {
                        case "add": layerStack.AddLayer($"Layer {layerStack.Layers.Count + 1}"); RefreshPreviewFromLayers(); break;
                        case "duplicate": layerStack.DuplicateLayer(layerStack.ActiveLayerIndex); RefreshPreviewFromLayers(); break;
                        case "merge": layerStack.MergeDown(layerStack.ActiveLayerIndex); RefreshPreviewFromLayers(); break;
                        case "mask.add": if (layerStack.ActiveLayer != null) layerStack.ActiveLayer.CreateMask(); break;
                    }
                });
            StudioCommandPalette.Open(commands);
        }

        /// <summary>
        /// Photoshop-style number key → opacity shortcut.
        /// 数字キーで不透明度設定 (1=10%, 2=20%... 0=100%, 300ms以内に2桁で精密値)
        /// </summary>
        private bool HandleOpacityShortcut(Event e)
        {
            int digit = KeyCodeToDigit(e.keyCode);
            if (digit < 0) return false;

            float now = Time.realtimeSinceStartup;

            if (waitingForSecondDigit && (now - lastDigitTime) < 0.3f)
            {
                // Second digit: combine with first for precise value (e.g., 1→5 = 15%)
                int value = firstDigit * 10 + digit;
                ActiveBrushSettings.opacity = Mathf.Clamp01(value / 100f);
                waitingForSecondDigit = false;
                interactionStatus = L("不透明度", "Opacity") + $": {ActiveBrushSettings.opacity:P0}";
            }
            else
            {
                // First digit: set coarse value, wait for second
                firstDigit = digit;
                lastDigitTime = now;
                waitingForSecondDigit = true;
                // Immediately apply coarse value
                float coarse = digit == 0 ? 1f : digit * 0.1f;
                ActiveBrushSettings.opacity = coarse;
                interactionStatus = L("不透明度", "Opacity") + $": {coarse:P0} " + L("(もう1桁で精密値)", "(press another digit for precise)");
            }
            e.Use(); Repaint();
            return true;
        }

        private bool HandleStrengthShortcut(Event e)
        {
            int digit = KeyCodeToDigit(e.keyCode);
            if (digit < 0) return false;
            ActiveBrushSettings.strength = digit == 0 ? 1f : digit * 0.1f;
            interactionStatus = L("筆圧", "Strength") + $": {ActiveBrushSettings.strength:P0}";
            e.Use(); Repaint();
            return true;
        }

        private static int KeyCodeToDigit(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Alpha0: case KeyCode.Keypad0: return 0;
                case KeyCode.Alpha1: case KeyCode.Keypad1: return 1;
                case KeyCode.Alpha2: case KeyCode.Keypad2: return 2;
                case KeyCode.Alpha3: case KeyCode.Keypad3: return 3;
                case KeyCode.Alpha4: case KeyCode.Keypad4: return 4;
                case KeyCode.Alpha5: case KeyCode.Keypad5: return 5;
                case KeyCode.Alpha6: case KeyCode.Keypad6: return 6;
                case KeyCode.Alpha7: case KeyCode.Keypad7: return 7;
                case KeyCode.Alpha8: case KeyCode.Keypad8: return 8;
                case KeyCode.Alpha9: case KeyCode.Keypad9: return 9;
                default: return -1;
            }
        }

        /// <summary>Show the on-canvas popup palette.</summary>
        private void ShowPopupPalette()
        {
            var palette = new CanvasPopupPalette(ActiveBrushSettings, (toolIndex) =>
            {
                switch (toolIndex)
                {
                    case 0: SwitchTool(StudioTool.Brush); break;
                    case 1: SwitchTool(StudioTool.Eraser); break;
                    case 2: SwitchTool(StudioTool.Fill); break;
                }
            });
            PopupWindow.Show(new Rect(Event.current.mousePosition, Vector2.one), palette);
        }

        /// <summary>
        /// Step 9: Right-click context menu on canvas.
        /// キャンバス上の右クリックコンテキストメニュー
        /// </summary>
        private void ShowCanvasContextMenu()
        {
            var menu = new GenericMenu();

            // Brush size presets
            menu.AddItem(new GUIContent(L("サイズ/5px", "Size/5px")), false, () => { ActiveBrushSettings.size = 5f; Repaint(); });
            menu.AddItem(new GUIContent(L("サイズ/10px", "Size/10px")), false, () => { ActiveBrushSettings.size = 10f; Repaint(); });
            menu.AddItem(new GUIContent(L("サイズ/20px", "Size/20px")), false, () => { ActiveBrushSettings.size = 20f; Repaint(); });
            menu.AddItem(new GUIContent(L("サイズ/50px", "Size/50px")), false, () => { ActiveBrushSettings.size = 50f; Repaint(); });

            menu.AddSeparator("");

            // Tool switch
            menu.AddItem(new GUIContent(L("ブラシ (B)", "Brush (B)")),
                activeTool == StudioTool.Brush, () => SwitchTool(StudioTool.Brush));
            menu.AddItem(new GUIContent(L("消しゴム (E)", "Eraser (E)")),
                activeTool == StudioTool.Eraser, () => SwitchTool(StudioTool.Eraser));
            menu.AddItem(new GUIContent(L("スポイト (I)", "Eyedropper (I)")),
                activeTool == StudioTool.Eyedropper, () => SwitchTool(StudioTool.Eyedropper));
            menu.AddItem(new GUIContent(L("グラデーション", "Gradient")),
                activeTool == StudioTool.Gradient, () => SwitchTool(StudioTool.Gradient));

            menu.ShowAsContext();
        }

        // ===== Tool Input Handlers =====

        private void EnsureSelection()
        {
            int w = layerStack != null ? layerStack.Width : textureSize;
            int h = layerStack != null ? layerStack.Height : textureSize;
            if (selection == null || selection.Width != w || selection.Height != h)
                selection = new MaskTextureSelection(w, h);
        }

        private void HandleFillToolInput(Rect canvasArea, Rect texRect, MaskTextureLayer layer, bool wrapCoordinates)
        {
            Event e = Event.current;
            Vector2 pixelPos = BrushCanvasInputHandler.MouseToPixelPos(e.mousePosition, texRect, layer.width, layer.height);
            if (wrapCoordinates)
                pixelPos = MaskTextureBrush.WrapPixelPosition(pixelPos, layer.width, layer.height);
            int px = Mathf.RoundToInt(pixelPos.x);
            int py = Mathf.RoundToInt(pixelPos.y);

            Color[] beforePixels = MaskTextureHistory.ClonePixels(layer.pixels);
            if (fillSettings.colorMode)
            {
                MaskTextureFillTool.FloodFill(layer.pixels, layer.width, layer.height,
                    px, py, fillSettings.fillColor,
                    fillSettings.tolerance, fillSettings.contiguous);
            }
            else
            {
                MaskTextureFillTool.FloodFill(layer.pixels, layer.width, layer.height,
                    px, py, fillSettings.fillValue, fillSettings.fillAlpha,
                    fillSettings.tolerance, fillSettings.contiguous);
            }
            Color[] afterPixels = MaskTextureHistory.ClonePixels(layer.pixels);
            brushHistory.Record(layer, beforePixels, afterPixels, "Fill");
            layer.sourceType = MaskTextureLayer.SourceType.Paint;
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
            interactionStatus = L("塗りつぶし", "Fill") + $" ({px}, {py})";
            e.Use();
            Repaint();
        }

        private void HandleGradientToolInput(Rect canvasArea, Rect texRect, MaskTextureLayer layer)
        {
            Event e = Event.current;
            Vector2 pixelPos = BrushCanvasInputHandler.MouseToPixelPos(e.mousePosition, texRect, layer.width, layer.height);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        GradientTool.BeginDrag(pixelPos, layer.pixels);
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (e.button == 0 && GradientTool.IsDragging)
                    {
                        GradientTool.UpdateDrag(pixelPos);
                        e.Use();
                        Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (e.button == 0 && GradientTool.IsDragging)
                    {
                        Color[] beforePixels = MaskTextureHistory.ClonePixels(layer.pixels);
                        GradientTool.EndDrag(layer.pixels, layer.width, layer.height,
                            colorPicker.ForegroundColor, colorPicker.BackgroundColor);
                        Color[] afterPixels = MaskTextureHistory.ClonePixels(layer.pixels);
                        brushHistory.Record(layer, beforePixels, afterPixels, "Gradient");
                        layer.sourceType = MaskTextureLayer.SourceType.Paint;
                        layerStack.InvalidateFlattenCache();
                        RefreshPreviewFromLayers();
                        interactionStatus = L("グラデーション適用", "Gradient applied");
                        e.Use();
                        Repaint();
                    }
                    break;
            }
        }

        private void HandleMoveToolInput(Rect canvasArea, Rect texRect, MaskTextureLayer layer)
        {
            Event e = Event.current;
            if (!canvasArea.Contains(e.mousePosition) && !isMoveDragging) return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && canvasArea.Contains(e.mousePosition))
                    {
                        isMoveDragging = true;
                        moveDragStartMouse = e.mousePosition;
                        moveDragStartOffset = layer.transformOffset;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (isMoveDragging)
                    {
                        Vector2 delta = e.mousePosition - moveDragStartMouse;
                        // Convert screen delta to UV offset
                        float texDisplaySize = texRect.width;
                        if (texDisplaySize > 0)
                        {
                            layer.transformOffset = moveDragStartOffset + new Vector2(
                                delta.x / texDisplaySize,
                                -delta.y / texDisplaySize);
                        }
                        if (liveLink != null && liveLink.IsEnabled)
                        {
                            RequestPreviewRefresh(true, false);
                            RequestLiveLinkSync(false);
                        }
                        else
                            RequestPreviewRefresh(true, false);
                        interactionStatus = L("移動", "Move") + $": ({layer.transformOffset.x:F3}, {layer.transformOffset.y:F3})";
                        e.Use();
                        Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (isMoveDragging)
                    {
                        isMoveDragging = false;
                        FlushPendingPreviewRefresh(true, true);
                        interactionStatus = L("移動完了", "Move complete");
                        e.Use();
                        Repaint();
                    }
                    break;
            }
        }

        private void HandleSelectionToolInput(Rect canvasArea, Rect texRect, MaskTextureLayer layer)
        {
            Event e = Event.current;
            if (!canvasArea.Contains(e.mousePosition) && !isSelectDragging) return;
            EnsureSelection();

            if (activeTool == StudioTool.RectSelect)
            {
                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (e.button == 0 && canvasArea.Contains(e.mousePosition))
                        {
                            isSelectDragging = true;
                            selectDragStart = e.mousePosition;
                            e.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (isSelectDragging)
                        {
                            SelectionRenderer.DrawRectSelectionPreview(selectDragStart, e.mousePosition);
                            e.Use();
                            Repaint();
                        }
                        break;
                    case EventType.MouseUp:
                        if (isSelectDragging)
                        {
                            isSelectDragging = false;
                            // Convert screen coords to pixel coords
                            Vector2 p1 = BrushCanvasInputHandler.MouseToPixelPos(selectDragStart, texRect, layer.width, layer.height);
                            Vector2 p2 = BrushCanvasInputHandler.MouseToPixelPos(e.mousePosition, texRect, layer.width, layer.height);
                            SelectionMode mode = e.shift ? SelectionMode.Add : (e.alt ? SelectionMode.Subtract : SelectionMode.Replace);
                            selection.SelectRect(
                                Mathf.RoundToInt(Mathf.Min(p1.x, p2.x)), Mathf.RoundToInt(Mathf.Min(p1.y, p2.y)),
                                Mathf.RoundToInt(Mathf.Max(p1.x, p2.x)), Mathf.RoundToInt(Mathf.Max(p1.y, p2.y)),
                                mode);
                            interactionStatus = selection.HasSelection ? L("選択中", "Selection active") : L("選択なし", "No selection");
                            e.Use();
                            Repaint();
                        }
                        break;
                }
            }
            else if (activeTool == StudioTool.LassoSelect)
            {
                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (e.button == 0 && canvasArea.Contains(e.mousePosition))
                        {
                            isSelectDragging = true;
                            lassoPoints.Clear();
                            lassoPoints.Add(BrushCanvasInputHandler.MouseToPixelPos(e.mousePosition, texRect, layer.width, layer.height));
                            e.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (isSelectDragging)
                        {
                            lassoPoints.Add(BrushCanvasInputHandler.MouseToPixelPos(e.mousePosition, texRect, layer.width, layer.height));
                            // Draw preview in screen space
                            var screenPoints = new List<Vector2>();
                            foreach (var p in lassoPoints)
                                screenPoints.Add(BrushCanvasInputHandler.PixelToCanvasPos(p, texRect, layer.width, layer.height));
                            SelectionRenderer.DrawLassoPreview(screenPoints);
                            e.Use();
                            Repaint();
                        }
                        break;
                    case EventType.MouseUp:
                        if (isSelectDragging)
                        {
                            isSelectDragging = false;
                            SelectionMode mode = e.shift ? SelectionMode.Add : (e.alt ? SelectionMode.Subtract : SelectionMode.Replace);
                            selection.SelectLasso(lassoPoints, mode);
                            lassoPoints.Clear();
                            interactionStatus = selection.HasSelection ? L("投げ縄選択中", "Lasso selection active") : L("選択なし", "No selection");
                            e.Use();
                            Repaint();
                        }
                        break;
                }
            }
        }

        // ===== Filter Preview =====

        private void StartFilterPreview(System.Action<Color[], int, int> filterAction)
        {
            var layer = layerStack?.ActiveLayer;
            if (layer?.pixels == null) return;

            if (!filterPreviewActive)
            {
                filterPreviewBackup = new Color[layer.pixels.Length];
                System.Array.Copy(layer.pixels, filterPreviewBackup, layer.pixels.Length);
            }

            filterPreviewPixels = new Color[layer.pixels.Length];
            System.Array.Copy(filterPreviewBackup ?? layer.pixels, filterPreviewPixels, layer.pixels.Length);
            filterAction(filterPreviewPixels, layer.width, layer.height);

            // Temporarily show preview
            System.Array.Copy(filterPreviewPixels, layer.pixels, layer.pixels.Length);
            filterPreviewActive = true;
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
        }

        private void ApplyFilterPreview()
        {
            if (!filterPreviewActive) return;
            var layer = layerStack?.ActiveLayer;
            if (layer?.pixels == null || filterPreviewBackup == null) return;

            brushHistory.Record(layer, filterPreviewBackup, MaskTextureHistory.ClonePixels(layer.pixels), "Filter");
            filterPreviewActive = false;
            filterPreviewBackup = null;
            filterPreviewPixels = null;
        }

        private void CancelFilterPreview()
        {
            if (!filterPreviewActive) return;
            var layer = layerStack?.ActiveLayer;
            if (layer?.pixels != null && filterPreviewBackup != null)
            {
                System.Array.Copy(filterPreviewBackup, layer.pixels, layer.pixels.Length);
                layerStack.InvalidateFlattenCache();
                RefreshPreviewFromLayers();
            }
            filterPreviewActive = false;
            filterPreviewBackup = null;
            filterPreviewPixels = null;
        }

        private void HandleCanvasShortcuts(Rect canvasArea, Rect textureRect, MaskTextureLayer activeLayer, bool wrapCoordinates)
        {
            if (HandleBrushAdjustShortcut(canvasArea))
                return;

            if (HandleCanvasInput(canvasArea))
                return;

            if (!brushEnabled || activeLayer == null || activeLayer.pixels == null || activeLayer.locked)
                return;

            if (HandlePickerShortcut(canvasArea, textureRect, activeLayer, wrapCoordinates))
                return;

            HandleLineShortcut(canvasArea, textureRect, activeLayer, wrapCoordinates);
        }

        private bool HandleBrushAdjustShortcut(Rect canvasArea)
        {
            Event e = Event.current;
            if ((!canvasArea.Contains(e.mousePosition) && !shortcutState.isAdjustingBrush) || e == null)
                return false;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (MaskTextureShortcutUtility.IsBrushAdjustStart(e, shortcutProfile))
                    {
                        shortcutState.isAdjustingBrush = true;
                        shortcutState.adjustStartMousePosition = e.mousePosition;
                        shortcutState.adjustStartSize = ActiveBrushSettings.size;
                        shortcutState.adjustStartOpacity = ActiveBrushSettings.opacity;
                        interactionStatus = L("ブラシ調整: X=サイズ / Y=不透明度", "Adjust Brush: X=Size / Y=Opacity");
                        e.Use();
                        Repaint();
                        return true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (shortcutState.isAdjustingBrush)
                    {
                        Vector2 delta = e.mousePosition - shortcutState.adjustStartMousePosition;
                        ActiveBrushSettings.size = Mathf.Clamp(
                            shortcutState.adjustStartSize + delta.x * shortcutProfile.brushSizeDragSensitivity,
                            1f, 100f);
                        ActiveBrushSettings.opacity = Mathf.Clamp01(
                            shortcutState.adjustStartOpacity - delta.y * shortcutProfile.brushOpacityDragSensitivity);
                        e.Use();
                        Repaint();
                        return true;
                    }
                    break;
                case EventType.MouseUp:
                    if (shortcutState.isAdjustingBrush && e.button == 1)
                    {
                        shortcutState.isAdjustingBrush = false;
                        interactionStatus = L("ブラシ", "Brush") + $": {ActiveBrushSettings.size:F0}px / " + L("不透明度", "Opacity") + $" {ActiveBrushSettings.opacity:P0}";
                        e.Use();
                        Repaint();
                        return true;
                    }
                    break;
            }

            return false;
        }

        private bool HandlePickerShortcut(Rect canvasArea, Rect textureRect, MaskTextureLayer activeLayer, bool wrapCoordinates)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || !canvasArea.Contains(e.mousePosition))
                return false;

            if (!MaskTextureShortcutUtility.IsPickerEvent(e, shortcutProfile))
                return false;

            Vector2 pixelPosition = BrushCanvasInputHandler.MouseToPixelPos(
                e.mousePosition, textureRect, activeLayer.width, activeLayer.height);
            Vector2 sampledPixelPosition = wrapCoordinates
                ? MaskTextureBrush.WrapPixelPosition(pixelPosition, activeLayer.width, activeLayer.height)
                : pixelPosition;
            Color sampled = activeLayer.GetPixel(Mathf.RoundToInt(sampledPixelPosition.x), Mathf.RoundToInt(sampledPixelPosition.y));
            ActiveBrushSettings.strength = sampled.r;
            ActiveBrushSettings.paintAlpha = sampled.a;
            SetLineAnchor(pixelPosition);
            interactionStatus = L("取得値", "Picked Value") + $": {sampled.r:F2} / " + L("アルファ", "Alpha") + $" {sampled.a:F2}";
            e.Use();
            Repaint();
            return true;
        }

        private bool HandleLineShortcut(Rect canvasArea, Rect textureRect, MaskTextureLayer activeLayer, bool wrapCoordinates)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || !canvasArea.Contains(e.mousePosition))
                return false;

            if (!MaskTextureShortcutUtility.IsLineEvent(e, shortcutProfile))
                return false;

            Vector2 pixelPosition = BrushCanvasInputHandler.MouseToPixelPos(
                e.mousePosition, textureRect, activeLayer.width, activeLayer.height);

            if (!shortcutState.hasLineAnchor)
            {
                SetLineAnchor(pixelPosition);
                interactionStatus = L("ラインアンカー設定", "Line anchor set");
                e.Use();
                Repaint();
                return true;
            }

            Color[] beforePixels = MaskTextureHistory.ClonePixels(activeLayer.pixels);
            brush.PaintStraightLine(
                shortcutState.lineAnchorPixel,
                pixelPosition,
                activeLayer.pixels,
                activeLayer.width,
                activeLayer.height,
                activeLayer.lockTransparentPixels,
                1f,
                wrapCoordinates);
            Color[] afterPixels = MaskTextureHistory.ClonePixels(activeLayer.pixels);
            brushHistory.Record(activeLayer, beforePixels, afterPixels, "Line Stroke");
            activeLayer.sourceType = MaskTextureLayer.SourceType.Paint;
            SetLineAnchor(pixelPosition);
            interactionStatus = L("ラインストローク", "Line stroke");
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
            e.Use();
            Repaint();
            return true;
        }

        private void DrawBrushAdjustOverlay(Vector2 mousePosition)
        {
            Rect overlayRect = new Rect(mousePosition.x + 16f, mousePosition.y + 16f, 170f, 42f);
            GUI.Box(overlayRect, GUIContent.none, EditorStyles.helpBox);
            GUI.Label(
                new Rect(overlayRect.x + 8f, overlayRect.y + 6f, overlayRect.width - 16f, 16f),
                L("サイズ", "Size") + $": {ActiveBrushSettings.size:F0}px",
                EditorStyles.miniBoldLabel);
            GUI.Label(
                new Rect(overlayRect.x + 8f, overlayRect.y + 22f, overlayRect.width - 16f, 16f),
                L("不透明度", "Opacity") + $": {ActiveBrushSettings.opacity:P0}",
                EditorStyles.miniLabel);
        }

        private void SetLineAnchor(Vector2 pixelPosition)
        {
            shortcutState.hasLineAnchor = true;
            shortcutState.lineAnchorPixel = pixelPosition;
        }

        // ===== Project Save/Load =====

        private void SaveProject()
        {
            string path = currentProjectPath;
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel(
                    L("プロジェクトを保存", "Save Project"),
                    "Assets",
                    "untitled",
                    "nataneTex");
                if (string.IsNullOrEmpty(path)) return;
            }

            MaskTextureProjectFile.Save(path, layerStack,
                canvasZoom, canvasPan, showUVWireframe,
                (int)activeTool, (int)currentTab,
                brushSettings, eraserSettings, fillSettings);
            currentProjectPath = path;
            UpdateWindowTitle();
            autoSave?.MarkSaved();
            ToastNotification.ShowSuccess(L("保存完了", "Saved"));
            interactionStatus = L("保存しました", "Saved") + ": " + System.IO.Path.GetFileName(path);
            Repaint();
        }

        private void SaveProjectToPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            MaskTextureProjectFile.Save(path, layerStack,
                canvasZoom, canvasPan, showUVWireframe,
                (int)activeTool, (int)currentTab,
                brushSettings, eraserSettings, fillSettings);
            currentProjectPath = path;
            UpdateWindowTitle();
            autoSave?.MarkSaved();
        }

        private void OpenProject()
        {
            string path = EditorUtility.OpenFilePanel(
                L("プロジェクトを開く", "Open Project"),
                "Assets",
                "nataneTex");
            if (string.IsNullOrEmpty(path)) return;

            var data = MaskTextureProjectFile.Load(path);
            if (data == null) return;

            layerStack = data.layerStack;
            canvasZoom = data.canvasZoom;
            canvasPan = data.canvasPan;
            showUVWireframe = data.showUVWireframe;
            activeTool = (StudioTool)data.activeTool;
            currentTab = (GeneratorTab)data.currentTab;
            brushSettings = data.brushSettings ?? brushSettings;
            eraserSettings = data.eraserSettings ?? eraserSettings;
            fillSettings = data.fillSettings ?? fillSettings;
            brush = new MaskTextureBrush(ActiveBrushSettings);
            brushHistory = new MaskTextureHistory();
            currentProjectPath = path;
            UpdateWindowTitle();
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
            interactionStatus = L("開きました", "Opened") + ": " + System.IO.Path.GetFileName(path);
            Repaint();
        }

        private void UpdateWindowTitle()
        {
            string baseName = L("テクスチャスタジオ", "Texture Studio");
            if (!string.IsNullOrEmpty(currentProjectPath))
                titleContent = new GUIContent(baseName + " - " + System.IO.Path.GetFileName(currentProjectPath));
            else
                titleContent = new GUIContent(baseName);
        }

        private void UndoBrushStroke()
        {
            if (brushHistory != null && brushHistory.Undo(layerStack))
            {
                interactionStatus = L("ブラシストロークを元に戻しました", "Undo Brush Stroke");
                layerStack.InvalidateFlattenCache();
                RefreshPreviewFromLayers();
            }
        }

        private void RedoBrushStroke()
        {
            if (brushHistory != null && brushHistory.Redo(layerStack))
            {
                interactionStatus = L("ブラシストロークをやり直しました", "Redo Brush Stroke");
                layerStack.InvalidateFlattenCache();
                RefreshPreviewFromLayers();
            }
        }

        private void ZoomCanvasAt(Rect canvasArea, Vector2 mousePosition, float zoomDelta)
        {
            float oldZoom = canvasZoom;
            float newZoom = Mathf.Clamp(canvasZoom + zoomDelta * canvasZoom, 0.1f, 10f);
            if (Mathf.Approximately(oldZoom, newZoom))
                return;

            Vector2 canvasCenter = canvasArea.center;
            Vector2 offsetFromTextureCenter = mousePosition - canvasCenter - canvasPan;
            float ratio = newZoom / oldZoom;
            canvasPan += offsetFromTextureCenter * (1f - ratio);
            canvasZoom = newZoom;
            interactionStatus = L("ズーム", "Zoom") + $": {canvasZoom:F1}x";
        }

        /// <summary>
        /// Draw a checkerboard pattern for transparency visualization.
        /// 透過表示用のチェッカーボードパターンを描画
        /// </summary>
        private static void DrawCheckerboard(Rect rect, int cellSize)
        {
            Color light = CheckerLight;
            Color dark = CheckerDark;
            int cols = Mathf.CeilToInt(rect.width / cellSize);
            int rows = Mathf.CeilToInt(rect.height / cellSize);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    bool isDark = (r + c) % 2 == 0;
                    float x = rect.x + c * cellSize;
                    float y = rect.y + r * cellSize;
                    float w = Mathf.Min(cellSize, rect.xMax - x);
                    float h = Mathf.Min(cellSize, rect.yMax - y);
                    EditorGUI.DrawRect(new Rect(x, y, w, h), isDark ? dark : light);
                }
            }
        }

        // ================================================================
        // Filter Panel
        // ================================================================

        private void DrawFilterPanel()
        {
            filterFoldout = NataneToonShaderGUIUtility.DrawFoldoutHeader(L("フィルター", "Filters"), filterFoldout);
            if (!filterFoldout) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool hasLayer = layerStack != null && layerStack.ActiveLayer != null
                && layerStack.ActiveLayer.pixels != null;

            // Filter preview controls
            if (filterPreviewActive)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(L("プレビュー中", "Preview Active"), EditorStyles.boldLabel);
                if (GUILayout.Button(L("確定", "Apply"), GUILayout.Width(50)))
                    ApplyFilterPreview();
                if (GUILayout.Button(L("取消", "Cancel"), GUILayout.Width(50)))
                    CancelFilterPreview();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            // Gaussian Blur
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            filterBlurSigma = EditorGUILayout.Slider(L("ブラー (sigma)", "Blur (sigma)"), filterBlurSigma, 0.1f, 20f);
            bool blurChanged = EditorGUI.EndChangeCheck();
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button(L("プレビュー", "Preview"), GUILayout.Width(60)))
            {
                StartFilterPreview((pixels, w, h) =>
                    MaskTextureFilters.GaussianBlur(pixels, w, h, filterBlurSigma));
            }
            if (GUILayout.Button(L("適用", "Apply"), GUILayout.Width(50)))
            {
                CancelFilterPreview();
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    MaskTextureFilters.GaussianBlur(pixels, w, h, filterBlurSigma));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Levels
            EditorGUILayout.LabelField(L("レベル補正", "Levels"), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            filterLevelInputMin = EditorGUILayout.FloatField(L("入力Min", "Input Min"), filterLevelInputMin, GUILayout.Width(155));
            filterLevelInputMax = EditorGUILayout.FloatField(L("入力Max", "Input Max"), filterLevelInputMax, GUILayout.Width(155));
            EditorGUILayout.EndHorizontal();
            filterLevelGamma = EditorGUILayout.Slider(L("ガンマ", "Gamma"), filterLevelGamma, 0.01f, 10f);
            EditorGUILayout.BeginHorizontal();
            filterLevelOutputMin = EditorGUILayout.FloatField(L("出力Min", "Output Min"), filterLevelOutputMin, GUILayout.Width(155));
            filterLevelOutputMax = EditorGUILayout.FloatField(L("出力Max", "Output Max"), filterLevelOutputMax, GUILayout.Width(155));
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button(L("適用", "Apply"), GUILayout.Width(50)))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    MaskTextureFilters.Levels(pixels, w, h,
                        filterLevelInputMin, filterLevelInputMax, filterLevelGamma,
                        filterLevelOutputMin, filterLevelOutputMax));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Edge Detection
            EditorGUILayout.BeginHorizontal();
            filterEdgeStrength = EditorGUILayout.Slider(L("エッジ検出", "Edge Detection"), filterEdgeStrength, 0.1f, 5f);
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button(L("適用", "Apply"), GUILayout.Width(50)))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    MaskTextureFilters.SobelEdge(pixels, w, h, filterEdgeStrength));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Sharpen
            EditorGUILayout.BeginHorizontal();
            filterSharpenAmount = EditorGUILayout.Slider(L("シャープ", "Sharpen"), filterSharpenAmount, 0f, 3f);
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button(L("適用", "Apply"), GUILayout.Width(50)))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    MaskTextureFilters.Sharpen(pixels, w, h, filterSharpenAmount, filterSharpenSigma));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Threshold
            EditorGUILayout.BeginHorizontal();
            filterThreshold = EditorGUILayout.Slider(L("二値化", "Threshold"), filterThreshold, 0f, 1f);
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button(L("適用", "Apply"), GUILayout.Width(50)))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    MaskTextureFilters.Threshold(pixels, w, h, filterThreshold));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // === Color Adjustment Filters (Phase 4) ===
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(L("カラー調整", "Color Adjustments"), EditorStyles.miniLabel);

            // HSL Adjust
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button(L("色相/彩度/明度", "HSL Adjust"), EditorStyles.miniButton))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    ColorAdjustmentFilters.HSLAdjust(pixels, w, h, 0f, 1f, 1f));
            }
            if (GUILayout.Button(L("彩度除去", "Desaturate"), EditorStyles.miniButton))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    ColorAdjustmentFilters.Desaturate(pixels, w, h));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button(L("ポスタリゼーション", "Posterize"), EditorStyles.miniButton))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    ColorAdjustmentFilters.Posterize(pixels, w, h, 8));
            }
            if (GUILayout.Button(L("自然な彩度", "Vibrance"), EditorStyles.miniButton))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    ColorAdjustmentFilters.Vibrance(pixels, w, h, 0.5f));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplyFilterToActiveLayer(System.Action<Color[], int, int> filterAction)
        {
            if (layerStack == null || layerStack.ActiveLayer == null) return;
            var layer = layerStack.ActiveLayer;
            if (layer.pixels == null || layer.locked) return;

            filterAction(layer.pixels, layer.width, layer.height);
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
        }


        // ================================================================
        // Helpers
        // ================================================================

        private void DrawMeshSourceField()
        {
            EditorGUI.BeginChangeCheck();
            meshSource = EditorGUILayout.ObjectField(L("メッシュソース", "Mesh Source"), meshSource, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
                InvalidateUvCache();

            uvChannel = EditorGUILayout.IntPopup(L("UVチャンネル", "UV Channel"), uvChannel,
                new[] { "UV0", "UV1", "UV2", "UV3" }, new[] { 0, 1, 2, 3 });
        }

        /// <summary>
        /// Import a texture for editing from external caller (e.g., ShaderGUI Edit button).
        /// 外部呼び出し元（ShaderGUI Editボタン等）からテクスチャをインポートして編集
        /// </summary>
        internal void ImportTextureForEditing(Texture2D texture, string propertyName)
        {
            LoadTextureForEditing(texture, propertyName, false);
        }

        private static string GetLiveSourceLayerName(string propertyName)
        {
            return string.IsNullOrEmpty(propertyName)
                ? "LIVE Source"
                : $"LIVE Source ({propertyName})";
        }

        private MaskTextureLayer GetLiveSourceLayer(string propertyName)
        {
            if (layerStack == null || layerStack.Layers == null || layerStack.Layers.Count == 0)
                return null;

            string expectedName = GetLiveSourceLayerName(propertyName);
            for (int i = 0; i < layerStack.Layers.Count; i++)
            {
                MaskTextureLayer layer = layerStack.Layers[i];
                if (layer == null || layer.sourceType != MaskTextureLayer.SourceType.Import)
                    continue;

                if (layer.name == expectedName)
                    return layer;
            }

            return null;
        }

        private void ResetEditingStackForTexture(Texture2D texture)
        {
            layerStack = new MaskLayerStack(texture.width, texture.height);
            brushHistory = new MaskTextureHistory();
            selection?.Clear();
            canvasPan = Vector2.zero;
            canvasZoom = 1f;
        }

        private MaskTextureLayer CreateImportedLayer(string layerName, Texture2D texture, int width, int height)
        {
            var layer = new MaskTextureLayer(layerName, width, height);
            layer.ImportFromTexture(texture);
            layer.sourceType = MaskTextureLayer.SourceType.Import;
            return layer;
        }

        private void EnsureLiveSourceTextureLayer(Texture2D texture, string propertyName, bool replaceLayers)
        {
            if (texture == null)
                return;

            CurrentPropertyName = propertyName;

            if (replaceLayers || layerStack == null || layerStack.Layers.Count == 0)
            {
                ResetEditingStackForTexture(texture);
                layerStack.AddLayer(CreateImportedLayer(GetLiveSourceLayerName(propertyName), texture, texture.width, texture.height));
                layerStack.AddLayer(L("Paint Layer", "Paint Layer"), texture.width, texture.height);
                return;
            }

            EnsureLayerStack();
            MaskTextureLayer existingSourceLayer = GetLiveSourceLayer(propertyName);
            if (existingSourceLayer != null)
            {
                existingSourceLayer.ImportFromTexture(texture);
                existingSourceLayer.sourceType = MaskTextureLayer.SourceType.Import;
                return;
            }

            int width = layerStack.Width;
            int height = layerStack.Height;
            int previousActiveIndex = Mathf.Clamp(layerStack.ActiveLayerIndex, 0, Mathf.Max(0, layerStack.Layers.Count - 1));
            layerStack.Layers.Insert(0, CreateImportedLayer(GetLiveSourceLayerName(propertyName), texture, width, height));
            layerStack.ActiveLayerIndex = previousActiveIndex + 1;
        }

        private void LoadTextureForEditing(Texture2D texture, string propertyName, bool replaceLayers)
        {
            if (texture == null) return;

            if (replaceLayers || layerStack == null)
            {
                layerStack = new MaskLayerStack(texture.width, texture.height);
                brushHistory = new MaskTextureHistory();
                selection?.Clear();
                canvasPan = Vector2.zero;
                canvasZoom = 1f;
            }
            else
            {
                // Ensure layer stack exists
                EnsureLayerStack();
            }
            CurrentPropertyName = propertyName;

            // Create a new layer with the imported texture
            var layer = replaceLayers
                ? layerStack.AddLayer(propertyName ?? texture.name, texture.width, texture.height)
                : layerStack.AddLayer(propertyName ?? texture.name);
            layer.ImportFromTexture(texture);
            layer.sourceType = MaskTextureLayer.SourceType.Import;
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();

            // Auto-detect texture type and set canvas mode
            var texType = TextureTypeDetector.Detect(propertyName ?? "");
            if (TextureTypeDetector.IsGrayscale(texType))
            {
                canvasColorMode = CanvasColorMode.Mask;
                brushSettings.colorMode = false;
            }
            else
            {
                canvasColorMode = CanvasColorMode.Color;
                brushSettings.colorMode = true;
            }

            ChannelView.CurrentMode = SuggestChannelView(layer.pixels, texType);
            RefreshCanvasPreviewTexture();

            // Auto-enable Live Link
            if (targetMaterial != null && !string.IsNullOrEmpty(propertyName))
            {
                EnableLiveLink();
            }

            Repaint();
        }

        private void LoadLiveTextureForEditing(Texture2D texture, string propertyName, bool replaceLayers)
        {
            if (texture == null) return;

            EnsureLiveSourceTextureLayer(texture, propertyName, replaceLayers);

            MaskTextureLayer previewSource = layerStack != null && layerStack.Layers.Count > 0
                ? layerStack.Layers[0]
                : null;
            if (previewSource == null)
                return;

            var texType = TextureTypeDetector.Detect(propertyName ?? "");
            if (TextureTypeDetector.IsGrayscale(texType))
            {
                canvasColorMode = CanvasColorMode.Mask;
                brushSettings.colorMode = false;
            }
            else
            {
                canvasColorMode = CanvasColorMode.Color;
                brushSettings.colorMode = true;
            }

            ChannelView.CurrentMode = SuggestChannelView(previewSource.pixels, texType);
            layerStack.InvalidateFlattenCache();
            RefreshPreviewFromLayers();
            RefreshCanvasPreviewTexture();

            if (targetMaterial != null && !string.IsNullOrEmpty(propertyName))
                EnableLiveLink();

            Repaint();
        }

        private void EnsureLayerStack()
        {
            if (layerStack == null)
                layerStack = new MaskLayerStack(textureSize, textureSize);
            if (layerStack.Layers.Count == 0)
                layerStack.AddLayer("Base Layer");
        }

        private void RequestPreviewRefresh(bool immediate, bool updateLiveLink)
        {
            if (immediate)
            {
                previewRefreshPending = false;
                previewRefreshNeedsLiveLink = false;
                RefreshPreviewFromLayersInternal();
                if (updateLiveLink)
                    RequestLiveLinkSync(true);
                nextPreviewRefreshTime = EditorApplication.timeSinceStartup + InteractivePreviewRefreshInterval;
                return;
            }

            previewRefreshPending = true;
            previewRefreshNeedsLiveLink |= updateLiveLink;
            Repaint();
        }

        private void FlushPendingPreviewRefresh(bool forceImmediate = false, bool forceLiveLink = false)
        {
            if (!previewRefreshPending)
                return;

            double now = EditorApplication.timeSinceStartup;
            if (!forceImmediate && now < nextPreviewRefreshTime)
                return;

            bool updateLiveLink = previewRefreshNeedsLiveLink || forceLiveLink;
            previewRefreshPending = false;
            previewRefreshNeedsLiveLink = false;
            RefreshPreviewFromLayersInternal();
            if (updateLiveLink)
                RequestLiveLinkSync(forceImmediate || forceLiveLink);
            nextPreviewRefreshTime = now + InteractivePreviewRefreshInterval;
        }

        private void RefreshPreviewFromLayers()
        {
            RefreshPreviewFromLayersInternal();
            RequestLiveLinkSync(true);
        }

        /// <summary>
        /// Request preview refresh from brush painting, using partial path when possible.
        /// ブラシ描画からのプレビュー更新リクエスト（可能な場合は部分パスを使用）
        /// </summary>
        private void RequestPreviewRefreshFromBrush(bool updateLiveLink)
        {
            previewRefreshPending = false;
            previewRefreshNeedsLiveLink = false;

            // TODO: Re-enable partial flatten once coordinate mapping is validated
            // 部分フラットンは座標マッピングの検証後に再有効化する
            brushDirtyRect.Reset();
            RefreshPreviewFromLayersInternal();

            if (updateLiveLink)
                RequestLiveLinkSync(true);
            nextPreviewRefreshTime = EditorApplication.timeSinceStartup + InteractivePreviewRefreshInterval;
        }

        /// <summary>
        /// Partial preview refresh: only flatten and upload the dirty region.
        /// 部分プレビュー更新：ダーティ領域のみフラットンしてアップロード
        /// </summary>
        private void RefreshPreviewFromLayersPartial(DirtyRect region)
        {
            if (layerStack == null) return;
            int w = layerStack.Width;
            int h = layerStack.Height;

            DirtyRect clamped = region.Clamp(w, h);
            if (clamped.isEmpty)
            {
                RefreshPreviewFromLayersInternal();
                return;
            }

            // Ensure previewPixels is allocated / previewPixelsが確保されていることを保証
            if (previewPixels == null || previewPixels.Length != w * h)
            {
                RefreshPreviewFromLayersInternal();
                return;
            }

            // Flatten only the dirty region into previewPixels / ダーティ領域のみpreviewPixelsにフラットン
            layerStack.FlattenRegion(previewPixels, clamped.x, clamped.y, clamped.width, clamped.height);

            // Partial texture upload / 部分テクスチャアップロード
            if (previewTexture != null && previewTexture.width == w && previewTexture.height == h)
            {
                int rw = clamped.width;
                int rh = clamped.height;
                Color[] regionPixels = new Color[rw * rh];

                for (int ry = 0; ry < rh; ry++)
                {
                    int srcRow = (clamped.y + ry) * w + clamped.x;
                    int dstRow = ry * rw;
                    System.Array.Copy(previewPixels, srcRow, regionPixels, dstRow, rw);
                }

                previewTexture.SetPixels(clamped.x, clamped.y, rw, rh, regionPixels);
                previewTexture.Apply(false, false);
                RefreshCanvasPreviewTexture();
            }
            else
            {
                // Texture mismatch, fall back to full refresh / テクスチャ不一致、フルリフレッシュにフォールバック
                UpdatePreview(previewPixels, w);
            }

            autoSave?.MarkDirty();
        }

        private void RefreshPreviewFromLayersInternal()
        {
            if (layerStack == null) return;
            Color[] flattened = layerStack.Flatten();
            if (flattened != null)
            {
                if (ReferenceEquals(previewPixels, flattened) && previewTextureSize == layerStack.Width)
                    RefreshCanvasPreviewTexture();
                else
                    UpdatePreview(flattened, layerStack.Width);
            }
            autoSave?.MarkDirty();
        }

        private void UpdatePreview(Color[] pixels, int size)
        {
            previewPixels = pixels;
            previewTextureSize = size;
            UpdateTexturePixels(ref previewTexture, pixels, size, ref previewUploadBuffer);
            RefreshCanvasPreviewTexture();

            Repaint();
        }

        private void RequestLiveLinkSync(bool immediate)
        {
            if (liveLink == null || !liveLink.IsEnabled || previewTexture == null)
                return;

            if (immediate)
                liveLink.UpdateTextureImmediate(previewTexture);
            else
                liveLink.UpdateTexture(previewTexture);
        }

        private void RefreshCanvasPreviewTexture()
        {
            Color[] canvasSourcePixels;
            int canvasWidth;
            int canvasHeight;
            GetCanvasPreviewSource(out canvasSourcePixels, out canvasWidth, out canvasHeight);

            if (canvasSourcePixels == null || canvasWidth <= 0 || canvasHeight <= 0)
            {
                if (canvasPreviewTexture != null && canvasPreviewTexture != previewTexture)
                {
                    Object.DestroyImmediate(canvasPreviewTexture);
                }
                canvasPreviewTexture = null;
                return;
            }

            Color[] displayPixels = ChannelView.ApplyView(canvasSourcePixels, canvasWidth, canvasHeight, canvasDisplayBuffer);
            if (!ReferenceEquals(displayPixels, canvasSourcePixels))
                canvasDisplayBuffer = displayPixels;

            // RGBA mode with same source: reuse previewTexture to avoid double upload
            // RGBAモードで同一ソースの場合、二重アップロードを避けてpreviewTextureを再利用
            if (ReferenceEquals(displayPixels, previewPixels) && previewTexture != null)
            {
                if (canvasPreviewTexture != null && canvasPreviewTexture != previewTexture)
                    Object.DestroyImmediate(canvasPreviewTexture);
                canvasPreviewTexture = previewTexture;
                return;
            }

            // Detach shared reference before UpdateTexturePixels can destroy it
            // 共有参照を解除してUpdateTexturePixelsがpreviewTextureを破棄しないようにする
            if (canvasPreviewTexture == previewTexture)
                canvasPreviewTexture = null;
            UpdateTexturePixels(ref canvasPreviewTexture, displayPixels, canvasWidth, canvasHeight, ref canvasUploadBuffer);
        }

        private void GetCanvasPreviewSource(out Color[] pixels, out int width, out int height)
        {
            pixels = previewPixels;
            width = previewTextureSize;
            height = previewTextureSize;

            MaskTextureLayer activeLayer = layerStack != null ? layerStack.ActiveLayer : null;
            if (activeLayer != null && activeLayer.editingMask && activeLayer.mask != null)
            {
                pixels = activeLayer.mask;
                width = activeLayer.width;
                height = activeLayer.height;
            }
        }

        private void UpdateTexturePixels(ref Texture2D texture, Color[] pixels, int size, ref Color32[] uploadBuffer)
        {
            UpdateTexturePixels(ref texture, pixels, size, size, ref uploadBuffer);
        }

        private void UpdateTexturePixels(ref Texture2D texture, Color[] pixels, int width, int height, ref Color32[] uploadBuffer)
        {
            if (pixels == null || width <= 0 || height <= 0)
                return;

            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);

                texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            int pixelCount = width * height;
            if (uploadBuffer == null || uploadBuffer.Length != pixelCount)
                uploadBuffer = new Color32[pixelCount];

            for (int i = 0; i < pixelCount; i++)
                uploadBuffer[i] = pixels[i];

            texture.SetPixels32(uploadBuffer);
            texture.Apply(false, false);
        }

        private static ChannelViewMode SuggestChannelView(Color[] pixels, TextureEditType texType)
        {
            if (!TextureTypeDetector.IsGrayscale(texType))
                return ChannelViewMode.RGBA;

            if (pixels == null || pixels.Length == 0)
                return ChannelViewMode.Red;

            int step = Mathf.Max(1, pixels.Length / 512);
            float rgbEnergy = 0f;
            float alphaEnergy = 0f;
            int sampleCount = 0;

            for (int i = 0; i < pixels.Length; i += step)
            {
                Color pixel = pixels[i];
                rgbEnergy += Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
                alphaEnergy += pixel.a;
                sampleCount++;
            }

            if (sampleCount == 0)
                return ChannelViewMode.Red;

            float averageRgb = rgbEnergy / sampleCount;
            float averageAlpha = alphaEnergy / sampleCount;
            return averageAlpha > averageRgb + 0.05f && averageRgb < 0.1f
                ? ChannelViewMode.Alpha
                : ChannelViewMode.Red;
        }

        // ================================================================
        // NoiseGenerator (static class)
        // ================================================================

        private static class NoiseGenerator
        {
            public static float Generate(NoiseType type, float x, float y, int seed, int octaves, float lacunarity, float persistence)
            {
                switch (type)
                {
                    case NoiseType.Perlin:
                        return PerlinNoise(x, y, seed);
                    case NoiseType.Voronoi:
                        return VoronoiNoise(x, y, seed);
                    case NoiseType.Cellular:
                        return CellularNoise(x, y, seed);
                    case NoiseType.FBM:
                        return FBMNoise(x, y, seed, octaves, lacunarity, persistence);
                    case NoiseType.Value:
                        return ValueNoise(x, y, seed);
                    default:
                        return 0f;
                }
            }

            public static float PerlinNoise(float x, float y, int seed)
            {
                float offsetVal = seed * 137.31f;
                return Mathf.PerlinNoise(x + offsetVal, y + offsetVal);
            }

            public static float VoronoiNoise(float x, float y, int seed)
            {
                int xi = Mathf.FloorToInt(x);
                int yi = Mathf.FloorToInt(y);
                float minDist = float.MaxValue;

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        int cx = xi + i;
                        int cy = yi + j;
                        float px = cx + Hash2DFloat(cx, cy, seed, 0);
                        float py = cy + Hash2DFloat(cx, cy, seed, 1);

                        float dx = x - px;
                        float dy = y - py;
                        float dist = dx * dx + dy * dy;
                        if (dist < minDist) minDist = dist;
                    }
                }

                return Mathf.Clamp01(Mathf.Sqrt(minDist) / 1.414f);
            }

            public static float CellularNoise(float x, float y, int seed)
            {
                int xi = Mathf.FloorToInt(x);
                int yi = Mathf.FloorToInt(y);

                float minDist1 = float.MaxValue;
                float minDist2 = float.MaxValue;

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        int cx = xi + i;
                        int cy = yi + j;
                        float px = cx + Hash2DFloat(cx, cy, seed, 0);
                        float py = cy + Hash2DFloat(cx, cy, seed, 1);

                        float dx = x - px;
                        float dy = y - py;
                        float dist = dx * dx + dy * dy;

                        if (dist < minDist1)
                        {
                            minDist2 = minDist1;
                            minDist1 = dist;
                        }
                        else if (dist < minDist2)
                        {
                            minDist2 = dist;
                        }
                    }
                }

                return Mathf.Clamp01(Mathf.Sqrt(minDist2) - Mathf.Sqrt(minDist1));
            }

            public static float FBMNoise(float x, float y, int seed, int octaves, float lacunarity, float persistence)
            {
                float sum = 0f;
                float amplitude = 1f;
                float frequency = 1f;
                float maxValue = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    sum += amplitude * PerlinNoise(x * frequency, y * frequency, seed + i * 31);
                    maxValue += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                return sum / maxValue;
            }

            public static float ValueNoise(float x, float y, int seed)
            {
                int xi = Mathf.FloorToInt(x);
                int yi = Mathf.FloorToInt(y);
                float xf = x - xi;
                float yf = y - yi;

                float u = xf * xf * (3f - 2f * xf);
                float v = yf * yf * (3f - 2f * yf);

                float v00 = Hash2DFloat(xi, yi, seed, 0);
                float v10 = Hash2DFloat(xi + 1, yi, seed, 0);
                float v01 = Hash2DFloat(xi, yi + 1, seed, 0);
                float v11 = Hash2DFloat(xi + 1, yi + 1, seed, 0);

                float a = Mathf.Lerp(v00, v10, u);
                float b = Mathf.Lerp(v01, v11, u);
                return Mathf.Lerp(a, b, v);
            }

            private static float Hash2DFloat(int x, int y, int seed, int channel)
            {
                int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791 ^ channel * 39916801;
                h = (h ^ (h >> 13)) * 1274126177;
                h = h ^ (h >> 16);
                return (float)(h & 0x7FFFFFFF) / 2147483647f;
            }
        }

        // ================================================================
        // UVIsland data class
        // ================================================================

        internal class UVIsland
        {
            public List<int> triangleIndices = new List<int>();
            public Rect uvBounds;
            public float area;
            public int triangleCount;
            public bool selected = true;
        }

        // ================================================================
        // UVIslandDetector (Union-Find based)
        // ================================================================

        private static class UVIslandDetector
        {
            public static List<UVIsland> DetectIslands(Mesh mesh, List<Vector2> uvs, int[] triangles)
            {
                int triCount = triangles.Length / 3;

                if (triCount == 0) return new List<UVIsland>();

                int[] parent = new int[triCount];
                int[] rank = new int[triCount];
                for (int i = 0; i < triCount; i++) parent[i] = i;

                var edgeToTriangle = new Dictionary<long, int>();

                for (int t = 0; t < triCount; t++)
                {
                    int i0 = triangles[t * 3 + 0];
                    int i1 = triangles[t * 3 + 1];
                    int i2 = triangles[t * 3 + 2];

                    if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                    ProcessEdge(edgeToTriangle, parent, rank, uvs[i0], uvs[i1], t);
                    ProcessEdge(edgeToTriangle, parent, rank, uvs[i1], uvs[i2], t);
                    ProcessEdge(edgeToTriangle, parent, rank, uvs[i2], uvs[i0], t);
                }

                var groups = new Dictionary<int, List<int>>();
                for (int t = 0; t < triCount; t++)
                {
                    int root = Find(parent, t);
                    if (!groups.ContainsKey(root))
                        groups[root] = new List<int>();
                    groups[root].Add(t);
                }

                var result = new List<UVIsland>();
                foreach (var group in groups.Values)
                {
                    var island = new UVIsland();
                    island.triangleIndices = group;
                    island.triangleCount = group.Count;

                    float minU = float.MaxValue, maxU = float.MinValue;
                    float minV = float.MaxValue, maxV = float.MinValue;
                    float totalArea = 0f;

                    foreach (int t in group)
                    {
                        int i0 = triangles[t * 3 + 0];
                        int i1 = triangles[t * 3 + 1];
                        int i2 = triangles[t * 3 + 2];

                        if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                        Vector2 uv0 = uvs[i0], uv1 = uvs[i1], uv2 = uvs[i2];

                        minU = Mathf.Min(minU, Mathf.Min(uv0.x, Mathf.Min(uv1.x, uv2.x)));
                        maxU = Mathf.Max(maxU, Mathf.Max(uv0.x, Mathf.Max(uv1.x, uv2.x)));
                        minV = Mathf.Min(minV, Mathf.Min(uv0.y, Mathf.Min(uv1.y, uv2.y)));
                        maxV = Mathf.Max(maxV, Mathf.Max(uv0.y, Mathf.Max(uv1.y, uv2.y)));

                        totalArea += Mathf.Abs(
                            (uv1.x - uv0.x) * (uv2.y - uv0.y) -
                            (uv2.x - uv0.x) * (uv1.y - uv0.y)) * 0.5f;
                    }

                    island.uvBounds = new Rect(minU, minV, maxU - minU, maxV - minV);
                    island.area = totalArea;
                    island.selected = true;
                    result.Add(island);
                }

                result.Sort((a, b) => b.area.CompareTo(a.area));
                return result;
            }

            private static void ProcessEdge(Dictionary<long, int> edgeToTriangle, int[] parent, int[] rank, Vector2 a, Vector2 b, int triIndex)
            {
                long keyA = QuantizeUV(a);
                long keyB = QuantizeUV(b);

                long edgeKey;
                if (keyA < keyB)
                    edgeKey = keyA * 100000007L + keyB;
                else
                    edgeKey = keyB * 100000007L + keyA;

                if (edgeToTriangle.TryGetValue(edgeKey, out int otherTri))
                    Union(parent, rank, triIndex, otherTri);
                else
                    edgeToTriangle[edgeKey] = triIndex;
            }

            private static long QuantizeUV(Vector2 uv)
            {
                int x = Mathf.RoundToInt(uv.x * 10000f);
                int y = Mathf.RoundToInt(uv.y * 10000f);
                return ((long)(x + 50000)) * 100001L + (long)(y + 50000);
            }

            private static int Find(int[] parent, int i)
            {
                while (parent[i] != i)
                {
                    parent[i] = parent[parent[i]];
                    i = parent[i];
                }
                return i;
            }

            private static void Union(int[] parent, int[] rank, int a, int b)
            {
                int ra = Find(parent, a);
                int rb = Find(parent, b);
                if (ra == rb) return;

                if (rank[ra] < rank[rb]) { int tmp = ra; ra = rb; rb = tmp; }
                parent[rb] = ra;
                if (rank[ra] == rank[rb]) rank[ra]++;
            }
        }

        // ================================================================
        // TriangleRasterizer (barycentric coordinate method)
        // ================================================================

        private static class TriangleRasterizer
        {
            public static void RasterizeIslands(Color[] pixels, int size, int[] triangles, List<Vector2> uvs,
                List<UVIsland> islands, bool[] selectedFlags)
            {
                Color white = Color.white;

                for (int i = 0; i < islands.Count; i++)
                {
                    if (i >= selectedFlags.Length || !selectedFlags[i]) continue;

                    foreach (int t in islands[i].triangleIndices)
                    {
                        int i0 = triangles[t * 3 + 0];
                        int i1 = triangles[t * 3 + 1];
                        int i2 = triangles[t * 3 + 2];

                        if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                        RasterizeTriangle(pixels, size, uvs[i0], uvs[i1], uvs[i2], white);
                    }
                }
            }

            public static void RasterizeTriangle(Color[] pixels, int size, Vector2 v0, Vector2 v1, Vector2 v2, Color color)
            {
                float px0 = v0.x * (size - 1);
                float py0 = v0.y * (size - 1);
                float px1 = v1.x * (size - 1);
                float py1 = v1.y * (size - 1);
                float px2 = v2.x * (size - 1);
                float py2 = v2.y * (size - 1);

                int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(px0, Mathf.Min(px1, px2))), 0, size - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(px0, Mathf.Max(px1, px2))), 0, size - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(py0, Mathf.Min(py1, py2))), 0, size - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(py0, Mathf.Max(py1, py2))), 0, size - 1);

                float denom = (py1 - py2) * (px0 - px2) + (px2 - px1) * (py0 - py2);
                if (Mathf.Abs(denom) < 1e-8f) return;

                float invDenom = 1f / denom;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float px = x + 0.5f;
                        float py = y + 0.5f;

                        float w0 = ((py1 - py2) * (px - px2) + (px2 - px1) * (py - py2)) * invDenom;
                        float w1 = ((py2 - py0) * (px - px2) + (px0 - px2) * (py - py2)) * invDenom;
                        float w2 = 1f - w0 - w1;

                        if (w0 >= 0f && w1 >= 0f && w2 >= 0f)
                            pixels[y * size + x] = color;
                    }
                }
            }
        }

        // ================================================================
        // BoundaryGradient (iterative erosion)
        // ================================================================

        private static class BoundaryGradient
        {
            public static void Apply(Color[] pixels, int size, int widthPixels, GradientDirection direction)
            {
                if (widthPixels <= 0) return;

                int totalPixels = size * size;

                bool[] mask = new bool[totalPixels];
                for (int i = 0; i < totalPixels; i++)
                    mask[i] = pixels[i].r > 0.5f;

                float[] distance = new float[totalPixels];
                for (int i = 0; i < totalPixels; i++)
                    distance[i] = mask[i] ? widthPixels : 0f;

                bool[] current = (bool[])mask.Clone();

                for (int iter = 1; iter <= widthPixels; iter++)
                {
                    bool[] next = (bool[])current.Clone();

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            int idx = y * size + x;
                            if (!current[idx]) continue;

                            bool onBoundary = false;
                            if (x > 0 && !current[idx - 1]) onBoundary = true;
                            else if (x < size - 1 && !current[idx + 1]) onBoundary = true;
                            else if (y > 0 && !current[idx - size]) onBoundary = true;
                            else if (y < size - 1 && !current[idx + size]) onBoundary = true;

                            if (onBoundary)
                            {
                                distance[idx] = iter;
                                next[idx] = false;
                            }
                        }
                    }

                    current = next;
                }

                float invWidth = 1f / widthPixels;
                for (int i = 0; i < totalPixels; i++)
                {
                    if (!mask[i])
                    {
                        pixels[i] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }

                    float value = distance[i] * invWidth;

                    if (direction == GradientDirection.Outward)
                        value = 1f - value;

                    pixels[i] = new Color(value, value, value, 1f);
                }
            }
        }
        // ================================================================
        // Panel Splitter
        // ================================================================

        private void DrawSplitter(ref bool isDragging, ref float panelWidth, float minWidth, float maxWidth, bool invertDrag)
        {
            Rect splitterRect = GUILayoutUtility.GetRect(SplitterWidth, 0, GUILayout.Width(SplitterWidth), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            // Draw visual indicator
            Color prevColor = GUI.color;
            GUI.color = isDragging ? SplitterActive : SplitterInactive;
            GUI.DrawTexture(splitterRect, EditorGUIUtility.whiteTexture);
            GUI.color = prevColor;

            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (splitterRect.Contains(e.mousePosition))
                    {
                        isDragging = true;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (isDragging)
                    {
                        float delta = invertDrag ? -e.delta.x : e.delta.x;
                        panelWidth = Mathf.Clamp(panelWidth + delta, minWidth, maxWidth);
                        e.Use();
                        Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (isDragging)
                    {
                        isDragging = false;
                        e.Use();
                    }
                    break;
            }
        }

        private void WriteTextureToMaterial()
        {
            if (targetMaterial == null || previewTexture == null) return;
            string propName = GetSelectedPropertyName();
            if (string.IsNullOrEmpty(propName)) return;
            if (!targetMaterial.HasProperty(propName)) return;

            Texture2D copy = new Texture2D(previewTexture.width, previewTexture.height, TextureFormat.RGBA32, false);
            copy.SetPixels(previewTexture.GetPixels());
            copy.Apply();
            copy.filterMode = previewTexture.filterMode;
            copy.wrapMode = previewTexture.wrapMode;

            Undo.RecordObject(targetMaterial, "Write Texture to Material");
            targetMaterial.SetTexture(propName, copy);
            EditorUtility.SetDirty(targetMaterial);
        }

    }
}
