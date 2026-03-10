using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// lilToon 邵ｺ荵晢ｽ・Natane Toon Shader 邵ｺ・ｸ邵ｺ・ｮ髢ｾ・ｪ陷肴・・ｧ・ｻ髯ｦ蠕後Ζ郢晢ｽｼ郢晢ｽｫ
    /// </summary>
    public class LilToonMigrationTool : EditorWindow
    {
        private List<Material> lilToonMaterials = new List<Material>();
        private Vector2 windowScrollPosition;
        private Vector2 scrollPosition;
        private bool createBackup = true;
        private bool replaceOriginal = false;
        private bool showPreview = false;
        private Material previewMaterial = null;
        private const float CompactLayoutWidth = 720f;
        private const float NarrowLayoutWidth = 560f;

        // 陞溽判驪､郢晢ｽ｢郢晢ｽｼ郢昴・ VisualMatch繝ｻ驛・ｽｦ荵昶螺騾ｶ・ｮ闕ｳﾂ髢ｾ・ｴ繝ｻ逶郭 MinimalSafe繝ｻ蝓滓呵氣蝓主応郢晢ｽｻ隴鯉ｽｧ陷咲ｩゑｽｽ諛ｶ・ｼ繝ｻ
        private enum ConversionMode { VisualMatch, MinimalSafe }
        private ConversionMode conversionMode = ConversionMode.VisualMatch;
        private string[] conversionModeNames => new[] {
            L("見た目優先（推奨）", "Visual Match (Recommended)"),
            L("最小安全（旧来互換）", "Minimal Safe (Legacy)")
        };

        // 郢晢ｽ｢郢晢ｽｼ郢晉甥繝ｻ隴厄ｽｿ
        private enum MigrationMode { Project, Prefab }
        private MigrationMode currentMode = MigrationMode.Project;
        private string[] modeNames => new[] { L("プロジェクト", "Project"), L("アバター/プレハブ", "Avatar/Prefab") };

        // 郢晏干ﾎ樒ｹ昜ｸ翫Ω郢晢ｽ｢郢晢ｽｼ郢晁・逡醍ｹ晁ｼ斐≦郢晢ｽｼ郢晢ｽｫ郢昴・
        private GameObject targetPrefab = null;
        private List<PrefabMaterialInfo> prefabMaterials = new List<PrefabMaterialInfo>();
        private Vector2 prefabScrollPosition;
        private bool updatePrefabReferences = true;
        private bool duplicateInHierarchy = false;
        private GameObject lastDuplicatedObject = null;
        private bool hasScannedProjectMaterials = false;
        private int projectPageIndex = 0;
        private int projectMaterialTotalCount = 0;
        private const int ProjectResultsPerPage = 100;

        // 郢晏干ﾎ樒ｹ昜ｸ翫Ω陷繝ｻ繝ｻ郢昴・ﾎ懃ｹｧ・｢郢晢ｽｫ隲繝ｻ・ｰ・ｱ
        private class PrefabMaterialInfo
        {
            public Material original;
            public Material converted;
            public Renderer renderer;
            public int materialIndex;
            public bool willConvert = true;
            public string rendererPath;
        }

        // 陞溽判驪､郢晢ｽｬ郢晄亢繝ｻ郢昴・
        private class ConversionReport
        {
            public string materialName;
            public bool success;
            public List<string> warnings = new List<string>();
            public List<string> infos = new List<string>();
            public bool hasMultipleShadowLayers;
            public bool outlineWidthAdjusted;
            public float originalOutlineWidth;
            public float convertedOutlineWidth;
        }

        [MenuItem("Tools/Natane/Migration/lilToon 移行ツール lilToon Migration Tool", false, 51)]
        public static void ShowWindow()
        {
            var window = GetWindow<LilToonMigrationTool>(L("lilToon 移行", "lilToon Migration"));
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            hasScannedProjectMaterials = false;
            projectPageIndex = 0;
            projectMaterialTotalCount = 0;
            lilToonMaterials.Clear();
        }

        private void OnGUI()
        {
            windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition);
            NataneToonShaderGUIUtility.DrawToolHeader("lilToon 移行ツール", "lilToon Migration Tool", nameof(LilToonMigrationTool));
            EditorGUILayout.Space();

            if (!hasScannedProjectMaterials)
            {
                EditorGUILayout.HelpBox(
                    L("大規模プロジェクトでも開いた直後の応答性を保つため、プロジェクト全体のマテリアル走査は手動実行です。",
                      "Project-wide material scan is manual so opening the tool stays responsive on large projects."),
                    MessageType.Info);
            }

            EditorGUILayout.HelpBox(
                L("このツールは lilToon マテリアルを Natane Toon Shader へ自動変換します。\n" +
                  "見た目優先: 有効な lilToon 機能をできるだけ見た目維持で変換します。\n" +
                  "最小安全: 基本設定のみ有効化し、他機能の値は保持したまま OFF で移行します。",
                  "This tool automatically converts lilToon materials to Natane Toon Shader.\n" +
                  "Visual Match mode: Converts and enables all active lilToon features (rim light, outline, emission, etc.).\n" +
                  "Minimal Safe mode: Only basic settings enabled. Property values are preserved for manual activation."),
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Options
            EditorGUILayout.LabelField(L("オプション", "Options"), EditorStyles.boldLabel);
            createBackup = EditorGUILayout.Toggle(L("バックアップを作成", "Create Backup"), createBackup);
            replaceOriginal = EditorGUILayout.Toggle(L("元マテリアルを置換（破壊的）", "Replace Original (Destructive)"), replaceOriginal);
            showPreview = EditorGUILayout.Toggle(L("変換後にプレビューを表示", "Show Preview After Conversion"), showPreview);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L("変換モード", "Conversion Mode"), EditorStyles.boldLabel);
            conversionMode = DrawResponsiveSelection(conversionMode, conversionModeNames);
            if (conversionMode == ConversionMode.VisualMatch)
            {
                EditorGUILayout.HelpBox(
                    L("有効な lilToon 機能（リムライト、アウトライン、エミッション、MatCap、スペキュラーなど）を変換して有効化します。\n" +
                      "変換直後の見た目が元マテリアルに近くなるようにします。",
                      "All active lilToon features (rim light, outline, emission, MatCap, specular, etc.) will be converted and enabled.\n" +
                      "The result will visually match the original material immediately after conversion."),
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    L("基本設定（テクスチャ、色、影）のみ有効化します。他機能は値を保持したまま OFF で移行します。\n" +
                      "移行後に必要な機能だけ個別に有効化できます。",
                      "Only basic settings (texture, color, shadow) are enabled. Other features are migrated in OFF state.\n" +
                      "Property values are preserved, so you can enable features individually after migration."),
                    MessageType.Info
                );
            }

            EditorGUILayout.Space(5);

            if (replaceOriginal)
            {
                EditorGUILayout.HelpBox(
                    L("警告: 元のマテリアルを直接変更します。\n" +
                      "必ずプロジェクトのバックアップを確認してください。",
                      "WARNING: This will permanently modify your original materials!\n" +
                      "Make sure you have a backup of your project."),
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space();

            // 郢晢ｽ｢郢晢ｽｼ郢晉甥繝ｻ隴厄ｽｿ郢ｧ・ｿ郢昴・
            currentMode = DrawResponsiveSelection(currentMode, modeNames);
            EditorGUILayout.Space();

            switch (currentMode)
            {
                case MigrationMode.Project:
                    DrawProjectMode();
                    break;
                case MigrationMode.Prefab:
                    DrawPrefabMode();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 陷茨ｽｨ郢晏干ﾎ溽ｹｧ・ｸ郢ｧ・ｧ郢ｧ・ｯ郢晏現ﾎ皮ｹ晢ｽｼ郢晏ｳｨ繝ｻUI隰蜀怜愛
        /// </summary>
        private void DrawProjectMode()
        {
            // Scan button
            if (GUILayout.Button(L("lilToon マテリアルをスキャン", "Scan for lilToon Materials"), GUILayout.Height(30)))
            {
                ScanForLilToonMaterials();
            }

            EditorGUILayout.Space();
            if (!hasScannedProjectMaterials)
            {
                return;
            }

            // Materials list
            EditorGUILayout.LabelField(L($"lilToon マテリアルが {lilToonMaterials.Count} 件見つかりました", $"Found {lilToonMaterials.Count} lilToon Materials"), EditorStyles.boldLabel);

            DrawProjectPageControls();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(GetAdaptiveListHeight(180f, 340f, 0.35f)));

            foreach (var material in lilToonMaterials)
            {
                if (IsCompactLayout())
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.ObjectField(material, typeof(Material), false);

                    if (GUILayout.Button(L("変換", "Convert"), GUILayout.Height(24f)))
                    {
                        var report = ConvertMaterialWithReport(material);
                        if (report.success)
                        {
                            AssetDatabase.SaveAssets();
                            ShowConversionReport(new List<ConversionReport> { report }, 1);
                            ScanForLilToonMaterials();
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(material, typeof(Material), false);

                    if (GUILayout.Button(L("変換", "Convert"), GUILayout.Width(80)))
                    {
                        var report = ConvertMaterialWithReport(material);
                        if (report.success)
                        {
                            AssetDatabase.SaveAssets();
                            ShowConversionReport(new List<ConversionReport> { report }, 1);
                            ScanForLilToonMaterials();
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Convert all button
            GUI.enabled = lilToonMaterials.Count > 0;
            if (GUILayout.Button(L("すべてのマテリアルを変換", "Convert All Materials"), GUILayout.Height(40)))
            {
                ConvertAllMaterials();
            }
            GUI.enabled = true;
        }

        private void DrawProjectPageControls()
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(projectMaterialTotalCount / (float)ProjectResultsPerPage));
            projectPageIndex = Mathf.Clamp(projectPageIndex, 0, totalPages - 1);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = projectPageIndex > 0;
            if (GUILayout.Button("<", GUILayout.Width(32)))
            {
                projectPageIndex--;
                RefreshProjectMaterialPage();
            }

            GUI.enabled = projectPageIndex < totalPages - 1;
            if (GUILayout.Button(">", GUILayout.Width(32)))
            {
                projectPageIndex++;
                RefreshProjectMaterialPage();
            }

            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{projectPageIndex + 1}/{totalPages}", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 郢ｧ・｢郢晁・縺｡郢晢ｽｼ/郢晏干ﾎ樒ｹ昜ｸ翫Ω郢晢ｽ｢郢晢ｽｼ郢晏ｳｨ繝ｻUI隰蜀怜愛
        /// </summary>
        private void DrawPrefabMode()
        {
            // 郢ｧ・ｻ郢ｧ・ｯ郢ｧ・ｷ郢晢ｽｧ郢晢ｽｳ1: 郢晏干ﾎ樒ｹ昜ｸ翫Ω鬩包ｽｸ隰壹・
            EditorGUILayout.LabelField(L("プレハブ選択", "Prefab Selection"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            targetPrefab = (GameObject)EditorGUILayout.ObjectField(
                L("対象プレハブ", "Target Prefab"),
                targetPrefab,
                typeof(GameObject),
                true // allowSceneObjects - 郢ｧ・ｷ郢晢ｽｼ郢晢ｽｳ闕ｳ鄙ｫ繝ｻ郢ｧ・､郢晢ｽｳ郢ｧ・ｹ郢ｧ・ｿ郢晢ｽｳ郢ｧ・ｹ郢ｧ繝ｻ&D陷ｿ・ｯ髢ｭ・ｽ
            );
            if (EditorGUI.EndChangeCheck() && targetPrefab != null)
            {
                ScanPrefabMaterials();
            }

            if (targetPrefab == null)
            {
                EditorGUILayout.HelpBox(
                    L("ここにプレハブまたはシーン内アバターをドラッグ&ドロップして lilToon マテリアルを走査します。", "Drag & drop a prefab or scene avatar here to scan for lilToon materials."),
                    MessageType.Info
                );
                return;
            }

            EditorGUILayout.Space();

            // 郢ｧ・ｻ郢ｧ・ｯ郢ｧ・ｷ郢晢ｽｧ郢晢ｽｳ2: 郢晏干ﾎ樒ｹ昜ｸ翫Ω髫ｪ・ｭ陞ｳ繝ｻ
            EditorGUILayout.LabelField(L("プレハブ設定", "Prefab Settings"), EditorStyles.boldLabel);

            // 髫阪・・｣・ｽ郢晢ｽ｢郢晢ｽｼ郢昜ｼ夲ｽｼ莠･繝ｻ邵ｺ・ｮ郢晏干ﾎ樒ｹ昜ｸ翫Ω郢ｧ蝣､・ｶ・ｭ隰悶・・ｼ繝ｻ
            duplicateInHierarchy = EditorGUILayout.Toggle(
                L("Hierarchy に複製を作成", "Create Duplicate in Hierarchy"),
                duplicateInHierarchy
            );
            if (duplicateInHierarchy)
            {
                EditorGUILayout.HelpBox(
                    L("Creates a duplicate in the hierarchy with converted materials, keeping the original prefab untouched.\n", "Creates a duplicate in the hierarchy with converted materials, keeping the original prefab untouched.\n" +
                    "You can compare the original and converted avatars side by side."),
                    MessageType.Info
                );
            }

            // 髫阪・・｣・ｽ郢晢ｽ｢郢晢ｽｼ郢晏ｳｨ縲堤ｸｺ・ｪ邵ｺ繝ｻ・ｰ・ｴ陷ｷ蛹ｻ繝ｻ邵ｺ・ｿ邵ｲ竏ｵ驥瑚氛蛟･繝ｻ陷ｿ繧峨・隴厄ｽｴ隴・ｽｰ/驗ゑｽｮ隰蟶吶′郢晏干縺咏ｹ晢ｽｧ郢晢ｽｳ郢ｧ螳夲ｽ｡・ｨ驕会ｽｺ
            using (new EditorGUI.DisabledScope(duplicateInHierarchy))
            {
                updatePrefabReferences = EditorGUILayout.Toggle(
                    L("参照を自動更新", "Auto-update References"),
                    updatePrefabReferences
                );
            }

            if (duplicateInHierarchy)
            {
                // 髫阪・・｣・ｽ郢晢ｽ｢郢晢ｽｼ郢晏ｳｨ繝ｻ髫ｱ・ｬ隴剰ｶ｣・ｼ莠包ｽｻ謔ｶ繝ｻ郢ｧ・ｪ郢晏干縺咏ｹ晢ｽｧ郢晢ｽｳ邵ｺ・ｯ霎滂ｽ｡鬮｢・｢闖ｫ繧托ｽｼ繝ｻ
            }
            else if (updatePrefabReferences && !replaceOriginal)
            {
                EditorGUILayout.HelpBox(
                    L("変換後、プレハブ内の Renderer 参照は新しいマテリアルへ自動更新されます。", "After conversion, Renderer references in the prefab will be automatically updated to new materials."),
                    MessageType.Info
                );
            }
            else if (replaceOriginal)
            {
                EditorGUILayout.HelpBox(
                    L("「元マテリアルを置換」モードでは、元マテリアルをその場で変更するため参照更新は不要です。", "In 'Replace Original' mode, the original material is modified in-place, so reference updates are unnecessary."),
                    MessageType.Info
                );
            }

            EditorGUILayout.Space();

            // 郢ｧ・ｹ郢ｧ・ｭ郢晢ｽ｣郢晢ｽｳ郢晄㈱縺｡郢晢ｽｳ
            if (GUILayout.Button(L("マテリアルを再スキャン", "Rescan Materials"), GUILayout.Height(25)))
            {
                ScanPrefabMaterials();
            }

            EditorGUILayout.Space();

            // 郢ｧ・ｻ郢ｧ・ｯ郢ｧ・ｷ郢晢ｽｧ郢晢ｽｳ3: 郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ闕ｳﾂ髫包ｽｧ
            if (prefabMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    L("このプレハブでは lilToon マテリアルが見つかりませんでした。", "No lilToon materials found in this prefab."),
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.LabelField(
                L($"lilToon マテリアルが {prefabMaterials.Select(m => m.original).Distinct().Count()} 件見つかりました", $"Found {prefabMaterials.Select(m => m.original).Distinct().Count()} lilToon Materials"),
                EditorStyles.boldLabel
            );

            prefabScrollPosition = EditorGUILayout.BeginScrollView(prefabScrollPosition, GUILayout.Height(GetAdaptiveListHeight(220f, 400f, 0.38f)));

            // 陷ｷ蠕｡・ｸﾂ郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ邵ｺ・ｧ郢ｧ・ｰ郢晢ｽｫ郢晢ｽｼ郢晄懷密邵ｺ蜉ｱ窶ｻ髯ｦ・ｨ驕会ｽｺ
            var grouped = prefabMaterials.GroupBy(m => m.original);
            foreach (var group in grouped)
            {
                Material mat = group.Key;
                var entries = group.ToList();
                bool willConvert = entries[0].willConvert;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ髯ｦ繝ｻ
                bool newWillConvert;
                if (IsCompactLayout())
                {
                    EditorGUILayout.BeginHorizontal();
                    newWillConvert = EditorGUILayout.Toggle(willConvert, GUILayout.Width(20));
                    EditorGUILayout.ObjectField(mat, typeof(Material), false);
                    EditorGUILayout.EndHorizontal();

                    if (entries[0].converted != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("->", GUILayout.Width(20));
                        EditorGUILayout.ObjectField(entries[0].converted, typeof(Material), false);
                        EditorGUILayout.EndHorizontal();
                    }

                    if (GUILayout.Button(L("変換", "Convert"), GUILayout.Height(24f)))
                    {
                        ConvertSinglePrefabMaterial(mat);
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    newWillConvert = EditorGUILayout.Toggle(willConvert, GUILayout.Width(20));
                    EditorGUILayout.ObjectField(mat, typeof(Material), false);

                    if (entries[0].converted != null)
                    {
                        EditorGUILayout.LabelField("->", GUILayout.Width(20));
                        EditorGUILayout.ObjectField(entries[0].converted, typeof(Material), false);
                    }

                    if (GUILayout.Button(L("変換", "Convert"), GUILayout.Width(100)))
                    {
                        ConvertSinglePrefabMaterial(mat);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (newWillConvert != willConvert)
                {
                    foreach (var entry in entries)
                    {
                        entry.willConvert = newWillConvert;
                    }
                }

                // 闖ｴ・ｿ騾包ｽｨ驍ゅ・蝨堤ｹｧ螳夲ｽ｡・ｨ驕会ｽｺ
                EditorGUI.indentLevel++;
                foreach (var entry in entries)
                {
                    EditorGUILayout.LabelField(
                        L($"Used by: {entry.rendererPath} [Slot {entry.materialIndex}]", $"Used by: {entry.rendererPath} [Slot {entry.materialIndex}]"),
                        EditorStyles.miniLabel
                    );
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 郢ｧ・ｻ郢ｧ・ｯ郢ｧ・ｷ郢晢ｽｧ郢晢ｽｳ4: 陞ｳ貅ｯ・｡蠕後・郢ｧ・ｿ郢晢ｽｳ
            int convertCount = prefabMaterials.Where(m => m.willConvert && m.converted == null)
                                              .Select(m => m.original).Distinct().Count();

            if (convertCount > 0)
            {
                EditorGUILayout.LabelField(
                    L($"Target: {convertCount} materials", $"Target: {convertCount} materials"),
                    EditorStyles.boldLabel
                );
            }

            GUI.enabled = convertCount > 0;
            string buttonLabel = duplicateInHierarchy
                ? L($"Duplicate & Convert ({convertCount})", $"Duplicate & Convert ({convertCount})")
                : L($"Convert Selected ({convertCount})", $"Convert Selected ({convertCount})");
            if (GUILayout.Button(buttonLabel, GUILayout.Height(40)))
            {
                ConvertPrefabMaterials();
            }
            GUI.enabled = true;

            // 陷第ｦ雁ｱ鍋ｸｺ・ｮ髫阪・・｣・ｽ郢ｧ・ｪ郢晄じ縺夂ｹｧ・ｧ郢ｧ・ｯ郢晏現竏育ｸｺ・ｮ陷ｿ繧峨・
            if (lastDuplicatedObject != null)
            {
                EditorGUILayout.Space(5);
                if (IsCompactLayout())
                {
                    EditorGUILayout.LabelField(L("最後に作成した複製", "Last Duplicate"), EditorStyles.boldLabel);
                    EditorGUILayout.ObjectField(lastDuplicatedObject, typeof(GameObject), true);
                    if (GUILayout.Button(L("選択", "Select"), GUILayout.Height(24f)))
                    {
                        Selection.activeGameObject = lastDuplicatedObject;
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(L("最後の複製:", "Last Duplicate:"), GUILayout.Width(100));
                    EditorGUILayout.ObjectField(lastDuplicatedObject, typeof(GameObject), true);
                    if (GUILayout.Button(L("選択", "Select"), GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = lastDuplicatedObject;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private bool IsCompactLayout()
        {
            return position.width < CompactLayoutWidth;
        }

        private TEnum DrawResponsiveSelection<TEnum>(TEnum currentValue, string[] labels) where TEnum : System.Enum
        {
            int selectedIndex = System.Convert.ToInt32(currentValue);
            if (position.width < NarrowLayoutWidth)
            {
                selectedIndex = GUILayout.SelectionGrid(selectedIndex, labels, 1, EditorStyles.miniButton);
            }
            else if (IsCompactLayout())
            {
                selectedIndex = GUILayout.SelectionGrid(selectedIndex, labels, labels.Length, EditorStyles.miniButton);
            }
            else
            {
                selectedIndex = GUILayout.Toolbar(selectedIndex, labels);
            }

            return (TEnum)System.Enum.ToObject(typeof(TEnum), selectedIndex);
        }

        private float GetAdaptiveListHeight(float minHeight, float maxHeight, float ratio)
        {
            return Mathf.Clamp(position.height * ratio, minHeight, maxHeight);
        }

        private void ScanForLilToonMaterials()
        {
            lilToonMaterials.Clear();
            hasScannedProjectMaterials = true;
            projectPageIndex = 0;
            RefreshProjectMaterialPage();
            Debug.Log($"Found {projectMaterialTotalCount} lilToon materials via asset index.");
        }

        private void RefreshProjectMaterialPage()
        {
            lilToonMaterials.Clear();

            List<MaterialIndexEntry> pageEntries = NataneAssetIndexService.GetMaterialEntriesPage(
                entry => entry.isLilToonShader,
                projectPageIndex,
                ProjectResultsPerPage,
                out int totalCount);

            projectMaterialTotalCount = totalCount;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(projectMaterialTotalCount / (float)ProjectResultsPerPage));
            projectPageIndex = Mathf.Clamp(projectPageIndex, 0, totalPages - 1);

            if (projectMaterialTotalCount > 0 && pageEntries.Count == 0)
            {
                pageEntries = NataneAssetIndexService.GetMaterialEntriesPage(
                    entry => entry.isLilToonShader,
                    projectPageIndex,
                    ProjectResultsPerPage,
                    out totalCount);
                projectMaterialTotalCount = totalCount;
            }

            for (int i = 0; i < pageEntries.Count; i++)
            {
                Material material = NataneAssetIndexService.LoadMaterial(pageEntries[i]);
                if (material != null)
                {
                    lilToonMaterials.Add(material);
                }
            }
        }

        private void ConvertAllMaterials()
        {
            List<MaterialIndexEntry> allEntries = NataneAssetIndexService
                .EnumerateMaterialEntries(entry => entry.isLilToonShader)
                .OrderBy(entry => entry.name)
                .ThenBy(entry => entry.path)
                .ToList();

            if (!EditorUtility.DisplayDialog(
                L("????????????", "Convert All Materials"),
                L($"{lilToonMaterials.Count} ???????????????", $"Are you sure you want to convert {lilToonMaterials.Count} materials?"),
                L("はい", "Yes"), L("キャンセル", "Cancel")))
            {
                return;
            }

            int successCount = 0;
            List<ConversionReport> reports = new List<ConversionReport>();

            try
            {
                for (int i = 0; i < allEntries.Count; i++)
                {
                    Material material = NataneAssetIndexService.LoadMaterial(allEntries[i]);
                    if (material == null)
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        L("マテリアル変換中", "Converting Materials"),
                        L($"Converting {i + 1}/{lilToonMaterials.Count}: {lilToonMaterials[i].name}", $"Converting {i + 1}/{lilToonMaterials.Count}: {lilToonMaterials[i].name}"),
                        (float)i / lilToonMaterials.Count
                    );

                    var report = ConvertMaterialWithReport(lilToonMaterials[i]);
                    reports.Add(report);
                    if (report.success)
                    {
                        successCount++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // 陞溽判驪､郢晢ｽｬ郢晄亢繝ｻ郢晏現・帝勗・ｨ驕会ｽｺ
            AssetDatabase.SaveAssets();
            ShowConversionReport(reports, successCount);

            // Rescan
            ScanForLilToonMaterials();
        }

        private bool ConvertMaterial(Material sourceMaterial)
        {
            var report = ConvertMaterialWithReport(sourceMaterial);
            return report.success;
        }

        private ConversionReport ConvertMaterialWithReport(Material sourceMaterial)
        {
            var report = new ConversionReport();
            report.materialName = sourceMaterial.name;

            try
            {
                // Create backup if requested
                if (createBackup)
                {
                    string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                    string backupPath = sourcePath.Replace(".mat", "_lilToon_backup.mat");
                    AssetDatabase.CopyAsset(sourcePath, backupPath);
                    Debug.Log($"Created backup: {backupPath}");
                    report.infos.Add($"Created backup material: {backupPath}");
                }

                Material targetMaterial;

                if (replaceOriginal)
                {
                    targetMaterial = sourceMaterial;
                }
                else
                {
                    // Create new material
                    targetMaterial = new Material(sourceMaterial);
                    string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                    string newPath = sourcePath.Replace(".mat", "_NataneToon.mat");
                    AssetDatabase.CreateAsset(targetMaterial, newPath);
                    report.infos.Add($"Created converted material: {newPath}");
                }

                // Store original properties before changing shader
                var originalProperties = CaptureProperties(sourceMaterial);

                // 髫阪・辟夂ｹｧ・ｷ郢晢ｽ｣郢晏ｳｨ縺育ｹ晢ｽｬ郢ｧ・､郢晢ｽ､郢晢ｽｼ邵ｺ・ｮ隶諛ｷ繝ｻ
                DetectMultipleShadowLayers(originalProperties, report);

                // Find Natane Toon Shader (郢晁・ﾎ懃ｹｧ・｢郢晢ｽｳ郢晞メ繝ｻ陷榊｢難ｽ､諛ｷ繝ｻ)
                Shader nataneToonShader = DetectNataneShaderVariant(sourceMaterial);
                if (nataneToonShader == null)
                {
                    Debug.LogError("Natane Toon Shader not found! Please make sure it's in your project.");
                    report.success = false;
                    report.warnings.Add("Natane Toon Shader was not found in the project.");
                    return report;
                }
                report.infos.Add($"Using shader: {nataneToonShader.name}");

                // Change shader
                targetMaterial.shader = nataneToonShader;

                // Map properties
                MapPropertiesWithReport(originalProperties, targetMaterial, report);

                // ConversionMode邵ｺ・ｫ陟｢諛環ｧ邵ｺ・ｦ隶匁ｺｯ繝ｻ邵ｺ・ｮ隴帷甥譟題峪繝ｻ霎滂ｽ｡陷会ｽｹ陋ｹ謔ｶ・定崕・ｶ陟包ｽ｡
                if (conversionMode == ConversionMode.MinimalSafe)
                {
                    // 隴鯉ｽｧ陷咲ｩゑｽｽ繝ｻ 陜難ｽｺ隴幢ｽｬ郢ｧ・ｿ郢晉ｴ具ｽｻ・･陞滓じ繝ｻ隶匁ｺｯ繝ｻ郢ｧ蛛ｵ笘・ｸｺ・ｹ邵ｺ・ｦ郢ｧ・ｪ郢晁ｼ披・邵ｺ蜷ｶ・・
                    DisableNonBasicFeatures(targetMaterial, report);
                }
                else
                {
                    // VisualMatch郢晢ｽ｢郢晢ｽｼ郢昴・ MapPropertiesWithReport邵ｺ・ｧ隴帷甥譟題峪謔ｶ・邵ｺ貊難ｽｩ貅ｯ繝ｻ郢ｧ蛛ｵ笳守ｸｺ・ｮ邵ｺ・ｾ邵ｺ・ｾ闖ｫ譎・亜
                    report.infos.Add("Visual Match mode keeps migrated features enabled when possible.");
                }

                // 郢晏干ﾎ樒ｹ晁侭ﾎ礼ｹ晢ｽｼ隶匁ｺｯ繝ｻ
                if (showPreview)
                {
                    previewMaterial = targetMaterial;
                    Selection.activeObject = targetMaterial;
                }

                EditorUtility.SetDirty(targetMaterial);

                Debug.Log($"Successfully converted: {sourceMaterial.name}");
                report.success = true;
                return report;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to convert {sourceMaterial.name}: {e.Message}");
                report.success = false;
                report.warnings.Add($"Conversion failed: {e.Message}");
                return report;
            }
        }

        private void DetectMultipleShadowLayers(Dictionary<string, object> properties, ConversionReport report)
        {
            // lilToon邵ｺ・ｮ2nd, 3rd shadow郢晢ｽｬ郢ｧ・､郢晢ｽ､郢晢ｽｼ郢ｧ蜻茨ｽ､諛ｷ繝ｻ
            bool has2ndShadow = properties.ContainsKey("_Shadow2ndColor");
            bool has3rdShadow = properties.ContainsKey("_Shadow3rdColor");

            if (has2ndShadow || has3rdShadow)
            {
                report.hasMultipleShadowLayers = true;
                string layerInfo = has3rdShadow ? "3 layers" : "2 layers";
                report.infos.Add($"Detected {layerInfo} and converted them as multi-shadow.");
            }
        }

        private Dictionary<string, object> CaptureProperties(Material material)
        {
            var properties = new Dictionary<string, object>();

            // =============================================
            // lilToon 邵ｺ・ｮ陞ｳ貊・怙邵ｺ・ｮ郢晏干ﾎ溽ｹ昜ｻ｣繝ｦ郢ｧ・｣陷ｷ髦ｪ竊楢搏・ｺ邵ｺ・･邵ｺ荳翫￥郢晢ｽ｣郢晏干繝｡郢晢ｽ｣
            // (GitHub: lilxyzw/lilToon lts.shader 郢ｧ蛹ｻ・・
            // =============================================

            // === Core ===
            CaptureTexture(material, "_MainTex", properties);
            CaptureColor(material, "_Color", properties);
            CaptureFloat(material, "_Cutoff", properties);

            // === Normal Map ===
            CaptureTexture(material, "_BumpMap", properties);
            CaptureFloat(material, "_BumpScale", properties);

            // === lilToon Feature Toggles (鬩･蟠趣ｽｦ繝ｻ・ｼ繝ｻ ===
            CaptureFloat(material, "_UseShadow", properties);
            CaptureFloat(material, "_UseRim", properties);
            CaptureFloat(material, "_UseRimShade", properties);
            CaptureFloat(material, "_UseMatCap", properties);
            CaptureFloat(material, "_UseMatCap2nd", properties);
            CaptureFloat(material, "_UseEmission", properties);
            CaptureFloat(material, "_UseEmission2nd", properties);
            CaptureFloat(material, "_UseOutline", properties);

            // === Shadow (lilToon邵ｺ・ｮ陞ｳ貊・怙邵ｺ・ｮ郢晏干ﾎ溽ｹ昜ｻ｣繝ｦ郢ｧ・｣陷ｷ繝ｻ _ShadowBorder, _ShadowBlur) ===
            CaptureColor(material, "_ShadowColor", properties);
            CaptureFloat(material, "_ShadowBorder", properties);
            CaptureFloat(material, "_ShadowBlur", properties);
            CaptureFloat(material, "_ShadowStrength", properties);
            CaptureFloat(material, "_ShadowNormalStrength", properties);

            // Shadow 2nd/3rd
            CaptureColor(material, "_Shadow2ndColor", properties);
            CaptureFloat(material, "_Shadow2ndBorder", properties);
            CaptureFloat(material, "_Shadow2ndBlur", properties);
            CaptureColor(material, "_Shadow3rdColor", properties);
            CaptureFloat(material, "_Shadow3rdBorder", properties);
            CaptureFloat(material, "_Shadow3rdBlur", properties);

            // Shadow textures
            CaptureTexture(material, "_ShadowColorTex", properties);
            CaptureTexture(material, "_ShadowStrengthMask", properties);
            CaptureTexture(material, "_ShadowBorderMask", properties);
            CaptureTexture(material, "_ShadowBlurMask", properties);

            // Shadow Environment Strength (lilToon陜暦ｽｺ隴帙・ 鬮｢謐ｺ逎∬怦蟲ｨ竊鍋ｹｧ蛹ｻ・玖厄ｽｱ隰問・笆闕ｳ鄙ｫ・｡)
            CaptureFloat(material, "_ShadowEnvStrength", properties);
            // Shadow Receive (lilToon: 郢晢ｽｪ郢ｧ・｢郢晢ｽｫ郢ｧ・ｿ郢ｧ・､郢晢｣ｰ郢ｧ・ｷ郢晢ｽ｣郢晏ｳｨ縺郁愾蜉ｱ・陷ｿ謔ｶ・企ｩ･繝ｻ
            CaptureFloat(material, "_ShadowReceive", properties);

            // === Rim Light ===
            CaptureColor(material, "_RimColor", properties);
            CaptureTexture(material, "_RimColorTex", properties);
            CaptureFloat(material, "_RimBorder", properties);
            CaptureFloat(material, "_RimBlur", properties);
            CaptureFloat(material, "_RimFresnelPower", properties);
            CaptureFloat(material, "_RimEnableLighting", properties);
            CaptureFloat(material, "_RimShadowMask", properties);

            // === Outline ===
            CaptureColor(material, "_OutlineColor", properties);
            CaptureFloat(material, "_OutlineWidth", properties);
            CaptureFloat(material, "_OutlineFixWidth", properties);
            CaptureTexture(material, "_OutlineTex", properties);

            // === Emission ===
            CaptureTexture(material, "_EmissionMap", properties);
            CaptureColor(material, "_EmissionColor", properties);
            CaptureTexture(material, "_Emission2ndMap", properties);
            CaptureColor(material, "_Emission2ndColor", properties);

            // === MatCap ===
            CaptureTexture(material, "_MatCapTex", properties);
            CaptureTexture(material, "_MatCapBlendMask", properties);
            CaptureColor(material, "_MatCapColor", properties);
            CaptureFloat(material, "_MatCapBlend", properties);
            CaptureFloat(material, "_MatCapBlendMode", properties);
            // MatCap 2nd
            CaptureTexture(material, "_MatCap2ndTex", properties);
            CaptureTexture(material, "_MatCap2ndBlendMask", properties);
            CaptureColor(material, "_MatCap2ndColor", properties);
            CaptureFloat(material, "_MatCap2ndBlend", properties);
            CaptureFloat(material, "_MatCap2ndBlendMode", properties);

            // === Specular / Surface ===
            CaptureFloat(material, "_Smoothness", properties);
            CaptureFloat(material, "_Metallic", properties);
            CaptureFloat(material, "_SpecularToon", properties);
            CaptureFloat(material, "_SpecularBorder", properties);
            CaptureFloat(material, "_SpecularBlur", properties);

            // === Light Color Limits (lilToon陜暦ｽｺ隴帙・ ===
            CaptureFloat(material, "_LightMinLimit", properties);
            CaptureFloat(material, "_LightMaxLimit", properties);
            CaptureFloat(material, "_MonochromeLighting", properties);
            CaptureFloat(material, "_AsUnlit", properties);

            return properties;
        }

        private void CaptureTexture(Material material, string propertyName, Dictionary<string, object> properties)
        {
            if (material.HasProperty(propertyName))
            {
                properties[propertyName] = material.GetTexture(propertyName);
            }
        }

        private void CaptureColor(Material material, string propertyName, Dictionary<string, object> properties)
        {
            if (material.HasProperty(propertyName))
            {
                properties[propertyName] = material.GetColor(propertyName);
            }
        }

        private void CaptureFloat(Material material, string propertyName, Dictionary<string, object> properties)
        {
            if (material.HasProperty(propertyName))
            {
                properties[propertyName] = material.GetFloat(propertyName);
            }
        }

        private void MapProperties(Dictionary<string, object> sourceProps, Material targetMaterial)
        {
            var dummyReport = new ConversionReport();
            MapPropertiesWithReport(sourceProps, targetMaterial, dummyReport);
        }

        private void MapPropertiesWithReport(Dictionary<string, object> sourceProps, Material targetMaterial, ConversionReport report)
        {
            // =============================================
            // lilToon 遶翫・Natane Toon Shader 郢晏干ﾎ溽ｹ昜ｻ｣繝ｦ郢ｧ・｣郢晄ｧｭ繝｣郢晄鱒ﾎｦ郢ｧ・ｰ
            // lilToon邵ｺ・ｮ陞ｳ貊・怙邵ｺ・ｮ郢晏干ﾎ溽ｹ昜ｻ｣繝ｦ郢ｧ・｣陷ｷ髦ｪ竊楢搏・ｺ邵ｺ・･邵ｺ繝ｻ(lts.shader)
            // =============================================

            // === Main Texture & Color ===
            SetTextureIfExists(sourceProps, "_MainTex", targetMaterial, "_MainTex");
            SetColorIfExists(sourceProps, "_Color", targetMaterial, "_Color");

            // === Shadow ===
            bool useShadow = GetFloatOr(sourceProps, "_UseShadow", 0) > 0.5f;

            if (sourceProps.ContainsKey("_ShadowColor"))
            {
                Color shadowColor = (Color)sourceProps["_ShadowColor"];

                // ShadowStrength 遶翫・ShadowColor邵ｺ・ｫ鬩包ｽｩ騾包ｽｨ
                float shadowStrength = GetFloatOr(sourceProps, "_ShadowStrength", 1.0f);
                if (shadowStrength < 0.99f)
                {
                    shadowColor = Color.Lerp(Color.white, shadowColor, shadowStrength);
                    report.infos.Add($"ShadowStrength={shadowStrength:F2}: shadow color was blended toward white.");
                }

                targetMaterial.SetColor("_ShadowColor", shadowColor);

                // 郢ｧ・ｷ郢晢ｽ｣郢晏ｳｨ縺育ｹｧ・ｫ郢晢ｽｩ郢晢ｽｼ郢晢ｽ｢郢昴・ﾎ晉ｸｺ・ｮ陝ｾ・ｮ騾｡・ｰ郢ｧ蛛ｵﾎ樒ｹ晄亢繝ｻ郢晏現竊馴坎蛟ｩ・ｼ繝ｻ
                // lilToon: indirectCol = albedo * _ShadowColor (闕ｵ遉ｼ・ｮ邇ｲ蟀ｿ陟代・
                //   遶翫・ShadowColor邵ｺ・ｯ陷ｷ繝ｻ繝｡郢晢ｽ｣郢晞亂ﾎ晉ｸｺ・ｮ雋ょｹ・ｽ｡・ｰ驍・・竊堤ｸｺ蜉ｱ窶ｻ隶匁ｺｯ繝ｻ
                // Natane: directResult = lerp(_ShadowColor, (1,1,1), shadingValue)
                //   遶翫・ShadowColor髢ｾ・ｪ闖ｴ阮吮ｲ陟厄ｽｱ邵ｺ・ｮ豼ｶ・ｲ繝ｻ繝ｻlbedo邵ｺ・ｨ邵ｺ・ｯ陟募ｾ後定嵯遉ｼ・ｮ證ｦ・ｼ繝ｻ
                // 邵ｺ阮吶・隴・ｽｹ陟台ｸ翫・鬩戊ｼ費ｼ樒ｸｺ・ｫ郢ｧ蛹ｻ・顔ｸｲ竏ｫ髻ｳ邵ｺ・ｫ郢ｧ・｢郢晢ｽｫ郢晏生繝ｩ邵ｺ謔滂ｽｽ・ｩ陟趣ｽｦ邵ｺ・ｮ鬯ｮ蛟･・樊ｿｶ・ｲ邵ｺ・ｮ陜｣・ｴ陷ｷ蛹ｻ竊楢厄ｽｱ豼ｶ・ｲ邵ｺ讙守・邵ｺ・ｪ郢ｧ蜿･・ｰ・ｴ陷ｷ蛹ｻ窶ｲ邵ｺ繧・ｽ・
                if (shadowColor.r < 0.95f || shadowColor.g < 0.95f || shadowColor.b < 0.95f)
                {
                    report.infos.Add(
                        $"Shadow Color: ({shadowColor.r:F2},{shadowColor.g:F2},{shadowColor.b:F2})" +
                        " Mapping may differ slightly because lilToon multiplies indirect color while Natane uses lerp-based shading.");
                }
            }

            // =============================================
            // StandardToon v2 郢晢ｽ｢郢晢ｽｼ郢晁歓繝ｻ陷肴坩竏郁ｬ壹・(lilToon陞ｳ謔溘・闔蜻磯共郢昜ｻ｣縺・ｹ晏干ﾎ帷ｹｧ・､郢晢ｽｳ)
            //
            // lilToon邵ｺ・ｮ髫ｪ閧ｲ・ｮ蜉ｱ繝ｱ郢ｧ・､郢晏干ﾎ帷ｹｧ・､郢晢ｽｳ郢ｧ雋橸ｽｮ謔溘・邵ｺ・ｫ陷蜥ｲ讓溽ｸｺ蜷ｶ・鬼tandardToon v2郢晢ｽ｢郢晢ｽｼ郢晏ｳｨ・定抄・ｿ騾包ｽｨ邵ｲ繝ｻ
            // 郢晢ｽｻlightColor = MAINLIGHT + SHToon繝ｻ繝ｻH騾ｶ・ｴ隰暦ｽ･陷ｷ閧ｲ・ｮ證ｦ・ｼ繝ｻ
            // 郢晢ｽｻlerp(indirectCol, directCol, toon) 闕ｳﾂ騾具ｽｺ陷ｷ蝓溘・
            // 郢晢ｽｻAsUnlit 郢ｧ繝ｻlightColor 邵ｺ・ｫ騾ｶ・ｴ隰暦ｽ･鬩包ｽｩ騾包ｽｨ
            // 郢晢ｽｻShadowEnvStrength 邵ｺ・ｧ鬮｢謐ｺ逎∬怦蟲ｨ竊鍋ｹｧ蛹ｻ・玖厄ｽｱ隰問・笆闕ｳ鄙ｫ・｡
            // 郢晢ｽｻmin(indirectCol, directCol) 陞ｳ迚吶・郢ｧ・ｯ郢晢ｽｩ郢晢ｽｳ郢昴・
            // 郢昜ｻ｣ﾎ帷ｹ晢ｽ｡郢晢ｽｼ郢ｧ・ｿ驕ｨ・ｺ鬮｢阮吶・陞溽判驪､邵ｺ蠕｡・ｸ蟠趣ｽｦ竏堋・ｭilToon邵ｺ・ｮ郢昜ｻ｣ﾎ帷ｹ晢ｽ｡郢晢ｽｼ郢ｧ・ｿ郢ｧ蛛ｵ笳守ｸｺ・ｮ邵ｺ・ｾ邵ｺ・ｾ騾ｶ・ｴ隰暦ｽ･郢晄ｧｭ繝｣郢晄鱒ﾎｦ郢ｧ・ｰ邵ｺ蜷ｶ・狗ｸｲ繝ｻ
            // =============================================
            targetMaterial.SetFloat("_ShadingMode", 2.0f); // StandardToon
            targetMaterial.EnableKeyword("_STANDARD_TOON");

            // lilToon 郢昜ｻ｣ﾎ帷ｹ晢ｽ｡郢晢ｽｼ郢ｧ・ｿ郢ｧ蝣､蟲ｩ隰暦ｽ･郢晄ｧｭ繝｣郢晄鱒ﾎｦ郢ｧ・ｰ繝ｻ閧ｲ・ｩ・ｺ鬮｢轣假ｽ､逕ｻ驪､闕ｳ蟠趣ｽｦ繝ｻ・ｼ繝ｻ
            {
                float border = GetFloatOr(sourceProps, "_ShadowBorder", 0.5f);
                float blur = GetFloatOr(sourceProps, "_ShadowBlur", 0.1f);
                float strength = GetFloatOr(sourceProps, "_ShadowStrength", 1.0f);

                targetMaterial.SetFloat("_STShadowBorder", border);
                targetMaterial.SetFloat("_STShadowBlur", blur);
                targetMaterial.SetFloat("_STShadowStrength", strength);

                // ShadowOffset=0: StandardToon邵ｺ・ｧ邵ｺ・ｯHalf-Lambert陷繝ｻ魑ｩ邵ｺ・ｮ邵ｺ貅假ｽ∬叉蟠趣ｽｦ繝ｻ
                targetMaterial.SetFloat("_ShadowOffset", 0);

                report.infos.Add($"StandardToon shadow mapped: Border={border:F2}, Blur={blur:F2}, Strength={strength:F2}");
            }

            // 郢晄ｧｭﾎ晉ｹ昶・縺咏ｹ晢ｽ｣郢晏ｳｨ縺育ｹ晢ｽｬ郢ｧ・､郢晢ｽ､郢晢ｽｼ陞溽判驪､繝ｻ繝ｻUseShadow邵ｺ譴ｧ諤剰怏・ｹ邵ｺ・ｪ陜｣・ｴ陷ｷ蛹ｻ繝ｻ邵ｺ・ｿ繝ｻ繝ｻ
            // lilToon邵ｺ・ｧ邵ｺ・ｯ郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ邵ｺ・ｫ郢昴・繝ｵ郢ｧ・ｩ郢晢ｽｫ郢晏現縲胆Shadow2ndColor邵ｺ謔滂ｽｭ莨懈Β邵ｺ蜷ｶ・狗ｸｺ蠕個繝ｻ
            // _UseShadow=0邵ｺ・ｮ陜｣・ｴ陷ｷ蛹ｻ繝ｻ闖ｴ・ｿ騾包ｽｨ邵ｺ霈費ｽ檎ｸｺ・ｦ邵ｺ繝ｻ竊醍ｸｺ繝ｻ
            if (useShadow)
            {
                MapMultiShadowLayers(sourceProps, targetMaterial, report);
            }

            // === Shadow Color Texture ===
            // lilToon: _ShadowColorTex 遶翫・郢ｧ・ｫ郢ｧ・ｹ郢ｧ・ｿ郢晢｣ｰ陟厄ｽｱ豼ｶ・ｲ郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ・
            // Natane: _ShadowColorTex + _ShadowColorTexStrength
            if (sourceProps.ContainsKey("_ShadowColorTex") && sourceProps["_ShadowColorTex"] != null)
            {
                targetMaterial.SetTexture("_ShadowColorTex", (Texture)sourceProps["_ShadowColorTex"]);
                targetMaterial.SetFloat("_ShadowColorTexStrength", 1.0f);
                report.infos.Add("Shadow Color Texture mapped.");
            }

            // === Shadow Normal Strength 遶翫・BumpScale 髫ｱ・ｿ隰ｨ・ｴ ===
            // lilToon: _ShadowNormalStrength (0-1) 遶翫・雎墓・・ｷ螢ｹ繝ｻ郢昴・繝ｻ邵ｺ蠕後☆郢晢ｽ｣郢晏ｳｨ縺育ｸｺ・ｫ闕ｳ蠑ｱ竏ｴ郢ｧ蜿･・ｽ・ｱ鬮ｻ・ｿ陟趣ｽｦ
            // Natane邵ｺ・ｧ邵ｺ・ｯ _BumpScale 邵ｺ・ｧ雎墓・・ｷ螢ｼ・ｼ・ｷ陟趣ｽｦ郢ｧ蝣､蟲ｩ隰暦ｽ･陋ｻ・ｶ陟包ｽ｡邵ｺ蜷ｶ・・
            // _ShadowNormalStrength < 1.0 邵ｺ・ｮ陜｣・ｴ陷ｷ蛹ｻﾂ竏晢ｽｽ・ｱ邵ｺ・ｸ邵ｺ・ｮ郢晏ｼｱ繝ｻ郢晄ｧｭﾎ晉ｸｺ・ｮ陟厄ｽｱ鬮ｻ・ｿ郢ｧ雋橸ｽｼ・ｱ郢ｧ竏壺螺邵ｺ繝ｻﾑ崎摎・ｳ
            // 遶翫・_BumpScale 郢ｧ繝ｻShadowNormalStrength 邵ｺ・ｧ髫ｱ・ｿ隰ｨ・ｴ繝ｻ莠･繝ｻ邵ｺ・ｮBumpScale邵ｺ・ｨ邵ｺ・ｮ闕ｵ遉ｼ・ｮ證ｦ・ｼ繝ｻ
            float shadowNormalStrength = GetFloatOr(sourceProps, "_ShadowNormalStrength", 1.0f);
            if (shadowNormalStrength < 0.99f)
            {
                float currentBumpScale = GetFloatOr(sourceProps, "_BumpScale", 1.0f);
                float adjustedBumpScale = currentBumpScale * shadowNormalStrength;
                targetMaterial.SetFloat("_BumpScale", adjustedBumpScale);
                report.infos.Add($"Shadow Normal: Strength={shadowNormalStrength:F2}, BumpScale {currentBumpScale:F2} -> {adjustedBumpScale:F2}");
            }

            // === Normal Map ===
            SetTextureIfExists(sourceProps, "_BumpMap", targetMaterial, "_BumpMap");
            SetFloatIfExists(sourceProps, "_BumpScale", targetMaterial, "_BumpScale");

            if (sourceProps.ContainsKey("_BumpMap") && sourceProps["_BumpMap"] != null)
            {
                targetMaterial.SetFloat("_UseNormalMap", 1.0f);
                targetMaterial.EnableKeyword("_NORMALMAP");
            }

            // === Rim Light ===
            // lilToon: _UseRim=1 邵ｺ・ｧ隴帷甥譟題峪繝ｻ
            bool useRim = GetFloatOr(sourceProps, "_UseRim", 0) > 0.5f;

            if (useRim)
            {
                targetMaterial.SetFloat("_RimLight", 1.0f);
                targetMaterial.EnableKeyword("_RIM_LIGHT");

                // RimColor
                if (sourceProps.ContainsKey("_RimColor"))
                {
                    targetMaterial.SetColor("_RimColor", (Color)sourceProps["_RimColor"]);
                }

                // RimFresnelPower 遶翫・RimPower
                float rimPower = GetFloatOr(sourceProps, "_RimFresnelPower", 3.5f);
                targetMaterial.SetFloat("_RimPower", Mathf.Clamp(rimPower, 0.1f, 10f));
                report.infos.Add($"Rim: FresnelPower={rimPower:F2} -> RimPower={Mathf.Clamp(rimPower, 0.1f, 10f):F2}");

                // RimIntensity: lilToon邵ｺ・ｧ邵ｺ・ｯ豼ｶ・ｲ邵ｺ・ｮ郢ｧ・｢郢晢ｽｫ郢晁ｼ斐＜邵ｺ・ｨ陟托ｽｷ陟趣ｽｦ邵ｺ・ｧ陋ｻ・ｶ陟包ｽ｡
                targetMaterial.SetFloat("_RimIntensity", 1.0f);

                // RimBorder 遶翫・RimSpread (鬨ｾ繝ｻ蠍碁ｫ｢・｢)
                // lilToon: border=0.5遶雁・ﾎ懃ｹ晢｣ｰ闕ｳ・ｭ驕槫唱・ｺ・ｦ, border=0遶願ｲ橸ｽｺ繝ｻ・・ border=1遶雁､蠑ｷ邵ｺ繝ｻ
                float rimBorder = GetFloatOr(sourceProps, "_RimBorder", 0.5f);
                // Natane邵ｺ・ｮRimSpread邵ｺ・ｯ0-1邵ｺ・ｧ邵ｲ繝ｻ=鬨ｾ螢ｼ・ｸ・ｸ邵ｲ竏敖・､邵ｺ謔滂ｽ､・ｧ邵ｺ髦ｪ・樒ｸｺ・ｻ邵ｺ・ｩ陟弱・窶ｲ郢ｧ繝ｻ
                // lilToon邵ｺ・ｮborder邵ｺ謔滂ｽｰ荳奇ｼ・ｸｺ繝ｻ竓・ｸｺ・ｩ郢晢ｽｪ郢晢｣ｰ邵ｺ謔滂ｽｺ繝ｻ・・
                float rimSpread = Mathf.Clamp01(1.0f - rimBorder);
                targetMaterial.SetFloat("_RimSpread", rimSpread);
                report.infos.Add($"Rim: Border={rimBorder:F2} -> RimSpread={rimSpread:F2}");

                // RimBlur 遶翫・RimPower邵ｺ・ｮ陟包ｽｮ髫ｱ・ｿ隰ｨ・ｴ
                float rimBlur = GetFloatOr(sourceProps, "_RimBlur", 0.65f);
                if (rimBlur > 0.01f)
                {
                    float currentPower = targetMaterial.GetFloat("_RimPower");
                    // blur邵ｺ謔滂ｽ､・ｧ邵ｺ髦ｪ・樒ｸｺ・ｻ邵ｺ・ｩpower郢ｧ蜑・ｽｸ荵晢ｿ｡邵ｺ・ｦ隴滓鱒・臥ｸｺ荵晢ｿ･邵ｺ蜷ｶ・・
                    float adjustedPower = currentPower * (1.0f - rimBlur * 0.5f);
                    targetMaterial.SetFloat("_RimPower", Mathf.Max(adjustedPower, 0.1f));
                    report.infos.Add($"Rim: Blur={rimBlur:F2} -> adjusted RimPower={adjustedPower:F2}");
                }

                // RimEnableLighting 遶翫・RimDirStrength
                float rimEnableLighting = GetFloatOr(sourceProps, "_RimEnableLighting", 1.0f);
                targetMaterial.SetFloat("_RimDirStrength", rimEnableLighting);

                // RimShadowMask
                float rimShadowMask = GetFloatOr(sourceProps, "_RimShadowMask", 0.5f);
                targetMaterial.SetFloat("_RimShadowMask", rimShadowMask);

                // RimBlendMode陞溽判驪､
                // StandardToon v2: LilBlendColor 郢ｧ蜑・ｽｽ・ｿ騾包ｽｨ (0=Normal, 1=Add, 2=Screen, 3=Multiply)
                // lilToon邵ｺ・ｮ郢晢ｽｪ郢晢｣ｰ邵ｺ・ｯ郢昴・繝ｵ郢ｧ・ｩ郢晢ｽｫ郢晏現縲・Add 陷ｷ蝓溘・繝ｻ蛹ｻ繝ｯ郢晢ｽｼ郢晏ｳｨ縺慕ｹ晢ｽｼ郢昜ｼ夲ｽｼ繝ｻ
                // 遶翫・LilBlendColor 邵ｺ・ｮ Add = 1 郢ｧ螳夲ｽｨ・ｭ陞ｳ繝ｻ
                targetMaterial.SetFloat("_RimBlendMode", 1); // LilBlendColor Add(1)

                report.infos.Add($"Rim Light enabled: DirStrength={rimEnableLighting:F2}, ShadowMask={rimShadowMask:F2}, BlendMode=1(LilBlendColor Add)");
            }

            // === Outline ===
            // lilToon邵ｺ・ｧ邵ｺ・ｯ_UseOutline邵ｺ・ｯ郢ｧ・ｷ郢ｧ・ｧ郢晢ｽｼ郢敖郢晢ｽｼ郢晁・ﾎ懃ｹｧ・｢郢晢ｽｳ郢晏現縲定崕繝ｻﾂｰ郢ｧ蠕鯉ｽ狗ｸｺ蠕個竏壹・郢晢ｽｭ郢昜ｻ｣繝ｦ郢ｧ・｣邵ｺ・ｨ邵ｺ蜉ｱ窶ｻ郢ｧ繧・ｽｭ莨懈Β
            bool hasOutline = false;
            if (sourceProps.ContainsKey("_OutlineWidth"))
            {
                float originalWidth = (float)sourceProps["_OutlineWidth"];
                if (originalWidth > 0)
                {
                    // lilToon: outlineWidth *= 0.01 (陷繝ｻﾎ夂ｸｺ・ｧ1/100郢ｧ・ｹ郢ｧ・ｱ郢晢ｽｼ郢晢ｽｫ)
                    // Natane:  outlineWidth = _OutlineWidth * 0.1 (陷繝ｻﾎ夂ｸｺ・ｧ1/10郢ｧ・ｹ郢ｧ・ｱ郢晢ｽｼ郢晢ｽｫ)
                    // 遶翫・lilToon邵ｺ・ｮ陋滂ｽ､郢ｧ隱ｰatane邵ｺ・ｫ陞溽判驪､邵ｺ蜷ｶ・狗ｸｺ・ｫ邵ｺ・ｯ 0.01/0.1 = 1/10 邵ｺ・ｫ郢ｧ・ｹ郢ｧ・ｱ郢晢ｽｼ郢晢ｽｫ
                    float convertedWidth = originalWidth * 0.1f;
                    convertedWidth = Mathf.Clamp(convertedWidth, 0.001f, 1.0f);
                    targetMaterial.SetFloat("_OutlineWidth", convertedWidth);
                    hasOutline = true;

                    report.outlineWidthAdjusted = true;
                    report.originalOutlineWidth = originalWidth;
                    report.convertedOutlineWidth = convertedWidth;
                    report.infos.Add($"Outline Width: lilToon {originalWidth:F4} -> Natane {convertedWidth:F4} (0.1 scale)");
                }
            }

            if (sourceProps.ContainsKey("_OutlineColor"))
            {
                targetMaterial.SetColor("_OutlineColor", (Color)sourceProps["_OutlineColor"]);
                hasOutline = true;
            }

            if (hasOutline)
            {
                targetMaterial.SetFloat("_Outline", 1.0f);
                targetMaterial.EnableKeyword("_OUTLINE");
            }

            // === Emission ===
            bool useEmission = GetFloatOr(sourceProps, "_UseEmission", 0) > 0.5f;

            if (useEmission)
            {
                SetTextureIfExists(sourceProps, "_EmissionMap", targetMaterial, "_EmissionMap");
                if (sourceProps.ContainsKey("_EmissionColor"))
                {
                    targetMaterial.SetColor("_EmissionColor", (Color)sourceProps["_EmissionColor"]);
                }
                targetMaterial.SetFloat("_Emission", 1.0f);
                targetMaterial.EnableKeyword("_EMISSION");
                report.infos.Add("Emission mapped.");
            }

            // === MatCap ===
            bool useMatCap = GetFloatOr(sourceProps, "_UseMatCap", 0) > 0.5f;

            if (useMatCap && sourceProps.ContainsKey("_MatCapTex") && sourceProps["_MatCapTex"] != null)
            {
                // 郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ慕ｸｺ・ｨ郢昜ｻ｣ﾎ帷ｹ晢ｽ｡郢晢ｽｼ郢ｧ・ｿ郢晢ｽｼ邵ｺ・ｯ驕假ｽｻ髯ｦ蠕娯・郢ｧ荵昶ｲ邵ｲ・atCap邵ｺ・ｯ郢ｧ・ｪ郢晁ｼ斐・邵ｺ・ｾ邵ｺ・ｾ邵ｺ・ｫ邵ｺ蜷ｶ・・
                // lilToon邵ｺ・ｨNatane邵ｺ・ｧ邵ｺ・ｯMatCap邵ｺ・ｮ隰門雀陌夂ｸｺ謔滂ｽ､・ｧ邵ｺ髦ｪ・･騾｡・ｰ邵ｺ・ｪ郢ｧ荵昶螺郢ｧ竏堋繝ｻ
                // 髢ｾ・ｪ陷榊｢捺剰怏・ｹ陋ｹ謔ｶ笘・ｹｧ荵昶・髫穂ｹ昶螺騾ｶ・ｮ邵ｺ謔滂ｽｴ・ｩ郢ｧ蠕鯉ｽ狗ｸｲ繧・倡ｹ晢ｽｼ郢ｧ・ｶ郢晢ｽｼ邵ｺ譴ｧ辟碑恪霈斐定ｭ帷甥譟題峪謔ｶ繝ｻ髫ｱ・ｿ隰ｨ・ｴ邵ｺ蜷ｶ・狗ｸｲ繝ｻ
                SetTextureIfExists(sourceProps, "_MatCapTex", targetMaterial, "_MatCapTex");

                targetMaterial.SetFloat("_MatCap", 0.0f);
                targetMaterial.DisableKeyword("_MATCAP");

                // 郢昜ｻ｣ﾎ帷ｹ晢ｽ｡郢晢ｽｼ郢ｧ・ｿ郢晢ｽｼ邵ｺ・ｮ驕假ｽｻ髯ｦ魃会ｽｼ蛹ｻ縺檎ｹ晄・諞ｾ隲ｷ荵昴定将譎・亜繝ｻ繝ｻ
                float matCapBlend = GetFloatOr(sourceProps, "_MatCapBlend", 1.0f);
                targetMaterial.SetFloat("_MatCapIntensity", 1.0f);
                targetMaterial.SetFloat("_MatCapBlend", matCapBlend);

                // MatCap Blend Mode陞溽判驪､
                int lilBlendMode = 1;
                if (sourceProps.ContainsKey("_MatCapBlendMode"))
                {
                    lilBlendMode = (int)(float)sourceProps["_MatCapBlendMode"];
                }
                int nataneBlendMode = ConvertMatCapBlendMode(lilBlendMode);
                targetMaterial.SetFloat("_MatCapBlendMode", nataneBlendMode);

                // MatCap 郢晄ｧｭ縺帷ｹｧ・ｯ郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ暮§・ｻ髯ｦ繝ｻ
                SetTextureIfExists(sourceProps, "_MatCapBlendMask", targetMaterial, "_MatCapMask");

                report.infos.Add("MatCap texture, blend mask, and blend mode were captured. The feature stays disabled by default.");
                report.warnings.Add(
                    "MatCap is implemented differently between lilToon and Natane. " +
                    "Review the final look and enable the feature manually if needed.");
            }

            // === MatCap 2nd ===
            bool useMatCap2nd = GetFloatOr(sourceProps, "_UseMatCap2nd", 0) > 0.5f;

            if (useMatCap2nd && sourceProps.ContainsKey("_MatCap2ndTex") && sourceProps["_MatCap2ndTex"] != null)
            {
                // 郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ慕ｸｺ・ｨ郢昜ｻ｣ﾎ帷ｹ晢ｽ｡郢晢ｽｼ郢ｧ・ｿ郢晢ｽｼ邵ｺ・ｯ驕假ｽｻ髯ｦ蠕娯・郢ｧ荵昶ｲ邵ｲ・atCap 2nd郢ｧ繧・′郢晁ｼ斐・邵ｺ・ｾ邵ｺ・ｾ邵ｺ・ｫ邵ｺ蜷ｶ・・
                SetTextureIfExists(sourceProps, "_MatCap2ndTex", targetMaterial, "_MatCapTex2");

                targetMaterial.SetFloat("_MatCap2", 0.0f);
                targetMaterial.DisableKeyword("_MATCAP_2");

                // 郢昜ｻ｣ﾎ帷ｹ晢ｽ｡郢晢ｽｼ郢ｧ・ｿ郢晢ｽｼ邵ｺ・ｮ驕假ｽｻ髯ｦ魃会ｽｼ蛹ｻ縺檎ｹ晄・諞ｾ隲ｷ荵昴定将譎・亜繝ｻ繝ｻ
                float matCap2ndBlend = GetFloatOr(sourceProps, "_MatCap2ndBlend", 1.0f);
                targetMaterial.SetFloat("_MatCapIntensity2", 1.0f);
                targetMaterial.SetFloat("_MatCapBlend2", matCap2ndBlend);

                // MatCap 2nd Blend Mode陞溽判驪､
                int lilBlendMode2 = 1;
                if (sourceProps.ContainsKey("_MatCap2ndBlendMode"))
                {
                    lilBlendMode2 = (int)(float)sourceProps["_MatCap2ndBlendMode"];
                }
                int nataneBlendMode2 = ConvertMatCapBlendMode(lilBlendMode2);
                targetMaterial.SetFloat("_MatCapBlendMode2", nataneBlendMode2);

                // MatCap 2nd 郢晄ｧｭ縺帷ｹｧ・ｯ郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ暮§・ｻ髯ｦ繝ｻ
                SetTextureIfExists(sourceProps, "_MatCap2ndBlendMask", targetMaterial, "_MatCapMask2");

                report.infos.Add("MatCap 2nd texture, blend mask, and blend mode were captured. The feature stays disabled by default.");
                report.warnings.Add(
                    "MatCap 2nd is implemented differently between lilToon and Natane. " +
                    "Review the final look and enable the feature manually if needed.");
            }

            // === Specular ===
            // lilToon邵ｺ・ｮ郢ｧ・ｹ郢晏｣ｹ縺冗ｹ晢ｽ･郢晢ｽｩ邵ｺ・ｯPBR郢晏生繝ｻ郢ｧ・ｹ邵ｺ・ｧ邵ｲ・ｼSpecularToon=1/_SpecularBorder=0.5邵ｺ・ｯ郢昴・繝ｵ郢ｧ・ｩ郢晢ｽｫ郢昜ｺ･ﾂ・､邵ｲ繝ｻ
            // 邵ｺ・ｻ邵ｺ・ｨ郢ｧ阮吮・邵ｺ・ｮ郢晏現縺・ｹ晢ｽｼ郢晢ｽｳ郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ邵ｺ・ｧ邵ｺ・ｯ郢ｧ・ｹ郢晏｣ｹ縺冗ｹ晢ｽ･郢晢ｽｩ邵ｺ・ｯ騾ｶ・ｮ驕ｶ荵昶螺邵ｺ・ｪ邵ｺ繝ｻ笳・ｹｧ竏堋繝ｻ
            // _Metallic > 0 邵ｺ・ｮ陜｣・ｴ陷ｷ蛹ｻ繝ｻ邵ｺ・ｿNatane邵ｺ・ｮ郢晏現縺・ｹ晢ｽｼ郢晢ｽｳ郢ｧ・ｹ郢晏｣ｹ縺冗ｹ晢ｽ･郢晢ｽｩ郢ｧ蜻域剰怏・ｹ陋ｹ謔ｶ笘・ｹｧ荵敖繝ｻ
            float metallic = GetFloatOr(sourceProps, "_Metallic", 0);
            if (metallic > 0.01f)
            {
                bool hasSpecular = false;

                if (sourceProps.ContainsKey("_SpecularToon"))
                {
                    float specToon = (float)sourceProps["_SpecularToon"];
                    if (specToon > 0.5f && sourceProps.ContainsKey("_SpecularBorder"))
                    {
                        float specBorder = (float)sourceProps["_SpecularBorder"];
                        float specBlur = GetFloatOr(sourceProps, "_SpecularBlur", 0.0f);

                        float size = Mathf.Clamp01(1.0f - specBorder);
                        float softness = Mathf.Clamp01(specBlur);
                        targetMaterial.SetFloat("_SpecularSize", size);
                        targetMaterial.SetFloat("_SpecularSoftness", Mathf.Max(softness, 0.05f));
                        hasSpecular = true;
                        report.infos.Add($"Specular(Toon): Border={specBorder:F2} -> Size={size:F2}, Metallic={metallic:F2}");
                    }
                }

                if (!hasSpecular && sourceProps.ContainsKey("_Smoothness"))
                {
                    float smoothness = (float)sourceProps["_Smoothness"];
                    float specularSize = OptimizeSpecularSize(smoothness);
                    targetMaterial.SetFloat("_SpecularSize", specularSize);
                    hasSpecular = true;
                    report.infos.Add($"Specular(PBR): Smoothness={smoothness:F2} -> Size={specularSize:F3}");
                }

                if (hasSpecular)
                {
                    targetMaterial.SetFloat("_Specular", 1.0f);
                    targetMaterial.EnableKeyword("_SPECULAR");
                    targetMaterial.SetColor("_SpecularColor", new Color(1, 1, 1, 1));
                    report.infos.Add("Specular enabled (fallback from Metallic).");
                }
            }
            else
            {
                report.infos.Add($"Specular mapped from Metallic: Metallic={metallic:F2}");
            }

            // === Alpha Cutoff ===
            if (sourceProps.ContainsKey("_Cutoff"))
            {
                float cutoff = (float)sourceProps["_Cutoff"];
                targetMaterial.SetFloat("_Cutoff", cutoff);
                report.infos.Add($"Alpha Cutoff: {cutoff:F2}");
            }

            // === StandardToon v2: 髯ｬ諛ｷ笏闕ｳ蟠趣ｽｦ繝ｻ===
            // v2 郢昜ｻ｣縺・ｹ晏干ﾎ帷ｹｧ・､郢晢ｽｳ邵ｺ・ｯ lerp(indirectCol, directCol, toon) 騾ｶ・ｴ隰暦ｽ･陷ｷ蝓溘・邵ｺ・ｮ邵ｺ貅假ｽ∫ｸｲ繝ｻ
            // Natane 邵ｺ・ｮ normalize+luminance 郢ｧ・ｹ郢昴・繝｣郢晄圜・ｼ繝ｻragment.hlsl:521-524繝ｻ蟲ｨ・定楜謔溘・邵ｺ・ｫ郢晁・縺・ｹ昜ｻ｣縺帷ｸｺ蜷ｶ・狗ｸｲ繝ｻ
            // _LightIntensity, _Brightness, _Saturation 邵ｺ・ｯ陷茨ｽｨ邵ｺ・ｦ 1.0繝ｻ蛹ｻ繝ｫ郢晢ｽ･郢晢ｽｼ郢晏現ﾎ帷ｹ晢ｽｫ繝ｻ蟲ｨﾂ繝ｻ
            targetMaterial.SetFloat("_LightIntensity", 1.0f);
            targetMaterial.SetFloat("_LightMaxInfluence", 2.0f);
            targetMaterial.SetFloat("_Brightness", 1.0f);
            targetMaterial.SetFloat("_Saturation", 1.0f);
            report.infos.Add("StandardToon v2 light defaults applied (_LightIntensity=1.0).");

            // === Shadow Floor / GI繝ｻ繝ｻtandardToon v2 邵ｺ・ｧ邵ｺ・ｯ闕ｳ蝣ｺ・ｽ・ｿ騾包ｽｨ繝ｻ繝ｻ===
            // StandardToon 邵ｺ・ｯ霑｢・ｬ髢ｾ・ｪ邵ｺ・ｮ lilToon 闔蜻磯共郢昜ｻ｣縺・ｹ晏干ﾎ帷ｹｧ・､郢晢ｽｳ邵ｺ・ｧ陷ｷ蝓溘・邵ｺ蜷ｶ・狗ｸｺ貅假ｽ∫ｸｲ繝ｻ
            // Natane 邵ｺ・ｮ indirectResult / _IndirectLightMinColor 驍会ｽｻ邵ｺ・ｯ闖ｴ・ｿ騾包ｽｨ邵ｺ蜉ｱ竊醍ｸｺ繝ｻﾂ繝ｻ
            // _GIIntensity = 0 邵ｺ・ｧ鬮｢謐ｺ逎∬怦閾･・ｳ・ｻ郢ｧ蝣､笏瑚怏・ｹ陋ｹ謔ｶ・邵ｲ竏ｬ迚｡邵ｺ荵昴・郢ｧ鄙ｫ・帝ｫｦ・ｲ雎・ｽ｢邵ｲ繝ｻ
            // 魄溷ｸ昜ｺ溯ｱ・ｽ｢邵ｺ・ｯ _LightColorMin (= lilToon _LightMinLimit) 邵ｺ・ｧ闖ｫ譎・ｽｨ・ｼ邵ｲ繝ｻ
            targetMaterial.SetFloat("_ShadowMaxDarkness", 0.15f);
            targetMaterial.SetFloat("_LightMinInfluence", 0.05f);
            targetMaterial.SetFloat("_GIIntensity", 0.0f);
            report.infos.Add("Floor/GI defaults applied (_GIIntensity=0, _LightColorMin preserved).");

            // === Light Color Limits (lilToon闔蜻磯共) ===
            // StandardToon郢晢ｽ｢郢晢ｽｼ郢晏ｳｨ縲堤ｸｺ・ｯ騾ｶ・ｴ隰暦ｽ･闕ｵ遉ｼ・ｮ蜉ｱ繝ｻ邵ｺ貅假ｽ∫ｸｲ繝ｻ・ｷ2髯ｬ諛茨ｽｭ・｣邵ｺ・ｯ闕ｳ蟠趣ｽｦ竏堋繝ｻ
            // lilToon邵ｺ・ｮ郢昜ｻ｣ﾎ帷ｹ晢ｽ｡郢晢ｽｼ郢ｧ・ｿ郢ｧ蛛ｵ笳守ｸｺ・ｮ邵ｺ・ｾ邵ｺ・ｾ闖ｴ・ｿ騾包ｽｨ邵ｺ蜷ｶ・狗ｸｲ繝ｻ
            float lightMinLimit = GetFloatOr(sourceProps, "_LightMinLimit", 0.05f);
            float lightMaxLimit = GetFloatOr(sourceProps, "_LightMaxLimit", 1.0f);
            float monochromeLighting = GetFloatOr(sourceProps, "_MonochromeLighting", 0.0f);

            targetMaterial.SetFloat("_LightColorMin", lightMinLimit);
            targetMaterial.SetFloat("_LightColorMax", lightMaxLimit);
            targetMaterial.SetFloat("_MonochromeLighting", monochromeLighting);
            report.infos.Add($"StandardToon light color limits mapped: ColorMax={lightMaxLimit:F2}, ColorMin={lightMinLimit:F2}, Monochrome={monochromeLighting:F2}");

            // _AsUnlit 遶翫・_STAsUnlit 邵ｺ・ｫ騾ｶ・ｴ隰暦ｽ･郢晄ｧｭ繝｣郢晄鱒ﾎｦ郢ｧ・ｰ
            // StandardToon v2邵ｺ・ｧ邵ｺ・ｯ郢ｧ・ｷ郢ｧ・ｧ郢晢ｽｼ郢敖郢晢ｽｼ陷繝ｻ縲・stLightColor = lerp(stLightColor, 1, _STAsUnlit) 郢ｧ雋橸ｽｮ貅ｯ・｡繝ｻ
            float asUnlit = GetFloatOr(sourceProps, "_AsUnlit", 0.0f);
            targetMaterial.SetFloat("_STAsUnlit", asUnlit);
            if (asUnlit > 0.01f)
            {
                report.infos.Add($"AsUnlit mapped to _STAsUnlit={asUnlit:F2}");
            }

            // Shadow Environment Strength (鬮｢謐ｺ逎∬怦蟲ｨ竊鍋ｹｧ蛹ｻ・玖厄ｽｱ隰問・笆闕ｳ鄙ｫ・｡)
            // lilToon: _ShadowEnvStrength (default 1.0) 遯ｶ繝ｻindirectCol 郢ｧ蟶昜ｿ｣隰暦ｽ･陷亥ｳｨ縲定ｬ問・笆闕ｳ鄙ｫ・｡
            // StandardToon v2: stIndirectCol = lerp(stIndirectCol, stAlbedo, saturate(stIndLightColor * _STShadowEnvStrength))
            float shadowEnvStrength = GetFloatOr(sourceProps, "_ShadowEnvStrength", 1.0f);
            targetMaterial.SetFloat("_STShadowEnvStrength", shadowEnvStrength);
            report.infos.Add($"Shadow Env Strength mapped to _STShadowEnvStrength={shadowEnvStrength:F2}");
        }

        /// <summary>
        /// sourceProps邵ｺ荵晢ｽ映loat郢ｧ雋槫徐陟募干ﾂ竏壺・邵ｺ莉｣・檎ｸｺ・ｰ郢昴・繝ｵ郢ｧ・ｩ郢晢ｽｫ郢昜ｺ･ﾂ・､郢ｧ螳夲ｽｿ譁絶・
        /// </summary>
        private float GetFloatOr(Dictionary<string, object> props, string key, float defaultValue)
        {
            if (props.ContainsKey(key))
                return (float)props[key];
            return defaultValue;
        }

        private void SetTextureIfExists(Dictionary<string, object> source, string sourceKey, Material target, string targetKey)
        {
            if (source.ContainsKey(sourceKey) && source[sourceKey] != null)
            {
                target.SetTexture(targetKey, (Texture)source[sourceKey]);
            }
        }

        private void SetColorIfExists(Dictionary<string, object> source, string sourceKey, Material target, string targetKey)
        {
            if (source.ContainsKey(sourceKey))
            {
                target.SetColor(targetKey, (Color)source[sourceKey]);
            }
        }

        private void SetFloatIfExists(Dictionary<string, object> source, string sourceKey, Material target, string targetKey)
        {
            if (source.ContainsKey(sourceKey))
            {
                target.SetFloat(targetKey, (float)source[sourceKey]);
            }
        }

        /// <summary>
        /// Smoothness邵ｺ荵晢ｽ唄pecular Size邵ｺ・ｸ陞溽判驪､繝ｻ逎ｯ謦ｼ驍ｱ螢ｼ・ｽ・｢郢ｧ・ｫ郢晢ｽｼ郢晏私・ｼ繝ｻ
        /// lilToon邵ｺ・ｮSmoothness邵ｺ・ｯ0-1邵ｺ・ｮ驕ｽ繝ｻ蟲・ｸｲﾂｨatane Toon Shader邵ｺ・ｮSpecularSize邵ｺ・ｯ0.01-1.0邵ｺ譴ｧ閠ｳ陞ゑｽｨ
        /// 1.5闕ｵ蜉ｱ縺咲ｹ晢ｽｼ郢晄じ縲定叉・ｭ鬮｢轣伉・､郢ｧ蛛ｵ・・ｹｧ繝ｻ・ｰ荳奇ｼ・ｸｺ荳奇ｼ邵ｲ竏夲ｽ育ｹｧ鬘倥・霎滂ｽｶ邵ｺ・ｪ郢昜ｸ翫≧郢晢ｽｩ郢ｧ・､郢晏現竊・
        /// </summary>
        private float OptimizeSpecularSize(float smoothness)
        {
            // smoothness: 0 (rough) 遶翫・1 (smooth/glossy)
            // specularSize: 0.01 (small highlight) 遶翫・0.5 (large highlight)

            float normalized = Mathf.Clamp01(smoothness);
            float curved = Mathf.Pow(normalized, 1.5f); // 1.5闕ｵ蜉ｱ縲定叉・ｭ鬮｢轣伉・､郢ｧ蛛ｵ・・ｹｧ繝ｻ・ｰ荳奇ｼ・ｸｺ繝ｻ
            float specularSize = Mathf.Lerp(0.01f, 0.5f, curved);

            return specularSize;
        }

        /// <summary>
        /// lilToon邵ｺ・ｮMatCapBlendMode郢ｧ隱ｰatane Toon Shader邵ｺ・ｮBlendMode邵ｺ・ｫ陞溽判驪､
        /// lilToon: 0=Normal(lerp), 1=Add, 2=Screen, 3=Multiply
        /// Natane:  0=Add, 1=Multiply, 2=Replace
        ///
        /// 隨倥・纃ｾ髫輔・ lilToon Normal(0) = lerp(dst, src, srcA) = 郢ｧ・｢郢晢ｽｫ郢晁ｼ斐＜郢晄じﾎ樒ｹ晢ｽｳ郢昴・
        ///   遶翫・Natane Add(0)邵ｺ・ｫ郢晄ｧｭ繝｣郢晄鱒ﾎｦ郢ｧ・ｰ邵ｺ蜷ｶ・狗ｸｺ・ｨ騾具ｽｽ鬯溷ｸ吶・邵ｺ蜷ｶ・九・繝ｻ
        ///   遶翫・Replace(2) = lerp(base, matcap, blend) 邵ｺ譴ｧ諤咏ｹｧ繧奇ｽｿ莉｣・・
        /// </summary>
        private int ConvertMatCapBlendMode(int lilBlendMode)
        {
            switch (lilBlendMode)
            {
                case 0: return 2; // Normal(lerp) 遶翫・Replace (隨倥・蜊鬯溷ｸ吶・闖ｫ・ｮ雎・ｽ｣: Add遶雁擱eplace)
                case 1: return 0; // Add 遶翫・Add
                case 2: return 0; // Screen 遶翫・Add (髴大床・ｼ・ｼ邵ｲﾂｨatane邵ｺ・ｫScreen隴幢ｽｪ陝・ｽｾ陟｢繝ｻ
                case 3: return 1; // Multiply 遶翫・Multiply
                default: return 2; // 闕ｳ閧ｴ繝ｻ 遶翫・Replace (陞ｳ迚吶・)
            }
        }

        /// <summary>
        /// lilToon邵ｺ・ｮRimBlendMode郢ｧ隱ｰatane Toon Shader邵ｺ・ｮRimBlendMode邵ｺ・ｫ陞溽判驪､
        /// lilToon: 0=Normal(lerp), 1=Add, 2=Screen, 3=Multiply
        /// Natane:  0=Normal(SoftLight), 1=Soft, 2=Screen, 3=Overlay
        /// </summary>
        private int ConvertRimBlendMode(int lilBlendMode)
        {
            switch (lilBlendMode)
            {
                case 0: return 2; // Normal(lerp) 遶翫・Screen (lerp邵ｺ・ｮ髴大床・ｼ・ｼ邵ｺ・ｧScreen邵ｺ譴ｧ諤咏ｹｧ繧翫・霎滂ｽｶ)
                case 1: return 0; // Add 遶翫・Normal(SoftLight) (陷会｣ｰ驍ら､ｼ蝎ｪ陷会ｽｹ隴ｫ繝ｻ
                case 2: return 2; // Screen 遶翫・Screen
                case 3: return 3; // Multiply 遶翫・Overlay (雋ょｹ・ｽ｡・ｰ驍会ｽｻ邵ｺ・ｮ髴大床・ｼ・ｼ)
                default: return 0;
            }
        }

        /// <summary>
        /// lilToon邵ｺ・ｮ郢ｧ・ｷ郢ｧ・ｧ郢晢ｽｼ郢敖郢晢ｽｼ陷ｷ髦ｪﾂｰ郢ｧ荳疎tane Toon Shader邵ｺ・ｮ郢晁・ﾎ懃ｹｧ・｢郢晢ｽｳ郢晏現・帝明・ｪ陷榊｢難ｽ､諛ｷ繝ｻ
        /// </summary>
        private Shader DetectNataneShaderVariant(Material sourceMaterial)
        {
            string shaderName = sourceMaterial.shader.name.ToLower();

            if (shaderName.Contains("transparent") || shaderName.Contains("fade"))
            {
                Shader transparentShader = Shader.Find("Natane/Toon Shader Transparent");
                if (transparentShader != null) return transparentShader;
            }
            else if (shaderName.Contains("cutout"))
            {
                Shader cutoutShader = Shader.Find("Natane/Toon Shader Cutout");
                if (cutoutShader != null) return cutoutShader;
            }

            // 郢昴・繝ｵ郢ｧ・ｩ郢晢ｽｫ郢昴・ Opaque郢晁・ﾎ懃ｹｧ・｢郢晢ｽｳ郢晁肩・ｼ蛹ｻ繝ｵ郢ｧ・ｩ郢晢ｽｼ郢晢ｽｫ郢晁・繝｣郢ｧ・ｯ陷ｷ・ｫ郢ｧﾂ繝ｻ繝ｻ
            return Shader.Find("Natane/Toon Shader");
        }

        /// <summary>
        /// 郢晄ｧｭﾎ晉ｹ昶・縺咏ｹ晢ｽ｣郢晏ｳｨ縺育ｹ晢ｽｬ郢ｧ・､郢晢ｽ､郢晢ｽｼ陞溽判驪､繝ｻ繝ｻnd/3rd Shadow繝ｻ繝ｻ
        /// lilToon: _Shadow2ndColor, _Shadow2ndBorder (0-1), _Shadow3rdColor, _Shadow3rdBorder (0-1)
        /// Natane:  _Shadow2ndColor, _Shadow2ndBorder, _Shadow3rdColor, _Shadow3rdBorder + _USE_MULTI_SHADOW keyword
        /// </summary>
        private void MapMultiShadowLayers(Dictionary<string, object> sourceProps, Material targetMaterial, ConversionReport report)
        {
            bool has2nd = sourceProps.ContainsKey("_Shadow2ndColor");
            bool has3rd = sourceProps.ContainsKey("_Shadow3rdColor");

            if (!has2nd && !has3rd) return;

            // Check if 2nd shadow is actually meaningful (not just default values)
            // lilToon always has _Shadow2ndColor as a property, but it may be unused
            bool meaningful2nd = false;
            if (has2nd)
            {
                Color shadow2nd = (Color)sourceProps["_Shadow2ndColor"];
                // Consider meaningful if alpha > 0 and color isn't pure white (default/unused)
                meaningful2nd = shadow2nd.a > 0.01f &&
                    (shadow2nd.r < 0.99f || shadow2nd.g < 0.99f || shadow2nd.b < 0.99f);
            }

            bool meaningful3rd = false;
            if (has3rd)
            {
                Color shadow3rd = (Color)sourceProps["_Shadow3rdColor"];
                meaningful3rd = shadow3rd.a > 0.01f &&
                    (shadow3rd.r < 0.99f || shadow3rd.g < 0.99f || shadow3rd.b < 0.99f);
            }

            if (!meaningful2nd && !meaningful3rd)
            {
                report.infos.Add("Multi-shadow skipped: 2nd/3rd shadow colors are effectively unused.");
                return;
            }

            // Enable multi-shadow keyword
            targetMaterial.EnableKeyword("_USE_MULTI_SHADOW");

            if (meaningful2nd)
            {
                Color shadow2nd = (Color)sourceProps["_Shadow2ndColor"];
                targetMaterial.SetColor("_Shadow2ndColor", shadow2nd);
                float border2nd = GetFloatOr(sourceProps, "_Shadow2ndBorder", 0.15f);
                targetMaterial.SetFloat("_Shadow2ndBorder", border2nd);
                report.infos.Add($"2nd Shadow: Color={shadow2nd}, Border={border2nd:F2}");
            }

            if (meaningful3rd)
            {
                Color shadow3rd = (Color)sourceProps["_Shadow3rdColor"];
                targetMaterial.SetColor("_Shadow3rdColor", shadow3rd);
                float border3rd = GetFloatOr(sourceProps, "_Shadow3rdBorder", 0.25f);
                targetMaterial.SetFloat("_Shadow3rdBorder", border3rd);
                report.infos.Add($"3rd Shadow: Color={shadow3rd}, Border={border3rd:F2}");
            }
        }

        /// <summary>
        /// 陜難ｽｺ隴幢ｽｬ郢ｧ・ｿ郢晉ｴ具ｽｻ・･陞滓じ繝ｻ邵ｺ蜷ｶ竏狗ｸｺ・ｦ邵ｺ・ｮ隶匁ｺｯ繝ｻ郢ｧ蛛ｵ縺檎ｹ晁ｼ披・邵ｺ蜷ｶ・・
        /// 郢晏干ﾎ溽ｹ昜ｻ｣繝ｦ郢ｧ・｣邵ｺ・ｮ陋滂ｽ､邵ｺ・ｯ闖ｫ譎・亜邵ｺ霈費ｽ檎ｹｧ荵昶ｲ邵ｲ竏壹Κ郢ｧ・ｰ郢晢ｽｫ邵ｺ・ｨ郢ｧ・ｭ郢晢ｽｼ郢晢ｽｯ郢晢ｽｼ郢晏ｳｨ・定ｾ滂ｽ｡陷会ｽｹ陋ｹ謔ｶ笘・ｹｧ繝ｻ
        /// 驕假ｽｻ髯ｦ謔滂ｽｾ蠕娯・郢晢ｽｦ郢晢ｽｼ郢ｧ・ｶ郢晢ｽｼ邵ｺ謔滂ｽｿ繝ｻ・ｦ竏壺・隶匁ｺｯ繝ｻ郢ｧ雋楪蜿･謖ｨ邵ｺ・ｫ隴帷甥譟題峪謔ｶ笘・ｹｧ荵晢ｼ・ｸｺ・ｨ郢ｧ蜻夷ｦ陞ｳ繝ｻ
        /// </summary>
        private void DisableNonBasicFeatures(Material material, ConversionReport report)
        {
            // === 郢晢ｽｩ郢ｧ・､郢昴・縺・ｹ晢ｽｳ郢ｧ・ｰ郢ｧ・ｿ郢昴・===
            DisableFeature(material, "_SoftLightingMode", "_SOFT_LIGHTING_MODE");
            DisableFeature(material, "_UsePixelVertexLights", "_PIXEL_VERTEX_LIGHTS");
            DisableFeature(material, "_UseLightVolume", "_USE_LIGHT_VOLUME");
            DisableFeature(material, "_LightVolumeSpecular", "_LIGHT_VOLUME_SPECULAR");
            DisableFeature(material, "_LTCGI", "_LTCGI");
            DisableFeature(material, "_UseAO", "_USE_AO");
            DisableFeature(material, "_UseDithering", "_USE_DITHERING");

            // === 郢ｧ・ｨ郢晁ｼ斐♂郢ｧ・ｯ郢晏現縺｡郢昴・===
            DisableFeature(material, "_Specular", "_SPECULAR");
            DisableFeature(material, "_RimLight", "_RIM_LIGHT");
            DisableFeature(material, "_RimLight2", "_RIM_LIGHT_2");
            DisableFeature(material, "_SSS", "_SSS");
            DisableFeature(material, "_MatCap", "_MATCAP");
            DisableFeature(material, "_MatCap2", "_MATCAP_2");
            DisableFeature(material, "_MatCap3", "_MATCAP_3");
            DisableFeature(material, "_Glitter", "_GLITTER");
            DisableFeature(material, "_WaterDrip", "_WATER_DRIP");
            DisableFeature(material, "_Hologram", "_HOLOGRAM");
            DisableFeature(material, "_UseHologramNoise", "_HOLOGRAM_NOISE");
            DisableFeature(material, "_Glitch", "_GLITCH");
            DisableFeature(material, "_Outline", "_OUTLINE");
            DisableFeature(material, "_Emission", "_EMISSION");
            DisableFeature(material, "_Dissolve", "_DISSOLVE");
            DisableFeature(material, "_HueShiftEnable", "_HUE_SHIFT");
            DisableFeature(material, "_UseAlphaMask", "_ALPHA_MASK");
            DisableFeature(material, "_AudioLink", "_AUDIOLINK");

            // === 霑ｺ・ｰ陟・・縺｡郢昴・===
            DisableFeature(material, "_Reflection", "_REFLECTION");
            DisableFeature(material, "_Iridescence", "_IRIDESCENCE");
            DisableFeature(material, "_EnvRim", "_ENV_RIM");
            DisableFeature(material, "_Refraction", "_REFRACTION");

            // === 髫ｧ・ｳ驍擾ｽｰ郢ｧ・ｿ郢昴・===
            DisableFeature(material, "_UseNormalMap", "_NORMALMAP");
            DisableFeature(material, "_Parallax", "_PARALLAX");
            DisableFeature(material, "_VAT", "_VAT");
            DisableFeature(material, "_VATNormal", "_VAT_NORMAL");
            DisableFeature(material, "_BackfaceTexture", "_BACKFACE_TEXTURE");
            DisableFeature(material, "_VideoTexture", "_VIDEO_TEXTURE");
            DisableFeature(material, "_DistanceFade", "_DISTANCE_FADE");

            report.infos.Add("Disabled non-basic features. Their stored values were preserved for manual re-enable.");
        }

        /// <summary>
        /// 陋溷唱謖ｨ邵ｺ・ｮ隶匁ｺｯ繝ｻ郢ｧ蛛ｵ縺檎ｹ晁ｼ披・邵ｺ蜷ｶ・九・蛹ｻ繝ｻ郢晢ｽｭ郢昜ｻ｣繝ｦ郢ｧ・｣陋滂ｽ､郢ｧ繝ｻ邵ｺ・ｫ邵ｺ蜉ｱﾂ竏壹￥郢晢ｽｼ郢晢ｽｯ郢晢ｽｼ郢晏ｳｨ・定ｾ滂ｽ｡陷会ｽｹ陋ｹ蜴・ｽｼ繝ｻ
        /// </summary>
        private void DisableFeature(Material material, string propertyName, string keyword)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, 0f);
            }
            material.DisableKeyword(keyword);
        }

        // ===================================
        // 郢晏干ﾎ樒ｹ昜ｸ翫Ω郢晢ｽ｢郢晢ｽｼ郢晁・逡醍ｹ晢ｽ｡郢ｧ・ｽ郢昴・繝ｩ驗抵ｽ､
        // ===================================

        /// <summary>
        /// 郢晏干ﾎ樒ｹ昜ｸ翫Ω陷繝ｻ繝ｻlilToon郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ郢ｧ蛛ｵ縺帷ｹｧ・ｭ郢晢ｽ｣郢晢ｽｳ邵ｺ蜉ｱ窶ｻ闕ｳﾂ髫包ｽｧ郢ｧ蜻茨ｽｧ迢暦ｽｯ繝ｻ
        /// </summary>
        private void ScanPrefabMaterials()
        {
            prefabMaterials.Clear();

            if (targetPrefab == null) return;

            Renderer[] renderers = targetPrefab.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                Material[] sharedMats = renderer.sharedMaterials;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    Material mat = sharedMats[i];
                    if (mat == null || mat.shader == null) continue;

                    string shaderName = mat.shader.name;
                    if (shaderName.Contains("lilToon") || shaderName.StartsWith("_lil/"))
                    {
                        prefabMaterials.Add(new PrefabMaterialInfo
                        {
                            original = mat,
                            converted = null,
                            renderer = renderer,
                            materialIndex = i,
                            willConvert = true,
                            rendererPath = GetHierarchyPath(renderer.transform, targetPrefab.transform)
                        });
                    }
                }
            }

            Debug.Log($"Found {prefabMaterials.Select(m => m.original).Distinct().Count()} lilToon materials in prefab '{targetPrefab.name}'.");
        }

        /// <summary>
        /// Transform鬮ｫ荳ｻ・ｱ・､郢昜ｻ｣縺帷ｹｧ雋槫徐陟墓圜・ｼ蛹ｻ繝ｻ郢晢ｽｬ郢昜ｸ翫Ω郢晢ｽｫ郢晢ｽｼ郢晏現ﾂｰ郢ｧ蟲ｨ繝ｻ騾ｶ・ｸ陝・ｽｾ郢昜ｻ｣縺帙・繝ｻ
        /// </summary>
        private string GetHierarchyPath(Transform target, Transform root)
        {
            var parts = new List<string>();
            Transform current = target;

            while (current != null && current != root)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }

            return parts.Count > 0 ? string.Join("/", parts) : target.name;
        }

        /// <summary>
        /// 郢晏干ﾎ樒ｹ昜ｸ翫Ω陷繝ｻ繝ｻ鬩包ｽｸ隰壽ｧｭ繝ｻ郢昴・ﾎ懃ｹｧ・｢郢晢ｽｫ郢ｧ蜑・ｽｸﾂ隲｡・ｬ陞溽判驪､
        /// </summary>
        private void ConvertPrefabMaterials()
        {
            // 陞溽判驪､陝・ｽｾ髮趣ｽ｡郢ｧ雋槫徐陟墓圜・ｼ逎ｯ纃ｾ髫阪・雉憺ｫｯ・､繝ｻ繝ｻ
            var materialsToConvert = prefabMaterials
                .Where(m => m.willConvert && m.converted == null)
                .Select(m => m.original)
                .Distinct()
                .ToList();

            if (materialsToConvert.Count == 0) return;

            string dialogMessage = duplicateInHierarchy
                ? L($"'{targetPrefab.name}' ???????? {materialsToConvert.Count} ???????????????\n???????????????", $"Create a duplicate of '{targetPrefab.name}' and convert {materialsToConvert.Count} materials?\nThe original prefab will not be modified.")
                : L($"'{targetPrefab.name}' ?? {materialsToConvert.Count} ???????????????", $"Are you sure you want to convert {materialsToConvert.Count} materials in '{targetPrefab.name}'?");

            if (!EditorUtility.DisplayDialog(
                L("?????????????", "Convert Prefab Materials"),
                dialogMessage,
                L("はい", "Yes"), L("キャンセル", "Cancel")))
            {
                return;
            }

            // Undo郢ｧ・ｰ郢晢ｽｫ郢晢ｽｼ郢晉､ｼ蛹ｳ鬪ｭ・ｲ
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"lilToon Migration - {targetPrefab.name}");

            int successCount = 0;
            List<ConversionReport> reports = new List<ConversionReport>();
            Dictionary<Material, Material> materialMapping = new Dictionary<Material, Material>();

            try
            {
                for (int i = 0; i < materialsToConvert.Count; i++)
                {
                    Material sourceMat = materialsToConvert[i];

                EditorUtility.DisplayProgressBar(
                    L("プレハブ内マテリアル変換中", "Converting Prefab Materials"),
                    L($"Converting {i + 1}/{materialsToConvert.Count}: {sourceMat.name}", $"Converting {i + 1}/{materialsToConvert.Count}: {sourceMat.name}"),
                    (float)i / materialsToConvert.Count
                );

                var report = ConvertMaterialWithReport(sourceMat);
                reports.Add(report);

                if (report.success)
                {
                    successCount++;

                    // materialMapping郢ｧ蜻茨ｽｧ迢暦ｽｯ莨夲ｽｼ驛・ｽ､繝ｻ・｣・ｽ郢晢ｽ｢郢晢ｽｼ郢晏ｳｨ縲堤ｸｺ・ｯ陝ｶ・ｸ邵ｺ・ｫ隴・ｽｰ髫穂ｸ翫・郢昴・ﾎ懃ｹｧ・｢郢晢ｽｫ郢ｧ蜑・ｽｽ・ｿ騾包ｽｨ繝ｻ繝ｻ
                    if (!replaceOriginal || duplicateInHierarchy)
                    {
                        string sourcePath = AssetDatabase.GetAssetPath(sourceMat);
                        string newPath = sourcePath.Replace(".mat", "_NataneToon.mat");
                        Material newMat = AssetDatabase.LoadAssetAtPath<Material>(newPath);
                        if (newMat != null)
                        {
                            materialMapping[sourceMat] = newMat;
                        }
                    }
                }
            }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (duplicateInHierarchy && materialMapping.Count > 0)
            {
                // 髫阪・・｣・ｽ郢晢ｽ｢郢晢ｽｼ郢昴・ 郢晏・縺顔ｹ晢ｽｩ郢晢ｽｫ郢ｧ・ｭ郢晢ｽｼ邵ｺ・ｫ髫阪・・｣・ｽ郢ｧ蜑・ｽｽ諛医・邵ｺ蜉ｱﾂ竏ｬ・､繝ｻ・｣・ｽ邵ｺ・ｮ邵ｺ・ｿ郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ郢ｧ雋橸ｽｷ・ｮ邵ｺ邇ｲ蟠帷ｸｺ繝ｻ
                DuplicateAndApplyMaterials(materialMapping);
            }
            else if (updatePrefabReferences && !replaceOriginal && materialMapping.Count > 0)
            {
                // 鬨ｾ螢ｼ・ｸ・ｸ郢晢ｽ｢郢晢ｽｼ郢昴・ 陷医・繝ｻ郢晏干ﾎ樒ｹ昜ｸ翫Ω邵ｺ・ｮ陷ｿ繧峨・郢ｧ蜻亥ｳｩ隴・ｽｰ
                UpdatePrefabReferences(materialMapping);
            }

            // Undo郢ｧ・ｰ郢晢ｽｫ郢晢ｽｼ郢晏干・帝ｫ｢蟲ｨﾂｧ郢ｧ繝ｻ
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);

            // 郢晢ｽｬ郢晄亢繝ｻ郢晞メ・｡・ｨ驕会ｽｺ
            ShowConversionReport(reports, successCount);

            // 郢晢ｽｪ郢ｧ・ｹ郢晏沺蟲ｩ隴・ｽｰ
            ScanPrefabMaterials();
        }

        /// <summary>
        /// 陋溷唱謖ｨ邵ｺ・ｮ郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ郢ｧ雋橸ｽ､逕ｻ驪､繝ｻ蛹ｻ繝ｻ郢晢ｽｬ郢昜ｸ翫Ω郢晢ｽ｢郢晢ｽｼ郢晁・逡代・繝ｻ
        /// </summary>
        private void ConvertSinglePrefabMaterial(Material sourceMaterial)
        {
            // Undo郢ｧ・ｰ郢晢ｽｫ郢晢ｽｼ郢晉､ｼ蛹ｳ鬪ｭ・ｲ
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"lilToon Migration - {sourceMaterial.name}");

            var report = ConvertMaterialWithReport(sourceMaterial);

            if (report.success)
            {
                string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
                string newPath = sourcePath.Replace(".mat", "_NataneToon.mat");
                Material newMat = AssetDatabase.LoadAssetAtPath<Material>(newPath);

                if (newMat != null)
                {
                    var mapping = new Dictionary<Material, Material> { { sourceMaterial, newMat } };

                    if (duplicateInHierarchy)
                    {
                        // 髫阪・・｣・ｽ郢晢ｽ｢郢晢ｽｼ郢昴・ 髫阪・・｣・ｽ郢ｧ蜑・ｽｽ諛医・邵ｺ蜉ｱ窶ｻ郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ郢ｧ雋橸ｽｷ・ｮ邵ｺ邇ｲ蟠帷ｸｺ繝ｻ
                        DuplicateAndApplyMaterials(mapping);
                    }
                    else if (updatePrefabReferences && !replaceOriginal)
                    {
                        // 鬨ｾ螢ｼ・ｸ・ｸ郢晢ｽ｢郢晢ｽｼ郢昴・ 陷医・繝ｻ郢晏干ﾎ樒ｹ昜ｸ翫Ω邵ｺ・ｮ陷ｿ繧峨・郢ｧ蜻亥ｳｩ隴・ｽｰ
                        UpdatePrefabReferences(mapping);
                    }
                }

                AssetDatabase.SaveAssets();
                ShowConversionReport(new List<ConversionReport> { report }, 1);
            }

            // Undo郢ｧ・ｰ郢晢ｽｫ郢晢ｽｼ郢晏干・帝ｫ｢蟲ｨﾂｧ郢ｧ繝ｻ
            Undo.CollapseUndoOperations(undoGroup);

            // 郢晢ｽｪ郢ｧ・ｹ郢晏沺蟲ｩ隴・ｽｰ
            ScanPrefabMaterials();
        }

        /// <summary>
        /// 郢晏干ﾎ樒ｹ昜ｸ翫Ω邵ｺ・ｮ郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ陷ｿ繧峨・郢ｧ蜻育悛邵ｺ蜉ｱ・樒ｹ晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ邵ｺ・ｫ隴厄ｽｴ隴・ｽｰ
        /// 郢ｧ・ｷ郢晢ｽｼ郢晢ｽｳ闕ｳ鄙ｫ繝ｻ郢ｧ・､郢晢ｽｳ郢ｧ・ｹ郢ｧ・ｿ郢晢ｽｳ郢ｧ・ｹ邵ｺ・ｨ郢晏干ﾎ樒ｹ昜ｸ翫Ω郢ｧ・｢郢ｧ・ｻ郢昴・繝ｨ邵ｺ・ｮ闕ｳ・｡隴・ｽｹ邵ｺ・ｫ陝・ｽｾ陟｢繝ｻ
        /// </summary>
        private void UpdatePrefabReferences(Dictionary<Material, Material> materialMapping)
        {
            if (targetPrefab == null || materialMapping.Count == 0) return;

            // 郢ｧ・ｷ郢晢ｽｼ郢晢ｽｳ闕ｳ鄙ｫ繝ｻ郢ｧ・､郢晢ｽｳ郢ｧ・ｹ郢ｧ・ｿ郢晢ｽｳ郢ｧ・ｹ邵ｺ・ｮ陜｣・ｴ陷ｷ蛹ｻ繝ｻ郢晏干ﾎ樒ｹ昜ｸ翫Ω郢ｧ・｢郢ｧ・ｻ郢昴・繝ｨ郢昜ｻ｣縺帷ｹｧ雋槫徐陟輔・
            string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
            bool isSceneInstance = string.IsNullOrEmpty(prefabPath);

            if (isSceneInstance)
            {
                prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetPrefab);
            }

            // 郢晏干ﾎ樒ｹ昜ｸ翫Ω郢ｧ・｢郢ｧ・ｻ郢昴・繝ｨ郢ｧ蝣､・ｷ・ｨ鬮ｮ繝ｻ笘・ｹｧ蜿･・ｰ・ｴ陷ｷ繝ｻ
            if (!string.IsNullOrEmpty(prefabPath))
            {
                GameObject prefabContents = null;
                try
                {
                    prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

                    Renderer[] renderers = prefabContents.GetComponentsInChildren<Renderer>(true);
                    bool changed = false;

                    foreach (var renderer in renderers)
                    {
                        Material[] materials = renderer.sharedMaterials;
                        bool rendererChanged = false;

                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (materials[i] != null && materialMapping.ContainsKey(materials[i]))
                            {
                                materials[i] = materialMapping[materials[i]];
                                rendererChanged = true;
                            }
                        }

                        if (rendererChanged)
                        {
                            renderer.sharedMaterials = materials;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                        Debug.Log($"Updated material references in prefab '{System.IO.Path.GetFileName(prefabPath)}'.");
                    }
                }
                finally
                {
                    if (prefabContents != null)
                    {
                        PrefabUtility.UnloadPrefabContents(prefabContents);
                    }
                }
            }

            // 郢ｧ・ｷ郢晢ｽｼ郢晢ｽｳ闕ｳ鄙ｫ繝ｻ郢ｧ・､郢晢ｽｳ郢ｧ・ｹ郢ｧ・ｿ郢晢ｽｳ郢ｧ・ｹ郢ｧ繧牙ｳｩ隰暦ｽ･隴厄ｽｴ隴・ｽｰ
            if (isSceneInstance)
            {
                Renderer[] sceneRenderers = targetPrefab.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in sceneRenderers)
                {
                    Undo.RecordObject(renderer, "Update Material References");
                    Material[] materials = renderer.sharedMaterials;
                    bool rendererChanged = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null && materialMapping.ContainsKey(materials[i]))
                        {
                            materials[i] = materialMapping[materials[i]];
                            rendererChanged = true;
                        }
                    }

                    if (rendererChanged)
                    {
                        renderer.sharedMaterials = materials;
                    }
                }

                Debug.Log($"Updated material references in scene instance '{targetPrefab.name}'.");
            }
        }

        /// <summary>
        /// 郢晏干ﾎ樒ｹ昜ｸ翫Ω郢ｧ螳夲ｽ､繝ｻ・｣・ｽ邵ｺ蜉ｱ窶ｻ郢晏・縺顔ｹ晢ｽｩ郢晢ｽｫ郢ｧ・ｭ郢晢ｽｼ邵ｺ・ｫ髴托ｽｽ陷会｣ｰ邵ｺ蜉ｱﾂ竏ｬ・､繝ｻ・｣・ｽ邵ｺ・ｮ邵ｺ・ｿ郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ郢ｧ雋橸ｽｷ・ｮ邵ｺ邇ｲ蟠帷ｸｺ蛹ｻ・狗ｸｲ繝ｻ
        /// 陷医・繝ｻ郢晏干ﾎ樒ｹ昜ｸ翫Ω/郢ｧ・､郢晢ｽｳ郢ｧ・ｹ郢ｧ・ｿ郢晢ｽｳ郢ｧ・ｹ邵ｺ・ｯ闕ｳﾂ陋ｻ繝ｻ・､逕ｻ蟲ｩ邵ｺ霈費ｽ檎ｸｺ・ｪ邵ｺ繝ｻﾂ繝ｻ
        /// </summary>
        private void DuplicateAndApplyMaterials(Dictionary<Material, Material> materialMapping)
        {
            if (targetPrefab == null || materialMapping.Count == 0) return;

            // ---- 1. 髫阪・・｣・ｽ郢ｧ蜑・ｽｽ諛医・ ----
            GameObject duplicate;
            string prefabAssetPath = AssetDatabase.GetAssetPath(targetPrefab);
            bool isProjectAsset = !string.IsNullOrEmpty(prefabAssetPath) && !targetPrefab.scene.IsValid();

            if (isProjectAsset)
            {
                // 郢晏干ﾎ溽ｹｧ・ｸ郢ｧ・ｧ郢ｧ・ｯ郢晏現縺育ｹｧ・｣郢晢ｽｳ郢晏ｳｨ縺育ｸｺ・ｮ郢晏干ﾎ樒ｹ昜ｸ翫Ω郢ｧ・｢郢ｧ・ｻ郢昴・繝ｨ 遶翫・郢ｧ・ｷ郢晢ｽｼ郢晢ｽｳ邵ｺ・ｫ郢ｧ・､郢晢ｽｳ郢ｧ・ｹ郢ｧ・ｿ郢晢ｽｳ郢ｧ・ｹ陋ｹ繝ｻ
                duplicate = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
            }
            else
            {
                // 郢ｧ・ｷ郢晢ｽｼ郢晢ｽｳ闕ｳ鄙ｫ繝ｻ郢ｧ・､郢晢ｽｳ郢ｧ・ｹ郢ｧ・ｿ郢晢ｽｳ郢ｧ・ｹ 遶翫・Instantiate邵ｺ・ｧ髫阪・・｣・ｽ
                duplicate = Object.Instantiate(targetPrefab);
                // 陷医・竊定惺蠕個ｧ髫包ｽｪ郢晢ｽｻ陷ｷ蠕個ｧ鬮ｫ荳ｻ・ｱ・､邵ｺ・ｫ鬩溷調・ｽ・ｮ
                duplicate.transform.SetParent(targetPrefab.transform.parent, false);
            }

            // 陷ｷ讎顔√郢ｧ螳夲ｽｨ・ｭ陞ｳ繝ｻ
            duplicate.name = targetPrefab.name + "_NataneToon";
            Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate {targetPrefab.name} for NataneToon Migration");

            // 陷医・竊定叉・ｦ邵ｺ・ｹ邵ｺ・ｦ雎育｢托ｽｼ繝ｻ・郢ｧ繝ｻ笘・ｸｺ繝ｻ・育ｸｺ繝ｻ竊鍋ｹｧ・ｪ郢晁ｼ斐◎郢昴・繝ｨ鬩溷調・ｽ・ｮ
            if (isProjectAsset)
            {
                // 郢晏干ﾎ樒ｹ昜ｸ翫Ω郢ｧ・｢郢ｧ・ｻ郢昴・繝ｨ邵ｺ荵晢ｽ臥ｸｺ・ｮ隴・ｽｰ髫募沁繝ｻ驗ゑｽｮ邵ｺ・ｯ郢昴・繝ｵ郢ｧ・ｩ郢晢ｽｫ郢昜ｺ包ｽｽ蜥ｲ・ｽ・ｮ邵ｺ・ｧOK
            }
            else
            {
                // 郢ｧ・ｷ郢晢ｽｼ郢晢ｽｳ郢ｧ・､郢晢ｽｳ郢ｧ・ｹ郢ｧ・ｿ郢晢ｽｳ郢ｧ・ｹ邵ｺ・ｮ髫阪・・｣・ｽ邵ｺ・ｯ隶難ｽｪ邵ｺ・ｫ陝・ｻ｣・邵ｺ螢ｹ・臥ｸｺ繝ｻ
                Vector3 offset = duplicate.transform.right * GetBoundsWidth(duplicate);
                if (offset.magnitude < 0.1f) offset = Vector3.right * 1.0f;
                duplicate.transform.position = targetPrefab.transform.position + offset;
            }

            // ---- 2. 髫阪・・｣・ｽ邵ｺ・ｮRenderer邵ｺ・ｮ郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ郢ｧ雋橸ｽｷ・ｮ邵ｺ邇ｲ蟠帷ｸｺ繝ｻ----
            Renderer[] renderers = duplicate.GetComponentsInChildren<Renderer>(true);
            int replacedCount = 0;

            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                bool rendererChanged = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materialMapping.ContainsKey(materials[i]))
                    {
                        materials[i] = materialMapping[materials[i]];
                        rendererChanged = true;
                        replacedCount++;
                    }
                }

                if (rendererChanged)
                {
                    renderer.sharedMaterials = materials;
                }
            }

            // ---- 3. 郢晏干ﾎ樒ｹ昜ｸ翫Ω郢晢ｽｪ郢晢ｽｳ郢ｧ・ｯ郢ｧ螳夲ｽｧ・｣鬮ｯ・､繝ｻ閧ｲ蟲｡驕ｶ荵晢ｼ邵ｺ貅倥′郢晄じ縺夂ｹｧ・ｧ郢ｧ・ｯ郢晏現竊鍋ｸｺ蜷ｶ・九・繝ｻ----
            if (PrefabUtility.IsPartOfPrefabInstance(duplicate))
            {
                PrefabUtility.UnpackPrefabInstance(duplicate, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            // ---- 4. 驍ｨ蜈域｣｡郢ｧ螳夲ｽｨ蛟ｬ鮖ｸ ----
            lastDuplicatedObject = duplicate;
            Selection.activeGameObject = duplicate;

            Debug.Log(L($"Created duplicate '{duplicate.name}' of '{targetPrefab.name}' in hierarchy ({replacedCount} material slots replaced).", $"Created duplicate '{duplicate.name}' of '{targetPrefab.name}' in hierarchy ({replacedCount} material slots replaced)."));
        }

        /// <summary>
        /// GameObject邵ｺ・ｮ郢晁・縺育ｹ晢ｽｳ郢昴・縺・ｹ晢ｽｳ郢ｧ・ｰ郢晄㈱繝｣郢ｧ・ｯ郢ｧ・ｹ陝ｷ繝ｻ・定愾髢・ｾ證ｦ・ｼ驛・ｽ､繝ｻ・｣・ｽ鬩溷調・ｽ・ｮ邵ｺ・ｮ郢ｧ・ｪ郢晁ｼ斐◎郢昴・繝ｨ髫ｪ閧ｲ・ｮ遉ｼ逡代・繝ｻ
        /// </summary>
        private float GetBoundsWidth(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return 1.0f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return Mathf.Max(bounds.size.x, 0.5f);
        }

        /// <summary>
        /// 陞溽判驪､郢晢ｽｬ郢晄亢繝ｻ郢晏現・堤ｹ敖郢ｧ・､郢ｧ・｢郢晢ｽｭ郢ｧ・ｰ邵ｺ・ｧ髯ｦ・ｨ驕会ｽｺ
        /// </summary>
        private void ShowConversionReport(List<ConversionReport> reports, int successCount)
        {
            StringBuilder reportText = new StringBuilder();
            reportText.AppendLine(L($"????: {reports.Count} ?? {successCount} ?????????????????", $"Conversion Complete: Successfully converted {successCount}/{reports.Count} materials."));
            reportText.AppendLine();

            int warningCount = 0;

            foreach (var report in reports)
            {
                if (!report.success)
                {
                    reportText.AppendLine($"x {report.materialName}: {L("失敗", "Failed")}");
                    foreach (var warning in report.warnings)
                    {
                        reportText.AppendLine($"   ! {warning}");
                    }
                    reportText.AppendLine();
                    continue;
                }

                bool hasWarnings = report.warnings.Count > 0;
                string statusIcon = hasWarnings ? "!" : "*";
                reportText.AppendLine($"{statusIcon} {report.materialName}");

                // 髫ｴ・ｦ陷ｻ鄙ｫ・帝勗・ｨ驕会ｽｺ
                if (hasWarnings)
                {
                    foreach (var warning in report.warnings)
                    {
                        reportText.AppendLine($"   ! {warning}");
                        warningCount++;
                    }
                }

                // 鬩･蟠趣ｽｦ竏壺・隲繝ｻ・ｰ・ｱ邵ｺ・ｮ邵ｺ・ｿ髯ｦ・ｨ驕会ｽｺ繝ｻ繝ｻutline髫ｱ・ｿ隰ｨ・ｴ邵ｺ・ｪ邵ｺ・ｩ繝ｻ繝ｻ
                if (report.outlineWidthAdjusted)
                {
                    reportText.AppendLine(L($"   > ??????????: {report.originalOutlineWidth:F2} -> {report.convertedOutlineWidth:F4}", $"   > Outline Width Adjusted: {report.originalOutlineWidth:F2} -> {report.convertedOutlineWidth:F4}"));
                }
                if (report.hasMultipleShadowLayers)
                {
                    reportText.AppendLine(L($"   i Multiple shadow layers detected (converted as multi-shadow)", $"   i Multiple shadow layers detected (converted as multi-shadow)"));
                }

                reportText.AppendLine();
            }

            reportText.AppendLine(L("=== サマリー ===", "=== Summary ==="));
            reportText.AppendLine(L($"Success: {successCount}", $"Success: {successCount}"));
            reportText.AppendLine(L($"Failed: {reports.Count - successCount}", $"Failed: {reports.Count - successCount}"));
            reportText.AppendLine(L($"Warnings: {warningCount}", $"Warnings: {warningCount}"));

            // 郢晢ｽｭ郢ｧ・ｰ邵ｺ・ｫ髫ｧ・ｳ驍擾ｽｰ郢ｧ雋槭・陷峨・
            Debug.Log("=== Detailed Conversion Report ===");
            foreach (var report in reports)
            {
                if (report.success && report.infos.Count > 0)
                {
                    Debug.Log($"[{report.materialName}]");
                    foreach (var info in report.infos)
                    {
                        Debug.Log($"  {info}");
                    }
                }
            }

            EditorUtility.DisplayDialog(
                L("変換レポート", "Conversion Report"),
                reportText.ToString(),
                "OK"
            );

            // 髫ｴ・ｦ陷ｻ鄙ｫ窶ｲ邵ｺ繧・ｽ狗ｹ晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ邵ｺ蠕娯旺郢ｧ蜿･・ｰ・ｴ陷ｷ蛹ｻ繝ｻ髴托ｽｽ陷会｣ｰ邵ｺ・ｮ驕抵ｽｺ髫ｱ髦ｪ繝郢ｧ・､郢ｧ・｢郢晢ｽｭ郢ｧ・ｰ
            if (warningCount > 0)
            {
                bool openConsole = EditorUtility.DisplayDialog(
                    L("警告を検出", "Warnings Detected"),
                    L($"{warningCount} warnings detected.\nCheck the console log for details.\n\nOpen Console?", $"{warningCount} warnings detected.\nCheck the console log for details.\n\nOpen Console?"),
                    L("コンソールを開く", "Open Console"),
                    L("閉じる", "Close")
                );

                if (openConsole)
                {
                    EditorWindow.GetWindow(System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor"));
                }
            }
        }
    }
}
