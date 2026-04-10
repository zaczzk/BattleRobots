using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="StreakBonusCalculator"/>.
    ///
    /// Covers:
    ///   • <see cref="StreakBonusCalculator.GetBonusMultiplier"/>: zero streak → 1.0;
    ///     each level adds 0.1; streak ≥ 5 is capped at 1.5; negative streak → 1.0.
    ///   • <see cref="StreakBonusCalculator.ApplyToReward"/>: zero-streak returns base
    ///     reward unchanged; streak-3 returns correct boosted value; negative base
    ///     reward returns 0.
    /// </summary>
    public class StreakBonusCalculatorTests
    {
        // ── GetBonusMultiplier ────────────────────────────────────────────────

        [Test]
        public void GetBonusMultiplier_StreakZero_ReturnsOne()
        {
            float result = StreakBonusCalculator.GetBonusMultiplier(0);
            Assert.AreEqual(1f, result, 1e-5f);
        }

        [Test]
        public void GetBonusMultiplier_StreakOne_Returns1Point1()
        {
            float result = StreakBonusCalculator.GetBonusMultiplier(1);
            Assert.AreEqual(1.1f, result, 1e-5f);
        }

        [Test]
        public void GetBonusMultiplier_StreakFive_Returns1Point5()
        {
            float result = StreakBonusCalculator.GetBonusMultiplier(5);
            Assert.AreEqual(1.5f, result, 1e-5f);
        }

        [Test]
        public void GetBonusMultiplier_StreakAboveFive_CappedAt1Point5()
        {
            // Streak 10 must give the same result as streak 5.
            float atFive  = StreakBonusCalculator.GetBonusMultiplier(5);
            float atTen   = StreakBonusCalculator.GetBonusMultiplier(10);
            Assert.AreEqual(atFive, atTen, 1e-5f, "Bonus must be capped at MaxBonusStreak.");
        }

        [Test]
        public void GetBonusMultiplier_NegativeStreak_ReturnsOne()
        {
            float result = StreakBonusCalculator.GetBonusMultiplier(-3);
            Assert.AreEqual(1f, result, 1e-5f, "Negative streak must behave like streak 0.");
        }

        // ── ApplyToReward ─────────────────────────────────────────────────────

        [Test]
        public void ApplyToReward_ZeroStreak_ReturnsSameReward()
        {
            int result = StreakBonusCalculator.ApplyToReward(baseReward: 200, streak: 0);
            Assert.AreEqual(200, result);
        }

        [Test]
        public void ApplyToReward_Streak3_ReturnsCorrectValue()
        {
            // 200 × 1.3 = 260, rounded to nearest int.
            int result = StreakBonusCalculator.ApplyToReward(baseReward: 200, streak: 3);
            Assert.AreEqual(Mathf.RoundToInt(200 * 1.3f), result);
        }

        [Test]
        public void ApplyToReward_NegativeBaseReward_ReturnsZero()
        {
            int result = StreakBonusCalculator.ApplyToReward(baseReward: -100, streak: 2);
            Assert.AreEqual(0, result, "Negative base reward must return 0.");
        }
    }
}
