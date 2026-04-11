using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PowerUpController"/>.
    ///
    /// Tests drive the controller through the public
    /// <see cref="PowerUpController.TriggerPickup"/> method, which contains the
    /// same logic as <c>OnTriggerEnter</c> but is callable without physics
    /// simulation. The respawn coroutine is not tested here (coroutines never
    /// resolve in EditMode).
    ///
    /// Also covers the patches added to <see cref="DamageReceiver"/>:
    ///   • <see cref="DamageReceiver.Heal"/> — delegates to HealthSO (null-safe).
    ///   • <see cref="DamageReceiver.RestoreShield"/> — delegates to ShieldController (null-safe).
    ///
    /// Covers:
    ///   • <see cref="PowerUpController.IsActive"/> — default true.
    ///   • TriggerPickup guard conditions (null powerUp, inactive, null DR, dead DR).
    ///   • TriggerPickup HealthRestore effect (HealthSO.CurrentHealth increases).
    ///   • TriggerPickup ShieldRecharge effect (ShieldSO.CurrentHP increases).
    ///   • TriggerPickup fires optional pickup event.
    ///   • TriggerPickup sets IsActive = false and hides _visualRoot.
    ///   • TriggerPickup with null _visualRoot does not throw.
    ///   • OnDisable does not throw (StopCoroutine guard).
    ///   • DamageReceiver.Heal with null HealthSO does not throw.
    ///   • DamageReceiver.RestoreShield with null ShieldController does not throw.
    /// </summary>
    public class PowerUpControllerTests
    {
        // ── Shared fixtures ───────────────────────────────────────────────────

        private GameObject       _pickupGo;
        private PowerUpController _ctrl;

        private GameObject     _robotGo;
        private DamageReceiver _dr;
        private HealthSO       _health;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private PowerUpSO MakePowerUp(PowerUpType type, float amount = 20f)
        {
            var pu = ScriptableObject.CreateInstance<PowerUpSO>();
            SetField(pu, "_type",         type);
            SetField(pu, "_effectAmount", amount);
            return pu;
        }

        private static void InjectHealth(DamageReceiver dr, HealthSO health)
        {
            FieldInfo fi = typeof(DamageReceiver)
                .GetField("_health", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_health' not found on DamageReceiver.");
            fi.SetValue(dr, health);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Pickup
            _pickupGo = new GameObject("Pickup");
            _ctrl     = _pickupGo.AddComponent<PowerUpController>();

            // Robot with health (50 / 100 HP)
            _robotGo = new GameObject("Robot");
            _dr      = _robotGo.AddComponent<DamageReceiver>();
            _health  = ScriptableObject.CreateInstance<HealthSO>();
            _health.Reset();                  // CurrentHealth = MaxHealth = 100
            _health.ApplyDamage(50f);         // CurrentHealth = 50
            InjectHealth(_dr, _health);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_pickupGo);
            Object.DestroyImmediate(_robotGo);
            Object.DestroyImmediate(_health);

            _pickupGo = null;
            _ctrl     = null;
            _robotGo  = null;
            _dr       = null;
            _health   = null;
        }

        // ── IsActive default ──────────────────────────────────────────────────

        [Test]
        public void IsActive_Default_IsTrue()
        {
            var go   = new GameObject("FreshPickup");
            var ctrl = go.AddComponent<PowerUpController>();
            Assert.IsTrue(ctrl.IsActive);
            Object.DestroyImmediate(go);
        }

        // ── TriggerPickup guard conditions ────────────────────────────────────

        [Test]
        public void TriggerPickup_NullPowerUp_DoesNotThrow()
        {
            // _powerUp defaults to null — must not throw.
            Assert.DoesNotThrow(() => _ctrl.TriggerPickup(_dr));
        }

        [Test]
        public void TriggerPickup_NullPowerUp_LeavesHealthUnchanged()
        {
            float before = _health.CurrentHealth;
            _ctrl.TriggerPickup(_dr);
            Assert.AreEqual(before, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void TriggerPickup_InactivePowerUp_NoEffect()
        {
            var pu = MakePowerUp(PowerUpType.HealthRestore, 30f);
            SetField(_ctrl, "_powerUp", pu);
            SetField(_ctrl, "_isActive", false);

            float before = _health.CurrentHealth;
            _ctrl.TriggerPickup(_dr);
            Assert.AreEqual(before, _health.CurrentHealth, 0.001f,
                "Inactive pickup should not heal.");

            Object.DestroyImmediate(pu);
        }

        [Test]
        public void TriggerPickup_NullDamageReceiver_DoesNotThrow()
        {
            var pu = MakePowerUp(PowerUpType.HealthRestore, 20f);
            SetField(_ctrl, "_powerUp", pu);

            Assert.DoesNotThrow(() => _ctrl.TriggerPickup(null));

            Object.DestroyImmediate(pu);
        }

        [Test]
        public void TriggerPickup_DeadDamageReceiver_NoEffect()
        {
            var pu = MakePowerUp(PowerUpType.HealthRestore, 20f);
            SetField(_ctrl, "_powerUp", pu);

            // Kill the robot
            _health.ApplyDamage(_health.MaxHealth);
            Assert.IsTrue(_dr.IsDead);

            float before = _health.CurrentHealth;
            _ctrl.TriggerPickup(_dr);
            Assert.AreEqual(before, _health.CurrentHealth, 0.001f,
                "Dead robot should not receive healing.");

            Object.DestroyImmediate(pu);
        }

        // ── HealthRestore effect ──────────────────────────────────────────────

        [Test]
        public void TriggerPickup_HealthRestore_HealsDamageReceiver()
        {
            // Health is at 50; pickup restores 20 → should reach 70.
            var pu = MakePowerUp(PowerUpType.HealthRestore, 20f);
            SetField(_ctrl, "_powerUp", pu);

            _ctrl.TriggerPickup(_dr);

            Assert.AreEqual(70f, _health.CurrentHealth, 0.001f,
                "HealthRestore should heal 20 HP (50 → 70).");

            Object.DestroyImmediate(pu);
        }

        // ── ShieldRecharge effect ─────────────────────────────────────────────

        [Test]
        public void TriggerPickup_ShieldRecharge_RestoresShieldHP()
        {
            // Build a ShieldSO at 10 / 50 HP (depleted by 40).
            var shieldSO = ScriptableObject.CreateInstance<ShieldSO>();
            shieldSO.Reset(50f);         // MaxHP = 50, CurrentHP = 50
            shieldSO.AbsorbDamage(40f);  // CurrentHP = 10

            // Build a ShieldController and inject ShieldSO.
            var shieldGo   = new GameObject("ShieldCtrl");
            var shieldCtrl = shieldGo.AddComponent<ShieldController>();
            SetField(shieldCtrl, "_shield", shieldSO);

            // Wire DamageReceiver._shield → ShieldController.
            SetField(_dr, "_shield", shieldCtrl);

            var pu = MakePowerUp(PowerUpType.ShieldRecharge, 15f);
            SetField(_ctrl, "_powerUp", pu);

            _ctrl.TriggerPickup(_dr);

            Assert.AreEqual(25f, shieldSO.CurrentHP, 0.001f,
                "ShieldRecharge should add 15 HP to shield (10 → 25).");

            Object.DestroyImmediate(pu);
            Object.DestroyImmediate(shieldGo);
            Object.DestroyImmediate(shieldSO);
        }

        // ── Optional pickup event ─────────────────────────────────────────────

        [Test]
        public void TriggerPickup_FiresPickedUpEvent()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            var pu  = MakePowerUp(PowerUpType.HealthRestore, 10f);
            SetField(pu, "_onPickedUp", evt);
            SetField(_ctrl, "_powerUp", pu);

            int fireCount = 0;
            Action listener = () => fireCount++;
            evt.RegisterCallback(listener);

            _ctrl.TriggerPickup(_dr);
            Assert.AreEqual(1, fireCount, "_onPickedUp should fire exactly once on pickup.");

            evt.UnregisterCallback(listener);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(pu);
        }

        // ── IsActive and visual root ──────────────────────────────────────────

        [Test]
        public void TriggerPickup_SetsIsActive_ToFalse()
        {
            var pu = MakePowerUp(PowerUpType.HealthRestore, 10f);
            SetField(_ctrl, "_powerUp", pu);

            _ctrl.TriggerPickup(_dr);
            Assert.IsFalse(_ctrl.IsActive,
                "IsActive should be false immediately after a successful pickup.");

            Object.DestroyImmediate(pu);
        }

        [Test]
        public void TriggerPickup_HidesVisualRoot()
        {
            var visual = new GameObject("Visual");
            visual.SetActive(true);
            SetField(_ctrl, "_visualRoot", visual);

            var pu = MakePowerUp(PowerUpType.HealthRestore, 10f);
            SetField(_ctrl, "_powerUp", pu);

            _ctrl.TriggerPickup(_dr);
            Assert.IsFalse(visual.activeSelf,
                "_visualRoot should be hidden after pickup.");

            Object.DestroyImmediate(visual);
            Object.DestroyImmediate(pu);
        }

        [Test]
        public void TriggerPickup_NullVisualRoot_DoesNotThrow()
        {
            var pu = MakePowerUp(PowerUpType.HealthRestore, 10f);
            SetField(_ctrl, "_powerUp", pu);
            SetField(_ctrl, "_visualRoot", null);   // explicit null

            Assert.DoesNotThrow(() => _ctrl.TriggerPickup(_dr));

            Object.DestroyImmediate(pu);
        }

        // ── OnDisable ─────────────────────────────────────────────────────────

        [Test]
        public void OnDisable_DoesNotThrow()
        {
            // Disable without any coroutine running — should be a safe no-op.
            Assert.DoesNotThrow(() => _pickupGo.SetActive(false));
        }

        // ── DamageReceiver.Heal null-safety ───────────────────────────────────

        [Test]
        public void DamageReceiver_Heal_NullHealth_DoesNotThrow()
        {
            // Create a DamageReceiver with no HealthSO wired.
            var go = new GameObject("NullHealthRobot");
            var dr = go.AddComponent<DamageReceiver>();
            // _health defaults to null — Heal must not throw.
            Assert.DoesNotThrow(() => dr.Heal(25f));
            Object.DestroyImmediate(go);
        }

        // ── DamageReceiver.RestoreShield null-safety ──────────────────────────

        [Test]
        public void DamageReceiver_RestoreShield_NullShield_DoesNotThrow()
        {
            // Create a DamageReceiver with no ShieldController wired.
            var go = new GameObject("NullShieldRobot");
            var dr = go.AddComponent<DamageReceiver>();
            // _shield defaults to null — RestoreShield must not throw.
            Assert.DoesNotThrow(() => dr.RestoreShield(15f));
            Object.DestroyImmediate(go);
        }
    }
}
