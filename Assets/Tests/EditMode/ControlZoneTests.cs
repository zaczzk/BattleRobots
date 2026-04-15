using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T251: <see cref="ControlZoneSO"/> and
    /// <see cref="ControlZoneController"/>.
    ///
    /// ControlZoneTests (16):
    ///   SO_FreshInstance_ZoneId_Default_Zone                        ×1
    ///   SO_FreshInstance_CaptureTime_Default_Three                  ×1
    ///   SO_FreshInstance_ScorePerSecond_Default_Five                ×1
    ///   SO_FreshInstance_IsCaptured_False                           ×1
    ///   SO_FreshInstance_IsCapturing_False                          ×1
    ///   SO_CaptureProgress_BelowThreshold_NotCaptured               ×1
    ///   SO_CaptureProgress_ExceedsThreshold_IsCaptured              ×1
    ///   SO_CaptureProgress_Captured_FiresOnCaptured                 ×1
    ///   Controller_FreshInstance_Zone_Null                          ×1
    ///   Controller_FreshInstance_IsMatchRunning_False               ×1
    ///   Controller_FreshInstance_IsOccupied_False                   ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_Unregisters_BothChannels               ×1
    ///   Controller_HandleMatchStarted_SetsRunning_ResetsZone        ×1
    ///   Controller_HandleMatchEnded_SetsRunning_False_ResetsZone    ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class ControlZoneTests
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

        private static ControlZoneSO CreateZoneSO() =>
            ScriptableObject.CreateInstance<ControlZoneSO>();

        private static ControlZoneController CreateController() =>
            new GameObject("ControlZone_Test").AddComponent<ControlZoneController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_ZoneId_Default_Zone()
        {
            var so = CreateZoneSO();
            Assert.AreEqual("Zone", so.ZoneId,
                "ZoneId must default to 'Zone'.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CaptureTime_Default_Three()
        {
            var so = CreateZoneSO();
            Assert.AreEqual(3f, so.CaptureTime, 0.001f,
                "CaptureTime must default to 3 seconds.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ScorePerSecond_Default_Five()
        {
            var so = CreateZoneSO();
            Assert.AreEqual(5f, so.ScorePerSecond, 0.001f,
                "ScorePerSecond must default to 5.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsCaptured_False()
        {
            var so = CreateZoneSO();
            Assert.IsFalse(so.IsCaptured,
                "IsCaptured must be false on a fresh ControlZoneSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsCapturing_False()
        {
            var so = CreateZoneSO();
            Assert.IsFalse(so.IsCapturing,
                "IsCapturing must be false on a fresh ControlZoneSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureProgress_BelowThreshold_NotCaptured()
        {
            var so = CreateZoneSO();
            // Default CaptureTime = 3f; advance 1 second — should not capture.
            so.CaptureProgress(1f);
            Assert.IsFalse(so.IsCaptured,
                "Zone must not be captured when progress is below CaptureTime.");
            Assert.IsTrue(so.IsCapturing,
                "Zone must be in capturing state after partial progress.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureProgress_ExceedsThreshold_IsCaptured()
        {
            var so = CreateZoneSO();
            // Advance 4 seconds — exceeds default CaptureTime of 3.
            so.CaptureProgress(4f);
            Assert.IsTrue(so.IsCaptured,
                "Zone must be captured after elapsed time exceeds CaptureTime.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureProgress_Captured_FiresOnCaptured()
        {
            var so     = CreateZoneSO();
            var evt    = CreateEvent();
            int called = 0;
            SetField(so, "_onCaptured", evt);
            evt.RegisterCallback(() => called++);

            // Trigger capture
            so.CaptureProgress(10f);

            Assert.AreEqual(1, called,
                "_onCaptured must fire exactly once when the zone is captured.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_Zone_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Zone,
                "Zone must be null on a fresh ControlZoneController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_IsMatchRunning_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false on a fresh ControlZoneController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_IsOccupied_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsOccupied,
                "IsOccupied must be false on a fresh ControlZoneController.");
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
        public void Controller_OnDisable_Unregisters_BothChannels()
        {
            var ctrl  = CreateController();
            var start = CreateEvent();
            var end   = CreateEvent();
            SetField(ctrl, "_onMatchStarted", start);
            SetField(ctrl, "_onMatchEnded",   end);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // After disable, raising either event must not flip IsMatchRunning.
            start.Raise();
            Assert.IsFalse(ctrl.IsMatchRunning,
                "After OnDisable, _onMatchStarted must not update IsMatchRunning.");
            end.Raise();
            Assert.IsFalse(ctrl.IsMatchRunning,
                "After OnDisable, _onMatchEnded must not update IsMatchRunning.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(start);
            Object.DestroyImmediate(end);
        }

        [Test]
        public void Controller_HandleMatchStarted_SetsRunning_ResetsZone()
        {
            var ctrl = CreateController();
            var zone = CreateZoneSO();
            SetField(ctrl, "_zone", zone);
            InvokePrivate(ctrl, "Awake");

            // Advance zone to captured state before match start.
            zone.CaptureProgress(10f);
            Assert.IsTrue(zone.IsCaptured, "Pre-condition: zone must be captured.");

            ctrl.HandleMatchStarted();

            Assert.IsTrue(ctrl.IsMatchRunning,
                "HandleMatchStarted must set IsMatchRunning to true.");
            Assert.IsFalse(zone.IsCaptured,
                "HandleMatchStarted must reset the zone (clear captured state).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(zone);
        }

        [Test]
        public void Controller_HandleMatchEnded_SetsRunning_False_ResetsZone()
        {
            var ctrl = CreateController();
            var zone = CreateZoneSO();
            SetField(ctrl, "_zone", zone);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            zone.CaptureProgress(10f);
            Assert.IsTrue(zone.IsCaptured, "Pre-condition: zone must be captured.");

            ctrl.HandleMatchEnded();

            Assert.IsFalse(ctrl.IsMatchRunning,
                "HandleMatchEnded must set IsMatchRunning to false.");
            Assert.IsFalse(zone.IsCaptured,
                "HandleMatchEnded must reset the zone (clear captured state).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(zone);
        }
    }
}
