using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Testable core logic that analyses a set of <see cref="OpponentProfileSO"/> assets
    /// and reports any that are missing a <see cref="DamageResistanceConfig"/> or
    /// <see cref="DamageVulnerabilityConfig"/>.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   This static class contains no Unity Editor dependencies so it can be called
    ///   from EditMode unit tests without AssetDatabase access.
    ///   The Editor window <c>OpponentDamageProfileValidator</c> (BattleRobots.Editor)
    ///   gathers profiles via AssetDatabase and then delegates to
    ///   <see cref="Analyse"/> to compute the issue list.
    ///
    /// ── Issue semantics ──────────────────────────────────────────────────────────
    ///   A <see cref="ProfileIssue"/> is only generated when at least one config is
    ///   missing.  Profiles with both configs assigned produce no issue.
    ///   Null entries in the input list are skipped silently.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI / Editor references.
    ///   - Zero allocation on fully-configured runs (empty result list).
    /// </summary>
    public static class OpponentDamageProfileAnalyser
    {
        // ── Issue record ──────────────────────────────────────────────────────────

        /// <summary>
        /// Describes one under-configured <see cref="OpponentProfileSO"/> asset.
        /// At least one flag (<see cref="MissingResistance"/> or
        /// <see cref="MissingVulnerability"/>) is always true when an issue is reported.
        /// </summary>
        public readonly struct ProfileIssue
        {
            /// <summary>The OpponentProfileSO that is missing one or both damage configs.</summary>
            public readonly OpponentProfileSO Profile;

            /// <summary>True when <see cref="OpponentProfileSO.OpponentResistance"/> is null.</summary>
            public readonly bool MissingResistance;

            /// <summary>True when <see cref="OpponentProfileSO.OpponentVulnerability"/> is null.</summary>
            public readonly bool MissingVulnerability;

            /// <summary>Constructs a new <see cref="ProfileIssue"/>.</summary>
            public ProfileIssue(OpponentProfileSO profile, bool missingResistance, bool missingVulnerability)
            {
                Profile              = profile;
                MissingResistance    = missingResistance;
                MissingVulnerability = missingVulnerability;
            }
        }

        // ── Analysis API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Scans <paramref name="profiles"/> and returns one <see cref="ProfileIssue"/>
        /// for each profile that is missing a <see cref="DamageResistanceConfig"/>,
        /// a <see cref="DamageVulnerabilityConfig"/>, or both.
        ///
        /// Returns an empty (non-null) list when <paramref name="profiles"/> is null,
        /// empty, or every entry is fully configured.
        /// Null entries within the list are skipped silently.
        /// </summary>
        public static List<ProfileIssue> Analyse(IReadOnlyList<OpponentProfileSO> profiles)
        {
            var issues = new List<ProfileIssue>();
            if (profiles == null) return issues;

            for (int i = 0; i < profiles.Count; i++)
            {
                OpponentProfileSO profile = profiles[i];
                if (profile == null) continue;

                bool missingRes  = profile.OpponentResistance    == null;
                bool missingVuln = profile.OpponentVulnerability == null;

                if (missingRes || missingVuln)
                    issues.Add(new ProfileIssue(profile, missingRes, missingVuln));
            }

            return issues;
        }
    }
}
