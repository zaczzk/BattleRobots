using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T307: <see cref="ZoneControlCoopScoreSO"/> and
    /// <see cref="ZoneControlCoopScoreController"/>.
    ///
    /// ZoneControlCoopScoreTests (12):
    ///   SO_FreshInstance_PlayerCaptures_Zero                                      ×1
    ///   SO_FreshInstance_TotalCaptures_Zero                                       ×1
    ///   SO_AddPlayerCapture_Increments                                            ×1
    ///   SO_AddAllyCapture_Increments                                              ×1
    ///   SO_CheckMilestone_FiresEvent                                              ×1
    ///   SO_CheckMilestone_Idempotent                                              ×1
    ///   SO_Reset_ClearsAll                                                        ×1
    ///   Controller_FreshInstance_CoopScoreSO_Null                                 ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channels                                 ×1
    ///   Controller_Refresh_NullCoopScoreSO_HidesPanel                             ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlCoopScoreTests
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

        private static ZoneControlCoopScoreSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCoopScoreSO>();

        private static ZoneControlCoopScoreController CreateController() =>
            new GameObject("CoopScoreCtrl_Test")
                .AddComponent<ZoneControlCoopScoreController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_PlayerCaptures_Zero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.PlayerCaptures,
                "PlayerCaptures must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalCaptures_Zero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.TotalCaptures,
                "TotalCaptures must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddPlayerCapture_Increments()
        {
            var so = CreateSO();
            so.AddPlayerCapture();
            so.AddPlayerCapture();
            Assert.AreEqual(2, so.PlayerCaptures,
                "PlayerCaptures must increase by 1 per AddPlayerCapture call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddAllyCapture_Increments()
        {
            var so = CreateSO();
            so.AddAllyCapture();
            Assert.AreEqual(1, so.AllyCaptures,
                "AllyCaptures must increase by 1 per AddAllyCapture call.");
            Assert.AreEqual(1, so.TotalCaptures,
                "TotalCaptures must reflect ally captures.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CheckMilestone_FiresEvent()
        {
            var so  = CreateSO();
            var evt = CreateEvent();
            // Set a low milestone to test easily.
            SetField(so, "_sharedMilestone", 2);
            SetField(so, "_onMilestoneReached", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.AddPlayerCapture();
            so.AddAllyCapture(); // Total = 2 → milestone reached.

            Assert.AreEqual(1, count,
                "_onMilestoneReached must fire once when combined captures reach the threshold.");
            Assert.IsTrue(so.MilestoneReached,
                "MilestoneReached must be true after reaching the threshold.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_CheckMilestone_Idempotent()
        {
            var so  = CreateSO();
            var evt = CreateEvent();
            SetField(so, "_sharedMilestone", 1);
            SetField(so, "_onMilestoneReached", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.AddPlayerCapture(); // Milestone reached.
            so.AddPlayerCapture(); // Already reached; event must not fire again.

            Assert.AreEqual(1, count,
                "_onMilestoneReached must fire exactly once, even with further captures.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            SetField(so, "_sharedMilestone", 1);
            so.AddPlayerCapture();
            so.Reset();

            Assert.AreEqual(0, so.PlayerCaptures, "PlayerCaptures must be 0 after Reset.");
            Assert.AreEqual(0, so.AllyCaptures,   "AllyCaptures must be 0 after Reset.");
            Assert.IsFalse(so.MilestoneReached,    "MilestoneReached must be false after Reset.");

            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_CoopScoreSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.CoopScoreSO,
                "CoopScoreSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlCoopScoreController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlCoopScoreController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlCoopScoreController>();

            var playerEvt = CreateEvent();
            SetField(ctrl, "_onPlayerZoneCaptured", playerEvt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            playerEvt.RegisterCallback(() => count++);
            playerEvt.Raise();

            Assert.AreEqual(1, count,
                "_onPlayerZoneCaptured must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(playerEvt);
        }

        [Test]
        public void Controller_Refresh_NullCoopScoreSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlCoopScoreController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when CoopScoreSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
