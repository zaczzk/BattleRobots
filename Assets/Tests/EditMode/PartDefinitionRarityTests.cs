using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the <see cref="PartRarity"/> field on <see cref="PartDefinition"/>.
    ///
    /// Covers:
    ///   • Default rarity is Common.
    ///   • Rarity property round-trips for Uncommon and Legendary via reflection.
    ///   • Rarity property exists on PartDefinition.
    ///   • All five PartRarity enum values can be assigned and read back.
    /// </summary>
    public class PartDefinitionRarityTests
    {
        private static FieldInfo RarityField =>
            typeof(PartDefinition).GetField(
                "_rarity", BindingFlags.NonPublic | BindingFlags.Instance);

        [Test]
        public void FreshInstance_Rarity_IsCommon()
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            Assert.AreEqual(PartRarity.Common, part.Rarity);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Rarity_ReflectionRoundTrip_Uncommon()
        {
            var part  = ScriptableObject.CreateInstance<PartDefinition>();
            var field = RarityField;
            Assert.IsNotNull(field, "_rarity field not found on PartDefinition.");
            field.SetValue(part, PartRarity.Uncommon);
            Assert.AreEqual(PartRarity.Uncommon, part.Rarity);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Rarity_ReflectionRoundTrip_Legendary()
        {
            var part  = ScriptableObject.CreateInstance<PartDefinition>();
            var field = RarityField;
            Assert.IsNotNull(field, "_rarity field not found on PartDefinition.");
            field.SetValue(part, PartRarity.Legendary);
            Assert.AreEqual(PartRarity.Legendary, part.Rarity);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void PartDefinition_HasRarityProperty()
        {
            var prop = typeof(PartDefinition).GetProperty("Rarity");
            Assert.IsNotNull(prop, "PartDefinition must expose a public Rarity property.");
        }

        [Test]
        public void Rarity_AllEnumValues_AssignableAndReadable()
        {
            var part  = ScriptableObject.CreateInstance<PartDefinition>();
            var field = RarityField;
            Assert.IsNotNull(field, "_rarity field not found on PartDefinition.");

            foreach (PartRarity rarity in System.Enum.GetValues(typeof(PartRarity)))
            {
                field.SetValue(part, rarity);
                Assert.AreEqual(rarity, part.Rarity, $"Round-trip failed for PartRarity.{rarity}");
            }

            Object.DestroyImmediate(part);
        }
    }
}
