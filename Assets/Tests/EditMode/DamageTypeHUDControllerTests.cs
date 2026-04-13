using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DamageTypeHUDController"/>.
    ///
    /// Covers:
    ///   • DisplayTimer is 0 on a fresh instance (panel hidden initially).
    ///   • OnEnable subscribes channel; OnDisable unsubscribes.
    ///   • Null channel in OnEnable/OnDisable — no exception.
    ///   • OnDamageTaken with null iconConfig — panel not toggled, no crash.
    ///   • OnDamageTaken sets _typeLabel text from iconConfig.GetLabel.
    ///   • OnDamageTaken sets _typeIcon color from iconConfig.GetColor.
    ///   • OnDamageTaken activates _indicatorPanel.
    ///   • OnDamageTaken sets DisplayTimer to iconConfig.DisplayDuration.
    ///   • Tick decrements DisplayTimer.
    ///   • Tick reaching zero hides _indicatorPanel.
    ///   • OnDisable hides panel and unregisters channel.
    ///   • IconConfig property returns assigned config.
    /// </summary>
    public class DamageTypeHUDControllerTests
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

        private static DamageTypeIconConfig CreateConfig(float duration = 2f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeIconConfig>();
            SetField(cfg, "_physicalColor",   Color.white);
            SetField(cfg, "_physicalLabel",   "PHYSICAL");
            SetField(cfg, "_energyColor",     Color.cyan);
            SetField(cfg, "_energyLabel",     "ENERGY");
            SetField(cfg, "_thermalColor",    new Color(1f, 0.45f, 0f));
            SetField(cfg, "_thermalLabel",    "THERMAL");
            SetField(cfg, "_shockColor",      Color.yellow);
            SetField(cfg, "_shockLabel",      "SHOCK");
            SetField(cfg, "_displayDuration", duration);
            return cfg;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void DisplayTimer_Default_IsZero()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<DamageTypeHUDController>();
            Assert.AreEqual(0f, hud.DisplayTimer, 0.0001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<DamageTypeHUDController>();
            SetField(hud, "_onDamageTaken", null);
            Assert.DoesNotThrow(() => InvokePrivate(hud, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<DamageTypeHUDController>();
            SetField(hud, "_onDamageTaken", null);
            Assert.DoesNotThrow(() => InvokePrivate(hud, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDamageTaken_NullConfig_DoesNotThrow()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            SetField(hud, "_iconConfig",    null);
            SetField(hud, "_onDamageTaken", channel);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");

            Assert.DoesNotThrow(() =>
                channel.Raise(new DamageInfo(10f, "", Vector3.zero, null, DamageType.Energy)));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnDamageTaken_SetsTypeLabelText()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateConfig();
            var label   = new GameObject().AddComponent<Text>();

            SetField(hud, "_iconConfig",    cfg);
            SetField(hud, "_onDamageTaken", channel);
            SetField(hud, "_typeLabel",     label);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");

            channel.Raise(new DamageInfo(20f, "", Vector3.zero, null, DamageType.Thermal));

            Assert.AreEqual("THERMAL", label.text,
                "Label should display the thermal type string.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(label.gameObject);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void OnDamageTaken_SetsTypeIconColor()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateConfig();
            var icon    = new GameObject().AddComponent<Image>();

            SetField(hud, "_iconConfig",    cfg);
            SetField(hud, "_onDamageTaken", channel);
            SetField(hud, "_typeIcon",      icon);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");

            channel.Raise(new DamageInfo(15f, "", Vector3.zero, null, DamageType.Shock));

            Assert.AreEqual(Color.yellow, icon.color,
                "Icon color should be yellow for Shock damage type.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(icon.gameObject);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void OnDamageTaken_ActivatesIndicatorPanel()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateConfig();
            var panel   = new GameObject();
            panel.SetActive(false);

            SetField(hud, "_iconConfig",       cfg);
            SetField(hud, "_onDamageTaken",    channel);
            SetField(hud, "_indicatorPanel",   panel);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");

            channel.Raise(new DamageInfo(5f, "", Vector3.zero, null, DamageType.Energy));

            Assert.IsTrue(panel.activeSelf, "Panel should be activated on damage.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void OnDamageTaken_SetsDisplayTimerToDisplayDuration()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateConfig(duration: 2.5f);

            SetField(hud, "_iconConfig",    cfg);
            SetField(hud, "_onDamageTaken", channel);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");

            channel.Raise(new DamageInfo(10f, "", Vector3.zero, null, DamageType.Physical));

            Assert.AreEqual(2.5f, hud.DisplayTimer, 0.001f,
                "DisplayTimer should be set to iconConfig.DisplayDuration after a hit.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Tick_DescendsDisplayTimer()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateConfig(duration: 3f);

            SetField(hud, "_iconConfig",    cfg);
            SetField(hud, "_onDamageTaken", channel);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");
            channel.Raise(new DamageInfo(10f, "", Vector3.zero, null, DamageType.Energy));

            hud.Tick(1f);   // 3f → 2f

            Assert.AreEqual(2f, hud.DisplayTimer, 0.001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Tick_ReachingZero_HidesPanel()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateConfig(duration: 1f);
            var panel   = new GameObject();

            SetField(hud, "_iconConfig",     cfg);
            SetField(hud, "_onDamageTaken",  channel);
            SetField(hud, "_indicatorPanel", panel);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");
            channel.Raise(new DamageInfo(10f, "", Vector3.zero, null, DamageType.Thermal));

            Assert.IsTrue(panel.activeSelf, "Panel active after hit");
            hud.Tick(1.1f);  // timer expires
            Assert.IsFalse(panel.activeSelf, "Panel should deactivate when timer expires.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void OnDisable_HidesPanelAndUnsubscribes()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<DamageTypeHUDController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            var cfg     = CreateConfig(duration: 5f);
            var panel   = new GameObject();

            SetField(hud, "_iconConfig",     cfg);
            SetField(hud, "_onDamageTaken",  channel);
            SetField(hud, "_indicatorPanel", panel);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");
            channel.Raise(new DamageInfo(10f, "", Vector3.zero, null, DamageType.Shock));

            InvokePrivate(hud, "OnDisable");

            Assert.IsFalse(panel.activeSelf, "OnDisable should hide the panel.");
            // After unsubscribe, raising the channel should NOT re-activate the panel.
            channel.Raise(new DamageInfo(10f, "", Vector3.zero, null, DamageType.Shock));
            Assert.IsFalse(panel.activeSelf, "Panel must stay hidden after unsubscribe.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void IconConfig_Property_ReturnsAssignedConfig()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<DamageTypeHUDController>();
            var cfg = CreateConfig();
            SetField(hud, "_iconConfig", cfg);

            Assert.AreSame(cfg, hud.IconConfig);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }
    }
}
