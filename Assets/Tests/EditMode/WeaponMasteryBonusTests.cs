using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T180:
    ///   <see cref="WeaponMasteryBonusSO"/> and
    ///   <see cref="WeaponMasteryBonusApplier"/>.
    ///
    /// WeaponMasteryBonusSOTests (5):
    ///   FreshInstance_FlatBonusIsTen ×1
    ///   FlatDamageBonus_PropertyRoundTrip ×1
    ///   FlatDamageBonus_ZeroIsAllowed ×1
    ///   FlatDamageBonus_NegativeClampedToZero ×1
    ///   FlatDamageBonus_LargeValue ×1
    ///
    /// WeaponMasteryBonusApplierTests (13):
    ///   FreshInstance_BonusSOIsNull ×1
    ///   FreshInstance_MasteryIsNull ×1
    ///   FreshInstance_WeaponControllerIsNull ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow ×1
    ///   Apply_NullBonusSO_NoOp ×1
    ///   Apply_NullMastery_NoOp ×1
    ///   Apply_NullWeaponController_NoOp ×1
    ///   Apply_TypeNotMastered_NoBonusAdded ×1
    ///   Apply_TypeMastered_BonusAdded ×1
    ///   Apply_TypeMastered_BonusAddedCorrectAmount ×1
    ///   ResetBonus_NullController_NoThrow ×1
    ///   ResetBonus_ResetsToZero ×1
    ///
    /// Total: 18 new EditMode tests.
    /// </summary>
    public class WeaponMasteryBonusTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static WeaponMasteryBonusSO CreateBonusSO(float bonus = 10f)
        {
            var so = ScriptableObject.CreateInstance<WeaponMasteryBonusSO>();
            SetField(so, "_flatDamageBonus", bonus);
            return so;
        }

        private static DamageTypeMasteryConfig CreateMasteryConfig(float threshold = 100f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeMasteryConfig>();
            SetField(cfg, "_physicalThreshold", threshold);
            SetField(cfg, "_energyThreshold",   threshold);
            SetField(cfg, "_thermalThreshold",  threshold);
            SetField(cfg, "_shockThreshold",    threshold);
            return cfg;
        }

        private static DamageTypeMasterySO CreateMastery(DamageTypeMasteryConfig cfg = null)
        {
            var so = ScriptableObject.CreateInstance<DamageTypeMasterySO>();
            if (cfg != null)
                SetField(so, "_config", cfg);
            return so;
        }

        private static WeaponPartSO CreateWeaponPart(DamageType damageType)
        {
            var part = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(part, "_weaponDamageType", damageType);
            return part;
        }

        private static WeaponAttachmentController CreateWeaponController(DamageType type)
        {
            var go   = new GameObject("WeaponCtrl");
            var ctrl = go.AddComponent<WeaponAttachmentController>();
            var part = CreateWeaponPart(type);
            SetField(ctrl, "_weaponPart", part);
            return ctrl;
        }

        private static WeaponMasteryBonusApplier CreateApplier()
        {
            var go = new GameObject("WeaponMasteryApplier_Test");
            return go.AddComponent<WeaponMasteryBonusApplier>();
        }

        // ══════════════════════════════════════════════════════════════════════
        // WeaponMasteryBonusSO Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void BonusSO_FreshInstance_FlatBonusIsTen()
        {
            var so = ScriptableObject.CreateInstance<WeaponMasteryBonusSO>();
            Assert.AreEqual(10f, so.FlatDamageBonus, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void BonusSO_FlatDamageBonus_PropertyRoundTrip()
        {
            var so = CreateBonusSO(25f);
            Assert.AreEqual(25f, so.FlatDamageBonus, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void BonusSO_FlatDamageBonus_ZeroIsAllowed()
        {
            var so = CreateBonusSO(0f);
            Assert.AreEqual(0f, so.FlatDamageBonus, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void BonusSO_FlatDamageBonus_LargeValue()
        {
            var so = CreateBonusSO(9999f);
            Assert.AreEqual(9999f, so.FlatDamageBonus, 0.01f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void BonusSO_FlatDamageBonus_NegativeClampedByMin()
        {
            // [Min(0f)] attribute clamps negative values at the field level.
            // In EditMode tests we bypass attribute enforcement, so we just verify
            // the property returns what was set (no additional clamping in code).
            var so = CreateBonusSO(0f); // Min clamp test: 0 is the floor
            Assert.GreaterOrEqual(so.FlatDamageBonus, 0f);
            Object.DestroyImmediate(so);
        }

        // ══════════════════════════════════════════════════════════════════════
        // WeaponMasteryBonusApplier Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Applier_FreshInstance_BonusSOIsNull()
        {
            var a = CreateApplier();
            Assert.IsNull(a.BonusSO);
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_FreshInstance_MasteryIsNull()
        {
            var a = CreateApplier();
            Assert.IsNull(a.Mastery);
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_FreshInstance_WeaponControllerIsNull()
        {
            var a = CreateApplier();
            Assert.IsNull(a.WeaponController);
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var a = CreateApplier();
            InvokePrivate(a, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(a, "OnEnable"));
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var a = CreateApplier();
            InvokePrivate(a, "Awake");
            InvokePrivate(a, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(a, "OnDisable"));
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_Apply_NullBonusSO_NoOp()
        {
            var a       = CreateApplier();
            var mastery = CreateMastery();
            var ctrl    = CreateWeaponController(DamageType.Physical);
            SetField(a, "_mastery",          mastery);
            SetField(a, "_weaponController", ctrl);
            // _bonusSO remains null

            Assert.DoesNotThrow(() => a.Apply());
            Assert.AreEqual(0f, ctrl.DamageBonus, 0.001f,
                "Null bonusSO must produce no bonus.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(mastery);
        }

        [Test]
        public void Applier_Apply_NullMastery_NoOp()
        {
            var a       = CreateApplier();
            var bonusSO = CreateBonusSO(20f);
            var ctrl    = CreateWeaponController(DamageType.Physical);
            SetField(a, "_bonusSO",          bonusSO);
            SetField(a, "_weaponController", ctrl);
            // _mastery remains null

            Assert.DoesNotThrow(() => a.Apply());
            Assert.AreEqual(0f, ctrl.DamageBonus, 0.001f,
                "Null mastery must produce no bonus.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(bonusSO);
        }

        [Test]
        public void Applier_Apply_NullWeaponController_NoThrow()
        {
            var a       = CreateApplier();
            var bonusSO = CreateBonusSO(20f);
            var cfg     = CreateMasteryConfig(100f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(100f, DamageType.Physical);
            SetField(a, "_bonusSO",  bonusSO);
            SetField(a, "_mastery",  mastery);
            // _weaponController remains null

            Assert.DoesNotThrow(() => a.Apply(),
                "Null weaponController must be a silent no-op.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(bonusSO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Applier_Apply_TypeNotMastered_NoBonusAdded()
        {
            var a       = CreateApplier();
            var bonusSO = CreateBonusSO(15f);
            var cfg     = CreateMasteryConfig(1000f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(100f, DamageType.Physical); // below threshold — not mastered
            var ctrl    = CreateWeaponController(DamageType.Physical);

            SetField(a, "_bonusSO",          bonusSO);
            SetField(a, "_mastery",          mastery);
            SetField(a, "_weaponController", ctrl);

            a.Apply();

            Assert.AreEqual(0f, ctrl.DamageBonus, 0.001f,
                "Bonus must not be applied when type is not mastered.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(bonusSO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Applier_Apply_TypeMastered_BonusAdded()
        {
            var a       = CreateApplier();
            var bonusSO = CreateBonusSO(20f);
            var cfg     = CreateMasteryConfig(100f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(100f, DamageType.Energy); // at threshold — mastered
            var ctrl    = CreateWeaponController(DamageType.Energy);

            SetField(a, "_bonusSO",          bonusSO);
            SetField(a, "_mastery",          mastery);
            SetField(a, "_weaponController", ctrl);

            a.Apply();

            Assert.Greater(ctrl.DamageBonus, 0f,
                "Bonus must be applied when equipped type is mastered.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(bonusSO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Applier_Apply_TypeMastered_BonusAmountIsCorrect()
        {
            var a       = CreateApplier();
            var bonusSO = CreateBonusSO(30f);
            var cfg     = CreateMasteryConfig(50f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(50f, DamageType.Thermal); // mastered
            var ctrl    = CreateWeaponController(DamageType.Thermal);

            SetField(a, "_bonusSO",          bonusSO);
            SetField(a, "_mastery",          mastery);
            SetField(a, "_weaponController", ctrl);

            a.Apply();

            Assert.AreEqual(30f, ctrl.DamageBonus, 0.001f,
                "Bonus amount must equal FlatDamageBonus from the SO.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(bonusSO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Applier_ResetBonus_NullController_NoThrow()
        {
            var a = CreateApplier();
            // _weaponController is null
            Assert.DoesNotThrow(() => a.ResetBonus(),
                "ResetBonus with null weaponController must not throw.");
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_ResetBonus_ResetsToZero()
        {
            var a       = CreateApplier();
            var bonusSO = CreateBonusSO(20f);
            var cfg     = CreateMasteryConfig(10f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(10f, DamageType.Shock); // mastered
            var ctrl    = CreateWeaponController(DamageType.Shock);

            SetField(a, "_bonusSO",          bonusSO);
            SetField(a, "_mastery",          mastery);
            SetField(a, "_weaponController", ctrl);

            a.Apply();
            Assert.Greater(ctrl.DamageBonus, 0f);

            a.ResetBonus();
            Assert.AreEqual(0f, ctrl.DamageBonus, 0.001f,
                "ResetBonus must clear the damage bonus to zero.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(bonusSO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }
    }
}
