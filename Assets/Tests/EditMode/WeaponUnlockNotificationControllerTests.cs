using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WeaponUnlockNotificationController"/> (T178).
    ///
    /// Tests cover:
    ///   OnEnable / OnDisable null-safety (×2).
    ///   Null channel does not throw (×2).
    ///   OnDisable unregisters from _onPrestige channel.
    ///   Fresh instance public properties are null / default (×2).
    ///   OnPrestige with null _unlockConfig — does not throw.
    ///   OnPrestige with null _prestigeSystem — does not throw.
    ///   OnPrestige with no newly-unlocked types — panel stays hidden.
    ///   OnPrestige with a newly-unlocked type — raises _onWeaponTypeUnlocked event.
    ///   OnPrestige with a newly-unlocked type — activates notification panel.
    ///   Tick after DisplayDuration expires — hides the notification panel.
    ///
    /// Total: 13 tests.
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

        /// <summary>Creates a WeaponTypeUnlockConfig with specific requirements.</summary>
        private static WeaponTypeUnlockConfig CreateConfig(
            int physical = 0, int energy = 0, int thermal = 0, int shock = 0)
        {
            var cfg = ScriptableObject.CreateInstance<WeaponTypeUnlockConfig>();
            SetField(cfg, "_physicalRequiredPrestige", physical);
            SetField(cfg, "_energyRequiredPrestige",   energy);
            SetField(cfg, "_thermalRequiredPrestige",  thermal);
            SetField(cfg, "_shockRequiredPrestige",    shock);
            return cfg;
        }

        /// <summary>Creates a PrestigeSystemSO snapshotted to a given count.</summary>
        private static PrestigeSystemSO CreatePrestige(int count, int max = 10)
        {
            var p = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            SetField(p, "_maxPrestigeRank", max);
            p.LoadSnapshot(count);
            return p;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Null-safety lifecycle tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            SetField(ctl, "_onPrestige", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with null _onPrestige must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            SetField(ctl, "_onPrestige", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with null _onPrestige must not throw.");
            Object.DestroyImmediate(go);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Unsubscription test
        // ══════════════════════════════════════════════════════════════════════

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

            Assert.AreEqual(1, callCount,
                "After OnDisable the controller must have unregistered from _onPrestige.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Fresh-instance property tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void FreshInstance_UnlockConfig_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            Assert.IsNull(ctl.UnlockConfig, "UnlockConfig should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_DisplayDuration_DefaultIs2Point5()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            Assert.AreEqual(2.5f, ctl.DisplayDuration, 0.001f,
                "DisplayDuration should default to 2.5 seconds.");
            Object.DestroyImmediate(go);
        }

        // ══════════════════════════════════════════════════════════════════════
        // OnPrestige logic tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void OnPrestige_NullUnlockConfig_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            SetField(ctl, "_unlockConfig",  null);
            SetField(ctl, "_prestigeSystem", CreatePrestige(1));
            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnPrestige"),
                "OnPrestige with null _unlockConfig must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnPrestige_NullPrestigeSystem_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponUnlockNotificationController>();
            SetField(ctl, "_unlockConfig",   CreateConfig(energy: 1));
            SetField(ctl, "_prestigeSystem", null);
            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnPrestige"),
                "OnPrestige with null _prestigeSystem must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnPrestige_NoNewUnlocks_PanelStaysHidden()
        {
            // Energy requires 2; prestige goes from 0 → 1 — not enough to unlock.
            var go       = new GameObject();
            var ctl      = go.AddComponent<WeaponUnlockNotificationController>();
            var panel    = new GameObject("Panel");
            var prestige = CreatePrestige(count: 0);

            SetField(ctl, "_unlockConfig",      CreateConfig(energy: 2));
            SetField(ctl, "_prestigeSystem",    prestige);
            SetField(ctl, "_notificationPanel", panel);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable"); // snapshots count = 0

            // Simulate prestige to count 1 (still below threshold of 2).
            prestige.LoadSnapshot(1);
            InvokePrivate(ctl, "OnPrestige");

            Assert.IsFalse(panel.activeSelf,
                "Panel must stay hidden when no new types are unlocked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnPrestige_NewlyUnlocked_RaisesOnWeaponTypeUnlockedEvent()
        {
            // Energy requires 1; prestige goes from 0 → 1 — newly unlocked.
            var go       = new GameObject();
            var ctl      = go.AddComponent<WeaponUnlockNotificationController>();
            var evtOut   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var prestige = CreatePrestige(count: 0);

            SetField(ctl, "_unlockConfig",         CreateConfig(energy: 1));
            SetField(ctl, "_prestigeSystem",       prestige);
            SetField(ctl, "_onWeaponTypeUnlocked", evtOut);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable"); // snapshots count = 0

            int raised = 0;
            evtOut.RegisterCallback(() => raised++);

            prestige.LoadSnapshot(1);
            InvokePrivate(ctl, "OnPrestige");

            Assert.AreEqual(1, raised,
                "_onWeaponTypeUnlocked must be raised once for the newly-unlocked Energy type.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evtOut);
        }

        [Test]
        public void OnPrestige_NewlyUnlocked_ShowsNotificationPanel()
        {
            // Energy requires 1; prestige goes from 0 → 1 — newly unlocked.
            var go       = new GameObject();
            var ctl      = go.AddComponent<WeaponUnlockNotificationController>();
            var panel    = new GameObject("Panel");
            var prestige = CreatePrestige(count: 0);

            SetField(ctl, "_unlockConfig",      CreateConfig(energy: 1));
            SetField(ctl, "_prestigeSystem",    prestige);
            SetField(ctl, "_notificationPanel", panel);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable"); // snapshots count = 0

            prestige.LoadSnapshot(1);
            InvokePrivate(ctl, "OnPrestige");

            Assert.IsTrue(panel.activeSelf,
                "Notification panel must be activated when a new weapon type is unlocked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Tick_AfterDisplayDuration_HidesPanel()
        {
            // Show a notification then manually expire the timer via Update/Tick.
            var go       = new GameObject();
            var ctl      = go.AddComponent<WeaponUnlockNotificationController>();
            var panel    = new GameObject("Panel");
            var prestige = CreatePrestige(count: 0);

            SetField(ctl, "_unlockConfig",      CreateConfig(energy: 1));
            SetField(ctl, "_prestigeSystem",    prestige);
            SetField(ctl, "_notificationPanel", panel);
            SetField(ctl, "_displayDuration",   1.0f);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");

            prestige.LoadSnapshot(1);
            InvokePrivate(ctl, "OnPrestige");

            // Panel should be active now.
            Assert.IsTrue(panel.activeSelf, "Panel should be active after unlock.");

            // Drain the timer: pass a deltaTime large enough to exceed _displayDuration.
            ctl.Tick(999f);

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden after display timer expires.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
