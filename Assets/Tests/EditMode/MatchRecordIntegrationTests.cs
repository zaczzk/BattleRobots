using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// Integration tests that exercise the full match-record persistence pipeline:
    ///   HealthSO damage → MatchRecord construction → SaveSystem → reload.
    ///
    /// These tests do NOT instantiate MatchManager (a MonoBehaviour that requires
    /// a scene and Time.deltaTime) — instead they exercise the shared data path:
    ///   HealthSO state changes + SaveSystem round-trip + SettingsData co-persistence.
    ///
    /// The MatchManager's win-condition logic is tested indirectly by verifying that
    /// HealthSO.IsAlive correctly reflects the state MatchManager would observe.
    /// </summary>
    [TestFixture]
    public sealed class MatchRecordIntegrationTests
    {
        private HealthSO _playerHealth;
        private HealthSO _opponentHealth;

        [SetUp]
        public void SetUp()
        {
            _playerHealth   = ScriptableObject.CreateInstance<HealthSO>();
            _opponentHealth = ScriptableObject.CreateInstance<HealthSO>();

            _playerHealth.Initialize();
            _opponentHealth.Initialize();

            SaveSystem.Delete();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_playerHealth);
            Object.DestroyImmediate(_opponentHealth);
            SaveSystem.Delete();
        }

        // ── HealthSO win-condition gate (mirrors MatchManager.Update logic) ──

        [Test]
        public void WinCondition_PlayerAlive_OpponentDead_IsDetectedCorrectly()
        {
            // Drain opponent HP to zero.
            _opponentHealth.TakeDamage(_opponentHealth.MaxHp);

            Assert.IsTrue(_playerHealth.IsAlive,    "Player should still be alive.");
            Assert.IsFalse(_opponentHealth.IsAlive, "Opponent should be dead after full-damage hit.");
        }

        [Test]
        public void WinCondition_PlayerDead_OpponentAlive_IsDetectedCorrectly()
        {
            _playerHealth.TakeDamage(_playerHealth.MaxHp);

            Assert.IsFalse(_playerHealth.IsAlive, "Player should be dead after full-damage hit.");
            Assert.IsTrue(_opponentHealth.IsAlive, "Opponent should still be alive.");
        }

        [Test]
        public void WinCondition_BothAlive_MatchContinues()
        {
            _playerHealth.TakeDamage(10f);
            _opponentHealth.TakeDamage(10f);

            Assert.IsTrue(_playerHealth.IsAlive,   "Player still alive after partial damage.");
            Assert.IsTrue(_opponentHealth.IsAlive, "Opponent still alive after partial damage.");
        }

        // ── MatchRecord + SaveSystem pipeline ─────────────────────────────────

        [Test]
        public void MatchRecord_PersistenceRoundTrip_WinScenario()
        {
            // Simulate end-of-match damage totals.
            float damageDone   = 85f;
            float damageTaken  = 20f;
            int   currencyWon  = 350;

            var record = new MatchRecord
            {
                timestamp       = "2026-04-05T10:00:00Z",
                arenaIndex      = 1,
                playerWon       = true,
                durationSeconds = 42.5f,
                damageDone      = damageDone,
                damageTaken     = damageTaken,
                currencyEarned  = currencyWon,
                walletSnapshot  = 850,
                equippedPartIds = new List<string> { "slot_body_0", "slot_weapon_0" },
            };

            var save = new SaveData { walletBalance = 850 };
            save.matchHistory.Add(record);
            SaveSystem.Save(save);

            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(1, loaded.matchHistory.Count);
            MatchRecord r = loaded.matchHistory[0];
            Assert.IsTrue(r.playerWon);
            Assert.AreEqual(damageDone,  r.damageDone,  0.01f);
            Assert.AreEqual(damageTaken, r.damageTaken, 0.01f);
            Assert.AreEqual(currencyWon, r.currencyEarned);
        }

        [Test]
        public void MatchRecord_PersistenceRoundTrip_LossScenario()
        {
            var record = new MatchRecord
            {
                timestamp       = "2026-04-05T11:00:00Z",
                arenaIndex      = 0,
                playerWon       = false,
                durationSeconds = 15f,
                damageDone      = 30f,
                damageTaken     = 100f,
                currencyEarned  = 0,
                walletSnapshot  = 500,
            };

            var save = new SaveData { walletBalance = 500 };
            save.matchHistory.Add(record);
            SaveSystem.Save(save);

            MatchRecord loaded = SaveSystem.Load().matchHistory[0];

            Assert.IsFalse(loaded.playerWon);
            Assert.AreEqual(0, loaded.currencyEarned);
        }

        // ── SettingsData co-persistence ───────────────────────────────────────

        [Test]
        public void SettingsData_PersistedAlongsideMatchHistory_BothSurvive()
        {
            var settings = new SettingsData
            {
                masterVolume   = 0.8f,
                sfxVolume      = 0.6f,
                invertControls = true,
            };

            var save = new SaveData
            {
                walletBalance = 200,
                settings      = settings,
            };
            save.matchHistory.Add(new MatchRecord { playerWon = true, currencyEarned = 200 });
            SaveSystem.Save(save);

            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual(1, loaded.matchHistory.Count, "Match history must persist alongside settings.");
            Assert.IsNotNull(loaded.settings,             "SettingsData must persist.");
            Assert.AreEqual(0.8f, loaded.settings.masterVolume, 1e-4f);
            Assert.IsTrue(loaded.settings.invertControls);
        }

        // ── History accumulation ──────────────────────────────────────────────

        [Test]
        public void MatchHistory_AccumulatesAcrossMultipleSaves()
        {
            for (int i = 0; i < 3; i++)
            {
                SaveData sd   = SaveSystem.Load();
                sd.matchHistory.Add(new MatchRecord { arenaIndex = i, playerWon = i % 2 == 0 });
                sd.walletBalance += 100;
                SaveSystem.Save(sd);
            }

            SaveData final = SaveSystem.Load();

            Assert.AreEqual(3, final.matchHistory.Count,  "Three sequential saves should accumulate 3 records.");
            Assert.AreEqual(300, final.walletBalance,     "Wallet balance should reflect three +100 increments.");
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(i, final.matchHistory[i].arenaIndex);
        }
    }
}
