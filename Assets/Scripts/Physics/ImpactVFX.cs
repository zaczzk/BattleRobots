using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Spawns impact spark particles from a <see cref="ParticlePool"/> when an
    /// ArticulationBody part receives a collision above a configurable impulse threshold.
    ///
    /// Architecture constraints:
    ///   • Lives in <c>BattleRobots.Physics</c> — no UI references.
    ///   • ArticulationBody required; no Rigidbody.
    ///   • ParticlePool is pre-warmed in Awake — zero allocs in OnCollisionEnter.
    /// </summary>
    [RequireComponent(typeof(ArticulationBody))]
    public sealed class ImpactVFX : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Particle Prefab")]
        [Tooltip("ParticleSystem prefab for impact sparks.")]
        [SerializeField] private ParticleSystem _sparksPrefab;

        [Header("Pool")]
        [Tooltip("Number of concurrent spark effects supported before oldest is recycled.")]
        [SerializeField, Min(1)] private int _poolCapacity = 4;

        [Header("Trigger Threshold")]
        [Tooltip("Minimum collision impulse magnitude (N·s) required to trigger sparks.")]
        [SerializeField, Min(0f)] private float _impulseThreshold = 0.5f;

        // ── State ─────────────────────────────────────────────────────────────

        private ParticlePool _pool;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_sparksPrefab == null)
            {
                Debug.LogWarning($"[ImpactVFX] '{name}' — no spark prefab assigned. Effect disabled.", this);
                return;
            }

            _pool = new ParticlePool(_sparksPrefab, _poolCapacity, parent: null);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }

        // ── Collision ─────────────────────────────────────────────────────────

        private void OnCollisionEnter(Collision collision)
        {
            if (_pool == null) return;

            float impulse = collision.impulse.magnitude;
            if (impulse < _impulseThreshold) return;

            // Use the first contact point's position and the inverse of the normal.
            ContactPoint contact = collision.GetContact(0);
            Quaternion rotation  = Quaternion.LookRotation(-contact.normal);

            _pool.Play(contact.point, rotation);
        }
    }
}
