using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    /// <summary>
    /// Interactive help system for Natane Toon Shader
    /// Provides contextual help, tutorials, glossary, and troubleshooting
    /// </summary>
    public class InteractiveHelpSystem : EditorWindow
    {
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabs = new[] { "クイックスタート", "用語集", "チュートリアル", "トラブルシューティング", "ヒント" };
        private string searchQuery = "";
        private Dictionary<string, string> glossary;
        private Dictionary<string, Tutorial> tutorials;

        private class Tutorial
        {
            public string title;
            public string description;
            public List<string> steps;
            public string category;
        }

        [MenuItem("Tools/Natane/インタラクティブヘルプ", false, 70)]
        public static void ShowWindow()
        {
            var window = GetWindow<InteractiveHelpSystem>("Natane Toon ヘルプ");
            window.minSize = new Vector2(650, 500);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeGlossary();
            InitializeTutorials();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(5);
            DrawTabs();
            EditorGUILayout.Space(10);
            DrawTabContent();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Natane Toon Shader - インタラクティブヘルプ", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("すべての機能を効果的に使用する方法を学ぶ", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(25));
        }

        private void DrawTabContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawQuickStart(); break;
                case 1: DrawGlossary(); break;
                case 2: DrawTutorials(); break;
                case 3: DrawTroubleshooting(); break;
                case 4: DrawTips(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuickStart()
        {
            DrawSection("🚀 クイックスタートガイド", () =>
            {
                DrawSubSection("はじめに (5分)", () =>
                {
                    DrawStep("1", "マテリアルを作成し、'Natane/Toon Shader'またはそのバリアントを選択");
                    DrawStep("2", "マテリアルプリセットブラウザを使用（Tools > Natane > マテリアルプリセットブラウザ）");
                    DrawStep("3", "ニーズに合ったプリセットを選択（例：Character_Skin_Soft）");
                    DrawStep("4", "プリセットをマテリアルに適用");
                    DrawStep("5", "必要に応じてInspectorでパラメータを調整");
                });

                EditorGUILayout.Space(10);

                DrawSubSection("初回セットアップ", () =>
                {
                    DrawBullet("デフォルトプリセットを生成：Tools > Natane > デフォルトプリセット生成");
                    DrawBullet("マテリアルプリセットブラウザを開く：Tools > Natane > マテリアルプリセットブラウザ");
                    DrawBullet("マテリアルを検証：Tools > Natane > マテリアルバリデーター");
                });

                EditorGUILayout.Space(10);

                DrawSubSection("一般的なワークフロー", () =>
                {
                    DrawWorkflow("キャラクター作成", new[]
                    {
                        "肌にはCharacter_Skin_Softプリセットを使用",
                        "髪にはCharacter_Hair_Standardプリセットを使用",
                        "衣服にはCharacter_Clothing_Fabricを使用",
                        "目にはCharacter_Eyes_Standardを使用",
                        "色とライティングを自分のスタイルに合わせて微調整"
                    });

                    EditorGUILayout.Space(5);

                    DrawWorkflow("環境作成", new[]
                    {
                        "草にはEnvironment_Nature_Grassを使用",
                        "建物にはEnvironment_Architecture_Stoneを使用",
                        "トゥーンステップを調整してスタイライゼーションレベルを設定"
                    });
                });
            });
        }

        private void DrawGlossary()
        {
            DrawSection("📖 技術用語集", () =>
            {
                // Search bar
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("検索:", GUILayout.Width(60));
                searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("クリア", GUILayout.Width(50)))
                {
                    searchQuery = "";
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);

                foreach (var entry in glossary)
                {
                    if (string.IsNullOrEmpty(searchQuery) ||
                        entry.Key.ToLower().Contains(searchQuery.ToLower()) ||
                        entry.Value.ToLower().Contains(searchQuery.ToLower()))
                    {
                        DrawGlossaryEntry(entry.Key, entry.Value);
                    }
                }
            });
        }

        private void DrawTutorials()
        {
            DrawSection("📚 ステップバイステップチュートリアル", () =>
            {
                var categories = new[] { "初級", "中級", "上級", "VRChat" };
                var categoryMap = new Dictionary<string, string>
                {
                    { "初級", "Beginner" },
                    { "中級", "Intermediate" },
                    { "上級", "Advanced" },
                    { "VRChat", "VRChat" }
                };

                foreach (var category in categories)
                {
                    var categoryTutorials = new List<Tutorial>();
                    foreach (var tut in tutorials.Values)
                    {
                        if (tut.category == categoryMap[category])
                        {
                            categoryTutorials.Add(tut);
                        }
                    }

                    if (categoryTutorials.Count > 0)
                    {
                        DrawSubSection($"{category} チュートリアル", () =>
                        {
                            foreach (var tutorial in categoryTutorials)
                            {
                                DrawTutorial(tutorial);
                                EditorGUILayout.Space(5);
                            }
                        });
                    }
                }
            });
        }

        private void DrawTroubleshooting()
        {
            DrawSection("🔧 トラブルシューティングガイド", () =>
            {
                DrawProblemSolution(
                    "マテリアルが暗すぎるまたは明るすぎる",
                    new[]
                    {
                        "シーンライティングを確認 - Directional Lightがあることを確認",
                        "Shadow Receiveパラメータを調整 (0-1)",
                        "Light Influenceパラメータを調整 (0-1)",
                        "Shadow Colorを確認 - 明るくまたは暗くする",
                        "Lighting設定でAmbient Colorを確認"
                    });

                DrawProblemSolution(
                    "アウトラインが表示されない",
                    new[]
                    {
                        "Outline機能のチェックボックスを有効化",
                        "Outline Widthを増やす (0.1-0.2を試す)",
                        "Outline Colorをマテリアルとコントラストがつく色に変更",
                        "モデルの法線が正しいか確認",
                        "OpaqueまたはCutoutバリアントを使用していることを確認"
                    });

                DrawProblemSolution(
                    "透明マテリアルが正しくレンダリングされない",
                    new[]
                    {
                        "Transparentバリアントシェーダーを使用",
                        "Render Queueを調整 (透明度には3000を試す)",
                        "Z Writeが正しく設定されているか確認",
                        "必要に応じてCull Modeを有効/無効化",
                        "透明オブジェクトを後ろから前へソート"
                    });

                DrawProblemSolution(
                    "テクスチャがぼやけているまたはピクセル化されている",
                    new[]
                    {
                        "テクスチャのインポート設定を確認",
                        "テクスチャインポーターでMax Sizeを増やす",
                        "必要に応じてGenerate Mip Mapsを無効化",
                        "Filter Modeを確認 (Point/Bilinear/Trilinear)",
                        "テクスチャの解像度が十分か確認"
                    });

                DrawProblemSolution(
                    "パフォーマンスが遅すぎる",
                    new[]
                    {
                        "Material Validatorを使用して問題を確認",
                        "未使用の機能（キーワード）を無効化",
                        "テクスチャサイズを削減",
                        "テクスチャ圧縮を使用",
                        "マテリアルインスペクターでPerformance評価を確認",
                        "高コストな機能（SSS、Reflection、Parallax）の使用を制限"
                    });

                DrawProblemSolution(
                    "VRChatアップロードに失敗またはアバターが重すぎる",
                    new[]
                    {
                        "VRChatチェックを有効にしてMaterial Validatorを実行",
                        "テクスチャサイズを2048x2048以下に削減",
                        "テクスチャ圧縮（DXT/BC）を使用",
                        "不要なシェーダー機能を無効化",
                        "可能な場所でマテリアルを結合",
                        "テクスチャアトラスを使用してマテリアル数を削減"
                    });
            });
        }

        private void DrawTips()
        {
            DrawSection("💡 ヒントとベストプラクティス", () =>
            {
                DrawTipCategory("パフォーマンス", new[]
                {
                    "使用しない機能は無効化 - 各機能にはパフォーマンスコストがあります",
                    "Material Validatorを定期的に使用して問題を早期発見",
                    "VRではパフォーマンス評価B以上を目指す",
                    "テクスチャサイズは大きな影響 - 見た目が良い最小サイズを使用",
                    "テクスチャ圧縮（DXT/ASTC）を使用してメモリ使用量を改善"
                });

                DrawTipCategory("ワークフロー", new[]
                {
                    "プリセットから始めてカスタマイズ - 時間を節約し良好なデフォルトを確保",
                    "カスタマイズしたマテリアルを新しいプリセットとして保存して再利用",
                    "クリップボードのコピー/ペーストを使用して設定を素早く転送",
                    "マテリアルをファイルにエクスポートしてチーム共有",
                    "お気に入りの設定のライブラリを保持"
                });

                DrawTipCategory("ビジュアル品質", new[]
                {
                    "Toon Stepsはセルシェーディングを制御 - アニメ風には2-3ステップ",
                    "Toon Sharpnessは影のエッジを制御 - 高いほどシャープ",
                    "Rim Lightを使用してキャラクターを背景から際立たせる",
                    "SSS（Subsurface Scattering）は肌や葉をよりリアルに",
                    "MatCapは低コストで偽の反射を追加可能"
                });

                DrawTipCategory("VRChat固有", new[]
                {
                    "確定前にVRChatでテスト - ライティングがUnityと異なります",
                    "Editorプレウォーミングを使用 - ランタイムスクリプト不要",
                    "マテリアルあたりの総テクスチャメモリを40MB未満に保持",
                    "最小目標としてPerformance Rank Good（PC）を使用",
                    "モバイルVRを対象とする場合はQuestでテスト"
                });

                DrawTipCategory("学習", new[]
                {
                    "デフォルトプリセットを調べてパラメータの組み合わせを学習",
                    "Performanceインジケーターを使用して機能コストを理解",
                    "Material Validatorでマテリアルを比較",
                    "用語集を読んで技術用語を理解",
                    "実験！変更をテストする前にコピーを作成"
                });
            });
        }

        // UI Helper Methods
        private void DrawSection(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            content();
            EditorGUILayout.EndVertical();
        }

        private void DrawSubSection(string title, System.Action content)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            content();
            EditorGUI.indentLevel--;
        }

        private void DrawStep(string number, string text)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(number + ".", EditorStyles.boldLabel, GUILayout.Width(20));
            EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBullet(string text)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("•", EditorStyles.boldLabel, GUILayout.Width(15));
            EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawWorkflow(string workflowName, string[] steps)
        {
            EditorGUILayout.LabelField("→ " + workflowName, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var step in steps)
            {
                DrawBullet(step);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawGlossaryEntry(string term, string definition)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(term, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(definition, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private void DrawTutorial(Tutorial tutorial)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📖 " + tutorial.title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(tutorial.description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);

            for (int i = 0; i < tutorial.steps.Count; i++)
            {
                DrawStep((i + 1).ToString(), tutorial.steps[i]);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawProblemSolution(string problem, string[] solutions)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("❓ " + problem, EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Solutions:", EditorStyles.miniBoldLabel);
            foreach (var solution in solutions)
            {
                DrawBullet(solution);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawTipCategory(string category, string[] tips)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("💡 " + category, EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            foreach (var tip in tips)
            {
                DrawBullet(tip);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // Data Initialization
        private void InitializeGlossary()
        {
            glossary = new Dictionary<string, string>
            {
                { "Cel Shading / Toon Shading", "Non-photorealistic rendering technique that creates flat, cartoon-like shading with distinct color regions instead of smooth gradients." },
                { "NPR (Non-Photorealistic Rendering)", "Rendering style that intentionally avoids photorealism, used for artistic effects like cartoon, sketch, or painterly looks." },
                { "Toon Steps", "Number of discrete shading levels in cel shading. 2-3 steps creates classic anime look, more steps creates softer shading." },
                { "Toon Sharpness", "Controls how sharp the transition between shading levels is. Higher = harder edges, lower = softer transitions." },
                { "Rim Light", "Lighting effect that highlights edges of objects, making them stand out from the background. Creates a 'halo' effect." },
                { "SSS (Subsurface Scattering)", "Light penetrating and scattering beneath a surface, creating soft translucent effect. Important for skin, leaves, wax." },
                { "MatCap (Material Capture)", "Texture that encodes lighting information in a sphere, providing cheap fake reflections and lighting." },
                { "Fresnel Effect", "Optical phenomenon where reflections become stronger at glancing angles. Makes edges more reflective than flat surfaces." },
                { "Parallax Occlusion Mapping (POM)", "Technique that simulates depth on a flat surface using height maps, creating 3D relief without actual geometry." },
                { "Cubemap / Environment Map", "360-degree texture used for reflections and environment lighting, captured from or representing surroundings." },
                { "Refraction", "Bending of light as it passes through transparent materials like glass or water." },
                { "IOR (Index of Refraction)", "Number that describes how much light bends when entering a material. Glass ≈ 1.5, Water ≈ 1.33, Diamond ≈ 2.4." },
                { "Specular Highlight", "Bright spot of reflected light on shiny surfaces, representing direct reflection of light source." },
                { "Shader Keyword", "Compiler directive that includes/excludes code blocks, allowing features to be toggled without performance cost when disabled." },
                { "Render Queue", "Order in which materials are rendered. Opaque (2000), AlphaTest (2450), Transparent (3000). Lower renders first." },
                { "Cull Mode", "Which face of triangles to render. Back (default) = front facing only, Front = back facing only, Off = both sides." },
                { "Z Write", "Whether to write depth information. Usually on for opaque, off for transparent objects." },
                { "Shader Variant", "Specific compiled version of shader with certain features enabled/disabled. Each keyword combination creates a variant." },
                { "GPU Instancing", "Technique to render multiple copies of same mesh/material with single draw call, greatly improving performance." },
                { "Emission", "Self-illumination of material, making it glow. Can be HDR (>1.0) for bloom effects." },
                { "Normal Map / Bump Map", "Texture that encodes surface detail information, creating illusion of bumps and grooves without actual geometry." },
                { "Mask Texture", "Grayscale texture controlling where/how much an effect applies. White = full effect, black = no effect, gray = partial." },
                { "Dissolve Effect", "Animated transition that makes objects appear/disappear using noise patterns, common in VR avatar systems." },
                { "Hue Shift", "Color manipulation that rotates colors around the color wheel, allowing dynamic recoloring." }
            };
        }

        private void InitializeTutorials()
        {
            tutorials = new Dictionary<string, Tutorial>
            {
                {
                    "basic_setup",
                    new Tutorial
                    {
                        title = "Basic Material Setup",
                        category = "Beginner",
                        description = "Create your first toon material from scratch",
                        steps = new List<string>
                        {
                            "Create a new material (Right-click > Create > Material)",
                            "Select the shader: Natane/Toon Shader (or variant)",
                            "Assign a Main Texture (optional but recommended)",
                            "Set Main Color to your desired base color",
                            "Adjust Shadow Color to match your art style",
                            "Set Toon Steps to 2 for anime look",
                            "Apply to your model and test in Scene view"
                        }
                    }
                },
                {
                    "using_presets",
                    new Tutorial
                    {
                        title = "Using Material Presets",
                        category = "Beginner",
                        description = "Quickest way to get professional results",
                        steps = new List<string>
                        {
                            "Run: Tools > Natane > Generate Default Presets (first time only)",
                            "Open: Tools > Natane > Material Preset Browser",
                            "Select your material in the Target Material field",
                            "Browse presets - use Category filter to narrow down",
                            "Click Apply on a preset that fits your needs",
                            "Fine-tune parameters in Material Inspector",
                            "Save as new preset if you want to reuse these settings"
                        }
                    }
                },
                {
                    "character_skin",
                    new Tutorial
                    {
                        title = "Creating Realistic Skin Material",
                        category = "Intermediate",
                        description = "Set up skin with SSS for believable character appearance",
                        steps = new List<string>
                        {
                            "Start with Character_Skin_Soft preset",
                            "Enable SSS (Subsurface Scattering) feature",
                            "Set SSS Color to reddish-orange (simulates blood under skin)",
                            "Adjust SSS Intensity (0.3-0.6 works well)",
                            "Enable Rim Light for edge definition",
                            "Set Rim Color to slightly brighter than skin",
                            "Add subtle Specular if skin should be slightly shiny",
                            "Test under different lighting conditions"
                        }
                    }
                },
                {
                    "sharing_params",
                    new Tutorial
                    {
                        title = "Sharing Material Parameters",
                        category = "Beginner",
                        description = "Collaborate with team members",
                        steps = new List<string>
                        {
                            "Select material you want to share",
                            "Click 'Export to File' in Material Inspector",
                            "Choose location and save as .ntmaterial file",
                            "Share file with team (email, Slack, Git, etc.)",
                            "Team member opens Material Preset Browser",
                            "Click 'Import from File' and select the .ntmaterial",
                            "Apply to their material - done!"
                        }
                    }
                },
                {
                    "vrchat_optimization",
                    new Tutorial
                    {
                        title = "VRChat Optimization",
                        category = "VRChat",
                        description = "Optimize materials for VRChat performance",
                        steps = new List<string>
                        {
                            "Add all materials to Material Validator",
                            "Enable VRChat Optimization check",
                            "Click Validate All",
                            "Review errors (red) and warnings (yellow)",
                            "Use Auto-Fix for fixable issues",
                            "Manually resize textures flagged as too large",
                            "Disable unused features to reduce variants",
                            "Re-validate until only green/info messages remain",
                            "Use Tools > Natane > Shader Prewarming > Settings"
                        }
                    }
                },
                {
                    "glass_effect",
                    new Tutorial
                    {
                        title = "Creating Glass Material",
                        category = "Advanced",
                        description = "Realistic glass with refraction and reflection",
                        steps = new List<string>
                        {
                            "Use Transparent shader variant",
                            "Start with Effects_Glass_Clear preset",
                            "Enable Refraction feature",
                            "Set Refraction Index to 1.5 (glass)",
                            "Enable Reflection feature",
                            "Assign reflection cubemap",
                            "Set Smoothness to 0.95-1.0",
                            "Add subtle Specular for highlights",
                            "Adjust alpha to control transparency",
                            "Test with objects behind glass"
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Show contextual help for a specific topic
        /// Can be called from other editor scripts
        /// </summary>
        public static void ShowHelp(string topic)
        {
            var window = GetWindow<InteractiveHelpSystem>("Natane Toon Help");
            window.searchQuery = topic;
            window.selectedTab = 1; // Glossary tab
            window.Show();
        }

        /// <summary>
        /// Show tutorial for a specific feature
        /// Can be called from other editor scripts
        /// </summary>
        public static void ShowTutorial(string tutorialId)
        {
            var window = GetWindow<InteractiveHelpSystem>("Natane Toon Help");
            window.selectedTab = 2; // Tutorials tab
            window.Show();
        }
    }

    /// <summary>
    /// Provides helper methods for displaying help buttons in other editor windows
    /// </summary>
    public static class HelpButton
    {
        public static void Draw(string topic, float width = 30)
        {
            if (GUILayout.Button("?", GUILayout.Width(width), GUILayout.Height(18)))
            {
                InteractiveHelpSystem.ShowHelp(topic);
            }
        }

        public static void DrawInline(string tooltip, float width = 20)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.6f, 1f) }
            };

            if (GUILayout.Button(new GUIContent("?", tooltip), style, GUILayout.Width(width), GUILayout.Height(18)))
            {
                InteractiveHelpSystem.ShowHelp(tooltip);
            }
        }
    }
}
