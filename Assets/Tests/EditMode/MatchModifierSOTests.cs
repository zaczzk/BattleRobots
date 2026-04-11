using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchModifierSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance property defaults (7 tests).
    ///   • <see cref="MatchModifierType"/> enum contract — exactly 6 values.
    ///   • Reflection-set field round-trips for each multiplier property.
    ///
    /// All tests are pure C# with no scene dependency.
    /// </summary>
    public class MatchModifierSOTests
    {
        private MatchModifierSO _so;

        // ── Setup / Teardown ───────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so = ScriptableObject.CreateInstance<MatchModifierSO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_so);
            _so = null;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi,
                $"Reflection: field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Fresh-instance defaults ────────────────────────────────────────────

        [Test]
        public void FreshInstance_ModifierType_IsStandard()
        {
            Assert.AreEqual(MatchModifierType.Standard, _so.ModifierType,
                "ModifierType must default to Standard.");
        }

        [Test]
        public void FreshInstance_DisplayName_IsStandard()
        {
            Assert.AreEqual("Standard", _so.DisplayName,
                "DisplayName must default to \"Standard\".");
        }

        [Test]
        public void FreshInstance_Description_IsEmpty()
        {
            Assert.AreEqual(string.Empty, _so.Description,
                "Description must default to empty string.");
        }

        [Test]
        public void FreshInstance_RewardMultiplier_IsOne()
        {
            Assert.AreEqual(1f, _so.RewardMultiplier,
                "RewardMultiplier must default to 1.0 (no effect).");
        }

        [Test]
        public void FreshInstance_TimeMultiplier_IsOne()
        {
            Assert.AreEqual(1f, _so.TimeMultiplier,
                "TimeMultiplier must default to 1.0 (no effect).");
        }

        [Test]
        public void FreshInstance_ArmorMultiplier_IsOne()
        {
            Assert.AreEqual(1f, _so.ArmorMultiplier,
                "ArmorMultiplier must default to 1.0 (no effect).");
        }

        [Test]
        public void FreshInstance_SpeedMultiplier_IsOne()
        {
            Assert.AreEqual(1f, _so.SpeedMultiplier,
                "SpeedMultiplier must default to 1.0 (no effect).");
        }

        // ── Enum contract ──────────────────────────────────────────────────────

        [Test]
        public void MatchModifierType_HasExactlySixValues()
        {
            var values = Enum.GetValues(typeof(MatchModifierType));
            Assert.AreEqual(6, values.Length,
                "MatchModifierType must define exactly 6 values: " +
                "Standard, DoubleRewards, ExtendedTime, ShortTime, FragileArmor, Overdrive.");
        }

        [Test]
        public void AllModifierTypes_CanBeAssignedAndRead()
        {
            foreach (MatchModifierType t in Enum.GetValues(typeof(MatchModifierType)))
            {
                SetField(_so, "_modifierType", t);
                Assert.AreEqual(t, _so.ModifierType,
                    $"ModifierType round-trip failed for value {t}.");
            }
        }

        // ── Reflection field round-trips ───────────────────────────────────────

        [Test]
        public void ReflectionSet_RewardMultiplier_ReturnsCorrectValue()
        {
            SetField(_so, "_rewardMultiplier", 2.5f);
            Assert.AreEqual(2.5f, _so.RewardMultiplier,
                "RewardMultiplier must return the injected value.");
        }

        [Test]
        public void ReflectionSet_TimeMultiplier_ReturnsCorrectValue()
        {
            SetField(_so, "_timeMultiplier", 0.5f);
            Assert.AreEqual(0.5f, _so.TimeMultiplier,
                "TimeMultiplier must return the injected value.");
        }

        [Test]
        public void ReflectionSet_ArmorMultiplier_ReturnsCorrectValue()
        {
            SetField(_so, "_armorMultiplier", 0f);
            Assert.AreEqual(0f, _so.ArmorMultiplier,
                "ArmorMultiplier must allow 0 (FragileArmor preset strips armor entirely).");
        }

        [Test]
        public void ReflectionSet_SpeedMultiplier_ReturnsCorrectValue()
        {
            SetField(_so, "_speedMultiplier", 1.5f);
            Assert.AreEqual(1.5f, _so.SpeedMultiplier,
                "SpeedMultiplier must return the injected value.");
        }
    }
}
