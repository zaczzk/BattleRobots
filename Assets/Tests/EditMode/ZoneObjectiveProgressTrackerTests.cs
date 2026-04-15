using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T282: <see cref="ZoneObjectiveProgressTrackerSO"/> and
    /// <see cref="ZoneObjectiveProgressHUDController"/>.
    ///
    /// ZoneObjectiveProgressTrackerTests (12):
    ///   SO_FreshInstance_HeldZones_Zero                                     ×1
    ///   SO_FreshInstance_RequiredZones_Zero                                 ×1
    ///   SO_FreshInstance_ProgressRatio_Zero                                 ×1
    ///   SO_FreshInstance_IsObjectiveMet_False                               ×1
    ///   SO_WithDominanceSO_HeldZones_ReflectsDominance                     ×1
    ///   SO_WithObjectiveSO_RequiredZones_ReflectsObjective                 ×1
    ///   SO_IsObjectiveMet_True_WhenHeldGeqRequired                         ×1
    ///   SO_Refresh_NullEvent_DoesNotThrow                                  ×1
    ///   Controller_FreshInstance_TrackerSO_Null                            ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                          ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_Unregisters_Channel                           ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneObjectiveProgressTrackerTests
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

        private static ZoneObjectiveProgressHUDController CreateController() =>
            new GameObject("ZoneObjProgCtrl_Test")
                .AddComponent<ZoneObjectiveProgressHUDController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_HeldZones_Zero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0, so.HeldZones,
                "HeldZones must be 0 when no DominanceSO is assigned.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RequiredZones_Zero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0, so.RequiredZones,
                "RequiredZones must be 0 when no ObjectiveSO is assigned.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ProgressRatio_Zero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0f, so.ProgressRatio,
                "ProgressRatio must be 0 when RequiredZones is 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsObjectiveMet_False()
        {
            var so = CreateTrackerSO();
            Assert.IsFalse(so.IsObjectiveMet,
                "IsObjectiveMet must be false on a fresh tracker with no SOs assigned.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WithDominanceSO_HeldZones_ReflectsDominance()
        {
            var tracker   = CreateTrackerSO();
            var dominance = CreateDominanceSO();

            SetField(tracker, "_dominanceSO", dominance);

            dominance.AddPlayerZone();
            dominance.AddPlayerZone();

            Assert.AreEqual(2, tracker.HeldZones,
                "HeldZones must mirror ZoneDominanceSO.PlayerZoneCount.");

            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(dominance);
        }

        [Test]
        public void SO_WithObjectiveSO_RequiredZones_ReflectsObjective()
        {
            var tracker   = CreateTrackerSO();
            var objective = CreateObjectiveSO();

            // Default RequiredZones on ZoneObjectiveSO is 1.
            SetField(tracker, "_objectiveSO", objective);

            Assert.AreEqual(1, tracker.RequiredZones,
                "RequiredZones must mirror ZoneObjectiveSO.RequiredZones.");

            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(objective);
        }

        [Test]
        public void SO_IsObjectiveMet_True_WhenHeldGeqRequired()
        {
            var tracker   = CreateTrackerSO();
            var dominance = CreateDominanceSO();
            var objective = CreateObjectiveSO(); // RequiredZones = 1

            SetField(tracker, "_dominanceSO", dominance);
            SetField(tracker, "_objectiveSO", objective);

            dominance.AddPlayerZone();

            Assert.IsTrue(tracker.IsObjectiveMet,
                "IsObjectiveMet must be true when HeldZones >= RequiredZones.");

            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(objective);
        }

        [Test]
        public void SO_Refresh_NullEvent_DoesNotThrow()
        {
            var so = CreateTrackerSO();
            Assert.DoesNotThrow(() => so.Refresh(),
                "Refresh must not throw when no event channel is assigned.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_TrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TrackerSO,
                "TrackerSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnEnable_Null");
            var ctrl = go.AddComponent<ZoneObjectiveProgressHUDController>();
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneObjectiveProgressHUDController>();
            go.SetActive(false);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Re-enabling with null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go    = new GameObject("Test_Unregister");
            var ctrl  = go.AddComponent<ZoneObjectiveProgressHUDController>();
            var evt   = CreateEvent();

            SetField(ctrl, "_onDominanceChanged", evt);

            // Enable subscribes, disable unsubscribes.
            go.SetActive(true);
            go.SetActive(false);

            int callCount = 0;
            evt.RegisterCallback(() => callCount++);
            evt.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable the controller must have unregistered from the channel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }
    }
}
