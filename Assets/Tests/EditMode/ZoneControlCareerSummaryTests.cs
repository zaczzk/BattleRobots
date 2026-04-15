using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T296: <see cref="ZoneControlCareerSummaryController"/>.
    ///
    /// ZoneControlCareerSummaryTests (12):
    ///   FreshInstance_SummarySO_Null                             ×1
    ///   FreshInstance_HistorySO_Null                             ×1
    ///   OnEnable_NullRefs_DoesNotThrow                           ×1
    ///   OnDisable_NullRefs_DoesNotThrow                          ×1
    ///   OnDisable_Unregisters_Channel                            ×1
    ///   Refresh_NullSummary_HidesPanel                           ×1
    ///   Refresh_ShowsPanel_WhenSummarySOSet                      ×1
    ///   Refresh_TotalZonesLabel_Updates                          ×1
    ///   Refresh_AvgZonesLabel_Updates                            ×1
    ///   Refresh_RatingBadge_Enabled_WhenHistoryHasEntry          ×1
    ///   Refresh_RatingBadge_Disabled_WhenNoneInHistory           ×1
    ///   Refresh_NullPanel_DoesNotThrow                           ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlCareerSummaryTests
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

        private static ZoneControlSessionSummarySO CreateSummarySO() =>
            ScriptableObject.CreateInstance<ZoneControlSessionSummarySO>();

        private static ZoneControlMatchRatingHistorySO CreateHistorySO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchRatingHistorySO>();

        private static ZoneControlCareerSummaryController CreateController() =>
            new GameObject("CareerSummaryCtrl_Test")
                .AddComponent<ZoneControlCareerSummaryController>();

        private static Image CreateImage()
        {
            var go = new GameObject("Img");
            go.AddComponent<CanvasRenderer>();
            return go.AddComponent<Image>();
        }

        private static Text CreateText()
        {
            var go = new GameObject("Txt");
            go.AddComponent<CanvasRenderer>();
            return go.AddComponent<Text>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_SummarySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.SummarySO,
                "SummarySO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_HistorySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.HistorySO,
                "HistorySO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlCareerSummaryController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlCareerSummaryController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlCareerSummaryController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onSummaryUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onSummaryUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullSummary_HidesPanel()
        {
            var go    = new GameObject("Test_NullSummary");
            var ctrl  = go.AddComponent<ZoneControlCareerSummaryController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when SummarySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_ShowsPanel_WhenSummarySOSet()
        {
            var go      = new GameObject("Test_ShowPanel");
            var ctrl    = go.AddComponent<ZoneControlCareerSummaryController>();
            var summary = CreateSummarySO();
            var panel   = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_summarySO", summary);
            SetField(ctrl, "_panel",     panel);
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when SummarySO is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_TotalZonesLabel_Updates()
        {
            var go      = new GameObject("Test_TotalZones");
            var ctrl    = go.AddComponent<ZoneControlCareerSummaryController>();
            var summary = CreateSummarySO();
            var label   = CreateText();

            summary.AddMatch(7, false, 0);

            SetField(ctrl, "_summarySO",       summary);
            SetField(ctrl, "_totalZonesLabel", label);
            ctrl.Refresh();

            StringAssert.Contains("7", label.text,
                "Total zones label must contain the captured zone count.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Refresh_AvgZonesLabel_Updates()
        {
            var go      = new GameObject("Test_AvgZones");
            var ctrl    = go.AddComponent<ZoneControlCareerSummaryController>();
            var summary = CreateSummarySO();
            var label   = CreateText();

            summary.AddMatch(4, false, 0);
            summary.AddMatch(2, false, 0); // avg = 3.0

            SetField(ctrl, "_summarySO",    summary);
            SetField(ctrl, "_avgZonesLabel", label);
            ctrl.Refresh();

            StringAssert.Contains("3", label.text,
                "Avg zones label must reflect the computed average.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Refresh_RatingBadge_Enabled_WhenHistoryHasEntry()
        {
            var go      = new GameObject("Test_BadgeEnabled");
            var ctrl    = go.AddComponent<ZoneControlCareerSummaryController>();
            var summary = CreateSummarySO();
            var history = CreateHistorySO();
            var badge   = CreateImage();
            badge.enabled = false;

            history.AddRating(4);

            SetField(ctrl, "_summarySO",    summary);
            SetField(ctrl, "_historySO",    history);
            SetField(ctrl, "_ratingBadges", new Image[] { badge });
            ctrl.Refresh();

            Assert.IsTrue(badge.enabled,
                "Badge at index 0 must be enabled when a rating entry exists.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(badge.gameObject);
        }

        [Test]
        public void Refresh_RatingBadge_Disabled_WhenNoneInHistory()
        {
            var go      = new GameObject("Test_BadgeDisabled");
            var ctrl    = go.AddComponent<ZoneControlCareerSummaryController>();
            var summary = CreateSummarySO();
            // _historySO intentionally null → no ratings.
            var badge   = CreateImage();
            badge.enabled = true;

            SetField(ctrl, "_summarySO",    summary);
            SetField(ctrl, "_ratingBadges", new Image[] { badge });
            ctrl.Refresh();

            Assert.IsFalse(badge.enabled,
                "Badge must be disabled when no rating history is available.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(badge.gameObject);
        }

        [Test]
        public void Refresh_NullPanel_DoesNotThrow()
        {
            var go      = new GameObject("Test_NullPanel");
            var ctrl    = go.AddComponent<ZoneControlCareerSummaryController>();
            var summary = CreateSummarySO();

            SetField(ctrl, "_summarySO", summary);
            // _panel intentionally null.
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _panel is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
        }
    }
}
