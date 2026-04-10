using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotAssembler.AssembleFromCatalog"/>.
    ///
    /// Covers:
    ///   • Null-safety: null partIds / null catalog both fall back to Assemble().
    ///   • ID resolution: valid IDs are matched to PartDefinitions and placed.
    ///   • Unknown IDs are silently warned and skipped.
    ///   • Whitespace / empty IDs are skipped.
    ///   • Multiple known IDs: each filled into a matching slot.
    ///   • Only one part per slot: duplicate IDs for the same category are limited
    ///     by available slots in RobotDefinition.
    ///   • Calling AssembleFromCatalog twice disassembles then reassembles cleanly.
    ///   • Null-partIds fallback preserves the inspector-assigned _equippedParts list.
    ///
    /// Test infrastructure:
    ///   • RobotAssembler is a MonoBehaviour — added via GameObject.AddComponent.
    ///   • Private serialised fields are injected via reflection.
    ///   • PartDefinition.Prefab is left null so no Instantiate() call is made;
    ///     stat-only parts still reach _equippedPartIds after slot matching.
    ///   • Each test creates its own _slotGo for the SlotMount attachPoint Transform.
    /// </summary>
    public class RobotAssemblerLoadoutTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────
        private GameObject     _go;
        private RobotAssembler _assembler;

        // ── SO assets ─────────────────────────────────────────────────────────
        private RobotDefinition _robotDef;
        private PartDefinition  _weaponPart;
        private ShopCatalog     _catalog;

        // ── Scene helper ──────────────────────────────────────────────────────
        private GameObject _slotGo;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Reflection: field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void SetFieldOnType(Type type, object target, string fieldName, object value)
        {
            FieldInfo fi = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Reflection: field '{fieldName}' not found on {type.Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // ── Robot assembler ──────────────────────────────────────────────
            _go        = new GameObject("TestAssembler");
            _assembler = _go.AddComponent<RobotAssembler>();

            // ── RobotDefinition with one Weapon slot ─────────────────────────
            _robotDef = ScriptableObject.CreateInstance<RobotDefinition>();
            var slot  = new PartSlot { slotId = "slot_weapon", category = PartCategory.Weapon };
            SetFieldOnType(typeof(RobotDefinition), _robotDef, "_slots",
                           new List<PartSlot> { slot });

            // ── PartDefinition: stat-only Weapon part (no prefab) ─────────────
            _weaponPart = ScriptableObject.CreateInstance<PartDefinition>();
            SetFieldOnType(typeof(PartDefinition), _weaponPart, "_partId",    "weapon_01");
            SetFieldOnType(typeof(PartDefinition), _weaponPart, "_category",  PartCategory.Weapon);

            // ── ShopCatalog ───────────────────────────────────────────────────
            _catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetFieldOnType(typeof(ShopCatalog), _catalog, "_parts",
                           new List<PartDefinition> { _weaponPart });

            // ── SlotMount: maps "slot_weapon" to a Transform ──────────────────
            _slotGo = new GameObject("SlotAttach");
            var mount = new SlotMount { slotId = "slot_weapon", attachPoint = _slotGo.transform };

            // ── Inject into assembler ─────────────────────────────────────────
            SetField(_assembler, "_robotDefinition", _robotDef);
            SetField(_assembler, "_slotMounts",      new List<SlotMount> { mount });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_slotGo);
            Object.DestroyImmediate(_robotDef);
            Object.DestroyImmediate(_weaponPart);
            Object.DestroyImmediate(_catalog);
            _go         = null;
            _slotGo     = null;
            _assembler  = null;
            _robotDef   = null;
            _weaponPart = null;
            _catalog    = null;
        }

        // ── Null-safety fallback ──────────────────────────────────────────────

        [Test]
        public void AssembleFromCatalog_NullPartIds_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _assembler.AssembleFromCatalog(null, _catalog));
        }

        [Test]
        public void AssembleFromCatalog_NullCatalog_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _assembler.AssembleFromCatalog(new[] { "weapon_01" }, null));
        }

        [Test]
        public void AssembleFromCatalog_NullPartIds_FallsBackToAssemble_SetsIsAssembledTrue()
        {
            // Fallback to Assemble(); _equippedParts is empty but _robotDefinition
            // is assigned so Assemble() completes — IsAssembled should be true.
            _assembler.AssembleFromCatalog(null, _catalog);
            Assert.IsTrue(_assembler.IsAssembled);
        }

        [Test]
        public void AssembleFromCatalog_NullCatalog_FallsBackToAssemble_SetsIsAssembledTrue()
        {
            _assembler.AssembleFromCatalog(new[] { "weapon_01" }, null);
            Assert.IsTrue(_assembler.IsAssembled);
        }

        /// <summary>
        /// When null partIds triggers the fallback Assemble(), any parts that were
        /// already assigned to _equippedParts (inspector list) are still assembled.
        /// </summary>
        [Test]
        public void AssembleFromCatalog_NullPartIds_UsesExistingEquippedPartsList()
        {
            // Pre-populate the inspector list via reflection.
            SetField(_assembler, "_equippedParts", new List<PartDefinition> { _weaponPart });

            _assembler.AssembleFromCatalog(null, _catalog);

            // The weapon part should be assembled via the inspector list fallback.
            Assert.AreEqual(1, _assembler.GetEquippedPartIds().Count);
            Assert.AreEqual("weapon_01", _assembler.GetEquippedPartIds()[0]);
        }

        // ── ID resolution ─────────────────────────────────────────────────────

        [Test]
        public void AssembleFromCatalog_WithValidId_ReturnsPartInEquippedIds()
        {
            _assembler.AssembleFromCatalog(new[] { "weapon_01" }, _catalog);

            Assert.AreEqual(1, _assembler.GetEquippedPartIds().Count);
            Assert.AreEqual("weapon_01", _assembler.GetEquippedPartIds()[0]);
        }

        [Test]
        public void AssembleFromCatalog_WithValidId_SetsIsAssembledTrue()
        {
            _assembler.AssembleFromCatalog(new[] { "weapon_01" }, _catalog);
            Assert.IsTrue(_assembler.IsAssembled);
        }

        [Test]
        public void AssembleFromCatalog_WithValidId_GetEquippedPartsContainsDef()
        {
            _assembler.AssembleFromCatalog(new[] { "weapon_01" }, _catalog);

            Assert.AreEqual(1,           _assembler.GetEquippedParts().Count);
            Assert.AreEqual(_weaponPart, _assembler.GetEquippedParts()[0]);
        }

        // ── Unknown / invalid IDs ─────────────────────────────────────────────

        [Test]
        public void AssembleFromCatalog_WithUnknownId_NoPartsEquipped()
        {
            _assembler.AssembleFromCatalog(new[] { "no_such_part" }, _catalog);
            Assert.AreEqual(0, _assembler.GetEquippedPartIds().Count);
        }

        [Test]
        public void AssembleFromCatalog_WithUnknownId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _assembler.AssembleFromCatalog(new[] { "does_not_exist" }, _catalog));
        }

        [Test]
        public void AssembleFromCatalog_WithMixedKnownAndUnknownIds_OnlyEquipsKnown()
        {
            _assembler.AssembleFromCatalog(new[] { "no_such_part", "weapon_01" }, _catalog);

            // Only the known ID should be placed.
            Assert.AreEqual(1, _assembler.GetEquippedPartIds().Count);
            Assert.AreEqual("weapon_01", _assembler.GetEquippedPartIds()[0]);
        }

        // ── Whitespace / empty IDs ────────────────────────────────────────────

        [Test]
        public void AssembleFromCatalog_WithEmptyIds_NoPartsEquipped()
        {
            _assembler.AssembleFromCatalog(new string[0], _catalog);
            Assert.AreEqual(0, _assembler.GetEquippedPartIds().Count);
        }

        [Test]
        public void AssembleFromCatalog_WithWhitespaceEntry_SkipsWhitespace()
        {
            _assembler.AssembleFromCatalog(new[] { "  ", "\t", "weapon_01" }, _catalog);
            Assert.AreEqual(1, _assembler.GetEquippedPartIds().Count);
            Assert.AreEqual("weapon_01", _assembler.GetEquippedPartIds()[0]);
        }

        // ── Slot-count limitation ─────────────────────────────────────────────

        [Test]
        public void AssembleFromCatalog_DuplicateValidIds_LimitedBySlotCount()
        {
            // RobotDefinition has only ONE weapon slot.
            // Passing the same ID twice should only equip once.
            _assembler.AssembleFromCatalog(new[] { "weapon_01", "weapon_01" }, _catalog);
            Assert.AreEqual(1, _assembler.GetEquippedPartIds().Count);
        }

        // ── Re-assembly ───────────────────────────────────────────────────────

        [Test]
        public void AssembleFromCatalog_CalledTwice_SecondCallReassemblesClean()
        {
            _assembler.AssembleFromCatalog(new[] { "weapon_01" }, _catalog);
            Assert.AreEqual(1, _assembler.GetEquippedPartIds().Count);

            // Second call should disassemble first, then reassemble.
            _assembler.AssembleFromCatalog(new[] { "weapon_01" }, _catalog);
            Assert.AreEqual(1, _assembler.GetEquippedPartIds().Count);
        }

        [Test]
        public void AssembleFromCatalog_CalledTwice_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _assembler.AssembleFromCatalog(new[] { "weapon_01" }, _catalog);
                _assembler.AssembleFromCatalog(new[] { "weapon_01" }, _catalog);
            });
        }

        [Test]
        public void AssembleFromCatalog_AfterDirectAssemble_ReplacesEquippedParts()
        {
            // Start with inspector list populated.
            SetField(_assembler, "_equippedParts", new List<PartDefinition> { _weaponPart });
            _assembler.Assemble();
            Assert.AreEqual(1, _assembler.GetEquippedPartIds().Count);

            // Now call AssembleFromCatalog with an empty list — parts should be gone.
            _assembler.AssembleFromCatalog(new string[0], _catalog);
            Assert.AreEqual(0, _assembler.GetEquippedPartIds().Count);
        }
    }
}
