using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Pure-math helper that converts a win-streak count into a reward multiplier
    /// and applies it to a base currency reward.
    ///
    /// ── Formula ───────────────────────────────────────────────────────────────
    ///   multiplier = 1.0 + 0.1 × min(streak, 5)
    ///
    ///   Streak │ Multiplier │ On 200-credit win
    ///   ───────┼────────────┼──────────────────
    ///     0    │   1.00 ×   │ 200
    ///     1    │   1.10 ×   │ 220
    ///     2    │   1.20 ×   │ 240
    ///     3    │   1.30 ×   │ 260
    ///     4    │   1.40 ×   │ 280
    ///    5+    │   1.50 ×   │ 300  (capped)
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - Static class — no MonoBehaviour, no ScriptableObject.
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Negative streak values treated as 0 (no penalty, no bonus).
    ///   - Negative base rewards return 0 (defensive guard).
    ///   - Result rounded to nearest integer via Mathf.RoundToInt.
    /// </summary>
    public static class StreakBonusCalculator
    {
        /// <summary>Bonus fraction added per streak level.</summary>
        public const float BonusPerStreak = 0.1f;

        /// <summary>Maximum streak level that contributes to the bonus.</summary>
        public const int MaxBonusStreak = 5;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the reward multiplier for the given streak count.
        /// Streak is clamped to [0, <see cref="MaxBonusStreak"/>].
        /// </summary>
        /// <param name="streak">Current consecutive-win count.</param>
        /// <returns>Float multiplier ≥ 1.0.</returns>
        public static float GetBonusMultiplier(int streak)
        {
            int clamped = Mathf.Clamp(streak, 0, MaxBonusStreak);
            return 1f + BonusPerStreak * clamped;
        }

        /// <summary>
        /// Applies the streak multiplier to <paramref name="baseReward"/> and returns
        /// the rounded integer result.  Negative base rewards return 0.
        /// </summary>
        /// <param name="baseReward">Currency awarded before streak bonus.</param>
        /// <param name="streak">Current consecutive-win count.</param>
        /// <returns>Total reward after streak bonus, rounded to nearest integer.</returns>
        public static int ApplyToReward(int baseReward, int streak)
        {
            if (baseReward <= 0) return 0;
            return Mathf.RoundToInt(baseReward * GetBonusMultiplier(streak));
        }
    }
}
