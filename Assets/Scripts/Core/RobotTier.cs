using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    // ── Tier enum ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Classification tiers for a robot build, ordered by prestige.
    /// <c>Unranked</c> is the default when no threshold has been met.
    /// The integer backing values are significant: higher tier = larger int,
    /// which allows direct <c>&gt;=</c> comparison in gating logic.
    /// </summary>
    public enum RobotTierLevel
    {
        Unranked = 0,
        Bronze   = 1,
        Silver   = 2,
        Gold     = 3,
        Platinum = 4,
        Diamond  = 5,
    }

    // ── Per-tier config entry ──────────────────────────────────────────────────────

    /// <summary>
    /// One row in a <see cref="RobotTierConfig"/> that maps a minimum build-rating
    /// threshold to a <see cref="RobotTierLevel"/>, display name, and badge colour.
    /// </summary>
    [Serializable]
    public struct TierThresholdEntry
    {
        [Tooltip("Minimum build-power rating required to reach this tier.")]
        [Min(0)] public int ratingThreshold;

        [Tooltip("The tier awarded when ratingThreshold is met.")]
        public RobotTierLevel tier;

        [Tooltip("Player-facing tier name shown in the HUD (e.g. 'Gold Warrior').  "
               + "Leave blank to fall back to the tier enum name.")]
        public string displayName;

        [Tooltip("Badge / highlight colour used for this tier in the pre-match UI.")]
        public Color tintColor;
    }

    // ── Config ScriptableObject ───────────────────────────────────────────────────

    /// <summary>
    /// Immutable SO that maps build-power rating ranges to
    /// <see cref="RobotTierLevel"/> classifications.
    ///
    /// ── Tier resolution ───────────────────────────────────────────────────────────
    ///   <see cref="GetTier"/> returns the <see cref="RobotTierLevel"/> with the
    ///   <em>highest enum value</em> whose <c>ratingThreshold</c> ≤ the supplied
    ///   rating.  This means:
    ///   <list type="bullet">
    ///     <item>Entries may be added in any order.</item>
    ///     <item>Gaps are fine — a player with rating 750 in a config that only
    ///       defines Bronze=100 and Diamond=1000 will be <c>Bronze</c>.</item>
    ///     <item>If no entry threshold is met the result is <c>Unranked</c>.</item>
    ///   </list>
    ///
    /// ── Suggested defaults ────────────────────────────────────────────────────────
    ///   Bronze 100 · Silver 300 · Gold 600 · Platinum 900 · Diamond 1 200
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics / UI references.
    ///   • SO is immutable at runtime (no public setters).
    ///   • All accessors are null-safe and return sensible fallbacks.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ RobotTierConfig.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/RobotTierConfig",
        fileName = "RobotTierConfig")]
    public sealed class RobotTierConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("One entry per tier.  Entries may be in any order.  "
               + "Each entry specifies the minimum rating to reach that tier.")]
        [SerializeField] private List<TierThresholdEntry> _thresholds
            = new List<TierThresholdEntry>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Read-only view of all configured tier-threshold entries.</summary>
        public IReadOnlyList<TierThresholdEntry> Thresholds => _thresholds;

        /// <summary>
        /// Returns the highest <see cref="RobotTierLevel"/> whose configured
        /// <c>ratingThreshold</c> is ≤ <paramref name="rating"/>.
        /// Returns <see cref="RobotTierLevel.Unranked"/> when the threshold list
        /// is empty or no threshold is met.
        /// </summary>
        public RobotTierLevel GetTier(int rating)
        {
            if (_thresholds == null || _thresholds.Count == 0)
                return RobotTierLevel.Unranked;

            RobotTierLevel best = RobotTierLevel.Unranked;
            for (int i = 0; i < _thresholds.Count; i++)
            {
                var entry = _thresholds[i];
                if (rating >= entry.ratingThreshold && entry.tier > best)
                    best = entry.tier;
            }
            return best;
        }

        /// <summary>
        /// Returns the display name configured for <paramref name="tier"/>.
        /// Falls back to <c>tier.ToString()</c> when no matching entry exists
        /// or when the configured <c>displayName</c> is null / empty.
        /// </summary>
        public string GetDisplayName(RobotTierLevel tier)
        {
            for (int i = 0; i < _thresholds.Count; i++)
            {
                if (_thresholds[i].tier == tier)
                {
                    var name = _thresholds[i].displayName;
                    return string.IsNullOrEmpty(name) ? tier.ToString() : name;
                }
            }
            return tier.ToString();
        }

        /// <summary>
        /// Returns the badge tint colour configured for <paramref name="tier"/>,
        /// or <c>Color.white</c> when no matching entry exists.
        /// </summary>
        public Color GetTierColor(RobotTierLevel tier)
        {
            for (int i = 0; i < _thresholds.Count; i++)
                if (_thresholds[i].tier == tier)
                    return _thresholds[i].tintColor;
            return Color.white;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_thresholds == null) return;

            var seen = new HashSet<RobotTierLevel>();
            for (int i = 0; i < _thresholds.Count; i++)
            {
                if (!seen.Add(_thresholds[i].tier))
                    Debug.LogWarning(
                        $"[RobotTierConfig] Duplicate tier '{_thresholds[i].tier}' at index {i}. "
                        + "Only one entry per tier is expected — the first match wins in "
                        + "GetDisplayName/GetTierColor; GetTier returns the highest-valued tier met.",
                        this);
            }
        }
#endif
    }
}
