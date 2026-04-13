using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DamageTypeEffectivenessHUDController"/>.
    ///
    /// Covers:
    ///   • DisplayTimer is 0 on a fresh instance.
    ///   • OnEnable with null channel does not throw.
    ///   • OnDisable with null channel does not throw.
    ///   • OnDisable unregisters the callback (raising channel after disable → no-op).
    ///   • OnDamageTaken with null config does not throw.
    ///   • OnDamageTaken with null resistance and null vulnerability → ratio 1 → sets timer.
    ///   • OnDamageTaken with vulnerability ×2 → ratio > 1 → sets timer to DisplayDuration.
    ///   • Tick decrements DisplayTimer.
    ///   • EffectivenessConfig property returns assigned config.
    /// </summary>
    public class DamageTypeEffectivenessHUDControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method, object[] args = null)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args ?? System.Array.Empty<object>());
        }

        private static DamageTypeEffectivenessConfig CreateEffectivenessConfig(
            float effectiveThreshold = 1.1f, float resistedThreshold = 0.9f,
            float displayDuration = 2f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            SetField(cfg, "_effectiveThreshold", effectiveThreshold);
            SetField(cfg, "_resistedThreshold",  resistedThreshold);
            SetField(cfg, "_displayDuration",    displayDuration);
            return cfg;
        }

        private static DamageVulnerabilityConfig CreateVulnConfig(
            float physical = 1f, float energy = 1f,
            float thermal  = 1f, float shock  = 1f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageVulnerabilityConfig>();
            SetField(cfg, "_physicalMultiplier", physical);
            SetField(cfg, "_energyMultiplier",   energy);
            SetField(cfg, "_thermalMultiplier",  thermal);
            SetField(cfg, "_shockMultiplier",    shock);
            return cfg;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void DisplayTimer_Default_IsZero()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<DamageTypeEffectivenessHUDController>();
            Assert.AreEqual(0f, hud.DisplayTimer, 0.0001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<DamageTypeEffectivenessHUDController>();
            SetField(hud, "_onDamageTaken", null);
            Assert.DoesNotThrow(() => InvokePrivate(hud, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<DamageTypeEffectivenessHUDController>();
            SetField(hud, "_onDamageTaken", null);
            Assert.DoesNotThrow(() => InvokePrivate(hud, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_UnregistersCallback()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeEffectivenessHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateEffectivenessConfig(displayDuration: 2f);
            var panel   = new GameObject();

            SetField(hud, "_onDamageTaken",       channel);
            SetField(hud, "_effectivenessConfig", cfg);
            SetField(hud, "_outcomePanel",        panel);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");
            InvokePrivate(hud, "OnDisable");

            panel.SetActive(false);
            // After unregistering, raising the event must NOT re-activate the panel.
            channel.Raise(new DamageInfo(10f));
            Assert.IsFalse(panel.activeSelf, "Panel must stay hidden after unsubscribe.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void OnDamageTaken_NullConfig_DoesNotThrow()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeEffectivenessHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            SetField(hud, "_onDamageTaken",       channel);
            SetField(hud, "_effectivenessConfig", null);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");

            Assert.DoesNotThrow(() => channel.Raise(new DamageInfo(20f)));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnDamageTaken_NullResistanceNullVulnerability_SetsTimerToDisplayDuration()
        {
            // ratio = (1-0) × 1 = 1 → Neutral; DisplayTimer should still be set.
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeEffectivenessHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateEffectivenessConfig(displayDuration: 2f);

            SetField(hud, "_onDamageTaken",       channel);
            SetField(hud, "_effectivenessConfig", cfg);
            SetField(hud, "_resistanceConfig",    null);
            SetField(hud, "_vulnerabilityConfig", null);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");
            channel.Raise(new DamageInfo(20f, "", Vector3.zero, null, DamageType.Physical));

            Assert.AreEqual(2f, hud.DisplayTimer, 0.001f,
                "DisplayTimer should be set to DisplayDuration on a valid hit.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void OnDamageTaken_VulnerabilityMultiplierTwo_SetsTimerToDisplayDuration()
        {
            // vulnerability ×2 → ratio = 2 > 1.1 threshold → Effective.
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeEffectivenessHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateEffectivenessConfig(effectiveThreshold: 1.1f, displayDuration: 1.5f);
            var vuln    = CreateVulnConfig(energy: 2f);

            SetField(hud, "_onDamageTaken",       channel);
            SetField(hud, "_effectivenessConfig", cfg);
            SetField(hud, "_vulnerabilityConfig", vuln);
            SetField(hud, "_resistanceConfig",    null);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");
            channel.Raise(new DamageInfo(10f, "", Vector3.zero, null, DamageType.Energy));

            Assert.AreEqual(1.5f, hud.DisplayTimer, 0.001f,
                "Timer should be set to DisplayDuration on Effective hit.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(vuln);
        }

        [Test]
        public void Tick_DecrementsDisplayTimer()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<DamageTypeEffectivenessHUDController>();
            SetField(hud, "_displayTimer", 2f);

            hud.Tick(0.5f);

            Assert.AreEqual(1.5f, hud.DisplayTimer, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EffectivenessConfig_Property_ReturnsAssignedConfig()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<DamageTypeEffectivenessHUDController>();
            var cfg = CreateEffectivenessConfig();
            SetField(hud, "_effectivenessConfig", cfg);

            Assert.AreSame(cfg, hud.EffectivenessConfig);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }
    }
}
