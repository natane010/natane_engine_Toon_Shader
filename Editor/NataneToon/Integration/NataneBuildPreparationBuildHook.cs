using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NataneToon.Editor
{
    internal sealed class NataneBuildPreparationBuildHook : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            // キーワード同期: .mat ファイルのキーワード状態をプロパティ値と一致させ、ディスクに保存する。
            // VRChat SDK がディスクの .mat を直接読むため、ビルド前にディスク上で正しい状態にしておく必要がある。
            NataneShaderKeywordSynchronizer.SynchronizeAllNataneMaterials();

            NataneBuildPreparationService.PrepareForBuild(forceRefresh: false, logSummary: true);
        }
    }
}
