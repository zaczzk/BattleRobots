using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T266: <see cref="ZoneControlMatchManager"/>.
    ///
    /// ZoneControlMatchManagerTests (14):
    ///   FreshInstance_CatalogSO_Null                                    ×1
    ///   FreshInstance_DominanceSO_Null                                  ×1
    ///   FreshInstance_ObjectiveSO_Null                                  ×1
    ///   FreshInstance_TimerSO_Null                                      ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_Channels                                  ×1
    ///   HandleMatchStarted_NullRefs_DoesNotThrow                        ×1
    ///   HandleMatchStarted_ResetsDominance                              ×1
    ///   HandleMatchStarted_ResetsObjective                              ×1
    ///   HandleMatchEnded_NullRefs_RaisesMatchLost                       ×1
    ///   HandleMatchEnded_ObjectiveMet_RaisesMatchWon                    ×1
    ///   HandleMatchEnded_ObjectiveNotMet_RaisesMatchLost                ×1
    ///   MatchStarted_Event_TriggersReset                                ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchManagerTests
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

        private static ZoneObjectiveSO CreateObjectiveSO() =>
            ScriptableObject.CreateInstance<ZoneObjectiveSO>();

        private static ZoneTimerSO CreateTimerSO() =>
            ScriptableObject.CreateInstance<ZoneTimerSO>();

        private static ZoneControlMatchManager CreateManager() =>
            new GameObject("ZoneCtrlMgr_Test").AddComponent<ZoneControlMatchManager>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CatalogSO_Null()
        {
            var mgr = CreateManager();
            Assert.IsNull(mgr.CatalogSO,
                "CatalogSO must be null on a fresh ZoneControlMatchManager.");
            Object.DestroyImmediate(mgr.gameObject);
        }

        [Test]
        public void FreshInstance_DominanceSO_Null()
        {
            var mgr = CreateManager();
            Assert.IsNull(mgr.DominanceSO,
                "DominanceSO must be null on a fresh ZoneControlMatchManager.");
            Object.DestroyImmediate(mgr.gameObject);
        }

        [Test]
        public void FreshInstance_ObjectiveSO_Null()
        {
            var mgr = CreateManager();
            Assert.IsNull(mgr.ObjectiveSO,
                "ObjectiveSO must be null on a fresh ZoneControlMatchManager.");
            Object.DestroyImmediate(mgr.gameObject);
        }

        [Test]
        public void FreshInstance_TimerSO_Null()
        {
            var mgr = CreateManager();
            Assert.IsNull(mgr.TimerSO,
                "TimerSO must be null on a fresh ZoneControlMatchManager.");
            Object.DestroyImmediate(mgr.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var mgr = CreateManager();
            InvokePrivate(mgr, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(mgr, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(mgr.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var mgr = CreateManager();
            InvokePrivate(mgr, "Awake");
            InvokePrivate(mgr, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(mgr, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(mgr.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_Channels()
        {
            var mgr          = CreateManager();
            var domSO        = CreateDominanceSO();
            var evtStarted   = CreateEvent();

            SetField(mgr, "_dominanceSO",    domSO);
            SetField(mgr, "_onMatchStarted", evtStarted);

            // Set zone count so we can detect if reset fires.
            domSO.AddPlayerZone();
            Assert.AreEqual(1, domSO.PlayerZoneCount, "Pre-condition: zone count should be 1.");

            InvokePrivate(mgr, "Awake");
            InvokePrivate(mgr, "OnEnable");
            InvokePrivate(mgr, "OnDisable");

            // Raising event after disable must NOT reset dominance.
            domSO.AddPlayerZone();   // count = 2
            evtStarted.Raise();      // should not reset
            Assert.AreEqual(2, domSO.PlayerZoneCount,
                "After OnDisable, _onMatchStarted must not trigger HandleMatchStarted.");

            Object.DestroyImmediate(mgr.gameObject);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(evtStarted);
        }

        [Test]
        public void HandleMatchStarted_NullRefs_DoesNotThrow()
        {
            var mgr = CreateManager();
            InvokePrivate(mgr, "Awake");
            Assert.DoesNotThrow(() => mgr.HandleMatchStarted(),
                "HandleMatchStarted with all null refs must not throw.");
            Object.DestroyImmediate(mgr.gameObject);
        }

        [Test]
        public void HandleMatchStarted_ResetsDominance()
        {
            var mgr   = CreateManager();
            var domSO = CreateDominanceSO();

            SetField(mgr, "_dominanceSO", domSO);

            domSO.AddPlayerZone();
            Assert.AreEqual(1, domSO.PlayerZoneCount, "Pre-condition: zone count should be 1.");

            InvokePrivate(mgr, "Awake");
            mgr.HandleMatchStarted();

            Assert.AreEqual(0, domSO.PlayerZoneCount,
                "HandleMatchStarted must reset player zone count to 0.");

            Object.DestroyImmediate(mgr.gameObject);
            Object.DestroyImmediate(domSO);
        }

        [Test]
        public void HandleMatchStarted_ResetsObjective()
        {
            var mgr   = CreateManager();
            var domSO = CreateDominanceSO();
            var objSO = CreateObjectiveSO();

            SetField(mgr, "_dominanceSO", domSO);
            SetField(mgr, "_objectiveSO", objSO);

            domSO.AddPlayerZone();
            objSO.Evaluate(1);
            Assert.IsTrue(objSO.IsComplete, "Pre-condition: objective must be complete.");

            InvokePrivate(mgr, "Awake");
            mgr.HandleMatchStarted();

            Assert.IsFalse(objSO.IsComplete,
                "HandleMatchStarted must reset ZoneObjectiveSO.IsComplete to false.");

            Object.DestroyImmediate(mgr.gameObject);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(objSO);
        }

        [Test]
        public void HandleMatchEnded_NullRefs_RaisesMatchLost()
        {
            var mgr      = CreateManager();
            var evtLost  = CreateEvent();
            bool lostFired = false;
            evtLost.RegisterCallback(() => lostFired = true);

            SetField(mgr, "_onMatchLost", evtLost);

            InvokePrivate(mgr, "Awake");
            mgr.HandleMatchEnded();

            Assert.IsTrue(lostFired,
                "HandleMatchEnded with null ObjectiveSO must raise _onMatchLost.");

            Object.DestroyImmediate(mgr.gameObject);
            Object.DestroyImmediate(evtLost);
        }

        [Test]
        public void HandleMatchEnded_ObjectiveMet_RaisesMatchWon()
        {
            var mgr       = CreateManager();
            var domSO     = CreateDominanceSO();
            var objSO     = CreateObjectiveSO();  // RequiredZones = 1
            var evtWon    = CreateEvent();
            bool wonFired = false;
            evtWon.RegisterCallback(() => wonFired = true);

            SetField(mgr, "_dominanceSO", domSO);
            SetField(mgr, "_objectiveSO", objSO);
            SetField(mgr, "_onMatchWon",  evtWon);

            domSO.AddPlayerZone();
            Assert.AreEqual(1, domSO.PlayerZoneCount, "Pre-condition: player holds 1 zone.");

            InvokePrivate(mgr, "Awake");
            mgr.HandleMatchEnded();

            Assert.IsTrue(wonFired,
                "HandleMatchEnded must raise _onMatchWon when the player meets the objective.");

            Object.DestroyImmediate(mgr.gameObject);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(objSO);
            Object.DestroyImmediate(evtWon);
        }

        [Test]
        public void HandleMatchEnded_ObjectiveNotMet_RaisesMatchLost()
        {
            var mgr        = CreateManager();
            var domSO      = CreateDominanceSO();
            var objSO      = CreateObjectiveSO();  // RequiredZones = 1
            var evtLost    = CreateEvent();
            bool lostFired = false;
            evtLost.RegisterCallback(() => lostFired = true);

            SetField(mgr, "_dominanceSO", domSO);
            SetField(mgr, "_objectiveSO", objSO);
            SetField(mgr, "_onMatchLost", evtLost);

            // Player holds 0 zones → objective NOT met.
            Assert.AreEqual(0, domSO.PlayerZoneCount, "Pre-condition: player holds 0 zones.");

            InvokePrivate(mgr, "Awake");
            mgr.HandleMatchEnded();

            Assert.IsTrue(lostFired,
                "HandleMatchEnded must raise _onMatchLost when the player misses the objective.");

            Object.DestroyImmediate(mgr.gameObject);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(objSO);
            Object.DestroyImmediate(evtLost);
        }

        [Test]
        public void MatchStarted_Event_TriggersReset()
        {
            var mgr        = CreateManager();
            var domSO      = CreateDominanceSO();
            var evtStarted = CreateEvent();

            SetField(mgr, "_dominanceSO",    domSO);
            SetField(mgr, "_onMatchStarted", evtStarted);

            domSO.AddPlayerZone();
            Assert.AreEqual(1, domSO.PlayerZoneCount, "Pre-condition: player holds 1 zone.");

            InvokePrivate(mgr, "Awake");
            InvokePrivate(mgr, "OnEnable");

            evtStarted.Raise();

            Assert.AreEqual(0, domSO.PlayerZoneCount,
                "_onMatchStarted event must call HandleMatchStarted and reset dominance.");

            Object.DestroyImmediate(mgr.gameObject);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(evtStarted);
        }
    }
}
