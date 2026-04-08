using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Listens to a VoidGameEvent death channel (the SO wired to HealthSO._onDeath)
    /// and spawns an explosion particle effect at this GameObject's world position.
    ///
    /// Owns and manages a <see cref="ParticlePool"/> instance parented to this transform.
    /// Attach to the robot root GameObject alongside DamageReceiver; assign the same
    /// VoidGameEvent SO that HealthSO._onDeath uses.
    ///
    /// Scene setup:
    ///   1. Add this component to the robot root (or a dedicated VFX manager GO).
    ///   2. Assign _deathChannel (the VoidGameEvent SO wired as HealthSO._onDeath).
    ///   3. Assign _explosionPrefab (a ParticleSystem prefab for the explosion).
    ///   4. Optionally adjust _poolSize (default: 4 — one explosion per robot death).
    ///
    /// The Action delegate is cached in Awake so OnEnable/OnDisable never allocate.
    /// </summary>
    public sealed class DestructionVFXHandler : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Event Channel")]
        [Tooltip("VoidGameEvent SO that fires when this robot's HealthSO reaches zero.")]
        [SerializeField] private VoidGameEvent _deathChannel;

        [Header("VFX Pool")]
        [Tooltip("Particle prefab for the destruction explosion.")]
        [SerializeField] private ParticleSystem _explosionPrefab;

        [Tooltip("Number of pooled explosion instances. One is usually sufficient per robot.")]
        [SerializeField, Min(1)] private int _poolSize = 4;

        // ── State ──────────────────────────────────────────────────────────────

        private ParticlePool _pool;

        // Delegate cached in Awake — no allocation in OnEnable/OnDisable.
        private Action _onDeath;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_explosionPrefab != null)
                _pool = new ParticlePool(_explosionPrefab, _poolSize, transform);

            _onDeath = HandleDeath;
        }

        private void OnEnable()
        {
            _deathChannel?.RegisterCallback(_onDeath);
        }

        private void OnDisable()
        {
            _deathChannel?.UnregisterCallback(_onDeath);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }

        // ── Handler ────────────────────────────────────────────────────────────

        private void HandleDeath()
        {
            _pool?.Play(transform.position, Quaternion.identity);
        }
    }
}
