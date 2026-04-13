using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WeaponCombatStatsBonusApplier"/>.
    ///
    /// Covers:
    ///   Fresh instance:
    ///   • BonusConfig is null.
    ///   • WeaponController is null.
    ///
    ///   Null-safety:
    ///   • OnEnable with null channels does not throw.
    ///   • OnDisable with null channels does not throw.
    ///   • Apply() with null config does not throw.
    ///   • Apply() with null controller does not throw.
    ///   • ResetBonus() with null controller does not throw.
    ///
    ///   Apply() — type mismatch:
    ///   • When weapon type ≠ RequiredWeaponType, no bonus is added.
    ///
    ///   Apply() — type match:
    ///   • When weapon type == RequiredWeaponType, FlatDamageBonus is added to controller.
    ///
    ///   ResetBonus():
    ///   • Clears controller DamageBonus to 0 after Apply has added a bonus.
    ///
    ///   OnDisable — unregisters:
    ///   • Raising _onMatchStarted after OnDisable does not re-apply bonus.
    ///   • Raising _onMatchEnded after OnDisable does not reset bonus.
    /// </summary>
    public class WeaponCombatStatsBonusApplierTests
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

        private static WeaponCombatStatsBonusSO CreateConfig(
            DamageType requiredType = DamageType.Physical,
            float      bonus        = 10f)
        {
            var so = ScriptableObject.CreateInstance<WeaponCombatStatsBonusSO>();
            SetField(so, "_requiredWeaponType", requiredType);
            SetField(so, "_flatDamageBonus",    bonus);
            return so;
        }

        private static WeaponPartSO CreateWeaponPart(DamageType type = DamageType.Physical)
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_damageType", type);
            SetField(so, "_baseDamage", 10f);
            return so;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_BonusConfig_IsNull()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponCombatStatsBonusApplier>();
            Assert.IsNull(app.BonusConfig);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_WeaponController_IsNull()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponCombatStatsBonusApplier>();
            Assert.IsNull(app.WeaponController);
            Object.DestroyImmediate(go);
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponCombatStatsBonusApplier>();
            SetField(app, "_onMatchStarted", null);
            SetField(app, "_onMatchEnded",   null);
            InvokePrivate(app, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(app, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannels_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponCombatStatsBonusApplier>();
            SetField(app, "_onMatchStarted", null);
            SetField(app, "_onMatchEnded",   null);
            InvokePrivate(app, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(app, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Apply_NullConfig_DoesNotThrow()
        {
            var go       = new GameObject();
            var app      = go.AddComponent<WeaponCombatStatsBonusApplier>();
            var ctlGo    = new GameObject();
            var ctl      = ctlGo.AddComponent<WeaponAttachmentController>();
            SetField(app, "_bonusConfig",      null);
            SetField(app, "_weaponController", ctl);
            Assert.DoesNotThrow(() => app.Apply());
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
        }

        [Test]
        public void Apply_NullController_DoesNotThrow()
        {
            var go     = new GameObject();
            var app    = go.AddComponent<WeaponCombatStatsBonusApplier>();
            var config = CreateConfig(DamageType.Energy, 15f);
            SetField(app, "_bonusConfig",      config);
            SetField(app, "_weaponController", null);
            Assert.DoesNotThrow(() => app.Apply());
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ResetBonus_NullController_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponCombatStatsBonusApplier>();
            SetField(app, "_weaponController", null);
            Assert.DoesNotThrow(() => app.ResetBonus());
            Object.DestroyImmediate(go);
        }

        // ── Apply() — type mismatch ───────────────────────────────────────────

        [Test]
        public void Apply_TypeMismatch_NoBonusAdded()
        {
            var go     = new GameObject();
            var app    = go.AddComponent<WeaponCombatStatsBonusApplier>();
            var config = CreateConfig(DamageType.Energy, 20f);   // requires Energy
            var ctlGo  = new GameObject();
            var ctl    = ctlGo.AddComponent<WeaponAttachmentController>();
            var part   = CreateWeaponPart(DamageType.Thermal);    // equipped with Thermal
            ctl.SetWeaponPart(part);

            SetField(app, "_bonusConfig",      config);
            SetField(app, "_weaponController", ctl);

            app.Apply();

            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f,
                "Type mismatch (Energy required, Thermal equipped) must not add any bonus.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(part);
        }

        // ── Apply() — type match ──────────────────────────────────────────────

        [Test]
        public void Apply_TypeMatches_BonusAddedToController()
        {
            var go     = new GameObject();
            var app    = go.AddComponent<WeaponCombatStatsBonusApplier>();
            var config = CreateConfig(DamageType.Shock, 15f);   // requires Shock, bonus 15
            var ctlGo  = new GameObject();
            var ctl    = ctlGo.AddComponent<WeaponAttachmentController>();
            var part   = CreateWeaponPart(DamageType.Shock);    // equipped Shock
            ctl.SetWeaponPart(part);

            SetField(app, "_bonusConfig",      config);
            SetField(app, "_weaponController", ctl);

            app.Apply();

            Assert.AreEqual(15f, ctl.DamageBonus, 0.001f,
                "Matching weapon type must add FlatDamageBonus to the controller.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(part);
        }

        // ── ResetBonus() ──────────────────────────────────────────────────────

        [Test]
        public void ResetBonus_ClearsControllerDamageBonusToZero()
        {
            var go     = new GameObject();
            var app    = go.AddComponent<WeaponCombatStatsBonusApplier>();
            var config = CreateConfig(DamageType.Physical, 10f);
            var ctlGo  = new GameObject();
            var ctl    = ctlGo.AddComponent<WeaponAttachmentController>();
            var part   = CreateWeaponPart(DamageType.Physical);
            ctl.SetWeaponPart(part);

            SetField(app, "_bonusConfig",      config);
            SetField(app, "_weaponController", ctl);

            app.Apply();
            Assert.AreEqual(10f, ctl.DamageBonus, 0.001f,
                "Precondition: bonus should be 10 after Apply.");

            app.ResetBonus();
            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f,
                "ResetBonus must clear DamageBonus back to 0.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(part);
        }

        // ── OnDisable — unregisters channels ─────────────────────────────────

        [Test]
        public void OnDisable_UnregistersMatchStarted_RaisingChannelNoLongerApplies()
        {
            var go      = new GameObject();
            var app     = go.AddComponent<WeaponCombatStatsBonusApplier>();
            var config  = CreateConfig(DamageType.Energy, 10f);
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();
            var part    = CreateWeaponPart(DamageType.Energy);
            ctl.SetWeaponPart(part);
            var startCh = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(app, "_bonusConfig",      config);
            SetField(app, "_weaponController", ctl);
            SetField(app, "_onMatchStarted",   startCh);
            SetField(app, "_onMatchEnded",     null);

            InvokePrivate(app, "Awake");
            InvokePrivate(app, "OnEnable");

            InvokePrivate(app, "OnDisable");

            // After OnDisable, the controller bonus has been accumulated once from Apply
            // called during OnEnable. Reset it manually so we can detect re-application.
            ctl.ResetDamageBonus();
            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f, "Precondition: bonus reset.");

            // Raising the channel should NOT re-apply (unregistered).
            startCh.Raise();
            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f,
                "After OnDisable, raising _onMatchStarted must not re-apply the bonus.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(startCh);
        }

        [Test]
        public void OnDisable_UnregistersMatchEnded_RaisingChannelNoLongerResets()
        {
            var go     = new GameObject();
            var app    = go.AddComponent<WeaponCombatStatsBonusApplier>();
            var config = CreateConfig(DamageType.Thermal, 8f);
            var ctlGo  = new GameObject();
            var ctl    = ctlGo.AddComponent<WeaponAttachmentController>();
            var part   = CreateWeaponPart(DamageType.Thermal);
            ctl.SetWeaponPart(part);
            var endCh  = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(app, "_bonusConfig",      config);
            SetField(app, "_weaponController", ctl);
            SetField(app, "_onMatchStarted",   null);
            SetField(app, "_onMatchEnded",     endCh);

            InvokePrivate(app, "Awake");
            InvokePrivate(app, "OnEnable");

            // Apply manually so we have a bonus to observe.
            app.Apply();
            Assert.AreEqual(8f, ctl.DamageBonus, 0.001f, "Precondition: bonus applied.");

            InvokePrivate(app, "OnDisable");

            // Raising end channel should NOT reset (unregistered).
            endCh.Raise();
            Assert.AreEqual(8f, ctl.DamageBonus, 0.001f,
                "After OnDisable, raising _onMatchEnded must not reset the bonus.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(endCh);
        }
    }
}
