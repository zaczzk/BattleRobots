using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Metric type used to evaluate a <see cref="ZoneControlChallengeEntry"/> against
    /// <see cref="ZoneControlSessionSummarySO"/> data.
    /// </summary>
    public enum ZoneControlChallengeType
    {
        /// <summary>Cumulative zones captured across all matches.</summary>
        TotalZones    = 0,

        /// <summary>Best consecutive capture streak ever recorded.</summary>
        BestStreak    = 1,

        /// <summary>Total number of matches played.</summary>
        MatchesPlayed = 2,

        /// <summary>Matches in which the player held a zone majority.</summary>
        DominanceMatches = 3
    }

    /// <summary>
    /// Serialisable data for a single named zone-control challenge.
    /// </summary>
    [Serializable]
    public sealed class ZoneControlChallengeEntry
    {
        [Tooltip("Unique identifier used for persistence.  Must not be null or empty.")]
        public string Id;

        [Tooltip("Human-readable challenge name shown in the HUD.")]
        public string DisplayName;

        [Tooltip("Metric type to evaluate.")]
        public ZoneControlChallengeType Type;

        [Tooltip("Target value.  Challenge is met when the chosen metric reaches or exceeds this.")]
        [Min(0f)]
        public float TargetValue;
    }

    /// <summary>
    /// Runtime ScriptableObject holding a catalog of named zone-control challenges.
    /// Each match, call <see cref="EvaluateAll"/> to mark any newly satisfied challenges.
    /// Completion state is tracked at runtime and persisted via
    /// <see cref="LoadSnapshot"/> / <see cref="TakeSnapshot"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — use LoadSnapshot for persistence.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlChallengeCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlChallengeCatalog", order = 37)]
    public sealed class ZoneControlChallengeCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Challenges")]
        [SerializeField] private List<ZoneControlChallengeEntry> _entries =
            new List<ZoneControlChallengeEntry>();

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once per newly completed challenge (inside EvaluateAll).")]
        [SerializeField] private VoidGameEvent _onChallengeCompleted;

        [Tooltip("Raised once at the end of every EvaluateAll call and after Reset.")]
        [SerializeField] private VoidGameEvent _onCatalogUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly HashSet<string> _completedIds = new HashSet<string>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total number of entries in the catalog (including null entries).</summary>
        public int EntryCount => _entries.Count;

        /// <summary>Number of challenges that have been marked as completed.</summary>
        public int CompletedCount => _completedIds.Count;

        /// <summary>Number of challenges not yet completed (null entries excluded).</summary>
        public int ActiveCount
        {
            get
            {
                int active = 0;
                foreach (var entry in _entries)
                    if (entry != null && !_completedIds.Contains(entry.Id))
                        active++;
                return active;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the challenge with the given <paramref name="id"/> is completed.
        /// </summary>
        public bool IsCompleted(string id) => id != null && _completedIds.Contains(id);

        /// <summary>
        /// Evaluates all catalog entries against the provided summary SO.
        /// Newly satisfied challenges are marked completed and fire
        /// <see cref="_onChallengeCompleted"/> individually.
        /// <see cref="_onCatalogUpdated"/> fires once at the end.
        /// Null summary or null/empty IDs are skipped silently.
        /// </summary>
        public void EvaluateAll(ZoneControlSessionSummarySO summary)
        {
            foreach (var entry in _entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id))
                    continue;
                if (_completedIds.Contains(entry.Id))
                    continue;

                float metric = GetMetric(entry.Type, summary);
                if (metric >= entry.TargetValue)
                {
                    _completedIds.Add(entry.Id);
                    _onChallengeCompleted?.Raise();
                }
            }
            _onCatalogUpdated?.Raise();
        }

        /// <summary>
        /// Restores the set of completed challenge IDs from a persisted snapshot.
        /// Bootstrapper-safe; no events fired.
        /// </summary>
        public void LoadSnapshot(IReadOnlyList<string> completedIds)
        {
            _completedIds.Clear();
            if (completedIds == null) return;
            foreach (string id in completedIds)
                if (!string.IsNullOrEmpty(id))
                    _completedIds.Add(id);
        }

        /// <summary>Returns the current set of completed IDs as a read-only list.</summary>
        public IReadOnlyList<string> TakeSnapshot()
        {
            var list = new List<string>(_completedIds.Count);
            foreach (string id in _completedIds)
                list.Add(id);
            return list;
        }

        /// <summary>
        /// Clears all completion state and fires <see cref="_onCatalogUpdated"/>.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _completedIds.Clear();
            _onCatalogUpdated?.Raise();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static float GetMetric(ZoneControlChallengeType type,
                                       ZoneControlSessionSummarySO summary)
        {
            if (summary == null) return 0f;
            switch (type)
            {
                case ZoneControlChallengeType.TotalZones:       return summary.TotalZonesCaptured;
                case ZoneControlChallengeType.BestStreak:       return summary.BestCaptureStreak;
                case ZoneControlChallengeType.MatchesPlayed:    return summary.MatchesPlayed;
                case ZoneControlChallengeType.DominanceMatches: return summary.MatchesWithDominance;
                default:                                        return 0f;
            }
        }
    }
}
