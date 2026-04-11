using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="StatusEffectSO"/> and the
    /// <see cref="StatusEffectType"/> enum.
    ///
    /// Covers:
    ///   • Fresh-instance default values for all five properties.
    ///   • StatusEffectType enum — exactly three values, all assignable.
    ///   • Reflection-set field storage (DurationSeconds, DamagePerSecond, SlowFactor).
    ///   • DisplayName defaults to empty string (no null).
    /// </summary>
    public class StatusEffectSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static StatusEffectSO MakeSO() =>
            ScriptableObject.CreateInstance<StatusEffectSO>();

        private static void SetField(StatusEffectSO so, string name, object value)
        {
            FieldInfo fi = typeof(StatusEffectSO)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on StatusEffectSO.");
            fi.SetValue(so, value);
        }

        private static T GetProp<T>(StatusEffectSO so, string propName)
        {
            var pi = typeof(StatusEffectSO).GetProperty(propName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(pi, $"Property '{propName}' not found on StatusEffectSO.");
            return (T)pi.GetValue(so);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Type_IsBurn()
        {
            var so = MakeSO();
            Assert.AreEqual(StatusEffectType.Burn, so.Type,
                "Default StatusEffectType must be Burn.");
            ScriptableObject.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_DurationSeconds_IsThreeF()
        {
            var so = MakeSO();
            Assert.AreEqual(3f, so.DurationSeconds, 1e-6f,
                "Default DurationSeconds must be 3 s.");
            ScriptableObject.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_DamagePerSecond_IsFiveF()
        {
            var so = MakeSO();
            Assert.AreEqual(5f, so.DamagePerSecond, 1e-6f,
                "Default DamagePerSecond must be 5.");
            ScriptableObject.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_SlowFactor_IsPointFive()
        {
            var so = MakeSO();
            Assert.AreEqual(0.5f, so.SlowFactor, 1e-6f,
                "Default SlowFactor must be 0.5.");
            ScriptableObject.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_DisplayName_IsEmptyString()
        {
            var so = MakeSO();
            // Must be empty (not null) — HUD safely displays "" without null-guard.
            Assert.IsNotNull(so.DisplayName,
                "DisplayName must not be null on a fresh StatusEffectSO.");
            Assert.AreEqual(string.Empty, so.DisplayName,
                "Default DisplayName must be an empty string.");
            ScriptableObject.DestroyImmediate(so);
        }

        // ── StatusEffectType enum ─────────────────────────────────────────────

        [Test]
        public void StatusEffectType_HasExactlyThreeValues()
        {
            var values = Enum.GetValues(typeof(StatusEffectType));
            Assert.AreEqual(3, values.Length,
                "StatusEffectType must have exactly three members: Burn, Stun, Slow.");
        }

        [Test]
        public void AllStatusEffectTypes_CanBeAssigned()
        {
            var so = MakeSO();
            foreach (StatusEffectType t in Enum.GetValues(typeof(StatusEffectType)))
            {
                Assert.DoesNotThrow(() => SetField(so, "_type", t),
                    $"Assigning StatusEffectType.{t} must not throw.");
                Assert.AreEqual(t, so.Type,
                    $"Type property must return {t} after reflection assignment.");
            }
            ScriptableObject.DestroyImmediate(so);
        }

        // ── Reflection-set field storage ──────────────────────────────────────

        [Test]
        public void ReflectionSet_DurationSeconds_Stores()
        {
            var so = MakeSO();
            SetField(so, "_durationSeconds", 7.5f);
            Assert.AreEqual(7.5f, so.DurationSeconds, 1e-6f,
                "DurationSeconds must return the reflection-injected value.");
            ScriptableObject.DestroyImmediate(so);
        }

        [Test]
        public void ReflectionSet_DamagePerSecond_Stores()
        {
            var so = MakeSO();
            SetField(so, "_damagePerSecond", 20f);
            Assert.AreEqual(20f, so.DamagePerSecond, 1e-6f,
                "DamagePerSecond must return the reflection-injected value.");
            ScriptableObject.DestroyImmediate(so);
        }

        [Test]
        public void ReflectionSet_SlowFactor_Stores()
        {
            var so = MakeSO();
            SetField(so, "_slowFactor", 0.25f);
            Assert.AreEqual(0.25f, so.SlowFactor, 1e-6f,
                "SlowFactor must return the reflection-injected value.");
            ScriptableObject.DestroyImmediate(so);
        }
    }
}
