using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace NataneToon.Editor
{
    /// <summary>
    /// Live preview: applies texture updates from the standalone studio to Scene View materials.
    /// Uses shared memory (MemoryMappedFile) for near-zero-latency texture transfer.
    /// </summary>
    [InitializeOnLoad]
    internal class TextureStudioLivePreview
    {
        private Material _targetMaterial;
        private string _targetProperty;
        private Texture2D _originalTexture;
        private Texture2D _previewTexture;
        private bool _enabled;

        // Shared memory for real-time preview (no file I/O)
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private const string SharedMemoryName = "NataneTextureStudio_LivePreview";
        private const int HeaderSize = 16; // width(4) + height(4) + frameCounter(4) + flags(4)
        private int _lastFrameCounter = -1;
        private int _textureWidth;
        private int _textureHeight;
        private byte[] _pixelBuffer;

        // Polling timer for shared memory updates
        private static bool _runInBackgroundSet;

        public bool IsEnabled => _enabled;

        static TextureStudioLivePreview()
        {
            // Ensure Unity runs in background for live preview responsiveness
            if (!_runInBackgroundSet)
            {
                EditorApplication.update += EnsureRunInBackground;
            }
        }

        private static void EnsureRunInBackground()
        {
            // Only set when Texture Studio is running
            if (TextureStudioLauncher.IsRunning && !Application.runInBackground)
            {
                Application.runInBackground = true;
                _runInBackgroundSet = true;
            }
        }

        public void Enable(Material material, string propertyName)
        {
            Disable();
            if (material == null || string.IsNullOrEmpty(propertyName)) return;

            _targetMaterial = material;
            _targetProperty = propertyName;
            _originalTexture = material.GetTexture(propertyName) as Texture2D;
            _enabled = true;

            // Force Unity to run in background
            Application.runInBackground = true;

            // Start polling for shared memory updates
            EditorApplication.update += PollSharedMemory;

            Debug.Log($"[LivePreview] Enabled for {material.name}.{propertyName} (shared memory + runInBackground)");
        }

        public void Disable()
        {
            EditorApplication.update -= PollSharedMemory;

            if (_enabled && _targetMaterial != null && _originalTexture != null)
            {
                _targetMaterial.SetTexture(_targetProperty, _originalTexture);
                SceneView.RepaintAll();
            }

            CloseSharedMemory();

            if (_previewTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }

            _enabled = false;
            _targetMaterial = null;
            _targetProperty = null;
            _originalTexture = null;
            _lastFrameCounter = -1;
        }

        /// <summary>
        /// Poll shared memory for new frames (called every EditorApplication.update).
        /// </summary>
        private void PollSharedMemory()
        {
            if (!_enabled || _targetMaterial == null) return;

            try
            {
                // Open shared memory if not yet open
                if (_mmf == null)
                {
                    try
                    {
                        _mmf = MemoryMappedFile.OpenExisting(SharedMemoryName);
                        _accessor = _mmf.CreateViewAccessor();
                    }
                    catch (FileNotFoundException)
                    {
                        return; // Studio hasn't created shared memory yet
                    }
                }

                if (_accessor == null) return;

                // Read header
                int width = _accessor.ReadInt32(0);
                int height = _accessor.ReadInt32(4);
                int frameCounter = _accessor.ReadInt32(8);

                // Check if new frame available
                if (frameCounter == _lastFrameCounter) return;
                if (width <= 0 || height <= 0 || width > 8192 || height > 8192) return;

                _lastFrameCounter = frameCounter;

                // Read pixel data (RGBA)
                int pixelDataSize = width * height * 4;
                if (_pixelBuffer == null || _pixelBuffer.Length != pixelDataSize)
                    _pixelBuffer = new byte[pixelDataSize];

                // Reopen accessor if size changed
                long requiredSize = HeaderSize + pixelDataSize;
                if (_accessor.Capacity < requiredSize)
                {
                    _accessor.Dispose();
                    _mmf.Dispose();
                    _mmf = MemoryMappedFile.OpenExisting(SharedMemoryName);
                    _accessor = _mmf.CreateViewAccessor();
                }

                _accessor.ReadArray(HeaderSize, _pixelBuffer, 0, pixelDataSize);

                // Convert premultiplied alpha → straight alpha (SkiaSharp → Unity)
                for (int i = 0; i < pixelDataSize; i += 4)
                {
                    byte a = _pixelBuffer[i + 3];
                    if (a > 0 && a < 255)
                    {
                        float inv = 255f / a;
                        _pixelBuffer[i] = (byte)Mathf.Min(255, _pixelBuffer[i] * inv);
                        _pixelBuffer[i + 1] = (byte)Mathf.Min(255, _pixelBuffer[i + 1] * inv);
                        _pixelBuffer[i + 2] = (byte)Mathf.Min(255, _pixelBuffer[i + 2] * inv);
                    }
                }

                // Unity textures are bottom-up, SkiaSharp is top-down — flip Y
                int stride = width * 4;
                byte[] flipped = new byte[pixelDataSize];
                for (int y = 0; y < height; y++)
                    System.Array.Copy(_pixelBuffer, y * stride, flipped, (height - 1 - y) * stride, stride);

                // Update texture
                if (_previewTexture == null || _previewTexture.width != width || _previewTexture.height != height)
                {
                    if (_previewTexture != null) UnityEngine.Object.DestroyImmediate(_previewTexture);
                    _previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    _previewTexture.hideFlags = HideFlags.HideAndDontSave;
                    _previewTexture.filterMode = FilterMode.Bilinear;
                }

                _previewTexture.LoadRawTextureData(flipped);
                _previewTexture.Apply(false, false);

                _targetMaterial.SetTexture(_targetProperty, _previewTexture);
                SceneView.RepaintAll();
            }
            catch (Exception)
            {
                // Shared memory may be closed by studio — ignore
            }
        }

        /// <summary>
        /// Fallback: Called when the studio sends a preview update with a temp file path.
        /// </summary>
        public void OnPreviewUpdate(string tempFilePath)
        {
            if (!_enabled || _targetMaterial == null) return;

            try
            {
                byte[] data = File.ReadAllBytes(tempFilePath);

                if (_previewTexture == null)
                {
                    _previewTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    _previewTexture.hideFlags = HideFlags.HideAndDontSave;
                }

                _previewTexture.LoadImage(data);
                _targetMaterial.SetTexture(_targetProperty, _previewTexture);
                SceneView.RepaintAll();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LivePreview] File update failed: {ex.Message}");
            }
        }

        private void CloseSharedMemory()
        {
            _accessor?.Dispose();
            _accessor = null;
            _mmf?.Dispose();
            _mmf = null;
            _pixelBuffer = null;
        }
    }
}
