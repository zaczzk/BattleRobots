using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ShieldSO"/>.
    ///
    /// Covers:
    ///   • <see cref="ShieldSO.Reset"/>: sets MaxHP / CurrentHP; fires _onShieldChanged;
    ///     negative values clamped to zero.
    ///   • <see cref="ShieldSO.IsActive"/>: true when HP > 0, false otherwise.
    ///   • <see cref="ShieldSO.AbsorbDamage"/>: zero/negative pass-through; partial,
    ///     full, and over-capacity absorption; event firing; depletion event.
    ///   • <see cref="ShieldSO.Recharge"/>: HP increase; clamp to MaxHP; no-op when
    ///     full; _onShieldChanged and _onShieldRecharged event firing.
    ///
    /// Private fields injected via reflection to wire SO event channels.
    /// </summary>
    public class ShieldSOTests
    {
        private ShieldSO       _so;
        private FloatGameEvent _onShieldChanged;
        private VoidGameEvent  _onShieldDepleted;
        private VoidGameEvent  _onShieldRecharged;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void WireEvents()
        {
            SetField(_so, "_onShieldChanged",  _onShieldChanged);
            SetField(_so, "_onShieldDepleted", _onShieldDepleted);
            SetField(_so, "_onShieldRecharged", _onShieldRecharged);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so                = ScriptableObject.CreateInstance<ShieldSO>();
            _onShieldChanged   = ScriptableObject.CreateInstance<FloatGameEvent>();
            _onShieldDepleted  = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onShieldRecharged = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onShieldChanged);
            Object.DestroyImmediate(_onShieldDepleted);
            Object.DestroyImmediate(_onShieldRecharged);
            _so                = null;
            _onShieldChanged   = null;
            _onShieldDepleted  = null;
            _onShieldRecharged = null;
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_SetsCurrentHPToMaxHP()
        {
            _so.Reset(80f);
            Assert.AreEqual(80f, _so.CurrentHP, 0.001f);
            Assert.AreEqual(80f, _so.MaxHP,     0.001f);
        }

        [Test]
        public void Reset_ZeroMaxHP_BothZero()
        {
            _so.Reset(0f);
            Assert.AreEqual(0f, _so.CurrentHP, 0.001f);
            Assert.AreEqual(0f, _so.MaxHP,     0.001f);
        }

        [Test]
        public void Reset_NegativeMaxHP_ClampsToZero()
        {
            _so.Reset(-10f);
            Assert.AreEqual(0f, _so.MaxHP, 0.001f);
        }

        [Test]
        public void Reset_FiresShieldChangedEvent()
        {
            WireEvents();
            float received = -1f;
            _onShieldChanged.RegisterCallback(v => received = v);
            _so.Reset(60f);
            Assert.AreEqual(60f, received, 0.001f);
        }

        // ── IsActive ──────────────────────────────────────────────────────────

        [Test]
        public void IsActive_TrueWhenHPPositive()
        {
            _so.Reset(50f);
            Assert.IsTrue(_so.IsActive);
        }

        [Test]
        public void IsActive_FalseWhenHPZero()
        {
            _so.Reset(0f);
            Assert.IsFalse(_so.IsActive);
        }

        // ── AbsorbDamage ──────────────────────────────────────────────────────

        [Test]
        public void AbsorbDamage_ZeroAmount_ReturnsZero()
        {
            _so.Reset(50f);
            float leftover = _so.AbsorbDamage(0f);
            Assert.AreEqual(0f, leftover, 0.001f);
            Assert.AreEqual(50f, _so.CurrentHP, 0.001f);   // unchanged
        }

        [Test]
        public void AbsorbDamage_NegativeAmount_PassesThroughUnchanged()
        {
            _so.Reset(50f);
            float leftover = _so.AbsorbDamage(-5f);
            Assert.AreEqual(-5f, leftover, 0.001f);
        }

        [Test]
        public void AbsorbDamage_PartialAbsorption_ReturnsLeftover()
        {
            _so.Reset(30f);
            float leftover = _so.AbsorbDamage(50f);   // shield absorbs 30, leftover = 20
            Assert.AreEqual(20f, leftover, 0.001f);
            Assert.AreEqual(0f,  _so.CurrentHP, 0.001f);
        }

        [Test]
        public void AbsorbDamage_FullAbsorption_ReturnsZero()
        {
            _so.Reset(50f);
            float leftover = _so.AbsorbDamage(30f);
            Assert.AreEqual(0f,  leftover, 0.001f);
            Assert.AreEqual(20f, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void AbsorbDamage_WhenShieldEmpty_PassesThroughAll()
        {
            _so.Reset(0f);
            float leftover = _so.AbsorbDamage(40f);
            Assert.AreEqual(40f, leftover, 0.001f);
        }

        [Test]
        public void AbsorbDamage_FiresShieldChangedEvent()
        {
            WireEvents();
            _so.Reset(50f);
            float received = -1f;
            _onShieldChanged.RegisterCallback(v => received = v);
            _so.AbsorbDamage(10f);
            Assert.AreEqual(40f, received, 0.001f);
        }

        [Test]
        public void AbsorbDamage_Depletion_FiresShieldDepletedEvent()
        {
            WireEvents();
            _so.Reset(20f);
            bool fired = false;
            _onShieldDepleted.RegisterCallback(() => fired = true);
            _so.AbsorbDamage(25f);   // exceeds shield
            Assert.IsTrue(fired);
        }

        [Test]
        public void AbsorbDamage_PartialHit_DoesNotFireDepletedEvent()
        {
            WireEvents();
            _so.Reset(50f);
            bool fired = false;
            _onShieldDepleted.RegisterCallback(() => fired = true);
            _so.AbsorbDamage(10f);   // shield not depleted
            Assert.IsFalse(fired);
        }

        // ── Recharge ─────────────────────────────────────────────────────────

        [Test]
        public void Recharge_IncreasesHP()
        {
            _so.Reset(50f);
            _so.AbsorbDamage(20f);   // HP = 30
            _so.Recharge(5f);
            Assert.AreEqual(35f, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void Recharge_ClampsToMaxHP()
        {
            _so.Reset(50f);
            _so.AbsorbDamage(10f);   // HP = 40
            _so.Recharge(100f);      // would overshoot
            Assert.AreEqual(50f, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void Recharge_WhenFull_NoEventFired()
        {
            WireEvents();
            _so.Reset(50f);
            // Register after Reset so we only capture post-Reset events.
            int callCount = 0;
            _onShieldChanged.RegisterCallback(_ => callCount++);
            _so.Recharge(10f);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Recharge_FiresShieldChangedEvent()
        {
            WireEvents();
            _so.Reset(50f);
            _so.AbsorbDamage(20f);   // HP = 30
            float received = -1f;
            _onShieldChanged.RegisterCallback(v => received = v);
            _so.Recharge(5f);
            Assert.AreEqual(35f, received, 0.001f);
        }

        [Test]
        public void Recharge_FromEmpty_FiresShieldRecharged()
        {
            WireEvents();
            _so.Reset(50f);
            _so.AbsorbDamage(50f);   // deplete
            bool fired = false;
            _onShieldRecharged.RegisterCallback(() => fired = true);
            _so.Recharge(5f);
            Assert.IsTrue(fired);
        }

        [Test]
        public void Recharge_FromPartial_DoesNotFireShieldRecharged()
        {
            WireEvents();
            _so.Reset(50f);
            _so.AbsorbDamage(20f);   // HP = 30 (not fully depleted)
            bool fired = false;
            _onShieldRecharged.RegisterCallback(() => fired = true);
            _so.Recharge(5f);
            Assert.IsFalse(fired);
        }
    }
}
