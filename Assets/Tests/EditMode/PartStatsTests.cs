using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the <see cref="PartStats"/> struct and its integration
    /// with <see cref="PartDefinition.Stats"/>.
    ///
    /// Covers:
    ///   • PartStats.Default neutral values (zero bonus, 1.0 multipliers, zero armor).
    ///   • PartDefinition fresh-instance returns Default via the Stats property.
    ///   • Custom PartStats correctly survive a reflection-based field write.
    /// </summary>
    public class PartStatsTests
    {
        // ── PartStats.Default ──────────────────────────────────────────────────

        [Test]
        public void Default_HealthBonus_IsZero()
        {
            Assert.AreEqual(0, PartStats.Default.healthBonus);
        }

        [Test]
        public void Default_SpeedMultiplier_IsOne()
        {
            Assert.AreEqual(1f, PartStats.Default.speedMultiplier);
        }

        [Test]
        public void Default_DamageMultiplier_IsOne()
        {
            Assert.AreEqual(1f, PartStats.Default.damageMultiplier);
        }

        [Test]
        public void Default_ArmorRating_IsZero()
        {
            Assert.AreEqual(0, PartStats.Default.armorRating);
        }

        // ── PartDefinition.Stats integration ──────────────────────────────────

        private PartDefinition _part;

        [SetUp]
        public void SetUp()
        {
            _part = ScriptableObject.CreateInstance<PartDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_part);
            _part = null;
        }

        [Test]
        public void FreshPartDefinition_Stats_HealthBonus_IsZero()
        {
            Assert.AreEqual(0, _part.Stats.healthBonus);
        }

        [Test]
        public void FreshPartDefinition_Stats_SpeedMultiplier_IsOne()
        {
            Assert.AreEqual(1f, _part.Stats.speedMultiplier);
        }

        [Test]
        public void FreshPartDefinition_Stats_DamageMultiplier_IsOne()
        {
            Assert.AreEqual(1f, _part.Stats.damageMultiplier);
        }

        [Test]
        public void FreshPartDefinition_Stats_ArmorRating_IsZero()
        {
            Assert.AreEqual(0, _part.Stats.armorRating);
        }

        [Test]
        public void SetStats_ViaReflection_Stats_ReturnsInjectedValues()
        {
            var injected = new PartStats
            {
                healthBonus      = 50,
                speedMultiplier  = 1.5f,
                damageMultiplier = 2f,
                armorRating      = 30,
            };

            FieldInfo field = typeof(PartDefinition).GetField(
                "_stats", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Reflection: '_stats' not found on PartDefinition.");
            field.SetValue(_part, injected);

            Assert.AreEqual(50,   _part.Stats.healthBonus);
            Assert.AreEqual(1.5f, _part.Stats.speedMultiplier);
            Assert.AreEqual(2f,   _part.Stats.damageMultiplier);
            Assert.AreEqual(30,   _part.Stats.armorRating);
        }

        // ── Struct value semantics ─────────────────────────────────────────────

        [Test]
        public void PartStats_IsValueType()
        {
            // Struct copy should be independent of the original.
            PartStats original = PartStats.Default;
            PartStats copy     = original;
            copy.healthBonus   = 99;

            Assert.AreEqual(0,  original.healthBonus, "Mutating copy should not affect original.");
            Assert.AreEqual(99, copy.healthBonus);
        }
    }
}
