using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PowerUpSpawnConfig"/> and
    /// the <see cref="PowerUpSpawnPoint"/> struct.
    ///
    /// Covers:
    ///   • Fresh-instance defaults: SpawnPoints not-null, empty, IReadOnlyList contract,
    ///     RespawnDelay == 15.
    ///   • SpawnPoints list: 1-entry count, 2-entry count, insertion order preserved.
    ///   • RespawnDelay zero is valid.
    ///   • OnValidate with null PowerUp entry does not throw (logs warning only).
    /// </summary>
    public class PowerUpSpawnConfigTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private PowerUpSpawnConfig _cfg;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target)
        {
            MethodInfo mi = target.GetType()
                .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, "OnValidate not found.");
            mi.Invoke(target, null);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<PowerUpSpawnConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cfg);
            _cfg = null;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_SpawnPoints_NotNull()
        {
            Assert.IsNotNull(_cfg.SpawnPoints);
        }

        [Test]
        public void FreshInstance_SpawnPoints_IsEmpty()
        {
            Assert.AreEqual(0, _cfg.SpawnPoints.Count);
        }

        [Test]
        public void FreshInstance_SpawnPoints_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<PowerUpSpawnPoint>>(_cfg.SpawnPoints);
        }

        [Test]
        public void FreshInstance_RespawnDelay_Is15()
        {
            Assert.AreEqual(15f, _cfg.RespawnDelay, 0.001f);
        }

        // ── SpawnPoints list ──────────────────────────────────────────────────

        [Test]
        public void WithOneEntry_Count_IsOne()
        {
            var powerUp = ScriptableObject.CreateInstance<PowerUpSO>();
            var point   = new PowerUpSpawnPoint { Position = Vector3.zero, PowerUp = powerUp };
            var list    = new List<PowerUpSpawnPoint> { point };
            SetField(_cfg, "_spawnPoints", list);

            Assert.AreEqual(1, _cfg.SpawnPoints.Count);

            Object.DestroyImmediate(powerUp);
        }

        [Test]
        public void WithTwoEntries_Count_IsTwo()
        {
            var pu1 = ScriptableObject.CreateInstance<PowerUpSO>();
            var pu2 = ScriptableObject.CreateInstance<PowerUpSO>();
            var list = new List<PowerUpSpawnPoint>
            {
                new PowerUpSpawnPoint { Position = Vector3.zero,    PowerUp = pu1 },
                new PowerUpSpawnPoint { Position = Vector3.one,     PowerUp = pu2 }
            };
            SetField(_cfg, "_spawnPoints", list);

            Assert.AreEqual(2, _cfg.SpawnPoints.Count);

            Object.DestroyImmediate(pu1);
            Object.DestroyImmediate(pu2);
        }

        [Test]
        public void SpawnPoints_PreservesInsertionOrder()
        {
            var pu1 = ScriptableObject.CreateInstance<PowerUpSO>();
            var pu2 = ScriptableObject.CreateInstance<PowerUpSO>();
            var p1  = new PowerUpSpawnPoint { Position = new Vector3(1, 0, 0), PowerUp = pu1 };
            var p2  = new PowerUpSpawnPoint { Position = new Vector3(2, 0, 0), PowerUp = pu2 };
            SetField(_cfg, "_spawnPoints", new List<PowerUpSpawnPoint> { p1, p2 });

            Assert.AreEqual(1f, _cfg.SpawnPoints[0].Position.x, 0.001f, "First entry should be p1.");
            Assert.AreEqual(2f, _cfg.SpawnPoints[1].Position.x, 0.001f, "Second entry should be p2.");

            Object.DestroyImmediate(pu1);
            Object.DestroyImmediate(pu2);
        }

        // ── RespawnDelay edge cases ────────────────────────────────────────────

        [Test]
        public void RespawnDelay_Zero_IsValid()
        {
            SetField(_cfg, "_respawnDelay", 0f);
            Assert.AreEqual(0f, _cfg.RespawnDelay, 0.001f);
        }

        // ── OnValidate with null PowerUp entry ────────────────────────────────

        [Test]
        public void OnValidate_NullPowerUpEntry_DoesNotThrow()
        {
            // Entry with null PowerUp → OnValidate logs a warning but must not throw.
            var list = new List<PowerUpSpawnPoint>
            {
                new PowerUpSpawnPoint { Position = Vector3.zero, PowerUp = null }
            };
            SetField(_cfg, "_spawnPoints", list);

            Assert.DoesNotThrow(() => InvokeOnValidate(_cfg),
                "OnValidate should only log a warning for null entries, not throw.");
        }
    }
}
