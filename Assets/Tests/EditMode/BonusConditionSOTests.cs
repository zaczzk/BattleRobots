using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="BonusConditionSO"/> and the
    /// <see cref="BonusConditionType"/> enum.
    ///
    /// Covers:
    ///   • Fresh-instance default property values.
    ///   • Reflection-injected field → correct property return.
    ///   • BonusConditionType enum has exactly the expected 4 values.
    /// </summary>
    public class BonusConditionSOTests
    {
        private BonusConditionSO _condition;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static FieldInfo Field(string name) =>
            typeof(BonusConditionSO).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);

        private void Set(string fieldName, object value) =>
            Field(fieldName).SetValue(_condition, value);

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _condition = ScriptableObject.CreateInstance<BonusConditionSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_condition);
            _condition = null;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_ConditionType_IsNoDamageTaken()
        {
            Assert.AreEqual(BonusConditionType.NoDamageTaken, _condition.ConditionType);
        }

        [Test]
        public void FreshInstance_Threshold_IsZero()
        {
            Assert.AreEqual(0f, _condition.Threshold, 0.001f);
        }

        [Test]
        public void FreshInstance_BonusAmount_IsFifty()
        {
            Assert.AreEqual(50, _condition.BonusAmount);
        }

        [Test]
        public void FreshInstance_DisplayName_IsEmpty()
        {
            Assert.AreEqual("", _condition.DisplayName);
        }

        [Test]
        public void FreshInstance_DisplayDescription_IsEmpty()
        {
            Assert.AreEqual("", _condition.DisplayDescription);
        }

        // ── Reflection-injected field → correct property ──────────────────────

        [Test]
        public void ConditionType_Set_ReturnsCorrectValue()
        {
            Set("_conditionType", BonusConditionType.WonUnderDuration);
            Assert.AreEqual(BonusConditionType.WonUnderDuration, _condition.ConditionType);
        }

        [Test]
        public void Threshold_Set_ReturnsCorrectValue()
        {
            Set("_threshold", 30f);
            Assert.AreEqual(30f, _condition.Threshold, 0.001f);
        }

        [Test]
        public void BonusAmount_Set_ReturnsCorrectValue()
        {
            Set("_bonusAmount", 100);
            Assert.AreEqual(100, _condition.BonusAmount);
        }

        [Test]
        public void DisplayName_Set_ReturnsCorrectValue()
        {
            Set("_displayName", "Perfect Shield");
            Assert.AreEqual("Perfect Shield", _condition.DisplayName);
        }

        [Test]
        public void DisplayDescription_Set_ReturnsCorrectValue()
        {
            Set("_displayDescription", "Win without taking damage.");
            Assert.AreEqual("Win without taking damage.", _condition.DisplayDescription);
        }

        // ── All four ConditionType values covered ─────────────────────────────

        [Test]
        public void AllConditionTypes_CanBeAssigned()
        {
            // Verify each enum value can be set and retrieved without error.
            foreach (BonusConditionType type in Enum.GetValues(typeof(BonusConditionType)))
            {
                Set("_conditionType", type);
                Assert.AreEqual(type, _condition.ConditionType,
                    $"ConditionType round-trip failed for {type}.");
            }
        }

        [Test]
        public void BonusConditionType_HasExactlyFourValues()
        {
            // Guards against silent enum additions that break the evaluator switch.
            int count = Enum.GetValues(typeof(BonusConditionType)).Length;
            Assert.AreEqual(4, count,
                "BonusConditionType should have exactly 4 values. " +
                "Add a case to MatchEndBonusEvaluator.EvaluateCondition() for each new value.");
        }
    }
}
