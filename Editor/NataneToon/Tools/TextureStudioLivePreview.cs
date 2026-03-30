using UnityEngine;
using UnityEditor;
using System.IO;

namespace NataneToon.Editor
{
    /// <summary>
    /// Live preview: applies texture updates from the standalone studio to Scene View materials.
    /// ライブプレビュー: スタンドアロンスタジオからのテクスチャ更新をシーンビューのマテリアルに適用する
    /// </summary>
    internal class TextureStudioLivePreview
    {
        private Material _targetMaterial;
        private string _targetProperty;
        private Texture2D _originalTexture;
        private Texture2D _previewTexture;
        private bool _enabled;

        public bool IsEnabled => _enabled;

        public void Enable(Material material, string propertyName)
        {
            Disable();
            if (material == null || string.IsNullOrEmpty(propertyName)) return;

            _targetMaterial = material;
            _targetProperty = propertyName;
            _originalTexture = material.GetTexture(propertyName) as Texture2D;
            _enabled = true;

            Debug.Log($"[LivePreview] Enabled for {material.name}.{propertyName}");
        }

        public void Disable()
        {
            if (_enabled && _targetMaterial != null && _originalTexture != null)
            {
                _targetMaterial.SetTexture(_targetProperty, _originalTexture);
                SceneView.RepaintAll();
            }

            if (_previewTexture != null)
            {
                Object.DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }

            _enabled = false;
            _targetMaterial = null;
            _targetProperty = null;
            _originalTexture = null;
        }

        /// <summary>
        /// Called when the studio sends a preview update with a temp file path.
        /// スタジオが一時ファイルパスでプレビュー更新を送信したときに呼び出される
        /// </summary>
        public void OnPreviewUpdate(string tempFilePath)
        {
            if (!_enabled || _targetMaterial == null) return;

            try
            {
                byte[] pngData = File.ReadAllBytes(tempFilePath);

                if (_previewTexture == null)
                {
                    _previewTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    _previewTexture.hideFlags = HideFlags.HideAndDontSave;
                }

                _previewTexture.LoadImage(pngData);
                _targetMaterial.SetTexture(_targetProperty, _previewTexture);
                SceneView.RepaintAll();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LivePreview] Update failed: {ex.Message}");
            }
        }
    }
}
