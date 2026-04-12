using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that records the last <see cref="MaxEntries"/> match scores
    /// in chronological order (oldest → newest), giving the player a rolling window view of
    /// recent performance.
    ///
    /// Unlike <see cref="MatchLeaderboardSO"/> (which sorts by score descending and shows
    /// all-time best), ScoreHistorySO keeps insertion order so the player can see whether
    /// their scores are trending up or down over time.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────────
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> (no event) to
    ///      rehydrate the rolling window from <see cref="SaveData.scoreHistoryScores"/>.
    ///   2. <see cref="MatchManager"/> calls <see cref="Record"/> in <c>EndMatch()</c> after
    ///      the match score is computed.  <c>_onHistoryUpdated</c> fires so any open
    ///      ScoreHistoryController UI refreshes immediately.
    ///   3. <see cref="MatchManager"/> then calls <see cref="TakeSnapshot"/> and stores the
    ///      result in <see cref="SaveData.scoreHistoryScores"/> before calling
    ///      <see cref="SaveSystem.Save"/>.
    ///
    /// ── Capacity ──────────────────────────────────────────────────────────────────
    ///   <see cref="Record"/> appends to the end and evicts the oldest (front) entry when
    ///   the count would exceed <see cref="MaxEntries"/>.  The list is always chronological.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace; no Physics / UI references.
    ///   • <see cref="LoadSnapshot"/> and <see cref="Reset"/> do NOT fire
    ///     <c>_onHistoryUpdated</c> — bootstrapper-safe.
    ///   • <see cref="_scores"/> is a runtime-only list (not <c>[SerializeField]</c>);
    ///     persistence is handled entirely through the SaveSystem round-trip.
    ///   • Zero heap allocations on the hot subscribe/unsubscribe path; the one-time
    ///     List operations in Record are acceptable (cold EndMatch path).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ ScoreHistory.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/ScoreHistory",
        fileName = "ScoreHistorySO",
        order    = 7)]
    public sealed class ScoreHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum number of match scores retained in the rolling window. " +
                 "Oldest entries are evicted when this limit is exceeded.")]
        [SerializeField, Range(5, 50)] private int _maxEntries = 20;

        [Tooltip("Raised after Record() adds a score. " +
                 "Leave null if no UI reacts to score history changes.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Runtime state (not serialized — domain-reload safe via LoadSnapshot) ──

        private readonly List<int> _scores = new List<int>();

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Maximum number of scores retained in the rolling window.
        /// Configured in the Inspector (default 20, range [5, 50]).
        /// </summary>
        public int MaxEntries => _maxEntries;

        /// <summary>
        /// Read-only view of all recorded scores in chronological order (oldest first).
        /// Never null; may be empty.
        /// </summary>
        public IReadOnlyList<int> Scores => _scores;

        /// <summary>
        /// Average score across all recorded entries.
        /// Returns 0f when the history is empty.
        /// </summary>
        public float AverageScore
        {
            get
            {
                if (_scores.Count == 0) return 0f;

                long sum = 0;
                for (int i = 0; i < _scores.Count; i++)
                    sum += _scores[i];

                return (float)sum / _scores.Count;
            }
        }

        /// <summary>
        /// Difference between the most recent score and the oldest score in the window.
        /// Positive → improving trend; negative → declining trend; zero when fewer than
        /// two entries exist.
        /// </summary>
        public int TrendDelta
        {
            get
            {
                if (_scores.Count < 2) return 0;
                return _scores[_scores.Count - 1] - _scores[0];
            }
        }

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Appends <paramref name="score"/> to the chronological history.
        /// Evicts the oldest entry from the front when the count exceeds
        /// <see cref="MaxEntries"/>.  Always fires <c>_onHistoryUpdated</c>.
        /// </summary>
        /// <param name="score">Match score computed by <see cref="MatchScoreCalculator"/>. Any int accepted.</param>
        public void Record(int score)
        {
            _scores.Add(score);

            // Evict oldest entries until within capacity.
            while (_scores.Count > _maxEntries)
                _scores.RemoveAt(0);

            _onHistoryUpdated?.Raise();
        }

        /// <summary>
        /// Silent rehydration from a <see cref="SaveData"/> snapshot.
        /// Does NOT fire <c>_onHistoryUpdated</c> — safe to call from
        /// <see cref="GameBootstrapper"/>.
        ///
        /// Only the most recent <see cref="MaxEntries"/> entries are retained when
        /// the saved list is longer than the current inspector limit.
        /// Null input clears the history.
        /// </summary>
        /// <param name="scores">Serialised score history from <see cref="SaveData.scoreHistoryScores"/>.</param>
        public void LoadSnapshot(List<int> scores)
        {
            _scores.Clear();

            if (scores == null) return;

            // Determine the start index so we only keep the tail (most recent) entries
            // when the saved list exceeds the current MaxEntries limit.
            int start = scores.Count > _maxEntries ? scores.Count - _maxEntries : 0;
            for (int i = start; i < scores.Count; i++)
                _scores.Add(scores[i]);
        }

        /// <summary>
        /// Returns a shallow copy of the current score history for persistence.
        /// Safe to serialise directly into <see cref="SaveData.scoreHistoryScores"/>.
        /// Chronological order (oldest first) is preserved.
        /// </summary>
        public List<int> TakeSnapshot()
        {
            return new List<int>(_scores);
        }

        /// <summary>
        /// Silently clears all recorded scores. Does NOT fire <c>_onHistoryUpdated</c>.
        /// Intended for test teardown and fresh-install resets.
        /// </summary>
        public void Reset()
        {
            _scores.Clear();
        }
    }
}
