using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that persists the player's all-time best zone-control
    /// figures: best single-match zone count, best pace (captures-per-minute), and
    /// best consecutive capture streak.
    ///
    /// ── Update flow ─────────────────────────────────────────────────────────────
    ///   Call <see cref="UpdateFromMatch"/> at match end.
    ///   For each category that improves a personal best the corresponding
    ///   <see cref="_onNewHighScore"/> event fires once and the matching
    ///   IsNew* flag is set to <c>true</c> for that call.
    ///   Flags are reset to <c>false</c> at the start of each
    ///   <see cref="UpdateFromMatch"/> call so they reflect only the most recent result.
    ///
    /// ── Persistence ────────────────────────────────────────────────────────────
    ///   Use <see cref="LoadSnapshot"/> / <see cref="TakeSnapshot"/> with a
    ///   bootstrapper.  <see cref="Reset"/> clears all bests silently.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — use LoadSnapshot for persistence.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlHighScore.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlHighScore", order = 31)]
    public sealed class ZoneControlHighScoreSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once per category whenever a new personal best is set.")]
        [SerializeField] private VoidGameEvent _onNewHighScore;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int   _bestZoneCount;
        private float _bestPace;
        private int   _bestStreak;

        // Transient new-record flags — reset at the start of each UpdateFromMatch.
        private bool _isNewZoneCount;
        private bool _isNewPace;
        private bool _isNewStreak;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Best single-match zone-capture count.</summary>
        public int BestZoneCount => _bestZoneCount;

        /// <summary>Best captures-per-minute pace reading.</summary>
        public float BestPace => _bestPace;

        /// <summary>Best consecutive zone-capture streak.</summary>
        public int BestStreak => _bestStreak;

        /// <summary>
        /// True if the most recent <see cref="UpdateFromMatch"/> set a new
        /// zone-count record.  Reset to false on the next call.
        /// </summary>
        public bool IsNewZoneCount => _isNewZoneCount;

        /// <summary>
        /// True if the most recent <see cref="UpdateFromMatch"/> set a new
        /// pace record.  Reset to false on the next call.
        /// </summary>
        public bool IsNewPace => _isNewPace;

        /// <summary>
        /// True if the most recent <see cref="UpdateFromMatch"/> set a new
        /// streak record.  Reset to false on the next call.
        /// </summary>
        public bool IsNewStreak => _isNewStreak;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Compares <paramref name="zoneCount"/>, <paramref name="pace"/>, and
        /// <paramref name="streak"/> against current personal bests and updates
        /// any that are exceeded.  Fires <see cref="_onNewHighScore"/> once per
        /// improved category.  All values are clamped to ≥ 0.
        /// </summary>
        public void UpdateFromMatch(int zoneCount, float pace, int streak)
        {
            // Reset transient flags.
            _isNewZoneCount = false;
            _isNewPace      = false;
            _isNewStreak    = false;

            int   clampedZones  = Mathf.Max(0, zoneCount);
            float clampedPace   = Mathf.Max(0f, pace);
            int   clampedStreak = Mathf.Max(0, streak);

            if (clampedZones > _bestZoneCount)
            {
                _bestZoneCount  = clampedZones;
                _isNewZoneCount = true;
                _onNewHighScore?.Raise();
            }

            if (clampedPace > _bestPace)
            {
                _bestPace  = clampedPace;
                _isNewPace = true;
                _onNewHighScore?.Raise();
            }

            if (clampedStreak > _bestStreak)
            {
                _bestStreak  = clampedStreak;
                _isNewStreak = true;
                _onNewHighScore?.Raise();
            }
        }

        /// <summary>
        /// Restores all personal bests from persisted data.
        /// Bootstrapper-safe; does not fire any events.
        /// All values are clamped to ≥ 0.
        /// </summary>
        public void LoadSnapshot(int bestZoneCount, float bestPace, int bestStreak)
        {
            _bestZoneCount  = Mathf.Max(0, bestZoneCount);
            _bestPace       = Mathf.Max(0f, bestPace);
            _bestStreak     = Mathf.Max(0, bestStreak);
            _isNewZoneCount = false;
            _isNewPace      = false;
            _isNewStreak    = false;
        }

        /// <summary>
        /// Returns the current personal bests for persistence as a value tuple.
        /// </summary>
        public (int bestZoneCount, float bestPace, int bestStreak) TakeSnapshot() =>
            (_bestZoneCount, _bestPace, _bestStreak);

        /// <summary>
        /// Resets all personal bests to zero.
        /// Does not fire any events.
        /// </summary>
        public void Reset()
        {
            _bestZoneCount  = 0;
            _bestPace       = 0f;
            _bestStreak     = 0;
            _isNewZoneCount = false;
            _isNewPace      = false;
            _isNewStreak    = false;
        }
    }
}
