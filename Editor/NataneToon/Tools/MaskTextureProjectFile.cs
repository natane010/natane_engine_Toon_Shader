using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// .nataneTex project file save/load system.
    /// .nataneTexプロジェクトファイルのセーブ/ロードシステム
    /// </summary>
    internal static class MaskTextureProjectFile
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("NTEX");
        private const ushort FileVersion = 1;

        [Serializable]
        internal class ProjectMetadata
        {
            public int version = 1;
            public int width;
            public int height;
            public int activeLayerIndex;
            public List<LayerMetadata> layers = new List<LayerMetadata>();
            public float canvasZoom;
            public float canvasPanX;
            public float canvasPanY;
            public bool showUVWireframe;
            public int activeTool;
            public int currentTab;
            public float brushSize;
            public float brushHardness;
            public float brushOpacity;
            public float brushStrength;
            public float brushPaintAlpha;
            public int brushMode;
            public float eraserSize;
            public float eraserHardness;
            public float eraserOpacity;
            public float eraserStrength;
            public float eraserPaintAlpha;
            public float fillValue;
            public float fillAlpha;
            public float fillTolerance;
            public bool fillContiguous;
        }

        [Serializable]
        internal class LayerMetadata
        {
            public string name;
            public bool visible;
            public float opacity;
            public int blendMode;
            public bool locked;
            public bool lockTransparentPixels;
            public int sourceType;
            public float transformOffsetX;
            public float transformOffsetY;
            public float transformScaleX;
            public float transformScaleY;
        }

        /// <summary>
        /// Data class returned by Load.
        /// ロード結果のデータクラス
        /// </summary>
        internal class NataneTexProjectData
        {
            public MaskLayerStack layerStack;
            public float canvasZoom;
            public Vector2 canvasPan;
            public bool showUVWireframe;
            public int activeTool;
            public int currentTab;
            public BrushSettings brushSettings;
            public BrushSettings eraserSettings;
            public FillToolSettings fillSettings;
        }

        /// <summary>
        /// Save the current project state to a .nataneTex file.
        /// 現在のプロジェクト状態を.nataneTexファイルに保存
        /// </summary>
        public static void Save(
            string path,
            MaskLayerStack layerStack,
            float canvasZoom,
            Vector2 canvasPan,
            bool showUVWireframe,
            int activeTool,
            int currentTab,
            BrushSettings brushSettings,
            BrushSettings eraserSettings,
            FillToolSettings fillSettings)
        {
            if (layerStack == null || layerStack.Layers.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "保存エラー / Save Error",
                    "レイヤーデータがありません。\nNo layer data to save.",
                    "OK");
                return;
            }

            try
            {
                // Build metadata
                var metadata = new ProjectMetadata
                {
                    version = FileVersion,
                    width = layerStack.Width,
                    height = layerStack.Height,
                    activeLayerIndex = layerStack.ActiveLayerIndex,
                    canvasZoom = canvasZoom,
                    canvasPanX = canvasPan.x,
                    canvasPanY = canvasPan.y,
                    showUVWireframe = showUVWireframe,
                    activeTool = activeTool,
                    currentTab = currentTab,
                    brushSize = brushSettings != null ? brushSettings.size : 20f,
                    brushHardness = brushSettings != null ? brushSettings.hardness : 0.8f,
                    brushOpacity = brushSettings != null ? brushSettings.opacity : 1f,
                    brushStrength = brushSettings != null ? brushSettings.strength : 1f,
                    brushPaintAlpha = brushSettings != null ? brushSettings.paintAlpha : 1f,
                    brushMode = brushSettings != null ? (int)brushSettings.mode : 0,
                    eraserSize = eraserSettings != null ? eraserSettings.size : 20f,
                    eraserHardness = eraserSettings != null ? eraserSettings.hardness : 0.8f,
                    eraserOpacity = eraserSettings != null ? eraserSettings.opacity : 1f,
                    eraserStrength = eraserSettings != null ? eraserSettings.strength : 1f,
                    eraserPaintAlpha = eraserSettings != null ? eraserSettings.paintAlpha : 1f,
                    fillValue = fillSettings != null ? fillSettings.fillValue : 1f,
                    fillAlpha = fillSettings != null ? fillSettings.fillAlpha : 1f,
                    fillTolerance = fillSettings != null ? fillSettings.tolerance : 0.1f,
                    fillContiguous = fillSettings != null ? fillSettings.contiguous : true,
                };

                foreach (var layer in layerStack.Layers)
                {
                    metadata.layers.Add(new LayerMetadata
                    {
                        name = layer.name,
                        visible = layer.visible,
                        opacity = layer.opacity,
                        blendMode = (int)layer.blendMode,
                        locked = layer.locked,
                        lockTransparentPixels = layer.lockTransparentPixels,
                        sourceType = (int)layer.sourceType,
                        transformOffsetX = layer.transformOffset.x,
                        transformOffsetY = layer.transformOffset.y,
                        transformScaleX = layer.transformScale.x,
                        transformScaleY = layer.transformScale.y,
                    });
                }

                string metadataJson = JsonUtility.ToJson(metadata);
                byte[] metadataBytes = Encoding.UTF8.GetBytes(metadataJson);

                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(stream))
                {
                    // Magic
                    writer.Write(Magic);

                    // Version
                    writer.Write(FileVersion);

                    // Metadata JSON
                    writer.Write(metadataBytes.Length);
                    writer.Write(metadataBytes);

                    // Layer count
                    writer.Write(layerStack.Layers.Count);

                    // Layer pixel data as PNG
                    for (int i = 0; i < layerStack.Layers.Count; i++)
                    {
                        var layer = layerStack.Layers[i];
                        byte[] pngBytes = EncodeLayerToPNG(layer);
                        writer.Write(pngBytes.Length);
                        writer.Write(pngBytes);
                    }
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "保存エラー / Save Error",
                    $"ファイルの保存に失敗しました。\nFailed to save file.\n\n{e.Message}",
                    "OK");
                Debug.LogError($"[MaskTextureProjectFile] Save failed: {e}");
            }
        }

        /// <summary>
        /// Load a .nataneTex project file.
        /// .nataneTexプロジェクトファイルをロード
        /// </summary>
        public static NataneTexProjectData Load(string path)
        {
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog(
                    "読み込みエラー / Load Error",
                    "ファイルが見つかりません。\nFile not found.",
                    "OK");
                return null;
            }

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    // Validate magic
                    byte[] magic = reader.ReadBytes(4);
                    if (magic.Length < 4 ||
                        magic[0] != Magic[0] || magic[1] != Magic[1] ||
                        magic[2] != Magic[2] || magic[3] != Magic[3])
                    {
                        EditorUtility.DisplayDialog(
                            "読み込みエラー / Load Error",
                            "無効なファイル形式です。\nInvalid file format.",
                            "OK");
                        return null;
                    }

                    // Version
                    ushort version = reader.ReadUInt16();
                    if (version > FileVersion)
                    {
                        EditorUtility.DisplayDialog(
                            "読み込みエラー / Load Error",
                            $"未対応のファイルバージョンです (v{version})。\nUnsupported file version (v{version}).",
                            "OK");
                        return null;
                    }

                    // Metadata JSON
                    int metadataLength = reader.ReadInt32();
                    byte[] metadataBytes = reader.ReadBytes(metadataLength);
                    string metadataJson = Encoding.UTF8.GetString(metadataBytes);
                    var metadata = JsonUtility.FromJson<ProjectMetadata>(metadataJson);

                    if (metadata == null || metadata.width <= 0 || metadata.height <= 0)
                    {
                        EditorUtility.DisplayDialog(
                            "読み込みエラー / Load Error",
                            "メタデータが無効です。\nInvalid metadata.",
                            "OK");
                        return null;
                    }

                    // Layer count
                    int layerCount = reader.ReadInt32();

                    // Reconstruct layer stack
                    var layerStack = new MaskLayerStack(metadata.width, metadata.height);

                    for (int i = 0; i < layerCount; i++)
                    {
                        int pngLength = reader.ReadInt32();
                        byte[] pngBytes = reader.ReadBytes(pngLength);

                        var layer = new MaskTextureLayer(
                            i < metadata.layers.Count ? metadata.layers[i].name : $"Layer {i + 1}",
                            metadata.width,
                            metadata.height);

                        // Decode PNG to pixels
                        DecodeLayerFromPNG(layer, pngBytes);

                        // Apply layer metadata
                        if (i < metadata.layers.Count)
                        {
                            var lm = metadata.layers[i];
                            layer.visible = lm.visible;
                            layer.opacity = lm.opacity;
                            layer.blendMode = (MaskBlendMode)lm.blendMode;
                            layer.locked = lm.locked;
                            layer.lockTransparentPixels = lm.lockTransparentPixels;
                            layer.sourceType = (MaskTextureLayer.SourceType)lm.sourceType;
                            layer.transformOffset = new Vector2(lm.transformOffsetX, lm.transformOffsetY);
                            layer.transformScale = new Vector2(lm.transformScaleX, lm.transformScaleY);
                        }

                        layerStack.AddLayer(layer);
                    }

                    // Restore active layer index
                    layerStack.ActiveLayerIndex = Mathf.Clamp(
                        metadata.activeLayerIndex, 0,
                        Mathf.Max(0, layerStack.Layers.Count - 1));

                    // Build brush settings
                    var brushSettings = new BrushSettings
                    {
                        size = metadata.brushSize,
                        hardness = metadata.brushHardness,
                        opacity = metadata.brushOpacity,
                        strength = metadata.brushStrength,
                        paintAlpha = metadata.brushPaintAlpha,
                        mode = (BrushMode)metadata.brushMode,
                    };

                    var eraserSettings = new BrushSettings
                    {
                        size = metadata.eraserSize,
                        hardness = metadata.eraserHardness,
                        opacity = metadata.eraserOpacity,
                        strength = metadata.eraserStrength,
                        paintAlpha = metadata.eraserPaintAlpha,
                        mode = BrushMode.Erase,
                    };

                    var fillSettings = new FillToolSettings
                    {
                        fillValue = metadata.fillValue,
                        fillAlpha = metadata.fillAlpha,
                        tolerance = metadata.fillTolerance,
                        contiguous = metadata.fillContiguous,
                    };

                    return new NataneTexProjectData
                    {
                        layerStack = layerStack,
                        canvasZoom = metadata.canvasZoom,
                        canvasPan = new Vector2(metadata.canvasPanX, metadata.canvasPanY),
                        showUVWireframe = metadata.showUVWireframe,
                        activeTool = metadata.activeTool,
                        currentTab = metadata.currentTab,
                        brushSettings = brushSettings,
                        eraserSettings = eraserSettings,
                        fillSettings = fillSettings,
                    };
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "読み込みエラー / Load Error",
                    $"ファイルの読み込みに失敗しました。\nFailed to load file.\n\n{e.Message}",
                    "OK");
                Debug.LogError($"[MaskTextureProjectFile] Load failed: {e}");
                return null;
            }
        }

        /// <summary>
        /// Encode a layer's pixel data to PNG bytes.
        /// レイヤーのピクセルデータをPNGバイトにエンコード
        /// </summary>
        private static byte[] EncodeLayerToPNG(MaskTextureLayer layer)
        {
            var tex = new Texture2D(layer.width, layer.height, TextureFormat.RGBA32, false);
            try
            {
                tex.SetPixels(layer.pixels);
                tex.Apply();
                return tex.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        /// <summary>
        /// Decode PNG bytes into a layer's pixel array.
        /// PNGバイトをレイヤーのピクセル配列にデコード
        /// </summary>
        private static void DecodeLayerFromPNG(MaskTextureLayer layer, byte[] pngBytes)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                tex.LoadImage(pngBytes);
                Color[] decoded = tex.GetPixels();

                if (decoded.Length == layer.pixels.Length)
                {
                    Array.Copy(decoded, layer.pixels, decoded.Length);
                }
                else
                {
                    // Resample if dimensions differ
                    for (int y = 0; y < layer.height; y++)
                    {
                        float v = (float)y / Mathf.Max(1, layer.height - 1);
                        for (int x = 0; x < layer.width; x++)
                        {
                            float u = (float)x / Mathf.Max(1, layer.width - 1);
                            layer.pixels[y * layer.width + x] = tex.GetPixelBilinear(u, v);
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
