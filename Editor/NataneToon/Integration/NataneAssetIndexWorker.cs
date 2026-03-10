using UnityEditor;

namespace NataneToon.Editor
{
    [InitializeOnLoad]
    internal static class NataneAssetIndexWorker
    {
        static NataneAssetIndexWorker()
        {
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!NataneBuildPolicySettings.instance.AutoIndexInEditor)
            {
                return;
            }

            NataneAssetIndexService.ProcessPendingWork(NataneBuildPolicySettings.instance.EditorUpdateBudgetMs);
        }
    }
}
