using UnityEngine;
using UnityEditor;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Dockable 3D preview window for mask textures.
    /// マスクテクスチャ用のドッキング可能な3Dプレビューウィンドウ
    /// </summary>
    internal class MaskTexture3DPreviewWindow : EditorWindow
    {
        private MaskTexture3DPreview preview3D;

        [MenuItem("Tools/Natane/3Dプレビュー 3D Preview", false, 145)]
        public static void Open()
        {
            var window = GetWindow<MaskTexture3DPreviewWindow>(L("3Dプレビュー", "3D Preview"));
            window.minSize = new Vector2(250, 250);
            window.Show();
        }

        private UVTextureGenerator FindParent()
        {
            var parents = Resources.FindObjectsOfTypeAll<UVTextureGenerator>();
            return parents.FirstOrDefault();
        }

        private void OnEnable()
        {
            preview3D = new MaskTexture3DPreview();
        }

        private void OnDisable()
        {
            preview3D?.Dispose();
            preview3D = null;
        }

        private void OnGUI()
        {
            var parent = FindParent();
            if (parent == null)
            {
                EditorGUILayout.HelpBox(
                    L("テクスチャスタジオを開いてください。",
                      "Please open Texture Studio first."),
                    MessageType.Info);
                return;
            }

            Mesh mesh = parent.CurrentMesh;
            Texture2D texture = parent.CurrentPreviewTexture;

            if (mesh == null)
            {
                EditorGUILayout.HelpBox(
                    L("3Dプレビューにはメッシュが必要です。メッシュソースを設定してください。",
                      "Mesh is required for 3D preview. Set a mesh source."),
                    MessageType.Info);
                return;
            }

            if (preview3D == null)
                preview3D = new MaskTexture3DPreview();

            preview3D.SetMesh(mesh);
            if (texture != null)
                preview3D.SetTexture(texture);

            preview3D.DrawPreviewUI();

            Rect previewRect = GUILayoutUtility.GetRect(0, position.height - 30f,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            preview3D.HandleInput(previewRect);
            preview3D.DrawPreview(previewRect);

            if (Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDrag)
                Repaint();
        }
    }
}
