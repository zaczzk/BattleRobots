using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PowerUpSO"/> and the <see cref="PowerUpType"/> enum.
    ///
    /// Covers:
    ///   • <see cref="PowerUpSO"/> fresh-instance defaults (Type, EffectAmount, DisplayName).
    ///   • <see cref="PowerUpType"/> enum shape (exact value count, all types assignable).
    ///   • <c>OnValidate</c> negative-clamp behaviour for <see cref="PowerUpSO.EffectAmount"/>.
    ///   • <see cref="PowerUpSO.FirePickedUp"/>: null-channel safety; callback invocation.
    /// </summary>
    public class PowerUpSOTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private PowerUpSO _so;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target)
        {
            MethodInfo mi = target.GetType()
                .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, "OnValidate not found.");
            mi.Invoke(target, null);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so = ScriptableObject.CreateInstance<PowerUpSO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_so);
            _so = null;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Type_IsHealthRestore()
        {
            Assert.AreEqual(PowerUpType.HealthRestore, _so.Type);
        }

        [Test]
        public void FreshInstance_EffectAmount_Is25()
        {
            Assert.AreEqual(25f, _so.EffectAmount, 0.001f);
        }

        [Test]
        public void FreshInstance_DisplayName_IsEmpty()
        {
            Assert.AreEqual("", _so.DisplayName);
        }

        // ── PowerUpType enum ──────────────────────────────────────────────────

        [Test]
        public void PowerUpType_HasExactlyTwoValues()
        {
            string[] names = Enum.GetNames(typeof(PowerUpType));
            Assert.AreEqual(2, names.Length,
                "Expected exactly 2 PowerUpType values (HealthRestore, ShieldRecharge).");
        }

        [Test]
        public void AllPowerUpTypes_CanBeAssignedAndRead()
        {
            foreach (PowerUpType type in Enum.GetValues(typeof(PowerUpType)))
            {
                SetField(_so, "_type", type);
                Assert.AreEqual(type, _so.Type,
                    $"PowerUpType.{type} round-trip failed.");
            }
        }

        // ── EffectAmount ──────────────────────────────────────────────────────

        [Test]
        public void EffectAmount_Zero_IsValid()
        {
            SetField(_so, "_effectAmount", 0f);
            InvokeOnValidate(_so);
            Assert.AreEqual(0f, _so.EffectAmount, 0.001f);
        }

        [Test]
        public void EffectAmount_Positive_StoresCorrectly()
        {
            SetField(_so, "_effectAmount", 50f);
            Assert.AreEqual(50f, _so.EffectAmount, 0.001f);
        }

        [Test]
        public void EffectAmount_Negative_ClampedToZero_ViaOnValidate()
        {
            SetField(_so, "_effectAmount", -10f);
            InvokeOnValidate(_so);
            Assert.AreEqual(0f, _so.EffectAmount, 0.001f,
                "OnValidate should clamp negative EffectAmount to 0.");
        }

        // ── FirePickedUp ──────────────────────────────────────────────────────

        [Test]
        public void FirePickedUp_NullEvent_DoesNotThrow()
        {
            // _onPickedUp defaults to null → FirePickedUp must not throw.
            Assert.DoesNotThrow(() => _so.FirePickedUp());
        }

        [Test]
        public void FirePickedUp_WithEvent_RaisesCallback()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_so, "_onPickedUp", evt);

            int callCount = 0;
            Action listener = () => callCount++;
            evt.RegisterCallback(listener);

            _so.FirePickedUp();
            Assert.AreEqual(1, callCount, "FirePickedUp should raise _onPickedUp exactly once.");

            evt.UnregisterCallback(listener);
            UnityEngine.Object.DestroyImmediate(evt);
        }
    }
}
