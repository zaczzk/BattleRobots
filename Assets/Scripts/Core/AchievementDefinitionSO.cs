using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Categorises what in-game condition must be met to unlock an achievement.
    /// Evaluated by <see cref="AchievementManager"/> after each relevant SO event fires.
    /// </summary>
    public enum AchievementTrigger
    {
        /// <summary>
        /// Unlocked when <see cref="PlayerAchievementsSO.TotalMatchesWon"/> reaches
        /// <see cref="AchievementDefinitionSO.TargetCount"/>.
        /// </summary>
        MatchWon,

        /// <summary>
        /// Unlocked when <see cref="WinStreakSO.BestStreak"/> reaches
        /// <see cref="AchievementDefinitionSO.TargetCount"/>.
        /// </summary>
        WinStreak,

        /// <summary>
        /// Unlocked when <see cref="PlayerProgressionSO.CurrentLevel"/> reaches
        /// <see cref="AchievementDefinitionSO.TargetCount"/>.
        /// </summary>
        ReachLevel,

        /// <summary>
        /// Unlocked when <see cref="PlayerAchievementsSO.TotalMatchesPlayed"/> reaches
        /// <see cref="AchievementDefinitionSO.TargetCount"/>.
        /// </summary>
        TotalMatches,

        /// <summary>
        /// Unlocked when the sum of all upgrade tiers across all parts in
        /// <see cref="PlayerPartUpgrades"/> reaches <see cref="AchievementDefinitionSO.TargetCount"/>.
        /// </summary>
        PartUpgraded,
    }

    /// <summary>
    /// Immutable definition of a single achievement: what triggers it, when it
    /// unlocks, and what currency reward it grants.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace. No Physics / UI references.
    ///   - All properties are read-only at runtime; SO is treated as immutable
    ///     in play mode.
    ///   - <see cref="_id"/> must be unique across the catalog and must never
    ///     change once shipped — it is used as the persistence key.
    ///   - OnValidate warns when <c>_id</c> or <c>_displayName</c> is empty.
    ///
    /// ── Scene / SO wiring ─────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ AchievementDefinitionSO.
    ///   2. Add to an <see cref="AchievementCatalogSO"/> list.
    ///   3. Assign the catalog to <see cref="AchievementManager._catalog"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/AchievementDefinitionSO",
        fileName = "AchievementDefinitionSO")]
    public sealed class AchievementDefinitionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Unique string key used for persistence. " +
                 "Must be stable — never rename after shipping.")]
        [SerializeField] private string _id;

        [Tooltip("Short player-facing title, e.g. 'First Victory'.")]
        [SerializeField] private string _displayName;

        [Tooltip("One-sentence description of the unlock condition.")]
        [SerializeField] private string _description;

        [Tooltip("Which in-game condition type triggers evaluation.")]
        [SerializeField] private AchievementTrigger _triggerType;

        [Tooltip("Progress threshold at which the achievement unlocks " +
                 "(e.g. win 5 matches → TargetCount = 5).")]
        [SerializeField, Min(1)] private int _targetCount = 1;

        [Tooltip("Credits awarded to the player on first unlock. " +
                 "Set to 0 for prestige-only achievements.")]
        [SerializeField, Min(0)] private int _rewardCredits;

        // ── Read-only API ──────────────────────────────────────────────────────

        /// <summary>Unique string identifier used for save-data persistence.</summary>
        public string Id => _id;

        /// <summary>Player-facing achievement title.</summary>
        public string DisplayName => _displayName;

        /// <summary>Short description of the unlock condition shown in the UI.</summary>
        public string Description => _description;

        /// <summary>Condition category evaluated by <see cref="AchievementManager"/>.</summary>
        public AchievementTrigger TriggerType => _triggerType;

        /// <summary>
        /// Progress value the relevant counter must reach to unlock this achievement.
        /// Always ≥ 1.
        /// </summary>
        public int TargetCount => _targetCount;

        /// <summary>
        /// Currency reward granted on first unlock.
        /// Zero means a prestige achievement with no monetary reward.
        /// </summary>
        public int RewardCredits => _rewardCredits;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                Debug.LogWarning(
                    $"[AchievementDefinitionSO] '{name}' has an empty _id — " +
                    "persistence will not work correctly.", this);
            if (string.IsNullOrWhiteSpace(_displayName))
                Debug.LogWarning(
                    $"[AchievementDefinitionSO] '{name}' has an empty _displayName.", this);
        }
#endif
    }
}
