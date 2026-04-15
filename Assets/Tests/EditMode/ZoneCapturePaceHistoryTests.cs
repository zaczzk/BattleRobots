using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T294:
    ///   <see cref="ZoneCapturePaceHistorySO"/> and
    ///   <see cref="ZoneCapturePaceHistoryController"/>.
    ///
    /// ZoneCapturePaceHistoryTests (12):
    ///   FreshInstance_HistorySO_Null                             ×1
    ///   FreshInstance_TrackerSO_Null                             ×1
    ///   OnEnable_NullRefs_DoesNotThrow                           ×1
    ///   OnDisable_NullRefs_DoesNotThrow                          ×1
    ///   OnDisable_Unregisters_Channels                           ×1
    ///   HandleMatchEnded_NullHistorySO_NoThrow                   ×1
    ///   HandleMatchEnded_AddsPaceReading                         ×1
    ///   Refresh_NullHistorySO_HidesPanel                         ×1
    ///   Refresh_ShowsPanel_WhenHistorySOSet                      ×1
    ///   Refresh_Bar_Enabled_WhenReadingAvailable                 ×1
    ///   Refresh_Bar_Disabled_WhenBeyondReadingCount              ×1
    ///   Refresh_NullPanel_DoesNotThrow                           ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneCapturePaceHistoryTests
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

        private static ZoneCapturePaceHistorySO CreateHistorySO() =>
            ScriptableObject.CreateInstance<ZoneCapturePaceHistorySO>();

        private static ZoneCapturePaceTrackerSO CreateTrackerSO() =>
            ScriptableObject.CreateInstance<ZoneCapturePaceTrackerSO>();

        private static ZoneCapturePaceHistoryController CreateController() =>
            new GameObject("PaceHistoryCtrl_Test")
                .AddComponent<ZoneCapturePaceHistoryController>();

        private static Image CreateImage()
        {
            var go = new GameObject("Img");
            go.AddComponent<CanvasRenderer>();
            return go.AddComponent<Image>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_HistorySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.HistorySO,
                "HistorySO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_TrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TrackerSO,
                "TrackerSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneCapturePaceHistoryController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneCapturePaceHistoryController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneCapturePaceHistoryController>();

            var evtEnd     = CreateEvent();
            var evtHistory = CreateEvent();
            SetField(ctrl, "_onMatchEnded",         evtEnd);
            SetField(ctrl, "_onPaceHistoryUpdated",  evtHistory);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evtEnd.RegisterCallback(() => count++);
            evtHistory.RegisterCallback(() => count++);
            evtEnd.Raise();
            evtHistory.Raise();

            Assert.AreEqual(2, count,
                "Both channels must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evtEnd);
            Object.DestroyImmediate(evtHistory);
        }

        [Test]
        public void HandleMatchEnded_NullHistorySO_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchEnded_AddsPaceReading()
        {
            var go      = new GameObject("Test_HandleMatchEnded");
            var ctrl    = go.AddComponent<ZoneCapturePaceHistoryController>();
            var history = CreateHistorySO();
            var tracker = CreateTrackerSO();

            SetField(ctrl, "_historySO", history);
            SetField(ctrl, "_trackerSO", tracker);

            ctrl.HandleMatchEnded();

            Assert.AreEqual(1, history.EntryCount,
                "HandleMatchEnded must add one pace reading to the history SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Refresh_NullHistorySO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_NullHistory");
            var ctrl  = go.AddComponent<ZoneCapturePaceHistoryController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when HistorySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_ShowsPanel_WhenHistorySOSet()
        {
            var go      = new GameObject("Test_Refresh_ShowPanel");
            var ctrl    = go.AddComponent<ZoneCapturePaceHistoryController>();
            var history = CreateHistorySO();
            var panel   = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_historySO", history);
            SetField(ctrl, "_panel",     panel);
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when HistorySO is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_Bar_Enabled_WhenReadingAvailable()
        {
            var go      = new GameObject("Test_BarEnabled");
            var ctrl    = go.AddComponent<ZoneCapturePaceHistoryController>();
            var history = CreateHistorySO();
            var bar     = CreateImage();

            history.AddPaceReading(2.5f);

            SetField(ctrl, "_historySO",     history);
            SetField(ctrl, "_paceBarImages", new Image[] { bar });
            ctrl.Refresh();

            Assert.IsTrue(bar.enabled,
                "Bar at index 0 must be enabled when a reading exists.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(bar.gameObject);
        }

        [Test]
        public void Refresh_Bar_Disabled_WhenBeyondReadingCount()
        {
            var go      = new GameObject("Test_BarDisabled");
            var ctrl    = go.AddComponent<ZoneCapturePaceHistoryController>();
            var history = CreateHistorySO();
            var bar0    = CreateImage();
            var bar1    = CreateImage();
            bar1.enabled = true;

            // Only one reading — bar1 (index 1) should be disabled.
            history.AddPaceReading(1.0f);

            SetField(ctrl, "_historySO",     history);
            SetField(ctrl, "_paceBarImages", new Image[] { bar0, bar1 });
            ctrl.Refresh();

            Assert.IsFalse(bar1.enabled,
                "Bar at index 1 must be disabled when only one reading exists.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(bar0.gameObject);
            Object.DestroyImmediate(bar1.gameObject);
        }

        [Test]
        public void Refresh_NullPanel_DoesNotThrow()
        {
            var go      = new GameObject("Test_NullPanel");
            var ctrl    = go.AddComponent<ZoneCapturePaceHistoryController>();
            var history = CreateHistorySO();

            SetField(ctrl, "_historySO", history);
            // _panel intentionally null.
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _panel is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
        }
    }
}
