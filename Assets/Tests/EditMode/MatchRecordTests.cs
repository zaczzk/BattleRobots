using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchRecord"/> and <see cref="SaveData"/>
    /// plain-data classes.
    ///
    /// Verifies:
    ///   - All fields survive a JsonUtility round-trip (same path used by SaveSystem).
    ///   - Default values are sane.
    ///   - Nested list (equippedPartIds) round-trips correctly.
    /// </summary>
    [TestFixture]
    public sealed class MatchRecordTests
    {
        // ── MatchRecord defaults ──────────────────────────────────────────────

        [Test]
        public void MatchRecord_DefaultEquippedPartIds_IsEmptyList()
        {
            var record = new MatchRecord();
            Assert.IsNotNull(record.equippedPartIds,
                "equippedPartIds must be initialized (not null) by default.");
            Assert.AreEqual(0, record.equippedPartIds.Count);
        }

        // ── SaveData defaults ─────────────────────────────────────────────────

        [Test]
        public void SaveData_DefaultMatchHistory_IsEmptyList()
        {
            var data = new SaveData();
            Assert.IsNotNull(data.matchHistory);
            Assert.AreEqual(0, data.matchHistory.Count);
        }

        // ── JSON round-trip: MatchRecord ─────────────────────────────────────

        [Test]
        public void MatchRecord_JsonRoundTrip_PreservesAllFields()
        {
            var record = new MatchRecord
            {
                timestamp       = "2026-04-05T09:30:00Z",
                arenaIndex      = 3,
                playerWon       = false,
                durationSeconds = 120.75f,
                damageDone      = 55.5f,
                damageTaken     = 88.25f,
                currencyEarned  = 0,
                walletSnapshot  = 450,
                equippedPartIds = new List<string> { "slot_body_main", "slot_leftarm_0", "slot_weapon_0" },
            };

            string json     = JsonUtility.ToJson(record);
            var    restored = JsonUtility.FromJson<MatchRecord>(json);

            Assert.AreEqual(record.timestamp,       restored.timestamp);
            Assert.AreEqual(record.arenaIndex,      restored.arenaIndex);
            Assert.AreEqual(record.playerWon,       restored.playerWon);
            Assert.AreEqual(record.durationSeconds, restored.durationSeconds, 0.001f);
            Assert.AreEqual(record.damageDone,      restored.damageDone,      0.001f);
            Assert.AreEqual(record.damageTaken,     restored.damageTaken,     0.001f);
            Assert.AreEqual(record.currencyEarned,  restored.currencyEarned);
            Assert.AreEqual(record.walletSnapshot,  restored.walletSnapshot);
            Assert.AreEqual(3, restored.equippedPartIds.Count);
            Assert.AreEqual("slot_body_main",  restored.equippedPartIds[0]);
            Assert.AreEqual("slot_leftarm_0",  restored.equippedPartIds[1]);
            Assert.AreEqual("slot_weapon_0",   restored.equippedPartIds[2]);
        }

        // ── JSON round-trip: SaveData ─────────────────────────────────────────

        [Test]
        public void SaveData_JsonRoundTrip_PreservesWalletAndHistory()
        {
            var data = new SaveData { walletBalance = 750 };
            data.matchHistory.Add(new MatchRecord { arenaIndex = 1, playerWon = true,  currencyEarned = 300 });
            data.matchHistory.Add(new MatchRecord { arenaIndex = 2, playerWon = false, currencyEarned = 0   });

            string   json     = JsonUtility.ToJson(data);
            SaveData restored = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(750, restored.walletBalance);
            Assert.AreEqual(2,   restored.matchHistory.Count);
            Assert.AreEqual(1,   restored.matchHistory[0].arenaIndex);
            Assert.IsTrue(       restored.matchHistory[0].playerWon);
            Assert.AreEqual(300, restored.matchHistory[0].currencyEarned);
            Assert.AreEqual(2,   restored.matchHistory[1].arenaIndex);
            Assert.IsFalse(      restored.matchHistory[1].playerWon);
        }

        // ── Empty equippedPartIds round-trips ─────────────────────────────────

        [Test]
        public void MatchRecord_EmptyEquippedPartIds_RoundTrips()
        {
            var record = new MatchRecord { equippedPartIds = new List<string>() };

            string     json     = JsonUtility.ToJson(record);
            MatchRecord restored = JsonUtility.FromJson<MatchRecord>(json);

            Assert.IsNotNull(restored.equippedPartIds);
            Assert.AreEqual(0, restored.equippedPartIds.Count);
        }
    }
}
