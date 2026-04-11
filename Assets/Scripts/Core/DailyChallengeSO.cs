using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime blackboard ScriptableObject that stores the current daily challenge and
    /// its completion state for the current UTC day.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> to rehydrate
    ///      the saved date, pool index, and completion flag.
    ///   2. <see cref="DailyChallengeManager.Awake"/> calls <see cref="RefreshIfNeeded"/>
    ///      to resolve the current challenge:
    ///        • Same day  → restores <see cref="CurrentChallenge"/> from saved index.
    ///        • New day   → selects a new challenge deterministically from the pool
    ///                      and resets <see cref="IsCompleted"/>.
    ///   3. <see cref="DailyChallengeManager"/> calls <see cref="MarkCompleted"/> when
    ///      the condition is satisfied at match end, firing <c>_onChallengeCompleted</c>.
    ///   4. <see cref="BattleRobots.UI.DailyChallengeController"/> subscribes to
    ///      <c>_onChallengeCompleted</c> and calls Refresh() to update the badge.
    ///
    /// ── Date-based selection ──────────────────────────────────────────────────
    ///   The daily challenge is selected deterministically using the number of UTC days
    ///   elapsed since 2026-01-01 modulo the count of non-null pool entries.  This
    ///   ensures all players see the same challenge each day without a server.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace; no Physics / UI references.
    ///   • <see cref="LoadSnapshot"/> and <see cref="Reset"/> do NOT fire events
    ///     (bootstrapper-safe).
    ///   • <see cref="MarkCompleted"/> is idempotent — safe to call multiple times.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ DailyChallengeSO.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DailyChallengeSO",
        menuName  = "BattleRobots/Economy/DailyChallenge",
        order     = 3)]
    public sealed class DailyChallengeSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Fired when MarkCompleted() transitions the challenge from incomplete → " +
                 "complete.  Subscribe in DailyChallengeController to refresh the UI badge. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onChallengeCompleted;

        // ── Runtime state (not serialized — clears on domain reload) ──────────

        private BonusConditionSO _currentChallenge;
        private string           _lastRefreshDate = "";  // yyyy-MM-dd UTC
        private bool             _isCompleted;
        private int              _currentIndex = -1;     // index in pool of _currentChallenge

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// The <see cref="BonusConditionSO"/> selected for today's challenge.
        /// Null until <see cref="RefreshIfNeeded"/> is called with a valid pool.
        /// </summary>
        public BonusConditionSO CurrentChallenge => _currentChallenge;

        /// <summary>
        /// The UTC date (yyyy-MM-dd) of the last successful refresh.
        /// Empty string on a fresh instance or after <see cref="Reset"/>.
        /// </summary>
        public string LastRefreshDate => _lastRefreshDate;

        /// <summary>True when <see cref="MarkCompleted"/> has been called today.</summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>
        /// Pool index of <see cref="CurrentChallenge"/> within
        /// <see cref="DailyChallengeConfig.ChallengePool"/>.
        /// -1 when no challenge has been selected yet.
        /// </summary>
        public int CurrentIndex => _currentIndex;

        // ── Date helper ───────────────────────────────────────────────────────

        /// <summary>Returns today's UTC date formatted as "yyyy-MM-dd".</summary>
        public static string TodayUtcString() =>
            DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Selects a new daily challenge from <paramref name="config"/> when the UTC
        /// date has changed since the last refresh, or restores the saved challenge
        /// when it is the same day.
        ///
        /// Same-day behaviour: if <see cref="_currentChallenge"/> is null (e.g. after
        /// <see cref="LoadSnapshot"/> rehydrates without a live SO reference) the
        /// challenge is restored from the saved pool index.
        ///
        /// Does nothing when <paramref name="config"/> is null, its pool is empty,
        /// or all entries are null.
        /// </summary>
        public void RefreshIfNeeded(DailyChallengeConfig config)
        {
            if (config == null) return;

            var pool = config.ChallengePool;
            if (pool == null || pool.Count == 0) return;

            // Count valid (non-null) pool entries.
            int validCount = 0;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null) validCount++;
            if (validCount == 0) return;

            string today = TodayUtcString();

            if (_lastRefreshDate == today)
            {
                // Same day — restore challenge from saved index if reference was lost
                // (LoadSnapshot cannot store the SO reference directly).
                if (_currentChallenge == null
                    && _currentIndex >= 0
                    && _currentIndex < pool.Count
                    && pool[_currentIndex] != null)
                {
                    _currentChallenge = pool[_currentIndex];
                }
                return;
            }

            // New day — select deterministically using days since project epoch (2026-01-01).
            int daysSinceEpoch = (int)(DateTime.UtcNow.Date
                - new DateTime(2026, 1, 1)).TotalDays;
            int pick = ((daysSinceEpoch % validCount) + validCount) % validCount;

            // Walk pool, skip nulls, pick the n-th valid entry.
            int validSeen = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] == null) continue;
                if (validSeen == pick)
                {
                    _currentChallenge = pool[i];
                    _currentIndex     = i;
                    break;
                }
                validSeen++;
            }

            _lastRefreshDate = today;
            _isCompleted     = false;
        }

        /// <summary>
        /// Marks the current daily challenge as completed and fires
        /// <c>_onChallengeCompleted</c>.  Idempotent — subsequent calls are no-ops.
        /// </summary>
        public void MarkCompleted()
        {
            if (_isCompleted) return;
            _isCompleted = true;
            _onChallengeCompleted?.Raise();
        }

        /// <summary>
        /// Silent rehydration from a <see cref="SaveData"/> snapshot.
        /// Does NOT fire <c>_onChallengeCompleted</c> — safe to call from
        /// <see cref="GameBootstrapper"/>.
        ///
        /// After calling LoadSnapshot, call <see cref="RefreshIfNeeded"/> to resolve
        /// <see cref="CurrentChallenge"/> from the pool (same-day restore or new-day
        /// selection).
        /// </summary>
        /// <param name="date">Last refresh date (yyyy-MM-dd); null treated as "".</param>
        /// <param name="index">Pool index of the saved challenge (−1 = none).</param>
        /// <param name="completed">Whether the challenge was completed today.</param>
        public void LoadSnapshot(string date, int index, bool completed)
        {
            _lastRefreshDate  = date ?? "";
            _currentIndex     = index;
            _isCompleted      = completed;
            // _currentChallenge is intentionally not set here; RefreshIfNeeded resolves it.
        }

        /// <summary>
        /// Returns the current persistence state as a value tuple for
        /// <see cref="DailyChallengeManager"/> to write into <see cref="SaveData"/>.
        /// </summary>
        public (string date, int index, bool completed) TakeSnapshot() =>
            (_lastRefreshDate, _currentIndex, _isCompleted);

        /// <summary>
        /// Silently clears all state.  Does NOT fire any event.
        /// Intended for test reset helpers and fresh-install scenarios.
        /// </summary>
        public void Reset()
        {
            _currentChallenge = null;
            _lastRefreshDate  = "";
            _currentIndex     = -1;
            _isCompleted      = false;
        }
    }
}
