using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T079 (HealthBarUI) and T080 (MatchEndScreenUI).
    ///
    /// Coverage (20 cases):
    ///
    /// HealthBarUI — FloatGameEvent-path subscription + state tracking
    ///   [01] DefaultDisplayedHp_EqualsMaxHp
    ///   [02] DefaultDisplayedMaxHp_EqualsInspectorDefault
    ///   [03] SetMaxHp_UpdatesDisplayedMaxHp
    ///   [04] SetMaxHp_ResetsDisplayedHpToNewMax
    ///   [05] SetMaxHp_ClampsToMinimumOne
    ///   [06] HandleHealthChanged_UpdatesDisplayedHp
    ///   [07] HandleHealthChanged_MultipleUpdates_TracksLatest
    ///   [08] HandleHealthChanged_ZeroHp_DisplayedHpIsZero
    ///   [09] NullChannel_DoesNotThrow_OnEnableDisable
    ///   [10] SetMaxHp_ThenHealthChanged_BothFieldsCorrect
    ///
    /// MatchEndScreenUI — HandleMatchEnd + IsPanelVisible + LastMatchWon
    ///   [11] DefaultState_IsPanelVisible_False
    ///   [12] DefaultState_LastMatchWon_IsNull
    ///   [13] HandleMatchEnd_NoSaveFile_PanelBecomesVisible
    ///   [14] HandleMatchEnd_NoSaveFile_LastMatchWon_IsFalse
    ///   [15] HandleMatchEnd_WinRecord_LastMatchWon_IsTrue
    ///   [16] HandleMatchEnd_LossRecord_LastMatchWon_IsFalse
    ///   [17] HandleMatchEnd_CalledTwice_ReflectsLatestRecord
    ///   [18] HandleContinueClicked_HidesPanel
    ///   [19] HandleMatchEnd_MultipleRecords_ReadsLastOne
    ///   [20] HandleMatchEnd_NullChannels_DoesNotThrow
    /// </summary>
    [TestFixture]
    public sealed class HealthBarAndMatchEndScreenTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject   _healthBarGO;
        private HealthBarUI  _healthBar;

        private GameObject       _endScreenGO;
        private MatchEndScreenUI _endScreen;

        [SetUp]
        public void SetUp()
        {
            _healthBarGO = new GameObject("TestHealthBar");
            _healthBar   = _healthBarGO.AddComponent<HealthBarUI>();

            SaveSystem.Delete();
            _endScreenGO = new GameObject("TestMatchEndScreen");
            _endScreen   = _endScreenGO.AddComponent<MatchEndScreenUI>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_healthBarGO);
            Object.DestroyImmediate(_endScreenGO);
            SaveSystem.Delete();
        }

        // ══════════════════════════════════════════════════════════════════════
        // HealthBarUI tests
        // ══════════════════════════════════════════════════════════════════════

        // ── [01] Default DisplayedHp == max ───────────────────────────────────

        [Test]
        public void HealthBar_DefaultDisplayedHp_EqualsMaxHp()
        {
            Assert.AreEqual(_healthBar.DisplayedMaxHp, _healthBar.DisplayedHp, 0.001f,
                "DisplayedHp must equal DisplayedMaxHp (full health) immediately after Awake.");
        }

        // ── [02] Default DisplayedMaxHp == 100 (serialised field default) ─────

        [Test]
        public void HealthBar_DefaultDisplayedMaxHp_EqualsInspectorDefault()
        {
            Assert.AreEqual(100f, _healthBar.DisplayedMaxHp, 0.001f,
                "DisplayedMaxHp must equal the serialised _maxHp default (100).");
        }

        // ── [03] SetMaxHp updates DisplayedMaxHp ──────────────────────────────

        [Test]
        public void HealthBar_SetMaxHp_UpdatesDisplayedMaxHp()
        {
            _healthBar.SetMaxHp(150f);

            Assert.AreEqual(150f, _healthBar.DisplayedMaxHp, 0.001f);
        }

        // ── [04] SetMaxHp resets DisplayedHp to new max ───────────────────────

        [Test]
        public void HealthBar_SetMaxHp_ResetsDisplayedHpToNewMax()
        {
            _healthBar.SetMaxHp(200f);

            Assert.AreEqual(200f, _healthBar.DisplayedHp, 0.001f,
                "After SetMaxHp the bar should display full health at the new max.");
        }

        // ── [05] SetMaxHp clamps non-positive values to 1 ────────────────────

        [Test]
        public void HealthBar_SetMaxHp_ClampsToMinimumOne()
        {
            _healthBar.SetMaxHp(-50f);

            Assert.AreEqual(1f, _healthBar.DisplayedMaxHp, 0.001f,
                "SetMaxHp with a non-positive argument must clamp to 1.");
        }

        // ── [06] HandleHealthChanged updates DisplayedHp ──────────────────────

        [Test]
        public void HealthBar_HandleHealthChanged_UpdatesDisplayedHp()
        {
            RaiseHealthChanged(_healthBar, 60f);

            Assert.AreEqual(60f, _healthBar.DisplayedHp, 0.001f);
        }

        // ── [07] Multiple HandleHealthChanged calls track the latest value ────

        [Test]
        public void HealthBar_HandleHealthChanged_MultipleUpdates_TracksLatest()
        {
            RaiseHealthChanged(_healthBar, 80f);
            RaiseHealthChanged(_healthBar, 50f);
            RaiseHealthChanged(_healthBar, 25f);

            Assert.AreEqual(25f, _healthBar.DisplayedHp, 0.001f);
        }

        // ── [08] HandleHealthChanged with 0 sets DisplayedHp to 0 ─────────────

        [Test]
        public void HealthBar_HandleHealthChanged_ZeroHp_DisplayedHpIsZero()
        {
            RaiseHealthChanged(_healthBar, 0f);

            Assert.AreEqual(0f, _healthBar.DisplayedHp, 0.001f,
                "Zero HP (death) must be correctly reflected in DisplayedHp.");
        }

        // ── [09] Null channel does not throw on enable / disable ──────────────

        [Test]
        public void HealthBar_NullChannel_DoesNotThrow_OnEnableDisable()
        {
            // _healthChangedChannel is null (not wired); cycling active state must be safe.
            Assert.DoesNotThrow(() =>
            {
                _healthBarGO.SetActive(false);
                _healthBarGO.SetActive(true);
            }, "HealthBarUI must not throw when _healthChangedChannel is null.");
        }

        // ── [10] SetMaxHp then HandleHealthChanged: both fields correct ───────

        [Test]
        public void HealthBar_SetMaxHp_ThenHealthChanged_BothFieldsCorrect()
        {
            _healthBar.SetMaxHp(80f);
            RaiseHealthChanged(_healthBar, 40f);

            Assert.AreEqual(80f, _healthBar.DisplayedMaxHp, 0.001f,
                "DisplayedMaxHp must remain at the value set by SetMaxHp.");
            Assert.AreEqual(40f, _healthBar.DisplayedHp, 0.001f,
                "DisplayedHp must reflect the value from HandleHealthChanged.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // MatchEndScreenUI tests
        // ══════════════════════════════════════════════════════════════════════

        // ── [11] Panel hidden on Awake ────────────────────────────────────────

        [Test]
        public void MatchEndScreen_DefaultState_IsPanelVisible_False()
        {
            Assert.IsFalse(_endScreen.IsPanelVisible,
                "IsPanelVisible must be false immediately after Awake.");
        }

        // ── [12] LastMatchWon is null before first HandleMatchEnd ─────────────

        [Test]
        public void MatchEndScreen_DefaultState_LastMatchWon_IsNull()
        {
            Assert.IsNull(_endScreen.LastMatchWon,
                "LastMatchWon must be null before HandleMatchEnd is called.");
        }

        // ── [13] HandleMatchEnd with no save file: panel becomes visible ──────

        [Test]
        public void MatchEndScreen_HandleMatchEnd_NoSaveFile_PanelBecomesVisible()
        {
            _endScreen.HandleMatchEnd();

            Assert.IsTrue(_endScreen.IsPanelVisible,
                "IsPanelVisible must be true after HandleMatchEnd even with no save data.");
        }

        // ── [14] HandleMatchEnd with no save file: defaults to loss ───────────

        [Test]
        public void MatchEndScreen_HandleMatchEnd_NoSaveFile_LastMatchWon_IsFalse()
        {
            // SaveSystem returns empty history — should show loss UI.
            _endScreen.HandleMatchEnd();

            Assert.AreEqual(false, _endScreen.LastMatchWon,
                "With an empty match history, LastMatchWon must default to false.");
        }

        // ── [15] Win record → LastMatchWon true ───────────────────────────────

        [Test]
        public void MatchEndScreen_HandleMatchEnd_WinRecord_LastMatchWon_IsTrue()
        {
            WriteRecord(playerWon: true, currency: 200);

            _endScreen.HandleMatchEnd();

            Assert.AreEqual(true, _endScreen.LastMatchWon,
                "LastMatchWon must be true when the last MatchRecord has playerWon = true.");
        }

        // ── [16] Loss record → LastMatchWon false ─────────────────────────────

        [Test]
        public void MatchEndScreen_HandleMatchEnd_LossRecord_LastMatchWon_IsFalse()
        {
            WriteRecord(playerWon: false, currency: 50);

            _endScreen.HandleMatchEnd();

            Assert.AreEqual(false, _endScreen.LastMatchWon,
                "LastMatchWon must be false when the last MatchRecord has playerWon = false.");
        }

        // ── [17] Second call reflects newest record ────────────────────────────

        [Test]
        public void MatchEndScreen_HandleMatchEnd_CalledTwice_ReflectsLatestRecord()
        {
            WriteRecord(playerWon: false, currency: 50);
            _endScreen.HandleMatchEnd();
            Assert.AreEqual(false, _endScreen.LastMatchWon, "First call: loss.");

            // Append a win record.
            SaveData data = SaveSystem.Load();
            data.matchHistory.Add(new MatchRecord { playerWon = true, currencyEarned = 200 });
            SaveSystem.Save(data);

            _endScreen.HandleMatchEnd();

            Assert.AreEqual(true, _endScreen.LastMatchWon,
                "Second HandleMatchEnd must reflect the newest (win) record.");
        }

        // ── [18] Continue button hides panel ──────────────────────────────────

        [Test]
        public void MatchEndScreen_HandleContinueClicked_HidesPanel()
        {
            _endScreen.HandleMatchEnd();
            Assert.IsTrue(_endScreen.IsPanelVisible, "Panel must be shown first.");

            InvokeContinue(_endScreen);

            Assert.IsFalse(_endScreen.IsPanelVisible,
                "IsPanelVisible must be false after HandleContinueClicked.");
        }

        // ── [19] Multiple records: reads the last one ─────────────────────────

        [Test]
        public void MatchEndScreen_HandleMatchEnd_MultipleRecords_ReadsLastOne()
        {
            var data = new SaveData();
            data.matchHistory.Add(new MatchRecord { playerWon = false });
            data.matchHistory.Add(new MatchRecord { playerWon = false });
            data.matchHistory.Add(new MatchRecord { playerWon = true, currencyEarned = 200 });
            SaveSystem.Save(data);

            _endScreen.HandleMatchEnd();

            Assert.AreEqual(true, _endScreen.LastMatchWon,
                "HandleMatchEnd must read the last element of matchHistory.");
        }

        // ── [20] Null channels do not throw ───────────────────────────────────

        [Test]
        public void MatchEndScreen_NullChannels_DoesNotThrow()
        {
            // _onMatchEnd and _onContinue are null (not wired in test).
            Assert.DoesNotThrow(
                () => _endScreen.HandleMatchEnd(),
                "HandleMatchEnd must not throw when event channel fields are null.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Invokes <c>HealthBarUI.HandleHealthChanged(hp)</c> via the pre-cached private
        /// delegate field <c>_handleHealthChanged</c>.  Mirrors the path taken at runtime
        /// when a FloatGameEvent SO broadcasts to a registered callback.
        /// </summary>
        private static void RaiseHealthChanged(HealthBarUI bar, float hp)
        {
            FieldInfo fi = typeof(HealthBarUI).GetField(
                "_handleHealthChanged",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(fi,
                "HealthBarUI._handleHealthChanged not found. Ensure the field name matches production code.");

            var del = fi.GetValue(bar) as System.Action<float>;

            Assert.IsNotNull(del,
                "_handleHealthChanged delegate is null — was HealthBarUI.Awake() called?");

            del.Invoke(hp);
        }

        /// <summary>
        /// Invokes <c>MatchEndScreenUI.HandleContinueClicked()</c> via reflection.
        /// </summary>
        private static void InvokeContinue(MatchEndScreenUI screen)
        {
            MethodInfo mi = typeof(MatchEndScreenUI).GetMethod(
                "HandleContinueClicked",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(mi,
                "MatchEndScreenUI.HandleContinueClicked not found via reflection.");

            mi.Invoke(screen, null);
        }

        /// <summary>Writes one <see cref="MatchRecord"/> to SaveSystem for hermetic tests.</summary>
        private static void WriteRecord(bool playerWon, int currency)
        {
            var data = new SaveData();
            data.matchHistory.Add(new MatchRecord
            {
                playerWon       = playerWon,
                currencyEarned  = currency,
                durationSeconds = 60f,
                damageDone      = 50f,
                damageTaken     = 30f,
            });
            SaveSystem.Save(data);
        }
    }
}
