using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that accumulates objective completions and expirations
    /// across a single match.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────────
    ///   1. Call <see cref="Reset"/> at match start (wire via VoidGameEventListener).
    ///   2. Call <see cref="RecordCompletion"/> when an objective is successfully completed.
    ///   3. Call <see cref="RecordExpiry"/> when an objective expires before completion.
    ///   4. Read <see cref="CompletionRatio"/> and counts for post-match summary.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All mutation methods are zero-allocation (integer arithmetic only).
    ///   - SO assets are immutable at runtime — only counter fields mutate.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchObjectiveTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/MatchObjectiveTracker")]
    public sealed class MatchObjectiveTrackerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Informational cap shown in the DoD; does not enforce a runtime limit.")]
        [SerializeField, Range(1, 20)] private int _maxTracked = 10;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time RecordCompletion is called.")]
        [SerializeField] private VoidGameEvent _onObjectiveCompleted;

        [Tooltip("Raised each time RecordCompletion or RecordExpiry is called.")]
        [SerializeField] private VoidGameEvent _onTrackerChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _completedCount;
        private int _expiredCount;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Informational maximum number of tracked objectives per match.</summary>
        public int MaxTracked => _maxTracked;

        /// <summary>Number of objectives completed this match.</summary>
        public int CompletedCount => _completedCount;

        /// <summary>Number of objectives expired (not completed) this match.</summary>
        public int ExpiredCount => _expiredCount;

        /// <summary>Total objectives resolved (completed + expired) this match.</summary>
        public int TotalTracked => _completedCount + _expiredCount;

        /// <summary>
        /// Fraction of resolved objectives that were completed. Range [0, 1].
        /// Returns 0 when no objectives have been resolved yet.
        /// </summary>
        public float CompletionRatio =>
            TotalTracked > 0 ? (float)_completedCount / TotalTracked : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records that an objective was successfully completed.
        /// Raises <c>_onObjectiveCompleted</c> and <c>_onTrackerChanged</c>.
        /// Zero allocation.
        /// </summary>
        public void RecordCompletion()
        {
            _completedCount++;
            _onObjectiveCompleted?.Raise();
            _onTrackerChanged?.Raise();
        }

        /// <summary>
        /// Records that an objective expired before it was completed.
        /// Raises <c>_onTrackerChanged</c>.
        /// Zero allocation.
        /// </summary>
        public void RecordExpiry()
        {
            _expiredCount++;
            _onTrackerChanged?.Raise();
        }

        /// <summary>
        /// Zeroes all counters. Does NOT fire any event channels.
        /// Call at match start via a VoidGameEventListener.
        /// </summary>
        public void Reset()
        {
            _completedCount = 0;
            _expiredCount   = 0;
        }
    }
}
