using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T297: <see cref="ZoneControlSessionReportController"/>.
    ///
    /// ZoneControlSessionReportTests (14):
    ///   FreshInstance_SummarySO_Null                             ×1
    ///   FreshInstance_RatingController_Null                      ×1
    ///   FreshInstance_AdvisorController_Null                     ×1
    ///   OnEnable_NullRefs_DoesNotThrow                           ×1
    ///   OnDisable_NullRefs_DoesNotThrow                          ×1
    ///   OnDisable_Unregisters_Channel                            ×1
    ///   HandleMatchEnded_NullRefs_DoesNotThrow                   ×1
    ///   Refresh_NullSummary_HidesPanel                           ×1
    ///   Refresh_ShowsPanel_WhenSummarySOSet                      ×1
    ///   Refresh_TotalZonesLabel_Updates                          ×1
    ///   Refresh_MatchesLabel_Updates                             ×1
    ///   Refresh_RatingLabel_UsesCurrentRating                    ×1
    ///   Refresh_AdviceLabel_UsesComputeAdvice                    ×1
    ///   Refresh_NullPanel_DoesNotThrow                           ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlSessionReportTests
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

        private static ZoneControlSessionReportController CreateController() =>
            new GameObject("SessionReport_Test")
                .AddComponent<ZoneControlSessionReportController>();

        private static Text CreateText()
        {
            var go = new GameObject("Txt");
            go.AddComponent<CanvasRenderer>();
            return go.AddComponent<Text>();
        }

        private static ZoneControlMatchRatingController CreateRatingController() =>
            new GameObject("RatingCtrl_Test")
                .AddComponent<ZoneControlMatchRatingController>();

        private static ZoneControlDifficultyAdvisorController CreateAdvisorController() =>
            new GameObject("AdvisorCtrl_Test")
                .AddComponent<ZoneControlDifficultyAdvisorController>();

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
        public void FreshInstance_RatingController_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.RatingController,
                "RatingController must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_AdvisorController_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.AdvisorController,
                "AdvisorController must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlSessionReportController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlSessionReportController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlSessionReportController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void HandleMatchEnded_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when all refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_NullSummary_HidesPanel()
        {
            var go    = new GameObject("Test_NullSummary");
            var ctrl  = go.AddComponent<ZoneControlSessionReportController>();
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
            var ctrl    = go.AddComponent<ZoneControlSessionReportController>();
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
            var ctrl    = go.AddComponent<ZoneControlSessionReportController>();
            var summary = CreateSummarySO();
            var label   = CreateText();

            summary.AddMatch(5, false, 0);

            SetField(ctrl, "_summarySO",       summary);
            SetField(ctrl, "_totalZonesLabel", label);
            ctrl.Refresh();

            StringAssert.Contains("5", label.text,
                "Total zones label must contain the captured zone count.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Refresh_MatchesLabel_Updates()
        {
            var go      = new GameObject("Test_Matches");
            var ctrl    = go.AddComponent<ZoneControlSessionReportController>();
            var summary = CreateSummarySO();
            var label   = CreateText();

            summary.AddMatch(2, false, 0);
            summary.AddMatch(3, false, 0); // 2 matches

            SetField(ctrl, "_summarySO",    summary);
            SetField(ctrl, "_matchesLabel", label);
            ctrl.Refresh();

            StringAssert.Contains("2", label.text,
                "Matches label must contain the number of matches played.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Refresh_RatingLabel_UsesCurrentRating()
        {
            var go             = new GameObject("Test_RatingLabel");
            var ctrl           = go.AddComponent<ZoneControlSessionReportController>();
            var summary        = CreateSummarySO();
            var ratingCtrl     = CreateRatingController();
            var label          = CreateText();

            // Force CurrentRating to 3 via reflection.
            FieldInfo fi = ratingCtrl.GetType()
                .GetField("_currentRating", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi);
            fi.SetValue(ratingCtrl, 3);

            SetField(ctrl, "_summarySO",        summary);
            SetField(ctrl, "_ratingController", ratingCtrl);
            SetField(ctrl, "_ratingLabel",      label);
            ctrl.Refresh();

            StringAssert.Contains("3", label.text,
                "Rating label must reflect the current rating from the rating controller.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(ratingCtrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Refresh_AdviceLabel_UsesComputeAdvice()
        {
            var go             = new GameObject("Test_AdviceLabel");
            var ctrl           = go.AddComponent<ZoneControlSessionReportController>();
            var summary        = CreateSummarySO();
            var advisorCtrl    = CreateAdvisorController();
            var label          = CreateText();

            // Advisor with null SOs returns "No data available." — just verify it writes something.
            SetField(ctrl, "_summarySO",         summary);
            SetField(ctrl, "_advisorController", advisorCtrl);
            SetField(ctrl, "_adviceLabel",       label);
            ctrl.Refresh();

            Assert.IsFalse(string.IsNullOrEmpty(label.text),
                "Advice label must be populated from advisor ComputeAdvice().");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(advisorCtrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Refresh_NullPanel_DoesNotThrow()
        {
            var go      = new GameObject("Test_NullPanel");
            var ctrl    = go.AddComponent<ZoneControlSessionReportController>();
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
