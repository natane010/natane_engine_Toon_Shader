using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NataneToon.Editor
{
    internal sealed class NataneBuildPreparationBuildHook : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            NataneBuildPreparationService.PrepareForBuild(forceRefresh: false, logSummary: true);
        }
    }
}
