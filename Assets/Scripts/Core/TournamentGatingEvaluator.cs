namespace BattleRobots.Core
{
    /// <summary>
    /// Stateless helper that evaluates whether the player's current build meets
    /// the tier and rating requirements defined on a <see cref="TournamentConfig"/>.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────────
    ///   Gate check (TournamentManager / TournamentController):
    ///     <code>
    ///     if (!TournamentGatingEvaluator.IsUnlocked(config, buildRating, tierConfig))
    ///         ShowLockedUI(TournamentGatingEvaluator.GetLockReason(config, buildRating, tierConfig));
    ///     </code>
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics / UI references.
    ///   • All methods are null-safe and allocation-free on the "unlocked" path.
    ///   • <c>null config</c> → no requirements → always unlocked (backwards-compatible).
    ///   • <c>null buildRating</c> → treated as 0 rating / Unranked (most conservative build).
    ///   • <c>null tierConfig</c> → tier display names fall back to enum.ToString().
    /// </summary>
    public static class TournamentGatingEvaluator
    {
        /// <summary>
        /// Returns <c>true</c> when the player's build satisfies all requirements
        /// defined by <paramref name="config"/>.
        ///
        /// <list type="bullet">
        ///   <item><c>null config</c>   → <c>true</c> (no requirements).</item>
        ///   <item>RequiredTier == Unranked and MinRating == 0 → <c>true</c>.</item>
        ///   <item>Otherwise checks tier via <see cref="RobotTierEvaluator"/> and
        ///         rating via <see cref="BuildRatingSO.CurrentRating"/>.</item>
        /// </list>
        ///
        /// Zero allocation on both locked and unlocked paths.
        /// </summary>
        public static bool IsUnlocked(
            TournamentConfig config,
            BuildRatingSO    buildRating,
            RobotTierConfig  tierConfig)
        {
            if (config == null) return true;

            // Tier requirement
            bool tierMet = RobotTierEvaluator.MeetsTierRequirement(
                buildRating, tierConfig, config.RequiredTier);

            // Rating requirement
            int currentRating = buildRating != null ? buildRating.CurrentRating : 0;
            bool ratingMet    = currentRating >= config.MinRating;

            return tierMet && ratingMet;
        }

        /// <summary>
        /// Returns a human-readable string explaining the first failing requirement,
        /// or <see cref="string.Empty"/> when the player qualifies.
        ///
        /// Tier requirement is evaluated first; if the tier gate fails, the rating
        /// gate is not reported (one reason at a time for cleaner UX).
        ///
        /// Zero allocation when the tournament is unlocked (returns <c>string.Empty</c>).
        /// Allocates a formatted string only on the locked path.
        /// </summary>
        public static string GetLockReason(
            TournamentConfig config,
            BuildRatingSO    buildRating,
            RobotTierConfig  tierConfig)
        {
            if (config == null) return string.Empty;

            // ── Tier check ────────────────────────────────────────────────────
            bool tierMet = RobotTierEvaluator.MeetsTierRequirement(
                buildRating, tierConfig, config.RequiredTier);

            if (!tierMet)
            {
                RobotTierLevel current = RobotTierEvaluator.EvaluateTier(buildRating, tierConfig);
                string reqName = tierConfig != null
                    ? tierConfig.GetDisplayName(config.RequiredTier)
                    : config.RequiredTier.ToString();
                string curName = tierConfig != null
                    ? tierConfig.GetDisplayName(current)
                    : current.ToString();
                return $"Requires {reqName} tier (current: {curName})";
            }

            // ── Rating check ──────────────────────────────────────────────────
            int currentRating = buildRating != null ? buildRating.CurrentRating : 0;
            if (currentRating < config.MinRating)
                return $"Requires {config.MinRating}+ rating (current: {currentRating})";

            return string.Empty;
        }
    }
}
