using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T287:
    /// <see cref="ZoneObjectiveProgressNotificationController"/>.
    ///
    /// ZoneObjectiveProgressNotificationTests (12):
    ///   FreshInstance_TrackerSO_Null                                         ×1
    ///   FreshInstance_IsActive_False                                         ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                       ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                      ×1
    ///   OnDisable_Unregisters_Channel                                        ×1
    ///   HandleProgressUpdated_NullTracker_NoThrow                            ×1
    ///   HandleProgressUpdated_ObjectiveNotMet_NoShow                         ×1
    ///   HandleProgressUpdated_ObjectiveMet_ShowsBanner                       ×1
    ///   HandleProgressUpdated_AlreadyMet_NoReshow                            ×1
    ///   Tick_NotActive_NoOp                                                  ×1
    ///   Tick_BelowDuration_StaysActive                                       ×1
    ///   Tick_ExceedsDuration_HidesBanner                                     ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneObjectiveProgressNotificationTests
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

        private static ZoneObjectiveProgressTrackerSO CreateTrackerSO() =>
            ScriptableObject.CreateInstance<ZoneObjectiveProgressTrackerSO>();

        private static ZoneDominanceSO CreateDominanceSO() =>
            ScriptableObject.CreateInstance<ZoneDominanceSO>();

        private static ZoneObjectiveSO CreateObjectiveSO() =>
            ScriptableObject.CreateInstance<ZoneObjectiveSO>();

        private static ZoneObjectiveProgressNotificationController CreateController() =>
            new GameObject("ZoneObjNotif_Test")
                .AddComponent<ZoneObjectiveProgressNotificationController>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_TrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TrackerSO,
                "TrackerSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_IsActive_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsActive,
                "IsActive must be false on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(() => go.AddComponent<ZoneObjectiveProgressNotificationController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneObjectiveProgressNotificationController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneObjectiveProgressNotificationController>();
            var evt  = CreateEvent();

            SetField(ctrl, "_onProgressUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable the controller must have unregistered from the channel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void HandleProgressUpdated_NullTracker_NoThrow()
        {
            var ctrl = CreateController();
            // _trackerSO is null
            Assert.DoesNotThrow(() => ctrl.HandleProgressUpdated(),
                "HandleProgressUpdated must not throw when TrackerSO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleProgressUpdated_ObjectiveNotMet_NoShow()
        {
            var go        = new GameObject("Test_NotMet");
            var ctrl      = go.AddComponent<ZoneObjectiveProgressNotificationController>();
            var tracker   = CreateTrackerSO();
            var panel     = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_trackerSO", tracker);
            SetField(ctrl, "_panel",     panel);

            // No DominanceSO/ObjectiveSO → IsObjectiveMet == false
            ctrl.HandleProgressUpdated();

            Assert.IsFalse(ctrl.IsActive,
                "IsActive must remain false when the objective is not met.");
            Assert.IsFalse(panel.activeSelf,
                "Panel must remain hidden when the objective is not met.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void HandleProgressUpdated_ObjectiveMet_ShowsBanner()
        {
            var go        = new GameObject("Test_ObjMet");
            var ctrl      = go.AddComponent<ZoneObjectiveProgressNotificationController>();
            var tracker   = CreateTrackerSO();
            var dominance = CreateDominanceSO();
            var objective = CreateObjectiveSO(); // RequiredZones = 1
            var panel     = new GameObject("Panel");
            panel.SetActive(false);

            SetField(tracker, "_dominanceSO", dominance);
            SetField(tracker, "_objectiveSO", objective);
            dominance.AddPlayerZone(); // IsObjectiveMet = true

            SetField(ctrl, "_trackerSO", tracker);
            SetField(ctrl, "_panel",     panel);

            ctrl.HandleProgressUpdated();

            Assert.IsTrue(ctrl.IsActive,
                "IsActive must be true after objective is first met.");
            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when the objective transitions to met.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(objective);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void HandleProgressUpdated_AlreadyMet_NoReshow()
        {
            var go        = new GameObject("Test_AlreadyMet");
            var ctrl      = go.AddComponent<ZoneObjectiveProgressNotificationController>();
            var tracker   = CreateTrackerSO();
            var dominance = CreateDominanceSO();
            var objective = CreateObjectiveSO(); // RequiredZones = 1

            SetField(tracker, "_dominanceSO", dominance);
            SetField(tracker, "_objectiveSO", objective);
            dominance.AddPlayerZone();

            SetField(ctrl, "_trackerSO", tracker);

            // First call: shows banner.
            ctrl.HandleProgressUpdated();
            Assert.IsTrue(ctrl.IsActive, "Banner must be active after first call.");

            // Manually expire the timer.
            ctrl.Tick(100f);
            Assert.IsFalse(ctrl.IsActive, "Banner must be hidden after timer expires.");

            // Second call while still met: must NOT re-show.
            ctrl.HandleProgressUpdated();
            Assert.IsFalse(ctrl.IsActive,
                "Banner must not re-show when objective was already met.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(objective);
        }

        [Test]
        public void Tick_NotActive_NoOp()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.Tick(1f),
                "Tick must not throw when banner is not active.");
            Assert.IsFalse(ctrl.IsActive,
                "IsActive must remain false after Tick when banner was never shown.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Tick_BelowDuration_StaysActive()
        {
            var go      = new GameObject("Test_Tick_Below");
            var ctrl    = go.AddComponent<ZoneObjectiveProgressNotificationController>();
            var tracker = CreateTrackerSO();
            var dominance = CreateDominanceSO();
            var objective = CreateObjectiveSO();

            SetField(tracker, "_dominanceSO", dominance);
            SetField(tracker, "_objectiveSO", objective);
            dominance.AddPlayerZone();
            SetField(ctrl, "_trackerSO", tracker);

            ctrl.HandleProgressUpdated(); // shows banner (duration=2.5s)
            ctrl.Tick(1f);               // 1s elapsed, 1.5s remaining

            Assert.IsTrue(ctrl.IsActive,
                "Banner must remain active before display duration elapses.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(objective);
        }

        [Test]
        public void Tick_ExceedsDuration_HidesBanner()
        {
            var go      = new GameObject("Test_Tick_Expire");
            var ctrl    = go.AddComponent<ZoneObjectiveProgressNotificationController>();
            var tracker = CreateTrackerSO();
            var dominance = CreateDominanceSO();
            var objective = CreateObjectiveSO();
            var panel   = new GameObject("Panel");

            SetField(tracker, "_dominanceSO", dominance);
            SetField(tracker, "_objectiveSO", objective);
            dominance.AddPlayerZone();
            SetField(ctrl, "_trackerSO", tracker);
            SetField(ctrl, "_panel",     panel);

            ctrl.HandleProgressUpdated(); // shows banner
            ctrl.Tick(100f);              // far beyond duration

            Assert.IsFalse(ctrl.IsActive,
                "Banner must be hidden after display duration elapses.");
            Assert.IsFalse(panel.activeSelf,
                "Panel must be deactivated after display duration elapses.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(objective);
            Object.DestroyImmediate(panel);
        }
    }
}
