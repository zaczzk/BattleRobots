using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchModifierCatalogSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance list is non-null, empty, and exposes
    ///     <see cref="System.Collections.Generic.IReadOnlyList{T}"/>.
    ///   • Count is correct after 1 and 2 entries.
    ///   • Insertion order is preserved.
    ///
    /// All tests run headless; the private <c>_modifiers</c> list is
    /// injected via reflection.
    /// </summary>
    public class MatchModifierCatalogSOTests
    {
        private MatchModifierCatalogSO _catalog;

        // ── Setup / Teardown ───────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<MatchModifierCatalogSO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_catalog);
            _catalog = null;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static List<MatchModifierSO> SetModifiers(
            MatchModifierCatalogSO catalog, int count)
        {
            var list = new List<MatchModifierSO>(count);
            for (int i = 0; i < count; i++)
            {
                var mod = ScriptableObject.CreateInstance<MatchModifierSO>();
                FieldInfo fi = typeof(MatchModifierSO)
                    .GetField("_displayName", BindingFlags.NonPublic | BindingFlags.Instance);
                fi?.SetValue(mod, $"Mod{i}");
                list.Add(mod);
            }

            FieldInfo field = typeof(MatchModifierCatalogSO)
                .GetField("_modifiers", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Reflection: _modifiers not found on MatchModifierCatalogSO.");
            field.SetValue(catalog, list);
            return list;
        }

        // ── Fresh-instance contracts ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Modifiers_NotNull()
        {
            Assert.IsNotNull(_catalog.Modifiers,
                "Modifiers must not be null on a fresh instance.");
        }

        [Test]
        public void FreshInstance_Modifiers_IsEmpty()
        {
            Assert.AreEqual(0, _catalog.Modifiers.Count,
                "Modifiers must be empty on a fresh instance.");
        }

        [Test]
        public void Modifiers_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<MatchModifierSO>>(_catalog.Modifiers,
                "Modifiers must implement IReadOnlyList<MatchModifierSO>.");
        }

        // ── Count after injection ──────────────────────────────────────────────

        [Test]
        public void WithOneModifier_Count_IsOne()
        {
            var mods = SetModifiers(_catalog, 1);

            Assert.AreEqual(1, _catalog.Modifiers.Count,
                "Modifiers.Count must be 1 after injecting one entry.");

            foreach (var m in mods) UnityEngine.Object.DestroyImmediate(m);
        }

        [Test]
        public void WithTwoModifiers_Count_IsTwo()
        {
            var mods = SetModifiers(_catalog, 2);

            Assert.AreEqual(2, _catalog.Modifiers.Count,
                "Modifiers.Count must be 2 after injecting two entries.");

            foreach (var m in mods) UnityEngine.Object.DestroyImmediate(m);
        }

        // ── Insertion order ────────────────────────────────────────────────────

        [Test]
        public void Modifiers_PreservesInsertionOrder()
        {
            var mods = SetModifiers(_catalog, 3);

            for (int i = 0; i < mods.Count; i++)
                Assert.AreSame(mods[i], _catalog.Modifiers[i],
                    $"Modifiers[{i}] must equal the {i}-th injected modifier SO (insertion order preserved).");

            foreach (var m in mods) UnityEngine.Object.DestroyImmediate(m);
        }
    }
}
