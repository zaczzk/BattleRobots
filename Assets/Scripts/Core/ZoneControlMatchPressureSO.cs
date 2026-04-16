using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that accumulates a pressure value when bots lead
    /// the player on the scoreboard, and decays it when the player retakes the lead.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="EvaluatePressure(bool)"/> after every scoreboard update,
    ///     passing <c>true</c> when the bots are ahead of the player.
    ///   • Pressure rises by <see cref="PressureIncrement"/> while bots lead and
    ///     decays by <see cref="PressureDecay"/> once the player retakes the lead.
    ///   • <see cref="_onHighPressure"/> fires the first time pressure crosses
    ///     <see cref="HighPressureThreshold"/> upwards.
    ///   • <see cref="_onPressureRelieved"/> fires the first time pressure drops
    ///     back below <see cref="HighPressureThreshold"/> after a high-pressure state.
    ///   • Call <see cref="Reset"/> at match start (called automatically by OnEnable).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchPressure.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchPressure", order = 45)]
    public sealed class ZoneControlMatchPressureSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Pressure Settings")]
        [Tooltip("How much pressure increases per evaluation while bots lead (0.1–1).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _pressureIncrement = 0.25f;

        [Tooltip("How much pressure decays per evaluation while player leads (0.1–1).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _pressureDecay = 0.25f;

        [Tooltip("Pressure level (0–1) at which high-pressure is declared.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _highPressureThreshold = 0.75f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised the first time pressure crosses the high-pressure threshold upwards.")]
        [SerializeField] private VoidGameEvent _onHighPressure;

        [Tooltip("Raised the first time pressure drops below the threshold after a high-pressure state.")]
        [SerializeField] private VoidGameEvent _onPressureRelieved;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _pressure;
        private bool  _isHighPressure;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current pressure value in the range [0, 1].</summary>
        public float Pressure => _pressure;

        /// <summary>True when pressure is at or above <see cref="HighPressureThreshold"/>.</summary>
        public bool IsHighPressure => _isHighPressure;

        /// <summary>The threshold at which high-pressure is declared.</summary>
        public float HighPressureThreshold => _highPressureThreshold;

        /// <summary>Pressure increase per evaluation while bots lead.</summary>
        public float PressureIncrement => _pressureIncrement;

        /// <summary>Pressure decay per evaluation while player leads.</summary>
        public float PressureDecay => _pressureDecay;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates pressure based on whether bots currently lead the player.
        /// Increases pressure when <paramref name="botLeads"/> is true; decreases
        /// otherwise.  Fires <see cref="_onHighPressure"/> or
        /// <see cref="_onPressureRelieved"/> at threshold crossings.
        /// </summary>
        /// <param name="botLeads">True if any bot has a higher score than the player.</param>
        public void EvaluatePressure(bool botLeads)
        {
            if (botLeads)
                IncreasePressure();
            else
                DecreasePressure();
        }

        /// <summary>
        /// Resets pressure to zero and clears the high-pressure flag silently.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _pressure      = 0f;
            _isHighPressure = false;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void IncreasePressure()
        {
            _pressure = Mathf.Clamp01(_pressure + _pressureIncrement);

            if (!_isHighPressure && _pressure >= _highPressureThreshold)
            {
                _isHighPressure = true;
                _onHighPressure?.Raise();
            }
        }

        private void DecreasePressure()
        {
            _pressure = Mathf.Clamp01(_pressure - _pressureDecay);

            if (_isHighPressure && _pressure < _highPressureThreshold)
            {
                _isHighPressure = false;
                _onPressureRelieved?.Raise();
            }
        }
    }
}
