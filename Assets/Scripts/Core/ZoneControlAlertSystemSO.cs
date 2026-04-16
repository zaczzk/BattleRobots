using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that declares a <c>CriticalAlert</c> when all three
    /// of the following conditions hold simultaneously:
    ///   • match pressure is high,
    ///   • bot threat level is <see cref="ThreatLevel.High"/>,
    ///   • the player does NOT have zone dominance.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="EvaluateAlert(bool, bool, bool)"/> after any of the
    ///     three input values changes.
    ///   • <see cref="_onCriticalAlert"/> fires on the false → true transition.
    ///   • <see cref="_onAlertCleared"/> fires on the true → false transition.
    ///   • Call <see cref="Reset"/> at match start (called automatically by OnEnable).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlAlertSystem.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlAlertSystem", order = 50)]
    public sealed class ZoneControlAlertSystemSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when the alert transitions from inactive to active.")]
        [SerializeField] private VoidGameEvent _onCriticalAlert;

        [Tooltip("Raised when the alert transitions from active to inactive.")]
        [SerializeField] private VoidGameEvent _onAlertCleared;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool _isCriticalAlert;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True when all three critical conditions are active.</summary>
        public bool IsCriticalAlert => _isCriticalAlert;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates whether all three critical conditions are simultaneously true.
        /// Fires <see cref="_onCriticalAlert"/> on false→true transition and
        /// <see cref="_onAlertCleared"/> on true→false transition.
        /// </summary>
        /// <param name="isHighPressure">True when match pressure is high.</param>
        /// <param name="isThreatHigh">True when bot threat level is High.</param>
        /// <param name="hasDominance">True when the player holds zone dominance.</param>
        public void EvaluateAlert(bool isHighPressure, bool isThreatHigh, bool hasDominance)
        {
            bool newAlert = isHighPressure && isThreatHigh && !hasDominance;

            if (newAlert == _isCriticalAlert) return;

            _isCriticalAlert = newAlert;

            if (_isCriticalAlert)
                _onCriticalAlert?.Raise();
            else
                _onAlertCleared?.Raise();
        }

        /// <summary>
        /// Clears the alert state silently. Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _isCriticalAlert = false;
        }
    }
}
