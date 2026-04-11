using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="HazardZoneSO"/> and the <see cref="HazardZoneType"/> enum.
    ///
    /// Covers:
    ///   • Fresh-instance defaults for all four configurable fields.
    ///   • Reflection round-trips for every serialised field.
    ///   • <see cref="HazardZoneSO.DamageSourceId"/> fallback when serialised value is null/empty.
    ///   • <see cref="HazardZoneType"/> enum cardinality — exactly four values.
    /// </summary>
    public class HazardZoneSOTests
    {
        private HazardZoneSO _so;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(HazardZoneSO target, string fieldName, object value)
        {
            FieldInfo fi = typeof(HazardZoneSO)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on HazardZoneSO.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so = ScriptableObject.CreateInstance<HazardZoneSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            _so = null;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_HazardType_DefaultsToLava()
        {
            Assert.AreEqual(HazardZoneType.Lava, _so.HazardType);
        }

        [Test]
        public void FreshInstance_DamagePerTick_DefaultsFive()
        {
            Assert.AreEqual(5f, _so.DamagePerTick, 0.001f);
        }

        [Test]
        public void FreshInstance_TickInterval_DefaultsOne()
        {
            Assert.AreEqual(1f, _so.TickInterval, 0.001f);
        }

        [Test]
        public void FreshInstance_DamageSourceId_DefaultsEnvironment()
        {
            Assert.AreEqual("Environment", _so.DamageSourceId);
        }

        // ── Reflection round-trips ────────────────────────────────────────────

        [Test]
        public void HazardType_Reflection_RoundTrip()
        {
            SetField(_so, "_hazardType", HazardZoneType.Electric);
            Assert.AreEqual(HazardZoneType.Electric, _so.HazardType);
        }

        [Test]
        public void DamagePerTick_Reflection_RoundTrip()
        {
            SetField(_so, "_damagePerTick", 12.5f);
            Assert.AreEqual(12.5f, _so.DamagePerTick, 0.001f);
        }

        [Test]
        public void TickInterval_Reflection_RoundTrip()
        {
            SetField(_so, "_tickInterval", 0.5f);
            Assert.AreEqual(0.5f, _so.TickInterval, 0.001f);
        }

        [Test]
        public void DamageSourceId_Reflection_RoundTrip()
        {
            SetField(_so, "_damageSourceId", "Hazard_Lava_01");
            Assert.AreEqual("Hazard_Lava_01", _so.DamageSourceId);
        }

        // ── DamageSourceId fallback ───────────────────────────────────────────

        [Test]
        public void DamageSourceId_EmptyString_FallsBackToEnvironment()
        {
            SetField(_so, "_damageSourceId", "");
            Assert.AreEqual("Environment", _so.DamageSourceId);
        }

        [Test]
        public void DamageSourceId_NullString_FallsBackToEnvironment()
        {
            SetField(_so, "_damageSourceId", null);
            Assert.AreEqual("Environment", _so.DamageSourceId);
        }

        // ── HazardZoneType enum cardinality ───────────────────────────────────

        [Test]
        public void HazardZoneType_HasExactlyFourValues()
        {
            string[] names = Enum.GetNames(typeof(HazardZoneType));
            Assert.AreEqual(4, names.Length,
                $"Expected exactly 4 HazardZoneType values but found {names.Length}: " +
                string.Join(", ", names));
        }

        [Test]
        public void HazardZoneType_ContainsExpectedValues()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.Lava));
            Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.Electric));
            Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.Spikes));
            Assert.IsTrue(Enum.IsDefined(typeof(HazardZoneType), HazardZoneType.Acid));
        }
    }
}
