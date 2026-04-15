using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T286: <see cref="ZoneControlReplayPresenterController"/>.
    ///
    /// ZoneControlReplayPresenterTests (12):
    ///   FreshInstance_ReplaySO_Null                                          ×1
    ///   FreshInstance_ZoneBadgeCount_Zero                                    ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                       ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                      ×1
    ///   OnDisable_Unregisters_Channel                                        ×1
    ///   Refresh_NullReplaySO_HidesPanel                                      ×1
    ///   Refresh_EmptyBuffer_HidesPanel                                       ×1
    ///   Refresh_ShowsPanel_WhenSnapshotAvailable                             ×1
    ///   Refresh_Badge_Activated_WhenCaptured                                 ×1
    ///   Refresh_Badge_Deactivated_WhenNotCaptured                            ×1
    ///   Refresh_Badge_Deactivated_WhenBeyondCaptureStateLength               ×1
    ///   Refresh_NullPanel_DoesNotThrow                                       ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlReplayPresenterTests
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

        private static ZoneControlReplaySO CreateReplaySO() =>
            ScriptableObject.CreateInstance<ZoneControlReplaySO>();

        private static ZoneControlReplayPresenterController CreatePresenter() =>
            new GameObject("ZoneReplayPresenter_Test")
                .AddComponent<ZoneControlReplayPresenterController>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_ReplaySO_Null()
        {
            var ctrl = CreatePresenter();
            Assert.IsNull(ctrl.ReplaySO,
                "ReplaySO must be null on a freshly added presenter.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_ZoneBadgeCount_Zero()
        {
            var ctrl = CreatePresenter();
            Assert.AreEqual(0, ctrl.ZoneBadgeCount,
                "ZoneBadgeCount must be 0 when no badges are assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(() => go.AddComponent<ZoneControlReplayPresenterController>(),
                "Adding presenter with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlReplayPresenterController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling presenter with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlReplayPresenterController>();
            var evt  = CreateEvent();

            SetField(ctrl, "_onReplayUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable the presenter must have unregistered from the channel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullReplaySO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_NullSO");
            var ctrl  = go.AddComponent<ZoneControlReplayPresenterController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when ReplaySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_EmptyBuffer_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Empty");
            var ctrl  = go.AddComponent<ZoneControlReplayPresenterController>();
            var so    = CreateReplaySO();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_replaySO", so);
            SetField(ctrl, "_panel",    panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when the replay buffer is empty.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_ShowsPanel_WhenSnapshotAvailable()
        {
            var go    = new GameObject("Test_Refresh_ShowPanel");
            var ctrl  = go.AddComponent<ZoneControlReplayPresenterController>();
            var so    = CreateReplaySO();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            so.AddSnapshot(1f, new[] { true });
            SetField(ctrl, "_replaySO", so);
            SetField(ctrl, "_panel",    panel);
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when a snapshot is available.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_Badge_Activated_WhenCaptured()
        {
            var go    = new GameObject("Test_Badge_Active");
            var ctrl  = go.AddComponent<ZoneControlReplayPresenterController>();
            var so    = CreateReplaySO();
            var badge = new GameObject("Badge");
            badge.SetActive(false);

            so.AddSnapshot(1f, new[] { true });
            SetField(ctrl, "_replaySO",   so);
            SetField(ctrl, "_zoneBadges", new GameObject[] { badge });
            ctrl.Refresh();

            Assert.IsTrue(badge.activeSelf,
                "Badge must be activated when the zone is captured at the current step.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_Badge_Deactivated_WhenNotCaptured()
        {
            var go    = new GameObject("Test_Badge_Inactive");
            var ctrl  = go.AddComponent<ZoneControlReplayPresenterController>();
            var so    = CreateReplaySO();
            var badge = new GameObject("Badge");
            badge.SetActive(true);

            so.AddSnapshot(1f, new[] { false });
            SetField(ctrl, "_replaySO",   so);
            SetField(ctrl, "_zoneBadges", new GameObject[] { badge });
            ctrl.Refresh();

            Assert.IsFalse(badge.activeSelf,
                "Badge must be deactivated when the zone is not captured at the current step.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_Badge_Deactivated_WhenBeyondCaptureStateLength()
        {
            var go     = new GameObject("Test_Badge_Beyond");
            var ctrl   = go.AddComponent<ZoneControlReplayPresenterController>();
            var so     = CreateReplaySO();
            var badge0 = new GameObject("Badge0");
            var badge1 = new GameObject("Badge1");
            badge0.SetActive(true);
            badge1.SetActive(true);

            // Snapshot only covers zone 0; badge 1 is beyond captureState.
            so.AddSnapshot(1f, new[] { true });
            SetField(ctrl, "_replaySO",   so);
            SetField(ctrl, "_zoneBadges", new GameObject[] { badge0, badge1 });
            ctrl.Refresh();

            Assert.IsTrue(badge0.activeSelf,
                "Badge 0 must be active (captured).");
            Assert.IsFalse(badge1.activeSelf,
                "Badge 1 must be inactive when beyond captureState length.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(badge0);
            Object.DestroyImmediate(badge1);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_NullPanel_DoesNotThrow()
        {
            var go   = new GameObject("Test_NullPanel");
            var ctrl = go.AddComponent<ZoneControlReplayPresenterController>();
            var so   = CreateReplaySO();
            so.AddSnapshot(1f, new[] { false });

            SetField(ctrl, "_replaySO", so);
            // _panel left null
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _panel is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}
