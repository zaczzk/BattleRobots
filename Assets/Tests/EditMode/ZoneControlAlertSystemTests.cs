using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T325: <see cref="ZoneControlAlertSystemSO"/> and
    /// <see cref="ZoneControlAlertSystemController"/>.
    ///
    /// ZoneControlAlertSystemTests (12):
    ///   SO_FreshInstance_IsCritical_False                             ×1
    ///   SO_EvaluateAlert_AllConditionsMet_SetsCritical                ×1
    ///   SO_EvaluateAlert_PressureOnly_NotCritical                     ×1
    ///   SO_EvaluateAlert_AllMet_FiresCriticalAlertEvent               ×1
    ///   SO_EvaluateAlert_CriticalToFalse_FiresAlertClearedEvent       ×1
    ///   SO_EvaluateAlert_SameState_NoEvent                            ×1
    ///   SO_Reset_ClearsCritical                                       ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_Unregisters_Channel                      ×1
    ///   Controller_Refresh_NullAlertSO_HidesPanel                     ×1
    ///   Controller_HandleMatchStarted_ResetsAlertSO                   ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlAlertSystemTests
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

        private static ZoneControlAlertSystemSO CreateAlertSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAlertSystemSO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsCritical_False()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAlertSystemSO>();
            Assert.IsFalse(so.IsCritical,
                "IsCritical must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateAlert_AllConditionsMet_SetsCritical()
        {
            var so = CreateAlertSO();
            so.EvaluateAlert(isHighPressure: true, threat: ThreatLevel.High, hasDominance: false);
            Assert.IsTrue(so.IsCritical,
                "IsCritical must be true when all three danger conditions are met.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateAlert_PressureOnly_NotCritical()
        {
            var so = CreateAlertSO();
            so.EvaluateAlert(isHighPressure: true, threat: ThreatLevel.Low, hasDominance: false);
            Assert.IsFalse(so.IsCritical,
                "IsCritical must be false when threat is not High.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateAlert_AllMet_FiresCriticalAlertEvent()
        {
            var so  = CreateAlertSO();
            var evt = CreateEvent();
            SetField(so, "_onCriticalAlert", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.EvaluateAlert(true, ThreatLevel.High, false);
            Assert.AreEqual(1, fired,
                "_onCriticalAlert must fire on the false→true transition.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluateAlert_CriticalToFalse_FiresAlertClearedEvent()
        {
            var so  = CreateAlertSO();
            var evt = CreateEvent();
            SetField(so, "_onAlertCleared", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.EvaluateAlert(true,  ThreatLevel.High, false); // critical
            so.EvaluateAlert(false, ThreatLevel.High, false); // cleared

            Assert.AreEqual(1, fired,
                "_onAlertCleared must fire on the true→false transition.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluateAlert_SameState_NoEvent()
        {
            var so  = CreateAlertSO();
            var evt = CreateEvent();
            SetField(so, "_onCriticalAlert", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            // Both calls result in IsCritical == false.
            so.EvaluateAlert(false, ThreatLevel.Low, true);
            so.EvaluateAlert(false, ThreatLevel.Low, true);
            Assert.AreEqual(0, fired,
                "_onCriticalAlert must NOT fire when state does not change.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsCritical()
        {
            var so = CreateAlertSO();
            so.EvaluateAlert(true, ThreatLevel.High, false);
            so.Reset();
            Assert.IsFalse(so.IsCritical,
                "IsCritical must be false after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlAlertSystemController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlAlertSystemController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlAlertSystemController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onHighPressure", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onHighPressure must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullAlertSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlAlertSystemController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when AlertSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsAlertSO()
        {
            var go   = new GameObject("Test_HandleMatchStarted");
            var ctrl = go.AddComponent<ZoneControlAlertSystemController>();
            var so   = CreateAlertSO();

            so.EvaluateAlert(true, ThreatLevel.High, false); // set critical
            SetField(ctrl, "_alertSO", so);

            ctrl.HandleMatchStarted();

            Assert.IsFalse(so.IsCritical,
                "HandleMatchStarted must reset the alert SO (clearing IsCritical).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}
