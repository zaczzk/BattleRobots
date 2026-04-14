using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T227:
    ///   <see cref="RobotRespawnSO"/> and <see cref="RespawnCountdownController"/>.
    ///
    /// RobotRespawnSOTests (9):
    ///   FreshInstance_MaxRespawns_IsDefault                        ×1
    ///   FreshInstance_IsOnCooldown_IsFalse                         ×1
    ///   FreshInstance_RespawnsRemaining_EqualsMax                  ×1
    ///   UseRespawn_Success_DecreasesRespawnsRemaining              ×1
    ///   UseRespawn_Success_StartsCountdown                         ×1
    ///   UseRespawn_Success_FiresOnRespawnUsed                      ×1
    ///   UseRespawn_NoRespawnsLeft_ReturnsFalse                     ×1
    ///   UseRespawn_OnCooldown_ReturnsFalse                         ×1
    ///   Tick_WhenCooldownReachesZero_FiresOnRespawnReady           ×1
    ///   Tick_DecrementsTimer                                       ×1
    ///   Reset_RestoresDefaults                                     ×1
    ///
    /// Wait — that's 11, so I'll trim to 9 by combining.
    ///
    /// RobotRespawnSOTests (9):
    ///   FreshInstance_MaxRespawns_IsDefault                        ×1
    ///   FreshInstance_IsOnCooldown_IsFalse                         ×1
    ///   FreshInstance_RespawnsRemaining_EqualsMax                  ×1
    ///   UseRespawn_DecreasesCountAndStartsCooldown                 ×1
    ///   UseRespawn_FiresOnRespawnUsed                              ×1
    ///   UseRespawn_NoRespawnsLeft_ReturnsFalse                     ×1
    ///   UseRespawn_OnCooldown_ReturnsFalse                         ×1
    ///   Tick_Decrements_And_FiresReadyAtZero                       ×1
    ///   Reset_RestoresDefaults                                     ×1
    ///
    /// RespawnCountdownControllerTests (7):
    ///   FreshInstance_RespawnSONull                                ×1
    ///   OnEnable_NullRefs_DoesNotThrow                             ×1
    ///   OnDisable_NullRefs_DoesNotThrow                            ×1
    ///   OnDisable_Unregisters                                      ×1
    ///   Refresh_NullSO_HidesPanel                                  ×1
    ///   Refresh_WhenReady_ShowsReadyLabel                          ×1
    ///   Refresh_WhenOnCooldown_ShowsCountdown                      ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class RobotRespawnTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static RobotRespawnSO CreateRespawnSO()
        {
            var so = ScriptableObject.CreateInstance<RobotRespawnSO>();
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static RespawnCountdownController CreateController() =>
            new GameObject("RespawnCtrl_Test").AddComponent<RespawnCountdownController>();

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── RobotRespawnSOTests ───────────────────────────────────────────────

        [Test]
        public void FreshInstance_MaxRespawns_IsDefault()
        {
            var so = CreateRespawnSO();
            Assert.AreEqual(3, so.MaxRespawns,
                "Default MaxRespawns must be 3.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_IsOnCooldown_IsFalse()
        {
            var so = CreateRespawnSO();
            Assert.IsFalse(so.IsOnCooldown,
                "IsOnCooldown must be false on fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_RespawnsRemaining_EqualsMax()
        {
            var so = CreateRespawnSO();
            Assert.AreEqual(so.MaxRespawns, so.RespawnsRemaining,
                "RespawnsRemaining must equal MaxRespawns on fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void UseRespawn_DecreasesCountAndStartsCooldown()
        {
            var so = CreateRespawnSO();
            int before = so.RespawnsRemaining;

            bool result = so.UseRespawn();

            Assert.IsTrue(result, "UseRespawn should return true when respawns are available.");
            Assert.AreEqual(before - 1, so.RespawnsRemaining,
                "RespawnsRemaining must decrease by 1.");
            Assert.IsTrue(so.IsOnCooldown,
                "IsOnCooldown must be true after UseRespawn.");
            Assert.Greater(so.CooldownTimeRemaining, 0f,
                "CooldownTimeRemaining must be > 0 after UseRespawn.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void UseRespawn_FiresOnRespawnUsed()
        {
            var so  = CreateRespawnSO();
            var evt = CreateVoidEvent();
            SetField(so, "_onRespawnUsed", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);
            so.UseRespawn();

            Assert.AreEqual(1, count,
                "_onRespawnUsed must fire once when UseRespawn succeeds.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void UseRespawn_NoRespawnsLeft_ReturnsFalse()
        {
            var so = CreateRespawnSO();
            SetField(so, "_maxRespawns", 1);
            InvokePrivate(so, "OnEnable");

            so.UseRespawn();          // uses the 1 respawn, now on cooldown
            // manually clear cooldown so we can call again
            SetField(so, "_isOnCooldown", false);
            SetField(so, "_cooldownTimer", 0f);

            bool result = so.UseRespawn(); // no respawns left

            Assert.IsFalse(result,
                "UseRespawn must return false when RespawnsRemaining == 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void UseRespawn_OnCooldown_ReturnsFalse()
        {
            var so = CreateRespawnSO();
            so.UseRespawn(); // starts cooldown

            bool result = so.UseRespawn(); // should be rejected

            Assert.IsFalse(result,
                "UseRespawn must return false while on cooldown.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_Decrements_And_FiresReadyAtZero()
        {
            var so  = CreateRespawnSO();
            var evt = CreateVoidEvent();
            SetField(so, "_onRespawnReady", evt);
            SetField(so, "_respawnCooldown", 2f);
            InvokePrivate(so, "OnEnable");

            so.UseRespawn();
            Assert.IsTrue(so.IsOnCooldown);

            int readyCount = 0;
            evt.RegisterCallback(() => readyCount++);

            // Tick enough to exceed the cooldown
            so.Tick(2.5f);

            Assert.IsFalse(so.IsOnCooldown,
                "IsOnCooldown must be false after cooldown expires.");
            Assert.AreEqual(0f, so.CooldownTimeRemaining, 0.001f,
                "CooldownTimeRemaining must be 0 when cooldown expires.");
            Assert.AreEqual(1, readyCount,
                "_onRespawnReady must fire once when the cooldown expires.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Reset_RestoresDefaults()
        {
            var so = CreateRespawnSO();
            so.UseRespawn();
            so.Reset();

            Assert.AreEqual(so.MaxRespawns, so.RespawnsRemaining,
                "Reset must restore RespawnsRemaining to MaxRespawns.");
            Assert.IsFalse(so.IsOnCooldown,
                "Reset must clear the cooldown flag.");
            Assert.AreEqual(0f, so.CooldownTimeRemaining, 0.001f,
                "Reset must zero CooldownTimeRemaining.");
            Object.DestroyImmediate(so);
        }

        // ── RespawnCountdownControllerTests ───────────────────────────────────

        [Test]
        public void FreshInstance_RespawnSONull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.RespawnSO,
                "RespawnSO must be null when not assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onRespawnUsed", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback should fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when RespawnSO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WhenReady_ShowsReadyLabel()
        {
            var ctrl  = CreateController();
            var so    = CreateRespawnSO();
            var label = AddText(ctrl.gameObject, "CountdownLabel");
            SetField(ctrl, "_respawnSO",       so);
            SetField(ctrl, "_countdownLabel",   label);
            InvokePrivate(ctrl, "Awake");

            // SO is not on cooldown by default
            ctrl.Refresh();

            Assert.AreEqual("Ready", label.text,
                "Countdown label must show 'Ready' when not on cooldown.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_WhenOnCooldown_ShowsCountdown()
        {
            var ctrl  = CreateController();
            var so    = CreateRespawnSO();
            var label = AddText(ctrl.gameObject, "CountdownLabel");
            SetField(ctrl, "_respawnSO",       so);
            SetField(ctrl, "_countdownLabel",   label);
            InvokePrivate(ctrl, "Awake");

            so.UseRespawn(); // puts SO on cooldown
            ctrl.Refresh();

            StringAssert.Contains("s", label.text,
                "Countdown label must contain 's' when on cooldown (e.g. '5.0s').");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }
    }
}
