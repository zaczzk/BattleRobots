using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the <see cref="ScoreMultiplierSO"/> overload of
    /// <see cref="MatchScoreCalculator.Calculate"/> added in T188.
    ///
    /// Covers:
    ///   • Null multiplier — score is unchanged (backwards-compatible).
    ///   • Multiplier = 1 — score is unchanged.
    ///   • Multiplier = 2 — score is doubled.
    ///   • Multiplier = 0.5 — score is halved (rounded).
    ///   • Null result with non-null multiplier — returns 0.
    ///   • Multiplier applied after non-negative clamp, not before.
    ///   • Min multiplier (0.01) produces a very small but non-negative result.
    ///   • Max multiplier (10) amplifies a large score correctly.
    ///   • Combo and multiplier combine correctly.
    ///   • Win (all factors) with multiplier applied to final clamped value.
    ///   • Loss with multiplier applied.
    ///   • Multiplier rounds to integer via RoundToInt.
    /// </summary>
    public class MatchScoreCalculatorMultiplierTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

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

        private static ScoreMultiplierSO MakeMultiplier(float value)
        {
            var so = ScriptableObject.CreateInstance<ScoreMultiplierSO>();
            so.SetMultiplier(value);
            return so;
        }

        // ── Null / passthrough cases ──────────────────────────────────────────

        [Test]
        public void NullMultiplier_ScoreIsUnchanged()
        {
            // Loss base = 100; no multiplier → 100 (backwards-compatible).
            var r = MakeResult(playerWon: false, durationSecs: 0f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0, scoreMultiplier: null);
            Assert.AreEqual(100, score);
            Object.DestroyImmediate(r);
        }

        [Test]
        public void Multiplier_One_ScoreIsUnchanged()
        {
            // Loss base = 100; multiplier × 1 → 100.
            var r  = MakeResult(playerWon: false, durationSecs: 0f);
            var sm = MakeMultiplier(1f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0, scoreMultiplier: sm);
            Assert.AreEqual(100, score);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }

        // ── Multiplication ────────────────────────────────────────────────────

        [Test]
        public void Multiplier_Two_DoublesScore()
        {
            // Loss base = 100; multiplier × 2 → RoundToInt(200) = 200.
            var r  = MakeResult(playerWon: false, durationSecs: 0f);
            var sm = MakeMultiplier(2f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0, scoreMultiplier: sm);
            Assert.AreEqual(200, score);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }

        [Test]
        public void Multiplier_Half_HalvesScoreRounded()
        {
            // Loss base = 100; multiplier × 0.5 → RoundToInt(50.0) = 50.
            var r  = MakeResult(playerWon: false, durationSecs: 0f);
            var sm = MakeMultiplier(0.5f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0, scoreMultiplier: sm);
            Assert.AreEqual(50, score);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }

        // ── Null result ───────────────────────────────────────────────────────

        [Test]
        public void NullResult_WithMultiplier_ReturnsZero()
        {
            // Null result guard must fire before multiplier is applied.
            var sm    = MakeMultiplier(2f);
            int score = MatchScoreCalculator.Calculate(null, maxCombo: 0, scoreMultiplier: sm);
            Assert.AreEqual(0, score);
            Object.DestroyImmediate(sm);
        }

        // ── Multiplier applied after clamp ────────────────────────────────────

        [Test]
        public void Multiplier_AppliedAfterNonNegativeClamp()
        {
            // Heavy damage taken → raw score negative → clamped to 0 before multiply.
            // Loss base = 100; damageTaken=500 → raw=100−500=−400 → clamp to 0 → 0×2=0.
            var r  = MakeResult(playerWon: false, durationSecs: 0f, damageTaken: 500f);
            var sm = MakeMultiplier(2f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0, scoreMultiplier: sm);
            Assert.AreEqual(0, score,
                "Multiplier must be applied after the non-negative clamp, so 0×2 = 0.");
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }

        // ── Boundary multipliers ──────────────────────────────────────────────

        [Test]
        public void Multiplier_Min_0_01_ProducesSmallPositiveScore()
        {
            // Win at 0s → 1600; × 0.01 → RoundToInt(16.0) = 16.
            var r  = MakeResult(playerWon: true, durationSecs: 0f);
            var sm = MakeMultiplier(0.01f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0, scoreMultiplier: sm);
            Assert.AreEqual(16, score);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }

        [Test]
        public void Multiplier_Max_10_LargelyAmplifiesScore()
        {
            // Win at 0s → 1600; × 10 → 16000.
            var r  = MakeResult(playerWon: true, durationSecs: 0f);
            var sm = MakeMultiplier(10f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0, scoreMultiplier: sm);
            Assert.AreEqual(16000, score);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }

        // ── Combined with other parameters ────────────────────────────────────

        [Test]
        public void Combo_And_Multiplier_CombinedCorrectly()
        {
            // Loss base = 100; maxCombo=10 → +50 → clamped=150; × 2 → 300.
            var r  = MakeResult(playerWon: false, durationSecs: 0f);
            var sm = MakeMultiplier(2f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 10, scoreMultiplier: sm);
            Assert.AreEqual(300, score);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }

        [Test]
        public void Win_AllFactors_Multiplier_Applied()
        {
            // Win base=1000; duration=30s → timeBonus=450; damageDone=100→+200;
            // damageTaken=20→−20; bonusEarned=10→+30; maxCombo=5→+25
            // subtotal = 1000+450+200−20+30+25 = 1685; × 2 → 3370
            var r  = MakeResult(
                playerWon: true, durationSecs: 30f,
                damageDone: 100f, damageTaken: 20f, bonusEarned: 10);
            var sm = MakeMultiplier(2f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 5, scoreMultiplier: sm);
            Assert.AreEqual(3370, score);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }

        [Test]
        public void Loss_Multiplier_Applied_Correctly()
        {
            // Loss base=100; damageDone=25→+50; bonusEarned=5→+15 → clamped=165; × 3 → 495
            var r  = MakeResult(playerWon: false, durationSecs: 0f,
                                damageDone: 25f, bonusEarned: 5);
            var sm = MakeMultiplier(3f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0, scoreMultiplier: sm);
            Assert.AreEqual(495, score);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }

        [Test]
        public void Multiplier_RoundsToNearestInt()
        {
            // Loss base=100; × 1.5 → RoundToInt(150.0) = 150.
            var r  = MakeResult(playerWon: false, durationSecs: 0f);
            var sm = MakeMultiplier(1.5f);
            int score = MatchScoreCalculator.Calculate(r, maxCombo: 0, scoreMultiplier: sm);
            Assert.AreEqual(150, score,
                "Mathf.RoundToInt(100 × 1.5) must equal 150.");
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(sm);
        }
    }
}
