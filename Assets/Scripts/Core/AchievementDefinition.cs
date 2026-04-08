using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Classifies the unlock condition for a single <see cref="AchievementDefinition"/>.
    /// The <see cref="AchievementProgressSO.CheckAndUnlock"/> method evaluates
    /// the condition against the current <see cref="PlayerProfileSO"/> career stats
    /// and the most-recent <see cref="MatchRecord"/>.
    /// </summary>
    public enum AchievementCondition
    {
        /// <summary>Unlocks when <see cref="PlayerProfileSO.CareerWins"/> &ge; Threshold.</summary>
        WinCount        = 0,

        /// <summary>Unlocks when <see cref="PlayerProfileSO.CareerMatches"/> &ge; Threshold.</summary>
        MatchCount      = 1,

        /// <summary>Unlocks when <see cref="PlayerProfileSO.CareerDamageDone"/> &ge; Threshold.</summary>
        CareerDamage    = 2,

        /// <summary>Unlocks when <see cref="PlayerProfileSO.CareerEarnings"/> &ge; Threshold.</summary>
        CareerEarnings  = 3,

        /// <summary>
        /// Unlocks when the triggering <see cref="MatchRecord.playerWon"/> is true.
        /// Threshold is ignored.
        /// </summary>
        WinCurrentMatch = 4,
    }

    /// <summary>
    /// ScriptableObject that describes a single achievement — its ID, display strings,
    /// and the numeric condition required to unlock it.
    ///
    /// Create via: Assets ▶ Create ▶ BattleRobots ▶ Achievements ▶ AchievementDefinition
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • <c>BattleRobots.Core</c> only — no Physics or UI references.
    ///   • SO asset is immutable at runtime (all fields are read-only properties).
    ///   • <see cref="Evaluate"/> is called from <see cref="AchievementProgressSO"/>
    ///     on the post-match cold path — allocation is acceptable.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Achievements/AchievementDefinition", order = 0)]
    public sealed class AchievementDefinition : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Unique machine-readable identifier, e.g. 'achievement_first_win'. " +
                 "Must be non-empty and unique within the AchievementCatalogSO.")]
        [SerializeField] private string _achievementId;

        [Tooltip("Short player-facing title shown in achievement popups and lists.")]
        [SerializeField] private string _title;

        [Tooltip("Longer description of how to unlock this achievement.")]
        [SerializeField, TextArea(2, 4)] private string _description;

        [Tooltip("What career metric must reach Threshold for this achievement to unlock.")]
        [SerializeField] private AchievementCondition _condition;

        [Tooltip("Numeric threshold for the condition. Ignored for WinCurrentMatch.")]
        [SerializeField, Min(0f)] private float _threshold;

        // ── Public read-only properties ───────────────────────────────────────

        /// <summary>Unique machine-readable identifier for this achievement.</summary>
        public string AchievementId  => _achievementId;

        /// <summary>Short player-facing title.</summary>
        public string Title          => _title;

        /// <summary>Longer unlock description.</summary>
        public string Description    => _description;

        /// <summary>Unlock condition category.</summary>
        public AchievementCondition Condition => _condition;

        /// <summary>Numeric threshold required by the condition (≥ 0).</summary>
        public float  Threshold      => _threshold;

        // ── Condition evaluation ──────────────────────────────────────────────

        /// <summary>
        /// Returns true when the unlock condition is satisfied.
        /// <para>
        ///   <paramref name="record"/> may be null for conditions that only inspect
        ///   career stats. <paramref name="profile"/> must not be null.
        /// </para>
        /// </summary>
        public bool Evaluate(MatchRecord record, PlayerProfileSO profile)
        {
            if (profile == null) return false;

            switch (_condition)
            {
                case AchievementCondition.WinCount:
                    return profile.CareerWins >= (int)_threshold;
                case AchievementCondition.MatchCount:
                    return profile.CareerMatches >= (int)_threshold;
                case AchievementCondition.CareerDamage:
                    return profile.CareerDamageDone >= _threshold;
                case AchievementCondition.CareerEarnings:
                    return profile.CareerEarnings >= (int)_threshold;
                case AchievementCondition.WinCurrentMatch:
                    return record != null && record.playerWon;
                default:
                    return false;
            }
        }
    }
}
