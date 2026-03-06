using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    public static class NataneMaterialAssetCache
    {
        private static bool isDirty = true;
        private static List<Material> allMaterials = new List<Material>();
        private static readonly Dictionary<string, List<Material>> materialsByShaderPrefix =
            new Dictionary<string, List<Material>>(StringComparer.OrdinalIgnoreCase);

        static NataneMaterialAssetCache()
        {
            EditorApplication.projectChanged += Invalidate;
        }

        public static IReadOnlyList<Material> GetAllMaterials()
        {
            EnsureBuilt();
            return allMaterials;
        }

        public static IReadOnlyList<Material> GetMaterialsByShaderPrefix(string shaderNamePrefix)
        {
            EnsureBuilt();

            if (string.IsNullOrEmpty(shaderNamePrefix))
            {
                return allMaterials;
            }

            if (!materialsByShaderPrefix.TryGetValue(shaderNamePrefix, out List<Material> materials))
            {
                materials = allMaterials
                    .Where(material => material != null &&
                                       material.shader != null &&
                                       material.shader.name.StartsWith(shaderNamePrefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                materialsByShaderPrefix[shaderNamePrefix] = materials;
            }

            return materials;
        }

        public static void Invalidate()
        {
            isDirty = true;
            materialsByShaderPrefix.Clear();
        }

        private static void EnsureBuilt()
        {
            if (!isDirty)
            {
                return;
            }

            allMaterials = AssetDatabase.FindAssets("t:Material")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Material>)
                .Where(material => material != null)
                .ToList();

            materialsByShaderPrefix.Clear();
            isDirty = false;
        }
    }
}
