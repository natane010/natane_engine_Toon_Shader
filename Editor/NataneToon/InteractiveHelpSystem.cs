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
        private string[] tabs = new[] { "Quick Start", "Glossary", "Tutorials", "Troubleshooting", "Tips" };
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

        [MenuItem("Tools/Natane/Interactive Help", false, 70)]
        public static void ShowWindow()
        {
            var window = GetWindow<InteractiveHelpSystem>("Natane Toon Help");
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
            EditorGUILayout.LabelField("Natane Toon Shader - Interactive Help", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Learn how to use all features effectively", EditorStyles.miniLabel);
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
            DrawSection("🚀 Quick Start Guide", () =>
            {
                DrawSubSection("Getting Started (5 minutes)", () =>
                {
                    DrawStep("1", "Create a material and select 'Natane/Toon Shader' or one of its variants");
                    DrawStep("2", "Use Material Preset Browser (Tools > Natane > Material Preset Browser)");
                    DrawStep("3", "Select a preset that matches your needs (e.g., Character_Skin_Soft)");
                    DrawStep("4", "Apply the preset to your material");
                    DrawStep("5", "Adjust parameters as needed in the Inspector");
                });

                EditorGUILayout.Space(10);

                DrawSubSection("First Time Setup", () =>
                {
                    DrawBullet("Generate default presets: Tools > Natane > Generate Default Presets");
                    DrawBullet("Open Material Preset Browser: Tools > Natane > Material Preset Browser");
                    DrawBullet("Validate materials: Tools > Natane > Material Validator");
                });

                EditorGUILayout.Space(10);

                DrawSubSection("Common Workflows", () =>
                {
                    DrawWorkflow("Character Creation", new[]
                    {
                        "Use Character_Skin_Soft preset for skin",
                        "Use Character_Hair_Standard preset for hair",
                        "Use Character_Clothing_Fabric for clothes",
                        "Use Character_Eyes_Standard for eyes",
                        "Fine-tune colors and lighting to match your style"
                    });

                    EditorGUILayout.Space(5);

                    DrawWorkflow("Environment Creation", new[]
                    {
                        "Use Environment_Nature_Grass for grass",
                        "Use Environment_Architecture_Stone for buildings",
                        "Adjust toon steps for stylization level"
                    });
                });
            });
        }

        private void DrawGlossary()
        {
            DrawSection("📖 Technical Glossary", () =>
            {
                // Search bar
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
                searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
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
            DrawSection("📚 Step-by-Step Tutorials", () =>
            {
                var categories = new[] { "Beginner", "Intermediate", "Advanced", "VRChat" };

                foreach (var category in categories)
                {
                    var categoryTutorials = new List<Tutorial>();
                    foreach (var tut in tutorials.Values)
                    {
                        if (tut.category == category)
                        {
                            categoryTutorials.Add(tut);
                        }
                    }

                    if (categoryTutorials.Count > 0)
                    {
                        DrawSubSection($"{category} Tutorials", () =>
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
            DrawSection("🔧 Troubleshooting Guide", () =>
            {
                DrawProblemSolution(
                    "Material appears too dark or too bright",
                    new[]
                    {
                        "Check your scene lighting - ensure you have a Directional Light",
                        "Adjust Shadow Receive parameter (0-1)",
                        "Adjust Light Influence parameter (0-1)",
                        "Check Shadow Color - make it lighter or darker",
                        "Verify Ambient Color in Lighting settings"
                    });

                DrawProblemSolution(
                    "Outline is not visible",
                    new[]
                    {
                        "Enable the Outline feature checkbox",
                        "Increase Outline Width (try 0.1-0.2)",
                        "Change Outline Color to contrast with material",
                        "Check model's normals are correct",
                        "Ensure you're using Opaque or Cutout variant"
                    });

                DrawProblemSolution(
                    "Transparent materials render incorrectly",
                    new[]
                    {
                        "Use the Transparent variant shader",
                        "Adjust Render Queue (try 3000 for transparency)",
                        "Check Z Write is set correctly",
                        "Enable/disable Cull Mode as needed",
                        "Sort transparent objects back-to-front"
                    });

                DrawProblemSolution(
                    "Textures look blurry or pixelated",
                    new[]
                    {
                        "Check texture import settings",
                        "Increase Max Size in texture importer",
                        "Disable Generate Mip Maps if needed",
                        "Check Filter Mode (Point/Bilinear/Trilinear)",
                        "Verify texture is high enough resolution"
                    });

                DrawProblemSolution(
                    "Performance is too slow",
                    new[]
                    {
                        "Use Material Validator to check issues",
                        "Disable unused features (keywords)",
                        "Reduce texture sizes",
                        "Use texture compression",
                        "Check Performance rating in material inspector",
                        "Limit use of expensive features (SSS, Reflection, Parallax)"
                    });

                DrawProblemSolution(
                    "VRChat upload fails or avatar is too heavy",
                    new[]
                    {
                        "Run Material Validator with VRChat checks",
                        "Reduce texture sizes to 2048x2048 or lower",
                        "Use texture compression (DXT/BC)",
                        "Disable unnecessary shader features",
                        "Combine materials where possible",
                        "Use texture atlases to reduce material count"
                    });
            });
        }

        private void DrawTips()
        {
            DrawSection("💡 Tips & Best Practices", () =>
            {
                DrawTipCategory("Performance", new[]
                {
                    "Disable features you don't use - each enabled feature has a performance cost",
                    "Use Material Validator regularly to catch issues early",
                    "Aim for Performance Rating B or better for VR",
                    "Texture size has huge impact - use smallest size that looks good",
                    "Use texture compression (DXT/ASTC) for better memory usage"
                });

                DrawTipCategory("Workflow", new[]
                {
                    "Start with presets, then customize - saves time and ensures good defaults",
                    "Save your customized materials as new presets for reuse",
                    "Use clipboard copy/paste to quickly transfer settings",
                    "Export materials to files for team sharing",
                    "Keep a library of your favorite settings"
                });

                DrawTipCategory("Visual Quality", new[]
                {
                    "Toon Steps controls cel-shading - 2-3 steps for anime look",
                    "Toon Sharpness controls shadow edges - higher = sharper",
                    "Use Rim Light to make characters pop from background",
                    "SSS (Subsurface Scattering) makes skin and leaves more realistic",
                    "MatCap can add fake reflections cheaply"
                });

                DrawTipCategory("VRChat Specific", new[]
                {
                    "Test in VRChat before finalizing - lighting differs from Unity",
                    "Use Editor Prewarming - no runtime scripts needed",
                    "Keep total texture memory under 40MB per material",
                    "Use Performance Rank Good (PC) as minimum target",
                    "Test on Quest if targeting mobile VR"
                });

                DrawTipCategory("Learning", new[]
                {
                    "Examine default presets to learn parameter combinations",
                    "Use Performance indicator to understand feature costs",
                    "Compare materials with Material Validator",
                    "Read glossary to understand technical terms",
                    "Experiment! Make copies before testing changes"
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
