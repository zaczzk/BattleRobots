using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="HealthSO"/> ScriptableObject.
    ///
    /// Event channel fields (_onHealthChanged, _onDamageReceived, _onDeath) are all
    /// null in test instances, which is safe — every raise call uses ?. operator.
    /// </summary>
    [TestFixture]
    public sealed class HealthSOTests
    {
        private HealthSO _health;

        [SetUp]
        public void SetUp()
        {
            _health = ScriptableObject.CreateInstance<HealthSO>();
            // Default _maxHp is 100f (defined in HealthSO).
            _health.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_health);
        }

        // ── Initialize ────────────────────────────────────────────────────────

        [Test]
        public void Initialize_SetsCurrentHpToMaxHp()
        {
            Assert.AreEqual(_health.MaxHp, _health.CurrentHp, 0.001f,
                "After Initialize(), CurrentHp must equal MaxHp.");
        }

        [Test]
        public void Initialize_IsAlive_IsTrue()
        {
            Assert.IsTrue(_health.IsAlive);
        }

        // ── TakeDamage ────────────────────────────────────────────────────────

        [Test]
        public void TakeDamage_PositiveAmount_ReducesHp()
        {
            _health.TakeDamage(30f);
            Assert.AreEqual(70f, _health.CurrentHp, 0.001f);
        }

        [Test]
        public void TakeDamage_ExactMaxHp_KillsRobot()
        {
            _health.TakeDamage(100f);
            Assert.AreEqual(0f, _health.CurrentHp, 0.001f);
            Assert.IsFalse(_health.IsAlive);
        }

        [Test]
        public void TakeDamage_MoreThanMaxHp_ClampsToZero()
        {
            _health.TakeDamage(999f);
            Assert.AreEqual(0f, _health.CurrentHp, 0.001f,
                "HP should never go below 0 when over-damaged.");
        }

        [Test]
        public void TakeDamage_ZeroAmount_Ignored()
        {
            _health.TakeDamage(0f);
            Assert.AreEqual(100f, _health.CurrentHp, 0.001f,
                "TakeDamage(0) must not change HP.");
        }

        [Test]
        public void TakeDamage_NegativeAmount_Ignored()
        {
            _health.TakeDamage(-20f);
            Assert.AreEqual(100f, _health.CurrentHp, 0.001f,
                "TakeDamage with negative amount must not change HP.");
        }

        [Test]
        public void TakeDamage_WhenAlreadyDead_Ignored()
        {
            _health.TakeDamage(100f); // kill
            _health.TakeDamage(50f);  // should be ignored
            Assert.AreEqual(0f, _health.CurrentHp, 0.001f,
                "TakeDamage on a dead robot must have no effect.");
        }

        [Test]
        public void TakeDamage_MultipleTimes_AccumulatesCorrectly()
        {
            _health.TakeDamage(10f);
            _health.TakeDamage(15f);
            _health.TakeDamage(25f);
            Assert.AreEqual(50f, _health.CurrentHp, 0.001f);
        }

        // ── Heal ──────────────────────────────────────────────────────────────

        [Test]
        public void Heal_PositiveAmount_IncreasesHp()
        {
            _health.TakeDamage(40f); // 60 HP
            _health.Heal(20f);
            Assert.AreEqual(80f, _health.CurrentHp, 0.001f);
        }

        [Test]
        public void Heal_ClampsToMaxHp()
        {
            _health.TakeDamage(10f); // 90 HP
            _health.Heal(999f);
            Assert.AreEqual(100f, _health.CurrentHp, 0.001f,
                "Heal should never exceed MaxHp.");
        }

        [Test]
        public void Heal_ZeroAmount_Ignored()
        {
            _health.TakeDamage(20f); // 80 HP
            _health.Heal(0f);
            Assert.AreEqual(80f, _health.CurrentHp, 0.001f);
        }

        [Test]
        public void Heal_WhenDead_Ignored()
        {
            _health.TakeDamage(100f); // kill
            _health.Heal(50f);        // should be ignored
            Assert.AreEqual(0f, _health.CurrentHp, 0.001f,
                "Heal on a dead robot must have no effect.");
            Assert.IsFalse(_health.IsAlive);
        }

        // ── Re-Initialize after death ─────────────────────────────────────────

        [Test]
        public void Initialize_AfterDeath_ResetsHpAndIsAlive()
        {
            _health.TakeDamage(100f); // kill
            _health.Initialize();     // round reset

            Assert.AreEqual(100f, _health.CurrentHp, 0.001f);
            Assert.IsTrue(_health.IsAlive,
                "Initialize() must fully reset a dead robot to alive state.");
        }
    }
}
