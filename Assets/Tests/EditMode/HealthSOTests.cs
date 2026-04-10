using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="HealthSO"/>.
    ///
    /// Uses <c>ScriptableObject.CreateInstance</c> so the SO is allocated on the
    /// UnityEngine heap. The optional FloatGameEvent and VoidGameEvent channels
    /// are left null; HealthSO guards all raises with <c>?.</c>.
    ///
    /// Default values: MaxHealth = 100 (field initialiser in HealthSO.cs).
    /// </summary>
    public class HealthSOTests
    {
        private HealthSO _health;

        [SetUp]
        public void SetUp()
        {
            _health = ScriptableObject.CreateInstance<HealthSO>();
            _health.Reset(); // CurrentHealth = MaxHealth = 100
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_health);
            _health = null;
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_SetsCurrentHealthToMaxHealth()
        {
            Assert.AreEqual(_health.MaxHealth, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void Reset_AfterDamage_RestoresFullHealth()
        {
            _health.ApplyDamage(50f);
            _health.Reset();
            Assert.AreEqual(_health.MaxHealth, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void Reset_AfterDeath_ClearsIsDeadFlag()
        {
            _health.ApplyDamage(100f);
            Assert.IsTrue(_health.IsDead);
            _health.Reset();
            Assert.IsFalse(_health.IsDead);
        }

        // ── IsDead ────────────────────────────────────────────────────────────

        [Test]
        public void IsDead_False_WhenHealthAboveZero()
        {
            Assert.IsFalse(_health.IsDead);
        }

        [Test]
        public void IsDead_True_WhenHealthIsZero()
        {
            _health.ApplyDamage(100f);
            Assert.IsTrue(_health.IsDead);
        }

        // ── ApplyDamage ───────────────────────────────────────────────────────

        [Test]
        public void ApplyDamage_ReducesCurrentHealth()
        {
            _health.ApplyDamage(30f);
            Assert.AreEqual(70f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ApplyDamage_LethalHit_SetsHealthToZero()
        {
            _health.ApplyDamage(100f);
            Assert.AreEqual(0f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ApplyDamage_OverkillHit_ClampsToZero()
        {
            _health.ApplyDamage(9999f);
            Assert.AreEqual(0f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ApplyDamage_ZeroAmount_IsIgnored()
        {
            _health.ApplyDamage(0f);
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ApplyDamage_NegativeAmount_IsIgnored()
        {
            _health.ApplyDamage(-10f);
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ApplyDamage_WhenAlreadyDead_IsNoOp()
        {
            _health.ApplyDamage(100f); // kill
            _health.ApplyDamage(50f);  // must be ignored
            Assert.AreEqual(0f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ApplyDamage_Cumulative_DecreasesCorrectly()
        {
            _health.ApplyDamage(25f);
            _health.ApplyDamage(25f);
            Assert.AreEqual(50f, _health.CurrentHealth, 0.001f);
        }

        // ── Heal ──────────────────────────────────────────────────────────────

        [Test]
        public void Heal_AfterDamage_IncreasesCurrentHealth()
        {
            _health.ApplyDamage(50f);
            _health.Heal(20f);
            Assert.AreEqual(70f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void Heal_CapsAtMaxHealth()
        {
            _health.ApplyDamage(10f);
            _health.Heal(9999f);
            Assert.AreEqual(_health.MaxHealth, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void Heal_WhenDead_IsNoOp()
        {
            _health.ApplyDamage(100f);
            _health.Heal(50f);
            Assert.AreEqual(0f, _health.CurrentHealth, 0.001f);
            Assert.IsTrue(_health.IsDead);
        }

        [Test]
        public void Heal_ZeroAmount_IsIgnored()
        {
            _health.ApplyDamage(20f);
            _health.Heal(0f);
            Assert.AreEqual(80f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void Heal_NegativeAmount_IsIgnored()
        {
            _health.ApplyDamage(20f);
            _health.Heal(-10f);
            Assert.AreEqual(80f, _health.CurrentHealth, 0.001f);
        }

        // ── InitForMatch ──────────────────────────────────────────────────────

        [Test]
        public void InitForMatch_OverridesMaxHealth()
        {
            _health.InitForMatch(150f);
            Assert.AreEqual(150f, _health.MaxHealth, 0.001f);
        }

        [Test]
        public void InitForMatch_ResetUsesOverriddenMaxHealth()
        {
            _health.InitForMatch(200f);
            _health.Reset();
            Assert.AreEqual(200f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void InitForMatch_ZeroValue_ClampsToOne()
        {
            _health.InitForMatch(0f);
            Assert.GreaterOrEqual(_health.MaxHealth, 1f);
        }

        [Test]
        public void InitForMatch_NegativeValue_ClampsToOne()
        {
            _health.InitForMatch(-100f);
            Assert.GreaterOrEqual(_health.MaxHealth, 1f);
        }

        [Test]
        public void InitForMatch_HealCapsAtNewMaxHealth()
        {
            _health.InitForMatch(150f);
            _health.Reset();             // CurrentHealth = 150
            _health.ApplyDamage(50f);   // CurrentHealth = 100
            _health.Heal(9999f);        // should cap at 150, not 100 (old default)
            Assert.AreEqual(150f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void InitForMatch_CalledTwice_LastValueWins()
        {
            _health.InitForMatch(300f);
            _health.InitForMatch(80f);
            Assert.AreEqual(80f, _health.MaxHealth, 0.001f);
        }

        [Test]
        public void InitForMatch_DoesNotAffectCurrentHealthUntilReset()
        {
            // After SetUp, CurrentHealth == 100. Changing MaxHealth without Reset
            // should not immediately change CurrentHealth.
            _health.InitForMatch(200f);
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }
    }
}
