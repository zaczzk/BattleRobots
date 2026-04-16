using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T313: <see cref="ZoneControlSpeedrunSO"/> and
    /// <see cref="ZoneControlSpeedrunController"/>.
    ///
    /// ZoneControlSpeedrunTests (12):
    ///   SO_FreshInstance_RecordCount_Zero                                         ×1
    ///   SO_FreshInstance_GetBestTime_ReturnsMinusOne                              ×1
    ///   SO_RecordAttempt_SetsRecord                                               ×1
    ///   SO_RecordAttempt_OnlyUpdateOnImprovement                                  ×1
    ///   SO_RecordAttempt_NegativeTime_Ignored                                     ×1
    ///   SO_Reset_ClearsAll                                                        ×1
    ///   Controller_FreshInstance_SpeedrunSO_Null                                  ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channel                                  ×1
    ///   Controller_HandleMatchEnded_NullSO_NoThrow                                ×1
    ///   Controller_Refresh_NullSO_HidesPanel                                      ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlSpeedrunTests
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

        private static ZoneControlSpeedrunSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlSpeedrunSO>();

        private static ZoneControlSpeedrunController CreateController() =>
            new GameObject("SpeedrunCtrl_Test")
                .AddComponent<ZoneControlSpeedrunController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_RecordCount_Zero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.RecordCount,
                "RecordCount must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GetBestTime_ReturnsMinusOne()
        {
            var so = CreateSO();
            Assert.AreEqual(-1f, so.GetBestTime(5),
                "GetBestTime must return -1 when no record exists for the given zone count.");
            Assert.IsFalse(so.HasRecord(5),
                "HasRecord must return false when no record exists.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordAttempt_SetsRecord()
        {
            var so = CreateSO();
            so.RecordAttempt(30f, 5);

            Assert.IsTrue(so.HasRecord(5),
                "HasRecord must return true after a valid RecordAttempt.");
            Assert.AreEqual(30f, so.GetBestTime(5), 0.001f,
                "GetBestTime must return the recorded time.");
            Assert.AreEqual(1, so.RecordCount,
                "RecordCount must be 1 after one unique zone-count entry.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordAttempt_OnlyUpdateOnImprovement()
        {
            var so = CreateSO();
            so.RecordAttempt(30f, 5);
            so.RecordAttempt(45f, 5); // Slower — must not replace.
            Assert.AreEqual(30f, so.GetBestTime(5), 0.001f,
                "A slower attempt must not replace the existing best time.");

            so.RecordAttempt(20f, 5); // Faster — must replace.
            Assert.AreEqual(20f, so.GetBestTime(5), 0.001f,
                "A faster attempt must replace the existing best time.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordAttempt_NegativeTime_Ignored()
        {
            var so = CreateSO();
            so.RecordAttempt(-1f, 5);
            Assert.IsFalse(so.HasRecord(5),
                "A negative time must be ignored and must not create a record.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordAttempt(10f, 3);
            so.RecordAttempt(20f, 6);
            so.Reset();

            Assert.AreEqual(0, so.RecordCount,
                "RecordCount must be 0 after Reset.");
            Assert.IsFalse(so.HasRecord(3),
                "HasRecord must return false after Reset.");
            Assert.IsFalse(so.HasRecord(6),
                "HasRecord must return false after Reset (second entry).");

            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_SpeedrunSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.SpeedrunSO,
                "SpeedrunSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlSpeedrunController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlSpeedrunController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlSpeedrunController>();

            var matchEndEvt = CreateEvent();
            SetField(ctrl, "_onMatchEnded", matchEndEvt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            matchEndEvt.RegisterCallback(() => count++);
            matchEndEvt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEndEvt);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullSO_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when SpeedrunSO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlSpeedrunController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when SpeedrunSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
