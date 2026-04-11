using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchBonusCatalogSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance: Conditions list is not-null, empty, and IReadOnlyList.
    ///   • Count correctness with 1, 2, and null entries.
    ///   • Insertion-order preservation.
    /// </summary>
    public class MatchBonusCatalogSOTests
    {
        private MatchBonusCatalogSO _catalog;

        // ── Reflection helper ─────────────────────────────────────────────────

        private List<BonusConditionSO> GetList() =>
            (List<BonusConditionSO>)typeof(MatchBonusCatalogSO)
                .GetField("_conditions", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_catalog);

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<MatchBonusCatalogSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            _catalog = null;
        }

        // ── Fresh-instance contract ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Conditions_NotNull()
        {
            Assert.IsNotNull(_catalog.Conditions);
        }

        [Test]
        public void FreshInstance_Conditions_IsEmpty()
        {
            Assert.AreEqual(0, _catalog.Conditions.Count);
        }

        [Test]
        public void FreshInstance_Conditions_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<BonusConditionSO>>(_catalog.Conditions);
        }

        // ── Count correctness ─────────────────────────────────────────────────

        [Test]
        public void WithOneEntry_Conditions_CountIsOne()
        {
            var c = ScriptableObject.CreateInstance<BonusConditionSO>();
            GetList().Add(c);

            Assert.AreEqual(1, _catalog.Conditions.Count);

            Object.DestroyImmediate(c);
        }

        [Test]
        public void WithTwoEntries_Conditions_CountIsTwo()
        {
            var c1 = ScriptableObject.CreateInstance<BonusConditionSO>();
            var c2 = ScriptableObject.CreateInstance<BonusConditionSO>();
            GetList().Add(c1);
            GetList().Add(c2);

            Assert.AreEqual(2, _catalog.Conditions.Count);

            Object.DestroyImmediate(c1);
            Object.DestroyImmediate(c2);
        }

        [Test]
        public void WithNullEntry_Conditions_CountIncludesNull()
        {
            // Null entries are allowed in the list; MatchEndBonusEvaluator skips them.
            GetList().Add(null);

            Assert.AreEqual(1, _catalog.Conditions.Count);
        }

        // ── Insertion-order preservation ──────────────────────────────────────

        [Test]
        public void WithTwoEntries_Conditions_PreservesInsertionOrder()
        {
            var c1 = ScriptableObject.CreateInstance<BonusConditionSO>();
            var c2 = ScriptableObject.CreateInstance<BonusConditionSO>();

            // Give each a distinct BonusAmount so we can tell them apart.
            typeof(BonusConditionSO)
                .GetField("_bonusAmount", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(c1, 10);
            typeof(BonusConditionSO)
                .GetField("_bonusAmount", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(c2, 20);

            GetList().Add(c1);
            GetList().Add(c2);

            Assert.AreEqual(10, _catalog.Conditions[0].BonusAmount);
            Assert.AreEqual(20, _catalog.Conditions[1].BonusAmount);

            Object.DestroyImmediate(c1);
            Object.DestroyImmediate(c2);
        }
    }
}
