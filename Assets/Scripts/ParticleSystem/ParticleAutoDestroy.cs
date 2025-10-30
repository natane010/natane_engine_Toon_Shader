using UnityEngine;

namespace NataneParticleSystem
{
    /// <summary>
    /// Automatically destroys the GameObject when the particle system has finished playing
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleAutoDestroy : MonoBehaviour
    {
        private ParticleSystem ps;
        private float checkInterval = 0.5f;
        private float nextCheckTime = 0f;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
        }

        private void Update()
        {
            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + checkInterval;

                // Check if particle system has stopped and all particles are gone
                if (!ps.IsAlive(true))
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
