using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks which achievements the local player
    /// has unlocked and evaluates unlock conditions after each match.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadFromData"/> on startup.
    ///   2. After every match, <see cref="GameBootstrapper.RecordMatchAndSave"/> calls
    ///      <see cref="CheckAndUnlock"/> (after <c>PlayerProfileSO.UpdateFromMatchRecord</c>
    ///      so career stats already reflect the completed match).
    ///   3. Bootstrapper then calls <see cref="BuildData"/> and persists the result.
    ///
    /// ── Event channels ────────────────────────────────────────────────────────
    ///   <list type="bullet">
    ///     <item><c>_onAchievementUnlocked</c> (VoidGameEvent) — raised once per
    ///       newly unlocked achievement.</item>
    ///     <item><c>_onAchievementTitle</c> (StringGameEvent) — raised with the
    ///       achievement title so AchievementUI can display a popup without a direct
    ///       SO reference.</item>
    ///   </list>
    ///   Wire a StringGameEventListener on the AchievementUI GameObject:
    ///     Event    → AchievementProgressSO._onAchievementTitle
    ///     Response → AchievementUI.ShowUnlock(string)
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • <c>BattleRobots.Core</c> only — no Physics or UI references.
    ///   • <see cref="CheckAndUnlock"/> is on the cold post-match path;
    ///     per-iteration allocation from <c>HashSet.Contains</c> is acceptable.
    ///   • An already-unlocked achievement will never fire events again.
    ///
    /// Create via: Assets ▶ Create ▶ BattleRobots ▶ Achievements ▶ AchievementProgressSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Achievements/AchievementProgressSO", order = 2)]
    public sealed class AchievementProgressSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Catalog")]
        [Tooltip("The AchievementCatalogSO asset listing all available achievements.")]
        [SerializeField] private AchievementCatalogSO _catalog;

        [Header("Event Channels — Out")]
        [Tooltip("Raised once per newly unlocked achievement (no payload).")]
        [SerializeField] private VoidGameEvent _onAchievementUnlocked;

        [Tooltip("Raised once per newly unlocked achievement. Payload = achievement title. " +
                 "Wire a StringGameEventListener on AchievementUI → ShowUnlock(string).")]
        [SerializeField] private StringGameEvent _onAchievementTitle;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Ordinal comparison — IDs are case-sensitive machine strings.
        private readonly HashSet<string> _unlockedIds =
            new HashSet<string>(StringComparer.Ordinal);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Number of achievements unlocked so far this session.</summary>
        public int UnlockedCount => _unlockedIds.Count;

        /// <summary>
        /// Returns true if the achievement with the given ID has been unlocked.
        /// Returns false for null or empty IDs.
        /// </summary>
        public bool IsUnlocked(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return false;
            return _unlockedIds.Contains(achievementId);
        }

        /// <summary>
        /// Evaluates every achievement in the catalog against the post-match
        /// career stats and fires event channels for each newly unlocked achievement.
        ///
        /// <para>
        ///   Call this <em>after</em> <c>PlayerProfileSO.UpdateFromMatchRecord(record)</c>
        ///   so that career stats already include the completed match.
        /// </para>
        /// <para>
        ///   Already-unlocked achievements are skipped without re-firing events.
        ///   Null or empty-ID definitions are skipped silently.
        /// </para>
        /// </summary>
        public void CheckAndUnlock(MatchRecord record, PlayerProfileSO profile)
        {
            if (_catalog == null || profile == null) return;

            IReadOnlyList<AchievementDefinition> defs = _catalog.Achievements;
            for (int i = 0; i < defs.Count; i++)
            {
                AchievementDefinition def = defs[i];
                if (def == null || string.IsNullOrEmpty(def.AchievementId)) continue;
                if (_unlockedIds.Contains(def.AchievementId)) continue;

                if (def.Evaluate(record, profile))
                {
                    _unlockedIds.Add(def.AchievementId);
                    _onAchievementTitle?.Raise(def.Title);
                    _onAchievementUnlocked?.Raise();
                }
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────

        /// <summary>
        /// Restores the unlocked-ID set from a persisted <see cref="AchievementData"/> snapshot.
        /// Skips null/empty IDs; deduplicates automatically via the internal HashSet.
        /// Called by <see cref="GameBootstrapper"/> on startup.
        /// </summary>
        public void LoadFromData(AchievementData data)
        {
            _unlockedIds.Clear();
            if (data?.unlockedIds == null) return;

            List<string> ids = data.unlockedIds;
            for (int i = 0; i < ids.Count; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]))
                    _unlockedIds.Add(ids[i]);
            }
        }

        /// <summary>
        /// Snapshots the current unlocked-ID set into an <see cref="AchievementData"/> POCO
        /// for XOR-SaveSystem persistence.
        /// Called by <see cref="GameBootstrapper.RecordMatchAndSave"/>.
        /// </summary>
        public AchievementData BuildData()
        {
            var data = new AchievementData();
            foreach (string id in _unlockedIds)
                data.unlockedIds.Add(id);
            return data;
        }
    }
}
