using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

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
            var window = GetWindow<SmoothNormalBaker>(L("スムース法線ベイク", "Smooth Normal Baker"));
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
                L("スムース法線を頂点カラーにベイクしてアウトラインを改善",
                  "Bake smooth normals into vertex colors for better outlines"),
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
            EditorGUILayout.LabelField(L("ターゲットメッシュ", "Target Mesh"), EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                L("SkinnedMeshRenderer または MeshFilter を持つオブジェクトをドラッグしてください。",
                  "Drag an object with SkinnedMeshRenderer or MeshFilter."),
                MessageType.Info);

            targetObject = EditorGUILayout.ObjectField(
                L("ターゲットオブジェクト", "Target Object"), targetObject, typeof(GameObject), true);

            if (targetObject != null)
            {
                Mesh mesh = GetMeshFromTarget();
                if (mesh != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(L("メッシュ名", "Mesh Name"), mesh.name);
                    EditorGUILayout.LabelField(L("頂点数", "Vertex Count"), mesh.vertexCount.ToString());
                    EditorGUILayout.LabelField(L("サブメッシュ数", "Submesh Count"), mesh.subMeshCount.ToString());

                    bool hasColors = mesh.colors != null && mesh.colors.Length > 0;
                    EditorGUILayout.LabelField(L("頂点カラー", "Vertex Colors"),
                        hasColors ? L("あり (上書きされます)", "Present (will be overwritten)") : L("なし", "None"));

                    if (hasColors)
                    {
                        EditorGUILayout.HelpBox(
                            L("既存の頂点カラーはベイク結果で上書きされます。",
                              "Existing vertex colors will be overwritten."),
                            MessageType.Warning);
                    }

                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        L("選択されたオブジェクトにメッシュが見つかりません。\nSkinnedMeshRenderer または MeshFilter が必要です。",
                          "No mesh found on the selected object.\nA SkinnedMeshRenderer or MeshFilter is required."),
                        MessageType.Error);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBakeSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("ベイク設定", "Bake Settings"), EditorStyles.boldLabel);

            bakeMode = (BakeMode)EditorGUILayout.EnumPopup(L("ベイクモード", "Bake Mode"), bakeMode);

            switch (bakeMode)
            {
                case BakeMode.ObjectSpace:
                    EditorGUILayout.HelpBox(
                        L("Object Space: スムース法線をオブジェクト空間でエンコードします。\nシンプルで安定しますが、メッシュの変形には追従しません。",
                          "Object Space: Encodes smooth normals in object space.\nSimple and stable, but does not follow mesh deformation."),
                        MessageType.Info);
                    break;
                case BakeMode.TangentSpace:
                    EditorGUILayout.HelpBox(
                        L("Tangent Space: スムース法線をタンジェント空間でエンコードします。\nスキニングやブレンドシェイプの変形に追従します（推奨）。",
                          "Tangent Space: Encodes smooth normals in tangent space.\nFollows skinning and blend shape deformation (recommended)."),
                        MessageType.Info);
                    break;
            }

            autoAssign = EditorGUILayout.Toggle(L("自動アサイン", "Auto Assign"), autoAssign);
            if (autoAssign)
            {
                EditorGUILayout.LabelField(
                    L("  ベイク後に自動的にメッシュを差し替えます", "  Automatically replaces mesh after baking"),
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBakeButton()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("実行", "Execute"), EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(targetObject == null || GetMeshFromTarget() == null);

            if (GUILayout.Button(L("ベイク実行", "Bake"), GUILayout.Height(35)))
            {
                ExecuteBake();
            }

            EditorGUI.EndDisabledGroup();

            if (lastBakedMesh != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(
                    L($"最後のベイク結果: {lastBakedMesh.name}", $"Last bake result: {lastBakedMesh.name}"),
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawUsageHelp()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("使い方", "Usage"), EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                L("1. SkinnedMeshRenderer または MeshFilter を持つオブジェクトを選択\n" +
                  "2. ベイクモードを選択（通常は Tangent Space を推奨）\n" +
                  "3.「ベイク実行」ボタンを押す\n" +
                  "4. ベイクされたメッシュが .asset として保存されます\n" +
                  "5. シェーダーの「スムース法線アウトライン」を有効にしてください\n\n" +
                  "※ 元のメッシュは変更されません（非破壊ワークフロー）\n" +
                  "※ 頂点カラーの RGB チャンネルにスムース法線が格納されます",
                  "1. Select an object with SkinnedMeshRenderer or MeshFilter\n" +
                  "2. Choose a bake mode (Tangent Space recommended)\n" +
                  "3. Press the 'Bake' button\n" +
                  "4. The baked mesh will be saved as a .asset file\n" +
                  "5. Enable 'Smooth Normal Outline' in the shader\n\n" +
                  "* The original mesh is not modified (non-destructive workflow)\n" +
                  "* Smooth normals are stored in the RGB channels of vertex colors"),
                MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void ExecuteBake()
        {
            Mesh sourceMesh = GetMeshFromTarget();
            if (sourceMesh == null)
            {
                EditorUtility.DisplayDialog(L("エラー", "Error"), L("メッシュが見つかりません。", "Mesh not found."), "OK");
                return;
            }

            bool useTangentSpace = bakeMode == BakeMode.TangentSpace;

            if (useTangentSpace && (sourceMesh.tangents == null || sourceMesh.tangents.Length == 0))
            {
                bool proceed = EditorUtility.DisplayDialog(
                    L("警告", "Warning"),
                    L("メッシュにタンジェント情報がありません。\nObject Space モードに切り替えてベイクしますか？",
                      "The mesh has no tangent information.\nSwitch to Object Space mode and bake?"),
                    L("Object Space でベイク", "Bake in Object Space"),
                    L("キャンセル", "Cancel"));

                if (!proceed) return;
                useTangentSpace = false;
            }

            EditorUtility.DisplayProgressBar(L("スムース法線ベイク", "Smooth Normal Bake"), L("ベイク処理中...", "Baking..."), 0.3f);

            Mesh bakedMesh;
            try
            {
                bakedMesh = BakeSmoothNormals(sourceMesh, useTangentSpace);
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(L("エラー", "Error"),
                    L($"ベイク中にエラーが発生しました:\n{e.Message}",
                      $"An error occurred during baking:\n{e.Message}"), "OK");
                Debug.LogException(e);
                return;
            }

            EditorUtility.DisplayProgressBar(L("スムース法線ベイク", "Smooth Normal Bake"), L("保存中...", "Saving..."), 0.7f);

            string savedPath = SaveBakedMesh(bakedMesh, sourceMesh);

            EditorUtility.DisplayProgressBar(L("スムース法線ベイク", "Smooth Normal Bake"), L("完了", "Complete"), 1.0f);
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
                L("ベイク完了", "Bake Complete"),
                L($"スムース法線をベイクしました。\n保存先: {savedPath}",
                  $"Smooth normals have been baked.\nSaved to: {savedPath}"),
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
                L("スムース法線メッシュを保存", "Save Smooth Normal Mesh"),
                defaultName,
                "asset",
                L("ベイクしたメッシュの保存場所を選択してください", "Choose a location to save the baked mesh"),
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
