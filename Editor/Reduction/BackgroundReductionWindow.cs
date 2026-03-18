using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reduction.Editor
{
    public sealed class BackgroundReductionWindow : EditorWindow
    {
        // --- Shader paths ---
        private const string DepthShaderName  = "Hidden/Reduction/BakeDepth";
        private const string NormalShaderName  = "Hidden/Reduction/BakeNormal";
        private const string ParallaxShaderName = "Custom/ParallaxBackground";

        private const string DefaultOutputRoot = "Assets/reduction/Generated";
        private const string TextureFolderName = "Textures";
        private const string MeshFolderName    = "Meshes";
        private const string MaterialFolderName = "Materials";
        private const string PrefabFolderName  = "Prefabs";

        // --- View Direction ---
        private enum ViewDirection { Front, Back, Left, Right, Top, Custom }

        private static readonly Vector3[] ViewForwards =
        {
            Vector3.forward, Vector3.back,
            Vector3.left,    Vector3.right,
            Vector3.down,    Vector3.forward
        };

        // --- Mesh Shape ---
        private enum MeshShape { Cylinder, Sphere, Plane }

        // --- Resolution options ---
        private static readonly int[] Resolutions = { 256, 512, 1024, 2048, 4096 };
        private static readonly string[] ResolutionLabels = { "256", "512", "1024", "2048", "4096" };

        // =====================================================================
        //  Serialized UI state
        // =====================================================================

        // Source
        [SerializeField] private GameObject sourceObject;
        [SerializeField] private ViewDirection viewDirection = ViewDirection.Front;
        [SerializeField] private float customYAngle;

        // Camera
        [SerializeField] private bool autoDistance = true;
        [SerializeField] private float manualDistance = 50f;
        [SerializeField] private float fieldOfView = 60f;
        [SerializeField] private float nearClip = 0.1f;
        [SerializeField] private float farClip = 500f;

        // Mesh
        [SerializeField] private MeshShape meshShape = MeshShape.Cylinder;
        [SerializeField] private float meshArc = 180f;
        [SerializeField] private float meshVArc = 90f;
        [SerializeField] private float meshHeight = 10f;
        [SerializeField] private float meshRadius = 50f;
        [SerializeField] private float meshWidth = 20f;
        [SerializeField] private float meshPlaneHeight = 10f;
        [SerializeField] private int subdivU = 32;
        [SerializeField] private int subdivV = 16;

        // Texture
        [SerializeField] private int resolutionIndex = 3; // default 2048
        [SerializeField] private bool useManualTextures;
        [SerializeField] private Texture2D manualColorTex;
        [SerializeField] private Texture2D manualNormalTex;
        [SerializeField] private Texture2D manualHeightTex;

        // Output
        [SerializeField] private string setName = "MyBackground";
        [SerializeField] private string outputRootFolder = DefaultOutputRoot;
        [SerializeField] private bool overwriteExisting = true;

        // Preview
        private Texture2D previewColor;
        private Texture2D previewNormal;
        private Texture2D previewHeight;

        private Vector2 scrollPos;

        // =====================================================================
        //  Menu
        // =====================================================================
        [MenuItem("Tools/Reduction/\u80CC\u666F\u8EFD\u91CF\u5316\u30C4\u30FC\u30EB")]
        private static void OpenWindow()
        {
            var window = GetWindow<BackgroundReductionWindow>("\u80CC\u666F\u8EFD\u91CF\u5316");
            window.minSize = new Vector2(460f, 600f);
            window.Show();
        }

        // =====================================================================
        //  OnGUI
        // =====================================================================
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("\u80CC\u666F\u8EFD\u91CF\u5316\u30C4\u30FC\u30EB", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "\u9060\u65B9\u80CC\u666F\u306E\u30D7\u30EC\u30CF\u30D6\u3092\u66F2\u9762\u30E1\u30C3\u30B7\u30E5\uFF0B\u8996\u5DEE\u30DE\u30C3\u30D4\u30F3\u30B0\u306B\u7F6E\u304D\u63DB\u3048\u3001\n" +
                "\u30DD\u30EA\u30B4\u30F3\u6570\u3092\u524A\u6E1B\u3057\u307E\u3059\u3002",
                MessageType.Info);

            DrawSourceSection();
            EditorGUILayout.Space(4f);
            DrawCameraSection();
            EditorGUILayout.Space(4f);
            DrawMeshSection();
            EditorGUILayout.Space(4f);
            DrawTextureSection();
            EditorGUILayout.Space(4f);
            DrawOutputSection();
            EditorGUILayout.Space(8f);
            DrawActionButtons();
            EditorGUILayout.Space(8f);
            DrawPreview();

            EditorGUILayout.EndScrollView();
        }

        // =====================================================================
        //  UI Sections
        // =====================================================================

        private void DrawSourceSection()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("\u30BD\u30FC\u30B9\u8A2D\u5B9A", EditorStyles.boldLabel);
                sourceObject = (GameObject)EditorGUILayout.ObjectField(
                    "\u5BFE\u8C61\u30AA\u30D6\u30B8\u30A7\u30AF\u30C8", sourceObject, typeof(GameObject), true);
                viewDirection = (ViewDirection)EditorGUILayout.EnumPopup("\u8996\u70B9\u65B9\u5411", viewDirection);
                if (viewDirection == ViewDirection.Custom)
                {
                    customYAngle = EditorGUILayout.Slider("\u30AB\u30B9\u30BF\u30E0\u89D2\u5EA6 (Y\u8EF8)", customYAngle, 0f, 360f);
                }
            }
        }

        private void DrawCameraSection()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("\u30AB\u30E1\u30E9\u8A2D\u5B9A", EditorStyles.boldLabel);
                autoDistance = EditorGUILayout.Toggle("\u8DDD\u96E2\u81EA\u52D5\u8ABF\u6574", autoDistance);
                using (new EditorGUI.DisabledScope(autoDistance))
                {
                    manualDistance = EditorGUILayout.FloatField("\u624B\u52D5\u8DDD\u96E2", manualDistance);
                }
                fieldOfView = EditorGUILayout.Slider("FOV", fieldOfView, 10f, 120f);
                nearClip = EditorGUILayout.FloatField("Near Clip", nearClip);
                farClip = EditorGUILayout.FloatField("Far Clip", farClip);
            }
        }

        private void DrawMeshSection()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("\u30E1\u30C3\u30B7\u30E5\u8A2D\u5B9A", EditorStyles.boldLabel);
                meshShape = (MeshShape)EditorGUILayout.EnumPopup("\u5F62\u72B6", meshShape);

                switch (meshShape)
                {
                    case MeshShape.Cylinder:
                        meshArc    = EditorGUILayout.Slider("\u5F27\u89D2\u5EA6", meshArc, 30f, 360f);
                        meshHeight = EditorGUILayout.FloatField("\u9AD8\u3055", meshHeight);
                        meshRadius = EditorGUILayout.FloatField("\u534A\u5F84", meshRadius);
                        break;
                    case MeshShape.Sphere:
                        meshArc  = EditorGUILayout.Slider("\u6C34\u5E73\u5F27\u89D2\u5EA6", meshArc, 30f, 360f);
                        meshVArc = EditorGUILayout.Slider("\u5782\u76F4\u5F27\u89D2\u5EA6", meshVArc, 10f, 180f);
                        meshRadius = EditorGUILayout.FloatField("\u534A\u5F84", meshRadius);
                        break;
                    case MeshShape.Plane:
                        meshWidth       = EditorGUILayout.FloatField("\u5E45", meshWidth);
                        meshPlaneHeight = EditorGUILayout.FloatField("\u9AD8\u3055", meshPlaneHeight);
                        break;
                }

                subdivU = EditorGUILayout.IntSlider("\u6C34\u5E73\u5206\u5272\u6570", subdivU, 2, 128);
                subdivV = EditorGUILayout.IntSlider("\u5782\u76F4\u5206\u5272\u6570", subdivV, 2, 128);
            }
        }

        private void DrawTextureSection()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("\u30C6\u30AF\u30B9\u30C1\u30E3\u8A2D\u5B9A", EditorStyles.boldLabel);
                resolutionIndex = EditorGUILayout.Popup("\u89E3\u50CF\u5EA6", resolutionIndex, ResolutionLabels);

                useManualTextures = EditorGUILayout.Toggle("\u624B\u52D5\u30C6\u30AF\u30B9\u30C1\u30E3\u6307\u5B9A", useManualTextures);
                if (useManualTextures)
                {
                    manualColorTex  = (Texture2D)EditorGUILayout.ObjectField("Color", manualColorTex, typeof(Texture2D), false);
                    manualNormalTex = (Texture2D)EditorGUILayout.ObjectField("Normal", manualNormalTex, typeof(Texture2D), false);
                    manualHeightTex = (Texture2D)EditorGUILayout.ObjectField("Height", manualHeightTex, typeof(Texture2D), false);
                }
            }
        }

        private void DrawOutputSection()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("\u51FA\u529B\u8A2D\u5B9A", EditorStyles.boldLabel);
                setName = EditorGUILayout.TextField("\u30BB\u30C3\u30C8\u540D", setName);
                outputRootFolder = EditorGUILayout.TextField("\u51FA\u529B\u5148\u30EB\u30FC\u30C8", outputRootFolder);
                overwriteExisting = EditorGUILayout.Toggle("\u65E2\u5B58\u3092\u4E0A\u66F8\u304D", overwriteExisting);
            }
        }

        private void DrawActionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("\u30C6\u30AF\u30B9\u30C1\u30E3\u3092\u30D9\u30A4\u30AF", GUILayout.Height(30f)))
                {
                    BakeTextures();
                }
                if (GUILayout.Button("\u30E1\u30C3\u30B7\u30E5\u3092\u751F\u6210", GUILayout.Height(30f)))
                {
                    GenerateMeshAsset();
                }
                if (GUILayout.Button("Prefab\u3092\u4F5C\u6210", GUILayout.Height(30f)))
                {
                    CreatePrefab();
                }
            }
        }

        private void DrawPreview()
        {
            if (previewColor == null && previewNormal == null && previewHeight == null)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("\u30D7\u30EC\u30D3\u30E5\u30FC", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawPreviewThumbnail("Color", previewColor);
                    DrawPreviewThumbnail("Normal", previewNormal);
                    DrawPreviewThumbnail("Height", previewHeight);
                }
            }
        }

        private static void DrawPreviewThumbnail(string label, Texture2D tex)
        {
            if (tex == null) return;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(128)))
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(128));
                Rect r = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
                EditorGUI.DrawPreviewTexture(r, tex);
            }
        }

        // =====================================================================
        //  View Direction Helpers
        // =====================================================================

        private Vector3 GetViewForward()
        {
            if (viewDirection == ViewDirection.Custom)
            {
                float rad = customYAngle * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            }
            return ViewForwards[(int)viewDirection];
        }

        private Bounds ComputeSourceBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(obj.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        // =====================================================================
        //  Bake Textures
        // =====================================================================

        private void BakeTextures()
        {
            // --- Validation ---
            if (sourceObject == null)
            {
                EditorUtility.DisplayDialog("\u30D9\u30A4\u30AF\u5931\u6557", "\u5BFE\u8C61\u30AA\u30D6\u30B8\u30A7\u30AF\u30C8\u3092\u6307\u5B9A\u3057\u3066\u304F\u3060\u3055\u3044\u3002", "OK");
                return;
            }

            Shader depthShader = Shader.Find(DepthShaderName);
            Shader normalShader = Shader.Find(NormalShaderName);

            if (depthShader == null || normalShader == null)
            {
                EditorUtility.DisplayDialog("\u30D9\u30A4\u30AF\u5931\u6557",
                    $"\u30D9\u30A4\u30AF\u7528\u30B7\u30A7\u30FC\u30C0\u30FC\u304C\u898B\u3064\u304B\u308A\u307E\u305B\u3093\u3002\n{DepthShaderName}\n{NormalShaderName}", "OK");
                return;
            }

            string safeSetName = SanitizeFileName(setName);
            if (string.IsNullOrWhiteSpace(safeSetName)) safeSetName = "MyBackground";

            string normalizedOutput = NormalizeAssetPath(outputRootFolder);
            if (!IsAssetsPath(normalizedOutput))
            {
                EditorUtility.DisplayDialog("\u30D9\u30A4\u30AF\u5931\u6557", "\u51FA\u529B\u5148\u306F Assets \u914D\u4E0B\u3092\u6307\u5B9A\u3057\u3066\u304F\u3060\u3055\u3044\u3002", "OK");
                return;
            }

            string setFolder = CombineAssetPath(normalizedOutput, safeSetName);
            string textureFolder = CombineAssetPath(setFolder, TextureFolderName);
            EnsureFolder(textureFolder);

            int resolution = Resolutions[Mathf.Clamp(resolutionIndex, 0, Resolutions.Length - 1)];

            // --- Instantiate source ---
            GameObject instance = null;
            Camera tempCamera = null;
            RenderTexture colorRT = null;
            RenderTexture normalRT = null;
            RenderTexture depthRT = null;

            try
            {
                EditorUtility.DisplayProgressBar("\u30D9\u30A4\u30AF\u4E2D", "\u30BD\u30FC\u30B9\u30AA\u30D6\u30B8\u30A7\u30AF\u30C8\u3092\u6E96\u5099\u4E2D...", 0f);

                instance = Instantiate(sourceObject);
                instance.hideFlags = HideFlags.HideAndDontSave;

                // Compute bounds
                Bounds bounds = ComputeSourceBounds(instance);
                float boundsRadius = bounds.extents.magnitude;

                // Camera setup
                Vector3 viewForward = GetViewForward();
                float fovRad = fieldOfView * Mathf.Deg2Rad * 0.5f;
                float distance = autoDistance
                    ? boundsRadius / Mathf.Tan(fovRad) * 1.1f
                    : manualDistance;

                Vector3 camPos = bounds.center - viewForward * distance;

                var camGo = new GameObject("__ReductionBakeCamera__");
                camGo.hideFlags = HideFlags.HideAndDontSave;
                tempCamera = camGo.AddComponent<Camera>();
                tempCamera.enabled = false;
                tempCamera.fieldOfView = fieldOfView;
                tempCamera.aspect = 1f;
                tempCamera.nearClipPlane = Mathf.Max(0.01f, nearClip);
                tempCamera.farClipPlane = Mathf.Max(nearClip + 1f, farClip);
                tempCamera.clearFlags = CameraClearFlags.SolidColor;
                tempCamera.backgroundColor = Color.black;
                tempCamera.transform.position = camPos;
                tempCamera.transform.rotation = Quaternion.LookRotation(viewForward, Vector3.up);

                colorRT  = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
                normalRT = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
                depthRT  = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.RFloat);

                // --- Pass 1: Color ---
                EditorUtility.DisplayProgressBar("\u30D9\u30A4\u30AF\u4E2D", "Color\u30C6\u30AF\u30B9\u30C1\u30E3\u3092\u30AD\u30E3\u30D7\u30C1\u30E3\u4E2D...", 0.2f);
                tempCamera.targetTexture = colorRT;
                tempCamera.Render();

                string colorPath = CombineAssetPath(textureFolder, $"{safeSetName}_Color.png");
                SaveRenderTextureToPNG(colorRT, colorPath);

                // --- Pass 2: Normal ---
                EditorUtility.DisplayProgressBar("\u30D9\u30A4\u30AF\u4E2D", "Normal\u30C6\u30AF\u30B9\u30C1\u30E3\u3092\u30AD\u30E3\u30D7\u30C1\u30E3\u4E2D...", 0.5f);
                tempCamera.targetTexture = normalRT;
                tempCamera.RenderWithShader(normalShader, "RenderType");

                string normalPath = CombineAssetPath(textureFolder, $"{safeSetName}_Normal.png");
                SaveRenderTextureToPNG(normalRT, normalPath);
                ConfigureNormalTextureImport(normalPath);

                // --- Pass 3: Depth ---
                EditorUtility.DisplayProgressBar("\u30D9\u30A4\u30AF\u4E2D", "Height\u30C6\u30AF\u30B9\u30C1\u30E3\u3092\u30AD\u30E3\u30D7\u30C1\u30E3\u4E2D...", 0.8f);
                tempCamera.targetTexture = depthRT;
                tempCamera.RenderWithShader(depthShader, "RenderType");

                string depthPath = CombineAssetPath(textureFolder, $"{safeSetName}_Height.exr");
                SaveRenderTextureToEXR(depthRT, depthPath);
                ConfigureDepthTextureImport(depthPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Load preview textures
                previewColor  = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
                previewNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                previewHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(depthPath);

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("\u30D9\u30A4\u30AF\u5B8C\u4E86",
                    $"\u30C6\u30AF\u30B9\u30C1\u30E3\u306E\u30D9\u30A4\u30AF\u304C\u5B8C\u4E86\u3057\u307E\u3057\u305F\u3002\n\u51FA\u529B\u5148: {textureFolder}\n\n" +
                    $"Color: {safeSetName}_Color.png\nNormal: {safeSetName}_Normal.png\nHeight: {safeSetName}_Height.exr",
                    "OK");

                Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BackgroundReduction] \u30D9\u30A4\u30AF\u5931\u6557: {ex}");
                EditorUtility.DisplayDialog("\u30D9\u30A4\u30AF\u5931\u6557", ex.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (instance != null) DestroyImmediate(instance);
                if (tempCamera != null) DestroyImmediate(tempCamera.gameObject);
                ReleaseRT(colorRT);
                ReleaseRT(normalRT);
                ReleaseRT(depthRT);
            }
        }

        // =====================================================================
        //  Generate Mesh Asset
        // =====================================================================

        private void GenerateMeshAsset()
        {
            string safeSetName = SanitizeFileName(setName);
            if (string.IsNullOrWhiteSpace(safeSetName)) safeSetName = "MyBackground";

            string normalizedOutput = NormalizeAssetPath(outputRootFolder);
            if (!IsAssetsPath(normalizedOutput))
            {
                EditorUtility.DisplayDialog("\u751F\u6210\u5931\u6557", "\u51FA\u529B\u5148\u306F Assets \u914D\u4E0B\u3092\u6307\u5B9A\u3057\u3066\u304F\u3060\u3055\u3044\u3002", "OK");
                return;
            }

            string setFolder = CombineAssetPath(normalizedOutput, safeSetName);
            string meshFolder = CombineAssetPath(setFolder, MeshFolderName);
            EnsureFolder(meshFolder);

            Mesh mesh;
            string shapeSuffix;

            switch (meshShape)
            {
                case MeshShape.Cylinder:
                    mesh = CurvedMeshGenerator.GenerateCylinder(meshArc, meshHeight, meshRadius, subdivU, subdivV);
                    shapeSuffix = "Cylinder";
                    break;
                case MeshShape.Sphere:
                    mesh = CurvedMeshGenerator.GenerateSpherePatch(meshArc, meshVArc, meshRadius, subdivU, subdivV);
                    shapeSuffix = "Sphere";
                    break;
                case MeshShape.Plane:
                default:
                    mesh = CurvedMeshGenerator.GeneratePlane(meshWidth, meshPlaneHeight, subdivU, subdivV);
                    shapeSuffix = "Plane";
                    break;
            }

            string meshPath = CombineAssetPath(meshFolder, $"{safeSetName}_{shapeSuffix}.asset");

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null && overwriteExisting)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                DestroyImmediate(mesh);
                mesh = existing;
                EditorUtility.SetDirty(mesh);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(mesh);
            EditorUtility.DisplayDialog("\u30E1\u30C3\u30B7\u30E5\u751F\u6210\u5B8C\u4E86",
                $"\u30E1\u30C3\u30B7\u30E5\u3092\u751F\u6210\u3057\u307E\u3057\u305F\u3002\n{meshPath}\n\n" +
                $"\u5F62\u72B6: {shapeSuffix}\n\u9802\u70B9\u6570: {mesh.vertexCount}\n\u4E09\u89D2\u5F62\u6570: {mesh.triangles.Length / 3}",
                "OK");
        }

        // =====================================================================
        //  Create Prefab
        // =====================================================================

        private void CreatePrefab()
        {
            string safeSetName = SanitizeFileName(setName);
            if (string.IsNullOrWhiteSpace(safeSetName)) safeSetName = "MyBackground";

            string normalizedOutput = NormalizeAssetPath(outputRootFolder);
            if (!IsAssetsPath(normalizedOutput))
            {
                EditorUtility.DisplayDialog("\u4F5C\u6210\u5931\u6557", "\u51FA\u529B\u5148\u306F Assets \u914D\u4E0B\u3092\u6307\u5B9A\u3057\u3066\u304F\u3060\u3055\u3044\u3002", "OK");
                return;
            }

            Shader parallaxShader = Shader.Find(ParallaxShaderName);
            if (parallaxShader == null)
            {
                EditorUtility.DisplayDialog("\u4F5C\u6210\u5931\u6557",
                    $"\u30B7\u30A7\u30FC\u30C0\u30FC\u304C\u898B\u3064\u304B\u308A\u307E\u305B\u3093: {ParallaxShaderName}", "OK");
                return;
            }

            string setFolder = CombineAssetPath(normalizedOutput, safeSetName);
            string meshFolder = CombineAssetPath(setFolder, MeshFolderName);
            string textureFolder = CombineAssetPath(setFolder, TextureFolderName);
            string materialFolder = CombineAssetPath(setFolder, MaterialFolderName);
            string prefabFolder = CombineAssetPath(setFolder, PrefabFolderName);

            // Find mesh
            string shapeSuffix = meshShape == MeshShape.Cylinder ? "Cylinder"
                               : meshShape == MeshShape.Sphere   ? "Sphere"
                               : "Plane";
            string meshPath = CombineAssetPath(meshFolder, $"{safeSetName}_{shapeSuffix}.asset");
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                EditorUtility.DisplayDialog("\u4F5C\u6210\u5931\u6557",
                    $"\u30E1\u30C3\u30B7\u30E5\u304C\u898B\u3064\u304B\u308A\u307E\u305B\u3093\u3002\u5148\u306B\u300C\u30E1\u30C3\u30B7\u30E5\u3092\u751F\u6210\u300D\u3092\u5B9F\u884C\u3057\u3066\u304F\u3060\u3055\u3044\u3002\n{meshPath}", "OK");
                return;
            }

            // Find textures (manual or baked)
            Texture2D colorTex, normalTex, heightTex;
            if (useManualTextures)
            {
                colorTex  = manualColorTex;
                normalTex = manualNormalTex;
                heightTex = manualHeightTex;
            }
            else
            {
                string colorPath  = CombineAssetPath(textureFolder, $"{safeSetName}_Color.png");
                string normalPath = CombineAssetPath(textureFolder, $"{safeSetName}_Normal.png");
                string heightPath = CombineAssetPath(textureFolder, $"{safeSetName}_Height.exr");

                colorTex  = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
                normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                heightTex = AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath);

                if (colorTex == null || heightTex == null)
                {
                    EditorUtility.DisplayDialog("\u4F5C\u6210\u5931\u6557",
                        "\u30C6\u30AF\u30B9\u30C1\u30E3\u304C\u898B\u3064\u304B\u308A\u307E\u305B\u3093\u3002\u5148\u306B\u300C\u30C6\u30AF\u30B9\u30C1\u30E3\u3092\u30D9\u30A4\u30AF\u300D\u3092\u5B9F\u884C\u3057\u3066\u304F\u3060\u3055\u3044\u3002", "OK");
                    return;
                }
            }

            // Create material
            EnsureFolder(materialFolder);
            string matPath = CombineAssetPath(materialFolder, $"{safeSetName}_Mat.mat");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null || overwriteExisting)
            {
                if (mat == null)
                {
                    mat = new Material(parallaxShader);
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else
                {
                    mat.shader = parallaxShader;
                }

                if (colorTex != null)  mat.SetTexture("_MainTex", colorTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (heightTex != null) mat.SetTexture("_HeightMap", heightTex);

                EditorUtility.SetDirty(mat);
            }

            // Create prefab
            EnsureFolder(prefabFolder);
            string prefabPath = CombineAssetPath(prefabFolder, $"{safeSetName}.prefab");

            var go = new GameObject(safeSetName);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null && overwriteExisting)
            {
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            }
            else if (existingPrefab == null)
            {
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            }

            DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;

            EditorUtility.DisplayDialog("Prefab\u4F5C\u6210\u5B8C\u4E86",
                $"Prefab\u3092\u4F5C\u6210\u3057\u307E\u3057\u305F\u3002\n{prefabPath}\n\n" +
                $"\u30E1\u30C3\u30B7\u30E5: {meshPath}\n\u30DE\u30C6\u30EA\u30A2\u30EB: {matPath}",
                "OK");
        }

        // =====================================================================
        //  Texture I/O (same pattern as ParallaxBoxCaptureWindow)
        // =====================================================================

        private static void SaveRenderTextureToPNG(RenderTexture rt, string assetPath)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;

            byte[] bytes = tex.EncodeToPNG();
            DestroyImmediate(tex);

            string diskPath = AssetPathToDiskPath(assetPath);
            File.WriteAllBytes(diskPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void SaveRenderTextureToEXR(RenderTexture rt, string assetPath)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;

            byte[] bytes = tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            DestroyImmediate(tex);

            string diskPath = AssetPathToDiskPath(assetPath);
            File.WriteAllBytes(diskPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureDepthTextureImport(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;

            var platformSettings = importer.GetDefaultPlatformTextureSettings();
            platformSettings.format = TextureImporterFormat.RFloat;
            platformSettings.overridden = true;
            importer.SetPlatformTextureSettings(platformSettings);

            importer.SaveAndReimport();
        }

        private static void ConfigureNormalTextureImport(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.sRGBTexture = false;
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            importer.SaveAndReimport();
        }

        // =====================================================================
        //  Utility helpers (same pattern as ParallaxBoxCaptureWindow)
        // =====================================================================

        private static void ReleaseRT(RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            DestroyImmediate(rt);
        }

        private static string AssetPathToDiskPath(string assetPath)
        {
            string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool IsAssetsPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && path.StartsWith("Assets");
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim().TrimEnd('/');
        }

        private static string CombineAssetPath(string root, string leaf)
        {
            return NormalizeAssetPath($"{NormalizeAssetPath(root)}/{leaf}");
        }

        private static string SanitizeFileName(string input)
        {
            string value = (input ?? string.Empty).Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid.ToString(), string.Empty);
            }
            return value;
        }

        private static void EnsureFolder(string folderPath)
        {
            string normalized = NormalizeAssetPath(folderPath);
            if (string.IsNullOrEmpty(normalized) || normalized == "Assets" || AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string parent = NormalizeAssetPath(Path.GetDirectoryName(normalized));
            string folderName = Path.GetFileName(normalized);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Vector3 EstimateBoundsCenter(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return obj.transform.position;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }
    }
}
