using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NataneToon.Editor
{
    /// <summary>
    /// Creates a camera-attached full-screen quad for Natane screen FX.
    /// Runtime scripts are not required, so this is VRC-safe for worlds.
    /// </summary>
    internal static class ScreenFXSetupTool
    {
        private const string ScreenFxShaderName = "Natane/Screen FX Overlay";
        private const string MaterialFolderRoot = "Assets/NataneToon";
        private const string MaterialFolder = "Assets/NataneToon/ScreenFX";

        [MenuItem(NataneToolMenuPaths.ScreenFXSetup, false, 45)]
        private static void CreateScreenFxOverlay()
        {
            var camera = ResolveTargetCamera();
            if (camera == null)
            {
                EditorUtility.DisplayDialog(
                    "Camera Not Found",
                    "No camera was found in the scene.\nSelect a camera object, or set the MainCamera tag and try again.",
                    "OK");
                return;
            }

            var shader = Shader.Find(ScreenFxShaderName);
            if (shader == null)
            {
                EditorUtility.DisplayDialog(
                    "Shader Not Found",
                    $"Shader '{ScreenFxShaderName}' was not found.",
                    "OK");
                return;
            }

            EnsureFolders();
            string materialPath = AssetDatabase.GenerateUniqueAssetPath($"{MaterialFolder}/NataneScreenFXOverlay.mat");
            var material = new Material(shader);
            ApplyRecommendedDefaults(material);
            AssetDatabase.CreateAsset(material, materialPath);

            var overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlay.name = "NataneScreenFXOverlay";
            overlay.transform.SetParent(camera.transform, false);
            PlaceOverlayToCamera(overlay.transform, camera);

            var collider = overlay.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = overlay.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            Undo.RegisterCreatedObjectUndo(overlay, "Create Screen FX Overlay");
            Selection.activeGameObject = overlay;

            EditorUtility.DisplayDialog(
                "Screen FX Overlay Created",
                $"作成完了\n- Camera: {camera.name}\n- Overlay: {overlay.name}\n- Material: {materialPath}\n\n" +
                "ヒント\n" +
                "1) Overlayをカメラ直下で微調整\n" +
                "2) 必要に応じて描画レイヤー/カリングマスクを調整\n" +
                "3) MaterialでPosterize/Edge/Vignetteを調整",
                "OK");
        }

        private static Camera ResolveTargetCamera()
        {
            if (Selection.activeGameObject != null)
            {
                var selectedCamera = Selection.activeGameObject.GetComponent<Camera>();
                if (selectedCamera != null)
                {
                    return selectedCamera;
                }
            }

            if (Camera.main != null)
            {
                return Camera.main;
            }

            return Object.FindObjectOfType<Camera>();
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolderRoot))
            {
                AssetDatabase.CreateFolder("Assets", "NataneToon");
            }

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder(MaterialFolderRoot, "ScreenFX");
            }
        }

        private static void ApplyRecommendedDefaults(Material material)
        {
            material.SetFloat("_Intensity", 1.0f);
            material.SetFloat("_PosterizeStrength", 0.15f);
            material.SetFloat("_PosterizeSteps", 8.0f);
            material.SetFloat("_EdgeStrength", 0.25f);
            material.SetFloat("_EdgeThreshold", 0.1f);
            material.SetFloat("_Vignette", 0.1f);
            material.SetFloat("_VignetteSoftness", 0.35f);
            material.SetFloat("_ScanlineStrength", 0.0f);
            material.SetFloat("_GrainStrength", 0.04f);
        }

        private static void PlaceOverlayToCamera(Transform overlay, Camera camera)
        {
            float distance = Mathf.Max(camera.nearClipPlane + 0.05f, 0.1f);
            float width;
            float height;

            if (camera.orthographic)
            {
                height = camera.orthographicSize * 2.0f;
                width = height * camera.aspect;
            }
            else
            {
                height = 2.0f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                width = height * camera.aspect;
            }

            overlay.localPosition = new Vector3(0.0f, 0.0f, distance);
            overlay.localRotation = Quaternion.identity;
            overlay.localScale = new Vector3(width, height, 1.0f);
        }
    }
}
