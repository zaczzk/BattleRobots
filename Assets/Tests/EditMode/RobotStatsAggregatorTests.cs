using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotStatsAggregator.Compute"/>.
    ///
    /// Covers:
    ///   • Null robotDefinition → zero stats, no throw.
    ///   • No parts (null collection, empty list) → base stats only.
    ///   • Single part — each stat field in isolation.
    ///   • Multiple parts — additive health, multiplicative speed/damage, clamped armor.
    ///   • Armor clamped to 100 when sum exceeds limit.
    ///   • Null entries in parts collection silently skipped.
    ///   • RobotCombatStats equality helpers.
    /// </summary>
    public class RobotStatsAggregatorTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private RobotDefinition _robot;
        private List<PartDefinition> _parts;

        [SetUp]
        public void SetUp()
        {
            _robot = ScriptableObject.CreateInstance<RobotDefinition>();
            _parts = new List<PartDefinition>();

            // Inject known base stats via reflection.
            SetRobotField("_maxHitPoints", 100f);
            SetRobotField("_moveSpeed",     5f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var p in _parts)
                if (p != null) Object.DestroyImmediate(p);
            _parts.Clear();
            Object.DestroyImmediate(_robot);
            _robot = null;
        }

        private void SetRobotField(string fieldName, float value)
        {
            FieldInfo f = typeof(RobotDefinition).GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Reflection: '{fieldName}' not found on RobotDefinition.");
            f.SetValue(_robot, value);
        }

        private PartDefinition MakePart(PartStats stats)
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            FieldInfo f = typeof(PartDefinition).GetField(
                "_stats", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "Reflection: '_stats' not found on PartDefinition.");
            f.SetValue(part, stats);
            _parts.Add(part);
            return part;
        }

        // ── Null definition ───────────────────────────────────────────────────

        [Test]
        public void NullDefinition_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => RobotStatsAggregator.Compute(null, null));
        }

        [Test]
        public void NullDefinition_TotalMaxHealth_IsZero()
        {
            var result = RobotStatsAggregator.Compute(null, null);
            Assert.AreEqual(0f, result.TotalMaxHealth);
        }

        [Test]
        public void NullDefinition_EffectiveSpeed_IsZero()
        {
            var result = RobotStatsAggregator.Compute(null, null);
            Assert.AreEqual(0f, result.EffectiveSpeed);
        }

        // ── Base stats only (no parts) ─────────────────────────────────────────

        [Test]
        public void NoParts_TotalMaxHealth_EqualsBaseMaxHitPoints()
        {
            var result = RobotStatsAggregator.Compute(_robot, null);
            Assert.AreEqual(100f, result.TotalMaxHealth);
        }

        [Test]
        public void EmptyList_TotalMaxHealth_EqualsBaseMaxHitPoints()
        {
            var result = RobotStatsAggregator.Compute(_robot, new List<PartDefinition>());
            Assert.AreEqual(100f, result.TotalMaxHealth);
        }

        [Test]
        public void NoParts_EffectiveSpeed_EqualsBaseMoveSpeed()
        {
            var result = RobotStatsAggregator.Compute(_robot, null);
            Assert.AreEqual(5f, result.EffectiveSpeed);
        }

        [Test]
        public void NoParts_EffectiveDamageMultiplier_IsOne()
        {
            var result = RobotStatsAggregator.Compute(_robot, null);
            Assert.AreEqual(1f, result.EffectiveDamageMultiplier);
        }

        [Test]
        public void NoParts_TotalArmorRating_IsZero()
        {
            var result = RobotStatsAggregator.Compute(_robot, null);
            Assert.AreEqual(0, result.TotalArmorRating);
        }

        // ── Single part — individual stat fields ───────────────────────────────

        [Test]
        public void SinglePart_HealthBonus_AddedToBase()
        {
            MakePart(new PartStats
                { healthBonus = 50, speedMultiplier = 1f, damageMultiplier = 1f, armorRating = 0 });

            var result = RobotStatsAggregator.Compute(_robot, _parts);

            Assert.AreEqual(150f, result.TotalMaxHealth);
        }

        [Test]
        public void SinglePart_SpeedMultiplier_MultipliesBase()
        {
            MakePart(new PartStats
                { healthBonus = 0, speedMultiplier = 2f, damageMultiplier = 1f, armorRating = 0 });

            var result = RobotStatsAggregator.Compute(_robot, _parts);

            Assert.AreEqual(10f, result.EffectiveSpeed, 1e-5f);
        }

        [Test]
        public void SinglePart_DamageMultiplier_Returned()
        {
            MakePart(new PartStats
                { healthBonus = 0, speedMultiplier = 1f, damageMultiplier = 1.5f, armorRating = 0 });

            var result = RobotStatsAggregator.Compute(_robot, _parts);

            Assert.AreEqual(1.5f, result.EffectiveDamageMultiplier, 1e-5f);
        }

        [Test]
        public void SinglePart_ArmorRating_Returned()
        {
            MakePart(new PartStats
                { healthBonus = 0, speedMultiplier = 1f, damageMultiplier = 1f, armorRating = 25 });

            var result = RobotStatsAggregator.Compute(_robot, _parts);

            Assert.AreEqual(25, result.TotalArmorRating);
        }

        // ── Multiple parts ─────────────────────────────────────────────────────

        [Test]
        public void TwoParts_HealthBonus_Summed()
        {
            MakePart(new PartStats { healthBonus = 20, speedMultiplier = 1f, damageMultiplier = 1f, armorRating = 0 });
            MakePart(new PartStats { healthBonus = 30, speedMultiplier = 1f, damageMultiplier = 1f, armorRating = 0 });

            var result = RobotStatsAggregator.Compute(_robot, _parts);

            Assert.AreEqual(150f, result.TotalMaxHealth);
        }

        [Test]
        public void TwoParts_SpeedMultiplier_IsProduct()
        {
            // 5 * 2.0 * 0.5 = 5
            MakePart(new PartStats { healthBonus = 0, speedMultiplier = 2.0f, damageMultiplier = 1f, armorRating = 0 });
            MakePart(new PartStats { healthBonus = 0, speedMultiplier = 0.5f, damageMultiplier = 1f, armorRating = 0 });

            var result = RobotStatsAggregator.Compute(_robot, _parts);

            Assert.AreEqual(5f, result.EffectiveSpeed, 1e-5f);
        }

        [Test]
        public void TwoParts_DamageMultiplier_IsProduct()
        {
            MakePart(new PartStats { healthBonus = 0, speedMultiplier = 1f, damageMultiplier = 2f,  armorRating = 0 });
            MakePart(new PartStats { healthBonus = 0, speedMultiplier = 1f, damageMultiplier = 1.5f,armorRating = 0 });

            var result = RobotStatsAggregator.Compute(_robot, _parts);

            Assert.AreEqual(3f, result.EffectiveDamageMultiplier, 1e-5f);
        }

        [Test]
        public void TwoParts_ArmorRating_Summed()
        {
            MakePart(new PartStats { healthBonus = 0, speedMultiplier = 1f, damageMultiplier = 1f, armorRating = 40 });
            MakePart(new PartStats { healthBonus = 0, speedMultiplier = 1f, damageMultiplier = 1f, armorRating = 30 });

            var result = RobotStatsAggregator.Compute(_robot, _parts);

            Assert.AreEqual(70, result.TotalArmorRating);
        }

        [Test]
        public void Armor_ClampedAt100_WhenSumExceedsLimit()
        {
            MakePart(new PartStats { healthBonus = 0, speedMultiplier = 1f, damageMultiplier = 1f, armorRating = 80 });
            MakePart(new PartStats { healthBonus = 0, speedMultiplier = 1f, damageMultiplier = 1f, armorRating = 80 });

            var result = RobotStatsAggregator.Compute(_robot, _parts);

            Assert.AreEqual(100, result.TotalArmorRating);
        }

        // ── Null entries in collection ────────────────────────────────────────

        [Test]
        public void NullPartEntriesInCollection_AreSkipped_NoThrow()
        {
            var mixed = new List<PartDefinition> { null, null };

            RobotCombatStats result = default;
            Assert.DoesNotThrow(() => result = RobotStatsAggregator.Compute(_robot, mixed));
            Assert.AreEqual(100f, result.TotalMaxHealth);
        }

        // ── RobotCombatStats equality ──────────────────────────────────────────

        [Test]
        public void RobotCombatStats_EqualityOperator_SameValues_ReturnsTrue()
        {
            var a = new RobotCombatStats(100f, 5f, 1f, 0);
            var b = new RobotCombatStats(100f, 5f, 1f, 0);
            Assert.IsTrue(a == b);
        }

        [Test]
        public void RobotCombatStats_InequalityOperator_DifferentHealth_ReturnsTrue()
        {
            var a = new RobotCombatStats(100f, 5f, 1f, 0);
            var b = new RobotCombatStats(150f, 5f, 1f, 0);
            Assert.IsTrue(a != b);
        }
    }
}
