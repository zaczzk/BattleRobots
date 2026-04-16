using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that adjusts bot/loot spawn rate based on whether
    /// the arena is currently in a high-pressure state.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="SetHighPressure(bool)"/> when
    ///     <see cref="ZoneControlMatchPressureSO._onHighPressure"/> or
    ///     <see cref="ZoneControlMatchPressureSO._onPressureRelieved"/> fires.
    ///   • <see cref="CurrentSpawnInterval"/> returns <see cref="PressureSpawnInterval"/>
    ///     while under high pressure, otherwise <see cref="BaseSpawnInterval"/>.
    ///   • <see cref="_onSpawnRateChanged"/> fires whenever the pressure flag changes.
    ///   • Call <see cref="Reset"/> at match start (called automatically by OnEnable).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlAdaptiveSpawn.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlAdaptiveSpawn", order = 48)]
    public sealed class ZoneControlAdaptiveSpawnSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Spawn Intervals")]
        [Tooltip("Spawn interval in seconds under normal (low) pressure.")]
        [Min(0.1f)]
        [SerializeField] private float _baseSpawnInterval = 5f;

        [Tooltip("Spawn interval in seconds when arena is under high pressure " +
                 "(typically shorter to add challenge).")]
        [Min(0.1f)]
        [SerializeField] private float _pressureSpawnInterval = 2f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised whenever the spawn rate changes due to a pressure transition.")]
        [SerializeField] private VoidGameEvent _onSpawnRateChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool _isHighPressure;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Spawn interval under normal pressure.</summary>
        public float BaseSpawnInterval => _baseSpawnInterval;

        /// <summary>Spawn interval under high pressure.</summary>
        public float PressureSpawnInterval => _pressureSpawnInterval;

        /// <summary>True while the arena is under high pressure.</summary>
        public bool IsHighPressure => _isHighPressure;

        /// <summary>
        /// The currently active spawn interval: <see cref="PressureSpawnInterval"/>
        /// during high pressure, otherwise <see cref="BaseSpawnInterval"/>.
        /// </summary>
        public float CurrentSpawnInterval =>
            _isHighPressure ? _pressureSpawnInterval : _baseSpawnInterval;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Transitions the pressure state and fires <see cref="_onSpawnRateChanged"/>
        /// when the flag actually changes.
        /// </summary>
        public void SetHighPressure(bool value)
        {
            if (value == _isHighPressure) return;

            _isHighPressure = value;
            _onSpawnRateChanged?.Raise();
        }

        /// <summary>
        /// Clears the pressure flag silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _isHighPressure = false;
        }
    }
}
