using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T256: <see cref="ZoneDominanceSO"/> and
    /// <see cref="ZoneDominanceController"/>.
    ///
    /// ZoneDominanceTests (14):
    ///   SO_FreshInstance_PlayerZoneCount_Zero                           ×1
    ///   SO_FreshInstance_TotalZones_Default_Three                       ×1
    ///   SO_FreshInstance_DominanceRatio_Zero                            ×1
    ///   SO_AddPlayerZone_Increments_PlayerZoneCount                     ×1
    ///   SO_RemovePlayerZone_Decrements_PlayerZoneCount                  ×1
    ///   SO_Reset_ZerosCount_FiresEvent                                  ×1
    ///   Controller_FreshInstance_DominanceSO_Null                       ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                       ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_Unregisters_MatchChannels                  ×1
    ///   Controller_OnDisable_Unregisters_ZoneChannels                   ×1
    ///   Controller_HandleMatchStarted_ResetsDominance                   ×1
    ///   Controller_HandleZoneCaptured_CallsAddPlayerZone                ×1
    ///   Controller_HandleZoneLost_CallsRemovePlayerZone                 ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneDominanceTests
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

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneDominanceSO CreateDominanceSO() =>
            ScriptableObject.CreateInstance<ZoneDominanceSO>();

        private static ZoneDominanceController CreateController() =>
            new GameObject("ZoneDomCtrl_Test").AddComponent<ZoneDominanceController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_PlayerZoneCount_Zero()
        {
            var so = CreateDominanceSO();
            Assert.AreEqual(0, so.PlayerZoneCount,
                "PlayerZoneCount must be 0 on a fresh ZoneDominanceSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalZones_Default_Three()
        {
            var so = CreateDominanceSO();
            Assert.AreEqual(3, so.TotalZones,
                "TotalZones must default to 3.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DominanceRatio_Zero()
        {
            var so = CreateDominanceSO();
            Assert.AreEqual(0f, so.DominanceRatio, 0.001f,
                "DominanceRatio must be 0 when PlayerZoneCount is 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddPlayerZone_Increments_PlayerZoneCount()
        {
            var so = CreateDominanceSO();
            so.AddPlayerZone();
            Assert.AreEqual(1, so.PlayerZoneCount,
                "AddPlayerZone must increment PlayerZoneCount by one.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RemovePlayerZone_Decrements_PlayerZoneCount()
        {
            var so = CreateDominanceSO();
            so.AddPlayerZone();
            so.AddPlayerZone();
            so.RemovePlayerZone();
            Assert.AreEqual(1, so.PlayerZoneCount,
                "RemovePlayerZone must decrement PlayerZoneCount by one.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ZerosCount_FiresEvent()
        {
            var so     = CreateDominanceSO();
            var evt    = CreateEvent();
            int called = 0;
            SetField(so, "_onDominanceChanged", evt);
            evt.RegisterCallback(() => called++);

            so.AddPlayerZone();
            so.AddPlayerZone();
            Assert.AreEqual(2, so.PlayerZoneCount, "Pre-condition: two zones added.");

            called = 0;
            so.Reset();

            Assert.AreEqual(0, so.PlayerZoneCount,
                "Reset must zero PlayerZoneCount.");
            Assert.AreEqual(1, called,
                "_onDominanceChanged must fire once during Reset.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_DominanceSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.DominanceSO,
                "DominanceSO must be null on a fresh ZoneDominanceController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_MatchChannels()
        {
            var ctrl  = CreateController();
            var so    = CreateDominanceSO();
            var start = CreateEvent();
            var end   = CreateEvent();
            SetField(ctrl, "_dominanceSO",    so);
            SetField(ctrl, "_onMatchStarted", start);
            SetField(ctrl, "_onMatchEnded",   end);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Add two zones so reset (triggered by start event) will be detectable.
            so.AddPlayerZone();
            so.AddPlayerZone();
            Assert.AreEqual(2, so.PlayerZoneCount, "Pre-condition: 2 zones added.");

            InvokePrivate(ctrl, "OnDisable");

            // Raising match-started must NOT reset dominance now.
            start.Raise();
            Assert.AreEqual(2, so.PlayerZoneCount,
                "After OnDisable, _onMatchStarted must not reset dominance.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(start);
            Object.DestroyImmediate(end);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_ZoneChannels()
        {
            var ctrl     = CreateController();
            var so       = CreateDominanceSO();
            var captured = CreateEvent();
            var lost     = CreateEvent();
            SetField(ctrl, "_dominanceSO",          so);
            SetField(ctrl, "_onPlayerZoneCaptured", captured);
            SetField(ctrl, "_onPlayerZoneLost",     lost);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // Raising zone events must not modify count after disable.
            captured.Raise();
            Assert.AreEqual(0, so.PlayerZoneCount,
                "After OnDisable, _onPlayerZoneCaptured must not add a zone.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(captured);
            Object.DestroyImmediate(lost);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsDominance()
        {
            var ctrl = CreateController();
            var so   = CreateDominanceSO();
            SetField(ctrl, "_dominanceSO", so);
            InvokePrivate(ctrl, "Awake");

            so.AddPlayerZone();
            Assert.AreEqual(1, so.PlayerZoneCount, "Pre-condition: one zone added.");

            ctrl.HandleMatchStarted();

            Assert.AreEqual(0, so.PlayerZoneCount,
                "HandleMatchStarted must reset the dominance count to 0.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleZoneCaptured_CallsAddPlayerZone()
        {
            var ctrl = CreateController();
            var so   = CreateDominanceSO();
            SetField(ctrl, "_dominanceSO", so);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleZoneCaptured();

            Assert.AreEqual(1, so.PlayerZoneCount,
                "HandleZoneCaptured must call AddPlayerZone on the dominance SO.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleZoneLost_CallsRemovePlayerZone()
        {
            var ctrl = CreateController();
            var so   = CreateDominanceSO();
            SetField(ctrl, "_dominanceSO", so);
            InvokePrivate(ctrl, "Awake");

            so.AddPlayerZone();
            so.AddPlayerZone();
            Assert.AreEqual(2, so.PlayerZoneCount, "Pre-condition: two zones added.");

            ctrl.HandleZoneLost();

            Assert.AreEqual(1, so.PlayerZoneCount,
                "HandleZoneLost must call RemovePlayerZone on the dominance SO.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }
    }
}
