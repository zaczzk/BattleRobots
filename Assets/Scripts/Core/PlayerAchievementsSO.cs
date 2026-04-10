using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks which achievements the player has unlocked and
    /// the cumulative match-play counters consumed by <see cref="AchievementManager"/>.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────
    ///   • <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> on startup
    ///     to restore the persisted state from <see cref="SaveData"/>.
    ///   • <see cref="AchievementManager"/> calls <see cref="RecordMatchResult"/> and
    ///     <see cref="Unlock"/> at the end of each match.
    ///   • Persistence is handled exclusively by
    ///     <see cref="AchievementManager.PersistAchievements"/> via the
    ///     load → mutate → save pattern.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace. No Physics / UI references.
    ///   - All runtime state is non-serialised; no stale SO asset data on disk.
    ///   - A <see cref="HashSet{T}"/> mirrors <c>_unlockedIds</c> for O(1) lookups.
    ///   - <see cref="_onAchievementUnlocked"/> fires only on the first unlock of each
    ///     unique ID (idempotent guard applied).
    ///   - <see cref="RecordMatchResult"/> increments counters silently — evaluation
    ///     and persistence are handled by <see cref="AchievementManager"/>.
    ///   - <see cref="LoadSnapshot"/> and <see cref="Reset"/> do NOT fire events
    ///     (bootstrapper-safe).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ PlayerAchievementsSO.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/PlayerAchievementsSO",
        fileName = "PlayerAchievementsSO")]
    public sealed class PlayerAchievementsSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Raised the first time any achievement is unlocked. " +
                 "Subscribers can read LastUnlockedId to identify which one fired. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onAchievementUnlocked;

        // ── Runtime state (not SO-serialised) ────────────────────────────────

        // Ordered list — preserves unlock order for UI display.
        private readonly List<string>    _unlockedIds = new List<string>();
        // Shadow set — O(1) membership test; kept in sync with the list at all times.
        private readonly HashSet<string> _unlockedSet = new HashSet<string>();

        private int    _totalMatchesPlayed;
        private int    _totalMatchesWon;
        private string _lastUnlockedId;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Read-only ordered list of achievement IDs that have been unlocked,
        /// in the order they were first unlocked.
        /// </summary>
        public IReadOnlyList<string> UnlockedIds => _unlockedIds;

        /// <summary>
        /// Total matches played (win + loss) since the account was created.
        /// Incremented by <see cref="RecordMatchResult"/> regardless of outcome.
        /// </summary>
        public int TotalMatchesPlayed => _totalMatchesPlayed;

        /// <summary>
        /// Total matches the player won.
        /// Incremented by <see cref="RecordMatchResult"/> when
        /// <paramref name="playerWon"/> is <c>true</c>.
        /// </summary>
        public int TotalMatchesWon => _totalMatchesWon;

        /// <summary>
        /// ID of the most-recently unlocked achievement in this session,
        /// or <c>null</c> if none have been unlocked yet.
        /// Updated each time <see cref="Unlock"/> fires for a new ID.
        /// </summary>
        public string LastUnlockedId => _lastUnlockedId;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if the achievement identified by <paramref name="id"/>
        /// has been unlocked.  O(1).  Returns <c>false</c> for null/whitespace IDs.
        /// </summary>
        public bool HasUnlocked(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            return _unlockedSet.Contains(id);
        }

        /// <summary>
        /// Marks the achievement as unlocked and fires <c>_onAchievementUnlocked</c>.
        /// <para>
        /// • Idempotent — safe to call multiple times for the same ID (event fires
        ///   only on the first call).
        /// • Silently ignores null/whitespace IDs.
        /// • Updates <see cref="LastUnlockedId"/> to <paramref name="id"/> on unlock.
        /// </para>
        /// </summary>
        public void Unlock(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;

            if (_unlockedSet.Add(id))
            {
                _unlockedIds.Add(id);
                _lastUnlockedId = id;
                _onAchievementUnlocked?.Raise();
            }
        }

        /// <summary>
        /// Increments <see cref="TotalMatchesPlayed"/> by 1 and, when
        /// <paramref name="playerWon"/> is <c>true</c>, also increments
        /// <see cref="TotalMatchesWon"/> by 1.
        /// <para>
        /// Does NOT fire any events — counter evaluation and persistence are
        /// handled by <see cref="AchievementManager"/> after this call.
        /// </para>
        /// </summary>
        public void RecordMatchResult(bool playerWon)
        {
            _totalMatchesPlayed++;
            if (playerWon) _totalMatchesWon++;
        }

        /// <summary>
        /// Silently rehydrates all runtime state from persisted <see cref="SaveData"/> fields.
        /// Does NOT fire any events — safe to call from <see cref="GameBootstrapper"/>.
        /// <para>
        /// • Negative match counts are clamped to 0.
        /// • Duplicate IDs in <paramref name="unlockedIds"/> are silently dropped.
        /// • Null or empty <paramref name="unlockedIds"/> resets to an empty unlock set.
        /// </para>
        /// </summary>
        public void LoadSnapshot(
            int          matchesPlayed,
            int          matchesWon,
            List<string> unlockedIds)
        {
            _totalMatchesPlayed = Mathf.Max(0, matchesPlayed);
            _totalMatchesWon    = Mathf.Max(0, matchesWon);

            _unlockedIds.Clear();
            _unlockedSet.Clear();

            if (unlockedIds != null)
            {
                for (int i = 0; i < unlockedIds.Count; i++)
                {
                    string id = unlockedIds[i];
                    if (!string.IsNullOrWhiteSpace(id) && _unlockedSet.Add(id))
                        _unlockedIds.Add(id);
                }
            }
        }

        /// <summary>
        /// Silently clears all runtime state (unlock list, counters, last-unlocked ID).
        /// Does NOT fire any events.  Intended for fresh-install resets.
        /// </summary>
        public void Reset()
        {
            _totalMatchesPlayed = 0;
            _totalMatchesWon    = 0;
            _unlockedIds.Clear();
            _unlockedSet.Clear();
            _lastUnlockedId     = null;
        }
    }
}
