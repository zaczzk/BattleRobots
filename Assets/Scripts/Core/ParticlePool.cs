using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Generic pooled-particle helper. Pre-warms <see cref="_poolSize"/> instances at Awake
    /// and recycles them via a coroutine-free, Update-driven lifetime timer.
    ///
    /// Usage:
    ///   1. Assign a ParticleSystem prefab in the Inspector.
    ///   2. Call <see cref="Play"/> from any component (VFX handlers, physics, etc.).
    ///   3. Instances auto-return to the pool once their cached lifetime expires.
    ///
    /// Zero heap allocations in the hot path:
    ///   • Pool uses a pre-capacity Stack — no resize after Awake.
    ///   • Active-entry tracking uses a fixed-size struct array with swap-remove.
    ///   • No coroutines, no LINQ, no boxing.
    ///
    /// Lives in BattleRobots.Core so both BattleRobots.VFX and BattleRobots.UI can
    /// reference it without violating the UI→Physics dependency ban.
    ///
    /// Create via Assets ▶ GameObject ▶ add component, or place on a scene manager.
    /// </summary>
    public sealed class ParticlePool : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("ParticleSystem prefab to pool. Must not be null at Awake.")]
        [SerializeField] private ParticleSystem _prefab;

        [Tooltip("Number of instances to pre-warm at Awake. Also the maximum concurrent active count.")]
        [SerializeField, Min(1)] private int _poolSize = 8;

        // ── Internal structs ───────────────────────────────────────────────────

        // Stores an active instance and the time remaining before it should be returned.
        // Struct kept small (1 reference + 1 float = ~12 bytes) to keep cache pressure low.
        private struct ActiveEntry
        {
            public ParticleSystem Ps;
            public float TimeRemaining;
        }

        // ── Runtime state ──────────────────────────────────────────────────────

        // Pre-capacity Stack — no internal resize after Awake fills it.
        private Stack<ParticleSystem> _pool;

        // Fixed-size array for active entries; swap-remove keeps it dense and alloc-free.
        private ActiveEntry[] _active;
        private int _activeCount;

        // Cached from prefab's MainModule at Awake — never read per-frame.
        private float _cachedLifetime;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (_prefab == null)
            {
                Debug.LogWarning("[ParticlePool] No prefab assigned — pool disabled.", this);
                return;
            }

            // Cache duration + max start-lifetime from the prefab once.
            // Add a small buffer so all particles finish before we reclaim the instance.
            var main = _prefab.main;
            _cachedLifetime = main.duration + main.startLifetime.constantMax + 0.1f;

            // Allocate collections with exact capacity — no resize ever in hot path.
            _pool   = new Stack<ParticleSystem>(_poolSize);
            _active = new ActiveEntry[_poolSize];

            // Pre-warm
            for (int i = 0; i < _poolSize; i++)
            {
                var ps = Instantiate(_prefab, transform);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                _pool.Push(ps);
            }
        }

        private void Update()
        {
            // Zero alloc: iterate fixed struct array, decrement timers, swap-remove expired.
            float dt = Time.deltaTime;
            for (int i = _activeCount - 1; i >= 0; i--)
            {
                _active[i].TimeRemaining -= dt;
                if (_active[i].TimeRemaining <= 0f)
                {
                    _active[i].Ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    _active[i].Ps.gameObject.SetActive(false);
                    _pool.Push(_active[i].Ps);

                    // Swap-remove: copy last entry over this slot, shrink count.
                    // Struct copy is stack-allocated — no heap alloc.
                    _active[i] = _active[--_activeCount];
                }
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Plays a pooled particle effect at <paramref name="position"/> / <paramref name="rotation"/>.
        /// Silently no-ops if the pool is exhausted or unpopulated (no alloc, no exception).
        /// </summary>
        public void Play(Vector3 position, Quaternion rotation)
        {
            if (_pool == null || _pool.Count == 0) return;

            var ps = _pool.Pop();
            ps.transform.SetPositionAndRotation(position, rotation);
            ps.gameObject.SetActive(true);
            ps.Play();

            // Record active entry for timer-based return.
            // _active is pre-sized to _poolSize; _activeCount can never exceed it because
            // we only play an instance after a successful pop from the same-sized pool.
            _active[_activeCount].Ps            = ps;
            _active[_activeCount].TimeRemaining = _cachedLifetime;
            _activeCount++;
        }
    }
}
