using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SaveSystem"/>.
    ///
    /// Tests call the real Save / Load / Delete API against
    /// <c>Application.persistentDataPath</c> (valid in EditMode).
    /// TearDown deletes the test save file to avoid polluting real save data.
    /// </summary>
    public class SaveSystemTests
    {
        // ── Lifecycle ──────────────────────────────────────────────────────────

        [TearDown]
        public void Cleanup()
        {
            SaveSystem.Delete();
        }

        // ── Round-trip ────────────────────────────────────────────────────────

        [Test]
        public void Save_Then_Load_PreservesWalletBalance()
        {
            var data = new SaveData { walletBalance = 1234 };
            SaveSystem.Save(data);

            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(1234, loaded.walletBalance);
        }

        [Test]
        public void Save_Then_Load_PreservesMatchHistory()
        {
            var data = new SaveData { walletBalance = 100 };
            data.matchHistory.Add(new MatchRecord
            {
                timestamp       = "2026-04-10T12:00:00Z",
                playerWon       = true,
                durationSeconds = 45.5f,
                currencyEarned  = 250
            });

            SaveSystem.Save(data);
            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(1, loaded.matchHistory.Count);
            Assert.AreEqual("2026-04-10T12:00:00Z", loaded.matchHistory[0].timestamp);
            Assert.IsTrue(loaded.matchHistory[0].playerWon);
            Assert.AreEqual(250, loaded.matchHistory[0].currencyEarned);
        }

        [Test]
        public void Save_Then_Load_PreservesEquippedPartIds()
        {
            var record = new MatchRecord();
            record.equippedPartIds.Add("arm_heavy");
            record.equippedPartIds.Add("wheel_fast");
            var data = new SaveData();
            data.matchHistory.Add(record);

            SaveSystem.Save(data);
            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(2, loaded.matchHistory[0].equippedPartIds.Count);
            Assert.AreEqual("arm_heavy",  loaded.matchHistory[0].equippedPartIds[0]);
            Assert.AreEqual("wheel_fast", loaded.matchHistory[0].equippedPartIds[1]);
        }

        /// <summary>
        /// Indirect XOR correctness test: encrypted bytes on disk must decrypt
        /// back to the original value (XOR applied twice = identity).
        /// </summary>
        [Test]
        public void XorEncryption_RoundTrip_ProducesOriginalData()
        {
            var data = new SaveData { walletBalance = 0xDEAD };
            SaveSystem.Save(data);
            SaveData loaded = SaveSystem.Load();
            Assert.AreEqual(0xDEAD, loaded.walletBalance);
        }

        // ── Edge cases ────────────────────────────────────────────────────────

        [Test]
        public void Load_WhenNoFileExists_ReturnsDefaultSaveData()
        {
            SaveSystem.Delete(); // ensure no file

            SaveData data = SaveSystem.Load();

            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.walletBalance);
            Assert.IsNotNull(data.matchHistory);
            Assert.AreEqual(0, data.matchHistory.Count);
        }

        [Test]
        public void Delete_CausesSubsequentLoad_ToReturnDefaults()
        {
            SaveSystem.Save(new SaveData { walletBalance = 999 });
            SaveSystem.Delete();

            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(0, loaded.walletBalance);
        }

        [Test]
        public void Save_WithEmptyMatchHistory_RoundTripsCleanly()
        {
            var data = new SaveData { walletBalance = 42 };
            SaveSystem.Save(data);
            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(42, loaded.walletBalance);
            Assert.IsNotNull(loaded.matchHistory);
            Assert.AreEqual(0, loaded.matchHistory.Count);
        }

        [Test]
        public void Save_WithMultipleMatchRecords_PreservesCount()
        {
            var data = new SaveData();
            for (int i = 0; i < 5; i++)
                data.matchHistory.Add(new MatchRecord { currencyEarned = i * 10 });

            SaveSystem.Save(data);
            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(5, loaded.matchHistory.Count);
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(i * 10, loaded.matchHistory[i].currencyEarned);
        }
    }
}
