using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace NataneToon.Editor
{
    /// <summary>
    /// Extracts UV wireframe data from Unity meshes and sends it to
    /// NataneTextureStudio via the TextureStudioBridge named pipe.
    /// UV data is sent as JSON for display as a wireframe overlay on the painting canvas.
    /// </summary>
    internal static class TextureStudioUVExporter
    {
        /// <summary>
        /// Extract UV wireframe data from a mesh for a specific submesh (material slot).
        /// Sends as JSON to the standalone Texture Studio.
        /// </summary>
        /// <param name="mesh">Source mesh to extract UVs from.</param>
        /// <param name="submeshIndex">Material slot index, or -1 for all submeshes.</param>
        public static void SendUVWireframe(Mesh mesh, int submeshIndex = -1)
        {
            if (mesh == null) return;

            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);
            if (uvs.Count == 0) return;

            // Build triangle list for the submesh (or all if -1)
            var triangleData = new List<int[]>();

            if (submeshIndex >= 0 && submeshIndex < mesh.subMeshCount)
            {
                int[] tris = mesh.GetTriangles(submeshIndex);
                triangleData.Add(tris);
            }
            else
            {
                for (int s = 0; s < mesh.subMeshCount; s++)
                    triangleData.Add(mesh.GetTriangles(s));
            }

            // Build JSON: array of UV coords + array of triangle indices per submesh
            var sb = new StringBuilder();
            sb.Append("{\"method\":\"setUVOverlay\",\"params\":{");

            // UVs array: [[u,v], [u,v], ...]
            sb.Append("\"uvs\":[");
            for (int i = 0; i < uvs.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('[');
                sb.Append(uvs[i].x.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(',');
                sb.Append(uvs[i].y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(']');
            }
            sb.Append("],");

            // Submeshes: [{"indices": [0,1,2,...]}, ...]
            sb.Append("\"submeshes\":[");
            for (int s = 0; s < triangleData.Count; s++)
            {
                if (s > 0) sb.Append(',');
                sb.Append("{\"indices\":[");
                var tris = triangleData[s];
                for (int i = 0; i < tris.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(tris[i]);
                }
                sb.Append("]}");
            }
            sb.Append("]}}");

            TextureStudioBridge.SendRaw(sb.ToString());
        }

        /// <summary>
        /// Extract a Mesh from the currently selected GameObject's MeshFilter or SkinnedMeshRenderer.
        /// </summary>
        /// <returns>The shared mesh, or null if no mesh component is found.</returns>
        public static Mesh GetMeshFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null) return null;

            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) return mf.sharedMesh;

            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null) return smr.sharedMesh;

            return null;
        }

        /// <summary>
        /// Send UV wireframe for a specific material slot on a GameObject.
        /// </summary>
        /// <param name="go">GameObject with MeshFilter or SkinnedMeshRenderer.</param>
        /// <param name="materialSlotIndex">Index of the material slot (submesh).</param>
        public static void SendUVForMaterialSlot(GameObject go, int materialSlotIndex)
        {
            if (go == null) return;

            Mesh mesh = null;
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
            else
            {
                var smr = go.GetComponent<SkinnedMeshRenderer>();
                if (smr != null) mesh = smr.sharedMesh;
            }
            if (mesh == null) return;

            SendUVWireframe(mesh, materialSlotIndex);
        }

        /// <summary>
        /// Send a clear command to hide the UV overlay in Texture Studio.
        /// </summary>
        public static void ClearUVWireframe()
        {
            TextureStudioBridge.SendRaw("{\"method\":\"clearUVOverlay\",\"params\":{}}");
        }
    }
}
