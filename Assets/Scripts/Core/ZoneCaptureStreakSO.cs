using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks the player's consecutive zone-capture
    /// streak and derives a bonus-active flag when the streak meets a threshold.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • <see cref="IncrementStreak"/> is called each time the player captures a zone.
    ///   • <see cref="ResetStreak"/> is called each time the player loses a zone.
    ///     No-op (no event) when the streak is already zero.
    ///   • <see cref="HasBonus"/> is true when
    ///     <see cref="CurrentStreak"/> ≥ <see cref="StreakThreshold"/>.
    ///   • <see cref="Reset"/> silently zeros the streak — safe to call at match start.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on hot-path methods (integer arithmetic only).
    ///   - Runtime state is not serialised — streak resets on domain reload.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneCaptureStreak.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneCaptureStreak", order = 20)]
    public sealed class ZoneCaptureStreakSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Streak Settings")]
        [Tooltip("Number of consecutive captures required to activate the bonus.")]
        [SerializeField, Min(1)] private int _streakThreshold = 3;

        [Tooltip("Score multiplier applied while HasBonus is true. " +
                 "Read by ZoneCaptureStreakHUDController and bonus appliers.")]
        [SerializeField, Min(1f)] private float _bonusMultiplier = 2f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by IncrementStreak and ResetStreak. " +
                 "Wire to ZoneCaptureStreakHUDController.Refresh.")]
        [SerializeField] private VoidGameEvent _onStreakChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _currentStreak;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of consecutive zone captures this match.</summary>
        public int CurrentStreak => _currentStreak;

        /// <summary>Captures required before <see cref="HasBonus"/> becomes true.</summary>
        public int StreakThreshold => _streakThreshold;

        /// <summary>Score multiplier active while <see cref="HasBonus"/> is true.</summary>
        public float BonusMultiplier => _bonusMultiplier;

        /// <summary>True when <see cref="CurrentStreak"/> ≥ <see cref="StreakThreshold"/>.</summary>
        public bool HasBonus => _currentStreak >= _streakThreshold;

        /// <summary>Event raised on each IncrementStreak and ResetStreak. May be null.</summary>
        public VoidGameEvent OnStreakChanged => _onStreakChanged;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Increments the consecutive capture streak by one and fires
        /// <see cref="_onStreakChanged"/>.
        /// Zero allocation.
        /// </summary>
        public void IncrementStreak()
        {
            _currentStreak++;
            _onStreakChanged?.Raise();
        }

        /// <summary>
        /// Resets the streak to zero and fires <see cref="_onStreakChanged"/>.
        /// No-op (no event) when the streak is already zero.
        /// Zero allocation.
        /// </summary>
        public void ResetStreak()
        {
            if (_currentStreak == 0) return;
            _currentStreak = 0;
            _onStreakChanged?.Raise();
        }

        /// <summary>
        /// Silently zeros the streak without firing any events.
        /// Safe to call at match start from a controller.
        /// </summary>
        public void Reset()
        {
            _currentStreak = 0;
        }
    }
}
