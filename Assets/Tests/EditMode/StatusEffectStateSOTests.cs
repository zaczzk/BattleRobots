using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="StatusEffectStateSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (all inactive, slow factor = 1).
    ///   • UpdateState — sets active flags + remaining times + slow factor.
    ///   • UpdateState — zeroes times for inactive effects.
    ///   • UpdateState — raises _onEffectsChanged event.
    ///   • UpdateState — null event channel is safe (no-throw).
    ///   • AnyEffectActive — true when any one of the three types is active.
    ///   • AnyEffectActive — false when all are inactive.
    ///   • Reset — clears all fields silently (no event raised).
    ///   • Reset — slow factor returns to 1.
    ///   • Multiple UpdateState calls — last state wins.
    /// </summary>
    public class StatusEffectStateSOTests
    {
        private StatusEffectStateSO _so;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so = ScriptableObject.CreateInstance<StatusEffectStateSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            _so = null;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_AllEffectsInactive()
        {
            Assert.IsFalse(_so.IsBurnActive);
            Assert.IsFalse(_so.IsStunActive);
            Assert.IsFalse(_so.IsSlowActive);
        }

        [Test]
        public void FreshInstance_TimesAreZero()
        {
            Assert.AreEqual(0f, _so.BurnTimeRemaining, 0.001f);
            Assert.AreEqual(0f, _so.StunTimeRemaining, 0.001f);
            Assert.AreEqual(0f, _so.SlowTimeRemaining, 0.001f);
        }

        [Test]
        public void FreshInstance_SlowFactor_IsOne()
        {
            Assert.AreEqual(1f, _so.CurrentSlowFactor, 0.001f);
        }

        [Test]
        public void FreshInstance_AnyEffectActive_IsFalse()
        {
            Assert.IsFalse(_so.AnyEffectActive);
        }

        // ── UpdateState ───────────────────────────────────────────────────────

        [Test]
        public void UpdateState_BurnActive_SetsBurnFlagsAndTime()
        {
            _so.UpdateState(burnActive: true,  burnTime: 2.5f,
                            stunActive: false, stunTime: 0f,
                            slowActive: false, slowTime: 0f, slowFactor: 1f);

            Assert.IsTrue(_so.IsBurnActive);
            Assert.AreEqual(2.5f, _so.BurnTimeRemaining, 0.001f);
            Assert.IsFalse(_so.IsStunActive);
            Assert.IsFalse(_so.IsSlowActive);
        }

        [Test]
        public void UpdateState_StunActive_SetsStunFlag()
        {
            _so.UpdateState(burnActive: false, burnTime: 0f,
                            stunActive: true,  stunTime: 1.8f,
                            slowActive: false, slowTime: 0f, slowFactor: 1f);

            Assert.IsTrue(_so.IsStunActive);
            Assert.AreEqual(1.8f, _so.StunTimeRemaining, 0.001f);
        }

        [Test]
        public void UpdateState_SlowActive_SetsSlowFlagsAndFactor()
        {
            _so.UpdateState(burnActive: false, burnTime: 0f,
                            stunActive: false, stunTime: 0f,
                            slowActive: true,  slowTime: 4f, slowFactor: 0.4f);

            Assert.IsTrue(_so.IsSlowActive);
            Assert.AreEqual(4f,   _so.SlowTimeRemaining, 0.001f);
            Assert.AreEqual(0.4f, _so.CurrentSlowFactor, 0.001f);
        }

        [Test]
        public void UpdateState_InactiveEffect_TimeClampsToZero()
        {
            // Even if caller passes a non-zero time for an inactive effect,
            // UpdateState should store 0 so stale time values don't leak.
            _so.UpdateState(burnActive: false, burnTime: 99f,
                            stunActive: false, stunTime: 99f,
                            slowActive: false, slowTime: 99f, slowFactor: 0.5f);

            Assert.AreEqual(0f, _so.BurnTimeRemaining, 0.001f);
            Assert.AreEqual(0f, _so.StunTimeRemaining, 0.001f);
            Assert.AreEqual(0f, _so.SlowTimeRemaining, 0.001f);
            Assert.AreEqual(1f, _so.CurrentSlowFactor, 0.001f);
        }

        [Test]
        public void UpdateState_RaisesEffectsChangedEvent()
        {
            var evt     = ScriptableObject.CreateInstance<VoidGameEvent>();
            int counter = 0;
            evt.RegisterCallback(() => counter++);
            SetField(_so, "_onEffectsChanged", evt);

            _so.UpdateState(true, 3f, false, 0f, false, 0f, 1f);

            Assert.AreEqual(1, counter);
            evt.UnregisterCallback(() => counter++);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void UpdateState_NullEventChannel_DoesNotThrow()
        {
            SetField(_so, "_onEffectsChanged", null);

            Assert.DoesNotThrow(() =>
                _so.UpdateState(true, 2f, true, 1f, true, 3f, 0.5f));
        }

        [Test]
        public void AnyEffectActive_TrueWhenAtLeastOneActive()
        {
            _so.UpdateState(false, 0f, true, 2f, false, 0f, 1f);
            Assert.IsTrue(_so.AnyEffectActive);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllState()
        {
            _so.UpdateState(true, 3f, true, 2f, true, 4f, 0.3f);
            _so.Reset();

            Assert.IsFalse(_so.IsBurnActive);
            Assert.IsFalse(_so.IsStunActive);
            Assert.IsFalse(_so.IsSlowActive);
            Assert.AreEqual(1f, _so.CurrentSlowFactor, 0.001f);
            Assert.IsFalse(_so.AnyEffectActive);
        }

        [Test]
        public void UpdateState_MultipleCallsLastWins()
        {
            _so.UpdateState(true, 5f, false, 0f, false, 0f, 1f);
            _so.UpdateState(false, 0f, false, 0f, true, 3f, 0.6f);

            Assert.IsFalse(_so.IsBurnActive);
            Assert.IsTrue (_so.IsSlowActive);
            Assert.AreEqual(3f,   _so.SlowTimeRemaining, 0.001f);
            Assert.AreEqual(0.6f, _so.CurrentSlowFactor, 0.001f);
        }
    }
}
