using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the armor-reduction feature of <see cref="DamageReceiver"/>.
    ///
    /// Covers:
    ///   • <see cref="DamageReceiver.SetArmorRating"/> — bounds clamping, idempotency.
    ///   • <see cref="DamageReceiver.TakeDamage(float)"/> — flat armor subtraction,
    ///     full-block edge cases, zero-armor pass-through.
    ///   • <see cref="DamageReceiver.TakeDamage(DamageInfo)"/> — armored struct path.
    ///
    /// Private field <c>_health</c> is injected via reflection (field is serialised
    /// but private; same pattern used by RobotDefinitionTests).
    /// </summary>
    public class DamageReceiverArmorTests
    {
        private GameObject     _go;
        private DamageReceiver _receiver;
        private HealthSO       _health;

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Inject the HealthSO asset reference into DamageReceiver via reflection.</summary>
        private static void InjectHealth(DamageReceiver receiver, HealthSO health)
        {
            FieldInfo fi = typeof(DamageReceiver)
                .GetField("_health", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_health' not found on DamageReceiver.");
            fi.SetValue(receiver, health);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go       = new GameObject("TestReceiver");
            _receiver = _go.AddComponent<DamageReceiver>();

            _health = ScriptableObject.CreateInstance<HealthSO>();
            _health.Reset();   // CurrentHealth = MaxHealth = 100

            InjectHealth(_receiver, _health);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_health);
            _go       = null;
            _receiver = null;
            _health   = null;
        }

        // ── SetArmorRating ────────────────────────────────────────────────────

        [Test]
        public void SetArmorRating_StoresValue()
        {
            _receiver.SetArmorRating(25);
            Assert.AreEqual(25, _receiver.ArmorRating);
        }

        [Test]
        public void SetArmorRating_ClampsNegativeToZero()
        {
            _receiver.SetArmorRating(-10);
            Assert.AreEqual(0, _receiver.ArmorRating);
        }

        [Test]
        public void SetArmorRating_ClampsAbove100To100()
        {
            _receiver.SetArmorRating(150);
            Assert.AreEqual(100, _receiver.ArmorRating);
        }

        [Test]
        public void SetArmorRating_BoundaryZero_IsAccepted()
        {
            _receiver.SetArmorRating(0);
            Assert.AreEqual(0, _receiver.ArmorRating);
        }

        [Test]
        public void SetArmorRating_Boundary100_IsAccepted()
        {
            _receiver.SetArmorRating(100);
            Assert.AreEqual(100, _receiver.ArmorRating);
        }

        [Test]
        public void SetArmorRating_CalledTwice_LastValueWins()
        {
            _receiver.SetArmorRating(30);
            _receiver.SetArmorRating(50);
            Assert.AreEqual(50, _receiver.ArmorRating);
        }

        // ── TakeDamage(float) with armor ──────────────────────────────────────

        [Test]
        public void TakeDamage_ZeroArmor_PassesDamageThrough()
        {
            _receiver.SetArmorRating(0);
            _receiver.TakeDamage(40f);
            Assert.AreEqual(60f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void TakeDamage_Armor20_ReducesDamageBy20()
        {
            _receiver.SetArmorRating(20);
            _receiver.TakeDamage(50f);            // effective = 50 - 20 = 30
            Assert.AreEqual(70f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void TakeDamage_ArmorEqualsDamage_ZeroEffectiveDamage()
        {
            _receiver.SetArmorRating(30);
            _receiver.TakeDamage(30f);            // effective = 0
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void TakeDamage_ArmorExceedsDamage_ZeroEffectiveDamage()
        {
            _receiver.SetArmorRating(50);
            _receiver.TakeDamage(10f);            // effective = max(0, 10-50) = 0
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void TakeDamage_MaxArmor100_BlocksAllDamage()
        {
            _receiver.SetArmorRating(100);
            _receiver.TakeDamage(9999f);
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }

        // ── TakeDamage(DamageInfo) with armor ────────────────────────────────

        [Test]
        public void TakeDamage_DamageInfo_ArmorIsApplied()
        {
            _receiver.SetArmorRating(15);
            var info = new DamageInfo(35f, "enemy", Vector3.zero);
            _receiver.TakeDamage(info);           // effective = 35 - 15 = 20
            Assert.AreEqual(80f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void TakeDamage_DamageInfo_ZeroArmor_FullDamageApplied()
        {
            _receiver.SetArmorRating(0);
            var info = new DamageInfo(25f, "enemy", Vector3.zero);
            _receiver.TakeDamage(info);
            Assert.AreEqual(75f, _health.CurrentHealth, 0.001f);
        }
    }
}
