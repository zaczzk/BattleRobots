using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T283: <see cref="ZoneCaptureHistoryPresenterController"/>.
    ///
    /// ZoneCaptureHistoryPresenterTests (12):
    ///   Controller_FreshInstance_HistorySO_Null                            ×1
    ///   Controller_FreshInstance_CaptureColor_Green                        ×1
    ///   Controller_FreshInstance_LostColor_Red                             ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                          ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_Unregisters_Channel                           ×1
    ///   Controller_Refresh_NullHistorySO_HidesPanel                        ×1
    ///   Controller_Refresh_NullStripContainer_HidesPanel                   ×1
    ///   Controller_Refresh_NullPrefab_HidesPanel                           ×1
    ///   Controller_Refresh_EmptyHistory_HidesPanel                         ×1
    ///   Controller_Refresh_EmptyHistory_ShowsEmptyLabel                    ×1
    ///   Controller_Refresh_WithEntries_ShowsPanel                          ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneCaptureHistoryPresenterTests
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

        private static ZoneCaptureHistorySO CreateHistorySO() =>
            ScriptableObject.CreateInstance<ZoneCaptureHistorySO>();

        private static ZoneCaptureHistoryPresenterController CreateController() =>
            new GameObject("ZoneHistPresenter_Test")
                .AddComponent<ZoneCaptureHistoryPresenterController>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_HistorySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.HistorySO,
                "HistorySO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_CaptureColor_Green()
        {
            var ctrl = CreateController();
            Assert.AreEqual(Color.green, ctrl.CaptureColor,
                "CaptureColor must default to green.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_LostColor_Red()
        {
            var ctrl = CreateController();
            Assert.AreEqual(Color.red, ctrl.LostColor,
                "LostColor must default to red.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_NullRefs");
            Assert.DoesNotThrow(() => go.AddComponent<ZoneCaptureHistoryPresenterController>(),
                "Adding component with null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_NullRefs");
            var ctrl = go.AddComponent<ZoneCaptureHistoryPresenterController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling with null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneCaptureHistoryPresenterController>();
            var evt  = CreateEvent();

            SetField(ctrl, "_onHistoryUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int callCount = 0;
            evt.RegisterCallback(() => callCount++);
            evt.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable the controller must have unregistered from the channel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullHistorySO_HidesPanel()
        {
            var go    = new GameObject("Test_NullHistory");
            var ctrl  = go.AddComponent<ZoneCaptureHistoryPresenterController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when HistorySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_NullStripContainer_HidesPanel()
        {
            var go      = new GameObject("Test_NullContainer");
            var ctrl    = go.AddComponent<ZoneCaptureHistoryPresenterController>();
            var history = CreateHistorySO();
            var panel   = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_historySO",       history);
            SetField(ctrl, "_entryPrefab",     new GameObject("Prefab"));
            SetField(ctrl, "_panel",           panel);
            // _stripContainer intentionally left null

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _stripContainer is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_NullPrefab_HidesPanel()
        {
            var go        = new GameObject("Test_NullPrefab");
            var ctrl      = go.AddComponent<ZoneCaptureHistoryPresenterController>();
            var history   = CreateHistorySO();
            var container = new GameObject("Strip").transform;
            var panel     = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_historySO",      history);
            SetField(ctrl, "_stripContainer", container);
            SetField(ctrl, "_panel",          panel);
            // _entryPrefab intentionally left null

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _entryPrefab is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_EmptyHistory_HidesPanel()
        {
            var go        = new GameObject("Test_Empty_HidesPanel");
            var ctrl      = go.AddComponent<ZoneCaptureHistoryPresenterController>();
            var history   = CreateHistorySO();
            var container = new GameObject("Strip").transform;
            var prefab    = new GameObject("Prefab");
            var panel     = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_historySO",      history);
            SetField(ctrl, "_stripContainer", container);
            SetField(ctrl, "_entryPrefab",    prefab);
            SetField(ctrl, "_panel",          panel);

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when history has zero entries.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_EmptyHistory_ShowsEmptyLabel()
        {
            var go         = new GameObject("Test_Empty_EmptyLabel");
            var ctrl       = go.AddComponent<ZoneCaptureHistoryPresenterController>();
            var history    = CreateHistorySO();
            var container  = new GameObject("Strip").transform;
            var prefab     = new GameObject("Prefab");
            var emptyLabel = new GameObject("EmptyLabel");
            emptyLabel.SetActive(false);

            SetField(ctrl, "_historySO",      history);
            SetField(ctrl, "_stripContainer", container);
            SetField(ctrl, "_entryPrefab",    prefab);
            SetField(ctrl, "_emptyLabel",     emptyLabel);

            ctrl.Refresh();

            Assert.IsTrue(emptyLabel.activeSelf,
                "Empty label must be shown when history has zero entries.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(emptyLabel);
        }

        [Test]
        public void Controller_Refresh_WithEntries_ShowsPanel()
        {
            var go        = new GameObject("Test_WithEntries");
            var ctrl      = go.AddComponent<ZoneCaptureHistoryPresenterController>();
            var history   = CreateHistorySO();
            var container = new GameObject("Strip").transform;
            var prefab    = new GameObject("Prefab");
            var panel     = new GameObject("Panel");
            panel.SetActive(false);

            history.AddEntry("ZoneA", 1f, isCapture: true);

            SetField(ctrl, "_historySO",      history);
            SetField(ctrl, "_stripContainer", container);
            SetField(ctrl, "_entryPrefab",    prefab);
            SetField(ctrl, "_panel",          panel);

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when history has at least one entry.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
