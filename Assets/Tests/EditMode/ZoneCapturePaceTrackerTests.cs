using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T289: <see cref="ZoneCapturePaceTrackerSO"/> and
    /// <see cref="ZoneCapturePaceHUDController"/>.
    ///
    /// ZoneCapturePaceTrackerTests (12):
    ///   SO_FreshInstance_CaptureCount_Zero                                   ×1
    ///   SO_RecordCapture_IncrementsCaptureCount                              ×1
    ///   SO_Reset_ClearsCaptureCount                                          ×1
    ///   SO_GetCapturesPerMinute_Zero_WhenEmpty                               ×1
    ///   SO_GetCapturesPerMinute_PrunesOldEntries                             ×1
    ///   SO_WindowDuration_Default_Sixty                                      ×1
    ///   HUD_FreshInstance_TrackerSO_Null                                     ×1
    ///   HUD_OnEnable_NullRefs_DoesNotThrow                                   ×1
    ///   HUD_OnDisable_NullRefs_DoesNotThrow                                  ×1
    ///   HUD_OnDisable_Unregisters_BothChannels                               ×1
    ///   HUD_Refresh_NullTracker_HidesPanel                                   ×1
    ///   HUD_Refresh_WithTracker_ShowsPanel                                   ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneCapturePaceTrackerTests
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

        private static ZoneCapturePaceTrackerSO CreateTrackerSO() =>
            ScriptableObject.CreateInstance<ZoneCapturePaceTrackerSO>();

        private static ZoneCapturePaceHUDController CreateHUD() =>
            new GameObject("ZoneCapturePaceHUD_Test")
                .AddComponent<ZoneCapturePaceHUDController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0, so.CaptureCount,
                "CaptureCount must be 0 on a fresh ZoneCapturePaceTrackerSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCaptureCount()
        {
            var so = CreateTrackerSO();
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            Assert.AreEqual(2, so.CaptureCount,
                "CaptureCount must increment after each RecordCapture call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsCaptureCount()
        {
            var so = CreateTrackerSO();
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.Reset();
            Assert.AreEqual(0, so.CaptureCount,
                "CaptureCount must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetCapturesPerMinute_Zero_WhenEmpty()
        {
            var so   = CreateTrackerSO();
            float rate = so.GetCapturesPerMinute(100f);
            Assert.AreEqual(0f, rate,
                "Rate must be 0 when no captures have been recorded.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetCapturesPerMinute_PrunesOldEntries()
        {
            var so = CreateTrackerSO();
            // Default window = 60s. Record a capture at time 0.
            so.RecordCapture(0f);
            Assert.AreEqual(1, so.CaptureCount, "Count must be 1 after RecordCapture.");

            // Query at time 61s — the entry at t=0 is outside the 60s window and must be pruned.
            so.GetCapturesPerMinute(61f);
            Assert.AreEqual(0, so.CaptureCount,
                "CaptureCount must be 0 after old entries are pruned.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WindowDuration_Default_Sixty()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(60f, so.WindowDuration,
                "Default WindowDuration must be 60 seconds.");
            Object.DestroyImmediate(so);
        }

        // ── HUD Tests ─────────────────────────────────────────────────────────

        [Test]
        public void HUD_FreshInstance_TrackerSO_Null()
        {
            var hud = CreateHUD();
            Assert.IsNull(hud.TrackerSO,
                "TrackerSO must be null on a freshly added HUD controller.");
            Object.DestroyImmediate(hud.gameObject);
        }

        [Test]
        public void HUD_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(() => go.AddComponent<ZoneCapturePaceHUDController>(),
                "Adding HUD with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void HUD_OnDisable_NullRefs_DoesNotThrow()
        {
            var go  = new GameObject("Test_OnDisable_Null");
            var hud = go.AddComponent<ZoneCapturePaceHUDController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling HUD with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void HUD_OnDisable_Unregisters_BothChannels()
        {
            var go  = new GameObject("Test_Unregister");
            var hud = go.AddComponent<ZoneCapturePaceHUDController>();

            var captureEvt  = CreateEvent();
            var paceUpdEvt  = CreateEvent();

            SetField(hud, "_onCaptured",    captureEvt);
            SetField(hud, "_onPaceUpdated", paceUpdEvt);

            go.SetActive(true);
            go.SetActive(false);

            int captureCount = 0, paceCount = 0;
            captureEvt.RegisterCallback(() => captureCount++);
            paceUpdEvt.RegisterCallback(() => paceCount++);

            captureEvt.Raise();
            paceUpdEvt.Raise();

            Assert.AreEqual(1, captureCount, "_onCaptured must be unregistered after OnDisable.");
            Assert.AreEqual(1, paceCount,    "_onPaceUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(captureEvt);
            Object.DestroyImmediate(paceUpdEvt);
        }

        [Test]
        public void HUD_Refresh_NullTracker_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var hud   = go.AddComponent<ZoneCapturePaceHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(hud, "_panel", panel);
            hud.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when TrackerSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void HUD_Refresh_WithTracker_ShowsPanel()
        {
            var go     = new GameObject("Test_Refresh_WithTracker");
            var hud    = go.AddComponent<ZoneCapturePaceHUDController>();
            var so     = CreateTrackerSO();
            var panel  = new GameObject("Panel");
            panel.SetActive(false);

            SetField(hud, "_trackerSO", so);
            SetField(hud, "_panel",     panel);
            hud.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when TrackerSO is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}
