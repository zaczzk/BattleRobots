using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Classifies the performance metric evaluated by a <see cref="BonusConditionSO"/>.
    /// </summary>
    public enum BonusConditionType
    {
        /// <summary>
        /// Player won AND total damage taken is at or below <c>Threshold</c>.
        /// Set Threshold = 0 for a true "perfect shield" (zero damage taken) bonus.
        /// </summary>
        NoDamageTaken,

        /// <summary>
        /// Player won AND match duration (seconds) is at or below <c>Threshold</c>.
        /// Rewards quick victories.
        /// </summary>
        WonUnderDuration,

        /// <summary>
        /// Player won AND total damage dealt is at or above <c>Threshold</c>.
        /// Rewards aggressive, high-output play.
        /// </summary>
        DamageDealtExceeds,

        /// <summary>
        /// Player won AND damage-efficiency ratio (damageDone / (damageDone + damageTaken))
        /// is at or above <c>Threshold</c> (range 0–1).
        /// Zero total damage yields an efficiency of 0 and never satisfies a positive threshold.
        /// </summary>
        DamageEfficiency,
    }

    /// <summary>
    /// A single performance-bonus condition evaluated by
    /// <see cref="MatchEndBonusEvaluator"/> at match end.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ BonusCondition.
    ///
    /// A condition is satisfied only when the player won AND the specific
    /// numerical criterion for the <see cref="BonusConditionType"/> is met.
    /// The <see cref="BonusAmount"/> is then added to the match reward by
    /// <see cref="MatchManager"/> when a <see cref="MatchBonusCatalogSO"/> is assigned.
    ///
    /// Architecture notes:
    ///   • SO asset is immutable at runtime (all setters are Editor-only).
    ///   • No Unity Physics or UI namespace references.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BonusCondition",
        menuName  = "BattleRobots/Economy/BonusCondition",
        order     = 0)]
    public sealed class BonusConditionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Condition")]
        [Tooltip("Which match statistic is evaluated against the threshold.")]
        [SerializeField] private BonusConditionType _conditionType;

        [Tooltip("Threshold whose meaning depends on ConditionType:\n" +
                 "• NoDamageTaken    — max damage taken allowed (0 = perfect).\n" +
                 "• WonUnderDuration — max match duration in seconds.\n" +
                 "• DamageDealtExceeds — min damage dealt required.\n" +
                 "• DamageEfficiency — min efficiency ratio in [0, 1].")]
        [SerializeField, Min(0f)] private float _threshold;

        [Header("Reward")]
        [Tooltip("Bonus credits added to the match reward when the condition is satisfied.")]
        [SerializeField, Min(0)] private int _bonusAmount = 50;

        [Header("Display (optional)")]
        [Tooltip("Short name shown on the post-match results screen.")]
        [SerializeField] private string _displayName = "";

        [Tooltip("One-line description shown alongside the bonus label.")]
        [SerializeField] private string _displayDescription = "";

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>Which match statistic is evaluated.</summary>
        public BonusConditionType ConditionType => _conditionType;

        /// <summary>Threshold value for the condition (non-negative).</summary>
        public float Threshold => _threshold;

        /// <summary>Bonus credits awarded when the condition is satisfied.</summary>
        public int BonusAmount => _bonusAmount;

        /// <summary>Short UI display name (may be empty).</summary>
        public string DisplayName => _displayName;

        /// <summary>One-line UI description (may be empty).</summary>
        public string DisplayDescription => _displayDescription;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_bonusAmount < 0) _bonusAmount = 0;
            if (_threshold   < 0f) _threshold  = 0f;

            if (string.IsNullOrWhiteSpace(_displayName))
                Debug.LogWarning($"[BonusConditionSO] '{name}': DisplayName is empty — " +
                                 "consider adding a name so the post-match UI can identify it.", this);
        }
#endif
    }
}
