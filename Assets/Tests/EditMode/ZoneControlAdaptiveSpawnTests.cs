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
    ///   SO_FreshInstance_CurrentSpawnRate_EqualsBase                     ×1
    ///   SO_FreshInstance_IsHighPressure_False                            ×1
    ///   SO_SetHighPressure_True_SetsHighPressureRate                     ×1
    ///   SO_SetHighPressure_False_SetsLowPressureRate                     ×1
    ///   SO_SetHighPressure_SameValue_NoEvent                             ×1
    ///   SO_SetHighPressure_Changed_FiresEvent                            ×1
    ///   SO_Reset_RestoresBase                                            ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                        ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                       ×1
    ///   Controller_OnDisable_Unregisters_Channel                         ×1
    ///   Controller_HandleHighPressure_SetsHighPressure                   ×1
    ///   Controller_HandleMatchStarted_ResetsSpawnSO                      ×1
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
            float baseRate = 5f, float highRate = 2f, float lowRate = 8f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAdaptiveSpawnSO>();
            SetField(so, "_baseSpawnRate",         baseRate);
            SetField(so, "_highPressureSpawnRate",  highRate);
            SetField(so, "_lowPressureSpawnRate",   lowRate);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CurrentSpawnRate_EqualsBase()
        {
            var so = CreateSpawnSO(baseRate: 5f);
            Assert.AreEqual(5f, so.CurrentSpawnRate, 0.001f,
                "CurrentSpawnRate must equal the base rate after Reset.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsHighPressure_False()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAdaptiveSpawnSO>();
            Assert.IsFalse(so.IsHighPressure,
                "IsHighPressure must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetHighPressure_True_SetsHighPressureRate()
        {
            var so = CreateSpawnSO(highRate: 2f);
            so.SetHighPressure(true);
            Assert.AreEqual(2f, so.CurrentSpawnRate, 0.001f,
                "CurrentSpawnRate must equal highPressureSpawnRate after SetHighPressure(true).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetHighPressure_False_SetsLowPressureRate()
        {
            var so = CreateSpawnSO(highRate: 2f, lowRate: 8f);
            so.SetHighPressure(true);  // enter high
            so.SetHighPressure(false); // return to low
            Assert.AreEqual(8f, so.CurrentSpawnRate, 0.001f,
                "CurrentSpawnRate must equal lowPressureSpawnRate after SetHighPressure(false).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetHighPressure_SameValue_NoEvent()
        {
            var so  = CreateSpawnSO();
            var evt = CreateEvent();
            SetField(so, "_onSpawnRateChanged", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.SetHighPressure(false); // already false
            Assert.AreEqual(0, fired,
                "_onSpawnRateChanged must NOT fire when pressure state is unchanged.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
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
        public void SO_Reset_RestoresBase()
        {
            var so = CreateSpawnSO(baseRate: 5f, highRate: 2f);
            so.SetHighPressure(true);
            so.Reset();
            Assert.AreEqual(5f, so.CurrentSpawnRate, 0.001f,
                "CurrentSpawnRate must return to base after Reset.");
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
        public void Controller_HandleHighPressure_SetsHighPressure()
        {
            var go   = new GameObject("Test_HandleHP");
            var ctrl = go.AddComponent<ZoneControlAdaptiveSpawnController>();
            var so   = CreateSpawnSO(highRate: 2f);
            SetField(ctrl, "_spawnSO", so);

            ctrl.HandleHighPressure();

            Assert.IsTrue(so.IsHighPressure,
                "HandleHighPressure must set high-pressure mode on the spawn SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsSpawnSO()
        {
            var go   = new GameObject("Test_MatchStart");
            var ctrl = go.AddComponent<ZoneControlAdaptiveSpawnController>();
            var so   = CreateSpawnSO(baseRate: 5f, highRate: 2f);
            SetField(ctrl, "_spawnSO", so);

            so.SetHighPressure(true);
            ctrl.HandleMatchStarted();

            Assert.AreEqual(5f, so.CurrentSpawnRate, 0.001f,
                "HandleMatchStarted must reset the spawn SO to base rate.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}
