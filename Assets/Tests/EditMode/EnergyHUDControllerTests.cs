using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="EnergyHUDController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs — no throw.
    ///   • OnEnable / OnDisable with null event channel — no throw.
    ///   • OnDisable unregisters the refresh callback.
    ///   • Refresh with null _energySystem hides the panel.
    ///   • Refresh with valid _energySystem shows the panel.
    ///   • FormatEnergy: full pool, partial pool, zero energy.
    ///   • _onEnergyChanged raised → Refresh() runs (panel shown).
    ///   • Refresh with null _energyPanel does not throw.
    /// </summary>
    public class EnergyHUDControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static EnergyHUDController MakeController(out GameObject go)
        {
            go = new GameObject("EnergyHUDTest");
            go.SetActive(false); // prevent OnEnable before wiring
            return go.AddComponent<EnergyHUDController>();
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
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<EnergyHUDController>();
            var energy = MakeFullEnergy();
            SetField(ctrl, "_energySystem", energy);
            // _onEnergyChanged remains null
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _onEnergyChanged must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<EnergyHUDController>();
            var energy = MakeFullEnergy();
            SetField(ctrl, "_energySystem", energy);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null _onEnergyChanged must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
        }

        // ── OnDisable unregisters callback ────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersCallback()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<EnergyHUDController>();
            var energy  = MakeFullEnergy();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var panel   = new GameObject("Panel");

            SetField(ctrl, "_energySystem",      energy);
            SetField(ctrl, "_onEnergyChanged",   channel);
            SetField(ctrl, "_energyPanel",       panel);
            panel.SetActive(false);

            go.SetActive(true);   // subscribes
            go.SetActive(false);  // unsubscribes

            panel.SetActive(false);
            energy.Consume(10f);  // triggers channel.Raise via energy's own _onEnergyChanged
            // But we need to raise channel directly to test unregistration
            channel.Raise();      // should NOT call Refresh (unsubscribed)

            // Panel stays false (no Refresh ran after unsubscribe)
            bool panelActive = panel.activeSelf;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(panel);

            Assert.IsFalse(panelActive,
                "After OnDisable, raising the channel must not trigger Refresh " +
                "(callback must be unregistered).");
        }

        // ── Refresh — hide when no energy system ─────────────────────────────

        [Test]
        public void Refresh_NullEnergySystem_HidesPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<EnergyHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_energyPanel", panel);
            // _energySystem remains null

            go.SetActive(true); // OnEnable → Refresh → null → Hide

            bool result = panel.activeSelf;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Assert.IsFalse(result,
                "_energyPanel must be hidden when _energySystem is null.");
        }

        // ── Refresh — show when energy system present ─────────────────────────

        [Test]
        public void Refresh_WithEnergySystem_ShowsPanel()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<EnergyHUDController>();
            var energy = MakeFullEnergy();
            var panel  = new GameObject("Panel");
            panel.SetActive(false);
            SetField(ctrl, "_energySystem", energy);
            SetField(ctrl, "_energyPanel",  panel);

            go.SetActive(true); // OnEnable → Refresh → show

            bool result = panel.activeSelf;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
            Object.DestroyImmediate(panel);
            Assert.IsTrue(result,
                "_energyPanel must be shown when _energySystem is assigned.");
        }

        // ── FormatEnergy ──────────────────────────────────────────────────────

        [Test]
        public void FormatEnergy_FullPool_ShowsMaxOverMax()
        {
            string result = EnergyHUDController.FormatEnergy(100f, 100f);
            Assert.AreEqual("100/100", result,
                "FormatEnergy(100,100) must return '100/100'.");
        }

        [Test]
        public void FormatEnergy_PartialPool_RoundsToInt()
        {
            string result = EnergyHUDController.FormatEnergy(73.6f, 100f);
            Assert.AreEqual("74/100", result,
                "FormatEnergy must round current energy to the nearest integer.");
        }

        [Test]
        public void FormatEnergy_ZeroEnergy_ShowsZeroSlashMax()
        {
            string result = EnergyHUDController.FormatEnergy(0f, 100f);
            Assert.AreEqual("0/100", result,
                "FormatEnergy(0,100) must return '0/100'.");
        }

        // ── Event channel integration ─────────────────────────────────────────

        [Test]
        public void OnEnergyChangedRaised_TriggersRefresh_ShowsPanel()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<EnergyHUDController>();
            var energy  = MakeFullEnergy();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var panel   = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_energySystem",    energy);
            SetField(ctrl, "_onEnergyChanged", channel);
            SetField(ctrl, "_energyPanel",     panel);

            go.SetActive(true);       // OnEnable: subscribe + Refresh (already shows panel)
            panel.SetActive(false);   // manually hide to test event path
            channel.Raise();          // should trigger Refresh → show panel

            bool result = panel.activeSelf;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(panel);
            Assert.IsTrue(result,
                "Raising _onEnergyChanged must trigger Refresh and show the panel.");
        }

        // ── Null UI ref safety ────────────────────────────────────────────────

        [Test]
        public void Refresh_NullEnergyPanel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<EnergyHUDController>();
            var energy = MakeFullEnergy();
            SetField(ctrl, "_energySystem", energy);
            // _energyPanel remains null

            Assert.DoesNotThrow(() => go.SetActive(true),
                "Refresh must not throw when _energyPanel is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(energy);
        }
    }
}
