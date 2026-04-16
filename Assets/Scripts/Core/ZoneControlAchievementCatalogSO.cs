using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// The metric used to evaluate a <see cref="ZoneControlAchievementEntry"/>.
    /// </summary>
    public enum ZoneControlAchievementType
    {
        /// <summary>Achievement targets a cumulative zone-capture count.</summary>
        TotalZones    = 0,
        /// <summary>Achievement targets a matches-with-dominance count.</summary>
        Dominance     = 1,
        /// <summary>Achievement targets a consecutive capture streak.</summary>
        Streak        = 2,
    }

    /// <summary>
    /// A single achievement definition stored inside a
    /// <see cref="ZoneControlAchievementCatalogSO"/>.
    /// </summary>
    [Serializable]
    public sealed class ZoneControlAchievementEntry
    {
        [Tooltip("Unique identifier used for persistence (must not be empty).")]
        [SerializeField] private string _achievementId;

        [Tooltip("Human-readable name shown in the HUD.")]
        [SerializeField] private string _displayName;

        [Tooltip("Metric type that must be met to earn this achievement.")]
        [SerializeField] private ZoneControlAchievementType _type;

        [Tooltip("Minimum value required to earn the achievement.")]
        [SerializeField, Min(1)] private int _targetValue = 1;

        /// <summary>Unique identifier used for persistence.</summary>
        public string AchievementId  => _achievementId;
        /// <summary>Human-readable name shown in the HUD.</summary>
        public string DisplayName    => _displayName;
        /// <summary>Metric type that must be met.</summary>
        public ZoneControlAchievementType Type => _type;
        /// <summary>Minimum value required to earn the achievement.</summary>
        public int TargetValue       => _targetValue;
    }

    /// <summary>
    /// Runtime ScriptableObject that catalogs zone-capture achievements and tracks
    /// which have been earned.
    ///
    /// ── Evaluation ─────────────────────────────────────────────────────────────
    ///   Call <see cref="EvaluateAchievements"/> with a
    ///   <see cref="ZoneControlSessionSummarySO"/> at match end.
    ///   <see cref="_onAchievementUnlocked"/> fires once per newly earned achievement.
    ///   Already-earned achievements are skipped (idempotent).
    ///
    /// ── Persistence ────────────────────────────────────────────────────────────
    ///   Use <see cref="LoadSnapshot"/> / <see cref="TakeSnapshot"/> with a
    ///   bootstrapper.  <see cref="Reset"/> clears all earned IDs silently.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — use LoadSnapshot for persistence.
    ///   - Zero heap allocation on EvaluateAchievements hot path (except first-time
    ///     HashSet initialisation).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlAchievementCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlAchievementCatalog", order = 28)]
    public sealed class ZoneControlAchievementCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Achievement Entries")]
        [Tooltip("All available zone-control achievements.")]
        [SerializeField] private ZoneControlAchievementEntry[] _entries;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once per newly earned achievement.")]
        [SerializeField] private VoidGameEvent _onAchievementUnlocked;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly HashSet<string> _earnedIds = new HashSet<string>();
        private string _latestEarnedDisplayName = string.Empty;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of achievements currently earned.</summary>
        public int EarnedCount => _earnedIds.Count;

        /// <summary>Total number of achievements in the catalog.</summary>
        public int TotalCount  => _entries != null ? _entries.Length : 0;

        /// <summary>
        /// Display name of the most recently earned achievement.
        /// Empty string when no achievement has been earned yet.
        /// </summary>
        public string LatestEarnedDisplayName => _latestEarnedDisplayName;

        /// <summary>Read-only view of all achievement entries.</summary>
        public IReadOnlyList<ZoneControlAchievementEntry> Entries =>
            _entries ?? Array.Empty<ZoneControlAchievementEntry>() as IReadOnlyList<ZoneControlAchievementEntry>;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if the achievement with the given
        /// <paramref name="achievementId"/> has been earned.
        /// </summary>
        public bool HasEarned(string achievementId) =>
            !string.IsNullOrEmpty(achievementId) && _earnedIds.Contains(achievementId);

        /// <summary>
        /// Evaluates all catalog entries against <paramref name="summary"/> and
        /// earns any newly met achievements, firing
        /// <see cref="_onAchievementUnlocked"/> once per new achievement.
        /// Already-earned achievements are skipped (idempotent).
        /// No-op when <paramref name="summary"/> is null.
        /// </summary>
        public void EvaluateAchievements(ZoneControlSessionSummarySO summary)
        {
            if (summary == null || _entries == null) return;

            for (int i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (entry == null) continue;
                if (string.IsNullOrEmpty(entry.AchievementId)) continue;
                if (_earnedIds.Contains(entry.AchievementId)) continue;

                int metricValue;
                switch (entry.Type)
                {
                    case ZoneControlAchievementType.Dominance:
                        metricValue = summary.MatchesWithDominance;
                        break;
                    case ZoneControlAchievementType.Streak:
                        metricValue = summary.BestCaptureStreak;
                        break;
                    case ZoneControlAchievementType.TotalZones:
                    default:
                        metricValue = summary.TotalZonesCaptured;
                        break;
                }

                if (metricValue >= entry.TargetValue)
                {
                    _earnedIds.Add(entry.AchievementId);
                    _latestEarnedDisplayName = entry.DisplayName ?? string.Empty;
                    _onAchievementUnlocked?.Raise();
                }
            }
        }

        /// <summary>
        /// Restores the earned achievement IDs from persisted data.
        /// Bootstrapper-safe; does not fire any events.
        /// </summary>
        public void LoadSnapshot(IReadOnlyList<string> earnedIds)
        {
            _earnedIds.Clear();
            _latestEarnedDisplayName = string.Empty;
            if (earnedIds == null) return;
            for (int i = 0; i < earnedIds.Count; i++)
            {
                if (!string.IsNullOrEmpty(earnedIds[i]))
                    _earnedIds.Add(earnedIds[i]);
            }
        }

        /// <summary>
        /// Returns the current earned IDs for persistence.
        /// </summary>
        public List<string> TakeSnapshot()
        {
            var list = new List<string>(_earnedIds.Count);
            foreach (var id in _earnedIds)
                list.Add(id);
            return list;
        }

        /// <summary>
        /// Clears all earned achievements and resets the latest display name.
        /// Does not fire any events.
        /// </summary>
        public void Reset()
        {
            _earnedIds.Clear();
            _latestEarnedDisplayName = string.Empty;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_entries == null) return;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i] == null) continue;
                // TargetValue clamping handled by [Min(1)] attribute.
            }
        }
    }
}
