using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the shield-routing integration in <see cref="DamageReceiver"/>.
    ///
    /// Covers the new optional <c>_shield</c> field added in T088:
    ///   • No shield (null): TakeDamage behaviour is identical to the original.
    ///   • Shield active: damage is absorbed by ShieldController first; only leftover
    ///     reaches armor reduction and HealthSO.
    ///   • Shield + armor stacking: shield absorbs first, armor reduces the remainder.
    ///   • Shield absorbs all: HealthSO is untouched.
    ///   • DamageInfo overload routes correctly through the shield.
    ///
    /// Private fields (_health, _shield) are injected via reflection.
    /// ShieldController is placed on a child GameObject so DamageReceiver can hold
    /// a component reference via reflection (simulating Inspector wiring).
    /// </summary>
    public class DamageReceiverShieldTests
    {
        private GameObject       _go;
        private DamageReceiver   _receiver;
        private ShieldController _shieldController;
        private ShieldSO         _shieldSO;
        private ShieldConfig     _config;
        private HealthSO         _health;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go              = new GameObject("TestReceiverShield");
            _receiver        = _go.AddComponent<DamageReceiver>();
            _shieldController = _go.AddComponent<ShieldController>();

            _health   = ScriptableObject.CreateInstance<HealthSO>();
            _shieldSO = ScriptableObject.CreateInstance<ShieldSO>();
            _config   = ScriptableObject.CreateInstance<ShieldConfig>();

            // Wire health into DamageReceiver.
            SetField(_receiver, "_health", _health);
            _health.Reset();   // CurrentHealth = 100

            // Wire shield data into ShieldController.
            SetField(_shieldController, "_shield", _shieldSO);
            SetField(_shieldController, "_config", _config);
            _shieldController.ResetShield();   // CurrentShieldHP = 50 (config default)
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_health);
            Object.DestroyImmediate(_shieldSO);
            Object.DestroyImmediate(_config);
            _go              = null;
            _receiver        = null;
            _shieldController = null;
            _health          = null;
            _shieldSO        = null;
            _config          = null;
        }

        // ── No shield (backwards compat) ──────────────────────────────────────

        [Test]
        public void TakeDamage_NullShield_BehavesAsOriginal()
        {
            SetField(_receiver, "_shield", null);   // no shield
            _receiver.TakeDamage(30f);
            Assert.AreEqual(70f, _health.CurrentHealth, 0.001f);
        }

        // ── Shield active ─────────────────────────────────────────────────────

        [Test]
        public void TakeDamage_WithShield_DamageReducedByShieldAbsorption()
        {
            SetField(_receiver, "_shield", _shieldController);

            // Shield HP = 50. Damage = 20. Shield absorbs all 20 → leftover = 0.
            // Health should remain at 100.
            _receiver.TakeDamage(20f);
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
            Assert.AreEqual(30f,  _shieldSO.CurrentHP,   0.001f);
        }

        [Test]
        public void TakeDamage_DamageExceedsShield_LeftoverHitsHealth()
        {
            SetField(_receiver, "_shield", _shieldController);

            // Shield HP = 50. Damage = 70. Shield absorbs 50, leftover = 20.
            // Health should drop by 20 → 80.
            _receiver.TakeDamage(70f);
            Assert.AreEqual(80f, _health.CurrentHealth, 0.001f);
            Assert.AreEqual(0f,  _shieldSO.CurrentHP,   0.001f);
        }

        [Test]
        public void TakeDamage_ShieldAbsorbsAll_HealthUnchanged()
        {
            SetField(_receiver, "_shield", _shieldController);

            _receiver.TakeDamage(50f);   // exactly the shield capacity
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }

        // ── Shield + armor stacking ───────────────────────────────────────────

        [Test]
        public void TakeDamage_ShieldAndArmor_StackCorrectly()
        {
            SetField(_receiver, "_shield", _shieldController);
            _receiver.SetArmorRating(10);

            // Shield HP = 50. Damage = 80. Shield absorbs 50, leftover = 30.
            // Armor 10 reduces to 20. Health drops by 20 → 80.
            _receiver.TakeDamage(80f);
            Assert.AreEqual(80f, _health.CurrentHealth, 0.001f);
        }

        // ── DamageInfo overload ───────────────────────────────────────────────

        [Test]
        public void TakeDamage_DamageInfo_RoutedThroughShield()
        {
            SetField(_receiver, "_shield", _shieldController);

            var info = new DamageInfo(60f, "enemy", Vector3.zero);
            // Shield absorbs 50, leftover 10 → health drops by 10 → 90.
            _receiver.TakeDamage(info);
            Assert.AreEqual(90f, _health.CurrentHealth, 0.001f);
        }
    }
}
