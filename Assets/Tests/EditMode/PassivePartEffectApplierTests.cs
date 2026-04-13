using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PassivePartEffectSO"/>, <see cref="PassiveEffectApplier"/>,
    /// and the <see cref="EnergySystemSO.SetRechargeRate"/> patch.
    ///
    /// Covers:
    ///   PassivePartEffectSO:
    ///     • Default StatType is DamageReduction.
    ///     • Default Value is 5.
    ///     • StatType round-trip assignment.
    ///     • Value round-trip assignment.
    ///
    ///   EnergySystemSO.SetRechargeRate patch:
    ///     • Sets the recharge rate to the given value.
    ///     • Clamps negative values to 0.
    ///
    ///   PassiveEffectApplier:
    ///     • OnEnable/OnDisable with all fields null — no throw.
    ///     • Null event channel — no throw.
    ///     • OnDisable unregisters the callback.
    ///     • Apply with null effect — no-op.
    ///     • DamageReduction stat — SetArmorRating called with correct value.
    ///     • MaxHealthBonus stat — health InitForMatch+Reset raises max and current HP.
    ///     • RechargeRateBonus stat — EnergySystemSO recharge rate increases.
    ///     • onMatchStarted raise triggers Apply().
    /// </summary>
    public class PassivePartEffectApplierTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static PassivePartEffectSO MakeEffect(PassiveStatType type, float value)
        {
            var effect = ScriptableObject.CreateInstance<PassivePartEffectSO>();
            SetField(effect, "_statType", type);
            SetField(effect, "_value",    value);
            return effect;
        }

        private static HealthSO MakeHealth(float max = 100f)
        {
            var h = ScriptableObject.CreateInstance<HealthSO>();
            SetField(h, "_maxHealth", max);
            h.Reset();
            return h;
        }

        private static EnergySystemSO MakeEnergy(float rechargeRate = 10f)
        {
            var e = ScriptableObject.CreateInstance<EnergySystemSO>();
            SetField(e, "_rechargeRate", rechargeRate);
            SetField(e, "_maxEnergy",    100f);
            return e;
        }

        // ── PassivePartEffectSO ───────────────────────────────────────────────

        [Test]
        public void PassivePartEffectSO_DefaultStatType_IsDamageReduction()
        {
            var effect = ScriptableObject.CreateInstance<PassivePartEffectSO>();
            Assert.AreEqual(PassiveStatType.DamageReduction, effect.StatType,
                "Default StatType should be DamageReduction.");
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void PassivePartEffectSO_DefaultValue_IsFive()
        {
            var effect = ScriptableObject.CreateInstance<PassivePartEffectSO>();
            Assert.AreEqual(5f, effect.Value, 0.001f,
                "Default Value should be 5.");
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void PassivePartEffectSO_StatType_RoundTrip()
        {
            var effect = MakeEffect(PassiveStatType.MaxHealthBonus, 20f);
            Assert.AreEqual(PassiveStatType.MaxHealthBonus, effect.StatType,
                "StatType should round-trip MaxHealthBonus.");
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void PassivePartEffectSO_Value_RoundTrip()
        {
            var effect = MakeEffect(PassiveStatType.RechargeRateBonus, 7.5f);
            Assert.AreEqual(7.5f, effect.Value, 0.001f,
                "Value should round-trip 7.5.");
            Object.DestroyImmediate(effect);
        }

        // ── EnergySystemSO.SetRechargeRate patch ─────────────────────────────

        [Test]
        public void EnergySystemSO_SetRechargeRate_SetsRate()
        {
            var energy = MakeEnergy(10f);
            energy.SetRechargeRate(25f);
            Assert.AreEqual(25f, energy.RechargeRate, 0.001f,
                "SetRechargeRate should update RechargeRate.");
            Object.DestroyImmediate(energy);
        }

        [Test]
        public void EnergySystemSO_SetRechargeRate_ClampsNegativeToZero()
        {
            var energy = MakeEnergy(10f);
            energy.SetRechargeRate(-5f);
            Assert.AreEqual(0f, energy.RechargeRate, 0.001f,
                "SetRechargeRate should clamp negative values to 0.");
            Object.DestroyImmediate(energy);
        }

        // ── PassiveEffectApplier ──────────────────────────────────────────────

        [Test]
        public void PassiveEffectApplier_OnEnableDisable_NullAll_DoesNotThrow()
        {
            var go       = new GameObject("Robot");
            var applier  = go.AddComponent<PassiveEffectApplier>();

            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null fields must not throw.");
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null fields must not throw.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PassiveEffectApplier_NullEventChannel_DoesNotThrow()
        {
            var go      = new GameObject("Robot");
            var applier = go.AddComponent<PassiveEffectApplier>();
            var effect  = MakeEffect(PassiveStatType.DamageReduction, 5f);
            SetField(applier, "_effect", effect);
            // _onMatchStarted left null.

            Assert.DoesNotThrow(() => applier.Apply(),
                "Apply with null _onMatchStarted channel must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void PassiveEffectApplier_OnDisable_UnregistersCallback()
        {
            var go       = new GameObject("Robot");
            var applier  = go.AddComponent<PassiveEffectApplier>();
            var channel  = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(applier, "_onMatchStarted", channel);

            // Assign a DamageReduction effect and a DR with ArmorRating 0.
            var drGo     = new GameObject("DR");
            var dr       = drGo.AddComponent<DamageReceiver>();
            var effect   = MakeEffect(PassiveStatType.DamageReduction, 10f);
            SetField(applier, "_effect",         effect);
            SetField(applier, "_damageReceiver", dr);

            // OnEnable (from AddComponent) registers callback.
            // Baseline armor is 0.
            Assert.AreEqual(0, dr.ArmorRating);

            // Disable — unregisters callback.
            go.SetActive(false);

            // Raise event — must NOT apply effect since callback was unregistered.
            channel.Raise();
            Assert.AreEqual(0, dr.ArmorRating,
                "After OnDisable the callback should be unregistered; Raise must have no effect.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(drGo);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Apply_NullEffect_IsNoOp()
        {
            var go      = new GameObject("Robot");
            var applier = go.AddComponent<PassiveEffectApplier>();
            // _effect is null by default.

            Assert.DoesNotThrow(() => applier.Apply(),
                "Apply with null _effect must not throw.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Apply_DamageReduction_IncreasesArmorRating()
        {
            var go      = new GameObject("Robot");
            var drGo    = new GameObject("DR");
            var applier = go.AddComponent<PassiveEffectApplier>();
            var dr      = drGo.AddComponent<DamageReceiver>();
            var effect  = MakeEffect(PassiveStatType.DamageReduction, 15f);

            SetField(applier, "_effect",         effect);
            SetField(applier, "_damageReceiver", dr);

            // Baseline: 0 armor.
            Assert.AreEqual(0, dr.ArmorRating);
            applier.Apply();
            Assert.AreEqual(15, dr.ArmorRating,
                "DamageReduction effect should add 15 to ArmorRating.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(drGo);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Apply_MaxHealthBonus_IncreasesMaxHealthAndReset()
        {
            var go      = new GameObject("Robot");
            var applier = go.AddComponent<PassiveEffectApplier>();
            var health  = MakeHealth(100f);
            var effect  = MakeEffect(PassiveStatType.MaxHealthBonus, 50f);

            SetField(applier, "_effect", effect);
            SetField(applier, "_health", health);

            applier.Apply();

            // MaxHealth should be 150; CurrentHealth should also be 150 after Reset.
            Assert.AreEqual(150f, health.MaxHealth, 0.001f,
                "MaxHealthBonus should increase MaxHealth by 50.");
            Assert.AreEqual(150f, health.CurrentHealth, 0.001f,
                "After MaxHealthBonus Apply the health should reset to the new max.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Apply_RechargeRateBonus_IncreasesRechargeRate()
        {
            var go      = new GameObject("Robot");
            var applier = go.AddComponent<PassiveEffectApplier>();
            var energy  = MakeEnergy(10f);
            var effect  = MakeEffect(PassiveStatType.RechargeRateBonus, 5f);

            SetField(applier, "_effect",       effect);
            SetField(applier, "_energySystem", energy);

            applier.Apply();

            Assert.AreEqual(15f, energy.RechargeRate, 0.001f,
                "RechargeRateBonus should add 5 to the base recharge rate of 10.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void OnMatchStarted_Raise_TriggersApply()
        {
            var go      = new GameObject("Robot");
            var drGo    = new GameObject("DR");
            var applier = go.AddComponent<PassiveEffectApplier>();
            var dr      = drGo.AddComponent<DamageReceiver>();
            var effect  = MakeEffect(PassiveStatType.DamageReduction, 20f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(applier, "_effect",         effect);
            SetField(applier, "_damageReceiver", dr);
            SetField(applier, "_onMatchStarted", channel);

            // OnEnable registers the callback (component is active by default).
            // Raise the event — should trigger Apply.
            channel.Raise();

            Assert.AreEqual(20, dr.ArmorRating,
                "Raising _onMatchStarted should trigger Apply and add 20 armor.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(drGo);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(channel);
        }
    }
}
