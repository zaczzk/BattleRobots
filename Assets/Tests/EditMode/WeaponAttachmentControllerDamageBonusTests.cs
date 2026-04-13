using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the runtime damage-bonus patch applied to
    /// <see cref="WeaponAttachmentController"/> (T169).
    ///
    /// Covers:
    ///   Fresh instance:
    ///   • DamageBonus defaults to 0.
    ///
    ///   AddDamageBonus:
    ///   • Positive value accumulates correctly.
    ///   • Multiple calls accumulate additively.
    ///   • Zero value is ignored (bonus stays unchanged).
    ///   • Negative value is ignored (bonus stays unchanged).
    ///
    ///   ResetDamageBonus:
    ///   • Clears accumulated bonus back to 0.
    ///   • Reset on fresh instance (already 0) does not throw.
    ///
    ///   HandleFire with bonus:
    ///   • DamageInfo.amount equals BaseDamage + DamageBonus.
    ///   • Zero bonus → DamageInfo.amount equals BaseDamage (no change).
    /// </summary>
    public class WeaponAttachmentControllerDamageBonusTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static WeaponPartSO CreateWeaponPart(DamageType type = DamageType.Physical,
                                                      float damage    = 10f)
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_damageType", type);
            SetField(so, "_baseDamage", damage);
            return so;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_DamageBonus_IsZero()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f,
                "DamageBonus must be 0 on a fresh instance.");
            Object.DestroyImmediate(go);
        }

        // ── AddDamageBonus ────────────────────────────────────────────────────

        [Test]
        public void AddDamageBonus_PositiveValue_AccumulatesBonus()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            ctl.AddDamageBonus(15f);
            Assert.AreEqual(15f, ctl.DamageBonus, 0.001f,
                "AddDamageBonus(15) should set DamageBonus to 15.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AddDamageBonus_MultipleCalls_AccumulatesAdditively()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            ctl.AddDamageBonus(10f);
            ctl.AddDamageBonus(5f);
            Assert.AreEqual(15f, ctl.DamageBonus, 0.001f,
                "Two AddDamageBonus calls should accumulate additively.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AddDamageBonus_ZeroValue_BonusUnchanged()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            ctl.AddDamageBonus(10f);
            ctl.AddDamageBonus(0f);
            Assert.AreEqual(10f, ctl.DamageBonus, 0.001f,
                "AddDamageBonus(0) must not change the accumulated bonus.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AddDamageBonus_NegativeValue_BonusUnchanged()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            ctl.AddDamageBonus(10f);
            ctl.AddDamageBonus(-5f);
            Assert.AreEqual(10f, ctl.DamageBonus, 0.001f,
                "AddDamageBonus with a negative value must be ignored.");
            Object.DestroyImmediate(go);
        }

        // ── ResetDamageBonus ──────────────────────────────────────────────────

        [Test]
        public void ResetDamageBonus_ClearsBonusToZero()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            ctl.AddDamageBonus(20f);
            ctl.ResetDamageBonus();
            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f,
                "ResetDamageBonus must clear the accumulated bonus to 0.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResetDamageBonus_OnFreshInstance_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            Assert.DoesNotThrow(() => ctl.ResetDamageBonus(),
                "ResetDamageBonus on a fresh instance must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── HandleFire with bonus ─────────────────────────────────────────────

        [Test]
        public void HandleFire_WithBonus_AmountEqualsBasePlusBonus()
        {
            var go          = new GameObject();
            var ctl         = go.AddComponent<WeaponAttachmentController>();
            var part        = CreateWeaponPart(DamageType.Energy, 10f);
            var damageEvent = ScriptableObject.CreateInstance<DamageGameEvent>();

            SetField(ctl, "_weaponPart",     part);
            SetField(ctl, "_outDamageEvent", damageEvent);
            ctl.AddDamageBonus(5f);

            DamageInfo received = default;
            Action<DamageInfo> cb = info => received = info;
            damageEvent.RegisterCallback(cb);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "HandleFire");

            Assert.AreEqual(15f, received.amount, 0.001f,
                "HandleFire should include DamageBonus in the DamageInfo amount (10 base + 5 bonus = 15).");

            damageEvent.UnregisterCallback(cb);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(damageEvent);
        }

        [Test]
        public void HandleFire_ZeroBonus_AmountEqualsBaseDamage()
        {
            var go          = new GameObject();
            var ctl         = go.AddComponent<WeaponAttachmentController>();
            var part        = CreateWeaponPart(DamageType.Physical, 20f);
            var damageEvent = ScriptableObject.CreateInstance<DamageGameEvent>();

            SetField(ctl, "_weaponPart",     part);
            SetField(ctl, "_outDamageEvent", damageEvent);
            // No AddDamageBonus call — bonus stays 0.

            DamageInfo received = default;
            Action<DamageInfo> cb = info => received = info;
            damageEvent.RegisterCallback(cb);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "HandleFire");

            Assert.AreEqual(20f, received.amount, 0.001f,
                "With zero bonus, HandleFire amount must equal BaseDamage exactly.");

            damageEvent.UnregisterCallback(cb);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(damageEvent);
        }
    }
}
