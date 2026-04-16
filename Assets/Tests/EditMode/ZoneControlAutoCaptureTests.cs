using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T337: <see cref="ZoneControlAutoCaptureSO"/> and
    /// <see cref="ZoneControlAutoCaptureController"/>.
    ///
    /// ZoneControlAutoCaptureTests (12):
    ///   SO_FreshInstance_AutoCaptureDuration_Default                ×1
    ///   SO_FireAutoCapture_RaisesEvent                              ×1
    ///   SO_Reset_DoesNotThrow                                       ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_Unregisters_Channel                    ×1
    ///   Controller_HandleMatchStarted_ResetsAccumulator             ×1
    ///   Controller_Tick_NullRefs_DoesNotThrow                       ×1
    ///   Controller_Tick_WhenPlayerHasZones_DoesNotAccumulate        ×1
    ///   Controller_Tick_WhenNoPlayerZones_Accumulates               ×1
    ///   Controller_Tick_DoesNotFireBelowDuration                    ×1
    ///   Controller_Tick_FiresAutoCapture_WhenDurationMet            ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlAutoCaptureTests
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

        private static ZoneControlAutoCaptureSO CreateAutoCaptureSO(float duration = 5f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAutoCaptureSO>();
            SetField(so, "_autoCaptureDuration", duration);
            so.Reset();
            return so;
        }

        private static ZoneControlZoneControllerCatalogSO CreateCatalogSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneControllerCatalogSO>();
            so.Reset();
            return so;
        }

        private static ZoneControlAutoCaptureController CreateController() =>
            new GameObject("AutoCapture_Test")
                .AddComponent<ZoneControlAutoCaptureController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_AutoCaptureDuration_Default()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAutoCaptureSO>();
            Assert.AreEqual(5f, so.AutoCaptureDuration, 0.001f,
                "AutoCaptureDuration must default to 5f.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FireAutoCapture_RaisesEvent()
        {
            var so  = CreateAutoCaptureSO();
            var evt = CreateEvent();
            SetField(so, "_onAutoCapture", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.FireAutoCapture();

            Assert.AreEqual(1, fired,
                "_onAutoCapture must fire once when FireAutoCapture is called.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_DoesNotThrow()
        {
            var so = CreateAutoCaptureSO();
            Assert.DoesNotThrow(() => so.Reset(),
                "Reset must not throw.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlAutoCaptureController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlAutoCaptureController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlAutoCaptureController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchStarted", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchStarted must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsAccumulator()
        {
            var ctrl    = CreateController();
            var so      = CreateAutoCaptureSO();
            var catalog = CreateCatalogSO();

            SetField(ctrl, "_autoCaptureSO", so);
            SetField(ctrl, "_catalogSO",     catalog);

            ctrl.Tick(2f);  // start accumulating
            ctrl.HandleMatchStarted();

            Assert.IsFalse(ctrl.IsAccumulating,
                "IsAccumulating must be false after HandleMatchStarted.");
            Assert.AreEqual(0f, ctrl.AccumulatedTime, 0.001f,
                "AccumulatedTime must be 0 after HandleMatchStarted.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Controller_Tick_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.Tick(1f),
                "Tick must not throw when SO refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Tick_WhenPlayerHasZones_DoesNotAccumulate()
        {
            var ctrl    = CreateController();
            var so      = CreateAutoCaptureSO();
            var catalog = CreateCatalogSO();
            catalog.SetZoneController(0, true);  // player owns zone 0

            SetField(ctrl, "_autoCaptureSO", so);
            SetField(ctrl, "_catalogSO",     catalog);

            ctrl.Tick(1f);

            Assert.IsFalse(ctrl.IsAccumulating,
                "IsAccumulating must be false when the player owns at least one zone.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Controller_Tick_WhenNoPlayerZones_Accumulates()
        {
            var ctrl    = CreateController();
            var so      = CreateAutoCaptureSO(duration: 10f);
            var catalog = CreateCatalogSO();  // all zones bot-owned by default

            SetField(ctrl, "_autoCaptureSO", so);
            SetField(ctrl, "_catalogSO",     catalog);

            ctrl.Tick(1f);

            Assert.IsTrue(ctrl.IsAccumulating,
                "IsAccumulating must be true when the player owns zero zones.");
            Assert.AreEqual(1f, ctrl.AccumulatedTime, 0.001f,
                "AccumulatedTime must equal the elapsed delta.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Controller_Tick_DoesNotFireBelowDuration()
        {
            var ctrl    = CreateController();
            var so      = CreateAutoCaptureSO(duration: 5f);
            var catalog = CreateCatalogSO();
            var evt     = CreateEvent();
            SetField(so, "_onAutoCapture", evt);

            SetField(ctrl, "_autoCaptureSO", so);
            SetField(ctrl, "_catalogSO",     catalog);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            ctrl.Tick(4f);  // below 5s duration

            Assert.AreEqual(0, fired,
                "_onAutoCapture must not fire before the duration threshold is reached.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Tick_FiresAutoCapture_WhenDurationMet()
        {
            var ctrl    = CreateController();
            var so      = CreateAutoCaptureSO(duration: 3f);
            var catalog = CreateCatalogSO();
            var evt     = CreateEvent();
            SetField(so, "_onAutoCapture", evt);

            SetField(ctrl, "_autoCaptureSO", so);
            SetField(ctrl, "_catalogSO",     catalog);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            ctrl.Tick(4f);  // exceeds 3s duration

            Assert.AreEqual(1, fired,
                "_onAutoCapture must fire exactly once when duration is met.");
            Assert.IsFalse(ctrl.IsAccumulating,
                "IsAccumulating must reset to false after firing.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(evt);
        }
    }
}
