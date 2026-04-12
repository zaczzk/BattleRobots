using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotTierConfig"/>.
    ///
    /// Covers:
    ///   • Fresh instance defaults (non-null list, empty).
    ///   • GetTier: empty config → Unranked.
    ///   • GetTier: rating below all thresholds → Unranked.
    ///   • GetTier: exact-threshold match → correct tier.
    ///   • GetTier: between two thresholds → lower tier.
    ///   • GetTier: meets all thresholds → Diamond (highest enum value).
    ///   • GetTier: above all thresholds → highest configured tier.
    ///   • GetDisplayName: no matching entry → tier.ToString() fallback.
    ///   • GetDisplayName: matching entry → configured display name.
    ///   • GetTierColor: no matching entry → Color.white fallback.
    ///   • GetTierColor: matching entry → configured colour.
    ///   • Thresholds property exposes IReadOnlyList contract.
    /// </summary>
    public class RobotTierConfigTests
    {
        private RobotTierConfig _config;

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<RobotTierConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null) Object.DestroyImmediate(_config);
        }

        // ── Reflection helpers ────────────────────────────────────────────────

        private static List<TierThresholdEntry> GetList(RobotTierConfig cfg)
        {
            FieldInfo fi = typeof(RobotTierConfig)
                .GetField("_thresholds", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "'_thresholds' field not found on RobotTierConfig.");
            return (List<TierThresholdEntry>)fi.GetValue(cfg);
        }

        private static TierThresholdEntry MakeEntry(
            RobotTierLevel tier,
            int            threshold,
            string         name  = "",
            Color?         color = null)
        {
            return new TierThresholdEntry
            {
                tier            = tier,
                ratingThreshold = threshold,
                displayName     = name,
                tintColor       = color ?? Color.white,
            };
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Thresholds_IsNotNull()
        {
            Assert.IsNotNull(_config.Thresholds);
        }

        [Test]
        public void FreshInstance_Thresholds_IsEmpty()
        {
            Assert.AreEqual(0, _config.Thresholds.Count);
        }

        // ── GetTier ───────────────────────────────────────────────────────────

        [Test]
        public void GetTier_EmptyConfig_ReturnsUnranked()
        {
            Assert.AreEqual(RobotTierLevel.Unranked, _config.GetTier(9999));
        }

        [Test]
        public void GetTier_RatingBelowAllThresholds_ReturnsUnranked()
        {
            GetList(_config).Add(MakeEntry(RobotTierLevel.Bronze, 100));
            Assert.AreEqual(RobotTierLevel.Unranked, _config.GetTier(50));
        }

        [Test]
        public void GetTier_ExactBronzeThreshold_ReturnsBronze()
        {
            GetList(_config).Add(MakeEntry(RobotTierLevel.Bronze, 100));
            Assert.AreEqual(RobotTierLevel.Bronze, _config.GetTier(100));
        }

        [Test]
        public void GetTier_AboveBronzeBelowSilver_ReturnsBronze()
        {
            var list = GetList(_config);
            list.Add(MakeEntry(RobotTierLevel.Bronze, 100));
            list.Add(MakeEntry(RobotTierLevel.Silver, 300));
            Assert.AreEqual(RobotTierLevel.Bronze, _config.GetTier(200));
        }

        [Test]
        public void GetTier_ExactDiamondThreshold_ReturnsDiamond()
        {
            var list = GetList(_config);
            list.Add(MakeEntry(RobotTierLevel.Bronze,   100));
            list.Add(MakeEntry(RobotTierLevel.Silver,   300));
            list.Add(MakeEntry(RobotTierLevel.Gold,     600));
            list.Add(MakeEntry(RobotTierLevel.Platinum, 900));
            list.Add(MakeEntry(RobotTierLevel.Diamond, 1200));
            Assert.AreEqual(RobotTierLevel.Diamond, _config.GetTier(1200));
        }

        [Test]
        public void GetTier_AboveAllThresholds_ReturnsHighestTier()
        {
            var list = GetList(_config);
            list.Add(MakeEntry(RobotTierLevel.Bronze,  100));
            list.Add(MakeEntry(RobotTierLevel.Diamond, 1200));
            Assert.AreEqual(RobotTierLevel.Diamond, _config.GetTier(9999));
        }

        // ── GetDisplayName ────────────────────────────────────────────────────

        [Test]
        public void GetDisplayName_NoMatchingEntry_ReturnsTierToString()
        {
            // Config has no entry for Gold — must fall back to enum name.
            Assert.AreEqual("Gold", _config.GetDisplayName(RobotTierLevel.Gold));
        }

        [Test]
        public void GetDisplayName_MatchingEntry_ReturnsConfiguredName()
        {
            GetList(_config).Add(MakeEntry(RobotTierLevel.Gold, 600, "Golden Warrior"));
            Assert.AreEqual("Golden Warrior", _config.GetDisplayName(RobotTierLevel.Gold));
        }

        // ── GetTierColor ──────────────────────────────────────────────────────

        [Test]
        public void GetTierColor_NoMatchingEntry_ReturnsWhite()
        {
            Assert.AreEqual(Color.white, _config.GetTierColor(RobotTierLevel.Silver));
        }

        [Test]
        public void GetTierColor_MatchingEntry_ReturnsConfiguredColor()
        {
            GetList(_config).Add(MakeEntry(RobotTierLevel.Silver, 300, "", Color.blue));
            Assert.AreEqual(Color.blue, _config.GetTierColor(RobotTierLevel.Silver));
        }

        // ── IReadOnlyList contract ────────────────────────────────────────────

        [Test]
        public void Thresholds_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<TierThresholdEntry>>(_config.Thresholds);
        }
    }
}
