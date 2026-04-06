using System.Collections.Generic;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable aggregate statistics computed from a match history list.
    ///
    /// Create via <see cref="Compute"/>. The struct is stack-allocated; no heap
    /// allocations are made after the initial single-pass enumeration.
    ///
    /// Usage:
    ///   LeaderboardStats stats = LeaderboardStats.Compute(saveData.matchHistory);
    ///   // read stats.Wins, stats.WinRatePercent, etc.
    /// </summary>
    public readonly struct LeaderboardStats
    {
        // ── Aggregate fields ──────────────────────────────────────────────────

        /// <summary>Total number of recorded matches.</summary>
        public readonly int MatchCount;

        /// <summary>Matches where the player won.</summary>
        public readonly int Wins;

        /// <summary>Matches where the player lost.</summary>
        public readonly int Losses;

        /// <summary>Win percentage in [0, 100]. 0 when no matches played.</summary>
        public readonly float WinRatePercent;

        /// <summary>Average damage dealt per match. 0 when no matches played.</summary>
        public readonly float AvgDamageDealt;

        /// <summary>Average damage taken per match. 0 when no matches played.</summary>
        public readonly float AvgDamageTaken;

        /// <summary>Sum of all currency earned across every recorded match.</summary>
        public readonly int TotalEarnings;

        /// <summary>Average duration of a match in seconds. 0 when no matches played.</summary>
        public readonly float AvgDurationSeconds;

        // ── Constructor (private — use Compute) ───────────────────────────────

        private LeaderboardStats(
            int matchCount, int wins, int losses,
            float winRatePercent,
            float avgDamageDealt, float avgDamageTaken,
            int totalEarnings, float avgDurationSeconds)
        {
            MatchCount         = matchCount;
            Wins               = wins;
            Losses             = losses;
            WinRatePercent     = winRatePercent;
            AvgDamageDealt     = avgDamageDealt;
            AvgDamageTaken     = avgDamageTaken;
            TotalEarnings      = totalEarnings;
            AvgDurationSeconds = avgDurationSeconds;
        }

        // ── Factory ───────────────────────────────────────────────────────────

        /// <summary>
        /// Computes aggregate statistics from <paramref name="history"/> in a single pass.
        /// Returns a zeroed struct if the list is null or empty.
        /// </summary>
        /// <param name="history">Match records from <see cref="SaveData.matchHistory"/>.</param>
        public static LeaderboardStats Compute(List<MatchRecord> history)
        {
            if (history == null || history.Count == 0)
                return new LeaderboardStats(0, 0, 0, 0f, 0f, 0f, 0, 0f);

            int   matchCount       = history.Count;
            int   wins             = 0;
            float totalDamageDealt = 0f;
            float totalDamageTaken = 0f;
            int   totalEarnings    = 0;
            float totalDuration    = 0f;

            for (int i = 0; i < matchCount; i++)
            {
                MatchRecord r = history[i];
                if (r.playerWon) wins++;
                totalDamageDealt += r.damageDone;
                totalDamageTaken += r.damageTaken;
                totalEarnings    += r.currencyEarned;
                totalDuration    += r.durationSeconds;
            }

            int   losses           = matchCount - wins;
            float winRate          = (wins / (float)matchCount) * 100f;
            float avgDamageDealt   = totalDamageDealt / matchCount;
            float avgDamageTaken   = totalDamageTaken / matchCount;
            float avgDuration      = totalDuration    / matchCount;

            return new LeaderboardStats(
                matchCount, wins, losses,
                winRate,
                avgDamageDealt, avgDamageTaken,
                totalEarnings, avgDuration);
        }
    }
}
