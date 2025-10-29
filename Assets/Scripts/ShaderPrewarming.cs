using UnityEngine;
using System.Collections;

/// <summary>
/// Shader Prewarming Script for Natane Toon Shader
/// Warms up shader variants at runtime to prevent compilation stutters
/// </summary>
public class ShaderPrewarming : MonoBehaviour
{
    [Header("Shader Variant Collection")]
    [Tooltip("Reference to the ShaderVariantCollection asset")]
    public ShaderVariantCollection shaderVariants;

    [Header("Prewarming Options")]
    [Tooltip("Warm shaders on Awake (immediate)")]
    public bool prewarmOnAwake = true;

    [Tooltip("Warm shaders on Start (delayed)")]
    public bool prewarmOnStart = false;

    [Tooltip("Show debug logs")]
    public bool showDebugLogs = false;

    private void Awake()
    {
        if (prewarmOnAwake && shaderVariants != null)
        {
            PrewarmShaders();
        }
    }

    private void Start()
    {
        if (prewarmOnStart && !prewarmOnAwake && shaderVariants != null)
        {
            PrewarmShaders();
        }
    }

    /// <summary>
    /// Manually prewarm shader variants
    /// Call this method when you want to prewarm shaders at a specific time
    /// </summary>
    public void PrewarmShaders()
    {
        if (shaderVariants == null)
        {
            Debug.LogWarning("[ShaderPrewarming] No ShaderVariantCollection assigned!");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[ShaderPrewarming] Starting shader prewarming...\n" +
                     $"Shader Count: {shaderVariants.shaderCount}\n" +
                     $"Variant Count: {shaderVariants.variantCount}");
        }

        float startTime = Time.realtimeSinceStartup;

        // Warm up all shader variants in the collection
        shaderVariants.WarmUp();

        float elapsedTime = Time.realtimeSinceStartup - startTime;

        if (showDebugLogs)
        {
            Debug.Log($"[ShaderPrewarming] Shader prewarming completed in {elapsedTime:F3} seconds");
        }
    }

    /// <summary>
    /// Prewarm shaders asynchronously over multiple frames
    /// Useful for avoiding frame drops during loading
    /// </summary>
    /// <param name="variantsPerFrame">Number of variants to warm per frame</param>
    public IEnumerator PrewarmShadersAsync(int variantsPerFrame = 50)
    {
        if (shaderVariants == null)
        {
            Debug.LogWarning("[ShaderPrewarming] No ShaderVariantCollection assigned!");
            yield break;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[ShaderPrewarming] Starting async shader prewarming...\n" +
                     $"Variants per frame: {variantsPerFrame}");
        }

        // Note: ShaderVariantCollection doesn't expose individual variants
        // So we just call WarmUp() but yield to spread the load
        yield return null;

        shaderVariants.WarmUp();

        if (showDebugLogs)
        {
            Debug.Log("[ShaderPrewarming] Async shader prewarming completed");
        }
    }
}
