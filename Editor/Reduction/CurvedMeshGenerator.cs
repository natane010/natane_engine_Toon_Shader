using UnityEngine;

namespace Reduction.Editor
{
    /// <summary>
    /// Static utility class for generating curved mesh geometry used by the
    /// background reduction tool. All meshes have inward-facing normals,
    /// UV range [0,1], and analytically computed tangents for POM accuracy.
    /// </summary>
    public static class CurvedMeshGenerator
    {
        /// <summary>
        /// Generates a half-cylinder centred on the origin, spanning from -arc/2 to +arc/2
        /// around the Y axis. Normals point inward (toward the viewer at the origin).
        /// </summary>
        /// <param name="arc">Horizontal arc angle in degrees.</param>
        /// <param name="height">Total height of the cylinder.</param>
        /// <param name="radius">Radius of the cylinder.</param>
        /// <param name="subdivU">Number of horizontal subdivisions.</param>
        /// <param name="subdivV">Number of vertical subdivisions.</param>
        public static Mesh GenerateCylinder(float arc, float height, float radius, int subdivU, int subdivV)
        {
            subdivU = Mathf.Max(subdivU, 2);
            subdivV = Mathf.Max(subdivV, 2);

            int vertCount = (subdivU + 1) * (subdivV + 1);
            var vertices = new Vector3[vertCount];
            var normals  = new Vector3[vertCount];
            var tangents = new Vector4[vertCount];
            var uvs      = new Vector2[vertCount];

            float halfArc    = arc * Mathf.Deg2Rad * 0.5f;
            float halfHeight = height * 0.5f;

            int idx = 0;
            for (int v = 0; v <= subdivV; v++)
            {
                float tv = (float)v / subdivV;
                float y = Mathf.Lerp(halfHeight, -halfHeight, tv);

                for (int u = 0; u <= subdivU; u++)
                {
                    float tu = (float)u / subdivU;
                    float angle = Mathf.Lerp(-halfArc, halfArc, tu);

                    float x = Mathf.Sin(angle) * radius;
                    float z = Mathf.Cos(angle) * radius;

                    vertices[idx] = new Vector3(x, y, z);

                    // Inward-facing normal
                    normals[idx] = new Vector3(-Mathf.Sin(angle), 0f, -Mathf.Cos(angle));

                    // Analytical tangent: derivative of position w.r.t. angle (horizontal)
                    // d/dangle (sin(a)*r, 0, cos(a)*r) = (cos(a)*r, 0, -sin(a)*r)
                    // For inward-facing surface, we reverse it
                    Vector3 tan = new Vector3(-Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    tangents[idx] = new Vector4(tan.x, tan.y, tan.z, -1f);

                    uvs[idx] = new Vector2(tu, 1f - tv);
                    idx++;
                }
            }

            int[] triangles = GenerateGridTriangles(subdivU, subdivV, true);

            var mesh = new Mesh();
            mesh.name = "CurvedCylinder";
            mesh.vertices  = vertices;
            mesh.normals   = normals;
            mesh.tangents  = tangents;
            mesh.uv        = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Generates a spherical patch centred on the +Z direction, spanning
        /// hArc horizontally and vArc vertically. Normals point inward.
        /// </summary>
        /// <param name="hArc">Horizontal arc angle in degrees.</param>
        /// <param name="vArc">Vertical arc angle in degrees.</param>
        /// <param name="radius">Sphere radius.</param>
        /// <param name="subdivU">Horizontal subdivisions.</param>
        /// <param name="subdivV">Vertical subdivisions.</param>
        public static Mesh GenerateSpherePatch(float hArc, float vArc, float radius, int subdivU, int subdivV)
        {
            subdivU = Mathf.Max(subdivU, 2);
            subdivV = Mathf.Max(subdivV, 2);

            int vertCount = (subdivU + 1) * (subdivV + 1);
            var vertices = new Vector3[vertCount];
            var normals  = new Vector3[vertCount];
            var tangents = new Vector4[vertCount];
            var uvs      = new Vector2[vertCount];

            float halfH = hArc * Mathf.Deg2Rad * 0.5f;
            float halfV = vArc * Mathf.Deg2Rad * 0.5f;

            int idx = 0;
            for (int v = 0; v <= subdivV; v++)
            {
                float tv = (float)v / subdivV;
                float phi = Mathf.Lerp(halfV, -halfV, tv); // top to bottom

                for (int u = 0; u <= subdivU; u++)
                {
                    float tu = (float)u / subdivU;
                    float theta = Mathf.Lerp(-halfH, halfH, tu);

                    // Spherical coordinates (theta around Y, phi around X from Z axis)
                    float cosPhi   = Mathf.Cos(phi);
                    float sinPhi   = Mathf.Sin(phi);
                    float cosTheta = Mathf.Cos(theta);
                    float sinTheta = Mathf.Sin(theta);

                    float x = sinTheta * cosPhi * radius;
                    float y = sinPhi * radius;
                    float z = cosTheta * cosPhi * radius;

                    vertices[idx] = new Vector3(x, y, z);

                    // Inward-facing normal
                    Vector3 outward = new Vector3(x, y, z).normalized;
                    normals[idx] = -outward;

                    // Analytical tangent: d/dtheta of position
                    // dx/dtheta = cos(theta)*cos(phi)*r
                    // dy/dtheta = 0
                    // dz/dtheta = -sin(theta)*cos(phi)*r
                    // For inward face, negate
                    Vector3 tan = new Vector3(
                        -cosTheta * cosPhi,
                        0f,
                        sinTheta * cosPhi
                    ).normalized;
                    tangents[idx] = new Vector4(tan.x, tan.y, tan.z, -1f);

                    uvs[idx] = new Vector2(tu, 1f - tv);
                    idx++;
                }
            }

            int[] triangles = GenerateGridTriangles(subdivU, subdivV, true);

            var mesh = new Mesh();
            mesh.name = "SpherePatch";
            mesh.vertices  = vertices;
            mesh.normals   = normals;
            mesh.tangents  = tangents;
            mesh.uv        = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Generates a subdivided plane facing -Z (toward the viewer at origin).
        /// Normals point inward (toward -Z).
        /// </summary>
        /// <param name="width">Width of the plane.</param>
        /// <param name="height">Height of the plane.</param>
        /// <param name="subdivU">Horizontal subdivisions.</param>
        /// <param name="subdivV">Vertical subdivisions.</param>
        public static Mesh GeneratePlane(float width, float height, int subdivU, int subdivV)
        {
            subdivU = Mathf.Max(subdivU, 2);
            subdivV = Mathf.Max(subdivV, 2);

            int vertCount = (subdivU + 1) * (subdivV + 1);
            var vertices = new Vector3[vertCount];
            var normals  = new Vector3[vertCount];
            var tangents = new Vector4[vertCount];
            var uvs      = new Vector2[vertCount];

            float halfW = width * 0.5f;
            float halfH = height * 0.5f;

            int idx = 0;
            for (int v = 0; v <= subdivV; v++)
            {
                float tv = (float)v / subdivV;
                float y = Mathf.Lerp(halfH, -halfH, tv);

                for (int u = 0; u <= subdivU; u++)
                {
                    float tu = (float)u / subdivU;
                    float x = Mathf.Lerp(-halfW, halfW, tu);

                    vertices[idx] = new Vector3(x, y, 0f);

                    // Inward-facing (toward viewer at -Z side would mean normal = -Z,
                    // but plane is placed at Z=distance facing back, so normal = -Z)
                    normals[idx] = new Vector3(0f, 0f, -1f);

                    // Tangent along +X, bitangent sign -1 for inward face
                    tangents[idx] = new Vector4(1f, 0f, 0f, -1f);

                    uvs[idx] = new Vector2(tu, 1f - tv);
                    idx++;
                }
            }

            int[] triangles = GenerateGridTriangles(subdivU, subdivV, true);

            var mesh = new Mesh();
            mesh.name = "SubdividedPlane";
            mesh.vertices  = vertices;
            mesh.normals   = normals;
            mesh.tangents  = tangents;
            mesh.uv        = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Generates triangle indices for a subdivU x subdivV grid.
        /// When inwardFacing is true, winding order is reversed for inside-out rendering.
        /// </summary>
        private static int[] GenerateGridTriangles(int subdivU, int subdivV, bool inwardFacing)
        {
            int quadCount = subdivU * subdivV;
            int[] triangles = new int[quadCount * 6];

            int ti = 0;
            for (int v = 0; v < subdivV; v++)
            {
                for (int u = 0; u < subdivU; u++)
                {
                    int bottomLeft  = v * (subdivU + 1) + u;
                    int bottomRight = bottomLeft + 1;
                    int topLeft     = bottomLeft + (subdivU + 1);
                    int topRight    = topLeft + 1;

                    if (inwardFacing)
                    {
                        // CW winding for inward-facing normals
                        triangles[ti++] = bottomLeft;
                        triangles[ti++] = topRight;
                        triangles[ti++] = topLeft;

                        triangles[ti++] = bottomLeft;
                        triangles[ti++] = bottomRight;
                        triangles[ti++] = topRight;
                    }
                    else
                    {
                        // CCW winding for outward-facing normals
                        triangles[ti++] = bottomLeft;
                        triangles[ti++] = topLeft;
                        triangles[ti++] = topRight;

                        triangles[ti++] = bottomLeft;
                        triangles[ti++] = topRight;
                        triangles[ti++] = bottomRight;
                    }
                }
            }

            return triangles;
        }
    }
}
