using System.Collections.Generic;
using NUnit.Framework;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LeaderboardStats.Compute"/>.
    ///
    /// Covers:
    ///   - Null / empty history returns zeroed struct.
    ///   - Single-match win and loss records compute correct stats.
    ///   - Mixed history: wins, losses, win-rate, averages, total earnings.
    ///   - All-wins and all-losses edge cases.
    ///   - Win-rate boundary (50 %, 100 %, 0 %).
    ///   - Average damage and duration computed correctly.
    /// </summary>
    [TestFixture]
    public sealed class LeaderboardStatsTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static MatchRecord MakeRecord(
            bool won, float damageDone = 0f, float damageTaken = 0f,
            int currencyEarned = 0, float durationSeconds = 60f)
        {
            return new MatchRecord
            {
                playerWon       = won,
                damageDone      = damageDone,
                damageTaken     = damageTaken,
                currencyEarned  = currencyEarned,
                durationSeconds = durationSeconds,
            };
        }

        // ── Null / empty ──────────────────────────────────────────────────────

        [Test]
        public void Compute_NullHistory_ReturnsZeroed()
        {
            LeaderboardStats stats = LeaderboardStats.Compute(null);

            Assert.AreEqual(0,    stats.MatchCount);
            Assert.AreEqual(0,    stats.Wins);
            Assert.AreEqual(0,    stats.Losses);
            Assert.AreEqual(0f,   stats.WinRatePercent,   0.001f);
            Assert.AreEqual(0f,   stats.AvgDamageDealt,   0.001f);
            Assert.AreEqual(0f,   stats.AvgDamageTaken,   0.001f);
            Assert.AreEqual(0,    stats.TotalEarnings);
            Assert.AreEqual(0f,   stats.AvgDurationSeconds, 0.001f);
        }

        [Test]
        public void Compute_EmptyHistory_ReturnsZeroed()
        {
            LeaderboardStats stats = LeaderboardStats.Compute(new List<MatchRecord>());

            Assert.AreEqual(0,  stats.MatchCount);
            Assert.AreEqual(0f, stats.WinRatePercent, 0.001f);
            Assert.AreEqual(0,  stats.TotalEarnings);
        }

        // ── Single win ────────────────────────────────────────────────────────

        [Test]
        public void Compute_SingleWin_CorrectCounts()
        {
            var history = new List<MatchRecord> { MakeRecord(won: true, currencyEarned: 200) };
            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(1,     stats.MatchCount);
            Assert.AreEqual(1,     stats.Wins);
            Assert.AreEqual(0,     stats.Losses);
            Assert.AreEqual(100f,  stats.WinRatePercent, 0.001f);
            Assert.AreEqual(200,   stats.TotalEarnings);
        }

        // ── Single loss ───────────────────────────────────────────────────────

        [Test]
        public void Compute_SingleLoss_CorrectCounts()
        {
            var history = new List<MatchRecord> { MakeRecord(won: false, currencyEarned: 50) };
            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(1,    stats.MatchCount);
            Assert.AreEqual(0,    stats.Wins);
            Assert.AreEqual(1,    stats.Losses);
            Assert.AreEqual(0f,   stats.WinRatePercent, 0.001f);
            Assert.AreEqual(50,   stats.TotalEarnings);
        }

        // ── Mixed history: 3W / 2L ────────────────────────────────────────────

        [Test]
        public void Compute_MixedHistory_WinsAndLossesSum()
        {
            var history = new List<MatchRecord>
            {
                MakeRecord(won: true),
                MakeRecord(won: false),
                MakeRecord(won: true),
                MakeRecord(won: true),
                MakeRecord(won: false),
            };

            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(5, stats.MatchCount);
            Assert.AreEqual(3, stats.Wins);
            Assert.AreEqual(2, stats.Losses);
            Assert.AreEqual(3 + 2, stats.Wins + stats.Losses, "Wins + Losses must equal MatchCount.");
        }

        // ── Win rate ─────────────────────────────────────────────────────────

        [Test]
        public void Compute_TwoWinsOneLoss_WinRateIs66Point6Percent()
        {
            var history = new List<MatchRecord>
            {
                MakeRecord(won: true),
                MakeRecord(won: true),
                MakeRecord(won: false),
            };

            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(66.666f, stats.WinRatePercent, 0.01f);
        }

        [Test]
        public void Compute_FiftyFifty_WinRateIs50Percent()
        {
            var history = new List<MatchRecord> { MakeRecord(won: true), MakeRecord(won: false) };
            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(50f, stats.WinRatePercent, 0.001f);
        }

        // ── Average damage ────────────────────────────────────────────────────

        [Test]
        public void Compute_AverageDamageDealt_IsCorrect()
        {
            var history = new List<MatchRecord>
            {
                MakeRecord(won: true,  damageDone: 100f),
                MakeRecord(won: false, damageDone: 50f),
            };

            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(75f, stats.AvgDamageDealt, 0.001f);
        }

        [Test]
        public void Compute_AverageDamageTaken_IsCorrect()
        {
            var history = new List<MatchRecord>
            {
                MakeRecord(won: true,  damageTaken: 30f),
                MakeRecord(won: true,  damageTaken: 70f),
                MakeRecord(won: false, damageTaken: 50f),
            };

            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(50f, stats.AvgDamageTaken, 0.001f);
        }

        // ── Total earnings ────────────────────────────────────────────────────

        [Test]
        public void Compute_TotalEarnings_SumsAllRecords()
        {
            var history = new List<MatchRecord>
            {
                MakeRecord(won: true,  currencyEarned: 300),
                MakeRecord(won: false, currencyEarned: 50),
                MakeRecord(won: true,  currencyEarned: 200),
            };

            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(550, stats.TotalEarnings);
        }

        // ── Average duration ──────────────────────────────────────────────────

        [Test]
        public void Compute_AvgDuration_IsCorrect()
        {
            var history = new List<MatchRecord>
            {
                MakeRecord(won: true,  durationSeconds: 90f),
                MakeRecord(won: false, durationSeconds: 30f),
            };

            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(60f, stats.AvgDurationSeconds, 0.001f);
        }

        // ── All wins ──────────────────────────────────────────────────────────

        [Test]
        public void Compute_AllWins_LossesIsZero_WinRateIs100()
        {
            var history = new List<MatchRecord>
            {
                MakeRecord(won: true),
                MakeRecord(won: true),
                MakeRecord(won: true),
            };

            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(0,     stats.Losses);
            Assert.AreEqual(100f,  stats.WinRatePercent, 0.001f);
        }

        // ── All losses ────────────────────────────────────────────────────────

        [Test]
        public void Compute_AllLosses_WinsIsZero_WinRateIs0()
        {
            var history = new List<MatchRecord>
            {
                MakeRecord(won: false),
                MakeRecord(won: false),
            };

            LeaderboardStats stats = LeaderboardStats.Compute(history);

            Assert.AreEqual(0,   stats.Wins);
            Assert.AreEqual(0f,  stats.WinRatePercent, 0.001f);
        }
    }
}
