using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Stateless helper that evaluates <see cref="BonusConditionSO"/> conditions against
    /// match-end statistics and returns bonus credit totals.
    ///
    /// Called by <see cref="MatchManager"/> in <c>EndMatch()</c> when a
    /// <see cref="MatchBonusCatalogSO"/> is assigned to the optional <c>_bonusCatalog</c>
    /// field.  The returned bonus is added to the match reward before the wallet is credited
    /// and before the MatchRecord is built, so both reflect the final boosted amount.
    ///
    /// Architecture notes:
    ///   • All methods are static — no instance state, no MonoBehaviour dependency.
    ///   • Allocation-free: iterates IReadOnlyList by index (no enumerator alloc).
    ///   • No Unity Physics or UI namespace references.
    /// </summary>
    public static class MatchEndBonusEvaluator
    {
        /// <summary>
        /// Iterates every condition in <paramref name="catalog"/>, evaluates each against
        /// the supplied match-end data, and returns the sum of all satisfied bonus amounts.
        ///
        /// Returns 0 when <paramref name="catalog"/> is null, the condition list is empty,
        /// or no conditions are satisfied.  Null entries within the catalog are skipped.
        /// </summary>
        /// <param name="playerWon">Whether the local player won the match.</param>
        /// <param name="durationSeconds">Elapsed match time in seconds.</param>
        /// <param name="damageDone">Total damage dealt by the player this match.</param>
        /// <param name="damageTaken">Total damage received by the player this match.</param>
        /// <param name="catalog">Catalog to evaluate; null returns 0 immediately.</param>
        /// <returns>Total bonus credits from all satisfied conditions. Always ≥ 0.</returns>
        public static int Evaluate(
            bool playerWon,
            float durationSeconds,
            float damageDone,
            float damageTaken,
            MatchBonusCatalogSO catalog)
        {
            if (catalog == null) return 0;

            var conditions = catalog.Conditions;
            int total = 0;

            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                if (condition == null) continue;

                if (EvaluateCondition(condition, playerWon, durationSeconds, damageDone, damageTaken))
                    total += condition.BonusAmount;
            }

            return total;
        }

        /// <summary>
        /// Evaluates a single <see cref="BonusConditionSO"/> against the supplied match-end data.
        ///
        /// All condition types require <paramref name="playerWon"/> to be true as a prerequisite.
        /// Returns false when <paramref name="condition"/> is null.
        /// </summary>
        /// <param name="condition">The condition to evaluate; null returns false.</param>
        /// <param name="playerWon">Whether the local player won the match.</param>
        /// <param name="durationSeconds">Elapsed match time in seconds.</param>
        /// <param name="damageDone">Total damage dealt by the player this match.</param>
        /// <param name="damageTaken">Total damage received by the player this match.</param>
        /// <returns>True if the condition is satisfied; false otherwise.</returns>
        public static bool EvaluateCondition(
            BonusConditionSO condition,
            bool playerWon,
            float durationSeconds,
            float damageDone,
            float damageTaken)
        {
            if (condition == null) return false;

            // All condition types require the player to have won.
            if (!playerWon) return false;

            switch (condition.ConditionType)
            {
                case BonusConditionType.NoDamageTaken:
                    // Satisfied when damage taken is at or below the threshold.
                    // Threshold = 0 → true "no damage taken" perfect-shield bonus.
                    return damageTaken <= condition.Threshold;

                case BonusConditionType.WonUnderDuration:
                    // Satisfied when the match ended at or before the threshold in seconds.
                    return durationSeconds <= condition.Threshold;

                case BonusConditionType.DamageDealtExceeds:
                    // Satisfied when the player dealt at least threshold damage.
                    return damageDone >= condition.Threshold;

                case BonusConditionType.DamageEfficiency:
                {
                    // Avoid divide-by-zero: zero total damage → efficiency 0.
                    float totalDamage = damageDone + damageTaken;
                    if (totalDamage <= 0f) return false;
                    float efficiency = damageDone / totalDamage;
                    return efficiency >= condition.Threshold;
                }

                default:
                    // Unknown or future condition type: conservatively return false.
                    Debug.LogWarning($"[MatchEndBonusEvaluator] Unhandled BonusConditionType " +
                                     $"'{condition.ConditionType}' — returning false.");
                    return false;
            }
        }
    }
}
