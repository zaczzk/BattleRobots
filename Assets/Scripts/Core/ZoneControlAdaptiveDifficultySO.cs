using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that stores an adaptive capture-duration value and
    /// adjusts it up or down based on a 1–5 star match rating.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • A higher <see cref="CurrentDuration"/> makes zones harder to capture
    ///     (player must stand in the zone longer).
    ///   • <see cref="AdjustFromRating"/>:
    ///       rating ≥ 4 (player performing well) → increase by <c>_adjustStep</c>
    ///       rating ≤ 2 (player struggling)      → decrease by <c>_adjustStep</c>
    ///       rating == 3 (average)               → no change
    ///     The result is always clamped to [MinCaptureDuration, MaxCaptureDuration].
    ///   • <see cref="Initialize"/> sets the baseline duration (e.g. from
    ///     <c>ZoneControlDifficultyScalerSO</c>) before the first match.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Initialize at startup.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlAdaptiveDifficulty.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlAdaptiveDifficulty", order = 28)]
    public sealed class ZoneControlAdaptiveDifficultySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Adjustment Settings")]
        [Tooltip("Amount by which CurrentDuration is increased or decreased per adjustment.")]
        [SerializeField, Min(0.1f)] private float _adjustStep = 1f;

        [Tooltip("Minimum allowed capture duration (seconds).")]
        [SerializeField, Min(0.1f)] private float _minCaptureDuration = 1f;

        [Tooltip("Maximum allowed capture duration (seconds).")]
        [SerializeField, Min(1f)]   private float _maxCaptureDuration = 30f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by Initialize, AdjustFromRating, and Reset.")]
        [SerializeField] private VoidGameEvent _onDifficultyAdjusted;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _currentDuration;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current adaptive capture duration in seconds.</summary>
        public float CurrentDuration => _currentDuration;

        /// <summary>Amount added or subtracted per difficulty adjustment.</summary>
        public float AdjustStep => _adjustStep;

        /// <summary>Minimum allowed capture duration.</summary>
        public float MinCaptureDuration => _minCaptureDuration;

        /// <summary>Maximum allowed capture duration.</summary>
        public float MaxCaptureDuration => _maxCaptureDuration;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the baseline capture duration, clamped to [MinCaptureDuration,
        /// MaxCaptureDuration], and fires <see cref="_onDifficultyAdjusted"/>.
        /// Call at match start or on game boot.
        /// </summary>
        public void Initialize(float baseDuration)
        {
            _currentDuration = Mathf.Clamp(baseDuration, _minCaptureDuration, _maxCaptureDuration);
            _onDifficultyAdjusted?.Raise();
        }

        /// <summary>
        /// Adjusts <see cref="CurrentDuration"/> based on a 1–5 star
        /// <paramref name="rating"/>:
        ///   ≥ 4 → increase by <see cref="AdjustStep"/> (harder)
        ///   ≤ 2 → decrease by <see cref="AdjustStep"/> (easier)
        ///    3  → no change
        /// Result is clamped to [MinCaptureDuration, MaxCaptureDuration].
        /// Fires <see cref="_onDifficultyAdjusted"/>.
        /// </summary>
        public void AdjustFromRating(int rating)
        {
            if (rating >= 4)
                _currentDuration = Mathf.Min(_maxCaptureDuration, _currentDuration + _adjustStep);
            else if (rating <= 2)
                _currentDuration = Mathf.Max(_minCaptureDuration, _currentDuration - _adjustStep);

            _onDifficultyAdjusted?.Raise();
        }

        /// <summary>
        /// Resets <see cref="CurrentDuration"/> to zero and fires
        /// <see cref="_onDifficultyAdjusted"/>.
        /// </summary>
        public void Reset()
        {
            _currentDuration = 0f;
            _onDifficultyAdjusted?.Raise();
        }
    }
}
