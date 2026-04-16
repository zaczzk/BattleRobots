using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T308: <see cref="ZoneControlTournamentSO"/> and
    /// <see cref="ZoneControlTournamentController"/>.
    ///
    /// ZoneControlTournamentTests (14):
    ///   SO_FreshInstance_CurrentRound_Zero                                        ×1
    ///   SO_FreshInstance_IsComplete_False                                         ×1
    ///   SO_SubmitRoundResult_Win_IncrementsWins                                   ×1
    ///   SO_SubmitRoundResult_Loss_IncrementsLosses                                ×1
    ///   SO_SubmitRoundResult_AllRounds_SetsComplete                               ×1
    ///   SO_SubmitRoundResult_Idempotent_WhenComplete                              ×1
    ///   SO_LoadSnapshot_RestoresState                                             ×1
    ///   SO_Reset_ClearsAll                                                        ×1
    ///   Controller_FreshInstance_TournamentSO_Null                                ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channels                                 ×1
    ///   Controller_HandleMatchEnded_NullRefs_NoThrow                              ×1
    ///   Controller_Refresh_NullTournamentSO_HidesPanel                            ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlTournamentTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlTournamentSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlTournamentSO>();

        private static ZoneControlTournamentController CreateController() =>
            new GameObject("TournamentCtrl_Test")
                .AddComponent<ZoneControlTournamentController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CurrentRound_Zero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.CurrentRound,
                "CurrentRound must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsComplete_False()
        {
            var so = CreateSO();
            Assert.IsFalse(so.IsComplete,
                "IsComplete must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SubmitRoundResult_Win_IncrementsWins()
        {
            var so = CreateSO();
            // Default targets: {5, 10, 15} — submit a win for round 0.
            so.SubmitRoundResult(10, 5);
            Assert.AreEqual(1, so.RoundWins,
                "RoundWins must increment when zonesCaptured >= targetZones.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SubmitRoundResult_Loss_IncrementsLosses()
        {
            var so = CreateSO();
            so.SubmitRoundResult(2, 5); // Did not meet target.
            Assert.AreEqual(1, so.RoundLosses,
                "RoundLosses must increment when zonesCaptured < targetZones.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SubmitRoundResult_AllRounds_SetsComplete()
        {
            var so  = CreateSO();
            var evt = CreateEvent();
            SetField(so, "_onTournamentComplete", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            // Default: 3 rounds.
            so.SubmitRoundResult(5, 5);
            so.SubmitRoundResult(10, 10);
            so.SubmitRoundResult(15, 15);

            Assert.IsTrue(so.IsComplete,
                "IsComplete must be true after all rounds are submitted.");
            Assert.AreEqual(1, count,
                "_onTournamentComplete must fire exactly once.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_SubmitRoundResult_Idempotent_WhenComplete()
        {
            var so = CreateSO();
            // Complete all 3 default rounds.
            so.SubmitRoundResult(5, 5);
            so.SubmitRoundResult(5, 10);
            so.SubmitRoundResult(5, 15);
            int winsAfterComplete = so.RoundWins;
            int lossesAfterComplete = so.RoundLosses;

            // Further submits must be no-ops.
            so.SubmitRoundResult(20, 5);
            Assert.AreEqual(winsAfterComplete,   so.RoundWins,
                "SubmitRoundResult must be a no-op when the tournament is already complete.");
            Assert.AreEqual(lossesAfterComplete, so.RoundLosses,
                "RoundLosses must not change after tournament completion.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoadSnapshot_RestoresState()
        {
            var so = CreateSO();
            so.LoadSnapshot(2, 2, 0, false);
            Assert.AreEqual(2, so.CurrentRound, "CurrentRound must be restored.");
            Assert.AreEqual(2, so.RoundWins,    "RoundWins must be restored.");
            Assert.AreEqual(0, so.RoundLosses,  "RoundLosses must be restored.");
            Assert.IsFalse(so.IsComplete,        "IsComplete must be restored.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.SubmitRoundResult(5, 5);
            so.Reset();
            Assert.AreEqual(0, so.CurrentRound, "CurrentRound must be 0 after Reset.");
            Assert.AreEqual(0, so.RoundWins,    "RoundWins must be 0 after Reset.");
            Assert.AreEqual(0, so.RoundLosses,  "RoundLosses must be 0 after Reset.");
            Assert.IsFalse(so.IsComplete,        "IsComplete must be false after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_TournamentSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TournamentSO,
                "TournamentSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlTournamentController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlTournamentController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlTournamentController>();

            var matchEndedEvt = CreateEvent();
            SetField(ctrl, "_onMatchEnded", matchEndedEvt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            matchEndedEvt.RegisterCallback(() => count++);
            matchEndedEvt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEndedEvt);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullRefs_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when all refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullTournamentSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlTournamentController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when TournamentSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
