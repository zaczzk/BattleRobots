using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="TournamentGatingEvaluator"/> and the new
    /// tier-gating fields added to <see cref="TournamentConfig"/> (T113).
    ///
    /// Covers:
    ///   TournamentConfig new fields:
    ///     • FreshInstance defaults: RequiredTier=Unranked, MinRating=0.
    ///     • Reflection round-trips for RequiredTier and MinRating.
    ///
    ///   TournamentGatingEvaluator.IsUnlocked:
    ///     • Null config → always unlocked.
    ///     • No requirements (Unranked + 0) → always unlocked.
    ///     • Null buildRating → treated as Unranked / 0 rating.
    ///     • Null tierConfig → tier display falls back to enum names but logic is intact.
    ///     • Both gates pass / only tier fails / only rating fails.
    ///     • Exact threshold values accepted; one-below rejected.
    ///
    ///   TournamentGatingEvaluator.GetLockReason:
    ///     • Empty string when unlocked.
    ///     • Tier reason returned when tier gate fails (checked first).
    ///     • Rating reason returned when only rating gate fails.
    ///     • Null config → empty string.
    /// </summary>
    public class TournamentGatingEvaluatorTests
    {
        // ── SO handles ────────────────────────────────────────────────────────

        private TournamentConfig _config;
        private BuildRatingSO    _buildRating;
        private RobotTierConfig  _tierConfig;

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>
        /// Builds a minimal <see cref="RobotTierConfig"/> with a single Bronze
        /// threshold at ratingThreshold = 100.
        /// </summary>
        private RobotTierConfig BuildTierConfig()
        {
            var cfg = ScriptableObject.CreateInstance<RobotTierConfig>();
            var thresholds = new List<TierThresholdEntry>
            {
                new TierThresholdEntry
                {
                    ratingThreshold = 100,
                    tier            = RobotTierLevel.Bronze,
                    displayName     = "Bronze",
                    tintColor       = Color.white
                }
            };
            SetField(cfg, "_thresholds", thresholds);
            return cfg;
        }

        /// <summary>Creates a <see cref="BuildRatingSO"/> with the given rating.</summary>
        private BuildRatingSO BuildRatingSO(int rating)
        {
            var br = ScriptableObject.CreateInstance<BuildRatingSO>();
            SetField(br, "_currentRating", rating);
            return br;
        }

        [SetUp]
        public void SetUp()
        {
            _config      = ScriptableObject.CreateInstance<TournamentConfig>();
            _buildRating = BuildRatingSO(0);
            _tierConfig  = BuildTierConfig();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_buildRating);
            Object.DestroyImmediate(_tierConfig);
        }

        // ══════════════════════════════════════════════════════════════════════
        // TournamentConfig new fields
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void TournamentConfig_FreshInstance_RequiredTier_IsUnranked()
        {
            Assert.AreEqual(RobotTierLevel.Unranked, _config.RequiredTier);
        }

        [Test]
        public void TournamentConfig_FreshInstance_MinRating_IsZero()
        {
            Assert.AreEqual(0, _config.MinRating);
        }

        [Test]
        public void TournamentConfig_RequiredTier_RoundTrip()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Gold);
            Assert.AreEqual(RobotTierLevel.Gold, _config.RequiredTier);
        }

        [Test]
        public void TournamentConfig_MinRating_RoundTrip()
        {
            SetField(_config, "_minRating", 500);
            Assert.AreEqual(500, _config.MinRating);
        }

        // ══════════════════════════════════════════════════════════════════════
        // IsUnlocked — null / no-requirements paths
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void IsUnlocked_NullConfig_ReturnsTrue()
        {
            Assert.IsTrue(TournamentGatingEvaluator.IsUnlocked(null, _buildRating, _tierConfig));
        }

        [Test]
        public void IsUnlocked_AllNull_ReturnsTrue()
        {
            // null config = no requirements defined → always unlocked
            Assert.IsTrue(TournamentGatingEvaluator.IsUnlocked(null, null, null));
        }

        [Test]
        public void IsUnlocked_NoRequirements_Unranked_ZeroRating_AlwaysUnlocked()
        {
            // config defaults: RequiredTier=Unranked, MinRating=0 → any build passes
            Assert.IsTrue(TournamentGatingEvaluator.IsUnlocked(_config, _buildRating, _tierConfig));
        }

        [Test]
        public void IsUnlocked_NullBuildRating_UnrankedRequired_ReturnsTrue()
        {
            // null buildRating → Unranked (0) >= Unranked (0) → true
            Assert.IsTrue(TournamentGatingEvaluator.IsUnlocked(_config, null, _tierConfig));
        }

        [Test]
        public void IsUnlocked_NullBuildRating_BronzeRequired_ReturnsFalse()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze);
            // null buildRating → Unranked (0) < Bronze (1) → false
            Assert.IsFalse(TournamentGatingEvaluator.IsUnlocked(_config, null, _tierConfig));
        }

        [Test]
        public void IsUnlocked_NullTierConfig_UnrankedRequired_ZeroMinRating_ReturnsTrue()
        {
            // null tierConfig: EvaluateTier returns Unranked; Unranked >= Unranked → true
            Assert.IsTrue(TournamentGatingEvaluator.IsUnlocked(_config, _buildRating, null));
        }

        // ══════════════════════════════════════════════════════════════════════
        // IsUnlocked — combined gate checks
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void IsUnlocked_TierMet_RatingMet_ReturnsTrue()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze); // need ≥ Bronze
            SetField(_config, "_minRating",     50);                   // need ≥ 50

            var br = BuildRatingSO(150); // 150 → Bronze tier (threshold 100); rating 150 ≥ 50
            bool result = TournamentGatingEvaluator.IsUnlocked(_config, br, _tierConfig);
            Object.DestroyImmediate(br);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsUnlocked_TierMet_RatingNotMet_ReturnsFalse()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze); // need ≥ Bronze
            SetField(_config, "_minRating",     200);                  // need ≥ 200

            var br = BuildRatingSO(150); // 150 → Bronze (tier met), but 150 < 200 (rating fails)
            bool result = TournamentGatingEvaluator.IsUnlocked(_config, br, _tierConfig);
            Object.DestroyImmediate(br);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsUnlocked_TierNotMet_RatingMet_ReturnsFalse()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze); // need ≥ Bronze
            SetField(_config, "_minRating",     0);                    // rating: no gate

            var br = BuildRatingSO(50); // 50 < 100 → Unranked (tier fails)
            bool result = TournamentGatingEvaluator.IsUnlocked(_config, br, _tierConfig);
            Object.DestroyImmediate(br);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsUnlocked_ExactTierThreshold_ReturnsTrue()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze);

            var br = BuildRatingSO(100); // exactly at Bronze threshold (100) → Bronze → passes
            bool result = TournamentGatingEvaluator.IsUnlocked(_config, br, _tierConfig);
            Object.DestroyImmediate(br);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsUnlocked_OneBelowTierThreshold_ReturnsFalse()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze);

            var br = BuildRatingSO(99); // 99 < 100 → Unranked → fails Bronze
            bool result = TournamentGatingEvaluator.IsUnlocked(_config, br, _tierConfig);
            Object.DestroyImmediate(br);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsUnlocked_ExactMinRating_ReturnsTrue()
        {
            SetField(_config, "_minRating", 300);

            var br = BuildRatingSO(300); // exactly meets MinRating
            bool result = TournamentGatingEvaluator.IsUnlocked(_config, br, _tierConfig);
            Object.DestroyImmediate(br);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsUnlocked_OneBelowMinRating_ReturnsFalse()
        {
            SetField(_config, "_minRating", 300);

            var br = BuildRatingSO(299); // one below MinRating
            bool result = TournamentGatingEvaluator.IsUnlocked(_config, br, _tierConfig);
            Object.DestroyImmediate(br);

            Assert.IsFalse(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // GetLockReason
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void GetLockReason_NullConfig_ReturnsEmpty()
        {
            string reason = TournamentGatingEvaluator.GetLockReason(null, _buildRating, _tierConfig);
            Assert.AreEqual(string.Empty, reason);
        }

        [Test]
        public void GetLockReason_Unlocked_NoRequirements_ReturnsEmpty()
        {
            // config defaults → Unranked + 0 → unlocked
            string reason = TournamentGatingEvaluator.GetLockReason(_config, _buildRating, _tierConfig);
            Assert.AreEqual(string.Empty, reason);
        }

        [Test]
        public void GetLockReason_TierLocked_ContainsBronze()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze);
            // _buildRating has rating 0 → Unranked → fails Bronze
            string reason = TournamentGatingEvaluator.GetLockReason(_config, _buildRating, _tierConfig);
            StringAssert.Contains("Bronze", reason);
        }

        [Test]
        public void GetLockReason_RatingLocked_TierMet_ContainsMinRating()
        {
            SetField(_config, "_minRating", 500);
            // tier OK (Unranked ≥ Unranked), rating fails (0 < 500)
            string reason = TournamentGatingEvaluator.GetLockReason(_config, _buildRating, _tierConfig);
            StringAssert.Contains("500", reason);
            StringAssert.Contains("0",   reason);
        }

        [Test]
        public void GetLockReason_BothFailed_TierReasonReturnedFirst()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze);
            SetField(_config, "_minRating",     500);
            // _buildRating rating 0 → Unranked → tier fails; would also fail rating
            string reason = TournamentGatingEvaluator.GetLockReason(_config, _buildRating, _tierConfig);
            // Tier reason first: "Requires Bronze tier ..."
            StringAssert.Contains("Bronze", reason);
            // Rating reason NOT mixed in
            StringAssert.DoesNotContain("500", reason);
        }
    }
}
