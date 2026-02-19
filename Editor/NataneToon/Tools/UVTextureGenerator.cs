using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
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

        // ===== Tab State =====
        private GeneratorTab currentTab = GeneratorTab.Noise;
        private Vector2 scrollPosition;

        // ===== Canvas State =====
        private Texture2D previewTexture;
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

        // ===== 3D Preview =====
        private MaskTexture3DPreview preview3D;
        private bool show3DPreview;

        // ===== Brush Tool =====
        private BrushSettings brushSettings = new BrushSettings();
        private MaskTextureBrush brush;
        private bool brushEnabled;

        // ===== Material Assignment =====
        private Material targetMaterial;
        private int selectedPropertyIndex;

        // ===== Noise Parameters =====
        private NoiseType noiseType = NoiseType.Perlin;
        private int textureSize = 512;
        private float scale = 5f;
        private int seed;
        private float contrast = 1f;
        private bool invert;
        private int octaves = 4;
        private float lacunarity = 2f;
        private float persistence = 0.5f;
        private Vector2 offset;

        // ===== UV Mask Parameters =====
        private Object meshSource;
        private int uvChannel;
        private int maskTextureSize = 512;
        private FillMode fillMode = FillMode.Solid;
        private GradientDirection gradientDirection = GradientDirection.Inward;
        private int gradientWidth = 10;
        private bool maskInvert;
        private List<UVIsland> islands = new List<UVIsland>();
        private Vector2 islandScrollPosition;
        private bool islandsFoldout = true;

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

        // ===== Tab Labels =====
        private static readonly string[] tabLabels = {
            "ノイズ", "UVマスク", "グラデ", "メッシュ", "複合", "テンプレ", "CHパック"
        };
        private static readonly int[] textureSizes = { 256, 512, 1024, 2048, 4096 };
        private static readonly string[] textureSizeLabels = { "256", "512", "1024", "2048", "4096" };

        // ===== Mask Properties =====
        private static readonly string[] maskProperties = {
            "_SpecularMask", "_RimMask", "_RimMask2", "_SSSMask",
            "_MatCapMask", "_MatCapMask2", "_MatCapMask3",
            "_GlitterMask", "_EmissionMask", "_DissolveTex", "_DissolveMask",
            "_AlphaMask", "_OutlineMask", "_OutlineWidthMap",
            "_IridescenceMask", "_EnvRimMask", "_ReflectionMask", "_RefractionMask",
            "_ShadowReceiveMask", "_2ndTexMask", "_3rdTexMask", "_4thTexMask", "_5thTexMask"
        };

        [MenuItem("Tools/Natane/UVテクスチャ生成 UV Texture Generator", false, 141)]
        public static void ShowWindow()
        {
            var window = GetWindow<UVTextureGenerator>("UVテクスチャ生成 UV Texture Generator");
            window.minSize = new Vector2(580, 700);
            window.Show();
        }

        private void OnEnable()
        {
            if (layerStack == null)
                layerStack = new MaskLayerStack(textureSize, textureSize);
            if (layerStack.Layers.Count == 0)
                layerStack.AddLayer("Base Layer");
            brush = new MaskTextureBrush(brushSettings);
        }

        private void OnDisable()
        {
            preview3D?.Dispose();
            preview3D = null;
        }

        private void OnDestroy()
        {
            if (previewTexture != null)
                DestroyImmediate(previewTexture);
            preview3D?.Dispose();
            preview3D = null;
        }

        // ================================================================
        // Main GUI
        // ================================================================

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            NataneToonShaderGUIUtility.DrawHeaderWithHelp("UVテクスチャ生成", "UV Texture Generator", "UVTextureGenerator");
            EditorGUILayout.LabelField(
                "マスクテクスチャの生成・編集・エクスポート\nGenerate, edit, and export mask textures",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            currentTab = (GeneratorTab)GUILayout.Toolbar((int)currentTab, tabLabels);
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
                "レイヤー Layers", layerPanelFoldout);
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
        }

        // ================================================================
        // Tab 1: Noise Generation
        // ================================================================

        private void DrawNoiseTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ノイズ設定 Noise Settings", EditorStyles.boldLabel);

            noiseType = (NoiseType)EditorGUILayout.EnumPopup("ノイズタイプ", noiseType);
            textureSize = EditorGUILayout.IntPopup("テクスチャサイズ", textureSize, textureSizeLabels, textureSizes);
            scale = EditorGUILayout.Slider("スケール", scale, 0.5f, 50f);
            seed = EditorGUILayout.IntField("シード", seed);
            contrast = EditorGUILayout.Slider("コントラスト", contrast, 0.1f, 3f);
            invert = EditorGUILayout.Toggle("反転", invert);

            if (noiseType == NoiseType.FBM)
            {
                NataneToonShaderGUIUtility.DrawSeparator();
                EditorGUILayout.LabelField("FBM設定", EditorStyles.boldLabel);
                octaves = EditorGUILayout.IntSlider("オクターブ", octaves, 1, 8);
                lacunarity = EditorGUILayout.Slider("ラクナリティ", lacunarity, 1f, 4f);
                persistence = EditorGUILayout.Slider("パーシステンス", persistence, 0f, 1f);
            }

            offset = EditorGUILayout.Vector2Field("オフセット", offset);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("プレビュー生成 Generate Preview", GUILayout.Height(30)))
            {
                GenerateNoiseTexture();
            }
            if (GUILayout.Button("レイヤーに追加 Add to Layer", GUILayout.Height(30)))
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
                    pixels[y * size + x] = new Color(value, value, value);
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
            EditorGUILayout.LabelField("メッシュ設定 Mesh Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            meshSource = EditorGUILayout.ObjectField("メッシュソース", meshSource, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
                islands.Clear();

            uvChannel = EditorGUILayout.IntPopup("UVチャンネル", uvChannel, new[] { "UV0", "UV1", "UV2", "UV3" }, new[] { 0, 1, 2, 3 });
            maskTextureSize = EditorGUILayout.IntPopup("テクスチャサイズ", maskTextureSize, textureSizeLabels, textureSizes);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("アイランドを検出 Detect Islands", GUILayout.Height(25)))
                DetectIslands();

            EditorGUILayout.EndVertical();

            if (islands.Count > 0)
            {
                DrawIslandList();
                DrawFillSettings();

                EditorGUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("プレビュー生成 Generate Preview", GUILayout.Height(30)))
                    GenerateUVMaskTexture();
                if (GUILayout.Button("レイヤーに追加 Add to Layer", GUILayout.Height(30)))
                    GenerateUVMaskToLayer();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawIslandList()
        {
            EditorGUILayout.Space(5);
            islandsFoldout = NataneToonShaderGUIUtility.DrawFoldoutHeader(
                $"検出アイランド Detected Islands ({islands.Count})", islandsFoldout);

            if (!islandsFoldout) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全選択 Select All", GUILayout.Height(20)))
                foreach (var island in islands) island.selected = true;
            if (GUILayout.Button("全解除 Deselect All", GUILayout.Height(20)))
                foreach (var island in islands) island.selected = false;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            float listHeight = Mathf.Min(islands.Count * 22f, 200f);
            islandScrollPosition = EditorGUILayout.BeginScrollView(islandScrollPosition, GUILayout.Height(listHeight));

            for (int i = 0; i < islands.Count; i++)
            {
                var island = islands[i];
                island.selected = EditorGUILayout.ToggleLeft(
                    $"アイランド #{i}  (\u25B3{island.triangleCount}, 面積: {island.area:F3})",
                    island.selected);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawFillSettings()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("フィル設定 Fill Settings", EditorStyles.boldLabel);

            fillMode = (FillMode)EditorGUILayout.EnumPopup("フィルモード", fillMode);

            if (fillMode == FillMode.BoundaryGradient)
            {
                gradientWidth = EditorGUILayout.IntSlider("グラデーション幅 (px)", gradientWidth, 1, 100);
                gradientDirection = (GradientDirection)EditorGUILayout.EnumPopup("方向", gradientDirection);
            }

            maskInvert = EditorGUILayout.Toggle("反転", maskInvert);

            EditorGUILayout.EndVertical();
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

        private void DetectIslands()
        {
            Mesh mesh = ExtractMesh();
            if (mesh == null)
            {
                EditorUtility.DisplayDialog("エラー",
                    "メッシュを取得できません。\nMesh, GameObject, MeshFilter, SkinnedMeshRenderer を指定してください。", "OK");
                return;
            }

            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(uvChannel, uvs);

            if (uvs.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", $"UV{uvChannel} が存在しません。", "OK");
                return;
            }

            islands = UVIslandDetector.DetectIslands(mesh, uvs);

            if (islands.Count == 0)
                EditorUtility.DisplayDialog("情報", "アイランドが検出されませんでした。", "OK");
        }

        private Color[] GenerateUVMaskPixels(int size)
        {
            Mesh mesh = ExtractMesh();
            if (mesh == null) return null;

            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(uvChannel, uvs);
            if (uvs.Count == 0) return null;

            Color[] pixels = new Color[size * size];
            bool[] selectedFlags = islands.Select(i => i.selected).ToArray();
            TriangleRasterizer.RasterizeIslands(pixels, size, mesh, uvs, islands, selectedFlags);

            if (fillMode == FillMode.BoundaryGradient)
                BoundaryGradient.Apply(pixels, size, gradientWidth, gradientDirection);

            if (maskInvert)
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    float v = 1f - pixels[i].r;
                    pixels[i] = new Color(v, v, v);
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
            RefreshPreviewFromLayers();
        }

        // ================================================================
        // Tab 3: Gradient
        // ================================================================

        private void DrawGradientTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("グラデーション設定 Gradient Settings", EditorStyles.boldLabel);

            textureSize = EditorGUILayout.IntPopup("テクスチャサイズ", textureSize, textureSizeLabels, textureSizes);
            gradientType = (GradientType)EditorGUILayout.EnumPopup("タイプ Type", gradientType);

            if (gradientType == GradientType.HeightBased)
            {
                DrawMeshSourceField();
                EditorGUILayout.HelpBox(
                    "高さベースグラデーションにはメッシュが必要です。\nHeight-based gradient requires a mesh.",
                    MessageType.Info);
            }
            else
            {
                if (gradientType == GradientType.Linear || gradientType == GradientType.Angular)
                    gradientParams.angle = EditorGUILayout.Slider("角度 Angle", gradientParams.angle, 0f, 360f);

                if (gradientType == GradientType.Radial)
                    gradientParams.radius = EditorGUILayout.Slider("半径 Radius", gradientParams.radius, 0.01f, 2f);

                gradientParams.center = EditorGUILayout.Vector2Field("中心 Center", gradientParams.center);
            }

            gradientParams.curve = EditorGUILayout.CurveField("カーブ Curve", gradientParams.curve);
            gradientParams.invert = EditorGUILayout.Toggle("反転 Invert", gradientParams.invert);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("プレビュー生成 Generate Preview", GUILayout.Height(30)))
                GenerateGradientTexture();
            if (GUILayout.Button("レイヤーに追加 Add to Layer", GUILayout.Height(30)))
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
                    EditorUtility.DisplayDialog("エラー", "メッシュを取得できません。", "OK");
                    return null;
                }
                List<Vector2> uvs = new List<Vector2>();
                mesh.GetUVs(uvChannel, uvs);
                if (uvs.Count == 0)
                {
                    EditorUtility.DisplayDialog("エラー", $"UV{uvChannel} が存在しません。", "OK");
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
            EditorGUILayout.LabelField("メッシュ情報 Mesh Info Settings", EditorStyles.boldLabel);

            DrawMeshSourceField();
            textureSize = EditorGUILayout.IntPopup("テクスチャサイズ", textureSize, textureSizeLabels, textureSizes);
            meshInfoType = (MeshInfoType)EditorGUILayout.EnumPopup("タイプ Type", meshInfoType);

            switch (meshInfoType)
            {
                case MeshInfoType.Curvature:
                    curvatureSensitivity = EditorGUILayout.Slider("感度 Sensitivity", curvatureSensitivity, 0.1f, 5f);
                    break;
                case MeshInfoType.NormalDirection:
                    normalDirection = EditorGUILayout.Vector3Field("方向 Direction", normalDirection);
                    normalThreshold = EditorGUILayout.Slider("閾値 Threshold", normalThreshold, 0f, 1f);
                    break;
                case MeshInfoType.VertexColor:
                    vertexColorChannel = EditorGUILayout.IntPopup("チャンネル Channel", vertexColorChannel,
                        new[] { "R", "G", "B", "A" }, new[] { 0, 1, 2, 3 });
                    break;
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("プレビュー生成 Generate Preview", GUILayout.Height(30)))
                GenerateMeshInfoTexture();
            if (GUILayout.Button("レイヤーに追加 Add to Layer", GUILayout.Height(30)))
                GenerateMeshInfoToLayer();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private Color[] GenerateMeshInfoPixels(int size)
        {
            Mesh mesh = ExtractMesh();
            if (mesh == null)
            {
                EditorUtility.DisplayDialog("エラー", "メッシュを取得できません。", "OK");
                return null;
            }
            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(uvChannel, uvs);
            if (uvs.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", $"UV{uvChannel} が存在しません。", "OK");
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
            RefreshPreviewFromLayers();
        }

        // ================================================================
        // Tab 5: Combined
        // ================================================================

        private void DrawCombinedTab()
        {
            // Mesh settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("メッシュ設定 Mesh Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            meshSource = EditorGUILayout.ObjectField("メッシュソース", meshSource, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
                islands.Clear();

            uvChannel = EditorGUILayout.IntPopup("UVチャンネル", uvChannel, new[] { "UV0", "UV1", "UV2", "UV3" }, new[] { 0, 1, 2, 3 });

            EditorGUILayout.Space(5);

            if (GUILayout.Button("アイランドを検出 Detect Islands", GUILayout.Height(25)))
                DetectIslands();

            EditorGUILayout.EndVertical();

            if (islands.Count > 0)
                DrawIslandList();

            // Noise settings
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ノイズ設定 Noise Settings", EditorStyles.boldLabel);

            noiseType = (NoiseType)EditorGUILayout.EnumPopup("ノイズタイプ", noiseType);
            textureSize = EditorGUILayout.IntPopup("テクスチャサイズ", textureSize, textureSizeLabels, textureSizes);
            scale = EditorGUILayout.Slider("スケール", scale, 0.5f, 50f);
            seed = EditorGUILayout.IntField("シード", seed);
            contrast = EditorGUILayout.Slider("コントラスト", contrast, 0.1f, 3f);

            if (noiseType == NoiseType.FBM)
            {
                NataneToonShaderGUIUtility.DrawSeparator();
                EditorGUILayout.LabelField("FBM設定", EditorStyles.boldLabel);
                octaves = EditorGUILayout.IntSlider("オクターブ", octaves, 1, 8);
                lacunarity = EditorGUILayout.Slider("ラクナリティ", lacunarity, 1f, 4f);
                persistence = EditorGUILayout.Slider("パーシステンス", persistence, 0f, 1f);
            }

            offset = EditorGUILayout.Vector2Field("オフセット", offset);
            EditorGUILayout.EndVertical();

            // Combine settings
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("合成設定 Combine Settings", EditorStyles.boldLabel);

            combineMode = (CombineMode)EditorGUILayout.EnumPopup("合成モード", combineMode);
            invert = EditorGUILayout.Toggle("反転", invert);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("テクスチャを生成 Generate Texture", GUILayout.Height(30)))
                GenerateCombinedTexture();
        }

        private void GenerateCombinedTexture()
        {
            Mesh mesh = ExtractMesh();
            if (mesh == null)
            {
                EditorUtility.DisplayDialog("エラー", "メッシュを取得できません。", "OK");
                return;
            }

            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(uvChannel, uvs);
            if (uvs.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", $"UV{uvChannel} が存在しません。", "OK");
                return;
            }

            int size = textureSize;

            Color[] maskPixels = new Color[size * size];
            bool[] selectedFlags = islands.Select(i => i.selected).ToArray();
            TriangleRasterizer.RasterizeIslands(maskPixels, size, mesh, uvs, islands, selectedFlags);

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
                    pixels[y * size + x] = new Color(combined, combined, combined);
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
            EditorGUILayout.LabelField("テンプレート Templates", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "テンプレートを選択してレイヤースタックに適用します。\nSelect a template to apply to the layer stack.",
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
                    GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(isSelected ? "\u25CF" : "\u25CB", GUILayout.Width(20)))
                    selectedTemplate = template;

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"{info.nameJP} / {info.nameEN}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(info.descriptionJP, EditorStyles.wordWrappedMiniLabel);
                if (info.requiresMesh)
                    EditorGUILayout.LabelField("* メッシュ必要 Mesh required", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = bgColor;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("テンプレートを適用 Apply Template", GUILayout.Height(30)))
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Canvas header with controls
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("キャンバス Canvas", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            showUVWireframe = GUILayout.Toggle(showUVWireframe,
                new GUIContent("UV", "UVワイヤーフレーム表示 Show UV wireframe"),
                EditorStyles.miniButton, GUILayout.Width(30));

            brushEnabled = GUILayout.Toggle(brushEnabled,
                new GUIContent("Brush", "ブラシツール有効化 Enable brush tool"),
                EditorStyles.miniButton, GUILayout.Width(50));

            if (GUILayout.Button("Fit", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                canvasZoom = 1f;
                canvasPan = Vector2.zero;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // Canvas area
            float canvasDisplaySize = 300f;
            Rect canvasArea = GUILayoutUtility.GetRect(canvasDisplaySize, canvasDisplaySize);

            // Background
            EditorGUI.DrawRect(canvasArea, new Color(0.15f, 0.15f, 0.15f));

            if (previewTexture != null)
            {
                // Calculate zoomed/panned rect
                float texSize = canvasDisplaySize * canvasZoom;
                float offsetX = canvasArea.x + (canvasDisplaySize - texSize) * 0.5f + canvasPan.x;
                float offsetY = canvasArea.y + (canvasDisplaySize - texSize) * 0.5f + canvasPan.y;
                Rect texRect = new Rect(offsetX, offsetY, texSize, texSize);

                // Clip to canvas
                GUI.BeginClip(canvasArea);
                Rect clippedRect = new Rect(
                    texRect.x - canvasArea.x,
                    texRect.y - canvasArea.y,
                    texRect.width, texRect.height);
                EditorGUI.DrawPreviewTexture(clippedRect, previewTexture, null, ScaleMode.ScaleToFit);

                // UV wireframe overlay
                if (showUVWireframe)
                {
                    Mesh mesh = ExtractMesh();
                    if (mesh != null)
                    {
                        List<Vector2> uvs = new List<Vector2>();
                        mesh.GetUVs(uvChannel, uvs);
                        if (uvs.Count > 0)
                            UVWireframeRenderer.DrawWireframe(clippedRect, mesh, uvs);
                    }
                }

                GUI.EndClip();

                // Brush input handling
                if (brushEnabled && layerStack != null && layerStack.ActiveLayer != null)
                {
                    var activeLayer = layerStack.ActiveLayer;
                    if (activeLayer.pixels != null && !activeLayer.locked)
                    {
                        bool modified;
                        BrushCanvasInputHandler.HandleBrushInput(
                            canvasArea, brush, brushSettings,
                            activeLayer.pixels, activeLayer.width, activeLayer.height,
                            out modified);
                        if (modified)
                        {
                            activeLayer.sourceType = MaskTextureLayer.SourceType.Paint;
                            RefreshPreviewFromLayers();
                        }
                    }
                }

                // Brush cursor
                if (brushEnabled && canvasArea.Contains(Event.current.mousePosition))
                {
                    float cursorRadius = brushSettings.size * canvasZoom *
                        (canvasDisplaySize / Mathf.Max(1, layerStack != null ? layerStack.Width : textureSize));
                    BrushCursorRenderer.DrawCursorWithHardness(
                        Event.current.mousePosition, cursorRadius, brushSettings.hardness,
                        Color.white);
                    Repaint();
                }

                // Canvas pan/zoom (only when brush is off)
                if (!brushEnabled)
                    HandleCanvasInput(canvasArea);
            }
            else
            {
                EditorGUI.LabelField(canvasArea,
                    "「テクスチャを生成」を押すとプレビューが表示されます\nClick 'Generate' to see preview",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    { alignment = TextAnchor.MiddleCenter, wordWrap = true });
            }

            // Zoom info
            EditorGUILayout.LabelField($"Zoom: {canvasZoom:F1}x", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void HandleCanvasInput(Rect canvasArea)
        {
            Event e = Event.current;
            if (!canvasArea.Contains(e.mousePosition) && !isDraggingCanvas) return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 2 || (e.button == 0 && e.alt))
                    {
                        isDraggingCanvas = true;
                        lastCanvasMousePos = e.mousePosition;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (isDraggingCanvas)
                    {
                        canvasPan += e.mousePosition - lastCanvasMousePos;
                        lastCanvasMousePos = e.mousePosition;
                        e.Use();
                        Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (isDraggingCanvas)
                    {
                        isDraggingCanvas = false;
                        e.Use();
                    }
                    break;
                case EventType.ScrollWheel:
                    if (canvasArea.Contains(e.mousePosition))
                    {
                        float zoomDelta = -e.delta.y * 0.05f;
                        canvasZoom = Mathf.Clamp(canvasZoom + zoomDelta * canvasZoom, 0.1f, 10f);
                        e.Use();
                        Repaint();
                    }
                    break;
            }
        }

        // ================================================================
        // Filter Panel
        // ================================================================

        private void DrawFilterPanel()
        {
            filterFoldout = NataneToonShaderGUIUtility.DrawFoldoutHeader("フィルター Filters", filterFoldout);
            if (!filterFoldout) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool hasLayer = layerStack != null && layerStack.ActiveLayer != null
                && layerStack.ActiveLayer.pixels != null;

            // Gaussian Blur
            EditorGUILayout.BeginHorizontal();
            filterBlurSigma = EditorGUILayout.Slider("ブラー Blur (sigma)", filterBlurSigma, 0.1f, 20f);
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button("適用", GUILayout.Width(40)))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    MaskTextureFilters.GaussianBlur(pixels, w, h, filterBlurSigma));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Levels
            EditorGUILayout.LabelField("レベル補正 Levels", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            filterLevelInputMin = EditorGUILayout.FloatField("入力Min", filterLevelInputMin, GUILayout.Width(120));
            filterLevelInputMax = EditorGUILayout.FloatField("入力Max", filterLevelInputMax, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            filterLevelGamma = EditorGUILayout.Slider("ガンマ Gamma", filterLevelGamma, 0.01f, 10f);
            EditorGUILayout.BeginHorizontal();
            filterLevelOutputMin = EditorGUILayout.FloatField("出力Min", filterLevelOutputMin, GUILayout.Width(120));
            filterLevelOutputMax = EditorGUILayout.FloatField("出力Max", filterLevelOutputMax, GUILayout.Width(120));
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button("適用", GUILayout.Width(40)))
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
            filterEdgeStrength = EditorGUILayout.Slider("エッジ検出 Edge", filterEdgeStrength, 0.1f, 5f);
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button("適用", GUILayout.Width(40)))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    MaskTextureFilters.SobelEdge(pixels, w, h, filterEdgeStrength));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Sharpen
            EditorGUILayout.BeginHorizontal();
            filterSharpenAmount = EditorGUILayout.Slider("シャープ Sharpen", filterSharpenAmount, 0f, 3f);
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button("適用", GUILayout.Width(40)))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    MaskTextureFilters.Sharpen(pixels, w, h, filterSharpenAmount, filterSharpenSigma));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Threshold
            EditorGUILayout.BeginHorizontal();
            filterThreshold = EditorGUILayout.Slider("二値化 Threshold", filterThreshold, 0f, 1f);
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button("適用", GUILayout.Width(40)))
            {
                ApplyFilterToActiveLayer((pixels, w, h) =>
                    MaskTextureFilters.Threshold(pixels, w, h, filterThreshold));
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
            RefreshPreviewFromLayers();
        }

        // ================================================================
        // 3D Preview
        // ================================================================

        private void Draw3DPreviewSection()
        {
            show3DPreview = NataneToonShaderGUIUtility.DrawFoldoutHeader(
                "3Dプレビュー 3D Preview", show3DPreview);

            if (!show3DPreview) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Mesh mesh = ExtractMesh();
            if (mesh == null)
            {
                EditorGUILayout.HelpBox(
                    "3Dプレビューにはメッシュが必要です。メッシュソースを設定してください。\n" +
                    "Mesh is required for 3D preview. Set a mesh source.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            if (preview3D == null)
                preview3D = new MaskTexture3DPreview();

            preview3D.SetMesh(mesh);
            if (previewTexture != null)
                preview3D.SetTexture(previewTexture);

            preview3D.DrawPreviewUI();

            Rect previewRect = GUILayoutUtility.GetRect(300, 200);
            preview3D.HandleInput(previewRect);
            preview3D.DrawPreview(previewRect);

            if (Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDrag)
                Repaint();

            EditorGUILayout.EndVertical();
        }

        // ================================================================
        // Material Assignment
        // ================================================================

        private void DrawMaterialAssignment()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("マテリアル割当 Material Assignment", EditorStyles.boldLabel);

            targetMaterial = (Material)EditorGUILayout.ObjectField("ターゲットマテリアル", targetMaterial, typeof(Material), false);

            if (targetMaterial != null)
            {
                List<string> availableProps = new List<string>();
                List<string> availableLabels = new List<string>();

                foreach (string prop in maskProperties)
                {
                    if (targetMaterial.HasProperty(prop))
                    {
                        availableProps.Add(prop);
                        availableLabels.Add(prop);
                    }
                }

                if (availableProps.Count > 0)
                {
                    selectedPropertyIndex = Mathf.Clamp(selectedPropertyIndex, 0, availableProps.Count - 1);
                    selectedPropertyIndex = EditorGUILayout.Popup("割当先プロパティ", selectedPropertyIndex, availableLabels.ToArray());
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "対応するテクスチャプロパティが見つかりません。\nNo compatible texture properties found.",
                        MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ================================================================
        // Helpers
        // ================================================================

        private void DrawMeshSourceField()
        {
            EditorGUI.BeginChangeCheck();
            meshSource = EditorGUILayout.ObjectField("メッシュソース Mesh Source", meshSource, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
                islands.Clear();

            uvChannel = EditorGUILayout.IntPopup("UVチャンネル", uvChannel,
                new[] { "UV0", "UV1", "UV2", "UV3" }, new[] { 0, 1, 2, 3 });
        }

        private void EnsureLayerStack()
        {
            if (layerStack == null)
                layerStack = new MaskLayerStack(textureSize, textureSize);
            if (layerStack.Layers.Count == 0)
                layerStack.AddLayer("Base Layer");
        }

        private void RefreshPreviewFromLayers()
        {
            if (layerStack == null) return;
            Color[] flattened = layerStack.Flatten();
            if (flattened != null)
            {
                UpdatePreview(flattened, layerStack.Width);
                if (preview3D != null)
                    preview3D.MarkDirty();
            }
        }

        private void UpdatePreview(Color[] pixels, int size)
        {
            if (previewTexture != null)
                DestroyImmediate(previewTexture);

            previewTexture = new Texture2D(size, size, TextureFormat.RGB24, false);
            previewTexture.filterMode = FilterMode.Bilinear;
            previewTexture.wrapMode = TextureWrapMode.Clamp;
            previewTexture.SetPixels(pixels);
            previewTexture.Apply();
            Repaint();
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

        private class UVIsland
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
            public static List<UVIsland> DetectIslands(Mesh mesh, List<Vector2> uvs)
            {
                int[] triangles = mesh.triangles;
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
            public static void RasterizeIslands(Color[] pixels, int size, Mesh mesh, List<Vector2> uvs,
                List<UVIsland> islands, bool[] selectedFlags)
            {
                int[] triangles = mesh.triangles;
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
                        pixels[i] = Color.black;
                        continue;
                    }

                    float value = distance[i] * invWidth;

                    if (direction == GradientDirection.Outward)
                        value = 1f - value;

                    pixels[i] = new Color(value, value, value);
                }
            }
        }
    }
}
