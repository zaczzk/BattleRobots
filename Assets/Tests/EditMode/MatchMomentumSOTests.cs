using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchMomentumSO"/>.
    ///
    /// Covers:
    ///   • Default field values (maxMomentum = 100, decayRate = 5).
    ///   • Momentum starts at zero after OnEnable.
    ///   • AddMomentum increases the value.
    ///   • AddMomentum clamps to MaxMomentum.
    ///   • AddMomentum fires _onMomentumChanged (null-safe).
    ///   • AddMomentum fires _onMomentumFull when filling to max.
    ///   • AddMomentum does not fire _onMomentumFull when not reaching max.
    ///   • AddMomentum ignores zero / negative amounts.
    ///   • Tick decays the value by decayRate × deltaTime.
    ///   • Tick clamps at zero.
    ///   • Tick fires _onMomentumChanged when value changes.
    ///   • Tick is no-op when momentum is already zero.
    ///   • Reset sets momentum to zero.
    ///   • Reset fires _onMomentumChanged.
    /// </summary>
    public class MatchMomentumSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static MatchMomentumSO CreateSO(float max = 100f, float decay = 5f)
        {
            var so = ScriptableObject.CreateInstance<MatchMomentumSO>();
            SetField(so, "_maxMomentum", max);
            SetField(so, "_decayRate",   decay);
            // Simulate OnEnable to reset runtime state.
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Momentum_IsZero()
        {
            var so = CreateSO();
            Assert.AreEqual(0f, so.Momentum, 0.0001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_MaxMomentum_Is100()
        {
            var so = ScriptableObject.CreateInstance<MatchMomentumSO>();
            Assert.AreEqual(100f, so.MaxMomentum, 0.0001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_DecayRate_Is5()
        {
            var so = ScriptableObject.CreateInstance<MatchMomentumSO>();
            Assert.AreEqual(5f, so.DecayRate, 0.0001f);
            Object.DestroyImmediate(so);
        }

        // ── AddMomentum ───────────────────────────────────────────────────────

        [Test]
        public void AddMomentum_IncreasesValue()
        {
            var so = CreateSO(max: 100f);
            so.AddMomentum(30f);
            Assert.AreEqual(30f, so.Momentum, 0.0001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddMomentum_ClampsToMaxMomentum()
        {
            var so = CreateSO(max: 50f);
            so.AddMomentum(200f);
            Assert.AreEqual(50f, so.Momentum, 0.0001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddMomentum_NullChannel_DoesNotThrow()
        {
            var so = CreateSO();
            SetField(so, "_onMomentumChanged", null);
            Assert.DoesNotThrow(() => so.AddMomentum(10f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddMomentum_FiresOnMomentumChanged()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count   = 0;
            channel.RegisterCallback(() => count++);
            SetField(so, "_onMomentumChanged", channel);

            so.AddMomentum(20f);

            Assert.AreEqual(1, count);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void AddMomentum_WhenFilledToMax_FiresOnMomentumFull()
        {
            var so      = CreateSO(max: 100f);
            var full    = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count   = 0;
            full.RegisterCallback(() => count++);
            SetField(so, "_onMomentumFull", full);

            so.AddMomentum(100f);   // fills to exactly max

            Assert.AreEqual(1, count);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(full);
        }

        [Test]
        public void AddMomentum_WhenNotFilledToMax_DoesNotFireOnMomentumFull()
        {
            var so      = CreateSO(max: 100f);
            var full    = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count   = 0;
            full.RegisterCallback(() => count++);
            SetField(so, "_onMomentumFull", full);

            so.AddMomentum(50f);    // half way — should NOT fire full

            Assert.AreEqual(0, count);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(full);
        }

        [Test]
        public void AddMomentum_ZeroAmount_Ignored()
        {
            var so = CreateSO();
            so.AddMomentum(0f);
            Assert.AreEqual(0f, so.Momentum, 0.0001f);
            Object.DestroyImmediate(so);
        }

        // ── Tick ──────────────────────────────────────────────────────────────

        [Test]
        public void Tick_DecaysValueByRateTimesDeltaTime()
        {
            var so = CreateSO(max: 100f, decay: 10f);
            so.AddMomentum(80f);

            so.Tick(1f);    // should decay by 10

            Assert.AreEqual(70f, so.Momentum, 0.0001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_ClampsAtZero()
        {
            var so = CreateSO(max: 100f, decay: 50f);
            so.AddMomentum(10f);

            so.Tick(10f);   // 10 − (50 × 10) → clamped to 0

            Assert.AreEqual(0f, so.Momentum, 0.0001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_FiresOnMomentumChanged_WhenValueChanges()
        {
            var so      = CreateSO(max: 100f, decay: 10f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count   = 0;
            channel.RegisterCallback(() => count++);
            SetField(so, "_onMomentumChanged", channel);
            so.AddMomentum(50f);
            count = 0;      // reset after AddMomentum

            so.Tick(0.5f);  // decays by 5 → value changes

            Assert.AreEqual(1, count);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Tick_WhenMomentumIsZero_IsNoOp()
        {
            var so      = CreateSO(max: 100f, decay: 10f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count   = 0;
            channel.RegisterCallback(() => count++);
            SetField(so, "_onMomentumChanged", channel);

            so.Tick(5f);    // momentum already zero → no change, no event

            Assert.AreEqual(0, count);
            Assert.AreEqual(0f, so.Momentum, 0.0001f);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_SetsMomentumToZero()
        {
            var so = CreateSO();
            so.AddMomentum(75f);
            so.Reset();
            Assert.AreEqual(0f, so.Momentum, 0.0001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Reset_FiresOnMomentumChanged()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count   = 0;
            channel.RegisterCallback(() => count++);
            SetField(so, "_onMomentumChanged", channel);

            so.Reset();

            Assert.AreEqual(1, count);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }
    }
}
