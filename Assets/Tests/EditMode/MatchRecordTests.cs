using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchRecord"/> and <see cref="SaveData"/>
    /// JSON serialisation via <c>JsonUtility</c>.
    ///
    /// These are pure POCO round-trip tests — no Unity runtime required beyond
    /// JsonUtility itself, which works in EditMode.
    /// </summary>
    public class MatchRecordTests
    {
        // ── MatchRecord ───────────────────────────────────────────────────────

        [Test]
        public void MatchRecord_JsonRoundTrip_PreservesAllPrimitiveFields()
        {
            var record = new MatchRecord
            {
                timestamp       = "2026-04-10T12:00:00Z",
                arenaIndex      = 2,
                playerWon       = true,
                durationSeconds = 63.7f,
                damageDone      = 350f,
                damageTaken     = 120f,
                currencyEarned  = 300,
                walletSnapshot  = 800
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
        }

        [Test]
        public void MatchRecord_JsonRoundTrip_PreservesEquippedPartIds()
        {
            var record = new MatchRecord();
            record.equippedPartIds.Add("arm_heavy");
            record.equippedPartIds.Add("wheel_fast");
            record.equippedPartIds.Add("chassis_light");

            string json     = JsonUtility.ToJson(record);
            var    restored = JsonUtility.FromJson<MatchRecord>(json);

            Assert.AreEqual(3,                 restored.equippedPartIds.Count);
            Assert.AreEqual("arm_heavy",       restored.equippedPartIds[0]);
            Assert.AreEqual("wheel_fast",      restored.equippedPartIds[1]);
            Assert.AreEqual("chassis_light",   restored.equippedPartIds[2]);
        }

        [Test]
        public void MatchRecord_JsonRoundTrip_EmptyPartIds_RoundTripsAsEmptyList()
        {
            var    record   = new MatchRecord();
            string json     = JsonUtility.ToJson(record);
            var    restored = JsonUtility.FromJson<MatchRecord>(json);

            Assert.IsNotNull(restored.equippedPartIds);
            Assert.AreEqual(0, restored.equippedPartIds.Count);
        }

        [Test]
        public void MatchRecord_PlayerLoss_PreservesPlayerWonFalse()
        {
            var record = new MatchRecord { playerWon = false };

            string json     = JsonUtility.ToJson(record);
            var    restored = JsonUtility.FromJson<MatchRecord>(json);

            Assert.IsFalse(restored.playerWon);
        }

        // ── SaveData ──────────────────────────────────────────────────────────

        [Test]
        public void SaveData_JsonRoundTrip_PreservesWalletBalance()
        {
            var data = new SaveData { walletBalance = 12345 };

            string json     = JsonUtility.ToJson(data);
            var    restored = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(12345, restored.walletBalance);
        }

        [Test]
        public void SaveData_JsonRoundTrip_PreservesMatchHistoryCount()
        {
            var data = new SaveData { walletBalance = 500 };
            data.matchHistory.Add(new MatchRecord { playerWon = true,  currencyEarned = 200 });
            data.matchHistory.Add(new MatchRecord { playerWon = false, currencyEarned = 0   });

            string json     = JsonUtility.ToJson(data);
            var    restored = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(500, restored.walletBalance);
            Assert.AreEqual(2,   restored.matchHistory.Count);
            Assert.IsTrue(restored.matchHistory[0].playerWon);
            Assert.IsFalse(restored.matchHistory[1].playerWon);
        }

        [Test]
        public void SaveData_DefaultConstruct_HasEmptyHistory()
        {
            var data = new SaveData();

            Assert.IsNotNull(data.matchHistory);
            Assert.AreEqual(0, data.matchHistory.Count);
            Assert.AreEqual(0, data.walletBalance);
        }
    }
}
