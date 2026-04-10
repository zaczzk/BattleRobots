using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LoadoutSlotController"/>.
    ///
    /// Covers:
    ///   • Setup with null / empty / populated candidate lists.
    ///   • Pre-selection by currentPartId: match, no-match, null.
    ///   • <see cref="LoadoutSlotController.NextPart"/> / <see cref="LoadoutSlotController.PreviousPart"/>
    ///     cycling and wrap-around behaviour.
    ///   • <see cref="LoadoutSlotController.GetSelectedPartDef"/> returns the correct part
    ///     or null for the None sentinel.
    ///   • <see cref="LoadoutSlotController.RebuildCandidates"/> preserves the selection
    ///     when the part is still available; falls back to None when it has been removed.
    ///   • <see cref="LoadoutSlotController.Category"/> returns the value passed to Setup.
    ///
    /// All tests use a headless <see cref="GameObject"/>; no UI refs (Text / Button /
    /// Image) are assigned, so only selection logic is exercised — zero dependency on
    /// uGUI rendering.
    /// </summary>
    public class LoadoutSlotControllerTests
    {
        // ── Fields ─────────────────────────────────────────────────────────────

        private GameObject            _go;
        private LoadoutSlotController _ctrl;

        private PartDefinition _partA; // Weapon, id = "weapon_a"
        private PartDefinition _partB; // Weapon, id = "weapon_b"

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi,
                $"Reflection: field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static PartDefinition MakePart(string partId, PartCategory category)
        {
            var def = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(def, "_partId",   partId);
            SetField(def, "_category", category);
            return def;
        }

        // ── Setup / Teardown ───────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("TestSlot");
            _ctrl = _go.AddComponent<LoadoutSlotController>();

            _partA = MakePart("weapon_a", PartCategory.Weapon);
            _partB = MakePart("weapon_b", PartCategory.Weapon);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_partA);
            Object.DestroyImmediate(_partB);
            _go    = null;
            _ctrl  = null;
            _partA = null;
            _partB = null;
        }

        // ── Setup — null / empty candidates ────────────────────────────────────

        [Test]
        public void Setup_NullOwnedParts_GetSelectedPartDef_ReturnsNull()
        {
            _ctrl.Setup(PartCategory.Weapon, null, null);
            Assert.IsNull(_ctrl.GetSelectedPartDef());
        }

        [Test]
        public void Setup_EmptyOwnedParts_GetSelectedPartDef_ReturnsNull()
        {
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition>(), null);
            Assert.IsNull(_ctrl.GetSelectedPartDef());
        }

        // ── Setup — pre-selection ──────────────────────────────────────────────

        [Test]
        public void Setup_WithOnePart_NullCurrentId_DefaultsToNone()
        {
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA }, null);
            Assert.IsNull(_ctrl.GetSelectedPartDef(),
                "currentPartId null should keep the None sentinel selected.");
        }

        [Test]
        public void Setup_WithOnePart_MatchingCurrentId_PreSelectsPart()
        {
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA }, "weapon_a");
            Assert.AreEqual(_partA, _ctrl.GetSelectedPartDef());
        }

        [Test]
        public void Setup_WithTwoParts_MatchingSecondId_PreSelectsCorrectPart()
        {
            _ctrl.Setup(PartCategory.Weapon,
                        new List<PartDefinition> { _partA, _partB }, "weapon_b");
            Assert.AreEqual(_partB, _ctrl.GetSelectedPartDef());
        }

        [Test]
        public void Setup_WithOnePart_NonMatchingCurrentId_DefaultsToNone()
        {
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA }, "weapon_x");
            Assert.IsNull(_ctrl.GetSelectedPartDef(),
                "Unknown currentPartId should fall back to None sentinel.");
        }

        // ── Category property ──────────────────────────────────────────────────

        [Test]
        public void Category_ReturnsCategoryPassedToSetup()
        {
            _ctrl.Setup(PartCategory.Armor, new List<PartDefinition>(), null);
            Assert.AreEqual(PartCategory.Armor, _ctrl.Category);
        }

        // ── NextPart ───────────────────────────────────────────────────────────

        [Test]
        public void NextPart_WhenOnlyNoneExists_DoesNotChangeSelection()
        {
            _ctrl.Setup(PartCategory.Weapon, null, null);
            _ctrl.NextPart();
            Assert.IsNull(_ctrl.GetSelectedPartDef(),
                "With no candidates NextPart should stay on None.");
        }

        [Test]
        public void NextPart_FromNone_AdvancesToFirstPart()
        {
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA }, null);
            _ctrl.NextPart();
            Assert.AreEqual(_partA, _ctrl.GetSelectedPartDef());
        }

        [Test]
        public void NextPart_WrapsAroundFromLastPartToNone()
        {
            // Start pre-selected on the only part.
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA }, "weapon_a");
            _ctrl.NextPart(); // past last → wraps to None (index 0)
            Assert.IsNull(_ctrl.GetSelectedPartDef(),
                "NextPart past the last part should wrap back to None sentinel.");
        }

        [Test]
        public void NextPart_TwoCycles_ReturnsToStartingPart()
        {
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA }, "weapon_a");
            _ctrl.NextPart(); // → None
            _ctrl.NextPart(); // → _partA again
            Assert.AreEqual(_partA, _ctrl.GetSelectedPartDef());
        }

        // ── PreviousPart ───────────────────────────────────────────────────────

        [Test]
        public void PreviousPart_WhenOnlyNoneExists_DoesNotChangeSelection()
        {
            _ctrl.Setup(PartCategory.Weapon, null, null);
            _ctrl.PreviousPart();
            Assert.IsNull(_ctrl.GetSelectedPartDef());
        }

        [Test]
        public void PreviousPart_FromNone_WrapsToLastPart()
        {
            // Candidates = [null(None), _partA, _partB]. None = index 0, last = _partB.
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA, _partB }, null);
            _ctrl.PreviousPart(); // 0 → wraps to last (index 2 = _partB)
            Assert.AreEqual(_partB, _ctrl.GetSelectedPartDef());
        }

        // ── RebuildCandidates ──────────────────────────────────────────────────

        [Test]
        public void RebuildCandidates_PreservesMatchingSelection()
        {
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA }, "weapon_a");
            Assert.AreEqual(_partA, _ctrl.GetSelectedPartDef(), "Pre-condition: _partA selected.");

            _ctrl.RebuildCandidates(new List<PartDefinition> { _partA });
            Assert.AreEqual(_partA, _ctrl.GetSelectedPartDef(),
                "RebuildCandidates should preserve the selection when the part is still available.");
        }

        [Test]
        public void RebuildCandidates_LostPart_FallsBackToNone()
        {
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA }, "weapon_a");
            Assert.AreEqual(_partA, _ctrl.GetSelectedPartDef(), "Pre-condition: _partA selected.");

            // Rebuild without _partA — it should fall back to None.
            _ctrl.RebuildCandidates(new List<PartDefinition> { _partB });
            Assert.IsNull(_ctrl.GetSelectedPartDef(),
                "Selection should fall back to None when the previously selected part is removed.");
        }

        [Test]
        public void RebuildCandidates_NullList_ResetsSelectionToNone()
        {
            _ctrl.Setup(PartCategory.Weapon, new List<PartDefinition> { _partA }, "weapon_a");
            _ctrl.RebuildCandidates(null);
            Assert.IsNull(_ctrl.GetSelectedPartDef());
        }
    }
}
