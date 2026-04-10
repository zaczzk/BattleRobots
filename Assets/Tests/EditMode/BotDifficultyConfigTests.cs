using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="BotDifficultyConfig"/>.
    ///
    /// Verifies that all six read-only properties are accessible and satisfy the
    /// inspector constraints declared on their serialized backing fields.
    /// Designer-authored preset values (Easy/Normal/Hard) are validated during
    /// the Editor wiring session via OnValidate; these tests cover the code contract.
    /// </summary>
    public class BotDifficultyConfigTests
    {
        private BotDifficultyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BotDifficultyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            _config = null;
        }

        // ── Property accessibility smoke test ─────────────────────────────────

        [Test]
        public void FreshInstance_AllSixProperties_AreAccessibleWithoutThrowing()
        {
            Assert.DoesNotThrow(() =>
            {
                _ = _config.DetectionRange;
                _ = _config.AttackRange;
                _ = _config.AttackDamage;
                _ = _config.AttackCooldown;
                _ = _config.FacingThreshold;
                _ = _config.MoveSpeedMultiplier;
            });
        }

        // ── Default value constraints (matching inspector [Min] / [Range] attrs) ──

        [Test]
        public void FreshInstance_DetectionRange_IsPositive()
        {
            // Inspector: Min(0f); default 15f.
            Assert.Greater(_config.DetectionRange, 0f);
        }

        [Test]
        public void FreshInstance_AttackRange_IsPositive()
        {
            // Inspector: Min(0f); default 3f.
            Assert.Greater(_config.AttackRange, 0f);
        }

        [Test]
        public void FreshInstance_AttackDamage_IsNonNegative()
        {
            // Inspector: Min(0f); default 10f.
            Assert.GreaterOrEqual(_config.AttackDamage, 0f);
        }

        [Test]
        public void FreshInstance_AttackCooldown_MeetsMinimum()
        {
            // Inspector: Min(0.1f); guarantees at least one tick between attacks.
            Assert.GreaterOrEqual(_config.AttackCooldown, 0.1f);
        }

        [Test]
        public void FreshInstance_FacingThreshold_MeetsMinimum()
        {
            // Inspector: Min(1f); avoids zero-degree cone that would never match.
            Assert.GreaterOrEqual(_config.FacingThreshold, 1f);
        }

        [Test]
        public void FreshInstance_MoveSpeedMultiplier_IsInRange()
        {
            // Inspector: Range(0.1f, 3f).
            Assert.GreaterOrEqual(_config.MoveSpeedMultiplier, 0.1f);
            Assert.LessOrEqual(_config.MoveSpeedMultiplier, 3f);
        }

        // ── Relational constraint ─────────────────────────────────────────────

        [Test]
        public void FreshInstance_AttackRange_DoesNotExceedDetectionRange()
        {
            // A robot cannot attack what it hasn't detected.
            // Default inspector values (attack=3, detection=15) must satisfy this.
            Assert.LessOrEqual(_config.AttackRange, _config.DetectionRange);
        }
    }
}
