using UnityEngine;

namespace BattleRobots.Core
{
    // ── Goal type enum ────────────────────────────────────────────────────────

    /// <summary>
    /// Determines how a daily challenge accumulates progress from match records.
    /// </summary>
    public enum DailyChallengeGoalType
    {
        /// <summary>Accumulate total damage dealt across one or more matches.</summary>
        DamageTotal,

        /// <summary>Win N matches today.</summary>
        WinCount,

        /// <summary>Complete (play) N matches today, regardless of outcome.</summary>
        PlayCount,

        /// <summary>Earn N total currency across matches today.</summary>
        CurrencyTotal,
    }

    // ── Definition SO ─────────────────────────────────────────────────────────

    /// <summary>
    /// Immutable data asset that defines a single daily-challenge variant.
    ///
    /// The <see cref="DailyChallengeProgressSO"/> selects one of these each UTC day
    /// from a catalog list using a date-seeded deterministic random.
    ///
    /// ── Architecture ──────────────────────────────────────────────────────────
    ///   • <c>BattleRobots.Core</c> only — no Physics or UI references.
    ///   • Immutable at runtime — all serialised fields are read via properties.
    ///   • <see cref="ComputeProgress"/> is called on the cold post-match path;
    ///     allocation here is acceptable.
    ///
    /// Create via: Assets ▶ Create ▶ BattleRobots ▶ DailyChallenge ▶ Definition
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/DailyChallenge/Definition",
        order    = 1)]
    public sealed class DailyChallengeDefinitionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Unique machine identifier for this challenge, e.g. 'daily_dmg_500'. " +
                 "Used as the persistence key — never change after shipping.")]
        [SerializeField] private string _challengeId;

        [Tooltip("Short player-facing description shown in DailyChallengeUI, " +
                 "e.g. 'Deal 500 damage across your battles today.'")]
        [SerializeField, TextArea(2, 4)] private string _description;

        [Tooltip("How progress is accumulated from each MatchRecord.")]
        [SerializeField] private DailyChallengeGoalType _goalType;

        [Tooltip("Amount of progress needed to complete the challenge (e.g. 500 for DamageTotal).")]
        [SerializeField, Min(1f)] private float _targetValue = 1f;

        [Tooltip("Currency credited to PlayerWallet when the reward is claimed.")]
        [SerializeField, Min(1)] private int _rewardCurrency = 100;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Unique machine identifier for this challenge variant.</summary>
        public string ChallengeId => _challengeId;

        /// <summary>Player-facing challenge description.</summary>
        public string Description => _description;

        /// <summary>Goal category that determines how progress is computed.</summary>
        public DailyChallengeGoalType GoalType => _goalType;

        /// <summary>Progress units required to complete the challenge.</summary>
        public float TargetValue => _targetValue;

        /// <summary>Currency awarded to the player on reward claim.</summary>
        public int RewardCurrency => _rewardCurrency;

        /// <summary>
        /// Returns the progress contribution from a single <see cref="MatchRecord"/>.
        ///
        /// <list type="bullet">
        ///   <item><see cref="DailyChallengeGoalType.DamageTotal"/> → <c>record.damageDone</c></item>
        ///   <item><see cref="DailyChallengeGoalType.WinCount"/>    → 1 if <c>record.playerWon</c>, else 0</item>
        ///   <item><see cref="DailyChallengeGoalType.PlayCount"/>   → 1 always</item>
        ///   <item><see cref="DailyChallengeGoalType.CurrencyTotal"/>→ <c>record.currencyEarned</c></item>
        /// </list>
        ///
        /// Returns 0 for a <c>null</c> record.
        /// </summary>
        public float ComputeProgress(MatchRecord record)
        {
            if (record == null) return 0f;

            switch (_goalType)
            {
                case DailyChallengeGoalType.DamageTotal:
                    return record.damageDone;

                case DailyChallengeGoalType.WinCount:
                    return record.playerWon ? 1f : 0f;

                case DailyChallengeGoalType.PlayCount:
                    return 1f;

                case DailyChallengeGoalType.CurrencyTotal:
                    return record.currencyEarned;

                default:
                    return 0f;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_challengeId))
                Debug.LogWarning(
                    $"[DailyChallengeDefinitionSO] '{name}' has an empty ChallengeId. " +
                    "Set a unique ID before shipping.", this);
        }
#endif
    }
}
