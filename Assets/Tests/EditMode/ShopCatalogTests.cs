using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ShopCatalog"/> and <see cref="PartDefinition"/>.
    ///
    /// ShopCatalog is a thin SO wrapper around a serialised List; its serialised
    /// <c>_parts</c> field is private and cannot be populated via CreateInstance
    /// without a full Editor asset round-trip.  Tests therefore focus on:
    ///   • Contract of a fresh (empty) instance.
    ///   • PartDefinition default-value contract (used by shop row setup).
    ///
    /// Compile-time validation (duplicate partId warnings) lives in OnValidate and
    /// is an Editor-only path; it is tested separately in the Editor wiring pass.
    /// </summary>
    public class ShopCatalogTests
    {
        // ── ShopCatalog ────────────────────────────────────────────────────────

        private ShopCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ShopCatalog>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            _catalog = null;
        }

        [Test]
        public void FreshInstance_Parts_IsNotNull()
        {
            Assert.IsNotNull(_catalog.Parts);
        }

        [Test]
        public void FreshInstance_Parts_IsEmpty()
        {
            Assert.AreEqual(0, _catalog.Parts.Count);
        }

        [Test]
        public void Parts_ReturnsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<PartDefinition>>(_catalog.Parts);
        }

        // ── PartDefinition ─────────────────────────────────────────────────────

        private PartDefinition _part;

        [SetUp]
        public void SetUpPart()
        {
            _part = ScriptableObject.CreateInstance<PartDefinition>();
        }

        [TearDown]
        public void TearDownPart()
        {
            Object.DestroyImmediate(_part);
            _part = null;
        }

        [Test]
        public void PartDefinition_FreshInstance_PartIdIsNotNull()
        {
            // Default inspector value "part_unnamed" — must never be null.
            Assert.IsNotNull(_part.PartId);
        }

        [Test]
        public void PartDefinition_FreshInstance_DisplayNameIsNotNull()
        {
            Assert.IsNotNull(_part.DisplayName);
        }

        [Test]
        public void PartDefinition_FreshInstance_CostIsNonNegative()
        {
            Assert.GreaterOrEqual(_part.Cost, 0);
        }

        [Test]
        public void PartDefinition_FreshInstance_DescriptionIsNotNull()
        {
            Assert.IsNotNull(_part.Description);
        }

        [Test]
        public void PartDefinition_FreshInstance_ThumbnailIsNull()
        {
            // No sprite assigned by default; UI must handle null thumbnail gracefully.
            Assert.IsNull(_part.Thumbnail);
        }

        [Test]
        public void PartDefinition_FreshInstance_PrefabIsNull()
        {
            // Prefab is optional; assembler must handle null gracefully.
            Assert.IsNull(_part.Prefab);
        }
    }
}
