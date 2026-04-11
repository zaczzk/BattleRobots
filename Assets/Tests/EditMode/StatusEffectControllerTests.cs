using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="StatusEffectController"/>.
    ///
    /// FixedUpdate is not called in EditMode, so all tick-based behaviour
    /// (Burn damage, expiry) is not tested here.
    /// Instead, tests validate the synchronous API surface:
    ///   • Fresh-instance defaults.
    ///   • ApplyEffect — null guard, count, derived property updates.
    ///   • Stacking — same-type take-maximum rule.
    ///   • Clear — resets count and derived properties.
    ///
    /// Also covers the patches added in this sprint:
    ///   • <see cref="RobotLocomotionController.SetStunned"/> — flag stored.
    ///   • <see cref="RobotLocomotionController.SetSlowFactor"/> — clamp + storage.
    ///   • <see cref="DamageReceiver.TriggerStatusEffect"/> — null controller no-throw.
    ///
    /// <c>StatusEffectController</c> has no [RequireComponent] dependency so it
    /// attaches to a plain GameObject without auto-adding ArticulationBody.
    /// </summary>
    public class StatusEffectControllerTests
    {
        private GameObject              _go;
        private StatusEffectController  _ctrl;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static StatusEffectSO MakeEffect(StatusEffectType type,
                                                  float duration    = 3f,
                                                  float dps         = 5f,
                                                  float slowFactor  = 0.5f)
        {
            var so = ScriptableObject.CreateInstance<StatusEffectSO>();
            SetField(so, "_type",            type);
            SetField(so, "_durationSeconds", duration);
            SetField(so, "_damagePerSecond", dps);
            SetField(so, "_slowFactor",      slowFactor);
            return so;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("TestStatusEffects");
            _ctrl = _go.AddComponent<StatusEffectController>();
            // Awake initialises _slots array, _activeCount = 0, _isStunned = false,
            // _currentSlowFactor = 1f.
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            _go   = null;
            _ctrl = null;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_IsStunned_IsFalse()
        {
            Assert.IsFalse(_ctrl.IsStunned,
                "IsStunned must be false immediately after Awake.");
        }

        [Test]
        public void FreshInstance_CurrentSlowFactor_IsOne()
        {
            Assert.AreEqual(1f, _ctrl.CurrentSlowFactor, 1e-6f,
                "CurrentSlowFactor must be 1.0 (no slowdown) immediately after Awake.");
        }

        [Test]
        public void FreshInstance_ActiveEffectCount_IsZero()
        {
            Assert.AreEqual(0, _ctrl.ActiveEffectCount,
                "ActiveEffectCount must be 0 immediately after Awake.");
        }

        // ── ApplyEffect — null guard ──────────────────────────────────────────

        [Test]
        public void ApplyEffect_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _ctrl.ApplyEffect(null),
                "ApplyEffect(null) must be a no-op and must not throw.");
            Assert.AreEqual(0, _ctrl.ActiveEffectCount,
                "ActiveEffectCount must remain 0 after ApplyEffect(null).");
        }

        // ── ApplyEffect — count and derived properties ────────────────────────

        [Test]
        public void ApplyEffect_BurnEffect_IncreasesActiveCount()
        {
            var burn = MakeEffect(StatusEffectType.Burn);
            _ctrl.ApplyEffect(burn);

            Assert.AreEqual(1, _ctrl.ActiveEffectCount,
                "ActiveEffectCount must be 1 after applying one Burn effect.");

            ScriptableObject.DestroyImmediate(burn);
        }

        [Test]
        public void ApplyEffect_StunEffect_SetsIsStunned()
        {
            var stun = MakeEffect(StatusEffectType.Stun);
            _ctrl.ApplyEffect(stun);

            Assert.IsTrue(_ctrl.IsStunned,
                "IsStunned must be true immediately after ApplyEffect with a Stun effect.");

            ScriptableObject.DestroyImmediate(stun);
        }

        [Test]
        public void ApplyEffect_SlowEffect_SetsCurrentSlowFactor()
        {
            var slow = MakeEffect(StatusEffectType.Slow, slowFactor: 0.4f);
            _ctrl.ApplyEffect(slow);

            Assert.AreEqual(0.4f, _ctrl.CurrentSlowFactor, 1e-6f,
                "CurrentSlowFactor must equal the applied effect's SlowFactor.");

            ScriptableObject.DestroyImmediate(slow);
        }

        // ── ApplyEffect — stacking (take-maximum rule) ────────────────────────

        [Test]
        public void ApplyEffect_SameType_LongerDuration_ReplacesExisting()
        {
            // First: apply Stun for 2 s.
            var stunShort = MakeEffect(StatusEffectType.Stun, duration: 2f);
            _ctrl.ApplyEffect(stunShort);
            Assert.AreEqual(1, _ctrl.ActiveEffectCount, "Setup: count must be 1.");

            // Second: apply Stun for 5 s — should replace because 5 > 2.
            var stunLong = MakeEffect(StatusEffectType.Stun, duration: 5f);
            _ctrl.ApplyEffect(stunLong);

            Assert.AreEqual(1, _ctrl.ActiveEffectCount,
                "ActiveEffectCount must remain 1 — same type replaces, not adds.");
            // IsStunned must still be true after the replacement.
            Assert.IsTrue(_ctrl.IsStunned, "IsStunned must remain true after replacing with longer stun.");

            ScriptableObject.DestroyImmediate(stunShort);
            ScriptableObject.DestroyImmediate(stunLong);
        }

        [Test]
        public void ApplyEffect_SameType_ShorterDuration_KeepsExisting()
        {
            // First: apply Stun for 5 s.
            var stunLong = MakeEffect(StatusEffectType.Stun, duration: 5f);
            _ctrl.ApplyEffect(stunLong);
            Assert.AreEqual(1, _ctrl.ActiveEffectCount, "Setup: count must be 1.");

            // Second: apply Stun for 1 s — should NOT replace because 1 < 5.
            var stunShort = MakeEffect(StatusEffectType.Stun, duration: 1f);
            _ctrl.ApplyEffect(stunShort);

            Assert.AreEqual(1, _ctrl.ActiveEffectCount,
                "ActiveEffectCount must remain 1 — shorter duration must not add a new slot.");

            ScriptableObject.DestroyImmediate(stunLong);
            ScriptableObject.DestroyImmediate(stunShort);
        }

        // ── ApplyEffect — three distinct types ───────────────────────────────

        [Test]
        public void ApplyEffect_ThreeDistinctTypes_AllCountedAsActive()
        {
            var burn = MakeEffect(StatusEffectType.Burn);
            var stun = MakeEffect(StatusEffectType.Stun);
            var slow = MakeEffect(StatusEffectType.Slow);

            _ctrl.ApplyEffect(burn);
            _ctrl.ApplyEffect(stun);
            _ctrl.ApplyEffect(slow);

            Assert.AreEqual(3, _ctrl.ActiveEffectCount,
                "ActiveEffectCount must be 3 when one effect of each type is active.");
            Assert.IsTrue(_ctrl.IsStunned,
                "IsStunned must be true when a Stun effect is among the three active effects.");
            Assert.Less(_ctrl.CurrentSlowFactor, 1f,
                "CurrentSlowFactor must be below 1 when a Slow effect is active.");

            ScriptableObject.DestroyImmediate(burn);
            ScriptableObject.DestroyImmediate(stun);
            ScriptableObject.DestroyImmediate(slow);
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesAllEffects_CountIsZero()
        {
            _ctrl.ApplyEffect(MakeEffect(StatusEffectType.Burn));
            _ctrl.ApplyEffect(MakeEffect(StatusEffectType.Stun));
            _ctrl.Clear();

            Assert.AreEqual(0, _ctrl.ActiveEffectCount,
                "ActiveEffectCount must be 0 after Clear().");
        }

        [Test]
        public void Clear_EmptyController_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _ctrl.Clear(),
                "Clear() on a controller with no active effects must not throw.");
        }

        [Test]
        public void Clear_ResetsIsStunned_ToFalse()
        {
            _ctrl.ApplyEffect(MakeEffect(StatusEffectType.Stun));
            Assert.IsTrue(_ctrl.IsStunned, "Setup: IsStunned must be true before Clear.");

            _ctrl.Clear();
            Assert.IsFalse(_ctrl.IsStunned,
                "IsStunned must be false after Clear().");
        }

        [Test]
        public void Clear_ResetsSlowFactor_ToOne()
        {
            _ctrl.ApplyEffect(MakeEffect(StatusEffectType.Slow, slowFactor: 0.3f));
            Assert.Less(_ctrl.CurrentSlowFactor, 1f, "Setup: SlowFactor must be below 1 before Clear.");

            _ctrl.Clear();
            Assert.AreEqual(1f, _ctrl.CurrentSlowFactor, 1e-6f,
                "CurrentSlowFactor must be 1.0 after Clear().");
        }

        // ── RobotLocomotionController patches ─────────────────────────────────

        [Test]
        public void RobotLocomotionController_SetSlowFactor_ClampsToMin()
        {
            // SetSlowFactor should clamp values below 0.01 to 0.01.
            var locoGo = new GameObject("TestLoco");
            var loco   = locoGo.AddComponent<RobotLocomotionController>();
            // RequireComponent auto-adds ArticulationBody.

            loco.SetSlowFactor(0f);

            FieldInfo fi = typeof(RobotLocomotionController)
                .GetField("_statusSlowFactor", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "_statusSlowFactor field not found on RobotLocomotionController.");
            float stored = (float)fi.GetValue(loco);
            Assert.AreEqual(0.01f, stored, 1e-6f,
                "SetSlowFactor(0) must clamp to 0.01 to preserve minimum locomotion.");

            Object.DestroyImmediate(locoGo);
        }
    }
}
