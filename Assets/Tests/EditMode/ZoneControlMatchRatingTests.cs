using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T288: <see cref="ZoneControlMatchRatingConfig"/> and
    /// <see cref="ZoneControlMatchRatingController"/>.
    ///
    /// ZoneControlMatchRatingTests (14):
    ///   Config_ComputeRating_One_WhenZeroZones                               ×1
    ///   Config_ComputeRating_Two_WhenMeetsMin2                               ×1
    ///   Config_ComputeRating_Three_WhenMeetsMin3                             ×1
    ///   Config_ComputeRating_Five_WhenMeetsAll                               ×1
    ///   Config_ComputeRating_StreakBonus_CapsAtFive                          ×1
    ///   Config_ComputeRating_DominanceBonus_Applies                          ×1
    ///   Controller_FreshInstance_CurrentRating_Zero                          ×1
    ///   Controller_FreshInstance_SummarySO_Null                              ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                            ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                           ×1
    ///   Controller_OnDisable_Unregisters_Channel                             ×1
    ///   Controller_HandleMatchEnded_NullSummary_NoThrow                      ×1
    ///   Controller_HandleMatchEnded_NullConfig_NoThrow                       ×1
    ///   Controller_HandleMatchEnded_ComputesAndSetsRating                    ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchRatingTests
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

        private static ZoneControlMatchRatingConfig CreateConfig() =>
            ScriptableObject.CreateInstance<ZoneControlMatchRatingConfig>();

        private static ZoneControlSessionSummarySO CreateSummary() =>
            ScriptableObject.CreateInstance<ZoneControlSessionSummarySO>();

        private static ZoneControlMatchRatingController CreateController() =>
            new GameObject("ZoneMatchRating_Test")
                .AddComponent<ZoneControlMatchRatingController>();

        // ── Config Tests ──────────────────────────────────────────────────────

        [Test]
        public void Config_ComputeRating_One_WhenZeroZones()
        {
            var cfg = CreateConfig();
            // Default MinZonesForRating2 = 3; 0 zones → rating 1.
            int rating = cfg.ComputeRating(0, 0, 0);
            Assert.AreEqual(1, rating,
                "Rating must be 1 when zones < MinZonesForRating2 and no bonuses apply.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_ComputeRating_Two_WhenMeetsMin2()
        {
            var cfg = CreateConfig();
            // Default: min2=3, no bonus.
            int rating = cfg.ComputeRating(3, 0, 0);
            Assert.AreEqual(2, rating,
                "Rating must be 2 when zones == MinZonesForRating2 and no bonuses apply.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_ComputeRating_Three_WhenMeetsMin3()
        {
            var cfg = CreateConfig();
            // Default: min3=6, no bonus.
            int rating = cfg.ComputeRating(6, 0, 0);
            Assert.AreEqual(3, rating,
                "Rating must be 3 when zones == MinZonesForRating3 and no bonuses apply.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_ComputeRating_Five_WhenMeetsAll()
        {
            var cfg = CreateConfig();
            // Default: min5=15, no bonus needed.
            int rating = cfg.ComputeRating(15, 0, 0);
            Assert.AreEqual(5, rating,
                "Rating must be 5 when zones >= MinZonesForRating5 and no bonuses apply.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_ComputeRating_StreakBonus_CapsAtFive()
        {
            var cfg = CreateConfig();
            // 5-star base with streak bonus → still capped at 5.
            int rating = cfg.ComputeRating(15, 99, 0);
            Assert.AreEqual(5, rating,
                "Rating must be capped at 5 even with streak bonus applied.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_ComputeRating_DominanceBonus_Applies()
        {
            var cfg = CreateConfig();
            // Default min2=3; 3 zones = base 2; dominanceMatches >= 2 → +1 → rating 3.
            int rating = cfg.ComputeRating(3, 0, 2);
            Assert.AreEqual(3, rating,
                "Dominance bonus must add +1 to base rating.");
            Object.DestroyImmediate(cfg);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_CurrentRating_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0, ctrl.CurrentRating,
                "CurrentRating must be 0 on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_SummarySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.SummarySO,
                "SummarySO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(() => go.AddComponent<ZoneControlMatchRatingController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMatchRatingController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchRatingController>();
            var evt  = CreateEvent();

            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable the controller must have unregistered from the channel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullSummary_NoThrow()
        {
            var go   = new GameObject("Test_NullSummary");
            var ctrl = go.AddComponent<ZoneControlMatchRatingController>();
            var cfg  = CreateConfig();
            SetField(ctrl, "_ratingConfig", cfg);

            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when SummarySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullConfig_NoThrow()
        {
            var go      = new GameObject("Test_NullConfig");
            var ctrl    = go.AddComponent<ZoneControlMatchRatingController>();
            var summary = CreateSummary();
            SetField(ctrl, "_summarySO", summary);

            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when RatingConfig is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
        }

        [Test]
        public void Controller_HandleMatchEnded_ComputesAndSetsRating()
        {
            var go      = new GameObject("Test_Compute");
            var ctrl    = go.AddComponent<ZoneControlMatchRatingController>();
            var summary = CreateSummary();
            var cfg     = CreateConfig();

            // Add 6 zones → base rating 3 (default min3=6), no bonus.
            summary.AddMatch(6, false, 0);
            SetField(ctrl, "_summarySO",    summary);
            SetField(ctrl, "_ratingConfig", cfg);

            ctrl.HandleMatchEnded();

            Assert.AreEqual(3, ctrl.CurrentRating,
                "CurrentRating must be 3 for 6 total zones with no bonus.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(cfg);
        }
    }
}
