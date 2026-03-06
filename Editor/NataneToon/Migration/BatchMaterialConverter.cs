using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// プロジェクト全体のマテリアルを一括変換するツール
    /// </summary>
    public class BatchMaterialConverter : EditorWindow
    {
        private string sourceShaderName = "lilToon";
        private string targetShaderPath = "Natane/Toon Shader";
        private bool searchInScenes = true;
        private bool searchInPrefabs = true;
        private bool updateReferences = true;
        private Vector2 windowScrollPosition;
        private Vector2 scrollPosition;
        private List<MaterialConversionInfo> conversionInfos = new List<MaterialConversionInfo>();
        private const float CompactLayoutWidth = 720f;

        private class MaterialConversionInfo
        {
            public Material material;
            public List<GameObject> affectedObjects = new List<GameObject>();
            public bool willConvert = true;
        }

        [MenuItem("Tools/Natane/移行 Migration/一括マテリアル変換 Batch Material Converter", false, 53)]
        public static void ShowWindow()
        {
            var window = GetWindow<BatchMaterialConverter>(L("一括マテリアル変換", "Batch Material Converter"));
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition);
            EditorGUILayout.LabelField(L("一括マテリアル変換ツール", "Batch Material Converter"), EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                L("このツールはプロジェクト全体から特定のシェーダーを使用しているマテリアルを検索し、\n" +
                "別のシェーダーに変換します。シーンやPrefabの参照も更新できます。",
                "This tool finds and converts materials from one shader to another across your entire project.\n" +
                "It can also update references in scenes and prefabs."),
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("変換設定", "Conversion Settings"), EditorStyles.boldLabel);

            sourceShaderName = EditorGUILayout.TextField(L("変換元シェーダー名", "Source Shader Name:"), sourceShaderName);
            targetShaderPath = EditorGUILayout.TextField(L("変換先シェーダー", "Target Shader:"), targetShaderPath);

            EditorGUILayout.Space();

            searchInScenes = EditorGUILayout.Toggle(L("シーン内を検索", "Search in Scenes"), searchInScenes);
            searchInPrefabs = EditorGUILayout.Toggle(L("Prefab内を検索", "Search in Prefabs"), searchInPrefabs);
            updateReferences = EditorGUILayout.Toggle(L("オブジェクト参照を更新", "Update Object References"), updateReferences);

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();

            // Scan button
            if (GUILayout.Button(L("プロジェクトをスキャン", "Scan Project"), GUILayout.Height(30)))
            {
                ScanProject();
            }

            EditorGUILayout.Space();

            // Results
            if (conversionInfos.Count > 0)
            {
                EditorGUILayout.LabelField(L($"見つかったマテリアル: {conversionInfos.Count}個", $"Found {conversionInfos.Count} Materials"), EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(GetAdaptiveListHeight(160f, 320f, 0.35f)));

                foreach (var info in conversionInfos)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();
                    info.willConvert = EditorGUILayout.Toggle(info.willConvert, GUILayout.Width(20));
                    EditorGUILayout.ObjectField(info.material, typeof(Material), false);
                    EditorGUILayout.EndHorizontal();

                    if (info.affectedObjects.Count > 0)
                    {
                        EditorGUILayout.LabelField(L($"{info.affectedObjects.Count}個のオブジェクトが使用", $"Used by {info.affectedObjects.Count} objects"), EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                // Convert button
                int selectedCount = conversionInfos.Count(i => i.willConvert);
                GUI.enabled = selectedCount > 0;

                if (GUILayout.Button(L($"選択した{selectedCount}個を変換", $"Convert {selectedCount} Selected Materials"), GUILayout.Height(40)))
                {
                    ConvertSelectedMaterials();
                }

                GUI.enabled = true;

                EditorGUILayout.Space();

                // Select/Deselect all
                if (IsCompactLayout())
                {
                    if (GUILayout.Button(L("すべて選択", "Select All")))
                    {
                        conversionInfos.ForEach(i => i.willConvert = true);
                    }

                    if (GUILayout.Button(L("すべて解除", "Deselect All")))
                    {
                        conversionInfos.ForEach(i => i.willConvert = false);
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(L("すべて選択", "Select All")))
                    {
                        conversionInfos.ForEach(i => i.willConvert = true);
                    }

                    if (GUILayout.Button(L("すべて解除", "Deselect All")))
                    {
                        conversionInfos.ForEach(i => i.willConvert = false);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// シェーダー名が検索文字列と一致するかを判定する。
        /// 完全一致、またはパス区切り（"/"）で始まるバリアントにマッチする。
        /// 例: "lilToon" → "lilToon" (一致), "Hidden/lilToon" (末尾一致),
        ///      "lilToon/Cutout" (先頭一致) にマッチするが、"Wirelight" にはマッチしない。
        /// </summary>
        private static bool MatchesShaderName(string shaderName, string source)
        {
            if (string.IsNullOrEmpty(shaderName) || string.IsNullOrEmpty(source))
                return false;

            // 完全一致
            if (shaderName == source)
                return true;

            // パス区切りでのプレフィックス一致 (例: "lilToon/Cutout")
            if (shaderName.StartsWith(source + "/"))
                return true;

            // パス区切りでのサフィックス一致 (例: "Hidden/lilToon")
            if (shaderName.EndsWith("/" + source))
                return true;

            // パス中間に含まれる場合 (例: "Hidden/lilToon/Cutout")
            if (shaderName.Contains("/" + source + "/"))
                return true;

            return false;
        }

        private void ScanProject()
        {
            conversionInfos.Clear();

            // Find all materials with source shader
            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");

            foreach (string guid in materialGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && material.shader != null &&
                    MatchesShaderName(material.shader.name, sourceShaderName))
                {
                    var info = new MaterialConversionInfo { material = material };

                    // Find objects using this material
                    if (searchInScenes || searchInPrefabs)
                    {
                        FindObjectsUsingMaterial(material, info);
                    }

                    conversionInfos.Add(info);
                }
            }

            Debug.Log($"Scan complete. Found {conversionInfos.Count} materials.");
        }

        private void FindObjectsUsingMaterial(Material material, MaterialConversionInfo info)
        {
            // Search in prefabs
            if (searchInPrefabs)
            {
                string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");

                foreach (string guid in prefabGUIDs)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab != null)
                    {
                        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                        foreach (var renderer in renderers)
                        {
                            if (renderer.sharedMaterials.Contains(material))
                            {
                                info.affectedObjects.Add(prefab);
                                break;
                            }
                        }
                    }
                }
            }

            // Search in current scene
            if (searchInScenes)
            {
                var allRenderers = GameObject.FindObjectsOfType<Renderer>(true);
                foreach (var renderer in allRenderers)
                {
                    if (renderer.sharedMaterials.Contains(material))
                    {
                        info.affectedObjects.Add(renderer.gameObject);
                    }
                }
            }
        }

        private void ConvertSelectedMaterials()
        {
            var selectedInfos = conversionInfos.Where(i => i.willConvert).ToList();

            if (!EditorUtility.DisplayDialog(
                L("マテリアルを変換", "Convert Materials"),
                L($"{selectedInfos.Count}個のマテリアルを変換してもよろしいですか？\n" +
                "Unity の Undo で戻せますが、プロジェクトのバックアップを推奨します。",
                $"Are you sure you want to convert {selectedInfos.Count} materials?\n" +
                "You can revert the material changes with Unity Undo, but a project backup is still recommended."),
                L("変換", "Convert"), L("キャンセル", "Cancel")))
            {
                return;
            }

            // Find target shader
            Shader targetShader = Shader.Find(targetShaderPath);
            if (targetShader == null)
            {
                EditorUtility.DisplayDialog(L("エラー", "Error"), L($"変換先シェーダー '{targetShaderPath}' が見つかりません！", $"Target shader '{targetShaderPath}' not found!"), "OK");
                return;
            }

            int successCount = 0;

            try
            {
                for (int i = 0; i < selectedInfos.Count; i++)
                {
                    var info = selectedInfos[i];

                    EditorUtility.DisplayProgressBar(
                        L("マテリアル変換中", "Converting Materials"),
                        L($"{i + 1}/{selectedInfos.Count}: {info.material.name} を変換中",
                          $"Converting {i + 1}/{selectedInfos.Count}: {info.material.name}"),
                        (float)i / selectedInfos.Count
                    );

                    try
                    {
                        Undo.RecordObject(info.material, "Convert Material Shader");

                        // Store original properties
                        var originalProps = CaptureAllProperties(info.material);

                        // Change shader
                        info.material.shader = targetShader;

                        // Try to restore compatible properties
                        RestoreCompatibleProperties(info.material, originalProps);

                        EditorUtility.SetDirty(info.material);
                        successCount++;

                        Debug.Log($"Converted: {info.material.name}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to convert {info.material.name}: {e.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                L("変換完了", "Conversion Complete"),
                L($"{successCount}/{selectedInfos.Count}個のマテリアルを正常に変換しました。",
                $"Successfully converted {successCount}/{selectedInfos.Count} materials."),
                "OK"
            );

            // Rescan
            ScanProject();
        }

        private Dictionary<string, MaterialProperty> CaptureAllProperties(Material material)
        {
            var properties = new Dictionary<string, MaterialProperty>();

            Shader shader = material.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                string propName = ShaderUtil.GetPropertyName(shader, i);
                ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(shader, i);

                var prop = new MaterialProperty
                {
                    name = propName,
                    type = propType
                };

                try
                {
                    switch (propType)
                    {
                        case ShaderUtil.ShaderPropertyType.Color:
                            prop.colorValue = material.GetColor(propName);
                            break;
                        case ShaderUtil.ShaderPropertyType.Float:
                        case ShaderUtil.ShaderPropertyType.Range:
                            prop.floatValue = material.GetFloat(propName);
                            break;
                        case ShaderUtil.ShaderPropertyType.TexEnv:
                            prop.textureValue = material.GetTexture(propName);
                            break;
                        case ShaderUtil.ShaderPropertyType.Vector:
                            prop.vectorValue = material.GetVector(propName);
                            break;
                    }

                    properties[propName] = prop;
                }
                catch
                {
                    // Property might not be set
                }
            }

            return properties;
        }

        private void RestoreCompatibleProperties(Material material, Dictionary<string, MaterialProperty> originalProps)
        {
            Shader shader = material.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                string propName = ShaderUtil.GetPropertyName(shader, i);

                if (originalProps.ContainsKey(propName))
                {
                    var originalProp = originalProps[propName];

                    try
                    {
                        switch (originalProp.type)
                        {
                            case ShaderUtil.ShaderPropertyType.Color:
                                material.SetColor(propName, originalProp.colorValue);
                                break;
                            case ShaderUtil.ShaderPropertyType.Float:
                            case ShaderUtil.ShaderPropertyType.Range:
                                material.SetFloat(propName, originalProp.floatValue);
                                break;
                            case ShaderUtil.ShaderPropertyType.TexEnv:
                                if (originalProp.textureValue != null)
                                    material.SetTexture(propName, originalProp.textureValue);
                                break;
                            case ShaderUtil.ShaderPropertyType.Vector:
                                material.SetVector(propName, originalProp.vectorValue);
                                break;
                        }
                    }
                    catch
                    {
                        // Type mismatch or other error
                    }
                }
            }
        }

        private bool IsCompactLayout()
        {
            return position.width < CompactLayoutWidth;
        }

        private float GetAdaptiveListHeight(float minHeight, float maxHeight, float ratio)
        {
            return Mathf.Clamp(position.height * ratio, minHeight, maxHeight);
        }

        private class MaterialProperty
        {
            public string name;
            public ShaderUtil.ShaderPropertyType type;
            public Color colorValue;
            public float floatValue;
            public Texture textureValue;
            public Vector4 vectorValue;
        }
    }
}
