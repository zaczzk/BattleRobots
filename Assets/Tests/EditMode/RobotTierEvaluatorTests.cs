using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotTierEvaluator"/>.
    ///
    /// Covers:
    ///   • EvaluateTier: null buildRating → Unranked.
    ///   • EvaluateTier: null config → Unranked.
    ///   • EvaluateTier: both null → Unranked.
    ///   • EvaluateTier: zero rating, no thresholds → Unranked.
    ///   • EvaluateTier: rating below all thresholds → Unranked.
    ///   • EvaluateTier: rating meets Bronze → Bronze.
    ///   • EvaluateTier: rating meets Gold (3-tier config) → Gold.
    ///   • MeetsTierRequirement: null args, required Bronze → false.
    ///   • MeetsTierRequirement: current tier meets required tier → true.
    ///   • MeetsTierRequirement: current tier below required tier → false.
    /// </summary>
    public class RobotTierEvaluatorTests
    {
        private BuildRatingSO   _buildRating;
        private RobotTierConfig _config;

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _buildRating = ScriptableObject.CreateInstance<BuildRatingSO>();
            _config      = ScriptableObject.CreateInstance<RobotTierConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_buildRating != null) Object.DestroyImmediate(_buildRating);
            if (_config      != null) Object.DestroyImmediate(_config);
        }

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetRating(BuildRatingSO so, int rating)
        {
            FieldInfo fi = typeof(BuildRatingSO)
                .GetField("_currentRating", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "'_currentRating' field not found on BuildRatingSO.");
            fi.SetValue(so, rating);
        }

        private static void AddThreshold(RobotTierConfig cfg, RobotTierLevel tier, int threshold)
        {
            FieldInfo fi = typeof(RobotTierConfig)
                .GetField("_thresholds", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "'_thresholds' field not found on RobotTierConfig.");
            var list = (List<TierThresholdEntry>)fi.GetValue(cfg);
            list.Add(new TierThresholdEntry { tier = tier, ratingThreshold = threshold });
        }

        // ── EvaluateTier null-guards ──────────────────────────────────────────

        [Test]
        public void EvaluateTier_NullBuildRating_ReturnsUnranked()
        {
            Assert.AreEqual(
                RobotTierLevel.Unranked,
                RobotTierEvaluator.EvaluateTier(null, _config));
        }

        [Test]
        public void EvaluateTier_NullConfig_ReturnsUnranked()
        {
            Assert.AreEqual(
                RobotTierLevel.Unranked,
                RobotTierEvaluator.EvaluateTier(_buildRating, null));
        }

        [Test]
        public void EvaluateTier_BothNull_ReturnsUnranked()
        {
            Assert.AreEqual(
                RobotTierLevel.Unranked,
                RobotTierEvaluator.EvaluateTier(null, null));
        }

        // ── EvaluateTier tier resolution ──────────────────────────────────────

        [Test]
        public void EvaluateTier_ZeroRating_NoThresholds_ReturnsUnranked()
        {
            // Default rating is 0 and config has no entries.
            Assert.AreEqual(
                RobotTierLevel.Unranked,
                RobotTierEvaluator.EvaluateTier(_buildRating, _config));
        }

        [Test]
        public void EvaluateTier_RatingBelowAllThresholds_ReturnsUnranked()
        {
            AddThreshold(_config, RobotTierLevel.Bronze, 100);
            SetRating(_buildRating, 50);
            Assert.AreEqual(
                RobotTierLevel.Unranked,
                RobotTierEvaluator.EvaluateTier(_buildRating, _config));
        }

        [Test]
        public void EvaluateTier_RatingMeetsBronzeThreshold_ReturnsBronze()
        {
            AddThreshold(_config, RobotTierLevel.Bronze, 100);
            SetRating(_buildRating, 100);
            Assert.AreEqual(
                RobotTierLevel.Bronze,
                RobotTierEvaluator.EvaluateTier(_buildRating, _config));
        }

        [Test]
        public void EvaluateTier_RatingMeetsGoldInThreeTierConfig_ReturnsGold()
        {
            AddThreshold(_config, RobotTierLevel.Bronze, 100);
            AddThreshold(_config, RobotTierLevel.Silver, 300);
            AddThreshold(_config, RobotTierLevel.Gold,   600);
            SetRating(_buildRating, 750);
            Assert.AreEqual(
                RobotTierLevel.Gold,
                RobotTierEvaluator.EvaluateTier(_buildRating, _config));
        }

        // ── MeetsTierRequirement ──────────────────────────────────────────────

        [Test]
        public void MeetsTierRequirement_NullArgs_RequiredBronze_ReturnsFalse()
        {
            // EvaluateTier(null, null) → Unranked (0); Bronze(1) > Unranked → false.
            Assert.IsFalse(
                RobotTierEvaluator.MeetsTierRequirement(null, null, RobotTierLevel.Bronze));
        }

        [Test]
        public void MeetsTierRequirement_CurrentTierMeetsRequired_ReturnsTrue()
        {
            AddThreshold(_config, RobotTierLevel.Bronze, 100);
            AddThreshold(_config, RobotTierLevel.Silver, 300);
            SetRating(_buildRating, 400); // Silver (tier >= Silver required)
            Assert.IsTrue(
                RobotTierEvaluator.MeetsTierRequirement(
                    _buildRating, _config, RobotTierLevel.Bronze));
        }

        [Test]
        public void MeetsTierRequirement_CurrentTierBelowRequired_ReturnsFalse()
        {
            AddThreshold(_config, RobotTierLevel.Bronze, 100);
            AddThreshold(_config, RobotTierLevel.Gold,   600);
            SetRating(_buildRating, 200); // Bronze only — Gold required
            Assert.IsFalse(
                RobotTierEvaluator.MeetsTierRequirement(
                    _buildRating, _config, RobotTierLevel.Gold));
        }
    }
}
