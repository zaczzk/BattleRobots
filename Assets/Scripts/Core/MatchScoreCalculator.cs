using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Computes a numeric match score from a <see cref="MatchResultSO"/> blackboard.
    ///
    /// ── Scoring formula ───────────────────────────────────────────────────────
    ///   Base      : 1 000 (win) or 100 (loss)
    ///   Time bonus: max(0, floor(600 − DurationSeconds × 5))  [wins only]
    ///               Rewards fast finishes: 0 s → +600, 60 s → +300, 120 s → 0.
    ///   Damage    : floor(DamageDone × 2) − floor(DamageTaken)
    ///   Bonus ×3  : BonusEarned × 3  (performance-condition credits triple)
    ///   Final     : max(0, sum of above)  — score is always non-negative.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - Static utility class — no MonoBehaviour, no ScriptableObject.
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Allocation-free (pure integer arithmetic on MatchResultSO properties).
    ///   - Null <paramref name="result"/> → returns 0 (safe to call without guard).
    /// </summary>
    public static class MatchScoreCalculator
    {
        /// <summary>
        /// Compute the match score from a populated <see cref="MatchResultSO"/>.
        /// </summary>
        /// <param name="result">
        /// Blackboard written by <see cref="MatchManager"/> before MatchEnded fires.
        /// Passing <c>null</c> returns 0.
        /// </param>
        /// <param name="maxCombo">
        /// Optional: highest combo streak achieved during the match
        /// (read from <see cref="ComboCounterSO.MaxCombo"/>).
        /// Each combo hit contributes 5 bonus points — e.g. MaxCombo of 10 adds +50.
        /// Defaults to 0 (backwards-compatible: existing callers unaffected).
        /// </param>
        /// <param name="scoreMultiplier">
        /// Optional runtime <see cref="ScoreMultiplierSO"/> applied to the final clamped
        /// score as <c>Mathf.RoundToInt(clamped × Multiplier)</c>.
        /// Passing <c>null</c> skips multiplication (backwards-compatible).
        /// Assigned by <see cref="MatchManager"/> when a prestige bonus is active.
        /// </param>
        /// <param name="combinedBonus">
        /// Optional <see cref="CombinedBonusCalculatorSO"/> applied as a second multiplicative
        /// pass after <paramref name="scoreMultiplier"/>: <c>Mathf.RoundToInt(clamped × FinalMultiplier)</c>.
        /// Passing <c>null</c> skips this pass (backwards-compatible).
        /// Use when both prestige-tier and mastery-tier multipliers should be combined
        /// before final score output.
        /// </param>
        /// <returns>Non-negative integer score.</returns>
        public static int Calculate(MatchResultSO result, int maxCombo = 0,
                                    ScoreMultiplierSO scoreMultiplier = null,
                                    CombinedBonusCalculatorSO combinedBonus = null)
        {
            if (result == null) return 0;

            // Base points: winning is worth substantially more than surviving a loss.
            int score = result.PlayerWon ? 1000 : 100;

            // Time bonus — wins only; rewards fast finishes.
            // Formula: max(0, 600 − duration×5)
            //   0 s  → +600  (instant KO)
            //   60 s → +300  (half-time win)
            //   120s → 0     (just before timer)
            //   >120s→ 0     (clamped)
            if (result.PlayerWon)
            {
                int timeBonus = Mathf.Max(
                    0,
                    Mathf.FloorToInt(600f - result.DurationSeconds * 5f));
                score += timeBonus;
            }

            // Damage contribution: dealing damage is worth twice as much as taking it.
            score += Mathf.FloorToInt(result.DamageDone * 2f);
            score -= Mathf.FloorToInt(result.DamageTaken);

            // Performance-condition bonus credits triple in score (already included in
            // CurrencyEarned by MatchManager; tripling here rewards skilled play extra).
            score += result.BonusEarned * 3;

            // Combo bonus: 5 points per hit in the best streak achieved this match.
            // Rewards aggressive, consistent play (MaxCombo of 10 → +50, capped by game length).
            score += Mathf.Max(0, maxCombo) * 5;

            // Clamp to non-negative — heavy damage taken should not produce a negative score.
            int clamped = Mathf.Max(0, score);

            // Apply optional prestige / mastery score multiplier.
            // Multiplier is already clamped to [0.01, 10] by ScoreMultiplierSO.SetMultiplier.
            if (scoreMultiplier != null)
                clamped = Mathf.RoundToInt(clamped * scoreMultiplier.Multiplier);

            // Apply optional combined bonus (prestige × mastery aggregate).
            // FinalMultiplier is clamped to [0.01, 10] by CombinedBonusCalculatorSO.
            if (combinedBonus != null)
                clamped = Mathf.RoundToInt(clamped * combinedBonus.FinalMultiplier);

            return clamped;
        }
    }
}
