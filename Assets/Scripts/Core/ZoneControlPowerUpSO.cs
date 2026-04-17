using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that manages a periodic in-match power-up spawn cycle.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="Tick"/> accumulates delta time; when the accumulated time reaches
    ///   <see cref="SpawnInterval"/>, <see cref="SpawnPowerUp"/> is called and the
    ///   accumulator resets.  <see cref="CollectPowerUp"/> increments the
    ///   <see cref="TotalCollected"/> counter and fires <c>_onPowerUpCollected</c>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlPowerUp.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlPowerUp", order = 66)]
    public sealed class ZoneControlPowerUpSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Time in seconds between power-up spawns.")]
        [Min(1f)]
        [SerializeField] private float _spawnInterval = 15f;

        [Tooltip("Bonus currency awarded when the player collects a power-up.")]
        [Min(0)]
        [SerializeField] private int _powerUpBonus = 100;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time a power-up spawns.")]
        [SerializeField] private VoidGameEvent _onPowerUpSpawned;

        [Tooltip("Raised each time the player collects a power-up.")]
        [SerializeField] private VoidGameEvent _onPowerUpCollected;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _accumulated;
        private int   _totalCollected;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Seconds between consecutive power-up spawns.</summary>
        public float SpawnInterval => _spawnInterval;

        /// <summary>Currency bonus awarded on each collection.</summary>
        public int PowerUpBonus => _powerUpBonus;

        /// <summary>Accumulated time towards the next spawn, in seconds.</summary>
        public float Accumulated => _accumulated;

        /// <summary>Total power-ups collected this match.</summary>
        public int TotalCollected => _totalCollected;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the spawn timer by <paramref name="dt"/> seconds.
        /// Fires <see cref="SpawnPowerUp"/> and resets the accumulator each time
        /// the interval is reached.  Zero allocation.
        /// </summary>
        public void Tick(float dt)
        {
            _accumulated += dt;
            if (_accumulated >= _spawnInterval)
            {
                _accumulated -= _spawnInterval;
                SpawnPowerUp();
            }
        }

        /// <summary>
        /// Fires <c>_onPowerUpSpawned</c>.  Can be called directly to force a spawn.
        /// Zero allocation.
        /// </summary>
        public void SpawnPowerUp()
        {
            _onPowerUpSpawned?.Raise();
        }

        /// <summary>
        /// Increments <see cref="TotalCollected"/> and fires <c>_onPowerUpCollected</c>.
        /// Zero allocation.
        /// </summary>
        public void CollectPowerUp()
        {
            _totalCollected++;
            _onPowerUpCollected?.Raise();
        }

        /// <summary>
        /// Clears accumulated time and collection count silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _accumulated    = 0f;
            _totalCollected = 0;
        }

        private void OnValidate()
        {
            _spawnInterval = Mathf.Max(1f, _spawnInterval);
            _powerUpBonus  = Mathf.Max(0, _powerUpBonus);
        }
    }
}
