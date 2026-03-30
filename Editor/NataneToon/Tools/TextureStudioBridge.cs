using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NataneToon.Editor
{
    /// <summary>
    /// Named Pipe client bridge for communicating with NataneTextureStudio.exe.
    /// NataneTextureStudio.exe と通信するための名前付きパイプクライアントブリッジ
    /// </summary>
    internal static class TextureStudioBridge
    {
        private static NamedPipeClientStream _pipe;
        private static StreamReader _reader;
        private static StreamWriter _writer;
        private static CancellationTokenSource _cts;
        private static bool _connected;

        public static bool IsConnected => _connected;

        /// <summary>Connect to the studio's named pipe server.</summary>
        public static void Connect(string pipeName)
        {
            Disconnect();
            _cts = new CancellationTokenSource();

            var token = _cts.Token;
            Task.Run(async () =>
            {
                try
                {
                    _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                    await _pipe.ConnectAsync(5000, token);

                    _reader = new StreamReader(_pipe, Encoding.UTF8);
                    _writer = new StreamWriter(_pipe, Encoding.UTF8) { AutoFlush = true };
                    _connected = true;

                    Debug.Log("[TextureStudioBridge] Connected to pipe: " + pipeName);

                    // Listen for events from studio
                    await ListenLoop(token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TextureStudioBridge] Connection failed: " + ex.Message);
                }
            });
        }

        public static void Disconnect()
        {
            _connected = false;
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            if (_writer != null) { _writer.Dispose(); _writer = null; }
            if (_reader != null) { _reader.Dispose(); _reader = null; }
            if (_pipe != null) { _pipe.Dispose(); _pipe = null; }
        }

        /// <summary>Send an openTexture command to the studio.</summary>
        public static void SendOpenTexture(string path, string propertyName)
        {
            SendMessage("{\"method\":\"openTexture\",\"params\":{\"path\":\"" + EscapeJson(path) + "\",\"propertyName\":\"" + EscapeJson(propertyName) + "\"}}");
        }

        /// <summary>Send an importLayer command to the studio.</summary>
        public static void SendImportLayer(string path, string name, float opacity = 1f)
        {
            SendMessage("{\"method\":\"importLayer\",\"params\":{\"path\":\"" + EscapeJson(path) + "\",\"name\":\"" + EscapeJson(name) + "\",\"opacity\":" + opacity.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}}");
        }

        private static void SendMessage(string json)
        {
            if (!_connected || _writer == null)
            {
                Debug.LogWarning("[TextureStudioBridge] Not connected. Launch Texture Studio first.");
                return;
            }

            try
            {
                _writer.WriteLine(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TextureStudioBridge] Send failed: " + ex.Message);
                _connected = false;
            }
        }

        private static async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _reader != null)
            {
                try
                {
                    string line = await _reader.ReadLineAsync();
                    if (line == null) break; // Pipe closed

                    // Dispatch to main thread
                    string captured = line;
                    EditorApplication.delayCall += () => HandleEvent(captured);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TextureStudioBridge] Read error: " + ex.Message);
                    break;
                }
            }
            _connected = false;
        }

        private static void HandleEvent(string json)
        {
            try
            {
                // Simple JSON parsing for event type
                if (json.Contains("\"event\":\"saved\""))
                {
                    // Extract path from JSON
                    int pathStart = json.IndexOf("\"path\":\"") + 8;
                    int pathEnd = json.IndexOf("\"", pathStart);
                    if (pathStart > 7 && pathEnd > pathStart)
                    {
                        string savedPath = json.Substring(pathStart, pathEnd - pathStart);
                        // Reimport the asset in Unity
                        string assetPath = FileToAssetPath(savedPath);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                            Debug.Log("[TextureStudioBridge] Reimported: " + assetPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TextureStudioBridge] Event handling error: " + ex.Message);
            }
        }

        private static string FileToAssetPath(string fullPath)
        {
            fullPath = fullPath.Replace("\\", "/");
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (fullPath.StartsWith(dataPath))
                return "Assets" + fullPath.Substring(dataPath.Length);
            return "";
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
