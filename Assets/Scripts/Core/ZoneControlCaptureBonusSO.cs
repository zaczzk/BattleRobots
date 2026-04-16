using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that awards bonus currency when cumulative zone captures
    /// exceed a configurable threshold.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="EvaluateBonus(int)"/> at match end with the total capture count.
    ///   Returns the bonus amount (0 when below or at threshold).
    ///   Accumulates across multiple calls in <see cref="TotalBonusAwarded"/>.
    ///   Call <see cref="Reset"/> to clear the accumulated total.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at session start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBonus", order = 51)]
    public sealed class ZoneControlCaptureBonusSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Bonus Settings")]
        [Tooltip("Minimum captures before any bonus is awarded.")]
        [Min(1)]
        [SerializeField] private int _captureThreshold = 3;

        [Tooltip("Currency awarded per capture above the threshold.")]
        [Min(0)]
        [SerializeField] private int _bonusPerCapture = 50;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised whenever a non-zero bonus is awarded.")]
        [SerializeField] private VoidGameEvent _onBonusAwarded;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _totalBonusAwarded;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Minimum captures required before any bonus is awarded.</summary>
        public int CaptureThreshold  => _captureThreshold;

        /// <summary>Currency awarded per capture above the threshold.</summary>
        public int BonusPerCapture   => _bonusPerCapture;

        /// <summary>Total bonus currency accumulated across all EvaluateBonus calls.</summary>
        public int TotalBonusAwarded => _totalBonusAwarded;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the bonus for <paramref name="captureCount"/> captures.
        /// Returns 0 when count is at or below the threshold.
        /// Accumulates into <see cref="TotalBonusAwarded"/> and fires
        /// <see cref="_onBonusAwarded"/> when a non-zero bonus is awarded.
        /// </summary>
        public int EvaluateBonus(int captureCount)
        {
            if (captureCount <= _captureThreshold) return 0;

            int bonus = (captureCount - _captureThreshold) * _bonusPerCapture;
            _totalBonusAwarded += bonus;
            _onBonusAwarded?.Raise();
            return bonus;
        }

        /// <summary>
        /// Clears the accumulated total silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _totalBonusAwarded = 0;
        }

        private void OnValidate()
        {
            _captureThreshold = Mathf.Max(1, _captureThreshold);
            _bonusPerCapture  = Mathf.Max(0, _bonusPerCapture);
        }
    }
}
