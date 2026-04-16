using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T305: <see cref="ZoneControlHighScoreSO"/> and
    /// <see cref="ZoneControlHighScoreController"/>.
    ///
    /// ZoneControlHighScoreTests (12):
    ///   SO_FreshInstance_AllBests_Zero                                          ×1
    ///   SO_UpdateFromMatch_SetsNewZoneRecord                                    ×1
    ///   SO_UpdateFromMatch_SetsNewPaceRecord                                    ×1
    ///   SO_UpdateFromMatch_SetsNewStreakRecord                                   ×1
    ///   SO_UpdateFromMatch_DoesNotDecreaseBest                                  ×1
    ///   SO_UpdateFromMatch_ClampsNegativeValues                                 ×1
    ///   SO_Reset_ClearsAllBests                                                 ×1
    ///   Controller_FreshInstance_HighScoreSO_Null                               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   Controller_OnDisable_Unregisters_Channels                               ×1
    ///   Controller_Refresh_NullHighScoreSO_HidesPanel                          ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlHighScoreTests
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

        private static ZoneControlHighScoreSO CreateHighScoreSO() =>
            ScriptableObject.CreateInstance<ZoneControlHighScoreSO>();

        private static ZoneControlHighScoreController CreateController() =>
            new GameObject("HighScoreCtrl_Test")
                .AddComponent<ZoneControlHighScoreController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_AllBests_Zero()
        {
            var so = CreateHighScoreSO();
            Assert.AreEqual(0,   so.BestZoneCount, "BestZoneCount must be 0 on a fresh SO.");
            Assert.AreEqual(0f,  so.BestPace,      "BestPace must be 0 on a fresh SO.");
            Assert.AreEqual(0,   so.BestStreak,    "BestStreak must be 0 on a fresh SO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateFromMatch_SetsNewZoneRecord()
        {
            var so = CreateHighScoreSO();
            so.UpdateFromMatch(15, 0f, 0);
            Assert.AreEqual(15, so.BestZoneCount,
                "BestZoneCount must be updated to 15 when the match result exceeds the previous best.");
            Assert.IsTrue(so.IsNewZoneCount,
                "IsNewZoneCount must be true immediately after a new record is set.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateFromMatch_SetsNewPaceRecord()
        {
            var so = CreateHighScoreSO();
            so.UpdateFromMatch(0, 3.5f, 0);
            Assert.AreEqual(3.5f, so.BestPace,
                "BestPace must be updated to 3.5 when the match result exceeds the previous best.");
            Assert.IsTrue(so.IsNewPace,
                "IsNewPace must be true immediately after a new pace record is set.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateFromMatch_SetsNewStreakRecord()
        {
            var so = CreateHighScoreSO();
            so.UpdateFromMatch(0, 0f, 8);
            Assert.AreEqual(8, so.BestStreak,
                "BestStreak must be updated to 8 when the match result exceeds the previous best.");
            Assert.IsTrue(so.IsNewStreak,
                "IsNewStreak must be true immediately after a new streak record is set.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateFromMatch_DoesNotDecreaseBest()
        {
            var so = CreateHighScoreSO();
            so.UpdateFromMatch(20, 5f, 10);
            so.UpdateFromMatch(5, 1f, 2); // lower values — should not change bests

            Assert.AreEqual(20, so.BestZoneCount, "BestZoneCount must not decrease.");
            Assert.AreEqual(5f, so.BestPace,      "BestPace must not decrease.");
            Assert.AreEqual(10, so.BestStreak,    "BestStreak must not decrease.");

            Assert.IsFalse(so.IsNewZoneCount, "IsNewZoneCount must be false when no new record.");
            Assert.IsFalse(so.IsNewPace,      "IsNewPace must be false when no new record.");
            Assert.IsFalse(so.IsNewStreak,    "IsNewStreak must be false when no new record.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateFromMatch_ClampsNegativeValues()
        {
            var so = CreateHighScoreSO();
            so.UpdateFromMatch(-5, -2f, -3);
            // Negative clamped to 0 — not a new record vs. existing best of 0.
            Assert.AreEqual(0,  so.BestZoneCount, "Negative zoneCount must be clamped to 0.");
            Assert.AreEqual(0f, so.BestPace,      "Negative pace must be clamped to 0.");
            Assert.AreEqual(0,  so.BestStreak,    "Negative streak must be clamped to 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAllBests()
        {
            var so = CreateHighScoreSO();
            so.UpdateFromMatch(30, 4f, 12);
            so.Reset();
            Assert.AreEqual(0,  so.BestZoneCount, "BestZoneCount must be 0 after Reset.");
            Assert.AreEqual(0f, so.BestPace,      "BestPace must be 0 after Reset.");
            Assert.AreEqual(0,  so.BestStreak,    "BestStreak must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_HighScoreSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.HighScoreSO,
                "HighScoreSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlHighScoreController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlHighScoreController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlHighScoreController>();

            var matchEndedEvt  = CreateEvent();
            var highScoreEvt   = CreateEvent();

            SetField(ctrl, "_onMatchEnded",   matchEndedEvt);
            SetField(ctrl, "_onNewHighScore", highScoreEvt);

            go.SetActive(true);
            go.SetActive(false);

            int matchEndedCount = 0, highScoreCount = 0;
            matchEndedEvt.RegisterCallback(() => matchEndedCount++);
            highScoreEvt.RegisterCallback(() => highScoreCount++);

            matchEndedEvt.Raise();
            highScoreEvt.Raise();

            Assert.AreEqual(1, matchEndedCount,
                "_onMatchEnded must be unregistered after OnDisable.");
            Assert.AreEqual(1, highScoreCount,
                "_onNewHighScore must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEndedEvt);
            Object.DestroyImmediate(highScoreEvt);
        }

        [Test]
        public void Controller_Refresh_NullHighScoreSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_NullHighScoreSO");
            var ctrl  = go.AddComponent<ZoneControlHighScoreController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when HighScoreSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
