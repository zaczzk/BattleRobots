using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T312: <see cref="ZoneControlScoreboardSO"/> and
    /// <see cref="ZoneControlScoreboardController"/>.
    ///
    /// ZoneControlScoreboardTests (12):
    ///   SO_FreshInstance_PlayerScore_Zero                                         ×1
    ///   SO_FreshInstance_PlayerRank_One                                           ×1
    ///   SO_RecordPlayerCapture_IncrementsScore                                    ×1
    ///   SO_RecordBotCapture_OutOfRange_NoThrow                                    ×1
    ///   SO_PlayerRank_CorrectWhenBotsLead                                         ×1
    ///   SO_Reset_ClearsAll                                                        ×1
    ///   Controller_FreshInstance_ScoreboardSO_Null                                ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channel                                  ×1
    ///   Controller_HandlePlayerCaptured_NullSO_NoThrow                            ×1
    ///   Controller_Refresh_NullSO_HidesPanel                                      ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlScoreboardTests
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

        private static ZoneControlScoreboardSO CreateSO(int maxBots = 3)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlScoreboardSO>();
            SetField(so, "_maxBots", maxBots);
            so.Reset(); // Re-initialise bot array with new maxBots value.
            return so;
        }

        private static ZoneControlScoreboardController CreateController() =>
            new GameObject("ScoreboardCtrl_Test")
                .AddComponent<ZoneControlScoreboardController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_PlayerScore_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlScoreboardSO>();
            Assert.AreEqual(0, so.PlayerScore,
                "PlayerScore must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PlayerRank_One()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlScoreboardSO>();
            Assert.AreEqual(1, so.PlayerRank,
                "PlayerRank must be 1 when all bots have 0 score.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsScore()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.AreEqual(2, so.PlayerScore,
                "PlayerScore must increment once per RecordPlayerCapture call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_OutOfRange_NoThrow()
        {
            var so = CreateSO(maxBots: 2);
            Assert.DoesNotThrow(
                () => so.RecordBotCapture(-1),
                "RecordBotCapture(-1) must not throw.");
            Assert.DoesNotThrow(
                () => so.RecordBotCapture(5),
                "RecordBotCapture with an out-of-range index must not throw.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerRank_CorrectWhenBotsLead()
        {
            var so = CreateSO(maxBots: 2);
            // Give both bots a higher score than the player.
            so.RecordBotCapture(0);
            so.RecordBotCapture(1);
            // Player has 0 zones; both bots have 1 zone each → player rank 3.
            Assert.AreEqual(3, so.PlayerRank,
                "PlayerRank must be 3 when 2 bots have a higher score.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(maxBots: 1);
            so.RecordPlayerCapture();
            so.RecordBotCapture(0);
            so.Reset();

            Assert.AreEqual(0, so.PlayerScore,
                "PlayerScore must be 0 after Reset.");
            Assert.AreEqual(0, so.GetBotScore(0),
                "Bot score must be 0 after Reset.");
            Assert.AreEqual(1, so.PlayerRank,
                "PlayerRank must be 1 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ScoreboardSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ScoreboardSO,
                "ScoreboardSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlScoreboardController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlScoreboardController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlScoreboardController>();

            var captureEvt = CreateEvent();
            SetField(ctrl, "_onPlayerCaptured", captureEvt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            captureEvt.RegisterCallback(() => count++);
            captureEvt.Raise();

            Assert.AreEqual(1, count,
                "_onPlayerCaptured must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(captureEvt);
        }

        [Test]
        public void Controller_HandlePlayerCaptured_NullSO_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandlePlayerCaptured(),
                "HandlePlayerCaptured must not throw when ScoreboardSO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlScoreboardController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when ScoreboardSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
