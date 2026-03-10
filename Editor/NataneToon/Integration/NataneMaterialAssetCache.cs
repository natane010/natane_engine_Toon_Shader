using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NataneToon.Editor
{
    public static class NataneMaterialAssetCache
    {
        public static IReadOnlyList<Material> GetAllMaterials()
        {
            return NataneAssetIndexService
                .EnumerateMaterialEntries()
                .Select(NataneAssetIndexService.LoadMaterial)
                .Where(material => material != null)
                .ToList();
        }

        public static IReadOnlyList<Material> GetMaterialsByShaderPrefix(string shaderNamePrefix)
        {
            return NataneAssetIndexService
                .EnumerateMaterialEntries(entry =>
                    string.IsNullOrEmpty(shaderNamePrefix) ||
                    (!string.IsNullOrEmpty(entry.shaderName) &&
                     entry.shaderName.StartsWith(shaderNamePrefix, StringComparison.OrdinalIgnoreCase)))
                .Select(NataneAssetIndexService.LoadMaterial)
                .Where(material => material != null)
                .ToList();
        }

        public static void Invalidate()
        {
        }
    }
}
