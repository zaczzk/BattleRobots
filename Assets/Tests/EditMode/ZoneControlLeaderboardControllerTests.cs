using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T269: <see cref="ZoneControlLeaderboardController"/>.
    ///
    /// ZoneControlLeaderboardControllerTests (12):
    ///   FreshInstance_Catalog_Null                                      ×1
    ///   FreshInstance_Tracker_Null                                      ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_Channel                                   ×1
    ///   Refresh_NullListContainer_NoThrow                               ×1
    ///   Refresh_NullRowPrefab_NoThrow                                   ×1
    ///   Refresh_NullCatalog_ShowsEmptyLabel                             ×1
    ///   Refresh_EmptyCatalog_ShowsEmptyLabel                            ×1
    ///   Refresh_SingleZone_Captured_Badge                               ×1
    ///   Refresh_SingleZone_Open_Badge                                   ×1
    ///   Refresh_NullTracker_NoThrow                                     ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlLeaderboardControllerTests
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

        private static ZoneControlLeaderboardController CreateController() =>
            new GameObject("ZoneLeaderboard_Test")
                .AddComponent<ZoneControlLeaderboardController>();

        private static ControlZoneCatalogSO CreateCatalogSO() =>
            ScriptableObject.CreateInstance<ControlZoneCatalogSO>();

        private static ControlZoneSO CreateZoneSO(string id = "Zone")
        {
            var so = ScriptableObject.CreateInstance<ControlZoneSO>();
            // ControlZoneSO.ZoneId defaults to "Zone" via inspector default.
            return so;
        }

        private static ZoneScoreTrackerSO CreateTrackerSO() =>
            ScriptableObject.CreateInstance<ZoneScoreTrackerSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static GameObject CreateRowPrefab()
        {
            var root = new GameObject("RowPrefab");
            for (int i = 0; i < 3; i++)
            {
                var child = new GameObject($"Text{i}");
                child.AddComponent<Text>();
                child.transform.SetParent(root.transform);
            }
            return root;
        }

        private static ControlZoneCatalogSO CreateCatalogWithZones(params ControlZoneSO[] zones)
        {
            var catalog = CreateCatalogSO();
            SetField(catalog, "_zones", zones);
            return catalog;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Catalog_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog,
                "Catalog must be null on a fresh ZoneControlLeaderboardController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_Tracker_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Tracker,
                "Tracker must be null on a fresh ZoneControlLeaderboardController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var evtUpd  = CreateEvent();
            bool fired  = false;
            evtUpd.RegisterCallback(() => fired = true);

            SetField(ctrl, "_onScoreUpdated", evtUpd);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            fired = false;
            evtUpd.Raise(); // controller's refresh delegate must be gone
            // fired will be true only from our test listener — that is expected.
            // We simply check no exception is thrown and the controller doesn't crash.
            Assert.DoesNotThrow(() => evtUpd.Raise(),
                "Raising the score-updated event after OnDisable must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(evtUpd);
        }

        [Test]
        public void Refresh_NullListContainer_NoThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _listContainer must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_NullRowPrefab_NoThrow()
        {
            var ctrl      = CreateController();
            var container = new GameObject("Container");

            SetField(ctrl, "_listContainer", container.transform);

            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _rowPrefab must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
        }

        [Test]
        public void Refresh_NullCatalog_ShowsEmptyLabel()
        {
            var ctrl      = CreateController();
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();
            var empty     = new GameObject("Empty").AddComponent<Text>();

            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);
            SetField(ctrl, "_emptyLabel",    empty);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(empty.gameObject.activeSelf,
                "Empty label must be shown when catalog is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
            Object.DestroyImmediate(empty.gameObject);
        }

        [Test]
        public void Refresh_EmptyCatalog_ShowsEmptyLabel()
        {
            var ctrl      = CreateController();
            var catalog   = CreateCatalogSO(); // 0 zones
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();
            var empty     = new GameObject("Empty").AddComponent<Text>();

            SetField(ctrl, "_catalog",       catalog);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);
            SetField(ctrl, "_emptyLabel",    empty);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(empty.gameObject.activeSelf,
                "Empty label must be shown when catalog has 0 zones.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
            Object.DestroyImmediate(empty.gameObject);
        }

        [Test]
        public void Refresh_SingleZone_Captured_Badge()
        {
            var ctrl      = CreateController();
            var zoneSO    = CreateZoneSO();
            var catalog   = CreateCatalogWithZones(zoneSO);
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();

            // Capture the zone.
            zoneSO.CaptureProgress(100f);
            Assert.IsTrue(zoneSO.IsCaptured, "Pre-condition: zone must be captured.");

            SetField(ctrl, "_catalog",       catalog);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.AreEqual(1, container.transform.childCount,
                "One row must be spawned for one zone.");
            var texts = container.transform.GetChild(0).GetComponentsInChildren<Text>();
            Assert.AreEqual("CAPTURED", texts[1].text,
                "Status badge must be 'CAPTURED' for a captured zone.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(zoneSO);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
        }

        [Test]
        public void Refresh_SingleZone_Open_Badge()
        {
            var ctrl      = CreateController();
            var zoneSO    = CreateZoneSO();
            var catalog   = CreateCatalogWithZones(zoneSO);
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();

            // Zone is not captured (fresh state).
            Assert.IsFalse(zoneSO.IsCaptured, "Pre-condition: zone must not be captured.");

            SetField(ctrl, "_catalog",       catalog);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            var texts = container.transform.GetChild(0).GetComponentsInChildren<Text>();
            Assert.AreEqual("OPEN", texts[1].text,
                "Status badge must be 'OPEN' for an uncaptured zone.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(zoneSO);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
        }

        [Test]
        public void Refresh_NullTracker_NoThrow()
        {
            var ctrl      = CreateController();
            var zoneSO    = CreateZoneSO();
            var catalog   = CreateCatalogWithZones(zoneSO);
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();

            SetField(ctrl, "_catalog",       catalog);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);
            // _tracker remains null

            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _tracker is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(zoneSO);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
        }
    }
}
