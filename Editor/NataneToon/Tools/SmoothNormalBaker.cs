using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// Smooth Normal Baker
    /// スムース法線ベイクツール - ハードエッジモデルのアウトライン表示を改善するため、
    /// スムース法線を頂点カラーにベイクする
    /// </summary>
    public class SmoothNormalBaker : EditorWindow
    {
        private enum BakeMode
        {
            ObjectSpace,
            TangentSpace
        }

        private Object targetObject;
        private BakeMode bakeMode = BakeMode.TangentSpace;
        private bool autoAssign = true;
        private Vector2 scrollPosition;

        private Mesh lastBakedMesh;

        [MenuItem("Tools/Natane/メッシュ Mesh/スムース法線ベイク Smooth Normal Baker", false, 60)]
        public static void ShowWindow()
        {
            var window = GetWindow<SmoothNormalBaker>("スムース法線ベイク");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            NataneToonShaderGUIUtility.DrawHeaderWithHelp(
                "スムース法線ベイクツール", "Smooth Normal Baker", "SmoothNormalBaker");
            EditorGUILayout.LabelField(
                "スムース法線を頂点カラーにベイクしてアウトラインを改善\nBake smooth normals into vertex colors for better outlines",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawTargetSelection();
            EditorGUILayout.Space(10);
            DrawBakeSettings();
            EditorGUILayout.Space(10);
            DrawBakeButton();
            EditorGUILayout.Space(10);
            DrawUsageHelp();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTargetSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ターゲットメッシュ", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "SkinnedMeshRenderer または MeshFilter を持つオブジェクトをドラッグしてください。\n" +
                "Drag an object with SkinnedMeshRenderer or MeshFilter.",
                MessageType.Info);

            targetObject = EditorGUILayout.ObjectField(
                "ターゲットオブジェクト", targetObject, typeof(GameObject), true);

            if (targetObject != null)
            {
                Mesh mesh = GetMeshFromTarget();
                if (mesh != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("メッシュ名", mesh.name);
                    EditorGUILayout.LabelField("頂点数", mesh.vertexCount.ToString());
                    EditorGUILayout.LabelField("サブメッシュ数", mesh.subMeshCount.ToString());

                    bool hasColors = mesh.colors != null && mesh.colors.Length > 0;
                    EditorGUILayout.LabelField("頂点カラー",
                        hasColors ? "あり (上書きされます)" : "なし");

                    if (hasColors)
                    {
                        EditorGUILayout.HelpBox(
                            "既存の頂点カラーはベイク結果で上書きされます。\n" +
                            "Existing vertex colors will be overwritten.",
                            MessageType.Warning);
                    }

                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "選択されたオブジェクトにメッシュが見つかりません。\n" +
                        "SkinnedMeshRenderer または MeshFilter が必要です。",
                        MessageType.Error);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBakeSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ベイク設定", EditorStyles.boldLabel);

            bakeMode = (BakeMode)EditorGUILayout.EnumPopup("ベイクモード", bakeMode);

            switch (bakeMode)
            {
                case BakeMode.ObjectSpace:
                    EditorGUILayout.HelpBox(
                        "Object Space: スムース法線をオブジェクト空間でエンコードします。\n" +
                        "シンプルで安定しますが、メッシュの変形には追従しません。",
                        MessageType.Info);
                    break;
                case BakeMode.TangentSpace:
                    EditorGUILayout.HelpBox(
                        "Tangent Space: スムース法線をタンジェント空間でエンコードします。\n" +
                        "スキニングやブレンドシェイプの変形に追従します（推奨）。",
                        MessageType.Info);
                    break;
            }

            autoAssign = EditorGUILayout.Toggle("自動アサイン", autoAssign);
            if (autoAssign)
            {
                EditorGUILayout.LabelField(
                    "  ベイク後に自動的にメッシュを差し替えます",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBakeButton()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("実行", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(targetObject == null || GetMeshFromTarget() == null);

            if (GUILayout.Button("ベイク実行", GUILayout.Height(35)))
            {
                ExecuteBake();
            }

            EditorGUI.EndDisabledGroup();

            if (lastBakedMesh != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(
                    $"最後のベイク結果: {lastBakedMesh.name}",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawUsageHelp()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("使い方", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "1. SkinnedMeshRenderer または MeshFilter を持つオブジェクトを選択\n" +
                "2. ベイクモードを選択（通常は Tangent Space を推奨）\n" +
                "3.「ベイク実行」ボタンを押す\n" +
                "4. ベイクされたメッシュが .asset として保存されます\n" +
                "5. シェーダーの「スムース法線アウトライン」を有効にしてください\n\n" +
                "※ 元のメッシュは変更されません（非破壊ワークフロー）\n" +
                "※ 頂点カラーの RGB チャンネルにスムース法線が格納されます",
                MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void ExecuteBake()
        {
            Mesh sourceMesh = GetMeshFromTarget();
            if (sourceMesh == null)
            {
                EditorUtility.DisplayDialog("エラー", "メッシュが見つかりません。", "OK");
                return;
            }

            bool useTangentSpace = bakeMode == BakeMode.TangentSpace;

            if (useTangentSpace && (sourceMesh.tangents == null || sourceMesh.tangents.Length == 0))
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "警告",
                    "メッシュにタンジェント情報がありません。\n" +
                    "Object Space モードに切り替えてベイクしますか？",
                    "Object Space でベイク",
                    "キャンセル");

                if (!proceed) return;
                useTangentSpace = false;
            }

            EditorUtility.DisplayProgressBar("スムース法線ベイク", "ベイク処理中...", 0.3f);

            Mesh bakedMesh;
            try
            {
                bakedMesh = BakeSmoothNormals(sourceMesh, useTangentSpace);
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("エラー",
                    $"ベイク中にエラーが発生しました:\n{e.Message}", "OK");
                Debug.LogException(e);
                return;
            }

            EditorUtility.DisplayProgressBar("スムース法線ベイク", "保存中...", 0.7f);

            string savedPath = SaveBakedMesh(bakedMesh, sourceMesh);

            EditorUtility.DisplayProgressBar("スムース法線ベイク", "完了", 1.0f);
            EditorUtility.ClearProgressBar();

            if (string.IsNullOrEmpty(savedPath))
            {
                Object.DestroyImmediate(bakedMesh);
                return;
            }

            Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savedPath);
            lastBakedMesh = savedMesh;

            if (autoAssign && targetObject != null)
            {
                AssignMeshToTarget(savedMesh);
            }

            EditorUtility.DisplayDialog(
                "ベイク完了",
                $"スムース法線をベイクしました。\n保存先: {savedPath}",
                "OK");

            Selection.activeObject = savedMesh;
            EditorGUIUtility.PingObject(savedMesh);
        }

        /// <summary>
        /// Bake smooth normals into vertex colors of a cloned mesh.
        /// スムース法線を頂点カラーにベイクしたクローンメッシュを返す。
        /// </summary>
        public static Mesh BakeSmoothNormals(Mesh sourceMesh, bool useTangentSpace)
        {
            Mesh bakedMesh = Object.Instantiate(sourceMesh);
            bakedMesh.name = sourceMesh.name + "_SmoothNormal";

            Vector3[] vertices = bakedMesh.vertices;
            Vector3[] normals = bakedMesh.normals;
            Vector4[] tangents = bakedMesh.tangents;

            // Group vertices by position (with quantization for floating point tolerance)
            var positionGroups = new Dictionary<Vector3Int, List<int>>();
            const float quantize = 10000f;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3Int key = new Vector3Int(
                    Mathf.RoundToInt(vertices[i].x * quantize),
                    Mathf.RoundToInt(vertices[i].y * quantize),
                    Mathf.RoundToInt(vertices[i].z * quantize)
                );
                if (!positionGroups.ContainsKey(key))
                    positionGroups[key] = new List<int>();
                positionGroups[key].Add(i);
            }

            // Average normals per position group
            Vector3[] smoothNormals = new Vector3[vertices.Length];
            foreach (var group in positionGroups)
            {
                Vector3 avgNormal = Vector3.zero;
                foreach (int idx in group.Value)
                    avgNormal += normals[idx];
                avgNormal.Normalize();

                foreach (int idx in group.Value)
                    smoothNormals[idx] = avgNormal;
            }

            // Encode to vertex colors
            Color[] colors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 encoded;
                if (useTangentSpace && tangents != null && tangents.Length > 0)
                {
                    // Transform to tangent space
                    Vector3 T = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                    Vector3 N = normals[i];
                    Vector3 B = Vector3.Cross(N, T) * tangents[i].w;

                    // Inverse TBN to transform object-space smooth normal to tangent space
                    Matrix4x4 tbnInverse = Matrix4x4.identity;
                    tbnInverse.SetRow(0, new Vector4(T.x, T.y, T.z, 0));
                    tbnInverse.SetRow(1, new Vector4(B.x, B.y, B.z, 0));
                    tbnInverse.SetRow(2, new Vector4(N.x, N.y, N.z, 0));
                    encoded = tbnInverse.MultiplyVector(smoothNormals[i]).normalized;
                }
                else
                {
                    encoded = smoothNormals[i];
                }

                // Encode [-1,1] -> [0,1]
                colors[i] = new Color(
                    encoded.x * 0.5f + 0.5f,
                    encoded.y * 0.5f + 0.5f,
                    encoded.z * 0.5f + 0.5f,
                    1.0f
                );
            }

            bakedMesh.colors = colors;
            return bakedMesh;
        }

        private Mesh GetMeshFromTarget()
        {
            if (targetObject == null) return null;

            GameObject go = targetObject as GameObject;
            if (go == null) return null;

            SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
                return smr.sharedMesh;

            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                return mf.sharedMesh;

            return null;
        }

        private string SaveBakedMesh(Mesh bakedMesh, Mesh sourceMesh)
        {
            // Determine save path based on source mesh location
            string sourcePath = AssetDatabase.GetAssetPath(sourceMesh);
            string directory;
            string defaultName = bakedMesh.name;

            if (!string.IsNullOrEmpty(sourcePath))
            {
                directory = System.IO.Path.GetDirectoryName(sourcePath);
            }
            else
            {
                directory = "Assets";
            }

            string savePath = EditorUtility.SaveFilePanelInProject(
                "スムース法線メッシュを保存",
                defaultName,
                "asset",
                "ベイクしたメッシュの保存場所を選択してください",
                directory);

            if (string.IsNullOrEmpty(savePath)) return null;

            // Check if an asset already exists at this path
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(bakedMesh, existing);
                Object.DestroyImmediate(bakedMesh);
                AssetDatabase.SaveAssets();
            }
            else
            {
                AssetDatabase.CreateAsset(bakedMesh, savePath);
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.Refresh();
            return savePath;
        }

        private void AssignMeshToTarget(Mesh mesh)
        {
            if (targetObject == null) return;

            GameObject go = targetObject as GameObject;
            if (go == null) return;

            SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                Undo.RecordObject(smr, "Assign Baked Smooth Normal Mesh");
                smr.sharedMesh = mesh;
                EditorUtility.SetDirty(smr);
                return;
            }

            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null)
            {
                Undo.RecordObject(mf, "Assign Baked Smooth Normal Mesh");
                mf.sharedMesh = mesh;
                EditorUtility.SetDirty(mf);
            }
        }
    }
}
