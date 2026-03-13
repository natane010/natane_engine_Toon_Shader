using System.Collections.Generic;
using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// アセンブリ間のツール起動を橋渡しする静的クラス
    /// NataneToon.Editor（ShaderGUI）から NataneToon.Editor.Tools（統合ウィンドウ）を
    /// 直接参照できないため、このブリッジを経由してタブ指定＋マテリアル渡しを行う
    /// </summary>
    public static class NataneToolBridge
    {
        /// <summary>開く際に渡すマテリアル（後方互換用グローバルペンディング）</summary>
        public static Material PendingMaterial { get; set; }

        /// <summary>開くタブのインデックス (0-based, -1 = デフォルト)（後方互換用グローバルペンディング）</summary>
        public static int PendingTabIndex { get; set; } = -1;

        // ウィンドウタイプ別ペンディングリクエスト（レースコンディション防止）
        private static readonly Dictionary<string, BridgeRequest> pendingRequests = new Dictionary<string, BridgeRequest>();

        private struct BridgeRequest
        {
            public Material Material;
            public int TabIndex;
        }

        /// <summary>
        /// 統合ウィンドウを開く（ウィンドウタイプキー指定版・レースコンディション安全）
        /// </summary>
        public static void OpenConsolidatedWindow(string menuPath, int tabIndex, Material material, string windowTypeKey)
        {
            pendingRequests[windowTypeKey] = new BridgeRequest { Material = material, TabIndex = tabIndex };
            NataneToolMenuPaths.TryExecute(menuPath);
        }

        /// <summary>
        /// 統合ウィンドウを開く（メニューパス経由 + タブ＆マテリアル受け渡し）
        /// 後方互換性のために旧シグネチャを維持
        /// </summary>
        public static void OpenConsolidatedWindow(string menuPath, int tabIndex, Material material)
        {
            PendingMaterial = material;
            PendingTabIndex = tabIndex;
            NataneToolMenuPaths.TryExecute(menuPath);
        }

        /// <summary>
        /// ウィンドウタイプキー指定でペンディングリクエストを消費する
        /// 旧グローバルペンディングへのフォールバック付き
        /// </summary>
        public static bool TryConsume(string windowTypeKey, out Material material, out int tabIndex)
        {
            // 新パターン: ウィンドウタイプ別ペンディング
            if (pendingRequests.TryGetValue(windowTypeKey, out var req))
            {
                material = req.Material;
                tabIndex = req.TabIndex;
                pendingRequests.Remove(windowTypeKey);
                return true;
            }

            // フォールバック: 旧グローバルペンディング（後方互換）
            if (PendingTabIndex >= 0)
            {
                material = PendingMaterial;
                tabIndex = PendingTabIndex;
                ClearPending();
                return true;
            }

            material = null;
            tabIndex = -1;
            return false;
        }

        /// <summary>
        /// ペンディング状態をクリアする（ウィンドウ側で呼び出す）
        /// </summary>
        public static void ClearPending()
        {
            PendingMaterial = null;
            PendingTabIndex = -1;
        }
    }
}
