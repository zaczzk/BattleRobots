using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WeaponAttachmentController"/>.
    ///
    /// Covers:
    ///   Fresh instance:
    ///   • WeaponPart is null.
    ///   • CurrentDamageType defaults to Physical when no weapon part is assigned.
    ///
    ///   SetWeaponPart:
    ///   • Sets the weapon part reference.
    ///   • Passing null clears the weapon part.
    ///
    ///   CurrentDamageType:
    ///   • Returns the weapon's DamageType when a part is assigned.
    ///
    ///   WeaponPart property:
    ///   • Returns the inspector-injected value.
    ///
    ///   Event handling:
    ///   • OnEnable with null fire event does not throw.
    ///   • OnDisable with null fire event does not throw.
    ///   • HandleFire with null weapon part does not throw.
    ///   • HandleFire with null damage event does not throw.
    ///   • HandleFire with valid part and event raises DamageInfo with correct type/amount/source.
    ///   • OnDisable unregisters callback — raising fire event after disable → no damage raised.
    /// </summary>
    public class WeaponAttachmentControllerTests
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

        private static void InvokePrivate(object target, string method, object[] args = null)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args ?? Array.Empty<object>());
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
        public void FreshInstance_WeaponPart_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            Assert.IsNull(ctl.WeaponPart);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_CurrentDamageType_IsPhysical()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            Assert.AreEqual(DamageType.Physical, ctl.CurrentDamageType);
            Object.DestroyImmediate(go);
        }

        // ── SetWeaponPart ─────────────────────────────────────────────────────

        [Test]
        public void SetWeaponPart_UpdatesWeaponPart()
        {
            var go   = new GameObject();
            var ctl  = go.AddComponent<WeaponAttachmentController>();
            var part = CreateWeaponPart(DamageType.Energy);
            ctl.SetWeaponPart(part);
            Assert.AreSame(part, ctl.WeaponPart);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void SetWeaponPart_Null_ClearsWeaponPart()
        {
            var go   = new GameObject();
            var ctl  = go.AddComponent<WeaponAttachmentController>();
            var part = CreateWeaponPart();
            ctl.SetWeaponPart(part);
            ctl.SetWeaponPart(null);
            Assert.IsNull(ctl.WeaponPart);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
        }

        // ── CurrentDamageType ─────────────────────────────────────────────────

        [Test]
        public void CurrentDamageType_WithEnergyWeapon_ReturnsEnergy()
        {
            var go   = new GameObject();
            var ctl  = go.AddComponent<WeaponAttachmentController>();
            var part = CreateWeaponPart(DamageType.Energy);
            ctl.SetWeaponPart(part);
            Assert.AreEqual(DamageType.Energy, ctl.CurrentDamageType);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
        }

        // ── WeaponPart property ───────────────────────────────────────────────

        [Test]
        public void WeaponPart_Property_ReturnsInjectedValue()
        {
            var go   = new GameObject();
            var ctl  = go.AddComponent<WeaponAttachmentController>();
            var part = CreateWeaponPart(DamageType.Thermal);
            SetField(ctl, "_weaponPart", part);
            Assert.AreSame(part, ctl.WeaponPart);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
        }

        // ── Event handling ────────────────────────────────────────────────────

        [Test]
        public void OnEnable_NullFireEvent_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            SetField(ctl, "_onFireEvent", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullFireEvent_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponAttachmentController>();
            SetField(ctl, "_onFireEvent", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void HandleFire_NullWeaponPart_DoesNotThrow()
        {
            var go          = new GameObject();
            var ctl         = go.AddComponent<WeaponAttachmentController>();
            var damageEvent = ScriptableObject.CreateInstance<DamageGameEvent>();
            SetField(ctl, "_weaponPart",     null);
            SetField(ctl, "_outDamageEvent", damageEvent);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "HandleFire"));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(damageEvent);
        }

        [Test]
        public void HandleFire_NullDamageEvent_DoesNotThrow()
        {
            var go   = new GameObject();
            var ctl  = go.AddComponent<WeaponAttachmentController>();
            var part = CreateWeaponPart(DamageType.Shock);
            SetField(ctl, "_weaponPart",     part);
            SetField(ctl, "_outDamageEvent", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "HandleFire"));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void HandleFire_ValidPartAndEvent_RaisesDamageInfoWithCorrectTypeAmountSource()
        {
            var go          = new GameObject();
            var ctl         = go.AddComponent<WeaponAttachmentController>();
            var part        = CreateWeaponPart(DamageType.Thermal, 20f);
            var damageEvent = ScriptableObject.CreateInstance<DamageGameEvent>();

            SetField(ctl, "_weaponPart",     part);
            SetField(ctl, "_outDamageEvent", damageEvent);
            SetField(ctl, "_sourceId",       "player_robot");

            DamageInfo received = default;
            bool wasCalled = false;
            Action<DamageInfo> cb = info => { received = info; wasCalled = true; };
            damageEvent.RegisterCallback(cb);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "HandleFire");

            Assert.IsTrue(wasCalled, "DamageGameEvent was not raised by HandleFire.");
            Assert.AreEqual(DamageType.Thermal,   received.damageType,        "DamageType mismatch.");
            Assert.AreEqual(20f,                   received.amount,    0.001f, "Amount mismatch.");
            Assert.AreEqual("player_robot",        received.sourceId,          "SourceId mismatch.");
            Assert.IsNull(received.statusEffect,                               "StatusEffect should be null.");

            damageEvent.UnregisterCallback(cb);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(damageEvent);
        }

        [Test]
        public void OnDisable_UnregistersCallback_FireEventNoLongerTriggersDamage()
        {
            var go          = new GameObject();
            var ctl         = go.AddComponent<WeaponAttachmentController>();
            var part        = CreateWeaponPart(DamageType.Energy, 15f);
            var fireEvent   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var damageEvent = ScriptableObject.CreateInstance<DamageGameEvent>();

            SetField(ctl, "_weaponPart",     part);
            SetField(ctl, "_onFireEvent",    fireEvent);
            SetField(ctl, "_outDamageEvent", damageEvent);

            int callCount = 0;
            Action<DamageInfo> cb = _ => callCount++;
            damageEvent.RegisterCallback(cb);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");
            fireEvent.Raise();
            Assert.AreEqual(1, callCount, "Should have fired once before disable.");

            InvokePrivate(ctl, "OnDisable");
            fireEvent.Raise();
            Assert.AreEqual(1, callCount, "Should not fire again after OnDisable unregisters.");

            damageEvent.UnregisterCallback(cb);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(fireEvent);
            Object.DestroyImmediate(damageEvent);
        }
    }
}
