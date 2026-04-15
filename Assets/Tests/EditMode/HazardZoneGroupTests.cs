using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T239: <see cref="HazardZoneGroupSO"/> and
    /// <see cref="HazardZoneGroupController"/>.
    ///
    /// HazardZoneGroupTests (16):
    ///   SO_FreshInstance_IsGroupActive_False                    ×1
    ///   SO_Activate_SetsIsGroupActive_True                      ×1
    ///   SO_Deactivate_SetsIsGroupActive_False                   ×1
    ///   SO_Toggle_ActivatesWhenInactive                         ×1
    ///   SO_Toggle_DeactivatesWhenActive                         ×1
    ///   SO_Activate_FiresOnGroupActivated                       ×1
    ///   SO_Deactivate_FiresOnGroupDeactivated                   ×1
    ///   SO_Reset_SilentAndSetsInactive                         ×1
    ///   Controller_FreshInstance_GroupNull                      ×1
    ///   Controller_FreshInstance_IsGroupActive_False            ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow              ×1
    ///   Controller_OnDisable_Unregisters_Channels               ×1
    ///   Controller_HandleActivate_SetsAllHazardsActive          ×1
    ///   Controller_HandleDeactivate_SetsAllHazardsInactive      ×1
    ///   Controller_Activate_NullGroup_DoesNotThrow              ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class HazardZoneGroupTests
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

        private static HazardZoneGroupSO CreateGroupSO() =>
            ScriptableObject.CreateInstance<HazardZoneGroupSO>();

        private static HazardZoneGroupController CreateController() =>
            new GameObject("GroupCtrl_Test").AddComponent<HazardZoneGroupController>();

        private static HazardZoneController CreateHazardZone() =>
            new GameObject("HazardZone_Test").AddComponent<HazardZoneController>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsGroupActive_False()
        {
            var so = CreateGroupSO();
            Assert.IsFalse(so.IsGroupActive,
                "IsGroupActive must be false on a fresh HazardZoneGroupSO instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Activate_SetsIsGroupActive_True()
        {
            var so = CreateGroupSO();
            so.Activate();
            Assert.IsTrue(so.IsGroupActive,
                "IsGroupActive must be true after Activate().");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Deactivate_SetsIsGroupActive_False()
        {
            var so = CreateGroupSO();
            so.Activate();
            so.Deactivate();
            Assert.IsFalse(so.IsGroupActive,
                "IsGroupActive must be false after Deactivate().");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Toggle_ActivatesWhenInactive()
        {
            var so = CreateGroupSO();
            so.Toggle();
            Assert.IsTrue(so.IsGroupActive,
                "Toggle() on an inactive group must activate it.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Toggle_DeactivatesWhenActive()
        {
            var so = CreateGroupSO();
            so.Activate();
            so.Toggle();
            Assert.IsFalse(so.IsGroupActive,
                "Toggle() on an active group must deactivate it.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Activate_FiresOnGroupActivated()
        {
            var so  = CreateGroupSO();
            var evt = CreateEvent();
            SetField(so, "_onGroupActivated", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);
            so.Activate();

            Assert.AreEqual(1, count,
                "_onGroupActivated must fire exactly once on Activate().");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Deactivate_FiresOnGroupDeactivated()
        {
            var so  = CreateGroupSO();
            var evt = CreateEvent();
            SetField(so, "_onGroupDeactivated", evt);
            so.Activate();

            int count = 0;
            evt.RegisterCallback(() => count++);
            so.Deactivate();

            Assert.AreEqual(1, count,
                "_onGroupDeactivated must fire exactly once on Deactivate().");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_SilentAndSetsInactive()
        {
            var so  = CreateGroupSO();
            var evt = CreateEvent();
            SetField(so, "_onGroupDeactivated", evt);
            so.Activate();

            int count = 0;
            evt.RegisterCallback(() => count++);
            so.Reset();

            Assert.IsFalse(so.IsGroupActive,
                "IsGroupActive must be false after Reset().");
            Assert.AreEqual(0, count,
                "Reset() must not fire _onGroupDeactivated.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_GroupNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Group,
                "Group must be null on a fresh HazardZoneGroupController instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_IsGroupActive_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsGroupActive,
                "IsGroupActive must be false when Group is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null Group must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null Group must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var ctrl    = CreateController();
            var so      = CreateGroupSO();
            var actEvt  = CreateEvent();
            var deactEvt = CreateEvent();

            SetField(so, "_onGroupActivated",   actEvt);
            SetField(so, "_onGroupDeactivated", deactEvt);
            SetField(ctrl, "_group", so);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int actCount = 0, deactCount = 0;
            actEvt.RegisterCallback(() => actCount++);
            deactEvt.RegisterCallback(() => deactCount++);

            so.Activate();
            so.Deactivate();

            Assert.AreEqual(1, actCount,
                "After OnDisable, only external callbacks fire on _onGroupActivated.");
            Assert.AreEqual(1, deactCount,
                "After OnDisable, only external callbacks fire on _onGroupDeactivated.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(actEvt);
            Object.DestroyImmediate(deactEvt);
        }

        [Test]
        public void Controller_HandleActivate_SetsAllHazardsActive()
        {
            var ctrl   = CreateController();
            var hazardA = CreateHazardZone();
            var hazardB = CreateHazardZone();
            hazardA.IsActive = false;
            hazardB.IsActive = false;

            SetField(ctrl, "_hazards", new HazardZoneController[] { hazardA, hazardB });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleActivate();

            Assert.IsTrue(hazardA.IsActive, "hazardA.IsActive must be true after HandleActivate.");
            Assert.IsTrue(hazardB.IsActive, "hazardB.IsActive must be true after HandleActivate.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazardA.gameObject);
            Object.DestroyImmediate(hazardB.gameObject);
        }

        [Test]
        public void Controller_HandleDeactivate_SetsAllHazardsInactive()
        {
            var ctrl   = CreateController();
            var hazardA = CreateHazardZone();
            var hazardB = CreateHazardZone();
            hazardA.IsActive = true;
            hazardB.IsActive = true;

            SetField(ctrl, "_hazards", new HazardZoneController[] { hazardA, hazardB });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleDeactivate();

            Assert.IsFalse(hazardA.IsActive, "hazardA.IsActive must be false after HandleDeactivate.");
            Assert.IsFalse(hazardB.IsActive, "hazardB.IsActive must be false after HandleDeactivate.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazardA.gameObject);
            Object.DestroyImmediate(hazardB.gameObject);
        }

        [Test]
        public void Controller_Activate_NullGroup_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Activate(),
                "Activate() with null Group must not throw.");
            Assert.DoesNotThrow(() => ctrl.Deactivate(),
                "Deactivate() with null Group must not throw.");
            Assert.DoesNotThrow(() => ctrl.Toggle(),
                "Toggle() with null Group must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
