using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that declares a <c>CriticalAlert</c> when three
    /// danger conditions are simultaneously true:
    ///   1. Arena is under high pressure (<paramref name="isHighPressure"/>).
    ///   2. Threat level is <see cref="ThreatLevel.High"/>.
    ///   3. Player does NOT hold zone dominance (<paramref name="hasDominance"/> == false).
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="EvaluateAlert(bool, ThreatLevel, bool)"/> whenever any
    ///     of the three input conditions changes.
    ///   • <see cref="_onCriticalAlert"/>  fires on the false → true transition.
    ///   • <see cref="_onAlertCleared"/>   fires on the true  → false transition.
    ///   • Call <see cref="Reset"/> at match start (called automatically by OnEnable).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlAlertSystem.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlAlertSystem", order = 50)]
    public sealed class ZoneControlAlertSystemSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised on the false→true transition of IsCritical.")]
        [SerializeField] private VoidGameEvent _onCriticalAlert;

        [Tooltip("Raised on the true→false transition of IsCritical.")]
        [SerializeField] private VoidGameEvent _onAlertCleared;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool _isCritical;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// True while all three danger conditions are simultaneously met.
        /// </summary>
        public bool IsCritical => _isCritical;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the three danger conditions and fires transition events
        /// when the critical state changes.
        /// </summary>
        /// <param name="isHighPressure">True when the arena is under high pressure.</param>
        /// <param name="threat">Current assessed threat level.</param>
        /// <param name="hasDominance">True when the player holds zone dominance.</param>
        public void EvaluateAlert(bool isHighPressure, ThreatLevel threat, bool hasDominance)
        {
            bool newCritical = isHighPressure && threat >= ThreatLevel.High && !hasDominance;

            if (newCritical == _isCritical) return;

            _isCritical = newCritical;

            if (_isCritical)
                _onCriticalAlert?.Raise();
            else
                _onAlertCleared?.Raise();
        }

        /// <summary>
        /// Clears the critical state silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _isCritical = false;
        }
    }
}
