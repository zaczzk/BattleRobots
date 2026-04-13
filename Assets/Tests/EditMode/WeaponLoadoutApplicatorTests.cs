using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WeaponLoadoutApplicator"/>.
    ///
    /// Covers:
    ///   Fresh instance:
    ///   • Catalog / PlayerLoadout / WeaponController all null.
    ///
    ///   Null-safety:
    ///   • OnEnable with null channel does not throw.
    ///   • OnDisable with null channel does not throw.
    ///   • ApplyWeapon with null loadout does not throw.
    ///   • ApplyWeapon with null catalog does not throw.
    ///   • ApplyWeapon with null controller does not throw.
    ///
    ///   ApplyWeapon behaviour:
    ///   • Empty loadout clears weapon to null on controller.
    ///   • Matching PartId in loadout resolves and applies WeaponPartSO.
    ///   • No matching PartId in loadout sets null on controller.
    ///
    ///   OnDisable:
    ///   • Unregisters delegate — raising channel after disable does not re-apply weapon.
    /// </summary>
    public class WeaponLoadoutApplicatorTests
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

        private static void AddToListField<T>(object target, string fieldName, T item)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found.");
            var list = (List<T>)fi.GetValue(target);
            list.Add(item);
        }

        /// <summary>Creates a WeaponPartSO with an optional PartDefinition with the given PartId.</summary>
        private static WeaponPartSO CreateWeaponPart(string partId = null,
                                                      DamageType type = DamageType.Physical)
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_damageType", type);
            if (partId != null)
            {
                var def = ScriptableObject.CreateInstance<PartDefinition>();
                SetField(def, "_partId", partId);
                SetField(so, "_partDefinition", def);
            }
            return so;
        }

        /// <summary>Creates a WeaponPartCatalogSO with one entry for the given weapon part.</summary>
        private static WeaponPartCatalogSO CreateCatalog(WeaponPartSO entry = null)
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            if (entry != null)
                AddToListField(catalog, "_parts", entry);
            return catalog;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Catalog_IsNull()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponLoadoutApplicator>();
            Assert.IsNull(app.Catalog);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_PlayerLoadout_IsNull()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponLoadoutApplicator>();
            Assert.IsNull(app.PlayerLoadout);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_WeaponController_IsNull()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponLoadoutApplicator>();
            Assert.IsNull(app.WeaponController);
            Object.DestroyImmediate(go);
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponLoadoutApplicator>();
            SetField(app, "_onLoadoutConfirmed", null);
            InvokePrivate(app, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(app, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var app = go.AddComponent<WeaponLoadoutApplicator>();
            SetField(app, "_onLoadoutConfirmed", null);
            InvokePrivate(app, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(app, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyWeapon_NullLoadout_DoesNotThrow()
        {
            var go      = new GameObject();
            var app     = go.AddComponent<WeaponLoadoutApplicator>();
            var catalog = CreateCatalog();
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();
            SetField(app, "_playerLoadout", null);
            SetField(app, "_catalog",       catalog);
            SetField(app, "_weaponController", ctl);
            Assert.DoesNotThrow(() => app.ApplyWeapon());
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ApplyWeapon_NullCatalog_DoesNotThrow()
        {
            var go      = new GameObject();
            var app     = go.AddComponent<WeaponLoadoutApplicator>();
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();
            SetField(app, "_playerLoadout",    loadout);
            SetField(app, "_catalog",          null);
            SetField(app, "_weaponController", ctl);
            Assert.DoesNotThrow(() => app.ApplyWeapon());
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(loadout);
        }

        [Test]
        public void ApplyWeapon_NullController_DoesNotThrow()
        {
            var go      = new GameObject();
            var app     = go.AddComponent<WeaponLoadoutApplicator>();
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            var catalog = CreateCatalog();
            SetField(app, "_playerLoadout",    loadout);
            SetField(app, "_catalog",          catalog);
            SetField(app, "_weaponController", null);
            Assert.DoesNotThrow(() => app.ApplyWeapon());
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(catalog);
        }

        // ── ApplyWeapon behaviour ─────────────────────────────────────────────

        [Test]
        public void ApplyWeapon_EmptyLoadout_SetsNullOnController()
        {
            var go      = new GameObject();
            var app     = go.AddComponent<WeaponLoadoutApplicator>();
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            // empty loadout — no EquippedPartIds
            var part    = CreateWeaponPart("wp_laser");
            var catalog = CreateCatalog(part);
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();
            // Pre-set a weapon on the controller so we can observe it being cleared.
            ctl.SetWeaponPart(part);

            SetField(app, "_playerLoadout",    loadout);
            SetField(app, "_catalog",          catalog);
            SetField(app, "_weaponController", ctl);

            app.ApplyWeapon();

            Assert.IsNull(ctl.WeaponPart,
                "Empty loadout should clear the weapon controller (SetWeaponPart null).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(catalog);
            if (part.PartDefinition != null) Object.DestroyImmediate(part.PartDefinition);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void ApplyWeapon_MatchingPartId_SetsWeaponPart()
        {
            var go      = new GameObject();
            var app     = go.AddComponent<WeaponLoadoutApplicator>();
            var part    = CreateWeaponPart("wp_shock", DamageType.Shock);
            var catalog = CreateCatalog(part);
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new[] { "wp_shock" });
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();

            SetField(app, "_playerLoadout",    loadout);
            SetField(app, "_catalog",          catalog);
            SetField(app, "_weaponController", ctl);

            app.ApplyWeapon();

            Assert.AreSame(part, ctl.WeaponPart,
                "Matching PartId should resolve to the WeaponPartSO and apply it to the controller.");
            Assert.AreEqual(DamageType.Shock, ctl.CurrentDamageType,
                "Controller's CurrentDamageType should reflect the applied weapon.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(catalog);
            if (part.PartDefinition != null) Object.DestroyImmediate(part.PartDefinition);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void ApplyWeapon_NoMatchingPartId_SetsNullOnController()
        {
            var go      = new GameObject();
            var app     = go.AddComponent<WeaponLoadoutApplicator>();
            var part    = CreateWeaponPart("wp_thermal");
            var catalog = CreateCatalog(part);
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new[] { "wp_nonexistent" });
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();
            // Pre-set a weapon on the controller.
            ctl.SetWeaponPart(part);

            SetField(app, "_playerLoadout",    loadout);
            SetField(app, "_catalog",          catalog);
            SetField(app, "_weaponController", ctl);

            app.ApplyWeapon();

            Assert.IsNull(ctl.WeaponPart,
                "No matching PartId in catalog should clear the weapon to null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(catalog);
            if (part.PartDefinition != null) Object.DestroyImmediate(part.PartDefinition);
            Object.DestroyImmediate(part);
        }

        // ── OnDisable — unregisters ───────────────────────────────────────────

        [Test]
        public void OnDisable_Unregisters_ChannelNoLongerAppliesWeapon()
        {
            // After OnDisable, raising _onLoadoutConfirmed should NOT trigger ApplyWeapon.
            var go      = new GameObject();
            var app     = go.AddComponent<WeaponLoadoutApplicator>();
            var part    = CreateWeaponPart("wp_energy", DamageType.Energy);
            var catalog = CreateCatalog(part);
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new[] { "wp_energy" });
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var ctlGo   = new GameObject();
            var ctl     = ctlGo.AddComponent<WeaponAttachmentController>();

            SetField(app, "_playerLoadout",       loadout);
            SetField(app, "_catalog",             catalog);
            SetField(app, "_weaponController",    ctl);
            SetField(app, "_onLoadoutConfirmed",  channel);

            // Awake + OnEnable → ApplyWeapon called once; weapon = part.
            InvokePrivate(app, "Awake");
            InvokePrivate(app, "OnEnable");
            Assert.AreSame(part, ctl.WeaponPart, "Weapon should be applied after OnEnable.");

            // OnDisable → unregisters channel.
            InvokePrivate(app, "OnDisable");

            // Change the loadout to a non-matching ID so that if ApplyWeapon DID run,
            // the weapon would be cleared to null.
            loadout.SetLoadout(new[] { "wp_nonexistent" });

            // Raise channel — should NOT trigger ApplyWeapon (unregistered).
            channel.Raise();

            Assert.AreSame(part, ctl.WeaponPart,
                "After OnDisable, raising the channel should not re-apply (unregistered). " +
                "WeaponPart should remain the original part set during OnEnable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ctlGo);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(channel);
            if (part.PartDefinition != null) Object.DestroyImmediate(part.PartDefinition);
            Object.DestroyImmediate(part);
        }
    }
}
