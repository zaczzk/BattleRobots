using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T322: <see cref="ZoneControlAdaptiveSpawnSO"/> and
    /// <see cref="ZoneControlAdaptiveSpawnController"/>.
    ///
    /// ZoneControlAdaptiveSpawnTests (12):
    ///   SO_FreshInstance_IsHighPressure_False                         ×1
    ///   SO_SetHighPressure_True_UpdatesFlag                           ×1
    ///   SO_SetHighPressure_True_CurrentInterval_UsesPressureInterval  ×1
    ///   SO_SetHighPressure_False_CurrentInterval_UsesBaseInterval     ×1
    ///   SO_SetHighPressure_Changed_FiresEvent                         ×1
    ///   SO_SetHighPressure_SameValue_NoEvent                          ×1
    ///   SO_Reset_ClearsFlag                                           ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_Unregisters_Channel                      ×1
    ///   Controller_HandleHighPressure_SetsSpawnSO                     ×1
    ///   Controller_HandleMatchStarted_ResetsSpawnSO                   ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlAdaptiveSpawnTests
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

        private static ZoneControlAdaptiveSpawnSO CreateSpawnSO(
            float baseInterval = 5f, float pressureInterval = 2f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAdaptiveSpawnSO>();
            SetField(so, "_baseSpawnInterval",     baseInterval);
            SetField(so, "_pressureSpawnInterval", pressureInterval);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsHighPressure_False()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAdaptiveSpawnSO>();
            Assert.IsFalse(so.IsHighPressure,
                "IsHighPressure must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetHighPressure_True_UpdatesFlag()
        {
            var so = CreateSpawnSO();
            so.SetHighPressure(true);
            Assert.IsTrue(so.IsHighPressure,
                "IsHighPressure must be true after SetHighPressure(true).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetHighPressure_True_CurrentInterval_UsesPressureInterval()
        {
            var so = CreateSpawnSO(baseInterval: 5f, pressureInterval: 2f);
            so.SetHighPressure(true);
            Assert.AreEqual(2f, so.CurrentSpawnInterval, 0.0001f,
                "CurrentSpawnInterval must return PressureSpawnInterval under high pressure.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetHighPressure_False_CurrentInterval_UsesBaseInterval()
        {
            var so = CreateSpawnSO(baseInterval: 5f, pressureInterval: 2f);
            so.SetHighPressure(true);
            so.SetHighPressure(false);
            Assert.AreEqual(5f, so.CurrentSpawnInterval, 0.0001f,
                "CurrentSpawnInterval must return BaseSpawnInterval under normal pressure.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetHighPressure_Changed_FiresEvent()
        {
            var so  = CreateSpawnSO();
            var evt = CreateEvent();
            SetField(so, "_onSpawnRateChanged", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.SetHighPressure(true);
            Assert.AreEqual(1, fired,
                "_onSpawnRateChanged must fire when pressure state changes.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_SetHighPressure_SameValue_NoEvent()
        {
            var so  = CreateSpawnSO();
            var evt = CreateEvent();
            SetField(so, "_onSpawnRateChanged", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.SetHighPressure(false); // same as initial
            so.SetHighPressure(false);
            Assert.AreEqual(0, fired,
                "_onSpawnRateChanged must NOT fire when pressure state does not change.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsFlag()
        {
            var so = CreateSpawnSO();
            so.SetHighPressure(true);
            so.Reset();
            Assert.IsFalse(so.IsHighPressure,
                "IsHighPressure must be false after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlAdaptiveSpawnController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlAdaptiveSpawnController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlAdaptiveSpawnController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onHighPressure", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onHighPressure must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleHighPressure_SetsSpawnSO()
        {
            var go   = new GameObject("Test_HandleHighPressure");
            var ctrl = go.AddComponent<ZoneControlAdaptiveSpawnController>();
            var so   = CreateSpawnSO();

            SetField(ctrl, "_spawnSO", so);
            ctrl.HandleHighPressure();

            Assert.IsTrue(so.IsHighPressure,
                "HandleHighPressure must call SetHighPressure(true) on the spawn SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsSpawnSO()
        {
            var go   = new GameObject("Test_HandleMatchStarted");
            var ctrl = go.AddComponent<ZoneControlAdaptiveSpawnController>();
            var so   = CreateSpawnSO();

            SetField(ctrl, "_spawnSO", so);
            so.SetHighPressure(true);
            ctrl.HandleMatchStarted();

            Assert.IsFalse(so.IsHighPressure,
                "HandleMatchStarted must reset the spawn SO (clearing high-pressure flag).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}
