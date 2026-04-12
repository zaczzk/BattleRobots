using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotStatsAggregator.ApplySynergies"/>.
    ///
    /// Covers:
    ///   • null / empty active-synergy list → base stats returned unchanged.
    ///   • Single active synergy: each of the four bonus fields applied correctly.
    ///   • Multiple synergies: bonuses stack additively.
    ///   • Zero bonus fields on a synergy → stats unchanged.
    ///   • Armor clamps at 100 when sum exceeds cap.
    ///   • null entry inside the list is skipped gracefully.
    /// </summary>
    public class RobotStatsAggregatorSynergyTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static PartSynergyEntry MakeEntry(
            int   healthBonus          = 0,
            float speedMultiplierBonus = 0f,
            float damageMultiplierBonus = 0f,
            int   armorBonus           = 0)
        {
            return new PartSynergyEntry
            {
                displayName           = "Test Synergy",
                bonusDescription      = "",
                requirements          = new List<PartSynergyRequirement>(),
                healthBonus           = healthBonus,
                speedMultiplierBonus  = speedMultiplierBonus,
                damageMultiplierBonus = damageMultiplierBonus,
                armorBonus            = armorBonus,
            };
        }

        // ── Base stats fixture ────────────────────────────────────────────────

        private readonly RobotCombatStats _base = new RobotCombatStats(
            totalMaxHealth:            100f,
            effectiveSpeed:            5f,
            effectiveDamageMultiplier: 1.0f,
            totalArmorRating:          20);

        // ── Null / empty guards ───────────────────────────────────────────────

        [Test]
        public void ApplySynergies_NullSynergies_ReturnsBaseStatsUnchanged()
        {
            var result = RobotStatsAggregator.ApplySynergies(_base, null);
            Assert.AreEqual(_base, result);
        }

        [Test]
        public void ApplySynergies_EmptySynergies_ReturnsBaseStatsUnchanged()
        {
            var result = RobotStatsAggregator.ApplySynergies(
                _base, new List<PartSynergyEntry>());
            Assert.AreEqual(_base, result);
        }

        // ── Single-synergy applications ───────────────────────────────────────

        [Test]
        public void ApplySynergies_SingleSynergy_AppliesHealthBonus()
        {
            var synergy = MakeEntry(healthBonus: 30);
            var result  = RobotStatsAggregator.ApplySynergies(_base, new[] { synergy });

            Assert.AreEqual(130f, result.TotalMaxHealth, 0.001f); // 100 + 30
            // Other stats unchanged
            Assert.AreEqual(_base.EffectiveSpeed,            result.EffectiveSpeed,            0.001f);
            Assert.AreEqual(_base.EffectiveDamageMultiplier, result.EffectiveDamageMultiplier, 0.001f);
            Assert.AreEqual(_base.TotalArmorRating,          result.TotalArmorRating);
        }

        [Test]
        public void ApplySynergies_SingleSynergy_AppliesArmorBonus()
        {
            var synergy = MakeEntry(armorBonus: 15);
            var result  = RobotStatsAggregator.ApplySynergies(_base, new[] { synergy });

            Assert.AreEqual(35, result.TotalArmorRating); // 20 + 15
            Assert.AreEqual(_base.TotalMaxHealth,            result.TotalMaxHealth,            0.001f);
            Assert.AreEqual(_base.EffectiveSpeed,            result.EffectiveSpeed,            0.001f);
            Assert.AreEqual(_base.EffectiveDamageMultiplier, result.EffectiveDamageMultiplier, 0.001f);
        }

        [Test]
        public void ApplySynergies_SingleSynergy_AppliesSpeedBonus()
        {
            // speedMultiplierBonus = 0.15 → EffectiveSpeed × 1.15
            var synergy = MakeEntry(speedMultiplierBonus: 0.15f);
            var result  = RobotStatsAggregator.ApplySynergies(_base, new[] { synergy });

            Assert.AreEqual(5f * 1.15f, result.EffectiveSpeed, 0.001f);
            Assert.AreEqual(_base.TotalMaxHealth,            result.TotalMaxHealth,            0.001f);
            Assert.AreEqual(_base.EffectiveDamageMultiplier, result.EffectiveDamageMultiplier, 0.001f);
            Assert.AreEqual(_base.TotalArmorRating,          result.TotalArmorRating);
        }

        [Test]
        public void ApplySynergies_SingleSynergy_AppliesDamageBonus()
        {
            // damageMultiplierBonus = 0.10 → EffectiveDamageMultiplier × 1.10
            var synergy = MakeEntry(damageMultiplierBonus: 0.10f);
            var result  = RobotStatsAggregator.ApplySynergies(_base, new[] { synergy });

            Assert.AreEqual(1.0f * 1.10f, result.EffectiveDamageMultiplier, 0.001f);
            Assert.AreEqual(_base.TotalMaxHealth,   result.TotalMaxHealth,   0.001f);
            Assert.AreEqual(_base.EffectiveSpeed,   result.EffectiveSpeed,   0.001f);
            Assert.AreEqual(_base.TotalArmorRating, result.TotalArmorRating);
        }

        // ── Multiple synergies stack additively ───────────────────────────────

        [Test]
        public void ApplySynergies_MultipleSynergies_BonusesStackAdditively()
        {
            var s1 = MakeEntry(healthBonus: 20, speedMultiplierBonus: 0.10f);
            var s2 = MakeEntry(healthBonus: 10, damageMultiplierBonus: 0.20f, armorBonus: 5);

            var result = RobotStatsAggregator.ApplySynergies(_base, new[] { s1, s2 });

            // Health: 100 + 20 + 10 = 130
            Assert.AreEqual(130f, result.TotalMaxHealth, 0.001f);
            // Speed: 5 × (1 + 0.10) = 5.5
            Assert.AreEqual(5f * 1.10f, result.EffectiveSpeed, 0.001f);
            // Damage: 1.0 × (1 + 0.20) = 1.20
            Assert.AreEqual(1.0f * 1.20f, result.EffectiveDamageMultiplier, 0.001f);
            // Armor: 20 + 5 = 25
            Assert.AreEqual(25, result.TotalArmorRating);
        }

        // ── Zero bonus fields ─────────────────────────────────────────────────

        [Test]
        public void ApplySynergies_ZeroBonusFields_StatsUnchanged()
        {
            // An active synergy with all-zero bonuses should produce identical stats.
            var synergy = MakeEntry(); // all defaults are 0
            var result  = RobotStatsAggregator.ApplySynergies(_base, new[] { synergy });

            Assert.AreEqual(_base.TotalMaxHealth,            result.TotalMaxHealth,            0.001f);
            Assert.AreEqual(_base.EffectiveSpeed,            result.EffectiveSpeed,            0.001f);
            Assert.AreEqual(_base.EffectiveDamageMultiplier, result.EffectiveDamageMultiplier, 0.001f);
            Assert.AreEqual(_base.TotalArmorRating,          result.TotalArmorRating);
        }

        // ── Armor clamp ───────────────────────────────────────────────────────

        [Test]
        public void ApplySynergies_ArmorBonusExceedsCap_ClampedAt100()
        {
            // base armor = 20; bonus = 90 → raw sum = 110, clamped to 100
            var synergy = MakeEntry(armorBonus: 90);
            var result  = RobotStatsAggregator.ApplySynergies(_base, new[] { synergy });

            Assert.AreEqual(100, result.TotalArmorRating);
        }

        // ── Null entry in list ────────────────────────────────────────────────

        [Test]
        public void ApplySynergies_NullEntryInList_IsSkippedNoThrow()
        {
            var synergies = new List<PartSynergyEntry> { null, MakeEntry(healthBonus: 10) };
            RobotCombatStats result = default;

            Assert.DoesNotThrow(() =>
                result = RobotStatsAggregator.ApplySynergies(_base, synergies));

            // Only the non-null entry's bonus should be applied
            Assert.AreEqual(110f, result.TotalMaxHealth, 0.001f); // 100 + 10
        }
    }
}
