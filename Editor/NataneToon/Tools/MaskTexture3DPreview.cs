using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// 3D preview for mask textures using PreviewRenderUtility.
    /// PreviewRenderUtilityを使用したマスクテクスチャの3Dプレビュー
    /// </summary>
    internal class MaskTexture3DPreview : System.IDisposable
    {
        private PreviewRenderUtility previewUtility;
        private Material previewMaterial;
        private Mesh previewMesh;
        private Texture2D currentTexture;
        private Texture2D processedTexture;

        private float rotationX = 30f;
        private float rotationY = -30f;
        private float zoom = 3f;
        private Vector2 lastMousePosition;
        private bool isDragging;

        private DisplayMode displayMode = DisplayMode.Grayscale;
        private bool needsTextureUpdate;

        /// <summary>
        /// Display mode for the 3D texture preview.
        /// 3Dテクスチャプレビューの表示モード
        /// </summary>
        internal enum DisplayMode
        {
            Grayscale,
            HeatMap,
            ChannelR,
            ChannelG,
            ChannelB,
            ChannelA
        }

        /// <summary>
        /// Initialize the preview renderer and material.
        /// プレビューレンダラーとマテリアルを初期化
        /// </summary>
        public void Initialize()
        {
            if (previewUtility != null) return;

            previewUtility = new PreviewRenderUtility();

            var unlitShader = Shader.Find("Unlit/Texture");
            if (unlitShader != null)
            {
                previewMaterial = new Material(unlitShader);
                previewMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            previewUtility.camera.nearClipPlane = 0.01f;
            previewUtility.camera.farClipPlane = 100f;
            previewUtility.camera.fieldOfView = 30f;
            previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            previewUtility.camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        }

        /// <summary>
        /// Set the mesh to preview. Auto-centers and adjusts zoom to fit.
        /// プレビューするメッシュを設定。自動的に中心に配置しズームを調整
        /// </summary>
        public void SetMesh(Mesh mesh)
        {
            previewMesh = mesh;

            if (mesh != null)
            {
                Bounds bounds = mesh.bounds;
                float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                zoom = maxExtent * 4f;
                if (zoom < 0.5f) zoom = 3f;
            }
        }

        /// <summary>
        /// Set the texture to preview. Applies the current display mode.
        /// プレビューするテクスチャを設定。現在の表示モードを適用
        /// </summary>
        public void SetTexture(Texture2D texture)
        {
            currentTexture = texture;
            needsTextureUpdate = true;
        }

        /// <summary>
        /// Get or set the current display mode. Changing mode triggers texture update.
        /// 現在の表示モードを取得・設定。モード変更でテクスチャ更新
        /// </summary>
        public DisplayMode CurrentDisplayMode
        {
            get => displayMode;
            set
            {
                if (displayMode != value)
                {
                    displayMode = value;
                    needsTextureUpdate = true;
                }
            }
        }

        /// <summary>
        /// Draw the 3D preview in the given rect.
        /// 指定矩形に3Dプレビューを描画
        /// </summary>
        public void DrawPreview(Rect rect)
        {
            if (previewUtility == null) Initialize();
            if (previewMesh == null)
            {
                EditorGUI.LabelField(rect, L("メッシュが未設定", "No mesh assigned"),
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
                return;
            }

            if (needsTextureUpdate)
            {
                UpdateProcessedTexture();
                needsTextureUpdate = false;
            }

            if (previewMaterial != null && processedTexture != null)
            {
                previewMaterial.mainTexture = processedTexture;
            }

            previewUtility.BeginPreview(rect, GUIStyle.none);

            var camera = previewUtility.camera;
            Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);

            Bounds bounds = previewMesh.bounds;
            Vector3 center = bounds.center;
            camera.transform.position = center + rotation * new Vector3(0, 0, -zoom);
            camera.transform.rotation = rotation;

            if (previewUtility.lights != null && previewUtility.lights.Length > 0)
            {
                previewUtility.lights[0].intensity = 1f;
                previewUtility.lights[0].transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            if (previewMaterial != null)
            {
                previewUtility.DrawMesh(previewMesh, Matrix4x4.identity, previewMaterial, 0);
            }

            previewUtility.Render();
            var resultTexture = previewUtility.EndPreview();
            GUI.DrawTexture(rect, resultTexture, ScaleMode.ScaleToFit);
        }

        /// <summary>
        /// Handle mouse input for camera rotation and zoom.
        /// カメラ回転とズームのためのマウス入力を処理
        /// </summary>
        public void HandleInput(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition) && !isDragging) return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 || e.button == 2)
                    {
                        isDragging = true;
                        lastMousePosition = e.mousePosition;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (isDragging)
                    {
                        Vector2 delta = e.mousePosition - lastMousePosition;
                        rotationX += delta.x;
                        rotationY += delta.y;
                        rotationY = Mathf.Clamp(rotationY, -89f, 89f);
                        lastMousePosition = e.mousePosition;
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (isDragging)
                    {
                        isDragging = false;
                        e.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    if (rect.Contains(e.mousePosition))
                    {
                        zoom += e.delta.y * 0.1f;
                        zoom = Mathf.Clamp(zoom, 0.5f, 20f);
                        e.Use();
                    }
                    break;
            }
        }

        /// <summary>
        /// Draw the display mode selection UI.
        /// 表示モード選択UIを描画
        /// </summary>
        public void DrawPreviewUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L("表示モード", "Display Mode"), GUILayout.Width(160));
            var newMode = (DisplayMode)EditorGUILayout.EnumPopup(displayMode);
            if (newMode != displayMode)
            {
                displayMode = newMode;
                needsTextureUpdate = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Force refresh of the processed texture on next draw.
        /// 次回描画時にプロセステクスチャを強制更新
        /// </summary>
        public void MarkDirty()
        {
            needsTextureUpdate = true;
        }

        /// <summary>
        /// Update the processed texture based on the current display mode.
        /// 現在の表示モードに基づいてプロセステクスチャを更新
        /// </summary>
        private void UpdateProcessedTexture()
        {
            if (currentTexture == null)
            {
                if (processedTexture != null)
                {
                    Object.DestroyImmediate(processedTexture);
                    processedTexture = null;
                }
                return;
            }

            int w = currentTexture.width;
            int h = currentTexture.height;
            Color[] srcPixels = currentTexture.GetPixels();

            if (processedTexture == null || processedTexture.width != w || processedTexture.height != h)
            {
                if (processedTexture != null)
                    Object.DestroyImmediate(processedTexture);
                processedTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                processedTexture.hideFlags = HideFlags.HideAndDontSave;
                processedTexture.filterMode = FilterMode.Bilinear;
                processedTexture.wrapMode = TextureWrapMode.Clamp;
            }

            Color[] dstPixels = new Color[srcPixels.Length];

            for (int i = 0; i < srcPixels.Length; i++)
            {
                Color src = srcPixels[i];
                Color dst;

                switch (displayMode)
                {
                    case DisplayMode.Grayscale:
                        dst = src;
                        break;

                    case DisplayMode.HeatMap:
                    {
                        float v = (src.r + src.g + src.b) / 3f;
                        dst = GrayscaleToHeatMap(v);
                        break;
                    }

                    case DisplayMode.ChannelR:
                        dst = new Color(src.r, src.r, src.r, 1f);
                        break;

                    case DisplayMode.ChannelG:
                        dst = new Color(src.g, src.g, src.g, 1f);
                        break;

                    case DisplayMode.ChannelB:
                        dst = new Color(src.b, src.b, src.b, 1f);
                        break;

                    case DisplayMode.ChannelA:
                        dst = new Color(src.a, src.a, src.a, 1f);
                        break;

                    default:
                        dst = src;
                        break;
                }

                dstPixels[i] = dst;
            }

            processedTexture.SetPixels(dstPixels);
            processedTexture.Apply();
        }

        /// <summary>
        /// Convert a grayscale value (0-1) to a heat map color.
        /// Blue → Cyan → Green → Yellow → Red
        /// グレースケール値(0-1)をヒートマップ色に変換
        /// 青 → シアン → 緑 → 黄 → 赤
        /// </summary>
        private static Color GrayscaleToHeatMap(float value)
        {
            value = Mathf.Clamp01(value);

            // 5-stop gradient: Blue(0) → Cyan(0.25) → Green(0.5) → Yellow(0.75) → Red(1)
            if (value < 0.25f)
            {
                float t = value / 0.25f;
                return Color.Lerp(new Color(0f, 0f, 1f), new Color(0f, 1f, 1f), t);
            }
            else if (value < 0.5f)
            {
                float t = (value - 0.25f) / 0.25f;
                return Color.Lerp(new Color(0f, 1f, 1f), new Color(0f, 1f, 0f), t);
            }
            else if (value < 0.75f)
            {
                float t = (value - 0.5f) / 0.25f;
                return Color.Lerp(new Color(0f, 1f, 0f), new Color(1f, 1f, 0f), t);
            }
            else
            {
                float t = (value - 0.75f) / 0.25f;
                return Color.Lerp(new Color(1f, 1f, 0f), new Color(1f, 0f, 0f), t);
            }
        }

        /// <summary>
        /// Dispose all resources. Must be called when no longer needed.
        /// すべてのリソースを解放。不要になったら必ず呼び出すこと
        /// </summary>
        public void Dispose()
        {
            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }

            if (previewMaterial != null)
            {
                Object.DestroyImmediate(previewMaterial);
                previewMaterial = null;
            }

            if (processedTexture != null)
            {
                Object.DestroyImmediate(processedTexture);
                processedTexture = null;
            }

            previewMesh = null;
            currentTexture = null;
        }
    }
}
