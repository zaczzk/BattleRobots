using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T249: <see cref="ArenaHazardResetController"/>.
    ///
    /// ArenaHazardResetControllerTests (14):
    ///   FreshInstance_HazardsNull                                    ×1
    ///   FreshInstance_GroupsNull                                     ×1
    ///   OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   OnDisable_Unregisters_MatchStarted                           ×1
    ///   HandleMatchStarted_SetsAllHazards_Inactive                   ×1
    ///   HandleMatchStarted_ResetsAllGroups                           ×1
    ///   HandleMatchStarted_NullHazardsArray_NoThrow                  ×1
    ///   HandleMatchStarted_NullGroupsArray_NoThrow                   ×1
    ///   HandleMatchStarted_NullHazardEntry_Skipped                   ×1
    ///   HandleMatchStarted_NullGroupEntry_Skipped                    ×1
    ///   OnEnable_NullMatchStarted_DoesNotThrow                       ×1
    ///   HandleMatchStarted_EmptyArrays_NoThrow                       ×1
    ///   HandleMatchStarted_ViaEvent_WhenSubscribed                   ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ArenaHazardResetControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static ArenaHazardResetController CreateController() =>
            new GameObject("HazardReset_Test").AddComponent<ArenaHazardResetController>();

        private static HazardZoneController CreateHazardController() =>
            new GameObject("HazardZone_Test").AddComponent<HazardZoneController>();

        private static HazardZoneGroupSO CreateGroupSO() =>
            ScriptableObject.CreateInstance<HazardZoneGroupSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── Fresh-instance tests ──────────────────────────────────────────────

        [Test]
        public void FreshInstance_HazardsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Hazards,
                "Hazards must be null on a fresh ArenaHazardResetController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_GroupsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Groups,
                "Groups must be null on a fresh ArenaHazardResetController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle tests ───────────────────────────────────────────────────

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_MatchStarted()
        {
            var ctrl     = CreateController();
            var startEvt = CreateEvent();
            SetField(ctrl, "_onMatchStarted", startEvt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // After unsubscribe the controller's handler must not fire.
            // We test by setting up hazards and checking they are NOT deactivated.
            var hazard = CreateHazardController();
            hazard.IsActive = true;
            SetField(ctrl, "_hazards", new HazardZoneController[] { hazard });

            startEvt.Raise();   // should not trigger HandleMatchStarted

            Assert.IsTrue(hazard.IsActive,
                "After OnDisable, raising _onMatchStarted must NOT call HandleMatchStarted.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
            Object.DestroyImmediate(startEvt);
        }

        // ── HandleMatchStarted tests ──────────────────────────────────────────

        [Test]
        public void HandleMatchStarted_SetsAllHazards_Inactive()
        {
            var ctrl   = CreateController();
            var hazard = CreateHazardController();
            hazard.IsActive = true;
            SetField(ctrl, "_hazards", new HazardZoneController[] { hazard });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsFalse(hazard.IsActive,
                "HandleMatchStarted must set IsActive=false on all managed hazards.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
        }

        [Test]
        public void HandleMatchStarted_ResetsAllGroups()
        {
            var ctrl  = CreateController();
            var group = CreateGroupSO();
            group.Activate();   // mark as active before reset
            SetField(ctrl, "_groups", new HazardZoneGroupSO[] { group });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsFalse(group.IsGroupActive,
                "HandleMatchStarted must call Reset() on all managed groups (IsGroupActive→false).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void HandleMatchStarted_NullHazardsArray_NoThrow()
        {
            var ctrl = CreateController();
            // _hazards stays null.
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleMatchStarted(),
                "HandleMatchStarted with null hazards array must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchStarted_NullGroupsArray_NoThrow()
        {
            var ctrl = CreateController();
            // _groups stays null.
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleMatchStarted(),
                "HandleMatchStarted with null groups array must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchStarted_NullHazardEntry_Skipped()
        {
            var ctrl = CreateController();
            SetField(ctrl, "_hazards", new HazardZoneController[] { null });
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleMatchStarted(),
                "A null entry in the hazards array must be skipped without throwing.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchStarted_NullGroupEntry_Skipped()
        {
            var ctrl = CreateController();
            SetField(ctrl, "_groups", new HazardZoneGroupSO[] { null });
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleMatchStarted(),
                "A null entry in the groups array must be skipped without throwing.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullMatchStarted_DoesNotThrow()
        {
            var ctrl = CreateController();
            // _onMatchStarted stays null.
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null _onMatchStarted must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchStarted_EmptyArrays_NoThrow()
        {
            var ctrl = CreateController();
            SetField(ctrl, "_hazards", new HazardZoneController[0]);
            SetField(ctrl, "_groups",  new HazardZoneGroupSO[0]);
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleMatchStarted(),
                "HandleMatchStarted with empty arrays must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchStarted_ViaEvent_WhenSubscribed()
        {
            var ctrl     = CreateController();
            var startEvt = CreateEvent();
            var hazard   = CreateHazardController();
            hazard.IsActive = true;

            SetField(ctrl, "_onMatchStarted", startEvt);
            SetField(ctrl, "_hazards", new HazardZoneController[] { hazard });

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            startEvt.Raise();   // should trigger HandleMatchStarted via delegate

            Assert.IsFalse(hazard.IsActive,
                "Raising _onMatchStarted must trigger HandleMatchStarted and deactivate hazards.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
            Object.DestroyImmediate(startEvt);
        }
    }
}
