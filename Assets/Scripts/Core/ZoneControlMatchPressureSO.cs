using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that models the match-pressure felt by the player
    /// when bots are leading on the zone-control scoreboard.
    ///
    /// Call <see cref="Tick"/> from a MonoBehaviour's Update loop, passing whether
    /// bots currently hold the lead.  Pressure rises when bots lead and decays
    /// when the player leads.  Threshold-crossing events fire once per transition
    /// to avoid spam.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on Tick hot path.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchPressure.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchPressure", order = 45)]
    public sealed class ZoneControlMatchPressureSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Pressure Settings")]
        [Tooltip("Rate at which pressure increases per second when bots are leading.")]
        [Min(0f)]
        [SerializeField] private float _pressureIncreaseRate = 0.1f;

        [Tooltip("Rate at which pressure decays per second when the player is leading.")]
        [Min(0f)]
        [SerializeField] private float _pressureDecayRate = 0.05f;

        [Tooltip("Pressure level [0,1] above which _onHighPressure fires.")]
        [Range(0f, 1f)]
        [SerializeField] private float _highPressureThreshold = 0.8f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once when Pressure crosses above the high-pressure threshold.")]
        [SerializeField] private VoidGameEvent _onHighPressure;

        [Tooltip("Raised once when Pressure crosses back below the high-pressure threshold.")]
        [SerializeField] private VoidGameEvent _onPressureRelieved;

        [Tooltip("Raised on every Tick call (pressure changed).")]
        [SerializeField] private VoidGameEvent _onPressureChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _pressure;
        private bool  _wasHighPressure;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current pressure value in [0, 1].</summary>
        public float Pressure => _pressure;

        /// <summary>True when <see cref="Pressure"/> is at or above the high-pressure threshold.</summary>
        public bool IsHighPressure => _pressure >= _highPressureThreshold;

        /// <summary>Configured high-pressure threshold (serialised).</summary>
        public float HighPressureThreshold => _highPressureThreshold;

        /// <summary>Rate at which pressure increases per second when bots lead.</summary>
        public float PressureIncreaseRate => _pressureIncreaseRate;

        /// <summary>Rate at which pressure decays per second when player leads.</summary>
        public float PressureDecayRate => _pressureDecayRate;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the pressure simulation by <paramref name="dt"/> seconds.
        /// Pressure rises when <paramref name="botIsLeading"/> is <c>true</c> and
        /// decays otherwise.  Fires threshold-crossing events at most once per
        /// transition.  Always fires <see cref="_onPressureChanged"/>.
        /// </summary>
        public void Tick(float dt, bool botIsLeading)
        {
            if (botIsLeading)
                _pressure = Mathf.Clamp01(_pressure + _pressureIncreaseRate * dt);
            else
                _pressure = Mathf.Clamp01(_pressure - _pressureDecayRate * dt);

            _onPressureChanged?.Raise();

            bool isHigh = _pressure >= _highPressureThreshold;
            if (isHigh && !_wasHighPressure)
                _onHighPressure?.Raise();
            else if (!isHigh && _wasHighPressure)
                _onPressureRelieved?.Raise();

            _wasHighPressure = isHigh;
        }

        /// <summary>
        /// Resets pressure to zero silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _pressure        = 0f;
            _wasHighPressure = false;
        }
    }
}
