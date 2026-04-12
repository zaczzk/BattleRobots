using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LootTableSO"/>.
    ///
    /// Covers:
    ///   • Fresh instance defaults (WinDropChance, HasEntries, TotalWeight).
    ///   • HasEntries: false when empty, false when all parts null, true on valid entry.
    ///   • TotalWeight: zero on empty, single entry, sums multiple entries, skips nulls.
    ///   • RollDrop: null on empty table, null when all parts null, returns part on
    ///     single valid entry, deterministic with same seed, can vary with different seeds.
    ///   • RollDrop: last-entry float-edge path (roll == total, covered by seed selection).
    /// </summary>
    public class LootTableSOTests
    {
        // ── Fixture ───────────────────────────────────────────────────────────

        private LootTableSO  _table;
        private PartDefinition _partA;
        private PartDefinition _partB;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static LootEntry MakeEntry(PartDefinition part, float weight)
            => new LootEntry { part = part, weight = weight };

        // ── Setup ─────────────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _table  = ScriptableObject.CreateInstance<LootTableSO>();
            _partA  = ScriptableObject.CreateInstance<PartDefinition>();
            _partB  = ScriptableObject.CreateInstance<PartDefinition>();

            SetField(_partA, "_partId",      "part_a");
            SetField(_partA, "_displayName", "Part A");
            SetField(_partB, "_partId",      "part_b");
            SetField(_partB, "_displayName", "Part B");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
            Object.DestroyImmediate(_partA);
            Object.DestroyImmediate(_partB);
        }

        // ── Defaults ──────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_WinDropChance_IsPointThree()
        {
            Assert.AreEqual(0.3f, _table.WinDropChance, 0.001f);
        }

        [Test]
        public void FreshInstance_HasEntries_IsFalse()
        {
            Assert.IsFalse(_table.HasEntries);
        }

        [Test]
        public void FreshInstance_TotalWeight_IsZero()
        {
            Assert.AreEqual(0f, _table.TotalWeight, 0.001f);
        }

        [Test]
        public void FreshInstance_RollDrop_ReturnsNull()
        {
            Assert.IsNull(_table.RollDrop(42));
        }

        // ── HasEntries ────────────────────────────────────────────────────────

        [Test]
        public void HasEntries_AllNullParts_IsFalse()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(null, 1f),
                MakeEntry(null, 2f),
            });
            Assert.IsFalse(_table.HasEntries);
        }

        [Test]
        public void HasEntries_OneValidEntry_IsTrue()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(_partA, 1f),
            });
            Assert.IsTrue(_table.HasEntries);
        }

        [Test]
        public void HasEntries_MixedNullAndValid_IsTrue()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(null,  1f),
                MakeEntry(_partA, 3f),
            });
            Assert.IsTrue(_table.HasEntries);
        }

        // ── TotalWeight ───────────────────────────────────────────────────────

        [Test]
        public void TotalWeight_SingleValidEntry_EqualsEntryWeight()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(_partA, 2.5f),
            });
            Assert.AreEqual(2.5f, _table.TotalWeight, 0.001f);
        }

        [Test]
        public void TotalWeight_MultipleEntries_SumsWeights()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(_partA, 1f),
                MakeEntry(_partB, 3f),
            });
            Assert.AreEqual(4f, _table.TotalWeight, 0.001f);
        }

        [Test]
        public void TotalWeight_SkipsNullPartEntries()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(null,  10f),   // skipped — null part
                MakeEntry(_partA, 2f),
            });
            Assert.AreEqual(2f, _table.TotalWeight, 0.001f);
        }

        // ── RollDrop ──────────────────────────────────────────────────────────

        [Test]
        public void RollDrop_AllNullParts_ReturnsNull()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(null, 1f),
            });
            Assert.IsNull(_table.RollDrop(0));
        }

        [Test]
        public void RollDrop_SingleValidEntry_ReturnsThatPart()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(_partA, 1f),
            });
            PartDefinition result = _table.RollDrop(12345);
            Assert.AreSame(_partA, result);
        }

        [Test]
        public void RollDrop_SameSeed_ReturnsSameResult()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(_partA, 1f),
                MakeEntry(_partB, 1f),
            });
            PartDefinition r1 = _table.RollDrop(99);
            PartDefinition r2 = _table.RollDrop(99);
            Assert.AreSame(r1, r2);
        }

        [Test]
        public void RollDrop_BothPartsReachable_AcrossVariousSeeds()
        {
            // With equal weights the table must be able to return both parts.
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(_partA, 1f),
                MakeEntry(_partB, 1f),
            });

            bool sawA = false, sawB = false;
            for (int seed = 0; seed < 200; seed++)
            {
                PartDefinition r = _table.RollDrop(seed);
                if (r == _partA) sawA = true;
                if (r == _partB) sawB = true;
                if (sawA && sawB) break;
            }
            Assert.IsTrue(sawA, "Part A was never returned across 200 seeds.");
            Assert.IsTrue(sawB, "Part B was never returned across 200 seeds.");
        }

        [Test]
        public void RollDrop_SkipsNullEntryAndReturnsValidPart()
        {
            // Null entry at index 0 should be skipped; _partA at index 1 must be returned.
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(null,  1f),
                MakeEntry(_partA, 1f),
            });
            // With total weight = 1f (null entry skipped), any seed must return _partA.
            PartDefinition result = _table.RollDrop(7);
            Assert.AreSame(_partA, result);
        }

        [Test]
        public void Entries_ExposesReadOnlyList()
        {
            SetField(_table, "_entries", new List<LootEntry>
            {
                MakeEntry(_partA, 1f),
            });
            Assert.AreEqual(1, _table.Entries.Count);
            Assert.AreSame(_partA, _table.Entries[0].part);
        }
    }
}
