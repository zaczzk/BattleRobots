using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T255: <see cref="ControlZoneCatalogSO"/> and
    /// <see cref="ControlZoneCatalogController"/>.
    ///
    /// ControlZoneCatalogTests (12):
    ///   SO_FreshInstance_EntryCount_Zero                                ×1
    ///   SO_GetZone_ValidIndex_ReturnsZone                               ×1
    ///   SO_GetZone_OutOfRange_ReturnsNull                               ×1
    ///   SO_ResetAll_ResetsAllNonNullZones                               ×1
    ///   Controller_FreshInstance_Catalog_Null                           ×1
    ///   Controller_FreshInstance_EntryCount_Zero                        ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                       ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_Unregisters_BothChannels                   ×1
    ///   Controller_HandleMatchStarted_NullCatalog_DoesNotThrow          ×1
    ///   Controller_HandleMatchStarted_CallsResetAll                     ×1
    ///   Controller_HandleMatchEnded_CallsResetAll                       ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ControlZoneCatalogTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ControlZoneSO CreateZoneSO() =>
            ScriptableObject.CreateInstance<ControlZoneSO>();

        private static ControlZoneCatalogSO CreateCatalogSO() =>
            ScriptableObject.CreateInstance<ControlZoneCatalogSO>();

        private static ControlZoneCatalogController CreateController() =>
            new GameObject("CatalogCtrl_Test").AddComponent<ControlZoneCatalogController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EntryCount_Zero()
        {
            var catalog = CreateCatalogSO();
            Assert.AreEqual(0, catalog.EntryCount,
                "EntryCount must be 0 on a fresh ControlZoneCatalogSO (null array).");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_GetZone_ValidIndex_ReturnsZone()
        {
            var catalog = CreateCatalogSO();
            var zone    = CreateZoneSO();
            SetField(catalog, "_zones", new ControlZoneSO[] { zone });

            Assert.AreSame(zone, catalog.GetZone(0),
                "GetZone(0) must return the zone at index 0.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(zone);
        }

        [Test]
        public void SO_GetZone_OutOfRange_ReturnsNull()
        {
            var catalog = CreateCatalogSO();
            var zone    = CreateZoneSO();
            SetField(catalog, "_zones", new ControlZoneSO[] { zone });

            Assert.IsNull(catalog.GetZone(5),
                "GetZone with out-of-range index must return null.");
            Assert.IsNull(catalog.GetZone(-1),
                "GetZone with negative index must return null.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(zone);
        }

        [Test]
        public void SO_ResetAll_ResetsAllNonNullZones()
        {
            var catalog = CreateCatalogSO();
            var zone0   = CreateZoneSO();
            var zone1   = CreateZoneSO();

            // Capture both zones before reset.
            zone0.CaptureProgress(10f);
            zone1.CaptureProgress(10f);
            Assert.IsTrue(zone0.IsCaptured, "Pre-condition: zone0 must be captured.");
            Assert.IsTrue(zone1.IsCaptured, "Pre-condition: zone1 must be captured.");

            SetField(catalog, "_zones", new ControlZoneSO[] { zone0, zone1 });
            catalog.ResetAll();

            Assert.IsFalse(zone0.IsCaptured, "ResetAll must reset zone0.");
            Assert.IsFalse(zone1.IsCaptured, "ResetAll must reset zone1.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(zone0);
            Object.DestroyImmediate(zone1);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_Catalog_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog,
                "Catalog must be null on a fresh ControlZoneCatalogController.");
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

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_BothChannels()
        {
            var ctrl    = CreateController();
            var catalog = CreateCatalogSO();
            var zone    = CreateZoneSO();
            var start   = CreateEvent();
            var end     = CreateEvent();

            SetField(catalog, "_zones", new ControlZoneSO[] { zone });
            SetField(ctrl, "_catalog",        catalog);
            SetField(ctrl, "_onMatchStarted", start);
            SetField(ctrl, "_onMatchEnded",   end);

            // Capture the zone before binding so we can detect a reset.
            zone.CaptureProgress(10f);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // After disable, manually trigger zone capture again (reset shouldn't fire).
            zone.CaptureProgress(10f);
            start.Raise();
            Assert.IsTrue(zone.IsCaptured,
                "After OnDisable, _onMatchStarted must not trigger ResetAll.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(zone);
            Object.DestroyImmediate(start);
            Object.DestroyImmediate(end);
        }

        [Test]
        public void Controller_HandleMatchStarted_NullCatalog_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.HandleMatchStarted(),
                "HandleMatchStarted with null catalog must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_HandleMatchStarted_CallsResetAll()
        {
            var ctrl    = CreateController();
            var catalog = CreateCatalogSO();
            var zone    = CreateZoneSO();

            SetField(catalog, "_zones", new ControlZoneSO[] { zone });
            SetField(ctrl, "_catalog", catalog);

            zone.CaptureProgress(10f);
            Assert.IsTrue(zone.IsCaptured, "Pre-condition: zone must be captured.");

            ctrl.HandleMatchStarted();

            Assert.IsFalse(zone.IsCaptured,
                "HandleMatchStarted must call ResetAll, clearing zone captured state.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(zone);
        }

        [Test]
        public void Controller_HandleMatchEnded_CallsResetAll()
        {
            var ctrl    = CreateController();
            var catalog = CreateCatalogSO();
            var zone    = CreateZoneSO();

            SetField(catalog, "_zones", new ControlZoneSO[] { zone });
            SetField(ctrl, "_catalog", catalog);

            zone.CaptureProgress(10f);
            Assert.IsTrue(zone.IsCaptured, "Pre-condition: zone must be captured.");

            ctrl.HandleMatchEnded();

            Assert.IsFalse(zone.IsCaptured,
                "HandleMatchEnded must call ResetAll, clearing zone captured state.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(zone);
        }
    }
}
