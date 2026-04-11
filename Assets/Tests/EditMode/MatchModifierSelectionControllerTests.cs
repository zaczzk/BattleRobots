using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchModifierSelectionController"/>.
    ///
    /// Covers:
    ///   • Initial <see cref="MatchModifierSelectionController.SelectedIndex"/> is 0.
    ///   • <see cref="MatchModifierSelectionController.NextModifier"/>:
    ///       - null / empty catalog → no-op, no throw.
    ///       - increments index correctly.
    ///       - wraps forward from the last modifier back to index 0.
    ///       - full cycle returns to start.
    ///   • <see cref="MatchModifierSelectionController.PreviousModifier"/>:
    ///       - null / empty catalog → no-op, no throw.
    ///       - wraps backward from index 0 to the last modifier.
    ///       - decrements from non-zero index.
    ///   • <c>ApplySelection</c> (triggered via Next / Previous):
    ///       - writes the correct <see cref="MatchModifierSO"/> to
    ///         <see cref="SelectedModifierSO.Current"/>.
    ///       - null <see cref="SelectedModifierSO"/> → no throw.
    ///
    /// All tests run headless (no scene, no uGUI).  Private inspector fields are
    /// injected via reflection.
    /// </summary>
    public class MatchModifierSelectionControllerTests
    {
        // ── Scene objects ──────────────────────────────────────────────────────

        private GameObject                         _go;
        private MatchModifierSelectionController   _ctrl;

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi,
                $"Reflection: field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>
        /// Creates a <see cref="MatchModifierCatalogSO"/> whose internal
        /// <c>_modifiers</c> list is populated with <paramref name="count"/>
        /// distinct <see cref="MatchModifierSO"/> instances.
        /// Returns both the catalog SO and the array of created modifier SOs so
        /// the caller can destroy them in TearDown.
        /// </summary>
        private static (MatchModifierCatalogSO catalog, MatchModifierSO[] modifiers)
            MakeCatalog(int count)
        {
            var modifiers = new MatchModifierSO[count];
            var list      = new List<MatchModifierSO>(count);

            FieldInfo nameFi = typeof(MatchModifierSO)
                .GetField("_displayName", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < count; i++)
            {
                modifiers[i] = ScriptableObject.CreateInstance<MatchModifierSO>();
                nameFi?.SetValue(modifiers[i], $"Modifier{i}");
                list.Add(modifiers[i]);
            }

            var catalog = ScriptableObject.CreateInstance<MatchModifierCatalogSO>();
            FieldInfo listFi = typeof(MatchModifierCatalogSO)
                .GetField("_modifiers", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(listFi,
                "Reflection: _modifiers not found on MatchModifierCatalogSO.");
            listFi.SetValue(catalog, list);

            return (catalog, modifiers);
        }

        // ── Setup / Teardown ───────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("TestMatchModifierSelectionController");
            _ctrl = _go.AddComponent<MatchModifierSelectionController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            _go   = null;
            _ctrl = null;
        }

        // ── Initial state ──────────────────────────────────────────────────────

        [Test]
        public void SelectedIndex_InitialValue_IsZero()
        {
            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "SelectedIndex must start at 0 (first modifier selected by default).");
        }

        // ── NextModifier — guard paths ─────────────────────────────────────────

        [Test]
        public void NextModifier_NullCatalog_DoesNotThrow()
        {
            // _catalog remains null (not injected).
            Assert.DoesNotThrow(() => _ctrl.NextModifier(),
                "NextModifier with null _catalog must not throw.");
        }

        [Test]
        public void NextModifier_NullCatalog_IndexRemainsZero()
        {
            _ctrl.NextModifier();
            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "SelectedIndex must stay 0 when _catalog is null.");
        }

        [Test]
        public void NextModifier_EmptyCatalog_DoesNotChangeIndex()
        {
            var (catalog, _) = MakeCatalog(0);
            SetField(_ctrl, "_catalog", catalog);

            _ctrl.NextModifier();

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "NextModifier on an empty catalog must not change SelectedIndex.");
            Object.DestroyImmediate(catalog);
        }

        // ── NextModifier — cycling ─────────────────────────────────────────────

        [Test]
        public void NextModifier_ThreeModifiers_IncrementsIndex()
        {
            var (catalog, mods) = MakeCatalog(3);
            SetField(_ctrl, "_catalog", catalog);

            _ctrl.NextModifier();

            Assert.AreEqual(1, _ctrl.SelectedIndex,
                "NextModifier from index 0 should advance to index 1.");

            Object.DestroyImmediate(catalog);
            foreach (var m in mods) Object.DestroyImmediate(m);
        }

        [Test]
        public void NextModifier_AtLastModifier_WrapsToZero()
        {
            var (catalog, mods) = MakeCatalog(3);
            SetField(_ctrl, "_catalog", catalog);

            _ctrl.NextModifier(); // 0 → 1
            _ctrl.NextModifier(); // 1 → 2
            _ctrl.NextModifier(); // 2 → wraps to 0

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "NextModifier past the last modifier must wrap back to index 0.");

            Object.DestroyImmediate(catalog);
            foreach (var m in mods) Object.DestroyImmediate(m);
        }

        [Test]
        public void NextModifier_FullCycle_ReturnsToStart()
        {
            var (catalog, mods) = MakeCatalog(3);
            SetField(_ctrl, "_catalog", catalog);

            _ctrl.NextModifier();
            _ctrl.NextModifier();
            _ctrl.NextModifier();

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "Three consecutive NextModifier calls on 3 modifiers must return to 0.");

            Object.DestroyImmediate(catalog);
            foreach (var m in mods) Object.DestroyImmediate(m);
        }

        // ── PreviousModifier — guard paths ─────────────────────────────────────

        [Test]
        public void PreviousModifier_NullCatalog_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _ctrl.PreviousModifier(),
                "PreviousModifier with null _catalog must not throw.");
        }

        [Test]
        public void PreviousModifier_NullCatalog_IndexRemainsZero()
        {
            _ctrl.PreviousModifier();
            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "SelectedIndex must stay 0 when _catalog is null.");
        }

        [Test]
        public void PreviousModifier_EmptyCatalog_DoesNotChangeIndex()
        {
            var (catalog, _) = MakeCatalog(0);
            SetField(_ctrl, "_catalog", catalog);

            _ctrl.PreviousModifier();

            Assert.AreEqual(0, _ctrl.SelectedIndex);
            Object.DestroyImmediate(catalog);
        }

        // ── PreviousModifier — cycling ─────────────────────────────────────────

        [Test]
        public void PreviousModifier_AtFirstModifier_WrapsToLast()
        {
            var (catalog, mods) = MakeCatalog(3);
            SetField(_ctrl, "_catalog", catalog);

            _ctrl.PreviousModifier(); // 0 → wraps to 2

            Assert.AreEqual(2, _ctrl.SelectedIndex,
                "PreviousModifier at index 0 must wrap to the last modifier index (2).");

            Object.DestroyImmediate(catalog);
            foreach (var m in mods) Object.DestroyImmediate(m);
        }

        [Test]
        public void PreviousModifier_FromSecond_GoesToFirst()
        {
            var (catalog, mods) = MakeCatalog(3);
            SetField(_ctrl, "_catalog", catalog);

            _ctrl.NextModifier();     // 0 → 1
            _ctrl.PreviousModifier(); // 1 → 0

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "PreviousModifier from index 1 should return to index 0.");

            Object.DestroyImmediate(catalog);
            foreach (var m in mods) Object.DestroyImmediate(m);
        }

        [Test]
        public void NextThenPrevious_ReturnsToStartingIndex()
        {
            var (catalog, mods) = MakeCatalog(3);
            SetField(_ctrl, "_catalog", catalog);

            _ctrl.NextModifier();
            _ctrl.PreviousModifier();

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "NextModifier then PreviousModifier should return to the starting index.");

            Object.DestroyImmediate(catalog);
            foreach (var m in mods) Object.DestroyImmediate(m);
        }

        // ── ApplySelection — SelectedModifierSO writes ─────────────────────────

        [Test]
        public void NextModifier_WritesCorrectModifierToSelectedModifierSO()
        {
            var (catalog, mods) = MakeCatalog(3);
            var selected = ScriptableObject.CreateInstance<SelectedModifierSO>();
            SetField(_ctrl, "_catalog",          catalog);
            SetField(_ctrl, "_selectedModifier", selected);

            _ctrl.NextModifier(); // advances to index 1

            Assert.AreSame(mods[1], selected.Current,
                "After NextModifier to index 1, SelectedModifierSO.Current must equal modifiers[1].");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(selected);
            foreach (var m in mods) Object.DestroyImmediate(m);
        }

        [Test]
        public void PreviousModifier_FromZero_WritesLastModifierToSelectedModifierSO()
        {
            var (catalog, mods) = MakeCatalog(3);
            var selected = ScriptableObject.CreateInstance<SelectedModifierSO>();
            SetField(_ctrl, "_catalog",          catalog);
            SetField(_ctrl, "_selectedModifier", selected);

            _ctrl.PreviousModifier(); // 0 → wraps to 2

            Assert.AreSame(mods[2], selected.Current,
                "PreviousModifier from 0 wraps to last modifier; " +
                "SelectedModifierSO.Current must equal modifiers[2].");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(selected);
            foreach (var m in mods) Object.DestroyImmediate(m);
        }

        [Test]
        public void ApplySelection_NullSelectedModifier_DoesNotThrow()
        {
            var (catalog, mods) = MakeCatalog(3);
            SetField(_ctrl, "_catalog", catalog);
            // _selectedModifier remains null.

            Assert.DoesNotThrow(() => _ctrl.NextModifier(),
                "NextModifier must not throw when _selectedModifier is null.");

            Object.DestroyImmediate(catalog);
            foreach (var m in mods) Object.DestroyImmediate(m);
        }
    }
}
