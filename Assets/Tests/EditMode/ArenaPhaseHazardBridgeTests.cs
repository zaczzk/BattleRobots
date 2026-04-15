using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T247: <see cref="ArenaPhaseHazardBridgeSO"/> and
    /// <see cref="ArenaPhaseHazardBridgeController"/>.
    ///
    /// ArenaPhaseHazardBridgeTests (14):
    ///   SO_FreshInstance_EntryCount_Zero                             ×1
    ///   SO_GetGroup_ReturnsMatchingGroup                             ×1
    ///   SO_GetGroup_NoMatch_ReturnsNull                              ×1
    ///   SO_GetGroup_NullEvent_ReturnsNull                            ×1
    ///   SO_GetEntry_OutOfRange_ReturnsDefault                        ×1
    ///   Controller_FreshInstance_ConfigNull                          ×1
    ///   Controller_FreshInstance_EntryCount_Zero                     ×1
    ///   Controller_OnEnable_NullConfig_DoesNotThrow                  ×1
    ///   Controller_OnDisable_NullConfig_DoesNotThrow                 ×1
    ///   Controller_OnDisable_Unregisters_AllChannels                 ×1
    ///   Controller_ActivateGroup_ActivatesMatchingGroup              ×1
    ///   Controller_ActivateGroup_DeactivatesOthers                   ×1
    ///   Controller_ActivateGroup_NullConfig_NoThrow                  ×1
    ///   Controller_ActivateGroup_NullGroupEntry_NoThrow              ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ArenaPhaseHazardBridgeTests
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

        private static ArenaPhaseHazardBridgeSO CreateBridgeSO() =>
            ScriptableObject.CreateInstance<ArenaPhaseHazardBridgeSO>();

        private static HazardZoneGroupSO CreateGroupSO() =>
            ScriptableObject.CreateInstance<HazardZoneGroupSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ArenaPhaseHazardBridgeController CreateController() =>
            new GameObject("BridgeCtrl_Test").AddComponent<ArenaPhaseHazardBridgeController>();

        /// <summary>Builds a bridge SO with one entry.</summary>
        private static ArenaPhaseHazardBridgeSO BuildBridgeSO(
            VoidGameEvent evt, HazardZoneGroupSO group)
        {
            var so    = CreateBridgeSO();
            var entry = new ArenaPhaseHazardBridgeEntry { phaseEvent = evt, group = group };
            SetField(so, "_entries", new ArenaPhaseHazardBridgeEntry[] { entry });
            return so;
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EntryCount_Zero()
        {
            var so = CreateBridgeSO();
            Assert.AreEqual(0, so.EntryCount,
                "EntryCount must be 0 on a fresh instance with no entries assigned.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetGroup_ReturnsMatchingGroup()
        {
            var evt   = CreateEvent();
            var group = CreateGroupSO();
            var so    = BuildBridgeSO(evt, group);

            Assert.AreEqual(group, so.GetGroup(evt),
                "GetGroup must return the paired HazardZoneGroupSO for a matching event.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void SO_GetGroup_NoMatch_ReturnsNull()
        {
            var evt1   = CreateEvent();
            var evt2   = CreateEvent();
            var group  = CreateGroupSO();
            var so     = BuildBridgeSO(evt1, group);

            Assert.IsNull(so.GetGroup(evt2),
                "GetGroup must return null when no entry matches the given event.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt1);
            Object.DestroyImmediate(evt2);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void SO_GetGroup_NullEvent_ReturnsNull()
        {
            var group = CreateGroupSO();
            var evt   = CreateEvent();
            var so    = BuildBridgeSO(evt, group);

            Assert.IsNull(so.GetGroup(null),
                "GetGroup(null) must return null.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void SO_GetEntry_OutOfRange_ReturnsDefault()
        {
            var so = CreateBridgeSO();
            // No entries → any index is out of range.
            ArenaPhaseHazardBridgeEntry entry = so.GetEntry(0);
            Assert.IsNull(entry.phaseEvent, "Out-of-range GetEntry phaseEvent must be null.");
            Assert.IsNull(entry.group,      "Out-of-range GetEntry group must be null.");
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ConfigNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Config,
                "Config must be null on a fresh ArenaPhaseHazardBridgeController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_EntryCount_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0, ctrl.EntryCount,
                "EntryCount must be 0 when Config is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullConfig_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null config must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullConfig_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null config must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_AllChannels()
        {
            var ctrl  = CreateController();
            var evt   = CreateEvent();
            var group = CreateGroupSO();
            var so    = BuildBridgeSO(evt, group);

            SetField(ctrl, "_config", so);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // Raise the event — only external callbacks should fire (0 from controller).
            int externalCount = 0;
            evt.RegisterCallback(() => externalCount++);
            evt.Raise();

            Assert.AreEqual(1, externalCount,
                "Only the external callback must fire after OnDisable; controller must be unregistered.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Controller_ActivateGroup_ActivatesMatchingGroup()
        {
            var ctrl  = CreateController();
            var evt   = CreateEvent();
            var group = CreateGroupSO();
            var so    = BuildBridgeSO(evt, group);

            SetField(ctrl, "_config", so);
            InvokePrivate(ctrl, "Awake");

            ctrl.ActivateGroup(0);

            Assert.IsTrue(group.IsGroupActive,
                "ActivateGroup(0) must activate the group at entry 0.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Controller_ActivateGroup_DeactivatesOthers()
        {
            var ctrl   = CreateController();
            var evt0   = CreateEvent();
            var evt1   = CreateEvent();
            var group0 = CreateGroupSO();
            var group1 = CreateGroupSO();

            // Pre-activate both groups.
            group0.Activate();
            group1.Activate();

            var so = CreateBridgeSO();
            SetField(so, "_entries", new ArenaPhaseHazardBridgeEntry[]
            {
                new ArenaPhaseHazardBridgeEntry { phaseEvent = evt0, group = group0 },
                new ArenaPhaseHazardBridgeEntry { phaseEvent = evt1, group = group1 },
            });

            SetField(ctrl, "_config", so);
            InvokePrivate(ctrl, "Awake");

            // Activate only group0 — group1 must be deactivated.
            ctrl.ActivateGroup(0);

            Assert.IsTrue(group0.IsGroupActive,  "group0 must be active after ActivateGroup(0).");
            Assert.IsFalse(group1.IsGroupActive, "group1 must be deactivated by ActivateGroup(0).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt0);
            Object.DestroyImmediate(evt1);
            Object.DestroyImmediate(group0);
            Object.DestroyImmediate(group1);
        }

        [Test]
        public void Controller_ActivateGroup_NullConfig_NoThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.ActivateGroup(0),
                "ActivateGroup with null config must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_ActivateGroup_NullGroupEntry_NoThrow()
        {
            var ctrl = CreateController();
            var evt  = CreateEvent();
            var so   = CreateBridgeSO();

            // Entry with null group.
            SetField(so, "_entries", new ArenaPhaseHazardBridgeEntry[]
            {
                new ArenaPhaseHazardBridgeEntry { phaseEvent = evt, group = null },
            });

            SetField(ctrl, "_config", so);
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.ActivateGroup(0),
                "ActivateGroup with a null group entry must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }
    }
}
