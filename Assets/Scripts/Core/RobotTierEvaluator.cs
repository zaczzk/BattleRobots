namespace BattleRobots.Core
{
    /// <summary>
    /// Stateless helper that resolves a <see cref="RobotTierLevel"/> from a
    /// <see cref="BuildRatingSO"/> and a <see cref="RobotTierConfig"/>, and tests
    /// whether a build meets a minimum tier requirement.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────────
    ///   Tier display (pre-match UI):
    ///     <code>var tier = RobotTierEvaluator.EvaluateTier(_buildRating, _tierConfig);</code>
    ///
    ///   Tournament entry gating:
    ///     <code>
    ///     if (!RobotTierEvaluator.MeetsTierRequirement(_buildRating, _tierConfig, RobotTierLevel.Gold))
    ///         ShowLockedMessage();
    ///     </code>
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics / UI references.
    ///   • Both methods are null-safe: a null argument returns the most
    ///     conservative result (<c>Unranked</c> / <c>false for non-Unranked required</c>).
    ///   • Zero allocations — no collections created at runtime.
    /// </summary>
    public static class RobotTierEvaluator
    {
        /// <summary>
        /// Returns the <see cref="RobotTierLevel"/> for the player's current
        /// build rating as defined by <paramref name="config"/>.
        ///
        /// Returns <see cref="RobotTierLevel.Unranked"/> when either argument is null.
        /// </summary>
        public static RobotTierLevel EvaluateTier(
            BuildRatingSO  buildRating,
            RobotTierConfig config)
        {
            if (buildRating == null || config == null)
                return RobotTierLevel.Unranked;

            return config.GetTier(buildRating.CurrentRating);
        }

        /// <summary>
        /// Returns <c>true</c> when the player's current build tier is greater
        /// than or equal to <paramref name="requiredTier"/>.
        ///
        /// Null arguments: <see cref="EvaluateTier"/> returns <c>Unranked</c>
        /// (0), so:
        ///   • <c>requiredTier == Unranked</c> (0) → <c>true</c>.
        ///   • any higher tier                     → <c>false</c>.
        /// </summary>
        public static bool MeetsTierRequirement(
            BuildRatingSO   buildRating,
            RobotTierConfig config,
            RobotTierLevel  requiredTier)
        {
            return EvaluateTier(buildRating, config) >= requiredTier;
        }
    }
}
