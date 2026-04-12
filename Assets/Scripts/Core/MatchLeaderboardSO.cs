using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that maintains a descending-score sorted local leaderboard
    /// of up to <see cref="MaxEntries"/> match results, persisted across sessions via
    /// <see cref="SaveData.leaderboardEntries"/>.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> (no event) to
    ///      rehydrate saved entries from <see cref="SaveData.leaderboardEntries"/>.
    ///   2. <see cref="MatchManager"/> calls <see cref="Submit"/> in <c>EndMatch()</c>,
    ///      passing the live <see cref="MatchResultSO"/> blackboard just after the score
    ///      is computed for <see cref="PersonalBestSO"/>. The call fires
    ///      <c>_onLeaderboardUpdated</c> so any open leaderboard UI refreshes immediately.
    ///   3. <see cref="MatchManager"/> then calls <see cref="TakeSnapshot"/> and stores
    ///      the result in <see cref="SaveData.leaderboardEntries"/> before calling
    ///      <see cref="SaveSystem.Save"/>.
    ///   4. <see cref="BattleRobots.UI.MatchLeaderboardController"/> subscribes to
    ///      <c>_onLeaderboardUpdated</c> and rebuilds the UI row list on each signal.
    ///
    /// ── Capacity ──────────────────────────────────────────────────────────────
    ///   <see cref="Submit"/> always inserts the new entry in score order and then evicts
    ///   the tail entry when <see cref="Entries"/>.Count would exceed
    ///   <see cref="MaxEntries"/>. A new entry with a score lower than all existing
    ///   entries on a full board will be inserted and then immediately evicted — the
    ///   board is unchanged, but the method still fires the updated event.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace; no Physics / UI references.
    ///   • <see cref="LoadSnapshot"/> and <see cref="Reset"/> do NOT fire
    ///     <c>_onLeaderboardUpdated</c> — bootstrapper-safe.
    ///   • <see cref="_entries"/> is a runtime-only list (not <c>[SerializeField]</c>);
    ///     persistence is handled entirely through the SaveSystem round-trip.
    ///   • Zero heap allocations on the hot subscribe/unsubscribe path; the
    ///     one-time List.Insert allocation on Submit is acceptable (cold path).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchLeaderboard.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/MatchLeaderboard",
        fileName = "MatchLeaderboardSO",
        order    = 6)]
    public sealed class MatchLeaderboardSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum number of entries retained on the leaderboard. " +
                 "The lowest-scoring entry is evicted when this limit is exceeded.")]
        [SerializeField, Range(5, 20)] private int _maxEntries = 10;

        [Tooltip("Raised after Submit() adds an entry (even if the new entry is immediately " +
                 "evicted due to a full board). Leave null if no UI reacts to leaderboard changes.")]
        [SerializeField] private VoidGameEvent _onLeaderboardUpdated;

        // ── Runtime state (not serialized — domain-reload safe via LoadSnapshot) ──

        private readonly List<LeaderboardEntry> _entries = new List<LeaderboardEntry>();

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Maximum number of entries retained simultaneously.
        /// Configured in the Inspector (default 10, range [5, 20]).
        /// </summary>
        public int MaxEntries => _maxEntries;

        /// <summary>
        /// Read-only view of all leaderboard entries sorted descending by score.
        /// Never null; may be empty.
        /// </summary>
        public IReadOnlyList<LeaderboardEntry> Entries => _entries;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Computes a score from <paramref name="result"/> via
        /// <see cref="MatchScoreCalculator.Calculate"/> and inserts a new
        /// <see cref="LeaderboardEntry"/> into the board in descending score order.
        ///
        /// Returns <c>false</c> when <paramref name="result"/> is null (no-op).
        /// Always fires <c>_onLeaderboardUpdated</c> on success (even if the entry
        /// was immediately evicted from a full board).
        /// </summary>
        /// <param name="result">
        /// Blackboard written by <see cref="MatchManager"/> before MatchEnded fires.
        /// Passing <c>null</c> returns <c>false</c>.
        /// </param>
        /// <param name="opponentName">
        /// Display name of the selected opponent. Stored as-is on the entry (null → empty string).
        /// </param>
        /// <param name="arenaIndex">Zero-based index of the arena played.</param>
        /// <returns>True when a new entry was created and inserted into the board.</returns>
        public bool Submit(MatchResultSO result, string opponentName = "", int arenaIndex = 0)
        {
            if (result == null) return false;

            int score = MatchScoreCalculator.Calculate(result);

            var entry = new LeaderboardEntry
            {
                score           = score,
                playerWon       = result.PlayerWon,
                opponentName    = opponentName ?? string.Empty,
                arenaIndex      = arenaIndex,
                durationSeconds = result.DurationSeconds,
                timestamp       = DateTime.UtcNow.ToString("o"),
            };

            // Insert at the correct descending-score position.
            int insertAt = _entries.Count;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (entry.score >= _entries[i].score)
                {
                    insertAt = i;
                    break;
                }
            }
            _entries.Insert(insertAt, entry);

            // Evict tail entries that exceed capacity.
            while (_entries.Count > _maxEntries)
                _entries.RemoveAt(_entries.Count - 1);

            _onLeaderboardUpdated?.Raise();
            return true;
        }

        /// <summary>
        /// Silent rehydration from a <see cref="SaveData"/> snapshot.
        /// Does NOT fire <c>_onLeaderboardUpdated</c> — safe to call from
        /// <see cref="GameBootstrapper"/>.
        ///
        /// Entries are sorted descending by score and capped to <see cref="MaxEntries"/>
        /// so that stale saves with more entries than the current inspector limit are
        /// handled gracefully.  Null input clears the board.
        /// </summary>
        /// <param name="entries">Serialised leaderboard data from <see cref="SaveData.leaderboardEntries"/>.</param>
        public void LoadSnapshot(List<LeaderboardEntry> entries)
        {
            _entries.Clear();

            if (entries == null) return;

            for (int i = 0; i < entries.Count; i++)
            {
                LeaderboardEntry e = entries[i];
                if (e == null) continue;

                _entries.Add(new LeaderboardEntry
                {
                    score           = e.score,
                    playerWon       = e.playerWon,
                    opponentName    = e.opponentName ?? string.Empty,
                    arenaIndex      = e.arenaIndex,
                    durationSeconds = e.durationSeconds,
                    timestamp       = e.timestamp ?? string.Empty,
                });
            }

            // Re-sort descending — the serialiser may reorder or new data may arrive
            // out of order from a patched save.
            _entries.Sort((a, b) => b.score.CompareTo(a.score));

            // Enforce capacity in case the save was written with a larger MaxEntries value.
            while (_entries.Count > _maxEntries)
                _entries.RemoveAt(_entries.Count - 1);
        }

        /// <summary>
        /// Returns a deep copy of the current leaderboard entries for persistence.
        /// Safe to serialise directly into <see cref="SaveData.leaderboardEntries"/>.
        /// </summary>
        public List<LeaderboardEntry> TakeSnapshot()
        {
            var copy = new List<LeaderboardEntry>(_entries.Count);
            for (int i = 0; i < _entries.Count; i++)
            {
                LeaderboardEntry e = _entries[i];
                copy.Add(new LeaderboardEntry
                {
                    score           = e.score,
                    playerWon       = e.playerWon,
                    opponentName    = e.opponentName,
                    arenaIndex      = e.arenaIndex,
                    durationSeconds = e.durationSeconds,
                    timestamp       = e.timestamp,
                });
            }
            return copy;
        }

        /// <summary>
        /// Silently clears all leaderboard entries. Does NOT fire
        /// <c>_onLeaderboardUpdated</c>.
        /// Intended for test teardown and fresh-install resets.
        /// </summary>
        public void Reset()
        {
            _entries.Clear();
        }
    }
}
