using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchRewardConfig"/>.
    ///
    /// Covers:
    ///   • Fresh-instance default values (BaseWinReward=200, ConsolationReward=50, RoundDuration=120).
    ///   • Non-negative / minimum-value range contracts.
    ///   • Design constraint: ConsolationReward ≤ BaseWinReward.
    ///   • Property return types.
    ///   • Reflection-injected field values are reflected correctly through properties.
    ///   • Zero values are valid for both reward fields.
    ///   • Minimum-value edge cases (RoundDuration at 10 s boundary).
    ///
    /// <see cref="MatchRewardConfig"/> is a <see cref="ScriptableObject"/>;
    /// instances are created via <c>ScriptableObject.CreateInstance</c> and
    /// destroyed in TearDown — no scene required.
    /// Private serialised fields are injected via reflection to exercise the property
    /// getters independently of the Inspector's [Min] attribute clamping.
    /// </summary>
    public class MatchRewardConfigTests
    {
        // ── Instance ──────────────────────────────────────────────────────────

        private MatchRewardConfig _config;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<MatchRewardConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            _config = null;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_BaseWinReward_Is200()
        {
            Assert.AreEqual(200, _config.BaseWinReward);
        }

        [Test]
        public void FreshInstance_ConsolationReward_Is50()
        {
            Assert.AreEqual(50, _config.ConsolationReward);
        }

        [Test]
        public void FreshInstance_RoundDuration_Is120()
        {
            Assert.AreEqual(120f, _config.RoundDuration, 0.001f);
        }

        // ── Range / non-negativity contracts ─────────────────────────────────

        [Test]
        public void FreshInstance_BaseWinReward_IsNonNegative()
        {
            Assert.GreaterOrEqual(_config.BaseWinReward, 0,
                "BaseWinReward must never be negative.");
        }

        [Test]
        public void FreshInstance_ConsolationReward_IsNonNegative()
        {
            Assert.GreaterOrEqual(_config.ConsolationReward, 0,
                "ConsolationReward must never be negative.");
        }

        [Test]
        public void FreshInstance_RoundDuration_AtLeast10Seconds()
        {
            Assert.GreaterOrEqual(_config.RoundDuration, 10f,
                "RoundDuration must be at least 10 s to match MatchManager's [Min(10f)] guard.");
        }

        // ── Design contract: consolation ≤ win reward ─────────────────────────

        [Test]
        public void FreshInstance_ConsolationReward_LessOrEqualToBaseWinReward()
        {
            Assert.LessOrEqual(_config.ConsolationReward, _config.BaseWinReward,
                "ConsolationReward must not exceed BaseWinReward — " +
                "losing a match should reward less currency than winning.");
        }

        // ── Reflection injection: properties mirror backing fields ─────────────

        [Test]
        public void BaseWinReward_ReturnsInjectedValue()
        {
            SetField(_config, "_baseWinReward", 500);
            Assert.AreEqual(500, _config.BaseWinReward);
        }

        [Test]
        public void ConsolationReward_ReturnsInjectedValue()
        {
            SetField(_config, "_consolationReward", 75);
            Assert.AreEqual(75, _config.ConsolationReward);
        }

        [Test]
        public void RoundDuration_ReturnsInjectedValue()
        {
            SetField(_config, "_roundDuration", 60f);
            Assert.AreEqual(60f, _config.RoundDuration, 0.001f);
        }

        // ── Zero values are valid ─────────────────────────────────────────────

        [Test]
        public void ZeroBaseWinReward_IsValid()
        {
            SetField(_config, "_baseWinReward", 0);
            Assert.AreEqual(0, _config.BaseWinReward,
                "A BaseWinReward of 0 is a valid (if unusual) configuration.");
        }

        [Test]
        public void ZeroConsolationReward_IsValid()
        {
            SetField(_config, "_consolationReward", 0);
            Assert.AreEqual(0, _config.ConsolationReward,
                "A ConsolationReward of 0 is valid — some games give no loss reward.");
        }

        // ── Minimum RoundDuration edge case ───────────────────────────────────

        [Test]
        public void RoundDuration_ExactlyAtMinimum_Is10Seconds()
        {
            SetField(_config, "_roundDuration", 10f);
            Assert.AreEqual(10f, _config.RoundDuration, 0.001f,
                "RoundDuration at the minimum boundary (10 s) must be returned as-is.");
        }

        // ── Large values are valid (no upper cap) ─────────────────────────────

        [Test]
        public void LargeBaseWinReward_IsValid()
        {
            SetField(_config, "_baseWinReward", 99999);
            Assert.AreEqual(99999, _config.BaseWinReward,
                "There is no upper cap on BaseWinReward — exotic game modes may use large values.");
        }

        [Test]
        public void LongRoundDuration_IsValid()
        {
            SetField(_config, "_roundDuration", 600f);
            Assert.AreEqual(600f, _config.RoundDuration, 0.001f,
                "RoundDuration has no upper cap — 10-minute rounds are a valid design choice.");
        }
    }
}
