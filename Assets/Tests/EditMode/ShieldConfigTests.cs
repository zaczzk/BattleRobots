using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ShieldConfig"/>.
    ///
    /// Covers:
    ///   • Default values for MaxShieldHP, RechargeRate, and RechargeDelay.
    ///   • Property accessors expose correct inspector field values.
    /// </summary>
    public class ShieldConfigTests
    {
        private ShieldConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ShieldConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            _config = null;
        }

        // ── Defaults ──────────────────────────────────────────────────────────

        [Test]
        public void MaxShieldHP_Default_IsPositive()
        {
            Assert.Greater(_config.MaxShieldHP, 0f);
        }

        [Test]
        public void RechargeRate_Default_IsPositive()
        {
            Assert.Greater(_config.RechargeRate, 0f);
        }

        [Test]
        public void RechargeDelay_Default_IsNonNegative()
        {
            Assert.GreaterOrEqual(_config.RechargeDelay, 0f);
        }

        [Test]
        public void MaxShieldHP_Default_Is50()
        {
            Assert.AreEqual(50f, _config.MaxShieldHP, 0.001f);
        }

        [Test]
        public void RechargeRate_Default_Is10()
        {
            Assert.AreEqual(10f, _config.RechargeRate, 0.001f);
        }

        [Test]
        public void RechargeDelay_Default_Is3()
        {
            Assert.AreEqual(3f, _config.RechargeDelay, 0.001f);
        }
    }
}
