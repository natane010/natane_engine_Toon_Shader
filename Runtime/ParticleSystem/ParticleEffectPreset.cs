using UnityEngine;
using System;

namespace NataneParticleSystem
{
    /// <summary>
    /// Particle Effect Preset - ScriptableObject for storing particle system configurations
    /// </summary>
    [CreateAssetMenu(fileName = "New Particle Preset", menuName = "Natane/Particle Effect Preset", order = 1)]
    public class ParticleEffectPreset : ScriptableObject
    {
        [Header("Preset Information")]
        public string presetName = "New Effect";
        [TextArea(3, 5)]
        public string description = "Particle effect description";
        public Sprite previewIcon;
        public EffectCategory category = EffectCategory.Custom;

        [Header("Main Module Settings")]
        public float duration = 5f;
        public bool looping = false;
        public float startLifetime = 2f;
        public float startSpeed = 5f;
        public float startSize = 1f;
        public Color startColor = Color.white;
        public float gravityModifier = 0f;
        public int maxParticles = 1000;

        [Header("Emission")]
        public float emissionRate = 10f;
        public bool useBurst = false;
        public int burstCount = 30;
        public float burstTime = 0f;

        [Header("Shape")]
        public ParticleSystemShapeType shapeType = ParticleSystemShapeType.Cone;
        public float shapeAngle = 25f;
        public float shapeRadius = 1f;

        [Header("Velocity Over Lifetime")]
        public bool useVelocityOverLifetime = false;
        public Vector3 velocityOverLifetime = Vector3.zero;

        [Header("Color Over Lifetime")]
        public bool useColorOverLifetime = false;
        public Gradient colorGradient;

        [Header("Size Over Lifetime")]
        public bool useSizeOverLifetime = false;
        public AnimationCurve sizeOverLifetime = AnimationCurve.Linear(0, 1, 1, 0);

        [Header("Rotation")]
        public bool useRotation = false;
        public float rotationSpeed = 45f;

        [Header("Texture")]
        public Material particleMaterial;
        public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;

        [Header("Trails")]
        public bool useTrails = false;
        public float trailLifetime = 1f;
        public float trailMinVertexDistance = 0.2f;
        public Material trailMaterial;

        /// <summary>
        /// Apply this preset to a ParticleSystem
        /// </summary>
        public void ApplyToParticleSystem(ParticleSystem ps)
        {
            if (ps == null) return;

            // Main Module
            var main = ps.main;
            main.duration = duration;
            main.loop = looping;
            main.startLifetime = startLifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.startColor = startColor;
            main.gravityModifier = gravityModifier;
            main.maxParticles = maxParticles;

            // Emission Module
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            // Clear existing bursts
            emission.burstCount = 0;

            if (useBurst)
            {
                var burst = new ParticleSystem.Burst(burstTime, burstCount);
                emission.SetBurst(0, burst);
            }

            // Shape Module
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = shapeType;
            shape.angle = shapeAngle;
            shape.radius = shapeRadius;

            // Velocity Over Lifetime Module
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = useVelocityOverLifetime;
            if (useVelocityOverLifetime)
            {
                velocity.x = velocityOverLifetime.x;
                velocity.y = velocityOverLifetime.y;
                velocity.z = velocityOverLifetime.z;
            }

            // Color Over Lifetime Module
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = useColorOverLifetime;
            if (useColorOverLifetime && colorGradient != null)
            {
                var gradient = new ParticleSystem.MinMaxGradient(colorGradient);
                colorOverLifetime.color = gradient;
            }

            // Size Over Lifetime Module
            var sizeOverLifetimeModule = ps.sizeOverLifetime;
            sizeOverLifetimeModule.enabled = useSizeOverLifetime;
            if (useSizeOverLifetime)
            {
                sizeOverLifetimeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeOverLifetime);
            }

            // Rotation Over Lifetime Module
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = useRotation;
            if (useRotation)
            {
                rotation.z = rotationSpeed * Mathf.Deg2Rad;
            }

            // Renderer
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = renderMode;
                if (particleMaterial != null)
                {
                    renderer.material = particleMaterial;
                }
            }

            // Trails Module
            var trails = ps.trails;
            trails.enabled = useTrails;
            if (useTrails)
            {
                trails.lifetime = trailLifetime;
                trails.minVertexDistance = trailMinVertexDistance;
                if (trailMaterial != null)
                {
                    renderer.trailMaterial = trailMaterial;
                }
            }
        }

        /// <summary>
        /// Create a new GameObject with ParticleSystem using this preset
        /// </summary>
        public GameObject CreateParticleEffect(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject go = new GameObject(presetName);
            go.transform.position = position;
            go.transform.rotation = rotation;
            if (parent != null)
            {
                go.transform.SetParent(parent);
            }

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ApplyToParticleSystem(ps);

            // Add auto-destroy component if not looping
            if (!looping)
            {
                go.AddComponent<ParticleAutoDestroy>();
            }

            return go;
        }
    }

    [Serializable]
    public enum EffectCategory
    {
        Explosion,
        Fire,
        Smoke,
        Magic,
        Water,
        Electric,
        Nature,
        Impact,
        Weather,
        Custom
    }
}
