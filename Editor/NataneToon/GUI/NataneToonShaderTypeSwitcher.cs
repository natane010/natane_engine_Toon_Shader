using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    /// <summary>
    /// シェーダータイプの定義と切り替えユーティリティ
    /// Shader type definitions and switching utility
    /// </summary>
    public enum ShaderType
    {
        Toon = 0,
        Eye = 1,
        Wirelight = 2,
        ScreenFX = 3
    }

    public static class NataneToonShaderTypeSwitcher
    {
        // シェーダー名の定義
        private static readonly string[] ToonShaderNames = new string[]
        {
            "Natane/Toon Shader",
            "Natane/Toon Shader (Cutout)",
            "Natane/Toon Shader (Transparent)",
            "Natane/Toon Shader (Fur)"
        };

        private const string EyeShaderName = "Natane/Eye";
        private const string WirelightShaderName = "Natane/Toon Shader Wirelight";
        private const string ScreenFXShaderName = "Natane/Screen FX Overlay";

        // 表示名（日本語）
        private static readonly string[] ShaderTypeLabels = new string[]
        {
            "Toon (トゥーン)",
            "Eye (目)",
            "Wirelight (ワイヤーライト)",
            "Screen FX (スクリーンエフェクト)"
        };

        /// <summary>
        /// マテリアルのシェーダー名からShaderTypeを検出
        /// </summary>
        public static ShaderType DetectShaderType(Material material)
        {
            if (material == null || material.shader == null)
                return ShaderType.Toon;

            string shaderName = material.shader.name;

            // Toon variants
            for (int i = 0; i < ToonShaderNames.Length; i++)
            {
                if (shaderName == ToonShaderNames[i])
                    return ShaderType.Toon;
            }

            if (shaderName == EyeShaderName)
                return ShaderType.Eye;

            if (shaderName == WirelightShaderName)
                return ShaderType.Wirelight;

            if (shaderName == ScreenFXShaderName)
                return ShaderType.ScreenFX;

            // フォールバック: 不明なシェーダーの場合はToon扱い
            return ShaderType.Toon;
        }

        /// <summary>
        /// ShaderTypeに応じたデフォルトシェーダー名を返す
        /// </summary>
        public static string GetDefaultShaderName(ShaderType type)
        {
            switch (type)
            {
                case ShaderType.Toon: return ToonShaderNames[0];
                case ShaderType.Eye: return EyeShaderName;
                case ShaderType.Wirelight: return WirelightShaderName;
                case ShaderType.ScreenFX: return ScreenFXShaderName;
                default: return ToonShaderNames[0];
            }
        }

        /// <summary>
        /// シェーダータイプを切り替える（Undo対応）
        /// </summary>
        public static void SetShaderType(Material material, ShaderType type, MaterialEditor editor)
        {
            if (material == null) return;

            string newShaderName = GetDefaultShaderName(type);
            Shader newShader = Shader.Find(newShaderName);

            if (newShader == null)
            {
                Debug.LogError($"[NataneToonShaderTypeSwitcher] Shader not found: {newShaderName}");
                return;
            }

            if (material.shader == newShader) return;

            Undo.RecordObject(material, "Change Shader Type");
            material.shader = newShader;
            EditorUtility.SetDirty(material);

            if (editor != null)
            {
                editor.Repaint();
            }
        }

        /// <summary>
        /// シェーダータイプ選択のドロップダウンを描画
        /// 変更があった場合 true を返す
        /// </summary>
        public static bool DrawShaderTypeDropdown(Material material, MaterialEditor editor, out bool shouldReturn)
        {
            shouldReturn = false;
            ShaderType currentType = DetectShaderType(material);

            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            ShaderType newType = (ShaderType)EditorGUILayout.EnumPopup("シェーダータイプ", currentType);

            if (EditorGUI.EndChangeCheck() && newType != currentType)
            {
                if (EditorUtility.DisplayDialog(
                    "シェーダータイプ変更",
                    "シェーダータイプを変更すると、現在の設定の一部が失われる可能性があります。\n続行しますか？",
                    "変更する",
                    "キャンセル"))
                {
                    SetShaderType(material, newType, editor);
                    shouldReturn = true;
                    return true;
                }
            }

            return currentType != ShaderType.Toon;
        }
    }
}
