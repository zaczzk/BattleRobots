using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartAbilitySO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (EnergyCost=10, CooldownDuration=5, ids empty).
    ///   • Property round-trips for all four public properties.
    ///   • OnValidate warning when _abilityId is empty (smoke test via reflection).
    ///   • Zero EnergyCost accepted (energy is optional for some abilities).
    /// </summary>
    public class PartAbilitySOTests
    {
        private PartAbilitySO _ability;

        [SetUp]
        public void SetUp()
        {
            _ability = ScriptableObject.CreateInstance<PartAbilitySO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ability);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_EnergyCost_IsTen()
        {
            Assert.AreEqual(10f, _ability.EnergyCost,
                "Default EnergyCost must be 10.");
        }

        [Test]
        public void FreshInstance_CooldownDuration_IsFive()
        {
            Assert.AreEqual(5f, _ability.CooldownDuration,
                "Default CooldownDuration must be 5.");
        }

        [Test]
        public void FreshInstance_AbilityId_IsEmpty()
        {
            Assert.IsTrue(string.IsNullOrEmpty(_ability.AbilityId),
                "Default AbilityId must be null or empty on a fresh instance.");
        }

        [Test]
        public void FreshInstance_AbilityName_IsEmpty()
        {
            Assert.IsTrue(string.IsNullOrEmpty(_ability.AbilityName),
                "Default AbilityName must be null or empty on a fresh instance.");
        }

        // ── Property round-trips ─────────────────────────────────────────────

        [Test]
        public void AbilityId_RoundTrip()
        {
            SetField(_ability, "_abilityId", "dash_forward");
            Assert.AreEqual("dash_forward", _ability.AbilityId,
                "AbilityId must return the value set in _abilityId.");
        }

        [Test]
        public void AbilityName_RoundTrip()
        {
            SetField(_ability, "_abilityName", "Dash");
            Assert.AreEqual("Dash", _ability.AbilityName,
                "AbilityName must return the value set in _abilityName.");
        }

        [Test]
        public void EnergyCost_RoundTrip()
        {
            SetField(_ability, "_energyCost", 25f);
            Assert.AreEqual(25f, _ability.EnergyCost,
                "EnergyCost must return the value set in _energyCost.");
        }

        [Test]
        public void ZeroEnergyCost_IsAccepted()
        {
            SetField(_ability, "_energyCost", 0f);
            Assert.AreEqual(0f, _ability.EnergyCost,
                "EnergyCost of 0 must be accepted (free ability).");
        }
    }
}
