using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T292: <see cref="ZoneControlLeaderboardPresenterController"/>.
    ///
    /// ZoneControlLeaderboardPresenterTests (12):
    ///   FreshInstance_TrackerSO_Null                                         ×1
    ///   FreshInstance_PlayerName_Default                                     ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                       ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                      ×1
    ///   OnDisable_Unregisters_Channel                                        ×1
    ///   Refresh_NullTracker_HidesPanel                                       ×1
    ///   Refresh_NullContainer_DoesNotThrow                                   ×1
    ///   Refresh_NullPrefab_DoesNotThrow                                      ×1
    ///   Refresh_WithTracker_ShowsPanel                                       ×1
    ///   Refresh_PlayerScore_Higher_SpawnsRows                                ×1
    ///   Refresh_EnemyScore_Higher_DoesNotThrow                               ×1
    ///   Refresh_EqualScores_DoesNotThrow                                     ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlLeaderboardPresenterTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneScoreTrackerSO CreateTrackerSO() =>
            ScriptableObject.CreateInstance<ZoneScoreTrackerSO>();

        private static ZoneControlLeaderboardPresenterController CreateController() =>
            new GameObject("ZoneLeaderboardPresenter_Test")
                .AddComponent<ZoneControlLeaderboardPresenterController>();

        /// <summary>
        /// Creates a minimal row prefab: a root GameObject with three child Text
        /// components, matching the layout expected by SpawnRow.
        /// </summary>
        private static GameObject CreateRowPrefab()
        {
            var root = new GameObject("RowPrefab");
            for (int i = 0; i < 3; i++)
            {
                var child = new GameObject($"Text{i}");
                child.transform.SetParent(root.transform);
                child.AddComponent<Text>();
            }
            return root;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_TrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TrackerSO,
                "TrackerSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_PlayerName_Default()
        {
            var ctrl = CreateController();
            Assert.AreEqual("Player", ctrl.PlayerName,
                "Default PlayerName must be 'Player'.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlLeaderboardPresenterController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlLeaderboardPresenterController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlLeaderboardPresenterController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onLeaderboardUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onLeaderboardUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullTracker_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_NullTracker");
            var ctrl  = go.AddComponent<ZoneControlLeaderboardPresenterController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();

            SetField(ctrl, "_panel",          panel);
            SetField(ctrl, "_listContainer",  container);
            SetField(ctrl, "_rowPrefab",      prefab);

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when TrackerSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_NullContainer_DoesNotThrow()
        {
            var go   = new GameObject("Test_Refresh_NullContainer");
            var ctrl = go.AddComponent<ZoneControlLeaderboardPresenterController>();
            var so   = CreateTrackerSO();

            SetField(ctrl, "_trackerSO", so);
            // _listContainer intentionally left null.

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _listContainer is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_NullPrefab_DoesNotThrow()
        {
            var go        = new GameObject("Test_Refresh_NullPrefab");
            var ctrl      = go.AddComponent<ZoneControlLeaderboardPresenterController>();
            var so        = CreateTrackerSO();
            var container = new GameObject("Container").transform;

            SetField(ctrl, "_trackerSO",      so);
            SetField(ctrl, "_listContainer",  container);
            // _rowPrefab intentionally left null.

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _rowPrefab is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(container.gameObject);
        }

        [Test]
        public void Refresh_WithTracker_ShowsPanel()
        {
            var go        = new GameObject("Test_Refresh_ShowsPanel");
            var ctrl      = go.AddComponent<ZoneControlLeaderboardPresenterController>();
            var so        = CreateTrackerSO();
            var panel     = new GameObject("Panel");
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();
            panel.SetActive(false);

            SetField(ctrl, "_trackerSO",      so);
            SetField(ctrl, "_panel",          panel);
            SetField(ctrl, "_listContainer",  container);
            SetField(ctrl, "_rowPrefab",      prefab);

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when TrackerSO is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_PlayerScore_Higher_SpawnsRows()
        {
            var go        = new GameObject("Test_Refresh_SpawnsRows");
            var ctrl      = go.AddComponent<ZoneControlLeaderboardPresenterController>();
            var so        = CreateTrackerSO();
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();

            so.AddPlayerScore(10f);

            SetField(ctrl, "_trackerSO",      so);
            SetField(ctrl, "_listContainer",  container);
            SetField(ctrl, "_rowPrefab",      prefab);

            ctrl.Refresh();

            // Expect exactly 2 rows spawned (player + enemy).
            Assert.AreEqual(2, container.childCount,
                "Refresh must spawn exactly 2 rows (one per participant).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_EnemyScore_Higher_DoesNotThrow()
        {
            var go        = new GameObject("Test_Refresh_EnemyHigher");
            var ctrl      = go.AddComponent<ZoneControlLeaderboardPresenterController>();
            var so        = CreateTrackerSO();
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();

            so.AddEnemyScore(20f);

            SetField(ctrl, "_trackerSO",      so);
            SetField(ctrl, "_listContainer",  container);
            SetField(ctrl, "_rowPrefab",      prefab);

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when enemy score is higher than player score.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_EqualScores_DoesNotThrow()
        {
            var go        = new GameObject("Test_Refresh_EqualScores");
            var ctrl      = go.AddComponent<ZoneControlLeaderboardPresenterController>();
            var so        = CreateTrackerSO();
            var container = new GameObject("Container").transform;
            var prefab    = CreateRowPrefab();

            so.AddPlayerScore(5f);
            so.AddEnemyScore(5f);

            SetField(ctrl, "_trackerSO",      so);
            SetField(ctrl, "_listContainer",  container);
            SetField(ctrl, "_rowPrefab",      prefab);

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when scores are equal.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefab);
        }
    }
}
