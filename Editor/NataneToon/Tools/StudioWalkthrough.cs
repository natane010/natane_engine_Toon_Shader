using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// First-time walkthrough guide for the texture studio.
    /// テクスチャスタジオの初回使用ガイダンス
    /// </summary>
    internal class StudioWalkthrough : EditorWindow
    {
        private int currentStep;
        private static readonly string PrefsKey = "NataneToon_StudioWalkthroughDone";
        private Vector2 scrollPos;

        private static GUIStyle titleStyle;
        private static GUIStyle bodyStyle;
        private static GUIStyle stepStyle;

        public static bool HasCompleted => EditorPrefs.GetBool(PrefsKey, false);

        public static void ShowIfFirstTime()
        {
            if (HasCompleted) return;
            Open();
        }

        [MenuItem("Tools/Natane/Texture Studio Guide", false, 160)]
        public static void Open()
        {
            var w = GetWindow<StudioWalkthrough>(true,
                L("テクスチャスタジオ ガイド", "Texture Studio Guide"));
            w.minSize = new Vector2(480, 400);
            w.maxSize = new Vector2(520, 500);
            w.currentStep = 0;
            w.ShowUtility();
        }

        private static void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 12, padding = new RectOffset(8, 8, 4, 4) };
            stepStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = new Color(0.55f, 0.78f, 1f) } };
        }

        private static readonly (string title, string body)[] steps = new[]
        {
            (L("テクスチャスタジオへようこそ！", "Welcome to Texture Studio!"),
             L("Unity内でテクスチャをリアルタイム編集できるプロフェッショナルツールです。\n\n外部ツールとの往復なしで、ブラシで塗った瞬間にScene Viewに反映されます。", "A professional tool for editing textures in real-time within Unity.\n\nNo more round-tripping to external tools - paint and see changes instantly in Scene View.")),

            (L("Step 1: マテリアルを選択", "Step 1: Select a Material"),
             L("右パネル「設定」タブ → マテリアル割当 で対象マテリアルを選択します。\nまたは、マテリアルインスペクタのテクスチャ横の🖌ボタンから直接起動できます。", "Go to right panel 'Settings' tab → Material Assignment to select a target material.\nOr click the 🖌 button next to textures in the Material Inspector.")),

            (L("Step 2: テクスチャプロパティを選択", "Step 2: Choose Texture Property"),
             L("編集したいテクスチャプロパティ（影マスク、エミッション、リムマスク等）を選択します。\nLIVEボタンを押すとリアルタイムプレビューが有効になります。", "Select the texture property to edit (shadow mask, emission, rim mask, etc.).\nPress the LIVE button to enable real-time preview.")),

            (L("Step 3: ブラシで描く", "Step 3: Paint with Brush"),
             L("左のツールバーからブラシ(B)を選択して、キャンバスに描きます。\n\n• Mask モード: グレースケールでマスク値をペイント\n• Color モード: フルカラーでテクスチャをペイント\n• 対称描画、グラデーション、塗りつぶしも利用可能", "Select Brush (B) from the left toolbar and paint on the canvas.\n\n• Mask mode: Paint grayscale mask values\n• Color mode: Paint full-color textures\n• Symmetry, gradient, and fill tools also available")),

            (L("Step 4: 書き出し & 適用", "Step 4: Export & Apply"),
             L("完成したら:\n• コマンドバーの「Export」で書き出し\n• LIVEモードなら自動的にマテリアルに反映済み\n• Ctrl+S でプロジェクト保存\n\nヒント: Tab キーでクイックパレット、F1 でショートカット一覧", "When finished:\n• Use 'Export' in command bar to save\n• In LIVE mode, material is already updated\n• Ctrl+S to save project\n\nTips: Tab for quick palette, F1 for shortcuts")),
        };

        private void OnGUI()
        {
            EnsureStyles();

            EditorGUILayout.LabelField(
                L("テクスチャスタジオ ガイド", "Texture Studio Guide"), titleStyle);
            EditorGUILayout.Space(8);

            // Progress
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < steps.Length; i++)
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = i == currentStep ? new Color(0.45f, 0.67f, 0.96f)
                    : i < currentStep ? new Color(0.3f, 0.7f, 0.3f) : Color.gray;
                GUILayout.Button($"{i + 1}", GUILayout.Width(32), GUILayout.Height(20));
                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Content
            var step = steps[currentStep];
            EditorGUILayout.LabelField(step.title, stepStyle);
            EditorGUILayout.Space(4);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField(step.body, bodyStyle);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);

            // Navigation
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = currentStep > 0;
            if (GUILayout.Button(L("← 前へ", "← Back"), GUILayout.Height(30)))
                currentStep--;
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            if (currentStep < steps.Length - 1)
            {
                if (GUILayout.Button(L("次へ →", "Next →"), GUILayout.Height(30)))
                    currentStep++;
            }
            else
            {
                if (GUILayout.Button(L("始める！", "Get Started!"), GUILayout.Height(30)))
                {
                    EditorPrefs.SetBool(PrefsKey, true);
                    Close();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
