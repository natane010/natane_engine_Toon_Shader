using System;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    /// <summary>
    /// まだどの .shader にも書かれていない「新規プロパティ」の定義表。
    ///
    /// 正準カタログは現行 12 本の .shader から導出しているため、それだけでは
    /// 「新機能のプロパティを 1 箇所に足したら全バリアントに反映される」という
    /// 当初の狙いが満たせない。どこか 1 本に足しても、採否グループが
    /// 「その 1 本だけ」と推論されて他へ広がらないため。
    ///
    /// そこでここに「どのグループに属するか」を明示した追加分を書く。
    /// 生成時にカタログへ合流し、そのグループを持つ全バリアントへ展開される。
    ///
    /// 反映されたあとも項目を消す必要はない。導出結果と一致するだけなので無害。
    /// ただし恒久的に残すなら、シェーダー側が真の所在になった時点で
    /// ここから外しておくほうが二重管理を避けられる。
    /// </summary>
    internal static class NataneShaderPropertyAdditions
    {
        internal sealed class Addition
        {
            /// <summary>属する採否グループ ID（CORE / EXCEPT_Background など）。</summary>
            public string GroupId;

            /// <summary>
            /// 挿入位置。このプロパティの直後に入る。
            /// null や未知の名前なら末尾へ。
            /// </summary>
            public string AfterProperty;

            /// <summary>
            /// 装飾行（[Header] / セクションコメント / 空行）。宣言の直前に出力される。
            /// </summary>
            public string[] LeadingLines = Array.Empty<string>();

            /// <summary>宣言行。.shader にそのまま書く形式。</summary>
            public string[] Declarations = Array.Empty<string>();
        }

        /// <summary>
        /// 追加分。空なら何もしない（カタログは導出結果のままになる）。
        /// </summary>
        public static readonly Addition[] Items =
        {
            // 影の玉ボケ（木漏れ日）。全バリアント共通なので CORE。
            // Caustics の直後に置く（同じサーフェスFX の系統で、ShadowOnly 合成という
            // 発想も共有しているため、インスペクターでも隣り合うのが自然）。
            new Addition
            {
                GroupId = "CORE",
                AfterProperty = "_CausticsMask",
                LeadingLines = new[]
                {
                    string.Empty,
                    "// D. Shadow Bokeh (影の玉ボケ / 木漏れ日)",
                },
                Declarations = new[]
                {
                    "[Toggle(_SHADOW_BOKEH)] _ShadowBokeh (\"Enable Shadow Bokeh (影の玉ボケ)\", Float) = 0",
                    "[Enum(ShadowOnly,0,LitOnly,1,All,2)] _ShadowBokehComposite (\"Shadow Bokeh Composite\", Float) = 0",
                    "[HDR] _ShadowBokehColor (\"Shadow Bokeh Color\", Color) = (1, 0.95, 0.8, 1)",
                    "_ShadowBokehIntensity (\"Shadow Bokeh Intensity\", Range(0, 10)) = 2",
                    "_ShadowBokehScale (\"Shadow Bokeh Scale\", Range(0.1, 20)) = 3",
                    "_ShadowBokehSize (\"Shadow Bokeh Size\", Range(0.05, 1)) = 0.35",
                    "_ShadowBokehSoftness (\"Shadow Bokeh Softness\", Range(0, 1)) = 0.5",
                    "_ShadowBokehBlades (\"Shadow Bokeh Aperture Blades\", Range(0, 8)) = 0",
                    "_ShadowBokehRimGain (\"Shadow Bokeh Rim Gain\", Range(0, 1)) = 0.25",
                    "_ShadowBokehSpeed (\"Shadow Bokeh Drift Speed\", Float) = 0.05",
                    "_ShadowBokehDirection (\"Shadow Bokeh Drift Direction (XY)\", Vector) = (1, 0.3, 0, 0)",
                    "_ShadowBokehShadowMin (\"Shadow Bokeh Shadow Threshold\", Range(0, 1)) = 0.35",
                    "_ShadowBokehBlend (\"Shadow Bokeh Blend\", Range(0, 1)) = 1",
                    "[NoScaleOffset] _ShadowBokehMask (\"Shadow Bokeh Mask (R)\", 2D) = \"white\" {}",
                },
            },
        };

        /// <summary>
        /// 追加分をカタログへ合流させる。
        /// 既に導出済みのプロパティ（＝どこかの .shader に書かれている）は、
        /// 現物を正とみなして飛ばす。
        /// </summary>
        /// <returns>実際に合流させたプロパティ名。生成器の安全検査に渡して、
        /// 「意図した追加」と「生成器が勝手に増やした宣言」を区別させる。</returns>
        public static List<string> Merge(NataneShaderPropertyCatalog catalog)
        {
            var addedNames = new List<string>();
            if (catalog == null || Items.Length == 0) return addedNames;

            var existing = new HashSet<string>(
                catalog.Declarations.Select(d => d.Name), StringComparer.Ordinal);

            foreach (Addition addition in Items)
            {
                NatanePropertyGroup group =
                    catalog.Groups.FirstOrDefault(g => g.Id == addition.GroupId);

                if (group == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[NataneToonShader] 追加定義のグループ '{addition.GroupId}' が見つかりません。" +
                        "採否グループ ID は Properties カタログ整合チェックのレポートで確認できます。");
                    continue;
                }

                // 挿入位置を決める。見つからなければ末尾。
                int insertAt = catalog.Declarations.Count;
                if (!string.IsNullOrEmpty(addition.AfterProperty))
                {
                    int idx = catalog.Declarations.FindIndex(d => d.Name == addition.AfterProperty);
                    if (idx >= 0) insertAt = idx + 1;
                }

                var leading = new List<string>(addition.LeadingLines ?? Array.Empty<string>());

                foreach (string line in addition.Declarations ?? Array.Empty<string>())
                {
                    List<NatanePropertyDeclaration> parsed =
                        NatanePropertyParser.ParseBlockText(line, out List<string> unparsed);

                    if (unparsed.Count > 0 || parsed.Count != 1)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[NataneToonShader] 追加定義の宣言を解釈できませんでした: {line}");
                        continue;
                    }

                    NatanePropertyDeclaration decl = parsed[0];
                    if (existing.Contains(decl.Name)) continue; // 現物が正

                    // 最初の宣言にだけ装飾行を付ける（見出しの重複を避ける）。
                    decl.LeadingLines = leading;
                    leading = new List<string>();

                    catalog.Declarations.Insert(insertAt, decl);
                    insertAt++;

                    group.PropertyNames.Add(decl.Name);
                    existing.Add(decl.Name);
                    addedNames.Add(decl.Name);
                }
            }

            return addedNames;
        }
    }
}
