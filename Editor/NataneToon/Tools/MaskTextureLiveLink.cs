using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// Live link between texture studio canvas and Scene View material.
    /// テクスチャスタジオのキャンバスとScene Viewマテリアルのライブリンク
    /// Supports multiple property bindings on the same material.
    /// </summary>
    internal class MaskTextureLiveLink : System.IDisposable
    {
        private class LiveLinkBinding
        {
            public Material material;
            public string propertyName;
            public Texture originalTexture;
            public RenderTexture frontRT;
            public RenderTexture backRT;
        }

        private const double UploadInterval = 1.0 / 30.0;
        private const double SceneRepaintInterval = 1.0 / 30.0;

        private bool _enabled;
        private bool _updateHookRegistered;
        private bool _hasPendingUpload;
        private bool _pendingUpdateAllBindings;
        private bool _sceneRepaintQueued;
        private double _nextUploadTime;
        private double _nextSceneRepaintTime;
        private Texture2D _pendingSource;
        private readonly List<LiveLinkBinding> _bindings = new List<LiveLinkBinding>();

        // --- Compatibility properties (reference first binding) ---
        public bool IsEnabled => _enabled;

        public string BoundPropertyName =>
            _bindings.Count > 0 ? _bindings[0].propertyName : null;

        public Material TargetMaterial =>
            _bindings.Count > 0 ? _bindings[0].material : null;

        public int BindingCount => _bindings.Count;

        public IEnumerable<string> BoundPropertyNames =>
            _bindings.Select(b => b.propertyName);

        // --- Primary API (single property, backward compatible) ---

        public void Enable(Material mat, string propertyName, int texSize)
        {
            if (_enabled)
                Disable();

            if (mat == null || string.IsNullOrEmpty(propertyName))
                return;

            if (!mat.HasProperty(propertyName))
                return;

            var binding = CreateBinding(mat, propertyName, texSize);
            if (binding == null)
                return;

            _bindings.Add(binding);
            _enabled = true;
            RegisterUpdateHook();
        }

        /// <summary>
        /// Add an additional property binding while Live Link is active.
        /// Live Link有効中に追加のプロパティをバインド
        /// </summary>
        public void EnableAdditional(Material mat, string propertyName, int texSize)
        {
            if (mat == null || string.IsNullOrEmpty(propertyName))
                return;

            if (!mat.HasProperty(propertyName))
                return;

            // Avoid duplicate bindings for the same material+property
            if (_bindings.Any(b => b.material == mat && b.propertyName == propertyName))
                return;

            var binding = CreateBinding(mat, propertyName, texSize);
            if (binding == null)
                return;

            _bindings.Add(binding);
            _enabled = true;
            RegisterUpdateHook();
        }

        public void UpdateTexture(Texture2D source)
        {
            ScheduleUpload(source, false, false);
        }

        public void UpdateTextureImmediate(Texture2D source)
        {
            ScheduleUpload(source, false, true);
        }

        /// <summary>
        /// Update all bindings with the same source texture.
        /// 全バインディングを同一ソーステクスチャで更新
        /// </summary>
        public void UpdateAllTextures(Texture2D source)
        {
            ScheduleUpload(source, true, false);
        }

        public void UpdateAllTexturesImmediate(Texture2D source)
        {
            ScheduleUpload(source, true, true);
        }

        public void Disable()
        {
            if (!_enabled && _bindings.Count == 0)
                return;

            UnregisterUpdateHook();

            foreach (var binding in _bindings)
                ReleaseBinding(binding);

            _bindings.Clear();
            _enabled = false;
            _hasPendingUpload = false;
            _pendingUpdateAllBindings = false;
            _sceneRepaintQueued = false;
            _pendingSource = null;
            _nextUploadTime = 0d;
            _nextSceneRepaintTime = 0d;
        }

        public void Dispose()
        {
            Disable();
        }

        // --- Internal helpers ---

        private void ScheduleUpload(Texture2D source, bool updateAllBindings, bool forceImmediate)
        {
            if (!_enabled || source == null)
                return;

            _pendingSource = source;
            _pendingUpdateAllBindings |= updateAllBindings;
            _hasPendingUpload = true;
            RegisterUpdateHook();

            if (forceImmediate)
            {
                FlushScheduledUpdates(true);
                if (!_hasPendingUpload && !_sceneRepaintQueued)
                    UnregisterUpdateHook();
            }
        }

        private void RegisterUpdateHook()
        {
            if (_updateHookRegistered)
                return;

            EditorApplication.update += OnEditorUpdate;
            _updateHookRegistered = true;
        }

        private void UnregisterUpdateHook()
        {
            if (!_updateHookRegistered)
                return;

            EditorApplication.update -= OnEditorUpdate;
            _updateHookRegistered = false;
        }

        private void OnEditorUpdate()
        {
            if (!_enabled)
            {
                UnregisterUpdateHook();
                return;
            }

            FlushScheduledUpdates(false);

            if (!_hasPendingUpload && !_sceneRepaintQueued)
                UnregisterUpdateHook();
        }

        private void FlushScheduledUpdates(bool forceImmediate)
        {
            double now = EditorApplication.timeSinceStartup;

            if (_hasPendingUpload && (forceImmediate || now >= _nextUploadTime))
            {
                Texture2D source = _pendingSource;
                bool updateAllBindings = _pendingUpdateAllBindings;

                _hasPendingUpload = false;
                _pendingUpdateAllBindings = false;
                _pendingSource = null;
                _nextUploadTime = now + UploadInterval;

                if (source != null && CommitSourceTexture(source, updateAllBindings))
                    _sceneRepaintQueued = true;
            }

            if (_sceneRepaintQueued && (forceImmediate || now >= _nextSceneRepaintTime))
            {
                _sceneRepaintQueued = false;
                _nextSceneRepaintTime = now + SceneRepaintInterval;
                SceneView.RepaintAll();
            }
        }

        private bool CommitSourceTexture(Texture source, bool updateAllBindings)
        {
            if (source == null || _bindings.Count == 0)
                return false;

            if (updateAllBindings)
            {
                bool updated = false;
                for (int i = 0; i < _bindings.Count; i++)
                    updated |= CommitBindingSource(_bindings[i], source);
                return updated;
            }

            return CommitBindingSource(_bindings[0], source);
        }

        private static bool CommitBindingSource(LiveLinkBinding binding, Texture source)
        {
            if (binding == null || binding.frontRT == null || binding.backRT == null || source == null)
                return false;

            Graphics.Blit(source, binding.backRT);
            SwapBuffers(binding);
            return true;
        }

        private static void SwapBuffers(LiveLinkBinding binding)
        {
            if (binding == null || binding.frontRT == null || binding.backRT == null)
                return;

            binding.material.SetTexture(binding.propertyName, binding.backRT);

            RenderTexture previousFront = binding.frontRT;
            binding.frontRT = binding.backRT;
            binding.backRT = previousFront;
        }

        private LiveLinkBinding CreateBinding(Material mat, string propertyName, int texSize)
        {
            int resolvedSize = Mathf.Max(1, texSize);
            Texture originalTexture = mat.GetTexture(propertyName);
            RenderTextureReadWrite readWrite = ResolveReadWrite(propertyName, originalTexture);

            var binding = new LiveLinkBinding
            {
                material = mat,
                propertyName = propertyName,
                originalTexture = originalTexture,
                frontRT = CreateLiveRenderTexture(resolvedSize, propertyName, "Front", originalTexture, readWrite),
                backRT = CreateLiveRenderTexture(resolvedSize, propertyName, "Back", originalTexture, readWrite)
            };

            if (binding.frontRT == null || binding.backRT == null)
            {
                ReleaseBinding(binding);
                return null;
            }

            if (originalTexture != null)
            {
                Graphics.Blit(originalTexture, binding.frontRT);
                Graphics.Blit(originalTexture, binding.backRT);
            }

            Undo.RecordObject(mat, "Live Link Enable");
            mat.SetTexture(propertyName, binding.frontRT);

            return binding;
        }

        private static RenderTexture CreateLiveRenderTexture(
            int texSize, string propertyName, string suffix, Texture template, RenderTextureReadWrite readWrite)
        {
            var rt = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.ARGB32, readWrite)
            {
                name = $"MaskTexLiveLink_{propertyName}_{suffix}",
                hideFlags = HideFlags.HideAndDontSave
            };

            if (template != null)
            {
                rt.filterMode = template.filterMode;
                rt.wrapMode = template.wrapMode;
                rt.anisoLevel = template.anisoLevel;
            }
            else
            {
                rt.filterMode = FilterMode.Bilinear;
                rt.wrapMode = TextureWrapMode.Clamp;
            }

            rt.Create();
            return rt;
        }

        private static RenderTextureReadWrite ResolveReadWrite(string propertyName, Texture originalTexture)
        {
            TextureEditType type = TextureTypeDetector.Detect(propertyName);
            if (type == TextureEditType.GrayscaleMask || type == TextureEditType.NormalMap || type == TextureEditType.RampTexture)
                return RenderTextureReadWrite.Linear;

            if (originalTexture is Texture2D originalTexture2D)
            {
                string assetPath = AssetDatabase.GetAssetPath(originalTexture2D);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null)
                        return importer.sRGBTexture ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
                }
            }

            return TextureTypeDetector.IsGrayscale(type)
                ? RenderTextureReadWrite.Linear
                : RenderTextureReadWrite.sRGB;
        }

        private static void ReleaseBinding(LiveLinkBinding binding)
        {
            if (binding == null)
                return;

            if (binding.material != null && !string.IsNullOrEmpty(binding.propertyName))
            {
                binding.material.SetTexture(binding.propertyName, binding.originalTexture);
                EditorUtility.SetDirty(binding.material);
            }

            ReleaseRenderTexture(ref binding.frontRT);
            ReleaseRenderTexture(ref binding.backRT);
        }

        private static void ReleaseRenderTexture(ref RenderTexture renderTexture)
        {
            if (renderTexture == null)
                return;

            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            renderTexture = null;
        }
    }
}
