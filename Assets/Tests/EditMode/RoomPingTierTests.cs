using System.Collections.Generic;
using NUnit.Framework;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T057 — RoomListUI ping-tier section headers.
    ///
    /// Coverage (14 cases):
    ///
    /// RoomEntryUI.GetPingTier — tier classification
    ///   [01] GetPingTier_ZeroMs_ReturnsUnknown
    ///   [02] GetPingTier_NegativeMs_ReturnsUnknown
    ///   [03] GetPingTier_80ms_ReturnsExcellent
    ///   [04] GetPingTier_81ms_ReturnsGood
    ///   [05] GetPingTier_150ms_ReturnsGood
    ///   [06] GetPingTier_151ms_ReturnsHigh
    ///
    /// RoomEntryUI.GetTierLabel — section header strings
    ///   [07] GetTierLabel_Excellent
    ///   [08] GetTierLabel_Good
    ///   [09] GetTierLabel_High
    ///   [10] GetTierLabel_Unknown
    ///
    /// PingTier enum — sort order contract
    ///   [11] PingTier_Excellent_IsLessThanGood
    ///   [12] PingTier_Good_IsLessThanHigh
    ///   [13] PingTier_High_IsLessThanUnknown
    ///
    /// RoomListUI.SortByTier — grouping helper (internal, tested via reflection)
    ///   [14] SortByTier_MixedPings_OrdersByTierThenStable
    /// </summary>
    [TestFixture]
    public sealed class RoomPingTierTests
    {
        // ── [01] 0 ms → Unknown ───────────────────────────────────────────────

        [Test]
        public void GetPingTier_ZeroMs_ReturnsUnknown()
        {
            Assert.AreEqual(PingTier.Unknown, RoomEntryUI.GetPingTier(0));
        }

        // ── [02] Negative → Unknown ───────────────────────────────────────────

        [Test]
        public void GetPingTier_NegativeMs_ReturnsUnknown()
        {
            Assert.AreEqual(PingTier.Unknown, RoomEntryUI.GetPingTier(-1));
        }

        // ── [03] 80 ms → Excellent ────────────────────────────────────────────

        [Test]
        public void GetPingTier_80ms_ReturnsExcellent()
        {
            Assert.AreEqual(PingTier.Excellent, RoomEntryUI.GetPingTier(80));
        }

        // ── [04] 81 ms → Good ─────────────────────────────────────────────────

        [Test]
        public void GetPingTier_81ms_ReturnsGood()
        {
            Assert.AreEqual(PingTier.Good, RoomEntryUI.GetPingTier(81));
        }

        // ── [05] 150 ms → Good ────────────────────────────────────────────────

        [Test]
        public void GetPingTier_150ms_ReturnsGood()
        {
            Assert.AreEqual(PingTier.Good, RoomEntryUI.GetPingTier(150));
        }

        // ── [06] 151 ms → High ────────────────────────────────────────────────

        [Test]
        public void GetPingTier_151ms_ReturnsHigh()
        {
            Assert.AreEqual(PingTier.High, RoomEntryUI.GetPingTier(151));
        }

        // ── [07] Label — Excellent ────────────────────────────────────────────

        [Test]
        public void GetTierLabel_Excellent_ContainsExcellentAndThreshold()
        {
            string label = RoomEntryUI.GetTierLabel(PingTier.Excellent);
            StringAssert.Contains("Excellent", label);
            StringAssert.Contains("80", label);
        }

        // ── [08] Label — Good ─────────────────────────────────────────────────

        [Test]
        public void GetTierLabel_Good_ContainsGoodAndThreshold()
        {
            string label = RoomEntryUI.GetTierLabel(PingTier.Good);
            StringAssert.Contains("Good", label);
            StringAssert.Contains("150", label);
        }

        // ── [09] Label — High ─────────────────────────────────────────────────

        [Test]
        public void GetTierLabel_High_ContainsHigh()
        {
            string label = RoomEntryUI.GetTierLabel(PingTier.High);
            StringAssert.Contains("High", label);
        }

        // ── [10] Label — Unknown ──────────────────────────────────────────────

        [Test]
        public void GetTierLabel_Unknown_ReturnsUnknown()
        {
            string label = RoomEntryUI.GetTierLabel(PingTier.Unknown);
            StringAssert.Contains("Unknown", label);
        }

        // ── [11] Sort order: Excellent < Good ────────────────────────────────

        [Test]
        public void PingTier_Excellent_SortsBefore_Good()
        {
            Assert.Less((int)PingTier.Excellent, (int)PingTier.Good,
                "Excellent tier must sort before Good tier.");
        }

        // ── [12] Sort order: Good < High ─────────────────────────────────────

        [Test]
        public void PingTier_Good_SortsBefore_High()
        {
            Assert.Less((int)PingTier.Good, (int)PingTier.High,
                "Good tier must sort before High tier.");
        }

        // ── [13] Sort order: High < Unknown ──────────────────────────────────

        [Test]
        public void PingTier_High_SortsBefore_Unknown()
        {
            Assert.Less((int)PingTier.High, (int)PingTier.Unknown,
                "High tier must sort before Unknown tier.");
        }

        // ── [14] SortByTier — mixed pings, stable within tier ─────────────────

        [Test]
        public void SortByTier_MixedPings_OrdersByTierThenStable()
        {
            // Build a list in "mixed" network order.
            // Tier annotations:  Unknown, High, Excellent, Good, Excellent (in-order index)
            var rooms = new List<RoomEntry>
            {
                new RoomEntry("AA11", 1, 2, false, pingMs: 0),    // index 0 → Unknown
                new RoomEntry("BB22", 1, 2, false, pingMs: 200),  // index 1 → High
                new RoomEntry("CC33", 1, 2, false, pingMs: 50),   // index 2 → Excellent
                new RoomEntry("DD44", 1, 2, false, pingMs: 120),  // index 3 → Good
                new RoomEntry("EE55", 1, 2, false, pingMs: 30),   // index 4 → Excellent
            };

            List<RoomEntry> sorted = RoomListUI.SortByTier(rooms);

            // Expected order: CC33 (Excellent, idx 2), EE55 (Excellent, idx 4),
            //                 DD44 (Good, idx 3), BB22 (High, idx 1), AA11 (Unknown, idx 0)
            Assert.AreEqual(5, sorted.Count, "All 5 rooms must be present.");
            Assert.AreEqual("CC33", sorted[0].roomCode, "First: Excellent (earliest index).");
            Assert.AreEqual("EE55", sorted[1].roomCode, "Second: Excellent (later index).");
            Assert.AreEqual("DD44", sorted[2].roomCode, "Third: Good.");
            Assert.AreEqual("BB22", sorted[3].roomCode, "Fourth: High.");
            Assert.AreEqual("AA11", sorted[4].roomCode, "Fifth: Unknown.");
        }
    }
}
