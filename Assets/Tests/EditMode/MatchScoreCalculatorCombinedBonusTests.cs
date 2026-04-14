using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T190 — <see cref="MatchScoreCalculator.Calculate"/>
    /// optional 4th param (<see cref="CombinedBonusCalculatorSO"/> combinedBonus).
    ///
    /// MatchScoreCalculatorCombinedBonusTests (6):
    ///   NullCombinedBonus_ScoreUnchanged                     ×1
    ///   CombinedBonus_AppliesToClampedScore                  ×1
    ///   CombinedBonus_StacksWithScoreMultiplier              ×1
    ///   CombinedBonus_NullResult_ReturnsZero                 ×1
    ///   CombinedBonus_MultiplierOneX_ScoreUnchanged          ×1
    ///   CombinedBonus_MultiplierTwo_DoublesScore             ×1
    /// </summary>
    public class MatchScoreCalculatorCombinedBonusTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static MatchResultSO CreateResult(bool playerWon = true,
                                                   float duration = 30f,
                                                   float damageDone = 0f,
                                                   float damageTaken = 0f,
                                                   int bonusEarned = 0)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            r.Write(playerWon, duration, 0, 0, damageDone, damageTaken, bonusEarned);
            return r;
        }

        private static CombinedBonusCalculatorSO CreateCombined(float scoreMultValue)
        {
            var calc = ScriptableObject.CreateInstance<CombinedBonusCalculatorSO>();
            var mult = ScriptableObject.CreateInstance<ScoreMultiplierSO>();
            mult.SetMultiplier(scoreMultValue);

            FieldInfo fi = typeof(CombinedBonusCalculatorSO)
                .GetField("_scoreMultiplier",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            fi?.SetValue(calc, mult);
            return calc;
        }

        private static ScoreMultiplierSO CreateMult(float value)
        {
            var m = ScriptableObject.CreateInstance<ScoreMultiplierSO>();
            m.SetMultiplier(value);
            return m;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Calc_NullCombinedBonus_ScoreUnchanged()
        {
            // Baseline: no multiplier, no combinedBonus → score unchanged.
            var result = CreateResult(playerWon: true, duration: 30f);
            int without = MatchScoreCalculator.Calculate(result);
            int with    = MatchScoreCalculator.Calculate(result, 0, null, null);

            Assert.AreEqual(without, with,
                "null combinedBonus must not alter the score.");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void Calc_CombinedBonus_AppliesToClampedScore()
        {
            // Win base=1000, duration=30 → timeBonus=450 → clamped=1450; ×2 → 2900.
            var result   = CreateResult(playerWon: true, duration: 30f);
            var combined = CreateCombined(2f);

            int score = MatchScoreCalculator.Calculate(result, 0, null, combined);

            int expected = Mathf.RoundToInt(1450f * 2f); // 2900
            Assert.AreEqual(expected, score,
                $"combinedBonus FinalMultiplier ×2 must double the clamped score. " +
                $"Expected {expected}, got {score}.");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void Calc_CombinedBonus_StacksWithScoreMultiplier()
        {
            // Win base=1000, duration=30 → clamped=1450; ×2 (3rd) → 2900; ×1.5 (4th) → 4350.
            var result   = CreateResult(playerWon: true, duration: 30f);
            var mult     = CreateMult(2f);
            var combined = CreateCombined(1.5f);

            int score = MatchScoreCalculator.Calculate(result, 0, mult, combined);

            int expected = Mathf.RoundToInt(Mathf.RoundToInt(1450f * 2f) * 1.5f); // 4350
            Assert.AreEqual(expected, score,
                $"3rd and 4th multiplier params must stack multiplicatively. " +
                $"Expected {expected}, got {score}.");

            Object.DestroyImmediate(result);
            Object.DestroyImmediate(mult);
        }

        [Test]
        public void Calc_CombinedBonus_NullResult_ReturnsZero()
        {
            var combined = CreateCombined(2f);
            int score = MatchScoreCalculator.Calculate(null, 0, null, combined);
            Assert.AreEqual(0, score,
                "null MatchResultSO must return 0 even with a combinedBonus supplied.");
        }

        [Test]
        public void Calc_CombinedBonus_MultiplierOneX_ScoreUnchanged()
        {
            var result   = CreateResult(playerWon: true, duration: 30f);
            var combined = CreateCombined(1f); // FinalMultiplier = 1×

            int withoutBonus = MatchScoreCalculator.Calculate(result);
            int withBonus    = MatchScoreCalculator.Calculate(result, 0, null, combined);

            Assert.AreEqual(withoutBonus, withBonus,
                "combinedBonus FinalMultiplier of exactly 1× must leave the score unchanged.");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void Calc_CombinedBonus_MultiplierTwo_DoublesScore()
        {
            // Simple loss: base=100, no other factors → clamped=100; ×2 → 200.
            var result   = CreateResult(playerWon: false, duration: 120f);
            var combined = CreateCombined(2f);

            int score = MatchScoreCalculator.Calculate(result, 0, null, combined);
            Assert.AreEqual(200, score,
                "combinedBonus ×2 on a pure-base loss score (100) must yield 200.");

            Object.DestroyImmediate(result);
        }
    }
}
