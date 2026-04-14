using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the M27 Staggered Weapon Bonus system (T175):
    ///   <see cref="StaggeredWeaponBonusCatalogSO"/> and
    ///   <see cref="StaggeredWeaponBonusApplier"/>.
    ///
    /// StaggeredWeaponBonusCatalogSOTests (4):
    ///   Fresh instance — Bonuses list is empty.
    ///   Catalog exposes all registered entries via IReadOnlyList.
    ///   Null entry in catalog list is accessible via Bonuses property.
    ///   Catalog with two entries returns count 2.
    ///
    /// StaggeredWeaponBonusApplierTests (12):
    ///   Fresh instance — Catalog is null.
    ///   Fresh instance — WeaponController is null.
    ///   OnEnable with null channels does not throw.
    ///   OnDisable with null channels does not throw.
    ///   Apply() with null catalog does not throw.
    ///   Apply() with null weapon controller does not throw.
    ///   ResetBonus() with null weapon controller does not throw.
    ///   Apply() — no matching type — no bonus added.
    ///   Apply() — single matching bonus — correct bonus added.
    ///   Apply() — two matching bonuses — both stack additively.
    ///   Apply() — mixed types — only matching type stacked.
    ///   ResetBonus() — clears accumulated bonus to zero.
    ///   OnDisable — unregisters match-started channel.
    ///   OnDisable — unregisters match-ended channel.
    ///
    /// Total: 16 new EditMode tests.
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class StaggeredWeaponBonusTests
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

        private static WeaponCombatStatsBonusSO CreateBonusSO(
            DamageType requiredType, float bonus)
        {
            var so = ScriptableObject.CreateInstance<WeaponCombatStatsBonusSO>();
            SetField(so, "_requiredWeaponType", requiredType);
            SetField(so, "_flatDamageBonus",    bonus);
            return so;
        }

        private static WeaponPartSO CreateWeaponPart(DamageType type)
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_damageType", type);
            SetField(so, "_baseDamage", 10f);
            return so;
        }

        private static StaggeredWeaponBonusCatalogSO CreateCatalog(
            params WeaponCombatStatsBonusSO[] entries)
        {
            var cat = ScriptableObject.CreateInstance<StaggeredWeaponBonusCatalogSO>();
            SetField(cat, "_bonuses", new List<WeaponCombatStatsBonusSO>(entries));
            return cat;
        }

        // ══════════════════════════════════════════════════════════════════════
        // StaggeredWeaponBonusCatalogSO Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Catalog_FreshInstance_BonusesListIsEmpty()
        {
            var cat = ScriptableObject.CreateInstance<StaggeredWeaponBonusCatalogSO>();
            Assert.AreEqual(0, cat.Bonuses.Count,
                "A freshly created catalog should have zero bonus entries.");
            Object.DestroyImmediate(cat);
        }

        [Test]
        public void Catalog_TwoEntries_CountIsTwo()
        {
            var b1  = CreateBonusSO(DamageType.Physical, 10f);
            var b2  = CreateBonusSO(DamageType.Energy, 15f);
            var cat = CreateCatalog(b1, b2);

            Assert.AreEqual(2, cat.Bonuses.Count,
                "Catalog with two entries must expose count 2.");

            Object.DestroyImmediate(cat);
            Object.DestroyImmediate(b1);
            Object.DestroyImmediate(b2);
        }

        [Test]
        public void Catalog_NullEntry_IsAccessibleViaBonuses()
        {
            // The catalog SO itself permits null entries (applier skips them).
            var cat = ScriptableObject.CreateInstance<StaggeredWeaponBonusCatalogSO>();
            SetField(cat, "_bonuses", new List<WeaponCombatStatsBonusSO> { null });

            Assert.AreEqual(1, cat.Bonuses.Count,
                "Null entry must be visible in Bonuses (applier is responsible for skipping it).");
            Assert.IsNull(cat.Bonuses[0],
                "Null entry in the list must remain null when accessed via Bonuses.");

            Object.DestroyImmediate(cat);
        }

        [Test]
        public void Catalog_ExposesAllEntries_ViaIReadOnlyList()
        {
            var b1  = CreateBonusSO(DamageType.Thermal, 5f);
            var b2  = CreateBonusSO(DamageType.Shock,  20f);
            var b3  = CreateBonusSO(DamageType.Physical, 12f);
            var cat = CreateCatalog(b1, b2, b3);

            Assert.AreEqual(3, cat.Bonuses.Count,
                "Catalog with three entries must expose all three.");
            Assert.AreSame(b1, cat.Bonuses[0]);
            Assert.AreSame(b2, cat.Bonuses[1]);
            Assert.AreSame(b3, cat.Bonuses[2]);

            Object.DestroyImmediate(cat);
            Object.DestroyImmediate(b1);
            Object.DestroyImmediate(b2);
            Object.DestroyImmediate(b3);
        }

        // ══════════════════════════════════════════════════════════════════════
        // StaggeredWeaponBonusApplier Tests — Fresh instance
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Applier_FreshInstance_CatalogIsNull()
        {
            var go  = new GameObject();
            var app = go.AddComponent<StaggeredWeaponBonusApplier>();
            Assert.IsNull(app.Catalog,
                "Catalog should default to null on a freshly added applier.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Applier_FreshInstance_WeaponControllerIsNull()
        {
            var go  = new GameObject();
            var app = go.AddComponent<StaggeredWeaponBonusApplier>();
            Assert.IsNull(app.WeaponController,
                "WeaponController should default to null on a freshly added applier.");
            Object.DestroyImmediate(go);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Null-safety
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Applier_OnEnable_NullChannels_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<StaggeredWeaponBonusApplier>();
            SetField(app, "_onMatchStarted", null);
            SetField(app, "_onMatchEnded",   null);
            InvokePrivate(app, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(app, "OnEnable"),
                "OnEnable with null channels must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Applier_OnDisable_NullChannels_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<StaggeredWeaponBonusApplier>();
            SetField(app, "_onMatchStarted", null);
            SetField(app, "_onMatchEnded",   null);
            InvokePrivate(app, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(app, "OnDisable"),
                "OnDisable with null channels must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Applier_Apply_NullCatalog_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<StaggeredWeaponBonusApplier>();
            SetField(app, "_catalog", null);
            Assert.DoesNotThrow(() => app.Apply(),
                "Apply() with null catalog must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Applier_Apply_NullController_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<StaggeredWeaponBonusApplier>();
            var cat = CreateCatalog(CreateBonusSO(DamageType.Physical, 10f));
            SetField(app, "_catalog",          cat);
            SetField(app, "_weaponController", null);
            Assert.DoesNotThrow(() => app.Apply(),
                "Apply() with null weapon controller must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cat);
        }

        [Test]
        public void Applier_ResetBonus_NullController_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<StaggeredWeaponBonusApplier>();
            SetField(app, "_weaponController", null);
            Assert.DoesNotThrow(() => app.ResetBonus(),
                "ResetBonus() with null controller must not throw.");
            Object.DestroyImmediate(go);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Apply() — behaviour
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Applier_Apply_NoMatchingType_NoBonusAdded()
        {
            // Catalog has Energy bonus; weapon is equipped with Thermal → no bonus.
            var go      = new GameObject();
            var app     = go.AddComponent<StaggeredWeaponBonusApplier>();
            var bonus   = CreateBonusSO(DamageType.Energy, 20f);
            var cat     = CreateCatalog(bonus);
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();
            var part    = CreateWeaponPart(DamageType.Thermal);
            ctl.SetWeaponPart(part);

            SetField(app, "_catalog",          cat);
            SetField(app, "_weaponController", ctl);

            app.Apply();

            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f,
                "No bonus should be added when weapon type does not match any catalog entry.");

            Object.DestroyImmediate(go); Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(cat); Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Applier_Apply_SingleMatchingBonus_CorrectValueAdded()
        {
            var go    = new GameObject();
            var app   = go.AddComponent<StaggeredWeaponBonusApplier>();
            var bonus = CreateBonusSO(DamageType.Physical, 12f);
            var cat   = CreateCatalog(bonus);
            var ctlGo = new GameObject();
            var ctl   = ctlGo.AddComponent<WeaponAttachmentController>();
            var part  = CreateWeaponPart(DamageType.Physical);
            ctl.SetWeaponPart(part);

            SetField(app, "_catalog",          cat);
            SetField(app, "_weaponController", ctl);

            app.Apply();

            Assert.AreEqual(12f, ctl.DamageBonus, 0.001f,
                "A single matching catalog entry must add its full FlatDamageBonus.");

            Object.DestroyImmediate(go); Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(cat); Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Applier_Apply_TwoMatchingBonuses_Stack()
        {
            // Two Physical bonuses (10 + 15 = 25) in the catalog; weapon is Physical.
            var go     = new GameObject();
            var app    = go.AddComponent<StaggeredWeaponBonusApplier>();
            var bonus1 = CreateBonusSO(DamageType.Physical, 10f);
            var bonus2 = CreateBonusSO(DamageType.Physical, 15f);
            var cat    = CreateCatalog(bonus1, bonus2);
            var ctlGo  = new GameObject();
            var ctl    = ctlGo.AddComponent<WeaponAttachmentController>();
            var part   = CreateWeaponPart(DamageType.Physical);
            ctl.SetWeaponPart(part);

            SetField(app, "_catalog",          cat);
            SetField(app, "_weaponController", ctl);

            app.Apply();

            Assert.AreEqual(25f, ctl.DamageBonus, 0.001f,
                "Two matching catalog entries must stack additively (10 + 15 = 25).");

            Object.DestroyImmediate(go); Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(cat); Object.DestroyImmediate(bonus1);
            Object.DestroyImmediate(bonus2); Object.DestroyImmediate(part);
        }

        [Test]
        public void Applier_Apply_MixedTypes_OnlyMatchingTypeStacked()
        {
            // Catalog has Physical (10) + Energy (20); weapon is Physical → only 10 added.
            var go      = new GameObject();
            var app     = go.AddComponent<StaggeredWeaponBonusApplier>();
            var physBon = CreateBonusSO(DamageType.Physical, 10f);
            var engBon  = CreateBonusSO(DamageType.Energy,  20f);
            var cat     = CreateCatalog(physBon, engBon);
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();
            var part    = CreateWeaponPart(DamageType.Physical);
            ctl.SetWeaponPart(part);

            SetField(app, "_catalog",          cat);
            SetField(app, "_weaponController", ctl);

            app.Apply();

            Assert.AreEqual(10f, ctl.DamageBonus, 0.001f,
                "Only the Physical catalog entry (10) should be applied, not the Energy entry (20).");

            Object.DestroyImmediate(go); Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(cat); Object.DestroyImmediate(physBon);
            Object.DestroyImmediate(engBon); Object.DestroyImmediate(part);
        }

        [Test]
        public void Applier_ResetBonus_ClearsAccumulatedBonusToZero()
        {
            var go    = new GameObject();
            var app   = go.AddComponent<StaggeredWeaponBonusApplier>();
            var bonus = CreateBonusSO(DamageType.Shock, 18f);
            var cat   = CreateCatalog(bonus);
            var ctlGo = new GameObject();
            var ctl   = ctlGo.AddComponent<WeaponAttachmentController>();
            var part  = CreateWeaponPart(DamageType.Shock);
            ctl.SetWeaponPart(part);

            SetField(app, "_catalog",          cat);
            SetField(app, "_weaponController", ctl);

            app.Apply();
            Assert.AreEqual(18f, ctl.DamageBonus, 0.001f, "Precondition: bonus applied.");

            app.ResetBonus();
            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f,
                "ResetBonus must clear the accumulated damage bonus to zero.");

            Object.DestroyImmediate(go); Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(cat); Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(part);
        }

        // ══════════════════════════════════════════════════════════════════════
        // OnDisable — unregisters channels
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Applier_OnDisable_UnregistersMatchStarted()
        {
            var go      = new GameObject();
            var app     = go.AddComponent<StaggeredWeaponBonusApplier>();
            var bonus   = CreateBonusSO(DamageType.Energy, 10f);
            var cat     = CreateCatalog(bonus);
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();
            var part    = CreateWeaponPart(DamageType.Energy);
            ctl.SetWeaponPart(part);
            var startCh = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(app, "_catalog",          cat);
            SetField(app, "_weaponController", ctl);
            SetField(app, "_onMatchStarted",   startCh);
            SetField(app, "_onMatchEnded",     null);

            InvokePrivate(app, "Awake");
            InvokePrivate(app, "OnEnable");
            InvokePrivate(app, "OnDisable");

            // Reset the bonus so we can detect re-application.
            ctl.ResetDamageBonus();
            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f, "Precondition: bonus reset.");

            startCh.Raise();
            Assert.AreEqual(0f, ctl.DamageBonus, 0.001f,
                "After OnDisable, raising _onMatchStarted must not re-apply the bonus.");

            Object.DestroyImmediate(go); Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(cat); Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(part); Object.DestroyImmediate(startCh);
        }

        [Test]
        public void Applier_OnDisable_UnregistersMatchEnded()
        {
            var go    = new GameObject();
            var app   = go.AddComponent<StaggeredWeaponBonusApplier>();
            var bonus = CreateBonusSO(DamageType.Thermal, 8f);
            var cat   = CreateCatalog(bonus);
            var ctlGo = new GameObject();
            var ctl   = ctlGo.AddComponent<WeaponAttachmentController>();
            var part  = CreateWeaponPart(DamageType.Thermal);
            ctl.SetWeaponPart(part);
            var endCh = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(app, "_catalog",          cat);
            SetField(app, "_weaponController", ctl);
            SetField(app, "_onMatchStarted",   null);
            SetField(app, "_onMatchEnded",     endCh);

            InvokePrivate(app, "Awake");
            InvokePrivate(app, "OnEnable");

            // Apply manually to accumulate a bonus.
            app.Apply();
            Assert.AreEqual(8f, ctl.DamageBonus, 0.001f, "Precondition: bonus applied.");

            InvokePrivate(app, "OnDisable");

            // Raising end channel should NOT reset (unregistered).
            endCh.Raise();
            Assert.AreEqual(8f, ctl.DamageBonus, 0.001f,
                "After OnDisable, raising _onMatchEnded must not reset the bonus.");

            Object.DestroyImmediate(go); Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(cat); Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(part); Object.DestroyImmediate(endCh);
        }
    }
}
