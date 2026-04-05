using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Plays a destruction explosion effect when the robot's <see cref="HealthSO"/>
    /// fires its death event.
    ///
    /// Wire-up:
    ///   1. Assign <c>_explosionPrefab</c> in the Inspector.
    ///   2. Add a <see cref="VoidGameEventListener"/> component to the same GO,
    ///      set its event to the robot's HealthSO._onDeath VoidGameEvent,
    ///      and wire the UnityEvent response to this component's <c>OnRobotDeath()</c>.
    ///
    /// Architecture constraints:
    ///   • Lives in <c>BattleRobots.Physics</c> — no UI references.
    ///   • ParticlePool pre-warmed in Awake — zero allocs on explosion.
    ///   • No Update override.
    /// </summary>
    public sealed class DestructionVFX : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Particle Prefab")]
        [Tooltip("ParticleSystem prefab for the destruction explosion.")]
        [SerializeField] private ParticleSystem _explosionPrefab;

        [Header("Pool")]
        [Tooltip("Pool capacity (usually 1 per robot, but > 1 for demo/stress testing).")]
        [SerializeField, Min(1)] private int _poolCapacity = 2;

        [Header("Spawn Offset")]
        [Tooltip("World-space offset relative to this transform where the explosion appears.")]
        [SerializeField] private Vector3 _offset = Vector3.zero;

        // ── State ─────────────────────────────────────────────────────────────

        private ParticlePool _pool;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_explosionPrefab == null)
            {
                Debug.LogWarning($"[DestructionVFX] '{name}' — no explosion prefab assigned. Effect disabled.", this);
                return;
            }

            _pool = new ParticlePool(_explosionPrefab, _poolCapacity, parent: null);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }

        // ── Public API (wired via VoidGameEventListener UnityEvent) ──────────

        /// <summary>
        /// Triggers the explosion at this transform's position plus <see cref="_offset"/>.
        /// Wire this to the HealthSO _onDeath VoidGameEvent via a
        /// <see cref="VoidGameEventListener"/> component in the Inspector.
        /// </summary>
        public void OnRobotDeath()
        {
            if (_pool == null) return;

            Vector3 worldPos = transform.position + _offset;
            _pool.Play(worldPos, Quaternion.identity);
        }
    }
}
