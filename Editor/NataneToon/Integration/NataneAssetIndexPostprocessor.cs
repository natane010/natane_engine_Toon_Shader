using UnityEditor;

namespace NataneToon.Editor
{
    internal sealed class NataneAssetIndexPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            NataneAssetIndexService.MarkAssetsDirty(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }
    }
}
