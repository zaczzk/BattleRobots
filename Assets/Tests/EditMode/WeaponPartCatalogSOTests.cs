using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WeaponPartCatalogSO"/>.
    ///
    /// Covers:
    ///   Fresh instance:
    ///   • Parts is empty (count zero).
    ///
    ///   Lookup — null / whitespace guards:
    ///   • Null partId returns null.
    ///   • Whitespace partId returns null.
    ///   • Empty string partId returns null.
    ///
    ///   Lookup — empty catalog:
    ///   • Returns null when catalog has no entries.
    ///
    ///   Lookup — matching:
    ///   • Returns the matching WeaponPartSO when PartId matches.
    ///   • Returns null when PartId does not match any entry.
    ///   • Null entry in list is skipped; subsequent entries still checked.
    ///   • Entry with null PartDefinition does not throw.
    ///   • Multiple entries: returns the first match, not subsequent ones.
    ///
    ///   Parts property:
    ///   • Exposes all registered entries via IReadOnlyList.
    /// </summary>
    public class WeaponPartCatalogSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>
        /// Adds an item to the private List&lt;T&gt; field on <paramref name="target"/>.
        /// </summary>
        private static void AddToListField<T>(object target, string fieldName, T item)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            var list = (List<T>)fi.GetValue(target);
            list.Add(item);
        }

        private static WeaponPartSO CreateWeaponPart(string partId = null)
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            if (partId != null)
            {
                var def = ScriptableObject.CreateInstance<PartDefinition>();
                SetField(def, "_partId", partId);
                SetField(so, "_partDefinition", def);
            }
            return so;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Parts_IsEmpty()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            Assert.AreEqual(0, catalog.Parts.Count,
                "Parts should be empty on a fresh instance.");
            Object.DestroyImmediate(catalog);
        }

        // ── Lookup — null / whitespace guards ─────────────────────────────────

        [Test]
        public void Lookup_NullPartId_ReturnsNull()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            Assert.IsNull(catalog.Lookup(null),
                "Lookup(null) should return null.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Lookup_WhitespacePartId_ReturnsNull()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            Assert.IsNull(catalog.Lookup("   "),
                "Lookup with whitespace-only partId should return null.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Lookup_EmptyStringPartId_ReturnsNull()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            Assert.IsNull(catalog.Lookup(string.Empty),
                "Lookup with empty string partId should return null.");
            Object.DestroyImmediate(catalog);
        }

        // ── Lookup — empty catalog ────────────────────────────────────────────

        [Test]
        public void Lookup_EmptyCatalog_ReturnsNull()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            Assert.IsNull(catalog.Lookup("wp_test"),
                "Lookup on an empty catalog should return null.");
            Object.DestroyImmediate(catalog);
        }

        // ── Lookup — matching ─────────────────────────────────────────────────

        [Test]
        public void Lookup_MatchingPartId_ReturnsPart()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            var part    = CreateWeaponPart("wp_laser");
            AddToListField(catalog, "_parts", part);

            WeaponPartSO result = catalog.Lookup("wp_laser");
            Assert.AreSame(part, result,
                "Lookup should return the matching WeaponPartSO.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(part.PartDefinition);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Lookup_NonMatchingPartId_ReturnsNull()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            var part    = CreateWeaponPart("wp_laser");
            AddToListField(catalog, "_parts", part);

            Assert.IsNull(catalog.Lookup("wp_plasma"),
                "Lookup with a non-matching PartId should return null.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(part.PartDefinition);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Lookup_NullEntryInList_SkipsAndContinues()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            var part    = CreateWeaponPart("wp_shock");
            AddToListField<WeaponPartSO>(catalog, "_parts", null);  // null entry first
            AddToListField(catalog, "_parts", part);

            WeaponPartSO result = catalog.Lookup("wp_shock");
            Assert.AreSame(part, result,
                "Null entries should be skipped; subsequent matching entry should be found.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(part.PartDefinition);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Lookup_PartWithNullPartDefinition_DoesNotThrow()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            var part    = ScriptableObject.CreateInstance<WeaponPartSO>(); // no PartDefinition
            AddToListField(catalog, "_parts", part);

            Assert.DoesNotThrow(() => catalog.Lookup("wp_any"),
                "Entry with null PartDefinition should be skipped without throwing.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Lookup_MultipleEntries_ReturnsFirstMatch()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            var partA   = CreateWeaponPart("wp_energy");
            var partB   = CreateWeaponPart("wp_energy"); // duplicate ID
            AddToListField(catalog, "_parts", partA);
            AddToListField(catalog, "_parts", partB);

            WeaponPartSO result = catalog.Lookup("wp_energy");
            Assert.AreSame(partA, result,
                "Lookup should return the first matching entry, not subsequent ones.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(partA.PartDefinition);
            Object.DestroyImmediate(partA);
            Object.DestroyImmediate(partB.PartDefinition);
            Object.DestroyImmediate(partB);
        }

        // ── Parts property ────────────────────────────────────────────────────

        [Test]
        public void Parts_ExposesAllEntries()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            var partA   = CreateWeaponPart("wp_a");
            var partB   = CreateWeaponPart("wp_b");
            AddToListField(catalog, "_parts", partA);
            AddToListField(catalog, "_parts", partB);

            IReadOnlyList<WeaponPartSO> parts = catalog.Parts;
            Assert.AreEqual(2, parts.Count, "Parts should expose all registered entries.");
            Assert.AreSame(partA, parts[0], "Parts[0] should be partA.");
            Assert.AreSame(partB, parts[1], "Parts[1] should be partB.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(partA.PartDefinition);
            Object.DestroyImmediate(partA);
            Object.DestroyImmediate(partB.PartDefinition);
            Object.DestroyImmediate(partB);
        }

        [Test]
        public void Lookup_MixedValidAndNullEntries_FindsCorrectPart()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            var partA   = ScriptableObject.CreateInstance<WeaponPartSO>(); // null PartDef
            var partB   = CreateWeaponPart("wp_thermal");
            AddToListField(catalog, "_parts", partA);
            AddToListField<WeaponPartSO>(catalog, "_parts", null);
            AddToListField(catalog, "_parts", partB);

            WeaponPartSO result = catalog.Lookup("wp_thermal");
            Assert.AreSame(partB, result,
                "Should skip null entries and entries without PartDefinition to find the match.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(partA);
            Object.DestroyImmediate(partB.PartDefinition);
            Object.DestroyImmediate(partB);
        }
    }
}
