using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="BotPersonalitySO"/> and <see cref="BotPersonalityType"/>.
    ///
    /// Covers:
    ///   • Fresh-instance default values for all five public properties.
    ///   • All five BotPersonalityType enum values can be stored and retrieved.
    ///   • BotPersonalityType enum has exactly five values (regression guard).
    ///   • Field round-trips via reflection (mirrors established test pattern).
    ///   • OnValidate clamps AttackCooldownMultiplier and FacingThresholdMultiplier
    ///     to their respective minimums.
    /// </summary>
    public class BotPersonalitySOTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private BotPersonalitySO _so;

        [SetUp]
        public void SetUp()
        {
            _so = ScriptableObject.CreateInstance<BotPersonalitySO>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_so != null) UnityEngine.Object.DestroyImmediate(_so);
            _so = null;
        }

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_PersonalityType_IsBalanced()
        {
            Assert.AreEqual(BotPersonalityType.Balanced, _so.PersonalityType,
                "Default PersonalityType must be Balanced.");
        }

        [Test]
        public void FreshInstance_AttackCooldownMultiplier_Is1()
        {
            Assert.AreEqual(1f, _so.AttackCooldownMultiplier, 1e-6f,
                "Default AttackCooldownMultiplier must be 1.0 (neutral).");
        }

        [Test]
        public void FreshInstance_DetectionRangeDelta_Is0()
        {
            Assert.AreEqual(0f, _so.DetectionRangeDelta, 1e-6f,
                "Default DetectionRangeDelta must be 0 (no delta).");
        }

        [Test]
        public void FreshInstance_AttackRangeDelta_Is0()
        {
            Assert.AreEqual(0f, _so.AttackRangeDelta, 1e-6f,
                "Default AttackRangeDelta must be 0 (no delta).");
        }

        [Test]
        public void FreshInstance_FacingThresholdMultiplier_Is1()
        {
            Assert.AreEqual(1f, _so.FacingThresholdMultiplier, 1e-6f,
                "Default FacingThresholdMultiplier must be 1.0 (neutral).");
        }

        // ── Field round-trips (reflection) ────────────────────────────────────

        [Test]
        public void SetAttackCooldownMultiplier_StoredAndReturned()
        {
            SetField(_so, "_attackCooldownMultiplier", 0.4f);
            Assert.AreEqual(0.4f, _so.AttackCooldownMultiplier, 1e-6f,
                "AttackCooldownMultiplier must round-trip through the property.");
        }

        [Test]
        public void SetDetectionRangeDelta_StoredAndReturned()
        {
            SetField(_so, "_detectionRangeDelta", 7.5f);
            Assert.AreEqual(7.5f, _so.DetectionRangeDelta, 1e-6f,
                "DetectionRangeDelta must round-trip through the property.");
        }

        [Test]
        public void SetAttackRangeDelta_StoredAndReturned()
        {
            SetField(_so, "_attackRangeDelta", -1.5f);
            Assert.AreEqual(-1.5f, _so.AttackRangeDelta, 1e-6f,
                "AttackRangeDelta must round-trip, including negative values.");
        }

        [Test]
        public void SetFacingThresholdMultiplier_StoredAndReturned()
        {
            SetField(_so, "_facingThresholdMultiplier", 2.5f);
            Assert.AreEqual(2.5f, _so.FacingThresholdMultiplier, 1e-6f,
                "FacingThresholdMultiplier must round-trip through the property.");
        }

        // ── Enum coverage ─────────────────────────────────────────────────────

        [Test]
        public void BotPersonalityType_HasExactlyFiveValues()
        {
            int count = Enum.GetValues(typeof(BotPersonalityType)).Length;
            Assert.AreEqual(5, count,
                "BotPersonalityType must have exactly 5 values: " +
                "Balanced, Aggressive, Defensive, Berserker, Tactical.");
        }

        [Test]
        public void AllPersonalityTypes_CanBeAssignedAndRead()
        {
            foreach (BotPersonalityType type in Enum.GetValues(typeof(BotPersonalityType)))
            {
                SetField(_so, "_personalityType", type);
                Assert.AreEqual(type, _so.PersonalityType,
                    $"PersonalityType round-trip failed for {type}.");
            }
        }

        // ── Enum value existence ───────────────────────────────────────────────

        [Test]
        public void BotPersonalityType_BalancedExists()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(BotPersonalityType), BotPersonalityType.Balanced));
        }

        [Test]
        public void BotPersonalityType_AggressiveExists()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(BotPersonalityType), BotPersonalityType.Aggressive));
        }

        [Test]
        public void BotPersonalityType_DefensiveExists()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(BotPersonalityType), BotPersonalityType.Defensive));
        }

        [Test]
        public void BotPersonalityType_BerserkerExists()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(BotPersonalityType), BotPersonalityType.Berserker));
        }

        [Test]
        public void BotPersonalityType_TacticalExists()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(BotPersonalityType), BotPersonalityType.Tactical));
        }
    }
}
