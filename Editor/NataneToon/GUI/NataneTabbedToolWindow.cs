using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    using static NataneUIConstants;

    /// <summary>
    /// タブ付きツールウィンドウの抽象基底クラス
    /// TEnum で定義されたタブを切り替えながら複数ツールを統合表示する
    /// </summary>
    public abstract class NataneTabbedToolWindow<TEnum> : EditorWindow
        where TEnum : struct, Enum
    {
        protected TEnum activeTab;
        protected Material contextMaterial;

        private Vector2 scrollPosition;
        private Dictionary<TEnum, INataneToolTab> tabInstances;

        // ===== Abstract Members =====

        /// <summary>ウィンドウタイトル（日本語）</summary>
        protected abstract string WindowTitleJP { get; }

        /// <summary>ウィンドウタイトル（英語）</summary>
        protected abstract string WindowTitleEN { get; }

        /// <summary>ウィンドウの最小サイズ</summary>
        protected abstract Vector2 DefaultMinSize { get; }

        /// <summary>TEnum 値に対応するタブインスタンスを生成する</summary>
        protected abstract INataneToolTab CreateTab(TEnum tabEnum);

        // ===== Tab Persistence & First-Run Guidance =====

        /// <summary>EditorPrefs キー（タブ永続化用）</summary>
        private string TabPrefKey
        {
            get { return string.Format("NataneTool_{0}_ActiveTab", GetType().Name); }
        }

        /// <summary>EditorPrefs キー（初回表示フラグ用）</summary>
        private string FirstRunPrefKey
        {
            get { return string.Format("NataneTool_{0}_FirstRunDone", GetType().Name); }
        }

        private bool showFirstRunGuidance;

        // ===== Public API =====

        /// <summary>
        /// 指定タブを開いてウィンドウを前面に表示する
        /// </summary>
        public void OpenToTab(TEnum tab, Material material = null)
        {
            activeTab = tab;
            if (material != null) contextMaterial = material;
            EnsureTabInstance(tab);
            Show();
            Focus();
        }

        // ===== EditorWindow Lifecycle =====

        protected virtual void OnEnable()
        {
            tabInstances = new Dictionary<TEnum, INataneToolTab>();
            minSize = DefaultMinSize;

            // M-7: タブ永続化 - EditorPrefs から復元
            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            int savedIdx = EditorPrefs.GetInt(TabPrefKey, 0);
            if (savedIdx >= 0 && savedIdx < enumValues.Length)
            {
                activeTab = enumValues[savedIdx];
            }

            // L-2: 初回ガイダンス表示判定
            showFirstRunGuidance = !EditorPrefs.GetBool(FirstRunPrefKey, false);

            // M-8: タイトルにアクティブタブ名を反映
            UpdateWindowTitle();

            EnsureTabInstance(activeTab);
        }

        protected virtual void OnDisable()
        {
            if (tabInstances == null) return;

            foreach (var kvp in tabInstances)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.OnTabDestroy();
                }
            }

            tabInstances.Clear();
        }

        protected virtual void OnGUI()
        {
            // Check for pending bridge request (cross-assembly tab navigation)
            ConsumeBridgePending();

            // 1. Tool header
            NataneToonShaderGUIUtility.DrawToolHeader(WindowTitleJP, WindowTitleEN, GetActiveHelpKey());

            EditorGUILayout.Space(SPACE_SMALL);

            // 2. Tab bar
            DrawTabBar();

            EditorGUILayout.Space(SPACE_SMALL);

            // 3. Material context field (if required)
            var activeTabInstance = GetActiveTab();
            if (activeTabInstance != null && activeTabInstance.RequiresMaterial)
            {
                DrawMaterialField();
                EditorGUILayout.Space(SPACE_SMALL);
            }

            // L-2: 初回ガイダンスパネル
            if (showFirstRunGuidance)
            {
                DrawFirstRunGuidance();
            }

            // 4. Scrollable tab content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                if (activeTabInstance != null)
                {
                    activeTabInstance.OnTabGUI(contextMaterial);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // ===== Tab Bar Drawing =====

        private void DrawTabBar()
        {
            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));

            EditorGUILayout.BeginHorizontal();
            {
                foreach (var tab in enumValues)
                {
                    var tabInstance = GetTabInstance(tab);
                    bool isActive = EqualityComparer<TEnum>.Default.Equals(tab, activeTab);

                    // H-5: アクティブタブに背景色と太字を適用
                    GUIStyle style;
                    if (isActive)
                    {
                        style = new GUIStyle(EditorStyles.miniButtonMid)
                        {
                            fontStyle = FontStyle.Bold
                        };
                        style.normal.textColor = Color.white;
                    }
                    else
                    {
                        style = EditorStyles.miniButtonMid;
                    }

                    var content = new GUIContent(
                        tabInstance != null ? tabInstance.TabLabel : tab.ToString(),
                        tabInstance != null ? tabInstance.TabTooltip : string.Empty
                    );

                    if (GUILayout.Button(content, style,
                        GUILayout.MinWidth(TAB_BUTTON_MIN_WIDTH),
                        GUILayout.Height(TAB_BAR_HEIGHT)))
                    {
                        if (!isActive)
                        {
                            SwitchTab(tab);
                        }
                    }

                    // H-5: アクティブタブの下に色付きインジケーターラインを描画
                    if (isActive)
                    {
                        var lastRect = GUILayoutUtility.GetLastRect();
                        EditorGUI.DrawRect(
                            new Rect(lastRect.x, lastRect.yMax - TAB_INDICATOR_HEIGHT, lastRect.width, TAB_INDICATOR_HEIGHT),
                            TAB_ACTIVE_INDICATOR_COLOR);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMaterialField()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(
                    L("対象マテリアル", "Target Material"),
                    GUILayout.Width(120f));

                contextMaterial = (Material)EditorGUILayout.ObjectField(
                    contextMaterial,
                    typeof(Material),
                    true,
                    GUILayout.Height(MATERIAL_FIELD_HEIGHT));
            }
            EditorGUILayout.EndHorizontal();

            // H-6: NataneToon 以外のマテリアルに対する警告表示
            if (contextMaterial != null && contextMaterial.shader != null)
            {
                string shaderName = contextMaterial.shader.name;
                if (!shaderName.Contains("Natane") && !shaderName.Contains("Toon"))
                {
                    EditorGUILayout.HelpBox(
                        L("このマテリアルは Natane Toon シェーダーを使用していません。一部の機能が正しく動作しない場合があります。",
                          "This material does not use the Natane Toon shader. Some features may not work correctly."),
                        MessageType.Warning);
                }
            }
        }

        // ===== Tab Management =====

        private void SwitchTab(TEnum newTab)
        {
            // Deactivate old tab
            if (tabInstances != null && tabInstances.TryGetValue(activeTab, out var oldTab))
            {
                oldTab?.OnTabDisable();
            }

            activeTab = newTab;

            // M-7: タブ永続化 - EditorPrefs に保存
            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            EditorPrefs.SetInt(TabPrefKey, Array.IndexOf(enumValues, newTab));

            // Activate new tab
            var newTabInstance = GetTabInstance(newTab);
            newTabInstance?.OnTabEnable(this);

            // M-8: ウィンドウタイトルにアクティブタブ名を反映
            UpdateWindowTitle();
        }

        /// <summary>
        /// ウィンドウタイトルをベースタイトル + アクティブタブ名に更新する
        /// </summary>
        private void UpdateWindowTitle()
        {
            var tabInstance = GetTabInstance(activeTab);
            if (tabInstance != null)
            {
                titleContent = new GUIContent(
                    string.Format("{0} - {1}", L(WindowTitleJP, WindowTitleEN), tabInstance.TabLabel));
            }
            else
            {
                titleContent = new GUIContent(L(WindowTitleJP, WindowTitleEN));
            }
        }

        private INataneToolTab GetActiveTab()
        {
            return GetTabInstance(activeTab);
        }

        private void EnsureTabInstance(TEnum tab)
        {
            if (tabInstances == null)
            {
                tabInstances = new Dictionary<TEnum, INataneToolTab>();
            }

            if (!tabInstances.ContainsKey(tab))
            {
                var instance = CreateTab(tab);
                if (instance != null)
                {
                    tabInstances[tab] = instance;
                }
            }
        }

        private INataneToolTab GetTabInstance(TEnum tab)
        {
            EnsureTabInstance(tab);

            if (tabInstances != null && tabInstances.TryGetValue(tab, out var instance))
            {
                return instance;
            }

            return null;
        }

        private string GetActiveHelpKey()
        {
            var tab = GetActiveTab();
            return tab != null ? tab.HelpToolKey : string.Empty;
        }

        // ===== First-Run Guidance =====

        /// <summary>
        /// 初回起動時のガイダンスパネルを描画する
        /// タブの使い方やインスペクターからのアクセス方法を説明する
        /// </summary>
        private void DrawFirstRunGuidance()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(
                L("統合ウィンドウへようこそ！", "Welcome to the Consolidated Window!"),
                headerStyle);

            EditorGUILayout.Space(SPACE_TINY);

            var messageStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11
            };
            EditorGUILayout.LabelField(
                L("このウィンドウは複数のツールをタブで統合しています。上部のタブバーで機能を切り替えられます。\n\n" +
                  "マテリアルインスペクターの各セクションにある「→ ...で開く」ボタンからも、対応するタブを直接開くことができます。",
                  "This window consolidates multiple tools into tabs. Use the tab bar above to switch between features.\n\n" +
                  "You can also open specific tabs directly from the material inspector using the '→ Open in...' buttons in each section."),
                messageStyle);

            EditorGUILayout.Space(SPACE_SMALL);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L("OK、閉じる", "OK, Got it"), GUILayout.Width(120), GUILayout.Height(24)))
            {
                showFirstRunGuidance = false;
                EditorPrefs.SetBool(FirstRunPrefKey, true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(SPACE_SMALL);
        }

        /// <summary>
        /// NataneToolBridge からのペンディングリクエストを消費する
        /// ShaderGUI などの別アセンブリからタブ指定＋マテリアル渡しに使用
        /// C-3: TryConsume を使用してレースコンディション安全に消費
        /// </summary>
        private void ConsumeBridgePending()
        {
            string windowTypeKey = GetType().Name;
            Material bridgeMaterial;
            int bridgeTabIndex;

            if (!NataneToolBridge.TryConsume(windowTypeKey, out bridgeMaterial, out bridgeTabIndex))
            {
                return;
            }

            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            if (bridgeTabIndex >= 0 && bridgeTabIndex < enumValues.Length)
            {
                var targetTab = enumValues[bridgeTabIndex];
                if (!EqualityComparer<TEnum>.Default.Equals(targetTab, activeTab))
                {
                    SwitchTab(targetTab);
                }
            }

            if (bridgeMaterial != null)
            {
                contextMaterial = bridgeMaterial;
            }
        }
    }
}
