using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the upgrade-aware overload:
    /// <see cref="RobotStatsAggregator.Compute(RobotDefinition,
    ///     IEnumerable{PartDefinition}, PlayerPartUpgrades, PartUpgradeConfig)"/>.
    ///
    /// Covers:
    ///   • null upgrades / null config → falls back to base Compute (same result)
    ///   • tier 0 → result identical to base Compute
    ///   • tier 1 → health bonus, armor, speed bonus, damage bonus scaled correctly
    ///   • tier at max (3) → all stats at max multiplier
    ///   • null robot definition → zero stats (no throw)
    ///   • null part entry inside collection → skipped
    ///   • neutral part (all multipliers = 1.0, bonuses = 0) unaffected by tier
    /// </summary>
    public class RobotStatsAggregatorUpgradeTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetRobotField(RobotDefinition rob, string name, object value)
        {
            FieldInfo fi = typeof(RobotDefinition)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on RobotDefinition.");
            fi.SetValue(rob, value);
        }

        private static void SetPartField(PartDefinition part, string name, object value)
        {
            FieldInfo fi = typeof(PartDefinition)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on PartDefinition.");
            fi.SetValue(part, value);
        }

        private static void SetConfigField(PartUpgradeConfig cfg, string name, object value)
        {
            FieldInfo fi = typeof(PartUpgradeConfig)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on PartUpgradeConfig.");
            fi.SetValue(cfg, value);
        }

        private static void SetUpgradesField(PlayerPartUpgrades u, string name, object value)
        {
            FieldInfo fi = typeof(PlayerPartUpgrades)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on PlayerPartUpgrades.");
            fi.SetValue(u, value);
        }

        // ── Fixture data ──────────────────────────────────────────────────────

        private RobotDefinition   _robot;
        private PartDefinition    _part;
        private PlayerPartUpgrades _upgrades;
        private PartUpgradeConfig  _config;

        [SetUp]
        public void SetUp()
        {
            // Robot: base HP=100, base speed=5
            _robot = ScriptableObject.CreateInstance<RobotDefinition>();
            SetRobotField(_robot, "_maxHitPoints", 100f);
            SetRobotField(_robot, "_moveSpeed",    5f);

            // Part: healthBonus=20, speedMultiplier=1.2, damageMultiplier=1.3, armorRating=10
            _part = ScriptableObject.CreateInstance<PartDefinition>();
            SetPartField(_part, "_partId", "arm_heavy");
            var stats = new PartStats
            {
                healthBonus      = 20,
                speedMultiplier  = 1.2f,
                damageMultiplier = 1.3f,
                armorRating      = 10,
            };
            SetPartField(_part, "_stats", stats);

            // Config: maxTier=3, costs=[100,250,500], mults=[1.0, 1.1, 1.25, 1.5]
            _config = ScriptableObject.CreateInstance<PartUpgradeConfig>();
            SetConfigField(_config, "_maxTier",             3);
            SetConfigField(_config, "_tierCosts",           new int[]   { 100, 250, 500 });
            SetConfigField(_config, "_tierStatMultipliers", new float[] { 1.0f, 1.1f, 1.25f, 1.5f });

            // Upgrades SO (no event wired — EditMode, not needed for tests)
            _upgrades = ScriptableObject.CreateInstance<PlayerPartUpgrades>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_robot);
            Object.DestroyImmediate(_part);
            Object.DestroyImmediate(_upgrades);
            Object.DestroyImmediate(_config);
        }

        // ── Null / fallback behaviour ─────────────────────────────────────────

        [Test]
        public void NullUpgrades_FallsBackToBaseCompute()
        {
            var parts = new[] { _part };
            var withNull  = RobotStatsAggregator.Compute(_robot, parts, null, _config);
            var baseResult = RobotStatsAggregator.Compute(_robot, parts);
            Assert.AreEqual(baseResult, withNull);
        }

        [Test]
        public void NullConfig_FallsBackToBaseCompute()
        {
            var parts = new[] { _part };
            var withNull  = RobotStatsAggregator.Compute(_robot, parts, _upgrades, null);
            var baseResult = RobotStatsAggregator.Compute(_robot, parts);
            Assert.AreEqual(baseResult, withNull);
        }

        [Test]
        public void NullRobotDefinition_ReturnsZeroStats_NoThrow()
        {
            RobotCombatStats result = default;
            Assert.DoesNotThrow(() =>
                result = RobotStatsAggregator.Compute(null, new[] { _part }, _upgrades, _config));
            Assert.AreEqual(new RobotCombatStats(0f, 0f, 0f, 0), result);
        }

        [Test]
        public void NullPartInCollection_IsSkipped_NoThrow()
        {
            var parts = new PartDefinition[] { null, _part };
            RobotCombatStats result = default;
            Assert.DoesNotThrow(() =>
                result = RobotStatsAggregator.Compute(_robot, parts, _upgrades, _config));
            // Should only count _part's contribution (tier 0 = mult 1.0 = same as base)
            var expected = RobotStatsAggregator.Compute(_robot, new[] { _part });
            Assert.AreEqual(expected, result);
        }

        // ── Tier 0 is identical to base ───────────────────────────────────────

        [Test]
        public void Tier0_HealthBonus_SameAsBase()
        {
            // Tier 0 mult = 1.0 → scaled health = 20 × 1.0 = 20
            var result = RobotStatsAggregator.Compute(_robot, new[] { _part }, _upgrades, _config);
            Assert.AreEqual(120f, result.TotalMaxHealth, 0.01f); // 100 + 20
        }

        [Test]
        public void Tier0_UpgradeResult_EqualsBaseResult()
        {
            var upgrade = RobotStatsAggregator.Compute(_robot, new[] { _part }, _upgrades, _config);
            var   baseR = RobotStatsAggregator.Compute(_robot, new[] { _part });
            Assert.AreEqual(baseR, upgrade);
        }

        // ── Tier 1 scaling (mult = 1.1) ───────────────────────────────────────

        [Test]
        public void Tier1_HealthBonus_Scaled()
        {
            _upgrades.SetTier("arm_heavy", 1); // mult = 1.1
            var result = RobotStatsAggregator.Compute(_robot, new[] { _part }, _upgrades, _config);
            // healthBonus = (int)(20 × 1.1) = (int)22.0 = 22 → total HP = 122
            Assert.AreEqual(122f, result.TotalMaxHealth, 0.01f);
        }

        [Test]
        public void Tier1_ArmorRating_Scaled()
        {
            _upgrades.SetTier("arm_heavy", 1); // mult = 1.1
            var result = RobotStatsAggregator.Compute(_robot, new[] { _part }, _upgrades, _config);
            // armorRating = (int)(10 × 1.1) = (int)11.0 = 11
            Assert.AreEqual(11, result.TotalArmorRating);
        }

        [Test]
        public void Tier1_SpeedBonus_Scaled()
        {
            _upgrades.SetTier("arm_heavy", 1); // mult = 1.1
            var result = RobotStatsAggregator.Compute(_robot, new[] { _part }, _upgrades, _config);
            // speedMult contribution: 1 + (1.2 - 1) * 1.1 = 1 + 0.22 = 1.22
            // finalSpeed = 5 * 1.22 = 6.1
            Assert.AreEqual(5f * 1.22f, result.EffectiveSpeed, 0.01f);
        }

        [Test]
        public void Tier1_DamageMult_Scaled()
        {
            _upgrades.SetTier("arm_heavy", 1); // mult = 1.1
            var result = RobotStatsAggregator.Compute(_robot, new[] { _part }, _upgrades, _config);
            // damageMult contribution: 1 + (1.3 - 1) * 1.1 = 1 + 0.33 = 1.33
            Assert.AreEqual(1f + (1.3f - 1f) * 1.1f, result.EffectiveDamageMultiplier, 0.001f);
        }

        // ── Max tier (3, mult = 1.5) ──────────────────────────────────────────

        [Test]
        public void MaxTier_HealthBonus_AtMaxMultiplier()
        {
            _upgrades.SetTier("arm_heavy", 3); // mult = 1.5
            var result = RobotStatsAggregator.Compute(_robot, new[] { _part }, _upgrades, _config);
            // healthBonus = (int)(20 × 1.5) = 30 → total HP = 130
            Assert.AreEqual(130f, result.TotalMaxHealth, 0.01f);
        }

        [Test]
        public void MaxTier_SpeedBonus_AtMaxMultiplier()
        {
            _upgrades.SetTier("arm_heavy", 3); // mult = 1.5
            var result = RobotStatsAggregator.Compute(_robot, new[] { _part }, _upgrades, _config);
            // speedMult contribution: 1 + (1.2 - 1) * 1.5 = 1 + 0.30 = 1.30
            Assert.AreEqual(5f * 1.30f, result.EffectiveSpeed, 0.01f);
        }

        // ── Neutral part unaffected by tier ───────────────────────────────────

        [Test]
        public void NeutralPart_UnaffectedByAnyTier()
        {
            // Override part with all neutral stats (PartStats.Default)
            SetPartField(_part, "_stats", PartStats.Default);

            _upgrades.SetTier("arm_heavy", 3);
            var result   = RobotStatsAggregator.Compute(_robot, new[] { _part }, _upgrades, _config);
            var expected = RobotStatsAggregator.Compute(_robot, new[] { _part });
            Assert.AreEqual(expected, result);
        }
    }
}
