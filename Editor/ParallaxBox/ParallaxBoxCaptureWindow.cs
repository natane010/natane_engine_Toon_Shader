using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ParallaxBox.Editor
{
    public sealed class ParallaxBoxCaptureWindow : EditorWindow
    {
        private const string DepthShaderName = "Hidden/ParallaxBox/DepthCapture";
        private const string InteriorShaderName = "Custom/ParallaxBoxInterior";
        private const string DefaultOutputRoot = "Assets/ParallaxBox/Generated";
        private const string TextureFolderName = "Textures";
        private const string MaterialFolderName = "Materials";

        private static readonly string[] FaceNames = { "PosX", "NegX", "PosY", "NegY", "PosZ", "NegZ" };

        private static readonly Vector3[] FaceForwards =
        {
            Vector3.right,   Vector3.left,
            Vector3.up,      Vector3.down,
            Vector3.forward, Vector3.back
        };

        private static readonly Vector3[] FaceUpwards =
        {
            Vector3.up, Vector3.up,
            Vector3.forward, Vector3.back,
            Vector3.up, Vector3.up
        };

        // --- Serialized UI state ---
        [SerializeField] private Transform capturePosition;
        [SerializeField] private bool autoDetectCapturePos = true;
        [SerializeField] private int resolutionIndex = 2; // 0=512,1=1024,2=2048,3=4096
        [SerializeField] private float farPlane = 100f;
        [SerializeField] private LayerMask excludeLayerMask;

        [SerializeField] private GameObject boxObject;
        [SerializeField] private Vector3 boxSize = new Vector3(10f, 10f, 10f);
        [SerializeField] private bool autoDetectSize = true;

        [SerializeField] private string setName = "MyBox";
        [SerializeField] private string outputRootFolder = DefaultOutputRoot;
        [SerializeField] private bool overwriteExisting = true;
        [SerializeField] private bool pingAfterCreate = true;

        private Vector2 scrollPos;

        private static readonly int[] Resolutions = { 512, 1024, 2048, 4096 };
        private static readonly string[] ResolutionLabels = { "512", "1024", "2048", "4096" };

        [MenuItem("Tools/ParallaxBox/視差ボックス背景キャプチャ")]
        private static void OpenWindow()
        {
            var window = GetWindow<ParallaxBoxCaptureWindow>("ParallaxBox Capture");
            window.minSize = new Vector2(440f, 480f);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("視差ボックス背景キャプチャ", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ボックス中心から6方向のカラー+深度テクスチャをキャプチャし、\n" +
                "視差マッピング用マテリアルを自動生成します。",
                MessageType.Info);

            // --- Capture settings ---
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("キャプチャ設定", EditorStyles.boldLabel);
                autoDetectCapturePos = EditorGUILayout.Toggle("位置をメッシュ中心から自動推定", autoDetectCapturePos);

                using (new EditorGUI.DisabledScope(autoDetectCapturePos && boxObject != null))
                {
                    capturePosition = (Transform)EditorGUILayout.ObjectField(
                        "キャプチャ位置", capturePosition, typeof(Transform), true);
                }

                if (autoDetectCapturePos && boxObject != null)
                {
                    Vector3 previewCenter = EstimateBoundsCenter(boxObject);
                    EditorGUILayout.HelpBox(
                        $"自動推定位置: ({previewCenter.x:F2}, {previewCenter.y:F2}, {previewCenter.z:F2})",
                        MessageType.None);
                }

                resolutionIndex = EditorGUILayout.Popup("テクスチャ解像度", resolutionIndex, ResolutionLabels);
                farPlane = EditorGUILayout.FloatField("Far Plane", farPlane);
                excludeLayerMask = EditorMaskField("除外レイヤー", excludeLayerMask);
            }

            EditorGUILayout.Space(4f);

            // --- Box settings ---
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("ボックス設定", EditorStyles.boldLabel);
                boxObject = (GameObject)EditorGUILayout.ObjectField(
                    "対象GameObject", boxObject, typeof(GameObject), true);
                autoDetectSize = EditorGUILayout.Toggle("サイズ自動検出", autoDetectSize);

                using (new EditorGUI.DisabledScope(autoDetectSize && boxObject != null))
                {
                    boxSize = EditorGUILayout.Vector3Field("ボックスサイズ", boxSize);
                }
            }

            EditorGUILayout.Space(4f);

            // --- Output settings ---
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("出力設定", EditorStyles.boldLabel);
                setName = EditorGUILayout.TextField("セット名", setName);
                outputRootFolder = EditorGUILayout.TextField("出力先ルート", outputRootFolder);
                overwriteExisting = EditorGUILayout.Toggle("既存を上書き", overwriteExisting);
                pingAfterCreate = EditorGUILayout.Toggle("生成後にPing", pingAfterCreate);
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("キャプチャを実行", GUILayout.Height(34f)))
            {
                ExecuteCapture();
            }

            EditorGUILayout.EndScrollView();
        }

        // Wrapper for layer mask field (Editor-only API)
        private static LayerMask EditorMaskField(string label, LayerMask mask)
        {
            // InternalEditorUtility not always available; use a simple int field.
            mask.value = EditorGUILayout.MaskField(label, mask.value,
                UnityEditorInternal.InternalEditorUtility.layers);
            return mask;
        }

        // ==================================================================
        //  Main capture flow
        // ==================================================================
        private void ExecuteCapture()
        {
            // --- Validation ---
            bool useAutoPos = autoDetectCapturePos && boxObject != null;
            if (!useAutoPos && capturePosition == null)
            {
                EditorUtility.DisplayDialog("作成失敗",
                    "キャプチャ位置の Transform を指定するか、対象GameObjectを指定して自動推定を有効にしてください。", "OK");
                return;
            }

            string normalizedOutput = NormalizeAssetPath(outputRootFolder);
            if (!IsAssetsPath(normalizedOutput))
            {
                EditorUtility.DisplayDialog("作成失敗", "出力先ルートは Assets 配下を指定してください。", "OK");
                return;
            }

            string safeSetName = SanitizeFileName(setName);
            if (string.IsNullOrWhiteSpace(safeSetName))
            {
                safeSetName = "MyBox";
            }

            Shader depthShader = Shader.Find(DepthShaderName);
            if (depthShader == null)
            {
                EditorUtility.DisplayDialog("作成失敗",
                    $"深度シェーダーが見つかりません: {DepthShaderName}", "OK");
                return;
            }

            Shader interiorShader = Shader.Find(InteriorShaderName);
            if (interiorShader == null)
            {
                EditorUtility.DisplayDialog("作成失敗",
                    $"視差シェーダーが見つかりません: {InteriorShaderName}", "OK");
                return;
            }

            farPlane = Mathf.Max(0.1f, farPlane);

            // --- Auto-detect box size ---
            Vector3 finalBoxSize = boxSize;
            if (autoDetectSize && boxObject != null)
            {
                var renderer = boxObject.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    finalBoxSize = renderer.bounds.size;
                }
                else
                {
                    var col = boxObject.GetComponentInChildren<Collider>();
                    if (col != null)
                    {
                        finalBoxSize = col.bounds.size;
                    }
                }
            }

            // --- Create folders ---
            string setFolder = CombineAssetPath(normalizedOutput, safeSetName);
            string textureFolder = CombineAssetPath(setFolder, TextureFolderName);
            string materialFolder = CombineAssetPath(setFolder, MaterialFolderName);
            EnsureFolder(textureFolder);
            EnsureFolder(materialFolder);

            int resolution = Resolutions[Mathf.Clamp(resolutionIndex, 0, Resolutions.Length - 1)];
            Vector3 capturePos = useAutoPos
                ? EstimateBoundsCenter(boxObject)
                : capturePosition.position;

            // Build culling mask (everything minus excluded layers)
            int cullingMask = ~excludeLayerMask.value;

            Camera tempCamera = null;
            RenderTexture colorRT = null;
            RenderTexture depthRT = null;

            try
            {
                // --- Setup temporary camera ---
                var camGo = new GameObject("__ParallaxBoxCaptureCamera__");
                camGo.hideFlags = HideFlags.HideAndDontSave;
                tempCamera = camGo.AddComponent<Camera>();
                tempCamera.enabled = false;
                tempCamera.fieldOfView = 90f;
                tempCamera.aspect = 1f;
                tempCamera.nearClipPlane = 0.01f;
                tempCamera.farClipPlane = farPlane;
                tempCamera.cullingMask = cullingMask;
                tempCamera.clearFlags = CameraClearFlags.SolidColor;
                tempCamera.backgroundColor = Color.black;
                tempCamera.transform.position = capturePos;

                colorRT = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
                depthRT = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.RFloat);

                Material[] generatedMaterials = new Material[6];

                for (int face = 0; face < 6; face++)
                {
                    EditorUtility.DisplayProgressBar(
                        "視差ボックスキャプチャ",
                        $"面 {FaceNames[face]} をキャプチャ中... ({face + 1}/6)",
                        (float)face / 6f);

                    // Orient camera
                    tempCamera.transform.rotation = Quaternion.LookRotation(FaceForwards[face], FaceUpwards[face]);

                    // --- Color capture ---
                    tempCamera.targetTexture = colorRT;
                    tempCamera.Render();

                    string colorPath = CombineAssetPath(textureFolder, $"{safeSetName}_{FaceNames[face]}_Color.png");
                    SaveRenderTextureToPNG(colorRT, colorPath);

                    // --- Depth capture ---
                    tempCamera.targetTexture = depthRT;
                    tempCamera.RenderWithShader(depthShader, "RenderType");

                    string depthPath = CombineAssetPath(textureFolder, $"{safeSetName}_{FaceNames[face]}_Depth.exr");
                    SaveRenderTextureToEXR(depthRT, depthPath);

                    // Import settings for depth texture
                    ConfigureDepthTextureImport(depthPath);

                    // --- Create material ---
                    string matPath = CombineAssetPath(materialFolder, $"{safeSetName}_{FaceNames[face]}_Mat.mat");
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null || overwriteExisting)
                    {
                        if (mat == null)
                        {
                            mat = new Material(interiorShader);
                            AssetDatabase.CreateAsset(mat, matPath);
                        }
                        else
                        {
                            mat.shader = interiorShader;
                        }

                        var colorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
                        var depthTex = AssetDatabase.LoadAssetAtPath<Texture2D>(depthPath);

                        mat.SetTexture("_ColorTex", colorTex);
                        mat.SetTexture("_DepthTex", depthTex);

                        Vector3 halfSize = finalBoxSize * 0.5f;
                        mat.SetVector("_BoxCenter", new Vector4(capturePos.x, capturePos.y, capturePos.z, 0));
                        mat.SetVector("_BoxHalfSize", new Vector4(halfSize.x, halfSize.y, halfSize.z, 0));
                        mat.SetFloat("_CaptureFarPlane", farPlane);

                        EditorUtility.SetDirty(mat);
                    }

                    generatedMaterials[face] = mat;
                }

                // --- Assign materials to box faces ---
                if (boxObject != null)
                {
                    AssignMaterialsToBox(boxObject, generatedMaterials);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();

                if (pingAfterCreate && generatedMaterials[0] != null)
                {
                    EditorGUIUtility.PingObject(generatedMaterials[0]);
                    Selection.activeObject = generatedMaterials[0];
                }

                EditorUtility.DisplayDialog(
                    "作成完了",
                    $"視差ボックス背景のキャプチャが完了しました。\n" +
                    $"出力先: {setFolder}\n" +
                    $"テクスチャ: 6面 × 2枚 = 12ファイル\n" +
                    $"マテリアル: 6個",
                    "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ParallaxBoxCapture] キャプチャ失敗: {ex}");
                EditorUtility.DisplayDialog("作成失敗", ex.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                if (tempCamera != null)
                {
                    DestroyImmediate(tempCamera.gameObject);
                }

                if (colorRT != null)
                {
                    colorRT.Release();
                    DestroyImmediate(colorRT);
                }

                if (depthRT != null)
                {
                    depthRT.Release();
                    DestroyImmediate(depthRT);
                }
            }
        }

        // ==================================================================
        //  Texture I/O
        // ==================================================================

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

            importer.sRGBTexture = false;                     // Linear
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

        // ==================================================================
        //  Bounds estimation
        // ==================================================================

        private static Vector3 EstimateBoundsCenter(GameObject go)
        {
            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer != null)
                return renderer.bounds.center;

            var col = go.GetComponentInChildren<Collider>();
            if (col != null)
                return col.bounds.center;

            // Fallback: use the transform position itself
            return go.transform.position;
        }

        // ==================================================================
        //  Material auto-assignment to box faces
        // ==================================================================

        private static void AssignMaterialsToBox(GameObject box, Material[] materials)
        {
            var renderers = box.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return;

            // If single renderer with 6 material slots (e.g. a Cube), assign directly
            if (renderers.Length == 1 && renderers[0].sharedMaterials.Length == 6)
            {
                renderers[0].sharedMaterials = materials;
                EditorUtility.SetDirty(renderers[0]);
                return;
            }

            // Otherwise try matching by face normal direction
            foreach (var r in renderers)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                // Use the first triangle's normal to determine face direction
                Vector3 worldNormal = GetDominantNormal(r.transform, mf.sharedMesh);
                int bestFace = GetClosestFaceIndex(worldNormal);

                if (bestFace >= 0 && bestFace < materials.Length)
                {
                    r.sharedMaterial = materials[bestFace];
                    EditorUtility.SetDirty(r);
                }
            }
        }

        private static Vector3 GetDominantNormal(Transform transform, Mesh mesh)
        {
            Vector3[] normals = mesh.normals;
            if (normals == null || normals.Length == 0)
                return transform.forward;

            // Average all normals to get dominant direction
            Vector3 avg = Vector3.zero;
            for (int i = 0; i < normals.Length; i++)
            {
                avg += normals[i];
            }

            avg = avg.normalized;
            return transform.TransformDirection(avg);
        }

        private static int GetClosestFaceIndex(Vector3 worldNormal)
        {
            int best = 0;
            float bestDot = -2f;

            for (int i = 0; i < FaceForwards.Length; i++)
            {
                // We compare against negative face-forward because box interior
                // normals point inward (opposite to the capture direction).
                float d = Vector3.Dot(worldNormal, -FaceForwards[i]);
                if (d > bestDot)
                {
                    bestDot = d;
                    best = i;
                }
            }

            return best;
        }

        // ==================================================================
        //  Path utilities (matches existing project patterns)
        // ==================================================================

        private static string AssetPathToDiskPath(string assetPath)
        {
            // Application.dataPath ends with "Assets"
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
    }
}
