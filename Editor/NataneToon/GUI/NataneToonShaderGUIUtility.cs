using UnityEngine;
using UnityEditor;
using NataneToon.MaterialSystem;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    /// <summary>
    /// Utility class for Natane Toon Shader GUI operations
    /// Provides reusable UI components and helper functions
    /// Natane Toon Shader GUI操作用のユーティリティクラス
    /// 再利用可能なUIコンポーネントとヘルパー関数を提供
    /// </summary>
    public static class NataneToonShaderGUIUtility
    {
        private static GUIStyle headerStyle;
        private static GUIStyle boxStyle;
        private static readonly string[] PerformanceKeywords =
        {
            "_SPECULAR", "_RIM_LIGHT", "_SSS", "_MATCAP", "_OUTLINE", "_EMISSION",
            "_DISSOLVE", "_HUE_SHIFT",
            "_REFLECTION", "_ENV_RIM", "_PARALLAX", "_REFRACTION",
            "_IRIDESCENCE", "_GLITTER", "_MATCAP_2", "_MATCAP_3",
            "_AUDIOLINK", "_HOLOGRAM", "_GLITCH", "_DECAL",
            "_VAT", "_VERTEX_ANIMATION", "_PIXEL_VERTEX_LIGHTS",
            "_NORMALMAP"
        };

        /// <summary>
        /// Initialize styles
        /// スタイルを初期化
        /// </summary>
        public static void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    padding = new RectOffset(5, 5, 5, 5)
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        /// <summary>
        /// Draw a section header with foldout
        /// 折りたたみ機能付きセクションヘッダーを描画
        /// </summary>
        public static bool DrawFoldoutHeader(string title, bool foldout)
        {
            InitializeStyles();
            EditorGUILayout.Space(5);
            return EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
        }

        /// <summary>
        /// Draw a property with optional tooltip
        /// オプションのツールチップ付きでプロパティを描画
        /// </summary>
        public static void DrawProperty(MaterialEditor editor, MaterialProperty prop, string label = null, string tooltip = null)
        {
            GUIContent content;
            if (string.IsNullOrEmpty(label))
            {
                content = new GUIContent(prop.displayName, tooltip ?? "");
            }
            else
            {
                content = new GUIContent(label, tooltip ?? "");
            }

            editor.ShaderProperty(prop, content);
        }

        /// <summary>
        /// Draw a color property with HDR support check
        /// HDRサポートチェック付きでカラープロパティを描画
        /// </summary>
        public static void DrawColorProperty(MaterialEditor editor, MaterialProperty prop, string label = null, string tooltip = null, bool hdr = false)
        {
            GUIContent content;
            if (string.IsNullOrEmpty(label))
            {
                content = new GUIContent(prop.displayName, tooltip ?? "");
            }
            else
            {
                content = new GUIContent(label, tooltip ?? "");
            }

            if (hdr)
            {
                editor.ColorProperty(prop, content.text);
            }
            else
            {
                Color oldColor = prop.colorValue;
                Color newColor = EditorGUILayout.ColorField(content, oldColor);
                if (newColor != oldColor)
                {
                    prop.colorValue = newColor;
                }
            }
        }

        /// <summary>
        /// Draw a toggle with keyword enable/disable
        /// キーワードの有効/無効化機能付きトグルを描画
        /// </summary>
        public static bool DrawToggleWithKeyword(Material material, MaterialProperty prop, string keyword, string label = null, string tooltip = null)
        {
            GUIContent content;
            if (string.IsNullOrEmpty(label))
            {
                content = new GUIContent(prop.displayName, tooltip ?? "");
            }
            else
            {
                content = new GUIContent(label, tooltip ?? "");
            }

            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle(content, material.IsKeywordEnabled(keyword));

            if (EditorGUI.EndChangeCheck())
            {
                if (enabled)
                {
                    material.EnableKeyword(keyword);
                    prop.floatValue = 1f;
                }
                else
                {
                    material.DisableKeyword(keyword);
                    prop.floatValue = 0f;
                }
            }

            return enabled;
        }

        /// <summary>
        /// Draw help button for tool window
        /// ツールウィンドウ用のヘルプボタンを描画
        /// </summary>
        public static void DrawHelpButton(string toolKey)
        {
            if (GUILayout.Button(L("❓ ヘルプ", "❓ Help"), GUILayout.Width(100), GUILayout.Height(25)))
            {
                UnifiedHelpSystem.ShowToolHelp(toolKey);
            }
        }

        /// <summary>
        /// Draw header with help button
        /// ヘルプボタン付きヘッダーを描画
        /// </summary>
        public static void DrawHeaderWithHelp(string titleJP, string titleEN, string toolKey)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L(titleJP, titleEN), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawLanguageToggleButton();
            DrawHelpButton(toolKey);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw texture property with tiling/offset
        /// タイリング/オフセット付きテクスチャプロパティを描画
        /// </summary>
        public static void DrawTextureProperty(MaterialEditor editor, MaterialProperty texProp, bool showScaleOffset = true)
        {
            editor.TexturePropertySingleLine(new GUIContent(texProp.displayName), texProp);

            if (showScaleOffset && texProp.textureValue != null)
            {
                EditorGUI.indentLevel++;
                editor.TextureScaleOffsetProperty(texProp);
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draw a separator line
        /// </summary>
        public static void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Draw a category divider with centered label
        /// カテゴリ区切り線（中央ラベル付き）
        /// </summary>
        public static void DrawCategoryDivider(string label)
        {
            EditorGUILayout.Space(8);

            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            float lineY = rect.y + rect.height * 0.5f;

            // Determine colors based on theme
            bool isDark = EditorGUIUtility.isProSkin;
            Color lineColor = isDark ? new Color(0.5f, 0.5f, 0.5f, 0.4f) : new Color(0.3f, 0.3f, 0.3f, 0.3f);
            Color textColor = isDark ? new Color(0.6f, 0.6f, 0.6f, 0.8f) : new Color(0.4f, 0.4f, 0.4f, 0.8f);

            // Measure text width
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor },
                fontSize = 10
            };
            GUIContent content = new GUIContent(label);
            float textWidth = labelStyle.CalcSize(content).x + 16; // padding

            float centerX = rect.x + rect.width * 0.5f;
            float halfTextWidth = textWidth * 0.5f;

            // Draw left line
            EditorGUI.DrawRect(new Rect(rect.x, lineY, centerX - halfTextWidth - rect.x, 1), lineColor);

            // Draw right line
            EditorGUI.DrawRect(new Rect(centerX + halfTextWidth, lineY, rect.x + rect.width - centerX - halfTextWidth, 1), lineColor);

            // Draw label
            EditorGUI.LabelField(rect, label, labelStyle);

            EditorGUILayout.Space(4);
        }

        /// <summary>
        /// Draw a help box with icon
        /// </summary>
        public static void DrawHelpBox(string message, MessageType type = MessageType.Info)
        {
            EditorGUILayout.HelpBox(message, type);
        }

        /// <summary>
        /// Draw preset and sharing buttons
        /// </summary>
        public static void DrawMaterialActionsToolbar(Material material, MaterialEditor editor)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("マテリアル操作", "Material Actions"), EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // Preset Browser
            if (GUILayout.Button(new GUIContent(
                L("プリセットブラウザ", "Preset Browser"),
                L("マテリアルプリセットブラウザを開く", "Open Material Preset Browser")),
                GUILayout.Height(25)))
            {
                MaterialPresetBrowser.ShowWindow();
            }

            // Create Preset
            if (GUILayout.Button(new GUIContent(
                L("プリセットとして保存", "Save as Preset"),
                L("このマテリアルからプリセットを作成", "Create preset from this material")),
                GUILayout.Height(25)))
            {
                CreatePresetFromMaterial(material);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // Export to File
            if (GUILayout.Button(new GUIContent(
                L("ファイルへ書き出し", "Export to File"),
                L("パラメータをファイルへ書き出す", "Export parameters to file")),
                GUILayout.Height(25)))
            {
                ExportMaterialToFile(material);
            }

            // Copy to Clipboard
            if (GUILayout.Button(new GUIContent(
                L("コピー", "Copy"),
                L("パラメータをクリップボードへコピー", "Copy parameters to clipboard")),
                GUILayout.Height(25)))
            {
                CopyToClipboard(material);
            }

            // Paste from Clipboard
            bool clipboardValid = MaterialParameterShareSystem.IsClipboardValid();
            using (new EditorGUI.DisabledScope(!clipboardValid))
            {
                if (GUILayout.Button(new GUIContent(
                    L("貼り付け", "Paste"),
                    L("クリップボードからパラメータを貼り付け", "Paste parameters from clipboard")),
                    GUILayout.Height(25)))
                {
                    PasteFromClipboard(material);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Create preset from material
        /// </summary>
        private static void CreatePresetFromMaterial(Material material)
        {
            string defaultName = $"{material.name}_Preset";
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Preset from Material",
                defaultName,
                "asset",
                "Choose where to save the preset");

            if (!string.IsNullOrEmpty(path))
            {
                var preset = ScriptableObject.CreateInstance<NataneToonMaterialPreset>();
                preset.presetName = System.IO.Path.GetFileNameWithoutExtension(path);
                preset.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd");
                preset.CreateFromMaterial(material);

                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorGUIUtility.PingObject(preset);

                EditorUtility.DisplayDialog(
                    "Preset Created",
                    $"Created preset '{preset.presetName}' from material '{material.name}'\n\n" +
                    "You can now share this preset file with others!",
                    "OK");
            }
        }

        /// <summary>
        /// Export material to file
        /// </summary>
        private static void ExportMaterialToFile(Material material)
        {
            string defaultFolder = MaterialParameterShareSystem.GetDefaultExportFolder();
            string defaultName = $"{material.name}_Parameters";
            string extension = MaterialParameterShareSystem.GetFileExtension();

            string path = EditorUtility.SaveFilePanel(
                "Export Material Parameters",
                defaultFolder,
                defaultName,
                extension.TrimStart('.'));

            if (!string.IsNullOrEmpty(path))
            {
                string result = MaterialParameterShareSystem.ExportToFile(material, path);
                if (!string.IsNullOrEmpty(result))
                {
                    EditorUtility.DisplayDialog(
                        "Export Successful",
                        $"Material parameters exported to:\n{result}\n\n" +
                        "Share this file with others to transfer settings!",
                        "OK");
                }
            }
        }

        /// <summary>
        /// Copy material to clipboard
        /// </summary>
        private static void CopyToClipboard(Material material)
        {
            bool success = MaterialParameterShareSystem.CopyToClipboard(material);
            if (success)
            {
                EditorUtility.DisplayDialog(
                    "Copied to Clipboard",
                    $"Material '{material.name}' parameters copied to clipboard.\n\n" +
                    "You can now paste these parameters to another material or share with others.",
                    "OK");
            }
        }

        /// <summary>
        /// Paste material from clipboard
        /// </summary>
        private static void PasteFromClipboard(Material material)
        {
            var info = MaterialParameterShareSystem.GetClipboardInfo();
            bool proceed = EditorUtility.DisplayDialog(
                "Paste Material Parameters",
                $"Paste parameters from:\n\n" +
                $"Material: {info.materialName}\n" +
                $"Exported by: {info.exportedBy}\n" +
                $"Export date: {info.exportDate}\n" +
                $"Notes: {info.notes}\n\n" +
                $"This will overwrite current settings of '{material.name}'",
                "Paste",
                "Cancel");

            if (proceed)
            {
                Undo.RecordObject(material, "Paste Material Parameters");
                bool success = MaterialParameterShareSystem.PasteFromClipboard(material);

                if (success)
                {
                    EditorUtility.SetDirty(material);
                    EditorUtility.DisplayDialog(
                        "Paste Successful",
                        $"Material parameters pasted to '{material.name}'",
                        "OK");
                }
            }
        }

        /// <summary>
        /// Draw a property with inline toggle
        /// </summary>
        public static void DrawPropertyWithToggle(MaterialEditor editor, MaterialProperty toggleProp, MaterialProperty valueProp, string label, string tooltip = null)
        {
            EditorGUILayout.BeginHorizontal();

            // Toggle
            bool enabled = toggleProp.floatValue > 0.5f;
            enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(15));
            toggleProp.floatValue = enabled ? 1f : 0f;

            // Property
            using (new EditorGUI.DisabledScope(!enabled))
            {
                DrawProperty(editor, valueProp, label, tooltip);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Get MaterialProperty by name safely
        /// </summary>
        public static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool optional = false)
        {
            foreach (MaterialProperty prop in properties)
            {
                if (prop.name == propertyName)
                {
                    return prop;
                }
            }

            if (!optional)
            {
                throw new System.ArgumentException($"Could not find MaterialProperty: '{propertyName}'");
            }

            return null;
        }

        /// <summary>
        /// Validate material shader compatibility
        /// </summary>
        public static bool ValidateShaderCompatibility(Material material)
        {
            if (material == null || material.shader == null)
            {
                return false;
            }

            string shaderName = material.shader.name;
            return shaderName.Contains("Natane") && shaderName.Contains("Toon");
        }

        /// <summary>
        /// Draw performance indicator
        /// </summary>
        public static void DrawPerformanceIndicator(Material material)
        {
            int activeFeatures = CountActiveFeatures(material);
            string rating = GetPerformanceRating(activeFeatures);
            Color ratingColor = GetRatingColor(activeFeatures);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(L("パフォーマンス評価", "Performance Rating") + ":", GUILayout.Width(130));

            Color oldColor = GUI.color;
            GUI.color = ratingColor;
            EditorGUILayout.LabelField(rating, EditorStyles.boldLabel);
            GUI.color = oldColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                $"{L("有効機能数", "Active Features")}: {activeFeatures}",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"{L("推定コスト", "Estimated Cost")}: {GetEstimatedCostLabel(activeFeatures)}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        public static void DrawCompactPerformanceSummary(Material material)
        {
            DrawCompactPerformanceSummary(material, NataneToonSamplerBudgetEstimator.Estimate(material));
        }

        public static void DrawCompactPerformanceSummary(Material material, NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate samplerBudget)
        {
            if (material == null)
            {
                return;
            }

            int activeFeatures = CountActiveFeatures(material);
            string rating = GetPerformanceRating(activeFeatures);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L("パフォーマンス", "Performance"), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                $"{L("Sampler", "Sampler")}: {samplerBudget.EstimatedSamplers}/{samplerBudget.Limit}",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                $"{L("機能", "Features")}: {activeFeatures} / {L("評価", "Rating")}: {rating} / {L("状態", "Status")}: {GetSamplerBudgetStatus(samplerBudget)}",
                EditorStyles.wordWrappedMiniLabel);
            DrawSamplerBudgetBar(samplerBudget);
            EditorGUILayout.EndVertical();
        }

        private static int CountActiveFeatures(Material material)
        {
            int count = 0;

            foreach (string keyword in PerformanceKeywords)
            {
                if (material.IsKeywordEnabled(keyword))
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetPerformanceRating(int featureCount)
        {
            if (featureCount <= 3) return L("軽量 (A)", "Excellent (A)");
            if (featureCount <= 6) return L("良好 (B)", "Good (B)");
            if (featureCount <= 9) return L("標準 (C)", "Fair (C)");
            return L("重い (D)", "Heavy (D)");
        }

        private static string GetEstimatedCostLabel(int featureCount)
        {
            if (featureCount <= 3) return L("低", "Low");
            if (featureCount <= 6) return L("中", "Medium");
            if (featureCount <= 9) return L("やや高い", "Moderate");
            return L("高い", "High");
        }

        private static Color GetRatingColor(int featureCount)
        {
            if (featureCount <= 3) return Color.green;
            if (featureCount <= 6) return Color.cyan;
            if (featureCount <= 9) return Color.yellow;
            return new Color(1f, 0.5f, 0f); // Orange
        }

        public static void DrawPerformanceIndicatorWithSamplerBudget(Material material)
        {
            DrawPerformanceIndicatorWithSamplerBudget(material, NataneToonSamplerBudgetEstimator.Estimate(material));
        }

        public static void DrawPerformanceIndicatorWithSamplerBudget(Material material, NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate samplerBudget)
        {
            DrawPerformanceIndicator(material);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(L("推定 Sampler 負荷", "Estimated Sampler Budget"), EditorStyles.boldLabel);
            DrawSamplerBudgetBar(samplerBudget);
            EditorGUILayout.LabelField(
                $"{L("ベース", "Base")}: {samplerBudget.BaseSamplers} / {L("追加", "Optional")}: {samplerBudget.OptionalSamplers}",
                EditorStyles.miniLabel);

            string contributorSummary = GetContributorSummary(samplerBudget, 4);
            if (!string.IsNullOrEmpty(contributorSummary))
            {
                EditorGUILayout.LabelField(
                    $"{L("主な負荷", "Top Contributors")}: {contributorSummary}",
                    EditorStyles.wordWrappedMiniLabel);
            }

            if (samplerBudget.IsOverLimit)
            {
                EditorGUILayout.HelpBox(
                    L(
                        $"推定 Sampler 数が上限を超えています ({samplerBudget.EstimatedSamplers}/{samplerBudget.Limit})。見た目は保持されますが、新しい重い機能は有効化できません。",
                        $"Estimated sampler usage is over the limit ({samplerBudget.EstimatedSamplers}/{samplerBudget.Limit}). The current look is preserved, but new heavy features cannot be enabled."),
                    MessageType.Warning);
            }
            else if (samplerBudget.IsNearLimit)
            {
                EditorGUILayout.HelpBox(
                    L(
                        $"推定 Sampler 数が上限付近です ({samplerBudget.EstimatedSamplers}/{samplerBudget.Limit})。重い機能を追加する前に構成を見直すのが安全です。",
                        $"Estimated sampler usage is close to the limit ({samplerBudget.EstimatedSamplers}/{samplerBudget.Limit}). Review the current setup before enabling more heavy features."),
                    MessageType.Warning);
            }
            else if (samplerBudget.IsWarning)
            {
                EditorGUILayout.HelpBox(
                    L(
                        $"推定 Sampler 数は注意域です ({samplerBudget.EstimatedSamplers}/{samplerBudget.Limit})。Light Volume や LTCGI などの追加時は上限に注意してください。",
                        $"Estimated sampler usage is in the caution range ({samplerBudget.EstimatedSamplers}/{samplerBudget.Limit}). Be careful when adding features such as Light Volume or LTCGI."),
                    MessageType.Info);
            }

            if (samplerBudget.HasCriticalLightingCombo)
            {
                EditorGUILayout.HelpBox(
                    L(
                        "Light Volume + LTCGI + ハッチング は Sampler 使用数が急増しやすい組み合わせです。D3D11 では特に注意してください。",
                        "Light Volume + LTCGI + Hatching is a high-risk sampler combination. Be especially careful on D3D11."),
                    MessageType.Warning);
            }
            else if (samplerBudget.HasLightVolumeLtcgiCombo)
            {
                EditorGUILayout.HelpBox(
                    L(
                        "Light Volume と LTCGI の同時使用は Sampler 上限に近づきやすいです。他の重い機能と併用する場合は注意してください。",
                        "Using Light Volume and LTCGI together can quickly approach the sampler limit. Be careful when combining them with other heavy features."),
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawSamplerBudgetBar(NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate estimate)
        {
            Rect rect = GUILayoutUtility.GetRect(18, 18f);
            float fill = Mathf.Clamp01((float)estimate.EstimatedSamplers / estimate.Limit);
            Color color = GetSamplerBudgetColor(estimate);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.45f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * fill, rect.height), color);
            }

            EditorGUI.ProgressBar(
                rect,
                fill,
                $"{estimate.EstimatedSamplers} / {estimate.Limit} ({GetSamplerBudgetStatus(estimate)})");
        }

        private static Color GetSamplerBudgetColor(NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate estimate)
        {
            if (estimate.IsOverLimit) return new Color(0.86f, 0.42f, 0.18f);
            if (estimate.IsNearLimit) return new Color(0.95f, 0.65f, 0.2f);
            if (estimate.IsWarning) return new Color(0.95f, 0.82f, 0.24f);
            return new Color(0.32f, 0.78f, 0.44f);
        }

        private static string GetSamplerBudgetStatus(NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate estimate)
        {
            if (estimate.IsOverLimit) return L("上限超過", "Over Limit");
            if (estimate.IsNearLimit) return L("上限付近", "Near Limit");
            if (estimate.IsWarning) return L("注意", "Caution");
            return L("安全", "Safe");
        }

        private static string GetContributorSummary(NataneToonSamplerBudgetEstimator.SamplerBudgetEstimate estimate, int maxCount)
        {
            if (estimate.Contributors == null || estimate.Contributors.Length == 0 || maxCount <= 0)
            {
                return string.Empty;
            }

            int count = Mathf.Min(maxCount, estimate.Contributors.Length);
            string[] parts = new string[count];
            for (int i = 0; i < count; i++)
            {
                parts[i] = NataneToonSamplerBudgetEstimator.FormatContributor(estimate.Contributors[i]);
            }

            return string.Join(" / ", parts);
        }

        /// <summary>
        /// Draw unified tool header with help button
        /// ヘルプボタン付き統一ツールヘッダーを描画
        /// </summary>
        public static void DrawToolHeader(string titleJP, string titleEN, string toolKey)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField(L(titleJP, titleEN), titleStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            DrawLanguageToggleButton();
            DrawHelpButton(toolKey);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw JP/EN language toggle button
        /// JP/EN 言語切り替えボタンを描画
        /// </summary>
        public static void DrawLanguageToggleButton()
        {
            string label = NataneToonLocalization.IsJapanese ? "EN" : "JP";
            if (GUILayout.Button(label, GUILayout.Width(35), GUILayout.Height(25)))
            {
                NataneToonLocalization.ToggleLanguage();
            }
        }

        /// <summary>
        /// Draw primary action button
        /// プライマリアクションボタンを描画
        /// </summary>
        public static bool DrawPrimaryButton(string label, float width = 150, float height = 30)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            return GUILayout.Button(label, style, GUILayout.Width(width), GUILayout.Height(height));
        }

        /// <summary>
        /// Draw secondary action button
        /// セカンダリアクションボタンを描画
        /// </summary>
        public static bool DrawSecondaryButton(string label, float width = 150, float height = 25)
        {
            return GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(height));
        }

        /// <summary>
        /// Draw foldout section with unified style
        /// 統一スタイルのフォールドアウトセクションを描画
        /// </summary>
        public static bool DrawFoldoutSection(string title, bool foldout, System.Action content)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
            if (foldout)
            {
                EditorGUI.indentLevel++;
                content?.Invoke();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            return foldout;
        }

        /// <summary>
        /// Draw success message box
        /// 成功メッセージボックスを描画
        /// </summary>
        public static void DrawSuccessBox(string message)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = NataneToonColorPalette.Success;
            EditorGUILayout.HelpBox(message, MessageType.Info);
            GUI.backgroundColor = oldColor;
        }

        /// <summary>
        /// Draw warning message box
        /// 警告メッセージボックスを描画
        /// </summary>
        public static void DrawWarningBox(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        /// <summary>
        /// Draw error message box
        /// エラーメッセージボックスを描画
        /// </summary>
        public static void DrawErrorBox(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Error);
        }
    }
}

