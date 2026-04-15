using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T246: <see cref="HazardZoneGroupCatalogSO"/> and
    /// <see cref="HazardZoneGroupCatalogController"/>.
    ///
    /// HazardZoneGroupCatalogTests (12):
    ///   SO_FreshInstance_EntryCount_Zero                         ×1
    ///   SO_GetGroup_ValidIndex_ReturnsGroup                      ×1
    ///   SO_GetGroup_OutOfRange_ReturnsNull                       ×1
    ///   SO_GetGroup_NullGroups_ReturnsNull                       ×1
    ///   Controller_FreshInstance_CatalogNull                     ×1
    ///   Controller_FreshInstance_EntryCount_Zero                 ×1
    ///   Controller_ActivateGroup_ValidIndex_CallsActivate        ×1
    ///   Controller_DeactivateGroup_ValidIndex_CallsDeactivate    ×1
    ///   Controller_ToggleGroup_ValidIndex_CallsToggle            ×1
    ///   Controller_ActivateGroup_NullCatalog_DoesNotThrow        ×1
    ///   Controller_ActivateGroup_OutOfRange_DoesNotThrow         ×1
    ///   Controller_ActivateGroup_NullGroupEntry_DoesNotThrow     ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class HazardZoneGroupCatalogTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static HazardZoneGroupCatalogSO CreateCatalogSO() =>
            ScriptableObject.CreateInstance<HazardZoneGroupCatalogSO>();

        private static HazardZoneGroupCatalogController CreateController() =>
            new GameObject("CatalogCtrl_Test").AddComponent<HazardZoneGroupCatalogController>();

        private static HazardZoneGroupSO CreateGroupSO() =>
            ScriptableObject.CreateInstance<HazardZoneGroupSO>();

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EntryCount_Zero()
        {
            var so = CreateCatalogSO();
            Assert.AreEqual(0, so.EntryCount,
                "EntryCount must be 0 on a fresh HazardZoneGroupCatalogSO instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetGroup_ValidIndex_ReturnsGroup()
        {
            var so    = CreateCatalogSO();
            var group = CreateGroupSO();
            SetField(so, "_groups", new HazardZoneGroupSO[] { group });

            Assert.AreEqual(group, so.GetGroup(0),
                "GetGroup must return the correct HazardZoneGroupSO at the given index.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void SO_GetGroup_OutOfRange_ReturnsNull()
        {
            var so    = CreateCatalogSO();
            var group = CreateGroupSO();
            SetField(so, "_groups", new HazardZoneGroupSO[] { group });

            Assert.IsNull(so.GetGroup(-1),  "Negative index must return null.");
            Assert.IsNull(so.GetGroup(99),  "Out-of-range index must return null.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void SO_GetGroup_NullGroups_ReturnsNull()
        {
            var so = CreateCatalogSO();
            // _groups defaults to null on a fresh SO.
            Assert.IsNull(so.GetGroup(0),
                "GetGroup must return null when _groups is null.");
            Object.DestroyImmediate(so);
        }

        // ── Controller fresh-instance tests ───────────────────────────────────

        [Test]
        public void Controller_FreshInstance_CatalogNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog,
                "Catalog must be null on a fresh HazardZoneGroupCatalogController instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_EntryCount_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0, ctrl.EntryCount,
                "EntryCount must be 0 when Catalog is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── ActivateGroup / DeactivateGroup / ToggleGroup tests ───────────────

        [Test]
        public void Controller_ActivateGroup_ValidIndex_CallsActivate()
        {
            var ctrl    = CreateController();
            var catalog = CreateCatalogSO();
            var group   = CreateGroupSO();   // starts inactive
            SetField(catalog, "_groups", new HazardZoneGroupSO[] { group });
            SetField(ctrl, "_catalog", catalog);

            ctrl.ActivateGroup(0);

            Assert.IsTrue(group.IsGroupActive,
                "ActivateGroup must call Activate on the HazardZoneGroupSO at the given index.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Controller_DeactivateGroup_ValidIndex_CallsDeactivate()
        {
            var ctrl    = CreateController();
            var catalog = CreateCatalogSO();
            var group   = CreateGroupSO();
            group.Activate();   // start active
            SetField(catalog, "_groups", new HazardZoneGroupSO[] { group });
            SetField(ctrl, "_catalog", catalog);

            ctrl.DeactivateGroup(0);

            Assert.IsFalse(group.IsGroupActive,
                "DeactivateGroup must call Deactivate on the HazardZoneGroupSO at the given index.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Controller_ToggleGroup_ValidIndex_CallsToggle()
        {
            var ctrl    = CreateController();
            var catalog = CreateCatalogSO();
            var group   = CreateGroupSO();   // starts inactive
            SetField(catalog, "_groups", new HazardZoneGroupSO[] { group });
            SetField(ctrl, "_catalog", catalog);

            ctrl.ToggleGroup(0);   // inactive → active

            Assert.IsTrue(group.IsGroupActive,
                "ToggleGroup must call Toggle on the HazardZoneGroupSO at the given index.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Controller_ActivateGroup_NullCatalog_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.ActivateGroup(0),
                "ActivateGroup with null catalog must not throw.");
            Assert.DoesNotThrow(() => ctrl.DeactivateGroup(0),
                "DeactivateGroup with null catalog must not throw.");
            Assert.DoesNotThrow(() => ctrl.ToggleGroup(0),
                "ToggleGroup with null catalog must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_ActivateGroup_OutOfRange_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var catalog = CreateCatalogSO();
            var group   = CreateGroupSO();
            SetField(catalog, "_groups", new HazardZoneGroupSO[] { group });
            SetField(ctrl, "_catalog", catalog);

            Assert.DoesNotThrow(() => ctrl.ActivateGroup(99),
                "ActivateGroup with out-of-range index must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Controller_ActivateGroup_NullGroupEntry_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var catalog = CreateCatalogSO();
            // Catalog has a null entry at index 0.
            SetField(catalog, "_groups", new HazardZoneGroupSO[] { null });
            SetField(ctrl, "_catalog", catalog);

            Assert.DoesNotThrow(() => ctrl.ActivateGroup(0),
                "ActivateGroup with a null group entry must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
        }
    }
}
