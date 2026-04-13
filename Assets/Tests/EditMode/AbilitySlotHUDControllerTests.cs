using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AbilitySlotHUDController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all null refs — no throw.
    ///   • OnEnable / OnDisable with null channels — no throw.
    ///   • OnDisable unregisters all three callbacks.
    ///   • Refresh with null _ability — clears name and cost labels.
    ///   • Refresh with valid _ability — sets name and cost labels correctly.
    ///   • Refresh with null _energySystem — shows unavailable overlay.
    ///   • Refresh with sufficient energy — hides unavailable overlay.
    ///   • Refresh with insufficient energy — shows unavailable overlay.
    ///   • OnAbilityActivated with null _ability — no throw.
    ///   • OnAbilityActivated with valid _ability — sets _isOnCooldown when cooldown > 0.
    ///   • OnAbilityActivated with zero cooldown — _isOnCooldown is false.
    ///   • _onEnergyChanged raised — triggers Refresh (overlay updated).
    ///   • Refresh with null panel refs — no throw.
    ///   • _onAbilityFailed raised — no throw (no state change).
    /// </summary>
    public class AbilitySlotHUDControllerTests
    {
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

        private static AbilitySlotHUDController MakeController(out GameObject go)
        {
            go = new GameObject("AbilitySlotHUDTest");
            go.SetActive(false); // prevent OnEnable before wiring
            return go.AddComponent<AbilitySlotHUDController>();
        }

        private static PartAbilitySO MakeAbility(string name = "TestAbility",
                                                  float energyCost = 20f,
                                                  float cooldown   = 5f)
        {
            var ability = ScriptableObject.CreateInstance<PartAbilitySO>();
            SetField(ability, "_abilityId",    "test_id");
            SetField(ability, "_abilityName",  name);
            SetField(ability, "_energyCost",   energyCost);
            SetField(ability, "_cooldown",     cooldown);
            return ability;
        }

        private static EnergySystemSO MakeFullEnergy()
        {
            // CreateInstance fires OnEnable → fills to max (100)
            return ScriptableObject.CreateInstance<EnergySystemSO>();
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility();
            var energy  = MakeFullEnergy();
            SetField(ctrl, "_ability",      ability);
            SetField(ctrl, "_energySystem", energy);
            // All three VoidGameEvent channels remain null

            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null channels must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
        }

        [Test]
        public void OnDisable_NullChannels_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility();
            var energy  = MakeFullEnergy();
            SetField(ctrl, "_ability",      ability);
            SetField(ctrl, "_energySystem", energy);
            go.SetActive(true);

            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null channels must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
        }

        // ── OnDisable unregisters callbacks ──────────────────────────────────

        [Test]
        public void OnDisable_UnregistersEnergyChangedCallback()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var energy  = MakeFullEnergy();
            var overlay = new GameObject("Overlay");
            overlay.SetActive(false);

            SetField(ctrl, "_energySystem",   energy);
            SetField(ctrl, "_onEnergyChanged", channel);
            SetField(ctrl, "_unavailableOverlay", overlay);

            go.SetActive(true);   // OnEnable: subscribe + Refresh
            go.SetActive(false);  // OnDisable: unsubscribe

            // Drain energy so the next Refresh would show the unavailable overlay
            energy.Consume(100f);
            overlay.SetActive(false);

            channel.Raise(); // must NOT trigger Refresh (unsubscribed)

            bool stillHidden = !overlay.activeSelf;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(energy);
            Object.DestroyImmediate(overlay);

            Assert.IsTrue(stillHidden,
                "After OnDisable, raising _onEnergyChanged must not trigger Refresh.");
        }

        // ── Refresh — label display ───────────────────────────────────────────

        [Test]
        public void Refresh_NullAbility_SetsEmptyNameText()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<AbilitySlotHUDController>();
            var labelGO  = new GameObject("Label");
            var label    = labelGO.AddComponent<Text>();
            label.text   = "PREVIOUS";
            SetField(ctrl, "_abilityNameText", label);
            // _ability remains null

            go.SetActive(true); // OnEnable → Refresh

            string result = label.text;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGO);
            Assert.AreEqual(string.Empty, result,
                "Refresh must set ability name text to empty when _ability is null.");
        }

        [Test]
        public void Refresh_WithAbility_SetsAbilityNameText()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility(name: "Power Surge");
            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();
            SetField(ctrl, "_ability",         ability);
            SetField(ctrl, "_abilityNameText", label);

            go.SetActive(true); // OnEnable → Refresh

            string result = label.text;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(labelGO);
            Assert.AreEqual("Power Surge", result,
                "Refresh must display the ability's AbilityName in _abilityNameText.");
        }

        [Test]
        public void Refresh_WithAbility_SetsEnergyCostTextAsRoundedInt()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility(energyCost: 25.7f);
            var labelGO = new GameObject("CostLabel");
            var label   = labelGO.AddComponent<Text>();
            SetField(ctrl, "_ability",        ability);
            SetField(ctrl, "_energyCostText", label);

            go.SetActive(true); // OnEnable → Refresh

            string result = label.text;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(labelGO);
            Assert.AreEqual("26", result,
                "Refresh must display energy cost rounded to nearest integer.");
        }

        // ── Refresh — unavailable overlay ────────────────────────────────────

        [Test]
        public void Refresh_NullEnergySystem_ShowsUnavailableOverlay()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility(energyCost: 20f);
            var overlay = new GameObject("UnavailableOverlay");
            overlay.SetActive(false);
            SetField(ctrl, "_ability",             ability);
            SetField(ctrl, "_unavailableOverlay",  overlay);
            // _energySystem remains null → can't afford → show unavailable

            go.SetActive(true); // OnEnable → Refresh

            bool shown = overlay.activeSelf;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(overlay);
            Assert.IsTrue(shown,
                "Unavailable overlay must be shown when _energySystem is null.");
        }

        [Test]
        public void Refresh_SufficientEnergy_HidesUnavailableOverlay()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility(energyCost: 20f);
            var energy  = MakeFullEnergy(); // 100 energy, cost is 20 → can afford
            var overlay = new GameObject("UnavailableOverlay");
            overlay.SetActive(true);
            SetField(ctrl, "_ability",             ability);
            SetField(ctrl, "_energySystem",        energy);
            SetField(ctrl, "_unavailableOverlay",  overlay);

            go.SetActive(true); // OnEnable → Refresh

            bool hidden = !overlay.activeSelf;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
            Object.DestroyImmediate(overlay);
            Assert.IsTrue(hidden,
                "Unavailable overlay must be hidden when energy is sufficient.");
        }

        [Test]
        public void Refresh_InsufficientEnergy_ShowsUnavailableOverlay()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility(energyCost: 80f);
            var energy  = MakeFullEnergy();
            energy.Consume(50f); // 100 → 50, cost is 80 → cannot afford
            var overlay = new GameObject("UnavailableOverlay");
            overlay.SetActive(false);
            SetField(ctrl, "_ability",             ability);
            SetField(ctrl, "_energySystem",        energy);
            SetField(ctrl, "_unavailableOverlay",  overlay);

            go.SetActive(true); // OnEnable → Refresh

            bool shown = overlay.activeSelf;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
            Object.DestroyImmediate(overlay);
            Assert.IsTrue(shown,
                "Unavailable overlay must be shown when energy is insufficient.");
        }

        // ── OnAbilityActivated ────────────────────────────────────────────────

        [Test]
        public void OnAbilityActivated_NullAbility_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctrl, "_onAbilityActivated", channel);
            // _ability remains null
            go.SetActive(true);

            Assert.DoesNotThrow(() => channel.Raise(),
                "OnAbilityActivated must not throw when _ability is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnAbilityActivated_WithCooldown_SetsIsOnCooldown()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility(cooldown: 5f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctrl, "_ability",             ability);
            SetField(ctrl, "_onAbilityActivated",  channel);
            go.SetActive(true);

            channel.Raise(); // fires OnAbilityActivated

            bool isOnCooldown = GetField<bool>(ctrl, "_isOnCooldown");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(channel);
            Assert.IsTrue(isOnCooldown,
                "_isOnCooldown must be true after activation when cooldown > 0.");
        }

        [Test]
        public void OnAbilityActivated_ZeroCooldown_IsOnCooldownFalse()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility(cooldown: 0f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctrl, "_ability",             ability);
            SetField(ctrl, "_onAbilityActivated",  channel);
            go.SetActive(true);

            channel.Raise(); // fires OnAbilityActivated

            bool isOnCooldown = GetField<bool>(ctrl, "_isOnCooldown");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(channel);
            Assert.IsFalse(isOnCooldown,
                "_isOnCooldown must be false after activation when cooldown is 0.");
        }

        // ── _onEnergyChanged integration ──────────────────────────────────────

        [Test]
        public void OnEnergyChanged_Raised_TriggersRefreshUpdatesOverlay()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility(energyCost: 80f);
            var energy  = MakeFullEnergy(); // 100 energy
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var overlay = new GameObject("UnavailableOverlay");
            overlay.SetActive(false);

            SetField(ctrl, "_ability",             ability);
            SetField(ctrl, "_energySystem",        energy);
            SetField(ctrl, "_onEnergyChanged",     channel);
            SetField(ctrl, "_unavailableOverlay",  overlay);

            go.SetActive(true);           // Refresh → energy 100 >= 80 → overlay hidden
            bool hiddenBefore = !overlay.activeSelf;

            energy.Consume(50f);          // energy drops to 50 < 80
            overlay.SetActive(false);     // force-hide to test event path
            channel.Raise();              // triggers Refresh → energy 50 < 80 → overlay shown

            bool shownAfter = overlay.activeSelf;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(overlay);

            Assert.IsTrue(hiddenBefore, "Overlay must be hidden initially with sufficient energy.");
            Assert.IsTrue(shownAfter,   "Overlay must appear after energy drops below cost on channel raise.");
        }

        // ── Null UI refs — no throw ───────────────────────────────────────────

        [Test]
        public void Refresh_NullUiRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<AbilitySlotHUDController>();
            var ability = MakeAbility();
            var energy  = MakeFullEnergy();
            SetField(ctrl, "_ability",      ability);
            SetField(ctrl, "_energySystem", energy);
            // All Text / Slider / GameObject UI refs remain null

            Assert.DoesNotThrow(() => go.SetActive(true),
                "Refresh must not throw when all UI refs are null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(energy);
        }

        // ── OnAbilityFailed — no throw ────────────────────────────────────────

        [Test]
        public void OnAbilityFailed_Raised_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<AbilitySlotHUDController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctrl, "_onAbilityFailed", channel);
            go.SetActive(true);

            Assert.DoesNotThrow(() => channel.Raise(),
                "Raising _onAbilityFailed must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }
    }
}
