using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NataneToon.Editor
{
    /// <summary>
    /// VRChat SDK 等のビルドパイプラインでシーン処理時にマテリアルキーワードを同期する。
    /// IProcessSceneWithReport はビルド対象シーンがロードされた後、
    /// アセットバンドル化される前に呼ばれるため、ここでキーワードを修正すれば
    /// ビルド成果物に正しいキーワード状態が反映される。
    /// </summary>
    internal sealed class NataneBuildSceneKeywordSync : IProcessSceneWithReport
    {
        // VRChat SDK より先に実行（VRChat SDK は通常 callbackOrder = 0 付近）
        public int callbackOrder => -50;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // エディタ再生時は処理しない（ビルド時のみ）
            if (report == null)
                return;

            var renderers = new List<Renderer>();
            var rootObjects = scene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                root.GetComponentsInChildren(true, renderers);
            }

            int fixedCount = 0;
            var processedMaterials = new HashSet<int>(); // instanceID で重複排除

            foreach (var renderer in renderers)
            {
                var sharedMaterials = renderer.sharedMaterials;
                foreach (var material in sharedMaterials)
                {
                    if (material == null || material.shader == null)
                        continue;

                    int id = material.GetInstanceID();
                    if (!processedMaterials.Add(id))
                        continue;

                    if (!NataneShaderCatalog.IsNataneShader(material.shader.name))
                        continue;

                    if (NataneShaderKeywordSynchronizer.SynchronizeMaterialKeywords(material))
                    {
                        fixedCount++;
                    }
                }
            }

            if (fixedCount > 0)
            {
                Debug.Log($"[NataneToonShader] ビルド時キーワード同期: シーン '{scene.name}' の {fixedCount} マテリアルを修正しました");
            }
        }
    }
}
