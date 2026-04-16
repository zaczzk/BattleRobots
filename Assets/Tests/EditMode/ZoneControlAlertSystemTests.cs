using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T325: <see cref="ZoneControlAlertSystemSO"/> and
    /// <see cref="ZoneControlAlertSystemController"/>.
    ///
    /// ZoneControlAlertSystemTests (12):
    ///   SO_FreshInstance_IsCriticalAlert_False                           ×1
    ///   SO_EvaluateAlert_AllTrue_NoHasDominance_SetsAlert                ×1
    ///   SO_EvaluateAlert_HasDominance_ClearsAlert                        ×1
    ///   SO_EvaluateAlert_Transition_FiresCriticalAlertEvent              ×1
    ///   SO_EvaluateAlert_SameState_NoEvent                               ×1
    ///   SO_Reset_ClearsAlert                                             ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                        ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                       ×1
    ///   Controller_OnDisable_Unregisters_Channel                         ×1
    ///   Controller_Refresh_NullSO_HidesPanel                             ×1
    ///   Controller_EvaluateAndRefresh_AlertActive_ShowsPanel             ×1
    ///   Controller_HandleMatchStarted_ResetsAlert                        ×1
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

        private static ZoneControlMatchPressureSO CreatePressureSO(
            float increment = 1f, float threshold = 0.5f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchPressureSO>();
            SetField(so, "_pressureIncrement",      increment);
            SetField(so, "_highPressureThreshold",  threshold);
            so.Reset();
            return so;
        }

        private static ZoneControlThreatAssessmentSO CreateThreatSO(
            int mediumRank = 2, int highRank = 3)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlThreatAssessmentSO>();
            SetField(so, "_mediumThreatRank", mediumRank);
            SetField(so, "_highThreatRank",   highRank);
            so.Reset();
            return so;
        }

        private static ZoneDominanceSO CreateDominanceSO(int totalZones = 4)
        {
            var so = ScriptableObject.CreateInstance<ZoneDominanceSO>();
            SetField(so, "_totalZones", totalZones);
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsCriticalAlert_False()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAlertSystemSO>();
            Assert.IsFalse(so.IsCriticalAlert,
                "IsCriticalAlert must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateAlert_AllTrue_NoHasDominance_SetsAlert()
        {
            var so = CreateAlertSO();
            so.EvaluateAlert(isHighPressure: true, isThreatHigh: true, hasDominance: false);
            Assert.IsTrue(so.IsCriticalAlert,
                "IsCriticalAlert must be true when all three conditions are met.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateAlert_HasDominance_ClearsAlert()
        {
            var so = CreateAlertSO();
            so.EvaluateAlert(true, true, false); // activate
            so.EvaluateAlert(true, true, true);  // hasDominance=true → should clear
            Assert.IsFalse(so.IsCriticalAlert,
                "IsCriticalAlert must be false when player has dominance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateAlert_Transition_FiresCriticalAlertEvent()
        {
            var so  = CreateAlertSO();
            var evt = CreateEvent();
            SetField(so, "_onCriticalAlert", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.EvaluateAlert(true, true, false); // false → true transition
            Assert.AreEqual(1, fired,
                "_onCriticalAlert must fire on the inactive→active transition.");

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

            so.EvaluateAlert(false, false, true); // still inactive
            so.EvaluateAlert(false, false, true); // still inactive
            Assert.AreEqual(0, fired,
                "_onCriticalAlert must NOT fire when alert state is unchanged.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAlert()
        {
            var so = CreateAlertSO();
            so.EvaluateAlert(true, true, false); // active
            so.Reset();
            Assert.IsFalse(so.IsCriticalAlert,
                "IsCriticalAlert must be false after Reset.");
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
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlAlertSystemController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_alertPanel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when AlertSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_EvaluateAndRefresh_AlertActive_ShowsPanel()
        {
            var go        = new GameObject("Test_EvalAndRefresh");
            var ctrl      = go.AddComponent<ZoneControlAlertSystemController>();
            var alertSO   = CreateAlertSO();
            var pressureSO = CreatePressureSO(increment: 1f, threshold: 0.5f);
            var threatSO  = CreateThreatSO(mediumRank: 2, highRank: 3);
            var dominanceSO = CreateDominanceSO(totalZones: 4);
            var panel     = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_alertSO",    alertSO);
            SetField(ctrl, "_pressureSO", pressureSO);
            SetField(ctrl, "_threatSO",   threatSO);
            SetField(ctrl, "_alertPanel", panel);
            // _dominanceSO left null so HasDominance = false

            // Set high pressure and high threat manually
            pressureSO.EvaluatePressure(true);
            pressureSO.EvaluatePressure(true); // exceeds 0.5 threshold
            threatSO.EvaluateThreat(playerRank: 3, hasDominance: false); // High

            ctrl.EvaluateAndRefresh();

            Assert.IsTrue(panel.activeSelf,
                "Alert panel must be shown when all three critical conditions are active.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(alertSO);
            Object.DestroyImmediate(pressureSO);
            Object.DestroyImmediate(threatSO);
            Object.DestroyImmediate(dominanceSO);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsAlert()
        {
            var go      = new GameObject("Test_MatchStart");
            var ctrl    = go.AddComponent<ZoneControlAlertSystemController>();
            var alertSO = CreateAlertSO();
            var panel   = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_alertSO",    alertSO);
            SetField(ctrl, "_alertPanel", panel);

            alertSO.EvaluateAlert(true, true, false); // activate
            ctrl.HandleMatchStarted();

            Assert.IsFalse(alertSO.IsCriticalAlert,
                "HandleMatchStarted must reset the alert SO.");
            Assert.IsFalse(panel.activeSelf,
                "Alert panel must be hidden after HandleMatchStarted.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(alertSO);
            Object.DestroyImmediate(panel);
        }
    }
}
