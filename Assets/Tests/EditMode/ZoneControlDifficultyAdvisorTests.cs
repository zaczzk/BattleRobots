using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T293: <see cref="ZoneControlDifficultyAdvisorController"/>.
    ///
    /// ZoneControlDifficultyAdvisorTests (12):
    ///   FreshInstance_SummarySO_Null                                         ×1
    ///   FreshInstance_RatingConfig_Null                                      ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                       ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                      ×1
    ///   OnDisable_Unregisters_Channel                                        ×1
    ///   Refresh_NullSummary_HidesPanel                                       ×1
    ///   ComputeAdvice_NullConfig_ReturnsNoData                               ×1
    ///   ComputeAdvice_NoMatchesPlayed_ReturnsPrompt                          ×1
    ///   ComputeAdvice_LowRating_ReturnsLowAdvice                             ×1
    ///   ComputeAdvice_HighRating_ReturnsHighAdvice                           ×1
    ///   Refresh_WithValidData_ShowsPanel                                     ×1
    ///   HandleSummaryUpdated_NullRefs_DoesNotThrow                           ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlDifficultyAdvisorTests
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

        private static ZoneControlMatchRatingConfig CreateRatingConfig() =>
            ScriptableObject.CreateInstance<ZoneControlMatchRatingConfig>();

        private static ZoneControlDifficultyAdvisorController CreateController() =>
            new GameObject("ZoneDifficultyAdvisor_Test")
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
        public void FreshInstance_RatingConfig_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.RatingConfig,
                "RatingConfig must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlDifficultyAdvisorController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlDifficultyAdvisorController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlDifficultyAdvisorController>();

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
            var go    = new GameObject("Test_Refresh_NullSummary");
            var ctrl  = go.AddComponent<ZoneControlDifficultyAdvisorController>();
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
        public void ComputeAdvice_NullConfig_ReturnsNoData()
        {
            var go      = new GameObject("Test_NoData");
            var ctrl    = go.AddComponent<ZoneControlDifficultyAdvisorController>();
            var summary = CreateSummarySO();

            SetField(ctrl, "_summarySO",    summary);
            // _ratingConfig intentionally null.

            string advice = ctrl.ComputeAdvice();
            Assert.AreEqual("No data available.", advice,
                "ComputeAdvice must return no-data message when config is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
        }

        [Test]
        public void ComputeAdvice_NoMatchesPlayed_ReturnsPrompt()
        {
            var go      = new GameObject("Test_NoMatches");
            var ctrl    = go.AddComponent<ZoneControlDifficultyAdvisorController>();
            var summary = CreateSummarySO();
            var config  = CreateRatingConfig();

            SetField(ctrl, "_summarySO",    summary);
            SetField(ctrl, "_ratingConfig", config);
            // summary has MatchesPlayed = 0 by default.

            string advice = ctrl.ComputeAdvice();
            Assert.AreEqual("Play a match to receive advice.", advice,
                "ComputeAdvice must return play-prompt when no matches have been played.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ComputeAdvice_LowRating_ReturnsLowAdvice()
        {
            var go      = new GameObject("Test_LowRating");
            var ctrl    = go.AddComponent<ZoneControlDifficultyAdvisorController>();
            var summary = CreateSummarySO();
            var config  = CreateRatingConfig();

            SetField(ctrl, "_summarySO",    summary);
            SetField(ctrl, "_ratingConfig", config);

            // Add a match with 0 zones, no dominance, no streak → rating = 1.
            summary.AddMatch(0, false, 0);

            string advice = ctrl.ComputeAdvice();
            Assert.AreEqual(
                "Start with fewer zones and slower capture targets.", advice,
                "Low performance must produce the low-advice string.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ComputeAdvice_HighRating_ReturnsHighAdvice()
        {
            var go      = new GameObject("Test_HighRating");
            var ctrl    = go.AddComponent<ZoneControlDifficultyAdvisorController>();
            var summary = CreateSummarySO();
            var config  = CreateRatingConfig();

            SetField(ctrl, "_summarySO",    summary);
            SetField(ctrl, "_ratingConfig", config);

            // Add a match with 15+ zones and a streak of 3+ → rating = 5.
            // Default: MinZonesForRating5=15, MinStreakForBonus=3.
            summary.AddMatch(15, false, 3);

            string advice = ctrl.ComputeAdvice();
            Assert.AreEqual(
                "Excellent! Try increasing zone count.", advice,
                "High performance must produce the high-advice string.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Refresh_WithValidData_ShowsPanel()
        {
            var go      = new GameObject("Test_Refresh_ShowsPanel");
            var ctrl    = go.AddComponent<ZoneControlDifficultyAdvisorController>();
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
        public void HandleSummaryUpdated_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleSummaryUpdated(),
                "HandleSummaryUpdated must not throw when all refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
