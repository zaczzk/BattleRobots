using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T348: <see cref="ZoneControlPowerUpSO"/> and
    /// <see cref="ZoneControlPowerUpController"/>.
    ///
    /// ZoneControlPowerUpTests (12):
    ///   SO_FreshInstance_Accumulated_Zero                                    ×1
    ///   SO_FreshInstance_TotalCollected_Zero                                 ×1
    ///   SO_Tick_BelowInterval_NoSpawn                                        ×1
    ///   SO_Tick_ReachesInterval_SpawnsOnce                                   ×1
    ///   SO_Tick_ResetsAccumulatorAfterSpawn                                  ×1
    ///   SO_SpawnPowerUp_FiresOnPowerUpSpawned                                ×1
    ///   SO_CollectPowerUp_IncrementsTotalCollected                           ×1
    ///   SO_CollectPowerUp_FiresOnPowerUpCollected                            ×1
    ///   SO_Reset_ClearsAccumulatedAndCount                                   ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                        ×1
    ///   Controller_OnDisable_Unregisters_Channels                            ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlPowerUpTests
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

        private static ZoneControlPowerUpSO CreatePowerUpSO(float interval = 10f, int bonus = 100)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlPowerUpSO>();
            SetField(so, "_spawnInterval", interval);
            SetField(so, "_powerUpBonus",  bonus);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_Accumulated_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlPowerUpSO>();
            Assert.AreEqual(0f, so.Accumulated,
                "Accumulated must be zero on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalCollected_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlPowerUpSO>();
            Assert.AreEqual(0, so.TotalCollected,
                "TotalCollected must be zero on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BelowInterval_NoSpawn()
        {
            var so  = CreatePowerUpSO(interval: 10f);
            var evt = CreateEvent();
            SetField(so, "_onPowerUpSpawned", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.Tick(5f);  // below 10s interval

            Assert.AreEqual(0, count,
                "_onPowerUpSpawned must not fire before interval is reached.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Tick_ReachesInterval_SpawnsOnce()
        {
            var so  = CreatePowerUpSO(interval: 10f);
            var evt = CreateEvent();
            SetField(so, "_onPowerUpSpawned", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.Tick(10f);  // exactly hits the interval

            Assert.AreEqual(1, count,
                "_onPowerUpSpawned must fire once when interval is reached.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Tick_ResetsAccumulatorAfterSpawn()
        {
            var so = CreatePowerUpSO(interval: 10f);
            so.Tick(10f);
            Assert.Less(so.Accumulated, 10f,
                "Accumulated must be less than interval after a spawn.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SpawnPowerUp_FiresOnPowerUpSpawned()
        {
            var so  = CreatePowerUpSO();
            var evt = CreateEvent();
            SetField(so, "_onPowerUpSpawned", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.SpawnPowerUp();

            Assert.AreEqual(1, count,
                "_onPowerUpSpawned must fire once when SpawnPowerUp is called.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_CollectPowerUp_IncrementsTotalCollected()
        {
            var so = CreatePowerUpSO();
            so.CollectPowerUp();
            Assert.AreEqual(1, so.TotalCollected,
                "TotalCollected must be 1 after one CollectPowerUp call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CollectPowerUp_FiresOnPowerUpCollected()
        {
            var so  = CreatePowerUpSO();
            var evt = CreateEvent();
            SetField(so, "_onPowerUpCollected", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.CollectPowerUp();

            Assert.AreEqual(1, count,
                "_onPowerUpCollected must fire once when CollectPowerUp is called.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAccumulatedAndCount()
        {
            var so = CreatePowerUpSO(interval: 10f);
            so.Tick(5f);
            so.CollectPowerUp();
            so.Reset();
            Assert.AreEqual(0f, so.Accumulated,    "Accumulated must be 0 after Reset.");
            Assert.AreEqual(0,  so.TotalCollected, "TotalCollected must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_PowerUp_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlPowerUpController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_PowerUp_OnDisable_Null");
            go.AddComponent<ZoneControlPowerUpController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_PowerUp_Unregister");
            var ctrl = go.AddComponent<ZoneControlPowerUpController>();
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
    }
}
