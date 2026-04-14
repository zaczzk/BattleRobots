using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that aggregates two independent score-bonus sources into a single
    /// <see cref="FinalMultiplier"/> value consumed by
    /// <see cref="MatchScoreCalculator.Calculate"/>.
    ///
    /// ── Bonus sources ─────────────────────────────────────────────────────────────
    ///   1. <see cref="ScoreMultiplierSO"/> — prestige-tier flat multiplier set by
    ///      <see cref="BattleRobots.Physics.PrestigeRewardBonusApplier"/> at match start.
    ///   2. <see cref="MasteryBonusCatalogSO"/> — product of all active per-type mastery
    ///      bonuses, evaluated against the live <see cref="DamageTypeMasterySO"/> state.
    ///
    /// ── FinalMultiplier formula ───────────────────────────────────────────────────
    ///   prestigeM  = <see cref="_scoreMultiplier"/>?.Multiplier   ?? 1f
    ///   masteryM   = <see cref="_masteryBonusCatalog"/>?.<see cref="MasteryBonusCatalogSO.GetTotalMultiplier"/> ?? 1f
    ///   FinalMultiplier = Clamp(prestigeM × masteryM, 0.01, 10)
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO asset is immutable at runtime — component SOs hold live state.
    ///   - Zero alloc: FinalMultiplier is a pure computed float property.
    ///   - All component SOs are optional; absent refs default to 1× contribution.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ CombinedBonusCalculator.
    /// Assign to <see cref="MatchManager"/> for the optional 4th score-multiply pass.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/CombinedBonusCalculator",
        fileName = "CombinedBonusCalculatorSO")]
    public sealed class CombinedBonusCalculatorSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Bonus Sources (all optional)")]
        [Tooltip("Prestige-tier score multiplier. Provides Multiplier in [0.01, 10]. " +
                 "Leave null to treat the prestige contribution as 1×.")]
        [SerializeField] private ScoreMultiplierSO _scoreMultiplier;

        [Tooltip("Catalog of mastery-gated bonus multipliers. " +
                 "GetTotalMultiplier is the product of all active entries. " +
                 "Leave null to treat the mastery contribution as 1×.")]
        [SerializeField] private MasteryBonusCatalogSO _masteryBonusCatalog;

        [Tooltip("Live mastery state SO required by MasteryBonusCatalogSO to evaluate " +
                 "which entries are active. Leave null to treat all entries as inactive.")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Aggregated score multiplier combining the prestige and mastery contributions.
        ///
        /// <para>Formula: <c>Clamp(prestigeMultiplier × masteryMultiplier, 0.01, 10)</c>
        /// where each term defaults to 1× when its source SO is unassigned.</para>
        ///
        /// <para>Zero alloc — computed from existing runtime SO values.</para>
        /// </summary>
        public float FinalMultiplier
        {
            get
            {
                float prestigeM = _scoreMultiplier  != null ? _scoreMultiplier.Multiplier              : 1f;
                float masteryM  = _masteryBonusCatalog != null
                                  ? _masteryBonusCatalog.GetTotalMultiplier(_mastery)
                                  : 1f;
                return Mathf.Clamp(prestigeM * masteryM, 0.01f, 10f);
            }
        }

        /// <summary>The optional <see cref="ScoreMultiplierSO"/>. May be null.</summary>
        public ScoreMultiplierSO ScoreMultiplier => _scoreMultiplier;

        /// <summary>The optional <see cref="MasteryBonusCatalogSO"/>. May be null.</summary>
        public MasteryBonusCatalogSO MasteryBonusCatalog => _masteryBonusCatalog;

        /// <summary>The optional <see cref="DamageTypeMasterySO"/>. May be null.</summary>
        public DamageTypeMasterySO Mastery => _mastery;
    }
}
