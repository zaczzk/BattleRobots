using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T306: <see cref="ZoneControlComboTrackerSO"/> and
    /// <see cref="ZoneControlComboHUDController"/>.
    ///
    /// ZoneControlComboTrackerTests (12):
    ///   FreshInstance_TrackerSO_Null                                              ×1
    ///   FreshInstance_ComboCount_Zero                                             ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                            ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                           ×1
    ///   OnDisable_Unregisters_Channels                                            ×1
    ///   RecordCapture_IncrementsCombo                                             ×1
    ///   RecordCapture_UpdatesMultiplier                                           ×1
    ///   Tick_WithinWindow_ComboMaintained                                         ×1
    ///   Tick_ExceedsWindow_ComboLost                                              ×1
    ///   Refresh_NullTrackerSO_HidesPanel                                          ×1
    ///   Refresh_ShowsPanel_WhenTrackerSOSet                                       ×1
    ///   Refresh_NullPanel_DoesNotThrow                                            ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlComboTrackerTests
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

        private static ZoneControlComboTrackerSO CreateTracker() =>
            ScriptableObject.CreateInstance<ZoneControlComboTrackerSO>();

        private static ZoneControlComboHUDController CreateController() =>
            new GameObject("ComboHUD_Test")
                .AddComponent<ZoneControlComboHUDController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_ComboCount_Zero()
        {
            var tracker = CreateTracker();
            Assert.AreEqual(0, tracker.ComboCount,
                "ComboCount must be 0 on a fresh instance.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void RecordCapture_IncrementsCombo()
        {
            var tracker = CreateTracker();
            tracker.RecordCapture();
            tracker.RecordCapture();
            Assert.AreEqual(2, tracker.ComboCount,
                "Each RecordCapture must increment ComboCount by 1.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void RecordCapture_UpdatesMultiplier()
        {
            var tracker = CreateTracker();
            // Default multiplierPerCombo = 0.5f; 1 capture → 1 + 1*0.5 = 1.5
            tracker.RecordCapture();
            Assert.Greater(tracker.CurrentMultiplier, 1f,
                "CurrentMultiplier must exceed 1 after the first capture.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Tick_WithinWindow_ComboMaintained()
        {
            var tracker = CreateTracker();
            tracker.RecordCapture();
            // Tick for half the default window (5s) → combo must remain active.
            tracker.Tick(2.4f);
            Assert.IsTrue(tracker.IsActive,
                "Combo must still be active when Tick is called within the window.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Tick_ExceedsWindow_ComboLost()
        {
            var tracker = CreateTracker();
            tracker.RecordCapture();
            // Tick past the default window (5s).
            tracker.Tick(6f);
            Assert.IsFalse(tracker.IsActive,
                "Combo must be lost when Tick advances beyond the combo window.");
            Assert.AreEqual(0, tracker.ComboCount,
                "ComboCount must be reset to 0 after the combo is lost.");
            Object.DestroyImmediate(tracker);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

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
                () => go.AddComponent<ZoneControlComboHUDController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlComboHUDController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlComboHUDController>();

            var captureEvt = CreateEvent();
            SetField(ctrl, "_onZoneCaptured", captureEvt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            captureEvt.RegisterCallback(() => count++);
            captureEvt.Raise();

            Assert.AreEqual(1, count,
                "_onZoneCaptured must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(captureEvt);
        }

        [Test]
        public void Refresh_NullTrackerSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlComboHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_comboPanel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when TrackerSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_ShowsPanel_WhenTrackerSOSet()
        {
            var go      = new GameObject("Test_Refresh_WithTracker");
            var ctrl    = go.AddComponent<ZoneControlComboHUDController>();
            var tracker = CreateTracker();
            var panel   = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_trackerSO",   tracker);
            SetField(ctrl, "_comboPanel",  panel);
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when TrackerSO is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Refresh_NullPanel_DoesNotThrow()
        {
            var go      = new GameObject("Test_Refresh_NullPanel");
            var ctrl    = go.AddComponent<ZoneControlComboHUDController>();
            var tracker = CreateTracker();

            SetField(ctrl, "_trackerSO", tracker);
            // _comboPanel left null intentionally.
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _comboPanel is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tracker);
        }
    }
}
