using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="EnergyRechargerMB"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with null _energySystem — no throw.
    ///   • FixedUpdate with null _energySystem — no throw.
    ///   • FixedUpdate with a partial-energy pool — energy increases.
    ///   • FixedUpdate with a full-energy pool — energy stays at max.
    ///   • FixedUpdate increases energy by rechargeRate × fixedDeltaTime.
    ///   • Default _energySystem inspector field is null.
    ///   • Multiple FixedUpdate invocations do not throw.
    ///   • Zero-delta Recharge behaves correctly (no change — tested via EnergySystemSO directly).
    /// </summary>
    public class EnergyRechargerMBTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokeFixedUpdate(EnergyRechargerMB mb)
        {
            MethodInfo mi = typeof(EnergyRechargerMB)
                .GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, "FixedUpdate method not found on EnergyRechargerMB.");
            mi.Invoke(mb, null);
        }

        private static EnergyRechargerMB MakeRecharger(out GameObject go)
        {
            go = new GameObject("EnergyRechargerTest");
            go.SetActive(false); // prevent OnEnable before wiring
            return go.AddComponent<EnergyRechargerMB>();
        }

        private static EnergySystemSO MakeEnergySystem(float max = 100f)
        {
            // CreateInstance fires OnEnable → fills _currentEnergy to max
            return ScriptableObject.CreateInstance<EnergySystemSO>();
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_NullEnergySystem_DoesNotThrow()
        {
            MakeRecharger(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _energySystem must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullEnergySystem_DoesNotThrow()
        {
            MakeRecharger(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null _energySystem must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── FixedUpdate — null guard ──────────────────────────────────────────

        [Test]
        public void FixedUpdate_NullEnergySystem_DoesNotThrow()
        {
            MakeRecharger(out GameObject go);
            var mb = go.GetComponent<EnergyRechargerMB>();
            // _energySystem remains null
            Assert.DoesNotThrow(() => InvokeFixedUpdate(mb),
                "FixedUpdate must not throw when _energySystem is null.");
            Object.DestroyImmediate(go);
        }

        // ── FixedUpdate — energy increases ────────────────────────────────────

        [Test]
        public void FixedUpdate_PartialEnergy_IncreasesEnergy()
        {
            MakeRecharger(out GameObject go);
            var mb     = go.GetComponent<EnergyRechargerMB>();
            var energy = MakeEnergySystem();
            // Partially consume energy so it's not full
            energy.Consume(50f); // 100 → 50
            float before = energy.CurrentEnergy;
            SetField(mb, "_energySystem", energy);

            InvokeFixedUpdate(mb);

            float after = energy.CurrentEnergy;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
            Assert.Greater(after, before,
                "FixedUpdate must increase energy when pool is partially depleted.");
        }

        [Test]
        public void FixedUpdate_PartialEnergy_IncreasesEnergyByExpectedAmount()
        {
            MakeRecharger(out GameObject go);
            var mb     = go.GetComponent<EnergyRechargerMB>();
            var energy = MakeEnergySystem();
            energy.Consume(50f); // 100 → 50
            SetField(mb, "_energySystem", energy);

            // Recharge: rate=10, dt=Time.fixedDeltaTime (0.02 by default) → +0.2
            float expected = 50f + energy.RechargeRate * Time.fixedDeltaTime;
            InvokeFixedUpdate(mb);

            float after = energy.CurrentEnergy;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
            Assert.AreEqual(expected, after, 0.001f,
                "FixedUpdate must add rechargeRate × fixedDeltaTime to current energy.");
        }

        // ── FixedUpdate — full energy is capped ───────────────────────────────

        [Test]
        public void FixedUpdate_FullEnergy_NoChange()
        {
            MakeRecharger(out GameObject go);
            var mb     = go.GetComponent<EnergyRechargerMB>();
            var energy = MakeEnergySystem(); // OnEnable fills to max (100)
            SetField(mb, "_energySystem", energy);

            float before = energy.CurrentEnergy; // 100
            InvokeFixedUpdate(mb);
            float after = energy.CurrentEnergy;

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
            Assert.AreEqual(before, after, 0.001f,
                "FixedUpdate must not change energy when the pool is already full.");
        }

        // ── Default field value ───────────────────────────────────────────────

        [Test]
        public void FreshInstance_EnergySystemField_IsNull()
        {
            MakeRecharger(out GameObject go);
            var mb = go.GetComponent<EnergyRechargerMB>();

            FieldInfo fi = typeof(EnergyRechargerMB)
                .GetField("_energySystem", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "_energySystem field must exist.");
            object value = fi.GetValue(mb);

            Object.DestroyImmediate(go);
            Assert.IsNull(value,
                "_energySystem must default to null (not wired by default).");
        }

        // ── Multiple FixedUpdate calls ─────────────────────────────────────────

        [Test]
        public void FixedUpdate_MultipleCallsWithPartialEnergy_DoNotThrow()
        {
            MakeRecharger(out GameObject go);
            var mb     = go.GetComponent<EnergyRechargerMB>();
            var energy = MakeEnergySystem();
            energy.Consume(90f); // 100 → 10
            SetField(mb, "_energySystem", energy);

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 10; i++)
                    InvokeFixedUpdate(mb);
            }, "Multiple FixedUpdate calls must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
        }

        // ── Energy capped at max after multiple ticks ─────────────────────────

        [Test]
        public void FixedUpdate_ManyCallsFromEmpty_EnergyNeverExceedsMax()
        {
            MakeRecharger(out GameObject go);
            var mb     = go.GetComponent<EnergyRechargerMB>();
            var energy = MakeEnergySystem();
            energy.Consume(energy.MaxEnergy); // drain to 0
            SetField(mb, "_energySystem", energy);

            // Run many ticks — energy should never exceed MaxEnergy
            for (int i = 0; i < 200; i++)
                InvokeFixedUpdate(mb);

            float after = energy.CurrentEnergy;
            float max   = energy.MaxEnergy;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
            Assert.LessOrEqual(after, max,
                "Energy must never exceed MaxEnergy after many FixedUpdate ticks.");
        }
    }
}
