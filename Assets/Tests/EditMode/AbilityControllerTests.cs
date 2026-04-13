using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AbilityController"/>.
    ///
    /// Covers:
    ///   • TryActivate when _ability is null → false (no event).
    ///   • TryActivate when on cooldown → false + raises _onAbilityFailed.
    ///   • TryActivate when _energySystem is null → succeeds (no energy check).
    ///   • TryActivate when energy insufficient → false + raises _onAbilityFailed.
    ///   • TryActivate valid: returns true, sets cooldown, raises _onAbilityActivated,
    ///     consumes energy from _energySystem.
    ///   • IsOnCooldown and RemainingCooldown defaults.
    ///   • TryActivate with null activated-event channel does not throw.
    /// </summary>
    public class AbilityControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static AbilityController MakeController(out GameObject go)
        {
            go = new GameObject("AbilityControllerTest");
            go.SetActive(false); // prevent OnEnable side-effects
            return go.AddComponent<AbilityController>();
        }

        private static PartAbilitySO MakeAbility(float energyCost = 10f, float cooldown = 5f)
        {
            var ability = ScriptableObject.CreateInstance<PartAbilitySO>();
            SetField(ability, "_energyCost", energyCost);
            SetField(ability, "_cooldown",   cooldown);
            SetField(ability, "_abilityId",  "test_ability");
            return ability;
        }

        private static EnergySystemSO MakeEnergy(float max = 100f)
        {
            var energy = ScriptableObject.CreateInstance<EnergySystemSO>();
            // OnEnable fires on CreateInstance → fills to max
            return energy;
        }

        // ── TryActivate — null ability ────────────────────────────────────────

        [Test]
        public void TryActivate_NullAbility_ReturnsFalse()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<AbilityController>();
            // _ability remains null

            bool result = ctrl.TryActivate();

            Object.DestroyImmediate(go);
            Assert.IsFalse(result,
                "TryActivate must return false when _ability is null.");
        }

        // ── TryActivate — on cooldown ─────────────────────────────────────────

        [Test]
        public void TryActivate_OnCooldown_ReturnsFalse()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 0f, cooldown: 5f);
            SetField(ctrl, "_ability",           ability);
            SetField(ctrl, "_remainingCooldown", 3f); // on cooldown

            bool result = ctrl.TryActivate();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Assert.IsFalse(result,
                "TryActivate must return false when IsOnCooldown is true.");
        }

        [Test]
        public void TryActivate_OnCooldown_RaisesOnAbilityFailed()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 0f, cooldown: 5f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised  = 0;
            channel.RegisterCallback(() => raised++);

            SetField(ctrl, "_ability",           ability);
            SetField(ctrl, "_onAbilityFailed",   channel);
            SetField(ctrl, "_remainingCooldown", 3f);

            ctrl.TryActivate();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onAbilityFailed must be raised once when TryActivate is blocked by cooldown.");
        }

        // ── TryActivate — null energy system ─────────────────────────────────

        [Test]
        public void TryActivate_NullEnergySystem_ReturnsTrue()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 50f, cooldown: 0f);
            SetField(ctrl, "_ability", ability);
            // _energySystem remains null → no energy check

            bool result = ctrl.TryActivate();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Assert.IsTrue(result,
                "TryActivate must succeed when _energySystem is null (no energy requirement).");
        }

        // ── TryActivate — insufficient energy ────────────────────────────────

        [Test]
        public void TryActivate_InsufficientEnergy_ReturnsFalse()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 200f, cooldown: 0f); // costs more than max
            var energy  = MakeEnergy(max: 100f);
            SetField(ctrl, "_ability",       ability);
            SetField(ctrl, "_energySystem",  energy);

            bool result = ctrl.TryActivate();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
            Assert.IsFalse(result,
                "TryActivate must return false when energy is insufficient.");
        }

        [Test]
        public void TryActivate_InsufficientEnergy_RaisesOnAbilityFailed()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 200f, cooldown: 0f);
            var energy  = MakeEnergy(max: 100f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised  = 0;
            channel.RegisterCallback(() => raised++);

            SetField(ctrl, "_ability",         ability);
            SetField(ctrl, "_energySystem",    energy);
            SetField(ctrl, "_onAbilityFailed", channel);

            ctrl.TryActivate();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onAbilityFailed must be raised once when energy is insufficient.");
        }

        // ── TryActivate — success path ────────────────────────────────────────

        [Test]
        public void TryActivate_Valid_ReturnsTrue()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 10f, cooldown: 5f);
            var energy  = MakeEnergy(max: 100f);
            SetField(ctrl, "_ability",      ability);
            SetField(ctrl, "_energySystem", energy);

            bool result = ctrl.TryActivate();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
            Assert.IsTrue(result,
                "TryActivate must return true when ability is ready and energy is sufficient.");
        }

        [Test]
        public void TryActivate_Valid_SetsCooldown()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 0f, cooldown: 3f);
            SetField(ctrl, "_ability", ability);

            ctrl.TryActivate();

            float remaining = ctrl.RemainingCooldown;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Assert.AreEqual(3f, remaining, 0.001f,
                "TryActivate must set RemainingCooldown to CooldownDuration on success.");
        }

        [Test]
        public void TryActivate_Valid_RaisesOnAbilityActivated()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 0f, cooldown: 0f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised  = 0;
            channel.RegisterCallback(() => raised++);

            SetField(ctrl, "_ability",            ability);
            SetField(ctrl, "_onAbilityActivated", channel);

            ctrl.TryActivate();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onAbilityActivated must be raised once on a successful TryActivate.");
        }

        [Test]
        public void TryActivate_Valid_ConsumesEnergyFromEnergySystem()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 30f, cooldown: 0f);
            var energy  = MakeEnergy(max: 100f);
            SetField(ctrl, "_ability",      ability);
            SetField(ctrl, "_energySystem", energy);

            ctrl.TryActivate();

            float remaining = energy.CurrentEnergy;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
            Assert.AreEqual(70f, remaining, 0.001f,
                "TryActivate must consume EnergyCost from EnergySystemSO on success.");
        }

        // ── IsOnCooldown / RemainingCooldown defaults ─────────────────────────

        [Test]
        public void IsOnCooldown_Default_IsFalse()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<AbilityController>();
            bool result = ctrl.IsOnCooldown;
            Object.DestroyImmediate(go);
            Assert.IsFalse(result,
                "IsOnCooldown must be false on a fresh AbilityController.");
        }

        [Test]
        public void RemainingCooldown_Default_IsZero()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<AbilityController>();
            float result = ctrl.RemainingCooldown;
            Object.DestroyImmediate(go);
            Assert.AreEqual(0f, result, 0.001f,
                "RemainingCooldown must be 0 on a fresh AbilityController.");
        }

        [Test]
        public void IsOnCooldown_AfterActivation_IsTrue()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 0f, cooldown: 5f);
            SetField(ctrl, "_ability", ability);

            ctrl.TryActivate();
            bool result = ctrl.IsOnCooldown;

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Assert.IsTrue(result,
                "IsOnCooldown must be true immediately after a successful TryActivate.");
        }

        [Test]
        public void RemainingCooldown_AfterActivation_EqualsCooldownDuration()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 0f, cooldown: 7f);
            SetField(ctrl, "_ability", ability);

            ctrl.TryActivate();
            float result = ctrl.RemainingCooldown;

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Assert.AreEqual(7f, result, 0.001f,
                "RemainingCooldown must equal CooldownDuration immediately after activation.");
        }

        // ── Null event channel safety ─────────────────────────────────────────

        [Test]
        public void TryActivate_NullActivatedEvent_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilityController>();
            var ability = MakeAbility(energyCost: 0f, cooldown: 0f);
            SetField(ctrl, "_ability",            ability);
            // _onAbilityActivated remains null

            Assert.DoesNotThrow(() => ctrl.TryActivate(),
                "TryActivate must not throw when _onAbilityActivated is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
        }
    }
}
