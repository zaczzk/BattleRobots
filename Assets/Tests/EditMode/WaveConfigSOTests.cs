using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WaveConfigSO"/>.
    ///
    /// Covers:
    ///   • Default field values (base bots, increment, max, rewards).
    ///   • GetBotsForWave — wave 1, wave 2, large wave clamped to max.
    ///   • GetBotsForWave — wave &lt; 1 treated as wave 1.
    ///   • GetRewardForWave — wave 1 returns base; wave N stacks bonus.
    ///   • Roster property accessible (null by default).
    /// </summary>
    public class WaveConfigSOTests
    {
        private WaveConfigSO _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<WaveConfigSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_BaseBotsPerWave_IsOne()
        {
            Assert.AreEqual(1, _config.BaseBotsPerWave,
                "Default BaseBotsPerWave must be 1.");
        }

        [Test]
        public void FreshInstance_BotCountIncrement_IsOne()
        {
            Assert.AreEqual(1, _config.BotCountIncrement,
                "Default BotCountIncrement must be 1.");
        }

        [Test]
        public void FreshInstance_MaxBotsPerWave_IsTen()
        {
            Assert.AreEqual(10, _config.MaxBotsPerWave,
                "Default MaxBotsPerWave must be 10.");
        }

        [Test]
        public void FreshInstance_BaseWaveReward_IsFifty()
        {
            Assert.AreEqual(50f, _config.BaseWaveReward, 0.001f,
                "Default BaseWaveReward must be 50.");
        }

        [Test]
        public void FreshInstance_BonusRewardPerWave_IsTen()
        {
            Assert.AreEqual(10f, _config.BonusRewardPerWave, 0.001f,
                "Default BonusRewardPerWave must be 10.");
        }

        [Test]
        public void FreshInstance_Roster_IsNull()
        {
            Assert.IsNull(_config.Roster,
                "Default Roster must be null — no roster override.");
        }

        // ── GetBotsForWave ────────────────────────────────────────────────────

        [Test]
        public void GetBotsForWave_Wave1_ReturnsBase()
        {
            // default: base=1, increment=1 → wave 1 = 1 + 1*(1-1) = 1
            Assert.AreEqual(1, _config.GetBotsForWave(1),
                "Wave 1 must return BaseBotsPerWave (1).");
        }

        [Test]
        public void GetBotsForWave_Wave2_ReturnsBasePlusIncrement()
        {
            // default: base=1, increment=1 → wave 2 = 1 + 1*(2-1) = 2
            Assert.AreEqual(2, _config.GetBotsForWave(2),
                "Wave 2 must return base + increment = 2.");
        }

        [Test]
        public void GetBotsForWave_LargeWave_ClampedToMax()
        {
            // default: base=1, increment=1, max=10 → wave 100 = clamped to 10
            Assert.AreEqual(10, _config.GetBotsForWave(100),
                "Large wave must be clamped to MaxBotsPerWave (10).");
        }

        [Test]
        public void GetBotsForWave_WaveZeroOrNegative_TreatedAsWaveOne()
        {
            // wave ≤ 0 must be treated as wave 1
            Assert.AreEqual(_config.GetBotsForWave(1), _config.GetBotsForWave(0),
                "Wave 0 must return same count as wave 1.");
            Assert.AreEqual(_config.GetBotsForWave(1), _config.GetBotsForWave(-5),
                "Negative wave must return same count as wave 1.");
        }

        // ── GetRewardForWave ──────────────────────────────────────────────────

        [Test]
        public void GetRewardForWave_Wave1_ReturnsBase()
        {
            // default: base=50, bonus=10 → wave 1 = 50 + 10*(1-1) = 50
            Assert.AreEqual(50f, _config.GetRewardForWave(1), 0.001f,
                "Wave 1 reward must equal BaseWaveReward (50).");
        }

        [Test]
        public void GetRewardForWave_Wave3_ReturnsBaseAndTwoBonus()
        {
            // default: base=50, bonus=10 → wave 3 = 50 + 10*(3-1) = 70
            Assert.AreEqual(70f, _config.GetRewardForWave(3), 0.001f,
                "Wave 3 reward must equal 50 + 10*2 = 70.");
        }
    }
}
