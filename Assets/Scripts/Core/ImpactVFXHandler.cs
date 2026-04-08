using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Listens to a DamageGameEvent SO channel and spawns impact-spark VFX at
    /// the world-space hit position carried in DamageInfo.hitPoint.
    ///
    /// Owns and manages a <see cref="ParticlePool"/> instance whose GameObjects
    /// are parented to this transform. No MonoBehaviour pool reference is required
    /// in the Inspector — the handler creates the pool from the prefab directly.
    ///
    /// Lives in BattleRobots.Core and references only Core types — safe to attach
    /// alongside Physics or UI components without violating the UI→Physics rule.
    ///
    /// Scene setup:
    ///   1. Add this component to the impact-VFX manager GameObject.
    ///   2. Assign _damageChannel (the DamageGameEvent SO fired on every hit).
    ///   3. Assign _impactPrefab (a ParticleSystem prefab for the spark effect).
    ///   4. Optionally adjust _poolSize (default: 8).
    ///
    /// The Action delegate is cached in Awake so OnEnable/OnDisable never allocate.
    /// </summary>
    public sealed class ImpactVFXHandler : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Event Channel")]
        [Tooltip("DamageGameEvent SO that fires on every hit in the arena.")]
        [SerializeField] private DamageGameEvent _damageChannel;

        [Header("VFX Pool")]
        [Tooltip("Particle prefab for impact sparks.")]
        [SerializeField] private ParticleSystem _impactPrefab;

        [Tooltip("Number of pooled spark instances. Excess hits are silently skipped.")]
        [SerializeField, Min(1)] private int _poolSize = 8;

        // ── State ──────────────────────────────────────────────────────────────

        private ParticlePool _pool;

        // Delegate cached in Awake — no allocation in OnEnable/OnDisable.
        private Action<DamageInfo> _onDamage;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_impactPrefab != null)
                _pool = new ParticlePool(_impactPrefab, _poolSize, transform);

            _onDamage = HandleDamage;
        }

        private void OnEnable()
        {
            _damageChannel?.RegisterCallback(_onDamage);
        }

        private void OnDisable()
        {
            _damageChannel?.UnregisterCallback(_onDamage);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }

        // ── Handler ────────────────────────────────────────────────────────────

        private void HandleDamage(DamageInfo info)
        {
            _pool?.Play(info.hitPoint, Quaternion.identity);
        }
    }
}
