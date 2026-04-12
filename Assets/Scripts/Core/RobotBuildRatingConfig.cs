using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable ScriptableObject that holds the weighting coefficients used by
    /// <see cref="RobotBuildRatingCalculator.Calculate"/> to produce a single
    /// composite "build power" score for the player's current robot.
    ///
    /// ── Scoring model ─────────────────────────────────────────────────────────
    ///   For each equipped part:
    ///     • Stat contribution  = (healthBonus
    ///                             + (speedMultiplier  - 1) × 50
    ///                             + (damageMultiplier - 1) × 50
    ///                             + armorRating) × <see cref="BaseStatWeight"/>
    ///     • Rarity contribution = <see cref="GetRarityPoints"/> for the part's tier
    ///     • Upgrade contribution = upgradeRegistry.GetTier(partId) × <see cref="UpgradeWeight"/>
    ///   Plus, for the whole loadout:
    ///     • Synergy contribution = activeSynergies.Count × <see cref="SynergyWeight"/>
    ///
    ///   Final rating = Mathf.Max(0, Mathf.RoundToInt(total)).
    ///
    /// ── Create ────────────────────────────────────────────────────────────────
    ///   Assets ▶ Create ▶ BattleRobots ▶ Core ▶ BuildRatingConfig
    ///   One shared SO for BuildRatingController and any stats-summary UI.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/BuildRatingConfig",
        fileName = "BuildRatingConfig")]
    public sealed class RobotBuildRatingConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Stat Weight")]
        [Tooltip("Multiplier applied to each part's combat-stat contribution "
               + "(health + speed-bonus*50 + damage-bonus*50 + armor). "
               + "Set to 0 to ignore raw stats in the rating.")]
        [SerializeField, Min(0f)] private float _baseStatWeight = 1f;

        [Header("Upgrade Weight")]
        [Tooltip("Rating points added per upgrade tier level across all equipped parts. "
               + "Example: a part at tier 2 adds 2 × UpgradeWeight to the rating.")]
        [SerializeField, Min(0f)] private float _upgradeWeight = 15f;

        [Header("Synergy Weight")]
        [Tooltip("Rating points added per active build synergy.")]
        [SerializeField, Min(0f)] private float _synergyWeight = 25f;

        [Header("Rarity Points (flat, per part)")]
        [Tooltip("Points awarded for a Common rarity part. Default 0.")]
        [SerializeField, Min(0)] private int _commonPoints    = 0;

        [Tooltip("Points awarded for an Uncommon rarity part.")]
        [SerializeField, Min(0)] private int _uncommonPoints  = 5;

        [Tooltip("Points awarded for a Rare rarity part.")]
        [SerializeField, Min(0)] private int _rarePoints      = 15;

        [Tooltip("Points awarded for an Epic rarity part.")]
        [SerializeField, Min(0)] private int _epicPoints      = 30;

        [Tooltip("Points awarded for a Legendary rarity part.")]
        [SerializeField, Min(0)] private int _legendaryPoints = 50;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Multiplier applied to each part's raw stat contribution.</summary>
        public float BaseStatWeight  => _baseStatWeight;

        /// <summary>Rating points added per upgrade tier level on each part.</summary>
        public float UpgradeWeight   => _upgradeWeight;

        /// <summary>Rating points added per active build synergy.</summary>
        public float SynergyWeight   => _synergyWeight;

        /// <summary>
        /// Flat rating points awarded for the given part rarity tier.
        /// Returns 0 for unrecognised rarity values.
        /// </summary>
        public int GetRarityPoints(PartRarity rarity)
        {
            switch (rarity)
            {
                case PartRarity.Common:    return _commonPoints;
                case PartRarity.Uncommon:  return _uncommonPoints;
                case PartRarity.Rare:      return _rarePoints;
                case PartRarity.Epic:      return _epicPoints;
                case PartRarity.Legendary: return _legendaryPoints;
                default:                   return 0;
            }
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _baseStatWeight  = Mathf.Max(0f, _baseStatWeight);
            _upgradeWeight   = Mathf.Max(0f, _upgradeWeight);
            _synergyWeight   = Mathf.Max(0f, _synergyWeight);
            _commonPoints    = Mathf.Max(0, _commonPoints);
            _uncommonPoints  = Mathf.Max(0, _uncommonPoints);
            _rarePoints      = Mathf.Max(0, _rarePoints);
            _epicPoints      = Mathf.Max(0, _epicPoints);
            _legendaryPoints = Mathf.Max(0, _legendaryPoints);
        }
#endif
    }
}
