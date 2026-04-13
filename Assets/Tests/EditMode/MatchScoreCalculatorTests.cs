using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchScoreCalculator"/>.
    ///
    /// Covers:
    ///   • Null result → 0.
    ///   • Loss base (100) with zero damage contribution.
    ///   • Win base (1 000) with zero damage contribution.
    ///   • Time bonus on wins: 0 s → +600, 60 s → +300, 120 s → 0, over 120 s → 0.
    ///   • No time bonus on a loss.
    ///   • Damage contribution: DamageDone ×2 adds, DamageTaken ×1 subtracts.
    ///   • BonusEarned multiplied by 3.
    ///   • Final score clamped to 0 when combination would go negative.
    ///   • All factors combined.
    ///   • Win with zero damage and zero bonus — only base + time bonus.
    /// </summary>
    public class MatchScoreCalculatorTests
    {
        // ── Helper ────────────────────────────────────────────────────────────

        /// <summary>Creates a MatchResultSO and writes data to it.</summary>
        private static MatchResultSO MakeResult(
            bool  playerWon      = false,
            float durationSecs   = 0f,
            float damageDone     = 0f,
            float damageTaken    = 0f,
            int   bonusEarned    = 0,
            int   currencyEarned = 0,
            int   walletBalance  = 0)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            r.Write(playerWon, durationSecs, currencyEarned, walletBalance,
                    damageDone, damageTaken, bonusEarned);
            return r;
        }

        // ── Null / edge cases ─────────────────────────────────────────────────

        [Test]
        public void NullResult_Returns_Zero()
        {
            Assert.AreEqual(0, MatchScoreCalculator.Calculate(null));
        }

        // ── Base scores ───────────────────────────────────────────────────────

        [Test]
        public void Loss_ZeroDamage_ZeroBonus_BaseIs_100()
        {
            var r = MakeResult(playerWon: false, durationSecs: 0f);
            int score = MatchScoreCalculator.Calculate(r);
            // Loss base = 100; no time bonus (loss); no damage; no bonus → 100
            Assert.AreEqual(100, score);
            Object.DestroyImmediate(r);
        }

        [Test]
        public void Win_ZeroDamage_ZeroBonus_ZeroDuration_BaseIs_1600()
        {
            // Win base (1000) + time bonus at 0 s (600) = 1600
            var r = MakeResult(playerWon: true, durationSecs: 0f);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(1600, score);
            Object.DestroyImmediate(r);
        }

        // ── Time bonus (wins only) ────────────────────────────────────────────

        [Test]
        public void Win_At60s_TimeBonus_Is_300()
        {
            // 600 − 60×5 = 300
            var r = MakeResult(playerWon: true, durationSecs: 60f);
            int score = MatchScoreCalculator.Calculate(r);
            // 1000 + 300 = 1300
            Assert.AreEqual(1300, score);
            Object.DestroyImmediate(r);
        }

        [Test]
        public void Win_At120s_TimeBonus_Is_Zero()
        {
            // 600 − 120×5 = 0
            var r = MakeResult(playerWon: true, durationSecs: 120f);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(1000, score);
            Object.DestroyImmediate(r);
        }

        [Test]
        public void Win_Over120s_TimeBonus_ClampedToZero()
        {
            // 600 − 200×5 = −400 → clamped to 0
            var r = MakeResult(playerWon: true, durationSecs: 200f);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(1000, score);
            Object.DestroyImmediate(r);
        }

        [Test]
        public void Loss_NoTimeBonus_Regardless_Of_Duration()
        {
            // Loss has no time bonus — score should still be 100 even at 0 s.
            var r = MakeResult(playerWon: false, durationSecs: 0f);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(100, score);
            Object.DestroyImmediate(r);
        }

        // ── Damage contribution ───────────────────────────────────────────────

        [Test]
        public void DamageDone_AddsFloorOf_TimesTwo()
        {
            // Loss base = 100; damageDone=50 → +floor(100) = +100 → total 200
            var r = MakeResult(playerWon: false, durationSecs: 0f, damageDone: 50f);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(200, score);
            Object.DestroyImmediate(r);
        }

        [Test]
        public void DamageTaken_SubtractsFloorOf_TimesOne()
        {
            // Loss base = 100; damageTaken=30 → −floor(30) = −30 → total 70
            var r = MakeResult(playerWon: false, durationSecs: 0f, damageTaken: 30f);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(70, score);
            Object.DestroyImmediate(r);
        }

        [Test]
        public void DamageDone_FractionalValue_TruncatedByFloor()
        {
            // floor(7.9 × 2) = floor(15.8) = 15
            var r = MakeResult(playerWon: false, durationSecs: 0f, damageDone: 7.9f);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(115, score); // 100 + 15
            Object.DestroyImmediate(r);
        }

        // ── BonusEarned multiplier ────────────────────────────────────────────

        [Test]
        public void BonusEarned_MultipliedByThree()
        {
            // Loss base = 100; bonusEarned=50 → +150 → total 250
            var r = MakeResult(playerWon: false, durationSecs: 0f, bonusEarned: 50);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(250, score);
            Object.DestroyImmediate(r);
        }

        // ── Clamp to zero ─────────────────────────────────────────────────────

        [Test]
        public void HeavyDamageTaken_Score_ClampedToZero()
        {
            // Loss base = 100; damageTaken=500 → −500 → raw=−400 → clamped to 0
            var r = MakeResult(playerWon: false, durationSecs: 0f, damageTaken: 500f);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(0, score);
            Object.DestroyImmediate(r);
        }

        // ── All-factors combined ──────────────────────────────────────────────

        [Test]
        public void AllFactors_Win_Combined()
        {
            // Win base = 1000
            // Duration 30s → timeBonus = floor(600 − 30×5) = floor(450) = 450
            // damageDone=100 → +200
            // damageTaken=20 → −20
            // bonusEarned=10 → +30
            // Total = 1000 + 450 + 200 − 20 + 30 = 1660
            var r = MakeResult(
                playerWon: true,
                durationSecs: 30f,
                damageDone: 100f,
                damageTaken: 20f,
                bonusEarned: 10);
            int score = MatchScoreCalculator.Calculate(r);
            Assert.AreEqual(1660, score);
            Object.DestroyImmediate(r);
        }

        // ── maxCombo bonus ────────────────────────────────────────────────────

        [Test]
        public void MaxCombo_Zero_AddsNoBonus()
        {
            // Loss base = 100; maxCombo=0 → no bonus → 100
            var r = MakeResult(playerWon: false, durationSecs: 0f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0);
            Assert.AreEqual(100, score);
            Object.DestroyImmediate(r);
        }

        [Test]
        public void MaxCombo_Ten_AddsFiftyPoints()
        {
            // Loss base = 100; maxCombo=10 → +50 → 150
            var r = MakeResult(playerWon: false, durationSecs: 0f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 10);
            Assert.AreEqual(150, score);
            Object.DestroyImmediate(r);
        }

        [Test]
        public void MaxCombo_Negative_ClampedToZero_AddsNoBonus()
        {
            // Negative maxCombo must be clamped — should not subtract from score.
            // Loss base = 100; maxCombo=-5 → clamped to 0 → 100
            var r = MakeResult(playerWon: false, durationSecs: 0f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: -5);
            Assert.AreEqual(100, score);
            Object.DestroyImmediate(r);
        }
    }
}
