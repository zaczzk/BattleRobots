using System.Collections.Generic;
using NUnit.Framework;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SaveSystem"/>.
    ///
    /// These tests write to Application.persistentDataPath (the real device path in
    /// Edit mode). The file is deleted in SetUp and TearDown so tests are hermetic.
    /// </summary>
    [TestFixture]
    public sealed class SaveSystemTests
    {
        // ── Fixture lifecycle ─────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Ensure no leftover save from a previous failed run.
            SaveSystem.Delete();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Load_WhenNoFileExists_ReturnsDefaultSaveData()
        {
            SaveData data = SaveSystem.Load();

            Assert.IsNotNull(data, "Load() must never return null.");
            Assert.AreEqual(0, data.walletBalance, "Default walletBalance should be 0.");
            Assert.IsNotNull(data.matchHistory, "matchHistory list must be initialized.");
            Assert.AreEqual(0, data.matchHistory.Count, "Fresh save should have no match history.");
        }

        [Test]
        public void SaveAndLoad_WalletBalance_RoundTrips()
        {
            var original = new SaveData { walletBalance = 1337 };
            SaveSystem.Save(original);

            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(1337, loaded.walletBalance,
                "walletBalance must survive a Save/Load round-trip.");
        }

        [Test]
        public void SaveAndLoad_SingleMatchRecord_RoundTrips()
        {
            var record = new MatchRecord
            {
                timestamp       = "2026-04-05T12:00:00Z",
                arenaIndex      = 2,
                playerWon       = true,
                durationSeconds = 73.5f,
                damageDone      = 88f,
                damageTaken     = 12f,
                currencyEarned  = 350,
                walletSnapshot  = 850,
                equippedPartIds = new List<string> { "slot_body_main", "slot_weapon_0" },
            };

            var original = new SaveData { walletBalance = 850 };
            original.matchHistory.Add(record);
            SaveSystem.Save(original);

            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(1, loaded.matchHistory.Count, "Should have exactly 1 match record.");

            MatchRecord r = loaded.matchHistory[0];
            Assert.AreEqual(record.timestamp,       r.timestamp);
            Assert.AreEqual(record.arenaIndex,      r.arenaIndex);
            Assert.AreEqual(record.playerWon,       r.playerWon);
            Assert.AreEqual(record.durationSeconds, r.durationSeconds, 0.001f);
            Assert.AreEqual(record.damageDone,      r.damageDone,      0.001f);
            Assert.AreEqual(record.damageTaken,     r.damageTaken,     0.001f);
            Assert.AreEqual(record.currencyEarned,  r.currencyEarned);
            Assert.AreEqual(record.walletSnapshot,  r.walletSnapshot);
            Assert.AreEqual(2, r.equippedPartIds.Count);
            Assert.AreEqual("slot_body_main", r.equippedPartIds[0]);
            Assert.AreEqual("slot_weapon_0",  r.equippedPartIds[1]);
        }

        [Test]
        public void SaveAndLoad_MultipleMatchRecords_AllPresent()
        {
            var data = new SaveData { walletBalance = 500 };
            for (int i = 0; i < 5; i++)
            {
                data.matchHistory.Add(new MatchRecord
                {
                    arenaIndex     = i,
                    playerWon      = i % 2 == 0,
                    currencyEarned = i * 100,
                });
            }
            SaveSystem.Save(data);

            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(5, loaded.matchHistory.Count, "All 5 records should survive.");
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i,          loaded.matchHistory[i].arenaIndex);
                Assert.AreEqual(i % 2 == 0, loaded.matchHistory[i].playerWon);
                Assert.AreEqual(i * 100,    loaded.matchHistory[i].currencyEarned);
            }
        }

        [Test]
        public void Delete_RemovesFile_SubsequentLoadReturnsDefault()
        {
            SaveSystem.Save(new SaveData { walletBalance = 999 });
            SaveSystem.Delete();

            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(0, loaded.walletBalance,
                "After Delete, Load should return default (walletBalance = 0).");
        }

        [Test]
        public void Save_CalledWithNull_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => SaveSystem.Save(null));
        }
    }
}
