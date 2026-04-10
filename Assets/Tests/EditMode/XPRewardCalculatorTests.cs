using NUnit.Framework;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="XPRewardCalculator"/>.
    ///
    /// Covers:
    ///   • Base XP for win (100) and loss (25).
    ///   • Duration bonus: +1 XP per 5 seconds (floor, not ceil).
    ///   • Streak bonus: +10 XP per streak level, capped at streak 5 (+50).
    ///   • Negative duration guarded to zero.
    ///   • Negative streak guarded to zero.
    ///   • Combined bonus calculation.
    ///   • Constant values match formula documentation.
    /// </summary>
    public class XPRewardCalculatorTests
    {
        // ── Constants ─────────────────────────────────────────────────────────

        [Test]
        public void BaseWinXP_Constant_Is100()
        {
            Assert.AreEqual(100, XPRewardCalculator.BaseWinXP);
        }

        [Test]
        public void BaseLossXP_Constant_Is25()
        {
            Assert.AreEqual(25, XPRewardCalculator.BaseLossXP);
        }

        [Test]
        public void MaxStreakBonus_Constant_Is5()
        {
            Assert.AreEqual(5, XPRewardCalculator.MaxStreakBonus);
        }

        // ── Base XP (no duration, no streak) ─────────────────────────────────

        [Test]
        public void Win_ZeroDuration_ZeroStreak_Returns100()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 0f, winStreak: 0);
            Assert.AreEqual(100, xp);
        }

        [Test]
        public void Loss_ZeroDuration_ZeroStreak_Returns25()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: false, durationSeconds: 0f, winStreak: 0);
            Assert.AreEqual(25, xp);
        }

        // ── Duration bonus ────────────────────────────────────────────────────

        [Test]
        public void DurationBonus_Exactly5Seconds_AddsOne()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 5f, winStreak: 0);
            Assert.AreEqual(101, xp, "5 seconds exactly should add 1 duration XP.");
        }

        [Test]
        public void DurationBonus_10Seconds_AddsTwo()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 10f, winStreak: 0);
            Assert.AreEqual(102, xp);
        }

        [Test]
        public void DurationBonus_UsesFloor_Not_Ceil()
        {
            // 9.9 seconds → floor(9.9 / 5) = 1, not 2.
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 9.9f, winStreak: 0);
            Assert.AreEqual(101, xp, "Duration bonus must use floor division.");
        }

        [Test]
        public void DurationBonus_NegativeDuration_TreatedAsZero()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: -30f, winStreak: 0);
            Assert.AreEqual(100, xp, "Negative duration must not subtract XP.");
        }

        [Test]
        public void DurationBonus_60Seconds_Adds12()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 60f, winStreak: 0);
            Assert.AreEqual(112, xp, "60 / 5 = 12 bonus XP.");
        }

        // ── Streak bonus ──────────────────────────────────────────────────────

        [Test]
        public void StreakBonus_ZeroStreak_NoBonus()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 0f, winStreak: 0);
            Assert.AreEqual(100, xp);
        }

        [Test]
        public void StreakBonus_OneStreak_Adds10()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 0f, winStreak: 1);
            Assert.AreEqual(110, xp);
        }

        [Test]
        public void StreakBonus_FiveStreak_Adds50()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 0f, winStreak: 5);
            Assert.AreEqual(150, xp);
        }

        [Test]
        public void StreakBonus_BeyondMaxStreak_CappedAtFive()
        {
            int xpAt5  = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 0f, winStreak: 5);
            int xpAt10 = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 0f, winStreak: 10);
            Assert.AreEqual(xpAt5, xpAt10, "Streak bonus must be capped at MaxStreakBonus.");
        }

        [Test]
        public void StreakBonus_NegativeStreak_TreatedAsZero()
        {
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 0f, winStreak: -3);
            Assert.AreEqual(100, xp, "Negative streak must not reduce XP.");
        }

        // ── Combined calculation ──────────────────────────────────────────────

        [Test]
        public void Win_60Seconds_Streak3_CombinesAllBonuses()
        {
            // base=100, duration=12 (60/5), streak=30 (3×10) → 142
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: true, durationSeconds: 60f, winStreak: 3);
            Assert.AreEqual(142, xp, "100 base + 12 duration + 30 streak = 142.");
        }

        [Test]
        public void Loss_WithStreak_GetsStreakBonusXP()
        {
            // After a loss MatchManager resets streak to 0 before calling this,
            // but the calculator itself accepts any winStreak for generality.
            int xp = XPRewardCalculator.CalculateMatchXP(playerWon: false, durationSeconds: 0f, winStreak: 3);
            Assert.AreEqual(55, xp, "25 base + 30 streak = 55 (caller controls what streak value to pass).");
        }
    }
}
