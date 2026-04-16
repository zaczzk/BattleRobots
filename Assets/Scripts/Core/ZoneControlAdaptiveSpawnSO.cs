using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that adjusts the bot/loot spawn interval based on
    /// the current match pressure state.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="SetHighPressure(bool)"/> when the pressure state changes.
    ///   • <see cref="CurrentSpawnRate"/> switches between
    ///     <see cref="HighPressureSpawnRate"/> and <see cref="LowPressureSpawnRate"/>
    ///     depending on the pressure flag.
    ///   • <see cref="_onSpawnRateChanged"/> fires only when the rate actually changes.
    ///   • Call <see cref="Reset"/> at match start (called automatically by OnEnable).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlAdaptiveSpawn.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlAdaptiveSpawn", order = 48)]
    public sealed class ZoneControlAdaptiveSpawnSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Spawn Rate Settings")]
        [Tooltip("Baseline spawn interval (seconds) used at match start before any pressure event.")]
        [Min(0.1f)]
        [SerializeField] private float _baseSpawnRate = 5f;

        [Tooltip("Spawn interval (seconds) used while match pressure is high — smaller = faster spawns.")]
        [Min(0.1f)]
        [SerializeField] private float _highPressureSpawnRate = 2f;

        [Tooltip("Spawn interval (seconds) used while match pressure is low — larger = slower spawns.")]
        [Min(0.1f)]
        [SerializeField] private float _lowPressureSpawnRate = 8f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when the spawn rate changes.")]
        [SerializeField] private VoidGameEvent _onSpawnRateChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _currentSpawnRate;
        private bool  _isHighPressure;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Active spawn interval in seconds.</summary>
        public float CurrentSpawnRate => _currentSpawnRate;

        /// <summary>True when high-pressure mode is active.</summary>
        public bool IsHighPressure => _isHighPressure;

        /// <summary>Baseline spawn rate used at match start.</summary>
        public float BaseSpawnRate => _baseSpawnRate;

        /// <summary>Spawn rate used during high-pressure phases.</summary>
        public float HighPressureSpawnRate => _highPressureSpawnRate;

        /// <summary>Spawn rate used during low-pressure phases.</summary>
        public float LowPressureSpawnRate => _lowPressureSpawnRate;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the pressure state and, if it changed, adjusts
        /// <see cref="CurrentSpawnRate"/> and fires <see cref="_onSpawnRateChanged"/>.
        /// </summary>
        public void SetHighPressure(bool isHighPressure)
        {
            if (_isHighPressure == isHighPressure) return;

            _isHighPressure   = isHighPressure;
            _currentSpawnRate = isHighPressure ? _highPressureSpawnRate : _lowPressureSpawnRate;
            _onSpawnRateChanged?.Raise();
        }

        /// <summary>
        /// Resets spawn rate to <see cref="BaseSpawnRate"/> and clears the pressure flag
        /// silently. Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _currentSpawnRate = _baseSpawnRate;
            _isHighPressure   = false;
        }
    }
}
