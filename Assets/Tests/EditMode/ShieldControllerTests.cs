using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ShieldController"/>.
    ///
    /// Covers:
    ///   • Null-safety: no throws when _shield or _config are null.
    ///   • <see cref="ShieldController.AbsorbDamage"/>: null-shield pass-through;
    ///     full/partial/over-capacity absorption; depletion pass-through.
    ///   • <see cref="ShieldController.ResetShield"/>: restores full HP; null-safe.
    ///   • Accessors <see cref="ShieldController.CurrentShieldHP"/> and
    ///     <see cref="ShieldController.IsShieldActive"/>: correct values.
    ///
    /// Private fields injected via reflection (same pattern as DamageReceiverArmorTests).
    /// Awake runs automatically when AddComponent is called; tests call ResetShield()
    /// explicitly after wiring to ensure a known initial state.
    /// </summary>
    public class ShieldControllerTests
    {
        private GameObject       _go;
        private ShieldController _controller;
        private ShieldSO         _shieldSO;
        private ShieldConfig     _config;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void Wire(ShieldSO shield = null, ShieldConfig config = null)
        {
            SetField(_controller, "_shield", shield ?? _shieldSO);
            SetField(_controller, "_config", config ?? _config);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go         = new GameObject("TestShieldController");
            _controller = _go.AddComponent<ShieldController>();   // Awake runs; refs are null so no-op
            _shieldSO   = ScriptableObject.CreateInstance<ShieldSO>();
            _config     = ScriptableObject.CreateInstance<ShieldConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_shieldSO);
            Object.DestroyImmediate(_config);
            _go         = null;
            _controller = null;
            _shieldSO   = null;
            _config     = null;
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void ResetShield_NullShield_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SetField(_controller, "_shield", null);
                SetField(_controller, "_config", _config);
                _controller.ResetShield();
            });
        }

        [Test]
        public void ResetShield_NullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SetField(_controller, "_shield", _shieldSO);
                SetField(_controller, "_config", null);
                _controller.ResetShield();
            });
        }

        // ── AbsorbDamage ──────────────────────────────────────────────────────

        [Test]
        public void AbsorbDamage_NullShield_PassesThroughFull()
        {
            SetField(_controller, "_shield", null);
            float leftover = _controller.AbsorbDamage(40f);
            Assert.AreEqual(40f, leftover, 0.001f);
        }

        [Test]
        public void AbsorbDamage_WithFullShield_ReducesDamage()
        {
            Wire();
            _controller.ResetShield();       // shield = MaxHP (50 by default)
            float maxHP  = _shieldSO.MaxHP;
            float damage = maxHP * 0.5f;     // half the shield

            float leftover = _controller.AbsorbDamage(damage);
            Assert.AreEqual(0f, leftover, 0.001f);
            Assert.AreEqual(maxHP - damage, _shieldSO.CurrentHP, 0.001f);
        }

        [Test]
        public void AbsorbDamage_ExceedsShield_ReturnsLeftover()
        {
            Wire();
            _controller.ResetShield();
            float maxHP  = _shieldSO.MaxHP;
            float damage = maxHP + 20f;      // 20 more than shield capacity

            float leftover = _controller.AbsorbDamage(damage);
            Assert.AreEqual(20f, leftover, 0.001f);
            Assert.AreEqual(0f,  _shieldSO.CurrentHP, 0.001f);
        }

        [Test]
        public void AbsorbDamage_WhenShieldDepleted_PassesFullDamage()
        {
            Wire();
            _controller.ResetShield();
            _shieldSO.AbsorbDamage(_shieldSO.MaxHP);   // deplete manually

            float leftover = _controller.AbsorbDamage(30f);
            Assert.AreEqual(30f, leftover, 0.001f);
        }

        // ── ResetShield ───────────────────────────────────────────────────────

        [Test]
        public void ResetShield_RestoresFullHP()
        {
            Wire();
            _controller.ResetShield();
            _shieldSO.AbsorbDamage(15f);    // partially deplete
            _controller.ResetShield();
            Assert.AreEqual(_shieldSO.MaxHP, _shieldSO.CurrentHP, 0.001f);
        }

        [Test]
        public void ResetShield_BothNull_NoThrow()
        {
            SetField(_controller, "_shield", null);
            SetField(_controller, "_config", null);
            Assert.DoesNotThrow(() => _controller.ResetShield());
        }

        // ── CurrentShieldHP ───────────────────────────────────────────────────

        [Test]
        public void CurrentShieldHP_NullShield_ReturnsZero()
        {
            SetField(_controller, "_shield", null);
            Assert.AreEqual(0f, _controller.CurrentShieldHP, 0.001f);
        }

        [Test]
        public void CurrentShieldHP_WithShield_ReturnsCurrentHP()
        {
            Wire();
            _controller.ResetShield();
            _shieldSO.AbsorbDamage(10f);
            Assert.AreEqual(_shieldSO.CurrentHP, _controller.CurrentShieldHP, 0.001f);
        }

        // ── IsShieldActive ────────────────────────────────────────────────────

        [Test]
        public void IsShieldActive_NullShield_ReturnsFalse()
        {
            SetField(_controller, "_shield", null);
            Assert.IsFalse(_controller.IsShieldActive);
        }

        [Test]
        public void IsShieldActive_WithFullShield_ReturnsTrue()
        {
            Wire();
            _controller.ResetShield();
            Assert.IsTrue(_controller.IsShieldActive);
        }

        [Test]
        public void IsShieldActive_WhenDepleted_ReturnsFalse()
        {
            Wire();
            _controller.ResetShield();
            _shieldSO.AbsorbDamage(_shieldSO.MaxHP);   // deplete
            Assert.IsFalse(_controller.IsShieldActive);
        }
    }
}
