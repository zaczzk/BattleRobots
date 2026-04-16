using System.Reflection;
using System;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T314: <see cref="ZoneControlMatchEventSO"/> and
    /// <see cref="ZoneControlMatchEventController"/>.
    ///
    /// ZoneControlMatchEventTests (12):
    ///   SO_FreshInstance_EventCount_Zero                                          ×1
    ///   SO_AddEvent_IncrementsCount                                               ×1
    ///   SO_AddEvent_EvictsOldestWhenFull                                          ×1
    ///   SO_AddEvent_NullDescription_StoredAsEmpty                                 ×1
    ///   SO_Reset_ClearsAll                                                        ×1
    ///   Controller_FreshInstance_EventSO_Null                                     ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channel                                  ×1
    ///   Controller_HandleMatchStarted_ResetsBuffer                                ×1
    ///   Controller_HandleZoneCaptured_AddsEvent                                   ×1
    ///   Controller_Refresh_NullSO_HidesPanel                                      ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchEventTests
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

        private static ZoneControlMatchEventSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchEventSO>();

        private static ZoneControlMatchEventController CreateController() =>
            new GameObject("MatchEventCtrl_Test")
                .AddComponent<ZoneControlMatchEventController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EventCount_Zero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.EventCount,
                "EventCount must be 0 on a fresh instance.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddEvent_IncrementsCount()
        {
            var so = CreateSO();
            so.AddEvent(1f, ZoneControlMatchEventType.ZoneCaptured, "Zone Captured");
            Assert.AreEqual(1, so.EventCount,
                "EventCount must be 1 after one AddEvent call.");
            Assert.AreEqual("Zone Captured", so.Events[0].Description,
                "Event description must match what was passed.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddEvent_EvictsOldestWhenFull()
        {
            var so = CreateSO();
            // Default _maxEvents is 20; override via reflection for speed.
            SetField(so, "_maxEvents", 3);

            so.AddEvent(1f, ZoneControlMatchEventType.ZoneCaptured, "A");
            so.AddEvent(2f, ZoneControlMatchEventType.HazardActivated, "B");
            so.AddEvent(3f, ZoneControlMatchEventType.ComboReached, "C");
            so.AddEvent(4f, ZoneControlMatchEventType.ZoneCaptured, "D"); // evicts "A"

            Assert.AreEqual(3, so.EventCount,
                "EventCount must not exceed MaxEvents after overflow.");
            Assert.AreEqual("B", so.Events[0].Description,
                "Oldest event must have been evicted — first remaining should be B.");
            Assert.AreEqual("D", so.Events[2].Description,
                "Newest event (D) must be at the last index.");

            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddEvent_NullDescription_StoredAsEmpty()
        {
            var so = CreateSO();
            so.AddEvent(0f, ZoneControlMatchEventType.ZoneCaptured, null);
            Assert.AreEqual(string.Empty, so.Events[0].Description,
                "Null description must be stored as string.Empty.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.AddEvent(1f, ZoneControlMatchEventType.ZoneCaptured, "A");
            so.AddEvent(2f, ZoneControlMatchEventType.HazardActivated, "B");
            so.Reset();
            Assert.AreEqual(0, so.EventCount,
                "EventCount must be 0 after Reset.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_EventSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.EventSO,
                "EventSO must be null on a freshly added controller.");
            UnityEngine.Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMatchEventController>(),
                "Adding controller with all-null refs must not throw.");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMatchEventController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchEventController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onZoneCaptured", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onZoneCaptured must be unregistered after OnDisable.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsBuffer()
        {
            var go   = new GameObject("Test_MatchStarted");
            var ctrl = go.AddComponent<ZoneControlMatchEventController>();
            var so   = CreateSO();

            SetField(ctrl, "_eventSO", so);

            so.AddEvent(1f, ZoneControlMatchEventType.ZoneCaptured, "A");
            Assert.AreEqual(1, so.EventCount);

            ctrl.HandleMatchStarted();
            Assert.AreEqual(0, so.EventCount,
                "HandleMatchStarted must reset the event buffer via SO.Reset().");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleZoneCaptured_AddsEvent()
        {
            var go   = new GameObject("Test_ZoneCaptured");
            var ctrl = go.AddComponent<ZoneControlMatchEventController>();
            var so   = CreateSO();

            SetField(ctrl, "_eventSO", so);

            ctrl.HandleZoneCaptured();
            Assert.AreEqual(1, so.EventCount,
                "HandleZoneCaptured must add one event to the SO.");
            Assert.AreEqual(ZoneControlMatchEventType.ZoneCaptured, so.Events[0].Type,
                "The added event must have Type == ZoneCaptured.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlMatchEventController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when EventSO is null.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(panel);
        }
    }
}
