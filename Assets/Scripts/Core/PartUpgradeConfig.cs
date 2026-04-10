using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable SO that defines the upgrade tier rules shared across all parts.
    ///
    /// ── Tier model ──────────────────────────────────────────────────────────
    ///   A part starts at tier 0 (no upgrade).  Each upgrade step costs
    ///   <see cref="_tierCosts"/>[tier] credits and advances the part by one tier
    ///   up to <see cref="_maxTier"/>.
    ///
    ///   At tier T the <see cref="RobotStatsAggregator.Compute(RobotDefinition,
    ///   IEnumerable{PartDefinition}, PlayerPartUpgrades, PartUpgradeConfig)"/>
    ///   overload multiplies each part's bonus stats by
    ///   <see cref="_tierStatMultipliers"/>[T] so that:
    ///     • tier 0 → multiplier 1.0 (base stats, no upgrade bonus)
    ///     • tier 3 → multiplier 1.5 (50 % stronger bonus stats)
    ///
    ///   The multiplier scales only the *bonus* component of speed/damage
    ///   multipliers (i.e. the amount above 1.0) to avoid exponential compounding.
    ///
    /// ── Create ──────────────────────────────────────────────────────────────
    ///   Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ PartUpgradeConfig
    ///   One global SO shared by UpgradeManager and LoadoutBuilderController.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Economy/PartUpgradeConfig", fileName = "PartUpgradeConfig")]
    public sealed class PartUpgradeConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum upgrade tier a part can reach.")]
        [SerializeField, Min(1)] private int _maxTier = 3;

        [Tooltip("Credits required to upgrade from tier N to tier N+1. " +
                 "Length must equal _maxTier. Index 0 = cost to reach tier 1.")]
        [SerializeField] private int[] _tierCosts = { 100, 250, 500 };

        [Tooltip("Stat-bonus multiplier applied at each tier. " +
                 "Length must equal _maxTier + 1. Index 0 = 1.0 (tier 0 = no bonus change).")]
        [SerializeField] private float[] _tierStatMultipliers = { 1.0f, 1.1f, 1.25f, 1.5f };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Maximum upgrade tier. Parts cannot exceed this value.</summary>
        public int MaxTier => _maxTier;

        /// <summary>Read-only view of per-tier upgrade costs.</summary>
        public IReadOnlyList<int> TierCosts => _tierCosts;

        /// <summary>Read-only view of per-tier stat multipliers (index = tier).</summary>
        public IReadOnlyList<float> TierStatMultipliers => _tierStatMultipliers;

        /// <summary>
        /// Cost to upgrade a part currently at <paramref name="currentTier"/> to
        /// <c>currentTier + 1</c>.
        /// Returns -1 if the part is already at max tier or the configuration is invalid.
        /// </summary>
        public int GetUpgradeCost(int currentTier)
        {
            int nextTier = currentTier + 1;
            if (nextTier < 1 || nextTier > _maxTier || _tierCosts == null
                || nextTier > _tierCosts.Length)
                return -1;
            return _tierCosts[nextTier - 1]; // index 0 = cost to reach tier 1
        }

        /// <summary>
        /// Stat-bonus multiplier for a given upgrade <paramref name="tier"/>.
        /// Returns 1.0 (no bonus) for out-of-range values.
        /// </summary>
        public float GetStatMultiplier(int tier)
        {
            if (_tierStatMultipliers == null || tier < 0 || tier >= _tierStatMultipliers.Length)
                return 1f;
            return _tierStatMultipliers[tier];
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_tierCosts != null && _tierCosts.Length != _maxTier)
                Debug.LogWarning(
                    $"[PartUpgradeConfig] {name}: _tierCosts.Length ({_tierCosts.Length}) " +
                    $"should equal _maxTier ({_maxTier}).", this);

            if (_tierStatMultipliers != null && _tierStatMultipliers.Length != _maxTier + 1)
                Debug.LogWarning(
                    $"[PartUpgradeConfig] {name}: _tierStatMultipliers.Length " +
                    $"({_tierStatMultipliers.Length}) should equal _maxTier + 1 ({_maxTier + 1}).", this);

            for (int i = 0; _tierCosts != null && i < _tierCosts.Length; i++)
                if (_tierCosts[i] <= 0)
                    Debug.LogWarning(
                        $"[PartUpgradeConfig] {name}: _tierCosts[{i}] should be > 0.", this);

            for (int i = 0; _tierStatMultipliers != null && i < _tierStatMultipliers.Length; i++)
                if (_tierStatMultipliers[i] <= 0f)
                    Debug.LogWarning(
                        $"[PartUpgradeConfig] {name}: _tierStatMultipliers[{i}] should be > 0.", this);
        }
#endif
    }
}
