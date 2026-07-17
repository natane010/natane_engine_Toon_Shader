using System;
using System.Collections.Generic;
using System.Linq;

namespace NataneToon.Editor
{
    public enum NataneInspectorTab
    {
        Setup,
        Lighting,
        Surface,
        Effects,
        Output
    }

    public sealed class NataneInspectorSectionDescriptor
    {
        public string Key { get; }
        public NataneInspectorTab Tab { get; }
        public string GroupJP { get; }
        public string GroupEN { get; }
        public string LabelJP { get; }
        public string LabelEN { get; }
        public string ToggleKeyword { get; }
        public string ToggleProperty { get; }
        public string SearchTerms { get; }
        public int Order { get; }

        public string Label => NataneToonLocalization.IsJapanese ? LabelJP : LabelEN;
        public string Group => NataneToonLocalization.IsJapanese ? GroupJP : GroupEN;

        public NataneInspectorSectionDescriptor(
            string key,
            NataneInspectorTab tab,
            string groupJP,
            string groupEN,
            string labelJP,
            string labelEN,
            int order,
            string toggleKeyword = null,
            string toggleProperty = null,
            string searchTerms = null)
        {
            Key = key;
            Tab = tab;
            GroupJP = groupJP;
            GroupEN = groupEN;
            LabelJP = labelJP;
            LabelEN = labelEN;
            Order = order;
            ToggleKeyword = toggleKeyword;
            ToggleProperty = toggleProperty;
            SearchTerms = searchTerms ?? string.Empty;
        }

        public int GetSearchScore(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return 0;

            string normalized = query.Trim().ToLowerInvariant();
            string labelJP = LabelJP.ToLowerInvariant();
            string labelEN = LabelEN.ToLowerInvariant();
            string key = Key.ToLowerInvariant();
            string terms = SearchTerms.ToLowerInvariant();
            string keyword = (ToggleKeyword ?? string.Empty).ToLowerInvariant();
            string property = (ToggleProperty ?? string.Empty).ToLowerInvariant();

            if (labelJP == normalized || labelEN == normalized || key == normalized) return 100;
            if (labelJP.StartsWith(normalized) || labelEN.StartsWith(normalized)) return 80;
            if (labelJP.Contains(normalized) || labelEN.Contains(normalized)) return 60;
            if (property.Contains(normalized) || keyword.Contains(normalized)) return 50;
            return terms.Contains(normalized) ? 30 : 0;
        }
    }

    /// <summary>
    /// インスペクターのナビゲーション、検索、ON/OFF操作に使うメタデータ。
    /// 一元管理することで、検索やジャンプメニューからのセクション漏れを防ぐ。
    /// </summary>
    public static class NataneToonInspectorSectionRegistry
    {
        private static NataneInspectorSectionDescriptor D(
            string key, NataneInspectorTab tab, string groupJP, string groupEN,
            string labelJP, string labelEN, int order,
            string keyword = null, string property = null, string terms = null)
        {
            return new NataneInspectorSectionDescriptor(
                key, tab, groupJP, groupEN, labelJP, labelEN, order, keyword, property, terms);
        }

        private static readonly IReadOnlyList<NataneInspectorSectionDescriptor> Sections =
            new List<NataneInspectorSectionDescriptor>
            {
                D("QuickSetup", NataneInspectorTab.Setup, "スタート", "Start", "クイックセットアップ", "Quick Setup", 0, terms: "auto guided preset role look quality 自動 用途 見た目 品質"),
                D("Presets", NataneInspectorTab.Setup, "スタート", "Start", "プリセットと共有", "Presets & Sharing", 1, terms: "material preset copy paste share プリセット 共有"),
                D("MainTexture", NataneInspectorTab.Setup, "基本ルック", "Base Look", "メインテクスチャ", "Main Texture", 10, terms: "albedo base color texture アルベド 色 テクスチャ"),
                D("MakeupTextures", NataneInspectorTab.Setup, "基本ルック", "Base Look", "追加テクスチャ", "Additional Textures", 11, terms: "makeup layer second third fourth fifth メイク レイヤー"),
                D("GradientBaseColor", NataneInspectorTab.Setup, "基本ルック", "Base Look", "グラデーションベースカラー", "Gradient Base Color", 12, "_GRADIENT_BASE_COLOR", "_GradientBaseColor", "gradient tint position グラデーション 色"),
                D("ScreenTone", NataneInspectorTab.Setup, "基本ルック", "Base Look", "スクリーントーン", "Screen Tone", 13, "_SCREEN_TONE", "_ScreenTone", "halftone dot overlay ハーフトーン 網点"),
                D("Shading", NataneInspectorTab.Setup, "陰影", "Shading", "トゥーンシェーディング", "Toon Shading", 20, terms: "shadow shade toon gradient ramp 影 陰影 ランプ"),
                D("HalftoneShadow", NataneInspectorTab.Setup, "陰影", "Shading", "ハーフトーンシャドウ", "Halftone Shadow", 21, "_HALFTONE_SHADOW", "_HalftoneShadow", "shadow dots comic 影 網点 漫画"),
                D("ShadowEdgeNoise", NataneInspectorTab.Setup, "陰影", "Shading", "影エッジノイズ", "Shadow Edge Noise", 22, "_SHADOW_EDGE_NOISE", "_ShadowEdgeNoise", "hand drawn analog edge noise 手描き 揺らぎ"),

                D("AdvancedLighting", NataneInspectorTab.Lighting, "基本ライティング", "Lighting Basics", "ライティング調整", "Lighting Controls", 100, terms: "light minimum maximum blend gi lighting 光 ライト"),
                D("CastShadowColor", NataneInspectorTab.Lighting, "基本ライティング", "Lighting Basics", "キャストシャドウカラー", "Cast Shadow Color", 101, "_CAST_SHADOW_COLOR", "_CastShadowColorEnable", "cast shadow tint 落ち影 色"),
                D("LightSnap", NataneInspectorTab.Lighting, "基本ライティング", "Lighting Basics", "ライト方向スナップ", "Light Direction Snap", 102, "_LIGHT_SNAP", "_LightSnap", "direction stabilize flicker 方向 固定 ちらつき"),
                D("AO", NataneInspectorTab.Lighting, "基本ライティング", "Lighting Basics", "アンビエントオクルージョン", "Ambient Occlusion", 103, "_USE_AO", "_UseAO", "ao occlusion cavity ambient 遮蔽"),
                D("Dithering", NataneInspectorTab.Lighting, "基本ライティング", "Lighting Basics", "ディザリング", "Dithering", 104, "_USE_DITHERING", "_UseDithering", "screen tone dither pattern ディザ 網点"),
                D("Specular", NataneInspectorTab.Lighting, "表面の光", "Surface Lighting", "スペキュラー", "Specular", 110, "_SPECULAR", "_Specular", "highlight gloss roughness 光沢 ハイライト"),
                D("HairSpecular", NataneInspectorTab.Lighting, "表面の光", "Surface Lighting", "ヘアハイライト", "Hair Highlight", 111, "_HAIR_SPECULAR", "_HairSpecular", "hair kajiya highlight flow 髪 天使の輪"),
                D("RimLight", NataneInspectorTab.Lighting, "表面の光", "Surface Lighting", "リムライト", "Rim Light", 112, "_RIM_LIGHT", "_RimLight", "edge light fresnel outline 輪郭光 縁"),
                D("SSS", NataneInspectorTab.Lighting, "表面の光", "Surface Lighting", "半透明表現", "Subsurface Scattering", 113, "_SSS", "_SSS", "subsurface skin translucent thickness 肌 透過"),
                D("LightVolume", NataneInspectorTab.Lighting, "外部ライティング", "External Lighting", "VRC Light Volumes", "VRC Light Volumes", 120, "_USE_LIGHT_VOLUME", "_UseLightVolume", "vrchat voxel world light volume"),
                D("LTCGI", NataneInspectorTab.Lighting, "外部ライティング", "External Lighting", "LTCGI", "LTCGI", 121, "_LTCGI", "_LTCGI", "area light realtime gi エリアライト"),

                D("NormalMap", NataneInspectorTab.Surface, "マッピング", "Mapping", "ノーマルマップ", "Normal Map", 200, "_NORMALMAP", "_UseNormalMap", "normal bump detail 法線 凹凸"),
                D("DetailMap", NataneInspectorTab.Surface, "マッピング", "Mapping", "ディテールマップ", "Detail Map", 201, "_DETAIL_MAP", "_DetailMap", "secondary uv detail closeup 細部"),
                D("Parallax", NataneInspectorTab.Surface, "マッピング", "Mapping", "視差マッピング", "Parallax Mapping", 202, "_PARALLAX", "_Parallax", "height depth pom parallax 視差 高さ"),
                D("Triplanar", NataneInspectorTab.Surface, "マッピング", "Mapping", "トライプレーナー", "Triplanar Mapping", 203, "_TRIPLANAR", "_Triplanar", "projection no uv terrain rock 投影 地形"),
                D("MatCap", NataneInspectorTab.Surface, "質感", "Surface Finish", "MatCap", "MatCap", 210, "_MATCAP", "_MatCap", "sphere map material capture 質感"),
                D("ProceduralMatCap", NataneInspectorTab.Surface, "質感", "Surface Finish", "プロシージャルMatCap", "Procedural MatCap", 211, "_PROCEDURAL_MATCAP", "_ProceduralMatCap", "procedural mathematical texture free 数式"),
                D("Reflection", NataneInspectorTab.Surface, "質感", "Surface Finish", "リフレクション", "Reflection", 212, "_REFLECTION", "_Reflection", "cubemap probe environment reflection 反射"),
                D("FakeReflection", NataneInspectorTab.Surface, "質感", "Surface Finish", "フェイクリフレクション", "Fake Reflection", 213, "_FAKE_REFLECTION", "_FakeReflection", "fake sky ground lightweight 擬似反射"),
                D("Iridescence", NataneInspectorTab.Surface, "質感", "Surface Finish", "イリデッセンス", "Iridescence", 214, "_IRIDESCENCE", "_Iridescence", "rainbow thin film 玉虫色 虹"),
                D("EnvironmentalRim", NataneInspectorTab.Surface, "質感", "Surface Finish", "環境リム", "Environmental Rim", 215, "_ENV_RIM", "_EnvRim", "environment rim sky ground 環境 縁"),
                D("Outline", NataneInspectorTab.Surface, "形状表現", "Shape", "アウトライン", "Outline", 220, "_OUTLINE", "_Outline", "contour line smooth normal 輪郭線 線"),
                D("Fur", NataneInspectorTab.Surface, "形状表現", "Shape", "ファー", "Fur", 221, "_FUR", "_Fur", "shell fur hair pelt 毛皮 毛"),
                D("SurfaceCover", NataneInspectorTab.Surface, "形状表現", "Shape", "サーフェスカバー", "Surface Cover", 222, "_SURFACE_COVER", "_SurfaceCover", "snow sand accumulation 雪 砂 堆積"),
                D("PBR", NataneInspectorTab.Surface, "質感", "Surface Finish", "PBRマテリアル", "PBR Material", 223, "_PBR", "_EnablePBR", "metallic smoothness pbr 金属 粗さ"),

                D("Emission", NataneInspectorTab.Effects, "発光とスタイル", "Glow & Style", "エミッション", "Emission", 300, "_EMISSION", "_Emission", "glow bloom emissive 発光 ブルーム"),
                D("Hologram", NataneInspectorTab.Effects, "発光とスタイル", "Glow & Style", "ホログラムとグリッチ", "Hologram & Glitch", 301, "_HOLOGRAM", "_Hologram", "scanline glitch digital hologram ノイズ"),
                D("IllustrationStyle", NataneInspectorTab.Effects, "発光とスタイル", "Glow & Style", "イラスト調スタイル", "Illustration Style", 302, "_COLOR_QUANTIZE", "_UseColorQuantize", "watercolor kuwahara hatching lut quantize イラスト 水彩"),
                D("Glitter", NataneInspectorTab.Effects, "サーフェスFX", "Surface FX", "グリッター", "Glitter", 310, "_GLITTER", "_Glitter", "sparkle glint glitter ラメ 輝き"),
                D("Drip", NataneInspectorTab.Effects, "サーフェスFX", "Surface FX", "雫エフェクト", "Drip Effect", 311, "_WATER_DRIP", "_WaterDrip", "water drop rain 雫 水滴"),
                D("Smear", NataneInspectorTab.Effects, "サーフェスFX", "Surface FX", "スミア", "Smear", 312, "_SMEAR", "_Smear", "afterimage stretch trail 残像 引き伸ばし"),
                D("Decal", NataneInspectorTab.Effects, "サーフェスFX", "Surface FX", "デカール", "Decal", 313, "_DECAL", "_Decal", "sticker overlay 貼り付け 模様"),
                D("VirtualExpression", NataneInspectorTab.Effects, "演出", "Presentation", "バーチャル表現", "Virtual Expression", 320, terms: "dissolve virtual expression ディゾルブ 消滅"),
                D("AudioLink", NataneInspectorTab.Effects, "演出", "Presentation", "AudioLink", "AudioLink", 321, "_AUDIOLINK", "_AudioLink", "music reactive vrchat 音楽 連動"),
                D("VertexAnimation", NataneInspectorTab.Effects, "モーション", "Motion", "頂点アニメーション", "Vertex Animation", 330, "_VERTEX_ANIMATION", "_VertexAnimation", "wind breath pulse motion 風 呼吸 脈動"),
                D("VAT", NataneInspectorTab.Effects, "モーション", "Motion", "VAT", "VAT", 331, "_VAT", "_VAT", "vertex animation texture houdini 頂点アニメ"),
                D("Tessellation", NataneInspectorTab.Effects, "モーション", "Motion", "テッセレーション", "Tessellation", 332, "_TESSELLATION", "_Tessellation", "phong smooth subdivision 分割 スムーズ"),
                D("Refraction", NataneInspectorTab.Effects, "空間FX", "Spatial FX", "屈折", "Refraction", 340, "_REFRACTION", "_Refraction", "grabpass ior glass 屈折 ガラス"),
                D("HeightFog", NataneInspectorTab.Effects, "空間FX", "Spatial FX", "ハイトフォグ", "Height Fog", 341, "_HEIGHT_FOG", "_HeightFog", "fog mist atmosphere 霧 高さ"),
                D("DepthColorFade", NataneInspectorTab.Effects, "空間FX", "Spatial FX", "深度カラーフェード", "Depth Color Fade", 342, "_DEPTH_COLOR_FADE", "_DepthColorFade", "distance atmosphere depth color 遠景 空気遠近"),

                D("MirrorControl", NataneInspectorTab.Output, "VRChat", "VRChat", "ミラー・カメラ制御", "Mirror / Camera Control", 400, "_MIRROR_CONTROL", "_MirrorControl", "mirror camera photo visibility 鏡 カメラ 写真"),
                D("MirrorTexture", NataneInspectorTab.Output, "VRChat", "VRChat", "鏡・カメラ写り分け", "Mirror / Camera Alt Texture", 401, "_MIRROR_TEXTURE", "_MirrorTexture", "mirror camera alternate secret texture 鏡 写り分け"),
                D("Backface", NataneInspectorTab.Output, "表示制御", "Visibility", "裏面テクスチャ", "Backface Texture", 410, "_BACKFACE_TEXTURE", "_BackfaceTexture", "backface cull double sided 裏面 両面"),
                D("Video", NataneInspectorTab.Output, "表示制御", "Visibility", "ビデオテクスチャ", "Video Texture", 411, "_VIDEO_TEXTURE", "_VideoTexture", "video render texture movie 動画"),
                D("HeightFade", NataneInspectorTab.Output, "表示制御", "Visibility", "高さフェード", "Height Fade", 412, "_HEIGHT_FADE", "_HeightFade", "height alpha fade feet 高さ 足元 消す"),
                D("IntersectionFade", NataneInspectorTab.Output, "表示制御", "Visibility", "交差フェード", "Intersection Fade", 413, "_INTERSECTION_FADE", "_IntersectionFade", "depth contact intersection 交差 接触"),
                D("DistanceFade", NataneInspectorTab.Output, "表示制御", "Visibility", "距離フェード", "Distance Fade", 414, "_DISTANCE_FADE", "_DistanceFade", "distance lod camera fade 距離 消える"),
                D("PerspectiveFlat", NataneInspectorTab.Output, "投影と変形", "Projection", "パースフラット", "Perspective Flatten", 420, "_PERSPECTIVE_FLAT", "_PerspectiveFlat", "2d depth compression perspective 平面 パース"),
                D("FaceOrtho", NataneInspectorTab.Output, "投影と変形", "Projection", "顔直交投影", "Face Ortho Projection", 421, "_FACE_ORTHO", "_FaceOrtho", "face fov orthographic vr 顔 直交"),
                D("Ghost", NataneInspectorTab.Output, "バリアント設定", "Variant", "ゴースト", "Ghost", 430, terms: "ghost transparent spectral fresnel 幽霊 透明"),
                D("QuestLite", NataneInspectorTab.Output, "バリアント設定", "Variant", "Quest軽量パス", "Quest Lite", 431, "_QUEST_LITE", "_QuestLite", "quest mobile optimization 軽量 モバイル"),
                D("BackgroundLightmap", NataneInspectorTab.Output, "バリアント設定", "Variant", "背景ライトマップ", "Background Lightmap", 432, terms: "background lightmap bake gi 背景 ベイク"),
                D("Rendering", NataneInspectorTab.Output, "出力", "Output", "レンダリング設定", "Rendering Settings", 440, terms: "render queue blend zwrite stencil cull 描画 ステンシル"),
                D("FeatureOverview", NataneInspectorTab.Output, "診断", "Diagnostics", "有効機能", "Active Features", 450, terms: "feature overview active toggle 機能 一覧"),
                D("Performance", NataneInspectorTab.Output, "診断", "Diagnostics", "パフォーマンス", "Performance", 451, terms: "sampler pass cost rating optimize 性能 最適化"),
            };

        public static IReadOnlyList<NataneInspectorSectionDescriptor> All => Sections;

        public static IEnumerable<NataneInspectorSectionDescriptor> ForTab(NataneInspectorTab tab)
        {
            return Sections.Where(section => section.Tab == tab).OrderBy(section => section.Order);
        }

        public static NataneInspectorSectionDescriptor Find(string key)
        {
            return Sections.FirstOrDefault(section => string.Equals(section.Key, key, StringComparison.Ordinal));
        }

        public static NataneInspectorSectionDescriptor FindByKeyword(string keyword)
        {
            return Sections.FirstOrDefault(section =>
                string.Equals(section.ToggleKeyword, keyword, StringComparison.Ordinal));
        }

        public static IEnumerable<NataneInspectorSectionDescriptor> Search(string query)
        {
            return Sections
                .Select(section => new { Section = section, Score = section.GetSearchScore(query) })
                .Where(result => result.Score > 0)
                .OrderByDescending(result => result.Score)
                .ThenBy(result => result.Section.Order)
                .Select(result => result.Section);
        }
    }
}
