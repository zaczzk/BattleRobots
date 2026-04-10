using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Pure-math helper that converts match outcome, duration, and win streak
    /// into an XP reward for <see cref="PlayerProgressionSO.AddXP"/>.
    ///
    /// ── Formula ───────────────────────────────────────────────────────────────
    ///   xp = baseXP + durationBonus + streakBonus
    ///
    ///   baseXP        = 100 (win) or 25 (loss)
    ///   durationBonus = floor(max(0, durationSeconds) / 5)   [+1 per 5 seconds]
    ///   streakBonus   = 10 × min(winStreak, 5)               [+10 per streak level, capped at +50]
    ///
    ///   Example — win, 60-second match, streak of 3:
    ///     100 + 12 + 30 = 142 XP
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - Static class — no MonoBehaviour, no ScriptableObject.
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Negative duration is treated as 0 (no penalty, no bonus).
    ///   - Negative streak is treated as 0.
    ///   - Always returns a positive value (minimum = <see cref="BaseLossXP"/>).
    ///
    /// ── Caller note ───────────────────────────────────────────────────────────
    ///   <see cref="MatchManager"/> calls this after updating the WinStreakSO,
    ///   so <paramref name="winStreak"/> reflects the post-match streak value
    ///   (e.g. streak was 2, won → pass 3 for the +30 streak bonus).
    /// </summary>
    public static class XPRewardCalculator
    {
        /// <summary>Base XP awarded for winning a match.</summary>
        public const int BaseWinXP = 100;

        /// <summary>Base XP awarded for losing a match (consolation).</summary>
        public const int BaseLossXP = 25;

        /// <summary>XP added per <see cref="DurationIntervalSeconds"/> of match time.</summary>
        public const int XPPerDurationInterval = 1;

        /// <summary>Seconds of match time that earn one bonus XP point.</summary>
        public const float DurationIntervalSeconds = 5f;

        /// <summary>XP bonus added per streak level.</summary>
        public const int XPPerStreakLevel = 10;

        /// <summary>Maximum streak level that contributes to the XP bonus.</summary>
        public const int MaxStreakBonus = 5;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates total XP earned for one match.
        /// </summary>
        /// <param name="playerWon">True if the local player won the match.</param>
        /// <param name="durationSeconds">
        ///   Actual match duration in seconds (time elapsed, not time remaining).
        ///   Negative values are treated as 0.
        /// </param>
        /// <param name="winStreak">
        ///   Post-match consecutive-win streak count.
        ///   Pass 0 after a loss (MatchManager calls RecordLoss which resets the streak first).
        ///   Negative values are treated as 0.
        /// </param>
        /// <returns>Total XP to award.  Always &gt; 0.</returns>
        public static int CalculateMatchXP(bool playerWon, float durationSeconds, int winStreak)
        {
            int baseXP        = playerWon ? BaseWinXP : BaseLossXP;
            int durationBonus = Mathf.FloorToInt(Mathf.Max(0f, durationSeconds) / DurationIntervalSeconds);
            int streakBonus   = XPPerStreakLevel * Mathf.Clamp(winStreak, 0, MaxStreakBonus);

            return baseXP + durationBonus + streakBonus;
        }
    }
}
