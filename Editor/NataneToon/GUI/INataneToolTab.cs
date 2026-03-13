using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    /// <summary>
    /// タブ付きツールウィンドウの各タブが実装するインターフェース
    /// タブのライフサイクル管理とGUI描画を定義する
    /// </summary>
    public interface INataneToolTab
    {
        /// <summary>ローカライズ済みタブ表示名</summary>
        string TabLabel { get; }

        /// <summary>タブのツールチップテキスト</summary>
        string TabTooltip { get; }

        /// <summary>UnifiedHelpSystem 参照用キー</summary>
        string HelpToolKey { get; }

        /// <summary>タブがアクティブになった時に呼ばれる</summary>
        void OnTabEnable(EditorWindow parentWindow);

        /// <summary>タブが非アクティブになった時に呼ばれる</summary>
        void OnTabDisable();

        /// <summary>タブのメインGUI描画</summary>
        void OnTabGUI(Material contextMaterial);

        /// <summary>タブ破棄時のクリーンアップ</summary>
        void OnTabDestroy();

        /// <summary>タブがマテリアルコンテキストを必要とするかどうか</summary>
        bool RequiresMaterial { get; }
    }
}
