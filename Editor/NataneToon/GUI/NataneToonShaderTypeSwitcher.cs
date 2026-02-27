using UnityEngine;
using UnityEditor;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;
    /// <summary>
    /// シェーダータイプの定義と切り替えユーティリティ
    /// Shader type definitions and switching utility
    /// </summary>
    public enum ShaderType
    {
        Toon = 0,
        Eye = 1,
        Wirelight = 2,
        ScreenFX = 3,
        StandardToon = 4
    }

    public static class NataneToonShaderTypeSwitcher
    {
        // シェーダー名の定義
        private static readonly string[] ToonShaderNames = new string[]
        {
            "Natane/Toon Shader",
            "Natane/Toon Shader (Cutout)",
            "Natane/Toon Shader (Transparent)",
            "Natane/Toon Shader (Fur)",
            "Natane/Toon Shader (Background)"
        };

        private const string EyeShaderName = "Natane/Eye";
        private const string WirelightShaderName = "Natane/Toon Shader Wirelight";
        private const string ScreenFXShaderName = "Natane/Screen FX Overlay";

        // Display names (bilingual)
        private static string[] ShaderTypeLabels => new string[]
        {
            L("Toon (トゥーン)", "Toon"),
            L("Eye (目)", "Eye"),
            L("Wirelight (ワイヤーライト)", "Wirelight"),
            L("Screen FX (スクリーンエフェクト)", "Screen FX"),
            L("StandardToon (lilToon互換)", "StandardToon (lilToon)")
        };

        /// <summary>
        /// マテリアルのシェーダー名からShaderTypeを検出
        /// </summary>
        public static ShaderType DetectShaderType(Material material)
        {
            if (material == null || material.shader == null)
                return ShaderType.Toon;

            string shaderName = material.shader.name;

            // Toon variants (check for StandardToon mode first)
            for (int i = 0; i < ToonShaderNames.Length; i++)
            {
                if (shaderName == ToonShaderNames[i])
                {
                    // StandardToon uses same shader but with _ShadingMode >= 2
                    if (material.HasProperty("_ShadingMode") && material.GetFloat("_ShadingMode") >= 1.5f)
                        return ShaderType.StandardToon;
                    return ShaderType.Toon;
                }
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
                case ShaderType.StandardToon: return ToonShaderNames[0]; // same shader as Toon
                default: return ToonShaderNames[0];
            }
        }

        /// <summary>
        /// シェーダータイプを切り替える（Undo対応）
        /// Returns true on success, false on failure (with error dialog shown).
        /// </summary>
        public static bool SetShaderType(Material material, ShaderType type, MaterialEditor editor)
        {
            if (material == null)
            {
                NataneToonErrorDialog.ShowNullMaterialError(L("シェーダータイプの変更", "Change Shader Type"));
                return false;
            }

            Undo.RecordObject(material, "Change Shader Type");

            if (type == ShaderType.StandardToon)
            {
                // StandardToon uses the same Toon shader but with _ShadingMode = 2
                string newShaderName = ToonShaderNames[0];
                Shader newShader = Shader.Find(newShaderName);
                if (newShader == null)
                {
                    Debug.LogError($"[NataneToonShaderTypeSwitcher] Shader not found: {newShaderName}");
                    NataneToonErrorDialog.ShowShaderNotFoundError(newShaderName);
                    return false;
                }
                material.shader = newShader;
                material.SetFloat("_ShadingMode", 2.0f);
                material.EnableKeyword("_STANDARD_TOON");
            }
            else
            {
                string newShaderName = GetDefaultShaderName(type);
                Shader newShader = Shader.Find(newShaderName);

                if (newShader == null)
                {
                    Debug.LogError($"[NataneToonShaderTypeSwitcher] Shader not found: {newShaderName}");
                    NataneToonErrorDialog.ShowShaderNotFoundError(newShaderName);
                    return false;
                }

                if (material.shader == newShader && type != ShaderType.Toon) return true;

                material.shader = newShader;

                // Toon type: reset _ShadingMode if was StandardToon
                if (type == ShaderType.Toon && material.HasProperty("_ShadingMode"))
                {
                    if (material.GetFloat("_ShadingMode") >= 1.5f)
                    {
                        material.SetFloat("_ShadingMode", 0.0f);
                        material.DisableKeyword("_STANDARD_TOON");
                    }
                }
            }

            EditorUtility.SetDirty(material);

            if (editor != null)
            {
                editor.Repaint();
            }

            return true;
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
            ShaderType newType = (ShaderType)EditorGUILayout.EnumPopup(L("シェーダータイプ", "Shader Type"), currentType);

            if (EditorGUI.EndChangeCheck() && newType != currentType)
            {
                if (EditorUtility.DisplayDialog(
                    L("シェーダータイプ変更", "Change Shader Type"),
                    L("シェーダータイプを変更すると、現在の設定の一部が失われる可能性があります。\n続行しますか？",
                      "Changing the shader type may cause some current settings to be lost.\nContinue?"),
                    L("変更する", "Change"),
                    L("キャンセル", "Cancel")))
                {
                    bool success = SetShaderType(material, newType, editor);
                    if (success)
                    {
                        shouldReturn = true;
                        return true;
                    }
                    // 失敗時: ダイアログは SetShaderType 内で表示済み。UIは現在の状態を維持。
                }
            }

            return currentType != ShaderType.Toon && currentType != ShaderType.StandardToon;
        }
    }
}
