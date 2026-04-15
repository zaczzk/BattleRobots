using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T276: <see cref="ZonePresenceLeaderboardController"/>.
    ///
    /// Tests (12):
    ///   FreshInstance_TimerSO_Null                                         ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   OnDisable_Unregisters                                              ×1
    ///   Refresh_NullListContainer_NoOp                                     ×1
    ///   Refresh_NullRowPrefab_NoOp                                         ×1
    ///   Refresh_NullTimerSO_ShowsEmptyLabel                                ×1
    ///   Refresh_ZeroZones_ShowsEmptyLabel                                  ×1
    ///   Refresh_SingleZone_ShowsPresenceTime                               ×1
    ///   Refresh_MultiZone_SortedDescending                                 ×1
    ///   Refresh_TopZone_RankBadgeActive                                    ×1
    ///   OnPresenceUpdated_Raise_Refreshes                                  ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZonePresenceLeaderboardControllerTests
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

        private static ZonePresenceLeaderboardController CreateController() =>
            new GameObject("ZonePresenceLeaderboard_Test")
                .AddComponent<ZonePresenceLeaderboardController>();

        private static ZonePresenceTimerSO CreateTimerSO(int maxZones = 3)
        {
            var so = ScriptableObject.CreateInstance<ZonePresenceTimerSO>();
            SetField(so, "_maxZones", maxZones);
            // Trigger OnEnable to initialise the internal array.
            so.Reset();
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        /// <summary>
        /// Creates a minimal row prefab with two Text children.
        /// </summary>
        private static GameObject CreateRowPrefab(bool withBadge = false)
        {
            var root = new GameObject("RowPrefab");
            root.AddComponent<Text>(); // texts[0]

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(root.transform);
            labelGO.AddComponent<Text>(); // texts[1]

            if (withBadge)
            {
                var badgeGO = new GameObject("Badge");
                badgeGO.transform.SetParent(root.transform);
                badgeGO.AddComponent<Image>();
            }

            return root;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_TimerSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TimerSO,
                "TimerSO must be null on a fresh ZonePresenceLeaderboardController.");
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
        public void OnDisable_Unregisters()
        {
            var ctrl    = CreateController();
            var timerSO = CreateTimerSO(1);
            var evt     = CreateEvent();
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();

            SetField(ctrl, "_timerSO",            timerSO);
            SetField(ctrl, "_onPresenceUpdated",   evt);
            SetField(ctrl, "_listContainer",        container);
            SetField(ctrl, "_rowPrefab",            prefab);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int rowsBefore = container.childCount;
            evt.Raise(); // must not trigger Refresh after unsubscribe
            Assert.AreEqual(rowsBefore, container.childCount,
                "After OnDisable, raising _onPresenceUpdated must not rebuild rows.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_NullListContainer_NoOp()
        {
            var ctrl    = CreateController();
            var timerSO = CreateTimerSO(1);
            SetField(ctrl, "_timerSO",        timerSO);
            // _listContainer intentionally left null.

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _listContainer must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
        }

        [Test]
        public void Refresh_NullRowPrefab_NoOp()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateTimerSO(1);
            var container = new GameObject("Container").transform;
            SetField(ctrl, "_timerSO",       timerSO);
            SetField(ctrl, "_listContainer",  container);
            // _rowPrefab intentionally left null.

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _rowPrefab must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(container.gameObject);
        }

        [Test]
        public void Refresh_NullTimerSO_ShowsEmptyLabel()
        {
            var ctrl      = CreateController();
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();
            var emptyLabelGO = new GameObject("EmptyLabel");
            var emptyLabel = emptyLabelGO.AddComponent<Text>();

            SetField(ctrl, "_listContainer", container);
            SetField(ctrl, "_rowPrefab",     prefab);
            SetField(ctrl, "_emptyLabel",    emptyLabel);
            // _timerSO intentionally left null.

            ctrl.Refresh();

            Assert.IsTrue(emptyLabelGO.activeSelf,
                "Refresh with null _timerSO must show _emptyLabel.");
            Assert.AreEqual(0, container.childCount,
                "Refresh with null _timerSO must spawn no rows.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(emptyLabelGO);
        }

        [Test]
        public void Refresh_ZeroZones_ShowsEmptyLabel()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateTimerSO(0); // maxZones = 0 — clamped to 1 by [Min(1)]
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();
            var emptyLabelGO = new GameObject("EmptyLabel");
            var emptyLabel = emptyLabelGO.AddComponent<Text>();

            // Force MaxZones to 0 via reflection to simulate the "no zones" branch.
            SetField(timerSO, "_maxZones", 0);
            timerSO.Reset(); // re-init with 0 zones

            SetField(ctrl, "_timerSO",       timerSO);
            SetField(ctrl, "_listContainer",  container);
            SetField(ctrl, "_rowPrefab",      prefab);
            SetField(ctrl, "_emptyLabel",     emptyLabel);

            ctrl.Refresh();

            Assert.IsTrue(emptyLabelGO.activeSelf,
                "Refresh with MaxZones == 0 must show _emptyLabel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(emptyLabelGO);
        }

        [Test]
        public void Refresh_SingleZone_ShowsPresenceTime()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateTimerSO(1);
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();

            SetField(ctrl, "_timerSO",       timerSO);
            SetField(ctrl, "_listContainer",  container);
            SetField(ctrl, "_rowPrefab",      prefab);

            timerSO.AddPresenceTime(0, 5.5f);
            ctrl.Refresh();

            Assert.AreEqual(1, container.childCount, "Should spawn exactly one row.");
            Text[] texts = container.GetChild(0).GetComponentsInChildren<Text>(true);
            Assert.IsTrue(texts.Length >= 2, "Row must have at least 2 Text components.");
            Assert.AreEqual("Zone 1", texts[0].text, "First text must show 'Zone 1'.");
            Assert.AreEqual("5.5s", texts[1].text,   "Second text must show '5.5s'.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_MultiZone_SortedDescending()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateTimerSO(3);
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();

            SetField(ctrl, "_timerSO",       timerSO);
            SetField(ctrl, "_listContainer",  container);
            SetField(ctrl, "_rowPrefab",      prefab);

            // Zone 0 = 1s, Zone 1 = 10s, Zone 2 = 5s  → sorted: Zone 2(10s), Zone 3(5s), Zone 1(1s)
            timerSO.AddPresenceTime(0, 1f);
            timerSO.AddPresenceTime(1, 10f);
            timerSO.AddPresenceTime(2, 5f);
            ctrl.Refresh();

            Assert.AreEqual(3, container.childCount, "Should spawn 3 rows.");

            Text[] firstRow  = container.GetChild(0).GetComponentsInChildren<Text>(true);
            Text[] secondRow = container.GetChild(1).GetComponentsInChildren<Text>(true);

            // First row should be the zone with highest presence time (Zone 2 → 10s).
            Assert.AreEqual("Zone 2", firstRow[0].text,
                "Row 0 must show the zone with the most presence time.");
            Assert.AreEqual("10.0s", firstRow[1].text,
                "Row 0 must show 10.0s as the top presence time.");
            Assert.AreEqual("Zone 3", secondRow[0].text,
                "Row 1 must be the second-highest zone.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_TopZone_RankBadgeActive()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateTimerSO(2);
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab(withBadge: true);

            SetField(ctrl, "_timerSO",       timerSO);
            SetField(ctrl, "_listContainer",  container);
            SetField(ctrl, "_rowPrefab",      prefab);

            timerSO.AddPresenceTime(0, 3f);
            timerSO.AddPresenceTime(1, 7f); // Zone 2 is top
            ctrl.Refresh();

            Assert.AreEqual(2, container.childCount, "Should spawn 2 rows.");

            Image topBadge    = container.GetChild(0).GetComponentInChildren<Image>(true);
            Image secondBadge = container.GetChild(1).GetComponentInChildren<Image>(true);

            Assert.IsNotNull(topBadge, "Top row must have an Image component.");
            Assert.IsTrue(topBadge.gameObject.activeSelf,
                "Rank badge must be active on the top-ranked zone row.");
            Assert.IsNotNull(secondBadge, "Second row must have an Image component.");
            Assert.IsFalse(secondBadge.gameObject.activeSelf,
                "Rank badge must be inactive on non-top-ranked zone rows.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void OnPresenceUpdated_Raise_Refreshes()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateTimerSO(1);
            var evt       = CreateEvent();
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();

            SetField(ctrl, "_timerSO",           timerSO);
            SetField(ctrl, "_onPresenceUpdated",  evt);
            SetField(ctrl, "_listContainer",       container);
            SetField(ctrl, "_rowPrefab",           prefab);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            timerSO.AddPresenceTime(0, 2.0f);
            evt.Raise();

            Assert.AreEqual(1, container.childCount,
                "Raising _onPresenceUpdated must trigger Refresh and spawn rows.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }
    }
}
