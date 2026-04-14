using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the M28 WeaponUnlockNotificationController (T177).
    ///
    /// WeaponUnlockNotificationControllerTests (14):
    ///   Fresh instance — all refs null — does not throw (constructor safety).
    ///   Fresh instance — UnlockConfig property is null.
    ///   Fresh instance — PrestigeSystem property is null.
    ///   Fresh instance — NotificationDuration defaults to 3.
    ///   OnEnable with all-null refs does not throw.
    ///   OnDisable with all-null refs does not throw.
    ///   OnDisable with null channel does not throw.
    ///   OnDisable unregisters from _onPrestige channel.
    ///   OnPrestige with null _unlockConfig does not throw.
    ///   OnPrestige with null _prestigeSystem does not throw.
    ///   OnPrestige — no new unlocks — panel stays hidden.
    ///   OnPrestige — newly unlocked type — panel becomes active.
    ///   OnPrestige — newly unlocked type — fires _onNewTypeUnlocked.
    ///   Update — displayTimer expires — hides panel.
    ///
    /// Total: 14 new EditMode tests.
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class WeaponUnlockNotificationControllerTests
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

        private static WeaponTypeUnlockConfig CreateConfig(
            int physical = 0, int energy = 1, int thermal = 4, int shock = 7)
        {
            var cfg = ScriptableObject.CreateInstance<WeaponTypeUnlockConfig>();
            SetField(cfg, "_physicalRequiredPrestige", physical);
            SetField(cfg, "_energyRequiredPrestige",   energy);
            SetField(cfg, "_thermalRequiredPrestige",  thermal);
            SetField(cfg, "_shockRequiredPrestige",    shock);
            return cfg;
        }

        private static PrestigeSystemSO CreatePrestige(int count, int max = 10)
        {
            var p = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            SetField(p, "_maxPrestigeRank", max);
            p.LoadSnapshot(count);
            return p;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            Assert.DoesNotThrow(() => go.AddComponent<WeaponUnlockNotificationController>(),
                "Adding WeaponUnlockNotificationController with no refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_UnlockConfig_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            Assert.IsNull(ctl.UnlockConfig, "UnlockConfig should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_PrestigeSystem_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            Assert.IsNull(ctl.PrestigeSystem, "PrestigeSystem should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_NotificationDuration_IsThree()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            Assert.AreEqual(3f, ctl.NotificationDuration, 1e-5f,
                "NotificationDuration should default to 3 seconds.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            SetField(ctl, "_onPrestige", null);
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with null _onPrestige must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_UnregistersFromOnPrestige()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<WeaponUnlockNotificationController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctl, "_onPrestige", channel);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            InvokePrivate(ctl, "OnDisable");
            channel.Raise();

            // Only the manual callback fires; the controller's delegate was unregistered.
            Assert.AreEqual(1, callCount,
                "After OnDisable, the controller must have unregistered its OnPrestige delegate.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnPrestige_NullUnlockConfig_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            SetField(ctl, "_unlockConfig", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnPrestige"),
                "OnPrestige with null _unlockConfig must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnPrestige_NullPrestigeSystem_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            var cfg = CreateConfig();
            SetField(ctl, "_unlockConfig",   cfg);
            SetField(ctl, "_prestigeSystem", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnPrestige"),
                "OnPrestige with null _prestigeSystem must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void OnPrestige_NoNewUnlocks_PanelRemainsHidden()
        {
            // Physical requires 0 — already unlocked at count 0.
            // Start at prestige 0, advance to 0 again → nothing newly unlocks.
            var go        = new GameObject();
            var panelGo   = new GameObject();
            var ctl       = go.AddComponent<WeaponUnlockNotificationController>();
            var cfg       = CreateConfig(physical: 0, energy: 5, thermal: 5, shock: 5);
            var prestige  = CreatePrestige(count: 0);

            panelGo.SetActive(false);
            SetField(ctl, "_unlockConfig",      cfg);
            SetField(ctl, "_prestigeSystem",    prestige);
            SetField(ctl, "_notificationPanel", panelGo);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");   // snapshots previousCount = 0
            InvokePrivate(ctl, "OnPrestige"); // newCount still 0 → no change

            Assert.IsFalse(panelGo.activeSelf,
                "Panel must remain hidden when no type transitions from locked to unlocked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void OnPrestige_NewlyUnlockedType_ShowsPanel()
        {
            // Energy requires 1; player moves from prestige 0 to prestige 1 → Energy unlocks.
            var go       = new GameObject();
            var panelGo  = new GameObject();
            var ctl      = go.AddComponent<WeaponUnlockNotificationController>();
            var cfg      = CreateConfig(physical: 0, energy: 1, thermal: 4, shock: 7);
            var prestige = CreatePrestige(count: 1);

            panelGo.SetActive(false);
            SetField(ctl, "_unlockConfig",      cfg);
            SetField(ctl, "_prestigeSystem",    prestige);
            SetField(ctl, "_notificationPanel", panelGo);

            InvokePrivate(ctl, "Awake");

            // Simulate previous count = 0 (before this prestige event).
            SetField(ctl, "_previousPrestigeCount", 0);

            InvokePrivate(ctl, "OnPrestige");

            Assert.IsTrue(panelGo.activeSelf,
                "Panel must be shown when Energy transitions from locked to unlocked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void OnPrestige_NewlyUnlockedType_FiresOnNewTypeUnlocked()
        {
            var go       = new GameObject();
            var ctl      = go.AddComponent<WeaponUnlockNotificationController>();
            var cfg      = CreateConfig(physical: 0, energy: 1, thermal: 4, shock: 7);
            var prestige = CreatePrestige(count: 1);
            var channel  = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(ctl, "_unlockConfig",      cfg);
            SetField(ctl, "_prestigeSystem",    prestige);
            SetField(ctl, "_onNewTypeUnlocked", channel);

            InvokePrivate(ctl, "Awake");
            SetField(ctl, "_previousPrestigeCount", 0);

            int fireCount = 0;
            channel.RegisterCallback(() => fireCount++);

            InvokePrivate(ctl, "OnPrestige");

            Assert.AreEqual(1, fireCount,
                "_onNewTypeUnlocked must fire exactly once when Energy unlocks.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(prestige);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Tick_DisplayTimerExpires_HidesPanel()
        {
            var go      = new GameObject();
            var panelGo = new GameObject();
            var ctl     = go.AddComponent<WeaponUnlockNotificationController>();

            panelGo.SetActive(true);
            SetField(ctl, "_notificationPanel", panelGo);
            // Start with a 1-second timer and advance by 2 seconds → panel should hide.
            SetField(ctl, "_displayTimer", 1f);

            ctl.Tick(2f);

            Assert.IsFalse(panelGo.activeSelf,
                "Panel must be hidden when the display timer expires via Tick().");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }
    }
}
