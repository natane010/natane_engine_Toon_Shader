using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NataneParticleSystem
{
    /// <summary>
    /// Spawns particle effects from presets
    /// </summary>
    public class ParticleEffectSpawner : MonoBehaviour
    {
        [Header("Effect Library")]
        [SerializeField] private List<ParticleEffectPreset> effectPresets = new List<ParticleEffectPreset>();

        [Header("Spawn Settings")]
        [SerializeField] private bool spawnOnStart = false;
        [SerializeField] private ParticleEffectPreset defaultEffect;
        [SerializeField] private float spawnDelay = 0f;

        private Dictionary<string, ParticleEffectPreset> presetCache = new Dictionary<string, ParticleEffectPreset>();

        private void Awake()
        {
            // Cache presets for faster lookup
            foreach (var preset in effectPresets)
            {
                if (preset != null && !presetCache.ContainsKey(preset.presetName))
                {
                    presetCache.Add(preset.presetName, preset);
                }
            }
        }

        private void Start()
        {
            if (spawnOnStart && defaultEffect != null)
            {
                if (spawnDelay > 0)
                {
                    StartCoroutine(SpawnDefaultEffectDelayed(spawnDelay));
                }
                else
                {
                    SpawnDefaultEffect();
                }
            }
        }

        private IEnumerator SpawnDefaultEffectDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnDefaultEffect();
        }

        /// <summary>
        /// Spawn the default effect
        /// </summary>
        public GameObject SpawnDefaultEffect()
        {
            if (defaultEffect == null)
            {
                Debug.LogWarning("No default effect set!");
                return null;
            }

            return SpawnEffect(defaultEffect, transform.position, transform.rotation);
        }

        /// <summary>
        /// Spawn effect by preset name
        /// </summary>
        public GameObject SpawnEffect(string presetName)
        {
            if (presetCache.TryGetValue(presetName, out ParticleEffectPreset preset))
            {
                return SpawnEffect(preset, transform.position, transform.rotation);
            }

            Debug.LogWarning($"Preset '{presetName}' not found!");
            return null;
        }

        /// <summary>
        /// Spawn effect at specific position
        /// </summary>
        public GameObject SpawnEffect(string presetName, Vector3 position)
        {
            if (presetCache.TryGetValue(presetName, out ParticleEffectPreset preset))
            {
                return SpawnEffect(preset, position, Quaternion.identity);
            }

            Debug.LogWarning($"Preset '{presetName}' not found!");
            return null;
        }

        /// <summary>
        /// Spawn effect with full control
        /// </summary>
        public GameObject SpawnEffect(ParticleEffectPreset preset, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (preset == null)
            {
                Debug.LogWarning("Preset is null!");
                return null;
            }

            return preset.CreateParticleEffect(position, rotation, parent);
        }

        /// <summary>
        /// Add preset to library
        /// </summary>
        public void AddPreset(ParticleEffectPreset preset)
        {
            if (preset != null && !effectPresets.Contains(preset))
            {
                effectPresets.Add(preset);
                if (!presetCache.ContainsKey(preset.presetName))
                {
                    presetCache.Add(preset.presetName, preset);
                }
            }
        }

        /// <summary>
        /// Get all available preset names
        /// </summary>
        public List<string> GetAvailablePresets()
        {
            return new List<string>(presetCache.Keys);
        }

        #if UNITY_EDITOR
        // Editor helper: Spawn effect at scene view position
        [ContextMenu("Test Spawn Effect")]
        private void TestSpawnEffect()
        {
            SpawnDefaultEffect();
        }
        #endif
    }
}
